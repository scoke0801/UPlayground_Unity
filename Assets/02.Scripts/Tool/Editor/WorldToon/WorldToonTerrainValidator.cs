using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Rendering;

namespace UPlayGround.Tool.Editor.WorldToon
{
    /// <summary>Terrain 하나의 Terrain4 적용 가능성 판정.</summary>
    public sealed class WorldToonTerrainAnalysis
    {
        public Terrain Terrain;
        public WorldToonConversionStatus Status = WorldToonConversionStatus.Ready;
        public int LayerCount;
        public bool HasHoles;
        public readonly List<string> Warnings = new List<string>();
    }

    /// <summary>
    /// Terrain4 전용 검사·적용. TerrainData·TerrainLayer·원본 텍스처는 읽기만 하고 수정하지 않는다.
    /// 적용은 Terrain 컴포넌트의 materialTemplate 참조만 복제 재질로 바꾼다.
    /// </summary>
    public static class WorldToonTerrainValidator
    {
        public const int MaxLayers = 4;

        /// <summary>레이어 수·레이어 기능 의존·현재 재질을 검사한다.</summary>
        public static WorldToonTerrainAnalysis Analyze(Terrain terrain)
        {
            var analysis = new WorldToonTerrainAnalysis { Terrain = terrain };
            TerrainData data = terrain != null ? terrain.terrainData : null;
            if (data == null)
            {
                Raise(analysis, WorldToonConversionStatus.Unsupported, "WT-TERRAIN-002: TerrainData 없음");
                return analysis;
            }

            TerrainLayer[] layers = data.terrainLayers;
            analysis.LayerCount = layers.Length;
            analysis.HasHoles = !data.enableHolesTextureCompression || HasAnyHole(data);
            if (layers.Length > MaxLayers)
                Raise(analysis, WorldToonConversionStatus.Unsupported, $"WT-TERRAIN-001: TerrainLayer {layers.Length}개(>4) — 원본 Terrain Lit 유지");
            if (layers.Length == 0)
                Raise(analysis, WorldToonConversionStatus.Unsupported, "WT-TERRAIN-002: TerrainLayer 없음");

            foreach (TerrainLayer layer in layers)
                InspectLayer(layer, analysis);
            InspectTemplate(terrain.materialTemplate, analysis);
            return analysis;
        }

        private static bool HasAnyHole(TerrainData data)
        {
            int resolution = data.holesResolution;
            bool[,] holes = data.GetHoles(0, 0, resolution, resolution);
            foreach (bool solid in holes)
            {
                if (!solid)
                    return true;
            }
            return false;
        }

        // v1은 레이어 Normal/Mask/Height Blend를 쓰지 않는다. 이에 의존하는 지형은 외형이 달라지므로 검토 대상으로 둔다.
        private static void InspectLayer(TerrainLayer layer, WorldToonTerrainAnalysis analysis)
        {
            if (layer == null)
            {
                Raise(analysis, WorldToonConversionStatus.Unsupported, "WT-TERRAIN-002: 비어 있는 TerrainLayer 슬롯");
                return;
            }
            if (layer.diffuseTexture == null)
                Raise(analysis, WorldToonConversionStatus.NeedsReview, $"WT-TERRAIN-002: {layer.name} diffuse 없음");
            if (layer.normalMapTexture != null)
                Raise(analysis, WorldToonConversionStatus.NeedsReview, $"WT-TERRAIN-002: {layer.name} Normal Map 사용 — v1 제외 기능");
            if (layer.maskMapTexture != null)
                Raise(analysis, WorldToonConversionStatus.NeedsReview, $"WT-TERRAIN-002: {layer.name} Mask Map 사용 — v1 제외 기능");
        }

        private static void InspectTemplate(Material template, WorldToonTerrainAnalysis analysis)
        {
            if (template == null)
            {
                analysis.Warnings.Add("INFO: materialTemplate 없음(내장 기본 Terrain 재질)");
                return;
            }
            string shaderName = template.shader != null ? template.shader.name : "null";
            if (shaderName == WorldToonVariantManifest.Terrain4Shader)
                return;
            if (shaderName != "Universal Render Pipeline/Terrain/Lit")
                Raise(analysis, WorldToonConversionStatus.NeedsReview, $"WT-TERRAIN-002: 기존 커스텀 지형 셰이더 {shaderName} — 기능 대응 확인 필요");
            if (template.HasProperty("_EnableHeightBlend") && template.GetFloat("_EnableHeightBlend") > 0.5f)
                Raise(analysis, WorldToonConversionStatus.NeedsReview, "WT-TERRAIN-002: Height Blend 사용 — v1 제외 기능");
        }

        private static void Raise(WorldToonTerrainAnalysis analysis, WorldToonConversionStatus status, string warning)
        {
            analysis.Warnings.Add(warning);
            if (Rank(status) > Rank(analysis.Status))
                analysis.Status = status;
        }

        private static int Rank(WorldToonConversionStatus status)
        {
            return status == WorldToonConversionStatus.Unsupported ? 2 : status == WorldToonConversionStatus.NeedsReview ? 1 : 0;
        }

        /// <summary>Ready인 Terrain에 Terrain4 복제 재질을 만들어 연결한다. 기존 템플릿은 반환해 복구에 쓴다.</summary>
        public static string Apply(Terrain terrain, WorldToonStyleProfileSO style, string outputRoot, out Material previousTemplate)
        {
            previousTemplate = terrain.materialTemplate;
            WorldToonTerrainAnalysis analysis = Analyze(terrain);
            if (analysis.Status != WorldToonConversionStatus.Ready)
                return $"{analysis.Status}: 적용 차단 — {string.Join(" / ", analysis.Warnings)}";

            Shader shader = Shader.Find(WorldToonVariantManifest.Terrain4Shader);
            if (shader == null)
                return "WT-BUILD-001: Terrain4 셰이더 없음";

            string path = $"{outputRoot}/Terrain4/{terrain.name}__Terrain4.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
                material = new Material(shader) { name = System.IO.Path.GetFileNameWithoutExtension(path) };
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader)
            {
                return "WT-CONFLICT-001: 같은 경로에 다른 셰이더 재질 존재";
            }
            material.enableInstancing = true;
            WorldToonMaterialConverter.ApplyStyle(material, style);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);

            Undo.RecordObject(terrain, "WorldToon Terrain4 적용");
            terrain.materialTemplate = material;
            EditorUtility.SetDirty(terrain);
            return "적용";
        }
    }
}
