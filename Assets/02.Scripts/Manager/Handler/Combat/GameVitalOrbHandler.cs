using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Pool;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
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
        private ObjectPool<VitalOrbActor> _orbPool;

        private readonly Dictionary<VitalOrbTrigger, TriggerRuntimeState> _triggerStates = new();
        private readonly Dictionary<VitalOrbTrigger, int> _activeCountMap = new();
        private readonly Dictionary<VitalOrbActor, VitalOrbTrigger> _activeTriggerMap = new();
        private readonly List<VitalOrbActor> _activeOrbs = new();

        public override void Init()
        {
            LoadAssetsAsync().Forget();

            string[] lockLayer = { "Ground" };
            foreach (string layerName in lockLayer)
            {
                int layer = LayerMask.NameToLayer(layerName);
                if (layer != -1)
                    _groundLayerMask |= (1 << layer);
            }
        }

        public override void Dispose()
        {
            ReleaseAllActiveOrbs();
            _orbPool?.Clear();
            _orbPool = null;
            _vitalOrbObjectPrefab = null;
            _triggerConfig = null;
            _triggerStates.Clear();
            _activeCountMap.Clear();
            _activeTriggerMap.Clear();
        }

        public override void OnSceneChanged(string sceneType)
        {
            ReleaseAllActiveOrbs();

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

            bool hasPlayer = TryGetPlayerPosition(out Vector3 playerPosition);
            for (int i = _activeOrbs.Count - 1; i >= 0; --i)
            {
                var orb = _activeOrbs[i];
                if (orb == null)
                {
                    _activeOrbs.RemoveAt(i);
                    continue;
                }

                orb.Tick(dt, playerPosition, hasPlayer);
            }
        }

        /// <summary>
        /// 트리거별 드롭 시도. 기존 전투 이벤트 발생 지점에 삽입합니다.
        /// </summary>
        public void TrySpawn(VitalOrbTrigger trigger, Vector3 spawnPosition)
        {
            if (_triggerConfig == null || _vitalOrbObjectPrefab == null || _orbPool == null)
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

        private async UniTask LoadAssetsAsync()
        {
            UniTask configTask = LoadConfigDataAsync();
            UniTask prefabTask = LoadOrbPrefabAsync();
            await UniTask.WhenAll(configTask, prefabTask);
        }

        private async UniTask LoadOrbPrefabAsync()
        {
            try
            {
                GameObject prefab = await AssetManager.Instance.LoadGlobalAsync<GameObject>(
                    PrefabPath,
                    nameof(GameVitalOrbHandler));

                _vitalOrbObjectPrefab = prefab.GetComponent<VitalOrbActor>();
                if (_vitalOrbObjectPrefab == null)
                {
                    Debug.LogError($"[VitalOrbHandler] '{PrefabPath}' 프리팹에 VitalOrbActor가 없습니다.");
                    return;
                }

                CreatePool();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[VitalOrbHandler] 프리팹 로드 실패: {e.Message}");
            }
        }

        private async UniTask LoadConfigDataAsync()
        {
            try
            {
                _triggerConfig =
                    await AssetManager.Instance.LoadGlobalAsync<VitalOrbTriggerConfig>(
                        ConfigPath,
                        nameof(GameVitalOrbHandler));

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
            var instance = _orbPool.Get();
            instance.transform.SetPositionAndRotation(position, Quaternion.identity);
            instance.Initialize(entry.dropData, OnOrbFinished);

            _triggerStates[trigger].StartCooldown(entry.cooldown);
            _activeCountMap[trigger]++;
            _activeTriggerMap[instance] = trigger;
            _activeOrbs.Add(instance);

            GameObjectManager.Instance.ShowFX(entry.dropData.spawnParticleName, position);
        }

        private void OnOrbFinished(VitalOrbActor orb, VitalOrbActor.FinishReason reason)
        {
            if (orb == null)
                return;

            if (_activeTriggerMap.TryGetValue(orb, out VitalOrbTrigger trigger))
            {
                _activeCountMap[trigger] = Mathf.Max(0, _activeCountMap[trigger] - 1);
                _activeTriggerMap.Remove(orb);
            }

            _activeOrbs.Remove(orb);
            _orbPool.Release(orb);
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

        private void CreatePool()
        {
            _orbPool = new ObjectPool<VitalOrbActor>(
                createFunc: () => UnityEngine.Object.Instantiate(_vitalOrbObjectPrefab),
                actionOnGet: orb => orb.gameObject.SetActive(true),
                actionOnRelease: orb =>
                {
                    orb.ResetForPool();
                    orb.gameObject.SetActive(false);
                },
                actionOnDestroy: orb =>
                {
                    if (orb != null)
                        UnityEngine.Object.Destroy(orb.gameObject);
                },
                collectionCheck: true,
                defaultCapacity: 8,
                maxSize: 32);
        }

        private void ReleaseAllActiveOrbs()
        {
            for (int i = _activeOrbs.Count - 1; i >= 0; --i)
            {
                var orb = _activeOrbs[i];
                if (orb != null && _orbPool != null)
                    _orbPool.Release(orb);
            }

            _activeOrbs.Clear();
            _activeTriggerMap.Clear();
        }

        private static bool TryGetPlayerPosition(out Vector3 playerPosition)
        {
            var playerObj = GameObjectManager.Instance?.Player;
            if (playerObj == null)
            {
                playerPosition = Vector3.zero;
                return false;
            }

            var socket = playerObj.GetSocket(ActorSocketType.Center);
            playerPosition = socket != null ? socket.position : playerObj.transform.position;
            return true;
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
