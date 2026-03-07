using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data
{
    /// <summary>
    /// 하나의 공격 애니메이션 안에서 발생하는 개별 히트 구간 데이터.
    /// BeginCollisionEvent의 hitPhaseIndex와 1:1 매칭된다.
    /// </summary>
    [Serializable]
    public class HitPhaseData
    {
        [Header("Damage")]
        public float damage = 10f;
        [Tooltip("적중 시 Poise를 깎는 양. 0이면 항상 경직")]
        public float poiseDamage = 30f;
        public AttackReactionType reactionType = AttackReactionType.Hit;

        [Header("Hitbox")]
        public Vector3 attackOffset = new Vector3(0, 1, 1.5f);
        public float attackRadius = 1.5f;
        [Tooltip("-1이면 Y 범위 무제한. 0 초과면 attackOffset.y 기준 위아래 hitHeightRange로 클램프")]
        public float hitHeightRange = 1.2f;

        [Header("FX")]
        public string hitParticleName = "LiteHit";

        [Header("Reaction Forces")]
        public float pullForce    = 10f;
        public float airborneForce = 8f;
        public float knockBackForce = 10f;

        [Header("Grab")]
        [Tooltip("Grab 지속 시간 (초)")]
        public float grabDuration = 1.5f;
    }

    /// <summary>
    /// 기본 공격 정보.
    /// hitPhases[0]이 기본값이며, 멀티 히트 공격은 hitPhases를 추가한다.
    /// </summary>
    [Serializable]
    public class AttackInfoBase
    {
        [Header("Basic Info")]
        public AnimKey animKey = AnimKey.Attack_1;
        public AttackType attackType = AttackType.Melee;

        [Header("Hit Phases")]
        [Tooltip("히트 구간 별 데이터. BeginCollisionEvent의 hitPhaseIndex와 인덱스가 일치해야 한다.")]
        public List<HitPhaseData> hitPhases = new List<HitPhaseData> { new HitPhaseData() };

        /// <summary> 인덱스가 범위를 벗어나면 마지막 Phase를 반환 (안전 폴백) </summary>
        public HitPhaseData GetHitPhase(int index)
        {
            if (hitPhases == null || hitPhases.Count == 0) return new HitPhaseData();
            return hitPhases[Mathf.Clamp(index, 0, hitPhases.Count - 1)];
        }

        public AttackReactionType reactionType => GetHitPhase(0).reactionType;
        public string hitParticleName          => GetHitPhase(0).hitParticleName;
        public Vector3 attackOffset            => GetHitPhase(0).attackOffset;
        public float attackRadius              => GetHitPhase(0).attackRadius;
        public float damage                    => GetHitPhase(0).damage;
        public float poiseDamage               => GetHitPhase(0).poiseDamage;
    }
    
    /// <summary>
    /// 적 공격 정보, 에디터 타임 사전 설정
    /// </summary>
    [Serializable]
    public class EnemyAttackInfo
    {  
        public AttackInfoBase baseInfo;
        
        [Header("Skill Type")]
        public SkillType skillType = SkillType.Attack;

        [Header("Selection Weight")]
        [Range(0f, 100f)]
        public float selectionWeight = 10f;
        
        [Header("Range")]
        public float minRange = 0f;    // 이 거리보다 멀어야 함
        public float maxRange = 2.5f; // 이 거리보다 가까워야 함
        
        [Header("Cooldown")]
        public float cooldown = 2f;
        
        [Header("Activation Conditions")]
        [Tooltip("복합 조건 설정 (여러 조건을 AND/OR로 연결)")]
        public SkillConditionGroup conditionGroup = new SkillConditionGroup();

        /// <summary>
        /// 거리 범위 체크
        /// </summary>
        public bool IsInRange(float distance)
        {
            return distance >= minRange && distance <= maxRange;
        }
        
        /// <summary>
        /// 스킬 발동 조건 체크 (복합 조건 지원)
        /// </summary>
        public bool CheckCondition(SkillConditionContext context)
        {
            return conditionGroup.CheckAll(context);
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
        public float poiseDamage = 30f;
        public bool canBeInterrupted;
        public AttackKind attackKind = AttackKind.NormalAttack;  // 게이지 충전 구분용

        public AttackReactionType reactionType = AttackReactionType.Hit;

        // Hit Detection Data
        public GameActor attacker;
        public float hitRange;
        public float hitAngle;
        public float hitHeightOffset;
        // -1이면 Y축 범위 무제한 (기존 OverlapSphere에 맡김). 0 초과면 origin 기준 위 아래 hitHeightRange로 클램프
        public float hitHeightRange = -1f;

        public Vector3 hitPoint;
        public GameObject hitTarget;
        public float criticalMultiplier;
        public bool isCounterAttack;
        public Vector3 attackDirection;
        public string hitParticleName = "LiteHit";

        // ── 반응 파라미터 ──────────────────────────
        public float pullForce    = 10f;
        public float airborneForce = 8f;
        public float knockbackForce = 10f;

        // ── Grab 파라미터 ──────────────────────────
        /// <summary> Grab 지속 시간 (초). </summary>
        public float grabDuration = 1.5f;

        // ── 멀티 히트 ──────────────────────────────
        /// <summary> 현재 몇 번째 히트 구간인지 (BeginCollisionEvent.hitPhaseIndex와 동기화) </summary>
        public int hitPhaseIndex = 0;
    }

}