using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using UPlayGround.WorldBuilder.Core;
using UPlayGround.WorldBuilder.Data;
using UPlayGround.WorldBuilder.Editor;
using UPlayGround.WorldBuilder.Runtime;
using Clock = System.Diagnostics.Stopwatch;

namespace UPlayGround.Editor
{
    /// <summary>P09의 고정 규격에서 실제 저작과 독립 Player용 콘텐츠를 생성한다.</summary>
    public static class WorldBuilderP09Validation
    {
        public const string Output = "output/WorldBuilder/P09";
        private const string AssetsPath = "Assets/10.Datas/WorldBuilder/P09Validation";
        private const string SourceLayout = "Assets/10.Datas/WorldBuilder/P08Validation/NavigationLayout.asset";

        public static string Id(int tiles) => "a909a909a909a909a909a909a909a90" + tiles;

        /// <summary>기존 P09 4km 타일을 임시 에셋으로 복제하고 같은 저작 명령을 반복한다.</summary>
        public static void ProfileDirty4Km(string logName)
        {
            if (string.IsNullOrWhiteSpace(logName) || Path.GetFileName(logName) != logName
                || logName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new ArgumentException("프로파일 로그 이름이 유효하지 않습니다.", nameof(logName));
            const int tiles = 8;
            string data = AssetsPath + "/" + tiles;
            var source = AssetDatabase.LoadAssetAtPath<WorldDefinition>(data + "/Definition.asset");
            var raw = AssetDatabase.LoadAssetAtPath<TextAsset>(data + "/BaseHeight.bytes");
            string id = Guid.NewGuid().ToString("N");
            string folder = "Assets/P09Profile-" + id;
            string cache = "Library/WorldBuilder/" + id;
            string log = Output + "/" + logName + ".csv";
            if (File.Exists(log)) throw new IOException("기존 프로파일 로그를 덮어쓸 수 없습니다.");
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            WorldDefinition definition = null;
            try
            {
                SceneManager.SetActiveScene(scene);
                MakeFolder(folder);
                var terrains = new Terrain[tiles, tiles];
                var grid = source.Profile.CreateGrid();
                for (int z = 0; z < tiles; z++)
                for (int x = 0; x < tiles; x++)
                {
                    string name = $"Terrain_{x}_{z}.asset";
                    string path = folder + "/" + name;
                    if (!AssetDatabase.CopyAsset(WorldBakePipeline.WorldRoot(Id(tiles)) + "/Terrain/" + name, path))
                        throw new IOException(path);
                    var tile = Terrain.CreateTerrainGameObject(AssetDatabase.LoadAssetAtPath<TerrainData>(path));
                    tile.transform.position = new Vector3((float)grid.SampleToWorld(x * source.Profile.TileCells),
                        source.Profile.OriginY, (float)grid.SampleToWorld(z * source.Profile.TileCells));
                    terrains[z, x] = tile.GetComponent<Terrain>();
                    terrains[z, x].drawInstanced = true;
                }
                definition = ScriptableObject.CreateInstance<WorldDefinition>();
                definition.Initialize(id, source.Profile, raw, source.PreviewSceneGuid);
                definition.SetOperations(source.Operations);
                definition.SetAppliedState(source.AppliedTerrainHash, source.AppliedStackHash, source.DirtyLayers);
                AssetDatabase.CreateAsset(definition, folder + "/Definition.asset");
                using var session = new WorldTerrainAuthoringSession(definition, terrains);
                var operation = definition.Operations[0].Clone();
                operation.value = .2f;
                session.ReplaceOperation(0, operation);
                session.Regenerate();
                Directory.CreateDirectory(Output);
                using (var stream = new FileStream(log, FileMode.CreateNew, FileAccess.Write))
                using (var writer = new StreamWriter(stream))
                    writer.WriteLine("iteration,dirty_ms,full_ms,dirty_hash,full_hash");
                for (int i = 0; i < 3; i++)
                {
                    operation.value = .3f + i * .1f;
                    var timer = Clock.StartNew();
                    session.ReplaceOperation(0, operation);
                    double dirtyMs = timer.Elapsed.TotalMilliseconds;
                    string dirtyHash = definition.AppliedTerrainHash;
                    timer.Restart();
                    session.Regenerate();
                    string fullHash = definition.AppliedTerrainHash;
                    File.AppendAllText(log, FormattableString.Invariant(
                        $"{i + 1},{dirtyMs:F2},{timer.Elapsed.TotalMilliseconds:F2},{dirtyHash},{fullHash}\n"));
                    if (dirtyHash != fullHash) throw new InvalidDataException("Dirty와 Full 지형 해시가 다릅니다.");
                }
            }
            finally
            {
                if (definition != null) Undo.ClearUndo(definition);
                SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
                AssetDatabase.DeleteAsset(folder);
                if (Directory.Exists(cache)) Directory.Delete(cache, true);
            }
        }

        public static void Generate(int tiles)
        {
            if (tiles != 4 && tiles != 8) throw new ArgumentOutOfRangeException(nameof(tiles));
            string world = Id(tiles);
            string root = WorldBakePipeline.WorldRoot(world);
            string data = AssetsPath + "/" + tiles;
            if (Directory.Exists(root) || Directory.Exists(data)) throw new IOException("이미 생성된 P09 검증 월드입니다.");
            var setup = EditorSceneManager.GetSceneManagerSetup();
            if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
                throw new InvalidOperationException("미저장 씬이 있습니다.");
            Directory.CreateDirectory(Output);
            string log = Output + "/authoring-" + tiles + ".log";
            File.WriteAllText(log, $"{SystemInfo.processorType} | {SystemInfo.graphicsDeviceName} | RAM {SystemInfo.systemMemorySize} MiB | Unity {Application.unityVersion} | {QualitySettings.names[QualitySettings.GetQualityLevel()]} | {QualitySettings.renderPipeline}\n");
            File.AppendAllText(log, "phase\telapsed_ms\tunity_bytes\tworking_set_bytes\tunity_start_bytes\tworking_set_start_bytes\tdetails\n");
            try
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                SceneManager.SetActiveScene(scene);
                MakeFolder(root + "/Terrain");
                MakeFolder(root + "/Published");
                MakeFolder(data);
                EditorUtility.UnloadUnusedAssetsImmediate();
                GC.Collect();
                var profile = ScriptableObject.CreateInstance<WorldProfile>();
                int count = tiles * 512 + 1;
                profile.Configure(count - 1, count, -80, 480, 512);
                AssetDatabase.CreateAsset(profile, data + "/Profile.asset");
                using var process = System.Diagnostics.Process.GetCurrentProcess();
                long startWorkingSet = process.WorkingSet64;
                long startMemory = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
                var timer = Clock.StartNew();
                var samples = new ushort[checked(count * count)];
                for (int z = 0; z < count; z++)
                for (int x = 0; x < count; x++)
                    samples[z * count + x] = (ushort)(12288 + ((x % 512) * (512 - x % 512) + (z % 512) * (512 - z % 512)) / 4096);
                var field = new HeightField(count, count, samples);
                byte[] raw = Raw16HeightmapCodec.Encode(field, HeightmapRowOrigin.South);
                string bytesPath = data + "/BaseHeight.bytes";
                File.WriteAllBytes(bytesPath, raw);
                AssetDatabase.ImportAsset(bytesPath, ImportAssetOptions.ForceSynchronousImport);
                var terrains = WorldBuilderSampleImporter.CreateTerrains(field, profile, scene, root + "/Terrain");
                WorldBuilderSampleImporter.ApplyPreviewSurface(terrains, root + "/Terrain");
                string scenePath = root + "/Authoring.unity";
                if (!EditorSceneManager.SaveScene(scene, scenePath)) throw new IOException(scenePath);
                var definition = ScriptableObject.CreateInstance<WorldDefinition>();
                definition.Initialize(world, profile, AssetDatabase.LoadAssetAtPath<TextAsset>(bytesPath), AssetDatabase.AssetPathToGUID(scenePath));
                AssetDatabase.CreateAsset(definition, data + "/Definition.asset");
                using var session = new WorldTerrainAuthoringSession(definition, terrains);
                Mark("Generate", $"raw={raw.Length}; tiles={terrains.Length}; samples={count}; height=513; holes=512; spacing=1");
                var operation = new TerrainOperationRecord
                {
                    operationGuid = Guid.NewGuid().ToString("N"), kind = TerrainOperationKind.AddDelta,
                    x = 240, z = 240, width = 32, height = 32, value = .2f, enabled = true
                };
                session.AddOperation(operation);
                // 실제 OffMeshLink 주행을 위해 타일 사이에 2m 지상 단절을 고정한다.
                session.AddOperation(new TerrainOperationRecord
                {
                    operationGuid = Guid.NewGuid().ToString("N"), kind = TerrainOperationKind.HoleCut,
                    x = 511, z = 0, width = 2, height = count - 1, value = 1, enabled = true
                });
                session.Regenerate();
                for (int iteration = 0; iteration < 3; iteration++)
                {
                    operation.value = .3f + iteration * .1f;
                    timer.Restart();
                    session.ReplaceOperation(0, operation);
                    Mark("Dirty", $"iteration={iteration + 1}; patch=32x32");
                    string dirty = definition.AppliedTerrainHash;
                    timer.Restart();
                    session.Regenerate();
                    Mark("Full", $"iteration={iteration + 1}; equal={dirty == definition.AppliedTerrainHash}");
                    if (dirty != definition.AppliedTerrainHash) throw new InvalidDataException("Dirty와 Full 지형 해시가 다릅니다.");
                }
                var template = AssetDatabase.LoadAssetAtPath<WorldNavigationLayout>(SourceLayout);
                var layout = ScriptableObject.CreateInstance<WorldNavigationLayout>();
                layout.worldGuid = world;
                layout.playerPrefab = template.playerPrefab;
                layout.agentTypeId = template.agentTypeId;
                string west = "tile_0_0", east = "tile_1_0";
                Vector3 from = Surface(500, 256), to = Surface(524, 256);
                string crossing = Guid.NewGuid().ToString("N");
                layout.crossings = new[] { new Crossing { id = crossing, fromSurface = west, toSurface = east,
                    from = Surface(510, 256), to = Surface(514, 256), width = 2f } };
                layout.routes = new[] { new RequiredRoute { id = Guid.NewGuid().ToString("N"),
                    fromSurface = west, toSurface = east, from = from, to = to, requiredCrossingId = crossing } };
                AssetDatabase.CreateAsset(layout, data + "/NavigationLayout.asset");
                AssetDatabase.SaveAssetIfDirty(definition);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException(scenePath);
                timer.Restart();
                var receipt = WorldBakePipeline.Bake(definition, layout, step => Mark("BakeStep", step), chunked: true);
                Mark("Bake", receipt.buildId);
                var bake = WorldBakePipeline.ReadRevision(world, receipt.buildId, reopen: true);
                var catalog = AssetDatabase.LoadAssetAtPath<WorldChunkCatalog>(WorldBakePipeline.Revision(world, receipt.buildId) + "/Chunks.asset");
                if (catalog.chunks.Length != tiles * tiles) throw new InvalidDataException("청크 수가 입력과 다릅니다.");
                Mark("Catalog", AssetDatabase.GetAssetPath(catalog));
                CreateBootstrap(root, data, bake);
                Mark("Complete", receipt.buildId);

                Vector3 Surface(float x, float z)
                {
                    var tile = terrains[(int)z / 512, (int)x / 512];
                    return new Vector3(x, tile.SampleHeight(new Vector3(x, 0, z)) + profile.OriginY, z);
                }

                void Mark(string stage, string details)
                {
                    process.Refresh();
                    File.AppendAllText(log, FormattableString.Invariant(
                        $"{stage}\t{timer.Elapsed.TotalMilliseconds:F2}\t{UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()}\t{process.WorkingSet64}\t{startMemory}\t{startWorkingSet}\t{details}\n"));
                }
            }
            catch (Exception error)
            {
                File.AppendAllText(log, error + "\n");
                throw;
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }

        private static void CreateBootstrap(string root, string data, WorldBakeCatalog bake)
        {
            string manifestPath = root + "/Published/Manifest.json";
            // 프로파일 빌드는 격리 카탈로그만 사용하므로 기존 Addressables 설정을 저장하지 않는다.
            File.WriteAllText(manifestPath, JsonUtility.ToJson(new WorldPublishedPointer
            {
                worldGuid = bake.worldGuid, current = bake.buildId,
                catalogGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(bake))
            }, true));
            AssetDatabase.ImportAsset(manifestPath, ImportAssetOptions.ForceSynchronousImport);
            SafeSaveBootstrap(root + "/Profile.unity", manifestPath, data + "/Proxy.mat", false);
        }

        public static void CheckIntegratedScenes()
        {
            string root = WorldBakePipeline.WorldRoot(WorldBuilderP08Validation.World);
            var published = WorldBakePipeline.ReadPublished(WorldBuilderP08Validation.World);
            string dependencyLog = Output + "/T21-editor.txt";
            File.WriteAllText(dependencyLog, string.Empty);
            foreach (string revision in new[] { published.current, published.previous })
            {
                _ = WorldBakePipeline.ReadRevision(WorldBuilderP08Validation.World, revision, reopen: true);
                File.AppendAllText(dependencyLog, revision + " | runtime transitive dependencies and reopened chunk scenes | PASS\n");
            }

            string regressionLog = Output + "/T23-missing-scripts.txt";
            var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).Concat(new[]
            {
                "Assets/WorldBuilder/Worlds/f606f606f606f606f606f606f6060906/P06Validation.unity",
                "Assets/WorldBuilder/Worlds/a707a707a707a707a707a707a707a707/P07Validation.unity",
                root + "/P08Authoring.unity"
            }).Distinct(StringComparer.Ordinal);
            File.WriteAllText(regressionLog, string.Empty);
            foreach (string path in scenes)
            {
                if (!File.Exists(path))
                {
                    File.AppendAllText(regressionLog, "MISSING SCENE | " + path + "\n");
                    continue;
                }
                var scene = EditorSceneManager.OpenPreviewScene(path);
                try
                {
                    int missing = scene.GetRootGameObjects().SelectMany(node => node.GetComponentsInChildren<Transform>(true))
                        .Sum(node => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(node.gameObject));
                    File.AppendAllText(regressionLog, $"Missing Script {missing} | {path}\n");
                }
                finally { EditorSceneManager.ClosePreviewScene(scene); }
            }
        }

        public static void VerifyPublishRecovery()
        {
            const string world = "a909a909a909a909a909a909a909a90e";
            const string data = AssetsPath + "/Recovery";
            string output = Output + "/T20.log";
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var definition = CreateRecoveryFixture(world, data, out var layout);
                var first = WorldBakePipeline.Bake(definition, layout);
                WorldBakePipeline.Publish(definition, first.buildId);
                string pointerPath = WorldBakePipeline.PublishedPath(world);
                string manifestGuid = AssetDatabase.AssetPathToGUID(pointerPath);
                byte[] original = File.ReadAllBytes(pointerPath);
                Expect<OperationCanceledException>(() => WorldBakePipeline.Bake(definition, layout,
                    step => { if (step == "입력 고정") throw new OperationCanceledException("P09 취소"); }));
                VerifyOriginal();
                var rejected = WorldBakePipeline.BuildIds(world).Single(id => id != first.buildId);
                if (WorldBakePipeline.LoadReceipt(world, rejected).state != WorldBuildState.Cancelled)
                    throw new InvalidOperationException("취소 receipt의 상태가 다릅니다.");
                Expect<InvalidDataException>(() => WorldBakePipeline.Publish(definition, rejected));
                VerifyOriginal();
                var second = WorldBakePipeline.Bake(definition, layout);
                Expect<IOException>(() => WorldBakePipeline.Publish(definition, second.buildId,
                    checkpoint: step => { if (step == "AfterPointerReplace") throw new IOException("P09 교체 직후 실패"); }));
                VerifyOriginal();
                WorldBakePipeline.Publish(definition, second.buildId);
                WorldBakePipeline.Rollback(world);
                var current = WorldBakePipeline.ReadPublished(world);
                if (current.current != first.buildId || current.previous != second.buildId ||
                    AssetDatabase.AssetPathToGUID(pointerPath) != manifestGuid)
                    throw new InvalidOperationException("Rollback 발행 포인터가 다릅니다.");
                File.WriteAllText(output, $"취소·거절·교체 실패·A→B→A 통과 | A {first.buildId} | B {second.buildId} | 취소 {rejected} | manifest {manifestGuid}\n");

                void VerifyOriginal()
                {
                    if (!File.ReadAllBytes(pointerPath).SequenceEqual(original) ||
                        AssetDatabase.AssetPathToGUID(pointerPath) != manifestGuid)
                        throw new InvalidDataException("실패 뒤 기존 발행 포인터가 달라졌습니다.");
                }
            }
            catch (Exception error)
            {
                File.AppendAllText(output, error + "\n");
                throw;
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }

        private static WorldDefinition CreateRecoveryFixture(string world, string data, out WorldNavigationLayout layout)
        {
            string root = WorldBakePipeline.WorldRoot(world);
            if (Directory.Exists(root) || Directory.Exists(data)) throw new IOException("P09 복구 검증 자산이 이미 있습니다.");
            MakeFolder(root + "/Terrain");
            MakeFolder(data);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var profile = ScriptableObject.CreateInstance<WorldProfile>();
            profile.Configure(64, 65, 0, 32, 64);
            AssetDatabase.CreateAsset(profile, data + "/Profile.asset");
            var field = new HeightField(65, 65, Enumerable.Repeat((ushort)16384, 65 * 65).ToArray());
            string basePath = data + "/BaseHeight.bytes";
            File.WriteAllBytes(basePath, Raw16HeightmapCodec.Encode(field, HeightmapRowOrigin.South));
            AssetDatabase.ImportAsset(basePath, ImportAssetOptions.ForceSynchronousImport);
            var terrains = WorldBuilderSampleImporter.CreateTerrains(field, profile, scene, root + "/Terrain");
            WorldBuilderSampleImporter.ApplyPreviewSurface(terrains, root + "/Terrain");
            string scenePath = root + "/Recovery.unity";
            if (!EditorSceneManager.SaveScene(scene, scenePath)) throw new IOException(scenePath);
            var definition = ScriptableObject.CreateInstance<WorldDefinition>();
            definition.Initialize(world, profile, AssetDatabase.LoadAssetAtPath<TextAsset>(basePath), AssetDatabase.AssetPathToGUID(scenePath));
            AssetDatabase.CreateAsset(definition, data + "/Definition.asset");
            using (var session = new WorldTerrainAuthoringSession(definition, terrains)) session.EnsureReadyForBake();
            AssetDatabase.SaveAssetIfDirty(definition);
            var template = AssetDatabase.LoadAssetAtPath<WorldNavigationLayout>(SourceLayout);
            layout = ScriptableObject.CreateInstance<WorldNavigationLayout>();
            layout.worldGuid = world;
            layout.playerPrefab = template.playerPrefab;
            layout.agentTypeId = template.agentTypeId;
            layout.routes = new[] { new RequiredRoute { id = Guid.NewGuid().ToString("N"),
                fromSurface = "tile_0_0", toSurface = "tile_0_0", from = new Vector3(8, 8, 8), to = new Vector3(16, 8, 8) } };
            AssetDatabase.CreateAsset(layout, data + "/NavigationLayout.asset");
            return definition;
        }

        private static void Expect<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException("기대한 실패: " + typeof(T).Name);
        }

        public static void CreateP08EditorProfile()
        {
            const string folder = AssetsPath + "/P08Baseline";
            MakeFolder(folder);
            string scenePath = folder + "/P08Baseline.unity";
            if (File.Exists(scenePath)) throw new IOException("P08 경로 측정용 씬이 이미 있습니다.");
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                SafeSaveBootstrap(scenePath, WorldBakePipeline.PublishedPath(WorldBuilderP08Validation.World),
                    folder + "/Proxy.mat", true);
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }

        private static void SafeSaveBootstrap(string scenePath, string manifestPath, string materialPath, bool p08Baseline)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", new Color(.3f, .36f, .24f));
            AssetDatabase.CreateAsset(material, materialPath);
            var light = new GameObject("햇빛").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(45, -30, 0);
            var camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.gameObject.AddComponent<AudioListener>();
            camera.farClipPlane = 5500;
            var host = new GameObject("P09 스트리밍 측정");
            var nav = host.AddComponent<WorldBakedNavigation>();
            var stream = host.AddComponent<WorldChunkStreamer>();
            stream.Configure(AssetDatabase.LoadAssetAtPath<TextAsset>(manifestPath), Array.Empty<WorldChunkCatalog>(), nav, material);
            var probe = host.AddComponent<WorldBuilderP09Probe>();
            probe.streamer = stream;
            probe.view = camera;
            probe.p08Baseline = p08Baseline;
            if (!EditorSceneManager.SaveScene(scene, scenePath)) throw new IOException("P09 측정용 씬 저장 실패");
        }

        public static void Build(string run)
        {
            if (string.IsNullOrWhiteSpace(run) || run.Any(c => !char.IsLetterOrDigit(c) && c != '-'))
                throw new ArgumentException(nameof(run));
            string output = Path.GetFullPath(Output + "/" + run);
            if (Directory.Exists(output)) throw new IOException("빌드 출력 경로가 이미 있습니다.");
            Directory.CreateDirectory(output);
            string log = output + "/build.log";
            string settingsPath = AssetsPath + "/Settings/" + run;
            MakeFolder(settingsPath);
            var settings = AddressableAssetSettings.Create(settingsPath, "P09-" + run, true, true);
            settings.BuildRemoteCatalog = false;
            var group = settings.DefaultGroup;
            group.GetSchema<BundledAssetGroupSchema>().BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackSeparately;
            var scenes = new List<string>();
            var allowed = new HashSet<string>(StringComparer.Ordinal);
            foreach (int tiles in new[] { 4, 8 })
            {
                string world = Id(tiles);
                string root = WorldBakePipeline.WorldRoot(world);
                var published = WorldBakePipeline.ReadPublished(world);
                string folder = WorldBakePipeline.Revision(world, published.current);
                var catalog = AssetDatabase.LoadAssetAtPath<WorldChunkCatalog>(folder + "/Chunks.asset");
                Add(folder + "/Chunks.asset", WorldChunkCatalog.Address(world, published.current));
                foreach (var chunk in catalog.chunks) Add(folder + "/" + chunk.id + ".unity", chunk.sceneKey);
                scenes.Add(root + "/Profile.unity");
            }
            string baseline = AssetsPath + "/P08Baseline/P08Baseline.unity";
            if (File.Exists(baseline))
            {
                string world = WorldBuilderP08Validation.World;
                var published = WorldBakePipeline.ReadPublished(world);
                string folder = WorldBakePipeline.Revision(world, published.current);
                var catalog = AssetDatabase.LoadAssetAtPath<WorldChunkCatalog>(folder + "/Chunks.asset");
                Add(folder + "/Chunks.asset", WorldChunkCatalog.Address(world, published.current));
                foreach (var chunk in catalog.chunks) Add(folder + "/" + chunk.id + ".unity", chunk.sceneKey);
                scenes.Add(baseline);
            }
            foreach (string scene in scenes) Inspect(scene);
            AssetDatabase.SaveAssetIfDirty(group);
            AssetDatabase.SaveAssetIfDirty(settings);
            File.AppendAllLines(log, allowed.OrderBy(path => path));
            var original = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
            var originalBuild = original.BuildAddressablesWithPlayerBuild;
            var originalOverride = AddressablesPlayerBuildProcessor.BuildAddressablesOverride;
            string originalLibrary = Addressables.LibraryPath;
            var stopwatch = Clock.StartNew();
            bool frameTiming = PlayerSettings.enableFrameTimingStats;
            const string playerSettingsPath = "ProjectSettings/ProjectSettings.asset";
            string playerSettingsText = File.ReadAllText(playerSettingsPath);
            byte[] playerSettingsBytes = File.ReadAllBytes(playerSettingsPath);
            bool buildLayout = ProjectConfigData.GenerateBuildLayout;
            string linkPath = original.ConfigFolder + "/link.xml";
            if (File.Exists(linkPath) || File.Exists(linkPath + ".meta"))
                throw new IOException("기본 Addressables link.xml이 이미 있습니다.");
            try
            {
                ProjectConfigData.GenerateBuildLayout = true;
                PlayerSettings.enableFrameTimingStats = true;
                original.BuildAddressablesWithPlayerBuild = AddressableAssetSettings.PlayerBuildOption.BuildWithPlayer;
                // Addressables 빌드 로그와 content-state 작성기가 프로젝트 경로를 앞에 붙인다.
                Addressables.LibraryPath = Output + "/" + run + "/Addressables/";
                AddressablesPlayerBuildProcessor.BuildAddressablesOverride = _ =>
                {
                    File.AppendAllText(log, $"Addressables start {Addressables.BuildPath}\n");
                    var result = settings.ActivePlayerDataBuilder.BuildData<UnityEditor.AddressableAssets.Build.AddressablesPlayerBuildResult>(
                        new UnityEditor.AddressableAssets.Build.AddressablesDataBuilderInput(settings));
                    File.AppendAllText(log, $"Addressables {stopwatch.Elapsed.TotalSeconds:F3}s: {result.OutputPath} | {result.Error}\n");
                    return result;
                };
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = scenes.ToArray(), target = BuildTarget.StandaloneWindows64,
                    locationPathName = output + "/WorldBuilderP09.exe",
                    options = BuildOptions.Development | BuildOptions.DetailedBuildReport,
                });
                File.AppendAllText(log, $"Player {report.summary.result} | {report.summary.totalTime} | {report.summary.totalSize} | errors {report.summary.totalErrors}\n");
                foreach (var file in report.GetFiles())
                {
                    File.AppendAllText(log, file.path + "\n");
                    if (Path.GetFileName(file.path) == "WorldBuilder.Editor.dll")
                        throw new InvalidDataException("Player에 WorldBuilder.Editor.dll이 포함됐습니다.");
                }
                foreach (var packed in report.packedAssets)
                foreach (var item in packed.contents)
                {
                    string path = item.sourceAssetPath;
                    File.AppendAllText(log, "packed\t" + path + "\n");
                    Verify(path);
                }
                if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Player 빌드 실패");
            }
            catch (Exception error)
            {
                File.AppendAllText(log, error + "\n");
                throw;
            }
            finally
            {
                if (File.Exists(linkPath)) AssetDatabase.DeleteAsset(linkPath);
                ProjectConfigData.GenerateBuildLayout = buildLayout;
                PlayerSettings.enableFrameTimingStats = frameTiming;
                // 빌드가 기록한 프로파일링 플래그만 제거하고 다른 설정 차이는 보존한다.
                if (!frameTiming && File.ReadAllText(playerSettingsPath) == playerSettingsText.Replace(
                    "  enableFrameTimingStats: 0", "  enableFrameTimingStats: 1"))
                    File.WriteAllBytes(playerSettingsPath, playerSettingsBytes);
                Addressables.LibraryPath = originalLibrary;
                AddressablesPlayerBuildProcessor.BuildAddressablesOverride = originalOverride;
                original.BuildAddressablesWithPlayerBuild = originalBuild;
            }

            void Add(string path, string address)
            {
                Inspect(path);
                settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(path), group).SetAddress(address);
                File.AppendAllText(log, $"Entry\t{address}\t{path}\n");
            }

            void Inspect(string path)
            {
                foreach (string dependency in AssetDatabase.GetDependencies(path, true))
                    if (allowed.Add(dependency)) Verify(dependency);
            }

            void Verify(string path)
            {
                if (path.EndsWith("/Definition.asset", StringComparison.Ordinal) ||
                    path.EndsWith("/BaseHeight.bytes", StringComparison.Ordinal) ||
                    path.EndsWith("/NavigationLayout.asset", StringComparison.Ordinal) ||
                    path.EndsWith("/Authoring.unity", StringComparison.Ordinal) ||
                    path.Contains("/WorldBuilder/Editor/"))
                    throw new InvalidDataException("Player에서 금지된 저작 의존성: " + path);
            }
        }

        private static void MakeFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            MakeFolder(path.Substring(0, slash));
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }
    }
}

