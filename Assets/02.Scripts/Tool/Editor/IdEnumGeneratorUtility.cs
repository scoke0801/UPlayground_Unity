// 에디터 전용 유틸리티 — IdEnumGeneratorWindow 및 각 DB 에디터에서 공유 사용
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Tool.Editor
{
    /// <summary>
    /// string-key / int-key 두 가지 패턴의 ID enum 파일을 생성하는 공통 유틸리티.
    /// </summary>
    public static class IdEnumGeneratorUtility
    {
        // ── 공개 API ──────────────────────────────────────────────────

        /// <summary>
        /// string 키 기반 enum을 생성한다 (FX, UI, CameraShake, Actor 등).
        /// extension: type.ToKey() → 원본 string 반환 (switch 문 생성).
        /// </summary>
        public static bool GenerateStringKeyEnum(
            string enumName,
            string extensionMethodName,
            string extensionReturnDoc,
            string outputPath,
            string namespaceName,
            IReadOnlyList<(string identifier, string originalKey)> entries,
            bool silent = false)
        {
            if (HasDuplicates(entries, e => e.identifier)) return false;

            var sb = new StringBuilder();
            WriteAutoGenHeader(sb);

            bool ns = !string.IsNullOrEmpty(namespaceName);
            string i1 = ns ? "    " : "";
            string i2 = ns ? "        " : "    ";

            if (ns) { sb.AppendLine($"namespace {namespaceName}"); sb.AppendLine("{"); }

            sb.AppendLine($"{i1}/// <summary>{enumName} — {extensionReturnDoc} 키 열거형 (자동 생성)</summary>");
            sb.AppendLine($"{i1}public enum {enumName}");
            sb.AppendLine($"{i1}{{");
            sb.AppendLine($"{i2}None = 0,");
            for (int idx = 0; idx < entries.Count; idx++)
                sb.AppendLine($"{i2}{entries[idx].identifier} = {idx + 1},");
            sb.AppendLine($"{i1}}}");
            sb.AppendLine();

            sb.AppendLine($"{i1}public static class {enumName}Extensions");
            sb.AppendLine($"{i1}{{");
            sb.AppendLine($"{i2}/// <summary>enum 값을 {extensionReturnDoc} 키 문자열로 변환한다.</summary>");
            sb.AppendLine($"{i2}public static string {extensionMethodName}(this {enumName} type) => type switch");
            sb.AppendLine($"{i2}{{");
            foreach (var (identifier, originalKey) in entries)
                sb.AppendLine($"{i2}    {enumName}.{identifier} => \"{Escape(originalKey)}\",");
            sb.AppendLine($"{i2}    _ => string.Empty,");
            sb.AppendLine($"{i2}}};");
            sb.AppendLine($"{i1}}}");

            if (ns) sb.AppendLine("}");

            WriteFile(outputPath, sb.ToString(), silent);
            return true;
        }

        /// <summary>
        /// int 키 기반 enum을 생성한다 (Item, Recipe 등).
        /// enum value = int ID 자체. extension: (int)type.
        /// </summary>
        public static bool GenerateIntKeyEnum(
            string enumName,
            string extensionMethodName,
            string extensionReturnDoc,
            string outputPath,
            string namespaceName,
            IReadOnlyList<(string identifier, int id)> entries,
            bool silent = false)
        {
            if (HasDuplicates(entries, e => e.identifier)) return false;

            var sb = new StringBuilder();
            WriteAutoGenHeader(sb);

            bool ns = !string.IsNullOrEmpty(namespaceName);
            string i1 = ns ? "    " : "";
            string i2 = ns ? "        " : "    ";

            if (ns) { sb.AppendLine($"namespace {namespaceName}"); sb.AppendLine("{"); }

            sb.AppendLine($"{i1}/// <summary>{enumName} — {extensionReturnDoc} int ID 열거형 (자동 생성). 값 자체가 ID이므로 (int)type으로 변환한다.</summary>");
            sb.AppendLine($"{i1}public enum {enumName}");
            sb.AppendLine($"{i1}{{");
            sb.AppendLine($"{i2}None = 0,");
            foreach (var (identifier, id) in entries)
                sb.AppendLine($"{i2}{identifier} = {id},");
            sb.AppendLine($"{i1}}}");
            sb.AppendLine();

            sb.AppendLine($"{i1}public static class {enumName}Extensions");
            sb.AppendLine($"{i1}{{");
            sb.AppendLine($"{i2}/// <summary>enum 값을 {extensionReturnDoc} int ID로 변환한다. (int)type과 동일.</summary>");
            sb.AppendLine($"{i2}public static int {extensionMethodName}(this {enumName} type) => (int)type;");
            sb.AppendLine($"{i1}}}");

            if (ns) sb.AppendLine("}");

            WriteFile(outputPath, sb.ToString(), silent);
            return true;
        }

        // ── 헬퍼 ──────────────────────────────────────────────────────

        /// <summary>ID 문자열을 유효한 C# 식별자로 변환한다.</summary>
        public static string SanitizeToIdentifier(string id)
        {
            if (string.IsNullOrEmpty(id)) return "_Empty";
            var sb = new StringBuilder(id.Length);
            foreach (char c in id)
                sb.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');
            if (char.IsDigit(sb[0])) sb.Insert(0, '_');
            return sb.ToString();
        }

        /// <summary>
        /// (rawName, key) 목록에서 중복 식별자를 제거하고 경고를 출력한다.
        /// </summary>
        public static List<(string identifier, T key)> DeduplicateEntries<T>(
            IEnumerable<(string rawName, T key)> source)
        {
            var result = new List<(string, T)>();
            var seen   = new Dictionary<string, T>();
            foreach (var (rawName, key) in source)
            {
                string id = SanitizeToIdentifier(rawName);
                if (seen.TryGetValue(id, out var existing))
                {
                    Debug.LogWarning($"[IdEnumGenerator] 식별자 충돌: '{key}'와 '{existing}' 모두 '{id}'로 변환됩니다. 중복 항목 제외.");
                    continue;
                }
                seen[id] = key;
                result.Add((id, key));
            }
            return result;
        }

        // ── 내부 ──────────────────────────────────────────────────────

        private static void WriteAutoGenHeader(StringBuilder sb)
        {
            sb.AppendLine("// 자동 생성 파일입니다. 직접 수정하지 마세요.");
            sb.AppendLine("// UPlayGround/ID Enum Generator 창에서 재생성하세요.");
            sb.AppendLine($"// Generated: {DateTime.Now:yyyy-MM-dd HH:mm}");
        }

        private static void WriteFile(string relativePath, string content, bool silent)
        {
            string fullPath = Path.GetFullPath(relativePath);
            string dir      = Path.GetDirectoryName(fullPath);
            if (dir != null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            File.WriteAllText(fullPath, content, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(relativePath);

            if (!silent)
                Debug.Log($"[IdEnumGenerator] 생성 완료: {relativePath}");
        }

        private static bool HasDuplicates<T>(IReadOnlyList<T> list, Func<T, string> selector)
        {
            var seen = new HashSet<string>();
            foreach (var item in list)
            {
                if (!seen.Add(selector(item)))
                {
                    Debug.LogError($"[IdEnumGenerator] 중복 식별자 발견. DeduplicateEntries를 먼저 호출하세요.");
                    return true;
                }
            }
            return false;
        }

        private static string Escape(string s) =>
            s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
#endif
