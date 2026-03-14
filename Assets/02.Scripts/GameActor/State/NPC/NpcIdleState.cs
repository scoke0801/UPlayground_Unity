using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// NPC 기본 대기 상태.
    /// 대화 중이 아닐 때 항상 이 상태로 돌아옵니다.
    /// </summary>
    public class NpcIdleState : NpcActorState
    {
        public override string StateName => "Idle";

        public NpcIdleState(NpcMovementController controller) : base(controller) { }

        public override bool CanTransitionState(string stateName) => true;

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            gameActor.Animator.PlayMotion(AnimKey.Idle, 0.25f);
        }

        public override void UpdateState(float deltaTime)
        {
            // 대화가 시작되면 TalkState로 전환
            if (npcActor.IsInteracting())
            {
                npcController.TransitionToState(new NpcTalkState(npcController));
            }
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            currentRotation = currentRotation.normalized;
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            // NPC는 Idle 중 제자리 고정
            currentVelocity = Vector3.zero;
        }
    }
}
