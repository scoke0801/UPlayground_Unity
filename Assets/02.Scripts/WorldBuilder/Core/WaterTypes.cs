using System;
using System.Collections.Generic;
using System.IO;

namespace UPlayGround.WorldBuilder.Core
{
    public enum WaterBodyKind { River, Lake, Sea }

    /// <summary>수면 함수와 지형 단면은 공유하되 시각 메시·게임플레이 체적은 분리한다.</summary>
    [Serializable]
    public sealed class WaterBody
    {
        public string guid;
        public WaterBodyKind kind;
        public bool isRouteLocked;
        public PlanPoint[] route, boundary;
        public double level, width = 12, depth = 2, shoreWidth = 2, bankWidth = 3, bankClearance = .5, falloff = 6;
        public double flowSpeed = 1, maxCutFill = 12, uvRepeat = 8, queryHeight = 3;
        public int priority;
        public WaterBody Clone()
        {
            var copy = (WaterBody)MemberwiseClone();
            copy.route = route == null ? null : (PlanPoint[])route.Clone();
            copy.boundary = boundary == null ? null : (PlanPoint[])boundary.Clone(); return copy;
        }
    }

    [Serializable]
    public sealed class WaterExclusion
    {
        public string guid;
        public PlanPoint[] boundary;
        public double minY, maxY;
    }

    [Serializable]
    public sealed class WaterfallEdge
    {
        public string guid, upstreamGuid, downstreamGuid;
        public PlanPoint upperLip, lowerLanding;
        public double width = 8;
    }

    /// <summary>실패 위치와 원본 ID를 보존해 편집자가 문제 구간으로 이동할 수 있게 한다.</summary>
    public sealed class WaterConflict
    {
        public string Guid, Code, Message;
        public PlanPoint Position;
        public WaterConflict(string guid, string code, string message, PlanPoint position)
        { Guid = guid; Code = code; Message = message; Position = position; }
        public override string ToString() => Code + " " + Guid + " (" + Position.x + "," + Position.y + "," + Position.z + ") " + Message;
    }

    public struct WaterVertex
    {
        public PlanPoint Position, Flow;
        public double U, V;
        public static WaterVertex Lerp(WaterVertex a, WaterVertex b, double t) => new WaterVertex
        { Position = PlanPoint.Lerp(a.Position, b.Position, t), Flow = PlanPoint.Lerp(a.Flow, b.Flow, t), U = a.U + (b.U - a.U) * t, V = a.V + (b.V - a.V) * t };
    }
    public sealed class WaterChunk
    {
        public string BodyGuid;
        public int TileX, TileZ;
        public readonly List<WaterVertex> Vertices = new List<WaterVertex>();
        public readonly List<int> Triangles = new List<int>();
    }
    public sealed class WaterBuildResult
    {
        public readonly List<WaterConflict> Conflicts = new List<WaterConflict>();
        public readonly List<WaterBody> Bodies = new List<WaterBody>();
        public readonly List<GradePatch> Patches = new List<GradePatch>();
        public readonly List<WaterChunk> Chunks = new List<WaterChunk>();
        public TerrainBuildResult Terrain;
        public bool HasErrors => Conflicts.Count > 0;
        public void Reject(string guid, string code, string message, PlanPoint p)
        { if (!Conflicts.Exists(c => c.Guid == guid && c.Code == code)) Conflicts.Add(new WaterConflict(guid, code, message, p)); }
        public string ComputeHash()
        {
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream))
            {
                writer.Write(Terrain.ComputeHash());
                foreach (var body in Bodies) { writer.Write(body.guid); writer.Write(body.priority); writer.Write(body.flowSpeed); }
                foreach (var chunk in Chunks)
                {
                    writer.Write(chunk.BodyGuid); writer.Write(chunk.TileX); writer.Write(chunk.TileZ);
                    foreach (var v in chunk.Vertices)
                    { writer.Write(v.Position.x); writer.Write(v.Position.y); writer.Write(v.Position.z); writer.Write(v.U); writer.Write(v.V); writer.Write(v.Flow.x); writer.Write(v.Flow.z); }
                    foreach (int i in chunk.Triangles) writer.Write(i);
                }
                writer.Flush(); return PlacementIdentity.Hash(stream.ToArray());
            }
        }
    }
}
