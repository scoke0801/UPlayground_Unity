# 궁극기 연출 시스템 설계 문서

## 개요

현재 `CameraSnapshotSequence`는 궁극기 연출의 카메라 레일로 사용할 수 있다. 다만 명조식 궁극기처럼 캐릭터 애니메이션, 카메라, 시간 제어, VFX, SFX, UI 숨김, 전투 판정을 하나의 연출로 묶으려면 카메라 위에 별도 시퀀스 레이어가 필요하다.

이 문서는 기존 카메라 스냅샷 시스템을 재사용하면서, 캐릭터별 궁극기 연출을 데이터 주도로 구성하기 위한 설계안이다.

핵심 목표:

- `CameraSnapshotProfile`을 궁극기 카메라 시퀀스의 구성 요소로 재사용
- 캐릭터별 궁극기 데이터를 `ScriptableObject`로 분리
- 연출 중 입력, AI, 카메라, HUD, 피격 반응을 일관되게 잠금
- 연출 시작/타격/종료 타이밍을 MotionSet 또는 별도 타임라인 데이터와 동기화
- 실패/인터럽트/씬 전환 시 원상 복귀 누락을 줄이는 복구 정책 제공

---

## 현재 기반

### 이미 구현된 기능

| 시스템 | 활용 방식 |
|--------|----------|
| `CameraSnapshotProfile` | 궁극기 카메라 샷 목록, FOV, 진입 블렌드, 공전 이동 |
| `CameraSnapshotSequenceMode` | 궁극기 중 카메라 모드 점유 |
| `CameraSnapshotSequenceEvent` | MotionSet 이벤트에서 카메라 시퀀스 실행 |
| `CameraSnapshotSequenceTrigger` | 맵 트리거 기반 시네마틱 카메라 실행 |
| `MotionSetAsset` / `MotionEventExecutor` | 애니메이션 타임라인 이벤트 발화 |
| `MotionEvent_TimeScale` | 연출 중 시간 배율 제어 후보 |
| `MotionEvent_CameraEffect` | FOV, 흔들림, 회전 같은 카메라 효과 후보 |
| `MotionEvent_SpawnSkill` / `SpawnProjectile` | 궁극기 VFX/피격 오브젝트 스폰 후보 |
| `PlayerCombat`, `EnemyCombat` | 궁극기 데미지/피격 반응 연결 지점 |
| `InputManager` | 연출 중 입력 레이어 차단 지점 |

### 부족한 부분

| 부족한 시스템 | 문제 |
|---------------|------|
| 궁극기 시퀀스 오케스트레이터 | 카메라, 애니메이션, VFX, SFX, 데미지, 잠금 상태를 하나의 실행 단위로 묶지 못함 |
| 전투/AI/입력 잠금 컨텍스트 | 카메라 입력만 잠기고 플레이어 조작, AI, 피격 반응은 별도 제어 필요 |
| 캐릭터별 궁극기 데이터 | `CharacterActorType`별 궁극기 구성 데이터가 없음 |
| 타겟 배치 보정 | 연출 프레임 안으로 시전자/타겟을 정렬하는 정책이 없음 |
| 후처리/화면 연출 타임라인 | Volume, 화면 플래시, HUD 숨김, 컷 전환을 통합 제어하지 못함 |
| 종료 복구 정책 | 인터럽트, 사망, 씬 전환 시 잠금/카메라/TimeScale 복구 책임이 분산될 위험 |

---

## 목표 아키텍처

```
UltimateSequenceAsset
├── CharacterActorType ownerType
├── MotionSetAsset motionSet
├── CameraSnapshotProfile cameraProfile
├── UltimateTargetPolicy targetPolicy
├── UltimateGameplayLockSettings lockSettings
├── UltimatePlacementSettings placementSettings
├── UltimateTimelineEvent[] events
└── UltimateRestorePolicy restorePolicy

UltimateSequencePlayer
├── ValidateRequest(...)
├── ResolveTarget(...)
├── ApplyGameplayLock(...)
├── ApplyPlacement(...)
├── PlayMotion(...)
├── PushCameraSnapshotSequence(...)
├── TickTimelineEvents(...)
└── RestoreAll()
```

### 실행 흐름

```
PlayerCombat.RequestUltimate()
        │
        ▼
UltimateSequencePlayer.Play(asset, caster, targetContext)
        │
        ├── 중복 실행 / 게이지 / 상태 검증
        ├── 타겟 선택 및 배치 보정
        ├── 입력/AI/피격/HUD/카메라 잠금
        ├── MotionSet 궁극기 애니메이션 재생
        ├── CameraSnapshotProfile 재생
        ├── VFX/SFX/TimeScale/PostProcess/데미지 이벤트 실행
        └── 완료 또는 인터럽트 시 RestoreAll()
```

---

## 제안 데이터 모델

### UltimateSequenceAsset

```csharp
namespace UPlayGround.Data
{
    [CreateAssetMenu(fileName = "UltimateSequence", menuName = "UPlayGround/SO/Combat/Ultimate Sequence")]
    public class UltimateSequenceAsset : ScriptableObject
    {
        public CharacterActorType ownerType;
        public MotionSetAsset motionSet;
        public CameraSnapshotProfile cameraProfile;
        public UltimateTargetPolicy targetPolicy;
        public UltimateGameplayLockSettings lockSettings;
        public UltimatePlacementSettings placementSettings;
        public UltimateTimelineEvent[] events;
        public UltimateRestorePolicy restorePolicy;
    }
}
```

필드 역할:

| 필드 | 설명 |
|------|------|
| `ownerType` | 궁극기를 소유한 캐릭터 타입 |
| `motionSet` | 궁극기 애니메이션 MotionSet |
| `cameraProfile` | 궁극기 카메라 스냅샷 프로필 |
| `targetPolicy` | 타겟 선택, 다수 타겟 처리, 락온 대상 사용 여부 |
| `lockSettings` | 입력, AI, 카메라, HUD, 피격 반응 잠금 정책 |
| `placementSettings` | 시전자/타겟 위치 보정 정책 |
| `events` | VFX, SFX, TimeScale, 데미지, 후처리 이벤트 |
| `restorePolicy` | 정상 종료/인터럽트/씬 전환 시 복구 정책 |

### UltimateTargetPolicy

```csharp
public enum UltimateTargetMode
{
    CurrentLockOn,
    NearestEnemy,
    ForwardCone,
    ManualTransform,
    None
}
```

| 옵션 | 설명 |
|------|------|
| `CurrentLockOn` | 현재 락온 대상을 궁극기 주 타겟으로 사용 |
| `NearestEnemy` | 범위 내 가장 가까운 적을 선택 |
| `ForwardCone` | 전방 부채꼴 범위 내 대표 타겟 선택 |
| `ManualTransform` | 이벤트/트리거에서 전달한 Transform 사용 |
| `None` | 타겟 없는 자기 중심 연출 |

추가 필드 후보:

- `float searchRadius`
- `float coneAngle`
- `LayerMask targetLayer`
- `bool requireTarget`
- `bool includeMultipleTargets`
- `int maxTargets`

### UltimateGameplayLockSettings

```csharp
[System.Serializable]
public class UltimateGameplayLockSettings
{
    public bool lockPlayerInput = true;
    public bool lockCameraInput = true;
    public bool pauseEnemyAI = true;
    public bool freezeTargets = true;
    public bool ignoreCasterDamage = true;
    public bool ignoreTargetReactions = true;
    public bool hideHud = true;
    public bool releaseLockOnOnEnter = false;
}
```

잠금은 `UltimateSequencePlayer`가 한 곳에서 획득하고 해제해야 한다. 개별 MotionEvent가 직접 입력/AI/타임스케일을 건드리면 종료 누락 시 복구가 어렵다.

### UltimatePlacementSettings

궁극기 연출은 카메라뿐 아니라 캐릭터와 타겟이 프레임 안에 들어오도록 배치가 필요하다.

| 필드 | 설명 |
|------|------|
| `bool warpCaster` | 시전자를 연출 기준 위치로 보정 |
| `bool warpPrimaryTarget` | 대표 타겟을 연출 위치로 보정 |
| `Vector3 casterOffsetFromTarget` | 타겟 기준 시전자 위치 |
| `Vector3 targetOffsetFromCaster` | 시전자 기준 타겟 위치 |
| `bool faceTarget` | 시작 시 서로 바라보도록 회전 |
| `float placementBlendDuration` | 순간이동 대신 짧은 보간을 사용할 때 |
| `bool restorePositionsOnFinish` | 종료 후 원래 위치로 복귀할지 여부 |

KCC 사용 액터는 단순 `transform.position` 변경보다 기존 `ActorMovementController` 또는 KCC Motor의 위치 보정 API를 통해 이동시키는 편이 안전하다.

### UltimateTimelineEvent

MotionSet의 `MotionEvent`와 겹치지 않게, 궁극기 전용 이벤트는 큰 연출 단위만 담당한다.

```csharp
public enum UltimateTimelineEventType
{
    SpawnVfx,
    PlaySfx,
    PlayVoice,
    ApplyTimeScale,
    ApplyPostProcess,
    HideHud,
    DamageWindow,
    CameraShake,
    CustomCallback
}
```

권장 분리:

| 이벤트 | 권장 위치 |
|--------|----------|
| 히트박스 활성화, 투사체 발사 | 기존 MotionEvent |
| 카메라 스냅샷 시작 | UltimateSequencePlayer 또는 MotionEvent |
| TimeScale, Volume, HUD, 대형 VFX | UltimateTimelineEvent |
| 평타/스킬 공통 VFX | 기존 MotionEvent |

---

## 런타임 시스템

### UltimateSequencePlayer

`UltimateSequencePlayer`는 `PlayerActor` 또는 `PlayerCombat` 하위 컴포넌트로 붙이는 방식이 적합하다. 싱글플레이 구조이므로 처음에는 전역 매니저보다 플레이어 컴포넌트로 시작하는 편이 변경 범위가 작다.

책임:

- 현재 캐릭터 타입에 맞는 `UltimateSequenceAsset` 선택
- 궁극기 실행 가능 상태 검증
- 타겟 해석
- 입력/AI/카메라/HUD 잠금 획득
- 위치 보정
- MotionSet 재생 요청
- `CameraManager.PushCameraSnapshotSequence()` 호출
- 궁극기 전용 타임라인 이벤트 실행
- 종료/실패/인터럽트 복구

### UltimateRuntimeContext

```csharp
public sealed class UltimateRuntimeContext
{
    public PlayerActor Caster;
    public Transform PrimaryTarget;
    public List<Transform> Targets;
    public UltimateSequenceAsset Asset;
    public float ElapsedTime;
    public bool IsInterrupted;
}
```

모든 이벤트는 이 컨텍스트를 받아 실행한다. 이렇게 해야 VFX, 데미지, 카메라, 후처리 이벤트가 같은 타겟 정보를 공유할 수 있다.

### 복구 정책

궁극기 연출은 실패 시 원복이 더 중요하다.

복구 대상:

- 카메라 모드
- 입력 잠금
- AI 정지
- 타겟 이동/정지
- TimeScale
- HUD 표시 상태
- Volume/후처리
- 액터 무적/피격 반응 무시
- 임시 VFX 오브젝트

권장 API:

```csharp
public void RestoreAll(UltimateRestoreReason reason)
```

```csharp
public enum UltimateRestoreReason
{
    Completed,
    Interrupted,
    CasterDead,
    TargetLost,
    SceneChanged,
    Error
}
```

---

## CameraSnapshot 확장 요구

현재 카메라 스냅샷은 궁극기 연출에 사용할 수 있지만, 장기적으로는 아래 확장이 유용하다.

| 확장 | 이유 |
|------|------|
| 샷별 LookAt 대상 | 얼굴, 무기, 타겟, 월드 포인트를 샷마다 바꾸기 위함 |
| 샷별 LookAt offset/weight | 완전 고정 회전과 자동 추적 사이를 조절 |
| 샷별 Roll | 궁극기 컷의 기울어진 구도 연출 |
| 샷별 이벤트 마커 | 카메라 컷 타이밍에 VFX/SFX/플래시 동기화 |
| 프로필 큐잉 | 이미 궁극기 카메라가 돌 때 다른 카메라 요청 처리 |
| 토큰 기반 Stop | 같은 모드라도 내가 시작한 시퀀스만 종료하기 위함 |

단, 1차 궁극기 구현에서는 현재 `CameraSnapshotProfile`을 그대로 쓰고, 부족한 동기화는 `UltimateSequencePlayer`에서 담당하는 편이 안전하다.

---

## 구현 단계

### 1단계: 최소 궁극기 실행기

- `UltimateSequenceAsset` 추가
- `UltimateSequencePlayer` 추가
- `PlayerCombat`에서 궁극기 입력 또는 테스트 API로 실행
- `CameraSnapshotProfile` 재생
- 궁극기 MotionSet 재생
- 완료 시 카메라/입력 복구

### 2단계: 잠금 컨텍스트

- 입력 잠금
- 카메라 입력 잠금
- HUD 숨김
- 적 AI 정지 또는 슬로우
- 시전자 무적/피격 반응 잠금
- 타겟 피격 반응 무시

### 3단계: 타겟/배치 보정

- 락온 대상 우선 선택
- 전방/근거리 타겟 폴백
- 시전자/타겟 FaceTo 처리
- KCC 기반 위치 보정
- 종료 후 위치 복구 여부 옵션

### 4단계: 연출 이벤트

- TimeScale curve
- Volume/PostProcess 페이드
- 대형 VFX/SFX/Voice
- 데미지 윈도우
- 화면 플래시/페이드

### 5단계: 에디터 도구

- 캐릭터별 `UltimateSequenceAsset` 생성 도구
- 카메라 프로필, MotionSet, VFX 누락 검증
- PlayMode 테스트 버튼
- 샷/이벤트 타임라인 미리보기

---

## 에디터 워크플로 제안

1. `Camera Snapshot 에디터`에서 궁극기 카메라 프로필을 만든다.
2. `MotionSet Editor`에서 궁극기 애니메이션과 타격 이벤트를 만든다.
3. `UltimateSequenceAsset`에 카메라 프로필, MotionSet, VFX/SFX/TimeScale 이벤트를 연결한다.
4. PlayMode에서 `UltimateSequencePlayer` 테스트 버튼으로 실행한다.
5. 카메라 샷, 애니메이션 타격 시점, VFX 스폰 시점을 반복 조정한다.

---

## 주의 사항

1. 궁극기 중 `Time.timeScale`을 직접 바꾸는 코드는 한 곳으로 모아야 한다. 기존 HitStop/TimeScale 시스템과 충돌하면 복구가 어려워진다.
2. 카메라 시퀀스가 끝났다고 궁극기 연출이 끝난 것은 아니다. 궁극기 종료 기준은 `UltimateSequencePlayer`가 결정해야 한다.
3. MotionEvent에서 카메라를 종료하는 방식과 UltimateSequencePlayer가 종료하는 방식이 섞이면 중복 Pop 위험이 있다.
4. 적 AI 정지는 전체 정지와 타겟만 정지를 분리해야 한다. 보스전에서는 주변 적까지 멈추는 것이 어색할 수 있다.
5. 위치 보정은 KCC Motor와 충돌하지 않게 처리해야 한다.
6. 캐릭터별 궁극기는 데이터 중심으로 만들되, 특수 캐릭터 전용 로직은 `CustomCallback` 또는 별도 `UltimateAction`으로 격리한다.

---

## 관련 문서

- `Assets/docs/Complete/CAMERA_SNAPSHOT_SEQUENCE_GUIDE.md`
- `Assets/docs/Complete/CAMERA_SYSTEM_GUIDE.md`
- `Assets/docs/Complete/CAMERA_MODE_ARCHITECTURE_DESIGN.md`
- `Assets/docs/Complete/TIME_HITSTOP_GUIDE.md`

---

## 구현 현황 (2026-06-19)

설계 문서의 1~5단계 기반 구현을 완료했다.

| 단계 | 상태 | 주요 구현 |
|------|------|-----------|
| 1단계 | 완료 | `UltimateSequenceAsset`, `UltimateSequencePlayer`, `PlayerCombat` 입력 연결, MotionSet/카메라 재생 및 종료 복구 |
| 2단계 | 완료 | 플레이어·카메라 입력, HUD, 적 AI, 무적, 타겟 피격 반응을 소유권 기반으로 잠그고 복구 |
| 3단계 | 완료 | 락온/수동/근거리/콘 타겟 선택, KCC 위치·회전 보정, 선택적 원위치 복구 |
| 4단계 | 완료 | VFX, SFX/Voice, 타임스케일, 카메라 효과/흔들림, 데미지 윈도우, 커스텀 콜백 이벤트 |
| 5단계 | 완료 | 궁극기 시퀀스 전용 에디터, 검증, 타임라인 미리보기, PlayMode 테스트, MotionSet/카메라 촬영 도구 연동 |

### 주요 구현 경로

- `Assets/02.Scripts/Data/Combat/Ultimate/`
- `Assets/02.Scripts/GameActor/Component/Player/UltimateSequencePlayer.cs`
- `Assets/02.Scripts/GameActor/Component/Player/UltimateGameplayLockContext.cs`
- `Assets/02.Scripts/GameActor/Component/Player/UltimateTargetResolver.cs`
- `Assets/02.Scripts/GameActor/Component/Player/UltimatePlacementContext.cs`
- `Assets/02.Scripts/Data/Actor/Animation/Editor/MotionSetWindow.CaptureBridge.cs`

### 에디터 진입점

- `UPlayGround/캐릭터/궁극기/궁극기 시퀀스 에디터`
- MotionSet Editor의 `촬영 연동` 탭

캐릭터별 에셋을 생성한 뒤 해당 플레이어의 `PlayerCombat._ultimateSequences` 목록에 연결하면 궁극기 입력에서 실행된다. 연결된 에셋이 없으면 기존 Ultimate 스킬 입력 경로를 유지한다.
