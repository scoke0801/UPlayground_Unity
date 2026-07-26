using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UnityEngine;

namespace UPlayGround.Components
{
    /// <summary>플레이어 공격 정의를 런타임 공격 데이터로 변환하고 복제하는 책임.</summary>
    public sealed class PlayerAttackController
    {
        public AttackData Create(AbilityAttackInfo attackInfo, AttackKind attackKind)
            => CreateFromAbility(attackInfo, attackKind, 0);

        public static AttackData CreateFromAbility(
            AbilityAttackInfo attackInfo,
            AttackKind attackKind,
            int hitPhaseIndex)
        {
            if (attackInfo?.baseInfo == null)
                return null;

            var data = new AttackData
            {
                interruptActions = attackInfo.interruptActions,
                moveCancelDelayAfterLastHit = Mathf.Max(0f, attackInfo.moveCancelDelayAfterLastHit),
                attackKind = attackKind,
                defenseType = attackInfo.defenseType,
                criticalMultiplier = 1f,
            };

            ApplyHitPhase(data, attackInfo.baseInfo.GetHitPhase(hitPhaseIndex), hitPhaseIndex);
            return data;
        }

        public static void ApplyHitPhase(AttackData data, HitPhaseData phase, int hitPhaseIndex)
        {
            if (data == null || phase == null)
                return;

            data.hitPhaseIndex = hitPhaseIndex;
            data.damage = UPlayGround.Util.ApplyRandomValue(phase.damage, -0.2f, 0.2f)
                          * data.damageMultiplier;
            data.poiseDamage = phase.poiseDamage * data.poiseMultiplier;
            data.breakDamage = phase.breakDamage
                               * data.poiseMultiplier
                               * data.breakDamageMultiplier;
            data.reactionDuration = phase.reactionDuration;
            data.forceReaction = phase.forceReaction;
            data.forceBreakExpose = phase.forceBreakExpose;
            data.reactionType = phase.reactionType;
            data.hitParticleName = phase.hitParticleName;
            data.pullForce = phase.pullForce;
            data.airborneForce = phase.airborneForce;
            data.knockbackForce = phase.knockBackForce;
            data.knockbackDrag = phase.knockBackDrag;
            data.grabDuration = phase.grabDuration;
            data.victimForcedMotionSlot = phase.victimForcedMotionSlot;
            data.guaranteedReaction = phase.guaranteedReaction;
            data.reactionData = phase.reactionProfile?.Resolve();
        }

        public static AttackData Copy(AttackData source)
        {
            if (source == null)
                return null;

            return new AttackData
            {
                motionAsset = source.motionAsset,
                damage = source.damage,
                poiseDamage = source.poiseDamage,
                breakDamage = source.breakDamage,
                damageMultiplier = source.damageMultiplier,
                poiseMultiplier = source.poiseMultiplier,
                breakDamageMultiplier = source.breakDamageMultiplier,
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
                isReflectableProjectile = source.isReflectableProjectile,
                attackDirection = source.attackDirection,
                hitParticleName = source.hitParticleName,
                defenseType = source.defenseType,
                pullForce = source.pullForce,
                airborneForce = source.airborneForce,
                knockbackForce = source.knockbackForce,
                knockbackDrag = source.knockbackDrag,
                grabDuration = source.grabDuration,
                victimForcedMotionSlot = source.victimForcedMotionSlot,
                guaranteedReaction = source.guaranteedReaction,
                hitPhaseIndex = source.hitPhaseIndex,
                reactionData = source.reactionData,
            };
        }
    }
}
