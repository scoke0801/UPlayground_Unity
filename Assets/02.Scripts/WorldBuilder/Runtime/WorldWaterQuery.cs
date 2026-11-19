using UnityEngine;
using UPlayGround.Contracts;
using UPlayGround.WorldBuilder.Data;

namespace UPlayGround.WorldBuilder.Runtime
{
    /// <summary>물리 조회와 기존 수영 어댑터가 같은 우선순위·면적·안정 ID 규칙을 사용한다.</summary>
    [ExecuteAlways]
    public sealed class WorldWaterQuery : MonoBehaviour, IWaterQuery
    {
        #if UNITY_EDITOR
        public WorldDefinition definition;
        public WaterSystemDefinition source;
        public string appliedHash, hierarchyHash;
        #endif
        public WaterVolume[] volumes;
        public bool TrySample(Vector3 position, out WaterSample sample)
        {
            sample = default; bool found = false;
            if (volumes == null) return false;
            foreach (var volume in volumes)
            {
                if (volume == null || !volume.isActiveAndEnabled || !volume.TrySample(position, out var next)) continue;
                if (!found || WaterSample.Precedes(next, sample)) { sample = next; found = true; }
            }
            return found;
        }
        public bool TryRaycastDown(Vector3 origin, float distance, out WaterSample sample)
        {
            sample = default; bool found = false;
            if (volumes == null) return false;
            foreach (var volume in volumes)
            {
                if (volume == null || !volume.isActiveAndEnabled || !volume.TryRaycastDown(origin, distance, out var next)) continue;
                if (!found || WaterSample.Precedes(next, sample)) { sample = next; found = true; }
            }
            return found;
        }
    }
}
