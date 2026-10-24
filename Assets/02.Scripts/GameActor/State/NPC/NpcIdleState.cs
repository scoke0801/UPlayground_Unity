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
        public override ActorStateId StateId => ActorStateId.Idle;

        private NpcBrain _brain;

        // Wander 진입 전 최소 대기 시간 (배회 직후 바로 다시 배회하는 것 방지)
        private const float WANDER_DELAY = 1.5f;
        private float _wanderDelayTimer;
        private bool _isWorking;

        public NpcIdleState(NpcMovementController controller) : base(controller) { }

        public override bool CanTransitionState(ActorStateId fromState) => true;

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            _brain = npcActor.GetComponent<NpcBrain>();
            _wanderDelayTimer = 0f;
            _isWorking = false;
            gameActor.Animator.PlayMotion(UPlayGround.Data.Actor.Animation.MotionTags.Idle, 0.25f);
        }

        public override void UpdateState(float deltaTime)
        {
            if (npcActor.IsInteracting() || npcActor.IsDialogueStaged)
            {
                npcController.TransitionToState(new NpcTalkState(npcController));
                return;
            }

            if (npcActor.IsRoutinePaused) return;

            if (npcController.Routine != null && npcController.Routine.CanRun)
            {
                npcController.Routine.TryStart();
                return;
            }

            if (_brain != null && !_brain.EnableWander && _brain.WorkMotion != null)
            {
                // 상대를 바라본 채 손 작업을 재개하면 소품 밖을 집게 되므로 먼저 작업 방향을 회복한다.
                if (!_isWorking && Quaternion.Angle(motor.TransientRotation, _brain.HomeRotation) <= _brain.WorkFacingTolerance)
                {
                    gameActor.Animator.PlayMotion(_brain.WorkMotion, 0.25f);
                    _isWorking = true;
                }
                return;
            }

            // Brain이 없거나 배회 비활성이면 Idle 유지
            if (npcController.Routine != null && npcController.Routine.HasMovementControl) return;
            if (_brain == null || !_brain.EnableWander) return;

            _wanderDelayTimer += deltaTime;
            if (_wanderDelayTimer >= WANDER_DELAY)
                npcController.TransitionToState(new NpcWanderState(npcController, _brain));
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            if (npcController.Routine != null && npcController.Routine.CanRun) return;
            if (_brain != null && !_brain.EnableWander && _brain.WorkMotion != null)
                currentRotation = Quaternion.Slerp(currentRotation, _brain.HomeRotation,
                    1f - Mathf.Exp(-npcController.OrientationSharpness * deltaTime));
            currentRotation = currentRotation.normalized;
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            // 일과 취소 직후에도 컨트롤러가 누적한 낙하 속도를 유지한다.
            currentVelocity = motor.GroundingStatus.IsStableOnGround
                ? Vector3.zero : Vector3.Project(currentVelocity, motor.CharacterUp);
        }
    }
}
