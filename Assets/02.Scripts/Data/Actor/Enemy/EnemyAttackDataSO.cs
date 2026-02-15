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
        public List<ComboData> AttackList = new List<ComboData>();
        
        [Header("Cooldown")]
        [Tooltip("공격 후 대기 시간 (초)")]
        public float attackCooldown = 1.5f;
        
        [Header("Decision")]
        [Tooltip("공격 시도 확률 (0~1)")]
        [Range(0f, 1f)]
        public float attackProbability = 0.7f;
    }
}