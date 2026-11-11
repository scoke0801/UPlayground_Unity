using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Pet;
using UPlayGround.Data.Stat;

namespace UPlayGround.Components.Pet
{
    /// <summary>현재 플레이어의 GAS로 지원을 실행하고 공용 쿨다운과 펫 출처 효과만 보존한다.</summary>
    public sealed class PetSupportController
    {
        private readonly PetSkillCatalogSO _catalog;
        private readonly List<GameplayAbilitySO> _abilities = new();
        private readonly List<ActiveEffectSaveEntry> _effects = new();
        private PlayerActor _player;
        public PetSupportController(PetSkillCatalogSO catalog)
        {
            _catalog = catalog;
            if (catalog == null) return;
            foreach (var skill in catalog.skills)
                foreach (var level in skill.levels)
                    if (level.ability != null) _abilities.Add(level.ability);
        }
        public void Bind(PlayerActor player, PetSaveData state)
        {
            if (_player == player) return;
            Capture(state);
            _player?.Abilities?.RevokeTemporaryAbilities(_abilities);
            _player = player;
            if (_player == null) return;
            _player.Abilities.GrantTemporaryAbilities(_abilities);
            var cooldowns = _player.AbilitySystem.Runtime.Cooldowns;
            cooldowns.Restore(PetSkillCatalogSO.CooldownGroup, state.supportCooldownRemaining);
            _player.Effects.RestoreRuntimeState(state.supportEffects, ResolveEffect);
        }
        public void Capture(PetSaveData state)
        {
            if (_player == null || _player.AbilitySystem == null) return;
            state.supportCooldownRemaining = _player.AbilitySystem.Runtime.Cooldowns.GetRemaining(PetSkillCatalogSO.CooldownGroup);
            _effects.Clear(); _player.Effects.CaptureRuntimeState(_effects, false);
            for (int i = state.supportEffects.Count - 1; i >= 0; i--)
                if (ResolveEffect(state.supportEffects[i].effectId) != null) state.supportEffects.RemoveAt(i);
            for (int i = 0; i < _effects.Count; i++)
                if (ResolveEffect(_effects[i].effectId) != null) state.supportEffects.Add(_effects[i]);
        }
        public void ClearEffects(PetSaveData state)
        {
            foreach (var ability in _abilities)
                foreach (var variant in ability.variants)
                    foreach (var effect in variant.targetEffects)
                        _player?.Effects?.RemoveEffectsByDefinition(effect);
            state.supportEffects.Clear();
        }
        public void Dispose(PetSaveData state)
        { Capture(state); _player?.Abilities?.RevokeTemporaryAbilities(_abilities); _player = null; }
        public void Tick(PetActor pet, PetSaveData state, int level)
        {
            if (_player == null || !_player.IsAlive() || !pet.CanSupport || _catalog == null) return;
            var definition = _catalog.Find(state.selectedSupportId)?.GetLevel(level);
            var ability = definition?.ability;
            if (ability == null || Vector3.Distance(pet.transform.position, _player.transform.position) > _catalog.supportDistance) return;
            if (_player.AbilitySystem.Runtime.Cooldowns.GetRemaining(PetSkillCatalogSO.CooldownGroup) > 0f
                || _player.Effects.HasActiveEffect(definition.Effect)) return;
            if (Physics.Linecast(pet.transform.position + Vector3.up * 0.3f,
                _player.transform.position + Vector3.up, _catalog.supportObstructionMask, QueryTriggerInteraction.Ignore)) return;
            if (state.selectedSupportId == PetSkillCatalogSO.Heal)
            {
                var attributes = _player.AbilitySystem.Attributes;
                if (attributes.GetCurrent(Attributes.Vital.Health) >= attributes.GetCurrent(Attributes.Vital.MaxHealth)) return;
            }
            else if (!_player.IsInCombat) return;
            var result = _player.Abilities.TryPrepareAbility(ability, true, _player, out var handle, out _);
            if (result != AbilityActivationResult.Success) return;
            float previousHealth = _player.CurrentHealth;
            result = _player.Abilities.Commit(handle, pet);
            if (result == AbilityActivationResult.Success)
            {
                _player.Abilities.EndAbility(handle, true);
                if (state.selectedSupportId == PetSkillCatalogSO.Heal) _player.NotifyHealingApplied(previousHealth);
            }
            else _player.Abilities.Abort(handle);
        }
        private GameplayEffectSO ResolveEffect(string id)
        {
            foreach (var ability in _abilities)
                foreach (var variant in ability.variants)
                    foreach (var effect in variant.targetEffects)
                        if (effect != null && effect.effectId == id) return effect;
            return null;
        }
    }
}
