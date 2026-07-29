using UnityEditor;
using UnityEngine;

namespace UPlayGround.Animation.Editor
{
    public sealed partial class MotionSetEditorWindow
    {
        public readonly struct CapturePlaybackState
        {
            public CapturePlaybackState(
                MotionSetAsset asset,
                float currentTime,
                float startTime,
                float endTime,
                bool isPlaying,
                bool isPaused)
            {
                Asset = asset;
                CurrentTime = currentTime;
                StartTime = startTime;
                EndTime = endTime;
                IsPlaying = isPlaying;
                IsPaused = isPaused;
            }

            public MotionSetAsset Asset { get; }
            public float CurrentTime { get; }
            public float StartTime { get; }
            public float EndTime { get; }
            public bool IsPlaying { get; }
            public bool IsPaused { get; }
        }

        private float _captureStartTime;

        public static bool TryStartCapturePlayback(
            MotionSetAsset asset,
            float startTime,
            float endTime,
            out string error)
        {
            error = string.Empty;
            if (!Application.isPlaying)
            {
                error = "동기 촬영은 PlayMode에서만 사용할 수 있습니다.";
                return false;
            }

            if (asset == null || asset.motionSet == null || !asset.motionSet.IsValid())
            {
                error = "유효한 MotionSetAsset이 필요합니다.";
                return false;
            }

            MotionSetEditorWindow window = ShowWindow();
            window.SetCatalog(null, null, asset);
            if (window._subject?.Animancer == null)
            {
                window.LoadSelectedSubject();

                // 대상 바인딩이 제공하는 카탈로그가 캡처 대상 MotionSet을
                // 바꾸지 않도록 호출자가 지정한 에셋을 다시 고정한다.
                window.SetCatalog(null, null, asset);
            }

            if (window._subject?.Animancer == null)
            {
                error = "모션 에디터의 프리뷰 대상을 찾지 못했습니다. 대상을 먼저 로드하세요.";
                return false;
            }

            float duration = asset.motionSet.TotalDuration;
            window._captureStartTime = Mathf.Clamp(startTime, 0f, duration);
            window._playbackStopTime = Mathf.Clamp(
                endTime > 0f ? endTime : duration,
                window._captureStartTime,
                duration);
            if (window._playbackStopTime <= window._captureStartTime)
            {
                error = "동기 촬영 종료 시간은 시작 시간보다 커야 합니다.";
                window._playbackStopTime = -1f;
                return false;
            }

            window._loop = false;
            window.SetPlaybackTime(window._captureStartTime);
            window.StartPlayback();
            return window._isPlaying;
        }

        /// <summary>
        /// 열려 있는 창을 포커스 이동 없이 찾는다.
        /// GetWindow는 기본적으로 창을 앞으로 끌어오므로 폴링 경로에서 사용하지 않는다.
        /// </summary>
        private static MotionSetEditorWindow FindOpenWindow()
        {
            if (!HasOpenInstances<MotionSetEditorWindow>())
                return null;

            MotionSetEditorWindow[] windows =
                Resources.FindObjectsOfTypeAll<MotionSetEditorWindow>();
            return windows.Length > 0 ? windows[0] : null;
        }

        public static bool TryGetCapturePlaybackState(
            out CapturePlaybackState state)
        {
            state = default;
            MotionSetEditorWindow window = FindOpenWindow();
            if (window == null)
                return false;

            state = new CapturePlaybackState(
                window._asset,
                window._playbackTime,
                window._captureStartTime,
                window._playbackStopTime,
                window._isPlaying,
                window._isPaused);
            return true;
        }

        public static void StopCapturePlayback(MotionSetAsset expectedAsset = null)
        {
            MotionSetEditorWindow window = FindOpenWindow();
            if (window == null)
                return;

            if (expectedAsset != null && window._asset != expectedAsset)
                return;
            window.StopPlayback();
            window._playbackStopTime = -1f;
            window.Repaint();
        }

        public static void OpenForCapture(MotionSetAsset asset)
        {
            if (asset != null)
                Open(asset);
            else
                ShowWindow();
        }
    }
}
