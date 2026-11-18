using System;
using UnityEngine;
using UPlayGround.WorldBuilder.Core;

namespace UPlayGround.WorldBuilder.Data
{
    /// <summary>실제 교량 프리팹의 span·상판·교대 접근·계획 수위 여유고 계약.</summary>
    [CreateAssetMenu(fileName = "BridgePlanningPreset", menuName = "UPlayGround/WorldBuilder/Bridge Preset")]
    public sealed class BridgePlanningPreset : ScriptableObject
    {
        public PlacementCatalog catalog;
        public string prefabGuid;
        public float span, deckWidth = 6, deckThickness = .5f, clearance = 3, spanTolerance = .01f, maxApproachSlope = 12, approachLength = 16, maxApproachCutFill = 3, supportTolerance = .05f;
        public PlacementCatalog.Entry Entry
        {
            get { if (catalog != null) foreach (var entry in catalog.Entries) if (entry.prefabGuid == prefabGuid) return entry; throw new InvalidOperationException("교량 catalog GUID가 없습니다."); }
        }
        public BridgePlan ToCore()
        {
            var entry = Entry;
            PlacementIdentity.RequireFinite(span, deckWidth, entry.halfExtents.x, entry.halfExtents.y, entry.pivotHeight);
            if (Math.Abs(entry.halfExtents.y * 2 - span) > .001 || Math.Abs(entry.halfExtents.x * 2 - deckWidth) > .001 || Math.Abs(entry.pivotHeight) > .001)
                throw new InvalidOperationException("교량 Catalog 길이/폭은 프리셋과 같고 pivotHeight는0이어야 합니다. 로컬 +Z 길이·중앙 XZ pivot·상판 Y0 프리팹을 사용하세요.");
            return new BridgePlan { PrefabGuid = prefabGuid, PresetSpan = span, DeckWidth = deckWidth, DeckThickness = deckThickness, Clearance = clearance, SpanTolerance = spanTolerance,
                MaxApproachSlope = maxApproachSlope, ApproachLength = approachLength, MaxApproachCutFill = maxApproachCutFill, SupportTolerance = supportTolerance };
        }
    }
}
