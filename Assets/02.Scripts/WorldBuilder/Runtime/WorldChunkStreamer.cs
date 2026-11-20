using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UPlayGround.WorldBuilder.Data;

namespace UPlayGround.WorldBuilder.Runtime
{
    public enum WorldChunkState { Unloaded, Requested, Loading, Initializing, Ready, Unloading, Failed }

    public sealed class WorldChunkStreamer : MonoBehaviour
    {
        private static readonly ProfilerMarker UpdateMarker = new(ProfilerCategory.Scripts, "WorldBuilder.Streaming.Update");
        private static readonly ProfilerMarker ActivateGroupMarker = new(ProfilerCategory.Scripts, "WorldBuilder.Streaming.ActivateGroup");

        private sealed class Chunk
        {
            public WorldChunkEntry entry;
            public WorldChunkState state;
            public AsyncOperationHandle<SceneInstance> scene, unload;
            public WorldChunkContent content;
            public GameObject proxy;
            public bool desired, forced, retirementFailed;
            public float readyAt, started, priority, retryUnloadAt;
            public string failure;
        }

        [SerializeField] private TextAsset publishedManifest;
        [SerializeField] private WorldChunkCatalog[] revisions;
        [SerializeField] private Material proxyMaterial;
        [SerializeField] private WorldBakedNavigation navigation;

        private Chunk[] _chunks;
        private WorldChunkCatalog _catalog;
        private WorldPublishedPointer _pointer;
        private AsyncOperationHandle<WorldChunkCatalog> _catalogLoad;
        private string _catalogFailure;
        private Terrain[] _readyTerrains;
        private readonly HashSet<int> _closure = new();
        private readonly HashSet<int> _entry = new();
        private readonly Dictionary<int, int> _pins = new();
        private Transform _player;
        private Vector3 _previous;
        private bool _hasPrevious, _closing;
        private float _loadSeconds;
        public WorldChunkCatalog Catalog => _catalog;
        public bool IsClosing => _closing;
        public int OwnedSceneCount => _chunks?.Count(c => c.scene.IsValid()) ?? 0;
        public int OwnedAssetCount => _catalogLoad.IsValid() ? 1 : 0;
        public int PinCount => _pins.Values.Sum();
        public float DiagnosticDelay { get; set; }
        public float? DiagnosticMinimumResidency { get; set; }
        public Predicate<string> DiagnosticFailure { get; set; }
        public Action<WorldBakedIdentity> ApplyState { get; set; }
        public Action<WorldBakedIdentity> CaptureState { get; set; }
        public event Action<Scene, bool> ChunkVisibilityChanged;
        public event Action<Scene> ChunkPreparing;
        public WorldChunkState GetState(int index) => _chunks[index].state;
        public string GetFailure(int index) => _chunks[index].failure;

        public void Configure(TextAsset manifest, WorldChunkCatalog[] catalogs, WorldBakedNavigation owner, Material material)
        {
            publishedManifest = manifest;
            revisions = catalogs;
            navigation = owner;
            proxyMaterial = material;
        }

        private void Awake()
        {
            try
            {
                if (publishedManifest == null || navigation == null)
                    throw new InvalidOperationException("발행 목록 또는 Navigation이 연결되지 않았습니다.");
                _pointer = JsonUtility.FromJson<WorldPublishedPointer>(publishedManifest.text);
                if (_pointer == null || !Guid.TryParseExact(_pointer.current, "N", out _) ||
                    !Guid.TryParseExact(_pointer.worldGuid, "N", out _) || !Guid.TryParseExact(_pointer.catalogGuid, "N", out _))
                    throw new InvalidOperationException("발행 목록을 확인할 수 없습니다.");
                var selected = Array.Find(revisions ?? Array.Empty<WorldChunkCatalog>(), MatchesPointer);
                if (selected != null) SelectPublished(selected);
                else _catalogLoad = Addressables.LoadAssetAsync<WorldChunkCatalog>(WorldChunkCatalog.Address(_pointer.worldGuid, _pointer.current));
            }
            catch (Exception error) { _catalogFailure = error.Message; }
        }

        private bool MatchesPointer(WorldChunkCatalog candidate) => candidate != null && candidate.bake != null &&
            candidate.bake.worldGuid == _pointer.worldGuid && candidate.bake.buildId == _pointer.current &&
            candidate.bakeCatalogGuid == _pointer.catalogGuid;

        private void SelectPublished(WorldChunkCatalog selected)
        {
            if (!MatchesPointer(selected) || selected.chunks == null || selected.chunks.Length == 0)
                throw new InvalidOperationException("발행 목록과 청크 catalog가 일치하지 않습니다.");
            _catalog = selected;
            navigation.ConfigureStreaming(_catalog.bake, _catalog.boundaryCrossings);
            _chunks = _catalog.chunks.Select(entry => new Chunk { entry = entry, proxy = CreateProxy(entry) }).ToArray();
            _readyTerrains = new Terrain[_chunks.Length];
        }

        public IEnumerator WaitForCatalog(Action<string> result)
        {
            float timeout = Time.realtimeSinceStartup + 30;
            while (!_closing && _catalog == null && _catalogFailure == null && Time.realtimeSinceStartup < timeout) yield return null;
            if (_catalog == null && _catalogFailure == null && !_closing)
            {
                _catalogFailure = "월드 catalog 준비가 지연되고 있습니다.";
                ReleaseCatalog();
            }
            result(_catalog != null && !_closing ? null : _catalogFailure ?? "월드 준비가 취소되었습니다.");
        }

        private GameObject CreateProxy(WorldChunkEntry entry)
        {
            var proxy = new GameObject("Terrain Proxy " + entry.id);
            proxy.transform.SetParent(transform, false);
            proxy.AddComponent<MeshFilter>().sharedMesh = entry.proxy;
            proxy.AddComponent<MeshRenderer>().sharedMaterial = proxyMaterial;
            return proxy;
        }

        public void TrackPlayer(Transform player)
        {
            _player = player;
            _hasPrevious = false;
        }

        public int FindChunk(Vector3 position)
        {
            if (_catalog == null || !float.IsFinite(position.x) || !float.IsFinite(position.z)) return -1;
            for (int i = 0; i < _catalog.chunks.Length; i++)
            {
                var b = _catalog.chunks[i].bounds;
                if (position.x >= b.min.x && position.x < b.max.x && position.z >= b.min.z && position.z < b.max.z) return i;
            }
            return -1;
        }

        public IDisposable Pin(Vector3 position)
        {
            int index = FindChunk(position);
            if (index < 0 || _closing) throw new ArgumentOutOfRangeException(nameof(position));
            _pins.TryGetValue(index, out int count);
            _pins[index] = count + 1;
            return new PinLease(this, index);
        }

        private sealed class PinLease : IDisposable
        {
            private WorldChunkStreamer _owner;
            private readonly int _index;
            public PinLease(WorldChunkStreamer owner, int index) { _owner = owner; _index = index; }
            public void Dispose()
            {
                if (_owner == null) return;
                if (_owner._pins.TryGetValue(_index, out int count) && count > 1) _owner._pins[_index] = count - 1;
                else _owner._pins.Remove(_index);
                _owner = null;
            }
        }

        public bool CanTraverse(Vector3 from, Vector3 to)
        {
            if (_chunks == null || _closing || FindChunk(from) < 0 || FindChunk(to) < 0) return false;
            from.y = to.y = 0;
            Vector3 segment = to - from;
            var ray = new Ray(from, segment.normalized);
            float distance = segment.magnitude;
            foreach (var chunk in _chunks)
            {
                if (chunk.state == WorldChunkState.Ready) continue;
                var bounds = chunk.entry.bounds;
                bounds.center = new Vector3(bounds.center.x, 0, bounds.center.z);
                if (bounds.Contains(to) || (distance > 0 && bounds.IntersectRay(ray, out float hit) && hit < distance))
                    return false;
            }
            return true;
        }

        public IEnumerator PrepareDestination(Vector3 destination, Action<string> result)
        {
            int index = FindChunk(destination);
            if (_chunks == null || _closing || index < 0 || _entry.Count != 0)
            {
                result("이동할 곳을 준비할 수 없습니다.");
                yield break;
            }
            AddRing(_entry, index, _catalog.settings.readyRing);
            AddDependencies(_entry);
            foreach (int i in _entry)
            {
                var chunk = _chunks[i];
                if (chunk.state == WorldChunkState.Failed) chunk.state = WorldChunkState.Unloaded;
                if (chunk.state == WorldChunkState.Unloading && chunk.retirementFailed) chunk.retirementFailed = false;
                chunk.failure = null;
            }

            try
            {
                float timeout = Time.realtimeSinceStartup + _catalog.settings.travelTimeout;
                while (!_closing && Time.realtimeSinceStartup < timeout)
                {
                    if (_entry.All(i => _chunks[i].state == WorldChunkState.Ready))
                    {
                        result(null);
                        yield break;
                    }
                    int failed = _entry.Where(i => _chunks[i].state == WorldChunkState.Failed).DefaultIfEmpty(-1).First();
                    if (failed >= 0)
                    {
                        result(_chunks[failed].failure);
                        yield break;
                    }
                    yield return null;
                }
                result("이동할 곳을 불러오는 데 시간이 너무 오래 걸렸습니다.");
            }
            finally { _entry.Clear(); }
        }

        private void AddRing(HashSet<int> set, int center, int radius)
        {
            if (center < 0) return;
            Vector2Int tile = _chunks[center].entry.tile;
            for (int i = 0; i < _chunks.Length; i++)
            {
                Vector2Int delta = _chunks[i].entry.tile - tile;
                if (Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y)) <= radius) set.Add(i);
            }
        }

        private void AddDependencies(HashSet<int> set)
        {
            for (int i = 0; i < _chunks.Length; i++)
            {
                if (!set.Contains(i)) continue;
                foreach (int dependency in _chunks[i].entry.dependencies)
                    if (set.Add(dependency)) i = -1;
            }
        }

        private void Update()
        {
            using var sample = UpdateMarker.Auto();
            if (!_closing && _catalog == null && _catalogLoad.IsValid() && _catalogLoad.IsDone)
            {
                try
                {
                    if (_catalogLoad.Status != AsyncOperationStatus.Succeeded)
                        throw _catalogLoad.OperationException ?? new InvalidOperationException("월드 catalog 로딩 실패");
                    SelectPublished(_catalogLoad.Result);
                }
                catch (Exception error) { _catalogFailure = error.Message; ReleaseCatalog(); }
            }
            if (_chunks == null) return;
            float now = Time.realtimeSinceStartup;
            Vector3 position = _player != null ? _player.position : default;
            Vector3 direction = _hasPrevious ? position - _previous : Vector3.zero;
            direction.y = 0;
            _previous = position;
            _hasPrevious = _player != null;
            var settings = _catalog.settings;
            int center = _player != null ? FindChunk(position) : -1;
            Vector3 prediction = position + direction.normalized * settings.lookAheadMargin +
                Vector3.ClampMagnitude(direction / Mathf.Max(Time.unscaledDeltaTime, .001f) * _loadSeconds, _catalog.tileSize * 2);

            _closure.Clear();
            AddRing(_closure, center, settings.readyRing);
            AddRing(_closure, center >= 0 ? FindChunk(prediction) : -1, settings.readyRing);
            _closure.UnionWith(_entry);
            _closure.UnionWith(_pins.Keys);
            AddDependencies(_closure);
            for (int i = 0; i < _chunks.Length; i++) _chunks[i].forced = _closure.Contains(i);
            AddRing(_closure, center, settings.preloadRing);
            for (int i = 0; i < _chunks.Length; i++)
            {
                var c = _chunks[i];
                var flat = new Vector3(position.x, c.entry.bounds.center.y, position.z);
                if (c.state == WorldChunkState.Ready && (now - c.readyAt < (DiagnosticMinimumResidency ?? settings.minimumResidency) ||
                    (center >= 0 && c.entry.bounds.SqrDistance(flat) <= settings.hysteresis * settings.hysteresis)))
                    _closure.Add(i);
            }
            if (_closing) _closure.Clear();
            AddDependencies(_closure);

            for (int i = 0; i < _chunks.Length; i++)
            {
                var c = _chunks[i];
                bool wasDesired = c.desired;
                c.desired = _closure.Contains(i);
                var flat = new Vector3(prediction.x, c.entry.bounds.center.y, prediction.z);
                c.priority = c.entry.bounds.SqrDistance(flat);
                if (c.state == WorldChunkState.Failed && c.desired && !wasDesired && !c.scene.IsValid()) c.state = WorldChunkState.Unloaded;
                if (c.state == WorldChunkState.Unloaded && c.desired) c.state = WorldChunkState.Requested;
                if (c.state == WorldChunkState.Requested && !c.desired) c.state = WorldChunkState.Unloaded;
                if (c.state == WorldChunkState.Loading && c.scene.IsDone && now - c.started >= DiagnosticDelay) FinishLoad(c, now);
                if (c.state == WorldChunkState.Unloading && c.unload.IsValid() && c.unload.IsDone) FinishUnload(c, now);
                if (!c.desired && (c.state == WorldChunkState.Ready || c.state == WorldChunkState.Initializing))
                    Retire(c);
            }

            for (int i = 0; i < _chunks.Length; i++)
            {
                if (_chunks[i].state != WorldChunkState.Initializing) continue;
                _closure.Clear();
                _closure.Add(i);
                AddDependencies(_closure);
                int failed = _closure.Where(j => _chunks[j].state == WorldChunkState.Failed).DefaultIfEmpty(-1).First();
                if (failed >= 0)
                {
                    Fail(_chunks[i], $"필수 청크 {_chunks[failed].entry.id}를 준비하지 못했습니다. {_chunks[failed].failure}");
                    continue;
                }
                if (_closure.All(j => _chunks[j].state is WorldChunkState.Initializing or WorldChunkState.Ready))
                    ActivateGroup(now);
            }

            int unloadSlots = settings.parallelUnloads - _chunks.Count(c => c.unload.IsValid());
            foreach (var c in _chunks)
            {
                if (unloadSlots <= 0) break;
                if (c.state != WorldChunkState.Unloading || c.unload.IsValid() || now < c.retryUnloadAt) continue;
                StartUnload(c, now);
                unloadSlots--;
            }
            int loadSlots = settings.parallelLoads - _chunks.Count(c => c.state == WorldChunkState.Loading);
            while (loadSlots-- > 0)
            {
                Chunk next = null;
                foreach (var c in _chunks)
                    if (c.state == WorldChunkState.Requested && (next == null ||
                        (c.forced == next.forced ? c.priority < next.priority : c.forced))) next = c;
                if (next == null) break;
                try
                {
                    next.started = now;
                    next.failure = null;
                    next.scene = Addressables.LoadSceneAsync(next.entry.sceneKey, LoadSceneMode.Additive, activateOnLoad: true);
                    next.state = WorldChunkState.Loading;
                }
                catch (Exception error) { Fail(next, error.Message); }
            }
        }

        private void FinishLoad(Chunk chunk, float now)
        {
            if (chunk.scene.Status != AsyncOperationStatus.Succeeded)
            {
                string reason = chunk.scene.OperationException?.Message ?? "청크 로딩 실패";
                Addressables.Release(chunk.scene);
                chunk.scene = default;
                if (chunk.desired) Fail(chunk, reason);
                else chunk.state = WorldChunkState.Unloaded;
                return;
            }
            _loadSeconds = Mathf.Lerp(_loadSeconds, now - chunk.started, .25f);
            if (!chunk.desired) { Retire(chunk); return; }
            try
            {
                if (DiagnosticFailure?.Invoke(chunk.entry.id) == true) throw new InvalidOperationException("검증용 청크 초기화 실패");
                var scene = chunk.scene.Result.Scene;
                var content = scene.GetRootGameObjects().SelectMany(root => root.GetComponents<WorldChunkContent>()).Single();
                if (content.revision != _catalog.bake.buildId || content.worldGuid != _catalog.bake.worldGuid || content.chunkId != chunk.entry.id ||
                    content.content.activeSelf || content.terrain == null || !content.terrain.TryGetComponent<TerrainCollider>(out var collider) ||
                    !collider.enabled || collider.terrainData != content.terrain.terrainData)
                    throw new InvalidOperationException("청크 소유권 또는 충돌 데이터 오류 · " + chunk.entry.id);
                chunk.content = content;
                ChunkPreparing?.Invoke(scene);
                foreach (var water in content.water) water.BindTerrains(_readyTerrains);
                foreach (var waterfall in content.content.GetComponentsInChildren<WorldWaterfallEffects>(true))
                    waterfall.FollowMainCamera = true;
                foreach (var identity in content.identities) ApplyState?.Invoke(identity);
                chunk.state = WorldChunkState.Initializing;
            }
            catch (Exception error) { Fail(chunk, error.Message); }
        }

        private void ActivateGroup(float now)
        {
            using var sample = ActivateGroupMarker.Auto();
            var pending = _closure.Where(i => _chunks[i].state == WorldChunkState.Initializing).ToArray();
            try
            {
                foreach (int i in pending)
                    foreach (var surface in _chunks[i].entry.surfaces) navigation.PrepareSurface(surface);
                foreach (int i in pending)
                {
                    var c = _chunks[i];
                    c.content.content.SetActive(true);
                    _readyTerrains[i] = c.content.terrain;
                }
                Physics.SyncTransforms();
                foreach (int i in pending)
                    foreach (var surface in _chunks[i].entry.surfaces) navigation.SetSurfaceReady(surface, true, true);
                foreach (int i in pending)
                foreach (int crossing in _chunks[i].content.crossingIndices)
                    if (!navigation.IsLinkReady(crossing))
                        throw new InvalidOperationException("필수 통과 링크를 열지 못했습니다.");
                foreach (int i in pending)
                {
                    var c = _chunks[i];
                    c.state = WorldChunkState.Ready;
                    c.readyAt = now;
                    c.proxy.SetActive(false);
                    ChunkVisibilityChanged?.Invoke(c.content.gameObject.scene, true);
                }
                RefreshBoundaries();
            }
            catch (Exception error)
            {
                foreach (int i in pending) Fail(_chunks[i], error.Message);
            }
        }

        private void RefreshBoundaries()
        {
            for (int i = 0; i < _chunks.Length; i++)
            {
                var c = _chunks[i];
                if (c.state != WorldChunkState.Ready) continue;
                var tile = c.entry.tile;
                c.content.terrain.SetNeighbors(Neighbor(-1, 0), Neighbor(0, 1), Neighbor(1, 0), Neighbor(0, -1));
                for (int j = 0; j < c.content.crossingIndices.Length; j++)
                {
                    bool ready = navigation.IsLinkReady(c.content.crossingIndices[j]);
                    if (c.content.crossingBarriers[j] != null) c.content.crossingBarriers[j].enabled = !ready;
                    var gate = c.content.gates[j];
                    if (gate == null) continue;
                    gate.SetEndpointReady(0, ready);
                    gate.SetEndpointReady(1, ready);
                }

                Terrain Neighbor(int x, int z)
                {
                    var target = tile + new Vector2Int(x, z);
                    for (int j = 0; j < _chunks.Length; j++)
                        if (_chunks[j].entry.tile == target) return _readyTerrains[j];
                    return null;
                }
            }
        }

        private void Fail(Chunk chunk, string reason)
        {
            chunk.failure = reason;
            chunk.retirementFailed = true;
            if (chunk.scene.IsValid()) Retire(chunk);
            else { chunk.state = WorldChunkState.Failed; chunk.retirementFailed = false; }
        }

        private void Retire(Chunk c)
        {
            bool wasReady = c.state == WorldChunkState.Ready;
            c.state = WorldChunkState.Unloading;
            if (c.content == null) return;
            foreach (var surface in c.entry.surfaces) navigation.SetSurfaceReady(surface, false, false);
            if (!c.content.content.activeSelf)
            {
                ChunkVisibilityChanged?.Invoke(c.content.gameObject.scene, false);
                return;
            }
            _readyTerrains[Array.IndexOf(_chunks, c)] = null;
            c.content.terrain.SetNeighbors(null, null, null, null);
            RefreshBoundaries();
            try
            {
                if (wasReady)
                    foreach (var identity in c.content.identities) CaptureState?.Invoke(identity);
                ChunkVisibilityChanged?.Invoke(c.content.gameObject.scene, false);
            }
            finally
            {
                c.content.content.SetActive(false);
                c.proxy.SetActive(true);
            }
        }

        private static void StartUnload(Chunk chunk, float now)
        {
            if (!chunk.scene.IsValid())
            {
                chunk.state = chunk.retirementFailed ? WorldChunkState.Failed : WorldChunkState.Unloaded;
                chunk.content = null;
                chunk.retirementFailed = false;
                return;
            }
            try { chunk.unload = Addressables.UnloadSceneAsync(chunk.scene, autoReleaseHandle: false); }
            catch (Exception error)
            {
                chunk.failure = error.Message;
                chunk.retryUnloadAt = now + 1;
            }
        }

        private static void FinishUnload(Chunk chunk, float now)
        {
            bool succeeded = chunk.unload.Status == AsyncOperationStatus.Succeeded;
            string error = chunk.unload.OperationException?.Message;
            Addressables.Release(chunk.unload);
            chunk.unload = default;
            if (!succeeded && chunk.scene.IsValid() && chunk.scene.Result.Scene.isLoaded)
            {
                chunk.failure = error ?? "청크 해제 실패";
                chunk.retryUnloadAt = now + 1;
                return;
            }
            chunk.scene = default;
            chunk.content = null;
            chunk.state = chunk.retirementFailed ? WorldChunkState.Failed :
                chunk.desired ? WorldChunkState.Requested : WorldChunkState.Unloaded;
            chunk.retirementFailed = false;
        }

        public IEnumerator CloseWorld()
        {
            _closing = true;
            _entry.Clear();
            _pins.Clear();
            TrackPlayer(null);
            while (OwnedSceneCount != 0 || (_chunks != null && _chunks.Any(c => c.unload.IsValid()))) yield return null;
            if (_chunks != null) foreach (var chunk in _chunks) Destroy(chunk.proxy);
            _chunks = null;
            _readyTerrains = null;
            _catalog = null;
            if (navigation != null) navigation.ClearStreaming();
            ReleaseCatalog();
        }

        public void CaptureReadyState()
        {
            if (_chunks == null) return;
            foreach (var c in _chunks)
                if (c.state == WorldChunkState.Ready && c.content != null)
                    foreach (var identity in c.content.identities)
                        if (identity != null) CaptureState?.Invoke(identity);
        }

        private void OnDestroy()
        {
            _closing = true;
            ReleaseCatalog();
            if (_chunks == null) return;
            foreach (var c in _chunks)
            {
                if (c.state == WorldChunkState.Ready && c.content != null)
                    foreach (var identity in c.content.identities) if (identity != null) CaptureState?.Invoke(identity);
                if (c.unload.IsValid())
                {
                    if (c.unload.IsDone) ReleaseUnload(c.unload);
                    else c.unload.Completed += ReleaseUnload;
                }
                else if (c.scene.IsValid())
                {
                    if (c.scene.IsDone) CloseScene(c.scene);
                    else c.scene.Completed += CloseScene;
                }
            }

            static void CloseScene(AsyncOperationHandle<SceneInstance> handle)
            {
                if (!handle.IsValid()) return;
                if (handle.Status != AsyncOperationStatus.Succeeded) Addressables.Release(handle);
                else if (handle.Result.Scene.isLoaded) Addressables.UnloadSceneAsync(handle);
            }

            static void ReleaseUnload(AsyncOperationHandle<SceneInstance> handle)
            {
                if (handle.IsValid()) Addressables.Release(handle);
            }
        }

        private void ReleaseCatalog()
        {
            if (_catalogLoad.IsValid()) Addressables.Release(_catalogLoad);
            _catalogLoad = default;
        }
    }
}
