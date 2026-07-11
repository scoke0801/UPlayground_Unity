using UPlayGround.Data.Config;

namespace UPlayGround.UI
{
    public class UISettingPageGamePlay : UISettingPageBase
    {
        private UICommonSlider[] _sliders;
        private UISwitchButton[] _switches;
        private UICommonDropdown[] _dropdowns;

        protected override void BindControls(SettingsData settingsData)
        {
            CacheControls();

            var sensitivityX = GetAt(_sliders, 0);
            var sensitivityY = GetAt(_sliders, 1);
            var invertY = GetAt(_switches, 0);
            var screenShake = GetAt(_switches, 1);
            var aimAssist = GetAt(_switches, 2);
            var language = GetAt(_dropdowns, 0);

            if (sensitivityX != null) sensitivityX.OnValueChanged += value => settingsData.sensitivityX = RoundToInt(value);
            if (sensitivityY != null) sensitivityY.OnValueChanged += value => settingsData.sensitivityY = RoundToInt(value);
            if (invertY != null) invertY.OnValueChanged += value => settingsData.invertY = value;
            if (screenShake != null) screenShake.OnValueChanged += value => settingsData.screenShake = value;
            if (aimAssist != null) aimAssist.OnValueChanged += value => settingsData.aimAssist = value;
            if (language != null) language.OnIndexChanged += index => settingsData.languageIndex = index;
        }

        public override void SyncUIFromData(SettingsData settingsData)
        {
            CacheControls();

            GetAt(_sliders, 0)?.SetValueWithoutNotify(settingsData.sensitivityX);
            GetAt(_sliders, 1)?.SetValueWithoutNotify(settingsData.sensitivityY);
            GetAt(_switches, 0)?.SetValueWithoutNotify(settingsData.invertY);
            GetAt(_switches, 1)?.SetValueWithoutNotify(settingsData.screenShake);
            GetAt(_switches, 2)?.SetValueWithoutNotify(settingsData.aimAssist);
            GetAt(_dropdowns, 0)?.SetIndexWithoutNotify(settingsData.languageIndex);
        }

        private void CacheControls()
        {
            _sliders ??= GetComponentsInChildren<UICommonSlider>(true);
            _switches ??= GetComponentsInChildren<UISwitchButton>(true);
            _dropdowns ??= GetComponentsInChildren<UICommonDropdown>(true);
        }
    }
}
