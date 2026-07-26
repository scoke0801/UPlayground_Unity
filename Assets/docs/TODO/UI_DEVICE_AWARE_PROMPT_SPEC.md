# UI 장치별 입력 프롬프트 설계

작성 2026-07-26. 선행 문서: `Assets/docs/TODO/GAMEPAD_UI_INPUT_REBINDING_SYSTEM_SPEC.md`
(이 문서는 그 스펙의 "남은 작업" 중 UI 표시 계층만 다룬다. 액션 에셋·리바인딩·컨텍스트
스택 규약은 선행 문서를 따르고 여기서 다시 정의하지 않는다.)

---

## 1. 문제 정의

전체 화면 메뉴들이 조작 중인 장치와 무관하게 같은 조작 안내를 노출한다. 두 방향 모두
잘못돼 있다.

- 키보드·마우스로 조작 중인데 `LT / RT`, `LB / RB` 게임패드 힌트가 보인다.
  게임패드 전용 액션이라 키보드에는 대응 키 자체가 없다 — **없는 기능을 광고**하고 있다.
- 게임패드로 조작 중인데 `ESC 닫기`, `Esc 취소` 키보드 힌트가 보인다.

그리고 트리거·숄더로 조작하는 메뉴(페이지 순환, 분류 전환)에 해당 UI 영역 근처의
프롬프트가 없다. 지금 있는 안내는 헤더 구석의 회색 텍스트 한 줄이라 조작 대상과
시각적으로 이어지지 않는다.

---

## 2. 조사 결과

### 2.1 이미 갖춰진 기반 (재사용 대상)

| 자산 | 위치 | 상태 |
| --- | --- | --- |
| 장치 감지 | `InputManager.Device.cs` — `ActiveDevice`, `GamepadBrand`, `OnActiveDeviceChanged` | 동작. 입력 이벤트 단위로 즉시 전환 |
| 글리프 해석 | `InputGlyphResolver.Resolve(map, action, device, brand, glyphData)` | 동작. 장치에 맞는 바인딩을 골라 스프라이트/텍스트 파트를 돌려줌 |
| 글리프 데이터 | `Assets/10.Datas/UI/Input/InputGlyphData.asset` | 86개 항목. `leftTrigger` / `rightTrigger` / `leftShoulder` / `rightShoulder` / `escape` 모두 등록돼 있음 |
| 프롬프트 위젯 | `UI_InputPromptIcon` | 동작. 단일·조합 바인딩 모두 렌더 |
| 장치 게이트 선례 | `UIFocusIndicator.cs:173` — `_gamepadOnly` 플래그 | 있음. 다만 이 컴포넌트 전용 |
| 리바인딩 반영 | `IInputService.OnBindingsChanged` | 동작 |

**필요한 재료는 전부 있다.** 빠진 것은 (a) "이 장치에서만 보인다"는 공용 게이트와
(b) 화면들이 그것을 쓰지 않는다는 점이다.

### 2.2 액션별 장치 커버리지

`Assets/Resources/Input/PlayerInputActions.inputactions` + 런타임 보강
(`InputManager.Action.cs:79-133` `EnsureStandardUiActions`)을 합친 실제 상태다.
에셋만 보면 오판하기 쉬우므로 반드시 런타임 보강까지 같이 봐야 한다.

| UI 액션 | 키보드·마우스 | 게임패드 | 비고 |
| --- | --- | --- | --- |
| `Navigate` | WASD + 방향키 | leftStick + dpad | 런타임 보강으로 게임패드 추가 |
| `Submit` | Enter, Space | buttonSouth | 런타임 보강으로 게임패드 추가 |
| `Cancel` | Escape | buttonEast | 런타임 보강으로 게임패드 추가 |
| `MenuPanel` | backquote | start | 양쪽 |
| **`MainTabPrevious`** | **없음** | leftTrigger | **게임패드 전용** |
| **`MainTabNext`** | **없음** | rightTrigger | **게임패드 전용** |
| **`SubTabPrevious`** | **없음** | leftShoulder | **게임패드 전용** |
| **`SubTabNext`** | **없음** | rightShoulder | **게임패드 전용** |

`MainTab*` / `SubTab*`은 `UI_Base`가 모든 전체 화면 메뉴에 자동 등록한다
(`UI_Base.cs:346-383`). 순환 대상 페이지는 `UI_Base.cs:45-54`의
`MainPageOrder` — 지도 → 인벤토리 → 제작 → 퀘스트 → 파티 → 도감 → 설정 7개다.

### 2.3 현재 안내 문구 (전부 하드코딩 · 상시 노출)

| 위치 | 문구 | 문제 |
| --- | --- | --- |
| `UIInventoryPrefabBuilder.cs:85-89` | `LT / RT  메뉴 전환     LB / RB  분류 전환` | 게임패드 전용 안내를 키보드에도 노출 |
| `UICraftMenuPrefabBuilder.cs:81` | 동일 | 동일 |
| `UIQuestMenuPrefabBuilder.cs:78` | `LT / RT  메뉴 전환     LB / RB  상태 전환` | 동일 |
| `UISettingMenuPrefabBuilder.cs:141` | `LT / RT  메뉴     LB / RB  설정 탭     방향키  이동     A  선택     B  취소` | 동일 |
| `UISettingMenuPrefabBuilder.cs:107` | `ESC  닫기` | 키보드 전용 안내를 게임패드에도 노출 |
| `UISettingMenuPrefabBuilder.cs:149` | `ESC  취소` | 동일 |
| `UISaveSlotMenuPrefabBuilder.cs:120` | `Esc` | 동일 |

전부 문자열 리터럴이라 **리바인딩해도 문구가 바뀌지 않는다.** 사용자가 설정에서
서브 탭을 다른 버튼으로 바꿔도 화면은 계속 `LB / RB`라고 안내한다.

### 2.4 안내가 아예 없는 화면

`UI_CharacterSelect`, `UI_Map`, `UI_MonsterCodex`, `UI_PartyMenu`, `UI_PartySelect`,
`UI_PauseMenu`, 성장(`UIRestGrowthPrefabBuilder`) — 대응 빌더에 힌트 텍스트가 없다.
이 중 지도·파티·도감은 `MainPageOrder`에 포함돼 있어 **게임패드로 LT/RT 페이지 순환이
실제로 되는데 알 방법이 없다.**

### 2.5 장치 전환에 반응하는 화면

`UI_Inventory`가 유일하다(`UI_Inventory.cs:229-272`). 퀵슬롯 라벨만
`OnActiveDeviceChanged`를 구독해 글리프 텍스트를 교체한다. 화면별 일회성 구현이라
다른 화면으로 복사되지 않았다. **이 패턴을 공용화하는 것이 이번 작업의 핵심이다.**

---

## 3. 결함 목록

| ID | 내용 | 심각도 | 근거 |
| --- | --- | --- | --- |
| F1 | 게임패드 전용 액션 안내가 키보드에서도 노출. 대응 키가 없어 동작 불가한 기능을 안내한다 | 높음 | 2.2 + 2.3 |
| F2 | 키보드 전용 `ESC` 안내가 게임패드에서도 노출 | 높음 | 2.3 |
| F3 | 안내가 하드코딩 문자열이라 리바인딩이 반영되지 않는다 | 높음 | 2.3 |
| F4 | 지도·파티·도감·캐릭터 선택·성장·일시정지에 안내가 전무 | 중간 | 2.4 |
| F5 | 안내가 헤더 구석의 텍스트 한 줄이라 조작 대상 UI와 시각적으로 연결되지 않는다 | 중간 | 2.3 |
| F6 | `UI_InputPromptIcon`에 장치 게이트가 없다. 게임패드 전용 액션을 키보드 상태로 조회하면 `FindBindingIndexForDevice`가 -1을 돌려주고 `InputGlyphResult.Missing`이 **액션 이름 원문**을 폴백 텍스트로 넣는다 → 화면에 `MainTabNext`가 그대로 노출될 수 있다 | 높음 | `InputGlyphResolver.cs:117-129, 251` |
| F7 | 탭 그룹이 없는 화면(지도·파티·도감)에서도 `SubTab*` 액션이 등록된다. 눌러도 아무 일이 없다 | 낮음 | `UI_Base.cs:405-415` |
| F8 | `SubTabPrevious`와 `DialogueBacklog`가 둘 다 `<Gamepad>/leftShoulder`. 컨텍스트가 달라 실사용 충돌은 없어 보이나, 프롬프트를 액션 기준으로 그리면 같은 글리프가 두 의미로 보인다 | 낮음 | `.inputactions` UI 맵 |

F6은 표시 버그다. 이번 작업으로 게이트를 넣으면 자연히 해소되지만, 게이트 없이
프롬프트만 늘리면 오히려 노출 빈도가 올라간다. 순서를 지켜야 한다.

---

## 4. 수정 방안

### 4.1 설계 원칙

1. **문자열이 아니라 액션을 지정한다.** 모든 안내는 `(map, action)`을 참조하고 글리프는
   `InputGlyphResolver`가 해석한다. 리바인딩이 자동 반영된다.
2. **장치에 바인딩이 없으면 그 안내를 숨긴다.** "게임패드일 때만"을 직접 지정하는 대신,
   *해당 장치에 바인딩이 존재하는가*로 판정한다. 나중에 `MainTab*`에 키보드 바인딩을
   추가하면 코드 수정 없이 키보드에서도 뜬다.
3. **조작 대상 옆에 붙인다.** 탭 스트립 양끝에 숄더 글리프, 메뉴 제목 양끝에 트리거
   글리프처럼 대상과 붙여 배치한다. 화면 하단 공용 바는 전역 액션(취소·확인)만 담는다.
4. **기존 힌트 텍스트는 지운다.** 프롬프트로 대체되는 하드코딩 문자열은 남기지 않는다.

### 4.2 신규 공용 컴포넌트

#### (a) `InputPromptAvailability` — 판정 헬퍼

`Assets/02.Scripts/UI/InputPrompt/InputPromptAvailability.cs` (신규, static)

```
bool HasBindingFor(string map, string action, ActiveInputDevice device)
```

`InputGlyphResolver.FindBindingIndexForDevice`와 같은 판정을 공개 API로 노출한다.
현재 이 로직은 `private`이라 밖에서 쓸 수 없다. **`InputGlyphResult`에 `IsValid`가 이미
있으므로 `Resolve(...).IsValid`로 대체 가능하지만, 그러면 글리프 해석 비용을 판정에만
쓰게 되므로 경량 경로를 따로 둔다.**

#### (b) `UI_InputPromptIcon` 확장 — 장치 게이트

```csharp
[Header("표시 조건")]
[Tooltip("활성 장치에 이 액션의 바인딩이 없으면 자신을 숨긴다.")]
[SerializeField] private bool _hideWhenUnbound = true;
[SerializeField] private DevicePromptFilter _deviceFilter = DevicePromptFilter.Any;
```

`DevicePromptFilter` = `Any` / `GamepadOnly` / `KeyboardMouseOnly`.

`Refresh()`에서 `result.IsValid == false && _hideWhenUnbound`이면 루트를
`SetActive(false)`. F6도 함께 해소된다. **기본값을 `true`로 두면 기존 배치본의 동작이
바뀌므로, 이미 배치된 HUD 프롬프트에 영향이 없는지 확인해야 한다**(HUD 액션은 양쪽
장치에 모두 바인딩돼 있어 실질 영향은 없을 것으로 보이나 검증 필요).

#### (c) `UI_InputPromptBar` — 라벨 달린 프롬프트 묶음

`Assets/02.Scripts/UI/InputPrompt/UI_InputPromptBar.cs` (신규)

인스펙터에 `(map, action, 라벨)` 목록을 받아 `[글리프] 라벨` 항목을 가로로 배치한다.
활성 장치에 바인딩이 없는 항목은 자동으로 빠지고, 남은 항목이 0개면 바 자체를 숨긴다.
`OnActiveDeviceChanged` / `OnBindingsChanged` 구독은 이 컴포넌트가 담당한다.

기존 `UI_ComboRouteHint` / `UI_ComboRouteHintRow`가 유사한 목록 렌더링을 하고 있으므로
레이아웃 구성 방식을 참고한다.

### 4.3 배치 규칙

| 위치 | 담는 액션 | 조건 |
| --- | --- | --- |
| 메뉴 제목 좌우 | `MainTabPrevious` / `MainTabNext` | `ConfigureMainPageShortcut` 호출 화면 |
| 서브 탭 스트립 좌우 | `SubTabPrevious` / `SubTabNext` | `ConfigureTabShortcuts(subTabs:)` 호출 화면 |
| 화면 하단 공용 바 | `Submit`(확인·결정), `Cancel`(닫기·뒤로) | 전체 화면 메뉴 전부 |
| 화면 고유 영역 | 화면별 액션 | 아래 4.4 |

`UI_Base`가 이미 어떤 화면이 어떤 단축키를 쓰는지 알고 있다(`_mainTabGroup`,
`_subTabGroup`, `_mainPageKey`). **탭 프롬프트는 화면마다 손으로 붙이지 말고
`UI_Base`가 `ConfigureTabShortcuts` 인자로 받은 `UITabGroup`에 자동 부착하는 방안을
우선 검토한다.** 이렇게 하면 7개 화면 프리팹을 각각 고치지 않아도 되고, 새 메뉴가
추가돼도 자동으로 따라온다. 자동 부착이 레이아웃과 충돌하는 화면만 수동 배치로 뺀다.

### 4.4 화면별 작업 항목

| 화면 | 현재 | 작업 |
| --- | --- | --- |
| `UI_Inventory` | 하드코딩 힌트 + 퀵슬롯 라벨만 장치 반응 | 힌트 텍스트 제거. 탭 프롬프트(LB/RB) + 메인 페이지 프롬프트(LT/RT) + 하단 바. 퀵슬롯 라벨 갱신 로직은 `UI_InputPromptIcon`으로 대체 검토 |
| `UI_CraftMenu` | 하드코딩 힌트 | 힌트 제거 + 탭/페이지/하단 바 |
| `UI_QuestMenu` | 하드코딩 힌트 | 동일 |
| `UI_SettingMenu` | 하드코딩 힌트 2종 + `ESC` 2곳 | 힌트 제거. `ESC 닫기`/`ESC 취소` 버튼 라벨을 액션 프롬프트로 교체. 적용/취소/초기화 버튼에 `Submit`/`Cancel` 프롬프트 |
| `UI_Map` | 안내 없음 | 페이지 프롬프트 + 하단 바. 가상 커서 조작(스틱 이동·줌)이 게임패드 전용이므로 지도 뷰 근처에 전용 프롬프트 추가 (선행 스펙 6차 항목과 연계) |
| `UI_MonsterCodex` | 안내 없음 | 페이지 프롬프트 + 하단 바 |
| `UI_PartyMenu` | 안내 없음 | 페이지 프롬프트 + 하단 바 |
| `UI_PartySelect` | 안내 없음 | 하단 바(확인·취소). 페이지 순환 대상 아님 |
| `UI_CharacterSelect` | 안내 없음. `시작`/`취소` 버튼만 존재 | 하단 바(`Submit` 확인 / `Cancel` 취소). 페이지 순환 대상 아님 |
| `UI_SaveSlotMenu` | `Esc` 하드코딩 | 액션 프롬프트로 교체 |
| `UI_PauseMenu` | 안내 없음 | 하단 바 |
| 성장 화면 | 안내 없음 | 하단 바 |

### 4.5 F7 정리

`UI_Base.RegisterNavigationShortcutEvents`가 `_subTabGroup == null`인 화면에서도
`SubTab*`을 등록한다. 프롬프트를 붙이면 "없는 탭 전환"이 안내되지는 않지만(탭 그룹이
없으면 프롬프트도 안 붙음), 등록 자체는 불필요하다. `_subTabGroup`이 있을 때만 등록하도록
좁힌다. 등록 대상이 줄면 F8의 `DialogueBacklog` 중복도 실질 위험이 더 낮아진다.

---

## 5. 단계 계획

| 단계 | 내용 | 산출물 |
| --- | --- | --- |
| 1 | `UI_InputPromptIcon` 장치 게이트 + `InputPromptAvailability` | 코드 2개. F6 해소 |
| 2 | `UI_InputPromptBar` 구현 | 코드 1개 |
| 3 | `UI_Base` 탭 프롬프트 자동 부착 + F7 정리 | `UI_Base.cs` 수정 |
| 4 | 프리팹 빌더 수정 — 하드코딩 힌트 제거, 하단 바 삽입 | 빌더 7~10개 |
| 5 | 프리팹 재생성 후 Play Mode에서 장치 전환 확인 | 수동 검증 |

1~3단계는 코드만이라 컴파일 검증까지 가능하다. 4단계는 프리팹 재생성이 필요하고,
5단계는 Unity 에디터에서 실기 확인이 필요하다.

---

## 6. 결정이 필요한 사항

1. **`MainTab*` / `SubTab*`에 키보드 바인딩을 추가할 것인가.**
   지금은 키보드로 페이지·탭 순환이 불가능하다(마우스로 탭을 직접 클릭해야 함).
   `Q`/`E`, `PageUp`/`PageDown` 같은 키를 추가하면 F1이 "숨김"이 아니라 "양쪽 표시"로
   해결되고 키보드 조작성도 올라간다. 추가하지 않기로 하면 키보드에서는 해당 프롬프트가
   영구히 숨겨진다. **이 결정에 따라 4.1의 원칙 2가 실제로 무엇을 숨길지가 달라진다.**

2. **하단 공용 바를 `UI_Base`가 자동 생성할 것인가, 프리팹에 저작할 것인가.**
   자동 생성은 12개 화면을 한 번에 커버하지만 화면별 레이아웃(세이프 영역, 기존 푸터
   버튼과의 충돌)을 개별 조정하기 어렵다.

3. **마우스 조작 시 프롬프트 정책.** 현재 `ActiveInputDevice`는 키보드와 마우스를
   `KeyboardMouse` 하나로 묶는다. 마우스만 쓰는 사용자에게 키보드 글리프를 보일지,
   클릭 가능한 버튼에는 프롬프트를 생략할지 정해야 한다.

---

## 7. 비목표

- 액션 에셋의 컨트롤 스킴·컨텍스트 스택 재설계 (선행 스펙 소관)
- 게임패드 브랜드별 글리프 추가 저작 (이미 동작)
- HUD 전투 프롬프트 변경 (이번 범위는 전체 화면 메뉴)
- 키 설정 화면의 키캡 표시 (`UIKeyCapStrip`, 별도 경로로 이미 동작)
