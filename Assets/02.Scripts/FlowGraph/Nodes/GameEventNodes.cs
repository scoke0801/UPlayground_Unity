using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UPlayGround.Manager;

namespace UPlayGround.FlowGraph
{
    /// <summary>
    /// EventManager의 enum 이벤트를 그래프에서 저작하기 위한 직렬화 참조.
    /// EventManager API가 컴파일 타임 enum 제네릭이므로, 타입/값 이름을 저장했다가
    /// 런타임에 리플렉션으로 닫힌 제네릭 Subscribe/Send를 호출한다.
    /// </summary>
    [Serializable]
    public sealed class GameEventRef
    {
        [Tooltip("이벤트 enum 타입의 FullName (예: UPlayGround.Data.Event.GameMilestoneEvent)")]
        public string enumTypeName = "UPlayGround.Data.Event.GameMilestoneEvent";

        [Tooltip("enum 값 이름 (예: CombatStarted)")]
        public string valueName;

        public override string ToString()
        {
            int dot = enumTypeName?.LastIndexOf('.') ?? -1;
            string shortType = dot >= 0 ? enumTypeName.Substring(dot + 1) : enumTypeName;
            return $"{shortType}.{valueName}";
        }

        public bool TryResolve(out Type enumType, out object enumValue)
        {
            enumType = ResolveEnumType(enumTypeName);
            enumValue = null;
            if (enumType == null || string.IsNullOrEmpty(valueName))
                return false;

            try
            {
                enumValue = Enum.Parse(enumType, valueName);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        private static Type ResolveEnumType(string fullName)
        {
            if (string.IsNullOrEmpty(fullName))
                return null;

            Type type = Type.GetType(fullName);
            if (type == null)
            {
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = assembly.GetType(fullName);
                    if (type != null)
                        break;
                }
            }
            return type != null && type.IsEnum ? type : null;
        }
    }

    /// <summary>IGameEventObservable/Publisher의 enum 제네릭 메서드를 닫아 호출하는 리플렉션 브릿지.</summary>
    internal static class GameEventReflection
    {
        // Subscribe<TEnum>(TEnum, Action, EventSubscriptionScope) — 제네릭 인자 1개(payload 없는 오버로드)
        private static readonly MethodInfo SubscribeMethod = FindMethod(
            typeof(IGameEventObservable), nameof(IGameEventObservable.Subscribe), 1, 3);

        // Send<TEnum>(TEnum) — 제네릭 인자 1개
        private static readonly MethodInfo SendMethod = FindMethod(
            typeof(IGameEventPublisher), nameof(IGameEventPublisher.Send), 1, 1);

        private static MethodInfo FindMethod(Type owner, string name, int genericArgCount, int paramCount)
        {
            foreach (MethodInfo method in owner.GetMethods())
            {
                if (method.Name == name
                    && method.IsGenericMethodDefinition
                    && method.GetGenericArguments().Length == genericArgCount
                    && method.GetParameters().Length == paramCount)
                {
                    return method;
                }
            }
            return null;
        }

        /// <summary>이벤트를 구독하고 해제용 IDisposable을 반환한다. 실패 시 null.</summary>
        public static IDisposable Subscribe(GameEventRef eventRef, Action handler, EventSubscriptionScope scope)
        {
            IGameEventObservable events = Svc.Events;
            if (events == null || SubscribeMethod == null || !eventRef.TryResolve(out Type enumType, out object enumValue))
            {
                Debug.LogWarning($"[FlowGraph] 이벤트 구독 실패: {eventRef}");
                return null;
            }

            return (IDisposable)SubscribeMethod
                .MakeGenericMethod(enumType)
                .Invoke(events, new object[] { enumValue, handler, scope });
        }

        public static bool Send(GameEventRef eventRef)
        {
            IGameEventPublisher publisher = Svc.EventPublisher;
            if (publisher == null || SendMethod == null || !eventRef.TryResolve(out Type enumType, out object enumValue))
            {
                Debug.LogWarning($"[FlowGraph] 이벤트 발행 실패: {eventRef}");
                return false;
            }

            SendMethod.MakeGenericMethod(enumType).Invoke(publisher, new[] { enumValue });
            return true;
        }
    }

    /// <summary>특정 게임 이벤트 발행 시 발화하는 진입점.</summary>
    [FlowNodeMenu("진입점/OnGameEvent", Summary = "지정 GameEvent가 발행될 때 시작합니다.", Keywords = new[] { "event", "entry", "subscribe" })]
    [Serializable]
    public sealed class OnGameEventEntryNode : EntryNode
    {
        public GameEventRef gameEvent = new();
        public EventSubscriptionScope scope = EventSubscriptionScope.Scene;

        public override string DisplayName => $"Entry: Event [{gameEvent}]";

        public override void Arm(FlowGraphRunner runner)
        {
            IDisposable subscription = GameEventReflection.Subscribe(
                gameEvent, () => runner.FireEntry(this), scope);
            if (subscription != null)
                runner.StoreEntryTeardown(this, subscription.Dispose);
        }
    }

    /// <summary>게임 이벤트를 발행한다.</summary>
    [FlowNodeMenu("이벤트/PublishGameEvent", Summary = "EventManager에 GameEvent를 발행합니다.", Keywords = new[] { "event", "publish", "send", "발행" })]
    [Serializable]
    public sealed class PublishGameEventNode : FlowNode
    {
        public GameEventRef gameEvent = new();

        public override string DisplayName => $"Publish [{gameEvent}]";

        public override IEnumerable<FlowPortDef> Ports
        {
            get
            {
                yield return FlowPortDef.Input();
                yield return FlowPortDef.Output();
            }
        }

        public override IEnumerator Execute(FlowToken token)
        {
            GameEventReflection.Send(gameEvent);
            token.Emit(FlowPort.Out);
            yield break;
        }
    }

    /// <summary>게임 이벤트가 발행될 때까지 토큰을 보류한다.</summary>
    [FlowNodeMenu("이벤트/WaitForGameEvent", Summary = "지정 GameEvent가 발행될 때까지 대기합니다.", Keywords = new[] { "event", "wait", "listen", "대기" })]
    [Serializable]
    public sealed class WaitForGameEventNode : FlowNode
    {
        public GameEventRef gameEvent = new();
        public EventSubscriptionScope scope = EventSubscriptionScope.Scene;

        public override string DisplayName => $"WaitEvent [{gameEvent}]";

        public override IEnumerable<FlowPortDef> Ports
        {
            get
            {
                yield return FlowPortDef.Input();
                yield return FlowPortDef.Output();
            }
        }

        public override IEnumerator Execute(FlowToken token)
        {
            bool received = false;
            IDisposable subscription = GameEventReflection.Subscribe(
                gameEvent, () => received = true, scope);

            if (subscription == null)
            {
                // 구독 불가 시 고착 대신 통과 (경고는 리플렉션 브릿지가 출력)
                token.Emit(FlowPort.Out);
                yield break;
            }

            try
            {
                while (!received && !token.Context.Cancelled)
                    yield return null;
            }
            finally
            {
                subscription.Dispose();
            }

            token.Emit(FlowPort.Out);
        }
    }
}
