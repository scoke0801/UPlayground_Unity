using UnityEngine;

/// <summary>
/// 플레이어 설정 데이터
/// </summary>
[CreateAssetMenu(fileName = "PlayerSettings", menuName = "Game/Player Settings")]
public class PlayerSettingsData : ScriptableObject
{
    [Header("카메라 설정")]
    [Tooltip("마우스 감도")]
    [Range(1f, 30f)]
    public float mouseSensitivity = 1f; //
    
    [Tooltip("카메라와 플레이어 간 거리")]
    public float cameraDistance = 5f;
    
    [Tooltip("카메라 높이")]
    public float cameraHeight = 2f;
    
    [Tooltip("카메라 이동 부드러움 정도")]
    public float cameraSmoothness = 10f;
    
    [Tooltip("카메라 최소 수직 각도")]
    public float cameraMinVerticalAngle = -30f;
    
    [Tooltip("카메라 최대 수직 각도")]
    public float cameraMaxVerticalAngle = 70f;
    
    [Header("입력 설정")]
    [Tooltip("Y축 반전 여부")]
    public bool invertYAxis = false;
    
    [Tooltip("입력 데드존 (스틱 입력 무시 범위)")]
    [Range(0f, 0.5f)]
    public float deadzone = 0.1f;
}
