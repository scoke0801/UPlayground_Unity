using System;
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Data.ReactiveEnvironment
{
    /// <summary>식생 원본 GUID별 반응 정책과 원본 재질 대응을 저작한다.</summary>
    [CreateAssetMenu(menuName = "UPlayGround/Environment/식생 반응 대응표")]
    public sealed class VegetationReactionCatalogSO : ScriptableObject
    {
        [Serializable]
        public sealed class Source
        {
            public string meshGuid;
            public ReactiveEnvironmentDefinitionSO definition;
            public string reason;
        }

        [Serializable]
        public sealed class Surface
        {
            public Material original;
            public Material reactive;
        }

        [Serializable]
        public sealed class Placement
        {
            public string globalId;
            public ReactiveEnvironmentDefinitionSO definition;
            public string reason;
        }

        public string sceneGuid;
        public List<Source> sources = new();
        public List<Surface> surfaces = new();
        public List<Placement> placements = new();
        public ReactiveEnvironmentDefinitionSO protectedVegetation;
        public ReactiveEnvironmentDefinitionSO terrainDefinition;
        public GameObject[] detailPrefabs;
        [Min(1)] public int detailPixelsPerCell = 8;
        [Min(0.01f)] public float interactionHeight = 1;
    }
}
