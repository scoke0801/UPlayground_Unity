using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround.CameraSystem
{
    /// <summary>무리 수와 퍼짐으로 연속 거리를 계산하고 작은 경계 변화를 억제한다.</summary>
    public static class EncounterCameraComposition
    {
        public static float CalculateCrowdDistance(int count, float radius, float detectionRadius,
            int maximumCount, Vector2 range)
        {
            float population = Mathf.Clamp01((count - 1f) / Mathf.Max(1f, maximumCount - 1f));
            float spread = Mathf.Clamp01(radius / Mathf.Max(0.01f, detectionRadius));
            return Mathf.Lerp(range.x, range.y, Mathf.Max(population, spread));
        }

        public static float ApplyDeadZone(float previous, float desired, float deadZone) =>
            Mathf.Abs(desired - previous) <= Mathf.Max(0f, deadZone) ? previous : desired;

        public static float ResolveLockDistance(float fixedDistance, float crowdDistance, bool isSweep) =>
            isSweep ? Mathf.Max(fixedDistance, crowdDistance) : fixedDistance;
    }
}
