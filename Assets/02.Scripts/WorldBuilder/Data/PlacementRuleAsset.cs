using UnityEngine;
using UPlayGround.WorldBuilder.Core;

namespace UPlayGround.WorldBuilder.Data
{
    /// <summary>수치·영역·리소스 식별자를 코드 수정 없이 저작한다.</summary>
    [CreateAssetMenu(fileName = "PlacementRule", menuName = "UPlayGround/WorldBuilder/Placement Rule")]
    public sealed class PlacementRuleAsset : ScriptableObject
    {
        public string layerGuid, ruleGuid, prefabGuid, ruleTag;
        public bool isEnabled = true, mandatory;
        public int seedSalt, slots = 1, minimumCount;
        public float cellSize = 16, jitter = 1, density = 1;
        public Rect region = new Rect(0, 0, 512, 512);
        public float minHeight = -1000, maxHeight = 10000, maxSlope = 45, spacing = 2, minScale = 1, maxScale = 1, maxSupportDelta = 2;
        public string surfaceId = "terrain";
        public PlacementRule ToCore() => new PlacementRule { LayerGuid = layerGuid, RuleGuid = ruleGuid, PrefabGuid = prefabGuid, Tag = ruleTag, Enabled = isEnabled, Mandatory = mandatory,
            SeedSalt = seedSalt, Slots = slots, MinimumCount = minimumCount, CellSize = cellSize, Jitter = jitter, Density = density,
            MinX = region.xMin, MinZ = region.yMin, MaxX = region.xMax, MaxZ = region.yMax, MinHeight = minHeight, MaxHeight = maxHeight, MaxSlope = maxSlope,
            Spacing = spacing, MinScale = minScale, MaxScale = maxScale, MaxSupportDelta = maxSupportDelta, SurfaceId = surfaceId };
    }
}
