using System;
using System.Collections.Generic;
using UPlayGround.Data.Story;

namespace UPlayGround.Manager
{
    public enum RecruitmentEncounterStartResult
    {
        CombatStarted,
        CombatResumed,
        DialoguePending,
        AlreadyCompleted,
        AlreadyRunning,
        UnknownEncounter,
        RuntimeUnavailable,
        ActivationFailed,
    }

    public enum RecruitmentCommitResult
    {
        Completed,
        AlreadyCompleted,
        DialogueProofMissing,
        InvalidAttempt,
        NotCombatResolved,
        PartyUnavailable,
        UnlockFailed,
    }

    public interface IRecruitmentEncounterRuntimePort
    {
        string EncounterId { get; }
        RecruitmentEncounterDefinitionSO Definition { get; }
        string DialoguePartnerActorId { get; }
        IReadOnlyList<string> HostileParticipantIds { get; }
        bool TryApplyPhase(RecruitmentEncounterPhase phase);
        bool TryActivateCombat();
        bool TryPrepareDialogue();
    }

    public interface IRecruitmentDialogueAttempt : IDisposable
    {
        string EncounterId { get; }
    }

    public interface IRecruitmentEncounterService : IGameService
    {
        IDisposable RegisterRuntime(IRecruitmentEncounterRuntimePort runtime);
        RecruitmentEncounterPhase GetPhase(string encounterId);
        IReadOnlyList<string> GetDefeatedHostileIds(string encounterId);
        bool TryAcquireExecution(string encounterId, out IDisposable lease);
        RecruitmentEncounterStartResult TryStartOrResume(string encounterId);
        IDisposable ObservePhase(
            string encounterId,
            Action<RecruitmentEncounterPhase> observer);
        void RecordHostileDefeated(string encounterId, string participantId);
        bool TryPrepareDialogue(string encounterId);
        float GetPostCombatSettleSeconds(string encounterId);
        bool TryGetDialoguePartnerActorId(string encounterId, out string actorId);
        bool TryBeginDialogueAttempt(
            string encounterId,
            out IRecruitmentDialogueAttempt attempt);
        void ConfirmDialogueCompleted(IRecruitmentDialogueAttempt attempt);
        RecruitmentCommitResult TryCommitRecruitment(
            string encounterId,
            IRecruitmentDialogueAttempt completedAttempt);
    }
}
