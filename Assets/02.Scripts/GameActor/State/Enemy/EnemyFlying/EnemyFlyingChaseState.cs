using UnityEngine;
using UPlayGround.Component;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 비행 보스 지상 추격.
    /// 기존 EnemyChaseState와 유사하나, Brain 판단을 FlyingBrain에 위임.
    /// </summary>
    public class EnemyFlyingChaseState : GameActorState
    {
        public override string StateName => "Flying_Chase";

        private readonly EnemyFlyingBrain _brain;
        private float _chaseSpeed;

        public EnemyFlyingChaseState(ActorMovementController controller, EnemyFlyingBrain brain)
            : base(controller)
        {
            _brain = brain;
        }

        public override bool CanTransitionState(string stateName) => true;

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            motor.SetGroundSolvingActivation(true); // 공중 State에서 꺼졌을 수 있으므로 복구
            _chaseSpeed = controller.MaxRunMoveSpeed * _brain.ChaseSpeedMultiplier;
            gameActor.Animator.PlayMotion(AnimKey.Run, 0.25f);
        }

        public override void UpdateState(float deltaTime)
        {
            if (!motor.GroundingStatus.IsStableOnGround)
            {
                controller.TransitionToState(new EnemyAirborneState(controller));
                return;
            }

            if (!_brain.Detection.HasTarget)
            {
                controller.TransitionToState(new EnemyIdleState(controller));
                return;
            }

            // Brain에 판단 위임 (거리 진입 → 공격, 시간 초과 → 이륙)
            _brain.EvaluateChase();
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            if (!_brain.Detection.HasTarget) return;

            Vector3 dir = (_brain.Detection.CurrentTarget.position - motor.TransientPosition);
            dir.y = 0;
            if (dir.sqrMagnitude < 0.01f) return;

            Quaternion target = Quaternion.LookRotation(dir.normalized);
            currentRotation = Quaternion.Slerp(
                currentRotation, target,
                1 - Mathf.Exp(-controller.OrientationSharpness * deltaTime));
            currentRotation = currentRotation.normalized;
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (!_brain.Detection.HasTarget || !motor.GroundingStatus.IsStableOnGround)
            {
                currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero,
                    1 - Mathf.Exp(-controller.StableMovementSharpness * deltaTime));
                return;
            }

            Vector3 toTarget = _brain.Detection.CurrentTarget.position - motor.TransientPosition;
            toTarget.y = 0;
            float dist = toTarget.magnitude;

            if (dist <= _brain.ChaseStopDistance)
            {
                currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero,
                    1 - Mathf.Exp(-controller.StableMovementSharpness * deltaTime));
                return;
            }

            Vector3 targetVel = toTarget.normalized * _chaseSpeed;
            targetVel = motor.GetDirectionTangentToSurface(targetVel, motor.GroundingStatus.GroundNormal)
                        * targetVel.magnitude;

            currentVelocity = Vector3.Lerp(currentVelocity, targetVel,
                1 - Mathf.Exp(-controller.StableMovementSharpness * deltaTime));
        }
    }
}
