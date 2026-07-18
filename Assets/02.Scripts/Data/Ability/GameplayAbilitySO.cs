using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Gameplay.Tag;

namespace UPlayGround.Data.Ability
{
    [CreateAssetMenu(fileName = "GA_", menuName = "UPlayGround/Ability/Gameplay Ability")]
    public sealed class GameplayAbilitySO : ScriptableObject
    {
        public string abilityId;
        [Min(1)] public int schemaVersion = 1;
        public AbilityPresentationDefinition presentation = new();
        public List<GameplayTagId> abilityTagIds = new();
        public AbilityActivationRules activation = new();
        public AbilityCostDefinition cost = new();
        public AbilityCooldownDefinition cooldown = new();
        public AbilityConcurrencyPolicy concurrency = AbilityConcurrencyPolicy.RejectNew;
        public List<AbilityVariantDefinition> variants = new();
        public List<GameplayEffectSO> commitEffects = new();
        public List<GameplayEffectSO> endEffects = new();
        public AbilityCueDefinition cues = new();
        public AbilityPersistencePolicy persistence = new();
        public AbilityBalanceMetadata balance = new();
    }
}
