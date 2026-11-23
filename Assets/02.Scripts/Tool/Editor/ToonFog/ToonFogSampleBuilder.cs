using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UPlayGround.Data.Rendering;
using UPlayGround.EditorTools;
using UPlayGround.Gameplay.World.ToonFog;

namespace UPlayGround.Tool.Editor.ToonFog
{
    /// <summary>독립 안개 비교 씬과 최초 데이터 에셋을 만든다.</summary>
    public static class ToonFogSampleBuilder
    {
        public const string ScenePath = "Assets/01.Scenes/ArtReview/ToonFogValidation.unity";
        public const string DataPath = "Assets/10.Datas/Environment/ToonFog";
        private const string MaterialPath = "Assets/11.Material/ToonFog";
        private const string CompositePath = "Assets/06.Shaders/ToonFog/ToonFogComposite.shader";
        private const string RendererPath = "Assets/Settings/PC_Renderer.asset";
        private const string SurfacePath = "Assets/06.Shaders/ToonFog/ToonFogSampleSurface.shader";
        public static readonly Vector3 SampleOffset = Vector3.right * 20000;

        private static readonly (string name, string near, string middle, string far, float distance, float height, float scale, float baseHeight)[] Presets =
        {
            ("우거진 숲", "#527B72", "#8FB4AA", "#C4D6C7", 0.0016f, 0.008f, 12, 3),
            ("가을 숲", "#A17B58", "#D7B58A", "#E9D9B9", 0.0012f, 0.005f, 14, 4),
            ("메마른 지역", "#AA8B71", "#CEB99B", "#E4D6BB", 0.0014f, 0.002f, 25, 1),
            ("평야와 성", "#8EA7AC", "#B9CDD0", "#DCE5E3", 0.0007f, 0.002f, 20, 2),
            ("고산", "#8198B1", "#B5C9DC", "#DFE9F0", 0.0009f, 0.014f, 40, 16),
            ("강가 마을", "#739D9D", "#AEC9C6", "#DCE6DD", 0.0011f, 0.007f, 8, 2),
            ("호수 마을", "#829EB1", "#BBCCD8", "#E4EBEE", 0.0010f, 0.009f, 10, 3)
        };

        [UPlaygroundTool("UPlayGround/검증/툰 안개 샘플 생성")]
        private static void BuildFromCatalog() => Build();

        public static string Build()
        {
            Directory.CreateDirectory(DataPath);
            Directory.CreateDirectory(MaterialPath);
            AssetDatabase.Refresh();

            var profiles = new ToonFogProfile[Presets.Length];
            for (int i = 0; i < profiles.Length; i++) profiles[i] = EnsureProfile(i);
            var quality = EnsureQuality();
            EnsureRendererFeature();
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) return ScenePath;

            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                BuildLighting();
                var world = new GameObject("Toon Fog World").AddComponent<ToonFogWorld>();
                world.profile = profiles[0];
                world.quality = quality;
                var controls = world.gameObject.AddComponent<ToonFogSampleControls>();
                controls.world = world;
                controls.profiles = profiles;
                world.environmentSource = controls;
                var camera = new GameObject("Main Camera").AddComponent<Camera>();
                camera.tag = "MainCamera";
                camera.nearClipPlane = 0.2f;
                camera.farClipPlane = 1150;
                camera.fieldOfView = 56;
                camera.allowMSAA = true;
                camera.GetUniversalAdditionalCameraData().requiresColorOption = CameraOverrideOption.On;
                controls.sampleCamera = camera;

                var rock = EnsureSurface("Fog_Opaque", 0, false, new Color(0.38f, 0.48f, 0.37f));
                var stone = EnsureSurface("Fog_PaleOpaque", 0, false, new Color(0.77f, 0.73f, 0.59f));
                var flat = EnsureSurface("Fog_Reference", 0, false, new Color(0.93f, 0.52f, 0.24f));
                var clipped = EnsureSurface("Fog_Cutout", 4, false, new Color(0.29f, 0.57f, 0.27f));
                BuildCalibration(stone, flat);
                BuildValley(rock, stone);
                BuildIntegration(scene, stone, flat, clipped);
                BuildRegions(profiles, rock, stone);
                controls.viewpoints = new[]
                {
                    MakeView("01 거리 기준", new Vector3(0, 3, 0), new Vector3(0, 3, 50)),
                    MakeView("02 계곡과 국소 부피", new Vector3(2300, 4, 0), new Vector3(2300, 4, 45)),
                    MakeView("03 캐릭터·식생·굴절", new Vector3(4600, 4.5f, 0), new Vector3(4600, 2.5f, 25)),
                    MakeView("04 지역 경계", new Vector3(6900, 6, 0), new Vector3(6900, 3, 60)),
                    MakeView("05 계곡 상단", new Vector3(2286, 38, 5), new Vector3(2300, 1, 49))
                };
                controls.SelectView(0);
                foreach (var root in scene.GetRootGameObjects()) root.transform.position += SampleOffset;
                if (!EditorSceneManager.SaveScene(scene, ScenePath))
                    throw new InvalidOperationException("툰 안개 샘플 씬을 저장하지 못했습니다.");
            }
            finally
            {
                if (previous.IsValid()) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
            return ScenePath;
        }

        private static ToonFogProfile EnsureProfile(int index)
        {
            var preset = Presets[index];
            string path = $"{DataPath}/Fog_{index + 1:00}_{preset.name}.asset";
            var profile = AssetDatabase.LoadAssetAtPath<ToonFogProfile>(path);
            if (profile != null) return profile;
            profile = ScriptableObject.CreateInstance<ToonFogProfile>();
            profile.distanceDensity = preset.distance;
            profile.heightDensity = preset.height;
            profile.heightScale = preset.scale;
            profile.baseHeight = preset.baseHeight;
            ColorUtility.TryParseHtmlString(preset.near, out Color near);
            ColorUtility.TryParseHtmlString(preset.middle, out Color middle);
            ColorUtility.TryParseHtmlString(preset.far, out Color far);
            profile.heightTint = middle;
            profile.distanceGradient.SetKeys(new[]
            {
                new GradientColorKey(near, 0), new GradientColorKey(middle, 0.5f), new GradientColorKey(far, 1)
            }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
            AssetDatabase.CreateAsset(profile, path);
            return profile;
        }

        private static ToonFogQualitySettings EnsureQuality()
        {
            string path = $"{DataPath}/Fog_Quality.asset";
            var quality = AssetDatabase.LoadAssetAtPath<ToonFogQualitySettings>(path);
            if (quality != null) return quality;
            quality = ScriptableObject.CreateInstance<ToonFogQualitySettings>();
            string noisePath = $"{DataPath}/Fog_Noise.asset";
            quality.noise = AssetDatabase.LoadAssetAtPath<Texture2D>(noisePath);
            if (quality.noise == null)
            {
                const int size = 64;
                var noise = new Texture2D(size, size, TextureFormat.R8, false, true)
                { name = "Fog_Noise", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
                var pixels = new byte[size * size];
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float u = x * 2 * Mathf.PI / size;
                    float v = y * 2 * Mathf.PI / size;
                    pixels[y * size + x] = (byte)Mathf.RoundToInt(255 * (0.5f + 0.18f * Mathf.Sin(u + 2 * v)
                        + 0.16f * Mathf.Cos(2 * u - v) + 0.1f * Mathf.Sin(u - 3 * v)));
                }
                noise.SetPixelData(pixels, 0);
                noise.Apply();
                AssetDatabase.CreateAsset(noise, noisePath);
                quality.noise = noise;
            }
            AssetDatabase.CreateAsset(quality, path);
            return quality;
        }

        private static void EnsureRendererFeature()
        {
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (renderer == null) throw new InvalidOperationException(RendererPath);
            foreach (var existing in renderer.rendererFeatures) if (existing is ToonFogRendererFeature) return;
            var feature = ScriptableObject.CreateInstance<ToonFogRendererFeature>();
            feature.name = "Toon Fog";
            var properties = new SerializedObject(feature);
            properties.FindProperty("_shader").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Shader>(CompositePath);
            properties.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.AddObjectToAsset(feature, renderer);
            int index = renderer.rendererFeatures.FindIndex(item => item is UPlayGround.Gameplay.World.UnderwaterRendererFeature);
            if (index < 0) index = renderer.rendererFeatures.Count;
            renderer.rendererFeatures.Insert(index, feature);
            var serialized = new SerializedObject(renderer);
            var map = serialized.FindProperty("m_RendererFeatureMap");
            map.arraySize = renderer.rendererFeatures.Count;
            for (int i = 0; i < renderer.rendererFeatures.Count; i++)
            {
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(renderer.rendererFeatures[i], out string _, out long localId);
                map.GetArrayElementAtIndex(i).longValue = localId;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            renderer.SetDirty();
            EditorUtility.SetDirty(renderer);
            AssetDatabase.SaveAssetIfDirty(renderer);
        }

        private static void BuildLighting()
        {
            var light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.3f;
            light.transform.rotation = Quaternion.Euler(48, -32, 0);
            RenderSettings.sun = light;
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.66f, 0.73f, 0.8f);
            RenderSettings.ambientEquatorColor = new Color(0.46f, 0.51f, 0.56f);
            RenderSettings.ambientGroundColor = new Color(0.31f, 0.34f, 0.32f);
        }

        private static void BuildCalibration(Material ground, Material reference)
        {
            MakeShape(PrimitiveType.Cube, "거리 눈금 바닥", new Vector3(0, -1, 260), new Vector3(400, 2, 550), ground);
            int[] distances = { 10, 50, 100, 500 };
            for (int i = 0; i < distances.Length; i++)
            {
                float d = distances[i];
                float s = d * 0.085f;
                float x = d * (-0.32f + 0.2f * i);
                MakeShape(PrimitiveType.Quad, $"{d} m 기준", new Vector3(x, 3 + s * 0.15f, d), new Vector3(s, s, 1), reference);
            }
            for (int i = 1; i <= 3; i++)
            {
                var material = EnsureSurface($"Fog_Blend_{i}", i, true, new Color(0.3f, 0.78f, 0.95f));
                var full = EnsureSurface($"Fog_Blend_{i}_Full", i, false, reference.GetColor("_BaseColor"));
                MakeShape(PrimitiveType.Quad, $"Blend {i} alpha 0–1", new Vector3(-17 + i * 8, 6, 32),
                    new Vector3(5, 3, 1), material);
                MakeShape(PrimitiveType.Quad, $"Blend {i} alpha 1", new Vector3(-17 + i * 8, 10, 32),
                    new Vector3(5, 3, 1), full);
            }
        }

        private static void BuildValley(Material rock, Material stone)
        {
            MakeShape(PrimitiveType.Cube, "계곡 바닥", new Vector3(2300, -1, 85), new Vector3(240, 2, 220), stone);
            for (int i = 0; i < 7; i++)
            {
                float x = 2250 + i * 16;
                MakeShape(PrimitiveType.Cube, $"능선 {i + 1}", new Vector3(x, -1, 100 + i * 5),
                    new Vector3(24, 30 + Mathf.Abs(i - 3) * 8, 36), rock);
            }
            MakeShape(PrimitiveType.Cube, "앞벽·가림 비교", new Vector3(2291, 4, 25), new Vector3(8, 8, 2), rock);
            CreateVolume("회전 상자", new Vector3(2298, 3, 48), new Vector3(18, 4, 12), 26, ToonFogShape.Box, 1, 0.025f);
            CreateVolume("하늘 앞 타원체", new Vector3(2321, 19, 65), new Vector3(9, 5, 12), -45, ToonFogShape.Sphere, 2, 0.046f);
            CreateVolume("얇은 균일 부피", new Vector3(2302, 5, 33), new Vector3(5, 5, 0.15f), 0, ToonFogShape.Box, 3, 0.5f).edgeFeather01 = 0;
        }

        private static void BuildIntegration(Scene scene, Material ground, Material reference, Material clipped)
        {
            MakeShape(PrimitiveType.Cube, "통합 지면", new Vector3(4600, -1.4f, 42), new Vector3(140, 2, 100), ground);
            MakeShape(PrimitiveType.Cube, "근거리 디딤판", new Vector3(4600, -0.75f, 5), new Vector3(30, 1.5f, 30), ground);
            MakeShape(PrimitiveType.Cube, "굴절 물 아래 비교색", new Vector3(4603, -0.03f, 31), new Vector3(8, 0.25f, 7), reference);
            MakeShape(PrimitiveType.Cube, "외곽선 비교 벽", new Vector3(4596, 4, 19), new Vector3(5, 8, 1), ground);
            MakeShape(PrimitiveType.Cube, "외곽선 비교 원경", new Vector3(4602, 7, 140), new Vector3(38, 14, 18), ground);
            MakeShape(PrimitiveType.Quad, "Alpha Clip 비교", new Vector3(4608, 2, 23), new Vector3(5, 4, 1), clipped);
            var character = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Actor/Player/Models/PlayerModel_Raon.prefab");
            if (character != null) InstantiateDisplay(character, scene, new Vector3(4597, 0, 12), Quaternion.Euler(0, 180, 0));
            var tree = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Environment/ReactiveTrees/PF_ReactiveIdyllicFir02.prefab");
            if (tree != null) InstantiateDisplay(tree, scene, new Vector3(4607, 0, 16), Quaternion.identity);
            string path = $"{MaterialPath}/Fog_Lake_Refraction.mat";
            var water = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (water == null)
            {
                water = new Material(AssetDatabase.LoadAssetAtPath<Shader>("Assets/06.Shaders/LakeWater.shader"));
                water.SetFloat("_RefractionStrength", 0.025f);
                AssetDatabase.CreateAsset(water, path);
            }
            MakeShape(PrimitiveType.Plane, "굴절 수면", new Vector3(4603, 0.35f, 34), new Vector3(2.8f, 1, 2.6f), water);
            CreateVolume("수면 경계 안개", new Vector3(4603, 1, 39), new Vector3(20, 1.4f, 16), 0, ToonFogShape.Box, 11, 0.048f);
            var particle = new GameObject("수역 대응 반투명 입자").AddComponent<ParticleSystem>();
            particle.transform.position = new Vector3(4603, 2, 25);
            var main = particle.main;
            main.startSpeed = 1;
            main.startSize = 0.42f;
            var particleMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialPath}/Fog_Water_Particle.mat");
            if (particleMaterial == null)
            {
                particleMaterial = new Material(AssetDatabase.LoadAssetAtPath<Shader>("Assets/06.Shaders/Water/WaterAwareParticle.shader"));
                AssetDatabase.CreateAsset(particleMaterial, $"{MaterialPath}/Fog_Water_Particle.mat");
            }
            particle.GetComponent<ParticleSystemRenderer>().sharedMaterial = particleMaterial;
        }

        private static void BuildRegions(ToonFogProfile[] profiles, Material rock, Material stone)
        {
            MakeShape(PrimitiveType.Cube, "지역 경계 바닥", new Vector3(6900, -1, 85), new Vector3(220, 2, 230), stone);
            for (int i = 0; i < profiles.Length; i++)
            {
                float x = 6834 + 22 * i;
                var region = new GameObject(profiles[i].name).AddComponent<ToonFogRegion>();
                region.transform.position = new Vector3(x, 6, 10);
                region.transform.localScale = new Vector3(11, 10, 42);
                region.priority = i == 3 ? 1 : 0;
                region.stableId = i + 1;
                region.blendDistance = 12;
                region.profile = profiles[i];
                MakeShape(PrimitiveType.Cube, profiles[i].name + " 기둥", new Vector3(x, 5, 58), new Vector3(8, 10, 8), rock);
                CreateVolume(profiles[i].name + " 국소", new Vector3(x, 2, 36), new Vector3(13, 4, 11),
                    i * 13, ToonFogShape.Box, 30 + i, 0.022f).noiseAmount = 0.35f;
            }
        }

        private static ToonFogLocalVolume CreateVolume(string name, Vector3 point, Vector3 scale, float yaw,
            ToonFogShape shape, int id, float density)
        {
            var volume = new GameObject(name).AddComponent<ToonFogLocalVolume>();
            volume.transform.SetPositionAndRotation(point, Quaternion.Euler(0, yaw, 0));
            volume.transform.localScale = scale;
            volume.shape = shape;
            volume.stableId = id;
            volume.density = density;
            return volume;
        }

        private static void InstantiateDisplay(GameObject prefab, Scene scene, Vector3 position, Quaternion rotation)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.SetActive(true);
            instance.transform.SetPositionAndRotation(position, rotation);
            foreach (var behaviour in instance.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
            foreach (var animator in instance.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
        }

        private static Transform MakeView(string name, Vector3 position, Vector3 focus)
        {
            Transform view = new GameObject(name).transform;
            view.SetPositionAndRotation(position, Quaternion.LookRotation(focus - position));
            return view;
        }

        private static GameObject MakeShape(PrimitiveType shape, string name, Vector3 position, Vector3 size, Material material)
        {
            var item = GameObject.CreatePrimitive(shape);
            item.name = name;
            item.transform.position = position;
            item.transform.localScale = size;
            item.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(item.GetComponent<Collider>());
            return item;
        }

        private static Material EnsureSurface(string name, int mode, bool ramp, Color color)
        {
            string path = $"{MaterialPath}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(AssetDatabase.LoadAssetAtPath<Shader>(SurfacePath));
            bool transparent = mode >= 1 && mode <= 3;
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Mode", mode);
            material.SetFloat("_AlphaRamp", ramp ? 1 : 0);
            material.SetFloat("_Src", mode == 1 ? (float)BlendMode.SrcAlpha : (float)BlendMode.One);
            material.SetFloat("_Dst", mode == 3 ? (float)BlendMode.One : transparent ? (float)BlendMode.OneMinusSrcAlpha : (float)BlendMode.Zero);
            material.SetFloat("_DepthWrite", transparent ? 0 : 1);
            material.renderQueue = transparent ? (int)RenderQueue.Transparent : mode == 4 ? (int)RenderQueue.AlphaTest : (int)RenderQueue.Geometry;
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
