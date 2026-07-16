using System;
using System.Collections.Generic;

namespace UPlayGround.Animation.Editor
{
    /// <summary>
    /// MotionEventAddPopup 빌트인 프리셋 등록점.
    /// 게임/프로젝트 측 에디터 코드가 [InitializeOnLoad] 시점에 제공자를 등록한다.
    /// 사용자 저장 프리셋(MotionEventPresetLibrarySO)과 별개로, 코드로 정의하는 기본 프리셋을 다룬다.
    /// </summary>
    public static class MotionEventPresetProviders
    {
        static readonly List<Func<IEnumerable<MotionEventAddPopup.EventPreset>>> _providers
            = new List<Func<IEnumerable<MotionEventAddPopup.EventPreset>>>();

        public static void Register(Func<IEnumerable<MotionEventAddPopup.EventPreset>> provider)
        {
            if (provider == null || _providers.Contains(provider)) return;
            _providers.Add(provider);
        }

        public static List<MotionEventAddPopup.EventPreset> CollectAll()
        {
            var result = new List<MotionEventAddPopup.EventPreset>();
            foreach (var provider in _providers)
            {
                var presets = provider?.Invoke();
                if (presets == null) continue;

                foreach (var preset in presets)
                {
                    if (preset != null)
                        result.Add(preset);
                }
            }
            return result;
        }
    }
}
