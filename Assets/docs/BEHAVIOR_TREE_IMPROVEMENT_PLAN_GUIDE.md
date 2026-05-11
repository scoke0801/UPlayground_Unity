# Behavior Tree 개선 방안 가이드

> 작성일: 2026-04-28
> 대상 버전: Unity 6 (6000.0.60f1), URP
> 적용 범위: 커스텀 BT 런타임/에디터 신규 구현 제안. 기존 `EnemyBrain`, `EnemyFlyingBrain`, Enemy State 구조는 직접 수정하지 않는다.
>
> **2026-05-11 갱신**: 본 문서의 1차 BT 신규 구현 계획은 상당 부분 적용되었다. 그 이후의 AAA 비교 분석 및 EnemyBrain과의 통합 방향 검토는 `BEHAVIOR_TREE_AAA_REFERENCE_ANALYSIS.md`로 이어진다. 누락 기능 보강은 `BEHAVIOR_TREE_REFERENCE_GAP_IMPLEMENTATION_GUIDE.md` + AAA 분석 문서 §9 참고.

---

## 개요

Behavior Designer Pro 3와 유사한 사용성을 목표로, UPlayground 전용 커스텀 Behavior Tree(BT) 런타임과 노드 에디터를 별도 시스템으로 구현하기 위한 설계 제안이다.

핵심 방향은 다음과 같다.

- 현재 프로젝트 AI 구조를 즉시 대체하지 않고, 완성된 BT 시스템을 별도 네임스페이스/컴포넌트로 먼저 구현한다.
- 기존 `EnemyBrain`, `EnemyFlyingBrain`, `EnemyDetection`, `EnemyTacticalMemory`, `EnemyCombat`, `ActorMovementController`는 초기 BT 구현 단계에서 읽기 전용 레퍼런스로 둔다.
- 런타임은 ScriptableObject 기반 BT 에셋 + MonoBehaviour 실행 컴포넌트로 시작하고, 에디터는 Unity UI Toolkit/GraphView 기반 노드 에디터로 구현한다.
- DOTS/Burst 구조는 1차 목표가 아니다. Behavior Designer Pro 3의 DOTS 구조는 장기 확장 참고로만 둔다.
- 기존 Enemy AI 전환은 BT 런타임, 디버거, 검증 도구, 최소 1종 테스트 몬스터가 모두 안정화된 뒤 별도 단계에서 진행한다.

---

## 웹 조사 요약

### Behavior Designer Pro 3

조사 기준 레퍼런스:

| 출처 | 확인한 내용 |
|------|-------------|
| [Unity Marketplace - Behavior Designer Pro](https://marketplace.unity.com/packages/tools/visual-scripting/behavior-designer-pro-dots-powered-behavior-trees-298743) | DOTS Powered Behavior Trees, Burst/Job System 키워드, Unity 2022.3.11 기준 호환, URP 호환 |
| [Opsive Behavior Designer Pro Overview](https://opsive.com/support/documentation/behavior-designer-pro/overview/) | 그래프 영역, Inspector 패널, Operation Toolbar, Debug Toolbar, Start 이벤트 루트, Task 생성/연결/정렬, Shared Variable 패널, Subtree export |
| [Opsive Conditional Aborts](https://opsive.com/support/documentation/behavior-designer-pro/conditional-aborts) | `None`, `Self`, `Lower Priority`, `Both` abort 타입. 조건 노드 재평가와 실행 중 Action 중단 |
| [Opsive Entity Task](https://opsive.com/support/documentation/behavior-designer-pro/new-tasks/entity-task/) | DOTS Task는 Authoring Object, Component Struct, System Logic, optional Job/Reevaluation Logic으로 분리 |
| [Opsive GameObject Task](https://opsive.com/support/documentation/behavior-designer-pro/new-tasks/gameobject-task/) | GameObject 기반 Action/Conditional Task 확장, Conditional Abort용 재평가 API |

Behavior Designer Pro 3에서 참고할 핵심 UX는 다음이다.

| 기능 | UPlayground 적용 판단 |
|------|-----------------------|
| 그래프 편집 영역 | 필수. BT 자산의 주 편집 화면 |
| 노드 Inspector | 필수. 선택 노드의 직렬화 필드 편집 |
| Shared Variable / Blackboard | 필수. 타겟, 거리, 쿨다운, 플래그, 임시 값 공유 |
| Debug Toolbar | 필수. Play Mode에서 실행 노드, 상태, tick 순서 표시 |
| Subtree | 2차 필수. 공통 패턴 재사용용 |
| Conditional Abort | 2차 필수. 현재 Enemy AI의 반응형 판단을 BT로 옮길 때 필요 |
| DOTS Entity Task | 장기 과제. 현재 프로젝트는 KCC, Animancer, GameObject 컴포넌트 의존도가 높으므로 초기 구현에는 부적합 |

### 유사 커스텀 BT 구현 사례

| 사례 | 특징 | 참고 포인트 |
|------|------|-------------|
| [UniBT](https://github.com/yoshidan/UniBT) | GraphView 기반 무료 Behavior Tree 디자이너. 런타임 active node 시각화. Action/Conditional/Composite 확장 | 작게 시작하는 GraphView BT 에디터 구조 |
| [AkiBT](https://github.com/AkiKurisu/AkiBT) | UniBT 기반 확장. GraphView 시각 노드 에디터, 런타임 노드 상태 디버깅, 그래프에서 필드 직접 편집 | 노드 자체에 필드를 노출하는 UX, 런타임 디버깅 |

공통적으로 성공적인 커스텀 BT는 다음 경계를 분리한다.

```
Editor Graph
    └── BT Asset 편집, 노드 배치, 연결, 필드 수정, 검증

Serialized Asset
    └── 노드 GUID, 좌표, 부모/자식 관계, 노드별 파라미터 저장

Runtime Tree
    └── 에셋을 실행 가능한 노드 인스턴스로 변환, tick/update, 상태 캐시

Agent Context
    └── GameObject 컴포넌트 참조, Blackboard, 상태 전환 API, 전투 API 접근
```

---

## 현재 프로젝트 AI 구조

현재 AI는 BT가 아니라 C# Brain + State Machine 조합이다.

```
MonsterActor
├── EnemyBrain / EnemyFlyingBrain
├── EnemyDetection
├── EnemyTacticalMemory
├── EnemyCombat
└── ActorMovementController
        └── EnemyActorState / EnemyFlying State
```

### 확인된 핵심 클래스

| 클래스 | 현재 역할 | BT 도입 시 관계 |
|--------|----------|----------------|
| `EnemyBrain` | 지상 몬스터 의사결정. 감지, 거리, 플레이어 상태, 공격 쿨다운, 그룹 슬롯 기반으로 State 전환 | 초기에는 수정하지 않는다. 이후 BT Action/Condition 노드 설계의 기능 레퍼런스 |
| `EnemyFlyingBrain` | 비행 몬스터 루프. 지상 추격/공격, 이륙, 공중 선회, 투사체, 급강하/착지 | 초기에는 별도. 비행 BT는 지상 BT 안정화 이후 |
| `EnemyDetection` | 타겟 획득, 시야각, 차폐, 타겟 유효성, 아군 탐지 | BT Blackboard/Condition 노드에서 읽을 후보 |
| `EnemyTacticalMemory` | 플레이어 상태 관찰, 공격/후퇴/피격/가드 기록, 회피/가드 빈도 | BT Blackboard/Condition 노드에서 읽을 후보 |
| `EnemyCombat` | 스킬 선택, 공격 데이터, 현재 스킬 실행 | BT Action 노드에서 호출할 후보 |
| `ActorMovementController` | 현재 State 보관 및 `TransitionToState` 수행 | BT Action 노드가 최종적으로 호출할 이동/상태 전환 API |
| `EnemyBehaviorSO` | 지상 AI 거리/확률/페이즈 튜닝 | BT 전환 전까지 유지. 이후 BT Blackboard 기본값 또는 BT Asset 생성 입력으로 변환 가능 |
| `BehaviorPhase` | HP 구간별 행동 확률/옵션 | BT Decorator 또는 Blackboard Override 구조로 이전 가능 |

### 현재 구조에서 바로 바꾸면 위험한 지점

| 위험 | 이유 |
|------|------|
| `EnemyBrain.MakeDecision()` 즉시 대체 | 공격 슬롯, 전술 기억, 피격/공중/잡힘 상태 예외, 그룹 경보가 한 메서드 흐름에 섞여 있음 |
| State 클래스 직접 BT 노드화 | KCC 콜백과 상태 생명주기가 강하게 결합되어 있어 BT tick과 물리 update 타이밍 충돌 가능 |
| `EnemyBehaviorSO`를 바로 BT Asset으로 교체 | 현재 데이터 에셋과 몬스터 프리팹 연결이 이미 존재하므로 회귀 범위가 큼 |
| DOTS 기반 선도입 | KCC/Animancer/GameObject 컴포넌트 의존 액션이 대부분이라 초기 비용 대비 이득이 낮음 |

따라서 1차 목표는 “기존 AI와 병렬로 존재하는 BT 시스템 완성”이다.

---

## 목표 아키텍처

```
Assets/02.Scripts/AI/BehaviorTree/
├── Runtime/
│   ├── BTStatus.cs
│   ├── BTNode.cs
│   ├── BTCompositeNode.cs
│   ├── BTDecoratorNode.cs
│   ├── BTActionNode.cs
│   ├── BTConditionNode.cs
│   ├── BehaviorTreeAsset.cs
│   ├── BehaviorTreeRunner.cs
│   ├── BehaviorTreeContext.cs
│   └── Blackboard/
│       ├── Blackboard.cs
│       ├── BlackboardKey.cs
│       └── BlackboardValue.cs
│
├── Nodes/
│   ├── Composite/
│   │   ├── SequenceNode.cs
│   │   ├── SelectorNode.cs
│   │   └── ParallelNode.cs
│   ├── Decorator/
│   │   ├── InverterNode.cs
│   │   ├── CooldownNode.cs
│   │   └── RepeatNode.cs
│   ├── Condition/
│   │   ├── HasTargetNode.cs
│   │   ├── IsTargetInRangeNode.cs
│   │   └── IsCurrentActorStateNode.cs
│   └── Action/
│       ├── WaitNode.cs
│       ├── LogNode.cs
│       └── SetBlackboardValueNode.cs
│
└── Editor/
    ├── BehaviorTreeEditorWindow.cs
    ├── BehaviorTreeGraphView.cs
    ├── BehaviorTreeNodeView.cs
    ├── BehaviorTreeInspectorView.cs
    ├── BehaviorTreeBlackboardView.cs
    └── BehaviorTreeAssetValidator.cs
```

### 런타임 데이터 흐름

```
BehaviorTreeRunner (MonoBehaviour)
    │
    ├── BehaviorTreeAsset 참조
    ├── Runtime Tree 인스턴스 생성
    ├── BehaviorTreeContext 생성
    │       ├── Owner GameObject
    │       ├── Transform
    │       ├── Blackboard
    │       └── Component Cache
    │
    └── Update 주기마다 Root.Tick(context)
            └── BTStatus: Success / Failure / Running
```

### 에디터 데이터 흐름

```
BehaviorTreeEditorWindow
    ├── Toolbar
    │   ├── BT Asset 선택
    │   ├── Save
    │   ├── Validate
    │   └── Play Mode Debug Target 선택
    │
    ├── GraphView
    │   ├── 노드 생성
    │   ├── 연결 생성/삭제
    │   ├── 자식 순서 정렬
    │   └── 런타임 상태 색상 표시
    │
    ├── Inspector
    │   └── 선택 노드 SerializedObject 편집
    │
    └── Blackboard
        └── Key 생성/삭제/기본값 편집
```

---

## 핵심 런타임 설계

### BTStatus

```csharp
namespace UPlayGround.AI.BehaviorTree
{
    public enum BTStatus
    {
        Success,
        Failure,
        Running
    }
}
```

초기 상태는 세 가지로 충분하다. `Inactive`, `Aborted`는 디버거/Conditional Abort 구현 시 추가한다.

### BTNode

| API | 역할 |
|-----|------|
| `Initialize(BehaviorTreeContext context)` | 런타임 캐시 준비 |
| `OnStart()` | 노드가 처음 실행될 때 호출 |
| `OnUpdate()` | 매 tick 실행 |
| `OnStop()` | Success/Failure/Abort로 종료될 때 호출 |
| `Abort()` | Running 노드를 중단 |

중요한 점은 ScriptableObject 노드 에셋을 그대로 실행 상태로 쓰지 않는 것이다. 같은 BT Asset을 여러 몬스터가 공유할 수 있어야 하므로 런타임 상태는 Runner별 인스턴스에 둔다.

### BehaviorTreeAsset

| 필드 | 설명 |
|------|------|
| `rootNodeGuid` | 루트 노드 GUID |
| `nodes` | 직렬화된 노드 목록 |
| `blackboardSchema` | Key와 기본값 목록 |
| `editorGraphState` | 노드 좌표, 접힘 상태, 선택 상태 등 에디터 전용 정보 |

Unity 직렬화 안정성을 위해 노드는 `ScriptableObject` 서브에셋으로 저장하는 방식을 우선 검토한다. GraphView 좌표와 연결 정보는 각 노드의 `guid`, `position`, `children`으로 보관한다.

### BehaviorTreeRunner

| 필드 | 설명 |
|------|------|
| `_treeAsset` | 실행할 BT Asset |
| `_tickInterval` | tick 간격. 초기 기본값 0.1초 |
| `_startOnEnable` | 활성화 시 자동 시작 |
| `_debugMode` | Play Mode 디버그 정보 수집 여부 |

현재 프로젝트의 `EnemyBrain`도 `_decisionInterval = 0.1f`를 사용하므로, BT 기본 tick도 0.1초로 시작하면 기존 AI 리듬과 비교하기 쉽다.

---

## Blackboard 설계

Behavior Designer Pro의 Shared Variable 개념을 UPlayground에서는 Blackboard로 구현한다.

### 권장 Key 타입

| 타입 | 용도 |
|------|------|
| `Bool` | 타겟 보유, 공격 가능, 가드 가능 |
| `Int` | 연속 공격 횟수, 페이즈 인덱스 |
| `Float` | 거리, 쿨다운, HP 비율 |
| `String` | 현재 Actor State 이름 |
| `Vector3` | 순찰 지점, 마지막 타겟 위치 |
| `Transform` | 현재 타겟 |
| `GameObject` | 소유자, 타겟 오브젝트 |
| `ScriptableObject` | 공격 데이터, 설정 에셋 |

### UPlayground 전용 Context Cache

초기 Enemy 연동 노드를 만들 때는 Blackboard보다 Context Cache를 우선 사용한다.

| 캐시 | 읽기/쓰기 |
|------|-----------|
| `EnemyDetection` | 읽기 |
| `EnemyTacticalMemory` | 읽기/이벤트 기록 |
| `EnemyCombat` | 읽기/공격 선택 |
| `ActorMovementController` | State 전환 |
| `MonsterActor` | 생존/스탯/타입 확인 |

단, 이 연동 노드는 BT 시스템 완성 이후 별도 단계에서 추가한다. 1차 런타임 검증은 `Wait`, `Log`, `Sequence`, `Selector`, `Blackboard` 노드만으로 진행한다.

---

## 에디터 기능 설계

### 1차 필수 기능

| 기능 | 설명 |
|------|------|
| BT Asset 생성 | `Create/UPlayGround/AI/Behavior Tree` |
| Editor Window | `UPlayGround/AI/Behavior Tree Editor` |
| GraphView | 노드 생성, 삭제, 드래그, 연결 |
| Inspector | 선택 노드 필드 편집 |
| Blackboard 패널 | Key 추가/삭제/기본값 편집 |
| Save/Validate | 루트 누락, 순환 참조, 자식 개수 오류 검증 |
| Play Mode Debug | 실행 중 노드 상태 색상 표시 |

### 노드 색상 기준

| 상태 | 표시 |
|------|------|
| `Running` | 노란색 테두리 |
| `Success` | 초록색 테두리 |
| `Failure` | 빨간색 테두리 |
| `Inactive` | 기본 회색 |
| `Aborted` | 주황색 |

### 검증 규칙

| 규칙 | 오류 수준 |
|------|----------|
| 루트 노드 없음 | Error |
| 루트가 둘 이상 | Error |
| Composite 자식 없음 | Error |
| Decorator 자식이 0개 또는 2개 이상 | Error |
| 순환 참조 | Error |
| 끊어진 노드 | Warning |
| Blackboard Key 참조 누락 | Error |
| 동일 sibling order 중복 | Warning |

---

## Conditional Abort 도입 방안

Behavior Designer Pro의 Conditional Abort는 반응형 AI를 만들 때 중요하다. 다만 초기 런타임에 바로 넣으면 구현 난도가 크게 오른다.

### 단계별 도입

| 단계 | 내용 |
|------|------|
| 1단계 | 일반 BT tick. Root부터 매 tick 평가 |
| 2단계 | Running 노드 유지. Sequence/Selector가 실행 위치를 캐시 |
| 3단계 | Condition 노드 상태 캐시와 재평가 목록 수집 |
| 4단계 | `Self`, `Lower Priority`, `Both` Abort 구현 |
| 5단계 | Abort 디버그 표시와 OnAbort 콜백 구현 |

### Abort 타입 정의

| 타입 | 의미 |
|------|------|
| `None` | 재평가 없음 |
| `Self` | 현재 브랜치 내부 Running 노드를 중단 |
| `LowerPriority` | 오른쪽 낮은 우선순위 브랜치를 중단 |
| `Both` | Self + LowerPriority |

UPlayground Enemy AI에서 Abort가 필요한 대표 사례는 다음이다.

| 상황 | 예상 Abort |
|------|------------|
| Patrol 중 타겟 발견 | LowerPriority |
| Chase 중 피격 | LowerPriority |
| Attack 준비 중 타겟 사망 | Self |
| Circle 중 플레이어가 경직 상태 | LowerPriority |
| Retreat 중 플레이어가 매우 가까워짐 | Self |

---

## UPlayground 적용 로드맵

### 구현 상태

| Phase | 상태 | 비고 |
|-------|------|------|
| Phase 0 | 완료 | `Assets/02.Scripts/AI/BehaviorTree/` 신규 경로로 분리. 기존 Enemy AI 파일 미수정 |
| Phase 1 | 완료 | 순수 BT 런타임, Blackboard, 기본 Composite/Decorator/Action/Condition 노드 구현 |
| Phase 2 | 완료 | `UPlayGround/AI/Behavior Tree Editor` GraphView 에디터, Inspector, Blackboard, Validator, Play Mode 상태 표시 구현 |
| Phase 3 | 진행 중 | Enemy 컴포넌트 연동 노드와 JSON Import/Export, 지상 Enemy 기본 테스트 JSON 추가 |
| Phase 4 | 미진행 | 기존 AI와 병렬 검증 전 |
| Phase 5 | 미진행 | Enemy 프리팹 마이그레이션 전 |

### Phase 0: 기존 AI 동결

목표: 기존 Enemy AI 동작을 변경하지 않는 별도 개발 영역을 확정한다.

| 작업 | 설명 |
|------|------|
| 기존 AI 수정 금지 | `EnemyBrain`, `EnemyFlyingBrain`, Enemy State, `EnemyBehaviorSO` 직접 변경 없음 |
| 신규 경로 분리 | `Assets/02.Scripts/AI/BehaviorTree/` 아래 신규 작성 |
| 네임스페이스 분리 | `UPlayGround.AI.BehaviorTree` |
| 테스트 대상 분리 | 새 테스트 프리팹 또는 빈 GameObject에서 BT Runner 검증 |

#### 작업 단위

| 단위 | 작업 | 산출물 |
|------|------|--------|
| 0-1 | 신규 폴더 구조 생성 | `Runtime`, `Nodes`, `Editor` 폴더 |
| 0-2 | 네임스페이스/asmdef 정책 결정 | Runtime/Editor 분리 기준 |
| 0-3 | 테스트 전용 씬 또는 프리팹 기준 결정 | BT 검증용 빈 GameObject 또는 테스트 Actor |
| 0-4 | 기존 AI 보호 규칙 문서화 | 기존 Brain/State 직접 수정 금지 체크리스트 |

#### 완료 조건

- 신규 BT 파일이 기존 Enemy AI 파일과 섞이지 않는다.
- 기존 몬스터 프리팹과 `EnemyBrain` 동작에 변경이 없다.
- 이후 Phase에서 사용할 테스트 대상이 명확하다.

### Phase 1: 순수 BT 런타임

목표: Enemy 컴포넌트에 의존하지 않는 순수 BT 실행기를 만든다.

| 구현 | 완료 기준 |
|------|----------|
| `BTStatus`, `BTNode` 계층 | `Sequence`, `Selector`, `Wait`, `Log` 실행 |
| `BehaviorTreeAsset` | Unity 에셋으로 저장/로드 가능 |
| `BehaviorTreeRunner` | Play Mode에서 tick 실행 |
| Blackboard 기본형 | bool/int/float/string/vector/object 저장 |

이 단계에서는 Enemy 컴포넌트에 접근하지 않는다.

#### 작업 단위

| 단위 | 작업 | 산출물 |
|------|------|--------|
| 1-1 | 상태/기본 타입 정의 | `BTStatus`, `BTAbortType`, 노드 GUID 정책 |
| 1-2 | 노드 베이스 구현 | `BTNode`, `BTCompositeNode`, `BTDecoratorNode`, `BTActionNode`, `BTConditionNode` |
| 1-3 | 실행 컨텍스트 구현 | `BehaviorTreeContext`, owner/cache/blackboard 접근 API |
| 1-4 | BT 에셋 모델 구현 | `BehaviorTreeAsset`, root guid, node list, editor position 저장 |
| 1-5 | 런타임 인스턴스 분리 | 에셋 노드와 실행 상태를 분리하는 clone/instance 구조 |
| 1-6 | 기본 Composite 구현 | `SequenceNode`, `SelectorNode` |
| 1-7 | 기본 Decorator 구현 | `InverterNode`, `CooldownNode` |
| 1-8 | 기본 Action 구현 | `WaitNode`, `LogNode`, `SetBlackboardValueNode` |
| 1-9 | Blackboard 구현 | Key schema, 기본값, 런타임 값 복사 |
| 1-10 | Runner 구현 | `BehaviorTreeRunner` tick interval, start/stop/restart |

#### 완료 조건

- Play Mode에서 `Sequence(Wait -> Log)`가 정상 실행된다.
- 같은 `BehaviorTreeAsset`을 두 Runner가 공유해도 런타임 상태가 섞이지 않는다.
- Runner 비활성화/재활성화 시 Running 노드가 올바르게 정리된다.
- Unity 재시작 후 BT Asset의 노드/연결/Blackboard 기본값이 유지된다.

#### 제외 범위

- GraphView 에디터
- Enemy 컴포넌트 연동 노드
- Conditional Abort
- Subtree

### Phase 2: GraphView 에디터

목표: BT Asset을 시각적으로 만들고 저장할 수 있는 에디터를 완성한다.

| 구현 | 완료 기준 |
|------|----------|
| Editor Window | BT Asset 열기/저장 |
| Node View | 생성/삭제/이동/연결 |
| Inspector | 선택 노드 필드 편집 |
| Validator | 오류 표시 |
| Runtime Debug | Play Mode에서 Running/Success/Failure 시각화 |

이 단계가 끝나야 “커스텀 BT 에디터 구조”의 기본 완성으로 본다.

#### 작업 단위

| 단위 | 작업 | 산출물 |
|------|------|--------|
| 2-1 | 에디터 창 생성 | `BehaviorTreeEditorWindow`, 메뉴 `UPlayGround/AI/Behavior Tree Editor` |
| 2-2 | GraphView 기본 구성 | 줌, 드래그, 선택, Grid Background |
| 2-3 | NodeView 구현 | 노드 타입별 포트, 제목, 상태 표시 |
| 2-4 | 노드 생성 메뉴 | Space/right click 기반 노드 생성 |
| 2-5 | 연결 저장/삭제 | 부모/자식 관계와 sibling order 저장 |
| 2-6 | Inspector 패널 | 선택 노드 SerializedObject 필드 편집 |
| 2-7 | Blackboard 패널 | Key 추가/삭제/기본값 편집 |
| 2-8 | Asset 저장/로드 | 노드 좌표, 연결, 필드 값 유지 |
| 2-9 | Validator | 루트, 순환, 자식 수, 끊어진 노드 검증 |
| 2-10 | Play Mode Debug 표시 | Running/Success/Failure 색상 반영 |

#### 완료 조건

- 에디터에서 새 BT Asset을 만들고 `Sequence -> Wait -> Log` 그래프를 구성할 수 있다.
- Unity 에디터를 재시작해도 그래프 배치와 연결이 유지된다.
- 잘못된 그래프는 저장 전 또는 Validate 시 명확한 오류를 표시한다.
- Play Mode에서 실행 중인 노드 상태가 그래프에 표시된다.

#### 제외 범위

- 고급 검색/히스토리/북마크
- Subtree export/import
- 노드별 커스텀 UI 고도화

### Phase 3: GameObject 연동 노드

목표: BT가 UPlayground GameObject 컴포넌트를 읽고, 테스트 Actor에서 상태 전환을 지시할 수 있게 한다.

| 노드 | 역할 |
|------|------|
| `HasTargetNode` | `EnemyDetection.HasTarget` 확인 |
| `IsTargetInRangeNode` | `EnemyDetection.DistanceToTarget` 비교 |
| `IsCurrentActorStateNode` | `ActorMovementController.CurrentState.StateName` 비교 |
| `TransitionEnemyStateNode` | 지정 Enemy State로 전환 |
| `CanUseEnemySkillNode` | `EnemyCombat` 쿨다운/거리 조건 확인 |
| `ExecuteEnemyAttackNode` | 현재 선택 스킬 기반 공격 State 진입 |

이 단계에서도 기존 Brain을 수정하지 않는다. 별도 테스트 몬스터에 `BehaviorTreeRunner`만 붙여서 검증한다.

#### 구현된 연동 노드

| 노드 | 역할 |
|------|------|
| `HasTargetNode` | `EnemyDetection.HasTarget` 확인 |
| `IsTargetInRangeNode` | `EnemyDetection.DistanceToTarget` 거리 비교 |
| `IsCurrentActorStateNode` | `ActorMovementController.CurrentState.StateName` 확인 |
| `CanUseEnemySkillNode` | `EnemyCombat.HasAvailableSkillAtDistance()` 확인 |
| `IsEnemyPatrolEnabledNode` | `EnemyBrain.EnablePatrol` 확인 |
| `SyncEnemyBlackboardNode` | 타겟, 거리, 현재 State를 Blackboard에 복사 |
| `TransitionEnemyStateNode` | 테스트용 `Idle`, `Patrol`, `Chase`, `Attack`, `Retreat` State 전환 |
| `ExecuteEnemyAttackNode` | 사용 가능한 스킬이 있을 때 `EnemyAttackState` 진입 |

`TransitionEnemyStateNode`와 `ExecuteEnemyAttackNode`는 `Death`, `Hit`, `Grabbed`, `Airborne` 등 개입 금지 State에서는 전환을 시도하지 않는다.

#### JSON Import / Export

BT 에셋은 JSON으로 내보내고 다시 에셋으로 가져올 수 있다.

| 메뉴 | 기능 |
|------|------|
| `UPlayGround/AI/Behavior Tree Json/Export Selected` | 선택한 `BehaviorTreeAsset`을 JSON 파일로 저장 |
| `UPlayGround/AI/Behavior Tree Json/Import Json` | JSON 파일을 선택해 `BehaviorTreeAsset`으로 생성 |

JSON에는 다음 정보가 저장된다.

| 항목 | 내용 |
|------|------|
| `rootGuid` | 루트 노드 GUID |
| `blackboard` | Key, 타입, 기본값 |
| `nodes` | 노드 타입, GUID, 표시 이름, 주석, 위치, 자식 GUID 목록 |
| `properties` | 노드별 직렬화 필드 값 |

현재 테스트 데이터:

| 파일 | 설명 |
|------|------|
| `Assets/10.Datas/AI/BehaviorTree/Json/BT_EnemyGroundBasic_Test.json` | 현재 지상 `EnemyBrain` 기본 흐름을 테스트용 BT JSON으로 옮긴 데이터 |

이 JSON은 다음 흐름을 담는다.

```
Root Selector
├── Combat_HasTarget
│   ├── Sync_EnemyBlackboard
│   ├── HasTarget
│   └── Combat_Decision
│       ├── Attack_WhenSkillAvailable
│       ├── Retreat_WhenTooClose
│       ├── Chase_WhenFar
│       └── CombatIdle
└── NonCombat_NoTarget
    ├── NoTarget
    └── NonCombat_Decision
        ├── Patrol_WhenEnabled
        └── Idle_NoPatrol
```

#### 작업 단위

| 단위 | 작업 | 산출물 |
|------|------|--------|
| 3-1 | Component Cache 확장 | `BehaviorTreeContext.GetComponentCached<T>()` |
| 3-2 | 읽기 전용 Condition 노드 | `HasTargetNode`, `IsTargetInRangeNode`, `IsCurrentActorStateNode` |
| 3-3 | Blackboard Sync 노드 | 타겟 Transform, 거리, 현재 State 이름 저장 |
| 3-4 | 테스트용 이동 Action | 기존 Enemy State를 직접 건드리지 않는 단순 Transform 이동 또는 Log 기반 대체 액션 |
| 3-5 | Enemy 연동 Action 초안 | `TransitionEnemyStateNode` 등은 테스트 프리팹 전용으로 제한 |
| 3-6 | 안전 가드 추가 | Death/Hit/Grabbed 등 개입 금지 State 체크 |
| 3-7 | 디버그 로그 정리 | BT 노드 실행과 State 전환 요청 추적 |

#### 완료 조건

- 기존 Brain이 붙지 않은 테스트 오브젝트에서 Detection 기반 조건 노드가 동작한다.
- `BehaviorTreeRunner`가 없는 기존 몬스터에는 영향이 없다.
- Enemy State 전환 Action은 테스트 프리팹에서만 검증된다.
- 개입 금지 State에서는 BT Action이 State 전환을 시도하지 않는다.

#### 제외 범위

- 기존 몬스터 프리팹 교체
- 기존 `EnemyBehaviorSO` 자동 변환
- 비행 몬스터 BT화

### Phase 4: 기존 AI와 병렬 검증

목표: 기존 AI와 BT AI를 같은 조건에서 비교해 회귀 위험을 줄인다.

| 검증 | 방법 |
|------|------|
| 동일 몬스터 2종 비교 | 기존 Brain 프리팹과 BT Runner 프리팹을 분리 |
| 전투 리듬 비교 | 공격 빈도, 후퇴 빈도, 추격 안정성 기록 |
| 디버그 로그 비교 | BT tick trace와 State 전환 로그 비교 |
| 회귀 확인 | 피격, 사망, Grabbed, Airborne, Group Slot 예외 확인 |

#### 작업 단위

| 단위 | 작업 | 산출물 |
|------|------|--------|
| 4-1 | 비교 프리팹 구성 | 기존 Brain 버전, BT Runner 버전 |
| 4-2 | 기본 지상 BT 작성 | Patrol/Idle, HasTarget, Chase, Attack, Retreat 흐름 |
| 4-3 | 실행 로그 포맷 통일 | 시간, 노드, 상태, 타겟 거리, 결과 |
| 4-4 | 전투 리듬 측정 | 공격 간격, 공격 성공/실패, 후퇴/선회 빈도 |
| 4-5 | 예외 케이스 테스트 | Death, Hit, Grabbed, Airborne, 타겟 소실 |
| 4-6 | Conditional Abort 필요 지점 확정 | Patrol 중 타겟 발견, Chase 중 피격 등 |
| 4-7 | 개선 목록 작성 | BT 노드 추가/수정 후보 |

#### 완료 조건

- 최소 1종 지상 몬스터의 기본 전투 루프가 BT로 재현된다.
- 기존 AI 대비 명확히 부족한 행동이 문서화된다.
- Conditional Abort 없이 해결 가능한 문제와 필요한 문제가 분리된다.
- 기존 AI 프리팹은 계속 정상 동작한다.

### Phase 5: 선택적 마이그레이션

목표: 검증된 범위에서만 일부 Enemy를 BT 기반으로 전환한다.

| 조건 | 설명 |
|------|------|
| BT 런타임 안정화 | Running/Abort/Reset 누수 없음 |
| 에디터 저장 안정화 | Unity 재시작 후 그래프 손상 없음 |
| 최소 Enemy 1종 완전 재현 | Patrol, Chase, Attack, Retreat, Death 흐름 재현 |
| 디버그 가능 | Play Mode에서 현재 실행 노드와 Blackboard 확인 가능 |

이 조건을 만족한 뒤에만 기존 Enemy 프리팹 일부를 BT 기반으로 전환한다.

#### 작업 단위

| 단위 | 작업 | 산출물 |
|------|------|--------|
| 5-1 | 전환 후보 선정 | 단순 지상 근접 몬스터 1종 |
| 5-2 | 프리팹 분기 | 기존 Brain 프리팹 보존, BT 프리팹 별도 생성 |
| 5-3 | `EnemyBehaviorSO` 값 수동 반영 | BT Blackboard 기본값 또는 노드 필드 입력 |
| 5-4 | 플레이 테스트 | 추격, 공격, 피격, 사망, 타겟 소실 |
| 5-5 | 성능 확인 | tick interval, GC allocation, 로그 비용 확인 |
| 5-6 | 전환 기준 문서화 | 어떤 몬스터부터 BT화할지 기준 정리 |
| 5-7 | 기존 Brain 제거 여부 판단 | 충분히 검증된 프리팹에 한해 별도 작업으로 진행 |

#### 완료 조건

- BT 버전 프리팹이 기존 버전과 분리되어 롤백 가능하다.
- 전환 후보 몬스터 1종이 BT만으로 기본 전투를 수행한다.
- Unity Editor에서 BT 그래프를 열어 현재 행동을 추적할 수 있다.
- 기존 AI 제거는 이 Phase의 자동 결과가 아니라 별도 승인 작업으로 남긴다.

---

## 초기 구현 우선순위

### 먼저 만들 것

1. `BehaviorTreeAsset`
2. `BTNode` 런타임 모델
3. `Sequence`, `Selector`, `Wait`, `Log`
4. `BehaviorTreeRunner`
5. GraphView 기반 저장/로드
6. Play Mode 상태 색상 디버그

### 나중에 만들 것

| 기능 | 뒤로 미루는 이유 |
|------|----------------|
| Conditional Abort | 기본 런타임 안정화 전에는 디버깅 비용이 큼 |
| Subtree | 에셋 참조/순환 검증이 필요 |
| Utility Selector | 현재 Enemy AI의 확률 분기 이전 시 유용하지만 1차 필수 아님 |
| DOTS/Burst | 기존 GameObject 컴포넌트 의존도가 높음 |
| 자동 마이그레이션 툴 | 기존 AI 의미 보존이 먼저 필요 |

---

## 권장 파일/메뉴 규칙

| 항목 | 값 |
|------|----|
| Runtime 경로 | `Assets/02.Scripts/AI/BehaviorTree/Runtime/` |
| Node 경로 | `Assets/02.Scripts/AI/BehaviorTree/Nodes/` |
| Editor 경로 | `Assets/02.Scripts/AI/BehaviorTree/Editor/` |
| Data 경로 | `Assets/10.Datas/AI/BehaviorTree/` |
| Create 메뉴 | `Create/UPlayGround/AI/Behavior Tree` |
| Editor 메뉴 | `UPlayGround/AI/Behavior Tree Editor` |
| 네임스페이스 | `UPlayGround.AI.BehaviorTree` |

---

## 주의 사항

- 기존 `EnemyBrain`과 `EnemyFlyingBrain`은 BT 구현이 완성될 때까지 변경하지 않는다.
- BT Runner는 동일 GameObject에서 기존 Brain과 동시에 State 전환을 시도하면 안 된다. 병렬 검증 시에는 프리팹을 분리한다.
- ScriptableObject 노드에 런타임 상태를 직접 저장하지 않는다. 동일 BT Asset을 여러 객체가 공유할 때 상태가 섞인다.
- GraphView는 Editor 전용 API이므로 Runtime asmdef와 Editor asmdef를 분리하는 것이 좋다.
- 노드 연결 순서는 BT 실행 의미와 직접 연결된다. sibling order를 명시적으로 저장한다.
- `TransitionEnemyStateNode` 같은 프로젝트 연동 Action은 가장 마지막 단계에서 추가한다.

---

## 결론

UPlayground에는 이미 `EnemyBrain` 중심의 반응형 AI가 존재하므로, 커스텀 BT는 “즉시 교체용”이 아니라 “완성 후 교체 가능한 독립 AI 저작/디버깅 시스템”으로 구현하는 것이 맞다.

가장 현실적인 1차 목표는 Behavior Designer Pro 3의 전체 기능 복제가 아니라 다음 네 가지다.

1. ScriptableObject 기반 BT Asset
2. GraphView 기반 노드 에디터
3. Blackboard와 기본 Composite/Decorator/Action/Condition 노드
4. Play Mode 런타임 디버거

이 네 가지가 안정화된 뒤에 `EnemyDetection`, `EnemyCombat`, `ActorMovementController` 연동 노드를 추가하고, 마지막에 기존 Enemy AI를 선택적으로 이전한다.
