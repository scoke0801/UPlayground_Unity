using UnityEngine;

namespace UPlayGround.Data.World
{
    /// <summary>표면 스냅 방식. 밑면 피벗 프리팹은 LowerOnly, 중앙 피벗 프리팹(바위 등)은 Full.</summary>
    public enum PlacementSurfaceSnap
    {
        None = 0,
        LowerOnly = 1,
        Full = 2,
    }

    /// <summary>배치물이 씬 오브젝트로 남을지, RuntimeData로 Bake될지.</summary>
    public enum PlacementBakeTarget
    {
        SceneObject = 0,
        RuntimeData = 1,
    }

    /// <summary>
    /// 배치 규칙 묶음. 캐릭터/채집물/바위/트리거처럼 배치물 성격마다 다른 규칙을
    /// 매번 토글로 맞추지 않고 에셋으로 저장해 전환한다.
    /// </summary>
    [CreateAssetMenu(fileName = "PlacementRuleProfile", menuName = "UPlayGround/World/Placement Rule Profile")]
    public sealed class PlacementRuleProfileSO : ScriptableObject
    {
        [SerializeField] private string _displayName;

        [TextArea(2, 3)]
        [SerializeField] private string _usage;

        [Header("표면")]
        [SerializeField] private PlacementSurfaceSnap _surfaceSnapMode = PlacementSurfaceSnap.LowerOnly;
        [SerializeField] private bool _alignToSurface;
        [SerializeField] private float _heightOffset;

        [Header("레이캐스트")]
        [SerializeField] private LayerMask _raycastMask = ~0;
        [Tooltip("낚시터 수면처럼 트리거 위에 배치해야 하는 경우가 있어 기본값은 허용이다.")]
        [SerializeField] private bool _ignoreTriggerColliders;

        [Header("정렬")]
        [SerializeField] private bool _snapToGrid;
        [Min(0.01f)] [SerializeField] private float _gridSize = 1f;
        [Range(-180f, 180f)] [SerializeField] private float _yawOffset;
        [SerializeField] private bool _randomRotation;
        [SerializeField] private Vector2 _randomRotationXRange = Vector2.zero;
        [SerializeField] private Vector2 _randomRotationYRange = new(0f, 360f);
        [SerializeField] private Vector2 _randomRotationZRange = Vector2.zero;

        [Header("부착")]
        [SerializeField] private bool _autoSetupCollider = true;
        [SerializeField] private bool _addSceneEntityId = true;
        [SerializeField] private bool _addPlacementMetadata = true;
        [SerializeField] private PlacementBakeTarget _bakeTarget = PlacementBakeTarget.SceneObject;

        [Header("검증")]
        [Tooltip("이 각도를 넘는 경사에 배치하면 경고한다.")]
        [Range(0f, 90f)] [SerializeField] private float _maxSlopeAngle = 35f;

        [Tooltip("이 반경 안에 다른 배치물이 있으면 경고한다. 0이면 검사하지 않는다.")]
        [Min(0f)] [SerializeField] private float _overlapWarnRadius = 0.5f;

        [Tooltip("NavMesh 위가 아니면 경고한다. 이동하는 액터 프로필에서만 켠다.")]
        [SerializeField] private bool _requireNavMesh;

        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
        public string Usage => _usage;
        public PlacementSurfaceSnap SurfaceSnapMode => _surfaceSnapMode;
        public bool AlignToSurface => _alignToSurface;
        public float HeightOffset => _heightOffset;
        public LayerMask RaycastMask => _raycastMask;
        public bool IgnoreTriggerColliders => _ignoreTriggerColliders;
        public bool SnapToGrid => _snapToGrid;
        public float GridSize => _gridSize;
        public float YawOffset => _yawOffset;
        public bool RandomRotation => _randomRotation;
        public Vector2 RandomRotationXRange => _randomRotationXRange;
        public Vector2 RandomRotationYRange => _randomRotationYRange;
        public Vector2 RandomRotationZRange => _randomRotationZRange;
        public bool AutoSetupCollider => _autoSetupCollider;
        public bool AddSceneEntityId => _addSceneEntityId;
        public bool AddPlacementMetadata => _addPlacementMetadata;
        public PlacementBakeTarget BakeTarget => _bakeTarget;
        public float MaxSlopeAngle => _maxSlopeAngle;
        public float OverlapWarnRadius => _overlapWarnRadius;
        public bool RequireNavMesh => _requireNavMesh;

#if UNITY_EDITOR
        /// <summary>에디터에서 현재 창 설정을 프로필로 굳힐 때 사용한다.</summary>
        public void EditorCapture(
            PlacementSurfaceSnap surfaceSnapMode,
            bool alignToSurface,
            float heightOffset,
            LayerMask raycastMask,
            bool ignoreTriggerColliders,
            bool snapToGrid,
            float gridSize,
            float yawOffset,
            bool randomRotation,
            Vector2 randomRotationXRange,
            Vector2 randomRotationYRange,
            Vector2 randomRotationZRange,
            bool autoSetupCollider,
            bool addSceneEntityId,
            bool addPlacementMetadata,
            PlacementBakeTarget bakeTarget)
        {
            _surfaceSnapMode = surfaceSnapMode;
            _alignToSurface = alignToSurface;
            _heightOffset = heightOffset;
            _raycastMask = raycastMask;
            _ignoreTriggerColliders = ignoreTriggerColliders;
            _snapToGrid = snapToGrid;
            _gridSize = gridSize;
            _yawOffset = yawOffset;
            _randomRotation = randomRotation;
            _randomRotationXRange = randomRotationXRange;
            _randomRotationYRange = randomRotationYRange;
            _randomRotationZRange = randomRotationZRange;
            _autoSetupCollider = autoSetupCollider;
            _addSceneEntityId = addSceneEntityId;
            _addPlacementMetadata = addPlacementMetadata;
            _bakeTarget = bakeTarget;
        }
#endif
    }
}
