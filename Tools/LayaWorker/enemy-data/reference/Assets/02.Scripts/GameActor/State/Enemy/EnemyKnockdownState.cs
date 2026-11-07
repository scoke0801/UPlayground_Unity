using UnityEngine;
using UPlayGround.Combat;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;
using UPlayGround.Animation;

namespace UPlayGround.State
{
    public class EnemyKnockdownState : EnemyActorState
    {
        public override ActorStateId StateId => ActorStateId.Knockdown;
        public override bool BlocksBehaviorTree => true;

        private readonly HitContext _hit;
        private readonly float _overrideDownDuration;
        private readonly float _knockbackDistance;
        private readonly float _knockbackDuration;
        private readonly float _maxKnockbackSpeed;
        private readonly Transform _knockbackSource;

        private const float FALLBACK_MOTION_TIMEOUT = 2f;
        private const float MINIMUM_PLAY_RATE = 0.5f;
        private const float MOTION_COMPLETION_GRACE = 0.25f;

        private bool _isActive;
        private bool _wallImpactConsumed;
        private bool _getupStarted;
        private bool _knockdownMotionEnded;
        private float _downTimer;
        private float _knockbackElapsed;
        private float _knockdownMotionTimer;
        private float _knockdownMotionTimeout;
        private float _getupMotionTimer;
        private float _getupMotionTimeout;
        private Vector3 _knockbackDirection;
        private MotionSet _knockdownMotionSet;
        private MotionSet _getupMotionSet;

        /// <param name="overrideDownDuration">0보다 크면 누워있는 시간을 강제로 지정(브레이크 마무리용). 0이면 기존 규칙 사용.</param>
        /// <param name="knockbackDistance">0보다 크면 진입 시 공격자 반대 방향으로 날아가는 거리. 0이면 런치 없음.</param>
        /// <param name="knockbackSource">날아가는 방향 기준(공격자). null이면 -forward.</param>
        public EnemyKnockdownState(
            ActorMovementController controller,
            in HitContext hit = default,
            float overrideDownDuration = 0f,
            float knockbackDistance = 0f,
            float knockbackDuration = 0f,
            float maxKnockbackSpeed = 0f,
            Transform knockbackSource = null) : base(controller)
        {
            _hit = hit;
            _overrideDownDuration = Mathf.Max(0f, overrideDownDuration);
            _knockbackDistance = Mathf.Max(0f, knockbackDistance);
            _knockbackDuration = Mathf.Max(0f, knockbackDuration);
            _maxKnockbackSpeed = Mathf.Max(0f, maxKnockbackSpeed);
            _knockbackSource = knockbackSource;
        }

        public override bool CanTransitionState(ActorStateId fromState)
            => fromState is not (ActorStateId.Death
                or ActorStateId.Grabbed
                or ActorStateId.SpecialBreakVictim);

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            controller.MotionWarp?.ClearTarget();
            _isActive = true;
            _wallImpactConsumed = false;
            _getupStarted = false;
            _knockdownMotionEnded = false;
            _knockbackElapsed = 0f;
            _knockdownMotionTimer = 0f;
            _knockdownMotionTimeout = FALLBACK_MOTION_TIMEOUT;
            _getupMotionTimer = 0f;
            _getupMotionTimeout = FALLBACK_MOTION_TIMEOUT;
            _knockdownMotionSet = null;
            _getupMotionSet = null;
            _knockbackDirection = ResolveKnockbackDirection();
            _downTimer = _overrideDownDuration > 0f
                ? _overrideDownDuration
                : (_hit.ReactionDuration > 0f ? _hit.ReactionDuration : 1.0f);

            UPlayGround.Gameplay.Tag.GameplayTag animKey = gameActor.Animator.HasMotion(UPlayGround.Data.Actor.Animation.MotionTags.Knockdown, true)
                ? UPlayGround.Data.Actor.Animation.MotionTags.Knockdown
                : UPlayGround.Data.Actor.Animation.MotionTags.Knockback;
            var state = gameActor.Animator.PlayMotion(animKey, 0.1f);
            if (state != null)
            {
                _knockdownMotionSet = gameActor.Animator.CurrentMotionSet;
                gameActor.Animator.OnMotionSetEndedWithReason += OnMotionSetEnded;
                _knockdownMotionTimeout = ResolveMotionTimeout(_knockdownMotionSet);
            }
            else
                _knockdownMotionEnded = true;
        }

        public override void OnExit(GameActorState toState)
        {
            gameActor.Animator.OnMotionSetEndedWithReason -= OnMotionSetEnded;
            _isActive = false;
            _knockdownMotionSet = null;
            _getupMotionSet = null;
            base.OnExit(toState);
        }

        public override void UpdateState(float deltaTime)
        {
            if (!_isActive || controller.CurrentState != this)
                return;

            if (_getupStarted)
            {
                _getupMotionTimer += deltaTime;
                if (_getupMotionSet != null && _getupMotionTimer >= _getupMotionTimeout)
                {
                    Debug.LogWarning(
                        $"[EnemyKnockdownState] 기상 Motion 종료 신호가 없어 강제 복귀합니다. " +
                        $"actor={gameActor.name}, timeout={_getupMotionTimeout:0.00}s",
                        gameActor);
                    TransitionOut();
                }

                return;
            }

            _downTimer -= deltaTime;

            if (!_knockdownMotionEnded)
            {
                _knockdownMotionTimer += deltaTime;
                if (_knockdownMotionTimer >= _knockdownMotionTimeout)
                {
                    Debug.LogWarning(
                        $"[EnemyKnockdownState] 쓰러짐 Motion 종료 신호가 없어 대기 단계를 계속합니다. " +
                        $"actor={gameActor.name}, timeout={_knockdownMotionTimeout:0.00}s",
                        gameActor);
                    _knockdownMotionEnded = true;
                    _knockdownMotionSet = null;
                }
            }

            if (_downTimer <= 0f && _knockdownMotionEnded)
                BeginGetup();
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            currentRotation = currentRotation.normalized;
        }

        /// <summary>넉백으로 밀려나던 중 벽에 부딪힌 경우(환경 넉백 T0). 한 넉백당 1회만 소비한다.</summary>
        public override void OnMovementHit(
            Collider hitCollider,
            Vector3 hitNormal,
            Vector3 hitPoint,
            ref KinematicCharacterController.HitStabilityReport hitStabilityReport)
        {
            if (_wallImpactConsumed)
                return;

            if (WallImpactResolver.TryApplyWallImpact(
                    controller,
                    _hit.ReactionType,
                    hitCollider,
                    hitNormal,
                    hitPoint,
                    hitStabilityReport))
            {
                _wallImpactConsumed = true;

                // 이 상태의 넉백은 damper가 아니라 UpdateVelocity가 매 프레임 직접 덮어쓴다.
                // 구동 자체를 끝내지 않으면 리졸버가 속도를 눌러도 다음 프레임에 되살아나
                // 벽에 박힌 채 남은 시간 동안 계속 밀린다.
                CancelKnockbackDrive();
            }
        }

        /// <summary>상태가 구동하던 넉백을 즉시 종료한다(벽 충돌 등 외부 중단).</summary>
        private void CancelKnockbackDrive()
        {
            _knockbackElapsed = _knockbackDuration;
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            // 진입 직후 일정 시간 동안 공격자 반대 방향으로 날아간다(브레이크 마무리). 거리는 선형 감속으로 소진.
            if (CanApplyKnockback())
            {
                float verticalVelocity = currentVelocity.y;
                _knockbackElapsed += deltaTime;
                float speed = Mathf.Min(
                    _maxKnockbackSpeed,
                    2f * _knockbackDistance / Mathf.Max(0.01f, _knockbackDuration));
                float ratio = 1f - Mathf.Clamp01(_knockbackElapsed / _knockbackDuration);
                currentVelocity = _knockbackDirection * (speed * ratio);
                currentVelocity.y = verticalVelocity;
                return;
            }

            if (!motor.GroundingStatus.IsStableOnGround) return;
            currentVelocity = Vector3.Lerp(
                currentVelocity,
                Vector3.zero,
                1f - Mathf.Exp(controller.StableMovementSharpness * -deltaTime));
        }

        private bool CanApplyKnockback()
        {
            return _knockbackDistance > 0f
                   && _knockbackDuration > 0f
                   && _maxKnockbackSpeed > 0f
                   && _knockbackElapsed < _knockbackDuration
                   && motor.GroundingStatus.IsStableOnGround;
        }

        private Vector3 ResolveKnockbackDirection()
        {
            Vector3 direction = _knockbackSource != null
                ? gameActor.transform.position - _knockbackSource.position
                : -gameActor.transform.forward;
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.001f)
                direction = -gameActor.transform.forward;

            direction.y = 0f;
            return direction.sqrMagnitude > 0.001f
                ? direction.normalized
                : Vector3.back;
        }

        private void OnMotionSetEnded(MotionSet motionSet, MotionSetEndReason _)
        {
            if (_knockdownMotionSet != null && ReferenceEquals(motionSet, _knockdownMotionSet))
            {
                _knockdownMotionEnded = true;
                _knockdownMotionSet = null;
                return;
            }

            if (_getupMotionSet != null && ReferenceEquals(motionSet, _getupMotionSet))
            {
                _getupMotionSet = null;
                TransitionOut();
            }
        }

        private void BeginGetup()
        {
            if (_getupStarted) return;
            _getupStarted = true;

            if (gameActor.Animator.HasMotion(UPlayGround.Data.Actor.Animation.MotionTags.Knockdown_Getup, true))
            {
                var state = gameActor.Animator.PlayMotion(UPlayGround.Data.Actor.Animation.MotionTags.Knockdown_Getup, 0.1f);
                if (state != null)
                {
                    _getupMotionSet = gameActor.Animator.CurrentMotionSet;
                    _getupMotionTimer = 0f;
                    _getupMotionTimeout = ResolveMotionTimeout(_getupMotionSet);
                    return;
                }
            }

            TransitionOut();
        }

        private void TransitionOut()
        {
            if (!_isActive || controller.CurrentState != this)
                return;

            _isActive = false;
            gameActor.Animator.OnMotionSetEndedWithReason -= OnMotionSetEnded;
            controller.TransitionToState(ActorStateId.Idle);
        }

        private static float ResolveMotionTimeout(MotionSet motionSet)
        {
            float motionDuration = motionSet?.TotalDuration ?? 0f;
            return motionDuration > 0f
                ? Mathf.Max(
                    FALLBACK_MOTION_TIMEOUT,
                    motionDuration / MINIMUM_PLAY_RATE + MOTION_COMPLETION_GRACE)
                : FALLBACK_MOTION_TIMEOUT;
        }
    }
}
