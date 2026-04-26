using System;

namespace UPlayGround.Data.Stat
{
    /// <summary>
    /// 스탯 수정자 적용 방식.
    /// 계산 순서: Base + Σ(Flat) → ×(1 + Σ(Percent)) → ×Π(Multiply).
    /// </summary>
    public enum ModifierType
    {
        Flat,       // finalValue += value
        Percent,    // finalValue *= (1 + ΣPercent)  — 0.1 = +10%
        Multiply,   // finalValue *= value           — 직접 배율 (위상 변환 등)
    }

    /// <summary>
    /// 스탯 변경 단위. ActorStatContainer.AddModifier로 등록하고 source로 식별해 제거한다.
    /// </summary>
    [Serializable]
    public struct StatModifier
    {
        public StatType     statType;
        public ModifierType modifierType;
        public float        value;

        /// <summary>
        /// 제거 시 식별자. 장비 SO 인스턴스, 버프 ID 문자열 등을 넣는다.
        /// RemoveModifiersBySource(source)로 일괄 제거.
        /// </summary>
        public object source;

        /// <summary>
        /// -1 = 영구 (장비 장착 등), 0 초과 = 남은 지속 시간(초).
        /// </summary>
        public float duration;

        public StatModifier(StatType type, ModifierType mod, float val, object src = null, float dur = -1f)
        {
            statType     = type;
            modifierType = mod;
            value        = val;
            source       = src;
            duration     = dur;
        }

        public bool IsPermanent => duration < 0f;
    }
}
