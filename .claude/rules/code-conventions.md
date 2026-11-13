---
paths:
  - "Assets/02.Scripts/**"
  - "Assets/Tests/**"
---

# 코드 작성 원칙과 컨벤션

코드는 요청을 만족시키는 순간이 아니라 6개월 뒤 다른 사람이 열었을 때 평가된다. 판정 표·체크리스트·근거는 [코드 작성 지침](../../Assets/docs/guide/CODE_AUTHORING_GUIDE.md)이 정본이며, 코드를 새로 쓰거나 구조를 바꿀 때 먼저 읽는다. 아래는 항상 적용하는 최소 규칙이다.

## 원칙

- **하드코딩 지양.** 기준은 "값을 바꾸려고 코드를 고치고 재컴파일하게 만들지 않는다"이다. 밸런스·연출 수치는 SO(`Assets/10.Datas/`)로, 인스턴스별 값은 `[SerializeField]`로 뺀다. 액터·스테이지 **이름으로 분기하는 코드는 금지**. 기획이 만질 일 없는 계약값의 과잉 데이터화도 하지 않는다.
- **이름은 주석보다 강하다.** 메서드는 동사로 시작, `bool`은 `Is`/`Has`/`Can`, 새 약어 발명·부정형 이름 금지. 이름이 길어지면 함수를 쪼갠다.
- **주석은 코드가 말하지 못하는 것만.** 클래스·공개 메서드에 `/// <summary>` 한 줄 기능 요약과 비직관적 결정의 근거. 번역 주석·구획 장식·죽은 코드 금지. 히스토리성 주석은 재발 방지·제거된 시스템 표식·직렬화 호환 제약만, 날짜·작업자 주석 금지.
- **일반화는 세 번째 반복에서.** 수단은 데이터화 → 컴포넌트 조합 → 인터페이스 → 상속 → 제네릭 순, 데이터화가 1순위.
- **핫패스 성능.** `Update`류, KCC 콜백, `UpdateState`, 히트 판정, BT 평가에서 `GetComponent`·`Find*`·`Camera.main`·LINQ·매 프레임 할당·문자열 결합 금지. 액터별 `Update` 대신 `IManagedTick`으로 `AgentTickManager`에 등록. 진단 로그는 `RuntimeLog.Trace`/`TraceThrottled`. 가독성을 해치는 미세 최적화는 프로파일러 근거가 있을 때만.
- **초보자가 읽을 수 있게.** 한 함수 한 가지 일, 중첩 3단계까지(조기 반환), 부수효과를 이름에 숨기지 않음, 매직 불리언 대신 enum.

## 컨벤션

- 폴더 숫자 접두사(`01.Scenes`, `02.Scripts` 등)로 Project 창 정렬.
- 상태 `{액터타입}{액션이름}State` (예: `PlayerDashState`), 컴포넌트 `{액터타입}{기능}` (예: `PlayerCombat`). 적 비행 상태는 `State/Enemy/EnemyFlying/`.
- 네임스페이스는 `UPlayGround` 루트 아래 폴더 경로를 따른다 (`Manager/` → `UPlayGround.Manager`). 신규 파일은 네임스페이스 필수. `UPlayGround.Object` 네임스페이스 때문에 static 코드에서는 `UnityEngine.Object`를 명시한다.
- UI: `UI_Base` 계열 화면 클래스만 `UI_` 접두사와 소속 `CanvasLayer`를 쓴다 (`UI_HUD_{}` / `UI_Scene_{}` / `UI_Popup_{}` / `UI_System_{}`). 이름 본문에 레이어 단어를 중복하지 않는다. 공통 베이스(`UI_Base`, `UI_SceneBase`, `UI_PopupBase`) 외 UI 보조 클래스는 `UIXxx`. 프리팹 이름은 컴포넌트 클래스 이름과 같게.
- 대형 클래스(GameManager, InputManager, GameObjectManager, CheatManager 등)는 `클래스명.기능.cs` partial 분리.

## 툴 런처

사람이 쓰는 도구만 노출한다. 에디터 도구 실행 메서드에는 항상 `[UPlaygroundTool("ID")]`를 붙이고 Unity 상단 메뉴에 올리지 않는다.

- `Assets/02.Scripts/Tool/Editor/UPlaygroundToolsLauncher.cs`의 `s_categories`에 카테고리·요약·상세 설명과 함께 올리는 것은 사람이 반복해서 여는 도구뿐이다 — 편집기·저작 창, 런타임 모니터·디버그 창, 사람이 데이터를 고친 뒤 돌리는 상시 검증·동기화, 플레이 테스트 진입점.
- **등록하지 않는다:** 씬 제작·배치 적용, 일회성 마이그레이션, 기본 에셋 생성, AI 작업 결과 확인용 검증(Play Mode 자동 검증, 빌드 검증, 조사·감사·검수 촬영), 특정 시나리오·캐릭터 전용 스크립트. 이런 도구는 `UPlaygroundToolCatalog.TryExecute("ID", out string error)`(MCP `execute_code`)나 batchmode로 실행한다.
- 사용자 테스트 명세가 안내하는 일회성 진입점(예: `P04 아린 시작`)은 런처에 올리되 테스트가 끝나면 내린다.
- `UPlayGround` 최상위 개별 메뉴는 툴 런처와 허용된 예외 외에 새로 추가하지 않는다.
