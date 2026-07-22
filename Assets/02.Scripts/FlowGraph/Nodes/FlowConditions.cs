using System;
using UnityEngine;
using UPlayGround.Data.Quest;
using UPlayGround.Manager;

namespace UPlayGround.FlowGraph
{
    /// <summary>
    /// Branch/Wait 노드가 소유하는 다형 조건. 노드와 마찬가지로 [SerializeReference]로 직렬화되므로
    /// 어셈블리 이동 시 [MovedFrom] 규약을 따른다.
    /// </summary>
    [Serializable]
    public abstract class FlowCondition
    {
        public abstract bool Evaluate(FlowContext context);
    }

    /// <summary>전역 플래그 값 비교.</summary>
    [Serializable]
    public sealed class FlagCondition : FlowCondition
    {
        public string flagKey;
        public bool expectedValue = true;

        public override bool Evaluate(FlowContext context)
        {
            IGlobalFlagService flags = Svc.Flags;
            return flags != null && flags.GetFlag(flagKey) == expectedValue;
        }
    }

    /// <summary>퀘스트 상태 비교.</summary>
    [Serializable]
    public sealed class QuestStatusCondition : FlowCondition
    {
        public string questId;
        public QuestStatus expectedStatus = QuestStatus.Completed;

        public override bool Evaluate(FlowContext context)
        {
            IQuestFlowService quest = Svc.QuestFlow;
            return quest != null && quest.GetQuestStatus(questId) == expectedStatus;
        }
    }

    /// <summary>스토리 진행도 하한 비교.</summary>
    [Serializable]
    public sealed class StoryProgressAtLeastCondition : FlowCondition
    {
        [Tooltip("현재 스토리 진행도가 이 값 이상이면 true.")]
        public int minProgress;

        public override bool Evaluate(FlowContext context)
        {
            IStoryFlowService story = Svc.StoryFlow;
            return story != null && story.CurrentProgress >= minProgress;
        }
    }
}
