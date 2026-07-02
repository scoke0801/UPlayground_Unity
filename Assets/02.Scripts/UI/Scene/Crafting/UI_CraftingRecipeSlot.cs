using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UPlayGround.Data.Crafting;
using UPlayGround.Manager;

/// <summary>
/// 제작 UI — 레시피 리스트 슬롯 1개
/// UI_Crafting의 레시피 ScrollView에서 Instantiate해 사용한다.
/// </summary>
public class UI_CraftingRecipeSlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image            _imgResultIcon;
    [SerializeField] private TextMeshProUGUI  _txtRecipeName;
    [SerializeField] private Image            _imgCraftable;     // 제작 가능 인디케이터
    [SerializeField] private TextMeshProUGUI  _txtStatus;        // "제작 가능" / "재료 부족" 상태 문구
    [SerializeField] private GameObject       _selectOverlay;    // 선택 시 하이라이트

    [Header("색상")]
    [SerializeField] private Color _colorCraftable   = new Color(0.2f, 0.9f, 0.3f);
    [SerializeField] private Color _colorUncraftable = new Color(0.5f, 0.5f, 0.5f);
    [SerializeField] private Color _colorStatusFail  = new Color(0.9f, 0.25f, 0.25f); // "재료 부족" 문구색
    [SerializeField] private Color _colorHover       = new Color(1f, 1f, 1f, 0.12f);

    private int        _recipeID;
    private UI_CraftMenu _parent;

    // ──────────────────────────────────────────

    /// <summary>
    /// 슬롯 초기화.
    /// </summary>
    public void Init(int recipeID, UI_CraftMenu parent)
    {
        _recipeID = recipeID;
        _parent   = parent;

        var recipe = RecipeManager.Instance.GetRecipeData(recipeID);
        if (recipe == null) return;

        // 결과 아이템 아이콘
        var resultItem = ItemManager.Instance.GetItemData(recipe.resultItemID);
        if (resultItem != null && resultItem.icon != null)
        {
            _imgResultIcon.sprite  = resultItem.icon;
            _imgResultIcon.enabled = true;
        }
        else
        {
            _imgResultIcon.enabled = false;
        }

        _txtRecipeName.text = $"{recipe.recipeName}";

        if (recipe.resultQuantity > 1)
            _txtRecipeName.text += $" <size=80%>x{recipe.resultQuantity}</size>";

        RefreshCraftable();

        if (_selectOverlay != null)
            _selectOverlay.SetActive(false);
    }

    /// <summary>
    /// 현재 인벤토리 상태에 맞게 제작 가능 여부 인디케이터를 갱신한다.
    /// </summary>
    public void RefreshCraftable()
    {
        bool can = RecipeManager.Instance.CanCraft(_recipeID);
        if (_imgCraftable != null)
            _imgCraftable.color = can ? _colorCraftable : _colorUncraftable;

        if (_txtStatus != null)
        {
            _txtStatus.text  = can ? "제작 가능" : "재료 부족";
            _txtStatus.color = can ? _colorCraftable : _colorStatusFail;
        }
    }

    /// <summary>
    /// 선택 상태 표시
    /// </summary>
    public void SetSelected(bool selected)
    {
        if (_selectOverlay != null)
            _selectOverlay.SetActive(selected);
    }

    public int RecipeID => _recipeID;

    // ──────────────────────────────────────────
    #region IPointer

    public void OnPointerClick(PointerEventData eventData)
    {
        _parent?.OnRecipeSlotClicked(_recipeID);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_selectOverlay != null && !_selectOverlay.activeSelf)
        {
            // hover 시 약한 하이라이트 (선택 오버레이와 다른 방법으로 처리해도 됨)
        }
    }

    public void OnPointerExit(PointerEventData eventData) { }

    #endregion
}
