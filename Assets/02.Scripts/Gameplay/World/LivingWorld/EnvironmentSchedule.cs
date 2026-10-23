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
        public Key[] keys = Array.Empty<Key>();

        /// <summary>게임 시각으로 재입장과 무관한 바람·비·건조 상태를 복원한다.</summary>
        public bool TryEvaluate(double totalMinutes, out Sample sample)
        {
            sample = default;
            if (!IsValid() || double.IsNaN(totalMinutes) || double.IsInfinity(totalMinutes) || totalMinutes < 0)
                return false;

            float phase = (float)(totalMinutes % cycleMinutes);
            int index = keys.Length - 1;
            for (int i = 0; i < keys.Length - 1; i++)
            {
                if (phase < keys[i + 1].minute) { index = i; break; }
            }
            Key current = keys[index];
            Key next = keys[(index + 1) % keys.Length];
            float end = index == keys.Length - 1 ? cycleMinutes : next.minute;
            float blend = Mathf.SmoothStep(0, 1, (phase - current.minute) / (end - current.minute));
            sample = new Sample
            {
                PresetId = current.presetId,
                PhaseMinute = phase,
                Wind = Vector2.Lerp(current.wind, next.wind, blend),
                Rain = Mathf.Lerp(current.rain, next.rain, blend),
                Wetness = Mathf.Lerp(current.wetness, next.wetness, blend),
                Cloud = Mathf.Lerp(current.cloud, next.cloud, blend)
            };
            return true;
        }

        /// <summary>순환 경계와 값 범위를 검사해 잘못된 프로필의 전역 반영을 막는다.</summary>
        public bool IsValid()
        {
            if (version < 1 || !IsFinite(cycleMinutes) || cycleMinutes <= 0 || keys == null || keys.Length == 0)
                return false;
            if (keys[0].minute != 0) return false;
            for (int i = 0; i < keys.Length; i++)
            {
                Key key = keys[i];
                if (string.IsNullOrWhiteSpace(key.presetId) || !IsFinite(key.minute)
                    || key.minute < 0 || key.minute >= cycleMinutes
                    || (i > 0 && key.minute <= keys[i - 1].minute)
                    || !IsFinite(key.wind.x) || !IsFinite(key.wind.y) || key.wind.sqrMagnitude > 1.001f
                    || !IsUnit(key.rain) || !IsUnit(key.wetness) || !IsUnit(key.cloud)) return false;
            }
            return true;
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool IsUnit(float value) => IsFinite(value) && value >= 0 && value <= 1;

        /// <summary>강수 종료 뒤 젖음 키를 남겨 저장 시각만으로 건조 중간값을 복원한다.</summary>
        [Serializable]
        public struct Key
        {
            public string presetId;
            [Min(0)] public float minute;
            public Vector2 wind;
            [Range(0, 1)] public float rain, wetness, cloud;
        }

        /// <summary>공용 서비스 게시 전 독립 검증과 I 어댑터가 읽는 D의 계산 결과.</summary>
        public struct Sample
        {
            public string PresetId;
            public float PhaseMinute;
            public Vector2 Wind;
            public float Rain, Wetness, Cloud;
        }
    }
}
