using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data
{
    /// <summary>
    /// MotionSet/HitPhase 기반 자동 분석 결과.
    /// 1차 구현은 AnimationClip 샘플링 없이 Collision 타이밍과 공격 카테고리 기반 초안값을 저장한다.
    /// </summary>
    [Serializable]
    public class AttackMotionAnalysisResult
    {
        [Tooltip("true면 실제 AnimationClip 샘플링이 아니라 MotionSet 타이밍/공격 카테고리 기반 추정값이다.")]
        public bool isEstimated = true;

        [Range(0f, 1f)] public float weaponSpeedScore;
        [Range(0f, 1f)] public float rootMotionScore;
        [Range(0f, 1f)] public float bodyRotationScore;
        [Range(0f, 1f)] public float attackWeightScore;
        [Range(0f, 1f)] public float impactScore;

        public float activeStart;
        public float activeEnd;
        public float activeDuration;
        public float startupDuration;
        public float recoveryDuration;
    }

    /// <summary>
    /// 적중/허공 공격 피드백에 사용할 확정 반응 데이터.
    /// HitStop은 실제 적중 시점에서만 사용하고, FakeImpact는 허공 공격 연출에 사용한다.
    /// </summary>
    [Serializable]
    public class AttackReactionData
    {
        public float impactTime;

        [Header("Hit Confirm")]
        public float hitStopDuration;
        [Range(0.01f, 1f)] public float hitStopScale = 0.1f;

        [Header("Camera")]
        public float cameraShakeAmplitude;
        public float cameraShakeDuration;
        public float fovKickAmount;
        public float fovKickDuration;

        [Header("Air Swing")]
        public float trailIntensity;
        [Range(0.01f, 1f)] public float fakeImpactSlowScale = 0.9f;
        public float fakeImpactDuration;
    }

    [Serializable]
    public class ManualReactionOverride
    {
        public bool overrideImpactTime;
        public float impactTime;

        public bool overrideHitStop;
        public float hitStopDuration;
        [Range(0.01f, 1f)] public float hitStopScale = 0.1f;

        public bool overrideCamera;
        public float cameraShakeAmplitude;
        public float cameraShakeDuration;

        public bool overrideFov;
        public float fovKickAmount;
        public float fovKickDuration;

        public bool overrideTrail;
        public float trailIntensity;

        public bool overrideFakeImpact;
        [Range(0.01f, 1f)] public float fakeImpactSlowScale = 0.9f;
        public float fakeImpactDuration;
    }

    [Serializable]
    public class AttackReactionProfile
    {
        public bool useAutoReaction = true;
        public bool hasAutoReactionGenerated;
        public AttackMotionAnalysisResult analysis = new AttackMotionAnalysisResult();
        public AttackReactionData autoData = new AttackReactionData();

        public bool useManualOverride;
        public ManualReactionOverride manualOverride = new ManualReactionOverride();

        [NonSerialized] private AttackReactionData _resolvedCache;

        public AttackReactionData Resolve()
        {
            if (!useAutoReaction)
                return null;

            AttackReactionData source = autoData ?? new AttackReactionData();
            if (!useManualOverride || manualOverride == null)
                return source;

            _resolvedCache ??= new AttackReactionData();
            _resolvedCache.impactTime = manualOverride.overrideImpactTime ? manualOverride.impactTime : source.impactTime;
            _resolvedCache.hitStopDuration = manualOverride.overrideHitStop ? manualOverride.hitStopDuration : source.hitStopDuration;
            _resolvedCache.hitStopScale = manualOverride.overrideHitStop ? manualOverride.hitStopScale : source.hitStopScale;
            _resolvedCache.cameraShakeAmplitude = manualOverride.overrideCamera ? manualOverride.cameraShakeAmplitude : source.cameraShakeAmplitude;
            _resolvedCache.cameraShakeDuration = manualOverride.overrideCamera ? manualOverride.cameraShakeDuration : source.cameraShakeDuration;
            _resolvedCache.fovKickAmount = manualOverride.overrideFov ? manualOverride.fovKickAmount : source.fovKickAmount;
            _resolvedCache.fovKickDuration = manualOverride.overrideFov ? manualOverride.fovKickDuration : source.fovKickDuration;
            _resolvedCache.trailIntensity = manualOverride.overrideTrail ? manualOverride.trailIntensity : source.trailIntensity;
            _resolvedCache.fakeImpactSlowScale = manualOverride.overrideFakeImpact ? manualOverride.fakeImpactSlowScale : source.fakeImpactSlowScale;
            _resolvedCache.fakeImpactDuration = manualOverride.overrideFakeImpact ? manualOverride.fakeImpactDuration : source.fakeImpactDuration;
            return _resolvedCache;
        }
    }

    public enum TelegraphShape
    {
        Circle,
        Cone,
        Line
    }

    public enum TelegraphAnchorType
    {
        CasterOffset,
        TargetPosition
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
        [Tooltip("적중 시 Poise를 깎는 양. 0이면 Poise를 깎지 않음")]
        public float poiseDamage = 30f;
        [Tooltip("몬스터 Break Gauge 잔량을 깎는 양. 0이면 Break 피해 없음")]
        public float breakDamage = 10f;
        public AttackReactionType reactionType = AttackReactionType.Hit;
        [Tooltip("0이면 상태/애니메이션 기본 지속시간을 사용한다.")]
        public float reactionDuration = 0f;
        [Tooltip("true면 Poise가 남아 있어도 해당 리액션 상태 전환을 강제한다.")]
        public bool forceReaction = false;
        [Tooltip("true면 Break Gauge 잔량과 무관하게 즉시 노출(브레이크 공격 가능) 상태로 만든다.")]
        public bool forceBreakExpose = false;

        [Header("Attached HitBox")]
        [Tooltip("BeginCollisionEvent에 그룹이 없을 때 사용할 CombatHitbox.groupId")]
        public string hitboxGroupId = "Default";

        [Header("Telegraph (Generated — HitBox에서 베이크)")]
        [Tooltip("적 텔레그래프/위협존 기준 위치. attackOrigin 상대 좌표. 직접 수정 금지 — 부착형 HitBox impact 포즈에서 베이크된다.")]
        public Vector3 impactOffset = new Vector3(0, 1, 1.5f);
        [Min(0f)]
        [Tooltip("적 텔레그래프/위협존 반경. 직접 수정 금지 — 부착형 HitBox impact 포즈에서 베이크된다.")]
        public float targetingRange = 1.5f;

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
        [Tooltip("피격자에게 강제할 리액션 애니메이션. Grab은 None일 때 AnimKey.Grabbed 폴백, 일반 Hit은 None일 때 reactionType/방향 기본 리액션 폴백.")]
        public AnimKey victimForcedAnimKey = AnimKey.None;
        [Tooltip("true면 등급 리액션 정책(allowHit/requirePoiseBreak 등)을 무시하고 피격 리액션을 보장한다. 보스 등 강인한 적도 확실히 흔들린다.")]
        public bool guaranteedReaction = false;

        [Header("Auto Reaction")]
        public AttackReactionProfile reactionProfile = new AttackReactionProfile();
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
        public Vector3 impactOffset            => GetHitPhase(0).impactOffset;
        public float targetingRange            => GetHitPhase(0).targetingRange;
        public float damage                    => GetHitPhase(0).damage;
        public float poiseDamage               => GetHitPhase(0).poiseDamage;
        public float breakDamage               => GetHitPhase(0).breakDamage;
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

        [Header("AI Selection")]
        [Tooltip("BT가 특정 공격 카테고리를 요청할 때 필터링에 사용한다. None이면 모든 카테고리 요청에 포함된다.")]
        public EnemyAttackCategory attackCategory = EnemyAttackCategory.None;

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
        [Tooltip("비워두면 기본 형태별 FX 키를 사용한다. 현재 기본값: EnemyHeavyAttackTelegraph_Circle")]
        public string telegraphFXKey;
        [Tooltip("true면 EnemyAttackState 진입 시 자동 표시하지 않고 MotionSet의 TelegraphEvent 타이밍을 따른다.")]
        public bool useMotionEventTelegraph = false;
        [Tooltip("텔레그래프 위치 기준. TargetPosition은 시전 시작 시 현재 타겟 위치에 고정하는 AOE 장판에 사용한다.")]
        public TelegraphAnchorType telegraphAnchorType = TelegraphAnchorType.CasterOffset;
        [Tooltip("true면 TelegraphEvent에서 예약한 위치를 실제 Collision 판정 위치로 사용한다. TargetPosition AOE에 사용한다.")]
        public bool useTelegraphPositionForHit = false;

        [Header("Danger Ring (UI)")]
        [Tooltip("공격 윈드업 동안 적 몸통(락온 포커스 지점)에 수축 타이밍 링을 표시할지 여부. useTelegraph와 독립이다.")]
        public bool useDangerRing = false;
        [Tooltip("Danger Ring 수축 시간(초) — 보통 비워둠(0). 기본은 타임라인의 다음 Collision 이벤트까지 자동 산출된다. 공격자 타임라인에 Collision 이벤트가 없는 투사체 공격 등에서만 폴백으로 수동 지정. 0 이하면 자동/기본값(0.6초) 사용.")]
        public float dangerRingDuration = 0f;
        [Tooltip("비워두면 기본 Danger Ring 프리팹(UIPrefabDatabase의 \"DangerRing\" 키)을 사용한다.")]
        public string dangerRingPrefabKey;

        [Header("Defense")]
        [Tooltip("이 공격에 대한 플레이어 방어 대응 분류. Danger Ring 색과 패링(카운터) 성립 여부를 결정한다.")]
        public AttackDefenseType defenseType = AttackDefenseType.Parryable;

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
    /// AnimKey는 AbilitySetSO의 차지 단계 Ability Payload가 소유한다.
    /// </summary>
    [Serializable]
    public class ChargeStageData
    {
        [Header("Hit Phases")]
        [Tooltip("히트 구간 별 데이터. BeginCollisionEvent의 hitPhaseIndex와 인덱스가 일치해야 한다.")]
        public List<HitPhaseData> hitPhases = new List<HitPhaseData> { new HitPhaseData() };

        [Tooltip("공격 중 캔슬 가능한 입력 액션 마스크 (None이면 캔슬 불가).\n허용 구간은 캔슬 윈도우(콜리전 비활성 구간)가 결정 — 액티브 히트 중엔 캔슬 불가.\n공격타입(Light/Heavy/Skill)은 '다른 타입'으로의 전환용. 같은 타입 연계는 ComboWindow 사용.")]
        public PlayerInterruptAction interruptActions = PlayerInterruptAction.None;

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

        [Tooltip("공격 중 캔슬 가능한 입력 액션 마스크 (None이면 캔슬 불가).\n허용 구간은 캔슬 윈도우(콜리전 비활성 구간)가 결정 — 액티브 히트 중엔 캔슬 불가.\n공격타입(Light/Heavy/Skill)은 '다른 타입'으로의 전환용. 같은 타입 연계는 ComboWindow 사용.")]
        public PlayerInterruptAction interruptActions = PlayerInterruptAction.None;

        [Min(0f)]
        [Tooltip("마지막 히트 판정이 끝난 뒤 이동 후딜 캔슬을 허용하기까지의 지연 시간(초). 0이면 기존처럼 조건 충족 즉시 이동 캔슬.")]
        public float moveCancelDelayAfterLastHit = 0f;
    }

}
