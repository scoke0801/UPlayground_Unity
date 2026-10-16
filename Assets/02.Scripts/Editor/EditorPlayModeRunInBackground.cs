using UnityEditor;
using UnityEngine;

namespace UPlayGround.Editor
{
    /// <summary>
    /// 에디터 Play 모드에서 Unity 창이 포커스를 잃어도 게임 루프가 계속 돌게 하는 도구.
    /// 게임 설정의 '백그라운드 실행'(기본 꺼짐)은 SettingsApplier가 설정을 적용할 때마다
    /// Application.runInBackground를 덮어쓰므로, Play 중에는 에디터 업데이트마다 켜진 상태를 유지한다.
    ///
    /// 주의: 에디터에서 Application.runInBackground는 Player Settings 값과 연결되어 있다.
    /// 복원하지 않으면 Play 중 바꾼 값이 ProjectSettings에 남아 빌드 기본값이 바뀌므로,
    /// Play 진입 전 원래 값을 SessionState에 저장하고 에디트 모드로 돌아오면 원복한다.
    /// Editor 폴더에 있으므로 빌드에는 포함되지 않고, 플레이어 빌드는 게임 설정 값을 그대로 따른다.
    /// Tools 메뉴에서 켜고 끌 수 있으며 설정은 EditorPrefs에 저장된다.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorPlayModeRunInBackground
    {
        private const string EnabledPrefKey = "UPlayGround.EditorRunInBackground.Enabled";
        private const string AppliedSessionKey = "UPlayGround.EditorPlayMode.RunInBackgroundApplied";
        private const string PreviousValueSessionKey = "UPlayGround.EditorPlayMode.PreviousRunInBackground";
        private const string MenuPath = "Tools/UPlayGround/Debug/에디터 Play 백그라운드 실행";

        public static bool Enabled
        {
            get => EditorPrefs.GetBool(EnabledPrefKey, true);
            set => EditorPrefs.SetBool(EnabledPrefKey, value);
        }

        static EditorPlayModeRunInBackground()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += EnforceWhilePlaying;
        }

        private static void EnforceWhilePlaying()
        {
            if (!EditorApplication.isPlaying || !Enabled)
                return;

            ApplyRunInBackground();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                // Play 직후 Addressables 로딩 중에 다른 창으로 전환해도 멈추지 않도록 진입 전부터 켠다.
                case PlayModeStateChange.ExitingEditMode:
                case PlayModeStateChange.EnteredPlayMode:
                    if (Enabled)
                        ApplyRunInBackground();
                    break;

                // Play 진입이 중간에 취소된 경우도 이 시점을 거치므로 복원 지점을 하나로 둔다.
                case PlayModeStateChange.EnteredEditMode:
                    RestoreRunInBackground();
                    break;
            }
        }

        private static void ApplyRunInBackground()
        {
            if (!SessionState.GetBool(AppliedSessionKey, false))
            {
                SessionState.SetBool(PreviousValueSessionKey, Application.runInBackground);
                SessionState.SetBool(AppliedSessionKey, true);
            }

            if (!Application.runInBackground)
                Application.runInBackground = true;
        }

        private static void RestoreRunInBackground()
        {
            if (!SessionState.GetBool(AppliedSessionKey, false))
                return;

            Application.runInBackground = SessionState.GetBool(
                PreviousValueSessionKey,
                Application.runInBackground);
            SessionState.SetBool(AppliedSessionKey, false);
        }

        [MenuItem(MenuPath, false, 100)]
        private static void ToggleEnabled()
        {
            Enabled = !Enabled;

            // 이미 Play 중이면 즉시 반영
            if (EditorApplication.isPlaying)
            {
                if (Enabled)
                    ApplyRunInBackground();
                else
                    RestoreRunInBackground();
            }

            Debug.Log($"[RunInBackground] 에디터 Play 백그라운드 실행 {(Enabled ? "켜짐" : "꺼짐")}");
        }

        [MenuItem(MenuPath, true)]
        private static bool ToggleEnabledValidate()
        {
            Menu.SetChecked(MenuPath, Enabled);
            return true;
        }
    }
}
