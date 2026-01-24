using UnityEngine;
using Animancer;

public class FootIKHandler  : MonoBehaviour
{
    [Header("IK Settings")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float raycastDistance = 1.5f;
    [SerializeField] private float footOffset = 0.1f;
    [SerializeField] [Range(0f, 1f)] private float ikWeight = 1f;
    
    [Header("Pelvis Adjustment")]
    [SerializeField] private bool adjustPelvisHeight = true;
    [SerializeField] private float pelvisAdjustmentSpeed = 5f;
    [SerializeField] private float pelvisOffset = 0f; // 골반 추가 오프셋
    
    [Header("Foot Rotation Limits")]
    [SerializeField] private float maxFootAngle = 45f;
    [SerializeField] private float footRotationSpeed = 10f;
    
    private Animator animator;
    private Vector3 rightFootPosition, leftFootPosition;
    private Quaternion rightFootRotation, leftFootRotation;
    private float rightFootIKWeight, leftFootIKWeight;
    
    // 골반 높이 조정을 위한 변수
    private float currentPelvisOffset;
    private float leftFootDistance, rightFootDistance;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (animator == null) return;

        // 1단계: 양쪽 발의 지면까지 거리 측정
        MeasureFootDistances();

        // 2단계: 골반 높이 조정 (더 낮은 발 기준)
        if (adjustPelvisHeight)
        {
            AdjustPelvisHeight();
        }

        // 3단계: 발 IK 적용
        ProcessFootIK(AvatarIKGoal.LeftFoot, ref leftFootPosition, ref leftFootRotation, ref leftFootIKWeight, leftFootDistance);
        ProcessFootIK(AvatarIKGoal.RightFoot, ref rightFootPosition, ref rightFootRotation, ref rightFootIKWeight, rightFootDistance);
    }

    void MeasureFootDistances()
    {
        // 왼발 거리 측정
        leftFootDistance = MeasureFootToGround(AvatarIKGoal.LeftFoot);
        
        // 오른발 거리 측정
        rightFootDistance = MeasureFootToGround(AvatarIKGoal.RightFoot);
    }

    float MeasureFootToGround(AvatarIKGoal foot)
    {
        Vector3 footPos = animator.GetIKPosition(foot);
        RaycastHit hit;
        Vector3 rayStart = footPos + Vector3.up * 0.5f;
        
        if (Physics.Raycast(rayStart, Vector3.down, out hit, raycastDistance + 0.5f, groundLayer))
        {
            // 지면까지의 거리 (음수면 발이 지면보다 위, 양수면 아래)
            return footPos.y - (hit.point.y + footOffset);
        }
        
        return 0f;
    }

    void AdjustPelvisHeight()
    {
        // 더 낮은 발 찾기 (더 큰 distance 값 = 더 많이 내려가야 함)
        float targetOffset = Mathf.Max(leftFootDistance, rightFootDistance);
        
        // 발이 지면보다 위에 있을 때만 골반을 내림
        if (targetOffset > 0f)
        {
            targetOffset = -targetOffset + pelvisOffset;
            
            // 부드러운 전환
            currentPelvisOffset = Mathf.Lerp(currentPelvisOffset, targetOffset, Time.deltaTime * pelvisAdjustmentSpeed);
            
            // bodyPosition 조정
            Vector3 bodyPos = animator.bodyPosition;
            bodyPos.y += currentPelvisOffset;
            animator.bodyPosition = bodyPos;
        }
        else
        {
            // 지면에 닿았으면 원래 위치로 복귀
            currentPelvisOffset = Mathf.Lerp(currentPelvisOffset, 0f, Time.deltaTime * pelvisAdjustmentSpeed);
            
            if (Mathf.Abs(currentPelvisOffset) > 0.001f)
            {
                Vector3 bodyPos = animator.bodyPosition;
                bodyPos.y += currentPelvisOffset;
                animator.bodyPosition = bodyPos;
            }
        }
    }

    void ProcessFootIK(AvatarIKGoal foot, ref Vector3 footPosition, ref Quaternion footRotation, ref float footWeight, float footDistance)
    {
        Vector3 footOriginalPos = animator.GetIKPosition(foot);
        RaycastHit hit;
        Vector3 rayStart = footOriginalPos + Vector3.up * 0.5f;
        
        if (Physics.Raycast(rayStart, Vector3.down, out hit, raycastDistance + 0.5f, groundLayer))
        {
            // 발 위치 계산
            footPosition = hit.point + Vector3.up * footOffset;
            footWeight = ikWeight;
            
            // 발 회전 계산
            footRotation = CalculateSafeFootRotation(hit.normal);
            
            // IK 적용
            animator.SetIKPositionWeight(foot, footWeight);
            animator.SetIKPosition(foot, footPosition);
            
            animator.SetIKRotationWeight(foot, footWeight);
            animator.SetIKRotation(foot, footRotation);
        }
        else
        {
            // 지면이 감지되지 않으면 IK 비활성화
            footWeight = Mathf.Lerp(footWeight, 0f, Time.deltaTime * footRotationSpeed);
            animator.SetIKPositionWeight(foot, footWeight);
            animator.SetIKRotationWeight(foot, footWeight);
        }
    }

    Quaternion CalculateSafeFootRotation(Vector3 normal)
    {
        // 지면 노멀 기반 회전 계산
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, normal).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(forward, normal);
        
        // 캐릭터 기본 회전
        Quaternion characterRotation = transform.rotation;
        
        // 각도 제한 적용
        float angle = Quaternion.Angle(characterRotation, targetRotation);
        
        if (angle > maxFootAngle)
        {
            targetRotation = Quaternion.Slerp(characterRotation, targetRotation, maxFootAngle / angle);
        }
        
        return targetRotation;
    }

    // 디버그 시각화
    void OnDrawGizmosSelected()
    {
        if (animator == null) return;

        // 왼발 레이캐스트
        DrawFootRaycast(AvatarIKGoal.LeftFoot, Color.green);
        
        // 오른발 레이캐스트
        DrawFootRaycast(AvatarIKGoal.RightFoot, Color.red);
        
        // 골반 위치 표시
        if (adjustPelvisHeight)
        {
            Vector3 bodyPos = animator.bodyPosition;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(bodyPos, 0.1f);
            
            // 골반 오프셋 표시
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(bodyPos, bodyPos + Vector3.down * Mathf.Abs(currentPelvisOffset));
        }
    }

    void DrawFootRaycast(AvatarIKGoal foot, Color color)
    {
        Vector3 footPos = animator.GetIKPosition(foot);
        Vector3 rayStart = footPos + Vector3.up * 0.5f;
        
        Gizmos.color = color;
        Gizmos.DrawLine(rayStart, rayStart + Vector3.down * (raycastDistance + 0.5f));
        Gizmos.DrawWireSphere(footPos, 0.05f);
    }
}