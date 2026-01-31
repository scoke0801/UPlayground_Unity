

using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Manager;

public class UI_ItemPopup : UI_Base
{
    [SerializeField] private UIItemSlot _itemSlot;
    [SerializeField] private TextMeshProUGUI _itemNameText;
    [SerializeField] private TextMeshProUGUI _itemWeightText;
    [SerializeField] private TextMeshProUGUI _itemDescText;
    [SerializeField] private UICommonButton _bottomButton;
    [SerializeField] private Button _closeButton;
    
    private ItemSO _cachedItemSo = null;

    private void Awake()
    {
        _closeButton.onClick.AddListener(OnClickClose);
    }

    protected override void OnShow()
    {
        base.OnShow();
    }

    protected override void OnHide()
    {
        _cachedItemSo = null;
        
        base.OnHide();
    }

    public override bool PerformBackFunction()
    {
        // ESC 키 입력 시 닫는다.
        Hide();
        return false;
    }
    
    public void Init(ItemSO itemData, int count)
    {
        _itemSlot.Init(itemData, count);
        
        _itemNameText.text = itemData.name;
        _itemDescText.text = itemData.itemDescription;
        
        _itemWeightText.text = $"{InventoryManager.Instance.GetItemWeight(itemData.itemId):0.0}";

        InitButton(itemData);
    }

    private void InitButton(ItemSO itemData)
    {
        // [TODO]버튼은 상황에 따라 다르게 하자
        // 1. 장착 2. 해제 3. 사용
        _bottomButton.Text.text = "장착";
    }
    
    private void OnClickClose()
    {
        Hide();
    }
}