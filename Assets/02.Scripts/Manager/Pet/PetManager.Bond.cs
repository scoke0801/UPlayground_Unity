using System;
using UnityEngine;
using UPlayGround.Data.Pet;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Quest;

namespace UPlayGround.Manager
{
    public sealed partial class PetManager
    {
        private IDisposable _questCompletedSubscription;
        private IDisposable _questObjectiveSubscription;
        private ICombatEncounterService _encounters;
        private IGroundDropService _drops;
        private CombatEncounterKind _encounterKind;
        private float _lastBondTick;
        private float _lastActivity = float.NegativeInfinity;
        private float _lastCombatActivity = float.NegativeInfinity;
        private Vector3 _lastPlayerPosition;
        private PlayerActor _activityPlayer;
        public int BondStage => Catalog?.skills?.bondProgression?.GetStage(_save.sharedBondPoints) ?? 1;
        public int SupportSlotCount => Catalog?.skills?.bondProgression?.GetSlotCount(_save.sharedBondPoints) ?? 1;
        public int SignatureActivationVersion => _support?.SignatureActivationVersion ?? 0;
        public int LastActivatedSupportSlot => _support?.LastActivatedSlot ?? 0;
        public string GetSupportId(int slot) => slot >= 0 && slot < _save.supportSlots.Count ? _save.supportSlots[slot] : null;

        private void SubscribeBond()
        {
            UnsubscribeBond();
            _lastBondTick = Time.unscaledTime;
            if (Services.TryGet<IGameEventObservable>(out var events))
            {
                _questCompletedSubscription = events.Subscribe<QuestEvent, QuestStateEventData>(
                    QuestEvent.QuestCompleted, OnBondQuestCompleted, EventSubscriptionScope.Global);
                _questObjectiveSubscription = events.Subscribe<QuestEvent, QuestObjectiveEventData>(
                    QuestEvent.QuestObjectiveUpdated, OnBondObjective, EventSubscriptionScope.Global);
            }
            if (Services.TryGet<ICombatEncounterService>(out _encounters))
            {
                _encounters.EncounterChanged += OnBondEncounter;
                _encounterKind = _encounters.CurrentEncounter;
                if (_encounterKind == CombatEncounterKind.Explore) _save.bondLedger.encounterActive = false;
                if (!_save.bondLedger.encounterActive && _encounterKind != CombatEncounterKind.Explore)
                    BeginBondEncounter();
            }
            if (Services.TryGet<IGroundDropService>(out _drops)) _drops.Collected += OnBondCollected;
        }
        private void UnsubscribeBond()
        {
            if (_activityPlayer != null) _activityPlayer.PetBondActivity -= OnBondPlayerActivity;
            if (_activityPlayer != null) _activityPlayer.PetBondCombatAction -= OnBondCombatAction;
            _questCompletedSubscription?.Dispose(); _questCompletedSubscription = null;
            _questObjectiveSubscription?.Dispose(); _questObjectiveSubscription = null;
            if (_encounters != null) _encounters.EncounterChanged -= OnBondEncounter;
            if (_drops != null) _drops.Collected -= OnBondCollected;
            _encounters = null; _drops = null;
            _activityPlayer = null; _lastActivity = float.NegativeInfinity;
            _lastCombatActivity = float.NegativeInfinity;
        }
        private bool CanCollectBond => _ready && State == PetRuntimeState.Ready && _save.isAccompanying
            && _actor != null && _actor.CanSupport && _owner != null && _owner.IsAlive()
            && Time.timeScale > 0f && CanReactToSupport();
        // 퀘스트 성과는 대화·보상 화면에서도 인정하되 명시적 동행 금지와 씬 전환은 허용하지 않는다.
        private bool CanCollectQuestBond => _ready && _save.isAccompanying && _actor != null
            && _owner != null && _owner.IsAlive() && _suspensions.Count == 0 && Time.timeScale > 0f
            && (_sceneType == SceneType.GamePlay || _sceneType == SceneType.Test)
            && !SceneManager.Instance.IsLoading && !SaveManager.Instance.IsPreparingSceneLoad;
        private void OnBondCollected(string id) { if (CanCollectBond) _lastActivity = Time.unscaledTime; }
        private void OnBondPlayerActivity() { if (CanCollectBond) _lastActivity = Time.unscaledTime; }
        private void OnBondCombatAction()
        {
            if (!CanCollectBond || !_save.bondLedger.encounterActive) return;
            _lastCombatActivity = Time.unscaledTime;
            _save.bondLedger.encounterHadAction = true;
        }
        private void TickBond()
        {
            float now = Time.unscaledTime;
            float elapsed = Mathf.Clamp(now - _lastBondTick, 0f, .5f);
            _lastBondTick = now;
            var player = ActorSvc.Objects?.Player;
            if (_activityPlayer != player)
            {
                if (_activityPlayer != null) _activityPlayer.PetBondActivity -= OnBondPlayerActivity;
                if (_activityPlayer != null) _activityPlayer.PetBondCombatAction -= OnBondCombatAction;
                _activityPlayer = player;
                if (player != null) { player.PetBondActivity += OnBondPlayerActivity; player.PetBondCombatAction += OnBondCombatAction; _lastPlayerPosition = player.transform.position; }
                _lastActivity = float.NegativeInfinity;
                _lastCombatActivity = float.NegativeInfinity;
                return;
            }
            var data = Catalog?.skills?.bondProgression;
            float distance = player != null ? Vector3.Distance(_lastPlayerPosition, player.transform.position) : 0f;
            if (player != null) _lastPlayerPosition = player.transform.position;
            bool eligible = CanCollectBond && data != null;
            if (eligible && Svc.Input?.ReadPlayerMoveInput().sqrMagnitude > .01f
                && distance >= data.movementMinimumDistance && distance <= data.movementMaximumSampleDistance)
                _lastActivity = now;
            var ledger = _save.bondLedger;
            if (ledger.encounterActive && Time.timeScale > 0f && CanReactToSupport())
            {
                ledger.encounterSeconds += elapsed;
                if (eligible && CanSupport) ledger.encounterPresentSeconds += elapsed;
                _revision++; HasUnsavedChanges = true;
            }
            if (!eligible || (now - _lastActivity > data.recentActivitySeconds
                && now - _lastCombatActivity > data.recentActivitySeconds)) return;
            float points = PetBondRules.AdvanceActivity(ledger, data, elapsed,
                now - _lastActivity <= data.recentActivitySeconds, now - _lastCombatActivity <= data.recentActivitySeconds);
            _revision++; HasUnsavedChanges = true;
            if (points > 0f) AwardBond(PetBondSource.Lifestyle, points);
        }
        private void AwardBond(PetBondSource source, float amount, bool encounter = false)
        {
            var data = Catalog?.skills?.bondProgression;
            if (data == null) return;
            long previous = _save.sharedBondPoints;
            int day = Services.TryGet<IGameTimeService>(out var clock) ? clock.CurrentDay : 0;
            if (PetBondRules.Award(_save, data, source, amount, day, encounter) <= 0f) return;
            _revision++; HasUnsavedChanges = true;
            if (_save.sharedBondPoints != previous) Changed?.Invoke();
        }
        private void OnBondSupport(PetSupportTriggerKind trigger)
        {
            if (!CanCollectBond || _encounterKind == CombatEncounterKind.Explore) return;
            if (trigger == PetSupportTriggerKind.HealthBelow) return;
            var data = Catalog.skills.bondProgression;
            if (data == null) return;
            float points = data.GetSupportPoints(trigger, Time.unscaledTime - _lastActivity <= data.recentActivitySeconds
                || Time.unscaledTime - _lastCombatActivity <= data.recentActivitySeconds);
            if (trigger == PetSupportTriggerKind.PerfectDefense || trigger == PetSupportTriggerKind.EnemyBroken)
            {
                _lastCombatActivity = Time.unscaledTime;
                _save.bondLedger.encounterHadAction = true;
            }
            AwardBond(PetBondSource.Combat, points * EncounterWeight(data), true);
        }
        private float EncounterWeight(PetBondProgressionSO data) => _encounterKind == CombatEncounterKind.Duel
            ? data.duelWeight : _encounterKind == CombatEncounterKind.Sweep ? data.sweepWeight : 0f;
        private void BeginBondEncounter()
        {
            var ledger = _save.bondLedger;
            ledger.encounterActive = true; ledger.encounterEarned = 0f;
            ledger.encounterSeconds = ledger.encounterPresentSeconds = 0f; ledger.encounterHadAction = false;
        }
        private void OnBondEncounter(CombatEncounterKind kind)
        {
            var ledger = _save.bondLedger;
            var data = Catalog?.skills?.bondProgression;
            if (kind == CombatEncounterKind.Explore && ledger.encounterActive)
            {
                if (CanCollectBond && data != null && ledger.encounterHadAction
                    && ledger.encounterSeconds >= data.minimumEncounterSeconds)
                    AwardBond(PetBondSource.Combat, data.encounterPoints * EncounterWeight(data)
                        * Mathf.Clamp01(ledger.encounterPresentSeconds / ledger.encounterSeconds), true);
                ledger.encounterActive = false;
            }
            else if (kind != CombatEncounterKind.Explore && !ledger.encounterActive) BeginBondEncounter();
            _encounterKind = kind;
        }
        private void OnBondQuestCompleted(QuestStateEventData data)
        {
            if (!CanCollectQuestBond || Catalog?.skills?.bondProgression == null || data == null
                || !PetBondRules.Claim(_save.bondLedger, data.QuestId, null)) return;
            AwardBond(PetBondSource.Quest, Catalog.skills.bondProgression.questPoints);
        }
        private void OnBondObjective(QuestObjectiveEventData data)
        {
            if (!CanCollectQuestBond || Catalog?.skills?.bondProgression == null || data == null
                || data.RequiredCount <= 0 || data.CurrentCount < data.RequiredCount
                || string.IsNullOrWhiteSpace(data.ObjectiveId)
                || !PetBondRules.Claim(_save.bondLedger, data.QuestId, data.ObjectiveId)) return;
            AwardBond(PetBondSource.Quest, Catalog.skills.bondProgression.objectivePoints);
        }
    }
}
