using UnityEngine;
using UPlayGround.Data.Config;

namespace UPlayGround.UI
{
    public class UISettingPageBase : MonoBehaviour
    {
        private bool _isBound;

        public void Bind(SettingsData settingsData)
        {
            if (_isBound || settingsData == null)
                return;

            BindControls(settingsData);
            _isBound = true;
        }

        protected virtual void BindControls(SettingsData settingsData)
        {
        }

        public virtual void SyncUIFromData(SettingsData settingsData)
        {
        }

        protected static int RoundToInt(float value) => Mathf.RoundToInt(value);

        protected static T GetAt<T>(T[] items, int index) where T : class
            => items != null && index >= 0 && index < items.Length ? items[index] : null;
    }
}
