using System;
using UnityEngine;

namespace UPlayGround.Gameplay.World.LivingWorld
{
    /// <summary>D 독립 미리보기용 시간표. 공용 Data 타입의 게시 전에는 프리팹 내부 설정으로만 사용한다.</summary>
    [Serializable]
    public sealed class EnvironmentSchedule
    {
        [Min(1)] public int version = 1;
        [Min(1)] public float cycleMinutes = 1440;
        [Tooltip("강수보다 먼저 하늘이 흐려지는 게임 시간(분). 0이면 예고를 끈다.")]
        [Min(0)] public float preRainMinutes = 30;
        [Range(0.01f, 1)] public float rainCloudFullIntensity = 0.3f;
        [Range(0, 1)] public float rainCloudCoverage = 0.98f;
        public Key[] keys = Array.Empty<Key>();

        /// <summary>게임 시각으로 재입장과 무관한 바람·비·건조 상태를 복원한다.</summary>
        public bool TryEvaluate(double totalMinutes, out Sample sample)
        {
            sample = default;
            if (!IsValid() || double.IsNaN(totalMinutes) || double.IsInfinity(totalMinutes) || totalMinutes < 0)
                return false;

            float phase = (float)(totalMinutes % cycleMinutes);
            sample = EvaluatePhase(phase);
            float approachingRain = sample.Rain;
            if (preRainMinutes > 0)
            {
                float lead = Mathf.Min(preRainMinutes, cycleMinutes);
                approachingRain = Mathf.Max(approachingRain,
                    EvaluatePhase((float)((totalMinutes + lead) % cycleMinutes)).Rain);
                // 짧은 소나기를 예보 끝점에서 건너뛰지 않고 자정 경계에서도 연속해서 예고한다.
                for (int i = 0; i < keys.Length; i++)
                {
                    float distance = Mathf.Repeat(keys[i].minute - phase, cycleMinutes);
                    if (distance < lead)
                        approachingRain = Mathf.Max(approachingRain,
                            keys[i].rain * Mathf.SmoothStep(0, 1, 1 - distance / lead));
                }
            }
            sample.RainCloud = Mathf.SmoothStep(0, 1,
                Mathf.Clamp01(Mathf.Max(approachingRain, sample.Storm) / rainCloudFullIntensity));
            sample.Cloud = Mathf.Max(sample.Cloud, sample.RainCloud * rainCloudCoverage);
            return true;
        }

        private Sample EvaluatePhase(float phase)
        {
            int index = keys.Length - 1;
            for (int i = 0; i < keys.Length - 1; i++)
            {
                if (phase < keys[i + 1].minute) { index = i; break; }
            }
            Key current = keys[index];
            Key next = keys[(index + 1) % keys.Length];
            float end = index == keys.Length - 1 ? cycleMinutes : next.minute;
            float blend = Mathf.SmoothStep(0, 1, (phase - current.minute) / (end - current.minute));
            return new Sample
            {
                PresetId = current.presetId,
                PhaseMinute = phase,
                Wind = Vector2.Lerp(current.wind, next.wind, blend),
                Rain = Mathf.Lerp(current.rain, next.rain, blend),
                Snow = Mathf.Lerp(current.snow, next.snow, blend),
                Storm = Mathf.Lerp(current.storm, next.storm, blend),
                Wetness = Mathf.Lerp(current.wetness, next.wetness, blend),
                Cloud = Mathf.Lerp(current.cloud, next.cloud, blend)
            };
        }

        /// <summary>순환 경계와 값 범위를 검사해 잘못된 프로필의 전역 반영을 막는다.</summary>
        public bool IsValid()
        {
            if (version < 1 || !IsFinite(cycleMinutes) || cycleMinutes <= 0 || keys == null || keys.Length == 0
                || !IsFinite(preRainMinutes) || preRainMinutes < 0
                || !IsUnit(rainCloudFullIntensity) || rainCloudFullIntensity <= 0 || !IsUnit(rainCloudCoverage))
                return false;
            if (keys[0].minute != 0) return false;
            for (int i = 0; i < keys.Length; i++)
            {
                Key key = keys[i];
                if (string.IsNullOrWhiteSpace(key.presetId) || !IsFinite(key.minute)
                    || key.minute < 0 || key.minute >= cycleMinutes
                    || (i > 0 && key.minute <= keys[i - 1].minute)
                    || !IsFinite(key.wind.x) || !IsFinite(key.wind.y) || key.wind.sqrMagnitude > 1.001f
                    || !IsUnit(key.rain) || !IsUnit(key.snow) || !IsUnit(key.storm)
                    || !IsUnit(key.wetness) || !IsUnit(key.cloud)) return false;
            }
            return true;
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool IsUnit(float value) => IsFinite(value) && value >= 0 && value <= 1;

        /// <summary>풍속의 누적 적분을 저장 시각에서 재구성해 재입장·풍향 변경 때 구름이 튀지 않게 한다.</summary>
        public bool TryIntegrateWind(double totalMinutes, out Vector2 displacement)
        {
            displacement = default;
            if (!IsValid() || double.IsNaN(totalMinutes) || double.IsInfinity(totalMinutes) || totalMinutes < 0)
                return false;
            double cycles = Math.Floor(totalMinutes / cycleMinutes);
            double phase = totalMinutes % cycleMinutes;
            double x = 0, y = 0;
            for (int i = 0; i < keys.Length; i++)
            {
                Key current = keys[i], next = keys[(i + 1) % keys.Length];
                double end = i == keys.Length - 1 ? cycleMinutes : next.minute;
                double duration = end - current.minute;
                double fraction = Math.Max(0, Math.Min(1, (phase - current.minute) / duration));
                // SmoothStep(0,1,u)=3u²-2u³의 적분. 순환 구간 전체의 평균 가중치는 1/2이다.
                double integral = fraction * fraction * fraction * (1 - fraction * 0.5);
                double currentWeight = duration * (cycles * 0.5 + fraction - integral);
                double nextWeight = duration * (cycles * 0.5 + integral);
                x += current.wind.x * currentWeight + next.wind.x * nextWeight;
                y += current.wind.y * currentWeight + next.wind.y * nextWeight;
            }
            displacement = new Vector2((float)x, (float)y);
            return true;
        }

        /// <summary>강수 종료 뒤 젖음 키를 남겨 저장 시각만으로 건조 중간값을 복원한다.</summary>
        [Serializable]
        public struct Key
        {
            public string presetId;
            [Min(0)] public float minute;
            public Vector2 wind;
            [Range(0, 1)] public float rain, wetness, cloud;
            [Range(0, 1)] public float snow, storm;
        }

        /// <summary>공용 서비스 게시 전 독립 검증과 I 어댑터가 읽는 D의 계산 결과.</summary>
        public struct Sample
        {
            public string PresetId;
            public float PhaseMinute;
            public Vector2 Wind;
            public float Rain, Wetness, Cloud;
            public float Snow, Storm;
            public float RainCloud;
        }
    }
}
