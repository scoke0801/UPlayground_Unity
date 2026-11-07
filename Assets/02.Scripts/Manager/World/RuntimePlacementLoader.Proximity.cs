using System;
using System.Collections;
using System.Collections.Generic;
using UPlayGround.Data.World;
using UnityEngine;

namespace UPlayGround.Manager.World
{
    public sealed partial class RuntimePlacementLoader
    {
        private sealed class SpawnEntry
        {
            public WorldPlacementRecord Record;
            public MonsterActor Instance;
        }

        private readonly Dictionary<Vector2Int, List<int>> _spawnAreaCells = new();
        private readonly Dictionary<int, SpawnEntry> _spawnEntries = new();
        private readonly HashSet<int> _candidateAreaIds = new();
        private readonly List<int> _candidateAreas = new();
        private Coroutine _proximityRoutine;
        private MonsterRespawnManager _proximityRespawns;
        private WorldStateManager _proximityWorldState;
        private GameTimeManager _proximityTime;
        private IActorQueryService _proximityActors;
        private string _proximityMapId;
        private float _spawnCellSize;
        private Vector3 _spawnQueryPosition;
        private Comparison<int> _compareSpawnAreas;

        /// <summary>아직 생성하지 않은 배치도 포함한 인접 생성 대상 수.</summary>
        public int ProximityPlacementCount => _spawnEntries.Count;

        private void OnEnable()
        {
            if (_hasSpawnCompleted) StartProximitySpawning();
        }

        private void OnDestroy() => ClearProximitySpawning();

        private void PrepareProximitySpawning()
        {
            if (!_placementData.spawnMonstersByProximity || _spawnEntries.Count > 0) return;
            _proximityMapId = RegionOwnership.Resolve(gameObject.scene);
            if (string.IsNullOrEmpty(_proximityMapId))
                throw new InvalidOperationException("인접 몬스터 생성에는 SceneContext 또는 지역 mapId가 필요합니다.");

            _proximityRespawns = MonsterRespawnManager.Instance;
            _proximityWorldState = WorldStateManager.Instance;
            _proximityTime = GameTimeManager.Instance;
            _proximityActors = Svc.ActorQuery;
            if (_proximityRespawns == null || _proximityWorldState == null || _proximityTime == null)
                throw new InvalidOperationException("인접 몬스터 생성에 필요한 월드 서비스가 준비되지 않았습니다.");

            _spawnCellSize = Mathf.Max(10f, _placementData.monsterSpawnCellSize);
            _compareSpawnAreas = CompareSpawnAreas;
            var indexedRecords = new HashSet<int>();
            var guids = new HashSet<string>();
            var areas = _placementData.MonsterSpawnAreas;
            for (int areaId = 0; areaId < areas.Count; areaId++)
            {
                var area = areas[areaId];
                foreach (int recordId in area.recordIndices)
                {
                    if (recordId < 0 || recordId >= _placementData.Records.Count || !indexedRecords.Add(recordId))
                        throw new InvalidOperationException("몬스터 구역 인덱스가 유효하지 않습니다. 다시 Bake 하세요.");
                    var record = _placementData.Records[recordId];
                    string guid = record != null ? GetRecordGuid(record) : null;
                    if (record == null || !record.spawnWhenNearby || string.IsNullOrEmpty(record.actorId)
                        || string.IsNullOrEmpty(guid) || !guids.Add(guid))
                        throw new InvalidOperationException("인접 몬스터 배치의 ActorID 또는 고유 GUID가 유효하지 않습니다.");
                    var database = ActorSpawnManager.Instance.Database;
                    if (!database.TryGetDefinition(record.actorId, out var definition)
                        || definition.prefab == null || definition.prefab.GetComponent<MonsterActor>() == null)
                        throw new InvalidOperationException($"'{record.actorId}'는 생성 가능한 몬스터가 아닙니다.");

                    _proximityRespawns.RegisterProximityPlacement(_proximityMapId, guid, this);
                    _spawnEntries.Add(recordId, new SpawnEntry { Record = record });
                }
                AddSpawnAreaToCells(areaId, area.bounds);
            }
            for (int i = 0; i < _placementData.Records.Count; i++)
                if (_placementData.Records[i]?.spawnWhenNearby == true && !indexedRecords.Contains(i))
                    throw new InvalidOperationException("인접 몬스터 구역이 누락되었습니다. 다시 Bake 하세요.");
        }

        private void AddSpawnAreaToCells(int areaId, Bounds bounds)
        {
            Vector2Int min = GetSpawnCell(bounds.min);
            Vector2Int max = GetSpawnCell(bounds.max);
            for (int x = min.x; x <= max.x; x++)
            for (int z = min.y; z <= max.y; z++)
            {
                var cell = new Vector2Int(x, z);
                if (!_spawnAreaCells.TryGetValue(cell, out var areaIds))
                {
                    areaIds = new List<int>();
                    _spawnAreaCells.Add(cell, areaIds);
                }
                areaIds.Add(areaId);
            }
        }

        private Vector2Int GetSpawnCell(Vector3 position) => new(
            Mathf.FloorToInt(position.x / _spawnCellSize), Mathf.FloorToInt(position.z / _spawnCellSize));

        private void StartProximitySpawning()
        {
            if (isActiveAndEnabled && _spawnEntries.Count > 0 && _proximityRoutine == null)
                _proximityRoutine = StartCoroutine(SpawnNearbyMonsters());
        }

        private void StopProximitySpawning()
        {
            if (_proximityRoutine != null) StopCoroutine(_proximityRoutine);
            _proximityRoutine = null;
        }

        private void ClearProximitySpawning()
        {
            StopProximitySpawning();
            if (_proximityRespawns != null) _proximityRespawns.UnregisterProximityPlacements(this);
            _spawnEntries.Clear();
            _spawnAreaCells.Clear();
            _candidateAreaIds.Clear();
            _candidateAreas.Clear();
        }

        private IEnumerator SpawnNearbyMonsters()
        {
            // 준비 완료는 인덱스·정적 배치의 준비를 뜻한다. 플레이어를 기다리며 지역 로딩을 막지 않는다.
            yield return null;
            float nextEvaluation = 0f;
            while (!HasSpawnFailed)
            {
                if (Time.timeScale <= 0f || Time.unscaledTime < nextEvaluation || _proximityActors?.PlayerTransform == null)
                {
                    yield return null;
                    continue;
                }

                CollectNearbySpawnAreas(_proximityActors.PlayerTransform.position);
                int spawnedThisFrame = 0;
                double frameStartedAt = Time.realtimeSinceStartupAsDouble;
                foreach (int areaId in _candidateAreas)
                {
                    var area = _placementData.MonsterSpawnAreas[areaId];
                    foreach (int recordId in area.recordIndices)
                    {
                        Transform player = _proximityActors.PlayerTransform;
                        float radius = Mathf.Max(1f, _placementData.monsterSpawnDistance);
                        if (player == null || area.GetDistanceSquared(player.position) > radius * radius) break;
                        if (!CanSpawnEntry(_spawnEntries[recordId])) continue;
                        if (!TrySpawnProximityEntry(_spawnEntries[recordId])) yield break;
                        spawnedThisFrame++;
                        if (spawnedThisFrame >= Mathf.Max(1, _placementData.monsterSpawnsPerFrame)
                            || (Time.realtimeSinceStartupAsDouble - frameStartedAt) * 1000d
                            >= Mathf.Max(0.1f, _placementData.monsterSpawnBudgetMilliseconds))
                        {
                            yield return null;
                            while (Time.timeScale <= 0f) yield return null;
                            spawnedThisFrame = 0;
                            frameStartedAt = Time.realtimeSinceStartupAsDouble;
                        }
                    }
                }
                nextEvaluation = Time.unscaledTime + Mathf.Max(0.05f, _placementData.monsterSpawnInterval);
                yield return null;
            }
        }

        private void CollectNearbySpawnAreas(Vector3 position)
        {
            _spawnQueryPosition = position;
            _candidateAreaIds.Clear();
            _candidateAreas.Clear();
            float radius = Mathf.Max(1f, _placementData.monsterSpawnDistance);
            Vector2Int min = GetSpawnCell(position - new Vector3(radius, 0f, radius));
            Vector2Int max = GetSpawnCell(position + new Vector3(radius, 0f, radius));
            for (int x = min.x; x <= max.x; x++)
            for (int z = min.y; z <= max.y; z++)
            {
                if (!_spawnAreaCells.TryGetValue(new Vector2Int(x, z), out var areaIds)) continue;
                foreach (int areaId in areaIds)
                    if (_candidateAreaIds.Add(areaId)
                        && _placementData.MonsterSpawnAreas[areaId].GetDistanceSquared(position) <= radius * radius)
                        _candidateAreas.Add(areaId);
            }
            if (_compareSpawnAreas != null) _candidateAreas.Sort(_compareSpawnAreas);
        }

        private int CompareSpawnAreas(int first, int second) =>
            _placementData.MonsterSpawnAreas[first].GetDistanceSquared(_spawnQueryPosition).CompareTo(
                _placementData.MonsterSpawnAreas[second].GetDistanceSquared(_spawnQueryPosition));

        private bool CanSpawnEntry(SpawnEntry entry)
        {
            if (!entry.Record.initiallyActive || (entry.Instance != null && entry.Instance.IsAlive())) return false;
            string guid = GetRecordGuid(entry.Record);
            if (_proximityWorldState.IsPermanentlyKilled(_proximityMapId, guid)) return false;
            if (_proximityWorldState.TryGetRespawnState(_proximityMapId, guid, out var state))
                return !state.waitingRespawn || _proximityTime.TotalGameMinutes >= state.nextRespawnGameMinute;
            // 사망 처리와 저장 사실 기록 사이에 같은 배치를 다시 만들지 않는다.
            return entry.Instance == null;
        }

        private bool TrySpawnProximityEntry(SpawnEntry entry)
        {
            try
            {
                GameObject instance = SpawnViaActorManager(entry.Record);
                if (instance == null) throw new InvalidOperationException($"'{entry.Record.actorId}' 생성 실패");
                instance.transform.localScale = entry.Record.scale;
                instance.SetActive(entry.Record.initiallyActive);
                entry.Instance = instance.GetComponent<MonsterActor>();
                _instances.RemoveAll(IsDestroyedInstance);
                _instances.Add(instance);
                return true;
            }
            catch (Exception exception)
            {
                SpawnFailure = $"[RuntimePlacementLoader] 인접 몬스터 생성 실패: {exception.Message}";
                Debug.LogError(SpawnFailure, this);
                return false;
            }
        }

        private static bool IsDestroyedInstance(GameObject instance) => instance == null;
    }
}
