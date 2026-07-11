using System;
using UnityEditor;

namespace UPlayGround.Editor.P09Builder
{
    internal sealed class P09AssetCatalogPostprocessor : AssetPostprocessor
    {
        public static event Action CatalogRootChanged;

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (ContainsCatalogPath(importedAssets) ||
                ContainsCatalogPath(deletedAssets) ||
                ContainsCatalogPath(movedAssets) ||
                ContainsCatalogPath(movedFromAssetPaths))
            {
                CatalogRootChanged?.Invoke();
            }
        }

        private static bool ContainsCatalogPath(string[] paths)
        {
            if (paths == null) return false;
            for (int i = 0; i < paths.Length; i++)
            {
                var path = paths[i];
                if (!string.IsNullOrEmpty(path) &&
                    path.StartsWith(PathConfig.CatalogRoot, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}
