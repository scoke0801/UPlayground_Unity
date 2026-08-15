# 게임패드 UI·입력 리바인딩 시스템 스펙

> 작성일: 2026-07-23  
> 대상 버전: Unity 6 (6000.0.60f1), Input System 1.14.2, URP  
> 분류: TODO 구현 스펙  
> 적용 범위: 게임플레이 입력, UI 내비게이션, 입력 글리프, 설정 메뉴의 키 설정 하위 패널, 단일키·2키 조합 리바인딩  
> 관련 문서: `Assets/docs/Complete/INPUT_SYSTEM_GUIDE.md`, `Assets/docs/ProjectReadme.md`  
> 관련 코드:
>
> - `Assets/02.Scripts/Manager/Input/InputManager*.cs`
> - `Assets/02.Scripts/Data/Input/InputDefine.cs`
> - `Assets/02.Scripts/Contracts/GameServices.cs`
> - `Assets/02.Scripts/UI/UIManager.cs`
> - `Assets/02.Scripts/UI/UI_Base.cs`
> - `Assets/02.Scripts/UI/Scene/UI_Scene_SettingMenu.cs`
> - `Assets/02.Scripts/UI/Scene/SettingPage/UISettingPageKeyBinding.cs`
> - `Assets/02.Scripts/UI/InputPrompt/`
> - `Assets/Resources/Input/PlayerInputActions.inputactions`
> - `Assets/03.Prefabs/UI/UIRoot.prefab`

## 구현 진행 상태 (2026-07-26)

1차 수직 슬라이스에 이어 2차 작업(중재·마이그레이션·포커스 추적·EditMode 테스트)까지 구현되었다.

### 1차 (2026-07-23)

- 완료: 기본 게임패드 충돌 정리, Gamepad Control Scheme, 프로젝트 UI 액션 연결
- 완료: 공통 `UIFocusScope`, 전역 UI Cancel, 장치 연결 해제 시 fallback
- 완료: 최상위 Focus Scope 전용 Selectable/레이캐스트 잠금과 Cancel 관통 방지
- 완료: Primary/Secondary 단일키·2키 조합 캡처, 충돌 대체·취소, 필수 UI 키 보호
- 완료: 설정 메뉴 키 설정 하위 패널, 장치/카테고리 필터, 액션/장치/전체 초기화
- 완료: 임시 편집 후 Apply/Cancel, PlayerPrefs JSON 저장·로드, 글리프 변경 이벤트

### 2차 (2026-07-25)

- 완료 §9: 단일키 grace를 포함한 완전한 chord arbiter
  (`Data/Input/InputChordArbiter.cs`, `Manager/Input/InputManager.Chord.cs`).
  Unity Input System 타입을 참조하지 않는 순수 로직이라 EditMode에서 단독 검증한다.
  `InputManager`의 세 콜백 진입점은 이제 중재기를 거친 뒤에만 콜백을 디스패치한다.
- 완료 §9.3: `InputBuffer.AddInput`에 `timestamp` 인자 추가.
  grace 지연분을 되돌려 만료 기준을 원래 물리 입력 시각으로 맞춘다.
- 완료 §9.4: 레이어 변경·입력 억제 시 `InputChordArbiter.Reset`으로 보류 입력과
  provisional hold를 폐기한다.
- 완료 §13.4: 액션 GUID 우선 프로필 마이그레이션
  (`Data/Input/InputBindingProfileMigration.cs`). `InputBindingOverrideEntry.actionId` 신설,
  profileVersion 1→2. 실패·모호한 슬롯만 기본값 복구하고 프로필 전체는 유지한다.
- 완료 §15.3: `UIFocusScope`의 ScrollRect 자동 추적. 선택 항목이 뷰포트 밖이면
  최소 이동량만큼 스크롤을 따라간다(여백·보간 속도 인스펙터 노출).
- 완료 §19.1: EditMode 테스트 `Assets/Tests/EditMode/Input/`
  (`InputChordArbiterTests` 14개, `InputBindingProfileMigrationTests` 8개).
  §19.1의 필수 조합 시나리오 6종을 모두 포함한다.
- 검증 완료: Data/Contracts/UI/Assembly-CSharp/Input.Tests 보조 컴파일 오류 0,
  중재기·마이그레이션 로직 44개 단언 전부 통과(구현 소스 직접 실행).

### 3차 (2026-07-25)

- 완료: 전역 `UIFocusIndicator`를 레거시 fallback으로 축소하고
  `IUIFocusPresentation` 계약을 추가했다. 인벤토리 슬롯, 키 바인딩 행,
  일시정지 메뉴, 탭, 캐릭터 선택 카드처럼 자체 선택 연출이 있는 UI는
  공통 파란 테두리를 억제한다.
- 완료: `UIFocusNavigation` 공통 유틸리티로 유효한 Selectable만 연결하는
  수직·수평·그리드 explicit navigation을 제공한다.
- 완료: CharacterSelect는 게임패드 포커스 이동 즉시 캐릭터 프리뷰와 기존 카드
  선택 연출을 갱신하고, 잠긴 카드를 제외한 카드↔시작/취소 경계를 명시한다.
- 완료: Title, MenuPanel, Pause, SaveSlot, CommonPopup, Respawn, Setting,
  Inventory, Map, Party, Craft, Quest, MonsterCodex, RestGrowth의 초기 포커스와
  주요 영역 간 explicit navigation을 연결했다.
- 완료: Craft/Quest/Codex 동적 슬롯은 `OnSelect`만으로 상세가 갱신된다.
  Party는 포커스의 상세 선택과 Submit의 편성 변경을 분리했다.
- 완료: 지도 내부 확인/지역 상세 패널은 열기 전 포커스를 보존하고 닫을 때 복원한다.
- 완료: 빈 배경 클릭으로 포커스를 지우지 않으며 UI 이동 반복을
  0.35초 지연/0.09초 간격으로 조정했다.
- 완료: 설정의 적용 버튼은 변경을 저장한 뒤 화면을 유지하고, 적용 시점을 새 취소
  기준점으로 갱신한다. 이후 추가 편집을 취소해도 이미 적용한 키는 보존된다.
- 추가: `UIFocusNavigationTests` 3개(비활성 항목 건너뛰기, 수평 순환,
  그리드 이웃)를 EditMode 테스트 어셈블리에 등록했다.
- 검증 완료: `UPlayGround.UI.csproj --no-restore` 오류 0,
  Unity Editor 스크립트 리로드 오류 0.

### 4차 (2026-07-26, 웹 레퍼런스 재검토)

- Unity Input System의 `PerformInteractiveRebinding`/`RebindingOperation`과
  binding override 저장 권장 패턴을 재검토했다. 현재 시스템은 적용 전 임시 프로필과
  2키 조합 캡처가 필요하므로 단일 바인딩 중심의 샘플 컴포넌트로 교체하지 않고,
  `InputAction` binding override를 최종 반영하는 기존 경계를 유지한다.
- Xbox Accessibility Guideline의 전체 입력 재매핑 권고에 맞춰 Escape, Gamepad East,
  Backspace, Delete를 완전 예약 키에서 해제했다. 짧게 누르면 일반 바인딩, 0.75초 이상
  길게 누르면 각각 캡처 취소/바인딩 제거로 동작한다.
- 캡처 시작 시 대상 장치가 없거나 캡처 중 마지막 대상 장치가 분리되면 즉시 취소하고
  입력 억제를 복구한다.
- 저장 후 키 목록 갱신은 행 구조가 같으면 기존 `UIKeyBindingRow`를 재사용한다.
  키캡도 표시 내용 키를 캐시해 변경되지 않은 글리프의 자식 오브젝트를 다시 만들지 않는다.
  카테고리 전환이나 액션 구성 변경으로 행 구조가 달라진 경우에만 전체 목록을 재구축한다.

### 5차 (2026-07-26, 메뉴 계층 내비게이션)

- Xbox Accessibility Guideline 112의 일관된 메뉴 예시를 기준으로 입력 계층을 확정했다.
  **LT/RT는 전체 화면 메인 페이지**, **LB/RB는 현재 화면의 서브 탭**을 순환한다.
  참고:
  [XAG 112 UI navigation](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/112),
  [XAG 113 UI focus handling](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/113),
  [XAG 107 Input](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/107),
  [Unity Input System UI support](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.14/manual/UISupport.html).
- `UI/MainTabPrevious`, `UI/MainTabNext`, `UI/SubTabPrevious`, `UI/SubTabNext`
  의미 액션을 추가했다. 물리 기본값은 각각 Left Trigger, Right Trigger,
  Left Shoulder, Right Shoulder이며 키 설정 UI의 UI 카테고리에서도 변경할 수 있다.
- `UI_Base`가 의미 단축키 등록/해제를 공통 소유한다. 개별 화면은
  `ConfigureMainPageShortcut`과 `ConfigureTabShortcuts`로 구조만 선언한다.
  이로써 파생 클래스가 `RegisterInputEvents`의 `base` 호출을 빠뜨려도 공통 탭 입력이
  누락되지 않는다.
- 메인 페이지 순서는 지도 → 인벤토리 → 제작 → 퀘스트 → 파티 → 도감 → 설정이다.
  닫힘 트윈이 끝나기 전에 다음 페이지를 열어 중복 콜백이 발생하지 않도록, 전환 시작 시
  의미 단축키를 먼저 해제하고 현재 UI가 실제로 숨겨진 다음 목표 페이지를 연다.
- `UITabGroup.SelectRelative`는 비활성/상호작용 불가 탭을 건너뛰고 순환한다.
  단축키 전환은 EventSystem 포커스를 탭 버튼으로 옮기지 않으므로, 목록/그리드에서
  작업하던 위치를 유지한 채 분류만 바뀐다.
- 설정·인벤토리·제작·퀘스트의 서브 탭을 LB/RB에 연결했다. 인벤토리의 좌측 탭 레일은
  시각 배치와 동일한 상/하 Navigation으로 수정했고, 제작/퀘스트는 탭↔목록↔하단 동작
  버튼을 explicit Navigation으로 연결해 방향키만으로도 모든 버튼에 도달한다.
- 네 Builder에는 LT/RT·LB/RB·확인·취소의 화면 문맥 힌트를 반영했다.
  Builder 재실행 시 프리팹에도 같은 디자인 규칙이 생성된다.
- 검증 완료: Input Action JSON 파싱 및 4개 액션/바인딩 1:1 검사,
  `UPlayGround.UI` 보조 컴파일 오류 0, `UPlayGround.UI.Tests` 보조 컴파일 오류 0,
  Unity Editor 전체 스크립트 컴파일 오류 0.

### 6차 (2026-07-26, 지도 공간 UI 가상 커서)

- `UI/VirtualCursorMove` 의미 액션을 추가하고 기본값을 Gamepad Right Stick으로 연결했다.
  전체 축은 리바인딩 캡처 대상에서 제외한다는 기존 정책을 유지한다.
- `UIVirtualCursorController`를 공용 컴포넌트로 추가했다. 오른쪽 스틱이 데드존을
  넘을 때만 가상 커서 모드로 진입하고, A(`UI/Submit`)로 지도 마커의 포인터 클릭을
  발생시킨다. 실제 OS 마우스 위치는 변경하지 않는다.
- 지도 패널·필터·하단 버튼은 기존 Left Stick/D-Pad UINavigation을 유지한다.
  가상 커서 중 Navigate 입력이 들어오면 커서를 숨기고 직전 선택으로 복귀한다.
  `UIFocusScope`는 가상 커서 동안 기본 선택 자동 복원을 중지해 Submit 이중 실행을 막는다.
- 커서가 `MapViewport` 가장자리에서 바깥 방향으로 입력되면 해당 방향으로 뷰를
  자동 패닝한다. 확인 팝업이 열릴 때는 UINavigation 모드로 먼저 복귀한다.
- `UIMapPanelsBuilder`가 `MapViewport/VirtualCursor`와 공용 컨트롤러 참조를 생성하도록
  수정했으며, Builder를 실행해 `UI_Scene_Map.prefab`에도 반영했다.
- 검증 완료: `UPlayGround.UI`와 `UPlayGround.UI.Editor` 보조 컴파일 오류 0,
  Builder 완료 로그와 프리팹 직렬화 참조 확인. 전체 Editor 컴파일은 동시 작업 중인
  `UPlayGround.FlowGraph.Editor`의 `FlowPortDef.Name` 오류로 별도 차단돼 있다.

### 남은 작업

- 장치별 UI 프롬프트 표시 계층은 `UI_DEVICE_AWARE_PROMPT_SPEC.md`에 따라 코드,
  10개 프리팹 빌더, 실제 프리팹 11개까지 반영했다. `UI_Scene_PartySelect`는 독립 프리팹이
  없고 현재 등록 데이터는 `UI_Scene_PartyMenu`다.
- 토큰 기반 Input Context Stack은 아직 미구현이다. 현재 런타임은 `InputLayer`와
  리바인딩 캡처 게이트를 병행한다. 기존 UI 전환 경로를 한 번에 교체하지 않고 별도
  수직 슬라이스와 누수 검증을 마련한 뒤 도입한다.
- `InputBindingCatalogSO`는 아직 도입하지 않았고, 현재 카탈로그 메타데이터는
  `InputManager.BindingProfile.cs`의 정적 목록이 제공한다. 실제 바인딩 권위는 계속
  InputAction effective binding과 binding GUID다.
- §19.2 PlayMode 수직 슬라이스 8종. 타이틀/일시정지/설정 씬과 프리팹 부트스트랩이 필요해
  Unity 에디터에서 씬 기준을 확정한 뒤 작성한다.
- 새 `UPlayGround.UI.Tests` EditMode 3개는 Unity Test Runner에서 실행해 결과를 확정한다.
- 지도 가상 커서의 이동 속도·데드존·가장자리 패닝 속도를 실제 패드별로 체감 조정한다.
  줌은 현재 UINavigation으로 접근 가능한 확대/축소 버튼과 슬라이더 정책을 유지한다.
- §20 실제 패드 3종(Xbox / DualSense / Switch Pro) 수동 스모크와 Player Build 검증.
- §20에 걸린 미결정 사항: Switch의 Submit/Cancel 물리 위치 정책을 옵션화할지
  출시 플랫폼 정책으로 고정할지.
- Unity 에디터에서 Play Mode 실기 확인. 특히 grace 0.12초가 대시·회피 조작감을
  해치지 않는지 체감 검증이 필요하다(수치는 `InputChordArbiter.GraceSeconds`에서 조정).

---

## 1. 목적

이 작업의 목적은 현재 부분적으로 구현된 게임패드 입력을 **게임 시작부터 모든 게임플레이·UI·설정 조작까지 마우스 없이 완주 가능한 입력 시스템**으로 확장하는 것이다.

단순히 `.inputactions`에 게임패드 바인딩을 추가하는 작업으로 한정하지 않는다. 다음을 하나의 시스템으로 완성한다.

1. 게임플레이 물리 키 충돌 제거와 기본 게임패드 레이아웃 확정
2. EventSystem과 프로젝트 `InputManager`의 UI 입력 원본 일원화
3. 모든 모달·전체 화면 UI의 초기 포커스, 포커스 스택, 내비게이션 복원
4. 설정 메뉴 안의 **키 설정 하위 패널**
5. 키보드·마우스·게임패드의 단일키 및 2키 조합 리바인딩
6. 조합키와 단일키가 물리 키를 공유할 때의 런타임 우선순위 중재
7. 바인딩 충돌 검사, 교환·대체·취소 UX
8. 바인딩 override 저장·로드·초기화와 입력 글리프 즉시 갱신
9. 게임패드 연결 해제·브랜드 전환·리바인딩 중 예외 처리

---

## 2. 현재 상태와 해결해야 할 문제

### 2.1 이미 존재하며 재사용할 기반

| 기능 | 현재 구현 | 방침 |
|------|-----------|------|
| 입력 라우팅 | `InputManager.RegisterInputEvent` | 유지·확장 |
| 입력 레이어 | `InputLayer` + `UI_Base.BlocksLowerInput` | 입력 컨텍스트 토큰으로 보강 |
| 전투 선입력 | `InputBuffer` | 유지 |
| 활성 장치 감지 | `InputManager.Device.cs` | 연결 해제 처리 추가 |
| 패드 브랜드 | Xbox / PlayStation / Switch / Generic | 유지 |
| 입력 글리프 | `InputGlyphDataSO`, `InputGlyphResolver` | 바인딩 변경 이벤트 연동 |
| UI 입력 모듈 | `InputSystemUIInputModule` | 프로젝트 UI 액션 에셋으로 일원화 |
| 설정 메뉴 | `UI_Scene_SettingMenu` + `UISettingPageKeyBinding` | 빈 키 설정 페이지 구현 |
| 카메라 패드 룩 | 별도 yaw/pitch 각속도 | 감도·데드존 옵션 확장 |

### 2.2 현재 액션 에셋의 충돌

`PlayerInputActions.inputactions`에는 같은 컨텍스트에서 동시에 평가될 수 있는 물리 키 중복이 있다.

| 물리 입력 | 연결된 액션 | 문제 |
|-----------|-------------|------|
| Gamepad Right Trigger | `Interact`, `SkillUltimate` | 상호작용과 궁극기 동시 요청 가능 |
| Gamepad D-pad 4방향 | `CharacterSwap_1~4`, `QuickSlot_*` | 캐릭터 교체와 아이템 사용 동시 요청 가능 |
| Keyboard R | `Equip`, `SkillUltimate` | 두 액션 동시 요청 가능 |
| Gamepad Left Trigger | 테스트 맵의 `L2`, `R2` | `R2` 오바인딩 |

추가 누락:

- `System.Back`은 키보드 Escape만 바인딩되어 있다.
- `BossAssist`는 키보드만 존재한다.
- 게임패드 Zoom 바인딩이 없다.
- `Walk`, `Crouching`은 바인딩이 없다.
- Control Scheme은 `Keyboard&Mouse`만 정의되어 있다.

### 2.3 UI 입력 원본 분리

현재 UI 열기·닫기 액션은 `PlayerInputActions`와 `InputManager`를 사용하지만, `UIRoot`의 `InputSystemUIInputModule`은 별도 액션 에셋을 참조한다. EventSystem이 런타임 생성될 때도 `AssignDefaultActions()`를 사용한다.

따라서 현재 구조에서는 다음 세 경로가 서로 다른 바인딩을 볼 수 있다.

```text
메뉴 열기              → PlayerInputActions/UI
EventSystem 이동·확정  → UIRoot의 별도 UI Action Asset
전역 뒤로 가기         → PlayerInputActions/System/Back
```

리바인딩 이후에도 한쪽만 변경되는 문제를 막기 위해 세 경로를 하나의 `PlayerInputActions`로 통합해야 한다.

### 2.4 UI 포커스 정책 편차

일시정지, 인벤토리, 가이드 팝업은 초기 포커스를 개별 구현하지만 타이틀, 메뉴 패널, 설정, 저장 슬롯, 파티, 제작, 퀘스트, 도감 등은 공통 보장이 없다.

또한 팝업을 닫았을 때 아래 UI의 이전 선택으로 돌아가는 포커스 스택이 없다. 숨겨진 오브젝트가 `EventSystem.current.currentSelectedGameObject`로 남거나 null이 되면 게임패드 내비게이션이 멈출 수 있다.

### 2.5 키 설정 페이지 미구현

`UISettingPageKeyBinding`은 현재 빈 클래스다. Interactive Rebinding, override 저장, 중복 검사, 장치별 표시와 초기화가 구현되어 있지 않다.

---

## 3. 설계 원칙

### 3.1 단일 입력 권위

- 런타임 액션과 UI 글리프의 단일 원본은 `PlayerInputActions.inputactions`의 **effective binding**이다.
- UI 전용 복제 액션 에셋이나 코드 하드코딩 키 이름을 추가하지 않는다.
- `InputManager`와 `InputSystemUIInputModule`은 같은 액션 에셋 인스턴스를 사용한다.
- 사용자 변경값은 원본 에셋을 수정하지 않고 binding override로만 적용한다.

### 3.2 액션 의미와 물리 키 분리

게임 로직은 `<Gamepad>/buttonSouth`, `<Keyboard>/space` 같은 물리 경로를 직접 알지 않는다. 항상 `(actionMap, actionName)`으로 등록한다.

```text
게임 로직 → PlayerAction.Jump
입력 시스템 → 현재 프로필의 Jump effective binding 해석
글리프 UI → 동일 effective binding 표시
```

### 3.3 긴 조합 우선

같은 입력 컨텍스트에서 단일키 `A`와 조합키 `A+B`가 모두 존재할 경우 **더 구체적인 조합키를 우선**한다.

- `A`를 누른 뒤 조합 판정 유예 시간 안에 `B`가 눌리면 `A+B`만 발화한다.
- 두 번째 키가 오지 않으면 유예 시간 만료 후 `A` 단일 액션이 발화한다.
- 조합키가 성립하면 대기 중인 구성 단일키 콜백은 취소한다.
- 입력 버퍼에도 최종 확정된 액션만 적재한다.

Unity Input System의 `OneModifier` composite만으로는 구성 단일 액션의 동시 발화를 막지 못하므로, 이 규칙은 `InputManager`의 콜백 디스패치 전에 별도 중재 계층으로 구현해야 한다.

### 3.4 UI가 소유하는 UI 계약

- UI 포커스, 기본 선택 항목, 마지막 선택 복원은 UI 모듈이 소유한다.
- `InputManager`에 신규 `UIManager.Instance` 직접 의존을 추가하지 않는다.
- UI는 `IInputService`의 입력 컨텍스트 API를 사용하고, 입력 모듈은 UI 구현 타입을 알지 않는다.
- UI 소비자는 `UISvc`와 UI 모듈 내부 계약을 사용한다.

### 3.5 안전하게 빠져나올 수 있는 설정

- UI Cancel과 설정 메뉴 진입에 필요한 최소 바인딩은 동시에 모두 제거할 수 없다.
- 리바인딩 도중에는 Escape 또는 Gamepad East를 길게 눌러 항상 취소할 수 있다.
- 모든 변경은 적용 전 임시 프로필에 기록하며, 취소 시 원상 복원한다.
- 잘못된 프로필 로드 시 기본 바인딩으로 복구하고 경고를 남긴다.

---

## 4. 용어와 지원 범위

| 용어 | 정의 |
|------|------|
| 액션 | `PlayerAction/Jump`, `UI/Cancel` 같은 의미 단위 |
| 컨트롤 | `<Keyboard>/space`, `<Gamepad>/buttonSouth` 같은 물리 입력 |
| 바인딩 슬롯 | 한 액션이 가질 수 있는 Primary 또는 Secondary 사용자 바인딩 |
| 단일키 | 한 컨트롤만으로 구성된 바인딩 |
| 조합키 | Modifier 1개 + Trigger 1개로 구성된 2컨트롤 바인딩 |
| Modifier | 먼저 누르고 유지하는 첫 번째 컨트롤 |
| Trigger | Modifier 유지 중 눌러 액션을 확정하는 두 번째 컨트롤 |
| 입력 컨텍스트 | Gameplay, UI, Rebinding, System 등 현재 허용되는 입력 집합 |
| effective binding | 원본 path에 사용자 override를 적용한 최종 경로 |

### 4.1 V1 지원

- 키보드 키
- 마우스 버튼과 휠 방향
- 일반 게임패드 버튼, 트리거, D-pad
- 키보드 내부 2키 조합
- 마우스 버튼 + 키보드 키 조합
- 동일 게임패드 내부 2버튼 조합
- 액션별 Primary / Secondary 슬롯

### 4.2 V1 비지원

- 3키 이상 조합
- 키보드 + 게임패드 교차 장치 조합
- 축과 버튼의 임의 조합
- 스틱 방향을 Modifier로 사용하는 조합
- 매크로, 순차 커맨드, 길게 누르기 시간 사용자 편집
- 플레이 중 로컬 멀티플레이어별 독립 프로필

`Ctrl+K`는 V1 조합키지만 `A → B → X` 같은 순차 커맨드는 조합키가 아니다.

---

## 5. 목표 아키텍처

```text
PlayerInputActions.inputactions
│
├─ PlayerAction
├─ UI
├─ System
└─ Rebinding
        │
        ▼
InputManager : IInputService
├─ Action Cache
├─ Device Tracker
├─ Input Context Stack
├─ Binding Profile Store
├─ Rebinding Session
├─ Chord Arbiter
└─ Callback Router / InputBuffer
        │
        ├───────────────┬──────────────────┐
        ▼               ▼                  ▼
InputSystemUIInputModule UI Focus Coordinator InputGlyphResolver
        │               │                  │
        ▼               ▼                  ▼
Navigate/Submit/Cancel  초기/복원 포커스    effective binding 표시
                                           │
                                           ▼
                             UISettingPageKeyBinding
                             ├─ UIKeyBindingRow
                             ├─ UIKeyCapturePanel
                             └─ UIKeyConflictPopup
```

### 5.1 제안 파일 구조

```text
Assets/02.Scripts/
├─ Data/Input/
│  ├─ InputDefine.cs                         기존
│  ├─ InputBindingCatalogSO.cs               신규
│  ├─ InputBindingProfileData.cs             신규 DTO
│  └─ InputBindingPolicy.cs                  신규 enum/정책
│
├─ Contracts/
│  └─ GameServices.cs                        IInputService 확장
│
├─ Manager/Input/
│  ├─ InputManager.cs                        기존 생명주기
│  ├─ InputManager.Action.cs                 기존 액션 캐시
│  ├─ InputManager.Event.cs                  기존 콜백 라우팅
│  ├─ InputManager.Device.cs                 기존 장치 추적
│  ├─ InputManager.Context.cs                신규 컨텍스트 토큰
│  ├─ InputManager.Rebinding.cs              신규 캡처 세션
│  ├─ InputManager.BindingProfile.cs          신규 저장·로드
│  └─ InputManager.Chord.cs                  신규 조합 우선 중재
│
└─ UI/
   ├─ Focus/
   │  ├─ UIFocusCoordinator.cs               신규
   │  ├─ UIFocusScope.cs                     신규
   │  └─ UIExplicitNavigationBuilder.cs      신규
   └─ Scene/SettingPage/KeyBinding/
      ├─ UISettingPageKeyBinding.cs           구현
      ├─ UIKeyBindingRow.cs                   신규
      ├─ UIKeyCapturePanel.cs                 신규
      └─ UIKeyConflictPopup.cs                신규
```

`InputBindingCatalogSO`는 액션 표시 순서와 정책을 제공하는 데이터이며 실제 바인딩 값의 권위가 아니다. 실제 값은 액션 에셋의 effective binding이다.

---

## 6. InputActionAsset 구성 규약

### 6.1 Action Map

| 맵 | 역할 | 활성 정책 |
|----|------|-----------|
| `PlayerAction` | 이동·전투·상호작용 | Gameplay 컨텍스트에서 활성 |
| `UI` | 메뉴 열기, Navigate, Submit, Cancel, 포인터 | UI/HUD 컨텍스트에서 활성 |
| `System` | 항상 필요한 시스템 입력 | Rebinding 중 허용 목록을 제외하고 활성 |
| `Rebinding` | 캡처 취소·단일 확정 등 | Rebinding 컨텍스트에서만 활성 |
| `Gamepad` | 현재 테스트 전용 | 런타임 의존 제거 후 개발 테스트로 격리 또는 삭제 |

### 6.2 UI 표준 액션

`InputSystemUIInputModule`이 필요로 하는 액션을 프로젝트 `UI` 맵에 완성한다.

| 액션 | 타입 | 기본 입력 |
|------|------|-----------|
| `Navigate` | PassThrough / Vector2 | WASD, 방향키, Gamepad Left Stick, D-pad |
| `Submit` | Button | Enter, Space, Gamepad South |
| `Cancel` | Button | Escape, Gamepad East |
| `Point` | PassThrough / Vector2 | Mouse Position |
| `Click` | PassThrough / Button | Mouse Left |
| `RightClick` | PassThrough / Button | Mouse Right |
| `MiddleClick` | PassThrough / Button | Mouse Middle |
| `ScrollWheel` | PassThrough / Vector2 | Mouse Scroll |

기존 `System.Back`은 다음 중 하나로 정리한다.

1. 권장: `UI/Cancel`을 `UIManager.OnPerformedBack`과 EventSystem Cancel 양쪽의 단일 원본으로 사용
2. 호환: `System.Back`을 유지하되 `UI/Cancel`과 동일 binding ID를 공유하고 한쪽 콜백만 전역 닫기를 수행

두 액션이 같은 물리 입력으로 각각 닫기 콜백을 실행하는 구조는 금지한다.

### 6.3 Control Scheme

최소 두 스킴을 정의한다.

```text
Keyboard&Mouse
├─ <Keyboard> required
└─ <Mouse> optional

Gamepad
└─ <Gamepad> required
```

바인딩 그룹을 채워 장치별 필터링, 리바인딩 대상 선택, 글리프 해석에 동일한 기준을 사용한다.

### 6.4 리바인딩 슬롯

단일 ↔ 조합 형태를 runtime override만으로 안전하게 바꾸기 위해 각 재매핑 가능 액션의 각 슬롯을 미리 구조화한다.

```text
Jump / KeyboardMouse / Primary
├─ Single binding
└─ OneModifier composite
   ├─ modifier
   └─ binding

Jump / Gamepad / Primary
├─ Single binding
└─ OneModifier composite
   ├─ modifier
   └─ binding
```

- 한 시점에는 Single 또는 Composite 중 하나만 활성화한다.
- 비활성 형태는 빈 override path로 disable한다.
- Primary와 Secondary는 서로 다른 고정 binding ID를 갖는다.
- 코드가 리스트 인덱스만 저장하지 않도록 binding GUID를 영속 키로 사용한다.
- Action Asset 편집 후에도 기존 프로필을 복구할 수 있도록 `profileVersion`과 마이그레이션 테이블을 둔다.

### 6.5 리바인딩 금지 액션

다음은 V1에서 고정하거나 제한적으로만 변경한다.

- `UI/Navigate`의 개별 방향 파트
- `UI/Point`, `Click`, `ScrollWheel`
- `Rebinding/CancelCapture`
- 개발·치트 전용 액션

`UI/Submit`, `UI/Cancel`은 변경 가능하지만 각 장치군에 최소 1개씩 유효한 바인딩을 유지해야 한다.

---

## 7. 기본 게임 키 매핑

아래는 충돌 제거를 위한 **초기 권장안**이다. 실제 전투 플레이테스트 후 수치는 바꿀 수 있지만, 같은 컨텍스트의 물리 입력 중복은 허용하지 않는다.

### 7.1 게임패드

| 액션 | 기본 바인딩 | 비고 |
|------|-------------|------|
| Move | Left Stick | 고정 축 |
| Look | Right Stick | 고정 축 |
| Jump | South | Xbox A / PS Cross |
| Dash | East | Xbox B / PS Circle |
| Attack | West | Xbox X / PS Square |
| HeavyAttack | North | Xbox Y / PS Triangle |
| Guard | Left Shoulder | 홀드 |
| SkillAbility | Left Trigger | 홀드/버튼 |
| ElementBuff | Right Shoulder | 버튼 |
| Interact | Right Trigger | 버튼 |
| SkillUltimate | Left Trigger + Right Trigger | 조합 우선 적용 |
| Dodge | Left Shoulder + East | Guard 단일과 조합 중재 |
| LockOn | Right Stick Press | |
| LockOnSwitchLeft/Right | Right Stick X 방향 | 락온 중 컨텍스트 |
| CharacterSwap 1~4 | D-pad 4방향 | |
| QuickSlot 4방향 | Left Shoulder + D-pad 4방향 | 교체보다 조합 우선 |
| BossAssist | View / Select | Start와 분리 |
| MenuPanel | Start | |
| UI Cancel | East | UI 컨텍스트에서만 |

`Left Shoulder + East`를 Dodge로 쓸 때 `Dash(East)`와 `Guard(Left Shoulder)`가 함께 실행되지 않도록 Chord Arbiter가 두 단일 액션을 억제해야 한다.

Zoom은 버튼 부족과 전투 중 오작동 가능성을 고려해 다음 중 플레이테스트로 확정한다.

- View + D-pad Up/Down
- 설정 메뉴에서 카메라 거리 프리셋만 제공하고 런타임 Zoom은 키보드·마우스 전용 유지
- Lock-on 비활성 상태의 별도 입력 컨텍스트에서 조합 축 제공

완전한 게임패드 대응 완료 전까지 Zoom 정책을 미확정 상태로 방치하지 않는다.

### 7.2 키보드·마우스

현재 레이아웃을 최대한 보존하되 `R` 중복을 제거한다.

| 액션 | 기본 바인딩 |
|------|-------------|
| Move | WASD |
| Look | Mouse Delta |
| Jump | Space |
| Dash | Shift |
| Dodge | Ctrl |
| Attack | Mouse Left |
| HeavyAttack | Mouse Right |
| Guard | V |
| Interact | F |
| SkillAbility | E |
| SkillUltimate | R |
| Equip | 별도 키로 재배치 또는 제거된 게임플레이 요구 확인 |
| ElementBuff | T |
| CharacterSwap 1~4 | 숫자 1~4 |
| QuickSlot 4방향 | F1~F4 |
| BossAssist | Q |
| MenuPanel | Backquote |
| UI Cancel | Escape |

---

## 8. 입력 컨텍스트 스택

### 8.1 필요성

현재 `CurrentLayer`는 UI 차단에는 유용하지만 다음을 모두 표현하기 어렵다.

- 리바인딩 중 모든 일반 입력 차단
- UI 위에서 EventSystem만 활성
- 락온 중 우스틱 플릭과 일반 Look 분리
- 같은 물리 키가 서로 다른 문맥에서 재사용되는 경우

### 8.2 토큰 기반 API

제안 계약:

```csharp
public interface IInputContextToken : IDisposable
{
    int Id { get; }
}

public interface IInputService : IGameService
{
    InputContextType ActiveContext { get; }
    IInputContextToken PushContext(
        InputContextType context,
        InputLayer layer,
        object owner);
}
```

- `PushContext`는 토큰을 반환한다.
- UI 또는 시스템은 자신이 받은 토큰만 해제한다.
- 중첩 컨텍스트는 우선순위와 push 순서로 결정한다.
- owner가 파괴된 토큰은 개발 빌드에서 누수 경고를 낸다.
- `UI_Base.Show/Hide`는 기존 입력 레이어 재계산과 토큰을 한 전환 기간 동안 병행한 뒤 교체한다.

### 8.3 컨텍스트 우선순위

```text
Rebinding > SystemModal > Popup > SceneUI > Gameplay > Debug
```

`System`의 모든 액션이 항상 통과하는 것은 아니다. Rebinding 중에는 `CancelCapture`와 장치 변경 감지만 통과한다.

---

## 9. 조합키 런타임 판정

### 9.1 판정 규칙

조합키 `Modifier + Trigger`는 다음 조건에서 성립한다.

1. Modifier가 actuation threshold 이상으로 눌려 있다.
2. Trigger가 새로 performed 된다.
3. 두 컨트롤이 같은 허용 장치군에 속한다.
4. 현재 입력 컨텍스트에서 해당 조합 액션이 활성이다.

키를 정확히 동시에 누를 필요는 없다. Modifier를 먼저 유지하고 Trigger를 누르는 형태를 표준으로 한다.

### 9.2 단일키 유예

조합의 Modifier 또는 Trigger가 다른 단일 액션에도 쓰이면 짧은 입력을 바로 디스패치하지 않는다.

| 설정 | 기본값 | 설명 |
|------|--------|------|
| `chordGraceSeconds` | 0.12초 | 두 번째 키를 기다리는 최대 시간 |
| `buttonActuationThreshold` | 0.5 | 버튼·트리거 유효 임계값 |
| `releaseThreshold` | 0.2 | 재입력 가능 상태로 보는 해제 임계값 |

흐름:

```text
East 입력
├─ East가 조합 후보 아님 → Dash 즉시
└─ LeftShoulder+East 조합 후보 있음
   ├─ LeftShoulder가 눌림 → Dodge 즉시, Dash 취소
   └─ LeftShoulder가 없음 → 최대 0.12초 대기 후 Dash
```

Guard처럼 hold 상태가 필요한 액션은 started를 무조건 0.12초 늦추면 조작감이 나빠질 수 있다. 다음 정책을 사용한다.

- Hold 단일 액션은 started 상태를 내부에서 provisional로 기록한다.
- 조합이 성립하면 provisional hold를 외부 콜백에 노출하지 않고 폐기한다.
- grace가 끝나면 started를 확정하고, 이미 해제됐다면 performed/canceled 순서를 보정한다.
- 이 동작이 복잡한 액션은 카탈로그에서 `DisallowSharedChordPart`로 지정해 애초에 충돌 바인딩을 거부할 수 있다.

### 9.3 InputBuffer 연동

- 물리 입력 시점이 아니라 **중재 결과 확정 시점**에 버퍼에 넣는다.
- 조합이 성립하면 구성 단일 액션은 버퍼에 넣지 않는다.
- 버퍼 타임스탬프는 조합 Trigger 입력 시점을 사용한다.
- grace 지연 때문에 전투 입력 버퍼 유효 시간이 줄지 않도록 만료 기준은 원래 물리 입력 시점에서 계산한다.

### 9.4 취소와 컨텍스트 변경

UI가 열리거나 입력 컨텍스트가 바뀌면:

- 대기 중 단일키 후보 제거
- provisional hold 취소
- 진행 중 조합 상태 초기화
- 기존 `cancelCallback` 1회 호출

---

## 10. 키 설정 UI

### 10.1 설정 메뉴 계층

키 설정은 별도 최상위 화면이 아니라 설정 메뉴의 기존 키 설정 탭 안에 하위 패널로 구현한다.

```text
UI_Scene_SettingMenu
└─ UISettingPageKeyBinding
   ├─ DeviceTabs
   │  ├─ Keyboard & Mouse
   │  └─ Gamepad
   ├─ CategoryTabs
   │  ├─ 이동
   │  ├─ 전투
   │  ├─ 상호작용
   │  ├─ 카메라
   │  └─ UI
   ├─ BindingScrollView
   │  └─ UIKeyBindingRow[]
   │     ├─ ActionLabel
   │     ├─ PrimaryBindingButton
   │     ├─ SecondaryBindingButton
   │     └─ ResetActionButton
   ├─ Footer
   │  ├─ Apply
   │  ├─ Cancel
   │  └─ ResetAll
   └─ ChildOverlay
      ├─ UIKeyCapturePanel
      └─ UIKeyConflictPopup
```

`ChildOverlay`는 `UI_Scene_SettingMenu` 내부 하위 패널이다. 별도 `UIManager.ShowUI`로 생성하지 않는다. 표시 중 `Rebinding` 입력 컨텍스트 토큰을 소유해 배경 설정 UI와 EventSystem 입력을 차단한다.

### 10.2 바인딩 행

각 행은 다음 정보를 표시한다.

- 현지화 가능한 액션 표시명
- Primary effective binding 글리프/텍스트
- Secondary effective binding 글리프/텍스트
- 단일/조합 상태
- 충돌 또는 필수 바인딩 경고
- 해당 액션만 기본값 복원

게임패드 탭은 현재 연결된 브랜드 글리프를 우선 표시한다. 연결된 패드가 없으면 마지막 사용 브랜드 또는 Generic 글리프를 사용한다.

### 10.3 내비게이션

- Device 탭: LB/RB 또는 좌우
- Category 탭: 좌우
- 바인딩 행: 위아래
- Primary ↔ Secondary ↔ Reset: 좌우
- 선택된 행은 ScrollRect 안으로 자동 스크롤
- 캡처·충돌 팝업을 닫으면 원래 바인딩 버튼으로 포커스 복원

Automatic Navigation에만 의존하지 않는다. 행 재생성 후 `UIExplicitNavigationBuilder`가 실제 활성 행 기준으로 이웃을 다시 계산한다.

---

## 11. 단일키·조합키 캡처 UX

### 11.1 진입

사용자가 Primary 또는 Secondary 버튼을 Submit/클릭하면:

1. 현재 바인딩 프로필 스냅샷 생성
2. `Rebinding` 컨텍스트 push
3. EventSystem Navigate/Submit 일시 차단
4. 캡처 진입에 사용된 키가 완전히 release될 때까지 대기
5. `UIKeyCapturePanel` 표시

캡처 진입 버튼을 누른 동일 입력이 새 바인딩으로 즉시 잡히지 않아야 한다.

### 11.2 표시 문구

초기:

```text
새 키를 입력하세요
한 개만 누르면 단일키, 첫 키를 누른 상태에서 다른 키를 누르면 조합키가 됩니다.
[Esc / B 길게] 취소
```

첫 번째 키 입력 후:

```text
[Left Shoulder] + …
다른 키를 누르면 조합키
첫 키를 놓고 잠시 기다리면 단일키로 확정
```

두 번째 키 입력 후:

```text
[Left Shoulder] + [East]
조합키 확인 중…
```

### 11.3 캡처 상태 머신

```text
Idle
  ↓
WaitForNeutral
  ↓
WaitForFirstControl
  ├─ Cancel → Canceled
  └─ First 입력
       ↓
WaitForSecondControl
  ├─ 서로 다른 유효 컨트롤 입력 → ChordCaptured
  ├─ 첫 키 release + singleConfirmDelay 만료 → SingleCaptured
  ├─ 전체 captureTimeout 만료 → Canceled
  └─ Cancel → Canceled
       ↓
Validate
  ├─ 충돌 없음 → PreviewApplied
  ├─ 충돌 있음 → ConflictPopup
  └─ 금지 입력 → WaitForFirstControl + 오류 표시
```

권장 기본값:

| 설정 | 값 |
|------|----|
| 전체 캡처 제한 | 10초 |
| 첫 키 이후 두 번째 키 대기 | 1.25초 |
| 첫 키 release 후 단일 확정 | 0.35초 |
| 아날로그 버튼 유효 임계값 | 0.5 |

### 11.4 “특정 키를 누른 후 다른 키”의 의미

- 캡처할 때는 첫 키와 두 번째 키를 순서대로 인식한다.
- 런타임에서는 첫 키가 Modifier, 두 번째 키가 Trigger가 된다.
- 런타임 사용 시 Modifier를 유지한 상태에서 Trigger를 누르면 된다.
- 첫 키를 떼고 두 번째 키만 누르는 순차 커맨드로 해석하지 않는다.

### 11.5 허용 컨트롤

버튼 액션 캡처 시 허용:

- Keyboard Key
- Mouse Left/Right/Middle/기타 Button
- Mouse Scroll Up/Down을 가상 버튼으로 정규화
- Gamepad Face/Shoulder/Trigger/StickPress/D-pad/Start/Select

제외:

- Mouse Position / Delta
- Stick 전체 축
- Gyro, accelerometer
- 스틱 드리프트로 발생한 미세 입력
- Pointer move
- 현재 선택한 장치 탭과 다른 장치군

축 액션(Move, Look)은 V1에서 프리셋 단위만 선택하며 임의 버튼 캡처 대상에서 제외한다.

### 11.6 예약 키

- Escape / Gamepad East는 0.75초 이상 유지하면 캡처 취소로 사용한다.
- Backspace / Delete는 0.75초 이상 유지하면 현재 슬롯 바인딩 제거로 사용한다.
- 이 키 자체를 `UI/Cancel`에 매핑하는 기본 구성은 유지할 수 있다.
- 네 키 모두 짧게 눌렀다 놓으면 단일키 또는 조합의 구성 키로 캡처할 수 있다.
- 캡처 UI는 “짧게 놓으면 할당, 계속 누르면 취소/제거”를 실시간으로 표시한다.

---

## 12. 바인딩 충돌 정책

### 12.1 충돌 판정 키

다음 값이 모두 같거나 런타임 중재상 겹치면 충돌이다.

```text
Device Group
Input Context
Binding Shape
Normalized Control Paths
```

### 12.2 충돌 유형

| 유형 | 예 | 기본 처리 |
|------|----|-----------|
| Exact | Jump=A, Interact=A | 오류 |
| Chord Exact | Dodge=LB+B, Skill=LB+B | 오류 |
| Chord Subset | Guard=LB, Dodge=LB+B | 경고 + 중재 필요 |
| Context Separated | Gameplay Dash=B, UI Cancel=B | 허용 |
| Device Separated | Keyboard Space, Gamepad South | 허용 |
| Reserved | Rebinding Cancel 제거 | 금지 |

### 12.3 충돌 팝업

```text
이미 사용 중인 입력입니다.

[East]는 현재 “대시”에 할당되어 있습니다.

[교환] [기존 바인딩 해제 후 적용] [취소]
```

- **교환**: 두 슬롯의 장치군과 binding shape가 호환될 때만 제공
- **기존 해제 후 적용**: 기존 액션이 필수 바인딩이면 비활성
- **취소**: 캡처 전 스냅샷으로 복원
- 동일 컨텍스트에 같은 키를 그대로 유지하는 “둘 다 사용”은 기본 제공하지 않는다.
- Chord Subset은 둘 다 유지할 수 있지만 grace 지연이 발생한다는 경고를 표시한다.

### 12.4 필수 바인딩

다음 조건을 깨는 변경은 거부한다.

- UI Submit: 활성 장치군에 최소 1개
- UI Cancel: 활성 장치군에 최소 1개
- Menu/Settings 탈출 경로: 최소 1개
- Move/Look: 게임패드와 키보드·마우스 기본 축 슬롯 유지

---

## 13. 바인딩 프로필 저장

### 13.1 저장 데이터

`SettingsData`의 일반 그래픽·오디오 JSON과 분리해 저장한다.

```csharp
[Serializable]
public sealed class InputBindingProfileData
{
    public int profileVersion;
    public string actionAssetId;
    public string bindingOverridesJson;
    public List<BindingShapeOverrideData> bindingShapes;
}
```

`bindingOverridesJson`은 `InputActionAsset.SaveBindingOverridesAsJson()` 결과를 사용한다. 단일/조합 중 어느 형태가 활성인지와 슬롯별 정책은 `bindingShapes`에 별도로 저장한다.

저장 키 제안:

```text
InputBindings_v1
```

### 13.2 로드 순서

```text
InputManager.Init
→ InputActionAsset 로드
→ Action/Binding ID 검증
→ 프로필 JSON 로드
→ profileVersion 마이그레이션
→ LoadBindingOverridesFromJson
→ Binding Shape 활성화
→ 충돌·필수 바인딩 검증
→ Action Map Enable
→ OnBindingsChanged 발화
```

현재처럼 모든 Action Map을 먼저 Enable한 뒤 override를 적용하지 않는다.

### 13.3 설정 메뉴 Apply / Cancel

- 설정 화면 진입 시 원본 프로필 스냅샷 생성
- 개별 리바인딩 결과는 preview override로 즉시 UI에 반영
- Apply: 검증 후 디스크 저장, 새 스냅샷 확정
- Cancel: 화면 진입 스냅샷으로 모든 override 복원
- Reset Action: 해당 액션·장치군·슬롯만 기본값
- Reset Device: 현재 장치 탭 전체 기본값
- Reset All: 전체 기본값, 확인 팝업 필수

### 13.4 버전 마이그레이션

액션 이름보다 GUID를 우선 식별자로 사용한다.

- GUID가 유지되면 이름 변경에도 override 유지
- GUID가 사라지면 `(map, action, deviceGroup, slot)` 보조 키로 이전 시도
- 후보가 없거나 둘 이상이면 해당 슬롯만 기본값 복구
- 프로필 전체를 무조건 폐기하지 않는다.

구현 시 binding GUID가 아니라 **액션 GUID**(`InputAction.id`)를 사용한다.
사용자 슬롯 바인딩은 `EnsureUserBindingSlot`이 런타임에 생성하므로 binding GUID가 세션 간
안정적이지 않은 반면, 액션 GUID는 `.inputactions` 에셋에 저장돼 이름 변경에도 유지된다.
슬롯 식별의 나머지 축은 `deviceGroup`/`slot`이 그대로 담당한다.

`profileVersion`은 1(이름만 저장) → 2(`actionId` 추가)로 올린다.
PlayerPrefs 키 `InputBindings_v1`은 저장 슬롯 이름이며 profileVersion과 별개로 유지한다.

---

## 14. 입력 글리프 연동

### 14.1 이벤트

`IInputService`에 다음 이벤트를 추가한다.

```csharp
event Action<ActiveInputDevice> OnActiveDeviceChanged;
event Action OnBindingsChanged;
```

`UIInputPromptIcon`과 HUD 스킬·퀵슬롯 UI는 두 이벤트를 모두 구독한다.

### 14.2 표시 규칙

- 단일키: 글리프 1개
- 조합키: Modifier + `+` + Trigger
- Primary가 비어 있으면 Secondary 표시
- 둘 다 비어 있으면 액션 이름과 오류 상태 표시
- 브랜드 전용 글리프가 없으면 Generic, 그마저 없으면 Input System 표시 문자열
- 설정 목록에서는 현재 편집 중인 장치군의 글리프를 강제하고, HUD는 `ActiveDevice`를 따른다.

### 14.3 데이터 생성기

`InputGlyphDataGenerator`는 다음 경로까지 수집하도록 검증한다.

- 모든 Single binding effective path
- 모든 composite part path
- Primary / Secondary 슬롯
- Xbox / PlayStation / Switch override 누락

글리프 누락은 리바인딩을 막지 않지만 설정 화면에 경고 아이콘을 표시한다.

---

## 15. UI 포커스 시스템

### 15.1 UIFocusScope

모든 조작 가능한 전체 화면 UI와 팝업은 `UIFocusScope`를 가진다.

필드:

```text
DefaultSelectable
RememberLastSelection
RestorePreviousOnHide
AutoFocusWhenGamepadActivated
```

동작:

- Show: 기존 선택을 스택에 저장하고 자신의 기본/마지막 선택 지정
- Hide: 자신의 선택을 제거하고 아래 scope의 이전 선택 복원
- 선택 대상이 비활성·파괴됐으면 가까운 첫 유효 Selectable 선택
- 마우스→게임패드 전환 시 현재 선택이 없으면 최상위 scope 자동 포커스
- 게임패드→마우스 전환 시 선택을 강제로 지우지는 않는다.

### 15.2 우선 적용 화면

P0:

- `UI_Scene_TitleMenu`
- `UI_Scene_MenuPanel`
- `UI_Scene_PauseMenu`
- `UI_Scene_SettingMenu`
- `UI_Scene_SaveSlotMenu`
- `UI_Popup_Common`
- `UI_Popup_Respawn`

P1:

- Inventory
- Map
- Party
- Craft
- Quest
- MonsterCodex
- CharacterSelect
- RestGrowth

### 15.3 복잡한 화면 내비게이션

다음 화면은 Automatic Navigation만 사용하지 않는다.

- Inventory: 탭 ↔ 아이템 그리드 ↔ 상세 액션 ↔ 퀵슬롯
- Setting: 상단 탭 ↔ 현재 페이지 컨트롤 ↔ Apply/Cancel/Reset
- KeyBinding: 장치/카테고리 ↔ 바인딩 행 ↔ 캡처·충돌 팝업
- Party: 캐릭터 목록 ↔ 장비/어시스트 영역
- Map: 필터 ↔ 맵 컨트롤 ↔ 범례/닫기

동적 리스트가 바뀔 때마다 활성 항목 기준으로 explicit neighbor를 다시 계산한다.

---

## 16. 장치 변경과 연결 해제

### 16.1 활성 장치

현재 `InputSystem.onEvent` 기반 감지를 유지하되 다음을 추가한다.

- `InputSystem.onDeviceChange` 구독
- 활성 게임패드 Disconnected/Removed 시 KeyboardMouse 또는 연결된 다른 패드로 전환
- 같은 종류 패드 교체 시 브랜드 재검출
- 연결만으로 HUD 글리프를 바꾸지 않고 실제 actuation을 기본 전환 기준으로 유지
- 설정 게임패드 탭에서는 연결된 장치 목록을 별도로 표시

### 16.2 캡처 중 연결 해제

- 캡처 대상 게임패드가 해제되면 세션 취소
- 기존 프로필 복원
- “게임패드 연결이 해제되었습니다” 표시
- 키보드 Escape 취소는 계속 가능

### 16.3 아날로그 노이즈

- 스틱 드리프트와 트리거 미세값은 캡처하지 않는다.
- `WaitForNeutral`에서 모든 대상 컨트롤이 release threshold 아래로 돌아올 때까지 기다린다.
- 데드존 설정 변경은 리바인딩과 별도 설정으로 관리한다.

---

## 17. 카메라·접근성 설정

설정 메뉴 게임플레이 페이지에 다음을 추가하거나 기존 값을 분리한다.

| 설정 | 설명 |
|------|------|
| Mouse Sensitivity X/Y | 기존 마우스 delta 배율 |
| Gamepad Sensitivity X/Y | 게임패드 각속도 배율 |
| Gamepad Look Deadzone | 우스틱 카메라 데드존 |
| Invert Mouse Y | 마우스 반전 |
| Invert Gamepad Y | 게임패드 반전 |
| Vibration Enabled | 진동 전체 사용 |
| Vibration Strength | 0~1 |
| UI Navigation Repeat Delay | 고급 옵션, 기본값 유지 가능 |

기존 `SettingsData.sensitivityX/Y`, `invertY`를 바로 삭제하지 않는다. 새 필드 도입 시 구 저장 데이터 마이그레이션을 제공한다.

---

## 18. 구현 단계

### Phase 0 — 액션 에셋 정리

- [x] 현재 물리 키 중복·빈 바인딩 자동 검사 작성
- [x] RT, D-pad, Keyboard R 충돌 제거
- [x] Gamepad/R2 오바인딩 수정
- [x] Keyboard&Mouse / Gamepad Control Scheme 완성
- [x] 프로젝트 `UI` 표준 액션 완성
- [x] `UIRoot`의 `InputSystemUIInputModule`을 동일 액션 에셋에 연결
- [x] `AssignDefaultActions()` fallback 제거 또는 프로젝트 액션 명시 할당으로 변경

### Phase 1 — 전 UI 게임패드 내비게이션

- [x] 공통 `UIFocusScope` 구현
- [x] 공통 fallback 기반 P0 화면 기본 포커스 연결
- [x] UI Cancel → 전역 Back 경로 일원화
- [x] 팝업 중첩 포커스 push/pop/restore
- [x] 장치 전환 시 null 포커스 자동 복구
- [x] 주요 복잡 화면 explicit navigation 구축

### Phase 2 — 입력 컨텍스트와 조합 중재

- [ ] 토큰 기반 Input Context Stack
- [x] `InputManager.Chord.cs` 구현
- [x] 긴 조합 우선, 단일키 grace, provisional hold 처리
- [x] InputBuffer 확정 시점 연동
- [x] 레이어 변경·입력 억제 시 pending 상태 일괄 취소

### Phase 3 — 리바인딩 런타임

- [ ] `InputBindingCatalogSO`
- [x] 고정 Single/Composite 슬롯과 binding GUID 규약
- [x] Rebinding Session 상태 머신
- [x] 단일키·조합키 캡처
- [x] 충돌 검사와 대체·취소
- [x] 필수 UI 바인딩 보호

### Phase 4 — 설정 메뉴 키 설정 하위 패널

- [x] `UISettingPageKeyBinding` 구현
- [x] 장치·카테고리 탭
- [x] `UIKeyBindingRow` 풀링
- [x] 캡처 오버레이
- [x] 충돌 오버레이
- [x] ScrollRect 자동 추적과 explicit navigation
- [x] Apply / Cancel / Reset Action / Reset Device / Reset All

### Phase 5 — 저장·글리프·장치 복구

- [x] override JSON 저장·로드
- [x] profileVersion 마이그레이션
- [x] `OnBindingsChanged`
- [x] 공용 입력 프롬프트 즉시 갱신
- [x] 연결 해제·재연결 처리
- [x] 브랜드별 글리프 검증

### Phase 6 — 검증과 문서 동기화

- [x] EditMode 입력 로직 테스트
- [ ] PlayMode UI 수직 슬라이스 테스트
- [ ] Xbox / DualSense / Switch Pro 수동 스모크
- [x] `INPUT_SYSTEM_GUIDE.md`를 실제 partial 파일·API 기준으로 갱신
- [x] `GAMEPLAY_GUIDE.md` 기본 조작표 갱신

---

## 19. 자동 검증

### 19.1 EditMode

| 테스트 | 검증 |
|--------|------|
| `InputBindingAssetValidationTests` | 액션 이름, binding GUID, 필수 슬롯, Control Scheme |
| `InputBindingCollisionTests` | 같은 컨텍스트의 exact 충돌 0 |
| `InputChordArbiterTests` | 단일, 조합, timeout, release, 컨텍스트 취소 |
| `InputBindingProfileTests` | Save→Load round trip |
| `InputBindingMigrationTests` | GUID 유지·삭제·이름 변경 |
| `InputBindingRequiredActionTests` | Submit/Cancel 탈출 경로 보존 |
| `InputGlyphCoverageTests` | 기본 바인딩과 composite part 글리프 누락 |

필수 조합 테스트:

```text
East 단독                    → Dash 1회
LB 유지 + East               → Dodge 1회, Dash 0회, Guard 외부 시작 0회
LB 단독 유지                 → grace 후 Guard started 1회
LB + D-pad Up                → QuickSlotUp 1회, CharacterSwap1 0회
D-pad Up 단독                → CharacterSwap1 1회
컨텍스트가 UI로 변경         → pending Gameplay 입력 0회
```

### 19.2 PlayMode

| 시나리오 | 완료 조건 |
|----------|-----------|
| 타이틀→새 게임 | 게임패드만으로 진입 |
| Pause→Settings→KeyBinding | 포커스 유실 없이 진입·복귀 |
| 단일키 변경 | 즉시 글리프 변경, Apply 후 재시작에도 유지 |
| 조합키 변경 | Modifier+Trigger 표시와 런타임 단독 발화 |
| 충돌 교환 | 두 액션 바인딩이 정확히 교환 |
| Cancel | 설정 진입 전 프로필로 복원 |
| 중첩 팝업 | 닫은 뒤 원래 버튼 포커스 복원 |
| 패드 해제 | 캡처 취소 및 키보드 조작 복구 |

---

## 20. 수동 스모크 매트릭스

| 영역 | Xbox | DualSense | Switch Pro |
|------|------|-----------|------------|
| 브랜드 글리프 | A/B/X/Y | Cross/Circle/Square/Triangle | B/A/Y/X |
| UI Navigate | 필수 | 필수 | 필수 |
| Submit/Cancel 위치 | 물리 위치 정책 확인 | 확인 | Nintendo 역배열 정책 확인 |
| Trigger 조합 | 필수 | 필수 | 필수 |
| 연결 해제/재연결 | 필수 | 필수 | 필수 |
| 진동 중지 | Pause/Disconnect 시 | 동일 | 지원 범위 확인 |

Switch는 `buttonSouth`의 표시 문자가 Xbox와 다르므로 “논리 Submit”과 “인쇄 글리프”를 혼동하지 않는다. 브랜드별 Submit/Cancel 위치 정책을 옵션화할지 출시 플랫폼 정책으로 고정할지 Phase 0에서 확정한다.

---

## 21. 완료 조건

### 기능

- [ ] 타이틀부터 중앙 보스 정산까지 마우스 없이 플레이 가능
- [ ] 모든 주요 UI를 패드로 열고 닫고 조작 가능
- [ ] 모든 팝업 닫기 후 이전 포커스 복원
- [ ] 동일 물리 입력으로 의도하지 않은 게임플레이 액션이 동시에 발화하지 않음
- [ ] 설정 메뉴 하위 패널에서 Primary/Secondary를 변경 가능
- [ ] 첫 키 후 두 번째 키 입력으로 2키 조합을 설정 가능
- [ ] 첫 키만 입력하면 단일키로 설정 가능
- [ ] 충돌 교환·대체·취소와 필수 키 보호 동작
- [ ] Apply/Cancel/Reset과 재시작 후 저장 복원 동작
- [ ] 모든 HUD 글리프가 장치·브랜드·리바인딩을 즉시 반영

### 구조

- [x] EventSystem과 프로젝트 입력 라우터가 동일 UI Action Asset 사용
- [x] UI 모듈에 신규 Manager 싱글톤 직접 의존 0
- [x] InputManager에 신규 구체 UI 구현 의존 0
- [x] Data 모듈이 Manager/UI 구현 참조 0
- [x] 사용자 프로필이 binding GUID 기반으로 저장됨
- [x] 조합 중재가 InputBuffer 이전에 적용됨

### 검증

- [ ] Unity 컴파일 오류 0
- [ ] EditMode 입력 테스트 전체 통과
- [ ] PlayMode UI 수직 슬라이스 통과
- [ ] Xbox / DualSense / Switch Pro 스모크 완료
- [ ] Player Build 오류 0
- [ ] `INPUT_SYSTEM_GUIDE.md`, `GAMEPLAY_GUIDE.md` 동기화

---

## 22. 리스크와 대응

| 리스크 | 대응 |
|--------|------|
| 조합 유예로 단일 공격 반응이 느려짐 | 조합 공유 금지 정책 제공, 액션별 grace 허용 여부 데이터화 |
| Action Asset 편집으로 binding index 변경 | index가 아닌 binding GUID 저장 |
| EventSystem Cancel과 전역 Back 이중 실행 | UI Cancel 단일 소비 경로 |
| 캡처 시작 Submit이 새 키로 잡힘 | `WaitForNeutral` 필수 |
| 스틱 드리프트가 캡처됨 | 버튼 필터 + actuation/release threshold |
| 동적 리스트에서 포커스가 파괴됨 | FocusScope fallback + 내비게이션 재빌드 |
| 모든 맵 Enable로 컨텍스트 충돌 | Context Stack에 따른 맵/콜백 활성 정책 |
| 프로필 손상으로 UI 조작 불가 | 시작 시 필수 바인딩 검증 후 기본값 부분 복구 |
| 기존 글리프 에셋 누락 | Generic→텍스트 폴백, 자동 커버리지 테스트 |
| 기존 `INPUT_SYSTEM_GUIDE`와 실제 코드 불일치 | Phase 6에서 구현 완료 상태로 전면 동기화 |

---

## 23. 비목표

- 온라인 계정별 클라우드 입력 프로필
- 플레이어 여러 명의 동시 로컬 입력
- Steam Input API 직접 통합
- 키보드 매크로와 3키 이상 조합
- 전투 커맨드 입력(예: 아래→오른쪽→공격)
- 플랫폼 인증용 접근성 전체 범위
- 게임패드 햅틱 패턴 시스템 본 구현

햅틱은 `IInputService` 또는 별도 피드백 계약으로 확장할 수 있지만 본 스펙에서는 진동 사용 여부·강도 설정과 연결 해제 시 정지만 다룬다.
