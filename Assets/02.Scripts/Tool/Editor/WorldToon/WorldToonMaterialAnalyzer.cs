using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace UPlayGround.Tool.Editor.WorldToon
{
    /// <summary>변환 가능 상태.</summary>
    public enum WorldToonConversionStatus
    {
        Ready,
        NeedsReview,
        Unsupported,
        Protected,
        Applied,
        RolledBack,
    }

    /// <summary>원본 재질 하나의 읽기 전용 분석 결과.</summary>
    public sealed class WorldToonMaterialAnalysis
    {
        public Material Source;
        public WorldToonConversionStatus Status = WorldToonConversionStatus.Ready;
        public readonly List<string> Warnings = new List<string>();
        public Texture BaseTexture;
        public Vector2 BaseScale = Vector2.one;
        public Vector2 BaseOffset = Vector2.zero;
        public Color BaseColor = Color.white;
        public bool IsCutout;
        public float Cutoff = 0.5f;
        public float Cull = (float)CullMode.Back;

        /// <summary>상태를 더 심각한 쪽으로만 올리고 경고 코드를 남긴다.</summary>
        public void Raise(WorldToonConversionStatus status, string warning)
        {
            Warnings.Add(warning);
            if (Severity(status) > Severity(Status))
                Status = status;
        }

        private static int Severity(WorldToonConversionStatus status)
        {
            switch (status)
            {
                case WorldToonConversionStatus.Protected: return 3;
                case WorldToonConversionStatus.Unsupported: return 2;
                case WorldToonConversionStatus.NeedsReview: return 1;
                default: return 0;
            }
        }
    }

    /// <summary>원본 재질을 변경하지 않고 WorldToon 변환 가능성과 보호·미지원 사유를 판정한다.</summary>
    public static class WorldToonMaterialAnalyzer
    {
        // 셰이더 이름 접두사 기준 보호 대상. 플레이어·주요 캐릭터·비인간형 몬스터의 lilToon은 전부 여기에 걸린다.
        private static readonly string[] ProtectedShaderPrefixes =
        {
            "lilToon", "Hidden/lilToon", "_lil/", "Hidden/ltspass",
            "UPlayGround/Environment/ToonReactive",
        };

        // 범위 밖(물·하늘·파티클·UI·식생 그래프·지형) 셰이더. Mesh 변환 대상이 아니다.
        private static readonly string[] OutOfScopeShaderPrefixes =
        {
            "UPlayGround/Environment/Lake", "UPlayGround/Water", "UPlayGround/Environment/Water",
            "UPlayGround/Sky", "Skybox/", "Particles/", "Universal Render Pipeline/Particles",
            "Cartoon FX", "Vefects/", "UI/", "Sprites/", "TextMeshPro", "GUI/",
            "UPlayGround/Foliage", "Universal Render Pipeline/Terrain", "Nature/",
            "Hidden/", "UPlayGround/WorldToon",
        };

        private static readonly string[] BaseTextureProperties = { "_BaseMap", "_MainTex", "_BaseColorMap", "_Albedo", "_MainTexture" };
        private static readonly string[] BaseColorProperties = { "_BaseColor", "_Color", "_MainColor", "_Tint" };

        /// <summary>재질 하나를 분석한다. renderer가 있으면 렌더러 문맥(스키닝·애니메이션)도 검사한다.</summary>
        public static WorldToonMaterialAnalysis Analyze(Material source, Renderer renderer = null)
        {
            var analysis = new WorldToonMaterialAnalysis { Source = source };
            if (source == null || source.shader == null)
            {
                analysis.Raise(WorldToonConversionStatus.Unsupported, "WT-MAT-001: 재질 또는 셰이더 없음");
                return analysis;
            }

            string shaderName = source.shader.name;
            if (StartsWithAny(shaderName, ProtectedShaderPrefixes))
            {
                analysis.Raise(WorldToonConversionStatus.Protected, $"WT-PROTECT-001: 보호 셰이더 {shaderName}");
                return analysis;
            }
            if (StartsWithAny(shaderName, OutOfScopeShaderPrefixes))
            {
                analysis.Raise(WorldToonConversionStatus.Unsupported, $"WT-MAT-001: 범위 밖 셰이더 {shaderName}");
                return analysis;
            }
            if (IsTransparent(source))
            {
                analysis.Raise(WorldToonConversionStatus.Unsupported, "WT-MAT-001: Transparent는 v1 미지원(원본 유지)");
                return analysis;
            }

            ReadBaseInputs(source, analysis);
            ReadSurfaceInputs(source, analysis);
            InspectUnsupportedFeatures(source, analysis);
            if (renderer != null)
                InspectRendererContext(renderer, analysis);
            return analysis;
        }

        private static bool StartsWithAny(string value, string[] prefixes)
        {
            foreach (string prefix in prefixes)
            {
                if (value.StartsWith(prefix, System.StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        // 렌더 큐만 보지 않고 URP의 _Surface·블렌드 상태까지 확인한다.
        private static bool IsTransparent(Material material)
        {
            if (material.HasProperty("_Surface") && material.GetFloat("_Surface") > 0.5f)
                return true;
            if (material.GetTag("RenderType", false) == "Transparent")
                return true;
            if (material.HasProperty("_DstBlend") && material.GetFloat("_DstBlend") != (float)BlendMode.Zero)
                return true;
            return material.renderQueue >= (int)RenderQueue.Transparent;
        }

        private static void ReadBaseInputs(Material source, WorldToonMaterialAnalysis analysis)
        {
            foreach (string property in BaseTextureProperties)
            {
                if (!source.HasProperty(property))
                    continue;
                analysis.BaseTexture = source.GetTexture(property);
                analysis.BaseScale = source.GetTextureScale(property);
                analysis.BaseOffset = source.GetTextureOffset(property);
                break;
            }
            foreach (string property in BaseColorProperties)
            {
                if (!source.HasProperty(property))
                    continue;
                analysis.BaseColor = source.GetColor(property);
                break;
            }
            if (analysis.BaseTexture == null)
                analysis.Warnings.Add("INFO: BaseMap 없음 → White 텍스처와 Base Color로 변환");
        }

        private static void ReadSurfaceInputs(Material source, WorldToonMaterialAnalysis analysis)
        {
            bool clipFlag = source.HasProperty("_AlphaClip") && source.GetFloat("_AlphaClip") > 0.5f;
            bool clipKeyword = source.IsKeywordEnabled("_ALPHATEST_ON");
            bool cutoutQueue = source.renderQueue >= (int)RenderQueue.AlphaTest && source.renderQueue < (int)RenderQueue.Transparent;
            analysis.IsCutout = clipFlag || clipKeyword || cutoutQueue;
            if (source.HasProperty("_Cutoff"))
                analysis.Cutoff = source.GetFloat("_Cutoff");
            if (source.HasProperty("_Cull"))
                analysis.Cull = source.GetFloat("_Cull");
            if (analysis.Cull == (float)CullMode.Off)
                analysis.Warnings.Add("INFO: 원본이 양면(Cull Off) — 그대로 유지");
        }

        private static void InspectUnsupportedFeatures(Material source, WorldToonMaterialAnalysis analysis)
        {
            bool hasEmission = source.IsKeywordEnabled("_EMISSION")
                && source.HasProperty("_EmissionColor")
                && source.GetColor("_EmissionColor").maxColorComponent > 0.001f;
            if (hasEmission)
                analysis.Raise(WorldToonConversionStatus.NeedsReview, "WT-MAT-001: Emission 사용 — v1 제외 기능, 외형 확인 필요");

            if (source.HasProperty("_BumpMap") && source.GetTexture("_BumpMap") != null)
                analysis.Warnings.Add("INFO: Normal Map 사용 — v1 Mesh는 제외(Standard 승인 확장 전)");

            bool isMetal = source.HasProperty("_Metallic") && source.GetFloat("_Metallic") > 0.5f;
            if (isMetal)
                analysis.Raise(WorldToonConversionStatus.NeedsReview, "WT-MAT-001: 금속 반사 재질 — 반사 표현 손실");
        }

        // 스키닝·재질 애니메이션은 피격색·디졸브 등 속성 의존이 있을 수 있어 어댑터 검증 전 자동 적용하지 않는다.
        private static void InspectRendererContext(Renderer renderer, WorldToonMaterialAnalysis analysis)
        {
            if (renderer is SkinnedMeshRenderer)
                analysis.Raise(WorldToonConversionStatus.NeedsReview, "WT-MAT-002: 스키닝 렌더러 — 재질 속성 코드·애니메이션 의존 검증 필요");
            if (renderer.GetComponentInParent<Animator>() != null || renderer.GetComponentInParent<UnityEngine.Animation>() != null)
                analysis.Raise(WorldToonConversionStatus.NeedsReview, "WT-MAT-002: 애니메이터 하위 — 재질 프로퍼티 애니메이션 확인 필요");
            if (renderer.GetComponentInParent<Terrain>() != null)
                analysis.Raise(WorldToonConversionStatus.Unsupported, "WT-TERRAIN-002: Terrain은 일반 변환기로 처리하지 않음");
        }

        /// <summary>에셋 경로 기준으로 원본 재질 의존성 해시를 반환한다(모델 내장 재질은 모델 파일 기준).</summary>
        public static string SourceDependencyHash(Material source)
        {
            string path = AssetDatabase.GetAssetPath(source);
            return string.IsNullOrEmpty(path) ? string.Empty : AssetDatabase.GetAssetDependencyHash(path).ToString();
        }
    }
}
