using Animancer.Units;
using UnityEngine;

namespace UPlayGround.Component
{
    /// <summary>
    /// Foot IK + Hip 보정 컴포넌트.
    /// OnAnimatorIK 콜백으로 양발을 지면에 밀착시키고, 골반 높이를 자동 보정한다.
    /// 
    /// [핵심 원리]
    /// - 캐릭터 루트(transform.position.y) 기준 수직 레이캐스트로 각 발 아래 지면 높이를 탐지
    /// - 지면과 애니메이션 발 높이의 차이(offset)를 IK로 보정
    /// - 양발 중 더 낮은 쪽에 맞춰 골반(hip)을 내려 무릎이 자연스럽게 구부러지게 함
    /// - 이동 중에는 IK weight를 0으로 페이드하여 애니메이션과 충돌하지 않음
    /// </summary>
    public class FootIKController : MonoBehaviour
    {
        [Header("Raycast")]
        [SerializeField, Meters] private float _raycastOriginY = 0.5f;
        [SerializeField, Meters] private float _raycastEndY = -0.5f;
        [SerializeField] private LayerMask _groundLayers;
        [SerializeField, Tooltip("지면으로 인정할 최소 법선 Y값 (계단 측면 필터링)")]
        private float _minGroundNormalY = 0.7f;

        [Header("IK Blend")]
        [SerializeField] private float _ikBlendSpeed = 10f;

        [Header("Hip Correction")]
        [SerializeField] private float _hipDropSpeed = 10f;
        [SerializeField, Meters] private float _maxHipDrop = 0.35f;

        private Animator _animator;
        private Transform _leftFoot;
        private Transform _rightFoot;

        private float _targetIKWeight;
        private float _currentIKWeight;
        private float _currentHipDrop;

        private void Awake()
        {
            _animator = GetComponentInChildren<Animator>();
            _leftFoot = _animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            _rightFoot = _animator.GetBoneTransform(HumanBodyBones.RightFoot);
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
                return;
            }

            float leftFootBottom = _animator.leftFeetBottomHeight;
            float rightFootBottom = _animator.rightFeetBottomHeight;

            float leftOffset = SolveFootIK(
                _leftFoot, AvatarIKGoal.LeftFoot, _currentIKWeight, leftFootBottom);

            float rightOffset = SolveFootIK(
                _rightFoot, AvatarIKGoal.RightFoot, _currentIKWeight, rightFootBottom);

            SolveHipDrop(leftOffset, rightOffset, dt);
        }

        /// <summary>
        /// 개별 발 IK. 지면까지의 오프셋(음수=발 아래에 지면)을 반환.
        /// 레이 히트 실패 시 0을 반환하고 IK weight를 0으로 설정한다.
        /// </summary>
        private float SolveFootIK(
            Transform footBone,
            AvatarIKGoal goal,
            float weight,
            float footBottomHeight)
        {
            // 캐릭터 루트 높이 + 오프셋에서 발의 XZ 위치로 수직 하향 레이캐스트
            Vector3 rayOrigin = new Vector3(
                footBone.position.x,
                transform.position.y + _raycastOriginY,
                footBone.position.z);

            float distance = _raycastOriginY - _raycastEndY;

            if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, distance,
                    _groundLayers, QueryTriggerInteraction.Ignore)
                || hit.normal.y < _minGroundNormalY)
            {
                _animator.SetIKPositionWeight(goal, 0f);
                _animator.SetIKRotationWeight(goal, 0f);
                return 0f;
            }

            _animator.SetIKPositionWeight(goal, weight);
            _animator.SetIKRotationWeight(goal, weight);

            // 발바닥 높이를 고려한 목표 위치
            _animator.SetIKPosition(goal, hit.point + Vector3.up * footBottomHeight);

            // 지면 법선에 맞춘 발 회전
            Quaternion footRot = _animator.GetIKRotation(goal);
            Vector3 footUp = footRot * Vector3.up;
            Vector3 axis = Vector3.Cross(footUp, hit.normal);
            float angle = Vector3.Angle(footUp, hit.normal);
            _animator.SetIKRotation(goal, Quaternion.AngleAxis(angle, axis) * footRot);

            return hit.point.y - transform.position.y;
        }

        /// <summary>
        /// 양발 오프셋 중 더 낮은 쪽에 맞춰 골반을 내린다.
        /// 이렇게 해야 낮은 쪽 다리의 무릎이 자연스럽게 구부러진다.
        /// </summary>
        private void SolveHipDrop(float leftOffset, float rightOffset, float dt)
        {
            // 두 발 모두 지면을 찾았으면 더 낮은 쪽, 아니면 찾은 쪽 기준
            float targetDrop;
            bool hasLeft = leftOffset != 0f;
            bool hasRight = rightOffset != 0f;

            if (hasLeft && hasRight)
                targetDrop = Mathf.Min(leftOffset, rightOffset);
            else if (hasLeft)
                targetDrop = leftOffset;
            else if (hasRight)
                targetDrop = rightOffset;
            else
                targetDrop = 0f;

            targetDrop = Mathf.Clamp(targetDrop, -_maxHipDrop, 0f);

            _currentHipDrop = Mathf.Lerp(_currentHipDrop, targetDrop, dt * _hipDropSpeed);
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
                _currentHipDrop = Mathf.MoveTowards(_currentHipDrop, 0f, dt * _hipDropSpeed);
                _animator.bodyPosition += Vector3.up * _currentHipDrop;
            }
        }
    }
}
