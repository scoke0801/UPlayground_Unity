using System;
using System.Collections.Generic;

namespace UPlayGround.Ability.Core
{
    /// <summary>
    /// Unity 및 프로젝트 타입에 의존하지 않는 쿨다운 상태 저장소.
    /// 시간 공급자를 외부에서 주입해 런타임과 테스트에서 동일하게 사용한다.
    /// </summary>
    public sealed class AbilityCooldownRuntime
    {
        private sealed class CooldownState
        {
            public float RechargeDuration;
            public float NextChargeTime;
            public int AvailableCharges;
            public int MaxCharges;
        }

        private readonly Dictionary<string, CooldownState> _states =
            new(StringComparer.Ordinal);
        private readonly List<string> _expiredGroupIds = new();
        private readonly IAbilityClock _clock;

        public AbilityCooldownRuntime(IAbilityClock clock)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public void Start(string groupId, float durationSeconds)
        {
            TryConsumeCharge(groupId, durationSeconds, 1);
        }

        public bool TryConsumeCharge(
            string groupId,
            float rechargeDurationSeconds,
            int maxCharges = 1)
        {
            if (string.IsNullOrWhiteSpace(groupId))
                return false;

            string key = groupId.Trim();
            int normalizedMax = Math.Max(1, maxCharges);
            float normalizedDuration = Math.Max(0f, rechargeDurationSeconds);
            if (!_states.TryGetValue(key, out CooldownState state))
            {
                state = new CooldownState
                {
                    RechargeDuration = normalizedDuration,
                    AvailableCharges = normalizedMax,
                    MaxCharges = normalizedMax,
                };
                _states.Add(key, state);
            }
            else
            {
                Normalize(state);
                state.MaxCharges = normalizedMax;
                state.RechargeDuration = normalizedDuration;
                state.AvailableCharges = Math.Min(
                    state.AvailableCharges,
                    state.MaxCharges);
            }

            if (state.AvailableCharges <= 0)
                return false;

            state.AvailableCharges--;
            if (state.AvailableCharges < state.MaxCharges)
            {
                if (normalizedDuration <= 0f)
                {
                    state.AvailableCharges = state.MaxCharges;
                    state.NextChargeTime = 0f;
                }
                else if (state.NextChargeTime <= 0f)
                {
                    state.NextChargeTime = _clock.Time + normalizedDuration;
                }
            }
            return true;
        }

        public float GetRemaining(string groupId)
        {
            if (string.IsNullOrWhiteSpace(groupId)
                || !_states.TryGetValue(groupId.Trim(), out CooldownState state))
                return 0f;
            Normalize(state);
            return state.AvailableCharges > 0
                ? 0f
                : Math.Max(0f, state.NextChargeTime - _clock.Time);
        }

        public float GetNextChargeRemaining(string groupId)
        {
            if (string.IsNullOrWhiteSpace(groupId)
                || !_states.TryGetValue(groupId.Trim(), out CooldownState state))
                return 0f;
            Normalize(state);
            return state.AvailableCharges >= state.MaxCharges
                ? 0f
                : Math.Max(0f, state.NextChargeTime - _clock.Time);
        }

        public int GetAvailableCharges(string groupId, int defaultMaxCharges = 1)
        {
            if (string.IsNullOrWhiteSpace(groupId)
                || !_states.TryGetValue(groupId.Trim(), out CooldownState state))
                return Math.Max(1, defaultMaxCharges);
            Normalize(state);
            return state.AvailableCharges;
        }

        public int GetMaxCharges(string groupId, int defaultMaxCharges = 1)
        {
            if (string.IsNullOrWhiteSpace(groupId)
                || !_states.TryGetValue(groupId.Trim(), out CooldownState state))
                return Math.Max(1, defaultMaxCharges);
            return state.MaxCharges;
        }

        public void Restore(string groupId, float remainingSeconds)
        {
            Restore(groupId, remainingSeconds, 0, 1, remainingSeconds);
        }

        public void Restore(
            string groupId,
            float nextChargeRemainingSeconds,
            int availableCharges,
            int maxCharges,
            float rechargeDurationSeconds)
        {
            if (string.IsNullOrWhiteSpace(groupId))
                return;
            int normalizedMax = Math.Max(1, maxCharges);
            int normalizedAvailable = Math.Max(
                0,
                Math.Min(availableCharges, normalizedMax));
            float remaining = Math.Max(0f, nextChargeRemainingSeconds);
            if (normalizedAvailable >= normalizedMax && remaining <= 0f)
            {
                _states.Remove(groupId.Trim());
                return;
            }

            _states[groupId.Trim()] = new CooldownState
            {
                RechargeDuration = Math.Max(
                    remaining,
                    Math.Max(0f, rechargeDurationSeconds)),
                NextChargeTime = remaining > 0f ? _clock.Time + remaining : 0f,
                AvailableCharges = normalizedAvailable,
                MaxCharges = normalizedMax,
            };
        }

        public bool Remove(string groupId) =>
            !string.IsNullOrWhiteSpace(groupId) && _states.Remove(groupId.Trim());

        public void Clear() => _states.Clear();

        public void Capture(List<AbilityCooldownSnapshot> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            foreach (KeyValuePair<string, CooldownState> pair in _states)
            {
                Normalize(pair.Value);
                if (pair.Value.AvailableCharges >= pair.Value.MaxCharges)
                    continue;
                float remaining = Math.Max(
                    0f,
                    pair.Value.NextChargeTime - _clock.Time);
                destination.Add(new AbilityCooldownSnapshot(
                    pair.Key,
                    remaining,
                    pair.Value.AvailableCharges,
                    pair.Value.MaxCharges,
                    pair.Value.RechargeDuration));
            }
        }

        public bool RemoveExpired()
        {
            bool changed = false;
            _expiredGroupIds.Clear();
            foreach (KeyValuePair<string, CooldownState> pair in _states)
            {
                int previousCharges = pair.Value.AvailableCharges;
                Normalize(pair.Value);
                if (pair.Value.AvailableCharges != previousCharges)
                    changed = true;
                if (pair.Value.AvailableCharges >= pair.Value.MaxCharges)
                    _expiredGroupIds.Add(pair.Key);
            }
            for (int i = 0; i < _expiredGroupIds.Count; i++)
            {
                _states.Remove(_expiredGroupIds[i]);
                changed = true;
            }
            _expiredGroupIds.Clear();
            return changed;
        }

        private void Normalize(CooldownState state)
        {
            if (state == null
                || state.AvailableCharges >= state.MaxCharges
                || state.NextChargeTime <= 0f
                || state.RechargeDuration <= 0f)
                return;

            while (state.AvailableCharges < state.MaxCharges
                   && _clock.Time >= state.NextChargeTime)
            {
                state.AvailableCharges++;
                state.NextChargeTime = state.AvailableCharges < state.MaxCharges
                    ? state.NextChargeTime + state.RechargeDuration
                    : 0f;
            }
        }
    }

    public readonly struct AbilityCooldownSnapshot
    {
        public string GroupId { get; }
        public float RemainingSeconds { get; }
        public int AvailableCharges { get; }
        public int MaxCharges { get; }
        public float RechargeDurationSeconds { get; }

        public AbilityCooldownSnapshot(
            string groupId,
            float remainingSeconds,
            int availableCharges = 0,
            int maxCharges = 1,
            float rechargeDurationSeconds = 0f)
        {
            GroupId = groupId;
            RemainingSeconds = remainingSeconds;
            AvailableCharges = availableCharges;
            MaxCharges = Math.Max(1, maxCharges);
            RechargeDurationSeconds = Math.Max(
                remainingSeconds,
                rechargeDurationSeconds);
        }
    }
}
