using UnityEngine;
using UnityEngine.Audio;
using UPlayGround.Data.Config;

namespace UPlayGround.Manager
{
    /// <summary>
    /// SettingsData 값을 Unity 시스템(QualitySettings, Screen, AudioMixer)에 실제로 반영.
    /// UI와 완전히 분리되어 있어 게임 시작 시 초기 적용에도 재사용 가능.
    /// </summary>
    public static class SettingsApplier
    {
        private static readonly (int width, int height)[] LegacyResolutions =
        {
            (1920, 1080),
            (1280, 720),
            (2560, 1440),
        };

        public static void ApplyAll(SettingsData data, AudioMixer mixer = null)
        {
            ApplyGraphics(data);
            ApplyAudio(data, mixer);
        }

        public static void ApplyGraphics(SettingsData data)
        {
            Application.runInBackground = data.runInBackground;

            int width = data.resolutionWidth;
            int height = data.resolutionHeight;
            if (width <= 0 || height <= 0)
            {
                int legacyIndex = Mathf.Clamp(data.resolutionIndex, 0, LegacyResolutions.Length - 1);
                (width, height) = LegacyResolutions[legacyIndex];
            }
            Screen.SetResolution(width, height, ToFullScreenMode(data));

            ApplyQualityPreset(data.qualityIndex);
            ApplyFrameTiming(data);
        }

        private static void ApplyQualityPreset(int qualityIndex)
        {
            int preset = Mathf.Clamp(qualityIndex, 0, 3);

            // 프로젝트의 기본 PC 렌더 파이프라인을 유지한 채 런타임 품질 차이를 적용한다.
            // 현재 QualitySettings 에셋에는 PC 레벨 하나만 있으므로 SetQualityLevel만으로는
            // 드롭다운의 낮음~최고 단계가 구분되지 않는다.
            QualitySettings.globalTextureMipmapLimit = preset == 0 ? 1 : 0;
            QualitySettings.anisotropicFiltering = preset switch
            {
                0 => AnisotropicFiltering.Disable,
                1 => AnisotropicFiltering.Enable,
                _ => AnisotropicFiltering.ForceEnable
            };
            QualitySettings.softParticles = preset >= 1;
            QualitySettings.realtimeReflectionProbes = preset >= 2;
            QualitySettings.shadowDistance = preset switch
            {
                0 => 20f,
                1 => 40f,
                2 => 60f,
                _ => 100f
            };
            QualitySettings.shadows = preset == 0 ? ShadowQuality.HardOnly : ShadowQuality.All;
            QualitySettings.shadowResolution = preset switch
            {
                0 => ShadowResolution.Low,
                1 => ShadowResolution.Medium,
                2 => ShadowResolution.High,
                _ => ShadowResolution.VeryHigh
            };
            QualitySettings.antiAliasing = preset switch
            {
                0 => 0,
                1 => 2,
                2 => 4,
                _ => 8
            };
        }

        private static FullScreenMode ToFullScreenMode(SettingsData data)
        {
            return data.windowModeIndex switch
            {
                0 => FullScreenMode.ExclusiveFullScreen,
                1 => FullScreenMode.FullScreenWindow,
                2 => FullScreenMode.Windowed,
                _ => data.fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed
            };
        }

        private static void ApplyFrameTiming(SettingsData data)
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = Mathf.Clamp(data.targetFrameRate, 30, 144);
        }

        public static void ApplyAudio(SettingsData data, AudioMixer mixer)
        {
            if (mixer == null) return;

            // AudioMixer는 로그 스케일(dB)로 동작 → 슬라이더 선형값(0~10)을 변환
            mixer.SetFloat("MasterVolume", VolumeToDb(data.masterVolume));
            mixer.SetFloat("BGMVolume",    VolumeToDb(data.bgmVolume));
            mixer.SetFloat("SFXVolume",    VolumeToDb(data.sfxVolume));
            mixer.SetFloat("VoiceVolume",  VolumeToDb(data.voiceVolume));
        }

        // volume == 0 → -80dB(무음), 나머지 → 로그 변환
        private static float VolumeToDb(int volume)
            => volume == 0 ? -80f : Mathf.Log10(volume / 10f) * 20f;
    }
}
