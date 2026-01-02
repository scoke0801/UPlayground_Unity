using UnityEngine;

// 1. 입력 데이터 구조체
public struct PlayerCharacterInputs
{
    public float MoveAxisForward;
    public float MoveAxisRight;
    public Quaternion CameraRotation;
    public bool JumpDown;
    public bool SprintHeld; // 추가: 달리기 키 유지 여부
    public bool CrouchDown; // 추가: 앉기 버튼 입력
}