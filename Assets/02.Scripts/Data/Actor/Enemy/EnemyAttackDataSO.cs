using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Enum;

namespace UPlayGround.Data.Enemy
{
    public enum EnemyAttackType { Melee, Ranged } // 공격 유형 정의
    /// <summary>
    /// 적 공격 정보
    /// </summary>
    [Serializable]
    public class EnemyAttackInfo
    {
        [Header("Basic Info")]
        public AnimKey animKey = AnimKey.Attack_1;
        public EnemyAttackType attackType = EnemyAttackType.Melee;
        public AttackReactionType reactionType = AttackReactionType.Hit;

        public string hitParticleName;
        
        [Header("Damage & Selection")]
        public float damage = 10f;
        
        [Header("Selection Weight")]
        [Range(0f, 100f)]
        
        public float selectionWeight = 10f;
        [Header("Hitbox")]
        public Vector3 attackOffset = new Vector3(0, 1, 1.5f);
        public float attackRadius = 1.5f;
        
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
    /// 적 공격 데이터 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyAttackData", menuName = "UPlayGround/Enemy/Attack Data")]
    public class EnemyAttackDataSO : ScriptableObject
    {
        [Header("Attack Pool")]
        [Tooltip("사용 가능한 공격 리스트 (가중치 기반 선택)")]
        public List<EnemyAttackInfo> skills  = new List<EnemyAttackInfo>();
        
        [Header("Global Settings")]
        public float globalCooldown = 1f;
        
        public List<EnemyAttackInfo> GetAvailableSkillsAtRange(float distance)
        {
            List<EnemyAttackInfo> availableSkills = new List<EnemyAttackInfo>();
            
            foreach (var skill in skills)
            {
                if (skill.IsInRange(distance))
                {
                    availableSkills.Add(skill);
                }
            }
            
            return availableSkills;
        }
        
        public EnemyAttackInfo SelectRandomSkill(List<EnemyAttackInfo> availableSkills)
        {
            if (availableSkills == null || availableSkills.Count == 0)
                return null;
            
            float totalWeight = 0f;
            foreach (var skill in availableSkills)
            {
                totalWeight += skill.selectionWeight;
            }
            
            float randomValue = UnityEngine.Random.Range(0f, totalWeight);
            float cumulativeWeight = 0f;
            
            foreach (var skill in availableSkills)
            {
                cumulativeWeight += skill.selectionWeight;
                if (randomValue <= cumulativeWeight)
                {
                    return skill;
                }
            }
            
            return availableSkills[availableSkills.Count - 1];
        }
        
        public float GetMaxAttackRange()
        {
            float maxRange = 0f;
            foreach (var skill in skills)
            {
                if (skill.maxRange > maxRange)
                {
                    maxRange = skill.maxRange;
                }
            }
            return maxRange;
        }
        
        public bool HasRangedSkill()
        {
            foreach (var skill in skills)
            {
                if (skill.attackType == EnemyAttackType.Ranged)
                    return true;
            }
            return false;
        }
    }
}