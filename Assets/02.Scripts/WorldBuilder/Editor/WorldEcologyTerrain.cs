using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.WorldBuilder.Editor
{
    internal static class WorldEcologyTerrain
    {
        internal sealed class Snapshot
        {
            public readonly Terrain Terrain;
            public readonly TerrainLayer[] Layers;
            public readonly float[,,] Alpha;
            public readonly TreePrototype[] TreePrototypes;
            public readonly TreeInstance[] Trees;
            public readonly DetailPrototype[] DetailPrototypes;
            public readonly int[][,] Details;
            public readonly int DetailResolution, PatchResolution;
            public readonly float TreeDistance, DetailDistance;

            public Snapshot(Terrain terrain)
            {
                Terrain = terrain;
                var data = terrain.terrainData;
                Layers = data.terrainLayers;
                Alpha = data.GetAlphamaps(0, 0, data.alphamapWidth, data.alphamapHeight);
                TreePrototypes = data.treePrototypes;
                Trees = data.treeInstances;
                DetailPrototypes = data.detailPrototypes;
                DetailResolution = data.detailResolution;
                PatchResolution = data.detailResolutionPerPatch;
                Details = new int[DetailPrototypes.Length][,];
                for (int i = 0; i < Details.Length; i++) Details[i] = data.GetDetailLayer(0, 0, data.detailWidth, data.detailHeight, i);
                TreeDistance = terrain.treeDistance;
                DetailDistance = terrain.detailObjectDistance;
            }

            public void Restore()
            {
                Write(Terrain, Layers, Alpha, TreePrototypes, Trees, DetailPrototypes, Details, DetailResolution, PatchResolution, TreeDistance, DetailDistance);
                AssetDatabase.SaveAssetIfDirty(Terrain.terrainData);
            }
        }

        public static List<Snapshot> Capture(Terrain[,] terrains)
        {
            var before = new List<Snapshot>();
            foreach (var terrain in terrains) before.Add(new Snapshot(terrain));
            return before;
        }

        public static void Apply(Terrain[,] terrains, WorldVegetationEvaluation output, int detailResolution, float treeDistance, float detailDistance)
        {
            for (int z = 0; z < terrains.GetLength(0); z++)
            for (int x = 0; x < terrains.GetLength(1); x++)
            {
                var terrain = terrains[z, x];
                Undo.RegisterCompleteObjectUndo(new Object[] { terrain, terrain.terrainData }, "생태 지표·식생");
                Undo.RegisterCompleteObjectUndo(terrain.terrainData.alphamapTextures, "지표 가중치");
                var tile = output.Tiles[z, x];
                Write(terrain, output.Layers, tile.Alpha, output.TreePrototypes, tile.Trees.ToArray(), output.DetailPrototypes, tile.Details,
                    detailResolution, 8, treeDistance, detailDistance);
            }
        }

        private static void Write(Terrain terrain, TerrainLayer[] layers, float[,,] alpha, TreePrototype[] treePrototypes, TreeInstance[] trees,
            DetailPrototype[] details, int[][,] maps, int resolution, int patchSize, float treeDistance, float detailDistance)
        {
            var data = terrain.terrainData;
            if (data.alphamapResolution != alpha.GetLength(0)) data.alphamapResolution = alpha.GetLength(0);
            data.terrainLayers = layers;
            if (layers.Length > 0) data.SetAlphamaps(0, 0, alpha);
            data.treePrototypes = treePrototypes;
            data.SetTreeInstances(trees, false);
            if (data.detailResolution != resolution || data.detailResolutionPerPatch != patchSize)
                data.SetDetailResolution(resolution, Mathf.Max(8, patchSize));
            data.detailPrototypes = details;
            for (int i = 0; i < maps.Length; i++) data.SetDetailLayer(0, 0, i, maps[i]);
            terrain.treeDistance = treeDistance;
            terrain.detailObjectDistance = detailDistance;
            terrain.Flush();
            EditorUtility.SetDirty(data);
            EditorUtility.SetDirty(terrain);
        }

        public static string Hash(Terrain[,] terrains)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            foreach (var tile in Capture(terrains))
            {
                writer.Write(tile.Alpha.GetLength(0)); writer.Write(tile.Layers.Length);
                foreach (var layer in tile.Layers) WriteAsset(writer, layer);
                foreach (float weight in tile.Alpha) writer.Write(weight);
                writer.Write(tile.TreeDistance); writer.Write(tile.DetailDistance);
                writer.Write(tile.TreePrototypes.Length);
                foreach (var tree in tile.TreePrototypes) { WriteAsset(writer, tree.prefab); writer.Write(tree.bendFactor); }
                writer.Write(tile.Trees.Length);
                foreach (var tree in tile.Trees)
                {
                    writer.Write(tree.prototypeIndex); writer.Write(tree.position.x); writer.Write(tree.position.y); writer.Write(tree.position.z);
                    writer.Write(tree.heightScale); writer.Write(tree.widthScale); writer.Write(tree.rotation);
                    Color32 color = tree.color, light = tree.lightmapColor;
                    writer.Write(color.r); writer.Write(color.g); writer.Write(color.b); writer.Write(color.a);
                    writer.Write(light.r); writer.Write(light.g); writer.Write(light.b); writer.Write(light.a);
                }
                writer.Write(tile.DetailResolution); writer.Write(tile.PatchResolution); writer.Write(tile.Details.Length);
                for (int i = 0; i < tile.Details.Length; i++)
                {
                    var detail = tile.DetailPrototypes[i];
                    WriteAsset(writer, detail.prototype); WriteAsset(writer, detail.prototypeTexture);
                    writer.Write(detail.usePrototypeMesh); writer.Write(detail.useInstancing); writer.Write((int)detail.renderMode);
                    writer.Write(detail.minHeight); writer.Write(detail.maxHeight); writer.Write(detail.minWidth); writer.Write(detail.maxWidth);
                    writer.Write(detail.noiseSeed); writer.Write(detail.noiseSpread); writer.Write(detail.density);
                    writer.Write(detail.alignToGround); writer.Write(detail.positionJitter); writer.Write(detail.holeEdgePadding);
                    writer.Write(detail.healthyColor.ToString("R")); writer.Write(detail.dryColor.ToString("R"));
                    foreach (int density in tile.Details[i]) writer.Write(density);
                }
            }
            writer.Flush();
            return Data.WorldDefinition.HashBytes(stream.ToArray());
        }

        private static void WriteAsset(BinaryWriter writer, Object asset)
        {
            if (asset != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long id))
            {
                writer.Write(guid);
                writer.Write(id);
                return;
            }
            writer.Write("");
            writer.Write(0L);
        }
    }
}

