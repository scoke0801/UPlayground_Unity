using UnityEngine;
using UPlayGround.Data.Enemy;

namespace UPlayGround.MovementController
{
    /// <summary>수평 전투 구역과 회피 실행 예산을 실제 이동 경로와 같은 식으로 판정한다.</summary>
    public static class EnemyCombatMovementPolicy
    {
        public static float PlanarDistance(Vector3 a, Vector3 b)
        {
            var delta = a - b;
            delta.y = 0f;
            return delta.magnitude;
        }

        public static bool ShouldReturn(Vector3 position, Vector3 anchor, bool hasTarget, Vector3 target,
            bool returning, EnemyCombatMovementSettings settings)
        {
            if (settings == null || settings.territoryRadius <= 0f) return false;
            float radius = settings.territoryRadius;
            float targetLimit = returning ? Mathf.Max(settings.returnArrivalDistance, radius - settings.boundaryMargin) : radius;
            if (hasTarget && PlanarDistance(target, anchor) > targetLimit) return true;
            return PlanarDistance(position, anchor) > (returning ? settings.returnArrivalDistance : radius);
        }

        public static Vector3 ConstrainVelocity(Vector3 position, Vector3 anchor, Vector3 velocity, float radius, float deltaTime)
        {
            if (radius <= 0f || deltaTime <= 0f) return velocity;
            var relative = position - anchor;
            relative.y = 0f;
            var planar = velocity;
            planar.y = 0f;
            // 외부 충격으로 이미 밖에 있으면 순간 보정하지 않고, 더 멀어지는 자발 이동만 막는다.
            float allowedRadius = Mathf.Max(radius, relative.magnitude);
            var predicted = relative + planar * deltaTime;
            if (predicted.sqrMagnitude <= allowedRadius * allowedRadius) return velocity;
            planar = (predicted.normalized * allowedRadius - relative) / deltaTime;
            return new Vector3(planar.x, velocity.y, planar.z);
        }
    }

    /// <summary>실제 수락된 회피만 횟수와 시간에 반영한다.</summary>
    public struct EnemyDodgeBudget
    {
        public int ConsecutiveCount { get; private set; }
        public float ReadyAt { get; private set; }
        public float RecoveryUntil { get; private set; }
        public float LastEndedAt { get; private set; }

        public void Reset()
        {
            ConsecutiveCount = 0;
            ReadyAt = RecoveryUntil = LastEndedAt = float.NegativeInfinity;
        }

        public bool CanStart(float now, EnemyCombatMovementSettings settings)
            => settings != null && now >= ReadyAt && now >= RecoveryUntil
                && (ConsecutiveCount < Mathf.Max(1, settings.maxConsecutiveDodges)
                    || now - LastEndedAt >= settings.dodgeChainResetDuration);

        public void RecordStarted(float now, EnemyCombatMovementSettings settings)
        {
            if (now - LastEndedAt >= settings.dodgeChainResetDuration) ConsecutiveCount = 0;
            ConsecutiveCount++;
            ReadyAt = now + Mathf.Max(0f, settings.dodgeCooldown);
        }

        public void RecordEnded(float now, EnemyCombatMovementSettings settings)
        {
            LastEndedAt = now;
            RecoveryUntil = now + Mathf.Max(0f, settings.dodgeRecoveryDuration);
        }
    }
}
