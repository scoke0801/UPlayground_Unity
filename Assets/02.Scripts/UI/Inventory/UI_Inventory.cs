using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UPlayGround.Manager;

/// <summary>
/// 인벤토리 UI
/// </summary>
public class UI_Inventory : UI_Base
{
    [SerializeField] private UI_InventorySlot _itemPanelPrefab;
    [SerializeField] private Transform _content;
    [SerializeField] private Image _imgWeightFill;
    [SerializeField] private TextMeshProUGUI _txtWeight;
   
    [Header("Character Preview")]
    [SerializeField] private RawImage _characterPreview;
    [SerializeField] private CharacterPreviewRenderer _previewRenderer;

    private List<UI_InventorySlot> _uiSlots = new List<UI_InventorySlot>();
    private int itemMaximumValue = 50;

    public GameObject _itemClickTap;
    
    private void Awake()
    {
        Init();
        
        // RenderTexture 연결
        if (_previewRenderer != null && _characterPreview != null)
        {
            _characterPreview.texture = _previewRenderer.GetRenderTexture();
        }
    }

    protected override void OnShow()
    {
        RefreshDictItem();
        SetInventory();
        
        // 캐릭터 프리뷰 활성화
        if (_previewRenderer != null)
        {
            _previewRenderer.ShowPreview();
        }
    }

    protected override void OnHide()
    {
        // 캐릭터 프리뷰 비활성화
        if (_previewRenderer != null)
        {
            _previewRenderer.HidePreview();
        }
    }
    
    public void SetInventory()
    {
        foreach (var t in _uiSlots)
        {
            t.RefreshUI();
        }

        _imgWeightFill.fillAmount = InventoryManager.Instance.GetTotalWeight() / InventoryManager.Instance.MaxWeight;
        _txtWeight.text =
            $"({InventoryManager.Instance.GetTotalWeight():0.0}/{InventoryManager.Instance.MaxWeight:0.0})";
    }

    public void SetItemClickAnimation(UI_InventorySlot slot)
    {
        _itemClickTap.gameObject.SetActive(true);
        _itemClickTap.transform.SetParent(slot.transform);
        
        _itemClickTap.transform.localPosition = Vector3.zero;
        //_itemClickTap.GetComponent<RectTransform>().anchoredPosition = slot.GetComponent<RectTransform>().anchoredPosition;
    }

    public void OnSlotPointerExit()
    {
        _itemClickTap.gameObject.SetActive(false);
    }
    
    private void Init()
    {
        for (int i = 0; i < itemMaximumValue; ++i)
        {
            var go = Instantiate(_itemPanelPrefab, _content);
            _uiSlots.Add(go);
            _uiSlots[i].SetParent(this);
        }
    }

    private void RefreshDictItem()
    {
        int value = 0;
        foreach (var item in InventoryManager.Instance.ItemDict)
        {
            _uiSlots[value++].Init(item.Value);
        }
    }
}
