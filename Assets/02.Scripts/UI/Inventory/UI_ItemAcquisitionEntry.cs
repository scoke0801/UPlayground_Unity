using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Manager;

public class UI_ItemAcquisitionEntry : UI_Base
{
    [SerializeField] TextMeshProUGUI _itemInfoText;
    [SerializeField] private Image _rarityIcon;
    [SerializeField] private Image _itemIcon;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    public void Init(ItemSO itemData)
    {
        _rarityIcon.sprite = AssetManager.Instance.GetAtlas(itemData.itemRarity.ToString());
        _itemIcon.sprite = AssetManager.Instance.GetAtlas(itemData.itemId.ToString());
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
}
