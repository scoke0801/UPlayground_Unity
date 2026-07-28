namespace UPlayGround.Animation
{
    public interface IMotionTimeSource
    {
        float DeltaTime { get; }
    }

    public enum MotionTimelineControlMode
    {
        Loop,
        Freeze,
        InfiniteLoop,
    }

    /// <summary>
    /// 프로젝트 구체 이벤트를 참조하지 않고 재생 타임라인을 제어하기 위한 계약.
    /// </summary>
    public interface IMotionTimelineControlEvent
    {
        MotionTimelineControlMode Mode { get; }
        int LoopCount { get; }
        float FreezeDuration { get; }
    }
}
