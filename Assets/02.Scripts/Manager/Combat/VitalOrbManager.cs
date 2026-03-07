using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AI;
using UPlayGround.Data.Combat;

namespace UPlayGround.Manager
{
    /// <summary>
    /// 회복 구슬 오브젝트 스폰 / 풀링을 담당하는 매니저.
    /// 전투 코드에서 TrySpawn() 호출
    /// </summary>
    public class VitalOrbManager : BaseManager<VitalOrbManager>, IManager
    {
        [Tooltip("지형으로 판정할 레이어")] 
        private LayerMask _groundLayerMask = 0;
        
        [Tooltip("바닥에서 얼만큼 띄워서 스폰할지")] 
        private float _spawnYOffset = 0.5f;

        [Tooltip("원하는 위치 기준 얼만큼 위에서 레이캐스트를 쏠지")] 
        private float _raycastStartOffset = 10f;

        [Tooltip("레이캐스트 최대 탐색 거리")] 
        private float _raycastDistance = 20f;
        
        private readonly string configPath = "VitalOrbConfig";
        private readonly string prefabPath = "VitalOrbPrefab";
        
        private VitalOrbActor _vitalOrbObjectPrefab;
        private VitalOrbTriggerConfig   _triggerConfig;

        // 트리거별 런타임 상태
        private readonly Dictionary<VitalOrbTrigger, TriggerRuntimeState> _triggerStates = new();

        // 현재 월드에 살아있는 오브젝트 수 (트리거별 maxStack 체크용)
        private readonly Dictionary<VitalOrbTrigger, int> _activeCountMap = new();

        // -----------------------------------------------------------
        // IManager
        // -----------------------------------------------------------
        public void Init()
        {
            LoadConfigData();
            LoadOrbPrefab();

            string[] lockLayer =
            {
                "Ground"
            };
            foreach (string layerName in lockLayer)
            {
                int layer = LayerMask.NameToLayer(layerName);
                if (layer != -1)
                {
                    _groundLayerMask |= (1 << layer);  // 비트 OR로 레이어 추가
                }
            }
        }

        private void LoadOrbPrefab()
        {
            Addressables.LoadAssetAsync<GameObject>(prefabPath).Completed += handle =>
            {
                _vitalOrbObjectPrefab = handle.Result.GetComponent<VitalOrbActor>(); 
            };
        }

        private async void LoadConfigData()
        {
            var handle = Addressables.LoadAssetAsync<VitalOrbTriggerConfig>(configPath);

            try
            {
                _triggerConfig = await handle.Task;

                if (_triggerConfig == null)
                {
                    Debug.LogError($"[VitalOrbManager] '{configPath}' 경로에서 찾을 수 없습니다.");
                    return;
                }

                foreach (var entry in _triggerConfig.entries)
                {
                    _triggerStates[entry.trigger]  = new TriggerRuntimeState();
                    _activeCountMap[entry.trigger] = 0;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[VitalOrbManager] Config 로드 실패: {e.Message}");
            }
        }

        public void Dispose()  { }
        public void AfterInit() { }
        public void OnSceneChanged(string sceneType)
        {
            // 씬 전환 시 쿨다운 리셋
            foreach (var state in _triggerStates.Values)
                state.Reset();

            foreach (var key in new List<VitalOrbTrigger>(_activeCountMap.Keys))
                _activeCountMap[key] = 0;
        }
        public void OnUpdate()
        {
            float dt = Time.deltaTime;
            foreach (var state in _triggerStates.Values)
                state.TickCooldown(dt);
        }
        public void OnFixedUpdate() { }
        public void OnLateUpdate()  { }

        // -----------------------------------------------------------
        // Public API
        // -----------------------------------------------------------

        /// <summary>
        /// 트리거별 드롭 시도. 기존 전투 이벤트 발생 지점에 삽입합니다.
        /// </summary>
        /// <param name="trigger">트리거 종류</param>
        /// <param name="spawnPosition">스폰 희망 위치</param>
        public void TrySpawn(VitalOrbTrigger trigger, Vector3 spawnPosition)
        {
            if (_triggerConfig == null || _vitalOrbObjectPrefab == null)
                return;

            var entry = GetEntry(trigger);
            if (entry == null) return;

            // 쿨다운 체크
            if (_triggerStates[trigger].IsOnCooldown)
                return;

            // maxStack 체크
            if (_activeCountMap[trigger] >= entry.maxStack)
                return;

            // 확률 체크
            if (Random.value > entry.probability)
                return;
            
            if (!TryGetGroundPosition(spawnPosition, out Vector3 validPos))
                return; 
            
            SpawnDropObject(entry, trigger, validPos);
        }

        // -----------------------------------------------------------
        // Internal
        // -----------------------------------------------------------

        private void SpawnDropObject(VitalOrbTriggerEntry entry, VitalOrbTrigger trigger, Vector3 position)
        {
            var instance = Instantiate(_vitalOrbObjectPrefab, position, Quaternion.identity);
            instance.Initialize(entry.dropData, () => OnDropCollected(trigger));

            _triggerStates[trigger].StartCooldown(entry.cooldown);
            _activeCountMap[trigger]++;

            // 수명 만료 콜백 (Expire 상태로 소멸 시에도 카운트 감소)
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
            // 원하는 위치(desired)가 바닥보다 살짝 아래에 있을 수도 있으므로,
            // 허공(_raycastStartOffset 만큼 위)에서 아래로 레이캐스트를 쏩니다.
            Vector3 origin = desired + Vector3.up * _raycastStartOffset;
    
            // 아래쪽으로 레이를 쏴서 지형(_groundLayerMask)과 충돌하는지 확인
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, _raycastDistance, _groundLayerMask))
            {
                // 충돌한 바닥 좌표에 원하는 yOffset만큼 위로 올려서 반환
                result = hit.point + Vector3.up * _spawnYOffset;
                return true;
            }

            // 바닥을 찾지 못한 경우
            result = Vector3.zero;
            return false;
        }
        
        private static bool TryGetNavMeshPosition(Vector3 desired, out Vector3 result)
        {
            // 반경 2m 안에서 가장 가까운 NavMesh 샘플
            if (NavMesh.SamplePosition(desired, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
            result = Vector3.zero;
            return false;
        }

        // -----------------------------------------------------------
        // Runtime State Helper
        // -----------------------------------------------------------
        private class TriggerRuntimeState
        {
            private float _cooldownRemaining;
            public bool IsOnCooldown => _cooldownRemaining > 0f;

            public void StartCooldown(float duration) => _cooldownRemaining = duration;
            public void TickCooldown(float dt)        => _cooldownRemaining = Mathf.Max(0f, _cooldownRemaining - dt);
            public void Reset()                       => _cooldownRemaining = 0f;
        }
    }
}
