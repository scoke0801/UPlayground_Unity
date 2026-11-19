// WorldToon Terrain4 — Unity Terrain 전용. 타일당 TerrainLayer 4개 이하만 지원한다(AddPass 의존성을 선언하지 않음).
// 레이어 Normal/Mask/Height Blend는 v1 제외: 이 기능에 의존하는 지형은 Validator가 NeedsReview로 막고 원본 Terrain Lit을 유지한다.
// 원거리는 BaseMapShader(WorldToon), 원거리 텍스처 생성은 URP Basemap Gen(라이팅을 굽지 않음)을 쓴다.
Shader "UPlayGround/WorldToon/Terrain4"
{
    Properties
    {
        [HideInInspector] [PerRendererData] _NumLayersCount ("Total Layer Count", Float) = 1.0
        [HideInInspector] _Control("Control (RGBA)", 2D) = "red" {}
        [HideInInspector] _Splat3("Layer 3 (A)", 2D) = "grey" {}
        [HideInInspector] _Splat2("Layer 2 (B)", 2D) = "grey" {}
        [HideInInspector] _Splat1("Layer 1 (G)", 2D) = "grey" {}
        [HideInInspector] _Splat0("Layer 0 (R)", 2D) = "grey" {}
        [HideInInspector] _MainTex("BaseMap (RGB)", 2D) = "grey" {}
        [HideInInspector] _BaseColor("Main Color", Color) = (1,1,1,1)
        [HideInInspector] _TerrainHolesTexture("Holes Map (RGB)", 2D) = "white" {}
        [ToggleUI] _EnableInstancedPerPixelNormal("Enable Instanced per-pixel normal", Float) = 1.0

        [Header(Toon Style)]
        _LitTint("Lit Tint", Color) = (1, 1, 1, 1)
        _ShadeTint("Shade Tint", Color) = (0.45, 0.47, 0.6, 1)
        _BandThreshold("Band Threshold", Range(-1, 1)) = 0.05
        _BandSoftness("Band Softness", Range(0.001, 0.5)) = 0.04
        _IndirectStrength("Indirect Strength", Range(0, 2)) = 1
        _AdditionalLightStrength("Additional Light Strength", Range(0, 2)) = 1
    }

    HLSLINCLUDE
    #pragma multi_compile_fragment __ _ALPHATEST_ON
    ENDHLSL

    SubShader
    {
        Tags { "Queue" = "Geometry-100" "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "False" "TerrainCompatible" = "True" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex SplatmapVert
            #pragma fragment WorldToonTerrainFragment

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _SCREEN_SPACE_IRRADIANCE
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile_fragment _ DEBUG_DISPLAY
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap
            #pragma shader_feature_local _TERRAIN_INSTANCED_PERPIXEL_NORMAL

            #include "Includes/WorldToonTerrainInput.hlsl"
            #include "Includes/WorldToonTerrainPasses.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ColorMask 0
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Includes/WorldToonTerrainInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Terrain/TerrainLitPasses.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap
            #include "Includes/WorldToonTerrainInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Terrain/TerrainLitPasses.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthNormalOnlyVertex
            #pragma fragment DepthNormalOnlyFragment
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap
            #include "Includes/WorldToonTerrainInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Terrain/TerrainLitDepthNormalsPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "SceneSelectionPass"
            Tags { "LightMode" = "SceneSelectionPass" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap
            #define SCENESELECTIONPASS
            #include "Includes/WorldToonTerrainInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Terrain/TerrainLitPasses.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Meta"
            Tags { "LightMode" = "Meta" }
            Cull Off
            HLSLPROGRAM
            #pragma vertex TerrainVertexMeta
            #pragma fragment TerrainFragmentMeta
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap
            #pragma shader_feature EDITOR_VISUALIZATION
            #define _METALLICSPECGLOSSMAP 1
            #define _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A 1
            #include "Includes/WorldToonTerrainInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Terrain/TerrainLitMetaPass.hlsl"
            ENDHLSL
        }

        UsePass "Hidden/Nature/Terrain/Utilities/PICKING"
    }

    Dependency "BaseMapShader" = "Hidden/UPlayGround/WorldToon/TerrainBaseMap"
    Dependency "BaseMapGenShader" = "Hidden/Universal Render Pipeline/Terrain/Lit (Basemap Gen)"

    Fallback "Hidden/Universal Render Pipeline/FallbackError"
}
