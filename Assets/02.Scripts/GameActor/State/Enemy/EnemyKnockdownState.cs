using UnityEngine;
using UPlayGround.Combat;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    public class EnemyKnockdownState : EnemyActorState
    {
        public override string StateName => "Knockdown";
        public override bool BlocksBehaviorTree => true;

        private readonly HitContext _hit;
        private readonly float _overrideDownDuration;
        private readonly float _knockbackDistance;
        private readonly float _knockbackDuration;
        private readonly float _maxKnockbackSpeed;
        private readonly Transform _knockbackSource;

        private bool _getupStarted;
        private bool _knockdownMotionEnded;
        private float _downTimer;
        private float _knockbackElapsed;
        private Vector3 _knockbackDirection;

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

        public override bool CanTransitionState(string stateName) => stateName is "Death" or "Grabbed";

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            controller.MotionWarp?.ClearTarget();
            _getupStarted = false;
            _knockdownMotionEnded = false;
            _knockbackElapsed = 0f;
            _knockbackDirection = ResolveKnockbackDirection();
            _downTimer = _overrideDownDuration > 0f
                ? _overrideDownDuration
                : (_hit.ReactionDuration > 0f ? _hit.ReactionDuration : 1.0f);

            AnimKey animKey = gameActor.Animator.HasMotion(AnimKey.Knockdown, true)
                ? AnimKey.Knockdown
                : AnimKey.Knockback;
            var state = gameActor.Animator.PlayMotion(animKey, 0.1f);
            if (state != null)
                state.OwnedEvents.OnEnd = OnKnockdownMotionEnd;
            else
                _knockdownMotionEnded = true;
        }

        public override void UpdateState(float deltaTime)
        {
            if (_getupStarted) return;

            _downTimer -= deltaTime;
            if (_downTimer <= 0f && _knockdownMotionEnded)
                BeginGetup();
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            currentRotation = currentRotation.normalized;
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

        private void OnKnockdownMotionEnd()
        {
            _knockdownMotionEnded = true;
        }

        private void BeginGetup()
        {
            if (_getupStarted) return;
            _getupStarted = true;

            if (gameActor.Animator.HasMotion(AnimKey.Knockdown_Getup, true))
            {
                var state = gameActor.Animator.PlayMotion(AnimKey.Knockdown_Getup, 0.1f);
                if (state != null)
                {
                    state.OwnedEvents.OnEnd = TransitionOut;
                    return;
                }
            }

            TransitionOut();
        }

        private void TransitionOut()
        {
            controller.TransitionToState(new EnemyIdleState(controller));
        }
    }
}
