using UnityEngine;
using Animancer;
using KinematicCharacterController;

public class FootIKHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AnimancerComponent _animancer;
    [SerializeField] private KinematicCharacterMotor _motor;

    [Header("Settings")]
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private float _footOffset = 0.1f;
    [SerializeField] private float _lerpSpeed = 15f;
    
    private Animator _animator;
    private Vector3 _leftFootIKPos, _rightFootIKPos;
    private Quaternion _leftFootIKRot, _rightFootIKRot;
    private float _currentIKWeight;

    void Awake() => _animator = _animancer.Animator;

    // Animancer는 매 프레임 Animator의 IK 패스를 호출합니다.
    private void OnAnimatorIK(int layerIndex)
    {
        // 1. KCC가 지면에 있을 때만 IK 가중치를 높임
        float targetWeight = _motor.GroundingStatus.IsStableOnGround ? 1f : 0f;
        _currentIKWeight = Mathf.Lerp(_currentIKWeight, targetWeight, Time.deltaTime * _lerpSpeed);

        if (_currentIKWeight <= 0.001f) return;

        // 2. 가중치 적용
        ApplyWeights(AvatarIKGoal.LeftFoot, _currentIKWeight);
        ApplyWeights(AvatarIKGoal.RightFoot, _currentIKWeight);

        // 3. 발 위치/회전 계산 및 적용
        SolveFootIK(AvatarIKGoal.LeftFoot, ref _leftFootIKPos, ref _leftFootIKRot);
        SolveFootIK(AvatarIKGoal.RightFoot, ref _rightFootIKPos, ref _rightFootIKRot);

        // 4. 골반 높이 보정 (발 계산 이후 수행)
        AdjustPelvisHeight();
    }
    
    private void AdjustPelvisHeight()
    {
        // 양발 중 더 낮은 곳의 오프셋을 계산
        float lOffset = _leftFootIKPos.y - transform.position.y;
        float rOffset = _rightFootIKPos.y - transform.position.y;
        float targetPelvisOffset = Mathf.Min(lOffset, rOffset);

        // 골반 위치 이동 (Lerp를 사용하여 부드럽게)
        if (targetPelvisOffset < 0)
        {
            Vector3 pelvisPos = _animator.bodyPosition;
            pelvisPos.y += targetPelvisOffset;
            _animator.bodyPosition = pelvisPos;
        }
    }
    private void SolveFootIK(AvatarIKGoal goal, ref Vector3 lastPos, ref Quaternion lastRot)
    {
        Vector3 origin = _animator.GetIKPosition(goal) + Vector3.up;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 1.5f, _groundLayer))
        {
            // 목표 위치 계산
            Vector3 targetPos = hit.point + (Vector3.up * _footOffset);
            // 목표 회전 계산 (지면 노멀 반영)
            Quaternion targetRot = Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, hit.normal), hit.normal);

            // 부드러운 이동을 위해 Lerp 사용
            lastPos = Vector3.Lerp(lastPos, targetPos, Time.deltaTime * _lerpSpeed);
            lastRot = Quaternion.Slerp(lastRot, targetRot, Time.deltaTime * _lerpSpeed);

            _animator.SetIKPosition(goal, lastPos);
            _animator.SetIKRotation(goal, lastRot);
        }
    }

    private void ApplyWeights(AvatarIKGoal goal, float weight)
    {
        _animator.SetIKPositionWeight(goal, weight);
        _animator.SetIKRotationWeight(goal, weight);
    }
}