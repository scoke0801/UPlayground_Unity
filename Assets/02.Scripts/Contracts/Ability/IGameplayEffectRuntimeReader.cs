using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Ability;

namespace UPlayGround.Contracts.Ability
{
    public readonly struct GameplayEffectViewState
    {
        public readonly ulong RuntimeId;
        public readonly string EffectId;
        public readonly string DisplayName;
        public readonly Sprite Icon;
        public readonly GameplayEffectPolarity Polarity;
        public readonly int HudPriority;
        public readonly int StackCount;
        public readonly float DurationSeconds;
        public readonly float RemainingSeconds;
        public readonly bool IsInfinite;
        public readonly bool ShowRemainingTime;
        public readonly bool ShowStackCount;

        public GameplayEffectViewState(
            ulong runtimeId,
            string effectId,
            string displayName,
            Sprite icon,
            GameplayEffectPolarity polarity,
            int hudPriority,
            int stackCount,
            float durationSeconds,
            float remainingSeconds,
            bool isInfinite,
            bool showRemainingTime,
            bool showStackCount)
        {
            RuntimeId = runtimeId;
            EffectId = effectId;
            DisplayName = displayName;
            Icon = icon;
            Polarity = polarity;
            HudPriority = hudPriority;
            StackCount = stackCount;
            DurationSeconds = durationSeconds;
            RemainingSeconds = remainingSeconds;
            IsInfinite = isInfinite;
            ShowRemainingTime = showRemainingTime;
            ShowStackCount = showStackCount;
        }
    }

    public interface IGameplayEffectRuntimeReader
    {
        event Action StateChanged;

        void CopyVisibleEffects(List<GameplayEffectViewState> destination);

        bool TryGetVisibleEffect(
            ulong runtimeId,
            out GameplayEffectViewState state);
    }
}
