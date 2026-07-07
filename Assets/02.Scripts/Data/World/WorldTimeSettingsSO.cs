using UnityEngine;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.World
{
    /// <summary>
    /// 인게임 시간 흐름/낮밤 구간 설정.
    /// GameTimeManager가 Addressables 키 "WorldTimeSettings"로 로드하며,
    /// 에셋이 없으면 코드 기본값(이 SO의 필드 기본값과 동일)으로 동작한다.
    /// </summary>
    [CreateAssetMenu(fileName = "WorldTimeSettings", menuName = "UPlayGround/월드/World Time Settings")]
    public class WorldTimeSettingsSO : ScriptableObject
    {
        public const float MinutesPerDay = 1440f;

        [Header("시간 흐름")]
        [Tooltip("실제 1초당 흐르는 인게임 분. 1.0이면 실제 24분 = 인게임 1일.")]
        [Min(0f)] public float gameMinutesPerRealSecond = 1f;

        [Tooltip("새 게임 시작 시각(하루 중 분). 480 = 08:00.")]
        [Range(0f, MinutesPerDay)] public float startMinuteOfDay = 8f * 60f;

        [Tooltip("메뉴/부활 팝업 등으로 IsPaused일 때 인게임 시간도 함께 멈출지 여부.")]
        public bool pauseStopsWorldTime = true;

        [Header("낮밤 구간 경계 (하루 중 분)")]
        [Tooltip("새벽 시작. 300 = 05:00.")]
        [Range(0f, MinutesPerDay)] public float dawnStartMinute = 5f * 60f;

        [Tooltip("낮 시작. 420 = 07:00.")]
        [Range(0f, MinutesPerDay)] public float dayStartMinute = 7f * 60f;

        [Tooltip("황혼 시작. 1080 = 18:00.")]
        [Range(0f, MinutesPerDay)] public float duskStartMinute = 18f * 60f;

        [Tooltip("밤 시작. 1200 = 20:00. 이후 새벽 시작 전까지 밤.")]
        [Range(0f, MinutesPerDay)] public float nightStartMinute = 20f * 60f;

        /// <summary> 하루 중 시각(분)에 해당하는 낮밤 구간을 계산한다. </summary>
        public DayPeriod GetPeriod(float minuteOfDay)
        {
            minuteOfDay = Mathf.Repeat(minuteOfDay, MinutesPerDay);

            if (minuteOfDay < dawnStartMinute) return DayPeriod.Night;
            if (minuteOfDay < dayStartMinute) return DayPeriod.Dawn;
            if (minuteOfDay < duskStartMinute) return DayPeriod.Day;
            if (minuteOfDay < nightStartMinute) return DayPeriod.Dusk;
            return DayPeriod.Night;
        }
    }
}
