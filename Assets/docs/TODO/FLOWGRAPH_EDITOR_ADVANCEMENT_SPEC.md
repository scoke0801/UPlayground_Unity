# FlowGraph 사용성·표현성 고도화 제안

> 작성일: 2026-07-26  
> 대상: `UPlayGround.FlowGraph`, `UPlayGround.FlowGraph.Editor`  
> 상태: M1~M4 기반 구현 완료. Unity Play Mode 실행 검증과 실제 콘텐츠 적용은 후속 확인 필요.
> 기준 문서: `Assets/docs/guide/FLOWGRAPH_SYSTEM_GUIDE.md`  
> 레퍼런스 우선순위: NodeCanvas → Unreal Engine Blueprint

---

## 1. 결론

현재 FlowGraph는 “게임 진행용 실행 토큰 그래프”의 MVP를 넘어, 소규모 그래프를 실제로 저작하고 관찰할 수 있는 기본기는 이미 갖췄다. 노드 검색·라이브러리, Blackboard, 그룹/미니맵, 복사/붙여넣기, 검증 배지, SubGraph 탐색, 실행 하이라이트와 브레이크포인트까지 구현되어 있다.

다음 단계의 병목은 노드 수가 아니라 아래 네 가지다.

1. **큰 그래프를 찾고 고치는 능력** — 그래프/노드/변수/참조를 한 번에 검색하는 Explorer와 강한 검증이 없다.
2. **실행을 설명하는 능력** — 현재 브레이크포인트는 Unity 일시 정지에 가깝고, 실행 이력·값 Watch·Step·실행 인스턴스 선택이 없다.
3. **재사용 가능한 계약** — SubGraph는 실행만 중첩하며 부모/자식 변수의 명시적 입출력 매핑이 없다.
4. **값 표현력** — 포트는 모두 실행 포트이고 값은 노드 Inspector의 상수 또는 문자열 Blackboard 참조에 머문다.

권장 전략은 **FlowGraph를 즉시 범용 Blueprint 복제품으로 바꾸지 않는 것**이다. 이 프로젝트에서 FlowGraph의 1차 책임은 플래그·이벤트·대화·퀘스트·연출을 잇는 이벤트 기반 오케스트레이션이다. 먼저 NodeCanvas 수준의 저작/탐색/디버깅 완성도를 확보하고, 이후 Blueprint에서 검증된 “타입 핀·함수화·Watch” 중 프로젝트에 필요한 부분만 도입한다.

```text
P0  저작 마찰 제거
    검색/Explorer → 연결 UX → 검증/자동 수정 → 실행 Trace
                         │
P1  계약 기반 표현력
    타입 안전 ValueRef → SubGraph 입출력 매핑 → 선택 영역 SubGraph화
                         │
P2  수요 기반 BP-lite
    데이터 핀/Pure 노드 → 변환 규칙 → 함수/매크로 성격 분리
```

---

## 2. 조사 범위와 판단 기준

### 프로젝트에서 확인한 범위

- 런타임: `Assets/02.Scripts/FlowGraph/`
- 에디터: `Assets/02.Scripts/FlowGraph/Editor/`
- 외부 브릿지: `Assets/02.Scripts/Gameplay/TriggerSystem/FlowGraphTriggerBridgeNodes.cs`
- 변환 도구: `Assets/02.Scripts/Editor/FlowGraphComposerConverter.cs`
- 테스트: `Assets/Tests/EditMode/FlowGraph/FlowGraphSerializationTests.cs`
- 실제 에셋: 현재 추적 파일 기준 `Assets/10.Datas/Test/FLOW_.asset` 1개

### 평가 축

| 축 | 질문 |
|---|---|
| 발견성 | 원하는 노드·그래프·변수·참조를 이름을 정확히 몰라도 찾을 수 있는가? |
| 조작 비용 | 연결 변경, 삽입, 정리, 재사용에 필요한 클릭과 재배선이 적은가? |
| 읽기성 | 멀리서도 실행 흐름, 분기 의미, 값의 출처를 읽을 수 있는가? |
| 표현성 | 상수/변수/노드 결과/SubGraph 인자를 타입 안전하게 조합할 수 있는가? |
| 디버깅 | 어떤 인스턴스가 왜 이 경로를 택했는지 시간 순으로 재현할 수 있는가? |
| 안전성 | 저장 전에 구조·참조·타입·순환 문제를 잡고 안전하게 수정할 수 있는가? |
| 확장성 | 외부 asmdef의 노드가 코어 경계를 깨지 않고 자연스럽게 참여하는가? |

---

## 3. 현재 구현 상태

### 3.1 이미 구현됨 — 재제안하지 않을 항목

| 영역 | 현재 구현 |
|---|---|
| 기본 캔버스 | Zoom/Pan/Marquee, 미니맵, 노드 이동, 다중 선택 |
| 노드 생성 | 좌측 카테고리 라이브러리, 검색 창, 포트를 빈 공간에 놓아 생성 후 자동 연결 |
| 그래프 편집 | 연결/삭제, Undo/Redo, 노드·내부 연결 복사/붙여넣기, 그룹 |
| 노드 가독성 | 카테고리 색/아이콘, 인라인 요약, 컴팩트 모드, 사용자 라벨/메모 |
| Blackboard | Bool/Int/Float/String, 기본값, 검색, 사용처 이동, Rename 연동, 사용 중 삭제 경고 |
| 탐색 | 에셋 더블클릭, 그래프 열기 메뉴, SubGraph 더블클릭, 뒤로 가기와 breadcrumb 라벨 |
| 검증 표시 | Error/Warning 목록, 상태바, 문제 노드 배지, 이슈 클릭 포커스 |
| 런타임 시각화 | 활성 노드, 최근 실행 잔광, 통과 Edge, Wait 진행률, 실행 Blackboard |
| 디버그 진입 | 노드 브레이크포인트 토글, Play Mode Manual Entry 실행 |
| 데이터/경계 | `[SerializeReference]` 노드, 실행 상태의 Context/Runner 분리, Core/Data/Contracts만 참조하는 런타임 asmdef |

### 3.2 현재 표현 모델

```text
FlowGraphSO
├─ List<FlowNode>          [SerializeReference]
├─ List<FlowConnection>    fromNodeId/fromPort → toNodeId/toPort
├─ List<FlowVariableDef>   Bool/Int/Float/String
└─ List<FlowGraphGroup>    에디터 전용 그룹

FlowNode
├─ 고정 실행 포트 정의     이름 + Input/Output
├─ Inspector 직렬화 필드
└─ Execute(FlowToken)      Emit(port)로 실행 전파
```

`FlowPortView`의 포트 타입은 모두 `typeof(bool)`로 생성되지만 실제 의미는 데이터가 아닌 실행 펄스다. `GetCompatiblePorts`도 현재 방향과 자기 연결만 검사한다. 즉, **포트 타입·용량·연결 스키마가 아직 모델에 없다.**

Blackboard 변수는 노드 필드에 문자열 이름으로 저장된다. Drawer와 Rename 연동이 실수를 줄여 주지만, 노드 간 데이터 결과를 선으로 표현하거나 SubGraph 호출 계약으로 노출할 수는 없다.

### 3.3 현재 강점

- 런타임 데이터와 GraphView가 분리되어 있어 에디터 프런트엔드를 교체하더라도 실행 모델을 보존하기 쉽다.
- `FlowNodeMenu`, `FlowNodeStyle`, `FlowNodeCategoryStyle`로 외부 어셈블리 확장 지점이 이미 있다.
- `FlowContext`가 발화별 Blackboard와 노드 상태를 소유하여 공유 SO 오염을 피한다.
- 즉시 실행 256회 예산, SubGraph 깊이 8, 취소/Dispose 등 런타임 방어가 존재한다.
- TriggerComposer와 병존하고 기존 Action/Condition SO를 브릿지로 재사용하므로 점진 도입에 유리하다.

### 3.4 확인된 한계

| 한계 | 코드상 근거 | 사용자 영향 |
|---|---|---|
| 검색 의미가 얕음 | Library는 라벨/타입명 부분 일치, SearchWindow는 카테고리/라벨 목록 | “대화 종료 후”, “bool 비교” 같은 의도/별칭으로 찾기 어려움 |
| 연결 문맥이 약함 | 호환성은 방향/자기 노드만 검사 | 포트에서 검색을 열어도 실제 연결 가능한 의미로 좁혀지지 않음 |
| 그래프 전체 검색 없음 | 현재 그래프의 Blackboard 사용처 이동만 존재 | 노드/메모/Entry/변수/SubGraph 참조를 프로젝트 전체에서 추적하기 어려움 |
| 실행 이력이 없음 | Runner는 마지막 노드/Edge 시각만 보관 | 분기가 왜 선택됐는지, 직전 값이 무엇이었는지 재현 불가 |
| 브레이크포인트가 거침 | `Debug.Break()`만 호출 | Resume/Step Over/Step Into, Watch, 조건부 중단 불가 |
| 실행 인스턴스 선택 없음 | 같은 그래프 Runner 중 처음 찾은 인스턴스를 사용 | 동일 그래프를 여러 오브젝트가 실행할 때 관찰 대상이 모호함 |
| SubGraph 계약 없음 | `subGraph`, `entryId`, `waitForCompletion`만 존재 | 부모 값을 인자로 전달하거나 결과를 명시적으로 돌려받지 못함 |
| 값 종류가 제한적 | Blackboard가 4개 기본 타입만 지원 | Actor/Object/Enum/Vector, 선택 결과, 이벤트 Payload 표현이 어려움 |
| 데이터 흐름이 보이지 않음 | 실행 포트만 존재 | 값의 출처가 Inspector와 문자열 참조 안에 숨음 |
| 정적 검증이 제한적 | null/고아 Edge/진입점/일부 필드/변수만 검사 | 중복 ID, 없는 포트, 무대기 순환, 중복 graphId/entryId, SubGraph 순환을 저장 전에 놓침 |
| 자동 검증 근거가 얕음 | EditMode 테스트 3개만 추적됨 | Runner, 비동기 노드, Join/Gate/SubGraph, 디버그 Trace 회귀 보호 부족 |
| 실제 사용 데이터가 적음 | 추적된 FlowGraphSO 에셋은 Test 폴더 1개 | 대규모 기능 확장 전 실제 콘텐츠 수직 슬라이스가 필요 |

> `FLOWGRAPH_SYSTEM_GUIDE.md`에는 PlayMode 테스트 경로가 기술되어 있으나, 2026-07-26 추적 파일 기준 FlowGraph 전용 PlayMode 테스트는 확인되지 않았다. 문서와 실제 검증 상태를 맞춰야 한다.

---

## 4. 레퍼런스에서 가져올 것

### 4.1 NodeCanvas

NodeCanvas는 FlowGraph와 동일한 Unity 저작 환경에서 “작은 모듈을 그래프로 조립한다”는 점에서 1차 레퍼런스다.

| NodeCanvas의 교훈 | FlowGraph 적용 |
|---|---|
| 실행 중 노드/연결 상태를 색과 상태로 표시하고 Live Edit 지원 | 현재 하이라이트를 Trace/상태/값까지 확장하되, 구조 변경의 안전 범위를 명시 |
| 포트에서 빈 공간으로 드래그하면 노드 생성 후 연결 | 이미 구현됨. 다음은 포트 문맥에 따른 후보 필터와 연결 스키마 |
| Branch 이동, 연결 재링크, 노드/Task 교차 그래프 복사 | 연결 재링크·노드 사이 삽입·선택 영역 SubGraph화를 우선 |
| Graph Explorer가 노드·연결·Task·변수 참조를 평면 검색 | 프로젝트 전체 FlowGraph Explorer와 참조 검색 도입 |
| SubGraph 공개 변수를 부모 변수에 Write In/Read Out 매핑 | `SubGraphInterface`와 명시적 In/Out/InOut 매핑 도입 |
| Action/Condition Task를 작은 재사용 단위로 분리 | 노드 클래스를 폭증시키기보다 공통 조건/액션의 카탈로그·템플릿·브릿지 강화 |

참고:

- [NodeCanvas Controls & Shortcuts](https://nodecanvas.paradoxnotion.com/documentation/?section=controls-shortcuts)
- [NodeCanvas Visual Debugging](https://nodecanvas.paradoxnotion.com/documentation/?section=visual-debugging)
- [NodeCanvas Graph Explorer](https://nodecanvas.paradoxnotion.com/documentation/?section=using-the-graph-explorer)
- [NodeCanvas Sub Graph Variable Mapping](https://nodecanvas.paradoxnotion.com/documentation/?section=mapping-sub-graph-variables)

### 4.2 Unreal Engine Blueprint

Blueprint는 최종적인 표현성과 대규모 그래프 UX의 레퍼런스다. 단, UPlayground FlowGraph는 완전한 객체 지향 비주얼 언어가 아니므로 필요한 패턴만 선택한다.

| Blueprint의 교훈 | FlowGraph 적용 |
|---|---|
| 핀에서 끌어 연 메뉴는 연결 가능한 노드만 표시 | 실행/데이터 포트 스키마 기반 Context Sensitive Search |
| Alt+Click 연결 해제, Ctrl+Drag 연결 이동, Reroute | 배선 정리와 재배선 비용 축소 |
| 탭·앞/뒤·breadcrumb로 중첩 그래프 탐색 | 여러 그래프 탭과 실제 History 스택 |
| 함수/매크로/Collapsed Graph로 반복 로직 캡슐화 | 선택 영역을 SubGraph로 추출하고 입출력 계약 자동 생성 |
| Breakpoint, Watch, Execution Trace, Call Stack, Step | FlowGraph 전용 Debug Session/Trace/Watch/Step |
| Local 변수와 외부에 노출되는 변수 구분 | Graph local과 SubGraph public parameter 분리 |
| 프로젝트 전체 Find | graphId, Entry, 노드 필드, 변수, 메모, SubGraph 역참조 검색 |

참고:

- [Blueprint Graph Editor](https://dev.epicgames.com/documentation/en-us/unreal-engine/graph-editor-for-the-blueprints-visual-scripting-editor-in-unreal-engine)
- [Connecting Nodes / Reroute](https://dev.epicgames.com/documentation/en-us/unreal-engine/connecting-nodes-in-unreal-engine)
- [Blueprint Debugger](https://dev.epicgames.com/documentation/en-us/unreal-engine/blueprint-debugger-in-unreal-engine)
- [Blueprint Best Practices](https://dev.epicgames.com/documentation/unreal-engine/blueprint-best-practices-in-unreal-engine)

---

## 5. 목표 UX

### 5.1 화면 구조

```text
┌ Toolbar: History | Tabs | Graph | Save | Validate | Run | Debug Instance ┐
├──────────────┬──────────────────────────────────────┬─────────────────────┤
│ Palette      │ Graph Canvas                         │ Inspector           │
│ - Search     │ - Exec wires                         │ - Property search   │
│ - Favorites  │ - Typed value wires(P1)              │ - Inline errors     │
│ - Recent     │ - Reroute / Groups                   │ - Watches           │
│──────────────│                                      │                     │
│ Blackboard   │                                      │                     │
│ - Local      │                                      │                     │
│ - Parameters │                                      │                     │
├──────────────┴──────────────────────────────────────┴─────────────────────┤
│ Problems | Execution Trace | Watches | Find Results                      │
└───────────────────────────────────────────────────────────────────────────┘
```

### 5.2 대표 작업 흐름

#### 노드 추가

1. 실행 포트에서 빈 공간으로 드래그한다.
2. 해당 방향에 연결 가능한 노드만 보인다.
3. 한글명, C# 타입명, 키워드, 별칭, 설명을 통합 검색한다.
4. 선택 즉시 노드가 생성·연결되고 첫 필수 필드에 포커스한다.

#### 오류 수정

1. Problems에 오류가 Graph/Node/Field 단위로 표시된다.
2. 클릭 시 그래프를 열고 노드와 Inspector 필드를 포커스한다.
3. 안전한 항목은 `빠른 수정`을 제공한다.
4. Error가 있으면 명시적 우회 없이는 실행/배포 검증을 통과하지 않는다.

#### 런타임 원인 추적

1. Debug Instance에서 Runner를 고른다.
2. Trace에서 `Entry → Branch(False) → ...`를 시간 순으로 본다.
3. Branch 행에서 조건 입력값과 결과를 펼친다.
4. Watch한 변수/출력은 마지막 값과 갱신 시간을 유지한다.
5. 브레이크 시 Continue/Step Into/Step Over/Stop을 사용한다.

---

## 6. 개선 백로그

### P0 — 사용성·관측성 완성

#### P0-1. Node Catalog 메타데이터와 Context Sensitive Search

`FlowNodeMenuAttribute`를 깨지 않으면서 별도 메타데이터를 추가한다.

```csharp
[FlowNodeMenu("대화/PlayDialogue")]
[FlowNodeDescriptor(
    Summary = "대화를 재생하고 종료까지 대기합니다.",
    Keywords = new[] { "dialogue", "대사", "conversation", "wait" })]
public sealed class PlayDialogueNode : FlowNode
```

필수 기능:

- 라벨·타입명·카테고리·키워드·설명 통합 검색
- 최근 사용, 즐겨찾기, 프로젝트 권장 노드
- 포트에서 연 검색은 실제 호환 후보만 표시
- 검색 결과에 요약, 카테고리 색, 예상 입력/출력 표시
- 결과 선택 후 자동 연결 가능한 포트를 명시적으로 결정

연결 검색을 위해 `FlowPortDef`에 최소한 다음 메타가 필요하다.

```csharp
FlowPortKind Kind;          // Execution, Data
FlowPortCapacity Capacity;  // Single, Multi
Type ValueType;             // P1 데이터 포트용
```

P0에서는 `Execution`만 실제 사용해도 된다. 모델을 먼저 명시하면 잘못된 다중 입력/출력과 포트 이름 변경을 검증할 수 있다.

#### P0-2. 배선·정리 조작

- Alt+Click 포트: 모든 연결 해제
- Ctrl+Drag: 연결의 원본/대상 재링크
- Edge 더블클릭: Reroute 생성
- 노드를 Edge 위에 드롭: 호환 시 자동 삽입
- 선택 노드 자동 정렬: 좌/우, 상/하, 간격 균등
- `F`: 전체/선택 포커스, Home: 전체 그래프
- 그룹 접기와 그룹 단위 이동
- 모든 동작은 한 번의 Undo group으로 원복

Reroute는 런타임 노드가 아니라 에디터 라우팅 데이터로 두는 편이 적절하다. 실행 모델과 디버그 Trace를 오염시키지 않는다.

#### P0-3. FlowGraph Explorer / Find in Graphs

검색 대상:

- graphId, 에셋명/경로
- 노드 표시명, 타입, 사용자 라벨, 메모
- Manual `entryId`
- Blackboard 선언과 사용처
- 문자열/ID 필드(Flag, Quest, Story, Event)
- SubGraph 정방향/역방향 참조
- TriggerComposer 변환 원본 정보가 있다면 그 출처

결과 행은 `Graph > Group > Node > Field` 경로, 타입, 요약을 표시하고 더블클릭으로 해당 그래프/노드/필드를 연다. 첫 구현은 `AssetDatabase` 기반 온디맨드 색인으로 충분하며, 에셋 수가 늘면 변경 GUID만 증분 색인한다.

#### P0-4. 검증기 2단계화와 빠른 수정

검증을 다음 두 층으로 나눈다.

| 층 | 책임 |
|---|---|
| Runtime-safe validation | null 노드, 중복 Node ID, 없는 Node/Port, 방향/용량 위반, 직렬화 무결성 |
| Authoring validation | 도달 불가, 미설정 필드, 중복 graphId/entryId, 무대기 순환, SubGraph 순환/깊이, 변수/인자 타입 |

추가 규칙:

- `FlowConnection`의 from/to 포트가 실제 노드에 존재하는지 확인
- Single 포트의 중복 연결
- 동일 그래프 내 Node ID 중복
- 프로젝트 전체 `ResolvedGraphId` 중복
- Manual Entry ID 중복/공백 정책
- Wait/Gate 없이 닫힌 실행 사이클
- 직접/간접 SubGraph 재귀와 최대 깊이 예상
- SubGraph 대상 Entry 유실
- 사용되지 않는 Blackboard 변수
- 외부 노드 타입 유실 및 `[MovedFrom]` 의심

빠른 수정 후보:

- 고아 Edge 삭제
- 중복 Node ID 재발급과 Edge 재연결
- 사용하지 않는 변수 삭제
- 누락 기본 Entry 생성
- 선택 노드에서 선언되지 않은 변수 생성

빠른 수정은 반드시 Undo 가능해야 하며, 다중 수정 전 변경 목록을 보여 준다.

#### P0-5. Execution Trace v1

`LastNodeExecuteTimes`만으로는 원인 분석이 불가능하다. Editor/Development 전용 고정 크기 Ring Buffer를 추가한다.

```text
TraceEvent
├─ runnerInstanceId / contextId / parentContextId
├─ sequence / frame / realtime
├─ graphId / nodeId / port
├─ kind: Entry, NodeBegin, NodeEnd, Emit, Cancel, Exception
├─ outcome: Success/False/Timeout 등 선택적 태그
└─ payloadSummary: 개발 빌드용 제한된 값 스냅샷
```

요구 사항:

- 기본 비활성 또는 Editor 전용으로 런타임 비용 제한
- 최대 이벤트 수/문자열 길이 제한
- Context별 필터, Pause View, Clear
- Trace 행에서 노드로 이동
- 예외/취소/실행 예산 초과를 Error 행으로 승격
- 대화/이벤트 Payload 전체를 무제한 직렬화하지 않음

#### P0-6. Debug Instance와 Breakpoint v1.5

- 같은 그래프를 실행하는 Runner 목록을 Object/Scene 경로와 함께 표시
- 자동 선택은 Runner 1개일 때만
- 브레이크포인트 Enable/Disable/Remove 구분
- 모든 브레이크포인트 목록과 노드 이동
- 조건부 브레이크: 변수 비교 또는 N번째 실행
- 변수 Watch: 값, 타입, 마지막 변경 시각

진짜 Step은 코루틴 실행 제어 모델 변경이 필요하므로 P0 후반에 분리한다. 우선 `Debug.Break()` 의존을 제거하고 Runner가 “다음 노드 실행 전 대기”할 수 있는 Editor 전용 `FlowDebugGate`를 두는 설계를 검증한다.

---

### P1 — 타입 안전 계약과 재사용

#### P1-1. `FlowValueRef<T>` 하이브리드 값 모델

처음부터 모든 값을 Edge로 바꾸지 않는다. NodeCanvas의 BBParameter처럼 각 값 필드가 세 가지 출처를 고르게 한다.

```text
Value Source
├─ Literal        Inspector 상수
├─ Blackboard     선언 변수 참조
└─ Node Output    P1 후반 데이터 포트 연결
```

권장 지원 순서:

1. Bool/Int/Float/String
2. Enum/Vector2/Vector3
3. UnityEngine.Object 제한 타입
4. Actor/Collider 같은 Context 제공 값
5. Collection은 실제 사례가 생긴 뒤 결정

기존 `variableName` 문자열은 곧바로 삭제하지 않는다. `[SerializeReference]` 마이그레이션 안전을 위해 신규 wrapper로 읽어 들인 뒤 에디터 저장 시 명시적 업그레이드한다.

#### P1-2. SubGraph Interface와 인자 매핑

`FlowGraphSO`에 공개 입출력 선언을 추가한다.

```text
SubGraph Parameter
├─ name / stableId / type
├─ direction: In, Out, InOut
├─ defaultValue
└─ required
```

`SubGraphNode`는 부모 값을 자식 파라미터에 Write In하고 완료 후 Out을 부모에 Read Out한다. 이름이 아니라 stable ID로 매핑하여 Rename을 안전하게 한다.

완료 조건:

- 하위 그래프의 공개 인자가 노드에 동적 포트 또는 명시적 매핑 UI로 표시
- 타입이 맞는 부모 Blackboard만 후보로 표시
- 필수 In 미연결, Out 대상 없음, 타입 불일치를 검증
- `waitForCompletion=false`에서 Out 매핑 금지 또는 완료 콜백 계약을 별도 정의
- 여러 Manual Entry 발화 시 출력 병합 정책을 금지/명시

#### P1-3. 선택 영역을 SubGraph로 추출

반복되는 노드 묶음을 재사용 가능한 그래프로 바꾼다.

1. 선택 노드와 내부 Edge를 새 `FlowGraphSO`로 이동한다.
2. 경계를 가로지르는 Edge에서 Entry/Exit 인터페이스를 유도한다.
3. 외부 Blackboard 참조를 공개 파라미터 후보로 제안한다.
4. 원래 위치에 `SubGraphNode`를 만들고 재연결한다.
5. 전체 작업을 단일 Undo 또는 실패 시 완전 롤백한다.

초기 버전은 실행 입력 1개/출력 1개인 연결된 선택만 허용해 안전하게 시작한다.

#### P1-4. Typed Data Port 최소 도입

데이터 포트는 아래 사례로 한정해 수직 슬라이스를 만든다.

```text
[Get Context Actor] --Actor--> [Is Actor Type] --Bool--> [Branch]
```

규칙:

- 흰색 실행선과 타입별 데이터선을 시각적으로 분리
- Data 노드는 side effect가 없는 Pure 노드만 허용
- 자동 변환은 명시적으로 등록된 안전 변환만 허용(Int→Float 등)
- 순환 데이터 의존은 금지
- 값 평가 시점은 소비 실행 노드가 Pull하는 방식으로 고정
- Object 포트는 정확 타입/상속 호환을 검사

이 단계가 검증되기 전에는 범용 리플렉션 함수 호출 노드를 도입하지 않는다.

---

### P2 — 실제 수요가 확인된 뒤의 BP-lite

#### P2-1. Flow Function과 Flow Macro의 역할 분리

- **Flow Function**: 단일 실행 입출력, 명시적 파라미터, 재사용 에셋, Trace/Call Stack에 별도 프레임
- **Flow Macro**: 다중 실행 입출력 가능, 지연 노드 허용, 편집 시 펼쳐 보는 조직화 단위

현 `SubGraphNode`는 Function에 가까우나 다중 Manual Entry와 비동기 실행 때문에 계약이 느슨하다. 새 개념을 추가하기 전에 SubGraph Interface를 먼저 안정화한다.

#### P2-2. 노드 버전/폐기/마이그레이션 체계

표현력이 커질수록 노드 타입 변경 위험이 커진다.

- 노드 타입별 schema version
- `[FlowNodeDeprecated(replacementType)]`
- 에디터 업그레이드 미리보기
- managed reference 유실 검사
- 이전/이후 YAML 백업 또는 Undo
- 외부 asmdef 이동 시 `[MovedFrom(true, sourceAssembly: ...)]` 강제 검사

#### P2-3. Graph Compiler/IR 재평가

노드 수와 데이터 핀이 크게 늘기 전까지 현재 직접 실행 모델을 유지한다. 아래 조건이 발생할 때만 중간 표현(IR) 또는 컴파일 단계를 검토한다.

- 정적 타입 검사와 자동 변환 계획이 런타임 분기보다 복잡해짐
- 같은 Pure 식을 반복 평가해 성능 문제가 측정됨
- 그래프 버전 호환을 런타임에 해결하기 어려움
- Player Build에서 리플렉션/AOT 문제가 실제 발생

---

## 7. 구현 순서

| 순서 | 작업 | 가치 | 위험 | 선행 조건 |
|---|---|---:|---:|---|
| 1 | 포트 스키마 + 강화 검증 | 매우 높음 | 낮음 | 없음 |
| 2 | Catalog 메타/문맥 검색 | 매우 높음 | 낮음 | 포트 스키마 |
| 3 | 배선 재링크/Reroute/자동 삽입 | 높음 | 중간 | 포트 스키마 |
| 4 | Explorer/프로젝트 검색 | 높음 | 낮음 | 없음 |
| 5 | Trace v1 + Runner 선택 | 매우 높음 | 중간 | Context ID 설계 |
| 6 | Watch/조건부 Breakpoint | 높음 | 중간 | Trace |
| 7 | `FlowValueRef<T>` | 매우 높음 | 높음 | 마이그레이션 설계 |
| 8 | SubGraph Interface/매핑 | 매우 높음 | 높음 | ValueRef/stable ID |
| 9 | 선택 영역 SubGraph화 | 중간 | 높음 | SubGraph Interface |
| 10 | Typed Data Port 수직 슬라이스 | 높음 | 높음 | 포트 스키마/ValueRef |

### 권장 마일스톤

#### M1 — “찾고 고칠 수 있다”

- 포트/연결 무결성 검증
- Context Sensitive Search
- Explorer
- Reroute/재링크
- EditMode 테스트 확대

#### M2 — “왜 실행됐는지 설명할 수 있다”

- Runner 선택
- Trace
- Watch
- Breakpoint Enable/Disable/조건
- PlayMode 수직 슬라이스

#### M3 — “그래프를 계약으로 재사용할 수 있다”

- ValueRef
- 공개 Parameter
- SubGraph In/Out 매핑
- 선택 영역 추출

#### M4 — “필요한 값만 선으로 표현한다”

- Pure Data 노드 소수
- 타입/변환/순환 검사
- 실제 퀘스트 또는 보스 연출 1건으로 검증

---

## 8. 테스트와 완료 기준

### EditMode

- Node ID/Port/Edge 무결성 및 빠른 수정
- 문맥 검색 후보 정확성
- 복사/붙여넣기 시 stable ID와 내부 연결 보존
- Reroute가 런타임 연결 의미를 바꾸지 않음
- SubGraph 직접/간접 순환 검출
- Parameter Rename/타입 변경/매핑 마이그레이션
- 구버전 `variableName` → ValueRef 왕복
- `[SerializeReference]` 노드 타입/필드 보존

### PlayMode

- 동기/대기/분기/Join/Gate/SubGraph 실행
- 비활성화/씬 전환 취소와 Disposable 정리
- 같은 그래프 Runner 2개에서 Trace/Watch 분리
- 브레이크/Continue/Step 중 토큰 유실 없음
- 부모 In → 자식 실행 → Out 매핑
- 예외와 실행 예산 초과가 Trace에 남음

### 실제 콘텐츠 수직 슬라이스

다음 중 하나를 선정해 기존 방식과 비교한다.

- 보스 처치 → 플래그 → 대화 → 퀘스트 완료 → 포털 활성
- 튜토리얼 진입 → 안내 → 조건 대기 → 다음 단계

측정 항목:

- 그래프 작성 시간
- 재배선 클릭 수
- 오류 발견까지 걸린 시간
- 새 사용자가 실행 원인을 찾는 시간
- 노드/Edge 수와 SubGraph 재사용 수

### 공통 완료 조건

- Unity 컴파일 오류 0
- FlowGraph 런타임 asmdef의 Core/Data/Contracts 경계 유지
- 런타임 무가드 `UnityEditor` 참조 0
- 기존 FlowGraph 에셋 managed reference 유실 0
- Play Mode 예외/서비스 경고 0
- Player Build 오류 0

---

## 9. 데이터·아키텍처 안전 규칙

1. `FlowNode`, `FlowConnection`, 변수 선언 변경은 기존 `FLOW_*.asset` 마이그레이션 계획과 함께 구현한다.
2. `[SerializeReference]` 타입 이동/이름 변경 시 `[MovedFrom(true, sourceAssembly: "...")]`를 적용한다.
3. 포트의 영속 식별자는 표시 이름과 분리한다. 표시 이름 변경으로 Edge가 끊기면 안 된다.
4. 변수/Parameter도 표시 이름 대신 stable ID로 참조하고 Rename은 표시 정보만 바꾼다.
5. Editor 전용 Reroute/Group/Comment 데이터는 런타임 실행 의미와 분리한다.
6. 외부 시스템 호출은 계속 `Svc`/Contracts 또는 기존 TriggerAction/Condition 브릿지를 사용한다.
7. Camera 노드는 Camera 모듈의 기존 어댑터/계약을 우회하지 않는다.
8. Live Edit는 처음부터 무제한 허용하지 않는다. 값/브레이크포인트 편집과 구조 편집의 안전 등급을 나눈다.
9. Trace는 Editor/Development 한정, 고정 용량, 값 요약 원칙을 지킨다.
10. 범용 Reflection Call 노드는 AOT·권한·의존 경계 문제가 정리되기 전 도입하지 않는다.

---

## 10. 비목표

- FlowGraph로 Ability, Behavior Tree, Dialogue 내부 그래프를 대체하지 않는다.
- 일반 C# 전체를 시각 스크립팅으로 노출하지 않는다.
- 매 프레임 수학/애니메이션 계산을 FlowGraph로 옮기지 않는다.
- TriggerComposer의 단순 2~3단계 사용처를 강제로 마이그레이션하지 않는다.
- Graph Toolkit 이관을 본 고도화의 선행 조건으로 삼지 않는다.
- 실제 사용 사례 없이 배열/맵/제네릭/임의 Reflection 호출을 한꺼번에 추가하지 않는다.

---

## 11. 구현 전 결정할 사항

| 결정 | 권장안 |
|---|---|
| 포트 영속 ID | 노드 타입에 선언된 안정된 string key. 표시명과 분리 |
| 변수 참조 | stable GUID + 표시 이름 캐시 |
| SubGraph 출력 | `waitForCompletion=true`에서만 허용 |
| 여러 자식 Entry의 Out | 1차 금지. 단일 Entry만 출력 계약 허용 |
| 데이터 평가 | 소비 실행 노드에서 Pull |
| 자동 타입 변환 | 등록제, 손실 없는 변환만 기본 허용 |
| Reroute 저장 위치 | `FlowGraphSO`의 editor-only 성격 직렬화 목록 |
| Trace 보관 | Runner별 고정 Ring Buffer, Editor/Development만 |
| Live Edit | 디버그 값/Breakpoint 우선, 구조 편집은 후속 |
| GraphView 장기 전략 | 런타임 모델과 분리를 유지하고 Graph Toolkit 안정화 후 재평가 |

---

## 12. 최종 제안

가장 먼저 구현할 묶음은 **포트 스키마/검증 + 문맥 검색 + Explorer + Trace**다. 이 네 가지는 현재 실행 모델을 거의 바꾸지 않으면서도 NodeCanvas 수준의 실사용 감각을 크게 높인다.

그다음은 **SubGraph 공개 Parameter와 ValueRef**다. 이 단계가 FlowGraph의 표현성을 실질적으로 확장한다. 값의 출처와 그래프 경계를 명시함으로써 복잡한 연출을 재사용 가능한 단위로 나눌 수 있다.

마지막으로 데이터 핀과 함수/매크로를 검토한다. Blueprint의 강점은 핀 수 자체가 아니라, 타입·탐색·캡슐화·디버깅이 하나의 일관된 언어를 이룬다는 점이다. FlowGraph도 같은 순서로 기반을 쌓아야 하며, 프로젝트에서 필요한 이벤트 기반 오케스트레이션 범위를 넘는 기능은 실제 수직 슬라이스가 요구할 때만 추가하는 것이 적절하다.

---

## 13. 구현 결과 (2026-07-26)

본 문서의 우선순위를 기능 단위로 순차 구현했다. 아래 표는 제안이 아니라 현재 코드 상태다.

| 단위 | 구현 결과 |
|---|---|
| M1 포트/검증 | 실행·데이터 Kind, 타입, Single/Multi 용량, 안정 Port ID를 모델에 추가. 없는 포트·타입 불일치·중복 Edge·용량 초과·중복 Node ID를 검출하고 일부 오류는 Problems 패널에서 빠르게 수정 |
| M1 검색/배선 | 노드 Summary/Keyword, 즐겨찾기/최근 사용, 포트 문맥 호환 후보 필터, Alt 연결 해제, Edge 더블클릭/메뉴를 통한 노드 삽입, 정렬/분배 추가 |
| M1 Explorer | 프로젝트 전체 Graph/Node/변수/메모/SubGraph 참조 검색, 중복 graphId 표시, 결과에서 그래프와 노드로 직접 이동 |
| M2 디버그 | Runner 선택/고정, Context ID가 포함된 512개 Ring Trace, 실행/Emit/Blackboard/Cancel/Exception 기록, Problems·Execution Trace·Watches 탭, Continue/Step/Stop 추가 |
| M2 Breakpoint | 활성/일시 비활성, N회 도달, Blackboard 값 조건을 지원. Unity 전역 `Debug.Break()` 대신 FlowGraph Runner 내부 토큰 게이트로 일시 정지 |
| M3 안정 참조 | 변수와 공개 Parameter에 stable ID 추가. 기존 이름은 마이그레이션 호환/표시 캐시로 유지 |
| M3 SubGraph 계약 | In/Out/InOut Parameter, 부모 Blackboard Binding, 필수/타입/Entry/출력 대기 정책 검증, 부모→자식 입력 및 자식→부모 출력 반영 |
| M3 추출 | 연결된 선택 영역을 새 SubGraph 에셋으로 옮기고 단일 입출력 경계를 재배선. 사용 변수는 InOut Parameter/Binding으로 생성하며 Undo와 실패 롤백 적용 |
| M4 데이터 포트 | Pull 평가 방식의 `FlowDataNode`, 컨텍스트 단위 순환 가드, `Context Actor → Is Actor Type → Branch (Data)` 수직 슬라이스 추가 |
| 탐색 이력 | SubGraph breadcrumb에 뒤로/앞으로 이동 이력을 추가 |
| 자동 검증 | EditMode에 포트/ID/Typed Data/NodeOutput 검증 추가. PlayMode에 `Manual Entry → SetVariable → Trace` 수직 슬라이스 테스트 어셈블리 추가 |

### 구현된 주요 파일

- 런타임 모델: `FlowNode.cs`, `FlowGraphSO.cs`, `FlowContext.cs`, `FlowVariables.cs`
- 실행/디버그: `FlowGraphRunner.cs`
- SubGraph 계약: `Nodes/SubGraphNode.cs`
- Typed Data 수직 슬라이스: `Nodes/CoreNodes.cs`
- 편집기: `Editor/FlowGraphEditorWindow.cs`, `FlowGraphView.cs`, `FlowGraphExplorerWindow.cs`
- 검증: `Editor/FlowGraphValidator.cs`
- 테스트: `Assets/Tests/EditMode/FlowGraph/FlowGraphSerializationTests.cs`, `Assets/Tests/PlayMode/FlowGraph/FlowGraphVerticalSliceTests.cs`

### 검증 결과

- `UPlayGround.FlowGraph.csproj`: 컴파일 오류 0
- `UPlayGround.FlowGraph.Editor.csproj`: 컴파일 오류 0
- `UPlayGround.FlowGraph.Tests.csproj`: 컴파일 오류 0
- `UPlayGround.FlowGraph.PlayModeTests.csproj`: 컴파일 오류 0
- 런타임 asmdef 참조는 기존 `Core/Data/Contracts` 경계를 유지

CLI 컴파일은 Unity Test Runner의 실제 실행을 대체하지 않는다. 완료 판정 전 Unity 6에서 EditMode/PlayMode 테스트 실행, 기존 `FLOW_*.asset` managed reference 확인, 실제 퀘스트 또는 보스 연출 1건 적용, Player Build 재검증이 필요하다.

### 의도적으로 후속 수요까지 보류한 범위

- 범용 Reflection Call, 배열/맵/제네릭 데이터 포트
- 임의 자동 타입 변환. 현재는 정확 타입/상속 호환만 허용
- 별도 Flow Macro 언어와 Graph Compiler/IR
- 무제한 Play Mode 구조 Live Edit

이는 구현 누락이 아니라 본 문서의 비목표와 P2 도입 조건에 따른 경계다. 현재 SubGraph는 명시적 Parameter와 Trace 문맥을 가진 Function 성격의 재사용 단위로 운용한다.

### 미사용·레거시 정리 감사

2026-07-26 코드/에셋 참조 감사를 수행해 다음을 정리했다.

- 실제 노드 필드에서 사용되지 않고 Typed Data Port와 역할이 겹치던 `FlowValueSource`/`FlowValueRef` 제거
- 사용처가 없던 `FlowPortDef.Name` 호환 별칭 제거. 영속 포트 식별자는 `Id`만 사용
- 생성 시 기록만 하고 소비되지 않던 `FlowContext.StartedTime` 제거
- 노드 클래스는 코드에서 직접 생성되지 않더라도 `TypeCache` 카탈로그와 `[SerializeReference]` 에셋이 소비하므로 단순 참조 횟수만으로 제거하지 않음
- Trigger Action/Condition 브릿지는 Composer 변환기와 기존 `FLOW_.asset`에서 사용하므로 유지

이름 기반 `variableName`, `parameterName`, `parentVariableName` fallback은 아직 레거시 제거 대상이 아니다. 현재 기존 `Assets/10.Datas/Test/FLOW_.asset`이 stable ID 이전 형식이므로, Unity에서 에셋을 마이그레이션·저장하고 ID 누락 0을 확인하기 전에는 제거하면 참조가 끊어진다.
