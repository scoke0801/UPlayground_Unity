using UnityEngine;

/// <summary>
/// 이동 관련 설정 데이터
/// 걷기, 달리기, 점프, 구르기 등의 이동 파라미터
/// </summary>
[CreateAssetMenu(fileName = "New Movement Settings", menuName = "RPG/Character/Movement Settings")]
public class MovementSettingsData : ScriptableObject
{
    [Header("이동 속도")]
    [Tooltip("걷기 속도")]
    public float walkSpeed = 3f;
    [Tooltip("달리기 속도")]
    public float runSpeed = 6f;
    [Tooltip("회전 속도 (도/초)")]
    public float rotationSpeed = 360f;
    
    [Header("점프")]
    [Tooltip("점프 파워")]
    public float jumpPower = 5f;
    [Tooltip("지면 체크 거리")]
    public float groundCheckDistance = 0.1f;
    
    [Header("구르기")]
    [Tooltip("구르기 이동 거리")]
    public float rollDistance = 3f;
    [Tooltip("구르기 지속 시간")]
    public float rollDuration = 0.5f;
}
