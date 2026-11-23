using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace UPlayGround.Editor
{
    /// <summary>
    /// 환경 반응 식생 셰이더(UPlayGround/Environment/Vegetation/*)의 불필요한 변종을 빌드 시 제거한다.
    /// 근거: 이 프로젝트의 런타임 씬은 베이크 라이트맵을 쓰지 않는다(조명 데이터가 기본값이거나 비어 있음).
    /// 기본(키워드 전부 꺼짐) 변종과 런타임 기능 키워드(_ENVIRONMENT_REACTION_BUFFER 등)는 건드리지 않는다.
    /// </summary>
    public sealed class VegetationShaderVariantStripper : IPreprocessShaders, IPostprocessBuildWithReport
    {
        private const string ShaderNamePrefix = "UPlayGround/Environment/Vegetation/";

        // 파생 셰이더의 원본. 같은 구조의 Forward 패스라 변종 비용이 크고, 같은 근거(라이트맵 미사용)가 적용된다.
        private static readonly string[] SourceShaderNames =
        {
            "UPlayGround/Foliage/GrassGraph",
            "UPlayGround/Foliage/IdyllicVegetationGraph",
            "UPlayGround/Foliage/LeavesGraph",
            "UPlayGround/Foliage/PlantGraph",
            "Polyart/Dreamscape/Tiling",
            "Polyart/Dreamscape/Foliage/Trunk",
            "Idyllic Fantasy Nature/Surface",
            "Toon/TGV_CustomGrass",
        };

        // 라이트맵 계열 중 이 프로젝트 런타임이 쓰지 않는 키워드만 제거한다.
        // SHADOWMASK·LIGHTMAP_SHADOW_MIXING은 Title 씬의 Mixed 라이트 때문에 남긴다.
        private static readonly string[] StripKeywordNames =
        {
            "LIGHTMAP_ON",
            "DIRLIGHTMAP_COMBINED",
            "DYNAMICLIGHTMAP_ON",
        };

        // 실측용: 어떤 키워드가 남은 변종에 얼마나 들어 있는지 기록한다.
        private static readonly string[] MeasureKeywordNames =
        {
            "LIGHTMAP_ON", "DIRLIGHTMAP_COMBINED", "DYNAMICLIGHTMAP_ON", "LIGHTMAP_SHADOW_MIXING", "SHADOWS_SHADOWMASK",
            "_ENVIRONMENT_REACTION_BUFFER", "_SCREEN_SPACE_OCCLUSION", "_ADDITIONAL_LIGHT_SHADOWS", "_LIGHT_COOKIES",
            "_LIGHT_LAYERS", "_DBUFFER_MRT1", "_DBUFFER_MRT2", "_DBUFFER_MRT3", "_REFLECTION_PROBE_BLENDING",
            "_REFLECTION_PROBE_BOX_PROJECTION", "_REFLECTION_PROBE_ATLAS", "_SHADOWS_SOFT", "_SHADOWS_SOFT_LOW",
            "_SHADOWS_SOFT_MEDIUM", "_SHADOWS_SOFT_HIGH", "_MAIN_LIGHT_SHADOWS", "_MAIN_LIGHT_SHADOWS_CASCADE",
            "_MAIN_LIGHT_SHADOWS_SCREEN", "_ADDITIONAL_LIGHTS", "_ADDITIONAL_LIGHTS_VERTEX", "_CLUSTER_LIGHT_LOOP",
            "EVALUATE_SH_MIXED", "EVALUATE_SH_VERTEX", "DEBUG_DISPLAY", "INSTANCING_ON",
        };

        private static readonly Dictionary<string, int> s_keptByKeyword = new Dictionary<string, int>();
        private static int s_input;
        private static int s_removed;

        public int callbackOrder => 0;

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            if (shader == null || !IsTargetShader(shader.name)) return;

            var stripKeywords = CreateKeywords(shader, StripKeywordNames);
            int input = data.Count;
            for (int i = data.Count - 1; i >= 0; i--)
            {
                if (HasAnyKeyword(data[i].shaderKeywordSet, stripKeywords)) data.RemoveAt(i);
            }

            var measureKeywords = CreateKeywords(shader, MeasureKeywordNames);
            for (int i = 0; i < data.Count; i++)
            {
                for (int k = 0; k < measureKeywords.Length; k++)
                {
                    if (!data[i].shaderKeywordSet.IsEnabled(measureKeywords[k])) continue;
                    s_keptByKeyword.TryGetValue(MeasureKeywordNames[k], out int count);
                    s_keptByKeyword[MeasureKeywordNames[k]] = count + 1;
                }
            }

            int removed = input - data.Count;
            s_input += input;
            s_removed += removed;
            if (removed > 0 || input >= 100)
                Debug.Log($"[VegetationStrip] {shader.name} / {snippet.passName} ({snippet.shaderType}): 입력 {input} → 제거 {removed} → 남음 {data.Count}");
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            if (s_input == 0) return;
            var lines = new List<string>();
            foreach (var pair in s_keptByKeyword) lines.Add($"{pair.Key}={pair.Value}");
            lines.Sort();
            Debug.Log($"[VegetationStrip] 요약: 입력 {s_input}, 제거 {s_removed}. 남은 변종 중 키워드 활성 수: {string.Join(", ", lines)}");
            s_keptByKeyword.Clear();
            s_input = 0;
            s_removed = 0;
        }

        private static bool IsTargetShader(string shaderName)
        {
            if (shaderName.StartsWith(ShaderNamePrefix)) return true;
            return System.Array.IndexOf(SourceShaderNames, shaderName) >= 0;
        }

        private static ShaderKeyword[] CreateKeywords(Shader shader, string[] names)
        {
            var keywords = new ShaderKeyword[names.Length];
            for (int i = 0; i < names.Length; i++) keywords[i] = new ShaderKeyword(shader, names[i]);
            return keywords;
        }

        private static bool HasAnyKeyword(ShaderKeywordSet set, ShaderKeyword[] keywords)
        {
            for (int i = 0; i < keywords.Length; i++)
            {
                if (set.IsEnabled(keywords[i])) return true;
            }
            return false;
        }
    }
}
