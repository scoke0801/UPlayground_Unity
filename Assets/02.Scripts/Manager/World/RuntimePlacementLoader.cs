using System.Collections;
using System.Collections.Generic;
using UPlayGround.Components;
using UPlayGround.Data.World;
using UPlayGround.Group;
using UnityEngine;

namespace UPlayGround.Manager.World
{
    /// <summary>
    /// WorldPlacementDataSO에 저장된 RuntimeData 배치 레코드를 런타임에 실제 오브젝트로 생성한다.
    /// actorId 레코드는 ActorSpawnManager로 스폰해 매니저 등록/그룹 소속까지 처리하고,
    /// 프리팹 직접 참조가 없어 씬 데이터-Addressables 번들 중복 포함을 막는다.
    /// actorId가 없는 레코드(직접 프리팹, 채집물 등)만 prefab 직접 생성으로 폴백한다.
    /// </summary>
    public sealed class RuntimePlacementLoader : MonoBehaviour
    {
        private const float DatabaseWaitTimeoutSeconds = 10f;

        [SerializeField]
        private WorldPlacementDataSO _placementData;

        [SerializeField]
        private bool _spawnOnStart = true;

        [SerializeField]
        private bool _clearBeforeSpawn = true;

        private readonly List<GameObject> _instances = new();
        private Dictionary<string, MonsterGroupController> _groupLookup;
        private bool _isSpawning;
        private bool _hasSpawnCompleted;

        public WorldPlacementDataSO PlacementData => _placementData;
        public IReadOnlyList<GameObject> Instances => _instances;
        public bool IsSpawnComplete => !_spawnOnStart || _hasSpawnCompleted;
        public bool IsSpawning => _isSpawning;

        private void Start()
        {
            if (_spawnOnStart)
                StartCoroutine(SpawnAllWhenReady());
            else
                _hasSpawnCompleted = true;
        }

        /// <summary>
        /// actorId 레코드가 있으면 ActorDatabase 로드(비동기 매니저 초기화)를 기다린 뒤 스폰한다.
        /// </summary>
        public IEnumerator SpawnAllWhenReady()
        {
            _isSpawning = true;
            _hasSpawnCompleted = false;

            if (HasActorIdRecords())
            {
                float timeoutAt = Time.realtimeSinceStartup + DatabaseWaitTimeoutSeconds;
                while (!IsSpawnManagerReady() && Time.realtimeSinceStartup < timeoutAt)
                    yield return null;

                if (!IsSpawnManagerReady())
                    Debug.LogWarning("[RuntimePlacementLoader] ActorDatabase 로드 대기 시간 초과 — actorId 레코드는 스폰되지 않습니다.", this);
            }

            SpawnAll();
            _isSpawning = false;
            _hasSpawnCompleted = true;
        }

        public void SpawnAll()
        {
            _hasSpawnCompleted = false;

            if (_clearBeforeSpawn)
                ClearSpawned();

            if (_placementData == null)
            {
                _hasSpawnCompleted = true;
                return;
            }

            _groupLookup = null;

            // 이미 존재하는 PlayerActor(선행 확보된 플레이어)는 레코드마다 재탐색하지 않고 루프 밖에서 1회만 캐싱한다.
            var existingPlayer = FindFirstObjectByType<PlayerActor>();

            foreach (var record in _placementData.Records)
            {
                if (record == null)
                    continue;

                if (existingPlayer != null
                    && !string.IsNullOrEmpty(record.actorId)
                    && PlayerMatchesActorId(existingPlayer, record.actorId))
                    continue;

                GameObject instance = !string.IsNullOrEmpty(record.actorId)
                    ? SpawnViaActorManager(record)
                    : SpawnViaPrefab(record);

                if (instance == null)
                    continue;

                instance.transform.localScale = record.scale;
                instance.SetActive(record.initiallyActive);
                SetupRuntimeState(record, instance);
                _instances.Add(instance);
            }

            _hasSpawnCompleted = true;
        }

        /// <summary>
        /// 씬에 PlayerActor가 없을 때 PartyManager가 호출한다.
        /// 전체 배치 스폰보다 먼저 Player만 확보하기 위한 경로이므로 _instances 소유로 넣지 않는다.
        /// </summary>
        public bool TrySpawnPlayerActor(string actorId, out PlayerActor player)
        {
            player = FindExistingPlayerActor(actorId);
            if (player != null)
                return true;

            if (_placementData == null || string.IsNullOrWhiteSpace(actorId))
                return false;

            foreach (var record in _placementData.Records)
            {
                if (record == null || record.actorId != actorId)
                    continue;

                GameObject instance = SpawnViaActorManager(record);
                if (instance == null)
                    return false;

                instance.transform.localScale = record.scale;

                string guid = GetRecordGuid(record);
                EnsureSceneEntityId(instance, guid);

                player = instance.GetComponent<PlayerActor>();
                if (player != null)
                {
                    // 플레이어는 파티 구성에 필수이므로 배치의 initiallyActive와 무관하게 활성 상태로 확보한다.
                    instance.SetActive(true);
                    return true;
                }

                instance.SetActive(record.initiallyActive);
                Debug.LogWarning($"[RuntimePlacementLoader] '{actorId}' 레코드가 PlayerActor를 생성하지 않았습니다.", this);
                return false;
            }

            return false;
        }

        private GameObject SpawnViaActorManager(WorldPlacementRecord record)
        {
            if (!IsSpawnManagerReady())
            {
                Debug.LogWarning($"[RuntimePlacementLoader] ActorSpawnManager가 준비되지 않아 '{record.actorId}' 스폰을 건너뜁니다.", this);
                return null;
            }

            MonsterGroupController group = ResolveGroup(record.groupName);
            Transform parent = group != null ? group.transform : transform;

            var actor = ActorSpawnManager.Instance.SpawnActor(record.actorId, record.position, record.rotation, group, parent);
            if (actor != null)
            {
                string guid = GetRecordGuid(record);
                EnsureSceneEntityId(actor.gameObject, guid);
            }

            if (actor is MonsterActor monster)
            {
                string guid = GetRecordGuid(record);
                MonsterRespawnManager.Instance?.RegisterRuntimePlacement(
                    monster,
                    guid,
                    record.actorId,
                    record.position,
                    record.rotation,
                    group);
            }

            return actor != null ? actor.gameObject : null;
        }

        private GameObject SpawnViaPrefab(WorldPlacementRecord record)
        {
            GameObject instance;
            if (record.prefab != null)
            {
                instance = Instantiate(record.prefab, record.position, record.rotation, transform);
            }
            else
            {
                instance = CreateDefaultInstance(record);
                if (instance == null)
                    return null;

                instance.transform.SetParent(transform);
                instance.transform.SetPositionAndRotation(record.position, record.rotation);
            }

            ApplyRecordData(record, instance);
            return instance;
        }

        private static GameObject CreateDefaultInstance(WorldPlacementRecord record)
        {
            return record.sourceKind switch
            {
                WorldPlacementSourceKind.GatheringData => new GameObject(BuildDefaultName("Gathering", record.interactableData)),
                WorldPlacementSourceKind.DropItemData => new GameObject(BuildDefaultName("DropItem", record.itemData)),
                _ => null,
            };
        }

        private static string BuildDefaultName(string prefix, UnityEngine.Object data)
        {
            return data != null ? $"{prefix}_{data.name}" : prefix;
        }

        private void ApplyRecordData(WorldPlacementRecord record, GameObject instance)
        {
            string guid = GetRecordGuid(record);
            EnsureSceneEntityId(instance, guid);

            if (record.sourceKind == WorldPlacementSourceKind.GatheringData)
            {
                var gatheringActor = instance.GetComponent<GatheringActor>();
                if (gatheringActor == null)
                    gatheringActor = instance.AddComponent<GatheringActor>();

                gatheringActor.Init(record.interactableData);
                InteractionRespawnManager.Instance?.RegisterRuntimePlacement(gatheringActor, guid);
                return;
            }

            if (record.sourceKind == WorldPlacementSourceKind.DropItemData)
            {
                var dropItemActor = instance.GetComponent<DropItemActor>();
                if (dropItemActor == null)
                    dropItemActor = instance.AddComponent<DropItemActor>();

                dropItemActor.Init(record.itemData, Mathf.Max(1, record.itemCount), record.interactableData);
                InteractionRespawnManager.Instance?.RegisterRuntimePlacement(dropItemActor, guid);
            }
        }

        private void SetupRuntimeState(WorldPlacementRecord record, GameObject instance)
        {
            if (!string.IsNullOrEmpty(record.actorId)
                || record.sourceKind is WorldPlacementSourceKind.GatheringData or WorldPlacementSourceKind.DropItemData)
                return;

            string guid = GetRecordGuid(record);
            EnsureSceneEntityId(instance, guid);

            var monster = instance.GetComponent<MonsterActor>();
            if (monster != null && !string.IsNullOrEmpty(monster.ActorId))
            {
                MonsterRespawnManager.Instance?.RegisterRuntimePlacement(
                    monster,
                    guid,
                    monster.ActorId,
                    record.position,
                    record.rotation,
                    ResolveGroup(record.groupName));
                return;
            }

            var gatheringActor = instance.GetComponent<GatheringActor>();
            if (gatheringActor != null)
            {
                InteractionRespawnManager.Instance?.RegisterRuntimePlacement(gatheringActor, guid);
                return;
            }

            var dropItemActor = instance.GetComponent<DropItemActor>();
            if (dropItemActor != null)
                InteractionRespawnManager.Instance?.RegisterRuntimePlacement(dropItemActor, guid);
        }

        private static void EnsureSceneEntityId(GameObject instance, string guid)
        {
            if (instance == null || string.IsNullOrEmpty(guid))
                return;

            var entityId = instance.GetComponent<SceneEntityId>();
            if (entityId == null)
                entityId = instance.AddComponent<SceneEntityId>();

            entityId.RuntimeSetGuid(guid);
        }

        private static string GetRecordGuid(WorldPlacementRecord record)
        {
            return !string.IsNullOrEmpty(record.sceneEntityGuid)
                ? record.sceneEntityGuid
                : record.placementGuid;
        }

        public void ClearSpawned()
        {
            for (int i = _instances.Count - 1; i >= 0; i--)
            {
                GameObject instance = _instances[i];
                if (instance != null)
                    Destroy(instance);
            }

            _instances.Clear();
        }

        private bool HasActorIdRecords()
        {
            if (_placementData == null)
                return false;

            foreach (var record in _placementData.Records)
            {
                if (record != null && !string.IsNullOrEmpty(record.actorId))
                    return true;
            }

            return false;
        }

        private static PlayerActor FindExistingPlayerActor(string actorId)
        {
            var player = FindFirstObjectByType<PlayerActor>();
            return PlayerMatchesActorId(player, actorId) ? player : null;
        }

        private static bool PlayerMatchesActorId(PlayerActor player, string actorId)
        {
            if (player == null)
                return false;

            if (string.IsNullOrEmpty(actorId))
                return true;

            return player.ActorId == actorId || (player.Definition != null && player.Definition.actorId == actorId);
        }

        private static bool IsSpawnManagerReady()
        {
            return ActorSpawnManager.Instance != null && ActorSpawnManager.Instance.IsDBLoaded;
        }

        private MonsterGroupController ResolveGroup(string groupName)
        {
            if (string.IsNullOrEmpty(groupName))
                return null;

            // 첫 조회 시 씬의 그룹을 한 번만 수집한다. Bake 시 그룹 오브젝트는 씬에 남는 것을 전제로 한다.
            if (_groupLookup == null)
            {
                _groupLookup = new Dictionary<string, MonsterGroupController>();
                foreach (var group in FindObjectsByType<MonsterGroupController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (!_groupLookup.ContainsKey(group.name))
                        _groupLookup[group.name] = group;
                }
            }

            if (_groupLookup.TryGetValue(groupName, out var found) && found != null)
                return found;

            Debug.LogWarning($"[RuntimePlacementLoader] 그룹 '{groupName}'을 씬에서 찾지 못했습니다. 그룹 없이 스폰합니다.", this);
            return null;
        }

#if UNITY_EDITOR
        public void EditorSetPlacementData(WorldPlacementDataSO placementData)
        {
            _placementData = placementData;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
