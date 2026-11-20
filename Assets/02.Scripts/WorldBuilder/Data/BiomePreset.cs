using UnityEngine;

namespace UPlayGround.WorldBuilder.Data
{
    [CreateAssetMenu(menuName = "UPlayGround/WorldBuilder/바이옴")]
    public sealed class BiomePreset : ScriptableObject
    {
        public string biomeId;
        public TerrainLayer layer;
        public int priority;
        public Vector2[] mask;
        public Vector2 height = new Vector2(-1000, 1000), slope = new Vector2(0, 90);
        public Vector2 shoreDistance = new Vector2(0, 10000), humidity = new Vector2(0, 1);
    }
}

