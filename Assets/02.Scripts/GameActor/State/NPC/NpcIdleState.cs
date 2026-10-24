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

        private float _wanderDelayTimer;
        private float _wanderDelay;
        private bool _isWorking;

        public NpcIdleState(NpcMovementController controller) : base(controller) { }

        public override bool CanTransitionState(ActorStateId fromState) => true;

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            _brain = npcActor.GetComponent<NpcBrain>();
            _wanderDelayTimer = 0f;
            _wanderDelay = _brain != null ? _brain.SamplePatrolWaitTime() : 0f;
            _brain?.BeginIdleLook(_brain.EnableWander ? motor.TransientRotation : _brain.HomeRotation);
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

            if (npcController.Routine != null && npcController.Routine.CanRun
                && npcController.Routine.TryStart())
            {
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

            // 일과 예약을 기다리는 동안도 대기 행동은 유지하되 이동 소유권은 넘기지 않는다.
            _brain?.TickIdleLook(deltaTime);
            if (npcController.Routine != null && npcController.Routine.HasMovementControl) return;
            if (_brain == null || !_brain.EnableWander) return;

            _wanderDelayTimer += deltaTime;
            if (_wanderDelayTimer >= _wanderDelay)
                npcController.TransitionToState(new NpcWanderState(npcController, _brain));
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            if (npcActor.IsRoutinePaused || npcActor.IsInteracting() || npcActor.IsDialogueStaged) return;
            if (_brain != null && !_brain.EnableWander && _brain.WorkMotion != null)
                currentRotation = Quaternion.Slerp(currentRotation, _brain.HomeRotation,
                    1f - Mathf.Exp(-npcController.OrientationSharpness * deltaTime));
            else if (_brain != null)
                currentRotation = _brain.RotateIdleLook(currentRotation, deltaTime);
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
