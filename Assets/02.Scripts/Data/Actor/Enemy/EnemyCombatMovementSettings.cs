using System;
using UnityEngine;

namespace UPlayGround.Data.Enemy
{
    /// <summary>전투 구역과 회피 공략 시간을 행동 SO에서 함께 조정한다.</summary>
    [Serializable]
    public sealed class EnemyCombatMovementSettings
    {
        [Min(0f)] public float territoryRadius = 18f;
        [Min(0f)] public float boundaryMargin = 3f;
        [Min(0f)] public float returnArrivalDistance = 1.25f;
        [Min(0.01f)] public float returnRepathInterval = 0.5f;
        [Min(0f)] public float dodgeCooldown = 2.4f;
        [Min(1)] public int maxConsecutiveDodges = 1;
        [Min(0f)] public float dodgeChainResetDuration = 4f;
        [Min(0f)] public float dodgeRecoveryDuration = 0.65f;
        [Min(0f)] public float dodgeInvincibilityDuration = 0.18f;
        [Min(0.01f)] public float dodgeMovementDuration = 0.32f;
        [Min(0f)] public float dodgeSpeedRatio = 1.55f;
        [Min(0f)] public float dodgeThreatDistance = 4.5f;
        [Min(0f)] public float dodgeGroundProbeDepth = 1.5f;
        [Min(0f)] public float collisionClearance = 0.05f;
        public LayerMask movementObstacleMask = ~0;
    }
}
