using UnityEngine;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Enemy
{
    /// <summary>
    /// 적 스탯 데이터
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyStats", menuName = "UPlayGround/Enemy/Stats")]
    public class EnemyStatsSO : ScriptableObject
    {
        [Header("Health")]
        public float maxHealth = 100f;
        
        [Header("Movement")]
        public float walkSpeed = 2f;
        public float runSpeed = 4f;
        public float chaseSpeedMultiplier = 1.2f;
        
        [Header("Detection")]
        public float detectionRadius = 10f;
        public float lostTargetRadius = 15f;
        public float fieldOfView = 120f;
        
        [Header("Combat")]
        public float attackRange = 2.5f;
        public float attackCooldown = 1.5f;
        public MonsterActorGrade grade = MonsterActorGrade.Normal;
        
        [Header("Patrol")]
        public bool enablePatrol = true;
        public float patrolRadius = 5f;
        public float patrolWaitTime = 2f;
    }
}