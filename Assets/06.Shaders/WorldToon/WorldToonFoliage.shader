// WorldToon Foliage — 잔디·덤불·잎 카드. Cutout + 양면 선택 + 정점 바람.
// 프리셋: FoliageNear = _WT_WIND, FoliageFar = _RECEIVE_SHADOWS_OFF(바람 없음). 각 × _ALPHATEST_ON.
// 바람 위치 변형은 Forward·ShadowCaster·DepthOnly·DepthNormals·MotionVectors가 같은 함수를 쓴다.
// 접촉 변위(FoliageInteraction)는 v1에 넣지 않는다: 접촉이 필요한 식생은 기존 셰이더를 유지한다.
Shader "UPlayGround/WorldToon/Foliage"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)

        [Header(Toon Style)]
        _LitTint("Lit Tint", Color) = (1, 1, 1, 1)
        _ShadeTint("Shade Tint", Color) = (0.45, 0.47, 0.6, 1)
        _BandThreshold("Band Threshold", Range(-1, 1)) = 0.05
        _BandSoftness("Band Softness", Range(0.001, 0.5)) = 0.04
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
            #pragma shader_feature_local_vertex _WT_WIND

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
