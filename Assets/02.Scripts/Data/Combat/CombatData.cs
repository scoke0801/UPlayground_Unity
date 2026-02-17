using System;
using UnityEngine;
using UnityEngine.Serialization;
using UPlayGround.Data.Enum;

namespace UPlayGround.Data
{
    /// <summary>
    /// 기본 공격 정보
    /// </summary>
    [Serializable]
    public class AttackInfoBase
    {
        [Header("Basic Info")]
        public AnimKey animKey = AnimKey.Attack_1;
        public AttackType attackType = AttackType.Melee;
        public AttackReactionType reactionType = AttackReactionType.Hit;

        public string hitParticleName;
        
        [Header("Hitbox")]
        public Vector3 attackOffset = new Vector3(0, 1, 1.5f);
        public float attackRadius = 1.5f;
        
        [Header("Damage & Selection")]
        public float damage = 10f;

    }
    
    /// <summary>
    /// 적 공격 정보, 에디터 타임 사전 설정
    /// </summary>
    [Serializable]
    public class EnemyAttackInfo
    {
        public AttackInfoBase baseInfo;
        
        [Header("Selection Weight")]
        [Range(0f, 100f)]
        public float selectionWeight = 10f;
        
        [Header("Range")]
        public float minRange = 0f;    // 이 거리보다 멀어야 함
        public float maxRange = 2.5f; // 이 거리보다 가까워야 함
        
        [Header("Cooldown")]
        public float cooldown = 2f;
        
        public bool IsInRange(float distance)
        {
            return distance >= minRange && distance <= maxRange;
        }
    }
    
    /// <summary>
    /// 캐릭터 공격 정보, 에디터 타임 사전 설정
    /// </summary>
    [Serializable]
    public class PlayerAttackInfo
    {
        public AttackInfoBase baseInfo;
        
        [Tooltip("공격 중 끊을 수 있는지 여부")]
        public bool canBeInterrupted;
        
            
        [Tooltip("히트 판정 각도 (전방 기준, 양쪽 각도)")]
        public float hitAngle = 60f;
    }
    
    // 런타임에 결정되는 공격 정보
    public class AttackData
    {
        public AnimKey animKey;
        public float damage;
        public bool canBeInterrupted;

        public AttackReactionType reactionType = AttackReactionType.Hit;
        
        // Hit Detection Data
        public float hitRange;
        public float hitAngle;
        public float hitHeightOffset;
        
        public Vector3 hitPoint;        // 공격 적중 위치
        public GameObject hitTarget;     // 피격 대상
        public float criticalMultiplier; // 크리티컬 배율
        public bool isCounterAttack;     // 카운터 공격 여부
        public Vector3 attackDirection;
        public string hitParticleName = "LiteHit";
    }

}