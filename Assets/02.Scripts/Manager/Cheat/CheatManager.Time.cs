#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using UPlayGround.Data.World;

namespace UPlayGround.Manager
{
    /// <summary>CheatManager — 인게임 시간 치트(스킵/시각 설정/배속). 개발 빌드 전용.</summary>
    public partial class CheatManager
    {
        /// <summary>
        /// 인게임 시간을 지정 분만큼 앞으로 스킵한다.
        /// SetTotalGameMinutes가 분 이벤트 재발행을 예약하므로 다음 프레임에
        /// 낮밤 조명/재스폰 due 처리가 자동으로 따라온다.
        /// </summary>
        public void AdvanceGameMinutes(float minutes)
        {
            var time = GameTimeManager.Instance;
            if (time == null || minutes <= 0f) return;

            time.SetTotalGameMinutes(time.TotalGameMinutes + minutes);
            Log(CheatCategory.Time, $"시간 +{FormatMinutes(minutes)} → {time.FormatGameTime()}");
        }

        /// <summary>
        /// 하루 중 지정 시각(분)으로 스킵한다. 항상 앞으로만 이동한다
        /// (현재 시각보다 이르면 다음 날 해당 시각으로).
        /// </summary>
        public void SkipToMinuteOfDay(float minuteOfDay)
        {
            var time = GameTimeManager.Instance;
            if (time == null) return;

            minuteOfDay = Mathf.Repeat(minuteOfDay, WorldTimeSettingsSO.MinutesPerDay);
            float dayStart = Mathf.Floor(time.TotalGameMinutes / WorldTimeSettingsSO.MinutesPerDay)
                             * WorldTimeSettingsSO.MinutesPerDay;
            float target = dayStart + minuteOfDay;
            if (target <= time.TotalGameMinutes)
                target += WorldTimeSettingsSO.MinutesPerDay;

            time.SetTotalGameMinutes(target);
            Log(CheatCategory.Time, $"시각 이동 → {time.FormatGameTime()}");
        }

        /// <summary> 인게임 시계 배속을 설정한다(설정 SO는 건드리지 않음, 저장 안 됨). 1이 기본. </summary>
        public void SetWorldClockMultiplier(float multiplier)
        {
            var time = GameTimeManager.Instance;
            if (time == null) return;

            time.WorldClockMultiplier = multiplier;
            Log(CheatCategory.Time, $"시간 배속 x{time.WorldClockMultiplier:0.##}");
        }

        private static string FormatMinutes(float minutes)
        {
            if (minutes >= WorldTimeSettingsSO.MinutesPerDay
                && minutes % WorldTimeSettingsSO.MinutesPerDay == 0f)
                return $"{(int)(minutes / WorldTimeSettingsSO.MinutesPerDay)}일";
            if (minutes >= 60f && minutes % 60f == 0f)
                return $"{(int)(minutes / 60f)}시간";
            return $"{(int)minutes}분";
        }
    }
}
#endif
