using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Story;
using UPlayGround.Dialogue;
using UPlayGround.Manager;

namespace UPlayGround.FlowGraph
{
    internal static class RecruitmentEncounterFlowKeys
    {
        public static string DialogueAttempt(string encounterId) =>
            $"recruitment.dialogueAttempt.{encounterId}";
    }

    [FlowNodeMenu("영입 조우/Resume", Summary = "저장된 영입 조우 단계부터 전투 또는 대화를 재개합니다.")]
    [Serializable]
    public sealed class ResumeRecruitmentEncounterNode : FlowNode
    {
        public const string CombatPort = "Combat";
        public const string DialoguePort = "Dialogue";
        public const string PostDialoguePort = "PostDialogue";
        public const string CompletedPort = "Completed";
        public const string FailedPort = "Failed";

        public string encounterId;

        public override IEnumerable<FlowPortDef> Ports
        {
            get
            {
                yield return FlowPortDef.Input();
                yield return FlowPortDef.Output(CombatPort);
                yield return FlowPortDef.Output(DialoguePort);
                yield return FlowPortDef.Output(PostDialoguePort);
                yield return FlowPortDef.Output(CompletedPort);
                yield return FlowPortDef.Output(FailedPort);
            }
        }

        public override IEnumerator Execute(FlowToken token)
        {
            IRecruitmentEncounterService service = Svc.RecruitmentEncounters;
            if (service == null
                || !service.TryAcquireExecution(encounterId, out IDisposable executionLease))
            {
                Debug.LogWarning(
                    $"[RecruitmentEncounter] '{encounterId}' 실행 권한을 얻지 못했습니다. 서비스 등록과 중복 실행을 확인하세요.");
                token.Emit(FailedPort);
                yield break;
            }

            token.Context.RegisterTeardown(executionLease);
            RecruitmentEncounterStartResult result = service.TryStartOrResume(encounterId);
            switch (result)
            {
                case RecruitmentEncounterStartResult.CombatStarted:
                case RecruitmentEncounterStartResult.CombatResumed:
                    token.Emit(CombatPort);
                    break;
                case RecruitmentEncounterStartResult.DialoguePending:
                    token.Emit(DialoguePort);
                    break;
                case RecruitmentEncounterStartResult.PostDialoguePending:
                    token.Emit(PostDialoguePort);
                    break;
                case RecruitmentEncounterStartResult.AlreadyCompleted:
                    token.Emit(CompletedPort);
                    break;
                default:
                    Debug.LogWarning(
                        $"[RecruitmentEncounter] '{encounterId}' 시작·재개에 실패했습니다: {result}");
                    token.Emit(FailedPort);
                    break;
            }
            yield break;
        }
    }

    [FlowNodeMenu("영입 조우/Wait Combat Resolved", Summary = "적 참가자 전멸로 조우 전투가 끝날 때까지 대기합니다.")]
    [Serializable]
    public sealed class WaitRecruitmentCombatResolvedNode : FlowNode
    {
        public const string ResolvedPort = "Resolved";
        public const string FailedPort = "Failed";
        public string encounterId;

        public override IEnumerable<FlowPortDef> Ports
        {
            get
            {
                yield return FlowPortDef.Input();
                yield return FlowPortDef.Output(ResolvedPort);
                yield return FlowPortDef.Output(FailedPort);
            }
        }

        public override IEnumerator Execute(FlowToken token)
        {
            IRecruitmentEncounterService service = Svc.RecruitmentEncounters;
            if (service == null)
            {
                token.Emit(FailedPort);
                yield break;
            }

            bool resolved = false;
            IDisposable observation = service.ObservePhase(
                encounterId,
                phase => resolved = phase is RecruitmentEncounterPhase.CombatResolved
                    or RecruitmentEncounterPhase.Completed);
            if (observation == null)
            {
                token.Emit(FailedPort);
                yield break;
            }

            try
            {
                while (!resolved && !token.Context.Cancelled)
                    yield return null;
            }
            finally
            {
                observation.Dispose();
            }

            if (resolved && !token.Context.Cancelled)
                token.Emit(ResolvedPort);
        }
    }

    [FlowNodeMenu("영입 조우/Prepare Dialogue", Summary = "마지막 사망 연출을 보존하고 영입 대상의 실제 접근이 끝난 뒤 대화를 준비합니다.")]
    [Serializable]
    public sealed class PrepareRecruitmentDialogueNode : FlowNode
    {
        public const string ReadyPort = "Ready";
        public const string FailedPort = "Failed";
        public string encounterId;

        public override IEnumerable<FlowPortDef> Ports
        {
            get
            {
                yield return FlowPortDef.Input();
                yield return FlowPortDef.Output(ReadyPort);
                yield return FlowPortDef.Output(FailedPort);
            }
        }

        public override IEnumerator Execute(FlowToken token)
        {
            IRecruitmentEncounterService service = Svc.RecruitmentEncounters;
            if (service == null)
            {
                token.Emit(FailedPort);
                yield break;
            }

            // 마지막 적의 사망 모션·히트스톱·카메라 반응을 먼저 끝낸다.
            // 전투 정리와 위치 전환을 먼저 하면 이 대기 자체가 있어도 순간이동처럼 보인다.
            float settleSeconds = service.GetPostCombatSettleSeconds(encounterId);
            if (settleSeconds > 0f)
                yield return new WaitForSeconds(settleSeconds);
            if (token.Context.Cancelled)
                yield break;

            if (!service.TryPrepareDialogue(encounterId))
            {
                token.Emit(FailedPort);
                yield break;
            }

            while (!service.IsDialogueTransitionReady(encounterId)
                   && !token.Context.Cancelled)
            {
                yield return null;
            }

            if (!token.Context.Cancelled)
                token.Emit(ReadyPort);
        }
    }

    [FlowNodeMenu("영입 조우/Play Dialogue Required", Summary = "대화가 정상 완료됐을 때만 영입 증명을 발급합니다.")]
    [Serializable]
    public sealed class PlayDialogueRequiredNode : FlowNode
    {
        public const string CompletedPort = "Completed";
        public const string RejectedPort = "Rejected";

        public string encounterId;
        public DialogueGraphSO dialogue;

        public override IEnumerable<FlowPortDef> Ports
        {
            get
            {
                yield return FlowPortDef.Input();
                yield return FlowPortDef.Output(CompletedPort);
                yield return FlowPortDef.Output(RejectedPort);
            }
        }

        public override IEnumerator Execute(FlowToken token)
        {
            IRecruitmentEncounterService encounterService = Svc.RecruitmentEncounters;
            IDialogueService dialogueService = Svc.Dialogue;
            if (encounterService == null
                || dialogueService == null
                || dialogue == null
                || !encounterService.TryBeginDialogueAttempt(encounterId, out var attempt))
            {
                token.Emit(RejectedPort);
                yield break;
            }

            token.Context.RegisterTeardown(attempt);
            encounterService.TryGetDialoguePartnerActorId(encounterId, out string partnerActorId);
            bool done = false;
            bool cancelled = false;
            IDisposable request = dialogueService.TryStartDialogueTracked(
                dialogue,
                () => done = true,
                partnerActorId,
                () =>
                {
                    cancelled = true;
                    done = true;
                });
            if (request == null)
            {
                attempt.Dispose();
                token.Emit(RejectedPort);
                yield break;
            }

            try
            {
                while (!done && !token.Context.Cancelled)
                    yield return null;
            }
            finally
            {
                request.Dispose();
            }

            if (cancelled || token.Context.Cancelled)
            {
                token.Emit(RejectedPort);
                yield break;
            }

            encounterService.ConfirmDialogueCompleted(attempt);
            token.Context.Set(
                RecruitmentEncounterFlowKeys.DialogueAttempt(encounterId),
                attempt);
            token.Emit(CompletedPort);
        }
    }

    [FlowNodeMenu("영입 조우/Commit", Summary = "정상 대화 증명을 소비해 캐릭터를 멱등 영입하고 후속 대화 단계로 전환합니다.")]
    [Serializable]
    public sealed class CommitRecruitmentEncounterNode : FlowNode
    {
        public const string CompletedPort = "Completed";
        public const string FailedPort = "Failed";
        public string encounterId;

        public override IEnumerable<FlowPortDef> Ports
        {
            get
            {
                yield return FlowPortDef.Input();
                yield return FlowPortDef.Output(CompletedPort);
                yield return FlowPortDef.Output(FailedPort);
            }
        }

        public override IEnumerator Execute(FlowToken token)
        {
            IRecruitmentEncounterService service = Svc.RecruitmentEncounters;
            if (service == null
                || !token.Context.TryGet(
                    RecruitmentEncounterFlowKeys.DialogueAttempt(encounterId),
                    out IRecruitmentDialogueAttempt attempt))
            {
                token.Emit(FailedPort);
                yield break;
            }

            RecruitmentCommitResult result = service.TryCommitRecruitment(encounterId, attempt);
            token.Emit(result is RecruitmentCommitResult.Committed
                or RecruitmentCommitResult.AlreadyCommitted
                or RecruitmentCommitResult.AlreadyCompleted
                    ? CompletedPort
                    : FailedPort);
            yield break;
        }
    }

    [FlowNodeMenu("영입 조우/Play Post Dialogue", Summary = "파티 해금 뒤 영입 대상과 후속 대화를 재생합니다.")]
    [Serializable]
    public sealed class PlayRecruitmentPostDialogueNode : FlowNode
    {
        public const string CompletedPort = "Completed";
        public const string RejectedPort = "Rejected";

        public string encounterId;
        public DialogueGraphSO dialogue;

        public override IEnumerable<FlowPortDef> Ports
        {
            get
            {
                yield return FlowPortDef.Input();
                yield return FlowPortDef.Output(CompletedPort);
                yield return FlowPortDef.Output(RejectedPort);
            }
        }

        public override IEnumerator Execute(FlowToken token)
        {
            IRecruitmentEncounterService encounterService = Svc.RecruitmentEncounters;
            IDialogueService dialogueService = Svc.Dialogue;
            if (encounterService == null
                || dialogueService == null
                || dialogue == null
                || encounterService.GetPhase(encounterId)
                != RecruitmentEncounterPhase.RecruitmentCommitted)
            {
                token.Emit(RejectedPort);
                yield break;
            }

            encounterService.TryGetDialoguePartnerActorId(encounterId, out string partnerActorId);
            bool done = false;
            bool cancelled = false;
            IDisposable request = dialogueService.TryStartDialogueTracked(
                dialogue,
                () => done = true,
                partnerActorId,
                () =>
                {
                    cancelled = true;
                    done = true;
                });
            if (request == null)
            {
                token.Emit(RejectedPort);
                yield break;
            }

            try
            {
                while (!done && !token.Context.Cancelled)
                    yield return null;
            }
            finally
            {
                request.Dispose();
            }

            token.Emit(!cancelled && !token.Context.Cancelled
                ? CompletedPort
                : RejectedPort);
        }
    }

    [FlowNodeMenu("영입 조우/Finalize", Summary = "획득 후 대화가 끝난 조우를 완료 상태로 저장합니다.")]
    [Serializable]
    public sealed class FinalizeRecruitmentEncounterNode : FlowNode
    {
        public const string CompletedPort = "Completed";
        public const string FailedPort = "Failed";
        public string encounterId;

        public override IEnumerable<FlowPortDef> Ports
        {
            get
            {
                yield return FlowPortDef.Input();
                yield return FlowPortDef.Output(CompletedPort);
                yield return FlowPortDef.Output(FailedPort);
            }
        }

        public override IEnumerator Execute(FlowToken token)
        {
            RecruitmentFinalizeResult result = Svc.RecruitmentEncounters?
                .TryFinalizeRecruitment(encounterId)
                ?? RecruitmentFinalizeResult.NotCommitted;
            token.Emit(result is RecruitmentFinalizeResult.Completed
                or RecruitmentFinalizeResult.AlreadyCompleted
                    ? CompletedPort
                    : FailedPort);
            yield break;
        }
    }
}
