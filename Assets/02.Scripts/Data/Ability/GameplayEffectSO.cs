using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Gameplay.Tag;

namespace UPlayGround.Data.Ability
{
    [CreateAssetMenu(fileName = "GE_", menuName = "UPlayGround/Ability/Gameplay Effect")]
    public sealed class GameplayEffectSO : ScriptableObject
    {
        public string effectId;
        [Min(1)] public int schemaVersion = 1;
        public GameplayEffectPolarity polarity;
        [Tooltip("패시브의 상태강화/상태이상 지속시간 배율을 적용하지 않습니다.")]
        public bool ignorePassiveDurationModifiers;
        public GameplayEffectDurationType durationType = GameplayEffectDurationType.Instant;
        [Min(0f)] public float durationSeconds;
        [Min(0f)] public float periodSeconds;
        public string stackingKey;
        public GameplayEffectStackPolicy stackPolicy = GameplayEffectStackPolicy.RejectNew;
        [Min(1)] public int maxStackCount = 1;
        public List<GameplayEffectModifierDefinition> modifiers = new();
        public List<GameplayResourceOperation> resourceOperations = new();
        public List<GameplayTagId> grantedTagIds = new();
        public GameplayEffectRemovalPolicy removalPolicy = GameplayEffectRemovalPolicy.RemoveOnSwap;
        public GameplayEffectSavePolicy savePolicy = GameplayEffectSavePolicy.DoNotSave;

        public bool IsPeriodic => periodSeconds > 0f;
        public string EffectiveStackingKey =>
            string.IsNullOrWhiteSpace(stackingKey) ? effectId : stackingKey.Trim();
    }
}
