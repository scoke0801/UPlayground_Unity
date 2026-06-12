# 대화 카메라 사전 녹화/재생 시스템 설계 문서

> 범위: **카메라만**. 액터(캐릭터) 모션 녹화는 범위 밖이며, 액터는 기존 대화/Animancer 클립으로 구동한다.
> 전제: **Unity Timeline 미사용**. 재생은 기존 `ICameraMode` 파이프라인에 얹는다.

---

## 0. 한 줄 요약

대화 장면용 카메라 움직임을 **개발 중(에디터 플레이 모드)에 직접 몰아서 녹화**해 가벼운 트랙 에셋으로 굽고, 런타임에는 **기존 카메라 모드 시스템으로 재생**한다. 새로 만드는 것은 "녹화 도구 + 얇은 재생 모드 + 트랙 에셋" 3개뿐이며, 포즈/좌표공간/충돌/이펙트 합성 plumbing은 기존 스냅샷 인프라를 재사용한다.

---

## 1. 기존 스냅샷 시스템과 무엇이 다른가 (핵심 질문)

결론부터: **재생 메커니즘은 같고, 저작(authoring) 방식이 다르다. 중복이 아니라 형제(sibling) 시스템이다.**

| 구분 | 기존 `CameraSnapshotProfile` / `CameraSnapshotSequenceMode` | 본 문서의 녹화/재생 |
|---|---|---|
| 저작 방식 | **소수의 키 포즈를 손으로 배치** (`CameraSnapshotShot` 몇 개) | **카메라를 직접 몰아 연속 궤적을 녹화** (~30Hz 다수 샘플) |
| 데이터 형태 | 샷 N개 (각 position/euler/FOV + duration + blendCurve + orbit) | 균일 샘플 배열 (position[]/rotation[]/fov[] + sampleRate) |
| 샷 사이 움직임 | 코드가 **절차적으로 보간/오빗** | 녹화된 움직임 **그대로 재생** (샘플 간 보간만) |
| 적합한 경우 | "포즈 A → 이징 → 포즈 B" 식의 정형 컷 | 손으로 직접 흘린 **프리폼 카메라 워크** (수동 키잉이 번거로운 곡선 이동) |
| 재생 경로 | `ICameraMode` → `CameraRigPose` | **동일** (`ICameraMode` → `CameraRigPose`) |

**판단 기준:** 대화 컷이 "정면 → 어깨너머 → 투샷" 같은 **몇 개 포즈의 이징**으로 충분하면 **기존 스냅샷 시스템이 이미 답이다.** 녹화 시스템은 "정해진 포즈 키잉으로는 안 나오는, 손맛 있는 연속 카메라 이동"이 필요할 때만 값을 한다. 도입 전 이 질문에 먼저 답할 것.

> 참고: 미구현 설계 문서 `Assets/docs/TODO/camera-dialogue-snapshot-system.md`(노드별 스냅샷 지정)와도 구별된다. 그 문서는 "대화 노드에 손으로 포즈를 지정", 본 문서는 "포즈를 녹화로 생성". 셋은 **저작 방식만 다른 같은 재생 파이프라인**의 변형이다.

---

## 2. 왜 AnimationClip(GameObjectRecorder) 직행이 아닌가

웹 표준 경로는 `GameObjectRecorder → AnimationClip → Animator/Animancer 재생`이다. 이 프로젝트에서 **카메라에는 부적합**하다. 이유:

- 카메라에 AnimationClip을 직접 물리면 트랜스폼이 **`CameraRigPose` 파이프라인을 우회**한다. 그러면 이 프로젝트 카메라 스택이 의존하는 다음을 전부 잃는다:
  - **충돌 회피**(`context.Collision.Evaluate`)
  - **셰이크/FOV 이펙트 델타 합성**(`CameraEffectState.positionDelta/yawDelta/pitchDelta/fovDelta`)
  - **모드 우선순위 + 완료 시 PopMode** 흐름
- `GameObjectRecorder`가 **에디터 플레이 모드 전용**인 점은 문제 아님 — 본 시스템은 "사전 녹화"라 저작이 에디터에서만 일어나면 충분하다. (다만 우리는 AnimationClip 대신 **자체 트랙 에셋**에 굽는다. 위 plumbing을 보존하기 위해.)

즉 녹화 자체는 표준 기법을 쓰되, **출력 포맷을 AnimationClip이 아니라 `CameraRigPose`로 환원 가능한 트랙 에셋**으로 둔다.

---

## 3. 좌표 공간 — 가장 중요한 설계 결정

대화 카메라 녹화는 **여러 NPC·여러 장소에서 재사용**되어야 의미가 있다. 월드 좌표로 녹화하면 **녹화한 그 장소에 용접**되어 재사용 불가.

→ **녹화·재생 모두 앵커 상대(anchor-relative) 좌표를 기본으로 한다.** 기존 자산을 그대로 재사용:

- 공간 enum: `CameraSnapshotSpace { World, ActorRelative }` (기존)
- 앵커 참조: `CameraSnapshotActorReference` + `CameraSnapshotActorReferenceResolver` (기존)
- 녹화 시점에 각 샘플을 `anchor.InverseTransformPoint(...)` / `Inverse(anchor.rotation) * camRot`로 로컬화 (기존 `CameraSnapshotShot.Capture`와 동일 수식).
- 재생 시 `anchor.TransformPoint(...)`로 월드 복원 (기존 `ResolveWorldPose`와 동일).

**앵커 선택지** (트랙 에셋에 기록):
| 앵커 | 의미 | 적합 |
|---|---|---|
| `ActivePlayer` / 화자 | 한 인물 기준 | 클로즈업·어깨너머 |
| Conversation Center (옵션) | (화자+청자)/2, 화자→청자 forward | 두 인물 함께 잡는 워크 |

> Conversation Center는 단일 Transform 앵커가 아니므로, 필요 시 녹화/재생 양쪽에서 **런타임 가상 앵커 Transform**을 만들어 같은 수식에 태운다(2단계 과제로 분리 가능).

---

## 4. 데이터 모델

### 4.1 트랙 에셋 `DialogueCameraRecordingSO`

샷 리스트(샷마다 curve/duration)는 **밀집 녹화에 부적합**하다. 균일 샘플 배열로 둔다.

```csharp
[CreateAssetMenu(fileName = "DCR_", menuName = "UPlayGround/SO/Camera/Dialogue Camera Recording")]
public class DialogueCameraRecordingSO : ScriptableObject
{
    public string recordingName;

    [Header("좌표 기준")]
    public CameraSnapshotSpace space = CameraSnapshotSpace.ActorRelative;
    public CameraSnapshotActorReference anchor = CameraSnapshotActorReference.ActivePlayer();

    [Header("샘플 (앵커 로컬 또는 월드)")]
    public float sampleRate = 30f;          // Hz
    public Vector3[]   positions;           // 길이 N
    public Vector3[]   eulerAngles;         // 길이 N (Quaternion 직렬화 회피용 euler)
    public float[]     fieldsOfView;        // 길이 N

    [Header("재생")]
    public bool  useUnscaledTime = true;
    public float playbackSpeed   = 1f;
    public bool  useCollision    = false;
    public bool  restorePreviousModeOnFinish = true;
    public bool  lockCameraInput = true;

    public float Duration => positions == null || sampleRate <= 0f
        ? 0f : (positions.Length - 1) / sampleRate;
}
```

설계 노트:
- **Quaternion 대신 euler 배열** 직렬화 — 기존 `CameraSnapshotShot`이 euler를 쓰는 것과 일관. 재생 시 `Quaternion.Euler` 복원.
- **FOV도 트랙으로** — 줌 연출 보존.
- 데이터 크기: 10초 × 30Hz × (12+12+4 byte) ≈ 8.4KB. 가볍다. 압축/키 데시메이션은 필요 시 후순위 과제.
- look-at 타깃 회전 오버라이드는 **녹화에 이미 회전이 들어있으므로 기본 불필요**. (정밀 추적이 필요하면 옵션으로 추가.)

### 4.2 왜 `CameraSnapshotProfile` 재사용이 아닌가
샷 300개에 각각 blendCurve/duration/orbit을 다는 것은 **데이터 형태가 틀렸다**(절차 보간용 구조에 밀집 샘플을 욱여넣는 꼴). 데이터는 분리하되 **재생 plumbing(포즈/공간/충돌/이펙트)은 공유**하는 것이 옳다.

---

## 5. 재생 — 얇은 새 모드

`ICameraMode`를 구현하는 `DialogueCameraReplayMode` 신규. 분량 대부분이 기존 패턴 복제라 얇다.

- `CameraModeType`에 `DialogueCameraReplay` 추가 (또는 `Cinematic` 재활용 검토 — enum에 이미 미사용 `Cinematic` 존재).
- `OnEnter`: 트랙·앵커 해석, 진입 시점 카메라 포즈를 `CameraRigPose.FromCamera(...)`로 캡처(첫 샘플로의 진입 블렌드용), 입력 잠금/락온 해제.
- `EvaluatePose(context, deltaTime, effectState)`:
  1. `t += dt * playbackSpeed` (unscaled 옵션)
  2. `frame = t * sampleRate`, 정수부 `i`/소수부 `f` → 샘플 `i`와 `i+1` 사이 `Lerp`(pos/fov)·`Slerp`(rot). (회전은 euler→Quaternion 후 Slerp.)
  3. `space == ActorRelative`면 `anchor.TransformPoint/rotation*`으로 월드 복원.
  4. `useCollision`면 `CameraSnapshotActorReferenceResolver` + `context.Collision.Evaluate`로 보정 (기존 `BuildPoseFromShot`와 동일 패턴).
  5. `effectState`(셰이크/FOV 델타) 합성.
  6. 끝(`t >= Duration`)에서 `ActiveEnterParams.OnComplete?.Invoke()` → `restorePreviousModeOnFinish`면 `context.PopCameraMode`.
  7. `CameraRigPose` 반환 (PivotPosition/CameraPosition/CameraRotation/Yaw/Pitch/Distance/FieldOfView).

→ `CameraSnapshotSequenceMode`의 `EvaluatePose`/`BuildPoseFromShot`/진입 블렌드 로직을 거의 그대로 줄여 쓴다.

**CameraManager 재생 API** (기존 `PushCameraSnapshotSequence`와 대칭):
```csharp
public bool PushDialogueCameraRecording(
    DialogueCameraRecordingSO recording,
    CameraSnapshotActorReference? anchorOverride = null,
    System.Action onComplete = null);
public bool StopDialogueCameraRecording(DialogueCameraRecordingSO recording = null);
public bool IsDialogueCameraRecordingActive(DialogueCameraRecordingSO recording = null);
```
모드 등록은 `InitializeCameraModes()`에 `_modeController.Register(new DialogueCameraReplayMode())` 한 줄.

---

## 6. 녹화 — 에디터 도구

`GameObjectRecorder` 대신, **카메라 리그 트랜스폼을 매 프레임 직접 샘플링**해 위 배열에 적재(앵커 상대로 즉시 로컬화). 더 단순하고 우리 포맷에 바로 맞다.

`CameraRecorderEditorWindow` (기존 `CameraSnapshotEditorWindow` 패턴 차용):
- 플레이 모드에서 **앵커 지정 → Start → (카메라를 InGame/Free 모드로 직접 조작하며 연기) → Stop**.
- `EditorApplication.update` 또는 코루틴에서 `1/sampleRate` 간격으로 현 카메라 포즈를 캡처(`Capture` 수식 재사용)해 누적.
- Stop 시 `DialogueCameraRecordingSO` 에셋 생성/덮어쓰기(`AssetDatabase.CreateAsset`).
- 보조: 미리보기 재생(스크럽), 앞/뒤 트림, (후순위) 키 데시메이션으로 샘플 수 절감.

> 카메라를 무엇으로 모느냐는 자유다. 기존 `FreeCameraMode`로 손으로 날리거나, 다른 임시 컨트롤러로 몰아도 된다. 녹화기는 **결과 트랜스폼만** 본다.

---

## 7. 통합(트리거) 지점

대화에서 이 녹화를 언제 트는가 — 기존 두 경로를 그대로 재사용:

- **대화 노드 훅:** `DialogueManager.UpdateDialogueCamera`가 현재 `PushDialogueCamera`를 호출하듯, 노드에 "녹화 카메라" 옵션이 있으면 `PushDialogueCameraRecording`으로 분기. (`DialogueNodeSO`에 선택 필드 추가는 최소 침습으로.)
- **트리거 시스템:** 기존 `PlayCameraSnapshotTriggerActionSO`와 대칭인 `PlayDialogueCameraRecordingTriggerActionSO` 추가.
- **모션 이벤트:** 필요 시 기존 `MotionEvent_CameraSnapshotSequence`와 대칭 이벤트 추가.

종료는 모드의 `OnComplete`/`Pop`이 처리하므로, 대화 흐름과는 "재생 시작"만 엮으면 된다.

---

## 8. 구현 단계 / 진행 상태

확정된 권장 사양: **신규 `DialogueCameraReplay` 모드**(`Cinematic` 미재활용), **단일 화자 앵커**(Conversation Center는 후순위), 진입 블렌드는 스냅샷 `entryBlendDuration` 패턴 차용, **v1 look-at 보정 없음**(녹화 회전이 권위).

- [x] **Stage 1 — 데이터+재생 코어** *(작성 완료, Unity 검증 대기)*
  - `DialogueCameraRecordingSO`(단일 `Sample` 구조체 배열), `DialogueCameraReplayMode`(Priority 60),
    `CameraModeType.DialogueCameraReplay`, `CameraModeEnterParams.DialogueRecording`,
    `CameraManager.Push/Stop/IsDialogueCameraRecording…` API + 모드 등록.
- [x] **Stage 2 — 녹화 도구** *(작성 완료, Unity 검증 대기)*
  - `DialogueCameraRecorder`(런타임 샘플러: `[DefaultExecutionOrder(20000)]` LateUpdate 후캡처 + 어큐뮬레이터 고정간격),
    `DialogueCameraRecorderWindow`(PlayMode 프리카메라 몰기→녹화→베이크→미리보기). 메뉴: `UPlayGround/월드/카메라/대화 카메라 녹화`.
- [x] **Stage 2.5 — 손떨림 스무딩** *(작성 완료, Unity 검증 대기)* — §10 참조.
  - `DialogueCameraTrackSmoother`(zero-phase Gaussian, 회전 quaternion 헤미스피어 정렬), SO에 `rawSamples`+`smoothingStrength`(비파괴), 윈도우에 강도 슬라이더+재생성.
- [x] **Stage 3 — 통합** *(작성 완료, Unity 검증 대기)* — §11 참조.
  - 대화 경로: `DialogueNodeSO.cameraRecording`(선택 필드) → `DialogueManager.UpdateDialogueCamera`가 화자 기준으로 replay 재생.
  - 독립 컷신 경로: `PlayDialogueCameraRecordingTriggerActionSO`(스냅샷 트리거 미러, 런타임 변경 없음).
  - 스택 누수 수정: `CameraManager.EnterDialogueLayerMode`(대화 계층 내부 전환은 SetMode로 교체).
- [ ] **Stage 4 (후순위)** — Conversation Center 가상 앵커, 키 데시메이션/압축, 트림·블렌드 UX. *구체 필요 발생 전까지 미착수.*

> ⚠️ 진단 서버 미연결로 **에디터 컴파일/런타임 동작은 미검증**. Unity에서 컴파일 통과 + 실측 녹화 재생/대화 통합 확인 필요.

---

## 9. 미해결/결정 필요

- **앵커 기본값**: 화자 단일 앵커로 시작할지, 처음부터 Conversation Center를 지원할지. (1단계는 단일 앵커 권장.)
- **모드 타입**: 신규 `DialogueCameraReplay` vs 기존 미사용 `Cinematic` 재활용.
- **진입/이탈 블렌드**: 첫 샘플로의 진입 블렌드 시간, 종료 시 InGame 복귀 블렌드 정책(스냅샷 시스템의 `entryBlendDuration` 패턴 차용 가능).
- **look-at 보정 옵션** 필요 여부(녹화 회전만으로 충분한지 실측 후 결정).
- **스무딩 강도 기본값/프리셋**: 현재 윈도우 기본 0.35. 라이브 프리뷰(슬라이더 드래그 즉시 반영) 필요 여부.

---

## 10. 손떨림(녹화 노이즈) 처리

사람이 마우스·키보드로 카메라를 몰면 고주파 손떨림이 그대로 베이크된다. 업계 일반 처리법은 두 갈래다.

### 10.1 두 가지 접근
| 접근 | 방식 | 장점 | 단점 |
|---|---|---|---|
| **(A) 캡처 타임 댐핑** | 사람이 모는 입력을 **댐핑된 가상 카메라**가 따라가게 함(Cinemachine position/aim damping, 또는 `SmoothDamp` 추종 리그). 떨림이 애초에 녹화에 안 들어감 | 게임엔진 표준 답, 실시간 | 한 번 녹화하면 강도 고정 — 사후 조정 불가 |
| **(B) 사후 필터링** | 녹화 후 트랙에 필터 적용 | **사후 재조정 가능**, 자유롭게 녹화 | 별도 패스 필요 |

필터 종류: 저역통과/EMA, **One Euro**(인과·실시간용, 속도적응), Gaussian/이동평균(오프라인), Savitzky-Golay(피크 보존), B-spline 적합.

### 10.2 채택: (B) 사후 zero-phase 필터 — 근거
- **전체 트랙을 이미 보유** → 비인과(centered/forward-backward) 필터를 쓸 수 있고 **위상 지연이 0**이다. One Euro 등 인과 필터는 미래를 못 보는 **실시간** 신호용이라 여기선 불필요하고 오히려 열등(저속에서 lag 발생).
- **사후 재조정 가능**이 핵심 사용성 — 결과를 보고 강도를 다시 맞추는 게 자연스럽다.
- (A) 캡처 타임 댐핑은 더 싸지만 강도 고정이라, **추후 보조 수단**으로만 고려(프리카메라 리그에 `SmoothDamp` 추종 추가).

### 10.3 구현 핵심 (Stage 2.5, 작성 완료)
- `DialogueCameraTrackSmoother.Smooth(raw, strength)` — **zero-phase Gaussian**(centered, 엔드포인트로 갈수록 창 대칭 축소).
- **비파괴**: SO가 `rawSamples`(원본)를 보존하고 `samples`는 항상 raw에서 재계산(`RebuildSmoothedSamples`). 재스무딩이 누적되지 않음. 구버전 에셋은 기존 `samples`를 raw로 1회 승격.
- **앵커 로컬 공간 그대로 필터링** → 앵커 이동과 손떨림이 분리됨(월드로 풀지 않음).
- **회전 함정 처리**: euler 성분 평균은 wrap/짐벌에서 깨짐 → quaternion 변환 + **헤미스피어 정렬**(dot<0 부호 반전) 후 가중 평균/정규화.
- **sample[0]/마지막 불변**: 진입 블렌드 타깃(sample[0]) 보존을 위해 양 끝은 창이 0으로 줄어 이동하지 않음.
- 윈도우: 강도 슬라이더(기본 0.35) + "스무딩 적용/재생성" 버튼 → 적용 후 미리보기 재생으로 확인, 강도 0으로 적용하면 원복.

### 10.4 범위 밖(분리)
**키 리덕션/압축은 별개 문제**(데이터 크기)다. 스무딩(떨림 제거)과 데시메이션(샘플 수 축소)을 섞으면 튜닝 축이 둘로 늘고 위험만 커진다 → Stage 4로 분리.

---

## 11. 대화/트리거 통합 (Stage 3, 작성 완료)

### 11.1 통합 지점은 왜 `UpdateDialogueCamera` 내부여야 하는가
`DialogueManager.UpdateDialogueCamera`는 **Main Talk/Choice 노드마다** 자동 추종 카메라(`PushDialogueCamera`)를 무조건 push한다. 따라서 트리거나 `eventActions`로 녹화를 시작해도 **바로 다음 노드의 push가 덮어쓴다** → 대화 중 재생 통합은 `UpdateDialogueCamera` 내부 분기가 유일한 깨끗한 지점. (덤으로 eventActions의 "NotifyNodeEnter보다 먼저 실행" 순서 문제도 회피된다.)

```
node.cameraRecording != null → PushDialogueCameraRecording(화자앵커, restore=false)
else                         → 기존 PushDialogueCamera(speaker, listener)
```

### 11.2 스택 누수 수정 (`EnterDialogueLayerMode`)
모드 컨트롤러는 **우선순위 게이팅 없는 단순 스택**이다. 오늘 스택이 안 자라는 건 `DialogueCameraMode` 인스턴스가 하나뿐이라 `PushMode`의 `CurrentMode==nextMode` 가드가 재-OnEnter로 처리하기 때문. Replay는 **다른 인스턴스**라 Dialogue↔Replay 전환마다 push가 쌓여(+2/회) 대화 종료의 1회 Pop으로 InGame까지 못 돌아온다(누수 버그).

해결: `CameraManager.EnterDialogueLayerMode` — 현재 모드가 **대화 계층(Dialogue/Replay)**이면 `SetMode`(교체, push 없음), 밖이면 `PushMode`(계층 진입). `PushDialogueCamera`·`PushDialogueCameraRecording` 모두 이 헬퍼 경유. 결과: 첫 노드만 1회 push(`[InGame]`), 대화 내 전환은 전부 교체, 종료 Pop이 InGame으로 깨끗이 복귀. (동일 인스턴스 경로는 동작 불변 — 수정은 cross-mode 케이스에만 영향.)

### 11.3 완료 동작 / 앵커
- **대화 중 재생은 `restorePreviousOnFinish=false`** 강제 → 완료 시 pop하지 않고 마지막 프레임 유지(다음 노드가 카메라 교체). true면 대화 도중 InGame으로 튕겨 거슬림. replay 모드는 이 값을 `enterParams.RestorePreviousOnExit`에서 읽는다.
- **앵커 = 현재 화자**: `UpdateDialogueCamera`가 화자 actorId로 `CameraSnapshotActorReference`를 만들어 override → 같은 녹화를 여러 화자에 재사용 가능.
- **독립 컷신 트리거**는 녹화 에셋의 `restorePreviousModeOnFinish`(기본 true)를 따라 완료 후 InGame 복귀.

### 11.4 장면 단위 연속 재생 (핵심 케이스)
대화 "장면"은 보통 여러 Talk 노드에 걸친다 → **한 녹화가 여러 줄에 걸쳐 연속 재생**되는 것이 주 케이스. 그래서 `PushDialogueCameraRecording`에 **same-recording 가드**(기존 `IsSameSpeaker` 패턴 동일): 현재 모드가 Replay + 같은 녹화 + 미완료면 재진입을 no-op → 처음부터 재시작하지 않고 이어서 재생. 완료 후엔 가드가 풀려 재진입 시 다시 처음부터.
- 연속 노드가 같은 녹화 = 한 번에 연속 재생, 완료 시 마지막 프레임 유지(`restore=false`와 합성).
- 노드가 **다른 녹화**/녹화 없음으로 바뀌면 그때 교체.

### 11.5 추후
- 완료 후 **자동 추종으로 복귀**(현재는 마지막 프레임 유지)는 complete→`SetMode(Dialogue)` 콜백으로 가능하나 후순위.
- 연속 재생 중 화자가 바뀌어도 앵커는 최초 화자 유지(연속 스윕엔 보통 적절). 화자별 재앵커가 필요하면 별도 처리.
