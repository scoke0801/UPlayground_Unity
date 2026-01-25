using UnityEngine;
using Animancer;

public class FootIkHandler : MonoBehaviour
{
    [Header("IK Settings")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float raycastDistance = 1.5f;
    [SerializeField] private float footOffset = 0.1f;
    [SerializeField] [Range(0f, 1f)] private float ikWeight = 1f;
    
    [Header("Pelvis Adjustment")]
    [SerializeField] private bool adjustPelvisHeight = true;
    [SerializeField] private float pelvisAdjustmentSpeed = 5f;
    [SerializeField] private float pelvisOffset = 0f;
    
    [Header("Foot Planting Detection")]
    [SerializeField] private bool useAnimationCurves = true;
    [SerializeField] private string leftFootCurveName = "LeftFootIK";  // 애니메이션 커브 이름
    [SerializeField] private string rightFootCurveName = "RightFootIK";
    [SerializeField] private float plantThreshold = 0.5f; // 커브 값이 이 이상일 때 접지 상태로 판단
    
    [Header("Grounding Check")]
    [SerializeField] private float groundCheckDistance = 0.3f; // 발이 이 거리 안에 있어야 IK 적용
    [SerializeField] private float ikBlendSpeed = 10f; // IK 가중치 블렌딩 속도
    
    [Header("Foot Rotation Limits")]
    [SerializeField] private float maxFootAngle = 45f;
    [SerializeField] private float footRotationSpeed = 10f;
    
    private Animator animator;
    private Vector3 rightFootPosition, leftFootPosition;
    private Quaternion rightFootRotation, leftFootRotation;
    private float rightFootIKWeight, leftFootIKWeight;
    
    private float currentPelvisOffset;
    private float leftFootDistance, rightFootDistance;
    
    // 발 접지 상태
    private float leftFootPlantWeight;
    private float rightFootPlantWeight;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (animator == null) return;

        // 1단계: 발 접지 상태 확인
        UpdateFootPlantingWeights();

        // 2단계: 양쪽 발의 지면까지 거리 측정
        MeasureFootDistances();

        // 3단계: 골반 높이 조정 (접지된 발만 고려)
        if (adjustPelvisHeight)
        {
            AdjustPelvisHeight();
        }

        // 4단계: 발 IK 적용 (접지된 발만)
        ProcessFootIK(AvatarIKGoal.LeftFoot, ref leftFootPosition, ref leftFootRotation, 
                     ref leftFootIKWeight, leftFootDistance, leftFootPlantWeight);
        ProcessFootIK(AvatarIKGoal.RightFoot, ref rightFootPosition, ref rightFootRotation, 
                     ref rightFootIKWeight, rightFootDistance, rightFootPlantWeight);
    }

    void UpdateFootPlantingWeights()
    {
        if (useAnimationCurves)
        {
            // 애니메이션 커브에서 발 접지 가중치 가져오기
            float leftCurve = animator.GetFloat(leftFootCurveName);
            float rightCurve = animator.GetFloat(rightFootCurveName);
            
            // 커브 값이 threshold 이상이면 접지 상태
            leftFootPlantWeight = Mathf.Lerp(leftFootPlantWeight, 
                leftCurve >= plantThreshold ? 1f : 0f, 
                Time.deltaTime * ikBlendSpeed);
            
            rightFootPlantWeight = Mathf.Lerp(rightFootPlantWeight, 
                rightCurve >= plantThreshold ? 1f : 0f, 
                Time.deltaTime * ikBlendSpeed);
        }
        else
        {
            // 애니메이션 커브 없이 Raycast만으로 판단
            leftFootPlantWeight = IsFootGrounded(AvatarIKGoal.LeftFoot) ? 1f : 0f;
            rightFootPlantWeight = IsFootGrounded(AvatarIKGoal.RightFoot) ? 1f : 0f;
        }
    }

    bool IsFootGrounded(AvatarIKGoal foot)
    {
        Vector3 footPos = animator.GetIKPosition(foot);
        RaycastHit hit;
        Vector3 rayStart = footPos + Vector3.up * 0.5f;
        
        // 발이 지면 근처에 있는지 확인
        if (Physics.Raycast(rayStart, Vector3.down, out hit, groundCheckDistance + 0.5f, groundLayer))
        {
            float distanceToGround = footPos.y - hit.point.y;
            return distanceToGround <= groundCheckDistance;
        }
        
        return false;
    }

    void MeasureFootDistances()
    {
        leftFootDistance = MeasureFootToGround(AvatarIKGoal.LeftFoot);
        rightFootDistance = MeasureFootToGround(AvatarIKGoal.RightFoot);
    }

    float MeasureFootToGround(AvatarIKGoal foot)
    {
        Vector3 footPos = animator.GetIKPosition(foot);
        RaycastHit hit;
        Vector3 rayStart = footPos + Vector3.up * 0.5f;
        
        if (Physics.Raycast(rayStart, Vector3.down, out hit, raycastDistance + 0.5f, groundLayer))
        {
            return footPos.y - (hit.point.y + footOffset);
        }
        
        return 0f;
    }

    void AdjustPelvisHeight()
    {
        // 접지된 발만 고려해서 골반 높이 조정
        float leftOffset = leftFootPlantWeight > 0.5f ? leftFootDistance : 0f;
        float rightOffset = rightFootPlantWeight > 0.5f ? rightFootDistance : 0f;
        
        // 둘 다 접지되지 않았으면 골반 조정 안 함
        if (leftFootPlantWeight < 0.5f && rightFootPlantWeight < 0.5f)
        {
            currentPelvisOffset = Mathf.Lerp(currentPelvisOffset, 0f, Time.deltaTime * pelvisAdjustmentSpeed);
            return;
        }
        
        // 접지된 발 중 더 낮은 발 기준
        float targetOffset = 0f;
        if (leftFootPlantWeight > 0.5f && rightFootPlantWeight > 0.5f)
        {
            targetOffset = Mathf.Max(leftOffset, rightOffset);
        }
        else if (leftFootPlantWeight > 0.5f)
        {
            targetOffset = leftOffset;
        }
        else if (rightFootPlantWeight > 0.5f)
        {
            targetOffset = rightOffset;
        }
        
        if (targetOffset > 0f)
        {
            targetOffset = -targetOffset + pelvisOffset;
            currentPelvisOffset = Mathf.Lerp(currentPelvisOffset, targetOffset, Time.deltaTime * pelvisAdjustmentSpeed);
            
            Vector3 bodyPos = animator.bodyPosition;
            bodyPos.y += currentPelvisOffset;
            animator.bodyPosition = bodyPos;
        }
        else
        {
            currentPelvisOffset = Mathf.Lerp(currentPelvisOffset, 0f, Time.deltaTime * pelvisAdjustmentSpeed);
            
            if (Mathf.Abs(currentPelvisOffset) > 0.001f)
            {
                Vector3 bodyPos = animator.bodyPosition;
                bodyPos.y += currentPelvisOffset;
                animator.bodyPosition = bodyPos;
            }
        }
    }

    void ProcessFootIK(AvatarIKGoal foot, ref Vector3 footPosition, ref Quaternion footRotation, 
                      ref float footWeight, float footDistance, float plantWeight)
    {
        // 접지 상태가 아니면 IK 비활성화
        if (plantWeight < 0.1f)
        {
            footWeight = Mathf.Lerp(footWeight, 0f, Time.deltaTime * ikBlendSpeed);
            animator.SetIKPositionWeight(foot, footWeight);
            animator.SetIKRotationWeight(foot, footWeight);
            return;
        }
        
        Vector3 footOriginalPos = animator.GetIKPosition(foot);
        RaycastHit hit;
        Vector3 rayStart = footOriginalPos + Vector3.up * 0.5f;
        
        if (Physics.Raycast(rayStart, Vector3.down, out hit, raycastDistance + 0.5f, groundLayer))
        {
            footPosition = hit.point + Vector3.up * footOffset;
            footRotation = CalculateSafeFootRotation(hit.normal);
            
            // 접지 가중치를 고려한 IK 가중치
            float targetWeight = ikWeight * plantWeight;
            footWeight = Mathf.Lerp(footWeight, targetWeight, Time.deltaTime * ikBlendSpeed);
            
            animator.SetIKPositionWeight(foot, footWeight);
            animator.SetIKPosition(foot, footPosition);
            
            animator.SetIKRotationWeight(foot, footWeight);
            animator.SetIKRotation(foot, footRotation);
        }
        else
        {
            footWeight = Mathf.Lerp(footWeight, 0f, Time.deltaTime * ikBlendSpeed);
            animator.SetIKPositionWeight(foot, footWeight);
            animator.SetIKRotationWeight(foot, footWeight);
        }
    }

    Quaternion CalculateSafeFootRotation(Vector3 normal)
    {
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, normal).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(forward, normal);
        Quaternion characterRotation = transform.rotation;
        
        float angle = Quaternion.Angle(characterRotation, targetRotation);
        
        if (angle > maxFootAngle)
        {
            targetRotation = Quaternion.Slerp(characterRotation, targetRotation, maxFootAngle / angle);
        }
        
        return targetRotation;
    }

    void OnDrawGizmosSelected()
    {
        if (animator == null) return;

        // 왼발
        DrawFootDebug(AvatarIKGoal.LeftFoot, Color.green, leftFootPlantWeight);
        
        // 오른발
        DrawFootDebug(AvatarIKGoal.RightFoot, Color.red, rightFootPlantWeight);
        
        // 골반
        if (adjustPelvisHeight)
        {
            Vector3 bodyPos = animator.bodyPosition;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(bodyPos, 0.1f);
        }
    }

    void DrawFootDebug(AvatarIKGoal foot, Color color, float plantWeight)
    {
        Vector3 footPos = animator.GetIKPosition(foot);
        Vector3 rayStart = footPos + Vector3.up * 0.5f;
        
        // 접지 상태에 따라 색상 조정
        Gizmos.color = Color.Lerp(Color.grey, color, plantWeight);
        Gizmos.DrawLine(rayStart, rayStart + Vector3.down * (raycastDistance + 0.5f));
        Gizmos.DrawWireSphere(footPos, 0.05f);
        
        // 접지 영역 표시
        Gizmos.color = new Color(color.r, color.g, color.b, 0.3f);
        Gizmos.DrawWireSphere(footPos, groundCheckDistance);
    }
}