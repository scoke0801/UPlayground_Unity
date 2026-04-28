using UnityEngine;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Enemy
{
    /// <summary>
    /// 레거시 적 튜닝 데이터.
    /// 전투 공식에 들어가는 스탯은 ActorStatSO/ActorStatContainer를 기준으로 사용한다.
    /// 이 SO의 체력 값은 StatDataGeneratorWindow의 초기 마이그레이션 입력으로만 사용한다.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyStats", menuName = "UPlayGround/Enemy/Stats")]
    public class EnemyStatsSO : ScriptableObject
    {
        [Header("Health")]
        [Tooltip("레거시 체력. 런타임 폴백으로 쓰지 않고 ActorStatSO 생성 시 MaxHealth 초기값으로만 사용한다.")]
        public float maxHealth = 100f;
        
        [Header("Movement")]
        [Tooltip("레거시 걷기 속도 튜닝. 런타임 스탯 배율은 ActorStatSO.MoveSpeed를 사용한다.")]
        public float walkSpeed = 2f;
        public float runSpeed = 4f;
        public float chaseSpeedMultiplier = 1.2f;
        
        [Header("Detection")]
        public float detectionRadius = 10f;
        public float lostTargetRadius = 15f;
        public float fieldOfView = 120f;
        
        [Header("Combat")]
        [Tooltip("레거시 공격 거리. 실제 스킬 사거리는 EnemyAttackDataSO/EnemyAttackInfo를 우선한다.")]
        public float attackRange = 2.5f;
        [Tooltip("레거시 공격 쿨타임. 실제 스킬 쿨타임은 EnemyAttackDataSO/EnemyAttackInfo를 우선한다.")]
        public float attackCooldown = 1.5f;
        public MonsterActorGrade grade = MonsterActorGrade.Normal;
        
        [Header("Patrol")]
        public bool enablePatrol = true;
        public float patrolRadius = 5f;
        public float patrolWaitTime = 2f;
    }
}
