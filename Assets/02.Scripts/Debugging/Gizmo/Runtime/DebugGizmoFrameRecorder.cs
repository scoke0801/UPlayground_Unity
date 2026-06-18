using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Debugging
{
    public sealed class DebugGizmoFrameRecorder
    {
        private readonly List<DebugGizmoFrameSnapshot> _frames = new();
        private int _writeIndex;

        public IReadOnlyList<DebugGizmoFrameSnapshot> Frames => _frames;
        public bool IsRecording { get; private set; }

        public void SetRecording(bool value)
        {
            IsRecording = value;
        }

        public void Clear()
        {
            _frames.Clear();
            _writeIndex = 0;
        }

        public DebugGizmoFrameSnapshot BeginFrame(float recordSeconds)
        {
            int capacity = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(1f, recordSeconds) * Mathf.Max(1, Application.targetFrameRate > 0 ? Application.targetFrameRate : 60)));

            DebugGizmoFrameSnapshot snapshot;
            if (_frames.Count < capacity)
            {
                snapshot = new DebugGizmoFrameSnapshot();
                _frames.Add(snapshot);
                _writeIndex = _frames.Count % capacity;
            }
            else
            {
                snapshot = _frames[_writeIndex];
                _writeIndex = (_writeIndex + 1) % _frames.Count;
            }

            snapshot.Reset(Time.frameCount, Time.time);
            return snapshot;
        }
    }
}
