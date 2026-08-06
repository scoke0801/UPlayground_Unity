using UPlayGround.Data.Ability;
using UPlayGround.Data.EnumType;
using UPlayGround.Gameplay.Tag;

namespace UPlayGround.AI.BehaviorTree
{
    /// <summary>몬스터 공격 카테고리와 GameplayEvent 태그의 단일 매핑.</summary>
    public static class EnemyAbilityTriggerTags
    {
        public static bool TryGetAttackTag(
            AbilityAttackCategory category,
            out GameplayTag tag)
        {
            tag = category switch
            {
                AbilityAttackCategory.Basic => GameplayTags.Trigger_Monster_Attack_Basic,
                AbilityAttackCategory.Heavy => GameplayTags.Trigger_Monster_Attack_Heavy,
                AbilityAttackCategory.Skill => GameplayTags.Trigger_Monster_Attack_Skill,
                _ => default,
            };
            return tag.IsValid();
        }

        public static bool TryGetAttackCategory(
            GameplayTag tag,
            out AbilityAttackCategory category)
        {
            category = tag.TagName switch
            {
                "Trigger.Monster.Attack.Basic" => AbilityAttackCategory.Basic,
                "Trigger.Monster.Attack.Heavy" => AbilityAttackCategory.Heavy,
                "Trigger.Monster.Attack.Skill" => AbilityAttackCategory.Skill,
                _ => AbilityAttackCategory.None,
            };
            return category != AbilityAttackCategory.None;
        }

        public static GameplayAbilitySO FindTriggerAbility(
            AbilitySetSO abilitySet,
            AbilityAttackCategory category)
        {
            return TryResolveAttackTrigger(
                abilitySet,
                category,
                out _,
                out GameplayAbilitySO ability,
                out _)
                ? ability
                : null;
        }

        /// <summary>
        /// 명시적 카테고리는 해당 트리거만 해석하고, None은 기존 BT의
        /// "모든 공격 후보" 의미를 보존하기 위해 사용 가능한 라우터 하나를 선택한다.
        /// 선택한 카테고리는 트리거 운반 경로일 뿐 실제 공격 후보 필터로 사용하지 않는다.
        /// </summary>
        public static bool TryResolveAttackTrigger(
            AbilitySetSO abilitySet,
            AbilityAttackCategory requestedCategory,
            out AbilityAttackCategory resolvedCategory,
            out GameplayAbilitySO ability,
            out GameplayTag tag)
        {
            resolvedCategory = AbilityAttackCategory.None;
            ability = null;
            tag = default;
            if (abilitySet == null)
                return false;

            if (requestedCategory != AbilityAttackCategory.None)
                return TryResolveExactAttackTrigger(
                    abilitySet,
                    requestedCategory,
                    out resolvedCategory,
                    out ability,
                    out tag);

            return TryResolveExactAttackTrigger(
                       abilitySet,
                       AbilityAttackCategory.Basic,
                       out resolvedCategory,
                       out ability,
                       out tag)
                   || TryResolveExactAttackTrigger(
                       abilitySet,
                       AbilityAttackCategory.Heavy,
                       out resolvedCategory,
                       out ability,
                       out tag)
                   || TryResolveExactAttackTrigger(
                       abilitySet,
                       AbilityAttackCategory.Skill,
                       out resolvedCategory,
                       out ability,
                       out tag);
        }

        private static bool TryResolveExactAttackTrigger(
            AbilitySetSO abilitySet,
            AbilityAttackCategory category,
            out AbilityAttackCategory resolvedCategory,
            out GameplayAbilitySO resolvedAbility,
            out GameplayTag resolvedTag)
        {
            resolvedCategory = AbilityAttackCategory.None;
            resolvedAbility = null;
            resolvedTag = default;
            if (!TryGetAttackTag(category, out GameplayTag tag))
                return false;

            foreach (GameplayAbilitySO ability in abilitySet.GetRuntimeAbilities())
            {
                if (ability?.triggers == null)
                    continue;
                for (int i = 0; i < ability.triggers.Count; i++)
                {
                    AbilityTriggerDefinition trigger = ability.triggers[i];
                    if (trigger == null
                        || trigger.source != AbilityTriggerSource.GameplayEvent
                        || trigger.mode != AbilityTriggerActivationMode.Request)
                        continue;

                    bool matches = trigger.matchMode == AbilityTagMatchMode.Exact
                        ? trigger.triggerTag == tag
                        : tag.IsChildOf(trigger.triggerTag);
                    if (matches)
                    {
                        resolvedCategory = category;
                        resolvedAbility = ability;
                        resolvedTag = tag;
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
