using Animancer;
using KinematicCharacterController;
using UnityEngine;

namespace UPlayGround.Component
{
    /// <summary>
    /// Foot IK — 각 발에서 레이를 쏴서 지면에 부착, 골반 하강 및 상체 기울기 보정.
    ///
    /// [동작 원리]
    /// 1. KCC 수평 속도 기준으로 정지 여부 판단 → IK weight 자동 블렌딩
    ///    - 이동 중: weight → 0 (발이 바닥에 끌리지 않음)
    ///    - 정지 시: weight → 1
    /// 2. 각 발 위치에서 아래로 Raycast → 지면 높이(groundY) + 법선 획득
    /// 3. 두 발 delta 중 하강 방향만 반영 → 골반(hip) 자연스럽게 내림
    /// 4. 두 발 법선 평균 → bodyRotation으로 상체 기울기 보정
    /// 5. 각 발을 groundY + footBottomHeight 위치로 IK Position/Rotation 설정
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

        [Header("Weight Blending")]
        [SerializeField, Tooltip("IK weight 변화 속도. 높을수록 on/off 전환이 빠름.")]
        private float _blendSpeed = 15f;
        [SerializeField, Tooltip("이 수평속도(m/s) 이하일 때 IK 활성 후보.")]
        private float _idleSpeedThreshold = 0.1f;
        [SerializeField, Tooltip("정지 판정까지 대기 시간(초). 루트모션 순간 속도 진동 필터링용.")]
        private float _idleDelay = 0.15f;

        private Animator                 _animator;
        private KinematicCharacterMotor  _motor;

        private float      _weight;
        private bool       _wasActive;   // IK 활성화 첫 프레임 감지용
        private float      _idleTimer;   // 정지 유지 시간 누적

        // 외부(상태머신 등)에서 IK를 강제 비활성화. true면 weight를 0으로 블렌딩.
        public bool ForceDisabled;

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

            // 수평 속도로 정지 여부 판단 → targetWeight 결정
            // motor가 없으면 항상 활성 (fallback)
            if (ForceDisabled)
            {
                _weight    = Mathf.MoveTowards(_weight, 0f, dt * _blendSpeed);
                _idleTimer = 0f;
                SetFootWeight(AvatarIKGoal.LeftFoot,  _weight);
                SetFootWeight(AvatarIKGoal.RightFoot, _weight);
                _hipOffset     = Mathf.Lerp(_hipOffset, 0f, 1f - Mathf.Exp(-_smoothSpeed * dt));
                _bodyRotOffset = Quaternion.Slerp(_bodyRotOffset, Quaternion.identity, 1f - Mathf.Exp(-_smoothSpeed * dt));
                if (Mathf.Abs(_hipOffset) > 0.001f)
                    _animator.bodyPosition += Vector3.up * _hipOffset;
                if (_weight < 0.01f) _wasActive = false;
                return;
            }

            float targetWeight = 1f;
            if (_motor != null)
            {
                bool isGrounded = _motor.GroundingStatus.IsStableOnGround;
                Vector3 hVel    = Vector3.ProjectOnPlane(_motor.BaseVelocity, _motor.CharacterUp);
                bool isIdle     = isGrounded && hVel.magnitude <= _idleSpeedThreshold;

                if (isIdle)
                {
                    // 정지 상태가 _idleDelay 초 이상 유지돼야 IK 활성.
                    // 루트모션 순간 진동(수십ms)은 타이머를 채우지 못해 필터링됨.
                    _idleTimer += dt;
                }
                else
                {
                    // 이동 감지 즉시 타이머 리셋 → targetWeight = 0
                    _idleTimer = 0f;
                }

                targetWeight = _idleTimer >= _idleDelay ? 1f : 0f;
            }

            _weight = Mathf.MoveTowards(_weight, targetWeight, dt * _blendSpeed);

            float t = 1f - Mathf.Exp(-_smoothSpeed * dt);

            // weight = 0 수렴: hip/tilt 복원 후 종료
            // _wasActive = false로 세팅해서, 다음 활성화 시 snap이 동작하도록 함
            if (_weight < 0.01f)
            {
                SetFootWeight(AvatarIKGoal.LeftFoot,  0f);
                SetFootWeight(AvatarIKGoal.RightFoot, 0f);
                _hipOffset     = Mathf.Lerp(_hipOffset, 0f, t);
                _bodyRotOffset = Quaternion.Slerp(_bodyRotOffset, Quaternion.identity, t);
                if (Mathf.Abs(_hipOffset) > 0.001f)
                    _animator.bodyPosition += Vector3.up * _hipOffset;
                _wasActive = false;
                return;
            }

            Vector3 leftAnimPos  = _animator.GetIKPosition(AvatarIKGoal.LeftFoot);
            Vector3 rightAnimPos = _animator.GetIKPosition(AvatarIKGoal.RightFoot);
            float   rootY        = transform.position.y;

            // === 1) 각 발에서 아래로 레이캐스트 ===
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

            // === 2) 목표 발 높이 ===
            float leftTargetY  = leftHit  ? leftGroundY  + _footBottomHeight : leftAnimPos.y;
            float rightTargetY = rightHit ? rightGroundY + _footBottomHeight : rightAnimPos.y;

            // === 3) IK 활성화 첫 프레임: footY를 현재 애니메이션 발 위치로 snap ===
            // 직전 비활성 구간의 낡은 값에서 스무딩이 시작되는 것을 방지
            if (!_wasActive)
            {
                _leftFootY   = leftAnimPos.y;
                _rightFootY  = rightAnimPos.y;
                _leftNormal  = Vector3.up;
                _rightNormal = Vector3.up;
                _hipOffset   = 0f;
                _wasActive   = true;
            }

            // === 4) 발 Y / 법선 스무딩 ===
            _leftFootY  = Mathf.Lerp(_leftFootY,  leftTargetY,  t);
            _rightFootY = Mathf.Lerp(_rightFootY, rightTargetY, t);

            _leftNormal = Vector3.Slerp(_leftNormal,
                leftHit  && leftNorm.y  >= _minNormalY ? leftNorm  : Vector3.up, t);
            _rightNormal = Vector3.Slerp(_rightNormal,
                rightHit && rightNorm.y >= _minNormalY ? rightNorm : Vector3.up, t);

            // === 5) 골반 보정: 하강 방향 delta만 반영 ===
            // 이유: 한 발이 계단 위에 올라가도 골반을 올려선 안 됨. 반드시 내리거나 유지만.
            float leftDelta  = leftTargetY  - leftAnimPos.y;
            float rightDelta = rightTargetY - rightAnimPos.y;

            float hipTarget = 0f;
            if      (leftHit && rightHit) hipTarget = Mathf.Min(leftDelta, rightDelta, 0f);
            else if (leftHit)             hipTarget = Mathf.Min(leftDelta,  0f);
            else if (rightHit)            hipTarget = Mathf.Min(rightDelta, 0f);

            hipTarget  = Mathf.Max(hipTarget, -_maxHipDrop);
            _hipOffset = Mathf.Lerp(_hipOffset, hipTarget, t);
            _animator.bodyPosition += Vector3.up * _hipOffset;

            // === 6) 상체 기울기 보정 ===
            // 두 발 모두 닿았고 법선 차이가 완만할 때만 적용
            Quaternion targetBodyRot = Quaternion.identity;
            if (leftHit && rightHit &&
                Vector3.Angle(_leftNormal, _rightNormal) <= _maxNormalDiffAngle)
            {
                targetBodyRot = CalculateBodyTilt((_leftNormal + _rightNormal).normalized);
            }
            _bodyRotOffset         = Quaternion.Slerp(_bodyRotOffset, targetBodyRot, t);
            _animator.bodyRotation = _bodyRotOffset * _animator.bodyRotation;

            // === 7) 발 IK 적용 ===
            ApplyFoot(AvatarIKGoal.LeftFoot,  leftAnimPos,  _leftFootY,  _leftNormal);
            ApplyFoot(AvatarIKGoal.RightFoot, rightAnimPos, _rightFootY, _rightNormal);
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

        private void ApplyFoot(AvatarIKGoal goal, Vector3 animPos, float targetY, Vector3 normal)
        {
            _animator.SetIKPositionWeight(goal, _weight);
            _animator.SetIKRotationWeight(goal, _weight);
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
                $"[FootIK] w={_weight:F2} hip={_hipOffset:F3}\n" +
                $"L={(_dbgLeftDidHit ? _dbgLeftHit.y.ToString("F3") : "miss")} " +
                $"R={(_dbgRightDidHit ? _dbgRightHit.y.ToString("F3") : "miss")}");
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
