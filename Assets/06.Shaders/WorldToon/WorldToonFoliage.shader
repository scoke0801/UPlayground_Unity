// WorldToon Foliage — 나무 잎·줄기·덤불·풀의 경량 셰이더. Cutout + 양면 선택 + 정점 바람 + 풀 밟기 + 환경 반응.
// 프리셋: FoliageNear = _WT_WIND, FoliageFar = _RECEIVE_SHADOWS_OFF(바람 없음). 각 × _ALPHATEST_ON.
// 위치 변형(바람·밟기)과 연료 소거는 Forward·ShadowCaster·DepthOnly·DepthNormals·MotionVectors가 같은 함수를 쓴다.
// 환경 반응 계약(_EnvironmentReactionSupported·_EnvState*·_FuelCoverage·공유 버퍼)은 기존 식생 파생 셰이더와 같다.
// 기존 파생 셰이더(PBR·그래프) 대비 생략: 스페큘러·환경 반사·노멀맵·디테일맵·그래프별 색 변주.
Shader "UPlayGround/WorldToon/Foliage"
{
    Properties
    {
        [HideInInspector] _EnvironmentReactionSupported("환경 반응 계약", Float) = 1
        [HideInInspector] _EnvironmentReactionBufferSupported("공유 표현 버퍼", Float) = 1
        [PerRendererData] [HideInInspector] _EnvironmentReactionIndex("표현 상태 인덱스", Float) = -1
        [HideInInspector] _EnvState0("젖음 그을림 손상 연소", Vector) = (0,0,0,0)
        [HideInInspector] _EnvState1("열", Vector) = (0,0,0,0)
        [HideInInspector] _FuelCoverage("잔여 연료", Float) = 1
        [HideInInspector] _EnvironmentCellMask("지형 상태", 2D) = "black" {}
        [HideInInspector] _EnvironmentCellBounds("지형 범위", Vector) = (0,0,1,1)
        [HideInInspector] _UseEnvironmentCells("지형 상태 사용", Float) = 0

        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)

        [Header(Toon Style)]
        _LitTint("Lit Tint", Color) = (1, 1, 1, 1)
        _ShadeTint("Shade Tint", Color) = (0.1, 0.1, 0.1, 1)
        _BandThreshold("Band Threshold", Range(-1, 1)) = 0.2
        _BandSoftness("Band Softness", Range(0.001, 0.5)) = 0.4
        _IndirectStrength("Indirect Strength", Range(0, 2)) = 1
        _AdditionalLightStrength("Additional Light Strength", Range(0, 2)) = 1

        [Header(Foliage)]
        _UpNormalBlend("Up Normal Blend", Range(0, 1)) = 0.35
        _Translucency("Back Translucency", Range(0, 1)) = 0.2

        [Header(Wind)]
        [Toggle(_WT_WIND)] _WTWind("Vertex Wind", Float) = 1
        _WindAmplitude("Wind Amplitude (m)", Range(0, 1)) = 0.12
        _WindFrequency("Wind Frequency", Range(0, 6)) = 1.6
        _WindIdle("Idle Sway (no global wind)", Range(0, 1)) = 0.15
        [Toggle(_WT_WIND_HEIGHT)] _WTWindHeight("Sway Weight From Object Height (no vertex color)", Float) = 0
        _HeightScale("Normalized Height Scale (1/m)", Float) = 0.25
        _HeightOffset("Normalized Height Offset", Float) = 0

        [Header(Height Gradient)]
        [Toggle(_WT_GRADIENT)] _WTGradient("Top/Bottom Color Gradient", Float) = 0
        _GradientTop("Gradient Top", Color) = (1, 1, 1, 1)
        _GradientBottom("Gradient Bottom", Color) = (1, 1, 1, 1)
        [Toggle(_WT_INTERACTION)] _WTInteraction("Actor Contact Bend", Float) = 0

        [Header(Environment Reaction)]
        [Toggle] _AllowFuelCoverage("연료 소진 소거", Float) = 0
        _WetTint("젖음 색", Color) = (0.55,0.65,0.7,1)
        _CharTint("그을림 색", Color) = (0.12,0.10,0.08,1)
        _FireColor("연소 발광", Color) = (2,0.4,0.03,1)

        [Header(Surface)]
        [Toggle(_ALPHATEST_ON)] _AlphaClip("Alpha Clip (Cutout)", Float) = 1
        _Cutoff("Alpha Cutoff", Range(0, 1)) = 0.5
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 0
        [ToggleOff(_RECEIVE_SHADOWS_OFF)] _ReceiveShadows("Receive Shadows", Float) = 1

        [HideInInspector] _Surface("__surface", Float) = 0
        [HideInInspector][NoScaleOffset] unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex FoliageForwardVertex
            #pragma fragment FoliageForwardFragment

            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
            #pragma shader_feature_local_fragment _WT_GRADIENT
            #pragma shader_feature_local_vertex _WT_WIND
            #pragma shader_feature_local_vertex _WT_WIND_HEIGHT
            #pragma shader_feature_local_vertex _WT_INTERACTION
            #pragma multi_compile _ _ENVIRONMENT_REACTION_BUFFER
            #pragma target 4.5 _ENVIRONMENT_REACTION_BUFFER

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ USE_LEGACY_LIGHTMAPS
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer

            #define WORLDTOON_FOLIAGE_FORWARD
            #include "Includes/WorldToonFoliageInput.hlsl"
            #include "Includes/WorldToonFoliagePasses.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex FoliageShadowVertex
            #pragma fragment FoliageShadowFragment
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_vertex _WT_WIND
            #pragma shader_feature_local_vertex _WT_WIND_HEIGHT
            #pragma shader_feature_local_vertex _WT_INTERACTION
            #pragma multi_compile _ _ENVIRONMENT_REACTION_BUFFER
            #pragma target 4.5 _ENVIRONMENT_REACTION_BUFFER
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #define WORLDTOON_FOLIAGE_SHADOW
            #include "Includes/WorldToonFoliageInput.hlsl"
            #include "Includes/WorldToonFoliagePasses.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex FoliageDepthVertex
            #pragma fragment FoliageDepthOnlyFragment
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_vertex _WT_WIND
            #pragma shader_feature_local_vertex _WT_WIND_HEIGHT
            #pragma shader_feature_local_vertex _WT_INTERACTION
            #pragma multi_compile _ _ENVIRONMENT_REACTION_BUFFER
            #pragma target 4.5 _ENVIRONMENT_REACTION_BUFFER
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_instancing

            #define WORLDTOON_FOLIAGE_DEPTH
            #include "Includes/WorldToonFoliageInput.hlsl"
            #include "Includes/WorldToonFoliagePasses.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex FoliageDepthVertex
            #pragma fragment FoliageDepthNormalsFragment
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_vertex _WT_WIND
            #pragma shader_feature_local_vertex _WT_WIND_HEIGHT
            #pragma shader_feature_local_vertex _WT_INTERACTION
            #pragma multi_compile _ _ENVIRONMENT_REACTION_BUFFER
            #pragma target 4.5 _ENVIRONMENT_REACTION_BUFFER
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #pragma multi_compile_instancing

            #define WORLDTOON_FOLIAGE_DEPTH
            #include "Includes/WorldToonFoliageInput.hlsl"
            #include "Includes/WorldToonFoliagePasses.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Meta"
            Tags { "LightMode" = "Meta" }
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex UniversalVertexMeta
            #pragma fragment WorldToonFragmentMeta
            #pragma shader_feature EDITOR_VISUALIZATION

            #include "Includes/WorldToonFoliageInput.hlsl"
            #include "Includes/WorldToonMetaPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "MotionVectors"
            Tags { "LightMode" = "MotionVectors" }
            ColorMask RG
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex FoliageMotionVertex
            #pragma fragment FoliageMotionFragment
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_vertex _WT_WIND
            #pragma shader_feature_local_vertex _WT_WIND_HEIGHT
            #pragma shader_feature_local_vertex _WT_INTERACTION
            #pragma multi_compile _ _ENVIRONMENT_REACTION_BUFFER
            #pragma target 4.5 _ENVIRONMENT_REACTION_BUFFER
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_instancing

            #define WORLDTOON_FOLIAGE_MOTION
            #include "Includes/WorldToonFoliageInput.hlsl"
            #include "Includes/WorldToonFoliagePasses.hlsl"
            ENDHLSL
        }
    }

    Fallback "Hidden/Universal Render Pipeline/FallbackError"
}
