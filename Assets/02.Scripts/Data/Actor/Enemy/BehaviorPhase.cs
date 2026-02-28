using System;
using UnityEngine;

namespace UPlayGround.Data.Enemy
{
    /// <summary>
    /// HP 구간별 행동 프로파일.
    /// EnemyBehaviorSO에 배열로 포함된다.
    /// </summary>
    [Serializable]
    public class BehaviorPhase
    {
        public string phaseName = "Phase";

        [Tooltip("이 HP 비율 이하가 되면 전환")]
        [Range(0f, 1f)] public float hpThreshold = 0.5f;

        [Header("공격 후 행동 확률")]
        [Range(0f, 1f)] public float continueAttackChance = 0.3f;
        [Range(0f, 1f)] public float guardChance          = 0.25f;
        [Range(0f, 1f)] public float retreatChance        = 0.2f;
        [Range(0f, 1f)] public float chargeChance         = 0.0f;
        [Range(0f, 1f)] public float flankChance          = 0.0f;

        [Header("이동")]
        public float chaseSpeedMultiplier = 1.2f;
        public bool  allowCharge          = false;
        public bool  allowFlank           = false;

        [Header("연속 공격 한계")]
        public int maxConsecutiveAttacks = 3;
    }
}
