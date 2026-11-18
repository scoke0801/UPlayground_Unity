using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.WorldBuilder.Core;

namespace UPlayGround.WorldBuilder.Data
{
    /// <summary>Region·POI·예약·도로·교량의 저작 원본을 파생 지형/씬과 분리한다.</summary>
    [CreateAssetMenu(fileName = "WorldPlan", menuName = "UPlayGround/WorldBuilder/World Plan")]
    public sealed class WorldPlan : ScriptableObject
    {
        public string worldGuid, planGuid;
        public List<RegionRecord> regions = new List<RegionRecord>();
        public List<PoiPlan> pois = new List<PoiPlan>();
        public List<PlanZone> reservations = new List<PlanZone>();
        public List<StructureRecord> structures = new List<StructureRecord>();
        public List<RoadRecord> roads = new List<RoadRecord>();
        public List<BridgeRecord> bridges = new List<BridgeRecord>();
        [SerializeField] private string _appliedHash;
        [SerializeField] private List<PlacementRecord> _appliedStructures = new List<PlacementRecord>();
        [SerializeField] private List<string> _appliedRoadGuids = new List<string>();
        [SerializeField] private string _appliedPlanGuid;
        public string AppliedHash => _appliedHash;
        public IReadOnlyList<PlacementRecord> AppliedStructures => _appliedStructures;
        public IReadOnlyList<string> AppliedRoadGuids => _appliedRoadGuids;
        public void InitializeForWorld(string world)
        {
            PlacementIdentity.ValidateGuid(world);
            if (string.IsNullOrEmpty(worldGuid) && string.IsNullOrEmpty(_appliedHash)) worldGuid = world;
            if (string.IsNullOrEmpty(planGuid) && string.IsNullOrEmpty(_appliedHash)) planGuid = System.Guid.NewGuid().ToString("N");
            if (worldGuid != world || (!string.IsNullOrEmpty(_appliedHash) && planGuid != _appliedPlanGuid)) throw new InvalidOperationException("PlanningIdentityConflict: 기존 월드/계획 GUID를 변경할 수 없습니다.");
        }
        public void SetApplied(WorldPlanningResult result)
        {
            if (result.HasErrors) throw new InvalidOperationException("계획 검증 오류가 있어 이전 결과를 유지합니다.");
            _appliedHash = result.ComputeHash(); _appliedStructures = new List<PlacementRecord>();
            foreach (var structure in result.Structures) _appliedStructures.Add(structure.Placement.Clone());
            _appliedPlanGuid = planGuid; _appliedRoadGuids = new List<string>(); foreach (var road in result.Roads) _appliedRoadGuids.Add(road.Guid);
        }
        [Serializable]
        public sealed class RegionRecord
        {
            public string regionGuid, label;
            public Vector2[] boundary;
            public PlanRegion ToCore()
            {
                if (boundary == null) throw new InvalidOperationException("Region 경계가 없습니다.");
                var points = new PlanPoint[boundary.Length]; for (int i = 0; i < points.Length; i++) points[i] = new PlanPoint(boundary[i].x, 0, boundary[i].y);
                return new PlanRegion { Guid = regionGuid, Boundary = points };
            }
        }
        [Serializable]
        public sealed class StructureRecord
        {
            public string structureGuid, poiGuid;
            public StructurePlanningPreset preset;
            public Vector3 localOffset;
            public float yaw;
            public StructurePlan ToCore()
            {
                if (preset == null) throw new InvalidOperationException("건물 프리셋이 없습니다.");
                var result = preset.ToCore(); result.Guid = structureGuid; result.PoiGuid = poiGuid; result.Offset = Point(localOffset); result.Yaw = yaw; return result;
            }
        }
        [Serializable]
        public sealed class RoadRecord
        {
            public string roadGuid, startStructureGuid, endStructureGuid, requiredBridgeGuid;
            public int startSocket, endSocket;
            public RoadPlanningPreset preset;
            public bool isLocked;
            public RoadKnot[] knots;
            public RoadPlan ToCore()
            {
                if (preset == null) throw new InvalidOperationException("도로 프리셋이 없습니다.");
                return new RoadPlan { Guid = roadGuid, StartStructureGuid = startStructureGuid, EndStructureGuid = endStructureGuid, StartSocket = startSocket, EndSocket = endSocket,
                    RequiredBridgeGuid = requiredBridgeGuid, Options = preset.ToCore(), IsLocked = isLocked, Knots = CopyKnots() };
            }
            private RoadKnot[] CopyKnots()
            {
                if (knots == null) return null;
                var result = new RoadKnot[knots.Length]; for (int i = 0; i < result.Length; i++) result[i] = knots[i]?.Clone(); return result;
            }
        }
        [Serializable]
        public sealed class BridgeRecord
        {
            public string bridgeGuid, waterPlanGuid;
            public BridgePlanningPreset preset;
            public Vector3 startSocket, endSocket;
            public bool hasConfirmedWaterLevel;
            public float plannedWaterLevel;
            public PlanZone waterZone;
            public BridgePlan ToCore()
            {
                if (preset == null) throw new InvalidOperationException("교량 프리셋이 없습니다.");
                var result = preset.ToCore(); result.Guid = bridgeGuid; result.WaterPlanGuid = waterPlanGuid; result.Start = Point(startSocket); result.End = Point(endSocket);
                result.HasConfirmedWaterLevel = hasConfirmedWaterLevel; result.PlannedWaterLevel = plannedWaterLevel; result.WaterZone = waterZone; return result;
            }
        }
        public static PlanPoint Point(Vector3 value) => new PlanPoint(value.x, value.y, value.z);
        public static PlanPoint[] Points(Vector3[] values)
        { if (values == null) return null; var result = new PlanPoint[values.Length]; for (int i = 0; i < result.Length; i++) result[i] = Point(values[i]); return result; }
    }
}
