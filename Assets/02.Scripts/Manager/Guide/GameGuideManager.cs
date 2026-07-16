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
        private enum GuideType
        {
            Combat,
            Companion,
            Equipment,
        }

        private readonly Queue<GuideType> _pendingGuides = new();
        private readonly List<IDisposable> _subscriptions = new();
        private FirstTimeGuideConfigSO _config;
        private IGameEventObservable _events;
        private bool _combatGuideShown;
        private bool _companionGuideShown;
        private bool _equipmentGuideShown;

        public void Init()
        {
            SaveManager.Instance.RegisterSaveable(this);
        }

        public void AfterInit()
        {
            _config = QuestManager.Instance.FirstTimeGuideConfig;
            _events = EventManager.Instance;

            Observe(GameMilestoneEvent.CombatStarted);
            Observe(GameMilestoneEvent.CharacterUnlocked);
            Observe(GameMilestoneEvent.EquipmentAcquired);
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
            GuideType? guideType = eventType switch
            {
                GameMilestoneEvent.CombatStarted => GuideType.Combat,
                GameMilestoneEvent.CharacterUnlocked => GuideType.Companion,
                GameMilestoneEvent.EquipmentAcquired => GuideType.Equipment,
                _ => null,
            };

            if (guideType.HasValue)
                EnqueueOnce(guideType.Value);
        }

        public void OnUpdate()
        {
            if (_pendingGuides.Count == 0 || GuidePopupRuntime.IsOpen())
                return;

            GuideType type = _pendingGuides.Peek();
            GuidePopupDataSO data = GetGuideData(type);
            if (data == null)
            {
                _pendingGuides.Dequeue();
                return;
            }

            if (GuidePopupRuntime.Open(data) == null)
                return;

            _pendingGuides.Dequeue();
            MarkShown(type);
        }

        public void OnFixedUpdate() { }
        public void OnLateUpdate() { }
        public void OnSceneChanged(string sceneType) { }

        private void EnqueueOnce(GuideType type)
        {
            if (IsShown(type) || _pendingGuides.Contains(type))
                return;

            _pendingGuides.Enqueue(type);
        }

        private GuidePopupDataSO GetGuideData(GuideType type) => type switch
        {
            GuideType.Combat => _config?.CombatGuide,
            GuideType.Companion => _config?.CompanionGuide,
            GuideType.Equipment => _config?.EquipmentGuide,
            _ => null,
        };

        private bool IsShown(GuideType type) => type switch
        {
            GuideType.Combat => _combatGuideShown,
            GuideType.Companion => _companionGuideShown,
            GuideType.Equipment => _equipmentGuideShown,
            _ => true,
        };

        private void MarkShown(GuideType type)
        {
            switch (type)
            {
                case GuideType.Combat: _combatGuideShown = true; break;
                case GuideType.Companion: _companionGuideShown = true; break;
                case GuideType.Equipment: _equipmentGuideShown = true; break;
            }
        }

        public void ExportSaveData(GameSaveData saveData)
        {
            saveData.firstTimeGuide ??= new FirstTimeGuideSaveData();
            saveData.firstTimeGuide.combatGuideShown = _combatGuideShown;
            saveData.firstTimeGuide.companionGuideShown = _companionGuideShown;
            saveData.firstTimeGuide.equipmentGuideShown = _equipmentGuideShown;
        }

        public void ImportSaveData(GameSaveData saveData)
        {
            _pendingGuides.Clear();
            FirstTimeGuideSaveData data = saveData?.firstTimeGuide;
            _combatGuideShown = data?.combatGuideShown ?? false;
            _companionGuideShown = data?.companionGuideShown ?? false;
            _equipmentGuideShown = data?.equipmentGuideShown ?? false;
        }

        public void ResetForNewGame()
        {
            _pendingGuides.Clear();
            _combatGuideShown = false;
            _companionGuideShown = false;
            _equipmentGuideShown = false;
        }
    }
}
