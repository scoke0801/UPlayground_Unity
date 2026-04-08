# UI 파일 생성

사용자가 제공한 인자(`$ARGUMENTS`)를 파싱하여 이 Unity 프로젝트에 UI 파일을 생성한다.

## 인자 파싱 규칙

`$ARGUMENTS` 형식: `<UIName> [<Category>] [<Layer>]`

- `UIName` — 필수. `UI_` 접두사 없이 입력 가능 (예: `HudBossHealth`, `UI_HudBossHealth` 둘 다 허용). 내부적으로 `UI_` 접두사를 붙여서 처리.
- `Category` — 선택. 스크립트 서브폴더. 없으면 UIName에서 추론:
  - 이름에 `Hud` 포함 → `HUD`
  - 이름에 `Inventory` 또는 `Item` 포함 → `Inventory`
  - 이름에 `Dialogue` 포함 → `Dialogue`
  - 이름에 `Interaction` 포함 → `Interaction`
  - 이름에 `World` 포함 → `WorldSpace`
  - 그 외 → `Scene`
- `Layer` — 선택. `CanvasLayer` 값 (`HUD`, `Scene`, `Popup`, `System`). 없으면 Category에서 추론:
  - `HUD` → `HUD`
  - `WorldSpace` → `WorldSpace`
  - `Scene` → `Scene`
  - 그 외 → `Popup`

## 실행 단계

### 1단계: 스크립트 파일 생성

아래 템플릿을 기반으로 `Assets/02.Scripts/UI/<Category>/UI_<UIName>.cs` 를 **Write 툴**로 생성한다.

```csharp
using UnityEngine;
using UPlayGround.Manager;

public class UI_<UIName> : UI_Base
{
    #region UI_Base

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
```

### 2단계: 생성 완료 메시지 출력

다음 형식으로 안내 메시지를 출력한다:

---
**생성된 파일:**
- `Assets/02.Scripts/UI/<Category>/UI_<UIName>.cs`

**Unity 에디터에서 남은 작업 (수동):**

1. **프리팹 생성**
   - `Assets/03.Prefabs/UI/` 하위 적절한 폴더에 `UI_<UIName>.prefab` 생성
   - 프리팹 루트에 `Canvas` 컴포넌트 추가 (CanvasGroup은 UI_Base.Awake에서 자동 추가됨)
   - `UI_<UIName>` 스크립트 컴포넌트 추가
   - 필요 시 `Animator` 컴포넌트 추가 후 _animator 필드에 할당

2. **UIPrefabDatabase 등록**
   - `Assets/10.Datas/Path/UIPrefabDatabase.asset` 열기
   - Prefabs 리스트에 항목 추가:
     - Key: `<UIName>` (UI_ 접두사 없는 이름 그대로)
     - Prefab: 위에서 만든 프리팹
     - Default Layer: `<Layer>`

3. **UIManager에서 호출**
   ```csharp
   UIManager.Instance.ShowUI("<UIName>");
   ```
---

생성한 스크립트 파일 전체 내용을 보여준다.
