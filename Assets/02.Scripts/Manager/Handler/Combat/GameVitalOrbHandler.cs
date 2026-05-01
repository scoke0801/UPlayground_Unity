using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AI;
using UPlayGround.Data.Combat;
using UPlayGround.Manager.Handler;

namespace UPlayGround.Manager.Combat
{
    /// <summary>
    /// 회복 구슬 오브젝트 스폰 / 풀링을 담당하는 핸들러.
    /// 전투 코드에서 TrySpawn() 호출
    /// </summary>
    public class GameVitalOrbHandler : GameHandlerBase
    {
        private LayerMask _groundLayerMask = 0;
        private const float SpawnYOffset = 0.5f;
        private const float RaycastStartOffset = 10f;
        private const float RaycastDistance = 20f;

        private const string ConfigPath = "VitalOrbConfig";
        private const string PrefabPath = "VitalOrbPrefab";

        private VitalOrbActor _vitalOrbObjectPrefab;
        private VitalOrbTriggerConfig _triggerConfig;

        private readonly Dictionary<VitalOrbTrigger, TriggerRuntimeState> _triggerStates = new();
        private readonly Dictionary<VitalOrbTrigger, int> _activeCountMap = new();

        public override void Init()
        {
            LoadConfigData();
            LoadOrbPrefab();

            string[] lockLayer = { "Ground" };
            foreach (string layerName in lockLayer)
            {
                int layer = LayerMask.NameToLayer(layerName);
                if (layer != -1)
                    _groundLayerMask |= (1 << layer);
            }
        }

        public override void OnSceneChanged(string sceneType)
        {
            foreach (var state in _triggerStates.Values)
                state.Reset();

            foreach (var key in new List<VitalOrbTrigger>(_activeCountMap.Keys))
                _activeCountMap[key] = 0;
        }

        public override void Update()
        {
            float dt = Time.deltaTime;
            foreach (var state in _triggerStates.Values)
                state.TickCooldown(dt);
        }

        /// <summary>
        /// 트리거별 드롭 시도. 기존 전투 이벤트 발생 지점에 삽입합니다.
        /// </summary>
        public void TrySpawn(VitalOrbTrigger trigger, Vector3 spawnPosition)
        {
            if (_triggerConfig == null || _vitalOrbObjectPrefab == null)
                return;

            var entry = GetEntry(trigger);
            if (entry == null) return;

            if (_triggerStates[trigger].IsOnCooldown)
                return;

            if (_activeCountMap[trigger] >= entry.maxStack)
                return;

            if (Random.value > entry.probability)
                return;

            if (!TryGetGroundPosition(spawnPosition, out Vector3 validPos))
                return;

            SpawnDropObject(entry, trigger, validPos);
        }

        private void LoadOrbPrefab()
        {
            Addressables.LoadAssetAsync<GameObject>(PrefabPath).Completed += handle =>
            {
                _vitalOrbObjectPrefab = handle.Result.GetComponent<VitalOrbActor>();
            };
        }

        private async void LoadConfigData()
        {
            var handle = Addressables.LoadAssetAsync<VitalOrbTriggerConfig>(ConfigPath);

            try
            {
                _triggerConfig = await handle.Task;

                if (_triggerConfig == null)
                {
                    Debug.LogError($"[VitalOrbHandler] '{ConfigPath}' 경로에서 찾을 수 없습니다.");
                    return;
                }

                foreach (var entry in _triggerConfig.entries)
                {
                    _triggerStates[entry.trigger] = new TriggerRuntimeState();
                    _activeCountMap[entry.trigger] = 0;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[VitalOrbHandler] Config 로드 실패: {e.Message}");
            }
        }

        private void SpawnDropObject(VitalOrbTriggerEntry entry, VitalOrbTrigger trigger, Vector3 position)
        {
            var instance = UnityEngine.Object.Instantiate(_vitalOrbObjectPrefab, position, Quaternion.identity);
            instance.Initialize(entry.dropData, () => OnDropCollected(trigger));

            _triggerStates[trigger].StartCooldown(entry.cooldown);
            _activeCountMap[trigger]++;

            instance.OnExpired += () => OnDropExpired(trigger);

            GameObjectManager.Instance.ShowFX(entry.dropData.spawnParticleName, position);
        }

        private void OnDropCollected(VitalOrbTrigger trigger)
        {
            _activeCountMap[trigger] = Mathf.Max(0, _activeCountMap[trigger] - 1);
        }

        private void OnDropExpired(VitalOrbTrigger trigger)
        {
            _activeCountMap[trigger] = Mathf.Max(0, _activeCountMap[trigger] - 1);
        }

        private VitalOrbTriggerEntry GetEntry(VitalOrbTrigger trigger)
        {
            foreach (var entry in _triggerConfig.entries)
            {
                if (entry.trigger == trigger)
                    return entry;
            }
            return null;
        }

        private bool TryGetGroundPosition(Vector3 desired, out Vector3 result)
        {
            Vector3 origin = desired + Vector3.up * RaycastStartOffset;

            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, RaycastDistance, _groundLayerMask))
            {
                result = hit.point + Vector3.up * SpawnYOffset;
                return true;
            }

            result = Vector3.zero;
            return false;
        }

        private static bool TryGetNavMeshPosition(Vector3 desired, out Vector3 result)
        {
            if (NavMesh.SamplePosition(desired, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
            result = Vector3.zero;
            return false;
        }

        private class TriggerRuntimeState
        {
            private float _cooldownRemaining;
            public bool IsOnCooldown => _cooldownRemaining > 0f;

            public void StartCooldown(float duration) => _cooldownRemaining = duration;
            public void TickCooldown(float dt) => _cooldownRemaining = Mathf.Max(0f, _cooldownRemaining - dt);
            public void Reset() => _cooldownRemaining = 0f;
        }
    }
}
