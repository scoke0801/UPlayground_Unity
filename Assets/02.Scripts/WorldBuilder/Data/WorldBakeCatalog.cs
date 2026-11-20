using System;
using UnityEngine;
using UnityEngine.AI;

namespace UPlayGround.WorldBuilder.Data
{
    /// <summary>저작 그래프와 독립된 발행 revision의 런타임 참조 목록.</summary>
    public sealed class WorldBakeCatalog : ScriptableObject
    {
        public string worldGuid, buildId, scenePath, sceneGuid, geometryHash;
        public int agentTypeId;
        public float agentRadius, agentHeight, agentStep, agentSlope;
        public float playerRadius, playerHeight, playerStep, playerSlope;
        public BakedSurface[] surfaces;
        public BakedCrossing[] crossings;
        public BakedRoute[] routes;
        public BakedAnchor[] anchors;
        public string[] placementIds;
        public EcologyTreeRecord[] trees;
    }

    [Serializable]
    public struct BakedSurface
    {
        public string id;
        public string geometryHash;
        public Bounds bounds;
        public NavMeshData data;
    }

    [Serializable]
    public struct BakedCrossing
    {
        public string id, fromSurface, toSurface, portalId, bridgeId;
        public Vector3 from, to;
        public float width;
    }

    [Serializable]
    public struct BakedRoute
    {
        public string id, fromSurface, toSurface, requiredCrossingId;
        public Vector3 from, to;
    }

    [Serializable]
    public struct BakedAnchor
    {
        public string id;
        public Vector3 position;
        public bool required;
    }

    /// <summary>발행 포인터의 런타임 형식. 에셋 조회 정책은 소비자가 소유한다.</summary>
    [Serializable]
    public sealed class WorldPublishedPointer
    {
        public string worldGuid, current, previous, catalogGuid;
        public string previousCatalogGuid;
    }
}

