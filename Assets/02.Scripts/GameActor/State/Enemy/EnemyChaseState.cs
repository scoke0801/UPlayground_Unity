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
        public override ActorStateId StateId => ActorStateId.Chase;
        
        private EnemyAIContext _context;
        private EnemyDetection _detection;
        private EnemyTacticalMemory _memory;
        private EnemyCombat _combat;

        private float _chaseSpeed;
        private float _strafeSign; // +1 or -1, OnEnter마다 랜덤 결정
        private UPlayGround.Gameplay.Tag.GameplayTag _lastLocoKey = default;
        private Collider[] _selfColliders;
        private Collider[] _targetColliders;
        private Transform _cachedTarget;
        private float _targetContactTimer;
        private bool _hasTargetContact;
        private float _nextContactCheckTime;
        private bool _hasFormationTarget;
        private Vector3 _formationTarget;
        private float _formationArrivalTolerance;
        private float _preferredMeleeApproachDistance;
        private AbilityAttackCategory _approachAttackCategory;

        private const float TARGET_CONTACT_BREAK_TIME = 0.08f;
        private const float TARGET_CONTACT_CHECK_INTERVAL = 0.05f;
        // 그룹 분리 스티어링: 이 반경 안의 동료로부터 밀려나며, 밀어내기 강도를 이동 방향에 블렌드한다.
        private const float SEPARATION_RADIUS = 1.6f;
        private const float SEPARATION_WEIGHT = 0.65f;
        
        public EnemyChaseState(
            ActorMovementController controller,
            EnemyAIContext context,
            EnemyDetection detection,
            AbilityAttackCategory approachAttackCategory = AbilityAttackCategory.None) : base(controller)
        {
            _context = context;
            _detection = detection;
            _approachAttackCategory = approachAttackCategory;
            // 핫패스 GetComponent 제거: 액터 생애 동안 불변이므로 1회 캐싱
            _memory = gameActor.GetComponent<EnemyTacticalMemory>();
            _combat = gameActor.GetComponent<EnemyCombat>();
        }

        public override bool CanTransitionState(ActorStateId fromState)
        {
            return true;
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            _chaseSpeed = controller.MaxRunMoveSpeed * _context.ChaseSpeedMultiplier;
            _strafeSign = Random.value > 0.5f ? 1f : -1f;
            _lastLocoKey = default;
            _targetContactTimer = 0f;
            _hasTargetContact = false;
            _nextContactCheckTime = 0f;
            _hasFormationTarget = false;
            RefreshPreferredApproachDistance();
            CacheContactColliders();
            UpdateChaseAnimation(0.25f);
        }

        /// <summary>
        /// 실행 대기 중인 공격 카테고리에 맞춰 정지 거리를 갱신한다.
        /// 명시 카테고리는 그룹의 범용 진형 반경과 다를 수 있으므로 해당 접근 중에는 진형 슬롯을 사용하지 않는다.
        /// </summary>
        public void SetApproachAttackCategory(AbilityAttackCategory attackCategory)
        {
            // BT는 접근이 끝날 때까지 매 틱 같은 카테고리로 이 메서드를 호출한다.
            // 갱신은 AbilitySet 전 Variant 순회를 동반하므로 값이 바뀔 때만 수행한다.
            if (_approachAttackCategory == attackCategory)
                return;

            _approachAttackCategory = attackCategory;
            RefreshPreferredApproachDistance();
            if (_approachAttackCategory == AbilityAttackCategory.None)
                return;

            _hasFormationTarget = false;
            _context.ReleaseFormationSlot();
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
                controller.TransitionToState(ActorStateId.Idle);
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

            _hasFormationTarget = _approachAttackCategory == AbilityAttackCategory.None
                                  && _context.TryGetChaseFormationPosition(
                                      dist,
                                      out _formationTarget,
                                      out _formationArrivalTolerance);
            Vector3 toMoveTarget = _hasFormationTarget
                ? _formationTarget - motor.TransientPosition
                : toTarget;
            toMoveTarget.y = 0f;
            float moveDistance = toMoveTarget.magnitude;

            // 저작된 전투 정지 거리보다 실제 근접 공격 사거리가 짧으면
            // 공격 가능한 거리까지 추가 접근한다. 타깃 침투는 접촉 판정이 별도로 차단한다.
            float stopDistance = ResolveStopDistance();
            float arrivalDistance = _hasFormationTarget
                ? Mathf.Max(0.05f, _formationArrivalTolerance)
                : stopDistance;
            if (moveDistance <= arrivalDistance)
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

            Vector3 moveDir = toMoveTarget.normalized;

            // chaseStopDistance의 1.5배 이내 진입 시 측면 이동 혼합 (직진 70% + 측면 30%)
            // 단조로운 직선 돌진을 막아 자연스러운 접근처럼 보이게 한다
            if (dist < _context.ChaseStopDistance * 1.5f)
            {
                Vector3 strafeDir = Vector3.Cross(Vector3.up, moveDir) * _strafeSign;
                moveDir = (moveDir * 0.7f + strafeDir * 0.3f).normalized;
            }

            // 그룹 동료로부터의 분리 스티어링 — 여러 마리가 같은 각도로 몰려
            // 서로 콜라이더에 막혀 제자리에 갈리는 현상을 완화한다.
            Vector3 separation = _context.GetGroupSeparation(SEPARATION_RADIUS);
            if (separation.sqrMagnitude > 0.0001f)
                moveDir = (moveDir + separation * SEPARATION_WEIGHT).normalized;

            Vector3 targetVelocity = moveDir * _chaseSpeed;
            targetVelocity = motor.GetDirectionTangentToSurface(targetVelocity, motor.GroundingStatus.GroundNormal)
                             * targetVelocity.magnitude;

            currentVelocity = Vector3.Lerp(
                currentVelocity,
                targetVelocity,
                1 - Mathf.Exp(-controller.StableMovementSharpness * deltaTime));
        }

        public override void OnExit(GameActorState toState)
        {
            _context.ReleaseFormationSlot();
            base.OnExit(toState);
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
                if (_lastLocoKey != UPlayGround.Data.Actor.Animation.MotionTags.Idle)
                {
                    gameActor.Animator.PlayMotion(UPlayGround.Data.Actor.Animation.MotionTags.Idle, crossfade);
                    _lastLocoKey = UPlayGround.Data.Actor.Animation.MotionTags.Idle;
                }
                return;
            }

            EnemyLocomotionHelper.UpdateAnim(gameActor, motor, ref _lastLocoKey,
                EnemyLocomotionHelper.LocoStyle.Run, crossfade);

            if (_lastLocoKey == default)
            {
                gameActor.Animator.PlayMotion(UPlayGround.Data.Actor.Animation.MotionTags.Run, crossfade);
                _lastLocoKey = UPlayGround.Data.Actor.Animation.MotionTags.Run;
            }
        }

        private bool IsWithinStopDistance()
        {
            if (!_detection.HasTarget)
                return false;

            if (_hasFormationTarget)
            {
                Vector3 toFormation = _formationTarget - motor.TransientPosition;
                toFormation.y = 0f;
                float tolerance = Mathf.Max(0.05f, _formationArrivalTolerance);
                return toFormation.sqrMagnitude <= tolerance * tolerance;
            }

            Vector3 toTarget = _detection.CurrentTarget.position - motor.TransientPosition;
            toTarget.y = 0f;
            float stopDistance = ResolveStopDistance();
            return toTarget.sqrMagnitude <= stopDistance * stopDistance;
        }

        private float ResolveStopDistance()
        {
            float stopDistance = Mathf.Max(_context.ChaseStopDistance, _context.MinCombatDistance);
            return _preferredMeleeApproachDistance > 0f
                ? Mathf.Min(stopDistance, _preferredMeleeApproachDistance)
                : stopDistance;
        }

        private void RefreshPreferredApproachDistance()
        {
            _preferredMeleeApproachDistance = _combat?.GetPreferredMeleeApproachDistance(
                _approachAttackCategory) ?? 0f;
        }
    }
}
