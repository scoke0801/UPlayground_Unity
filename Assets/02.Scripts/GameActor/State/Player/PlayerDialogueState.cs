using UnityEngine;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 대화 연출 홀드 동안 플레이어를 대화 자세로 붙잡는 상태.
    /// FlowGraph·스토리처럼 상호작용을 거치지 않고 시작된 대화에서도
    /// NPC와 같은 연출(이동 정지·대화 모션·상대 주시)을 보장한다.
    /// 홀드 소유자는 <see cref="PlayerActor"/>이며, 이 상태는 홀드가 풀리면 스스로 빠져나온다.
    /// </summary>
    public class PlayerDialogueState : PlayerActorState
    {
        /// <summary>직전 이동 모션에서 대화 모션으로 넘어가는 페이드 시간.</summary>
        private const float DialogueMotionFadeDuration = 0.25f;

        public override GravityOwnership GravityOwner => GravityOwnership.State;

        public override ActorStateId StateId => ActorStateId.Dialogue;

        public PlayerDialogueState(ActorMovementController controller) : base(controller)
        {
        }

        public override bool CanTransitionState(ActorStateId fromState) => true;

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            PlayDialogueMotion(DialogueMotionFadeDuration);
        }

        public override void UpdateState(float deltaTime)
        {
            // 홀드 해제는 대화 계층이 결정한다. 여기서는 해제를 감지해 통상 상태로만 복귀한다.
            if (playerActor == null || !playerActor.IsDialogueStaged)
            {
                ForceChangeToNextState();
            }
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            SmoothLookAt(playerActor != null ? playerActor.DialogueStageLookTarget : null,
                ref currentRotation, deltaTime);
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            currentVelocity.x = 0f;
            currentVelocity.z = 0f;

            if (!motor.GroundingStatus.IsStableOnGround)
            {
                currentVelocity += controller.Gravity * deltaTime;
            }
        }

        private void ForceChangeToNextState()
        {
            if (!motor.GroundingStatus.IsStableOnGround)
            {
                playerController.TransitionToState(ActorStateId.Airborne);
                return;
            }

            // 대화 중 입력은 대화 UI가 막고 있으므로, 종료 직후 유지된 이동 입력만 이어받는다.
            if (playerController.HasMoveInput())
            {
                playerController.TransitionToState(ActorStateId.GroundMove);
            }
            else
            {
                playerController.TransitionToState(ActorStateId.Idle);
            }
        }
    }
}
