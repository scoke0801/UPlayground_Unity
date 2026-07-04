using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.Crafting;
using UPlayGround.InputDefine;
using UPlayGround.Manager;

/// <summary>
/// 제작(크래프팅) UI — Popup 레이어
///
/// 레이아웃:
///   왼쪽 패널  : 카테고리 탭 + 레시피 리스트 (스크롤)
///   오른쪽 패널: 레시피 상세 (결과 아이콘/이름/설명 + 재료 목록 + 비용/시간)
///   하단 패널  : 수량 스텝퍼 + 제작 버튼 + 진행 바
///
/// UIPrefabDatabase 키: "Craft" (UIKeyType.Craft)
/// 호출 예: UIManager.Instance.Toggle(UIKeyType.Craft);
///
/// 프리팹 초안은 에디터 툴 "UPlayGround/UI/제작 UI 프리팹 빌드"로 생성한다.
/// </summary>
public class UI_CraftMenu : UI_Base
{
    // ──── 왼쪽: 레시피 리스트 ────
    [Header("레시피 리스트")]
    [SerializeField] private Transform              _recipeListContent;
    [SerializeField] private UI_CraftingRecipeSlot  _recipeSlotPrefab;

    [Header("카테고리 탭")]
    // 탭 하이라이트/단일 선택은 UITabGroup이 관리한다. 인덱스 순서는 TabCategories와 일치.
    [SerializeField] private UITabGroup _tabGroup;

    // ──── 오른쪽: 레시피 상세 ────
    [Header("레시피 상세")]
    [SerializeField] private GameObject           _detailPanel;
    [SerializeField] private Image                _imgResultIcon;
    [SerializeField] private TextMeshProUGUI      _txtResultName;
    [SerializeField] private TextMeshProUGUI      _txtCategoryBadge; // 이름 아래 카테고리 배지 ("장비" 등)
    [SerializeField] private TextMeshProUGUI      _txtDescription;
    [SerializeField] private Transform            _ingredientContent;
    [SerializeField] private UI_CraftingIngredientSlot _ingredientSlotPrefab;
    [SerializeField] private TextMeshProUGUI      _txtCost;
    [SerializeField] private TextMeshProUGUI      _txtCastTime;

    // ──── 하단: 제작 조작 ────
    [Header("제작 조작")]
    [SerializeField] private Button              _btnCraft;
    [SerializeField] private TextMeshProUGUI     _txtCraftButton;
    [SerializeField] private Button              _btnQtyMinus;
    [SerializeField] private Button              _btnQtyPlus;
    [SerializeField] private Button              _btnQtyMax;
    [SerializeField] private TextMeshProUGUI     _txtQty;
    [SerializeField] private Image               _imgProgressBar;
    [SerializeField] private TextMeshProUGUI     _txtProgressPercent; // 진행바 위 "0%" 퍼센트 표시
    [SerializeField] private TextMeshProUGUI     _txtCraftStatus;
    [SerializeField] private Button              _btnClose;

    // ──── 런타임 상태 ────
    private readonly List<UI_CraftingRecipeSlot>      _spawnedRecipeSlots     = new List<UI_CraftingRecipeSlot>();
    private readonly List<UI_CraftingIngredientSlot>  _spawnedIngredientSlots = new List<UI_CraftingIngredientSlot>();

    private int               _selectedRecipeID = -1;
    private int               _quantity         = 1;
    private CraftingCategory? _categoryFilter   = null;  // null = 전체

    // ──────────────────────────────────────────────────────────
    #region UI_Base 생명주기

    protected override void Awake()
    {
        base.Awake();

        _btnCraft.onClick.AddListener(OnClickCraft);
        _btnQtyMinus.onClick.AddListener(() => ChangeQuantity(-1));
        _btnQtyPlus.onClick.AddListener(()  => ChangeQuantity(+1));
        _btnQtyMax?.onClick.AddListener(SetQuantityMax);
        _btnClose?.onClick.AddListener(Hide);

        if (_tabGroup != null)
            _tabGroup.SelectionChanged += OnTabSelected;
    }

    // 탭 인덱스 → 카테고리 필터 (프리팹의 탭 배치 순서와 반드시 일치, null = 전체)
    private static readonly CraftingCategory?[] TabCategories =
    {
        null,
        CraftingCategory.Consumable,
        CraftingCategory.Equipment,
        CraftingCategory.Material,
        CraftingCategory.Special,
    };

    // UITabGroup 선택 콜백 (탭 클릭 및 초기 Select 모두 여기로 들어온다)
    private void OnTabSelected(int index)
    {
        if (index < 0 || index >= TabCategories.Length) return;
        SetCategoryFilter(TabCategories[index]);
    }

    // 입력 레이어 상승/복원은 UI_Base가 BlocksLowerInput 기준으로 일괄 처리한다.
    protected override bool BlocksLowerInput => true;

    protected override void OnShow()
    {
        // RecipeManager 이벤트 구독
        RecipeManager.Instance.OnRecipeUnlocked    += OnRecipeUnlocked;
        RecipeManager.Instance.OnCraftingStarted   += OnCraftingStarted;
        RecipeManager.Instance.OnCraftingCompleted += OnCraftingCompleted;
        RecipeManager.Instance.OnCraftingCancelled += OnCraftingCancelled;

        SetProgress(0f);
        _txtCraftStatus.text       = string.Empty;
        _selectedRecipeID          = -1;
        _quantity                  = 1;

        if (_detailPanel != null)
            _detailPanel.SetActive(false);

        // "전체" 탭(인덱스 0)을 선택 상태로 시작 → SelectionChanged → SetCategoryFilter(null) → RefreshRecipeList
        if (_tabGroup != null)
        {
            _tabGroup.Select(0);
        }
        else
        {
            _categoryFilter = null;
            RefreshRecipeList();
        }
    }

    protected override void OnHide()
    {
        RecipeManager.Instance.OnRecipeUnlocked    -= OnRecipeUnlocked;
        RecipeManager.Instance.OnCraftingStarted   -= OnCraftingStarted;
        RecipeManager.Instance.OnCraftingCompleted -= OnCraftingCompleted;
        RecipeManager.Instance.OnCraftingCancelled -= OnCraftingCancelled;
    }

    protected override void OnDispose()
    {
        base.OnDispose();

        if (_tabGroup != null)
            _tabGroup.SelectionChanged -= OnTabSelected;

        // 혹시 구독이 남아있으면 정리
        if (RecipeManager.Instance != null)
        {
            RecipeManager.Instance.OnRecipeUnlocked    -= OnRecipeUnlocked;
            RecipeManager.Instance.OnCraftingStarted   -= OnCraftingStarted;
            RecipeManager.Instance.OnCraftingCompleted -= OnCraftingCompleted;
            RecipeManager.Instance.OnCraftingCancelled -= OnCraftingCancelled;
        }
    }

    public override bool PerformBackFunction()
    {
        Hide();
        return false;
    }

    protected override void Update()
    {
        base.Update();

        if (RecipeManager.Instance.IsCrafting())
            SetProgress(RecipeManager.Instance.GetCraftingProgress());
    }

    /// <summary>
    /// 진행바 fillAmount와 퍼센트 텍스트를 함께 갱신한다.
    /// </summary>
    private void SetProgress(float progress01)
    {
        progress01 = Mathf.Clamp01(progress01);
        _imgProgressBar.fillAmount = progress01;
        if (_txtProgressPercent != null)
            _txtProgressPercent.text = $"{Mathf.RoundToInt(progress01 * 100f)}%";
    }

    #endregion

    // ──────────────────────────────────────────────────────────
    #region 레시피 리스트

    private void RefreshRecipeList()
    {
        // 기존 슬롯 제거
        foreach (var slot in _spawnedRecipeSlots)
        {
            if (slot != null) Destroy(slot.gameObject);
        }
        _spawnedRecipeSlots.Clear();

        if (!RecipeManager.Instance.IsDBLoaded) return;

        var ids = RecipeManager.Instance.GetUnlockedRecipeIDs();

        bool selectionStillVisible = false;

        foreach (var id in ids)
        {
            var data = RecipeManager.Instance.GetRecipeData(id);
            if (data == null) continue;

            // 카테고리 필터
            if (_categoryFilter.HasValue && data.category != _categoryFilter.Value)
                continue;

            var slot = Instantiate(_recipeSlotPrefab, _recipeListContent);
            slot.Init(id, this);
            _spawnedRecipeSlots.Add(slot);

            // 이전 선택 유지
            if (id == _selectedRecipeID)
            {
                slot.SetSelected(true);
                selectionStillVisible = true;
            }
        }

        RefreshAllSlotCraftability();

        // 현재 선택이 목록에 없으면(최초 진입/카테고리 전환 등) 첫 번째 레시피를 자동 선택한다.
        if (!selectionStillVisible && _spawnedRecipeSlots.Count > 0)
            OnRecipeSlotClicked(_spawnedRecipeSlots[0].RecipeID);
    }

    private void RefreshAllSlotCraftability()
    {
        foreach (var slot in _spawnedRecipeSlots)
            slot.RefreshCraftable();
    }

    private void SetCategoryFilter(CraftingCategory? category)
    {
        _categoryFilter = category;
        RefreshRecipeList();
    }

    #endregion

    // ──────────────────────────────────────────────────────────
    #region 레시피 선택 (슬롯에서 호출)

    public void OnRecipeSlotClicked(int recipeID)
    {
        if (_selectedRecipeID == recipeID) return;

        // 선택 표시 갱신
        foreach (var slot in _spawnedRecipeSlots)
            slot.SetSelected(slot.RecipeID == recipeID);

        _selectedRecipeID = recipeID;
        _quantity         = 1;

        ShowRecipeDetail(recipeID);
        RefreshCraftButton();
    }

    private void ShowRecipeDetail(int recipeID)
    {
        var recipe = RecipeManager.Instance.GetRecipeData(recipeID);
        if (recipe == null) return;

        if (_detailPanel != null)
            _detailPanel.SetActive(true);

        // 결과 아이콘
        var resultItem = ItemManager.Instance.GetItemData(recipe.resultItemID);
        if (resultItem != null)
        {
            _imgResultIcon.sprite  = resultItem.icon;
            _imgResultIcon.enabled = resultItem.icon != null;
            _txtResultName.text    = recipe.resultQuantity > 1
                ? $"{recipe.recipeName}  <size=75%>x{recipe.resultQuantity}</size>"
                : recipe.recipeName;
        }
        else
        {
            _imgResultIcon.enabled = false;
            _txtResultName.text    = recipe.recipeName;
        }

        // 카테고리 배지
        if (_txtCategoryBadge != null)
            _txtCategoryBadge.text = GetCategoryLabel(recipe.category);

        _txtDescription.text = recipe.description;

        // 비용 / 시간
        _txtCost.text = recipe.costType == CostType.Free
            ? "무료"
            : $"{recipe.costAmount} G";

        _txtCastTime.text = $"{recipe.castTimeSeconds:0.0}초";

        // 재료 슬롯 재생성
        foreach (var s in _spawnedIngredientSlots)
            if (s != null) Destroy(s.gameObject);
        _spawnedIngredientSlots.Clear();

        foreach (var ingr in RecipeManager.Instance.GetIngredients(recipeID))
        {
            var slot = Instantiate(_ingredientSlotPrefab, _ingredientContent);
            slot.Init(ingr.ingredientItemID, ingr.requiredQuantity, _quantity);
            _spawnedIngredientSlots.Add(slot);
        }

        UpdateQuantityText();
    }

    #endregion

    // ──────────────────────────────────────────────────────────
    #region 수량 스텝퍼

    private void ChangeQuantity(int delta)
    {
        _quantity = Mathf.Max(1, _quantity + delta);
        UpdateQuantityText();
        RefreshIngredientCounts();
        RefreshCraftButton();
    }

    /// <summary>
    /// 현재 보유 재료·골드로 만들 수 있는 최대 수량으로 설정한다.
    /// 제작 가능한 조합이 없으면 최소 1로 유지한다.
    /// </summary>
    private void SetQuantityMax()
    {
        if (_selectedRecipeID == -1) return;
        if (RecipeManager.Instance.IsCrafting()) return;

        int max     = RecipeManager.Instance.GetMaxCraftableQuantity(_selectedRecipeID);
        _quantity   = Mathf.Max(1, max);

        UpdateQuantityText();
        RefreshIngredientCounts();
        RefreshCraftButton();
    }

    private static string GetCategoryLabel(CraftingCategory category)
    {
        switch (category)
        {
            case CraftingCategory.Consumable: return "소비";
            case CraftingCategory.Equipment:  return "장비";
            case CraftingCategory.Material:   return "재료";
            case CraftingCategory.Special:    return "특수";
            default:                          return string.Empty;
        }
    }

    private void UpdateQuantityText()
    {
        _txtQty.text = _quantity.ToString();
    }

    private void RefreshIngredientCounts()
    {
        if (_selectedRecipeID == -1) return;

        var ingredients = RecipeManager.Instance.GetIngredients(_selectedRecipeID);
        for (int i = 0; i < _spawnedIngredientSlots.Count && i < ingredients.Count; i++)
        {
            _spawnedIngredientSlots[i].RefreshCount(ingredients[i].requiredQuantity, _quantity);
        }
    }

    #endregion

    // ──────────────────────────────────────────────────────────
    #region 제작 버튼

    private void OnClickCraft()
    {
        if (_selectedRecipeID == -1) return;

        if (RecipeManager.Instance.IsCrafting())
        {
            RecipeManager.Instance.CancelCrafting();
            return;
        }

        RecipeManager.Instance.TryStartCrafting(_selectedRecipeID, _quantity);
    }

    private void RefreshCraftButton()
    {
        if (_selectedRecipeID == -1)
        {
            _btnCraft.interactable = false;
            _txtCraftButton.text   = "레시피 선택";
            return;
        }

        if (RecipeManager.Instance.IsCrafting())
        {
            _btnCraft.interactable = true;
            _txtCraftButton.text   = "취소";
            return;
        }

        bool canCraft = RecipeManager.Instance.CanCraft(_selectedRecipeID, _quantity);
        _btnCraft.interactable = canCraft;
        _txtCraftButton.text   = canCraft ? "제작" : "재료 부족";
    }

    #endregion

    // ──────────────────────────────────────────────────────────
    #region RecipeManager 이벤트 콜백

    private void OnRecipeUnlocked(int recipeID)
    {
        RefreshRecipeList();
    }

    private void OnCraftingStarted(int recipeID)
    {
        SetProgress(0f);
        _txtCraftStatus.text       = "제작 중...";
        _txtCraftButton.text       = "취소";

        SetCraftingControlsLocked(true);
    }

    private void OnCraftingCompleted(int recipeID, int resultCount)
    {
        SetProgress(1f);
        _txtCraftStatus.text       = "제작 완료!";
        SetCraftingControlsLocked(false);

        RefreshRecipeList();
        RefreshIngredientCounts();
        RefreshCraftButton();

        // 완료 메시지 1.5초 후 초기화
        CancelInvoke(nameof(ClearCraftStatus));
        Invoke(nameof(ClearCraftStatus), 1.5f);
    }

    private void OnCraftingCancelled()
    {
        SetProgress(0f);
        _txtCraftStatus.text       = "취소됨";
        SetCraftingControlsLocked(false);
        RefreshCraftButton();

        CancelInvoke(nameof(ClearCraftStatus));
        Invoke(nameof(ClearCraftStatus), 1f);
    }

    /// <summary>
    /// 제작 진행 중 수량·닫기 조작을 잠그고 취소(=제작) 버튼만 활성화한다.
    /// SetInteractable(CanvasGroup 전체)과 달리 개별 버튼을 제어하므로
    /// 취소 버튼 클릭이 blocksRaycasts에 막히지 않는다.
    /// </summary>
    private void SetCraftingControlsLocked(bool locked)
    {
        _btnQtyMinus.interactable             = !locked;
        _btnQtyPlus.interactable              = !locked;
        if (_btnQtyMax != null) _btnQtyMax.interactable = !locked;
        if (_btnClose != null) _btnClose.interactable = !locked;
        _btnCraft.interactable                = true;   // 제작/취소 버튼은 항상 활성
    }

    private void ClearCraftStatus()
    {
        _txtCraftStatus.text = string.Empty;
    }

    #endregion
}
