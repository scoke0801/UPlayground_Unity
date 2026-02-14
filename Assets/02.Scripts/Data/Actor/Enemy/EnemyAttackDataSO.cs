using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Enum;

namespace UPlayGround.Data.Enemy
{
    /// <summary>
    /// 적 공격 정보
    /// </summary>
    [Serializable]
    public class EnemyAttackInfo
    {
        [Header("Basic Info")]
        public AnimKey animKey = AnimKey.Attack_1;

        public AttackReactionType reactionType = AttackReactionType.Hit;
        
        [Header("Damage")]
        public float damage = 10f;
        
        [Header("Hitbox")]
        public Vector3 attackOffset = new Vector3(0, 1, 1.5f);
        public float attackRadius = 1.5f;
        
        [Header("Timing")]
        [Tooltip("공격 애니메이션 시작 후 히트 판정 시작 시간")]
        public float hitStartTime = 0.3f;
        
        [Tooltip("공격 애니메이션 시작 후 히트 판정 종료 시간")]
        public float hitEndTime = 0.6f;
        
        [Header("Movement")]
        [Tooltip("공격 중 이동 속도 배율 (0 = 정지, 1 = 일반 속도)")]
        [Range(0f, 1f)]
        public float moveSpeedMultiplier = 0.2f;
        
        [Header("Combo")]
        [Tooltip("다음 콤보 입력 가능 시작 시간")]
        public float comboWindowStart = 0.5f;
        
        [Tooltip("다음 콤보 입력 가능 종료 시간")]
        public float comboWindowEnd = 1.0f;
    }

    /// <summary>
    /// 적 공격 데이터 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyAttackData", menuName = "UPlayGround/Enemy/Attack Data")]
    public class EnemyAttackDataSO : ScriptableObject
    {
        [Header("Attack Chain")]
        [Tooltip("공격 콤보 리스트 (순서대로 실행)")]
        public List<EnemyAttackInfo> AttackList = new List<EnemyAttackInfo>();
        
        [Header("Cooldown")]
        [Tooltip("공격 후 대기 시간 (초)")]
        public float attackCooldown = 1.5f;
        
        [Header("Decision")]
        [Tooltip("공격 시도 확률 (0~1)")]
        [Range(0f, 1f)]
        public float attackProbability = 0.7f;
    }
}