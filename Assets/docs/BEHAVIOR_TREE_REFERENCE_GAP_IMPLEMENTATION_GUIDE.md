# Behavior Tree 레퍼런스 누락 기능 구현 가이드

> 작성일: 2026-04-28  
> 대상 버전: Unity 6 (6000.0.60f1), URP  
> 적용 범위: `Assets/02.Scripts/AI/BehaviorTree/` 커스텀 BT 런타임/에디터 보강 계획  
> 원칙: 기존 `EnemyBrain`, `EnemyFlyingBrain`, Enemy State 구조는 직접 수정하지 않는다.

---

## 개요

현재 커스텀 BT는 Behavior Designer 계열 구조의 기본 골격은 갖추고 있다.

- `Action`, `Conditional`, `Composite`, `Decorator`에 대응하는 노드 계층
- `Success`, `Failure`, `Running` 기반 실행 상태
- `Sequence`, `Selector`, `Parallel` 기본 Composite
- `Inverter`, `Cooldown`, `Repeat` 기본 Decorator
- Blackboard, GraphView 에디터, Inspector, Validator, JSON Import/Export
- Play Mode 상태 색상 표시

다만 Behavior Designer 레퍼런스와 Opsive 공식 Decorator/Flow 문서 기준으로 보면, 실제 운용 단계에 필요한 고급 실행 제어와 디버깅 기능은 아직 부족하다.

이 문서는 누락된 부분을 기능 단위로 나누고, 기존 AI 구조를 건드리지 않는 범위에서 추가 구현할 순서를 정리한다.

---

## 레퍼런스 기준

| 출처 | 구현 기준으로 삼을 내용 |
|------|-------------------------|
| [Unity Behaviour Designer 설명 글](https://wlsdn629.tistory.com/entry/unity-behaviour-designer) | Task 분류, Sequence/Selector/Parallel 흐름, Decorator, Conditional Abort, Shared Variable, Breakpoint/Disable Task, Behavior Manager 옵션 |
| [Opsive Decorator 공식 문서](https://opsive.com/support/documentation/behavior-designer-pro/concepts/tasks/decorator/) | Decorator는 단일 자식을 가지며 자식의 반환값이나 실행 흐름을 변경한다 |
| [Opsive Flow 문서](https://opsive.com/support/documentation/behavior-designer-pro/concepts/flow/) | `Success`, `Failure`, `Running`, 좌측 우선순위, Conditional Abort 실행 흐름 |

---

## 현재 구현 상태 요약

### 구현 완료

| 영역 | 현재 상태 | 관련 파일 |
|------|-----------|-----------|
| 노드 계층 | `BTNode`, `BTActionNode`, `BTConditionNode`, `BTCompositeNode`, `BTDecoratorNode` 구현 | `Assets/02.Scripts/AI/BehaviorTree/Runtime/` |
| 실행 상태 | `BTStatus.Success`, `Failure`, `Running` 구현 | `BTStatus.cs` |
| 런타임 실행 | `BehaviorTreeRunner`가 `BehaviorTreeAsset`을 복제해 tick 실행 | `BehaviorTreeRunner.cs`, `BehaviorTreeAsset.cs` |
| Composite | `SequenceNode`, `SelectorNode`, `ParallelNode` 기본 동작 구현 | `Nodes/Composite/` |
| Decorator | `InverterNode`, `CooldownNode`, `RepeatNode` 구현 | `Nodes/Decorator/` |
| Blackboard | 기본 Key/Value 저장과 런타임 복제 구현 | `Runtime/Blackboard/` |
| 에디터 | GraphView, Inspector, Blackboard, Validator, Debug Target 구현 | `Editor/` |
| JSON | BT Asset Export/Import 구현 | `BehaviorTreeJsonUtility.cs` |

### 주요 누락

| 누락 기능 | 현재 문제 | 우선순위 |
|-----------|-----------|----------|
| Conditional Abort | `BTAbortType` 필드만 있고 실제 재평가/중단 로직 없음 | 높음 |
| Pause/Resume | `StartTree`, `StopTree`, `RestartTree`만 있고 일시정지/재개 없음 | 높음 |
| Restart When Complete | Root가 `Success` 또는 `Failure`가 된 뒤 자동 재시작 옵션 없음 | 높음 |
| Manual Tick | 외부 시스템이 직접 tick을 호출하는 모드 없음 | 중간 |
| Parallel Abort | 실패 확정 시 다른 Running 자식 중단 없음 | 중간 |
| Breakpoint | 실행 중 특정 노드에서 멈추는 기능 없음 | 중간 |
| Disable Node | 에디터에서 특정 노드를 비활성화하고 실행에서 제외하는 기능 없음 | 중간 |
| Decorator 확장 | 공식 Decorator 개념 대비 `Return Failure`, `Return Success`, `Until Success`, `Until Failure` 등 부족 | 중간 |
| Debug Trace | tick 순서, 상태 변경, Abort 원인을 기록하는 추적 데이터 없음 | 중간 |
| Error Window 고도화 | 실시간 오류 목록은 있으나 Behavior Designer 수준의 상세 원인/이동 기능 부족 | 낮음 |

---

## 구현 원칙

1. 기존 Enemy AI 파일은 수정하지 않는다.
2. 모든 보강은 `Assets/02.Scripts/AI/BehaviorTree/` 내부에서 진행한다.
3. 런타임 기능을 먼저 완성하고, 에디터는 런타임 상태를 드러내는 방식으로 붙인다.
4. Behavior Designer와 1:1 복제를 목표로 하지 않고, UPlayground Enemy AI 이전에 필요한 기능부터 구현한다.
5. `BTAbortType`, `BTNode`, `BehaviorTreeRunner`처럼 이미 존재하는 API는 가능한 유지하며 내부 동작을 확장한다.
6. 동일 `BehaviorTreeAsset`을 여러 Runner가 공유해도 런타임 상태가 섞이지 않아야 한다.

---

## Phase R1: Runner 실행 제어 보강

### 목표

Behavior Designer의 Behavior Manager 옵션 중 UPlayground에 필요한 실행 제어를 `BehaviorTreeRunner`에 추가한다.

### 구현 항목

| 항목 | 설명 |
|------|------|
| `EnableBehavior()` | 정지 또는 일시정지된 BT 실행 시작 |
| `DisableBehavior(bool pause)` | `pause = true`면 현재 Running 상태 유지, `false`면 Abort 후 정지 |
| `PauseTree()` | tick만 멈추고 런타임 트리와 노드 상태는 보존 |
| `ResumeTree()` | Pause 이전 Running 상태에서 이어서 실행 |
| `TickOnce()` | Manual Tick 모드에서 외부 호출로 1회 tick |
| `_restartWhenComplete` | Root가 `Success` 또는 `Failure`가 되면 자동 재시작 |
| `_resetValuesOnRestart` | 재시작 시 Blackboard 값을 기본값으로 되돌릴지 선택 |
| `_tickMode` | `UpdateInterval`, `EveryFrame`, `Manual` 중 선택 |

### 예상 파일

| 파일 | 작업 |
|------|------|
| `Runtime/BehaviorTreeRunner.cs` | 실행 상태 enum, Pause/Resume/Manual Tick/Restart 옵션 추가 |
| `Runtime/BehaviorTreeRunnerMode.cs` | tick 모드 enum 추가 |
| `Editor/BehaviorTreeInspectorView.cs` | Runner 옵션은 필요 시 별도 CustomEditor에서 노출 |

### 완료 조건

- Play Mode에서 `PauseTree()` 호출 시 Running 노드가 Abort되지 않는다.
- `ResumeTree()` 호출 후 같은 런타임 트리에서 이어서 tick 된다.
- Manual Tick 모드에서는 `Update()`가 자동 tick 하지 않는다.
- `Restart When Complete`가 켜진 경우 Root 완료 후 다음 tick에 재시작한다.

---

## Phase R2: Composite Abort/Reset 규칙 보강

### 목표

Sequence, Selector, Parallel이 Running 자식과 실패/성공 확정 상황을 명확하게 정리하도록 만든다.

### 구현 항목

| 항목 | 설명 |
|------|------|
| Running sibling 정리 | Sequence/Selector가 완료될 때 실행 중이던 자식이 남지 않도록 정리 |
| `OnStop` 하위 Abort 정책 | Composite 종료 시 필요한 자식만 Abort |
| Parallel 실패 시 Abort | `requireAllSuccess`에서 하나가 Failure면 나머지 Running 자식 Abort |
| Parallel 성공 시 Abort | `requireAllSuccess = false`에서 하나가 Success면 나머지 Running 자식 Abort |
| Reset 안정화 | Composite 재시작 시 `_currentIndex`와 Running 자식 상태 초기화 |

### 예상 파일

| 파일 | 작업 |
|------|------|
| `Nodes/Composite/SequenceNode.cs` | 종료 시 현재 Running 자식 정리 |
| `Nodes/Composite/SelectorNode.cs` | 종료 시 현재 Running 자식 정리 |
| `Nodes/Composite/ParallelNode.cs` | 성공/실패 확정 시 나머지 Running 자식 Abort |
| `Runtime/BTNode.cs` | 필요 시 `IsRunning` 또는 `ExecutionState` 공개 보강 |

### 완료 조건

- Parallel에서 실패가 확정되면 다른 Running 자식의 `OnAbort`가 호출된다.
- Selector가 Success로 종료된 뒤 이전 Running 자식이 다음 실행에 남지 않는다.
- Sequence/Selector가 재시작될 때 `_currentIndex`가 항상 0에서 시작한다.

---

## Phase R3: Conditional Abort 구현

### 목표

Behavior Designer의 `None`, `Self`, `Lower Priority`, `Both` 개념을 현재 `BTAbortType`에 실제 동작으로 연결한다.

### 개념 정리

| Abort Type | 의미 |
|------------|------|
| `None` | 조건 재평가 없음 |
| `Self` | 같은 Composite 내부에서 현재 실행 중인 브랜치를 조건 변화에 따라 중단 |
| `LowerPriority` | 더 오른쪽에 있는 낮은 우선순위 브랜치가 Running 중일 때 왼쪽 조건을 재평가해 중단 |
| `Both` | `Self`와 `LowerPriority`를 모두 수행 |

### 구현 접근

초기 구현은 Behavior Designer의 모든 최적화 구조를 복제하지 않고, UPlayground에 필요한 안정성을 우선한다.

1. `BTConditionNode`에 마지막 평가 결과를 저장한다.
2. `BTCompositeNode`가 자식 중 Conditional 후보를 수집한다.
3. `SequenceNode`, `SelectorNode` tick 시작 시 abort 대상 조건을 재평가한다.
4. 결과가 바뀌면 현재 Running 자식을 Abort하고 `_currentIndex`를 재계산한다.
5. Debug Trace에 Abort 원인과 대상 노드를 기록한다.

### 예상 파일

| 파일 | 작업 |
|------|------|
| `Runtime/BTConditionNode.cs` | 조건 재평가 API 추가 |
| `Runtime/BTCompositeNode.cs` | Abort 평가 공통 헬퍼 추가 |
| `Nodes/Composite/SequenceNode.cs` | `Self`, `Both` 처리 |
| `Nodes/Composite/SelectorNode.cs` | `LowerPriority`, `Both` 처리 |
| `Runtime/BTAbortType.cs` | 기존 enum 유지 |
| `Editor/BehaviorTreeNodeView.cs` | Abort 발생 시 상태 색상 또는 아이콘 표시 |

### UPlayground 적용 예시

| 상황 | 권장 Abort |
|------|------------|
| Patrol 중 타겟 발견 | `LowerPriority` |
| Chase 중 타겟이 사라짐 | `Self` |
| CombatIdle 중 공격 가능 조건 충족 | `LowerPriority` |
| Attack 준비 중 타겟 사망 | `Self` |
| Retreat 중 거리가 충분히 벌어짐 | `Self` |

### 완료 조건

- `BT_EnemyGroundBasic_Test.json` 흐름에서 Patrol 중 `HasTarget`이 true가 되면 Patrol 브랜치가 중단될 수 있다.
- Running Action이 중단될 때 `Abort()`와 `OnAbort()`가 호출된다.
- Abort 발생 원인이 Debug Trace에 기록된다.
- 기존 `AbortType = None` 그래프의 동작은 변경되지 않는다.

---

## Phase R4: Decorator 노드 확장

### 목표

Opsive 공식 Decorator 문서의 핵심 개념인 “단일 자식을 감싸고 결과/흐름을 바꾸는 노드”를 더 넓은 기본 노드 세트로 확장한다.

### 추가 후보

| 노드 | 동작 |
|------|------|
| `ReturnSuccessNode` | 자식 결과와 무관하게 자식 완료 시 Success 반환. Running은 유지 |
| `ReturnFailureNode` | 자식 결과와 무관하게 자식 완료 시 Failure 반환. Running은 유지 |
| `UntilSuccessNode` | 자식이 Success가 될 때까지 반복 |
| `UntilFailureNode` | 자식이 Failure가 될 때까지 반복 |
| `TimeoutNode` | 지정 시간 안에 자식이 완료되지 않으면 Failure 반환 및 자식 Abort |
| `GuardConditionNode` | 조건 노드가 Success인 동안만 자식 실행 |
| `ForceAbortNode` | 특정 Blackboard 조건 변화 시 자식 Abort |

### Decorator 공통 규칙

| 규칙 | 설명 |
|------|------|
| 자식 수 | 정확히 1개 |
| Running 처리 | 자식이 Running이면 Decorator도 Running을 반환하는 것을 기본으로 한다 |
| 종료 처리 | Decorator가 Success/Failure로 확정되면 Running 자식은 남지 않아야 한다 |
| Reset 처리 | 반복형 Decorator는 반복 사이클마다 자식 상태를 명확히 Reset한다 |

### 예상 파일

| 파일 | 작업 |
|------|------|
| `Runtime/BTDecoratorNode.cs` | 공통 Child 검증/Abort 헬퍼 추가 |
| `Nodes/Decorator/ReturnSuccessNode.cs` | 신규 |
| `Nodes/Decorator/ReturnFailureNode.cs` | 신규 |
| `Nodes/Decorator/UntilSuccessNode.cs` | 신규 |
| `Nodes/Decorator/UntilFailureNode.cs` | 신규 |
| `Nodes/Decorator/TimeoutNode.cs` | 신규 |
| `Editor/BehaviorTreeAssetValidator.cs` | Decorator 자식 수 검증 유지 |

### 완료 조건

- Decorator 연결은 항상 단일 자식만 유지한다.
- Decorator가 교체 연결될 때 기존 Edge와 데이터 참조가 함께 정리된다.
- 자식 Running 상태는 Decorator 정책에 따라 보존 또는 Abort된다.

---

## Phase R5: Debug Trace, Breakpoint, Disable Node

### 목표

Behavior Designer의 Visual Debugger에 가까운 최소 기능을 추가한다.

### 구현 항목

| 항목 | 설명 |
|------|------|
| Debug Trace | tick 순서, 노드 GUID, 상태 변화, Abort 원인 기록 |
| Breakpoint | 특정 노드 진입 시 Runner를 pause |
| Disable Node | 에디터에서 특정 노드를 실행 제외 |
| Step Tick | Pause 상태에서 1 tick만 실행 |
| Runtime Blackboard View | 실행 중 Blackboard 값 표시 |
| Last Active Path | 마지막 실행 경로 하이라이트 |

### 예상 파일

| 파일 | 작업 |
|------|------|
| `Runtime/BTNode.cs` | Breakpoint/Disabled 플래그 또는 에디터 전용 메타 분리 |
| `Runtime/BehaviorTreeRunner.cs` | Pause on breakpoint, Step Tick |
| `Runtime/BehaviorTreeDebugTrace.cs` | trace record 저장 |
| `Editor/BehaviorTreeNodeView.cs` | Breakpoint/Disabled UI 표시 |
| `Editor/BehaviorTreeEditorWindow.cs` | Debug Toolbar 확장 |
| `Editor/BehaviorTreeBlackboardView.cs` | Runtime 값 표시 모드 추가 |

### 완료 조건

- 노드 우클릭 또는 Inspector에서 Breakpoint를 켜고 끌 수 있다.
- Breakpoint 노드가 시작되면 Runner가 Pause 상태가 된다.
- Disable Node는 Validate에서 Warning으로 표시되고 런타임에서 해당 노드를 건너뛴다.
- Debug Trace를 통해 최근 tick 결과를 에디터에서 확인할 수 있다.

---

## Phase R6: Error Window와 Validator 고도화

### 목표

레퍼런스의 실시간 오류 탐지에 가까운 검증 경험을 제공한다.

### 추가 검증 항목

| 검증 | 오류 수준 |
|------|-----------|
| Root 없음 | Error |
| Composite 자식 없음 | Error |
| Decorator 자식 수가 1이 아님 | Error |
| Action/Condition에 자식 연결 | Error 또는 자동 차단 |
| 순환 참조 | Error |
| 끊어진 GUID 참조 | Error |
| Blackboard Key 누락 | Error |
| 같은 자식을 여러 부모가 참조 | Error |
| Disabled Node가 필수 경로를 끊음 | Warning |
| Conditional Abort 대상 조건 없음 | Warning |

### 에디터 UX

| 기능 | 설명 |
|------|------|
| 오류 클릭 이동 | 오류 항목 클릭 시 해당 노드 선택 및 프레임 |
| 노드 위 오류 배지 | 오류가 있는 노드 상단에 표시 |
| 저장 전 경고 | Error가 있으면 Export/Save 전 확인 |
| JSON Import 검증 | Import 직후 자동 Validate |

### 완료 조건

- 잘못된 JSON Import 후 오류 위치를 바로 찾을 수 있다.
- Decorator 연결 오류, 다중 부모 참조, 순환 참조가 모두 검출된다.
- Error가 있는 그래프를 Play Mode에서 실행할 때 명확한 경고를 출력한다.

---

## 권장 구현 순서

| 순서 | Phase | 이유 |
|------|-------|------|
| 1 | R1 Runner 실행 제어 | Pause/Manual Tick/Restart가 있어야 디버깅과 테스트가 쉬워진다 |
| 2 | R2 Composite Abort/Reset | 현재 런타임 안정성의 기반이다 |
| 3 | R4 Decorator 확장 | 공식 Decorator 기준을 충족하고 그래프 표현력이 커진다 |
| 4 | R5 Debug Trace/Breakpoint | Conditional Abort 구현 전 관찰 도구가 필요하다 |
| 5 | R3 Conditional Abort | 가장 중요하지만 디버깅 난도가 높아 기반 기능 이후 진행한다 |
| 6 | R6 Error Window 고도화 | 기능 안정화 후 UX를 정리한다 |

Conditional Abort는 중요하지만, 바로 구현하면 문제 원인 추적이 어렵다. 먼저 Runner 제어와 Debug Trace를 갖춘 뒤 도입하는 것이 안전하다.

---

## 최소 테스트 그래프

### Runner 제어 테스트

```
Root Sequence
├── Log("Start")
├── Wait(3.0)
└── Log("Done")
```

검증:

- Wait 중 Pause하면 시간이 진행되어도 완료되지 않는다.
- Resume하면 남은 시간부터 이어진다.
- Restart When Complete가 켜져 있으면 Done 이후 다시 Start가 실행된다.

### Parallel Abort 테스트

```
Root Parallel(requireAllSuccess = true)
├── Wait(5.0)
└── ReturnFailureAfter(1.0)
```

검증:

- 1초 뒤 Parallel이 Failure가 된다.
- 5초 Wait 노드에 Abort가 호출된다.

### Conditional Abort 테스트

```
Root Selector(AbortType = LowerPriority)
├── Sequence_Combat
│   ├── HasTarget
│   └── Log("Combat")
└── Sequence_Patrol
    ├── Wait(10.0)
    └── Log("Patrol")
```

검증:

- Patrol Wait 중 `HasTarget`이 true가 되면 Patrol이 Abort되고 Combat가 실행된다.

---

## 구현 시 주의 사항

- `BTNode`에 런타임 상태를 추가할 때 에셋 원본이 아니라 `CloneRuntime()` 결과에만 상태가 남도록 유지한다.
- Pause는 Abort와 다르다. Pause에서는 `OnAbort()`를 호출하지 않는다.
- Restart는 Stop/Start와 같지 않다. `Reset Values On Restart` 옵션에 따라 Blackboard 초기화 여부가 달라져야 한다.
- Conditional Abort는 모든 tick마다 전체 트리를 무작정 재평가하면 비용과 예측 가능성이 나빠진다. Composite 단위 재평가 목록을 두는 방식이 적합하다.
- Decorator는 자식이 2개 이상 연결되지 않도록 에디터와 Validator 양쪽에서 막는다.
- 기존 Enemy AI와 BT Runner가 같은 프리팹에서 동시에 State 전환을 시도하지 않도록 테스트 프리팹을 분리한다.

---

## 완료 기준

다음 조건을 만족하면 Behavior Designer 레퍼런스를 기준으로 “기본 운용 가능한 커스텀 BT” 단계로 볼 수 있다.

1. Runner가 `Start`, `Stop`, `Pause`, `Resume`, `Restart`, `Manual Tick`을 지원한다.
2. Composite와 Decorator가 Running 자식의 Abort/Reset을 안정적으로 처리한다.
3. Decorator는 공식 문서 기준처럼 단일 자식의 결과 또는 흐름을 변경하는 노드 세트를 제공한다.
4. Conditional Abort가 `Self`, `LowerPriority`, `Both` 기준으로 동작한다.
5. Breakpoint, Disable Node, Debug Trace로 Play Mode 문제를 추적할 수 있다.
6. Validator가 구조 오류, Blackboard 오류, 연결 오류를 명확하게 표시한다.
7. `BT_EnemyGroundBasic_Test.json`을 Import한 그래프에서 Patrol, Chase, Attack, Retreat 흐름을 디버깅할 수 있다.

