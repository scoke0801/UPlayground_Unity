# UI 시스템 가이드

## 개요

UPlayground의 UI 시스템은 `UIManager`가 런타임 UI 루트, 캔버스 레이어, EventSystem, UI 프리팹 생명주기를 관리하는 구조다.

- `GameManager.InitializeManagers()`에서 `UIManager.Instance`가 등록되며 `Init()` 시점에 UI 루트를 구성한다.
- `UIRoot` Addressable 프리팹이 있으면 해당 프리팹을 생성하고, 없으면 코드 생성 방식으로 캔버스 레이어를 만든다.
- 씬에 `EventSystem`을 미리 배치하지 않아도 `UIManager`가 보장한다.
- UI 프리팹은 `UIPrefabDatabase`에 등록된 키와 기본 `CanvasLayer`를 기준으로 표시한다.
- 개별 UI는 `UI_Base`를 상속해 초기화, 표시, 숨김, 닫기, Back 동작을 구현한다.

---

## 아키텍처

```
GameManager
└── UIManager
    ├── UIRoot prefab (Addressables key: UIRoot)
    │   ├── Canvas_HUD
    │   ├── Canvas_Scene
    │   ├── Canvas_Popup
    │   ├── Canvas_System
    │   ├── Canvas_WorldSpace
    │   └── EventSystem
    ├── UIPrefabDatabase (Addressables key: UIPrefabDatabase)
    ├── DamageFloaterConfigSO (Addressables key: DamageFloaterConfig)
    └── UI_Base instances
```

### 파일 구조

```
Assets/
├── 02.Scripts/
│   ├── Manager/UIManager.cs
│   ├── UI/UI_Base.cs
│   ├── UI/UIManagerExtensions.cs
│   └── Data/Path/
│       ├── UIPrefabDatabase.cs
│       └── UIKeyType.cs
├── 03.Prefabs/UI/
│   └── UIRoot.prefab
└── 10.Datas/Path/
    └── UIPrefabDatabase.asset
```

---

## 캔버스 레이어

`CanvasLayer` 값은 정렬 순서와 입력 레이어 매핑의 기준이다.

| 레이어 | SortOrder | 용도 |
|--------|-----------|------|
| `HUD` | `0` | 인게임 HUD |
| `Scene` | `1000` | 씬 오버레이, 일반 메뉴 |
| `Popup` | `2000` | 인벤토리, 팝업, 선택 창 |
| `System` | `3000` | 설정, 로딩, 시스템 UI |
| `WorldSpace` | `10000` | 월드 위치 기반 HUD, HP바, 데미지 플로터 |

`UIManager`는 `UIRoot` 프리팹 안에서 다음 순서로 캔버스를 등록한다.

1. 자식 Canvas에 `UICanvasLayerBinding`이 있으면 해당 `Layer` 값을 사용한다.
2. 바인딩이 없으면 `Canvas_HUD`, `Canvas_Popup` 같은 오브젝트 이름으로 레이어를 추론한다.
3. 누락된 레이어는 기존 코드 생성 방식으로 보완한다.

---

## UI 루트와 EventSystem

`UIManager.Init()`은 다음 순서로 UI 환경을 만든다.

```csharp
CreateUIRoot();
CreateCanvasLayers();
EnsureEventSystem();
LoadAssetsAsync();
RegisterInputEvents();
```

`UIRoot` 프리팹을 수정할 때 지켜야 할 규칙:

- Addressables 주소는 `UIRoot`로 유지한다.
- 각 레이어 Canvas는 `Canvas_<CanvasLayer>` 이름을 쓰거나 `UICanvasLayerBinding`을 붙인다.
- Canvas에는 `CanvasScaler`, `GraphicRaycaster`를 포함한다.
- EventSystem은 프리팹 안에 둘 수 있다. 없으면 `UIManager`가 `EventSystem + InputSystemUIInputModule`을 생성한다.
- 씬에 남아 있는 별도 EventSystem은 런타임에 중복 제거될 수 있으므로 새 씬에는 배치하지 않는다.

---

## UI_Base

`UI_Base`는 모든 표시형 UI의 기본 클래스다.

| 멤버 | 역할 |
|------|------|
| `_layer` / `Layer` | UI가 속한 기본 `CanvasLayer` |
| `_canCloseWithEsc` / `IsCanCloseWithEsc` | Back 입력으로 닫을 수 있는지 여부 |
| `IsVisible` | 현재 표시 상태 |
| `IsInitialized` | 최초 초기화 완료 여부 |
| `Initialize()` | 최초 1회 `OnInit()` 호출 |
| `Show()` | 활성화, 입력 등록, `OnShow()` 호출 |
| `Hide()` | 입력 해제, `OnHide()` 호출, 비활성화 |
| `Close()` | `OnClose()` 호출. 실제 제거는 `UIManager.CloseUI()`가 담당 |
| `PerformBackFunction()` | Back 입력 처리. 기본 구현은 `Hide()` 후 `true` 반환 |
| `FadeIn()` / `FadeOut()` | `CanvasGroup` 기반 페이드 |
| `SetInteractable()` | `CanvasGroup.interactable`, `blocksRaycasts` 제어 |

`UI_Base`는 `[RequireComponent(typeof(Canvas))]`가 붙어 있으므로 UI 프리팹 루트에 Canvas가 필요하다. 루트가 레이어 Canvas 아래로 생성되더라도 개별 UI의 Canvas는 UI 내부 정렬과 raycast 제어에 사용된다.

---

## UI 표시 흐름

### 키 기반 표시

`UIPrefabDatabase`에 등록된 기본 레이어를 사용한다.

```csharp
UIManager.Instance.ShowUI(UIKeyType.Inventory);
UIManager.Instance.ShowUI(UIKeyType.PauseMenu);
```

### 레이어를 명시해서 표시

```csharp
UIManager.Instance.ShowUI(UIKeyType.PauseMenu, CanvasLayer.System);
```

### 프리팹 직접 표시

```csharp
[SerializeField] private GameObject _popupPrefab;

private void ShowPopup()
{
    UIManager.Instance.ShowUI(_popupPrefab, CanvasLayer.Popup, "CustomPopup");
}
```

### 타입으로 가져오기

```csharp
var inventory = UIManager.Instance.GetUI<UI_Inventory>(UIKeyType.Inventory);
if (inventory != null)
{
    inventory.SetInteractable(true);
}
```

---

## 커스텀 UI 작성

```csharp
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Manager;

public class UI_SamplePopup : UI_Base
{
    [SerializeField] private Button _closeButton;

    protected override void OnInit()
    {
        base.OnInit();
        _layer = CanvasLayer.Popup;
        _closeButton.onClick.AddListener(Hide);
    }

    protected override void OnShow()
    {
        base.OnShow();
        FadeIn(0.2f);
    }

    protected override void OnDispose()
    {
        _closeButton.onClick.RemoveAllListeners();
        base.OnDispose();
    }
}
```

프리팹 설정:

1. UI 루트 오브젝트에 Canvas와 `UI_Base` 상속 컴포넌트를 붙인다.
2. Inspector에서 `Layer`와 `Can Close With Esc`를 설정한다.
3. `UIPrefabDatabase.asset`에 key, prefab, defaultLayer를 등록한다.
4. 필요하면 `UIKeyType`에 키를 추가하고 `ToKey()` 매핑을 갱신한다.

---

## Back 입력 처리

`UIManager`는 `SystemAction.Back` 입력을 등록하고, 높은 `CanvasLayer`부터 표시 중인 `UI_Base`를 찾는다.

- `IsVisible == true`
- `IsCanCloseWithEsc == true`
- `PerformBackFunction()`이 `true`를 반환

열린 UI가 없고 현재 씬이 `SceneType.GamePlay`이면 `PauseMenu`를 토글한다.

커스텀 Back 동작이 필요하면 `PerformBackFunction()`을 오버라이드한다.

```csharp
public override bool PerformBackFunction()
{
    if (_isConfirmDialogOpen)
    {
        CloseConfirmDialog();
        return false;
    }

    Hide();
    return true;
}
```

---

## WorldSpace HUD

`UIManager`는 HUD Canvas 아래에 `UI_WorldSpaceHudLayer`를 준비한다.

| API | 역할 |
|-----|------|
| `CreateHpBar(GameActor actor)` | 액터 HP바 생성 |
| `ShowDamageFloater(Vector3, float, FloatStyle)` | 데미지 숫자 표시 |
| `ShowDamageFloaterMiss(Vector3)` | Miss 표시 |
| `ShowDamageFloaterHeal(Vector3, float, FloatStyle)` | 회복 숫자 표시 |

`DamageFloaterConfigSO`와 관련 UI 프리팹은 Addressables 로드가 완료된 뒤 풀링 설정된다. `UIManager.IsInitialized`가 `true`가 되기 전에는 DB 기반 UI 호출이 실패할 수 있다.

---

## 주의 사항

- 새 씬에는 EventSystem을 배치하지 않는다. UIManager가 `UIRoot` 또는 런타임 생성으로 보장한다.
- `CanvasLayer.Normal`, `CanvasLayer.Notification`, `CanvasLayer.Background`는 현재 존재하지 않는다. `Scene`, `Popup`, `System` 중 하나를 사용한다.
- `ShowUI()`는 `Initialize()`와 `Show()`를 내부에서 호출한다. 호출자가 다시 부를 필요가 없다.
- `HideUI()`는 오브젝트를 제거하지 않고 숨긴다. 완전 제거가 필요하면 `CloseUI()`를 사용한다.
- UI 프리팹 키를 문자열로 직접 쓰기보다 가능하면 `UIKeyType`을 사용한다.
- `UIRoot` 프리팹의 레이어 Canvas가 누락되어도 코드가 보완하지만, 의도한 정렬/스케일을 유지하려면 프리팹에 명시해두는 편이 좋다.

---

## 체크리스트

- [ ] UI 클래스가 `UI_Base`를 상속한다.
- [ ] UI 프리팹 루트에 Canvas가 있다.
- [ ] `Layer`와 `Can Close With Esc`가 의도대로 설정되어 있다.
- [ ] 버튼/이벤트 바인딩은 `OnInit()`에서 처리한다.
- [ ] 이벤트 해제는 `OnDispose()`에서 처리한다.
- [ ] `UIPrefabDatabase.asset`에 key, prefab, defaultLayer가 등록되어 있다.
- [ ] 새 씬에 별도 EventSystem을 추가하지 않았다.
