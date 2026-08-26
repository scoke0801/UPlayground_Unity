using UnityEngine;

namespace UPlayGround.Data.Item
{
    public enum ConsumableEffectType
    {
        None = 0,
        HealFlat,
        HealPercent,
        CompanionExperience,
    }

    [CreateAssetMenu(fileName = "ConsumableSO", menuName = "UPlayGround/아이템/Consumable")]
    public class ConsumableSO : ItemSO
    {
        [Header("Consumable Effect")]
        public ConsumableEffectType effectType = ConsumableEffectType.None;
        [Min(0f)] public float amount;
        [Tooltip("동료 경험치 효과가 지정 캐릭터에게 지급할 경험치.")]
        public long experienceAmount;
        public bool requireEffectiveUse = true;

        [Header("Cooldown")]
        [Tooltip("사용 성공 후 같은 소비 아이템을 다시 사용할 수 있을 때까지의 시간(초).")]
        [Min(0f)] public float cooldownDuration = 5f;

        public bool RequiresCharacterTarget =>
            effectType == ConsumableEffectType.CompanionExperience;

        public bool IsHealingEffect =>
            effectType is ConsumableEffectType.HealFlat or ConsumableEffectType.HealPercent;

        public bool IsQuickSlotCompatible => IsHealingEffect;

#if UNITY_EDITOR
        private void OnValidate()
        {
            amount = Mathf.Max(0f, amount);
            experienceAmount = System.Math.Max(0L, experienceAmount);
            cooldownDuration = Mathf.Max(0f, cooldownDuration);
        }
#endif
    }
}
