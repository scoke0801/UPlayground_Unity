#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using IOPath = System.IO.Path;

namespace UPlayGround.Data.Editor.Authoring
{
    /// <summary>
    /// 데이터 저작 패널의 자산 생성·복제·삭제와 Undo 등록을 한곳에서 처리합니다.
    /// </summary>
    public static class AssetCrudService
    {
        public static TAsset CreateAsset<TAsset>(
            string folderPath,
            string fileName,
            Action<TAsset> initialize = null,
            string undoName = "데이터 자산 생성") where TAsset : ScriptableObject
        {
            return (TAsset)CreateAsset(typeof(TAsset), folderPath, fileName,
                asset => initialize?.Invoke((TAsset)asset), undoName);
        }

        public static ScriptableObject CreateAsset(
            Type assetType,
            string folderPath,
            string fileName,
            Action<ScriptableObject> initialize = null,
            string undoName = "데이터 자산 생성")
        {
            if (assetType == null || !typeof(ScriptableObject).IsAssignableFrom(assetType))
                throw new ArgumentException("ScriptableObject 타입만 생성할 수 있습니다.", nameof(assetType));

            EnsureAssetFolder(folderPath);
            string safeFileName = SanitizeFileName(fileName);
            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/{safeFileName}.asset");
            var asset = ScriptableObject.CreateInstance(assetType);

            try
            {
                initialize?.Invoke(asset);
                AssetDatabase.CreateAsset(asset, assetPath);
                Undo.RegisterCreatedObjectUndo(asset, undoName);
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();
                return asset;
            }
            catch
            {
                if (asset != null && !AssetDatabase.Contains(asset))
                    Object.DestroyImmediate(asset);
                throw;
            }
        }

        public static TAsset DuplicateAsset<TAsset>(
            TAsset source,
            Action<TAsset> initializeCopy = null,
            string undoName = "데이터 자산 복제") where TAsset : Object
        {
            if (source == null)
                return null;

            string sourcePath = AssetDatabase.GetAssetPath(source);
            if (string.IsNullOrEmpty(sourcePath))
                throw new InvalidOperationException("프로젝트 자산만 복제할 수 있습니다.");

            string directory = IOPath.GetDirectoryName(sourcePath)?.Replace('\\', '/');
            string fileName = IOPath.GetFileName(sourcePath);
            string copyPath = AssetDatabase.GenerateUniqueAssetPath($"{directory}/{fileName}");
            if (!AssetDatabase.CopyAsset(sourcePath, copyPath))
                throw new InvalidOperationException($"자산 복제에 실패했습니다: {sourcePath}");

            TAsset copy = AssetDatabase.LoadAssetAtPath<TAsset>(copyPath);
            if (copy == null)
                throw new InvalidOperationException($"복제한 자산을 불러오지 못했습니다: {copyPath}");

            Undo.RegisterCreatedObjectUndo(copy, undoName);
            if (initializeCopy != null)
            {
                Undo.RecordObject(copy, undoName);
                initializeCopy(copy);
                EditorUtility.SetDirty(copy);
            }

            AssetDatabase.SaveAssets();
            return copy;
        }

        public static bool DeleteAsset(Object asset, string undoName = "데이터 자산 삭제")
        {
            if (asset == null || string.IsNullOrEmpty(AssetDatabase.GetAssetPath(asset)))
                return false;

            Undo.DestroyObjectImmediate(asset);
            AssetDatabase.SaveAssets();
            return asset == null;
        }

        public static void EnsureAssetFolder(string folderPath)
        {
            string normalized = (folderPath ?? string.Empty).Replace('\\', '/').TrimEnd('/');
            if (!normalized.Equals("Assets", StringComparison.Ordinal)
                && !normalized.StartsWith("Assets/", StringComparison.Ordinal))
            {
                throw new ArgumentException("저장 경로는 Assets 폴더 아래여야 합니다.", nameof(folderPath));
            }

            if (AssetDatabase.IsValidFolder(normalized))
                return;

            string[] segments = normalized.Split('/');
            string current = segments[0];
            for (int i = 1; i < segments.Length; i++)
            {
                string next = $"{current}/{segments[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    string guid = AssetDatabase.CreateFolder(current, segments[i]);
                    if (string.IsNullOrEmpty(guid))
                        throw new IOException($"자산 폴더 생성에 실패했습니다: {next}");
                }
                current = next;
            }
        }

        public static bool TryConvertAbsoluteFolderToAssetPath(string absolutePath, out string assetPath)
        {
            assetPath = null;
            if (string.IsNullOrWhiteSpace(absolutePath))
                return false;

            try
            {
                string assetsRoot = IOPath.GetFullPath(Application.dataPath)
                    .TrimEnd(IOPath.DirectorySeparatorChar, IOPath.AltDirectorySeparatorChar);
                string selected = IOPath.GetFullPath(absolutePath)
                    .TrimEnd(IOPath.DirectorySeparatorChar, IOPath.AltDirectorySeparatorChar);
                if (!IsSameOrChildPath(selected, assetsRoot))
                    return false;

                string suffix = selected.Length == assetsRoot.Length
                    ? string.Empty
                    : selected.Substring(assetsRoot.Length).Replace('\\', '/');
                assetPath = "Assets" + suffix;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool IsAssetPathWithin(string assetPath, string rootAssetPath)
        {
            string normalizedAssetPath = (assetPath ?? string.Empty).Replace('\\', '/').TrimEnd('/');
            string normalizedRootPath = (rootAssetPath ?? string.Empty).Replace('\\', '/').TrimEnd('/');
            if (!normalizedAssetPath.Equals("Assets", StringComparison.Ordinal)
                && !normalizedAssetPath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                return false;
            }
            if (!normalizedRootPath.Equals("Assets", StringComparison.Ordinal)
                && !normalizedRootPath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                return false;
            }

            try
            {
                string projectRoot = IOPath.GetFullPath(IOPath.Combine(Application.dataPath, ".."));
                string candidate = IOPath.GetFullPath(IOPath.Combine(
                    projectRoot,
                    normalizedAssetPath.Replace('/', IOPath.DirectorySeparatorChar)));
                string allowedRoot = IOPath.GetFullPath(IOPath.Combine(
                    projectRoot,
                    normalizedRootPath.Replace('/', IOPath.DirectorySeparatorChar)));
                return IsSameOrChildPath(candidate, allowedRoot);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool IsSameOrChildPath(string candidate, string root)
        {
            string normalizedRoot = root.TrimEnd(IOPath.DirectorySeparatorChar, IOPath.AltDirectorySeparatorChar);
            if (candidate.Equals(normalizedRoot, StringComparison.OrdinalIgnoreCase))
                return true;

            string boundary = normalizedRoot + IOPath.DirectorySeparatorChar;
            return candidate.StartsWith(boundary, StringComparison.OrdinalIgnoreCase);
        }

        private static string SanitizeFileName(string fileName)
        {
            string result = string.IsNullOrWhiteSpace(fileName) ? "NewAsset" : fileName.Trim();
            foreach (char invalidCharacter in IOPath.GetInvalidFileNameChars())
                result = result.Replace(invalidCharacter, '_');
            return result;
        }
    }
}
#endif
