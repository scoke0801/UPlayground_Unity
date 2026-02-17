using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Enum;

namespace UPlayGround.Data.Combat
{
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
                if (skill.baseInfo.attackType == AttackType.Ranged)
                    return true;
            }
            return false;
        }
    }
}