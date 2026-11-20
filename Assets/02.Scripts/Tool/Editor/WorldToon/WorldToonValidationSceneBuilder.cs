using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UPlayGround.Data.Rendering;
using UPlayGround.EditorTools;

namespace UPlayGround.Tool.Editor.WorldToon
{
    /// <summary>
    /// WorldToonValidation 씬을 고정 배치로 다시 만든다(AI 검증용, 툴 런처 비등록).
    /// 게임 씬은 수정하지 않으며 현재 열린 씬 위에 추가로 열었다가 저장 후 닫는다.
    /// 구역: A 재질 비교(원본|WorldToon), B 추가 광원, C 식생 카드, D Terrain 2/4/5 레이어, E 보호·미지원 입력.
    /// </summary>
    public static class WorldToonValidationSceneBuilder
    {
        public const string ScenePath = "Assets/01.Scenes/ArtReview/WorldToonValidation.unity";
        public const string StylePath = "Assets/10.Datas/Environment/WorldToon/WorldToonStyle_Default.asset";
        public const string ValidationAssetRoot = "Assets/11.Material/WorldToon/Validation";
        public const string CaptureRoot = "docs/work-status/worldtoon/captures";
        private const string ToolId = "UPlayGround/검증/WorldToon/검증 씬 재생성";

        private static readonly string[] RockModels =
        {
            "Assets/08.Models/PoorVillage/Terrain_Boulder_A.fbx",
            "Assets/08.Models/PoorVillage/Terrain_Boulder_B.fbx",
            "Assets/08.Models/PoorVillage/Terrain_RockCluster.fbx",
        };

        // 씬이 게임 월드와 겹쳐 보이지 않도록 먼 곳에 둔다.
        private static readonly Vector3 Origin = new Vector3(20000f, 0f, 20000f);

        [UPlaygroundTool(ToolId)]
        private static void BuildFromCatalog()
        {
            Debug.Log(Build());
        }

        /// <summary>스타일 프로파일을 보장하고 검증 씬을 다시 만든 뒤 보고문을 반환한다.</summary>
        public static string Build()
        {
            var report = new StringBuilder();
            WorldToonStyleProfileSO style = EnsureStyleProfile();
            var record = WorldToonConversionRecord.Load();

            Scene previousActive = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                BuildLighting(scene);
                BuildZoneA(scene, style, record, report);
                BuildZoneB(scene, style, record, report);
                BuildZoneC(scene, style, report);
                BuildZoneD(scene, style, report);
                BuildZoneE(scene, report);
                BuildCameras(scene);
                Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
                EditorSceneManager.SaveScene(scene, ScenePath);
                record.Save();
            }
            finally
            {
                if (previousActive.IsValid())
                    SceneManager.SetActiveScene(previousActive);
                EditorSceneManager.CloseScene(scene, true);
            }
            report.Insert(0, $"씬 저장: {ScenePath}\n");
            return report.ToString();
        }

        private static WorldToonStyleProfileSO EnsureStyleProfile()
        {
            var style = AssetDatabase.LoadAssetAtPath<WorldToonStyleProfileSO>(StylePath);
            if (style != null)
                return style;
            Directory.CreateDirectory(Path.GetDirectoryName(StylePath));
            style = ScriptableObject.CreateInstance<WorldToonStyleProfileSO>();
            AssetDatabase.CreateAsset(style, StylePath);
            // 전체 SaveAssets는 무관한 에셋(자동 업그레이드된 RP 등)까지 저장하므로 자기 에셋만 저장한다.
            AssetDatabase.SaveAssetIfDirty(style);
            return style;
        }

        private static void BuildLighting(Scene scene)
        {
            var sun = new GameObject("Sun");
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.6f;
            light.color = new Color(1f, 0.96f, 0.9f);
            light.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(42f, -35f, 0f);
            RenderSettings.sun = light;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.62f, 0.75f);
            RenderSettings.ambientEquatorColor = new Color(0.45f, 0.45f, 0.42f);
            RenderSettings.ambientGroundColor = new Color(0.25f, 0.22f, 0.2f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.62f, 0.68f, 0.76f);
            RenderSettings.fogStartDistance = 60f;
            RenderSettings.fogEndDistance = 220f;

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground_URPLit";
            ground.transform.position = Origin;
            ground.transform.localScale = new Vector3(14f, 1f, 14f);
            ground.GetComponent<Renderer>().sharedMaterial = EnsureGroundMaterial();
            Place(sun, scene);
            Place(ground, scene);
        }

        private static Material EnsureGroundMaterial()
        {
            string path = $"{ValidationAssetRoot}/WT_Validation_Ground.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
                return material;
            Directory.CreateDirectory(ValidationAssetRoot);
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", new Color(0.42f, 0.45f, 0.38f));
            material.SetFloat("_Smoothness", 0.1f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        // A: 왼쪽 원본(URP/Lit), 오른쪽 WorldToon MeshLite. 같은 메시·같은 조명.
        private static void BuildZoneA(Scene scene, WorldToonStyleProfileSO style, WorldToonConversionRecord record, StringBuilder report)
        {
            var zone = new GameObject("A_MaterialCompare");
            Place(zone, scene);
            for (int i = 0; i < RockModels.Length; i++)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(RockModels[i]);
                if (model == null)
                {
                    report.AppendLine($"A: 모델 없음 {RockModels[i]}");
                    continue;
                }
                Vector3 row = Origin + new Vector3(-12f + i * 0f, 0f, -6f + i * 7f);
                var original = (GameObject)PrefabUtility.InstantiatePrefab(model, scene);
                original.name = model.name + "_Original";
                original.transform.SetParent(zone.transform);
                original.transform.position = row;

                var toon = (GameObject)PrefabUtility.InstantiatePrefab(model, scene);
                toon.name = model.name + "_WorldToonLite";
                toon.transform.SetParent(zone.transform);
                toon.transform.position = row + new Vector3(8f, 0f, 0f);
                report.AppendLine("A: " + ConvertRenderers(toon, WorldToonPreset.MeshLite, style, record));
            }
            AddPrimitivePair(zone, scene, PrimitiveType.Sphere, Origin + new Vector3(-12f, 1f, 16f), style, record, report);
            AddPrimitivePair(zone, scene, PrimitiveType.Cube, Origin + new Vector3(-12f, 0.75f, 22f), style, record, report);
            AddDecal(zone, Origin + new Vector3(-4f, 1.5f, -6f), report);
        }

        private static void AddPrimitivePair(GameObject zone, Scene scene, PrimitiveType type, Vector3 position, WorldToonStyleProfileSO style, WorldToonConversionRecord record, StringBuilder report)
        {
            Material source = EnsureColoredLit(type.ToString(), type == PrimitiveType.Sphere ? new Color(0.75f, 0.32f, 0.28f) : new Color(0.3f, 0.45f, 0.7f));
            var original = GameObject.CreatePrimitive(type);
            original.name = type + "_Original";
            original.transform.SetParent(zone.transform);
            original.transform.position = position;
            original.transform.localScale = Vector3.one * 1.8f;
            original.GetComponent<Renderer>().sharedMaterial = source;

            var toon = UnityEngine.Object.Instantiate(original, zone.transform);
            toon.name = type + "_WorldToonLite";
            toon.transform.position = position + new Vector3(8f, 0f, 0f);
            report.AppendLine("A: " + ConvertRenderers(toon, WorldToonPreset.MeshLite, style, record));
        }

        private static Material EnsureColoredLit(string name, Color color)
        {
            string path = $"{ValidationAssetRoot}/WT_Validation_{name}_Lit.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
                return material;
            Directory.CreateDirectory(ValidationAssetRoot);
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.2f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        // 스킬 바닥 표시처럼 DBuffer 데칼이 WorldToon 표면에 합성되는지 보는 투영기(T22).
        private static void AddDecal(GameObject zone, Vector3 position, StringBuilder report)
        {
            Shader decalShader = Shader.Find("Shader Graphs/Decal");
            if (decalShader == null)
            {
                report.AppendLine("A: Decal 셰이더 없음 — T22 NOT_RUN");
                return;
            }
            string path = $"{ValidationAssetRoot}/WT_Validation_Decal.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(decalShader);
                AssetDatabase.CreateAsset(material, path);
            }
            // 흰 바위 위에서도 구분되도록 붉은 줄무늬 텍스처를 쓴다(URP Decal 그래프의 색 입력은 Base_Map).
            material.SetTexture("Base_Map", EnsureDecalTexture());
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
            var decal = new GameObject("Decal_OverRocks");
            decal.transform.SetParent(zone.transform);
            decal.transform.position = position;
            decal.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var projector = decal.AddComponent<DecalProjector>();
            projector.material = material;
            projector.size = new Vector3(14f, 3f, 6f);
        }

        private static Texture2D EnsureDecalTexture()
        {
            string path = $"{ValidationAssetRoot}/WT_Validation_DecalStripes.png";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture != null)
                return texture;
            const int size = 32;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    pixels[y * size + x] = ((x / 4) % 2 == 0) ? new Color32(230, 30, 20, 255) : new Color32(250, 210, 40, 255);
            var generated = new Texture2D(size, size, TextureFormat.RGBA32, false);
            generated.SetPixels32(pixels);
            File.WriteAllBytes(path, generated.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(generated);
            AssetDatabase.ImportAsset(path);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // B: 밤 조건의 Point/Spot. Lite(추가 광원 끔)와 Standard(켬)의 차이를 본다.
        private static void BuildZoneB(Scene scene, WorldToonStyleProfileSO style, WorldToonConversionRecord record, StringBuilder report)
        {
            var zone = new GameObject("B_AdditionalLights");
            Place(zone, scene);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(RockModels[0]);
            WorldToonPreset[] presets = { WorldToonPreset.MeshLite, WorldToonPreset.MeshStandard };
            for (int i = 0; i < presets.Length; i++)
            {
                var rock = (GameObject)PrefabUtility.InstantiatePrefab(model, scene);
                rock.name = $"Boulder_{presets[i]}";
                rock.transform.SetParent(zone.transform);
                rock.transform.position = Origin + new Vector3(16f + i * 8f, 0f, -6f);
                report.AppendLine("B: " + ConvertRenderers(rock, presets[i], style, record));

                var torch = new GameObject($"Torch_{presets[i]}");
                torch.transform.SetParent(zone.transform);
                torch.transform.position = rock.transform.position + new Vector3(-1.5f, 2.2f, -2.2f);
                var light = torch.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1f, 0.55f, 0.2f);
                light.intensity = 6f;
                light.range = 6f;
            }
            var spot = new GameObject("Spot_Standard");
            spot.transform.SetParent(zone.transform);
            spot.transform.position = Origin + new Vector3(24f, 6f, -10f);
            spot.transform.LookAt(Origin + new Vector3(24f, 0f, -6f));
            var spotLight = spot.AddComponent<Light>();
            spotLight.type = LightType.Spot;
            spotLight.color = new Color(0.4f, 0.6f, 1f);
            spotLight.intensity = 25f;
            spotLight.range = 14f;
            spotLight.spotAngle = 40f;
        }

        // C: 정점 색 R(뿌리 0→끝 1)을 가진 절차 카드 묶음. 기존 식생 메시는 Read/Write가 꺼져 마스크 확인이 불가하다.
        private static void BuildZoneC(Scene scene, WorldToonStyleProfileSO style, StringBuilder report)
        {
            var zone = new GameObject("C_Foliage");
            Place(zone, scene);
            Mesh mesh = EnsureGrassCardMesh();
            Material near = EnsureFoliageMaterial("FoliageNear", style, true);
            Material far = EnsureFoliageMaterial("FoliageFar", style, false);
            var random = new System.Random(1234);
            for (int i = 0; i < 160; i++)
            {
                var card = new GameObject($"GrassCard_{i:000}");
                card.transform.SetParent(zone.transform);
                float x = (float)random.NextDouble() * 10f;
                float z = (float)random.NextDouble() * 6f;
                card.transform.position = Origin + new Vector3(-12f + x, 0f, -16f + z);
                card.transform.rotation = Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);
                card.transform.localScale = Vector3.one * (0.7f + (float)random.NextDouble() * 0.6f);
                card.AddComponent<MeshFilter>().sharedMesh = mesh;
                card.AddComponent<MeshRenderer>().sharedMaterial = i % 2 == 0 ? near : far;
            }
            List<string> warnings = WorldToonFoliageValidator.Validate(zone.transform.GetChild(0).GetComponent<Renderer>(), near.GetFloat("_WindAmplitude"));
            report.AppendLine("C: 카드 160장(짝수 Near/홀수 Far), 검사 경고=" + (warnings.Count == 0 ? "없음" : string.Join(" / ", warnings)));
        }

        private static Mesh EnsureGrassCardMesh()
        {
            string path = $"{ValidationAssetRoot}/WT_Validation_GrassCard.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null)
                return mesh;
            Directory.CreateDirectory(ValidationAssetRoot);
            mesh = new Mesh { name = "WT_Validation_GrassCard" };
            // 교차 2장, 높이 0.8m. 정점 색 R=높이 비율, B=개체 위상.
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var colors = new List<Color>();
            var triangles = new List<int>();
            for (int plane = 0; plane < 2; plane++)
            {
                Quaternion rotation = Quaternion.Euler(0f, plane * 90f, 0f);
                int start = vertices.Count;
                for (int row = 0; row <= 2; row++)
                {
                    float v = row / 2f;
                    vertices.Add(rotation * new Vector3(-0.3f, v * 0.8f, 0f));
                    vertices.Add(rotation * new Vector3(0.3f, v * 0.8f, 0f));
                    uvs.Add(new Vector2(0f, v));
                    uvs.Add(new Vector2(1f, v));
                    colors.Add(new Color(v * v, 0f, plane * 0.5f, 0f));
                    colors.Add(new Color(v * v, 0f, plane * 0.5f, 0f));
                }
                for (int row = 0; row < 2; row++)
                {
                    int i = start + row * 2;
                    triangles.AddRange(new[] { i, i + 2, i + 1, i + 1, i + 2, i + 3 });
                }
            }
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static Material EnsureFoliageMaterial(string presetName, WorldToonStyleProfileSO style, bool wind)
        {
            string path = $"{ValidationAssetRoot}/WT_Validation_{presetName}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find(WorldToonVariantManifest.FoliageShader);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetTexture("_BaseMap", EnsureGrassTexture());
            material.SetColor("_BaseColor", new Color(0.45f, 0.7f, 0.3f));
            material.SetFloat("_WTWind", wind ? 1f : 0f);
            material.SetFloat("_ReceiveShadows", wind ? 1f : 0f);
            material.SetFloat("_AlphaClip", 1f);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
            WorldToonMaterialConverter.ApplyStyle(material, style);
            var keywords = new List<UnityEngine.Rendering.LocalKeyword> { new UnityEngine.Rendering.LocalKeyword(shader, WorldToonVariantManifest.AlphaTestKeyword) };
            keywords.Add(new UnityEngine.Rendering.LocalKeyword(shader, wind ? WorldToonVariantManifest.WindKeyword : WorldToonVariantManifest.ReceiveShadowsOffKeyword));
            material.enabledKeywords = keywords.ToArray();
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
            return material;
        }

        // 위로 갈수록 좁아지는 잎 실루엣 알파 텍스처.
        private static Texture2D EnsureGrassTexture()
        {
            string path = $"{ValidationAssetRoot}/WT_Validation_GrassAlpha.png";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture != null)
                return texture;
            const int size = 64;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (size - 1f) * 2f - 1f;
                    float v = y / (size - 1f);
                    float blade = Mathf.Abs(Mathf.Repeat(u * 2.5f, 1f) * 2f - 1f);
                    bool inside = blade < (1f - v) * 0.9f;
                    byte shade = (byte)(150 + v * 105);
                    pixels[y * size + x] = new Color32(shade, shade, shade, inside ? (byte)255 : (byte)0);
                }
            }
            var generated = new Texture2D(size, size, TextureFormat.RGBA32, false);
            generated.SetPixels32(pixels);
            File.WriteAllBytes(path, generated.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(generated);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.mipMapsPreserveCoverage = true;
            importer.alphaTestReferenceValue = 0.5f;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // D: 기존 TerrainLayer를 참조만 하는 검증 전용 TerrainData(2/4/5 레이어, 4레이어에 구멍).
        private static void BuildZoneD(Scene scene, WorldToonStyleProfileSO style, StringBuilder report)
        {
            TerrainLayer[] layers = FindTerrainLayers(5);
            if (layers.Length < 5)
            {
                report.AppendLine($"D: 재사용 가능한 TerrainLayer {layers.Length}개뿐 — 일부 Terrain 검증 NOT_RUN");
            }
            int[] counts = { 2, 4, 5 };
            for (int i = 0; i < counts.Length; i++)
            {
                if (layers.Length < counts[i])
                    continue;
                TerrainData data = EnsureTerrainData(counts[i], layers, i == 1);
                GameObject terrainObject = Terrain.CreateTerrainGameObject(data);
                terrainObject.name = $"Terrain_{counts[i]}Layers";
                terrainObject.transform.position = Origin + new Vector3(40f + i * 34f, 0f, -20f);
                Place(terrainObject, scene);
                var terrain = terrainObject.GetComponent<Terrain>();
                terrain.materialTemplate = AssetDatabase.LoadAssetAtPath<Material>(EnsureUrpTerrainBaseline());
                terrain.basemapDistance = 40f;
                string result = WorldToonTerrainValidator.Apply(terrain, style, $"{ValidationAssetRoot}", out _);
                report.AppendLine($"D: {terrainObject.name} → {result}");
            }
        }

        private static string EnsureUrpTerrainBaseline()
        {
            string path = $"{ValidationAssetRoot}/WT_Validation_TerrainLit.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) == null)
                AssetDatabase.CreateAsset(new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit")), path);
            return path;
        }

        private static TerrainLayer[] FindTerrainLayers(int count)
        {
            var result = new List<TerrainLayer>();
            foreach (string guid in AssetDatabase.FindAssets("t:TerrainLayer", new[] { "Assets" }))
            {
                var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(AssetDatabase.GUIDToAssetPath(guid));
                if (layer == null || layer.diffuseTexture == null || layer.normalMapTexture != null || layer.maskMapTexture != null)
                    continue;
                result.Add(layer);
                if (result.Count == count)
                    break;
            }
            return result.ToArray();
        }

        private static TerrainData EnsureTerrainData(int layerCount, TerrainLayer[] layers, bool withHoles)
        {
            string path = $"{ValidationAssetRoot}/WT_Validation_Terrain{layerCount}.asset";
            var data = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
            if (data == null)
            {
                data = new TerrainData { heightmapResolution = 65, alphamapResolution = 64 };
                data.size = new Vector3(30f, 6f, 30f);
                AssetDatabase.CreateAsset(data, path);
            }
            var used = new TerrainLayer[layerCount];
            System.Array.Copy(layers, used, layerCount);
            data.terrainLayers = used;

            var heights = new float[65, 65];
            for (int y = 0; y < 65; y++)
                for (int x = 0; x < 65; x++)
                    heights[y, x] = 0.25f + 0.2f * Mathf.Sin(x * 0.18f) * Mathf.Cos(y * 0.14f);
            data.SetHeights(0, 0, heights);

            // 레이어를 띠 모양으로 칠해 레이어 경계와 혼합을 본다.
            var alphas = new float[64, 64, layerCount];
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    float band = x / 64f * layerCount;
                    int index = Mathf.Min(layerCount - 1, Mathf.FloorToInt(band));
                    float blend = Mathf.Clamp01((band - index) * 4f - 3f);
                    alphas[y, x, index] = 1f - blend;
                    if (index + 1 < layerCount)
                        alphas[y, x, index + 1] = blend;
                }
            }
            data.SetAlphamaps(0, 0, alphas);

            if (withHoles)
            {
                int resolution = data.holesResolution;
                var holes = new bool[resolution, resolution];
                for (int y = 0; y < resolution; y++)
                    for (int x = 0; x < resolution; x++)
                        holes[y, x] = (new Vector2(x, y) - new Vector2(resolution * 0.5f, resolution * 0.5f)).magnitude > resolution * 0.12f;
                data.SetHoles(0, 0, holes);
            }
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssetIfDirty(data);
            return data;
        }

        // E: 보호(lilToon)와 미지원(Transparent) 입력이 변환에서 막히는지 보고만 한다.
        private static void BuildZoneE(Scene scene, StringBuilder report)
        {
            Shader lilToon = Shader.Find("lilToon");
            if (lilToon != null)
            {
                var protectedMaterial = new Material(lilToon);
                report.AppendLine("E: lilToon → " + WorldToonMaterialAnalyzer.Analyze(protectedMaterial).Status);
                UnityEngine.Object.DestroyImmediate(protectedMaterial);
            }
            var transparent = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            transparent.SetFloat("_Surface", 1f);
            report.AppendLine("E: URP Transparent → " + WorldToonMaterialAnalyzer.Analyze(transparent).Status);
            UnityEngine.Object.DestroyImmediate(transparent);
        }

        private static void BuildCameras(Scene scene)
        {
            AddCaptureCamera(scene, "Cam_A_Compare", Origin + new Vector3(-6f, 6f, -22f), Origin + new Vector3(-6f, 1f, 4f));
            // 해를 마주 보는 방향: 어두운 면(Shade Tint)과 명암 경계를 본다.
            AddCaptureCamera(scene, "Cam_A_ShadeSide", Origin + new Vector3(-6f, 5f, 34f), Origin + new Vector3(-6f, 1f, 4f));
            AddCaptureCamera(scene, "Cam_B_Lights", Origin + new Vector3(20f, 4f, -18f), Origin + new Vector3(20f, 1f, -6f));
            AddCaptureCamera(scene, "Cam_C_Foliage", Origin + new Vector3(-7f, 1.2f, -21f), Origin + new Vector3(-7f, 0.4f, -13f));
            AddCaptureCamera(scene, "Cam_D_Terrain", Origin + new Vector3(70f, 30f, -55f), Origin + new Vector3(80f, 0f, -5f));
        }

        private static void AddCaptureCamera(Scene scene, string name, Vector3 position, Vector3 target)
        {
            var cameraObject = new GameObject(name);
            cameraObject.transform.position = position;
            cameraObject.transform.LookAt(target);
            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 50f;
            camera.farClipPlane = 400f;
            camera.enabled = false;
            var data = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.None;
            Place(cameraObject, scene);
        }

        /// <summary>고정 카메라마다 PNG를 남긴다. 씬을 잠시 추가로 열고 활성화했다가 원래 활성 씬으로 되돌린다.</summary>
        public static string Capture(string suffix, bool night)
        {
            var report = new StringBuilder();
            Scene previousActive = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                Light sun = RenderSettings.sun;
                if (night && sun != null)
                {
                    sun.intensity = 0.05f;
                    sun.color = new Color(0.4f, 0.5f, 0.8f);
                    RenderSettings.ambientSkyColor *= 0.25f;
                    RenderSettings.ambientEquatorColor *= 0.25f;
                    RenderSettings.ambientGroundColor *= 0.25f;
                }
                Directory.CreateDirectory(CaptureRoot);
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    var camera = root.GetComponent<Camera>();
                    if (camera == null)
                        continue;
                    string file = $"{CaptureRoot}/{root.name}_{suffix}.png";
                    RenderToFile(camera, file);
                    report.AppendLine(file);
                }
            }
            finally
            {
                if (previousActive.IsValid())
                    SceneManager.SetActiveScene(previousActive);
                EditorSceneManager.CloseScene(scene, true);
            }
            return report.ToString();
        }

        private static void RenderToFile(Camera camera, string file)
        {
            var target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32) { antiAliasing = 1 };
            RenderTexture previous = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                image.Apply();
                File.WriteAllBytes(file, image.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(image);
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static string ConvertRenderers(GameObject root, WorldToonPreset preset, WorldToonStyleProfileSO style, WorldToonConversionRecord record)
        {
            var summary = new StringBuilder(root.name).Append(':');
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
            {
                Material[] materials = renderer.sharedMaterials;
                for (int slot = 0; slot < materials.Length; slot++)
                {
                    WorldToonMaterialAnalysis analysis = WorldToonMaterialAnalyzer.Analyze(materials[slot], renderer);
                    WorldToonMaterialConverter.CopyResult copy = WorldToonMaterialConverter.CreateCopy(analysis, preset, style, record);
                    summary.Append($" [{slot}] {analysis.Status} {copy.Message}");
                    if (copy.Material == null || copy.IsConflict)
                        continue;
                    materials[slot] = copy.Material;
                }
                // 검증 씬 인스턴스만 바꾼다. 원본 모델·프리팹은 수정하지 않는다.
                renderer.sharedMaterials = materials;
            }
            return summary.ToString();
        }

        private static void Place(GameObject gameObject, Scene scene)
        {
            if (gameObject.transform.parent == null && gameObject.scene != scene)
                SceneManager.MoveGameObjectToScene(gameObject, scene);
        }
    }
}
