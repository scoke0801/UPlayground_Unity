using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UPlayGround.Gameplay.World;
using UPlayGround.Data.World;
using UPlayGround.EditorTools;
using UPlayGround.Gameplay.World.LivingWorld;

namespace UPlayGround.Tool.Editor.Map
{
    /// <summary>구매 원본을 보존해 GPU 파생 자산·검증 씬·기존 장식 나비의 인계를 저작한다.</summary>
    public static partial class AmbientFaunaAuthoring
    {
        public const string DataRoot = "Assets/10.Datas/World/AmbientFauna";
        public const string ArtRoot = "Assets/08.Models/AmbientFauna";
        public const string PrefabRoot = "Assets/03.Prefabs/Environment/AmbientFauna";
        public const string TestScenePath = "Assets/01.Scenes/Test/AmbientFaunaTest.unity";
        public const string EvidenceRoot = "output/AmbientFauna/GpuImplementation";
        private const string SourcePrefab = "Assets/ExternalAssets/Environment/Idyllic Fantasy Nature/Prefabs/Code Related/Butterfly_01.prefab";
        private const string ShaderRoot = "Assets/06.Shaders/AmbientFauna";

        private static UniversalRenderPipelineAsset BuildRendering(RenderPipelineAsset source)
        {
            if (!(source is UniversalRenderPipelineAsset)) throw new InvalidOperationException("URP 원본 프로필이 필요합니다.");
            string root = DataRoot + "/Rendering";
            if (AssetDatabase.GetAssetPath(source).StartsWith(root + "/", StringComparison.Ordinal))
            {
                var sourceSettings = new SerializedObject(source).FindProperty("m_RendererDataList");
                bool ready = sourceSettings.arraySize > 0;
                for (int i = 0; i < sourceSettings.arraySize; i++)
                {
                    var data = sourceSettings.GetArrayElementAtIndex(i).objectReferenceValue as ScriptableRendererData;
                    if (data != null && !data.rendererFeatures.Any(f => f is AmbientFaunaRendererFeature)) ready = false;
                }
                if (ready) return (UniversalRenderPipelineAsset)source;
            }
            Directory.CreateDirectory(root);
            AssetDatabase.Refresh();
            string path = root + "/" + source.name + "_Ambient.asset";
            if (AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path) == null
                && !AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(source), path))
                throw new InvalidOperationException("씬 전용 URP 프로필 복제에 실패했습니다.");
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            var serialized = new SerializedObject(pipeline);
            var renderers = serialized.FindProperty("m_RendererDataList");
            for (int i = 0; i < renderers.arraySize; i++)
            {
                var property = renderers.GetArrayElementAtIndex(i);
                var original = property.objectReferenceValue as ScriptableRendererData;
                if (original == null) continue;
                string rendererPath = root + "/" + source.name + "_Renderer_" + i + ".asset";
                if (AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(rendererPath) == null
                    && !AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(original), rendererPath))
                    throw new InvalidOperationException("씬 전용 Renderer 복제에 실패했습니다.");
                var renderer = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(rendererPath);
                if (!renderer.rendererFeatures.Any(f => f is AmbientFaunaRendererFeature))
                {
                    var feature = ScriptableObject.CreateInstance<AmbientFaunaRendererFeature>();
                    feature.name = "Ambient Fauna";
                    AssetDatabase.AddObjectToAsset(feature, renderer);
                    renderer.rendererFeatures.Add(feature);
                    renderer.SetDirty();
                    EditorUtility.SetDirty(renderer);
                }
                property.objectReferenceValue = renderer;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pipeline);
            AssetDatabase.SaveAssets();
            return pipeline;
        }

        private static void AttachRendering(Scene scene)
        {
            var binding = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SceneEnvironmentRendering>(true)).FirstOrDefault();
            RenderPipelineAsset source = QualitySettings.renderPipeline != null ? QualitySettings.renderPipeline : GraphicsSettings.defaultRenderPipeline;
            if (binding != null)
            {
                var existing = new SerializedObject(binding).FindProperty("_renderPipeline").objectReferenceValue as RenderPipelineAsset;
                if (existing != null) source = existing;
            }
            var pipeline = BuildRendering(source);
            if (binding == null)
            {
                var gameObject = new GameObject("Ambient Fauna Rendering");
                Undo.RegisterCreatedObjectUndo(gameObject, "GPU 환경 생물 렌더 연결");
                SceneManager.MoveGameObjectToScene(gameObject, scene);
                binding = gameObject.AddComponent<SceneEnvironmentRendering>();
            }
            var settings = new SerializedObject(binding);
            Undo.RecordObject(binding, "GPU 환경 생물 렌더 연결");
            settings.FindProperty("_renderPipeline").objectReferenceValue = pipeline;
            settings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(binding);
        }

        /// <summary>선택한 그룹의 고정 좌표·안전 구와 씬 전용 렌더 연결을 갱신한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/환경 생물/GPU 서식지 베이크")]
        public static void BakeSelectedHabitats()
        {
            if (Application.isPlaying) throw new InvalidOperationException("편집 모드에서 베이크해야 합니다.");
            var groups = Selection.gameObjects.SelectMany(g => g.GetComponentsInChildren<AmbientFaunaGroup>(true)).Distinct().ToArray();
            if (groups.Length == 0) throw new InvalidOperationException("GPU 그룹 또는 상위 서식지를 선택하세요.");
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            try
            {
                foreach (var group in groups)
                {
                    Undo.RecordObject(group, "GPU 서식지 베이크");
                    var anchors = (Vector4[])typeof(AmbientFaunaGroup).GetField("_anchors",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(group);
                    var placement = group.GetComponent<AmbientFaunaPlacement>();
                    if (placement != null) anchors = BakePlacement(placement);
                    group.SetBakedAnchors(anchors);
                    if (!group.TryPrepare(out string error)) throw new InvalidOperationException(group.name + ": " + error);
                    EditorUtility.SetDirty(group);
                    EditorSceneManager.MarkSceneDirty(group.gameObject.scene);
                }
                foreach (Scene scene in groups.Select(g => g.gameObject.scene).Distinct()) AttachRendering(scene);
                Undo.CollapseUndoOperations(undoGroup);
            }
            catch { Undo.RevertAllDownToGroup(undoGroup); throw; }
        }

        /// <summary>기존에 만든 검증 씬에 씬 전용 RenderGraph 출력 프로필을 연결한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/환경 생물/GPU 검증 렌더 연결")]
        public static void ConnectTestRendering()
        {
            if (Application.isPlaying) throw new InvalidOperationException("편집 모드에서 실행해야 합니다.");
            Scene previous = SceneManager.GetActiveScene();
            Scene test = EditorSceneManager.OpenScene(TestScenePath, OpenSceneMode.Additive);
            try { AttachRendering(test); EditorSceneManager.SaveScene(test); }
            finally { SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(test, true); }
        }

        /// <summary>원본 메시를 정적 1m 메시로 파생하고 네 종의 공유 프로필을 생성한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/환경 생물/GPU 자산 생성")]
        public static void BuildAssets()
        {
            if (Application.isPlaying) throw new InvalidOperationException("편집 모드에서 생성해야 합니다.");
            Directory.CreateDirectory(DataRoot);
            Directory.CreateDirectory(ArtRoot);
            Directory.CreateDirectory(PrefabRoot);
            Directory.CreateDirectory(EvidenceRoot);
            AssetDatabase.Refresh();
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefab).GetComponentInChildren<SkinnedMeshRenderer>(true);
            Mesh butterfly = LoadOrCreateMesh(ArtRoot + "/ButterflyGpu.asset", () => DeriveButterfly(source.sharedMesh));
            Mesh mote = LoadOrCreateMesh(ArtRoot + "/AirMote.asset", CreateMote);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderRoot + "/AmbientFauna.shader");
            var compute = AssetDatabase.LoadAssetAtPath<ComputeShader>(ShaderRoot + "/AmbientFauna.compute");
            if (shader == null || compute == null || ShaderUtil.ShaderHasError(shader))
                throw new InvalidOperationException("GPU 환경 생물 셰이더를 먼저 복구해야 합니다.");
            Texture texture = source.sharedMaterials[1].mainTexture;
            string[] species = { "Butterfly", "Gnat", "Firefly", "AirMote" };
            int[] capacities = { 2, 4, 3, 4 };
            Color[] colors = { new(1, 0.57f, 0.22f), new(0.35f, 0.28f, 0.16f), new(0.65f, 1, 0.2f), new(0.8f, 0.85f, 0.65f) };
            for (int i = 0; i < species.Length; i++)
            {
                string materialPath = DataRoot + "/" + species[i] + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    material = new Material(shader) { name = species[i], enableInstancing = true };
                    material.SetTexture("_BaseMap", texture);
                    material.SetColor("_Tint", colors[i]);
                    material.SetColor("_BodyColor", new Color(0.045f, 0.03f, 0.015f));
                    material.SetFloat("_Appearance", i);
                    material.SetFloat("_Emission", i == 2 ? 2.5f : i == 3 ? 0.25f : 0);
                    AssetDatabase.CreateAsset(material, materialPath);
                }
                string profilePath = DataRoot + "/" + species[i] + ".asset";
                var profile = AssetDatabase.LoadAssetAtPath<AmbientFaunaProfileSO>(profilePath);
                if (profile == null)
                {
                    profile = ScriptableObject.CreateInstance<AmbientFaunaProfileSO>();
                    profile.SpeciesId = "ambient." + species[i].ToLowerInvariant();
                    profile.Mesh = i == 3 ? mote : butterfly;
                    profile.Material = material;
                    profile.Simulation = compute;
                    profile.SizeRange = i == 0 ? new Vector2(0.06f, 0.10f) : i == 3 ? new Vector2(0.012f, 0.025f) : new Vector2(0.025f, 0.045f);
                    profile.SpeedRange = i == 0 ? new Vector2(0.2f, 0.5f) : new Vector2(0.05f, 0.18f);
                    profile.FlapFrequency = i == 3 ? 0.3f : i == 0 ? 7 : 16;
                    profile.FlapAngle = i == 3 ? 0 : 60;
                    if (i == 2) profile.HourDensity = new AnimationCurve(new Keyframe(0, 1), new Keyframe(5, 1), new Keyframe(7, 0), new Keyframe(17, 0), new Keyframe(20, 1), new Keyframe(24, 1));
                    else if (i == 0) profile.HourDensity = new AnimationCurve(new Keyframe(0, 0), new Keyframe(5, 0), new Keyframe(8, 1), new Keyframe(17, 1), new Keyframe(20, 0), new Keyframe(24, 0));
                    AssetDatabase.CreateAsset(profile, profilePath);
                }
                if (!profile.TryValidate(out string error)) throw new InvalidOperationException(species[i] + ": " + error);
                BuildPrefab(species[i], profile, capacities[i]);
            }
            CompleteSpeciesAssets();
            AssetDatabase.SaveAssets();
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source.sharedMesh, out string guid, out long localId);
            File.WriteAllText(EvidenceRoot + "/asset-inventory.json", JsonUtility.ToJson(new AssetInventory {
                Source = SourcePrefab, SourceMeshGuid = guid, SourceMeshLocalId = localId,
                SourceTexture = AssetDatabase.GetAssetPath(texture), DerivedMesh = AssetDatabase.GetAssetPath(butterfly),
                Vertices = butterfly.vertexCount, Triangles = butterfly.triangles.Length / 3,
                Normalization = 1 / source.sharedMesh.bounds.size.x }, true));
        }

        private static Mesh DeriveButterfly(Mesh source)
        {
            if (source.subMeshCount != 2) throw new InvalidOperationException("몸통/날개 submesh 계약이 달라졌습니다.");
            Vector3[] vertices = source.vertices;
            Vector2[] wings = new Vector2[vertices.Length];
            Vector2[] hinges = new Vector2[vertices.Length];
            int[] wingIndices = source.GetIndices(1);
            float normalization = 1 / source.bounds.size.x;
            for (int i = 0; i < wingIndices.Length; i += 3)
            {
                float side = Mathf.Sign(vertices[wingIndices[i]].x + vertices[wingIndices[i + 1]].x + vertices[wingIndices[i + 2]].x);
                for (int j = 0; j < 3; j++) wings[wingIndices[i + j]] = new Vector2(side, 1);
            }
            float leftHinge = float.NegativeInfinity, rightHinge = float.PositiveInfinity;
            for (int i = 0; i < vertices.Length; i++)
            {
                if (wings[i].x < 0) leftHinge = Mathf.Max(leftHinge, vertices[i].x);
                if (wings[i].x > 0) rightHinge = Mathf.Min(rightHinge, vertices[i].x);
            }
            for (int i = 0; i < vertices.Length; i++)
            {
                if (wings[i].y > 0) hinges[i] = new Vector2((wings[i].x < 0 ? leftHinge : rightHinge) * normalization, 0);
                vertices[i] *= normalization;
            }
            var mesh = new Mesh { name = "ButterflyGpu", vertices = vertices, normals = source.normals,
                uv = source.uv, uv2 = wings, uv3 = hinges, triangles = source.triangles };
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateMote()
        {
            var mesh = new Mesh { name = "AirMote", vertices = new[] {
                new Vector3(-0.5f, 0, -0.5f), new Vector3(0.5f, 0, -0.5f), new Vector3(0.5f, 0, 0.5f), new Vector3(-0.5f, 0, 0.5f) },
                uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up },
                uv2 = new Vector2[4], uv3 = new Vector2[4], triangles = new[] { 0, 2, 1, 0, 3, 2 } };
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }

        private static Mesh LoadOrCreateMesh(string path, Func<Mesh> create)
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null) return mesh;
            mesh = create(); AssetDatabase.CreateAsset(mesh, path); return mesh;
        }

        private static void BuildPrefab(string species, AmbientFaunaProfileSO profile, int capacity)
        {
            string path = PrefabRoot + "/" + species + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
            var root = new GameObject(species);
            root.SetActive(false);
            try
            {
                root.AddComponent<AmbientFaunaHabitat>();
                CreateGroup(root.transform, species, profile, Vector3.up * 1.4f, capacity, 0.8f, 1);
                root.SetActive(true);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static AmbientFaunaGroup CreateGroup(Transform parent, string name, AmbientFaunaProfileSO profile,
            Vector3 localPosition, int capacity, float radius, uint seed)
        {
            var root = new GameObject(name);
            root.SetActive(false);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPosition;
            var group = root.AddComponent<AmbientFaunaGroup>();
            var serialized = new SerializedObject(group);
            serialized.FindProperty("_profile").objectReferenceValue = profile;
            serialized.FindProperty("_capacity").intValue = capacity;
            serialized.FindProperty("_seed").longValue = seed;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            group.SetBakedAnchors(new[] { new Vector4(0, 0, 0, radius) });
            EditorUtility.SetDirty(group);
            root.SetActive(true);
            return group;
        }

        /// <summary>열린 사용자 씬을 바꾸지 않고 독립 GPU 검증 씬을 만든다.</summary>
        [UPlaygroundTool("UPlayGround/월드/환경 생물/GPU 검증 씬 생성")]
        public static void CreateTestScene()
        {
            if (Application.isPlaying) throw new InvalidOperationException("편집 모드에서 생성해야 합니다.");
            if (File.Exists(TestScenePath)) throw new InvalidOperationException("기존 검증 씬을 자동 덮어쓰지 않습니다.");
            Directory.CreateDirectory(Path.GetDirectoryName(TestScenePath));
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                var root = new GameObject("AmbientFaunaTest");
                var habitat = root.AddComponent<AmbientFaunaHabitat>();
                var observer = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                observer.name = "Observer";
                observer.transform.position = new Vector3(0, 0.7f, -2.5f);
                observer.transform.localScale = new Vector3(0.3f, 0.6f, 0.3f);
                UnityEngine.Object.DestroyImmediate(observer.GetComponent<Collider>());
                var settings = new SerializedObject(habitat);
                settings.FindProperty("_observerOverride").objectReferenceValue = observer.transform;
                settings.FindProperty("_previewHour").floatValue = 18.5f;
                settings.ApplyModifiedPropertiesWithoutUndo();
                string[] names = { "Butterfly", "Gnat", "Firefly", "AirMote" };
                for (int i = 0; i < names.Length; i++) CreateGroup(root.transform, names[i],
                    AssetDatabase.LoadAssetAtPath<AmbientFaunaProfileSO>(DataRoot + "/" + names[i] + ".asset"),
                    new Vector3(-3 + i * 2, 1.35f, 0), i == 0 ? 2 : i == 2 ? 3 : 4, 0.85f, (uint)(73 + i));
                var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floor.name = "TestGround";
                floor.transform.position = new Vector3(0, -0.05f, 0);
                floor.transform.localScale = new Vector3(12, 0.1f, 6);
                string floorPath = DataRoot + "/TestGround.mat";
                var floorMaterial = AssetDatabase.LoadAssetAtPath<Material>(floorPath);
                if (floorMaterial == null)
                {
                    floorMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    floorMaterial.color = new Color(0.17f, 0.22f, 0.13f);
                    AssetDatabase.CreateAsset(floorMaterial, floorPath);
                }
                floor.GetComponent<Renderer>().sharedMaterial = floorMaterial;
                var light = new GameObject("Sun").AddComponent<Light>();
                light.type = LightType.Directional; light.intensity = 1.3f;
                light.transform.rotation = Quaternion.Euler(45, -25, 0);
                var camera = new GameObject("Main Camera").AddComponent<UnityEngine.Camera>();
                camera.tag = "MainCamera"; camera.fieldOfView = 45;
                camera.transform.position = new Vector3(0, 3.2f, -6.5f);
                camera.transform.LookAt(new Vector3(0, 1.2f, 0));
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.06f, 0.09f, 0.13f);
                RenderSettings.ambientLight = new Color(0.45f, 0.5f, 0.55f);
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                var preview = new GameObject("Preview Controls").AddComponent<UPlayGround.CombatValidation.AmbientFaunaPreviewDriver>();
                var driver = new SerializedObject(preview);
                driver.FindProperty("_habitat").objectReferenceValue = habitat;
                driver.FindProperty("_observer").objectReferenceValue = observer.transform;
                driver.ApplyModifiedPropertiesWithoutUndo();
                AttachRendering(scene);
                EditorSceneManager.SaveScene(scene, TestScenePath);
                AssetDatabase.SaveAssets();
            }
            finally { SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(scene, true); }
        }

        /// <summary>열린 정규 월드의 장식 나비에만 되돌릴 수 있는 GPU 인계 루트를 추가한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/환경 생물/GPU 월드 연결")]
        public static void InstallInActiveWorld()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (Application.isPlaying || scene.isDirty || scene.path != "Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")
                throw new InvalidOperationException("저장된 LakeVillageWorld 편집 씬에서만 실행합니다.");
            if (scene.GetRootGameObjects().Any(g => g.GetComponent<AmbientFaunaHabitat>() != null))
                throw new InvalidOperationException("이미 GPU 환경 생물이 연결돼 있습니다.");
            Directory.CreateDirectory(EvidenceRoot);
            string backup = EvidenceRoot + "/LakeVillageWorld.before-gpu-fauna.unity";
            if (!File.Exists(backup)) File.Copy(scene.path, backup);
            if (!File.Exists(backup + ".meta")) File.Copy(scene.path + ".meta", backup + ".meta");
            string data = "Assets/10.Datas/World/Wildlife/";
            var eligible = new[] { "Butterfly_01", "Butterfly_02", "Butterfly_03", "APD_Butterfly" }
                .Select(n => AssetDatabase.LoadAssetAtPath<WildlifeProfileSO>(data + n + ".asset")).ToArray();
            var sources = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<WildlifeCreature>(true))
                .Where(c => eligible.Contains(c.Profile) && c.Profile != null && c.Profile.MaxHealth <= 0 && c.Profile.DropTable == null).ToArray();
            var root = new GameObject("AmbientFauna");
            root.SetActive(false);
            Undo.RegisterCreatedObjectUndo(root, "GPU 환경 생물 연결");
            var habitat = root.AddComponent<AmbientFaunaHabitat>();
            var setting = new SerializedObject(habitat);
            setting.FindProperty("_environment").objectReferenceValue = scene.GetRootGameObjects()
                .SelectMany(g => g.GetComponentsInChildren<EnvironmentController>(true)).FirstOrDefault();
            setting.ApplyModifiedPropertiesWithoutUndo();
            var profile = AssetDatabase.LoadAssetAtPath<AmbientFaunaProfileSO>(DataRoot + "/Butterfly.asset");
            int converted = 0;
            for (int i = 0; i < sources.Length; i++)
            {
                var cpu = sources[i];
                float radius = FindSafeRadius(cpu.transform.position, cpu.GetComponentInParent<WildlifeHabitat>());
                if (radius < profile.SizeRange.y) continue;
                var group = CreateGroup(root.transform, "Butterfly_" + i, profile, cpu.transform.position, 2, radius, (uint)(i + 91));
                var serialized = new SerializedObject(group);
                var fallback = serialized.FindProperty("_cpuFallbacks");
                fallback.arraySize = 1; fallback.GetArrayElementAtIndex(0).objectReferenceValue = cpu;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                converted++;
            }
            root.SetActive(true);
            AttachRendering(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            File.WriteAllText(EvidenceRoot + "/world-integration.json", "{\"candidates\":" + sources.Length + ",\"convertedGroups\":" + converted + ",\"capacity\":" + converted * 2 + ",\"cpuSourcesDeleted\":0}");
        }

        private static float FindSafeRadius(Vector3 center, WildlifeHabitat habitat)
        {
            float[] radii = { 0.7f, 0.5f, 0.3f };
            for (int i = 0; i < radii.Length; i++)
            {
                float radius = radii[i];
                if (habitat != null && !habitat.CanOccupy(center - Vector3.up * radius)) continue;
                bool blocked = Physics.OverlapSphere(center, radius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                    .Any(c => c.attachedRigidbody == null && c.GetComponentInParent<GameActor>() == null
                        && c.GetComponentInParent<WildlifeCreature>() == null);
                if (!blocked) return radius;
            }
            return 0;
        }

        /// <summary>독립 검증 씬을 격리해 실제 GPU 이동과 URP 색상 출력을 캡처한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/환경 생물/GPU 출력 캡처")]
        public static string CapturePreview() => CaptureSpecies("Butterfly", 12);

        /// <summary>야간 반딧불의 GPU 발광 출력을 캡처한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/환경 생물/GPU 반딧불 캡처")]
        public static string CaptureFireflyPreview() => CaptureSpecies("Firefly", 22);

        /// <summary>카메라를 향하는 부유 입자의 출력을 캡처한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/환경 생물/GPU 부유 입자 캡처")]
        public static string CaptureMotePreview() => CaptureSpecies("AirMote", 12);

        public static string CaptureSpecies(string species, float hour, bool closeup = false)
        {
            if (Application.isPlaying) throw new InvalidOperationException("편집 모드에서 실행해야 합니다.");
            Directory.CreateDirectory(CompletionEvidence);
            Scene previous = SceneManager.GetActiveScene();
            Scene preview = EditorSceneManager.OpenScene(TestScenePath, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(preview);
            var target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            RenderTexture previousTarget = RenderTexture.active;
            RenderPipelineAsset previousPipeline = QualitySettings.renderPipeline;
            AmbientFaunaGroup[] previewGroups = Array.Empty<AmbientFaunaGroup>();
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            try
            {
                var roots = preview.GetRootGameObjects();
                var binding = roots.SelectMany(r => r.GetComponentsInChildren<SceneEnvironmentRendering>(true)).Single();
                QualitySettings.renderPipeline = (RenderPipelineAsset)new SerializedObject(binding).FindProperty("_renderPipeline").objectReferenceValue;
                Vector3 offset = new(10000, 0, 0);
                foreach (var root in roots)
                {
                    root.transform.position += offset;
                    foreach (Transform item in root.GetComponentsInChildren<Transform>(true)) item.gameObject.layer = 30;
                }
                Physics.SyncTransforms();
                var camera = roots.SelectMany(g => g.GetComponentsInChildren<UnityEngine.Camera>(true)).Single();
                camera.cullingMask = 1 << 30;
                var profile = AssetDatabase.LoadAssetAtPath<AmbientFaunaProfileSO>(DataRoot + "/" + species + ".asset");
                var focus = roots.SelectMany(g => g.GetComponentsInChildren<AmbientFaunaGroup>(true))
                    .Single(g => (AmbientFaunaProfileSO)typeof(AmbientFaunaGroup).GetField("_profile", flags).GetValue(g) == profile);
                typeof(AmbientFaunaGroup).GetField("_capacity", flags).SetValue(focus, closeup ? 1 : 4);
                if (closeup) typeof(AmbientFaunaGroup).GetField("_anchors", flags).SetValue(focus, new[] { new Vector4(0, 0, 0, 0.12f) });
                float distance = profile.SizeRange.y > 0.05f ? 2.5f : 1.7f;
                camera.transform.position = focus.transform.position + new Vector3(0, 0.75f, -distance);
                camera.transform.LookAt(focus.transform.position);
                camera.fieldOfView = closeup ? 10 : 42;
                if (hour >= 20 || hour < 6)
                {
                    RenderSettings.ambientLight = new Color(0.035f, 0.05f, 0.08f);
                    foreach (var light in roots.SelectMany(r => r.GetComponentsInChildren<Light>())) light.intensity = 0.08f;
                }
                var host = roots.SelectMany(g => g.GetComponentsInChildren<AmbientFaunaHabitat>(true)).Single();
                var groups = new[] { focus };
                previewGroups = groups;
                var request = new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest { destination = target };
                if (closeup && focus != null && focus.AllocatedCapacity > 0)
                    ((MaterialPropertyBlock)typeof(AmbientFaunaGroup).GetField("_properties", flags).GetValue(focus)).SetFloat("_AF_Density", 1);
                UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera, request);
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); image.Apply();
                Color32[] before = image.GetPixels32();
                string prefix = species.ToLowerInvariant() + (closeup ? "-macro" : "");
                File.WriteAllBytes(CompletionEvidence + "/" + prefix + "-off.png", image.EncodeToPNG());
                typeof(AmbientFaunaHabitat).GetField("_hour", flags).SetValue(host, hour);
                foreach (var group in groups)
                {
                    var anchors = (Vector4[])typeof(AmbientFaunaGroup).GetField("_anchors", flags).GetValue(group);
                    group.SetBakedAnchors(anchors);
                    typeof(AmbientFaunaGroup).GetField("_habitat", flags).SetValue(group, host);
                    if (!group.TryPrepare(out string error) || !host.TryRegister(group)) throw new InvalidOperationException(error);
                }
                RenderPipeline.SubmitRenderRequest(camera, request);
                for (int frame = 0; frame < 20; frame++)
                    foreach (var group in groups)
                    {
                        typeof(AmbientFaunaGroup).GetField("_lastFrame", flags).SetValue(group, -1);
                        group.ApplyWorldInputs(0.1f, Vector3.zero, false, Vector3.zero);
                    }
                if (closeup && focus != null && focus.AllocatedCapacity > 0)
                    ((MaterialPropertyBlock)typeof(AmbientFaunaGroup).GetField("_properties", flags).GetValue(focus)).SetFloat("_AF_Density", 1);
                UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera, request);
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); image.Apply();
                Color32[] after = image.GetPixels32();
                File.WriteAllBytes(CompletionEvidence + "/" + prefix + "-on.png", image.EncodeToPNG());
                int changed = 0;
                for (int i = 0; i < after.Length; i++)
                    if (Math.Abs(after[i].r - before[i].r) + Math.Abs(after[i].g - before[i].g) + Math.Abs(after[i].b - before[i].b) > 30) changed++;
                File.WriteAllText(CompletionEvidence + "/" + prefix + "-render-check.json", "{\"mode\":\"Editor GPU render request\",\"changedPixels\":" + changed
                    + ",\"allocatedCapacity\":" + host.AllocatedCapacity + ",\"width\":1280,\"height\":720}");
                if (changed == 0) throw new InvalidOperationException("GPU 출력 색상 차이가 없습니다: " + changed);
                                for (int sample = 1; sample <= 3; sample++)
                {
                    for (int frame = 0; frame < 5; frame++)
                    {
                        typeof(AmbientFaunaGroup).GetField("_lastFrame", flags).SetValue(focus, -1);
                        focus.ApplyWorldInputs(0.1f, Vector3.zero, false, new Vector3(0.3f, 0, 0.1f));
                    }
                    if (closeup && focus != null && focus.AllocatedCapacity > 0)
                    ((MaterialPropertyBlock)typeof(AmbientFaunaGroup).GetField("_properties", flags).GetValue(focus)).SetFloat("_AF_Density", 1);
                UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera, request);
                    RenderTexture.active = target;
                    image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); image.Apply();
                    File.WriteAllBytes(CompletionEvidence + "/" + prefix + "-t" + sample + ".png", image.EncodeToPNG());
                }
                return "GPU 출력 픽셀 차이: " + changed;
            }
            finally
            {
                foreach (var group in previewGroups) if (group != null) group.SetSuspended(true);
                foreach (var group in previewGroups)
                    if (group != null) group.GetComponentInParent<AmbientFaunaHabitat>().Unregister(group);
                RenderTexture.active = previousTarget;
                UnityEngine.Object.DestroyImmediate(image);
                UnityEngine.Object.DestroyImmediate(target);
                SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(preview, true);
                QualitySettings.renderPipeline = previousPipeline;
            }
        }

        [Serializable] private sealed class AssetInventory
        {
            public string Source, SourceMeshGuid, SourceTexture, DerivedMesh;
            public long SourceMeshLocalId;
            public int Vertices, Triangles;
            public float Normalization;
        }
    }
}
