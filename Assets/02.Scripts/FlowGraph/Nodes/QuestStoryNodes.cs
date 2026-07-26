using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
}
