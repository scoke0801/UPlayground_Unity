using Animancer;
using UnityEngine;

namespace UPlayGround.Component
{
    /// <summary>
    /// Foot IK — 각 발에서 레이를 쏴서 지면에 부착, 골반 하강 및 상체 기울기 보정.
    ///
    /// [동작 원리]
    /// 1. 각 발 위치에서 아래로 Raycast → 지면 높이(groundY) + 법선 획득
    /// 2. 두 발 delta 중 하강 방향만 반영 → 골반(hip) 자연스럽게 내림
    /// 3. 두 발 법선 평균 → bodyRotation으로 상체 기울기 보정
    /// 4. 각 발을 groundY + footBottomHeight 위치로 IK Position/Rotation 설정
    /// </summary>
    public class FootIKController : MonoBehaviour
    {
        [Header("Raycast")]
        [SerializeField] private LayerMask _groundLayers;
        [SerializeField] private float _rayOriginHeight = 0.5f;
        [SerializeField] private float _rayLength = 1.5f;

        [Header("IK")]
        [SerializeField] private float _footBottomHeight = 0.08f;
        [SerializeField] private float _smoothSpeed = 12f;
        [SerializeField] private float _maxHipDrop = 0.5f;
        [SerializeField, Tooltip("상체 기울기 최대 각도 (도)")]
        private float _maxBodyTiltAngle = 15f;
        [SerializeField, Tooltip("두 발 법선 차이가 이 각도 이하일 때만 상체 기울기 적용 (뾰족한 장애물/급경사 필터링)")]
        private float _maxNormalDiffAngle = 30f;
        [SerializeField, Tooltip("발 회전 보정 최소 법선 Y (미만이면 수직 사용)")]
        private float _minNormalY = 0.5f;
        [SerializeField, Tooltip("발이 지면 착지 위치에서 이 높이 이상 올라가면 IK 가중치 0 (스윙 페이즈 자동 감지, 커브 불필요)")]
        private float _maxLiftHeight = 0.15f;

        private Animator _animator;

        private float _hipOffset;
        private Quaternion _bodyRotOffset = Quaternion.identity;

        private float _leftFootY, _rightFootY;
        private Vector3 _leftNormal = Vector3.up;
        private Vector3 _rightNormal = Vector3.up;
        private float _leftIKWeight = 1f, _rightIKWeight = 1f;
        private bool _initialized;

        // 디버그
        private bool _ikCalled;
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

            // Animator가 자식 GameObject에 있으면 OnAnimatorIK가 이 컴포넌트에서 호출되지 않음
            // → 릴레이 컴포넌트를 Animator의 GO에 추가하여 콜백 전달
            if (_animator.gameObject != gameObject)
            {
                var existing = _animator.gameObject.GetComponent<FootIKRelay>();
                if (existing == null)
                {
                    var relay = _animator.gameObject.AddComponent<FootIKRelay>();
                    relay.Owner = this;
                }
                else
                {
                    existing.Owner = this;
                }
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

        // Animator가 같은 GO일 때 직접 호출됨
        private void OnAnimatorIK(int layerIndex) => ProcessFootIK();

        /// <summary>릴레이에서도 호출 가능하도록 internal.</summary>
        internal void ProcessFootIK()
        {
            float dt = Time.deltaTime;
            if (dt < Mathf.Epsilon || _animator == null) return;

            _ikCalled = true;

            float t = 1f - Mathf.Exp(-_smoothSpeed * dt);

            Vector3 leftAnimPos  = _animator.GetIKPosition(AvatarIKGoal.LeftFoot);
            Vector3 rightAnimPos = _animator.GetIKPosition(AvatarIKGoal.RightFoot);
            float   rootY        = transform.position.y;

            // 첫 프레임: 스무딩 시작점을 현재 애니메이션 발 위치로 초기화
            if (!_initialized)
            {
                _leftFootY   = leftAnimPos.y;
                _rightFootY  = rightAnimPos.y;
                _initialized = true;
            }

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

            // === 2) 목표 발 높이 (bone Y = groundY + footBottom) ===
            float leftTargetY  = leftHit  ? leftGroundY  + _footBottomHeight : leftAnimPos.y;
            float rightTargetY = rightHit ? rightGroundY + _footBottomHeight : rightAnimPos.y;

            // === 3) 골반 보정: 하강 방향 delta만 반영 ===
            // 이유: 한 발이 계단 위에 올라가도 골반을 올려선 안 됨. 반드시 내리거나 유지만.
            float leftDelta  = leftTargetY  - leftAnimPos.y;
            float rightDelta = rightTargetY - rightAnimPos.y;

            float hipTarget = 0f;
            if (leftHit && rightHit)
                hipTarget = Mathf.Min(leftDelta, rightDelta, 0f);
            else if (leftHit)
                hipTarget = Mathf.Min(leftDelta, 0f);
            else if (rightHit)
                hipTarget = Mathf.Min(rightDelta, 0f);

            hipTarget  = Mathf.Max(hipTarget, -_maxHipDrop);
            _hipOffset = Mathf.Lerp(_hipOffset, hipTarget, t);
            _animator.bodyPosition += Vector3.up * _hipOffset;

            // === 4) 발 Y 스무딩 ===
            _leftFootY  = Mathf.Lerp(_leftFootY,  leftTargetY,  t);
            _rightFootY = Mathf.Lerp(_rightFootY, rightTargetY, t);

            // === 5) 법선 스무딩 ===
            _leftNormal = Vector3.Slerp(_leftNormal,
                leftHit  && leftNorm.y  >= _minNormalY ? leftNorm  : Vector3.up, t);
            _rightNormal = Vector3.Slerp(_rightNormal,
                rightHit && rightNorm.y >= _minNormalY ? rightNorm : Vector3.up, t);

            // === 6) 상체 기울기 보정 ===
            // 두 발 모두 닿았고 법선 차이가 완만할 때만 적용. 뾰족한 장애물/급경사 필터링.
            Quaternion targetBodyRot = Quaternion.identity;
            if (leftHit && rightHit &&
                Vector3.Angle(_leftNormal, _rightNormal) <= _maxNormalDiffAngle)
            {
                targetBodyRot = CalculateBodyTilt((_leftNormal + _rightNormal).normalized);
            }
            _bodyRotOffset        = Quaternion.Slerp(_bodyRotOffset, targetBodyRot, t);
            _animator.bodyRotation = _bodyRotOffset * _animator.bodyRotation;

            // === 7) 발 IK Position/Rotation 적용 ===
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

        /// <summary>
        /// 지면 평균 법선 → 상체 기울기 Quaternion.
        /// _maxBodyTiltAngle로 최대 기울기 클램프.
        /// </summary>
        private Quaternion CalculateBodyTilt(Vector3 avgNormal)
        {
            Vector3 axis = Vector3.Cross(Vector3.up, avgNormal);
            if (axis.sqrMagnitude < 1e-6f) return Quaternion.identity;

            float angle = Mathf.Min(Vector3.Angle(Vector3.up, avgNormal), _maxBodyTiltAngle);
            return Quaternion.AngleAxis(angle, axis.normalized);
        }

        private void ApplyFoot(AvatarIKGoal goal, Vector3 animPos, float targetY, Vector3 normal)
        {
            _animator.SetIKPositionWeight(goal, 1f);
            _animator.SetIKRotationWeight(goal, 1f);

            _animator.SetIKPosition(goal, new Vector3(animPos.x, targetY, animPos.z));

            // 지면 법선에 맞춘 발 회전
            Quaternion rot  = _animator.GetIKRotation(goal);
            Vector3    axis = Vector3.Cross(rot * Vector3.up, normal);
            if (axis.sqrMagnitude > 1e-6f)
                _animator.SetIKRotation(goal,
                    Quaternion.AngleAxis(Vector3.Angle(rot * Vector3.up, normal), axis) * rot);
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
                $"[FootIK] hip={_hipOffset:F3}\n" +
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
