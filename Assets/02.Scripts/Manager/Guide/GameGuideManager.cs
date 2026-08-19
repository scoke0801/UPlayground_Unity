using System;
using System.Collections.Generic;
using UnityEngine;
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
        private float _presentationIdleSeconds;

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
            _presentationIdleSeconds = 0f;
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
            if (_pendingGuides.Count == 0)
                return;

            // 이정표는 대화·연출 도중에도 발행되므로, 연출이 끝날 때까지 출력을 미룬다.
            // (예: 영입 커밋은 필수 대화와 후속 대화 사이에서 CharacterUnlocked를 보낸다)
            if (IsPresentationBusy())
            {
                _presentationIdleSeconds = 0f;
                return;
            }

            // 대화가 연달아 이어지는 구간은 한두 프레임 비어 있을 수 있어 안정화 시간을 둔다.
            _presentationIdleSeconds += Time.unscaledDeltaTime;
            if (_presentationIdleSeconds < SettleSeconds)
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

        private float SettleSeconds =>
            _config != null ? _config.PresentationSettleSeconds : 0f;

        /// <summary>가이드 팝업을 겹쳐 띄우면 안 되는 연출·정지 상태인지 판정한다.</summary>
        private static bool IsPresentationBusy()
        {
            if (GuidePopupRuntime.IsOpen())
                return true;
            if (Svc.Dialogue?.IsDialogueActive == true)
                return true;
            if (Svc.CinematicStage?.IsActive == true)
                return true;
            if (Svc.RecruitmentEncounters?.IsAnyEncounterInPresentation == true)
                return true;

            // 다른 팝업·메뉴가 이미 게임을 멈춘 상태라면 그 화면 위에 겹치지 않는다.
            return Svc.GameTime?.IsPaused == true;
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
            _presentationIdleSeconds = 0f;
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
            _presentationIdleSeconds = 0f;
        }
    }
}
