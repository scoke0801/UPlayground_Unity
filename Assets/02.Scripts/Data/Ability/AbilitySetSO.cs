using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Combat;

namespace UPlayGround.Data.Ability
{
    [CreateAssetMenu(fileName = "AbilitySet_", menuName = "UPlayGround/Ability/Ability Set")]
    public sealed class AbilitySetSO : ScriptableObject
    {
        [System.Serializable]
        public sealed class PlayerSlotEntry
        {
            public PlayerSkillSlot slot;
            public GameplayAbilitySO ability;
        }

        public List<PlayerSlotEntry> playerSlots = new();
        public List<GameplayAbilitySO> additionalAbilities = new();

        public GameplayAbilitySO GetPlayerAbility(PlayerSkillSlot slot)
        {
            for (int i = 0; i < playerSlots.Count; i++)
            {
                PlayerSlotEntry entry = playerSlots[i];
                if (entry != null && entry.slot == slot)
                    return entry.ability;
            }

            return null;
        }

        public IEnumerable<GameplayAbilitySO> EnumerateAll()
        {
            for (int i = 0; i < playerSlots.Count; i++)
                if (playerSlots[i]?.ability != null)
                    yield return playerSlots[i].ability;
            for (int i = 0; i < additionalAbilities.Count; i++)
                if (additionalAbilities[i] != null)
                    yield return additionalAbilities[i];
        }
    }
}
