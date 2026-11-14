using UnityEngine;
using UPlayGround.MovementController;
using UPlayGround.Data.Actor.Animation;

namespace UPlayGround.State
{
    /// <summary>체력 초기화나 순간이동 없이 귀속 지점으로 복귀한다.</summary>
    public sealed class EnemyReturnHomeState : EnemyActorState
    {
        private readonly EnemyMovementController _movement;
        private bool _isMoving;
        public override ActorStateId StateId => ActorStateId.ReturnHome;
        public override bool BlocksBehaviorTree => true;
        public override GravityOwnership GravityOwner => _movement.IsFlyingCombatant && !motor.GroundingStatus.IsStableOnGround
            ? GravityOwnership.State : GravityOwnership.Controller;

        public EnemyReturnHomeState(EnemyMovementController controller) : base(controller) { _movement = controller; }

        public override bool CanTransitionState(ActorStateId fromState) => true;

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            _movement.NotifyCombatReturnStarted();
            motor.SetGroundSolvingActivation(true);
            _isMoving = false;
            gameActor.Animator?.PlayMotion(MotionTags.Idle, 0.15f);
        }

        public override void UpdateState(float deltaTime)
        {
            if (_movement.IsFlyingCombatant && !motor.GroundingStatus.IsStableOnGround
                && EnemyCombatMovementPolicy.PlanarDistance(motor.TransientPosition, _movement.CombatHome)
                    <= _movement.CombatMovementSettings.returnArrivalDistance)
            {
                controller.TryTransitionToState(ActorStateId.Flying_Land);
                return;
            }
            if (_movement.IsReturningHome) return;
            controller.TryTransitionToState(_movement.IsFlyingCombatant && !motor.GroundingStatus.IsStableOnGround
                ? ActorStateId.Flying_Land : ActorStateId.Idle);
        }

        public override void UpdateRotation(ref Quaternion rotation, float deltaTime)
        {
            Vector3 direction = _movement.CombatHome - motor.TransientPosition;
            direction.y = 0f;
            if (direction.sqrMagnitude > Mathf.Epsilon)
                rotation = Quaternion.Slerp(rotation, Quaternion.LookRotation(direction), 1f - Mathf.Exp(-controller.OrientationSharpness * deltaTime));
        }

        #if UNITY_EDITOR
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        #endif
        public override void UpdateVelocity(ref Vector3 velocity, float deltaTime)
        {
            Vector3 direction = _movement.GetCombatReturnDirection();
            bool moving = direction.sqrMagnitude > Mathf.Epsilon;
            if (moving != _isMoving)
            {
                _isMoving = moving;
                var key = moving ? MotionTags.Run : MotionTags.Idle;
                if (gameActor.Animator != null && gameActor.Animator.HasMotion(key)) gameActor.Animator.PlayMotion(key, 0.15f);
            }
            Vector3 desired = direction * controller.MaxRunMoveSpeed;
            if (motor.GroundingStatus.IsStableOnGround)
                velocity = motor.GetDirectionTangentToSurface(desired, motor.GroundingStatus.GroundNormal) * desired.magnitude;
            else
                velocity = new Vector3(desired.x, _movement.IsFlyingCombatant ? 0f : velocity.y, desired.z);
        }
    }
}
