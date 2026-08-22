using System;
using System.Collections.Generic;
using UPlayGround.Data.Story;

namespace UPlayGround.Manager
{
    public enum RecruitmentEncounterStartResult
    {
        CombatStarted = 0,
        CombatResumed = 1,
        DialoguePending = 2,
        PostDialoguePending = 3,
        AlreadyCompleted = 4,
        AlreadyRunning = 5,
        UnknownEncounter = 6,
        RuntimeUnavailable = 7,
        ActivationFailed = 8,
        PrerequisiteIncomplete = 9,
        IntroductionPending = 10,
        DialogueProofMissing = 11,
        InvalidDialogueAttempt = 12,
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
        /// 실행 중인 조우 가운데 전투 전후의 대화·영입 연출 구간에 있는 것이 있는지 여부.
        /// 이 구간은 대화와 단계 전환이 이어지므로,
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
        bool TryBeginIntroductionDialogueAttempt(
            string encounterId,
            out IRecruitmentDialogueAttempt attempt);
        void ConfirmDialogueCompleted(IRecruitmentDialogueAttempt attempt);
        RecruitmentEncounterStartResult TryStartCombatAfterIntroduction(
            string encounterId,
            IRecruitmentDialogueAttempt completedAttempt);
        RecruitmentCommitResult TryCommitRecruitment(
            string encounterId,
            IRecruitmentDialogueAttempt completedAttempt);
        RecruitmentCommitResult TryCommitRecruitmentAfterVictory(string encounterId);
        RecruitmentFinalizeResult TryFinalizeRecruitment(string encounterId);
    }
}
