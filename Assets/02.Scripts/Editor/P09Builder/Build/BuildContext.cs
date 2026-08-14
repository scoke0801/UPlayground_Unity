using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Editor.P09Builder
{
    public sealed class BuildContext
    {
        public CharacterBuildConfig Config { get; }
        public GameObject RootInstance { get; set; }
        public string PrefabFolder { get; set; }
        public string PrefabName { get; set; }
        public List<ScriptableObject> GeneratedDescs { get; } = new();
        public List<string> GeneratedAssetPaths { get; } = new();
        public List<string> Logs { get; } = new();
        public Dictionary<string, object> Bag { get; } = new();
        private readonly Dictionary<UnityEngine.Object, UnityEngine.Object> _stagedAssetBackups = new();

        public BuildContext(CharacterBuildConfig config)
        {
            Config = config;
        }

        /// <summary>기존 에셋을 처음 변경하기 직전의 메모리 스냅샷을 한 번만 보관한다.</summary>
        public void StageAssetForUpdate(UnityEngine.Object asset)
        {
            if (asset == null || _stagedAssetBackups.ContainsKey(asset))
                return;

            UnityEngine.Object backup = UnityEngine.Object.Instantiate(asset);
            backup.hideFlags = HideFlags.HideAndDontSave;
            _stagedAssetBackups.Add(asset, backup);
        }

        /// <summary>중간 단계 실패 시 제자리 갱신한 기존 에셋의 직렬화 상태를 복구한다.</summary>
        public void RestoreStagedAssetBackups()
        {
            foreach (KeyValuePair<UnityEngine.Object, UnityEngine.Object> pair in _stagedAssetBackups)
            {
                if (pair.Key == null || pair.Value == null)
                    continue;

                string assetName = pair.Key.name;
                HideFlags assetHideFlags = pair.Key.hideFlags;
                EditorUtility.CopySerialized(pair.Value, pair.Key);
                pair.Key.name = assetName;
                pair.Key.hideFlags = assetHideFlags;
                EditorUtility.SetDirty(pair.Key);
            }

            DiscardStagedAssetBackups();
        }

        public void DiscardStagedAssetBackups()
        {
            foreach (UnityEngine.Object backup in _stagedAssetBackups.Values)
                if (backup != null)
                    UnityEngine.Object.DestroyImmediate(backup);
            _stagedAssetBackups.Clear();
        }
    }
}
