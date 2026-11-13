# 입력 키 프롬프트 UI 시스템 설계 문서

> 작성일: 2026-06-05
> 대상 버전: Unity 6 (6000.0.60f1), URP, Input System (com.unity.inputsystem)
> 레퍼런스: 명조/원신/세키로 키 프롬프트, [InputSystemActionPrompts](https://github.com/simonoliver/InputSystemActionPrompts), [InputGlyphs](https://eviltwo.github.io/InputGlyphs_Docs/), Kenney Input Prompts
> 상태: Phase 1 + Phase 3(브랜드) + 복합 바인딩 다중 글리프 구현 완료 (2026-06-05)

---

## 구현 현황 (2026-06-05)

Phase 1을 구현하고 `dotnet build Assembly-CSharp.csproj` 컴파일을 검증했다(오류 0).

| 항목 | 상태 | 구현 |
|------|------|------|
| 활성 디바이스 enum | 완료 | `ActiveInputDevice`(`InputDefine.cs`) |
| 디바이스 감지 + 디바운스 + 이벤트 | 완료 | `InputManager.Device.cs` (`InputSystem.onEvent` + `EnumerateChangedControls(0.5f)`, `OnActiveDeviceChanged`) |
| 생명주기 연동 | 완료 | `InputManager.Init()` → `InitDeviceDetection()`, `Dispose()` → `DisposeDeviceDetection()` |
| 글리프 매핑 데이터 | 완료 | `InputGlyphDataSO` (controlPath→Sprite, 키보드/마우스·게임패드 2세트) |
| 액션→글리프 해석기 | 완료 | `InputGlyphResolver` (바인딩 순회 + 디바이스 레이아웃 매칭, 표준 `GetBindingDisplayString`) |
| 표시 위젯 | 완료 | `UIInputPromptIcon` (Image + 폴백 라벨, `OnActiveDeviceChanged` 구독) |
| 상호작용 키 통합 | 완료 | `UI_HUD_InteractionKey`가 `UIInputPromptIcon` 호스팅 |
| controlPath 자동 생성 툴 | 완료 | `InputGlyphDataGenerator` (에셋 파싱 → SO 생성·동기화 메뉴 + 인스펙터 버튼), `InputGlyphDataSO.EditorSyncControlPaths` |
| 아날로그/포인터 자동 제외 | 완료 | `InputGlyphDataGenerator.ExcludedSegments` (`delta`·`position`·`scroll`·`leftStick`·`rightStick`) |
| **복합 바인딩 다중 글리프** (Dodge=L1+R1) | 완료 | `InputGlyphResolver`가 컴포지트 파트를 `GlyphPart` 리스트로 반환, `UIInputPromptGlyphItem`+`UIInputPromptIcon` 콤보 렌더 |
| **Phase 3 — 게임패드 브랜드 분기** | 완료 | `GamepadBrand` enum, `InputManager.DetectBrand`(DualShock/XInput/Switch 타입), SO 브랜드별 오버라이드+제네릭 폴백, 생성툴 브랜드 채우기 버튼 |
| **스프라이트 자동 연결** (Kenney InputIcon) | 완료 | `InputGlyphDataGenerator.AutoLinkSprites` — controlPath→Kenney 파일명 매핑, 텍스처 Sprite 변환, 카테고리별 할당. 전수 검증: KM 24 + Xbox 16 + PS 16 = 미매칭 0 |
| **에디터 인스펙터 미리보기** | 완료 | `UIInputPromptIconEditor` — 디바이스/브랜드 선택 팝업으로 글리프를 인스펙터 GUI에만 그림(컴포넌트 무수정 → 프리팹 저장 0). 매니저 무관 `InputGlyphResolver.ResolveAction(InputAction,...)` + 에셋 직접 로드(`AssetDatabase`). 설정은 `EditorPrefs`에 기억 |

**확정된 결정 (§11 해소):**
- **글리프 스타일: 키캡형** (테두리+글자). 명조/원신 관례. 사용자 확정.
- **글리프 해석 방식: 자산 무수정.** 설계 검토 단계에선 A안(컨트롤 스킴 그룹 태깅)을 권장했으나, ① Unity 외부에서 `.inputactions` 편집을 검증할 수 없고 ② composite 바인딩(2DVector/OneModifier) 그룹 태깅이 손편집 시 깨지기 쉬워, **`GetBindingDisplayString(bindingIndex, out layout, out path)` 오버로드로 바인딩별 디바이스 레이아웃을 표준 API로 직접 매칭**하는 방식으로 구현했다. A안의 "표준 API + 리바인딩 자동 반영" 이점은 그대로 얻으면서 자산 편집 리스크를 0으로 만든다. 스킴 그룹 태깅(Phase 0)은 **선택 사항으로 연기**(추후 PlayerInput 도입 시에만 필요).
- **`actionName` 입력: `InputDefine` 상수 사용.** 위젯 기본값을 `InputMapNames.PlayerAction` / `PlayerAction.Interact` 상수로 둠. 전용 드롭다운 `PropertyDrawer`는 후순위(선택).
- **에셋 로드 경로: 위젯 직접 참조(SerializeField).** 간접 로드(Resources/Addressables)를 쓰지 않으므로 빌드 스트리핑 문제 없음.

**남은 사용자 작업(Unity 에디터에서만 가능):**
1. Unity로 프로젝트를 열어 새 스크립트가 임포트되게 한다(`.meta` 자동 생성).
2. 메뉴 **`UPlayGround/입력/글리프 데이터 생성·동기화`** 실행 → `InputGlyphDataSO` 에셋이 `Assets/10.Datas/Input/`에 자동 생성되고, PlayerInputActions 에셋의 버튼/이산 입력 controlPath(키보드/마우스 24개, 게임패드 16개)가 스프라이트 빈 슬롯으로 자동 채워진다(부록 B). 순수 아날로그/포인터(`delta`·`position`·`scroll`·`leftStick`·`rightStick`)는 자동 제외된다. 바인딩을 바꾼 뒤 다시 실행하면 기존 스프라이트는 보존한 채 재동기화된다.
3. **스프라이트 자동 연결:** SO 인스펙터의 "전체 자동 연결" 버튼(또는 카테고리별 버튼)을 누르면 `Assets/ExternalAssets/UI/InputIcon`의 Kenney 스프라이트가 controlPath에 맞춰 자동 할당된다. 매칭된 텍스처는 자동으로 Sprite 타입으로 변환된다. (수작업 드래그 불필요. 부록 C 매핑 참고)
4. HUD/상호작용 프리팹에 `UIInputPromptIcon`을 붙이고 `Image`·`InputGlyphDataSO`·(선택)폴백 라벨을 연결. 콤보 표시가 필요하면 `_comboContainer`+`_comboItemTemplate`도 연결.
5. `UI_HUD_InteractionKey` 프리팹의 `_promptIcon` 필드에 위 위젯을 연결.
6. 플레이 모드에서 키보드/마우스↔게임패드를 번갈아 입력해 글리프 전환과 키별 정확도를 런타임 검증.

> 참고: `Assembly-CSharp.csproj`에 새 파일 4개의 `<Compile>` 항목을 컴파일 검증용으로 추가해 두었다. 이 파일은 Unity가 재생성하므로 다음 에디터 진입 시 자동 정리된다.

---

## 요구사항

1. **디바이스 자동 감지** — 키보드+마우스 환경 ↔ 게임패드 환경 전환을 런타임에 감지하여 표시 중인 모든 키 프롬프트 UI를 즉시 교체한다.
2. **액션 기반 글리프 표시** — 지정된 액션에 바인딩된 실제 키를 아이콘으로 표시한다. (예: `CharacterSwap_1` → 키보드 환경에서 `1`, 게임패드 환경에서 D-Pad ↑ / 마우스 좌클릭 → 좌클릭 아이콘)

---

## 1. 현황 분석 (설계의 출발점)

코드/에셋을 조사한 결과, 표준 "컨트롤 스킴 기반" 튜토리얼을 그대로 적용할 수 없는 **프로젝트 고유 제약**이 4가지 있다. 설계는 이 제약 위에서 성립해야 한다.

| 제약 | 실제 상태 | 설계에 미치는 영향 |
|------|-----------|--------------------|
| **컨트롤 스킴 그룹이 비어 있음** | `PlayerInputActions.inputactions`에 컨트롤 스킴은 `Keyboard&Mouse` 하나만 정의돼 있고, 모든 바인딩의 `groups` 필드가 빈 문자열이다. | `GetBindingDisplayString(scheme)`로 스킴별 글리프를 바로 못 뽑는다. 스킴을 보강하거나(A안), 런타임 디바이스 매칭(B안)이 필요. |
| **게임패드 바인딩이 이미 같은 액션에 공존** | `PlayerAction` 맵의 액션 대부분이 키/마우스 + 게임패드 바인딩을 **함께** 갖는다. (예: `Attack` = `<Mouse>/leftButton` + `<Gamepad>/buttonWest`) | 액션 1개에서 두 디바이스 글리프를 모두 해석 가능. 별도 게임패드 액션맵을 글리프 소스로 쓸 필요 없음. |
| **PlayerInput 컴포넌트 미사용** | `InputManager`가 액션맵을 직접 `Enable()`하고 `action.started/performed/canceled`에 수동 구독한다. 컨트롤 스킴으로 입력을 라우팅하지 않는다. | 스킴 그룹을 추가해도 **런타임 입력 동작은 전혀 바뀌지 않는다**(순수 표시용 메타데이터). A안이 안전한 핵심 근거. `OnControlsChanged` 콜백도 못 쓰므로 감지를 직접 구현. |
| **레거시 `Gamepad` 액션맵 존재** | 별도 `Gamepad` 맵(L1/L2/R1/Up...)이 있으나 `AudioHapticsTest.cs`(테스트 코드)에서만 참조됨. | 글리프 해석 소스에서 **제외**한다. 포함하면 동일 물리 버튼이 중복 검출되어 해석이 모호해진다. |

### 기존 자산 현황 (그린필드)

- **키 아이콘 에셋: 없음.** 키보드/마우스/게임패드 글리프 스프라이트가 프로젝트에 전무. → 에셋 소싱이 구현 범위에 포함된다.
- **바인딩 표시 헬퍼 코드: 없음.** `GetBindingDisplayString`, 스프라이트 매핑 등 관련 코드 전무.
- **`UI_HUD_InteractionKey.cs`: 빈 껍데기.** `OnShow/OnHide`만 있고 실제 키 표시 로직 없음. → 이 시스템으로 대체/흡수 대상.
- **`InputManager._isGamepadActive`: 이미 존재.** 현재는 커서 잠금(`RefreshCursorState`)에만 쓰임. → 디바이스 감지 상태의 자연스러운 승격 지점.
- **액션 식별 규약:** 모든 액션은 `(맵 이름, 액션 이름)` 문자열 쌍으로 식별. 상수는 `InputDefine.cs`의 `InputMapNames` / `PlayerAction` / `UIAction` 등에 정의.

---

## 2. 외부 조사 요약 — 빌드 vs 도입

웹 조사에서 검증된 패턴과 기성 솔루션을 확인했다.

**검증된 핵심 패턴**
- 디바이스 "연결/해제"는 `InputSystem.onDeviceChange`로 감지하지만, **"지금 어떤 디바이스를 쓰는가"는 감지하지 못한다.** 후자는 `InputSystem.onEvent`(또는 `onActionChange`)에서 마지막 입력 이벤트의 디바이스를 추적하는 방식이 정석.
- 글리프는 `InputControlPath`(예: `<Keyboard>/1`, `<Gamepad>/dpad/up`)를 키로 스프라이트를 매핑하는 사전(Dictionary/ScriptableObject) 패턴.
- 텍스트 안 인라인 표시는 TMP `<sprite>` 태그 + Sprite Asset.

**기성 솔루션을 도입하지 않고 얇은 자체 레이어를 만드는 이유**
- `InputSystemActionPrompts`, `InputGlyphs`는 모두 **PlayerInput / 컨트롤 스킴 그룹**이 정상 구성됐다고 가정한다. 본 프로젝트는 스킴 그룹이 비어 있고 PlayerInput을 안 쓰므로 그대로는 동작하지 않는다.
- 입력 진입점이 이미 `InputManager`로 단일화돼 있어, 감지 로직을 여기에 붙이는 편이 외부 패키지의 별도 런타임을 끼워 넣는 것보다 결합도가 낮다.
- 단, **에셋(스프라이트)과 컨트롤패스→스프라이트 매핑 발상은 위 패키지/Kenney 팩을 그대로 차용**한다. 바퀴를 다시 발명하는 것은 아이콘 매핑 로직뿐이고, 그건 수십 줄 규모다.

> 결론: **감지 + 해석 + 위젯**의 얇은 자체 레이어를 만들고, 아이콘 에셋은 Kenney Input Prompts(무료, CC0)를 채택한다.

출처:
- [InputSystemActionPrompts (GitHub)](https://github.com/simonoliver/InputSystemActionPrompts)
- [InputGlyphs 문서](https://eviltwo.github.io/InputGlyphs_Docs/)
- [Detect most recent input device — Unity Discussions](https://discussions.unity.com/t/detect-most-recent-input-device-type/760071)
- [Detect when device changes and get control scheme — Unity Discussions](https://discussions.unity.com/t/detect-when-device-changes-and-get-corresponding-control-scheme/905624)
- [Detecting the Player's Controller Type — aidantakami](https://aidantakami.com/2021/02/02/detecting-the-players-controller-type-with-the-unity-input-system/)

---

## 3. 설계 목표 / 비목표

**목표**
- 표시 중인 모든 프롬프트가 디바이스 전환 시 한 프레임 내에 일괄 교체.
- 액션 1개(맵+액션명)만 지정하면 위젯이 알아서 현재 디바이스의 올바른 글리프를 찾는다.
- HUD 슬롯(독립 `Image`)과 본문 텍스트(인라인 `<sprite>`) 양쪽 렌더 경로 지원.
- 디자이너가 코드 없이 글리프 매핑·신규 디바이스를 추가할 수 있게 ScriptableObject 데이터 주도.

**비목표 (이번 범위 밖)**
- 키 리바인딩 UI (별도 시스템). 단, 본 시스템은 리바인딩 후에도 `GetBindingDisplayString`이 갱신값을 반환하도록 설계만 열어 둔다.
- 게임패드 브랜드 자동 판별(Xbox/PS/Switch 아이콘 자동 전환)은 Phase 3 선택 항목. Phase 1은 단일 제네릭 세트.
- 터치/모바일 입력.

---

## 4. 아키텍처 개요

두 축으로 분리한다. 각 축은 독립적으로 테스트·교체 가능하다.

```
┌─────────────────────────────────────────────────────────────┐
│ 축 A: 활성 디바이스 감지 (InputManager 확장)                   │
│   InputSystem.onEvent → 액추에이션 임계값 디바운스             │
│   → ActiveInputDevice 상태 전이                               │
│   → event OnActiveDeviceChanged(ActiveInputDevice)           │
└─────────────────────────────────────────────────────────────┘
                          │ (단일 소스, 요구사항 1·2 공통 소비)
                          ▼
┌─────────────────────────────────────────────────────────────┐
│ 축 B: 액션 → 글리프 해석                                       │
│   (맵,액션) + ActiveInputDevice                              │
│   → 활성 디바이스에 맞는 바인딩 선택                           │
│   → controlPath                                              │
│   → InputGlyphDataSO 조회 → Sprite                           │
└─────────────────────────────────────────────────────────────┘
                          │
          ┌───────────────┴───────────────┐
          ▼                               ▼
   UIInputPromptIcon              TMP 인라인 <sprite>
   (독립 Image, HUD 슬롯)          ("[E] 상호작용" 본문)
```

---

## 5. 축 A — 활성 디바이스 감지

### 5.1 디바이스 분류 enum

```csharp
namespace UPlayGround.InputDefine
{
    public enum ActiveInputDevice
    {
        KeyboardMouse,
        Gamepad,
    }
}
```

> 키보드와 마우스는 PC에서 항상 함께 쓰이므로 하나로 묶는다(명조/원신 관례). 마우스 좌클릭이든 키보드 `1`이든 같은 `KeyboardMouse` 상태이고, 글리프 해석 단계에서 디바이스 *클래스* 안에서 구체 바인딩을 고른다.

### 5.2 감지 로직 (InputManager 신규 partial 파일: `InputManager.Device.cs`)

기존 `bool _isGamepadActive`를 `ActiveInputDevice _activeDevice`로 승격하고, 감지를 추가한다.

```csharp
public partial class InputManager
{
    private ActiveInputDevice _activeDevice = ActiveInputDevice.KeyboardMouse;
    public ActiveInputDevice ActiveDevice => _activeDevice;

    // 위젯/시스템이 구독하는 단일 소스
    public event Action<ActiveInputDevice> OnActiveDeviceChanged;

    // 핫패스에서 미세 노이즈로 상태가 떨리는 것을 막는 임계값
    private const float kDeviceSwitchActuation = 0.5f;

    public void InitDeviceDetection()
    {
        InputSystem.onEvent += OnInputSystemEvent;
    }

    public void DisposeDeviceDetection()
    {
        InputSystem.onEvent -= OnInputSystemEvent;
    }

    private void OnInputSystemEvent(InputEventPtr eventPtr, InputDevice device)
    {
        // StateEvent/DeltaStateEvent만 검사
        if (!eventPtr.IsA<StateEvent>() && !eventPtr.IsA<DeltaStateEvent>())
            return;

        ActiveInputDevice candidate;
        switch (device)
        {
            case Gamepad:           candidate = ActiveInputDevice.Gamepad; break;
            case Keyboard:
            case Mouse:             candidate = ActiveInputDevice.KeyboardMouse; break;
            default:                return; // 그 외 디바이스 무시
        }

        if (candidate == _activeDevice)
            return;

        // 액추에이션 임계값: 스틱 드리프트·마우스 미세 이동·진동 노이즈로 인한 깜빡임 방지
        // (실제 눌림/충분한 이동이 있는 컨트롤이 하나라도 있을 때만 전환)
        if (!HasActuatedControl(eventPtr, device, kDeviceSwitchActuation))
            return;

        SetActiveDevice(candidate);
    }

    private void SetActiveDevice(ActiveInputDevice next)
    {
        _activeDevice = next;
        _isGamepadActive = next == ActiveInputDevice.Gamepad; // 기존 커서 로직 호환
        RefreshCursorState();
        OnActiveDeviceChanged?.Invoke(next);
    }
}
```

`HasActuatedControl`은 `eventPtr`의 변경된 컨트롤 중 `EvaluateMagnitude() >= 임계값`인 것이 있는지 검사한다(마우스 `delta`/`position`, 게임패드 `leftStick` 미세값 무시 목적). 구현 시 `eventPtr`를 순회하는 방식은 Input System 버전에 맞춰 확정한다.

### 5.3 디바운스가 핵심인 이유

`onEvent`/`onActionChange`는 `Move`/`Look`에서 매 프레임 발화한다. 임계값 없이 디바이스만 보고 전환하면, PC에서 패드를 꽂아만 둬도 스틱 드리프트로 프롬프트가 키보드↔패드로 떨린다. **임계값 게이트(≈0.5)** 가 이 시스템의 신뢰성을 좌우한다.

### 5.4 생명주기 연동

- `InputManager.Init()` 끝에서 `InitDeviceDetection()` 호출.
- `Dispose()`에서 `DisposeDeviceDetection()` + `OnActiveDeviceChanged = null`.
- 초기값은 `KeyboardMouse`. 첫 입력 이벤트가 패드면 즉시 1회 전환된다.

---

## 6. 축 B — 액션 → 글리프 해석

### 6.1 핵심 결정: 글리프 해석 방식 (A안 채택)

스킴 그룹이 비어 있다는 제약 때문에 두 가지 토대가 가능하다. **A안을 채택**한다.

| | **A안 — 컨트롤 스킴 그룹 보강 (채택)** | B안 — 런타임 경로 접두사 매칭 |
|---|---|---|
| 방법 | `.inputactions`에 `Gamepad` 스킴 추가, 각 바인딩의 `groups`에 `Keyboard&Mouse` / `Gamepad` 태그. 해석은 `InputAction.GetBindingDisplayString(group, out layout, out path)` 표준 API. | 에셋 무수정. 액션의 바인딩들을 순회하며 `path` 접두사(`<Keyboard>`/`<Mouse>` vs `<Gamepad>`)가 활성 디바이스와 맞는 것을 코드로 선택. |
| 장점 | 표준 API, 복합 바인딩(2DVector/모디파이어) 자동 처리, 리바인딩 표시 자동 반영, 추후 PlayerInput 도입 시 그대로 활용. | 에셋 변경 0. 현재 구조 그대로 동작. |
| 단점 | `.inputactions` 1회 편집(JSON 편집 가능하나 Unity에서 검증 필요). | 복합 바인딩·모디파이어·다중 바인딩 우선순위를 직접 처리해야 함. 표준 API 우회. |
| 안전성 | **런타임 입력 동작 불변** — `InputManager`가 스킴으로 라우팅하지 않으므로 그룹은 순수 표시 메타데이터. | 동일하게 안전하나 해석 코드 복잡도가 장기적으로 부채. |

**채택 근거:** 1회성 에셋 편집 비용을 한 번 치르면 이후 모든 해석이 표준 API로 단순해지고, 모디파이어/복합 바인딩 처리 부담이 사라진다. PlayerInput 미사용 구조 덕분에 그룹 추가가 입력 동작에 영향을 주지 않는 것이 결정적.

> 작업 시 주의: 각 바인딩에 스킴을 태그할 때, 마우스 바인딩(`<Mouse>/leftButton` 등)도 `Keyboard&Mouse` 그룹에 포함시켜야 한다. 복합 바인딩(2DVector 같은 컴포지트)은 부모 바인딩에 그룹을 지정한다. 편집 후 Unity 에디터에서 Input Actions 창으로 열어 스킴 드롭다운이 정상 표시되는지 검증한다.

### 6.2 글리프 매핑 데이터 (`InputGlyphDataSO`)

`Assets/10.Datas/` 하위에 ScriptableObject로 외부화(프로젝트 데이터 규약 준수).

```csharp
[CreateAssetMenu(menuName = "UPlayGround/Input/Glyph Data")]
public class InputGlyphDataSO : ScriptableObject
{
    [Serializable]
    public struct GlyphEntry
    {
        public string controlPath; // 예: "1", "leftButton", "dpad/up", "buttonWest"
        public Sprite sprite;
        public string fallbackText; // 스프라이트 없을 때 표시할 글자 (예: "1", "LMB")
    }

    public List<GlyphEntry> keyboardMouseGlyphs;
    public List<GlyphEntry> gamepadGlyphs;

    // 런타임 조회용 Dictionary는 OnEnable에서 빌드(controlPath → entry)
    public bool TryResolve(ActiveInputDevice device, string controlPath,
                           out Sprite sprite, out string fallbackText) { /* ... */ }
}
```

- 키는 `GetBindingDisplayString`이 돌려주는 `controlPath`의 마지막 세그먼트(레이아웃 prefix 제거 후) 또는 정규화 문자열을 사용한다. 정규화 규칙은 구현 시 1곳(`InputGlyphResolver`)에 고정한다.
- `fallbackText`는 스프라이트 미등록 키에 대한 안전망(개발 중 누락 키 가시화).

### 6.3 해석기 (`InputGlyphResolver`, static 유틸) — 실제 구현

스킴 그룹에 의존하지 않고, 액션의 바인딩을 순회하며 디바이스 레이아웃이 활성 디바이스와 맞는
단순 바인딩을 골라 표준 API `GetBindingDisplayString(bindingIndex, out layout, out controlPath)`로
`controlPath`를 얻는다. `controlPath`는 디바이스 prefix가 제거된 형태(`"1"`, `"leftButton"`,
`"dpad/up"`, `"buttonWest"`)로 반환되므로 SO 키와 그대로 매칭된다(Unity 공식 문서 확인).

```csharp
public static InputGlyphResult Resolve(string mapName, string actionName,
    ActiveInputDevice device, InputGlyphDataSO glyphData)
{
    if (InputManager.Instance == null) return InputGlyphResult.Missing(actionName);
    InputAction action = InputManager.Instance.GetAction(mapName, actionName);
    if (action == null) return InputGlyphResult.Missing(actionName);

    int bindingIndex = FindBindingIndexForDevice(action, device); // effectivePath prefix 매칭
    if (bindingIndex < 0) return InputGlyphResult.Missing(actionName);

    string display = action.GetBindingDisplayString(bindingIndex, out _, out string controlPath);

    if (glyphData != null && glyphData.TryResolve(device, controlPath, out Sprite sprite))
        return InputGlyphResult.WithSprite(sprite, display);
    return InputGlyphResult.TextOnly(display); // 미등록 키는 원문 텍스트로 가시화
}
```

> 컴포지트(`Move` 2DVector, `Dodge` OneModifier)는 키캡 1개로 표현하기 부적합하므로 건너뛴다.
> 단일 버튼 프롬프트(`CharacterSwap_1~4`, `Attack`, `Interact`, `Skill_1/2`, `Jump`, `Guard`, `Dash` 등)는 모두 단순 바인딩이라 정상 해석된다.

> 마우스 좌클릭(`Attack`)은 `controlPath = "leftButton"` → 매핑 테이블에서 좌클릭 아이콘. 키보드 `CharacterSwap_1`은 `"1"` → 숫자 1 아이콘(또는 키캡 스프라이트). 게임패드 `CharacterSwap_1`은 `"dpad/up"` → D-Pad ↑ 아이콘. 요구사항 2를 그대로 충족.

---

## 7. 표시 위젯

### 7.1 `UIInputPromptIcon` (독립 Image, HUD 슬롯용)

```csharp
public class UIInputPromptIcon : MonoBehaviour
{
    [SerializeField] private string _mapName = InputMapNames.PlayerAction;
    [SerializeField] private string _actionName;          // 인스펙터 드롭다운(에디터 확장 권장)
    [SerializeField] private InputGlyphDataSO _glyphData;
    [SerializeField] private Image _iconImage;
    [SerializeField] private TMP_Text _fallbackLabel;     // 스프라이트 없을 때

    private void OnEnable()
    {
        InputManager.Instance.OnActiveDeviceChanged += OnDeviceChanged;
        Refresh(InputManager.Instance.ActiveDevice);
    }
    private void OnDisable()
    {
        if (InputManager.HasInstance)
            InputManager.Instance.OnActiveDeviceChanged -= OnDeviceChanged;
    }
    private void OnDeviceChanged(ActiveInputDevice device) => Refresh(device);

    private void Refresh(ActiveInputDevice device)
    {
        var result = InputGlyphResolver.Resolve(_mapName, _actionName, device, _glyphData);
        // result에 따라 _iconImage.sprite 또는 _fallbackLabel.text 갱신
    }
}
```

- `OnActiveDeviceChanged` 구독만으로 일괄 교체가 성립한다(요구사항 1).
- HUD에 항상 떠 있는 프롬프트(예: 스킬 게이지 옆 키 표시)에 사용.

### 7.2 TMP 인라인 `<sprite>` 경로 (본문 텍스트용)

"[E] 상호작용", 다이얼로그 "[Space] 다음" 같은 **문장 안 키 표시**용. TMP Sprite Asset을 디바이스별로 1개씩 만들고(`Glyphs_KeyboardMouse`, `Glyphs_Gamepad`), 텍스트에 토큰(`{Interact}`)을 심은 뒤 런타임에 `<sprite name="...">`로 치환한다.

- 헬퍼 `InputPromptText.Format(string template)` 가 `{ActionName}` 토큰을 현재 디바이스의 `<sprite>` 태그로 치환.
- 디바이스 전환 시 해당 텍스트를 다시 포맷(구독한 위젯이 재호출).
- **범위 판단:** Phase 2. Phase 1은 독립 Image 위젯만으로 요구사항을 충족하고, 인라인은 다이얼로그/튜토리얼 도입 시 확장.

### 7.3 기존 `UI_HUD_InteractionKey` 통합

빈 껍데기인 `UI_HUD_InteractionKey`는 **`UIInputPromptIcon`을 내부에 품도록 리팩터**한다. 상호작용 가능 오브젝트 접근 시 노출되는 키 프롬프트가 디바이스에 따라 `F`(키보드) / `버튼 동그라미 East`(패드)로 자동 전환된다. 기존 `OnShow/OnHide` 생명주기는 유지하되 `SubscribeEvents`에서 글리프 갱신을 트리거.

---

## 8. 에셋 소싱

- **Kenney Input Prompts (CC0)** 채택: 키보드/마우스/Xbox/PS/Switch 글리프 일괄 제공, 라이선스 자유.
- 배치: `Assets/UI/InputGlyphs/KeyboardMouse/`, `Assets/UI/InputGlyphs/Gamepad/`.
- Phase 1 최소 세트: 숫자 1~4, WASD, Space, Shift, Ctrl, F/E/R/V/Z, 마우스 L/R/M, 패드 ABXY(제네릭)·D-Pad 4방향·L1/L2/R1/R2·스틱.
- TMP Sprite Asset은 위 스프라이트로부터 생성(Phase 2).

---

## 9. 엣지 케이스 / 함정 (구현 시 반드시 처리)

1. **스틱 드리프트·마우스 미세 이동 깜빡임** — §5.3 액추에이션 임계값으로 차단. 임계값은 인스펙터 노출해 튜닝 가능하게.
2. **레거시 `Gamepad` 액션맵 제외** — 글리프 해석은 `PlayerAction`/`UI` 맵만 소스로 삼는다. `Gamepad` 맵(테스트 전용)은 절대 해석 대상에 넣지 않는다. 중장기적으로 이 맵 자체를 정리(`AudioHapticsTest` 디커플)하는 것을 권장.
3. **게임패드 브랜드 차이 (Phase 3 구현됨)** — `buttonSouth`는 Xbox A / PS ✕ / Switch B. `InputManager.DetectBrand`가 디바이스 타입(`DualShockGamepad`/`SwitchProControllerHID`/`XInputController`)으로 브랜드를 판정하고, `OnActiveDeviceChanged`를 **브랜드 변경 시에도** 발화한다(클래스 변경에 게이트하지 않음 — 패드↔패드 브랜드 전환 누락 방지). `InputGlyphDataSO`는 브랜드별 오버라이드 리스트를 먼저 보고 **비어 있으면 제네릭으로 폴백**한다. 솔로 개발자는 제네릭만 채우면 동작하고, 브랜드 전용 아트가 생기면 생성툴의 "Xbox/PS/Switch 채우기" 버튼으로 옵트인한다.
4. **빌드 시 스프라이트 스트리핑** — 글리프 스프라이트가 씬에서 직접 참조되지 않고 SO를 통해 간접 참조되므로, 빌드에 포함되도록 `InputGlyphDataSO`가 항상 로드 경로(Addressables 또는 Resources)에 있어야 한다. 프로젝트는 Addressables 사용 중이므로 그에 맞춘다.
5. **누락 키 가시화** — 매핑 안 된 `controlPath`는 회색 박스가 아니라 `fallbackText`(원문 표시 문자열)로 노출해 디자이너가 누락을 즉시 인지.
6. **리바인딩 호환(미래)** — A안 표준 API는 리바인딩 결과를 자동 반영한다. 리바인딩 시스템 도입 시 `OnActiveDeviceChanged`와 동일하게 "바인딩 변경" 이벤트를 InputManager가 쏘면 위젯이 동일 경로로 갱신.
7. **단일 액션 다중 바인딩** — 한 디바이스 그룹에 바인딩이 2개 이상이면(`Dodge`에 `rightShoulder` 중복 존재) `GetBindingDisplayString`이 첫 매칭을 반환. 우선순위가 필요하면 SO에 액션별 우선 controlPath 오버라이드 필드를 둔다.

---

## 10. 구현 단계

| Phase | 범위 | 산출물 |
|-------|------|--------|
| **Phase 0 — 에셋/스킴 준비** | Kenney 팩 임포트, `.inputactions`에 `Gamepad` 스킴 추가 및 전 바인딩 그룹 태깅, Unity에서 검증 | 글리프 스프라이트 세트, 보강된 `PlayerInputActions.inputactions` |
| **Phase 1 — 감지 + 독립 위젯** | `ActiveInputDevice` enum, `InputManager.Device.cs`(onEvent 감지+디바운스+이벤트), `InputGlyphDataSO`, `InputGlyphResolver`, `UIInputPromptIcon`, `UI_HUD_InteractionKey` 통합 | 요구사항 1·2 충족(HUD 슬롯 기준) |
| **Phase 2 — 인라인 텍스트** | TMP Sprite Asset(디바이스별), `InputPromptText.Format`, 다이얼로그/튜토리얼 적용 | 본문 내 키 표시 |
| **Phase 3 — 브랜드 분기(선택)** | Gamepad description 기반 Xbox/PS/Switch 글리프 자동 전환 | 패드 브랜드별 정확한 버튼 표시 |

---

## 11. 미해결 결정 (구현 착수 전 확정 필요)

- **글리프 스타일** — 키캡형(테두리+글자) vs 순수 아이콘. 명조/원신은 키캡형 선호. 본문 가독성에 영향.
- **`actionName` 인스펙터 입력 방식** — 자유 문자열은 오타 위험. `InputDefine` 상수 기반 드롭다운 에디터 확장(`PropertyDrawer`)을 만들지 여부.
- **에셋 로드 경로** — 글리프 SO/스프라이트를 Addressables로 갈지 `Resources/`로 갈지(프로젝트는 둘 다 사용 중, `InputManager`는 `Resources` 사용).
- **인라인 텍스트 토큰 문법** — `{Interact}` vs `[Interact]` vs `<action:Interact>`. 다이얼로그 파서와 충돌하지 않는 문법 선택.

---

## 부록 A — 액션별 현재 바인딩 (해석 검증용)

`PlayerAction` 맵 기준, 키/마우스 ↔ 게임패드 동시 바인딩 현황(글리프 해석 테스트 케이스로 사용).

| 액션 | KeyboardMouse | Gamepad |
|------|---------------|---------|
| Move | W/A/S/D | leftStick |
| Look | Mouse delta | rightStick |
| Jump | Space | buttonSouth |
| Attack | Mouse Left | buttonWest |
| HeavyAttack | Mouse Right | buttonNorth |
| Interact | F | buttonEast |
| Skill_1 | E | leftTrigger |
| Skill_2 | R | rightTrigger |
| Guard | V | leftShoulder |
| Dash | Shift | rightShoulder |
| Dodge | Ctrl | rightShoulder |
| LockOn | Mouse Middle | rightStickPress |
| Sprint | Z | leftStickPress |
| CharacterSwap_1 | 1 | dpad/up |
| CharacterSwap_2 | 2 | dpad/right |
| CharacterSwap_3 | 3 | dpad/down |
| CharacterSwap_4 | 4 | dpad/left |
| LockOnSwitchRight | Tab | rightStick/right |
| LockOnSwitchLeft | (없음) | rightStick/left |

> **Dodge 게임패드 = L1+R1 복합 입력 (해결됨).** 게임패드 Dodge는 `rightShoulder` 단독이 아니라 OneModifier 컴포지트(`leftShoulder` 누른 채 `rightShoulder`)다. 게임패드에서 단독 `leftShoulder`=Guard, `rightShoulder`=Dash로 이미 쓰이므로 **자유 버튼이 없고**, 이는 의도된 콤보 입력이다. 리졸버가 컴포지트를 파트 리스트로 풀어 위젯이 "L1 + R1"로 표시한다(단독 R1만 보이면 Dash와 구분 불가). 키보드 Dodge는 `ctrl` 단일이라 그대로 1글리프. `Equip`은 키보드 `R`로 `Skill_2`와 충돌 — 별도 정리 대상.

---

## 부록 B — controlPath 인벤토리 (자동 생성 대상)

`UPlayGround/입력/글리프 데이터 생성·동기화` 메뉴가 PlayerInputActions 에셋(레거시 `Gamepad` 맵 + 순수 아날로그/포인터 제외)에서 자동 추출하는 controlPath 목록. `InputGlyphDataSO`의 키이자, 소싱할 키캡 글리프 스프라이트의 목록이다.

**키보드 / 마우스 (24)**
```
1  2  3  4              (캐릭터 스왑 / 숫자키)
space  shift  ctrl  z  tab  escape  alt  enter  backquote
e  r  f  v  i  o  m  p  (스킬/상호작용/장비/메뉴)
leftButton  rightButton  middleButton   (마우스 버튼)
```
> 컴포지트인 `wasd`(Move 2DVector)는 단일 키캡 대상이 아니므로 추출되지 않는다(이동 표기는 별도 처리).

**게임패드 (16)**
```
buttonSouth  buttonWest  buttonNorth  buttonEast   (페이스 버튼 — 제네릭 1세트)
dpad/up  dpad/right  dpad/down  dpad/left          (D-Pad)
leftShoulder  rightShoulder  leftTrigger  rightTrigger   (L1/R1/L2/R2)
leftStickPress  rightStickPress                    (스틱 클릭 L3/R3)
rightStick/right  rightStick/left                  (LockOnSwitch — 스틱 방향 이산 입력)
```

> **자동 제외(`InputGlyphDataGenerator.ExcludedSegments`):** `delta`·`position`·`scroll`(마우스 이동/커서/휠), `leftStick`·`rightStick`(스틱 전체 축). 키캡 글리프가 부적합한 순수 아날로그/포인터라 추출되지 않는다. 단, `rightStick/right`·`rightStick/left`는 스틱을 한 방향으로 튕기는 이산 입력(락온 전환)이라 **유지**한다.

---

## 부록 C — 스프라이트 자동 연결 매핑 (Kenney Input Prompts)

`InputGlyphDataGenerator.AutoLinkSprites`가 controlPath를 Kenney 파일명으로 매핑한다. 대상 폴더는 `Assets/ExternalAssets/UI/InputIcon/<시리즈>/Default`(표준 해상도). 매칭된 텍스처는 `TextureImporterType.Sprite`로 자동 변환된다. 전 controlPath 매핑이 실제 파일과 일치함을 검증했다(미매칭 0).

**키보드/마우스** (`Keyboard & Mouse/Default`)
- 단일 영숫자(`1`,`e`,`z`…) → `keyboard_<문자>`
- `space/shift/ctrl/tab/escape/alt/enter` → `keyboard_<이름>`, `backquote` → `keyboard_tilde`
- `leftButton`→`mouse_left`, `rightButton`→`mouse_right`, `middleButton`→`mouse_scroll`

**게임패드** (Xbox/제네릭 → `Xbox Series/Default`, PlayStation → `PlayStation Series/Default`)

| controlPath | Xbox(=제네릭) | PlayStation |
|---|---|---|
| buttonSouth/East/West/North | xbox_button_a/b/x/y | playstation_button_cross/circle/square/triangle |
| dpad/up·down·left·right | xbox_dpad_* | playstation_dpad_* |
| leftShoulder/rightShoulder | xbox_lb / xbox_rb | playstation_trigger_l1 / r1 |
| leftTrigger/rightTrigger | xbox_lt / xbox_rt | playstation_trigger_l2 / r2 |
| leftStickPress/rightStickPress | xbox_stick_l_press / r_press | playstation_button_l3 / r3 |
| rightStick/right·left | xbox_stick_r_right / left | playstation_stick_r_right / left |

> **제네릭 폴백은 Xbox 아트를 쓴다.** Kenney `Generic` 폴더는 `generic_button`·`generic_stick` 같은 추상 아이콘뿐이라 ABXY/D-Pad 등 controlPath 키에 1:1 매핑되지 않는다. 따라서 브랜드 미상(또는 `GamepadBrand.Generic`) 패드의 폴백은 가장 보편적으로 인식되는 Xbox 스타일로 채운다(업계 관례). PlayStation 패드만 PS 오버라이드로 분기된다. Switch 폴더는 미임포트 — 필요 시 폴더 추가 후 매핑 확장.
