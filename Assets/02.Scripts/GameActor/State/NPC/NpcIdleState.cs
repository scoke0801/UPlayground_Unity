using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// NPC 기본 대기 상태.
    /// - NpcBrain.EnableWander == true 이면 잠시 후 WanderState로 전환
    /// - 대화 시작 시 TalkState로 전환
    /// </summary>
    public class NpcIdleState : NpcActorState
    {
        public override string StateName => "Idle";

        private NpcBrain _brain;

        // Wander 진입 전 최소 대기 시간 (배회 직후 바로 다시 배회하는 것 방지)
        private const float WANDER_DELAY = 1.5f;
        private float _wanderDelayTimer;

        public NpcIdleState(NpcMovementController controller) : base(controller) { }

        public override bool CanTransitionState(string stateName) => true;

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            _brain = npcActor.GetComponent<NpcBrain>();
            _wanderDelayTimer = 0f;
            gameActor.Animator.PlayMotion(UPlayGround.Data.Actor.Animation.MotionTags.Idle, 0.25f);
        }

        public override void UpdateState(float deltaTime)
        {
            if (npcActor.IsInteracting())
            {
                npcController.TransitionToState(new NpcTalkState(npcController));
                return;
            }

            // Brain이 없거나 배회 비활성이면 Idle 유지
            if (_brain == null || !_brain.EnableWander) return;

            _wanderDelayTimer += deltaTime;
            if (_wanderDelayTimer >= WANDER_DELAY)
                npcController.TransitionToState(new NpcWanderState(npcController, _brain));
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            currentRotation = currentRotation.normalized;
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            currentVelocity = Vector3.zero;
        }
    }
}
