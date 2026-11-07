using System;
using System.Collections.Generic;
using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UPlayGround.EditorTools;
using UPlayGround.Gameplay.World;

namespace UPlayGround.Tool.Editor.Map
{
    /// <summary>생명의 호수 주변의 급경사와 환경 미술을 보정하고 접지 상태를 기록한다.</summary>
    public static partial class LakeVillageWorldPolishTool
    {
        private const string ScenePath = "Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity";
        private const string ReportDirectory = "output/OpenWorld/LakeVillagePolish";
        private const string LakeRootPath = "AuthoredEnvironment/월드 구간/호수와 사당";
        private const string LakeWaterPath = "Assets/10.Datas/World/LakeVillage/LakeVillageWorld_Water.mat";
        private const string HarborWaterPath = "Assets/10.Datas/World/OpenWorld/HarborNaturalWater.mat";
        private const string SharedRenderAssetPath = "Assets/Settings/PC_RPAsset.asset";
        private const string SceneRenderAssetPath = "Assets/Settings/LakeVillage_RPAsset.asset";
        private const float SceneShadowDistance = 80f;
        private const int SceneShadowCascadeCount = 3;
        private const float SceneAtmosphereThickness = 1f;
        private const string SilvergrassPath = "Assets/ExternalAssets/Environment/PolyartStudio/DreamscapeEastLands/Prefabs/Foliage/Plants/PF_EastLands_AmurSilvergrass_02.prefab";

        [UPlaygroundTool("OpenWorld/호수 마을 환경 미술 보정")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Edit Mode에서만 실행할 수 있습니다.");

            Directory.CreateDirectory(ReportDirectory);
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Terrain terrain = Terrain.activeTerrain;
            if (terrain == null)
                throw new InvalidOperationException("LakeVillageWorld Terrain을 찾지 못했습니다.");

            CreateBackup();
            int terrainSamples = SoftenLakeTerraces(terrain);
            int groundedProps = CorrectClearContactFailures(terrain);
            int reeds = AddShoreReeds(terrain);
            int disabledAprons = DisableInvalidShoreApron();
            ConfigureWaterMaterials();
            ConfigureLighting();
            BakeNavigation();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            File.WriteAllText(
                ReportDirectory + "/application.txt",
                "지형 완화 연산=" + terrainSamples + " / 접지 보정=" + groundedProps + " / 갈대=" + reeds + " / 비정상 호안 메쉬 비활성화=" + disabledAprons + "\n" + BuildGroundingAudit(terrain));
        }

        private static void CreateBackup()
        {
            string backupDirectory = ReportDirectory + "/backup";
            Directory.CreateDirectory(backupDirectory);
            CopyIfMissing(ScenePath, backupDirectory + "/LakeVillageWorld.before-polish.unity");
            string terrainPath = AssetDatabase.GetAssetPath(Terrain.activeTerrain.terrainData);
            CopyIfMissing(terrainPath, backupDirectory + "/Terrain.before-polish.asset");
        }

        private static void CopyIfMissing(string sourcePath, string destinationPath)
        {
            if (File.Exists(destinationPath))
                return;

            File.Copy(sourcePath, destinationPath);
        }

        private static int SoftenLakeTerraces(Terrain terrain)
        {
            TerrainData data = terrain.terrainData;
            Vector3 origin = terrain.transform.position;
            float sampleX = data.size.x / (data.heightmapResolution - 1);
            float sampleZ = data.size.z / (data.heightmapResolution - 1);
            // 중심 보정 범위 바깥으로 20m를 겹쳐 완화한다. 경계선을 정확히 잘라
            // 처리하면 인접 표본 사이에 새 절개 단차가 생길 수 있다.
            const float minX = 880f;
            const float maxX = 1260f;
            const float minZ = 560f;
            const float maxZ = 1120f;
            int x0 = Mathf.Clamp(Mathf.FloorToInt((minX - origin.x) / sampleX), 0, data.heightmapResolution - 1);
            int z0 = Mathf.Clamp(Mathf.FloorToInt((minZ - origin.z) / sampleZ), 0, data.heightmapResolution - 1);
            int x1 = Mathf.Clamp(Mathf.CeilToInt((maxX - origin.x) / sampleX), 0, data.heightmapResolution - 1);
            int z1 = Mathf.Clamp(Mathf.CeilToInt((maxZ - origin.z) / sampleZ), 0, data.heightmapResolution - 1);
            int width = x1 - x0 + 1;
            int height = z1 - z0 + 1;
            float[,] heights = data.GetHeights(x0, z0, width, height);
            int changed = 0;
            float maximumHorizontalStep = Mathf.Tan(26f * Mathf.Deg2Rad) * sampleX / data.size.y;
            float maximumVerticalStep = Mathf.Tan(26f * Mathf.Deg2Rad) * sampleZ / data.size.y;

            // 벽처럼 끊긴 인접 높이만 양쪽으로 분산해, 기존의 지형 덩어리는 유지하면서
            // 비현실적인 절개 단차를 플레이 가능한 완만한 경사로 되돌린다.
            for (int pass = 0; pass < 36; pass++)
            {
                for (int z = 1; z < height - 1; z++)
                {
                    for (int x = 1; x < width; x++)
                    {
                        changed += RelaxHeightPair(heights, z, x - 1, z, x, maximumHorizontalStep);
                    }
                }

                for (int z = 1; z < height; z++)
                {
                    for (int x = 1; x < width - 1; x++)
                        changed += RelaxHeightPair(heights, z - 1, x, z, x, maximumVerticalStep);
                }
            }

            data.SetHeights(x0, z0, heights);
            terrain.Flush();
            EditorUtility.SetDirty(data);
            return changed;
        }

        private static int RelaxHeightPair(float[,] heights, int firstZ, int firstX, int secondZ, int secondX, float maximumStep)
        {
            float delta = heights[secondZ, secondX] - heights[firstZ, firstX];
            float excess = Mathf.Abs(delta) - maximumStep;
            if (excess <= 0f)
                return 0;

            float correction = excess * 0.5f * Mathf.Sign(delta);
            heights[secondZ, secondX] -= correction;
            heights[firstZ, firstX] += correction;
            return 1;
        }

        private static int CorrectClearContactFailures(Terrain terrain)
        {
            int grounded = 0;
            Transform world = GameObject.Find("AuthoredEnvironment/월드 구간").transform;
            foreach (Transform transform in world.GetComponentsInChildren<Transform>(true))
            {
                if (!PrefabUtility.IsAnyPrefabInstanceRoot(transform.gameObject)
                    || PrefabUtility.GetOutermostPrefabInstanceRoot(transform.gameObject) != transform.gameObject)
                    continue;

                string prefabPath = AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(transform.gameObject));
                if (string.IsNullOrEmpty(prefabPath) || !IsNaturalGroundProp(prefabPath) || !IsLakeOrHarbor(transform.position))
                    continue;

                Renderer[] renderers = transform.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0)
                    continue;

                Bounds bounds = GetBounds(renderers);
                float lowestGround;
                float highestGround;
                GetFootprintGroundRange(terrain, bounds, out lowestGround, out highestGround);
                float offset = 0f;
                if (bounds.min.y > highestGround + 0.15f)
                    offset = highestGround - bounds.min.y - 0.02f;
                else if (bounds.max.y < lowestGround - 0.12f)
                    offset = lowestGround - bounds.max.y + 0.02f;

                bool isRaisingBuriedProp = offset > 0f;
                if (Mathf.Abs(offset) <= 0.01f
                    || (!isRaisingBuriedProp && Mathf.Abs(offset) > 1.5f)
                    || (isRaisingBuriedProp && Mathf.Abs(offset) > 4f))
                    continue;

                transform.position += Vector3.up * offset;
                PrefabUtility.RecordPrefabInstancePropertyModifications(transform);
                grounded++;
            }

            return grounded;
        }

        private static void GetFootprintGroundRange(Terrain terrain, Bounds bounds, out float lowestGround, out float highestGround)
        {
            lowestGround = float.MaxValue;
            highestGround = float.MinValue;
            float[] xs = { bounds.min.x, bounds.center.x, bounds.max.x };
            float[] zs = { bounds.min.z, bounds.center.z, bounds.max.z };
            for (int z = 0; z < zs.Length; z++)
            {
                for (int x = 0; x < xs.Length; x++)
                {
                    Vector3 samplePosition = new Vector3(xs[x], 0f, zs[z]);
                    float ground = terrain.SampleHeight(samplePosition) + terrain.transform.position.y;
                    lowestGround = Mathf.Min(lowestGround, ground);
                    highestGround = Mathf.Max(highestGround, ground);
                }
            }
        }

        private static bool IsNaturalGroundProp(string prefabPath)
        {
            return prefabPath.IndexOf("/Foliage/", StringComparison.OrdinalIgnoreCase) >= 0
                || prefabPath.IndexOf("Terrain_Boulder", StringComparison.OrdinalIgnoreCase) >= 0
                || prefabPath.IndexOf("RockDebris", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsLakeOrHarbor(Vector3 position)
        {
            bool isLake = position.x >= 875f && position.x <= 1255f && position.z >= 550f && position.z <= 1120f;
            bool isHarbor = position.x >= 1835f && position.x <= 2000f && position.z >= -355f && position.z <= -210f;
            return isLake || isHarbor;
        }

        private static Bounds GetBounds(Renderer[] renderers)
        {
            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
                bounds.Encapsulate(renderers[index].bounds);
            return bounds;
        }

        private static int AddShoreReeds(Terrain terrain)
        {
            Transform lakeRoot = GameObject.Find(LakeRootPath).transform;
            if (lakeRoot.Find("호숫가 갈대 군락") != null)
                return 0;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SilvergrassPath);
            if (prefab == null)
                throw new InvalidOperationException("갈대 프리팹을 찾지 못했습니다.");

            Vector2[] positions =
            {
                new Vector2(971f, 721f), new Vector2(983f, 714f), new Vector2(1001f, 704f),
                new Vector2(1128f, 710f), new Vector2(1151f, 721f), new Vector2(1168f, 739f),
                new Vector2(945f, 884f), new Vector2(952f, 902f), new Vector2(962f, 924f),
                new Vector2(1189f, 877f), new Vector2(1184f, 901f), new Vector2(1175f, 926f)
            };
            Transform group = new GameObject("호숫가 갈대 군락").transform;
            group.SetParent(lakeRoot, false);

            for (int index = 0; index < positions.Length; index++)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, group);
                Vector3 position = new Vector3(positions[index].x, 0f, positions[index].y);
                position.y = terrain.SampleHeight(position) + terrain.transform.position.y - 0.03f;
                instance.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, (index * 53f) % 360f, 0f));
                float scale = 0.52f + (index % 3) * 0.08f;
                instance.transform.localScale = Vector3.one * scale;
                foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true))
                    UnityEngine.Object.DestroyImmediate(collider);
                PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
            }

            return positions.Length;
        }

        private static int DisableInvalidShoreApron()
        {
            MeshRenderer apronRenderer = GameObject.Find("생활과 주민/LakeAbundanceArt/ShoreAndHousehold/Sandbanks/IrregularShoreAprons")?.GetComponent<MeshRenderer>();
            if (apronRenderer == null || !apronRenderer.enabled)
                return 0;

            // 자동 생성된 보조 메쉬가 완화된 지형보다 높게 솟아 검은 절개처럼 보인다.
            // 물·잔디·수변 암석은 독립적으로 남기므로, 이 실패한 보조 레이어만 제외한다.
            apronRenderer.enabled = false;
            EditorUtility.SetDirty(apronRenderer);
            return 1;
        }

        private static void ConfigureWaterMaterials()
        {
            ConfigureWater(AssetDatabase.LoadAssetAtPath<Material>(LakeWaterPath), new Color(0.10f, 0.41f, 0.39f), new Color(0.02f, 0.17f, 0.22f), 0.17f, 0.16f, 0.22f, 0.32f, 0.42f);
            ConfigureWater(AssetDatabase.LoadAssetAtPath<Material>(HarborWaterPath), new Color(0.09f, 0.34f, 0.34f), new Color(0.015f, 0.14f, 0.19f), 0.20f, 0.10f, 0.35f, 0.26f, 0.46f);
        }

        private static void ConfigureWater(Material material, Color shallowColor, Color deepColor, float waveStrength, float foamStrength, float weatherResponse, float surfaceOpacity, float reflectionStrength)
        {
            if (material == null)
                throw new InvalidOperationException("수면 재질을 찾지 못했습니다.");

            material.SetColor("_ShallowColor", shallowColor);
            material.SetColor("_DeepColor", deepColor);
            material.SetColor("_FoamColor", new Color(0.74f, 0.84f, 0.78f));
            material.SetFloat("_ReflectionStrength", reflectionStrength);
            material.SetFloat("_Smoothness", 0.88f);
            material.SetFloat("_WaveScale", 0.15f);
            material.SetFloat("_WaveStrength", waveStrength);
            material.SetFloat("_FoamStrength", foamStrength);
            material.SetFloat("_FoamWidth", 0.65f);
            material.SetFloat("_SurfaceOpacity", surfaceOpacity);
            material.SetFloat("_WeatherWindResponse", weatherResponse);
            EditorUtility.SetDirty(material);
        }

        /// <summary>룩 값만 적용한다. 지형·접지·갈대·NavMesh는 건드리지 않고 씬도 저장하지 않는다.</summary>
        [UPlaygroundTool("OpenWorld/호수 마을 색·빛 값 적용")]
        public static void ApplyLookOnly()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Edit Mode에서만 실행할 수 있습니다.");
            if (SceneManager.GetActiveScene().path != ScenePath)
                throw new InvalidOperationException("LakeVillageWorld 씬이 활성 씬일 때만 실행할 수 있습니다.");

            ConfigureLighting();
            ConfigureSceneShadowAsset();
            ConfigureSceneRendering();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssetIfDirty(GameObject.Find("LakeAtmosphere").GetComponent<Volume>().sharedProfile);
        }

        /// <summary>
        /// 그림자 거리·캐스케이드를 씬 전용 RP 에셋에 기록한다. 공용 PC_RPAsset은 모든 씬이 쓰므로
        /// 복제본에만 쓴다. 활성 씬에서의 적용과 복원은 SceneEnvironmentRendering이 소유한다.
        /// </summary>
        private static void ConfigureSceneShadowAsset()
        {
            if (AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(SceneRenderAssetPath) == null
                && !AssetDatabase.CopyAsset(SharedRenderAssetPath, SceneRenderAssetPath))
                throw new InvalidOperationException("씬 전용 RP 에셋 복제에 실패했습니다.");

            var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(SceneRenderAssetPath);
            asset.shadowDistance = SceneShadowDistance;
            asset.shadowCascadeCount = SceneShadowCascadeCount;
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
        }

        private static void ConfigureSceneRendering()
        {
            Light sun = RenderSettings.sun;
            if (sun == null || RenderSettings.skybox == null)
                throw new InvalidOperationException("씬 태양과 하늘 재질을 먼저 지정해야 합니다.");

            SceneEnvironmentRendering binding = sun.GetComponent<SceneEnvironmentRendering>();
            if (binding == null)
                binding = Undo.AddComponent<SceneEnvironmentRendering>(sun.gameObject);
            var serialized = new SerializedObject(binding);
            serialized.FindProperty("_renderPipeline").objectReferenceValue = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(SceneRenderAssetPath);
            serialized.FindProperty("_skySource").objectReferenceValue = RenderSettings.skybox;
            serialized.FindProperty("_atmosphereThickness").floatValue = SceneAtmosphereThickness;
            serialized.ApplyModifiedProperties();
        }

        private static void ConfigureLighting()
        {
            // 팔레트: 청록 앰비언트로 그림자에 색을 주고, 따뜻한 연한 하늘색 안개로 원경 대기 원근감을 만든다.
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.80f, 0.88f, 0.90f);
            RenderSettings.fogStartDistance = 100f;
            RenderSettings.fogEndDistance = 650f;
            RenderSettings.fogDensity = 0.0048f;
            RenderSettings.ambientSkyColor = new Color(0.50f, 0.66f, 0.74f);
            RenderSettings.ambientEquatorColor = new Color(0.36f, 0.52f, 0.50f);
            RenderSettings.ambientGroundColor = new Color(0.16f, 0.27f, 0.26f);
            // 동명 던전 조명이 있으므로 이름 검색으로 태양을 선택하면 안 된다.
            Light sun = RenderSettings.sun;
            if (sun == null)
                throw new InvalidOperationException("Directional Light를 찾지 못했습니다.");

            sun.intensity = 1.45f;
            sun.color = new Color(1f, 0.93f, 0.84f);
            sun.useColorTemperature = true;
            sun.colorTemperature = 5700f;
            EditorUtility.SetDirty(sun);

            Volume volume = GameObject.Find("LakeAtmosphere")?.GetComponent<Volume>();
            if (volume == null || volume.sharedProfile == null)
                return;

            foreach (VolumeComponent component in volume.sharedProfile.components)
            {
                ColorAdjustments colorAdjustments = component as ColorAdjustments;
                if (colorAdjustments != null)
                {
                    colorAdjustments.postExposure.value = 0f;
                    colorAdjustments.contrast.value = 2f;
                    colorAdjustments.saturation.value = 15f;
                    EditorUtility.SetDirty(colorAdjustments);
                    continue;
                }

                Bloom bloom = component as Bloom;
                if (bloom != null)
                {
                    bloom.intensity.value = 0.08f;
                    bloom.threshold.value = 1.35f;
                    EditorUtility.SetDirty(bloom);
                }
            }
        }

        private static void BakeNavigation()
        {
            NavMeshSurface surface = GameObject.Find("AuthoredEnvironment").GetComponent<NavMeshSurface>();
            if (surface == null)
                throw new InvalidOperationException("NavMeshSurface를 찾지 못했습니다.");

            surface.BuildNavMesh();
            if (surface.navMeshData == null)
                throw new InvalidOperationException("NavMesh 재생성에 실패했습니다.");

            EditorUtility.SetDirty(surface);
        }

        private static string BuildGroundingAudit(Terrain terrain)
        {
            var path = new NavMeshPath();
            Vector3 south = new Vector3(1067f, 81f, 590f);
            Vector3 center = new Vector3(1067f, 82f, 800f);
            bool pathComplete = NavMesh.CalculatePath(south, center, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete;
            return "접지 감사: 남쪽 호안=" + terrain.SampleHeight(south).ToString("F2")
                + " / 북안=" + terrain.SampleHeight(new Vector3(1067f, 0f, 985f)).ToString("F2")
                + " / 호안 경로=" + pathComplete;
        }
    }
}
