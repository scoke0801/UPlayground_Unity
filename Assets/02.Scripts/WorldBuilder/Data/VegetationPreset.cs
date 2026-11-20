using UnityEngine;

namespace UPlayGround.WorldBuilder.Data
{
    public enum VegetationPath { TerrainTree, Detail, Prefab }

    [CreateAssetMenu(menuName = "UPlayGround/WorldBuilder/식생")]
    public sealed class VegetationPreset : ScriptableObject
    {
        public string prototypeId;
        public BiomePreset biome;
        public VegetationPath path;
        public GameObject prefab;
        public PlacementRuleAsset placementRule;
        [Range(0, 1)] public float density = .5f;
        [Min(.1f)] public float spacing = 12, footprintRadius = 1;
        public Vector2 scale = new Vector2(.8f, 1.2f);
    }
}

