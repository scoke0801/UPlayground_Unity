using UnityEngine;
using UPlayGround.Data.Actor.Animation;
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
        private bool _isStepping;

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
                return;
            }

            // 라인이 넘어가며 지정된 제스처를 이어받는다. 지정을 이벤트로 밀지 않고 여기서 확인하는 이유는,
            // 홀드가 상태 진입보다 먼저 걸릴 수 있어 밀어넣기 방식이면 진입 직전의 지정을 놓치기 때문이다.
            bool isStepping = playerActor.DialogueStepTarget != null;
            if (isStepping)
            {
                if (!_isStepping)
                {
                    var motion = playerActor.IsDialogueStepFacingMovement ? MotionTags.Walk : MotionTags.Walk_B;
                    if (!gameActor.Animator.HasMotion(motion))
                        motion = MotionTags.Run;
                    gameActor.Animator.PlayMotion(motion, DialogueMotionFadeDuration);
                }
            }
            else
            {
                if (_isStepping)
                    gameActor.Animator.PlayMotion(
                        UPlayGround.Animation.DialogueMotionPlayback.Resolve(gameActor.Animator, playerActor.DialogueMotionTag),
                        DialogueMotionFadeDuration);
                PlayDialogueMotion(DialogueGestureSwapFade);
            }
            _isStepping = isStepping;
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            Transform lookTarget = playerActor != null ? playerActor.DialogueStageLookTarget : null;
            if (playerActor != null && playerActor.DialogueStepTarget != null
                && playerActor.IsDialogueStepFacingMovement)
                lookTarget = playerActor.DialogueStepTarget;
            SmoothLookAt(lookTarget, ref currentRotation, deltaTime);
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            currentVelocity.x = 0f;
            currentVelocity.z = 0f;

            Transform target = playerActor.DialogueStepTarget;
            if (target != null)
            {
                Vector3 offset = Vector3.ProjectOnPlane(target.position - motor.TransientPosition, motor.CharacterUp);
                if (offset.magnitude > playerActor.DialogueStepStopDistance)
                {
                    Vector3 direction = offset.normalized;
                    if (motor.GroundingStatus.IsStableOnGround)
                        direction = motor.GetDirectionTangentToSurface(direction, motor.GroundingStatus.GroundNormal);
                    float speed = controller.MaxRunMoveSpeed * playerActor.DialogueStepSpeedMultiplier;
                    currentVelocity = direction * Mathf.Min(speed, offset.magnitude / Mathf.Max(deltaTime, 0.0001f));
                }
            }

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
