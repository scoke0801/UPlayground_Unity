using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UPlayGround.WorldBuilder.Core;
using UPlayGround.WorldBuilder.Data;
using UPlayGround.WorldBuilder.Runtime;

namespace UPlayGround.WorldBuilder.Editor
{
    public enum WorldBuildState { Staging, Validated, Cancelled, Failed }

    [Serializable]
    public sealed class WorldBuildReceipt
    {
        public string worldGuid, buildId, sourceRevision, inputHash, layoutGuid, catalogGuid, lastStep, failure;
        public int bakeFormat;
        public bool chunked;
        public WorldBuildState state;
        public WorldBakeReport report;
        public WorldFileDigest[] files;
    }

    [Serializable]
    public sealed class WorldFileDigest
    {
        public string path, sha256;
    }

    [Serializable]
    internal sealed class WorldPublishTransition
    {
        public WorldPublishedPointer previous, next;
    }

    [Serializable]
    internal sealed class WorldRevisionRetention
    {
        public string[] builds;
    }

    /// <summary>검증된 불변 revision만 공개 포인터로 전환한다.</summary>
    public static class WorldBakePipeline
    {
        private const int BakeFormat = 1;
        public static string WorldRoot(string world) { PlacementIdentity.ValidateGuid(world); return "Assets/WorldBuilder/Worlds/" + world; }
        public static string Revision(string world, string build) { PlacementIdentity.ValidateGuid(build); return WorldRoot(world) + "/BakeRevisions/" + build; }
        public static string PublishedPath(string world) => WorldRoot(world) + "/Published/Manifest.json";
        private static string ReceiptPath(string world, string build) => Revision(world, build) + "/Build.json";

        public static WorldBuildReceipt Bake(WorldDefinition definition, WorldNavigationLayout layout, Action<string> checkpoint = null, bool chunked = false)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            string world = definition.WorldGuid;
            using var lease = Acquire(world);
            var report = WorldBakeValidation.Validate(definition, layout);
            if (report.HasErrors)
                throw new InvalidDataException("Bake 입력 검사 실패 · " + string.Join(" / ", report.issues.Select(i => i.code + " " + i.message)));
            string build = Guid.NewGuid().ToString("N");
            string folder = Revision(world, build);
            var receipt = new WorldBuildReceipt { worldGuid = world, buildId = build, sourceRevision = definition.SourceRevision,
                state = WorldBuildState.Staging, bakeFormat = BakeFormat, chunked = chunked,
                inputHash = InputHash(definition, layout), layoutGuid = GuidOf(layout), report = report };
            WorldBuilderAuthoringAssets.EnsureFolder(folder);
            WriteReceipt(receipt);
            try
            {
                Step("입력 고정");
                var catalog = WorldBakeStage.Save(definition, layout, folder, build, Step);
                receipt.catalogGuid = GuidOf(catalog);
                Step("저장 씬 재개방·참조 검증");
                if (chunked)
                {
                    WorldChunkBake.Stage(definition, catalog, () => ValidateCatalog(catalog, world, build, reopen: true), Step);
                    WorldChunkBake.Validate(catalog, reopen: true);
                }
                else
                    ValidateCatalog(catalog, world, build, reopen: true);
                receipt.report.issues.RemoveAll(i => i.code == "WB501");
                receipt.report.Add("WB503", $"저장 씬에서 필수 AI 경로 {catalog.routes.Length}개와 런타임 참조를 확인했습니다.",
                    world, "KCC 횡단 결과는 별도 플레이 검사에서 확인합니다.", severity: BakeSeverity.Info);
                Step("revision 지문");
                receipt.files = Files(folder);
                EnsureInput(definition, layout, receipt);
                receipt.state = WorldBuildState.Validated;
                WriteReceipt(receipt);
                return receipt;
            }
            catch (Exception error)
            {
                receipt.state = error is OperationCanceledException ? WorldBuildState.Cancelled : WorldBuildState.Failed;
                receipt.failure = error.Message;
                receipt.report.Add("WB700", error.Message, world, "저장 단계와 입력을 확인한 뒤 새 revision으로 재시도합니다.");
                WriteReceipt(receipt);
                throw;
            }

            void Step(string name)
            {
                receipt.lastStep = name;
                checkpoint?.Invoke(name);
            }
        }

        public static WorldPublishedPointer Publish(WorldDefinition definition, string buildId, string warningApprover = null,
            string warningReason = null, Action<string> checkpoint = null)
        {
            string world = definition.WorldGuid;
            using var lease = Acquire(world);
            var receipt = LoadReceipt(world, buildId);
            if (receipt.bakeFormat != BakeFormat) throw new InvalidDataException("이전 Bake 형식입니다. 현재 검증기로 새 revision을 Bake하세요.");
            if (receipt.report == null || receipt.report.HasErrors) throw new InvalidDataException("검증 오류가 있는 revision은 발행할 수 없습니다.");
            bool approve = receipt.report.issues.Any(issue => issue.severity == BakeSeverity.Warning) &&
                !string.IsNullOrWhiteSpace(warningApprover) && !string.IsNullOrWhiteSpace(warningReason) &&
                (receipt.report.warningApprover != warningApprover.Trim() || receipt.report.warningReason != warningReason.Trim());
            if (approve)
            {
                receipt.report.warningApprover = warningApprover.Trim();
                receipt.report.warningReason = warningReason.Trim();
            }
            if (!receipt.report.CanPublish) throw new InvalidDataException("Warning 승인자와 사유가 필요합니다.");
            var layout = AssetDatabase.LoadAssetAtPath<WorldNavigationLayout>(AssetDatabase.GUIDToAssetPath(receipt.layoutGuid));
            var candidate = ReadRevision(world, buildId, reopen: true, requireApprovedWarnings: false);
            var old = ReadPublished(world);
            if (old?.current == buildId) return old;
            if (old != null)
            {
                var current = ReadRevision(world, old.current, reopen: false);
                if (GuidOf(current) != old.catalogGuid) throw new InvalidDataException("현재 Published의 catalog GUID가 다릅니다.");
            }
            if (receipt.chunked) WorldChunkBake.RegisterAddressables(candidate);
            if (approve) WriteReceipt(receipt);
            var next = new WorldPublishedPointer { worldGuid = world, current = buildId, previous = old?.current,
                catalogGuid = receipt.catalogGuid, previousCatalogGuid = old?.catalogGuid };
            CommitPointer(world, old, next, step =>
            {
                checkpoint?.Invoke(step);
                if (step == "BeforePointerReplace") EnsureInput(definition, layout, receipt);
            });
            return next;
        }

        public static WorldPublishedPointer Rollback(string world, Action<string> checkpoint = null)
        {
            using var lease = Acquire(world);
            var old = ReadPublished(world);
            if (old == null || string.IsNullOrEmpty(old.previous)) throw new InvalidOperationException("이전 Published revision이 없습니다.");
            if (LoadReceipt(world, old.previous).bakeFormat != BakeFormat)
                throw new InvalidDataException("이전 Published가 현재 Bake 형식과 다릅니다.");
            var catalog = ReadRevision(world, old.previous, reopen: true);
            if (LoadReceipt(world, old.previous).chunked) WorldChunkBake.RegisterAddressables(catalog);
            if (GuidOf(catalog) != old.previousCatalogGuid) throw new InvalidDataException("이전 Published의 catalog GUID가 다릅니다.");
            var next = new WorldPublishedPointer { worldGuid = world, current = old.previous, previous = old.current,
                catalogGuid = old.previousCatalogGuid, previousCatalogGuid = old.catalogGuid };
            CommitPointer(world, old, next, checkpoint);
            return next;
        }

        public static WorldPublishedPointer ReadPublished(string world)
        {
            string path = PublishedPath(world);
            if (!File.Exists(path)) return null;
            var pointer = Read<WorldPublishedPointer>(path);
            if (pointer.worldGuid != world || pointer.previous == pointer.current) throw new InvalidDataException("Published 소유 월드 또는 revision 오류.");
            PlacementIdentity.ValidateGuid(pointer.current);
            PlacementIdentity.ValidateGuid(pointer.catalogGuid);
            if (!string.IsNullOrEmpty(pointer.previous))
            {
                PlacementIdentity.ValidateGuid(pointer.previous);
                PlacementIdentity.ValidateGuid(pointer.previousCatalogGuid);
            }
            return pointer;
        }

        public static WorldBuildReceipt LoadReceipt(string world, string build)
        {
            var receipt = Read<WorldBuildReceipt>(ReceiptPath(world, build));
            if (receipt.worldGuid != world || receipt.buildId != build) throw new InvalidDataException("revision 소유권이 다릅니다.");
            return receipt;
        }

        public static WorldBakeCatalog ReadRevision(string world, string build, bool reopen, bool requireApprovedWarnings = true)
        {
            var receipt = LoadReceipt(world, build);
            if (receipt.state != WorldBuildState.Validated || receipt.report == null || receipt.report.HasErrors ||
                (requireApprovedWarnings && !receipt.report.CanPublish) || receipt.files == null || receipt.files.Length == 0)
                throw new InvalidDataException("무결성 검증이 끝난 revision만 사용할 수 있습니다.");
            var actual = Files(Revision(world, build));
            if (actual.Length != receipt.files.Length || actual.Where((file, i) =>
                file.path != receipt.files[i].path || file.sha256 != receipt.files[i].sha256).Any())
                throw new InvalidDataException("revision 파일 목록이나 지문이 달라졌습니다 · " + build);
            string catalogPath = Revision(world, build) + "/Catalog.asset";
            if (AssetDatabase.GUIDToAssetPath(receipt.catalogGuid) != catalogPath)
                throw new InvalidDataException("revision catalog GUID가 다른 경로를 가리킵니다.");
            var catalog = AssetDatabase.LoadAssetAtPath<WorldBakeCatalog>(catalogPath);
            ValidateCatalog(catalog, world, build, reopen);
            if (receipt.chunked) WorldChunkBake.Validate(catalog, reopen);
            return catalog;
        }

        public static IReadOnlyList<string> BuildIds(string world)
        {
            string folder = WorldRoot(world) + "/BakeRevisions";
            return Directory.Exists(folder)
                ? Directory.GetDirectories(folder).Select(Path.GetFileName).Where(id => Guid.TryParseExact(id, "N", out _))
                    .OrderByDescending(id => Directory.GetCreationTimeUtc(folder + "/" + id)).ToArray()
                : Array.Empty<string>();
        }

        public static bool IsRetained(string world, string build) => Retention(world).Contains(build);

        public static void SetRetained(string world, string build, bool retained)
        {
            using var lease = Acquire(world);
            _ = LoadReceipt(world, build);
            var builds = Retention(world).ToHashSet(StringComparer.Ordinal);
            if (retained) builds.Add(build); else builds.Remove(build);
            string path = RetentionPath(world);
            WorldBuilderAuthoringAssets.EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            AtomicWrite(path, JsonUtility.ToJson(new WorldRevisionRetention { builds = builds.OrderBy(id => id).ToArray() }, true));
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        }

        public static void RemoveUnusedRevision(string world, string build)
        {
            using var lease = Acquire(world);
            var pointer = ReadPublished(world);
            if (pointer?.current == build || pointer?.previous == build || IsRetained(world, build))
                throw new InvalidOperationException("발행 또는 보존 지정 revision은 정리할 수 없습니다.");
            _ = ReadRevision(world, build, reopen: false, requireApprovedWarnings: false);
            string folder = Revision(world, build);
            bool InRevision(string path) => path.StartsWith(folder + "/", StringComparison.Ordinal);
            if (EditorBuildSettings.scenes.Any(scene => InRevision(scene.path)) ||
                InRevision(AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene) ?? ""))
                throw new InvalidOperationException("프로젝트 씬 설정에서 사용 중인 revision입니다.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (InRevision(SceneManager.GetSceneAt(i).path)) throw new InvalidOperationException("열린 revision은 정리할 수 없습니다.");

            foreach (string asset in AssetDatabase.GetAllAssetPaths())
            {
                if (!asset.StartsWith("Assets/", StringComparison.Ordinal) || InRevision(asset) ||
                    AssetDatabase.IsValidFolder(asset) || !File.Exists(asset)) continue;
                if (AssetDatabase.GetDependencies(asset, false).Any(InRevision))
                    throw new InvalidOperationException("다른 에셋이 revision을 참조합니다 · " + asset);
            }
            string addressables = "Assets/AddressableAssetsData";
            if (Directory.Exists(addressables))
            {
                var guids = AssetDatabase.FindAssets("", new[] { folder });
                foreach (string file in Directory.GetFiles(addressables, "*.asset", SearchOption.AllDirectories))
                {
                    string serialized = File.ReadAllText(file);
                    if (guids.Any(guid => serialized.Contains(guid, StringComparison.Ordinal)))
                        throw new InvalidOperationException("Addressables에 등록된 revision입니다 · " + file);
                }
            }
            if (!AssetDatabase.DeleteAsset(folder)) throw new IOException("과거 revision 정리에 실패했습니다.");
        }

        private static string RetentionPath(string world) => WorldRoot(world) + "/Published/Retention.json";

        private static string[] Retention(string world)
        {
            string path = RetentionPath(world);
            return File.Exists(path) ? Read<WorldRevisionRetention>(path).builds ?? Array.Empty<string>() : Array.Empty<string>();
        }

        internal static FileStream Acquire(string world)
        {
            PlacementIdentity.ValidateGuid(world);
            string folder = "Library/WorldBuilder/" + world;
            Directory.CreateDirectory(folder);
            try { return new FileStream(folder + "/P07.lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
            catch (IOException exception) { throw new InvalidOperationException("이 월드의 Bake 또는 Publish가 실행 중입니다.", exception); }
        }

        private static void EnsureInput(WorldDefinition definition, WorldNavigationLayout layout, WorldBuildReceipt receipt)
        {
            var scene = SceneManager.GetActiveScene();
            if (definition.ActiveSession != null || scene.isDirty || scene.path != AssetDatabase.GUIDToAssetPath(definition.PreviewSceneGuid) ||
                layout == null || layout.worldGuid != definition.WorldGuid || InputHash(definition, layout) != receipt.inputHash)
                throw new InvalidOperationException("WB102 · 입력 snapshot이 달라졌습니다. 저작을 저장하고 새 revision을 Bake하세요.");
        }

        internal static string InputHash(WorldDefinition definition, WorldNavigationLayout layout)
        {
            string scene = AssetDatabase.GUIDToAssetPath(definition.PreviewSceneGuid);
            string[] roots = { scene, AssetDatabase.GetAssetPath(definition), AssetDatabase.GetAssetPath(layout) };
            var paths = AssetDatabase.GetDependencies(roots, true).Concat(roots).Where(File.Exists).ToHashSet(StringComparer.Ordinal);
            foreach (var asset in Resources.FindObjectsOfTypeAll<UnityEngine.Object>())
            {
                if (!EditorUtility.IsPersistent(asset) || !EditorUtility.IsDirty(asset) || !AssetDatabase.IsNativeAsset(asset)) continue;
                string dirtyPath = AssetDatabase.GetAssetPath(asset);
                if (paths.Contains(dirtyPath)) throw new InvalidOperationException("저장되지 않은 입력 · " + dirtyPath);
            }
            var text = new StringBuilder();
            foreach (string path in paths.OrderBy(p => p, StringComparer.Ordinal))
            {
                text.Append(path).Append('|').Append(HashFile(path)).Append('|');
                if (File.Exists(path + ".meta")) text.Append(HashFile(path + ".meta"));
                text.AppendLine();
            }
            text.Append(HashFile("ProjectSettings/NavMeshAreas.asset")).Append(HashFile("ProjectSettings/DynamicsManager.asset"));
            return WorldDefinition.HashBytes(Encoding.UTF8.GetBytes(text.ToString()));
        }

        private static WorldFileDigest[] Files(string directory)
        {
            return Directory.GetFiles(directory, "*", SearchOption.AllDirectories).Select(p => p.Replace('\\', '/'))
                .Where(p => p != directory + "/Build.json" && p != directory + "/Build.json.meta")
                .OrderBy(p => p, StringComparer.Ordinal)
                .Select(p => new WorldFileDigest { path = p.Substring(directory.Length + 1), sha256 = HashFile(p) }).ToArray();
        }

        private static void CommitPointer(string world, WorldPublishedPointer old, WorldPublishedPointer next, Action<string> checkpoint)
        {
            string path = PublishedPath(world);
            string folder = Path.GetDirectoryName(path).Replace('\\', '/');
            WorldBuilderAuthoringAssets.EnsureFolder(folder);
            // 실제 포인터는 한 파일이다. 복구 기록을 먼저 남기고 같은 폴더에서 교체한다.
            string journal = "Library/WorldBuilder/" + world + "/PublishRecovery.json";
            string previousText = File.Exists(path) ? File.ReadAllText(path) : null;
            AtomicWrite(journal, JsonUtility.ToJson(new WorldPublishTransition { previous = old, next = next }, true));
            checkpoint?.Invoke("BeforePointerReplace");
            AtomicWrite(path, JsonUtility.ToJson(next, true));
            try
            {
                checkpoint?.Invoke("AfterPointerReplace");
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                if (AssetDatabase.LoadAssetAtPath<TextAsset>(path) == null)
                    throw new IOException("발행 포인터를 Unity 에셋으로 읽을 수 없습니다.");
            }
            catch
            {
                if (previousText != null)
                {
                    AtomicWrite(path, previousText);
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                }
                else if (AssetDatabase.LoadAssetAtPath<TextAsset>(path) != null) AssetDatabase.DeleteAsset(path);
                else File.Delete(path);
                throw;
            }
        }

        private static void ValidateCatalog(WorldBakeCatalog catalog, string world, string build, bool reopen)
        {
            string revision = Revision(world, build);
            if (catalog == null || catalog.worldGuid != world || catalog.buildId != build ||
                catalog.scenePath != revision + "/World.unity" || AssetDatabase.GUIDToAssetPath(catalog.sceneGuid) != catalog.scenePath ||
                catalog.surfaces == null || catalog.surfaces.Length == 0 || catalog.surfaces.Any(s => s.data == null ||
                    !AssetDatabase.GetAssetPath(s.data).StartsWith(revision + "/", StringComparison.Ordinal)))
                throw new InvalidDataException("발행 catalog의 씬 또는 Navigation 참조가 불완전합니다.");
            var main = new[] { AssetDatabase.GetAssetPath(catalog), catalog.scenePath };
            ValidateRuntimeDependencies(main);
            if (!reopen) return;
            var active = SceneManager.GetActiveScene();
            var scene = SceneManager.GetSceneByPath(catalog.scenePath);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(catalog.scenePath, OpenSceneMode.Additive);
            try
            {
                if (scene.isDirty) throw new InvalidDataException("열린 revision 씬에 미저장 편집이 있습니다.");
                var roots = scene.GetRootGameObjects();
                foreach (var component in roots.SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true)))
                {
                    if (component == null || IsEditorType(component.GetType()))
                        throw new InvalidDataException("런타임 씬에 누락 또는 Editor 전용 컴포넌트가 있습니다.");
                    using var serialized = new SerializedObject(component);
                    var property = serialized.GetIterator();
                    while (property.Next(true))
                    {
                        if (property.propertyType != SerializedPropertyType.ObjectReference || property.propertyPath == "m_Script") continue;
                        var reference = property.objectReferenceValue;
                        if (reference != null && IsEditorType(reference.GetType()))
                            throw new InvalidDataException("런타임 씬에 Editor 전용 직렬화 참조가 있습니다 · " + component.name + " / " + property.propertyPath);
                    }
                }
                var navigation = roots.SelectMany(r => r.GetComponentsInChildren<WorldBakedNavigation>(true)).Single();
                if (navigation.catalog != catalog || roots.SelectMany(r => r.GetComponentsInChildren<Terrain>(true)).Any(t =>
                    t.terrainData == null || !AssetDatabase.GetAssetPath(t.terrainData).StartsWith(revision + "/", StringComparison.Ordinal) ||
                    !t.TryGetComponent<TerrainCollider>(out var c) || c.terrainData != t.terrainData))
                    throw new InvalidDataException("재개방한 revision의 NavMesh 또는 TerrainCollider 참조 오류.");
                WorldNavigationBake.ValidatePaths(catalog);
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
                if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
            }
        }

        internal static void ValidateRuntimeDependencies(string[] main)
        {
            foreach (var path in AssetDatabase.GetDependencies(main, true))
            {
                if (path.Contains("/Authoring/") || path.Contains("/Source/") && path.StartsWith("Assets/WorldBuilder/Worlds/", StringComparison.Ordinal) ||
                    path.EndsWith(".raw", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("Assets/WorldBuilder/Worlds/", StringComparison.Ordinal) && path.EndsWith("/Base.bytes", StringComparison.Ordinal))
                    throw new InvalidDataException("런타임 revision이 저작 자료를 참조합니다 · " + path);
                var type = AssetDatabase.GetMainAssetTypeAtPath(path);
                if (type == typeof(WorldDefinition) || type == typeof(WorldNavigationLayout) || type == typeof(WorldPlan) ||
                    type == typeof(WorldProfile) || type == typeof(WorldEcology) || type == typeof(WaterSystemDefinition) ||
                    type == typeof(PlacementCatalog) || type == typeof(PlacementRuleAsset) || type == typeof(BiomePreset) ||
                    type == typeof(VegetationPreset) || type == typeof(CaveModulePreset) || type == typeof(StructurePlanningPreset) ||
                    type == typeof(BridgePlanningPreset) || type == typeof(RoadPlanningPreset) || type == typeof(WaterBodyPreset))
                    throw new InvalidDataException("런타임 revision이 저작 SO를 참조합니다 · " + path);
                if (path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                {
                    // ShaderGraph와 Sub Graph의 임포트 의존성은 셰이더를 만들 때만 사용된다.
                    if (IsShaderImportDependency(path)) continue;
                    var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                    var scriptType = script != null ? script.GetClass() : null;
                    if (IsEditorType(scriptType) || path.Split('/').Contains("Editor"))
                        throw new InvalidDataException("런타임 씬에 Editor 스크립트가 있습니다 · " + path);
                }
            }
        }

        private static bool IsEditorType(Type type)
        {
            if (type == null) return false;
            string assembly = type.Assembly.GetName().Name;
            return assembly.EndsWith(".Editor", StringComparison.Ordinal) || assembly.StartsWith("UnityEditor", StringComparison.Ordinal) ||
                type.Namespace?.StartsWith("UnityEditor", StringComparison.Ordinal) == true;
        }

        private static bool IsShaderImportDependency(string path) => path is
            "Packages/com.unity.shadergraph/Editor/Data/SubGraph/SubGraphAsset.cs" or
            "Packages/com.unity.shadergraph/Editor/Importers/ShaderGraphMetadata.cs" or
            "Packages/com.unity.shadergraph/Editor/Importers/ShaderGraphImporter.cs" or
            "Packages/com.unity.shadergraph/Editor/Importers/ShaderSubGraphMetadata.cs" or
            "Packages/com.unity.shadergraph/Editor/Importers/ShaderSubGraphImporter.cs" or
            "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/AssetVersion.cs" or
            "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/BuiltInMetadata.cs" or
            "Packages/com.unity.render-pipelines.universal/Editor/AssetVersion.cs" or
            "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/UniversalMetadata.cs";

        private static string GuidOf(UnityEngine.Object asset) => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));
        private static T Read<T>(string path) where T : class => JsonUtility.FromJson<T>(File.ReadAllText(path)) ??
            throw new InvalidDataException("JSON을 읽을 수 없습니다 · " + path);
        private static void WriteReceipt(WorldBuildReceipt receipt) =>
            AtomicWrite(ReceiptPath(receipt.worldGuid, receipt.buildId), JsonUtility.ToJson(receipt, true));

        private static string HashFile(string path)
        {
            using var stream = File.OpenRead(path);
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }

        private static void AtomicWrite(string path, string content)
        {
            string temp = Path.Combine(Path.GetDirectoryName(path), "." + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    byte[] data = Encoding.UTF8.GetBytes(content);
                    stream.Write(data, 0, data.Length);
                    stream.Flush(true);
                }
                if (File.Exists(path))
                {
                    for (int attempt = 0; ; attempt++)
                    {
                        try { File.Replace(temp, path, null); break; }
                        catch (IOException) when (attempt < 4 && File.Exists(temp) && File.Exists(path))
                        {
                            // Windows의 임포트 프로세스가 기존 파일을 잠깐 읽을 수 있다.
                            System.Threading.Thread.Sleep(40 << attempt);
                        }
                    }
                }
                else File.Move(temp, path);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }
    }
}



