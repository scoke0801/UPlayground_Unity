# Behavior Tree 에디터 개선 실행 계획

> 작성일: 2026-05-11  
> 완료일: 2026-05-13  
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
| Blackboard | 기본 Entry 추가/삭제/값 편집, Rename 일괄 업데이트, Asset/Runtime 사이드바이사이드 비교 |
| 검증 | 루트, 자식 수, 끊어진 참조, 순환 참조, Subtree 순환 일부 검증 |
| Debug | Runner 자동 감지, Play/Pause/Step/Stop, Trace 탭, 현재 Tick 노드 하이라이트, Breadcrumb |
| Import/Export | JSON 기반 BT Asset 내보내기/가져오기 |
| 검색 | DisplayName, Comment, Blackboard Key 참조 전역 검색 패널 |
| 성능 측정 | 합성 그래프 생성 메뉴(100/200/500 노드)와 저장 시간 로깅 |

---

## 개선 우선순위

### Phase 진행 상태

| Phase | 상태 | 비고 |
|------|------|-----------|
| E1 노드 생성 UX 개선 | 완료 | SearchWindow + 우클릭, Recent 그룹, 포트 드래그 생성 |
| E2 Blackboard 안정성 강화 | 완료 | Key 검증, 타입 드롭다운, Runtime 비교, Rename 일괄 업데이트 |
| E3 Trace와 디버깅 연결성 개선 | 완료 | Trace 클릭 포커스, Breakpoint 자동 선택, Runner 자동 감지, Breadcrumb |
| E4 그래프 제작 기능 확장 | 부분 완료 | Copy/Paste, 전역 검색 완료. SubTree 추출/외부 브랜치 가져오기는 후속 |
| E5 저장/성능 안정화 | 완료 | Save debounce, 명시적 Save flush, Undo 단일 group, 합성 그래프 측정 |

### Phase E1: 노드 생성 UX 개선

목표: 노드 수가 늘어도 빠르게 검색해 생성할 수 있게 한다.

| 작업 | 상태 | 설명 |
|------|------|------|
| GraphView SearchWindow 추가 | 완료 | `nodeCreationRequest`를 통해 Space/SearchWindow 기반 노드 검색 생성 |
| 우클릭 메뉴 유지 | 완료 | 기존 `Create/{Category}/{Type}` 메뉴는 보조 경로로 유지 |
| Service 노드 직접 생성 제외 | 완료 | Service는 Composite Inspector 부착 방식이므로 검색 목록에서 제외 |
| 즐겨찾기/최근 사용 노드 | 완료 | `EditorPrefs`에 최근 6개 노드 타입을 저장해 SearchWindow 상단 Recent 그룹에 노출 |
| 포트 드래그 후 노드 생성 | 완료 | Edge가 빈 공간에 드롭되면 포트 방향에 맞는 타입만 SearchWindow로 노출하고 자동 연결 |

### Phase E2: Blackboard 안정성 강화

목표: 문자열 Key 실수를 저장/실행 전에 잡는다.

| 작업 | 상태 | 설명 |
|------|------|------|
| Blackboard Key 중복 검증 | 완료 | Validator가 빈 Entry와 중복 Key를 Error로 표시 |
| `BlackboardKeySelector` 검증 | 완료 | Selector의 누락 Key와 타입 불일치를 검사 |
| 레거시 string `_key` 검증 | 완료 | `_key`, `key`, `*Key` 필드가 Blackboard에 존재하는지 검사 |
| 타입 기반 드롭다운 | 완료 | `BlackboardKeySelectorDrawer`로 타입 일치 Key만 노출 |
| Key rename 참조 업데이트 | 완료 | Blackboard Key를 Rename 다이얼로그로 변경하면 모든 노드의 Selector/`*Key` 문자열이 일괄 업데이트되며 단일 Undo group으로 묶임 |
| Runtime Blackboard 비교 | 완료 | Variables 탭에서 Asset 값과 Runtime 값을 사이드바이사이드로 표시. Runtime 전용 Key는 Runtime Only 섹션에 별도 노출 |

### Phase E3: Trace와 디버깅 연결성 개선

목표: 실행 중 어떤 조건 때문에 분기가 바뀌었는지 에디터에서 바로 추적한다.

| 작업 | 상태 | 설명 |
|------|------|------|
| Trace row 클릭 시 노드 포커스 | 완료 | Trace 항목의 GUID로 그래프 노드 선택 및 프레임 |
| Breakpoint hit 자동 선택 | 완료 | Pause 원인 노드를 Inspector에 표시 |
| Blackboard 읽기/쓰기 Trace | 완료 | Blackboard Condition/Action/Service에서 Key와 값을 Trace에 기록 |
| 현재 실행 경로 Breadcrumb | 완료 | Root부터 Running 노드까지 경로를 그래프 하단 바에 표시. 라벨 클릭 시 해당 노드로 포커스 |
| Runner 자동 감지 | 완료 | Scene 변경/선택 변경/Play Mode 진입 시 현재 트리와 일치하는 `BehaviorTreeRunner`를 Debug 슬롯에 자동 채움 |

### Phase E4: 그래프 제작 기능 확장

목표: 큰 Enemy AI 그래프를 반복 작업 없이 구성한다.

| 작업 | 상태 | 설명 |
|------|------|------|
| 복사/붙여넣기 | 완료 | 선택 노드/그룹 복제, GUID 재발급, 선택 내부 연결 복원 |
| 노드/Blackboard 전역 검색 | 완료 | Search 탭에서 DisplayName, Comment, BlackboardKeySelector, 레거시 `*Key` 참조를 한 번에 검색하고 결과 클릭으로 해당 노드 포커스 |
| 선택 영역 SubTree 추출 | 후속 | 정책 결정(추출 단위, 원본을 SubTree 참조 노드로 치환할지) 필요 — 별도 설계 회의 후 진행 |
| 다른 BT Asset에서 브랜치 가져오기 | 후속 | Blackboard Key 충돌 해소 정책 필요 — SubTree 추출과 함께 설계 |

### Phase E5: 저장/성능 안정화

목표: 그래프 편집 중 저장 비용을 줄인다.

| 작업 | 상태 | 설명 |
|------|------|------|
| 명시적 Save 우선 정책 검토 | 완료 | GraphView 변경은 Dirty 표시 후 지연 저장하고, Save 버튼/창 종료에서 flush |
| Debounce 저장 | 완료 | 짧은 시간 내 반복 변경은 0.35초 debounce로 묶어서 저장 |
| Debug UI 핫 패스 완화 | 완료 | Trace 버전/틱 변화가 없으면 Graph/Trace/Blackboard/MiniMap 갱신을 건너뜀. Breadcrumb도 Tick 변화 시에만 재생성 |
| Undo group 정리 | 완료 | Paste, Rename, 포트 드래그 생성, 그리고 `OnGraphViewChanged` 전체(이동/연결/삭제 묶음)를 각각 단일 Undo group으로 결합 |
| 대형 그래프 성능 측정 | 완료 | `UPlayGround/Character/AI/Behavior Tree Perf/100·200·500 Nodes` 메뉴로 합성 그래프 생성. 생성/저장 소요 시간을 콘솔에 로깅 (`BehaviorTreePerformanceProbe`) |

---

## 이번 반영 내용 (2026-05-13 완료)

### 1. SearchWindow Recent 그룹과 포트 드래그 생성

추가 파일:

```text
(없음 — 기존 파일 확장)
```

수정 파일:

```text
Assets/02.Scripts/AI/BehaviorTree/Editor/BehaviorTreeNodeSearchWindow.cs
Assets/02.Scripts/AI/BehaviorTree/Editor/BehaviorTreeGraphView.cs
```

주요 내용:

- SearchWindow에 `EditorPrefs` 기반 Recent(최근 6개) 그룹을 추가. 노드를 생성하면 해당 타입이 최상단으로 올라온다.
- 포트에서 Edge를 드래그한 뒤 빈 공간에 드롭하면 SearchWindow가 포트 방향에 맞는 타입만 노출하고, 선택 시 새 노드를 만들면서 즉시 연결한다.
- 노드의 포트 EdgeConnector를 `PortDragConnectorListener`로 교체해 정상 연결과 드롭 아웃을 모두 우리 로직으로 처리한다.

### 2. Blackboard Key Rename과 Runtime 사이드바이사이드

추가 파일:

```text
Assets/02.Scripts/AI/BehaviorTree/Editor/BehaviorTreeBlackboardKeyRenamer.cs
```

수정 파일:

```text
Assets/02.Scripts/AI/BehaviorTree/Editor/BehaviorTreeBlackboardView.cs
```

주요 내용:

- `BehaviorTreeBlackboardKeyRenamer.RenameKey`가 Asset 내 모든 노드의 `BlackboardKeySelector`와 `_key`/`*Key` 문자열 필드를 일괄 갱신하고 단일 Undo group으로 묶는다.
- BlackboardView에 Rename 버튼과 다이얼로그를 추가. 새 Key가 이미 존재하면 변경을 거절한다.
- Runtime 값은 Asset 값과 같은 행에 사이드바이사이드로 표시한다. Asset에 없고 Runtime에만 존재하는 Key는 `Runtime Only` 섹션에 별도로 노출한다.

### 3. Runner 자동 감지와 실행 경로 Breadcrumb

수정 파일:

```text
Assets/02.Scripts/AI/BehaviorTree/Editor/BehaviorTreeEditorWindow.cs
```

주요 내용:

- `EditorApplication.hierarchyChanged`, `playModeStateChanged`, `Selection.selectionChanged`를 구독해 현재 트리와 일치하는 `BehaviorTreeRunner`를 자동으로 Debug 슬롯에 채운다. 사용자가 수동으로 다른 Runner를 설정하면 자동 감지를 비활성화한다.
- Graph 영역 하단에 Breadcrumb 바를 추가. `RootNode`부터 `IsRunning` 상태인 자식을 따라 내려가며 경로를 라벨로 나열한다. 각 라벨 클릭 시 해당 노드로 포커스되며, Trace `CurrentTick` 변화 시에만 라벨을 다시 그려 핫 패스를 보존한다.

### 4. 노드/Blackboard 전역 검색

추가 파일:

```text
Assets/02.Scripts/AI/BehaviorTree/Editor/BehaviorTreeSearchPanel.cs
```

수정 파일:

```text
Assets/02.Scripts/AI/BehaviorTree/Editor/BehaviorTreeEditorWindow.cs
```

주요 내용:

- Inspector/Variables/Errors/Trace 옆에 `Search` 탭을 추가.
- DisplayName, Comment, `BlackboardKeySelector` Key, 레거시 `*Key` 문자열 필드를 동시에 검색한다. 결과 행은 종류별 색상으로 구분하고, 클릭 시 그래프에서 해당 노드로 포커스한다.
- Blackboard 자체 검색 결과는 해당 Key를 참조하는 노드 수도 함께 표시한다.

### 5. Undo 단일 그룹화

수정 파일:

```text
Assets/02.Scripts/AI/BehaviorTree/Editor/BehaviorTreeGraphView.cs
```

주요 내용:

- `OnGraphViewChanged`가 호출되면 한 번의 변경(이동/연결/삭제 묶음) 동안 `Undo.SetCurrentGroupName` + `Undo.CollapseUndoOperations`로 단일 Undo 항목으로 결합한다.
- 포트 드래그로 노드를 만들 때도 노드 생성과 연결을 단일 그룹으로 묶는다.

### 6. 대형 그래프 성능 측정

추가 파일:

```text
Assets/02.Scripts/AI/BehaviorTree/Editor/BehaviorTreePerformanceProbe.cs
```

주요 내용:

- `UPlayGround/Character/AI/Behavior Tree Perf/{100|200|500} Nodes` 메뉴를 추가.
- 4분기 균형 트리를 합성해 `Assets/10.Datas/Perf/` 경로에 임시 BT Asset을 만든다.
- 노드 생성 시간, `AssetDatabase.SaveAssets()` 시간, 총 소요 시간을 콘솔에 한국어로 로깅한다.
- 합성 Asset은 측정용이므로 필요시 사용자가 직접 삭제한다.

#### 측정 절차

1. Unity 에디터에서 메뉴 실행 → 콘솔 로그 확인.
2. 측정 Asset을 BT Editor에서 열어 PopulateView/MiniMap/Save 반응을 체감 점검.
3. 필요한 수치만 콘솔에서 캡처해 외부 노트(별도 운영용)에 기록한다. 본 문서는 회귀 추적용으로 유지하고 수치는 메뉴 실행으로 재현 가능하다.

---

## 후속 작업

다음 항목은 정책 결정이나 별도 설계 회의가 필요해 본 실행 계획에서는 의도적으로 제외했다.

1. 선택 영역 SubTree 추출 — 추출 단위(루트 단일/다중), 원본을 SubTree 참조로 치환할지 여부 정책 필요.
2. 다른 BT Asset에서 브랜치 가져오기 — Blackboard Key 충돌 해소 전략(자동 prefix, 사용자 매핑 UI 등) 필요.

두 항목은 SubTree 시스템 전반 재설계와 함께 다루는 것이 유리하므로 별도 설계 문서로 분리한다.

---

## 확인 포인트

- Space 또는 GraphView 노드 생성 요청으로 SearchWindow가 뜨고, 상단에 Recent 그룹이 노출되는지 확인.
- Composite/Decorator 노드의 출력 포트에서 빈 공간으로 Edge를 드래그하면 SearchWindow가 뜨고 선택한 노드가 자동 연결되는지 확인.
- Variables 탭에서 Blackboard Key 옆 `Rename` 버튼을 누르면 다이얼로그가 뜨고, 새 이름으로 변경 시 모든 참조 노드가 함께 갱신되는지 확인. 단일 Undo로 되돌릴 수 있는지 확인.
- Play Mode에서 Runner를 미지정 상태로 두고 Tree만 열어도 Hierarchy의 일치 Runner가 자동으로 Debug 슬롯에 채워지는지 확인.
- 실행 중 그래프 하단 Breadcrumb 바에 경로가 표시되고, 라벨 클릭으로 노드 포커스가 되는지 확인.
- Search 탭에서 노드 이름 일부/Blackboard Key 일부 입력 시 매칭 노드/Key가 나열되고, 결과 클릭으로 노드 포커스가 되는지 확인.
- 노드 다수를 한 번에 이동/삭제한 뒤 `Ctrl+Z` 한 번으로 모두 되돌릴 수 있는지 확인.
- `UPlayGround/Character/AI/Behavior Tree Perf/100 Nodes` 메뉴로 합성 Asset이 생성되고, 콘솔에 노드 생성/저장 시간이 로깅되는지 확인.
