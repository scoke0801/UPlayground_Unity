using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Animation;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Gameplay.Tag;
using UPlayGround.Data.Projectile;

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
        [Tooltip("이 히트 페이즈가 발사할 조합형 투사체 정의. 비어 있으면 레거시 투사체 경로를 사용한다.")]
        public ProjectileDefinitionSO projectileDefinition;
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
        [Tooltip("피격자에게 강제할 리액션 모션 슬롯. 비어 있으면 공격 반응 종류와 방향에 따른 기본 슬롯을 사용합니다.")]
        public GameplayTag victimForcedMotionSlot;
        [Tooltip("true면 등급 리액션 정책(allowHit/requirePoiseBreak 등)을 무시하고 피격 리액션을 보장한다. 보스 등 강인한 적도 확실히 흔들린다.")]
        public bool guaranteedReaction = false;

        [Header("Auto Reaction")]
        public AttackReactionProfile reactionProfile = new AttackReactionProfile();

        /// <summary>
        /// 히트 페이즈가 없는 모션 전용 Ability용 무피해 페이즈.
        /// 기본 생성자는 damage 10 / poise 30 / break 10을 가지므로, 페이즈 부재를
        /// 기본 인스턴스로 대체하면 의도하지 않은 유령 피해가 발생한다.
        /// 텔레그래프·위협 UI가 쓰는 impactOffset/targetingRange는 기본값을 유지한다.
        /// </summary>
        public static HitPhaseData CreateNonDamaging() => new()
        {
            damage = 0f,
            poiseDamage = 0f,
            breakDamage = 0f,
        };
    }

    /// <summary>
    /// 기본 공격 정보.
    /// hitPhases[0]이 기본값이며, 멀티 히트 공격은 hitPhases를 추가한다.
    /// </summary>
    [Serializable]
    public class AttackInfoBase
    {
        [Header("Basic Info")]
        [Tooltip("Ability/Variant를 액터 소유 모션에 연결하는 실행 키입니다.")]
        public MotionKey motionKey;

        public AttackType attackType = AttackType.Melee;

        [Header("Hit Phases")]
        [Tooltip("히트 구간 별 데이터. BeginCollisionEvent의 hitPhaseIndex와 인덱스가 일치해야 한다.")]
        public List<HitPhaseData> hitPhases = new List<HitPhaseData> { new HitPhaseData() };

        /// <summary>
        /// 히트 페이즈가 하나라도 있는지. 공격 Ability와 모션 전용 Ability를 구분하는 파생 술어로,
        /// 손으로 켜는 플래그보다 이쪽이 권위 있다. 런타임 BT 공격 선택도 이 값을 쓴다.
        /// </summary>
        public bool HasHitPhases => hitPhases != null && hitPhases.Count > 0;

        /// <summary>
        /// 인덱스가 범위를 벗어나면 마지막 Phase를 반환 (안전 폴백).
        /// 페이즈가 아예 없으면 무피해 페이즈를 반환한다 — 부재를 알리지 않고 값을 만들어
        /// 주므로, 공격 여부 판정에는 이 메서드가 아니라 <see cref="HasHitPhases"/>를 쓴다.
        /// </summary>
        public HitPhaseData GetHitPhase(int index)
        {
            if (!HasHitPhases) return HitPhaseData.CreateNonDamaging();
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

        public bool HasHitPhases => hitPhases != null && hitPhases.Count > 0;

        /// <summary>페이즈가 없으면 무피해 페이즈를 반환한다. 근거는 AttackInfoBase.GetHitPhase와 같다.</summary>
        public HitPhaseData GetHitPhase(int index)
        {
            if (!HasHitPhases) return HitPhaseData.CreateNonDamaging();
            return hitPhases[Mathf.Clamp(index, 0, hitPhases.Count - 1)];
        }
    }

    [Serializable]
    public class AerialMovementProfile
    {
        [Tooltip("진입 순간 상승 속도가 이 값보다 작으면 보정한다. 0이면 기존 탄도를 그대로 보존한다.")]
        [Min(0f)] public float minimumEntryUpwardSpeed;

        [Tooltip("공격 시작 구간의 중력 배율 보정")]
        [Min(0f)] public float startupGravityScale = 1f;
        [Min(0f)] public float startupDuration = 0.1f;

        [Tooltip("정점 부근의 중력 배율 보정")]
        [Min(0f)] public float apexGravityScale = 1f;
        [Min(0f)] public float apexVelocityThreshold;
        [Min(0f)] public float maximumApexDuration;

        [Tooltip("시작/정점 구간 이후 중력 배율 보정")]
        [Min(0f)] public float recoveryGravityScale = 1f;
        [Tooltip("0이면 종단 낙하 속도를 제한하지 않는다.")]
        [Min(0f)] public float terminalFallSpeed;

        [Tooltip("공중 공격 중 수평 루트모션 반영률. 수직 루트모션은 항상 물리 탄도에서 제외한다.")]
        [Range(0f, 1f)] public float horizontalRootMotionInfluence;
    }

    /// <summary>
    /// 캐릭터 공격 정보, 에디터 타임 사전 설정
    /// </summary>
    [Serializable]
    public class AbilityAttackInfo
    {
        public AttackInfoBase baseInfo;

        [Tooltip("공격 중 캔슬 가능한 입력 액션 마스크 (None이면 캔슬 불가).\n허용 구간은 캔슬 윈도우(콜리전 비활성 구간)가 결정 — 액티브 히트 중엔 캔슬 불가.\n공격타입(Light/Heavy/Skill)은 '다른 타입'으로의 전환용. 같은 타입 연계는 ComboWindow 사용.")]
        public PlayerInterruptAction interruptActions = PlayerInterruptAction.None;

        [Min(0f)]
        [Tooltip("마지막 히트 판정이 끝난 뒤 이동 후딜 캔슬을 허용하기까지의 지연 시간(초). 0이면 기존처럼 조건 충족 즉시 이동 캔슬.")]
        public float moveCancelDelayAfterLastHit = 0f;

        [Header("AI Selection")]
        [Tooltip("몬스터 AI가 AbilitySet 후보 중 이 공격을 자동 선택할 수 있는지 지정합니다.\n"
                 + "false인 Ability는 연출·명시 실행에는 사용할 수 있지만 BT/Intent 자동 선택 후보에서는 제외됩니다.")]
        public bool aiSelectable;
        public SkillType skillType = SkillType.Attack;
        [Tooltip("BT가 특정 공격 카테고리를 요청할 때 필터링에 사용한다. None이면 모든 요청에 포함된다.")]
        public AbilityAttackCategory attackCategory = AbilityAttackCategory.None;
        [Min(1)] public int requiredLevel = 1;
        [Range(0f, 100f)] public float selectionWeight = 10f;

        [Header("Telegraph")]
        public bool useTelegraph;
        public TelegraphShape telegraphShape = TelegraphShape.Circle;
        public float telegraphRadiusScale = 1f;
        public string telegraphFXKey;
        public bool useMotionEventTelegraph;
        public TelegraphAnchorType telegraphAnchorType = TelegraphAnchorType.CasterOffset;
        public bool useTelegraphPositionForHit;

        [Header("Danger Ring")]
        public bool useDangerRing;
        public float dangerRingDuration;
        public string dangerRingPrefabKey;

        [Header("Defense")]
        public AttackDefenseType defenseType = AttackDefenseType.Parryable;

        [Header("Aerial")]
        public bool isAerialSkill;
        public bool isDiveAttack;
        public float diveDescentSpeed = 15f;
        public float aerialSkillWeight = 1f;
        public AerialMovementProfile aerialMovement = new();

        [Header("AI Conditions")]
        public SkillConditionGroup conditionGroup = new();

        public bool IsUnlockedForLevel(int level) => level >= requiredLevel;

        public bool CheckCondition(SkillConditionContext context) =>
            conditionGroup == null || conditionGroup.CheckAll(context);
    }

}
