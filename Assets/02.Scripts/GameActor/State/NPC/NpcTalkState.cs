using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// NPC 대화 중 상태.
    /// - 플레이어 방향으로 부드럽게 회전
    /// - 대화 종료 시 자동으로 IdleState 복귀
    /// </summary>
    public class NpcTalkState : NpcActorState
    {
        public override ActorStateId StateId => ActorStateId.Talk;

        public NpcTalkState(NpcMovementController controller) : base(controller) { }

        public override bool CanTransitionState(ActorStateId fromState) => true;

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            
            
            gameActor.Animator.PlayMotion(UPlayGround.Data.Actor.Animation.MotionTags.Talk_1, 0.25f);
        }

        public override void UpdateState(float deltaTime)
        {
            // 상호작용 대화와 연출 홀드(FlowGraph·스토리 대화)가 모두 풀리면 Idle로 복귀
            if (!npcActor.IsInteracting() && !npcActor.IsDialogueStaged)
            {
                npcController.TransitionToState(new NpcIdleState(npcController));
            }
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            // 대화 상대를 향해 부드럽게 회전. 3인 이상 대화에서는 홀드가 지정한 상대가 플레이어가 아니다.
            Transform lookTarget = ResolveLookTarget();
            if (lookTarget == null) return;

            Vector3 lookDir = lookTarget.position - npcActor.transform.position;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude < 0.001f) return;

            Vector3 smoothed = Vector3.Slerp(
                motor.CharacterForward,
                lookDir.normalized,
                1 - Mathf.Exp(-npcController.OrientationSharpness * deltaTime));

            currentRotation = Quaternion.LookRotation(smoothed, motor.CharacterUp);
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            currentVelocity = Vector3.zero;
        }

        /// <summary>홀드가 지정한 상대를 우선하고, 없으면 플레이어를 본다.</summary>
        private Transform ResolveLookTarget()
        {
            Transform staged = npcActor.DialogueStageLookTarget;
            if (staged != null)
                return staged;

            var player = UPlayGround.Manager.ActorSvc.Objects.Player;
            return player != null ? player.transform : null;
        }
    }
}
