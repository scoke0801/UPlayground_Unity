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
        public override string StateName => "Talk";

        public NpcTalkState(NpcMovementController controller) : base(controller) { }

        public override bool CanTransitionState(string stateName) => true;

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            
            
            gameActor.Animator.PlayMotion(AnimKey.Talk_1, 0.25f);
        }

        public override void UpdateState(float deltaTime)
        {
            // 대화가 끝나면 Idle로 복귀
            if (!npcActor.IsInteracting())
            {
                npcController.TransitionToState(new NpcIdleState(npcController));
            }
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            // 대화 상대(플레이어)를 향해 부드럽게 회전
            var player = UPlayGround.Manager.GameObjectManager.Instance.Player;
            if (player == null) return;

            Vector3 lookDir = player.transform.position - npcActor.transform.position;
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
    }
}
