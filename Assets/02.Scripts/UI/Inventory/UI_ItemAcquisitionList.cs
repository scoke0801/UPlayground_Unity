using UnityEngine;

public class UI_ItemAcquisitionList : UI_Base
{
    [SerializeField] private UI_ItemAcquisitionEntry _itemEntry;
    [SerializeField] private Transform _content;

    public void Start()
    {
        //_itemEntry = GetComponentInChildren<UI_ItemAcquisitionEntry>();
    }
    
    public void SetItem(ItemSO itemDaItem)
    {
        var go = Instantiate(_itemEntry, _content);
        go.gameObject.SetActive(true);
        
        go.Init(itemDaItem);
    }
}
