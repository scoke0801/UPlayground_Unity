using System;
using UPlayGround.Ability.Core;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Combat;

namespace UPlayGround.Contracts.Ability
{
    public interface IAbilityRuntimeReader
    {
        event Action StateChanged;
        bool TryGetPlayerSlotState(PlayerSkillSlot slot, out AbilitySlotViewState state);
    }
}
