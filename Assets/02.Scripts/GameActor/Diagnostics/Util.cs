using System;
using System.Collections.Generic;
using UnityEngine;
using Conditional = System.Diagnostics.ConditionalAttribute;

namespace UPlayGround
{
    public static class Util
    {
        public static float ApplyRandomValue(float originData, float min, float max)
        {
            float randRange = UnityEngine.Random.Range(min, max);
            return Mathf.Max(1, originData + originData * randRange);
        }
    }
}

namespace UPlayGround.Diagnostics
{
    [Flags]
    public enum RuntimeLogCategory
    {
        None = 0,
        Boot = 1 << 0,
        Combat = 1 << 1,
        Input = 1 << 2,
        AI = 1 << 3,
        Camera = 1 << 4,
        UI = 1 << 5,
        Asset = 1 << 6,
        Performance = 1 << 7,
        All = ~0,
    }

    /// <summary>
    /// 개발용 상세 로그의 카테고리 게이트.
    /// Trace 호출은 Editor/Development Build가 아니면 호출 인자 평가까지 컴파일에서 제거된다.
    /// </summary>
    public static class RuntimeLog
    {
        private const string CategoryMaskPlayerPrefsKey = "UPlayGround.RuntimeLog.CategoryMask";
        private static readonly Dictionary<int, float> LastLogTimes = new();

        public static RuntimeLogCategory EnabledCategories { get; private set; } = RuntimeLogCategory.All;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            // 릴리스에서는 기존 Debug.Log 호출까지 차단한다. Warning/Error/Exception은 계속 출력한다.
            Debug.unityLogger.filterLogType = Debug.isDebugBuild ? LogType.Log : LogType.Warning;

            int defaultMask = (int)RuntimeLogCategory.All;
            EnabledCategories = (RuntimeLogCategory)PlayerPrefs.GetInt(
                CategoryMaskPlayerPrefsKey,
                defaultMask);
        }

        public static void SetEnabledCategories(RuntimeLogCategory categories, bool persist = true)
        {
            EnabledCategories = categories;
            if (!persist)
                return;

            PlayerPrefs.SetInt(CategoryMaskPlayerPrefsKey, (int)categories);
            PlayerPrefs.Save();
        }

        public static bool IsEnabled(RuntimeLogCategory category)
            => (EnabledCategories & category) != 0;

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Trace(
            RuntimeLogCategory category,
            string message,
            UnityEngine.Object context = null)
        {
            if (!IsEnabled(category))
                return;

            if (context != null)
                Debug.Log(message, context);
            else
                Debug.Log(message);
        }

        /// <summary>
        /// 같은 키의 상세 로그를 지정 시간마다 최대 한 번만 출력한다.
        /// 키는 호출 지점별 상수 사용을 권장한다.
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void TraceThrottled(
            RuntimeLogCategory category,
            int key,
            float intervalSeconds,
            string message,
            UnityEngine.Object context = null)
        {
            if (!IsEnabled(category))
                return;

            float now = Time.unscaledTime;
            if (LastLogTimes.TryGetValue(key, out float lastTime) &&
                now - lastTime < Mathf.Max(0f, intervalSeconds))
            {
                return;
            }

            LastLogTimes[key] = now;
            Trace(category, message, context);
        }
    }
}
