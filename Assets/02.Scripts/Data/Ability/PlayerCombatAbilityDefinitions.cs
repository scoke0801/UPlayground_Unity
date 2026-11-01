using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Gameplay.Tag;
using UPlayGround.Data.Actor.Animation;

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
        [Tooltip("이 모션에서 이어지는 후속 공격입니다. 비우면 입력 패턴만 검사합니다.")]
        public List<MotionKey> sourceMotionKeys = new();
        [Tooltip("직전 공격이 실제 피해를 준 경우에만 연결합니다.")]
        public bool requiresSourceHit;
        [Tooltip("훈련장에 표시할 용도: 압박 유지, 재진입, 강한 마무리 등")]
        public string purpose;
        public List<GameplayTag> requiredTagIds = new();
        public List<GameplayTag> blockedTagIds = new();
        public RouteGroundCondition groundCondition = RouteGroundCondition.Any;
        public int skillGaugeIndex = -1;
        public GameplayAbilitySO ability;
        public int priority;
        [Min(0f)] public float perfectWindow;
        public GameplayAbilitySO enhancedAbility;
        [Min(0f)] public float enhancedDamageMultiplier = 1.15f;
        [Min(0f)] public float enhancedPoiseMultiplier = 1.15f;
        public GameplayTag enhancedGrantTagId;

        public string DisplayLabel =>
            string.IsNullOrWhiteSpace(displayName) ? routeId : displayName;
        public bool IsEmpty => inputPattern == null || inputPattern.Count == 0;
        public bool HasPerfectWindow => perfectWindow > 0f;
    }
}
