using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Debugging
{
    public enum DebugGizmoShapeType
    {
        Line,
        WireSphere,
        WireCube,
        Label,
    }

    public struct DebugGizmoTextEntry
    {
        public UnityEngine.Object owner;
        public DebugGizmoCategory category;
        public Vector3 position;
        public string text;
    }

    public struct DebugGizmoShapeEntry
    {
        public UnityEngine.Object owner;
        public DebugGizmoCategory category;
        public DebugGizmoShapeType shapeType;
        public Vector3 a;
        public Vector3 b;
        public float radius;
        public Color color;
    }

    public sealed class DebugGizmoFrameSnapshot
    {
        public int frame;
        public float time;
        public readonly List<DebugGizmoTextEntry> texts = new();
        public readonly List<DebugGizmoShapeEntry> shapes = new();

        public void Reset(int frameValue, float timeValue)
        {
            frame = frameValue;
            time = timeValue;
            texts.Clear();
            shapes.Clear();
        }
    }
}
