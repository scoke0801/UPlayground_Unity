using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;

namespace UPlayGround.Tool.Editor.Build
{
    /// <summary>시각 대신 에셋 의존성과 설정 내용으로 재빌드 입력을 비교한다.</summary>
    public static class BuildManagerFingerprint
    {
        /// <summary>씬 의존성과 프로젝트 설정·빌드 옵션을 exe 입력으로 기록한다.</summary>
        public static string ComputePlayerSettings(BuildManagerProfileSO profile)
        {
            var rows = new List<string> { profile.buildTarget.ToString(), profile.GetBuildOptions().ToString() };
            foreach (string scene in profile.GetScenePaths())
                rows.Add(scene + ":" + (string.IsNullOrEmpty(scene) ? "missing" : AssetDatabase.GetAssetDependencyHash(scene).ToString()));
            foreach (string path in Directory.GetFiles("ProjectSettings", "*.asset"))
            {
                // 씬 목록은 프로필이 정본이며 전역 목록은 빌드 중에만 교체한다.
                if (Path.GetFileName(path) == "EditorBuildSettings.asset") continue;
                rows.Add(path + ":" + File.ReadAllText(path));
            }
            return HashRows(rows);
        }

        /// <summary>주소 지정 에셋의 간접 참조와 그룹·프로필 설정을 번들 입력으로 기록한다.</summary>
        public static string ComputeContent(AddressableAssetSettings settings)
        {
            if (settings == null) return string.Empty;
            var rows = new List<string> { settings.activeProfileId, "remoteCatalog:" + settings.BuildRemoteCatalog };
            foreach (string variable in settings.profileSettings.GetVariableNames())
                rows.Add(variable + ":" + settings.profileSettings.GetValueByName(settings.activeProfileId, variable));
            foreach (AddressableAssetGroup group in settings.groups)
            {
                if (group == null) continue;
                rows.Add(EditorJsonUtility.ToJson(group));
                foreach (AddressableAssetGroupSchema schema in group.Schemas)
                    rows.Add(EditorJsonUtility.ToJson(schema));
                foreach (AddressableAssetEntry entry in group.entries)
                    rows.Add(entry.guid + ":" + AssetDatabase.GetAssetDependencyHash(entry.AssetPath));
            }
            return HashRows(rows);
        }

        /// <summary>기존 콘텐츠를 exe 빌드에 재사용할 때 확인할 플랫폼별 캐시 기록 위치.</summary>
        public static string GetContentRecordDirectory(BuildTarget target) => Path.Combine("Library", "BuildManager", target.ToString());

        private static string HashRows(List<string> rows)
        {
            rows.Sort(StringComparer.Ordinal);
            using var sha = SHA256.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(string.Join("\n", rows));
            return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", string.Empty);
        }
    }
}
