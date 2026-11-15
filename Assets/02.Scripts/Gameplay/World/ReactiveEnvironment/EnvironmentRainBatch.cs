using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using UPlayGround.Gameplay.World.LivingWorld;

namespace UPlayGround.Gameplay.World.ReactiveEnvironment
{
    /// <summary>강수 샘플이 상태를 바꾸지 않는 대상만 두 단계 조회에 참여한다.</summary>
    public interface IEnvironmentRainBatchTarget
    {
        int RainSampleCapacity { get; }
        void CollectRainSamples(EnvironmentRainBatch batch);
    }

    /// <summary>한 논리 tick의 지붕 쿼리를 같은 조건으로 묶고 조회가 끝나면 일반 물리 경로로 돌아간다.</summary>
    public sealed class EnvironmentRainBatch : IDisposable
    {
        private const int MaximumHits = 32;
        private const int MinimumCapacity = 64;
        private const int CommandsPerJob = 64;
        internal static EnvironmentRainBatch Current { get; private set; }
        private static uint _nextEpoch;
        internal uint Epoch { get; private set; }
        private readonly List<Request> _requests = new();
        private readonly EnvironmentRainPositions _positions = new();

        internal EnvironmentRainPositions.Registration RegisterPositions(Transform[] sources) => _positions.Register(sources);
        internal void ReleasePositions(EnvironmentRainPositions.Registration registration) => _positions.Release(registration);
        internal void ResetPositionRegistrations() => _positions.Dispose();
        internal bool TryReadPosition(EnvironmentRainPositions.Registration registration, int index, out Vector3 position)
        {
            position = default;
            return ReferenceEquals(Current, this) && !_ready && _positions.TryRead(registration, index, Epoch, out position);
        }
        private NativeArray<RaycastCommand> _commands;
        private NativeArray<RayParameters> _parameters;
        private RayParameters[] _parameterInput = Array.Empty<RayParameters>();
        private NativeArray<EnvironmentEffects.ShelterVolume> _shelters;
        private EnvironmentEffects.ShelterVolume[] _shelterInput = Array.Empty<EnvironmentEffects.ShelterVolume>();
        private NativeArray<byte> _covered;
        private int _shelterCount;
        private NativeArray<RaycastHit> _hits;
        private NativeArray<int> _hitIndices;
        private NativeArray<int> _hitCount;
        private readonly RaycastHit[] _ownHits = new RaycastHit[MaximumHits];
        private bool _ready;
        private bool _backfaces;
        private PhysicsScene _physicsScene;

        /// <summary>동일 tick에서만 해석할 수 있는 강수 결과 표식.</summary>
        public readonly struct Sample
        {
            internal readonly uint epoch;
            internal readonly int index;
            internal Sample(uint epoch, int index) { this.epoch = epoch; this.index = index; }
        }
        internal Sample ShelteredSample => new(Epoch, -1);
        internal Sample ExposedSample => new(Epoch, -2);

        private struct Request
        {
            public Transform receiver;
            public bool sheltered;
            public Vector3 position;
            public int layers;
            public float distance;
        }

        private struct RayParameters
        {
            public Vector3 position;
            public int layers;
            public float distance;
            public int shelterStart, shelterCount;
        }

        [BurstCompile(FloatMode = FloatMode.Strict)]
        private struct PrepareCommands : IJobParallelFor
        {
            [ReadOnly] public NativeArray<RayParameters> parameters;
            [ReadOnly] public NativeArray<EnvironmentEffects.ShelterVolume> shelters;
            [WriteOnly] public NativeArray<RaycastCommand> commands;
            [WriteOnly] public NativeArray<byte> covered;
            public PhysicsScene physicsScene;
            public bool backfaces;
            public void Execute(int index)
            {
                var input = parameters[index];
                bool sheltered = false;
                for (int i = input.shelterStart; i < input.shelterStart + input.shelterCount; i++)
                {
                    var volume = shelters[i];
                    var point = input.position;
                    if (point.x < volume.minimum.x || point.x > volume.maximum.x
                        || point.y < volume.minimum.y || point.y > volume.maximum.y
                        || point.z < volume.minimum.z || point.z > volume.maximum.z) continue;
                    Vector3 local = volume.worldToLocal.MultiplyPoint3x4(point) - volume.center;
                    if (Mathf.Abs(local.x) <= volume.half.x && Mathf.Abs(local.y) <= volume.half.y
                        && Mathf.Abs(local.z) <= volume.half.z)
                    { sheltered = true; break; }
                }
                covered[index] = sheltered ? (byte)1 : (byte)0;
                var query = new QueryParameters(input.layers, false, QueryTriggerInteraction.Ignore, backfaces);
                commands[index] = new RaycastCommand(physicsScene, input.position, Vector3.up, query,
                    sheltered ? 0 : input.distance);
            }
        }

        // 빈 hit는 Unity 오브젝트 해석이 필요 없다. Debug에서도 전체 네이티브 배열을 관리 코드로 순회하지 않는다.
        [BurstCompile]
        private struct CollectHitIndices : IJob
        {
            [ReadOnly] public NativeArray<RaycastHit> hits;
            [ReadOnly] public NativeArray<byte> covered;
            [WriteOnly] public NativeArray<int> indices;
            [WriteOnly] public NativeArray<int> count;
            public void Execute()
            {
                int written = 0;
                for (int i = 0; i < hits.Length; i++)
                    if (covered[i] != 0 || hits[i].colliderEntityId != EntityId.None) indices[written++] = i;
                count[0] = written;
            }
        }

        /// <summary>종료 시 강수 조회를 원래 동기 경로로 돌리는 범위.</summary>
        public struct Scope : IDisposable
        {
            private EnvironmentRainBatch _owner;
            internal Scope(EnvironmentRainBatch owner) => _owner = owner;
            public void Dispose()
            {
                if (_owner == null) return;
                if (ReferenceEquals(Current, _owner)) Current = null;
                _owner = null;
            }
        }

        /// <summary>배치 등록/로드 때 확보해 고정 tick의 관리·네이티브 할당을 피한다.</summary>
        public void Reserve(int samples)
        {
            if (samples <= 0 || _commands.IsCreated && samples <= _commands.Length) return;
            int capacity = Mathf.NextPowerOfTwo(Mathf.Max(MinimumCapacity, samples));
            _requests.Capacity = capacity;
            EnsureShelterCapacity(16);
            var input = new RayParameters[capacity];
            var commands = new NativeArray<RaycastCommand>(capacity, Allocator.Persistent);
            NativeArray<RaycastHit> hits = default;
            NativeArray<RayParameters> parameters = default;
            NativeArray<byte> covered = default;
            NativeArray<int> indices = default, hitCount = default;
            try
            {
                hits = new NativeArray<RaycastHit>(capacity, Allocator.Persistent);
                parameters = new NativeArray<RayParameters>(capacity, Allocator.Persistent);
                covered = new NativeArray<byte>(capacity, Allocator.Persistent);
                indices = new NativeArray<int>(capacity, Allocator.Persistent);
                hitCount = new NativeArray<int>(1, Allocator.Persistent);
                if (_commands.IsCreated) _commands.Dispose();
                if (_hits.IsCreated) _hits.Dispose();
                if (_parameters.IsCreated) _parameters.Dispose();
                if (_covered.IsCreated) _covered.Dispose();
                if (_hitIndices.IsCreated) _hitIndices.Dispose();
                if (_hitCount.IsCreated) _hitCount.Dispose();
                _commands = commands; _hits = hits;
                _parameters = parameters;
                _parameterInput = input; _covered = covered;
                _hitIndices = indices; _hitCount = hitCount;
            }
            catch
            {
                if (commands.IsCreated) commands.Dispose();
                if (hits.IsCreated) hits.Dispose();
                if (parameters.IsCreated) parameters.Dispose();
                if (covered.IsCreated) covered.Dispose();
                if (indices.IsCreated) indices.Dispose();
                if (hitCount.IsCreated) hitCount.Dispose();
                throw;
            }
        }

        private void EnsureShelterCapacity(int count)
        {
            if (_shelters.IsCreated && _shelters.Length >= count) return;
            int capacity = Mathf.NextPowerOfTwo(Mathf.Max(16, count));
            var shelters = new NativeArray<EnvironmentEffects.ShelterVolume>(capacity, Allocator.Persistent);
            try { Array.Resize(ref _shelterInput, capacity); }
            catch { shelters.Dispose(); throw; }
            if (_shelters.IsCreated) _shelters.Dispose();
            _shelters = shelters;
        }

        /// <summary>이번 논리 tick의 샘플 준비 범위를 시작한다.</summary>
        public Scope Begin()
        {
            if (Current != null) throw new InvalidOperationException("강수 조회 묶음은 중첩할 수 없습니다.");
            _requests.Clear(); _ready = false;
            _shelterCount = 0;
            _backfaces = Physics.queriesHitBackfaces;
            _physicsScene = Physics.defaultPhysicsScene;
            Epoch = ++_nextEpoch;
            if (Epoch == 0) Epoch = ++_nextEpoch;
            Current = this;
            return new Scope(this);
        }

        /// <summary>활성 샘플 위치를 한 번만 읽고 현재 묶음에 속한 결과 표식을 돌려준다.</summary>
        public Sample Collect(EnvironmentEffects effects, Vector3 position, Transform receiver = null)
        {
            if (!ReferenceEquals(Current, this) || _ready) throw new InvalidOperationException("강수 준비 단계가 아닙니다.");
            return !ReferenceEquals(effects, null) ? effects.CollectRainSample(this, position, receiver) : ExposedSample;
        }

        /// <summary>같은 스텝의 날씨 값을 공유해 개체마다 Unity 참조 생존을 조회하지 않는다.</summary>
        internal float GetRain(EnvironmentController weather)
        {
            return ReferenceEquals(weather, null) ? 0 : weather.ReadRainForBatch(Epoch);
        }

        internal Vector2Int RegisterShelterSnapshot(EnvironmentEffects.ShelterVolume[] volumes, int count)
        {
            EnsureShelterCapacity(_shelterCount + count);
            Array.Copy(volumes, 0, _shelterInput, _shelterCount, count);
            var range = new Vector2Int(_shelterCount, count);
            _shelterCount += count;
            return range;
        }

        internal bool TryEnqueue(EnvironmentEffects effects, Vector3 position, Transform receiver, LayerMask layers, float distance, out Sample sample)
        {
            sample = default;
            if (!_commands.IsCreated || _requests.Count == _commands.Length) return false;
            var range = effects.GetBatchShelterRange(this);
            int index = _requests.Count;
            _requests.Add(new Request { receiver = receiver, position = position, layers = layers.value, distance = distance });
            // 수집 중에는 관리 배열에 쓰고 한 번에 복사해 개별 NativeArray 쓰기 검사를 피한다.
            _parameterInput[index] = new RayParameters { position = position, layers = layers.value, distance = distance,
                shelterStart = range.x, shelterCount = range.y };
            sample = new Sample(Epoch, index);
            return true;
        }

        /// <summary>다른 tick의 표식이나 물리 완료 전의 조회를 거부한다.</summary>
        public bool IsSheltered(Sample sample)
        {
            if (!ReferenceEquals(Current, this) || sample.epoch != Epoch || !_ready)
                throw new InvalidOperationException("완료된 현재 강수 묶음의 샘플이 아닙니다.");
            if (sample.index < 0) return sample.index == -1;
            return _requests[sample.index].sheltered;
        }

        /// <summary>모든 지붕 쿼리를 완료하고 결과를 같은 tick의 논리 계산에 제공한다.</summary>
        public void Complete()
        {
            if (!ReferenceEquals(Current, this)) throw new InvalidOperationException("활성 강수 묶음이 없습니다.");
            int count = _requests.Count;
            if (count > 0)
            {
                if (Physics.autoSyncTransforms) Physics.SyncTransforms();
                NativeArray<RayParameters>.Copy(_parameterInput, 0, _parameters, 0, count);
                if (_shelterCount > 0)
                    NativeArray<EnvironmentEffects.ShelterVolume>.Copy(_shelterInput, 0, _shelters, 0, _shelterCount);
                var commands = _commands.GetSubArray(0, count);
                var hitSlice = _hits.GetSubArray(0, count);
                var pending = new PrepareCommands { parameters = _parameters.GetSubArray(0, count),
                    shelters = _shelters, covered = _covered, commands = commands,
                    physicsScene = _physicsScene, backfaces = _backfaces }
                    .Schedule(count, CommandsPerJob);
                try
                {
                    pending = RaycastCommand.ScheduleBatch(commands, hitSlice, CommandsPerJob, 1, pending);
                    pending = new CollectHitIndices { hits = hitSlice, covered = _covered, indices = _hitIndices, count = _hitCount }
                        .Schedule(pending);
                }
                finally { pending.Complete(); }
                int hitCount = _hitCount[0];
                for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
                {
                    int i = _hitIndices[hitIndex];
                    var request = _requests[i];
                    var first = _hits[i];
                    if (_covered[i] != 0) request.sheltered = true;
                    else if (first.colliderEntityId != EntityId.None)
                    {
                        request.sheltered = request.receiver == null || !first.transform.IsChildOf(request.receiver);
                        // 자기 Collider가 먼저 맞은 경우만 기존 다중 hit·포화 판정을 유지한다.
                        if (!request.sheltered)
                        {
                            int hits = Physics.RaycastNonAlloc(request.position, Vector3.up, _ownHits,
                                request.distance, request.layers, QueryTriggerInteraction.Ignore);
                            request.sheltered = hits == MaximumHits;
                            for (int hit = 0; hit < hits && !request.sheltered; hit++)
                                request.sheltered = !_ownHits[hit].transform.IsChildOf(request.receiver);
                        }
                    }
                    _requests[i] = request;
                }
            }
            _ready = true;
        }

        /// <summary>월드 종료 때 예약된 네이티브 버퍼를 해제한다.</summary>
        public void Dispose()
        {
            if (ReferenceEquals(Current, this)) Current = null;
            _positions.Dispose();
            if (_commands.IsCreated) _commands.Dispose();
            if (_hits.IsCreated) _hits.Dispose();
            if (_parameters.IsCreated) _parameters.Dispose();
            if (_covered.IsCreated) _covered.Dispose();
            if (_shelters.IsCreated) _shelters.Dispose();
            if (_hitIndices.IsCreated) _hitIndices.Dispose();
            if (_hitCount.IsCreated) _hitCount.Dispose();
            _commands = default; _hits = default;
            _parameters = default;
            _covered = default; _shelters = default;
            _parameterInput = Array.Empty<RayParameters>();
            _shelterInput = Array.Empty<EnvironmentEffects.ShelterVolume>();
            _hitIndices = default; _hitCount = default;
            _requests.Clear();
        }
    }
}
