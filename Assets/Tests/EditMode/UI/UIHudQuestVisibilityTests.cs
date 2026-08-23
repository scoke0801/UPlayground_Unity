using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UPlayGround.Data.Event;
using UPlayGround.Data.Quest;
using UPlayGround.Manager;

namespace UPlayGround.UI.Tests
{
    public sealed class UIHudQuestVisibilityTests
    {
        private GameObject _hudObject;
        private GameObject _markerHost;
        private GameObject _worldMarkerBridgeObject;
        private QuestSO _quest;
        private FakeQuestService _questService;

        [SetUp]
        public void SetUp()
        {
            Services.Clear();
            WorldMarkerRegistry.Clear();
            Services.Register(new FakeEventObservable());
            _questService = new FakeQuestService();
            Services.Register(_questService);
        }

        [TearDown]
        public void TearDown()
        {
            if (_worldMarkerBridgeObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_worldMarkerBridgeObject);
            }

            if (_markerHost != null)
            {
                UnityEngine.Object.DestroyImmediate(_markerHost);
            }

            if (_hudObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_hudObject);
            }

            if (_quest != null)
            {
                UnityEngine.Object.DestroyImmediate(_quest);
            }

            WorldMarkerRegistry.Clear();
            Services.Clear();
        }

        [Test]
        public void 퀘스트가없으면_루트알파가복구되어도_내용패널은숨긴다()
        {
            UI_HUD_Quest hud = CreateHud(out GameObject titlePanel, out GameObject detailPanel);

            hud.Show();
            _hudObject.GetComponent<CanvasGroup>().alpha = 1f;

            Assert.IsFalse(titlePanel.activeSelf);
            Assert.IsFalse(detailPanel.activeSelf);
        }

        [Test]
        public void 표시할메인퀘스트가있으면_내용패널을표시한다()
        {
            _quest = ScriptableObject.CreateInstance<QuestSO>();
            _quest.questId = "quest_main_visibility_test";
            _quest.questName = "표시 테스트";
            _questService.ActiveQuests.Add(new QuestRuntimeData(_quest));

            UI_HUD_Quest hud = CreateHud(out GameObject titlePanel, out GameObject detailPanel);

            hud.Show();

            Assert.IsTrue(titlePanel.activeSelf);
            Assert.IsTrue(detailPanel.activeSelf);
        }

        [Test]
        public void 메인분류퀘스트는_레거시서브접두사여도_내용패널을표시한다()
        {
            _quest = ScriptableObject.CreateInstance<QuestSO>();
            _quest.questId = "quest_sub_promoted_main_test";
            _quest.questName = "승격된 메인 퀘스트";
            _quest.questType = QuestType.Main;
            _questService.ActiveQuests.Add(new QuestRuntimeData(_quest));

            UI_HUD_Quest hud = CreateHud(out GameObject titlePanel, out GameObject detailPanel);

            hud.Show();

            Assert.IsTrue(titlePanel.activeSelf);
            Assert.IsTrue(detailPanel.activeSelf);
        }

        [Test]
        public void 지역에도착하면_미니맵위치는유지하고_월드마커만숨긴다()
        {
            const string locationId = "encounter_marker_visibility_test";
            _quest = ScriptableObject.CreateInstance<QuestSO>();
            _quest.questId = "quest_sub_marker_visibility_test";
            _quest.objectives.Add(new QuestObjectiveData
            {
                objectiveId = "reach_encounter",
                markerLocationId = locationId,
                requiredCount = 1,
            });

            var runtime = new QuestRuntimeData(_quest);
            _questService.ActiveQuests.Add(runtime);
            _questService.TrackedQuest = runtime;

            _markerHost = new GameObject("QuestAreaMarker");
            MinimapMarkerRegistrar registrar = MinimapMarkerRegistrar.Install(
                _markerHost,
                locationId,
                MinimapMarkerType.QuestTarget);

            _worldMarkerBridgeObject = new GameObject("QuestWorldMarkerBridge");
            _worldMarkerBridgeObject.AddComponent<QuestWorldMarkerBridge>();

            Assert.IsTrue(MinimapMarkerRegistry.TryGet(locationId, out _));
            Assert.IsTrue(WorldMarkerRegistry.Contains($"quest:{locationId}"));

            registrar.SetWorldMarkerVisible(false);

            Assert.IsTrue(MinimapMarkerRegistry.TryGet(locationId, out _));
            Assert.IsFalse(WorldMarkerRegistry.Contains($"quest:{locationId}"));

            registrar.SetWorldMarkerVisible(true);

            Assert.IsTrue(WorldMarkerRegistry.Contains($"quest:{locationId}"));
        }

        private UI_HUD_Quest CreateHud(out GameObject titlePanel, out GameObject detailPanel)
        {
            _hudObject = new GameObject(
                "UI_HUD_Quest",
                typeof(RectTransform),
                typeof(Canvas));

            var titleContainer = new GameObject("Image", typeof(RectTransform));
            titleContainer.transform.SetParent(_hudObject.transform, false);
            titlePanel = new GameObject("QuestTitlePanel", typeof(RectTransform));
            titlePanel.transform.SetParent(titleContainer.transform, false);

            detailPanel = new GameObject("QuestDetailPanel", typeof(RectTransform));
            detailPanel.transform.SetParent(_hudObject.transform, false);

            return _hudObject.AddComponent<UI_HUD_Quest>();
        }

        private sealed class FakeQuestService : IUIQuestService
        {
            public bool IsDBLoaded => true;
            public bool IsQuestTrackingSuppressed => false;
            public List<QuestRuntimeData> ActiveQuests { get; } = new();
            public QuestRuntimeData TrackedQuest { get; set; }

            public IEnumerable<QuestRuntimeData> GetActiveQuests() => ActiveQuests;
            public QuestRuntimeData GetActiveQuestRuntime(string questId) => null;
            public QuestRuntimeData GetTrackedQuestRuntime() => TrackedQuest;
            public QuestSO GetQuestData(string questId) => null;
            public List<QuestSO> GetAvailableQuests() => new();
            public List<QuestSO> GetCompletedQuests() => new();
            public List<QuestSO> GetFailedQuests() => new();
            public bool IsQuestTracked(string questId) => false;
            public bool TrackQuest(string questId) => false;
            public bool UntrackQuest() => false;
            public bool CompleteQuest(string questId) => false;
            public bool AbandonQuest(string questId) => false;
        }

        private sealed class FakeEventObservable : IGameEventObservable
        {
            public IDisposable Subscribe<TEnum, TData>(
                TEnum eventType,
                Action<TData> handler,
                EventSubscriptionScope scope = EventSubscriptionScope.Scene)
                where TEnum : Enum
                where TData : IEventData => EmptySubscription.Instance;

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
        }

        private sealed class EmptySubscription : IDisposable
        {
            public static readonly EmptySubscription Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
