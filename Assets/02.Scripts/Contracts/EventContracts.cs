using System;
using UPlayGround.Data.Event;

namespace UPlayGround.Manager
{
    /// <summary>특정 게임 이벤트 종류를 전달받는 옵저버 계약.</summary>
    public interface IGameEventObserver<in TEnum> where TEnum : Enum
    {
        void OnEvent(TEnum eventType);
    }

    /// <summary>게임 이벤트를 타입 안전하게 관측하는 읽기 전용 계약.</summary>
    public interface IGameEventObservable : IGameService
    {
        IDisposable Subscribe<TEnum, TData>(
            TEnum eventType,
            Action<TData> handler,
            EventSubscriptionScope scope = EventSubscriptionScope.Scene)
            where TEnum : Enum
            where TData : IEventData;

        IDisposable Subscribe<TEnum>(
            TEnum eventType,
            Action handler,
            EventSubscriptionScope scope = EventSubscriptionScope.Scene)
            where TEnum : Enum;

        void Unsubscribe<TEnum, TData>(
            TEnum eventType,
            Action<TData> handler)
            where TEnum : Enum
            where TData : IEventData;

        void Unsubscribe<TEnum>(
            TEnum eventType,
            Action handler)
            where TEnum : Enum;

        IDisposable Observe<TEnum>(
            TEnum eventType,
            IGameEventObserver<TEnum> observer,
            EventSubscriptionScope scope = EventSubscriptionScope.Scene)
            where TEnum : Enum;
    }

    /// <summary>게임 이벤트를 타입 안전하게 발행하는 쓰기 전용 계약.</summary>
    public interface IGameEventPublisher : IGameService
    {
        void Send<TEnum, TData>(TEnum eventType, TData data)
            where TEnum : Enum
            where TData : IEventData;

        void Send<TEnum>(TEnum eventType)
            where TEnum : Enum;
    }

    public enum EventSubscriptionScope
    {
        Scene,
        Global,
    }
}
