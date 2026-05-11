# Behavior Tree 에디터 개선 실행 계획

> 작성일: 2026-05-11  
> 대상: `Assets/02.Scripts/AI/BehaviorTree/Editor/` 및 관련 Runtime/Blackboard 코드  
> 기준: 현재 프로젝트에 이미 구현된 BT 에디터에서 빠진 제작 생산성, 검증, 런타임 디버깅 기능을 우선 보강한다.

---

## 개요

현재 Behavior Tree 에디터는 GraphView 기반 편집, Inspector, Blackboard, Validator, JSON Import/Export, Runtime Trace, MiniMap을 이미 갖추고 있다.

따라서 개선 방향은 신규 BT 시스템을 다시 만드는 것이 아니라 다음 문제를 줄이는 데 둔다.

- 노드 수 증가 시 우클릭 메뉴만으로는 원하는 노드를 찾기 어렵다.
- Blackboard 키가 문자열 필드로 흩어져 있어 누락/타입 불일치를 늦게 발견한다.
- 런타임 Trace가 텍스트 목록 중심이라 실행 경로와 변수 변화를 추적하기 어렵다.
- 복사/붙여넣기, SubTree 추출, 검색 같은 대형 그래프 제작 기능이 부족하다.
- `AssetDatabase.SaveAssets()` 호출이 잦아 그래프 규모가 커지면 에디터 반응성이 떨어질 수 있다.

---

## 현재 구현 요약

| 영역 | 현재 상태 |
|------|-----------|
| 그래프 편집 | `BehaviorTreeGraphView`에서 노드 생성, 연결, 삭제, 이동, 그룹 박스, 미니맵 지원 |
| 노드 표시 | `BehaviorTreeNodeView`에서 타입별 색상, Breakpoint/Disabled, 런타임 상태 표시 |
| Inspector | 선택 노드와 그룹 박스 필드 편집 |
| Blackboard | 기본 Entry 추가/삭제/값 편집 |
| 검증 | 루트, 자식 수, 끊어진 참조, 순환 참조, Subtree 순환 일부 검증 |
| Debug | Runner 지정, Play/Pause/Step/Stop, Trace 탭, 현재 Tick 노드 하이라이트 |
| Import/Export | JSON 기반 BT Asset 내보내기/가져오기 |

---

## 개선 우선순위

### Phase 진행 상태

| Phase | 상태 | 이번 기준 |
|------|------|-----------|
| E1 노드 생성 UX 개선 | 완료 | SearchWindow 기반 노드 생성과 기존 우클릭 메뉴 병행 |
| E2 Blackboard 안정성 강화 | 핵심 완료 | Key 검증, 타입 드롭다운, Runtime 값 표시 완료. Key rename 일괄 업데이트는 후속 |
| E3 Trace와 디버깅 연결성 개선 | 핵심 완료 | Trace 클릭 포커스, Breakpoint 자동 선택, Blackboard Read/Write Trace 완료 |
| E4 그래프 제작 기능 확장 | 부분 완료 | Copy/Paste와 GUID 재발급 완료. SubTree 추출/브랜치 가져오기는 후속 |
| E5 저장/성능 안정화 | 핵심 완료 | Save debounce, 명시적 Save flush, 디버그 UI 핫 패스 완화, Paste Undo group 정리 완료. 대형 그래프 측정은 후속 |

### Phase E1: 노드 생성 UX 개선

목표: 노드 수가 늘어도 빠르게 검색해 생성할 수 있게 한다.

| 작업 | 상태 | 설명 |
|------|------|------|
| GraphView SearchWindow 추가 | 완료 | `nodeCreationRequest`를 통해 Space/SearchWindow 기반 노드 검색 생성 지원 |
| 우클릭 메뉴 유지 | 완료 | 기존 `Create/{Category}/{Type}` 메뉴는 보조 경로로 유지 |
| Service 노드 직접 생성 제외 | 완료 | Service는 Composite Inspector 부착 방식이므로 검색 목록에서도 제외 |
| 즐겨찾기/최근 사용 노드 | 예정 | Enemy 제작에 자주 쓰는 노드를 상단에 배치 |
| 포트 드래그 후 노드 생성 | 예정 | 연결 대상 포트 타입에 맞는 노드만 필터링 |

### Phase E2: Blackboard 안정성 강화

목표: 문자열 Key 실수를 저장/실행 전에 잡는다.

| 작업 | 상태 | 설명 |
|------|------|------|
| Blackboard Key 중복 검증 | 완료 | Validator가 빈 Entry와 중복 Key를 Error로 표시 |
| `BlackboardKeySelector` 검증 | 완료 | Selector의 누락 Key와 타입 불일치를 검사 |
| 레거시 string `_key` 검증 | 완료 | `_key`, `key`, `*Key` 필드가 Blackboard에 존재하는지 검사 |
| 타입 기반 드롭다운 | 진행 중 | `BlackboardKeySelectorDrawer`로 타입 일치 Key만 노출 |
| Key rename 참조 업데이트 | 예정 | Blackboard Key 변경 시 참조 노드 일괄 업데이트 |
| Runtime Blackboard 비교 | 예정 | Asset 기본값과 Runner 런타임 값을 나란히 표시 |

### Phase E3: Trace와 디버깅 연결성 개선

목표: 실행 중 어떤 조건 때문에 분기가 바뀌었는지 에디터에서 바로 추적한다.

| 작업 | 상태 | 설명 |
|------|------|------|
| Trace row 클릭 시 노드 포커스 | 완료 | Trace 항목의 GUID로 그래프 노드 선택 및 프레임 |
| Breakpoint hit 자동 선택 | 완료 | Pause 원인 노드를 Inspector에 표시 |
| Blackboard 읽기/쓰기 Trace | 완료 | Blackboard Condition/Action/Service에서 Key와 값을 Trace에 기록 |
| 현재 실행 경로 Breadcrumb | 예정 | Root부터 현재 Running 노드까지 경로 표시 |
| Runner 자동 감지 | 예정 | Scene Selection의 `BehaviorTreeRunner`를 Debug Runner로 자동 제안 |

### Phase E4: 그래프 제작 기능 확장

목표: 큰 Enemy AI 그래프를 반복 작업 없이 구성한다.

| 작업 | 상태 | 설명 |
|------|------|------|
| 복사/붙여넣기 | 완료 | 선택 노드/그룹 복제, GUID 재발급, 선택 내부 연결 복원 |
| 선택 영역 SubTree 추출 | 예정 | 공통 Combat/Patrol 브랜치를 별도 Asset으로 분리 |
| 다른 BT Asset에서 브랜치 가져오기 | 예정 | 테스트 그래프와 실제 그래프 간 재사용 |
| 노드/Blackboard 전역 검색 | 예정 | DisplayName, Comment, Key 참조 검색 |

### Phase E5: 저장/성능 안정화

목표: 그래프 편집 중 저장 비용을 줄인다.

| 작업 | 상태 | 설명 |
|------|------|------|
| 명시적 Save 우선 정책 검토 | 완료 | GraphView 변경은 Dirty 표시 후 지연 저장하고, Save 버튼/창 종료에서 flush |
| Debounce 저장 | 완료 | 짧은 시간 내 반복 변경은 0.35초 debounce로 묶어서 저장 |
| Debug UI 핫 패스 완화 | 완료 | Trace 버전/틱 변화가 없으면 Graph/Trace/Blackboard/MiniMap 갱신을 건너뜀 |
| Undo group 정리 | 부분 완료 | Paste 작업은 하나의 Undo group으로 묶음. 이동/연결 세부 group 정리는 후속 |
| 대형 그래프 성능 측정 | 예정 | 노드 100개 이상에서 드래그/저장 반응 확인 |

---

## 이번 반영 내용

### 1. SearchWindow 기반 노드 생성

추가 파일:

```text
Assets/02.Scripts/AI/BehaviorTree/Editor/BehaviorTreeNodeSearchWindow.cs
```

수정 파일:

```text
Assets/02.Scripts/AI/BehaviorTree/Editor/BehaviorTreeGraphView.cs
```

주요 내용:

- `nodeCreationRequest`를 연결해 GraphView 기본 노드 생성 요청에서 검색창을 띄운다.
- `Composite`, `Decorator`, `Condition`, `Action` 카테고리로 노드를 분류한다.
- `BTServiceNode`는 그래프 직접 생성 대상에서 제외한다.
- 기존 우클릭 Create 메뉴는 그대로 유지한다.

### 2. Blackboard 참조 검증 강화

수정 파일:

```text
Assets/02.Scripts/AI/BehaviorTree/Editor/BehaviorTreeAssetValidator.cs
```

주요 내용:

- Blackboard Entry가 null이거나 Key가 비어 있으면 Error를 표시한다.
- 동일 Key가 여러 번 등록되면 Error를 표시한다.
- `BlackboardKeySelector` 필드를 반사로 찾아 누락 Key와 타입 불일치를 검사한다.
- 기존 노드의 string `_key`, `key`, `*Key` 필드도 Blackboard 참조로 보고 존재 여부를 검사한다.
- `_valueType` 필드가 있으면 Blackboard Entry 타입과 일치하는지 검사한다.

### 3. Runtime Debug 연결성 보강

수정 파일:

```text
Assets/02.Scripts/AI/BehaviorTree/Editor/BehaviorTreeEditorWindow.cs
Assets/02.Scripts/AI/BehaviorTree/Editor/BehaviorTreeGraphView.cs
Assets/02.Scripts/AI/BehaviorTree/Editor/BehaviorTreeBlackboardView.cs
```

주요 내용:

- Trace 탭의 각 행을 클릭하면 `NodeGuid`와 같은 그래프 노드를 선택하고 프레임한다.
- Breakpoint로 Runner가 Pause되면 Pause를 요청한 노드를 자동으로 선택한다.
- Variables 탭에서 Play Mode Runtime Blackboard 값을 읽기 전용으로 함께 표시한다.
- `SetBlackboardValueNode`, `SyncEnemyBlackboardNode`, `SyncEnemyBlackboardService`, `BlackboardBoolConditionNode`가 Blackboard 읽기/쓰기 내용을 Debug Trace에 남긴다.

### 4. 선택 영역 Copy/Paste

수정 파일:

```text
Assets/02.Scripts/AI/BehaviorTree/Editor/BehaviorTreeGraphView.cs
```

주요 내용:

- GraphView `serializeGraphElements`, `canPasteSerializedData`, `unserializeAndPaste` 콜백을 연결했다.
- 우클릭 `Edit/Copy Selection`, `Edit/Paste` 메뉴를 추가했다.
- 선택한 일반 노드는 `Instantiate`로 복제하고 새 GUID를 발급한다.
- 선택한 노드끼리 연결된 자식 관계만 새 노드끼리 다시 연결한다.
- 선택한 그룹 박스도 새 GUID와 위치 offset으로 복제한다.
- Composite에 부착된 Service 노드는 그래프에 직접 표시되지 않으므로 Composite 복제 시 함께 서브에셋으로 복제한다.

### 5. Save Debounce와 Undo 정리

수정 파일:

```text
Assets/02.Scripts/AI/BehaviorTree/Editor/BehaviorTreeGraphView.cs
Assets/02.Scripts/AI/BehaviorTree/Editor/BehaviorTreeEditorWindow.cs
```

주요 내용:

- GraphView 변경 시 `EditorUtility.SetDirty`는 즉시 수행하되 `AssetDatabase.SaveAssets()`는 0.35초 debounce로 묶는다.
- 반복 이동/연결 변경 중 디스크 저장 호출이 과도하게 발생하지 않게 했다.
- 에디터 창이 비활성화될 때 pending save를 즉시 flush한다.
- 상단 Save 버튼을 누르면 pending save를 먼저 flush한 뒤 명시적으로 저장한다.
- Paste 작업은 `Undo.SetCurrentGroupName` / `Undo.CollapseUndoOperations`로 하나의 Undo 단위로 묶는다.

### 6. 핫 패스 검토 및 성능 개선

수정 파일:

```text
Assets/02.Scripts/AI/BehaviorTree/Runtime/BehaviorTreeRunner.cs
Assets/02.Scripts/AI/BehaviorTree/Editor/BehaviorTreeGraphView.cs
Assets/02.Scripts/AI/BehaviorTree/Editor/BehaviorTreeEditorWindow.cs
```

확인된 문제:

- `OnEditorUpdate()`가 에디터 프레임마다 Graph, MiniMap, Blackboard, Trace를 모두 갱신했다.
- Trace 탭이 열려 있으면 매 프레임 Label을 전부 삭제/재생성했다.
- `UpdateDebugState()`가 매 호출마다 런타임 노드 GUID Dictionary와 Edge `ToList()`를 할당했다.
- Trace row 클릭, Breakpoint 자동 포커스에서 GUID 기반 노드 탐색이 선형 탐색이었다.

개선 내용:

- `BehaviorTreeDebugTrace.Version`을 추가해 Trace 기록 변경 여부를 O(1)로 확인한다.
- Trace `Version`/`CurrentTick`, Runner 상태가 바뀌지 않으면 디버그 UI 갱신을 건너뛴다.
- 정지 상태에서는 `OnEditorUpdate()`가 빠르게 반환하고, 실행 중 무변화 프레임은 0.05초 단위로 제한한다.
- GraphView의 GUID -> NodeView 캐시를 추가해 Trace 클릭/Breakpoint 포커스를 선형 탐색에서 Dictionary 조회로 바꿨다.
- Runtime GUID Dictionary를 재사용하고, Edge 순회에서 불필요한 `ToList()` 할당을 제거했다.

---

## 다음 작업 권장 순서

1. Blackboard Key rename 참조 업데이트
2. 선택 영역 SubTree 추출
3. Runner 자동 감지
4. 현재 실행 경로 Breadcrumb
5. 대형 그래프 성능 측정

이 순서는 런타임 동작보다 에디터 저작/디버깅 병목을 먼저 줄이는 흐름이다. 기존 Enemy AI와 BT Runner의 실행 의미를 건드리지 않으므로 회귀 위험이 낮다.

---

## 확인 포인트

- Unity 에디터에서 BT GraphView를 열고 Space 키 또는 GraphView 노드 생성 요청으로 검색창이 뜨는지 확인한다.
- `SetBlackboardValueNode`, `BlackboardBoolConditionNode` 같은 레거시 string Key 노드에서 존재하지 않는 Key를 넣고 Validate가 Error를 표시하는지 확인한다.
- 중복 Blackboard Key를 만든 뒤 Validate가 Error를 표시하는지 확인한다.
- Service 노드가 검색창과 우클릭 생성 메뉴에 노출되지 않는지 확인한다.
