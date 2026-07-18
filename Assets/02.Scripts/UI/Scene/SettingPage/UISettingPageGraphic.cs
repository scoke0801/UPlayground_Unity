using UnityEngine;
using UPlayGround.Data.Config;

namespace UPlayGround.UI
{
    public class UISettingPageGraphic : UISettingPageBase
    {
        private static readonly string[] WindowModeOptions =
        {
            "전체화면",
            "경계없는 창",
            "창 화면"
        };

        [Header("Controls")]
        [SerializeField] private UICommonDropdown _resolutionDropdown;
        [SerializeField] private UICommonDropdown _windowModeDropdown;
        [SerializeField] private UICommonDropdown _qualityDropdown;
        [SerializeField] private UICommonSlider _frameRateSlider;
        [SerializeField] private UICommonSlider _brightnessSlider;
        private UISwitchButton[] _switches;

        protected override void BindControls(SettingsData settingsData)
        {
            CacheControls();

            if (_resolutionDropdown != null)
            {
                _resolutionDropdown.SetOptions(
                    UISvc.Settings.ResolutionOptions,
                    UISvc.Settings.GetCurrentResolutionOptionIndex());
                _resolutionDropdown.OnIndexChanged += UISvc.Settings.SetResolutionOption;
            }

            if (_windowModeDropdown != null)
            {
                _windowModeDropdown.SetOptions(WindowModeOptions, settingsData.windowModeIndex);
                _windowModeDropdown.OnIndexChanged += index =>
                {
                    settingsData.windowModeIndex = index;
                    settingsData.fullscreen = index != 2;
                };
            }

            if (_qualityDropdown != null)
            {
                _qualityDropdown.SetOptions(UISvc.Settings.QualityOptions, settingsData.qualityIndex);
                _qualityDropdown.OnIndexChanged += index => settingsData.qualityIndex = index;
            }

            if (_frameRateSlider != null) _frameRateSlider.OnValueChanged += value => settingsData.targetFrameRate = RoundToInt(value);
            if (_brightnessSlider != null) _brightnessSlider.OnValueChanged += value => settingsData.brightness = RoundToInt(value);

            var runInBackground = GetAt(_switches, 0);
            if (runInBackground != null)
                runInBackground.OnValueChanged += value => settingsData.runInBackground = value;
        }

        public override void SyncUIFromData(SettingsData settingsData)
        {
            CacheControls();

            _resolutionDropdown?.SetIndexWithoutNotify(UISvc.Settings.GetCurrentResolutionOptionIndex());
            _windowModeDropdown?.SetIndexWithoutNotify(settingsData.windowModeIndex);
            _qualityDropdown?.SetIndexWithoutNotify(settingsData.qualityIndex);
            _frameRateSlider?.SetValueWithoutNotify(settingsData.targetFrameRate);
            _brightnessSlider?.SetValueWithoutNotify(settingsData.brightness);
            GetAt(_switches, 0)?.SetValueWithoutNotify(settingsData.runInBackground);
        }

        private void CacheControls()
        {
            _switches ??= GetComponentsInChildren<UISwitchButton>(true);
        }
    }
}
