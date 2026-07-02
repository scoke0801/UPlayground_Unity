using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.EnumType;

public class UI_ItemAcquisitionEntry : UI_Base
{
    [SerializeField] TextMeshProUGUI _itemInfoText;
    [SerializeField] private Image _rarityIcon;
    [SerializeField] private Image _itemIcon;

    protected override void Awake()
    {
        base.Awake();
        _animator = GetComponent<Animator>();
    }

    public void Init(ItemSO itemData)
    {
        _rarityIcon.color = GetRarityColor(itemData.itemRarity);
        _itemIcon.sprite = itemData.icon;
        _itemInfoText.text = itemData.itemName;

        StartCoroutine(DestroyAfterAnimation());
    }

    private IEnumerator DestroyAfterAnimation()
    {
        yield return null; // 애니메이터 상태 갱신 대기

        float clipLength = 0f;
        if (_animator != null)
        {
            var info = _animator.GetCurrentAnimatorStateInfo(0);
            clipLength = info.length;
        }

        yield return new WaitForSeconds(clipLength);
        Destroy(gameObject);
    }

    private static Color GetRarityColor(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.COMMON => Color.white,
            ItemRarity.UNCOMMON => new Color(0.35f, 0.9f, 0.45f),
            ItemRarity.RARE => new Color(0.35f, 0.6f, 1f),
            ItemRarity.UNIQUE => new Color(0.85f, 0.45f, 1f),
            ItemRarity.LEGENDARY => new Color(1f, 0.65f, 0.2f),
            _ => Color.clear
        };
    }
}
