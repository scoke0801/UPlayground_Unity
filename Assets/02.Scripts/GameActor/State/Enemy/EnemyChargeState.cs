using UnityEngine;
using UPlayGround.Components;
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
    public class EnemyChargeState : EnemyActorState
    {
        public override ActorStateId StateId => ActorStateId.Charge;
        public override bool BlocksBehaviorTree => true;
        public override GravityOwnership GravityOwner => GravityOwnership.State;

        private readonly EnemyCombat _combat;
        private readonly EnemyAIContext _context;
        private readonly EnemyDetection _detection;
        private readonly EnemyTacticalMemory _memory;

        private float _chargeSpeed;
        private float _chargeTimer;
        private bool _hasReachedTarget;

        // 돌진 중 진행 방향 고정 (시작 시 스냅)
        private Vector3 _chargeDirection;

        // 오버슈트 가드: 시작 위치와 돌진해야 할 거리(진입 시점 타겟까지)를 스냅
        private Vector3 _startPosition;
        private float _chargeDistance;

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

        public override bool CanTransitionState(ActorStateId fromState) => true;

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            _chargeTimer      = 0f;
            _hasReachedTarget = false;
            _chargeSpeed      = controller.MaxRunMoveSpeed * CHARGE_SPEED_RATIO;

            // 돌진 방향은 진입 시점에 고정 (중간에 꺾이지 않아야 위협적)
            _startPosition = motor.TransientPosition;
            if (_detection.HasTarget)
            {
                Vector3 toTarget = _detection.CurrentTarget.position - motor.TransientPosition;
                toTarget.y = 0;
                _chargeDirection = toTarget.normalized;
                // 진입 시점 타겟까지의 거리를 스냅 → 이 지점을 통과하면 오버슈트로 간주
                _chargeDistance  = toTarget.magnitude;
            }
            else
            {
                _chargeDirection = motor.CharacterForward;
                _chargeDistance  = _chargeSpeed * MAX_CHARGE_DURATION;
            }

            // Run 애니를 빠른 속도로 재생 → 체감상 돌진처럼 보임
            gameActor.Animator.PlayMotion(UPlayGround.Data.Actor.Animation.MotionTags.Run, 0.1f);
        }

        public override void OnExit(GameActorState toState)
        {
            base.OnExit(toState);
        }

        public override void UpdateState(float deltaTime)
        {
            if (ShouldTransitionToAirborne(deltaTime))
            {
                controller.TransitionToState(
                    ActorStateId.Airborne,
                    EnemyAirborneContext.Natural);
                return;
            }

            _chargeTimer += deltaTime;

            float distance = _detection.HasTarget ? _detection.DistanceToTarget : float.MaxValue;

            // 타겟 도달 → 즉시 공격
            if (distance <= REACH_DISTANCE && !_hasReachedTarget)
            {
                _hasReachedTarget = true;
                // 캐릭터별 Ability 최소/최대 거리는 고정 REACH_DISTANCE와 다를 수 있다.
                // 실제 실행 가능한 공격이 없으면 빈 Attack 상태로 진입하지 않고 추격을 이어간다.
                if (_combat != null && _combat.HasAvailableSkillAtDistance(distance))
                {
                    controller.TransitionToState(
                        new EnemyAttackState(controller, _combat, _context, _detection));
                }
                else
                {
                    controller.TransitionToState(
                        ActorStateId.Chase,
                        EnemyChaseContext.Default);
                }
                return;
            }

            // 오버슈트 가드: 진입 시점 타겟 위치까지 이미 도달(통과)했는데도
            // REACH 안에 못 들었다면 플레이어가 피한 것 → 더 직진하지 말고 추격으로 전환
            float traveled = Vector3.Dot(motor.TransientPosition - _startPosition, _chargeDirection);
            if (traveled >= _chargeDistance && !_hasReachedTarget)
            {
                _memory?.NotifyAttackMissed();
                controller.TransitionToState(
                    ActorStateId.Chase,
                    EnemyChaseContext.Default);
                return;
            }

            // 타임아웃 → 타겟을 놓침, 추격으로 전환
            if (_chargeTimer >= MAX_CHARGE_DURATION)
            {
                _memory?.NotifyAttackMissed();
                controller.TransitionToState(
                    ActorStateId.Chase,
                    EnemyChaseContext.Default);
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
                controller.TransitionToState(
                    ActorStateId.Chase,
                    EnemyChaseContext.Default);
            }
        }
    }
}
