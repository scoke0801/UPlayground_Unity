using System;
using UnityEngine;

namespace UPlayGround.WorldBuilder.Data
{
    /// <summary>필수 이동 지점과 surface 사이의 명시 링크를 저작한다.</summary>
    [CreateAssetMenu(menuName = "UPlayGround/WorldBuilder/Navigation Layout")]
    public sealed class WorldNavigationLayout : ScriptableObject
    {
        public string worldGuid;
        public GameObject playerPrefab;
        public int agentTypeId;
        [Min(.1f)] public float endpointTolerance = 2;
        [Min(.1f)] public float sourceHalo = 3;
        public Crossing[] crossings = Array.Empty<Crossing>();
        public RequiredRoute[] routes = Array.Empty<RequiredRoute>();
    }

    [Serializable]
    public sealed class Crossing
    {
        public string id, portalId, bridgeId;
        public string fromSurface, toSurface;
        public Vector3 from, to;
        [Min(0)] public float width;
    }

    [Serializable]
    public sealed class RequiredRoute
    {
        public string id, fromSurface, toSurface, requiredCrossingId;
        public Vector3 from, to;
    }
}

