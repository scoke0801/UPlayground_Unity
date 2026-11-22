using UnityEngine;

namespace UPlayGround.Data.Rendering
{
    /// <summary>국소 안개 비용과 조작 가시성의 공통 설정이다.</summary>
    [CreateAssetMenu(menuName = "UPlayGround/Rendering/Toon Fog Quality")]
    public sealed class ToonFogQualitySettings : ScriptableObject
    {
        public enum Tier { Low, Medium, High }
        public Tier quality = Tier.Medium;
        public Texture2D noise;
        [Min(0)] public float localFadeSeconds = 0.2f;
        [Min(0)] public float teleportDistance = 50;
        [Min(0)] public float combatNearClearDistance = 12;
        [Range(0, 1)] public float combatDensityMultiplier = 0.8f;
        [Range(0, 1)] public float combatMaxOpacity = 0.85f;
        [Min(0)] public float combatEnterSeconds = 0.35f;
        [Min(0)] public float combatExitSeconds = 1;
        [Min(0)] public float rainDensityMultiplier = 1.25f;
        [Min(0)] public float weatherWindMetersPerSecond = 0.8f;
    }
}
