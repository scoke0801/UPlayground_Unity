using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UPlayGround.Data.Stat;

namespace UPlayGround.Gameplay.Tag.Editor
{
    internal enum GameplayTagReferenceKind
    {
        Registry,
        SerializedData,
        Code,
    }

    internal readonly struct GameplayTagReference
    {
        public readonly string TagName;
        public readonly string AssetPath;
        public readonly int LineNumber;
        public readonly GameplayTagReferenceKind Kind;
        public readonly string Preview;

        public GameplayTagReference(
            string tagName,
            string assetPath,
            int lineNumber,
            GameplayTagReferenceKind kind,
            string preview)
        {
            TagName = tagName;
            AssetPath = assetPath;
            LineNumber = lineNumber;
            Kind = kind;
            Preview = preview;
        }
    }

    internal static class GameplayTagReferenceSearch
    {
        internal const string RegistryPath =
            "Assets/Resources/GameplayTagRegistry.asset";

        private static readonly string[] SerializedRoots =
        {
            "Assets/01.Scenes",
            "Assets/03.Prefabs",
            "Assets/10.Datas",
            "Assets/Resources",
        };

        private static readonly string[] CodeRoots =
        {
            "Assets/02.Scripts",
            "Assets/Tests",
        };

        private static readonly HashSet<string> SerializedExtensions =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ".asset",
                ".prefab",
                ".unity",
            };

        public static List<GameplayTagReference> Find(
            string tagName,
            bool includeDescendants)
        {
            GameplayTagRegistrySO registry =
                AssetDatabase.LoadAssetAtPath<GameplayTagRegistrySO>(
                    RegistryPath);
            return Find(
                tagName,
                includeDescendants,
                RegistryPath,
                new[] { "_tagName" },
                "tagName",
                registry?.tags?
                    .Where(definition => definition?.IsValid() == true)
                    .Select(definition => definition.tagName)
                    ?? Enumerable.Empty<string>());
        }

        public static List<GameplayTagReference> FindAttribute(
            string attributeId,
            bool includeDescendants)
        {
            const string registryPath =
                "Assets/Resources/AttributeRegistry.asset";
            AttributeRegistrySO registry =
                AssetDatabase.LoadAssetAtPath<AttributeRegistrySO>(
                    registryPath);
            return Find(
                attributeId,
                includeDescendants,
                registryPath,
                new[] { "_attributeId", "attributeId" },
                "attributeId",
                registry?.attributes?
                    .Where(entry => entry?.IsValid() == true)
                    .Select(entry => entry.attributeId)
                    ?? Enumerable.Empty<string>());
        }

        private static List<GameplayTagReference> Find(
            string symbolName,
            bool includeDescendants,
            string registryPath,
            IReadOnlyList<string> serializedValueKeys,
            string registryDefinitionKey,
            IEnumerable<string> allNames)
        {
            var results = new List<GameplayTagReference>();
            if (string.IsNullOrWhiteSpace(symbolName))
                return results;

            HashSet<string> targets = BuildTargets(
                allNames,
                symbolName,
                includeDescendants);
            if (targets.Count == 0)
                targets.Add(symbolName);

            ScanSerializedFiles(
                targets,
                results,
                registryPath,
                serializedValueKeys,
                registryDefinitionKey);
            ScanCodeFiles(targets, results);
            results.Sort(CompareReferences);
            return results;
        }

        public static HashSet<string> BuildTargets(
            GameplayTagRegistrySO registry,
            string tagName,
            bool includeDescendants)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            if (registry?.tags == null || string.IsNullOrWhiteSpace(tagName))
                return result;

            string childPrefix = tagName + ".";
            for (int i = 0; i < registry.tags.Count; i++)
            {
                string candidate = registry.tags[i]?.tagName;
                if (string.Equals(candidate, tagName, StringComparison.Ordinal)
                    || (includeDescendants
                        && candidate?.StartsWith(
                            childPrefix,
                            StringComparison.Ordinal) == true))
                {
                    result.Add(candidate);
                }
            }
            return result;
        }

        private static HashSet<string> BuildTargets(
            IEnumerable<string> allNames,
            string symbolName,
            bool includeDescendants)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            if (allNames == null || string.IsNullOrWhiteSpace(symbolName))
                return result;
            string childPrefix = symbolName + ".";
            foreach (string candidate in allNames)
            {
                if (string.Equals(
                        candidate,
                        symbolName,
                        StringComparison.Ordinal)
                    || (includeDescendants
                        && candidate?.StartsWith(
                            childPrefix,
                            StringComparison.Ordinal) == true))
                    result.Add(candidate);
            }
            return result;
        }

        public static IEnumerable<string> EnumerateSerializedAssetPaths()
        {
            foreach (string root in SerializedRoots)
            {
                string absoluteRoot = ToAbsolutePath(root);
                if (!Directory.Exists(absoluteRoot))
                    continue;

                foreach (string absolutePath in Directory.EnumerateFiles(
                             absoluteRoot,
                             "*",
                             SearchOption.AllDirectories))
                {
                    if (SerializedExtensions.Contains(
                            Path.GetExtension(absolutePath)))
                    {
                        yield return ToAssetPath(absolutePath);
                    }
                }
            }
        }

        public static IEnumerable<string> EnumerateCodeAssetPaths()
        {
            foreach (string root in CodeRoots)
            {
                string absoluteRoot = ToAbsolutePath(root);
                if (!Directory.Exists(absoluteRoot))
                    continue;

                foreach (string absolutePath in Directory.EnumerateFiles(
                             absoluteRoot,
                             "*.cs",
                             SearchOption.AllDirectories))
                {
                    yield return ToAssetPath(absolutePath);
                }
            }
        }

        public static string ToAbsolutePath(string assetPath)
        {
            string projectRoot =
                Directory.GetParent(Application.dataPath)?.FullName
                ?? throw new InvalidOperationException(
                    "Unity 프로젝트 루트를 찾지 못했습니다.");
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath));
        }

        private static void ScanSerializedFiles(
            HashSet<string> targets,
            List<GameplayTagReference> results,
            string registryPath,
            IReadOnlyList<string> serializedValueKeys,
            string registryDefinitionKey)
        {
            var valuePatterns = new List<Regex>();
            for (int i = 0; i < serializedValueKeys.Count; i++)
            {
                valuePatterns.Add(new Regex(
                    $@"^\s*(?:-\s*)?{Regex.Escape(serializedValueKeys[i])}:\s*(?<tag>[^\r\n]*)\s*$",
                    RegexOptions.Compiled));
            }
            var definitionPattern = new Regex(
                $@"^\s*-\s+{Regex.Escape(registryDefinitionKey)}:\s*(?<tag>[^\r\n]*)\s*$",
                RegexOptions.Compiled);

            foreach (string assetPath in EnumerateSerializedAssetPaths())
            {
                string[] lines = File.ReadAllLines(ToAbsolutePath(assetPath));
                for (int i = 0; i < lines.Length; i++)
                {
                    if (string.Equals(
                            assetPath,
                            registryPath,
                            StringComparison.Ordinal))
                    {
                        Match definitionMatch =
                            definitionPattern.Match(lines[i]);
                        if (definitionMatch.Success)
                        {
                            string definition =
                                definitionMatch.Groups["tag"].Value.Trim();
                            if (targets.Contains(definition))
                            {
                                results.Add(new GameplayTagReference(
                                    definition,
                                    assetPath,
                                    i + 1,
                                    GameplayTagReferenceKind.Registry,
                                    lines[i].Trim()));
                            }
                            continue;
                        }
                    }

                    for (int patternIndex = 0;
                         patternIndex < valuePatterns.Count;
                         patternIndex++)
                    {
                        Match valueMatch =
                            valuePatterns[patternIndex].Match(lines[i]);
                        if (!valueMatch.Success)
                            continue;

                        string value =
                            valueMatch.Groups["tag"].Value.Trim();
                        if (targets.Contains(value))
                        {
                            results.Add(new GameplayTagReference(
                                value,
                                assetPath,
                                i + 1,
                                GameplayTagReferenceKind.SerializedData,
                                lines[i].Trim()));
                        }
                        break;
                    }
                }
            }
        }

        private static void ScanCodeFiles(
            HashSet<string> targets,
            List<GameplayTagReference> results)
        {
            if (targets.Count == 0)
                return;

            string alternatives = string.Join(
                "|",
                targets
                    .OrderByDescending(value => value.Length)
                    .Select(Regex.Escape));
            var pattern = new Regex(
                $"\"(?<tag>{alternatives})\"",
                RegexOptions.Compiled);

            foreach (string assetPath in EnumerateCodeAssetPaths())
            {
                string[] lines = File.ReadAllLines(ToAbsolutePath(assetPath));
                for (int i = 0; i < lines.Length; i++)
                {
                    MatchCollection matches = pattern.Matches(lines[i]);
                    foreach (Match match in matches)
                    {
                        results.Add(new GameplayTagReference(
                            match.Groups["tag"].Value,
                            assetPath,
                            i + 1,
                            GameplayTagReferenceKind.Code,
                            lines[i].Trim()));
                    }
                }
            }
        }

        private static string ToAssetPath(string absolutePath)
        {
            string normalized = absolutePath.Replace('\\', '/');
            string dataPath = Application.dataPath.Replace('\\', '/');
            return "Assets" + normalized.Substring(dataPath.Length);
        }

        private static int CompareReferences(
            GameplayTagReference left,
            GameplayTagReference right)
        {
            int path = string.Compare(
                left.AssetPath,
                right.AssetPath,
                StringComparison.Ordinal);
            return path != 0
                ? path
                : left.LineNumber.CompareTo(right.LineNumber);
        }
    }

    internal readonly struct GameplayTagRenameResult
    {
        public readonly int RenamedDefinitions;
        public readonly int ChangedFiles;
        public readonly int ReplacedReferences;

        public GameplayTagRenameResult(
            int renamedDefinitions,
            int changedFiles,
            int replacedReferences)
        {
            RenamedDefinitions = renamedDefinitions;
            ChangedFiles = changedFiles;
            ReplacedReferences = replacedReferences;
        }
    }

    internal static class GameplayTagRenameService
    {
        internal const string AttributeRegistryPath =
            "Assets/Resources/AttributeRegistry.asset";
        private static readonly Regex TagNamePattern = new(
            @"^[A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z0-9_]+)*$",
            RegexOptions.Compiled);

        public static GameplayTagRenameResult Rename(
            GameplayTagRegistrySO registry,
            string oldName,
            string newName,
            bool includeDescendants)
        {
            if (registry == null)
                throw new ArgumentNullException(nameof(registry));

            oldName = oldName?.Trim() ?? string.Empty;
            newName = newName?.Trim() ?? string.Empty;

            Dictionary<string, string> renameMap = BuildRenameMap(
                registry,
                oldName,
                newName,
                includeDescendants);

            List<GameplayTagReference> references =
                GameplayTagReferenceSearch.Find(
                    oldName,
                    includeDescendants);
            var targetPaths = new HashSet<string>(
                references
                    .Where(reference =>
                        reference.Kind
                        != GameplayTagReferenceKind.Registry)
                    .Select(reference => reference.AssetPath),
                StringComparer.Ordinal)
            {
                GameplayTagReferenceSearch.RegistryPath,
            };

            AssetDatabase.SaveAssets();
            Dictionary<string, byte[]> backups = targetPaths.ToDictionary(
                path => path,
                path => File.ReadAllBytes(
                    GameplayTagReferenceSearch.ToAbsolutePath(path)),
                StringComparer.Ordinal);

            int changedFiles = 0;
            int replacedReferences = 0;
            bool assetEditing = false;
            bool autoRefreshDisabled = false;
            try
            {
                AssetDatabase.DisallowAutoRefresh();
                autoRefreshDisabled = true;
                AssetDatabase.StartAssetEditing();
                assetEditing = true;

                foreach (string assetPath in targetPaths)
                {
                    if (string.Equals(
                            assetPath,
                            GameplayTagReferenceSearch.RegistryPath,
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    bool isCode = string.Equals(
                        Path.GetExtension(assetPath),
                        ".cs",
                        StringComparison.OrdinalIgnoreCase);
                    string absolutePath =
                        GameplayTagReferenceSearch.ToAbsolutePath(assetPath);
                    string original = ReadUtf8(absolutePath);
                    string changed = ReplaceReferences(
                        original,
                        renameMap,
                        isCode,
                        out int replacementCount);
                    if (replacementCount == 0)
                        continue;

                    WriteUtf8PreservingBom(
                        absolutePath,
                        changed,
                        HasUtf8Bom(backups[assetPath]));
                    changedFiles++;
                    replacedReferences += replacementCount;
                }

                for (int i = 0; i < registry.tags.Count; i++)
                {
                    GameplayTagDefinition definition = registry.tags[i];
                    if (definition != null
                        && renameMap.TryGetValue(
                            definition.tagName,
                            out string renamed))
                    {
                        definition.tagName = renamed;
                    }
                }

                registry.RebuildLookup();
                EditorUtility.SetDirty(registry);
                AssetDatabase.SaveAssetIfDirty(registry);
            }
            catch
            {
                foreach (KeyValuePair<string, byte[]> backup in backups)
                {
                    File.WriteAllBytes(
                        GameplayTagReferenceSearch.ToAbsolutePath(backup.Key),
                        backup.Value);
                }
                throw;
            }
            finally
            {
                if (assetEditing)
                    AssetDatabase.StopAssetEditing();
                if (autoRefreshDisabled)
                    AssetDatabase.AllowAutoRefresh();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }

            return new GameplayTagRenameResult(
                renameMap.Count,
                changedFiles,
                replacedReferences);
        }

        public static GameplayTagRenameResult RenameAttribute(
            AttributeRegistrySO registry,
            string oldName,
            string newName,
            bool includeDescendants)
        {
            if (registry == null)
                throw new ArgumentNullException(nameof(registry));
            Dictionary<string, string> renameMap =
                BuildAttributeRenameMap(
                    registry,
                    oldName,
                    newName,
                    includeDescendants);
            List<GameplayTagReference> references =
                GameplayTagReferenceSearch.FindAttribute(
                    oldName,
                    includeDescendants);
            var targetPaths = new HashSet<string>(
                references
                    .Where(reference =>
                        reference.Kind != GameplayTagReferenceKind.Registry)
                    .Select(reference => reference.AssetPath),
                StringComparer.Ordinal)
            {
                AttributeRegistryPath,
            };

            AssetDatabase.SaveAssets();
            Dictionary<string, byte[]> backups = targetPaths.ToDictionary(
                path => path,
                path => File.ReadAllBytes(
                    GameplayTagReferenceSearch.ToAbsolutePath(path)),
                StringComparer.Ordinal);
            int changedFiles = 0;
            int replacedReferences = 0;
            bool assetEditing = false;
            bool autoRefreshDisabled = false;
            try
            {
                AssetDatabase.DisallowAutoRefresh();
                autoRefreshDisabled = true;
                AssetDatabase.StartAssetEditing();
                assetEditing = true;
                foreach (string assetPath in targetPaths)
                {
                    if (string.Equals(
                            assetPath,
                            AttributeRegistryPath,
                            StringComparison.Ordinal))
                        continue;
                    bool isCode = string.Equals(
                        Path.GetExtension(assetPath),
                        ".cs",
                        StringComparison.OrdinalIgnoreCase);
                    string absolutePath =
                        GameplayTagReferenceSearch.ToAbsolutePath(assetPath);
                    string original = ReadUtf8(absolutePath);
                    string changed = ReplaceReferences(
                        original,
                        renameMap,
                        isCode,
                        out int replacementCount,
                        new[] { "_attributeId", "attributeId" });
                    if (replacementCount == 0) continue;
                    WriteUtf8PreservingBom(
                        absolutePath,
                        changed,
                        HasUtf8Bom(backups[assetPath]));
                    changedFiles++;
                    replacedReferences += replacementCount;
                }

                for (int i = 0; i < registry.attributes.Count; i++)
                {
                    AttributeRegistryEntry entry = registry.attributes[i];
                    if (entry == null) continue;
                    string previous = entry.attributeId;
                    if (renameMap.TryGetValue(
                            previous,
                            out string renamed))
                    {
                        entry.aliases ??= new List<string>();
                        if (!entry.aliases.Contains(previous))
                            entry.aliases.Add(previous);
                        entry.attributeId = renamed;
                    }
                    entry.minimumAttributeId = RenameReference(
                        entry.minimumAttributeId,
                        renameMap);
                    entry.maximumAttributeId = RenameReference(
                        entry.maximumAttributeId,
                        renameMap);
                    entry.dependentResourceId = RenameReference(
                        entry.dependentResourceId,
                        renameMap);
                }
                registry.RebuildLookup();
                EditorUtility.SetDirty(registry);
                AssetDatabase.SaveAssetIfDirty(registry);
            }
            catch
            {
                foreach (KeyValuePair<string, byte[]> backup in backups)
                    File.WriteAllBytes(
                        GameplayTagReferenceSearch.ToAbsolutePath(backup.Key),
                        backup.Value);
                throw;
            }
            finally
            {
                if (assetEditing) AssetDatabase.StopAssetEditing();
                if (autoRefreshDisabled) AssetDatabase.AllowAutoRefresh();
                AssetDatabase.Refresh(
                    ImportAssetOptions.ForceSynchronousImport);
            }
            return new GameplayTagRenameResult(
                renameMap.Count,
                changedFiles,
                replacedReferences);
        }

        public static Dictionary<string, string> BuildRenameMap(
            GameplayTagRegistrySO registry,
            string oldName,
            string newName,
            bool includeDescendants)
        {
            oldName = oldName?.Trim() ?? string.Empty;
            newName = newName?.Trim() ?? string.Empty;
            ValidateNewName(oldName, newName);

            HashSet<string> targets =
                GameplayTagReferenceSearch.BuildTargets(
                    registry,
                    oldName,
                    includeDescendants);
            if (!targets.Contains(oldName))
            {
                throw new InvalidOperationException(
                    $"Registry에 '{oldName}' 태그가 없습니다.");
            }

            var result = new Dictionary<string, string>(
                StringComparer.Ordinal);
            foreach (string target in targets)
            {
                string suffix = target.Length == oldName.Length
                    ? string.Empty
                    : target.Substring(oldName.Length);
                result.Add(target, newName + suffix);
            }
            ValidateCollisions(registry, result);
            return result;
        }

        public static Dictionary<string, string> BuildAttributeRenameMap(
            AttributeRegistrySO registry,
            string oldName,
            string newName,
            bool includeDescendants)
        {
            oldName = oldName?.Trim() ?? string.Empty;
            newName = newName?.Trim() ?? string.Empty;
            ValidateNewName(oldName, newName);
            var names = registry?.attributes?
                .Where(entry => entry?.IsValid() == true)
                .Select(entry => entry.attributeId)
                .ToArray()
                ?? Array.Empty<string>();
            var targets = new HashSet<string>(
                StringComparer.Ordinal);
            string prefix = oldName + ".";
            for (int i = 0; i < names.Length; i++)
            {
                if (string.Equals(
                        names[i],
                        oldName,
                        StringComparison.Ordinal)
                    || (includeDescendants
                        && names[i].StartsWith(
                            prefix,
                            StringComparison.Ordinal)))
                    targets.Add(names[i]);
            }
            if (!targets.Contains(oldName))
                throw new InvalidOperationException(
                    $"Registry에 '{oldName}' Attribute가 없습니다.");
            var result = new Dictionary<string, string>(
                StringComparer.Ordinal);
            foreach (string target in targets)
            {
                string suffix = target.Length == oldName.Length
                    ? string.Empty
                    : target.Substring(oldName.Length);
                result.Add(target, newName + suffix);
            }
            ValidateCollisions(names, result, "Attribute");
            return result;
        }

        private static void ValidateNewName(
            string oldName,
            string newName)
        {
            if (string.IsNullOrEmpty(oldName))
                throw new InvalidOperationException(
                    "변경할 기존 태그가 비어 있습니다.");
            if (string.IsNullOrEmpty(newName))
                throw new InvalidOperationException(
                    "새 태그 이름이 비어 있습니다.");
            if (string.Equals(oldName, newName, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "기존 이름과 새 이름이 같습니다.");
            if (!TagNamePattern.IsMatch(newName))
            {
                throw new InvalidOperationException(
                    "태그 이름은 영문/숫자/'_' 세그먼트를 '.'으로 "
                    + "구분해야 하며 첫 세그먼트는 영문 또는 '_'로 "
                    + "시작해야 합니다.");
            }
        }

        private static void ValidateCollisions(
            GameplayTagRegistrySO registry,
            Dictionary<string, string> renameMap)
        {
            var unaffected = new HashSet<string>(
                registry.tags
                    .Where(definition => definition != null)
                    .Select(definition => definition.tagName)
                    .Where(name => !renameMap.ContainsKey(name)),
                StringComparer.Ordinal);
            var mappedNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, string> pair in renameMap)
            {
                if (unaffected.Contains(pair.Value))
                {
                    throw new InvalidOperationException(
                        $"변경 결과가 기존 태그와 충돌합니다: "
                        + $"'{pair.Key}' → '{pair.Value}'");
                }
                if (!mappedNames.Add(pair.Value))
                {
                    throw new InvalidOperationException(
                        $"변경 결과끼리 중복됩니다: '{pair.Value}'");
                }
            }
        }

        private static void ValidateCollisions(
            IEnumerable<string> names,
            Dictionary<string, string> renameMap,
            string displayName)
        {
            var unaffected = new HashSet<string>(
                names.Where(name => !renameMap.ContainsKey(name)),
                StringComparer.Ordinal);
            var mappedNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, string> pair in renameMap)
            {
                if (unaffected.Contains(pair.Value))
                    throw new InvalidOperationException(
                        $"변경 결과가 기존 {displayName}와 충돌합니다: "
                        + $"'{pair.Key}' → '{pair.Value}'");
                if (!mappedNames.Add(pair.Value))
                    throw new InvalidOperationException(
                        $"변경 결과끼리 중복됩니다: '{pair.Value}'");
            }
        }

        private static string ReplaceReferences(
            string source,
            Dictionary<string, string> renameMap,
            bool isCode,
            out int replacementCount,
            IReadOnlyList<string> serializedValueKeys = null)
        {
            int count = 0;
            string result = source;
            serializedValueKeys ??= new[] { "_tagName" };
            foreach (KeyValuePair<string, string> pair in renameMap
                         .OrderByDescending(entry => entry.Key.Length))
            {
                Regex pattern = isCode
                    ? new Regex(
                        $"\"{Regex.Escape(pair.Key)}\"",
                        RegexOptions.Compiled)
                    : new Regex(
                        $@"(?m)^(?<prefix>\s*(?:-\s*)?(?:"
                        + string.Join(
                            "|",
                            serializedValueKeys.Select(Regex.Escape))
                        + @"):\s*)"
                        + Regex.Escape(pair.Key)
                        + @"(?<suffix>\s*)$",
                        RegexOptions.Compiled);
                result = pattern.Replace(
                    result,
                    match =>
                    {
                        count++;
                        return isCode
                            ? $"\"{pair.Value}\""
                            : match.Groups["prefix"].Value
                              + pair.Value
                              + match.Groups["suffix"].Value;
                    });
            }
            replacementCount = count;
            return result;
        }

        private static string RenameReference(
            string value,
            IReadOnlyDictionary<string, string> renameMap) =>
            !string.IsNullOrEmpty(value)
            && renameMap.TryGetValue(value, out string renamed)
                ? renamed
                : value;

        private static string ReadUtf8(string absolutePath)
        {
            byte[] bytes = File.ReadAllBytes(absolutePath);
            int offset = HasUtf8Bom(bytes) ? 3 : 0;
            return Encoding.UTF8.GetString(
                bytes,
                offset,
                bytes.Length - offset);
        }

        private static void WriteUtf8PreservingBom(
            string absolutePath,
            string contents,
            bool hasBom)
        {
            File.WriteAllText(
                absolutePath,
                contents,
                new UTF8Encoding(hasBom));
        }

        private static bool HasUtf8Bom(byte[] bytes) =>
            bytes.Length >= 3
            && bytes[0] == 0xEF
            && bytes[1] == 0xBB
            && bytes[2] == 0xBF;
    }

    public sealed class GameplayTagReferenceWindow : EditorWindow
    {
        private string[] _tagNames = Array.Empty<string>();
        private int _domainIndex;
        private int _selectedIndex;
        private bool _includeDescendants = true;
        private List<GameplayTagReference> _results = new();
        private Vector2 _scroll;

        [UPlayGround.EditorTools.UPlaygroundTool(
            "UPlayGround/게임플레이/게임플레이 태그/태그 사용처 검색",
            priority = 205)]
        public static void OpenFromMenu()
        {
            Open(string.Empty, 0);
        }

        [UPlayGround.EditorTools.UPlaygroundTool(
            "UPlayGround/게임플레이/Attribute/사용처 검색",
            priority = 223)]
        public static void OpenAttributeFromMenu()
        {
            Open(string.Empty, 1);
        }

        public static void Open(string tagName)
        {
            Open(tagName, 0);
        }

        private static void Open(string symbolName, int domainIndex)
        {
            GameplayTagReferenceWindow window =
                GetWindow<GameplayTagReferenceWindow>();
            window._domainIndex = domainIndex;
            window.titleContent = new GUIContent("심볼 사용처");
            window.minSize = new Vector2(760f, 420f);
            window.LoadRegistry(symbolName);
            window.Show();
        }

        private void OnEnable()
        {
            LoadRegistry(CurrentTag);
        }

        private string CurrentTag =>
            _selectedIndex >= 0 && _selectedIndex < _tagNames.Length
                ? _tagNames[_selectedIndex]
                : string.Empty;

        private void LoadRegistry(string preferredTag)
        {
            if (_domainIndex == 0)
            {
                GameplayTagRegistrySO registry =
                    AssetDatabase.LoadAssetAtPath<GameplayTagRegistrySO>(
                    GameplayTagReferenceSearch.RegistryPath);
                _tagNames = registry?.tags?
                    .Where(definition => definition?.IsValid() == true)
                    .Select(definition => definition.tagName)
                    .OrderBy(name => name, StringComparer.Ordinal)
                    .ToArray()
                    ?? Array.Empty<string>();
            }
            else
            {
                AttributeRegistrySO registry =
                    AssetDatabase.LoadAssetAtPath<AttributeRegistrySO>(
                        "Assets/Resources/AttributeRegistry.asset");
                _tagNames = registry?.attributes?
                    .Where(entry => entry?.IsValid() == true)
                    .Select(entry => entry.attributeId)
                    .OrderBy(name => name, StringComparer.Ordinal)
                    .ToArray()
                    ?? Array.Empty<string>();
            }
            _selectedIndex = Math.Max(
                0,
                Array.IndexOf(_tagNames, preferredTag));
            if (_tagNames.Length > 0)
                RefreshResults();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            int nextDomain = EditorGUILayout.Popup(
                _domainIndex,
                new[] { "GameplayTag", "Attribute" },
                GUILayout.Width(110f));
            if (nextDomain != _domainIndex)
            {
                _domainIndex = nextDomain;
                LoadRegistry(string.Empty);
            }
            using (new EditorGUI.DisabledScope(_tagNames.Length == 0))
            {
                int next = EditorGUILayout.Popup(
                    _selectedIndex,
                    _tagNames,
                    GUILayout.MinWidth(300f));
                if (next != _selectedIndex)
                {
                    _selectedIndex = next;
                    RefreshResults();
                }
            }
            bool nextInclude = GUILayout.Toggle(
                _includeDescendants,
                "하위 포함",
                EditorStyles.toolbarButton,
                GUILayout.Width(95f));
            if (nextInclude != _includeDescendants)
            {
                _includeDescendants = nextInclude;
                RefreshResults();
            }
            if (GUILayout.Button(
                    "새로고침",
                    EditorStyles.toolbarButton,
                    GUILayout.Width(65f)))
                RefreshResults();
            using (new EditorGUI.DisabledScope(_tagNames.Length == 0))
            {
                if (GUILayout.Button(
                        "이름 변경",
                        EditorStyles.toolbarButton,
                        GUILayout.Width(70f)))
                    GameplayTagRenameWindow.Open(
                        CurrentTag,
                        _domainIndex == 1);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField(
                $"사용처 {_results.Count}개",
                EditorStyles.boldLabel);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            for (int i = 0; i < _results.Count; i++)
                DrawReference(_results[i]);
            EditorGUILayout.EndScrollView();
        }

        private void RefreshResults()
        {
            _results = _domainIndex == 0
                ? GameplayTagReferenceSearch.Find(
                    CurrentTag,
                    _includeDescendants)
                : GameplayTagReferenceSearch.FindAttribute(
                    CurrentTag,
                    _includeDescendants);
            Repaint();
        }

        private static void DrawReference(GameplayTagReference reference)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                KindLabel(reference.Kind),
                GUILayout.Width(72f));
            EditorGUILayout.LabelField(
                reference.TagName,
                EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("열기", GUILayout.Width(46f)))
            {
                InternalEditorUtility.OpenFileAtLineExternal(
                    GameplayTagReferenceSearch.ToAbsolutePath(
                        reference.AssetPath),
                    reference.LineNumber);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField(
                $"{reference.AssetPath}:{reference.LineNumber}",
                EditorStyles.miniLabel);
            EditorGUILayout.SelectableLabel(
                reference.Preview,
                EditorStyles.textField,
                GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.EndVertical();
        }

        private static string KindLabel(
            GameplayTagReferenceKind kind) =>
            kind switch
            {
                GameplayTagReferenceKind.Registry => "정의",
                GameplayTagReferenceKind.SerializedData => "직렬화",
                GameplayTagReferenceKind.Code => "코드",
                _ => kind.ToString(),
            };
    }

    public sealed class GameplayTagRenameWindow : EditorWindow
    {
        private GameplayTagRegistrySO _registry;
        private AttributeRegistrySO _attributeRegistry;
        private bool _isAttribute;
        private string _oldName = string.Empty;
        private string _newName = string.Empty;
        private bool _includeDescendants = true;
        private List<GameplayTagReference> _references = new();
        private string _error = string.Empty;
        private Vector2 _scroll;

        public static void Open(string tagName)
        {
            Open(tagName, false);
        }

        public static void Open(string symbolName, bool isAttribute)
        {
            GameplayTagRenameWindow window =
                CreateInstance<GameplayTagRenameWindow>();
            window._isAttribute = isAttribute;
            window.titleContent = new GUIContent(
                isAttribute
                    ? "Attribute 이름 변경"
                    : "GameplayTag 이름 변경");
            window.minSize = new Vector2(620f, 440f);
            window._oldName = symbolName ?? string.Empty;
            window._newName = symbolName ?? string.Empty;
            window.Load();
            window.ShowUtility();
        }

        private void Load()
        {
            if (_isAttribute)
            {
                _attributeRegistry =
                    AssetDatabase.LoadAssetAtPath<AttributeRegistrySO>(
                        GameplayTagRenameService.AttributeRegistryPath);
            }
            else
            {
                _registry =
                    AssetDatabase.LoadAssetAtPath<GameplayTagRegistrySO>(
                        GameplayTagReferenceSearch.RegistryPath);
            }
            RefreshPreview();
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Registry 정의와 직렬화된 SO·프리팹·씬, 정확히 일치하는 "
                + "C# 문자열을 함께 변경합니다. 실패하면 변경 파일을 "
                + "원본 바이트로 복구합니다. C# 문자열의 의미까지는 "
                + "판별하지 않으므로 아래 코드 내용을 반드시 확인하세요."
                + (_isAttribute
                    ? " 기존 이름은 aliases에 보존됩니다."
                    : string.Empty),
                MessageType.Warning);

            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.TextField(
                    _isAttribute ? "기존 Attribute" : "기존 태그",
                    _oldName);

            EditorGUI.BeginChangeCheck();
            _newName = EditorGUILayout.TextField(
                _isAttribute ? "새 Attribute" : "새 태그",
                _newName);
            _includeDescendants = EditorGUILayout.ToggleLeft(
                "하위 심볼도 같은 접두사로 함께 변경",
                _includeDescendants);
            if (EditorGUI.EndChangeCheck())
                RefreshPreview();

            if (!string.IsNullOrEmpty(_error))
            {
                EditorGUILayout.HelpBox(_error, MessageType.Error);
            }
            else
            {
                int codeReferenceCount = _references.Count(
                    reference =>
                        reference.Kind == GameplayTagReferenceKind.Code);
                EditorGUILayout.LabelField(
                    $"변경 대상 사용처 {_references.Count}개",
                    EditorStyles.boldLabel);
                if (codeReferenceCount > 0)
                {
                    EditorGUILayout.HelpBox(
                        $"C# 동일 문자열 {codeReferenceCount}개를 함께 변경합니다. "
                        + "태그/Attribute와 무관한 문자열이 없는지 확인하세요.",
                        MessageType.Warning);
                }
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            for (int i = 0; i < _references.Count; i++)
            {
                GameplayTagReference reference = _references[i];
                EditorGUILayout.LabelField(
                    $"[{GetReferenceKindLabel(reference.Kind)}] "
                    + $"{reference.TagName}  ·  "
                    + $"{reference.AssetPath}:{reference.LineNumber}",
                    EditorStyles.miniLabel);
                EditorGUILayout.SelectableLabel(
                    reference.Preview,
                    EditorStyles.textField,
                    GUILayout.Height(EditorGUIUtility.singleLineHeight));
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("취소", GUILayout.Width(72f)))
                Close();
            using (new EditorGUI.DisabledScope(
                       !string.IsNullOrEmpty(_error)
                       || string.Equals(
                           _oldName,
                           _newName,
                           StringComparison.Ordinal)))
            {
                if (GUILayout.Button("일괄 변경", GUILayout.Width(110f)))
                    ExecuteRename();
            }
            EditorGUILayout.EndHorizontal();
        }

        private static string GetReferenceKindLabel(
            GameplayTagReferenceKind kind) =>
            kind switch
            {
                GameplayTagReferenceKind.Registry => "정의",
                GameplayTagReferenceKind.SerializedData => "직렬화",
                GameplayTagReferenceKind.Code => "코드",
                _ => kind.ToString(),
            };

        private void RefreshPreview()
        {
            _error = string.Empty;
            _references = _isAttribute
                ? GameplayTagReferenceSearch.FindAttribute(
                    _oldName,
                    _includeDescendants)
                : GameplayTagReferenceSearch.Find(
                    _oldName,
                    _includeDescendants);
            try
            {
                Dictionary<string, string> map =
                    _isAttribute
                        ? GameplayTagRenameService.BuildAttributeRenameMap(
                            _attributeRegistry,
                            _oldName,
                            _newName.Trim(),
                            _includeDescendants)
                        : GameplayTagRenameService.BuildRenameMap(
                            _registry,
                            _oldName,
                            _newName.Trim(),
                            _includeDescendants);
                if (map.Values.Distinct(StringComparer.Ordinal).Count()
                    != map.Count)
                {
                    _error = "변경 결과에 중복 심볼이 생깁니다.";
                }
            }
            catch (Exception exception)
            {
                _error = exception.Message;
            }
        }

        private void ExecuteRename()
        {
            int codeReferenceCount = _references.Count(
                reference =>
                    reference.Kind == GameplayTagReferenceKind.Code);
            if (!EditorUtility.DisplayDialog(
                    _isAttribute
                        ? "Attribute 이름 변경"
                        : "GameplayTag 이름 변경",
                    $"'{_oldName}'을(를) '{_newName.Trim()}'(으)로 "
                    + "변경하시겠습니까?\n\n"
                    + $"하위 태그 포함: {_includeDescendants}\n"
                    + $"발견된 사용처: {_references.Count}개\n"
                    + $"C# 동일 문자열: {codeReferenceCount}개\n\n"
                    + "C# 문자열은 의미를 판별하지 않고 변경합니다. "
                    + "미리보기 내용을 확인했습니까?",
                    "변경",
                    "취소"))
            {
                return;
            }

            try
            {
                GameplayTagRenameResult result =
                    _isAttribute
                        ? GameplayTagRenameService.RenameAttribute(
                            _attributeRegistry,
                            _oldName,
                            _newName,
                            _includeDescendants)
                        : GameplayTagRenameService.Rename(
                            _registry,
                            _oldName,
                            _newName,
                            _includeDescendants);
                Debug.Log(
                    $"[{(_isAttribute ? "Attribute" : "GameplayTag")}] "
                    + $"이름 변경 완료: '{_oldName}' → "
                    + $"'{_newName.Trim()}', 정의 {result.RenamedDefinitions}개, "
                    + $"파일 {result.ChangedFiles}개, "
                    + $"참조 {result.ReplacedReferences}개");
                Close();
            }
            catch (Exception exception)
            {
                _error = exception.Message;
                Debug.LogException(exception);
            }
        }
    }
}
