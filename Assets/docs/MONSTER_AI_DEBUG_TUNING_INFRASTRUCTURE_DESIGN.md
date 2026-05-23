# 몬스터 AI 디버깅·튜닝 인프라 설계 문서

> 작성일: 2026-05-23
> 대상 버전: Unity 6 (6000.0.60f1), URP
> 관련 시스템: `BehaviorTreeRunner.DebugTrace`, `BehaviorTreeInspectorView`, `EnemyCombatDecisionEvaluator`

---

## 0. 요약

현재 BT는 `DebugTrace` + `BehaviorTreeInspectorView`로 어떤 노드가 어떤 결과를 냈는지 추적 가능하다. 하지만 **"왜 이 Intent가 선택됐는가"**, **"시간축에서 Intent 점수가 어떻게 변했는가"** 같은 의사결정 가시화는 부족하다.

본 설계는 두 가지 인프라를 추가한다:
1. **Intent Score Timeline 패널** — BT Editor에 9개(또는 11개) Intent 점수의 시간축 차트
2. **Encounter Replay Dump** — 전투 종료 시 Intent 히스토리 + 입력 컨텍스트를 JSON으로 저장

플레이테스트 후 분석·튜닝의 핵심 도구가 된다.

---

## 1. 현재 상태

### 1.1 기존 디버깅 자원

| 자원 | 위치 | 출력 형태 |
|------|------|----------|
| `BTDebugTrace` | `BehaviorTreeRunner` | 노드별 Tick 결과, 사유 문자열 |
| `BehaviorTreeInspectorView` | Editor | 노드 클릭 시 최근 Trace 표시 |
| Blackboard 인스펙터 | Editor | 현재 Blackboard 값 |
| Breadcrumb Bar | `BehaviorTreeEditorWindow.Layout` | 현재 실행 경로 |
| `EnemyCombatDecisionEvaluator.Reason` | Service Tick → Trace | "Intent=Attack, score=0.78, reason=..." 단문 |

### 1.2 한계

- **점수 시간축 부재.** 한 순간의 점수만 보임. "5초 전엔 Pressure가 우세였는데 왜 Counter로 바뀌었나" 같은 질문에 답 못 함.
- **재현 불가.** 플레이테스트 중 이상한 의사결정이 보여도 그 순간의 입력 스냅샷이 없음.
- **튜닝 피드백 루프 느림.** SO 값 변경 → 게임 실행 → 같은 상황 만들기 → 결과 확인. 같은 상황 만들기가 가장 비싸다.

---

## 2. 설계 목표

1. **Intent 점수 타임라인 시각화** — BT Editor 안에 stacked area chart
2. **인카운터 단위 텔레메트리** — 전투 시작~종료 구간의 의사결정 전부 JSON 저장
3. **Replay 뷰어** — 저장된 JSON을 BT Editor에서 다시 열어 시간 슬라이더로 재생
4. **튜닝 워크플로우 단축** — 같은 상황을 만들지 않고도 SO 값 변경 후 "이 입력에서 점수가 어떻게 바뀌었을지"를 미리보기

---

## 3. 모듈 1: Intent Score Timeline 패널

### 3.1 데이터 수집

`EvaluateEnemyCombatIntentService`가 매 틱 평가 후 결과를 **링 버퍼**에 누적.

```csharp
public class IntentScoreTimeline : MonoBehaviour
{
    [SerializeField] private int _capacity = 600;  // 10초 분량 (60fps 기준)

    private readonly RingBuffer<IntentScoreSnapshot> _snapshots;

    public IReadOnlyList<IntentScoreSnapshot> Snapshots => _snapshots;

    public void Record(in CombatIntentEvaluation evaluation, float time);
}

public readonly struct IntentScoreSnapshot
{
    public readonly float Time;
    public readonly CombatIntent SelectedIntent;
    public readonly CombatIntent LastIntent;
    public readonly int ConsecutiveIntentCount;
    public readonly float AttackScore;
    public readonly float PunishScore;
    public readonly float CounterScore;
    public readonly float PressureScore;
    public readonly float ChaseScore;
    public readonly float RetreatScore;
    public readonly float KeepDistanceScore;
    public readonly float DefendScore;
    public readonly float RecoverScore;
    public readonly string RhythmPhase;
    public readonly string Reason;
}
```

`EnemyAIController.Awake`에서 `IntentScoreTimeline`을 자동 부착(없으면 추가). `EvaluateEnemyCombatIntentService`의 Tick 끝에서 `Record` 호출.

### 3.2 BT Editor 패널

`BehaviorTreeEditorWindow`에 신규 탭 또는 사이드 패널:

```
┌─ Intent Score Timeline ───────────────────────────────────────┐
│                                                                │
│ Attack     ████░░░░░░░░░░░░░░████████░░░░░░░░░░░░░░░░░░░░░░ │
│ Punish     ░░░░░░░░░░░░░░░░░░░░░░░░░██████████░░░░░░░░░░░░░ │
│ Counter    ░░░░░░░░░░░░░██████████████████░░░░░░░░░░░░░░░░░ │
│ Pressure   ░░██████████░░░░░░░░░░░░░░░░░░░░██████████░░░░░░ │
│ ...                                                            │
│                                                                │
│  -5s    -4s    -3s    -2s    -1s    0 (now)                   │
│                                                                │
│ Selected: ●Pressure ●Counter ●Attack ●Punish ●Pressure        │
│                                                                │
│ [▶ Play] [⏸ Pause] [Export Replay JSON]                       │
└────────────────────────────────────────────────────────────────┘
```

- **Stacked Area Chart** — IMGUI 또는 UI Toolkit 캔버스
- **상단 색띠** — 매 틱 선택된 Intent를 색상 띠로 표시
- **호버 시 툴팁** — 해당 시점의 9개 점수 정확값 + Blackboard 스냅샷
- **선택 안정화 표시** — 유지 보너스, 반복 패널티, 전환 비용이 적용된 시점을 마커로 표시

### 3.3 구현 위치

```
Assets/02.Scripts/AI/BehaviorTree/Editor/IntentScoreTimelineView.cs
Assets/02.Scripts/AI/BehaviorTree/Editor/IntentScoreTimelineRenderer.cs
```

`BehaviorTreeEditorWindow.Layout.cs`의 사이드 패널 영역에 dock.

---

## 4. 모듈 2: Encounter Replay Dump

### 4.1 데이터 구조

```csharp
[Serializable]
public class EncounterReplay
{
    public string actorId;
    public string actorName;
    public float startTime;
    public float endTime;
    public List<ReplayFrame> frames = new();
    public List<ReplayEvent> events = new();
}

[Serializable]
public class ReplayFrame
{
    public float t;
    public CombatIntent selectedIntent;
    public CombatIntent lastIntent;
    public int consecutiveIntentCount;
    public float[] scores;          // 9개 Intent 점수
    public float distance;
    public float preferredRange;
    public float optimalRange;
    public float healthPercent;
    public float stamina;           // 스태미나 시스템 도입 후
    public string playerState;
    public string predictedNextPlayerAction;
    public float predictionConfidence;
    public string rhythmPhase;
    public string reason;
    public bool hasAttackSlot;
    public string resolverFailureReason;
}

[Serializable]
public class ReplayEvent
{
    public float t;
    public string eventType;        // "attack_landed", "took_damage", "intent_change", ...
    public string detail;
}
```

### 4.2 기록 트리거

- **시작:** `EnemyDetection.AcquireTarget` 호출 시
- **종료:** 적 사망 시, 또는 `EnemyDetection.LoseTarget` 후 N초 경과
- **저장 경로:** `Application.persistentDataPath/EncounterReplays/{timestamp}_{actorId}.json`

### 4.3 활성화 토글

```csharp
[Tooltip("Encounter Replay 기록 활성화 여부. 빌드에서는 항상 false 권장")]
[SerializeField] private bool _enableReplayRecording = false;
```

기본은 비활성. 디자이너/QA가 Editor에서만 켬.

### 4.4 신규 매니저

`Assets/02.Scripts/Manager/EncounterReplayManager.cs` (BaseManager 패턴 추종).
- 모든 적의 `EncounterReplay` 인스턴스 수집
- 종료 시 한 파일에 모아 저장
- 빌드 시 자동 비활성 (Editor 전용 매니저로 처리해도 됨)

---

## 5. 모듈 3: Replay Viewer (BT Editor)

### 5.1 진입점

`BehaviorTreeEditorWindow` 상단 메뉴:
```
Replay → Load JSON…
```

선택한 파일을 파싱하여 Intent Score Timeline 패널에 표시. **라이브 실행이 아니라 정적 데이터 재생**.

### 5.2 컨트롤

- 시간 슬라이더 (Replay 전체 길이)
- ▶ Play / ⏸ Pause / ⏭ Step
- ReplayEvent를 타임라인 위 마커로 표시 ("Damage Taken" 마커 클릭 시 상세 표시)

### 5.3 비교 모드 (선택, 후속)

두 Replay 파일을 동시에 로드. 한 화면에 두 점수 곡선을 비교. SO 값 튜닝 전후 비교에 유용.

---

## 6. 모듈 4: Intent Score Preview (선행 문서와 통합)

`MONSTER_INTENT_WEIGHTS_EXTERNALIZATION_DESIGN.md` 8.2절의 "Intent Score Preview"와 동일. 본 설계는 그쪽으로 위임하고 중복 정의하지 않는다.

요약:
- `EnemyIntentWeightsSO` 인스펙터 하단에 입력 슬라이더 (거리, HP%, Aggression 등) + 9개 Intent 점수 막대그래프
- 디자이너가 SO 값을 바꾸면 즉시 점수 곡선 변화 확인

### 6.1 필수 디버그 필드

Intent 기반 AI 튜닝에서 최소로 보여야 하는 값은 다음이다. Timeline, Replay, Blackboard View가 같은 필드명을 사용한다.

| 필드 | 목적 |
|------|------|
| `Decision.SelectedIntent` | 최종 선택된 Intent |
| `Decision.LastIntent` | 직전 실행 Intent |
| `Decision.ConsecutiveIntentCount` | 반복 패널티 확인 |
| `Decision.IntentScore.*` | Intent별 최종 점수 |
| `RhythmPhase` | Observe / ReEnter / CommitAttack / Pressure / Disengage 등 리듬 상태 |
| `Reason` | Evaluator가 만든 사람이 읽는 설명 |
| `Distance`, `PreferredRange`, `OptimalRange` | 거리 판단 검증 |
| `PlayerReadSummary` | Dodge/Guard/Attack/Recover 카운트 |
| `PredictionConfidence`, `PredictedNextPlayerAction` | 예측기 연동 검증 |
| `StaminaNormalized`, `IsStaminaExhausted` | 스태미나 모델 연동 검증 |
| `HasAttackSlot`, `GroupBreatherRemaining` | 그룹 AI 연동 검증 |
| `ResolverFailureReason` | Intent는 골랐지만 실제 State 전환이 실패한 이유 |

`ResolverFailureReason`은 `EnemyActionResolver.TryTransition` 실패 사유를 마지막 1개만 Blackboard 또는 Timeline에 기록한다. 튜닝 중 "점수는 맞는데 행동이 안 나간다" 문제를 빠르게 분리하기 위함이다.

---

## 7. 호환성

- 본 설계의 모든 모듈은 **선택적 활성**. 토글 OFF 시 게임 동작 영향 없음.
- 빌드에서는 `EncounterReplayManager`와 `IntentScoreTimeline`이 `#if UNITY_EDITOR`로 컴파일 분기 가능. 런타임 오버헤드 0.
- 기존 `BehaviorTreeRunner.DebugTrace`와는 별개 시스템. 충돌 없음.

---

## 8. 성능 고려

- `IntentScoreTimeline` 링 버퍼 capacity 600 × snapshot 약 60바이트 = 36KB/적. 100마리 동시면 3.6MB. 허용 가능.
- 직렬화는 `JsonUtility` 사용. 한 인카운터 평균 5초 × 60fps × 9 floats = 약 30KB JSON. 디스크 부담 없음.
- 패널 렌더링은 BT Editor가 열려 있을 때만 활성. 게임 실행 중 항상 켜져 있어도 IMGUI 비용 미미.

---

## 9. 검증 / 테스트 시나리오

| ID | 시나리오 |
|----|---------|
| 9.1 | 적과 5초 교전 후 BT Editor의 Intent Timeline에 9개 점수 곡선이 시간축에 표시 |
| 9.2 | 점수 곡선 호버 시 해당 시점의 정확한 값과 Blackboard 스냅샷이 툴팁에 표시 |
| 9.3 | `_enableReplayRecording = true`로 교전 종료 시 `persistentDataPath/EncounterReplays/`에 JSON 생성 |
| 9.4 | 저장된 JSON을 BT Editor에서 Load → 슬라이더로 재생, 점수 곡선 동일 표시 |
| 9.5 | 토글 OFF 시 zero overhead (Editor Profiler에서 시간 측정) |

---

## 10. 신규/변경 클래스 요약

| 위치 | 변경 종류 | 비고 |
|------|----------|------|
| `Assets/02.Scripts/AI/Debug/IntentScoreTimeline.cs` | 신규 | 런타임 컴포넌트 |
| `Assets/02.Scripts/AI/Debug/IntentScoreSnapshot.cs` | 신규 | readonly struct |
| `Assets/02.Scripts/AI/Debug/EncounterReplay.cs` | 신규 | 직렬화 데이터 |
| `Assets/02.Scripts/AI/Debug/EncounterReplayRecorder.cs` | 신규 | 적별 기록기 |
| `Assets/02.Scripts/Manager/EncounterReplayManager.cs` | 신규 | 매니저 (Editor 전용 분기 가능) |
| `Assets/02.Scripts/AI/BehaviorTree/Editor/IntentScoreTimelineView.cs` | 신규 | Editor 패널 |
| `Assets/02.Scripts/AI/BehaviorTree/Editor/IntentScoreTimelineRenderer.cs` | 신규 | IMGUI 그래프 |
| `Assets/02.Scripts/AI/BehaviorTree/Editor/EncounterReplayLoader.cs` | 신규 | JSON 로더 |
| `Assets/02.Scripts/AI/BehaviorTree/Editor/BehaviorTreeEditorWindow.Layout.cs` | 패널 dock | 사이드 영역에 통합 |
| `Assets/02.Scripts/AI/BehaviorTree/Nodes/Service/EvaluateEnemyCombatIntentService.cs` | 호출 추가 | `IntentScoreTimeline.Record` |
| `Assets/02.Scripts/GameActor/Component/Enemy/EnemyDetection.cs` | hook | Acquire/Lose 시 Recorder 시작/종료 |

---

## 11. 작업 순서

1. **Phase A (1일)** — `IntentScoreTimeline` + `Record` 호출 연결, 메모리 링 버퍼만
2. **Phase B (1.5일)** — `IntentScoreTimelineView` IMGUI 차트 렌더링, BT Editor 사이드 패널 통합
3. **Phase C (1일)** — `EncounterReplay` 직렬화 + `EncounterReplayRecorder` + 시작/종료 hook
4. **Phase D (1일)** — Replay Viewer (JSON Load → 동일 패널 표시)
5. **Phase E (반일)** — 토글 정리, 빌드 분기, 성능 검증

총 4~5일. Phase A~B만으로도 즉시 효용이 있음 (Phase C 이후는 점진 추가 가능).

---

## 12. 명시적 비목표

- **자동 튜닝(파라미터 최적화)은 본 설계 범위 밖.** 사람 디자이너가 차트를 보고 SO 값을 수동 조정한다.
- **머신러닝 기반 행동 분석은 본 설계 범위 밖.**
- **Replay 영상 녹화는 본 설계 범위 밖.** 점수와 입력만 기록한다.
- **그룹 단위 통계 대시보드는 본 설계 범위 밖.** 다만 각 Frame에는 그룹 관련 필드를 기록해 후속 분석이 가능하게 한다.

---

## 13. 참고

- 관련 코드: `BehaviorTreeInspectorView.cs`, `BehaviorTreeEditorWindow.Debug.cs`, `BTDebugTrace.cs`
- 관련 설계 문서:
  - `MONSTER_INTENT_WEIGHTS_EXTERNALIZATION_DESIGN.md` (Intent Score Preview는 그쪽으로)
  - `BOSS_HIERARCHICAL_PLAN_DESIGN.md` (Plan 진행 시각화는 본 패널에 통합 가능)
