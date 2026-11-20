using System;
using UnityEngine;

namespace UPlayGround.WorldBuilder.Data
{
    public sealed class WorldChunkCatalog : ScriptableObject
    {
        public WorldBakeCatalog bake;
        public string sourceRevision, bakeCatalogGuid;
        public WorldChunkPoi[] pois;
        public BakedCrossing[] boundaryCrossings;
        public Bounds worldBounds;
        public float tileSize;
        public WorldChunkEntry[] chunks;
        public WorldStreamSettings settings = new();
        public static string Address(string world, string revision) => "WorldBuilder/" + world + "/" + revision + "/Catalog";
    }

    [Serializable]
    public struct WorldChunkPoi
    {
        public string id, regionId, waterId;
        public Vector3 position;
        public bool locked;
    }

    [Serializable]
    public sealed class WorldStreamSettings
    {
        [Min(1)] public int readyRing = 1;
        [Min(1)] public int preloadRing = 2;
        [Min(0)] public float hysteresis = 64;
        [Min(0)] public float minimumResidency = 5;
        [Min(0)] public float lookAheadMargin = 24;
        [Min(1)] public int parallelLoads = 2;
        [Min(1)] public int parallelUnloads = 1;
        [Min(1)] public float travelTimeout = 30;
    }

    [Serializable]
    public sealed class WorldChunkEntry
    {
        public string id, sceneKey;
        public Vector2Int tile;
        public Bounds bounds;
        public int[] dependencies;
        public string[] surfaces, instanceIds;
        public Mesh proxy;
    }
}

