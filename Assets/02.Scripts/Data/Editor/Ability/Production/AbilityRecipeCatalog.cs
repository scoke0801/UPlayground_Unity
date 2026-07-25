using System.Collections.Generic;

namespace UPlayGround.Data.Editor.Ability.Production
{
    public static class AbilityRecipeCatalog
    {
        public const string SharedMotionTaskGraphPath =
            "Assets/10.Datas/Ability/Migrated/TaskGraphs/"
            + "AbilityTaskGraph_MotionExecution.asset";

        private static AbilityRecipeDefinition MotionRecipe(
            string id,
            string name,
            AbilityProductionOwnerKind owner,
            UPlayGround.Data.EnumType.AbilityAttackCategory category,
            UPlayGround.Data.EnumType.AttackType attackType,
            bool aiSelectable,
            AbilitySetBindingMode bindingMode,
            bool supportsEffect = false,
            bool requiresEffect = false,
            UPlayGround.Data.Ability.AbilityTargetPolicy targetPolicy =
                UPlayGround.Data.Ability.AbilityTargetPolicy.Required,
            UPlayGround.Data.Ability.AbilityTargetRelation targetRelation =
                UPlayGround.Data.Ability.AbilityTargetRelation.Enemy,
            UPlayGround.Data.Ability.AbilityCategory abilityCategory =
                UPlayGround.Data.Ability.AbilityCategory.Attack) =>
            new()
            {
                RecipeId = id,
                DisplayName = name,
                Version = 1,
                OwnerKind = owner,
                Category = abilityCategory,
                TargetPolicy = targetPolicy,
                TargetRelation = targetRelation,
                GroundCondition = UPlayGround.Data.Ability.AbilityGroundCondition.Any,
                Concurrency = UPlayGround.Data.Ability.AbilityConcurrencyPolicy.RejectNew,
                AttackCategory = category,
                AttackType = attackType,
                AiSelectable = aiSelectable,
                DefaultSelectionWeight = 10f,
                DefaultTaskGraphPath = SharedMotionTaskGraphPath,
                BindingMode = bindingMode,
                SupportsEffect = supportsEffect,
                RequiresEffect = requiresEffect,
            };

        private static readonly AbilityRecipeDefinition PlayerBasicMeleeRecipe =
            MotionRecipe(
                "Player.Basic.Melee",
                "플레이어 기본 근접 공격",
                AbilityProductionOwnerKind.Player,
                UPlayGround.Data.EnumType.AbilityAttackCategory.Basic,
                UPlayGround.Data.EnumType.AttackType.Melee,
                false,
                AbilitySetBindingMode.PlayerCombatSequence,
                targetPolicy:
                    UPlayGround.Data.Ability.AbilityTargetPolicy.Optional);
        private static readonly AbilityRecipeDefinition PlayerSkillProjectileRecipe =
            MotionRecipe(
                "Player.Skill.Projectile",
                "플레이어 투사체 스킬",
                AbilityProductionOwnerKind.Player,
                UPlayGround.Data.EnumType.AbilityAttackCategory.Skill,
                UPlayGround.Data.EnumType.AttackType.Ranged,
                false,
                AbilitySetBindingMode.PlayerSkillSlot);
        private static readonly AbilityRecipeDefinition MonsterBasicMeleeRecipe =
            MotionRecipe(
                "Monster.Basic.Melee",
                "몬스터 기본 근접 공격",
                AbilityProductionOwnerKind.Monster,
                UPlayGround.Data.EnumType.AbilityAttackCategory.Basic,
                UPlayGround.Data.EnumType.AttackType.Melee,
                true,
                AbilitySetBindingMode.AdditionalAbilities);
        private static readonly AbilityRecipeDefinition MonsterHeavyTelegraphRecipe =
            MotionRecipe(
                "Monster.Heavy.Telegraph",
                "몬스터 강공격·전조",
                AbilityProductionOwnerKind.Monster,
                UPlayGround.Data.EnumType.AbilityAttackCategory.Heavy,
                UPlayGround.Data.EnumType.AttackType.Melee,
                true,
                AbilitySetBindingMode.AdditionalAbilities,
                targetPolicy:
                    UPlayGround.Data.Ability.AbilityTargetPolicy.Optional);
        private static readonly AbilityRecipeDefinition CombatAreaAttackRecipe =
            MotionRecipe(
                "Combat.AreaAttack",
                "범위 공격",
                AbilityProductionOwnerKind.Boss,
                UPlayGround.Data.EnumType.AbilityAttackCategory.Skill,
                UPlayGround.Data.EnumType.AttackType.Melee,
                true,
                AbilitySetBindingMode.AdditionalAbilities);
        private static readonly AbilityRecipeDefinition SupportHealOrBuffRecipe =
            MotionRecipe(
                "Support.HealOrBuff",
                "회복·버프 지원 스킬",
                AbilityProductionOwnerKind.Player,
                UPlayGround.Data.EnumType.AbilityAttackCategory.Skill,
                UPlayGround.Data.EnumType.AttackType.Melee,
                false,
                AbilitySetBindingMode.PlayerSkillSlot,
                supportsEffect: true,
                requiresEffect: true,
                targetPolicy:
                    UPlayGround.Data.Ability.AbilityTargetPolicy.Optional,
                targetRelation:
                    UPlayGround.Data.Ability.AbilityTargetRelation.Ally,
                abilityCategory:
                    UPlayGround.Data.Ability.AbilityCategory.Support);

        private static readonly IReadOnlyList<AbilityRecipeDefinition> AllRecipes =
            new[]
            {
                PlayerBasicMeleeRecipe,
                PlayerSkillProjectileRecipe,
                MonsterBasicMeleeRecipe,
                MonsterHeavyTelegraphRecipe,
                CombatAreaAttackRecipe,
                SupportHealOrBuffRecipe,
            };

        public static AbilityRecipeDefinition PlayerBasicMelee =>
            PlayerBasicMeleeRecipe;
        public static AbilityRecipeDefinition PlayerSkillProjectile =>
            PlayerSkillProjectileRecipe;
        public static AbilityRecipeDefinition MonsterBasicMelee =>
            MonsterBasicMeleeRecipe;
        public static AbilityRecipeDefinition MonsterHeavyTelegraph =>
            MonsterHeavyTelegraphRecipe;
        public static AbilityRecipeDefinition CombatAreaAttack =>
            CombatAreaAttackRecipe;
        public static AbilityRecipeDefinition SupportHealOrBuff =>
            SupportHealOrBuffRecipe;

        public static IReadOnlyList<AbilityRecipeDefinition> All => AllRecipes;
    }
}
