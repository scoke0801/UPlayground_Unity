using System;
using UnityEngine;

namespace UPlayGround.Ability.Core
{
    public enum AbilityActivationResult
    {
        Success,
        InvalidDefinition,
        NotGranted,
        Locked,
        MissingRequiredTag,
        BlockedByTag,
        InvalidGroundState,
        InvalidTarget,
        OutOfRange,
        InsufficientResource,
        CooldownActive,
        ConflictingAbility,
        StateTransitionRejected,
        MissingExecutionData,
        PreparedExecutionExpired,
        AlreadyCommitted,
    }

    public enum AbilityExecutionState
    {
        Created,
        Prepared,
        Active,
        Ended,
        Cancelled,
        Aborted,
    }

    public readonly struct AbilityExecutionHandle : IEquatable<AbilityExecutionHandle>
    {
        public ulong Value { get; }
        public bool IsValid => Value != 0;

        public AbilityExecutionHandle(ulong value) => Value = value;

        public bool Equals(AbilityExecutionHandle other) => Value == other.Value;
        public override bool Equals(object obj) =>
            obj is AbilityExecutionHandle other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
    }

    public readonly struct AbilitySlotViewState
    {
        public readonly string AbilityId;
        public readonly bool IsGranted;
        public readonly bool IsUnlocked;
        public readonly bool IsReady;
        public readonly AbilityActivationResult BlockReason;
        public readonly float ResourceCurrent;
        public readonly float ResourceRequired;
        public readonly float CooldownRemaining;
        public readonly float CooldownDuration;
        public readonly string ResolvedVariantId;

        public AbilitySlotViewState(
            string abilityId,
            bool isGranted,
            bool isUnlocked,
            bool isReady,
            AbilityActivationResult blockReason,
            float resourceCurrent,
            float resourceRequired,
            float cooldownRemaining,
            float cooldownDuration,
            string resolvedVariantId)
        {
            AbilityId = abilityId;
            IsGranted = isGranted;
            IsUnlocked = isUnlocked;
            IsReady = isReady;
            BlockReason = blockReason;
            ResourceCurrent = resourceCurrent;
            ResourceRequired = resourceRequired;
            CooldownRemaining = cooldownRemaining;
            CooldownDuration = cooldownDuration;
            ResolvedVariantId = resolvedVariantId;
        }
    }

    /// <summary>
    /// 프로젝트 전용 애니메이션, 공격 정보 등을 Core 정의에 연결하는 확장 지점.
    /// Core는 구체 Payload 타입을 알지 않는다.
    /// Payload 식별은 Variant의 직접 참조로 해결하므로 별도 실행 ID를 두지 않는다.
    /// </summary>
    public abstract class AbilityExecutionPayloadSO : ScriptableObject
    {
    }
}
