using UnityEditor;
using UnityEngine;

namespace UPlayGround.Editor
{
    /// <summary>
    /// 에디터 Play 모드에서만 프레임을 제한해 GPU 사용량을 줄이는 도구.
    /// Editor 폴더에 있으므로 실제 빌드에는 포함되지 않는다(빌드 성능 영향 없음).
    /// Play 진입 시 targetFrameRate를 캡하고, Play 종료 시 원래 값으로 복원한다.
    /// Tools 메뉴에서 켜고 끌 수 있으며 설정은 EditorPrefs에 저장된다.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorPlayModeFrameLimiter
    {
        private const string EnabledPrefKey = "UPlayGround.EditorFrameLimiter.Enabled";
        private const string FrameRatePrefKey = "UPlayGround.EditorFrameLimiter.FrameRate";
        private const string MenuPath = "Tools/성능/에디터 Play 프레임 제한";
        private const int DefaultFrameRate = 60;

        // Play 모드 진입 전 원래 값(종료 시 복원용)
        private static int _previousTargetFrameRate;
        private static int _previousVSyncCount;

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
            if (!Enabled || !EditorApplication.isPlaying)
                return;

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

                    // 복원용으로 현재 값 저장
                    _previousTargetFrameRate = Application.targetFrameRate;
                    _previousVSyncCount = QualitySettings.vSyncCount;

                    // vSync가 켜져 있으면 targetFrameRate가 무시되므로 끈다(에디터 런타임 한정).
                    QualitySettings.vSyncCount = 0;
                    Application.targetFrameRate = FrameRate;

                    Debug.Log($"[FrameLimiter] 에디터 Play 프레임 제한 적용: {FrameRate}fps (GPU 절약용)");
                    break;

                case PlayModeStateChange.ExitingPlayMode:
                    // 적용 여부와 무관하게 안전하게 원복
                    Application.targetFrameRate = _previousTargetFrameRate;
                    QualitySettings.vSyncCount = _previousVSyncCount;
                    break;
            }
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
                    QualitySettings.vSyncCount = 0;
                    Application.targetFrameRate = FrameRate;
                }
                else
                {
                    Application.targetFrameRate = -1; // 무제한
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
