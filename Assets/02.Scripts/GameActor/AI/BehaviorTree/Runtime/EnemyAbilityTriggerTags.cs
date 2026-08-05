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
            if (abilitySet == null || !TryGetAttackTag(category, out GameplayTag tag))
                return null;

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
                        return ability;
                }
            }
            return null;
        }
    }
}
