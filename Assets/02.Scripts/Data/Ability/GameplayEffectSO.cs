using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Combat;
using UPlayGround.Gameplay.Tag;

namespace UPlayGround.Data.Ability
{
    [CreateAssetMenu(fileName = "GE_", menuName = "UPlayGround/Ability/Gameplay Effect")]
    public sealed class GameplayEffectSO : ScriptableObject
    {
        public string effectId;
        [TextArea]
        [Tooltip("에디터 전용 메모. 입력하면 Ability Editor 목록에 함께 표시됩니다.")]
        public string editorMemo;
        public GameplayEffectPolarity polarity;
        public GameplayEffectPresentationDefinition presentation = new();
        [Header("속성 부여")]
        [Tooltip("Duration/Infinite Effect가 활성화된 동안 부여할 전투 속성. None이면 속성을 변경하지 않습니다.")]
        public CombatElement grantedElement = CombatElement.None;
        [Tooltip("여러 속성 Effect가 겹치면 높은 우선순위가 적용됩니다. 같으면 나중에 적용된 Effect가 우선합니다.")]
        public int elementPriority;
        [Tooltip("패시브의 상태강화/상태이상 지속시간 배율을 적용하지 않습니다.")]
        public bool ignorePassiveDurationModifiers;
        public GameplayEffectDurationType durationType = GameplayEffectDurationType.Instant;
        [Min(0f)] public float durationSeconds;
        [Min(0f)] public float periodSeconds;
        public string stackingKey;
        public GameplayEffectStackPolicy stackPolicy = GameplayEffectStackPolicy.RejectNew;
        [Min(1)] public int maxStackCount = 1;
        public List<GameplayEffectModifierDefinition> modifiers = new();
        public List<GameplayTag> grantedTagIds = new();
        public GameplayEffectRemovalPolicy removalPolicy = GameplayEffectRemovalPolicy.RemoveOnSwap;
        public GameplayEffectSavePolicy savePolicy = GameplayEffectSavePolicy.DoNotSave;

        public bool IsPeriodic => periodSeconds > 0f;
        public string EffectiveStackingKey =>
            string.IsNullOrWhiteSpace(stackingKey) ? effectId : stackingKey.Trim();
    }
}
