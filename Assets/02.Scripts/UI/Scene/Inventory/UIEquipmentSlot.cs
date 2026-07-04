using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UPlayGround.Data.EnumType;

/// <summary>
/// 인벤토리 화면의 캐릭터 장비 슬롯 하나(주/보조 무기 + 방어구 5부위).
/// 장착 아이템 아이콘을 표시하고, 클릭 시 해당 슬롯을 해제하도록 부모에 알린다.
/// </summary>
public class UIEquipmentSlot : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image _icon;
    [SerializeField] private GameObject _emptyOverlay;      // 빈 슬롯 표시
    [SerializeField] private TextMeshProUGUI _slotLabel;    // "주무기", "머리" 등
    [SerializeField] private EquipPosition _slot = EquipPosition.None;

    private Action<EquipPosition> _onClick;

    public EquipPosition Slot => _slot;

    public void SetSlot(EquipPosition slot) => _slot = slot;

    public void SetClickHandler(Action<EquipPosition> onClick) => _onClick = onClick;

    public void SetLabel(string text)
    {
        if (_slotLabel != null)
            _slotLabel.text = text;
    }

    public void SetItem(ItemSO item)
    {
        bool has = item != null;
        if (_icon != null)
        {
            _icon.sprite  = item != null ? item.icon : null;
            _icon.enabled = has && item.icon != null;
        }
        if (_emptyOverlay != null)
            _emptyOverlay.SetActive(!has);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        _onClick?.Invoke(_slot);
    }
}
