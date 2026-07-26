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

    /// <summary>
    /// FlowGraph 진행 기록 비교. 세이브에 남는 기록이므로 이어하기 후에도 같은 결과를 준다.
    /// graphId를 비우면 현재 실행 중인 그래프를 기준으로 한다.
    /// </summary>
    [Serializable]
    public sealed class FlowProgressCondition : FlowCondition
    {
        public enum ProgressState
        {
            /// <summary>한 번이라도 발화된 적이 있다.</summary>
            Started = 0,
            /// <summary>한 번이라도 끝까지 완주한 적이 있다.</summary>
            Completed = 1,
            /// <summary>발화됐지만 완주 기록이 없다(진행 중 또는 중단됨).</summary>
            InProgress = 2,
        }

        [Tooltip("대상 그래프 ID. 비우면 현재 실행 중인 그래프.")]
        public string graphId;

        [Tooltip("대상 진입점 노드의 ID (FlowGraph 에디터의 노드 ID).")]
        public string entryNodeId;

        public ProgressState expectedState = ProgressState.Completed;
        public bool expectedValue = true;

        public override bool Evaluate(FlowContext context)
        {
            string resolvedGraphId = string.IsNullOrEmpty(graphId)
                ? context?.Graph != null ? context.Graph.ResolvedGraphId : null
                : graphId;

            if (string.IsNullOrEmpty(resolvedGraphId) || string.IsNullOrEmpty(entryNodeId))
                return false;

            bool actual = expectedState switch
            {
                ProgressState.Started => FlowProgressState.IsEntryStarted(resolvedGraphId, entryNodeId),
                ProgressState.Completed => FlowProgressState.IsEntryCompleted(resolvedGraphId, entryNodeId),
                _ => FlowProgressState.IsEntryInProgress(resolvedGraphId, entryNodeId),
            };
            return actual == expectedValue;
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
