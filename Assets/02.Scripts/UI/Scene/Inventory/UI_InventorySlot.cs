using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;

/// <summary>
/// 인벤토리 UI 슬롯
///
/// 하이라이트는 두 종류:
///   - hover: UI_Inventory의 공유 _itemClickTap 오버레이가 마우스가 올라온 슬롯으로 이동
///   - focus: EventSystem이 이 슬롯을 선택(키보드/게임패드 네비게이션 또는 클릭)했을 때 _focusHighlight 표시
/// focus를 받으려면 슬롯 루트에 Selectable이 있어야 한다(프리팹 빌더에서 부착).
/// </summary>
public class UI_InventorySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler,
                                ISelectHandler, IDeselectHandler
{
    [SerializeField] private GameObject _rootContent;
    [SerializeField] private GameObject _rootEmptySlot;
    [SerializeField] private TextMeshProUGUI _txtCount;
    [SerializeField] private TextMeshProUGUI _txtWeight;
    [SerializeField] private TextMeshProUGUI _txtEnhance;   // 강화 배지 "+N"
    [SerializeField] private Image _imgItem;
    [SerializeField] private Image _imgRarity;
    [SerializeField] private GameObject _focusHighlight;    // 포커스(선택) 시 표시되는 하이라이트 프레임

    private ItemSO _itemData = null;
    private int _itemCount = 0;
    private int _enhanceLevel = 0;

    private UI_Inventory _parent;

    public bool HasItem => _itemData != null;

    private void OnEnable()
    {
        RefreshUI();
    }

    private void OnDisable()
    {
        // 비활성화되면 포커스 하이라이트도 해제(재사용 시 잔상 방지)
        SetFocus(false);
    }

    public void Init(ItemSO itemData, int count, int enhanceLevel = 0)
    {
        _itemData = itemData;
        _itemCount = count;
        _enhanceLevel = enhanceLevel;
    }

    public void Clear()
    {
        Init(null, 0);
        RefreshUI();
    }

    public void SetParent(UI_Inventory inventory)
    {
        _parent = inventory;
    }

    public void RefreshUI()
    {
        if (_itemData == null)
        {
            _rootContent.SetActive(false);
            _rootEmptySlot.SetActive(true);
        }
        else
        {            
            _rootContent.SetActive(true);
            _rootEmptySlot.SetActive(false);
            _imgRarity.color = _itemData.itemRarity.ToColor();
            _imgItem.sprite = _itemData.icon;
            _txtCount.text = _itemCount.ToString();
            _txtWeight.text = $"{InventoryManager.Instance.GetItemWeight(_itemData.itemId):0.0}";

            if (_txtEnhance != null)
                _txtEnhance.text = _enhanceLevel > 0 ? $"+{_enhanceLevel}" : string.Empty;
        }
    }

    #region IPointerEnterHandler / IPointerExitHandler
    public void OnPointerEnter(PointerEventData eventData)
    {
        _parent?.SetItemClickAnimation(this);
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        _parent?.OnSlotPointerExit();
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (_parent == null)
            return;

        if (_itemData != null)
            _parent.ShowSelectedItemDetail(_itemData, _itemCount);
        else
            _parent.ClearSelectedItemDetail();
    }
    #endregion

    #region ISelectHandler / IDeselectHandler (키보드/게임패드 포커스)
    public void OnSelect(BaseEventData eventData)
    {
        SetFocus(true);

        if (_itemData != null)
            _parent?.ShowSelectedItemDetail(_itemData, _itemCount);
        else
            _parent?.ClearSelectedItemDetail();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        SetFocus(false);
    }
    #endregion

    /// <summary> 포커스 하이라이트 표시/숨김. </summary>
    public void SetFocus(bool focused)
    {
        if (_focusHighlight != null)
            _focusHighlight.SetActive(focused);
    }
}
