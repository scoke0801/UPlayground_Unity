using UPlayGround.Data;
using UPlayGround.Ability.Core;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Combat;
using UnityEngine;
using UPlayGround.Gameplay.Tag;
using UPlayGround.Animation;
using UPlayGround.Data.Stat;

namespace UPlayGround.Combat
{
    public enum HitRequestType
    {
        Standard,
        SpecialBreak,
    }

    /// <summary>
    /// 공격자 영역에서 피격자 영역으로 전달되는 불변 입력 값.
    /// 가변 런타임 데이터인 AttackData를 피격 경계에서 복사해 이후 처리 중 값이 바뀌지 않게 한다.
    /// </summary>
    public readonly struct HitRequest
    {
        public readonly GameActor Attacker;
        public readonly MotionSetAsset MotionAsset;
        public readonly string AbilityId;
        public readonly string AbilityVariantId;
        public readonly string MotionKey;
        public readonly int HitPhaseIndex;
        public readonly AttackKind AttackKind;
        public readonly AttackReactionType ReactionType;
        public readonly AttackDefenseType DefenseType;
        public readonly float Damage;
        public readonly float PoiseDamage;
        public readonly float BreakDamage;
        public readonly float ReactionDuration;
        public readonly bool ForceReaction;
        public readonly bool ForceBreakExpose;
        public readonly float CriticalMultiplier;
        public readonly bool IsCounterAttack;
        public readonly bool UseCounterHitFeedback;
        public readonly bool IsProjectile;
        public readonly bool IsReflectableProjectile;
        public readonly Vector3 HitPoint;
        public readonly Vector3 AttackDirection;
        public readonly GameObject HitTarget;
        public readonly string HitParticleName;
        public readonly float PullForce;
        public readonly float AirborneForce;
        public readonly float KnockbackForce;
        public readonly float KnockbackDrag;
        public readonly float GrabDuration;
        public readonly GameplayTag VictimForcedMotionSlot;
        public readonly bool GuaranteedReaction;
        public readonly AttackReactionData ReactionData;
        public readonly HitRequestType RequestType;
        public readonly float SpecialDamageByMaxHpRate;
        public readonly float SpecialFixedDamage;
        public readonly float SpecialMinReferenceHealth;
        public readonly CombatTargetPolicy TargetPolicy;

        public bool IsSpecialBreak => RequestType == HitRequestType.SpecialBreak;

        public HitRequest(
            GameActor attacker,
            MotionSetAsset motionAsset,
            int hitPhaseIndex,
            AttackKind attackKind,
            AttackReactionType reactionType,
            AttackDefenseType defenseType,
            float damage,
            float poiseDamage,
            float breakDamage,
            float reactionDuration,
            bool forceReaction,
            bool forceBreakExpose,
            float criticalMultiplier,
            bool isCounterAttack,
            bool useCounterHitFeedback,
            bool isProjectile,
            Vector3 hitPoint,
            Vector3 attackDirection,
            GameObject hitTarget,
            string hitParticleName,
            float pullForce,
            float airborneForce,
            float knockbackForce,
            float knockbackDrag,
            float grabDuration,
            GameplayTag victimForcedMotionSlot,
            bool guaranteedReaction,
            AttackReactionData reactionData,
            HitRequestType requestType = HitRequestType.Standard,
            float specialDamageByMaxHpRate = 0f,
            float specialFixedDamage = 0f,
            float specialMinReferenceHealth = 0f,
            bool isReflectableProjectile = false,
            string abilityId = null,
            string abilityVariantId = null,
            string motionKey = null,
            CombatTargetPolicy targetPolicy = CombatTargetPolicy.Hostile)
        {
            Attacker = attacker;
            MotionAsset = motionAsset;
            AbilityId = abilityId;
            AbilityVariantId = abilityVariantId;
            MotionKey = motionKey;
            HitPhaseIndex = hitPhaseIndex;
            AttackKind = attackKind;
            ReactionType = reactionType;
            DefenseType = defenseType;
            Damage = damage;
            PoiseDamage = poiseDamage;
            BreakDamage = breakDamage;
            ReactionDuration = reactionDuration;
            ForceReaction = forceReaction;
            ForceBreakExpose = forceBreakExpose;
            CriticalMultiplier = criticalMultiplier;
            IsCounterAttack = isCounterAttack;
            UseCounterHitFeedback = useCounterHitFeedback;
            IsProjectile = isProjectile;
            IsReflectableProjectile = isReflectableProjectile;
            HitPoint = hitPoint;
            AttackDirection = attackDirection;
            HitTarget = hitTarget;
            HitParticleName = hitParticleName;
            PullForce = pullForce;
            AirborneForce = airborneForce;
            KnockbackForce = knockbackForce;
            KnockbackDrag = knockbackDrag;
            GrabDuration = grabDuration;
            VictimForcedMotionSlot = victimForcedMotionSlot;
            GuaranteedReaction = guaranteedReaction;
            ReactionData = reactionData;
            RequestType = requestType;
            SpecialDamageByMaxHpRate = specialDamageByMaxHpRate;
            SpecialFixedDamage = specialFixedDamage;
            SpecialMinReferenceHealth = specialMinReferenceHealth;
            TargetPolicy = targetPolicy;
        }

        public static HitRequest FromAttackData(AttackData data)
        {
            if (data == null)
                return default;

            float criticalMultiplier = ResolveCriticalMultiplier(data);

            return new HitRequest(
                data.attacker,
                data.motionAsset,
                data.hitPhaseIndex,
                data.attackKind,
                data.reactionType,
                data.defenseType,
                data.damage,
                data.poiseDamage,
                data.breakDamage,
                data.reactionDuration,
                data.forceReaction,
                data.forceBreakExpose,
                criticalMultiplier,
                data.isCounterAttack,
                data.useCounterHitFeedback,
                data.isProjectile,
                data.hitPoint,
                data.attackDirection,
                data.hitTarget,
                data.hitParticleName,
                data.pullForce,
                data.airborneForce,
                data.knockbackForce,
                data.knockbackDrag,
                data.grabDuration,
                data.victimForcedMotionSlot,
                data.guaranteedReaction,
                data.reactionData,
                isReflectableProjectile: data.isReflectableProjectile,
                abilityId: data.abilityId,
                abilityVariantId: data.abilityVariantId,
                motionKey: data.motionKey);
        }

        private static float ResolveCriticalMultiplier(AttackData data)
        {
            if (data == null)
                return 1f;

            // AttackData가 이미 1보다 큰 배율을 들고 있으면 스킬/특수공격이 강제한 치명타로 본다.
            if (data.criticalMultiplier > 1f)
                return data.criticalMultiplier;

            GameActor attacker = data.attacker;
            if (attacker?.AbilitySystem == null)
                return 1f;

            attacker.AbilitySystem.TryGetAttribute(
                global::UPlayGround.Data.Stat.Attributes.Combat.CritRate,
                current: true,
                out float rawCritRate);
            float critRate = Mathf.Clamp01(rawCritRate);
            if (critRate <= 0f)
                return 1f;

            if (Random.value > critRate)
                return 1f;

            return attacker.AbilitySystem.TryGetAttribute(
                global::UPlayGround.Data.Stat.Attributes.Combat.CritMultiplier,
                current: true,
                out float multiplier)
                ? Mathf.Max(1f, multiplier)
                : 1f;
        }

        public static HitRequest CreateSpecialBreak(
            GameActor attacker,
            MonsterActor victim,
            float damageByMaxHpRate,
            float fixedDamage,
            float minReferenceHealth,
            Vector3 hitPoint)
        {
            float effectiveHealth = Mathf.Max(
                victim != null ? victim.MaxHealth : 0f,
                Mathf.Max(0f, minReferenceHealth));
            float requestedDamage = Mathf.Max(0f, fixedDamage)
                                    + effectiveHealth * Mathf.Max(0f, damageByMaxHpRate);
            return new HitRequest(
                attacker,
                default,
                0,
                AttackKind.FinishAttack,
                AttackReactionType.Knockdown,
                AttackDefenseType.Unblockable,
                requestedDamage,
                0f,
                0f,
                0f,
                false,
                false,
                1f,
                false,
                false,
                false,
                hitPoint,
                Vector3.zero,
                victim != null ? victim.gameObject : null,
                null,
                0f,
                0f,
                0f,
                0f,
                0f,
                default,
                true,
                null,
                HitRequestType.SpecialBreak,
                damageByMaxHpRate,
                fixedDamage,
                minReferenceHealth);
        }

        /// <summary>MotionEvent 기반 피니시 공격을 치명 피해 정책에 전달할 불변 문맥으로 만든다.</summary>
        public static HitRequest CreateFinishAttack(
            GameActor attacker,
            MonsterActor victim,
            Vector3 attackDirection)
        {
            return new HitRequest(
                attacker,
                default,
                0,
                AttackKind.FinishAttack,
                AttackReactionType.Knockdown,
                AttackDefenseType.Unblockable,
                victim != null ? victim.CurrentHealth : 0f,
                0f,
                0f,
                0f,
                false,
                false,
                1f,
                false,
                false,
                false,
                victim != null ? victim.transform.position : Vector3.zero,
                attackDirection,
                victim != null ? victim.gameObject : null,
                null,
                0f,
                0f,
                0f,
                0f,
                0f,
                default,
                true,
                null);
        }

        /// <summary>
        /// 상태/애니메이션 계층의 기존 API를 위한 변환이다.
        /// 전투 판정과 계산에는 이 객체를 사용하지 않는다.
        /// </summary>
        public AttackData ToReactionData()
        {
            return new AttackData
            {
                attacker = Attacker,
                motionAsset = MotionAsset,
                abilityId = AbilityId,
                abilityVariantId = AbilityVariantId,
                motionKey = MotionKey,
                hitPhaseIndex = HitPhaseIndex,
                attackKind = AttackKind,
                reactionType = ReactionType,
                defenseType = DefenseType,
                damage = Damage,
                poiseDamage = PoiseDamage,
                breakDamage = BreakDamage,
                reactionDuration = ReactionDuration,
                forceReaction = ForceReaction,
                forceBreakExpose = ForceBreakExpose,
                criticalMultiplier = CriticalMultiplier,
                isCounterAttack = IsCounterAttack,
                useCounterHitFeedback = UseCounterHitFeedback,
                isProjectile = IsProjectile,
                isReflectableProjectile = IsReflectableProjectile,
                hitPoint = HitPoint,
                attackDirection = AttackDirection,
                hitTarget = HitTarget,
                hitParticleName = HitParticleName,
                pullForce = PullForce,
                airborneForce = AirborneForce,
                knockbackForce = KnockbackForce,
                knockbackDrag = KnockbackDrag,
                grabDuration = GrabDuration,
                victimForcedMotionSlot = VictimForcedMotionSlot,
                guaranteedReaction = GuaranteedReaction,
                reactionData = ReactionData,
            };
        }
    }
}
