using System;
using System.Collections;
using UnityEngine;
using UPlayGround.FlowGraph;

namespace UPlayGround.Manager.World
{
    /// <summary>엔진 씬 활성화와 콘텐츠 공개를 분리하고 지역 저장을 먼저 복원한다.</summary>
    public sealed class StreamedRegionContent : MonoBehaviour
    {
        public string MapId;
        public GameObject ContentRoot;
        public bool IsReady { get; private set; }
        private FlowGraphRunner[] _runners;
        private bool[] _runnerEnabled;

        /// <summary>배치 복원 뒤에만 이벤트 러너를 무장한다.</summary>
        public IEnumerator Prepare()
        {
            if (ContentRoot == null || string.IsNullOrWhiteSpace(MapId))
                throw new InvalidOperationException("지역 루트 또는 mapId가 없습니다.");
            RegionOwnership.Register(gameObject.scene, MapId);
            _runners = ContentRoot.GetComponentsInChildren<FlowGraphRunner>(true);
            _runnerEnabled = new bool[_runners.Length];
            for (int i = 0; i < _runners.Length; i++)
            {
                _runnerEnabled[i] = _runners[i].enabled;
                _runners[i].enabled = false;
            }
            ContentRoot.SetActive(true);
            var environment = UnityEngine.Object.FindFirstObjectByType<Gameplay.World.LivingWorld.EnvironmentController>();
            foreach (var binding in ContentRoot.GetComponentsInChildren<Gameplay.Interaction.LivingWorld.NpcRoutineEnvironmentBinding>(true))
                binding.BindEnvironment(environment);
            WorldStateManager.Instance.ApplyPermanentKilledToRegion(gameObject.scene, MapId);
            MonsterRespawnManager.Instance.RegisterRegion(gameObject.scene);
            InteractionRespawnManager.Instance.RegisterRegion(gameObject.scene);
            InteractionRespawnManager.Instance.ApplyConsumedStatesToScene();
            foreach (var loader in ContentRoot.GetComponentsInChildren<RuntimePlacementLoader>(true))
            {
                if (!loader.IsSpawnComplete) yield return loader.SpawnAllWhenReady();
                if (loader.HasSpawnFailed) throw new InvalidOperationException(loader.SpawnFailure);
            }
            Gameplay.Quest.NpcQuestMarkerInstaller.InstallAll(gameObject.scene);
            IsReady = true;
            for (int i = 0; i < _runners.Length; i++) _runners[i].enabled = _runnerEnabled[i];
        }

        /// <summary>실행 중인 장면을 언로드하여 취소하지 않도록 수명을 유지한다.</summary>
        public bool HasActiveFlow()
        {
            if (_runners == null) return false;
            foreach (var runner in _runners)
                if (runner != null && runner.HasRegionBlockingWork()) return true;
            return false;
        }

        /// <summary>현재 저장 사실에 따라 현장 표현만 다시 구성한다.</summary>
        public void Resume()
        {
            if (_runners == null) return;
            foreach (var runner in _runners)
                if (runner != null && runner.isActiveAndEnabled) runner.ResumeRegion();
        }

        /// <summary>다음 방문에 복원할 수 있는 목적지 대기를 지역 이탈 때 정리한다.</summary>
        public void SuspendWaits()
        {
            if (_runners == null) return;
            foreach (var runner in _runners)
                if (runner != null) runner.SuspendRegionWaits();
        }

        private void OnDestroy()
        {
            RegionOwnership.Unregister(gameObject.scene);
        }
    }
}
