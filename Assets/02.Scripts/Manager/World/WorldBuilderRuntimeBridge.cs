using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using KinematicCharacterController;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UPlayGround.Data.UI;
using UPlayGround.FlowGraph;
using UPlayGround.UI;
using UPlayGround.WorldBuilder.Runtime;

namespace UPlayGround.Manager.World
{
    /// <summary>발행 월드를 게임의 씬 진입·이동·저장·지도 경계에 연결한다.</summary>
    public sealed class WorldBuilderRuntimeBridge : MonoBehaviour
    {
        [SerializeField] private WorldChunkStreamer streamer;
        [SerializeField] private string playerActorId;
        [SerializeField] private Vector3 spawnPosition;

        private string _map;
        private PlayerActor _player;
        private WorldStateManager _worldState;
        private uint _stateEpoch;
        private readonly HashSet<string> _authoredActive = new(StringComparer.Ordinal);
        private readonly HashSet<Scene> _scenes = new();
        private readonly Dictionary<Scene, ChunkActivity> _activity = new();
        private readonly Dictionary<int, IDisposable> _traversalPins = new();
        private readonly HashSet<int> _occupiedTiles = new();
        private readonly List<int> _expiredPins = new();
        private float _nextPinRefresh;
        private bool _hasSafePose;
        private Vector3 _safePose;
        private Quaternion _safeRotation;

        public static WorldBuilderRuntimeBridge Active { get; private set; }
        public bool IsPrepared { get; private set; }
        public string EntryFailure { get; private set; }
        public IReadOnlyList<string> UnmatchedSavedIds { get; private set; } = Array.Empty<string>();
        public bool ContainsDestination(string scene) => _map == scene || gameObject.scene.name == scene;

        private sealed class ChunkActivity
        {
            public Vector3 center;
            public NavMeshAgent[] agents;
            public FlowGraphRunner[] flows;
            public IDisposable lease;
        }

        private void Awake()
        {
            Active = this;
            streamer.ApplyState = ApplyState;
            streamer.CaptureState = CaptureState;
            streamer.ChunkVisibilityChanged += ChangeOwnership;
            streamer.ChunkPreparing += OnChunkPreparing;
        }

        public IEnumerator PrepareEntry(SceneContext context)
        {
            _map = context.MapID;
            _worldState = WorldStateManager.Instance;
            _stateEpoch = _worldState.StateEpoch;
            yield return streamer.WaitForCatalog(error => EntryFailure = error);
            if (EntryFailure != null) yield break;
            if (streamer.Catalog == null || string.IsNullOrWhiteSpace(playerActorId))
            {
                EntryFailure = "월드를 불러올 수 없습니다.";
                yield break;
            }

            Vector3 destination = spawnPosition;
            Quaternion rotation = Quaternion.identity;
            if (PartyManager.Instance.TryGetPendingSceneLocation(out var map, out var saved) && map == _map)
                destination = saved;
            if (SceneManager.PendingArrivalPosition is Vector3 explicitPosition) destination = explicitPosition;
            if (!string.IsNullOrEmpty(SceneManager.PendingArrivalId))
            {
                if (!TryResolveAnchor(SceneManager.PendingArrivalId, out destination))
                {
                    EntryFailure = "도착 지점을 찾을 수 없습니다.";
                    yield break;
                }
            }

            yield return streamer.PrepareDestination(destination, error => EntryFailure = error);
            if (EntryFailure != null) yield break;
            if (!TryResolveSupport(destination, out var supported))
            {
                EntryFailure = "도착 위치의 지면을 확인할 수 없습니다.";
                yield break;
            }

            _player = ActorSpawnManager.Instance.SpawnActor(playerActorId, supported, rotation, null, transform) as PlayerActor;
            if (_player == null)
            {
                EntryFailure = "플레이어를 준비할 수 없습니다.";
                yield break;
            }
            _player.gameObject.SetActive(true);
            RegisterMap(context.WorldMapConfig);
            TrackPlayer(_player, supported, rotation);
            IsPrepared = true;
        }

        public UniTask<string> TravelAsync(string scene, string arrivalId, Vector3? requested, CancellationToken token) =>
            TravelAsync(scene, arrivalId, requested, token, Quaternion.identity);

        public async UniTask<string> TravelAsync(string scene, string arrivalId, Vector3? requested, CancellationToken token, Quaternion rotation)
        {
            if (!IsPrepared || _player == null) throw new InvalidOperationException("월드가 준비되지 않았습니다.");
            if (!string.IsNullOrEmpty(arrivalId) && TryResolveAnchor(arrivalId, out var anchor)) requested = anchor;
            else if (!string.IsNullOrEmpty(arrivalId)) throw new InvalidOperationException("도착 지점을 찾을 수 없습니다.");
            if (!requested.HasValue) throw new InvalidOperationException("도착 지점을 찾을 수 없습니다.");

            string failure = null;
            var preparation = streamer.PrepareDestination(requested.Value, error => failure = error);
            try
            {
                while (preparation.MoveNext()) await UniTask.Yield(token);
            }
            finally { (preparation as IDisposable)?.Dispose(); }
            token.ThrowIfCancellationRequested();
            if (failure != null) throw new InvalidOperationException(failure);
            if (!TryResolveSupport(requested.Value, out var supported))
                throw new InvalidOperationException("도착 위치의 지면을 확인할 수 없습니다.");
            _player.TeleportTo(supported, rotation);
            TrackPlayer(_player, supported, rotation);
            return null;
        }

        public bool TryResolveAnchor(string id, out Vector3 position)
        {
            foreach (var anchor in streamer.Catalog?.pois ?? Array.Empty<UPlayGround.WorldBuilder.Data.WorldChunkPoi>())
            {
                if (anchor.id != id) continue;
                position = anchor.position;
                return true;
            }
            position = default;
            return false;
        }

        public bool TryResolveSupport(Vector3 position, out Vector3 supported)
        {
            supported = position;
            if (!streamer.CanTraverse(position, position) || !float.IsFinite(position.y)) return false;
            float originOffset = 2;
            foreach (var hit in Physics.RaycastAll(position + Vector3.up * originOffset, Vector3.down, originOffset + 6,
                         Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance))
            {
                if (!_scenes.Contains(hit.collider.gameObject.scene) || hit.normal.y < .5f) continue;
                supported = hit.point + Vector3.up * .05f;
                return true;
            }
            return false;
        }

        public void TrackPlayer(PlayerActor player, Vector3 position, Quaternion rotation)
        {
            _player = player;
            streamer.TrackPlayer(player.transform);
            _safePose = position;
            _safeRotation = rotation;
            _hasSafePose = true;
        }

        internal void ConstrainMotor(KinematicCharacterMotor motor)
        {
            if (!IsPrepared || _player == null || motor != _player.ActorController?.Motor || !_hasSafePose) return;
            if (streamer.CanTraverse(_safePose, motor.TransientPosition))
            {
                _safePose = motor.TransientPosition;
                _safeRotation = motor.TransientRotation;
                return;
            }
            motor.BaseVelocity = Vector3.zero;
            motor.SetPositionAndRotation(_safePose, _safeRotation);
        }

        public void CaptureReadyState() => streamer.CaptureReadyState();

        private void RegisterMap(MinimapIconConfigSO map)
        {
            var catalog = streamer.Catalog;
            if (map != null)
            {
                map.captureCenter = new Vector2(catalog.worldBounds.center.x, catalog.worldBounds.center.z);
                map.captureWorldSizeXY = new Vector2(catalog.worldBounds.size.x, catalog.worldBounds.size.z);
            }
            foreach (var poi in catalog.pois)
            {
                if (MinimapMarkerRegistry.TryGet(poi.id, out _)) continue;
                var marker = new GameObject(poi.id);
                marker.transform.SetParent(transform);
                marker.transform.position = poi.position;
                MinimapMarkerRegistrar.Install(marker, poi.id, MinimapMarkerType.Custom).SetWorldMarkerVisible(false);
            }

            var known = catalog.chunks.SelectMany(c => c.instanceIds).ToHashSet(StringComparer.Ordinal);
            var consumed = _worldState.GetConsumedInteractables(_map);
            UnmatchedSavedIds = consumed?.Where(id => !known.Contains(id)).OrderBy(id => id, StringComparer.Ordinal).ToArray()
                ?? Array.Empty<string>();
            if (UnmatchedSavedIds.Count != 0)
                Debug.LogWarning($"[WorldBuilder] 저장 상태 매칭 보고 · {_map}/{catalog.bake.buildId} · " + string.Join(", ", UnmatchedSavedIds));
        }

        private void ApplyState(WorldBakedIdentity instance)
        {
            if (instance.TryGetComponent<Components.SceneEntityId>(out var entity)) entity.RuntimeSetGuid(instance.id);
            if (instance.gameObject.activeSelf) _authoredActive.Add(instance.id);
            if (_worldState.IsInteractableConsumed(_map, instance.id))
                instance.gameObject.SetActive(false);
        }

        private void CaptureState(WorldBakedIdentity instance)
        {
            if (_worldState != null && _worldState.StateEpoch == _stateEpoch &&
                _authoredActive.Contains(instance.id) && !instance.gameObject.activeSelf)
                _worldState.RecordConsumedInteractable(_map, instance.id);
        }

        private void OnChunkPreparing(Scene scene)
        {
            _scenes.Add(scene);
            RegionOwnership.Register(scene, _map);
        }

        private void ChangeOwnership(Scene scene, bool ready)
        {
            if (ready)
            {
                MonsterRespawnManager.Instance.RegisterRegion(scene);
                InteractionRespawnManager.Instance.RegisterRegion(scene);
                Gameplay.Quest.NpcQuestMarkerInstaller.InstallAll(scene);
                var roots = scene.GetRootGameObjects();
                var marker = roots.Select(r => r.GetComponent<WorldChunkContent>()).Single(c => c != null);
                _activity[scene] = new ChunkActivity
                {
                    center = marker.terrain.transform.position + marker.terrain.terrainData.size * .5f,
                    agents = roots.SelectMany(r => r.GetComponentsInChildren<NavMeshAgent>(true)).ToArray(),
                    flows = roots.SelectMany(r => r.GetComponentsInChildren<FlowGraphRunner>(true)).ToArray()
                };
            }
            else
            {
                _scenes.Remove(scene);
                if (_activity.Remove(scene, out var activity)) activity.lease?.Dispose();
                MonsterRespawnManager.Instance.UnregisterRegion(scene);
                InteractionRespawnManager.Instance.UnregisterRegion(scene);
                RegionOwnership.Unregister(scene);
            }
        }

        private void Update()
        {
            if (!IsPrepared || streamer.IsClosing || Time.unscaledTime < _nextPinRefresh) return;
            _nextPinRefresh = Time.unscaledTime + .25f;
            _occupiedTiles.Clear();
            foreach (var activity in _activity.Values)
            {
                bool needed = false;
                foreach (var agent in activity.agents)
                {
                    if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh ||
                        !(agent.pathPending || agent.hasPath || agent.isOnOffMeshLink)) continue;
                    needed = true;
                    RecordTile(agent.nextPosition);
                    if (agent.isOnOffMeshLink) RecordTile(agent.currentOffMeshLinkData.endPos);
                }
                foreach (var flow in activity.flows)
                    if (flow != null && flow.isActiveAndEnabled && flow.HasRegionBlockingWork()) { needed = true; break; }
                if (needed) activity.lease ??= streamer.Pin(activity.center);
                else if (activity.lease != null) { activity.lease.Dispose(); activity.lease = null; }
            }
            foreach (int index in _occupiedTiles)
                if (!_traversalPins.ContainsKey(index))
                    _traversalPins.Add(index, streamer.Pin(streamer.Catalog.chunks[index].bounds.center));
            _expiredPins.Clear();
            foreach (int index in _traversalPins.Keys)
                if (!_occupiedTiles.Contains(index)) _expiredPins.Add(index);
            foreach (int index in _expiredPins)
            {
                _traversalPins[index].Dispose();
                _traversalPins.Remove(index);
            }

            void RecordTile(Vector3 position)
            {
                int index = streamer.FindChunk(position);
                if (index >= 0) _occupiedTiles.Add(index);
            }
        }

        private void OnDestroy()
        {
            foreach (var activity in _activity.Values) activity.lease?.Dispose();
            foreach (var lease in _traversalPins.Values) lease.Dispose();
            foreach (var scene in _scenes) RegionOwnership.Unregister(scene);
            if (Active == this) Active = null;
        }
    }
}


