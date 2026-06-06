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
        private static readonly (int width, int height)[] SupportedResolutions =
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
            if ((uint)data.resolutionIndex < (uint)SupportedResolutions.Length)
            {
                var (w, h) = SupportedResolutions[data.resolutionIndex];
                Screen.SetResolution(w, h, data.fullscreen);
            }

            QualitySettings.SetQualityLevel(data.qualityIndex, true);
            ApplyFrameTiming();
        }

        private static void ApplyFrameTiming()
        {
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = -1;
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
