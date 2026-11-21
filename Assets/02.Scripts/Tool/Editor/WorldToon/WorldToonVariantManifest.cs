using System;
using System.Collections.Generic;

namespace UPlayGround.Tool.Editor.WorldToon
{
    /// <summary>WorldToon 프리셋.</summary>
    public enum WorldToonPreset
    {
        MeshLite,
        MeshStandard,
        MeshFar,
        FoliageNear,
        FoliageFar,
        Terrain4,
    }

    /// <summary>프리셋별 셰이더·local 키워드 조합의 단일 기준. 이 표에 없는 조합의 재질은 생성하지 않는다.</summary>
    public static class WorldToonVariantManifest
    {
        public const string MeshShader = "UPlayGround/WorldToon/Mesh";
        public const string FoliageShader = "UPlayGround/WorldToon/Foliage";
        public const string Terrain4Shader = "UPlayGround/WorldToon/Terrain4";

        public const string AlphaTestKeyword = "_ALPHATEST_ON";
        public const string ReceiveShadowsOffKeyword = "_RECEIVE_SHADOWS_OFF";
        public const string AdditionalLightsKeyword = "_WT_ADDITIONAL_LIGHTS";
        public const string WindKeyword = "_WT_WIND";
        // Foliage 재질별 선택 키워드: 원본 분석 결과(정점 색 없음·접촉 변위 사용)로 변환기가 켠다.
        public const string WindHeightKeyword = "_WT_WIND_HEIGHT";
        public const string InteractionKeyword = "_WT_INTERACTION";
        public const string GradientKeyword = "_WT_GRADIENT";
        private static readonly string[] FoliageOptionalKeywords = { WindHeightKeyword, InteractionKeyword, GradientKeyword };

        /// <summary>프리셋이 환경 반응 계약을 가진 셰이더(Foliage)를 쓰는지.</summary>
        public static bool SupportsEnvironmentReaction(WorldToonPreset preset)
        {
            return Get(preset).ShaderName == FoliageShader;
        }

        /// <summary>프리셋 하나가 쓰는 셰이더와 고정 키워드.</summary>
        public readonly struct Entry
        {
            public readonly WorldToonPreset Preset;
            public readonly string ShaderName;
            public readonly string[] Keywords;
            public readonly bool AllowsCutout;

            public Entry(WorldToonPreset preset, string shaderName, bool allowsCutout, params string[] keywords)
            {
                Preset = preset;
                ShaderName = shaderName;
                AllowsCutout = allowsCutout;
                Keywords = keywords;
            }
        }

        private static readonly Entry[] Entries =
        {
            new Entry(WorldToonPreset.MeshLite, MeshShader, true),
            new Entry(WorldToonPreset.MeshStandard, MeshShader, true, AdditionalLightsKeyword),
            new Entry(WorldToonPreset.MeshFar, MeshShader, true, ReceiveShadowsOffKeyword),
            new Entry(WorldToonPreset.FoliageNear, FoliageShader, true, WindKeyword),
            new Entry(WorldToonPreset.FoliageFar, FoliageShader, true, ReceiveShadowsOffKeyword),
            new Entry(WorldToonPreset.Terrain4, Terrain4Shader, false),
        };

        /// <summary>프리셋의 매니페스트 항목을 반환한다.</summary>
        public static Entry Get(WorldToonPreset preset)
        {
            foreach (Entry entry in Entries)
            {
                if (entry.Preset == preset)
                    return entry;
            }
            throw new ArgumentOutOfRangeException(nameof(preset), preset, "매니페스트에 없는 프리셋");
        }

        /// <summary>WorldToon 셰이더가 허용하는 local 키워드 전체(검증·스트리핑 보고용).</summary>
        public static IReadOnlyCollection<string> AllowedKeywords(string shaderName)
        {
            var set = new HashSet<string>();
            foreach (Entry entry in Entries)
            {
                if (entry.ShaderName != shaderName)
                    continue;
                foreach (string keyword in entry.Keywords)
                    set.Add(keyword);
                if (entry.AllowsCutout)
                    set.Add(AlphaTestKeyword);
            }
            if (shaderName == FoliageShader)
            {
                foreach (string keyword in FoliageOptionalKeywords)
                    set.Add(keyword);
            }
            return set;
        }

        /// <summary>WorldToon 셰이더 이름인지 반환한다.</summary>
        public static bool IsWorldToonShader(string shaderName)
        {
            return shaderName == MeshShader || shaderName == FoliageShader || shaderName == Terrain4Shader;
        }
    }
}
