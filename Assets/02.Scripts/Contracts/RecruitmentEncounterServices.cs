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
        PostDialoguePending,
        AlreadyCompleted,
        AlreadyRunning,
        UnknownEncounter,
        RuntimeUnavailable,
        ActivationFailed,
        PrerequisiteIncomplete,
    }

    public enum RecruitmentCommitResult
    {
        Committed,
        AlreadyCommitted,
        AlreadyCompleted,
        DialogueProofMissing,
        InvalidAttempt,
        NotCombatResolved,
        PartyUnavailable,
        UnlockFailed,
    }

    public enum RecruitmentFinalizeResult
    {
        Completed,
        AlreadyCompleted,
        NotCommitted,
    }

    public interface IRecruitmentEncounterRuntimePort
    {
        string EncounterId { get; }
        RecruitmentEncounterDefinitionSO Definition { get; }
        string DialoguePartnerActorId { get; }
        IReadOnlyList<string> HostileParticipantIds { get; }
        bool IsDialogueTransitionReady { get; }
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

        /// <summary>
        /// 실행 중인 조우 가운데 전투가 끝난 뒤의 대화·영입 연출 구간에 있는 것이 있는지 여부.
        /// 이 구간은 대화가 잠시 끊겼다가 후속 대화로 이어지므로,
        /// 그 틈에 다른 화면을 띄우면 안 되는 소비자가 조회한다.
        /// </summary>
        bool IsAnyEncounterInPresentation { get; }
        RecruitmentEncounterPhase GetPhase(string encounterId);
        bool IsEntryReady(string encounterId);
        IReadOnlyList<string> GetDefeatedHostileIds(string encounterId);
        bool TryAcquireExecution(string encounterId, out IDisposable lease);
        RecruitmentEncounterStartResult TryStartOrResume(string encounterId);
        IDisposable ObservePhase(
            string encounterId,
            Action<RecruitmentEncounterPhase> observer);
        void RecordHostileDefeated(string encounterId, string participantId);
        bool TryPrepareDialogue(string encounterId);
        bool IsDialogueTransitionReady(string encounterId);
        float GetPostCombatSettleSeconds(string encounterId);
        bool TryGetDialoguePartnerActorId(string encounterId, out string actorId);
        bool TryBeginDialogueAttempt(
            string encounterId,
            out IRecruitmentDialogueAttempt attempt);
        void ConfirmDialogueCompleted(IRecruitmentDialogueAttempt attempt);
        RecruitmentCommitResult TryCommitRecruitment(
            string encounterId,
            IRecruitmentDialogueAttempt completedAttempt);
        RecruitmentFinalizeResult TryFinalizeRecruitment(string encounterId);
    }
}
