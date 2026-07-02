# 제작 UI / 퀘스트 UI 에디터 작업 가이드

## 개요

이 문서는 제작 UI와 퀘스트 UI를 Unity 에디터에서 세팅할 때 무엇을 확인하고 어떤 순서로 작업해야 하는지 정리한다.

기준은 현재 코드와 데이터 구조다. 추측으로 새 API를 가정하지 않고, 확인된 클래스, 프리팹, ScriptableObject, Addressable 키만 사용한다.

현재 상태 요약:

| 구분 | 확인된 상태 |
|------|-------------|
| 제작 데이터 | `Assets/10.Datas/Craft/RecipeDatabase.asset` 존재 |
| 제작 DB 로드 | `RecipeManager`가 Addressable 키 `RecipeDatabase`로 로드 |
| 제작 UI 기능 코드 | `UI_Crafting`, `UI_CraftingRecipeSlot`, `UI_CraftingIngredientSlot` 구현됨 |
| 제작 메뉴 프리팹 | `UIPrefabDatabase`의 `Craft` 키가 `Assets/03.Prefabs/UI/Scene/Craft/UI_CraftMenu.prefab`을 가리킴 |
| 제작 메뉴 주의 | 현재 프리팹은 `UI_CraftMenu` 기반이며, 실제 제작 기능은 `UI_Crafting` 쪽에 있음 |
| 퀘스트 데이터 | `Assets/10.Datas/Quest/QuestDatabase.asset`과 여러 `QuestSO` 존재 |
| 퀘스트 DB 로드 | `QuestManager`가 Addressable 키 `QuestDatabase`로 로드 |
| 퀘스트 HUD | `UI_HudQuest`가 추적 중인 퀘스트 표시를 처리 |
| 퀘스트 메뉴 프리팹 | `UIPrefabDatabase`의 `Quest` 키가 `Assets/03.Prefabs/UI/Scene/Quest/UI_QuestMenu.prefab`을 가리킴 |
| 퀘스트 메뉴 주의 | `UI_QuestMenu`는 현재 추적/해제/토글 API만 있고 목록/상세 바인딩 로직은 없음 |

---

## 관련 파일

```text
Assets/02.Scripts/
├── Data/
│   ├── Crafting/
│   │   ├── IngredientData.cs
│   │   ├── RecipeData.cs
│   │   ├── RecipeIdType.cs
│   │   └── RecipeUnlockCondition.cs
│   ├── Path/
│   │   ├── RecipeDatabase.cs
│   │   ├── UIPrefabDatabase.cs
│   │   └── UIKeyType.cs
│   └── Quest/
│       ├── QuestDatabase.cs
│       ├── QuestEventData.cs
│       ├── QuestIdType.cs
│       ├── QuestObjectiveData.cs
│       ├── QuestObjectiveType.cs
│       ├── QuestRewardData.cs
│       ├── QuestRuntimeData.cs
│       ├── QuestSO.cs
│       └── QuestStatus.cs
├── Manager/
│   ├── Crafting/RecipeManager.cs
│   ├── Item/InventoryManager.cs
│   ├── Item/ItemManager.cs
│   ├── Quest/QuestManager.cs
│   └── UIManager.cs
└── UI/
    ├── HUD/
    │   ├── Menu/UI_MenuPanel.cs
    │   └── Quest/UI_HudQuest.cs
    └── Scene/
        ├── Crafting/
        │   ├── UI_Crafting.cs
        │   ├── UI_CraftingIngredientSlot.cs
        │   ├── UI_CraftingRecipeSlot.cs
        │   └── UI_CraftMenu.cs
        └── Quest/UI_QuestMenu.cs

Assets/03.Prefabs/UI/
├── HUD/Quest/UI_HudQuest.prefab
└── Scene/
    ├── Craft/UI_CraftMenu.prefab
    └── Quest/UI_QuestMenu.prefab

Assets/10.Datas/
├── Craft/RecipeDatabase.asset
├── Path/UIPrefabDatabase.asset
└── Quest/QuestDatabase.asset
```

---

## UI 열림 흐름

제작/퀘스트 메뉴는 `UI_MenuPanel`에서 열린다.

```csharp
private void OnClickedCraftButton()
{
    Toggle(UIKeyType.Craft);
}

private void OnClickedQuestButton()
{
    Toggle(UIKeyType.Quest);
}
```

`Toggle()`은 `UIManager.Instance.ShowUI(type)`을 호출한다. `UIKeyType.Craft`는 `"Craft"`, `UIKeyType.Quest`는 `"Quest"`로 변환된다.

현재 `UIPrefabDatabase.asset`의 관련 등록 상태:

| 키 | 프리팹 | 기본 레이어 |
|----|--------|-------------|
| `Craft` | `Assets/03.Prefabs/UI/Scene/Craft/UI_CraftMenu.prefab` | `Scene` |
| `Quest` | `Assets/03.Prefabs/UI/Scene/Quest/UI_QuestMenu.prefab` | `Scene` |
| `HudQuest` | `Assets/03.Prefabs/UI/HUD/Quest/UI_HudQuest.prefab` | `HUD` |

`UI_Base`는 `Canvas`, `CanvasGroup`, ESC 닫기, 커서 표시, 기본 버튼 클릭 사운드, 입력 레이어 상승/복원을 공통 처리한다. `UI_Crafting`, `UI_CraftMenu`, `UI_QuestMenu`는 `BlocksLowerInput`을 `true`로 오버라이드하므로 열려 있는 동안 하위 게임플레이 입력을 막는다.

---

## 제작 UI 구조

### 코드 의존 관계

```text
UI_MenuPanel
    │ Craft 버튼
    ▼
UIManager.ShowUI(UIKeyType.Craft)
    │
    ▼
UIPrefabDatabase["Craft"]
    │
    ▼
UI_CraftMenu.prefab

실제 제작 기능:
UI_Crafting
    ├── RecipeManager
    │   ├── RecipeDatabase
    │   ├── CanCraft()
    │   ├── TryStartCrafting()
    │   ├── CancelCrafting()
    │   └── OnCraftingStarted / OnCraftingCompleted / OnCraftingCancelled
    ├── ItemManager
    │   └── GetItemData()
    └── InventoryManager
        ├── GetItemCount()
        ├── RemoveItem()
        ├── RestoreItem()
        └── AddItem()
```

### 중요한 현재 상태

`UI_CraftMenu`는 실제 제작 목록/상세/제작 실행 UI가 아니다. 현재 `UI_CraftMenu.cs`는 `UI_Base` 생명주기, 입력 차단, 뒤로가기 닫기만 가진 얇은 메뉴 클래스다.

실제 제작 UI 기능은 `UI_Crafting.cs`에 있다. 따라서 에디터에서 제작 UI를 동작시키려면 다음 둘 중 하나를 선택해야 한다.

| 선택 | 작업 |
|------|------|
| 권장 | `UI_CraftMenu.prefab` 루트의 스크립트를 `UI_Crafting`으로 교체하고 필드를 모두 연결 |
| 대안 | 새 프리팹을 만들고 `UIPrefabDatabase`의 `Craft` 키 프리팹을 새 프리팹으로 교체 |

기존 메뉴 버튼, `UIKeyType.Craft`, `UIPrefabDatabase` 흐름을 유지하려면 첫 번째가 가장 단순하다.

---

## 제작 데이터 확인

먼저 아래 에셋을 연다.

```text
Assets/10.Datas/Craft/RecipeDatabase.asset
```

현재 확인된 데이터:

| recipeID | recipeName | resultItemID | resultQuantity | 재료 | 언락 조건 |
|----------|------------|--------------|----------------|------|-----------|
| 1 | 하급 생명 물약 | 206 | 1 | itemID `100005` 3개 | 없음 |
| 2 | 중급 생명 물약 | 0 | 1 | itemID `100005` 3개 | 없음 |
| 3 | 고급 생명 물약 | 0 | 1 | itemID `100005` 5개 | 없음 |

`RecipeManager.CanCraft()`는 내부에서 결과 아이템이 유효한지 검사한다.

```csharp
private bool HasValidResult(RecipeData recipe)
{
    if (recipe == null) return false;
    if (recipe.resultItemID <= 0) return false;
    if (recipe.resultQuantity <= 0) return false;
    return ItemManager.Instance.GetItemData(recipe.resultItemID) != null;
}
```

그래서 `resultItemID`가 `0`인 중급/고급 생명 물약은 현재 제작 가능 판정에서 실패한다. UI 테스트용 레시피는 `resultItemID`를 실제 `ItemDatabase`에 있는 `ItemSO.itemId`로 바꿔야 한다.

---

## 제작 UI 프리팹 세팅

### `UI_Crafting` 필수 Inspector 참조

`UI_Crafting`을 루트에 붙이면 아래 필드를 연결해야 한다.

| Header | 필드 | 연결 대상 |
|--------|------|-----------|
| 레시피 리스트 | `_recipeListContent` | 레시피 슬롯 생성용 ScrollView Content |
| 레시피 리스트 | `_recipeSlotPrefab` | `UI_CraftingRecipeSlot` 프리팹 |
| 카테고리 탭 | `_tabAll` | 전체 탭 Button |
| 카테고리 탭 | `_tabConsumable` | 소비 탭 Button |
| 카테고리 탭 | `_tabEquipment` | 장비 탭 Button |
| 카테고리 탭 | `_tabMaterial` | 재료 탭 Button |
| 카테고리 탭 | `_tabSpecial` | 특수 탭 Button |
| 레시피 상세 | `_detailPanel` | 선택 전 숨길 상세 패널 |
| 레시피 상세 | `_imgResultIcon` | 결과 아이콘 Image |
| 레시피 상세 | `_txtResultName` | 결과 이름 TextMeshProUGUI |
| 레시피 상세 | `_txtDescription` | 설명 TextMeshProUGUI |
| 레시피 상세 | `_ingredientContent` | 재료 슬롯 생성용 Content |
| 레시피 상세 | `_ingredientSlotPrefab` | `UI_CraftingIngredientSlot` 프리팹 |
| 레시피 상세 | `_txtCost` | 비용 텍스트 |
| 레시피 상세 | `_txtCastTime` | 제작 시간 텍스트 |
| 제작 조작 | `_btnCraft` | 제작/취소 Button |
| 제작 조작 | `_txtCraftButton` | 제작 버튼 텍스트 |
| 제작 조작 | `_btnQtyMinus` | 수량 감소 Button |
| 제작 조작 | `_btnQtyPlus` | 수량 증가 Button |
| 제작 조작 | `_txtQty` | 현재 수량 텍스트 |
| 제작 조작 | `_imgProgressBar` | 제작 진행 바 Image |
| 제작 조작 | `_txtCraftStatus` | 제작 상태 텍스트 |
| 제작 조작 | `_btnClose` | 닫기 Button |

### 권장 프리팹 계층

```text
UI_CraftMenu
├── Header
│   ├── TitleText
│   └── CloseButton
├── Body
│   ├── RecipePanel
│   │   ├── CategoryTabs
│   │   │   ├── TabAll
│   │   │   ├── TabConsumable
│   │   │   ├── TabEquipment
│   │   │   ├── TabMaterial
│   │   │   └── TabSpecial
│   │   └── RecipeScrollView
│   │       └── Viewport
│   │           └── RecipeListContent
│   └── DetailPanel
│       ├── ResultIcon
│       ├── ResultNameText
│       ├── DescriptionText
│       ├── IngredientScrollView
│       │   └── Viewport
│       │       └── IngredientContent
│       ├── CostText
│       └── CastTimeText
└── Footer
    ├── QtyMinusButton
    ├── QuantityText
    ├── QtyPlusButton
    ├── ProgressBar
    ├── CraftStatusText
    └── CraftButton
        └── CraftButtonText
```

### 진행 바 설정

`UI_Crafting.Update()`는 제작 중일 때 `Image.fillAmount`를 갱신한다.

```csharp
_imgProgressBar.fillAmount = RecipeManager.Instance.GetCraftingProgress();
```

따라서 `_imgProgressBar`에 연결할 Image는 Inspector에서 다음처럼 설정한다.

| 항목 | 값 |
|------|----|
| Image Type | `Filled` |
| Fill Method | `Horizontal` |
| Fill Origin | `Left` |
| Fill Amount | `0` |

`Simple` Image를 연결하면 코드가 실행되어도 진행 바가 차오르는 화면을 볼 수 없다.

### 레시피 슬롯 프리팹

권장 위치:

```text
Assets/03.Prefabs/UI/Scene/Craft/UI_CraftingRecipeSlot.prefab
```

권장 구조:

```text
UI_CraftingRecipeSlot
├── BackgroundImage
├── ResultIcon
├── RecipeNameText
├── CraftableIndicator
└── SelectOverlay
```

필드 연결:

| 필드 | 연결 |
|------|------|
| `_imgResultIcon` | `ResultIcon` Image |
| `_txtRecipeName` | `RecipeNameText` TextMeshProUGUI |
| `_imgCraftable` | `CraftableIndicator` Image |
| `_selectOverlay` | `SelectOverlay` GameObject |

루트 또는 자식 그래픽에는 Raycast Target이 켜져 있어야 `IPointerClickHandler`가 클릭을 받을 수 있다.

### 재료 슬롯 프리팹

권장 위치:

```text
Assets/03.Prefabs/UI/Scene/Craft/UI_CraftingIngredientSlot.prefab
```

권장 구조:

```text
UI_CraftingIngredientSlot
├── CountBackground
├── ItemIcon
├── ItemNameText
└── CountText
```

필드 연결:

| 필드 | 연결 |
|------|------|
| `_imgIcon` | `ItemIcon` Image |
| `_txtName` | `ItemNameText` TextMeshProUGUI |
| `_txtCount` | `CountText` TextMeshProUGUI |
| `_imgCountBg` | `CountBackground` Image |

---

## 제작 UI 작업 순서

1. `Assets/10.Datas/Craft/RecipeDatabase.asset`을 연다.
2. 테스트할 레시피의 `resultItemID`가 실제 `ItemDatabase`에 있는지 확인한다.
3. 테스트할 레시피는 `isDebugUnlocked`를 켜거나 조건 없이 언락되게 둔다.
4. `Assets/03.Prefabs/UI/Scene/Craft/UI_CraftMenu.prefab`을 연다.
5. 루트 UI 스크립트를 `UI_Crafting` 기준으로 정리한다.
6. 레시피 슬롯 프리팹을 만든다.
7. 재료 슬롯 프리팹을 만든다.
8. `UI_Crafting`의 Inspector 참조를 모두 연결한다.
9. `_imgProgressBar` Image Type을 `Filled`로 바꾼다.
10. `Assets/10.Datas/Path/UIPrefabDatabase.asset`에서 `Craft` 키가 작업한 프리팹을 가리키는지 확인한다.
11. Play Mode에서 메뉴 패널의 제작 버튼으로 연다.

---

## 퀘스트 UI 구조

### 코드 의존 관계

```text
UI_MenuPanel
    │ Quest 버튼
    ▼
UIManager.ShowUI(UIKeyType.Quest)
    │
    ▼
UIPrefabDatabase["Quest"]
    │
    ▼
UI_QuestMenu.prefab
    │
    └── UI_QuestMenu
        ├── TrackQuest(string questId)
        ├── UntrackQuest()
        └── ToggleTrackQuest(string questId)

QuestManager
    ├── QuestDatabase
    ├── GetAvailableQuests()
    ├── GetActiveQuests()
    ├── GetQuestStatus()
    ├── TrackQuest()
    ├── UntrackQuest()
    └── QuestEvent 발송

UI_HudQuest
    ├── QuestAccepted
    ├── QuestCompleted
    ├── QuestFailed
    ├── QuestTracked
    ├── QuestUntracked
    └── QuestObjectiveUpdated
```

### 중요한 현재 상태

`UI_QuestMenu`는 현재 퀘스트 목록과 상세를 표시하지 않는다. Inspector 필드도 없고, `QuestManager.GetActiveQuests()` 또는 `QuestManager.GetAvailableQuests()`를 읽어 슬롯을 생성하는 코드도 없다.

현재 `UI_QuestMenu`에서 가능한 것은 추적 대상 지정/해제뿐이다.

```csharp
public void TrackQuest(string questId)
public void UntrackQuest()
public void ToggleTrackQuest(string questId)
```

따라서 에디터에서 `UI_QuestMenu.prefab`을 꾸미는 것만으로는 실제 퀘스트 목록 UI가 완성되지 않는다. 에디터에서는 먼저 구조를 잡고, 이후 `UI_QuestMenu` 확장 스크립트에서 해당 구조를 연결해야 한다.

---

## 퀘스트 데이터 확인

먼저 아래 에셋을 연다.

```text
Assets/10.Datas/Quest/QuestDatabase.asset
```

예시로 `Assets/10.Datas/Quest/Generated/MainStory/quest_main_001.asset`은 다음 구조다.

| 필드 | 값 |
|------|----|
| `questId` | `quest_main_001` |
| `questName` | `끊긴 길` |
| `requiredStoryProgress` | `0` |
| 목표 타입 | `ReachLocation` |
| 목표 위치 ID | `loc_central_lake` |
| 보상 골드 | `100` |
| 보상 경험치 | `60` |
| `autoComplete` | true |

`QuestSO`의 주요 필드:

| 필드 | 의미 |
|------|------|
| `questId` | 전체 DB에서 유일해야 하는 문자열 ID |
| `questName` | 표시 이름 |
| `questDescription` | 설명 |
| `requiredQuestIds` | 수락 전 완료되어야 하는 퀘스트 ID 목록 |
| `requiredStoryProgress` | 수락 전 필요한 스토리 진행도 |
| `autoAcceptNextQuestIds` | 완료 직후 자동 수락할 후속 퀘스트 ID 목록 |
| `objectives` | 목표 목록 |
| `reward` | 보상 |
| `isRepeatable` | 반복 가능 여부 |
| `autoComplete` | 모든 목표 달성 시 자동 완료 여부 |

목표 타입별 갱신 API:

| 목표 타입 | QuestManager 호출 |
|----------|-------------------|
| `ItemCollect` | `NotifyItemCollected(itemId, count)` |
| `ItemDeliver` | `NotifyItemDelivered(npcId, itemId, count)` |
| `ItemUse` | `NotifyItemUsed(itemId, count)` |
| `MonsterKill` | `NotifyMonsterKill(actorId)` 또는 `NotifyMonsterKill(monsterId)` |
| `StoryProgress` | `NotifyStoryProgress(progress)` |
| `ItemCraft` | `NotifyItemCrafted(recipeId, quantity)` |
| `ItemEnhance` | `NotifyItemEnhanced(itemId)` |
| `ReachLocation` | `NotifyLocationReached(locationId)` |

---

## 퀘스트 메뉴 프리팹 세팅

### 권장 계층

```text
UI_QuestMenu
├── Header
│   ├── TitleText
│   └── CloseButton
├── QuestListPanel
│   ├── FilterTabs
│   │   ├── TabAvailable
│   │   ├── TabActive
│   │   ├── TabCompleted
│   │   └── TabFailed
│   └── QuestScrollView
│       └── Viewport
│           └── QuestListContent
└── DetailPanel
    ├── QuestTitleText
    ├── QuestStatusText
    ├── QuestDescriptionText
    ├── ObjectiveListContent
    ├── RewardListContent
    ├── TrackButton
    │   └── TrackButtonText
    ├── CompleteButton
    │   └── CompleteButtonText
    └── AbandonButton
        └── AbandonButtonText
```

이 이름들은 현재 코드가 자동 검색하지 않는다. 하지만 이후 `UI_QuestMenu`에 `[SerializeField]` 필드를 추가할 때 바로 연결하기 쉬운 기준 이름이다.

### 퀘스트 목록 슬롯 프리팹

현재 슬롯 스크립트는 없으므로 우선 UI 프리팹 껍데기를 만든다.

권장 위치:

```text
Assets/03.Prefabs/UI/Scene/Quest/UI_QuestListSlot.prefab
```

권장 구조:

```text
UI_QuestListSlot
├── BackgroundImage
├── StatusIcon
├── QuestNameText
├── ObjectiveSummaryText
├── TrackedIcon
└── SelectOverlay
```

나중에 필요한 데이터:

| UI | 데이터 |
|----|--------|
| `QuestNameText` | `QuestSO.questName` |
| `ObjectiveSummaryText` | 첫 번째 `QuestObjectiveData.description` 또는 현재 선택한 요약 규칙 |
| `StatusIcon` | `QuestStatus` |
| `TrackedIcon` | `QuestManager.IsQuestTracked(questId)` |

### 목표 슬롯 프리팹

권장 위치:

```text
Assets/03.Prefabs/UI/Scene/Quest/UI_QuestObjectiveSlot.prefab
```

권장 구조:

```text
UI_QuestObjectiveSlot
├── CompleteCheckImage
├── ObjectiveText
└── ProgressText
```

나중에 필요한 데이터:

| UI | 데이터 |
|----|--------|
| `ObjectiveText` | `QuestObjectiveData.description` |
| `ProgressText` | `QuestRuntimeData.ObjectiveProgress[objectiveId] / requiredCount` |
| `CompleteCheckImage` | `QuestRuntimeData.IsObjectiveComplete(obj)` |

### 보상 슬롯 프리팹

권장 위치:

```text
Assets/03.Prefabs/UI/Scene/Quest/UI_QuestRewardSlot.prefab
```

권장 구조:

```text
UI_QuestRewardSlot
├── RewardIcon
├── RewardNameText
└── RewardCountText
```

보상 데이터:

| 보상 | 데이터 |
|------|--------|
| 골드 | `QuestRewardData.gold` |
| 경험치 | `QuestRewardData.exp` |
| 아이템 | `QuestRewardData.items` |

아이템 보상의 이름과 아이콘은 `ItemManager.Instance.GetItemData(itemId)`로 조회해야 한다.

---

## 퀘스트 HUD 확인

퀘스트 전체 메뉴와 별개로 HUD 표시는 이미 구현되어 있다.

프리팹:

```text
Assets/03.Prefabs/UI/HUD/Quest/UI_HudQuest.prefab
```

필요 참조:

| 필드 | 연결 |
|------|------|
| `_questTitleText` | 현재 표시할 퀘스트 제목 |
| `_questDescText` | 목표 설명과 진행도 |
| `_questCompletePanel` | 완료 알림 패널 |
| `_questCompleteCanvasGroup` | 완료 알림 CanvasGroup |
| `_questCompleteTitleText` | 완료 알림 제목 |
| `_questCompleteNameText` | 완료된 퀘스트 이름 |

`UI_HudQuest`는 일부 텍스트 참조가 비어 있으면 자식 중 이름이 `QuestTitleText`, `QuestDescText`, `QuestCompleteTitleText`, `QuestCompleteNameText`인 TextMeshProUGUI를 자동 검색한다. 그래도 프리팹에서는 명시 연결하는 편이 안전하다.

HUD 표시 규칙:

- 수동 추적 해제 상태면 표시하지 않음
- 추적 중인 퀘스트가 있으면 해당 퀘스트 표시
- 추적 퀘스트가 없으면 활성 메인 퀘스트 중 ID가 빠른 것을 표시
- 메인 퀘스트 기본 접두사는 `quest_main_`
- 레거시 접두사 `main_`도 허용

---

## 퀘스트 UI 작업 순서

1. `Assets/10.Datas/Quest/QuestDatabase.asset`을 연다.
2. 표시할 퀘스트가 DB 리스트에 들어 있는지 확인한다.
3. 테스트할 퀘스트의 `questId`, `questName`, `objectives`, `reward`를 확인한다.
4. `Assets/03.Prefabs/UI/Scene/Quest/UI_QuestMenu.prefab`을 연다.
5. `QuestListContent`, `DetailPanel`, `ObjectiveListContent`, `RewardListContent` 기준으로 계층을 정리한다.
6. `UI_QuestListSlot.prefab` 껍데기를 만든다.
7. `UI_QuestObjectiveSlot.prefab` 껍데기를 만든다.
8. `UI_QuestRewardSlot.prefab` 껍데기를 만든다.
9. `TrackButton`, `CompleteButton`, `AbandonButton` 위치를 확정한다.
10. `UIPrefabDatabase.asset`에서 `Quest` 키가 작업한 프리팹을 가리키는지 확인한다.
11. 이후 `UI_QuestMenu`에 목록/상세 바인딩 코드를 추가한다.

---

## Play Mode 검증 체크리스트

### 제작

| 확인 | 기대 결과 |
|------|-----------|
| 메뉴 패널에서 제작 버튼 클릭 | `Craft` UI가 열린다 |
| `RecipeDatabase` 로드 | 콘솔에 로드 완료 로그가 나온다 |
| 레시피 목록 | 언락된 레시피만 표시된다 |
| 카테고리 탭 | 전체/소비/장비/재료/특수 필터가 적용된다 |
| 레시피 클릭 | 상세 패널이 켜진다 |
| 재료 부족 | 제작 버튼이 비활성화되고 `재료 부족` 텍스트가 표시된다 |
| 재료 충분 | 제작 버튼이 활성화되고 `제작` 텍스트가 표시된다 |
| 제작 중 | 버튼 텍스트가 `취소`가 되고 진행 바가 찬다 |
| 제작 완료 | 결과 아이템이 인벤토리에 추가되고 `제작 완료!`가 표시된다 |
| 제작 취소 | 진행 상태가 초기화되고 `취소됨`이 표시된다 |

### 퀘스트

| 확인 | 기대 결과 |
|------|-----------|
| 메뉴 패널에서 퀘스트 버튼 클릭 | `Quest` UI가 열린다 |
| `QuestDatabase` 로드 | 콘솔에 로드 완료 로그가 나온다 |
| 퀘스트 수락 | `QuestAccepted` 이벤트가 발송된다 |
| HUD 추적 | `UI_HudQuest`에 추적 중 퀘스트가 표시된다 |
| 목표 진행 | `QuestObjectiveUpdated` 이벤트 후 HUD 텍스트가 갱신된다 |
| 목표 완료 | `autoComplete`가 true면 자동 완료된다 |
| 퀘스트 완료 | 보상이 지급되고 완료 알림이 표시된다 |
| 추적 해제 | HUD 퀘스트 표시가 사라진다 |

---

## 자주 막히는 지점

### 제작 목록이 비어 있음

확인할 것:

- `RecipeManager.IsDBLoaded`가 true인지
- Addressable 키 `RecipeDatabase`가 올바른지
- `Craft` 키로 열리는 프리팹에 `UI_Crafting`이 붙어 있는지
- `_recipeListContent`, `_recipeSlotPrefab`이 연결되어 있는지
- 레시피가 언락 상태인지

### 제작 버튼이 항상 비활성화됨

확인할 것:

- `resultItemID`가 0이 아닌지
- `resultItemID`가 `ItemDatabase`에 존재하는지
- `resultQuantity`가 1 이상인지
- 재료 아이템 ID가 `ItemDatabase`에 존재하는지
- 인벤토리에 재료 수량이 충분한지
- 현재 다른 제작이 진행 중인지

### 진행 바가 안 움직임

확인할 것:

- `_imgProgressBar`가 연결되어 있는지
- Image Type이 `Filled`인지
- `RecipeManager.Instance.IsCrafting()`이 true가 되는지
- `RecipeManager.OnUpdate()`가 GameManager 생명주기로 호출되는지

### 퀘스트 메뉴에 데이터가 안 뜸

현재 구현 기준으로는 정상이다. `UI_QuestMenu`에는 목록/상세 바인딩 코드가 아직 없다.

### HUD 퀘스트가 안 뜸

확인할 것:

- `HudQuest` 키가 `UIPrefabDatabase`에 등록되어 있는지
- `UI_HudQuest`가 실제로 Show 되었는지
- 진행 중인 퀘스트가 있는지
- `QuestManager.IsQuestTrackingSuppressed`가 true인지
- 활성 퀘스트 ID가 `quest_main_` 또는 `main_`으로 시작하는지
- 텍스트 참조가 연결되어 있거나 자식 이름이 `QuestTitleText`, `QuestDescText`인지

---

## 결론

지금 에디터에서 먼저 할 일은 제작 UI를 `UI_Crafting` 기준으로 실제 연결하는 것이다. 제작 시스템은 데이터, 매니저, UI 스크립트가 이미 갖춰져 있으므로 프리팹 참조와 테스트 데이터만 맞추면 Play Mode 검증이 가능하다.

퀘스트 UI는 데이터와 런타임 매니저, HUD 표시가 준비되어 있지만 전체 퀘스트 메뉴는 아직 목록/상세 바인딩 코드가 없다. 따라서 에디터에서는 `UI_QuestMenu.prefab`의 구조와 슬롯 프리팹을 먼저 정리하고, 실제 데이터 표시는 이후 `UI_QuestMenu` 확장 작업으로 처리해야 한다.
