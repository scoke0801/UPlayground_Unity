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
    /// 2. ForceDisabled = true 시 전체 weight를 0으로 페이드 (공격 등 명시적 비활성화)
    /// 3. 각 발 위치에서 아래로 Raycast → 지면 높이(groundY) + 법선 획득
    /// 4. 지면 Y - 루트 Y 기준 → 골반(hip) 보정 (평지에서 가라앉음 없음)
    /// 5. 두 발 법선 평균 → bodyRotation으로 상체 기울기 보정
    /// </summary>
    public class FootIKController : MonoBehaviour
    {
        [Header("Raycast")]
        [SerializeField] private LayerMask _groundLayers;
        [SerializeField] private float _rayOriginHeight = 0.5f;
        [SerializeField] private float _rayLength       = 1.5f;

        [Header("IK")]
        [SerializeField] private float _footBottomHeight = 0.08f;
        [SerializeField] private float _smoothSpeed      = 12f;
        [SerializeField] private float _maxHipDrop       = 0.5f;
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
        private float _footWeightSpeed   = 10f;
        [SerializeField, Tooltip("ForceDisabled 시 전체 weight 페이드 속도.")]
        private float _globalFadeSpeed   = 15f;

        private Animator                _animator;
        private KinematicCharacterMotor _motor;

        // 외부(상태머신 등)에서 IK를 강제 비활성화. true면 globalWeight를 0으로 페이드.
        public bool ForceDisabled;

        private float      _globalWeight = 1f; // ForceDisabled 페이드용
        private bool       _initialized;

        private float      _leftFootWeight;
        private float      _rightFootWeight;

        private float      _hipOffset;
        private Quaternion _bodyRotOffset = Quaternion.identity;

        private float   _leftFootY,  _rightFootY;
        private Vector3 _leftNormal  = Vector3.up;
        private Vector3 _rightNormal = Vector3.up;

        // 디버그
        private bool    _ikCalled;
        private Vector3 _dbgLeftOrigin,  _dbgRightOrigin;
        private Vector3 _dbgLeftHit,     _dbgRightHit;
        private bool    _dbgLeftDidHit,  _dbgRightDidHit;

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
                animancer.Layers[0].ApplyFootIK = false;
        }

        private void OnAnimatorIK(int layerIndex) => ProcessFootIK();

        internal void ProcessFootIK()
        {
            float dt = Time.deltaTime;
            if (dt < Mathf.Epsilon || _animator == null) return;

            _ikCalled = true;

            float smoothT      = 1f - Mathf.Exp(-_smoothSpeed    * dt);
            float footWeightDt = _footWeightSpeed * dt;

            // ─── 1) 발 애니메이션 위치 & 레이캐스트 ───
            Vector3 leftAnimPos  = _animator.GetIKPosition(AvatarIKGoal.LeftFoot);
            Vector3 rightAnimPos = _animator.GetIKPosition(AvatarIKGoal.RightFoot);
            float   rootY        = transform.position.y;

            bool leftHit  = FootRay(leftAnimPos,  rootY, out float leftGroundY,  out Vector3 leftNorm);
            bool rightHit = FootRay(rightAnimPos, rootY, out float rightGroundY, out Vector3 rightNorm);

#if UNITY_EDITOR
            _dbgLeftOrigin  = new Vector3(leftAnimPos.x,  rootY + _rayOriginHeight, leftAnimPos.z);
            _dbgRightOrigin = new Vector3(rightAnimPos.x, rootY + _rayOriginHeight, rightAnimPos.z);
            _dbgLeftDidHit  = leftHit;
            _dbgRightDidHit = rightHit;
            if (leftHit)  _dbgLeftHit  = new Vector3(leftAnimPos.x,  leftGroundY,  leftAnimPos.z);
            if (rightHit) _dbgRightHit = new Vector3(rightAnimPos.x, rightGroundY, rightAnimPos.z);
#endif

            float leftTargetY  = leftHit  ? leftGroundY  + _footBottomHeight : leftAnimPos.y;
            float rightTargetY = rightHit ? rightGroundY + _footBottomHeight : rightAnimPos.y;

            // ─── 2) 발 위치·법선 항상 추적 (IK 비활성 중에도) ───
            if (!_initialized)
            {
                _leftFootY   = leftTargetY;
                _rightFootY  = rightTargetY;
                _leftNormal  = leftHit  && leftNorm.y  >= _minNormalY ? leftNorm  : Vector3.up;
                _rightNormal = rightHit && rightNorm.y >= _minNormalY ? rightNorm : Vector3.up;
                _initialized = true;
            }
            else
            {
                _leftFootY  = Mathf.Lerp(_leftFootY,  leftTargetY,  smoothT);
                _rightFootY = Mathf.Lerp(_rightFootY, rightTargetY, smoothT);
                _leftNormal  = Vector3.Slerp(_leftNormal,
                    leftHit  && leftNorm.y  >= _minNormalY ? leftNorm  : Vector3.up, smoothT);
                _rightNormal = Vector3.Slerp(_rightNormal,
                    rightHit && rightNorm.y >= _minNormalY ? rightNorm : Vector3.up, smoothT);
            }

            // ─── 3) per-foot weight: 발이 지면 근처면 1, 공중이면 0 ───
            // 이동 중 swing phase는 자연스럽게 weight=0이 되어 애니메이션이 제어
            // 이동/정지 전환과 무관하므로 on/off 끊김 없음
            float targetLeftWeight  = leftHit
                ? Mathf.Clamp01(1f - (leftAnimPos.y  - leftTargetY)  / _footLiftThreshold)
                : 0f;
            float targetRightWeight = rightHit
                ? Mathf.Clamp01(1f - (rightAnimPos.y - rightTargetY) / _footLiftThreshold)
                : 0f;

            _leftFootWeight  = Mathf.MoveTowards(_leftFootWeight,  targetLeftWeight,  footWeightDt);
            _rightFootWeight = Mathf.MoveTowards(_rightFootWeight, targetRightWeight, footWeightDt);

            // ─── 4) ForceDisabled: 전체 weight 페이드 ───
            float targetGlobal = ForceDisabled ? 0f : 1f;
            _globalWeight = Mathf.MoveTowards(_globalWeight, targetGlobal, _globalFadeSpeed * dt);

            float appliedLeft  = _leftFootWeight  * _globalWeight;
            float appliedRight = _rightFootWeight * _globalWeight;

            // ─── 5) 골반 오프셋 추적 ───
            // 지면 자체가 루트보다 낮을 때만 내림 (평지에서 가라앉음 방지)
            float leftGroundDelta  = leftHit  ? Mathf.Min(leftGroundY  + _footBottomHeight - rootY, 0f) : 0f;
            float rightGroundDelta = rightHit ? Mathf.Min(rightGroundY + _footBottomHeight - rootY, 0f) : 0f;
            float hipTarget = 0f;
            if      (leftHit && rightHit) hipTarget = Mathf.Min(leftGroundDelta, rightGroundDelta);
            else if (leftHit)             hipTarget = leftGroundDelta;
            else if (rightHit)            hipTarget = rightGroundDelta;
            hipTarget  = Mathf.Max(hipTarget, -_maxHipDrop);
            _hipOffset = Mathf.Lerp(_hipOffset, hipTarget, smoothT);

            // ─── 6) 상체 기울기 오프셋 추적 ───
            Quaternion targetBodyRot = Quaternion.identity;
            if (leftHit && rightHit &&
                Vector3.Angle(_leftNormal, _rightNormal) <= _maxNormalDiffAngle)
                targetBodyRot = CalculateBodyTilt((_leftNormal + _rightNormal).normalized);
            _bodyRotOffset = Quaternion.Slerp(_bodyRotOffset, targetBodyRot, smoothT);

            // ─── 7) IK 적용 ───
            float avgWeight = (appliedLeft + appliedRight) * 0.5f;

            if (avgWeight > 0.001f)
            {
                _animator.bodyPosition += Vector3.up * _hipOffset * avgWeight;
                _animator.bodyRotation  = Quaternion.Slerp(Quaternion.identity, _bodyRotOffset, avgWeight)
                                          * _animator.bodyRotation;
            }

            SetFootWeight(AvatarIKGoal.LeftFoot,  appliedLeft);
            SetFootWeight(AvatarIKGoal.RightFoot, appliedRight);

            if (appliedLeft  > 0.001f)
                ApplyFootPosition(AvatarIKGoal.LeftFoot,  leftAnimPos,  _leftFootY,  _leftNormal);
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
                normal  = hit.normal;
                return true;
            }
            groundY = 0f;
            normal  = Vector3.up;
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

            Quaternion rot  = _animator.GetIKRotation(goal);
            Vector3    axis = Vector3.Cross(rot * Vector3.up, normal);
            if (axis.sqrMagnitude > 1e-6f)
                _animator.SetIKRotation(goal,
                    Quaternion.AngleAxis(Vector3.Angle(rot * Vector3.up, normal), axis) * rot);
        }

        private void SetFootWeight(AvatarIKGoal goal, float w)
        {
            _animator.SetIKPositionWeight(goal, w);
            _animator.SetIKRotationWeight(goal, w);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || _animator == null) return;

            if (!_ikCalled)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 1.5f, 0.3f);
                UnityEditor.Handles.Label(transform.position + Vector3.up * 2f,
                    "[FootIK] OnAnimatorIK 미호출!\nAnimator Layer에서 IK Pass를 확인하세요.");
                return;
            }

            Gizmos.color = _dbgLeftDidHit ? Color.green : Color.red;
            Gizmos.DrawLine(_dbgLeftOrigin, _dbgLeftOrigin + Vector3.down * _rayLength);
            if (_dbgLeftDidHit)
            {
                Gizmos.DrawWireSphere(_dbgLeftHit, 0.04f);
                Gizmos.DrawLine(_dbgLeftHit, _dbgLeftHit + _leftNormal * 0.2f);
            }

            Gizmos.color = _dbgRightDidHit ? new Color(0f, 0.8f, 1f) : Color.red;
            Gizmos.DrawLine(_dbgRightOrigin, _dbgRightOrigin + Vector3.down * _rayLength);
            if (_dbgRightDidHit)
            {
                Gizmos.DrawWireSphere(_dbgRightHit, 0.04f);
                Gizmos.DrawLine(_dbgRightHit, _dbgRightHit + _rightNormal * 0.2f);
            }

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_animator.bodyPosition, 0.05f);

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(_animator.bodyPosition,
                _animator.bodyPosition + (_leftNormal + _rightNormal).normalized * 0.3f);

            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f,
                $"[FootIK] global={_globalWeight:F2} hip={_hipOffset:F3}\n" +
                $"L={_leftFootWeight:F2} ({(_dbgLeftDidHit ? _dbgLeftHit.y.ToString("F3") : "miss")})  " +
                $"R={_rightFootWeight:F2} ({(_dbgRightDidHit ? _dbgRightHit.y.ToString("F3") : "miss")})");
        }
#endif
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
