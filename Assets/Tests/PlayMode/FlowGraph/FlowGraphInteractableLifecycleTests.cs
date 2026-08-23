using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;
using UPlayGround.Data.Quest;
using UPlayGround.Manager;

namespace UPlayGround.FlowGraph.PlayModeTests
{
    public sealed class FlowGraphInteractableLifecycleTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string RequiredQuestId = "quest_test_flow_interactable";

        private GameObject _root;
        private FakeQuestFlowService _questFlow;
        private FakeGlobalFlagService _flags;
        private FakeEventObservable _events;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Services.Clear();
            _questFlow = new FakeQuestFlowService();
            _flags = new FakeGlobalFlagService();
            _events = new FakeEventObservable();
            Services.Register(_questFlow);
            Services.Register(_flags);
            Services.Register(_events);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_root != null)
                UnityEngine.Object.Destroy(_root);

            yield return null;
            Services.Clear();
        }

        [UnityTest]
        public IEnumerator 씬_구독_정리_뒤에도_퀘스트_활성화가_조사_대상을_연다()
        {
            int interactableLayer = LayerMask.NameToLayer("InteractableObject");
            int triggerLayer = LayerMask.NameToLayer("Trigger");
            Assert.That(interactableLayer, Is.GreaterThanOrEqualTo(0));
            Assert.That(triggerLayer, Is.GreaterThanOrEqualTo(0));

            _root = new GameObject("FlowGraphInteractable_LifecycleTest");
            _root.SetActive(false);
            _root.layer = interactableLayer;

            var visual = new GameObject("AvailableVisual");
            visual.transform.SetParent(_root.transform, false);
            BoxCollider collider = _root.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            FlowGraphTriggerVolume volume = _root.AddComponent<FlowGraphTriggerVolume>();

            Type interactableType = Type.GetType(
                "UPlayGround.Gameplay.Interaction.FlowGraphInteractable, Assembly-CSharp");
            Assert.That(interactableType, Is.Not.Null);
            Component interactable = _root.AddComponent(interactableType);
            SetPrivateField(interactable, "_flowVolume", volume);
            SetPrivateField(interactable, "_requiredQuestId", RequiredQuestId);
            SetPrivateField(interactable, "_availableVisuals", new[] { visual });

            _root.SetActive(true);
            yield return null;

            Assert.That(_events.RequestedScopes, Has.Count.EqualTo(3));
            Assert.That(_events.RequestedScopes, Is.All.EqualTo(EventSubscriptionScope.Global));
            Assert.That(_root.layer, Is.EqualTo(triggerLayer));
            Assert.That(visual.activeSelf, Is.False);
            Assert.That(IsRoutingEnabled(volume), Is.False);

            // 실제 SceneContext.Start와 같이 Scene 범위 구독만 정리한 뒤 첫 퀘스트 이벤트를 보낸다.
            _events.ClearSceneSubscriptions();
            _questFlow.Status = QuestStatus.Active;
            _events.Publish(
                QuestEvent.QuestAccepted,
                new QuestStateEventData { QuestId = RequiredQuestId });
            yield return null;

            Assert.That(_root.layer, Is.EqualTo(interactableLayer));
            Assert.That(visual.activeSelf, Is.True);
            Assert.That(IsRoutingEnabled(volume), Is.True);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, PrivateInstance);
            Assert.That(field, Is.Not.Null, $"필드를 찾지 못했습니다: {fieldName}");
            field.SetValue(target, value);
        }

        private static bool IsRoutingEnabled(FlowGraphTriggerVolume volume)
        {
            FieldInfo field = typeof(FlowGraphTriggerVolume).GetField(
                "_isRoutingEnabled",
                PrivateInstance);
            Assert.That(field, Is.Not.Null);
            return (bool)field.GetValue(volume);
        }

        private sealed class FakeQuestFlowService : IQuestFlowService
        {
            public QuestStatus Status { get; set; } = QuestStatus.Locked;

            public bool AcceptQuest(string questId) => false;
            public bool CompleteQuest(string questId) => false;
            public bool FailQuest(string questId) => false;
            public bool TrackQuest(string questId) => false;
            public void NotifyStoryEvent(string eventId) { }
            public QuestStatus GetQuestStatus(string questId) => Status;
        }

        private sealed class FakeGlobalFlagService : IGlobalFlagService
        {
            private readonly Dictionary<string, bool> _values = new();

            public event Action<string, bool> OnFlagChanged;
            public event Action OnFlagsReloaded;

            public bool GetFlag(string key) =>
                !string.IsNullOrEmpty(key) && _values.TryGetValue(key, out bool value) && value;

            public void SetFlag(string key, bool value)
            {
                if (string.IsNullOrEmpty(key))
                    return;

                _values[key] = value;
                OnFlagChanged?.Invoke(key, value);
            }
        }

        private sealed class FakeEventObservable : IGameEventObservable
        {
            private readonly List<QuestSubscription> _questSubscriptions = new();

            public List<EventSubscriptionScope> RequestedScopes { get; } = new();

            public IDisposable Subscribe<TEnum, TData>(
                TEnum eventType,
                Action<TData> handler,
                EventSubscriptionScope scope = EventSubscriptionScope.Scene)
                where TEnum : Enum
                where TData : IEventData
            {
                RequestedScopes.Add(scope);
                if (typeof(TEnum) != typeof(QuestEvent)
                    || typeof(TData) != typeof(QuestStateEventData))
                {
                    return EmptySubscription.Instance;
                }

                var subscription = new QuestSubscription(
                    (QuestEvent)(object)eventType,
                    data => handler((TData)(object)data),
                    scope,
                    Remove);
                _questSubscriptions.Add(subscription);
                return subscription;
            }

            public IDisposable Subscribe<TEnum>(
                TEnum eventType,
                Action handler,
                EventSubscriptionScope scope = EventSubscriptionScope.Scene)
                where TEnum : Enum => EmptySubscription.Instance;

            public void Unsubscribe<TEnum, TData>(TEnum eventType, Action<TData> handler)
                where TEnum : Enum
                where TData : IEventData
            {
            }

            public void Unsubscribe<TEnum>(TEnum eventType, Action handler)
                where TEnum : Enum
            {
            }

            public IDisposable Observe<TEnum>(
                TEnum eventType,
                IGameEventObserver<TEnum> observer,
                EventSubscriptionScope scope = EventSubscriptionScope.Scene)
                where TEnum : Enum => EmptySubscription.Instance;

            public void ClearSceneSubscriptions() =>
                _questSubscriptions.RemoveAll(
                    subscription => subscription.Scope == EventSubscriptionScope.Scene);

            public void Publish(QuestEvent eventType, QuestStateEventData data)
            {
                QuestSubscription[] snapshot = _questSubscriptions.ToArray();
                for (int i = 0; i < snapshot.Length; i++)
                {
                    QuestSubscription subscription = snapshot[i];
                    if (subscription.EventType == eventType)
                        subscription.Invoke(data);
                }
            }

            private void Remove(QuestSubscription subscription) =>
                _questSubscriptions.Remove(subscription);
        }

        private sealed class QuestSubscription : IDisposable
        {
            private readonly Action<QuestStateEventData> _handler;
            private readonly Action<QuestSubscription> _onDispose;

            public QuestSubscription(
                QuestEvent eventType,
                Action<QuestStateEventData> handler,
                EventSubscriptionScope scope,
                Action<QuestSubscription> onDispose)
            {
                EventType = eventType;
                Scope = scope;
                _handler = handler;
                _onDispose = onDispose;
            }

            public QuestEvent EventType { get; }
            public EventSubscriptionScope Scope { get; }

            public void Invoke(QuestStateEventData data) => _handler(data);
            public void Dispose() => _onDispose(this);
        }

        private sealed class EmptySubscription : IDisposable
        {
            public static readonly EmptySubscription Instance = new();

            public void Dispose() { }
        }
    }
}
