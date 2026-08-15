# UI 파일 생성

사용자가 제공한 인자(`$ARGUMENTS`)를 파싱하여 이 Unity 프로젝트에 UI 파일을 생성한다.

## 인자 파싱 규칙

`$ARGUMENTS` 형식: `<UIName> [<Category>] [<Layer>]`

- `UIName` — 필수. 접두사 없이 순수 이름만 입력 (예: `BossHealth`). `UI_`, `UI_HUD_` 같은 접두사가 붙어 들어와도 떼어내고 순수 이름만 남긴다.
  - 이름 본문에 레이어를 뜻하는 단어(`Hud`, `Scene`, `Popup`, `System`)를 중복해 넣지 않는다. (`HudBossHealth` → `BossHealth`)
- `Category` — 선택. 스크립트 서브폴더. 없으면 UIName에서 추론:
  - 이름에 `Hud` 포함 → `HUD`
  - 이름에 `Inventory` 또는 `Item` 포함 → `Inventory`
  - 이름에 `Dialogue` 포함 → `Dialogue`
  - 이름에 `Interaction` 포함 → `Interaction`
  - 이름에 `World` 포함 → `WorldSpace`
  - 그 외 → `Scene`
- `Layer` — 선택. `CanvasLayer` 값 (`HUD`, `Scene`, `Popup`, `System`, `WorldSpace`). 없으면 Category에서 추론:
  - `HUD` → `HUD`
  - `WorldSpace` → `WorldSpace`
  - `Scene` → `Scene`
  - 그 외 → `Popup`

## 클래스 이름 규칙

클래스 이름은 **`UI_<Layer>_<UIName>`** 으로 만든다. 이하 이 이름을 `<ClassName>`이라 부른다.

| Layer | 클래스 이름 | 예 |
| --- | --- | --- |
| `HUD` | `UI_HUD_<UIName>` | `UI_HUD_BossHealth` |
| `Scene` | `UI_Scene_<UIName>` | `UI_Scene_Inventory` |
| `Popup` | `UI_Popup_<UIName>` | `UI_Popup_Respawn` |
| `System` | `UI_System_<UIName>` | `UI_System_LoadingScreen` |

`Layer`가 `WorldSpace`면 이 커맨드를 쓰지 않는다. 월드 공간 UI는 `UI_Base` 화면이 아니라
`MonoBehaviour` 보조 클래스(`UIActorHpBar`처럼 언더스코어 없는 `UIXxx`)로 만든다.

## 실행 단계

### 1단계: 스크립트 파일 생성

`Assets/02.Scripts/UI/<Category>/<ClassName>.cs` 를 **Write 툴**로 생성한다.
`Layer`에 따라 상속 베이스가 달라진다.

- **`Layer`가 `Popup`이 아니면** 아래 기본 템플릿(`UI_Base` 상속)을 사용한다.

```csharp
using UnityEngine;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
    public class <ClassName> : UI_Base
    {
        #region UI_Base

        protected override void OnInit()
        {
        }

        protected override void OnShow()
        {
        }

        protected override void OnHide()
        {
        }

        protected override void OnClose()
        {
        }

        #endregion
    }
}
```

- **`Layer`가 `Popup`이면** 아래 팝업 템플릿(`UI_PopupBase` 상속)을 사용한다.
  `UI_PopupBase`는 Dim 페이드 인 + Panel 스케일 팝인/아웃 트윈(DOTween, `SetUpdate(true)`로
  일시정지 대응)을 내장한다.
  - `OnShow`에서 `base.OnShow()`를 호출하면 **오픈 트윈이 자동 재생**된다.
  - `UIManager.HideUI(...)`로 숨기면 `UI_PopupBase.Hide`가 **클로즈 트윈을 자동 재생한 뒤** 숨긴다.
    (직접 `Hide()`를 호출해도 동일. 별도로 트윈을 호출할 필요 없음.)
  - 트윈 사용 여부는 인스펙터의 `_playOpenTween` / `_playCloseTween`으로 켜고 끌 수 있으며 **기본값은 사용**이다.

```csharp
using UnityEngine;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
    public class <ClassName> : UI_PopupBase
    {
        #region UI_PopupBase

        protected override void OnInit()
        {
        }

        protected override void OnShow()
        {
            // Dim 페이드 인 + Panel 스케일 팝인 트윈이 재생된다.
            base.OnShow();
        }

        protected override void OnHide()
        {
            base.OnHide();
        }

        protected override void OnClose()
        {
        }

        #endregion
    }
}
```

### 2단계: 생성 완료 메시지 출력

다음 형식으로 안내 메시지를 출력한다:

---
**생성된 파일:**
- `Assets/02.Scripts/UI/<Category>/<ClassName>.cs`

**Unity 에디터에서 남은 작업 (수동):**

1. **프리팹 생성**
   - `Assets/03.Prefabs/UI/` 하위 적절한 폴더에 `<ClassName>.prefab` 생성 (프리팹 이름은 클래스 이름과 동일하게 맞춘다)
   - 프리팹 루트에 `Canvas` 컴포넌트 추가 (CanvasGroup은 UI_Base.Awake에서 자동 추가됨)
   - `<ClassName>` 스크립트 컴포넌트 추가
   - 필요 시 `Animator` 컴포넌트 추가 후 _animator 필드에 할당
   - **`Layer`가 `Popup`인 경우(`UI_PopupBase` 상속):** 루트 아래에 아래 두 오브젝트를 만들고
     스크립트의 `_dim` / `_panel` 필드에 각각 할당한다. (없으면 트윈은 자동으로 건너뛴다.)
     - `Dim` — 화면 전체를 덮는 반투명 `Image` + `CanvasGroup`. `_dim`에 CanvasGroup 할당.
     - `Panel` — 중앙 고정 UI 영역(전체 화면이 아님). 실제 콘텐츠는 이 아래에 배치. `_panel`에 RectTransform 할당.

2. **UIPrefabDatabase 등록**
   - `Assets/10.Datas/Path/UIPrefabDatabase.asset` 열기
   - Prefabs 리스트에 항목 추가:
     - Key: `<UIName>` (접두사 없는 순수 이름 그대로)
     - Prefab: 위에서 만든 프리팹
     - Default Layer: `<Layer>`

3. **UIManager에서 호출**
   ```csharp
   UIManager.Instance.ShowUI("<UIName>");
   ```
---

생성한 스크립트 파일 전체 내용을 보여준다.
