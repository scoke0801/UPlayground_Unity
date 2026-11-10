using UnityEngine;
using UPlayGround.Data.Item;

namespace UPlayGround
{
    /// <summary>미획득 바닥 아이템의 외형만 표시하며 지급 상태는 소유하지 않는다.</summary>
    public sealed class GroundDropRepresentation : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _icon;
        public void Initialize(ItemSO item)
        {
            if (_icon != null) _icon.sprite = item != null ? item.icon : null;
        }
    }
}
