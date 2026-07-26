using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Combat;

namespace UPlayGround.Data.Ability
{
    public enum AbilitySetOverrideOperation
    {
        Replace,
        Remove,
    }

    [CreateAssetMenu(fileName = "AbilitySet_", menuName = "UPlayGround/Ability/Ability Set")]
    public sealed class AbilitySetSO : ScriptableObject
    {
        [System.NonSerialized] private HashSet<GameplayAbilitySO> _runtimeIndex;
        [System.NonSerialized]
        private Dictionary<GameplayAbilitySO, PlayerSkillSlot> _runtimePlayerSlots;
        [System.NonSerialized] private List<GameplayAbilitySO> _runtimeAbilities;
        [System.Serializable]
        public sealed class PlayerSlotEntry
        {
            public PlayerSkillSlot slot;
            public GameplayAbilitySO ability;
        }

        [System.Serializable]
        public sealed class AbilityOverrideEntry
        {
            [Tooltip("Base Set의 유효 Ability 중 교체하거나 제거할 대상입니다.")]
            public GameplayAbilitySO sourceAbility;
            public AbilitySetOverrideOperation operation;
            [Tooltip("Replace일 때 사용할 파생 Ability입니다. Remove에서는 비워둡니다.")]
            public GameplayAbilitySO replacementAbility;
        }

        [TextArea]
        [Tooltip("에디터 전용 메모. 입력하면 Ability Editor 목록에 함께 표시됩니다.")]
        public string editorMemo;

        [Header("공용 Set 합성")]
        [Tooltip("동일 타입 몬스터 등이 공유하는 공용 AbilitySet입니다. 비어 있으면 독립 Set입니다.")]
        public AbilitySetSO baseSet;
        [Tooltip("Base Set에서 상속한 Ability에만 적용되는 교체·제거 목록입니다.")]
        public List<AbilityOverrideEntry> abilityOverrides = new();

        [Header("로컬 구성")]
        public List<PlayerSlotEntry> playerSlots = new();
        public List<GameplayAbilitySO> additionalAbilities = new();
        [Header("Player Combat Loadout")]
        public List<PlayerCombatAbilityBinding> combatBindings = new();
        [Tooltip("켜면 Base Set의 차지 구성을 로컬 charge로 대체합니다.")]
        public bool overrideCharge;
        public PlayerChargeAbilitySettings charge = new();
        [Tooltip("켜면 Base Set의 콤보 라우트를 로컬 comboRoutes로 대체합니다.")]
        public bool overrideComboRoutes;
        public List<AbilityComboRouteDefinition> comboRoutes = new();
        [Tooltip("켜면 Base Set의 콤보 연결 시간을 로컬 값으로 대체합니다.")]
        public bool overrideComboLinkWindow;
        [Min(0.05f)] public float comboLinkWindow = 1f;

        public GameplayAbilitySO GetPlayerAbility(PlayerSkillSlot slot)
        {
            return GetPlayerAbility(slot, new HashSet<AbilitySetSO>());
        }

        private GameplayAbilitySO GetPlayerAbility(
            PlayerSkillSlot slot,
            HashSet<AbilitySetSO> visited)
        {
            if (!visited.Add(this))
                return null;
            for (int i = 0; i < (playerSlots?.Count ?? 0); i++)
            {
                PlayerSlotEntry entry = playerSlots[i];
                if (entry != null && entry.slot == slot)
                    return entry.ability;
            }

            GameplayAbilitySO inherited =
                baseSet != null
                    ? baseSet.GetPlayerAbility(slot, visited)
                    : null;
            return ResolveInheritedAbility(inherited);
        }

        public IReadOnlyList<GameplayAbilitySO> GetCombatSequence(
            PlayerCombatAbilitySlot slot)
        {
            return GetCombatSequence(slot, new HashSet<AbilitySetSO>());
        }

        private IReadOnlyList<GameplayAbilitySO> GetCombatSequence(
            PlayerCombatAbilitySlot slot,
            HashSet<AbilitySetSO> visited)
        {
            if (!visited.Add(this))
                return System.Array.Empty<GameplayAbilitySO>();
            for (int i = 0; i < (combatBindings?.Count ?? 0); i++)
            {
                PlayerCombatAbilityBinding binding = combatBindings[i];
                if (binding != null && binding.slot == slot)
                    return binding.abilities
                        ?? (IReadOnlyList<GameplayAbilitySO>)
                            System.Array.Empty<GameplayAbilitySO>();
            }

            IReadOnlyList<GameplayAbilitySO> inherited =
                baseSet != null
                    ? baseSet.GetCombatSequence(slot, visited)
                    : System.Array.Empty<GameplayAbilitySO>();
            if (inherited.Count == 0)
                return inherited;
            var resolved = new List<GameplayAbilitySO>(inherited.Count);
            for (int i = 0; i < inherited.Count; i++)
            {
                GameplayAbilitySO ability =
                    ResolveInheritedAbility(inherited[i]);
                if (ability != null)
                    resolved.Add(ability);
            }
            return resolved;
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
            var yielded = new HashSet<GameplayAbilitySO>();

            foreach (PlayerSkillSlot slot in
                     System.Enum.GetValues(typeof(PlayerSkillSlot)))
            {
                GameplayAbilitySO ability = GetPlayerAbility(slot);
                if (ability != null && yielded.Add(ability))
                    yield return ability;
            }

            foreach (PlayerCombatAbilitySlot slot in
                     System.Enum.GetValues(typeof(PlayerCombatAbilitySlot)))
            {
                IReadOnlyList<GameplayAbilitySO> sequence =
                    GetCombatSequence(slot);
                for (int i = 0; i < sequence.Count; i++)
                    if (sequence[i] != null && yielded.Add(sequence[i]))
                        yield return sequence[i];
            }

            foreach (GameplayAbilitySO ability in
                     EnumerateEffectiveAdditional(
                         new HashSet<AbilitySetSO>()))
            {
                if (ability != null && yielded.Add(ability))
                    yield return ability;
            }

            PlayerChargeAbilitySettings effectiveCharge = GetEffectiveCharge();
            if (effectiveCharge?.stages != null)
                for (int i = 0; i < effectiveCharge.stages.Count; i++)
                {
                    GameplayAbilitySO ability =
                        ResolveEffectiveChargeAbility(effectiveCharge.stages[i]);
                    if (ability != null && yielded.Add(ability))
                        yield return ability;
                }

            IReadOnlyList<AbilityComboRouteDefinition> routes =
                GetEffectiveComboRoutes();
            for (int i = 0; i < routes.Count; i++)
            {
                AbilityComboRouteDefinition route = routes[i];
                GameplayAbilitySO ability =
                    ResolveEffectiveComboRouteAbility(route?.ability);
                GameplayAbilitySO enhanced =
                    ResolveEffectiveComboRouteAbility(route?.enhancedAbility);
                if (ability != null && yielded.Add(ability))
                    yield return ability;
                if (enhanced != null && yielded.Add(enhanced))
                    yield return enhanced;
            }
        }

        private IEnumerable<GameplayAbilitySO> EnumerateEffectiveAdditional(
            HashSet<AbilitySetSO> visited)
        {
            if (!visited.Add(this))
                yield break;
            if (baseSet != null)
            {
                foreach (GameplayAbilitySO inherited in
                         baseSet.EnumerateEffectiveAdditional(visited))
                {
                    GameplayAbilitySO resolved =
                        ResolveInheritedAbility(inherited);
                    if (resolved != null)
                        yield return resolved;
                }
            }
            for (int i = 0; i < (additionalAbilities?.Count ?? 0); i++)
                if (additionalAbilities[i] != null)
                    yield return additionalAbilities[i];
        }

        public GameplayAbilitySO ResolveInheritedAbility(
            GameplayAbilitySO inherited)
        {
            if (inherited == null)
                return null;
            for (int i = 0; i < (abilityOverrides?.Count ?? 0); i++)
            {
                AbilityOverrideEntry entry = abilityOverrides[i];
                if (entry?.sourceAbility != inherited)
                    continue;
                return entry.operation == AbilitySetOverrideOperation.Remove
                    ? null
                    : entry.replacementAbility;
            }
            return inherited;
        }

        public PlayerChargeAbilitySettings GetEffectiveCharge()
        {
            AbilitySetSO owner =
                GetEffectiveChargeOwner(new HashSet<AbilitySetSO>());
            return owner?.charge;
        }

        private AbilitySetSO GetEffectiveChargeOwner(
            HashSet<AbilitySetSO> visited)
        {
            if (!visited.Add(this))
                return null;
            if (baseSet == null || overrideCharge)
                return this;
            return baseSet.GetEffectiveChargeOwner(visited);
        }

        public GameplayAbilitySO ResolveEffectiveChargeAbility(
            GameplayAbilitySO ability)
        {
            AbilitySetSO owner =
                GetEffectiveChargeOwner(new HashSet<AbilitySetSO>());
            return ResolveFromAncestor(owner, ability);
        }

        public IReadOnlyList<AbilityComboRouteDefinition>
            GetEffectiveComboRoutes()
        {
            AbilitySetSO owner =
                GetEffectiveComboRouteOwner(new HashSet<AbilitySetSO>());
            return owner?.comboRoutes
                ?? (IReadOnlyList<AbilityComboRouteDefinition>)
                    System.Array.Empty<AbilityComboRouteDefinition>();
        }

        private AbilitySetSO GetEffectiveComboRouteOwner(
            HashSet<AbilitySetSO> visited)
        {
            if (!visited.Add(this))
                return null;
            if (baseSet == null || overrideComboRoutes)
                return this;
            return baseSet.GetEffectiveComboRouteOwner(visited);
        }

        public GameplayAbilitySO ResolveEffectiveComboRouteAbility(
            GameplayAbilitySO ability)
        {
            AbilitySetSO owner =
                GetEffectiveComboRouteOwner(new HashSet<AbilitySetSO>());
            return ResolveFromAncestor(owner, ability);
        }

        public float GetEffectiveComboLinkWindow()
        {
            return GetEffectiveComboLinkWindow(new HashSet<AbilitySetSO>());
        }

        private float GetEffectiveComboLinkWindow(
            HashSet<AbilitySetSO> visited)
        {
            if (!visited.Add(this))
                return comboLinkWindow;
            if (baseSet == null || overrideComboLinkWindow)
                return comboLinkWindow;
            return baseSet.GetEffectiveComboLinkWindow(visited);
        }

        private GameplayAbilitySO ResolveFromAncestor(
            AbilitySetSO ancestor,
            GameplayAbilitySO ability)
        {
            if (ancestor == null || ability == null)
                return null;
            if (ReferenceEquals(this, ancestor))
                return ability;
            GameplayAbilitySO inherited =
                baseSet != null
                    ? baseSet.ResolveFromAncestor(ancestor, ability)
                    : ability;
            return ResolveInheritedAbility(inherited);
        }

        public bool TryGetPlayerSlot(
            GameplayAbilitySO ability,
            out PlayerSkillSlot slot)
        {
            EnsureRuntimeIndex();
            if (ability != null
                && _runtimePlayerSlots.TryGetValue(ability, out slot))
                return true;
            slot = default;
            return false;
        }

        private bool TryGetPlayerSlotSlow(
            GameplayAbilitySO ability,
            out PlayerSkillSlot slot)
        {
            foreach (PlayerSkillSlot candidate in
                     System.Enum.GetValues(typeof(PlayerSkillSlot)))
            {
                if (GetPlayerAbility(candidate) != ability)
                    continue;
                slot = candidate;
                return true;
            }
            slot = default;
            return false;
        }

        public bool HasInheritanceCycle()
        {
            var visited = new HashSet<AbilitySetSO>();
            AbilitySetSO current = this;
            while (current != null)
            {
                if (!visited.Add(current))
                    return true;
                current = current.baseSet;
            }
            return false;
        }

        public bool IsDerivedFrom(AbilitySetSO ancestor)
        {
            if (ancestor == null)
                return false;
            var visited = new HashSet<AbilitySetSO>();
            AbilitySetSO current = baseSet;
            while (current != null && visited.Add(current))
            {
                if (ReferenceEquals(current, ancestor))
                    return true;
                current = current.baseSet;
            }
            return false;
        }

        public bool Contains(GameplayAbilitySO ability)
        {
            if (ability == null)
                return false;
            EnsureRuntimeIndex();
            return _runtimeIndex.Contains(ability);
        }

        public IReadOnlyList<GameplayAbilitySO> GetRuntimeAbilities()
        {
            EnsureRuntimeIndex();
            return _runtimeAbilities;
        }

        public void EnsureRuntimeIndex()
        {
            if (_runtimeIndex != null)
                return;

            _runtimeIndex = new HashSet<GameplayAbilitySO>();
            _runtimePlayerSlots =
                new Dictionary<GameplayAbilitySO, PlayerSkillSlot>();
            _runtimeAbilities = new List<GameplayAbilitySO>();
            foreach (GameplayAbilitySO ability in EnumerateAll())
            {
                if (ability == null || !_runtimeIndex.Add(ability))
                    continue;
                _runtimeAbilities.Add(ability);
                if (TryGetPlayerSlotSlow(ability, out PlayerSkillSlot slot))
                    _runtimePlayerSlots[ability] = slot;
            }
        }

        /// <summary>
        /// 런타임에 AbilitySet 구성을 바꾼 뒤 인덱스를 즉시 다시 구축합니다.
        /// 일반 에셋은 OnEnable/OnValidate에서 자동 처리됩니다.
        /// </summary>
        public void RebuildRuntimeIndex()
        {
            InvalidateRuntimeIndex();
            EnsureRuntimeIndex();
        }

        private void OnEnable()
        {
            InvalidateRuntimeIndex();
            EnsureRuntimeIndex();
        }

#if UNITY_EDITOR
        private void OnValidate() => InvalidateRuntimeIndex();
#endif

        private void InvalidateRuntimeIndex()
        {
            _runtimeIndex = null;
            _runtimePlayerSlots = null;
            _runtimeAbilities = null;
        }
    }
}
