using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Gameplay.Tag;

namespace UPlayGround.Data.Ability
{
    [CreateAssetMenu(fileName = "GA_", menuName = "UPlayGround/Ability/Gameplay Ability")]
    public sealed class GameplayAbilitySO : ScriptableObject
    {
        public string abilityId;
        [TextArea]
        [Tooltip("에디터 전용 메모. 입력하면 Ability Editor 목록에 함께 표시됩니다.")]
        public string editorMemo;
        public AbilityPresentationDefinition presentation = new();
        public List<GameplayTag> abilityTagIds = new();
        public List<AbilityTriggerDefinition> triggers = new();
        public List<GameplayTag> cancelAbilitiesWithTag = new();
        public List<GameplayTag> blockAbilitiesWithTag = new();
        public AbilityActivationRules activation = new();
        public AbilityTargetingDefinition targeting = new();
        public AbilityCostDefinition cost = new();
        public AbilityCooldownDefinition cooldown = new();
        public AbilityConcurrencyPolicy concurrency = AbilityConcurrencyPolicy.RejectNew;
        [Tooltip("다중 프레임 실행의 필수 Task Graph.")]
        public UPlayGround.Ability.Core.AbilityTaskGraphSO taskGraph;
        public List<AbilityVariantDefinition> variants = new();
        public List<GameplayEffectSO> commitEffects = new();
        public List<GameplayEffectSO> endEffects = new();
        public AbilityPersistencePolicy persistence = new();
        public AbilityBalanceMetadata balance = new();
    }
}
