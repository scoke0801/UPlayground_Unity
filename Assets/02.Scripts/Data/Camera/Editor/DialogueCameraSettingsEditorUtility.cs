using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace UPlayGround.Data.Editor
{
    public static class DialogueCameraSettingsEditorUtility
    {
        private const string DefaultAssetPath = "Assets/10.Datas/Camera/DialogueCameraSettings.asset";

        [MenuItem("UPlayGround/World/Camera/Create Dialogue Camera Settings")]
        public static void CreateOrSelectSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<DialogueCameraSettingsSO>(DefaultAssetPath);
            if (settings == null)
            {
                string directory = System.IO.Path.GetDirectoryName(DefaultAssetPath);
                if (!System.IO.Directory.Exists(directory))
                    System.IO.Directory.CreateDirectory(directory);

                settings = ScriptableObject.CreateInstance<DialogueCameraSettingsSO>();
                AssetDatabase.CreateAsset(settings, DefaultAssetPath);
                AssetDatabase.SaveAssets();
            }

            EnsureAddressable(settings, DialogueCameraSettingsSO.AddressableKey);
            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }

        private static void EnsureAddressable(UnityEngine.Object asset, string address)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            string guid = AssetDatabase.AssetPathToGUID(path);
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null || string.IsNullOrEmpty(guid))
            {
                Debug.LogWarning("[DialogueCameraSettingsEditorUtility] Addressables Settings를 찾지 못해 주소 등록을 건너뜁니다.");
                return;
            }

            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
            entry.address = address;
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
            AssetDatabase.SaveAssets();
        }
    }
}
