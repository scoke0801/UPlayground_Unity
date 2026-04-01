using Animancer.Units;
using UnityEngine;

namespace UPlayGround.Component
{
    /// <summary>
    /// Foot IK + Hip 보정 컴포넌트.
    /// OnAnimatorIK 콜백으로 양발을 지면에 밀착시키고, 골반 높이를 자동 보정한다.
    ///
    /// [핵심 원리]
    /// - 발 본 위치와 캐릭터 루트 중 높은 쪽 기준으로 SphereCast하여 지면 탐지 (꼭지점 대응)
    /// - 다중 샘플링으로 지형 꼭지점/경계에서도 안정적 지면 높이 산출
    /// - 발 위치·법선 모두 프레임 간 보간하여 jitter 방지
    /// - 양발 높이차를 고려한 hip 보정으로 양쪽 다리 모두 자연스럽게 구부러지게 함
    /// - 발 간 높이차가 다리 한계를 초과하면 IK weight를 점진적으로 줄여 애니메이션으로 복귀
    /// - 이동 중에는 IK weight를 0으로 페이드하여 애니메이션과 충돌하지 않음
    /// </summary>
    public class FootIKController : MonoBehaviour
    {
        private readonly struct GroundSample
        {
            public readonly bool HasHit;
            public readonly float GroundY;
            public readonly Vector3 Normal;

            public GroundSample(bool hasHit, float groundY, Vector3 normal)
            {
                HasHit = hasHit;
                GroundY = groundY;
                Normal = normal;
            }

            public static readonly GroundSample Miss = new GroundSample(false, 0f, Vector3.up);
        }

        /// <summary>
        /// 발별 보간 상태. 프레임 간 IK 위치/법선을 부드럽게 전환한다.
        /// </summary>
        private struct FootState
        {
            public float SmoothedGroundY;
            public Vector3 SmoothedNormal;
            public bool WasValid;
        }

        [Header("Raycast")]
        [SerializeField, Meters] private float _raycastOriginY = 0.5f;
        [SerializeField, Meters] private float _raycastEndY = -0.75f;
        [SerializeField] private LayerMask _groundLayers;
        [SerializeField, Tooltip("지면으로 인정할 최소 법선 Y값")]
        private float _minGroundNormalY = 0.7f;

        [Header("Ground Probe")]
        [SerializeField, Meters, Tooltip("SphereCast 반경 (꼭지점/엣지 탐지 강화)")]
        private float _sphereCastRadius = 0.06f;
        [SerializeField, Meters, Tooltip("다중 샘플링 반경 — 발 주변 추가 탐색 거리")]
        private float _probeRadius = 0.08f;
        [SerializeField, Range(0, 8), Tooltip("추가 샘플 수 (0이면 중앙 1개만 사용)")]
        private int _probeSampleCount = 4;

        [Header("IK Blend")]
        [SerializeField] private float _ikBlendSpeed = 10f;
        [SerializeField, Meters, Tooltip("이 높이차를 초과하면 IK weight 감쇠 시작")]
        private float _maxFootHeightDiff = 0.4f;
        [SerializeField, Meters, Tooltip("이 높이차 이상이면 IK weight = 0")]
        private float _footHeightDiffFadeOut = 0.7f;

        [Header("Foot Smoothing")]
        [SerializeField, Tooltip("발 위치 보간 속도 (높을수록 빠르게 추종)")]
        private float _footPositionSmoothSpeed = 15f;
        [SerializeField, Tooltip("법선 벡터 보간 속도 (꼭지점 떨림 방지)")]
        private float _normalSmoothSpeed = 12f;

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

        // 다중 샘플링용 재사용 배열
        private Vector3[] _sampleOffsets;

        private void Awake()
        {
            _animator = GetComponentInChildren<Animator>();
            _leftFoot = _animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            _rightFoot = _animator.GetBoneTransform(HumanBodyBones.RightFoot);

            _leftState.SmoothedNormal = Vector3.up;
            _rightState.SmoothedNormal = Vector3.up;

            BuildSampleOffsets();
        }

        /// <summary>
        /// 발 주변 샘플링 오프셋을 미리 계산한다. (원형 배치)
        /// </summary>
        private void BuildSampleOffsets()
        {
            _sampleOffsets = new Vector3[1 + _probeSampleCount];
            _sampleOffsets[0] = Vector3.zero;

            for (int i = 0; i < _probeSampleCount; i++)
            {
                float angle = (360f / _probeSampleCount) * i * Mathf.Deg2Rad;
                _sampleOffsets[i + 1] = new Vector3(
                    Mathf.Cos(angle) * _probeRadius,
                    0f,
                    Mathf.Sin(angle) * _probeRadius);
            }
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

            // 양발 높이차 기반 IK weight 감쇠
            float heightDiffWeight = CalcHeightDiffWeight(leftSample, rightSample);
            float effectiveWeight = _currentIKWeight * heightDiffWeight;

            if (effectiveWeight < 0.01f)
            {
                ClearAllIK();
                FadeOutHipDrop(dt);
                FadeOutFootStates(dt);
                return;
            }

            // Hip 보정
            SolveHipDrop(leftSample, rightSample, dt);

            // 발 IK 적용 (IK는 월드 스페이스이므로 hip과 독립)
            ApplyFootIK(AvatarIKGoal.LeftFoot, leftSample, effectiveWeight,
                leftFootBottom, ref _leftState, dt);
            ApplyFootIK(AvatarIKGoal.RightFoot, rightSample, effectiveWeight,
                rightFootBottom, ref _rightState, dt);
        }

        /// <summary>
        /// 지면 탐지. 중앙(발 바로 아래)을 우선하고, 실패 시에만 주변 샘플로 폴백한다.
        /// 주변 샘플은 법선 평균에만 기여하고, 높이는 중앙 히트를 기준으로 한다.
        /// </summary>
        private GroundSample ProbeGround(Transform footBone)
        {
            float baseY = Mathf.Max(footBone.position.y, transform.position.y) + _raycastOriginY;
            float distance = baseY - (transform.position.y + _raycastEndY);

            Vector3 centerOrigin = new Vector3(footBone.position.x, baseY, footBone.position.z);

            // 1차: 중앙 샘플 (발 바로 아래)
            if (TryGroundCast(centerOrigin, distance, out float centerY, out Vector3 centerNormal))
            {
                // 주변 샘플로 법선만 보강 (높이는 중앙 기준 유지)
                Vector3 accNormal = centerNormal;
                int normalCount = 1;

                for (int i = 1; i < _sampleOffsets.Length; i++)
                {
                    Vector3 origin = new Vector3(
                        footBone.position.x + _sampleOffsets[i].x,
                        baseY,
                        footBone.position.z + _sampleOffsets[i].z);

                    if (TryGroundCast(origin, distance, out _, out Vector3 sideNormal))
                    {
                        accNormal += sideNormal;
                        normalCount++;
                    }
                }

                Vector3 avgNormal = (accNormal / normalCount).normalized;
                return new GroundSample(true, centerY, avgNormal);
            }

            // 2차: 중앙 실패 시 주변 샘플에서 가장 가까운(높은) 히트로 폴백
            float bestY = float.NegativeInfinity;
            Vector3 bestNormal = Vector3.up;
            bool found = false;

            for (int i = 1; i < _sampleOffsets.Length; i++)
            {
                Vector3 origin = new Vector3(
                    footBone.position.x + _sampleOffsets[i].x,
                    baseY,
                    footBone.position.z + _sampleOffsets[i].z);

                if (TryGroundCast(origin, distance, out float hitY, out Vector3 hitNormal))
                {
                    found = true;
                    if (hitY > bestY)
                    {
                        bestY = hitY;
                        bestNormal = hitNormal;
                    }
                }
            }

            return found ? new GroundSample(true, bestY, bestNormal) : GroundSample.Miss;
        }

        /// <summary>
        /// 단일 지점 지면 탐지. SphereCast 우선, 실패 시 Raycast 폴백.
        /// </summary>
        private bool TryGroundCast(Vector3 origin, float distance, out float groundY, out Vector3 normal)
        {
            // 1차: SphereCast — 넓은 영역으로 꼭지점/엣지 안정 탐지
            if (_sphereCastRadius > 0f)
            {
                float sphereDistance = distance - _sphereCastRadius;
                if (sphereDistance > 0f &&
                    Physics.SphereCast(origin, _sphereCastRadius, Vector3.down,
                        out RaycastHit sphereHit, sphereDistance, _groundLayers,
                        QueryTriggerInteraction.Ignore)
                    && sphereHit.normal.y >= _minGroundNormalY)
                {
                    // sphereHit.point는 콜라이더 표면 실제 접촉점 — 꼭지점/엣지에서도 정확
                    groundY = sphereHit.point.y;
                    normal = sphereHit.normal;
                    return true;
                }
            }

            // 2차: Raycast 폴백 — SphereCast가 놓칠 수 있는 좁은 틈
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit rayHit, distance,
                    _groundLayers, QueryTriggerInteraction.Ignore)
                && rayHit.normal.y >= _minGroundNormalY)
            {
                groundY = rayHit.point.y;
                normal = rayHit.normal;
                return true;
            }

            groundY = 0f;
            normal = Vector3.up;
            return false;
        }

        /// <summary>
        /// 양발 높이차가 클 때 IK weight를 점진적으로 줄인다.
        /// 다리 길이를 초과하는 높이차에서 부자연스러운 스트레칭 방지.
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
        /// 히트 결과를 IK에 적용. 위치·법선 모두 프레임 간 보간하여 안정성 확보.
        /// IK 위치는 월드 스페이스이므로 hip 보정과 독립적으로 지면에 고정한다.
        /// </summary>
        private void ApplyFootIK(
            AvatarIKGoal goal, GroundSample sample, float weight,
            float footBottomHeight,
            ref FootState state, float dt)
        {
            if (!sample.HasHit)
            {
                // 지면을 잃었을 때 — 이전 상태가 있으면 서서히 페이드아웃
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

            // 위치·법선 보간
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

            // 발 위치: 보간된 지면 높이 + 발바닥 높이 (IK는 월드 스페이스이므로 hip과 독립)
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
        /// 낮은 발에 맞춰 내리되, 높이차가 과도하면 중간점으로 이동하여
        /// 양쪽 다리가 모두 자연스럽게 도달하도록 한다.
        /// </summary>
        private void SolveHipDrop(GroundSample left, GroundSample right, float dt)
        {
            float rootY = transform.position.y;
            float leftOffset = left.HasHit ? left.GroundY - rootY : 0f;
            float rightOffset = right.HasHit ? right.GroundY - rootY : 0f;

            float targetDrop;
            if (left.HasHit && right.HasHit)
            {
                float lower = Mathf.Min(leftOffset, rightOffset);
                float upper = Mathf.Max(leftOffset, rightOffset);
                float heightDiff = upper - lower;

                if (heightDiff > _maxHipDrop)
                    targetDrop = (lower + upper) * 0.5f;
                else
                    targetDrop = lower;
            }
            else if (left.HasHit)
                targetDrop = leftOffset;
            else if (right.HasHit)
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

        /// <summary>
        /// IK 비활성 시 발 상태도 서서히 초기화
        /// </summary>
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

            if (Mathf.Abs(state.SmoothedGroundY - transform.position.y) < 0.005f)
            {
                state.WasValid = false;
                state.SmoothedGroundY = 0f;
                state.SmoothedNormal = Vector3.up;
            }
        }
    }
}
