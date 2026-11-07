using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.World;
using UPlayGround.EditorTools;

namespace UPlayGround.Tool.Editor.Map
{
    /// <summary>
    /// 호수 마을 한 구역의 지면 풀·꽃 디테일과 지면 레이어 변주를 TerrainData에 적용한다.
    /// 같은 Apply를 Play Mode 런타임 복제본(실험)과 원본 에셋(확정)에 쓴다.
    /// </summary>
    public static class LakeVillageGroundDetailTool
    {
        private const string TerrainDataPath = "Assets/10.Datas/World/OpenWorld/Terrain.asset";
        private const string DetailAssetDirectory = "Assets/11.Material/TerrainDetail";
        private const string MonsterPlacementPath = "Assets/10.Datas/Map/Placement/PlacementData_LakeVillageWorld_Monsters.asset";
        private const string BackupPath = "output/LakeVillageVisual/Terrain.before-20261107.bin";

        private const string GrassPackDirectory =
            "Assets/ExternalAssets/Environment/PolyartStudio/FarmLand/PolyartStudio/Farmlands/";
        private const string GrassPrefabDirectory = GrassPackDirectory + "Prefabs/Foliage/Grass/Grass_02/";
        private const string GrassMaterialDirectory = GrassPackDirectory + "Meshes/Foliage/Grass/Grass_02/";
        private const string FlowerPackDirectory = "Assets/ExternalAssets/Environment/Idyllic Fantasy Nature/";

        private const int GrassLayerCount = 3;
        private const int FlowerLayerCount = 3;

        /// <summary>적용 구역과 밀도 단계. 기본값이 확정 값이다.</summary>
        public sealed class Settings
        {
            /// <summary>구역 중심(월드 XZ): 스폰·광장, 풀 밀도 최고 지점.</summary>
            public Vector2[] centers = { new Vector2(1091f, 418f), new Vector2(1134f, 273f) };
            public float innerRadius = 90f;
            public float outerRadius = 135f;
            /// <summary>1~3. 단계마다 셀당 풀 수가 늘어난다. 2단계가 렌더 비용 예산 안의 확정값이다.</summary>
            public int densityStage = 2;
            /// <summary>노란 풀 레이어로 옮기는 최대 비율. 높으면 지면이 칙칙한 카키색이 된다.</summary>
            public float yellowPatchStrength = 0.3f;
            /// <summary>꽃 패치 노이즈 임계 구간. 낮출수록 꽃이 넓게 퍼진다.</summary>
            public float flowerPatchMin = 0.55f;
            public float flowerPatchMax = 0.75f;
            public bool applyAlphamapVariation = true;
            public bool applyPrototypes = true;
            public bool applyDetailLayers = true;
            /// <summary>몬스터 배치 주변에서 풀을 낮추는 반경과 남기는 비율.</summary>
            public float combatClearRadius = 14f;
            public float combatDensityScale = 0.3f;
            /// <summary>구조물 콜라이더가 있는 셀에는 풀을 심지 않는다.</summary>
            public bool skipStructureCells = true;
            public int noiseSeed = 7;
        }

        private struct PrototypeSpec
        {
            public string name;
            public string sourcePrefab;
            public string sourceMaterial;
            public Color top;
            public Color topVariation;
            public Color bottom;
            public float minWidth, maxWidth, minHeight, maxHeight;
        }

        private static readonly PrototypeSpec[] GrassSpecs =
        {
            new PrototypeSpec
            {
                name = "TD_Grass_Lime", sourcePrefab = GrassPrefabDirectory + "SM_Farm_SmallGrass_01_Detail.prefab",
                sourceMaterial = GrassMaterialDirectory + "MI_FarmGrass_Small_01.mat",
                top = new Color(0.58f, 0.80f, 0.12f), topVariation = new Color(0.82f, 0.82f, 0.15f), bottom = new Color(0.28f, 0.48f, 0.18f),
                minWidth = 0.9f, maxWidth = 1.5f, minHeight = 1.0f, maxHeight = 1.7f
            },
            new PrototypeSpec
            {
                name = "TD_Grass_Fresh", sourcePrefab = GrassPrefabDirectory + "SM_Farm_SmallGrass_02_Detail.prefab",
                sourceMaterial = GrassMaterialDirectory + "MI_FarmGrass_Small_01.mat",
                top = new Color(0.30f, 0.74f, 0.32f), topVariation = new Color(0.52f, 0.80f, 0.22f), bottom = new Color(0.18f, 0.40f, 0.22f),
                minWidth = 1.0f, maxWidth = 1.5f, minHeight = 1.0f, maxHeight = 1.8f
            }
        };

        private static readonly PrototypeSpec[] FlowerSpecs =
        {
            new PrototypeSpec { name = "TD_Flower_White", sourcePrefab = FlowerPackDirectory + "Prefabs/Flower_White.prefab", minWidth = 0.5f, maxWidth = 0.8f, minHeight = 0.6f, maxHeight = 1.0f },
            new PrototypeSpec { name = "TD_Flower_Yellow", sourcePrefab = FlowerPackDirectory + "Prefabs/Flower_Yellow.prefab", minWidth = 0.5f, maxWidth = 0.8f, minHeight = 0.6f, maxHeight = 1.0f },
            new PrototypeSpec { name = "TD_Flower_Blue", sourcePrefab = FlowerPackDirectory + "Prefabs/Flower_Blue_01.prefab", minWidth = 0.5f, maxWidth = 0.8f, minHeight = 0.6f, maxHeight = 1.0f }
        };

        // 단계별 셀당 풀 개수 상한: [기존 프로토타입 0, 라임, 청록].
        private static readonly int[][] GrassCountsByStage =
        {
            new[] { 2, 1, 1 },
            new[] { 3, 2, 2 },
            new[] { 4, 3, 3 }
        };

        private static readonly int[] FlowerMaxByStage = { 1, 2, 3 };

        /// <summary>적용 결과 요약.</summary>
        public sealed class Result
        {
            public bool succeeded;
            public string message;
            public readonly StringBuilder log = new StringBuilder();
        }

        [UPlaygroundTool("OpenWorld/호수 마을 지면 디테일 적용")]
        public static void ApplyToAsset()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Edit Mode에서만 원본 에셋에 적용할 수 있습니다.");

            TerrainData data = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainDataPath);
            Terrain terrain = FindTerrain(data);
            if (data == null || terrain == null)
                throw new InvalidOperationException("Terrain.asset 또는 씬의 Terrain을 찾지 못했습니다.");

            if (!File.Exists(BackupPath))
                throw new InvalidOperationException("백업이 없어 적용하지 않습니다: " + BackupPath);

            Result result = Apply(terrain, data, new Settings());
            Debug.Log(result.log.ToString());
            if (!result.succeeded)
            {
                RestoreFromBackup();
                throw new InvalidOperationException("적용 실패, 백업으로 복구했습니다: " + result.message);
            }

            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssetIfDirty(data);
        }

        /// <summary>
        /// terrain 위치·크기를 기준으로 data에 지면 변주·프로토타입·디테일 맵을 쓴다.
        /// 기존 디테일 맵은 보관 후 전 셀을 대조해 보존 여부를 검증한다. SetDetailScatterMode는 호출하지 않는다.
        /// </summary>
        public static Result Apply(Terrain terrain, TerrainData data, Settings settings)
        {
            var result = new Result();
            DetailScatterMode modeBefore = data.detailScatterMode;
            int resolution = data.detailResolution;
            int oldProtoCount = data.detailPrototypes.Length;

            int[][,] backupLayers = ReadAllDetailLayers(data, oldProtoCount);
            result.log.AppendLine("[지면 디테일] 시작: 모드=" + modeBefore + " 프로토타입=" + oldProtoCount + " 해상도=" + resolution);

            Vector3 origin = terrain.transform.position;
            RectInt box = BuildBoundingBox(data, origin, settings, resolution);
            result.log.AppendLine("처리 영역 셀 박스: x " + box.xMin + "~" + (box.xMax - 1) + ", y " + box.yMin + "~" + (box.yMax - 1));

            if (settings.applyAlphamapVariation)
                ApplyAlphamapVariation(data, origin, settings, result);

            if (settings.applyPrototypes)
            {
                data.detailPrototypes = BuildPrototypes(data.detailPrototypes[0]);
                data.RefreshPrototypes();
            }

            if (data.detailScatterMode != modeBefore)
            {
                result.message = "산포 모드가 바뀌었다: " + modeBefore + " → " + data.detailScatterMode;
                return result;
            }

            int protoCount = data.detailPrototypes.Length;
            result.log.AppendLine("프로토타입 설정 후 개수=" + protoCount);

            // 프로토타입 배열 변경이 기존 레이어 맵을 비웠는지 확인하고, 필요하면 복원한다.
            for (int layer = 0; layer < Math.Min(oldProtoCount, protoCount); layer++)
            {
                int[,] current = data.GetDetailLayer(0, 0, resolution, resolution, layer);
                if (!MapsEqual(current, backupLayers[layer]))
                {
                    result.log.AppendLine("레이어 " + layer + " 맵이 프로토타입 변경으로 바뀌어 복원한다.");
                    data.SetDetailLayer(0, 0, layer, backupLayers[layer]);
                }
            }

            if (settings.applyDetailLayers)
                WriteDetailLayers(terrain, data, settings, box, backupLayers, result);

            return Verify(data, backupLayers, box, modeBefore, result);
        }

        private static Result Verify(TerrainData data, int[][,] backupLayers, RectInt box, DetailScatterMode modeBefore, Result result)
        {
            int resolution = data.detailResolution;
            long outsideDifferences = 0;
            long insideDecreases = 0;
            for (int layer = 0; layer < backupLayers.Length; layer++)
            {
                int[,] now = data.GetDetailLayer(0, 0, resolution, resolution, layer);
                int[,] before = backupLayers[layer];
                for (int y = 0; y < resolution; y++)
                {
                    for (int x = 0; x < resolution; x++)
                    {
                        bool inside = x >= box.xMin && x < box.xMax && y >= box.yMin && y < box.yMax;
                        if (!inside && now[y, x] != before[y, x])
                            outsideDifferences++;
                        else if (inside && layer == 0 && now[y, x] < before[y, x])
                            insideDecreases++;
                    }
                }
            }

            result.log.AppendLine("검증: 영역 밖 변경 셀=" + outsideDifferences + ", 영역 안 레이어0 감소 셀=" + insideDecreases + ", 모드=" + data.detailScatterMode);
            for (int layer = 0; layer < data.detailPrototypes.Length; layer++)
            {
                int[,] now = data.GetDetailLayer(0, 0, resolution, resolution, layer);
                long sum = 0;
                int nonZero = 0;
                for (int y = 0; y < resolution; y++)
                {
                    for (int x = 0; x < resolution; x++)
                    {
                        sum += now[y, x];
                        if (now[y, x] > 0) nonZero++;
                    }
                }

                result.log.AppendLine("  레이어 " + layer + ": 합계=" + sum + " 값이 있는 셀=" + nonZero);
            }

            result.succeeded = outsideDifferences == 0 && insideDecreases == 0 && data.detailScatterMode == modeBefore;
            result.message = result.succeeded ? "ok" : "검증 불일치";
            return result;
        }

        #region 알파맵

        private static void ApplyAlphamapVariation(TerrainData data, Vector3 origin, Settings settings, Result result)
        {
            int res = data.alphamapResolution;
            int layerCount = data.alphamapLayers;
            Vector3 size = data.size;
            RectInt box = BuildBoundingBox(data, origin, settings, res);
            float[,,] maps = data.GetAlphamaps(box.xMin, box.yMin, box.width, box.height);
            float[,,] blurred = Blur(maps, box.width, box.height, layerCount);
            int changed = 0;
            var buffer = new float[layerCount];

            for (int y = 0; y < box.height; y++)
            {
                for (int x = 0; x < box.width; x++)
                {
                    float wx = origin.x + (box.xMin + x + 0.5f) / res * size.x;
                    float wz = origin.z + (box.yMin + y + 0.5f) / res * size.z;
                    float f = ZoneFalloff(wx, wz, settings);
                    if (f <= 0f) continue;

                    for (int l = 0; l < layerCount; l++)
                        buffer[l] = Mathf.Lerp(maps[y, x, l], blurred[y, x, l], 0.8f * f);

                    float grass = buffer[0] + buffer[3] + buffer[4];
                    if (grass > 0.5f)
                    {
                        float patch = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.42f, 0.72f,
                            Mathf.PerlinNoise(wx / 38f + settings.noiseSeed * 3.1f, wz / 38f + settings.noiseSeed * 1.7f)));
                        float shift = patch * settings.yellowPatchStrength * f;
                        float moved = (buffer[0] + buffer[4]) * shift;
                        float baseSum = buffer[0] + buffer[4];
                        if (baseSum > 0.0001f)
                        {
                            buffer[0] -= moved * buffer[0] / baseSum;
                            buffer[4] -= moved * buffer[4] / baseSum;
                            buffer[3] += moved;
                        }
                    }

                    if (buffer[2] > 0.0001f)
                    {
                        float steepness = data.GetSteepness((box.xMin + x + 0.5f) / res, (box.yMin + y + 0.5f) / res);
                        if (steepness < 35f)
                        {
                            float moved = buffer[2] * 0.7f * f;
                            buffer[2] -= moved;
                            buffer[4] += moved;
                        }
                    }

                    float total = 0f;
                    for (int l = 0; l < layerCount; l++) total += buffer[l];
                    if (total <= 0.0001f) continue;
                    for (int l = 0; l < layerCount; l++) maps[y, x, l] = buffer[l] / total;
                    changed++;
                }
            }

            data.SetAlphamaps(box.xMin, box.yMin, maps);
            result.log.AppendLine("알파맵 변주: 변경 셀=" + changed + " (블러·노란 풀 패치·바위 완화)");
        }

        private static float[,,] Blur(float[,,] source, int width, int height, int layers)
        {
            var output = new float[height, width, layers];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int l = 0; l < layers; l++)
                    {
                        float sum = 0f;
                        float weight = 0f;
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                int sx = Mathf.Clamp(x + dx, 0, width - 1);
                                int sy = Mathf.Clamp(y + dy, 0, height - 1);
                                float w = (dx == 0 && dy == 0) ? 4f : (dx == 0 || dy == 0) ? 2f : 1f;
                                sum += source[sy, sx, l] * w;
                                weight += w;
                            }
                        }

                        output[y, x, l] = sum / weight;
                    }
                }
            }

            return output;
        }

        #endregion

        #region 프로토타입

        private static DetailPrototype[] BuildPrototypes(DetailPrototype existing)
        {
            var list = new List<DetailPrototype> { existing };
            EnsureDetailAssets();

            foreach (PrototypeSpec spec in GrassSpecs)
                list.Add(CreatePrototype(spec, LoadDetailPrefab(spec.name)));
            foreach (PrototypeSpec spec in FlowerSpecs)
                list.Add(CreatePrototype(spec, LoadDetailPrefab(spec.name)));
            return list.ToArray();
        }

        private static DetailPrototype CreatePrototype(PrototypeSpec spec, GameObject prefab)
        {
            return new DetailPrototype
            {
                prototype = prefab,
                usePrototypeMesh = true,
                useInstancing = true,
                renderMode = DetailRenderMode.VertexLit,
                minWidth = spec.minWidth,
                maxWidth = spec.maxWidth,
                minHeight = spec.minHeight,
                maxHeight = spec.maxHeight,
                noiseSpread = 0.25f,
                density = 1f,
                holeEdgePadding = 0f,
                healthyColor = Color.white,
                dryColor = Color.white,
                alignToGround = 0f,
                positionJitter = 0f
            };
        }

        private static GameObject LoadDetailPrefab(string name)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(DetailAssetDirectory + "/" + name + ".prefab");
        }

        /// <summary>디테일용 재질·프리팹이 없으면 만들고, 있으면 색 값을 다시 쓴다.</summary>
        public static void EnsureDetailAssets()
        {
            Directory.CreateDirectory(DetailAssetDirectory);
            foreach (PrototypeSpec spec in GrassSpecs)
            {
                Material material = LoadOrCreateMaterial(spec.name, spec.sourceMaterial);
                material.SetColor("_ColorTop", spec.top);
                material.SetColor("_ColorTopVariation", spec.topVariation);
                material.SetColor("_ColorBottom", spec.bottom);
                EditorUtility.SetDirty(material);
                SavePrefab(spec, material);
            }

            Material flowerMaterial = LoadOrCreateMaterial("TD_Flower", FlowerPackDirectory + "Materials/Flowers/Flower.mat");
            flowerMaterial.enableInstancing = true;
            EditorUtility.SetDirty(flowerMaterial);
            foreach (PrototypeSpec spec in FlowerSpecs)
                SavePrefab(spec, flowerMaterial);

            AssetDatabase.SaveAssets();
        }

        private static Material LoadOrCreateMaterial(string name, string sourcePath)
        {
            string path = DetailAssetDirectory + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;

            Material source = AssetDatabase.LoadAssetAtPath<Material>(sourcePath);
            if (source == null) throw new InvalidOperationException("원본 재질이 없다: " + sourcePath);
            material = new Material(source) { name = name, enableInstancing = true };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void SavePrefab(PrototypeSpec spec, Material material)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(spec.sourcePrefab);
            Mesh mesh = source != null ? source.GetComponentInChildren<MeshFilter>(true)?.sharedMesh : null;
            if (mesh == null) throw new InvalidOperationException("원본 프리팹 메시가 없다: " + spec.sourcePrefab);

            var go = new GameObject(spec.name);
            try
            {
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterial = material;
                PrefabUtility.SaveAsPrefabAsset(go, DetailAssetDirectory + "/" + spec.name + ".prefab");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        #endregion

        #region 디테일 레이어

        private static void WriteDetailLayers(Terrain terrain, TerrainData data, Settings settings, RectInt box,
            int[][,] backupLayers, Result result)
        {
            int resolution = data.detailResolution;
            Vector3 origin = terrain.transform.position;
            Vector3 size = data.size;
            int stage = Mathf.Clamp(settings.densityStage, 1, GrassCountsByStage.Length) - 1;
            int[] grassMax = GrassCountsByStage[stage];
            int flowerMax = FlowerMaxByStage[stage];
            int protoCount = data.detailPrototypes.Length;
            int alphaRes = data.alphamapResolution;

            float[,,] alpha = data.GetAlphamaps(0, 0, alphaRes, alphaRes);
            List<Vector2> combatPoints = LoadCombatPoints();
            var maps = new int[protoCount][,];
            for (int layer = 0; layer < protoCount; layer++)
                maps[layer] = layer < backupLayers.Length ? (int[,])backupLayers[layer].Clone() : new int[resolution, resolution];

            int blocked = 0;
            int grassCells = 0;
            var overlaps = new Collider[16];
            float halfX = size.x / resolution * 0.4f;
            float halfZ = size.z / resolution * 0.4f;
            Physics.SyncTransforms();

            for (int y = box.yMin; y < box.yMax; y++)
            {
                for (int x = box.xMin; x < box.xMax; x++)
                {
                    float wx = origin.x + (x + 0.5f) / resolution * size.x;
                    float wz = origin.z + (y + 0.5f) / resolution * size.z;
                    float f = ZoneFalloff(wx, wz, settings);
                    if (f <= 0f) continue;

                    int ax = Mathf.Clamp(Mathf.RoundToInt((x + 0.5f) / resolution * alphaRes - 0.5f), 0, alphaRes - 1);
                    int ay = Mathf.Clamp(Mathf.RoundToInt((y + 0.5f) / resolution * alphaRes - 0.5f), 0, alphaRes - 1);
                    float grass = alpha[ay, ax, 0] + alpha[ay, ax, 3] + alpha[ay, ax, 4];
                    float cover = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 0.8f, grass));
                    if (cover <= 0f) continue;

                    float density = cover * f * CombatFactor(wx, wz, combatPoints, settings);
                    if (density <= 0.01f) continue;

                    if (settings.skipStructureCells && IsBlockedByStructure(data, origin, wx, wz, x, y, resolution, halfX, halfZ, overlaps))
                    {
                        blocked++;
                        continue;
                    }

                    grassCells++;
                    for (int g = 0; g < GrassLayerCount; g++)
                    {
                        float variation = Mathf.Lerp(0.6f, 1.4f,
                            Mathf.PerlinNoise(wx / 17f + g * 41.3f + settings.noiseSeed, wz / 17f + g * 23.9f));
                        int count = Mathf.Min(255, Mathf.RoundToInt(density * grassMax[g] * variation));
                        if (g == 0)
                            maps[0][y, x] = Math.Max(maps[0][y, x], count);
                        else
                            maps[g][y, x] = count;
                    }

                    for (int k = 0; k < FlowerLayerCount; k++)
                    {
                        float patch = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(settings.flowerPatchMin, settings.flowerPatchMax,
                            Mathf.PerlinNoise(wx / 14f + k * 57.1f + settings.noiseSeed * 2f, wz / 14f + k * 31.7f)));
                        maps[GrassLayerCount + k][y, x] = Mathf.RoundToInt(density * patch * (flowerMax + 0.49f));
                    }
                }
            }

            for (int layer = 0; layer < protoCount; layer++)
                data.SetDetailLayer(0, 0, layer, maps[layer]);

            result.log.AppendLine("디테일 레이어 기록: 풀 셀=" + grassCells + " 구조물 제외 셀=" + blocked + " 몬스터 기준점=" + combatPoints.Count + " 밀도 단계=" + (stage + 1));
        }

        private static bool IsBlockedByStructure(TerrainData data, Vector3 origin, float wx, float wz, int x, int y,
            int resolution, float halfX, float halfZ, Collider[] buffer)
        {
            float height = origin.y + data.GetInterpolatedHeight((x + 0.5f) / resolution, (y + 0.5f) / resolution);
            var center = new Vector3(wx, height + 1.0f, wz);
            int count = Physics.OverlapBoxNonAlloc(center, new Vector3(halfX, 1.0f, halfZ), buffer, Quaternion.identity,
                ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider collider = buffer[i];
                if (collider is TerrainCollider) continue;
                if (collider.transform.root.name != "AuthoredEnvironment") continue;
                return true;
            }

            return false;
        }

        private static List<Vector2> LoadCombatPoints()
        {
            var points = new List<Vector2>();
            WorldPlacementDataSO placement = AssetDatabase.LoadAssetAtPath<WorldPlacementDataSO>(MonsterPlacementPath);
            if (placement == null) return points;
            foreach (WorldPlacementRecord record in placement.Records)
                points.Add(new Vector2(record.position.x, record.position.z));
            return points;
        }

        private static float CombatFactor(float wx, float wz, List<Vector2> points, Settings settings)
        {
            float nearest = float.MaxValue;
            var position = new Vector2(wx, wz);
            for (int i = 0; i < points.Count; i++)
                nearest = Mathf.Min(nearest, (points[i] - position).magnitude);

            float t = Mathf.InverseLerp(settings.combatClearRadius, settings.combatClearRadius * 2f, nearest);
            return Mathf.Lerp(settings.combatDensityScale, 1f, t);
        }

        #endregion

        #region 공통

        private static float ZoneFalloff(float wx, float wz, Settings settings)
        {
            float best = 0f;
            var position = new Vector2(wx, wz);
            foreach (Vector2 center in settings.centers)
            {
                float distance = (position - center).magnitude;
                best = Mathf.Max(best, 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(settings.innerRadius, settings.outerRadius, distance)));
            }

            return best;
        }

        private static RectInt BuildBoundingBox(TerrainData data, Vector3 origin, Settings settings, int resolution)
        {
            float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
            foreach (Vector2 center in settings.centers)
            {
                minX = Mathf.Min(minX, center.x - settings.outerRadius);
                maxX = Mathf.Max(maxX, center.x + settings.outerRadius);
                minZ = Mathf.Min(minZ, center.y - settings.outerRadius);
                maxZ = Mathf.Max(maxZ, center.y + settings.outerRadius);
            }

            int x0 = Mathf.Clamp(Mathf.FloorToInt((minX - origin.x) / data.size.x * resolution) - 1, 0, resolution - 1);
            int x1 = Mathf.Clamp(Mathf.CeilToInt((maxX - origin.x) / data.size.x * resolution) + 1, 1, resolution);
            int y0 = Mathf.Clamp(Mathf.FloorToInt((minZ - origin.z) / data.size.z * resolution) - 1, 0, resolution - 1);
            int y1 = Mathf.Clamp(Mathf.CeilToInt((maxZ - origin.z) / data.size.z * resolution) + 1, 1, resolution);
            return new RectInt(x0, y0, x1 - x0, y1 - y0);
        }

        private static int[][,] ReadAllDetailLayers(TerrainData data, int count)
        {
            int resolution = data.detailResolution;
            var layers = new int[count][,];
            for (int i = 0; i < count; i++)
                layers[i] = data.GetDetailLayer(0, 0, resolution, resolution, i);
            return layers;
        }

        private static bool MapsEqual(int[,] a, int[,] b)
        {
            int h = a.GetLength(0);
            int w = a.GetLength(1);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    if (a[y, x] != b[y, x]) return false;
            return true;
        }

        private static Terrain FindTerrain(TerrainData data)
        {
            foreach (Terrain terrain in Terrain.activeTerrains)
                if (terrain.terrainData == data) return terrain;
            return Terrain.activeTerrains.Length > 0 ? Terrain.activeTerrains[0] : null;
        }

        private static void RestoreFromBackup()
        {
            File.Copy(BackupPath, TerrainDataPath, true);
            AssetDatabase.ImportAsset(TerrainDataPath, ImportAssetOptions.ForceUpdate);
        }

        #endregion
    }
}
