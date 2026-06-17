using UnityEngine;
using UPlayGround.Data.Config;

public class UISettingPageGraphic : UISettingPageBase
{
    private static readonly string[] ResolutionOptions =
    {
        "1920x1080",
        "1280x720",
        "2560x1440"
    };

    private static readonly string[] WindowModeOptions =
    {
        "전체화면",
        "경계없는 창",
        "창 화면"
    };

    [Header("Controls")]
    [SerializeField] private UICommonDropdown _resolutionDropdown;
    [SerializeField] private UICommonDropdown _windowModeDropdown;
    [SerializeField] private UICommonSlider _frameRateSlider;
    [SerializeField] private UICommonSlider _brightnessSlider;

    protected override void BindControls(SettingsData settingsData)
    {
        if (_resolutionDropdown != null)
        {
            _resolutionDropdown.SetOptions(ResolutionOptions, settingsData.resolutionIndex);
            _resolutionDropdown.OnIndexChanged += index => settingsData.resolutionIndex = index;
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

        if (_frameRateSlider != null) _frameRateSlider.OnValueChanged += value => settingsData.targetFrameRate = RoundToInt(value);
        if (_brightnessSlider != null) _brightnessSlider.OnValueChanged += value => settingsData.brightness = RoundToInt(value);
    }

    public override void SyncUIFromData(SettingsData settingsData)
    {
        _resolutionDropdown?.SetIndexWithoutNotify(settingsData.resolutionIndex);
        _windowModeDropdown?.SetIndexWithoutNotify(settingsData.windowModeIndex);
        _frameRateSlider?.SetValueWithoutNotify(settingsData.targetFrameRate);
        _brightnessSlider?.SetValueWithoutNotify(settingsData.brightness);
    }
}
