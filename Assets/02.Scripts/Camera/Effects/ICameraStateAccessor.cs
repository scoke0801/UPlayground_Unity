using UnityEngine;

namespace UPlayGround
{
    /// <summary>
    /// 카메라 상태에 대한 읽기 전용 접근 인터페이스
    /// 이펙트가 현재 카메라 상태를 참조해야 할 때 사용
    /// </summary>
    public interface ICameraStateAccessor
    {
        float CurrentYaw { get; }
        float CurrentPitch { get; }
        float CurrentDistance { get; }
        float TargetDistance { get; }
        Vector3 CurrentOffset { get; }
        float CurrentFOV { get; }
        Camera MainCamera { get; }
        Transform Target { get; }
    }
}
