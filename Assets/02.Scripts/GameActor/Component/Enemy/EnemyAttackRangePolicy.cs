using UPlayGround.Ability.UPlayGround;
using UPlayGround.Data;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UnityEngine;

namespace UPlayGround.Components
{
    /// <summary>
    /// 적 Ability의 정적 사거리 커버리지를 판정한다.
    /// 쿨다운, 비용, 현재 실행 충돌처럼 시간이 지나면 해소되는 준비 상태는 의도적으로 검사하지 않는다.
    /// </summary>
    public static class EnemyAttackRangePolicy
    {
        public static bool HasAttackInRange(
            AbilitySetSO abilitySet,
            float distanceToTarget,
            int currentLevel,
            AbilityAttackCategory attackCategory = AbilityAttackCategory.None,
            bool aerialOnly = false,
            bool diveOnly = false)
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
                            diveOnly))
                        return true;
                }
            }

            return false;
        }

        public static bool CoversDistance(
            GameplayAbilitySO ability,
            AbilityAttackInfo attackInfo,
            float distanceToTarget,
            int currentLevel,
            AbilityAttackCategory attackCategory = AbilityAttackCategory.None,
            bool aerialOnly = false,
            bool diveOnly = false)
        {
            if (ability?.activation == null
                || !EnemyAbilitySelectionPolicy.IsAISelectableAttack(attackInfo)
                || !attackInfo.IsUnlockedForLevel(currentLevel)
                || attackInfo.isAerialSkill != aerialOnly)
                return false;

            if (diveOnly && !attackInfo.isDiveAttack
                || !diveOnly && aerialOnly && attackInfo.isDiveAttack)
                return false;

            if (attackCategory != AbilityAttackCategory.None
                && attackInfo.attackCategory != attackCategory
                && attackInfo.attackCategory != AbilityAttackCategory.None)
                return false;

            if (!aerialOnly && ability.activation.groundCondition == AbilityGroundCondition.Airborne)
                return false;

            float distance = Mathf.Max(0f, distanceToTarget);
            float minDistance = Mathf.Max(0f, ability.activation.minDistance);
            float maxDistance = ability.activation.maxDistance;
            if (distance < minDistance || maxDistance > 0f && distance > maxDistance)
                return false;

            return MatchesStaticRangeConditions(attackInfo.conditionGroup, distance);
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
