#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    internal static class EnemyBlackboardKeysGenerator
    {
        private const string RegistryPath = "Assets/10.Datas/AI/BehaviorTree/BehaviorTreeEditorRegistry.json";
        private const string OutputPath = "Assets/02.Scripts/AI/BehaviorTree/Runtime/EnemyBlackboardKeys.generated.cs";

        [MenuItem("UPlayGround/생성 도구/Enemy Blackboard Keys 생성", false, 11)]
        public static void GenerateMenu()
        {
            Generate();
        }

        public static bool Generate()
        {
            var document = LoadRegistry();
            var entries = BuildEntries(document.enemyBlackboardDefaults);
            if (entries.Count == 0)
            {
                UnityEngine.Debug.LogError("[EnemyBlackboardKeysGenerator] 생성할 enemyBlackboardDefaults 항목이 없습니다.");
                return false;
            }

            WriteGeneratedFile(entries);
            UnityEngine.Debug.Log($"[EnemyBlackboardKeysGenerator] 생성 완료: {OutputPath}");
            return true;
        }

        private static BehaviorTreeEditorRegistryDocument LoadRegistry()
        {
            var textAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.TextAsset>(RegistryPath);
            if (textAsset == null)
                throw new FileNotFoundException($"Behavior Tree 에디터 레지스트리 파일을 찾을 수 없습니다: {RegistryPath}");

            return UnityEngine.JsonUtility.FromJson<BehaviorTreeEditorRegistryDocument>(textAsset.text)
                ?? new BehaviorTreeEditorRegistryDocument();
        }

        private static List<(string Identifier, string Key)> BuildEntries(IEnumerable<EnemyBlackboardDefaultEntryDefinition> definitions)
        {
            var entries = new List<(string Identifier, string Key)>();
            var seenIdentifiers = new Dictionary<string, string>(StringComparer.Ordinal);
            var seenKeys = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var definition in definitions)
            {
                if (definition == null || string.IsNullOrWhiteSpace(definition.key))
                    continue;

                if (seenKeys.TryGetValue(definition.key, out var existingIdentifier))
                    throw new InvalidDataException($"EnemyBlackboardKeys key 중복: \"{definition.key}\" (기존 식별자 {existingIdentifier})");

                var identifier = string.IsNullOrWhiteSpace(definition.identifier)
                    ? CreateIdentifier(definition.key)
                    : definition.identifier.Trim();

                if (seenIdentifiers.TryGetValue(identifier, out var existingKey))
                    throw new InvalidDataException($"EnemyBlackboardKeys 식별자 충돌: {identifier} ({existingKey}, {definition.key})");

                seenIdentifiers.Add(identifier, definition.key);
                seenKeys.Add(definition.key, identifier);
                entries.Add((identifier, definition.key));
            }

            return entries;
        }

        private static string CreateIdentifier(string key)
        {
            var builder = new StringBuilder(key.Length);
            var uppercaseNext = true;

            foreach (var c in key)
            {
                if (char.IsLetterOrDigit(c))
                {
                    builder.Append(uppercaseNext ? char.ToUpperInvariant(c) : c);
                    uppercaseNext = false;
                }
                else
                {
                    uppercaseNext = true;
                }
            }

            if (builder.Length == 0)
                return "Empty";
            if (char.IsDigit(builder[0]))
                builder.Insert(0, '_');
            return builder.ToString();
        }

        private static void WriteGeneratedFile(IReadOnlyList<(string Identifier, string Key)> entries)
        {
            var builder = new StringBuilder();
            builder.AppendLine("// 자동 생성 파일입니다. 직접 수정하지 마세요.");
            builder.AppendLine("// UPlayGround/생성 도구/Enemy Blackboard Keys 생성 메뉴에서 재생성하세요.");
            builder.AppendLine($"// Source: {RegistryPath}");
            builder.AppendLine("// Identifier rule: key를 PascalCase로 자동 변환하며, 충돌/가독성 문제가 있으면 JSON identifier 필드를 사용합니다.");
            builder.AppendLine();
            builder.AppendLine("namespace UPlayGround.AI.BehaviorTree");
            builder.AppendLine("{");
            builder.AppendLine("    public static partial class EnemyBlackboardKeys");
            builder.AppendLine("    {");

            foreach (var (identifier, key) in entries)
                builder.AppendLine($"        public const string {identifier} = \"{Escape(key)}\";");

            builder.AppendLine("    }");
            builder.AppendLine("}");

            var directory = Path.GetDirectoryName(OutputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(OutputPath, builder.ToString(), new UTF8Encoding(false));
            AssetDatabase.ImportAsset(OutputPath);
        }

        private static string Escape(string value)
            => value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
#endif