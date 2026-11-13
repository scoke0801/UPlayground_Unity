Shader "UPlayGround/Environment/Reactive Toon"
{
    Properties
    {
        [MainTexture] _BaseMap("기본 색", 2D) = "white" {}
        [MainColor] _BaseColor("기본 색상", Color) = (1,1,1,1)
        [Normal] _BumpMap("노멀", 2D) = "bump" {}
        _BumpScale("노멀 강도", Range(0,2)) = 1
        [Toggle(_NORMALMAP)] _UseNormalMap("노멀맵 사용", Float) = 0
        _ReactionMask("반응 R / 균열 G / 그을림 B", 2D) = "white" {}
        [Toggle] _UseReactionMask("저작 마스크 사용", Float) = 0
        [Enum(Body,0,Foliage,1,Interior,2)] _SurfaceRole("표면 역할", Float) = 0
        _FoliageBottom("잎 아래 색", Color) = (1,1,1,1)
        _FoliageTop("잎 위 색", Color) = (1,1,1,1)
        _FoliageGradientStart("잎 색 시작 높이", Float) = 0
        _FoliageGradientHeight("잎 색 높이", Float) = 1
        _FoliageTransmission("잎 역광", Range(0,1)) = 0
        _WetTint("젖음 색", Color) = (0.55,0.65,0.7,1)
        _CharTint("그을림 색", Color) = (0.12,0.10,0.08,1)
        _CrackColor("균열 색", Color) = (0.08,0.06,0.04,1)
        _FireColor("연소 경계 색", Color) = (2,0.4,0.03,1)
        _ShadeColor("툰 그림자 색", Color) = (0.42,0.48,0.57,1)
        _ShadowThreshold("명암 경계", Range(0,1)) = 0.6
        _BandSoftness("명암 경계 부드러움", Range(0.001,0.5)) = 0.025
        _WetHighlight("젖음 하이라이트", Range(0,1)) = 0.12
        _AmbientFloor("툰 최저 간접광", Range(0,1)) = 0.15
        _CharSoftness("그을림 경계", Range(0.001,0.5)) = 0.08
        _CrackSoftness("균열 경계", Range(0.001,0.5)) = 0.03
        _CrackOpacity("균열 불투명도", Range(0,1)) = 0.65
        [Toggle] _UseImpactWound("명중 부근만 손상 표현", Float) = 0
        [HideInInspector] _EnvImpact("국소 상처 위치와 반경", Vector) = (0,0,0,0)
        _NoiseScale("로컬 패턴 크기", Float) = 4
        _EdgeWidth("연소 경계 폭", Range(0.001,0.5)) = 0.08
        _GlowScale("연소 발광", Range(0,4)) = 0.8
        _OutlineColor("외곽선 색", Color) = (0.08,0.07,0.06,1)
        _OutlineWidth("외곽선 폭 (m)", Range(0,0.03)) = 0.003
        [Toggle] _AlphaClip("알파 클리핑", Float) = 0
        _Cutoff("알파 경계", Range(0,1)) = 0.5
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("컬링", Float) = 2
        _WindAmplitude("바람 변형", Range(0,1)) = 0
        _WindFrequency("바람 공간 주기", Float) = 1
        _WindSpeed("바람 시간 주기", Float) = 2
        _WindRootHeight("고정 뿌리 높이", Float) = 0
        [Toggle] _AllowFuelCoverage("잎 연료 소진 시 실루엣 소거", Float) = 0
        [HideInInspector] _FuelCoverage("Fuel coverage", Float) = 1
        [HideInInspector] _EnvironmentCellMask("Terrain cell state", 2D) = "black" {}
        [HideInInspector] _EnvironmentCellBounds("Terrain cell bounds", Vector) = (0,0,1,1)
        [HideInInspector] _UseEnvironmentCells("Terrain cell mask", Float) = 0
        [HideInInspector] _EnvState0("Wet Char Damage Burning", Vector) = (0,0,0,0)
        [HideInInspector] _EnvState1("Heat Reserved", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Cull [_Cull]
        HLSLINCLUDE
        #pragma target 3.5
        #pragma multi_compile_instancing
        #pragma multi_compile _ LOD_FADE_CROSSFADE
        #pragma shader_feature_local _NORMALMAP
        #include "EnvironmentSurface.hlsl"
        ENDHLSL
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma vertex EnvVertex
            #pragma fragment EnvForward
            #pragma multi_compile_fog
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            ENDHLSL
        }
        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front
            HLSLPROGRAM
            #pragma vertex EnvOutlineVertex
            #pragma fragment EnvOutline
            #pragma multi_compile_fog
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ColorMask 0
            HLSLPROGRAM
            #pragma vertex EnvShadowVertex
            #pragma fragment EnvDepth
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ColorMask R
            ZWrite On
            HLSLPROGRAM
            #pragma vertex EnvVertex
            #pragma fragment EnvDepth
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormalsOnly"
            Tags { "LightMode"="DepthNormalsOnly" }
            ZWrite On
            HLSLPROGRAM
            #pragma vertex EnvVertex
            #pragma fragment EnvDepthNormals
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            ZWrite On
            HLSLPROGRAM
            #pragma vertex EnvVertex
            #pragma fragment EnvDepthNormals
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            ENDHLSL
        }
        Pass
        {
            Name "MotionVectors"
            Tags { "LightMode"="MotionVectors" }
            ColorMask RG
            HLSLPROGRAM
            #pragma vertex EnvMotionVertex
            #pragma fragment EnvMotion
            ENDHLSL
        }
        Pass
        {
            Name "Meta"
            Tags { "LightMode"="Meta" }
            Cull Off
            HLSLPROGRAM
            #pragma vertex EnvMetaVertex
            #pragma fragment EnvMeta
            #pragma shader_feature EDITOR_VISUALIZATION
            ENDHLSL
        }
    }
    FallBack Off
}
