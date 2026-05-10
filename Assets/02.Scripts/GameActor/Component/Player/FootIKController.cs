using Animancer;
using KinematicCharacterController;
using UnityEngine;

namespace UPlayGround.Component
{
    /// <summary>
    /// Foot IK — 각 발에서 레이를 쏴서 지면에 부착, 골반 하강 및 상체 기울기 보정.
    ///
    /// [동작 원리]
    /// 1. 각 발의 애니메이션 높이 vs 지면 높이로 per-foot IK weight 결정
    ///    - 발이 지면 근처 (stance): weight → 1  (IK가 발을 지형에 밀착)
    ///    - 발이 공중 (swing)      : weight → 0  (애니메이션이 자유롭게 제어)
    ///    → 이동/정지 전환과 무관하게 항상 부드럽게 동작
    /// 2. ForceDisabled = true 시 실제 IK weight는 0으로 두고, 내부 지면 목표는 계속 추적
    /// 3. 각 발 위치에서 아래로 Raycast → 지면 높이(groundY) + 법선 획득
    /// 4. 지면 Y - 루트 Y 기준 → 골반(hip) 보정 (평지에서 가라앉음 없음)
    /// 5. 두 발 법선 평균 → bodyRotation으로 상체 기울기 보정
    /// </summary>
    public class FootIKController : MonoBehaviour
    {
        [Header("Raycast")] [SerializeField] private LayerMask _groundLayers;
        [SerializeField] private float _rayOriginHeight = 0.5f;
        [SerializeField] private float _rayLength = 1.5f;

        [Header("IK")] [SerializeField] private float _footBottomHeight = 0.08f;
        [SerializeField] private float _smoothSpeed = 12f;
        [SerializeField] private float _maxHipDrop = 0.5f;

        [SerializeField, Tooltip("상체 기울기 최대 각도 (도)")]
        private float _maxBodyTiltAngle = 15f;

        [SerializeField, Tooltip("두 발 법선 차이가 이 각도 이하일 때만 상체 기울기 적용 (뾰족한 장애물/급경사 필터링)")]
        private float _maxNormalDiffAngle = 30f;

        [SerializeField, Tooltip("발 회전 보정 최소 법선 Y (미만이면 수직 사용)")]
        private float _minNormalY = 0.5f;

        [Header("Per-Foot Weight")]
        [SerializeField, Tooltip("발이 지면 목표보다 이 높이(m) 이상 들리면 IK weight=0.\n보행 중 swing phase를 자동으로 비활성화하는 임계값.")]
        private float _footLiftThreshold = 0.15f;

        [SerializeField, Tooltip("per-foot weight 변화 속도.")]
        private float _footWeightSpeed = 10f;

        [SerializeField, Tooltip("ForceDisabled 해제 후 전체 weight 페이드 인 속도.")]
        private float _globalFadeSpeed = 8f;

        [SerializeField, Tooltip("ForceDisabled 해제 직후 IK를 다시 켜기 전 대기 시간.\n0이면 weight는 즉시 페이드 인하고, 목표 발 위치만 별도 보간한다.")]
        private float _reenableDelay = 0f;

        [SerializeField, Tooltip("ForceDisabled 해제 직후 IK 목표 높이를 현재 애니메이션 발 높이에서 다시 추적하는 시간.")]
        private float _resumeTargetBlendTime = 0.12f;

        [SerializeField, Tooltip("ForceDisabled 해제 직후 골반/상체 보정 적용을 지연하는 시간.")]
        private float _bodyCorrectionDelay = 0.15f;

        private Animator _animator;
        private KinematicCharacterMotor _motor;

        // 외부(상태머신 등)에서 IK를 강제 비활성화.
        // 비대칭 페이드: 끄는 건 즉시(도약 시 발 stretch 방지), 켜는 건 지연 후 부드럽게(공격 종료 스냅 방지).
        public bool ForceDisabled
        {
            get => _forceDisabled;
            set => SetForceDisabled(value, _reenableDelay, _globalFadeSpeed);
        }

        private bool _forceDisabled;

        private float _globalWeight = 1f; // ForceDisabled 페이드용
        private float _currentGlobalFadeSpeed = 15f;
        private float _reenableTimer;
        private float _resumeTargetBlendTimer;
        private float _bodyCorrectionDelayTimer;
        private bool _initialized;

        private float _leftFootWeight;
        private float _rightFootWeight;

        private float _hipOffset;
        private Quaternion _bodyRotOffset = Quaternion.identity;

        private float _leftFootY, _rightFootY;
        private Vector3 _leftNormal = Vector3.up;
        private Vector3 _rightNormal = Vector3.up;

        // 디버그
        private Vector3 _dbgLeftOrigin, _dbgRightOrigin;
        private Vector3 _dbgLeftHit, _dbgRightHit;
        private bool _dbgLeftDidHit, _dbgRightDidHit;

        private void Awake()
        {
            _animator = GetComponentInChildren<Animator>();
            if (_animator == null)
            {
                Debug.LogError("[FootIK] Animator를 찾을 수 없습니다.", this);
                enabled = false;
                return;
            }

            _motor = GetComponentInParent<KinematicCharacterMotor>();
            if (_motor == null)
                Debug.LogWarning("[FootIK] KinematicCharacterMotor를 찾을 수 없습니다.", this);

            if (_animator.gameObject != gameObject)
            {
                var existing = _animator.gameObject.GetComponent<FootIKRelay>();
                if (existing == null)
                    _animator.gameObject.AddComponent<FootIKRelay>().Owner = this;
                else
                    existing.Owner = this;
            }
        }

        private void Start()
        {
            // Animancer 내장 Foot IK 비활성화 — 커스텀 IK와 충돌 방지
            // ActorAnimator.Awake()에서 ApplyFootIK = true로 설정하므로, Start에서 덮어씀
            var animancer = GetComponentInChildren<AnimancerComponent>();
            if (animancer != null && animancer.Layers.Count > 0)
                DisableAnimancerFootIK(animancer);
        }

        /// <summary>
        /// 모델 교체 시 호출. 이전 Relay를 정리하고 새 Animator에 Relay를 재등록한다.
        /// </summary>
        public void Refresh(Animator newAnimator)
        {
            if (newAnimator == null)
            {
                enabled = false;
                return;
            }

            // 기존 Relay 소유권 해제
            if (_animator != null && _animator.gameObject != gameObject)
            {
                var oldRelay = _animator.gameObject.GetComponent<FootIKRelay>();
                if (oldRelay != null) oldRelay.Owner = null;
            }

            _animator     = newAnimator;
            _initialized  = false;

            var animancer = _animator.GetComponent<AnimancerComponent>();
            if (animancer != null)
                DisableAnimancerFootIK(animancer);

            if (_animator.gameObject != gameObject)
            {
                var relay = _animator.gameObject.GetComponent<FootIKRelay>();
                if (relay == null) relay = _animator.gameObject.AddComponent<FootIKRelay>();
                relay.Owner = this;
            }

            enabled = true;
        }

        private void SetForceDisabled(bool value, float reenableDelay, float fadeSpeed)
        {
            if (_forceDisabled == value)
            {
                if (!value)
                {
                    _reenableTimer = Mathf.Max(_reenableTimer, reenableDelay);
                    _currentGlobalFadeSpeed = fadeSpeed;
                }
                return;
            }

            _forceDisabled = value;
            if (value)
            {
                // 즉시 비활성화. 내부 발 목표는 ProcessFootIK에서 계속 추적해 재활성 스냅을 줄인다.
                _globalWeight = 0f;
                _leftFootWeight = 0f;
                _rightFootWeight = 0f;
                _reenableTimer = 0f;
                _currentGlobalFadeSpeed = _globalFadeSpeed;
                _bodyCorrectionDelayTimer = 0f;
            }
            else
            {
                // false로 돌아갈 땐 현재 애니메이션 발 위치에서 지면 IK 목표로 다시 추적한다.
                _globalWeight = 0f;
                _reenableTimer = reenableDelay;
                _currentGlobalFadeSpeed = fadeSpeed;
                _resumeTargetBlendTimer = _resumeTargetBlendTime;
                _bodyCorrectionDelayTimer = _bodyCorrectionDelay;
                _initialized = false;
            }
        }

        private static void DisableAnimancerFootIK(AnimancerComponent animancer)
        {
            for (int i = 0; i < animancer.Layers.Count; i++)
                animancer.Layers[i].ApplyFootIK = false;
        }

        private void OnAnimatorIK(int layerIndex) => ProcessFootIK();

        internal void ProcessFootIK()
        {
            float dt = Time.deltaTime;
            if (dt < Mathf.Epsilon || _animator == null) return;

            float smoothT = 1f - Mathf.Exp(-_smoothSpeed * dt);
            float footWeightDt = _footWeightSpeed * dt;

            // ─── 1) 발 애니메이션 위치 & 레이캐스트 ───
            Vector3 leftAnimPos = _animator.GetIKPosition(AvatarIKGoal.LeftFoot);
            Vector3 rightAnimPos = _animator.GetIKPosition(AvatarIKGoal.RightFoot);
            float rootY = transform.position.y;

            bool leftHit = FootRay(leftAnimPos, rootY, out float leftGroundY, out Vector3 leftNorm);
            bool rightHit = FootRay(rightAnimPos, rootY, out float rightGroundY, out Vector3 rightNorm);

#if UNITY_EDITOR
            _dbgLeftOrigin = new Vector3(leftAnimPos.x, rootY + _rayOriginHeight, leftAnimPos.z);
            _dbgRightOrigin = new Vector3(rightAnimPos.x, rootY + _rayOriginHeight, rightAnimPos.z);
            _dbgLeftDidHit = leftHit;
            _dbgRightDidHit = rightHit;
            if (leftHit) _dbgLeftHit = new Vector3(leftAnimPos.x, leftGroundY, leftAnimPos.z);
            if (rightHit) _dbgRightHit = new Vector3(rightAnimPos.x, rightGroundY, rightAnimPos.z);
#endif

            float leftTargetY = leftHit ? leftGroundY + _footBottomHeight : leftAnimPos.y;
            float rightTargetY = rightHit ? rightGroundY + _footBottomHeight : rightAnimPos.y;

            // ─── 2) 발 위치·법선 항상 추적 (IK 비활성 중에도) ───
            if (!_initialized)
            {
                bool resumeBlend = !_forceDisabled && _resumeTargetBlendTimer > 0f;
                _leftFootY = resumeBlend ? leftAnimPos.y : leftTargetY;
                _rightFootY = resumeBlend ? rightAnimPos.y : rightTargetY;
                _leftNormal = leftHit && leftNorm.y >= _minNormalY ? leftNorm : Vector3.up;
                _rightNormal = rightHit && rightNorm.y >= _minNormalY ? rightNorm : Vector3.up;
                _initialized = true;
            }
            else
            {
                float targetSmoothT = smoothT;
                if (!_forceDisabled && _resumeTargetBlendTimer > 0f)
                {
                    _resumeTargetBlendTimer = Mathf.Max(0f, _resumeTargetBlendTimer - dt);
                    float blendSpeed = 1f / Mathf.Max(_resumeTargetBlendTime, 0.001f);
                    targetSmoothT = 1f - Mathf.Exp(-blendSpeed * dt);
                }

                _leftFootY = Mathf.Lerp(_leftFootY, leftTargetY, targetSmoothT);
                _rightFootY = Mathf.Lerp(_rightFootY, rightTargetY, targetSmoothT);
                _leftNormal = Vector3.Slerp(_leftNormal,
                    leftHit && leftNorm.y >= _minNormalY ? leftNorm : Vector3.up, smoothT);
                _rightNormal = Vector3.Slerp(_rightNormal,
                    rightHit && rightNorm.y >= _minNormalY ? rightNorm : Vector3.up, smoothT);
            }

            // ─── 3) per-foot weight: 발이 지면 근처면 1, 공중이면 0 ───
            // swing 판정은 "루트 대비 들림량"으로 측정 — 지면 기준이 아님!
            // 지면이 발보다 낮은 경우(단차/경계면)에도 IK가 살아있어야 발이 늘어가 부착됨.
            // 오직 애니메이터가 발을 stance 위로 실제로 들어올렸을 때만 weight를 낮춘다.
            float leftLift = leftAnimPos.y - rootY - _footBottomHeight;
            float rightLift = rightAnimPos.y - rootY - _footBottomHeight;
            float targetLeftWeight = leftHit
                ? Mathf.Clamp01(1f - Mathf.Max(0f, leftLift) / _footLiftThreshold)
                : 0f;
            float targetRightWeight = rightHit
                ? Mathf.Clamp01(1f - Mathf.Max(0f, rightLift) / _footLiftThreshold)
                : 0f;

            if (_forceDisabled)
            {
                // 비활성 중에는 지면 목표만 추적하고 weight는 0에 고정한다.
                // 공격 중 weight가 미리 1까지 차올랐다가 해제 프레임에 튀는 것을 막는다.
                _leftFootWeight = 0f;
                _rightFootWeight = 0f;
            }
            else
            {
                _leftFootWeight = Mathf.MoveTowards(_leftFootWeight, targetLeftWeight, footWeightDt);
                _rightFootWeight = Mathf.MoveTowards(_rightFootWeight, targetRightWeight, footWeightDt);
            }

            // ─── 4) ForceDisabled 해제 시 글로벌 weight 페이드 인 ───
            // 비활성 중에도 발 목표는 추적하되, 실제 IK weight는 0으로 유지한다.
            if (_forceDisabled)
            {
                _globalWeight = 0f;
            }
            else if (_reenableTimer > 0f)
            {
                _reenableTimer = Mathf.Max(0f, _reenableTimer - dt);
                _globalWeight = 0f;
            }
            else
            {
                _globalWeight = Mathf.MoveTowards(_globalWeight, 1f, _currentGlobalFadeSpeed * dt);
            }

            float appliedLeft = _leftFootWeight * _globalWeight;
            float appliedRight = _rightFootWeight * _globalWeight;

            // ─── 5) 골반 오프셋 추적 ───
            // 지면 자체가 루트보다 낮을 때만 내림 (평지에서 가라앉음 방지)
            float leftGroundDelta = leftHit ? Mathf.Min(leftGroundY + _footBottomHeight - rootY, 0f) : 0f;
            float rightGroundDelta = rightHit ? Mathf.Min(rightGroundY + _footBottomHeight - rootY, 0f) : 0f;
            float hipTarget = 0f;
            if (leftHit && rightHit) hipTarget = Mathf.Min(leftGroundDelta, rightGroundDelta);
            else if (leftHit) hipTarget = leftGroundDelta;
            else if (rightHit) hipTarget = rightGroundDelta;
            hipTarget = Mathf.Max(hipTarget, -_maxHipDrop);
            _hipOffset = Mathf.Lerp(_hipOffset, hipTarget, smoothT);

            // ─── 6) 상체 기울기 오프셋 추적 ───
            Quaternion targetBodyRot = Quaternion.identity;
            if (leftHit && rightHit &&
                Vector3.Angle(_leftNormal, _rightNormal) <= _maxNormalDiffAngle)
                targetBodyRot = CalculateBodyTilt((_leftNormal + _rightNormal).normalized);
            _bodyRotOffset = Quaternion.Slerp(_bodyRotOffset, targetBodyRot, smoothT);

            // ─── 7) IK 적용 ───
            float avgWeight = (appliedLeft + appliedRight) * 0.5f;

            float bodyWeight = avgWeight;
            if (_bodyCorrectionDelayTimer > 0f)
            {
                _bodyCorrectionDelayTimer = Mathf.Max(0f, _bodyCorrectionDelayTimer - dt);
                bodyWeight = 0f;
            }

            if (bodyWeight > 0.001f)
            {
                _animator.bodyPosition += Vector3.up * _hipOffset * bodyWeight;
                _animator.bodyRotation = Quaternion.Slerp(Quaternion.identity, _bodyRotOffset, bodyWeight)
                                         * _animator.bodyRotation;
            }

            SetFootWeight(AvatarIKGoal.LeftFoot, appliedLeft);
            SetFootWeight(AvatarIKGoal.RightFoot, appliedRight);

            if (appliedLeft > 0.001f)
                ApplyFootPosition(AvatarIKGoal.LeftFoot, leftAnimPos, _leftFootY, _leftNormal);
            if (appliedRight > 0.001f)
                ApplyFootPosition(AvatarIKGoal.RightFoot, rightAnimPos, _rightFootY, _rightNormal);
        }

        private bool FootRay(Vector3 footPos, float rootY, out float groundY, out Vector3 normal)
        {
            var origin = new Vector3(footPos.x, rootY + _rayOriginHeight, footPos.z);
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, _rayLength,
                    _groundLayers, QueryTriggerInteraction.Ignore))
            {
                groundY = hit.point.y;
                normal = hit.normal;
                return true;
            }

            groundY = 0f;
            normal = Vector3.up;
            return false;
        }

        private Quaternion CalculateBodyTilt(Vector3 avgNormal)
        {
            Vector3 axis = Vector3.Cross(Vector3.up, avgNormal);
            if (axis.sqrMagnitude < 1e-6f) return Quaternion.identity;
            float angle = Mathf.Min(Vector3.Angle(Vector3.up, avgNormal), _maxBodyTiltAngle);
            return Quaternion.AngleAxis(angle, axis.normalized);
        }

        private void ApplyFootPosition(AvatarIKGoal goal, Vector3 animPos, float targetY, Vector3 normal)
        {
            _animator.SetIKPosition(goal, new Vector3(animPos.x, targetY, animPos.z));

            Quaternion rot = _animator.GetIKRotation(goal);
            Vector3 axis = Vector3.Cross(rot * Vector3.up, normal);
            if (axis.sqrMagnitude > 1e-6f)
                _animator.SetIKRotation(goal,
                    Quaternion.AngleAxis(Vector3.Angle(rot * Vector3.up, normal), axis) * rot);
        }

        private void SetFootWeight(AvatarIKGoal goal, float w)
        {
            _animator.SetIKPositionWeight(goal, w);
            _animator.SetIKRotationWeight(goal, w);
        }
    }

    /// <summary>
    /// Animator가 FootIKController와 다른 GameObject에 있을 때
    /// OnAnimatorIK 콜백을 FootIKController로 전달하는 릴레이.
    /// </summary>
    internal class FootIKRelay : MonoBehaviour
    {
        internal FootIKController Owner;

        private void OnAnimatorIK(int layerIndex)
        {
            if (Owner != null && Owner.enabled)
                Owner.ProcessFootIK();
        }
    }
}
