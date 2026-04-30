# Input 시스템 가이드

## 개요

Unity Input System(`InputActionAsset`) 위에 **레이어 우선순위** + **이벤트 라우팅** + **선입력 버퍼** + **커서 스택**을 더한 입력 매니저입니다. 모든 게임 입력은 `InputManager.RegisterInputEvent` 한 진입점을 통해 등록되고, `CurrentLayer` 보다 낮은 레이어 콜백은 자동 차단됩니다.

핵심 특징:

- **partial class 3 파일** — `InputManager.cs`(라이프사이클/커서/레이어), `.Action.cs`(InputActionAsset 캐싱), `.Event.cs`(콜백 라우팅 + 버퍼)
- **레이어 우선순위 차단** — `CurrentLayer` 가 등록 콜백의 `Layer` 보다 높으면 콜백 비활성화 (UI 진입 시 게임 입력 자동 차단)
- **레이어 변경 시 진행 중 입력 자동 Cancel** — `cancelCallback` 등록자에게 Cancel 알림 전파
- **InputBuffer 선입력** — Attack, Dodge, Skill 등 전투 입력은 0.15초 동안 버퍼에 보관 → 프레임 손실/타이밍 가드 우회
- **콜백 실행 중 레이어 변경 감지** — 한 콜백이 레이어를 바꾸면 같은 이벤트의 후속 콜백 자동 중단
- **커서 가시성 스택** — 여러 시스템이 동시에 커서 표시를 요청 가능, 모두 해제 시 자동 잠금
- **게임패드 활성 시 커서 자동 잠금** — 마우스/패드 혼용 UX

---

## 아키텍처

```
InputActionAsset (Resources: "Input/PlayerInputActions")
   │
   │  InputManager.InitInputAction (Init 단계)
   ▼
InputManager (BaseManager<T>, IManager) ── partial 3 파일
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
│   └── SetInputLayer(layer) → InvokeCancelEvents (레이어 하락한 콜백의 cancelCallback 발화)
│
└── ShowCursor / RefreshCursorState (게임패드 활성 시 자동 잠금)


콜백 라우팅 흐름:
  InputAction.started/performed/canceled
        │
        ▼
  InputManager.OnInputEventStarted/Performed/Canceled
        │
        ├── (Performed + Level_0) 전투 액션이면 → _inputBuffer.AddInput
        │
        ▼
  ExecuteCallbacks(dict)
        │  for each callbackData:
        │     ├── Layer 검사     : data.Layer < CurrentLayer  → skip
        │     ├── CheckFunc 검사 : checkFunc()? false        → skip
        │     ├── Callback 실행
        │     └── 레이어 변경 감지 → break (후속 콜백 중단)
```

### 파일 구조

```
Assets/02.Scripts/
├── Manager/Input/
│   ├── InputManager.cs              라이프사이클 + 커서 + SetInputLayer + ShowCursor
│   ├── InputManager.Action.cs       InputActionAsset 캐싱 + Enable/Disable
│   └── InputManager.Event.cs        Register/Unregister + ExecuteCallbacks + Buffer 적재
│
└── Input/
    ├── InputDefine.cs               InputMapNames / PlayerAction / SystemAction / UIAction / GamepadAction / InputLayer
    └── InputBuffer.cs               BufferedInput + InputBuffer (선입력 큐)

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
| `Dispose` | 콜백 딕셔너리 Clear |

#### 콜백 등록

```csharp
public void RegisterInputEvent(
    string mapName,
    string actionName,
    Action<InputAction.CallbackContext> started,    // null 가능
    Action<InputAction.CallbackContext> performed,  // null 가능
    Action<InputAction.CallbackContext> canceled,   // null 가능
    Func<bool>                          checkFunc,  // null 가능 (false 반환 시 skip)
    Action                              cancelCallback,  // 레이어 하락 시 호출
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
| `SetInputLayer(InputLayer)` | 레이어 변경. `None` 전달 시 `UIManager.GetTopCanvasLayer().ToInputLayer()` 자동 적용. 변경 시 `InvokeCancelEvents` 호출 |
| `ShowCursor(bool show, bool isForce=false)` | 가시성 스택 push/pop. `isForce`이면 스택 초기화 |
| `InputBuffer` | InputBuffer 인스턴스 직접 접근 |

#### Action 직접 접근

| API | 용도 |
|-----|------|
| `GetAction(map, name)` | InputAction 인스턴스 |
| `SetActionEnabled(map, name, enabled)` | 개별 Action 토글 |

### 자동 버퍼 적재 액션

`OnInputEventPerformed`에서 `CurrentLayer == Level_0` 일 때 다음 액션은 자동으로 InputBuffer에 적재된다:

```
Attack / HeavyAttack / Dodge / Jump / Dash / PlayerSwap
Skill_1 / Skill_2 / Skill_3 / Skill_4
```

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

    // 5. 콜백이 레이어를 바꿨다면 후속 콜백 중단
    if (cachedLayer != CurrentLayer) break;
}
```

> **동작 의미:** 동일 액션에 여러 콜백이 등록되어 있어도, 첫 번째 콜백이 UI를 띄우면서 레이어를 올리면 나머지 콜백은 실행되지 않는다. 자연스러운 입력 우선순위 구현.

### 레이어 하락 시 Cancel 전파 (`InvokeCancelEvents`)

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

---

## 사용 예시

### 1. 게임 중 공격 입력 등록 (PlayerCombat)

```csharp
private void OnEnable()
{
    InputManager.Instance.RegisterInputEvent(
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
    InputManager.Instance.UnRegisterInputEvent(
        InputMapNames.PlayerAction, PlayerAction.Attack,
        null, OnAttackPerformed, null);
}
```

### 2. 시스템(어디서든 동작) 입력 등록

```csharp
// 커서 토글 — Level_Top 이라 UI / 시스템 메뉴 위에서도 동작
InputManager.Instance.RegisterInputEvent(
    InputMapNames.System, SystemAction.ShowCursor,
    OnStartedShowCursor, null, OnCanceledShowCursor,
    null, null, InputLayer.Level_Top);
```

### 3. UI 열 때 레이어 변경

```csharp
public override void Show()
{
    base.Show();
    InputManager.Instance.SetInputLayer(InputLayer.Level_2);  // Popup
}

public override void Hide()
{
    base.Hide();
    InputManager.Instance.SetInputLayer(InputLayer.None);     // 자동 — UIManager의 최상위 캔버스 기준
}
```

### 4. 선입력 버퍼 소비 (전투 상태 머신)

```csharp
// PlayerAttackState : 콤보 윈도우 안에서 다음 공격 입력이 있으면 콤보 진행
if (InputManager.Instance.InputBuffer.ConsumeInput(PlayerAction.Attack) != null)
{
    _combat.ExecuteAttack(isCombo: true);
}
```

```csharp
// PlayerGuardState : 퍼펙트 가드 카운터 창에서 Attack 입력 검사
if (_combat.IsPerfectGuardCounterAvailable &&
    InputManager.Instance.InputBuffer.ConsumeInput(PlayerAction.Attack) != null)
{
    // 반격 전환
}
```

### 5. 액션 토글 (특정 입력 임시 비활성화)

```csharp
// 컷씬 중 점프 비활성
InputManager.Instance.SetActionEnabled(InputMapNames.PlayerAction, PlayerAction.Jump, false);

// 컷씬 종료
InputManager.Instance.SetActionEnabled(InputMapNames.PlayerAction, PlayerAction.Jump, true);
```

### 6. 커서 표시 (인벤토리)

```csharp
public override void Show()
{
    base.Show();
    InputManager.Instance.ShowCursor(true);   // 스택 +1
}

public override void Hide()
{
    base.Hide();
    InputManager.Instance.ShowCursor(false);  // 스택 -1
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
   - 새 전투 액션을 자동 버퍼링 대상에 추가하려면 `InputManager.OnInputEventPerformed`의 switch에 case 추가

---

## 주의 사항

- **Register/Unregister 페어링 필수.** `OnEnable`에서 등록했다면 `OnDisable`에서 반드시 해제. 미해제 시 destroyed 콜백이 남아 NRE 위험.
- **CheckFunc은 매 호출.** 콜백 실행마다 평가되므로 가벼워야 함. 무거운 검사는 캐시.
- **Layer < CurrentLayer 만 차단.** `data.Layer == InputLayer.None` 은 차단되지 않는다 (어디서든 통과). `Level_Top`이 아닌 일반 `None` 사용은 의도하지 않은 항상-동작 입력을 만들 수 있음에 주의.
- **레이어 변경은 한 번에 한 곳에서.** UI Show/Hide마다 SetInputLayer를 호출하면 빠르게 토글되며 cancelCallback이 의도치 않게 발화될 수 있다. UIManager 단에서 일원화 권장.
- **InputBuffer는 단일 인스턴스.** 멀티 플레이어/스플릿스크린 도입 시 플레이어별 버퍼로 분리 필요.
- **버퍼 적재 조건은 `Level_0` 한정.** UI 위에서 들어오는 전투 키는 버퍼에 들어가지 않음. 의도된 설계 — 인벤토리 보면서 누른 공격 키가 닫자마자 발화하는 것을 방지.
- **레이어 변경 중 콜백 내 변경.** 콜백이 `SetInputLayer`를 호출하면 `cachedLayer != CurrentLayer` 가 감지되어 같은 이벤트의 후속 콜백은 즉시 중단된다. 의도하지 않은 동작이 일어나면 이 룰을 점검.
- **게임패드 자동 잠금.** `_isGamepadActive == true`일 때는 ShowCursor(true)도 잠금 상태가 유지된다. 패드 사용자에게는 마우스 커서를 노출하지 않는 정책. 패드 활성 갱신 코드는 외부에서 `_isGamepadActive`를 변경하는 진입점이 별도 존재해야 한다 (현재 코드상 set 진입점 미공개 — 확장 포인트 참조).
- **InputAction 예외.** `inputActions == null` 시 Init이 에러 로그 후 조기 반환. 빌드된 환경에서 Resources 경로가 정확한지 반드시 확인.

---

## 확장 포인트

### 신규 액션 / 액션맵 추가

1. `PlayerInputActions.inputactions` 에 액션 추가
2. `InputDefine.cs` 의 해당 정적 클래스에 string 상수 추가
3. 코드에서 `RegisterInputEvent(InputMapNames.X, ActionDef.Y, ...)` 사용

### 자동 버퍼 대상 변경

`InputManager.Event.cs`의 `OnInputEventPerformed` switch에 case 추가/제거. 또는 더 일반화된 화이트리스트(HashSet) 기반으로 리팩토링.

### 레이어 추가 / 정책 변경

`InputLayer` enum에 새 레벨 추가. CanvasLayer 매핑 헬퍼(`ToInputLayer`)도 함께 갱신.

### 게임패드 활성 토글 진입점

`_isGamepadActive` 외부 set이 필요. `Input.deviceChange` 이벤트 또는 InputUser API를 구독해 자동 갱신하는 보조 코드를 추가하면 좋다.

### 멀티 콜백 우선순위

현재 동일 액션의 콜백은 등록 순서로 평가. 명시적 priority 필드를 `InputCallbackData`에 추가하고 등록 시 정렬 삽입하는 식으로 확장 가능.

### 다중 InputBuffer (캐릭터 / 스플릿)

`PartyManager`로 캐릭터를 교체할 때 버퍼를 캐릭터별로 분리하면 캐릭터 전환 직후의 잘못된 콤보 입력 우회. `InputBuffer` 인스턴스를 `Dictionary<CharacterActorType, InputBuffer>` 로 보관.

### 디버그 모니터

런타임 창으로 `actionCache` / 등록 콜백 개수 / `_inputBuffer.DebugPrint()` 결과를 표시하면 입력 누락/중복/등록 누수 진단에 유용.
