using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UPlayGround.Manager.World;

namespace UPlayGround.Editor
{
    public static partial class OpenWorldSliceAuthoring
    {
        private static void MergeWorldGround(Terrain north, Terrain south, ContinuousWorldController world, CityRecipe recipe)
        {
            // 크기와 정점 간격이 다른 Terrain 경계는 높이가 같아도 LOD 균열이 보인다.
            // 원본 샘플을 월드 좌표로 다시 담아 렌더링·충돌면을 하나로 소유한다.
            var sources = new[] { north, south };
            var sourceData = sources.Select(t => t.terrainData).ToArray();
            Vector3 northSourceSize = sourceData[0].size;
            var origins = sources.Select(t => t.transform.position).ToArray();
            var colors = sourceData.Select(d => d.GetAlphamaps(0, 0, d.alphamapWidth, d.alphamapHeight)).ToArray();
            var layers = sourceData.SelectMany(d => d.terrainLayers).Distinct().ToArray();
            var layerMaps = sourceData.Select(d => d.terrainLayers.Select(l => Array.IndexOf(layers, l)).ToArray()).ToArray();
            Vector3 origin = new Vector3(origins[1].x, 0, origins[1].z);
            Vector3 size = new Vector3(sourceData[1].size.x, sourceData[0].size.y + origins[0].y,
                origins[0].z + sourceData[0].size.z - origin.z);
            bool expanded = recipe.landscape.worldSize != Vector3.zero;
            if (expanded) { origin = recipe.landscape.worldOrigin; size = recipe.landscape.worldSize; }
            var merged = new TerrainData { heightmapResolution = expanded ? recipe.landscape.worldResolution : 2049, alphamapResolution = 1024,
                baseMapResolution = 1024, size = size, terrainLayers = layers };
            int resolution = merged.heightmapResolution;
            var heights = new float[resolution, resolution];
            for (int z = 0; z < resolution; z++)
            for (int x = 0; x < resolution; x++)
            {
                Vector3 p = origin + new Vector3(x * size.x / (resolution - 1), 0, z * size.z / (resolution - 1));
                int index = p.z >= origins[0].z ? 0 : 1;
                Vector3 local = p - origins[index];
                float height = sourceData[index].GetInterpolatedHeight(Mathf.Clamp01(local.x / sourceData[index].size.x),
                    Mathf.Clamp01(local.z / sourceData[index].size.z)) + origins[index].y;
                if (expanded)
                    height = Mathf.Lerp(SampleLandscape(p, recipe.landscape), height,
                        GetSourceBlend(p, origins[index], sourceData[index].size, index == 0, recipe.landscape.edgeTransition));
                foreach (var platform in recipe.landscape.platforms)
                    height = Mathf.Lerp(height, platform.height, GetRectanglePreservation(p, platform.bounds, platform.transition));
                heights[z, x] = (height - origin.y) / size.y;
            }
            merged.SetHeights(0, 0, heights);
            var weights = new float[merged.alphamapHeight, merged.alphamapWidth, layers.Length];
            int grass = Array.FindIndex(layers, l => AssetDatabase.GetAssetPath(l) == recipe.landscape.grassLayer);
            int rock = Array.FindIndex(layers, l => AssetDatabase.GetAssetPath(l) == recipe.landscape.rockLayer);
            int earth = Array.FindIndex(layers, l => AssetDatabase.GetAssetPath(l) == recipe.landscape.earthLayer);
            for (int z = 0; z < merged.alphamapHeight; z++)
            for (int x = 0; x < merged.alphamapWidth; x++)
            {
                Vector3 p = origin + new Vector3(x * size.x / (merged.alphamapWidth - 1), 0, z * size.z / (merged.alphamapHeight - 1));
                int index = p.z >= origins[0].z ? 0 : 1;
                var data = sourceData[index];
                Vector3 local = p - origins[index];
                int sx = Mathf.RoundToInt(Mathf.Clamp01(local.x / data.size.x) * (data.alphamapWidth - 1));
                int sz = Mathf.RoundToInt(Mathf.Clamp01(local.z / data.size.z) * (data.alphamapHeight - 1));
                for (int layer = 0; layer < layerMaps[index].Length; layer++)
                    weights[z, x, layerMaps[index][layer]] = colors[index][sz, sx, layer];
                if (expanded)
                {
                    float blend = GetSourceBlend(p, origins[index], data.size, index == 0, recipe.landscape.edgeTransition);
                    for (int layer = 0; layer < layers.Length; layer++) weights[z, x, layer] *= blend;
                    float steep = Mathf.InverseLerp(22, 42, merged.GetSteepness(x / (float)(merged.alphamapWidth - 1), z / (float)(merged.alphamapHeight - 1)));
                    float soil = Mathf.Lerp(0.12f, 0.55f, Mathf.InverseLerp(63, 46, merged.GetInterpolatedHeight(
                        x / (float)(merged.alphamapWidth - 1), z / (float)(merged.alphamapHeight - 1)) + origin.y));
                    weights[z, x, grass] += (1 - blend) * (1 - steep) * (1 - soil);
                    weights[z, x, earth] += (1 - blend) * (1 - steep) * soil;
                    weights[z, x, rock] += (1 - blend) * steep;
                }
            }
            merged.SetAlphamaps(0, 0, weights);
            CopyNorthDetails(sourceData[0], origins[0], merged, origin);
            merged.treePrototypes = sourceData[0].treePrototypes;
            var trees = sourceData[0].treeInstances;
            for (int i = 0; i < trees.Length; i++)
            {
                Vector3 p = Vector3.Scale(trees[i].position, sourceData[0].size) + origins[0] - origin;
                trees[i].position = new Vector3(p.x / size.x, p.y / size.y, p.z / size.z);
            }
            merged.SetTreeInstances(trees, false);
            CopyGeneratedGround(merged, sourceData[0]);
            north.transform.position = origin;
            north.GetComponent<TerrainCollider>().terrainData = sourceData[0];
            north.Flush();
            if (expanded)
            {
                StampRoads(north, recipe);
                PopulateExpansionDetails(north, origins[0], northSourceSize, recipe);
                world.SurfaceRecoveryBounds = new Rect(origin.x, origin.z, size.x, size.z);
            }
            UnityEngine.Object.DestroyImmediate(south.gameObject);
            world.SurfaceRecoveryTerrains = new[] { north };
            foreach (var region in world.Regions) region.Grounds = new Collider[] { north.GetComponent<TerrainCollider>() };
        }

        private static float GetSourceBlend(Vector3 position, Vector3 origin, Vector3 size, bool northern, float transition)
        {
            Vector3 local = position - origin;
            if (local.x < 0 || local.z < 0 || local.x > size.x || local.z > size.z) return 0;
            // 남쪽 접합면은 이미 같은 높이로 조형했다. 북쪽 원본의 외곽만 새 산기슭으로 잇는다.
            if (!northern) return 1;
            float edge = Mathf.Min(Mathf.Min(local.x, size.x - local.x), size.z - local.z);
            return Mathf.SmoothStep(0, 1, Mathf.Clamp01(edge / transition));
        }

        private static void PopulateExpansionDetails(Terrain terrain, Vector3 originalOrigin, Vector3 originalSize, CityRecipe recipe)
        {
            var landscape = recipe.landscape;
            if (string.IsNullOrEmpty(landscape.groundCoverPrefab)) return;
            var data = terrain.terrainData;
            var prototypes = data.detailPrototypes.ToList();
            prototypes.Add(new DetailPrototype {
                prototype = AssetDatabase.LoadAssetAtPath<GameObject>(landscape.groundCoverPrefab),
                usePrototypeMesh = true, useInstancing = true, renderMode = DetailRenderMode.VertexLit,
                minWidth = 0.7f, maxWidth = 1.2f, minHeight = 0.7f, maxHeight = 1.2f,
                healthyColor = Color.white, dryColor = Color.white, noiseSpread = 0.2f
            });
            data.detailPrototypes = prototypes.ToArray();
            if (data.detailResolution == 0) data.SetDetailResolution(1024, 16);
            var values = new int[data.detailHeight, data.detailWidth];
            for (int z = 0; z < data.detailHeight; z++)
            for (int x = 0; x < data.detailWidth; x++)
            {
                float u = (x + 0.5f) / data.detailWidth, v = (z + 0.5f) / data.detailHeight;
                Vector3 p = terrain.transform.position + new Vector3(u * data.size.x, 0, v * data.size.z);
                if (p.x >= originalOrigin.x && p.x <= originalOrigin.x + originalSize.x
                    && p.z >= originalOrigin.z && p.z <= originalOrigin.z + originalSize.z) continue;
                float road = RoadDistance(p, recipe, out _);
                if (road < GetRoadHalfWidth(p, recipe) + 2 || road > landscape.groundCoverDistance) continue;
                if (data.GetInterpolatedHeight(u, v) + terrain.transform.position.y < 47 || data.GetSteepness(u, v) > 30) continue;
                bool foundation = false;
                foreach (var city in recipe.cities) foundation |= GetGroundPreservation(city.ToSource(p), city) > 0.1f;
                if (foundation) continue;
                float density = Mathf.PerlinNoise(p.x * 0.07f, p.z * 0.07f);
                values[z, x] = density > 0.35f ? landscape.groundCoverDensity : 0;
            }
            data.SetDetailLayer(0, 0, prototypes.Count - 1, values);
            EditorUtility.SetDirty(data);
        }

        private static void CopyGeneratedGround(TerrainData generated, TerrainData destination)
        {
            var weights = generated.GetAlphamaps(0, 0, generated.alphamapWidth, generated.alphamapHeight);
            // Terrain 내부 텍스처를 CopySerialized로 공유하면 임시 데이터 파괴/재임포트 때 도색이 유실된다.
            // 공개 데이터 API로 복사해 기존 에셋 GUID와 내부 텍스처 소유권을 함께 유지한다.
            destination.heightmapResolution = generated.heightmapResolution;
            destination.size = generated.size;
            destination.alphamapResolution = generated.alphamapResolution;
            destination.baseMapResolution = generated.baseMapResolution;
            destination.terrainLayers = generated.terrainLayers;
            destination.SetHeights(0, 0, generated.GetHeights(0, 0, generated.heightmapResolution, generated.heightmapResolution));
            destination.treePrototypes = generated.treePrototypes;
            destination.SetTreeInstances(generated.treeInstances, false);
            destination.detailPrototypes = generated.detailPrototypes;
            destination.SetDetailResolution(generated.detailResolution, generated.detailResolutionPerPatch);
            for (int layer = 0; layer < generated.detailPrototypes.Length; layer++)
                destination.SetDetailLayer(0, 0, layer, generated.GetDetailLayer(0, 0, generated.detailWidth, generated.detailHeight, layer));
            destination.SetAlphamaps(0, 0, weights);
            EditorUtility.SetDirty(destination);
            foreach (var texture in destination.alphamapTextures) EditorUtility.SetDirty(texture);
            UnityEngine.Object.DestroyImmediate(generated);
        }

        private static void CopyNorthDetails(TerrainData source, Vector3 sourceOrigin, TerrainData target, Vector3 targetOrigin)
        {
            target.detailPrototypes = source.detailPrototypes;
            target.SetDetailResolution(source.detailResolution, source.detailResolutionPerPatch);
            for (int layer = 0; layer < source.detailPrototypes.Length; layer++)
            {
                var original = source.GetDetailLayer(0, 0, source.detailWidth, source.detailHeight, layer);
                var values = new int[target.detailHeight, target.detailWidth];
                for (int z = 0; z < target.detailHeight; z++)
                for (int x = 0; x < target.detailWidth; x++)
                {
                    float u = (targetOrigin.x + x * target.size.x / target.detailWidth - sourceOrigin.x) / source.size.x;
                    float v = (targetOrigin.z + z * target.size.z / target.detailHeight - sourceOrigin.z) / source.size.z;
                    if (u < 0 || u >= 1 || v < 0 || v >= 1) continue;
                    values[z, x] = original[Mathf.FloorToInt(v * source.detailHeight), Mathf.FloorToInt(u * source.detailWidth)];
                }
                target.SetDetailLayer(0, 0, layer, values);
            }
        }
    }
}
