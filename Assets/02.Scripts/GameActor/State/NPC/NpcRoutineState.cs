using UnityEngine;
using UPlayGround.Components.LivingWorld;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>대화를 잠그지 않고 기존 KCC로 명시 경유점 사이를 걷는 일과 상태다.</summary>
    public sealed class NpcRoutineState : NpcActorState
    {
        private readonly NpcRoutine _routine;
        private bool _isWalking;
        public override ActorStateId StateId => ActorStateId.Wander;

        public NpcRoutineState(NpcMovementController controller, NpcRoutine routine) : base(controller) => _routine = routine;
        public override bool CanTransitionState(ActorStateId fromState) => true;

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            _isWalking = true;
            gameActor.Animator?.PlayMotion(UPlayGround.Data.Actor.Animation.MotionTags.Walk, _routine.FadeSeconds);
        }

        public override void UpdateState(float deltaTime)
        {
            if (npcActor.IsInteracting() || npcActor.IsDialogueStaged)
            { npcController.TransitionToState(new NpcTalkState(npcController)); return; }
            if (_routine == null || !_routine.Tick(deltaTime))
            { npcController.TransitionToState(new NpcIdleState(npcController)); return; }
            if (_routine.IsWorking) return;
            bool moving = motor.Velocity.sqrMagnitude >= EnemyLocomotionHelper.MIN_SPEED_SQ;
            if (moving == _isWalking) return;
            _isWalking = moving;
            gameActor.Animator?.PlayMotion(moving ? UPlayGround.Data.Actor.Animation.MotionTags.Walk
                : UPlayGround.Data.Actor.Animation.MotionTags.Idle, _routine.FadeSeconds);
        }

        public override void OnExit(GameActorState nextState)
        {
            _routine?.CancelRoutine();
            base.OnExit(nextState);
        }

        public override void UpdateRotation(ref Quaternion rotation, float deltaTime)
        {
            if (_routine == null || !_routine.CanRun || _routine.Target == null) return;
            Vector3 direction = Vector3.ProjectOnPlane(_routine.Target.position - motor.TransientPosition, motor.CharacterUp);
            Quaternion target = direction.magnitude <= _routine.ArrivalDistance
                ? _routine.Target.rotation : Quaternion.LookRotation(direction.normalized, motor.CharacterUp);
            rotation = Quaternion.Slerp(rotation, target, 1f - Mathf.Exp(-npcController.OrientationSharpness * deltaTime)).normalized;
        }

        public override void UpdateVelocity(ref Vector3 velocity, float deltaTime)
        {
            // 접지를 잃었을 때 컨트롤러가 누적한 중력을 다음 KCC 스텝에도 보존한다.
            velocity = motor.GroundingStatus.IsStableOnGround
                ? Vector3.zero : Vector3.Project(velocity, motor.CharacterUp);
            if (_routine == null || !_routine.CanRun || _routine.IsWorking || _routine.Target == null
                || !motor.GroundingStatus.IsStableOnGround) return;
            Vector3 offset = Vector3.ProjectOnPlane(_routine.Target.position - motor.TransientPosition, motor.CharacterUp);
            if (offset.magnitude <= _routine.ArrivalDistance) return;
            Vector3 direction = motor.GetDirectionTangentToSurface(offset.normalized, motor.GroundingStatus.GroundNormal);
            velocity = direction * Mathf.Min(npcController.MaxWalkMoveSpeed * _routine.SpeedMultiplier,
                offset.magnitude / Mathf.Max(deltaTime, 0.0001f));
        }

        public override void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
            ref KinematicCharacterController.HitStabilityReport hitStabilityReport)
        {
            if (_routine == null || _routine.IsWorking || _routine.Target == null || hitStabilityReport.IsStable) return;
            // KCC의 가장자리 판정은 걸을 수 있는 지형도 일시적으로 불안정하다고 보고한다.
            // 지면 법선까지 벽으로 취급하면 완만한 흙길에서 일과가 즉시 취소된다.
            if (Vector3.Dot(hitNormal, motor.CharacterUp) >= Mathf.Cos(motor.MaxStableSlopeAngle * Mathf.Deg2Rad)) return;
            Vector3 direction = Vector3.ProjectOnPlane(_routine.Target.position - motor.TransientPosition, motor.CharacterUp);
            if (Vector3.Dot(direction, hitNormal) < 0f) _routine.ReportBlocked();
        }
    }
}
