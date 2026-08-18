# 대화 카메라 다자 대화 대응 설계 문서

## 구현 상태

Stage 1~3 코드 구현 완료. 관련 asmdef(`UPlayGround.Data`, `UPlayGround.Camera`, `Assembly-CSharp`, `Assembly-CSharp-Editor`, `UPlayGround.Dialogue.Tests`) 컴파일 통과.

**EditMode 테스트 통과** (Unity 6000.3.21f1 batchmode):

- `UPlayGround.Dialogue.Tests` 31/31 통과. 신규 `DialogueShotSessionTests` 8개 + `DialogueShotDirectorAxisChangeTests` 5개 포함, 기존 18개 회귀 없음.
- EditMode 전체 538개 중 532 통과 / 6 실패. 실패 6건은 모두 Ability·AI·Cinematic·MotionSet 소속으로 본 작업이 건드리지 않은 영역이다(상세는 아래).

**Play Mode 육안 검증은 미완료.** 반평면 유지는 테스트로 확정됐으나 축 전환의 체감(Establish 길이, EstablishWide의 리듬 영향)은 실제 플레이로 확인해야 한다.

### 본 작업과 무관한 기존 EditMode 실패 6건

작업 시작 시점의 워킹 트리에 이미 다른 진행 중 변경(Cinematic, Recruitment, GameManager 등)이 있었고 실패는 그쪽에서 온다. 카메라·대화 모듈 실패는 0건이다.

| 테스트 | 내용 |
|--------|------|
| `MonsterAbilitySetIntegrationTests` 2건 | Dryad 3 + Training Dummy 1의 Motion Key 미해석. CLAUDE.md가 "콘텐츠 확정 전까지 예상된 상태"로 명시한 바로 그 4건 |
| `BlackboardKeyRegistryTests` 1건 | BT Blackboard 미등록 Key 로그의 `LogAssert.Expect` 누락 |
| `EnemyAbilitySelectionPolicyTests` 1건 | `EnemyCombat` 공격트리거 구독 복구 중 NRE |
| `CinematicStageCoreTests` 1건 | ActorPresentation 논리 가시성 보존 실패 |
| `MotionSetCoreTests` 1건 | MotionEvent 대상 해석 우선순위(Executor vs Provider) |

> CLAUDE.md는 Dryad/Training Dummy 4건을 "Error로 승격하지 않는다"고 규정하는데 현재 테스트는 Fail로 떨어진다. 규정과 테스트 동작이 어긋나 있으므로 별도 판단이 필요하다 — 본 작업 범위 밖이라 손대지 않았다.

### 구현 중 설계에서 달라진 점

- **가상선은 무방향 선으로 다룬다.** 축 회전량과 이탈 판정을 `UndirectedAngle`(0~90)로 측정한다. 세 인물이 일렬로 선 배치에서 pair가 바뀌면 축 벡터는 180° 뒤집히지만 *선*은 그대로이므로, 방향각을 쓰면 확립 전환이 오발동한다.
- **리버스 샷 가드를 추가했다.** `SetActivePair`에서 화자·청자가 자리만 바꾼 경우(`isReversedPair`)는 축 재계산 없이 0을 반환한다. 없으면 모든 shot-reverse-shot이 축 전환으로 오인된다.
- **`axisRecaptureAngle`의 Range를 179 → 90으로 좁혔다.** 무방향 측정에서 90을 넘는 값은 영원히 발동하지 않는 죽은 설정이 된다. 기존 에셋 값 75는 그대로 유효하다.
- **`_dialogueLastNonPlayerSpeaker` 필드를 신설했다.** 설계서는 `_dialoguePartner` 재할당 제거만 예고했으나, 재할당을 없애면 플레이어 발화 라인의 상대 폴백이 3인 대화에서 퇴화한다. 두 역할을 분리해 `_dialoguePartner`는 `partnerActorIdOverride` 매핑 앵커로, 신설 필드는 축 폴백으로 쓴다. 이 분리는 화자 승격이 override 매핑을 덮어쓰던 기존 버그도 함께 해소한다.

---

## 개요

현재 대화 카메라는 **"플레이어 + 현재 대화 상대 1명"** 페어 모델이다. 3인 이상 대화는 파트너를 갈아끼우는 방식으로만 동작하며, 그 과정에서 영화 문법상 가장 중요한 두 규칙이 깨진다.

- 그룹 전체를 관통하는 일관된 가상선이 없다 — 화자가 A↔B로 교대할 때마다 축과 카메라 쪽이 재정의된다.
- 축이 바뀌는 순간이 **컷**이다 — "선은 넘어도 되지만 컷으로 넘으면 안 된다"는 연속성 편집 원칙에 정면으로 어긋난다.

추가로 청자가 항상 플레이어로 강제되어, NPC A가 NPC B에게 말하는 라인을 카메라가 잡지 못한다.

본 문서는 [DIALOGUE_CAMERA_MODE_IMPROVEMENT_DESIGN.md](../Complete/DIALOGUE_CAMERA_MODE_IMPROVEMENT_DESIGN.md)로 구축된 Director/Composer/Session 구조를 유지한 채, 위 세 갭을 메우는 3단계 작업을 정의한다.

**범위 밖(별도 작업):** 참여자 전원을 한 프레임에 담는 `GroupShot` 프리셋, 참여자별 화면 가중치. 본 설계는 그 작업의 전제가 되는 세션 데이터 모델까지만 만든다.

---

## 조사 근거 요약

### 업계 사례

| 사례 | 3인 이상 처리 방식 |
|------|-------------------|
| Dragon Age II (BioWare) | `.stg` 스테이지 파일에 플레이어 1 + 팔로워 3 + NPC 슬롯을 사전 배치. 슬롯마다 default/close-up/wide 카메라를 저작. 복잡한 배치는 커스텀 스테이지 필요 |
| The Witcher 3 / Cyberpunk 2077 (CDPR) | Generator가 180° 룰·establishing shot을 지켜 카메라 초안을 자동 생성 → 디자이너가 메뉴에서 교체 |
| Mass Effect (BioWare) | 카메라·표정·바디랭귀지 절차적 생성. 약 70%는 손으로 보정, 30%만 완전 자동 |
| Baldur's Gate 3 (Larian) | 파티원이 대화에 합류/이탈하는 것을 전제로 한 적응형 카메라 |

우리 구조는 CDPR형(자동 디렉터 + 노드 오버라이드)이며, 이 선택 자체는 유지한다. 다만 **AAA도 자동에 100% 의존하지 않으므로**, 자동 규칙을 강화하는 만큼 저작 오버라이드가 그것을 항상 이길 수 있어야 한다.

### 영화 문법 (3인 이상)

N명이면 축은 하나가 아니라 `C(N,2)`개 존재하고, 180° 룰은 그 전부에 적용된다. 실무 규칙은 다음 세 가지다.

1. **현재 대화 중인 pair의 축 하나만 활성 축으로 잡고, 나머지 인물은 그 기하 안에 앉힌다.** 카메라가 A·B 쪽에 있으면 C도 같은 쪽에 있어야 한다.
2. **pair가 바뀌면 새 축을 쓰기 전에 명시적으로 확립한다.**
3. **선을 넘는 것 자체는 문제가 아니고, 컷으로 넘는 것이 문제다.** 연속 이동으로 가로지르면 관객이 관계 변화를 눈으로 따라간다.

### 연구 근거 — Toric Space (Lino & Christie, SIGGRAPH 2015)

- 2 타겟: 해집합이 2차원 매니폴드. 탐색 없이 대수적 해석 해.
- 3 타겟: 대수 해 존재(카메라 구성 2개).
- 4 타겟 이상: **over-constrained — 일반적으로 정확해가 없다.**
- 실용 해법: **"타겟 2개를 고정하고 나머지 전원의 화면상 위치 오차를 최소화한다."**

영화 문법과 수학이 같은 답을 준다: **pair를 축으로 고정하고 나머지는 근사한다.** 본 설계의 Stage 2는 이 원칙의 구현이다.

출처는 문서 말미 참고 자료 참조.

---

## 현재 구조 진단

### 데이터 흐름

```
DialogueNodeSO (speakerId, reactionSpeakerId, shotType, shotTransition)
  → DialogueManager.UpdateDialogueCamera
      · 화자가 비플레이어면 _dialoguePartner로 승격 → UpdateDialogueSessionPartner
      · listener = (화자==플레이어) ? _dialoguePartner : playerTransform   ← 강제
  → CameraManager.PushDialogueCamera(DialogueShotRequest)
  → DialogueCameraBehavior.OnEnter
      → DialogueShotDirector.Decide  (샷/전환/리액션/짧은라인)
      → DialogueShotComposer.Compose (축·측면 → 포즈)
         · session.AxisForward / AxisRight * SideSign 사용
```

### 갭 목록

| # | 위치 | 문제 |
|---|------|------|
| G1 | `DialogueManager.cs:437` | `listener`가 화자 기준으로만 결정되어 항상 플레이어 또는 현재 파트너. NPC↔NPC pair 표현 불가 |
| G2 | `DialogueShotSession.cs:17-20` | 세션이 `Player`/`Partner` 2개 Transform만 소유. 참여자 집합 개념 없음 |
| G3 | `DialogueShotSession.cs:78` | `SetPartner`가 `preserveSide:false` — 파트너 교체마다 SideSign을 카메라 위치에서 재추론. 세션 전체를 관통하는 카메라 반평면이 없다 |
| G4 | `DialogueShotDirector.cs:106-108` | 축이 바뀐 라인도 일반 규칙대로 `Cut`. "컷으로 선 넘기"에 해당 |
| G5 | `DialogueShotSession.cs:52,91` | `HasBothActors`/축 계산이 `Player != null`을 전제. 플레이어 불참 대화에서 축을 못 잡음 |
| G6 | `CameraManager.cs:920-921` | 주석은 "카메라 쪽은 유지"라고 하나 실제 동작은 재추론. 문서-코드 불일치 |

G6은 Stage 2에서 **동작을 주석 쪽(세션 고정)으로 맞추는 것**으로 해소한다. 조사 결과상 세션 고정이 문법적으로 옳으므로 주석을 고치는 방향이 아니다.

---

## 목표 아키텍처

```
DialogueShotSession (재설계)
├── Participants : List<Transform>        ← 세션 참여자 집합 (신규)
├── ActiveSubject / ActivePartner         ← 현재 활성 pair (Player/Partner 대체)
├── StageRight : Vector3                  ← 세션 고정 카메라 반평면 기준 (신규, 핵심)
├── AxisForward / AxisRight / SideSign    ← 활성 pair에서 파생, SideSign은 StageRight가 결정
├── LastAxisChangeAngle : float           ← 직전 축 전환 크기 (신규, Stage 3 입력)
└── Center                                ← 참여자 무게중심 (2인 중점에서 확장)

DialogueShotDirector
└── DecideTransition에 축 전환 확립 규칙 추가

DialogueCameraSettingsSO
├── axisEstablishAngle : float            ← 신규
└── axisChangeEstablishShot : enum        ← 신규

DialogueNodeSO
└── listenerSpeakerId : string            ← 신규 (optional)
```

핵심 발상은 **`SideSign`을 스칼라 상태가 아니라 세션 고정 기준 벡터 `StageRight`에서 매번 유도되는 값으로 바꾸는 것**이다. 축이 바뀌어도 카메라는 항상 세션이 처음 정한 반평면에 남는다 — 이것이 "카메라가 A·B 쪽에 있으면 C도 같은 쪽"의 코드적 구현이다.

---

## Stage 1 — 청자 지정 데이터화 (요청 3번)

가장 먼저 한다. Stage 2의 "활성 pair"에 무엇을 넣을지가 이 데이터에서 나오기 때문이다.

### 데이터 변경

`DialogueNodeSO`에 필드 추가:

```csharp
[Tooltip("비우면 자동(화자가 플레이어면 현재 대화 상대, 아니면 플레이어). " +
         "채우면 이 speakerId의 인물을 이 라인의 대화 상대로 삼는다. NPC끼리 주고받는 라인에 쓴다.")]
public string listenerSpeakerId;
```

`reactionSpeakerId` 바로 아래에 배치한다. 두 필드는 역할이 다르다 — `listenerSpeakerId`는 **가상선을 정의**하고, `reactionSpeakerId`는 **그 축 위에서 누구를 잡을지**를 정한다.

### 코드 변경

`DialogueManager.UpdateDialogueCamera` (`DialogueManager.cs:421-450`):

```csharp
Transform listener = ResolveListenerTransform(node, speaker, playerTransform);
```

```csharp
/// <summary>
/// 이 라인의 대화 상대를 해석한다.
/// 노드가 지정하면 그것을 쓰고, 없으면 화자 기준 자동 폴백(기존 동작).
/// 화자 자신을 가리키면 구도 계산이 퇴화하므로 무시한다.
/// </summary>
private Transform ResolveListenerTransform(
    DialogueNodeSO node, Transform speaker, Transform playerTransform)
{
    if (!string.IsNullOrEmpty(node.listenerSpeakerId))
    {
        Transform authored = ResolveSpeakerTransform(node.listenerSpeakerId);
        if (authored != null && authored != speaker)
            return authored;
    }

    return speaker == playerTransform ? _dialoguePartner : playerTransform;
}
```

### 파트너 승격 규칙 조정

현행 `DialogueManager.cs:425-432`는 "화자가 비플레이어면 파트너로 승격"한다. Stage 1 이후에는 **화자와 청자 중 비플레이어 쪽**을 파트너로 본다. 다만 이 로직은 Stage 2에서 `SetActivePair`로 통째로 대체되므로, Stage 1에서는 기존 승격 로직을 그대로 두고 `listener`만 분리한다. **Stage 1 단독으로도 컴파일·동작이 성립해야 한다.**

### 저작 도구 동기화

`DialogueNodeSO`를 편집하는 커스텀 인스펙터/그래프 에디터가 있으면 새 필드를 동일하게 노출한다. 데이터 필드만 추가하고 인스펙터를 빠뜨리면 저작이 불가능해진다.

### 호환성

기본값 `null` → 전원 기존 동작. 마이그레이션 불필요.

---

## Stage 2 — 참여자 집합 + 세션 고정 반평면 (요청 1번)

### 2-1. StageRight — 세션 고정 카메라 반평면

세션 시작 시 1회 확정하고 세션 내내 바꾸지 않는다.

```csharp
/// <summary>
/// 세션이 고정한 "카메라가 머무는 쪽"의 기준 벡터(수평, 정규화).
/// 활성 pair가 바뀌어 축이 재정의되어도 이 벡터는 유지되며,
/// 새 축의 SideSign은 항상 이 벡터를 기준으로 유도된다.
/// 그래서 3인 이상 대화에서도 카메라가 그룹 기준 같은 반평면에 남는다.
/// </summary>
public Vector3 StageRight { get; private set; } = Vector3.right;
```

세션 시작:

```csharp
public void Begin(IReadOnlyList<Transform> participants, Vector3 cameraPosition)
{
    ResetParticipants(participants);

    Vector3 fromCenter = cameraPosition - Center;
    fromCenter.y = 0f;
    StageRight = fromCenter.sqrMagnitude > AxisEpsilonSqr
        ? fromCenter.normalized
        : Vector3.right;

    // ... 나머지 상태 초기화
}
```

`StageRight`는 "카메라가 지금 서 있는 방향"이지 축의 right가 아니다. 축과 무관한 절대 기준이라야 축이 바뀌어도 의미가 보존된다.

### 2-2. 활성 pair 갱신

```csharp
/// <summary>
/// 이 라인의 가상선을 정의하는 두 인물을 설정한다.
/// 축은 pair에서 다시 잡되, 카메라 쪽은 세션 고정 StageRight가 결정하므로 반평면이 유지된다.
/// 반환값은 축이 회전한 각도(도) — 확립 전환 판정에 쓴다.
/// </summary>
public float SetActivePair(Transform subject, Transform partner)
{
    if (subject == null || partner == null || subject == partner)
        return 0f;

    RegisterParticipant(subject);
    RegisterParticipant(partner);

    ActiveSubject = subject;
    ActivePartner = partner;

    Vector3 previousAxis = AxisForward;
    RecaptureAxis();

    LastAxisChangeAngle = HasAxis ? Vector3.Angle(previousAxis, AxisForward) : 0f;
    return LastAxisChangeAngle;
}

private void RecaptureAxis()
{
    Vector3 forward = ActivePartner.position - ActiveSubject.position;
    forward.y = 0f;
    if (forward.sqrMagnitude < AxisEpsilonSqr)
    {
        forward = ActivePartner.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < AxisEpsilonSqr)
            forward = Vector3.forward;
    }

    AxisForward = forward.normalized;
    AxisRight = Vector3.Cross(Vector3.up, AxisForward).normalized;

    // 핵심: SideSign을 카메라 현재 위치가 아니라 세션 고정 StageRight에서 유도한다.
    // 카메라 위치에서 매번 재추론하면 pair마다 반평면이 새로 정해져 그룹 공간감이 무너진다.
    SideSign = Vector3.Dot(AxisRight, StageRight) >= 0f ? 1f : -1f;

    HasAxis = true;
}
```

> **주의:** `AxisRight`가 `StageRight`와 거의 직교하면(`|Dot|`이 0 근처) 부호가 노이즈에 뒤집힌다. 임계값(예: `0.05f`) 미만이면 **직전 SideSign을 유지**하는 가드를 넣는다. 이 케이스는 대화 인물들이 카메라 정면으로 일렬로 선 배치에서 실제로 발생한다.

### 2-3. Player/Partner → 참여자 집합

| 기존 | 변경 후 |
|------|---------|
| `Player` | `Participants` (전원). 플레이어는 특별 취급하지 않음 |
| `Partner` | `ActiveSubject` / `ActivePartner` (활성 pair) |
| `HasBothActors` | `HasActivePair` — 플레이어 유무와 무관 (**G5 해소**) |
| `Center` = 2인 중점 | 참여자 전원의 무게중심 |
| `SetPartner(t, camPos)` | `SetActivePair(subject, partner)` |
| `CaptureAxis(camPos, preserveSide)` | `Begin(...)` + 내부 `RecaptureAxis()` |

`Center`는 참여자 전원 기준으로 바꾼다. 다만 `DialogueShotComposer`의 `framesBothActors` 경로는 여전히 subject/anchor 2인 중점을 쓰므로 **Stage 2에서는 건드리지 않는다** — `Center`는 `StageRight` 산출과 향후 `GroupShot`의 입력이다. 이 구분을 흐리면 투샷 구도가 조용히 바뀐다.

### 2-4. Composer 연동

`DialogueShotComposer.ResolveForward` (`DialogueShotComposer.cs:149-156`)의 `session.Player` 폴백을 `session.ActiveSubject`로 교체한다. 로직 형태는 동일하다.

`ResolveSide`(`:172-181`)는 이미 `AxisRight * SideSign`만 쓰므로 **변경 없음** — Stage 2의 이득이 자동으로 전파된다. 이것이 기존 구조를 유지하는 이유다.

### 2-5. CameraManager / DialogueManager 연동

```csharp
public void BeginDialogueSession(IReadOnlyList<Transform> participants);
public void UpdateDialogueActivePair(Transform subject, Transform partner);  // UpdateDialogueSessionPartner 대체
```

`DialogueManager`:
- 세션 시작 시 플레이어 + 그래프에서 수집한 화자 전원을 참여자로 등록한다. 등록에 실패한 인물은 `SetActivePair`가 런타임에 자동 추가하므로 완전성은 필수가 아니다.
- 라인마다 `UpdateDialogueActivePair(speaker, listener)`를 호출한다. Stage 1의 `listener`가 그대로 입력이 된다.
- 기존 `_dialoguePartner` 승격 로직은 이 시점에 제거한다.

**G6(주석-동작 불일치)은 여기서 해소된다.** `UpdateDialogueActivePair`는 실제로 카메라 쪽을 유지하므로 기존 주석이 참이 된다.

---

## Stage 3 — 축 전환 확립 (요청 2번)

Stage 2가 "카메라가 어느 쪽에 있는가"를 지켰다면, Stage 3은 "축이 바뀌는 순간을 어떻게 보여주는가"를 지킨다.

### 설정 추가

```csharp
public enum DialogueAxisChangePolicy
{
    /// <summary>확립 처리를 하지 않는다(기존 동작).</summary>
    None = 0,
    /// <summary>전환을 Establish로 승격해 새 축으로 이동하며 넘어간다. 기본값.</summary>
    EstablishBlend = 1,
    /// <summary>Establish 블렌드에 더해 그 라인의 구도를 Wide로 강제해 새 관계를 명시한다.</summary>
    EstablishWide = 2
}
```

```csharp
[Header("축 전환")]
[Tooltip("활성 pair가 바뀌어 가상선이 이 각도(도) 이상 회전하면 확립 전환으로 처리한다. " +
         "컷으로 선을 넘지 않게 하는 안전장치.")]
[Range(15f, 179f)] public float axisEstablishAngle = 45f;

[Tooltip("가상선이 크게 바뀐 라인의 처리 방식.")]
public DialogueAxisChangePolicy axisChangePolicy = DialogueAxisChangePolicy.EstablishBlend;
```

### Director 변경

`DialogueShotDirector.Decide`에 축 전환 판정을 넣는다. **저작 오버라이드가 항상 이긴다**는 기존 원칙은 유지한다 — `request.ShotType`/`Transition`이 `Auto`가 아니면 아래 규칙은 개입하지 않는다.

```csharp
bool isAxisChange = session != null
                    && !isFirstLine
                    && settings.axisChangePolicy != DialogueAxisChangePolicy.None
                    && session.LastAxisChangeAngle >= settings.axisEstablishAngle;

// 샷 결정
DialogueShotType shot;
if (request.ShotType != DialogueShotType.Auto)
    shot = request.ShotType;                        // 저작 우선
else if (isAxisChange && settings.axisChangePolicy == DialogueAxisChangePolicy.EstablishWide)
    shot = DialogueShotType.Wide;                   // 새 관계를 한 프레임에 보여준다
else
    shot = DecideShot(settings, session, request, consecutiveShortLines);

// 전환 결정
DialogueShotTransition transition;
if (request.Transition != DialogueShotTransition.Auto)
    transition = request.Transition;                // 저작 우선
else if (isAxisChange)
    transition = DialogueShotTransition.Establish;  // 컷이 아니라 이동으로 넘는다
else
    transition = DecideTransition(session, shot, subject, isFirstLine);
```

`shot`이 `Wide`로 승격될 수 있으므로 **`subject` 해석은 `shot` 확정 이후**에 해야 한다. 현행 코드(`DialogueShotDirector.cs:30-34`)의 순서를 그대로 유지하면 된다.

### 소진 처리

`LastAxisChangeAngle`은 **판정에 쓰인 뒤 0으로 소진**한다. 소진하지 않으면 축이 한 번 크게 바뀐 뒤 같은 pair가 이어지는 동안 계속 Establish가 걸려 대화 전체가 늘어진다. 소진 지점은 세션 상태를 커밋하는 `DialogueCameraBehavior.OnEnter` 말미(`DialogueCameraBehavior.cs:92-96`)로, 다른 세션 커밋과 같은 자리에 둔다.

### 효과

`establishBlendTime`(기본 0.6초) 동안 카메라가 **연속 이동으로** 새 축 쪽으로 넘어간다. 조사에서 확인한 "crossing on a cut is the problem, not crossing itself"를 그대로 만족한다.

`EstablishWide`는 인물이 3명 이상 벌어져 있을 때 관계를 다시 읽히게 하는 강한 처리다. 대화 리듬을 끊을 수 있으므로 기본값은 `EstablishBlend`로 두고, 시나리오 단위로 올린다.

---

## 작업 순서와 의존 관계

| 단계 | 내용 | 선행 | 단독 동작 |
|------|------|------|-----------|
| Stage 1 | `listenerSpeakerId` + 청자 해석 분리 | 없음 | 가능 |
| Stage 2 | 참여자 집합 + `StageRight` 고정 반평면 | Stage 1 | 가능 |
| Stage 3 | 축 전환 확립 전환 | Stage 2 (`LastAxisChangeAngle` 필요) | 불가 |

각 단계는 독립 커밋으로 나눈다. Stage 2가 가장 위험하므로 Stage 1을 먼저 넣고 저작으로 검증한 뒤 진행한다.

---

## 리스크와 함정

| 리스크 | 대응 |
|--------|------|
| `AxisRight ⊥ StageRight` 근처에서 SideSign 부호 진동 | `|Dot| < 0.05f`면 직전 SideSign 유지 (Stage 2-2) |
| Replay(녹화) 노드 왕복 시 세션 상태 초기화 | `DialogueCameraBehavior.OnExit`는 세션을 건드리지 않는다는 기존 계약 유지. 신규 필드도 동일 |
| `LastAxisChangeAngle` 미소진으로 Establish 연발 | 판정 후 즉시 0으로 소진 (Stage 3) |
| `Center` 의미 변경이 투샷 구도에 누수 | `framesBothActors` 경로는 subject/anchor 중점 유지, `Center`와 분리 (Stage 2-3) |
| `HasBothActors` 제거로 인트로 조건 변화 | `DialogueShotDirector.cs:42`, `:80`, `DialogueShotSession.cs:52` 세 곳을 `HasActivePair`로 일괄 교체. 플레이어 불참 대화에서 인트로가 새로 발동하게 되므로 의도인지 확인 |
| `DialogueShotRequest.Matches`가 새 listener를 구분 못 함 | `Listener`가 이미 `Matches`에 포함되어 있어 추가 조치 불필요 |
| 노드 데이터 추가 시 저작 도구 누락 | Stage 1에서 커스텀 인스펙터 동기화를 완료 조건에 포함 |

---

## 검증 계획

### EditMode 테스트 (신규)

`Assets/Tests/EditMode/Camera/DialogueShotSessionTests.cs`:

1. `SetActivePair`로 축을 180° 뒤집어도 카메라 위치가 `StageRight` 반평면에 남는지 — Stage 2의 핵심 계약.
2. 3인 A/B/C에서 pair를 A-B → A-C → B-C로 순환시켜도 SideSign이 세션 내내 일관된 반평면을 가리키는지.
3. `AxisRight`가 `StageRight`와 직교에 가까울 때 SideSign이 유지되는지(부호 진동 가드).
4. `LastAxisChangeAngle`이 축 회전량을 정확히 보고하고, 같은 pair 재설정 시 0인지.
5. `request.ShotType`/`Transition`이 지정된 라인에서 축 전환 규칙이 개입하지 않는지 — 저작 우선 원칙.

### Play Mode 수동 검증

플레이어 + NPC 2명이 삼각으로 선 테스트 대화 그래프를 만들고:

- 화자를 P→A→B→A 순으로 교대시켜 카메라가 반평면을 넘지 않는지 육안 확인.
- `listenerSpeakerId`로 A→B 라인을 지정해 플레이어를 뺀 pair가 잡히는지 확인.
- `axisChangePolicy`를 `None` / `EstablishBlend` / `EstablishWide`로 바꿔가며 축 전환 라인의 체감 비교.
- Replay 노드를 중간에 끼워 세션 상태(인트로 소진, 반평면)가 유지되는지 확인.

**컴파일 통과와 EditMode 통과만으로 "완료"라고 보고하지 않는다.** 반평면 유지는 수치로 검증 가능하지만 축 전환의 체감은 Play Mode 육안 확인이 필요하다.

---

## 후속 작업 (본 문서 범위 밖)

- **GroupShot 프리셋** — 참여자 바운딩 기반 N인 프레이밍. Cinemachine `CinemachineGroupFraming`이 레퍼런스이며, Toric 연구의 "2개 고정 + 나머지 오차 최소화"에 해당한다. Stage 2의 `Participants`/`Center`가 그대로 입력이 된다.
- **참여자 가중치** — 화자 가중, 침묵자 감쇠. 4인 이상에서 화자를 프레임 중심에 유지한다.
- **중립 샷 리셋** — 인물이 없는 샷으로 축을 리셋하는 강한 전환. `EstablishWide`보다 상위 수단이며, 필요성이 실제로 확인된 뒤에 검토한다.

---

## 참고 자료

- [Behind the Scenes of Cinematic Dialogues in 'The Witcher 3: Wild Hunt' — GDC Vault](https://www.gdcvault.com/play/1022988/Behind-the-Scenes-of-Cinematic)
- [Cinematic Dialogue In The Witcher 3 — Game Anim](https://www.gameanim.com/2016/03/23/cinematic-dialogue-witcher-3/)
- [Intuitive and Efficient Camera Control with the Toric Space — Christophe Lino](https://sites.google.com/site/christophelino/research/toric-space)
- [Intuitive and efficient camera control with the toric space — ACM TOG / SIGGRAPH 2015](https://dl.acm.org/doi/10.1145/2766965)
- [Dialogue Modding pt 4 – Cinematics (Dragon Age II stages/places/cameras) — sapphimods](https://sapphimods.me/tutorials/dialogue-modding-pt-4/)
- [How to use 180-degree rule with one, two, three and more characters — wolfcrow](https://wolfcrow.com/how-to-use-180-degree-rule-with-one-character-two-characters-and-three-and-more-characters/)
- [180 Degree Rule in Film: How to Use & Break It — Peek at This](https://peekatthis.com/180-degree-rule-in-film-and-how-to-break-the-line/)
- [Cinemachine Group Framing component — Unity Docs](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/CinemachineGroupFraming.html)
- [10 Facts About Mass Effect's Animation — 80 Level](https://medium.com/@EightyLevel/10-facts-about-mass-effects-animation-47f94d7c1c49)
