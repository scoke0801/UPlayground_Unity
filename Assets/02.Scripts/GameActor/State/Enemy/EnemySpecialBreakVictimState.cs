using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    public class EnemySpecialBreakVictimState : EnemyActorState
    {
        public override string StateName => "SpecialBreakVictim";
        public override bool BlocksBehaviorTree => true;

        private readonly float _duration;
        private readonly Transform _source;
        private readonly float _knockbackDistance;
        private readonly float _knockbackDuration;
        private readonly float _maxKnockbackSpeed;
        private float _remainingDuration;
        private float _knockbackElapsed;
        private Vector3 _knockbackDirection;

        public EnemySpecialBreakVictimState(
            ActorMovementController controller,
            float duration = 1.2f,
            Transform source = null,
            float knockbackDistance = 0.75f,
            float knockbackDuration = 0.18f,
            float maxKnockbackSpeed = 7f) : base(controller)
        {
            _duration = Mathf.Max(0.1f, duration);
            _source = source;
            _knockbackDistance = Mathf.Max(0f, knockbackDistance);
            _knockbackDuration = Mathf.Max(0f, knockbackDuration);
            _maxKnockbackSpeed = Mathf.Max(0f, maxKnockbackSpeed);
        }

        public override bool CanTransitionState(string stateName) => stateName is "Death";

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            controller.MotionWarp?.ClearTarget();
            _remainingDuration = _duration;
            _knockbackElapsed = 0f;
            _knockbackDirection = ResolveKnockbackDirection();

            AnimKey animKey = gameActor.Animator.HasMotion(AnimKey.Grabbed, true)
                ? AnimKey.Grabbed
                : AnimKey.Hit_F;
            gameActor.Animator.PlayMotion(animKey, 0.1f);
        }

        public override void UpdateState(float deltaTime)
        {
            _remainingDuration -= deltaTime;
            if (_remainingDuration <= 0f)
                controller.TransitionToState(new EnemyIdleState(controller));
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            currentRotation = currentRotation.normalized;
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            float verticalVelocity = currentVelocity.y;

            if (CanApplyKnockback())
            {
                _knockbackElapsed += deltaTime;
                float speed = Mathf.Min(_maxKnockbackSpeed, 2f * _knockbackDistance / Mathf.Max(0.01f, _knockbackDuration));
                float ratio = 1f - Mathf.Clamp01(_knockbackElapsed / _knockbackDuration);
                currentVelocity = _knockbackDirection * (speed * ratio);
                currentVelocity.y = verticalVelocity;
                return;
            }

            if (!motor.GroundingStatus.IsStableOnGround)
            {
                currentVelocity += controller.Gravity * deltaTime;
                return;
            }

            currentVelocity = Vector3.Lerp(
                currentVelocity,
                Vector3.zero,
                1f - Mathf.Exp(-controller.StableMovementSharpness * deltaTime));
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
            Vector3 direction = _source != null
                ? gameActor.transform.position - _source.position
                : -gameActor.transform.forward;
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.001f)
                direction = -gameActor.transform.forward;

            direction.y = 0f;
            return direction.sqrMagnitude > 0.001f
                ? direction.normalized
                : Vector3.back;
        }
    }
}
