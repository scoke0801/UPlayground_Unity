using UPlayGround.Data.Config;

namespace UPlayGround.UI
{
    public class UISettingPageAudio : UISettingPageBase
    {
        private UICommonSlider[] _sliders;

        protected override void BindControls(SettingsData settingsData)
        {
            CacheControls();

            var master = GetAt(_sliders, 0);
            var bgm = GetAt(_sliders, 1);
            var sfx = GetAt(_sliders, 2);
            var voice = GetAt(_sliders, 3);

            if (master != null) master.OnValueChanged += value => settingsData.masterVolume = RoundToInt(value);
            if (bgm != null) bgm.OnValueChanged += value => settingsData.bgmVolume = RoundToInt(value);
            if (sfx != null) sfx.OnValueChanged += value => settingsData.sfxVolume = RoundToInt(value);
            if (voice != null) voice.OnValueChanged += value => settingsData.voiceVolume = RoundToInt(value);
        }

        public override void SyncUIFromData(SettingsData settingsData)
        {
            CacheControls();

            GetAt(_sliders, 0)?.SetValueWithoutNotify(settingsData.masterVolume);
            GetAt(_sliders, 1)?.SetValueWithoutNotify(settingsData.bgmVolume);
            GetAt(_sliders, 2)?.SetValueWithoutNotify(settingsData.sfxVolume);
            GetAt(_sliders, 3)?.SetValueWithoutNotify(settingsData.voiceVolume);
        }

        private void CacheControls()
        {
            _sliders ??= GetComponentsInChildren<UICommonSlider>(true);
        }
    }
}
