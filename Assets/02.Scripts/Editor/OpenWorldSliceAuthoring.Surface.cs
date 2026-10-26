using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UPlayGround.Manager.World;

namespace UPlayGround.Editor
{
    public static partial class OpenWorldSliceAuthoring
    {
        private static void MergeWorldGround(Terrain north, Terrain south, ContinuousWorldController world)
        {
            // 크기와 정점 간격이 다른 Terrain 경계는 높이가 같아도 LOD 균열이 보인다.
            // 원본 샘플을 월드 좌표로 다시 담아 렌더링·충돌면을 하나로 소유한다.
            var sources = new[] { north, south };
            var sourceData = sources.Select(t => t.terrainData).ToArray();
            var origins = sources.Select(t => t.transform.position).ToArray();
            var colors = sourceData.Select(d => d.GetAlphamaps(0, 0, d.alphamapWidth, d.alphamapHeight)).ToArray();
            var layers = sourceData.SelectMany(d => d.terrainLayers).Distinct().ToArray();
            var layerMaps = sourceData.Select(d => d.terrainLayers.Select(l => Array.IndexOf(layers, l)).ToArray()).ToArray();
            Vector3 origin = new Vector3(origins[1].x, 0, origins[1].z);
            Vector3 size = new Vector3(sourceData[1].size.x, sourceData[0].size.y + origins[0].y,
                origins[0].z + sourceData[0].size.z - origin.z);
            var merged = new TerrainData { heightmapResolution = 2049, alphamapResolution = 1024,
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
                heights[z, x] = height / size.y;
            }
            merged.SetHeights(0, 0, heights);
            var weights = new float[merged.alphamapHeight, merged.alphamapWidth, layers.Length];
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
            UnityEngine.Object.DestroyImmediate(south.gameObject);
            world.SurfaceRecoveryTerrains = new[] { north };
            foreach (var region in world.Regions) region.Grounds = new Collider[] { north.GetComponent<TerrainCollider>() };
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
