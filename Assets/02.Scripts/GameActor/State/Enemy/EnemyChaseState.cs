using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Components;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 추적 상태 - 타겟을 향해 이동
    /// </summary>
    public class EnemyChaseState : EnemyActorState
    {
        public override string StateName => "Chase";
        
        private EnemyAIContext _context;
        private EnemyDetection _detection;
        private EnemyTacticalMemory _memory;

        private float _chaseSpeed;
        private float _strafeSign; // +1 or -1, OnEnter마다 랜덤 결정
        private AnimKey _lastLocoKey = AnimKey.None;
        private Collider[] _selfColliders;
        private Collider[] _targetColliders;
        private Transform _cachedTarget;
        private float _targetContactTimer;
        private bool _hasTargetContact;
        private float _nextContactCheckTime;

        private const float TARGET_CONTACT_BREAK_TIME = 0.08f;
        private const float TARGET_CONTACT_CHECK_INTERVAL = 0.05f;
        
        public EnemyChaseState(ActorMovementController controller, EnemyAIContext context, EnemyDetection detection) : base(controller)
        {
            _context = context;
            _detection = detection;
            // 핫패스 GetComponent 제거: 액터 생애 동안 불변이므로 1회 캐싱
            _memory = gameActor.GetComponent<EnemyTacticalMemory>();
        }

        public override bool CanTransitionState(string stateName)
        {
            return true;
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            _chaseSpeed = controller.MaxRunMoveSpeed * _context.ChaseSpeedMultiplier;
            _strafeSign = Random.value > 0.5f ? 1f : -1f;
            _lastLocoKey = AnimKey.None;
            _targetContactTimer = 0f;
            _hasTargetContact = false;
            _nextContactCheckTime = 0f;
            CacheContactColliders();
            UpdateChaseAnimation(0.25f);
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

            if (ShouldBreakTargetContact(deltaTime))
            {
                controller.TransitionToState(
                    new EnemyJumpBackState(
                        controller,
                        _context,
                        _detection,
                        _memory));
                return;
            }

            UpdateChaseAnimation();
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            if (_detection.HasTarget)
            {
                // 타겟을 향해 회전
                Vector3 directionToTarget = (_detection.CurrentTarget.position - motor.TransientPosition).normalized;
                directionToTarget.y = 0; // 수평 방향만
                
                if (directionToTarget.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                    currentRotation = Quaternion.Slerp(
                        currentRotation,
                        targetRotation,
                        1 - Mathf.Exp(-controller.OrientationSharpness * deltaTime));
                }
            }
            
            currentRotation = currentRotation.normalized;
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (!_detection.HasTarget)
            {
                _hasTargetContact = false;
                currentVelocity = Vector3.Lerp(
                    currentVelocity,
                    Vector3.zero,
                    1 - Mathf.Exp(-controller.StableMovementSharpness * deltaTime));
                return;
            }

            if (_hasTargetContact)
            {
                currentVelocity = Vector3.Lerp(
                    currentVelocity,
                    Vector3.zero,
                    1 - Mathf.Exp(-controller.StableMovementSharpness * 2f * deltaTime));
                return;
            }

            Vector3 toTarget = _detection.CurrentTarget.position - motor.TransientPosition;
            toTarget.y       = 0;
            float dist       = toTarget.magnitude;

            // 전투 최소 거리 안에서는 절대 계속 밀고 들어가지 않는다.
            float stopDistance = Mathf.Max(_context.ChaseStopDistance, _context.MinCombatDistance);
            if (dist <= stopDistance)
            {
                currentVelocity = Vector3.Lerp(
                    currentVelocity,
                    Vector3.zero,
                    1 - Mathf.Exp(-controller.StableMovementSharpness * deltaTime));
                return;
            }

            if (!motor.GroundingStatus.IsStableOnGround)
            {
                if (currentVelocity.sqrMagnitude > 0.01f)
                {
                    Vector3 airVelocity = currentVelocity;
                    airVelocity.y   = 0;
                    currentVelocity = airVelocity.normalized * Mathf.Min(airVelocity.magnitude, _chaseSpeed);
                }
                return;
            }

            Vector3 moveDir = toTarget.normalized;

            // chaseStopDistance의 1.5배 이내 진입 시 측면 이동 혼합 (직진 70% + 측면 30%)
            // 단조로운 직선 돌진을 막아 자연스러운 접근처럼 보이게 한다
            if (dist < _context.ChaseStopDistance * 1.5f)
            {
                Vector3 strafeDir = Vector3.Cross(Vector3.up, moveDir) * _strafeSign;
                moveDir = (moveDir * 0.7f + strafeDir * 0.3f).normalized;
            }

            Vector3 targetVelocity = moveDir * _chaseSpeed;
            targetVelocity = motor.GetDirectionTangentToSurface(targetVelocity, motor.GroundingStatus.GroundNormal)
                             * targetVelocity.magnitude;

            currentVelocity = Vector3.Lerp(
                currentVelocity,
                targetVelocity,
                1 - Mathf.Exp(-controller.StableMovementSharpness * deltaTime));
        }

        public override void OnMovementHit(
            Collider hitCollider,
            Vector3 hitNormal,
            Vector3 hitPoint,
            ref KinematicCharacterController.HitStabilityReport hitStabilityReport)
        {
            if (!_detection.HasTarget || !IsTargetCollider(hitCollider))
                return;

            _hasTargetContact = true;
            controller.TransitionToState(
                new EnemyJumpBackState(
                    controller,
                    _context,
                    _detection,
                    _memory));
        }

        private bool ShouldBreakTargetContact(float deltaTime)
        {
            if (Time.time >= _nextContactCheckTime)
            {
                _nextContactCheckTime = Time.time + TARGET_CONTACT_CHECK_INTERVAL;
                _hasTargetContact = IsTouchingTargetCollider();
            }

            if (_hasTargetContact)
            {
                _targetContactTimer += deltaTime;
                return _targetContactTimer >= TARGET_CONTACT_BREAK_TIME;
            }

            _targetContactTimer = 0f;
            return false;
        }

        private void CacheContactColliders()
        {
            // self 콜라이더는 액터 생애 동안 불변 → 최초 1회만 수집
            if (_selfColliders == null)
                _selfColliders = gameActor.GetComponentsInChildren<Collider>();
            _cachedTarget = _detection.CurrentTarget;
            _targetColliders = _detection.CurrentTarget != null
                ? _detection.CurrentTarget.GetComponentsInChildren<Collider>()
                : null;
        }

        private bool IsTouchingTargetCollider()
        {
            if (_detection.CurrentTarget == null)
                return false;

            if (_cachedTarget != _detection.CurrentTarget || _targetColliders == null || _targetColliders.Length == 0)
                CacheContactColliders();

            if (_selfColliders == null || _targetColliders == null)
                return false;

            for (int i = 0; i < _selfColliders.Length; i++)
            {
                var self = _selfColliders[i];
                if (!IsUsableCollider(self))
                    continue;

                for (int j = 0; j < _targetColliders.Length; j++)
                {
                    var target = _targetColliders[j];
                    if (!IsUsableCollider(target))
                        continue;

                    if (Physics.ComputePenetration(
                            self,
                            self.transform.position,
                            self.transform.rotation,
                            target,
                            target.transform.position,
                            target.transform.rotation,
                            out _,
                            out _))
                    {
                        return true;
                    }

                    if ((self.ClosestPoint(target.bounds.center) - target.ClosestPoint(self.bounds.center)).sqrMagnitude <= 0.0025f)
                        return true;
                }
            }

            return false;
        }

        private static bool IsUsableCollider(Collider collider)
        {
            return collider != null && collider.enabled && !collider.isTrigger;
        }

        private bool IsTargetCollider(Collider hitCollider)
        {
            if (hitCollider == null || _detection.CurrentTarget == null)
                return false;

            return hitCollider.transform == _detection.CurrentTarget
                   || hitCollider.transform.IsChildOf(_detection.CurrentTarget)
                   || _detection.CurrentTarget.IsChildOf(hitCollider.transform);
        }

        private void UpdateChaseAnimation(float crossfade = 0.15f)
        {
            if (_hasTargetContact || IsWithinStopDistance())
            {
                if (_lastLocoKey != AnimKey.Idle)
                {
                    gameActor.Animator.PlayMotion(AnimKey.Idle, crossfade);
                    _lastLocoKey = AnimKey.Idle;
                }
                return;
            }

            EnemyLocomotionHelper.UpdateAnim(gameActor, motor, ref _lastLocoKey,
                EnemyLocomotionHelper.LocoStyle.Run, crossfade);

            if (_lastLocoKey == AnimKey.None)
            {
                gameActor.Animator.PlayMotion(AnimKey.Run, crossfade);
                _lastLocoKey = AnimKey.Run;
            }
        }

        private bool IsWithinStopDistance()
        {
            if (!_detection.HasTarget)
                return false;

            Vector3 toTarget = _detection.CurrentTarget.position - motor.TransientPosition;
            toTarget.y = 0f;
            float stopDistance = Mathf.Max(_context.ChaseStopDistance, _context.MinCombatDistance);
            return toTarget.sqrMagnitude <= stopDistance * stopDistance;
        }
    }
}
