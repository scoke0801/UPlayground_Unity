using Animancer.Units;
using UnityEngine;

namespace UPlayGround.Component
{
    /// <summary>
    /// Foot IK + Hip 보정 컴포넌트.
    /// OnAnimatorIK 콜백으로 양발을 지면에 밀착시키고, 골반 높이를 자동 보정한다.
    ///
    /// [핵심 원리]
    /// - 발당 3점 샘플링(중앙/toe/heel)으로 지면 높이 + 경계 감지
    /// - 경계에 걸친 발은 confidence가 낮아져 개별적으로 IK weight 감소 (애니메이션 복귀)
    /// - 꼭지점(vertex)에서는 toe/heel 높이로 보정하여 발이 뜨지 않게 함
    /// - 발 위치·법선·confidence 모두 프레임 간 보간하여 jitter 방지
    /// - 양발 높이차를 고려한 hip 보정으로 양쪽 다리 모두 자연스럽게 구부러지게 함
    /// - 이동 중에는 IK weight를 0으로 페이드하여 애니메이션과 충돌하지 않음
    /// </summary>
    public class FootIKController : MonoBehaviour
    {
        private readonly struct GroundSample
        {
            public readonly bool HasHit;
            public readonly float GroundY;
            public readonly Vector3 Normal;
            /// <summary>1 = 안정 지면, 0 = 경계/꼭지점 (해당 발의 IK weight에 반영)</summary>
            public readonly float Confidence;

            public GroundSample(bool hasHit, float groundY, Vector3 normal, float confidence = 1f)
            {
                HasHit = hasHit;
                GroundY = groundY;
                Normal = normal;
                Confidence = confidence;
            }

            public static readonly GroundSample Miss = new GroundSample(false, 0f, Vector3.up, 0f);
        }

        private struct FootState
        {
            public float SmoothedGroundY;
            public Vector3 SmoothedNormal;
            public float SmoothedConfidence;
            public bool WasValid;
        }

        [Header("Raycast")]
        [SerializeField, Meters] private float _raycastOriginY = 0.5f;
        [SerializeField, Meters] private float _raycastEndY = -0.75f;
        [SerializeField] private LayerMask _groundLayers;
        [SerializeField, Tooltip("지면으로 인정할 최소 법선 Y값")]
        private float _minGroundNormalY = 0.7f;

        [Header("Ground Probe")]
        [SerializeField, Meters, Tooltip("SphereCast 반경")]
        private float _sphereCastRadius = 0.06f;
        [SerializeField, Meters, Tooltip("발끝(toe) 샘플 오프셋")]
        private float _toeOffset = 0.12f;
        [SerializeField, Meters, Tooltip("뒤꿈치(heel) 샘플 오프셋")]
        private float _heelOffset = 0.06f;

        [Header("Vertex Detection")]
        [SerializeField, Meters, Tooltip("중앙 대비 주변이 이 값 이상 낮으면 꼭지점 판정")]
        private float _vertexThreshold = 0.05f;

        [Header("IK Blend")]
        [SerializeField] private float _ikBlendSpeed = 10f;
        [SerializeField, Meters, Tooltip("양발 높이차 이 값 초과 시 전체 IK weight 감쇠 (안전 제한)")]
        private float _maxFootHeightDiff = 0.4f;
        [SerializeField, Meters, Tooltip("양발 높이차 이 값 이상이면 전체 IK weight = 0")]
        private float _footHeightDiffFadeOut = 0.7f;

        [Header("Foot Smoothing")]
        [SerializeField] private float _footPositionSmoothSpeed = 15f;
        [SerializeField] private float _normalSmoothSpeed = 12f;
        [SerializeField] private float _confidenceSmoothSpeed = 8f;

        [Header("Hip Correction")]
        [SerializeField] private float _hipDropSpeed = 10f;
        [SerializeField, Meters] private float _maxHipDrop = 0.35f;
        [SerializeField, Meters] private float _maxHipRaise = 0.15f;

        private Animator _animator;
        private Transform _leftFoot;
        private Transform _rightFoot;

        private float _targetIKWeight;
        private float _currentIKWeight;
        private float _currentHipDrop;

        private FootState _leftState;
        private FootState _rightState;

        private void Awake()
        {
            _animator = GetComponentInChildren<Animator>();
            _leftFoot = _animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            _rightFoot = _animator.GetBoneTransform(HumanBodyBones.RightFoot);

            _leftState.SmoothedNormal = Vector3.up;
            _rightState.SmoothedNormal = Vector3.up;
            _leftState.SmoothedConfidence = 1f;
            _rightState.SmoothedConfidence = 1f;
        }

        /// <summary>
        /// IK 활성/비활성 설정. 상태 전환 시 호출.
        /// </summary>
        public void SetIKActive(bool active)
        {
            _targetIKWeight = active ? 1f : 0f;
        }

        private void OnAnimatorIK(int layerIndex)
        {
            float dt = Time.deltaTime;
            if (dt < Mathf.Epsilon) return;

            _currentIKWeight = Mathf.MoveTowards(_currentIKWeight, _targetIKWeight, dt * _ikBlendSpeed);

            if (_currentIKWeight < 0.01f)
            {
                ClearAllIK();
                FadeOutHipDrop(dt);
                FadeOutFootStates(dt);
                return;
            }

            float leftFootBottom = _animator.leftFeetBottomHeight;
            float rightFootBottom = _animator.rightFeetBottomHeight;

            GroundSample leftSample = ProbeGround(_leftFoot);
            GroundSample rightSample = ProbeGround(_rightFoot);

            // 양발 높이차 안전 제한 (다리 길이 초과 방지)
            float globalWeight = CalcHeightDiffWeight(leftSample, rightSample);

            // Per-foot confidence weight — 경계에 걸친 발만 개별적으로 감쇠
            float leftConfidence = SmoothConfidence(leftSample, ref _leftState, dt);
            float rightConfidence = SmoothConfidence(rightSample, ref _rightState, dt);

            float leftWeight = _currentIKWeight * globalWeight * leftConfidence;
            float rightWeight = _currentIKWeight * globalWeight * rightConfidence;

            if (leftWeight < 0.01f && rightWeight < 0.01f)
            {
                ClearAllIK();
                FadeOutHipDrop(dt);
                FadeOutFootStates(dt);
                return;
            }

            // Hip 보정 (confidence 가중 — 경계 발은 hip 보정에도 영향 축소)
            SolveHipDrop(leftSample, rightSample, leftConfidence, rightConfidence, dt);

            ApplyFootIK(AvatarIKGoal.LeftFoot, leftSample, leftWeight,
                leftFootBottom, ref _leftState, dt);
            ApplyFootIK(AvatarIKGoal.RightFoot, rightSample, rightWeight,
                rightFootBottom, ref _rightState, dt);
        }

        /// <summary>
        /// confidence를 프레임 간 보간하여 IK weight 변화를 부드럽게.
        /// </summary>
        private float SmoothConfidence(GroundSample sample, ref FootState state, float dt)
        {
            float target = sample.HasHit ? sample.Confidence : 0f;
            float blend = 1f - Mathf.Exp(-_confidenceSmoothSpeed * dt);
            state.SmoothedConfidence = Mathf.Lerp(state.SmoothedConfidence, target, blend);
            return state.SmoothedConfidence;
        }

        /// <summary>
        /// 3점 샘플링(중앙/toe/heel)으로 지면 탐지 + 경계 감지 + 꼭지점 보정.
        /// toe/heel 높이차가 크면 경계에 걸쳐있으므로 confidence를 낮춘다.
        /// 중앙이 toe/heel보다 높으면 꼭지점 위이므로 groundY를 아래로 보정한다.
        /// </summary>
        private GroundSample ProbeGround(Transform footBone)
        {
            float baseY = Mathf.Max(footBone.position.y, transform.position.y) + _raycastOriginY;
            float distance = baseY - (transform.position.y + _raycastEndY);

            Vector3 footPos = footBone.position;
            Vector3 centerOrigin = new Vector3(footPos.x, baseY, footPos.z);

            // 발 방향 (XZ 평면) — toe/heel 오프셋 방향
            Vector3 footFwd = footBone.forward;
            footFwd.y = 0f;
            if (footFwd.sqrMagnitude < 0.01f) footFwd = transform.forward;
            footFwd.Normalize();

            // 3점 샘플링
            bool centerHit = TryGroundCast(centerOrigin, distance,
                out float centerY, out Vector3 centerNormal);

            Vector3 toeOrigin = new Vector3(
                footPos.x + footFwd.x * _toeOffset, baseY,
                footPos.z + footFwd.z * _toeOffset);
            bool toeHit = TryGroundCast(toeOrigin, distance,
                out float toeY, out Vector3 toeNormal);

            Vector3 heelOrigin = new Vector3(
                footPos.x - footFwd.x * _heelOffset, baseY,
                footPos.z - footFwd.z * _heelOffset);
            bool heelHit = TryGroundCast(heelOrigin, distance,
                out float heelY, out Vector3 heelNormal);

            int hitCount = (centerHit ? 1 : 0) + (toeHit ? 1 : 0) + (heelHit ? 1 : 0);
            if (hitCount == 0) return GroundSample.Miss;

            float groundY;
            Vector3 normal;
            float confidence;

            if (centerHit)
            {
                // 중앙(발 바로 아래)에 지면이 있음 → IK를 완전히 유지 (confidence = 1)
                // 발이 뜨는 것보다 지면에 붙어있는 게 항상 나음
                confidence = 1f;

                // 법선 평균
                normal = centerNormal;
                int normalCount = 1;
                if (toeHit) { normal += toeNormal; normalCount++; }
                if (heelHit) { normal += heelNormal; normalCount++; }
                normal = (normal / normalCount).normalized;

                // 꼭지점 보정: 중앙이 모든 주변보다 높으면 메쉬 정점/능선 위
                // → groundY를 주변 높이로 내려서 발이 뜨지 않게 함
                float maxPeripheralY = float.MinValue;
                bool hasPeripheral = false;
                bool allLower = true;
                if (toeHit)
                {
                    maxPeripheralY = Mathf.Max(maxPeripheralY, toeY);
                    hasPeripheral = true;
                    if (toeY >= centerY - _vertexThreshold) allLower = false;
                }
                if (heelHit)
                {
                    maxPeripheralY = Mathf.Max(maxPeripheralY, heelY);
                    hasPeripheral = true;
                    if (heelY >= centerY - _vertexThreshold) allLower = false;
                }

                if (hasPeripheral && allLower)
                    groundY = maxPeripheralY; // 꼭지점: 주변 중 높은 쪽으로 보정
                else
                    groundY = centerY;
            }
            else
            {
                // 중앙 미스 — 발 아래 지면 불확실, 주변 폴백 + confidence 감소
                if (toeHit && heelHit)
                {
                    groundY = Mathf.Max(toeY, heelY);
                    normal = Vector3.Slerp(toeNormal, heelNormal, 0.5f);
                    confidence = 0.5f;
                }
                else if (toeHit)
                {
                    groundY = toeY;
                    normal = toeNormal;
                    confidence = 0.3f;
                }
                else
                {
                    groundY = heelY;
                    normal = heelNormal;
                    confidence = 0.3f;
                }
            }

            return new GroundSample(true, groundY, normal, confidence);
        }

        /// <summary>
        /// 단일 지점 지면 탐지.
        /// Raycast로 정밀 높이, SphereCast로 엣지 감지 + 법선 보강.
        /// </summary>
        private bool TryGroundCast(Vector3 origin, float distance, out float groundY, out Vector3 normal)
        {
            bool rayDidHit = Physics.Raycast(origin, Vector3.down, out RaycastHit rayHit, distance,
                _groundLayers, QueryTriggerInteraction.Ignore)
                && rayHit.normal.y >= _minGroundNormalY;

            bool sphereDidHit = false;
            RaycastHit sphereHit = default;
            if (_sphereCastRadius > 0f)
            {
                float sphereDistance = distance - _sphereCastRadius;
                sphereDidHit = sphereDistance > 0f &&
                    Physics.SphereCast(origin, _sphereCastRadius, Vector3.down,
                        out sphereHit, sphereDistance, _groundLayers,
                        QueryTriggerInteraction.Ignore)
                    && sphereHit.normal.y >= _minGroundNormalY;
            }

            if (rayDidHit)
            {
                groundY = rayHit.point.y;
                normal = sphereDidHit
                    ? Vector3.Slerp(rayHit.normal, sphereHit.normal, 0.5f)
                    : rayHit.normal;
                return true;
            }

            if (sphereDidHit)
            {
                Vector3 refinedOrigin = sphereHit.point + Vector3.up * 0.1f;
                if (Physics.Raycast(refinedOrigin, Vector3.down, out RaycastHit refineHit, 0.3f,
                        _groundLayers, QueryTriggerInteraction.Ignore))
                {
                    groundY = refineHit.point.y;
                }
                else
                {
                    groundY = sphereHit.point.y;
                }
                normal = sphereHit.normal;
                return true;
            }

            groundY = 0f;
            normal = Vector3.up;
            return false;
        }

        /// <summary>
        /// 양발 높이차 안전 제한. 다리 길이를 초과하는 극단적 높이차에서만 전체 IK 감쇠.
        /// </summary>
        private float CalcHeightDiffWeight(GroundSample left, GroundSample right)
        {
            if (!left.HasHit || !right.HasHit) return 1f;

            float diff = Mathf.Abs(left.GroundY - right.GroundY);
            if (diff <= _maxFootHeightDiff) return 1f;
            if (diff >= _footHeightDiffFadeOut) return 0f;

            return 1f - (diff - _maxFootHeightDiff) / (_footHeightDiffFadeOut - _maxFootHeightDiff);
        }

        /// <summary>
        /// 히트 결과를 IK에 적용. 위치·법선 모두 프레임 간 보간.
        /// </summary>
        private void ApplyFootIK(
            AvatarIKGoal goal, GroundSample sample, float weight,
            float footBottomHeight,
            ref FootState state, float dt)
        {
            if (!sample.HasHit)
            {
                if (state.WasValid)
                {
                    float fadeBlend = 1f - Mathf.Exp(-_footPositionSmoothSpeed * 0.5f * dt);
                    state.SmoothedGroundY = Mathf.Lerp(state.SmoothedGroundY,
                        transform.position.y, fadeBlend);
                    state.SmoothedNormal = Vector3.Slerp(state.SmoothedNormal, Vector3.up, fadeBlend);

                    if (Mathf.Abs(state.SmoothedGroundY - transform.position.y) < 0.005f)
                    {
                        state.WasValid = false;
                        _animator.SetIKPositionWeight(goal, 0f);
                        _animator.SetIKRotationWeight(goal, 0f);
                        return;
                    }

                    _animator.SetIKPositionWeight(goal, weight);
                    _animator.SetIKRotationWeight(goal, weight);

                    Vector3 fadePos = _animator.GetIKPosition(goal);
                    fadePos.y = state.SmoothedGroundY + footBottomHeight;
                    _animator.SetIKPosition(goal, fadePos);
                    ApplyFootRotation(goal, state.SmoothedNormal);
                    return;
                }

                _animator.SetIKPositionWeight(goal, 0f);
                _animator.SetIKRotationWeight(goal, 0f);
                return;
            }

            float posBlend = 1f - Mathf.Exp(-_footPositionSmoothSpeed * dt);
            float normalBlend = 1f - Mathf.Exp(-_normalSmoothSpeed * dt);

            if (state.WasValid)
            {
                state.SmoothedGroundY = Mathf.Lerp(state.SmoothedGroundY, sample.GroundY, posBlend);
                state.SmoothedNormal = Vector3.Slerp(state.SmoothedNormal, sample.Normal, normalBlend);
            }
            else
            {
                state.SmoothedGroundY = sample.GroundY;
                state.SmoothedNormal = sample.Normal;
            }

            state.WasValid = true;

            _animator.SetIKPositionWeight(goal, weight);
            _animator.SetIKRotationWeight(goal, weight);

            Vector3 ikPos = _animator.GetIKPosition(goal);
            ikPos.y = state.SmoothedGroundY + footBottomHeight;
            _animator.SetIKPosition(goal, ikPos);

            ApplyFootRotation(goal, state.SmoothedNormal);
        }

        /// <summary>
        /// 지면 법선에 맞춘 발 회전 적용.
        /// </summary>
        private void ApplyFootRotation(AvatarIKGoal goal, Vector3 groundNormal)
        {
            Quaternion footRot = _animator.GetIKRotation(goal);
            Vector3 footUp = footRot * Vector3.up;
            Vector3 axis = Vector3.Cross(footUp, groundNormal);
            if (axis.sqrMagnitude > 1e-6f)
            {
                float angle = Vector3.Angle(footUp, groundNormal);
                _animator.SetIKRotation(goal, Quaternion.AngleAxis(angle, axis) * footRot);
            }
        }

        /// <summary>
        /// 양발 지면 높이를 고려한 hip 보정.
        /// confidence가 낮은 발(경계)의 영향을 축소하여 안정된 발 기준으로 보정.
        /// </summary>
        private void SolveHipDrop(
            GroundSample left, GroundSample right,
            float leftConfidence, float rightConfidence, float dt)
        {
            float rootY = transform.position.y;

            // confidence 가중 오프셋 — 경계에 걸친 발은 hip에 미치는 영향 축소
            float leftOffset = left.HasHit ? (left.GroundY - rootY) * leftConfidence : 0f;
            float rightOffset = right.HasHit ? (right.GroundY - rootY) * rightConfidence : 0f;

            float targetDrop;
            bool leftValid = left.HasHit && leftConfidence > 0.1f;
            bool rightValid = right.HasHit && rightConfidence > 0.1f;

            if (leftValid && rightValid)
            {
                float lower = Mathf.Min(leftOffset, rightOffset);
                float upper = Mathf.Max(leftOffset, rightOffset);
                float heightDiff = upper - lower;
                float midpoint = (lower + upper) * 0.5f;

                float midBlend = Mathf.Clamp01(heightDiff / _maxHipDrop);
                targetDrop = Mathf.Lerp(lower, midpoint, midBlend);
            }
            else if (leftValid)
                targetDrop = leftOffset;
            else if (rightValid)
                targetDrop = rightOffset;
            else
                targetDrop = 0f;

            targetDrop = Mathf.Clamp(targetDrop, -_maxHipDrop, _maxHipRaise);

            float blend = 1f - Mathf.Exp(-_hipDropSpeed * dt);
            _currentHipDrop = Mathf.Lerp(_currentHipDrop, targetDrop, blend);
            _animator.bodyPosition += Vector3.up * _currentHipDrop;
        }

        private void ClearAllIK()
        {
            _animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0f);
            _animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0f);
            _animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 0f);
            _animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 0f);
        }

        private void FadeOutHipDrop(float dt)
        {
            if (Mathf.Abs(_currentHipDrop) > 0.001f)
            {
                float blend = 1f - Mathf.Exp(-_hipDropSpeed * dt);
                _currentHipDrop = Mathf.Lerp(_currentHipDrop, 0f, blend);
                _animator.bodyPosition += Vector3.up * _currentHipDrop;
            }
        }

        private void FadeOutFootStates(float dt)
        {
            FadeOutSingleFootState(ref _leftState, dt);
            FadeOutSingleFootState(ref _rightState, dt);
        }

        private void FadeOutSingleFootState(ref FootState state, float dt)
        {
            if (!state.WasValid) return;

            float blend = 1f - Mathf.Exp(-_footPositionSmoothSpeed * dt);
            state.SmoothedGroundY = Mathf.Lerp(state.SmoothedGroundY, transform.position.y, blend);
            state.SmoothedNormal = Vector3.Slerp(state.SmoothedNormal, Vector3.up, blend);
            state.SmoothedConfidence = Mathf.Lerp(state.SmoothedConfidence, 1f, blend);

            if (Mathf.Abs(state.SmoothedGroundY - transform.position.y) < 0.005f)
            {
                state.WasValid = false;
                state.SmoothedGroundY = 0f;
                state.SmoothedNormal = Vector3.up;
                state.SmoothedConfidence = 1f;
            }
        }
    }
}
