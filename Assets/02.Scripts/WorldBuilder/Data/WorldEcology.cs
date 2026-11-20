using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.WorldBuilder.Core;

namespace UPlayGround.WorldBuilder.Data
{
    [CreateAssetMenu(menuName = "UPlayGround/WorldBuilder/생태·동굴")]
    public sealed class WorldEcology : ScriptableObject
    {
        public string worldGuid, ecologyGuid;
        public GameObject playerPrefab;
        public List<CaveModulePreset> caveCatalog = new List<CaveModulePreset>();
        public List<CaveLayout> caves = new List<CaveLayout>();
        public TerrainLayer defaultLayer;
        public List<BiomePreset> biomes = new List<BiomePreset>();
        public List<VegetationPreset> vegetation = new List<VegetationPreset>();
        [Range(0, 1)] public float humidity = .5f;
        [Min(0)] public float biomeBlendDistance;
        public int seed, alphaResolution = 64, detailResolution = 64;
        [Min(0)] public float treeDistance = 300, detailDistance = 100;
    }

    [Serializable]
    public sealed class CaveLayout
    {
        public string caveGuid;
        public List<CaveModuleLayout> modules = new List<CaveModuleLayout>();
        public List<CaveConnection> connections = new List<CaveConnection>();
        public List<CaveFloorPlacement> props = new List<CaveFloorPlacement>();
    }

    [Serializable]
    public sealed class CaveModuleLayout
    {
        public string moduleId, surfaceId;
        public CaveModulePreset preset;
        public Vector3 position;
        public float yaw;
    }

    [Serializable]
    public sealed class CaveConnection
    {
        public string moduleA, socketA, moduleB, socketB;
    }

    [Serializable]
    public sealed class CaveFloorPlacement
    {
        public string placementId, surfaceId;
        public GameObject prefab;
        public Vector2 worldXZ;
        public float yaw, heightOffset;
    }

    [Serializable]
    public sealed class CaveSocket
    {
        public string id, profile;
        public Vector3 floor;
        public float yaw, width = 3, height = 2.5f;
        public bool exterior;
    }

    [Serializable]
    public sealed class EcologyTreeRecord
    {
        public string id, sourceIdentity, prototypeId;
        public Vector3 worldPosition;
    }

    [Serializable]
    public sealed class CaveDryVolume
    {
        public string caveGuid, moduleId;
        public WaterExclusion extent;
        public Vector3 start, end;
        public float clearHeight;

        public bool Contains(Vector3 point)
        {
            if (point.y < extent.minY || point.y > extent.maxY || !PlanningGeometry.Contains(extent.boundary, point.x, point.z)) return false;
            double t = 0;
            PlanningGeometry.DistanceToSegment(WorldPlan.Point(point), WorldPlan.Point(start), WorldPlan.Point(end), out t);
            float floor = Mathf.Lerp(start.y, end.y, (float)t);
            return point.y >= floor && point.y <= floor + clearHeight;
        }
    }
}

