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

        public IReadOnlyList<WorldPlacementRecord> Records => _records;

        public void SetRecords(IEnumerable<WorldPlacementRecord> records)
        {
            _records = records != null
                ? new List<WorldPlacementRecord>(records)
                : new List<WorldPlacementRecord>();
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
    }
}
