using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Flow;
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

        /// <summary>지역 데이터(MapRegionInfoSO)에서 자동 생성한 러너. 지역이 바뀌면 통째로 교체된다.</summary>
        private readonly List<FlowGraphRunner> _mapRunners = new();

        /// <summary>지역 그래프 적용 직후 자동 발화하는 표준 진입점 ID.</summary>
        public const string MapReadyEntryId = "MapReady";

        /// <summary>현재 자동 적용된 지역(맵) 식별자. 미적용 상태는 null.</summary>
        public string AppliedMapId { get; private set; }

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
            ClearMapFlowGraphs();
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

        // ──────────────────────────────────────────────────────────
        #region 지역(맵) 자동 적용

        /// <summary>
        /// 지역 데이터에 등록된 FlowGraph를 이 매니저 하위의 러너로 생성해 무장한다.
        /// 씬마다 FlowGraphRunner를 손으로 배치하지 않아도 되며, 지역을 떠날 때(다음 호출)
        /// 이전 지역의 러너는 파괴된다. graphs가 비면 해제만 수행한다.
        ///
        /// 러너는 매니저(DontDestroyOnLoad) 하위에 두므로 씬 로드 타이밍에 파괴되지 않는다.
        /// 대신 같은 맵을 다시 진입해도 항상 새로 만든다 — Once(러너 수명) 정책이
        /// "지역 진입 1회"로 일관되게 동작하게 하기 위해서다.
        /// </summary>
        public void ApplyMapFlowGraphs(string mapId, IReadOnlyList<FlowGraphAssetBase> graphs)
        {
            ClearMapFlowGraphs();
            AppliedMapId = mapId;

            if (graphs == null)
                return;

            for (int i = 0; i < graphs.Count; i++)
            {
                FlowGraphAssetBase asset = graphs[i];
                if (asset == null)
                    continue;

                if (asset is not FlowGraphSO graph)
                {
                    Debug.LogWarning(
                        $"[FlowGraphManager] '{mapId}' 지역의 FlowGraph 항목이 FlowGraphSO가 아닙니다: {asset.name}",
                        this);
                    continue;
                }

                // 씬에 같은 그래프의 러너가 직접 배치돼 있으면 이중 발화가 되므로 데이터 쪽을 건너뛴다.
                if (IsGraphRegistered(graph.ResolvedGraphId))
                {
                    Debug.LogWarning(
                        $"[FlowGraphManager] '{graph.ResolvedGraphId}' 그래프의 러너가 이미 씬에 있어 " +
                        $"지역 자동 적용을 건너뜁니다. (지역={mapId})",
                        this);
                    continue;
                }

                var runnerObject = new GameObject($"MapFlowGraphRunner ({graph.ResolvedGraphId})");
                runnerObject.transform.SetParent(transform, false);
                // SetGraph는 비활성 상태에서만 허용된다. 비활성으로 만든 뒤 그래프를 넣고 켜서
                // OnEnable(무장 + 등록)이 그래프가 확정된 상태로 한 번만 돌게 한다.
                runnerObject.SetActive(false);
                var runner = runnerObject.AddComponent<FlowGraphRunner>();
                runner.SetGraph(graph);
                runnerObject.SetActive(true);

                _mapRunners.Add(runner);
            }

            FireMapReadyEntries();
        }

        /// <summary>
        /// 지역 그래프가 모두 무장된 뒤 <see cref="MapReadyEntryId"/> 진입점을 발화한다.
        /// 플래그 변화 진입점은 저장 복원에서 다시 울리지 않으므로, 지역 그래프가
        /// 현재 진행 상태를 보고 스스로 복구할 자리를 이 진입점으로 통일한다.
        /// 해당 진입점이 없는 그래프는 아무 일도 일어나지 않는다.
        /// </summary>
        private void FireMapReadyEntries()
        {
            for (int i = 0; i < _mapRunners.Count; i++)
            {
                FlowGraphRunner runner = _mapRunners[i];
                if (runner != null)
                    runner.FireManualEntries(MapReadyEntryId);
            }
        }

        /// <summary>지역 자동 생성 러너를 모두 해제한다(실행 중 흐름은 러너 비활성화로 취소된다).</summary>
        public void ClearMapFlowGraphs()
        {
            for (int i = 0; i < _mapRunners.Count; i++)
            {
                FlowGraphRunner runner = _mapRunners[i];
                if (runner == null)
                    continue;

                // Destroy는 프레임 끝에 처리돼 OnDisable(등록 해제)이 늦는다.
                // 같은 맵을 곧바로 다시 적용할 때 "이미 등록됨"으로 오판되므로 즉시 비활성화한다.
                runner.gameObject.SetActive(false);
                Destroy(runner.gameObject);
            }
            _mapRunners.Clear();
            AppliedMapId = null;
        }

        #endregion

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
