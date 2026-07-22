using System;
using UPlayGround.Data.Quest;

namespace UPlayGround.Manager
{
    /// <summary>
    /// 전역 플래그 저장소 계약. FlowGraph 등 하위 모듈이 구체 매니저 없이 플래그를 읽고 쓴다.
    /// </summary>
    public interface IGlobalFlagService : IGameService
    {
        /// <summary>플래그 값이 실제로 변경될 때만 발화한다. (세이브 일괄 복원 시에는 발화하지 않음)</summary>
        event Action<string, bool> OnFlagChanged;

        bool GetFlag(string key);
        void SetFlag(string key, bool value);
    }

    /// <summary>
    /// 퀘스트 진행 제어의 흐름(오케스트레이션)용 최소 계약. 문자열 ID 기반.
    /// </summary>
    public interface IQuestFlowService : IGameService
    {
        bool AcceptQuest(string questId);
        bool CompleteQuest(string questId);
        bool FailQuest(string questId);
        QuestStatus GetQuestStatus(string questId);
    }

    /// <summary>
    /// 스토리 진행도 제어의 흐름용 최소 계약.
    /// </summary>
    public interface IStoryFlowService : IGameService
    {
        int CurrentProgress { get; }
        void SetProgress(int progress);
    }

    /// <summary>
    /// FlowGraph 실행 진입 계약. 코드/치트에서 등록된 그래프의 Manual 진입점을 발화한다.
    /// </summary>
    public interface IFlowGraphService : IGameService
    {
        /// <summary>graphId로 등록된 그래프의 진입점을 발화한다. entryId가 비어 있으면 모든 Manual 진입점.</summary>
        bool StartGraph(string graphId, string entryId = null);

        bool IsGraphRegistered(string graphId);
    }
}
