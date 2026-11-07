using System;

namespace UPlayGround.Ability.Core
{
    public enum AbilityEffectStackPolicy
    {
        RejectNew,
        RefreshDuration,
        AddStackAndRefresh,
        ReplaceExisting,
    }

    public enum AbilityEffectStackAction
    {
        KeepExisting,
        RefreshExisting,
        ReplaceExisting,
    }

    public readonly struct AbilityEffectStackResult
    {
        public AbilityEffectStackAction Action { get; }
        public int StackCount { get; }

        public AbilityEffectStackResult(
            AbilityEffectStackAction action,
            int stackCount)
        {
            Action = action;
            StackCount = stackCount;
        }
    }

    /// <summary>프로젝트 객체 없이 Effect 중첩 정책만 결정한다.</summary>
    public static class AbilityEffectStackRuntime
    {
        public static AbilityEffectStackResult Resolve(
            AbilityEffectStackPolicy policy,
            int currentStackCount,
            int maxStackCount)
        {
            int current = Math.Max(1, currentStackCount);
            int maximum = Math.Max(1, maxStackCount);
            return policy switch
            {
                AbilityEffectStackPolicy.RejectNew =>
                    new AbilityEffectStackResult(
                        AbilityEffectStackAction.KeepExisting, current),
                AbilityEffectStackPolicy.RefreshDuration =>
                    new AbilityEffectStackResult(
                        AbilityEffectStackAction.RefreshExisting, current),
                AbilityEffectStackPolicy.AddStackAndRefresh =>
                    new AbilityEffectStackResult(
                        AbilityEffectStackAction.RefreshExisting,
                        Math.Min(current + 1, maximum)),
                AbilityEffectStackPolicy.ReplaceExisting =>
                    new AbilityEffectStackResult(
                        AbilityEffectStackAction.ReplaceExisting, 1),
                _ => throw new ArgumentOutOfRangeException(nameof(policy), policy, null),
            };
        }
    }
}
