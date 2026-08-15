# 제작(Crafting) 시스템 가이드

## 개요

UPlayground의 제작 시스템은 **레시피 기반의 아이템 제작 메커니즘**입니다. 플레이어는 필요한 재료를 수집하여 새로운 아이템을 만들고, 특정 조건을 만족하면 새로운 레시피가 자동으로 언락됩니다.

### 핵심 특징

- **ScriptableObject 기반**: 모든 레시피/재료 데이터는 외부화되어 균형 조정이 용이
- **언락 시스템**: 몬스터 처치, 아이템 수집, 다른 레시피 제작 완료 등의 조건으로 레시피 자동 언락
- **비용 시스템**: 무료(Free) 또는 골드(Gold) 소모 방식 지원
- **카테고리 필터**: 소비, 장비, 재료, 특수 4가지 카테고리로 분류
- **시간 기반 제작**: 설정 가능한 제작 시간(castTime) 동안 진행 바 표시
- **수량 제작**: 1회 이상 원하는 만큼 제작 가능 (재료 × 수량)

---

## 아키텍처

```
┌─────────────────────────────────────────┐
│         GameManager (싱글톤)              │
└──────────────┬──────────────────────────┘
               │
        ┌──────▼──────┐
        │ RecipeManager │ (IManager, BaseManager<T>)
        └──────┬──────┘
               │
    ┌──────────┼──────────┐
    │          │          │
    ▼          ▼          ▼
RecipeDB   Inventory   Events
(SO)       Manager     (UI 연동)
```

### 계층 구조

```
데이터 계층 (Assets/02.Scripts/Data/Crafting/)
├── RecipeData              : 레시피 정보 (기본 정보, 결과, 비용, 제작 시간)
├── IngredientData          : 재료 정보 (어느 레시피에 속하는지, 필요 수량)
├── RecipeUnlockCondition   : 레시피 언락 조건 (조건 타입, 값들)
└── RecipeDatabase (SO)     : 모든 데이터 통합 관리 (Addressables 키: "RecipeDatabase")

매니저 계층 (Assets/02.Scripts/Manager/Crafting/)
└── RecipeManager           : 핵심 게임 로직 (제작, 언락, 이벤트)
    ├── 제작 판정 (CanCraft, HasEnoughIngredients 등)
    ├── 제작 실행 (TryStartCrafting → TickCrafting → FinishCrafting)
    ├── 언락 체크 (CheckUnlockConditions, EvaluateCondition)
    └── 외부 이벤트 (NotifyMonsterKill)

UI 계층 (Assets/02.Scripts/UI/Crafting/)
├── UI_Crafting             : 메인 UI (패널, 탭, 리스트, 상세, 진행 바)
├── UICraftingRecipeSlot   : 레시피 슬롯 (아이콘, 이름, 제작 가능 인디케이터)
└── UICraftingIngredientSlot: 재료 슬롯 (아이콘, 이름, 보유/필요 수량)

데이터 임포트 (Assets/02.Scripts/Data/Crafting/Editor/)
└── RecipeDataImporter     : CSV → RecipeDatabase 변환 (에디터 윈도우)
```

---

## 데이터 정의

### RecipeData (레시피 정보)

```csharp
[Serializable]
public class RecipeData
{
    // 기본 정보
    public int recipeID;              // 고유 ID (1부터)
    public string recipeName;         // 레시피 이름 (예: "철 검")
    public string description;        // 설명

    // 결과물
    public int resultItemID;          // 제작 결과 아이템 ID
    public int resultQuantity = 1;    // 1회 제작 시 획득 수량

    // 비용
    public CostType costType = CostType.Gold;  // Free 또는 Gold
    public int costAmount = 0;                 // 소모 비용 (costType가 Gold일 때)

    // 제작 설정
    public float castTimeSeconds = 2f;  // 제작 소요 시간 (초)
    public CraftingCategory category;   // 카테고리 (Consumable, Equipment, Material, Special)

    // 디버그
    public bool isDebugUnlocked = false;  // true면 조건 무시하고 처음부터 언락
}

public enum CostType { Free = 0, Gold = 1 }

public enum CraftingCategory
{
    Consumable = 0,  // 포션, 음식
    Equipment  = 1,  // 무기, 방어구
    Material   = 2,  // 강화 부품
    Special    = 3,  // 특수 아이템
}
```

### IngredientData (재료 정보)

```csharp
[Serializable]
public class IngredientData
{
    public int recipeID;              // 어느 레시피의 재료인지
    public int ingredientItemID;      // 필요한 아이템 ID (ItemSO.itemId)
    public int requiredQuantity = 1;  // 1회 제작에 필요한 수량
}
```

**예시**: 철 검 레시피(ID=10)에 철 광석(itemID=101) 5개 필요
```
IngredientData
  recipeID: 10
  ingredientItemID: 101
  requiredQuantity: 5
```

### RecipeUnlockCondition (언락 조건)

```csharp
[Serializable]
public class RecipeUnlockCondition
{
    public int recipeID;              // 어느 레시피의 조건인지
    public UnlockConditionType conditionType;  // 조건 타입
    public int conditionValue;        // 조건값 1 (몬스터ID, 아이템ID 등)
    public int conditionValue2;       // 조건값 2 (수량, 횟수 등)
}

public enum UnlockConditionType
{
    None        = 0,  // 조건 없음 (처음부터 언락)
    MonsterKill = 1,  // 특정 몬스터 처치 (value=몬스터ID, value2=처치 수)
    ItemCollect = 2,  // 특정 아이템 수집 (value=아이템ID, value2=수량)
    ItemHave    = 3,  // 특정 아이템 소지 (value=아이템ID, value2=수량)
    RecipeCraft = 4,  // 다른 레시피 제작 (value=레시피ID, value2=횟수)
}
```

**예시**: 철 검(ID=10)을 만들려면 드래곤(ID=5) 처치 3회 필요
```
RecipeUnlockCondition
  recipeID: 10
  conditionType: MonsterKill
  conditionValue: 5 (드래곤 ID)
  conditionValue2: 3 (처치 수)
```

### RecipeDatabase (ScriptableObject)

모든 레시피/재료/조건을 한 곳에서 관리:

```csharp
[CreateAssetMenu(fileName = "RecipeDatabase", menuName = "UPlayGround/PathDatabase/Recipe")]
public class RecipeDatabase : ScriptableObject
{
    [SerializeField] private List<RecipeData> recipes;
    [SerializeField] private List<IngredientData> ingredients;
    [SerializeField] private List<RecipeUnlockCondition> unlockConditions;

    // 조회 메서드
    public RecipeData GetRecipe(int recipeID);
    public List<IngredientData> GetIngredients(int recipeID);
    public RecipeUnlockCondition GetUnlockCondition(int recipeID);
    public List<int> GetAllRecipeIDs();
}
```

**생성 방법**: Unity Editor → UPlayGround / PathDatabase / Recipe

---

## RecipeManager (핵심 매니저)

### 생명주기

1. **Init()**: `LoadDatabaseAsync()` 호출 → Addressables에서 "RecipeDatabase" 로드
2. **AfterInit()**: (사용 안 함)
3. **OnUpdate()**: 제작 진행 중이면 `TickCrafting()` 호출 (deltaTime 누적)
4. **OnSceneChanged()**: (사용 안 함)
5. **Dispose()**: (사용 안 함)

### 이벤트

```csharp
public event Action<int> OnRecipeUnlocked;           // 레시피 언락 시
public event Action<int> OnCraftingStarted;          // 제작 시작 시 (recipeID)
public event Action<int, int> OnCraftingCompleted;   // 제작 완료 시 (recipeID, 획득수량)
public event Action OnCraftingCancelled;             // 제작 취소 시
```

### 주요 메서드

#### 제작 판정

```csharp
/// 제작 가능 여부 (UI 버튼 활성화/비활성화에 사용)
public bool CanCraft(int recipeID, int quantity = 1)
{
    // 체크 항목:
    // 1. DB 로드 여부
    // 2. 레시피 존재 여부
    // 3. 레시피 언락 여부
    // 4. 현재 제작 중인지 여부
    // 5. 비용 충분한지 여부 (InventoryManager)
    // 6. 재료 충분한지 여부 (InventoryManager)
    return true/false;
}

/// 부족한 재료 목록 (UI에서 빨간 표시 시 사용)
public List<int> GetMissingIngredients(int recipeID, int quantity = 1)
{
    // 필요 수량 >= 보유 수량이 아닌 재료 itemID 반환
    return missingItemIDs;
}

/// 재료별 충족 여부 (Dictionary<itemID, 충족여부>)
public Dictionary<int, bool> GetIngredientAvailability(int recipeID, int quantity = 1)
```

#### 제작 실행

```csharp
/// 제작 시작 (성공 시 true, 실패 시 false)
public bool TryStartCrafting(int recipeID, int quantity = 1)
{
    // 1. CanCraft() 확인
    // 2. DeductResources() → 재료 및 비용 차감 (실패 시 롤백)
    // 3. _castTimeRemaining = recipe.castTimeSeconds * quantity
    // 4. OnCraftingStarted 이벤트 발생
    return success;
}

/// 제작 취소 (재료는 환불 안 됨 — 기획 결정)
public void CancelCrafting()
{
    // 진행 상태 초기화
    OnCraftingCancelled?.Invoke();
}

/// 내부: 매 프레임 호출 (OnUpdate에서)
private void TickCrafting(float deltaTime)
{
    _castTimeRemaining -= deltaTime;
    _craftingProgress = 1f - (_castTimeRemaining / _totalCastTime);
    
    if (_castTimeRemaining <= 0f)
        FinishCrafting();
}

/// 내부: 제작 완료 처리
private void FinishCrafting()
{
    // 1. 결과 아이템 인벤토리에 추가
    // 2. _craftCounts[recipeID]++ 제작 횟수 기록
    // 3. CheckUnlockConditions() 새 레시피 언락 체크
    // 4. OnCraftingCompleted 이벤트 발생
}
```

#### 언락 시스템

```csharp
/// 레시피가 언락되었는지 확인
public bool IsRecipeUnlocked(int recipeID)
{
    // isDebugUnlocked=true면 항상 true
    // 아니면 _unlocked 딕셔너리에서 조회
}

/// 레시피 직접 언락 (스토리 이벤트, 치트 등)
public void UnlockRecipe(int recipeID)
{
    _unlocked[recipeID] = true;
    OnRecipeUnlocked?.Invoke(recipeID);
}

/// 모든 레시피의 언락 조건 재평가
public void CheckUnlockConditions()
{
    // 각 미언락 레시피마다 EvaluateCondition() 호출
    // 조건 만족 시 UnlockRecipe() 호출
    // 몬스터 처치/아이템 수집 후, 제작 완료 후 자동 호출
}

/// 내부: 조건 평가 (switch문으로 조건 타입별 처리)
private bool EvaluateCondition(RecipeUnlockCondition cond)
{
    return cond.conditionType switch
    {
        UnlockConditionType.None =>
            true,  // 항상 만족
        
        UnlockConditionType.MonsterKill =>
            GetMonsterKillCount(cond.conditionValue) >= Mathf.Max(1, cond.conditionValue2),
        
        UnlockConditionType.ItemCollect =>
            InventoryManager.Instance.GetItemCount(cond.conditionValue) >= cond.conditionValue2,
        
        UnlockConditionType.ItemHave =>
            InventoryManager.Instance.GetItemCount(cond.conditionValue) >= cond.conditionValue2,
        
        UnlockConditionType.RecipeCraft =>
            _craftCounts.TryGetValue(cond.conditionValue, out var cnt) && cnt >= cond.conditionValue2,
        
        _ => false
    };
}
```

#### 외부 이벤트 수신

```csharp
/// 몬스터 처치 알림 (EnemyCombat 또는 EnemyDeathState에서 호출)
public void NotifyMonsterKill(int monsterID)
{
    _monsterKills[monsterID]++;
    CheckUnlockConditions();  // 언락 조건 재평가
}

/// 몬스터 처치 횟수 조회
private int GetMonsterKillCount(int monsterID)
```

#### 정보 조회 (UI용)

```csharp
public RecipeData GetRecipeData(int recipeID);
public List<IngredientData> GetIngredients(int recipeID);
public float GetCraftingProgress();              // 0~1
public int GetCurrentCraftingRecipeID();
public bool IsCrafting();
public List<int> GetUnlockedRecipeIDs();
public int GetCraftingCount(int recipeID);      // 해당 레시피 제작 횟수
public Dictionary<int, bool> GetIngredientAvailability(recipeID, quantity);
```

---

## UI 구성

### UI_Crafting (메인 UI)

**레이아웃**:
```
┌─────────────────────────────────┐
│  [All] [소비] [장비] [재료] [특수]  │  ← 카테고리 탭
├──────────────┬──────────────────┤
│              │                  │
│ 레시피 리스트  │   레시피 상세    │
│ (ScrollView) │                  │
│              │  ├─ 결과물 아이콘 │
│              │  ├─ 설명          │
│              │  ├─ 재료 목록      │
│              │  ├─ 비용 / 시간    │
│              │                  │
├──────────────┴──────────────────┤
│ - / 수량 / +                     │
│                                  │
│ [제작] 또는 [취소]                 │
│ ▓▓▓▓▓▓░░░░░░░░░░░░ (진행 바)       │
│ 제작 완료!                         │
└──────────────────────────────────┘
```

**이벤트 구독/해제**:
```csharp
protected override void OnShow()
{
    RecipeManager.Instance.OnRecipeUnlocked    += OnRecipeUnlocked;
    RecipeManager.Instance.OnCraftingStarted   += OnCraftingStarted;
    RecipeManager.Instance.OnCraftingCompleted += OnCraftingCompleted;
    RecipeManager.Instance.OnCraftingCancelled += OnCraftingCancelled;
    // ...
}

protected override void OnHide()
{
    // 구독 해제
    // ...
}
```

**수량 선택**:
- `-` 버튼: `_quantity = Mathf.Max(1, _quantity - 1)`
- `+` 버튼: `_quantity++`
- 재료 필요량이 `requiredQuantity * _quantity`로 계산됨

**제작 버튼 상태**:
- 레시피 미선택: 비활성화 ("레시피 선택")
- 제작 중: 활성화 ("취소")
- 제작 가능: 활성화 ("제작")
- 재료 부족: 비활성화 ("재료 부족")

**진행 바**:
```csharp
protected override void Update()
{
    if (RecipeManager.Instance.IsCrafting())
        _imgProgressBar.fillAmount = RecipeManager.Instance.GetCraftingProgress();
}
```

### UICraftingRecipeSlot (레시피 슬롯)

**용도**: 왼쪽 패널의 레시피 리스트에 인스턴스화

**표시 내용**:
- 결과물 아이콘 (`_imgResultIcon`)
- 레시피 이름 (`_txtRecipeName`)
- 제작 가능 인디케이터 (`_imgCraftable`) — 초록색(가능) / 회색(불가)
- 선택 오버레이 (`_selectOverlay`) — 선택 시 하이라이트

**핵심 메서드**:
```csharp
public void Init(int recipeID, UI_Crafting parent)
{
    // 아이콘, 이름 설정
    // RefreshCraftable() 호출
}

public void RefreshCraftable()
{
    bool can = RecipeManager.Instance.CanCraft(_recipeID);
    _imgCraftable.color = can ? _colorCraftable : _colorUncraftable;
}

public void SetSelected(bool selected)
{
    _selectOverlay.SetActive(selected);
}

public void OnPointerClick(PointerEventData eventData)
{
    _parent?.OnRecipeSlotClicked(_recipeID);
}
```

### UICraftingIngredientSlot (재료 슬롯)

**용도**: 오른쪽 패널의 재료 목록에 인스턴스화

**표시 내용**:
- 재료 아이콘 (`_imgIcon`)
- 재료 이름 (`_txtName`)
- 보유 수량 / 필요 수량 (`_txtCount`) 형식: `"5/10"` (색상: 초록=충족, 빨강=부족)
- 충족 여부 배경색 (`_imgCountBg`)

**핵심 메서드**:
```csharp
public void Init(int ingredientItemID, int requiredPerCraft, int quantity = 1)
{
    int needed = requiredPerCraft * quantity;
    int have = InventoryManager.Instance.GetItemCount(ingredientItemID);
    
    _txtCount.text = $"{have}/{needed}";
    _imgCountBg.color = have >= needed ? _colorSufficient : _colorInsufficient;
}

public void RefreshCount(int requiredPerCraft, int quantity = 1)
{
    // 수량 변경 시 호출 (인벤토리 변동 후)
}
```

---

## 데이터 관리 (CSV 임포트)

### CSV 형식

**메뉴**: Unity Editor → UPlayGround / Crafting / Import Recipe Data

#### recipe_master.csv

```
recipeID,recipeName,resultItemID,resultQuantity,costType,costAmount,castTimeSeconds,category,description,isDebugUnlocked
1,작은 포션,100,1,Free,0,1.0,Consumable,체력을 회복한다,FALSE
2,철 검,101,1,Gold,500,2.5,Equipment,기본 검,FALSE
3,고급 철 검,102,1,Gold,2000,3.0,Equipment,강화된 검,FALSE
```

| 컬럼 | 설명 | 예시 |
|------|------|------|
| recipeID | 고유 ID | 1, 2, 3, ... |
| recipeName | 레시피 이름 | "작은 포션" |
| resultItemID | 결과 아이템 ID | 100 |
| resultQuantity | 1회 제작 수량 | 1 |
| costType | 비용 타입 | Free, Gold |
| costAmount | 소모 비용 | 0, 500, ... |
| castTimeSeconds | 제작 시간 | 1.0, 2.5, ... |
| category | 카테고리 | Consumable, Equipment, Material, Special |
| description | 설명 | "체력을 회복한다" |
| isDebugUnlocked | 디버그 언락 | TRUE, FALSE |

#### recipe_ingredients.csv

```
recipeID,ingredientItemID,requiredQuantity
1,201,3
2,202,5
2,203,2
3,202,10
3,203,5
```

| 컬럼 | 설명 |
|------|------|
| recipeID | 어느 레시피의 재료인지 |
| ingredientItemID | 필요 아이템 ID |
| requiredQuantity | 1회 필요 수량 |

#### recipe_unlocks.csv

```
recipeID,conditionType,conditionValue,conditionValue2
2,MonsterKill,5,3
3,RecipeCraft,2,1
```

| 컬럼 | 설명 |
|------|------|
| recipeID | 어느 레시피의 언락 조건인지 |
| conditionType | None, MonsterKill, ItemCollect, ItemHave, RecipeCraft |
| conditionValue | 몬스터ID / 아이템ID / 레시피ID |
| conditionValue2 | 횟수 / 수량 |

### 임포트 절차

1. CSV 파일 준비: `Assets/10.Datas/Crafting/CSV/` 아래
2. Unity Editor 열기
3. 메뉴: **UPlayGround → Crafting → Import Recipe Data**
4. 에디터 윈도우에서 CSV 경로 확인
5. **Import** 버튼 클릭
6. RecipeDatabase.asset이 `Assets/10.Datas/Crafting/`에 생성됨

---

## 외부 연동 포인트

### 몬스터 처치 알림

EnemyCombat 또는 EnemyDeathState에서:

```csharp
public class EnemyCombat : ActorComponent
{
    public void OnEnemyDeath(int monsterID)
    {
        RecipeManager.Instance.NotifyMonsterKill(monsterID);
    }
}
```

### 레시피 직접 언락

스토리 이벤트나 치트 기능:

```csharp
// 레시피 ID 10 언락
RecipeManager.Instance.UnlockRecipe(10);

// 또는 여러 개
for (int i = 1; i <= 5; i++)
    RecipeManager.Instance.UnlockRecipe(i);
```

### 제작 UI 열기

```csharp
UIManager.Instance.ShowUI("Crafting");
```

### 제작 완료 감지

```csharp
RecipeManager.Instance.OnCraftingCompleted += (recipeID, quantity) =>
{
    Debug.Log($"제작 완료: {recipeID}, 획득: {quantity}");
};
```

---

## 예시 흐름

### 시나리오 1: 기본 제작

1. **UI 오픈**
   ```csharp
   UIManager.Instance.ShowUI("Crafting");
   ```

2. **레시피 선택**
   - UICraftingRecipeSlot 클릭
   - `UI_Crafting.OnRecipeSlotClicked(recipeID)` 호출
   - `ShowRecipeDetail()`: 재료, 비용, 시간 표시

3. **수량 선택**
   - `+` 버튼 클릭 → `_quantity++`
   - `RefreshIngredientCounts()`: 각 재료의 필요 수량 갱신

4. **제작 버튼**
   - `OnClickCraft()` → `RecipeManager.Instance.TryStartCrafting(recipeID, quantity)`
   - 내부: `DeductResources()` → 재료/비용 차감 (실패 시 롤백)
   - 내부: `OnCraftingStarted` 이벤트
   - UI: 진행 바 활성화, 수량 버튼 비활성화, 버튼 텍스트 "취소"로 변경

5. **제작 진행**
   - 매 프레임: `TickCrafting(deltaTime)` → `_craftingProgress` 갱신
   - UI: 진행 바 업데이트 (`_imgProgressBar.fillAmount = progress`)

6. **제작 완료**
   - `_castTimeRemaining <= 0` → `FinishCrafting()` 호출
   - 내부: 결과 아이템 획득, 제작 횟수 기록, 언락 조건 재평가
   - 내부: `OnCraftingCompleted` 이벤트
   - UI: 진행 바 1.0, "제작 완료!" 메시지, 1.5초 후 초기화

### 시나리오 2: 언락 조건

**상황**: 철 검(레시피 ID=2)이 드래곤(ID=5) 처치 3회로 언락됨

1. **드래곤 처치 1회**
   ```csharp
   // EnemyDeathState에서
   RecipeManager.Instance.NotifyMonsterKill(5);  // dragon ID
   RecipeManager.Instance.CheckUnlockConditions();
   ```
   - `_monsterKills[5] = 1`
   - 언락 조건 평가: `1 >= 3` → false, 언락 안 됨

2. **드래곤 처치 3회**
   - `_monsterKills[5] = 3`
   - 언락 조건 평가: `3 >= 3` → true
   - `UnlockRecipe(2)` 호출
   - `OnRecipeUnlocked` 이벤트
   - UI: 레시피 리스트 새로고침, 철 검이 표시됨

### 시나리오 3: 제작 취소

```csharp
if (RecipeManager.Instance.IsCrafting())
{
    RecipeManager.Instance.CancelCrafting();
    // OnCraftingCancelled 이벤트 발생
    // UI: 진행 바 0, "취소됨" 메시지
    // 재료는 환불되지 않음 (기획 결정)
}
```

---

## 확장 포인트

### 추가할 수 있는 기능

1. **제작 페일 확률**
   - `RecipeData`에 `failChance: float` 추가
   - `FinishCrafting()`에서 랜덤 체크

2. **등급 시스템**
   - `RecipeData`에 `rarity: int` 추가
   - UI에서 색상 표시 (백색, 초록, 파랑, 보라, 주황)

3. **제작 EXP / 스킬**
   - 제작 경험치 누적 → 새 레시피 언락
   - 제작 스킬 레벨 → 성공률 증가, 소요 시간 감소

4. **재료 대체**
   - 비슷한 아이템으로 대체 제작 (비용 추가)

5. **조합 제작**
   - 2개 이상의 레시피를 조합하여 새로운 레시피 생성

### 수정 포인트 (구현 시)

**RecipeManager의 체크 로직 확장**:
```csharp
public bool CanCraft(int recipeID, int quantity = 1)
{
    // 기존 체크들...
    if (!IsRecipeUnlocked(recipeID)) return false;
    
    // 추가 체크
    // if (!HasRequiredSkill(recipeID)) return false;
    // if (GetIngredientQuality(recipeID) < recipe.minQuality) return false;
    
    return true;
}
```

**FinishCrafting()에 추가 로직**:
```csharp
private void FinishCrafting()
{
    // 기존 로직...
    
    // 제작 결과 처리
    // RecipeManager.Instance.AddCraftingExp(...)
    // if (UnityEngine.Random.value < failChance) { /* 실패 */ }
    
    InventoryManager.Instance.AddItem(recipe.resultItemID, totalYield);
}
```

---

## 디버그

### 로그 메시지

RecipeManager는 주요 포인트에서 Debug.Log 출력:
- `[RecipeManager] RecipeDatabase 로드 완료`
- `[RecipeManager] 제작 시작: {레시피명} x{수량}`
- `[RecipeManager] 제작 완료: {레시피명} x{획득수량}`
- `[RecipeManager] 레시피 언락: {레시피명}`

RecipeDataImporter는 CSV 파싱 오류 시 경고:
- `[Importer] recipe_master 행 X 파싱 오류: ...`

### 디버그 언락

RecipeData의 `isDebugUnlocked = true`로 설정하면 조건 무시하고 처음부터 언락됨.

### 에디터에서 확인

1. Hierarchy에서 **RecipeManager** 찾기
2. Inspector에서 private member 토글하여 조회:
   - `_unlocked`: 각 레시피 언락 여부
   - `_craftCounts`: 각 레시피 제작 횟수
   - `_monsterKills`: 몬스터 처치 수
   - `_craftingProgress`: 현재 제작 진행률 (0~1)

---

## 성능 고려사항

### 최적화된 부분

- **Dictionary 캐시**: RecipeDatabase.Initialize()에서 런타임 딕셔너리 생성 → O(1) 조회
- **무효 검사**: `IsDBLoaded` 플래그로 DB 로드 전 조회 방지
- **리스트 재생성 최소화**: 카테고리 필터 변경 시만 슬롯 재생성

### 주의할 점

- **많은 레시피 (1000+)**: UI 슬롯 재생성 시간 증가 → 풀링 고려
- **CheckUnlockConditions() 호출 빈도**: 매번 모든 레시피 평가 → 최적화 가능
  ```csharp
  // 현재: O(n) 모든 레시피 확인
  // 개선: 변경이 있을 수 있는 레시피만 확인
  public void CheckUnlockConditions(int? affectedRecipeID = null)
  {
      // affectedRecipeID != null이면 해당 조건 관련 레시피만 평가
  }
  ```

---

## 참고 자료

### 코드 경로

| 항목 | 경로 |
|------|------|
| 데이터 | `Assets/02.Scripts/Data/Crafting/` |
| 매니저 | `Assets/02.Scripts/Manager/Crafting/RecipeManager.cs` |
| UI | `Assets/02.Scripts/UI/Crafting/` |
| 임포터 | `Assets/02.Scripts/Data/Crafting/Editor/RecipeDataImporter.cs` |
| 데이터베이스 SO | `Assets/10.Datas/Crafting/RecipeDatabase.asset` (Addressables: "RecipeDatabase") |
| CSV | `Assets/10.Datas/Crafting/CSV/` |

### 프로젝트 아키텍처 관련

- **InventoryManager**: 아이템 소유/수량 관리 (RecipeManager이 의존)
- **ItemManager**: 아이템 데이터 조회 (UI에서 아이콘/이름 표시 시)
- **UIManager**: UI 전시 (RecipeManager 독립적)
- **GameManager**: RecipeManager 초기화 (다른 매니저와 동일 생명주기)

### CLAUDE.md

프로젝트 전체 아키텍처: `CLAUDE.md` 참고
- 매니저 시스템 구조
- GameActor 계층 구조
- 컴포넌트 시스템
- 상태 머신 패턴

---

## 작성자 노트

**시스템 특징**:
- 깔끔한 분리: 데이터(SO) ↔ 로직(Manager) ↔ 표현(UI)
- 이벤트 기반: UI가 RecipeManager 상태 변화를 구독하여 반응
- 확장 가능: 새로운 언락 조건 타입 추가 시 EvaluateCondition() switch 문만 수정
- CSV 기반 데이터 관리: 기획자가 직접 수정 가능

**보완할 점** (향후):
- 제작 실패 확률
- 등급/품질 시스템
- 제작 스킬 / 경험치
- 일괄 제작 애니메이션
- 제작 음향 효과

