using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UPlayGround.Components;
using UPlayGround.Data.World;
using UPlayGround.Gameplay.World;

namespace UPlayGround.EditorTools
{
    /// <summary>사용자 월드를 덮어쓰지 않는 수면·잠수·반잠수·차폐 재현 장면을 저작한다.</summary>
    public static class WaterLabAuthoring
    {
        public const string Folder = "Assets/10.Datas/World/ToonWater";
        public const string ScenePath = "Assets/01.Scenes/Tests/WaterLab.unity";
        public const string DynamicsPath = Folder + "/WaterDynamics.asset";
        private static readonly Vector3 Origin = new(10000f, 0f, 10000f);

        [UPlaygroundTool("Water/CreateLab")]
        public static void CreateLab()
        {
            if (AssetDatabase.LoadMainAssetAtPath(ScenePath) != null)
                throw new InvalidOperationException("기존 WaterLab을 덮어쓰지 않습니다.");
            EnsureFolder(Folder);
            EnsureFolder("Assets/01.Scenes/Tests");
            var dynamics = AssetDatabase.LoadAssetAtPath<WaterDynamicsProfileSO>(DynamicsPath);
            if (dynamics == null)
            {
                dynamics = ScriptableObject.CreateInstance<WaterDynamicsProfileSO>();
                AssetDatabase.CreateAsset(dynamics, DynamicsPath);
            }
            var lakeProfile = CreateProfile("LakeOptics", new Color(0.08f, 0.3f, 0.26f), new Vector3(0.14f, 0.065f, 0.05f));
            var upperProfile = CreateProfile("UpperOptics", new Color(0.09f, 0.22f, 0.35f), new Vector3(0.1f, 0.06f, 0.035f));
            Material water = CreateMaterial("LakeSurface", "UPlayGround/Environment/Lake Water", new Color(0.12f, 0.45f, 0.38f));
            water.SetFloat("_RefractionStrength", 0.008f);
            water.SetFloat("_ReflectionStrength", 0.4f);
            water.SetFloat("_FoamStrength", 0.24f);
            Material bed = CreateMaterial("ProxyBed", "Universal Render Pipeline/Simple Lit", new Color(0.35f, 0.32f, 0.24f));
            Material stone = CreateMaterial("ProxyStone", "Universal Render Pipeline/Simple Lit", new Color(0.4f, 0.43f, 0.4f));
            Material fish = CreateMaterial("ProxyFish", "Universal Render Pipeline/Simple Lit", new Color(0.82f, 0.49f, 0.16f));
            Material leaves = CreateMaterial("ProxyPlants", "Universal Render Pipeline/Simple Lit", new Color(0.17f, 0.32f, 0.13f));
            Material bubbles = CreateMaterial("WaterAwareBubbles", "UPlayGround/Environment/Water Aware Particle", new Color(0.7f, 0.9f, 0.95f, 0.28f));
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.45f, 0.48f, 0.5f);
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.fogStartDistance = 25f;
                RenderSettings.fogEndDistance = 100f;
                RenderSettings.fogColor = new Color(0.65f, 0.72f, 0.75f);
                var sun = new GameObject("WaterLab Sun").AddComponent<Light>();
                sun.type = LightType.Directional; sun.intensity = 1.5f; sun.shadows = LightShadows.Soft;
                sun.transform.rotation = Quaternion.Euler(48f, -30f, 0f);
                RenderSettings.sun = sun;
                CreateProxy("Proxy Lake Bed", PrimitiveType.Cube, new Vector3(0f, -6.3f, 4f), new Vector3(36f, 1f, 28f), bed);
                CreateProxy("Proxy Shallows", PrimitiveType.Cube, new Vector3(0f, -1.4f, -6f), new Vector3(32f, 2f, 4f), bed);
                var lake = CreateWater("Lake", Vector3.zero, new Vector2(32f, 24f), water, lakeProfile, dynamics);
                lake.transform.position = Origin + Vector3.forward * 4f;
                lake.Surface.volumeBounds = new Bounds(new Vector3(0f, -3f, 0f), new Vector3(32f, 6.8f, 24f));
                lake.Surface.dryVolumes = new[] { new Bounds(new Vector3(-8f, -3f, -4f), new Vector3(6f, 5f, 8f)) };
                var upper = CreateWater("Upper", new Vector3(10f, 6f, 14f), new Vector2(10f, 8f), water, upperProfile, dynamics);
                upper.Surface.priority = 1;
                upper.Surface.volumeBounds = new Bounds(new Vector3(0f, -0.8f, 0f), new Vector3(10f, 2.4f, 8f));
                upper.Surface.currentVelocity = new Vector3(-0.4f, 0f, -0.8f);
                CreateProxy("Proxy Upper Bed", PrimitiveType.Cube, new Vector3(10f, 4.65f, 14f), new Vector3(10f, 0.5f, 8f), bed);
                CreateProxy("Proxy Bridge Deck", PrimitiveType.Cube, new Vector3(2f, 1.4f, 4f), new Vector3(19f, 0.45f, 2.4f), stone);
                CreateProxy("Proxy Cave Ceiling", PrimitiveType.Cube, new Vector3(-8f, -0.6f, 0f), new Vector3(6f, 0.3f, 8f), stone);
                CreateProxy("Proxy Cave Wall", PrimitiveType.Cube, new Vector3(-8f, -3f, 2.5f), new Vector3(6f, 5f, 0.3f), stone);
                for (int i = 0; i < 5; i++)
                {
                    var proxy = CreateProxy("Proxy Fish " + i, PrimitiveType.Capsule, new Vector3(-1f + i * 2.4f, -1.6f - i * 0.4f, 3f + i * 1.7f), new Vector3(0.35f, 0.7f, 0.35f), fish);
                    proxy.transform.rotation = Quaternion.Euler(0f, 30f + i * 23f, 90f);
                    CreateProxy("Proxy Rock " + i, PrimitiveType.Sphere, new Vector3(3f + i * 1.8f, -5.5f, 9f - i), new Vector3(1.7f, 1.4f, 1.9f), stone);
                    CreateProxy("Proxy Plant " + i, PrimitiveType.Capsule, new Vector3(-2f + i * 1.4f, -4.8f, 11f + i * 0.3f), new Vector3(0.18f, 1.3f, 0.18f), leaves);
                }
                var bubble = CreateProxy("Proxy WaterAware Bubble", PrimitiveType.Quad, new Vector3(1f, -1f, 1f), new Vector3(0.3f, 0.3f, 0.3f), bubbles);
                UnityEngine.Object.DestroyImmediate(bubble.GetComponent<Collider>());
                CreateCamera("TopDown", new Vector3(0f, 25f, 2f), new Vector3(0f, 0f, 4f));
                CreateCamera("GrazingShore", new Vector3(0f, 1.5f, -9f), new Vector3(0f, -0.3f, 8f));
                CreateCamera("HalfSubmerged", new Vector3(0f, 0.01f, -4f), new Vector3(0f, 0.01f, 9f));
                CreateCamera("UnderLookUp", new Vector3(0f, -2f, -2f), new Vector3(0f, 0.5f, 7f));
                CreateCamera("UnderLookDown", new Vector3(0f, -2f, -2f), new Vector3(2f, -5.5f, 9f));
                CreateCamera("BridgeEdge", new Vector3(0f, 2f, -3f), new Vector3(1f, 0.5f, 9f));
                CreateCamera("TwoElevations", new Vector3(23f, 12f, -15f), new Vector3(5f, 2f, 8f));
                CreateCamera("DryCave", new Vector3(-8f, -3f, -2f), new Vector3(-8f, -3f, 2f));
                CreateWaterfall(lake, upper, bubbles);
                Physics.SyncTransforms();
                BakeExposure(lake, Folder + "/LakeRainExposure.asset", 64);
                BakeExposure(upper, Folder + "/UpperRainExposure.asset", 64);
                lake.rainIntensity = 0.65f;
                upper.rainIntensity = 0.65f;
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.SaveAssetIfDirty(water);
            }
            finally
            {
                SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static UnderwaterRegion CreateWater(string name, Vector3 position, Vector2 size, Material material,
            UnderwaterProfileSO optics, WaterDynamicsProfileSO dynamics)
        {
            var root = new GameObject(name + " Water");
            root.SetActive(false);
            root.transform.position = Origin + position;
            var filter = root.AddComponent<MeshFilter>();
            filter.sharedMesh = CreateGrid(name + "Visual", size, Mathf.CeilToInt(size.x), Mathf.CeilToInt(size.y));
            root.AddComponent<MeshRenderer>().sharedMaterial = material;
            var surface = root.AddComponent<FootstepWaterSurface>();
            surface.surfaceMesh = filter;
            surface.queryMesh = CreateGrid(name + "Domain", size, 1, 1);
            surface.dynamics = dynamics;
            surface.useVolumeBounds = true;
            var region = root.AddComponent<UnderwaterRegion>();
            region.Bind(surface, optics);
            root.SetActive(true);
            return region;
        }

        [UPlaygroundTool("Water/CompleteLabFixtures")]
        public static void CompleteLabFixtures()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Edit Mode에서 실행해야 합니다.");
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                UnderwaterRegion lake = null, upper = null;
                bool hasWaterfall = false;
                foreach (var root in scene.GetRootGameObjects())
                {
                    if (root.TryGetComponent<Camera>(out var camera))
                    {
                        if (camera.name == "DryCave") camera.fieldOfView = 28f;
                        if (camera.name == "WaterfallRear")
                        {
                            camera.transform.position = Origin + new Vector3(10f, 3f, 15f);
                            camera.transform.LookAt(Origin + new Vector3(10f, 2.5f, 8f));
                        }
                    }
                    if (root.TryGetComponent<UnderwaterRegion>(out var region))
                    {
                        if (region.Surface.priority == 0) lake = region;
                        else upper = region;
                    }
                    if (root.TryGetComponent<WaterfallLink>(out var link))
                    {
                        hasWaterfall = true;
                        if (link.mist != null) link.mist.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                    }
                }
                if (!hasWaterfall && lake != null && upper != null)
                    CreateWaterfall(lake, upper, AssetDatabase.LoadAssetAtPath<Material>(Folder + "/WaterAwareBubbles.mat"));
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void CreateWaterfall(UnderwaterRegion lake, UnderwaterRegion upper, Material mistMaterial)
        {
            var root = new GameObject("Proxy Waterfall Link");
            root.transform.position = Origin + new Vector3(10f, 6f, 10f);
            root.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            var mesh = new Mesh { name = "Waterfall Ribbon" };
            const int segments = 20;
            var vertices = new Vector3[(segments + 1) * 2];
            var uv = new Vector2[vertices.Length];
            var colors = new Color[vertices.Length];
            var indices = new int[segments * 6];
            float distance = 0f;
            Vector3 previous = Vector3.zero;
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                Vector3 center = new(0f, -6f * Mathf.SmoothStep(0f, 1f, t), 4f * t);
                if (i > 0) distance += Vector3.Distance(previous, center);
                previous = center;
                float width = Mathf.Lerp(1.5f, 1.8f, t);
                vertices[i * 2] = center - Vector3.right * width;
                vertices[i * 2 + 1] = center + Vector3.right * width;
                uv[i * 2] = new Vector2(0f, distance); uv[i * 2 + 1] = new Vector2(1f, distance);
                colors[i * 2] = colors[i * 2 + 1] = new Color(Mathf.Sin(t * Mathf.PI), 1f, 1f, 1f);
                if (i == segments) continue;
                int offset = i * 6, vertex = i * 2;
                indices[offset] = vertex; indices[offset + 1] = vertex + 2; indices[offset + 2] = vertex + 1;
                indices[offset + 3] = vertex + 1; indices[offset + 4] = vertex + 2; indices[offset + 5] = vertex + 3;
            }
            mesh.vertices = vertices; mesh.uv = uv; mesh.colors = colors; mesh.triangles = indices;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            string meshPath = Folder + "/WaterfallRibbon.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (existing == null) AssetDatabase.CreateAsset(mesh, meshPath);
            else { UnityEngine.Object.DestroyImmediate(mesh); mesh = existing; }
            root.AddComponent<MeshFilter>().sharedMesh = mesh;
            root.AddComponent<MeshRenderer>().sharedMaterial = CreateMaterial("WaterfallRibbon", "UPlayGround/Environment/Toon Waterfall", Color.white);
            var current = root.AddComponent<WaterfallCurrent>();
            var serialized = new SerializedObject(current);
            serialized.FindProperty("_region").boundsValue = new Bounds(new Vector3(0f, -3f, 2f), new Vector3(4f, 8f, 6f));
            serialized.FindProperty("_fallStart").floatValue = 0.4f;
            serialized.FindProperty("_releaseHeight").floatValue = -6.5f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var link = root.AddComponent<WaterfallLink>();
            link.source = upper.Surface; link.target = lake.Surface;
            var impact = new GameObject("Impact"); impact.transform.SetParent(root.transform, false); impact.transform.localPosition = new Vector3(0f, -6f, 4f);
            link.impact = impact.transform;
            var mist = new GameObject("WaterAware Mist"); mist.transform.SetParent(impact.transform, false);
            mist.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            var particles = mist.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.playOnAwake = true; main.maxParticles = 96; main.startLifetime = 1.4f; main.startSpeed = 0.7f;
            main.startSize = 0.3f; main.startColor = new Color(0.7f, 0.88f, 0.9f, 0.16f);
            var emission = particles.emission; emission.rateOverTime = 24f;
            var shape = particles.shape; shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 65f; shape.radius = 1.2f;
            particles.GetComponent<ParticleSystemRenderer>().sharedMaterial = mistMaterial;
            link.mist = particles;
            CreateCamera("WaterfallRear", new Vector3(10f, 3f, 15f), new Vector3(10f, 2.5f, 8f));
        }

        private static Mesh CreateGrid(string name, Vector2 size, int columns, int rows)
        {
            string path = Folder + "/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) return existing;
            var vertices = new Vector3[(columns + 1) * (rows + 1)];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[columns * rows * 6];
            for (int z = 0; z <= rows; z++)
            for (int x = 0; x <= columns; x++)
            {
                int i = z * (columns + 1) + x;
                uv[i] = new Vector2((float)x / columns, (float)z / rows);
                vertices[i] = new Vector3((uv[i].x - 0.5f) * size.x, 0f, (uv[i].y - 0.5f) * size.y);
            }
            for (int z = 0; z < rows; z++)
            for (int x = 0; x < columns; x++)
            {
                int vertex = z * (columns + 1) + x;
                int index = (z * columns + x) * 6;
                triangles[index] = vertex; triangles[index + 1] = vertex + columns + 1; triangles[index + 2] = vertex + 1;
                triangles[index + 3] = vertex + 1; triangles[index + 4] = vertex + columns + 1; triangles[index + 5] = vertex + columns + 2;
            }
            var mesh = new Mesh { name = name, vertices = vertices, uv = uv, triangles = triangles };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static UnderwaterProfileSO CreateProfile(string name, Color scattering, Vector3 extinction)
        {
            string path = Folder + "/" + name + ".asset";
            var result = AssetDatabase.LoadAssetAtPath<UnderwaterProfileSO>(path);
            if (result != null) return result;
            result = ScriptableObject.CreateInstance<UnderwaterProfileSO>();
            result.scatteringColor = scattering; result.extinction = extinction;
            AssetDatabase.CreateAsset(result, path);
            return result;
        }

        private static Material CreateMaterial(string name, string shaderName, Color color)
        {
            string path = Folder + "/" + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            Shader shader = Shader.Find(shaderName);
            if (shader == null) throw new InvalidOperationException(shaderName + " 셰이더가 없습니다.");
            var material = new Material(shader) { name = name };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static GameObject CreateProxy(string name, PrimitiveType primitive, Vector3 position, Vector3 scale, Material material)
        {
            var result = GameObject.CreatePrimitive(primitive);
            result.name = name; result.transform.position = Origin + position; result.transform.localScale = scale;
            result.GetComponent<Renderer>().sharedMaterial = material;
            return result;
        }

        private static void CreateCamera(string name, Vector3 position, Vector3 target)
        {
            var camera = new GameObject(name).AddComponent<Camera>();
            camera.transform.position = Origin + position;
            camera.transform.LookAt(Origin + target);
            camera.fieldOfView = name == "DryCave" ? 28f : 60f;
            camera.nearClipPlane = 0.3f; camera.farClipPlane = 160f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.35f, 0.43f, 0.49f);
            camera.allowHDR = true; camera.enabled = false;
            var data = camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            data.requiresDepthTexture = true; data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
        }

        [UPlaygroundTool("Water/BakeSelectedExposure")]
        public static void BakeSelectedExposure()
        {
            var selected = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponent<UnderwaterRegion>() : null;
            if (selected == null) throw new InvalidOperationException("UnderwaterRegion을 선택해야 합니다.");
            string path = EditorUtility.SaveFilePanelInProject("강수 노출 마스크", "WaterRainExposure", "asset", "새 노출 마스크 경로");
            if (string.IsNullOrEmpty(path)) return;
            if (AssetDatabase.LoadMainAssetAtPath(path) != null) throw new InvalidOperationException("기존 마스크는 덮어쓰지 않습니다.");
            Undo.RecordObject(selected, "수면 강수 노출 연결");
            BakeExposure(selected, path, 128);
            EditorUtility.SetDirty(selected);
        }

        public static void BakeExposure(UnderwaterRegion region, string path, int resolution)
        {
            region.RefreshBinding();
            Bounds bounds = region.SurfaceRenderer.bounds;
            var pixels = new byte[resolution * resolution];
            Physics.SyncTransforms();
            for (int y = 0; y < resolution; y++)
            for (int x = 0; x < resolution; x++)
            {
                var position = new Vector3(Mathf.Lerp(bounds.min.x, bounds.max.x, (x + 0.5f) / resolution), bounds.center.y,
                    Mathf.Lerp(bounds.min.z, bounds.max.z, (y + 0.5f) / resolution));
                bool onWater = region.Surface.TrySampleHeight(position, out var water);
                bool sheltered = !onWater || region.IsRainSheltered(water)
                    || Physics.Raycast(water + Vector3.up * 0.05f, Vector3.up, 40f, ~0, QueryTriggerInteraction.Ignore);
                byte exposure = sheltered ? (byte)0 : (byte)255;
                pixels[y * resolution + x] = exposure;
            }
            var texture = new Texture2D(resolution, resolution, TextureFormat.R8, false, true)
            {
                name = System.IO.Path.GetFileNameWithoutExtension(path), filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixelData(pixels, 0); texture.Apply(false, false);
            AssetDatabase.CreateAsset(texture, path);
            region.rainExposure = texture;
        }

        internal static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = System.IO.Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(folder));
        }
    }
}
