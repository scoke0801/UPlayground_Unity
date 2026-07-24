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

}
