using System;
using UnityEngine;

namespace UPlayGround.Data.Config
{
    [CreateAssetMenu(fileName = "SettingsData", menuName = "UPlayGround/설정/Settings Data")]
    public class SettingsData : ScriptableObject
    {
        [Header("게임플레이 - 카메라")]
        [Range(1, 10)] public int sensitivityX = 5;
        [Range(1, 10)] public int sensitivityY = 5;
        public bool invertY = false;

        [Header("게임플레이 - 전투")]
        public bool screenShake = true;
        public bool aimAssist = true;
        [Tooltip("게임패드 전투 진동을 사용합니다.")]
        public bool combatVibration = true;
        [Range(0f, 1f)]
        [Tooltip("전투 진동의 전체 세기 배율입니다.")]
        public float combatVibrationIntensity = 1f;
        [Range(0f, 2f)] public float cameraShakeScale = 1f;
        [Range(0f, 1f)] public float combatCameraAutoCorrection = 1f;
        [Range(0f, 1f)] public float combatCameraSequenceIntensity = 1f;

        [Header("게임플레이 - 언어")]
        public int languageIndex = 0; // 0=한국어, 1=English, 2=日本語

        [Header("게임플레이 - 대화")]
        [Tooltip("대화 타이핑 속도. 0=느림, 1=보통, 2=빠름")]
        [Range(0, 2)] public int dialogueTypingSpeedIndex = 1;
        [Tooltip("자동 재생 시 다음 대사까지 대기 시간. 0=느림, 1=보통, 2=빠름")]
        [Range(0, 2)] public int dialogueAutoDelayIndex = 1;

        /// <summary>
        /// 노드별 typingSpeed에 곱할 전역 배율. 값이 클수록 느리게 찍힙니다.
        /// </summary>
        public float DialogueTypingSpeedScale => dialogueTypingSpeedIndex switch
        {
            0 => 1.8f,
            2 => 0.45f,
            _ => 1f
        };

        /// <summary>
        /// 자동 재생 전역 대기 시간(초). 노드의 autoAdvanceDuration과 max로 결합됩니다.
        /// </summary>
        public float DialogueAutoAdvanceDelay => dialogueAutoDelayIndex switch
        {
            0 => 2.5f,
            2 => 0.7f,
            _ => 1.4f
        };

        [Header("그래픽")]
        public int resolutionIndex = 0;
        [Tooltip("선택한 해상도 너비. resolutionIndex는 UI 표시용이며 실제 적용에는 이 값을 사용합니다.")]
        public int resolutionWidth = 1920;
        [Tooltip("선택한 해상도 높이. resolutionIndex는 UI 표시용이며 실제 적용에는 이 값을 사용합니다.")]
        public int resolutionHeight = 1080;
        public int windowModeIndex = 1; // 0=전체화면, 1=경계없는 창, 2=창 화면
        public bool fullscreen = true;
        public int qualityIndex = 2; // 0=낮음 ~ 3=최고
        [Range(30, 144)] public int targetFrameRate = 60;
        [Range(0, 10)] public int brightness = 5;
        [Tooltip("창이 포커스를 잃어도 게임 로직과 오디오를 계속 실행합니다.")]
        public bool runInBackground = false;

        [Header("오디오")]
        [Range(0, 10)] public int masterVolume = 8;
        [Range(0, 10)] public int bgmVolume = 7;
        [Range(0, 10)] public int sfxVolume = 9;
        [Range(0, 10)] public int voiceVolume = 8;

        [Header("디버그")]
        [Tooltip("모션 워핑 시스템 전역 활성/비활성. 회귀 의심 시 일시적으로 끄고 1차 동작 비교용.")]
        public bool debugMotionWarpEnabled = true;

        private const string PREFS_KEY = "GameSettings_v1";

        public void Save(bool flushPlayerPrefs = true)
        {
            PlayerPrefs.SetString(PREFS_KEY, JsonUtility.ToJson(this));
            if (flushPlayerPrefs)
                PlayerPrefs.Save();
        }

        public void Load()
        {
            string json = PlayerPrefs.GetString(PREFS_KEY, "");
            if (!string.IsNullOrEmpty(json))
            {
                JsonUtility.FromJsonOverwrite(json, this);

                // 구버전 저장 데이터에는 실제 너비/높이가 없으므로 기존 인덱스를 한 번 변환한다.
                if (!json.Contains("\"resolutionWidth\""))
                {
                    (resolutionWidth, resolutionHeight) = resolutionIndex switch
                    {
                        1 => (1280, 720),
                        2 => (2560, 1440),
                        _ => (1920, 1080)
                    };
                }
            }
        }

        public void ResetToDefault()
        {
            sensitivityX = 5; sensitivityY = 5; invertY = false;
            screenShake = true; aimAssist = true;
            combatVibration = true; combatVibrationIntensity = 1f;
            languageIndex = 0;
            dialogueTypingSpeedIndex = 1; dialogueAutoDelayIndex = 1;
            cameraShakeScale = 1f; combatCameraAutoCorrection = 1f; combatCameraSequenceIntensity = 1f;
            resolutionIndex = 0; resolutionWidth = 1920; resolutionHeight = 1080;
            windowModeIndex = 1; fullscreen = true; qualityIndex = 2; targetFrameRate = 60; brightness = 5;
            runInBackground = false;
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
        public bool invertY, screenShake, aimAssist, combatVibration;
        public float combatVibrationIntensity;
        public float cameraShakeScale, combatCameraAutoCorrection, combatCameraSequenceIntensity;
        public int languageIndex, resolutionIndex, resolutionWidth, resolutionHeight;
        public int dialogueTypingSpeedIndex, dialogueAutoDelayIndex;
        public int windowModeIndex, qualityIndex, targetFrameRate, brightness;
        public bool fullscreen, runInBackground;
        public int masterVolume, bgmVolume, sfxVolume, voiceVolume;

        public static SettingsSnapshot From(SettingsData data) => new()
        {
            sensitivityX = data.sensitivityX, sensitivityY = data.sensitivityY,
            invertY = data.invertY, screenShake = data.screenShake, aimAssist = data.aimAssist,
            combatVibration = data.combatVibration,
            combatVibrationIntensity = data.combatVibrationIntensity,
            cameraShakeScale = data.cameraShakeScale,
            combatCameraAutoCorrection = data.combatCameraAutoCorrection,
            combatCameraSequenceIntensity = data.combatCameraSequenceIntensity,
            languageIndex = data.languageIndex, resolutionIndex = data.resolutionIndex,
            dialogueTypingSpeedIndex = data.dialogueTypingSpeedIndex,
            dialogueAutoDelayIndex = data.dialogueAutoDelayIndex,
            resolutionWidth = data.resolutionWidth, resolutionHeight = data.resolutionHeight,
            windowModeIndex = data.windowModeIndex, qualityIndex = data.qualityIndex,
            targetFrameRate = data.targetFrameRate, brightness = data.brightness,
            fullscreen = data.fullscreen,
            runInBackground = data.runInBackground,
            masterVolume = data.masterVolume, bgmVolume = data.bgmVolume,
            sfxVolume = data.sfxVolume, voiceVolume = data.voiceVolume
        };

        public void ApplyTo(SettingsData data)
        {
            data.sensitivityX = sensitivityX; data.sensitivityY = sensitivityY;
            data.invertY = invertY; data.screenShake = screenShake; data.aimAssist = aimAssist;
            data.combatVibration = combatVibration;
            data.combatVibrationIntensity = combatVibrationIntensity;
            data.cameraShakeScale = cameraShakeScale;
            data.combatCameraAutoCorrection = combatCameraAutoCorrection;
            data.combatCameraSequenceIntensity = combatCameraSequenceIntensity;
            data.languageIndex = languageIndex; data.resolutionIndex = resolutionIndex;
            data.dialogueTypingSpeedIndex = dialogueTypingSpeedIndex;
            data.dialogueAutoDelayIndex = dialogueAutoDelayIndex;
            data.resolutionWidth = resolutionWidth; data.resolutionHeight = resolutionHeight;
            data.windowModeIndex = windowModeIndex; data.qualityIndex = qualityIndex;
            data.targetFrameRate = targetFrameRate; data.brightness = brightness;
            data.fullscreen = fullscreen;
            data.runInBackground = runInBackground;
            data.masterVolume = masterVolume; data.bgmVolume = bgmVolume;
            data.sfxVolume = sfxVolume; data.voiceVolume = voiceVolume;
        }
    }
}
