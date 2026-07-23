using System;
using System.Collections.Generic;

namespace UPlayGround.Ability.Core
{
    public readonly struct AbilitySystemHandle : IEquatable<AbilitySystemHandle>
    {
        public ulong Value { get; }
        public bool IsValid => Value != 0;
        public AbilitySystemHandle(ulong value) => Value = value;
        public bool Equals(AbilitySystemHandle other) => Value == other.Value;
        public override bool Equals(object obj) => obj is AbilitySystemHandle other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
    }

    public readonly struct GameplayEffectSpecHandle : IEquatable<GameplayEffectSpecHandle>
    {
        public ulong Value { get; }
        public bool IsValid => Value != 0;
        public GameplayEffectSpecHandle(ulong value) => Value = value;
        public bool Equals(GameplayEffectSpecHandle other) => Value == other.Value;
        public override bool Equals(object obj) => obj is GameplayEffectSpecHandle other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
    }

    public readonly struct GameplayEventData
    {
        public AbilityTagId EventTag { get; }
        public AbilitySystemHandle Instigator { get; }
        public AbilitySystemHandle Target { get; }
        public AbilityExecutionHandle AbilityHandle { get; }
        public GameplayEffectSpecHandle EffectSpecHandle { get; }
        public float Magnitude { get; }
        public object Payload { get; }

        public GameplayEventData(
            AbilityTagId eventTag,
            AbilitySystemHandle instigator = default,
            AbilitySystemHandle target = default,
            AbilityExecutionHandle abilityHandle = default,
            GameplayEffectSpecHandle effectSpecHandle = default,
            float magnitude = 0f,
            object payload = null)
        {
            EventTag = eventTag;
            Instigator = instigator;
            Target = target;
            AbilityHandle = abilityHandle;
            EffectSpecHandle = effectSpecHandle;
            Magnitude = magnitude;
            Payload = payload;
        }
    }

    public sealed class GameplayEventRouter
    {
        private sealed class Subscription : IDisposable
        {
            private GameplayEventRouter _owner;
            internal readonly ulong Id;
            internal readonly AbilityTagId Tag;
            internal readonly bool MatchHierarchy;
            internal readonly Action<GameplayEventData> Callback;

            public Subscription(
                GameplayEventRouter owner,
                ulong id,
                AbilityTagId tag,
                bool matchHierarchy,
                Action<GameplayEventData> callback)
            {
                _owner = owner;
                Id = id;
                Tag = tag;
                MatchHierarchy = matchHierarchy;
                Callback = callback;
            }

            public void Dispose()
            {
                GameplayEventRouter owner = _owner;
                _owner = null;
                owner?.Unsubscribe(Id);
            }
        }

        private readonly Dictionary<ulong, Subscription> _subscriptions = new();
        private ulong _nextId = 1;

        public event Action<GameplayEventData> EventSent;
        public int SubscriptionCount => _subscriptions.Count;

        public IDisposable Subscribe(
            AbilityTagId eventTag,
            Action<GameplayEventData> callback,
            bool matchHierarchy = false)
        {
            if (!eventTag.IsValid) throw new ArgumentException("유효한 Event Tag가 필요합니다.", nameof(eventTag));
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            ulong id = _nextId++;
            if (id == 0) id = _nextId++;
            var subscription = new Subscription(this, id, eventTag, matchHierarchy, callback);
            _subscriptions.Add(id, subscription);
            return subscription;
        }

        public void Send(in GameplayEventData eventData)
        {
            if (!eventData.EventTag.IsValid) return;
            EventSent?.Invoke(eventData);
            var snapshot = new List<Subscription>(_subscriptions.Values);
            for (int i = 0; i < snapshot.Count; i++)
            {
                Subscription subscription = snapshot[i];
                if (!_subscriptions.ContainsKey(subscription.Id)) continue;
                bool matches = subscription.MatchHierarchy
                    ? eventData.EventTag.IsChildOf(subscription.Tag)
                    : eventData.EventTag.Equals(subscription.Tag);
                if (matches) subscription.Callback(eventData);
            }
        }

        private void Unsubscribe(ulong id) => _subscriptions.Remove(id);
        public void Clear() => _subscriptions.Clear();
    }
}
