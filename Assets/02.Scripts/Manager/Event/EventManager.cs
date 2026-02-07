using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Event;

namespace UPlayGround.Manager
{
    public partial class EventManager : BaseManager<EventManager>, IManager
    {
        public void Init()
        {
        }

        public void AfterInit()
        {
            
        }

        public void Dispose()
        {
            _eventTable.Clear();
        }

        public void OnUpdate()
        {
        }

        public void OnFixedUpdate()
        {
        }

        public void OnLateUpdate()
        {
        }
    }

    public partial class EventManager : BaseManager<EventManager>, IManager
    {
        // 모든 이벤트를 하나의 딕셔너리로 관리
        // Key: (EnumType, EnumValue) 조합
        private readonly Dictionary<(Type, int), Delegate> _eventTable = new();
        
        // 데이터 있는 이벤트 구독
        public void Subscribe<TEnum, TData>(TEnum eventType, Action<TData> handler) 
            where TEnum : System.Enum 
            where TData : IEventData
        {
            var key = (typeof(TEnum), Convert.ToInt32(eventType));
            
            if (!_eventTable.ContainsKey(key))
                _eventTable[key] = null;
            
            _eventTable[key] = (Action<TData>)_eventTable[key] + handler;
        }

        // 데이터 없는 이벤트 구독
        public void Subscribe<TEnum>(TEnum eventType, Action handler) 
            where TEnum : System.Enum
        {
            var key = (typeof(TEnum), Convert.ToInt32(eventType));
            
            if (!_eventTable.ContainsKey(key))
                _eventTable[key] = null;
            
            _eventTable[key] = (Action)_eventTable[key] + handler;
        }

        // 데이터 있는 이벤트 구독 해제
        public void Unsubscribe<TEnum, TData>(TEnum eventType, Action<TData> handler) 
            where TEnum : System.Enum 
            where TData : IEventData
        {
            var key = (typeof(TEnum), Convert.ToInt32(eventType));
            
            if (_eventTable.TryGetValue(key, out var existingDelegate))
                _eventTable[key] = (Action<TData>)existingDelegate - handler;
        }

        // 데이터 없는 이벤트 구독 해제
        public void Unsubscribe<TEnum>(TEnum eventType, Action handler) 
            where TEnum : System.Enum
        {
            var key = (typeof(TEnum), Convert.ToInt32(eventType));
            
            if (_eventTable.TryGetValue(key, out var existingDelegate))
                _eventTable[key] = (Action)existingDelegate - handler;
        }

        // 데이터 있는 이벤트 발송
        public void Send<TEnum, TData>(TEnum eventType, TData data) 
            where TEnum : System.Enum 
            where TData : IEventData
        {
            var key = (typeof(TEnum), Convert.ToInt32(eventType));
            
            if (_eventTable.TryGetValue(key, out var existingDelegate))
                (existingDelegate as Action<TData>)?.Invoke(data);
        }

        // 데이터 없는 이벤트 발송
        public void Send<TEnum>(TEnum eventType) 
            where TEnum : System.Enum
        {
            var key = (typeof(TEnum), Convert.ToInt32(eventType));
            
            if (_eventTable.TryGetValue(key, out var existingDelegate))
                (existingDelegate as Action)?.Invoke();
        }

        // 디버깅용: 특정 타입의 모든 구독자 수 확인
        public int GetSubscriberCount<TEnum>(TEnum eventType) where TEnum : System.Enum
        {
            var key = (typeof(TEnum), Convert.ToInt32(eventType));
            
            if (_eventTable.TryGetValue(key, out var del))
                return del?.GetInvocationList().Length ?? 0;
            
            return 0;
        }

        // 디버깅용: 전체 이벤트 통계
        public void LogEventStatistics()
        {
            Debug.Log($"=== Event Manager Statistics ===");
            Debug.Log($"Total Event Types: {_eventTable.Count}");
            
            foreach (var kvp in _eventTable)
            {
                var (enumType, enumValue) = kvp.Key;
                var enumName = System.Enum.GetName(enumType, enumValue);
                var subscriberCount = kvp.Value?.GetInvocationList().Length ?? 0;
                
                Debug.Log($"{enumType.Name}.{enumName}: {subscriberCount} subscribers");
            }
        }
        
    }
}