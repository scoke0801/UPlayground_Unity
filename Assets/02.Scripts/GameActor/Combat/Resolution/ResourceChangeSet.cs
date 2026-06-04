namespace UPlayGround.Combat
{
    /// <summary>
    /// 한 번의 히트로 <b>실제 적용된</b> 리소스 변화량(델타). 음수 = 감소(피해).
    /// "의도된 값"(attackData.poiseDamage 등)이 아니라 "적용된 값"을 담는다 — 둘을 혼동하면 튜닝 로그가 왜곡된다.
    ///
    /// P1: HpDelta만 채운다. PoiseDelta/BreakDelta는 적용 시점이 OnDamaged 내부로 분산되어 있어
    /// P2에서 ResourceApplier가 적용을 일원화할 때 함께 채운다(그 전에는 0 = 미집계).
    /// </summary>
    public readonly struct ResourceChangeSet
    {
        public readonly float HpDelta;
        public readonly float PoiseDelta;
        public readonly float BreakDelta;

        public ResourceChangeSet(float hpDelta, float poiseDelta = 0f, float breakDelta = 0f)
        {
            HpDelta = hpDelta;
            PoiseDelta = poiseDelta;
            BreakDelta = breakDelta;
        }

        public static ResourceChangeSet Empty => new ResourceChangeSet(0f);

        /// <summary>HP 피해만 적용된 경우의 헬퍼. damage는 양수, HpDelta는 음수로 저장한다.</summary>
        public static ResourceChangeSet FromDamage(float damageApplied) => new ResourceChangeSet(-damageApplied);

        public ResourceChangeSet WithPoiseAndBreak(float poiseDamageApplied, float breakDamageApplied)
        {
            return new ResourceChangeSet(
                HpDelta,
                -poiseDamageApplied,
                -breakDamageApplied);
        }
    }
}
