using System.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UPlayGround.Data.UI;
using UPlayGround.FlowGraph;
using UPlayGround.Manager;

namespace UPlayGround
{
    /// <summary>
    /// 씬마다 배치하는 씬 컨텍스트 베이스 클래스.
    /// Boot 씬을 거치지 않고 단독 실행해도 GameManager가 자동 초기화된다.
    /// </summary>
    public class SceneContext : MonoBehaviour
    {
        public string SceneType;

        [Tooltip("미니맵·전체맵 Config를 조회할 맵 식별자 (MapConfigDatabaseSO의 mapId와 일치해야 함)")]
        public string MapID;

        [Header("게임 흐름 (FlowGraph)")]
        [Tooltip("MapID로 지역 정보를 조회할 데이터베이스. 지역 정보의 FlowGraph 목록이 씬 진입 시 자동 적용된다.")]
        [SerializeField] private MapConfigDatabaseSO _mapConfigDB;

        [Tooltip("데이터베이스 조회를 건너뛰고 이 지역 정보를 직접 사용한다(테스트/특수 씬용). 비워두면 DB 조회.")]
        [SerializeField] private MapRegionInfoSO _regionInfoOverride;

        /// <summary>씬에 데이터베이스가 연결되지 않았을 때 사용할 Addressable 키.</summary>
        private const string MapConfigDatabaseKey = "MapConfigDatabase";

        private IEnumerator Start()
        {
            EnsureGameManagerInitialized();

            yield return new WaitUntil(() =>
                GameManager.Instance.BootState is GameBootState.Ready or GameBootState.Failed);

            if (GameManager.Instance.BootState == GameBootState.Failed)
            {
                Debug.LogError(
                    $"[SceneContext] GameManager 초기화 실패로 씬 준비를 중단합니다: " +
                    $"{GameManager.Instance.InitializationFailure}");
                yield break;
            }

            SceneManager.Instance.NotifySceneContextReady(this);

            // 흐름이 퀘스트를 열기 전에 마커 지점을 세워, 목표가 생기는 즉시 갈 곳이 보이게 한다.
            Gameplay.Quest.NpcQuestMarkerInstaller.InstallAll(gameObject.scene);

            yield return ResolveMapConfigDatabaseAsync().ToCoroutine();

            // 씬 전환 통보(매니저 레퍼런스 재수집) 이후에 지역 흐름을 무장한다.
            ApplyRegionFlowGraphs();
        }

        /// <summary>
        /// 지역 정보(MapRegionInfoSO)에 등록된 FlowGraph를 자동 적용한다.
        /// 지역 정보나 DB가 없으면 빈 목록으로 호출해 이전 지역의 러너를 해제만 한다
        /// (타이틀 등 흐름이 없는 씬으로 이동할 때 이전 지역 흐름이 남지 않게 한다).
        /// </summary>
        private void ApplyRegionFlowGraphs()
        {
            var flowGraphManager = FlowGraphManager.Instance;
            if (flowGraphManager == null)
                return;

            MapRegionInfoSO regionInfo = _regionInfoOverride != null
                ? _regionInfoOverride
                : _mapConfigDB != null
                    ? _mapConfigDB.GetRegionInfo(MapID)
                    : null;

            if (regionInfo == null && !string.IsNullOrEmpty(MapID))
            {
                // 지역 정보를 놓치면 그 지역의 흐름이 통째로 사라진다. 조용히 넘어가면 퀘스트가
                // 열리지 않는 진행 불능을 데이터 문제로 오인하게 되므로 반드시 드러낸다.
                Debug.LogError(
                    $"[SceneContext] '{MapID}' 지역 정보를 찾지 못해 지역 FlowGraph를 하나도 적용하지 않습니다. " +
                    $"MapConfigDatabase에 해당 mapId가 있는지 확인하세요.",
                    this);
            }

            flowGraphManager.ApplyMapFlowGraphs(MapID, regionInfo != null ? regionInfo.flowGraphs : null);
            CycleRunManager.Instance?.NotifyStoryFlowReady();
        }

        /// <summary>
        /// 씬에 <see cref="_mapConfigDB"/>가 연결되지 않았을 때 전역 데이터베이스로 보충한다.
        /// 지역 흐름이 씬 인스펙터 배선 하나에 걸려 조용히 비활성화되는 것을 막는다.
        /// </summary>
        private async UniTask ResolveMapConfigDatabaseAsync()
        {
            if (_mapConfigDB != null || _regionInfoOverride != null || string.IsNullOrEmpty(MapID))
                return;

            AssetManager assets = AssetManager.Instance;
            if (assets == null)
                return;

            _mapConfigDB = await assets.TryLoadGlobalAsync<MapConfigDatabaseSO>(
                MapConfigDatabaseKey, nameof(SceneContext), destroyCancellationToken);
        }

        /// <summary>
        /// GameManager가 없으면 생성하고 초기화한다.
        /// DontDestroyOnLoad 포함 전체 씬에서 탐색.
        /// </summary>
        private void EnsureGameManagerInitialized()
        {
            var gm = GameManager.Instance; 
            // 없으면 BaseManager가 자동 생성
            // Instance 접근만으로 생성 + Awake(→ InitializeManagers) 까지 실행됨
        }
    }
}
