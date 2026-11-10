using UPlayGround.Data.Actor.Animation;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>이동 속도 구간이 바뀔 때만 걷기·달리기 모션을 전환한다.</summary>
    public sealed class PetMoveState : PetIdleState
    {
        private bool _isRunning;
        public PetMoveState(PetMovementController controller) : base(controller) { }
        public override ActorStateId StateId => ActorStateId.GroundMove;
        public override void OnEnter(GameActorState fromState) => PlayMovement();
        public override void UpdateState(float deltaTime)
        {
            bool running = PetController.DesiredVelocity.magnitude > PetController.Profile.walkSpeed;
            if (running != _isRunning) PlayMovement();
        }
        private void PlayMovement()
        {
            _isRunning = PetController.DesiredVelocity.magnitude > PetController.Profile.walkSpeed;
            gameActor.Animator?.PlayMotion(_isRunning ? MotionTags.Run : MotionTags.Walk);
        }
    }
}
