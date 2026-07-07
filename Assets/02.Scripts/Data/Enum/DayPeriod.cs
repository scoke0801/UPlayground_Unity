namespace UPlayGround.Data.EnumType
{
    /// <summary>
    /// 인게임 하루를 구성하는 시간대 구간.
    /// 경계 시각은 하드코딩하지 않고 <see cref="UPlayGround.Data.World.WorldTimeSettingsSO"/>가 소유한다.
    /// </summary>
    public enum DayPeriod
    {
        Dawn,   // 새벽
        Day,    // 낮
        Dusk,   // 황혼
        Night,  // 밤
    }
}
