using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
    /// <summary>
    /// 퀘스트 상세 — 보상 아이템 1개 슬롯 (아이콘 + 수량).
    /// 골드/경험치는 별도 고정 필드로 표시하고, 이 슬롯은 아이템 보상 목록에만 쓴다.
    /// </summary>
    public class UIQuestRewardSlot : MonoBehaviour
    {
        [SerializeField] private Image           _imgIcon;
        [SerializeField] private TextMeshProUGUI _txtCount;

        public void Init(int itemId, int count)
        {
            var itemData = Svc.Item != null ? Svc.Item.GetItemData(itemId) : null;
            if (itemData != null && itemData.icon != null)
            {
                _imgIcon.sprite  = itemData.icon;
                _imgIcon.enabled = true;
            }
            else
            {
                _imgIcon.enabled = false;
            }

            if (_txtCount != null)
                _txtCount.text = count > 1 ? count.ToString() : string.Empty;
        }
    }
}
