using UnityEngine;

namespace UPlayGround.Component
{
    /// <summary>
    /// 월드 배치 툴로 생성된 씬 오브젝트의 출처와 향후 Bake 정보를 기록한다.
    /// 실제 영속 상태(처치, 채집 완료 등)는 SceneEntityId와 WorldState 계층이 담당한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldPlacementMetadata : MonoBehaviour
    {
        public enum PlacementBakeMode
        {
            SceneObject = 0,
            RuntimeData = 1,
        }

        public enum PlacementSourceKind
        {
            ActorDefinition = 0,
            DirectPrefab = 1,
            GatheringData = 2,
            DropItemData = 3,
        }

        [SerializeField, Tooltip("배치 레코드 식별자. 씬 상태 저장용 ID가 아니라 배치 데이터 Bake 추적용이다.")]
        private string _placementGuid;

        [SerializeField]
        private PlacementBakeMode _bakeMode = PlacementBakeMode.SceneObject;

        [SerializeField]
        private PlacementSourceKind _sourceKind;

        [SerializeField, Tooltip("Actor ID, 프리팹 GUID, 데이터 GUID 등 배치 원본을 안정적으로 찾기 위한 ID.")]
        private string _sourceId;

        [SerializeField, Tooltip("월드 스트리밍/데이터 Bake 단계에서 사용할 셀 ID. 비워두면 씬 또는 루트 기준으로 추론한다.")]
        private string _cellId;

        [SerializeField]
        private int _randomSeed;

        [SerializeField]
        private bool _initiallyActive = true;

        public string PlacementGuid => _placementGuid;
        public PlacementBakeMode BakeMode => _bakeMode;
        public PlacementSourceKind SourceKind => _sourceKind;
        public string SourceId => _sourceId;
        public string CellId => _cellId;
        public int RandomSeed => _randomSeed;
        public bool InitiallyActive => _initiallyActive;

#if UNITY_EDITOR
        public void EditorSetPlacementInfo(
            PlacementSourceKind sourceKind,
            string sourceId,
            PlacementBakeMode bakeMode,
            string cellId,
            int randomSeed,
            bool initiallyActive)
        {
            EnsurePlacementGuid();
            _sourceKind = sourceKind;
            _sourceId = sourceId;
            _bakeMode = bakeMode;
            _cellId = cellId;
            _randomSeed = randomSeed;
            _initiallyActive = initiallyActive;
        }

        public void EditorSetBakeMode(PlacementBakeMode bakeMode)
        {
            EnsurePlacementGuid();
            _bakeMode = bakeMode;
        }

        /// <summary>
        /// Bake 데이터 복원 시 레코드의 원본 GUID를 이어받는다.
        /// GUID가 유지되어야 재Bake 시 기존 레코드를 중복 추가 대신 제자리 갱신할 수 있다.
        /// </summary>
        public void EditorOverridePlacementGuid(string guid)
        {
            if (!string.IsNullOrEmpty(guid))
                _placementGuid = guid;
        }

        private void OnValidate()
        {
            if (Application.isPlaying) return;
            EnsurePlacementGuid();
        }

        private void EnsurePlacementGuid()
        {
            if (string.IsNullOrEmpty(_placementGuid))
                _placementGuid = System.Guid.NewGuid().ToString("N");
        }
#endif
    }
}
