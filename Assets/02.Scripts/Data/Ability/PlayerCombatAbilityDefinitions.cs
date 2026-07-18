using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Gameplay.Tag;

namespace UPlayGround.Data.Ability
{
    public enum PlayerCombatAbilitySlot
    {
        LightCombo,
        HeavyCombo,
        JumpCombo,
        DashAttack,
        CounterAttack,
        ParryCounterAttack,
        EntryAttack,
        EntryAttackVsGroggy,
        EntryAttackVsAirborne,
        SwapEvadeCounterAttack,
        SwapSpecialAttack,
    }

    [Serializable]
    public sealed class PlayerCombatAbilityBinding
    {
        public PlayerCombatAbilitySlot slot;
        [Tooltip("콤보 슬롯은 실행 순서, 단일 슬롯은 첫 번째 항목을 사용합니다.")]
        public List<GameplayAbilitySO> abilities = new();
    }

    [Serializable]
    public sealed class PlayerChargeAbilitySettings
    {
        public List<GameplayAbilitySO> stages = new();
        public List<float> stageThresholds = new();
        public PlayerInterruptAction interruptActions = PlayerInterruptAction.Dodge;
        public string fullChargeVfxKey;
        public ActorSocketType fullChargeVfxSocket = ActorSocketType.Center;
        public Vector3 fullChargeVfxOffset;
    }

    [Serializable]
    public sealed class AbilityComboRouteDefinition
    {
        public string routeId = "Route";
        public string displayName;
        public List<ComboInputToken> inputPattern = new();
        public ComboMatchMode matchMode = ComboMatchMode.Suffix;
        public List<GameplayTagId> requiredTagIds = new();
        public List<GameplayTagId> blockedTagIds = new();
        public RouteGroundCondition groundCondition = RouteGroundCondition.Any;
        public int skillGaugeIndex = -1;
        public GameplayAbilitySO ability;
        public int priority;
        [Min(0f)] public float perfectWindow;
        public GameplayAbilitySO enhancedAbility;
        [Min(0f)] public float enhancedDamageMultiplier = 1.15f;
        [Min(0f)] public float enhancedPoiseMultiplier = 1.15f;
        public GameplayTagId enhancedGrantTagId = GameplayTagId.None;

        public string DisplayLabel =>
            string.IsNullOrWhiteSpace(displayName) ? routeId : displayName;
        public bool IsEmpty => inputPattern == null || inputPattern.Count == 0;
        public bool HasPerfectWindow => perfectWindow > 0f;
    }
}
