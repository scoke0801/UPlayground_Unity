using System;
using UnityEngine;

namespace UPlayGround.Data.Config
{
    [CreateAssetMenu(fileName = "SettingsData", menuName = "UPlayGround/Config/SettingsData")]
    public class SettingsData : ScriptableObject
    {
        [Header("게임플레이 - 카메라")]
        [Range(1, 10)] public int sensitivityX = 5;
        [Range(1, 10)] public int sensitivityY = 5;
        public bool invertY = false;

        [Header("게임플레이 - 전투")]
        public bool screenShake = true;
        public bool aimAssist = true;
        [Range(0f, 2f)] public float cameraShakeScale = 1f;
        [Range(0f, 1f)] public float combatCameraAutoCorrection = 1f;
        [Range(0f, 1f)] public float combatCameraSequenceIntensity = 1f;

        [Header("게임플레이 - 언어")]
        public int languageIndex = 0; // 0=한국어, 1=English, 2=日本語

        [Header("그래픽")]
        public int resolutionIndex = 0;
        public bool fullscreen = true;
        public int qualityIndex = 2; // 0=낮음 ~ 3=최고
        [Range(50, 150)] public int brightness = 100;

        [Header("오디오")]
        [Range(0, 10)] public int masterVolume = 8;
        [Range(0, 10)] public int bgmVolume = 7;
        [Range(0, 10)] public int sfxVolume = 9;
        [Range(0, 10)] public int voiceVolume = 8;

        [Header("디버그")]
        [Tooltip("모션 워핑 시스템 전역 활성/비활성. 회귀 의심 시 일시적으로 끄고 1차 동작 비교용.")]
        public bool debugMotionWarpEnabled = true;

        private const string PREFS_KEY = "GameSettings_v1";

        public void Save()
        {
            PlayerPrefs.SetString(PREFS_KEY, JsonUtility.ToJson(this));
            PlayerPrefs.Save();
        }

        public void Load()
        {
            string json = PlayerPrefs.GetString(PREFS_KEY, "");
            if (!string.IsNullOrEmpty(json))
                JsonUtility.FromJsonOverwrite(json, this);
        }

        public void ResetToDefault()
        {
            sensitivityX = 5; sensitivityY = 5; invertY = false;
            screenShake = true; aimAssist = true; languageIndex = 0;
            cameraShakeScale = 1f; combatCameraAutoCorrection = 1f; combatCameraSequenceIntensity = 1f;
            resolutionIndex = 0; fullscreen = true; qualityIndex = 2; brightness = 100;
            masterVolume = 8; bgmVolume = 7; sfxVolume = 9; voiceVolume = 8;
            debugMotionWarpEnabled = true;
        }
    }

    /// <summary>
    /// 취소(Cancel) 기능을 위한 설정값 스냅샷.
    /// UI 열릴 때 찍어두고, 취소 시 복원.
    /// </summary>
    [Serializable]
    public class SettingsSnapshot
    {
        public int sensitivityX, sensitivityY;
        public bool invertY, screenShake, aimAssist;
        public float cameraShakeScale, combatCameraAutoCorrection, combatCameraSequenceIntensity;
        public int languageIndex, resolutionIndex, qualityIndex, brightness;
        public bool fullscreen;
        public int masterVolume, bgmVolume, sfxVolume, voiceVolume;

        public static SettingsSnapshot From(SettingsData data) => new()
        {
            sensitivityX = data.sensitivityX, sensitivityY = data.sensitivityY,
            invertY = data.invertY, screenShake = data.screenShake, aimAssist = data.aimAssist,
            cameraShakeScale = data.cameraShakeScale,
            combatCameraAutoCorrection = data.combatCameraAutoCorrection,
            combatCameraSequenceIntensity = data.combatCameraSequenceIntensity,
            languageIndex = data.languageIndex, resolutionIndex = data.resolutionIndex,
            qualityIndex = data.qualityIndex, brightness = data.brightness,
            fullscreen = data.fullscreen,
            masterVolume = data.masterVolume, bgmVolume = data.bgmVolume,
            sfxVolume = data.sfxVolume, voiceVolume = data.voiceVolume
        };

        public void ApplyTo(SettingsData data)
        {
            data.sensitivityX = sensitivityX; data.sensitivityY = sensitivityY;
            data.invertY = invertY; data.screenShake = screenShake; data.aimAssist = aimAssist;
            data.cameraShakeScale = cameraShakeScale;
            data.combatCameraAutoCorrection = combatCameraAutoCorrection;
            data.combatCameraSequenceIntensity = combatCameraSequenceIntensity;
            data.languageIndex = languageIndex; data.resolutionIndex = resolutionIndex;
            data.qualityIndex = qualityIndex; data.brightness = brightness;
            data.fullscreen = fullscreen;
            data.masterVolume = masterVolume; data.bgmVolume = bgmVolume;
            data.sfxVolume = sfxVolume; data.voiceVolume = voiceVolume;
        }
    }
}
