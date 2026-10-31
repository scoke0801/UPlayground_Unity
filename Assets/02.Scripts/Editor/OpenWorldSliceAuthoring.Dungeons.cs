using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Editor
{
    public static partial class OpenWorldSliceAuthoring
    {
        [Serializable]
        private sealed class DungeonTerrainPatch
        {
            public string id, sourceTerrain;
            public RegionRect bounds;
            public float height, transition, endHeight;
            public bool isHole, hasSlope;
        }

        /// <summary>조립 맵의 기초·매립 지붕·진입구를 월드 Terrain에 연결한다.</summary>
        private static void ApplyDungeonGround(Terrain[] terrains, LandscapeRecipe recipe)
        {
            foreach (var patch in recipe.dungeonGround)
            {
                if (patch.bounds.width <= 0 || patch.bounds.height <= 0 || patch.transition < 0)
                    throw new InvalidOperationException("던전 지형 범위 오류: " + patch.id);
                var bounds = new Rect(patch.bounds.x, patch.bounds.y, patch.bounds.width, patch.bounds.height);
                var source = string.IsNullOrEmpty(patch.sourceTerrain)
                    ? null : AssetDatabase.LoadAssetAtPath<TerrainData>(patch.sourceTerrain);
                if (!string.IsNullOrEmpty(patch.sourceTerrain) && source == null)
                    throw new InvalidOperationException("던전 원본 지형 누락: " + patch.sourceTerrain);
                foreach (var terrain in terrains)
                {
                    var data = terrain.terrainData;
                    var origin = terrain.transform.position;
                    int resolution = patch.isHole ? data.holesResolution : data.heightmapResolution - 1;
                    int minX = Mathf.Clamp(Mathf.FloorToInt((bounds.xMin - patch.transition - origin.x) / data.size.x * resolution), 0, resolution - 1);
                    int minZ = Mathf.Clamp(Mathf.FloorToInt((bounds.yMin - patch.transition - origin.z) / data.size.z * resolution), 0, resolution - 1);
                    int maxX = Mathf.Clamp(Mathf.CeilToInt((bounds.xMax + patch.transition - origin.x) / data.size.x * resolution), 0, resolution - 1);
                    int maxZ = Mathf.Clamp(Mathf.CeilToInt((bounds.yMax + patch.transition - origin.z) / data.size.z * resolution), 0, resolution - 1);
                    int width = maxX - minX + 1, depth = maxZ - minZ + 1;
                    if (patch.isHole)
                    {
                        var holes = data.GetHoles(minX, minZ, width, depth);
                        for (int z = 0; z < depth; z++)
                        for (int x = 0; x < width; x++)
                        {
                            var point = new Vector2(origin.x + (minX + x + .5f) * data.size.x / resolution,
                                origin.z + (minZ + z + .5f) * data.size.z / resolution);
                            if (bounds.Contains(point)) holes[z, x] = false;
                        }
                        data.SetHoles(minX, minZ, holes);
                        continue;
                    }
                    var heights = data.GetHeights(minX, minZ, width, depth);
                    for (int z = 0; z < depth; z++)
                    for (int x = 0; x < width; x++)
                    {
                        float wx = origin.x + (minX + x) * data.size.x / resolution;
                        float wz = origin.z + (minZ + z) * data.size.z / resolution;
                        float dx = Mathf.Max(bounds.xMin - wx, 0, wx - bounds.xMax);
                        float dz = Mathf.Max(bounds.yMin - wz, 0, wz - bounds.yMax);
                        float distance = Mathf.Sqrt(dx * dx + dz * dz);
                        float weight = patch.transition > 0 ? 1 - Mathf.SmoothStep(0, 1, distance / patch.transition) : distance == 0 ? 1 : 0;
                        float target = patch.height;
                        if (patch.hasSlope) target = Mathf.Lerp(patch.height, patch.endHeight, Mathf.Clamp01((wx - bounds.xMin) / bounds.width));
                        if (source != null) target += source.GetInterpolatedHeight(Mathf.Clamp01((wx - bounds.xMin) / bounds.width), Mathf.Clamp01((wz - bounds.yMin) / bounds.height));
                        heights[z, x] = Mathf.Lerp(heights[z, x], (target - origin.y) / data.size.y, weight);
                    }
                    data.SetHeights(minX, minZ, heights);
                    terrain.Flush();
                }
            }
        }

        /// <summary>새 절개면의 경사와 접근로를 기존 월드 지표로 표현한다.</summary>
        private static void PaintDungeonGround(Terrain[] terrains, CityRecipe recipe)
        {
            foreach (var terrain in terrains)
            {
                var data = terrain.terrainData;
                var layers = data.terrainLayers.Select(AssetDatabase.GetAssetPath).ToArray();
                int grass = Array.IndexOf(layers, recipe.landscape.grassLayer);
                int earth = Array.IndexOf(layers, recipe.landscape.earthLayer);
                int rock = Array.IndexOf(layers, recipe.landscape.rockLayer);
                if (grass < 0 || earth < 0 || rock < 0) throw new InvalidOperationException("던전 지표 레이어 누락");
                var routePatches = recipe.routes.Where(route => route.id.StartsWith("dungeon-")).Select(route => new DungeonTerrainPatch
                {
                    id = "route." + route.id, transition = 6,
                    bounds = new RegionRect { x = route.points.Min(p => p.x), y = route.points.Min(p => p.z),
                        width = Mathf.Max(1, route.points.Max(p => p.x) - route.points.Min(p => p.x)),
                        height = Mathf.Max(1, route.points.Max(p => p.z) - route.points.Min(p => p.z)) }
                });
                foreach (var patch in recipe.landscape.dungeonGround.Concat(routePatches))
                {
                    if (patch.isHole) continue;
                    float margin = patch.transition;
                    var bounds = new Rect(patch.bounds.x - margin, patch.bounds.y - margin,
                        patch.bounds.width + margin * 2, patch.bounds.height + margin * 2);
                    var origin = terrain.transform.position;
                    int minX = Mathf.Clamp(Mathf.FloorToInt((bounds.xMin - origin.x) / data.size.x * data.alphamapWidth), 0, data.alphamapWidth - 1);
                    int minZ = Mathf.Clamp(Mathf.FloorToInt((bounds.yMin - origin.z) / data.size.z * data.alphamapHeight), 0, data.alphamapHeight - 1);
                    int maxX = Mathf.Clamp(Mathf.CeilToInt((bounds.xMax - origin.x) / data.size.x * data.alphamapWidth), 0, data.alphamapWidth - 1);
                    int maxZ = Mathf.Clamp(Mathf.CeilToInt((bounds.yMax - origin.z) / data.size.z * data.alphamapHeight), 0, data.alphamapHeight - 1);
                    var weights = data.GetAlphamaps(minX, minZ, maxX - minX + 1, maxZ - minZ + 1);
                    for (int z = 0; z < weights.GetLength(0); z++)
                    for (int x = 0; x < weights.GetLength(1); x++)
                    {
                        float u = (minX + x) / (float)(data.alphamapWidth - 1);
                        float v = (minZ + z) / (float)(data.alphamapHeight - 1);
                        var position = origin + new Vector3(u * data.size.x, 0, v * data.size.z);
                        float dx = Mathf.Max(patch.bounds.x - position.x, 0, position.x - patch.bounds.x - patch.bounds.width);
                        float dz = Mathf.Max(patch.bounds.y - position.z, 0, position.z - patch.bounds.y - patch.bounds.height);
                        float distance = Mathf.Sqrt(dx * dx + dz * dz);
                        float blend = margin > 0 ? 1 - Mathf.SmoothStep(0, 1, distance / margin) : distance == 0 ? 1 : 0;
                        float stone = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(22, 42, data.GetSteepness(u, v)));
                        float pathDistance = recipe.routes.Where(route => route.id.StartsWith("dungeon-"))
                            .Select(route => GetLineDistance(position, route.points, out _) - route.halfWidth).DefaultIfEmpty(float.MaxValue).Min();
                        float soil = Mathf.Lerp(.12f, 1, 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-.6f, 1, pathDistance)));
                        if (patch.id.StartsWith("route.")) blend *= 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-.6f, 1, pathDistance));
                        for (int layer = 0; layer < data.alphamapLayers; layer++) weights[z, x, layer] *= 1 - blend;
                        weights[z, x, grass] += blend * (1 - stone) * (1 - soil);
                        weights[z, x, earth] += blend * (1 - stone) * soil;
                        weights[z, x, rock] += blend * stone;
                    }
                    data.SetAlphamaps(minX, minZ, weights);
                }
            }
        }
    }
}
