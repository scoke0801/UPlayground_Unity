using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Manager;

namespace UPlayGround.FlowGraph
{
    /// <summary>
    /// 씬의 FlowGraphRunner들을 graphId로 색인하고, IFlowGraphService로 외부(코드/치트)에
    /// Manual 진입점 발화를 제공하는 경량 매니저. 그래프 실행 자체는 각 러너가 소유한다.
    /// </summary>
    public sealed class FlowGraphManager : BaseManager<FlowGraphManager>, IManager, IFlowGraphService
    {
        private readonly Dictionary<string, List<FlowGraphRunner>> _runnersByGraphId = new();

        #region IManager

        public void Init()
        {
        }

        public void AfterInit()
        {
            foreach (List<FlowGraphRunner> runners in _runnersByGraphId.Values)
            {
                for (int i = 0; i < runners.Count; i++)
                    runners[i]?.RefreshEntrySubscriptions();
            }
        }

        public void Dispose()
        {
            _runnersByGraphId.Clear();
        }

        public void OnUpdate()
        {
        }

        public void OnFixedUpdate()
        {
        }

        public void OnLateUpdate()
        {
        }

        public void OnSceneChanged(string sceneType)
        {
            // 씬 러너는 파괴 시 스스로 해제하지만, 파괴 순서 경합으로 남은 항목을 정리한다.
            foreach (List<FlowGraphRunner> runners in _runnersByGraphId.Values)
                runners.RemoveAll(r => r == null);
        }

        #endregion

        internal void RegisterRunner(FlowGraphRunner runner)
        {
            if (runner == null || runner.Graph == null)
                return;

            string graphId = runner.Graph.ResolvedGraphId;
            if (!_runnersByGraphId.TryGetValue(graphId, out List<FlowGraphRunner> runners))
            {
                runners = new List<FlowGraphRunner>();
                _runnersByGraphId[graphId] = runners;
            }

            if (!runners.Contains(runner))
                runners.Add(runner);
        }

        internal void UnregisterRunner(FlowGraphRunner runner)
        {
            if (runner == null || runner.Graph == null)
                return;

            if (_runnersByGraphId.TryGetValue(runner.Graph.ResolvedGraphId, out List<FlowGraphRunner> runners))
                runners.Remove(runner);
        }

        #region IFlowGraphService

        public bool StartGraph(string graphId, string entryId = null)
        {
            if (string.IsNullOrEmpty(graphId)
                || !_runnersByGraphId.TryGetValue(graphId, out List<FlowGraphRunner> runners))
            {
                Debug.LogWarning($"[FlowGraphManager] 등록되지 않은 그래프 발화 요청: {graphId}");
                return false;
            }

            bool fired = false;
            for (int i = 0; i < runners.Count; i++)
            {
                if (runners[i] != null)
                    fired |= runners[i].FireManualEntries(entryId);
            }
            return fired;
        }

        public bool IsGraphRegistered(string graphId)
        {
            return !string.IsNullOrEmpty(graphId)
                && _runnersByGraphId.TryGetValue(graphId, out List<FlowGraphRunner> runners)
                && runners.Exists(r => r != null);
        }

        #endregion
    }
}
