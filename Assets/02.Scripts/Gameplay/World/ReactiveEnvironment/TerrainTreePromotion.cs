using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;
using UPlayGround.Combat;
using UPlayGround.Core.ReactiveEnvironment;
using UPlayGround.Data.Save;
using UPlayGround.Gameplay.World.LivingWorld;
using UPlayGround.Manager;
using UPlayGround.Manager.World;

namespace UPlayGround.Gameplay.World.ReactiveEnvironment
{
    /// <summary>상호작용한 TerrainTree만 기존 반응 원장에 인계한다.</summary>
    public sealed class TerrainTreePromotion : MonoBehaviour
    {
        internal const string PersistenceSignature = "terrain-tree:1";
        [SerializeField] private Terrain[] _terrains;
        [SerializeField] private TerrainTreePromotionTileSO[] _tiles;
        [SerializeField] private EnvironmentController _weather;
        [SerializeField, Min(1)] private int _capacity = 32;
        [SerializeField, Min(1)] private int _proxyCapacity = 48;
        [SerializeField, Min(1)] private float _proxyRadius = 24;
        [SerializeField, Min(1)] private float _releaseDistance = 36;
        [SerializeField, Min(0.05f)] private float _proxyInterval = 0.2f;
        private const float CellSize = 8;
        private static readonly ProfilerMarker CommitMarker = new("ReactiveEnvironment.TreeTerrainCommit");
        private readonly List<Tile> _runtimeTiles = new();
        private readonly Dictionary<Vector2Int, List<Tree>> _cells = new();
        private readonly List<Slot> _slots = new();
        private readonly List<TerrainTreeHitProxy> _proxies = new();
        private readonly List<Tree> _near = new();
        private readonly List<Tree> _restore = new();
        private float _maxTrunkReach, _nextProxyRefresh;
        private Transform _poolRoot;
        private WorldStateManager _world;
        public string MapId { get; private set; }
        public int ActiveTreeCount { get; private set; }

        private sealed class Tile
        {
            public Terrain terrain;
            public ReactiveTerrainData data;
            public TreePrototype[] prototypes;
            public TreeInstance[] source, working;
            public bool instancesDirty, colliderDirty;
        }

        private sealed class Slot
        {
            public ReactiveEnvironmentObject prefab, actor;
            public Tree tree;
        }

        private sealed class Tree : IEnvironmentStructureHit
        {
            public TerrainTreePromotion owner;
            public TerrainTreePromotionTileSO.Species species;
            public Tile tile;
            public int index, stump;
            public string id;
            public Vector3 local, scale, trunkCenter;
            public Quaternion rotation;
            public float radius, height;
            public bool terminal;
            public Slot active;
            public TerrainTreeHitProxy proxy;
            public Vector3 Position => tile.terrain.transform.position + local;
            public Vector3 HitCenter => Position + trunkCenter;
            public Bounds Trunk => new(HitCenter, new Vector3(radius * 2, height, radius * 2));

            public CombatResult ReceiveHit(in HitRequest request)
            {
                if (!CanTakeDamage() || !request.EnvironmentHit.IsValid || !EnvironmentReaction.IsNonNegativeFinite(request.Damage)
                    || request.Damage <= 0 && request.EnvironmentHit.heatImpulse <= 0) return default;
                var actor = owner.TryPromote(this);
                return actor != null ? actor.ReceiveHit(request) : default;
            }

            public bool IsAlive() => !terminal && owner.isActiveAndEnabled && (active == null || active.actor.IsAlive());
            public bool CanTakeDamage() => IsAlive() && (active == null || active.actor.CanTakeDamage());
            public Transform GetTransform() => active?.actor.transform ?? proxy?.transform ?? tile.terrain.transform;
            public void LockOn() { }
            public void UnLockOn() { }
            public float GetHealthPercent() => active?.actor.GetHealthPercent() ?? (terminal ? 0 : 1);
            public float GetCurrentHealth() => active?.actor.GetCurrentHealth() ?? (terminal ? 0 : species.actor.Definition.reaction.maxDurability);
            public void ApplyHealingEffect(float healAmount) { }
        }

        private void Update()
        {
            if (_world == null)
            {
                var world = WorldStateManager.Instance;
                if (world == null || _weather == null || !_weather.isActiveAndEnabled) return;
                MapId = RegionOwnership.Resolve(this);
                if (string.IsNullOrEmpty(MapId)) return;
                _world = world;
                try { Initialize(); }
                catch (Exception error) { Debug.LogException(error, this); enabled = false; return; }
            }
            var player = GameObjectManager.Instance?.Player;
            Vector3 observer = player != null ? player.transform.position : transform.position;
            Reclaim(observer, false);
            for (int i = _restore.Count - 1; i >= 0 && ActiveTreeCount < _capacity; i--)
                if (TryPromote(_restore[i], true) != null) _restore.RemoveAt(i);
            if (Time.time < _nextProxyRefresh) return;
            _nextProxyRefresh = Time.time + _proxyInterval;
            UpdateProxies(observer);
        }

        private void Initialize()
        {
            if (_tiles == null || _terrains == null || _tiles.Length != _terrains.Length)
                throw new InvalidOperationException("Terrain과 나무 ID 표의 개수가 다릅니다.");
            _poolRoot = new GameObject("반응형 나무 풀").transform;
            _poolRoot.SetParent(transform, false);
            _poolRoot.gameObject.SetActive(false);
            var ids = new HashSet<string>();
            for (int i = 0; i < _tiles.Length; i++) InitializeTile(_terrains[i], _tiles[i], ids);
            var prefabs = new HashSet<ReactiveEnvironmentObject>();
            foreach (var tile in _tiles)
                foreach (var species in tile.species)
                    if (prefabs.Add(species.actor) && _slots.Count < _capacity) FindSlot(species.actor);
            _world.RegisterTreePromotion(this);
            RefreshSavedTrees();
        }

        private void InitializeTile(Terrain terrain, TerrainTreePromotionTileSO authoring, HashSet<string> ids)
        {
            var tile = new Tile { terrain = terrain, data = ReactiveTerrainData.Acquire(terrain) };
            _runtimeTiles.Add(tile);
            if (tile.data.Source != authoring.source)
                throw new InvalidOperationException($"{terrain.name}의 나무 원본이 ID 표와 다릅니다.");
            tile.source = tile.data.Source.treeInstances;
            tile.working = tile.data.Runtime.treeInstances;
            ValidateInstanceOrder(tile.source, tile.working, terrain.name);
            if (authoring.placements.Length != tile.source.Length)
                throw new InvalidOperationException($"{terrain.name}의 나무 ID 개수가 다릅니다.");
            tile.prototypes = tile.data.Source.treePrototypes;
            var prototypes = new List<TreePrototype>(tile.prototypes);
            var byPrototype = new Dictionary<int, (TerrainTreePromotionTileSO.Species species, int stump)>();
            foreach (var species in authoring.species)
            {
                int index = Array.FindIndex(tile.prototypes, prototype => prototype.prefab == species.source);
                if (index < 0 || species.actor == null || species.stump == null || species.standing == null)
                    throw new InvalidOperationException($"{terrain.name}의 승격 수종 참조가 누락됐습니다.");
                prototypes[index] = new TreePrototype(tile.prototypes[index]) { prefab = species.standing };
                byPrototype.Add(index, (species, prototypes.Count));
                prototypes.Add(new TreePrototype { prefab = species.stump });
            }
            for (int i = 0; i < tile.source.Length; i++)
            {
                TreeInstance instance = tile.source[i];
                var placement = authoring.placements[i];
                if (!instance.position.Equals(placement.position) || instance.prototypeIndex != placement.prototype)
                    throw new InvalidOperationException($"{terrain.name}의 나무 {i}가 ID 표와 다릅니다.");
                if (!byPrototype.TryGetValue(instance.prototypeIndex, out var variant)) continue;
                if (string.IsNullOrEmpty(placement.id) || !ids.Add(placement.id))
                    throw new InvalidOperationException($"{terrain.name}의 나무 ID가 누락되거나 중복됐습니다.");
                var scale = new Vector3(instance.widthScale, instance.heightScale, instance.widthScale);
                var rotation = Quaternion.Euler(0, instance.rotation * Mathf.Rad2Deg, 0);
                var tree = new Tree { owner = this, tile = tile, index = i, id = placement.id,
                    species = variant.species, stump = variant.stump, scale = scale, rotation = rotation,
                    local = Vector3.Scale(instance.position, tile.data.Source.size),
                    radius = variant.species.trunkRadius * Mathf.Max(scale.x, scale.y),
                    height = variant.species.trunkHeight * scale.y,
                    trunkCenter = rotation * Vector3.Scale(variant.species.trunkCenter, scale) };
                tree.height = Mathf.Max(tree.height, tree.radius * 2);
                _maxTrunkReach = Mathf.Max(_maxTrunkReach, tree.trunkCenter.magnitude + tree.height * 0.5f);
                Vector2Int cell = Cell(tree.Position);
                if (!_cells.TryGetValue(cell, out var entries)) _cells.Add(cell, entries = new List<Tree>());
                entries.Add(tree);
            }
            tile.data.Runtime.treePrototypes = prototypes.ToArray();
            tile.data.Runtime.SetTreeInstances(tile.working, false);
            ValidateInstanceOrder(tile.source, tile.data.Runtime.treeInstances, terrain.name);
            tile.colliderDirty = true;
        }

        internal static void ValidateInstanceOrder(TreeInstance[] source, TreeInstance[] runtime, string tileName)
        {
            if (source.Length != runtime.Length) throw new InvalidOperationException($"{tileName}의 나무 개수가 바뀌었습니다.");
            for (int i = 0; i < source.Length; i++)
                if (!source[i].position.Equals(runtime[i].position) || source[i].prototypeIndex != runtime[i].prototypeIndex
                    || source[i].heightScale != runtime[i].heightScale || source[i].widthScale != runtime[i].widthScale
                    || source[i].rotation != runtime[i].rotation)
                    throw new InvalidOperationException($"{tileName}의 런타임 나무 {i}가 원본과 다릅니다.");
        }

        internal void RefreshSavedTrees()
        {
            _restore.Clear();
            foreach (var cell in _cells.Values)
                foreach (var tree in cell)
                {
                    if (tree.active != null) continue;
                    bool hasRecord = GetRecord(tree, out var record);
                    tree.terminal = hasRecord && IsTerminal(record);
                    if (tree.terminal && tree.proxy != null) ClearProxy(tree.proxy);
                    SetTree(tree, tree.terminal ? tree.stump : -1);
                    if (!tree.terminal && hasRecord && (record.state.isBroken || record.state.combustion == EnvironmentCombustion.Burning))
                    {
                        if (tree.proxy != null) ClearProxy(tree.proxy);
                        SetTree(tree, -2);
                        _restore.Add(tree);
                    }
                }
        }

        private bool GetRecord(Tree tree, out ReactiveEnvironmentSaveEntry record) =>
            _world.TryGetTerrainTreeRecord(MapId, tree.id, out record)
            && record.persistenceSignature == PersistenceSignature && record.definitionId == tree.species.actor.Definition.definitionId;

        private static bool IsTerminal(ReactiveEnvironmentSaveEntry record) =>
            record.state.isBroken ? record.rewardCommitted && record.destruction?.completion?.phase == EnvironmentCompletionPhase.Cleared
                : record.state.combustion == EnvironmentCombustion.Spent;

        private ReactiveEnvironmentObject TryPromote(Tree tree, bool restoring = false)
        {
            if (tree.terminal || _world == null) return null;
            if (tree.active != null) return tree.active.actor;
            var slot = FindSlot(tree.species.actor);
            if (slot == null && !restoring && Reclaim(tree.Position, true)) slot = FindSlot(tree.species.actor);
            if (slot == null) return null;
            slot.tree = tree;
            tree.active = slot;
            var actor = slot.actor;
            actor.transform.SetPositionAndRotation(tree.Position, tree.rotation);
            actor.transform.localScale = tree.scale;
            if (!actor.ActivateTerrainTree(tree.id, tree, _weather, _world))
            {
                actor.gameObject.SetActive(false);
                slot.tree = null;
                tree.active = null;
                return null;
            }
            ActiveTreeCount++;
            if (tree.proxy != null) ClearProxy(tree.proxy);
            SetTree(tree, -2);
            return actor;
        }

        private Slot FindSlot(ReactiveEnvironmentObject prefab)
        {
            Slot empty = null;
            foreach (var slot in _slots)
            {
                if (slot.tree != null) continue;
                if (slot.prefab == prefab) return slot;
                empty = slot;
            }
            if (empty == null && _slots.Count >= _capacity) return null;
            if (empty == null)
            {
                empty = new Slot();
                _slots.Add(empty);
            }
            if (empty.actor != null) Destroy(empty.actor.gameObject);
            empty.prefab = prefab;
            empty.actor = Instantiate(prefab, _poolRoot);
            empty.actor.gameObject.SetActive(false);
            empty.actor.transform.SetParent(transform, false);
            return empty;
        }

        private bool Reclaim(Vector3 observer, bool saturated)
        {
            Slot farthest = null;
            float distance = saturated ? 0 : _releaseDistance * _releaseDistance;
            foreach (var slot in _slots)
            {
                var tree = slot.tree;
                if (tree == null || !GetRecord(tree, out var record)) continue;
                if (IsTerminal(record)) { Retire(slot, true); if (saturated) return true; continue; }
                var state = record.state;
                if (state.isBroken || state.combustion == EnvironmentCombustion.Burning || state.heat > 0
                    || record.pendingHeat > 0 || slot.actor.HasPendingFixedTick) continue;
                float squared = (observer - tree.Position).sqrMagnitude;
                if (squared < distance) continue;
                if (!saturated) Retire(slot, false);
                else { distance = squared; farthest = slot; }
            }
            if (farthest == null) return false;
            Retire(farthest, false);
            return true;
        }

        private void Retire(Slot slot, bool terminal)
        {
            var tree = slot.tree;
            slot.actor.gameObject.SetActive(false);
            slot.tree = null;
            tree.active = null;
            tree.terminal = terminal;
            ActiveTreeCount--;
            SetTree(tree, terminal ? tree.stump : -1);
        }

        private static void SetTree(Tree tree, int representation)
        {
            var tile = tree.tile;
            var value = tile.source[tree.index];
            if (representation >= 0) value.prototypeIndex = representation;
            if (representation == -2) value.heightScale = value.widthScale = 0;
            var current = tile.working[tree.index];
            if (current.prototypeIndex == value.prototypeIndex && current.heightScale == value.heightScale
                && current.widthScale == value.widthScale) return;
            tile.working[tree.index] = value;
            tile.instancesDirty = tile.colliderDirty = true;
        }

        private void LateUpdate()
        {
            foreach (var tile in _runtimeTiles)
            {
                if (!tile.colliderDirty) continue;
                using var measure = CommitMarker.Auto();
                if (tile.instancesDirty) tile.data.Runtime.SetTreeInstances(tile.working, false);
                tile.data.RefreshCollider();
                tile.colliderDirty = tile.instancesDirty = false;
            }
        }

        private Vector2Int Cell(Vector3 point)
        {
            point -= transform.position;
            return new Vector2Int(Mathf.FloorToInt(point.x / CellSize), Mathf.FloorToInt(point.z / CellSize));
        }

        internal void CollectHits(in CombatHitboxShape shape, ISet<IDamageable> ignored,
            ISet<IDamageable> collected, List<CombatHit> results)
        {
            float reach = shape.Type == CombatHitboxShapeType.Box ? shape.HalfExtents.magnitude
                : Vector3.Distance(shape.Center, shape.Point0) + shape.Radius;
            Vector3 extents = Vector3.one * (reach + _maxTrunkReach);
            var min = Cell(shape.Center - extents);
            var max = Cell(shape.Center + extents);
            for (int z = min.y; z <= max.y; z++)
            for (int x = min.x; x <= max.x; x++)
            {
                if (!_cells.TryGetValue(new Vector2Int(x, z), out var entries)) continue;
                foreach (var tree in entries)
                {
                    if (tree.active != null || tree.proxy != null || !tree.CanTakeDamage() || ignored?.Contains(tree) == true
                        || !TerrainDetailReaction.IntersectsCell(shape, tree.Trunk) || !collected.Add(tree)) continue;
                    Vector3 point = tree.Trunk.ClosestPoint(shape.Center);
                    Vector3 direction = (point - shape.Center).normalized;
                    results.Add(new CombatHit(tree, null, point, direction));
                }
            }
        }

        internal void PromoteInHeat(Vector3 source, float radius, int layer)
        {
            Vector3 extents = Vector3.one * (radius + _maxTrunkReach);
            var min = Cell(source - extents);
            var max = Cell(source + extents);
            for (int z = min.y; z <= max.y; z++)
            for (int x = min.x; x <= max.x; x++)
            {
                if (!_cells.TryGetValue(new Vector2Int(x, z), out var entries)) continue;
                foreach (var tree in entries)
                    if (!tree.terminal && tree.active == null && tree.species.actor.Definition.surfaceLayerId == layer
                        && (tree.HitCenter - source).sqrMagnitude < radius * radius) TryPromote(tree, true);
            }
        }

        private void UpdateProxies(Vector3 observer)
        {
            if (_proxies.Count == 0)
            {
                int layer = LayerMask.NameToLayer("InteractableObject");
                for (int i = 0; i < _proxyCapacity; i++)
                {
                    var node = new GameObject("줄기 타격", typeof(CapsuleCollider), typeof(TerrainTreeHitProxy));
                    node.transform.SetParent(transform, false);
                    node.layer = layer;
                    var proxy = node.GetComponent<TerrainTreeHitProxy>();
                    proxy.Capsule = node.GetComponent<CapsuleCollider>();
                    proxy.Capsule.isTrigger = true;
                    node.SetActive(false);
                    _proxies.Add(proxy);
                }
            }
            _near.Clear();
            var extents = Vector3.one * _proxyRadius;
            var min = Cell(observer - extents);
            var max = Cell(observer + extents);
            for (int z = min.y; z <= max.y; z++)
            for (int x = min.x; x <= max.x; x++)
            {
                if (!_cells.TryGetValue(new Vector2Int(x, z), out var entries)) continue;
                foreach (var tree in entries)
                {
                    if (tree.terminal || tree.active != null) continue;
                    float distance = (tree.Position - observer).sqrMagnitude;
                    if (distance >= _proxyRadius * _proxyRadius) continue;
                    int index = _near.Count;
                    while (index > 0 && (_near[index - 1].Position - observer).sqrMagnitude > distance) index--;
                    if (index >= _proxyCapacity) continue;
                    if (_near.Count == _proxyCapacity) _near.RemoveAt(_near.Count - 1);
                    _near.Insert(index, tree);
                }
            }
            foreach (var proxy in _proxies)
                if (proxy.HitTarget is Tree tree && !_near.Contains(tree)) ClearProxy(proxy);
            int free = 0;
            foreach (var tree in _near)
            {
                if (tree.proxy != null) continue;
                while (_proxies[free].HitTarget != null) free++;
                var proxy = _proxies[free++];
                proxy.HitTarget = tree;
                tree.proxy = proxy;
                proxy.transform.position = tree.HitCenter;
                proxy.Capsule.radius = tree.radius;
                proxy.Capsule.height = tree.height;
                proxy.gameObject.SetActive(true);
            }
        }

        private static void ClearProxy(TerrainTreeHitProxy proxy)
        {
            if (proxy.HitTarget is Tree tree) tree.proxy = null;
            proxy.HitTarget = null;
            proxy.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            if (_world != null) _world.UnregisterTreePromotion(this);
            foreach (var slot in _slots)
                if (slot.actor != null) { slot.actor.gameObject.SetActive(false); Destroy(slot.actor.gameObject); }
            foreach (var proxy in _proxies)
                if (proxy != null) { ClearProxy(proxy); Destroy(proxy.gameObject); }
            foreach (var tile in _runtimeTiles)
            {
                if (tile.prototypes != null && tile.data.Runtime != null)
                {
                    tile.data.Runtime.treePrototypes = tile.prototypes;
                    tile.data.Runtime.SetTreeInstances(tile.source, false);
                    tile.data.RefreshCollider();
                }
                tile.data.Release();
            }
            if (_poolRoot != null) Destroy(_poolRoot.gameObject);
            _world = null;
            _runtimeTiles.Clear(); _slots.Clear(); _cells.Clear(); _proxies.Clear(); _near.Clear(); _restore.Clear();
            _maxTrunkReach = _nextProxyRefresh = 0;
            ActiveTreeCount = 0;
        }
    }
}
