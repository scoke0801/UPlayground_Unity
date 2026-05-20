# Behavior Tree 코드 정리 후속 계획

> 작성일: 2026-05-20
> 대상: `Assets/02.Scripts/AI/BehaviorTree/` 전체 (Runtime, Editor, Nodes)
> 선행 작업: Editor 대형 4개 파일 partial 분리 완료 (2026-05-20)
> 관련 문서: [BEHAVIOR_TREE_SYSTEM_GUIDE.md](../BEHAVIOR_TREE_SYSTEM_GUIDE.md), [Complete/BEHAVIOR_TREE_EDITOR_IMPROVEMENT_EXECUTION_PLAN.md](../Complete/BEHAVIOR_TREE_EDITOR_IMPROVEMENT_EXECUTION_PLAN.md)

---

## 개요

선행 작업으로 GraphView(1444줄)·EditorWindow(1383줄)·MonsterBehaviorTreeJsonImporter(1134줄)·NodeView(816줄)를 책임별 partial 파일로 분리해 단일 파일 최대 라인 수를 564줄로 축소했다. 동작은 모두 보존된 상태다.

본 문서는 그 다음 단계로 다음 3개 범위의 가독성·중복 정리 계획을 정의한다.

1. Runtime 핵심 파일(`BehaviorTreeRunner`, `Blackboard`, `BTNode`)의 가독성·중복 정리
2. 남은 중대형 Editor 파일(JsonUtility 403줄, InspectorView 381줄, BlackboardView 330줄, AssetValidator 290줄, SearchPanel 274줄)의 책임 정리
3. `Nodes/` 하위 노드에서 반복되는 보일러플레이트 통합

모든 단계의 기본 원칙은 **동작 보존 리팩토링**이다. 직렬화 키, JSON 스키마, BehaviorTreeAsset의 SubAsset 구성, 노드 클래스 이름은 변경하지 않는다.

---

## Phase 진행 상태

| Phase | 상태 | 비고 |
|------|------|-----------|
| C1 Runtime 핵심 정리 | 대기 | Runner/Blackboard/BTNode 가독성 및 중복 |
| C2 중대형 Editor 파일 정리 | 대기 | AssetValidator·JsonUtility·InspectorView·BlackboardView·SearchPanel |
| C3 Nodes 중복 정리 | 대기 | Cooldown 키, GetComponentCached 패턴, Blackboard 접근 헬퍼 |

---

## Phase C1: Runtime 핵심 정리

목표: 런타임 동작에 직접 영향을 주는 3개 파일의 가독성을 높이고 작은 중복을 제거한다. Asset 직렬화·CloneRuntime 흐름은 건드리지 않는다.

### C1.1 `BehaviorTreeRunner.cs` (225줄)

문제점

- `StartTree()`와 `RestartRuntimeTree()`이 거의 동일한 초기화 시퀀스(클론 → DebugTrace 생성 → Context 생성 → RootNode.Initialize → 상태 전환)를 각각 인라인으로 구현해 한쪽만 수정하면 동기화가 깨질 위험이 있다.
- `StopTree()`와 `RestartRuntimeTree()`이 모두 `Abort → DisposeRuntime → null 초기화` 시퀀스를 중복한다.
- 동일 파일에 `BehaviorTreeDebugTraceRecord`(struct)와 `BehaviorTreeDebugTrace`(class)가 함께 정의돼 있어 Runner와 책임이 섞여 있다.

작업 항목

| 작업 | 설명 |
|------|------|
| `BehaviorTreeDebugTrace` 별도 파일 분리 | `Runtime/Debug/BehaviorTreeDebugTrace.cs`로 추출. struct도 같이 이동. Runner는 사용처만 남김. |
| 초기화 시퀀스 추출 | `private void BootRuntimeTree(Blackboard blackboardOverride)` 같은 단일 헬퍼로 모아 `StartTree`/`RestartRuntimeTree`에서 재사용. |
| 종료 시퀀스 추출 | `private void TeardownRuntimeTree()` 헬퍼로 `Abort → Dispose → null` 묶음. `StopTree`/`RestartRuntimeTree` 양쪽에서 호출. |
| `TickOnce(bool allowPaused)` 흐름 분리 | 자동 재시작 조건과 실제 Tick 호출을 가드 함수로 분리. |

위험도: 낮음. Tick 흐름의 외부 가시 동작은 변하지 않는다. 단, Start/Restart 통합 시 `_tickTimer` 초기값 등을 그대로 보존해야 한다.

### C1.2 `Blackboard.cs` (193줄)

문제점

- `TryGetBool`/`TryGetInt`/`TryGetFloat`/`TryGetString`/`TryGetVector3` 5개 메서드가 본문 5줄씩 동일한 구조(`FindEntry → ValueType 검사 → 값 반환`)를 반복한다.
- `SetBool/SetInt/...`도 동일하게 6개 반복.
- `BlackboardKeySelector` 오버로드 12개는 단순 위임이라 가독성에는 무해하지만 줄 수를 차지한다.

작업 항목

| 작업 | 설명 |
|------|------|
| `TryGet`/`Set` 본문 제네릭화 | 내부 `TryGetTyped<T>(string key, BlackboardValueType expected, Func<BlackboardEntry, T> reader, out T value)` 헬퍼 도입. 공개 시그니처는 유지(직렬화·역호환). |
| Selector 오버로드 region 묶기 | 같은 메서드 그룹을 `#region BlackboardKeySelector` 등으로 묶어 본문에서 시각적으로 분리. |
| Lookup 캐시 검증 보강 | `EnsureLookup`이 중복 키 발생 시 silently skip — 디버그 빌드에서만 `Debug.LogWarning` 추가 검토(별도 작업). |

위험도: 낮음~중간. 제네릭화 시 BoolValue 등 값 필드 접근이 `BlackboardEntry`의 typed property를 그대로 사용하면 동작 동일. 단위 회귀를 위해 `MonsterBehaviorTreeJsonImporter`의 import 시나리오 1건 수동 검증 권장.

### C1.3 `BTNode.cs` (142줄)

문제점

- 가독성은 양호. 다만 `Tick()` 안에 Composite/Condition 특수 처리(`as BTCompositeNode`, `as BTConditionNode`)가 흩어져 있어 베이스 클래스가 파생 타입을 알게 되는 약한 의존 역전이 있다.
- `Initialize`/`ResetNode`의 자식·서비스 순회 패턴이 두 번 반복된다.

작업 항목

| 작업 | 설명 |
|------|------|
| 자식·서비스 순회 헬퍼 추출 | `private void ForEachChild(Action<BTNode>)`, `private void ForEachService(Action<BTServiceNode>)` 같은 헬퍼로 `Initialize`/`ResetNode`/`Abort`에서 재사용. |
| Composite/Condition 훅 정리 | `protected virtual void OnTickBeforeUpdate()/OnTickAfterUpdate()`를 추가하고 Composite·Condition에서 오버라이드. `Tick()`은 베이스 시퀀스만 유지. (선택 사항 — 비용 대비 효과 검토 필요) |

위험도: Composite/Condition 훅 도입은 모든 파생 노드 호출 경로에 영향. 자식 순회 헬퍼 추출만 우선 진행 권장.

---

## Phase C2: 중대형 Editor 파일 정리

목표: 200~400줄 범위의 Editor 파일에서 단일 메서드가 너무 큰 케이스, 책임이 섞인 케이스를 부분적으로 정리한다. partial 분리까지는 불필요하다고 판단되는 규모.

### C2.1 `BehaviorTreeAssetValidator.cs` (290줄) — 우선순위 높음

현재 상태

- `Validate(BehaviorTreeAsset)` 단일 메서드가 32~154줄(약 120줄)에 걸쳐 모든 검사를 인라인으로 수행한다.
- 검사 항목: Root null, Blackboard, Node null, Disabled 경고, Composite 자식 수, Decorator 자식 수, Service 부착, Composite Services 항목, WeightedRandom 가중치 길이, Subtree 참조 및 순환, Children 끊김, Root 미연결, 순환 참조.
- 검사 사이의 변수 스코프 누수(`compositeForServices` 같은 임시명) 가능성.

작업 항목

| 작업 | 설명 |
|------|------|
| 노드 단위 검사 분리 | `ValidateNode(tree, node, nodeIndex, messages)`로 추출, `Validate`는 트리 단위 검사와 루프만 담당. |
| 그래프 단위 검사 분리 | `ValidateOrphanNodes(tree, messages)`, `ValidateRootCycle(tree, messages)`로 추출. |
| Service 부착 검사 메서드 그대로 사용 | 이미 `IsAttachedAsService`로 분리돼 있음. 변경 없음. |
| 메시지 빌더 헬퍼 | `AddError(messages, node, fmt, args)` / `AddWarning(...)` 헬퍼로 동일한 매개변수 패턴 정리(선택). |

위험도: 낮음. 검사 결과 메시지 텍스트와 Level만 동일하게 유지하면 외부 가시 동작 변화 없음.

### C2.2 `BehaviorTreeJsonUtility.cs` (403줄)

현재 상태

- 직렬화 DTO 클래스(`BehaviorTreeJsonData` 등) + 메뉴 진입점 + Export/Import + 노드 프로퍼티 리플렉션 직렬화 + Vector2/3/FloatList Wrapper struct가 모두 같은 파일.
- 메서드 그룹: 메뉴(75~163줄), `ExportToData`/`ImportFromData`(216~330줄), 노드 프로퍼티 리플렉션(349~430줄), `EnsureAssetDirectory`(435줄).

작업 항목

| 작업 | 설명 |
|------|------|
| DTO 분리 | `BehaviorTreeJsonData`, `BlackboardEntryJson`, `BehaviorTreeNodeJson`, `BehaviorTreeNodePropertyJson`, Wrapper struct 3개를 `Editor/Json/BehaviorTreeJsonDto.cs`로 이동. |
| 메뉴 partial | 메뉴 진입점은 별도 partial `BehaviorTreeJsonUtility.Menus.cs`로 분리. |
| 리플렉션 헬퍼 partial | `ExportNodeProperties`, `ApplyNodeProperties`, `GetSerializableNodeFields`, `SerializeValue`, `DeserializeValue`를 `BehaviorTreeJsonUtility.Reflection.cs`로 분리. |

위험도: 낮음. JSON 스키마는 DTO 필드명에 의존하므로 필드명·@Serializable 어트리뷰트·필드 순서 보존이 핵심.

### C2.3 `BehaviorTreeInspectorView.cs` (381줄)

현재 상태

- 노드 선택 모드와 그룹 선택 모드 두 흐름이 한 클래스에 공존(`UpdateSelection(BTNode)`, `UpdateSelection(BehaviorTreeEditorGroup)`).
- Composite Service 부착 UI(`CreateAddServiceButton`, `AttachService`)가 같은 파일에 들어 있음.

작업 항목

| 작업 | 설명 |
|------|------|
| Service 부착 UI 분리 | `BehaviorTreeServiceAttachField.cs`(VisualElement 서브클래스)로 추출. InspectorView는 인스턴스화만 함. |
| Group 인스펙터 분리 | `CreateGroupPropertyBox` + `UpdateSelection(group)` 흐름을 `BehaviorTreeGroupInspectorView.cs`로 분리하는 안 검토. (현재 한 클래스에 둘 다 있어도 가독성이 크게 나쁘진 않음 — 우선순위 낮음) |

위험도: 중간. SerializedObject 바인딩과 InspectorView 라이프사이클이 EditorWindow와 결합돼 있어 분리 시 콜백 흐름 검증 필수.

### C2.4 `BehaviorTreeBlackboardView.cs` (330줄)

현재 상태

- 단일 클래스가 Asset 편집 UI(`DrawBlackboard`, `DrawEntryToolbar`)와 Runtime 비교(`ResolveRuntimeBlackboard`, `DrawSideBySideValue`, `DrawRuntimeOnlyEntries`), Rename 다이얼로그(`PromptRename`, `OnRenameConfirmed`)를 모두 담당.
- 같은 파일 말미에 `internal sealed class BehaviorTreeKeyRenameDialog`가 존재.

작업 항목

| 작업 | 설명 |
|------|------|
| Rename 다이얼로그 분리 | `BehaviorTreeKeyRenameDialog`를 `Editor/Dialog/BehaviorTreeKeyRenameDialog.cs`로 이동. (`BehaviorTreeMiniMapView` 분리와 동일 패턴) |
| Runtime 비교 partial | `DrawSideBySideValue`, `DrawRuntimeOnlyEntries`, `ResolveRuntimeBlackboard`를 `BehaviorTreeBlackboardView.Runtime.cs` partial로 분리. |

위험도: 낮음. 클래스 자체는 namespace-level sibling이라 외부 참조 영향 없음.

### C2.5 `BehaviorTreeSearchPanel.cs` (274줄)

현재 상태

- `SearchHit` struct와 `HitKind` enum이 같은 파일에 nested로 정의. 검색 수집(`Collect`)·결과 행 빌드(`BuildResultRow`)·헬퍼(`FindFirstNodeReferencingKey`) 책임이 한 클래스.
- 274줄 자체는 큰 편이 아니라 굳이 partial까지는 불필요할 가능성.

작업 항목

| 작업 | 설명 |
|------|------|
| 헬퍼 메서드 위치 정리 | `FindFirstNodeReferencingKey` 등 트리 검색 헬퍼를 `BehaviorTreeSearchPanel`의 `#region` 처리하거나, 정적 헬퍼 클래스 `BehaviorTreeSearchQuery`로 이동 검토. |
| 우선순위 낮음 | 현재 가독성은 양호. C2.1·C2.2·C2.4 완료 후 필요성 재평가. |

위험도: 낮음.

---

## Phase C3: Nodes 중복 정리

목표: `Nodes/Condition/` 및 `Nodes/Action/`의 노드들이 반복하는 보일러플레이트를 베이스 클래스 또는 헬퍼로 통합한다. **노드 클래스 이름·필드명·SerializedField 키는 절대 변경하지 않는다** (BehaviorTreeAsset SubAsset 직렬화 호환).

### C3.1 Cooldown 키 포맷 통합 (즉시 적용 가능)

문제점

- `$"Cooldown.{_cooldownId}.ReadyTime"` 문자열 포맷이 `CooldownReadyNode.cs:20`, `TransitionEnemyStateNode.cs:72`, `TransitionEnemyStateNode.cs:80` 3곳에 중복.
- 키 명명 규칙이 향후 바뀌면 3곳 모두 수정 필요. 한 곳이라도 누락 시 Cooldown 검사가 깨진다.

작업 항목

| 작업 | 설명 |
|------|------|
| 헬퍼 메서드 추가 | `Runtime/Blackboard/EnemyBlackboardKeys.cs`(또는 동일 namespace의 새 static class)에 `public static string CooldownReadyTime(string cooldownId) => $"Cooldown.{cooldownId}.ReadyTime";` 추가. |
| 호출처 교체 | 3곳을 헬퍼 호출로 변경. |

위험도: 매우 낮음. 결과 문자열이 동일하면 동작 변화 없음.

### C3.2 `GetComponentCached<T>` + null 가드 헬퍼

문제점

- `Context?.GetComponentCached<EnemyFlyingAIContext>()` 반복: 11개 노드
- `Context?.GetComponentCached<EnemyAIContext>()` 반복: 7개 노드
- `Context?.GetComponentCached<ActorMovementController>()` 반복: 10개 노드
- 패턴은 거의 항상 동일: `if (x == null) return BTStatus.Failure;` 후 본문 1~2줄.

후보 방향

| 방향 | 장점 | 단점 |
|------|------|------|
| A. 베이스 클래스 도입 (`EnemyAIContextActionNode`, `FlyingContextActionNode` 등) | 보일러플레이트 즉시 제거. `Execute(ctx)` 추상 메서드만 구현 | 베이스 클래스 깊이가 늘어남. 한 노드가 여러 컨텍스트를 필요로 하면 어색해짐. SubAsset 직렬화는 베이스 변경에 견딘다(필드만 동일하면 안전). |
| B. `BehaviorTreeContext`에 확장 메서드 추가 | 비침습적 | 호출 패턴은 줄지만 null-가드는 여전히 호출처에 남음 |
| C. 그대로 유지 | 변경 위험 0 | 보일러플레이트 누적 계속 |

권장: 비행형/지상형 모두 동일 패턴을 따르는 단순 노드부터 **방향 A**를 시범 도입. 단, 첫 적용은 비행형 Action 4종(`DescendFlyingNode`, `ResetFlyingCountersNode`, `ResetFlyingAirCountersNode`, `SelectFlyingDiveSkillNode`)에 한정해 영향을 확인한 뒤 확장.

작업 항목

| 작업 | 설명 |
|------|------|
| 베이스 클래스 도입(시범) | `Nodes/Base/FlyingContextActionNode.cs` 추가. `protected abstract BTStatus Execute(EnemyFlyingAIContext context);` 정의. 베이스의 `OnUpdate`에서 null 가드. |
| 4개 노드 베이스 교체 | 클래스 이름·필드·서브에셋 GUID 변경 없이 베이스만 교체. SubAsset 호환을 위해 직렬화된 필드는 그대로. |
| 검증 | 기존 비행형 적 BT가 그대로 동작하는지 SourceJson 일부 재-import해 확인. |
| 결과 평가 후 확장 | 문제 없으면 `EnemyAIContext` 계열·`ActorMovementController` 계열에도 동일 패턴 도입. |

위험도: 중간. ScriptableObject 베이스 변경은 직렬화에 영향을 줄 수 있다. **반드시 임시 BT Asset 1개로 PlayMode 검증을 거쳐야 한다.** 만약 [SerializeField] 필드 집합이 동일하면 Unity는 SubAsset 직렬화를 유지하지만, MonoScript GUID는 동일해야 한다(같은 namespace/이름이면 안전).

대안: 베이스 변경이 위험하면 `BTActionNode`/`BTConditionNode`에 `protected bool TryGetContext<T>(out T ctx) where T : MonoBehaviour`를 추가하고 호출처를 `if (!TryGetContext<EnemyFlyingAIContext>(out var ctx)) return BTStatus.Failure;`로만 줄이는 방식도 가능. 베이스 클래스 깊이가 늘지 않으므로 더 안전.

### C3.3 비행 상태 이름 그룹 상수화

문제점

- `IsFlyingAirStateNode.cs:14`: `state is "Flying_AirCircle" or "Flying_TakeOff" or "Flying_Dive"`
- `IsFlyingGroundCombatStateNode.cs:14`: `state is "Flying_Chase" or "Flying_GroundAttack" or "Flying_Circle" or "Flying_Retreat"`
- 상태 이름 변경 시 BT 노드까지 동기화 필요.

작업 항목

| 작업 | 설명 |
|------|------|
| 상수 도입 | 비행 상태 이름을 `State/Enemy/EnemyFlying/`의 `EnemyFlyingStateNames.cs`(신규)에 `public const string AirCircle = "Flying_AirCircle";` 형태로 모은다. 또는 기존 상태 클래스의 `StateName` 프로퍼티가 이미 동일 문자열을 반환한다면 그쪽을 단일 출처로 사용. |
| BT 노드 교체 | 두 노드의 매칭 식을 상수 또는 set 멤버십 검사로 교체. |

위험도: 낮음. 상태 이름 자체는 변경하지 않으므로 행동 동일.

### C3.4 검토 후 보류 항목

다음 항목은 분석 결과 우선순위가 낮거나 위험 대비 효과가 작아 본 계획 범위에서 제외한다.

- `MonsterActor.GetHealthPercent()` 같은 단일 호출 노드(`IsSelfLowHealthNode`)는 이미 짧고 명확하므로 변경 불필요.
- `BlackboardBoolConditionNode`의 `Context.DebugTrace?.Record(...)` 호출 — 디버그 메시지 형식이 명확해 그대로 유지.
- Service 노드는 Composite Inspector를 통해서만 부착되므로 별도 베이스 도입 효과가 작다.

---

## 우선순위 정리

전체 작업 중 다음 순서를 권장한다.

| 순위 | 작업 | 분류 | 예상 영향 |
|------|------|------|-----------|
| 1 | C3.1 Cooldown 키 헬퍼 통합 | Nodes 즉시 | 3곳 수정. 위험 매우 낮음. |
| 2 | C2.1 AssetValidator.Validate 분할 | Editor | 120줄 메서드 분해. 위험 낮음. |
| 3 | C1.1 Runner 초기화/종료 시퀀스 추출 | Runtime | 동작 보존이 본 단계의 핵심. |
| 4 | C2.4 BlackboardView Rename 다이얼로그 분리 + Runtime partial | Editor | 단순 추출. |
| 5 | C2.2 JsonUtility DTO/Menu/Reflection 분리 | Editor | partial 패턴 재사용. |
| 6 | C1.2 Blackboard TryGet/Set 제네릭화 | Runtime | 회귀 테스트 1건 필요. |
| 7 | C3.2 시범 베이스 클래스 도입 (비행 Action 4개) | Nodes | PlayMode 검증 필수. |
| 8 | C3.3 비행 상태 이름 상수화 | Nodes | 위험 낮음. |
| 9 | C1.3 BTNode 순회 헬퍼 추출 | Runtime | 모든 노드 영향, 마지막에 검토. |
| 10 | C2.3 / C2.5 InspectorView·SearchPanel | Editor | 효과 대비 비용 평가 후 결정. |

---

## 검증 절차

각 단계 종료 후 다음을 수행한다.

1. Unity 컴파일 확인(에러·경고 비교).
2. `UPlayGround/Character/AI/Monster Behavior Json/Import All SourceJson` 실행해 기존 SourceJson이 동일하게 import되는지 확인.
3. 임의 적 1마리(지상형)와 1마리(비행형)를 PlayMode에서 가동해 BT가 정상 Tick하는지, Trace 탭에 동일 경로가 보이는지 확인.
4. `BehaviorTreeAssetValidator` 실행 결과 메시지 개수가 변경 전과 동일한지 확인(C2.1 진행 시).
5. C3.2 시범 적용 후 SubAsset 직렬화가 유지됐는지(`.asset` 파일 diff에서 GUID/MonoScript 참조가 살아 있는지) 확인.

---

## 비목표

- 노드 클래스 이름·필드명·`[SerializeField]` 키 변경
- JSON 스키마 변경(`MonsterBehaviorTreeJson`/`BehaviorTreeJsonData`)
- BehaviorTreeAsset의 SubAsset 구성 변경
- BT 에디터 UI 시각 디자인 변경
- 신규 기능 추가(검색·디버깅·런타임 훅)

위 항목은 별도 기획 문서가 필요한 변경이며, 본 코드 정리 범위와 분리한다.