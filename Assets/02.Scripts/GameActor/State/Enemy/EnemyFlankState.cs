using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 측면 기동 상태
    /// 타겟의 측면 또는 후방으로 이동한 뒤 공격한다.
    /// Circle과 달리 '목적지를 정하고 최단 경로로 이동 → 즉시 공격' 패턴이다.
    /// 
    /// Walk/Run 애니를 그대로 쓰되, 이동 목적지 계산으로 행동을 차별화한다.
    /// 사용 조건:
    ///   - 일반 공격이 자주 빗나갈 때 (플레이어가 회피 잘함)
    ///   - 페이즈 2 이상에서 확률적으로 선택
    /// </summary>
    public class EnemyFlankState : EnemyActorState
    {
        public override string StateName => "Flank";
        public override bool BlocksBehaviorTree => true;

        private readonly EnemyCombat _combat;
        private readonly EnemyAIContext _context;
        private readonly EnemyDetection _detection;

        private Vector3 _flankTarget;
        private float _flankSpeed;
        private float _flankTimer;
        private bool _hasReachedFlank;
        private bool _usesFormationSlot;
        private UPlayGround.Gameplay.Tag.GameplayTag _lastLocoKey = default;

        private const float FLANK_SPEED_RATIO   = 1.1f;
        private const float ARRIVAL_THRESHOLD   = 1.2f;
        private const float MAX_FLANK_DURATION  = 2.5f;
        // 측면 목적지: 타겟으로부터 이 거리만큼 옆에 위치
        private const float FLANK_OFFSET_DIST   = 2.5f;

        public EnemyFlankState(
            ActorMovementController controller,
            EnemyCombat combat,
            EnemyAIContext context,
            EnemyDetection detection) : base(controller)
        {
            _combat    = combat;
            _context   = context;
            _detection = detection;
        }

        public override bool CanTransitionState(string stateName) => true;

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            _flankTimer      = 0f;
            _hasReachedFlank = false;
            _flankSpeed      = controller.MaxRunMoveSpeed * FLANK_SPEED_RATIO;

            CalculateFlankTarget();
            if (_context.TryGetFormationSlotPosition(FLANK_OFFSET_DIST, out var formationTarget))
            {
                _flankTarget = formationTarget;
                _usesFormationSlot = true;
            }
            _lastLocoKey = UPlayGround.Data.Actor.Animation.MotionTags.Run;
            gameActor.Animator.PlayMotion(UPlayGround.Data.Actor.Animation.MotionTags.Run, 0.2f);
        }

        public override void OnExit(GameActorState toState)
        {
            base.OnExit(toState);
            if (_usesFormationSlot)
                _context.ReleaseFormationSlot();
        }

        /// <summary>
        /// 타겟의 왼쪽/오른쪽 중 현재 내 위치와 반대쪽을 목적지로 설정한다.
        /// → 항상 타겟이 예측하지 못한 방향에서 접근
        /// </summary>
        private void CalculateFlankTarget()
        {
            if (!_detection.HasTarget)
            {
                _flankTarget = motor.TransientPosition;
                return;
            }

            Vector3 toEnemy = (motor.TransientPosition - _detection.CurrentTarget.position).normalized;
            toEnemy.y = 0;

            // 타겟 기준으로 내가 왼쪽에 있으면 오른쪽으로, 오른쪽이면 왼쪽으로
            Vector3 right = Vector3.Cross(Vector3.up, toEnemy).normalized;
            float side    = Vector3.Dot(right, toEnemy) >= 0 ? -1f : 1f;

            _flankTarget = _detection.CurrentTarget.position + right * (FLANK_OFFSET_DIST * side);
            _flankTarget.y = motor.TransientPosition.y;
        }

        public override void UpdateState(float deltaTime)
        {
            if (ShouldTransitionToAirborne(deltaTime))
            {
                controller.TransitionToState(new EnemyAirborneState(controller));
                return;
            }

            if (!_detection.HasTarget)
            {
                controller.TransitionToState(new EnemyIdleState(controller));
                return;
            }

            _flankTimer += deltaTime;
            if (_usesFormationSlot)
                _context.TryGetFormationSlotPosition(FLANK_OFFSET_DIST, out _flankTarget);

            float distToFlank = Vector3.Distance(
                new Vector3(motor.TransientPosition.x, 0, motor.TransientPosition.z),
                new Vector3(_flankTarget.x, 0, _flankTarget.z));

            if (distToFlank <= ARRIVAL_THRESHOLD && !_hasReachedFlank)
            {
                _hasReachedFlank = true;
                // 측면 도달 → 공격
                controller.TransitionToState(
                    new EnemyAttackState(controller, _combat, _context, _detection));
                return;
            }

            if (_flankTimer >= MAX_FLANK_DURATION)
            {
                controller.TransitionToState(
                    new EnemyChaseState(controller, _context, _detection));
                return;
            }

            EnemyLocomotionHelper.UpdateAnim(gameActor, motor, ref _lastLocoKey,
                EnemyLocomotionHelper.LocoStyle.Run);
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            // 이동 방향이 아닌 타겟을 바라보며 이동 (게처럼 옆으로 가는 느낌)
            if (_detection.HasTarget)
            {
                Vector3 dirToTarget = (_detection.CurrentTarget.position - motor.TransientPosition).normalized;
                dirToTarget.y = 0;
                if (dirToTarget.sqrMagnitude > 0.01f)
                {
                    currentRotation = Quaternion.Slerp(
                        currentRotation,
                        Quaternion.LookRotation(dirToTarget),
                        1 - Mathf.Exp(-controller.OrientationSharpness * deltaTime));
                }
            }
            currentRotation = currentRotation.normalized;
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (!_detection.HasTarget || !motor.GroundingStatus.IsStableOnGround)
            {
                currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero,
                    1 - Mathf.Exp(-controller.StableMovementSharpness * deltaTime));
                return;
            }

            Vector3 dirToFlank = (_flankTarget - motor.TransientPosition).normalized;
            dirToFlank.y = 0;

            Vector3 targetVelocity = dirToFlank * _flankSpeed;
            targetVelocity = motor.GetDirectionTangentToSurface(
                targetVelocity, motor.GroundingStatus.GroundNormal) * targetVelocity.magnitude;

            currentVelocity = Vector3.Lerp(
                currentVelocity,
                targetVelocity,
                1 - Mathf.Exp(-controller.StableMovementSharpness * deltaTime));
        }

        public override void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
            ref KinematicCharacterController.HitStabilityReport hitStabilityReport)
        {
            // 측면 경로 막혔으면 재계산
            CalculateFlankTarget();
        }
    }
}
