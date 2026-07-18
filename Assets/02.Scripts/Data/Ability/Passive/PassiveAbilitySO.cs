using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Data.Ability
{
    [CreateAssetMenu(
        fileName = "PA_",
        menuName = "UPlayGround/Ability/Passive Ability")]
    public sealed class PassiveAbilitySO : ScriptableObject
    {
        public string passiveId;
        [Min(1)] public int schemaVersion = 1;
        public AbilityPresentationDefinition presentation = new()
        {
            category = AbilityCategory.Passive,
        };
        [TextArea]
        [Tooltip("UI_CharacterSelect에 표시할 수치 없는 요약 설명.")]
        public string characterSelectDescription;
        public PassiveActivationType activationType;
        public PassiveScope scope = PassiveScope.ActiveCharacter;
        public PassiveStackPolicy stackPolicy = PassiveStackPolicy.Additive;
        public List<PassiveModifierDefinition> modifiers = new();
        public List<GameplayEffectSO> triggeredEffects = new();

        public string CharacterSelectDescription =>
            string.IsNullOrWhiteSpace(characterSelectDescription)
                ? "캐릭터 고유 패시브."
                : characterSelectDescription.Trim();
    }
}
