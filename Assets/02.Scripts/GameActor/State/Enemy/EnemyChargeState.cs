using UnityEngine;
using UPlayGround.Component;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 돌진 상태
    /// Run 애니메이션을 재활용해 빠른 속도로 타겟에게 돌진 후 즉시 공격으로 전환한다.
    /// 플레이어가 반응할 시간이 짧아 회피를 요구하는 패턴이 된다.
    /// 
    /// 사용 조건 (EnemyAIController이 판단):
    ///   - 타겟이 일정 거리 이상 떨어져 있을 때 (먼 거리에서 갑자기 쇄도)
    ///   - 플레이어가 자주 회피해서 일반 추격으로 따라잡기 어려울 때
    /// </summary>
    public class EnemyChargeState : GameActorState
    {
        public override string StateName => "Charge";
        public override bool BlocksBehaviorTree => true;

        private readonly EnemyCombat _combat;
        private readonly EnemyAIContext _context;
        private readonly EnemyDetection _detection;
        private readonly EnemyTacticalMemory _memory;

        private float _chargeSpeed;
        private float _chargeTimer;
        private bool _hasReachedTarget;

        // 돌진 중 진행 방향 고정 (시작 시 스냅)
        private Vector3 _chargeDirection;

        private const float CHARGE_SPEED_RATIO  = 2.2f;   // MaxRunSpeed 대비 배율
        private const float MAX_CHARGE_DURATION = 1.2f;   // 최대 돌진 시간
        private const float REACH_DISTANCE      = 1.8f;   // 타겟 도달로 판단할 거리

        public EnemyChargeState(
            ActorMovementController controller,
            EnemyCombat combat,
            EnemyAIContext context,
            EnemyDetection detection,
            EnemyTacticalMemory memory) : base(controller)
        {
            _combat    = combat;
            _context   = context;
            _detection = detection;
            _memory    = memory;
        }

        public override bool CanTransitionState(string stateName) => true;

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            _chargeTimer      = 0f;
            _hasReachedTarget = false;
            _chargeSpeed      = controller.MaxRunMoveSpeed * CHARGE_SPEED_RATIO;

            // 돌진 방향은 진입 시점에 고정 (중간에 꺾이지 않아야 위협적)
            if (_detection.HasTarget)
            {
                _chargeDirection = (_detection.CurrentTarget.position - motor.TransientPosition).normalized;
                _chargeDirection.y = 0;
            }
            else
            {
                _chargeDirection = motor.CharacterForward;
            }

            // Run 애니를 빠른 속도로 재생 → 체감상 돌진처럼 보임
            gameActor.Animator.PlayMotion(AnimKey.Run, 0.1f);
        }

        public override void OnExit(GameActorState toState)
        {
            base.OnExit(toState);
        }

        public override void UpdateState(float deltaTime)
        {
            if (!motor.GroundingStatus.IsStableOnGround)
            {
                controller.TransitionToState(new EnemyAirborneState(controller));
                return;
            }

            _chargeTimer += deltaTime;

            float distance = _detection.HasTarget ? _detection.DistanceToTarget : float.MaxValue;

            // 타겟 도달 → 즉시 공격
            if (distance <= REACH_DISTANCE && !_hasReachedTarget)
            {
                _hasReachedTarget = true;
                controller.TransitionToState(
                    new EnemyAttackState(controller, _combat, _context, _detection));
                return;
            }

            // 타임아웃 → 타겟을 놓침, 추격으로 전환
            if (_chargeTimer >= MAX_CHARGE_DURATION)
            {
                _memory?.NotifyAttackMissed();
                controller.TransitionToState(
                    new EnemyChaseState(controller, _context, _detection));
            }
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            // 돌진 중에는 고정 방향 유지 (급격한 방향 전환 없음)
            if (_chargeDirection.sqrMagnitude > 0.01f)
            {
                Quaternion target = Quaternion.LookRotation(_chargeDirection);
                currentRotation = Quaternion.Slerp(
                    currentRotation,
                    target,
                    1 - Mathf.Exp(-controller.OrientationSharpness * 0.3f));  // 느린 회전으로 관성 표현
            }
            currentRotation = currentRotation.normalized;
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            float lastY = currentVelocity.y;

            if (motor.GroundingStatus.IsStableOnGround && !_hasReachedTarget)
            {
                Vector3 targetVelocity = _chargeDirection * _chargeSpeed;
                targetVelocity = motor.GetDirectionTangentToSurface(
                    targetVelocity, motor.GroundingStatus.GroundNormal) * targetVelocity.magnitude;

                // 순간 가속 (빠르게 속도에 도달)
                currentVelocity = Vector3.Lerp(
                    currentVelocity,
                    targetVelocity,
                    1 - Mathf.Exp(-controller.StableMovementSharpness * 2f * deltaTime));
            }

            currentVelocity.y = lastY;
            if (motor.GroundingStatus.IsStableOnGround)
            {
                if (currentVelocity.y < 0) currentVelocity.y = -0.1f;
            }
            else
            {
                currentVelocity += controller.Gravity * deltaTime;
            }
        }

        public override void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
            ref KinematicCharacterController.HitStabilityReport hitStabilityReport)
        {
            // 벽에 충돌하면 돌진 실패 → 추격으로
            Vector3 chargeDir = _chargeDirection;
            chargeDir.y = 0;
            float dot = Vector3.Dot(chargeDir, hitNormal);

            if (dot < -0.5f)
            {
                _memory?.NotifyAttackMissed();
                controller.TransitionToState(new EnemyChaseState(controller, _context, _detection));
            }
        }
    }
}
