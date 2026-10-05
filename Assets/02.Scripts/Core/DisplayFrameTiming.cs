using System;

namespace UPlayGround.Core
{
    /// <summary>화면 동기화를 유지하면서 요청 프레임 상한에 맞는 주사 간격을 계산한다.</summary>
    public static class DisplayFrameTiming
    {
        /// <summary>Unity가 지원하는 1~4회 수직 동기 간격 중 프레임 상한 이하인 가장 빠른 값을 선택한다.</summary>
        public static int GetVSyncCount(double refreshRate, int targetFrameRate)
        {
            if (double.IsNaN(refreshRate) || double.IsInfinity(refreshRate) || refreshRate <= 0d)
                return 1;

            // 59.94/119.88Hz 같은 분수 주사율을 정수 Hz로 반올림하지 않는다.
            double interval = Math.Ceiling(refreshRate / Math.Max(1, targetFrameRate));
            return (int)Math.Max(1d, Math.Min(4d, interval));
        }
    }
}
