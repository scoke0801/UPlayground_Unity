using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UPlayGround.FlowGraph;

namespace UPlayGround.Manager.World
{
    public enum RegionLoadState { Unloaded, Loading, Preparing, Ready, Unloading, Failed }

    /// <summary>공동 지면 위 여러 지역의 준비 상태와 플레이어의 논리 지역을 조정한다.</summary>
    public sealed class ContinuousWorldController : MonoBehaviour
    {
        public string ResidentMapId;
        public ContinuousWorldRegion[] Regions = Array.Empty<ContinuousWorldRegion>();
        [Min(0)] public float BoundaryHysteresis = 2;
        [Min(1)] public float EntryWaitLimit = 30;
        public Vector3 SafeEntryPosition;
        public Rect SurfaceRecoveryBounds;
        public Terrain[] SurfaceRecoveryTerrains = Array.Empty<Terrain>();
        public static ContinuousWorldController Active { get; private set; }
        public bool IsInitialized { get; private set; }
        public string EntrySceneName { get; private set; }
        public bool IsTravelling { get; private set; }
        public bool CanSave => IsInitialized && !_isClosing && !IsTravelling
            && (_current == null || _current.State == RegionLoadState.Ready);
        private ContinuousWorldRegion _current;
        private ContinuousWorldRegion _travelDestination;
        private Scene _residentScene;
        private PlayerActor _player;
        private FlowGraphRunner[] _residentFlows;
        private bool _isClosing;
        private Vector3 _safePosition;
        private Quaternion _safeRotation;

        private void Awake()
        {
            Active = this;
            _residentScene = gameObject.scene;
            EntrySceneName = _residentScene.name;
            _safePosition = SafeEntryPosition;
            _safeRotation = Quaternion.identity;
            RegionOwnership.Register(_residentScene, ResidentMapId);
            var flows = new System.Collections.Generic.List<FlowGraphRunner>();
            foreach (var root in _residentScene.GetRootGameObjects()) flows.AddRange(root.GetComponentsInChildren<FlowGraphRunner>(true));
            _residentFlows = flows.ToArray();
            foreach (var runner in _residentFlows) runner.OriginSceneName = ResidentMapId;
            foreach (var region in Regions) region.Initialize(this);
        }

        /// <summary>저장 위치 복원에 필요한 지역들을 먼저 준비한다.</summary>
        public IEnumerator PrepareEntry()
        {
            foreach (var region in Regions) region.Request(true);
            float deadline = Time.realtimeSinceStartup + EntryWaitLimit;
            foreach (var region in Regions)
                while (region.IsWorking && Time.realtimeSinceStartup < deadline) yield return null;
            IsInitialized = true;
            StartCoroutine(Monitor());
        }

        private IEnumerator Monitor()
        {
            var interval = new WaitForSecondsRealtime(0.25f);
            while (!_isClosing)
            {
                if (_player == null && Services.TryGet<IActorQueryService>(out var actors)) _player = actors.Player as PlayerActor;
                if (_player != null)
                    foreach (var region in Regions) region.UpdateDemand(_player.transform.position);
                UpdateBarriers();
                yield return interval;
            }
        }

        internal bool IsCurrent(ContinuousWorldRegion region) => _current == region;
        internal bool IsTravellingTo(ContinuousWorldRegion region) => IsTravelling && _travelDestination == region;
        internal string GetPinReason(ContinuousWorldRegion region)
        {
            // 전투·대화는 이미 실체화한 지역을 유지한다. 멀리 있는 지역을 새로 불러올 이유는 아니다.
            if (region.Content == null) return null;
            if (Services.TryGet<IDialogueService>(out var dialogue) && dialogue.IsDialogueActive) return "Dialogue";
            if (_player != null && _player.GetCombat()?.IsInCombat == true) return "Combat";
            if (region.Content != null && region.Content.HasActiveFlow()) return "RegionFlow";
            foreach (var runner in _residentFlows)
                if (runner != null && runner.HasRegionBlockingWork()) return "ResidentFlow";
            return null;
        }

        private void FixedUpdate()
        {
            if (_player == null || _player.ActorController?.Motor == null || !IsInitialized) return;
            Vector3 position = _player.transform.position;
            bool dialogue = Services.TryGet<IDialogueService>(out var service) && service.IsDialogueActive;
            var next = FindRegion(position, _current);
            bool blocked = dialogue && next != _current;
            foreach (var region in Regions)
                blocked |= region.State != RegionLoadState.Ready && CrossesBounds(region.Bounds, _safePosition, position);
            if (blocked)
            {
                _player.ActorController.Motor.SetPositionAndRotation(_safePosition, _safeRotation);
                return;
            }
            _safePosition = position;
            _safeRotation = _player.transform.rotation;
            if (next != _current) SetLogicalRegion(next);
        }

        /// <summary>이탈 여유를 먼저 적용해 인접 경계에서 지역이 진동하지 않게 한다.</summary>
        public ContinuousWorldRegion FindRegion(Vector3 position, ContinuousWorldRegion current = null)
        {
            if (current != null && Contains(current.Bounds, position, BoundaryHysteresis)) return current;
            foreach (var region in Regions)
                if (Contains(region.Bounds, position, -BoundaryHysteresis)) return region;
            return null;
        }

        internal void UpdateBarriers()
        {
            bool dialogue = Services.TryGet<IDialogueService>(out var service) && service.IsDialogueActive;
            foreach (var region in Regions)
            {
                bool blocked = region.State != RegionLoadState.Ready || dialogue;
                if (region.EntryBarrier != null && region.EntryBarrier.activeSelf != blocked) region.EntryBarrier.SetActive(blocked);
            }
        }

        private void SetLogicalRegion(ContinuousWorldRegion region)
        {
            if (_current != region)
            {
                if (_current != null) _current.Content?.SuspendWaits();
                else foreach (var runner in _residentFlows) if (runner != null) runner.SuspendRegionWaits();
            }
            _current = region;
            SceneManager.Instance.SetLogicalRegion(region != null ? region.MapId : ResidentMapId);
            ResumeCurrentRegion();
        }

        /// <summary>현재 진행 사실로 지역 현장만 복원한다.</summary>
        public void ResumeCurrentRegion()
        {
            if (_current != null) _current.Content?.Resume();
            else foreach (var runner in _residentFlows)
                if (runner != null && runner.isActiveAndEnabled) runner.ResumeRegion();
        }

        /// <summary>월드에 편입한 원본 지역을 조회한다.</summary>
        public ContinuousWorldRegion GetRegion(string mapId)
        {
            foreach (var region in Regions)
                if (region.MapId == mapId || region.LegacySceneName == mapId) return region;
            return null;
        }

        /// <summary>기존 도착 ID를 같은 월드 안에서도 사용할 수 있게 한다.</summary>
        public bool ContainsDestination(string sceneName) => sceneName == ResidentMapId || sceneName == EntrySceneName || GetRegion(sceneName) != null;

        /// <summary>저장 위치가 속한 지역이 실패했으면 안전 지면으로 복귀한다.</summary>
        public void RestoreLocation(string mapId, PlayerActor player)
        {
            _player = player;
            var region = GetRegion(mapId);
            if (region != null && region.State != RegionLoadState.Ready)
            {
                player.ActorController.Motor.SetPosition(SafeEntryPosition);
                region = null;
            }
            else if (TryResolveSurfacePosition(player.transform.position, out var supportedPosition))
                player.ActorController.Motor.SetPosition(supportedPosition);
            _safePosition = player.transform.position;
            _safeRotation = player.transform.rotation;
            SetLogicalRegion(region);
        }

        /// <summary>재조형 구역의 공중·지중 저장 위치만 현재 지면으로 복구하고 유효한 구조물 위 위치는 보존한다.</summary>
        public bool TryResolveSurfacePosition(Vector3 savedPosition, out Vector3 position)
        {
            position = savedPosition;
            if (!SurfaceRecoveryBounds.Contains(new Vector2(savedPosition.x, savedPosition.z))) return false;
            // 부두·다리·건물 위 저장을 Terrain 높이로 끌어내리지 않는다.
            if (Physics.Raycast(savedPosition + Vector3.up * 0.2f, Vector3.down, out var support,
                0.6f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                && support.collider.GetComponentInParent<GameActor>() == null) return false;
            foreach (var terrain in SurfaceRecoveryTerrains)
            {
                if (terrain == null) continue;
                Vector3 local = savedPosition - terrain.transform.position;
                Vector3 size = terrain.terrainData.size;
                if (local.x < 0 || local.z < 0 || local.x > size.x || local.z > size.z) continue;
                float ground = terrain.SampleHeight(savedPosition) + terrain.transform.position.y;
                if (Mathf.Abs(savedPosition.y - ground) < 0.5f) return false;
                position.y = ground + 0.05f;
                return true;
            }
            return false;
        }

        /// <summary>목적지 준비 후 기존 플레이어를 이동하며 명시 좌표는 원본 지역 좌표로 해석한다.</summary>
        public async UniTask<string> TravelAsync(string sceneName, string arrivalId, Vector3? position, CancellationToken token)
        {
            if (IsTravelling || !IsInitialized) throw new InvalidOperationException("월드 진입 또는 이동이 진행 중입니다.");
            var region = GetRegion(sceneName);
            _travelDestination = region;
            IsTravelling = true;
            try
            {
                if (region != null)
                {
                    region.Request(true);
                    while (region.IsWorking) await UniTask.Yield(token);
                    if (region.State != RegionLoadState.Ready) throw new InvalidOperationException(region.Failure ?? "지역이 준비되지 않았습니다.");
                    if (position.HasValue) position = region.ToWorld(position.Value);
                }
                Scene scene = region != null ? region.LoadedScene : _residentScene;
                Quaternion rotation = Quaternion.Euler(0, region != null ? region.SourceYaw : 0, 0);
                string activationId = null;
                if (!string.IsNullOrEmpty(arrivalId))
                {
                    if (!SceneArrivalRegistry.TryGet(arrivalId, scene, out var arrival)) throw new InvalidOperationException("지역 도착점이 없습니다: " + arrivalId);
                    position = arrival.Position;
                    rotation = arrival.Rotation;
                    activationId = arrival.ActivationId;
                }
                token.ThrowIfCancellationRequested();
                if (!position.HasValue || _player == null) throw new InvalidOperationException("도착 위치 또는 플레이어가 없습니다.");
                _player.TeleportTo(position.Value, rotation);
                _safePosition = position.Value;
                _safeRotation = rotation;
                SetLogicalRegion(region);
                return activationId;
            }
            finally { IsTravelling = false; _travelDestination = null; }
        }

        private void OnDestroy()
        {
            _isClosing = true;
            foreach (var region in Regions) region.Close();
            RegionOwnership.Unregister(_residentScene);
            if (Active == this) Active = null;
        }

        /// <summary>경계 설정과 동일한 XZ 포함 판정을 검증에서도 사용한다.</summary>
        public static bool Contains(Rect bounds, Vector3 position, float margin) =>
            position.x >= bounds.xMin - margin && position.x <= bounds.xMax + margin
            && position.z >= bounds.yMin - margin && position.z <= bounds.yMax + margin;

        /// <summary>한 물리 프레임에 경계 전체를 건너는 대시도 미준비 지역에 진입하지 못하게 한다.</summary>
        public static bool CrossesBounds(Rect bounds, Vector3 from, Vector3 to)
        {
            var volume = new Bounds(new Vector3(bounds.center.x, 0, bounds.center.y),
                new Vector3(bounds.width, 1, bounds.height));
            from.y = to.y = 0;
            if (volume.Contains(from) || volume.Contains(to)) return true;
            Vector3 delta = to - from;
            return delta.sqrMagnitude > 0 && volume.IntersectRay(new Ray(from, delta.normalized), out float distance)
                && distance <= delta.magnitude;
        }

        /// <summary>고도와 무관하게 공중 접근도 선행 로드한다.</summary>
        public static float DistanceToBounds(Rect bounds, Vector3 position)
        {
            float x = Mathf.Max(bounds.xMin - position.x, 0, position.x - bounds.xMax);
            float z = Mathf.Max(bounds.yMin - position.z, 0, position.z - bounds.yMax);
            return Mathf.Sqrt(x*x + z*z);
        }


    }
}
