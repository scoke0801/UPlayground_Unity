using System;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Combat;

namespace UPlayGround.Contracts.Ability
{
    public readonly struct AbilitySlotPresentationState
    {
        public readonly string DisplayName;
        public readonly Sprite Icon;

        public AbilitySlotPresentationState(string displayName, Sprite icon)
        {
            DisplayName = displayName;
            Icon = icon;
        }
    }

    public interface IAbilityRuntimeReader
    {
        event Action StateChanged;
        bool TryGetPlayerSlotState(PlayerSkillSlot slot, out AbilitySlotViewState state);
        bool TryGetPlayerSlotPresentation(
            PlayerSkillSlot slot,
            out AbilitySlotPresentationState presentation);
    }
}
