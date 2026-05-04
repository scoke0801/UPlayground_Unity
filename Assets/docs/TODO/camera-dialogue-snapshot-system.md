# 대화 카메라 스냅샷 시스템 설계 문서

## 1. 시스템 개요

### 1.1 배경 및 목표

현재 `DialogueCameraMode`는 화자(`PrimaryTarget`)와 청자(`SecondaryTarget`)의 트랜스폼만 받아 카메라 위치·회전·FOV를 **자동 산출**한다. 이는 "두 캐릭터가 마주보는 일반 대화"에는 충분하지만 다음 연출 요구를 충족하지 못한다.

- 인물 클로즈업 표정 컷
- 부감/앙각 등 의도적 앵글
- 환경(랜드마크, 소품)을 함께 잡는 와이드 샷
- "같은 화자가 말하는 동안 카메라만 이동"하는 연출

스냅샷 시스템은 **노드별로 카메라 포즈를 직접 지정**할 수 있게 하고, 지정 없는 노드는 기존 자동 카메라로 폴백하는 하이브리드 방식을 채택한다.

### 1.2 자동 카메라와의 공존 전략

`DialogueCameraMode` 단일 모드를 유지하되, 내부에 **두 가지 평가 경로**를 둔다.

| 경로 | 트리거 | 카메라 결정 방식 |
|---|---|---|
| Auto Follow | 노드에 스냅샷 없음 | 기존 `EvaluatePose` 로직 (speaker/listener 기반) |
| Snapshot Hold | 노드에 스냅샷 지정됨 | 스냅샷 정의 절대/상대 좌표 기준으로 보간 후 고정 |

스냅샷 있는 노드 → 없는 노드 전환 시에도 **현재 포즈 → 자동 추종 포즈**로 블렌드되며, 역방향도 동일하게 동작한다.

### 1.3 핵심 개념

- **Snapshot**: 카메라 위치·오일러 회전·FOV·좌표 공간을 가진 불변 데이터 단위
- **Blend Context**: "이전 포즈 → 새 스냅샷"을 보간하기 위한 런타임 상태 (시작 포즈, 경과 시간, 지속 시간, 커브)
- **Snapshot Space**: 스냅샷이 어떤 좌표 기준에서 정의되어 있는지 (월드/화자 로컬/청자 로컬/대화 중심)

---

## 2. 데이터 모델 설계

### 2.1 `CameraSnapshotSpace` enum

```csharp
public enum CameraSnapshotSpace
{
    WorldSpace,          // 절대 좌표 (고정 장소에서만 동작)
    SpeakerRelative,     // 화자 트랜스폼 기준 로컬 좌표
    ListenerRelative,    // 청자 트랜스폼 기준 로컬 좌표
    ConversationCenter   // (화자+청자)/2 위치, 화자→청자 방향 기준
}
```

| Space | 원점 / 축 | 적합 케이스 |
|---|---|---|
| `WorldSpace` | 월드 (0,0,0), 월드 축 | 마을 광장·보스방 같은 **고정 장소** 시네마틱 |
| `SpeakerRelative` | 화자.position / 화자.rotation | 화자 클로즈업, 어깨 너머 컷 — 장소 무관 재사용 |
| `ListenerRelative` | 청자.position / 청자.rotation | 플레이어 시점·어깨 너머 컷 |
| `ConversationCenter` | (화자+청자) 중점, 화자→청자 forward | 두 인물을 모두 잡는 two-shot·부감 |

**기본값: `ConversationCenter`** — 새 스냅샷 생성 시 가장 안전하게 두 인물을 잡을 수 있음.

### 2.2 `DialogueCameraSnapshotData` (Serializable 구조체)

```csharp
[System.Serializable]
public struct DialogueCameraSnapshotData
{
    [Header("좌표 기준")]
    public CameraSnapshotSpace space;

    [Header("포즈")]
    public Vector3 position;          // space에 따라 해석
    public Vector3 eulerAngles;       // 카메라 로컬 회전
    [Range(10f, 90f)] public float fieldOfView;

    [Header("블렌드")]
    [Min(0f)] public float blendDuration;    // 0이면 즉시 컷
    public AnimationCurve blendCurve;        // null이면 EaseInOut 기본

    [Header("선택")]
    public bool useCollisionAvoidance;       // 스냅샷 포즈에 충돌 보정 적용 여부
    [Range(0f, 1f)] public float lookAtSpeakerWeight;
    // 0=완전 고정 회전, 1=화자를 항상 바라봄

    public bool IsValid => fieldOfView > 0f;
}
```

### 2.3 `DialogueCameraSnapshotSO` (독립 에셋)

여러 노드가 같은 스냅샷을 공유하거나 시네마틱과 재사용할 때 쓴다.

```csharp
[CreateAssetMenu(fileName = "DCS_", menuName = "UPlayGround/Dialogue/Camera Snapshot")]
public class DialogueCameraSnapshotSO : ScriptableObject
{
    public DialogueCameraSnapshotData data;
    [TextArea] public string note; // "성주 클로즈업", "부감 와이드" 등 작업 메모
}
```

### 2.4 `DialogueNodeSO` 추가 필드 (최소 침습)

```csharp
[Header("Camera (Optional)")]
public bool useCameraSnapshot;
public DialogueCameraSnapshotSO cameraSnapshotAsset;     // SO 우선
public DialogueCameraSnapshotData cameraSnapshotInline;  // Asset이 null일 때 인라인

public bool TryGetSnapshot(out DialogueCameraSnapshotData data)
{
    if (!useCameraSnapshot) { data = default; return false; }
    if (cameraSnapshotAsset != null) { data = cameraSnapshotAsset.data; return true; }
    data = cameraSnapshotInline;
    return data.IsValid;
}
```

### 2.5 `DialogueActionSO` 경유 방식 비교

| 항목 | 노드 필드 직접 | DialogueActionSO 경유 |
|---|---|---|
| 발견 가능성 | 인스펙터에서 즉시 보임 | 액션 리스트를 펼쳐야 보임 |
| 노드 직렬화 비용 | 필드 3개 추가 | 노드 변경 없음 |
| 다중 카메라 시퀀스 | 1노드 1스냅샷 | 1노드 N액션 가능 |
| 에디터 통합 | 단순 | Action 리스트 검색 필요 |
| **권장** | **기본** | **고급/특수 케이스** |

**최종 권장**: 기본은 노드 필드. 시네마틱급 다단 연출은 `SetCameraSnapshotAction : DialogueActionSO`로 보조.

---

## 3. 런타임 블렌딩 아키텍처

### 3.1 클래스 구조

```
DialogueCameraMode
├── _speaker, _listener              (기존)
├── EvaluateAutoFollowPose()         (기존 로직 추출)
├── _snapshotPlayer : SnapshotPlayer (신규)
└── _blendState     : BlendState     (신규)

SnapshotPlayer
├── HasActive : bool
├── ActiveSnapshot : DialogueCameraSnapshotData
├── Apply(snapshot)
├── Clear()
└── ResolveWorldPose(speaker, listener) -> CameraRigPose

BlendState
├── Mode : BlendMode { None, Blending, Hold }
├── FromPose : CameraRigPose
├── ElapsedSec, DurationSec
├── Curve : AnimationCurve
└── Sample(deltaTime) -> t (0~1)
```

### 3.2 블렌드 상태 머신

```
        Dialogue 진입
              │
              ▼
    ┌─────────────────┐
    │   AutoFollow    │  스냅샷 없음 — 기존 동작
    └──────┬──────────┘
           │ 노드 진입 + Snapshot 지정
           ▼
    ┌─────────────────┐
    │    Blending     │  이전 포즈 → 스냅샷 포즈 보간
    └──────┬──────────┘
           │ Elapsed >= Duration
           ▼
    ┌─────────────────┐
    │  HoldSnapshot   │  스냅샷 유지 (lookAtSpeakerWeight 매 프레임 갱신)
    └──────┬──────────┘
           │ 다음 노드 진입
           ├── 새 스냅샷    → Blending (현재 hold 포즈를 FromPose로 캡처)
           ├── 스냅샷 없음  → Blending (ToPose = AutoFollow 산출 포즈)
           └── 종료(OnExit) → PopMode, InGame 카메라 자체 복귀
```

### 3.3 좌표 공간 → 월드 포즈 변환

```csharp
private CameraRigPose ResolveSnapshotWorldPose(
    in DialogueCameraSnapshotData snap, Transform speaker, Transform listener)
{
    Vector3 origin; Quaternion basis;

    switch (snap.space)
    {
        case CameraSnapshotSpace.WorldSpace:
            origin = Vector3.zero; basis = Quaternion.identity; break;

        case CameraSnapshotSpace.SpeakerRelative:
            origin = speaker.position;
            basis  = Quaternion.Euler(0f, speaker.eulerAngles.y, 0f); break;

        case CameraSnapshotSpace.ListenerRelative:
            var lr = listener != null ? listener : speaker;
            origin = lr.position;
            basis  = Quaternion.Euler(0f, lr.eulerAngles.y, 0f); break;

        case CameraSnapshotSpace.ConversationCenter:
            Vector3 mid = listener != null
                ? (speaker.position + listener.position) * 0.5f : speaker.position;
            Vector3 fwd = listener != null
                ? (listener.position - speaker.position).WithY(0f).normalized
                : speaker.forward.WithY(0f).normalized;
            origin = mid; basis = Quaternion.LookRotation(fwd, Vector3.up); break;
    }

    Vector3    worldPos = origin + basis * snap.position;
    Quaternion baseRot  = basis * Quaternion.Euler(snap.eulerAngles);

    Quaternion finalRot = baseRot;
    if (snap.lookAtSpeakerWeight > 0f && speaker != null)
    {
        var look = Quaternion.LookRotation(
            (speaker.position + Vector3.up * 1.4f) - worldPos, Vector3.up);
        finalRot = Quaternion.Slerp(baseRot, look, snap.lookAtSpeakerWeight);
    }

    return new CameraRigPose
    {
        PivotPosition  = speaker != null ? speaker.position : worldPos,
        CameraPosition = worldPos,
        CameraRotation = finalRot,
        FieldOfView    = snap.fieldOfView,
        Yaw     = finalRot.eulerAngles.y,
        Pitch   = finalRot.eulerAngles.x,
        Distance = speaker != null ? Vector3.Distance(speaker.position, worldPos) : 0f
    };
}
```

### 3.4 `EvaluatePose` 수정 의사코드

```csharp
public CameraRigPose EvaluatePose(CameraRuntimeContext ctx, float dt, CameraEffectState fx)
{
    CameraRigPose goalPose = _snapshotPlayer.HasActive
        ? ResolveSnapshotWorldPose(_snapshotPlayer.ActiveSnapshot, _speaker, _listener)
        : EvaluateAutoFollowPose(ctx, dt);

    if (ShouldApplyCollision(ctx)) goalPose = ApplyCollision(ctx, goalPose);

    CameraRigPose outPose;
    switch (_blendState.Mode)
    {
        case BlendMode.None:     outPose = goalPose; break;
        case BlendMode.Hold:     outPose = goalPose; break;
        case BlendMode.Blending:
            _blendState.ElapsedSec += dt;
            float t = Mathf.Clamp01(_blendState.ElapsedSec / _blendState.DurationSec);
            float k = _blendState.Curve.Evaluate(t);
            outPose = LerpPose(_blendState.FromPose, goalPose, k);
            if (t >= 1f)
                _blendState.Mode = _snapshotPlayer.HasActive ? BlendMode.Hold : BlendMode.None;
            break;
    }

    outPose.CameraPosition += fx.positionDelta;
    outPose.FieldOfView    += fx.fovDelta;
    _lastOutputPose = outPose;
    return outPose;
}
```

### 3.5 이전 포즈 캡처 타이밍

블렌드는 **새 노드 진입 시 스냅샷이 바뀌는 순간** 시작된다. `FromPose` 결정 우선순위:

1. `_lastOutputPose` 런타임 캐시 — 가장 부드러움
2. `MainCamera.transform` + `fov` 현재값
3. 둘 다 없으면 새 스냅샷의 `goalPose` (= 즉시 컷)

```csharp
public void ApplySnapshot(in DialogueCameraSnapshotData snap)
{
    _blendState.FromPose    = _lastOutputPose;
    _blendState.DurationSec = snap.blendDuration;
    _blendState.Curve       = snap.blendCurve ?? DefaultEaseInOut;
    _blendState.ElapsedSec  = 0f;
    _blendState.Mode        = snap.blendDuration <= 0f ? BlendMode.Hold : BlendMode.Blending;
    _snapshotPlayer.Apply(snap);
}
```

---

## 4. 스냅샷 적용 흐름

```
DialogueRunner.EnterNode(node)
        │
        ▼
DialogueManager.UpdateDialogueCamera(channel, node)
        │
        ├── node.TryGetSnapshot(out snap) == false
        │       → PushDialogueCamera(speaker, listener)     // 기존 자동
        │
        └── node.TryGetSnapshot(out snap) == true
                → PushDialogueCamera(speaker, listener, snap) // 스냅샷 오버로드

CameraManager.PushDialogueCamera(speaker, listener, snap)
        │
        ▼
CameraModeController.PushMode(Dialogue, new CameraModeEnterParams
    { PrimaryTarget=speaker, SecondaryTarget=listener, Snapshot=snap })
        │
        ▼
DialogueCameraMode.OnEnter(ctx, params)
        ├── 기존 speaker/listener/offset 세팅
        └── params.Snapshot.HasValue
              ? ApplySnapshot(params.Snapshot.Value)
              : ClearSnapshot()

대화 종료(NotifyDialogueEnd)
        │
        ▼
CameraManager.PopCameraMode()
        │
        ▼
DialogueCameraMode.OnExit()
        ├── _snapshotPlayer.Clear()
        ├── _blendState.Mode = None
        └── InGame 모드가 자체 복귀 블렌드 처리
```

---

## 5. 에디터 설계

### 5.1 `DialogueCameraPreviewWindow` 레이아웃

```
┌─ DialogueCameraPreviewWindow ─────────────────────────────────────┐
│ [Graph : DialogueGraphSO ▼]                                        │
│ [Speaker : Transform ▼]            [Listener : Transform ▼]       │
│ ─────────────────────────────────────────────────────────────────  │
│  Nodes                          │ Selected Node Snapshot           │
│  ┌──────────────────────────┐   │ ┌──────────────────────────────┐ │
│  │ ● node_001  [snap ✔]     │   │  Space   : ConversationCenter ▼│ │
│  │   node_002  [auto    ]   │   │  Pos     : (x, y, z)           │ │
│  │ ● node_003  [snap ✔]     │   │  Euler   : (x, y, z)           │ │
│  └──────────────────────────┘   │  FOV     : 45                  │ │
│                                 │  Blend   : 0.6   curve [edit]  │ │
│                                 │  LookAt  : 0.3                 │ │
│                                 │  [Capture from SceneView Cam]  │ │
│                                 │  [Apply to Node]               │ │
│                                 │  [Save as SnapshotSO Asset]    │ │
│                                 └────────────────────────────────┘ │
│ ─────────────────────────────────────────────────────────────────  │
│  [▶ Play Sequence]  Speed [1.0x]  Loop [ ]                         │
│  Status: Playing node_002  (1.4s / 2.0s)                          │
└───────────────────────────────────────────────────────────────────┘
```

#### "Capture from SceneView Camera" 핵심 로직

```csharp
private void CaptureFromSceneView()
{
    var sv = SceneView.lastActiveSceneView;
    if (sv == null) return;
    Camera cam = sv.camera;

    (Vector3 origin, Quaternion basis) = ResolveSpaceBasis(
        _editingSnapshot.space, _speakerDummy, _listenerDummy);

    Quaternion invBasis          = Quaternion.Inverse(basis);
    _editingSnapshot.position    = invBasis * (cam.transform.position - origin);
    _editingSnapshot.eulerAngles = (invBasis * cam.transform.rotation).eulerAngles;
    _editingSnapshot.fieldOfView = cam.fieldOfView;
    Repaint();
}
```

### 5.2 전용 미리보기 씬

경로: `Assets/01.Scenes/Editor/DialogueCameraPreview.unity` (빌드 자동 제외)

| 오브젝트 | 역할 |
|---|---|
| `Stage_Floor` | 1m 그리드 바닥 (스케일 감각) |
| `Dummy_Speaker` | 표준 신장(1.7m) T-pose 캡슐/스켈레톤 |
| `Dummy_Listener` | 동일, 다른 위치 |
| `Lighting_Default` | 중성 라이팅 + 스카이박스 |
| `PreviewCamera` | 미리보기 전용 (Main Camera 비활성) |
| `MarkerProps` | 1m/2m/3m 거리 마커 큐브 |

**워크플로**:
1. `UPlayGround/Dialogue/Open Camera Preview` → `DialogueCameraPreview.unity` Additive 로드
2. `DialogueCameraPreviewWindow`가 더미 트랜스폼 자동 바인딩
3. 닫기: `Close Camera Preview` → Additive 씬 언로드

### 5.3 SceneView 기즈모

```csharp
[CustomEditor(typeof(DialogueNodeSO))]
public class DialogueNodeSOEditor : Editor
{
    private void OnSceneGUI()
    {
        var node = (DialogueNodeSO)target;
        if (!node.TryGetSnapshot(out var snap)) return;

        Transform speaker  = DialogueCameraPreviewSettings.Instance.SpeakerDummy;
        Transform listener = DialogueCameraPreviewSettings.Instance.ListenerDummy;
        var pose = DialogueCameraMode.ResolveSnapshotWorldPose(snap, speaker, listener);

        DrawCameraFrustum(pose, snap.fieldOfView, color: Color.yellow);
    }
}
```

- 선택 노드 → **노란색** 프러스텀 + nodeId 라벨
- 비선택 스냅샷 → 회색 프러스텀

---

## 6. CameraManager 연동

### 6.1 `CameraModeEnterParams` 확장

```csharp
public class CameraModeEnterParams
{
    // 기존 필드...
    public DialogueCameraSnapshotData? Snapshot; // 신규 — null이면 AutoFollow
}
```

### 6.2 `PushDialogueCamera` 오버로드

```csharp
// 기존 (변경 없음)
public bool PushDialogueCamera(Transform speaker, Transform listener = null, Vector3 offset = default);

// 신규 오버로드
public bool PushDialogueCamera(Transform speaker, Transform listener,
                               in DialogueCameraSnapshotData snapshot)
{
    if (_modeController?.CurrentMode is DialogueCameraMode cur
        && cur.IsSameSpeaker(speaker, listener)
        && cur.IsSameSnapshot(snapshot))
        return true;

    return PushCameraMode(CameraModeType.Dialogue, new CameraModeEnterParams
    {
        PrimaryTarget   = speaker,
        SecondaryTarget = listener,
        Snapshot        = snapshot
    });
}
```

---

## 7. 구현 로드맵

| Phase | 내용 | 예상 |
|---|---|---|
| **1** | `CameraSnapshotSpace` / `DialogueCameraSnapshotData` / `DialogueCameraSnapshotSO` / `DialogueNodeSO` 필드 추가 | 1일 |
| **2** | `DialogueCameraMode` 블렌딩 로직 (`BlendState`, `SnapshotPlayer`, `EvaluatePose` 분기) | 2~3일 |
| **3** | `CameraModeEnterParams` 확장 / `CameraManager` 오버로드 / `DialogueManager` 분기 | 1일 |
| **4** | `DialogueCameraPreviewWindow` + `DialogueNodeSOEditor` 기즈모 | 2~3일 |
| **5** | `DialogueCameraPreview.unity` 씬 + 시퀀스 재생 기능 | 1~2일 |
| **합계** | | **7~10일** |

---

## 8. 미결 사항 및 트레이드오프

### 8.1 최종 권장: 노드 필드 직접 + 액션은 보조

- 대부분의 노드는 "한 라인 = 한 카메라"이므로 필드 1세트로 충분
- 시네마틱급 다단 연출은 `SetCameraSnapshotAction` + `WaitAction` 조합으로 확장
- 필드는 "정적 한 컷", 액션은 "동적 컷 시퀀스"로 의미 분리

### 8.2 결정 보류 사항

| 항목 | 1차 기본값 | 보류 이유 |
|---|---|---|
| 블렌드 중 상대좌표 갱신 빈도 | 매 프레임 갱신 | 화자 이동 시 살아있는 카메라 vs 고정 컷 취향 차이 |
| `useCollisionAvoidance` 기본값 | `false` (opt-in) | 시네마틱 의도로 벽을 뚫는 카메라 허용 필요 가능 |
| portrait와 스냅샷 일치 강제 | 경고만 | 클로즈업 앵글 ≠ portrait 인물인 경우 허용 여부 |

### 8.3 향후 확장

1. **Cinematic Mode 통합**: `ResolveSnapshotWorldPose`를 시네마틱 키프레임 평가기로 재사용
2. **카메라 셰이크 프리셋**: 스냅샷에 `CameraShakeData` 참조 추가
3. **Dolly 트랙**: `startSnap` + `endSnap`으로 노드 내내 카메라 이동
4. **멀티 스피커**: N명을 `extraTargets`로 받아 `MultiPersonCenter` space 추가
5. **보이스 클립 동기화**: 언어별 보이스 길이에 맞춰 `holdDuration` 자동 조정

---

## 참조 파일

- `Assets/02.Scripts/Camera/Modes/DialogueCameraMode.cs`
- `Assets/02.Scripts/Camera/Modes/CameraModeController.cs`
- `Assets/02.Scripts/Camera/Modes/CameraModeEnterParams.cs`
- `Assets/02.Scripts/Camera/Modes/CameraRigPose.cs`
- `Assets/02.Scripts/Camera/Modes/CameraRuntimeContext.cs`
- `Assets/02.Scripts/Manager/CameraManager.cs`
- `Assets/02.Scripts/Manager/Dialogue/DialogueManager.cs`
- `Assets/02.Scripts/Data/Dialogue/DialogueNodeSO.cs`
- `Assets/02.Scripts/Data/Camera/DialogueCameraSettingsSO.cs`
