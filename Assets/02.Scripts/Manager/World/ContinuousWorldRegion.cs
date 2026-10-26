using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace UPlayGround.Manager.World
{
    /// <summary>한 지역의 로드 핸들과 준비·재요청·회수 수명을 소유한다.</summary>
    [Serializable]
    public sealed class ContinuousWorldRegion
    {
        public string MapId;
        public string SceneKey;
        public string LegacySceneName;
        public Rect Bounds;
        public Vector3 SourceOffset;
        public float SourceYaw;
        public Vector3 SafeEntryPosition;
        public Vector3 NeighborEntryPosition;
        public Collider[] Grounds;
        public GameObject EntryBarrier;
        [Min(0)] public float PreloadDistance = 45;
        [Min(0)] public float UnloadDistance = 65;
        [Min(0)] public float UnloadDelay = 8;
        [Min(0.1f)] public float RetryDelay = 5;
        [Min(0)] public float DiagnosticDelay;
        public bool DiagnosticFailNextLoad;
        public RegionLoadState State { get; private set; }
        public string Failure { get; private set; }
        public string PinReason { get; private set; }
        public uint Generation { get; private set; }
        public int LoadCount { get; private set; }
        public int UnloadCount { get; private set; }
        public bool HasHandle => _handle.IsValid();
        public bool IsWorking => _working;
        public Scene LoadedScene => State == RegionLoadState.Ready ? _handle.Result.Scene : default;
        public StreamedRegionContent Content => _content;
        private ContinuousWorldController _owner;
        private AsyncOperationHandle<SceneInstance> _handle;
        private StreamedRegionContent _content;
        private bool _desired, _working, _isClosing;
        private float _outsideSince = -1, _retryAt;

        internal void Initialize(ContinuousWorldController owner) => _owner = owner;

        internal void UpdateDemand(Vector3 position)
        {
            float distance = ContinuousWorldController.DistanceToBounds(Bounds, position);
            PinReason = _owner.GetPinReason(this);
            if (distance <= PreloadDistance || _owner.IsCurrent(this) || _owner.IsTravellingTo(this) || PinReason != null)
            {
                _outsideSince = -1;
                Request(true);
                return;
            }
            if (distance <= UnloadDistance) { _outsideSince = -1; return; }
            if (_outsideSince < 0) _outsideSince = Time.realtimeSinceStartup;
            if (Time.realtimeSinceStartup - _outsideSince >= UnloadDelay) Request(false);
        }

        /// <summary>원본 지역 좌표를 생성 월드 좌표로 변환한다.</summary>
        public Vector3 ToWorld(Vector3 point) => SourceOffset + Quaternion.Euler(0, SourceYaw, 0) * point;
        /// <summary>저장과 지도에 사용할 원본 지역 좌표를 복원한다.</summary>
        public Vector3 ToSource(Vector3 point) => Quaternion.Euler(0, -SourceYaw, 0) * (point - SourceOffset);

        /// <summary>중복·반전 요청은 희망 상태만 바꾸며 엔진 작업은 끝까지 회수한다.</summary>
        public void Request(bool required)
        {
            if (!required && !_isClosing && (_owner.IsCurrent(this) || _owner.IsTravellingTo(this) || _owner.GetPinReason(this) != null)) return;
            required &= !_isClosing;
            if (_desired != required) { _desired = required; Generation++; }
            if (_working || (State == RegionLoadState.Failed && Time.realtimeSinceStartup < _retryAt)) return;
            if ((_desired && State != RegionLoadState.Ready) || (!_desired && HasHandle))
                ReconcileAsync().Forget();
        }

        private async UniTask ReconcileAsync()
        {
            _working = true;
            try
            {
                while (true)
                {
                    if (!_desired || _isClosing)
                    {
                        if (HasHandle) await UnloadAsync();
                        if (!_desired || _isClosing) break;
                    }
                    if (State == RegionLoadState.Ready) break;
                    if (HasHandle) await UnloadAsync();
                    Failure = null;
                    State = RegionLoadState.Loading;
                    if (DiagnosticDelay > 0) await UniTask.Delay(TimeSpan.FromSeconds(DiagnosticDelay), ignoreTimeScale:true);
                    if (DiagnosticFailNextLoad)
                    {
                        DiagnosticFailNextLoad = false;
                        throw new InvalidOperationException("진단용 지역 로딩 실패");
                    }
                    _handle = Addressables.LoadSceneAsync(SceneKey, LoadSceneMode.Additive, activateOnLoad:true);
                    while (!_handle.IsDone) await UniTask.Yield();
                    if (_handle.Status != AsyncOperationStatus.Succeeded)
                        throw _handle.OperationException ?? new InvalidOperationException("지역 씬 로딩 실패");
                    LoadCount++;
                    if (!_desired || _isClosing) continue;
                    State = RegionLoadState.Preparing;
                    var contents = GetSceneComponents<StreamedRegionContent>(_handle.Result.Scene);
                    if (contents.Length != 1 || contents[0].MapId != MapId)
                        throw new InvalidOperationException("지역 소유권 또는 콘텐츠 루트가 잘못되었습니다.");
                    _content = contents[0];
                    await _content.Prepare().ToUniTask();
                    if (!_desired || _isClosing) continue;
                    ValidateConnection();
                    State = RegionLoadState.Ready;
                    _owner.UpdateBarriers();
                    LightProbes.TetrahedralizeAsync();
                    break;
                }
            }
            catch (Exception exception)
            {
                Failure = exception.Message;
                try { if (HasHandle) await UnloadAsync(); }
                catch (Exception cleanup) { Failure += " / 해제: " + cleanup.Message; }
                State = RegionLoadState.Failed;
                _retryAt = Time.realtimeSinceStartup + RetryDelay;
                if (!_isClosing) Debug.LogWarning("[ContinuousWorld] " + Failure);
            }
            finally { _working = false; }
        }

        private async UniTask UnloadAsync()
        {
            State = RegionLoadState.Unloading;
            if (!_isClosing) _owner.UpdateBarriers();
            if (_handle.Status == AsyncOperationStatus.Succeeded && _handle.Result.Scene.isLoaded)
            {
                Scene scene = _handle.Result.Scene;
                if (!_isClosing)
                {
                    MonsterRespawnManager.Instance.UnregisterRegion(scene);
                    InteractionRespawnManager.Instance.UnregisterRegion(scene);
                }
                if (_content != null) _content.ContentRoot.SetActive(false);
                var operation = Addressables.UnloadSceneAsync(_handle, autoReleaseHandle:false);
                while (!operation.IsDone) await UniTask.Yield();
                bool success = operation.Status == AsyncOperationStatus.Succeeded;
                var error = operation.OperationException;
                Addressables.Release(operation);
                if (!success) throw error ?? new InvalidOperationException("지역 언로드 실패");
                UnloadCount++;
                RegionOwnership.Unregister(scene);
            }
            else if (_handle.IsValid()) Addressables.Release(_handle);
            _handle = default;
            _content = null;
            State = RegionLoadState.Unloaded;
            if (!_isClosing) LightProbes.TetrahedralizeAsync();
        }

        private void ValidateConnection()
        {
            if (Grounds == null || Grounds.Length == 0)
                throw new InvalidOperationException("상주 지면 Collider가 준비되지 않았습니다.");
            foreach (Vector3 point in new[] {SafeEntryPosition, NeighborEntryPosition})
                if (!HasGround(point))
                    throw new InvalidOperationException("연결부 도착점 아래에 상주 지면이 없습니다.");
            var route = new UnityEngine.AI.NavMeshPath();
            if (!UnityEngine.AI.NavMesh.SamplePosition(SafeEntryPosition, out var start, 3, UnityEngine.AI.NavMesh.AllAreas)
                || !UnityEngine.AI.NavMesh.SamplePosition(NeighborEntryPosition, out var end, 3, UnityEngine.AI.NavMesh.AllAreas)
                || !UnityEngine.AI.NavMesh.CalculatePath(start.position, end.position, UnityEngine.AI.NavMesh.AllAreas, route)
                || route.status != UnityEngine.AI.NavMeshPathStatus.PathComplete)
                throw new InvalidOperationException("연결부의 공동 NavMesh 경로가 준비되지 않았습니다.");
        }


        private bool HasGround(Vector3 point)
        {
            foreach (var ground in Grounds)
                if (ground != null && ground.enabled && ground.gameObject.activeInHierarchy
                    && ground.Raycast(new Ray(point + Vector3.up * 5, Vector3.down), out _, 15)) return true;
            return false;
        }

        internal void Close()
        {
            _isClosing = true;
            _desired = false;
            if (!_working && HasHandle) ReconcileAsync().Forget();
        }

        private static T[] GetSceneComponents<T>(Scene scene) where T : Component
        {
            var result = new List<T>();
            foreach (var root in scene.GetRootGameObjects()) result.AddRange(root.GetComponentsInChildren<T>(true));
            return result.ToArray();
        }
    }
}
