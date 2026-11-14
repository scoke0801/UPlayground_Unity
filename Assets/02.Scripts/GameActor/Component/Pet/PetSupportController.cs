using System.Collections.Generic;
using System;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Pet;
using UPlayGround.Manager;
using UPlayGround.Gameplay.Tag;

namespace UPlayGround.Components.Pet
{
    /// <summary>플레이어의 신호를 지원 정책으로 평가하고 GAS 효과와 독립 쿨다운을 보존한다.</summary>
    public sealed class PetSupportController
    {
        private readonly PetSkillCatalogSO _catalog;
        private readonly List<GameplayAbilitySO> _abilities = new();
        private readonly List<ActiveEffectSaveEntry> _effects = new();
        private PlayerActor _player;
        private PetActor _pet;
        private PetSaveData _state;
        private readonly Func<bool> _canReact;
        private readonly List<string> _groups = new();
        private float _minimumIntervalEnds;
        private bool _isActivating;
        public int ActivationVersion { get; private set; }
        public bool IsAvailable => _catalog != null && _state != null
            && _catalog.Find(_state.SelectedSupportId)?.supportAbility != null
            && _player != null && _player.IsAlive() && _pet != null
            && _pet.CanSupport && Time.timeScale > 0f && (_canReact == null || _canReact())
            && Vector3.SqrMagnitude(_pet.transform.position - _player.transform.position)
                <= _catalog.supportDistance * _catalog.supportDistance
            && !Physics.Linecast(_pet.transform.position + Vector3.up * .3f,
                _player.transform.position + Vector3.up, _catalog.supportObstructionMask, QueryTriggerInteraction.Ignore);

        public PetSupportController(PetSkillCatalogSO catalog, Func<bool> canReact = null)
        {
            _catalog = catalog;
            _canReact = canReact;
            if (catalog == null) return;
            foreach (var skill in catalog.skills)
                foreach (var level in skill.levels)
                    if (level.ability != null)
                    {
                        _abilities.Add(level.ability);
                        string group = level.ability.cooldown.ResolveGroupId(level.ability.abilityId);
                        if (!_groups.Contains(group)) _groups.Add(group);
                    }
        }
        public void Bind(PlayerActor player, PetSaveData state)
        {
            if (_player == player) return;
            Capture(state);
            Unsubscribe();
            _player?.Abilities?.RevokeTemporaryAbilities(_abilities);
            _player = player;
            _state = state;
            if (_player == null) return;
            _player.Abilities.GrantTemporaryAbilities(_abilities);
            var cooldowns = _player.AbilitySystem.Runtime.Cooldowns;
            for (int i = 0; i < state.supportCooldowns.Count; i++)
            {
                var entry = state.supportCooldowns[i];
                cooldowns.Restore(entry.groupId, entry.remainingSeconds, entry.availableCharges,
                    entry.maxCharges, entry.rechargeDurationSeconds);
            }
            // 구버전 공용 대기는 선택한 지원에 한 번만 인계한다.
            var selected = _catalog.Find(state.SelectedSupportId)?.supportAbility;
            if (selected != null && state.supportCooldownRemaining > 0f)
            {
                cooldowns.Restore(selected.cooldown.ResolveGroupId(selected.abilityId), state.supportCooldownRemaining);
                state.supportCooldownRemaining = 0f;
            }
            _minimumIntervalEnds = Time.time + state.supportMinimumIntervalRemaining;
            _player.Effects.RestoreRuntimeState(state.supportEffects, ResolveEffect);
            _player.PassiveActivationSucceeded += OnPassiveActivation;
            _player.AbilitySystem.Runtime.Events.EventSent += OnGameplayEvent;
        }
        public void Capture(PetSaveData state)
        {
            if (_player == null || _player.AbilitySystem == null) return;
            state.supportMinimumIntervalRemaining = Mathf.Max(0f, _minimumIntervalEnds - Time.time);
            var cooldowns = _player.AbilitySystem.Runtime.Cooldowns;
            for (int i = 0; i < _groups.Count; i++)
            {
                string group = _groups[i];
                var entry = state.supportCooldowns.Find(value => value.groupId == group);
                if (entry == null) { entry = new GasCooldownSaveEntry { groupId = group }; state.supportCooldowns.Add(entry); }
                entry.remainingSeconds = cooldowns.GetRemaining(group);
                entry.availableCharges = cooldowns.GetAvailableCharges(group);
                entry.maxCharges = cooldowns.GetMaxCharges(group);
                entry.rechargeDurationSeconds = entry.remainingSeconds;
            }
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
        { Capture(state); Unsubscribe(); _player?.Abilities?.RevokeTemporaryAbilities(_abilities); _player = null; _pet = null; _state = null; }

        /// <summary>회수·교체 중 이전 펫을 신호 대상으로 남기지 않는다.</summary>
        public void SetPet(PetActor pet) => _pet = pet;

        private void Unsubscribe()
        {
            if (_player == null) return;
            _player.PassiveActivationSucceeded -= OnPassiveActivation;
            if (_player.AbilitySystem?.Runtime != null)
                _player.AbilitySystem.Runtime.Events.EventSent -= OnGameplayEvent;
        }

        private void OnPassiveActivation(PassiveActivationType type)
        {
            if (type == PassiveActivationType.PerfectDodge || type == PassiveActivationType.PerfectGuard)
                TryActivate(PetSupportTriggerKind.PerfectDefense);
        }

        private void OnGameplayEvent(GameplayEventData data)
        {
            if (_state == null || !data.Target.Equals(_player.AbilitySystem.Runtime.Handle)
                || !data.EventTag.IsChildOf(new AbilityTagId(GameplayTags.Trigger_Player_Hit.TagName))) return;
            var policy = _catalog.Find(_state.SelectedSupportId)?.triggerPolicy;
            if (policy?.MatchesHit(data.EventTag) == true) TryActivate(PetSupportTriggerKind.HeavyHitTaken);
        }
        public void Tick(PetActor pet, PetSaveData state)
        {
            _pet = pet; _state = state;
            TryActivate(PetSupportTriggerKind.HealthBelow);
        }

        private void TryActivate(PetSupportTriggerKind signal)
        {
            if (_isActivating || _state == null || _catalog == null || _player == null || !_player.IsAlive()
                || _pet == null || Time.timeScale <= 0f || (_canReact != null && !_canReact())) return;
            var skill = _catalog.Find(_state.SelectedSupportId);
            var policy = skill?.triggerPolicy;
            if (policy == null || (policy.requiresCanSupport && !IsAvailable)
                || !policy.Matches(signal, _player.GetHealthPercent(), _player.IsInCombat)
                || Time.time < _minimumIntervalEnds) return;
            int level = 1;
            for (int i = 0; i < _state.skills.Count; i++)
                if (_state.skills[i].skillId == skill.skillId) { level = _state.skills[i].level; break; }
            var definition = skill.GetLevel(level);
            var ability = definition?.ability;
            if (ability == null) return;
            if (_player.AbilitySystem.Runtime.Cooldowns.GetRemaining(ability.cooldown.ResolveGroupId(ability.abilityId)) > 0f
                || _player.Effects.HasActiveEffect(definition.Effect)) return;
            _isActivating = true;
            try
            {
                var result = _player.Abilities.TryPrepareAbility(ability, true, _player, out var handle, out _);
                if (result != AbilityActivationResult.Success) return;
                float previousHealth = _player.CurrentHealth;
                result = _player.Abilities.Commit(handle, _pet);
                if (result == AbilityActivationResult.Success)
                {
                    _player.Abilities.EndAbility(handle, true);
                    _minimumIntervalEnds = Time.time + _catalog.supportMinimumInterval;
                    ActivationVersion++;
                    _player.NotifyHealingApplied(previousHealth);
                    string fxKey = skill.activateFxKey;
                    if (!string.IsNullOrWhiteSpace(fxKey)) ActorSvc.Objects?.ShowFX(fxKey, _player.transform.position);
                    if (!string.IsNullOrWhiteSpace(skill.petActivateFxKey)) ActorSvc.Objects?.ShowFX(skill.petActivateFxKey, _pet.transform.position);
                }
                else _player.Abilities.Abort(handle);
            }
            finally { _isActivating = false; }
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
