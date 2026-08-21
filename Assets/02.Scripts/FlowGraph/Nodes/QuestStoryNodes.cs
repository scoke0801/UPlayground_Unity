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
                yield return FlowPortDef.Output(FlowPort.True, optional: true);
                yield return FlowPortDef.Output(FlowPort.False, optional: true);
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
}
