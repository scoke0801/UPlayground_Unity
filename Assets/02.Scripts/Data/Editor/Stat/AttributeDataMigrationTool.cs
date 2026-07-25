using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Stat;

namespace UPlayGround.Data.Editor.Stat
{
    /// <summary>
    /// 기존 직렬화 Attribute 문자열을 Registry의 정규 ID로 변환한다.
    /// 드라이런에서 미등록 0건을 확인한 뒤에만 적용할 수 있다.
    /// </summary>
    public static class AttributeDataMigrationTool
    {
        private static readonly Regex AttributeLinePattern = new(
            @"^(?<prefix>\s*(?:-\s*)?(?:_attributeId|attributeId):\s*)(?<value>.*?)(?<suffix>\s*)$",
            RegexOptions.Compiled);

        private static readonly string[] Roots =
        {
            "Assets/01.Scenes",
            "Assets/03.Prefabs",
            "Assets/10.Datas",
            "Assets/Resources",
        };

        [UPlayGround.EditorTools.UPlaygroundTool(
            "UPlayGround/게임플레이/Attribute/데이터 마이그레이션 드라이런",
            priority = 221)]
        public static void DryRunFromMenu()
        {
            MigrationReport report = Scan();
            LogReport(report, false);
        }

        [UPlayGround.EditorTools.UPlaygroundTool(
            "UPlayGround/게임플레이/Attribute/데이터 마이그레이션 적용",
            priority = 222)]
        public static void ApplyFromMenu()
        {
            MigrationReport report = Scan();
            LogReport(report, false);
            if (report.Unregistered.Count > 0)
            {
                Debug.LogError(
                    "[Attribute] 미등록 문자열이 있어 마이그레이션 적용을 차단했습니다.");
                return;
            }
            if (report.Changes.Count == 0)
            {
                Debug.Log("[Attribute] 정규화할 데이터가 없습니다.");
                return;
            }
            if (!EditorUtility.DisplayDialog(
                    "Attribute 데이터 마이그레이션",
                    $"{report.Changes.Count}개 참조를 정규 ID로 변경합니다. 계속할까요?",
                    "적용",
                    "취소"))
                return;
            Apply(report);
        }

        internal static MigrationReport Scan()
        {
            var report = new MigrationReport();
            string projectRoot =
                Directory.GetParent(Application.dataPath)?.FullName
                ?? throw new InvalidOperationException(
                    "Unity 프로젝트 루트를 확인하지 못했습니다.");
            foreach (string root in Roots)
            {
                string absoluteRoot =
                    System.IO.Path.Combine(projectRoot, root);
                if (!Directory.Exists(absoluteRoot)) continue;
                foreach (string path in Directory.EnumerateFiles(
                             absoluteRoot,
                             "*",
                             SearchOption.AllDirectories))
                {
                    string extension =
                        System.IO.Path.GetExtension(path);
                    if (!string.Equals(
                            extension,
                            ".asset",
                            StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(
                            extension,
                            ".prefab",
                            StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(
                            extension,
                            ".unity",
                            StringComparison.OrdinalIgnoreCase))
                        continue;
                    ScanFile(projectRoot, path, report);
                }
            }
            return report;
        }

        internal static void Apply(MigrationReport report)
        {
            if (report == null)
                throw new ArgumentNullException(nameof(report));
            if (report.Unregistered.Count > 0)
                throw new InvalidOperationException(
                    "미등록 Attribute가 포함된 리포트는 적용할 수 없습니다.");

            var paths = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < report.Changes.Count; i++)
                paths.Add(report.Changes[i].AbsolutePath);
            var backups = new Dictionary<string, byte[]>(
                StringComparer.Ordinal);
            foreach (string path in paths)
                backups[path] = File.ReadAllBytes(path);

            bool editing = false;
            bool refreshDisabled = false;
            try
            {
                AssetDatabase.DisallowAutoRefresh();
                refreshDisabled = true;
                AssetDatabase.StartAssetEditing();
                editing = true;
                foreach (string path in paths)
                {
                    byte[] bytes = backups[path];
                    bool bom = HasUtf8Bom(bytes);
                    string contents = Encoding.UTF8.GetString(
                        bytes,
                        bom ? 3 : 0,
                        bytes.Length - (bom ? 3 : 0));
                    string changed = AttributeLinePattern.Replace(
                        contents,
                        match =>
                        {
                            string value = Unquote(
                                match.Groups["value"].Value.Trim());
                            if (!AttributeRegistry.Registry.TryResolve(
                                    value,
                                    out AttributeRegistryEntry entry)
                                || string.Equals(
                                    value,
                                    entry.attributeId,
                                    StringComparison.Ordinal))
                                return match.Value;
                            return match.Groups["prefix"].Value
                                   + entry.attributeId
                                   + match.Groups["suffix"].Value;
                        });
                    File.WriteAllText(
                        path,
                        changed,
                        new UTF8Encoding(bom));
                }
            }
            catch
            {
                foreach (KeyValuePair<string, byte[]> backup in backups)
                    File.WriteAllBytes(backup.Key, backup.Value);
                throw;
            }
            finally
            {
                if (editing) AssetDatabase.StopAssetEditing();
                if (refreshDisabled) AssetDatabase.AllowAutoRefresh();
                AssetDatabase.Refresh(
                    ImportAssetOptions.ForceSynchronousImport);
            }

            MigrationReport verification = Scan();
            if (verification.Unregistered.Count > 0
                || verification.Changes.Count > 0)
            {
                foreach (KeyValuePair<string, byte[]> backup in backups)
                    File.WriteAllBytes(backup.Key, backup.Value);
                AssetDatabase.Refresh(
                    ImportAssetOptions.ForceSynchronousImport);
                throw new InvalidOperationException(
                    "마이그레이션 사후 검증에 실패해 모든 파일을 복구했습니다.");
            }
            LogReport(report, true);
        }

        private static void ScanFile(
            string projectRoot,
            string absolutePath,
            MigrationReport report)
        {
            int lineNumber = 0;
            foreach (string line in File.ReadLines(absolutePath))
            {
                lineNumber++;
                Match match = AttributeLinePattern.Match(line);
                if (!match.Success) continue;
                string value =
                    Unquote(match.Groups["value"].Value.Trim());
                if (string.IsNullOrEmpty(value)) continue;
                string relativePath = absolutePath
                    .Substring(projectRoot.Length + 1)
                    .Replace('\\', '/');
                if (!AttributeRegistry.Registry.TryResolve(
                        value,
                        out AttributeRegistryEntry entry))
                {
                    report.Unregistered.Add(
                        $"{relativePath}:{lineNumber} {value}");
                    continue;
                }
                if (!string.Equals(
                        value,
                        entry.attributeId,
                        StringComparison.Ordinal))
                {
                    report.Changes.Add(new MigrationChange(
                        absolutePath,
                        relativePath,
                        lineNumber,
                        value,
                        entry.attributeId));
                }
            }
        }

        private static string Unquote(string value) =>
            value.Length >= 2
            && value[0] == '"'
            && value[value.Length - 1] == '"'
                ? value.Substring(1, value.Length - 2)
                : value;

        private static bool HasUtf8Bom(byte[] bytes) =>
            bytes.Length >= 3
            && bytes[0] == 0xEF
            && bytes[1] == 0xBB
            && bytes[2] == 0xBF;

        private static void LogReport(
            MigrationReport report,
            bool applied)
        {
            var builder = new StringBuilder();
            builder.AppendLine(
                applied
                    ? "[Attribute] 데이터 마이그레이션 적용 완료"
                    : "[Attribute] 데이터 마이그레이션 드라이런");
            builder.AppendLine($"정규화 예정: {report.Changes.Count}");
            for (int i = 0; i < report.Changes.Count; i++)
                builder.AppendLine(report.Changes[i].ToString());
            builder.AppendLine($"미등록(차단): {report.Unregistered.Count}");
            for (int i = 0; i < report.Unregistered.Count; i++)
                builder.AppendLine(report.Unregistered[i]);
            if (report.Unregistered.Count > 0)
                Debug.LogError(builder.ToString());
            else
                Debug.Log(builder.ToString());
        }

        internal sealed class MigrationReport
        {
            public readonly List<MigrationChange> Changes = new();
            public readonly List<string> Unregistered = new();
        }

        internal readonly struct MigrationChange
        {
            public readonly string AbsolutePath;
            public readonly string AssetPath;
            public readonly int LineNumber;
            public readonly string Before;
            public readonly string After;

            public MigrationChange(
                string absolutePath,
                string assetPath,
                int lineNumber,
                string before,
                string after)
            {
                AbsolutePath = absolutePath;
                AssetPath = assetPath;
                LineNumber = lineNumber;
                Before = before;
                After = after;
            }

            public override string ToString() =>
                $"{AssetPath}:{LineNumber} {Before} -> {After}";
        }
    }
}
