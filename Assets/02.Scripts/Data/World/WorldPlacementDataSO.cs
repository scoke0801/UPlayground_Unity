using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Item;

namespace UPlayGround.Data.World
{
    public enum WorldPlacementSourceKind
    {
        ActorDefinition = 0,
        DirectPrefab = 1,
        GatheringData = 2,
        DropItemData = 3,
    }

    [CreateAssetMenu(fileName = "WorldPlacementData", menuName = "UPlayGround/월드/Placement Data")]
    public sealed class WorldPlacementDataSO : ScriptableObject
    {
        [SerializeField]
        private List<WorldPlacementRecord> _records = new();

        [Tooltip("몬스터를 인접 구역에서 생성한다. 기존 데이터는 꺼진 상태로 호환된다.")]
        public bool spawnMonstersByProximity;
        [Min(10f)] public float monsterSpawnCellSize = 64f;
        [Min(1f)] public float monsterSpawnDistance = 100f;
        [Min(0.05f)] public float monsterSpawnInterval = 0.25f;
        [Min(1)] public int monsterSpawnsPerFrame = 2;
        [Min(0.1f)] public float monsterSpawnBudgetMilliseconds = 2f;

        [SerializeField] private List<WorldPlacementSpawnArea> _monsterSpawnAreas = new();

        public IReadOnlyList<WorldPlacementRecord> Records => _records;
        public IReadOnlyList<WorldPlacementSpawnArea> MonsterSpawnAreas => _monsterSpawnAreas;

        public void SetRecords(IEnumerable<WorldPlacementRecord> records)
        {
            _records = records != null
                ? new List<WorldPlacementRecord>(records)
                : new List<WorldPlacementRecord>();
            RebuildMonsterSpawnAreas();
        }

        /// <summary>그룹을 보존하고 나머지 몬스터를 공간 셀로 묶어 스폰 구역을 굽는다.</summary>
        public void RebuildMonsterSpawnAreas()
        {
            _monsterSpawnAreas.Clear();
            var areasByKey = new Dictionary<string, WorldPlacementSpawnArea>();
            float cellSize = Mathf.Max(10f, monsterSpawnCellSize);
            for (int i = 0; i < _records.Count; i++)
            {
                var record = _records[i];
                if (record == null || !record.spawnWhenNearby) continue;
                string key = !string.IsNullOrEmpty(record.groupGuid)
                    ? "group:" + record.groupGuid
                    : $"cell:{Mathf.FloorToInt(record.position.x / cellSize)}:{Mathf.FloorToInt(record.position.z / cellSize)}";
                if (!areasByKey.TryGetValue(key, out var area))
                {
                    area = new WorldPlacementSpawnArea { bounds = new Bounds(record.position, Vector3.zero) };
                    areasByKey.Add(key, area);
                    _monsterSpawnAreas.Add(area);
                }
                area.bounds.Encapsulate(record.position);
                area.recordIndices.Add(i);
            }
        }
    }

    /// <summary>몬스터 편대 또는 공간 셀 하나의 생성 경계와 원본 레코드 인덱스.</summary>
    [Serializable]
    public sealed class WorldPlacementSpawnArea
    {
        public Bounds bounds;
        public List<int> recordIndices = new();

        /// <summary>비행·지형 높이와 무관하게 수평 접근 거리를 판정한다.</summary>
        public float GetDistanceSquared(Vector3 position)
        {
            position.y = bounds.center.y;
            return bounds.SqrDistance(position);
        }
    }

    [Serializable]
    public sealed class WorldPlacementRecord
    {
        public string placementGuid;
        public string prefabId;
        public string sceneEntityGuid;
        public WorldPlacementSourceKind sourceKind;

        [Tooltip("ActorDatabase 스폰용 ID. 값이 있으면 prefab 대신 ActorSpawnManager로 스폰한다. " +
                 "프리팹 직접 참조를 끊어 씬 데이터와 Addressables 번들의 중복 포함을 막는다.")]
        public string actorId;

        [Tooltip("actorId가 없는 배치(직접 프리팹, 채집물 등)만 사용. actorId 배치는 비워 둔다.")]
        public GameObject prefab;

        [Tooltip("채집/벌목/채광/낚시터 RuntimeData 복원용 상호작용 데이터.")]
        public InteractableActorSO interactableData;

        [Tooltip("DropItem RuntimeData 복원용 아이템 데이터.")]
        public ItemSO itemData;

        [Tooltip("DropItem RuntimeData 복원용 아이템 수량.")]
        public int itemCount = 1;

        public Vector3 position;
        public Quaternion rotation = Quaternion.identity;
        public Vector3 scale = Vector3.one;

        [Tooltip("스폰 시 소속시킬 씬의 MonsterGroupController 오브젝트 이름. groupGuid가 없을 때만 쓰는 폴백이다.")]
        public string groupName;

        [Tooltip("소속 그룹의 SceneEntityId GUID. 동명 그룹이 있어도 정확히 매칭하기 위한 1순위 키.")]
        public string groupGuid;

        [Tooltip("이 배치물이 유래한 MonsterGroupPresetSO의 PresetId. 추적용이며 복원에는 쓰이지 않는다.")]
        public string groupPresetId;

        public string cellId;
        public int randomSeed;
        public bool initiallyActive = true;
        [Tooltip("Bake에서 확인한 ActorID 기반 몬스터만 인접 생성 대상으로 저장한다.")]
        public bool spawnWhenNearby;
        [Tooltip("음수면 프리팹 기본값 사용. Bake한 개체의 추격 경계를 최초 생성과 재스폰에 유지한다.")]
        public float monsterChaseDistance = -1f;
    }
}
