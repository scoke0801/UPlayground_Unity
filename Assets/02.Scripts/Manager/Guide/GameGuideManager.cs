using System;
using System.Collections.Generic;
using UPlayGround.Data.Event;
using UPlayGround.Data.Save;
using UPlayGround.Data.UI;
using UPlayGround.UI.Guide;

namespace UPlayGround.Manager
{
    /// <summary>
    /// 게임 이정표 이벤트를 관측해 세이브 슬롯별 최초 가이드를 순차 출력한다.
    /// 이벤트 발행 시스템과 직접 결합하지 않고 IGameEventObservable 계약만 사용한다.
    /// </summary>
    public sealed class GameGuideManager : BaseManager<GameGuideManager>,
        IManager, ISaveable, IUpdatableManager, IGameEventObserver<GameMilestoneEvent>
    {
        private const string LegacyCombatGuideId = "Guide.Combat";
        private const string LegacyCompanionGuideId = "Guide.Companion";
        private const string LegacyEquipmentGuideId = "Guide.Equipment";

        private readonly Queue<FirstTimeGuideEntry> _pendingGuides = new();
        private readonly HashSet<string> _shownGuideIds =
            new(StringComparer.Ordinal);
        private readonly List<IDisposable> _subscriptions = new();
        private FirstTimeGuideConfigSO _config;
        private IGameEventObservable _events;

        public void Init()
        {
            SaveManager.Instance.RegisterSaveable(this);
        }

        public void AfterInit()
        {
            _config = QuestManager.Instance.FirstTimeGuideConfig;
            _events = EventManager.Instance;
            SubscribeConfiguredMilestones();
        }

        private void SubscribeConfiguredMilestones()
        {
            for (int i = 0; i < _subscriptions.Count; i++)
                _subscriptions[i]?.Dispose();
            _subscriptions.Clear();

            if (_config?.Entries == null)
                return;

            var observed = new HashSet<GameMilestoneEvent>();
            for (int i = 0; i < _config.Entries.Count; i++)
            {
                FirstTimeGuideEntry entry = _config.Entries[i];
                if (entry?.IsValid == true
                    && observed.Add(entry.MilestoneEvent))
                {
                    Observe(entry.MilestoneEvent);
                }
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < _subscriptions.Count; i++)
                _subscriptions[i]?.Dispose();

            _subscriptions.Clear();
            _pendingGuides.Clear();
            _events = null;
            _config = null;
        }

        private void Observe(GameMilestoneEvent eventType)
        {
            if (_events == null)
                return;

            _subscriptions.Add(_events.Observe(
                eventType,
                this,
                EventSubscriptionScope.Global));
        }

        public void OnEvent(GameMilestoneEvent eventType)
        {
            if (_config != null
                && _config.TryGet(eventType, out FirstTimeGuideEntry entry))
            {
                EnqueueOnce(entry);
            }
        }

        public void OnUpdate()
        {
            if (_pendingGuides.Count == 0 || GuidePopupRuntime.IsOpen())
                return;

            FirstTimeGuideEntry entry = _pendingGuides.Peek();
            GuidePopupDataSO data = entry?.Popup;
            if (data == null)
            {
                _pendingGuides.Dequeue();
                return;
            }

            if (GuidePopupRuntime.Open(data) == null)
                return;

            _pendingGuides.Dequeue();
            _shownGuideIds.Add(entry.GuideId);
        }

        public void OnFixedUpdate() { }
        public void OnLateUpdate() { }
        public void OnSceneChanged(string sceneType)
        {
            SubscribeConfiguredMilestones();
        }

        private void EnqueueOnce(FirstTimeGuideEntry entry)
        {
            if (entry?.IsValid != true
                || _shownGuideIds.Contains(entry.GuideId)
                || ContainsPending(entry.GuideId))
            {
                return;
            }

            _pendingGuides.Enqueue(entry);
        }

        private bool ContainsPending(string guideId)
        {
            foreach (FirstTimeGuideEntry pending in _pendingGuides)
            {
                if (string.Equals(
                        pending?.GuideId,
                        guideId,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public void ExportSaveData(GameSaveData saveData)
        {
            saveData.firstTimeGuide ??= new FirstTimeGuideSaveData();
            saveData.firstTimeGuide.shownGuideIds =
                new List<string>(_shownGuideIds);
        }

        public void ImportSaveData(GameSaveData saveData)
        {
            _pendingGuides.Clear();
            _shownGuideIds.Clear();
            FirstTimeGuideSaveData data = saveData?.firstTimeGuide;
            if (data?.shownGuideIds is { Count: > 0 })
            {
                for (int i = 0; i < data.shownGuideIds.Count; i++)
                {
                    string guideId = data.shownGuideIds[i]?.Trim();
                    if (!string.IsNullOrEmpty(guideId))
                        _shownGuideIds.Add(guideId);
                }
                return;
            }

            // 구 세이브의 개별 bool을 안정 ID 목록으로 1회 호환한다.
            if (data?.combatGuideShown == true)
                _shownGuideIds.Add(LegacyCombatGuideId);
            if (data?.companionGuideShown == true)
                _shownGuideIds.Add(LegacyCompanionGuideId);
            if (data?.equipmentGuideShown == true)
                _shownGuideIds.Add(LegacyEquipmentGuideId);
        }

        public void ResetForNewGame()
        {
            _pendingGuides.Clear();
            _shownGuideIds.Clear();
        }
    }
}
