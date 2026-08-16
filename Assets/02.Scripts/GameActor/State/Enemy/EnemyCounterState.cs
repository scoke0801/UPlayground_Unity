using UnityEngine;
using UPlayGround.Combat;
using UPlayGround.Components;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;
using UPlayGround.Animation;

namespace UPlayGround.State
{
    /// <summary>
    /// 카운터 공격 상태
    /// Guard 블록 성공 직후 진입. 빠른 전진 + 공격으로 Guard를 의미있게 만든다.
    /// 별도 애니메이션 없이 Attack 애니를 활용하되, 진입 시 빠른 전진 속도를 부여해 체감을 차별화한다.
    /// </summary>
    public class EnemyCounterState : EnemyActorState
    {
        public override ActorStateId StateId => ActorStateId.Counter;
        public override bool BlocksBehaviorTree => true;
        public override GravityOwnership GravityOwner => GravityOwnership.State;

        private readonly EnemyCombat _combat;
        private readonly EnemyAIContext _context;
        private readonly EnemyDetection _detection;
        private readonly EnemyTacticalMemory _memory;

        private AbilityAttackInfo _skill;
        private bool _isActive;
        private float _counterTimer;
        private float _counterTimeout;
        private MotionSet _counterMotionSet;

        // 카운터 전진 - Guard 블록 후 순간적으로 파고드는 느낌
        private const float DASH_IN_SPEED   = 10f;
        private const float DASH_IN_DURATION = 0.15f;
        // MotionSet 디렉터가 마지막 포즈를 샘플링한 뒤 개별 AnimancerState의 OnEnd를
        // 발생시키지 않는 경우에도 BT 차단 상태를 반드시 회수한다.
        private const float FALLBACK_COUNTER_TIMEOUT = 4f;
        private const float MINIMUM_PLAY_RATE = 0.5f;
        private const float MOTION_COMPLETION_GRACE = 0.25f;
        private float _dashTimer;

        public EnemyCounterState(
            ActorMovementController controller,
            EnemyCombat combat,
            EnemyAIContext context,
            EnemyDetection detection,
            EnemyTacticalMemory memory) : base(controller)
        {
            _combat   = combat;
            _context  = context;
            _detection = detection;
            _memory   = memory;
        }

        public override bool CanTransitionState(ActorStateId fromState) => true;

        public override bool CanPlayHitReaction(in HitContext hit)
        {
            return base.CanPlayHitReaction(hit)
                   && _combat != null
                   && !_combat.IsPossibleCollide;
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            _isActive  = true;
            _dashTimer = 0f;
            _counterTimer = 0f;
            _counterTimeout = FALLBACK_COUNTER_TIMEOUT;
            _counterMotionSet = null;

            float distance = _detection.DistanceToTarget;
            bool hasCounterAbility = _combat.HasAvailableSkillAtDistance(
                distance,
                AbilityAttackCategory.None,
                AbilityAIRole.Counter);
            if (hasCounterAbility)
            {
                _skill = _combat.SelectAndExecuteSkill(
                    distance,
                    AbilityAttackCategory.None,
                    AbilityAIRole.Counter);
            }
            else
            {
                // 전용 Counter가 없는 기존 몬스터는 짧은 Basic 공격으로만 폴백한다.
                _skill = _combat.SelectAndExecuteSkill(
                    distance,
                    AbilityAttackCategory.Basic);
            }

            if (_skill == null)
            {
                // 사용 가능한 스킬이 없으면 그냥 추격으로 빠짐
                controller.TransitionToState(
                    ActorStateId.Chase,
                    EnemyChaseContext.Default);
                return;
            }

            var motion = _combat.CurrentMotionAsset;
            var animState = motion != null
                ? gameActor.Animator.PlayAbilityMotion(_skill.motionKey, 0.05f)
                : null;
            if (animState != null)
            {
                float motionDuration = motion.motionSet?.TotalDuration ?? 0f;
                if (motionDuration > 0f)
                {
                    _counterTimeout =
                        motionDuration / MINIMUM_PLAY_RATE
                        + MOTION_COMPLETION_GRACE;
                }

                _counterMotionSet = gameActor.Animator.CurrentMotionSet;
                gameActor.Animator.OnMotionSetEndedWithReason += OnCounterMotionEnded;
            }
            else
                OnCounterEnd();
        }

        public override void OnExit(GameActorState toState)
        {
            gameActor.Animator.OnMotionSetEndedWithReason -= OnCounterMotionEnded;
            _counterMotionSet = null;
            base.OnExit(toState);
            _isActive = false;
            _combat.CancelCurrentAction();
        }

        public override void UpdateState(float deltaTime)
        {
            if (!_isActive) return;

            _counterTimer += deltaTime;
            if (_counterTimer >= _counterTimeout)
            {
                Debug.LogWarning(
                    $"[EnemyCounterState] 카운터 Motion 완료 신호가 없어 강제 종료합니다. " +
                    $"actor={gameActor.name}, " +
                    $"ability={_combat.CurrentAbility?.abilityId ?? "-"}, " +
                    $"motion={_combat.CurrentMotionAsset?.name ?? "-"}, " +
                    $"timeout={_counterTimeout:0.00}s",
                    gameActor);
                ForceCompleteCounter();
                return;
            }

            // 검출 요청만 표시하고 실제 Overlap은 EnemyCombat.LateUpdate에서 수행한다(갓 적용된 포즈).
            if ((_skill?.baseInfo.attackType == AttackType.Melee
                 || _combat.HasActiveExplicitCollision)
                && _combat.IsPossibleCollide)
                _combat.RequestMeleeHitCheck();
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            if (!_detection.HasTarget) return;

            Vector3 dir = (_detection.CurrentTarget.position - motor.TransientPosition).normalized;
            dir.y = 0;
            if (dir.sqrMagnitude > 0.01f)
            {
                currentRotation = Quaternion.Slerp(
                    currentRotation,
                    Quaternion.LookRotation(dir),
                    1 - Mathf.Exp(-controller.OrientationSharpness * deltaTime));
            }
            currentRotation = currentRotation.normalized;
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            float lastY = currentVelocity.y;

            // 초반 DASH_IN_DURATION 동안 타겟 방향으로 빠르게 전진
            if (_dashTimer < DASH_IN_DURATION && _detection.HasTarget)
            {
                Vector3 dir = (_detection.CurrentTarget.position - motor.TransientPosition).normalized;
                dir.y = 0;
                currentVelocity = dir * DASH_IN_SPEED;
                _dashTimer += deltaTime;
            }
            else
            {
                // 루트모션 또는 정지
                currentVelocity = _skill != null
                    ? gameActor.Animator.GetRootMotionStepVelocity(deltaTime)
                    : Vector3.zero;
            }

            currentVelocity.y = lastY;
            if (motor.GroundingStatus.IsStableOnGround)
            {
                if (currentVelocity.y < 0) currentVelocity.y = -0.1f;
            }
            else
            {
                currentVelocity += controller.Gravity * deltaTime;
            }
        }

        private void OnCounterEnd()
        {
            if (!_isActive) return;

            gameActor.Animator.OnMotionSetEndedWithReason -= OnCounterMotionEnded;
            _counterMotionSet = null;
            _combat.CompleteCurrentAbility();
            _memory?.NotifyAttackLanded();
            _combat.ClearHitTargets();

            // 카운터 후 → 후퇴로 자연스럽게 빠짐 (거리 리셋)
            if (_detection.HasTarget)
            {
                controller.TransitionToState(
                    new EnemyRetreatState(controller, _context, _detection, _context.RetreatDistance));
            }
            else
            {
                controller.TransitionToState(ActorStateId.Idle);
            }
        }

        private void ForceCompleteCounter()
        {
            gameActor.Animator.OnMotionSetEndedWithReason -= OnCounterMotionEnded;
            OnCounterEnd();
        }

        private void OnCounterMotionEnded(MotionSet motionSet, MotionSetEndReason _)
        {
            if (_counterMotionSet != null && ReferenceEquals(motionSet, _counterMotionSet))
                OnCounterEnd();
        }
    }
}
