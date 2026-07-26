using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Data.EnumType;
using UPlayGround.Gameplay.Tag;

namespace UPlayGround.Data
{
    /// <summary>
    /// 런타임에 결정되는 공격 정보.
    /// </summary>
    public class AttackData
    {
        public MotionSetAsset motionAsset;
        public string MotionId => motionAsset != null ? motionAsset.name : "-";
        public float damage;
        public float poiseDamage = 30f;
        public float breakDamage = 10f;

        // 영속 배율: 멀티히트 페이즈 갱신(SetHitPhaseIndex)마다 phase 값에 곱해진다.
        // 연계 라우트 퍼펙트 타이밍 강화처럼 공유 SO(hitPhases)를 변형하지 않고 런타임 한정으로 데미지를 증폭할 때 쓴다.
        public float damageMultiplier = 1f;
        public float poiseMultiplier = 1f;
        public float breakDamageMultiplier = 1f;

        public float reactionDuration = 0f;
        public bool forceReaction = false;
        public bool forceBreakExpose = false;
        public PlayerInterruptAction interruptActions = PlayerInterruptAction.None;
        public float moveCancelDelayAfterLastHit = 0f;
        public AttackKind attackKind = AttackKind.NormalAttack;

        public AttackReactionType reactionType = AttackReactionType.Hit;

        public GameActor attacker;

        public Vector3 hitPoint;
        public GameObject hitTarget;
        public float criticalMultiplier;
        public bool isCounterAttack;

        // 카운터급 타격 피드백(히트스톱/카메라)만 원할 때 사용. isCounterAttack과 달리
        // MonsterActor의 리액션 정책 우회(정책 게이트 없는 shove 단락)를 유발하지 않는다.
        public bool useCounterHitFeedback;
        public Vector3 attackDirection;
        public string hitParticleName = "LiteHit";

        // 방어 대응 분류 — 퍼펙트 가드 카운터 성립 여부 판단에 사용. 기본 Parryable로 기존 동작 유지.
        public AttackDefenseType defenseType = AttackDefenseType.Parryable;

        // 투사체/AOE로 전달되는 공격 여부. defenseType과는 직교하는 전달 방식 플래그다.
        // true면 패리/카운터가 성립하지 않는다. BaseProjectile.Initialize에서 설정한다.
        public bool isProjectile = false;
        public bool isReflectableProjectile = false;

        public float pullForce = 10f;
        public float airborneForce = 8f;
        public float knockbackForce = 10f;
        public float knockbackDrag = 20f;

        public float grabDuration = 1.5f;

        public GameplayTag victimForcedMotionSlot;
        public bool guaranteedReaction = false;

        public int hitPhaseIndex = 0;

        public AttackReactionData reactionData;
    }
}
