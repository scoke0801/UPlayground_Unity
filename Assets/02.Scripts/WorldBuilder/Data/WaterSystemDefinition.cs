using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.WorldBuilder.Core;

namespace UPlayGround.WorldBuilder.Data
{
    /// <summary>소유권과 안정 ID를 보존하는 실제 수계 원본. P04 계획 참조는 자동 치환하지 않는다.</summary>
    [CreateAssetMenu(menuName = "UPlayGround/WorldBuilder/Water System")]
    public sealed class WaterSystemDefinition : ScriptableObject
    {
        public string systemGuid, worldGuid;
        public List<BodyRecord> bodies = new List<BodyRecord>();
        public List<WaterExclusion> exclusions = new List<WaterExclusion>();
        public List<WaterfallEdge> waterfalls = new List<WaterfallEdge>();
        public WaterfallPreset waterfallPreset;
        [SerializeField] private string _appliedHash, _appliedSourceHash, _appliedSystemGuid;
        public string AppliedHash => _appliedHash;
        public string AppliedSourceHash => _appliedSourceHash;
        public void ValidateIdentity(string world)
        {
            PlacementIdentity.ValidateGuid(systemGuid); PlacementIdentity.ValidateGuid(worldGuid);
            if (systemGuid == worldGuid || world != worldGuid || (!string.IsNullOrEmpty(_appliedSystemGuid) && _appliedSystemGuid != systemGuid))
                throw new InvalidOperationException("WaterIdentityConflict: 수계 소유 월드/시스템 ID가 다릅니다.");
        }
        public void SetApplied(WaterBuildResult result, string sourceHash)
        {
            if (result.HasErrors) throw new InvalidOperationException("수계 검증 오류가 있습니다.");
            _appliedSystemGuid = systemGuid; _appliedHash = result.ComputeHash(); _appliedSourceHash = sourceHash;
        }
        [Serializable]
        public sealed class BodyRecord
        {
            public string waterGuid;
            public WaterBodyKind kind;
            public bool isRouteLocked;
            public PlanPoint[] route, boundary;
            public double level;
            public int waterPriority;
            public WaterBodyPreset preset;
            public WaterBody ToCore()
            {
                if (preset == null) throw new InvalidOperationException("수계 프리셋이 없습니다: " + waterGuid);
                return new WaterBody { guid = waterGuid, kind = kind, isRouteLocked = isRouteLocked, route = route == null ? null : (PlanPoint[])route.Clone(),
                    boundary = boundary == null ? null : (PlanPoint[])boundary.Clone(), level = level, priority = waterPriority,
                    width = preset.width, depth = preset.requiredDepth, shoreWidth = preset.shoreWidth, bankWidth = preset.bankWidth, bankClearance = preset.bankClearance,
                    falloff = preset.shoreFalloff, flowSpeed = preset.flowSpeed, maxCutFill = preset.maxCutFill, uvRepeat = preset.uvRepeat, queryHeight = preset.queryHeight };
            }
        }
    }
}
