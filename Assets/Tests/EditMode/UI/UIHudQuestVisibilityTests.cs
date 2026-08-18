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
        private QuestSO _quest;
        private FakeQuestService _questService;

        [SetUp]
        public void SetUp()
        {
            Services.Clear();
            Services.Register(new FakeEventObservable());
            _questService = new FakeQuestService();
            Services.Register(_questService);
        }

        [TearDown]
        public void TearDown()
        {
            if (_hudObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_hudObject);
            }

            if (_quest != null)
            {
                UnityEngine.Object.DestroyImmediate(_quest);
            }

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

            public IEnumerable<QuestRuntimeData> GetActiveQuests() => ActiveQuests;
            public QuestRuntimeData GetActiveQuestRuntime(string questId) => null;
            public QuestRuntimeData GetTrackedQuestRuntime() => null;
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
