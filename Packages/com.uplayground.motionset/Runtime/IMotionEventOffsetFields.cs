namespace UPlayGround.Data.Event
{
    /// <summary>
    /// 위치/회전 오프셋 필드를 가진 MotionEvent가 에디터의 오프셋 전용 위젯
    /// (축별 색상 필드 + 좌표 공간 라벨)을 받기 위한 계약.
    /// 에디터(MotionEventOffsetFieldUtil)가 이 인터페이스로 판별하므로,
    /// 구체 이벤트는 어느 어셈블리에 있어도 위젯이 적용된다.
    /// </summary>
    public interface IMotionEventOffsetFields
    {
        /// <summary>해당 직렬화 필드가 로컬 위치 오프셋 위젯으로 그려져야 하는지.</summary>
        bool IsLocalOffsetField(string fieldName);

        /// <summary>해당 직렬화 필드가 회전 오프셋(Euler) 위젯으로 그려져야 하는지.</summary>
        bool IsRotationOffsetField(string fieldName);

        /// <summary>위치 오프셋 위젯에 표시할 좌표 공간 라벨 (예: "Blade", "World", "Spawn Point").</summary>
        string LocalOffsetSpaceLabel { get; }

        /// <summary>회전 오프셋 위젯에 표시할 좌표 공간 라벨 (예: "Blade Offset", "World Euler").</summary>
        string RotationOffsetSpaceLabel { get; }
    }
}
