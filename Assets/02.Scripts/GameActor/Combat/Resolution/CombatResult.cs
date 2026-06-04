using UPlayGround.UI;

namespace UPlayGround.Combat
{
    /// <summary>
    /// 한 번의 히트 처리 결과를 묶는 집계 값 객체 (P1 도입).
    /// 방어(<see cref="DefenseResult"/>), 피해(<see cref="DamageResult"/>), 리액션(<see cref="ReactionDecision"/>),
    /// 적용된 리소스 변화(<see cref="ResourceChangeSet"/>)를 한곳에 모아 피드백과 전투 로그가 같은 객체를 읽게 한다.
    ///
    /// P1 단계에서는 Resolver 호출이 Actor 코드에 흩어져 있어 일부 필드(특히 Reaction, Poise/Break 델타)가
    /// 채워지지 않을 수 있다. 호출 순서 표준화와 누락 필드 채움은 P2(CombatResolutionPipeline)에서 진행한다.
    /// </summary>
    public readonly struct CombatResult
    {
        public readonly HitContext Hit;
        public readonly DefenseResult Defense;
        public readonly DamageResult Damage;
        public readonly ReactionDecision Reaction;
        public readonly ResourceChangeSet Resources;

        public CombatResult(
            HitContext hit,
            DefenseResult defense,
            DamageResult damage,
            ReactionDecision reaction,
            ResourceChangeSet resources)
        {
            Hit = hit;
            Defense = defense;
            Damage = damage;
            Reaction = reaction;
            Resources = resources;
        }

        public GameActor Attacker => Hit.Attacker;
        public GameActor Victim => Hit.Victim;
        public bool DamageApplied => Defense.ShouldApplyDamage;
        public float FinalDamage => Damage.FinalDamage;
        public FloatStyle FloaterStyle => Damage.FloaterStyle;
        public DefenseOutcome DefenseOutcome => Defense.Outcome;
        public CombatReactionState ReactionState => Reaction.TargetState;

        public static CombatResult Build(
            HitContext hit,
            DefenseResult defense,
            DamageResult damage,
            ReactionDecision reaction,
            ResourceChangeSet resources)
            => new CombatResult(hit, defense, damage, reaction, resources);
    }
}
