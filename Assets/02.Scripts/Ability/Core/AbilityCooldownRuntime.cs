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
        private readonly Dictionary<string, float> _endTimes =
            new(StringComparer.Ordinal);
        private readonly IAbilityClock _clock;

        public AbilityCooldownRuntime(IAbilityClock clock)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public void Start(string groupId, float durationSeconds)
        {
            if (string.IsNullOrWhiteSpace(groupId) || durationSeconds <= 0f)
                return;
            _endTimes[groupId.Trim()] = _clock.Time + durationSeconds;
        }

        public float GetRemaining(string groupId)
        {
            if (string.IsNullOrWhiteSpace(groupId)
                || !_endTimes.TryGetValue(groupId, out float endTime))
                return 0f;
            return Math.Max(0f, endTime - _clock.Time);
        }

        public void Restore(string groupId, float remainingSeconds)
        {
            if (string.IsNullOrWhiteSpace(groupId) || remainingSeconds <= 0f)
                return;
            _endTimes[groupId.Trim()] = _clock.Time + remainingSeconds;
        }

        public void Clear() => _endTimes.Clear();

        public void Capture(List<AbilityCooldownSnapshot> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            foreach (KeyValuePair<string, float> pair in _endTimes)
            {
                float remaining = Math.Max(0f, pair.Value - _clock.Time);
                if (remaining > 0f)
                    destination.Add(new AbilityCooldownSnapshot(pair.Key, remaining));
            }
        }

        public bool RemoveExpired()
        {
            bool changed = false;
            var expired = new List<string>();
            foreach (KeyValuePair<string, float> pair in _endTimes)
                if (pair.Value <= _clock.Time)
                    expired.Add(pair.Key);
            for (int i = 0; i < expired.Count; i++)
            {
                _endTimes.Remove(expired[i]);
                changed = true;
            }
            return changed;
        }
    }

    public readonly struct AbilityCooldownSnapshot
    {
        public string GroupId { get; }
        public float RemainingSeconds { get; }

        public AbilityCooldownSnapshot(string groupId, float remainingSeconds)
        {
            GroupId = groupId;
            RemainingSeconds = remainingSeconds;
        }
    }
}
