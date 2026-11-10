using UnityEngine;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>지상 대기 중 수평 속도를 감속하고 공중 수직 속도는 보존한다.</summary>
    public class PetIdleState : GameActorState
    {
        protected readonly PetMovementController PetController;
        public PetIdleState(PetMovementController controller) : base(controller) => PetController = controller;
        public override ActorStateId StateId => ActorStateId.Idle;
        public override bool CanTransitionState(ActorStateId fromState) => true;
        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            gameActor.Animator?.PlayMotion(MotionTags.Idle);
        }
        public override void UpdateVelocity(ref Vector3 velocity, float deltaTime)
        {
            PetController.RefreshDesiredVelocity();
            Vector3 vertical = motor.GroundingStatus.IsStableOnGround ? Vector3.zero : Vector3.Project(velocity, motor.CharacterUp);
            velocity = Vector3.MoveTowards(Vector3.ProjectOnPlane(velocity, motor.CharacterUp),
                PetController.DesiredVelocity, PetController.Profile.acceleration * deltaTime) + vertical;
        }
        public override void UpdateRotation(ref Quaternion rotation, float deltaTime)
        {
            PetController.RefreshDesiredVelocity();
            if (PetController.DesiredVelocity.sqrMagnitude < 0.001f) return;
            rotation = Quaternion.RotateTowards(rotation,
                Quaternion.LookRotation(PetController.DesiredVelocity, motor.CharacterUp),
                PetController.Profile.rotationSpeed * deltaTime);
        }
    }
}
