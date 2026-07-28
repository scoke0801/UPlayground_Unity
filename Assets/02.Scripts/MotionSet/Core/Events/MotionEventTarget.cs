using UnityEngine;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// MotionEventExecutor가 프로젝트 구체 타입을 알지 않고 이벤트 대상을 해석하기 위한 계약.
    /// </summary>
    public interface IMotionEventTargetProvider
    {
        GameObject MotionEventTarget { get; }
    }
}
