using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UPlayGround.Data.Combat;

namespace UPlayGround.Manager
{
    /// <summary>
    /// 회복 구슬 오브젝트 스폰 / 풀링을 담당하는 매니저.
    /// 기존 전투 코드에서 TrySpawn() 하나만 호출하면 됩니다.
    /// </summary>
    public class VitalOrbManager : BaseManager<VitalOrbManager>, IManager
    {
        [SerializeField] private VitalOrbActor _vitalOrbObjectPrefab;
        [SerializeField] private VitalOrbTriggerConfig   _triggerConfig;

        // 트리거별 런타임 상태
        private readonly Dictionary<VitalOrbTrigger, TriggerRuntimeState> _triggerStates = new();

        // 현재 월드에 살아있는 오브젝트 수 (트리거별 maxStack 체크용)
        private readonly Dictionary<VitalOrbTrigger, int> _activeCountMap = new();

        // -----------------------------------------------------------
        // IManager
        // -----------------------------------------------------------
        public void Init()
        {
            if (_triggerConfig == null)
            {
                Debug.LogError("[DropManager] DropTriggerConfig가 할당되지 않았습니다.");
                return;
            }

            foreach (var entry in _triggerConfig.entries)
            {
                _triggerStates[entry.trigger]  = new TriggerRuntimeState();
                _activeCountMap[entry.trigger] = 0;
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

            // NavMesh 위 유효 위치 탐색
            if (!TryGetNavMeshPosition(spawnPosition, out Vector3 validPos))
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
