using System;
using UnityEngine;
using UPlayGround.WorldBuilder.Core;

namespace UPlayGround.WorldBuilder.Data
{
    /// <summary>건물 공통 catalog·지지점·출입구·부지 정지 제한의 단일 원본.</summary>
    [CreateAssetMenu(fileName = "StructurePlanningPreset", menuName = "UPlayGround/WorldBuilder/Structure Preset")]
    public sealed class StructurePlanningPreset : ScriptableObject
    {
        public PlacementCatalog catalog;
        public string prefabGuid;
        public Vector3[] supportPoints, entranceSockets;
        public float plotMargin = 2, falloff = 6, maxAverageSlope = 8, maxSlope = 12, maxCutFill = 3, supportTolerance = .05f;
        public bool requiresAccess = true;
        public PlacementCatalog.Entry Entry
        {
            get
            {
                if (catalog != null) foreach (var entry in catalog.Entries) if (entry.prefabGuid == prefabGuid) return entry;
                throw new InvalidOperationException("건물 catalog GUID가 없습니다.");
            }
        }
        public StructurePlan ToCore()
        {
            var entry = Entry;
            return new StructurePlan { PrefabGuid = prefabGuid, HalfX = entry.halfExtents.x, HalfZ = entry.halfExtents.y, PivotHeight = entry.pivotHeight,
                PlotMargin = plotMargin, Falloff = falloff, MaxAverageSlope = maxAverageSlope, MaxSlope = maxSlope, MaxCutFill = maxCutFill, SupportTolerance = supportTolerance,
                SupportPoints = WorldPlan.Points(supportPoints), EntranceSockets = WorldPlan.Points(entranceSockets), RequiresAccess = requiresAccess };
        }
    }
}
