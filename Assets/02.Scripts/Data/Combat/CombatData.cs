using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data
{
    public enum TelegraphShape
    {
        Circle,
        Cone,
        Line
    }

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
        public float pullForce      = 10f;
        public float airborneForce  = 8f;
        public float knockBackForce = 10f;
        [Tooltip("넉백 감속 강도. 높을수록 빠르게 멈춤\nKnockBack 권장: 20 / Airborne 권장: 5")]
        public float knockBackDrag  = 20f;

        [Header("Grab")]
        [Tooltip("Grab 지속 시간 (초)")]
        public float grabDuration = 1.5f;

        [Header("Forced Motion")]
        [Tooltip("Grab 리액션 시 피격자에게 강제할 애니메이션. None이면 AnimKey.Grabbed 폴백.")]
        public AnimKey victimForcedAnimKey = AnimKey.None;
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

        [Header("Unlock")]
        [Min(1)]
        [Tooltip("이 레벨 이상인 몬스터만 이 스킬을 선택할 수 있습니다.")]
        public int requiredLevel = 1;

        [Header("Selection Weight")]
        [Range(0f, 100f)]
        public float selectionWeight = 10f;

        [Header("Range")]
        public float minRange = 0f;
        public float maxRange = 2.5f;

        [Header("Cooldown")]
        public float cooldown = 2f;

        [Header("Telegraph")]
        [Tooltip("강공격 판정 전에 텔레그래프 경고 연출을 사용할지 여부")]
        public bool useTelegraph = false;
        [Tooltip("텔레그래프 형태. 현재 런타임 구현은 Circle만 지원한다.")]
        public TelegraphShape telegraphShape = TelegraphShape.Circle;
        [Tooltip("현재 히트 반경에 곱할 텔레그래프 표시 배율")]
        public float telegraphRadiusScale = 1f;

        [Header("Aerial")]
        [Tooltip("true = EnemyAerialState에서만 선택되는 공중 전용 스킬")]
        public bool isAerialSkill = false;
        [Tooltip("true = Dive Attack 전용 하강 이동 로직 사용")]
        public bool isDiveAttack = false;
        [Tooltip("Dive Attack 전용 하강 속도 (기본 낙하보다 빠르게)")]
        public float diveDescentSpeed = 15f;
        [Tooltip("공중 스킬 가중치 (aerialSkillWeight > 0 인 스킬끼리 경쟁)")]
        public float aerialSkillWeight = 1f;

        [Header("Activation Conditions")]
        [Tooltip("복합 조건 설정 (여러 조건을 AND/OR로 연결)")]
        public SkillConditionGroup conditionGroup = new SkillConditionGroup();

        public bool IsUnlockedForLevel(int level) => level >= requiredLevel;

        public bool IsInRange(float distance) => distance >= minRange && distance <= maxRange;

        public bool CheckCondition(SkillConditionContext context) => conditionGroup.CheckAll(context);

        public bool CanUse(float distance, SkillConditionContext context)
            => IsUnlockedForLevel(context.CurrentLevel)
               && IsInRange(distance)
               && CheckCondition(context);
    }

    /// <summary>
    /// 차지 단계별 공격 데이터.
    /// AnimKey는 PlayerAttackDataSO.chargeAnimKey 하나로 공유하므로 수치만 포함한다.
    /// </summary>
    [Serializable]
    public class ChargeStageData
    {
        [Header("Hit Phases")]
        [Tooltip("히트 구간 별 데이터. BeginCollisionEvent의 hitPhaseIndex와 인덱스가 일치해야 한다.")]
        public List<HitPhaseData> hitPhases = new List<HitPhaseData> { new HitPhaseData() };

        [Tooltip("공격 중 끊을 수 있는지 여부")]
        public bool canBeInterrupted;

        [Tooltip("히트 판정 각도 (전방 기준, 양쪽 각도)")]
        public float hitAngle = 60f;

        public HitPhaseData GetHitPhase(int index)
        {
            if (hitPhases == null || hitPhases.Count == 0) return new HitPhaseData();
            return hitPhases[Mathf.Clamp(index, 0, hitPhases.Count - 1)];
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
        public AttackKind attackKind = AttackKind.NormalAttack;

        public AttackReactionType reactionType = AttackReactionType.Hit;

        // Hit Detection
        public GameActor attacker;
        public float hitRange;
        public float hitAngle;
        public float hitHeightOffset;
        public float hitHeightRange = -1f;

        public Vector3 hitPoint;
        public GameObject hitTarget;
        public float criticalMultiplier;
        public bool isCounterAttack;
        public Vector3 attackDirection;
        public string hitParticleName = "LiteHit";

        // 반응 파라미터
        public float pullForce      = 10f;
        public float airborneForce  = 8f;
        public float knockbackForce = 10f;
        public float knockbackDrag  = 20f;

        // Grab
        public float grabDuration = 1.5f;

        // Forced Motion
        public AnimKey victimForcedAnimKey = AnimKey.None;

        // 멀티 히트
        public int hitPhaseIndex = 0;
    }
}
