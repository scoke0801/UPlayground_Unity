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

        [Header("Cooldown")]
        [Tooltip("사용 성공 후 같은 소비 아이템을 다시 사용할 수 있을 때까지의 시간(초).")]
        [Min(0f)] public float cooldownDuration = 5f;
    }
}
