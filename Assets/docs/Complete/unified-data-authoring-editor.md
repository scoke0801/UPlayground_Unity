# 통합 데이터 저작 에디터 설계 (Data Authoring Hub)

> UI Toolkit로 저작하는 **단일 데이터 저작 허브** 설계 문서.
> 현재 프로젝트에 창(EditorWindow) 단위로 흩어져 있는 콘텐츠 데이터 저작 도구 — 아이템, 퀘스트, 제작(레시피), 드랍 테이블, NPC, 사운드, 가이드 등 —
> 을 **하나의 좌측 도메인 내비게이션 + 공용 목록/상세(master-detail) 셸**로 통합하고, 도메인별 편집기를 이 셸에 꽂히는 **패널 모듈**로 재편한다.
> 동시에 세 편집기(`ItemEditorWindow` / `QuestEditorWindow` / `RecipeEditorWindow`)에 통째로 복제된 목록·툴바·생성 팝업·CRUD 로직을 공용 기반 클래스로 뽑아내 UIX를 일관화한다.
> 기존 메뉴 경로와 개별 창은 마이그레이션 동안만 유지하고, 도메인 이식이 끝나면 제거한다.

---

## 1. 배경 & 목표

### 1.1 현재 데이터 저작 도구의 분산 현황

콘텐츠 수치·정의 데이터를 편집하는 도구가 도메인마다 **독립 `EditorWindow`**로 존재하고, 같은 도메인 안에서도 "편집기 / 생성기 / 임포터 / DB 인스펙터 / 검증"이 다시 쪼개져 있다.

| 도메인 | 편집기 (per-asset) | 생성기 (bulk) | 임포터/기타 | DB 인스펙터 | 데이터 타입 |
|--------|--------------------|---------------|-------------|-------------|-------------|
| 아이템 | `ItemEditorWindow` | `ItemDataGeneratorWindow` | — | `ItemDatabaseEditor` | `ItemSO`/`EquipmentSO`, `ItemDatabase` |
| 드랍 | `DropTableEditorWindow` | — | — | `EnemyDropTableEditor` | 드랍 테이블 |
| 퀘스트 | `QuestEditorWindow` | (메인/서브 스토리 생성기 경유) | — | `QuestDatabaseEditor` | `QuestSO`, `QuestDatabase` |
| 제작 | `RecipeEditorWindow` | `RecipeDataGeneratorWindow` | `RecipeDataImporter`(CSV) | — | `RecipeDatabase`/`RecipeData` |
| NPC | — | `NpcDataGeneratorWindow` | — | (액터 DB 경유) | `ActorDefinitionSO`(NPC) |
| 액터 | `ActorDatabaseEditorWindow` | — | 몬스터 데이터 가져오기/내보내기 | `ActorDefinitionSOEditor` | `ActorDefinitionSO` |
| 스탯 | `StatDatabaseEditorWindow` | `StatDataGeneratorWindow` | — | `ActorStatSOEditor` | `ActorStatSO` |
| 사운드 | — | — | — | `SoundDatabaseSOEditor` | 사운드 DB |
| UI 가이드 | `GuidePopupDataEditorWindow` | — | — | — | 가이드 팝업 데이터 |
| 검증(횡단) | — | — | — | — | `DataValidationHubWindow` |

*(진입점은 `UPlaygroundToolsLauncher`가 카탈로그로 모아 두었으나, 이는 "메뉴 런처"일 뿐 창은 여전히 분리 실행된다.)*

### 1.2 핵심 문제

1. **UIX 파편화** — 도구마다 툴바 위치·필터 방식·생성 흐름·단축키가 제각각이라, 아이템→퀘스트→레시피를 오가는 콘텐츠 작업이 매번 다른 창·다른 조작을 요구한다.
2. **코드 중복** — `ItemEditorWindow`(741줄) · `QuestEditorWindow`(1153줄) · `RecipeEditorWindow`는 **거의 동일한** 구조를 각자 복제한다:
   - 좌우 2패널(목록 `ListView` / 상세 `VisualElement`)
   - 툴바: `+ 새 X` · 복제 · 삭제 · 타입/카테고리 필터 `ToolbarToggle` 그룹 · `ToolbarSearchField` · DB 갱신 · 새로고침
   - `AssetDatabase.FindAssets("t:...")` 로드 → ID 정렬 → 중복 ID `HashSet` 검출
   - 생성 팝업(`_createPopup`) · 선택 버튼 활성/비활성(`UpdateSelectionButtons`)
3. **편집기 ↔ 생성기 ↔ 검증 단절** — 대량 생성(생성기)과 개별 편집(에디터)과 정합성 검증(허브)이 서로 다른 창이라, "생성 → 다듬기 → 검증"이 하나의 흐름으로 이어지지 않는다.
4. **교차 참조 저작 불편** — 퀘스트 보상/레시피 재료/드랍 테이블이 모두 `ItemSO`를 참조하는데, 아이템 피커 팝업이 창마다 재구현되어 있고 아이템을 만들려면 다른 창으로 이동해야 한다.

### 1.3 목표

1. **단일 진입점** — `데이터 저작 허브` 창 하나에서 좌측 도메인 트리로 액터·아이템·퀘스트·제작·드랍·NPC·스탯·사운드·가이드 등을 오간다.
2. **공용 셸 + 도메인 모듈** — 목록/상세/툴바/생성/CRUD/검증 배지를 공용 셸이 제공하고, 도메인은 "무엇을 로드하고 어떻게 상세를 그리는가"만 구현한다.
3. **UIX 일관화 (UI Toolkit)** — 모든 도메인이 동일한 레이아웃·단축키·필터·검색·인라인 검증 배지를 공유한다.
4. **생성·편집·검증 통합 흐름** — 생성기/임포터를 도메인 패널의 액션으로 흡수하고, `DataValidationHub`의 이슈를 상세 패널 인라인 배지로 노출한다.
5. **점진 마이그레이션** — 도메인을 하나씩 셸로 이식하는 동안 개별 창을 얇은 리다이렉트로 전환하고, 통합 완료 후 리다이렉트와 중복 메뉴를 제거한다.

### 1.4 비목표 (Non-goals)

- **런타임 데이터 편집 아님.** 저작은 에디터 전용. 런타임 모니터(`ActorRuntimeMonitor` 등)는 별도 유지.
- **밸런스/전투 튜닝 도구 흡수 아님.** `BalanceDesigner`·`CombatFrameDataWindow`·`Ability Editor`·`MotionSet 에디터`는 성격(시뮬레이션·타임라인)이 달라 이번 통합 범위에서 제외한다. 단 진입 링크는 허브에서 제공 가능.
- **DialogueGraph·FlowGraph 대체 아님.** 노드 그래프 저작(`node-flow-graph-system.md`)은 별도 시스템. 허브는 퀘스트/스토리 "데이터"를 편집하고, 흐름 배선은 FlowGraph가 담당한다.
- **Odin Inspector 도입 아님.** 외부 의존성 없이 순수 UI Toolkit로 구현한다(3.1 근거).
- **새 asmdef 강제 아님.** 에디터 전용 코드이므로 기존 에디터 asmdef 경계 안에서 재편한다(중복 제거가 목적이지 모듈 신설이 목적이 아님).

---

## 2. 웹 리서치 요약

### 2.1 통합 데이터 에디터 접근 방식 (2026-07 기준)

| 접근 | 성격 | 판단 |
|------|------|------|
| **UI Toolkit master-detail** (`ListView` + `ObjectPicker` + 데이터 바인딩) | 좌측 목록(master) / 우측 상세(detail), `SerializedObject` 양방향 바인딩으로 즉시 동기화 | **채택.** 프로젝트의 세 편집기가 이미 이 패턴을 각자 구현 중 → 공용화만 하면 됨. 외부 의존성 0 |
| **Odin Inspector / Editor** | 어트리뷰트 기반으로 복잡한 데이터(퀘스트/인벤토리/AI/대화) 편집기를 코드 없이 구성. 유료·서드파티 | **미채택.** 유료 의존성 추가, 프로젝트가 순수 UI Toolkit 노선(Ability/MotionSet 에디터 선례)을 유지 중 |
| **SO 프레임워크(오픈소스)** | 퀘스트/제작 포함 SO 데이터 프레임워크 존재 | **미채택.** Odin 의존 + 자체 데이터 스키마 강제. 기존 `ItemSO`/`QuestSO`/`RecipeDatabase`를 버릴 수 없음 |

**핵심 시사점(리서치):**
- UI Toolkit는 `ListView`(가상화된 목록) + `ObjectPicker` + `SerializedObject` 바인딩으로 Odin 없이도 데이터-UI 자동 동기화를 제공한다 → 상세 패널에서 수동 `RegisterValueChangedCallback` 배선을 줄일 수 있다.
- **주의(리서치에서 반복 지적):** UI Toolkit `EditorWindow` + `SerializedProperty`는 **도메인 리로드/스크립트 컴파일 후 `SerializedObject` 참조를 잃는다.** 공용 셸은 `OnEnable`/도메인 리로드 콜백에서 바인딩을 재구성해야 한다(`RecipeEditorWindow`가 이미 `playModeStateChanged`로 재로드하는 것과 동일한 방어).
- Undo/Redo는 UI Toolkit 바인딩에 자동 반영되지 않으므로, CRUD·인라인 편집에 `Undo.RegisterCompleteObjectUndo`/`Undo.RecordObject`를 명시 삽입한다.

### 2.2 프로젝트 내 재사용 가능한 선례

- `UPlaygroundToolsLauncher` — 좌측 카테고리 + 우측 카드, 즐겨찾기/최근 사용(`EditorPrefs`) 패턴. 허브의 **좌측 내비게이션·최근 항목** 구현에 재활용.
- `DataValidationHubWindow` — 프로젝트 전역 검증(`EditorValidationContext.Project()`), 이슈 목록/상세 분할, MD/JSON 리포트. 허브의 **검증 탭·인라인 배지 소스**로 연동.
- `SOSpreadsheetWindow` — 다수 SO를 표로 일괄 편집. 허브의 **대량 편집(스프레드시트) 뷰** 모드로 흡수 검토.
- `BehaviorTreeEditorWindow` / `MotionSetEditorWindow` — UI Toolkit partial 분리, 증분 리프레시 노하우. 셸의 partial 구조 참고.

---

## 3. 아키텍처 설계

### 3.1 계층 구조

```
DataAuthoringHubWindow (EditorWindow, UI Toolkit)
├── LeftNav          도메인 트리 + 검색 + 최근/즐겨찾기 (Launcher 패턴 재사용)
├── DomainHost       현재 선택 도메인 패널을 호스팅 (교체 시 바인딩 재구성)
│   └── IDataDomainPanel  (도메인별 구현이 꽂히는 슬롯)
└── ValidationBar    선택 자산의 인라인 이슈 배지 (ValidationHub 연동)

DataDomainPanel<TAsset> : IDataDomainPanel   ← 공용 기반 (기존 3창의 중복 흡수)
├── BuildToolbar()        새로 만들기 / 복제 / 삭제 / 필터 토글 / 검색 / DB 갱신 / 새로고침
├── BuildListPanel()      가상화 ListView + 카운트 라벨 + 중복 ID 하이라이트
├── BuildDetailPane()     abstract — 도메인이 상세 폼 구현
├── LoadAll() / Save()    AssetDatabase.FindAssets 로드, 정렬, 중복 검출
├── Create/Duplicate/Delete   Undo 통합 CRUD
└── (선택) BulkActions()  생성기·임포터를 액션 드롭다운으로 노출
```

- **`IDataDomainPanel`** — 셸이 아는 최소 계약: `DisplayName`, `Icon`, `VisualElement Root`, `OnActivate()`, `OnReload()`(도메인 리로드/컴파일 후 재바인딩), `IEnumerable<ValidationIssue> IssuesFor(Object)`.
- **`DataDomainPanel<TAsset>`** — 목록/툴바/CRUD/필터/검색/중복검출을 제네릭으로 제공. 도메인은 다음만 구현:
  - `LoadAssets()` — `t:TAsset` 쿼리 또는 DB 기반 목록
  - `KeyOf(TAsset)` / `LabelOf(TAsset)` / `IconOf(TAsset)` — 목록 표시·중복 키
  - `BuildDetail(TAsset)` — 상세 폼(가능하면 `SerializedObject` 바인딩)
  - `Filters` — 필터 탭 정의(타입/카테고리/상태)
  - `CreateNew(spec)` — 생성 팝업 확정 시 자산 생성

### 3.2 도메인 모듈 매핑

| 도메인 패널 | 흡수하는 기존 창 | 상세 폼 핵심 | 비고 |
|-------------|------------------|--------------|------|
| `ActorDomainPanel` | ActorDatabaseEditorWindow의 Definition 편집·DB 동기화 | ActorDefinitionSO 식별/프리팹/스탯/전투/AI/NPC 연결 | 고급 DB 순서·프리팹 ID 도구는 액션 연결 |
| `ItemDomainPanel` | ItemEditorWindow + ItemDataGenerator + ItemDatabaseEditor | ItemSO/EquipmentSO 필드, 아이콘 프리뷰 | 아이템 피커의 **원본 제공자** |
| `DropTableDomainPanel` | DropTableEditorWindow | 드랍 항목·확률 | 아이템 피커 공유 |
| `QuestDomainPanel` | QuestEditorWindow + QuestDatabaseEditor | 목표 타입별 조건부 필드, 보상 아이템 피커 | 목표 컬러 코딩 유지 |
| `RecipeDomainPanel` | RecipeEditorWindow + RecipeDataGenerator + RecipeDataImporter(CSV) | 재료·언락 조건 인라인, 아이템명 실시간 | CSV 왕복을 액션으로 |
| `NpcDomainPanel` | NpcDataGeneratorWindow | NPC ActorDefinition, Talkable | 액터 도메인과 양방향 연동 |
| `StatDomainPanel` | StatDatabaseEditorWindow + StatDataGenerator | ActorStatSO | 커버리지 검증 액션 |
| `SoundDomainPanel` | SoundDatabaseSOEditor의 Entry 편집·DB 동기화 | SoundEntrySO 클립/버스/거리/제한 | 기존 DB Inspector에서도 허브 딥링크 |
| `GuidePopupDomainPanel` | GuidePopupDataEditorWindow의 목록·기본 편집 | 가이드 페이지/미디어 누락 검증 | 미디어 미리보기는 기존 전문 뷰 연결 |

### 3.3 공용 서비스

- **`SharedItemPicker`** — `ItemSO` 검색 팝업을 단일 컴포넌트로. 퀘스트 보상·레시피 재료·드랍 항목이 공유. 아이템이 없으면 팝업에서 즉시 `ItemDomainPanel`로 점프(교차 저작).
- **`AssetCrudService`** — 생성/복제/삭제 + `Undo` + `AssetDatabase.SaveAssets` + 폴더 규약(`Assets/10.Datas/<Domain>`)을 캡슐화.
- **`ValidationBridge`** — `DataValidationHub`의 이슈를 자산 GUID로 인덱싱해 상세 패널 배지에 공급. 저장 시 해당 도메인만 재검증(증분).
- **`DuplicateIdIndex<TKey>`** — `HashSet` 중복 검출을 제네릭화(현재 3창 각자 구현).

---

## 4. UI/UX 설계

### 4.1 레이아웃

```
┌───────────────────────────────────────────────────────────────┐
│ [탐색▾] [최근] [즐겨찾기]        (전역 검색)         [검증 ●3] [?]│  ← 상단 바
├──────────┬───────────────────────┬────────────────────────────┤
│ 도메인    │ 목록 (ListView)         │ 상세 (SerializedObject 폼)  │
│ ▸ 아이템  │ 툴바: +새로 복제 삭제    │  ┌ 인라인 검증 배지 ────┐   │
│ ▸ 드랍    │ 필터탭 · 검색           │  │ ⚠ itemId 중복(1004) │   │
│ ▸ 퀘스트  │ ───────────────        │  └───────────────────┘   │
│ ▸ 제작    │ • 1001 회복포션        │  아이콘 [ ]  이름 [    ]   │
│ ▸ NPC     │ • 1004 강철검 ⚠        │  타입 [장비▾] ...          │
│ ▸ 스탯    │ • ...                  │  [아이템 피커] [DB 링크]    │
└──────────┴───────────────────────┴────────────────────────────┘
```

- 3-컬럼(도메인 / 목록 / 상세). 창 폭이 좁으면 도메인 컬럼을 아이콘 레일로 축소.
- 상단 바에 **전역 검색**(도메인 교차: "강철" 입력 시 아이템·레시피·퀘스트 결과를 묶어 표시)과 **검증 배지**(전체 이슈 수 → 클릭 시 검증 탭).

### 4.2 상호작용 규약 (전 도메인 공통)

| 동작 | 단축키/방식 | 비고 |
|------|-------------|------|
| 새 자산 | `Ctrl+N` / `+ 새로 만들기` | 인라인 생성 폼(팝업 통일) |
| 복제 | `Ctrl+D` | ID 자동 증분 |
| 삭제 | `Delete` | 확인 다이얼로그 |
| 저장 | `Ctrl+S` | 도메인 증분 재검증 트리거 |
| 도메인 이동 | 좌측 트리 / `Ctrl+1..9` | |
| 참조 점프 | 상세의 참조 필드 클릭 | 해당 도메인+자산으로 이동 |

### 4.3 인라인 검증

- 상세 상단에 배지(에러/경고/정보), 목록 행에 아이콘. 소스는 `DataValidationHub`의 규칙 재사용 → 중복 로직 방지.
- 중복 ID·미해결 참조(없는 itemId 등)는 저장 없이 실시간(현재 3창의 중복 검출을 배지로 승격).

---

## 5. 마이그레이션 단계

기존 창을 깨지 않고 **도메인 단위**로 이식한다. 각 단계는 독립 컴파일·검증 가능.

- [x] **Stage 0 — 공용 셸 골격** *(2026-07-21 완료)*
  `DataAuthoringHubWindow` + `IDataDomainPanel` + `DataDomainPanel<TAsset>` + 좌측 내비(런처 패턴 이식). 도메인 0개로 빈 셸이 뜨는지 확인.
  - `DataAuthoringDomainRegistry` 팩토리 등록 방식과 도메인/자산 딥링크 진입점 추가.
  - Undo/Redo·Play Mode 왕복 시 활성 패널 `OnReload()` 재호출.
  - Unity Tundra 및 `UPlayGround.Data.Editor`/`Assembly-CSharp-Editor` CLI 컴파일 오류 0 확인.
- [x] **Stage 1 — 아이템 도메인 (레퍼런스 이식)** *(2026-07-21 완료)*
  `ItemEditorWindow` 로직을 `ItemDomainPanel`로 이동, 공용 툴바/목록/CRUD로 대체. `SharedItemPicker` 추출. 마이그레이션 중에는 기존 창을 얇은 리다이렉트로 축소했고 Stage 6에서 제거.
  - `ItemSO`/`EquipmentSO`/`ConsumableSO` 생성과 타입별 상세 폼, 검색·필터·ID 중복 표시, DB 갱신 이식.
  - `AssetCrudService`에 생성·복제·삭제와 Undo 등록, Assets 하위 폴더 생성을 공용화.
  - Unity Tundra 및 `UPlayGround.Data.Editor` CLI 컴파일 오류 0 확인.
- [x] **Stage 2 — 퀘스트 도메인** *(2026-07-21 완료)*
  목표 타입 조건부 필드·보상 피커를 상세 폼으로. `SharedItemPicker` 첫 재사용 검증.
  - 목표 카드 추가·삭제·순서 변경과 타입별 조건부 필드, 아이템 목표 피커를 이식.
  - 보상 아이템 행에서 `SharedItemPicker`를 재사용하고 미해결 itemId를 경고 표시.
  - 마이그레이션 중 `QuestEditorWindow.ShowAndSelect`를 퀘스트 도메인 딥링크로 유지했고, Stage 6에서 호출부를 허브 직접 딥링크로 교체한 뒤 제거.
  - Unity/`Assembly-CSharp-Editor` CLI 컴파일 오류 0 확인.
- [x] **Stage 3 — 제작 도메인** *(2026-07-21 구현 완료)*
  레시피 상세(재료/언락) + CSV 임포트/익스포트를 액션으로. `RecipeDataGenerator` 흡수.
  - `RecipeDatabase` 내부 직렬화 레코드를 작업 복사본으로 편집하고 명시적 저장·Undo를 지원.
  - 카테고리 필터, 검색, 생성·복제·삭제, 재료·언락 조건 편집과 `SharedItemPicker` 재사용을 연결.
  - CSV 가져오기/내보내기와 기존 레시피 생성기를 제작 도메인 액션으로 통합.
  - 마이그레이션 중 `RecipeEditorWindow.ShowWindow`를 제작 도메인 딥링크로 유지했고 Stage 6에서 제거.
  - `UPlayGround.Data.Editor` CLI 컴파일 오류 0 확인. Unity가 Play Mode인 동안 강제 새로고침은 보류했으므로 도메인 열기/저장 수동 스모크는 Edit Mode 복귀 후 확인 필요.
- [x] **Stage 4 — 드랍/NPC/스탯 도메인** *(2026-07-21 구현 완료)*
  나머지 게임플레이 도메인 이식. 스탯 커버리지 검증을 도메인 액션으로.
  - 몬스터/상호작용 드랍 데이터를 한 목록에서 필터링하고 확률·최대 수량·기대 수량 요약과 `SharedItemPicker`를 연결.
  - `NpcActorSO` CRUD·대화 연결 편집과 NPC/ActorDefinition 통합 생성기 진입을 추가.
  - `ActorStatSO`의 명시 값/기본값 폴백 편집, 누락 채우기·전체 해제와 생성기·DB 편집기·커버리지 검증 액션을 추가.
  - 기존 개별 편집기와 생성기 메뉴는 Stage 6 정리 전까지 호환 진입점으로 유지.
  - `UPlayGround.Data.Editor` CLI 컴파일 오류 0 확인. Unity 도메인 GUI 수동 스모크는 Edit Mode에서 확인 필요.
- [x] **Stage 5 — 검증·전역검색·대량편집 통합** *(2026-07-21 구현 완료)*
  `ValidationBridge` 인라인 배지, 전역 검색, `SOSpreadsheet` 대량 편집 진입을 통합.
  - 모든 도메인의 키·이름을 한 번에 검색하고 검색 결과에서 도메인 자산 또는 제작 레코드로 딥링크하도록 연결.
  - 도메인별 검증 결과를 목록 인라인 배지와 통합 결과 화면에 표시하고, 기존 `EditorValidationRegistry` 결과도 공급자 어댑터로 병합.
  - 기존 SO 스프레드시트를 허브의 대량 편집 액션으로 연결하여 중복 구현 없이 재사용.
  - `UPlayGround.Data.Editor`/`Assembly-CSharp-Editor` CLI 컴파일 오류 0, Unity Tundra 재컴파일 및 도메인 리로드 성공 확인. 허브 검색·검증·딥링크 GUI 수동 스모크는 Edit Mode에서 확인 필요.
- [x] **Stage 6 — 정리** *(2026-07-21 구현 완료)*
  통합이 끝난 개별 창과 중복 메뉴를 제거하고, 인스펙터·런처 진입점을 허브 딥링크로 단일화. 사운드/가이드 등 후순위 도메인은 후속 범위로 유지.
  - `ItemEditorWindow`·`QuestEditorWindow`·`RecipeEditorWindow`·`DropTableEditorWindow`와 각 `.meta`를 제거. 퀘스트/드랍 커스텀 인스펙터는 선택 에셋을 포함해 허브로 직접 딥링크.
  - `GeneratorToolMenu`와 툴 런처에서 아이템·제작·스탯·NPC의 중복 메뉴 및 구형 편집기 항목을 제거. 실제 생성 로직은 아직 도메인 내부 액션이 호출하므로 `DataAuthoringToolBridge` 뒤의 구현체로 유지.
  - 데이터 저작의 공개 진입점은 `UPlayGround/게임플레이/데이터 저작 허브`와 툴 런처의 허브 항목으로 단일화.
  - `Assets/docs/design/데이터허브_시안.png` 기준으로 상단 브랜드·전역 검색·검증 요약, 도메인 이슈 배지, 작업/필터/검색 계층, 카드형 목록·상세 인라인 검증 UIX를 반영. `Ctrl+K`로 전역 검색에 포커스.
  - 원본 창 제거 후 `UPlayGround.Data.Editor` 및 `Assembly-CSharp-Editor` CLI 컴파일 오류 0. Unity GUI 메뉴·딥링크 수동 스모크는 Edit Mode에서 확인 필요.
- [x] **Stage 7 — 연계 도메인 확장** *(2026-07-21 구현 완료)*
  액터·사운드·가이드 데이터를 공용 셸에 추가하고 기존 전문 도구와 양방향 진입점을 연결.
  - `ActorDomainPanel`: ActorDefinitionSO CRUD·필터·조건부 상세·인라인 검증·ActorDatabase 전체 동기화와 스탯/NPC/드랍 도메인 참조 점프 추가.
  - `SoundDomainPanel`: SoundEntrySO CRUD·버스 필터·재생/거리/동시재생 설정·검증·SoundDatabase 전체 동기화 추가.
  - `GuidePopupDomainPanel`: GuidePopupDataSO CRUD·페이지 기본 편집·미디어 누락 검증과 기존 미디어 미리보기 편집기 연결.
  - ActorDefinitionSO·SoundDatabaseSO·GuidePopupDataSO Inspector에서 허브 도메인 딥링크 제공. Actor 고급 DB 도구는 `DataAuthoringToolBridge`로 유지.
  - `UPlayGround.Data.Editor` 및 `Assembly-CSharp-Editor` CLI 컴파일 오류 0 확인. GUI CRUD·참조 점프·DB 동기화 수동 스모크는 Edit Mode에서 확인 필요.

각 단계 완료 시: 생성된 `.csproj`로 `dotnet build <에디터 asmdef>.csproj --no-restore` 컴파일 확인 → Unity에서 도메인 열기/생성/복제/삭제/저장 수동 검증.

---

## 6. 검증 계획

- **컴파일** — 에디터 asmdef별 `dotnet build --no-restore`.
- **기능 회귀(도메인별)** — 로드·필터·검색·생성·복제·삭제·저장·중복검출이 기존 창과 동일 결과인지 대조.
- **바인딩 안정성** — 스크립트 컴파일/도메인 리로드/플레이모드 왕복 후 상세 폼이 `SerializedObject`를 잃지 않는지(2.1 리스크).
- **Undo/Redo** — CRUD·인라인 편집 후 되돌리기 정상 동작.
- **교차 참조** — 아이템 피커가 세 도메인에서 동일 동작, 없는 아이템 생성 후 참조 갱신.

---

## 7. 리스크 & 완화

| 리스크 | 영향 | 완화 |
|--------|------|------|
| `SerializedObject` 리로드 소실 | 상세 폼 빈 화면/예외 | `OnReload()`에서 재바인딩, `RecipeEditorWindow`의 `playModeStateChanged` 방어 이식 |
| 도메인 특수 UI(퀘스트 목표 컬러/조건부, 레시피 인라인)를 제네릭이 못 담음 | 통합이 기능 후퇴 | 상세 폼은 도메인 자유 구현, 공용화는 목록/툴바/CRUD/필터에 한정 |
| 마이그레이션 중 두 진입점(허브/개별창) 혼란 | 저작 실수 | 이식 중에는 개별 창을 리다이렉트로 축소하고, 통합 완료 후 리다이렉트와 중복 메뉴 제거 |
| 대량 생성기 흡수 시 흐름 변화 | 기존 워크플로 붕괴 | 생성기 로직은 유지하고 진입만 도메인 액션으로. 결과물 동일성 회귀 테스트 |
| 범위 과확장(밸런스/전투/그래프까지) | 완성 지연 | 4개 게임플레이 도메인(아이템·퀘스트·제작·드랍)만 P0, 나머지 후순위 |

---

## 8. 참고

**프로젝트 내 선례**
- `Assets/02.Scripts/Tool/Editor/UPlaygroundToolsLauncher.cs` — 좌측 내비/최근/즐겨찾기 패턴
- `Assets/02.Scripts/Tool/Editor/Validation/DataValidationHubWindow.cs` — 검증 이슈 목록/상세·리포트
- 제거된 `ItemEditorWindow` · `QuestEditorWindow` · `RecipeEditorWindow` · `DropTableEditorWindow` — 통합 완료 후 삭제한 중복 원본 창
- `Assets/02.Scripts/Tool/Editor/SOSpreadsheet/SOSpreadsheetWindow.cs` — 대량 편집 뷰 후보
- `Assets/docs/TODO/node-flow-graph-system.md` — 흐름 배선은 FlowGraph 담당(범위 분리)

**웹 리서치**
- [Tutorial: Create an item management editor window with UI Toolkit (Unity Discussions)](https://forum.unity.com/threads/tutorial-create-an-item-management-editor-window-with-ui-toolkit.1147481/)
- [UI Toolkit custom editor fundamentals — ListView & ObjectPicker (GitHub)](https://github.com/gamedev-resources/ui-toolkit-custom-editor-fundamentals)
- [UI Toolkit, EditorWindow, SerializedProperty loses SerializedObject after reload (Unity Discussions)](https://discussions.unity.com/t/ui-toolkit-editor-window-scriptableobject-serializedproperty-loses-serializedobject-reference-after-reload/1686844)
- [Undo/Redo Using UI Toolkit (Editor Scripting) — Medium](https://medium.com/@brunolorenz98/unity-3d-undo-redo-using-ui-toolkit-editor-scripting-63788d08adbc)
- [Unity UIToolkit as the best inspector editor tool (Prographers)](https://prographers.com/blog/unity-uitoolkit-as-the-best-inspector-editor-tool)
