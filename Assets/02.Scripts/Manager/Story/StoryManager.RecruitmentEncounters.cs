using System;
using System.Collections.Generic;
using UPlayGround.Data.Story;
using UPlayGround.Manager;

namespace UPlayGround.Story
{
    public partial class StoryManager
    {
        private readonly RecruitmentEncounterStateStore _recruitmentStateStore = new();
        private readonly Dictionary<string, IRecruitmentEncounterRuntimePort> _recruitmentRuntimes = new();
        private readonly Dictionary<string, HashSet<Action<RecruitmentEncounterPhase>>> _recruitmentObservers = new();
        private readonly HashSet<string> _activeRecruitmentExecutions = new();
        private int _nextRecruitmentAttemptId;

        private void InitializeRecruitmentEncounters()
        {
            _nextRecruitmentAttemptId = 0;
        }

        private void DisposeRecruitmentEncounters()
        {
            _recruitmentRuntimes.Clear();
            _recruitmentObservers.Clear();
            _activeRecruitmentExecutions.Clear();
        }

        private void ResetRecruitmentEncountersForNewGame()
        {
            _recruitmentStateStore.ResetForNewGame();
            RestoreRegisteredRecruitmentDefinitions();
            _activeRecruitmentExecutions.Clear();
        }

        private void RestoreRegisteredRecruitmentDefinitions()
        {
            foreach (IRecruitmentEncounterRuntimePort runtime in _recruitmentRuntimes.Values)
                RegisterDefinition(runtime);
        }

        public IDisposable RegisterRuntime(IRecruitmentEncounterRuntimePort runtime)
        {
            string encounterId = runtime?.EncounterId?.Trim();
            if (string.IsNullOrEmpty(encounterId)
                || runtime.Definition == null
                || _recruitmentRuntimes.ContainsKey(encounterId)
                || !RegisterDefinition(runtime))
            {
                return null;
            }

            _recruitmentRuntimes.Add(encounterId, runtime);
            runtime.TryApplyPhase(_recruitmentStateStore.GetPhase(encounterId));
            return new ActionLease(() =>
            {
                if (_recruitmentRuntimes.TryGetValue(encounterId, out var registered)
                    && ReferenceEquals(registered, runtime))
                {
                    _recruitmentRuntimes.Remove(encounterId);
                }
            });
        }

        public RecruitmentEncounterPhase GetPhase(string encounterId) =>
            _recruitmentStateStore.GetPhase(encounterId);

        public bool IsEntryReady(string encounterId)
        {
            encounterId = encounterId?.Trim();
            if (string.IsNullOrEmpty(encounterId)
                || !_recruitmentRuntimes.TryGetValue(encounterId, out var runtime))
            {
                return false;
            }

            string prerequisiteId = runtime.Definition?.PrerequisiteEncounterId?.Trim();
            if (!string.IsNullOrEmpty(prerequisiteId)
                && !_recruitmentStateStore.IsCompleted(prerequisiteId))
            {
                return false;
            }

            string requiredFlagKey = runtime.Definition?.RequiredFlagKey?.Trim();
            return string.IsNullOrEmpty(requiredFlagKey)
                   || Svc.Flags?.GetFlag(requiredFlagKey) == true;
        }

        public IReadOnlyList<string> GetDefeatedHostileIds(string encounterId) =>
            _recruitmentStateStore.GetDefeatedHostileIds(encounterId);

        /// <summary>
        /// 전투 전후의 대화·영입 연출을 진행 중인 조우가 있는지 여부.
        /// 전투 구간(CombatActive)은 연출로 보지 않는다 — 전투 중 안내는 그대로 나가야 한다.
        /// </summary>
        public bool IsAnyEncounterInPresentation
        {
            get
            {
                foreach (string encounterId in _activeRecruitmentExecutions)
                {
                    RecruitmentEncounterPhase phase =
                        _recruitmentStateStore.GetPhase(encounterId);
                    if (phase is RecruitmentEncounterPhase.IntroductionPending
                        or RecruitmentEncounterPhase.CombatResolved
                        or RecruitmentEncounterPhase.RecruitmentCommitted)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool TryAcquireExecution(string encounterId, out IDisposable lease)
        {
            lease = null;
            encounterId = encounterId?.Trim();
            if (string.IsNullOrEmpty(encounterId)
                || !_recruitmentStateStore.Contains(encounterId)
                || !_activeRecruitmentExecutions.Add(encounterId))
            {
                return false;
            }

            lease = new ActionLease(() => _activeRecruitmentExecutions.Remove(encounterId));
            return true;
        }

        public RecruitmentEncounterStartResult TryStartOrResume(string encounterId)
        {
            encounterId = encounterId?.Trim();
            if (string.IsNullOrEmpty(encounterId) || !_recruitmentStateStore.Contains(encounterId))
                return RecruitmentEncounterStartResult.UnknownEncounter;
            if (!_recruitmentRuntimes.TryGetValue(encounterId, out var runtime))
                return RecruitmentEncounterStartResult.RuntimeUnavailable;

            RecruitmentEncounterPhase phase = _recruitmentStateStore.GetPhase(encounterId);
            switch (phase)
            {
                case RecruitmentEncounterPhase.Dormant:
                    if (!IsEntryReady(encounterId))
                        return RecruitmentEncounterStartResult.PrerequisiteIncomplete;
                    if (runtime.Definition.CombatMode
                        == RecruitmentEncounterCombatMode.HostileRecruitTarget)
                    {
                        if (!runtime.TryPrepareDialogue()
                            || !_recruitmentStateStore.TryBeginIntroduction(encounterId))
                        {
                            return RecruitmentEncounterStartResult.ActivationFailed;
                        }

                        NotifyRecruitmentPhaseChanged(
                            encounterId,
                            RecruitmentEncounterPhase.IntroductionPending);
                        return RecruitmentEncounterStartResult.IntroductionPending;
                    }
                    if (!runtime.TryActivateCombat()
                        || !_recruitmentStateStore.TryStartCombat(encounterId))
                    {
                        return RecruitmentEncounterStartResult.ActivationFailed;
                    }

                    NotifyRecruitmentPhaseChanged(encounterId, RecruitmentEncounterPhase.CombatActive);
                    return RecruitmentEncounterStartResult.CombatStarted;

                case RecruitmentEncounterPhase.IntroductionPending:
                    return runtime.TryPrepareDialogue()
                        ? RecruitmentEncounterStartResult.IntroductionPending
                        : RecruitmentEncounterStartResult.ActivationFailed;

                case RecruitmentEncounterPhase.CombatActive:
                    if (!runtime.TryActivateCombat())
                        return RecruitmentEncounterStartResult.ActivationFailed;
                    if (AreAllExpectedHostilesDefeated(encounterId, runtime)
                        && _recruitmentStateStore.TryResolveCombat(encounterId))
                    {
                        NotifyRecruitmentPhaseChanged(
                            encounterId,
                            RecruitmentEncounterPhase.CombatResolved);
                        return RecruitmentEncounterStartResult.DialoguePending;
                    }
                    return RecruitmentEncounterStartResult.CombatResumed;

                case RecruitmentEncounterPhase.CombatResolved:
                    return RecruitmentEncounterStartResult.DialoguePending;

                case RecruitmentEncounterPhase.RecruitmentCommitted:
                    return RecruitmentEncounterStartResult.PostDialoguePending;

                case RecruitmentEncounterPhase.Completed:
                    runtime.TryApplyPhase(RecruitmentEncounterPhase.Completed);
                    return RecruitmentEncounterStartResult.AlreadyCompleted;

                default:
                    return RecruitmentEncounterStartResult.UnknownEncounter;
            }
        }

        public IDisposable ObservePhase(
            string encounterId,
            Action<RecruitmentEncounterPhase> observer)
        {
            encounterId = encounterId?.Trim();
            if (string.IsNullOrEmpty(encounterId)
                || observer == null
                || !_recruitmentStateStore.Contains(encounterId))
            {
                return null;
            }

            if (!_recruitmentObservers.TryGetValue(encounterId, out var observers))
            {
                observers = new HashSet<Action<RecruitmentEncounterPhase>>();
                _recruitmentObservers.Add(encounterId, observers);
            }

            observers.Add(observer);
            observer(_recruitmentStateStore.GetPhase(encounterId));
            return new ActionLease(() => observers.Remove(observer));
        }

        public void RecordHostileDefeated(string encounterId, string participantId)
        {
            if (!_recruitmentStateStore.RecordHostileDefeated(encounterId, participantId)
                || !_recruitmentRuntimes.TryGetValue(encounterId, out var runtime))
            {
                return;
            }

            if (!AreAllExpectedHostilesDefeated(encounterId, runtime))
                return;

            if (_recruitmentStateStore.TryResolveCombat(encounterId))
                NotifyRecruitmentPhaseChanged(encounterId, RecruitmentEncounterPhase.CombatResolved);
        }

        public bool TryPrepareDialogue(string encounterId)
        {
            RecruitmentEncounterPhase phase = _recruitmentStateStore.GetPhase(encounterId);
            return (phase is RecruitmentEncounterPhase.IntroductionPending
                       or RecruitmentEncounterPhase.CombatResolved
                       or RecruitmentEncounterPhase.RecruitmentCommitted)
                   && _recruitmentRuntimes.TryGetValue(encounterId, out var runtime)
                   && runtime.TryPrepareDialogue();
        }

        public bool IsDialogueTransitionReady(string encounterId)
        {
            RecruitmentEncounterPhase phase = _recruitmentStateStore.GetPhase(encounterId);
            return (phase is RecruitmentEncounterPhase.IntroductionPending
                       or RecruitmentEncounterPhase.CombatResolved
                       or RecruitmentEncounterPhase.RecruitmentCommitted)
                   && _recruitmentRuntimes.TryGetValue(encounterId, out var runtime)
                   && runtime.IsDialogueTransitionReady;
        }

        public float GetPostCombatSettleSeconds(string encounterId)
        {
            return _recruitmentStateStore.GetPhase(encounterId)
                   != RecruitmentEncounterPhase.IntroductionPending
                   && _recruitmentRuntimes.TryGetValue(encounterId, out var runtime)
                   && runtime.Definition != null
                ? runtime.Definition.PostCombatSettleSeconds
                : 0f;
        }

        public bool TryGetDialoguePartner(string encounterId, out IWorldActor partner)
        {
            if (_recruitmentRuntimes.TryGetValue(encounterId, out var runtime)
                && runtime.DialoguePartner != null
                && runtime.DialoguePartner.Transform != null)
            {
                partner = runtime.DialoguePartner;
                return true;
            }

            partner = null;
            return false;
        }

        public bool TryBeginDialogueAttempt(
            string encounterId,
            out IRecruitmentDialogueAttempt attempt)
        {
            attempt = null;
            if (_recruitmentStateStore.GetPhase(encounterId)
                != RecruitmentEncounterPhase.CombatResolved)
            {
                return false;
            }

            attempt = new RecruitmentDialogueAttempt(
                encounterId,
                ++_nextRecruitmentAttemptId,
                RecruitmentDialoguePurpose.RecruitmentCommit);
            return true;
        }

        public bool TryBeginIntroductionDialogueAttempt(
            string encounterId,
            out IRecruitmentDialogueAttempt attempt)
        {
            attempt = null;
            if (_recruitmentStateStore.GetPhase(encounterId)
                != RecruitmentEncounterPhase.IntroductionPending)
            {
                return false;
            }

            attempt = new RecruitmentDialogueAttempt(
                encounterId,
                ++_nextRecruitmentAttemptId,
                RecruitmentDialoguePurpose.CombatIntroduction);
            return true;
        }

        public void ConfirmDialogueCompleted(IRecruitmentDialogueAttempt attempt)
        {
            if (attempt is RecruitmentDialogueAttempt owned)
                owned.ConfirmCompleted();
        }

        public RecruitmentEncounterStartResult TryStartCombatAfterIntroduction(
            string encounterId,
            IRecruitmentDialogueAttempt completedAttempt)
        {
            RecruitmentEncounterPhase phase = _recruitmentStateStore.GetPhase(encounterId);
            if (phase == RecruitmentEncounterPhase.CombatActive)
                return RecruitmentEncounterStartResult.CombatResumed;
            if (phase != RecruitmentEncounterPhase.IntroductionPending)
                return RecruitmentEncounterStartResult.ActivationFailed;
            if (completedAttempt is not RecruitmentDialogueAttempt owned
                || !owned.IsValidFor(
                    encounterId,
                    RecruitmentDialoguePurpose.CombatIntroduction))
            {
                return RecruitmentEncounterStartResult.InvalidDialogueAttempt;
            }
            if (!owned.IsCompleted)
                return RecruitmentEncounterStartResult.DialogueProofMissing;
            if (!_recruitmentRuntimes.TryGetValue(encounterId, out var runtime)
                || !runtime.TryActivateCombat()
                || !_recruitmentStateStore.TryStartCombat(encounterId))
            {
                return RecruitmentEncounterStartResult.ActivationFailed;
            }

            owned.Consume();
            NotifyRecruitmentPhaseChanged(encounterId, RecruitmentEncounterPhase.CombatActive);
            return RecruitmentEncounterStartResult.CombatStarted;
        }

        public RecruitmentCommitResult TryCommitRecruitment(
            string encounterId,
            IRecruitmentDialogueAttempt completedAttempt)
        {
            RecruitmentEncounterPhase phase = _recruitmentStateStore.GetPhase(encounterId);
            if (phase == RecruitmentEncounterPhase.Completed)
                return RecruitmentCommitResult.AlreadyCompleted;
            if (phase == RecruitmentEncounterPhase.RecruitmentCommitted)
                return RecruitmentCommitResult.AlreadyCommitted;
            if (phase != RecruitmentEncounterPhase.CombatResolved)
                return RecruitmentCommitResult.NotCombatResolved;
            if (completedAttempt is not RecruitmentDialogueAttempt owned
                || !owned.IsValidFor(
                    encounterId,
                    RecruitmentDialoguePurpose.RecruitmentCommit))
            {
                return RecruitmentCommitResult.InvalidAttempt;
            }
            if (!owned.IsCompleted)
                return RecruitmentCommitResult.DialogueProofMissing;

            RecruitmentCommitResult result = TryCommitRecruitmentCore(encounterId);
            if (result == RecruitmentCommitResult.Committed)
                owned.Consume();
            return result;
        }

        public RecruitmentCommitResult TryCommitRecruitmentAfterVictory(string encounterId)
        {
            if (!_recruitmentRuntimes.TryGetValue(encounterId, out var runtime)
                || runtime.Definition == null
                || runtime.Definition.CombatMode
                != RecruitmentEncounterCombatMode.HostileRecruitTarget)
            {
                return RecruitmentCommitResult.DialogueProofMissing;
            }

            return TryCommitRecruitmentCore(encounterId);
        }

        private RecruitmentCommitResult TryCommitRecruitmentCore(string encounterId)
        {
            RecruitmentEncounterPhase phase = _recruitmentStateStore.GetPhase(encounterId);
            if (phase == RecruitmentEncounterPhase.Completed)
                return RecruitmentCommitResult.AlreadyCompleted;
            if (phase == RecruitmentEncounterPhase.RecruitmentCommitted)
                return RecruitmentCommitResult.AlreadyCommitted;
            if (phase != RecruitmentEncounterPhase.CombatResolved)
                return RecruitmentCommitResult.NotCombatResolved;

            IPartyService party = Svc.Party;
            if (party == null)
                return RecruitmentCommitResult.PartyUnavailable;

            CharacterUnlockResult unlockResult = party.EnsureCharacterUnlocked(
                _recruitmentStateStore.GetRecruitCharacter(encounterId));
            if (unlockResult is not CharacterUnlockResult.AddedToBattle
                and not CharacterUnlockResult.PreparingBattle
                and not CharacterUnlockResult.AddedToRoster
                and not CharacterUnlockResult.AlreadyOwned)
            {
                return RecruitmentCommitResult.UnlockFailed;
            }

            if (!_recruitmentStateStore.TryCommitRecruitment(encounterId))
                return RecruitmentCommitResult.NotCombatResolved;

            if (_recruitmentRuntimes.TryGetValue(encounterId, out var runtime))
                runtime.TryApplyPhase(RecruitmentEncounterPhase.RecruitmentCommitted);
            NotifyRecruitmentPhaseChanged(encounterId, RecruitmentEncounterPhase.RecruitmentCommitted);
            return RecruitmentCommitResult.Committed;
        }

        public RecruitmentFinalizeResult TryFinalizeRecruitment(string encounterId)
        {
            RecruitmentEncounterPhase phase = _recruitmentStateStore.GetPhase(encounterId);
            if (phase == RecruitmentEncounterPhase.Completed)
                return RecruitmentFinalizeResult.AlreadyCompleted;
            if (phase != RecruitmentEncounterPhase.RecruitmentCommitted
                || !_recruitmentStateStore.TryComplete(encounterId))
            {
                return RecruitmentFinalizeResult.NotCommitted;
            }

            if (_recruitmentRuntimes.TryGetValue(encounterId, out var runtime))
                runtime.TryApplyPhase(RecruitmentEncounterPhase.Completed);
            NotifyRecruitmentPhaseChanged(encounterId, RecruitmentEncounterPhase.Completed);
            return RecruitmentFinalizeResult.Completed;
        }

        private bool RegisterDefinition(IRecruitmentEncounterRuntimePort runtime)
        {
            RecruitmentEncounterDefinitionSO definition = runtime.Definition;
            bool registered = definition != null
                              && string.Equals(
                                  runtime.EncounterId?.Trim(),
                                  definition.EncounterId?.Trim(),
                                  StringComparison.Ordinal)
                              && _recruitmentStateStore.TryRegisterDefinition(
                                  definition.EncounterId,
                                  definition.RecruitCharacter,
                                  definition.ResetScope);
            if (!registered)
            {
                UnityEngine.Debug.LogError(
                    $"[Story] 영입 조우 정의 등록 실패: {runtime?.EncounterId ?? "<null>"}");
            }
            return registered;
        }

        private void NotifyRecruitmentPhaseChanged(
            string encounterId,
            RecruitmentEncounterPhase phase)
        {
            if (_recruitmentObservers.TryGetValue(encounterId, out var observers))
            {
                var snapshot = new Action<RecruitmentEncounterPhase>[observers.Count];
                observers.CopyTo(snapshot);
                for (int i = 0; i < snapshot.Length; i++)
                    snapshot[i]?.Invoke(phase);
            }
        }

        private static bool ContainsOrdinal(IReadOnlyList<string> values, string expected)
        {
            for (int i = 0; i < values.Count; i++)
            {
                if (string.Equals(values[i], expected, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        private bool AreAllExpectedHostilesDefeated(
            string encounterId,
            IRecruitmentEncounterRuntimePort runtime)
        {
            IReadOnlyList<string> expected = runtime?.HostileParticipantIds;
            if (expected == null || expected.Count == 0)
                return false;

            IReadOnlyList<string> defeated =
                _recruitmentStateStore.GetDefeatedHostileIds(encounterId);
            for (int i = 0; i < expected.Count; i++)
            {
                if (!ContainsOrdinal(defeated, expected[i]))
                    return false;
            }
            return true;
        }

        private enum RecruitmentDialoguePurpose
        {
            RecruitmentCommit,
            CombatIntroduction,
        }

        private sealed class RecruitmentDialogueAttempt : IRecruitmentDialogueAttempt
        {
            private readonly int _attemptId;
            private readonly RecruitmentDialoguePurpose _purpose;
            private bool _disposed;
            private bool _consumed;

            public RecruitmentDialogueAttempt(
                string encounterId,
                int attemptId,
                RecruitmentDialoguePurpose purpose)
            {
                EncounterId = encounterId;
                _attemptId = attemptId;
                _purpose = purpose;
            }

            public string EncounterId { get; }
            public bool IsCompleted { get; private set; }

            public bool IsValidFor(
                string encounterId,
                RecruitmentDialoguePurpose purpose) =>
                !_disposed
                && !_consumed
                && _attemptId > 0
                && _purpose == purpose
                && string.Equals(EncounterId, encounterId, StringComparison.Ordinal);

            public void ConfirmCompleted()
            {
                if (!_disposed && !_consumed)
                    IsCompleted = true;
            }

            public void Consume() => _consumed = true;
            public void Dispose() => _disposed = true;
        }

        private sealed class ActionLease : IDisposable
        {
            private Action _release;

            public ActionLease(Action release) => _release = release;

            public void Dispose()
            {
                Action release = _release;
                _release = null;
                release?.Invoke();
            }
        }
    }
}
