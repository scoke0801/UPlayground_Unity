#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Editor;

namespace UPlayGround.Animation.Editor
{
    /// <summary>
    /// 카메라 녹화/시퀀스 저작 도구가 모션 프리뷰를 동일 타임코드로 제어하기 위한 브리지.
    /// 에디터 도구끼리 private 필드나 GUI 버튼을 우회하지 않도록 최소한의 촬영 API만 노출한다.
    /// </summary>
    public partial class MotionSetEditorWindow
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

        /// <summary>
        /// 지정한 MotionSet과 구간을 모션 에디터에서 재생한다.
        /// 카메라 레코더는 샘플러를 먼저 시작한 뒤 이 메서드를 호출해야 t=0 포즈를 놓치지 않는다.
        /// </summary>
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

            MotionSetEditorWindow window = GetWindow<MotionSetEditorWindow>();
            window.titleContent = new GUIContent("애니메이션 에디터");
            window.minSize = new Vector2(600f, 400f);
            window.Show();

            if (window._isPlaying)
                window.StopPlayback();

            window.SetAsset(asset);
            window._useTemporarySet = false;

            if (window._targetActor == null || window._animancer == null)
                window.AutoFindPlayer();

            if (window._targetActor == null || window._animancer == null)
            {
                error = "모션 에디터의 대상 액터를 찾지 못했습니다. 대상 액터를 먼저 지정하세요.";
                return false;
            }

            float totalDuration = asset.motionSet.TotalDuration;
            float clampedStart = Mathf.Clamp(startTime, 0f, totalDuration);
            float requestedEnd = endTime > 0f ? endTime : totalDuration;
            float clampedEnd = Mathf.Clamp(requestedEnd, clampedStart, totalDuration);

            if (clampedEnd <= clampedStart)
            {
                error = "촬영 종료 시간은 시작 시간보다 커야 합니다.";
                return false;
            }

            window._startTime = clampedStart;
            window._endTime = clampedEnd;
            window._isLooping = false;
            window.StartPlayback();
            window.Repaint();

            if (!window._isPlaying)
            {
                error = "모션 재생을 시작하지 못했습니다.";
                return false;
            }

            return true;
        }

        public static bool TryGetCapturePlaybackState(out CapturePlaybackState state)
        {
            if (!HasOpenInstances<MotionSetEditorWindow>())
            {
                state = default;
                return false;
            }

            MotionSetEditorWindow window = GetWindow<MotionSetEditorWindow>();
            state = new CapturePlaybackState(
                window._asset,
                window._playbackTime,
                window._startTime,
                window._endTime,
                window._isPlaying,
                window._isPaused);
            return true;
        }

        public static void StopCapturePlayback(MotionSetAsset expectedAsset = null)
        {
            if (!HasOpenInstances<MotionSetEditorWindow>())
                return;

            MotionSetEditorWindow window = GetWindow<MotionSetEditorWindow>();
            if (expectedAsset != null && window._asset != expectedAsset)
                return;

            if (window._isPlaying || window._isPaused)
                window.StopPlayback();

            window.Repaint();
        }

        public static void OpenForCapture(MotionSetAsset asset)
        {
            if (asset != null)
                Open(asset);
            else
                OpenWindow();
        }

        private void DrawCaptureBridgeControls()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("카메라 동기 촬영", EditorStyles.boldLabel);

                MotionSet motionSet = GetCurrentMotionSet();
                float totalDuration = motionSet?.TotalDuration ?? 0f;
                float effectiveEnd = _endTime > 0f
                    ? Mathf.Min(_endTime, totalDuration)
                    : totalDuration;

                EditorGUILayout.LabelField(
                    $"MotionSet: {(_asset != null ? _asset.name : "-")}",
                    EditorStyles.miniLabel);
                EditorGUILayout.LabelField(
                    $"대상: {(_targetActor != null ? _targetActor.name : "-")}  ·  구간 {_startTime:0.000}s → {effectiveEnd:0.000}s",
                    EditorStyles.miniLabel);

                using (new EditorGUI.DisabledScope(_asset == null))
                {
                    if (GUILayout.Button("현재 모션으로 카메라 녹화 열기", GUILayout.Height(24f)))
                    {
                        Transform suggestedAnchor = _targetActor != null
                            ? _targetActor.transform
                            : null;
                        DialogueCameraRecorderWindow.OpenForMotion(_asset, suggestedAnchor);
                    }
                }

                EditorGUILayout.HelpBox(
                    "카메라 녹화 창의 '동기 촬영 시작'이 현재 MotionSet을 지정 구간으로 재생하고, 종료 시 녹화를 자동 정지합니다.",
                    MessageType.None);
            }
        }
    }
}
#endif
