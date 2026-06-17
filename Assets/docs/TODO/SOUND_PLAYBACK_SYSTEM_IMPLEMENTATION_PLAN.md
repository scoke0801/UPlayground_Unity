# 사운드 재생 시스템 구현 계획서 v1.0

> 작성일: 2026-06-16
> 상태: Stage 1~5, 7 1차 구현 완료 / Unity 플레이 검증 대기
> 대상: `SoundManager` 기반 BGM/SFX/UI/Voice 재생, 거리 감쇠, MotionEvent 연동, 설정 볼륨 연동

---

## 진행 현황 (2026-06-16 기준)

| 단계 | 상태 | 검증 |
|---|---|---|
| Stage 1 - 데이터와 매니저 골격 | 완료 | `dotnet build UPlayground.sln --no-restore` 성공 |
| Stage 2 - 2D/3D SFX 재생과 풀링 | 완료 | 컴파일 |
| Stage 3 - 거리 감쇠 | 완료 | 컴파일 |
| Stage 4 - 기존 시스템 연결 | 완료 | 컴파일 |
| Stage 5 - BGM 크로스페이드 | 완료 | 컴파일 |
| Stage 6 - Footstep 지형별 확장 | 제외 | 현재 요구 범위에서 불필요 |
| Stage 7 - 에디터 검증과 운영 편의 | 완료 | `dotnet build UPlayground.sln --no-restore` 성공 |

남은 검증:

- Unity Editor 플레이 모드에서 실제 출력, MixerGroup 라우팅, Addressables `SoundDatabase` 로드 확인.
- `SoundDatabase` 에셋 생성 및 key 등록: `footstep_default`, `LevelUp`, 각 VitalOrb `collectSoundName`.
- `SoundDatabaseSO` Inspector의 `Validate Sound Database` 버튼으로 key/clip/거리 설정 검증.

## 0. 결론

현재 프로젝트에는 오디오 설정 데이터와 `AudioMixer` 볼륨 적용 경로는 이미 있다. 반면 실제 재생 경로는 분산되어 있다.

- `MotionEvent_PlaySound`는 `AudioSource.PlayClipAtPoint`를 직접 호출한다.
- `FootstepEvent`는 로그만 출력한다.
- `VitalOrbActor`, `LevelUpFeedbackHandler` 등에는 AudioManager 미구현 TODO가 남아 있다.
- `SettingsManager` / `SettingsApplier`는 `MasterVolume`, `BGMVolume`, `SFXVolume`, `VoiceVolume` 파라미터를 이미 적용한다.

따라서 새 시스템의 목표는 **재생 요청을 `SoundManager`로 단일화**하고, 내부에서 `AudioMixerGroup`, `AudioSource` 풀, Addressables 기반 사운드 DB, 거리 감쇠 정책을 관리하는 것이다.

핵심 원칙:

1. 게임 코드와 MotionEvent는 직접 `AudioSource`를 만들거나 `PlayClipAtPoint`를 호출하지 않는다.
2. 2D/3D 여부, 거리 감쇠, 믹서 라우팅, 쿨다운, 동시 재생 제한은 `SoundEntry` 데이터로 관리한다.
3. 거리 증감은 Unity `AudioSource`의 3D 감쇠 기능을 기본으로 쓰고, 필요한 경우 `SoundManager`에서 거리 프리컷을 보강한다.
4. 기존 `SettingsData`의 볼륨 구조는 유지한다.

---

## 1. 웹 조사 요약

Unity 공식 문서 기준:

- `AudioSource.spatialBlend`가 `0`이면 2D, `1`이면 완전한 3D 사운드다. 중간값은 2D/3D 블렌드다.
  - https://docs.unity3d.com/ScriptReference/AudioSource.html
- 3D 사운드는 `minDistance`, `maxDistance`, `rolloffMode`로 거리 감쇠를 제어한다.
  - https://docs.unity3d.com/Manual/AudioSource-reference.html
- `minDistance` 안에서는 최대 볼륨을 유지하고, 그 밖에서 감쇠가 시작된다.
- `Linear Rolloff`에서는 `maxDistance` 지점에서 볼륨이 0이 된다.
- `Logarithmic Rolloff`는 가까울 때 빠르게 작아지고 멀어질수록 완만하게 줄지만, Unity 문서상 `maxDistance`로 완전 무음 컷을 보장하지 않는다.
- `Custom Rolloff`는 거리별 볼륨 커브를 직접 지정한다.
  - https://docs.unity3d.com/ScriptReference/AudioSource.SetCustomCurve.html
- `AudioSource.outputAudioMixerGroup`으로 사운드 출력을 특정 Mixer Group에 라우팅한다.
  - https://docs.unity3d.com/ScriptReference/AudioSource-outputAudioMixerGroup.html
- `AudioMixer.SetFloat`는 노출된 Mixer 파라미터를 코드에서 제어한다.
  - https://docs.unity3d.com/ScriptReference/Audio.AudioMixer.SetFloat.html

설계 반영:

- 액션 전투 SFX는 `Logarithmic3D` 또는 `Custom3D`를 기본으로 사용한다.
- 발자국, 충돌, 몬스터 원거리 착탄처럼 멀리서 사라져야 하는 소리는 `Linear3D` 또는 `Custom3D + 프리컷`을 사용한다.
- UI, 보상음, 메뉴음은 거리 감쇠 없는 2D 사운드로 처리한다.

---

## 2. 목표 아키텍처

```
GameManager
└── SoundManager : BaseManager<SoundManager>, IManager
    ├── SoundDatabaseSO              Addressables: "SoundDatabase"
    ├── BGM AudioSource A/B          크로스페이드
    ├── 2D SFX AudioSource Pool      UI/SFX/Voice 단발 재생
    ├── 3D SFX AudioSource Pool      월드 위치 기반 단발 재생
    ├── ActiveSound 관리             반환/쿨다운/동시 재생 제한
    └── AudioMixerGroup 라우팅        BGM/SFX/UI/Voice/Ambience
```

### 2.1 매니저 등록 위치

`GameManager.InitializeManagers`에서 `SettingsManager` 뒤에 등록한다.

```csharp
RegisterManager(SettingsManager.Instance);
RegisterManager(SoundManager.Instance);
RegisterManager(AssetManager.Instance);
```

이유:

- `SettingsManager`가 설정 데이터를 로드하고 `AudioMixer` 볼륨을 먼저 적용한다.
- `SoundManager`는 이후 사운드 DB와 재생 소스를 준비한다.
- `SoundManager`가 `AudioMixerGroup`을 직접 보유하더라도 전역 볼륨 파라미터는 기존 설정 시스템이 계속 담당한다.

---

## 3. 폴더 구조

```
Assets/02.Scripts/
├── Manager/Sound/
│   ├── SoundManager.cs
│   ├── SoundManager.Bgm.cs
│   ├── SoundManager.Sfx.cs
│   ├── AudioSourcePool.cs
│   └── ActiveSoundHandle.cs
├── Data/Sound/
│   ├── SoundDatabaseSO.cs
│   ├── SoundEntry.cs
│   ├── SoundBusType.cs
│   ├── SoundDistanceMode.cs
│   └── FootstepSoundDatabaseSO.cs
└── GameActor/Component/Common/
    └── SurfaceTypeProvider.cs       선택. 지형/머티리얼 기반 발자국 확장용

Assets/10.Datas/Audio/
├── SoundDatabase.asset
└── FootstepSoundDatabase.asset

Assets/06.Sounds/
├── BGM/
├── SFX/
├── UI/
├── Voice/
└── Ambience/
```

---

## 4. 데이터 모델

### 4.1 SoundBusType

```csharp
public enum SoundBusType
{
    Master,
    BGM,
    SFX,
    UI,
    Voice,
    Ambience
}
```

### 4.2 SoundDistanceMode

```csharp
public enum SoundDistanceMode
{
    None2D,
    Logarithmic3D,
    Linear3D,
    Custom3D
}
```

### 4.3 SoundEntry

```csharp
[Serializable]
public sealed class SoundEntry
{
    public string key;
    public AudioClip clip;
    public SoundBusType bus = SoundBusType.SFX;
    public SoundDistanceMode distanceMode = SoundDistanceMode.Logarithmic3D;

    [Range(0f, 1f)] public float volume = 1f;
    public float pitchMin = 1f;
    public float pitchMax = 1f;

    public float minDistance = 1.5f;
    public float maxDistance = 24f;
    public AnimationCurve customRolloff;
    public bool preCullByMaxDistance = true;

    public float cooldown = 0f;
    public int maxSimultaneous = 4;
    [Range(0, 256)] public int priority = 128;
}
```

### 4.4 SoundDatabaseSO

```csharp
[CreateAssetMenu(fileName = "SoundDatabase", menuName = "UPlayGround/Audio/Sound Database")]
public sealed class SoundDatabaseSO : ScriptableObject
{
    [SerializeField] private List<SoundEntry> entries = new();

    private Dictionary<string, SoundEntry> _lookup;

    public void Initialize();
    public bool TryGet(string key, out SoundEntry entry);
}
```

주의:

- `key` 중복은 에디터 검증 대상으로 둔다.
- 장기적으로는 `FXKeyType`처럼 자동 생성 enum을 만들 수 있지만, 1차 구현은 문자열 키로 시작한다.

---

## 5. SoundManager API

외부에서 사용할 API는 작게 유지한다.

```csharp
public void Play(string key, Vector3? position = null, float volumeScale = 1f);
public void PlaySfx(string key, Vector3 position, float volumeScale = 1f);
public void PlayUi(string key, float volumeScale = 1f);
public void PlayVoice(string key, float volumeScale = 1f);
public void PlayClip(AudioClip clip, SoundBusType bus, Vector3? position = null, float volumeScale = 1f);

public void PlayBgm(string key, float fadeTime = 1f);
public void StopBgm(float fadeTime = 1f);
```

권장 사용:

```csharp
SoundManager.Instance.PlaySfx("enemy_hit_light", hitPoint);
SoundManager.Instance.PlayUi("ui_confirm");
SoundManager.Instance.PlayBgm("field_day", 1.5f);
```

---

## 6. 거리 감쇠 처리

### 6.1 기본 규칙

거리 감쇠는 `SoundEntry.distanceMode`에 따라 `AudioSource`에 적용한다.

```csharp
private void ApplyDistance(AudioSource source, SoundEntry entry, Vector3? position)
{
    bool is3D = entry.distanceMode != SoundDistanceMode.None2D && position.HasValue;

    source.spatialBlend = is3D ? 1f : 0f;
    source.transform.position = position ?? Vector3.zero;
    source.minDistance = entry.minDistance;
    source.maxDistance = entry.maxDistance;

    switch (entry.distanceMode)
    {
        case SoundDistanceMode.None2D:
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            break;
        case SoundDistanceMode.Logarithmic3D:
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            break;
        case SoundDistanceMode.Linear3D:
            source.rolloffMode = AudioRolloffMode.Linear;
            break;
        case SoundDistanceMode.Custom3D:
            source.rolloffMode = AudioRolloffMode.Custom;
            if (entry.customRolloff != null)
                source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, entry.customRolloff);
            break;
    }
}
```

### 6.2 거리 프리컷

`Logarithmic Rolloff`는 멀어져도 완전 무음이 되지 않는 성격이 있으므로, hot path 사운드는 재생 전에 거리 프리컷을 적용한다.

```csharp
private bool IsAudible(SoundEntry entry, Vector3 position)
{
    if (!entry.preCullByMaxDistance)
        return true;

    Transform listener = AudioListenerTransform;
    if (listener == null)
        return true;

    float max = entry.maxDistance;
    return (listener.position - position).sqrMagnitude <= max * max;
}
```

### 6.3 권장 프리셋

| 용도 | 모드 | minDistance | maxDistance | 비고 |
|---|---:|---:|---:|---|
| UI 버튼 | `None2D` | - | - | `PlayUi` 사용 |
| VitalOrb 획득 | `None2D` | - | - | 플레이어 보상 피드백이므로 2D 권장 |
| 플레이어 공격/피격 | `Custom3D` | 2~4 | 20~30 | 가까운 전투 가독성 우선 |
| 몬스터 공격 | `Logarithmic3D` | 2 | 25~35 | 위치감 유지 |
| 착탄/폭발 | `Linear3D` 또는 `Custom3D` | 3 | 35~45 | 먼 소리는 확실히 컷 |
| 발자국 | `Custom3D` | 0.8~1.2 | 8~12 | 쿨다운 필수 |
| Ambience | `Logarithmic3D` 또는 2D Loop | 5 | 40~80 | 씬 연출에 따라 선택 |

### 6.4 기본 Custom Rolloff 예시

```csharp
new AnimationCurve(
    new Keyframe(0f, 1f),
    new Keyframe(0.2f, 0.85f),
    new Keyframe(0.55f, 0.35f),
    new Keyframe(1f, 0f)
);
```

`AudioSource.SetCustomCurve`는 `maxDistance` 기준으로 커브를 스케일링하므로, x축은 `0..1` 기준으로 작성한다.

---

## 7. AudioSource 풀링

### 7.1 필요성

전투 중 피격음, 발자국, 투사체 착탄음은 같은 프레임에 여러 번 발생할 수 있다. 매번 `GameObject`와 `AudioSource`를 생성/파괴하면 GC와 스파이크가 생긴다.

### 7.2 구조

```
AudioSourcePool
├── Queue<AudioSource> _inactive
├── List<ActiveSoundHandle> _active
├── Rent()
├── Return(AudioSource)
└── Tick()
```

`SoundManager.OnUpdate`에서 active 목록을 검사하고 재생이 끝난 소스를 반환한다.

### 7.3 반환 규칙

- `source.isPlaying == false`이면 반환한다.
- 루프 사운드는 풀 반환 대상이 아니며 BGM/Ambience 전용 API에서 관리한다.
- `PlayClip`로 외부 클립을 재생한 경우에도 동일한 반환 경로를 사용한다.

---

## 8. 믹서 라우팅

권장 Mixer Group:

```
Master
├── BGM
├── SFX
│   ├── GameplaySFX
│   └── UISFX
├── Voice
└── Ambience
```

`SettingsApplier`는 현재 구조를 유지한다.

```csharp
mixer.SetFloat("MasterVolume", VolumeToDb(data.masterVolume));
mixer.SetFloat("BGMVolume", VolumeToDb(data.bgmVolume));
mixer.SetFloat("SFXVolume", VolumeToDb(data.sfxVolume));
mixer.SetFloat("VoiceVolume", VolumeToDb(data.voiceVolume));
```

`SoundManager`는 `SoundBusType`에 따라 `AudioSource.outputAudioMixerGroup`을 지정한다.

UI 사운드를 SFX 볼륨과 함께 조절하려면 `UISFX`를 `SFX` 하위 그룹으로 둔다. UI만 별도 볼륨이 필요해지면 `SettingsData`에 `uiVolume`을 추가하는 Phase 2 작업으로 분리한다.

---

## 9. 기존 코드 연동

### 9.1 MotionEvent_PlaySound

현재 직접 재생:

```csharp
AudioSource.PlayClipAtPoint(audioClip, target.transform.position, volume);
```

변경:

```csharp
public override void Execute(GameObject target)
{
    if (audioClip == null) return;

    Vector3? position = is3D && target != null
        ? target.transform.position
        : null;

    SoundManager.Instance.PlayClip(
        audioClip,
        SoundBusType.SFX,
        position,
        volume);
}
```

장기 개선:

```csharp
public string soundKey;
public AudioClip fallbackClip;
```

`soundKey`가 있으면 DB 기반 재생, 없으면 `fallbackClip` 재생으로 호환한다.

### 9.2 FootstepEvent

1차 구현:

```csharp
SoundManager.Instance.PlaySfx("footstep_default", target.transform.position, volume);
```

2차 구현:

```
FootstepEvent
└── target 위치에서 아래 Raycast
    └── SurfaceType 판정
        └── FootstepSoundDatabaseSO에서 key 선택
            └── SoundManager.PlaySfx(...)
```

### 9.3 VitalOrbActor

`VitalOrbDataSO.collectSoundName`을 사용한다.

```csharp
if (!string.IsNullOrEmpty(_data.collectSoundName))
    SoundManager.Instance.PlayUi(_data.collectSoundName);
```

획득음은 플레이어 보상 피드백이므로 기본은 2D를 권장한다. 월드 위치감을 살리고 싶으면 `Play`에 position을 넘기고 해당 SoundEntry를 `Logarithmic3D`로 설정한다.

### 9.4 LevelUpFeedbackHandler

TODO 훅을 `PlayUi("level_up")`로 연결한다.

### 9.5 UI 버튼음

`UIManager`나 개별 UI에서 직접 AudioSource를 두지 말고 `SoundManager.PlayUi`로 통일한다.

---

## 10. 단계별 구현 계획

### Stage 1 - 데이터와 매니저 골격

작업:

1. `SoundBusType`, `SoundDistanceMode`, `SoundEntry`, `SoundDatabaseSO` 추가.
2. `SoundManager : BaseManager<SoundManager>, IManager` 추가.
3. Addressables 키 `"SoundDatabase"`로 DB 비동기 로드.
4. `GameManager`에 `SoundManager` 등록.
5. `SoundManager` 미로드 상태에서 호출되면 경고 후 무시한다.

검증:

- Unity 컴파일 성공.
- `SoundDatabase` 미등록 시 명확한 에러 로그.
- `SoundManager.Instance` 자동 생성 및 `GameManager` 등록 확인.

### Stage 2 - 2D/3D SFX 재생과 풀링

작업:

1. `AudioSourcePool` 구현.
2. 2D 풀과 3D 풀 분리.
3. `Play`, `PlaySfx`, `PlayUi`, `PlayClip` 구현.
4. `SoundEntry.cooldown`, `maxSimultaneous` 최소 구현.
5. `SoundBusType`에 따른 `AudioMixerGroup` 라우팅.

검증:

- 2D UI 사운드 재생.
- 3D 위치 사운드 재생.
- 같은 key 쿨다운 적용.
- 재생 종료 후 pool 반환.
- Audio Profiler에서 과도한 GameObject 생성 없음.

### Stage 3 - 거리 감쇠

작업:

1. `minDistance`, `maxDistance`, `rolloffMode`, `spatialBlend` 적용.
2. `Custom3D`일 때 `SetCustomCurve` 적용.
3. `preCullByMaxDistance` 거리 프리컷 적용.
4. AudioListener Transform 캐싱.

검증:

- 가까운 거리에서 최대 볼륨.
- `minDistance` 밖에서 감쇠 시작.
- `Linear3D`는 `maxDistance` 근처에서 사실상 무음.
- `Logarithmic3D + preCullByMaxDistance`는 max 밖에서 재생 요청 자체가 무시됨.
- 전투 중 다수 SFX가 멀리서 불필요하게 재생되지 않음.

### Stage 4 - 기존 시스템 연결

작업:

1. `MotionEvent_PlaySound`를 `SoundManager` 경유로 변경.
2. `FootstepEvent` 1차 연결.
3. `VitalOrbActor` TODO 연결.
4. `LevelUpFeedbackHandler` TODO 연결.
5. UI 버튼음 진입점 정리.

검증:

- MotionSet 타임라인의 PlaySound 이벤트 정상 재생.
- 3D 체크 시 캐릭터 위치에서 소리가 난다.
- 2D 체크 시 거리와 관계없이 들린다.
- VitalOrb 획득음 재생.
- Footstep 기본음 재생.

### Stage 5 - BGM 크로스페이드

작업:

1. BGM 전용 AudioSource 2개 생성.
2. `PlayBgm(key, fadeTime)` 구현.
3. 같은 BGM 재요청은 무시하거나 restart 옵션으로 분기.
4. `StopBgm(fadeTime)` 구현.
5. 씬 전환 시 BGM 유지/교체 정책 추가.

검증:

- A/B 소스 크로스페이드.
- BGM은 `BGM` Mixer Group으로 라우팅.
- 설정 메뉴의 BGM 볼륨 변경 반영.
- 씬 전환 중 의도치 않은 중복 재생 없음.

### Stage 6 - Footstep 지형별 확장

상태: 제외. 현재 요구 범위에서는 지형별 발자국 확장이 필요하지 않다.

작업:

1. `SurfaceType` enum 추가.
2. `FootstepSoundDatabaseSO` 추가.
3. Raycast 기반 표면 판정.
4. Terrain/Collider/PhysicMaterial/Tag 기반 판정 우선순위 정의.
5. 발 좌우, 캐릭터 타입, 장비별 변형은 데이터 확장 포인트로 남긴다.

검증:

- Default 표면 폴백.
- Grass/Stone/Metal 등 표면별 key 선택.
- 발자국 쿨다운으로 중복 재생 방지.

### Stage 7 - 에디터 검증과 운영 편의

상태: 완료.

작업:

1. `SoundDatabaseSO` 중복 key 검사. 완료.
2. null clip 검사. 완료.
3. `Custom3D`인데 curve가 비어 있는 데이터 경고. 완료.
4. 거리/피치/쿨다운/동시 재생 제한 값 이상 경고. 완료.
5. `None2D`인데 `preCullByMaxDistance`가 켜진 데이터 경고. 완료.
6. 사용되지 않는 SoundEntry 탐색은 후속 툴로 분리.

검증:

- 잘못된 데이터가 Console 경고로 드러난다.
- 런타임 null reference 없이 폴백/무시된다.

---

## 11. 위험 요소와 완화

| 위험 | 설명 | 완화 |
|---|---|---|
| Addressables 로드 전 재생 요청 | 부팅 직후 MotionEvent/피드백이 먼저 호출될 수 있음 | 미로드 시 경고 후 무시. 필요하면 pending queue는 BGM에만 적용 |
| AudioMixerGroup 미할당 | 소리는 나지만 설정 볼륨이 적용되지 않음 | `SoundManager.Init`에서 필수 그룹 null 검사 |
| Logarithmic 사운드가 멀리서 계속 남음 | 전투 SFX가 먼 거리에서도 미세하게 들릴 수 있음 | `preCullByMaxDistance` 기본 true |
| 풀 크기 부족 | 전투 중 소스가 부족해 소리가 끊김 | 초기 16~32개, 부족 시 확장하되 상한 로그 |
| 같은 key 과다 재생 | 다단 히트/발자국이 소리를 덮음 | cooldown + maxSimultaneous |
| MotionEvent 직렬화 호환성 | 기존 `audioClip`, `is3D`, `volume` 데이터 손실 가능 | 1차 구현은 기존 필드 유지 후 내부 재생 경로만 변경 |
| UI 사운드 일시정지 | `AudioListener.pause` 사용 시 UI음도 멈출 수 있음 | UI/BGM source는 필요 시 `ignoreListenerPause = true` |

---

## 12. 검증 체크리스트

### 컴파일

- Unity 컴파일 에러 0.
- 신규 `.meta` 생성 확인.
- `GameManager` 매니저 등록 순서 확인.

### 런타임

- 설정 메뉴의 Master/BGM/SFX/Voice 볼륨이 실제 출력에 반영된다.
- BGM 재생/교체/정지 동작이 자연스럽다.
- MotionEvent PlaySound가 2D/3D 옵션에 따라 다르게 들린다.
- 거리별 3D SFX가 가까워질수록 커지고 멀어질수록 작아진다.
- `maxDistance` 밖 SFX는 프리컷으로 재생되지 않는다.
- 전투 중 다단 히트에서 소리가 과도하게 겹치지 않는다.
- Footstep 기본음이 중복 없이 재생된다.
- VitalOrb 획득음과 레벨업음이 정상 재생된다.

### 성능

- 반복 SFX 재생 중 GC Alloc이 과도하게 증가하지 않는다.
- `AudioSource` GameObject가 무한히 늘지 않는다.
- 씬 전환 후 active sound 목록이 정리된다.

---

## 13. 최종 목표 형태

최종적으로 사운드 재생 책임은 다음처럼 분리한다.

```
SettingsManager / SettingsApplier
└── 볼륨 저장, 로드, AudioMixer exposed parameter 적용

SoundDatabaseSO
└── key 기반 사운드 데이터 보관

SoundManager
└── 재생 API, 풀링, 믹서 라우팅, 거리 감쇠, BGM 크로스페이드

MotionEvent / GameActor / UI
└── SoundManager에 key 또는 clip 재생 요청만 전달
```

이 구조에서는 게임플레이 코드가 오디오 구현 세부사항을 알 필요가 없다. 사운드 교체, 거리 감쇠 튜닝, 믹서 라우팅, 중복 제한을 모두 데이터와 `SoundManager`에서 관리할 수 있다.
