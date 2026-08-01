using UnityEditor;
using UnityEngine;

namespace UPlayGround.Editor
{
    /// <summary>
    /// 에디터 Play 모드에서만 프레임과 LOD Bias를 제한해 GPU 사용량을 줄이는 도구.
    /// Editor 폴더에 있으므로 실제 빌드에는 포함되지 않는다(빌드 성능 영향 없음).
    /// Play 진입 시 targetFrameRate와 LOD Bias를 캡하고, Play 종료 시 원래 값으로 복원한다.
    /// Tools 메뉴에서 켜고 끌 수 있으며 설정은 EditorPrefs에 저장된다.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorPlayModeFrameLimiter
    {
        private const string EnabledPrefKey = "UPlayGround.EditorFrameLimiter.Enabled";
        private const string FrameRatePrefKey = "UPlayGround.EditorFrameLimiter.FrameRate";
        private const string LodBiasAppliedSessionKey = "UPlayGround.EditorPlayMode.LodBiasApplied";
        private const string PreviousLodBiasSessionKey = "UPlayGround.EditorPlayMode.PreviousLodBias";
        private const string FrameCapAppliedSessionKey = "UPlayGround.EditorPlayMode.FrameCapApplied";
        private const string PreviousFrameRateSessionKey = "UPlayGround.EditorPlayMode.PreviousFrameRate";
        private const string PreviousVSyncSessionKey = "UPlayGround.EditorPlayMode.PreviousVSync";
        private const string MenuPath = "Tools/성능/에디터 Play 프레임 제한";
        private const int DefaultFrameRate = 60;
        private const float EditorPlayModeLodBias = 1f;

        public static bool Enabled
        {
            get => EditorPrefs.GetBool(EnabledPrefKey, true);
            set => EditorPrefs.SetBool(EnabledPrefKey, value);
        }

        public static int FrameRate
        {
            get => EditorPrefs.GetInt(FrameRatePrefKey, DefaultFrameRate);
            set => EditorPrefs.SetInt(FrameRatePrefKey, Mathf.Max(1, value));
        }

        static EditorPlayModeFrameLimiter()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            // 게임 코드(GameManager/SettingsApplier)가 Play 시작 후 vSync/targetFrameRate를
            // 다시 설정해 캡을 덮어쓰므로, 매 틱 다시 강제해 캡이 항상 이기도록 한다.
            EditorApplication.update += EnforceCapWhilePlaying;
        }

        private static void EnforceCapWhilePlaying()
        {
            if (!EditorApplication.isPlaying)
                return;

            if (!Enabled)
                return;

            ApplyEditorLodBias();
            ApplyFrameCap();

            // vSync가 켜져 있으면 targetFrameRate가 무시되므로 항상 0으로 유지
            if (QualitySettings.vSyncCount != 0)
                QualitySettings.vSyncCount = 0;
            if (Application.targetFrameRate != FrameRate)
                Application.targetFrameRate = FrameRate;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    if (!Enabled)
                        return;

                    ApplyEditorLodBias();
                    ApplyFrameCap();

                    Debug.Log($"[FrameLimiter] 에디터 Play 프레임 제한 적용: {FrameRate}fps (GPU 절약용)");
                    break;

                case PlayModeStateChange.ExitingPlayMode:
                    RestoreEditorLodBias();
                    RestoreFrameCap();
                    break;
            }
        }

        /// <summary>
        /// Player 빌드의 QualitySettings은 건드리지 않고, 에디터 Play Mode에서만 LOD Bias를 1로 제한한다.
        /// SessionState를 사용해 Play 중 스크립트 재컴파일이 발생해도 원래 값을 보존한다.
        /// </summary>
        private static void ApplyEditorLodBias()
        {
            if (!SessionState.GetBool(LodBiasAppliedSessionKey, false))
            {
                SessionState.SetFloat(PreviousLodBiasSessionKey, QualitySettings.lodBias);
                SessionState.SetBool(LodBiasAppliedSessionKey, true);
            }

            float limitedBias = Mathf.Min(QualitySettings.lodBias, EditorPlayModeLodBias);
            if (!Mathf.Approximately(QualitySettings.lodBias, limitedBias))
                QualitySettings.lodBias = limitedBias;
        }

        private static void RestoreEditorLodBias()
        {
            if (!SessionState.GetBool(LodBiasAppliedSessionKey, false))
                return;

            QualitySettings.lodBias = SessionState.GetFloat(
                PreviousLodBiasSessionKey,
                QualitySettings.lodBias);
            SessionState.SetBool(LodBiasAppliedSessionKey, false);
        }

        private static void ApplyFrameCap()
        {
            if (!SessionState.GetBool(FrameCapAppliedSessionKey, false))
            {
                SessionState.SetInt(PreviousFrameRateSessionKey, Application.targetFrameRate);
                SessionState.SetInt(PreviousVSyncSessionKey, QualitySettings.vSyncCount);
                SessionState.SetBool(FrameCapAppliedSessionKey, true);
            }

            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = FrameRate;
        }

        private static void RestoreFrameCap()
        {
            if (!SessionState.GetBool(FrameCapAppliedSessionKey, false))
                return;

            Application.targetFrameRate = SessionState.GetInt(
                PreviousFrameRateSessionKey,
                Application.targetFrameRate);
            QualitySettings.vSyncCount = SessionState.GetInt(
                PreviousVSyncSessionKey,
                QualitySettings.vSyncCount);
            SessionState.SetBool(FrameCapAppliedSessionKey, false);
        }

        // ── 메뉴: 켜기/끄기 토글 ──────────────────────────────────────────
        [MenuItem(MenuPath, false, 100)]
        private static void ToggleEnabled()
        {
            Enabled = !Enabled;

            // 이미 Play 중이면 즉시 반영
            if (EditorApplication.isPlaying)
            {
                if (Enabled)
                {
                    ApplyEditorLodBias();
                    ApplyFrameCap();
                }
                else
                {
                    RestoreEditorLodBias();
                    RestoreFrameCap();
                }
            }

            Debug.Log($"[FrameLimiter] 에디터 Play 프레임 제한 {(Enabled ? $"켜짐 ({FrameRate}fps)" : "꺼짐")}");
        }

        [MenuItem(MenuPath, true)]
        private static bool ToggleEnabledValidate()
        {
            Menu.SetChecked(MenuPath, Enabled);
            return true;
        }
    }
}
