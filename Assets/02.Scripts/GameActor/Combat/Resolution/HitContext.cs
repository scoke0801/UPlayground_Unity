using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Combat
{
    /// <summary>
    /// 단일 히트 1회의 입력 정보를 표현하는 값 객체 (P1 도입).
    /// HitRequest와 피격 대상을 결합한 파이프라인 입력이다.
    /// </summary>
    public readonly struct HitContext
    {
        public readonly GameActor Attacker;
        public readonly GameActor Victim;
        public readonly AnimKey AnimKey;
        public readonly int HitPhaseIndex;
        public readonly AttackKind AttackKind;
        public readonly AttackReactionType ReactionType;
        public readonly AttackDefenseType DefenseType;

        /// <summary>적용 전 raw 입력 피해(attackData.damage). 최종 피해는 <see cref="DamageResult"/>가 담는다.</summary>
        public readonly float Damage;
        public readonly float PoiseDamage;
        public readonly float BreakDamage;
        public readonly bool IsCounterAttack;
        public readonly bool UseCounterHitFeedback;
        public readonly bool IsProjectile;
        public readonly float ReactionDuration;
        public readonly bool ForceReaction;
        public readonly bool ForceBreakExpose;
        public readonly float CriticalMultiplier;

        /// <summary>raw attackData.hitPoint. 피드백의 zero 폴백 판정이 동일하게 동작하도록 가공하지 않고 보관한다.</summary>
        public readonly Vector3 HitPoint;
        public readonly Vector3 AttackDirection;
        public readonly GameObject HitTarget;
        public readonly string HitParticleName;
        public readonly float PullForce;
        public readonly float AirborneForce;
        public readonly float KnockbackForce;
        public readonly float KnockbackDrag;
        public readonly float GrabDuration;
        public readonly AnimKey VictimForcedAnimKey;
        public readonly bool GuaranteedReaction;
        public readonly AttackReactionData ReactionData;
        public readonly HitRequestType RequestType;
        public bool IsSpecialBreak => RequestType == HitRequestType.SpecialBreak;

        public HitContext(in HitRequest request, GameActor victim)
        {
            Attacker = request.Attacker;
            Victim = victim;
            AnimKey = request.AnimKey;
            HitPhaseIndex = request.HitPhaseIndex;
            AttackKind = request.AttackKind;
            ReactionType = request.ReactionType;
            DefenseType = request.DefenseType;
            Damage = request.Damage;
            PoiseDamage = request.PoiseDamage;
            BreakDamage = request.BreakDamage;
            ReactionDuration = request.ReactionDuration;
            ForceReaction = request.ForceReaction;
            ForceBreakExpose = request.ForceBreakExpose;
            CriticalMultiplier = request.CriticalMultiplier;
            IsCounterAttack = request.IsCounterAttack;
            UseCounterHitFeedback = request.UseCounterHitFeedback;
            IsProjectile = request.IsProjectile;
            HitPoint = request.HitPoint;
            AttackDirection = request.AttackDirection;
            HitTarget = request.HitTarget;
            HitParticleName = request.HitParticleName;
            PullForce = request.PullForce;
            AirborneForce = request.AirborneForce;
            KnockbackForce = request.KnockbackForce;
            KnockbackDrag = request.KnockbackDrag;
            GrabDuration = request.GrabDuration;
            VictimForcedAnimKey = request.VictimForcedAnimKey;
            GuaranteedReaction = request.GuaranteedReaction;
            ReactionData = request.ReactionData;
            RequestType = request.RequestType;
        }

        public static HitContext Create(in HitRequest request, GameActor victim)
            => new HitContext(request, victim);
    }
}
