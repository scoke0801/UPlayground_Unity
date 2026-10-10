using UPlayGround.Data.Config;

namespace UPlayGround.UI
{
    public class UISettingPageGamePlay : UISettingPageBase
    {
        [UnityEngine.SerializeField] private UISwitchButton _reduceHudMotion;
        private UICommonSlider[] _sliders;
        private UISwitchButton[] _switches;
        private UICommonDropdown[] _dropdowns;
        [UnityEngine.SerializeField] private UISwitchButton _hitCameraAssist;
        [UnityEngine.SerializeField] private UISwitchButton _movementCameraRecentering;

        protected override void BindControls(SettingsData settingsData)
        {
            CacheControls();
            if (_reduceHudMotion != null)
                _reduceHudMotion.OnValueChanged += value => settingsData.reduceHudMotion = value;

            var sensitivityX = GetAt(_sliders, 0);
            var sensitivityY = GetAt(_sliders, 1);
            var invertY = GetAt(_switches, 0);
            var screenShake = GetAt(_switches, 1);
            var combatVibration = GetAt(_switches, 3);
            var language = GetAt(_dropdowns, 0);
            var dialogueTypingSpeed = GetAt(_dropdowns, 1);
            var dialogueAutoDelay = GetAt(_dropdowns, 2);

            if (sensitivityX != null) sensitivityX.OnValueChanged += value => settingsData.sensitivityX = RoundToInt(value);
            if (sensitivityY != null) sensitivityY.OnValueChanged += value => settingsData.sensitivityY = RoundToInt(value);
            if (invertY != null) invertY.OnValueChanged += value => settingsData.invertY = value;
            if (screenShake != null) screenShake.OnValueChanged += value => settingsData.screenShake = value;
            if (combatVibration != null)
                combatVibration.OnValueChanged += value => settingsData.combatVibration = value;
            if (_hitCameraAssist != null)
                _hitCameraAssist.OnValueChanged += value => settingsData.hitCameraAssist = value;
            if (_movementCameraRecentering != null)
                _movementCameraRecentering.OnValueChanged += value => settingsData.movementCameraRecentering = value;
            if (language != null) language.OnIndexChanged += index => settingsData.languageIndex = index;
            if (dialogueTypingSpeed != null)
                dialogueTypingSpeed.OnIndexChanged += index => settingsData.dialogueTypingSpeedIndex = index;
            if (dialogueAutoDelay != null)
                dialogueAutoDelay.OnIndexChanged += index => settingsData.dialogueAutoDelayIndex = index;
        }

        public override void SyncUIFromData(SettingsData settingsData)
        {
            CacheControls();

            GetAt(_sliders, 0)?.SetValueWithoutNotify(settingsData.sensitivityX);
            GetAt(_sliders, 1)?.SetValueWithoutNotify(settingsData.sensitivityY);
            GetAt(_switches, 0)?.SetValueWithoutNotify(settingsData.invertY);
            GetAt(_switches, 1)?.SetValueWithoutNotify(settingsData.screenShake);
            GetAt(_switches, 3)?.SetValueWithoutNotify(settingsData.combatVibration);
            _reduceHudMotion?.SetValueWithoutNotify(settingsData.reduceHudMotion);
            _hitCameraAssist?.SetValueWithoutNotify(settingsData.hitCameraAssist);
            _movementCameraRecentering?.SetValueWithoutNotify(settingsData.movementCameraRecentering);
            GetAt(_dropdowns, 0)?.SetIndexWithoutNotify(settingsData.languageIndex);
            GetAt(_dropdowns, 1)?.SetIndexWithoutNotify(settingsData.dialogueTypingSpeedIndex);
            GetAt(_dropdowns, 2)?.SetIndexWithoutNotify(settingsData.dialogueAutoDelayIndex);
        }

        private void CacheControls()
        {
            _sliders ??= GetComponentsInChildren<UICommonSlider>(true);
            _switches ??= GetComponentsInChildren<UISwitchButton>(true);
            _dropdowns ??= GetComponentsInChildren<UICommonDropdown>(true);
        }
    }
}
