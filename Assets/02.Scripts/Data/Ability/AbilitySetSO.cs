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
        [Header("Player Combat Loadout")]
        public List<PlayerCombatAbilityBinding> combatBindings = new();
        public PlayerChargeAbilitySettings charge = new();
        public List<AbilityComboRouteDefinition> comboRoutes = new();
        [Min(0.05f)] public float comboLinkWindow = 1f;

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

        public IReadOnlyList<GameplayAbilitySO> GetCombatSequence(
            PlayerCombatAbilitySlot slot)
        {
            for (int i = 0; i < combatBindings.Count; i++)
            {
                PlayerCombatAbilityBinding binding = combatBindings[i];
                if (binding != null && binding.slot == slot)
                    return binding.abilities;
            }
            return System.Array.Empty<GameplayAbilitySO>();
        }

        public GameplayAbilitySO GetCombatAbility(
            PlayerCombatAbilitySlot slot,
            int index = 0)
        {
            IReadOnlyList<GameplayAbilitySO> sequence = GetCombatSequence(slot);
            return index >= 0 && index < sequence.Count ? sequence[index] : null;
        }

        public IEnumerable<GameplayAbilitySO> EnumerateAll()
        {
            for (int i = 0; i < playerSlots.Count; i++)
                if (playerSlots[i]?.ability != null)
                    yield return playerSlots[i].ability;
            for (int i = 0; i < additionalAbilities.Count; i++)
                if (additionalAbilities[i] != null)
                    yield return additionalAbilities[i];
            for (int i = 0; i < combatBindings.Count; i++)
            {
                List<GameplayAbilitySO> abilities = combatBindings[i]?.abilities;
                if (abilities == null) continue;
                for (int j = 0; j < abilities.Count; j++)
                    if (abilities[j] != null)
                        yield return abilities[j];
            }
            if (charge?.stages != null)
                for (int i = 0; i < charge.stages.Count; i++)
                    if (charge.stages[i] != null)
                        yield return charge.stages[i];
            for (int i = 0; i < comboRoutes.Count; i++)
            {
                AbilityComboRouteDefinition route = comboRoutes[i];
                if (route?.ability != null) yield return route.ability;
                if (route?.enhancedAbility != null) yield return route.enhancedAbility;
            }
        }
    }
}
