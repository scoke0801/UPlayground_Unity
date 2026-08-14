using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Event;
using UPlayGround.Data.Quest;
using UPlayGround.Manager;

namespace UPlayGround.FlowGraph
{
    /// <summary>퀘스트를 수락한다.</summary>
    [FlowNodeMenu("퀘스트/StartQuest", Summary = "지정 퀘스트를 시작합니다.", Keywords = new[] { "quest", "start", "퀘스트" })]
    [Serializable]
    public sealed class StartQuestNode : FlowNode
    {
        public string questId;

        public override string DisplayName => $"StartQuest [{questId}]";

        public override IEnumerable<FlowPortDef> Ports
        {
            get
            {
                yield return FlowPortDef.Input();
                yield return FlowPortDef.Output();
            }
        }

        public override IEnumerator Execute(FlowToken token)
        {
            Svc.QuestFlow?.AcceptQuest(questId);
            token.Emit(FlowPort.Out);
            yield break;
        }
    }

    /// <summary>퀘스트를 완료 처리한다.</summary>
    [FlowNodeMenu("퀘스트/CompleteQuest", Summary = "지정 퀘스트를 완료 처리합니다.", Keywords = new[] { "quest", "complete", "finish" })]
    [Serializable]
    public sealed class CompleteQuestNode : FlowNode
    {
        public string questId;

        public override string DisplayName => $"CompleteQuest [{questId}]";

        public override IEnumerable<FlowPortDef> Ports
        {
            get
            {
                yield return FlowPortDef.Input();
                yield return FlowPortDef.Output();
            }
        }

        public override IEnumerator Execute(FlowToken token)
        {
            Svc.QuestFlow?.CompleteQuest(questId);
            token.Emit(FlowPort.Out);
            yield break;
        }
    }

    /// <summary>퀘스트를 HUD 추적 대상으로 지정한다.</summary>
    [FlowNodeMenu("퀘스트/TrackQuest", Summary = "지정 퀘스트를 HUD 추적 대상으로 설정합니다.", Keywords = new[] { "quest", "track", "hud", "추적" })]
    [Serializable]
    public sealed class TrackQuestNode : FlowNode
    {
        public string questId;

        public override string DisplayName => $"TrackQuest [{questId}]";

        public override IEnumerable<FlowPortDef> Ports
        {
            get
            {
                yield return FlowPortDef.Input();
                yield return FlowPortDef.Output();
            }
        }

        public override IEnumerator Execute(FlowToken token)
        {
            Svc.QuestFlow?.TrackQuest(questId);
            token.Emit(FlowPort.Out);
            yield break;
        }
    }

    /// <summary>문자열 서사 이벤트를 활성 퀘스트 목표에 알린다.</summary>
    [FlowNodeMenu("퀘스트/NotifyStoryEvent", Summary = "StoryEvent 목표에 문자열 이벤트를 알립니다.", Keywords = new[] { "quest", "story", "event", "서사" })]
    [Serializable]
    public sealed class NotifyQuestStoryEventNode : FlowNode
    {
        public string eventId;

        public override string DisplayName => $"NotifyStoryEvent [{eventId}]";

        public override IEnumerable<FlowPortDef> Ports
        {
            get
            {
                yield return FlowPortDef.Input();
                yield return FlowPortDef.Output();
            }
        }

        public override IEnumerator Execute(FlowToken token)
        {
            Svc.QuestFlow?.NotifyStoryEvent(eventId);
            token.Emit(FlowPort.Out);
            yield break;
        }
    }

    /// <summary>퀘스트 상태로 True/False 분기.</summary>
    [FlowNodeMenu("퀘스트/CheckQuestStatus (Branch)", Summary = "퀘스트 상태를 비교해 분기합니다.", Keywords = new[] { "quest", "status", "if", "조건" })]
    [Serializable]
    public sealed class CheckQuestStatusNode : FlowNode
    {
        public string questId;
        public QuestStatus expectedStatus = QuestStatus.Completed;

        public override string DisplayName => $"CheckQuest [{questId}]=={expectedStatus}";

        public override IEnumerable<FlowPortDef> Ports
        {
            get
            {
                yield return FlowPortDef.Input();
                yield return FlowPortDef.Output(FlowPort.True);
                yield return FlowPortDef.Output(FlowPort.False);
            }
        }

        public override IEnumerator Execute(FlowToken token)
        {
            IQuestFlowService quest = Svc.QuestFlow;
            bool result = quest != null && quest.GetQuestStatus(questId) == expectedStatus;
            token.Emit(result ? FlowPort.True : FlowPort.False);
            yield break;
        }
    }

    /// <summary>스토리 진행도를 설정한다. (현재 값보다 낮으면 매니저 정책에 따름)</summary>
    [FlowNodeMenu("스토리/SetStoryProgress", Summary = "스토리 진행도를 설정합니다.", Keywords = new[] { "story", "progress", "진행도" })]
    [Serializable]
    public sealed class SetStoryProgressNode : FlowNode
    {
        public int progress;

        public override string DisplayName => $"SetStoryProgress [{progress}]";

        public override IEnumerable<FlowPortDef> Ports
        {
            get
            {
                yield return FlowPortDef.Input();
                yield return FlowPortDef.Output();
            }
        }

        public override IEnumerator Execute(FlowToken token)
        {
            Svc.StoryFlow?.SetProgress(progress);
            token.Emit(FlowPort.Out);
            yield break;
        }
    }

    public enum CycleStoryAnchorSyncMode
    {
        NewGameReady,
        FirstRequestStarted,
        FirstAnchorResolved,
        FirstReturnStarted,
        FirstReturnRequestHeard,
        FirstReturnAnchorResolved,
        FirstReturnGuideCompleted,
        Resume,
    }

    /// <summary>
    /// P0 반복 앵커의 여러 플래그를 퀘스트 상태와 동기화한다.
    /// 모든 동작은 현재 상태를 다시 확인하므로 저장 복구와 중복 발화에 안전하다.
    /// </summary>
    [FlowNodeMenu("스토리/CycleAnchorSync", Summary = "반복 앵커 플래그와 퀘스트·진행도를 멱등 동기화합니다.", Keywords = new[] { "cycle", "anchor", "resume", "반복", "앵커" })]
    [Serializable]
    public sealed class CycleStoryAnchorSyncNode : FlowNode
    {
        private const string AnchorQuestId = "quest_cycle_anchor_lost_ribbon";
        private const string MainQuest001 = "quest_main_001";
        private const string MainQuest003 = "quest_main_003";
        private const string RequestEvent = "cycle.anchor.request_started";
        private const string ReturnedEvent = "cycle.story.first_return_anchor_returned";
        private const string ArrivedEvent = "cycle.story.first_return_arrived";
        private const string GuideTalkedEvent = "cycle.story.first_return_guide_completed";
        private const string FirstRequestFlag = "cycle.anchor.first_request_started";
        private const string ResolvedOnceFlag = "cycle.anchor.lostitem_resolved_once";
        private const string FirstReturnStartedFlag = "cycle.story.first_return_started";
        private const string FirstReturnRequestFlag = "cycle.anchor.first_return_request_heard";
        private const string FirstReturnAnchorFlag = "cycle.anchor.first_return_anchor_completed";
        private const string FirstReturnGuideFlag = "cycle.anchor.first_return_guide_completed";

        public CycleStoryAnchorSyncMode mode;

        public override string DisplayName => $"CycleAnchorSync [{mode}]";

        public override IEnumerable<FlowPortDef> Ports
        {
            get
            {
                yield return FlowPortDef.Input();
                yield return FlowPortDef.Output();
            }
        }

        public override IEnumerator Execute(FlowToken token)
        {
            Synchronize(mode);
            token.Emit(FlowPort.Out);
            yield break;
        }

        private static void Synchronize(CycleStoryAnchorSyncMode requestedMode)
        {
            IQuestFlowService quest = Svc.QuestFlow;
            IGlobalFlagService flags = Svc.Flags;
            if (quest == null || flags == null)
            {
                Debug.LogWarning("[CycleStoryAnchorSync] 퀘스트 또는 플래그 서비스가 준비되지 않았습니다.");
                return;
            }

            CycleStoryAnchorSyncMode effectiveMode = requestedMode;
            if (requestedMode == CycleStoryAnchorSyncMode.Resume)
            {
                if (flags.GetFlag(FirstReturnGuideFlag))
                    effectiveMode = CycleStoryAnchorSyncMode.FirstReturnGuideCompleted;
                else if (flags.GetFlag(FirstReturnAnchorFlag))
                    effectiveMode = CycleStoryAnchorSyncMode.FirstReturnAnchorResolved;
                else if (flags.GetFlag(FirstReturnStartedFlag))
                    effectiveMode = flags.GetFlag(FirstReturnRequestFlag)
                        ? CycleStoryAnchorSyncMode.FirstReturnRequestHeard
                        : CycleStoryAnchorSyncMode.FirstReturnStarted;
                else if (flags.GetFlag(ResolvedOnceFlag))
                    effectiveMode = CycleStoryAnchorSyncMode.FirstAnchorResolved;
                else if (flags.GetFlag(FirstRequestFlag))
                    effectiveMode = CycleStoryAnchorSyncMode.FirstRequestStarted;
                else
                    effectiveMode = CycleStoryAnchorSyncMode.NewGameReady;
            }

            switch (effectiveMode)
            {
                case CycleStoryAnchorSyncMode.NewGameReady:
                    EnsureActiveAndTrack(quest, AnchorQuestId);
                    break;

                case CycleStoryAnchorSyncMode.FirstRequestStarted:
                    EnsureActiveAndTrack(quest, AnchorQuestId);
                    quest.NotifyStoryEvent(RequestEvent);
                    break;

                case CycleStoryAnchorSyncMode.FirstAnchorResolved:
                    quest.NotifyStoryEvent(RequestEvent);
                    quest.CompleteQuest(AnchorQuestId);
                    EnsureActiveAndTrack(quest, MainQuest001);
                    Svc.EventPublisher?.Send(CycleStoryEvent.FirstAnchorGateCompleted);
                    break;

                case CycleStoryAnchorSyncMode.FirstReturnStarted:
                    quest.NotifyStoryEvent(ArrivedEvent);
                    EnsureRepeatableActiveAndTrack(quest, AnchorQuestId);
                    break;

                case CycleStoryAnchorSyncMode.FirstReturnRequestHeard:
                    quest.NotifyStoryEvent(ArrivedEvent);
                    EnsureRepeatableActiveAndTrack(quest, AnchorQuestId);
                    quest.NotifyStoryEvent(RequestEvent);
                    break;

                case CycleStoryAnchorSyncMode.FirstReturnAnchorResolved:
                    quest.NotifyStoryEvent(ArrivedEvent);
                    quest.NotifyStoryEvent(ReturnedEvent);
                    quest.TrackQuest(MainQuest003);
                    break;

                case CycleStoryAnchorSyncMode.FirstReturnGuideCompleted:
                    EnsureActive(quest, MainQuest003);
                    quest.NotifyStoryEvent(ArrivedEvent);
                    quest.NotifyStoryEvent(ReturnedEvent);
                    Svc.StoryFlow?.SetProgress(30);
                    quest.NotifyStoryEvent(GuideTalkedEvent);
                    quest.CompleteQuest(MainQuest003);
                    break;
            }
        }

        private static void EnsureActiveAndTrack(IQuestFlowService quest, string questId)
        {
            EnsureActive(quest, questId);
            if (quest.GetQuestStatus(questId) == QuestStatus.Active)
                quest.TrackQuest(questId);
        }

        private static void EnsureRepeatableActiveAndTrack(IQuestFlowService quest, string questId)
        {
            if (quest.GetQuestStatus(questId) != QuestStatus.Active)
                quest.AcceptQuest(questId);
            if (quest.GetQuestStatus(questId) == QuestStatus.Active)
                quest.TrackQuest(questId);
        }

        private static void EnsureActive(IQuestFlowService quest, string questId)
        {
            QuestStatus status = quest.GetQuestStatus(questId);
            if (status is not (QuestStatus.Active or QuestStatus.Completed))
                quest.AcceptQuest(questId);
        }
    }
}
