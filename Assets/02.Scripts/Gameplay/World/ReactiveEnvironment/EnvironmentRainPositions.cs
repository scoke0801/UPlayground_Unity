using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

namespace UPlayGround.Gameplay.World.ReactiveEnvironment
{
    /// <summary>등록된 강수 앵커의 현재 위치를 읽기 전용 작업으로 수집한다. 등록/해제만 버퍼를 확장한다.</summary>
    internal sealed class EnvironmentRainPositions : IDisposable
    {
        internal sealed class Registration
        {
            internal readonly EnvironmentRainPositions owner;
            internal readonly int[] slots;
            internal bool live = true;
            internal Registration(EnvironmentRainPositions owner, int count)
            { this.owner = owner; slots = new int[count]; }
        }

        private readonly HashSet<Registration> _registrations = new();
        private readonly Dictionary<Transform, int> _lookup = new();
        private readonly Stack<int> _free = new();
        private TransformAccessArray _access;
        private NativeArray<int> _nativeSlots;
        private NativeArray<Vector4> _nativePositions;
        private int[] _slots = Array.Empty<int>(), _indices = Array.Empty<int>(), _references = Array.Empty<int>();
        private Transform[] _sources = Array.Empty<Transform>();
        private Vector4[] _positions = Array.Empty<Vector4>();
        private int _next;
        private uint _positionEpoch;

        [BurstCompile]
        private struct ReadPositions : IJobParallelForTransform
        {
            [ReadOnly] public NativeArray<int> slots;
            // 각 고유 Transform은 서로 다른 안정 슬롯에만 쓴다.
            [WriteOnly, NativeDisableParallelForRestriction] public NativeArray<Vector4> positions;
            public void Execute(int index, TransformAccess transform)
            {
                if (!transform.isValid) return;
                Vector3 point = transform.position;
                positions[slots[index]] = new Vector4(point.x, point.y, point.z, 1);
            }
        }

        // 무효 Transform은 작업 실행 자체가 생략될 수 있으므로 이전 tick의 유효 표식을 먼저 비운다.
        [BurstCompile]
        private struct ClearPositions : IJobParallelFor
        {
            [WriteOnly] public NativeArray<Vector4> positions;
            public void Execute(int index) { positions[index] = default; }
        }

        internal Registration Register(Transform[] sources)
        {
            if (sources == null || sources.Length == 0) return null;
            EnsureCapacity(_next + Mathf.Max(0, sources.Length - _free.Count));
            var registration = new Registration(this, sources.Length);
            for (int i = 0; i < sources.Length; i++)
            {
                registration.slots[i] = -1;
                var source = sources[i];
                if (source == null) continue;
                if (!_lookup.TryGetValue(source, out int slot))
                {
                    slot = _free.Count > 0 ? _free.Pop() : _next++;
                    int index = _access.length;
                    _access.Add(source);
                    _slots[index] = slot; _nativeSlots[index] = slot; _indices[slot] = index;
                    _sources[slot] = source; _lookup.Add(source, slot);
                }
                _references[slot]++;
                registration.slots[i] = slot;
            }
            _registrations.Add(registration); _positionEpoch = 0;
            return registration;
        }

        internal void Release(Registration registration)
        {
            if (registration == null || !ReferenceEquals(registration.owner, this) || !registration.live) return;
            registration.live = false; _registrations.Remove(registration);
            foreach (int slot in registration.slots)
            {
                if (slot < 0 || --_references[slot] > 0) continue;
                int index = _indices[slot], last = _access.length - 1;
                int moved = _slots[last];
                _access.RemoveAtSwapBack(index);
                if (index != last)
                { _slots[index] = moved; _nativeSlots[index] = moved; _indices[moved] = index; }
                _lookup.Remove(_sources[slot]); _sources[slot] = null;
                _indices[slot] = -1; _free.Push(slot);
            }
            _positionEpoch = 0;
        }

        internal bool TryRead(Registration registration, int index, uint epoch, out Vector3 position)
        {
            position = default;
            if (registration == null || !registration.live || !ReferenceEquals(registration.owner, this)
                || index < 0 || index >= registration.slots.Length) return false;
            int slot = registration.slots[index];
            if (slot < 0 || ReferenceEquals(_sources[slot], null)) return false;
            if (_positionEpoch != epoch)
            {
                var pending = new ClearPositions { positions = _nativePositions }.Schedule(_nativePositions.Length, 128);
                try
                {
                    pending = new ReadPositions { slots = _nativeSlots, positions = _nativePositions }
                        .ScheduleReadOnly(_access, 64, pending);
                }
                finally { pending.Complete(); }
                _nativePositions.CopyTo(_positions);
                _positionEpoch = epoch;
            }
            if (_positions[slot].w == 0) return false;
            position = _positions[slot];
            return true;
        }

        private void EnsureCapacity(int count)
        {
            if (_positions.Length >= count && _access.isCreated) return;
            int capacity = Mathf.NextPowerOfTwo(Mathf.Max(64, count));
            var slots = _slots; var indices = _indices; var references = _references;
            var sources = _sources; var positions = _positions;
            Array.Resize(ref slots, capacity); Array.Resize(ref indices, capacity);
            Array.Resize(ref references, capacity); Array.Resize(ref sources, capacity); Array.Resize(ref positions, capacity);
            var nativeSlots = new NativeArray<int>(capacity, Allocator.Persistent);
            NativeArray<Vector4> nativePositions = default;
            try
            {
                nativePositions = new NativeArray<Vector4>(capacity, Allocator.Persistent);
                if (_nativeSlots.IsCreated) NativeArray<int>.Copy(_nativeSlots, nativeSlots, _access.length);
                if (_access.isCreated) _access.capacity = capacity;
                else _access = new TransformAccessArray(capacity);
            }
            catch
            {
                nativeSlots.Dispose(); if (nativePositions.IsCreated) nativePositions.Dispose(); throw;
            }
            if (_nativeSlots.IsCreated) _nativeSlots.Dispose();
            if (_nativePositions.IsCreated) _nativePositions.Dispose();
            _nativeSlots = nativeSlots; _nativePositions = nativePositions;
            _slots = slots; _indices = indices; _references = references; _sources = sources; _positions = positions;
            _positionEpoch = 0;
        }

        public void Dispose()
        {
            foreach (var registration in _registrations) registration.live = false;
            _registrations.Clear(); _lookup.Clear(); _free.Clear();
            if (_access.isCreated) _access.Dispose();
            if (_nativeSlots.IsCreated) _nativeSlots.Dispose();
            if (_nativePositions.IsCreated) _nativePositions.Dispose();
            _access = default; _nativeSlots = default; _nativePositions = default;
            _slots = _indices = _references = Array.Empty<int>();
            _sources = Array.Empty<Transform>(); _positions = Array.Empty<Vector4>();
            _next = 0; _positionEpoch = 0;
        }
    }
}
