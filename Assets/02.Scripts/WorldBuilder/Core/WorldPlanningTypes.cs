using System;
using System.Collections.Generic;
using System.IO;

namespace UPlayGround.WorldBuilder.Core
{
    /// <summary>엔진 좌표 타입 없이 계획·곡선·메시 수치를 공유한다.</summary>
    [Serializable]
    public struct PlanPoint
    {
        public double x, y, z;
        public PlanPoint(double x, double y, double z) { this.x = x; this.y = y; this.z = z; }
        public static PlanPoint operator +(PlanPoint a, PlanPoint b) => new PlanPoint(a.x + b.x, a.y + b.y, a.z + b.z);
        public static PlanPoint operator -(PlanPoint a, PlanPoint b) => new PlanPoint(a.x - b.x, a.y - b.y, a.z - b.z);
        public static PlanPoint operator *(PlanPoint a, double value) => new PlanPoint(a.x * value, a.y * value, a.z * value);
        public double Length => Math.Sqrt(x * x + y * y + z * z);
        public PlanPoint Normalized => Length > 1e-12 ? this * (1 / Length) : new PlanPoint(0, 0, 1);
        public static PlanPoint Lerp(PlanPoint a, PlanPoint b, double value) => a + (b - a) * value;
        public static PlanPoint Cross(PlanPoint a, PlanPoint b) => new PlanPoint(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
    }
    public enum PlanningIssue { InvalidInput, OutsideRegion, UnconfirmedElevation, CutFillExceeded, SlopeExceeded, HoleOverlap, ProtectedArea, FootprintOverlap, Unsupported, MissingSocket, NoPath, CurveRejected, MissingWaterPlan, MissingBridgePreset, InvalidSpan, InsufficientClearance, BridgeApproachRejected, DisconnectedEntrance }
    public sealed class PlanningDiagnostic
    {
        public string elementGuid;
        public PlanningIssue issue;
        public string message;
        public PlanningDiagnostic(string guid, PlanningIssue issue, string message) { elementGuid = guid; this.issue = issue; this.message = message; }
    }
    /// <summary>지역 다각형은 계획 영역이며 바이옴 가중치로 취급하지 않는다.</summary>
    public sealed class PlanRegion
    {
        public string Guid;
        public PlanPoint[] Boundary;
    }
    /// <summary>통행·지형·자동 배치 용도를 분리한 예약 영역.</summary>
    [Serializable]
    public sealed class PlanZone
    {
        public string guid;
        public PlanPoint[] boundary;
        public bool blocksRoad = true, blocksTerrain = true, blocksPlacement = true;
    }
    [Serializable]
    public sealed class PoiPlan
    {
        public string guid, regionGuid;
        public PlanPoint anchor;
        public bool isLocked, hasConfirmedElevation;
        public string waterPlanGuid;
        public bool requiresWaterPlan, hasConfirmedWaterPlan;
        public double plannedFloodLevel, floodFreeboard;
    }
    public sealed class StructurePlan
    {
        public string Guid, PoiGuid, PrefabGuid;
        public PlanPoint Offset;
        public double Yaw, HalfX, HalfZ, PivotHeight, PlotMargin = 2, Falloff = 6, MaxAverageSlope = 8, MaxSlope = 12, MaxCutFill = 3, SupportTolerance = .05;
        public PlanPoint[] SupportPoints, EntranceSockets;
        public bool RequiresAccess = true;
    }
    public sealed class PlannedStructure
    {
        public PlacementRecord Placement;
        public string PoiGuid;
        public double FloorY;
        public PlanPoint[] Entrances;
        public bool IsBridge;
    }
    /// <summary>사용자가 확정한 횡단 위치·계획 수위를 입력한다. 수계 생성과 구분한다.</summary>
    public sealed class BridgePlan
    {
        public string Guid, PrefabGuid, WaterPlanGuid;
        public PlanPoint Start, End;
        public PlanZone WaterZone;
        public bool HasConfirmedWaterLevel;
        public double PlannedWaterLevel, Clearance = 3, DeckThickness = .5, DeckWidth = 6, PresetSpan, SpanTolerance = .01, MaxApproachSlope = 12, SupportTolerance = .05;
        public double ApproachLength = 16, MaxApproachCutFill = 3;
        public double MaxShoulderSlope;
    }
    [Serializable]
    public sealed class RoadKnot
    {
        public PlanPoint position, tangentIn, tangentOut;
        public RoadKnot Clone() => (RoadKnot)MemberwiseClone();
    }
    public sealed class RoadOptions
    {
        public double Width = 4, ExclusionWidth = 8, Falloff = 6, MaxSlope = 12, MaxCutFill = 3, SampleStep = 1, SearchStep = 2, SlopeCost = 4, EarthworkCost = 1, MaxCurvature = 1, UvRepeat = 4, SurfaceOffset = .025;
        public int MaxVisited = 250000;
    }
    public sealed class RoadPlan
    {
        public string Guid, StartStructureGuid, EndStructureGuid, RequiredBridgeGuid;
        public int StartSocket, EndSocket;
        public bool IsLocked;
        public RoadKnot[] Knots;
        public RoadOptions Options;
    }
    public struct RoadVertex
    {
        public PlanPoint Position, Normal, Tangent;
        public double U, V;
    }
    public sealed class RoadChunk
    {
        public string RoadGuid;
        public int TileX, TileZ;
        public RoadVertex[] Vertices;
        public int[] Triangles;
    }
    public sealed class PlannedRoad
    {
        public string Guid;
        public RoadKnot[] Knots;
        public PlanPoint[] Samples;
        public List<RoadChunk> Chunks = new List<RoadChunk>();
    }
    public sealed class GradePatch
    {
        public string ElementGuid, OperationGuid;
        public SampleRect Bounds;
        public float[] Values, Masks;
        public int Stage;
        public TerrainOperation ToCore() => new TerrainOperation(OperationGuid, TerrainOperationKind.SetAbsolute, Bounds, stage: Stage, automatic: true, values: Values, masks: Masks);
    }
    /// <summary>검증된 계획과 파생 패치를 결과로 돌려주며 입력 SO를 수정하지 않는다.</summary>
    public sealed class WorldPlanningResult
    {
        public readonly List<PlanningDiagnostic> Diagnostics = new List<PlanningDiagnostic>();
        public readonly List<GradePatch> Patches = new List<GradePatch>();
        public readonly List<PlannedStructure> Structures = new List<PlannedStructure>();
        public readonly List<PlannedRoad> Roads = new List<PlannedRoad>();
        public readonly List<PlacementReservation> Reservations = new List<PlacementReservation>();
        public TerrainBuildResult Terrain;
        public bool HasErrors => Diagnostics.Count != 0;
        public void Reject(string guid, PlanningIssue issue, string message)
        {
            if (!Diagnostics.Exists(d => d.elementGuid == guid && d.issue == issue && d.message == message)) Diagnostics.Add(new PlanningDiagnostic(guid, issue, message));
        }
        public string ComputeHash()
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(Terrain.ComputeHash());
                foreach (var patch in Patches) { writer.Write(patch.OperationGuid); writer.Write(patch.Stage); }
                foreach (var reservation in Reservations)
                {
                    writer.Write(reservation.guid); writer.Write(reservation.minX); writer.Write(reservation.minZ); writer.Write(reservation.maxX); writer.Write(reservation.maxZ);
                    writer.Write(reservation.boundary?.Length ?? 0); if (reservation.boundary != null) foreach (var p in reservation.boundary) Write(writer, p);
                }
                var placements = new PlacementBuildResult(); foreach (var structure in Structures) placements.Records.Add(structure.Placement);
                writer.Write(placements.ComputeHash());
                foreach (var road in Roads)
                {
                    writer.Write(road.Guid);
                    foreach (var chunk in road.Chunks)
                    {
                        writer.Write(chunk.TileX); writer.Write(chunk.TileZ);
                        foreach (var v in chunk.Vertices) { Write(writer, v.Position); Write(writer, v.Normal); Write(writer, v.Tangent); writer.Write(v.U); writer.Write(v.V); }
                        foreach (int triangle in chunk.Triangles) writer.Write(triangle);
                    }
                }
                writer.Flush(); return PlacementIdentity.Hash(stream.ToArray());
            }
        }
        private static void Write(BinaryWriter writer, PlanPoint p) { writer.Write(p.x); writer.Write(p.y); writer.Write(p.z); }
    }
    public static class PlanningIdentity
    {
        public static string Derive(string world, string plan, string element, string domain)
        {
            PlacementIdentity.ValidateGuid(world); PlacementIdentity.ValidateGuid(plan); PlacementIdentity.ValidateGuid(element);
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            { writer.Write(1); writer.Write(world); writer.Write(plan); writer.Write(element); writer.Write(domain); writer.Flush(); return PlacementIdentity.Hash(stream.ToArray()).Substring(0, 32); }
        }
    }
}


