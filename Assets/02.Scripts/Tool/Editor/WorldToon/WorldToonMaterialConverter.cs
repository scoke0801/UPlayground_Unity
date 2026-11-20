using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UPlayGround.Data.Rendering;

namespace UPlayGround.Tool.Editor.WorldToon
{
    /// <summary>복제 재질 생성·스타일 기록·슬롯 적용·복구. 원본 재질과 모델은 어떤 경우에도 수정하지 않는다.</summary>
    public static class WorldToonMaterialConverter
    {
        public const string DefaultOutputRoot = "Assets/11.Material/WorldToon";

        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int CutoffId = Shader.PropertyToID("_Cutoff");
        private static readonly int CullId = Shader.PropertyToID("_Cull");

        /// <summary>복제 결과.</summary>
        public readonly struct CopyResult
        {
            public readonly Material Material;
            public readonly bool IsConflict;
            public readonly bool IsReused;
            public readonly string Message;

            public CopyResult(Material material, bool isConflict, bool isReused, string message)
            {
                Material = material;
                IsConflict = isConflict;
                IsReused = isReused;
                Message = message;
            }
        }

        /// <summary>원본+프리셋에 대응하는 복제 재질을 만들거나 재사용한다. 사람이 고친 생성본은 덮어쓰지 않는다.</summary>
        public static CopyResult CreateCopy(
            WorldToonMaterialAnalysis analysis,
            WorldToonPreset preset,
            WorldToonStyleProfileSO style,
            WorldToonConversionRecord record,
            string outputRoot = DefaultOutputRoot)
        {
            if (analysis.Status == WorldToonConversionStatus.Protected || analysis.Status == WorldToonConversionStatus.Unsupported)
                return new CopyResult(null, false, false, $"{analysis.Status}: 변환 차단 — {string.Join(" / ", analysis.Warnings)}");
            if (preset == WorldToonPreset.Terrain4)
                return new CopyResult(null, false, false, "WT-TERRAIN-002: Terrain4는 Terrain 전용 Validator 경로만 사용");

            WorldToonVariantManifest.Entry entry = WorldToonVariantManifest.Get(preset);
            Shader shader = Shader.Find(entry.ShaderName);
            if (shader == null)
                return new CopyResult(null, false, false, $"WT-BUILD-001: 셰이더 없음 {entry.ShaderName}");
            if (analysis.IsCutout && !entry.AllowsCutout)
                return new CopyResult(null, false, false, $"WT-MAT-001: {preset}는 Cutout 미허용");

            string sourceId = GlobalObjectId.GetGlobalObjectIdSlow(analysis.Source).ToString();
            string presetId = preset.ToString();
            string targetPath = BuildTargetPath(outputRoot, presetId, analysis.Source.name, sourceId);
            WorldToonGeneratedMaterialEntry existing = record.FindGenerated(sourceId, presetId);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(existing?.targetPath ?? targetPath);

            if (material != null && existing != null && ComputeContentHash(material) != existing.generatedContentHash)
                return new CopyResult(material, true, false, "WT-CONFLICT-001: 생성본이 기록 이후 수정됨 — 자동 덮어쓰기 차단");
            if (material != null && existing == null)
                return new CopyResult(material, true, false, "WT-CONFLICT-001: 기록 없는 같은 경로 재질 존재 — 자동 덮어쓰기 차단");

            bool isReused = material != null;
            if (material == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                material = new Material(shader) { name = Path.GetFileNameWithoutExtension(targetPath) };
                AssetDatabase.CreateAsset(material, targetPath);
            }

            Stamp(material, shader, entry, analysis, style);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);

            if (existing == null)
            {
                existing = new WorldToonGeneratedMaterialEntry { sourceMaterialId = sourceId, presetId = presetId };
                record.generated.Add(existing);
            }
            existing.sourceDependencyHash = WorldToonMaterialAnalyzer.SourceDependencyHash(analysis.Source);
            existing.sourceShaderName = analysis.Source.shader.name;
            existing.sourceKeywords = analysis.Source.shaderKeywords;
            existing.targetPath = AssetDatabase.GetAssetPath(material);
            existing.targetMaterialGuid = AssetDatabase.AssetPathToGUID(existing.targetPath);
            existing.generatedContentHash = ComputeContentHash(material);
            existing.status = analysis.Status.ToString();
            existing.warnings = analysis.Warnings.ToArray();
            existing.toolVersion = WorldToonConversionRecord.ToolVersion;
            return new CopyResult(material, false, isReused, isReused ? "재사용(스타일 재기록)" : "생성");
        }

        /// <summary>프리셋 키워드·표면·스타일을 재질에 기록한다. 매니페스트 밖의 키워드는 남기지 않는다.</summary>
        public static void Stamp(Material material, Shader shader, WorldToonVariantManifest.Entry entry, WorldToonMaterialAnalysis analysis, WorldToonStyleProfileSO style)
        {
            if (material.shader != shader)
                material.shader = shader;

            material.SetTexture(BaseMapId, analysis.BaseTexture);
            material.SetTextureScale(BaseMapId, analysis.BaseScale);
            material.SetTextureOffset(BaseMapId, analysis.BaseOffset);
            material.SetColor(BaseColorId, analysis.BaseColor);
            material.SetFloat(CullId, analysis.Cull);
            material.SetFloat(CutoffId, analysis.Cutoff);
            SetToggle(material, "_AlphaClip", analysis.IsCutout);
            SetToggle(material, "_ReceiveShadows", !HasKeyword(entry, WorldToonVariantManifest.ReceiveShadowsOffKeyword));
            SetToggle(material, "_WTAdditionalLights", HasKeyword(entry, WorldToonVariantManifest.AdditionalLightsKeyword));
            SetToggle(material, "_WTWind", HasKeyword(entry, WorldToonVariantManifest.WindKeyword));
            material.renderQueue = analysis.IsCutout ? (int)RenderQueue.AlphaTest : (int)RenderQueue.Geometry;
            material.SetOverrideTag("RenderType", analysis.IsCutout ? "TransparentCutout" : "Opaque");
            material.enableInstancing = true;
            ApplyStyle(material, style);
            ApplyKeywords(material, shader, entry, analysis.IsCutout);
        }

        // 저장된 재질은 속성 쓰기 중 키워드가 다시 동기화될 수 있어 모든 속성을 쓴 뒤 마지막에 키워드를 확정한다.
        // 셰이더 키워드 공간의 local 키워드만 다루며 매니페스트 밖 키워드는 끈다.
        private static void ApplyKeywords(Material material, Shader shader, WorldToonVariantManifest.Entry entry, bool isCutout)
        {
            var enabled = new System.Collections.Generic.List<UnityEngine.Rendering.LocalKeyword>();
            foreach (string keyword in entry.Keywords)
                enabled.Add(new UnityEngine.Rendering.LocalKeyword(shader, keyword));
            if (isCutout)
                enabled.Add(new UnityEngine.Rendering.LocalKeyword(shader, WorldToonVariantManifest.AlphaTestKeyword));
            material.enabledKeywords = enabled.ToArray();
        }

        /// <summary>스타일 프로파일 값을 재질 상수로 기록한다(전역 uniform 없음).</summary>
        public static void ApplyStyle(Material material, WorldToonStyleProfileSO style)
        {
            if (style == null)
                return;
            SetColorIfPresent(material, "_LitTint", style.litTint);
            SetColorIfPresent(material, "_ShadeTint", style.shadeTint);
            SetFloatIfPresent(material, "_BandThreshold", style.bandThreshold);
            SetFloatIfPresent(material, "_BandSoftness", style.bandSoftness);
            SetFloatIfPresent(material, "_IndirectStrength", style.indirectStrength);
            SetFloatIfPresent(material, "_AdditionalLightStrength", style.additionalLightStrength);
        }

        /// <summary>
        /// 렌더러 슬롯 하나에 생성 재질을 적용하고 기록한다. 렌더러 문맥(스키닝·애니메이터·Terrain)까지 분석해 Ready만 적용한다.
        /// NeedsReview(예: WT-MAT-002 재질 속성 의존)는 사람이 어댑터를 확인한 경우에만 allowNeedsReview로 허용한다.
        /// </summary>
        public static string ApplyToSlot(Renderer renderer, int slot, Material target, WorldToonConversionRecord record, bool allowNeedsReview = false)
        {
            Material[] materials = renderer.sharedMaterials;
            if (slot < 0 || slot >= materials.Length)
                return "슬롯 범위 밖";
            Material source = materials[slot];
            WorldToonMaterialAnalysis analysis = WorldToonMaterialAnalyzer.Analyze(source, renderer);
            if (analysis.Status == WorldToonConversionStatus.Protected)
                return "WT-PROTECT-001: 보호 슬롯 — 적용 차단";
            if (analysis.Status == WorldToonConversionStatus.Unsupported)
                return $"적용 차단 — {string.Join(" / ", analysis.Warnings)}";
            if (analysis.Status == WorldToonConversionStatus.NeedsReview && !allowNeedsReview)
                return $"검토 필요로 적용 차단 — {string.Join(" / ", analysis.Warnings)}";

            string rendererId = GlobalObjectId.GetGlobalObjectIdSlow(renderer).ToString();
            Undo.RecordObject(renderer, "WorldToon 적용");
            materials[slot] = target;
            renderer.sharedMaterials = materials;
            EditorUtility.SetDirty(renderer);

            WorldToonAppliedSlotEntry entry = record.FindApplied(rendererId, slot);
            if (entry == null)
            {
                entry = new WorldToonAppliedSlotEntry { rendererId = rendererId, materialSlot = slot };
                record.applied.Add(entry);
                entry.sourceMaterialId = GlobalObjectId.GetGlobalObjectIdSlow(source).ToString();
            }
            entry.rendererPath = renderer.transform.GetHierarchyPathForReport();
            entry.targetMaterialGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(target));
            entry.status = WorldToonConversionStatus.Applied.ToString();
            return "적용";
        }

        /// <summary>기록된 슬롯만 원본으로 되돌린다. 적용 이후 다른 재질로 바뀐 슬롯은 덮어쓰지 않는다.</summary>
        public static string Rollback(WorldToonAppliedSlotEntry entry)
        {
            if (!GlobalObjectId.TryParse(entry.rendererId, out GlobalObjectId rendererGid))
                return "WT-CONFLICT-001: 렌더러 식별자 해석 실패";
            var renderer = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(rendererGid) as Renderer;
            if (renderer == null)
                return "WT-CONFLICT-001: 렌더러를 찾지 못함(씬 미로드 또는 삭제)";
            if (!GlobalObjectId.TryParse(entry.sourceMaterialId, out GlobalObjectId sourceGid))
                return "WT-CONFLICT-001: 원본 재질 식별자 해석 실패";
            var source = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(sourceGid) as Material;
            if (source == null)
                return "WT-CONFLICT-001: 원본 재질을 찾지 못함";

            Material[] materials = renderer.sharedMaterials;
            if (entry.materialSlot >= materials.Length)
                return "WT-CONFLICT-001: 슬롯 수가 바뀜";
            string currentGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(materials[entry.materialSlot]));
            if (currentGuid != entry.targetMaterialGuid)
                return "WT-CONFLICT-001: 적용 이후 슬롯이 다른 재질로 바뀜 — 복구 차단";

            Undo.RecordObject(renderer, "WorldToon 복구");
            materials[entry.materialSlot] = source;
            renderer.sharedMaterials = materials;
            EditorUtility.SetDirty(renderer);
            entry.status = WorldToonConversionStatus.RolledBack.ToString();
            return "복구";
        }

        /// <summary>생성본의 사람 수정 여부를 판정하는 내용 해시. 셰이더·키워드·큐·직렬화 속성 전체를 포함한다.</summary>
        public static string ComputeContentHash(Material material)
        {
            var builder = new StringBuilder();
            builder.Append(material.shader != null ? material.shader.name : "null").Append('|');
            builder.Append(string.Join(",", material.shaderKeywords)).Append('|').Append(material.renderQueue).Append('|');
            var serialized = new SerializedObject(material);
            SerializedProperty properties = serialized.FindProperty("m_SavedProperties");
            SerializedProperty iterator = properties.Copy();
            SerializedProperty end = properties.GetEndProperty();
            while (iterator.Next(true) && !SerializedProperty.EqualContents(iterator, end))
            {
                switch (iterator.propertyType)
                {
                    case SerializedPropertyType.Float: builder.Append(iterator.floatValue.ToString("R")).Append(';'); break;
                    case SerializedPropertyType.Color: builder.Append(iterator.colorValue).Append(';'); break;
                    case SerializedPropertyType.Vector4: builder.Append(iterator.vector4Value).Append(';'); break;
                    case SerializedPropertyType.Vector2: builder.Append(iterator.vector2Value).Append(';'); break;
                    case SerializedPropertyType.ObjectReference:
                        // InstanceID는 세션마다 바뀌므로 GUID+localId로 비교한다.
                        UnityEngine.Object reference = iterator.objectReferenceValue;
                        if (reference != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(reference, out string guid, out long localId))
                            builder.Append(guid).Append(':').Append(localId);
                        builder.Append(';');
                        break;
                    case SerializedPropertyType.String: builder.Append(iterator.stringValue).Append(';'); break;
                }
            }
            return Hash128.Compute(builder.ToString()).ToString();
        }

        private static string BuildTargetPath(string outputRoot, string presetId, string sourceName, string sourceId)
        {
            string safeName = string.Join("_", sourceName.Split(Path.GetInvalidFileNameChars()));
            string shortId = Hash128.Compute(sourceId).ToString().Substring(0, 8);
            return $"{outputRoot}/{presetId}/{safeName}__{shortId}.mat";
        }

        private static bool HasKeyword(WorldToonVariantManifest.Entry entry, string keyword)
        {
            return System.Array.IndexOf(entry.Keywords, keyword) >= 0;
        }

        private static void SetToggle(Material material, string property, bool value)
        {
            SetFloatIfPresent(material, property, value ? 1f : 0f);
        }

        private static void SetFloatIfPresent(Material material, string property, float value)
        {
            if (material.HasProperty(property))
                material.SetFloat(property, value);
        }

        private static void SetColorIfPresent(Material material, string property, Color value)
        {
            if (material.HasProperty(property))
                material.SetColor(property, value);
        }

        private static string GetHierarchyPathForReport(this Transform transform)
        {
            var builder = new StringBuilder(transform.name);
            for (Transform parent = transform.parent; parent != null; parent = parent.parent)
                builder.Insert(0, parent.name + "/");
            return transform.gameObject.scene.name + ":" + builder;
        }
    }
}
