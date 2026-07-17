using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Event;

namespace UPlayGround.Manager
{
    public partial class EventManager : BaseManager<EventManager>, IManager,
        IGameEventObservable, IGameEventPublisher
    {
        private readonly struct EventKey : IEquatable<EventKey>
        {
            public readonly Type EnumType;
            public readonly int EnumValue;
            public readonly Type PayloadType;

            public EventKey(Type enumType, int enumValue, Type payloadType)
            {
                EnumType = enumType;
                EnumValue = enumValue;
                PayloadType = payloadType;
            }

            public bool Equals(EventKey other) =>
                EnumType == other.EnumType &&
                EnumValue == other.EnumValue &&
                PayloadType == other.PayloadType;

            public override bool Equals(object obj) =>
                obj is EventKey other && Equals(other);

            public override int GetHashCode() =>
                HashCode.Combine(EnumType, EnumValue, PayloadType);
        }

        private sealed class EmptyEventPayload
        {
            private EmptyEventPayload() { }
        }

        private sealed class EventSubscription : IDisposable
        {
            private Action _unsubscribe;

            public EventSubscription(Action unsubscribe)
            {
                _unsubscribe = unsubscribe;
            }

            public void Dispose()
            {
                Action unsubscribe = _unsubscribe;
                _unsubscribe = null;
                unsubscribe?.Invoke();
            }
        }

        private readonly Dictionary<EventKey, Delegate> _sceneEventTable = new();
        private readonly Dictionary<EventKey, Delegate> _globalEventTable = new();
        private readonly Dictionary<(Type EnumType, int EnumValue), Type> _payloadTypes = new();

        public void Init() { }
        public void AfterInit() { }

        public void Dispose()
        {
            _sceneEventTable.Clear();
            _globalEventTable.Clear();
            _payloadTypes.Clear();
        }

        public void OnUpdate() { }
        public void OnFixedUpdate() { }
        public void OnLateUpdate() { }

        public void OnSceneChanged(string sceneType)
        {
            _sceneEventTable.Clear();
            RebuildPayloadTypeLookup();
        }

        public IDisposable Subscribe<TEnum, TData>(
            TEnum eventType,
            Action<TData> handler,
            EventSubscriptionScope scope = EventSubscriptionScope.Scene)
            where TEnum : Enum
            where TData : IEventData
        {
            if (handler == null)
                return new EventSubscription(null);

            EventKey key = CreateKey<TEnum, TData>(eventType);
            var table = GetTable(scope);
            table.TryGetValue(key, out Delegate existing);
            table[key] = (Action<TData>)existing + handler;
            return new EventSubscription(() => UnsubscribeFromScope(scope, key, handler));
        }

        public IDisposable Subscribe<TEnum>(
            TEnum eventType,
            Action handler,
            EventSubscriptionScope scope = EventSubscriptionScope.Scene)
            where TEnum : Enum
        {
            if (handler == null)
                return new EventSubscription(null);

            EventKey key = CreateKey<TEnum, EmptyEventPayload>(eventType);
            var table = GetTable(scope);
            table.TryGetValue(key, out Delegate existing);
            table[key] = (Action)existing + handler;
            return new EventSubscription(() => UnsubscribeFromScope(scope, key, handler));
        }

        public IDisposable Observe<TEnum>(
            TEnum eventType,
            IGameEventObserver<TEnum> observer,
            EventSubscriptionScope scope = EventSubscriptionScope.Scene)
            where TEnum : Enum
        {
            return Subscribe(
                eventType,
                observer != null ? () => observer.OnEvent(eventType) : null,
                scope);
        }

        public void Unsubscribe<TEnum, TData>(TEnum eventType, Action<TData> handler)
            where TEnum : Enum
            where TData : IEventData
        {
            EventKey key = CreateKey<TEnum, TData>(eventType, validatePayload: false);
            RemoveHandler(_sceneEventTable, key, handler);
            RemoveHandler(_globalEventTable, key, handler);
            RemovePayloadTypeIfUnused(key);
        }

        public void Unsubscribe<TEnum>(TEnum eventType, Action handler)
            where TEnum : Enum
        {
            EventKey key = CreateKey<TEnum, EmptyEventPayload>(eventType, validatePayload: false);
            RemoveHandler(_sceneEventTable, key, handler);
            RemoveHandler(_globalEventTable, key, handler);
            RemovePayloadTypeIfUnused(key);
        }

        public void Send<TEnum, TData>(TEnum eventType, TData data)
            where TEnum : Enum
            where TData : IEventData
        {
            EventKey key = CreateKey<TEnum, TData>(eventType);
            InvokeSafely(_globalEventTable, key, data);
            InvokeSafely(_sceneEventTable, key, data);
        }

        public void Send<TEnum>(TEnum eventType)
            where TEnum : Enum
        {
            EventKey key = CreateKey<TEnum, EmptyEventPayload>(eventType);
            InvokeSafely(_globalEventTable, key);
            InvokeSafely(_sceneEventTable, key);
        }

        public int GetSubscriberCount<TEnum>(TEnum eventType)
            where TEnum : Enum
        {
            var eventIdentity = (typeof(TEnum), Convert.ToInt32(eventType));
            int count = 0;

            foreach (var pair in _sceneEventTable)
            {
                if ((pair.Key.EnumType, pair.Key.EnumValue) == eventIdentity)
                    count += pair.Value?.GetInvocationList().Length ?? 0;
            }

            foreach (var pair in _globalEventTable)
            {
                if ((pair.Key.EnumType, pair.Key.EnumValue) == eventIdentity)
                    count += pair.Value?.GetInvocationList().Length ?? 0;
            }

            return count;
        }

        public void LogEventStatistics()
        {
            Debug.Log(
                $"=== Event Manager Statistics ===\n" +
                $"Scene Event Types: {_sceneEventTable.Count}\n" +
                $"Global Event Types: {_globalEventTable.Count}");

            LogTableStatistics("Scene", _sceneEventTable);
            LogTableStatistics("Global", _globalEventTable);
        }

        private EventKey CreateKey<TEnum, TPayload>(
            TEnum eventType,
            bool validatePayload = true)
            where TEnum : Enum
        {
            Type enumType = typeof(TEnum);
            int enumValue = Convert.ToInt32(eventType);
            Type payloadType = typeof(TPayload);

            if (validatePayload)
                ValidatePayloadType(enumType, enumValue, payloadType);

            return new EventKey(enumType, enumValue, payloadType);
        }

        private void ValidatePayloadType(Type enumType, int enumValue, Type payloadType)
        {
            var identity = (enumType, enumValue);
            if (_payloadTypes.TryGetValue(identity, out Type registeredType) &&
                registeredType != payloadType)
            {
                string eventName = Enum.GetName(enumType, enumValue) ?? enumValue.ToString();
                throw new InvalidOperationException(
                    $"이벤트 {enumType.Name}.{eventName}의 Payload 타입이 일치하지 않습니다. " +
                    $"등록={registeredType.Name}, 요청={payloadType.Name}");
            }

            _payloadTypes[identity] = payloadType;
        }

        private Dictionary<EventKey, Delegate> GetTable(EventSubscriptionScope scope) =>
            scope == EventSubscriptionScope.Global
                ? _globalEventTable
                : _sceneEventTable;

        private static void RemoveHandler(
            Dictionary<EventKey, Delegate> table,
            EventKey key,
            Delegate handler)
        {
            if (!table.TryGetValue(key, out Delegate existing))
                return;

            Delegate updated = Delegate.Remove(existing, handler);
            if (updated == null)
                table.Remove(key);
            else
                table[key] = updated;
        }

        private void UnsubscribeFromScope(
            EventSubscriptionScope scope,
            EventKey key,
            Delegate handler)
        {
            RemoveHandler(GetTable(scope), key, handler);
            RemovePayloadTypeIfUnused(key);
        }

        private void RemovePayloadTypeIfUnused(EventKey key)
        {
            if (_sceneEventTable.ContainsKey(key) || _globalEventTable.ContainsKey(key))
                return;

            _payloadTypes.Remove((key.EnumType, key.EnumValue));
        }

        private static void InvokeSafely<TData>(
            Dictionary<EventKey, Delegate> table,
            EventKey key,
            TData data)
        {
            if (!table.TryGetValue(key, out Delegate existing))
                return;

            Delegate[] handlers = existing.GetInvocationList();
            for (int i = 0; i < handlers.Length; i++)
            {
                try
                {
                    ((Action<TData>)handlers[i]).Invoke(data);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        private static void InvokeSafely(
            Dictionary<EventKey, Delegate> table,
            EventKey key)
        {
            if (!table.TryGetValue(key, out Delegate existing))
                return;

            Delegate[] handlers = existing.GetInvocationList();
            for (int i = 0; i < handlers.Length; i++)
            {
                try
                {
                    ((Action)handlers[i]).Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        private void RebuildPayloadTypeLookup()
        {
            _payloadTypes.Clear();

            foreach (EventKey key in _globalEventTable.Keys)
                _payloadTypes[(key.EnumType, key.EnumValue)] = key.PayloadType;
        }

        private static void LogTableStatistics(
            string scope,
            Dictionary<EventKey, Delegate> table)
        {
            foreach (var pair in table)
            {
                string eventName =
                    Enum.GetName(pair.Key.EnumType, pair.Key.EnumValue) ??
                    pair.Key.EnumValue.ToString();
                int subscriberCount = pair.Value?.GetInvocationList().Length ?? 0;

                Debug.Log(
                    $"[{scope}] {pair.Key.EnumType.Name}.{eventName} " +
                    $"<{pair.Key.PayloadType.Name}>: {subscriberCount} subscribers");
            }
        }
    }
}
