# Input 시스템 가이드

## 개요

Unity Input System(`InputActionAsset`) 위에 **레이어 우선순위** + **이벤트 라우팅** +
**조합 중재** + **선입력 버퍼** + **장치 감지** + **리바인딩 프로필**을 더한 입력
매니저입니다. 공용 소비자는 `Svc.Input`의 `IInputService`를 사용하며, `CurrentLayer`보다
낮은 레이어 콜백은 자동 차단됩니다.

핵심 특징:

- **partial class 7 파일** — 생명주기, 액션 캐시, 이벤트 라우팅, 장치 추적, 조합 중재,
  바인딩 프로필, 리바인딩 캡처를 기능별로 분리
- **레이어 우선순위 차단** — `CurrentLayer` 가 등록 콜백의 `Layer` 보다 높으면 콜백 비활성화 (UI 진입 시 게임 입력 자동 차단)
- **레이어 변경·입력 억제 시 진행 중 입력 자동 Cancel** — `cancelCallback` 등록자에게 Cancel 알림 전파
- **InputBuffer 선입력** — Attack, Dodge, Skill 등 전투 입력은 0.15초 동안 버퍼에 보관 → 프레임 손실/타이밍 가드 우회
- **콜백 실행 중 컨텍스트 변경 감지** — 한 콜백이 레이어를 바꾸거나 PlayerAction 억제를 시작하면 같은 이벤트의 후속 콜백 자동 중단
- **커서 가시성 스택** — 여러 시스템이 동시에 커서 표시를 요청 가능, 모두 해제 시 자동 잠금
- **게임패드 활성 시 커서 자동 잠금** — 마우스/패드 혼용 UX
- **장치·브랜드 추적** — `ActiveDevice`, `GamepadBrand`, `OnActiveDeviceChanged`
- **리바인딩 프로필** — Primary/Secondary 단일·2키 조합, 충돌 처리, GUID 기반 저장·이전
- **장치별 UI 글리프** — 활성 장치와 바인딩 변경을 `UIInputPromptIcon`과
  `UIInputPromptBar`가 즉시 반영

---

## 아키텍처

```
InputActionAsset (Resources: "Input/PlayerInputActions")
   │
   │  InputManager.InitInputAction (Init 단계)
   ▼
InputManager (BaseManager<T>, IManager, IInputService) ── partial 7 파일
│
├── actionCache    : Dictionary<(map, name), InputAction>
├── actionMapCache : Dictionary<map, InputActionMap>
│
├── 콜백 라우팅
│   ├── startCallbackDict   : (map, name) → List<InputCallbackData>
│   ├── performCallbackDict
│   └── cancelCallbackDict
│
├── _inputBuffer : InputBuffer (선입력 큐)
│
├── _cursorVisibleStack : int  (커서 표시 요청 스택)
│
├── CurrentLayer : InputLayer
│   └── RefreshInputLayer() → UIManager 차단 UI 기준 재계산 + Cancel 전파
│
└── ShowCursor / RefreshCursorState (게임패드 활성 시 자동 잠금)


콜백 라우팅 흐름:
  InputAction.started/performed/canceled
        │
        ▼
  InputManager.OnInputEventStarted/Performed/Canceled
        │
        ├── 입력 억제/포인터/리바인딩 게이트 (`canceled`는 해제 대칭을 위해 통과)
        ▼
  InputChordArbiter
        │  긴 조합 우선 및 단일키 grace 확정
        ├── 확정된 전투 입력만 InputBuffer 적재 (확정 시점부터 액션별 전체 버퍼 창 보장)
        │
        ▼
  ExecuteCallbacks(dict)
        │  for each callbackData:
        │     ├── Layer 검사     : data.Layer < CurrentLayer  → skip
        │     ├── CheckFunc 검사 : checkFunc()? false        → skip
        │     ├── Callback 실행
        │     └── 레이어 변경/PlayerAction 억제 감지 → break (후속 콜백 중단)
```

### 파일 구조

```
Assets/02.Scripts/
├── Manager/Input/
│   ├── InputManager.cs              라이프사이클 + 커서 + 레이어 재계산 + ShowCursor
│   ├── InputManager.Action.cs       InputActionAsset 캐싱 + Enable/Disable
│   ├── InputManager.Event.cs        Register/Unregister + 게이트 + 콜백 실행
│   ├── InputManager.Device.cs       활성 장치/게임패드 브랜드/연결 해제
│   ├── InputManager.Chord.cs        조합 우선 중재 + InputBuffer 확정 적재
│   ├── InputManager.BindingProfile.cs GUID 기반 override 저장·로드·적용
│   └── InputManager.Rebinding.cs    단일키·2키 조합 캡처 세션
│
├── Data/Input/
│   ├── InputDefine.cs               맵/액션/레이어 상수
│   ├── InputBuffer.cs               timestamp 지원 선입력 큐
│   ├── InputChordArbiter.cs         Input System 비의존 조합 판정
│   ├── InputBindingProfileMigration.cs
│   └── InputRebindingTypes.cs
│
└── UI/InputPrompt/
    ├── InputGlyphResolver.cs
    ├── InputPromptAvailability.cs
    ├── UIInputPromptIcon.cs
    └── UIInputPromptBar.cs

Assets/Resources/Input/
└── PlayerInputActions.inputactions  Unity Input System 설정
```

---

## 핵심 클래스 / API

### InputLayer

```csharp
public enum InputLayer
{
    None     = -1,
    Level_0  = 0,      // == HUD          (인게임)
    Level_1  = 1000,   // == Scene        (씬 오버레이)
    Level_2  = 2000,   // == Popup        (인벤토리/팝업)
    Level_3  = 3000,   // == System       (시스템/설정)
    Level_Top = 10000  // 어디서든 통과해야 하는 입력 (커서 토글 등)
}
```

> CanvasLayer 값과 1:1 대응. `UIManager.GetTopCanvasLayer().ToInputLayer()` 헬퍼로 자동 매핑됨.

### InputMapNames / PlayerAction / SystemAction / UIAction / GamepadAction

각 InputMap / Action 이름이 string 상수로 정의되어 있어 컴파일 타임 안전 사용 가능. 예:

```csharp
InputMapNames.PlayerAction → "PlayerAction"
PlayerAction.Attack        → "Attack"
SystemAction.ShowCursor    → "ShowCursor"
UIAction.Cancel            → "Cancel"
```

### InputManager (Public API)

#### 라이프사이클

| API | 동작 |
|-----|------|
| `Init` | InputBuffer 생성 + 커서 텍스처 로드 + `InitInputAction()` + ShowCursor 등록(`Level_Top`) |
| `Dispose` | InputAction 델리게이트 분리 + 액션/콜백 캐시 Clear |

#### 콜백 등록

```csharp
public void RegisterInputEvent(
    string mapName,
    string actionName,
    Action<InputAction.CallbackContext> started,    // null 가능
    Action<InputAction.CallbackContext> performed,  // null 가능
    Action<InputAction.CallbackContext> canceled,   // null 가능
    Func<bool>                          checkFunc,  // null 가능 (false 반환 시 skip)
    Action                              cancelCallback,  // 레이어 상승·입력 억제 시 호출
    InputLayer                          inputLayer);
```

```csharp
public void UnRegisterInputEvent(
    string mapName, string actionName,
    Action<InputAction.CallbackContext> started,
    Action<InputAction.CallbackContext> performed,
    Action<InputAction.CallbackContext> canceled);
```

#### 레이어 / 커서

| API | 용도 |
|-----|------|
| `CurrentLayer` | 현재 활성 입력 레이어 |
| `RefreshInputLayer()` | 열린 차단 UI를 기준으로 레이어 재계산. 변경 시 pending 조합 초기화와 Cancel 전파 |
| `ShowCursor(bool show, bool isForce=false)` | 가시성 스택 push/pop. `isForce`이면 스택 초기화 |
| `InputBuffer` | InputBuffer 인스턴스 직접 접근 |
| `ActiveDevice` / `GamepadBrand` | 현재 입력 장치군과 게임패드 브랜드 |
| `OnActiveDeviceChanged` | 키보드·마우스/게임패드 전환 알림 |
| `OnBindingsChanged` | effective binding 변경 알림 |

#### Action 직접 접근

| API | 용도 |
|-----|------|
| `GetAction(map, name)` | InputAction 인스턴스 |
| `SetActionEnabled(map, name, enabled)` | 개별 Action 토글 |

#### 리바인딩

| API | 용도 |
|-----|------|
| `GetBindingDescriptors(deviceGroup)` | 설정 화면의 액션·슬롯 목록 |
| `CaptureBindingAsync(target)` | 단일키 또는 2키 조합 캡처 |
| `TryApplyBinding(capture, replaceConflict, out conflict)` | 충돌 검사 후 임시 프로필 반영 |
| `CaptureBindingProfileSnapshot()` / `RestoreBindingProfileSnapshot(json)` | 설정 Apply/Cancel 기준점 |
| `SaveBindingProfile()` | PlayerPrefs JSON 영구 저장 |
| `ResetBinding*` / `ClearBinding` | 슬롯·액션·장치·전체 초기화 |

### 자동 버퍼 적재 액션

`OnInputEventPerformed`에서 `CurrentLayer == Level_0` 일 때 다음 액션은 자동으로 InputBuffer에 적재된다:

```
Attack / Dodge / Jump / Dash
SkillAbility / SkillUltimate / ElementBuff
CharacterSwap_1 / CharacterSwap_2 / CharacterSwap_3 / CharacterSwap_4
```

`HeavyAttack`은 예외다. 같은 버튼의 짧은 누름과 차지를 구분해야 하므로 `performed`에서는
버퍼링하지 않고, `PlayerActor.OnHeavyAttackCanceled`가 짧은 누름으로 판정한 릴리스에서만
한 번 적재한다. 대시 공격에 사용한 입력과 릴리스 입력이 각각 강공격으로 중복 확정되지 않게 하는 계약이다.

소비는 호출자(예: `PlayerAttackState`, `PlayerGuardState`)가 `InputBuffer.ConsumeInput("Attack")` 으로.

### InputBuffer

| API | 용도 |
|-----|------|
| `new InputBuffer(bufferTime=0.15s, maxSize=10)` | 인스턴스 생성 |
| `AddInput(name, data=null)` | 입력 추가. `maxSize` 초과 시 가장 오래된 항목 제거 |
| `HasInput(name) → bool` | 만료된 항목 청소 후 존재 여부 |
| `ConsumeInput(name) → BufferedInput?` | 매칭 첫 항목을 꺼내고 제거 |
| `GetLatestInput() → BufferedInput?` | 가장 최근 항목 |
| `Count` | 만료 청소 후 개수 |
| `Clear()` | 비우기 |
| `DebugPrint()` | 디버그 로그 |

만료 시간은 인스턴스 생성 시 결정 (현재 매니저는 기본 0.15초).

---

## 콜백 실행 룰 (ExecuteCallbacks)

```csharp
foreach (var data in callbackList)
{
    // 1. 레이어 검사
    if (data.Layer != InputLayer.None && data.Layer < CurrentLayer)
        continue;

    // 2. 조건 함수 검사 (예: !PlayerAlive() 면 skip)
    if (data.CheckFunc != null && !data.CheckFunc.Invoke())
        continue;

    // 3. 실행 전 레이어 캐시
    var cachedLayer = CurrentLayer;

    // 4. 콜백 실행
    data.Callback?.Invoke(context);

    // 5. 콜백이 레이어를 바꾸거나 PlayerAction 억제를 시작했다면 후속 콜백 중단
    if (cachedLayer != CurrentLayer || IsPlayerActionCurrentlySuppressed()) break;
}
```

> **동작 의미:** 동일 액션에 여러 콜백이 등록되어 있어도, 첫 번째 콜백이 UI를 띄우거나 PlayerAction 억제를 시작하면 나머지 콜백은 실행되지 않는다. 자연스러운 입력 우선순위 구현.

### 입력 차단 시 Cancel 전파 (`InvokeCancelEvents`)

```csharp
// 새 레이어가 더 높아져서 비활성화된 콜백들의 cancelCallback을 1회 발화
foreach (var data in 모든 콜백)
{
    if (data.Layer != None && data.Layer < CurrentLayer && data.CancelCallback != null)
    {
        if (executedCancels.Add(data.CancelCallback))
            data.CancelCallback.Invoke();
    }
}
```

> **활용:** UI 팝업이 열리며 레이어가 `Level_2`로 올라갈 때, `Level_0`에 등록된 진행 중 입력(예: 차지 중)의 `cancelCallback`이 발화돼 차지가 자동 해제됨.

콜백이 실행 중 자신을 등록 해제해도 현재 리스트를 즉시 당기지 않고 비활성 표식만 남긴다.
최외곽 디스패치가 끝난 뒤 한 번에 정리하므로 같은 액션의 다음 콜백을 건너뛰지 않는다.
반대로 같은 액션 디스패치 중 새로 등록된 콜백은 현재 입력이 아니라 다음 입력부터 받는다.

---

## 사용 예시

### 1. 게임 중 공격 입력 등록 (PlayerCombat)

```csharp
private void OnEnable()
{
    Svc.Input.RegisterInputEvent(
        InputMapNames.PlayerAction, PlayerAction.Attack,
        started:        null,
        performed:      OnAttackPerformed,
        canceled:       null,
        checkFunc:      () => _playerActor.IsAlive,
        cancelCallback: () => _playerActor.CancelCharge(),
        inputLayer:     InputLayer.Level_0);
}

private void OnDisable()
{
    Svc.Input.UnRegisterInputEvent(
        InputMapNames.PlayerAction, PlayerAction.Attack,
        null, OnAttackPerformed, null);
}
```

### 2. 시스템(어디서든 동작) 입력 등록

```csharp
// 커서 토글 — Level_Top 이라 UI / 시스템 메뉴 위에서도 동작
Svc.Input.RegisterInputEvent(
    InputMapNames.System, SystemAction.ShowCursor,
    OnStartedShowCursor, null, OnCanceledShowCursor,
    null, null, InputLayer.Level_Top);
```

### 3. UI 열기/닫기

```csharp
public override void Show()
{
    // UI_Base가 표시 상태를 반영한 뒤 InputManager.RefreshInputLayer를 호출한다.
    base.Show();
}

public override void Hide()
{
    // 중첩 UI가 있으면 그 UI의 레이어가 유지된다.
    base.Hide();
}
```

### 4. 선입력 버퍼 소비 (전투 상태 머신)

```csharp
// PlayerAttackState : 콤보 윈도우 안에서 다음 공격 입력이 있으면 콤보 진행
if (Svc.Input?.InputBuffer.ConsumeInput(PlayerAction.Attack) != null)
{
    _combat.ExecuteAttack(isCombo: true);
}
```

```csharp
// PlayerGuardState : 퍼펙트 가드 카운터 창에서 Attack 입력 검사
if (_combat.IsPerfectGuardCounterAvailable &&
    Svc.Input?.InputBuffer.ConsumeInput(PlayerAction.Attack) != null)
{
    // 반격 전환
}
```

### 5. 플레이어 액션 임시 억제

```csharp
// 컷씬/리바인딩 전환처럼 게임플레이 입력 전체를 막아야 하는 구간
Svc.Input?.SetPlayerActionInputSuppressed(true);

// 컷씬 종료
Svc.Input?.SetPlayerActionInputSuppressed(false);
```

### 6. 커서 표시 (인벤토리)

```csharp
public override void Show()
{
    base.Show();
    Svc.Input?.ShowCursor(true);   // 스택 +1
}

public override void Hide()
{
    base.Hide();
    Svc.Input?.ShowCursor(false);  // 스택 -1
}
```

여러 UI가 중첩으로 ShowCursor(true)를 호출해도 모두 ShowCursor(false)로 정리되면 자동으로 잠금. 게임패드 활성 시에는 잠금 상태로 자동 강제됨.

---

## 셋업 방법

1. **PlayerInputActions.inputactions 작성**
   - `Resources/Input/PlayerInputActions.inputactions` 위치 (Resources 로드)
   - ActionMap: `PlayerAction`, `UI`, `System`, (선택) `Gamepad`
   - 각 ActionMap의 Action 이름은 `InputDefine.PlayerAction.*` 등의 상수와 일치해야 함
2. **커서 텍스처**
   - `Resources/Cursor/cursor_default.png`
3. **GameManager 등록 순서**
   - `[2] InputManager` — Init에서 ActionAsset 로드, `Level_Top` 으로 `ShowCursor` 등록
4. **UIManager 연동**
   - `UIManager.GetTopCanvasLayer().ToInputLayer()` 가 동작하려면 UIManager에 캔버스 레이어 정보가 정확히 반영되어야 함
5. **버퍼 시간 / 액션 추가 (선택)**
   - 새 전투 액션을 자동 버퍼링 대상에 추가하려면
     `InputManager.Chord.cs`의 `TryBufferPlayerAction`과
     `PlayerInputBufferPolicy.GetDuration`을 함께 갱신

### 입력 프롬프트 Editor 도구

메뉴 `Tools/UI/Input Prompt/` 아래에서 다음 도구를 사용한다.

| 메뉴 | 동작 |
|-----|------|
| `프리팹 마이그레이션` | 11개 전체 화면 UI 프리팹에 장치 반응형 프롬프트를 반복 적용 |
| `전체 계약 검증` | 액션 GUID·빈 경로·물리 경로 충돌·브랜드 글리프·프리팹 직렬화·Missing Script 검사 |
| `EditMode 테스트 실행` | `UPlayGround.UI.Tests` 실행 후 `Temp/UIInputPromptTestResults.xml` 저장 |

프리팹 마이그레이션은 `PrefabUtility.LoadPrefabContents`를 사용하며 기존 루트와 직렬화
참조를 유지한다. 입력 에셋에서 동일 물리 경로를 공유해야 하는 조합·문맥 액션은
`UIInputPromptPrefabTool.AllowedPhysicalPathSharing`에 정확한 액션 집합을 선언해야 한다.
새로운 의도하지 않은 중복이나 빈 binding path는 EditMode 계약 테스트를 실패시킨다.

---

## 주의 사항

- **Register/Unregister 페어링 필수.** `OnEnable`에서 등록했다면 `OnDisable`에서 반드시 해제. 미해제 시 destroyed 콜백이 남아 NRE 위험.
- **CheckFunc은 매 호출.** 콜백 실행마다 평가되므로 가벼워야 함. 무거운 검사는 캐시.
- **Layer < CurrentLayer 만 차단.** `data.Layer == InputLayer.None` 은 차단되지 않는다 (어디서든 통과). `Level_Top`이 아닌 일반 `None` 사용은 의도하지 않은 항상-동작 입력을 만들 수 있음에 주의.
- **레이어 변경은 공용 경로로.** UI는 `UI_Base.Show/Hide`를 사용하고,
  `InputManager.RefreshInputLayer`가 `UIManager.GetTopBlockingInputLayer()` 결과를 반영한다.
- **InputBuffer는 단일 인스턴스.** 멀티 플레이어/스플릿스크린 도입 시 플레이어별 버퍼로 분리 필요.
- **버퍼 적재 조건은 `Level_0` 한정.** UI 위에서 들어오는 전투 키는 버퍼에 들어가지 않음. 의도된 설계 — 인벤토리 보면서 누른 공격 키가 닫자마자 발화하는 것을 방지.
- **콜백 실행 중 UI 전환.** 콜백이 UI를 열어 `CurrentLayer`가 바뀌면
  `cachedLayer != CurrentLayer`가 감지되어 같은 이벤트의 후속 콜백은 즉시 중단된다.
- **게임패드 자동 잠금.** `InputManager.Device.cs`가 `InputSystem.onEvent`와 장치 변경을
  추적해 `_isGamepadActive`를 갱신한다. 별도 외부 setter를 추가하지 않는다.
- **InputAction 예외.** `inputActions == null` 시 Init이 에러 로그 후 조기 반환. 빌드된 환경에서 Resources 경로가 정확한지 반드시 확인.

---

## 확장 포인트

### 신규 액션 / 액션맵 추가

1. `PlayerInputActions.inputactions` 에 액션 추가
2. `InputDefine.cs` 의 해당 정적 클래스에 string 상수 추가
3. 코드에서 `RegisterInputEvent(InputMapNames.X, ActionDef.Y, ...)` 사용

### 자동 버퍼 대상 변경

`InputManager.Chord.cs`의 버퍼 대상 switch와 Data 모듈의
`PlayerInputBufferPolicy`를 함께 갱신한다. 조합 유예가 있는 액션도 중재 확정 뒤부터
정해진 버퍼 시간을 모두 받는다.

### 레이어 추가 / 정책 변경

`InputLayer` enum에 새 레벨 추가. CanvasLayer 매핑 헬퍼(`ToInputLayer`)도 함께 갱신.

### 입력 컨텍스트 스택

현재 입력 문맥 권위는 `InputLayer`와 리바인딩 캡처 게이트다. 토큰 기반 Input Context
Stack은 스펙에 남아 있지만 아직 런타임 계약에 추가하지 않았다. UI 소비자가 임의의 별도
문맥 스택을 만들지 말고, 도입 전까지 `UI_Base`/`RefreshInputLayer` 경로를 사용한다.

### 멀티 콜백 우선순위

현재 동일 액션의 콜백은 등록 순서로 평가. 명시적 priority 필드를 `InputCallbackData`에 추가하고 등록 시 정렬 삽입하는 식으로 확장 가능.

### 다중 InputBuffer (캐릭터 / 스플릿)

`PartyManager`로 캐릭터를 교체할 때 버퍼를 캐릭터별로 분리하면 캐릭터 전환 직후의 잘못된 콤보 입력 우회. `InputBuffer` 인스턴스를 `Dictionary<CharacterActorType, InputBuffer>` 로 보관.

### 디버그 모니터

런타임 창으로 `actionCache` / 등록 콜백 개수 / `_inputBuffer.DebugPrint()` 결과를 표시하면 입력 누락/중복/등록 누수 진단에 유용.
