using UnityEngine;

namespace UPlayGround.Data.Item
{
    /// <summary>바닥 드롭의 표시·수동 획득·안전 배치 기준.</summary>
    [CreateAssetMenu(menuName = "UPlayGround/아이템/Ground Drop Settings")]
    public sealed class GroundDropSettingsSO : ScriptableObject
    {
        public GameObject dropPrefab;
        public GameObject labelPrefab;
        [Min(0.1f)] public float pickupDistance = 4f;
        [Min(0.1f)] public float labelDistance = 15f;
        [Min(0.01f)] public float groundProbeHeight = 3f;
        [Min(0.01f)] public float groundProbeDepth = 12f;
        [Min(0.01f)] public float surfaceOffset = 0.08f;
        [Min(0.01f)] public float scatterRadius = 0.6f;
        [Min(0.01f)] public float labelHeight = 0.55f;
        [Min(0f)] public float labelFadeDuration = 0.12f;
        public string pickupFxKey = "LootArrival";
        [Min(0.05f)] public float pickupFxLifetime = 1.2f;
        public LayerMask groundMask;
        public LayerMask visibilityMask;
    }
}
