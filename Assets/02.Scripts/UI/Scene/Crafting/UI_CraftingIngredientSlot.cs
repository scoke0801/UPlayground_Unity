using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Manager;

/// <summary>
/// 제작 UI — 재료 슬롯 1개
/// UI_Crafting의 재료 리스트에서 Instantiate해 사용한다.
/// </summary>
public class UI_CraftingIngredientSlot : MonoBehaviour
{
    [SerializeField] private Image            _imgIcon;
    [SerializeField] private TextMeshProUGUI  _txtName;
    [SerializeField] private TextMeshProUGUI  _txtCount;   // "보유/필요"
    [SerializeField] private Image            _imgCountBg; // 충족 여부 배경색

    [Header("색상")]
    [SerializeField] private Color _colorSufficient = new Color(0.2f, 0.8f, 0.3f);
    [SerializeField] private Color _colorInsufficient = new Color(0.9f, 0.25f, 0.25f);

    private int _ingredientItemID;

    /// <summary>
    /// 슬롯 데이터 설정.
    /// quantity : 현재 UI에서 선택된 제작 수량 (재료 필요 수량에 곱해진다)
    /// </summary>
    public void Init(int ingredientItemID, int requiredPerCraft, int quantity = 1)
    {
        _ingredientItemID = ingredientItemID;

        var itemData = ItemManager.Instance.GetItemData(ingredientItemID);
        int needed   = requiredPerCraft * quantity;
        int have     = InventoryManager.Instance.GetItemCount(ingredientItemID);

        // 아이콘 / 이름
        if (itemData != null)
        {
            _imgIcon.sprite  = itemData.icon;
            _imgIcon.enabled = itemData.icon != null;
            _txtName.text    = itemData.itemName;
        }
        else
        {
            _imgIcon.enabled = false;
            _txtName.text    = $"ID:{ingredientItemID}";
        }

        // 수량 텍스트
        _txtCount.text = $"{have}<color=#888888>/{needed}</color>";

        // 충족 여부 색상
        bool sufficient = have >= needed;
        if (_imgCountBg != null)
            _imgCountBg.color = sufficient ? _colorSufficient : _colorInsufficient;
        _txtCount.color = sufficient ? _colorSufficient : _colorInsufficient;
    }

    /// <summary>
    /// 인벤토리 변동 후 수량만 갱신할 때 사용
    /// </summary>
    public void RefreshCount(int requiredPerCraft, int quantity = 1)
    {
        int needed = requiredPerCraft * quantity;
        int have   = InventoryManager.Instance.GetItemCount(_ingredientItemID);

        _txtCount.text = $"{have}<color=#888888>/{needed}</color>";

        bool sufficient = have >= needed;
        if (_imgCountBg != null)
            _imgCountBg.color = sufficient ? _colorSufficient : _colorInsufficient;
        _txtCount.color = sufficient ? _colorSufficient : _colorInsufficient;
    }
}
