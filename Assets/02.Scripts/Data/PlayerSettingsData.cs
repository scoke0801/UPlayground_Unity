using UnityEngine;

/// <summary>
/// 플레이어 전용 설정 데이터
/// 카메라, 레벨링, 성장 시스템 등
/// </summary>
[CreateAssetMenu(fileName = "New Player Settings", menuName = "RPG/Player/Player Settings")]
public class PlayerSettingsData : ScriptableObject
{
    [Header("카메라 설정")]
    [Tooltip("마우스 감도")]
    [Range(0.1f, 10f)]
    public float mouseSensitivity = 2f;
    [Tooltip("카메라 거리")]
    [Range(2f, 15f)]
    public float cameraDistance = 5f;
    [Tooltip("카메라 높이")]
    public float cameraHeight = 2f;
    [Tooltip("카메라 수직 각도 제한 (최소)")]
    public float cameraMinVerticalAngle = -80f;
    [Tooltip("카메라 수직 각도 제한 (최대)")]
    public float cameraMaxVerticalAngle = 80f;
    [Tooltip("카메라 이동 부드러움")]
    public float cameraSmoothness = 10f;
    
    [Header("레벨링 시스템")]
    [Tooltip("시작 레벨")]
    public int startLevel = 1;
    [Tooltip("기본 경험치 요구량")]
    public int baseExpRequired = 100;
    [Tooltip("레벨당 추가 경험치")]
    public int expIncreasePerLevel = 50;
    
    [Header("레벨업 성장")]
    [Tooltip("레벨당 HP 증가")]
    public float hpIncreasePerLevel = 20f;
    [Tooltip("레벨당 MP 증가")]
    public float mpIncreasePerLevel = 10f;
    [Tooltip("레벨당 스태미나 증가")]
    public float staminaIncreasePerLevel = 15f;
    
    [Header("초기 재화")]
    [Tooltip("시작 골드")]
    public int startGold = 0;
}
