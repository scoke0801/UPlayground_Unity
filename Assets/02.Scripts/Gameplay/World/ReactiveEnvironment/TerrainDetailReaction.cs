using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Gameplay.World.LivingWorld;
using UPlayGround.Combat;
using UPlayGround.Components;
using UPlayGround.Core.ReactiveEnvironment;
using UPlayGround.Data.ReactiveEnvironment;
using UPlayGround.Data.Save;
using UPlayGround.Manager;

namespace UPlayGround.Gameplay.World.ReactiveEnvironment
{
    /// <summary>Terrain Detail의 명시 구역을 셀 상태·밀도·툰 마스크로 연결한다.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(Terrain), typeof(SceneEntityId))]
    public sealed class TerrainDetailReaction : MonoBehaviour
    {
        [SerializeField] private ReactiveEnvironmentDefinitionSO _definition;
        [SerializeField] private EnvironmentController _weather;
        [SerializeField, Min(1)] private int _partitionVersion = 1;
        [SerializeField, Min(1)] private int _detailPixelsPerCell = 16;
        [SerializeField] private RectInt _cellRegion = new(0, 0, 8, 8);
        [SerializeField] private int[] _detailLayers;
        [SerializeField] private GameObject[] _reactiveDetailPrefabs;
        [SerializeField] private string _contentRevision;
        [SerializeField, Min(1)] private int _maximumCells = 4096;
        [SerializeField, Min(0.01f)] private float _interactionHeight = 0.5f;
        private Terrain _terrain;
        private TerrainData _original, _runtime;
        private TerrainCollider _terrainCollider;
        private WorldStateManager _world;
        private Texture2D _mask;
        private bool _maskDirty;
        private readonly List<GameObject> _prototypes = new();
        private readonly List<Material> _materials = new();
        private Cell[,] _cells;
        private EnvironmentCombustionPresentation[,] _patches;
        public ReactiveEnvironmentDefinitionSO Definition => _definition;
        public int CellCount { get; private set; }
        public int DensityWrites { get; private set; }
        public int RuntimeMaterialCount => _materials.Count;
        public Texture2D StateMask => _mask;
        public TerrainData SourceData => _original != null ? _original : GetComponent<Terrain>().terrainData;
        public RectInt CellRegion => _cellRegion;
        public int DetailPixelsPerCell => _detailPixelsPerCell;

        private void Start()
        {
            if (_weather == null && Services.TryGet<UPlayGround.UI.IUIWeatherService>(out var weather))
                _weather = weather as EnvironmentController;
            if (Services.TryGet<IEnvironmentCellHitService>(out var service))
                Initialize(service as WorldStateManager);
        }
        private void OnEnable() { if (_world != null && _runtime == null) Initialize(_world); }
        private void OnDisable() => Release();

        /// <summary>원본을 복제한 뒤 셀을 같은 중앙 반응·저장 작성자에 등록한다. 실패하면 전부 복구한다.</summary>
        public void Initialize(WorldStateManager world)
        {
            if (_runtime != null) return;
            _terrain = GetComponent<Terrain>();
            _original = _terrain.terrainData;
            _world = world;
            string error = ValidateAuthoring();
            if (_world == null || error != null) throw new InvalidOperationException(error ?? "환경 작성자가 없습니다.");
            try
            {
                _runtime = Instantiate(_original);
                _runtime.name = _original.name + " (Reactive Runtime)";
                _runtime.hideFlags = HideFlags.DontSave;
                _mask = new Texture2D(_cellRegion.width, _cellRegion.height, TextureFormat.RGBA32, false, true)
                    { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
                var colors = new Color[_cellRegion.width * _cellRegion.height];
                _mask.SetPixels(colors); _mask.Apply(false);
                PreparePrototypes();
                _terrain.terrainData = _runtime;
                _terrainCollider = GetComponent<TerrainCollider>();
                if (_terrainCollider != null) _terrainCollider.terrainData = _runtime;
                _cells = new Cell[_cellRegion.height, _cellRegion.width];
                string guid = GetComponent<SceneEntityId>().Guid;
                string signature = _contentRevision + ":" + _partitionVersion + ":" + _detailPixelsPerCell
                    + ":" + _original.detailWidth + ":" + _original.detailHeight + ":" + string.Join(",", _detailLayers);
                for (int z = 0; z < _cellRegion.height; z++)
                for (int x = 0; x < _cellRegion.width; x++)
                {
                    var cell = new Cell(this, x + _cellRegion.x, z + _cellRegion.y, guid, signature);
                    if (!cell.HasDetails) continue;
                    _cells[z, x] = cell;
                    if (!_world.RegisterEnvironment(cell)) throw new InvalidOperationException("Terrain 셀 등록 실패: " + cell.StableId);
                    CellCount++;
                }
                _world.RegisterDetailTerrain(this);
                if (_definition.presentation != null)
                {
                    int side = Mathf.Max(1, _definition.presentation.terrainPatchCells);
                    _patches = new EnvironmentCombustionPresentation[Mathf.CeilToInt((float)_cellRegion.height / side),
                        Mathf.CeilToInt((float)_cellRegion.width / side)];
                    for (int z = 0; z < _patches.GetLength(0); z++)
                    for (int x = 0; x < _patches.GetLength(1); x++)
                        _patches[z, x] = new EnvironmentCombustionPresentation(_definition.presentation);
                }
                FlushMask();
            }
            catch { Release(); throw; }
        }

        /// <summary>구획·ID·레이어와 인스턴싱용 호환 프리팹을 검사한다. Tree 인계는 지원하지 않는다.</summary>
        public string ValidateAuthoring()
        {
            var data = SourceData;
            if (_definition == null || !_definition.IsValid() || _definition.reaction.canBreak
                || _definition.destruction != null || _definition.impactDrops.Count > 0 || _definition.fireDrops.Count > 0)
                return "풀 셀은 비파괴·무보상 프로필을 명시하세요.";
            if (_weather == null || !GetComponent<SceneEntityId>().HasGuid || string.IsNullOrEmpty(_contentRevision))
                return "날씨·Terrain 배치 ID·원본 revision이 필요합니다.";
            if (!EnvironmentReaction.IsNonNegativeFinite(_interactionHeight) || _interactionHeight <= 0
                || data == null || _partitionVersion < 1 || _detailPixelsPerCell < 1 || _maximumCells < 1
                || _cellRegion.width < 1 || _cellRegion.height < 1 || (long)_cellRegion.width * _cellRegion.height > _maximumCells
                || _cellRegion.xMin < 0 || _cellRegion.yMin < 0
                || _cellRegion.xMax * _detailPixelsPerCell > data.detailWidth
                || _cellRegion.yMax * _detailPixelsPerCell > data.detailHeight
                || transform.rotation != Quaternion.identity || transform.lossyScale != Vector3.one)
                return "Terrain 정규 변환·구획 범위·셀 예산을 확인하세요.";
            if (_detailLayers == null || _detailLayers.Length == 0 || _reactiveDetailPrefabs == null
                || _reactiveDetailPrefabs.Length != _detailLayers.Length) return "Detail 레이어와 대응 툰 프리팹을 명시하세요.";
            var seen = new HashSet<int>();
            for (int i = 0; i < _detailLayers.Length; i++)
            {
                int layer = _detailLayers[i];
                var prefab = _reactiveDetailPrefabs[i];
                if (layer < 0 || layer >= data.detailPrototypes.Length || !seen.Add(layer)
                    || prefab == null || prefab.GetComponent<MeshFilter>()?.sharedMesh == null
                    || prefab.GetComponent<MeshRenderer>() == null || prefab.GetComponentsInChildren<Collider>(true).Length > 0)
                    return "Detail 레이어·메시·Renderer·Collider 제외를 확인하세요.";
                foreach (var material in prefab.GetComponent<MeshRenderer>().sharedMaterials)
                    if (material == null || material.shader.name != "UPlayGround/Environment/Reactive Toon")
                        return "Detail 표현은 프로젝트 소유 Reactive Toon 프리팹을 사용하세요.";
            }
            return null;
        }

        private void PreparePrototypes()
        {
            var prototypes = _runtime.detailPrototypes;
            Vector3 size = _original.size;
            Vector3 origin = transform.position;
            Vector4 bounds = new(origin.x + size.x * _cellRegion.x / (_original.detailWidth / _detailPixelsPerCell),
                origin.z + size.z * _cellRegion.y / (_original.detailHeight / _detailPixelsPerCell),
                _original.detailWidth / (size.x * _detailPixelsPerCell * _cellRegion.width),
                _original.detailHeight / (size.z * _detailPixelsPerCell * _cellRegion.height));
            for (int i = 0; i < _detailLayers.Length; i++)
            {
                var prototype = Instantiate(_reactiveDetailPrefabs[i]);
                prototype.hideFlags = HideFlags.HideAndDontSave;
                prototype.SetActive(false);
                _prototypes.Add(prototype);
                var renderer = prototype.GetComponent<MeshRenderer>();
                var materials = renderer.sharedMaterials;
                for (int j = 0; j < materials.Length; j++)
                {
                    var material = new Material(materials[j]) { hideFlags = HideFlags.DontSave, enableInstancing = true };
                    material.SetTexture("_EnvironmentCellMask", _mask);
                    material.SetVector("_EnvironmentCellBounds", bounds);
                    material.SetFloat("_UseEnvironmentCells", 1);
                    materials[j] = material; _materials.Add(material);
                }
                renderer.sharedMaterials = materials;
                var detail = new DetailPrototype(prototypes[_detailLayers[i]]);
                detail.prototype = prototype; detail.usePrototypeMesh = true; detail.useInstancing = true;
                prototypes[_detailLayers[i]] = detail;
            }
            _runtime.detailPrototypes = prototypes;
        }

        /// <summary>프레임의 모든 셀 커밋 뒤 GPU 업로드를 한 번 수행한다.</summary>
        public void FlushMask()
        {
            if (!_maskDirty || _mask == null) return;
            _mask.Apply(false); _maskDirty = false;
        }

        /// <summary>여러 셀의 시각 효과만 패치로 묶고 전파·저장 셀은 유지한다.</summary>
        public void TickPresentation(float seconds)
        {
            if (_patches == null || _cells == null) return;
            int side = Mathf.Max(1, _definition.presentation.terrainPatchCells);
            for (int pz = 0; pz < _patches.GetLength(0); pz++)
            for (int px = 0; px < _patches.GetLength(1); px++)
            {
                EnvironmentReactionState state = default;
                Vector3 position = Vector3.zero;
                int count = 0, burning = 0;
                bool residual = false;
                for (int z = pz * side; z < Mathf.Min((pz + 1) * side, _cells.GetLength(0)); z++)
                for (int x = px * side; x < Mathf.Min((px + 1) * side, _cells.GetLength(1)); x++)
                {
                    var cell = _cells[z, x];
                    if (cell == null) continue;
                    var current = cell.PresentationState;
                    state.heat = Mathf.Max(state.heat, current.heat);
                    state.wetness += current.wetness;
                    state.fuel += current.fuel;
                    if (current.combustion == EnvironmentCombustion.Burning) burning++;
                    residual |= current.combustion == EnvironmentCombustion.Cooling || current.combustion == EnvironmentCombustion.Spent;
                    position += cell.ReactionPosition; count++;
                }
                if (count == 0) continue;
                state.wetness /= count;
                state.combustion = burning > 0 ? EnvironmentCombustion.Burning
                    : residual ? EnvironmentCombustion.Cooling : EnvironmentCombustion.Heating;
                var effect = _patches[pz, px];
                effect.Apply(state);
                effect.Tick(seconds, position / count, _weather.IsReady ? _weather.Current.Wind : Vector2.zero,
                    burning > 0 ? (float)burning / count : 1);
            }
        }

        /// <summary>범위 후보만 조회하고 셀마다 기존 공격 중복 제거 집합을 재사용한다.</summary>
        public void CollectHits(in CombatHitboxShape shape, ISet<IDamageable> ignored,
            ISet<IDamageable> collected, List<CombatHit> results)
        {
            if (_cells == null) return;
            // 셀은 면적 단위다. 회전 Box와 Capsule은 외접 AABB에 닿은 셀을 반응시킨다.
            Vector3 extents;
            Vector3 center = shape.Center;
            if (shape.Type == CombatHitboxShapeType.Box)
            {
                var rotation = Matrix4x4.Rotate(shape.Rotation);
                extents = Abs(rotation.MultiplyVector(Vector3.right)) * shape.HalfExtents.x
                    + Abs(rotation.MultiplyVector(Vector3.up)) * shape.HalfExtents.y
                    + Abs(rotation.MultiplyVector(Vector3.forward)) * shape.HalfExtents.z;
            }
            else if (shape.Type == CombatHitboxShapeType.Capsule)
            {
                center = (shape.Point0 + shape.Point1) * 0.5f;
                extents = Abs(shape.Point1 - shape.Point0) * 0.5f + Vector3.one * shape.Radius;
            }
            else extents = Vector3.one * shape.Radius;
            var bounds = new Bounds(center, extents * 2);
            Vector3 size = _original.size, origin = transform.position;
            float width = size.x * _detailPixelsPerCell / _original.detailWidth;
            float depth = size.z * _detailPixelsPerCell / _original.detailHeight;
            int x0 = Mathf.Max(_cellRegion.xMin, Mathf.FloorToInt((bounds.min.x - origin.x) / width));
            int x1 = Mathf.Min(_cellRegion.xMax - 1, Mathf.FloorToInt((bounds.max.x - origin.x) / width));
            int z0 = Mathf.Max(_cellRegion.yMin, Mathf.FloorToInt((bounds.min.z - origin.z) / depth));
            int z1 = Mathf.Min(_cellRegion.yMax - 1, Mathf.FloorToInt((bounds.max.z - origin.z) / depth));
            for (int z = z0; z <= z1; z++)
            for (int x = x0; x <= x1; x++)
            {
                var cell = _cells[z - _cellRegion.y, x - _cellRegion.x];
                if (cell == null || !cell.IsAlive() || collected.Contains(cell) || ignored != null && ignored.Contains(cell)
                    || !IntersectsCell(shape, cell.Bounds)) continue;
                collected.Add(cell);
                results.Add(new CombatHit(cell, null, cell.ReactionPosition, Vector3.up));
            }
        }

        private static bool IntersectsCell(in CombatHitboxShape shape, Bounds cell)
        {
            if (shape.Type == CombatHitboxShapeType.Sphere)
                return (cell.ClosestPoint(shape.Center) - shape.Center).sqrMagnitude <= shape.Radius * shape.Radius;
            if (shape.Type == CombatHitboxShapeType.Capsule)
                return SegmentBoxDistanceSquared(shape.Point0, shape.Point1, cell) <= shape.Radius * shape.Radius;
            Vector3 right = shape.Rotation * Vector3.right, up = shape.Rotation * Vector3.up,
                forward = shape.Rotation * Vector3.forward, difference = shape.Center - cell.center;
            for (int i = 0; i < 3; i++)
            {
                Vector3 worldAxis = Axis(i), boxAxis = i == 0 ? right : i == 1 ? up : forward;
                if (HasSeparatingAxis(worldAxis, difference, cell.extents, shape.HalfExtents, right, up, forward)
                    || HasSeparatingAxis(boxAxis, difference, cell.extents, shape.HalfExtents, right, up, forward)) return false;
                for (int j = 0; j < 3; j++)
                    if (HasSeparatingAxis(Vector3.Cross(worldAxis, j == 0 ? right : j == 1 ? up : forward),
                        difference, cell.extents, shape.HalfExtents, right, up, forward)) return false;
            }
            return true;
        }

        private static Vector3 Axis(int index) => index == 0 ? Vector3.right : index == 1 ? Vector3.up : Vector3.forward;

        private static bool HasSeparatingAxis(Vector3 axis, Vector3 difference, Vector3 cell,
            Vector3 box, Vector3 right, Vector3 up, Vector3 forward) =>
            Mathf.Abs(Vector3.Dot(difference, axis)) > Vector3.Dot(Abs(axis), cell)
                + Mathf.Abs(Vector3.Dot(right, axis)) * box.x + Mathf.Abs(Vector3.Dot(up, axis)) * box.y
                + Mathf.Abs(Vector3.Dot(forward, axis)) * box.z + 0.00001f;

        private static float SegmentBoxDistanceSquared(Vector3 start, Vector3 end, Bounds bounds)
        {
            // slab 경계 사이 거리 제곱은 이차식이므로 구간별 최소값을 찾는다.
            System.Span<float> points = stackalloc float[8];
            int count = 2; points[0] = 0; points[1] = 1;
            Vector3 delta = end - start, min = bounds.min, max = bounds.max;
            for (int axis = 0; axis < 3; axis++)
            {
                if (Mathf.Abs(delta[axis]) < 0.000001f) continue;
                float a = (min[axis] - start[axis]) / delta[axis], b = (max[axis] - start[axis]) / delta[axis];
                if (a > 0 && a < 1) points[count++] = a;
                if (b > 0 && b < 1) points[count++] = b;
            }
            for (int i = 1; i < count; i++)
                for (int j = i; j > 0 && points[j] < points[j-1]; j--)
                { float value = points[j]; points[j] = points[j-1]; points[j-1] = value; }
            float result = float.MaxValue;
            for (int i = 1; i < count; i++)
            {
                float middle = (points[i-1] + points[i]) * 0.5f, squared = 0, linear = 0;
                for (int axis = 0; axis < 3; axis++)
                {
                    float value = start[axis] + middle * delta[axis];
                    if (value >= min[axis] && value <= max[axis]) continue;
                    float offset = start[axis] - (value < min[axis] ? min[axis] : max[axis]);
                    squared += delta[axis] * delta[axis]; linear += delta[axis] * offset;
                }
                float time = squared > 0 ? Mathf.Clamp(-linear / squared, points[i-1], points[i]) : middle;
                Vector3 position = start + time * delta;
                result = Mathf.Min(result, (position - bounds.ClosestPoint(position)).sqrMagnitude);
            }
            return result;
        }

        private static Vector3 Abs(Vector3 value) => new(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));

        /// <summary>언로드는 기존 상태를 보존하고 원본 TerrainData·Collider를 복구한다.</summary>
        public void Release()
        {
            if (_patches != null) foreach (var patch in _patches) patch.Release();
            _patches = null;
            if (_cells != null && _world != null)
                foreach (var cell in _cells) if (cell != null) _world.UnregisterEnvironment(cell);
            _world?.UnregisterDetailTerrain(this);
            _cells = null; CellCount = 0;
            if (_terrain != null && _terrain.terrainData == _runtime) _terrain.terrainData = _original;
            if (_terrainCollider != null && _terrainCollider.terrainData == _runtime) _terrainCollider.terrainData = _original;
            foreach (var prototype in _prototypes) DestroyOwned(prototype);
            foreach (var material in _materials) DestroyOwned(material);
            _prototypes.Clear(); _materials.Clear();
            DestroyOwned(_runtime); DestroyOwned(_mask);
            _runtime = null; _mask = null; _maskDirty = false;
        }

        private static void DestroyOwned(UnityEngine.Object value)
        { if (value != null) { if (Application.isPlaying) Destroy(value); else DestroyImmediate(value); } }

        /// <summary>셀당 C# 객체만 두고 잎별 GameObject·Collider는 만들지 않는다.</summary>
        private sealed class Cell : IEnvironmentReactionTarget, IDamageable
        {
            private readonly TerrainDetailReaction _owner;
            private readonly int _x, _z;
            private readonly int[][,] _density;
            private readonly int[][,] _output;
            private int _coverage = 255;
            private Color32 _maskColor;
            private EnvironmentReactionState _state;
            public EnvironmentReactionState PresentationState => _state;
            private bool _hasSnapshot;
            public bool HasDetails { get; }
            public Transform transform => _owner.transform;
            public Transform OcclusionRoot => null;
            public bool IsAvailable => _owner != null && _owner._runtime != null;
            public string StableId { get; }
            public string PersistenceSignature { get; }
            public ReactiveEnvironmentDefinitionSO Definition => _owner._definition;
            public Vector3 ReactionPosition { get; }
            public Bounds Bounds { get; }
            public Vector2 Wind => _owner._weather.IsReady ? _owner._weather.Current.Wind : Vector2.zero;

            public Cell(TerrainDetailReaction owner, int x, int z, string guid, string signature)
            {
                _owner = owner; _x = x; _z = z;
                StableId = guid + ":detail:" + x + ":" + z;
                int pixels = owner._detailPixelsPerCell;
                var size = owner._original.size;
                float px = (x + 0.5f) * pixels / owner._original.detailWidth;
                float pz = (z + 0.5f) * pixels / owner._original.detailHeight;
                Vector3 groundPosition = owner.transform.position + new Vector3(px * size.x,
                    owner._original.GetInterpolatedHeight(px, pz), pz * size.z);
                ReactionPosition = groundPosition + Vector3.up * owner._interactionHeight * 0.5f;
                Bounds = new Bounds(ReactionPosition,
                    new Vector3(size.x * pixels / owner._original.detailWidth, owner._interactionHeight,
                        size.z * pixels / owner._original.detailHeight));
                _density = new int[owner._detailLayers.Length][,];
                _output = new int[_density.Length][,];
                ulong densityHash = 14695981039346656037UL;
                for (int i = 0; i < _density.Length; i++)
                {
                    _density[i] = owner._original.GetDetailLayer(x * pixels, z * pixels, pixels, pixels, owner._detailLayers[i]);
                    _output[i] = new int[pixels, pixels];
                    foreach (int count in _density[i])
                    {
                        if (count > 0) HasDetails = true;
                        unchecked { densityHash = (densityHash ^ (uint)count) * 1099511628211UL; }
                    }
                }
                PersistenceSignature = signature + ":" + densityHash.ToString("X16");
            }
            public bool IsAuthoringValid() => IsAvailable && HasDetails;
            public void SampleRain(out float rain, out float exposure)
            {
                rain = _owner._weather.IsReady ? _owner._weather.Current.Rain : 0;
                exposure = rain > 0 && !_owner._weather.IsRainSheltered(ReactionPosition, null) ? 1 : 0;
            }
            public void ApplySnapshot(EnvironmentReactionState state, EnvironmentDestructionSaveState destruction = null)
            {
                if (_hasSnapshot && _state.HasSameValues(state)) { _state = state; return; }
                _hasSnapshot = true;
                _state = state;
                Color32 color = new Color(state.wetness, state.charAmount, 0,
                    state.combustion == EnvironmentCombustion.Burning ? 1 : 0);
                if (color.r != _maskColor.r || color.g != _maskColor.g || color.a != _maskColor.a)
                {
                    _maskColor = color;
                    _owner._mask.SetPixel(_x - _owner._cellRegion.x, _z - _owner._cellRegion.y, color);
                    _owner._maskDirty = true;
                }
                int coverage = Mathf.RoundToInt((Definition.reaction.initialFuel > 0
                    ? state.fuel / Definition.reaction.initialFuel : 1) * 255);
                if (_coverage == coverage) return;
                _coverage = coverage;
                for (int i = 0; i < _density.Length; i++)
                {
                    int pixels = _owner._detailPixelsPerCell;
                    for (int z = 0; z < pixels; z++)
                    for (int x = 0; x < pixels; x++)
                        _output[i][z, x] = Mathf.RoundToInt(_density[i][z, x] * coverage / 255f);
                    _owner._runtime.SetDetailLayer(_x * pixels, _z * pixels, _owner._detailLayers[i], _output[i]);
                    _owner.DensityWrites++;
                }
            }
            public void FixedTick(float seconds) { }
            public CombatResult ReceiveHit(in HitRequest request)
            {
                bool accepted = IsAlive() && _owner._world.ApplyEnvironmentCombatHit(this, request.Damage, request.EnvironmentHit);
                return new CombatResult(new HitContext(request, null), new DefenseResult(DefenseOutcome.None, accepted),
                    new DamageResult(request.Damage, 0, 1, 0, 1, 1, false, default), default, default);
            }
            public bool IsAlive() => IsAvailable;
            public bool CanTakeDamage() => IsAlive();
            public Transform GetTransform() => transform;
            public void LockOn() { }
            public void UnLockOn() { }
            public float GetHealthPercent() => _state.durability / Definition.reaction.maxDurability;
            public float GetCurrentHealth() => _state.durability;
            public void ApplyHealingEffect(float amount) { }
        }
    }
}
