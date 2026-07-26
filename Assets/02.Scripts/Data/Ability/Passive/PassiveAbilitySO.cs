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
        [TextArea]
        [Tooltip("에디터 전용 메모. 입력하면 Ability Editor 목록에 함께 표시됩니다.")]
        public string editorMemo;
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
        [Header("Triggered Effect HUD")]
        [Tooltip("패시브 조건으로 발생한 Effect는 기본적으로 Effect 정의의 HUD 노출 설정을 따릅니다.")]
        public GameplayEffectHudVisibility triggeredEffectHudVisibility =
            GameplayEffectHudVisibility.UseDefinition;

        public string CharacterSelectDescription =>
            string.IsNullOrWhiteSpace(characterSelectDescription)
                ? "캐릭터 고유 패시브."
                : characterSelectDescription.Trim();
    }
}
