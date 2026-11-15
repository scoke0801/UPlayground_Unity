using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace UPlayGround.Gameplay.World.ReactiveEnvironment
{
    /// <summary>논리 상태를 바꾸지 않고 식생 표현 값만 한 렌더 버퍼로 전달한다.</summary>
    internal static class EnvironmentSurfaceStateBuffer
    {
        internal const string Keyword = "_ENVIRONMENT_REACTION_BUFFER";
        internal static readonly int IndexProperty = Shader.PropertyToID("_EnvironmentReactionIndex");
        private static readonly int BufferProperty = Shader.PropertyToID("_EnvironmentReactionStates");
        private const int MinimumCapacity = 64;
        private const int StateStride = 32;

        [StructLayout(LayoutKind.Sequential)]
        private struct State
        {
            public Vector4 surface, thermal;
            public State(Vector4 surface, Vector4 thermal, float coverage)
            { this.surface = surface; thermal.y = coverage; this.thermal = thermal; }
        }

        private static State[] _values = Array.Empty<State>();
        private static bool[] _occupied = Array.Empty<bool>();
        private static Stack<int> Free = new();
        private static ComputeBuffer _buffer;
        private static int _next, _active, _dirtyMin = int.MaxValue, _dirtyMax = -1;
        internal static uint Epoch { get; private set; }
        internal static bool IsSupported => SystemInfo.graphicsShaderLevel >= 45 && SystemInfo.supportsComputeShaders;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlay() => Reset();

        internal static int Register(Vector4 surface, Vector4 thermal, float coverage)
        {
            int index = Free.Count > 0 ? Free.Pop() : _next;
            EnsureCapacity(index + 1);
            if (index == _next) _next++;
            _occupied[index] = true;
            if (_active++ == 0)
            {
                Shader.EnableKeyword(Keyword);
                RenderPipelineManager.beginFrameRendering += BeforeFrame;
                Application.quitting += Reset;
            }
            Set(index, surface, thermal, coverage);
            return index;
        }

        internal static bool IsValid(int index, uint epoch) => epoch == Epoch
            && index >= 0 && index < _next && _occupied[index];

        internal static void Set(int index, Vector4 surface, Vector4 thermal, float coverage)
        {
            _values[index] = new State(surface, thermal, coverage);
            _dirtyMin = Mathf.Min(_dirtyMin, index); _dirtyMax = Mathf.Max(_dirtyMax, index);
        }

        internal static void Release(int index, uint epoch)
        {
            if (!IsValid(index, epoch)) return;
            _occupied[index] = false;
            Free.Push(index);
            if (--_active == 0) Reset();
        }

        /// <summary>수정된 연속 구간만 렌더 시작 전에 업로드한다.</summary>
        internal static void Flush()
        {
            if (_buffer == null || _dirtyMax < _dirtyMin) return;
            _buffer.SetData(_values, _dirtyMin, _dirtyMin, _dirtyMax - _dirtyMin + 1);
            _dirtyMin = int.MaxValue; _dirtyMax = -1;
        }

        private static void BeforeFrame(ScriptableRenderContext context, Camera[] cameras) => Flush();

        private static void EnsureCapacity(int count)
        {
            if (_buffer != null && count <= _values.Length) return;
            int capacity = Mathf.NextPowerOfTwo(Mathf.Max(MinimumCapacity, count, _values.Length));
            Array.Resize(ref _values, capacity);
            Array.Resize(ref _occupied, capacity);
            var free = new Stack<int>(capacity);
            foreach (int index in Free) free.Push(index);
            Free = free;
            var buffer = new ComputeBuffer(capacity, StateStride, ComputeBufferType.Structured);
            try { if (_next > 0) buffer.SetData(_values, 0, 0, _next); }
            catch { buffer.Dispose(); throw; }
            _buffer?.Dispose(); _buffer = buffer;
            Shader.SetGlobalBuffer(BufferProperty, _buffer);
        }

        private static void Reset()
        {
            Shader.DisableKeyword(Keyword);
            RenderPipelineManager.beginFrameRendering -= BeforeFrame;
            Application.quitting -= Reset;
            _buffer?.Dispose(); _buffer = null;
            Array.Clear(_occupied, 0, _occupied.Length);
            Free.Clear(); _next = _active = 0;
            _dirtyMin = int.MaxValue; _dirtyMax = -1;
            Epoch++;
        }
    }
}
