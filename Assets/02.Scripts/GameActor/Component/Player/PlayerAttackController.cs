using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UnityEngine;

namespace UPlayGround.Components
{
    /// <summary>플레이어 공격 정의를 런타임 공격 데이터로 변환하고 복제하는 책임.</summary>
    public sealed class PlayerAttackController
    {
        public AttackData Create(PlayerAttackInfo attackInfo, AttackKind attackKind)
        {
            if (attackInfo?.baseInfo == null)
                return null;

            HitPhaseData phase = attackInfo.baseInfo.GetHitPhase(0);
            return new AttackData
            {
                animKey = attackInfo.baseInfo.animKey,
                damage = UPlayGround.Util.ApplyRandomValue(phase.damage, -0.2f, 0.2f),
                poiseDamage = phase.poiseDamage,
                breakDamage = phase.breakDamage,
                reactionDuration = phase.reactionDuration,
                forceReaction = phase.forceReaction,
                forceBreakExpose = phase.forceBreakExpose,
                interruptActions = attackInfo.interruptActions,
                moveCancelDelayAfterLastHit = Mathf.Max(0f, attackInfo.moveCancelDelayAfterLastHit),
                reactionType = phase.reactionType,
                hitParticleName = phase.hitParticleName,
                pullForce = phase.pullForce,
                knockbackForce = phase.knockBackForce,
                knockbackDrag = phase.knockBackDrag,
                airborneForce = phase.airborneForce,
                hitPhaseIndex = 0,
                attackKind = attackKind,
                victimForcedAnimKey = phase.victimForcedAnimKey,
                guaranteedReaction = phase.guaranteedReaction,
                reactionData = phase.reactionProfile?.Resolve(),
            };
        }

        public static AttackData Copy(AttackData source)
        {
            if (source == null)
                return null;

            return new AttackData
            {
                animKey = source.animKey,
                damage = source.damage,
                poiseDamage = source.poiseDamage,
                breakDamage = source.breakDamage,
                damageMultiplier = source.damageMultiplier,
                poiseMultiplier = source.poiseMultiplier,
                reactionDuration = source.reactionDuration,
                forceReaction = source.forceReaction,
                forceBreakExpose = source.forceBreakExpose,
                interruptActions = source.interruptActions,
                moveCancelDelayAfterLastHit = source.moveCancelDelayAfterLastHit,
                attackKind = source.attackKind,
                reactionType = source.reactionType,
                attacker = source.attacker,
                hitPoint = source.hitPoint,
                hitTarget = source.hitTarget,
                criticalMultiplier = source.criticalMultiplier,
                isCounterAttack = source.isCounterAttack,
                useCounterHitFeedback = source.useCounterHitFeedback,
                isProjectile = source.isProjectile,
                attackDirection = source.attackDirection,
                hitParticleName = source.hitParticleName,
                defenseType = source.defenseType,
                pullForce = source.pullForce,
                airborneForce = source.airborneForce,
                knockbackForce = source.knockbackForce,
                knockbackDrag = source.knockbackDrag,
                grabDuration = source.grabDuration,
                victimForcedAnimKey = source.victimForcedAnimKey,
                guaranteedReaction = source.guaranteedReaction,
                hitPhaseIndex = source.hitPhaseIndex,
                reactionData = source.reactionData,
            };
        }
    }
}
