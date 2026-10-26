using UnityEngine;
using UPlayGround.Animation;

namespace UPlayGround.MovementController
{
    public partial class MotionWarpController
    {
        private Vector3 _sessionStartPosition;
        private Vector3 _endpointCorrection;
        private Vector3 _arrivalPosition;
        private float _translationCutoff;
        private float _translationPath;
        private float _rotationAlpha;
        private float _plannedWarpYaw;
        private float _consumedCorrectionFraction;
        private bool _translationBlocked;
        private bool _trackingCommitted;
        private bool _attackTrackingCommitted;
        private bool _rotationApplicable;
        private Vector3 _attackIntentForward;
        private bool _sessionHasTarget;
        private ActorAnimator _motionSource;
        private ActorAnimator MotionSource => _motionSource != null ? _motionSource : _actor?.Animator;

        public Vector3 OriginalStepDelta { get; private set; }
        public Vector3 CorrectionStepDelta { get; private set; }
        public Vector3 ActualMotorStepDelta { get; private set; }
        public float RotationCorrectionDegrees { get; private set; }
        public float PredictionError { get; private set; }
        public Vector3 ExpectedArrival => _arrivalPosition;
        public bool HasCommittedAttackTracking => _attackTrackingCommitted;
        public Vector3 AttackIntentDirection => _attackIntentForward;
        /// <summary>이번 구간의 끝점 보정 총량. 진단과 검증 도구가 읽는다.</summary>
        public Vector3 PlannedCorrection => _endpointCorrection;
        /// <summary>KCC가 보정을 소비한 스텝만 관측한다. 구독자가 없으면 별도 진단 할당이 없다.</summary>
        public event System.Action<MotionWarpController> MotorStepEvaluated;

        /// <summary>잔류 모델도 동일한 Animator 시간과 리그 정보를 사용한다.</summary>
        public void BindMotionSource(ActorAnimator animator) => _motionSource = animator;

        private void BeginEndpointSession()
        {
            _sessionStartPosition = transform.position;
            _warpStartRotation = transform.rotation;
            _warpStartCaptured = true;
            _rotationAlpha = 0f;
            _plannedWarpYaw = 0f;
            _consumedCorrectionFraction = 0f;
            _translationBlocked = false;
            _trackingCommitted = _targetLockActive && _attackTrackingCommitted;
            _sessionHasTarget = _activeTarget.IsValid;
            _rotationApplicable = !_trackingCommitted && (_sessionHasTarget || _attackIntentForward.sqrMagnitude > .0001f)
                && _windowSettings.rotationWeight > 0f;
            _endpointCorrection = Vector3.zero;
            _translationCutoff = _windowSettings.duration > 0f
                ? Mathf.Clamp01(1f - _windowSettings.translationEndLeadTime / _windowSettings.duration) : 0f;
            _translationPath = 0f;
            OriginalStepDelta = CorrectionStepDelta = ActualMotorStepDelta = Vector3.zero;
            RotationCorrectionDegrees = PredictionError = 0f;
            PrepareEndpointCorrection();
        }

        private void PrepareEndpointCorrection()
        {
            _isApplicable = false;
            if (!_activeTarget.IsValid) { RejectTranslation("대상 없음"); return; }
            if (_windowSettings.translationWeight <= 0f) { RejectTranslation("저작된 회전 전용 구간"); return; }
            if (_activeTrajectoryProfile == null) { RejectTranslation("누락·불일치·stale 루트 프로필"); return; }
            MotionWarpTrajectory.SampleRawCumulative(_activeTrajectoryProfile, _translationCutoff,
                out Vector3 sourceEnd, out _translationPath, out _);
            float netDistance = sourceEnd.magnitude;
            if (netDistance < _windowSettings.minimumSourceDisplacement || _translationPath <= 0.0001f
                || netDistance / _translationPath < _windowSettings.minimumNetToPathRatio)
            { RejectTranslation("제자리·왕복 구간: 이동 생성 금지"); return; }
            _isApplicable = true;
            RefreshEndpoint();
        }

        private bool RefreshEndpoint()
        {
            Vector3 targetWorld = _activeTarget.follow ? _activeTarget.ResolveWorldPosition() : _snapshotPosition;
            if (Mathf.Abs(targetWorld.y - _sessionStartPosition.y) > _windowSettings.maxHeightDifference)
            { RejectTranslation("목표 높이 차 거절"); return false; }
            Vector3 offset = Vector3.ProjectOnPlane(targetWorld - _sessionStartPosition, transform.up);
            if (offset.magnitude > _windowSettings.maxDistance)
            { RejectTranslation("목표 거리 거절"); return false; }
            Vector3 selfCenter = GetSelfCapsuleCenterPosition(_sessionStartPosition);
            Vector3 targetCenter = ResolveTargetCenterWorld(targetWorld, _activeTarget.anchor);
            float contactDistance = Vector3.ProjectOnPlane(targetCenter - selfCenter, transform.up).magnitude;
            if (MotionWarpMath.IsInsideContactDeadZone(contactDistance,
                    GetHorizontalRadius(_selfCapsule), GetHorizontalRadius(GetTargetCapsule(_activeTarget.anchor)),
                    _windowSettings.desiredStandOff, _windowSettings.noTranslationWithinReach))
            { RejectTranslation("이미 유효 접촉 거리 안"); return false; }
            if (Physics.Linecast(selfCenter, targetCenter, _windowSettings.obstacleLayers, QueryTriggerInteraction.Ignore))
            { RejectTranslation("목표 경로 장애물"); return false; }
            _arrivalPosition = ResolveArrivalWorld(_windowSettings, _sessionStartPosition,
                selfCenter, targetWorld, targetCenter);
            Vector3 sourceWorld = PredictSourceDisplacement(targetWorld, targetCenter);
            Vector3 desired = Vector3.ProjectOnPlane(_arrivalPosition - _sessionStartPosition, transform.up);
            if (!MotionWarpMath.TryResolveEndpointCorrection(sourceWorld, _translationPath,
                    desired, _windowSettings, out Vector3 correction))
            { RejectTranslation("원본 궤적 변형 허용 범위 초과"); return false; }
            _endpointCorrection = correction;
            // 접촉 목표는 진단에 보존한다. 일부 접근을 목표 도달로 표시하지 않는다.
            if ((desired - sourceWorld - correction).sqrMagnitude > 0.00000001f)
                _constraintFlags |= MotionWarpConstraintFlags.CorrectionLimited;
            _translationSuppressed = false;
            _lastFailureReason = string.Empty;
            _blendWeight = 1f;
            return true;
        }

        /// <summary>
        /// 같은 구간의 회전 워프가 원본 궤적을 대상 쪽으로 돌린 결과까지 포함해 원본 끝점을 예측한다.
        /// 시작 방향 그대로 예측하면 회전으로 이미 맞춰질 각도까지 이동 보정이 떠안아 각도 상한에 걸린다.
        /// </summary>
        private Vector3 PredictSourceDisplacement(Vector3 targetWorld, Vector3 targetCenter)
        {
            Vector3 facingTarget = _windowSettings.arrivalMode == WarpArrivalMode.ContactShell ? targetCenter : targetWorld;
            Vector3 direction = Vector3.ProjectOnPlane(facingTarget - _sessionStartPosition, transform.up);
            _plannedWarpYaw = _rotationApplicable ? ResolveWarpYaw(direction, _windowSettings.maxWarpAngle) : 0f;
            return MotionWarpTrajectory.PredictRotatedDisplacement(_activeTrajectoryProfile, _translationCutoff,
                _trajectoryBasisRotation, transform.up, _plannedWarpYaw,
                _windowSettings.rotationCurve, _windowSettings.rotationWeight);
        }

        private Vector3 EvaluateEndpointVelocity(Vector3 rootVelocity, Vector3 currentPosition, float deltaTime)
        {
            OriginalStepDelta = rootVelocity * Mathf.Max(0f, deltaTime);
            CorrectionStepDelta = Vector3.zero;
            if (deltaTime <= 0f || !IsMotionWarping) return rootVelocity;
            if (!_hasMotorStep)
            {
                bool active = true;
                float remaining = _warpRemainingTime;
                float duration = _warpTotalDuration;
                UpdateAnimationProgress(ref active, ref remaining, ref duration);
            }
            if (_sessionHasTarget && IsTargetUnreachableLifecycle())
            {
                _rotationApplicable = false;
                RejectTranslation("대상 없음·사망·비활성");
                return rootVelocity;
            }
            if (!_isApplicable || _translationBlocked || _trackingCommitted) return rootVelocity;
            float previous = Mathf.Min(_previousAnimationProgress, _translationCutoff);
            float current = Mathf.Min(_currentAnimationProgress, _translationCutoff);
            if (current <= previous || _translationPath <= 0f) return rootVelocity;
            MotionWarpTrajectory.SampleRawCumulative(_activeTrajectoryProfile, previous,
                out Vector3 sourceBefore, out float pathBefore, out _);
            MotionWarpTrajectory.SampleRawCumulative(_activeTrajectoryProfile, current,
                out Vector3 sourceAfter, out float pathAfter, out _);
            Vector3 planarRoot = Vector3.ProjectOnPlane(OriginalStepDelta, transform.up);
            bool followsAuthoredCurve = _windowSettings.correctionTiming == MotionWarpCorrectionTiming.AuthoredCurve;
            // 루트 비례 분배는 멈춘 실제 포즈에 베이크만으로 전진을 만들지 않는다. 소비하지 않은 보정도 이월하지 않는다.
            // 저작 곡선은 애니메이션 시간으로만 진행하므로 히트스톱·정지 포즈에서는 위 진행도 검사에서 이미 멈춘다.
            if (!followsAuthoredCurve && planarRoot.sqrMagnitude <= 0.00000001f) return rootVelocity;
            if (_activeTarget.follow && !RefreshEndpoint()) return rootVelocity;
            Vector3 correction = followsAuthoredCurve
                ? ConsumeAuthoredCurveCorrection(current, deltaTime)
                : ConsumeRootPathCorrection(previous, current, pathBefore, pathAfter, planarRoot.magnitude, deltaTime);
            Quaternion appliedWarpYaw = Quaternion.AngleAxis(_plannedWarpYaw * _rotationAlpha, transform.up);
            PredictionError = (planarRoot - appliedWarpYaw * (_trajectoryBasisRotation * (sourceAfter - sourceBefore))).magnitude;
            CorrectionStepDelta = correction;
            _lastArrivalError = Vector3.ProjectOnPlane(_arrivalPosition - currentPosition, transform.up).magnitude;
            return rootVelocity + correction / deltaTime;
        }

        /// <summary>원본 루트가 이번 스텝에 실제로 진행한 경로 비율만큼 보정을 나눈다.</summary>
        private Vector3 ConsumeRootPathCorrection(float previous, float current, float pathBefore, float pathAfter,
            float actualPath, float deltaTime)
        {
            Vector3 correction = MotionWarpMath.DistributeEndpointCorrection(_endpointCorrection,
                pathBefore, pathAfter, _translationPath, actualPath) * _windowSettings.translationWeight;
            float weight = _windowSettings.translationCurve != null && _windowSettings.translationCurve.length > 0
                ? Mathf.Clamp01(_windowSettings.translationCurve.Evaluate((previous + current) * 0.5f)) : 1f;
            return ClampCorrectionSpeed(correction * weight, deltaTime);
        }

        /// <summary>
        /// 저작 곡선이 가리키는 누적 비율까지 보정을 소비한다.
        /// 속도 상한에 걸려 못 쓴 몫은 곡선이 완만해지는 뒤 스텝에서 이어 쓴다. 이동 종료 여유를 넘긴 몫은 버린다.
        /// </summary>
        private Vector3 ConsumeAuthoredCurveCorrection(float current, float deltaTime)
        {
            float translationProgress = _translationCutoff > 0f ? current / _translationCutoff : 1f;
            float targetFraction = MotionWarpMath.EvaluateCorrectionTimingFraction(
                _windowSettings.correctionTimingCurve, translationProgress);
            float maxStepDistance = _windowSettings.maxSpeed > 0f ? _windowSettings.maxSpeed * deltaTime : -1f;
            Vector3 correction = MotionWarpMath.ConsumeAuthoredCorrection(
                _endpointCorrection * _windowSettings.translationWeight, targetFraction, maxStepDistance,
                ref _consumedCorrectionFraction, out bool isSpeedLimited);
            if (isSpeedLimited)
                _constraintFlags |= MotionWarpConstraintFlags.SpeedClamped;
            return correction;
        }

        private Vector3 ClampCorrectionSpeed(Vector3 correction, float deltaTime)
        {
            if (_windowSettings.maxSpeed <= 0f || correction.magnitude / deltaTime <= _windowSettings.maxSpeed)
                return correction;
            _constraintFlags |= MotionWarpConstraintFlags.SpeedClamped;
            return Vector3.ClampMagnitude(correction, _windowSettings.maxSpeed * deltaTime);
        }

        private void RejectTranslation(string reason)
        {
            _isApplicable = false;
            _translationSuppressed = true;
            _lastFailureReason = reason;
            _constraintFlags |= MotionWarpConstraintFlags.TranslationSuppressed;
        }

        /// <summary>판정이 열린 뒤에는 같은 공격의 이동과 회전 추적을 다시 열지 않는다.</summary>
        public void CommitTargetTracking()
        {
            _trackingCommitted = true;
            _attackTrackingCommitted = true;
            _rotationApplicable = false;
            RejectTranslation("타격 확정 이후 추적 종료");
        }
    }
}
