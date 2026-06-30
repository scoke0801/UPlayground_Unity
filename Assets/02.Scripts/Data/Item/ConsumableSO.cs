using UnityEngine;

namespace UPlayGround.Data.Item
{
    public enum ConsumableEffectType
    {
        None = 0,
        HealFlat,
        HealPercent,
    }

    [CreateAssetMenu(fileName = "ConsumableSO", menuName = "UPlayGround/아이템/Consumable")]
    public class ConsumableSO : ItemSO
    {
        [Header("Consumable Effect")]
        public ConsumableEffectType effectType = ConsumableEffectType.None;
        [Min(0f)] public float amount;
        public bool requireEffectiveUse = true;
    }
}
