using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace UPlayGround.Editor.P09Builder
{
    /// <summary>검수한 비활성 메시 잎만 정규 경로에서 제거하고 실패 시 원문/meta를 복구한다.</summary>
    public static class NpcAppearanceCompactionTransaction
    {
        [Serializable]
        public sealed class Plan
        {
            public string reportFolder;
            public Entry[] entries;
        }

        [Serializable]
        public sealed class Entry
        {
            public string path, guid, sourceHash, metaHash, identityHash, serializedHash;
            public string visuals, skeleton, references;
            public string[] removedPaths, removedReferenceKeys;
        }

        [Serializable]
        private sealed class ExternalScan
        {
            public string planHash;
            public bool passed;
            public string[] hits;
            public string[] binaryAssets;
            public string[] skippedLinks;
            public int filesScanned;
            public long bytesScanned;
            public bool binaryDependenciesChecked;
            public string[] excludedNonImportedSamples;
        }

        public sealed class InjectedFailureException : Exception { }

        /// <summary>쓰기 대상을 고정하고 외부 참조 검사용 GUID/fileID를 문자열로 저장한다.</summary>
        public static string Prepare(string[] paths, string reportFolder)
        {
            NpcAppearanceImprovement.RequireEditMode();
            RequireReportPath(reportFolder);
            if (paths == null || paths.Length == 0 || paths.Distinct().Count() != paths.Length)
                throw new ArgumentException("입력이 비어 있거나 중복됩니다.");
            if (Directory.Exists(reportFolder)) throw new IOException("기존 계획은 덮어쓰지 않습니다.");
            var entries = new List<Entry>();
            foreach (string path in paths)
            {
                RequireSourcePath(path);
                var source = RequireSource(path);
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    string[] removedPaths = FindPaths(root, source);
                    if (removedPaths.Length == 0) throw new InvalidOperationException("제거 후보가 없습니다: " + path);
                    var removed = new HashSet<string>(removedPaths, StringComparer.Ordinal);
                    var retained = new HashSet<Component>(root.GetComponentsInChildren<Component>(true)
                        .Where(c => !removed.Contains(NodePath(c.transform, root))));
                    string guid = AssetDatabase.AssetPathToGUID(path);
                    var keys = AllObjects(source).Where(o => removed.Contains(ObjectPath(o, source)))
                        .Select(o => ReferenceKey(o)).ToArray();
                    if (keys.Any(k => !k.StartsWith(guid + ":", StringComparison.Ordinal)))
                        throw new InvalidOperationException("다른 prefab에 소속된 제거 후보가 있습니다.");
                    entries.Add(new Entry
                    {
                        path = path, guid = guid, sourceHash = HashFile(path), metaHash = HashFile(path + ".meta"),
                        removedPaths = removedPaths, removedReferenceKeys = keys,
                        identityHash = HashLines(IdentityLines(source, removed)),
                        serializedHash = HashLines(SerializedLines(source, removed)),
                        visuals = NpcAppearancePrototype.GetVisuals(root), skeleton = NpcAppearancePrototype.GetSkeleton(root),
                        references = NpcAppearancePrototype.GetReferenceFingerprint(root, retained)
                    });
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            Directory.CreateDirectory(reportFolder);
            string planPath = reportFolder + "/plan.json";
            File.WriteAllText(planPath, JsonUtility.ToJson(new Plan { reportFolder = reportFolder, entries = entries.ToArray() }, true));
            return planPath;
        }

        /// <summary>텍스트 검사가 읽지 못한 이진 Unity 자산은 실제 의존성으로 추가 검사한다.</summary>
        public static void ValidateExternalScan(string planPath)
        {
            NpcAppearanceImprovement.RequireEditMode();
            var plan = JsonUtility.FromJson<Plan>(File.ReadAllText(planPath));
            RequireReportPath(plan.reportFolder);
            string resultPath = plan.reportFolder + "/external-references.json";
            var scan = JsonUtility.FromJson<ExternalScan>(File.ReadAllText(resultPath));
            if (scan.planHash != HashFile(planPath) || scan.hits == null || scan.hits.Length > 0
                || scan.skippedLinks == null || scan.skippedLinks.Length > 0 || scan.binaryAssets == null)
                throw new InvalidOperationException("텍스트 참조 검사에 누락/외부 참조/계획 불일치가 있습니다.");
            var targets = new HashSet<string>(plan.entries.Select(e => e.path), StringComparer.Ordinal);
            var imported = new HashSet<string>(AssetDatabase.GetAllAssetPaths(), StringComparer.Ordinal);
            var excluded = new List<string>();
            foreach (string path in scan.binaryAssets)
            {
                // Packages의 미수입 Samples~는 현재 에셋 참조 그래프에 속하지 않는다.
                string[] parts = path.Split('/');
                int ignoredFolder = Array.FindIndex(parts, p => p.EndsWith("~", StringComparison.Ordinal));
                if (path.StartsWith("Packages/", StringComparison.Ordinal) && ignoredFolder > 1
                    && !imported.Contains(path) && string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path))
                    && !AssetDatabase.IsValidFolder(string.Join("/", parts.Take(ignoredFolder + 1))))
                {
                    excluded.Add(path);
                    continue;
                }
                if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path)) || AssetDatabase.GetDependencies(path, true).Any(targets.Contains))
                    throw new InvalidOperationException("이진 자산의 정규 NPC 참조를 추가 해석해야 합니다: " + path);
            }
            scan.binaryDependenciesChecked = true;
            scan.excludedNonImportedSamples = excluded.ToArray();
            scan.passed = true;
            File.WriteAllText(resultPath, JsonUtility.ToJson(scan, true));
        }

        /// <summary>외부 참조 검사 통과 계획만 적용한다. 실패 주입은 같은 복구 경로를 검증한다.</summary>
        public static void Apply(string planPath, int failAfterSaved = 0)
        {
            NpcAppearanceImprovement.RequireEditMode();
            var plan = JsonUtility.FromJson<Plan>(File.ReadAllText(planPath));
            RequireReportPath(plan.reportFolder);
            if (planPath != plan.reportFolder + "/plan.json" || plan.entries == null || plan.entries.Length == 0)
                throw new InvalidOperationException("계획 경로/입력이 잘못되었습니다.");
            var scan = JsonUtility.FromJson<ExternalScan>(File.ReadAllText(plan.reportFolder + "/external-references.json"));
            if (!scan.passed || !scan.binaryDependenciesChecked || scan.hits == null || scan.hits.Length != 0 || scan.planHash != HashFile(planPath))
                throw new InvalidOperationException("현재 계획의 외부 참조 검사가 통과하지 않았습니다.");
            foreach (Entry entry in plan.entries) ValidateInput(entry);
            string backup = plan.reportFolder + "/originals.zip";
            if (!File.Exists(backup)) Backup(plan, backup);
            ValidateBackup(plan, backup);
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("고정 NPC 비활성 메시 정리");
            var written = new List<Entry>();
            try
            {
                foreach (Entry entry in plan.entries)
                {
                    ValidateInput(entry);
                    var source = RequireSource(entry.path);
                    var root = PrefabUtility.LoadPrefabContents(entry.path);
                    try
                    {
                        if (!FindPaths(root, source).SequenceEqual(entry.removedPaths))
                            throw new InvalidOperationException("제거 후보가 계획과 다릅니다: " + entry.path);
                        Undo.RegisterFullObjectHierarchyUndo(root, "고정 NPC 비활성 메시 정리");
                        foreach (string path in entry.removedPaths)
                            Undo.DestroyObjectImmediate(root.transform.Find(path).gameObject);
                        ValidateContents(root, entry);
                        written.Add(entry);
                        PrefabUtility.SaveAsPrefabAsset(root, entry.path, out bool success);
                        if (!success) throw new IOException("Prefab 저장 실패: " + entry.path);
                    }
                    finally { PrefabUtility.UnloadPrefabContents(root); }
                    AssetDatabase.ImportAsset(entry.path, ImportAssetOptions.ForceUpdate);
                    ValidateSaved(entry);
                    if (failAfterSaved > 0 && written.Count == failAfterSaved) throw new InjectedFailureException();
                }
                Undo.CollapseUndoOperations(group);
                File.WriteAllText(plan.reportFolder + "/applied.json", JsonUtility.ToJson(plan, true));
            }
            catch
            {
                try { Undo.RevertAllDownToGroup(group); }
                finally { Restore(written, backup); }
                foreach (Entry entry in plan.entries) ValidateInput(entry);
                File.WriteAllText(plan.reportFolder + "/rollback-verified.txt", "저장된 모든 원문/meta 해시 복구. GUID와 입력 상태 확인.\n");
                throw;
            }
        }

        private static void RequireReportPath(string path)
        {
            if (string.IsNullOrEmpty(path) || !path.StartsWith("Logs/StartupProfiling/NpcCompaction/", StringComparison.Ordinal) || path.Contains(".."))
                throw new ArgumentException("계획/백업 폴더의 범위를 벗어났습니다.");
        }

        private static void RequireSourcePath(string path)
        {
            if (string.IsNullOrEmpty(path) || !path.StartsWith(NpcAppearanceGenerator.PrefabRoot + "/Npc/", StringComparison.Ordinal)
                || path.Contains("..") || !path.EndsWith(".prefab", StringComparison.Ordinal))
                throw new ArgumentException("정규 고정 NPC 경로가 아닙니다.");
        }

        private static GameObject RequireSource(string path)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            NpcAppearanceImprovement.RequireClean(source);
            if (source.GetComponent<NpcActor>() == null || PrefabUtility.GetPrefabAssetType(source) != PrefabAssetType.Regular)
                throw new InvalidOperationException("일반 NPC prefab만 지원합니다: " + path);
            foreach (Component component in source.GetComponentsInChildren<Component>(true)) NpcAppearanceImprovement.RequireClean(component);
            return source;
        }

        private static string[] FindPaths(GameObject root, GameObject source)
        {
            string[] paths = root.GetComponentsInChildren<Transform>(true).Select(t => NodePath(t, root)).ToArray();
            if (paths.Distinct(StringComparer.Ordinal).Count() != paths.Length)
                throw new InvalidOperationException("동일 계층 경로가 중복되어 정확한 잎을 식별할 수 없습니다.");
            return NpcAppearancePrototype.FindUnusedMeshLeaves(root,
                EditorUtility.CollectDependencies(new UnityObject[] { source }).OfType<AnimationClip>())
                .Select(t => NodePath(t, root)).OrderBy(p => p, StringComparer.Ordinal).ToArray();
        }

        private static void ValidateInput(Entry entry)
        {
            RequireSourcePath(entry.path);
            RequireSource(entry.path);
            if (AssetDatabase.AssetPathToGUID(entry.path) != entry.guid || HashFile(entry.path) != entry.sourceHash || HashFile(entry.path + ".meta") != entry.metaHash)
                throw new InvalidOperationException("원본/GUID/meta가 계획 이후 변경됐습니다: " + entry.path);
        }

        private static void ValidateContents(GameObject root, Entry entry)
        {
            if (root.GetComponentsInChildren<Component>(true).Any(c => c == null)
                || NpcAppearancePrototype.GetVisuals(root) != entry.visuals || NpcAppearancePrototype.GetSkeleton(root) != entry.skeleton
                || NpcAppearancePrototype.GetReferenceFingerprint(root, null) != entry.references)
                throw new InvalidOperationException("활성 외형/골격/유지 참조 변경: " + entry.path);
        }

        private static void ValidateSaved(Entry entry)
        {
            var source = RequireSource(entry.path);
            var root = PrefabUtility.LoadPrefabContents(entry.path);
            try { ValidateContents(root, entry); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            if (AssetDatabase.AssetPathToGUID(entry.path) != entry.guid || HashFile(entry.path + ".meta") != entry.metaHash
                || HashLines(IdentityLines(source, new HashSet<string>())) != entry.identityHash
                || HashLines(SerializedLines(source, new HashSet<string>())) != entry.serializedHash)
                throw new InvalidOperationException("유지 local fileID/직렬화 필드/GUID가 바뀌었습니다: " + entry.path);
        }

        private static IEnumerable<UnityObject> AllObjects(GameObject root) => root.GetComponentsInChildren<Component>(true).Cast<UnityObject>()
            .Concat(root.GetComponentsInChildren<Transform>(true).Select(t => (UnityObject)t.gameObject));

        private static string NodePath(Transform transform, GameObject root) => AnimationUtility.CalculateTransformPath(transform, root.transform);
        private static string ObjectPath(UnityObject value, GameObject root) => NodePath(value is GameObject go ? go.transform : ((Component)value).transform, root);
        private static string ObjectKey(UnityObject value, GameObject root)
        {
            int ordinal = value is Component component ? Array.IndexOf(component.GetComponents(component.GetType()), component) : 0;
            return ObjectPath(value, root) + "|" + value.GetType().FullName + "|" + ordinal;
        }

        private static string ReferenceKey(UnityObject value)
        {
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value, out string guid, out long id)) throw new InvalidOperationException("persistent ID 누락");
            return guid + ":" + id;
        }

        private static IEnumerable<string> IdentityLines(GameObject root, HashSet<string> removed) => AllObjects(root)
            .Where(o => !removed.Contains(ObjectPath(o, root))).Select(o => ObjectKey(o, root) + "=" + ReferenceKey(o));

        private static IEnumerable<string> SerializedLines(GameObject root, HashSet<string> removed) => AllObjects(root)
            .Where(o => !removed.Contains(ObjectPath(o, root))).Select(o => ObjectKey(o, root) + "=" + (o is Transform t
                ? t.localPosition.ToString("R") + t.localRotation.ToString("R") + t.localScale.ToString("R") + "|" + (t.parent == null ? "" : NodePath(t.parent, root))
                : EditorJsonUtility.ToJson(o)));

        private static void Backup(Plan plan, string path)
        {
            using var file = new FileStream(path, FileMode.CreateNew);
            using var zip = new ZipArchive(file, ZipArchiveMode.Create);
            foreach (Entry entry in plan.entries)
                foreach (string source in new[] { entry.path, entry.path + ".meta" })
                {
                    using var output = zip.CreateEntry(source, System.IO.Compression.CompressionLevel.Optimal).Open();
                    byte[] bytes = File.ReadAllBytes(source);
                    output.Write(bytes, 0, bytes.Length);
                }
        }

        private static void ValidateBackup(Plan plan, string path)
        {
            using var zip = ZipFile.OpenRead(path);
            foreach (Entry entry in plan.entries)
                foreach (string source in new[] { entry.path, entry.path + ".meta" })
                {
                    using var input = zip.GetEntry(source)?.Open() ?? throw new IOException("백업 누락: " + source);
                    using var memory = new MemoryStream(); input.CopyTo(memory);
                    if (HashBytes(memory.ToArray()) != (source == entry.path ? entry.sourceHash : entry.metaHash)) throw new IOException("백업 해시 불일치");
                }
        }

        private static void Restore(IEnumerable<Entry> entries, string path)
        {
            using var zip = ZipFile.OpenRead(path);
            foreach (Entry entry in entries)
                foreach (string target in new[] { entry.path, entry.path + ".meta" })
                {
                    using var input = zip.GetEntry(target)?.Open() ?? throw new IOException("백업 누락: " + target);
                    using var output = new FileStream(target, FileMode.Create, FileAccess.Write); input.CopyTo(output);
                }
            foreach (Entry entry in entries) AssetDatabase.ImportAsset(entry.path, ImportAssetOptions.ForceUpdate);
        }

        private static string HashFile(string path) => HashBytes(File.ReadAllBytes(path));
        private static string HashLines(IEnumerable<string> lines) => HashBytes(Encoding.UTF8.GetBytes(string.Join("\n", lines.OrderBy(s => s, StringComparer.Ordinal))));
        private static string HashBytes(byte[] bytes)
        {
            using var hash = SHA256.Create();
            return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "");
        }
    }
}
