using UPlayGround.Ability.UPlayGround;
using UPlayGround.Data;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UnityEngine;

namespace UPlayGround.Components
{
    public enum EnemyAttackDistanceRelation
    {
        Unavailable,
        TooClose,
        InRange,
        TooFar
    }

    /// <summary>
    /// 적 Ability의 정적 사거리 커버리지를 판정한다.
    /// 쿨다운, 비용, 현재 실행 충돌처럼 시간이 지나면 해소되는 준비 상태는 의도적으로 검사하지 않는다.
    /// </summary>
    public static class EnemyAttackRangePolicy
    {
        public const float DefaultPersonalSpaceDistance = 0.8f;
        private const float MeleeRangeSafetyMargin = 0.15f;
        private const float TargetSurfaceAllowance = 0.5f;

        public static bool HasAttackInRange(
            AbilitySetSO abilitySet,
            float distanceToTarget,
            int currentLevel,
            AbilityAttackCategory attackCategory = AbilityAttackCategory.None,
            bool aerialOnly = false,
            bool diveOnly = false,
            bool useMeleeApproachRange = false,
            float personalSpaceDistance = DefaultPersonalSpaceDistance,
            AbilityAIRole abilityRole = AbilityAIRole.None)
        {
            if (abilitySet == null)
                return false;

            foreach (GameplayAbilitySO ability in abilitySet.GetRuntimeAbilities())
            {
                if (ability?.variants == null)
                    continue;

                for (var variantIndex = 0; variantIndex < ability.variants.Count; variantIndex++)
                {
                    if (!UPlayGroundAbilityPayloadResolver.TryResolveAttackInfo(
                            ability.variants[variantIndex],
                            out AbilityAttackInfo attackInfo))
                        continue;

                    if (CoversDistance(
                            ability,
                            attackInfo,
                            distanceToTarget,
                            currentLevel,
                            attackCategory,
                            aerialOnly,
                            diveOnly,
                            useMeleeApproachRange,
                            personalSpaceDistance,
                            abilityRole))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 현재 거리가 정적 공격 범위의 어느 쪽에 있는지 판정한다.
        /// 여러 후보가 있으면 현재 실행 가능 범위를 우선하고, 접근해서 진입할 수 있는 후보가 하나라도 있으면
        /// <see cref="EnemyAttackDistanceRelation.TooFar"/>를 반환한다.
        /// </summary>
        public static EnemyAttackDistanceRelation EvaluateAttackDistance(
            AbilitySetSO abilitySet,
            float distanceToTarget,
            int currentLevel,
            AbilityAttackCategory attackCategory = AbilityAttackCategory.None,
            bool aerialOnly = false,
            bool diveOnly = false,
            bool useMeleeApproachRange = false,
            float personalSpaceDistance = DefaultPersonalSpaceDistance,
            AbilityAIRole abilityRole = AbilityAIRole.None)
        {
            if (abilitySet == null)
                return EnemyAttackDistanceRelation.Unavailable;

            bool hasTooFar = false;
            bool hasTooClose = false;
            foreach (GameplayAbilitySO ability in abilitySet.GetRuntimeAbilities())
            {
                if (ability?.variants == null)
                    continue;

                for (var variantIndex = 0; variantIndex < ability.variants.Count; variantIndex++)
                {
                    if (!UPlayGroundAbilityPayloadResolver.TryResolveAttackInfo(
                            ability.variants[variantIndex],
                            out AbilityAttackInfo attackInfo))
                        continue;

                    EnemyAttackDistanceRelation relation = EvaluateAttackDistance(
                        ability,
                        attackInfo,
                        distanceToTarget,
                        currentLevel,
                        attackCategory,
                        aerialOnly,
                        diveOnly,
                        useMeleeApproachRange,
                        personalSpaceDistance,
                        abilityRole);
                    if (relation == EnemyAttackDistanceRelation.InRange)
                        return relation;

                    hasTooFar |= relation == EnemyAttackDistanceRelation.TooFar;
                    hasTooClose |= relation == EnemyAttackDistanceRelation.TooClose;
                }
            }

            if (hasTooFar)
                return EnemyAttackDistanceRelation.TooFar;
            return hasTooClose
                ? EnemyAttackDistanceRelation.TooClose
                : EnemyAttackDistanceRelation.Unavailable;
        }

        public static EnemyAttackDistanceRelation EvaluateAttackDistance(
            GameplayAbilitySO ability,
            AbilityAttackInfo attackInfo,
            float distanceToTarget,
            int currentLevel,
            AbilityAttackCategory attackCategory = AbilityAttackCategory.None,
            bool aerialOnly = false,
            bool diveOnly = false,
            bool useMeleeApproachRange = false,
            float personalSpaceDistance = DefaultPersonalSpaceDistance,
            AbilityAIRole abilityRole = AbilityAIRole.None)
        {
            if (!IsStaticCandidate(
                    ability,
                    attackInfo,
                    currentLevel,
                    attackCategory,
                    aerialOnly,
                    diveOnly,
                    abilityRole))
                return EnemyAttackDistanceRelation.Unavailable;

            float distance = Mathf.Max(0f, distanceToTarget);
            float minDistance = Mathf.Max(0f, ability.activation.minDistance);
            float maxDistance = useMeleeApproachRange
                ? ResolveEffectiveMaxDistance(ability, attackInfo, personalSpaceDistance)
                : ability.activation.maxDistance;
            float upperBound = maxDistance > 0f ? maxDistance : float.PositiveInfinity;

            return EvaluateConditionDistance(
                attackInfo.conditionGroup,
                distance,
                minDistance,
                upperBound);
        }

        public static bool CoversDistance(
            GameplayAbilitySO ability,
            AbilityAttackInfo attackInfo,
            float distanceToTarget,
            int currentLevel,
            AbilityAttackCategory attackCategory = AbilityAttackCategory.None,
            bool aerialOnly = false,
            bool diveOnly = false,
            bool useMeleeApproachRange = false,
            float personalSpaceDistance = DefaultPersonalSpaceDistance,
            AbilityAIRole abilityRole = AbilityAIRole.None)
        {
            if (!IsStaticCandidate(
                    ability,
                    attackInfo,
                    currentLevel,
                    attackCategory,
                    aerialOnly,
                    diveOnly,
                    abilityRole))
                return false;

            float distance = Mathf.Max(0f, distanceToTarget);
            float minDistance = Mathf.Max(0f, ability.activation.minDistance);
            float maxDistance = useMeleeApproachRange
                ? ResolveEffectiveMaxDistance(ability, attackInfo, personalSpaceDistance)
                : ability.activation.maxDistance;
            if (distance < minDistance || (maxDistance > 0f && distance > maxDistance))
                return false;

            return MatchesStaticRangeConditions(attackInfo.conditionGroup, distance);
        }

        private static bool IsStaticCandidate(
            GameplayAbilitySO ability,
            AbilityAttackInfo attackInfo,
            int currentLevel,
            AbilityAttackCategory attackCategory,
            bool aerialOnly,
            bool diveOnly,
            AbilityAIRole abilityRole)
        {
            if (ability?.activation == null
                || !EnemyAbilitySelectionPolicy.IsAISelectableAttack(attackInfo)
                || !attackInfo.IsUnlockedForLevel(currentLevel)
                || attackInfo.isAerialSkill != aerialOnly)
                return false;

            if ((diveOnly && !attackInfo.isDiveAttack)
                || (!diveOnly && aerialOnly && attackInfo.isDiveAttack))
                return false;

            if (!EnemyAbilitySelectionPolicy.MatchesCategory(
                    attackInfo,
                    attackCategory)
                || !EnemyAbilitySelectionPolicy.MatchesRole(
                    attackInfo,
                    abilityRole))
                return false;

            return aerialOnly || ability.activation.groundCondition != AbilityGroundCondition.Airborne;
        }

        private static EnemyAttackDistanceRelation EvaluateConditionDistance(
            SkillConditionGroup conditionGroup,
            float distance,
            float activationMin,
            float activationMax)
        {
            if (conditionGroup?.conditions == null || conditionGroup.conditions.Count == 0)
                return EvaluateBounds(distance, activationMin, activationMax);

            bool hasRangeCondition = false;
            bool hasNonRangeCondition = false;
            float andMin = activationMin;
            float andMax = activationMax;
            bool orHasTooFar = false;
            bool orHasTooClose = false;

            for (var i = 0; i < conditionGroup.conditions.Count; i++)
            {
                SkillCondition condition = conditionGroup.conditions[i];
                if (condition == null || condition.type != ConditionType.RangeBased)
                {
                    hasNonRangeCondition = true;
                    continue;
                }

                hasRangeCondition = true;
                float conditionMin = Mathf.Min(condition.minRange, condition.maxRange);
                float conditionMax = Mathf.Max(condition.minRange, condition.maxRange);
                if (conditionGroup.conditionOperator == ConditionOperator.And)
                {
                    andMin = Mathf.Max(andMin, conditionMin);
                    andMax = Mathf.Min(andMax, conditionMax);
                    continue;
                }

                float orMin = Mathf.Max(activationMin, conditionMin);
                float orMax = Mathf.Min(activationMax, conditionMax);
                if (orMin > orMax)
                    continue;

                EnemyAttackDistanceRelation relation = EvaluateBounds(distance, orMin, orMax);
                if (relation == EnemyAttackDistanceRelation.InRange)
                    return relation;
                orHasTooFar |= relation == EnemyAttackDistanceRelation.TooFar;
                orHasTooClose |= relation == EnemyAttackDistanceRelation.TooClose;
            }

            if (!hasRangeCondition
                || (conditionGroup.conditionOperator == ConditionOperator.Or && hasNonRangeCondition))
                return EvaluateBounds(distance, activationMin, activationMax);

            if (conditionGroup.conditionOperator == ConditionOperator.And)
                return andMin <= andMax
                    ? EvaluateBounds(distance, andMin, andMax)
                    : EnemyAttackDistanceRelation.Unavailable;

            if (orHasTooFar)
                return EnemyAttackDistanceRelation.TooFar;
            return orHasTooClose
                ? EnemyAttackDistanceRelation.TooClose
                : EnemyAttackDistanceRelation.Unavailable;
        }

        private static EnemyAttackDistanceRelation EvaluateBounds(
            float distance,
            float minDistance,
            float maxDistance)
        {
            if (distance < minDistance)
                return EnemyAttackDistanceRelation.TooClose;
            if (distance > maxDistance)
                return EnemyAttackDistanceRelation.TooFar;
            return EnemyAttackDistanceRelation.InRange;
        }

        /// <summary>
        /// 근접 AI가 공격을 시작해도 되는 보수적인 피벗 간 거리.
        /// activation은 선택 게이트, HitPhase targetingRange는 공격 데이터가 아는 위협 반경,
        /// personalSpace는 대형 액터의 몸집 여유로 사용한다.
        /// </summary>
        public static float ResolveEffectiveMaxDistance(
            GameplayAbilitySO ability,
            AbilityAttackInfo attackInfo,
            float personalSpaceDistance)
        {
            if (ability?.activation == null || attackInfo?.baseInfo == null)
                return 0f;

            float authoredMax = ability.activation.maxDistance;
            if (attackInfo.baseInfo.attackType != AttackType.Melee)
                return authoredMax;

            float threatRange = 0f;
            if (attackInfo.baseInfo.hitPhases != null)
            {
                for (var i = 0; i < attackInfo.baseInfo.hitPhases.Count; i++)
                {
                    HitPhaseData phase = attackInfo.baseInfo.hitPhases[i];
                    if (phase != null)
                        threatRange = Mathf.Max(threatRange, phase.targetingRange);
                }
            }

            float dataDrivenApproach = Mathf.Max(
                Mathf.Max(0f, threatRange - MeleeRangeSafetyMargin),
                Mathf.Max(0f, personalSpaceDistance) + TargetSurfaceAllowance);
            if (dataDrivenApproach <= 0f)
                return authoredMax;

            // personalSpace 하한은 피벗이 물리적으로 더 접근하지 못하는 대형 액터를 위한 값이다.
            // 실제 포즈에서 베이크된 activation 최대 거리가 있으면 이를 절대 넘지 않는다.
            float effectiveMax = authoredMax > 0f
                ? Mathf.Min(authoredMax, dataDrivenApproach)
                : dataDrivenApproach;
            return Mathf.Max(Mathf.Max(0f, ability.activation.minDistance), effectiveMax);
        }

        private static bool MatchesStaticRangeConditions(
            SkillConditionGroup conditionGroup,
            float distanceToTarget)
        {
            if (conditionGroup?.conditions == null || conditionGroup.conditions.Count == 0)
                return true;

            bool hasRangeCondition = false;
            bool hasNonRangeCondition = false;
            bool anyRangeMatched = false;

            for (var i = 0; i < conditionGroup.conditions.Count; i++)
            {
                SkillCondition condition = conditionGroup.conditions[i];
                if (condition == null || condition.type != ConditionType.RangeBased)
                {
                    hasNonRangeCondition = true;
                    continue;
                }

                hasRangeCondition = true;
                bool matched = distanceToTarget >= Mathf.Min(condition.minRange, condition.maxRange)
                               && distanceToTarget <= Mathf.Max(condition.minRange, condition.maxRange);
                if (conditionGroup.conditionOperator == ConditionOperator.And && !matched)
                    return false;

                anyRangeMatched |= matched;
            }

            if (!hasRangeCondition || conditionGroup.conditionOperator == ConditionOperator.And)
                return true;

            // OR 그룹의 비거리 조건은 런타임 상태에 따라 참이 될 수 있으므로 정적 판정에서 막지 않는다.
            return hasNonRangeCondition || anyRangeMatched;
        }
    }
}
