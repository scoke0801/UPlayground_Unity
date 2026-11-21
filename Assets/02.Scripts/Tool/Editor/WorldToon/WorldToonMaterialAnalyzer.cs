using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UPlayGround.Gameplay.World.ReactiveEnvironment;

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
        /// <summary>원본이 환경 반응 계약(젖음·그을림·연소·연료)을 가진 재질. 같은 계약을 가진 Foliage로만 변환한다.</summary>
        public bool IsReactive;
        /// <summary>렌더러가 반응형 오브젝트에 속한다. 대상 재질이 반응 계약을 가져야만 적용한다.</summary>
        public bool IsReactiveOwned;
        /// <summary>원본 셰이더가 UPG 풀 밟기 변위를 쓴다. 변환본도 접촉 변위를 켠다.</summary>
        public bool UsesContactBend;
        /// <summary>원본 식생 그래프의 위·아래 색 그라데이션. 잎·풀 색의 주 출처라 Foliage가 높이로 재현한다.</summary>
        public bool HasGradient;
        public Color GradientTop = Color.white;
        public Color GradientBottom = Color.white;
        /// <summary>렌더러 메시의 오브젝트 공간 높이 범위(정규화 높이 계산용). 렌더러 없이 분석하면 기본 4m.</summary>
        public float ObjectMinY;
        public float ObjectHeight = 4f;

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

        public const string ReactiveToonShaderName = "UPlayGround/Environment/Reactive Toon";
        private const string ContactBendMarker = "UPG_Foliage";

        // 셰이더 소스가 UPG 접촉 변위 함수를 호출하는지로 판정한다(이름 분기 대신 기능 사용 여부).
        private static bool ShaderSourceUsesContactBend(Shader shader)
        {
            string path = AssetDatabase.GetAssetPath(shader);
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".shader") || !System.IO.File.Exists(path))
                return false;
            return System.IO.File.ReadAllText(path).Contains(ContactBendMarker);
        }

        // 앞쪽이 우선. 뒤쪽은 프로젝트 식생 그래프(Leaves·Trunk·Plant·Grass·Idyllic·Props)의 색 텍스처 이름이다.
        private static readonly string[] BaseTextureProperties =
        {
            "_BaseMap", "_MainTex", "_BaseColorMap", "_Albedo", "_MainTexture",
            "_Color_Map", "_ColorMap", "_Foliage_Map", "_Base_Color", "_Texture",
        };
        private static readonly string[] BaseColorProperties = { "_BaseColor", "_Color", "_MainColor", "_Tint", "_Color_Tint", "_ColorTint" };
        private static readonly string[] CutoffProperties = { "_Cutoff", "_Alpha_Clip", "_MaskClip", "_AlphaClipThreshold", "_Alpha_Cutoff" };
        private static readonly string[] CutoutKeywords = { "_ALPHATEST_ON", "_BUILTIN_ALPHATEST_ON" };
        private static readonly (string Top, string Bottom)[] GradientProperties =
        {
            ("_Foliage_Color_Top", "_Foliage_Color_Bottom"), ("_ColorTop", "_ColorBottom"), ("_Color_Top", "_Color_Bottom"),
            ("_Top_Color", "_Bottom_Color"), ("_Color_Tint_Top", "_Color_Tint_Bottom"),
        };

        private static bool HasPropertyOfType(Material material, string property, ShaderPropertyType type)
        {
            int index = material.shader.FindPropertyIndex(property);
            return index >= 0 && material.shader.GetPropertyType(index) == type;
        }

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
            // Reactive Toon은 외곽선·균열·역할(Body/Interior) 표현까지 가진 툰 셰이더라 Foliage로 대체하지 않는다.
            if (shaderName == ReactiveToonShaderName)
            {
                analysis.Raise(WorldToonConversionStatus.Protected, $"WT-PROTECT-002: Reactive Toon 유지 {shaderName}");
                return analysis;
            }
            // 반응 계약을 가진 식생 파생 셰이더는 같은 계약을 가진 WorldToon Foliage로만 변환할 수 있다(CreateCopy가 강제).
            // 판정은 런타임 ReactiveEnvironmentSurface와 같은 함수를 쓴다.
            analysis.IsReactive = ReactiveEnvironmentSurface.SupportsReaction(source);
            analysis.UsesContactBend = ShaderSourceUsesContactBend(source.shader);
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
            if (UsesBillboarding(source))
            {
                analysis.Raise(WorldToonConversionStatus.Unsupported, "WT-MAT-001: 임포스터 빌보드 — 시점 회전 계산이 없어 원본 유지");
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

        private static bool UsesBillboarding(Material material)
        {
            foreach (string keyword in material.shaderKeywords)
            {
                if (keyword.Contains("BILLBOARD"))
                    return true;
            }
            return false;
        }

        private static void ReadBaseInputs(Material source, WorldToonMaterialAnalysis analysis)
        {
            foreach (string property in BaseTextureProperties)
            {
                if (!HasPropertyOfType(source, property, ShaderPropertyType.Texture) || source.GetTexture(property) == null)
                    continue;
                analysis.BaseTexture = source.GetTexture(property);
                analysis.BaseScale = source.GetTextureScale(property);
                analysis.BaseOffset = source.GetTextureOffset(property);
                break;
            }
            foreach (string property in BaseColorProperties)
            {
                if (!HasPropertyOfType(source, property, ShaderPropertyType.Color))
                    continue;
                analysis.BaseColor = source.GetColor(property);
                break;
            }
            foreach ((string top, string bottom) in GradientProperties)
            {
                if (!HasPropertyOfType(source, top, ShaderPropertyType.Color) || !HasPropertyOfType(source, bottom, ShaderPropertyType.Color))
                    continue;
                analysis.HasGradient = true;
                analysis.GradientTop = source.GetColor(top);
                analysis.GradientBottom = source.GetColor(bottom);
                break;
            }
            if (analysis.BaseTexture == null)
                analysis.Warnings.Add("INFO: BaseMap 없음 → White 텍스처와 Base Color로 변환");
        }

        private static void ReadSurfaceInputs(Material source, WorldToonMaterialAnalysis analysis)
        {
            bool clipFlag = source.HasProperty("_AlphaClip") && source.GetFloat("_AlphaClip") > 0.5f;
            bool clipKeyword = false;
            foreach (string keyword in CutoutKeywords)
                clipKeyword |= source.IsKeywordEnabled(keyword);
            bool cutoutQueue = source.renderQueue >= (int)RenderQueue.AlphaTest && source.renderQueue < (int)RenderQueue.Transparent;
            analysis.IsCutout = clipFlag || clipKeyword || cutoutQueue;
            foreach (string property in CutoffProperties)
            {
                if (!HasPropertyOfType(source, property, ShaderPropertyType.Float) && !HasPropertyOfType(source, property, ShaderPropertyType.Range))
                    continue;
                float value = source.GetFloat(property);
                // 일부 그래프의 _Alpha_Clip은 0/1 토글이다. 1이면 임계값이 아니므로 URP 기본 0.5를 쓴다.
                analysis.Cutoff = value >= 0.99f ? 0.5f : value;
                break;
            }
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
            // 반응형 오브젝트는 모든 Renderer·슬롯이 반응 호환 재질이어야 검증을 통과한다(ReactiveEnvironmentSurface.Validate).
            // 차단 여부는 대상 재질을 아는 ApplyToSlot이 정한다.
            analysis.IsReactiveOwned = renderer.GetComponentInParent<ReactiveEnvironmentObject>(true) != null;
            Bounds local = renderer.localBounds;
            if (local.size.y > 0.01f)
            {
                analysis.ObjectMinY = local.min.y;
                analysis.ObjectHeight = local.size.y;
            }
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
