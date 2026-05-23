using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Component;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 배회 상태 - 타겟 주변을 자연스럽게 움직이며 거리를 유지
    /// Perlin Noise 기반 방향/속도 변화 + 간헐적 정지/방향 전환
    /// </summary>
    public class EnemyCircleState : GameActorState
    {
        public override string StateName => "Circle";
        public override bool BlocksBehaviorTree => true;

        private EnemyAIContext _context;
        private EnemyDetection _detection;

        private float _baseSpeed;
        private float _circleTimer;
        private float _circleDuration;
        private float _circleDirection; // +1 or -1

        // Perlin Noise 오프셋 (인스턴스별 고유)
        private float _noiseOffsetAngle;
        private float _noiseOffsetSpeed;
        private float _noiseOffsetRadial;

        // 간헐적 정지(일시 멈춤)
        private float _pauseTimer;
        private float _nextPauseTime;
        private float _pauseDuration;
        private bool _isPaused;

        // 방향 전환
        private float _directionChangeTimer;
        private float _nextDirectionChangeTime;
        private AnimKey _lastLocoKey = AnimKey.None;
        private AnimKey _pendingLocoKey = AnimKey.None;
        private float _pendingLocoKeyTimer;
        private bool _usesFormationSlot;
        private bool _movingToFormationSlot;
        private bool _formationSlotAcquired;
        private float _stationaryAnimTimer;

        private const float BASE_SPEED_RATIO = 0.5f;
        private const float NOISE_SPEED = 0.8f;            // 노이즈 변화 속도
        private const float ANGLE_NOISE_STRENGTH = 40f;     // 접선 각도 흔들림 (도)
        private const float SPEED_NOISE_MIN = 0.4f;         // 최소 속도 배율
        private const float SPEED_NOISE_MAX = 1.0f;         // 최대 속도 배율
        private const float RADIAL_NOISE_STRENGTH = 0.4f;   // 거리 보정 흔들림
        private const float STATIONARY_ANIM_DELAY = 0.12f;  // 실제 이동이 없을 때 Idle 전환 지연
        private const float LOCO_KEY_SWITCH_DELAY = 0.18f;  // 방향 키가 잠깐 튀는 경우 모션 재시작 방지
        private const float FORMATION_SLOT_ARRIVAL_DISTANCE = 0.75f;

        public EnemyCircleState(
            ActorMovementController controller,
            EnemyAIContext context,
            EnemyDetection detection,
            float duration) : base(controller)
        {
            _context = context;
            _detection = detection;
            _circleDuration = duration;
        }

        public override bool CanTransitionState(string stateName)
        {
            return true;
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            _circleTimer = 0f;
            _baseSpeed = controller.MaxRunMoveSpeed * BASE_SPEED_RATIO;
            _circleDirection = Random.value > 0.5f ? 1f : -1f;

            // 인스턴스별 고유 노이즈 시드
            _noiseOffsetAngle = Random.Range(0f, 1000f);
            _noiseOffsetSpeed = Random.Range(0f, 1000f);
            _noiseOffsetRadial = Random.Range(0f, 1000f);

            // 첫 정지 타이밍 예약
            _pauseTimer = 0f;
            _isPaused = false;
            ScheduleNextPause();

            // 첫 방향 전환 타이밍 예약
            _directionChangeTimer = 0f;
            ScheduleNextDirectionChange();

            _stationaryAnimTimer = 0f;
            _lastLocoKey = AnimKey.Idle;
            _pendingLocoKey = AnimKey.None;
            _pendingLocoKeyTimer = 0f;
            _movingToFormationSlot = false;
            gameActor.Animator.PlayMotion(AnimKey.Idle, 0.15f);
            _usesFormationSlot = _context.TryGetFormationSlotPosition(_context.RetreatDistance, out _);
            _formationSlotAcquired = !_usesFormationSlot;
        }

        public override void OnExit(GameActorState toState)
        {
            base.OnExit(toState);
            if (_usesFormationSlot)
                _context.ReleaseFormationSlot();
        }

        public override void UpdateState(float deltaTime)
        {
            if (!motor.GroundingStatus.IsStableOnGround)
            {
                controller.TransitionToState(new EnemyAirborneState(controller));
                return;
            }

            if (!_detection.HasTarget)
            {
                controller.TransitionToState(new EnemyIdleState(controller));
                return;
            }

            _circleTimer += deltaTime;

            if (_circleTimer >= _circleDuration)
            {
                // Circle 종료 → 다양한 행동으로 분기 (예측 불가능성)
                float roll = Random.value;
                if (roll < 0.5f)
                {
                    controller.TransitionToState(
                        new EnemyChaseState(controller, _context, _detection));
                }
                else
                {
                    // Brain의 다음 판단에 맡김 (Idle로 돌아가면 Brain이 즉시 재판단)
                    controller.TransitionToState(new EnemyIdleState(controller));
                }
                return;
            }

            UpdatePause(deltaTime);
            UpdateDirectionChange(deltaTime);
        }

        public override void AfterCharacterUpdate(float deltaTime)
        {
            if (!_isPaused)
                UpdateLocomotionAnimation(deltaTime);
        }

        private void UpdateLocomotionAnimation(float deltaTime)
        {
            var velocity = motor.Velocity;
            velocity.y = 0f;

            if (velocity.sqrMagnitude < EnemyLocomotionHelper.MIN_SPEED_SQ)
            {
                _stationaryAnimTimer += deltaTime;
                if (_stationaryAnimTimer >= STATIONARY_ANIM_DELAY && _lastLocoKey != AnimKey.Idle)
                {
                    gameActor.Animator.PlayMotion(AnimKey.Idle, 0.15f);
                    _lastLocoKey = AnimKey.Idle;
                    _pendingLocoKey = AnimKey.None;
                    _pendingLocoKeyTimer = 0f;
                }
                return;
            }

            _stationaryAnimTimer = 0f;

            if (_movingToFormationSlot)
            {
                UpdateFormationMoveAnimation();
                return;
            }

            float localAngle = 0f;
            AnimKey nextKey;
            if (gameActor.Animator.HasFallbackMotionSet)
            {
                Vector3 localVelocity = gameActor.transform.InverseTransformDirection(velocity);
                localVelocity.y = 0f;
                localAngle = Mathf.Atan2(localVelocity.x, localVelocity.z) * Mathf.Rad2Deg;
                nextKey = EnemyLocomotionHelper.GetKey(localAngle, EnemyLocomotionHelper.LocoStyle.Walk);
            }
            else
            {
                nextKey = EnemyLocomotionHelper.BasicKey(EnemyLocomotionHelper.LocoStyle.Walk);
            }

            if (nextKey == _lastLocoKey)
            {
                _pendingLocoKey = AnimKey.None;
                _pendingLocoKeyTimer = 0f;
                return;
            }

            if (_lastLocoKey != AnimKey.None && _lastLocoKey != AnimKey.Idle)
            {
                if (nextKey != _pendingLocoKey)
                {
                    _pendingLocoKey = nextKey;
                    _pendingLocoKeyTimer = 0f;
                    return;
                }

                _pendingLocoKeyTimer += deltaTime;
                if (_pendingLocoKeyTimer < LOCO_KEY_SWITCH_DELAY)
                    return;
            }

            gameActor.Animator.PlayMotion(nextKey, 0.15f);
            _lastLocoKey = nextKey;
            _pendingLocoKey = AnimKey.None;
            _pendingLocoKeyTimer = 0f;
        }

        private void UpdateFormationMoveAnimation()
        {
            const AnimKey nextKey = AnimKey.Walk;
            if (_lastLocoKey == nextKey)
            {
                _pendingLocoKey = AnimKey.None;
                _pendingLocoKeyTimer = 0f;
                return;
            }

            gameActor.Animator.PlayMotion(nextKey, 0.15f);
            _lastLocoKey = nextKey;
            _pendingLocoKey = AnimKey.None;
            _pendingLocoKeyTimer = 0f;
        }

        private void UpdatePause(float deltaTime)
        {
            if (_isPaused)
            {
                _pauseTimer += deltaTime;
                if (_pauseTimer >= _pauseDuration)
                {
                    _isPaused = false;
                    _pauseTimer = 0f;
                    _stationaryAnimTimer = 0f;
                    _lastLocoKey = AnimKey.None; // 재개 시 방향 재평가 강제
                    _pendingLocoKey = AnimKey.None;
                    _pendingLocoKeyTimer = 0f;
                    _movingToFormationSlot = false;
                    ScheduleNextPause();
                }
            }
            else
            {
                _pauseTimer += deltaTime;
                if (_pauseTimer >= _nextPauseTime)
                {
                    _isPaused = true;
                    _pauseTimer = 0f;
                    _stationaryAnimTimer = 0f;
                    _pauseDuration = Random.Range(0.3f, 0.8f);
                    gameActor.Animator.PlayMotion(AnimKey.Idle, 0.2f);
                    _lastLocoKey = AnimKey.Idle;
                    _pendingLocoKey = AnimKey.None;
                    _pendingLocoKeyTimer = 0f;
                    _movingToFormationSlot = false;
                }
            }
        }

        private void UpdateDirectionChange(float deltaTime)
        {
            _directionChangeTimer += deltaTime;
            if (_directionChangeTimer >= _nextDirectionChangeTime)
            {
                _circleDirection *= -1f;
                _directionChangeTimer = 0f;
                ScheduleNextDirectionChange();
            }
        }

        private void ScheduleNextPause()
        {
            _nextPauseTime = Random.Range(1.2f, 2.5f);
            _pauseTimer = 0f;
        }

        private void ScheduleNextDirectionChange()
        {
            _nextDirectionChangeTime = Random.Range(1.5f, 3.5f);
            _directionChangeTimer = 0f;
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            if (!_detection.HasTarget) return;

            Vector3 dirToTarget = (_detection.CurrentTarget.position - motor.TransientPosition).normalized;
            dirToTarget.y = 0;

            if (dirToTarget.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(dirToTarget);
                currentRotation = Quaternion.Slerp(
                    currentRotation,
                    targetRotation,
                    1 - Mathf.Exp(-controller.OrientationSharpness * deltaTime));
            }

            currentRotation = currentRotation.normalized;
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (!_detection.HasTarget || !motor.GroundingStatus.IsStableOnGround)
            {
                _movingToFormationSlot = false;
                currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero,
                    1 - Mathf.Exp(-controller.StableMovementSharpness * deltaTime));
                return;
            }

            // 정지 중이면 감속
            _movingToFormationSlot = false;
            if (_isPaused)
            {
                currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero,
                    1 - Mathf.Exp(-controller.StableMovementSharpness * deltaTime));
                return;
            }

            Vector3 toTarget = _detection.CurrentTarget.position - motor.TransientPosition;
            toTarget.y = 0;
            float currentDistance = toTarget.magnitude;

            if (currentDistance < 0.1f)
            {
                currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero,
                    1 - Mathf.Exp(-controller.StableMovementSharpness * deltaTime));
                return;
            }

            Vector3 dirToTarget = toTarget / currentDistance;

            if (_usesFormationSlot
                && !_formationSlotAcquired
                && _context.TryGetFormationSlotPosition(_context.RetreatDistance, out var formationTarget))
            {
                var toFormation = formationTarget - motor.TransientPosition;
                toFormation.y = 0f;
                if (toFormation.sqrMagnitude > FORMATION_SLOT_ARRIVAL_DISTANCE * FORMATION_SLOT_ARRIVAL_DISTANCE)
                {
                    _movingToFormationSlot = true;
                    var formationVelocity = toFormation.normalized * _baseSpeed;
                    formationVelocity = motor.GetDirectionTangentToSurface(
                        formationVelocity,
                        motor.GroundingStatus.GroundNormal) * formationVelocity.magnitude;

                    currentVelocity = Vector3.Lerp(
                        currentVelocity,
                        formationVelocity,
                        1 - Mathf.Exp(-controller.StableMovementSharpness * deltaTime));
                    return;
                }

                _formationSlotAcquired = true;
            }

            float time = _circleTimer;

            // --- Perlin Noise 기반 자연스러운 변화 ---

            // 1) 접선 각도 흔들림: 순수 접선(90도)이 아니라 노이즈로 ±40도 변동
            float angleNoise = (Mathf.PerlinNoise(time * NOISE_SPEED, _noiseOffsetAngle) - 0.5f) * 2f;
            float strafeAngle = 90f * _circleDirection + angleNoise * ANGLE_NOISE_STRENGTH;
            Vector3 moveDir = Quaternion.Euler(0, strafeAngle, 0) * dirToTarget;

            // 2) 거리 보정 + 노이즈: 목표 거리 유지하되 흔들림 추가
            float optimalDist = _context.RetreatDistance;
            float distanceDiff = currentDistance - optimalDist;
            float radialNoise = (Mathf.PerlinNoise(time * NOISE_SPEED * 0.7f, _noiseOffsetRadial) - 0.5f) * 2f;
            float radialCorrection = Mathf.Clamp(distanceDiff / optimalDist, -0.6f, 0.6f)
                                     + radialNoise * RADIAL_NOISE_STRENGTH;

            moveDir = (moveDir + dirToTarget * radialCorrection).normalized;

            // 3) 속도 변화: 일정하지 않고 느려졌다 빨라졌다
            float speedNoise = Mathf.PerlinNoise(time * NOISE_SPEED * 1.2f, _noiseOffsetSpeed);
            float speedMultiplier = Mathf.Lerp(SPEED_NOISE_MIN, SPEED_NOISE_MAX, speedNoise);

            Vector3 targetVelocity = moveDir * (_baseSpeed * speedMultiplier);

            targetVelocity = motor.GetDirectionTangentToSurface(
                targetVelocity,
                motor.GroundingStatus.GroundNormal) * targetVelocity.magnitude;

            currentVelocity = Vector3.Lerp(
                currentVelocity,
                targetVelocity,
                1 - Mathf.Exp(-controller.StableMovementSharpness * deltaTime));
        }

        public override void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
            ref KinematicCharacterController.HitStabilityReport hitStabilityReport)
        {
            if (!_detection.HasTarget) return;

            Vector3 toTarget = (_detection.CurrentTarget.position - motor.TransientPosition).normalized;
            toTarget.y = 0;
            Vector3 tangent = Vector3.Cross(Vector3.up, toTarget) * _circleDirection;
            float dot = Vector3.Dot(tangent, hitNormal);

            if (dot < -0.3f)
            {
                _circleDirection *= -1f;
            }
        }
    }
}
