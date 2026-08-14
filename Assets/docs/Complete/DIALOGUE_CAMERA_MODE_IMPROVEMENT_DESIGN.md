# Dialogue Camera Mode 개선 설계 문서

## 개요

현재 `DialogueCameraMode`는 화자/청자 두 Transform을 받아 단일 OTS(Over-The-Shoulder) 포즈만 매 프레임 계산한다. 이 구조는 단순 NPC 1:1 대사에는 충분하지만, 다음과 같은 한계가 있다.

- 모든 라인이 동일 구도(Closeup/Medium/Wide 구분 없음)
- 화자 전환 시 180° 룰(가상선) 보장 없음 — 시선 매칭 깨질 가능성
- `DialogueNodeSO`에 카메라 디렉션 메타데이터 부재
- 다자 대화(3명+), 선택지 페이즈, Reaction Shot 미지원
- `UseCollision = true` 선언만 있고 실제 collision 호출 없음
- 블렌드 종류가 단일(`speakerCutBlendTime`) — Cut/Blend 분리 안 됨

본 문서는 [CAMERA_MODE_ARCHITECTURE_DESIGN.md](CAMERA_MODE_ARCHITECTURE_DESIGN.md)에서 정의한 모드 시스템 위에서 `DialogueCameraMode`를 시네마틱 다이얼로그 카메라로 격상시키기 위한 설계와 단계적 도입 로드맵을 제시한다.

---

## 웹 구현 사례 조사 요약

### Pixel Crushers Dialogue System for Unity

대화 시스템 상용 에셋. 시퀀서 명령 `Camera(Closeup, listener)@3.5` 형식으로 라인 단위 카메라 컷·전환을 데이터로 기술한다. Closeup/Medium/Full 같은 프리셋을 prefab의 자식 Transform 계층으로 정의해 디자이너가 시각 편집 가능.

참고 포인트:

- 카메라 앵글을 prefab 기반 데이터로 외부화 → 코드 수정 없이 연출 변경 가능
- `Camera(angle, subject, duration)` 시퀀서 DSL이 노드 단위 디렉션과 잘 맞는다
- 전용 Camera Angle Editor로 씬에서 프리뷰 제공

출처:

- [Pixel Crushers — Sequencer Camera (Cutscene Sequences)](https://www.pixelcrushers.com/dialogue_system/manual2x/html/cutscene_sequences.html)
- [Pixel Crushers — Default Camera Angle](https://www.pixelcrushers.com/dialogue_system/manual2x/html/default_camera_angle.html)
- [Pixel Crushers — Sequencer Command Reference](https://www.pixelcrushers.com/dialogue_system/manual2x/html/sequencer_command_reference.html)

### The Witcher 3 / Mass Effect 시네마틱 다이얼로그

Witcher 3는 음성 파일을 파싱해 컷 마커를 자동 생성하고, 180° 룰·establishing shot 같은 영화 문법을 알고리즘 규칙으로 강제한 뒤 애니메이터가 사후 보정하는 하이브리드 방식. Mass Effect는 텍스트 노드를 기준으로 같은 자동화를 적용했다.

참고 포인트:

- "자동 디렉터 + 디자이너 오버라이드" 2계층 구조가 대량 콘텐츠에 효율적
- 180° 룰, establishing wide, reaction shot은 데이터가 아니라 규칙으로 코드화 가능
- 근접 제스처 클로즈업이 한 줄 대사보다 강한 감정을 전달 → emotion 태그 기반 push-in이 큰 효과

출처:

- [Game Anim — Cinematic Dialogue In The Witcher 3](https://www.gameanim.com/2016/03/23/cinematic-dialogue-witcher-3/)
- [PC Gamer — Most of The Witcher 3's dialogue scenes were animated by an algorithm](https://www.pcgamer.com/most-of-the-witcher-3s-dialogue-scenes-was-animated-by-an-algorithm/)

### Cinemachine TargetGroup + Group Framing

여러 타겟의 그룹 중심을 LookAt으로 잡고 FOV/Distance를 자동 조정해 모든 타겟을 화면에 담는 컴포넌트. 투샷·establishing wide 구도에 적합.

참고 포인트:

- 두 화자를 `CinemachineTargetGroup`에 묶고 Group Framing Size로 화면 차지 비율 제어
- Adjustment Mode로 Zoom / Dolly / Both 선택 가능
- 자체 카메라 모드 안에서 동일 알고리즘을 직접 구현해도 무방(외부 의존 회피)

출처:

- [Cinemachine Group Framing component (3.1.6)](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/CinemachineGroupFraming.html)
- [Cinemachine Target Group](https://docs.unity3d.com/Packages/com.unity.cinemachine@2.3/manual/CinemachineTargetGroup.html)

### 영화 문법: 180° 룰 / Shot-Reverse-Shot / Headroom

두 인물의 가상선(eyeline axis)을 기준으로 카메라가 같은 쪽 반각에 머무는 규칙. 화자 전환 시 OTS_A → OTS_B 컷이 자연스러운 시선 매칭을 만든다. Headroom(상단 여백), Leadroom(시선 방향 여백)은 구도 품질의 기본.

출처:

- [StudioBinder — What is the 180 Degree Rule in Film](https://www.studiobinder.com/blog/what-is-the-180-degree-rule-film/)
- [Wolfcrow — How to use 180-degree rule with two/three+ characters](https://wolfcrow.com/how-to-use-180-degree-rule-with-one-character-two-characters-and-three-and-more-characters/)
- [Backstage — Shot/Reverse Shot: How to Film Conversations](https://www.backstage.com/magazine/article/what-is-shot-reverse-shot-film-examples-75550/)

### 접근성

급격한 동적 카메라 이동은 모션 민감 사용자에게 부담. 강도 슬라이더와 비활성 옵션이 권장된다.

출처:

- [Unity Learn — Camera system (Practical Game Accessibility)](https://learn.unity.com/tutorial/camera-system)

---

## 현재 구조 진단

### 책임 분포

```
DialogueCameraMode
├── OnEnter: speaker/listener Transform 캐시, offset/distance/fov 단일 값 세팅
├── EvaluatePose: lookAt + (forward 회전 × offset × distance) → desiredPosition
├── 블렌드: speakerCutBlendTime 단일 값으로 위치/회전 보간
└── 효과: CameraEffectState position/fov/yaw/pitch delta 합성

DialogueCameraSettingsSO
├── speakerLookAtOffset, listenerShoulderOffset
├── twoShotDistance, minDistance, maxDistance
├── speakerCutBlendTime
└── fieldOfView

DialogueManager.UpdateDialogueCamera
└── Main 채널 노드 진입 시 PushDialogueCamera(speaker, listener) 호출
```

### 한계

| 항목 | 현재 | 문제 |
|------|------|------|
| 샷 종류 | OTS 1종 | Closeup/Medium/Wide/TwoShot/Reaction 부재 |
| 디렉션 데이터 | 없음 | 노드별 다른 구도 지정 불가 |
| 180° 룰 | 보장 안 함 | 화자 전환 시 시선 매칭 깨질 수 있음 |
| 헤드룸/리드룸 | 단순 lookAt | 시네마틱 구도 품질 부족 |
| 충돌 보정 | 미연결 | 벽 카메라 관통 위험 |
| 블렌드 | 단일 값 | Cut/Blend/Establish 구분 불가 |
| 강조 연출 | 없음 | emotion 기반 push-in 등 미지원 |
| 다자 대화 | 미지원 | 3명+ 시 모호한 동작 |
| 선택지 페이즈 | 동일 구도 | TwoShot 등 가독성 구도 자동 전환 부재 |
| 접근성 | 옵션 없음 | 모션 민감 사용자 미배려 |

---

## 목표 아키텍처

```
DialogueCameraMode
├── DialogueCameraDirector            ← 자동 디렉터(180°룰, Shot-Reverse-Shot)
├── DialogueShotResolver              ← 노드 메타 → 프리셋 결정
├── DialogueShotComposer              ← 프리셋 + 화자/청자 → CameraRigPose
└── DialogueBlendController           ← Cut/Blend/Establish 종류별 보간

DialogueCameraSettingsSO (확장)
├── List<DialogueShotPresetSO> presets
├── BlendProfile (cutInstant, softBlend, establishBlend)
├── HeadroomProfile (headroom, leadroom)
└── DirectorProfile (180° 강제 여부, 짧은 라인 누적 임계)

DialogueShotPresetSO (신규)
├── ShotKey (string)
├── ShotType { OTS_Speaker, OTS_Listener, Closeup, TwoShot, Wide, Reaction }
├── shoulderOffset, distance(min/max), fieldOfView
├── headroomNormalized, leadroomNormalized
└── transitionType { Cut, Blend, Push }

DialogueNodeSO (확장)
├── shotPresetKey (optional)
├── reactionTargetSpeakerId (optional)
├── shotDuration (optional)
├── transitionOverride (optional)
└── emotionTag (optional, push-in 트리거)
```

### 핵심 원칙

| 원칙 | 설명 |
|------|------|
| 데이터 우선, 규칙 보조 | 노드에 `shotPresetKey`가 있으면 그대로, 없으면 Director가 규칙으로 결정 |
| 가상선은 대화 시작 시 1회 고정 | 화자 위치 변동에 흔들리지 않게 axis 캐시 |
| 컷과 블렌드는 분리 | 화자 전환은 Cut, 같은 화자 내는 Blend, 진입/종료는 Establish |
| 충돌은 모드 결과 위에 적용 | `context.Collision`을 EvaluatePose 말미에서 호출 |
| 접근성 강도는 외부 설정 | `SettingsManager`가 0~1 강도를 제공, 모드는 그에 맞춰 컷 빈도/연출 강도 조절 |

---

## 신규/변경 타입 요약

### DialogueShotType

```csharp
namespace UPlayGround.CameraSystem
{
    public enum DialogueShotType
    {
        OverShoulderSpeaker,   // 청자 어깨 너머 화자
        OverShoulderListener,  // 화자 어깨 너머 청자(Reaction)
        Closeup,               // 화자 단독 클로즈업
        TwoShot,               // 두 인물 모두 프레임
        Wide,                  // Establishing
        Reaction               // 임의 대상 단독
    }
}
```

### DialogueShotPresetSO

```csharp
[CreateAssetMenu(menuName = "UPlayGround/Camera/Dialogue Shot Preset")]
public class DialogueShotPresetSO : ScriptableObject
{
    public string shotKey;
    public DialogueShotType shotType;
    public Vector3 shoulderOffset;
    public float distance;
    public float minDistance;
    public float maxDistance;
    [Range(10f, 90f)] public float fieldOfView;
    [Range(0f, 0.3f)] public float headroomNormalized;
    [Range(0f, 0.3f)] public float leadroomNormalized;
    public DialogueTransitionType transitionType;
}

public enum DialogueTransitionType { Cut, Blend, Establish }
```

### DialogueCameraSettingsSO 확장

```csharp
public class DialogueCameraSettingsSO : ScriptableObject
{
    public List<DialogueShotPresetSO> presets;

    [Header("블렌드 프로필")]
    [Min(0f)]    public float cutInstantTime    = 0f;
    [Min(0.01f)] public float softBlendTime     = 0.30f;
    [Min(0.01f)] public float establishBlendTime = 0.60f;

    [Header("디렉터")]
    public bool   enforce180Rule        = true;
    [Min(0.1f)]   public float shortLineThreshold = 1.5f;
    [Min(1)]      public int   shortLineRecoveryCount = 3;

    public DialogueShotPresetSO Resolve(string key);
    public DialogueShotPresetSO ResolveDefault(DialogueShotType type);
}
```

### DialogueNodeSO 확장

```csharp
public class DialogueNodeSO : ScriptableObject
{
    // 기존 필드 ...

    [Header("카메라 디렉션 (선택)")]
    public string shotPresetKey;
    public string reactionTargetSpeakerId;
    [Min(0f)] public float shotDuration;
    public DialogueTransitionType transitionOverride = DialogueTransitionType.Blend;
    public string emotionTag;
}
```

### CameraModeEnterParams 확장 (또는 DialogueEnterContext 신설)

```csharp
public class DialogueEnterContext
{
    public Transform Speaker;
    public Transform Listener;
    public IReadOnlyList<Transform> AllParticipants;
    public string ShotPresetKey;
    public string ReactionTargetId;
    public DialogueTransitionType Transition;
    public string EmotionTag;
}
```

`CameraManager.PushDialogueCamera`는 이 컨텍스트를 받는 오버로드를 추가하고, 기존 시그니처는 내부에서 새 컨텍스트로 변환한다(하위 호환).

---

## DialogueCameraDirector 알고리즘

### 입력

- 대화 참여자 Transform 목록
- 직전 화자 / 현재 화자 ID
- 직전 샷 종류, 직전 라인 길이
- 노드 메타데이터(있으면 우선)
- `enforce180Rule`, `shortLineThreshold`

### 가상선

대화 시작 시 1회 계산해 캐시한다.

```
axis = (speakerB.position - speakerA.position) projected on XZ plane, normalized
sideOf(speaker) = sign of cross(axis, cameraToSpeaker)
```

화자가 이동해도 axis는 유지(고정선 원칙). 다자 대화는 "현재 활성 페어"의 axis를 사용하며, 페어 변경 시 establishing wide 1샷 후 새 axis로 갱신.

### 라인 단위 결정 흐름

```
1. node.shotPresetKey 있음 → 그 프리셋 사용 (override)
2. 같은 화자 연속 라인 → 직전 샷 유지, transition = Blend
3. 화자 전환 → reverse cut
   3-1. 직전이 OTS_Speaker 였다면 OTS_Listener (또는 reverse OTS) 로 컷
   3-2. enforce180Rule이면 axis 같은 쪽 반각 내에서 카메라 위치 선택
4. 짧은 라인이 shortLineRecoveryCount회 누적 → TwoShot 또는 Wide로 회복
5. 선택지 페이즈 진입 → TwoShot
6. emotionTag 있음 → 단발성 push-in 효과 부여(샷 종류는 유지)
```

### 출력

`DialogueShotPresetSO + DialogueTransitionType + Optional EmotionEffect` 묶음을 `DialogueShotComposer`에 전달.

---

## DialogueShotComposer 포즈 계산

기존 `EvaluatePose`의 책임을 흡수해 다음 단계로 정리한다.

```
1. 기준 lookAt 계산
   - Closeup/OTS:  speaker.head + speakerLookAtOffset
   - TwoShot/Wide: midpoint(speaker, listener) + groupLookAtOffset
   - Reaction:     reactionTarget.head + offset

2. baseForward 계산
   - axis 기반(shotType.isOTS면 speaker→listener, reverse면 반대)
   - axis 없으면 speaker.forward fallback

3. desiredPosition = lookAt + Quaternion.LookRotation(baseForward, up) * shoulderOffset.normalized * distance

4. Headroom/Leadroom 보정
   - Camera.WorldToViewportPoint(speaker.head)이 (0.5 + leadroom, 0.5 - headroom) 근처에 오도록 yaw/pitch 미세 조정
   - 1~2회 iteration 충분

5. 충돌 보정
   - context.Collision.Resolve(lookAt, desiredPosition, capsuleRadius) 호출
   - 모드의 UseCollision == true 일 때만

6. 블렌드 적용
   - DialogueBlendController가 transitionType에 따라
       Cut       → 즉시 스냅
       Blend     → softBlendTime
       Establish → establishBlendTime
   - exponential damping 사용

7. CameraRigPose 반환 + CameraEffectState 합성
```

### Push-in 효과

`emotionTag`가 매핑된 경우, `DialogueEmotionEffectTable`에서 FOV delta·duration·curve를 받아 `CameraEffectState.fovDelta`에 단발 추가. 이펙트는 공용 `CameraEffectManager`를 재사용하므로 모드 외부와 충돌하지 않는다.

---

## DialogueManager / Runner 통합

### 변경 포인트

- `UpdateDialogueCamera`가 `DialogueEnterContext`를 구성해서 전달
- 같은 대화 내 노드 간 push 누적 방지를 위해 `ReplaceDialogueCamera` API 추가 (첫 노드만 Push, 이후는 Replace)
- `NotifyDialogueEnd`에서 Pop 시 establish 블렌드 사용
- 다자 대화 페어 변경은 Runner가 인지 → Director에 신호

### 시퀀서 DSL (선택, 후순위)

Pixel Crushers처럼 `Camera(Closeup, target=Listener, t=2.0, blend=Cut)` 같은 시퀀서 명령을 지원하면 노드 메타로 표현 안 되는 정밀 연출이 가능. 대화 시스템이 이미 노드 기반이라면 메타 필드만으로 80% 충당, DSL은 후일 옵션.

---

## 접근성 / 옵션화

`SettingsManager`에 다이얼로그 카메라 강도 슬라이더 추가.

| 강도 | 동작 |
|------|------|
| 0.0 (정적) | 진입 시 TwoShot 1회 고정. 화자 전환·push-in 비활성 |
| 0.5 (절제) | OTS/TwoShot만 사용. push-in/shake 강도 절반 |
| 1.0 (시네마틱, 기본) | 전체 디렉터 동작 |

별도로 "카메라 흔들림 비활성" 토글이 모드 외부의 `CameraEffectManager`에 적용되므로 본 모드는 강도 값만 소비.

---

## 구현 현황 (2026-08-14)

Phase 1~4와 Phase 5의 일부(선택지 투샷·짧은 라인 컷 억제)를 구현했다. Unity Play Mode 체감 검증은 미완료.

| 항목 | 상태 | 산출 |
|------|------|------|
| 상대편(counterpart) 해석 | 완료 | 플레이어가 화자일 때 `listener`에 플레이어를 넘겨 구도가 퇴화하던 문제 해결. `DialogueManager._dialoguePartner` |
| 대화 세션 상태 분리 | 완료 | `DialogueShotSession` — 가상선·인트로 소진·직전 샷. Dialogue↔Replay 왕복에도 유지 |
| 가상선 / Shot-Reverse-Shot | 완료 | `DialogueShotComposer` — 측면 벡터를 세션 고정값으로만 사용 |
| Cut / Blend / Establish 분리 | 완료 | `DialogueShotDirector.ResolveBlendTime` — 기존에 선언만 되어 있던 `cutInstantTime`/`establishBlendTime` 연결 |
| 노드 디렉션 메타 | 완료 | `DialogueNodeSO.shotType / shotTransition / reactionSpeakerId / shotDistanceOverride` |
| 리액션 샷 / 선택지 투샷 / 짧은 라인 컷 억제 | 완료 | `DialogueShotDirector.DecideShot` |
| Headroom / Leadroom 보정 | 미착수 | Phase 3 잔여 |
| emotion push-in, 접근성 강도 옵션 | 미착수 | Phase 5 잔여 |
| 작가 도구(프리뷰 창, 시퀀서 DSL) | 미착수 | Phase 6 |

설계 이탈점 2건:

- **`DialogueShotPresetSO`를 신설하지 않았다.** 샷 프리셋은 `DialogueCameraSettingsSO.shotPresets` 리스트(`DialogueShotPreset`)가 소유한다. 설정 에셋 하나만 Addressables에 등록되어 있어 자산·주소 추가 없이 저작할 수 있고, 리스트가 비면 기존 구도 필드에서 기본 프리셋을 파생하므로 현행 에셋이 그대로 동작한다. 프리셋을 그래프별로 교체해야 할 요구가 생기면 그때 SO로 승격한다.
- **노드 식별자를 `shotPresetKey`(문자열)가 아니라 `DialogueShotType`(열거형)으로 두었다.** 프리셋이 샷 종류당 1개이므로 문자열 키가 줄 유연성이 없고, 오타 위험만 생긴다.

또한 진입 첫 샷은 기존 동작(즉시 컷)을 기본값으로 유지했다. `establishBlendOnEnter`를 켜면 `establishBlendTime`으로 붙는다.

---

## Phase 로드맵

각 Phase는 독립 PR 단위로 작업하고, 직전 Phase가 동작 회귀 없이 머지된 뒤 진입한다.

### Phase 1 — 안전성 보강 (1~2일)

목표: 현재 동작은 유지하되 명백한 버그/누락을 메운다.

- [ ] `DialogueCameraMode.EvaluatePose` 말미에 `context.Collision.Resolve` 호출 추가 (`UseCollision` 가드)
- [ ] `DialogueCameraSettingsSO`에 `cutInstantTime / softBlendTime / establishBlendTime` 3종 분리 추가 (기존 `speakerCutBlendTime`는 `softBlendTime`로 마이그레이션, deprecated 표시)
- [ ] `CameraManager.PushDialogueCamera` 호출 시 동일 화자 재진입은 `Replace` 경로로 처리(누적 push 방지)
- [ ] 단위 회귀 확인: 기존 1:1 대사 씬에서 동일 구도 재현

산출: `DialogueCameraMode.cs`, `DialogueCameraSettingsSO.cs`, `CameraManager.cs`(API 추가)

리스크: 낮음. 데이터 마이그레이션은 기본값 유지로 호환.

### Phase 2 — 데이터 모델 도입 (2~4일)

목표: 샷 프리셋과 노드 디렉션 메타를 데이터로 도입. 동작은 Phase 3에서 활성화.

- [ ] `DialogueShotType`, `DialogueTransitionType` 열거형 추가
- [ ] `DialogueShotPresetSO` 신규
- [ ] `DialogueCameraSettingsSO`에 `presets` 리스트, `enforce180Rule`, `shortLineThreshold`, `shortLineRecoveryCount` 추가
- [ ] `DialogueNodeSO`에 `shotPresetKey` 외 5개 필드 추가 (모두 optional)
- [ ] `DialogueCameraSettingsEditorUtility`에 프리셋 일괄 생성 헬퍼 추가
- [ ] 기본 프리셋 5개(`OTS_Speaker`, `OTS_Listener`, `Closeup`, `TwoShot`, `Wide`) Addressables 등록

산출: 신규 SO 파일들, 기존 SO 확장, 에디터 유틸 확장.

리스크: 낮음. 노드 필드는 기본값이 비어 있으므로 기존 그래프는 영향 없음.

### Phase 3 — Composer / Resolver (4~6일)

목표: 데이터 기반으로 포즈를 계산하도록 모드 내부를 분해. 자동 디렉터는 미적용.

- [ ] `DialogueShotResolver` 신규 — 노드 메타 → 프리셋 결정 (메타 없으면 기본 OTS)
- [ ] `DialogueShotComposer` 신규 — 프리셋·참여자 → `CameraRigPose`
- [ ] Headroom/Leadroom 보정 로직 추가 (Composer 내부)
- [ ] `DialogueBlendController` 신규 — transition 종류별 블렌드 시간 선택
- [ ] `DialogueCameraMode`는 위 3개를 호출하는 얇은 컨테이너로 축소
- [ ] 노드에 `shotPresetKey` 명시한 테스트 씬에서 Closeup/TwoShot/Wide 동작 확인

산출: `DialogueShotResolver.cs`, `DialogueShotComposer.cs`, `DialogueBlendController.cs`, `DialogueCameraMode.cs` 리팩터.

리스크: 중간. 기존 OTS 동작은 기본 프리셋으로 재현 — 회귀 테스트 시나리오 필수.

### Phase 4 — Auto-Director (180° 룰 / Shot-Reverse-Shot) (5~7일)

목표: 메타 비어 있어도 자연스러운 컷이 만들어지도록 한다.

- [ ] `DialogueCameraDirector` 신규
  - [ ] 가상선 캐시
  - [ ] sideOf(speaker) 판정
  - [ ] 동일 화자 vs 화자 전환 분기
  - [ ] 짧은 라인 누적 → TwoShot/Wide 회복
  - [ ] 선택지 페이즈 진입 시 TwoShot 자동
- [ ] `DialogueRunner`가 라인 길이/선택지 노출을 Director에 전달할 채널 추가
- [ ] 다자 대화: 페어 변경 시 1회 Establishing Wide 삽입
- [ ] 회귀 테스트: 메타 없는 기존 그래프에서 OTS-only 대비 컷 변화 확인

산출: `DialogueCameraDirector.cs`, `DialogueRunner.cs` 시그널 확장, `DialogueCameraMode` 진입 흐름 변경.

리스크: 높음. 영화 문법을 자동화하는 부분이라 디자이너 피드백 필수. enforce180Rule 토글로 비활성 경로 보장.

### Phase 5 — 강조 연출 / 접근성 (3~4일)

목표: 감정 표현과 사용자 옵션.

- [ ] `DialogueEmotionEffectTable` SO — emotionTag → FOV delta/duration/curve
- [ ] `DialogueShotComposer`가 `CameraEffectState.fovDelta`에 push-in 적용
- [ ] `SettingsManager`에 다이얼로그 카메라 강도(0~1) 추가
- [ ] 강도가 0/0.5/1일 때 Director 동작 분기
- [ ] "카메라 흔들림 비활성" 토글이 본 모드 push-in에도 적용되도록 연결

산출: emotion 테이블, Composer 효과 라우팅, Settings/SettingsManager 확장.

리스크: 낮음. 효과 시스템은 이미 존재.

### Phase 6 — 에디터 / 작가 도구 (3~5일, 선택)

목표: 디자이너가 코드 변경 없이 연출 튜닝.

- [ ] `DialogueShotPreviewWindow` — 화자/청자 더미 두고 프리셋 결과 Scene 뷰에 시각화
- [ ] `DialogueNodeSO` Inspector에 프리셋 키 자동완성·미리보기 버튼
- [ ] 시퀀서 DSL (선택) — `Camera(...)` 명령 파서

산출: 에디터 어셈블리 확장.

리스크: 낮음. 런타임 영향 없음.

---

## 마이그레이션 전략

- 기존 `DialogueCameraSettings.asset`은 Phase 1에서 `softBlendTime` 자동 채움
- Phase 2에서 기본 프리셋 5종을 같은 Addressables 그룹에 추가, 기본 OTS 프리셋이 기존 `listenerShoulderOffset / twoShotDistance / fieldOfView`를 그대로 흡수
- 기존 `DialogueNodeSO` 그래프는 메타 필드를 비워 두면 Phase 4 Director가 자동 동작
- `CameraManager.PushDialogueCamera(speaker, listener, offset)` 시그니처는 유지, 내부에서 `DialogueEnterContext`로 변환

---

## 회귀 테스트 시나리오

1. **1:1 짧은 NPC 대사** — Phase 1 이후에도 동일 OTS 구도 유지
2. **선택지 분기** — Phase 4 이후 자동 TwoShot 진입, 선택 후 OTS 복귀
3. **다자 대화(3명)** — Phase 4에서 새 화자 등장 시 Establishing Wide 1샷
4. **벽 가까이 대화** — Phase 1 이후 카메라 관통 없음
5. **모션 민감 옵션 0.0** — Phase 5 이후 진입 시 TwoShot 고정, push-in 없음
6. **emotionTag = "shock"** — Phase 5 이후 단발 push-in 발생, 종료 후 원위치

---

## 참고 자료

- 프로젝트 내부: [CAMERA_MODE_ARCHITECTURE_DESIGN.md](CAMERA_MODE_ARCHITECTURE_DESIGN.md), [DIALOGUE_SYSTEM_GUIDE.md](DIALOGUE_SYSTEM_GUIDE.md)
- [Pixel Crushers — Sequencer Camera (Cutscene Sequences)](https://www.pixelcrushers.com/dialogue_system/manual2x/html/cutscene_sequences.html)
- [Pixel Crushers — Default Camera Angle](https://www.pixelcrushers.com/dialogue_system/manual2x/html/default_camera_angle.html)
- [Pixel Crushers — Sequencer Command Reference](https://www.pixelcrushers.com/dialogue_system/manual2x/html/sequencer_command_reference.html)
- [Game Anim — Cinematic Dialogue In The Witcher 3](https://www.gameanim.com/2016/03/23/cinematic-dialogue-witcher-3/)
- [PC Gamer — Most of The Witcher 3's dialogue scenes were animated by an algorithm](https://www.pcgamer.com/most-of-the-witcher-3s-dialogue-scenes-was-animated-by-an-algorithm/)
- [Cinemachine Group Framing component (3.1.6)](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/CinemachineGroupFraming.html)
- [Cinemachine Target Group](https://docs.unity3d.com/Packages/com.unity.cinemachine@2.3/manual/CinemachineTargetGroup.html)
- [Unity Learn — Camera system (Practical Game Accessibility)](https://learn.unity.com/tutorial/camera-system)
- [StudioBinder — What is the 180 Degree Rule in Film](https://www.studiobinder.com/blog/what-is-the-180-degree-rule-film/)
- [Wolfcrow — How to use 180-degree rule with two/three+ characters](https://wolfcrow.com/how-to-use-180-degree-rule-with-one-character-two-characters-and-three-and-more-characters/)
- [Backstage — Shot/Reverse Shot: How to Film Conversations](https://www.backstage.com/magazine/article/what-is-shot-reverse-shot-film-examples-75550/)
- [Wikipedia — Virtual camera system](https://en.wikipedia.org/wiki/Virtual_camera_system)
