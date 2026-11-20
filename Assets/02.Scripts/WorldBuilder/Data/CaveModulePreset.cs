using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.WorldBuilder.Data
{
    public enum CaveModuleKind { Entrance, Straight, Bend, Branch, Room, EndCap }

    [CreateAssetMenu(menuName = "UPlayGround/WorldBuilder/동굴 모듈")]
    public sealed class CaveModulePreset : ScriptableObject
    {
        public string catalogId;
        public CaveModuleKind kind;
        public GameObject prefab;
        public GameObject collarPrefab;
        public List<CaveSocket> sockets = new List<CaveSocket>();
        public float[] allowedYaw = { 0, 90, 180, 270 };
        public float positionTolerance = .05f, angleTolerance = 2, cover = .5f;
        public Vector2 footprintCenter, footprintSize = new Vector2(4, 8);
        public float floorAtMinZ, floorAtMaxZ, clearHeight = 2.5f, clearWidth = 3;
    }
}

