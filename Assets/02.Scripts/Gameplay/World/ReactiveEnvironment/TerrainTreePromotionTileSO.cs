using System;
using UnityEngine;

namespace UPlayGround.Gameplay.World.ReactiveEnvironment
{
    /// <summary>원본 인스턴스와 영속 ID 및 승격 표현을 연결한다.</summary>
    public sealed class TerrainTreePromotionTileSO : ScriptableObject
    {
        [Serializable]
        public struct Species
        {
            public GameObject source, standing, stump;
            public ReactiveEnvironmentObject actor;
            public Vector3 trunkCenter;
            public float trunkRadius, trunkHeight;
        }

        [Serializable]
        public struct Placement
        {
            public string id;
            public Vector3 position;
            public int prototype;
            public GameObject sourcePrototype;
        }

        public TerrainData source;
        public Species[] species;
        public Placement[] placements;
    }
}
