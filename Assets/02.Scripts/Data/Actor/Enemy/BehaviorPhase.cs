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

        [Header("Intent 가중치")]
        [Min(0f)] public float attackWeight       = 1f;
        [Min(0f)] public float punishWeight       = 1f;
        [Min(0f)] public float counterWeight      = 1f;
        [Min(0f)] public float pressureWeight     = 1f;
        [Min(0f)] public float chaseWeight        = 1f;
        [Min(0f)] public float retreatWeight      = 1f;
        [Min(0f)] public float keepDistanceWeight = 1f;
        [Min(0f)] public float defendWeight       = 1f;
        [Min(0f)] public float recoverWeight      = 1f;

        [Header("공중 행동 오버라이드 (AerialBehaviorSO 값을 덮어씀)")]
        [Tooltip("true = 아래 공중 수치를 이 페이즈에서 오버라이드")]
        public bool overrideAerial = false;
        [Range(0f, 1f)] public float aerialTakeOffChance      = 0.4f;
        public int                   aerialMaxAttackCount     = 3;
        [Range(0f, 1f)] public float aerialHpThreshold        = 1f;
        public float                 aerialDuration           = 12f;
    }
}
