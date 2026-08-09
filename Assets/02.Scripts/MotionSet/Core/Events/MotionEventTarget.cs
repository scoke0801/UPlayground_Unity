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

    /// <summary>
    /// Core 실행기가 프로젝트 액터 타입을 직접 참조하지 않고 적 전용 실행 정책을
    /// 적용하기 위한 대상 분류 계약.
    /// </summary>
    public interface IMotionEventExecutionScope
    {
        bool IsEnemyMotionEventTarget { get; }
    }
}
