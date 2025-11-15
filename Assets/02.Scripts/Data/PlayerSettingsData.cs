using UnityEngine;

/// <summary>
/// 플레이어 설정 데이터 (ScriptableObject)
/// TPS 카메라 각도 제한, 줌 기능 포함
/// </summary>
[CreateAssetMenu(fileName = "PlayerSettings", menuName = "Game/Player Settings", order = 1)]
public class PlayerSettingsData : ScriptableObject
{
    [Header("입력 설정")]
    [Range(0.01f, 0.5f)]
    [Tooltip("입력 데드존 (이 값 이하의 입력은 무시됨)")]
    public float deadzone = 0.1f;
    
    [Header("마우스 감도")]
    [Range(1f, 100f)]
    [Tooltip("마우스 감도")]
    public float mouseSensitivity = 5f;
    
    [Tooltip("Y축 반전 여부")]
    public bool invertYAxis = false;
    
    [Header("카메라 회전 설정")]
    [Range(-90f, 0f)]
    [Tooltip("카메라 최소 수직 각도 (아래를 볼 수 있는 각도)")]
    public float cameraMinVerticalAngle = -40f;
    
    [Range(0f, 90f)]
    [Tooltip("카메라 최대 수직 각도 (위를 볼 수 있는 각도)")]
    public float cameraMaxVerticalAngle = 80f;
    
    [Range(0.01f, 0.2f)]
    [Tooltip("카메라 회전 스무딩 (낮을수록 빠른 반응)")]
    public float cameraRotationSmoothing = 0.05f;
    
    [Header("카메라 거리 설정")]
    [Range(1f, 10f)]
    [Tooltip("기본 카메라 거리")]
    public float cameraDistance = 5f;
    
    [Range(0.5f, 3f)]
    [Tooltip("최소 카메라 거리 (최대 줌 인)")]
    public float cameraMinDistance = 1.5f;
    
    [Range(5f, 15f)]
    [Tooltip("최대 카메라 거리 (최대 줌 아웃)")]
    public float cameraMaxDistance = 10f;
    
    [Range(1f, 20f)]
    [Tooltip("줌 속도 (마우스 휠 감도)")]
    public float zoomSpeed = 2f;
    
    [Range(1f, 20f)]
    [Tooltip("줌 스무딩 속도")]
    public float zoomSmoothing = 10f;
    
    [Header("카메라 위치 설정")]
    [Range(0.5f, 3f)]
    [Tooltip("카메라 높이 (플레이어로부터의 높이)")]
    public float cameraHeight = 1.5f;
    
    [Range(0.5f, 3f)]
    [Tooltip("카메라가 바라보는 높이")]
    public float cameraLookAtHeight = 1.2f;
    
    [Range(1f, 20f)]
    [Tooltip("카메라 이동 스무딩 속도")]
    public float cameraSmoothness = 10f;
    
    [Range(0.5f, 3f)]
    [Tooltip("카메라 최소 수평 거리 (정수리 방지)")]
    public float minHorizontalDistance = 1.0f;
    
    [Header("카메라 충돌 설정")]
    [Tooltip("카메라 충돌 레이어")]
    public LayerMask cameraCollisionLayers = -1; // 모든 레이어
    
    [Range(0.1f, 1f)]
    [Tooltip("카메라 충돌 검사 반경")]
    public float cameraCollisionRadius = 0.2f;
    
    [Range(0.1f, 0.5f)]
    [Tooltip("벽과의 여유 공간")]
    public float cameraCollisionBuffer = 0.2f;
}