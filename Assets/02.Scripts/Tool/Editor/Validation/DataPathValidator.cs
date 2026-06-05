#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Tool.Editor.Validation
{
    public static class DataPathValidator
    {
        private const string DataRoot = "Assets/10.Datas";

        public static List<EditorValidationIssue> ValidateAll()
        {
            var issues = new List<EditorValidationIssue>();
            var targetDirectories = BuildTargetDirectoriesByType();

            foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadMainAssetAtPath(path) as ScriptableObject;
                if (!IsManagedDataAsset(asset, path))
                    continue;

                if (IsUnderDataRoot(path))
                    continue;

                Type type = asset.GetType();
                if (targetDirectories.TryGetValue(type, out string targetDirectory))
                {
                    Add(issues, EditorValidationSeverity.Warning, path, asset, "path",
                        "데이터 에셋이 Assets/10.Datas 하위에 있지 않습니다.",
                        $"경로 이동 실행 시 {targetDirectory} 하위로 이동합니다.");
                }
                else
                {
                    Add(issues, EditorValidationSeverity.Warning, path, asset, "path",
                        "데이터 에셋이 Assets/10.Datas 하위에 있지 않지만 기준 목적지를 찾지 못했습니다.",
                        "같은 타입의 기준 에셋을 Assets/10.Datas에 배치하거나 수동으로 이동하세요.");
                }
            }

            return issues;
        }

        public static DataPathMoveResult MoveAssetsToDataRoot()
        {
            var result = new DataPathMoveResult();
            var targetDirectories = BuildTargetDirectoriesByType();

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject"))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var asset = AssetDatabase.LoadMainAssetAtPath(path) as ScriptableObject;
                    if (!IsManagedDataAsset(asset, path) || IsUnderDataRoot(path))
                        continue;

                    Type type = asset.GetType();
                    if (!targetDirectories.TryGetValue(type, out string targetDirectory))
                    {
                        result.Skipped++;
                        continue;
                    }

                    EnsureAssetDirectory(targetDirectory);
                    string targetPath = AssetDatabase.GenerateUniqueAssetPath($"{targetDirectory}/{Path.GetFileName(path)}");
                    string error = AssetDatabase.MoveAsset(path, targetPath);
                    if (string.IsNullOrEmpty(error))
                    {
                        result.Moved++;
                        Debug.Log($"[DataPathValidator] 데이터 에셋 이동: {path} -> {targetPath}");
                    }
                    else
                    {
                        result.Failed++;
                        Debug.LogError($"[DataPathValidator] 데이터 에셋 이동 실패: {path} -> {targetPath}\n{error}");
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            return result;
        }

        private static Dictionary<Type, string> BuildTargetDirectoriesByType()
        {
            var counts = new Dictionary<Type, Dictionary<string, int>>();

            foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { DataRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadMainAssetAtPath(path) as ScriptableObject;
                if (!IsManagedDataAsset(asset, path))
                    continue;

                Type type = asset.GetType();
                string directory = Path.GetDirectoryName(path)?.Replace('\\', '/') ?? DataRoot;
                if (!counts.TryGetValue(type, out Dictionary<string, int> directoryCounts))
                {
                    directoryCounts = new Dictionary<string, int>(StringComparer.Ordinal);
                    counts.Add(type, directoryCounts);
                }

                directoryCounts.TryGetValue(directory, out int count);
                directoryCounts[directory] = count + 1;
            }

            var targets = new Dictionary<Type, string>();
            foreach (KeyValuePair<Type, Dictionary<string, int>> entry in counts)
            {
                string bestDirectory = null;
                int bestCount = -1;
                foreach (KeyValuePair<string, int> directory in entry.Value)
                {
                    if (directory.Value > bestCount
                        || (directory.Value == bestCount && string.Compare(directory.Key, bestDirectory, StringComparison.Ordinal) < 0))
                    {
                        bestDirectory = directory.Key;
                        bestCount = directory.Value;
                    }
                }

                if (!string.IsNullOrEmpty(bestDirectory))
                    targets[entry.Key] = bestDirectory;
            }

            return targets;
        }

        private static bool IsManagedDataAsset(ScriptableObject asset, string path)
        {
            if (asset == null || string.IsNullOrEmpty(path))
                return false;

            Type type = asset.GetType();
            string typeNamespace = type.Namespace ?? string.Empty;
            return typeNamespace.StartsWith("UPlayGround.", StringComparison.Ordinal)
                   && !typeNamespace.StartsWith("UPlayGround.Editor.", StringComparison.Ordinal)
                   && (typeNamespace.StartsWith("UPlayGround.Tool.Editor.Balance", StringComparison.Ordinal)
                       || !typeNamespace.StartsWith("UPlayGround.Tool.Editor.", StringComparison.Ordinal));
        }

        private static bool IsUnderDataRoot(string path)
        {
            return path.StartsWith(DataRoot + "/", StringComparison.OrdinalIgnoreCase);
        }

        private static void EnsureAssetDirectory(string assetDirectory)
        {
            if (AssetDatabase.IsValidFolder(assetDirectory))
                return;

            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), assetDirectory);
            Directory.CreateDirectory(fullPath);
        }

        private static void Add(
            List<EditorValidationIssue> issues,
            EditorValidationSeverity severity,
            string path,
            UnityEngine.Object asset,
            string field,
            string message,
            string fixHint)
        {
            issues.Add(new EditorValidationIssue(severity, "DataPath", path, asset, field, message, fixHint));
        }
    }

    public struct DataPathMoveResult
    {
        public int Moved;
        public int Skipped;
        public int Failed;
    }
}
#endif
