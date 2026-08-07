using System;
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Data.Combat
{
    /// <summary>
    /// 스킬 조건 체크에 필요한 컨텍스트
    /// </summary>
    public class SkillConditionContext
    {
        public int CurrentLevel = 1;
        public float CurrentHealth;
        public float MaxHealth;
        public float DistanceToTarget;
        public int AllyCount;
        public int SpawnedUnitCount;
        public bool HasTarget;
        public Transform CasterTransform;
        public LayerMask AllyLayer;
        public float AllyDetectionRadius;
    }
    
    /// <summary>
    /// 개별 조건 타입
    /// </summary>
    [Serializable]
    public enum ConditionType
    {
        None,                   // 조건 없음
        SelfHealthBased,        // 자신의 HP 기반
        TargetHealthBased,      // 타겟의 HP 기반
        RangeBased,             // 거리 기반
        AllyCountBased,         // 아군 수 기반
        InjuredAllyNearby,      // 부상당한 아군이 주변에 있음
        SpawnedUnitCount,       // 소환 유닛 수 기반
        LevelBased,             // 몬스터 레벨 기반
    }
    
    /// <summary>
    /// 조건 연산자
    /// </summary>
    [Serializable]
    public enum ConditionOperator
    {
        And,    // 모든 조건 만족
        Or      // 하나라도 만족
    }
    
    /// <summary>
    /// 단일 조건 정보
    /// </summary>
    [Serializable]
    public class SkillCondition
    {
        public ConditionType type = ConditionType.None;
        
        [Header("Health Condition (SelfHealthBased / InjuredAllyNearby)")]
        [Range(0f, 1f)]
        public float minHealthPercent = 0f;

        [Tooltip("최소 HP 경계값을 조건에 포함합니다.")]
        public bool includeMinHealth = true;
        
        [Range(0f, 1f)]
        public float maxHealthPercent = 1f;

        [Tooltip("최대 HP 경계값을 조건에 포함합니다.")]
        public bool includeMaxHealth = true;
        
        [Header("Range Condition (RangeBased / InjuredAllyNearby)")]
        public float minRange = 0f;
        public float maxRange = 10f;
        
        [Header("Ally Count Condition (AllyCountBased)")]
        public int minAllyCount = 0;
        public int maxAllyCount = 99;

        [Header("Spawned Unit Condition")]
        public int checkSpawnCount = 0;

        [Header("Level Condition")]
        [Min(1)]
        public int minLevel = 1;
        [Min(1)]
        public int maxLevel = 99;
        
        /// <summary>
        /// 조건 체크
        /// </summary>
        public bool Check(SkillConditionContext context)
        {
            switch (type)
            {
                case ConditionType.None:
                    return true;
                    
                case ConditionType.SelfHealthBased:
                    return CheckSelfHealth(context);
                    
                case ConditionType.TargetHealthBased:
                    return CheckTargetHealth(context);
                    
                case ConditionType.RangeBased:
                    return CheckRange(context);
                    
                case ConditionType.AllyCountBased:
                    return CheckAllyCount(context);
                    
                case ConditionType.InjuredAllyNearby:
                    return CheckInjuredAllyNearby(context);
                    
                case ConditionType.SpawnedUnitCount:
                    return CheckSpawnedUnitCount(context);

                case ConditionType.LevelBased:
                    return CheckLevel(context);
                
                default:
                    return true;
            }
        }

        private bool CheckSpawnedUnitCount(SkillConditionContext context)
        {
            return context.SpawnedUnitCount < checkSpawnCount;
        }

        private bool CheckLevel(SkillConditionContext context)
        {
            int lo = Mathf.Min(minLevel, maxLevel);
            int hi = Mathf.Max(minLevel, maxLevel);
            return context.CurrentLevel >= lo && context.CurrentLevel <= hi;
        }

        private bool CheckSelfHealth(SkillConditionContext context)
        {
            float healthPercent = context.CurrentHealth / context.MaxHealth;
            return MatchesHealthPercent(healthPercent);
        }

        public bool MatchesHealthPercent(float healthPercent)
        {
            bool aboveMinimum = includeMinHealth
                ? healthPercent >= minHealthPercent
                : healthPercent > minHealthPercent;
            bool belowMaximum = includeMaxHealth
                ? healthPercent <= maxHealthPercent
                : healthPercent < maxHealthPercent;
            return aboveMinimum && belowMaximum;
        }
        
        private bool CheckTargetHealth(SkillConditionContext context)
        {
            if (!context.HasTarget)
                return false;
            
            // TODO: 타겟의 Health 컴포넌트 체크 필요
            return true;
        }
        
        private bool CheckRange(SkillConditionContext context)
        {
            if (!context.HasTarget)
                return false;
                
            return context.DistanceToTarget >= minRange && 
                   context.DistanceToTarget <= maxRange;
        }
        
        private bool CheckAllyCount(SkillConditionContext context)
        {
            return context.AllyCount >= minAllyCount && 
                   context.AllyCount <= maxAllyCount;
        }
        
        private bool CheckInjuredAllyNearby(SkillConditionContext context)
        {
            if (context.CasterTransform == null)
                return false;
            
            Collider[] nearbyAllies = Physics.OverlapSphere(
                context.CasterTransform.position, 
                maxRange, 
                context.AllyLayer);
            
            foreach (var allyCollider in nearbyAllies)
            {
                // 자기 자신은 제외
                if (allyCollider.transform == context.CasterTransform)
                    continue;
                
                // 아군의 체력 체크
                var healthProvider = allyCollider.GetComponent<IHealthRatioProvider>()
                                     ?? allyCollider.GetComponentInParent<IHealthRatioProvider>();
                if (healthProvider != null)
                {
                    float allyHealthPercent = healthProvider.HealthRatio;
                    
                    if (MatchesHealthPercent(allyHealthPercent))
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }
    }
    
    /// <summary>
    /// 복합 조건 그룹
    /// </summary>
    [Serializable]
    public class SkillConditionGroup
    {
        [Tooltip("조건 연산자 (And: 모두 만족, Or: 하나라도 만족)")]
        public ConditionOperator conditionOperator = ConditionOperator.And;
        
        [Tooltip("조건 리스트")]
        public List<SkillCondition> conditions = new List<SkillCondition>();
        
        /// <summary>
        /// 모든 조건 체크
        /// </summary>
        public bool CheckAll(SkillConditionContext context)
        {
            if (conditions == null || conditions.Count == 0)
                return true;
            
            if (conditionOperator == ConditionOperator.And)
            {
                // AND: 모든 조건이 참이어야 함
                foreach (var condition in conditions)
                {
                    if (!condition.Check(context))
                        return false;
                }
                return true;
            }
            else // OR
            {
                // OR: 하나라도 참이면 됨
                foreach (var condition in conditions)
                {
                    if (condition.Check(context))
                        return true;
                }
                return false;
            }
        }
    }
}
