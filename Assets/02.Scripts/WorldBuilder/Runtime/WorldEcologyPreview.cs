using UnityEngine;
using UPlayGround.WorldBuilder.Core;
using UPlayGround.WorldBuilder.Data;

namespace UPlayGround.WorldBuilder.Runtime
{
    public sealed class WorldEcologyPreview : MonoBehaviour
    {
        public WorldDefinition definition;
        public string hierarchyHash, terrainOutputHash;
        public EcologyTreeRecord[] trees;
        public CaveDryVolume[] dryVolumes;
        public WorldCaveModule[] modules;
        public WorldCavePortal[] portals;

        public bool TryGetFloor(string surfaceId, Vector2 xz, out Vector3 floor)
        {
            foreach (var module in modules)
                if (module != null && module.surfaceId == surfaceId) return module.TryGetFloor(xz, out floor);
            floor = default;
            return false;
        }
    }

    [System.Serializable]
    public sealed class WorldCavePortal
    {
        public string id, caveGuid, moduleId, surfaceId;
        public Vector3 position, outward;
        public float width, height;
    }
}

