Shader "UPlayGround/Environment/Vegetation/5bf02d37cc25a0745988227a855e652f"
{
Properties
{

        [HideInInspector] _EnvironmentReactionSupported("환경 반응 계약", Float) = 1
        [HideInInspector] _EnvState0("젖음 그을림 손상 연소", Vector) = (0,0,0,0)
        [HideInInspector] _EnvState1("열", Vector) = (0,0,0,0)
        [HideInInspector] _FuelCoverage("잔여 연료", Float) = 1
        [Toggle] _AllowFuelCoverage("연료 소진 소거", Float) = 0
        _WetTint("젖음 색", Color) = (0.55,0.65,0.7,1)
        _CharTint("그을림 색", Color) = (0.12,0.10,0.08,1)
        _FireColor("연소 발광", Color) = (2,0.4,0.03,1)
        [HideInInspector] _EnvironmentCellMask("지형 상태", 2D) = "black" {}
        [HideInInspector] _EnvironmentCellBounds("지형 범위", Vector) = (0,0,1,1)
        [HideInInspector] _UseEnvironmentCells("지형 상태 사용", Float) = 0

_Texture_Size("Texture Size", Float) = 1
[NoScaleOffset]_Color_Map("Color Map", 2D) = "white" {}
_Color_Tint("Color Tint", Color) = (1, 1, 1, 0)
[NoScaleOffset]_Normal_Map("Normal Map", 2D) = "white" {}
_Normal_Intensity("Normal Intensity", Float) = 1
[NoScaleOffset]_ORM_Map("ORM Map", 2D) = "white" {}
_Roughness_Intensity("Roughness Intensity", Range(0, 1)) = 1
_AO_Intensity("AO Intensity", Range(0, 1)) = 0
_Metallic_Intensity("Metallic Intensity", Range(0, 1)) = 0
[NoScaleOffset]_Emissive_Map("Emissive Map", 2D) = "white" {}
[HDR]_Emissive_Color("Emissive Color", Color) = (0, 0, 0, 0)
_Height_Weight("Height Weight", Float) = 1
_Height_Gradience("Height Gradience", Float) = 1
_Wind_Intensity("Wind Intensity", Float) = 0
_Wind_Speed("Wind Speed", Float) = 2
_Wind_Direction("Wind Direction", Vector, 3) = (1, 0, 1, 0)
_Wind_Time_Random_Offset_Amount("Wind Time Random Offset Amount", Float) = 0
[NoScaleOffset]_Coverage_Color_Map("Coverage Color Map", 2D) = "white" {}
[NoScaleOffset]_Coverage_Normal_Map("Coverage Normal Map", 2D) = "white" {}
[NoScaleOffset]_Coverage_ORM_Map("Coverage ORM Map", 2D) = "white" {}
_Coverage_Size("Coverage Size", Float) = 2
_Height_from_Ground("Height from Ground", Float) = 0.5
_Height_from_Ground_Randomness("Height from Ground Randomness", Float) = 0.5
_Height_from_Ground_Softness("Height from Ground Softness", Float) = 0.1
_Normal_Pow("Normal Pow", Float) = 1
_Normal_Mul("Normal Mul", Float) = 1
[NoScaleOffset]_Mask_Map("Mask Map", 2D) = "white" {}
_Mask_Map_Size("Mask Map Size", Float) = 1
_Mask_Map_Intensity("Mask Map Intensity", Float) = 1
[Toggle(_USE_EMISSIVE)]_USE_EMISSIVE("Use Emissive?", Float) = 0
[Toggle(_USE_WPO)]_USE_WPO("Use WPO?", Float) = 1
[Toggle(_USE_ORM)]_USE_ORM("Use ORM?", Float) = 1
[Toggle(_USE_NORMAL_MAPS)]_USE_NORMAL_MAPS("Use Normal Maps?", Float) = 1
[Toggle(_USE_COVERAGE)]_USE_COVERAGE("Use Coverage?", Float) = 1
[Toggle(_USE_COVERAGE_MASK_TEXTURE)]_USE_COVERAGE_MASK_TEXTURE("Use Coverage Mask Texture?", Float) = 1
[HideInInspector]_QueueOffset("_QueueOffset", Float) = 0
[HideInInspector]_QueueControl("_QueueControl", Float) = -1
[HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
[HideInInspector][NoScaleOffset]unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
[HideInInspector][NoScaleOffset]unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
[HideInInspector]_BUILTIN_QueueOffset("Float", Float) = 0
[HideInInspector]_BUILTIN_QueueControl("Float", Float) = -1
}
SubShader
{
Tags
{
"RenderPipeline"="UniversalPipeline"
"RenderType"="Opaque"
"UniversalMaterialType" = "Lit"
"Queue"="Geometry"
"DisableBatching"="False"
"ShaderGraphShader"="true"
"ShaderGraphTargetId"="UniversalLitSubTarget"
}
Pass
{
    Name "Universal Forward"
    Tags
    {
        "LightMode" = "UniversalForward"
    }

// Render State
Cull Back
Blend One Zero
ZTest LEqual
ZWrite On

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 2.0
#pragma multi_compile_instancing
#pragma instancing_options renderinglayer
#pragma vertex vert
#pragma fragment frag

// Keywords
#pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
#pragma multi_compile_fragment _ _SCREEN_SPACE_IRRADIANCE
#pragma multi_compile _ LIGHTMAP_ON
#pragma multi_compile _ DYNAMICLIGHTMAP_ON
#pragma multi_compile _ DIRLIGHTMAP_COMBINED
#pragma multi_compile _ USE_LEGACY_LIGHTMAPS
#pragma multi_compile _ LIGHTMAP_BICUBIC_SAMPLING
#pragma multi_compile _ REFLECTION_PROBE_ROTATION
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
#pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
#pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
#pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
#pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
#pragma multi_compile _ SHADOWS_SHADOWMASK
#pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
#pragma multi_compile_fragment _ _LIGHT_LAYERS
#pragma multi_compile_fragment _ DEBUG_DISPLAY
#pragma multi_compile_fragment _ _LIGHT_COOKIES
#pragma multi_compile _ _CLUSTER_LIGHT_LOOP
#pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
#pragma shader_feature_local _ _USE_EMISSIVE
#pragma shader_feature_local _ _USE_WPO
#pragma shader_feature_local _ _USE_ORM
#pragma shader_feature_local _ _USE_NORMAL_MAPS
#pragma shader_feature_local _ _USE_COVERAGE
#pragma shader_feature_local _ _USE_COVERAGE_MASK_TEXTURE

#if defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_2
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_3
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_4
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_5
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_6
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM)
#define KEYWORD_PERMUTATION_7
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_8
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_9
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_10
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_11
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_12
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_13
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_14
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO)
#define KEYWORD_PERMUTATION_15
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_16
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_17
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_18
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_19
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_20
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_21
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_22
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM)
#define KEYWORD_PERMUTATION_23
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_24
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_25
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_26
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_27
#elif defined(_USE_EMISSIVE) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_28
#elif defined(_USE_EMISSIVE) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_29
#elif defined(_USE_EMISSIVE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_30
#elif defined(_USE_EMISSIVE)
#define KEYWORD_PERMUTATION_31
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_32
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_33
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_34
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_35
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_36
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_37
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_38
#elif defined(_USE_WPO) && defined(_USE_ORM)
#define KEYWORD_PERMUTATION_39
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_40
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_41
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_42
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_43
#elif defined(_USE_WPO) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_44
#elif defined(_USE_WPO) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_45
#elif defined(_USE_WPO) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_46
#elif defined(_USE_WPO)
#define KEYWORD_PERMUTATION_47
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_48
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_49
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_50
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_51
#elif defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_52
#elif defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_53
#elif defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_54
#elif defined(_USE_ORM)
#define KEYWORD_PERMUTATION_55
#elif defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_56
#elif defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_57
#elif defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_58
#elif defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_59
#elif defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_60
#elif defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_61
#elif defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_62
#else
#define KEYWORD_PERMUTATION_63
#endif


// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_TEXCOORD1
#define ATTRIBUTES_NEED_TEXCOORD2
#define GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_NORMAL_WS
#define VARYINGS_NEED_TANGENT_WS
#define VARYINGS_NEED_TEXCOORD0
#define VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
#define VARYINGS_NEED_SHADOW_COORD
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_FORWARD


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
 float3 positionOS : POSITION;
 float3 normalOS : NORMAL;
 float4 tangentOS : TANGENT;
 float4 uv0 : TEXCOORD0;
 float4 uv1 : TEXCOORD1;
 float4 uv2 : TEXCOORD2;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
 float4 positionCS : SV_POSITION;
 float3 positionWS;
 float3 normalWS;
 float4 tangentWS;
 float4 texCoord0;
#if defined(LIGHTMAP_ON)
 float2 staticLightmapUV;
#endif
#if defined(DYNAMICLIGHTMAP_ON)
 float2 dynamicLightmapUV;
#endif
#if !defined(LIGHTMAP_ON)
 float3 sh;
#endif
#if defined(USE_APV_PROBE_OCCLUSION)
 float4 probeOcclusion;
#endif
 float4 fogFactorAndVertexLight;
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
 float4 shadowCoord;
#endif
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};
struct SurfaceDescriptionInputs
{
 float3 WorldSpaceNormal;
 float3 TangentSpaceNormal;
 float3 ObjectSpacePosition;
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 AbsoluteWorldSpacePosition;
 float3 TimeParameters;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
#if defined(LIGHTMAP_ON)
 float2 staticLightmapUV : INTERP0;
#endif
#if defined(DYNAMICLIGHTMAP_ON)
 float2 dynamicLightmapUV : INTERP1;
#endif
#if !defined(LIGHTMAP_ON)
 float3 sh : INTERP2;
#endif
#if defined(USE_APV_PROBE_OCCLUSION)
 float4 probeOcclusion : INTERP3;
#endif
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
 float4 shadowCoord : INTERP4;
#endif
 float4 tangentWS : INTERP5;
 float4 texCoord0 : INTERP6;
 float4 fogFactorAndVertexLight : INTERP7;
 float3 positionWS : INTERP8;
 float3 normalWS : INTERP9;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};

PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.positionCS = input.positionCS;
#if defined(LIGHTMAP_ON)
output.staticLightmapUV = input.staticLightmapUV;
#endif
#if defined(DYNAMICLIGHTMAP_ON)
output.dynamicLightmapUV = input.dynamicLightmapUV;
#endif
#if !defined(LIGHTMAP_ON)
output.sh = input.sh;
#endif
#if defined(USE_APV_PROBE_OCCLUSION)
output.probeOcclusion = input.probeOcclusion;
#endif
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
output.shadowCoord = input.shadowCoord;
#endif
output.tangentWS.xyzw = input.tangentWS;
output.texCoord0.xyzw = input.texCoord0;
output.fogFactorAndVertexLight.xyzw = input.fogFactorAndVertexLight;
output.positionWS.xyz = input.positionWS;
output.normalWS.xyz = input.normalWS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.positionCS = input.positionCS;
#if defined(LIGHTMAP_ON)
output.staticLightmapUV = input.staticLightmapUV;
#endif
#if defined(DYNAMICLIGHTMAP_ON)
output.dynamicLightmapUV = input.dynamicLightmapUV;
#endif
#if !defined(LIGHTMAP_ON)
output.sh = input.sh;
#endif
#if defined(USE_APV_PROBE_OCCLUSION)
output.probeOcclusion = input.probeOcclusion;
#endif
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
output.shadowCoord = input.shadowCoord;
#endif
output.tangentWS = input.tangentWS.xyzw;
output.texCoord0 = input.texCoord0.xyzw;
output.fogFactorAndVertexLight = input.fogFactorAndVertexLight.xyzw;
output.positionWS = input.positionWS.xyz;
output.normalWS = input.normalWS.xyz;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}


// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)

float _EnvironmentReactionSupported;
float4 _EnvState0, _EnvState1;
float _FuelCoverage, _AllowFuelCoverage;
float4 _WetTint, _CharTint, _FireColor;
float4 _EnvironmentCellBounds;
float _UseEnvironmentCells;

float _Height_from_Ground_Randomness;
float _Height_from_Ground_Softness;
float _Normal_Pow;
float _Normal_Mul;
float _Mask_Map_Size;
float4 _Coverage_Normal_Map_TexelSize;
float _Mask_Map_Intensity;
float4 _Coverage_ORM_Map_TexelSize;
float _Height_Weight;
float _Height_Gradience;
float _Wind_Intensity;
float3 _Wind_Direction;
float _Wind_Speed;
float _Wind_Time_Random_Offset_Amount;
float _Texture_Size;
float4 _Color_Map_TexelSize;
float4 _Color_Tint;
float4 _Normal_Map_TexelSize;
float _Normal_Intensity;
float4 _ORM_Map_TexelSize;
float _Roughness_Intensity;
float _AO_Intensity;
float _Metallic_Intensity;
float4 _Emissive_Map_TexelSize;
float4 _Emissive_Color;
float4 _Coverage_Color_Map_TexelSize;
float _Coverage_Size;
float _Height_from_Ground;
float4 _Mask_Map_TexelSize;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Coverage_Normal_Map);
SAMPLER(sampler_Coverage_Normal_Map);
TEXTURE2D(_Coverage_ORM_Map);
SAMPLER(sampler_Coverage_ORM_Map);
TEXTURE2D(_Color_Map);
SAMPLER(sampler_Color_Map);
TEXTURE2D(_Normal_Map);
SAMPLER(sampler_Normal_Map);
TEXTURE2D(_ORM_Map);
SAMPLER(sampler_ORM_Map);
TEXTURE2D(_Emissive_Map);
SAMPLER(sampler_Emissive_Map);
TEXTURE2D(_Coverage_Color_Map);
SAMPLER(sampler_Coverage_Color_Map);
TEXTURE2D(_Mask_Map);
SAMPLER(sampler_Mask_Map);

// Graph Includes
// GraphIncludes: <None>

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Functions

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Sine_float(float In, out float Out)
{
    Out = sin(In);
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Length_float3(float3 In, out float Out)
{
    Out = length(In);
}

void Unity_Divide_float(float A, float B, out float Out)
{
    Out = A / B;
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
{
    Out = lerp(A, B, T);
}

struct Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float
{
float3 ObjectSpacePosition;
float3 TimeParameters;
};

void SG_WindSway_51c4027ae50a8cd49be53c4990b9f179_float(float _Sway_Wind_Speed, float3 _Sway_Wind_Direction, float _Sway_Wind_Intensity, float _Sway_Wind_Time_Random_Offset_Amount, Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float IN, out float3 OutVector3_1)
{
float _Property_5c4cd19861f64014ae8e16af9365ef49_Out_0_Float = _Sway_Wind_Time_Random_Offset_Amount;
float _Add_4b31033339dc4f8e8e71674d98e662a3_Out_2_Float;
Unity_Add_float(IN.TimeParameters.x, _Property_5c4cd19861f64014ae8e16af9365ef49_Out_0_Float, _Add_4b31033339dc4f8e8e71674d98e662a3_Out_2_Float);
float _Property_9a93fa589ac7409884a94247349a940f_Out_0_Float = _Sway_Wind_Speed;
float _Multiply_7418655003ae4fedb2bf68678629472c_Out_2_Float;
Unity_Multiply_float_float(_Add_4b31033339dc4f8e8e71674d98e662a3_Out_2_Float, _Property_9a93fa589ac7409884a94247349a940f_Out_0_Float, _Multiply_7418655003ae4fedb2bf68678629472c_Out_2_Float);
float _Sine_ebd14b0fc42b4b4cb4387f3a1b879c1f_Out_1_Float;
Unity_Sine_float(_Multiply_7418655003ae4fedb2bf68678629472c_Out_2_Float, _Sine_ebd14b0fc42b4b4cb4387f3a1b879c1f_Out_1_Float);
float _Add_660e92170c564f94a79277cd2fd0cf20_Out_2_Float;
Unity_Add_float(_Sine_ebd14b0fc42b4b4cb4387f3a1b879c1f_Out_1_Float, float(1), _Add_660e92170c564f94a79277cd2fd0cf20_Out_2_Float);
float _Multiply_b8fd654b771447aebafc1a6aad6957cf_Out_2_Float;
Unity_Multiply_float_float(_Add_660e92170c564f94a79277cd2fd0cf20_Out_2_Float, 0.5, _Multiply_b8fd654b771447aebafc1a6aad6957cf_Out_2_Float);
float3 _Property_fa2ee033b44946c59c3fa9aea4a167c8_Out_0_Vector3 = _Sway_Wind_Direction;
float3 _Multiply_a5500b7f7b8e43988a055eea7eb917f3_Out_2_Vector3;
Unity_Multiply_float3_float3((_Multiply_b8fd654b771447aebafc1a6aad6957cf_Out_2_Float.xxx), _Property_fa2ee033b44946c59c3fa9aea4a167c8_Out_0_Vector3, _Multiply_a5500b7f7b8e43988a055eea7eb917f3_Out_2_Vector3);
float _Property_66cba0a3223546238e18b58e017e756f_Out_0_Float = _Sway_Wind_Intensity;
float3 _Multiply_475f81423e39483cba045bef08b9f967_Out_2_Vector3;
Unity_Multiply_float3_float3(_Multiply_a5500b7f7b8e43988a055eea7eb917f3_Out_2_Vector3, (_Property_66cba0a3223546238e18b58e017e756f_Out_0_Float.xxx), _Multiply_475f81423e39483cba045bef08b9f967_Out_2_Vector3);
float3 _Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3;
Unity_Add_float3(_Multiply_475f81423e39483cba045bef08b9f967_Out_2_Vector3, IN.ObjectSpacePosition, _Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3);
float _Length_53965ffb38de43359ab019a247194557_Out_1_Float;
Unity_Length_float3(IN.ObjectSpacePosition, _Length_53965ffb38de43359ab019a247194557_Out_1_Float);
float _Length_77b6b3d742104f2abf3c6968dea9a342_Out_1_Float;
Unity_Length_float3(_Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3, _Length_77b6b3d742104f2abf3c6968dea9a342_Out_1_Float);
float _Divide_f5dd43de79e343638d2743ee93ce7326_Out_2_Float;
Unity_Divide_float(_Length_53965ffb38de43359ab019a247194557_Out_1_Float, _Length_77b6b3d742104f2abf3c6968dea9a342_Out_1_Float, _Divide_f5dd43de79e343638d2743ee93ce7326_Out_2_Float);
float3 _Lerp_7574daf550e1428cb9d35656c7bb9603_Out_3_Vector3;
Unity_Lerp_float3(float3(0, 0, 0), _Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3, (_Divide_f5dd43de79e343638d2743ee93ce7326_Out_2_Float.xxx), _Lerp_7574daf550e1428cb9d35656c7bb9603_Out_3_Vector3);
OutVector3_1 = _Lerp_7574daf550e1428cb9d35656c7bb9603_Out_3_Vector3;
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

void Unity_Power_float(float A, float B, out float Out)
{
    Out = pow(A, B);
}

void Unity_Divide_float4(float4 A, float4 B, out float4 Out)
{
    Out = A / B;
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

void Unity_Normalize_float3(float3 In, out float3 Out)
{
    Out = normalize(In);
}

void Unity_RandomRange_float(float2 Seed, float Min, float Max, out float Out)
{
     float randomno =  frac(sin(dot(Seed, float2(12.9898, 78.233)))*43758.5453);
     Out = lerp(Min, Max, randomno);
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
}

void Unity_DotProduct_float3(float3 A, float3 B, out float Out)
{
    Out = dot(A, B);
}

void Unity_Step_float(float Edge, float In, out float Out)
{
    Out = step(Edge, In);
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_NormalStrength_float(float3 In, float Strength, out float3 Out)
{
    Out = float3(In.rg * Strength, lerp(1, In.b, saturate(Strength)));
}

void Unity_Lerp_float(float A, float B, float T, out float Out)
{
    Out = lerp(A, B, T);
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 Position;
float3 Normal;
float3 Tangent;
};

VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
float _Property_b2e048f6276d4371b6ad931f5d1baa87_Out_0_Float = _Wind_Speed;
float3 _Property_7133a0551bc7429cb8bbfcbad044bf97_Out_0_Vector3 = _Wind_Direction;
float _Property_5aa2352867cd4da79dc9f9f9979925c7_Out_0_Float = _Wind_Intensity;
float _Property_e0c6ebff99e349d6966115347961a70c_Out_0_Float = _Wind_Time_Random_Offset_Amount;
Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf;
_WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf.ObjectSpacePosition = IN.ObjectSpacePosition;
_WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf.TimeParameters = IN.TimeParameters;
float3 _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf_OutVector3_1_Vector3;
SG_WindSway_51c4027ae50a8cd49be53c4990b9f179_float(_Property_b2e048f6276d4371b6ad931f5d1baa87_Out_0_Float, _Property_7133a0551bc7429cb8bbfcbad044bf97_Out_0_Vector3, _Property_5aa2352867cd4da79dc9f9f9979925c7_Out_0_Float, _Property_e0c6ebff99e349d6966115347961a70c_Out_0_Float, _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf, _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf_OutVector3_1_Vector3);
float _Split_38420bd2034746a1878048f0ead2a7b2_R_1_Float = IN.AbsoluteWorldSpacePosition[0];
float _Split_38420bd2034746a1878048f0ead2a7b2_G_2_Float = IN.AbsoluteWorldSpacePosition[1];
float _Split_38420bd2034746a1878048f0ead2a7b2_B_3_Float = IN.AbsoluteWorldSpacePosition[2];
float _Split_38420bd2034746a1878048f0ead2a7b2_A_4_Float = 0;
float3 _Vector3_0f8475b3a0df451ea79f6ea2e4e3f673_Out_0_Vector3 = float3(float(0), float(0), float(0));
float3 _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3;
{
// Converting Position from Object to AbsoluteWorld via world space
float3 world;
world = TransformObjectToWorld(_Vector3_0f8475b3a0df451ea79f6ea2e4e3f673_Out_0_Vector3.xyz);
_Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3 = GetAbsolutePositionWS(world);
}
float _Split_fb1b2d84926449e498da6c90f1436311_R_1_Float = _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3[0];
float _Split_fb1b2d84926449e498da6c90f1436311_G_2_Float = _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3[1];
float _Split_fb1b2d84926449e498da6c90f1436311_B_3_Float = _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3[2];
float _Split_fb1b2d84926449e498da6c90f1436311_A_4_Float = 0;
float _Subtract_18ddaa9851a547f1bc3eeafa55f3227b_Out_2_Float;
Unity_Subtract_float(_Split_38420bd2034746a1878048f0ead2a7b2_G_2_Float, _Split_fb1b2d84926449e498da6c90f1436311_G_2_Float, _Subtract_18ddaa9851a547f1bc3eeafa55f3227b_Out_2_Float);
float _Property_1db6873c977b47f186e42eef2cd75a57_Out_0_Float = _Height_Weight;
float _Split_bc5987163dba418d8de90e985de1247b_R_1_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[0];
float _Split_bc5987163dba418d8de90e985de1247b_G_2_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[1];
float _Split_bc5987163dba418d8de90e985de1247b_B_3_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[2];
float _Split_bc5987163dba418d8de90e985de1247b_A_4_Float = 0;
float _Multiply_89de5a5fb3cd451abb1227deba019c38_Out_2_Float;
Unity_Multiply_float_float(_Property_1db6873c977b47f186e42eef2cd75a57_Out_0_Float, _Split_bc5987163dba418d8de90e985de1247b_G_2_Float, _Multiply_89de5a5fb3cd451abb1227deba019c38_Out_2_Float);
float _Divide_2c406de6c63e463f9013e4faf1db1ae8_Out_2_Float;
Unity_Divide_float(_Subtract_18ddaa9851a547f1bc3eeafa55f3227b_Out_2_Float, _Multiply_89de5a5fb3cd451abb1227deba019c38_Out_2_Float, _Divide_2c406de6c63e463f9013e4faf1db1ae8_Out_2_Float);
float _Saturate_e9ea645ca89f482a9d0315df54a77ee5_Out_1_Float;
Unity_Saturate_float(_Divide_2c406de6c63e463f9013e4faf1db1ae8_Out_2_Float, _Saturate_e9ea645ca89f482a9d0315df54a77ee5_Out_1_Float);
float _Property_e96fd1920dff4533951da684129df200_Out_0_Float = _Height_Gradience;
float _Power_b88c5abc85424f17b03c60c98e9bc16c_Out_2_Float;
Unity_Power_float(_Saturate_e9ea645ca89f482a9d0315df54a77ee5_Out_1_Float, _Property_e96fd1920dff4533951da684129df200_Out_0_Float, _Power_b88c5abc85424f17b03c60c98e9bc16c_Out_2_Float);
float _Saturate_6c35d0fa108b483783bde88e06ff5ce0_Out_1_Float;
Unity_Saturate_float(_Power_b88c5abc85424f17b03c60c98e9bc16c_Out_2_Float, _Saturate_6c35d0fa108b483783bde88e06ff5ce0_Out_1_Float);
float3 _Lerp_8461a67d418941ee81ed9eb0f80e8a38_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf_OutVector3_1_Vector3, (_Saturate_6c35d0fa108b483783bde88e06ff5ce0_Out_1_Float.xxx), _Lerp_8461a67d418941ee81ed9eb0f80e8a38_Out_3_Vector3);
#if defined(_USE_WPO)
float3 _UseWPO_81cb92bff33547fbbfaa5ce2f60ba97c_Out_0_Vector3 = _Lerp_8461a67d418941ee81ed9eb0f80e8a38_Out_3_Vector3;
#else
float3 _UseWPO_81cb92bff33547fbbfaa5ce2f60ba97c_Out_0_Vector3 = IN.ObjectSpacePosition;
#endif
description.Position = _UseWPO_81cb92bff33547fbbfaa5ce2f60ba97c_Out_0_Vector3;
description.Normal = IN.ObjectSpaceNormal;
description.Tangent = IN.ObjectSpaceTangent;
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
float3 BaseColor;
float3 NormalTS;
float3 Emission;
float Metallic;
float Smoothness;
float Occlusion;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
UnityTexture2D _Property_0c79649b4042462682b4a7423715f1a1_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Color_Map, sampler_Color_Map, _Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_a9551365617a4a66b75c6a49ab8f9c5f_Out_0_Vector4 = IN.uv0;
float _Property_ac4a3a6f20bd42789825abf989f1a228_Out_0_Float = _Texture_Size;
float4 _Divide_1ff147fe44c540028660935d59272eaa_Out_2_Vector4;
Unity_Divide_float4(_UV_a9551365617a4a66b75c6a49ab8f9c5f_Out_0_Vector4, (_Property_ac4a3a6f20bd42789825abf989f1a228_Out_0_Float.xxxx), _Divide_1ff147fe44c540028660935d59272eaa_Out_2_Vector4);
float4 _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_0c79649b4042462682b4a7423715f1a1_Out_0_Texture2D.tex, _Property_0c79649b4042462682b4a7423715f1a1_Out_0_Texture2D.samplerstate, _Property_0c79649b4042462682b4a7423715f1a1_Out_0_Texture2D.GetTransformedUV((_Divide_1ff147fe44c540028660935d59272eaa_Out_2_Vector4.xy)) );
if (_Property_0c79649b4042462682b4a7423715f1a1_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4, _Property_0c79649b4042462682b4a7423715f1a1_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_R_4_Float = _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4.r;
float _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_G_5_Float = _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4.g;
float _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_B_6_Float = _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4.b;
float _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_A_7_Float = _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4.a;
float4 _Property_11be52d8078b41908ba72022455e8b98_Out_0_Vector4 = _Color_Tint;
float4 _Multiply_e3ef32e4a4e8442188dfd3c93394960c_Out_2_Vector4;
Unity_Multiply_float4_float4(_SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4, _Property_11be52d8078b41908ba72022455e8b98_Out_0_Vector4, _Multiply_e3ef32e4a4e8442188dfd3c93394960c_Out_2_Vector4);
UnityTexture2D _Property_415df38a141841e6aa11a7b1e336c379_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Coverage_Color_Map, sampler_Coverage_Color_Map, _Coverage_Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_0805a5601f5b49beb942e50ab3714e03_Out_0_Vector4 = IN.uv0;
float _Property_eb7418710f3f4dc6981c01445b1a809e_Out_0_Float = _Coverage_Size;
float4 _Divide_71dd940938164d5a9e7020e44b1cddf3_Out_2_Vector4;
Unity_Divide_float4(_UV_0805a5601f5b49beb942e50ab3714e03_Out_0_Vector4, (_Property_eb7418710f3f4dc6981c01445b1a809e_Out_0_Float.xxxx), _Divide_71dd940938164d5a9e7020e44b1cddf3_Out_2_Vector4);
float4 _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_415df38a141841e6aa11a7b1e336c379_Out_0_Texture2D.tex, _Property_415df38a141841e6aa11a7b1e336c379_Out_0_Texture2D.samplerstate, _Property_415df38a141841e6aa11a7b1e336c379_Out_0_Texture2D.GetTransformedUV((_Divide_71dd940938164d5a9e7020e44b1cddf3_Out_2_Vector4.xy)) );
if (_Property_415df38a141841e6aa11a7b1e336c379_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4, _Property_415df38a141841e6aa11a7b1e336c379_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_R_4_Float = _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4.r;
float _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_G_5_Float = _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4.g;
float _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_B_6_Float = _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4.b;
float _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_A_7_Float = _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4.a;
UnityTexture2D _Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Mask_Map, sampler_Mask_Map, _Mask_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_c2c5030522744aceb603ff104c9ccde0_Out_0_Vector4 = IN.uv0;
float _Property_a975da4683af46ab85b4a95b01cb9f32_Out_0_Float = _Mask_Map_Size;
float4 _Divide_3dd829d125dc4ec1b32772766d56a17b_Out_2_Vector4;
Unity_Divide_float4(_UV_c2c5030522744aceb603ff104c9ccde0_Out_0_Vector4, (_Property_a975da4683af46ab85b4a95b01cb9f32_Out_0_Float.xxxx), _Divide_3dd829d125dc4ec1b32772766d56a17b_Out_2_Vector4);
float4 _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D.tex, _Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D.samplerstate, _Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D.GetTransformedUV((_Divide_3dd829d125dc4ec1b32772766d56a17b_Out_2_Vector4.xy)) );
if (_Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4, _Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_R_4_Float = _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4.r;
float _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_G_5_Float = _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4.g;
float _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_B_6_Float = _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4.b;
float _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_A_7_Float = _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4.a;
float _Multiply_81f5f8bc82304adbae345a5fe4b0024b_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_R_4_Float, 0.1, _Multiply_81f5f8bc82304adbae345a5fe4b0024b_Out_2_Float);
float _Add_33507c032b9e48bfb61763d507bb031c_Out_2_Float;
Unity_Add_float(_Multiply_81f5f8bc82304adbae345a5fe4b0024b_Out_2_Float, float(0.9), _Add_33507c032b9e48bfb61763d507bb031c_Out_2_Float);
float _Property_883f8e1288fd43f1a3674ca1f5364253_Out_0_Float = _Mask_Map_Intensity;
float _Power_b45fec9ffb074e40b5cb5452c0fb982d_Out_2_Float;
Unity_Power_float(_Add_33507c032b9e48bfb61763d507bb031c_Out_2_Float, _Property_883f8e1288fd43f1a3674ca1f5364253_Out_0_Float, _Power_b45fec9ffb074e40b5cb5452c0fb982d_Out_2_Float);
float _Split_b6a9cc69e69c417ba235626d7beed2eb_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_b6a9cc69e69c417ba235626d7beed2eb_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_b6a9cc69e69c417ba235626d7beed2eb_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_b6a9cc69e69c417ba235626d7beed2eb_A_4_Float = 0;
float _Property_73ac42b2cd3d4b43976ac13e73b6de40_Out_0_Float = _Height_from_Ground;
float3 _Transform_425aee5b4b5e409eb94b431642b515fb_Out_1_Vector3;
_Transform_425aee5b4b5e409eb94b431642b515fb_Out_1_Vector3 = TransformObjectToWorld(float3 (0, 0, 0).xyz);
float3 _Normalize_960b1f24c996479a8c1a9a81cabcb835_Out_1_Vector3;
Unity_Normalize_float3(_Transform_425aee5b4b5e409eb94b431642b515fb_Out_1_Vector3, _Normalize_960b1f24c996479a8c1a9a81cabcb835_Out_1_Vector3);
float _Property_11f1157c9f364521bf765c261e8bb93e_Out_0_Float = _Height_from_Ground_Randomness;
float _Multiply_b7a3ed973f104b9a84af846ce299d95f_Out_2_Float;
Unity_Multiply_float_float(_Property_11f1157c9f364521bf765c261e8bb93e_Out_0_Float, -1, _Multiply_b7a3ed973f104b9a84af846ce299d95f_Out_2_Float);
float _RandomRange_82fa55b7f4da4f6ebc597a38d2ca2e6d_Out_3_Float;
Unity_RandomRange_float((_Normalize_960b1f24c996479a8c1a9a81cabcb835_Out_1_Vector3.xy), _Multiply_b7a3ed973f104b9a84af846ce299d95f_Out_2_Float, _Property_11f1157c9f364521bf765c261e8bb93e_Out_0_Float, _RandomRange_82fa55b7f4da4f6ebc597a38d2ca2e6d_Out_3_Float);
float _Add_109ea06d643c4826b0138a85becc2fa9_Out_2_Float;
Unity_Add_float(_Property_73ac42b2cd3d4b43976ac13e73b6de40_Out_0_Float, _RandomRange_82fa55b7f4da4f6ebc597a38d2ca2e6d_Out_3_Float, _Add_109ea06d643c4826b0138a85becc2fa9_Out_2_Float);
float _Subtract_fe60462bdbdd4a22a207774a6a8cb1b8_Out_2_Float;
Unity_Subtract_float(_Split_b6a9cc69e69c417ba235626d7beed2eb_G_2_Float, _Add_109ea06d643c4826b0138a85becc2fa9_Out_2_Float, _Subtract_fe60462bdbdd4a22a207774a6a8cb1b8_Out_2_Float);
float _Property_1487773737e44e78a7c514c7f470e603_Out_0_Float = _Height_from_Ground_Softness;
float _Divide_6eb043a8aee54d87a0c73169597bdcba_Out_2_Float;
Unity_Divide_float(_Subtract_fe60462bdbdd4a22a207774a6a8cb1b8_Out_2_Float, _Property_1487773737e44e78a7c514c7f470e603_Out_0_Float, _Divide_6eb043a8aee54d87a0c73169597bdcba_Out_2_Float);
float _Saturate_d8ec125670414d1888aeec654fcb04e2_Out_1_Float;
Unity_Saturate_float(_Divide_6eb043a8aee54d87a0c73169597bdcba_Out_2_Float, _Saturate_d8ec125670414d1888aeec654fcb04e2_Out_1_Float);
float _OneMinus_de85154fa1b742e3b6c9e0f92b85d96c_Out_1_Float;
Unity_OneMinus_float(_Saturate_d8ec125670414d1888aeec654fcb04e2_Out_1_Float, _OneMinus_de85154fa1b742e3b6c9e0f92b85d96c_Out_1_Float);
float _DotProduct_a5624fdd7fd94a4c82ad8c039922e638_Out_2_Float;
Unity_DotProduct_float3(IN.WorldSpaceNormal, float3(0, 1, 0), _DotProduct_a5624fdd7fd94a4c82ad8c039922e638_Out_2_Float);
float _Property_1d95c04e88f443bc91fe7ed217b674b8_Out_0_Float = _Normal_Pow;
float _Power_5b713b10f3e049f8a97be81969b52b7d_Out_2_Float;
Unity_Power_float(_DotProduct_a5624fdd7fd94a4c82ad8c039922e638_Out_2_Float, _Property_1d95c04e88f443bc91fe7ed217b674b8_Out_0_Float, _Power_5b713b10f3e049f8a97be81969b52b7d_Out_2_Float);
float _Property_6314206eb47742698a7d2833f119f39e_Out_0_Float = _Normal_Mul;
float _Multiply_6e6bcfc2a4df414e86d241952840e972_Out_2_Float;
Unity_Multiply_float_float(_Power_5b713b10f3e049f8a97be81969b52b7d_Out_2_Float, _Property_6314206eb47742698a7d2833f119f39e_Out_0_Float, _Multiply_6e6bcfc2a4df414e86d241952840e972_Out_2_Float);
float _Saturate_77d41005224e417eb678b78b9a10771a_Out_1_Float;
Unity_Saturate_float(_Multiply_6e6bcfc2a4df414e86d241952840e972_Out_2_Float, _Saturate_77d41005224e417eb678b78b9a10771a_Out_1_Float);
float _Add_8395f84ea8e546988dd02e5b922d32ee_Out_2_Float;
Unity_Add_float(_OneMinus_de85154fa1b742e3b6c9e0f92b85d96c_Out_1_Float, _Saturate_77d41005224e417eb678b78b9a10771a_Out_1_Float, _Add_8395f84ea8e546988dd02e5b922d32ee_Out_2_Float);
float _Multiply_b61ead58dd024fd7aacad7c0b68f3be7_Out_2_Float;
Unity_Multiply_float_float(5, _Add_8395f84ea8e546988dd02e5b922d32ee_Out_2_Float, _Multiply_b61ead58dd024fd7aacad7c0b68f3be7_Out_2_Float);
float _Multiply_b0c3a49303544ccaa1d15379e6b1e9d2_Out_2_Float;
Unity_Multiply_float_float(_Power_b45fec9ffb074e40b5cb5452c0fb982d_Out_2_Float, _Multiply_b61ead58dd024fd7aacad7c0b68f3be7_Out_2_Float, _Multiply_b0c3a49303544ccaa1d15379e6b1e9d2_Out_2_Float);
float _Step_9e404f0c811346c88c531f42e623243f_Out_2_Float;
Unity_Step_float(_Subtract_fe60462bdbdd4a22a207774a6a8cb1b8_Out_2_Float, float(-0.1), _Step_9e404f0c811346c88c531f42e623243f_Out_2_Float);
float _Add_0328340190574386b2d37a0d035b66fb_Out_2_Float;
Unity_Add_float(_Multiply_b0c3a49303544ccaa1d15379e6b1e9d2_Out_2_Float, _Step_9e404f0c811346c88c531f42e623243f_Out_2_Float, _Add_0328340190574386b2d37a0d035b66fb_Out_2_Float);
float _Power_3bfc30934311465db3ba28e64b49097d_Out_2_Float;
Unity_Power_float(_Add_0328340190574386b2d37a0d035b66fb_Out_2_Float, float(3), _Power_3bfc30934311465db3ba28e64b49097d_Out_2_Float);
float _Saturate_3ba4a70af5214b4086803937a3f8b09f_Out_1_Float;
Unity_Saturate_float(_Power_3bfc30934311465db3ba28e64b49097d_Out_2_Float, _Saturate_3ba4a70af5214b4086803937a3f8b09f_Out_1_Float);
float _Saturate_6e423c9f5f1249088750a2b84a42b764_Out_1_Float;
Unity_Saturate_float(_Add_8395f84ea8e546988dd02e5b922d32ee_Out_2_Float, _Saturate_6e423c9f5f1249088750a2b84a42b764_Out_1_Float);
#if defined(_USE_COVERAGE_MASK_TEXTURE)
float _UseCoverageMaskTexture_f75c00eee68746bd80e2fa6c2bc28d4c_Out_0_Float = _Saturate_3ba4a70af5214b4086803937a3f8b09f_Out_1_Float;
#else
float _UseCoverageMaskTexture_f75c00eee68746bd80e2fa6c2bc28d4c_Out_0_Float = _Saturate_6e423c9f5f1249088750a2b84a42b764_Out_1_Float;
#endif
float4 _Lerp_c6c7a7472707418491f2fa10989f05b9_Out_3_Vector4;
Unity_Lerp_float4(_Multiply_e3ef32e4a4e8442188dfd3c93394960c_Out_2_Vector4, _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4, (_UseCoverageMaskTexture_f75c00eee68746bd80e2fa6c2bc28d4c_Out_0_Float.xxxx), _Lerp_c6c7a7472707418491f2fa10989f05b9_Out_3_Vector4);
#if defined(_USE_COVERAGE)
float4 _UseCoverage_c2f2c567080e4d5f9b652fbcbf667c54_Out_0_Vector4 = _Lerp_c6c7a7472707418491f2fa10989f05b9_Out_3_Vector4;
#else
float4 _UseCoverage_c2f2c567080e4d5f9b652fbcbf667c54_Out_0_Vector4 = _Multiply_e3ef32e4a4e8442188dfd3c93394960c_Out_2_Vector4;
#endif
UnityTexture2D _Property_a38bf1e56c364aa0add1f01a99eb6c3e_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Normal_Map, sampler_Normal_Map, _Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_a38bf1e56c364aa0add1f01a99eb6c3e_Out_0_Texture2D.tex, _Property_a38bf1e56c364aa0add1f01a99eb6c3e_Out_0_Texture2D.samplerstate, _Property_a38bf1e56c364aa0add1f01a99eb6c3e_Out_0_Texture2D.GetTransformedUV((_Divide_1ff147fe44c540028660935d59272eaa_Out_2_Vector4.xy)) );
if (_Property_a38bf1e56c364aa0add1f01a99eb6c3e_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4, _Property_a38bf1e56c364aa0add1f01a99eb6c3e_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4);
float _SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_R_4_Float = _SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4.r;
float _SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_G_5_Float = _SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4.g;
float _SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_B_6_Float = _SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4.b;
float _SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_A_7_Float = _SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4.a;
float _Property_ff6f57141462418790b8f0ef1d7d69cc_Out_0_Float = _Normal_Intensity;
float3 _NormalStrength_6e1c84dc8cbd47f988a654d24e83669f_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4.xyz), _Property_ff6f57141462418790b8f0ef1d7d69cc_Out_0_Float, _NormalStrength_6e1c84dc8cbd47f988a654d24e83669f_Out_2_Vector3);
#if defined(_USE_NORMAL_MAPS)
float3 _UseNormalMaps_f01546d53fa54c3191638b3fe3736688_Out_0_Vector3 = _NormalStrength_6e1c84dc8cbd47f988a654d24e83669f_Out_2_Vector3;
#else
float3 _UseNormalMaps_f01546d53fa54c3191638b3fe3736688_Out_0_Vector3 = float3(0, 0, 1);
#endif
UnityTexture2D _Property_15070824f15143aea7ede9b130164716_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Coverage_Normal_Map, sampler_Coverage_Normal_Map, _Coverage_Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_15070824f15143aea7ede9b130164716_Out_0_Texture2D.tex, _Property_15070824f15143aea7ede9b130164716_Out_0_Texture2D.samplerstate, _Property_15070824f15143aea7ede9b130164716_Out_0_Texture2D.GetTransformedUV((_Divide_71dd940938164d5a9e7020e44b1cddf3_Out_2_Vector4.xy)) );
if (_Property_15070824f15143aea7ede9b130164716_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4, _Property_15070824f15143aea7ede9b130164716_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4);
float _SampleTexture2D_26c139222fa1483d83940748db6d922f_R_4_Float = _SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4.r;
float _SampleTexture2D_26c139222fa1483d83940748db6d922f_G_5_Float = _SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4.g;
float _SampleTexture2D_26c139222fa1483d83940748db6d922f_B_6_Float = _SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4.b;
float _SampleTexture2D_26c139222fa1483d83940748db6d922f_A_7_Float = _SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4.a;
float3 _Lerp_a5163d5f017c46b9b347647a7d361156_Out_3_Vector3;
Unity_Lerp_float3(_UseNormalMaps_f01546d53fa54c3191638b3fe3736688_Out_0_Vector3, (_SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4.xyz), (_UseCoverageMaskTexture_f75c00eee68746bd80e2fa6c2bc28d4c_Out_0_Float.xxx), _Lerp_a5163d5f017c46b9b347647a7d361156_Out_3_Vector3);
#if defined(_USE_COVERAGE)
float3 _UseCoverage_d6109394a0154e89890cf6ca228f700f_Out_0_Vector3 = _Lerp_a5163d5f017c46b9b347647a7d361156_Out_3_Vector3;
#else
float3 _UseCoverage_d6109394a0154e89890cf6ca228f700f_Out_0_Vector3 = _UseNormalMaps_f01546d53fa54c3191638b3fe3736688_Out_0_Vector3;
#endif
#if defined(_USE_NORMAL_MAPS)
float3 _UseNormalMaps_cf0fe6c3e8224d69a92ff77f7f270e12_Out_0_Vector3 = _UseCoverage_d6109394a0154e89890cf6ca228f700f_Out_0_Vector3;
#else
float3 _UseNormalMaps_cf0fe6c3e8224d69a92ff77f7f270e12_Out_0_Vector3 = float3(0, 0, 1);
#endif
UnityTexture2D _Property_c7dc38eaeea545aaada0e67cc32d9d25_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Emissive_Map, sampler_Emissive_Map, _Emissive_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_c7dc38eaeea545aaada0e67cc32d9d25_Out_0_Texture2D.tex, _Property_c7dc38eaeea545aaada0e67cc32d9d25_Out_0_Texture2D.samplerstate, _Property_c7dc38eaeea545aaada0e67cc32d9d25_Out_0_Texture2D.GetTransformedUV((_Divide_1ff147fe44c540028660935d59272eaa_Out_2_Vector4.xy)) );
if (_Property_c7dc38eaeea545aaada0e67cc32d9d25_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4, _Property_c7dc38eaeea545aaada0e67cc32d9d25_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_R_4_Float = _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4.r;
float _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_G_5_Float = _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4.g;
float _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_B_6_Float = _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4.b;
float _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_A_7_Float = _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4.a;
float4 _Property_857cbfa6469047f8904e0adc3eefe6b8_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_Emissive_Color) : _Emissive_Color;
float4 _Multiply_293fcf7127ab419cbcdff4183cccdba0_Out_2_Vector4;
Unity_Multiply_float4_float4(_SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4, _Property_857cbfa6469047f8904e0adc3eefe6b8_Out_0_Vector4, _Multiply_293fcf7127ab419cbcdff4183cccdba0_Out_2_Vector4);
#if defined(_USE_EMISSIVE)
float4 _UseEmissive_fdf874f7398e43a7838e4f04fd255375_Out_0_Vector4 = _Multiply_293fcf7127ab419cbcdff4183cccdba0_Out_2_Vector4;
#else
float4 _UseEmissive_fdf874f7398e43a7838e4f04fd255375_Out_0_Vector4 = float4(0, 0, 0, 0);
#endif
float _Property_3cef5a13363c421c89eb005848414161_Out_0_Float = _Metallic_Intensity;
UnityTexture2D _Property_6ced109efdc542d6868e0f24e23a1736_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_ORM_Map, sampler_ORM_Map, _ORM_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_6ced109efdc542d6868e0f24e23a1736_Out_0_Texture2D.tex, _Property_6ced109efdc542d6868e0f24e23a1736_Out_0_Texture2D.samplerstate, _Property_6ced109efdc542d6868e0f24e23a1736_Out_0_Texture2D.GetTransformedUV((_Divide_1ff147fe44c540028660935d59272eaa_Out_2_Vector4.xy)) );
if (_Property_6ced109efdc542d6868e0f24e23a1736_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_RGBA_0_Vector4, _Property_6ced109efdc542d6868e0f24e23a1736_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_R_4_Float = _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_RGBA_0_Vector4.r;
float _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_G_5_Float = _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_RGBA_0_Vector4.g;
float _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_B_6_Float = _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_RGBA_0_Vector4.b;
float _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_A_7_Float = _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_RGBA_0_Vector4.a;
float _Multiply_ffdbad83b5e74ce1b5ca4b9e7e162c40_Out_2_Float;
Unity_Multiply_float_float(_Property_3cef5a13363c421c89eb005848414161_Out_0_Float, _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_B_6_Float, _Multiply_ffdbad83b5e74ce1b5ca4b9e7e162c40_Out_2_Float);
UnityTexture2D _Property_75507ddd24ab4c63b88da2bbaec94f2c_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Coverage_ORM_Map, sampler_Coverage_ORM_Map, _Coverage_ORM_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_75507ddd24ab4c63b88da2bbaec94f2c_Out_0_Texture2D.tex, _Property_75507ddd24ab4c63b88da2bbaec94f2c_Out_0_Texture2D.samplerstate, _Property_75507ddd24ab4c63b88da2bbaec94f2c_Out_0_Texture2D.GetTransformedUV((_Divide_71dd940938164d5a9e7020e44b1cddf3_Out_2_Vector4.xy)) );
if (_Property_75507ddd24ab4c63b88da2bbaec94f2c_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_RGBA_0_Vector4, _Property_75507ddd24ab4c63b88da2bbaec94f2c_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_R_4_Float = _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_RGBA_0_Vector4.r;
float _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_G_5_Float = _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_RGBA_0_Vector4.g;
float _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_B_6_Float = _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_RGBA_0_Vector4.b;
float _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_A_7_Float = _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_RGBA_0_Vector4.a;
float _Lerp_3101e882d6a14372973d46e58ded048f_Out_3_Float;
Unity_Lerp_float(_Multiply_ffdbad83b5e74ce1b5ca4b9e7e162c40_Out_2_Float, _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_B_6_Float, _UseCoverageMaskTexture_f75c00eee68746bd80e2fa6c2bc28d4c_Out_0_Float, _Lerp_3101e882d6a14372973d46e58ded048f_Out_3_Float);
#if defined(_USE_COVERAGE)
float _UseCoverage_5268f0597a8b4df2a05cf3d5da317f6b_Out_0_Float = _Lerp_3101e882d6a14372973d46e58ded048f_Out_3_Float;
#else
float _UseCoverage_5268f0597a8b4df2a05cf3d5da317f6b_Out_0_Float = _Multiply_ffdbad83b5e74ce1b5ca4b9e7e162c40_Out_2_Float;
#endif
float _Property_d066edd24a97480b83f88a08c9a274b8_Out_0_Float = _Metallic_Intensity;
#if defined(_USE_ORM)
float _UseORM_11ec65ff6fc7436b9d5074e8f7a90024_Out_0_Float = _UseCoverage_5268f0597a8b4df2a05cf3d5da317f6b_Out_0_Float;
#else
float _UseORM_11ec65ff6fc7436b9d5074e8f7a90024_Out_0_Float = _Property_d066edd24a97480b83f88a08c9a274b8_Out_0_Float;
#endif
float _Property_ef0d0e2cdec043359496bb1a0782cda9_Out_0_Float = _Roughness_Intensity;
float _Multiply_4b10161d594b4863a7b31e2a1f1c1cf1_Out_2_Float;
Unity_Multiply_float_float(_Property_ef0d0e2cdec043359496bb1a0782cda9_Out_0_Float, _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_G_5_Float, _Multiply_4b10161d594b4863a7b31e2a1f1c1cf1_Out_2_Float);
float _OneMinus_d7dd0fb677e94540bbc094136ca963f5_Out_1_Float;
Unity_OneMinus_float(_Multiply_4b10161d594b4863a7b31e2a1f1c1cf1_Out_2_Float, _OneMinus_d7dd0fb677e94540bbc094136ca963f5_Out_1_Float);
float _OneMinus_bae441d794cd4092861f8242b9f96358_Out_1_Float;
Unity_OneMinus_float(_SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_G_5_Float, _OneMinus_bae441d794cd4092861f8242b9f96358_Out_1_Float);
float _Lerp_377c896eaf3243859da5c9b1446d27f5_Out_3_Float;
Unity_Lerp_float(_OneMinus_d7dd0fb677e94540bbc094136ca963f5_Out_1_Float, _OneMinus_bae441d794cd4092861f8242b9f96358_Out_1_Float, _UseCoverageMaskTexture_f75c00eee68746bd80e2fa6c2bc28d4c_Out_0_Float, _Lerp_377c896eaf3243859da5c9b1446d27f5_Out_3_Float);
#if defined(_USE_COVERAGE)
float _UseCoverage_58ec20069d0a4e0cb494b690fec0b60b_Out_0_Float = _Lerp_377c896eaf3243859da5c9b1446d27f5_Out_3_Float;
#else
float _UseCoverage_58ec20069d0a4e0cb494b690fec0b60b_Out_0_Float = _OneMinus_d7dd0fb677e94540bbc094136ca963f5_Out_1_Float;
#endif
float _Property_a7403d2892854317b50b8fa0268d0f27_Out_0_Float = _Roughness_Intensity;
float _OneMinus_3b59d96fc25b4b00a0364fda5bd47109_Out_1_Float;
Unity_OneMinus_float(_Property_a7403d2892854317b50b8fa0268d0f27_Out_0_Float, _OneMinus_3b59d96fc25b4b00a0364fda5bd47109_Out_1_Float);
#if defined(_USE_ORM)
float _UseORM_2aa3605b11694e28a61f126281dcd8f3_Out_0_Float = _UseCoverage_58ec20069d0a4e0cb494b690fec0b60b_Out_0_Float;
#else
float _UseORM_2aa3605b11694e28a61f126281dcd8f3_Out_0_Float = _OneMinus_3b59d96fc25b4b00a0364fda5bd47109_Out_1_Float;
#endif
float _Property_b578c28d617c46edb5452f0ee34e3698_Out_0_Float = _AO_Intensity;
float _Lerp_11f21031558c4c7db3e9b583ee8c5843_Out_3_Float;
Unity_Lerp_float(float(1), _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_R_4_Float, _Property_b578c28d617c46edb5452f0ee34e3698_Out_0_Float, _Lerp_11f21031558c4c7db3e9b583ee8c5843_Out_3_Float);
float _Lerp_f01b6ad8ed5e4f7aa0a392bc0bd1b199_Out_3_Float;
Unity_Lerp_float(_Lerp_11f21031558c4c7db3e9b583ee8c5843_Out_3_Float, _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_R_4_Float, _UseCoverageMaskTexture_f75c00eee68746bd80e2fa6c2bc28d4c_Out_0_Float, _Lerp_f01b6ad8ed5e4f7aa0a392bc0bd1b199_Out_3_Float);
#if defined(_USE_COVERAGE)
float _UseCoverage_ae6eaa1f25704217b662768dd49ad504_Out_0_Float = _Lerp_f01b6ad8ed5e4f7aa0a392bc0bd1b199_Out_3_Float;
#else
float _UseCoverage_ae6eaa1f25704217b662768dd49ad504_Out_0_Float = _Lerp_11f21031558c4c7db3e9b583ee8c5843_Out_3_Float;
#endif
#if defined(_USE_ORM)
float _UseORM_a8f4959fe3174af9bd41a7335d24f10e_Out_0_Float = _UseCoverage_ae6eaa1f25704217b662768dd49ad504_Out_0_Float;
#else
float _UseORM_a8f4959fe3174af9bd41a7335d24f10e_Out_0_Float = float(1);
#endif
surface.BaseColor = (_UseCoverage_c2f2c567080e4d5f9b652fbcbf667c54_Out_0_Vector4.xyz);
surface.NormalTS = _UseNormalMaps_cf0fe6c3e8224d69a92ff77f7f270e12_Out_0_Vector3;
surface.Emission = (_UseEmissive_fdf874f7398e43a7838e4f04fd255375_Out_0_Vector4.xyz);
surface.Metallic = _UseORM_11ec65ff6fc7436b9d5074e8f7a90024_Out_0_Float;
surface.Smoothness = _UseORM_2aa3605b11694e28a61f126281dcd8f3_Out_0_Float;
surface.Occlusion = _UseORM_a8f4959fe3174af9bd41a7335d24f10e_Out_0_Float;
float3 reactionUnusedColor = 0, reactionUnusedEmission = 0;
ClipVegetationFuel(TransformObjectToWorld(float3(0,0,0)));
ApplyVegetationReaction(TransformObjectToWorld(float3(0,0,0)),surface.BaseColor,surface.Emission);
return surface;
}

// --------------------------------------------------
// Build Graph Inputs
#ifdef HAVE_VFX_MODIFICATION
#define VFX_SRP_ATTRIBUTES Attributes
#define VFX_SRP_VARYINGS Varyings
#define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
#endif
VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

    output.ObjectSpaceNormal =                          input.normalOS;
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
    output.AbsoluteWorldSpacePosition =                 GetAbsolutePositionWS(TransformObjectToWorld(input.positionOS));
    output.TimeParameters =                             _TimeParameters.xyz;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

#ifdef HAVE_VFX_MODIFICATION
#if VFX_USE_GRAPH_VALUES
    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
#endif
    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */

#endif

    

    // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
    float3 unnormalizedNormalWS = input.normalWS;
    const float renormFactor = 1.0 / length(unnormalizedNormalWS);


    output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph
    output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);


    output.ObjectSpacePosition = TransformWorldToObject(input.positionWS);

    #if UNITY_UV_STARTS_AT_TOP
    #else
    #endif


    output.uv0 = input.texCoord0;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

// --------------------------------------------------
// Main

#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBRForwardPass.hlsl"

// --------------------------------------------------
// Visual Effect Vertex Invocations
#ifdef HAVE_VFX_MODIFICATION
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
#endif

ENDHLSL
}
Pass
{
    Name "GBuffer"
    Tags
    {
        "LightMode" = "UniversalGBuffer"
    }

// Render State
Cull Back
Blend One Zero
ZTest LEqual
ZWrite On

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 4.5
#pragma exclude_renderers gles3 glcore
#pragma multi_compile_instancing
#pragma instancing_options renderinglayer
#pragma vertex vert
#pragma fragment frag

// Keywords
#pragma multi_compile_fragment _ _SCREEN_SPACE_IRRADIANCE
#pragma multi_compile _ LIGHTMAP_ON
#pragma multi_compile _ DYNAMICLIGHTMAP_ON
#pragma multi_compile _ DIRLIGHTMAP_COMBINED
#pragma multi_compile _ USE_LEGACY_LIGHTMAPS
#pragma multi_compile _ LIGHTMAP_BICUBIC_SAMPLING
#pragma multi_compile _ REFLECTION_PROBE_ROTATION
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
#pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
#pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
#pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
#pragma multi_compile _ SHADOWS_SHADOWMASK
#pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
#pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
#pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
#pragma multi_compile_fragment _ _RENDER_PASS_ENABLED
#pragma multi_compile_fragment _ DEBUG_DISPLAY
#pragma multi_compile _ _CLUSTER_LIGHT_LOOP
#pragma shader_feature_local _ _USE_EMISSIVE
#pragma shader_feature_local _ _USE_WPO
#pragma shader_feature_local _ _USE_ORM
#pragma shader_feature_local _ _USE_NORMAL_MAPS
#pragma shader_feature_local _ _USE_COVERAGE
#pragma shader_feature_local _ _USE_COVERAGE_MASK_TEXTURE

#if defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_2
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_3
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_4
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_5
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_6
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM)
#define KEYWORD_PERMUTATION_7
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_8
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_9
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_10
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_11
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_12
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_13
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_14
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO)
#define KEYWORD_PERMUTATION_15
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_16
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_17
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_18
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_19
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_20
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_21
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_22
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM)
#define KEYWORD_PERMUTATION_23
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_24
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_25
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_26
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_27
#elif defined(_USE_EMISSIVE) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_28
#elif defined(_USE_EMISSIVE) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_29
#elif defined(_USE_EMISSIVE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_30
#elif defined(_USE_EMISSIVE)
#define KEYWORD_PERMUTATION_31
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_32
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_33
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_34
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_35
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_36
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_37
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_38
#elif defined(_USE_WPO) && defined(_USE_ORM)
#define KEYWORD_PERMUTATION_39
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_40
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_41
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_42
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_43
#elif defined(_USE_WPO) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_44
#elif defined(_USE_WPO) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_45
#elif defined(_USE_WPO) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_46
#elif defined(_USE_WPO)
#define KEYWORD_PERMUTATION_47
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_48
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_49
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_50
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_51
#elif defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_52
#elif defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_53
#elif defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_54
#elif defined(_USE_ORM)
#define KEYWORD_PERMUTATION_55
#elif defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_56
#elif defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_57
#elif defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_58
#elif defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_59
#elif defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_60
#elif defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_61
#elif defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_62
#else
#define KEYWORD_PERMUTATION_63
#endif


// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_TEXCOORD1
#define ATTRIBUTES_NEED_TEXCOORD2
#define GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_NORMAL_WS
#define VARYINGS_NEED_TANGENT_WS
#define VARYINGS_NEED_TEXCOORD0
#define VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
#define VARYINGS_NEED_SHADOW_COORD
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_GBUFFER
#define _FOG_FRAGMENT 1


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
 float3 positionOS : POSITION;
 float3 normalOS : NORMAL;
 float4 tangentOS : TANGENT;
 float4 uv0 : TEXCOORD0;
 float4 uv1 : TEXCOORD1;
 float4 uv2 : TEXCOORD2;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
 float4 positionCS : SV_POSITION;
 float3 positionWS;
 float3 normalWS;
 float4 tangentWS;
 float4 texCoord0;
#if defined(LIGHTMAP_ON)
 float2 staticLightmapUV;
#endif
#if defined(DYNAMICLIGHTMAP_ON)
 float2 dynamicLightmapUV;
#endif
#if !defined(LIGHTMAP_ON)
 float3 sh;
#endif
#if defined(USE_APV_PROBE_OCCLUSION)
 float4 probeOcclusion;
#endif
 float4 fogFactorAndVertexLight;
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
 float4 shadowCoord;
#endif
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};
struct SurfaceDescriptionInputs
{
 float3 WorldSpaceNormal;
 float3 TangentSpaceNormal;
 float3 ObjectSpacePosition;
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 AbsoluteWorldSpacePosition;
 float3 TimeParameters;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
#if defined(LIGHTMAP_ON)
 float2 staticLightmapUV : INTERP0;
#endif
#if defined(DYNAMICLIGHTMAP_ON)
 float2 dynamicLightmapUV : INTERP1;
#endif
#if !defined(LIGHTMAP_ON)
 float3 sh : INTERP2;
#endif
#if defined(USE_APV_PROBE_OCCLUSION)
 float4 probeOcclusion : INTERP3;
#endif
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
 float4 shadowCoord : INTERP4;
#endif
 float4 tangentWS : INTERP5;
 float4 texCoord0 : INTERP6;
 float4 fogFactorAndVertexLight : INTERP7;
 float3 positionWS : INTERP8;
 float3 normalWS : INTERP9;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};

PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.positionCS = input.positionCS;
#if defined(LIGHTMAP_ON)
output.staticLightmapUV = input.staticLightmapUV;
#endif
#if defined(DYNAMICLIGHTMAP_ON)
output.dynamicLightmapUV = input.dynamicLightmapUV;
#endif
#if !defined(LIGHTMAP_ON)
output.sh = input.sh;
#endif
#if defined(USE_APV_PROBE_OCCLUSION)
output.probeOcclusion = input.probeOcclusion;
#endif
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
output.shadowCoord = input.shadowCoord;
#endif
output.tangentWS.xyzw = input.tangentWS;
output.texCoord0.xyzw = input.texCoord0;
output.fogFactorAndVertexLight.xyzw = input.fogFactorAndVertexLight;
output.positionWS.xyz = input.positionWS;
output.normalWS.xyz = input.normalWS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.positionCS = input.positionCS;
#if defined(LIGHTMAP_ON)
output.staticLightmapUV = input.staticLightmapUV;
#endif
#if defined(DYNAMICLIGHTMAP_ON)
output.dynamicLightmapUV = input.dynamicLightmapUV;
#endif
#if !defined(LIGHTMAP_ON)
output.sh = input.sh;
#endif
#if defined(USE_APV_PROBE_OCCLUSION)
output.probeOcclusion = input.probeOcclusion;
#endif
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
output.shadowCoord = input.shadowCoord;
#endif
output.tangentWS = input.tangentWS.xyzw;
output.texCoord0 = input.texCoord0.xyzw;
output.fogFactorAndVertexLight = input.fogFactorAndVertexLight.xyzw;
output.positionWS = input.positionWS.xyz;
output.normalWS = input.normalWS.xyz;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}


// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)

float _EnvironmentReactionSupported;
float4 _EnvState0, _EnvState1;
float _FuelCoverage, _AllowFuelCoverage;
float4 _WetTint, _CharTint, _FireColor;
float4 _EnvironmentCellBounds;
float _UseEnvironmentCells;

float _Height_from_Ground_Randomness;
float _Height_from_Ground_Softness;
float _Normal_Pow;
float _Normal_Mul;
float _Mask_Map_Size;
float4 _Coverage_Normal_Map_TexelSize;
float _Mask_Map_Intensity;
float4 _Coverage_ORM_Map_TexelSize;
float _Height_Weight;
float _Height_Gradience;
float _Wind_Intensity;
float3 _Wind_Direction;
float _Wind_Speed;
float _Wind_Time_Random_Offset_Amount;
float _Texture_Size;
float4 _Color_Map_TexelSize;
float4 _Color_Tint;
float4 _Normal_Map_TexelSize;
float _Normal_Intensity;
float4 _ORM_Map_TexelSize;
float _Roughness_Intensity;
float _AO_Intensity;
float _Metallic_Intensity;
float4 _Emissive_Map_TexelSize;
float4 _Emissive_Color;
float4 _Coverage_Color_Map_TexelSize;
float _Coverage_Size;
float _Height_from_Ground;
float4 _Mask_Map_TexelSize;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Coverage_Normal_Map);
SAMPLER(sampler_Coverage_Normal_Map);
TEXTURE2D(_Coverage_ORM_Map);
SAMPLER(sampler_Coverage_ORM_Map);
TEXTURE2D(_Color_Map);
SAMPLER(sampler_Color_Map);
TEXTURE2D(_Normal_Map);
SAMPLER(sampler_Normal_Map);
TEXTURE2D(_ORM_Map);
SAMPLER(sampler_ORM_Map);
TEXTURE2D(_Emissive_Map);
SAMPLER(sampler_Emissive_Map);
TEXTURE2D(_Coverage_Color_Map);
SAMPLER(sampler_Coverage_Color_Map);
TEXTURE2D(_Mask_Map);
SAMPLER(sampler_Mask_Map);

// Graph Includes
// GraphIncludes: <None>

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Functions

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Sine_float(float In, out float Out)
{
    Out = sin(In);
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Length_float3(float3 In, out float Out)
{
    Out = length(In);
}

void Unity_Divide_float(float A, float B, out float Out)
{
    Out = A / B;
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
{
    Out = lerp(A, B, T);
}

struct Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float
{
float3 ObjectSpacePosition;
float3 TimeParameters;
};

void SG_WindSway_51c4027ae50a8cd49be53c4990b9f179_float(float _Sway_Wind_Speed, float3 _Sway_Wind_Direction, float _Sway_Wind_Intensity, float _Sway_Wind_Time_Random_Offset_Amount, Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float IN, out float3 OutVector3_1)
{
float _Property_5c4cd19861f64014ae8e16af9365ef49_Out_0_Float = _Sway_Wind_Time_Random_Offset_Amount;
float _Add_4b31033339dc4f8e8e71674d98e662a3_Out_2_Float;
Unity_Add_float(IN.TimeParameters.x, _Property_5c4cd19861f64014ae8e16af9365ef49_Out_0_Float, _Add_4b31033339dc4f8e8e71674d98e662a3_Out_2_Float);
float _Property_9a93fa589ac7409884a94247349a940f_Out_0_Float = _Sway_Wind_Speed;
float _Multiply_7418655003ae4fedb2bf68678629472c_Out_2_Float;
Unity_Multiply_float_float(_Add_4b31033339dc4f8e8e71674d98e662a3_Out_2_Float, _Property_9a93fa589ac7409884a94247349a940f_Out_0_Float, _Multiply_7418655003ae4fedb2bf68678629472c_Out_2_Float);
float _Sine_ebd14b0fc42b4b4cb4387f3a1b879c1f_Out_1_Float;
Unity_Sine_float(_Multiply_7418655003ae4fedb2bf68678629472c_Out_2_Float, _Sine_ebd14b0fc42b4b4cb4387f3a1b879c1f_Out_1_Float);
float _Add_660e92170c564f94a79277cd2fd0cf20_Out_2_Float;
Unity_Add_float(_Sine_ebd14b0fc42b4b4cb4387f3a1b879c1f_Out_1_Float, float(1), _Add_660e92170c564f94a79277cd2fd0cf20_Out_2_Float);
float _Multiply_b8fd654b771447aebafc1a6aad6957cf_Out_2_Float;
Unity_Multiply_float_float(_Add_660e92170c564f94a79277cd2fd0cf20_Out_2_Float, 0.5, _Multiply_b8fd654b771447aebafc1a6aad6957cf_Out_2_Float);
float3 _Property_fa2ee033b44946c59c3fa9aea4a167c8_Out_0_Vector3 = _Sway_Wind_Direction;
float3 _Multiply_a5500b7f7b8e43988a055eea7eb917f3_Out_2_Vector3;
Unity_Multiply_float3_float3((_Multiply_b8fd654b771447aebafc1a6aad6957cf_Out_2_Float.xxx), _Property_fa2ee033b44946c59c3fa9aea4a167c8_Out_0_Vector3, _Multiply_a5500b7f7b8e43988a055eea7eb917f3_Out_2_Vector3);
float _Property_66cba0a3223546238e18b58e017e756f_Out_0_Float = _Sway_Wind_Intensity;
float3 _Multiply_475f81423e39483cba045bef08b9f967_Out_2_Vector3;
Unity_Multiply_float3_float3(_Multiply_a5500b7f7b8e43988a055eea7eb917f3_Out_2_Vector3, (_Property_66cba0a3223546238e18b58e017e756f_Out_0_Float.xxx), _Multiply_475f81423e39483cba045bef08b9f967_Out_2_Vector3);
float3 _Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3;
Unity_Add_float3(_Multiply_475f81423e39483cba045bef08b9f967_Out_2_Vector3, IN.ObjectSpacePosition, _Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3);
float _Length_53965ffb38de43359ab019a247194557_Out_1_Float;
Unity_Length_float3(IN.ObjectSpacePosition, _Length_53965ffb38de43359ab019a247194557_Out_1_Float);
float _Length_77b6b3d742104f2abf3c6968dea9a342_Out_1_Float;
Unity_Length_float3(_Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3, _Length_77b6b3d742104f2abf3c6968dea9a342_Out_1_Float);
float _Divide_f5dd43de79e343638d2743ee93ce7326_Out_2_Float;
Unity_Divide_float(_Length_53965ffb38de43359ab019a247194557_Out_1_Float, _Length_77b6b3d742104f2abf3c6968dea9a342_Out_1_Float, _Divide_f5dd43de79e343638d2743ee93ce7326_Out_2_Float);
float3 _Lerp_7574daf550e1428cb9d35656c7bb9603_Out_3_Vector3;
Unity_Lerp_float3(float3(0, 0, 0), _Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3, (_Divide_f5dd43de79e343638d2743ee93ce7326_Out_2_Float.xxx), _Lerp_7574daf550e1428cb9d35656c7bb9603_Out_3_Vector3);
OutVector3_1 = _Lerp_7574daf550e1428cb9d35656c7bb9603_Out_3_Vector3;
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

void Unity_Power_float(float A, float B, out float Out)
{
    Out = pow(A, B);
}

void Unity_Divide_float4(float4 A, float4 B, out float4 Out)
{
    Out = A / B;
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

void Unity_Normalize_float3(float3 In, out float3 Out)
{
    Out = normalize(In);
}

void Unity_RandomRange_float(float2 Seed, float Min, float Max, out float Out)
{
     float randomno =  frac(sin(dot(Seed, float2(12.9898, 78.233)))*43758.5453);
     Out = lerp(Min, Max, randomno);
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
}

void Unity_DotProduct_float3(float3 A, float3 B, out float Out)
{
    Out = dot(A, B);
}

void Unity_Step_float(float Edge, float In, out float Out)
{
    Out = step(Edge, In);
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_NormalStrength_float(float3 In, float Strength, out float3 Out)
{
    Out = float3(In.rg * Strength, lerp(1, In.b, saturate(Strength)));
}

void Unity_Lerp_float(float A, float B, float T, out float Out)
{
    Out = lerp(A, B, T);
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 Position;
float3 Normal;
float3 Tangent;
};

VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
float _Property_b2e048f6276d4371b6ad931f5d1baa87_Out_0_Float = _Wind_Speed;
float3 _Property_7133a0551bc7429cb8bbfcbad044bf97_Out_0_Vector3 = _Wind_Direction;
float _Property_5aa2352867cd4da79dc9f9f9979925c7_Out_0_Float = _Wind_Intensity;
float _Property_e0c6ebff99e349d6966115347961a70c_Out_0_Float = _Wind_Time_Random_Offset_Amount;
Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf;
_WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf.ObjectSpacePosition = IN.ObjectSpacePosition;
_WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf.TimeParameters = IN.TimeParameters;
float3 _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf_OutVector3_1_Vector3;
SG_WindSway_51c4027ae50a8cd49be53c4990b9f179_float(_Property_b2e048f6276d4371b6ad931f5d1baa87_Out_0_Float, _Property_7133a0551bc7429cb8bbfcbad044bf97_Out_0_Vector3, _Property_5aa2352867cd4da79dc9f9f9979925c7_Out_0_Float, _Property_e0c6ebff99e349d6966115347961a70c_Out_0_Float, _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf, _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf_OutVector3_1_Vector3);
float _Split_38420bd2034746a1878048f0ead2a7b2_R_1_Float = IN.AbsoluteWorldSpacePosition[0];
float _Split_38420bd2034746a1878048f0ead2a7b2_G_2_Float = IN.AbsoluteWorldSpacePosition[1];
float _Split_38420bd2034746a1878048f0ead2a7b2_B_3_Float = IN.AbsoluteWorldSpacePosition[2];
float _Split_38420bd2034746a1878048f0ead2a7b2_A_4_Float = 0;
float3 _Vector3_0f8475b3a0df451ea79f6ea2e4e3f673_Out_0_Vector3 = float3(float(0), float(0), float(0));
float3 _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3;
{
// Converting Position from Object to AbsoluteWorld via world space
float3 world;
world = TransformObjectToWorld(_Vector3_0f8475b3a0df451ea79f6ea2e4e3f673_Out_0_Vector3.xyz);
_Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3 = GetAbsolutePositionWS(world);
}
float _Split_fb1b2d84926449e498da6c90f1436311_R_1_Float = _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3[0];
float _Split_fb1b2d84926449e498da6c90f1436311_G_2_Float = _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3[1];
float _Split_fb1b2d84926449e498da6c90f1436311_B_3_Float = _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3[2];
float _Split_fb1b2d84926449e498da6c90f1436311_A_4_Float = 0;
float _Subtract_18ddaa9851a547f1bc3eeafa55f3227b_Out_2_Float;
Unity_Subtract_float(_Split_38420bd2034746a1878048f0ead2a7b2_G_2_Float, _Split_fb1b2d84926449e498da6c90f1436311_G_2_Float, _Subtract_18ddaa9851a547f1bc3eeafa55f3227b_Out_2_Float);
float _Property_1db6873c977b47f186e42eef2cd75a57_Out_0_Float = _Height_Weight;
float _Split_bc5987163dba418d8de90e985de1247b_R_1_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[0];
float _Split_bc5987163dba418d8de90e985de1247b_G_2_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[1];
float _Split_bc5987163dba418d8de90e985de1247b_B_3_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[2];
float _Split_bc5987163dba418d8de90e985de1247b_A_4_Float = 0;
float _Multiply_89de5a5fb3cd451abb1227deba019c38_Out_2_Float;
Unity_Multiply_float_float(_Property_1db6873c977b47f186e42eef2cd75a57_Out_0_Float, _Split_bc5987163dba418d8de90e985de1247b_G_2_Float, _Multiply_89de5a5fb3cd451abb1227deba019c38_Out_2_Float);
float _Divide_2c406de6c63e463f9013e4faf1db1ae8_Out_2_Float;
Unity_Divide_float(_Subtract_18ddaa9851a547f1bc3eeafa55f3227b_Out_2_Float, _Multiply_89de5a5fb3cd451abb1227deba019c38_Out_2_Float, _Divide_2c406de6c63e463f9013e4faf1db1ae8_Out_2_Float);
float _Saturate_e9ea645ca89f482a9d0315df54a77ee5_Out_1_Float;
Unity_Saturate_float(_Divide_2c406de6c63e463f9013e4faf1db1ae8_Out_2_Float, _Saturate_e9ea645ca89f482a9d0315df54a77ee5_Out_1_Float);
float _Property_e96fd1920dff4533951da684129df200_Out_0_Float = _Height_Gradience;
float _Power_b88c5abc85424f17b03c60c98e9bc16c_Out_2_Float;
Unity_Power_float(_Saturate_e9ea645ca89f482a9d0315df54a77ee5_Out_1_Float, _Property_e96fd1920dff4533951da684129df200_Out_0_Float, _Power_b88c5abc85424f17b03c60c98e9bc16c_Out_2_Float);
float _Saturate_6c35d0fa108b483783bde88e06ff5ce0_Out_1_Float;
Unity_Saturate_float(_Power_b88c5abc85424f17b03c60c98e9bc16c_Out_2_Float, _Saturate_6c35d0fa108b483783bde88e06ff5ce0_Out_1_Float);
float3 _Lerp_8461a67d418941ee81ed9eb0f80e8a38_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf_OutVector3_1_Vector3, (_Saturate_6c35d0fa108b483783bde88e06ff5ce0_Out_1_Float.xxx), _Lerp_8461a67d418941ee81ed9eb0f80e8a38_Out_3_Vector3);
#if defined(_USE_WPO)
float3 _UseWPO_81cb92bff33547fbbfaa5ce2f60ba97c_Out_0_Vector3 = _Lerp_8461a67d418941ee81ed9eb0f80e8a38_Out_3_Vector3;
#else
float3 _UseWPO_81cb92bff33547fbbfaa5ce2f60ba97c_Out_0_Vector3 = IN.ObjectSpacePosition;
#endif
description.Position = _UseWPO_81cb92bff33547fbbfaa5ce2f60ba97c_Out_0_Vector3;
description.Normal = IN.ObjectSpaceNormal;
description.Tangent = IN.ObjectSpaceTangent;
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
float3 BaseColor;
float3 NormalTS;
float3 Emission;
float Metallic;
float Smoothness;
float Occlusion;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
UnityTexture2D _Property_0c79649b4042462682b4a7423715f1a1_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Color_Map, sampler_Color_Map, _Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_a9551365617a4a66b75c6a49ab8f9c5f_Out_0_Vector4 = IN.uv0;
float _Property_ac4a3a6f20bd42789825abf989f1a228_Out_0_Float = _Texture_Size;
float4 _Divide_1ff147fe44c540028660935d59272eaa_Out_2_Vector4;
Unity_Divide_float4(_UV_a9551365617a4a66b75c6a49ab8f9c5f_Out_0_Vector4, (_Property_ac4a3a6f20bd42789825abf989f1a228_Out_0_Float.xxxx), _Divide_1ff147fe44c540028660935d59272eaa_Out_2_Vector4);
float4 _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_0c79649b4042462682b4a7423715f1a1_Out_0_Texture2D.tex, _Property_0c79649b4042462682b4a7423715f1a1_Out_0_Texture2D.samplerstate, _Property_0c79649b4042462682b4a7423715f1a1_Out_0_Texture2D.GetTransformedUV((_Divide_1ff147fe44c540028660935d59272eaa_Out_2_Vector4.xy)) );
if (_Property_0c79649b4042462682b4a7423715f1a1_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4, _Property_0c79649b4042462682b4a7423715f1a1_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_R_4_Float = _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4.r;
float _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_G_5_Float = _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4.g;
float _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_B_6_Float = _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4.b;
float _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_A_7_Float = _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4.a;
float4 _Property_11be52d8078b41908ba72022455e8b98_Out_0_Vector4 = _Color_Tint;
float4 _Multiply_e3ef32e4a4e8442188dfd3c93394960c_Out_2_Vector4;
Unity_Multiply_float4_float4(_SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4, _Property_11be52d8078b41908ba72022455e8b98_Out_0_Vector4, _Multiply_e3ef32e4a4e8442188dfd3c93394960c_Out_2_Vector4);
UnityTexture2D _Property_415df38a141841e6aa11a7b1e336c379_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Coverage_Color_Map, sampler_Coverage_Color_Map, _Coverage_Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_0805a5601f5b49beb942e50ab3714e03_Out_0_Vector4 = IN.uv0;
float _Property_eb7418710f3f4dc6981c01445b1a809e_Out_0_Float = _Coverage_Size;
float4 _Divide_71dd940938164d5a9e7020e44b1cddf3_Out_2_Vector4;
Unity_Divide_float4(_UV_0805a5601f5b49beb942e50ab3714e03_Out_0_Vector4, (_Property_eb7418710f3f4dc6981c01445b1a809e_Out_0_Float.xxxx), _Divide_71dd940938164d5a9e7020e44b1cddf3_Out_2_Vector4);
float4 _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_415df38a141841e6aa11a7b1e336c379_Out_0_Texture2D.tex, _Property_415df38a141841e6aa11a7b1e336c379_Out_0_Texture2D.samplerstate, _Property_415df38a141841e6aa11a7b1e336c379_Out_0_Texture2D.GetTransformedUV((_Divide_71dd940938164d5a9e7020e44b1cddf3_Out_2_Vector4.xy)) );
if (_Property_415df38a141841e6aa11a7b1e336c379_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4, _Property_415df38a141841e6aa11a7b1e336c379_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_R_4_Float = _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4.r;
float _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_G_5_Float = _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4.g;
float _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_B_6_Float = _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4.b;
float _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_A_7_Float = _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4.a;
UnityTexture2D _Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Mask_Map, sampler_Mask_Map, _Mask_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_c2c5030522744aceb603ff104c9ccde0_Out_0_Vector4 = IN.uv0;
float _Property_a975da4683af46ab85b4a95b01cb9f32_Out_0_Float = _Mask_Map_Size;
float4 _Divide_3dd829d125dc4ec1b32772766d56a17b_Out_2_Vector4;
Unity_Divide_float4(_UV_c2c5030522744aceb603ff104c9ccde0_Out_0_Vector4, (_Property_a975da4683af46ab85b4a95b01cb9f32_Out_0_Float.xxxx), _Divide_3dd829d125dc4ec1b32772766d56a17b_Out_2_Vector4);
float4 _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D.tex, _Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D.samplerstate, _Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D.GetTransformedUV((_Divide_3dd829d125dc4ec1b32772766d56a17b_Out_2_Vector4.xy)) );
if (_Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4, _Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_R_4_Float = _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4.r;
float _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_G_5_Float = _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4.g;
float _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_B_6_Float = _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4.b;
float _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_A_7_Float = _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4.a;
float _Multiply_81f5f8bc82304adbae345a5fe4b0024b_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_R_4_Float, 0.1, _Multiply_81f5f8bc82304adbae345a5fe4b0024b_Out_2_Float);
float _Add_33507c032b9e48bfb61763d507bb031c_Out_2_Float;
Unity_Add_float(_Multiply_81f5f8bc82304adbae345a5fe4b0024b_Out_2_Float, float(0.9), _Add_33507c032b9e48bfb61763d507bb031c_Out_2_Float);
float _Property_883f8e1288fd43f1a3674ca1f5364253_Out_0_Float = _Mask_Map_Intensity;
float _Power_b45fec9ffb074e40b5cb5452c0fb982d_Out_2_Float;
Unity_Power_float(_Add_33507c032b9e48bfb61763d507bb031c_Out_2_Float, _Property_883f8e1288fd43f1a3674ca1f5364253_Out_0_Float, _Power_b45fec9ffb074e40b5cb5452c0fb982d_Out_2_Float);
float _Split_b6a9cc69e69c417ba235626d7beed2eb_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_b6a9cc69e69c417ba235626d7beed2eb_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_b6a9cc69e69c417ba235626d7beed2eb_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_b6a9cc69e69c417ba235626d7beed2eb_A_4_Float = 0;
float _Property_73ac42b2cd3d4b43976ac13e73b6de40_Out_0_Float = _Height_from_Ground;
float3 _Transform_425aee5b4b5e409eb94b431642b515fb_Out_1_Vector3;
_Transform_425aee5b4b5e409eb94b431642b515fb_Out_1_Vector3 = TransformObjectToWorld(float3 (0, 0, 0).xyz);
float3 _Normalize_960b1f24c996479a8c1a9a81cabcb835_Out_1_Vector3;
Unity_Normalize_float3(_Transform_425aee5b4b5e409eb94b431642b515fb_Out_1_Vector3, _Normalize_960b1f24c996479a8c1a9a81cabcb835_Out_1_Vector3);
float _Property_11f1157c9f364521bf765c261e8bb93e_Out_0_Float = _Height_from_Ground_Randomness;
float _Multiply_b7a3ed973f104b9a84af846ce299d95f_Out_2_Float;
Unity_Multiply_float_float(_Property_11f1157c9f364521bf765c261e8bb93e_Out_0_Float, -1, _Multiply_b7a3ed973f104b9a84af846ce299d95f_Out_2_Float);
float _RandomRange_82fa55b7f4da4f6ebc597a38d2ca2e6d_Out_3_Float;
Unity_RandomRange_float((_Normalize_960b1f24c996479a8c1a9a81cabcb835_Out_1_Vector3.xy), _Multiply_b7a3ed973f104b9a84af846ce299d95f_Out_2_Float, _Property_11f1157c9f364521bf765c261e8bb93e_Out_0_Float, _RandomRange_82fa55b7f4da4f6ebc597a38d2ca2e6d_Out_3_Float);
float _Add_109ea06d643c4826b0138a85becc2fa9_Out_2_Float;
Unity_Add_float(_Property_73ac42b2cd3d4b43976ac13e73b6de40_Out_0_Float, _RandomRange_82fa55b7f4da4f6ebc597a38d2ca2e6d_Out_3_Float, _Add_109ea06d643c4826b0138a85becc2fa9_Out_2_Float);
float _Subtract_fe60462bdbdd4a22a207774a6a8cb1b8_Out_2_Float;
Unity_Subtract_float(_Split_b6a9cc69e69c417ba235626d7beed2eb_G_2_Float, _Add_109ea06d643c4826b0138a85becc2fa9_Out_2_Float, _Subtract_fe60462bdbdd4a22a207774a6a8cb1b8_Out_2_Float);
float _Property_1487773737e44e78a7c514c7f470e603_Out_0_Float = _Height_from_Ground_Softness;
float _Divide_6eb043a8aee54d87a0c73169597bdcba_Out_2_Float;
Unity_Divide_float(_Subtract_fe60462bdbdd4a22a207774a6a8cb1b8_Out_2_Float, _Property_1487773737e44e78a7c514c7f470e603_Out_0_Float, _Divide_6eb043a8aee54d87a0c73169597bdcba_Out_2_Float);
float _Saturate_d8ec125670414d1888aeec654fcb04e2_Out_1_Float;
Unity_Saturate_float(_Divide_6eb043a8aee54d87a0c73169597bdcba_Out_2_Float, _Saturate_d8ec125670414d1888aeec654fcb04e2_Out_1_Float);
float _OneMinus_de85154fa1b742e3b6c9e0f92b85d96c_Out_1_Float;
Unity_OneMinus_float(_Saturate_d8ec125670414d1888aeec654fcb04e2_Out_1_Float, _OneMinus_de85154fa1b742e3b6c9e0f92b85d96c_Out_1_Float);
float _DotProduct_a5624fdd7fd94a4c82ad8c039922e638_Out_2_Float;
Unity_DotProduct_float3(IN.WorldSpaceNormal, float3(0, 1, 0), _DotProduct_a5624fdd7fd94a4c82ad8c039922e638_Out_2_Float);
float _Property_1d95c04e88f443bc91fe7ed217b674b8_Out_0_Float = _Normal_Pow;
float _Power_5b713b10f3e049f8a97be81969b52b7d_Out_2_Float;
Unity_Power_float(_DotProduct_a5624fdd7fd94a4c82ad8c039922e638_Out_2_Float, _Property_1d95c04e88f443bc91fe7ed217b674b8_Out_0_Float, _Power_5b713b10f3e049f8a97be81969b52b7d_Out_2_Float);
float _Property_6314206eb47742698a7d2833f119f39e_Out_0_Float = _Normal_Mul;
float _Multiply_6e6bcfc2a4df414e86d241952840e972_Out_2_Float;
Unity_Multiply_float_float(_Power_5b713b10f3e049f8a97be81969b52b7d_Out_2_Float, _Property_6314206eb47742698a7d2833f119f39e_Out_0_Float, _Multiply_6e6bcfc2a4df414e86d241952840e972_Out_2_Float);
float _Saturate_77d41005224e417eb678b78b9a10771a_Out_1_Float;
Unity_Saturate_float(_Multiply_6e6bcfc2a4df414e86d241952840e972_Out_2_Float, _Saturate_77d41005224e417eb678b78b9a10771a_Out_1_Float);
float _Add_8395f84ea8e546988dd02e5b922d32ee_Out_2_Float;
Unity_Add_float(_OneMinus_de85154fa1b742e3b6c9e0f92b85d96c_Out_1_Float, _Saturate_77d41005224e417eb678b78b9a10771a_Out_1_Float, _Add_8395f84ea8e546988dd02e5b922d32ee_Out_2_Float);
float _Multiply_b61ead58dd024fd7aacad7c0b68f3be7_Out_2_Float;
Unity_Multiply_float_float(5, _Add_8395f84ea8e546988dd02e5b922d32ee_Out_2_Float, _Multiply_b61ead58dd024fd7aacad7c0b68f3be7_Out_2_Float);
float _Multiply_b0c3a49303544ccaa1d15379e6b1e9d2_Out_2_Float;
Unity_Multiply_float_float(_Power_b45fec9ffb074e40b5cb5452c0fb982d_Out_2_Float, _Multiply_b61ead58dd024fd7aacad7c0b68f3be7_Out_2_Float, _Multiply_b0c3a49303544ccaa1d15379e6b1e9d2_Out_2_Float);
float _Step_9e404f0c811346c88c531f42e623243f_Out_2_Float;
Unity_Step_float(_Subtract_fe60462bdbdd4a22a207774a6a8cb1b8_Out_2_Float, float(-0.1), _Step_9e404f0c811346c88c531f42e623243f_Out_2_Float);
float _Add_0328340190574386b2d37a0d035b66fb_Out_2_Float;
Unity_Add_float(_Multiply_b0c3a49303544ccaa1d15379e6b1e9d2_Out_2_Float, _Step_9e404f0c811346c88c531f42e623243f_Out_2_Float, _Add_0328340190574386b2d37a0d035b66fb_Out_2_Float);
float _Power_3bfc30934311465db3ba28e64b49097d_Out_2_Float;
Unity_Power_float(_Add_0328340190574386b2d37a0d035b66fb_Out_2_Float, float(3), _Power_3bfc30934311465db3ba28e64b49097d_Out_2_Float);
float _Saturate_3ba4a70af5214b4086803937a3f8b09f_Out_1_Float;
Unity_Saturate_float(_Power_3bfc30934311465db3ba28e64b49097d_Out_2_Float, _Saturate_3ba4a70af5214b4086803937a3f8b09f_Out_1_Float);
float _Saturate_6e423c9f5f1249088750a2b84a42b764_Out_1_Float;
Unity_Saturate_float(_Add_8395f84ea8e546988dd02e5b922d32ee_Out_2_Float, _Saturate_6e423c9f5f1249088750a2b84a42b764_Out_1_Float);
#if defined(_USE_COVERAGE_MASK_TEXTURE)
float _UseCoverageMaskTexture_f75c00eee68746bd80e2fa6c2bc28d4c_Out_0_Float = _Saturate_3ba4a70af5214b4086803937a3f8b09f_Out_1_Float;
#else
float _UseCoverageMaskTexture_f75c00eee68746bd80e2fa6c2bc28d4c_Out_0_Float = _Saturate_6e423c9f5f1249088750a2b84a42b764_Out_1_Float;
#endif
float4 _Lerp_c6c7a7472707418491f2fa10989f05b9_Out_3_Vector4;
Unity_Lerp_float4(_Multiply_e3ef32e4a4e8442188dfd3c93394960c_Out_2_Vector4, _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4, (_UseCoverageMaskTexture_f75c00eee68746bd80e2fa6c2bc28d4c_Out_0_Float.xxxx), _Lerp_c6c7a7472707418491f2fa10989f05b9_Out_3_Vector4);
#if defined(_USE_COVERAGE)
float4 _UseCoverage_c2f2c567080e4d5f9b652fbcbf667c54_Out_0_Vector4 = _Lerp_c6c7a7472707418491f2fa10989f05b9_Out_3_Vector4;
#else
float4 _UseCoverage_c2f2c567080e4d5f9b652fbcbf667c54_Out_0_Vector4 = _Multiply_e3ef32e4a4e8442188dfd3c93394960c_Out_2_Vector4;
#endif
UnityTexture2D _Property_a38bf1e56c364aa0add1f01a99eb6c3e_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Normal_Map, sampler_Normal_Map, _Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_a38bf1e56c364aa0add1f01a99eb6c3e_Out_0_Texture2D.tex, _Property_a38bf1e56c364aa0add1f01a99eb6c3e_Out_0_Texture2D.samplerstate, _Property_a38bf1e56c364aa0add1f01a99eb6c3e_Out_0_Texture2D.GetTransformedUV((_Divide_1ff147fe44c540028660935d59272eaa_Out_2_Vector4.xy)) );
if (_Property_a38bf1e56c364aa0add1f01a99eb6c3e_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4, _Property_a38bf1e56c364aa0add1f01a99eb6c3e_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4);
float _SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_R_4_Float = _SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4.r;
float _SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_G_5_Float = _SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4.g;
float _SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_B_6_Float = _SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4.b;
float _SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_A_7_Float = _SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4.a;
float _Property_ff6f57141462418790b8f0ef1d7d69cc_Out_0_Float = _Normal_Intensity;
float3 _NormalStrength_6e1c84dc8cbd47f988a654d24e83669f_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4.xyz), _Property_ff6f57141462418790b8f0ef1d7d69cc_Out_0_Float, _NormalStrength_6e1c84dc8cbd47f988a654d24e83669f_Out_2_Vector3);
#if defined(_USE_NORMAL_MAPS)
float3 _UseNormalMaps_f01546d53fa54c3191638b3fe3736688_Out_0_Vector3 = _NormalStrength_6e1c84dc8cbd47f988a654d24e83669f_Out_2_Vector3;
#else
float3 _UseNormalMaps_f01546d53fa54c3191638b3fe3736688_Out_0_Vector3 = float3(0, 0, 1);
#endif
UnityTexture2D _Property_15070824f15143aea7ede9b130164716_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Coverage_Normal_Map, sampler_Coverage_Normal_Map, _Coverage_Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_15070824f15143aea7ede9b130164716_Out_0_Texture2D.tex, _Property_15070824f15143aea7ede9b130164716_Out_0_Texture2D.samplerstate, _Property_15070824f15143aea7ede9b130164716_Out_0_Texture2D.GetTransformedUV((_Divide_71dd940938164d5a9e7020e44b1cddf3_Out_2_Vector4.xy)) );
if (_Property_15070824f15143aea7ede9b130164716_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4, _Property_15070824f15143aea7ede9b130164716_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4);
float _SampleTexture2D_26c139222fa1483d83940748db6d922f_R_4_Float = _SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4.r;
float _SampleTexture2D_26c139222fa1483d83940748db6d922f_G_5_Float = _SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4.g;
float _SampleTexture2D_26c139222fa1483d83940748db6d922f_B_6_Float = _SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4.b;
float _SampleTexture2D_26c139222fa1483d83940748db6d922f_A_7_Float = _SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4.a;
float3 _Lerp_a5163d5f017c46b9b347647a7d361156_Out_3_Vector3;
Unity_Lerp_float3(_UseNormalMaps_f01546d53fa54c3191638b3fe3736688_Out_0_Vector3, (_SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4.xyz), (_UseCoverageMaskTexture_f75c00eee68746bd80e2fa6c2bc28d4c_Out_0_Float.xxx), _Lerp_a5163d5f017c46b9b347647a7d361156_Out_3_Vector3);
#if defined(_USE_COVERAGE)
float3 _UseCoverage_d6109394a0154e89890cf6ca228f700f_Out_0_Vector3 = _Lerp_a5163d5f017c46b9b347647a7d361156_Out_3_Vector3;
#else
float3 _UseCoverage_d6109394a0154e89890cf6ca228f700f_Out_0_Vector3 = _UseNormalMaps_f01546d53fa54c3191638b3fe3736688_Out_0_Vector3;
#endif
#if defined(_USE_NORMAL_MAPS)
float3 _UseNormalMaps_cf0fe6c3e8224d69a92ff77f7f270e12_Out_0_Vector3 = _UseCoverage_d6109394a0154e89890cf6ca228f700f_Out_0_Vector3;
#else
float3 _UseNormalMaps_cf0fe6c3e8224d69a92ff77f7f270e12_Out_0_Vector3 = float3(0, 0, 1);
#endif
UnityTexture2D _Property_c7dc38eaeea545aaada0e67cc32d9d25_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Emissive_Map, sampler_Emissive_Map, _Emissive_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_c7dc38eaeea545aaada0e67cc32d9d25_Out_0_Texture2D.tex, _Property_c7dc38eaeea545aaada0e67cc32d9d25_Out_0_Texture2D.samplerstate, _Property_c7dc38eaeea545aaada0e67cc32d9d25_Out_0_Texture2D.GetTransformedUV((_Divide_1ff147fe44c540028660935d59272eaa_Out_2_Vector4.xy)) );
if (_Property_c7dc38eaeea545aaada0e67cc32d9d25_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4, _Property_c7dc38eaeea545aaada0e67cc32d9d25_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_R_4_Float = _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4.r;
float _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_G_5_Float = _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4.g;
float _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_B_6_Float = _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4.b;
float _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_A_7_Float = _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4.a;
float4 _Property_857cbfa6469047f8904e0adc3eefe6b8_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_Emissive_Color) : _Emissive_Color;
float4 _Multiply_293fcf7127ab419cbcdff4183cccdba0_Out_2_Vector4;
Unity_Multiply_float4_float4(_SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4, _Property_857cbfa6469047f8904e0adc3eefe6b8_Out_0_Vector4, _Multiply_293fcf7127ab419cbcdff4183cccdba0_Out_2_Vector4);
#if defined(_USE_EMISSIVE)
float4 _UseEmissive_fdf874f7398e43a7838e4f04fd255375_Out_0_Vector4 = _Multiply_293fcf7127ab419cbcdff4183cccdba0_Out_2_Vector4;
#else
float4 _UseEmissive_fdf874f7398e43a7838e4f04fd255375_Out_0_Vector4 = float4(0, 0, 0, 0);
#endif
float _Property_3cef5a13363c421c89eb005848414161_Out_0_Float = _Metallic_Intensity;
UnityTexture2D _Property_6ced109efdc542d6868e0f24e23a1736_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_ORM_Map, sampler_ORM_Map, _ORM_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_6ced109efdc542d6868e0f24e23a1736_Out_0_Texture2D.tex, _Property_6ced109efdc542d6868e0f24e23a1736_Out_0_Texture2D.samplerstate, _Property_6ced109efdc542d6868e0f24e23a1736_Out_0_Texture2D.GetTransformedUV((_Divide_1ff147fe44c540028660935d59272eaa_Out_2_Vector4.xy)) );
if (_Property_6ced109efdc542d6868e0f24e23a1736_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_RGBA_0_Vector4, _Property_6ced109efdc542d6868e0f24e23a1736_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_R_4_Float = _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_RGBA_0_Vector4.r;
float _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_G_5_Float = _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_RGBA_0_Vector4.g;
float _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_B_6_Float = _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_RGBA_0_Vector4.b;
float _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_A_7_Float = _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_RGBA_0_Vector4.a;
float _Multiply_ffdbad83b5e74ce1b5ca4b9e7e162c40_Out_2_Float;
Unity_Multiply_float_float(_Property_3cef5a13363c421c89eb005848414161_Out_0_Float, _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_B_6_Float, _Multiply_ffdbad83b5e74ce1b5ca4b9e7e162c40_Out_2_Float);
UnityTexture2D _Property_75507ddd24ab4c63b88da2bbaec94f2c_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Coverage_ORM_Map, sampler_Coverage_ORM_Map, _Coverage_ORM_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_75507ddd24ab4c63b88da2bbaec94f2c_Out_0_Texture2D.tex, _Property_75507ddd24ab4c63b88da2bbaec94f2c_Out_0_Texture2D.samplerstate, _Property_75507ddd24ab4c63b88da2bbaec94f2c_Out_0_Texture2D.GetTransformedUV((_Divide_71dd940938164d5a9e7020e44b1cddf3_Out_2_Vector4.xy)) );
if (_Property_75507ddd24ab4c63b88da2bbaec94f2c_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_RGBA_0_Vector4, _Property_75507ddd24ab4c63b88da2bbaec94f2c_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_R_4_Float = _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_RGBA_0_Vector4.r;
float _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_G_5_Float = _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_RGBA_0_Vector4.g;
float _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_B_6_Float = _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_RGBA_0_Vector4.b;
float _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_A_7_Float = _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_RGBA_0_Vector4.a;
float _Lerp_3101e882d6a14372973d46e58ded048f_Out_3_Float;
Unity_Lerp_float(_Multiply_ffdbad83b5e74ce1b5ca4b9e7e162c40_Out_2_Float, _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_B_6_Float, _UseCoverageMaskTexture_f75c00eee68746bd80e2fa6c2bc28d4c_Out_0_Float, _Lerp_3101e882d6a14372973d46e58ded048f_Out_3_Float);
#if defined(_USE_COVERAGE)
float _UseCoverage_5268f0597a8b4df2a05cf3d5da317f6b_Out_0_Float = _Lerp_3101e882d6a14372973d46e58ded048f_Out_3_Float;
#else
float _UseCoverage_5268f0597a8b4df2a05cf3d5da317f6b_Out_0_Float = _Multiply_ffdbad83b5e74ce1b5ca4b9e7e162c40_Out_2_Float;
#endif
float _Property_d066edd24a97480b83f88a08c9a274b8_Out_0_Float = _Metallic_Intensity;
#if defined(_USE_ORM)
float _UseORM_11ec65ff6fc7436b9d5074e8f7a90024_Out_0_Float = _UseCoverage_5268f0597a8b4df2a05cf3d5da317f6b_Out_0_Float;
#else
float _UseORM_11ec65ff6fc7436b9d5074e8f7a90024_Out_0_Float = _Property_d066edd24a97480b83f88a08c9a274b8_Out_0_Float;
#endif
float _Property_ef0d0e2cdec043359496bb1a0782cda9_Out_0_Float = _Roughness_Intensity;
float _Multiply_4b10161d594b4863a7b31e2a1f1c1cf1_Out_2_Float;
Unity_Multiply_float_float(_Property_ef0d0e2cdec043359496bb1a0782cda9_Out_0_Float, _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_G_5_Float, _Multiply_4b10161d594b4863a7b31e2a1f1c1cf1_Out_2_Float);
float _OneMinus_d7dd0fb677e94540bbc094136ca963f5_Out_1_Float;
Unity_OneMinus_float(_Multiply_4b10161d594b4863a7b31e2a1f1c1cf1_Out_2_Float, _OneMinus_d7dd0fb677e94540bbc094136ca963f5_Out_1_Float);
float _OneMinus_bae441d794cd4092861f8242b9f96358_Out_1_Float;
Unity_OneMinus_float(_SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_G_5_Float, _OneMinus_bae441d794cd4092861f8242b9f96358_Out_1_Float);
float _Lerp_377c896eaf3243859da5c9b1446d27f5_Out_3_Float;
Unity_Lerp_float(_OneMinus_d7dd0fb677e94540bbc094136ca963f5_Out_1_Float, _OneMinus_bae441d794cd4092861f8242b9f96358_Out_1_Float, _UseCoverageMaskTexture_f75c00eee68746bd80e2fa6c2bc28d4c_Out_0_Float, _Lerp_377c896eaf3243859da5c9b1446d27f5_Out_3_Float);
#if defined(_USE_COVERAGE)
float _UseCoverage_58ec20069d0a4e0cb494b690fec0b60b_Out_0_Float = _Lerp_377c896eaf3243859da5c9b1446d27f5_Out_3_Float;
#else
float _UseCoverage_58ec20069d0a4e0cb494b690fec0b60b_Out_0_Float = _OneMinus_d7dd0fb677e94540bbc094136ca963f5_Out_1_Float;
#endif
float _Property_a7403d2892854317b50b8fa0268d0f27_Out_0_Float = _Roughness_Intensity;
float _OneMinus_3b59d96fc25b4b00a0364fda5bd47109_Out_1_Float;
Unity_OneMinus_float(_Property_a7403d2892854317b50b8fa0268d0f27_Out_0_Float, _OneMinus_3b59d96fc25b4b00a0364fda5bd47109_Out_1_Float);
#if defined(_USE_ORM)
float _UseORM_2aa3605b11694e28a61f126281dcd8f3_Out_0_Float = _UseCoverage_58ec20069d0a4e0cb494b690fec0b60b_Out_0_Float;
#else
float _UseORM_2aa3605b11694e28a61f126281dcd8f3_Out_0_Float = _OneMinus_3b59d96fc25b4b00a0364fda5bd47109_Out_1_Float;
#endif
float _Property_b578c28d617c46edb5452f0ee34e3698_Out_0_Float = _AO_Intensity;
float _Lerp_11f21031558c4c7db3e9b583ee8c5843_Out_3_Float;
Unity_Lerp_float(float(1), _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_R_4_Float, _Property_b578c28d617c46edb5452f0ee34e3698_Out_0_Float, _Lerp_11f21031558c4c7db3e9b583ee8c5843_Out_3_Float);
float _Lerp_f01b6ad8ed5e4f7aa0a392bc0bd1b199_Out_3_Float;
Unity_Lerp_float(_Lerp_11f21031558c4c7db3e9b583ee8c5843_Out_3_Float, _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_R_4_Float, _UseCoverageMaskTexture_f75c00eee68746bd80e2fa6c2bc28d4c_Out_0_Float, _Lerp_f01b6ad8ed5e4f7aa0a392bc0bd1b199_Out_3_Float);
#if defined(_USE_COVERAGE)
float _UseCoverage_ae6eaa1f25704217b662768dd49ad504_Out_0_Float = _Lerp_f01b6ad8ed5e4f7aa0a392bc0bd1b199_Out_3_Float;
#else
float _UseCoverage_ae6eaa1f25704217b662768dd49ad504_Out_0_Float = _Lerp_11f21031558c4c7db3e9b583ee8c5843_Out_3_Float;
#endif
#if defined(_USE_ORM)
float _UseORM_a8f4959fe3174af9bd41a7335d24f10e_Out_0_Float = _UseCoverage_ae6eaa1f25704217b662768dd49ad504_Out_0_Float;
#else
float _UseORM_a8f4959fe3174af9bd41a7335d24f10e_Out_0_Float = float(1);
#endif
surface.BaseColor = (_UseCoverage_c2f2c567080e4d5f9b652fbcbf667c54_Out_0_Vector4.xyz);
surface.NormalTS = _UseNormalMaps_cf0fe6c3e8224d69a92ff77f7f270e12_Out_0_Vector3;
surface.Emission = (_UseEmissive_fdf874f7398e43a7838e4f04fd255375_Out_0_Vector4.xyz);
surface.Metallic = _UseORM_11ec65ff6fc7436b9d5074e8f7a90024_Out_0_Float;
surface.Smoothness = _UseORM_2aa3605b11694e28a61f126281dcd8f3_Out_0_Float;
surface.Occlusion = _UseORM_a8f4959fe3174af9bd41a7335d24f10e_Out_0_Float;
float3 reactionUnusedColor = 0, reactionUnusedEmission = 0;
ClipVegetationFuel(TransformObjectToWorld(float3(0,0,0)));
ApplyVegetationReaction(TransformObjectToWorld(float3(0,0,0)),surface.BaseColor,surface.Emission);
return surface;
}

// --------------------------------------------------
// Build Graph Inputs
#ifdef HAVE_VFX_MODIFICATION
#define VFX_SRP_ATTRIBUTES Attributes
#define VFX_SRP_VARYINGS Varyings
#define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
#endif
VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

    output.ObjectSpaceNormal =                          input.normalOS;
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
    output.AbsoluteWorldSpacePosition =                 GetAbsolutePositionWS(TransformObjectToWorld(input.positionOS));
    output.TimeParameters =                             _TimeParameters.xyz;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

#ifdef HAVE_VFX_MODIFICATION
#if VFX_USE_GRAPH_VALUES
    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
#endif
    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */

#endif

    

    // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
    float3 unnormalizedNormalWS = input.normalWS;
    const float renormFactor = 1.0 / length(unnormalizedNormalWS);


    output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph
    output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);


    output.ObjectSpacePosition = TransformWorldToObject(input.positionWS);

    #if UNITY_UV_STARTS_AT_TOP
    #else
    #endif


    output.uv0 = input.texCoord0;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

// --------------------------------------------------
// Main

#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GBufferOutput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBRGBufferPass.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GBufferOutputFormat.hlsl"

// --------------------------------------------------
// Visual Effect Vertex Invocations
#ifdef HAVE_VFX_MODIFICATION
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
#endif

ENDHLSL
}
Pass
{
    Name "ShadowCaster"
    Tags
    {
        "LightMode" = "ShadowCaster"
    }

// Render State
Cull Back
ZTest LEqual
ZWrite On
ColorMask 0

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 2.0
#pragma multi_compile_instancing
#pragma vertex vert
#pragma fragment frag

// Keywords
#pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
#pragma shader_feature_local _ _USE_EMISSIVE
#pragma shader_feature_local _ _USE_WPO
#pragma shader_feature_local _ _USE_ORM
#pragma shader_feature_local _ _USE_NORMAL_MAPS
#pragma shader_feature_local _ _USE_COVERAGE
#pragma shader_feature_local _ _USE_COVERAGE_MASK_TEXTURE

#if defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_2
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_3
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_4
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_5
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_6
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM)
#define KEYWORD_PERMUTATION_7
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_8
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_9
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_10
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_11
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_12
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_13
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_14
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO)
#define KEYWORD_PERMUTATION_15
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_16
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_17
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_18
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_19
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_20
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_21
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_22
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM)
#define KEYWORD_PERMUTATION_23
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_24
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_25
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_26
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_27
#elif defined(_USE_EMISSIVE) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_28
#elif defined(_USE_EMISSIVE) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_29
#elif defined(_USE_EMISSIVE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_30
#elif defined(_USE_EMISSIVE)
#define KEYWORD_PERMUTATION_31
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_32
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_33
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_34
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_35
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_36
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_37
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_38
#elif defined(_USE_WPO) && defined(_USE_ORM)
#define KEYWORD_PERMUTATION_39
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_40
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_41
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_42
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_43
#elif defined(_USE_WPO) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_44
#elif defined(_USE_WPO) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_45
#elif defined(_USE_WPO) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_46
#elif defined(_USE_WPO)
#define KEYWORD_PERMUTATION_47
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_48
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_49
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_50
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_51
#elif defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_52
#elif defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_53
#elif defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_54
#elif defined(_USE_ORM)
#define KEYWORD_PERMUTATION_55
#elif defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_56
#elif defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_57
#elif defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_58
#elif defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_59
#elif defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_60
#elif defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_61
#elif defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_62
#else
#define KEYWORD_PERMUTATION_63
#endif


// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#define VARYINGS_NEED_NORMAL_WS
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_SHADOWCASTER


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
 float3 positionOS : POSITION;
 float3 normalOS : NORMAL;
 float4 tangentOS : TANGENT;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
 float4 positionCS : SV_POSITION;
 float3 normalWS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};
struct SurfaceDescriptionInputs
{
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 AbsoluteWorldSpacePosition;
 float3 TimeParameters;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
 float3 normalWS : INTERP0;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};

PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.positionCS = input.positionCS;
output.normalWS.xyz = input.normalWS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.positionCS = input.positionCS;
output.normalWS = input.normalWS.xyz;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}


// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)

float _EnvironmentReactionSupported;
float4 _EnvState0, _EnvState1;
float _FuelCoverage, _AllowFuelCoverage;
float4 _WetTint, _CharTint, _FireColor;
float4 _EnvironmentCellBounds;
float _UseEnvironmentCells;

float _Height_from_Ground_Randomness;
float _Height_from_Ground_Softness;
float _Normal_Pow;
float _Normal_Mul;
float _Mask_Map_Size;
float4 _Coverage_Normal_Map_TexelSize;
float _Mask_Map_Intensity;
float4 _Coverage_ORM_Map_TexelSize;
float _Height_Weight;
float _Height_Gradience;
float _Wind_Intensity;
float3 _Wind_Direction;
float _Wind_Speed;
float _Wind_Time_Random_Offset_Amount;
float _Texture_Size;
float4 _Color_Map_TexelSize;
float4 _Color_Tint;
float4 _Normal_Map_TexelSize;
float _Normal_Intensity;
float4 _ORM_Map_TexelSize;
float _Roughness_Intensity;
float _AO_Intensity;
float _Metallic_Intensity;
float4 _Emissive_Map_TexelSize;
float4 _Emissive_Color;
float4 _Coverage_Color_Map_TexelSize;
float _Coverage_Size;
float _Height_from_Ground;
float4 _Mask_Map_TexelSize;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Coverage_Normal_Map);
SAMPLER(sampler_Coverage_Normal_Map);
TEXTURE2D(_Coverage_ORM_Map);
SAMPLER(sampler_Coverage_ORM_Map);
TEXTURE2D(_Color_Map);
SAMPLER(sampler_Color_Map);
TEXTURE2D(_Normal_Map);
SAMPLER(sampler_Normal_Map);
TEXTURE2D(_ORM_Map);
SAMPLER(sampler_ORM_Map);
TEXTURE2D(_Emissive_Map);
SAMPLER(sampler_Emissive_Map);
TEXTURE2D(_Coverage_Color_Map);
SAMPLER(sampler_Coverage_Color_Map);
TEXTURE2D(_Mask_Map);
SAMPLER(sampler_Mask_Map);

// Graph Includes
// GraphIncludes: <None>

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Functions

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Sine_float(float In, out float Out)
{
    Out = sin(In);
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Length_float3(float3 In, out float Out)
{
    Out = length(In);
}

void Unity_Divide_float(float A, float B, out float Out)
{
    Out = A / B;
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
{
    Out = lerp(A, B, T);
}

struct Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float
{
float3 ObjectSpacePosition;
float3 TimeParameters;
};

void SG_WindSway_51c4027ae50a8cd49be53c4990b9f179_float(float _Sway_Wind_Speed, float3 _Sway_Wind_Direction, float _Sway_Wind_Intensity, float _Sway_Wind_Time_Random_Offset_Amount, Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float IN, out float3 OutVector3_1)
{
float _Property_5c4cd19861f64014ae8e16af9365ef49_Out_0_Float = _Sway_Wind_Time_Random_Offset_Amount;
float _Add_4b31033339dc4f8e8e71674d98e662a3_Out_2_Float;
Unity_Add_float(IN.TimeParameters.x, _Property_5c4cd19861f64014ae8e16af9365ef49_Out_0_Float, _Add_4b31033339dc4f8e8e71674d98e662a3_Out_2_Float);
float _Property_9a93fa589ac7409884a94247349a940f_Out_0_Float = _Sway_Wind_Speed;
float _Multiply_7418655003ae4fedb2bf68678629472c_Out_2_Float;
Unity_Multiply_float_float(_Add_4b31033339dc4f8e8e71674d98e662a3_Out_2_Float, _Property_9a93fa589ac7409884a94247349a940f_Out_0_Float, _Multiply_7418655003ae4fedb2bf68678629472c_Out_2_Float);
float _Sine_ebd14b0fc42b4b4cb4387f3a1b879c1f_Out_1_Float;
Unity_Sine_float(_Multiply_7418655003ae4fedb2bf68678629472c_Out_2_Float, _Sine_ebd14b0fc42b4b4cb4387f3a1b879c1f_Out_1_Float);
float _Add_660e92170c564f94a79277cd2fd0cf20_Out_2_Float;
Unity_Add_float(_Sine_ebd14b0fc42b4b4cb4387f3a1b879c1f_Out_1_Float, float(1), _Add_660e92170c564f94a79277cd2fd0cf20_Out_2_Float);
float _Multiply_b8fd654b771447aebafc1a6aad6957cf_Out_2_Float;
Unity_Multiply_float_float(_Add_660e92170c564f94a79277cd2fd0cf20_Out_2_Float, 0.5, _Multiply_b8fd654b771447aebafc1a6aad6957cf_Out_2_Float);
float3 _Property_fa2ee033b44946c59c3fa9aea4a167c8_Out_0_Vector3 = _Sway_Wind_Direction;
float3 _Multiply_a5500b7f7b8e43988a055eea7eb917f3_Out_2_Vector3;
Unity_Multiply_float3_float3((_Multiply_b8fd654b771447aebafc1a6aad6957cf_Out_2_Float.xxx), _Property_fa2ee033b44946c59c3fa9aea4a167c8_Out_0_Vector3, _Multiply_a5500b7f7b8e43988a055eea7eb917f3_Out_2_Vector3);
float _Property_66cba0a3223546238e18b58e017e756f_Out_0_Float = _Sway_Wind_Intensity;
float3 _Multiply_475f81423e39483cba045bef08b9f967_Out_2_Vector3;
Unity_Multiply_float3_float3(_Multiply_a5500b7f7b8e43988a055eea7eb917f3_Out_2_Vector3, (_Property_66cba0a3223546238e18b58e017e756f_Out_0_Float.xxx), _Multiply_475f81423e39483cba045bef08b9f967_Out_2_Vector3);
float3 _Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3;
Unity_Add_float3(_Multiply_475f81423e39483cba045bef08b9f967_Out_2_Vector3, IN.ObjectSpacePosition, _Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3);
float _Length_53965ffb38de43359ab019a247194557_Out_1_Float;
Unity_Length_float3(IN.ObjectSpacePosition, _Length_53965ffb38de43359ab019a247194557_Out_1_Float);
float _Length_77b6b3d742104f2abf3c6968dea9a342_Out_1_Float;
Unity_Length_float3(_Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3, _Length_77b6b3d742104f2abf3c6968dea9a342_Out_1_Float);
float _Divide_f5dd43de79e343638d2743ee93ce7326_Out_2_Float;
Unity_Divide_float(_Length_53965ffb38de43359ab019a247194557_Out_1_Float, _Length_77b6b3d742104f2abf3c6968dea9a342_Out_1_Float, _Divide_f5dd43de79e343638d2743ee93ce7326_Out_2_Float);
float3 _Lerp_7574daf550e1428cb9d35656c7bb9603_Out_3_Vector3;
Unity_Lerp_float3(float3(0, 0, 0), _Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3, (_Divide_f5dd43de79e343638d2743ee93ce7326_Out_2_Float.xxx), _Lerp_7574daf550e1428cb9d35656c7bb9603_Out_3_Vector3);
OutVector3_1 = _Lerp_7574daf550e1428cb9d35656c7bb9603_Out_3_Vector3;
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

void Unity_Power_float(float A, float B, out float Out)
{
    Out = pow(A, B);
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 Position;
float3 Normal;
float3 Tangent;
};

VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
float _Property_b2e048f6276d4371b6ad931f5d1baa87_Out_0_Float = _Wind_Speed;
float3 _Property_7133a0551bc7429cb8bbfcbad044bf97_Out_0_Vector3 = _Wind_Direction;
float _Property_5aa2352867cd4da79dc9f9f9979925c7_Out_0_Float = _Wind_Intensity;
float _Property_e0c6ebff99e349d6966115347961a70c_Out_0_Float = _Wind_Time_Random_Offset_Amount;
Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf;
_WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf.ObjectSpacePosition = IN.ObjectSpacePosition;
_WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf.TimeParameters = IN.TimeParameters;
float3 _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf_OutVector3_1_Vector3;
SG_WindSway_51c4027ae50a8cd49be53c4990b9f179_float(_Property_b2e048f6276d4371b6ad931f5d1baa87_Out_0_Float, _Property_7133a0551bc7429cb8bbfcbad044bf97_Out_0_Vector3, _Property_5aa2352867cd4da79dc9f9f9979925c7_Out_0_Float, _Property_e0c6ebff99e349d6966115347961a70c_Out_0_Float, _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf, _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf_OutVector3_1_Vector3);
float _Split_38420bd2034746a1878048f0ead2a7b2_R_1_Float = IN.AbsoluteWorldSpacePosition[0];
float _Split_38420bd2034746a1878048f0ead2a7b2_G_2_Float = IN.AbsoluteWorldSpacePosition[1];
float _Split_38420bd2034746a1878048f0ead2a7b2_B_3_Float = IN.AbsoluteWorldSpacePosition[2];
float _Split_38420bd2034746a1878048f0ead2a7b2_A_4_Float = 0;
float3 _Vector3_0f8475b3a0df451ea79f6ea2e4e3f673_Out_0_Vector3 = float3(float(0), float(0), float(0));
float3 _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3;
{
// Converting Position from Object to AbsoluteWorld via world space
float3 world;
world = TransformObjectToWorld(_Vector3_0f8475b3a0df451ea79f6ea2e4e3f673_Out_0_Vector3.xyz);
_Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3 = GetAbsolutePositionWS(world);
}
float _Split_fb1b2d84926449e498da6c90f1436311_R_1_Float = _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3[0];
float _Split_fb1b2d84926449e498da6c90f1436311_G_2_Float = _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3[1];
float _Split_fb1b2d84926449e498da6c90f1436311_B_3_Float = _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3[2];
float _Split_fb1b2d84926449e498da6c90f1436311_A_4_Float = 0;
float _Subtract_18ddaa9851a547f1bc3eeafa55f3227b_Out_2_Float;
Unity_Subtract_float(_Split_38420bd2034746a1878048f0ead2a7b2_G_2_Float, _Split_fb1b2d84926449e498da6c90f1436311_G_2_Float, _Subtract_18ddaa9851a547f1bc3eeafa55f3227b_Out_2_Float);
float _Property_1db6873c977b47f186e42eef2cd75a57_Out_0_Float = _Height_Weight;
float _Split_bc5987163dba418d8de90e985de1247b_R_1_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[0];
float _Split_bc5987163dba418d8de90e985de1247b_G_2_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[1];
float _Split_bc5987163dba418d8de90e985de1247b_B_3_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[2];
float _Split_bc5987163dba418d8de90e985de1247b_A_4_Float = 0;
float _Multiply_89de5a5fb3cd451abb1227deba019c38_Out_2_Float;
Unity_Multiply_float_float(_Property_1db6873c977b47f186e42eef2cd75a57_Out_0_Float, _Split_bc5987163dba418d8de90e985de1247b_G_2_Float, _Multiply_89de5a5fb3cd451abb1227deba019c38_Out_2_Float);
float _Divide_2c406de6c63e463f9013e4faf1db1ae8_Out_2_Float;
Unity_Divide_float(_Subtract_18ddaa9851a547f1bc3eeafa55f3227b_Out_2_Float, _Multiply_89de5a5fb3cd451abb1227deba019c38_Out_2_Float, _Divide_2c406de6c63e463f9013e4faf1db1ae8_Out_2_Float);
float _Saturate_e9ea645ca89f482a9d0315df54a77ee5_Out_1_Float;
Unity_Saturate_float(_Divide_2c406de6c63e463f9013e4faf1db1ae8_Out_2_Float, _Saturate_e9ea645ca89f482a9d0315df54a77ee5_Out_1_Float);
float _Property_e96fd1920dff4533951da684129df200_Out_0_Float = _Height_Gradience;
float _Power_b88c5abc85424f17b03c60c98e9bc16c_Out_2_Float;
Unity_Power_float(_Saturate_e9ea645ca89f482a9d0315df54a77ee5_Out_1_Float, _Property_e96fd1920dff4533951da684129df200_Out_0_Float, _Power_b88c5abc85424f17b03c60c98e9bc16c_Out_2_Float);
float _Saturate_6c35d0fa108b483783bde88e06ff5ce0_Out_1_Float;
Unity_Saturate_float(_Power_b88c5abc85424f17b03c60c98e9bc16c_Out_2_Float, _Saturate_6c35d0fa108b483783bde88e06ff5ce0_Out_1_Float);
float3 _Lerp_8461a67d418941ee81ed9eb0f80e8a38_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf_OutVector3_1_Vector3, (_Saturate_6c35d0fa108b483783bde88e06ff5ce0_Out_1_Float.xxx), _Lerp_8461a67d418941ee81ed9eb0f80e8a38_Out_3_Vector3);
#if defined(_USE_WPO)
float3 _UseWPO_81cb92bff33547fbbfaa5ce2f60ba97c_Out_0_Vector3 = _Lerp_8461a67d418941ee81ed9eb0f80e8a38_Out_3_Vector3;
#else
float3 _UseWPO_81cb92bff33547fbbfaa5ce2f60ba97c_Out_0_Vector3 = IN.ObjectSpacePosition;
#endif
description.Position = _UseWPO_81cb92bff33547fbbfaa5ce2f60ba97c_Out_0_Vector3;
description.Normal = IN.ObjectSpaceNormal;
description.Tangent = IN.ObjectSpaceTangent;
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
float3 reactionUnusedColor = 0, reactionUnusedEmission = 0;
ClipVegetationFuel(TransformObjectToWorld(float3(0,0,0)));
return surface;
}

// --------------------------------------------------
// Build Graph Inputs
#ifdef HAVE_VFX_MODIFICATION
#define VFX_SRP_ATTRIBUTES Attributes
#define VFX_SRP_VARYINGS Varyings
#define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
#endif
VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

    output.ObjectSpaceNormal =                          input.normalOS;
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
    output.AbsoluteWorldSpacePosition =                 GetAbsolutePositionWS(TransformObjectToWorld(input.positionOS));
    output.TimeParameters =                             _TimeParameters.xyz;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

#ifdef HAVE_VFX_MODIFICATION
#if VFX_USE_GRAPH_VALUES
    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
#endif
    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */

#endif

    






    #if UNITY_UV_STARTS_AT_TOP
    #else
    #endif


#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

// --------------------------------------------------
// Main

#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShadowCasterPass.hlsl"

// --------------------------------------------------
// Visual Effect Vertex Invocations
#ifdef HAVE_VFX_MODIFICATION
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
#endif

ENDHLSL
}
Pass
{
    Name "MotionVectors"
    Tags
    {
        "LightMode" = "MotionVectors"
    }

// Render State
Cull Back
ZTest LEqual
ZWrite On
ColorMask RG

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 3.5
#pragma multi_compile_instancing
#pragma vertex vert
#pragma fragment frag

// Keywords
// PassKeywords: <None>
#pragma shader_feature_local _ _USE_EMISSIVE
#pragma shader_feature_local _ _USE_WPO
#pragma shader_feature_local _ _USE_ORM
#pragma shader_feature_local _ _USE_NORMAL_MAPS
#pragma shader_feature_local _ _USE_COVERAGE
#pragma shader_feature_local _ _USE_COVERAGE_MASK_TEXTURE

#if defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_2
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_3
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_4
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_5
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_6
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM)
#define KEYWORD_PERMUTATION_7
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_8
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_9
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_10
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_11
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_12
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_13
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_14
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO)
#define KEYWORD_PERMUTATION_15
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_16
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_17
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_18
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_19
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_20
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_21
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_22
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM)
#define KEYWORD_PERMUTATION_23
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_24
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_25
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_26
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_27
#elif defined(_USE_EMISSIVE) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_28
#elif defined(_USE_EMISSIVE) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_29
#elif defined(_USE_EMISSIVE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_30
#elif defined(_USE_EMISSIVE)
#define KEYWORD_PERMUTATION_31
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_32
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_33
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_34
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_35
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_36
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_37
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_38
#elif defined(_USE_WPO) && defined(_USE_ORM)
#define KEYWORD_PERMUTATION_39
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_40
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_41
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_42
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_43
#elif defined(_USE_WPO) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_44
#elif defined(_USE_WPO) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_45
#elif defined(_USE_WPO) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_46
#elif defined(_USE_WPO)
#define KEYWORD_PERMUTATION_47
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_48
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_49
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_50
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_51
#elif defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_52
#elif defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_53
#elif defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_54
#elif defined(_USE_ORM)
#define KEYWORD_PERMUTATION_55
#elif defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_56
#elif defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_57
#elif defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_58
#elif defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_59
#elif defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_60
#elif defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_61
#elif defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_62
#else
#define KEYWORD_PERMUTATION_63
#endif


// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_MOTION_VECTORS


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
 float3 positionOS : POSITION;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
 float4 positionCS : SV_POSITION;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};
struct SurfaceDescriptionInputs
{
};
struct VertexDescriptionInputs
{
 float3 ObjectSpacePosition;
 float3 AbsoluteWorldSpacePosition;
 float3 TimeParameters;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};

PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.positionCS = input.positionCS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.positionCS = input.positionCS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}


// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)

float _EnvironmentReactionSupported;
float4 _EnvState0, _EnvState1;
float _FuelCoverage, _AllowFuelCoverage;
float4 _WetTint, _CharTint, _FireColor;
float4 _EnvironmentCellBounds;
float _UseEnvironmentCells;

float _Height_from_Ground_Randomness;
float _Height_from_Ground_Softness;
float _Normal_Pow;
float _Normal_Mul;
float _Mask_Map_Size;
float4 _Coverage_Normal_Map_TexelSize;
float _Mask_Map_Intensity;
float4 _Coverage_ORM_Map_TexelSize;
float _Height_Weight;
float _Height_Gradience;
float _Wind_Intensity;
float3 _Wind_Direction;
float _Wind_Speed;
float _Wind_Time_Random_Offset_Amount;
float _Texture_Size;
float4 _Color_Map_TexelSize;
float4 _Color_Tint;
float4 _Normal_Map_TexelSize;
float _Normal_Intensity;
float4 _ORM_Map_TexelSize;
float _Roughness_Intensity;
float _AO_Intensity;
float _Metallic_Intensity;
float4 _Emissive_Map_TexelSize;
float4 _Emissive_Color;
float4 _Coverage_Color_Map_TexelSize;
float _Coverage_Size;
float _Height_from_Ground;
float4 _Mask_Map_TexelSize;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Coverage_Normal_Map);
SAMPLER(sampler_Coverage_Normal_Map);
TEXTURE2D(_Coverage_ORM_Map);
SAMPLER(sampler_Coverage_ORM_Map);
TEXTURE2D(_Color_Map);
SAMPLER(sampler_Color_Map);
TEXTURE2D(_Normal_Map);
SAMPLER(sampler_Normal_Map);
TEXTURE2D(_ORM_Map);
SAMPLER(sampler_ORM_Map);
TEXTURE2D(_Emissive_Map);
SAMPLER(sampler_Emissive_Map);
TEXTURE2D(_Coverage_Color_Map);
SAMPLER(sampler_Coverage_Color_Map);
TEXTURE2D(_Mask_Map);
SAMPLER(sampler_Mask_Map);

// Graph Includes
// GraphIncludes: <None>

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Functions

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Sine_float(float In, out float Out)
{
    Out = sin(In);
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Length_float3(float3 In, out float Out)
{
    Out = length(In);
}

void Unity_Divide_float(float A, float B, out float Out)
{
    Out = A / B;
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
{
    Out = lerp(A, B, T);
}

struct Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float
{
float3 ObjectSpacePosition;
float3 TimeParameters;
};

void SG_WindSway_51c4027ae50a8cd49be53c4990b9f179_float(float _Sway_Wind_Speed, float3 _Sway_Wind_Direction, float _Sway_Wind_Intensity, float _Sway_Wind_Time_Random_Offset_Amount, Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float IN, out float3 OutVector3_1)
{
float _Property_5c4cd19861f64014ae8e16af9365ef49_Out_0_Float = _Sway_Wind_Time_Random_Offset_Amount;
float _Add_4b31033339dc4f8e8e71674d98e662a3_Out_2_Float;
Unity_Add_float(IN.TimeParameters.x, _Property_5c4cd19861f64014ae8e16af9365ef49_Out_0_Float, _Add_4b31033339dc4f8e8e71674d98e662a3_Out_2_Float);
float _Property_9a93fa589ac7409884a94247349a940f_Out_0_Float = _Sway_Wind_Speed;
float _Multiply_7418655003ae4fedb2bf68678629472c_Out_2_Float;
Unity_Multiply_float_float(_Add_4b31033339dc4f8e8e71674d98e662a3_Out_2_Float, _Property_9a93fa589ac7409884a94247349a940f_Out_0_Float, _Multiply_7418655003ae4fedb2bf68678629472c_Out_2_Float);
float _Sine_ebd14b0fc42b4b4cb4387f3a1b879c1f_Out_1_Float;
Unity_Sine_float(_Multiply_7418655003ae4fedb2bf68678629472c_Out_2_Float, _Sine_ebd14b0fc42b4b4cb4387f3a1b879c1f_Out_1_Float);
float _Add_660e92170c564f94a79277cd2fd0cf20_Out_2_Float;
Unity_Add_float(_Sine_ebd14b0fc42b4b4cb4387f3a1b879c1f_Out_1_Float, float(1), _Add_660e92170c564f94a79277cd2fd0cf20_Out_2_Float);
float _Multiply_b8fd654b771447aebafc1a6aad6957cf_Out_2_Float;
Unity_Multiply_float_float(_Add_660e92170c564f94a79277cd2fd0cf20_Out_2_Float, 0.5, _Multiply_b8fd654b771447aebafc1a6aad6957cf_Out_2_Float);
float3 _Property_fa2ee033b44946c59c3fa9aea4a167c8_Out_0_Vector3 = _Sway_Wind_Direction;
float3 _Multiply_a5500b7f7b8e43988a055eea7eb917f3_Out_2_Vector3;
Unity_Multiply_float3_float3((_Multiply_b8fd654b771447aebafc1a6aad6957cf_Out_2_Float.xxx), _Property_fa2ee033b44946c59c3fa9aea4a167c8_Out_0_Vector3, _Multiply_a5500b7f7b8e43988a055eea7eb917f3_Out_2_Vector3);
float _Property_66cba0a3223546238e18b58e017e756f_Out_0_Float = _Sway_Wind_Intensity;
float3 _Multiply_475f81423e39483cba045bef08b9f967_Out_2_Vector3;
Unity_Multiply_float3_float3(_Multiply_a5500b7f7b8e43988a055eea7eb917f3_Out_2_Vector3, (_Property_66cba0a3223546238e18b58e017e756f_Out_0_Float.xxx), _Multiply_475f81423e39483cba045bef08b9f967_Out_2_Vector3);
float3 _Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3;
Unity_Add_float3(_Multiply_475f81423e39483cba045bef08b9f967_Out_2_Vector3, IN.ObjectSpacePosition, _Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3);
float _Length_53965ffb38de43359ab019a247194557_Out_1_Float;
Unity_Length_float3(IN.ObjectSpacePosition, _Length_53965ffb38de43359ab019a247194557_Out_1_Float);
float _Length_77b6b3d742104f2abf3c6968dea9a342_Out_1_Float;
Unity_Length_float3(_Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3, _Length_77b6b3d742104f2abf3c6968dea9a342_Out_1_Float);
float _Divide_f5dd43de79e343638d2743ee93ce7326_Out_2_Float;
Unity_Divide_float(_Length_53965ffb38de43359ab019a247194557_Out_1_Float, _Length_77b6b3d742104f2abf3c6968dea9a342_Out_1_Float, _Divide_f5dd43de79e343638d2743ee93ce7326_Out_2_Float);
float3 _Lerp_7574daf550e1428cb9d35656c7bb9603_Out_3_Vector3;
Unity_Lerp_float3(float3(0, 0, 0), _Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3, (_Divide_f5dd43de79e343638d2743ee93ce7326_Out_2_Float.xxx), _Lerp_7574daf550e1428cb9d35656c7bb9603_Out_3_Vector3);
OutVector3_1 = _Lerp_7574daf550e1428cb9d35656c7bb9603_Out_3_Vector3;
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

void Unity_Power_float(float A, float B, out float Out)
{
    Out = pow(A, B);
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 Position;
};

VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
float _Property_b2e048f6276d4371b6ad931f5d1baa87_Out_0_Float = _Wind_Speed;
float3 _Property_7133a0551bc7429cb8bbfcbad044bf97_Out_0_Vector3 = _Wind_Direction;
float _Property_5aa2352867cd4da79dc9f9f9979925c7_Out_0_Float = _Wind_Intensity;
float _Property_e0c6ebff99e349d6966115347961a70c_Out_0_Float = _Wind_Time_Random_Offset_Amount;
Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf;
_WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf.ObjectSpacePosition = IN.ObjectSpacePosition;
_WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf.TimeParameters = IN.TimeParameters;
float3 _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf_OutVector3_1_Vector3;
SG_WindSway_51c4027ae50a8cd49be53c4990b9f179_float(_Property_b2e048f6276d4371b6ad931f5d1baa87_Out_0_Float, _Property_7133a0551bc7429cb8bbfcbad044bf97_Out_0_Vector3, _Property_5aa2352867cd4da79dc9f9f9979925c7_Out_0_Float, _Property_e0c6ebff99e349d6966115347961a70c_Out_0_Float, _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf, _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf_OutVector3_1_Vector3);
float _Split_38420bd2034746a1878048f0ead2a7b2_R_1_Float = IN.AbsoluteWorldSpacePosition[0];
float _Split_38420bd2034746a1878048f0ead2a7b2_G_2_Float = IN.AbsoluteWorldSpacePosition[1];
float _Split_38420bd2034746a1878048f0ead2a7b2_B_3_Float = IN.AbsoluteWorldSpacePosition[2];
float _Split_38420bd2034746a1878048f0ead2a7b2_A_4_Float = 0;
float3 _Vector3_0f8475b3a0df451ea79f6ea2e4e3f673_Out_0_Vector3 = float3(float(0), float(0), float(0));
float3 _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3;
{
// Converting Position from Object to AbsoluteWorld via world space
float3 world;
world = TransformObjectToWorld(_Vector3_0f8475b3a0df451ea79f6ea2e4e3f673_Out_0_Vector3.xyz);
_Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3 = GetAbsolutePositionWS(world);
}
float _Split_fb1b2d84926449e498da6c90f1436311_R_1_Float = _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3[0];
float _Split_fb1b2d84926449e498da6c90f1436311_G_2_Float = _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3[1];
float _Split_fb1b2d84926449e498da6c90f1436311_B_3_Float = _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3[2];
float _Split_fb1b2d84926449e498da6c90f1436311_A_4_Float = 0;
float _Subtract_18ddaa9851a547f1bc3eeafa55f3227b_Out_2_Float;
Unity_Subtract_float(_Split_38420bd2034746a1878048f0ead2a7b2_G_2_Float, _Split_fb1b2d84926449e498da6c90f1436311_G_2_Float, _Subtract_18ddaa9851a547f1bc3eeafa55f3227b_Out_2_Float);
float _Property_1db6873c977b47f186e42eef2cd75a57_Out_0_Float = _Height_Weight;
float _Split_bc5987163dba418d8de90e985de1247b_R_1_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[0];
float _Split_bc5987163dba418d8de90e985de1247b_G_2_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[1];
float _Split_bc5987163dba418d8de90e985de1247b_B_3_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[2];
float _Split_bc5987163dba418d8de90e985de1247b_A_4_Float = 0;
float _Multiply_89de5a5fb3cd451abb1227deba019c38_Out_2_Float;
Unity_Multiply_float_float(_Property_1db6873c977b47f186e42eef2cd75a57_Out_0_Float, _Split_bc5987163dba418d8de90e985de1247b_G_2_Float, _Multiply_89de5a5fb3cd451abb1227deba019c38_Out_2_Float);
float _Divide_2c406de6c63e463f9013e4faf1db1ae8_Out_2_Float;
Unity_Divide_float(_Subtract_18ddaa9851a547f1bc3eeafa55f3227b_Out_2_Float, _Multiply_89de5a5fb3cd451abb1227deba019c38_Out_2_Float, _Divide_2c406de6c63e463f9013e4faf1db1ae8_Out_2_Float);
float _Saturate_e9ea645ca89f482a9d0315df54a77ee5_Out_1_Float;
Unity_Saturate_float(_Divide_2c406de6c63e463f9013e4faf1db1ae8_Out_2_Float, _Saturate_e9ea645ca89f482a9d0315df54a77ee5_Out_1_Float);
float _Property_e96fd1920dff4533951da684129df200_Out_0_Float = _Height_Gradience;
float _Power_b88c5abc85424f17b03c60c98e9bc16c_Out_2_Float;
Unity_Power_float(_Saturate_e9ea645ca89f482a9d0315df54a77ee5_Out_1_Float, _Property_e96fd1920dff4533951da684129df200_Out_0_Float, _Power_b88c5abc85424f17b03c60c98e9bc16c_Out_2_Float);
float _Saturate_6c35d0fa108b483783bde88e06ff5ce0_Out_1_Float;
Unity_Saturate_float(_Power_b88c5abc85424f17b03c60c98e9bc16c_Out_2_Float, _Saturate_6c35d0fa108b483783bde88e06ff5ce0_Out_1_Float);
float3 _Lerp_8461a67d418941ee81ed9eb0f80e8a38_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf_OutVector3_1_Vector3, (_Saturate_6c35d0fa108b483783bde88e06ff5ce0_Out_1_Float.xxx), _Lerp_8461a67d418941ee81ed9eb0f80e8a38_Out_3_Vector3);
#if defined(_USE_WPO)
float3 _UseWPO_81cb92bff33547fbbfaa5ce2f60ba97c_Out_0_Vector3 = _Lerp_8461a67d418941ee81ed9eb0f80e8a38_Out_3_Vector3;
#else
float3 _UseWPO_81cb92bff33547fbbfaa5ce2f60ba97c_Out_0_Vector3 = IN.ObjectSpacePosition;
#endif
description.Position = _UseWPO_81cb92bff33547fbbfaa5ce2f60ba97c_Out_0_Vector3;
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
float3 reactionUnusedColor = 0, reactionUnusedEmission = 0;
ClipVegetationFuel(TransformObjectToWorld(float3(0,0,0)));
return surface;
}

// --------------------------------------------------
// Build Graph Inputs
#ifdef HAVE_VFX_MODIFICATION
#define VFX_SRP_ATTRIBUTES Attributes
#define VFX_SRP_VARYINGS Varyings
#define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
#endif
VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

    output.ObjectSpacePosition =                        input.positionOS;
    output.AbsoluteWorldSpacePosition =                 GetAbsolutePositionWS(TransformObjectToWorld(input.positionOS));
    output.TimeParameters =                             _TimeParameters.xyz;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

#ifdef HAVE_VFX_MODIFICATION
#if VFX_USE_GRAPH_VALUES
    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
#endif
    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */

#endif

    






    #if UNITY_UV_STARTS_AT_TOP
    #else
    #endif


#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

// --------------------------------------------------
// Main

#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/MotionVectorPass.hlsl"

// --------------------------------------------------
// Visual Effect Vertex Invocations
#ifdef HAVE_VFX_MODIFICATION
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
#endif

ENDHLSL
}
Pass
{
    Name "DepthOnly"
    Tags
    {
        "LightMode" = "DepthOnly"
    }

// Render State
Cull Back
ZTest LEqual
ZWrite On
ColorMask R

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 2.0
#pragma multi_compile_instancing
#pragma vertex vert
#pragma fragment frag

// Keywords
// PassKeywords: <None>
#pragma shader_feature_local _ _USE_EMISSIVE
#pragma shader_feature_local _ _USE_WPO
#pragma shader_feature_local _ _USE_ORM
#pragma shader_feature_local _ _USE_NORMAL_MAPS
#pragma shader_feature_local _ _USE_COVERAGE
#pragma shader_feature_local _ _USE_COVERAGE_MASK_TEXTURE

#if defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_2
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_3
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_4
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_5
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_6
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM)
#define KEYWORD_PERMUTATION_7
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_8
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_9
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_10
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_11
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_12
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_13
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_14
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO)
#define KEYWORD_PERMUTATION_15
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_16
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_17
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_18
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_19
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_20
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_21
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_22
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM)
#define KEYWORD_PERMUTATION_23
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_24
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_25
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_26
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_27
#elif defined(_USE_EMISSIVE) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_28
#elif defined(_USE_EMISSIVE) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_29
#elif defined(_USE_EMISSIVE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_30
#elif defined(_USE_EMISSIVE)
#define KEYWORD_PERMUTATION_31
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_32
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_33
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_34
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_35
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_36
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_37
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_38
#elif defined(_USE_WPO) && defined(_USE_ORM)
#define KEYWORD_PERMUTATION_39
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_40
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_41
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_42
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_43
#elif defined(_USE_WPO) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_44
#elif defined(_USE_WPO) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_45
#elif defined(_USE_WPO) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_46
#elif defined(_USE_WPO)
#define KEYWORD_PERMUTATION_47
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_48
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_49
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_50
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_51
#elif defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_52
#elif defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_53
#elif defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_54
#elif defined(_USE_ORM)
#define KEYWORD_PERMUTATION_55
#elif defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_56
#elif defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_57
#elif defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_58
#elif defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_59
#elif defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_60
#elif defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_61
#elif defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_62
#else
#define KEYWORD_PERMUTATION_63
#endif


// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_DEPTHONLY


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
 float3 positionOS : POSITION;
 float3 normalOS : NORMAL;
 float4 tangentOS : TANGENT;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
 float4 positionCS : SV_POSITION;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};
struct SurfaceDescriptionInputs
{
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 AbsoluteWorldSpacePosition;
 float3 TimeParameters;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};

PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.positionCS = input.positionCS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.positionCS = input.positionCS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}


// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)

float _EnvironmentReactionSupported;
float4 _EnvState0, _EnvState1;
float _FuelCoverage, _AllowFuelCoverage;
float4 _WetTint, _CharTint, _FireColor;
float4 _EnvironmentCellBounds;
float _UseEnvironmentCells;

float _Height_from_Ground_Randomness;
float _Height_from_Ground_Softness;
float _Normal_Pow;
float _Normal_Mul;
float _Mask_Map_Size;
float4 _Coverage_Normal_Map_TexelSize;
float _Mask_Map_Intensity;
float4 _Coverage_ORM_Map_TexelSize;
float _Height_Weight;
float _Height_Gradience;
float _Wind_Intensity;
float3 _Wind_Direction;
float _Wind_Speed;
float _Wind_Time_Random_Offset_Amount;
float _Texture_Size;
float4 _Color_Map_TexelSize;
float4 _Color_Tint;
float4 _Normal_Map_TexelSize;
float _Normal_Intensity;
float4 _ORM_Map_TexelSize;
float _Roughness_Intensity;
float _AO_Intensity;
float _Metallic_Intensity;
float4 _Emissive_Map_TexelSize;
float4 _Emissive_Color;
float4 _Coverage_Color_Map_TexelSize;
float _Coverage_Size;
float _Height_from_Ground;
float4 _Mask_Map_TexelSize;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Coverage_Normal_Map);
SAMPLER(sampler_Coverage_Normal_Map);
TEXTURE2D(_Coverage_ORM_Map);
SAMPLER(sampler_Coverage_ORM_Map);
TEXTURE2D(_Color_Map);
SAMPLER(sampler_Color_Map);
TEXTURE2D(_Normal_Map);
SAMPLER(sampler_Normal_Map);
TEXTURE2D(_ORM_Map);
SAMPLER(sampler_ORM_Map);
TEXTURE2D(_Emissive_Map);
SAMPLER(sampler_Emissive_Map);
TEXTURE2D(_Coverage_Color_Map);
SAMPLER(sampler_Coverage_Color_Map);
TEXTURE2D(_Mask_Map);
SAMPLER(sampler_Mask_Map);

// Graph Includes
// GraphIncludes: <None>

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Functions

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Sine_float(float In, out float Out)
{
    Out = sin(In);
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Length_float3(float3 In, out float Out)
{
    Out = length(In);
}

void Unity_Divide_float(float A, float B, out float Out)
{
    Out = A / B;
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
{
    Out = lerp(A, B, T);
}

struct Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float
{
float3 ObjectSpacePosition;
float3 TimeParameters;
};

void SG_WindSway_51c4027ae50a8cd49be53c4990b9f179_float(float _Sway_Wind_Speed, float3 _Sway_Wind_Direction, float _Sway_Wind_Intensity, float _Sway_Wind_Time_Random_Offset_Amount, Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float IN, out float3 OutVector3_1)
{
float _Property_5c4cd19861f64014ae8e16af9365ef49_Out_0_Float = _Sway_Wind_Time_Random_Offset_Amount;
float _Add_4b31033339dc4f8e8e71674d98e662a3_Out_2_Float;
Unity_Add_float(IN.TimeParameters.x, _Property_5c4cd19861f64014ae8e16af9365ef49_Out_0_Float, _Add_4b31033339dc4f8e8e71674d98e662a3_Out_2_Float);
float _Property_9a93fa589ac7409884a94247349a940f_Out_0_Float = _Sway_Wind_Speed;
float _Multiply_7418655003ae4fedb2bf68678629472c_Out_2_Float;
Unity_Multiply_float_float(_Add_4b31033339dc4f8e8e71674d98e662a3_Out_2_Float, _Property_9a93fa589ac7409884a94247349a940f_Out_0_Float, _Multiply_7418655003ae4fedb2bf68678629472c_Out_2_Float);
float _Sine_ebd14b0fc42b4b4cb4387f3a1b879c1f_Out_1_Float;
Unity_Sine_float(_Multiply_7418655003ae4fedb2bf68678629472c_Out_2_Float, _Sine_ebd14b0fc42b4b4cb4387f3a1b879c1f_Out_1_Float);
float _Add_660e92170c564f94a79277cd2fd0cf20_Out_2_Float;
Unity_Add_float(_Sine_ebd14b0fc42b4b4cb4387f3a1b879c1f_Out_1_Float, float(1), _Add_660e92170c564f94a79277cd2fd0cf20_Out_2_Float);
float _Multiply_b8fd654b771447aebafc1a6aad6957cf_Out_2_Float;
Unity_Multiply_float_float(_Add_660e92170c564f94a79277cd2fd0cf20_Out_2_Float, 0.5, _Multiply_b8fd654b771447aebafc1a6aad6957cf_Out_2_Float);
float3 _Property_fa2ee033b44946c59c3fa9aea4a167c8_Out_0_Vector3 = _Sway_Wind_Direction;
float3 _Multiply_a5500b7f7b8e43988a055eea7eb917f3_Out_2_Vector3;
Unity_Multiply_float3_float3((_Multiply_b8fd654b771447aebafc1a6aad6957cf_Out_2_Float.xxx), _Property_fa2ee033b44946c59c3fa9aea4a167c8_Out_0_Vector3, _Multiply_a5500b7f7b8e43988a055eea7eb917f3_Out_2_Vector3);
float _Property_66cba0a3223546238e18b58e017e756f_Out_0_Float = _Sway_Wind_Intensity;
float3 _Multiply_475f81423e39483cba045bef08b9f967_Out_2_Vector3;
Unity_Multiply_float3_float3(_Multiply_a5500b7f7b8e43988a055eea7eb917f3_Out_2_Vector3, (_Property_66cba0a3223546238e18b58e017e756f_Out_0_Float.xxx), _Multiply_475f81423e39483cba045bef08b9f967_Out_2_Vector3);
float3 _Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3;
Unity_Add_float3(_Multiply_475f81423e39483cba045bef08b9f967_Out_2_Vector3, IN.ObjectSpacePosition, _Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3);
float _Length_53965ffb38de43359ab019a247194557_Out_1_Float;
Unity_Length_float3(IN.ObjectSpacePosition, _Length_53965ffb38de43359ab019a247194557_Out_1_Float);
float _Length_77b6b3d742104f2abf3c6968dea9a342_Out_1_Float;
Unity_Length_float3(_Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3, _Length_77b6b3d742104f2abf3c6968dea9a342_Out_1_Float);
float _Divide_f5dd43de79e343638d2743ee93ce7326_Out_2_Float;
Unity_Divide_float(_Length_53965ffb38de43359ab019a247194557_Out_1_Float, _Length_77b6b3d742104f2abf3c6968dea9a342_Out_1_Float, _Divide_f5dd43de79e343638d2743ee93ce7326_Out_2_Float);
float3 _Lerp_7574daf550e1428cb9d35656c7bb9603_Out_3_Vector3;
Unity_Lerp_float3(float3(0, 0, 0), _Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3, (_Divide_f5dd43de79e343638d2743ee93ce7326_Out_2_Float.xxx), _Lerp_7574daf550e1428cb9d35656c7bb9603_Out_3_Vector3);
OutVector3_1 = _Lerp_7574daf550e1428cb9d35656c7bb9603_Out_3_Vector3;
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

void Unity_Power_float(float A, float B, out float Out)
{
    Out = pow(A, B);
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 Position;
float3 Normal;
float3 Tangent;
};

VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
float _Property_b2e048f6276d4371b6ad931f5d1baa87_Out_0_Float = _Wind_Speed;
float3 _Property_7133a0551bc7429cb8bbfcbad044bf97_Out_0_Vector3 = _Wind_Direction;
float _Property_5aa2352867cd4da79dc9f9f9979925c7_Out_0_Float = _Wind_Intensity;
float _Property_e0c6ebff99e349d6966115347961a70c_Out_0_Float = _Wind_Time_Random_Offset_Amount;
Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf;
_WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf.ObjectSpacePosition = IN.ObjectSpacePosition;
_WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf.TimeParameters = IN.TimeParameters;
float3 _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf_OutVector3_1_Vector3;
SG_WindSway_51c4027ae50a8cd49be53c4990b9f179_float(_Property_b2e048f6276d4371b6ad931f5d1baa87_Out_0_Float, _Property_7133a0551bc7429cb8bbfcbad044bf97_Out_0_Vector3, _Property_5aa2352867cd4da79dc9f9f9979925c7_Out_0_Float, _Property_e0c6ebff99e349d6966115347961a70c_Out_0_Float, _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf, _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf_OutVector3_1_Vector3);
float _Split_38420bd2034746a1878048f0ead2a7b2_R_1_Float = IN.AbsoluteWorldSpacePosition[0];
float _Split_38420bd2034746a1878048f0ead2a7b2_G_2_Float = IN.AbsoluteWorldSpacePosition[1];
float _Split_38420bd2034746a1878048f0ead2a7b2_B_3_Float = IN.AbsoluteWorldSpacePosition[2];
float _Split_38420bd2034746a1878048f0ead2a7b2_A_4_Float = 0;
float3 _Vector3_0f8475b3a0df451ea79f6ea2e4e3f673_Out_0_Vector3 = float3(float(0), float(0), float(0));
float3 _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3;
{
// Converting Position from Object to AbsoluteWorld via world space
float3 world;
world = TransformObjectToWorld(_Vector3_0f8475b3a0df451ea79f6ea2e4e3f673_Out_0_Vector3.xyz);
_Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3 = GetAbsolutePositionWS(world);
}
float _Split_fb1b2d84926449e498da6c90f1436311_R_1_Float = _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3[0];
float _Split_fb1b2d84926449e498da6c90f1436311_G_2_Float = _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3[1];
float _Split_fb1b2d84926449e498da6c90f1436311_B_3_Float = _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3[2];
float _Split_fb1b2d84926449e498da6c90f1436311_A_4_Float = 0;
float _Subtract_18ddaa9851a547f1bc3eeafa55f3227b_Out_2_Float;
Unity_Subtract_float(_Split_38420bd2034746a1878048f0ead2a7b2_G_2_Float, _Split_fb1b2d84926449e498da6c90f1436311_G_2_Float, _Subtract_18ddaa9851a547f1bc3eeafa55f3227b_Out_2_Float);
float _Property_1db6873c977b47f186e42eef2cd75a57_Out_0_Float = _Height_Weight;
float _Split_bc5987163dba418d8de90e985de1247b_R_1_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[0];
float _Split_bc5987163dba418d8de90e985de1247b_G_2_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[1];
float _Split_bc5987163dba418d8de90e985de1247b_B_3_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[2];
float _Split_bc5987163dba418d8de90e985de1247b_A_4_Float = 0;
float _Multiply_89de5a5fb3cd451abb1227deba019c38_Out_2_Float;
Unity_Multiply_float_float(_Property_1db6873c977b47f186e42eef2cd75a57_Out_0_Float, _Split_bc5987163dba418d8de90e985de1247b_G_2_Float, _Multiply_89de5a5fb3cd451abb1227deba019c38_Out_2_Float);
float _Divide_2c406de6c63e463f9013e4faf1db1ae8_Out_2_Float;
Unity_Divide_float(_Subtract_18ddaa9851a547f1bc3eeafa55f3227b_Out_2_Float, _Multiply_89de5a5fb3cd451abb1227deba019c38_Out_2_Float, _Divide_2c406de6c63e463f9013e4faf1db1ae8_Out_2_Float);
float _Saturate_e9ea645ca89f482a9d0315df54a77ee5_Out_1_Float;
Unity_Saturate_float(_Divide_2c406de6c63e463f9013e4faf1db1ae8_Out_2_Float, _Saturate_e9ea645ca89f482a9d0315df54a77ee5_Out_1_Float);
float _Property_e96fd1920dff4533951da684129df200_Out_0_Float = _Height_Gradience;
float _Power_b88c5abc85424f17b03c60c98e9bc16c_Out_2_Float;
Unity_Power_float(_Saturate_e9ea645ca89f482a9d0315df54a77ee5_Out_1_Float, _Property_e96fd1920dff4533951da684129df200_Out_0_Float, _Power_b88c5abc85424f17b03c60c98e9bc16c_Out_2_Float);
float _Saturate_6c35d0fa108b483783bde88e06ff5ce0_Out_1_Float;
Unity_Saturate_float(_Power_b88c5abc85424f17b03c60c98e9bc16c_Out_2_Float, _Saturate_6c35d0fa108b483783bde88e06ff5ce0_Out_1_Float);
float3 _Lerp_8461a67d418941ee81ed9eb0f80e8a38_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf_OutVector3_1_Vector3, (_Saturate_6c35d0fa108b483783bde88e06ff5ce0_Out_1_Float.xxx), _Lerp_8461a67d418941ee81ed9eb0f80e8a38_Out_3_Vector3);
#if defined(_USE_WPO)
float3 _UseWPO_81cb92bff33547fbbfaa5ce2f60ba97c_Out_0_Vector3 = _Lerp_8461a67d418941ee81ed9eb0f80e8a38_Out_3_Vector3;
#else
float3 _UseWPO_81cb92bff33547fbbfaa5ce2f60ba97c_Out_0_Vector3 = IN.ObjectSpacePosition;
#endif
description.Position = _UseWPO_81cb92bff33547fbbfaa5ce2f60ba97c_Out_0_Vector3;
description.Normal = IN.ObjectSpaceNormal;
description.Tangent = IN.ObjectSpaceTangent;
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
float3 reactionUnusedColor = 0, reactionUnusedEmission = 0;
ClipVegetationFuel(TransformObjectToWorld(float3(0,0,0)));
return surface;
}

// --------------------------------------------------
// Build Graph Inputs
#ifdef HAVE_VFX_MODIFICATION
#define VFX_SRP_ATTRIBUTES Attributes
#define VFX_SRP_VARYINGS Varyings
#define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
#endif
VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

    output.ObjectSpaceNormal =                          input.normalOS;
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
    output.AbsoluteWorldSpacePosition =                 GetAbsolutePositionWS(TransformObjectToWorld(input.positionOS));
    output.TimeParameters =                             _TimeParameters.xyz;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

#ifdef HAVE_VFX_MODIFICATION
#if VFX_USE_GRAPH_VALUES
    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
#endif
    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */

#endif

    






    #if UNITY_UV_STARTS_AT_TOP
    #else
    #endif


#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

// --------------------------------------------------
// Main

#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthOnlyPass.hlsl"

// --------------------------------------------------
// Visual Effect Vertex Invocations
#ifdef HAVE_VFX_MODIFICATION
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
#endif

ENDHLSL
}
Pass
{
    Name "DepthNormals"
    Tags
    {
        "LightMode" = "DepthNormals"
    }

// Render State
Cull Back
ZTest LEqual
ZWrite On

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 2.0
#pragma multi_compile_instancing
#pragma vertex vert
#pragma fragment frag

// Keywords
// PassKeywords: <None>
#pragma shader_feature_local _ _USE_EMISSIVE
#pragma shader_feature_local _ _USE_WPO
#pragma shader_feature_local _ _USE_ORM
#pragma shader_feature_local _ _USE_NORMAL_MAPS
#pragma shader_feature_local _ _USE_COVERAGE
#pragma shader_feature_local _ _USE_COVERAGE_MASK_TEXTURE

#if defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_2
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_3
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_4
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_5
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_6
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM)
#define KEYWORD_PERMUTATION_7
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_8
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_9
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_10
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_11
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_12
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_13
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_14
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO)
#define KEYWORD_PERMUTATION_15
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_16
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_17
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_18
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_19
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_20
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_21
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_22
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM)
#define KEYWORD_PERMUTATION_23
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_24
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_25
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_26
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_27
#elif defined(_USE_EMISSIVE) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_28
#elif defined(_USE_EMISSIVE) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_29
#elif defined(_USE_EMISSIVE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_30
#elif defined(_USE_EMISSIVE)
#define KEYWORD_PERMUTATION_31
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_32
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_33
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_34
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_35
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_36
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_37
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_38
#elif defined(_USE_WPO) && defined(_USE_ORM)
#define KEYWORD_PERMUTATION_39
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_40
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_41
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_42
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_43
#elif defined(_USE_WPO) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_44
#elif defined(_USE_WPO) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_45
#elif defined(_USE_WPO) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_46
#elif defined(_USE_WPO)
#define KEYWORD_PERMUTATION_47
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_48
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_49
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_50
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_51
#elif defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_52
#elif defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_53
#elif defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_54
#elif defined(_USE_ORM)
#define KEYWORD_PERMUTATION_55
#elif defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_56
#elif defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_57
#elif defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_58
#elif defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_59
#elif defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_60
#elif defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_61
#elif defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_62
#else
#define KEYWORD_PERMUTATION_63
#endif


// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_TEXCOORD1
#define GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_NORMAL_WS
#define VARYINGS_NEED_TANGENT_WS
#define VARYINGS_NEED_TEXCOORD0
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_DEPTHNORMALS


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
 float3 positionOS : POSITION;
 float3 normalOS : NORMAL;
 float4 tangentOS : TANGENT;
 float4 uv0 : TEXCOORD0;
 float4 uv1 : TEXCOORD1;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
 float4 positionCS : SV_POSITION;
 float3 positionWS;
 float3 normalWS;
 float4 tangentWS;
 float4 texCoord0;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};
struct SurfaceDescriptionInputs
{
 float3 WorldSpaceNormal;
 float3 TangentSpaceNormal;
 float3 ObjectSpacePosition;
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 AbsoluteWorldSpacePosition;
 float3 TimeParameters;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
 float4 tangentWS : INTERP0;
 float4 texCoord0 : INTERP1;
 float3 positionWS : INTERP2;
 float3 normalWS : INTERP3;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};

PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.positionCS = input.positionCS;
output.tangentWS.xyzw = input.tangentWS;
output.texCoord0.xyzw = input.texCoord0;
output.positionWS.xyz = input.positionWS;
output.normalWS.xyz = input.normalWS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.positionCS = input.positionCS;
output.tangentWS = input.tangentWS.xyzw;
output.texCoord0 = input.texCoord0.xyzw;
output.positionWS = input.positionWS.xyz;
output.normalWS = input.normalWS.xyz;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}


// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)

float _EnvironmentReactionSupported;
float4 _EnvState0, _EnvState1;
float _FuelCoverage, _AllowFuelCoverage;
float4 _WetTint, _CharTint, _FireColor;
float4 _EnvironmentCellBounds;
float _UseEnvironmentCells;

float _Height_from_Ground_Randomness;
float _Height_from_Ground_Softness;
float _Normal_Pow;
float _Normal_Mul;
float _Mask_Map_Size;
float4 _Coverage_Normal_Map_TexelSize;
float _Mask_Map_Intensity;
float4 _Coverage_ORM_Map_TexelSize;
float _Height_Weight;
float _Height_Gradience;
float _Wind_Intensity;
float3 _Wind_Direction;
float _Wind_Speed;
float _Wind_Time_Random_Offset_Amount;
float _Texture_Size;
float4 _Color_Map_TexelSize;
float4 _Color_Tint;
float4 _Normal_Map_TexelSize;
float _Normal_Intensity;
float4 _ORM_Map_TexelSize;
float _Roughness_Intensity;
float _AO_Intensity;
float _Metallic_Intensity;
float4 _Emissive_Map_TexelSize;
float4 _Emissive_Color;
float4 _Coverage_Color_Map_TexelSize;
float _Coverage_Size;
float _Height_from_Ground;
float4 _Mask_Map_TexelSize;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Coverage_Normal_Map);
SAMPLER(sampler_Coverage_Normal_Map);
TEXTURE2D(_Coverage_ORM_Map);
SAMPLER(sampler_Coverage_ORM_Map);
TEXTURE2D(_Color_Map);
SAMPLER(sampler_Color_Map);
TEXTURE2D(_Normal_Map);
SAMPLER(sampler_Normal_Map);
TEXTURE2D(_ORM_Map);
SAMPLER(sampler_ORM_Map);
TEXTURE2D(_Emissive_Map);
SAMPLER(sampler_Emissive_Map);
TEXTURE2D(_Coverage_Color_Map);
SAMPLER(sampler_Coverage_Color_Map);
TEXTURE2D(_Mask_Map);
SAMPLER(sampler_Mask_Map);

// Graph Includes
// GraphIncludes: <None>

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Functions

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Sine_float(float In, out float Out)
{
    Out = sin(In);
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Length_float3(float3 In, out float Out)
{
    Out = length(In);
}

void Unity_Divide_float(float A, float B, out float Out)
{
    Out = A / B;
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
{
    Out = lerp(A, B, T);
}

struct Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float
{
float3 ObjectSpacePosition;
float3 TimeParameters;
};

void SG_WindSway_51c4027ae50a8cd49be53c4990b9f179_float(float _Sway_Wind_Speed, float3 _Sway_Wind_Direction, float _Sway_Wind_Intensity, float _Sway_Wind_Time_Random_Offset_Amount, Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float IN, out float3 OutVector3_1)
{
float _Property_5c4cd19861f64014ae8e16af9365ef49_Out_0_Float = _Sway_Wind_Time_Random_Offset_Amount;
float _Add_4b31033339dc4f8e8e71674d98e662a3_Out_2_Float;
Unity_Add_float(IN.TimeParameters.x, _Property_5c4cd19861f64014ae8e16af9365ef49_Out_0_Float, _Add_4b31033339dc4f8e8e71674d98e662a3_Out_2_Float);
float _Property_9a93fa589ac7409884a94247349a940f_Out_0_Float = _Sway_Wind_Speed;
float _Multiply_7418655003ae4fedb2bf68678629472c_Out_2_Float;
Unity_Multiply_float_float(_Add_4b31033339dc4f8e8e71674d98e662a3_Out_2_Float, _Property_9a93fa589ac7409884a94247349a940f_Out_0_Float, _Multiply_7418655003ae4fedb2bf68678629472c_Out_2_Float);
float _Sine_ebd14b0fc42b4b4cb4387f3a1b879c1f_Out_1_Float;
Unity_Sine_float(_Multiply_7418655003ae4fedb2bf68678629472c_Out_2_Float, _Sine_ebd14b0fc42b4b4cb4387f3a1b879c1f_Out_1_Float);
float _Add_660e92170c564f94a79277cd2fd0cf20_Out_2_Float;
Unity_Add_float(_Sine_ebd14b0fc42b4b4cb4387f3a1b879c1f_Out_1_Float, float(1), _Add_660e92170c564f94a79277cd2fd0cf20_Out_2_Float);
float _Multiply_b8fd654b771447aebafc1a6aad6957cf_Out_2_Float;
Unity_Multiply_float_float(_Add_660e92170c564f94a79277cd2fd0cf20_Out_2_Float, 0.5, _Multiply_b8fd654b771447aebafc1a6aad6957cf_Out_2_Float);
float3 _Property_fa2ee033b44946c59c3fa9aea4a167c8_Out_0_Vector3 = _Sway_Wind_Direction;
float3 _Multiply_a5500b7f7b8e43988a055eea7eb917f3_Out_2_Vector3;
Unity_Multiply_float3_float3((_Multiply_b8fd654b771447aebafc1a6aad6957cf_Out_2_Float.xxx), _Property_fa2ee033b44946c59c3fa9aea4a167c8_Out_0_Vector3, _Multiply_a5500b7f7b8e43988a055eea7eb917f3_Out_2_Vector3);
float _Property_66cba0a3223546238e18b58e017e756f_Out_0_Float = _Sway_Wind_Intensity;
float3 _Multiply_475f81423e39483cba045bef08b9f967_Out_2_Vector3;
Unity_Multiply_float3_float3(_Multiply_a5500b7f7b8e43988a055eea7eb917f3_Out_2_Vector3, (_Property_66cba0a3223546238e18b58e017e756f_Out_0_Float.xxx), _Multiply_475f81423e39483cba045bef08b9f967_Out_2_Vector3);
float3 _Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3;
Unity_Add_float3(_Multiply_475f81423e39483cba045bef08b9f967_Out_2_Vector3, IN.ObjectSpacePosition, _Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3);
float _Length_53965ffb38de43359ab019a247194557_Out_1_Float;
Unity_Length_float3(IN.ObjectSpacePosition, _Length_53965ffb38de43359ab019a247194557_Out_1_Float);
float _Length_77b6b3d742104f2abf3c6968dea9a342_Out_1_Float;
Unity_Length_float3(_Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3, _Length_77b6b3d742104f2abf3c6968dea9a342_Out_1_Float);
float _Divide_f5dd43de79e343638d2743ee93ce7326_Out_2_Float;
Unity_Divide_float(_Length_53965ffb38de43359ab019a247194557_Out_1_Float, _Length_77b6b3d742104f2abf3c6968dea9a342_Out_1_Float, _Divide_f5dd43de79e343638d2743ee93ce7326_Out_2_Float);
float3 _Lerp_7574daf550e1428cb9d35656c7bb9603_Out_3_Vector3;
Unity_Lerp_float3(float3(0, 0, 0), _Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3, (_Divide_f5dd43de79e343638d2743ee93ce7326_Out_2_Float.xxx), _Lerp_7574daf550e1428cb9d35656c7bb9603_Out_3_Vector3);
OutVector3_1 = _Lerp_7574daf550e1428cb9d35656c7bb9603_Out_3_Vector3;
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

void Unity_Power_float(float A, float B, out float Out)
{
    Out = pow(A, B);
}

void Unity_Divide_float4(float4 A, float4 B, out float4 Out)
{
    Out = A / B;
}

void Unity_NormalStrength_float(float3 In, float Strength, out float3 Out)
{
    Out = float3(In.rg * Strength, lerp(1, In.b, saturate(Strength)));
}

void Unity_Normalize_float3(float3 In, out float3 Out)
{
    Out = normalize(In);
}

void Unity_RandomRange_float(float2 Seed, float Min, float Max, out float Out)
{
     float randomno =  frac(sin(dot(Seed, float2(12.9898, 78.233)))*43758.5453);
     Out = lerp(Min, Max, randomno);
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
}

void Unity_DotProduct_float3(float3 A, float3 B, out float Out)
{
    Out = dot(A, B);
}

void Unity_Step_float(float Edge, float In, out float Out)
{
    Out = step(Edge, In);
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 Position;
float3 Normal;
float3 Tangent;
};

VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
float _Property_b2e048f6276d4371b6ad931f5d1baa87_Out_0_Float = _Wind_Speed;
float3 _Property_7133a0551bc7429cb8bbfcbad044bf97_Out_0_Vector3 = _Wind_Direction;
float _Property_5aa2352867cd4da79dc9f9f9979925c7_Out_0_Float = _Wind_Intensity;
float _Property_e0c6ebff99e349d6966115347961a70c_Out_0_Float = _Wind_Time_Random_Offset_Amount;
Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf;
_WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf.ObjectSpacePosition = IN.ObjectSpacePosition;
_WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf.TimeParameters = IN.TimeParameters;
float3 _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf_OutVector3_1_Vector3;
SG_WindSway_51c4027ae50a8cd49be53c4990b9f179_float(_Property_b2e048f6276d4371b6ad931f5d1baa87_Out_0_Float, _Property_7133a0551bc7429cb8bbfcbad044bf97_Out_0_Vector3, _Property_5aa2352867cd4da79dc9f9f9979925c7_Out_0_Float, _Property_e0c6ebff99e349d6966115347961a70c_Out_0_Float, _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf, _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf_OutVector3_1_Vector3);
float _Split_38420bd2034746a1878048f0ead2a7b2_R_1_Float = IN.AbsoluteWorldSpacePosition[0];
float _Split_38420bd2034746a1878048f0ead2a7b2_G_2_Float = IN.AbsoluteWorldSpacePosition[1];
float _Split_38420bd2034746a1878048f0ead2a7b2_B_3_Float = IN.AbsoluteWorldSpacePosition[2];
float _Split_38420bd2034746a1878048f0ead2a7b2_A_4_Float = 0;
float3 _Vector3_0f8475b3a0df451ea79f6ea2e4e3f673_Out_0_Vector3 = float3(float(0), float(0), float(0));
float3 _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3;
{
// Converting Position from Object to AbsoluteWorld via world space
float3 world;
world = TransformObjectToWorld(_Vector3_0f8475b3a0df451ea79f6ea2e4e3f673_Out_0_Vector3.xyz);
_Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3 = GetAbsolutePositionWS(world);
}
float _Split_fb1b2d84926449e498da6c90f1436311_R_1_Float = _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3[0];
float _Split_fb1b2d84926449e498da6c90f1436311_G_2_Float = _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3[1];
float _Split_fb1b2d84926449e498da6c90f1436311_B_3_Float = _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3[2];
float _Split_fb1b2d84926449e498da6c90f1436311_A_4_Float = 0;
float _Subtract_18ddaa9851a547f1bc3eeafa55f3227b_Out_2_Float;
Unity_Subtract_float(_Split_38420bd2034746a1878048f0ead2a7b2_G_2_Float, _Split_fb1b2d84926449e498da6c90f1436311_G_2_Float, _Subtract_18ddaa9851a547f1bc3eeafa55f3227b_Out_2_Float);
float _Property_1db6873c977b47f186e42eef2cd75a57_Out_0_Float = _Height_Weight;
float _Split_bc5987163dba418d8de90e985de1247b_R_1_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[0];
float _Split_bc5987163dba418d8de90e985de1247b_G_2_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[1];
float _Split_bc5987163dba418d8de90e985de1247b_B_3_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[2];
float _Split_bc5987163dba418d8de90e985de1247b_A_4_Float = 0;
float _Multiply_89de5a5fb3cd451abb1227deba019c38_Out_2_Float;
Unity_Multiply_float_float(_Property_1db6873c977b47f186e42eef2cd75a57_Out_0_Float, _Split_bc5987163dba418d8de90e985de1247b_G_2_Float, _Multiply_89de5a5fb3cd451abb1227deba019c38_Out_2_Float);
float _Divide_2c406de6c63e463f9013e4faf1db1ae8_Out_2_Float;
Unity_Divide_float(_Subtract_18ddaa9851a547f1bc3eeafa55f3227b_Out_2_Float, _Multiply_89de5a5fb3cd451abb1227deba019c38_Out_2_Float, _Divide_2c406de6c63e463f9013e4faf1db1ae8_Out_2_Float);
float _Saturate_e9ea645ca89f482a9d0315df54a77ee5_Out_1_Float;
Unity_Saturate_float(_Divide_2c406de6c63e463f9013e4faf1db1ae8_Out_2_Float, _Saturate_e9ea645ca89f482a9d0315df54a77ee5_Out_1_Float);
float _Property_e96fd1920dff4533951da684129df200_Out_0_Float = _Height_Gradience;
float _Power_b88c5abc85424f17b03c60c98e9bc16c_Out_2_Float;
Unity_Power_float(_Saturate_e9ea645ca89f482a9d0315df54a77ee5_Out_1_Float, _Property_e96fd1920dff4533951da684129df200_Out_0_Float, _Power_b88c5abc85424f17b03c60c98e9bc16c_Out_2_Float);
float _Saturate_6c35d0fa108b483783bde88e06ff5ce0_Out_1_Float;
Unity_Saturate_float(_Power_b88c5abc85424f17b03c60c98e9bc16c_Out_2_Float, _Saturate_6c35d0fa108b483783bde88e06ff5ce0_Out_1_Float);
float3 _Lerp_8461a67d418941ee81ed9eb0f80e8a38_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf_OutVector3_1_Vector3, (_Saturate_6c35d0fa108b483783bde88e06ff5ce0_Out_1_Float.xxx), _Lerp_8461a67d418941ee81ed9eb0f80e8a38_Out_3_Vector3);
#if defined(_USE_WPO)
float3 _UseWPO_81cb92bff33547fbbfaa5ce2f60ba97c_Out_0_Vector3 = _Lerp_8461a67d418941ee81ed9eb0f80e8a38_Out_3_Vector3;
#else
float3 _UseWPO_81cb92bff33547fbbfaa5ce2f60ba97c_Out_0_Vector3 = IN.ObjectSpacePosition;
#endif
description.Position = _UseWPO_81cb92bff33547fbbfaa5ce2f60ba97c_Out_0_Vector3;
description.Normal = IN.ObjectSpaceNormal;
description.Tangent = IN.ObjectSpaceTangent;
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
float3 NormalTS;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
UnityTexture2D _Property_a38bf1e56c364aa0add1f01a99eb6c3e_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Normal_Map, sampler_Normal_Map, _Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_a9551365617a4a66b75c6a49ab8f9c5f_Out_0_Vector4 = IN.uv0;
float _Property_ac4a3a6f20bd42789825abf989f1a228_Out_0_Float = _Texture_Size;
float4 _Divide_1ff147fe44c540028660935d59272eaa_Out_2_Vector4;
Unity_Divide_float4(_UV_a9551365617a4a66b75c6a49ab8f9c5f_Out_0_Vector4, (_Property_ac4a3a6f20bd42789825abf989f1a228_Out_0_Float.xxxx), _Divide_1ff147fe44c540028660935d59272eaa_Out_2_Vector4);
float4 _SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_a38bf1e56c364aa0add1f01a99eb6c3e_Out_0_Texture2D.tex, _Property_a38bf1e56c364aa0add1f01a99eb6c3e_Out_0_Texture2D.samplerstate, _Property_a38bf1e56c364aa0add1f01a99eb6c3e_Out_0_Texture2D.GetTransformedUV((_Divide_1ff147fe44c540028660935d59272eaa_Out_2_Vector4.xy)) );
if (_Property_a38bf1e56c364aa0add1f01a99eb6c3e_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4, _Property_a38bf1e56c364aa0add1f01a99eb6c3e_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4);
float _SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_R_4_Float = _SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4.r;
float _SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_G_5_Float = _SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4.g;
float _SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_B_6_Float = _SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4.b;
float _SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_A_7_Float = _SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4.a;
float _Property_ff6f57141462418790b8f0ef1d7d69cc_Out_0_Float = _Normal_Intensity;
float3 _NormalStrength_6e1c84dc8cbd47f988a654d24e83669f_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4.xyz), _Property_ff6f57141462418790b8f0ef1d7d69cc_Out_0_Float, _NormalStrength_6e1c84dc8cbd47f988a654d24e83669f_Out_2_Vector3);
#if defined(_USE_NORMAL_MAPS)
float3 _UseNormalMaps_f01546d53fa54c3191638b3fe3736688_Out_0_Vector3 = _NormalStrength_6e1c84dc8cbd47f988a654d24e83669f_Out_2_Vector3;
#else
float3 _UseNormalMaps_f01546d53fa54c3191638b3fe3736688_Out_0_Vector3 = float3(0, 0, 1);
#endif
UnityTexture2D _Property_15070824f15143aea7ede9b130164716_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Coverage_Normal_Map, sampler_Coverage_Normal_Map, _Coverage_Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_0805a5601f5b49beb942e50ab3714e03_Out_0_Vector4 = IN.uv0;
float _Property_eb7418710f3f4dc6981c01445b1a809e_Out_0_Float = _Coverage_Size;
float4 _Divide_71dd940938164d5a9e7020e44b1cddf3_Out_2_Vector4;
Unity_Divide_float4(_UV_0805a5601f5b49beb942e50ab3714e03_Out_0_Vector4, (_Property_eb7418710f3f4dc6981c01445b1a809e_Out_0_Float.xxxx), _Divide_71dd940938164d5a9e7020e44b1cddf3_Out_2_Vector4);
float4 _SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_15070824f15143aea7ede9b130164716_Out_0_Texture2D.tex, _Property_15070824f15143aea7ede9b130164716_Out_0_Texture2D.samplerstate, _Property_15070824f15143aea7ede9b130164716_Out_0_Texture2D.GetTransformedUV((_Divide_71dd940938164d5a9e7020e44b1cddf3_Out_2_Vector4.xy)) );
if (_Property_15070824f15143aea7ede9b130164716_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4, _Property_15070824f15143aea7ede9b130164716_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4);
float _SampleTexture2D_26c139222fa1483d83940748db6d922f_R_4_Float = _SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4.r;
float _SampleTexture2D_26c139222fa1483d83940748db6d922f_G_5_Float = _SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4.g;
float _SampleTexture2D_26c139222fa1483d83940748db6d922f_B_6_Float = _SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4.b;
float _SampleTexture2D_26c139222fa1483d83940748db6d922f_A_7_Float = _SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4.a;
UnityTexture2D _Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Mask_Map, sampler_Mask_Map, _Mask_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_c2c5030522744aceb603ff104c9ccde0_Out_0_Vector4 = IN.uv0;
float _Property_a975da4683af46ab85b4a95b01cb9f32_Out_0_Float = _Mask_Map_Size;
float4 _Divide_3dd829d125dc4ec1b32772766d56a17b_Out_2_Vector4;
Unity_Divide_float4(_UV_c2c5030522744aceb603ff104c9ccde0_Out_0_Vector4, (_Property_a975da4683af46ab85b4a95b01cb9f32_Out_0_Float.xxxx), _Divide_3dd829d125dc4ec1b32772766d56a17b_Out_2_Vector4);
float4 _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D.tex, _Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D.samplerstate, _Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D.GetTransformedUV((_Divide_3dd829d125dc4ec1b32772766d56a17b_Out_2_Vector4.xy)) );
if (_Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4, _Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_R_4_Float = _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4.r;
float _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_G_5_Float = _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4.g;
float _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_B_6_Float = _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4.b;
float _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_A_7_Float = _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4.a;
float _Multiply_81f5f8bc82304adbae345a5fe4b0024b_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_R_4_Float, 0.1, _Multiply_81f5f8bc82304adbae345a5fe4b0024b_Out_2_Float);
float _Add_33507c032b9e48bfb61763d507bb031c_Out_2_Float;
Unity_Add_float(_Multiply_81f5f8bc82304adbae345a5fe4b0024b_Out_2_Float, float(0.9), _Add_33507c032b9e48bfb61763d507bb031c_Out_2_Float);
float _Property_883f8e1288fd43f1a3674ca1f5364253_Out_0_Float = _Mask_Map_Intensity;
float _Power_b45fec9ffb074e40b5cb5452c0fb982d_Out_2_Float;
Unity_Power_float(_Add_33507c032b9e48bfb61763d507bb031c_Out_2_Float, _Property_883f8e1288fd43f1a3674ca1f5364253_Out_0_Float, _Power_b45fec9ffb074e40b5cb5452c0fb982d_Out_2_Float);
float _Split_b6a9cc69e69c417ba235626d7beed2eb_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_b6a9cc69e69c417ba235626d7beed2eb_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_b6a9cc69e69c417ba235626d7beed2eb_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_b6a9cc69e69c417ba235626d7beed2eb_A_4_Float = 0;
float _Property_73ac42b2cd3d4b43976ac13e73b6de40_Out_0_Float = _Height_from_Ground;
float3 _Transform_425aee5b4b5e409eb94b431642b515fb_Out_1_Vector3;
_Transform_425aee5b4b5e409eb94b431642b515fb_Out_1_Vector3 = TransformObjectToWorld(float3 (0, 0, 0).xyz);
float3 _Normalize_960b1f24c996479a8c1a9a81cabcb835_Out_1_Vector3;
Unity_Normalize_float3(_Transform_425aee5b4b5e409eb94b431642b515fb_Out_1_Vector3, _Normalize_960b1f24c996479a8c1a9a81cabcb835_Out_1_Vector3);
float _Property_11f1157c9f364521bf765c261e8bb93e_Out_0_Float = _Height_from_Ground_Randomness;
float _Multiply_b7a3ed973f104b9a84af846ce299d95f_Out_2_Float;
Unity_Multiply_float_float(_Property_11f1157c9f364521bf765c261e8bb93e_Out_0_Float, -1, _Multiply_b7a3ed973f104b9a84af846ce299d95f_Out_2_Float);
float _RandomRange_82fa55b7f4da4f6ebc597a38d2ca2e6d_Out_3_Float;
Unity_RandomRange_float((_Normalize_960b1f24c996479a8c1a9a81cabcb835_Out_1_Vector3.xy), _Multiply_b7a3ed973f104b9a84af846ce299d95f_Out_2_Float, _Property_11f1157c9f364521bf765c261e8bb93e_Out_0_Float, _RandomRange_82fa55b7f4da4f6ebc597a38d2ca2e6d_Out_3_Float);
float _Add_109ea06d643c4826b0138a85becc2fa9_Out_2_Float;
Unity_Add_float(_Property_73ac42b2cd3d4b43976ac13e73b6de40_Out_0_Float, _RandomRange_82fa55b7f4da4f6ebc597a38d2ca2e6d_Out_3_Float, _Add_109ea06d643c4826b0138a85becc2fa9_Out_2_Float);
float _Subtract_fe60462bdbdd4a22a207774a6a8cb1b8_Out_2_Float;
Unity_Subtract_float(_Split_b6a9cc69e69c417ba235626d7beed2eb_G_2_Float, _Add_109ea06d643c4826b0138a85becc2fa9_Out_2_Float, _Subtract_fe60462bdbdd4a22a207774a6a8cb1b8_Out_2_Float);
float _Property_1487773737e44e78a7c514c7f470e603_Out_0_Float = _Height_from_Ground_Softness;
float _Divide_6eb043a8aee54d87a0c73169597bdcba_Out_2_Float;
Unity_Divide_float(_Subtract_fe60462bdbdd4a22a207774a6a8cb1b8_Out_2_Float, _Property_1487773737e44e78a7c514c7f470e603_Out_0_Float, _Divide_6eb043a8aee54d87a0c73169597bdcba_Out_2_Float);
float _Saturate_d8ec125670414d1888aeec654fcb04e2_Out_1_Float;
Unity_Saturate_float(_Divide_6eb043a8aee54d87a0c73169597bdcba_Out_2_Float, _Saturate_d8ec125670414d1888aeec654fcb04e2_Out_1_Float);
float _OneMinus_de85154fa1b742e3b6c9e0f92b85d96c_Out_1_Float;
Unity_OneMinus_float(_Saturate_d8ec125670414d1888aeec654fcb04e2_Out_1_Float, _OneMinus_de85154fa1b742e3b6c9e0f92b85d96c_Out_1_Float);
float _DotProduct_a5624fdd7fd94a4c82ad8c039922e638_Out_2_Float;
Unity_DotProduct_float3(IN.WorldSpaceNormal, float3(0, 1, 0), _DotProduct_a5624fdd7fd94a4c82ad8c039922e638_Out_2_Float);
float _Property_1d95c04e88f443bc91fe7ed217b674b8_Out_0_Float = _Normal_Pow;
float _Power_5b713b10f3e049f8a97be81969b52b7d_Out_2_Float;
Unity_Power_float(_DotProduct_a5624fdd7fd94a4c82ad8c039922e638_Out_2_Float, _Property_1d95c04e88f443bc91fe7ed217b674b8_Out_0_Float, _Power_5b713b10f3e049f8a97be81969b52b7d_Out_2_Float);
float _Property_6314206eb47742698a7d2833f119f39e_Out_0_Float = _Normal_Mul;
float _Multiply_6e6bcfc2a4df414e86d241952840e972_Out_2_Float;
Unity_Multiply_float_float(_Power_5b713b10f3e049f8a97be81969b52b7d_Out_2_Float, _Property_6314206eb47742698a7d2833f119f39e_Out_0_Float, _Multiply_6e6bcfc2a4df414e86d241952840e972_Out_2_Float);
float _Saturate_77d41005224e417eb678b78b9a10771a_Out_1_Float;
Unity_Saturate_float(_Multiply_6e6bcfc2a4df414e86d241952840e972_Out_2_Float, _Saturate_77d41005224e417eb678b78b9a10771a_Out_1_Float);
float _Add_8395f84ea8e546988dd02e5b922d32ee_Out_2_Float;
Unity_Add_float(_OneMinus_de85154fa1b742e3b6c9e0f92b85d96c_Out_1_Float, _Saturate_77d41005224e417eb678b78b9a10771a_Out_1_Float, _Add_8395f84ea8e546988dd02e5b922d32ee_Out_2_Float);
float _Multiply_b61ead58dd024fd7aacad7c0b68f3be7_Out_2_Float;
Unity_Multiply_float_float(5, _Add_8395f84ea8e546988dd02e5b922d32ee_Out_2_Float, _Multiply_b61ead58dd024fd7aacad7c0b68f3be7_Out_2_Float);
float _Multiply_b0c3a49303544ccaa1d15379e6b1e9d2_Out_2_Float;
Unity_Multiply_float_float(_Power_b45fec9ffb074e40b5cb5452c0fb982d_Out_2_Float, _Multiply_b61ead58dd024fd7aacad7c0b68f3be7_Out_2_Float, _Multiply_b0c3a49303544ccaa1d15379e6b1e9d2_Out_2_Float);
float _Step_9e404f0c811346c88c531f42e623243f_Out_2_Float;
Unity_Step_float(_Subtract_fe60462bdbdd4a22a207774a6a8cb1b8_Out_2_Float, float(-0.1), _Step_9e404f0c811346c88c531f42e623243f_Out_2_Float);
float _Add_0328340190574386b2d37a0d035b66fb_Out_2_Float;
Unity_Add_float(_Multiply_b0c3a49303544ccaa1d15379e6b1e9d2_Out_2_Float, _Step_9e404f0c811346c88c531f42e623243f_Out_2_Float, _Add_0328340190574386b2d37a0d035b66fb_Out_2_Float);
float _Power_3bfc30934311465db3ba28e64b49097d_Out_2_Float;
Unity_Power_float(_Add_0328340190574386b2d37a0d035b66fb_Out_2_Float, float(3), _Power_3bfc30934311465db3ba28e64b49097d_Out_2_Float);
float _Saturate_3ba4a70af5214b4086803937a3f8b09f_Out_1_Float;
Unity_Saturate_float(_Power_3bfc30934311465db3ba28e64b49097d_Out_2_Float, _Saturate_3ba4a70af5214b4086803937a3f8b09f_Out_1_Float);
float _Saturate_6e423c9f5f1249088750a2b84a42b764_Out_1_Float;
Unity_Saturate_float(_Add_8395f84ea8e546988dd02e5b922d32ee_Out_2_Float, _Saturate_6e423c9f5f1249088750a2b84a42b764_Out_1_Float);
#if defined(_USE_COVERAGE_MASK_TEXTURE)
float _UseCoverageMaskTexture_f75c00eee68746bd80e2fa6c2bc28d4c_Out_0_Float = _Saturate_3ba4a70af5214b4086803937a3f8b09f_Out_1_Float;
#else
float _UseCoverageMaskTexture_f75c00eee68746bd80e2fa6c2bc28d4c_Out_0_Float = _Saturate_6e423c9f5f1249088750a2b84a42b764_Out_1_Float;
#endif
float3 _Lerp_a5163d5f017c46b9b347647a7d361156_Out_3_Vector3;
Unity_Lerp_float3(_UseNormalMaps_f01546d53fa54c3191638b3fe3736688_Out_0_Vector3, (_SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4.xyz), (_UseCoverageMaskTexture_f75c00eee68746bd80e2fa6c2bc28d4c_Out_0_Float.xxx), _Lerp_a5163d5f017c46b9b347647a7d361156_Out_3_Vector3);
#if defined(_USE_COVERAGE)
float3 _UseCoverage_d6109394a0154e89890cf6ca228f700f_Out_0_Vector3 = _Lerp_a5163d5f017c46b9b347647a7d361156_Out_3_Vector3;
#else
float3 _UseCoverage_d6109394a0154e89890cf6ca228f700f_Out_0_Vector3 = _UseNormalMaps_f01546d53fa54c3191638b3fe3736688_Out_0_Vector3;
#endif
#if defined(_USE_NORMAL_MAPS)
float3 _UseNormalMaps_cf0fe6c3e8224d69a92ff77f7f270e12_Out_0_Vector3 = _UseCoverage_d6109394a0154e89890cf6ca228f700f_Out_0_Vector3;
#else
float3 _UseNormalMaps_cf0fe6c3e8224d69a92ff77f7f270e12_Out_0_Vector3 = float3(0, 0, 1);
#endif
surface.NormalTS = _UseNormalMaps_cf0fe6c3e8224d69a92ff77f7f270e12_Out_0_Vector3;
float3 reactionUnusedColor = 0, reactionUnusedEmission = 0;
ClipVegetationFuel(TransformObjectToWorld(float3(0,0,0)));
return surface;
}

// --------------------------------------------------
// Build Graph Inputs
#ifdef HAVE_VFX_MODIFICATION
#define VFX_SRP_ATTRIBUTES Attributes
#define VFX_SRP_VARYINGS Varyings
#define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
#endif
VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

    output.ObjectSpaceNormal =                          input.normalOS;
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
    output.AbsoluteWorldSpacePosition =                 GetAbsolutePositionWS(TransformObjectToWorld(input.positionOS));
    output.TimeParameters =                             _TimeParameters.xyz;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

#ifdef HAVE_VFX_MODIFICATION
#if VFX_USE_GRAPH_VALUES
    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
#endif
    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */

#endif

    

    // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
    float3 unnormalizedNormalWS = input.normalWS;
    const float renormFactor = 1.0 / length(unnormalizedNormalWS);


    output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph
    output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);


    output.ObjectSpacePosition = TransformWorldToObject(input.positionWS);

    #if UNITY_UV_STARTS_AT_TOP
    #else
    #endif


    output.uv0 = input.texCoord0;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

// --------------------------------------------------
// Main

#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthNormalsOnlyPass.hlsl"

// --------------------------------------------------
// Visual Effect Vertex Invocations
#ifdef HAVE_VFX_MODIFICATION
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
#endif

ENDHLSL
}
Pass
{
    Name "Meta"
    Tags
    {
        "LightMode" = "Meta"
    }

// Render State
Cull Off

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 2.0
#pragma vertex vert
#pragma fragment frag

// Keywords
#pragma shader_feature _ EDITOR_VISUALIZATION
#pragma shader_feature_local _ _USE_EMISSIVE
#pragma shader_feature_local _ _USE_WPO
#pragma shader_feature_local _ _USE_ORM
#pragma shader_feature_local _ _USE_NORMAL_MAPS
#pragma shader_feature_local _ _USE_COVERAGE
#pragma shader_feature_local _ _USE_COVERAGE_MASK_TEXTURE

#if defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_2
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_3
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_4
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_5
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_6
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM)
#define KEYWORD_PERMUTATION_7
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_8
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_9
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_10
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_11
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_12
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_13
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_14
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO)
#define KEYWORD_PERMUTATION_15
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_16
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_17
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_18
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_19
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_20
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_21
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_22
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM)
#define KEYWORD_PERMUTATION_23
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_24
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_25
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_26
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_27
#elif defined(_USE_EMISSIVE) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_28
#elif defined(_USE_EMISSIVE) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_29
#elif defined(_USE_EMISSIVE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_30
#elif defined(_USE_EMISSIVE)
#define KEYWORD_PERMUTATION_31
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_32
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_33
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_34
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_35
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_36
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_37
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_38
#elif defined(_USE_WPO) && defined(_USE_ORM)
#define KEYWORD_PERMUTATION_39
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_40
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_41
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_42
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_43
#elif defined(_USE_WPO) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_44
#elif defined(_USE_WPO) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_45
#elif defined(_USE_WPO) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_46
#elif defined(_USE_WPO)
#define KEYWORD_PERMUTATION_47
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_48
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_49
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_50
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_51
#elif defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_52
#elif defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_53
#elif defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_54
#elif defined(_USE_ORM)
#define KEYWORD_PERMUTATION_55
#elif defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_56
#elif defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_57
#elif defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_58
#elif defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_59
#elif defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_60
#elif defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_61
#elif defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_62
#else
#define KEYWORD_PERMUTATION_63
#endif


// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_TEXCOORD1
#define ATTRIBUTES_NEED_TEXCOORD2
#define ATTRIBUTES_NEED_INSTANCEID
#define GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_NORMAL_WS
#define VARYINGS_NEED_TEXCOORD0
#define VARYINGS_NEED_TEXCOORD1
#define VARYINGS_NEED_TEXCOORD2
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_META
#define _FOG_FRAGMENT 1


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
 float3 positionOS : POSITION;
 float3 normalOS : NORMAL;
 float4 tangentOS : TANGENT;
 float4 uv0 : TEXCOORD0;
 float4 uv1 : TEXCOORD1;
 float4 uv2 : TEXCOORD2;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
 float4 positionCS : SV_POSITION;
 float3 positionWS;
 float3 normalWS;
 float4 texCoord0;
 float4 texCoord1;
 float4 texCoord2;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};
struct SurfaceDescriptionInputs
{
 float3 WorldSpaceNormal;
 float3 ObjectSpacePosition;
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 AbsoluteWorldSpacePosition;
 float3 TimeParameters;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
 float4 texCoord0 : INTERP0;
 float4 texCoord1 : INTERP1;
 float4 texCoord2 : INTERP2;
 float3 positionWS : INTERP3;
 float3 normalWS : INTERP4;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};

PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.positionCS = input.positionCS;
output.texCoord0.xyzw = input.texCoord0;
output.texCoord1.xyzw = input.texCoord1;
output.texCoord2.xyzw = input.texCoord2;
output.positionWS.xyz = input.positionWS;
output.normalWS.xyz = input.normalWS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.positionCS = input.positionCS;
output.texCoord0 = input.texCoord0.xyzw;
output.texCoord1 = input.texCoord1.xyzw;
output.texCoord2 = input.texCoord2.xyzw;
output.positionWS = input.positionWS.xyz;
output.normalWS = input.normalWS.xyz;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}


// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)

float _EnvironmentReactionSupported;
float4 _EnvState0, _EnvState1;
float _FuelCoverage, _AllowFuelCoverage;
float4 _WetTint, _CharTint, _FireColor;
float4 _EnvironmentCellBounds;
float _UseEnvironmentCells;

float _Height_from_Ground_Randomness;
float _Height_from_Ground_Softness;
float _Normal_Pow;
float _Normal_Mul;
float _Mask_Map_Size;
float4 _Coverage_Normal_Map_TexelSize;
float _Mask_Map_Intensity;
float4 _Coverage_ORM_Map_TexelSize;
float _Height_Weight;
float _Height_Gradience;
float _Wind_Intensity;
float3 _Wind_Direction;
float _Wind_Speed;
float _Wind_Time_Random_Offset_Amount;
float _Texture_Size;
float4 _Color_Map_TexelSize;
float4 _Color_Tint;
float4 _Normal_Map_TexelSize;
float _Normal_Intensity;
float4 _ORM_Map_TexelSize;
float _Roughness_Intensity;
float _AO_Intensity;
float _Metallic_Intensity;
float4 _Emissive_Map_TexelSize;
float4 _Emissive_Color;
float4 _Coverage_Color_Map_TexelSize;
float _Coverage_Size;
float _Height_from_Ground;
float4 _Mask_Map_TexelSize;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Coverage_Normal_Map);
SAMPLER(sampler_Coverage_Normal_Map);
TEXTURE2D(_Coverage_ORM_Map);
SAMPLER(sampler_Coverage_ORM_Map);
TEXTURE2D(_Color_Map);
SAMPLER(sampler_Color_Map);
TEXTURE2D(_Normal_Map);
SAMPLER(sampler_Normal_Map);
TEXTURE2D(_ORM_Map);
SAMPLER(sampler_ORM_Map);
TEXTURE2D(_Emissive_Map);
SAMPLER(sampler_Emissive_Map);
TEXTURE2D(_Coverage_Color_Map);
SAMPLER(sampler_Coverage_Color_Map);
TEXTURE2D(_Mask_Map);
SAMPLER(sampler_Mask_Map);

// Graph Includes
// GraphIncludes: <None>

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Functions

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Sine_float(float In, out float Out)
{
    Out = sin(In);
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Length_float3(float3 In, out float Out)
{
    Out = length(In);
}

void Unity_Divide_float(float A, float B, out float Out)
{
    Out = A / B;
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
{
    Out = lerp(A, B, T);
}

struct Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float
{
float3 ObjectSpacePosition;
float3 TimeParameters;
};

void SG_WindSway_51c4027ae50a8cd49be53c4990b9f179_float(float _Sway_Wind_Speed, float3 _Sway_Wind_Direction, float _Sway_Wind_Intensity, float _Sway_Wind_Time_Random_Offset_Amount, Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float IN, out float3 OutVector3_1)
{
float _Property_5c4cd19861f64014ae8e16af9365ef49_Out_0_Float = _Sway_Wind_Time_Random_Offset_Amount;
float _Add_4b31033339dc4f8e8e71674d98e662a3_Out_2_Float;
Unity_Add_float(IN.TimeParameters.x, _Property_5c4cd19861f64014ae8e16af9365ef49_Out_0_Float, _Add_4b31033339dc4f8e8e71674d98e662a3_Out_2_Float);
float _Property_9a93fa589ac7409884a94247349a940f_Out_0_Float = _Sway_Wind_Speed;
float _Multiply_7418655003ae4fedb2bf68678629472c_Out_2_Float;
Unity_Multiply_float_float(_Add_4b31033339dc4f8e8e71674d98e662a3_Out_2_Float, _Property_9a93fa589ac7409884a94247349a940f_Out_0_Float, _Multiply_7418655003ae4fedb2bf68678629472c_Out_2_Float);
float _Sine_ebd14b0fc42b4b4cb4387f3a1b879c1f_Out_1_Float;
Unity_Sine_float(_Multiply_7418655003ae4fedb2bf68678629472c_Out_2_Float, _Sine_ebd14b0fc42b4b4cb4387f3a1b879c1f_Out_1_Float);
float _Add_660e92170c564f94a79277cd2fd0cf20_Out_2_Float;
Unity_Add_float(_Sine_ebd14b0fc42b4b4cb4387f3a1b879c1f_Out_1_Float, float(1), _Add_660e92170c564f94a79277cd2fd0cf20_Out_2_Float);
float _Multiply_b8fd654b771447aebafc1a6aad6957cf_Out_2_Float;
Unity_Multiply_float_float(_Add_660e92170c564f94a79277cd2fd0cf20_Out_2_Float, 0.5, _Multiply_b8fd654b771447aebafc1a6aad6957cf_Out_2_Float);
float3 _Property_fa2ee033b44946c59c3fa9aea4a167c8_Out_0_Vector3 = _Sway_Wind_Direction;
float3 _Multiply_a5500b7f7b8e43988a055eea7eb917f3_Out_2_Vector3;
Unity_Multiply_float3_float3((_Multiply_b8fd654b771447aebafc1a6aad6957cf_Out_2_Float.xxx), _Property_fa2ee033b44946c59c3fa9aea4a167c8_Out_0_Vector3, _Multiply_a5500b7f7b8e43988a055eea7eb917f3_Out_2_Vector3);
float _Property_66cba0a3223546238e18b58e017e756f_Out_0_Float = _Sway_Wind_Intensity;
float3 _Multiply_475f81423e39483cba045bef08b9f967_Out_2_Vector3;
Unity_Multiply_float3_float3(_Multiply_a5500b7f7b8e43988a055eea7eb917f3_Out_2_Vector3, (_Property_66cba0a3223546238e18b58e017e756f_Out_0_Float.xxx), _Multiply_475f81423e39483cba045bef08b9f967_Out_2_Vector3);
float3 _Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3;
Unity_Add_float3(_Multiply_475f81423e39483cba045bef08b9f967_Out_2_Vector3, IN.ObjectSpacePosition, _Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3);
float _Length_53965ffb38de43359ab019a247194557_Out_1_Float;
Unity_Length_float3(IN.ObjectSpacePosition, _Length_53965ffb38de43359ab019a247194557_Out_1_Float);
float _Length_77b6b3d742104f2abf3c6968dea9a342_Out_1_Float;
Unity_Length_float3(_Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3, _Length_77b6b3d742104f2abf3c6968dea9a342_Out_1_Float);
float _Divide_f5dd43de79e343638d2743ee93ce7326_Out_2_Float;
Unity_Divide_float(_Length_53965ffb38de43359ab019a247194557_Out_1_Float, _Length_77b6b3d742104f2abf3c6968dea9a342_Out_1_Float, _Divide_f5dd43de79e343638d2743ee93ce7326_Out_2_Float);
float3 _Lerp_7574daf550e1428cb9d35656c7bb9603_Out_3_Vector3;
Unity_Lerp_float3(float3(0, 0, 0), _Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3, (_Divide_f5dd43de79e343638d2743ee93ce7326_Out_2_Float.xxx), _Lerp_7574daf550e1428cb9d35656c7bb9603_Out_3_Vector3);
OutVector3_1 = _Lerp_7574daf550e1428cb9d35656c7bb9603_Out_3_Vector3;
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

void Unity_Power_float(float A, float B, out float Out)
{
    Out = pow(A, B);
}

void Unity_Divide_float4(float4 A, float4 B, out float4 Out)
{
    Out = A / B;
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

void Unity_Normalize_float3(float3 In, out float3 Out)
{
    Out = normalize(In);
}

void Unity_RandomRange_float(float2 Seed, float Min, float Max, out float Out)
{
     float randomno =  frac(sin(dot(Seed, float2(12.9898, 78.233)))*43758.5453);
     Out = lerp(Min, Max, randomno);
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
}

void Unity_DotProduct_float3(float3 A, float3 B, out float Out)
{
    Out = dot(A, B);
}

void Unity_Step_float(float Edge, float In, out float Out)
{
    Out = step(Edge, In);
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 Position;
float3 Normal;
float3 Tangent;
};

VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
float _Property_b2e048f6276d4371b6ad931f5d1baa87_Out_0_Float = _Wind_Speed;
float3 _Property_7133a0551bc7429cb8bbfcbad044bf97_Out_0_Vector3 = _Wind_Direction;
float _Property_5aa2352867cd4da79dc9f9f9979925c7_Out_0_Float = _Wind_Intensity;
float _Property_e0c6ebff99e349d6966115347961a70c_Out_0_Float = _Wind_Time_Random_Offset_Amount;
Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf;
_WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf.ObjectSpacePosition = IN.ObjectSpacePosition;
_WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf.TimeParameters = IN.TimeParameters;
float3 _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf_OutVector3_1_Vector3;
SG_WindSway_51c4027ae50a8cd49be53c4990b9f179_float(_Property_b2e048f6276d4371b6ad931f5d1baa87_Out_0_Float, _Property_7133a0551bc7429cb8bbfcbad044bf97_Out_0_Vector3, _Property_5aa2352867cd4da79dc9f9f9979925c7_Out_0_Float, _Property_e0c6ebff99e349d6966115347961a70c_Out_0_Float, _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf, _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf_OutVector3_1_Vector3);
float _Split_38420bd2034746a1878048f0ead2a7b2_R_1_Float = IN.AbsoluteWorldSpacePosition[0];
float _Split_38420bd2034746a1878048f0ead2a7b2_G_2_Float = IN.AbsoluteWorldSpacePosition[1];
float _Split_38420bd2034746a1878048f0ead2a7b2_B_3_Float = IN.AbsoluteWorldSpacePosition[2];
float _Split_38420bd2034746a1878048f0ead2a7b2_A_4_Float = 0;
float3 _Vector3_0f8475b3a0df451ea79f6ea2e4e3f673_Out_0_Vector3 = float3(float(0), float(0), float(0));
float3 _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3;
{
// Converting Position from Object to AbsoluteWorld via world space
float3 world;
world = TransformObjectToWorld(_Vector3_0f8475b3a0df451ea79f6ea2e4e3f673_Out_0_Vector3.xyz);
_Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3 = GetAbsolutePositionWS(world);
}
float _Split_fb1b2d84926449e498da6c90f1436311_R_1_Float = _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3[0];
float _Split_fb1b2d84926449e498da6c90f1436311_G_2_Float = _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3[1];
float _Split_fb1b2d84926449e498da6c90f1436311_B_3_Float = _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3[2];
float _Split_fb1b2d84926449e498da6c90f1436311_A_4_Float = 0;
float _Subtract_18ddaa9851a547f1bc3eeafa55f3227b_Out_2_Float;
Unity_Subtract_float(_Split_38420bd2034746a1878048f0ead2a7b2_G_2_Float, _Split_fb1b2d84926449e498da6c90f1436311_G_2_Float, _Subtract_18ddaa9851a547f1bc3eeafa55f3227b_Out_2_Float);
float _Property_1db6873c977b47f186e42eef2cd75a57_Out_0_Float = _Height_Weight;
float _Split_bc5987163dba418d8de90e985de1247b_R_1_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[0];
float _Split_bc5987163dba418d8de90e985de1247b_G_2_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[1];
float _Split_bc5987163dba418d8de90e985de1247b_B_3_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[2];
float _Split_bc5987163dba418d8de90e985de1247b_A_4_Float = 0;
float _Multiply_89de5a5fb3cd451abb1227deba019c38_Out_2_Float;
Unity_Multiply_float_float(_Property_1db6873c977b47f186e42eef2cd75a57_Out_0_Float, _Split_bc5987163dba418d8de90e985de1247b_G_2_Float, _Multiply_89de5a5fb3cd451abb1227deba019c38_Out_2_Float);
float _Divide_2c406de6c63e463f9013e4faf1db1ae8_Out_2_Float;
Unity_Divide_float(_Subtract_18ddaa9851a547f1bc3eeafa55f3227b_Out_2_Float, _Multiply_89de5a5fb3cd451abb1227deba019c38_Out_2_Float, _Divide_2c406de6c63e463f9013e4faf1db1ae8_Out_2_Float);
float _Saturate_e9ea645ca89f482a9d0315df54a77ee5_Out_1_Float;
Unity_Saturate_float(_Divide_2c406de6c63e463f9013e4faf1db1ae8_Out_2_Float, _Saturate_e9ea645ca89f482a9d0315df54a77ee5_Out_1_Float);
float _Property_e96fd1920dff4533951da684129df200_Out_0_Float = _Height_Gradience;
float _Power_b88c5abc85424f17b03c60c98e9bc16c_Out_2_Float;
Unity_Power_float(_Saturate_e9ea645ca89f482a9d0315df54a77ee5_Out_1_Float, _Property_e96fd1920dff4533951da684129df200_Out_0_Float, _Power_b88c5abc85424f17b03c60c98e9bc16c_Out_2_Float);
float _Saturate_6c35d0fa108b483783bde88e06ff5ce0_Out_1_Float;
Unity_Saturate_float(_Power_b88c5abc85424f17b03c60c98e9bc16c_Out_2_Float, _Saturate_6c35d0fa108b483783bde88e06ff5ce0_Out_1_Float);
float3 _Lerp_8461a67d418941ee81ed9eb0f80e8a38_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf_OutVector3_1_Vector3, (_Saturate_6c35d0fa108b483783bde88e06ff5ce0_Out_1_Float.xxx), _Lerp_8461a67d418941ee81ed9eb0f80e8a38_Out_3_Vector3);
#if defined(_USE_WPO)
float3 _UseWPO_81cb92bff33547fbbfaa5ce2f60ba97c_Out_0_Vector3 = _Lerp_8461a67d418941ee81ed9eb0f80e8a38_Out_3_Vector3;
#else
float3 _UseWPO_81cb92bff33547fbbfaa5ce2f60ba97c_Out_0_Vector3 = IN.ObjectSpacePosition;
#endif
description.Position = _UseWPO_81cb92bff33547fbbfaa5ce2f60ba97c_Out_0_Vector3;
description.Normal = IN.ObjectSpaceNormal;
description.Tangent = IN.ObjectSpaceTangent;
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
float3 BaseColor;
float3 Emission;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
UnityTexture2D _Property_0c79649b4042462682b4a7423715f1a1_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Color_Map, sampler_Color_Map, _Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_a9551365617a4a66b75c6a49ab8f9c5f_Out_0_Vector4 = IN.uv0;
float _Property_ac4a3a6f20bd42789825abf989f1a228_Out_0_Float = _Texture_Size;
float4 _Divide_1ff147fe44c540028660935d59272eaa_Out_2_Vector4;
Unity_Divide_float4(_UV_a9551365617a4a66b75c6a49ab8f9c5f_Out_0_Vector4, (_Property_ac4a3a6f20bd42789825abf989f1a228_Out_0_Float.xxxx), _Divide_1ff147fe44c540028660935d59272eaa_Out_2_Vector4);
float4 _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_0c79649b4042462682b4a7423715f1a1_Out_0_Texture2D.tex, _Property_0c79649b4042462682b4a7423715f1a1_Out_0_Texture2D.samplerstate, _Property_0c79649b4042462682b4a7423715f1a1_Out_0_Texture2D.GetTransformedUV((_Divide_1ff147fe44c540028660935d59272eaa_Out_2_Vector4.xy)) );
if (_Property_0c79649b4042462682b4a7423715f1a1_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4, _Property_0c79649b4042462682b4a7423715f1a1_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_R_4_Float = _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4.r;
float _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_G_5_Float = _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4.g;
float _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_B_6_Float = _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4.b;
float _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_A_7_Float = _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4.a;
float4 _Property_11be52d8078b41908ba72022455e8b98_Out_0_Vector4 = _Color_Tint;
float4 _Multiply_e3ef32e4a4e8442188dfd3c93394960c_Out_2_Vector4;
Unity_Multiply_float4_float4(_SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4, _Property_11be52d8078b41908ba72022455e8b98_Out_0_Vector4, _Multiply_e3ef32e4a4e8442188dfd3c93394960c_Out_2_Vector4);
UnityTexture2D _Property_415df38a141841e6aa11a7b1e336c379_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Coverage_Color_Map, sampler_Coverage_Color_Map, _Coverage_Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_0805a5601f5b49beb942e50ab3714e03_Out_0_Vector4 = IN.uv0;
float _Property_eb7418710f3f4dc6981c01445b1a809e_Out_0_Float = _Coverage_Size;
float4 _Divide_71dd940938164d5a9e7020e44b1cddf3_Out_2_Vector4;
Unity_Divide_float4(_UV_0805a5601f5b49beb942e50ab3714e03_Out_0_Vector4, (_Property_eb7418710f3f4dc6981c01445b1a809e_Out_0_Float.xxxx), _Divide_71dd940938164d5a9e7020e44b1cddf3_Out_2_Vector4);
float4 _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_415df38a141841e6aa11a7b1e336c379_Out_0_Texture2D.tex, _Property_415df38a141841e6aa11a7b1e336c379_Out_0_Texture2D.samplerstate, _Property_415df38a141841e6aa11a7b1e336c379_Out_0_Texture2D.GetTransformedUV((_Divide_71dd940938164d5a9e7020e44b1cddf3_Out_2_Vector4.xy)) );
if (_Property_415df38a141841e6aa11a7b1e336c379_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4, _Property_415df38a141841e6aa11a7b1e336c379_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_R_4_Float = _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4.r;
float _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_G_5_Float = _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4.g;
float _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_B_6_Float = _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4.b;
float _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_A_7_Float = _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4.a;
UnityTexture2D _Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Mask_Map, sampler_Mask_Map, _Mask_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_c2c5030522744aceb603ff104c9ccde0_Out_0_Vector4 = IN.uv0;
float _Property_a975da4683af46ab85b4a95b01cb9f32_Out_0_Float = _Mask_Map_Size;
float4 _Divide_3dd829d125dc4ec1b32772766d56a17b_Out_2_Vector4;
Unity_Divide_float4(_UV_c2c5030522744aceb603ff104c9ccde0_Out_0_Vector4, (_Property_a975da4683af46ab85b4a95b01cb9f32_Out_0_Float.xxxx), _Divide_3dd829d125dc4ec1b32772766d56a17b_Out_2_Vector4);
float4 _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D.tex, _Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D.samplerstate, _Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D.GetTransformedUV((_Divide_3dd829d125dc4ec1b32772766d56a17b_Out_2_Vector4.xy)) );
if (_Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4, _Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_R_4_Float = _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4.r;
float _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_G_5_Float = _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4.g;
float _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_B_6_Float = _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4.b;
float _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_A_7_Float = _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4.a;
float _Multiply_81f5f8bc82304adbae345a5fe4b0024b_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_R_4_Float, 0.1, _Multiply_81f5f8bc82304adbae345a5fe4b0024b_Out_2_Float);
float _Add_33507c032b9e48bfb61763d507bb031c_Out_2_Float;
Unity_Add_float(_Multiply_81f5f8bc82304adbae345a5fe4b0024b_Out_2_Float, float(0.9), _Add_33507c032b9e48bfb61763d507bb031c_Out_2_Float);
float _Property_883f8e1288fd43f1a3674ca1f5364253_Out_0_Float = _Mask_Map_Intensity;
float _Power_b45fec9ffb074e40b5cb5452c0fb982d_Out_2_Float;
Unity_Power_float(_Add_33507c032b9e48bfb61763d507bb031c_Out_2_Float, _Property_883f8e1288fd43f1a3674ca1f5364253_Out_0_Float, _Power_b45fec9ffb074e40b5cb5452c0fb982d_Out_2_Float);
float _Split_b6a9cc69e69c417ba235626d7beed2eb_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_b6a9cc69e69c417ba235626d7beed2eb_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_b6a9cc69e69c417ba235626d7beed2eb_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_b6a9cc69e69c417ba235626d7beed2eb_A_4_Float = 0;
float _Property_73ac42b2cd3d4b43976ac13e73b6de40_Out_0_Float = _Height_from_Ground;
float3 _Transform_425aee5b4b5e409eb94b431642b515fb_Out_1_Vector3;
_Transform_425aee5b4b5e409eb94b431642b515fb_Out_1_Vector3 = TransformObjectToWorld(float3 (0, 0, 0).xyz);
float3 _Normalize_960b1f24c996479a8c1a9a81cabcb835_Out_1_Vector3;
Unity_Normalize_float3(_Transform_425aee5b4b5e409eb94b431642b515fb_Out_1_Vector3, _Normalize_960b1f24c996479a8c1a9a81cabcb835_Out_1_Vector3);
float _Property_11f1157c9f364521bf765c261e8bb93e_Out_0_Float = _Height_from_Ground_Randomness;
float _Multiply_b7a3ed973f104b9a84af846ce299d95f_Out_2_Float;
Unity_Multiply_float_float(_Property_11f1157c9f364521bf765c261e8bb93e_Out_0_Float, -1, _Multiply_b7a3ed973f104b9a84af846ce299d95f_Out_2_Float);
float _RandomRange_82fa55b7f4da4f6ebc597a38d2ca2e6d_Out_3_Float;
Unity_RandomRange_float((_Normalize_960b1f24c996479a8c1a9a81cabcb835_Out_1_Vector3.xy), _Multiply_b7a3ed973f104b9a84af846ce299d95f_Out_2_Float, _Property_11f1157c9f364521bf765c261e8bb93e_Out_0_Float, _RandomRange_82fa55b7f4da4f6ebc597a38d2ca2e6d_Out_3_Float);
float _Add_109ea06d643c4826b0138a85becc2fa9_Out_2_Float;
Unity_Add_float(_Property_73ac42b2cd3d4b43976ac13e73b6de40_Out_0_Float, _RandomRange_82fa55b7f4da4f6ebc597a38d2ca2e6d_Out_3_Float, _Add_109ea06d643c4826b0138a85becc2fa9_Out_2_Float);
float _Subtract_fe60462bdbdd4a22a207774a6a8cb1b8_Out_2_Float;
Unity_Subtract_float(_Split_b6a9cc69e69c417ba235626d7beed2eb_G_2_Float, _Add_109ea06d643c4826b0138a85becc2fa9_Out_2_Float, _Subtract_fe60462bdbdd4a22a207774a6a8cb1b8_Out_2_Float);
float _Property_1487773737e44e78a7c514c7f470e603_Out_0_Float = _Height_from_Ground_Softness;
float _Divide_6eb043a8aee54d87a0c73169597bdcba_Out_2_Float;
Unity_Divide_float(_Subtract_fe60462bdbdd4a22a207774a6a8cb1b8_Out_2_Float, _Property_1487773737e44e78a7c514c7f470e603_Out_0_Float, _Divide_6eb043a8aee54d87a0c73169597bdcba_Out_2_Float);
float _Saturate_d8ec125670414d1888aeec654fcb04e2_Out_1_Float;
Unity_Saturate_float(_Divide_6eb043a8aee54d87a0c73169597bdcba_Out_2_Float, _Saturate_d8ec125670414d1888aeec654fcb04e2_Out_1_Float);
float _OneMinus_de85154fa1b742e3b6c9e0f92b85d96c_Out_1_Float;
Unity_OneMinus_float(_Saturate_d8ec125670414d1888aeec654fcb04e2_Out_1_Float, _OneMinus_de85154fa1b742e3b6c9e0f92b85d96c_Out_1_Float);
float _DotProduct_a5624fdd7fd94a4c82ad8c039922e638_Out_2_Float;
Unity_DotProduct_float3(IN.WorldSpaceNormal, float3(0, 1, 0), _DotProduct_a5624fdd7fd94a4c82ad8c039922e638_Out_2_Float);
float _Property_1d95c04e88f443bc91fe7ed217b674b8_Out_0_Float = _Normal_Pow;
float _Power_5b713b10f3e049f8a97be81969b52b7d_Out_2_Float;
Unity_Power_float(_DotProduct_a5624fdd7fd94a4c82ad8c039922e638_Out_2_Float, _Property_1d95c04e88f443bc91fe7ed217b674b8_Out_0_Float, _Power_5b713b10f3e049f8a97be81969b52b7d_Out_2_Float);
float _Property_6314206eb47742698a7d2833f119f39e_Out_0_Float = _Normal_Mul;
float _Multiply_6e6bcfc2a4df414e86d241952840e972_Out_2_Float;
Unity_Multiply_float_float(_Power_5b713b10f3e049f8a97be81969b52b7d_Out_2_Float, _Property_6314206eb47742698a7d2833f119f39e_Out_0_Float, _Multiply_6e6bcfc2a4df414e86d241952840e972_Out_2_Float);
float _Saturate_77d41005224e417eb678b78b9a10771a_Out_1_Float;
Unity_Saturate_float(_Multiply_6e6bcfc2a4df414e86d241952840e972_Out_2_Float, _Saturate_77d41005224e417eb678b78b9a10771a_Out_1_Float);
float _Add_8395f84ea8e546988dd02e5b922d32ee_Out_2_Float;
Unity_Add_float(_OneMinus_de85154fa1b742e3b6c9e0f92b85d96c_Out_1_Float, _Saturate_77d41005224e417eb678b78b9a10771a_Out_1_Float, _Add_8395f84ea8e546988dd02e5b922d32ee_Out_2_Float);
float _Multiply_b61ead58dd024fd7aacad7c0b68f3be7_Out_2_Float;
Unity_Multiply_float_float(5, _Add_8395f84ea8e546988dd02e5b922d32ee_Out_2_Float, _Multiply_b61ead58dd024fd7aacad7c0b68f3be7_Out_2_Float);
float _Multiply_b0c3a49303544ccaa1d15379e6b1e9d2_Out_2_Float;
Unity_Multiply_float_float(_Power_b45fec9ffb074e40b5cb5452c0fb982d_Out_2_Float, _Multiply_b61ead58dd024fd7aacad7c0b68f3be7_Out_2_Float, _Multiply_b0c3a49303544ccaa1d15379e6b1e9d2_Out_2_Float);
float _Step_9e404f0c811346c88c531f42e623243f_Out_2_Float;
Unity_Step_float(_Subtract_fe60462bdbdd4a22a207774a6a8cb1b8_Out_2_Float, float(-0.1), _Step_9e404f0c811346c88c531f42e623243f_Out_2_Float);
float _Add_0328340190574386b2d37a0d035b66fb_Out_2_Float;
Unity_Add_float(_Multiply_b0c3a49303544ccaa1d15379e6b1e9d2_Out_2_Float, _Step_9e404f0c811346c88c531f42e623243f_Out_2_Float, _Add_0328340190574386b2d37a0d035b66fb_Out_2_Float);
float _Power_3bfc30934311465db3ba28e64b49097d_Out_2_Float;
Unity_Power_float(_Add_0328340190574386b2d37a0d035b66fb_Out_2_Float, float(3), _Power_3bfc30934311465db3ba28e64b49097d_Out_2_Float);
float _Saturate_3ba4a70af5214b4086803937a3f8b09f_Out_1_Float;
Unity_Saturate_float(_Power_3bfc30934311465db3ba28e64b49097d_Out_2_Float, _Saturate_3ba4a70af5214b4086803937a3f8b09f_Out_1_Float);
float _Saturate_6e423c9f5f1249088750a2b84a42b764_Out_1_Float;
Unity_Saturate_float(_Add_8395f84ea8e546988dd02e5b922d32ee_Out_2_Float, _Saturate_6e423c9f5f1249088750a2b84a42b764_Out_1_Float);
#if defined(_USE_COVERAGE_MASK_TEXTURE)
float _UseCoverageMaskTexture_f75c00eee68746bd80e2fa6c2bc28d4c_Out_0_Float = _Saturate_3ba4a70af5214b4086803937a3f8b09f_Out_1_Float;
#else
float _UseCoverageMaskTexture_f75c00eee68746bd80e2fa6c2bc28d4c_Out_0_Float = _Saturate_6e423c9f5f1249088750a2b84a42b764_Out_1_Float;
#endif
float4 _Lerp_c6c7a7472707418491f2fa10989f05b9_Out_3_Vector4;
Unity_Lerp_float4(_Multiply_e3ef32e4a4e8442188dfd3c93394960c_Out_2_Vector4, _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4, (_UseCoverageMaskTexture_f75c00eee68746bd80e2fa6c2bc28d4c_Out_0_Float.xxxx), _Lerp_c6c7a7472707418491f2fa10989f05b9_Out_3_Vector4);
#if defined(_USE_COVERAGE)
float4 _UseCoverage_c2f2c567080e4d5f9b652fbcbf667c54_Out_0_Vector4 = _Lerp_c6c7a7472707418491f2fa10989f05b9_Out_3_Vector4;
#else
float4 _UseCoverage_c2f2c567080e4d5f9b652fbcbf667c54_Out_0_Vector4 = _Multiply_e3ef32e4a4e8442188dfd3c93394960c_Out_2_Vector4;
#endif
UnityTexture2D _Property_c7dc38eaeea545aaada0e67cc32d9d25_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Emissive_Map, sampler_Emissive_Map, _Emissive_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_c7dc38eaeea545aaada0e67cc32d9d25_Out_0_Texture2D.tex, _Property_c7dc38eaeea545aaada0e67cc32d9d25_Out_0_Texture2D.samplerstate, _Property_c7dc38eaeea545aaada0e67cc32d9d25_Out_0_Texture2D.GetTransformedUV((_Divide_1ff147fe44c540028660935d59272eaa_Out_2_Vector4.xy)) );
if (_Property_c7dc38eaeea545aaada0e67cc32d9d25_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4, _Property_c7dc38eaeea545aaada0e67cc32d9d25_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_R_4_Float = _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4.r;
float _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_G_5_Float = _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4.g;
float _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_B_6_Float = _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4.b;
float _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_A_7_Float = _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4.a;
float4 _Property_857cbfa6469047f8904e0adc3eefe6b8_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_Emissive_Color) : _Emissive_Color;
float4 _Multiply_293fcf7127ab419cbcdff4183cccdba0_Out_2_Vector4;
Unity_Multiply_float4_float4(_SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4, _Property_857cbfa6469047f8904e0adc3eefe6b8_Out_0_Vector4, _Multiply_293fcf7127ab419cbcdff4183cccdba0_Out_2_Vector4);
#if defined(_USE_EMISSIVE)
float4 _UseEmissive_fdf874f7398e43a7838e4f04fd255375_Out_0_Vector4 = _Multiply_293fcf7127ab419cbcdff4183cccdba0_Out_2_Vector4;
#else
float4 _UseEmissive_fdf874f7398e43a7838e4f04fd255375_Out_0_Vector4 = float4(0, 0, 0, 0);
#endif
surface.BaseColor = (_UseCoverage_c2f2c567080e4d5f9b652fbcbf667c54_Out_0_Vector4.xyz);
surface.Emission = (_UseEmissive_fdf874f7398e43a7838e4f04fd255375_Out_0_Vector4.xyz);
float3 reactionUnusedColor = 0, reactionUnusedEmission = 0;
ClipVegetationFuel(TransformObjectToWorld(float3(0,0,0)));
ApplyVegetationReaction(TransformObjectToWorld(float3(0,0,0)),surface.BaseColor,surface.Emission);
return surface;
}

// --------------------------------------------------
// Build Graph Inputs
#ifdef HAVE_VFX_MODIFICATION
#define VFX_SRP_ATTRIBUTES Attributes
#define VFX_SRP_VARYINGS Varyings
#define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
#endif
VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

    output.ObjectSpaceNormal =                          input.normalOS;
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
    output.AbsoluteWorldSpacePosition =                 GetAbsolutePositionWS(TransformObjectToWorld(input.positionOS));
    output.TimeParameters =                             _TimeParameters.xyz;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

#ifdef HAVE_VFX_MODIFICATION
#if VFX_USE_GRAPH_VALUES
    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
#endif
    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */

#endif

    

    // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
    float3 unnormalizedNormalWS = input.normalWS;
    const float renormFactor = 1.0 / length(unnormalizedNormalWS);


    output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph


    output.ObjectSpacePosition = TransformWorldToObject(input.positionWS);

    #if UNITY_UV_STARTS_AT_TOP
    #else
    #endif


    output.uv0 = input.texCoord0;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

// --------------------------------------------------
// Main

#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/LightingMetaPass.hlsl"

// --------------------------------------------------
// Visual Effect Vertex Invocations
#ifdef HAVE_VFX_MODIFICATION
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
#endif

ENDHLSL
}
Pass
{
    Name "SceneSelectionPass"
    Tags
    {
        "LightMode" = "SceneSelectionPass"
    }

// Render State
Cull Off

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 2.0
#pragma vertex vert
#pragma fragment frag

// Keywords
// PassKeywords: <None>
#pragma shader_feature_local _ _USE_EMISSIVE
#pragma shader_feature_local _ _USE_WPO
#pragma shader_feature_local _ _USE_ORM
#pragma shader_feature_local _ _USE_NORMAL_MAPS
#pragma shader_feature_local _ _USE_COVERAGE
#pragma shader_feature_local _ _USE_COVERAGE_MASK_TEXTURE

#if defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_2
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_3
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_4
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_5
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_6
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM)
#define KEYWORD_PERMUTATION_7
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_8
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_9
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_10
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_11
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_12
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_13
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_14
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO)
#define KEYWORD_PERMUTATION_15
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_16
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_17
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_18
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_19
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_20
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_21
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_22
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM)
#define KEYWORD_PERMUTATION_23
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_24
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_25
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_26
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_27
#elif defined(_USE_EMISSIVE) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_28
#elif defined(_USE_EMISSIVE) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_29
#elif defined(_USE_EMISSIVE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_30
#elif defined(_USE_EMISSIVE)
#define KEYWORD_PERMUTATION_31
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_32
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_33
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_34
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_35
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_36
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_37
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_38
#elif defined(_USE_WPO) && defined(_USE_ORM)
#define KEYWORD_PERMUTATION_39
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_40
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_41
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_42
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_43
#elif defined(_USE_WPO) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_44
#elif defined(_USE_WPO) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_45
#elif defined(_USE_WPO) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_46
#elif defined(_USE_WPO)
#define KEYWORD_PERMUTATION_47
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_48
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_49
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_50
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_51
#elif defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_52
#elif defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_53
#elif defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_54
#elif defined(_USE_ORM)
#define KEYWORD_PERMUTATION_55
#elif defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_56
#elif defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_57
#elif defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_58
#elif defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_59
#elif defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_60
#elif defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_61
#elif defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_62
#else
#define KEYWORD_PERMUTATION_63
#endif


// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_DEPTHONLY
#define SCENESELECTIONPASS 1
#define ALPHA_CLIP_THRESHOLD 1


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
 float3 positionOS : POSITION;
 float3 normalOS : NORMAL;
 float4 tangentOS : TANGENT;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
 float4 positionCS : SV_POSITION;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};
struct SurfaceDescriptionInputs
{
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 AbsoluteWorldSpacePosition;
 float3 TimeParameters;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};

PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.positionCS = input.positionCS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.positionCS = input.positionCS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}


// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)

float _EnvironmentReactionSupported;
float4 _EnvState0, _EnvState1;
float _FuelCoverage, _AllowFuelCoverage;
float4 _WetTint, _CharTint, _FireColor;
float4 _EnvironmentCellBounds;
float _UseEnvironmentCells;

float _Height_from_Ground_Randomness;
float _Height_from_Ground_Softness;
float _Normal_Pow;
float _Normal_Mul;
float _Mask_Map_Size;
float4 _Coverage_Normal_Map_TexelSize;
float _Mask_Map_Intensity;
float4 _Coverage_ORM_Map_TexelSize;
float _Height_Weight;
float _Height_Gradience;
float _Wind_Intensity;
float3 _Wind_Direction;
float _Wind_Speed;
float _Wind_Time_Random_Offset_Amount;
float _Texture_Size;
float4 _Color_Map_TexelSize;
float4 _Color_Tint;
float4 _Normal_Map_TexelSize;
float _Normal_Intensity;
float4 _ORM_Map_TexelSize;
float _Roughness_Intensity;
float _AO_Intensity;
float _Metallic_Intensity;
float4 _Emissive_Map_TexelSize;
float4 _Emissive_Color;
float4 _Coverage_Color_Map_TexelSize;
float _Coverage_Size;
float _Height_from_Ground;
float4 _Mask_Map_TexelSize;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Coverage_Normal_Map);
SAMPLER(sampler_Coverage_Normal_Map);
TEXTURE2D(_Coverage_ORM_Map);
SAMPLER(sampler_Coverage_ORM_Map);
TEXTURE2D(_Color_Map);
SAMPLER(sampler_Color_Map);
TEXTURE2D(_Normal_Map);
SAMPLER(sampler_Normal_Map);
TEXTURE2D(_ORM_Map);
SAMPLER(sampler_ORM_Map);
TEXTURE2D(_Emissive_Map);
SAMPLER(sampler_Emissive_Map);
TEXTURE2D(_Coverage_Color_Map);
SAMPLER(sampler_Coverage_Color_Map);
TEXTURE2D(_Mask_Map);
SAMPLER(sampler_Mask_Map);

// Graph Includes
// GraphIncludes: <None>

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Functions

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Sine_float(float In, out float Out)
{
    Out = sin(In);
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Length_float3(float3 In, out float Out)
{
    Out = length(In);
}

void Unity_Divide_float(float A, float B, out float Out)
{
    Out = A / B;
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
{
    Out = lerp(A, B, T);
}

struct Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float
{
float3 ObjectSpacePosition;
float3 TimeParameters;
};

void SG_WindSway_51c4027ae50a8cd49be53c4990b9f179_float(float _Sway_Wind_Speed, float3 _Sway_Wind_Direction, float _Sway_Wind_Intensity, float _Sway_Wind_Time_Random_Offset_Amount, Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float IN, out float3 OutVector3_1)
{
float _Property_5c4cd19861f64014ae8e16af9365ef49_Out_0_Float = _Sway_Wind_Time_Random_Offset_Amount;
float _Add_4b31033339dc4f8e8e71674d98e662a3_Out_2_Float;
Unity_Add_float(IN.TimeParameters.x, _Property_5c4cd19861f64014ae8e16af9365ef49_Out_0_Float, _Add_4b31033339dc4f8e8e71674d98e662a3_Out_2_Float);
float _Property_9a93fa589ac7409884a94247349a940f_Out_0_Float = _Sway_Wind_Speed;
float _Multiply_7418655003ae4fedb2bf68678629472c_Out_2_Float;
Unity_Multiply_float_float(_Add_4b31033339dc4f8e8e71674d98e662a3_Out_2_Float, _Property_9a93fa589ac7409884a94247349a940f_Out_0_Float, _Multiply_7418655003ae4fedb2bf68678629472c_Out_2_Float);
float _Sine_ebd14b0fc42b4b4cb4387f3a1b879c1f_Out_1_Float;
Unity_Sine_float(_Multiply_7418655003ae4fedb2bf68678629472c_Out_2_Float, _Sine_ebd14b0fc42b4b4cb4387f3a1b879c1f_Out_1_Float);
float _Add_660e92170c564f94a79277cd2fd0cf20_Out_2_Float;
Unity_Add_float(_Sine_ebd14b0fc42b4b4cb4387f3a1b879c1f_Out_1_Float, float(1), _Add_660e92170c564f94a79277cd2fd0cf20_Out_2_Float);
float _Multiply_b8fd654b771447aebafc1a6aad6957cf_Out_2_Float;
Unity_Multiply_float_float(_Add_660e92170c564f94a79277cd2fd0cf20_Out_2_Float, 0.5, _Multiply_b8fd654b771447aebafc1a6aad6957cf_Out_2_Float);
float3 _Property_fa2ee033b44946c59c3fa9aea4a167c8_Out_0_Vector3 = _Sway_Wind_Direction;
float3 _Multiply_a5500b7f7b8e43988a055eea7eb917f3_Out_2_Vector3;
Unity_Multiply_float3_float3((_Multiply_b8fd654b771447aebafc1a6aad6957cf_Out_2_Float.xxx), _Property_fa2ee033b44946c59c3fa9aea4a167c8_Out_0_Vector3, _Multiply_a5500b7f7b8e43988a055eea7eb917f3_Out_2_Vector3);
float _Property_66cba0a3223546238e18b58e017e756f_Out_0_Float = _Sway_Wind_Intensity;
float3 _Multiply_475f81423e39483cba045bef08b9f967_Out_2_Vector3;
Unity_Multiply_float3_float3(_Multiply_a5500b7f7b8e43988a055eea7eb917f3_Out_2_Vector3, (_Property_66cba0a3223546238e18b58e017e756f_Out_0_Float.xxx), _Multiply_475f81423e39483cba045bef08b9f967_Out_2_Vector3);
float3 _Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3;
Unity_Add_float3(_Multiply_475f81423e39483cba045bef08b9f967_Out_2_Vector3, IN.ObjectSpacePosition, _Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3);
float _Length_53965ffb38de43359ab019a247194557_Out_1_Float;
Unity_Length_float3(IN.ObjectSpacePosition, _Length_53965ffb38de43359ab019a247194557_Out_1_Float);
float _Length_77b6b3d742104f2abf3c6968dea9a342_Out_1_Float;
Unity_Length_float3(_Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3, _Length_77b6b3d742104f2abf3c6968dea9a342_Out_1_Float);
float _Divide_f5dd43de79e343638d2743ee93ce7326_Out_2_Float;
Unity_Divide_float(_Length_53965ffb38de43359ab019a247194557_Out_1_Float, _Length_77b6b3d742104f2abf3c6968dea9a342_Out_1_Float, _Divide_f5dd43de79e343638d2743ee93ce7326_Out_2_Float);
float3 _Lerp_7574daf550e1428cb9d35656c7bb9603_Out_3_Vector3;
Unity_Lerp_float3(float3(0, 0, 0), _Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3, (_Divide_f5dd43de79e343638d2743ee93ce7326_Out_2_Float.xxx), _Lerp_7574daf550e1428cb9d35656c7bb9603_Out_3_Vector3);
OutVector3_1 = _Lerp_7574daf550e1428cb9d35656c7bb9603_Out_3_Vector3;
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

void Unity_Power_float(float A, float B, out float Out)
{
    Out = pow(A, B);
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 Position;
float3 Normal;
float3 Tangent;
};

VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
float _Property_b2e048f6276d4371b6ad931f5d1baa87_Out_0_Float = _Wind_Speed;
float3 _Property_7133a0551bc7429cb8bbfcbad044bf97_Out_0_Vector3 = _Wind_Direction;
float _Property_5aa2352867cd4da79dc9f9f9979925c7_Out_0_Float = _Wind_Intensity;
float _Property_e0c6ebff99e349d6966115347961a70c_Out_0_Float = _Wind_Time_Random_Offset_Amount;
Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf;
_WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf.ObjectSpacePosition = IN.ObjectSpacePosition;
_WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf.TimeParameters = IN.TimeParameters;
float3 _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf_OutVector3_1_Vector3;
SG_WindSway_51c4027ae50a8cd49be53c4990b9f179_float(_Property_b2e048f6276d4371b6ad931f5d1baa87_Out_0_Float, _Property_7133a0551bc7429cb8bbfcbad044bf97_Out_0_Vector3, _Property_5aa2352867cd4da79dc9f9f9979925c7_Out_0_Float, _Property_e0c6ebff99e349d6966115347961a70c_Out_0_Float, _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf, _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf_OutVector3_1_Vector3);
float _Split_38420bd2034746a1878048f0ead2a7b2_R_1_Float = IN.AbsoluteWorldSpacePosition[0];
float _Split_38420bd2034746a1878048f0ead2a7b2_G_2_Float = IN.AbsoluteWorldSpacePosition[1];
float _Split_38420bd2034746a1878048f0ead2a7b2_B_3_Float = IN.AbsoluteWorldSpacePosition[2];
float _Split_38420bd2034746a1878048f0ead2a7b2_A_4_Float = 0;
float3 _Vector3_0f8475b3a0df451ea79f6ea2e4e3f673_Out_0_Vector3 = float3(float(0), float(0), float(0));
float3 _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3;
{
// Converting Position from Object to AbsoluteWorld via world space
float3 world;
world = TransformObjectToWorld(_Vector3_0f8475b3a0df451ea79f6ea2e4e3f673_Out_0_Vector3.xyz);
_Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3 = GetAbsolutePositionWS(world);
}
float _Split_fb1b2d84926449e498da6c90f1436311_R_1_Float = _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3[0];
float _Split_fb1b2d84926449e498da6c90f1436311_G_2_Float = _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3[1];
float _Split_fb1b2d84926449e498da6c90f1436311_B_3_Float = _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3[2];
float _Split_fb1b2d84926449e498da6c90f1436311_A_4_Float = 0;
float _Subtract_18ddaa9851a547f1bc3eeafa55f3227b_Out_2_Float;
Unity_Subtract_float(_Split_38420bd2034746a1878048f0ead2a7b2_G_2_Float, _Split_fb1b2d84926449e498da6c90f1436311_G_2_Float, _Subtract_18ddaa9851a547f1bc3eeafa55f3227b_Out_2_Float);
float _Property_1db6873c977b47f186e42eef2cd75a57_Out_0_Float = _Height_Weight;
float _Split_bc5987163dba418d8de90e985de1247b_R_1_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[0];
float _Split_bc5987163dba418d8de90e985de1247b_G_2_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[1];
float _Split_bc5987163dba418d8de90e985de1247b_B_3_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[2];
float _Split_bc5987163dba418d8de90e985de1247b_A_4_Float = 0;
float _Multiply_89de5a5fb3cd451abb1227deba019c38_Out_2_Float;
Unity_Multiply_float_float(_Property_1db6873c977b47f186e42eef2cd75a57_Out_0_Float, _Split_bc5987163dba418d8de90e985de1247b_G_2_Float, _Multiply_89de5a5fb3cd451abb1227deba019c38_Out_2_Float);
float _Divide_2c406de6c63e463f9013e4faf1db1ae8_Out_2_Float;
Unity_Divide_float(_Subtract_18ddaa9851a547f1bc3eeafa55f3227b_Out_2_Float, _Multiply_89de5a5fb3cd451abb1227deba019c38_Out_2_Float, _Divide_2c406de6c63e463f9013e4faf1db1ae8_Out_2_Float);
float _Saturate_e9ea645ca89f482a9d0315df54a77ee5_Out_1_Float;
Unity_Saturate_float(_Divide_2c406de6c63e463f9013e4faf1db1ae8_Out_2_Float, _Saturate_e9ea645ca89f482a9d0315df54a77ee5_Out_1_Float);
float _Property_e96fd1920dff4533951da684129df200_Out_0_Float = _Height_Gradience;
float _Power_b88c5abc85424f17b03c60c98e9bc16c_Out_2_Float;
Unity_Power_float(_Saturate_e9ea645ca89f482a9d0315df54a77ee5_Out_1_Float, _Property_e96fd1920dff4533951da684129df200_Out_0_Float, _Power_b88c5abc85424f17b03c60c98e9bc16c_Out_2_Float);
float _Saturate_6c35d0fa108b483783bde88e06ff5ce0_Out_1_Float;
Unity_Saturate_float(_Power_b88c5abc85424f17b03c60c98e9bc16c_Out_2_Float, _Saturate_6c35d0fa108b483783bde88e06ff5ce0_Out_1_Float);
float3 _Lerp_8461a67d418941ee81ed9eb0f80e8a38_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf_OutVector3_1_Vector3, (_Saturate_6c35d0fa108b483783bde88e06ff5ce0_Out_1_Float.xxx), _Lerp_8461a67d418941ee81ed9eb0f80e8a38_Out_3_Vector3);
#if defined(_USE_WPO)
float3 _UseWPO_81cb92bff33547fbbfaa5ce2f60ba97c_Out_0_Vector3 = _Lerp_8461a67d418941ee81ed9eb0f80e8a38_Out_3_Vector3;
#else
float3 _UseWPO_81cb92bff33547fbbfaa5ce2f60ba97c_Out_0_Vector3 = IN.ObjectSpacePosition;
#endif
description.Position = _UseWPO_81cb92bff33547fbbfaa5ce2f60ba97c_Out_0_Vector3;
description.Normal = IN.ObjectSpaceNormal;
description.Tangent = IN.ObjectSpaceTangent;
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
float3 reactionUnusedColor = 0, reactionUnusedEmission = 0;
ClipVegetationFuel(TransformObjectToWorld(float3(0,0,0)));
return surface;
}

// --------------------------------------------------
// Build Graph Inputs
#ifdef HAVE_VFX_MODIFICATION
#define VFX_SRP_ATTRIBUTES Attributes
#define VFX_SRP_VARYINGS Varyings
#define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
#endif
VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

    output.ObjectSpaceNormal =                          input.normalOS;
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
    output.AbsoluteWorldSpacePosition =                 GetAbsolutePositionWS(TransformObjectToWorld(input.positionOS));
    output.TimeParameters =                             _TimeParameters.xyz;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

#ifdef HAVE_VFX_MODIFICATION
#if VFX_USE_GRAPH_VALUES
    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
#endif
    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */

#endif

    






    #if UNITY_UV_STARTS_AT_TOP
    #else
    #endif


#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

// --------------------------------------------------
// Main

#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SelectionPickingPass.hlsl"

// --------------------------------------------------
// Visual Effect Vertex Invocations
#ifdef HAVE_VFX_MODIFICATION
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
#endif

ENDHLSL
}
Pass
{
    Name "ScenePickingPass"
    Tags
    {
        "LightMode" = "Picking"
    }

// Render State
Cull Back

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 2.0
#pragma vertex vert
#pragma fragment frag

// Keywords
// PassKeywords: <None>
#pragma shader_feature_local _ _USE_EMISSIVE
#pragma shader_feature_local _ _USE_WPO
#pragma shader_feature_local _ _USE_ORM
#pragma shader_feature_local _ _USE_NORMAL_MAPS
#pragma shader_feature_local _ _USE_COVERAGE
#pragma shader_feature_local _ _USE_COVERAGE_MASK_TEXTURE

#if defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_2
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_3
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_4
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_5
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_6
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM)
#define KEYWORD_PERMUTATION_7
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_8
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_9
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_10
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_11
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_12
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_13
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_14
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO)
#define KEYWORD_PERMUTATION_15
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_16
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_17
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_18
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_19
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_20
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_21
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_22
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM)
#define KEYWORD_PERMUTATION_23
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_24
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_25
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_26
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_27
#elif defined(_USE_EMISSIVE) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_28
#elif defined(_USE_EMISSIVE) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_29
#elif defined(_USE_EMISSIVE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_30
#elif defined(_USE_EMISSIVE)
#define KEYWORD_PERMUTATION_31
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_32
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_33
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_34
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_35
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_36
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_37
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_38
#elif defined(_USE_WPO) && defined(_USE_ORM)
#define KEYWORD_PERMUTATION_39
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_40
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_41
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_42
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_43
#elif defined(_USE_WPO) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_44
#elif defined(_USE_WPO) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_45
#elif defined(_USE_WPO) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_46
#elif defined(_USE_WPO)
#define KEYWORD_PERMUTATION_47
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_48
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_49
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_50
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_51
#elif defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_52
#elif defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_53
#elif defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_54
#elif defined(_USE_ORM)
#define KEYWORD_PERMUTATION_55
#elif defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_56
#elif defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_57
#elif defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_58
#elif defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_59
#elif defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_60
#elif defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_61
#elif defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_62
#else
#define KEYWORD_PERMUTATION_63
#endif


// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_NORMAL_WS
#define VARYINGS_NEED_TEXCOORD0
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_DEPTHONLY
#define SCENEPICKINGPASS 1
#define ALPHA_CLIP_THRESHOLD 1


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
 float3 positionOS : POSITION;
 float3 normalOS : NORMAL;
 float4 tangentOS : TANGENT;
 float4 uv0 : TEXCOORD0;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
 float4 positionCS : SV_POSITION;
 float3 positionWS;
 float3 normalWS;
 float4 texCoord0;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};
struct SurfaceDescriptionInputs
{
 float3 WorldSpaceNormal;
 float3 ObjectSpacePosition;
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 AbsoluteWorldSpacePosition;
 float3 TimeParameters;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
 float4 texCoord0 : INTERP0;
 float3 positionWS : INTERP1;
 float3 normalWS : INTERP2;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};

PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.positionCS = input.positionCS;
output.texCoord0.xyzw = input.texCoord0;
output.positionWS.xyz = input.positionWS;
output.normalWS.xyz = input.normalWS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.positionCS = input.positionCS;
output.texCoord0 = input.texCoord0.xyzw;
output.positionWS = input.positionWS.xyz;
output.normalWS = input.normalWS.xyz;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}


// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)

float _EnvironmentReactionSupported;
float4 _EnvState0, _EnvState1;
float _FuelCoverage, _AllowFuelCoverage;
float4 _WetTint, _CharTint, _FireColor;
float4 _EnvironmentCellBounds;
float _UseEnvironmentCells;

float _Height_from_Ground_Randomness;
float _Height_from_Ground_Softness;
float _Normal_Pow;
float _Normal_Mul;
float _Mask_Map_Size;
float4 _Coverage_Normal_Map_TexelSize;
float _Mask_Map_Intensity;
float4 _Coverage_ORM_Map_TexelSize;
float _Height_Weight;
float _Height_Gradience;
float _Wind_Intensity;
float3 _Wind_Direction;
float _Wind_Speed;
float _Wind_Time_Random_Offset_Amount;
float _Texture_Size;
float4 _Color_Map_TexelSize;
float4 _Color_Tint;
float4 _Normal_Map_TexelSize;
float _Normal_Intensity;
float4 _ORM_Map_TexelSize;
float _Roughness_Intensity;
float _AO_Intensity;
float _Metallic_Intensity;
float4 _Emissive_Map_TexelSize;
float4 _Emissive_Color;
float4 _Coverage_Color_Map_TexelSize;
float _Coverage_Size;
float _Height_from_Ground;
float4 _Mask_Map_TexelSize;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Coverage_Normal_Map);
SAMPLER(sampler_Coverage_Normal_Map);
TEXTURE2D(_Coverage_ORM_Map);
SAMPLER(sampler_Coverage_ORM_Map);
TEXTURE2D(_Color_Map);
SAMPLER(sampler_Color_Map);
TEXTURE2D(_Normal_Map);
SAMPLER(sampler_Normal_Map);
TEXTURE2D(_ORM_Map);
SAMPLER(sampler_ORM_Map);
TEXTURE2D(_Emissive_Map);
SAMPLER(sampler_Emissive_Map);
TEXTURE2D(_Coverage_Color_Map);
SAMPLER(sampler_Coverage_Color_Map);
TEXTURE2D(_Mask_Map);
SAMPLER(sampler_Mask_Map);

// Graph Includes
// GraphIncludes: <None>

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Functions

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Sine_float(float In, out float Out)
{
    Out = sin(In);
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Length_float3(float3 In, out float Out)
{
    Out = length(In);
}

void Unity_Divide_float(float A, float B, out float Out)
{
    Out = A / B;
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
{
    Out = lerp(A, B, T);
}

struct Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float
{
float3 ObjectSpacePosition;
float3 TimeParameters;
};

void SG_WindSway_51c4027ae50a8cd49be53c4990b9f179_float(float _Sway_Wind_Speed, float3 _Sway_Wind_Direction, float _Sway_Wind_Intensity, float _Sway_Wind_Time_Random_Offset_Amount, Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float IN, out float3 OutVector3_1)
{
float _Property_5c4cd19861f64014ae8e16af9365ef49_Out_0_Float = _Sway_Wind_Time_Random_Offset_Amount;
float _Add_4b31033339dc4f8e8e71674d98e662a3_Out_2_Float;
Unity_Add_float(IN.TimeParameters.x, _Property_5c4cd19861f64014ae8e16af9365ef49_Out_0_Float, _Add_4b31033339dc4f8e8e71674d98e662a3_Out_2_Float);
float _Property_9a93fa589ac7409884a94247349a940f_Out_0_Float = _Sway_Wind_Speed;
float _Multiply_7418655003ae4fedb2bf68678629472c_Out_2_Float;
Unity_Multiply_float_float(_Add_4b31033339dc4f8e8e71674d98e662a3_Out_2_Float, _Property_9a93fa589ac7409884a94247349a940f_Out_0_Float, _Multiply_7418655003ae4fedb2bf68678629472c_Out_2_Float);
float _Sine_ebd14b0fc42b4b4cb4387f3a1b879c1f_Out_1_Float;
Unity_Sine_float(_Multiply_7418655003ae4fedb2bf68678629472c_Out_2_Float, _Sine_ebd14b0fc42b4b4cb4387f3a1b879c1f_Out_1_Float);
float _Add_660e92170c564f94a79277cd2fd0cf20_Out_2_Float;
Unity_Add_float(_Sine_ebd14b0fc42b4b4cb4387f3a1b879c1f_Out_1_Float, float(1), _Add_660e92170c564f94a79277cd2fd0cf20_Out_2_Float);
float _Multiply_b8fd654b771447aebafc1a6aad6957cf_Out_2_Float;
Unity_Multiply_float_float(_Add_660e92170c564f94a79277cd2fd0cf20_Out_2_Float, 0.5, _Multiply_b8fd654b771447aebafc1a6aad6957cf_Out_2_Float);
float3 _Property_fa2ee033b44946c59c3fa9aea4a167c8_Out_0_Vector3 = _Sway_Wind_Direction;
float3 _Multiply_a5500b7f7b8e43988a055eea7eb917f3_Out_2_Vector3;
Unity_Multiply_float3_float3((_Multiply_b8fd654b771447aebafc1a6aad6957cf_Out_2_Float.xxx), _Property_fa2ee033b44946c59c3fa9aea4a167c8_Out_0_Vector3, _Multiply_a5500b7f7b8e43988a055eea7eb917f3_Out_2_Vector3);
float _Property_66cba0a3223546238e18b58e017e756f_Out_0_Float = _Sway_Wind_Intensity;
float3 _Multiply_475f81423e39483cba045bef08b9f967_Out_2_Vector3;
Unity_Multiply_float3_float3(_Multiply_a5500b7f7b8e43988a055eea7eb917f3_Out_2_Vector3, (_Property_66cba0a3223546238e18b58e017e756f_Out_0_Float.xxx), _Multiply_475f81423e39483cba045bef08b9f967_Out_2_Vector3);
float3 _Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3;
Unity_Add_float3(_Multiply_475f81423e39483cba045bef08b9f967_Out_2_Vector3, IN.ObjectSpacePosition, _Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3);
float _Length_53965ffb38de43359ab019a247194557_Out_1_Float;
Unity_Length_float3(IN.ObjectSpacePosition, _Length_53965ffb38de43359ab019a247194557_Out_1_Float);
float _Length_77b6b3d742104f2abf3c6968dea9a342_Out_1_Float;
Unity_Length_float3(_Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3, _Length_77b6b3d742104f2abf3c6968dea9a342_Out_1_Float);
float _Divide_f5dd43de79e343638d2743ee93ce7326_Out_2_Float;
Unity_Divide_float(_Length_53965ffb38de43359ab019a247194557_Out_1_Float, _Length_77b6b3d742104f2abf3c6968dea9a342_Out_1_Float, _Divide_f5dd43de79e343638d2743ee93ce7326_Out_2_Float);
float3 _Lerp_7574daf550e1428cb9d35656c7bb9603_Out_3_Vector3;
Unity_Lerp_float3(float3(0, 0, 0), _Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3, (_Divide_f5dd43de79e343638d2743ee93ce7326_Out_2_Float.xxx), _Lerp_7574daf550e1428cb9d35656c7bb9603_Out_3_Vector3);
OutVector3_1 = _Lerp_7574daf550e1428cb9d35656c7bb9603_Out_3_Vector3;
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

void Unity_Power_float(float A, float B, out float Out)
{
    Out = pow(A, B);
}

void Unity_Divide_float4(float4 A, float4 B, out float4 Out)
{
    Out = A / B;
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

void Unity_Normalize_float3(float3 In, out float3 Out)
{
    Out = normalize(In);
}

void Unity_RandomRange_float(float2 Seed, float Min, float Max, out float Out)
{
     float randomno =  frac(sin(dot(Seed, float2(12.9898, 78.233)))*43758.5453);
     Out = lerp(Min, Max, randomno);
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
}

void Unity_DotProduct_float3(float3 A, float3 B, out float Out)
{
    Out = dot(A, B);
}

void Unity_Step_float(float Edge, float In, out float Out)
{
    Out = step(Edge, In);
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 Position;
float3 Normal;
float3 Tangent;
};

VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
float _Property_b2e048f6276d4371b6ad931f5d1baa87_Out_0_Float = _Wind_Speed;
float3 _Property_7133a0551bc7429cb8bbfcbad044bf97_Out_0_Vector3 = _Wind_Direction;
float _Property_5aa2352867cd4da79dc9f9f9979925c7_Out_0_Float = _Wind_Intensity;
float _Property_e0c6ebff99e349d6966115347961a70c_Out_0_Float = _Wind_Time_Random_Offset_Amount;
Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf;
_WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf.ObjectSpacePosition = IN.ObjectSpacePosition;
_WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf.TimeParameters = IN.TimeParameters;
float3 _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf_OutVector3_1_Vector3;
SG_WindSway_51c4027ae50a8cd49be53c4990b9f179_float(_Property_b2e048f6276d4371b6ad931f5d1baa87_Out_0_Float, _Property_7133a0551bc7429cb8bbfcbad044bf97_Out_0_Vector3, _Property_5aa2352867cd4da79dc9f9f9979925c7_Out_0_Float, _Property_e0c6ebff99e349d6966115347961a70c_Out_0_Float, _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf, _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf_OutVector3_1_Vector3);
float _Split_38420bd2034746a1878048f0ead2a7b2_R_1_Float = IN.AbsoluteWorldSpacePosition[0];
float _Split_38420bd2034746a1878048f0ead2a7b2_G_2_Float = IN.AbsoluteWorldSpacePosition[1];
float _Split_38420bd2034746a1878048f0ead2a7b2_B_3_Float = IN.AbsoluteWorldSpacePosition[2];
float _Split_38420bd2034746a1878048f0ead2a7b2_A_4_Float = 0;
float3 _Vector3_0f8475b3a0df451ea79f6ea2e4e3f673_Out_0_Vector3 = float3(float(0), float(0), float(0));
float3 _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3;
{
// Converting Position from Object to AbsoluteWorld via world space
float3 world;
world = TransformObjectToWorld(_Vector3_0f8475b3a0df451ea79f6ea2e4e3f673_Out_0_Vector3.xyz);
_Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3 = GetAbsolutePositionWS(world);
}
float _Split_fb1b2d84926449e498da6c90f1436311_R_1_Float = _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3[0];
float _Split_fb1b2d84926449e498da6c90f1436311_G_2_Float = _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3[1];
float _Split_fb1b2d84926449e498da6c90f1436311_B_3_Float = _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3[2];
float _Split_fb1b2d84926449e498da6c90f1436311_A_4_Float = 0;
float _Subtract_18ddaa9851a547f1bc3eeafa55f3227b_Out_2_Float;
Unity_Subtract_float(_Split_38420bd2034746a1878048f0ead2a7b2_G_2_Float, _Split_fb1b2d84926449e498da6c90f1436311_G_2_Float, _Subtract_18ddaa9851a547f1bc3eeafa55f3227b_Out_2_Float);
float _Property_1db6873c977b47f186e42eef2cd75a57_Out_0_Float = _Height_Weight;
float _Split_bc5987163dba418d8de90e985de1247b_R_1_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[0];
float _Split_bc5987163dba418d8de90e985de1247b_G_2_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[1];
float _Split_bc5987163dba418d8de90e985de1247b_B_3_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[2];
float _Split_bc5987163dba418d8de90e985de1247b_A_4_Float = 0;
float _Multiply_89de5a5fb3cd451abb1227deba019c38_Out_2_Float;
Unity_Multiply_float_float(_Property_1db6873c977b47f186e42eef2cd75a57_Out_0_Float, _Split_bc5987163dba418d8de90e985de1247b_G_2_Float, _Multiply_89de5a5fb3cd451abb1227deba019c38_Out_2_Float);
float _Divide_2c406de6c63e463f9013e4faf1db1ae8_Out_2_Float;
Unity_Divide_float(_Subtract_18ddaa9851a547f1bc3eeafa55f3227b_Out_2_Float, _Multiply_89de5a5fb3cd451abb1227deba019c38_Out_2_Float, _Divide_2c406de6c63e463f9013e4faf1db1ae8_Out_2_Float);
float _Saturate_e9ea645ca89f482a9d0315df54a77ee5_Out_1_Float;
Unity_Saturate_float(_Divide_2c406de6c63e463f9013e4faf1db1ae8_Out_2_Float, _Saturate_e9ea645ca89f482a9d0315df54a77ee5_Out_1_Float);
float _Property_e96fd1920dff4533951da684129df200_Out_0_Float = _Height_Gradience;
float _Power_b88c5abc85424f17b03c60c98e9bc16c_Out_2_Float;
Unity_Power_float(_Saturate_e9ea645ca89f482a9d0315df54a77ee5_Out_1_Float, _Property_e96fd1920dff4533951da684129df200_Out_0_Float, _Power_b88c5abc85424f17b03c60c98e9bc16c_Out_2_Float);
float _Saturate_6c35d0fa108b483783bde88e06ff5ce0_Out_1_Float;
Unity_Saturate_float(_Power_b88c5abc85424f17b03c60c98e9bc16c_Out_2_Float, _Saturate_6c35d0fa108b483783bde88e06ff5ce0_Out_1_Float);
float3 _Lerp_8461a67d418941ee81ed9eb0f80e8a38_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf_OutVector3_1_Vector3, (_Saturate_6c35d0fa108b483783bde88e06ff5ce0_Out_1_Float.xxx), _Lerp_8461a67d418941ee81ed9eb0f80e8a38_Out_3_Vector3);
#if defined(_USE_WPO)
float3 _UseWPO_81cb92bff33547fbbfaa5ce2f60ba97c_Out_0_Vector3 = _Lerp_8461a67d418941ee81ed9eb0f80e8a38_Out_3_Vector3;
#else
float3 _UseWPO_81cb92bff33547fbbfaa5ce2f60ba97c_Out_0_Vector3 = IN.ObjectSpacePosition;
#endif
description.Position = _UseWPO_81cb92bff33547fbbfaa5ce2f60ba97c_Out_0_Vector3;
description.Normal = IN.ObjectSpaceNormal;
description.Tangent = IN.ObjectSpaceTangent;
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
float3 BaseColor;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
UnityTexture2D _Property_0c79649b4042462682b4a7423715f1a1_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Color_Map, sampler_Color_Map, _Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_a9551365617a4a66b75c6a49ab8f9c5f_Out_0_Vector4 = IN.uv0;
float _Property_ac4a3a6f20bd42789825abf989f1a228_Out_0_Float = _Texture_Size;
float4 _Divide_1ff147fe44c540028660935d59272eaa_Out_2_Vector4;
Unity_Divide_float4(_UV_a9551365617a4a66b75c6a49ab8f9c5f_Out_0_Vector4, (_Property_ac4a3a6f20bd42789825abf989f1a228_Out_0_Float.xxxx), _Divide_1ff147fe44c540028660935d59272eaa_Out_2_Vector4);
float4 _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_0c79649b4042462682b4a7423715f1a1_Out_0_Texture2D.tex, _Property_0c79649b4042462682b4a7423715f1a1_Out_0_Texture2D.samplerstate, _Property_0c79649b4042462682b4a7423715f1a1_Out_0_Texture2D.GetTransformedUV((_Divide_1ff147fe44c540028660935d59272eaa_Out_2_Vector4.xy)) );
if (_Property_0c79649b4042462682b4a7423715f1a1_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4, _Property_0c79649b4042462682b4a7423715f1a1_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_R_4_Float = _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4.r;
float _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_G_5_Float = _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4.g;
float _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_B_6_Float = _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4.b;
float _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_A_7_Float = _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4.a;
float4 _Property_11be52d8078b41908ba72022455e8b98_Out_0_Vector4 = _Color_Tint;
float4 _Multiply_e3ef32e4a4e8442188dfd3c93394960c_Out_2_Vector4;
Unity_Multiply_float4_float4(_SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4, _Property_11be52d8078b41908ba72022455e8b98_Out_0_Vector4, _Multiply_e3ef32e4a4e8442188dfd3c93394960c_Out_2_Vector4);
UnityTexture2D _Property_415df38a141841e6aa11a7b1e336c379_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Coverage_Color_Map, sampler_Coverage_Color_Map, _Coverage_Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_0805a5601f5b49beb942e50ab3714e03_Out_0_Vector4 = IN.uv0;
float _Property_eb7418710f3f4dc6981c01445b1a809e_Out_0_Float = _Coverage_Size;
float4 _Divide_71dd940938164d5a9e7020e44b1cddf3_Out_2_Vector4;
Unity_Divide_float4(_UV_0805a5601f5b49beb942e50ab3714e03_Out_0_Vector4, (_Property_eb7418710f3f4dc6981c01445b1a809e_Out_0_Float.xxxx), _Divide_71dd940938164d5a9e7020e44b1cddf3_Out_2_Vector4);
float4 _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_415df38a141841e6aa11a7b1e336c379_Out_0_Texture2D.tex, _Property_415df38a141841e6aa11a7b1e336c379_Out_0_Texture2D.samplerstate, _Property_415df38a141841e6aa11a7b1e336c379_Out_0_Texture2D.GetTransformedUV((_Divide_71dd940938164d5a9e7020e44b1cddf3_Out_2_Vector4.xy)) );
if (_Property_415df38a141841e6aa11a7b1e336c379_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4, _Property_415df38a141841e6aa11a7b1e336c379_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_R_4_Float = _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4.r;
float _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_G_5_Float = _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4.g;
float _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_B_6_Float = _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4.b;
float _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_A_7_Float = _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4.a;
UnityTexture2D _Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Mask_Map, sampler_Mask_Map, _Mask_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_c2c5030522744aceb603ff104c9ccde0_Out_0_Vector4 = IN.uv0;
float _Property_a975da4683af46ab85b4a95b01cb9f32_Out_0_Float = _Mask_Map_Size;
float4 _Divide_3dd829d125dc4ec1b32772766d56a17b_Out_2_Vector4;
Unity_Divide_float4(_UV_c2c5030522744aceb603ff104c9ccde0_Out_0_Vector4, (_Property_a975da4683af46ab85b4a95b01cb9f32_Out_0_Float.xxxx), _Divide_3dd829d125dc4ec1b32772766d56a17b_Out_2_Vector4);
float4 _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D.tex, _Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D.samplerstate, _Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D.GetTransformedUV((_Divide_3dd829d125dc4ec1b32772766d56a17b_Out_2_Vector4.xy)) );
if (_Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4, _Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_R_4_Float = _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4.r;
float _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_G_5_Float = _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4.g;
float _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_B_6_Float = _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4.b;
float _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_A_7_Float = _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4.a;
float _Multiply_81f5f8bc82304adbae345a5fe4b0024b_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_R_4_Float, 0.1, _Multiply_81f5f8bc82304adbae345a5fe4b0024b_Out_2_Float);
float _Add_33507c032b9e48bfb61763d507bb031c_Out_2_Float;
Unity_Add_float(_Multiply_81f5f8bc82304adbae345a5fe4b0024b_Out_2_Float, float(0.9), _Add_33507c032b9e48bfb61763d507bb031c_Out_2_Float);
float _Property_883f8e1288fd43f1a3674ca1f5364253_Out_0_Float = _Mask_Map_Intensity;
float _Power_b45fec9ffb074e40b5cb5452c0fb982d_Out_2_Float;
Unity_Power_float(_Add_33507c032b9e48bfb61763d507bb031c_Out_2_Float, _Property_883f8e1288fd43f1a3674ca1f5364253_Out_0_Float, _Power_b45fec9ffb074e40b5cb5452c0fb982d_Out_2_Float);
float _Split_b6a9cc69e69c417ba235626d7beed2eb_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_b6a9cc69e69c417ba235626d7beed2eb_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_b6a9cc69e69c417ba235626d7beed2eb_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_b6a9cc69e69c417ba235626d7beed2eb_A_4_Float = 0;
float _Property_73ac42b2cd3d4b43976ac13e73b6de40_Out_0_Float = _Height_from_Ground;
float3 _Transform_425aee5b4b5e409eb94b431642b515fb_Out_1_Vector3;
_Transform_425aee5b4b5e409eb94b431642b515fb_Out_1_Vector3 = TransformObjectToWorld(float3 (0, 0, 0).xyz);
float3 _Normalize_960b1f24c996479a8c1a9a81cabcb835_Out_1_Vector3;
Unity_Normalize_float3(_Transform_425aee5b4b5e409eb94b431642b515fb_Out_1_Vector3, _Normalize_960b1f24c996479a8c1a9a81cabcb835_Out_1_Vector3);
float _Property_11f1157c9f364521bf765c261e8bb93e_Out_0_Float = _Height_from_Ground_Randomness;
float _Multiply_b7a3ed973f104b9a84af846ce299d95f_Out_2_Float;
Unity_Multiply_float_float(_Property_11f1157c9f364521bf765c261e8bb93e_Out_0_Float, -1, _Multiply_b7a3ed973f104b9a84af846ce299d95f_Out_2_Float);
float _RandomRange_82fa55b7f4da4f6ebc597a38d2ca2e6d_Out_3_Float;
Unity_RandomRange_float((_Normalize_960b1f24c996479a8c1a9a81cabcb835_Out_1_Vector3.xy), _Multiply_b7a3ed973f104b9a84af846ce299d95f_Out_2_Float, _Property_11f1157c9f364521bf765c261e8bb93e_Out_0_Float, _RandomRange_82fa55b7f4da4f6ebc597a38d2ca2e6d_Out_3_Float);
float _Add_109ea06d643c4826b0138a85becc2fa9_Out_2_Float;
Unity_Add_float(_Property_73ac42b2cd3d4b43976ac13e73b6de40_Out_0_Float, _RandomRange_82fa55b7f4da4f6ebc597a38d2ca2e6d_Out_3_Float, _Add_109ea06d643c4826b0138a85becc2fa9_Out_2_Float);
float _Subtract_fe60462bdbdd4a22a207774a6a8cb1b8_Out_2_Float;
Unity_Subtract_float(_Split_b6a9cc69e69c417ba235626d7beed2eb_G_2_Float, _Add_109ea06d643c4826b0138a85becc2fa9_Out_2_Float, _Subtract_fe60462bdbdd4a22a207774a6a8cb1b8_Out_2_Float);
float _Property_1487773737e44e78a7c514c7f470e603_Out_0_Float = _Height_from_Ground_Softness;
float _Divide_6eb043a8aee54d87a0c73169597bdcba_Out_2_Float;
Unity_Divide_float(_Subtract_fe60462bdbdd4a22a207774a6a8cb1b8_Out_2_Float, _Property_1487773737e44e78a7c514c7f470e603_Out_0_Float, _Divide_6eb043a8aee54d87a0c73169597bdcba_Out_2_Float);
float _Saturate_d8ec125670414d1888aeec654fcb04e2_Out_1_Float;
Unity_Saturate_float(_Divide_6eb043a8aee54d87a0c73169597bdcba_Out_2_Float, _Saturate_d8ec125670414d1888aeec654fcb04e2_Out_1_Float);
float _OneMinus_de85154fa1b742e3b6c9e0f92b85d96c_Out_1_Float;
Unity_OneMinus_float(_Saturate_d8ec125670414d1888aeec654fcb04e2_Out_1_Float, _OneMinus_de85154fa1b742e3b6c9e0f92b85d96c_Out_1_Float);
float _DotProduct_a5624fdd7fd94a4c82ad8c039922e638_Out_2_Float;
Unity_DotProduct_float3(IN.WorldSpaceNormal, float3(0, 1, 0), _DotProduct_a5624fdd7fd94a4c82ad8c039922e638_Out_2_Float);
float _Property_1d95c04e88f443bc91fe7ed217b674b8_Out_0_Float = _Normal_Pow;
float _Power_5b713b10f3e049f8a97be81969b52b7d_Out_2_Float;
Unity_Power_float(_DotProduct_a5624fdd7fd94a4c82ad8c039922e638_Out_2_Float, _Property_1d95c04e88f443bc91fe7ed217b674b8_Out_0_Float, _Power_5b713b10f3e049f8a97be81969b52b7d_Out_2_Float);
float _Property_6314206eb47742698a7d2833f119f39e_Out_0_Float = _Normal_Mul;
float _Multiply_6e6bcfc2a4df414e86d241952840e972_Out_2_Float;
Unity_Multiply_float_float(_Power_5b713b10f3e049f8a97be81969b52b7d_Out_2_Float, _Property_6314206eb47742698a7d2833f119f39e_Out_0_Float, _Multiply_6e6bcfc2a4df414e86d241952840e972_Out_2_Float);
float _Saturate_77d41005224e417eb678b78b9a10771a_Out_1_Float;
Unity_Saturate_float(_Multiply_6e6bcfc2a4df414e86d241952840e972_Out_2_Float, _Saturate_77d41005224e417eb678b78b9a10771a_Out_1_Float);
float _Add_8395f84ea8e546988dd02e5b922d32ee_Out_2_Float;
Unity_Add_float(_OneMinus_de85154fa1b742e3b6c9e0f92b85d96c_Out_1_Float, _Saturate_77d41005224e417eb678b78b9a10771a_Out_1_Float, _Add_8395f84ea8e546988dd02e5b922d32ee_Out_2_Float);
float _Multiply_b61ead58dd024fd7aacad7c0b68f3be7_Out_2_Float;
Unity_Multiply_float_float(5, _Add_8395f84ea8e546988dd02e5b922d32ee_Out_2_Float, _Multiply_b61ead58dd024fd7aacad7c0b68f3be7_Out_2_Float);
float _Multiply_b0c3a49303544ccaa1d15379e6b1e9d2_Out_2_Float;
Unity_Multiply_float_float(_Power_b45fec9ffb074e40b5cb5452c0fb982d_Out_2_Float, _Multiply_b61ead58dd024fd7aacad7c0b68f3be7_Out_2_Float, _Multiply_b0c3a49303544ccaa1d15379e6b1e9d2_Out_2_Float);
float _Step_9e404f0c811346c88c531f42e623243f_Out_2_Float;
Unity_Step_float(_Subtract_fe60462bdbdd4a22a207774a6a8cb1b8_Out_2_Float, float(-0.1), _Step_9e404f0c811346c88c531f42e623243f_Out_2_Float);
float _Add_0328340190574386b2d37a0d035b66fb_Out_2_Float;
Unity_Add_float(_Multiply_b0c3a49303544ccaa1d15379e6b1e9d2_Out_2_Float, _Step_9e404f0c811346c88c531f42e623243f_Out_2_Float, _Add_0328340190574386b2d37a0d035b66fb_Out_2_Float);
float _Power_3bfc30934311465db3ba28e64b49097d_Out_2_Float;
Unity_Power_float(_Add_0328340190574386b2d37a0d035b66fb_Out_2_Float, float(3), _Power_3bfc30934311465db3ba28e64b49097d_Out_2_Float);
float _Saturate_3ba4a70af5214b4086803937a3f8b09f_Out_1_Float;
Unity_Saturate_float(_Power_3bfc30934311465db3ba28e64b49097d_Out_2_Float, _Saturate_3ba4a70af5214b4086803937a3f8b09f_Out_1_Float);
float _Saturate_6e423c9f5f1249088750a2b84a42b764_Out_1_Float;
Unity_Saturate_float(_Add_8395f84ea8e546988dd02e5b922d32ee_Out_2_Float, _Saturate_6e423c9f5f1249088750a2b84a42b764_Out_1_Float);
#if defined(_USE_COVERAGE_MASK_TEXTURE)
float _UseCoverageMaskTexture_f75c00eee68746bd80e2fa6c2bc28d4c_Out_0_Float = _Saturate_3ba4a70af5214b4086803937a3f8b09f_Out_1_Float;
#else
float _UseCoverageMaskTexture_f75c00eee68746bd80e2fa6c2bc28d4c_Out_0_Float = _Saturate_6e423c9f5f1249088750a2b84a42b764_Out_1_Float;
#endif
float4 _Lerp_c6c7a7472707418491f2fa10989f05b9_Out_3_Vector4;
Unity_Lerp_float4(_Multiply_e3ef32e4a4e8442188dfd3c93394960c_Out_2_Vector4, _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4, (_UseCoverageMaskTexture_f75c00eee68746bd80e2fa6c2bc28d4c_Out_0_Float.xxxx), _Lerp_c6c7a7472707418491f2fa10989f05b9_Out_3_Vector4);
#if defined(_USE_COVERAGE)
float4 _UseCoverage_c2f2c567080e4d5f9b652fbcbf667c54_Out_0_Vector4 = _Lerp_c6c7a7472707418491f2fa10989f05b9_Out_3_Vector4;
#else
float4 _UseCoverage_c2f2c567080e4d5f9b652fbcbf667c54_Out_0_Vector4 = _Multiply_e3ef32e4a4e8442188dfd3c93394960c_Out_2_Vector4;
#endif
surface.BaseColor = (_UseCoverage_c2f2c567080e4d5f9b652fbcbf667c54_Out_0_Vector4.xyz);
float3 reactionUnusedColor = 0, reactionUnusedEmission = 0;
ClipVegetationFuel(TransformObjectToWorld(float3(0,0,0)));
ApplyVegetationReaction(TransformObjectToWorld(float3(0,0,0)),surface.BaseColor,reactionUnusedEmission);
return surface;
}

// --------------------------------------------------
// Build Graph Inputs
#ifdef HAVE_VFX_MODIFICATION
#define VFX_SRP_ATTRIBUTES Attributes
#define VFX_SRP_VARYINGS Varyings
#define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
#endif
VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

    output.ObjectSpaceNormal =                          input.normalOS;
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
    output.AbsoluteWorldSpacePosition =                 GetAbsolutePositionWS(TransformObjectToWorld(input.positionOS));
    output.TimeParameters =                             _TimeParameters.xyz;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

#ifdef HAVE_VFX_MODIFICATION
#if VFX_USE_GRAPH_VALUES
    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
#endif
    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */

#endif

    

    // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
    float3 unnormalizedNormalWS = input.normalWS;
    const float renormFactor = 1.0 / length(unnormalizedNormalWS);


    output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph


    output.ObjectSpacePosition = TransformWorldToObject(input.positionWS);

    #if UNITY_UV_STARTS_AT_TOP
    #else
    #endif


    output.uv0 = input.texCoord0;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

// --------------------------------------------------
// Main

#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SelectionPickingPass.hlsl"

// --------------------------------------------------
// Visual Effect Vertex Invocations
#ifdef HAVE_VFX_MODIFICATION
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
#endif

ENDHLSL
}
Pass
{
    Name "Universal 2D"
    Tags
    {
        "LightMode" = "Universal2D"
    }

// Render State
Cull Back
Blend One Zero
ZTest LEqual
ZWrite On

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 2.0
#pragma vertex vert
#pragma fragment frag

// Keywords
// PassKeywords: <None>
#pragma shader_feature_local _ _USE_EMISSIVE
#pragma shader_feature_local _ _USE_WPO
#pragma shader_feature_local _ _USE_ORM
#pragma shader_feature_local _ _USE_NORMAL_MAPS
#pragma shader_feature_local _ _USE_COVERAGE
#pragma shader_feature_local _ _USE_COVERAGE_MASK_TEXTURE

#if defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_2
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_3
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_4
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_5
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_6
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM)
#define KEYWORD_PERMUTATION_7
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_8
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_9
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_10
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_11
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_12
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_13
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_14
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO)
#define KEYWORD_PERMUTATION_15
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_16
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_17
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_18
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_19
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_20
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_21
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_22
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM)
#define KEYWORD_PERMUTATION_23
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_24
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_25
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_26
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_27
#elif defined(_USE_EMISSIVE) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_28
#elif defined(_USE_EMISSIVE) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_29
#elif defined(_USE_EMISSIVE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_30
#elif defined(_USE_EMISSIVE)
#define KEYWORD_PERMUTATION_31
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_32
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_33
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_34
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_35
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_36
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_37
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_38
#elif defined(_USE_WPO) && defined(_USE_ORM)
#define KEYWORD_PERMUTATION_39
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_40
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_41
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_42
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_43
#elif defined(_USE_WPO) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_44
#elif defined(_USE_WPO) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_45
#elif defined(_USE_WPO) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_46
#elif defined(_USE_WPO)
#define KEYWORD_PERMUTATION_47
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_48
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_49
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_50
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_51
#elif defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_52
#elif defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_53
#elif defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_54
#elif defined(_USE_ORM)
#define KEYWORD_PERMUTATION_55
#elif defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_56
#elif defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_57
#elif defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_58
#elif defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_59
#elif defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_60
#elif defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_61
#elif defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_62
#else
#define KEYWORD_PERMUTATION_63
#endif


// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_NORMAL_WS
#define VARYINGS_NEED_TEXCOORD0
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_2D


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
 float3 positionOS : POSITION;
 float3 normalOS : NORMAL;
 float4 tangentOS : TANGENT;
 float4 uv0 : TEXCOORD0;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
 float4 positionCS : SV_POSITION;
 float3 positionWS;
 float3 normalWS;
 float4 texCoord0;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};
struct SurfaceDescriptionInputs
{
 float3 WorldSpaceNormal;
 float3 ObjectSpacePosition;
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 AbsoluteWorldSpacePosition;
 float3 TimeParameters;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
 float4 texCoord0 : INTERP0;
 float3 positionWS : INTERP1;
 float3 normalWS : INTERP2;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};

PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.positionCS = input.positionCS;
output.texCoord0.xyzw = input.texCoord0;
output.positionWS.xyz = input.positionWS;
output.normalWS.xyz = input.normalWS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.positionCS = input.positionCS;
output.texCoord0 = input.texCoord0.xyzw;
output.positionWS = input.positionWS.xyz;
output.normalWS = input.normalWS.xyz;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}


// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)

float _EnvironmentReactionSupported;
float4 _EnvState0, _EnvState1;
float _FuelCoverage, _AllowFuelCoverage;
float4 _WetTint, _CharTint, _FireColor;
float4 _EnvironmentCellBounds;
float _UseEnvironmentCells;

float _Height_from_Ground_Randomness;
float _Height_from_Ground_Softness;
float _Normal_Pow;
float _Normal_Mul;
float _Mask_Map_Size;
float4 _Coverage_Normal_Map_TexelSize;
float _Mask_Map_Intensity;
float4 _Coverage_ORM_Map_TexelSize;
float _Height_Weight;
float _Height_Gradience;
float _Wind_Intensity;
float3 _Wind_Direction;
float _Wind_Speed;
float _Wind_Time_Random_Offset_Amount;
float _Texture_Size;
float4 _Color_Map_TexelSize;
float4 _Color_Tint;
float4 _Normal_Map_TexelSize;
float _Normal_Intensity;
float4 _ORM_Map_TexelSize;
float _Roughness_Intensity;
float _AO_Intensity;
float _Metallic_Intensity;
float4 _Emissive_Map_TexelSize;
float4 _Emissive_Color;
float4 _Coverage_Color_Map_TexelSize;
float _Coverage_Size;
float _Height_from_Ground;
float4 _Mask_Map_TexelSize;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Coverage_Normal_Map);
SAMPLER(sampler_Coverage_Normal_Map);
TEXTURE2D(_Coverage_ORM_Map);
SAMPLER(sampler_Coverage_ORM_Map);
TEXTURE2D(_Color_Map);
SAMPLER(sampler_Color_Map);
TEXTURE2D(_Normal_Map);
SAMPLER(sampler_Normal_Map);
TEXTURE2D(_ORM_Map);
SAMPLER(sampler_ORM_Map);
TEXTURE2D(_Emissive_Map);
SAMPLER(sampler_Emissive_Map);
TEXTURE2D(_Coverage_Color_Map);
SAMPLER(sampler_Coverage_Color_Map);
TEXTURE2D(_Mask_Map);
SAMPLER(sampler_Mask_Map);

// Graph Includes
// GraphIncludes: <None>

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Functions

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Sine_float(float In, out float Out)
{
    Out = sin(In);
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Length_float3(float3 In, out float Out)
{
    Out = length(In);
}

void Unity_Divide_float(float A, float B, out float Out)
{
    Out = A / B;
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
{
    Out = lerp(A, B, T);
}

struct Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float
{
float3 ObjectSpacePosition;
float3 TimeParameters;
};

void SG_WindSway_51c4027ae50a8cd49be53c4990b9f179_float(float _Sway_Wind_Speed, float3 _Sway_Wind_Direction, float _Sway_Wind_Intensity, float _Sway_Wind_Time_Random_Offset_Amount, Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float IN, out float3 OutVector3_1)
{
float _Property_5c4cd19861f64014ae8e16af9365ef49_Out_0_Float = _Sway_Wind_Time_Random_Offset_Amount;
float _Add_4b31033339dc4f8e8e71674d98e662a3_Out_2_Float;
Unity_Add_float(IN.TimeParameters.x, _Property_5c4cd19861f64014ae8e16af9365ef49_Out_0_Float, _Add_4b31033339dc4f8e8e71674d98e662a3_Out_2_Float);
float _Property_9a93fa589ac7409884a94247349a940f_Out_0_Float = _Sway_Wind_Speed;
float _Multiply_7418655003ae4fedb2bf68678629472c_Out_2_Float;
Unity_Multiply_float_float(_Add_4b31033339dc4f8e8e71674d98e662a3_Out_2_Float, _Property_9a93fa589ac7409884a94247349a940f_Out_0_Float, _Multiply_7418655003ae4fedb2bf68678629472c_Out_2_Float);
float _Sine_ebd14b0fc42b4b4cb4387f3a1b879c1f_Out_1_Float;
Unity_Sine_float(_Multiply_7418655003ae4fedb2bf68678629472c_Out_2_Float, _Sine_ebd14b0fc42b4b4cb4387f3a1b879c1f_Out_1_Float);
float _Add_660e92170c564f94a79277cd2fd0cf20_Out_2_Float;
Unity_Add_float(_Sine_ebd14b0fc42b4b4cb4387f3a1b879c1f_Out_1_Float, float(1), _Add_660e92170c564f94a79277cd2fd0cf20_Out_2_Float);
float _Multiply_b8fd654b771447aebafc1a6aad6957cf_Out_2_Float;
Unity_Multiply_float_float(_Add_660e92170c564f94a79277cd2fd0cf20_Out_2_Float, 0.5, _Multiply_b8fd654b771447aebafc1a6aad6957cf_Out_2_Float);
float3 _Property_fa2ee033b44946c59c3fa9aea4a167c8_Out_0_Vector3 = _Sway_Wind_Direction;
float3 _Multiply_a5500b7f7b8e43988a055eea7eb917f3_Out_2_Vector3;
Unity_Multiply_float3_float3((_Multiply_b8fd654b771447aebafc1a6aad6957cf_Out_2_Float.xxx), _Property_fa2ee033b44946c59c3fa9aea4a167c8_Out_0_Vector3, _Multiply_a5500b7f7b8e43988a055eea7eb917f3_Out_2_Vector3);
float _Property_66cba0a3223546238e18b58e017e756f_Out_0_Float = _Sway_Wind_Intensity;
float3 _Multiply_475f81423e39483cba045bef08b9f967_Out_2_Vector3;
Unity_Multiply_float3_float3(_Multiply_a5500b7f7b8e43988a055eea7eb917f3_Out_2_Vector3, (_Property_66cba0a3223546238e18b58e017e756f_Out_0_Float.xxx), _Multiply_475f81423e39483cba045bef08b9f967_Out_2_Vector3);
float3 _Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3;
Unity_Add_float3(_Multiply_475f81423e39483cba045bef08b9f967_Out_2_Vector3, IN.ObjectSpacePosition, _Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3);
float _Length_53965ffb38de43359ab019a247194557_Out_1_Float;
Unity_Length_float3(IN.ObjectSpacePosition, _Length_53965ffb38de43359ab019a247194557_Out_1_Float);
float _Length_77b6b3d742104f2abf3c6968dea9a342_Out_1_Float;
Unity_Length_float3(_Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3, _Length_77b6b3d742104f2abf3c6968dea9a342_Out_1_Float);
float _Divide_f5dd43de79e343638d2743ee93ce7326_Out_2_Float;
Unity_Divide_float(_Length_53965ffb38de43359ab019a247194557_Out_1_Float, _Length_77b6b3d742104f2abf3c6968dea9a342_Out_1_Float, _Divide_f5dd43de79e343638d2743ee93ce7326_Out_2_Float);
float3 _Lerp_7574daf550e1428cb9d35656c7bb9603_Out_3_Vector3;
Unity_Lerp_float3(float3(0, 0, 0), _Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3, (_Divide_f5dd43de79e343638d2743ee93ce7326_Out_2_Float.xxx), _Lerp_7574daf550e1428cb9d35656c7bb9603_Out_3_Vector3);
OutVector3_1 = _Lerp_7574daf550e1428cb9d35656c7bb9603_Out_3_Vector3;
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

void Unity_Power_float(float A, float B, out float Out)
{
    Out = pow(A, B);
}

void Unity_Divide_float4(float4 A, float4 B, out float4 Out)
{
    Out = A / B;
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

void Unity_Normalize_float3(float3 In, out float3 Out)
{
    Out = normalize(In);
}

void Unity_RandomRange_float(float2 Seed, float Min, float Max, out float Out)
{
     float randomno =  frac(sin(dot(Seed, float2(12.9898, 78.233)))*43758.5453);
     Out = lerp(Min, Max, randomno);
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
}

void Unity_DotProduct_float3(float3 A, float3 B, out float Out)
{
    Out = dot(A, B);
}

void Unity_Step_float(float Edge, float In, out float Out)
{
    Out = step(Edge, In);
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 Position;
float3 Normal;
float3 Tangent;
};

VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
float _Property_b2e048f6276d4371b6ad931f5d1baa87_Out_0_Float = _Wind_Speed;
float3 _Property_7133a0551bc7429cb8bbfcbad044bf97_Out_0_Vector3 = _Wind_Direction;
float _Property_5aa2352867cd4da79dc9f9f9979925c7_Out_0_Float = _Wind_Intensity;
float _Property_e0c6ebff99e349d6966115347961a70c_Out_0_Float = _Wind_Time_Random_Offset_Amount;
Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf;
_WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf.ObjectSpacePosition = IN.ObjectSpacePosition;
_WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf.TimeParameters = IN.TimeParameters;
float3 _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf_OutVector3_1_Vector3;
SG_WindSway_51c4027ae50a8cd49be53c4990b9f179_float(_Property_b2e048f6276d4371b6ad931f5d1baa87_Out_0_Float, _Property_7133a0551bc7429cb8bbfcbad044bf97_Out_0_Vector3, _Property_5aa2352867cd4da79dc9f9f9979925c7_Out_0_Float, _Property_e0c6ebff99e349d6966115347961a70c_Out_0_Float, _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf, _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf_OutVector3_1_Vector3);
float _Split_38420bd2034746a1878048f0ead2a7b2_R_1_Float = IN.AbsoluteWorldSpacePosition[0];
float _Split_38420bd2034746a1878048f0ead2a7b2_G_2_Float = IN.AbsoluteWorldSpacePosition[1];
float _Split_38420bd2034746a1878048f0ead2a7b2_B_3_Float = IN.AbsoluteWorldSpacePosition[2];
float _Split_38420bd2034746a1878048f0ead2a7b2_A_4_Float = 0;
float3 _Vector3_0f8475b3a0df451ea79f6ea2e4e3f673_Out_0_Vector3 = float3(float(0), float(0), float(0));
float3 _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3;
{
// Converting Position from Object to AbsoluteWorld via world space
float3 world;
world = TransformObjectToWorld(_Vector3_0f8475b3a0df451ea79f6ea2e4e3f673_Out_0_Vector3.xyz);
_Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3 = GetAbsolutePositionWS(world);
}
float _Split_fb1b2d84926449e498da6c90f1436311_R_1_Float = _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3[0];
float _Split_fb1b2d84926449e498da6c90f1436311_G_2_Float = _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3[1];
float _Split_fb1b2d84926449e498da6c90f1436311_B_3_Float = _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3[2];
float _Split_fb1b2d84926449e498da6c90f1436311_A_4_Float = 0;
float _Subtract_18ddaa9851a547f1bc3eeafa55f3227b_Out_2_Float;
Unity_Subtract_float(_Split_38420bd2034746a1878048f0ead2a7b2_G_2_Float, _Split_fb1b2d84926449e498da6c90f1436311_G_2_Float, _Subtract_18ddaa9851a547f1bc3eeafa55f3227b_Out_2_Float);
float _Property_1db6873c977b47f186e42eef2cd75a57_Out_0_Float = _Height_Weight;
float _Split_bc5987163dba418d8de90e985de1247b_R_1_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[0];
float _Split_bc5987163dba418d8de90e985de1247b_G_2_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[1];
float _Split_bc5987163dba418d8de90e985de1247b_B_3_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[2];
float _Split_bc5987163dba418d8de90e985de1247b_A_4_Float = 0;
float _Multiply_89de5a5fb3cd451abb1227deba019c38_Out_2_Float;
Unity_Multiply_float_float(_Property_1db6873c977b47f186e42eef2cd75a57_Out_0_Float, _Split_bc5987163dba418d8de90e985de1247b_G_2_Float, _Multiply_89de5a5fb3cd451abb1227deba019c38_Out_2_Float);
float _Divide_2c406de6c63e463f9013e4faf1db1ae8_Out_2_Float;
Unity_Divide_float(_Subtract_18ddaa9851a547f1bc3eeafa55f3227b_Out_2_Float, _Multiply_89de5a5fb3cd451abb1227deba019c38_Out_2_Float, _Divide_2c406de6c63e463f9013e4faf1db1ae8_Out_2_Float);
float _Saturate_e9ea645ca89f482a9d0315df54a77ee5_Out_1_Float;
Unity_Saturate_float(_Divide_2c406de6c63e463f9013e4faf1db1ae8_Out_2_Float, _Saturate_e9ea645ca89f482a9d0315df54a77ee5_Out_1_Float);
float _Property_e96fd1920dff4533951da684129df200_Out_0_Float = _Height_Gradience;
float _Power_b88c5abc85424f17b03c60c98e9bc16c_Out_2_Float;
Unity_Power_float(_Saturate_e9ea645ca89f482a9d0315df54a77ee5_Out_1_Float, _Property_e96fd1920dff4533951da684129df200_Out_0_Float, _Power_b88c5abc85424f17b03c60c98e9bc16c_Out_2_Float);
float _Saturate_6c35d0fa108b483783bde88e06ff5ce0_Out_1_Float;
Unity_Saturate_float(_Power_b88c5abc85424f17b03c60c98e9bc16c_Out_2_Float, _Saturate_6c35d0fa108b483783bde88e06ff5ce0_Out_1_Float);
float3 _Lerp_8461a67d418941ee81ed9eb0f80e8a38_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf_OutVector3_1_Vector3, (_Saturate_6c35d0fa108b483783bde88e06ff5ce0_Out_1_Float.xxx), _Lerp_8461a67d418941ee81ed9eb0f80e8a38_Out_3_Vector3);
#if defined(_USE_WPO)
float3 _UseWPO_81cb92bff33547fbbfaa5ce2f60ba97c_Out_0_Vector3 = _Lerp_8461a67d418941ee81ed9eb0f80e8a38_Out_3_Vector3;
#else
float3 _UseWPO_81cb92bff33547fbbfaa5ce2f60ba97c_Out_0_Vector3 = IN.ObjectSpacePosition;
#endif
description.Position = _UseWPO_81cb92bff33547fbbfaa5ce2f60ba97c_Out_0_Vector3;
description.Normal = IN.ObjectSpaceNormal;
description.Tangent = IN.ObjectSpaceTangent;
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
float3 BaseColor;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
UnityTexture2D _Property_0c79649b4042462682b4a7423715f1a1_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Color_Map, sampler_Color_Map, _Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_a9551365617a4a66b75c6a49ab8f9c5f_Out_0_Vector4 = IN.uv0;
float _Property_ac4a3a6f20bd42789825abf989f1a228_Out_0_Float = _Texture_Size;
float4 _Divide_1ff147fe44c540028660935d59272eaa_Out_2_Vector4;
Unity_Divide_float4(_UV_a9551365617a4a66b75c6a49ab8f9c5f_Out_0_Vector4, (_Property_ac4a3a6f20bd42789825abf989f1a228_Out_0_Float.xxxx), _Divide_1ff147fe44c540028660935d59272eaa_Out_2_Vector4);
float4 _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_0c79649b4042462682b4a7423715f1a1_Out_0_Texture2D.tex, _Property_0c79649b4042462682b4a7423715f1a1_Out_0_Texture2D.samplerstate, _Property_0c79649b4042462682b4a7423715f1a1_Out_0_Texture2D.GetTransformedUV((_Divide_1ff147fe44c540028660935d59272eaa_Out_2_Vector4.xy)) );
if (_Property_0c79649b4042462682b4a7423715f1a1_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4, _Property_0c79649b4042462682b4a7423715f1a1_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_R_4_Float = _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4.r;
float _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_G_5_Float = _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4.g;
float _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_B_6_Float = _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4.b;
float _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_A_7_Float = _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4.a;
float4 _Property_11be52d8078b41908ba72022455e8b98_Out_0_Vector4 = _Color_Tint;
float4 _Multiply_e3ef32e4a4e8442188dfd3c93394960c_Out_2_Vector4;
Unity_Multiply_float4_float4(_SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4, _Property_11be52d8078b41908ba72022455e8b98_Out_0_Vector4, _Multiply_e3ef32e4a4e8442188dfd3c93394960c_Out_2_Vector4);
UnityTexture2D _Property_415df38a141841e6aa11a7b1e336c379_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Coverage_Color_Map, sampler_Coverage_Color_Map, _Coverage_Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_0805a5601f5b49beb942e50ab3714e03_Out_0_Vector4 = IN.uv0;
float _Property_eb7418710f3f4dc6981c01445b1a809e_Out_0_Float = _Coverage_Size;
float4 _Divide_71dd940938164d5a9e7020e44b1cddf3_Out_2_Vector4;
Unity_Divide_float4(_UV_0805a5601f5b49beb942e50ab3714e03_Out_0_Vector4, (_Property_eb7418710f3f4dc6981c01445b1a809e_Out_0_Float.xxxx), _Divide_71dd940938164d5a9e7020e44b1cddf3_Out_2_Vector4);
float4 _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_415df38a141841e6aa11a7b1e336c379_Out_0_Texture2D.tex, _Property_415df38a141841e6aa11a7b1e336c379_Out_0_Texture2D.samplerstate, _Property_415df38a141841e6aa11a7b1e336c379_Out_0_Texture2D.GetTransformedUV((_Divide_71dd940938164d5a9e7020e44b1cddf3_Out_2_Vector4.xy)) );
if (_Property_415df38a141841e6aa11a7b1e336c379_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4, _Property_415df38a141841e6aa11a7b1e336c379_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_R_4_Float = _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4.r;
float _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_G_5_Float = _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4.g;
float _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_B_6_Float = _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4.b;
float _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_A_7_Float = _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4.a;
UnityTexture2D _Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Mask_Map, sampler_Mask_Map, _Mask_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_c2c5030522744aceb603ff104c9ccde0_Out_0_Vector4 = IN.uv0;
float _Property_a975da4683af46ab85b4a95b01cb9f32_Out_0_Float = _Mask_Map_Size;
float4 _Divide_3dd829d125dc4ec1b32772766d56a17b_Out_2_Vector4;
Unity_Divide_float4(_UV_c2c5030522744aceb603ff104c9ccde0_Out_0_Vector4, (_Property_a975da4683af46ab85b4a95b01cb9f32_Out_0_Float.xxxx), _Divide_3dd829d125dc4ec1b32772766d56a17b_Out_2_Vector4);
float4 _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D.tex, _Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D.samplerstate, _Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D.GetTransformedUV((_Divide_3dd829d125dc4ec1b32772766d56a17b_Out_2_Vector4.xy)) );
if (_Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4, _Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_R_4_Float = _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4.r;
float _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_G_5_Float = _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4.g;
float _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_B_6_Float = _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4.b;
float _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_A_7_Float = _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4.a;
float _Multiply_81f5f8bc82304adbae345a5fe4b0024b_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_R_4_Float, 0.1, _Multiply_81f5f8bc82304adbae345a5fe4b0024b_Out_2_Float);
float _Add_33507c032b9e48bfb61763d507bb031c_Out_2_Float;
Unity_Add_float(_Multiply_81f5f8bc82304adbae345a5fe4b0024b_Out_2_Float, float(0.9), _Add_33507c032b9e48bfb61763d507bb031c_Out_2_Float);
float _Property_883f8e1288fd43f1a3674ca1f5364253_Out_0_Float = _Mask_Map_Intensity;
float _Power_b45fec9ffb074e40b5cb5452c0fb982d_Out_2_Float;
Unity_Power_float(_Add_33507c032b9e48bfb61763d507bb031c_Out_2_Float, _Property_883f8e1288fd43f1a3674ca1f5364253_Out_0_Float, _Power_b45fec9ffb074e40b5cb5452c0fb982d_Out_2_Float);
float _Split_b6a9cc69e69c417ba235626d7beed2eb_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_b6a9cc69e69c417ba235626d7beed2eb_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_b6a9cc69e69c417ba235626d7beed2eb_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_b6a9cc69e69c417ba235626d7beed2eb_A_4_Float = 0;
float _Property_73ac42b2cd3d4b43976ac13e73b6de40_Out_0_Float = _Height_from_Ground;
float3 _Transform_425aee5b4b5e409eb94b431642b515fb_Out_1_Vector3;
_Transform_425aee5b4b5e409eb94b431642b515fb_Out_1_Vector3 = TransformObjectToWorld(float3 (0, 0, 0).xyz);
float3 _Normalize_960b1f24c996479a8c1a9a81cabcb835_Out_1_Vector3;
Unity_Normalize_float3(_Transform_425aee5b4b5e409eb94b431642b515fb_Out_1_Vector3, _Normalize_960b1f24c996479a8c1a9a81cabcb835_Out_1_Vector3);
float _Property_11f1157c9f364521bf765c261e8bb93e_Out_0_Float = _Height_from_Ground_Randomness;
float _Multiply_b7a3ed973f104b9a84af846ce299d95f_Out_2_Float;
Unity_Multiply_float_float(_Property_11f1157c9f364521bf765c261e8bb93e_Out_0_Float, -1, _Multiply_b7a3ed973f104b9a84af846ce299d95f_Out_2_Float);
float _RandomRange_82fa55b7f4da4f6ebc597a38d2ca2e6d_Out_3_Float;
Unity_RandomRange_float((_Normalize_960b1f24c996479a8c1a9a81cabcb835_Out_1_Vector3.xy), _Multiply_b7a3ed973f104b9a84af846ce299d95f_Out_2_Float, _Property_11f1157c9f364521bf765c261e8bb93e_Out_0_Float, _RandomRange_82fa55b7f4da4f6ebc597a38d2ca2e6d_Out_3_Float);
float _Add_109ea06d643c4826b0138a85becc2fa9_Out_2_Float;
Unity_Add_float(_Property_73ac42b2cd3d4b43976ac13e73b6de40_Out_0_Float, _RandomRange_82fa55b7f4da4f6ebc597a38d2ca2e6d_Out_3_Float, _Add_109ea06d643c4826b0138a85becc2fa9_Out_2_Float);
float _Subtract_fe60462bdbdd4a22a207774a6a8cb1b8_Out_2_Float;
Unity_Subtract_float(_Split_b6a9cc69e69c417ba235626d7beed2eb_G_2_Float, _Add_109ea06d643c4826b0138a85becc2fa9_Out_2_Float, _Subtract_fe60462bdbdd4a22a207774a6a8cb1b8_Out_2_Float);
float _Property_1487773737e44e78a7c514c7f470e603_Out_0_Float = _Height_from_Ground_Softness;
float _Divide_6eb043a8aee54d87a0c73169597bdcba_Out_2_Float;
Unity_Divide_float(_Subtract_fe60462bdbdd4a22a207774a6a8cb1b8_Out_2_Float, _Property_1487773737e44e78a7c514c7f470e603_Out_0_Float, _Divide_6eb043a8aee54d87a0c73169597bdcba_Out_2_Float);
float _Saturate_d8ec125670414d1888aeec654fcb04e2_Out_1_Float;
Unity_Saturate_float(_Divide_6eb043a8aee54d87a0c73169597bdcba_Out_2_Float, _Saturate_d8ec125670414d1888aeec654fcb04e2_Out_1_Float);
float _OneMinus_de85154fa1b742e3b6c9e0f92b85d96c_Out_1_Float;
Unity_OneMinus_float(_Saturate_d8ec125670414d1888aeec654fcb04e2_Out_1_Float, _OneMinus_de85154fa1b742e3b6c9e0f92b85d96c_Out_1_Float);
float _DotProduct_a5624fdd7fd94a4c82ad8c039922e638_Out_2_Float;
Unity_DotProduct_float3(IN.WorldSpaceNormal, float3(0, 1, 0), _DotProduct_a5624fdd7fd94a4c82ad8c039922e638_Out_2_Float);
float _Property_1d95c04e88f443bc91fe7ed217b674b8_Out_0_Float = _Normal_Pow;
float _Power_5b713b10f3e049f8a97be81969b52b7d_Out_2_Float;
Unity_Power_float(_DotProduct_a5624fdd7fd94a4c82ad8c039922e638_Out_2_Float, _Property_1d95c04e88f443bc91fe7ed217b674b8_Out_0_Float, _Power_5b713b10f3e049f8a97be81969b52b7d_Out_2_Float);
float _Property_6314206eb47742698a7d2833f119f39e_Out_0_Float = _Normal_Mul;
float _Multiply_6e6bcfc2a4df414e86d241952840e972_Out_2_Float;
Unity_Multiply_float_float(_Power_5b713b10f3e049f8a97be81969b52b7d_Out_2_Float, _Property_6314206eb47742698a7d2833f119f39e_Out_0_Float, _Multiply_6e6bcfc2a4df414e86d241952840e972_Out_2_Float);
float _Saturate_77d41005224e417eb678b78b9a10771a_Out_1_Float;
Unity_Saturate_float(_Multiply_6e6bcfc2a4df414e86d241952840e972_Out_2_Float, _Saturate_77d41005224e417eb678b78b9a10771a_Out_1_Float);
float _Add_8395f84ea8e546988dd02e5b922d32ee_Out_2_Float;
Unity_Add_float(_OneMinus_de85154fa1b742e3b6c9e0f92b85d96c_Out_1_Float, _Saturate_77d41005224e417eb678b78b9a10771a_Out_1_Float, _Add_8395f84ea8e546988dd02e5b922d32ee_Out_2_Float);
float _Multiply_b61ead58dd024fd7aacad7c0b68f3be7_Out_2_Float;
Unity_Multiply_float_float(5, _Add_8395f84ea8e546988dd02e5b922d32ee_Out_2_Float, _Multiply_b61ead58dd024fd7aacad7c0b68f3be7_Out_2_Float);
float _Multiply_b0c3a49303544ccaa1d15379e6b1e9d2_Out_2_Float;
Unity_Multiply_float_float(_Power_b45fec9ffb074e40b5cb5452c0fb982d_Out_2_Float, _Multiply_b61ead58dd024fd7aacad7c0b68f3be7_Out_2_Float, _Multiply_b0c3a49303544ccaa1d15379e6b1e9d2_Out_2_Float);
float _Step_9e404f0c811346c88c531f42e623243f_Out_2_Float;
Unity_Step_float(_Subtract_fe60462bdbdd4a22a207774a6a8cb1b8_Out_2_Float, float(-0.1), _Step_9e404f0c811346c88c531f42e623243f_Out_2_Float);
float _Add_0328340190574386b2d37a0d035b66fb_Out_2_Float;
Unity_Add_float(_Multiply_b0c3a49303544ccaa1d15379e6b1e9d2_Out_2_Float, _Step_9e404f0c811346c88c531f42e623243f_Out_2_Float, _Add_0328340190574386b2d37a0d035b66fb_Out_2_Float);
float _Power_3bfc30934311465db3ba28e64b49097d_Out_2_Float;
Unity_Power_float(_Add_0328340190574386b2d37a0d035b66fb_Out_2_Float, float(3), _Power_3bfc30934311465db3ba28e64b49097d_Out_2_Float);
float _Saturate_3ba4a70af5214b4086803937a3f8b09f_Out_1_Float;
Unity_Saturate_float(_Power_3bfc30934311465db3ba28e64b49097d_Out_2_Float, _Saturate_3ba4a70af5214b4086803937a3f8b09f_Out_1_Float);
float _Saturate_6e423c9f5f1249088750a2b84a42b764_Out_1_Float;
Unity_Saturate_float(_Add_8395f84ea8e546988dd02e5b922d32ee_Out_2_Float, _Saturate_6e423c9f5f1249088750a2b84a42b764_Out_1_Float);
#if defined(_USE_COVERAGE_MASK_TEXTURE)
float _UseCoverageMaskTexture_f75c00eee68746bd80e2fa6c2bc28d4c_Out_0_Float = _Saturate_3ba4a70af5214b4086803937a3f8b09f_Out_1_Float;
#else
float _UseCoverageMaskTexture_f75c00eee68746bd80e2fa6c2bc28d4c_Out_0_Float = _Saturate_6e423c9f5f1249088750a2b84a42b764_Out_1_Float;
#endif
float4 _Lerp_c6c7a7472707418491f2fa10989f05b9_Out_3_Vector4;
Unity_Lerp_float4(_Multiply_e3ef32e4a4e8442188dfd3c93394960c_Out_2_Vector4, _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4, (_UseCoverageMaskTexture_f75c00eee68746bd80e2fa6c2bc28d4c_Out_0_Float.xxxx), _Lerp_c6c7a7472707418491f2fa10989f05b9_Out_3_Vector4);
#if defined(_USE_COVERAGE)
float4 _UseCoverage_c2f2c567080e4d5f9b652fbcbf667c54_Out_0_Vector4 = _Lerp_c6c7a7472707418491f2fa10989f05b9_Out_3_Vector4;
#else
float4 _UseCoverage_c2f2c567080e4d5f9b652fbcbf667c54_Out_0_Vector4 = _Multiply_e3ef32e4a4e8442188dfd3c93394960c_Out_2_Vector4;
#endif
surface.BaseColor = (_UseCoverage_c2f2c567080e4d5f9b652fbcbf667c54_Out_0_Vector4.xyz);
float3 reactionUnusedColor = 0, reactionUnusedEmission = 0;
ClipVegetationFuel(TransformObjectToWorld(float3(0,0,0)));
ApplyVegetationReaction(TransformObjectToWorld(float3(0,0,0)),surface.BaseColor,reactionUnusedEmission);
return surface;
}

// --------------------------------------------------
// Build Graph Inputs
#ifdef HAVE_VFX_MODIFICATION
#define VFX_SRP_ATTRIBUTES Attributes
#define VFX_SRP_VARYINGS Varyings
#define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
#endif
VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

    output.ObjectSpaceNormal =                          input.normalOS;
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
    output.AbsoluteWorldSpacePosition =                 GetAbsolutePositionWS(TransformObjectToWorld(input.positionOS));
    output.TimeParameters =                             _TimeParameters.xyz;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

#ifdef HAVE_VFX_MODIFICATION
#if VFX_USE_GRAPH_VALUES
    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
#endif
    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */

#endif

    

    // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
    float3 unnormalizedNormalWS = input.normalWS;
    const float renormFactor = 1.0 / length(unnormalizedNormalWS);


    output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph


    output.ObjectSpacePosition = TransformWorldToObject(input.positionWS);

    #if UNITY_UV_STARTS_AT_TOP
    #else
    #endif


    output.uv0 = input.texCoord0;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

// --------------------------------------------------
// Main

#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBR2DPass.hlsl"

// --------------------------------------------------
// Visual Effect Vertex Invocations
#ifdef HAVE_VFX_MODIFICATION
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
#endif

ENDHLSL
}
}
SubShader
{
Tags
{
// RenderPipeline: <None>
"RenderType"="Opaque"
"BuiltInMaterialType" = "Lit"
"Queue"="Geometry"
// DisableBatching: <None>
"ShaderGraphShader"="true"
"ShaderGraphTargetId"="BuiltInLitSubTarget"
}
Pass
{
    Name "BuiltIn Forward"
    Tags
    {
        "LightMode" = "ForwardBase"
    }

// Render State
Cull Back
Blend One Zero
ZTest LEqual
ZWrite On

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 3.0
#pragma multi_compile_instancing
#pragma multi_compile_fog
#pragma multi_compile_fwdbase
#pragma vertex vert
#pragma fragment frag

// Keywords
#pragma multi_compile _ _SCREEN_SPACE_OCCLUSION
#pragma multi_compile _ LIGHTMAP_ON
#pragma multi_compile _ DIRLIGHTMAP_COMBINED
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
#pragma multi_compile _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS _ADDITIONAL_OFF
#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
#pragma multi_compile _ _SHADOWS_SOFT
#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
#pragma multi_compile _ SHADOWS_SHADOWMASK
#pragma shader_feature_local _ _USE_EMISSIVE
#pragma shader_feature_local _ _USE_WPO
#pragma shader_feature_local _ _USE_ORM
#pragma shader_feature_local _ _USE_NORMAL_MAPS
#pragma shader_feature_local _ _USE_COVERAGE
#pragma shader_feature_local _ _USE_COVERAGE_MASK_TEXTURE

#if defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_2
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_3
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_4
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_5
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_6
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM)
#define KEYWORD_PERMUTATION_7
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_8
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_9
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_10
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_11
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_12
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_13
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_14
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO)
#define KEYWORD_PERMUTATION_15
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_16
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_17
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_18
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_19
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_20
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_21
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_22
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM)
#define KEYWORD_PERMUTATION_23
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_24
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_25
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_26
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_27
#elif defined(_USE_EMISSIVE) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_28
#elif defined(_USE_EMISSIVE) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_29
#elif defined(_USE_EMISSIVE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_30
#elif defined(_USE_EMISSIVE)
#define KEYWORD_PERMUTATION_31
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_32
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_33
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_34
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_35
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_36
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_37
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_38
#elif defined(_USE_WPO) && defined(_USE_ORM)
#define KEYWORD_PERMUTATION_39
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_40
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_41
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_42
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_43
#elif defined(_USE_WPO) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_44
#elif defined(_USE_WPO) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_45
#elif defined(_USE_WPO) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_46
#elif defined(_USE_WPO)
#define KEYWORD_PERMUTATION_47
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_48
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_49
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_50
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_51
#elif defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_52
#elif defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_53
#elif defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_54
#elif defined(_USE_ORM)
#define KEYWORD_PERMUTATION_55
#elif defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_56
#elif defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_57
#elif defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_58
#elif defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_59
#elif defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_60
#elif defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_61
#elif defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_62
#else
#define KEYWORD_PERMUTATION_63
#endif


// Defines
#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_TEXCOORD1
#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_NORMAL_WS
#define VARYINGS_NEED_TANGENT_WS
#define VARYINGS_NEED_TEXCOORD0
#define VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_FORWARD
#define BUILTIN_TARGET_API 1
#ifdef _BUILTIN_SURFACE_TYPE_TRANSPARENT
#define _SURFACE_TYPE_TRANSPARENT _BUILTIN_SURFACE_TYPE_TRANSPARENT
#endif
#ifdef _BUILTIN_ALPHATEST_ON
#define _ALPHATEST_ON _BUILTIN_ALPHATEST_ON
#endif
#ifdef _BUILTIN_AlphaClip
#define _AlphaClip _BUILTIN_AlphaClip
#endif
#ifdef _BUILTIN_ALPHAPREMULTIPLY_ON
#define _ALPHAPREMULTIPLY_ON _BUILTIN_ALPHAPREMULTIPLY_ON
#endif


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Shim/Shims.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/LegacySurfaceVertex.hlsl"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/ShaderGraphFunctions.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
 float3 positionOS : POSITION;
 float3 normalOS : NORMAL;
 float4 tangentOS : TANGENT;
 float4 uv0 : TEXCOORD0;
 float4 uv1 : TEXCOORD1;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
 float4 positionCS : SV_POSITION;
 float3 positionWS;
 float3 normalWS;
 float4 tangentWS;
 float4 texCoord0;
#if defined(LIGHTMAP_ON)
 float2 lightmapUV;
#endif
#if !defined(LIGHTMAP_ON)
 float3 sh;
#endif
 float4 fogFactorAndVertexLight;
 float4 shadowCoord;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};
struct SurfaceDescriptionInputs
{
 float3 WorldSpaceNormal;
 float3 TangentSpaceNormal;
 float3 ObjectSpacePosition;
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 AbsoluteWorldSpacePosition;
 float3 TimeParameters;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
#if defined(LIGHTMAP_ON)
 float2 lightmapUV : INTERP0;
#endif
#if !defined(LIGHTMAP_ON)
 float3 sh : INTERP1;
#endif
 float4 tangentWS : INTERP2;
 float4 texCoord0 : INTERP3;
 float4 fogFactorAndVertexLight : INTERP4;
 float4 shadowCoord : INTERP5;
 float3 positionWS : INTERP6;
 float3 normalWS : INTERP7;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};

PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.positionCS = input.positionCS;
#if defined(LIGHTMAP_ON)
output.lightmapUV = input.lightmapUV;
#endif
#if !defined(LIGHTMAP_ON)
output.sh = input.sh;
#endif
output.tangentWS.xyzw = input.tangentWS;
output.texCoord0.xyzw = input.texCoord0;
output.fogFactorAndVertexLight.xyzw = input.fogFactorAndVertexLight;
output.shadowCoord.xyzw = input.shadowCoord;
output.positionWS.xyz = input.positionWS;
output.normalWS.xyz = input.normalWS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.positionCS = input.positionCS;
#if defined(LIGHTMAP_ON)
output.lightmapUV = input.lightmapUV;
#endif
#if !defined(LIGHTMAP_ON)
output.sh = input.sh;
#endif
output.tangentWS = input.tangentWS.xyzw;
output.texCoord0 = input.texCoord0.xyzw;
output.fogFactorAndVertexLight = input.fogFactorAndVertexLight.xyzw;
output.shadowCoord = input.shadowCoord.xyzw;
output.positionWS = input.positionWS.xyz;
output.normalWS = input.normalWS.xyz;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}


// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)

float _EnvironmentReactionSupported;
float4 _EnvState0, _EnvState1;
float _FuelCoverage, _AllowFuelCoverage;
float4 _WetTint, _CharTint, _FireColor;
float4 _EnvironmentCellBounds;
float _UseEnvironmentCells;

float _Height_from_Ground_Randomness;
float _Height_from_Ground_Softness;
float _Normal_Pow;
float _Normal_Mul;
float _Mask_Map_Size;
float4 _Coverage_Normal_Map_TexelSize;
float _Mask_Map_Intensity;
float4 _Coverage_ORM_Map_TexelSize;
float _Height_Weight;
float _Height_Gradience;
float _Wind_Intensity;
float3 _Wind_Direction;
float _Wind_Speed;
float _Wind_Time_Random_Offset_Amount;
float _Texture_Size;
float4 _Color_Map_TexelSize;
float4 _Color_Tint;
float4 _Normal_Map_TexelSize;
float _Normal_Intensity;
float4 _ORM_Map_TexelSize;
float _Roughness_Intensity;
float _AO_Intensity;
float _Metallic_Intensity;
float4 _Emissive_Map_TexelSize;
float4 _Emissive_Color;
float4 _Coverage_Color_Map_TexelSize;
float _Coverage_Size;
float _Height_from_Ground;
float4 _Mask_Map_TexelSize;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Coverage_Normal_Map);
SAMPLER(sampler_Coverage_Normal_Map);
TEXTURE2D(_Coverage_ORM_Map);
SAMPLER(sampler_Coverage_ORM_Map);
TEXTURE2D(_Color_Map);
SAMPLER(sampler_Color_Map);
TEXTURE2D(_Normal_Map);
SAMPLER(sampler_Normal_Map);
TEXTURE2D(_ORM_Map);
SAMPLER(sampler_ORM_Map);
TEXTURE2D(_Emissive_Map);
SAMPLER(sampler_Emissive_Map);
TEXTURE2D(_Coverage_Color_Map);
SAMPLER(sampler_Coverage_Color_Map);
TEXTURE2D(_Mask_Map);
SAMPLER(sampler_Mask_Map);

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Includes
// GraphIncludes: <None>

// Graph Functions

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Sine_float(float In, out float Out)
{
    Out = sin(In);
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Length_float3(float3 In, out float Out)
{
    Out = length(In);
}

void Unity_Divide_float(float A, float B, out float Out)
{
    Out = A / B;
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
{
    Out = lerp(A, B, T);
}

struct Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float
{
float3 ObjectSpacePosition;
float3 TimeParameters;
};

void SG_WindSway_51c4027ae50a8cd49be53c4990b9f179_float(float _Sway_Wind_Speed, float3 _Sway_Wind_Direction, float _Sway_Wind_Intensity, float _Sway_Wind_Time_Random_Offset_Amount, Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float IN, out float3 OutVector3_1)
{
float _Property_5c4cd19861f64014ae8e16af9365ef49_Out_0_Float = _Sway_Wind_Time_Random_Offset_Amount;
float _Add_4b31033339dc4f8e8e71674d98e662a3_Out_2_Float;
Unity_Add_float(IN.TimeParameters.x, _Property_5c4cd19861f64014ae8e16af9365ef49_Out_0_Float, _Add_4b31033339dc4f8e8e71674d98e662a3_Out_2_Float);
float _Property_9a93fa589ac7409884a94247349a940f_Out_0_Float = _Sway_Wind_Speed;
float _Multiply_7418655003ae4fedb2bf68678629472c_Out_2_Float;
Unity_Multiply_float_float(_Add_4b31033339dc4f8e8e71674d98e662a3_Out_2_Float, _Property_9a93fa589ac7409884a94247349a940f_Out_0_Float, _Multiply_7418655003ae4fedb2bf68678629472c_Out_2_Float);
float _Sine_ebd14b0fc42b4b4cb4387f3a1b879c1f_Out_1_Float;
Unity_Sine_float(_Multiply_7418655003ae4fedb2bf68678629472c_Out_2_Float, _Sine_ebd14b0fc42b4b4cb4387f3a1b879c1f_Out_1_Float);
float _Add_660e92170c564f94a79277cd2fd0cf20_Out_2_Float;
Unity_Add_float(_Sine_ebd14b0fc42b4b4cb4387f3a1b879c1f_Out_1_Float, float(1), _Add_660e92170c564f94a79277cd2fd0cf20_Out_2_Float);
float _Multiply_b8fd654b771447aebafc1a6aad6957cf_Out_2_Float;
Unity_Multiply_float_float(_Add_660e92170c564f94a79277cd2fd0cf20_Out_2_Float, 0.5, _Multiply_b8fd654b771447aebafc1a6aad6957cf_Out_2_Float);
float3 _Property_fa2ee033b44946c59c3fa9aea4a167c8_Out_0_Vector3 = _Sway_Wind_Direction;
float3 _Multiply_a5500b7f7b8e43988a055eea7eb917f3_Out_2_Vector3;
Unity_Multiply_float3_float3((_Multiply_b8fd654b771447aebafc1a6aad6957cf_Out_2_Float.xxx), _Property_fa2ee033b44946c59c3fa9aea4a167c8_Out_0_Vector3, _Multiply_a5500b7f7b8e43988a055eea7eb917f3_Out_2_Vector3);
float _Property_66cba0a3223546238e18b58e017e756f_Out_0_Float = _Sway_Wind_Intensity;
float3 _Multiply_475f81423e39483cba045bef08b9f967_Out_2_Vector3;
Unity_Multiply_float3_float3(_Multiply_a5500b7f7b8e43988a055eea7eb917f3_Out_2_Vector3, (_Property_66cba0a3223546238e18b58e017e756f_Out_0_Float.xxx), _Multiply_475f81423e39483cba045bef08b9f967_Out_2_Vector3);
float3 _Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3;
Unity_Add_float3(_Multiply_475f81423e39483cba045bef08b9f967_Out_2_Vector3, IN.ObjectSpacePosition, _Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3);
float _Length_53965ffb38de43359ab019a247194557_Out_1_Float;
Unity_Length_float3(IN.ObjectSpacePosition, _Length_53965ffb38de43359ab019a247194557_Out_1_Float);
float _Length_77b6b3d742104f2abf3c6968dea9a342_Out_1_Float;
Unity_Length_float3(_Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3, _Length_77b6b3d742104f2abf3c6968dea9a342_Out_1_Float);
float _Divide_f5dd43de79e343638d2743ee93ce7326_Out_2_Float;
Unity_Divide_float(_Length_53965ffb38de43359ab019a247194557_Out_1_Float, _Length_77b6b3d742104f2abf3c6968dea9a342_Out_1_Float, _Divide_f5dd43de79e343638d2743ee93ce7326_Out_2_Float);
float3 _Lerp_7574daf550e1428cb9d35656c7bb9603_Out_3_Vector3;
Unity_Lerp_float3(float3(0, 0, 0), _Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3, (_Divide_f5dd43de79e343638d2743ee93ce7326_Out_2_Float.xxx), _Lerp_7574daf550e1428cb9d35656c7bb9603_Out_3_Vector3);
OutVector3_1 = _Lerp_7574daf550e1428cb9d35656c7bb9603_Out_3_Vector3;
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

void Unity_Power_float(float A, float B, out float Out)
{
    Out = pow(A, B);
}

void Unity_Divide_float4(float4 A, float4 B, out float4 Out)
{
    Out = A / B;
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

void Unity_Normalize_float3(float3 In, out float3 Out)
{
    Out = normalize(In);
}

void Unity_RandomRange_float(float2 Seed, float Min, float Max, out float Out)
{
     float randomno =  frac(sin(dot(Seed, float2(12.9898, 78.233)))*43758.5453);
     Out = lerp(Min, Max, randomno);
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
}

void Unity_DotProduct_float3(float3 A, float3 B, out float Out)
{
    Out = dot(A, B);
}

void Unity_Step_float(float Edge, float In, out float Out)
{
    Out = step(Edge, In);
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_NormalStrength_float(float3 In, float Strength, out float3 Out)
{
    Out = float3(In.rg * Strength, lerp(1, In.b, saturate(Strength)));
}

void Unity_Lerp_float(float A, float B, float T, out float Out)
{
    Out = lerp(A, B, T);
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 Position;
float3 Normal;
float3 Tangent;
};

VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
float _Property_b2e048f6276d4371b6ad931f5d1baa87_Out_0_Float = _Wind_Speed;
float3 _Property_7133a0551bc7429cb8bbfcbad044bf97_Out_0_Vector3 = _Wind_Direction;
float _Property_5aa2352867cd4da79dc9f9f9979925c7_Out_0_Float = _Wind_Intensity;
float _Property_e0c6ebff99e349d6966115347961a70c_Out_0_Float = _Wind_Time_Random_Offset_Amount;
Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf;
_WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf.ObjectSpacePosition = IN.ObjectSpacePosition;
_WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf.TimeParameters = IN.TimeParameters;
float3 _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf_OutVector3_1_Vector3;
SG_WindSway_51c4027ae50a8cd49be53c4990b9f179_float(_Property_b2e048f6276d4371b6ad931f5d1baa87_Out_0_Float, _Property_7133a0551bc7429cb8bbfcbad044bf97_Out_0_Vector3, _Property_5aa2352867cd4da79dc9f9f9979925c7_Out_0_Float, _Property_e0c6ebff99e349d6966115347961a70c_Out_0_Float, _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf, _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf_OutVector3_1_Vector3);
float _Split_38420bd2034746a1878048f0ead2a7b2_R_1_Float = IN.AbsoluteWorldSpacePosition[0];
float _Split_38420bd2034746a1878048f0ead2a7b2_G_2_Float = IN.AbsoluteWorldSpacePosition[1];
float _Split_38420bd2034746a1878048f0ead2a7b2_B_3_Float = IN.AbsoluteWorldSpacePosition[2];
float _Split_38420bd2034746a1878048f0ead2a7b2_A_4_Float = 0;
float3 _Vector3_0f8475b3a0df451ea79f6ea2e4e3f673_Out_0_Vector3 = float3(float(0), float(0), float(0));
float3 _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3;
{
// Converting Position from Object to AbsoluteWorld via world space
float3 world;
world = TransformObjectToWorld(_Vector3_0f8475b3a0df451ea79f6ea2e4e3f673_Out_0_Vector3.xyz);
_Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3 = GetAbsolutePositionWS(world);
}
float _Split_fb1b2d84926449e498da6c90f1436311_R_1_Float = _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3[0];
float _Split_fb1b2d84926449e498da6c90f1436311_G_2_Float = _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3[1];
float _Split_fb1b2d84926449e498da6c90f1436311_B_3_Float = _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3[2];
float _Split_fb1b2d84926449e498da6c90f1436311_A_4_Float = 0;
float _Subtract_18ddaa9851a547f1bc3eeafa55f3227b_Out_2_Float;
Unity_Subtract_float(_Split_38420bd2034746a1878048f0ead2a7b2_G_2_Float, _Split_fb1b2d84926449e498da6c90f1436311_G_2_Float, _Subtract_18ddaa9851a547f1bc3eeafa55f3227b_Out_2_Float);
float _Property_1db6873c977b47f186e42eef2cd75a57_Out_0_Float = _Height_Weight;
float _Split_bc5987163dba418d8de90e985de1247b_R_1_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[0];
float _Split_bc5987163dba418d8de90e985de1247b_G_2_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[1];
float _Split_bc5987163dba418d8de90e985de1247b_B_3_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[2];
float _Split_bc5987163dba418d8de90e985de1247b_A_4_Float = 0;
float _Multiply_89de5a5fb3cd451abb1227deba019c38_Out_2_Float;
Unity_Multiply_float_float(_Property_1db6873c977b47f186e42eef2cd75a57_Out_0_Float, _Split_bc5987163dba418d8de90e985de1247b_G_2_Float, _Multiply_89de5a5fb3cd451abb1227deba019c38_Out_2_Float);
float _Divide_2c406de6c63e463f9013e4faf1db1ae8_Out_2_Float;
Unity_Divide_float(_Subtract_18ddaa9851a547f1bc3eeafa55f3227b_Out_2_Float, _Multiply_89de5a5fb3cd451abb1227deba019c38_Out_2_Float, _Divide_2c406de6c63e463f9013e4faf1db1ae8_Out_2_Float);
float _Saturate_e9ea645ca89f482a9d0315df54a77ee5_Out_1_Float;
Unity_Saturate_float(_Divide_2c406de6c63e463f9013e4faf1db1ae8_Out_2_Float, _Saturate_e9ea645ca89f482a9d0315df54a77ee5_Out_1_Float);
float _Property_e96fd1920dff4533951da684129df200_Out_0_Float = _Height_Gradience;
float _Power_b88c5abc85424f17b03c60c98e9bc16c_Out_2_Float;
Unity_Power_float(_Saturate_e9ea645ca89f482a9d0315df54a77ee5_Out_1_Float, _Property_e96fd1920dff4533951da684129df200_Out_0_Float, _Power_b88c5abc85424f17b03c60c98e9bc16c_Out_2_Float);
float _Saturate_6c35d0fa108b483783bde88e06ff5ce0_Out_1_Float;
Unity_Saturate_float(_Power_b88c5abc85424f17b03c60c98e9bc16c_Out_2_Float, _Saturate_6c35d0fa108b483783bde88e06ff5ce0_Out_1_Float);
float3 _Lerp_8461a67d418941ee81ed9eb0f80e8a38_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf_OutVector3_1_Vector3, (_Saturate_6c35d0fa108b483783bde88e06ff5ce0_Out_1_Float.xxx), _Lerp_8461a67d418941ee81ed9eb0f80e8a38_Out_3_Vector3);
#if defined(_USE_WPO)
float3 _UseWPO_81cb92bff33547fbbfaa5ce2f60ba97c_Out_0_Vector3 = _Lerp_8461a67d418941ee81ed9eb0f80e8a38_Out_3_Vector3;
#else
float3 _UseWPO_81cb92bff33547fbbfaa5ce2f60ba97c_Out_0_Vector3 = IN.ObjectSpacePosition;
#endif
description.Position = _UseWPO_81cb92bff33547fbbfaa5ce2f60ba97c_Out_0_Vector3;
description.Normal = IN.ObjectSpaceNormal;
description.Tangent = IN.ObjectSpaceTangent;
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
float3 BaseColor;
float3 NormalTS;
float3 Emission;
float Metallic;
float Smoothness;
float Occlusion;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
UnityTexture2D _Property_0c79649b4042462682b4a7423715f1a1_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Color_Map, sampler_Color_Map, _Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_a9551365617a4a66b75c6a49ab8f9c5f_Out_0_Vector4 = IN.uv0;
float _Property_ac4a3a6f20bd42789825abf989f1a228_Out_0_Float = _Texture_Size;
float4 _Divide_1ff147fe44c540028660935d59272eaa_Out_2_Vector4;
Unity_Divide_float4(_UV_a9551365617a4a66b75c6a49ab8f9c5f_Out_0_Vector4, (_Property_ac4a3a6f20bd42789825abf989f1a228_Out_0_Float.xxxx), _Divide_1ff147fe44c540028660935d59272eaa_Out_2_Vector4);
float4 _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_0c79649b4042462682b4a7423715f1a1_Out_0_Texture2D.tex, _Property_0c79649b4042462682b4a7423715f1a1_Out_0_Texture2D.samplerstate, _Property_0c79649b4042462682b4a7423715f1a1_Out_0_Texture2D.GetTransformedUV((_Divide_1ff147fe44c540028660935d59272eaa_Out_2_Vector4.xy)) );
if (_Property_0c79649b4042462682b4a7423715f1a1_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4, _Property_0c79649b4042462682b4a7423715f1a1_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_R_4_Float = _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4.r;
float _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_G_5_Float = _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4.g;
float _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_B_6_Float = _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4.b;
float _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_A_7_Float = _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4.a;
float4 _Property_11be52d8078b41908ba72022455e8b98_Out_0_Vector4 = _Color_Tint;
float4 _Multiply_e3ef32e4a4e8442188dfd3c93394960c_Out_2_Vector4;
Unity_Multiply_float4_float4(_SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4, _Property_11be52d8078b41908ba72022455e8b98_Out_0_Vector4, _Multiply_e3ef32e4a4e8442188dfd3c93394960c_Out_2_Vector4);
UnityTexture2D _Property_415df38a141841e6aa11a7b1e336c379_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Coverage_Color_Map, sampler_Coverage_Color_Map, _Coverage_Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_0805a5601f5b49beb942e50ab3714e03_Out_0_Vector4 = IN.uv0;
float _Property_eb7418710f3f4dc6981c01445b1a809e_Out_0_Float = _Coverage_Size;
float4 _Divide_71dd940938164d5a9e7020e44b1cddf3_Out_2_Vector4;
Unity_Divide_float4(_UV_0805a5601f5b49beb942e50ab3714e03_Out_0_Vector4, (_Property_eb7418710f3f4dc6981c01445b1a809e_Out_0_Float.xxxx), _Divide_71dd940938164d5a9e7020e44b1cddf3_Out_2_Vector4);
float4 _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_415df38a141841e6aa11a7b1e336c379_Out_0_Texture2D.tex, _Property_415df38a141841e6aa11a7b1e336c379_Out_0_Texture2D.samplerstate, _Property_415df38a141841e6aa11a7b1e336c379_Out_0_Texture2D.GetTransformedUV((_Divide_71dd940938164d5a9e7020e44b1cddf3_Out_2_Vector4.xy)) );
if (_Property_415df38a141841e6aa11a7b1e336c379_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4, _Property_415df38a141841e6aa11a7b1e336c379_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_R_4_Float = _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4.r;
float _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_G_5_Float = _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4.g;
float _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_B_6_Float = _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4.b;
float _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_A_7_Float = _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4.a;
UnityTexture2D _Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Mask_Map, sampler_Mask_Map, _Mask_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_c2c5030522744aceb603ff104c9ccde0_Out_0_Vector4 = IN.uv0;
float _Property_a975da4683af46ab85b4a95b01cb9f32_Out_0_Float = _Mask_Map_Size;
float4 _Divide_3dd829d125dc4ec1b32772766d56a17b_Out_2_Vector4;
Unity_Divide_float4(_UV_c2c5030522744aceb603ff104c9ccde0_Out_0_Vector4, (_Property_a975da4683af46ab85b4a95b01cb9f32_Out_0_Float.xxxx), _Divide_3dd829d125dc4ec1b32772766d56a17b_Out_2_Vector4);
float4 _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D.tex, _Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D.samplerstate, _Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D.GetTransformedUV((_Divide_3dd829d125dc4ec1b32772766d56a17b_Out_2_Vector4.xy)) );
if (_Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4, _Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_R_4_Float = _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4.r;
float _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_G_5_Float = _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4.g;
float _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_B_6_Float = _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4.b;
float _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_A_7_Float = _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4.a;
float _Multiply_81f5f8bc82304adbae345a5fe4b0024b_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_R_4_Float, 0.1, _Multiply_81f5f8bc82304adbae345a5fe4b0024b_Out_2_Float);
float _Add_33507c032b9e48bfb61763d507bb031c_Out_2_Float;
Unity_Add_float(_Multiply_81f5f8bc82304adbae345a5fe4b0024b_Out_2_Float, float(0.9), _Add_33507c032b9e48bfb61763d507bb031c_Out_2_Float);
float _Property_883f8e1288fd43f1a3674ca1f5364253_Out_0_Float = _Mask_Map_Intensity;
float _Power_b45fec9ffb074e40b5cb5452c0fb982d_Out_2_Float;
Unity_Power_float(_Add_33507c032b9e48bfb61763d507bb031c_Out_2_Float, _Property_883f8e1288fd43f1a3674ca1f5364253_Out_0_Float, _Power_b45fec9ffb074e40b5cb5452c0fb982d_Out_2_Float);
float _Split_b6a9cc69e69c417ba235626d7beed2eb_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_b6a9cc69e69c417ba235626d7beed2eb_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_b6a9cc69e69c417ba235626d7beed2eb_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_b6a9cc69e69c417ba235626d7beed2eb_A_4_Float = 0;
float _Property_73ac42b2cd3d4b43976ac13e73b6de40_Out_0_Float = _Height_from_Ground;
float3 _Transform_425aee5b4b5e409eb94b431642b515fb_Out_1_Vector3;
_Transform_425aee5b4b5e409eb94b431642b515fb_Out_1_Vector3 = TransformObjectToWorld(float3 (0, 0, 0).xyz);
float3 _Normalize_960b1f24c996479a8c1a9a81cabcb835_Out_1_Vector3;
Unity_Normalize_float3(_Transform_425aee5b4b5e409eb94b431642b515fb_Out_1_Vector3, _Normalize_960b1f24c996479a8c1a9a81cabcb835_Out_1_Vector3);
float _Property_11f1157c9f364521bf765c261e8bb93e_Out_0_Float = _Height_from_Ground_Randomness;
float _Multiply_b7a3ed973f104b9a84af846ce299d95f_Out_2_Float;
Unity_Multiply_float_float(_Property_11f1157c9f364521bf765c261e8bb93e_Out_0_Float, -1, _Multiply_b7a3ed973f104b9a84af846ce299d95f_Out_2_Float);
float _RandomRange_82fa55b7f4da4f6ebc597a38d2ca2e6d_Out_3_Float;
Unity_RandomRange_float((_Normalize_960b1f24c996479a8c1a9a81cabcb835_Out_1_Vector3.xy), _Multiply_b7a3ed973f104b9a84af846ce299d95f_Out_2_Float, _Property_11f1157c9f364521bf765c261e8bb93e_Out_0_Float, _RandomRange_82fa55b7f4da4f6ebc597a38d2ca2e6d_Out_3_Float);
float _Add_109ea06d643c4826b0138a85becc2fa9_Out_2_Float;
Unity_Add_float(_Property_73ac42b2cd3d4b43976ac13e73b6de40_Out_0_Float, _RandomRange_82fa55b7f4da4f6ebc597a38d2ca2e6d_Out_3_Float, _Add_109ea06d643c4826b0138a85becc2fa9_Out_2_Float);
float _Subtract_fe60462bdbdd4a22a207774a6a8cb1b8_Out_2_Float;
Unity_Subtract_float(_Split_b6a9cc69e69c417ba235626d7beed2eb_G_2_Float, _Add_109ea06d643c4826b0138a85becc2fa9_Out_2_Float, _Subtract_fe60462bdbdd4a22a207774a6a8cb1b8_Out_2_Float);
float _Property_1487773737e44e78a7c514c7f470e603_Out_0_Float = _Height_from_Ground_Softness;
float _Divide_6eb043a8aee54d87a0c73169597bdcba_Out_2_Float;
Unity_Divide_float(_Subtract_fe60462bdbdd4a22a207774a6a8cb1b8_Out_2_Float, _Property_1487773737e44e78a7c514c7f470e603_Out_0_Float, _Divide_6eb043a8aee54d87a0c73169597bdcba_Out_2_Float);
float _Saturate_d8ec125670414d1888aeec654fcb04e2_Out_1_Float;
Unity_Saturate_float(_Divide_6eb043a8aee54d87a0c73169597bdcba_Out_2_Float, _Saturate_d8ec125670414d1888aeec654fcb04e2_Out_1_Float);
float _OneMinus_de85154fa1b742e3b6c9e0f92b85d96c_Out_1_Float;
Unity_OneMinus_float(_Saturate_d8ec125670414d1888aeec654fcb04e2_Out_1_Float, _OneMinus_de85154fa1b742e3b6c9e0f92b85d96c_Out_1_Float);
float _DotProduct_a5624fdd7fd94a4c82ad8c039922e638_Out_2_Float;
Unity_DotProduct_float3(IN.WorldSpaceNormal, float3(0, 1, 0), _DotProduct_a5624fdd7fd94a4c82ad8c039922e638_Out_2_Float);
float _Property_1d95c04e88f443bc91fe7ed217b674b8_Out_0_Float = _Normal_Pow;
float _Power_5b713b10f3e049f8a97be81969b52b7d_Out_2_Float;
Unity_Power_float(_DotProduct_a5624fdd7fd94a4c82ad8c039922e638_Out_2_Float, _Property_1d95c04e88f443bc91fe7ed217b674b8_Out_0_Float, _Power_5b713b10f3e049f8a97be81969b52b7d_Out_2_Float);
float _Property_6314206eb47742698a7d2833f119f39e_Out_0_Float = _Normal_Mul;
float _Multiply_6e6bcfc2a4df414e86d241952840e972_Out_2_Float;
Unity_Multiply_float_float(_Power_5b713b10f3e049f8a97be81969b52b7d_Out_2_Float, _Property_6314206eb47742698a7d2833f119f39e_Out_0_Float, _Multiply_6e6bcfc2a4df414e86d241952840e972_Out_2_Float);
float _Saturate_77d41005224e417eb678b78b9a10771a_Out_1_Float;
Unity_Saturate_float(_Multiply_6e6bcfc2a4df414e86d241952840e972_Out_2_Float, _Saturate_77d41005224e417eb678b78b9a10771a_Out_1_Float);
float _Add_8395f84ea8e546988dd02e5b922d32ee_Out_2_Float;
Unity_Add_float(_OneMinus_de85154fa1b742e3b6c9e0f92b85d96c_Out_1_Float, _Saturate_77d41005224e417eb678b78b9a10771a_Out_1_Float, _Add_8395f84ea8e546988dd02e5b922d32ee_Out_2_Float);
float _Multiply_b61ead58dd024fd7aacad7c0b68f3be7_Out_2_Float;
Unity_Multiply_float_float(5, _Add_8395f84ea8e546988dd02e5b922d32ee_Out_2_Float, _Multiply_b61ead58dd024fd7aacad7c0b68f3be7_Out_2_Float);
float _Multiply_b0c3a49303544ccaa1d15379e6b1e9d2_Out_2_Float;
Unity_Multiply_float_float(_Power_b45fec9ffb074e40b5cb5452c0fb982d_Out_2_Float, _Multiply_b61ead58dd024fd7aacad7c0b68f3be7_Out_2_Float, _Multiply_b0c3a49303544ccaa1d15379e6b1e9d2_Out_2_Float);
float _Step_9e404f0c811346c88c531f42e623243f_Out_2_Float;
Unity_Step_float(_Subtract_fe60462bdbdd4a22a207774a6a8cb1b8_Out_2_Float, float(-0.1), _Step_9e404f0c811346c88c531f42e623243f_Out_2_Float);
float _Add_0328340190574386b2d37a0d035b66fb_Out_2_Float;
Unity_Add_float(_Multiply_b0c3a49303544ccaa1d15379e6b1e9d2_Out_2_Float, _Step_9e404f0c811346c88c531f42e623243f_Out_2_Float, _Add_0328340190574386b2d37a0d035b66fb_Out_2_Float);
float _Power_3bfc30934311465db3ba28e64b49097d_Out_2_Float;
Unity_Power_float(_Add_0328340190574386b2d37a0d035b66fb_Out_2_Float, float(3), _Power_3bfc30934311465db3ba28e64b49097d_Out_2_Float);
float _Saturate_3ba4a70af5214b4086803937a3f8b09f_Out_1_Float;
Unity_Saturate_float(_Power_3bfc30934311465db3ba28e64b49097d_Out_2_Float, _Saturate_3ba4a70af5214b4086803937a3f8b09f_Out_1_Float);
float _Saturate_6e423c9f5f1249088750a2b84a42b764_Out_1_Float;
Unity_Saturate_float(_Add_8395f84ea8e546988dd02e5b922d32ee_Out_2_Float, _Saturate_6e423c9f5f1249088750a2b84a42b764_Out_1_Float);
#if defined(_USE_COVERAGE_MASK_TEXTURE)
float _UseCoverageMaskTexture_f75c00eee68746bd80e2fa6c2bc28d4c_Out_0_Float = _Saturate_3ba4a70af5214b4086803937a3f8b09f_Out_1_Float;
#else
float _UseCoverageMaskTexture_f75c00eee68746bd80e2fa6c2bc28d4c_Out_0_Float = _Saturate_6e423c9f5f1249088750a2b84a42b764_Out_1_Float;
#endif
float4 _Lerp_c6c7a7472707418491f2fa10989f05b9_Out_3_Vector4;
Unity_Lerp_float4(_Multiply_e3ef32e4a4e8442188dfd3c93394960c_Out_2_Vector4, _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4, (_UseCoverageMaskTexture_f75c00eee68746bd80e2fa6c2bc28d4c_Out_0_Float.xxxx), _Lerp_c6c7a7472707418491f2fa10989f05b9_Out_3_Vector4);
#if defined(_USE_COVERAGE)
float4 _UseCoverage_c2f2c567080e4d5f9b652fbcbf667c54_Out_0_Vector4 = _Lerp_c6c7a7472707418491f2fa10989f05b9_Out_3_Vector4;
#else
float4 _UseCoverage_c2f2c567080e4d5f9b652fbcbf667c54_Out_0_Vector4 = _Multiply_e3ef32e4a4e8442188dfd3c93394960c_Out_2_Vector4;
#endif
UnityTexture2D _Property_a38bf1e56c364aa0add1f01a99eb6c3e_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Normal_Map, sampler_Normal_Map, _Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_a38bf1e56c364aa0add1f01a99eb6c3e_Out_0_Texture2D.tex, _Property_a38bf1e56c364aa0add1f01a99eb6c3e_Out_0_Texture2D.samplerstate, _Property_a38bf1e56c364aa0add1f01a99eb6c3e_Out_0_Texture2D.GetTransformedUV((_Divide_1ff147fe44c540028660935d59272eaa_Out_2_Vector4.xy)) );
if (_Property_a38bf1e56c364aa0add1f01a99eb6c3e_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4, _Property_a38bf1e56c364aa0add1f01a99eb6c3e_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4);
float _SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_R_4_Float = _SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4.r;
float _SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_G_5_Float = _SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4.g;
float _SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_B_6_Float = _SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4.b;
float _SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_A_7_Float = _SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4.a;
float _Property_ff6f57141462418790b8f0ef1d7d69cc_Out_0_Float = _Normal_Intensity;
float3 _NormalStrength_6e1c84dc8cbd47f988a654d24e83669f_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4.xyz), _Property_ff6f57141462418790b8f0ef1d7d69cc_Out_0_Float, _NormalStrength_6e1c84dc8cbd47f988a654d24e83669f_Out_2_Vector3);
#if defined(_USE_NORMAL_MAPS)
float3 _UseNormalMaps_f01546d53fa54c3191638b3fe3736688_Out_0_Vector3 = _NormalStrength_6e1c84dc8cbd47f988a654d24e83669f_Out_2_Vector3;
#else
float3 _UseNormalMaps_f01546d53fa54c3191638b3fe3736688_Out_0_Vector3 = float3(0, 0, 1);
#endif
UnityTexture2D _Property_15070824f15143aea7ede9b130164716_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Coverage_Normal_Map, sampler_Coverage_Normal_Map, _Coverage_Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_15070824f15143aea7ede9b130164716_Out_0_Texture2D.tex, _Property_15070824f15143aea7ede9b130164716_Out_0_Texture2D.samplerstate, _Property_15070824f15143aea7ede9b130164716_Out_0_Texture2D.GetTransformedUV((_Divide_71dd940938164d5a9e7020e44b1cddf3_Out_2_Vector4.xy)) );
if (_Property_15070824f15143aea7ede9b130164716_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4, _Property_15070824f15143aea7ede9b130164716_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4);
float _SampleTexture2D_26c139222fa1483d83940748db6d922f_R_4_Float = _SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4.r;
float _SampleTexture2D_26c139222fa1483d83940748db6d922f_G_5_Float = _SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4.g;
float _SampleTexture2D_26c139222fa1483d83940748db6d922f_B_6_Float = _SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4.b;
float _SampleTexture2D_26c139222fa1483d83940748db6d922f_A_7_Float = _SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4.a;
float3 _Lerp_a5163d5f017c46b9b347647a7d361156_Out_3_Vector3;
Unity_Lerp_float3(_UseNormalMaps_f01546d53fa54c3191638b3fe3736688_Out_0_Vector3, (_SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4.xyz), (_UseCoverageMaskTexture_f75c00eee68746bd80e2fa6c2bc28d4c_Out_0_Float.xxx), _Lerp_a5163d5f017c46b9b347647a7d361156_Out_3_Vector3);
#if defined(_USE_COVERAGE)
float3 _UseCoverage_d6109394a0154e89890cf6ca228f700f_Out_0_Vector3 = _Lerp_a5163d5f017c46b9b347647a7d361156_Out_3_Vector3;
#else
float3 _UseCoverage_d6109394a0154e89890cf6ca228f700f_Out_0_Vector3 = _UseNormalMaps_f01546d53fa54c3191638b3fe3736688_Out_0_Vector3;
#endif
#if defined(_USE_NORMAL_MAPS)
float3 _UseNormalMaps_cf0fe6c3e8224d69a92ff77f7f270e12_Out_0_Vector3 = _UseCoverage_d6109394a0154e89890cf6ca228f700f_Out_0_Vector3;
#else
float3 _UseNormalMaps_cf0fe6c3e8224d69a92ff77f7f270e12_Out_0_Vector3 = float3(0, 0, 1);
#endif
UnityTexture2D _Property_c7dc38eaeea545aaada0e67cc32d9d25_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Emissive_Map, sampler_Emissive_Map, _Emissive_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_c7dc38eaeea545aaada0e67cc32d9d25_Out_0_Texture2D.tex, _Property_c7dc38eaeea545aaada0e67cc32d9d25_Out_0_Texture2D.samplerstate, _Property_c7dc38eaeea545aaada0e67cc32d9d25_Out_0_Texture2D.GetTransformedUV((_Divide_1ff147fe44c540028660935d59272eaa_Out_2_Vector4.xy)) );
if (_Property_c7dc38eaeea545aaada0e67cc32d9d25_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4, _Property_c7dc38eaeea545aaada0e67cc32d9d25_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_R_4_Float = _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4.r;
float _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_G_5_Float = _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4.g;
float _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_B_6_Float = _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4.b;
float _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_A_7_Float = _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4.a;
float4 _Property_857cbfa6469047f8904e0adc3eefe6b8_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_Emissive_Color) : _Emissive_Color;
float4 _Multiply_293fcf7127ab419cbcdff4183cccdba0_Out_2_Vector4;
Unity_Multiply_float4_float4(_SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4, _Property_857cbfa6469047f8904e0adc3eefe6b8_Out_0_Vector4, _Multiply_293fcf7127ab419cbcdff4183cccdba0_Out_2_Vector4);
#if defined(_USE_EMISSIVE)
float4 _UseEmissive_fdf874f7398e43a7838e4f04fd255375_Out_0_Vector4 = _Multiply_293fcf7127ab419cbcdff4183cccdba0_Out_2_Vector4;
#else
float4 _UseEmissive_fdf874f7398e43a7838e4f04fd255375_Out_0_Vector4 = float4(0, 0, 0, 0);
#endif
float _Property_3cef5a13363c421c89eb005848414161_Out_0_Float = _Metallic_Intensity;
UnityTexture2D _Property_6ced109efdc542d6868e0f24e23a1736_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_ORM_Map, sampler_ORM_Map, _ORM_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_6ced109efdc542d6868e0f24e23a1736_Out_0_Texture2D.tex, _Property_6ced109efdc542d6868e0f24e23a1736_Out_0_Texture2D.samplerstate, _Property_6ced109efdc542d6868e0f24e23a1736_Out_0_Texture2D.GetTransformedUV((_Divide_1ff147fe44c540028660935d59272eaa_Out_2_Vector4.xy)) );
if (_Property_6ced109efdc542d6868e0f24e23a1736_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_RGBA_0_Vector4, _Property_6ced109efdc542d6868e0f24e23a1736_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_R_4_Float = _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_RGBA_0_Vector4.r;
float _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_G_5_Float = _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_RGBA_0_Vector4.g;
float _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_B_6_Float = _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_RGBA_0_Vector4.b;
float _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_A_7_Float = _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_RGBA_0_Vector4.a;
float _Multiply_ffdbad83b5e74ce1b5ca4b9e7e162c40_Out_2_Float;
Unity_Multiply_float_float(_Property_3cef5a13363c421c89eb005848414161_Out_0_Float, _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_B_6_Float, _Multiply_ffdbad83b5e74ce1b5ca4b9e7e162c40_Out_2_Float);
UnityTexture2D _Property_75507ddd24ab4c63b88da2bbaec94f2c_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Coverage_ORM_Map, sampler_Coverage_ORM_Map, _Coverage_ORM_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_75507ddd24ab4c63b88da2bbaec94f2c_Out_0_Texture2D.tex, _Property_75507ddd24ab4c63b88da2bbaec94f2c_Out_0_Texture2D.samplerstate, _Property_75507ddd24ab4c63b88da2bbaec94f2c_Out_0_Texture2D.GetTransformedUV((_Divide_71dd940938164d5a9e7020e44b1cddf3_Out_2_Vector4.xy)) );
if (_Property_75507ddd24ab4c63b88da2bbaec94f2c_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_RGBA_0_Vector4, _Property_75507ddd24ab4c63b88da2bbaec94f2c_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_R_4_Float = _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_RGBA_0_Vector4.r;
float _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_G_5_Float = _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_RGBA_0_Vector4.g;
float _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_B_6_Float = _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_RGBA_0_Vector4.b;
float _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_A_7_Float = _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_RGBA_0_Vector4.a;
float _Lerp_3101e882d6a14372973d46e58ded048f_Out_3_Float;
Unity_Lerp_float(_Multiply_ffdbad83b5e74ce1b5ca4b9e7e162c40_Out_2_Float, _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_B_6_Float, _UseCoverageMaskTexture_f75c00eee68746bd80e2fa6c2bc28d4c_Out_0_Float, _Lerp_3101e882d6a14372973d46e58ded048f_Out_3_Float);
#if defined(_USE_COVERAGE)
float _UseCoverage_5268f0597a8b4df2a05cf3d5da317f6b_Out_0_Float = _Lerp_3101e882d6a14372973d46e58ded048f_Out_3_Float;
#else
float _UseCoverage_5268f0597a8b4df2a05cf3d5da317f6b_Out_0_Float = _Multiply_ffdbad83b5e74ce1b5ca4b9e7e162c40_Out_2_Float;
#endif
float _Property_d066edd24a97480b83f88a08c9a274b8_Out_0_Float = _Metallic_Intensity;
#if defined(_USE_ORM)
float _UseORM_11ec65ff6fc7436b9d5074e8f7a90024_Out_0_Float = _UseCoverage_5268f0597a8b4df2a05cf3d5da317f6b_Out_0_Float;
#else
float _UseORM_11ec65ff6fc7436b9d5074e8f7a90024_Out_0_Float = _Property_d066edd24a97480b83f88a08c9a274b8_Out_0_Float;
#endif
float _Property_ef0d0e2cdec043359496bb1a0782cda9_Out_0_Float = _Roughness_Intensity;
float _Multiply_4b10161d594b4863a7b31e2a1f1c1cf1_Out_2_Float;
Unity_Multiply_float_float(_Property_ef0d0e2cdec043359496bb1a0782cda9_Out_0_Float, _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_G_5_Float, _Multiply_4b10161d594b4863a7b31e2a1f1c1cf1_Out_2_Float);
float _OneMinus_d7dd0fb677e94540bbc094136ca963f5_Out_1_Float;
Unity_OneMinus_float(_Multiply_4b10161d594b4863a7b31e2a1f1c1cf1_Out_2_Float, _OneMinus_d7dd0fb677e94540bbc094136ca963f5_Out_1_Float);
float _OneMinus_bae441d794cd4092861f8242b9f96358_Out_1_Float;
Unity_OneMinus_float(_SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_G_5_Float, _OneMinus_bae441d794cd4092861f8242b9f96358_Out_1_Float);
float _Lerp_377c896eaf3243859da5c9b1446d27f5_Out_3_Float;
Unity_Lerp_float(_OneMinus_d7dd0fb677e94540bbc094136ca963f5_Out_1_Float, _OneMinus_bae441d794cd4092861f8242b9f96358_Out_1_Float, _UseCoverageMaskTexture_f75c00eee68746bd80e2fa6c2bc28d4c_Out_0_Float, _Lerp_377c896eaf3243859da5c9b1446d27f5_Out_3_Float);
#if defined(_USE_COVERAGE)
float _UseCoverage_58ec20069d0a4e0cb494b690fec0b60b_Out_0_Float = _Lerp_377c896eaf3243859da5c9b1446d27f5_Out_3_Float;
#else
float _UseCoverage_58ec20069d0a4e0cb494b690fec0b60b_Out_0_Float = _OneMinus_d7dd0fb677e94540bbc094136ca963f5_Out_1_Float;
#endif
float _Property_a7403d2892854317b50b8fa0268d0f27_Out_0_Float = _Roughness_Intensity;
float _OneMinus_3b59d96fc25b4b00a0364fda5bd47109_Out_1_Float;
Unity_OneMinus_float(_Property_a7403d2892854317b50b8fa0268d0f27_Out_0_Float, _OneMinus_3b59d96fc25b4b00a0364fda5bd47109_Out_1_Float);
#if defined(_USE_ORM)
float _UseORM_2aa3605b11694e28a61f126281dcd8f3_Out_0_Float = _UseCoverage_58ec20069d0a4e0cb494b690fec0b60b_Out_0_Float;
#else
float _UseORM_2aa3605b11694e28a61f126281dcd8f3_Out_0_Float = _OneMinus_3b59d96fc25b4b00a0364fda5bd47109_Out_1_Float;
#endif
float _Property_b578c28d617c46edb5452f0ee34e3698_Out_0_Float = _AO_Intensity;
float _Lerp_11f21031558c4c7db3e9b583ee8c5843_Out_3_Float;
Unity_Lerp_float(float(1), _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_R_4_Float, _Property_b578c28d617c46edb5452f0ee34e3698_Out_0_Float, _Lerp_11f21031558c4c7db3e9b583ee8c5843_Out_3_Float);
float _Lerp_f01b6ad8ed5e4f7aa0a392bc0bd1b199_Out_3_Float;
Unity_Lerp_float(_Lerp_11f21031558c4c7db3e9b583ee8c5843_Out_3_Float, _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_R_4_Float, _UseCoverageMaskTexture_f75c00eee68746bd80e2fa6c2bc28d4c_Out_0_Float, _Lerp_f01b6ad8ed5e4f7aa0a392bc0bd1b199_Out_3_Float);
#if defined(_USE_COVERAGE)
float _UseCoverage_ae6eaa1f25704217b662768dd49ad504_Out_0_Float = _Lerp_f01b6ad8ed5e4f7aa0a392bc0bd1b199_Out_3_Float;
#else
float _UseCoverage_ae6eaa1f25704217b662768dd49ad504_Out_0_Float = _Lerp_11f21031558c4c7db3e9b583ee8c5843_Out_3_Float;
#endif
#if defined(_USE_ORM)
float _UseORM_a8f4959fe3174af9bd41a7335d24f10e_Out_0_Float = _UseCoverage_ae6eaa1f25704217b662768dd49ad504_Out_0_Float;
#else
float _UseORM_a8f4959fe3174af9bd41a7335d24f10e_Out_0_Float = float(1);
#endif
surface.BaseColor = (_UseCoverage_c2f2c567080e4d5f9b652fbcbf667c54_Out_0_Vector4.xyz);
surface.NormalTS = _UseNormalMaps_cf0fe6c3e8224d69a92ff77f7f270e12_Out_0_Vector3;
surface.Emission = (_UseEmissive_fdf874f7398e43a7838e4f04fd255375_Out_0_Vector4.xyz);
surface.Metallic = _UseORM_11ec65ff6fc7436b9d5074e8f7a90024_Out_0_Float;
surface.Smoothness = _UseORM_2aa3605b11694e28a61f126281dcd8f3_Out_0_Float;
surface.Occlusion = _UseORM_a8f4959fe3174af9bd41a7335d24f10e_Out_0_Float;
float3 reactionUnusedColor = 0, reactionUnusedEmission = 0;
ClipVegetationFuel(TransformObjectToWorld(float3(0,0,0)));
ApplyVegetationReaction(TransformObjectToWorld(float3(0,0,0)),surface.BaseColor,surface.Emission);
return surface;
}

// --------------------------------------------------
// Build Graph Inputs

VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

    output.ObjectSpaceNormal =                          input.normalOS;
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
    output.AbsoluteWorldSpacePosition =                 GetAbsolutePositionWS(TransformObjectToWorld(input.positionOS));
    output.TimeParameters =                             _TimeParameters.xyz;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

    

    // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
    float3 unnormalizedNormalWS = input.normalWS;
    const float renormFactor = 1.0 / length(unnormalizedNormalWS);


    output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph
    output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);


    output.ObjectSpacePosition = TransformWorldToObject(input.positionWS);

    #if UNITY_UV_STARTS_AT_TOP
    #else
    #endif


    output.uv0 = input.texCoord0;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

void BuildAppDataFull(Attributes attributes, VertexDescription vertexDescription, inout appdata_full result)
{
    result.vertex     = float4(attributes.positionOS, 1);
    result.tangent    = attributes.tangentOS;
    result.normal     = attributes.normalOS;
    result.texcoord   = attributes.uv0;
    result.texcoord1  = attributes.uv1;
    result.vertex     = float4(vertexDescription.Position, 1);
    result.normal     = vertexDescription.Normal;
    result.tangent    = float4(vertexDescription.Tangent, 0);
    #if UNITY_ANY_INSTANCING_ENABLED
    #endif
}

void VaryingsToSurfaceVertex(Varyings varyings, inout v2f_surf result)
{
    result.pos = varyings.positionCS;
    result.worldPos = varyings.positionWS;
    result.worldNormal = varyings.normalWS;
    // World Tangent isn't an available input on v2f_surf

    result._ShadowCoord = varyings.shadowCoord;

    #if UNITY_ANY_INSTANCING_ENABLED
    #endif
    #if UNITY_SHOULD_SAMPLE_SH
    #if !defined(LIGHTMAP_ON)
    result.sh = varyings.sh;
    #endif
    #endif
    #if defined(LIGHTMAP_ON)
    result.lmap.xy = varyings.lightmapUV;
    #endif
    #ifdef VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
        result.fogCoord = varyings.fogFactorAndVertexLight.x;
        COPY_TO_LIGHT_COORDS(result, varyings.fogFactorAndVertexLight.yzw);
    #endif

    DEFAULT_UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(varyings, result);
}

void SurfaceVertexToVaryings(v2f_surf surfVertex, inout Varyings result)
{
    result.positionCS = surfVertex.pos;
    result.positionWS = surfVertex.worldPos;
    result.normalWS = surfVertex.worldNormal;
    // viewDirectionWS is never filled out in the legacy pass' function. Always use the value computed by SRP
    // World Tangent isn't an available input on v2f_surf
    result.shadowCoord = surfVertex._ShadowCoord;

    #if UNITY_ANY_INSTANCING_ENABLED
    #endif
    #if UNITY_SHOULD_SAMPLE_SH
    #if !defined(LIGHTMAP_ON)
    result.sh = surfVertex.sh;
    #endif
    #endif
    #if defined(LIGHTMAP_ON)
    result.lightmapUV = surfVertex.lmap.xy;
    #endif
    #ifdef VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
        result.fogFactorAndVertexLight.x = surfVertex.fogCoord;
        COPY_FROM_LIGHT_COORDS(result.fogFactorAndVertexLight.yzw, surfVertex);
    #endif

    DEFAULT_UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(surfVertex, result);
}

// --------------------------------------------------
// Main

#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/Varyings.hlsl"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/PBRForwardPass.hlsl"

ENDHLSL
}
Pass
{
    Name "BuiltIn ForwardAdd"
    Tags
    {
        "LightMode" = "ForwardAdd"
    }

// Render State
Blend SrcAlpha One, One One
ZWrite Off

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 3.0
#pragma multi_compile_instancing
#pragma multi_compile_fog
#pragma multi_compile_fwdadd_fullshadows
#pragma vertex vert
#pragma fragment frag

// Keywords
#pragma multi_compile _ _SCREEN_SPACE_OCCLUSION
#pragma multi_compile _ LIGHTMAP_ON
#pragma multi_compile _ DIRLIGHTMAP_COMBINED
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
#pragma multi_compile _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS _ADDITIONAL_OFF
#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
#pragma multi_compile _ _SHADOWS_SOFT
#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
#pragma multi_compile _ SHADOWS_SHADOWMASK
#pragma shader_feature_local _ _USE_EMISSIVE
#pragma shader_feature_local _ _USE_WPO
#pragma shader_feature_local _ _USE_ORM
#pragma shader_feature_local _ _USE_NORMAL_MAPS
#pragma shader_feature_local _ _USE_COVERAGE
#pragma shader_feature_local _ _USE_COVERAGE_MASK_TEXTURE

#if defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_2
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_3
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_4
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_5
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_6
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM)
#define KEYWORD_PERMUTATION_7
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_8
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_9
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_10
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_11
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_12
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_13
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_14
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO)
#define KEYWORD_PERMUTATION_15
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_16
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_17
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_18
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_19
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_20
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_21
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_22
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM)
#define KEYWORD_PERMUTATION_23
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_24
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_25
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_26
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_27
#elif defined(_USE_EMISSIVE) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_28
#elif defined(_USE_EMISSIVE) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_29
#elif defined(_USE_EMISSIVE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_30
#elif defined(_USE_EMISSIVE)
#define KEYWORD_PERMUTATION_31
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_32
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_33
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_34
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_35
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_36
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_37
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_38
#elif defined(_USE_WPO) && defined(_USE_ORM)
#define KEYWORD_PERMUTATION_39
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_40
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_41
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_42
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_43
#elif defined(_USE_WPO) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_44
#elif defined(_USE_WPO) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_45
#elif defined(_USE_WPO) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_46
#elif defined(_USE_WPO)
#define KEYWORD_PERMUTATION_47
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_48
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_49
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_50
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_51
#elif defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_52
#elif defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_53
#elif defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_54
#elif defined(_USE_ORM)
#define KEYWORD_PERMUTATION_55
#elif defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_56
#elif defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_57
#elif defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_58
#elif defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_59
#elif defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_60
#elif defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_61
#elif defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_62
#else
#define KEYWORD_PERMUTATION_63
#endif


// Defines
#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_TEXCOORD1
#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_NORMAL_WS
#define VARYINGS_NEED_TANGENT_WS
#define VARYINGS_NEED_TEXCOORD0
#define VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_FORWARD_ADD
#define BUILTIN_TARGET_API 1
#ifdef _BUILTIN_SURFACE_TYPE_TRANSPARENT
#define _SURFACE_TYPE_TRANSPARENT _BUILTIN_SURFACE_TYPE_TRANSPARENT
#endif
#ifdef _BUILTIN_ALPHATEST_ON
#define _ALPHATEST_ON _BUILTIN_ALPHATEST_ON
#endif
#ifdef _BUILTIN_AlphaClip
#define _AlphaClip _BUILTIN_AlphaClip
#endif
#ifdef _BUILTIN_ALPHAPREMULTIPLY_ON
#define _ALPHAPREMULTIPLY_ON _BUILTIN_ALPHAPREMULTIPLY_ON
#endif


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Shim/Shims.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/LegacySurfaceVertex.hlsl"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/ShaderGraphFunctions.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
 float3 positionOS : POSITION;
 float3 normalOS : NORMAL;
 float4 tangentOS : TANGENT;
 float4 uv0 : TEXCOORD0;
 float4 uv1 : TEXCOORD1;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
 float4 positionCS : SV_POSITION;
 float3 positionWS;
 float3 normalWS;
 float4 tangentWS;
 float4 texCoord0;
#if defined(LIGHTMAP_ON)
 float2 lightmapUV;
#endif
#if !defined(LIGHTMAP_ON)
 float3 sh;
#endif
 float4 fogFactorAndVertexLight;
 float4 shadowCoord;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};
struct SurfaceDescriptionInputs
{
 float3 WorldSpaceNormal;
 float3 TangentSpaceNormal;
 float3 ObjectSpacePosition;
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 AbsoluteWorldSpacePosition;
 float3 TimeParameters;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
#if defined(LIGHTMAP_ON)
 float2 lightmapUV : INTERP0;
#endif
#if !defined(LIGHTMAP_ON)
 float3 sh : INTERP1;
#endif
 float4 tangentWS : INTERP2;
 float4 texCoord0 : INTERP3;
 float4 fogFactorAndVertexLight : INTERP4;
 float4 shadowCoord : INTERP5;
 float3 positionWS : INTERP6;
 float3 normalWS : INTERP7;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};

PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.positionCS = input.positionCS;
#if defined(LIGHTMAP_ON)
output.lightmapUV = input.lightmapUV;
#endif
#if !defined(LIGHTMAP_ON)
output.sh = input.sh;
#endif
output.tangentWS.xyzw = input.tangentWS;
output.texCoord0.xyzw = input.texCoord0;
output.fogFactorAndVertexLight.xyzw = input.fogFactorAndVertexLight;
output.shadowCoord.xyzw = input.shadowCoord;
output.positionWS.xyz = input.positionWS;
output.normalWS.xyz = input.normalWS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.positionCS = input.positionCS;
#if defined(LIGHTMAP_ON)
output.lightmapUV = input.lightmapUV;
#endif
#if !defined(LIGHTMAP_ON)
output.sh = input.sh;
#endif
output.tangentWS = input.tangentWS.xyzw;
output.texCoord0 = input.texCoord0.xyzw;
output.fogFactorAndVertexLight = input.fogFactorAndVertexLight.xyzw;
output.shadowCoord = input.shadowCoord.xyzw;
output.positionWS = input.positionWS.xyz;
output.normalWS = input.normalWS.xyz;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}


// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)

float _EnvironmentReactionSupported;
float4 _EnvState0, _EnvState1;
float _FuelCoverage, _AllowFuelCoverage;
float4 _WetTint, _CharTint, _FireColor;
float4 _EnvironmentCellBounds;
float _UseEnvironmentCells;

float _Height_from_Ground_Randomness;
float _Height_from_Ground_Softness;
float _Normal_Pow;
float _Normal_Mul;
float _Mask_Map_Size;
float4 _Coverage_Normal_Map_TexelSize;
float _Mask_Map_Intensity;
float4 _Coverage_ORM_Map_TexelSize;
float _Height_Weight;
float _Height_Gradience;
float _Wind_Intensity;
float3 _Wind_Direction;
float _Wind_Speed;
float _Wind_Time_Random_Offset_Amount;
float _Texture_Size;
float4 _Color_Map_TexelSize;
float4 _Color_Tint;
float4 _Normal_Map_TexelSize;
float _Normal_Intensity;
float4 _ORM_Map_TexelSize;
float _Roughness_Intensity;
float _AO_Intensity;
float _Metallic_Intensity;
float4 _Emissive_Map_TexelSize;
float4 _Emissive_Color;
float4 _Coverage_Color_Map_TexelSize;
float _Coverage_Size;
float _Height_from_Ground;
float4 _Mask_Map_TexelSize;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Coverage_Normal_Map);
SAMPLER(sampler_Coverage_Normal_Map);
TEXTURE2D(_Coverage_ORM_Map);
SAMPLER(sampler_Coverage_ORM_Map);
TEXTURE2D(_Color_Map);
SAMPLER(sampler_Color_Map);
TEXTURE2D(_Normal_Map);
SAMPLER(sampler_Normal_Map);
TEXTURE2D(_ORM_Map);
SAMPLER(sampler_ORM_Map);
TEXTURE2D(_Emissive_Map);
SAMPLER(sampler_Emissive_Map);
TEXTURE2D(_Coverage_Color_Map);
SAMPLER(sampler_Coverage_Color_Map);
TEXTURE2D(_Mask_Map);
SAMPLER(sampler_Mask_Map);

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Includes
// GraphIncludes: <None>

// Graph Functions

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Sine_float(float In, out float Out)
{
    Out = sin(In);
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Length_float3(float3 In, out float Out)
{
    Out = length(In);
}

void Unity_Divide_float(float A, float B, out float Out)
{
    Out = A / B;
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
{
    Out = lerp(A, B, T);
}

struct Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float
{
float3 ObjectSpacePosition;
float3 TimeParameters;
};

void SG_WindSway_51c4027ae50a8cd49be53c4990b9f179_float(float _Sway_Wind_Speed, float3 _Sway_Wind_Direction, float _Sway_Wind_Intensity, float _Sway_Wind_Time_Random_Offset_Amount, Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float IN, out float3 OutVector3_1)
{
float _Property_5c4cd19861f64014ae8e16af9365ef49_Out_0_Float = _Sway_Wind_Time_Random_Offset_Amount;
float _Add_4b31033339dc4f8e8e71674d98e662a3_Out_2_Float;
Unity_Add_float(IN.TimeParameters.x, _Property_5c4cd19861f64014ae8e16af9365ef49_Out_0_Float, _Add_4b31033339dc4f8e8e71674d98e662a3_Out_2_Float);
float _Property_9a93fa589ac7409884a94247349a940f_Out_0_Float = _Sway_Wind_Speed;
float _Multiply_7418655003ae4fedb2bf68678629472c_Out_2_Float;
Unity_Multiply_float_float(_Add_4b31033339dc4f8e8e71674d98e662a3_Out_2_Float, _Property_9a93fa589ac7409884a94247349a940f_Out_0_Float, _Multiply_7418655003ae4fedb2bf68678629472c_Out_2_Float);
float _Sine_ebd14b0fc42b4b4cb4387f3a1b879c1f_Out_1_Float;
Unity_Sine_float(_Multiply_7418655003ae4fedb2bf68678629472c_Out_2_Float, _Sine_ebd14b0fc42b4b4cb4387f3a1b879c1f_Out_1_Float);
float _Add_660e92170c564f94a79277cd2fd0cf20_Out_2_Float;
Unity_Add_float(_Sine_ebd14b0fc42b4b4cb4387f3a1b879c1f_Out_1_Float, float(1), _Add_660e92170c564f94a79277cd2fd0cf20_Out_2_Float);
float _Multiply_b8fd654b771447aebafc1a6aad6957cf_Out_2_Float;
Unity_Multiply_float_float(_Add_660e92170c564f94a79277cd2fd0cf20_Out_2_Float, 0.5, _Multiply_b8fd654b771447aebafc1a6aad6957cf_Out_2_Float);
float3 _Property_fa2ee033b44946c59c3fa9aea4a167c8_Out_0_Vector3 = _Sway_Wind_Direction;
float3 _Multiply_a5500b7f7b8e43988a055eea7eb917f3_Out_2_Vector3;
Unity_Multiply_float3_float3((_Multiply_b8fd654b771447aebafc1a6aad6957cf_Out_2_Float.xxx), _Property_fa2ee033b44946c59c3fa9aea4a167c8_Out_0_Vector3, _Multiply_a5500b7f7b8e43988a055eea7eb917f3_Out_2_Vector3);
float _Property_66cba0a3223546238e18b58e017e756f_Out_0_Float = _Sway_Wind_Intensity;
float3 _Multiply_475f81423e39483cba045bef08b9f967_Out_2_Vector3;
Unity_Multiply_float3_float3(_Multiply_a5500b7f7b8e43988a055eea7eb917f3_Out_2_Vector3, (_Property_66cba0a3223546238e18b58e017e756f_Out_0_Float.xxx), _Multiply_475f81423e39483cba045bef08b9f967_Out_2_Vector3);
float3 _Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3;
Unity_Add_float3(_Multiply_475f81423e39483cba045bef08b9f967_Out_2_Vector3, IN.ObjectSpacePosition, _Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3);
float _Length_53965ffb38de43359ab019a247194557_Out_1_Float;
Unity_Length_float3(IN.ObjectSpacePosition, _Length_53965ffb38de43359ab019a247194557_Out_1_Float);
float _Length_77b6b3d742104f2abf3c6968dea9a342_Out_1_Float;
Unity_Length_float3(_Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3, _Length_77b6b3d742104f2abf3c6968dea9a342_Out_1_Float);
float _Divide_f5dd43de79e343638d2743ee93ce7326_Out_2_Float;
Unity_Divide_float(_Length_53965ffb38de43359ab019a247194557_Out_1_Float, _Length_77b6b3d742104f2abf3c6968dea9a342_Out_1_Float, _Divide_f5dd43de79e343638d2743ee93ce7326_Out_2_Float);
float3 _Lerp_7574daf550e1428cb9d35656c7bb9603_Out_3_Vector3;
Unity_Lerp_float3(float3(0, 0, 0), _Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3, (_Divide_f5dd43de79e343638d2743ee93ce7326_Out_2_Float.xxx), _Lerp_7574daf550e1428cb9d35656c7bb9603_Out_3_Vector3);
OutVector3_1 = _Lerp_7574daf550e1428cb9d35656c7bb9603_Out_3_Vector3;
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

void Unity_Power_float(float A, float B, out float Out)
{
    Out = pow(A, B);
}

void Unity_Divide_float4(float4 A, float4 B, out float4 Out)
{
    Out = A / B;
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

void Unity_Normalize_float3(float3 In, out float3 Out)
{
    Out = normalize(In);
}

void Unity_RandomRange_float(float2 Seed, float Min, float Max, out float Out)
{
     float randomno =  frac(sin(dot(Seed, float2(12.9898, 78.233)))*43758.5453);
     Out = lerp(Min, Max, randomno);
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
}

void Unity_DotProduct_float3(float3 A, float3 B, out float Out)
{
    Out = dot(A, B);
}

void Unity_Step_float(float Edge, float In, out float Out)
{
    Out = step(Edge, In);
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_NormalStrength_float(float3 In, float Strength, out float3 Out)
{
    Out = float3(In.rg * Strength, lerp(1, In.b, saturate(Strength)));
}

void Unity_Lerp_float(float A, float B, float T, out float Out)
{
    Out = lerp(A, B, T);
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 Position;
float3 Normal;
float3 Tangent;
};

VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
float _Property_b2e048f6276d4371b6ad931f5d1baa87_Out_0_Float = _Wind_Speed;
float3 _Property_7133a0551bc7429cb8bbfcbad044bf97_Out_0_Vector3 = _Wind_Direction;
float _Property_5aa2352867cd4da79dc9f9f9979925c7_Out_0_Float = _Wind_Intensity;
float _Property_e0c6ebff99e349d6966115347961a70c_Out_0_Float = _Wind_Time_Random_Offset_Amount;
Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf;
_WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf.ObjectSpacePosition = IN.ObjectSpacePosition;
_WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf.TimeParameters = IN.TimeParameters;
float3 _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf_OutVector3_1_Vector3;
SG_WindSway_51c4027ae50a8cd49be53c4990b9f179_float(_Property_b2e048f6276d4371b6ad931f5d1baa87_Out_0_Float, _Property_7133a0551bc7429cb8bbfcbad044bf97_Out_0_Vector3, _Property_5aa2352867cd4da79dc9f9f9979925c7_Out_0_Float, _Property_e0c6ebff99e349d6966115347961a70c_Out_0_Float, _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf, _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf_OutVector3_1_Vector3);
float _Split_38420bd2034746a1878048f0ead2a7b2_R_1_Float = IN.AbsoluteWorldSpacePosition[0];
float _Split_38420bd2034746a1878048f0ead2a7b2_G_2_Float = IN.AbsoluteWorldSpacePosition[1];
float _Split_38420bd2034746a1878048f0ead2a7b2_B_3_Float = IN.AbsoluteWorldSpacePosition[2];
float _Split_38420bd2034746a1878048f0ead2a7b2_A_4_Float = 0;
float3 _Vector3_0f8475b3a0df451ea79f6ea2e4e3f673_Out_0_Vector3 = float3(float(0), float(0), float(0));
float3 _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3;
{
// Converting Position from Object to AbsoluteWorld via world space
float3 world;
world = TransformObjectToWorld(_Vector3_0f8475b3a0df451ea79f6ea2e4e3f673_Out_0_Vector3.xyz);
_Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3 = GetAbsolutePositionWS(world);
}
float _Split_fb1b2d84926449e498da6c90f1436311_R_1_Float = _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3[0];
float _Split_fb1b2d84926449e498da6c90f1436311_G_2_Float = _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3[1];
float _Split_fb1b2d84926449e498da6c90f1436311_B_3_Float = _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3[2];
float _Split_fb1b2d84926449e498da6c90f1436311_A_4_Float = 0;
float _Subtract_18ddaa9851a547f1bc3eeafa55f3227b_Out_2_Float;
Unity_Subtract_float(_Split_38420bd2034746a1878048f0ead2a7b2_G_2_Float, _Split_fb1b2d84926449e498da6c90f1436311_G_2_Float, _Subtract_18ddaa9851a547f1bc3eeafa55f3227b_Out_2_Float);
float _Property_1db6873c977b47f186e42eef2cd75a57_Out_0_Float = _Height_Weight;
float _Split_bc5987163dba418d8de90e985de1247b_R_1_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[0];
float _Split_bc5987163dba418d8de90e985de1247b_G_2_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[1];
float _Split_bc5987163dba418d8de90e985de1247b_B_3_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[2];
float _Split_bc5987163dba418d8de90e985de1247b_A_4_Float = 0;
float _Multiply_89de5a5fb3cd451abb1227deba019c38_Out_2_Float;
Unity_Multiply_float_float(_Property_1db6873c977b47f186e42eef2cd75a57_Out_0_Float, _Split_bc5987163dba418d8de90e985de1247b_G_2_Float, _Multiply_89de5a5fb3cd451abb1227deba019c38_Out_2_Float);
float _Divide_2c406de6c63e463f9013e4faf1db1ae8_Out_2_Float;
Unity_Divide_float(_Subtract_18ddaa9851a547f1bc3eeafa55f3227b_Out_2_Float, _Multiply_89de5a5fb3cd451abb1227deba019c38_Out_2_Float, _Divide_2c406de6c63e463f9013e4faf1db1ae8_Out_2_Float);
float _Saturate_e9ea645ca89f482a9d0315df54a77ee5_Out_1_Float;
Unity_Saturate_float(_Divide_2c406de6c63e463f9013e4faf1db1ae8_Out_2_Float, _Saturate_e9ea645ca89f482a9d0315df54a77ee5_Out_1_Float);
float _Property_e96fd1920dff4533951da684129df200_Out_0_Float = _Height_Gradience;
float _Power_b88c5abc85424f17b03c60c98e9bc16c_Out_2_Float;
Unity_Power_float(_Saturate_e9ea645ca89f482a9d0315df54a77ee5_Out_1_Float, _Property_e96fd1920dff4533951da684129df200_Out_0_Float, _Power_b88c5abc85424f17b03c60c98e9bc16c_Out_2_Float);
float _Saturate_6c35d0fa108b483783bde88e06ff5ce0_Out_1_Float;
Unity_Saturate_float(_Power_b88c5abc85424f17b03c60c98e9bc16c_Out_2_Float, _Saturate_6c35d0fa108b483783bde88e06ff5ce0_Out_1_Float);
float3 _Lerp_8461a67d418941ee81ed9eb0f80e8a38_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf_OutVector3_1_Vector3, (_Saturate_6c35d0fa108b483783bde88e06ff5ce0_Out_1_Float.xxx), _Lerp_8461a67d418941ee81ed9eb0f80e8a38_Out_3_Vector3);
#if defined(_USE_WPO)
float3 _UseWPO_81cb92bff33547fbbfaa5ce2f60ba97c_Out_0_Vector3 = _Lerp_8461a67d418941ee81ed9eb0f80e8a38_Out_3_Vector3;
#else
float3 _UseWPO_81cb92bff33547fbbfaa5ce2f60ba97c_Out_0_Vector3 = IN.ObjectSpacePosition;
#endif
description.Position = _UseWPO_81cb92bff33547fbbfaa5ce2f60ba97c_Out_0_Vector3;
description.Normal = IN.ObjectSpaceNormal;
description.Tangent = IN.ObjectSpaceTangent;
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
float3 BaseColor;
float3 NormalTS;
float3 Emission;
float Metallic;
float Smoothness;
float Occlusion;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
UnityTexture2D _Property_0c79649b4042462682b4a7423715f1a1_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Color_Map, sampler_Color_Map, _Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_a9551365617a4a66b75c6a49ab8f9c5f_Out_0_Vector4 = IN.uv0;
float _Property_ac4a3a6f20bd42789825abf989f1a228_Out_0_Float = _Texture_Size;
float4 _Divide_1ff147fe44c540028660935d59272eaa_Out_2_Vector4;
Unity_Divide_float4(_UV_a9551365617a4a66b75c6a49ab8f9c5f_Out_0_Vector4, (_Property_ac4a3a6f20bd42789825abf989f1a228_Out_0_Float.xxxx), _Divide_1ff147fe44c540028660935d59272eaa_Out_2_Vector4);
float4 _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_0c79649b4042462682b4a7423715f1a1_Out_0_Texture2D.tex, _Property_0c79649b4042462682b4a7423715f1a1_Out_0_Texture2D.samplerstate, _Property_0c79649b4042462682b4a7423715f1a1_Out_0_Texture2D.GetTransformedUV((_Divide_1ff147fe44c540028660935d59272eaa_Out_2_Vector4.xy)) );
if (_Property_0c79649b4042462682b4a7423715f1a1_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4, _Property_0c79649b4042462682b4a7423715f1a1_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_R_4_Float = _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4.r;
float _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_G_5_Float = _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4.g;
float _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_B_6_Float = _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4.b;
float _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_A_7_Float = _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4.a;
float4 _Property_11be52d8078b41908ba72022455e8b98_Out_0_Vector4 = _Color_Tint;
float4 _Multiply_e3ef32e4a4e8442188dfd3c93394960c_Out_2_Vector4;
Unity_Multiply_float4_float4(_SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4, _Property_11be52d8078b41908ba72022455e8b98_Out_0_Vector4, _Multiply_e3ef32e4a4e8442188dfd3c93394960c_Out_2_Vector4);
UnityTexture2D _Property_415df38a141841e6aa11a7b1e336c379_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Coverage_Color_Map, sampler_Coverage_Color_Map, _Coverage_Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_0805a5601f5b49beb942e50ab3714e03_Out_0_Vector4 = IN.uv0;
float _Property_eb7418710f3f4dc6981c01445b1a809e_Out_0_Float = _Coverage_Size;
float4 _Divide_71dd940938164d5a9e7020e44b1cddf3_Out_2_Vector4;
Unity_Divide_float4(_UV_0805a5601f5b49beb942e50ab3714e03_Out_0_Vector4, (_Property_eb7418710f3f4dc6981c01445b1a809e_Out_0_Float.xxxx), _Divide_71dd940938164d5a9e7020e44b1cddf3_Out_2_Vector4);
float4 _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_415df38a141841e6aa11a7b1e336c379_Out_0_Texture2D.tex, _Property_415df38a141841e6aa11a7b1e336c379_Out_0_Texture2D.samplerstate, _Property_415df38a141841e6aa11a7b1e336c379_Out_0_Texture2D.GetTransformedUV((_Divide_71dd940938164d5a9e7020e44b1cddf3_Out_2_Vector4.xy)) );
if (_Property_415df38a141841e6aa11a7b1e336c379_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4, _Property_415df38a141841e6aa11a7b1e336c379_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_R_4_Float = _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4.r;
float _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_G_5_Float = _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4.g;
float _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_B_6_Float = _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4.b;
float _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_A_7_Float = _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4.a;
UnityTexture2D _Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Mask_Map, sampler_Mask_Map, _Mask_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_c2c5030522744aceb603ff104c9ccde0_Out_0_Vector4 = IN.uv0;
float _Property_a975da4683af46ab85b4a95b01cb9f32_Out_0_Float = _Mask_Map_Size;
float4 _Divide_3dd829d125dc4ec1b32772766d56a17b_Out_2_Vector4;
Unity_Divide_float4(_UV_c2c5030522744aceb603ff104c9ccde0_Out_0_Vector4, (_Property_a975da4683af46ab85b4a95b01cb9f32_Out_0_Float.xxxx), _Divide_3dd829d125dc4ec1b32772766d56a17b_Out_2_Vector4);
float4 _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D.tex, _Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D.samplerstate, _Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D.GetTransformedUV((_Divide_3dd829d125dc4ec1b32772766d56a17b_Out_2_Vector4.xy)) );
if (_Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4, _Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_R_4_Float = _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4.r;
float _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_G_5_Float = _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4.g;
float _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_B_6_Float = _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4.b;
float _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_A_7_Float = _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4.a;
float _Multiply_81f5f8bc82304adbae345a5fe4b0024b_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_R_4_Float, 0.1, _Multiply_81f5f8bc82304adbae345a5fe4b0024b_Out_2_Float);
float _Add_33507c032b9e48bfb61763d507bb031c_Out_2_Float;
Unity_Add_float(_Multiply_81f5f8bc82304adbae345a5fe4b0024b_Out_2_Float, float(0.9), _Add_33507c032b9e48bfb61763d507bb031c_Out_2_Float);
float _Property_883f8e1288fd43f1a3674ca1f5364253_Out_0_Float = _Mask_Map_Intensity;
float _Power_b45fec9ffb074e40b5cb5452c0fb982d_Out_2_Float;
Unity_Power_float(_Add_33507c032b9e48bfb61763d507bb031c_Out_2_Float, _Property_883f8e1288fd43f1a3674ca1f5364253_Out_0_Float, _Power_b45fec9ffb074e40b5cb5452c0fb982d_Out_2_Float);
float _Split_b6a9cc69e69c417ba235626d7beed2eb_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_b6a9cc69e69c417ba235626d7beed2eb_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_b6a9cc69e69c417ba235626d7beed2eb_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_b6a9cc69e69c417ba235626d7beed2eb_A_4_Float = 0;
float _Property_73ac42b2cd3d4b43976ac13e73b6de40_Out_0_Float = _Height_from_Ground;
float3 _Transform_425aee5b4b5e409eb94b431642b515fb_Out_1_Vector3;
_Transform_425aee5b4b5e409eb94b431642b515fb_Out_1_Vector3 = TransformObjectToWorld(float3 (0, 0, 0).xyz);
float3 _Normalize_960b1f24c996479a8c1a9a81cabcb835_Out_1_Vector3;
Unity_Normalize_float3(_Transform_425aee5b4b5e409eb94b431642b515fb_Out_1_Vector3, _Normalize_960b1f24c996479a8c1a9a81cabcb835_Out_1_Vector3);
float _Property_11f1157c9f364521bf765c261e8bb93e_Out_0_Float = _Height_from_Ground_Randomness;
float _Multiply_b7a3ed973f104b9a84af846ce299d95f_Out_2_Float;
Unity_Multiply_float_float(_Property_11f1157c9f364521bf765c261e8bb93e_Out_0_Float, -1, _Multiply_b7a3ed973f104b9a84af846ce299d95f_Out_2_Float);
float _RandomRange_82fa55b7f4da4f6ebc597a38d2ca2e6d_Out_3_Float;
Unity_RandomRange_float((_Normalize_960b1f24c996479a8c1a9a81cabcb835_Out_1_Vector3.xy), _Multiply_b7a3ed973f104b9a84af846ce299d95f_Out_2_Float, _Property_11f1157c9f364521bf765c261e8bb93e_Out_0_Float, _RandomRange_82fa55b7f4da4f6ebc597a38d2ca2e6d_Out_3_Float);
float _Add_109ea06d643c4826b0138a85becc2fa9_Out_2_Float;
Unity_Add_float(_Property_73ac42b2cd3d4b43976ac13e73b6de40_Out_0_Float, _RandomRange_82fa55b7f4da4f6ebc597a38d2ca2e6d_Out_3_Float, _Add_109ea06d643c4826b0138a85becc2fa9_Out_2_Float);
float _Subtract_fe60462bdbdd4a22a207774a6a8cb1b8_Out_2_Float;
Unity_Subtract_float(_Split_b6a9cc69e69c417ba235626d7beed2eb_G_2_Float, _Add_109ea06d643c4826b0138a85becc2fa9_Out_2_Float, _Subtract_fe60462bdbdd4a22a207774a6a8cb1b8_Out_2_Float);
float _Property_1487773737e44e78a7c514c7f470e603_Out_0_Float = _Height_from_Ground_Softness;
float _Divide_6eb043a8aee54d87a0c73169597bdcba_Out_2_Float;
Unity_Divide_float(_Subtract_fe60462bdbdd4a22a207774a6a8cb1b8_Out_2_Float, _Property_1487773737e44e78a7c514c7f470e603_Out_0_Float, _Divide_6eb043a8aee54d87a0c73169597bdcba_Out_2_Float);
float _Saturate_d8ec125670414d1888aeec654fcb04e2_Out_1_Float;
Unity_Saturate_float(_Divide_6eb043a8aee54d87a0c73169597bdcba_Out_2_Float, _Saturate_d8ec125670414d1888aeec654fcb04e2_Out_1_Float);
float _OneMinus_de85154fa1b742e3b6c9e0f92b85d96c_Out_1_Float;
Unity_OneMinus_float(_Saturate_d8ec125670414d1888aeec654fcb04e2_Out_1_Float, _OneMinus_de85154fa1b742e3b6c9e0f92b85d96c_Out_1_Float);
float _DotProduct_a5624fdd7fd94a4c82ad8c039922e638_Out_2_Float;
Unity_DotProduct_float3(IN.WorldSpaceNormal, float3(0, 1, 0), _DotProduct_a5624fdd7fd94a4c82ad8c039922e638_Out_2_Float);
float _Property_1d95c04e88f443bc91fe7ed217b674b8_Out_0_Float = _Normal_Pow;
float _Power_5b713b10f3e049f8a97be81969b52b7d_Out_2_Float;
Unity_Power_float(_DotProduct_a5624fdd7fd94a4c82ad8c039922e638_Out_2_Float, _Property_1d95c04e88f443bc91fe7ed217b674b8_Out_0_Float, _Power_5b713b10f3e049f8a97be81969b52b7d_Out_2_Float);
float _Property_6314206eb47742698a7d2833f119f39e_Out_0_Float = _Normal_Mul;
float _Multiply_6e6bcfc2a4df414e86d241952840e972_Out_2_Float;
Unity_Multiply_float_float(_Power_5b713b10f3e049f8a97be81969b52b7d_Out_2_Float, _Property_6314206eb47742698a7d2833f119f39e_Out_0_Float, _Multiply_6e6bcfc2a4df414e86d241952840e972_Out_2_Float);
float _Saturate_77d41005224e417eb678b78b9a10771a_Out_1_Float;
Unity_Saturate_float(_Multiply_6e6bcfc2a4df414e86d241952840e972_Out_2_Float, _Saturate_77d41005224e417eb678b78b9a10771a_Out_1_Float);
float _Add_8395f84ea8e546988dd02e5b922d32ee_Out_2_Float;
Unity_Add_float(_OneMinus_de85154fa1b742e3b6c9e0f92b85d96c_Out_1_Float, _Saturate_77d41005224e417eb678b78b9a10771a_Out_1_Float, _Add_8395f84ea8e546988dd02e5b922d32ee_Out_2_Float);
float _Multiply_b61ead58dd024fd7aacad7c0b68f3be7_Out_2_Float;
Unity_Multiply_float_float(5, _Add_8395f84ea8e546988dd02e5b922d32ee_Out_2_Float, _Multiply_b61ead58dd024fd7aacad7c0b68f3be7_Out_2_Float);
float _Multiply_b0c3a49303544ccaa1d15379e6b1e9d2_Out_2_Float;
Unity_Multiply_float_float(_Power_b45fec9ffb074e40b5cb5452c0fb982d_Out_2_Float, _Multiply_b61ead58dd024fd7aacad7c0b68f3be7_Out_2_Float, _Multiply_b0c3a49303544ccaa1d15379e6b1e9d2_Out_2_Float);
float _Step_9e404f0c811346c88c531f42e623243f_Out_2_Float;
Unity_Step_float(_Subtract_fe60462bdbdd4a22a207774a6a8cb1b8_Out_2_Float, float(-0.1), _Step_9e404f0c811346c88c531f42e623243f_Out_2_Float);
float _Add_0328340190574386b2d37a0d035b66fb_Out_2_Float;
Unity_Add_float(_Multiply_b0c3a49303544ccaa1d15379e6b1e9d2_Out_2_Float, _Step_9e404f0c811346c88c531f42e623243f_Out_2_Float, _Add_0328340190574386b2d37a0d035b66fb_Out_2_Float);
float _Power_3bfc30934311465db3ba28e64b49097d_Out_2_Float;
Unity_Power_float(_Add_0328340190574386b2d37a0d035b66fb_Out_2_Float, float(3), _Power_3bfc30934311465db3ba28e64b49097d_Out_2_Float);
float _Saturate_3ba4a70af5214b4086803937a3f8b09f_Out_1_Float;
Unity_Saturate_float(_Power_3bfc30934311465db3ba28e64b49097d_Out_2_Float, _Saturate_3ba4a70af5214b4086803937a3f8b09f_Out_1_Float);
float _Saturate_6e423c9f5f1249088750a2b84a42b764_Out_1_Float;
Unity_Saturate_float(_Add_8395f84ea8e546988dd02e5b922d32ee_Out_2_Float, _Saturate_6e423c9f5f1249088750a2b84a42b764_Out_1_Float);
#if defined(_USE_COVERAGE_MASK_TEXTURE)
float _UseCoverageMaskTexture_f75c00eee68746bd80e2fa6c2bc28d4c_Out_0_Float = _Saturate_3ba4a70af5214b4086803937a3f8b09f_Out_1_Float;
#else
float _UseCoverageMaskTexture_f75c00eee68746bd80e2fa6c2bc28d4c_Out_0_Float = _Saturate_6e423c9f5f1249088750a2b84a42b764_Out_1_Float;
#endif
float4 _Lerp_c6c7a7472707418491f2fa10989f05b9_Out_3_Vector4;
Unity_Lerp_float4(_Multiply_e3ef32e4a4e8442188dfd3c93394960c_Out_2_Vector4, _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4, (_UseCoverageMaskTexture_f75c00eee68746bd80e2fa6c2bc28d4c_Out_0_Float.xxxx), _Lerp_c6c7a7472707418491f2fa10989f05b9_Out_3_Vector4);
#if defined(_USE_COVERAGE)
float4 _UseCoverage_c2f2c567080e4d5f9b652fbcbf667c54_Out_0_Vector4 = _Lerp_c6c7a7472707418491f2fa10989f05b9_Out_3_Vector4;
#else
float4 _UseCoverage_c2f2c567080e4d5f9b652fbcbf667c54_Out_0_Vector4 = _Multiply_e3ef32e4a4e8442188dfd3c93394960c_Out_2_Vector4;
#endif
UnityTexture2D _Property_a38bf1e56c364aa0add1f01a99eb6c3e_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Normal_Map, sampler_Normal_Map, _Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_a38bf1e56c364aa0add1f01a99eb6c3e_Out_0_Texture2D.tex, _Property_a38bf1e56c364aa0add1f01a99eb6c3e_Out_0_Texture2D.samplerstate, _Property_a38bf1e56c364aa0add1f01a99eb6c3e_Out_0_Texture2D.GetTransformedUV((_Divide_1ff147fe44c540028660935d59272eaa_Out_2_Vector4.xy)) );
if (_Property_a38bf1e56c364aa0add1f01a99eb6c3e_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4, _Property_a38bf1e56c364aa0add1f01a99eb6c3e_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4);
float _SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_R_4_Float = _SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4.r;
float _SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_G_5_Float = _SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4.g;
float _SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_B_6_Float = _SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4.b;
float _SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_A_7_Float = _SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4.a;
float _Property_ff6f57141462418790b8f0ef1d7d69cc_Out_0_Float = _Normal_Intensity;
float3 _NormalStrength_6e1c84dc8cbd47f988a654d24e83669f_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4.xyz), _Property_ff6f57141462418790b8f0ef1d7d69cc_Out_0_Float, _NormalStrength_6e1c84dc8cbd47f988a654d24e83669f_Out_2_Vector3);
#if defined(_USE_NORMAL_MAPS)
float3 _UseNormalMaps_f01546d53fa54c3191638b3fe3736688_Out_0_Vector3 = _NormalStrength_6e1c84dc8cbd47f988a654d24e83669f_Out_2_Vector3;
#else
float3 _UseNormalMaps_f01546d53fa54c3191638b3fe3736688_Out_0_Vector3 = float3(0, 0, 1);
#endif
UnityTexture2D _Property_15070824f15143aea7ede9b130164716_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Coverage_Normal_Map, sampler_Coverage_Normal_Map, _Coverage_Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_15070824f15143aea7ede9b130164716_Out_0_Texture2D.tex, _Property_15070824f15143aea7ede9b130164716_Out_0_Texture2D.samplerstate, _Property_15070824f15143aea7ede9b130164716_Out_0_Texture2D.GetTransformedUV((_Divide_71dd940938164d5a9e7020e44b1cddf3_Out_2_Vector4.xy)) );
if (_Property_15070824f15143aea7ede9b130164716_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4, _Property_15070824f15143aea7ede9b130164716_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4);
float _SampleTexture2D_26c139222fa1483d83940748db6d922f_R_4_Float = _SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4.r;
float _SampleTexture2D_26c139222fa1483d83940748db6d922f_G_5_Float = _SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4.g;
float _SampleTexture2D_26c139222fa1483d83940748db6d922f_B_6_Float = _SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4.b;
float _SampleTexture2D_26c139222fa1483d83940748db6d922f_A_7_Float = _SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4.a;
float3 _Lerp_a5163d5f017c46b9b347647a7d361156_Out_3_Vector3;
Unity_Lerp_float3(_UseNormalMaps_f01546d53fa54c3191638b3fe3736688_Out_0_Vector3, (_SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4.xyz), (_UseCoverageMaskTexture_f75c00eee68746bd80e2fa6c2bc28d4c_Out_0_Float.xxx), _Lerp_a5163d5f017c46b9b347647a7d361156_Out_3_Vector3);
#if defined(_USE_COVERAGE)
float3 _UseCoverage_d6109394a0154e89890cf6ca228f700f_Out_0_Vector3 = _Lerp_a5163d5f017c46b9b347647a7d361156_Out_3_Vector3;
#else
float3 _UseCoverage_d6109394a0154e89890cf6ca228f700f_Out_0_Vector3 = _UseNormalMaps_f01546d53fa54c3191638b3fe3736688_Out_0_Vector3;
#endif
#if defined(_USE_NORMAL_MAPS)
float3 _UseNormalMaps_cf0fe6c3e8224d69a92ff77f7f270e12_Out_0_Vector3 = _UseCoverage_d6109394a0154e89890cf6ca228f700f_Out_0_Vector3;
#else
float3 _UseNormalMaps_cf0fe6c3e8224d69a92ff77f7f270e12_Out_0_Vector3 = float3(0, 0, 1);
#endif
UnityTexture2D _Property_c7dc38eaeea545aaada0e67cc32d9d25_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Emissive_Map, sampler_Emissive_Map, _Emissive_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_c7dc38eaeea545aaada0e67cc32d9d25_Out_0_Texture2D.tex, _Property_c7dc38eaeea545aaada0e67cc32d9d25_Out_0_Texture2D.samplerstate, _Property_c7dc38eaeea545aaada0e67cc32d9d25_Out_0_Texture2D.GetTransformedUV((_Divide_1ff147fe44c540028660935d59272eaa_Out_2_Vector4.xy)) );
if (_Property_c7dc38eaeea545aaada0e67cc32d9d25_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4, _Property_c7dc38eaeea545aaada0e67cc32d9d25_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_R_4_Float = _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4.r;
float _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_G_5_Float = _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4.g;
float _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_B_6_Float = _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4.b;
float _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_A_7_Float = _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4.a;
float4 _Property_857cbfa6469047f8904e0adc3eefe6b8_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_Emissive_Color) : _Emissive_Color;
float4 _Multiply_293fcf7127ab419cbcdff4183cccdba0_Out_2_Vector4;
Unity_Multiply_float4_float4(_SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4, _Property_857cbfa6469047f8904e0adc3eefe6b8_Out_0_Vector4, _Multiply_293fcf7127ab419cbcdff4183cccdba0_Out_2_Vector4);
#if defined(_USE_EMISSIVE)
float4 _UseEmissive_fdf874f7398e43a7838e4f04fd255375_Out_0_Vector4 = _Multiply_293fcf7127ab419cbcdff4183cccdba0_Out_2_Vector4;
#else
float4 _UseEmissive_fdf874f7398e43a7838e4f04fd255375_Out_0_Vector4 = float4(0, 0, 0, 0);
#endif
float _Property_3cef5a13363c421c89eb005848414161_Out_0_Float = _Metallic_Intensity;
UnityTexture2D _Property_6ced109efdc542d6868e0f24e23a1736_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_ORM_Map, sampler_ORM_Map, _ORM_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_6ced109efdc542d6868e0f24e23a1736_Out_0_Texture2D.tex, _Property_6ced109efdc542d6868e0f24e23a1736_Out_0_Texture2D.samplerstate, _Property_6ced109efdc542d6868e0f24e23a1736_Out_0_Texture2D.GetTransformedUV((_Divide_1ff147fe44c540028660935d59272eaa_Out_2_Vector4.xy)) );
if (_Property_6ced109efdc542d6868e0f24e23a1736_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_RGBA_0_Vector4, _Property_6ced109efdc542d6868e0f24e23a1736_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_R_4_Float = _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_RGBA_0_Vector4.r;
float _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_G_5_Float = _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_RGBA_0_Vector4.g;
float _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_B_6_Float = _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_RGBA_0_Vector4.b;
float _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_A_7_Float = _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_RGBA_0_Vector4.a;
float _Multiply_ffdbad83b5e74ce1b5ca4b9e7e162c40_Out_2_Float;
Unity_Multiply_float_float(_Property_3cef5a13363c421c89eb005848414161_Out_0_Float, _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_B_6_Float, _Multiply_ffdbad83b5e74ce1b5ca4b9e7e162c40_Out_2_Float);
UnityTexture2D _Property_75507ddd24ab4c63b88da2bbaec94f2c_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Coverage_ORM_Map, sampler_Coverage_ORM_Map, _Coverage_ORM_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_75507ddd24ab4c63b88da2bbaec94f2c_Out_0_Texture2D.tex, _Property_75507ddd24ab4c63b88da2bbaec94f2c_Out_0_Texture2D.samplerstate, _Property_75507ddd24ab4c63b88da2bbaec94f2c_Out_0_Texture2D.GetTransformedUV((_Divide_71dd940938164d5a9e7020e44b1cddf3_Out_2_Vector4.xy)) );
if (_Property_75507ddd24ab4c63b88da2bbaec94f2c_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_RGBA_0_Vector4, _Property_75507ddd24ab4c63b88da2bbaec94f2c_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_R_4_Float = _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_RGBA_0_Vector4.r;
float _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_G_5_Float = _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_RGBA_0_Vector4.g;
float _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_B_6_Float = _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_RGBA_0_Vector4.b;
float _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_A_7_Float = _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_RGBA_0_Vector4.a;
float _Lerp_3101e882d6a14372973d46e58ded048f_Out_3_Float;
Unity_Lerp_float(_Multiply_ffdbad83b5e74ce1b5ca4b9e7e162c40_Out_2_Float, _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_B_6_Float, _UseCoverageMaskTexture_f75c00eee68746bd80e2fa6c2bc28d4c_Out_0_Float, _Lerp_3101e882d6a14372973d46e58ded048f_Out_3_Float);
#if defined(_USE_COVERAGE)
float _UseCoverage_5268f0597a8b4df2a05cf3d5da317f6b_Out_0_Float = _Lerp_3101e882d6a14372973d46e58ded048f_Out_3_Float;
#else
float _UseCoverage_5268f0597a8b4df2a05cf3d5da317f6b_Out_0_Float = _Multiply_ffdbad83b5e74ce1b5ca4b9e7e162c40_Out_2_Float;
#endif
float _Property_d066edd24a97480b83f88a08c9a274b8_Out_0_Float = _Metallic_Intensity;
#if defined(_USE_ORM)
float _UseORM_11ec65ff6fc7436b9d5074e8f7a90024_Out_0_Float = _UseCoverage_5268f0597a8b4df2a05cf3d5da317f6b_Out_0_Float;
#else
float _UseORM_11ec65ff6fc7436b9d5074e8f7a90024_Out_0_Float = _Property_d066edd24a97480b83f88a08c9a274b8_Out_0_Float;
#endif
float _Property_ef0d0e2cdec043359496bb1a0782cda9_Out_0_Float = _Roughness_Intensity;
float _Multiply_4b10161d594b4863a7b31e2a1f1c1cf1_Out_2_Float;
Unity_Multiply_float_float(_Property_ef0d0e2cdec043359496bb1a0782cda9_Out_0_Float, _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_G_5_Float, _Multiply_4b10161d594b4863a7b31e2a1f1c1cf1_Out_2_Float);
float _OneMinus_d7dd0fb677e94540bbc094136ca963f5_Out_1_Float;
Unity_OneMinus_float(_Multiply_4b10161d594b4863a7b31e2a1f1c1cf1_Out_2_Float, _OneMinus_d7dd0fb677e94540bbc094136ca963f5_Out_1_Float);
float _OneMinus_bae441d794cd4092861f8242b9f96358_Out_1_Float;
Unity_OneMinus_float(_SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_G_5_Float, _OneMinus_bae441d794cd4092861f8242b9f96358_Out_1_Float);
float _Lerp_377c896eaf3243859da5c9b1446d27f5_Out_3_Float;
Unity_Lerp_float(_OneMinus_d7dd0fb677e94540bbc094136ca963f5_Out_1_Float, _OneMinus_bae441d794cd4092861f8242b9f96358_Out_1_Float, _UseCoverageMaskTexture_f75c00eee68746bd80e2fa6c2bc28d4c_Out_0_Float, _Lerp_377c896eaf3243859da5c9b1446d27f5_Out_3_Float);
#if defined(_USE_COVERAGE)
float _UseCoverage_58ec20069d0a4e0cb494b690fec0b60b_Out_0_Float = _Lerp_377c896eaf3243859da5c9b1446d27f5_Out_3_Float;
#else
float _UseCoverage_58ec20069d0a4e0cb494b690fec0b60b_Out_0_Float = _OneMinus_d7dd0fb677e94540bbc094136ca963f5_Out_1_Float;
#endif
float _Property_a7403d2892854317b50b8fa0268d0f27_Out_0_Float = _Roughness_Intensity;
float _OneMinus_3b59d96fc25b4b00a0364fda5bd47109_Out_1_Float;
Unity_OneMinus_float(_Property_a7403d2892854317b50b8fa0268d0f27_Out_0_Float, _OneMinus_3b59d96fc25b4b00a0364fda5bd47109_Out_1_Float);
#if defined(_USE_ORM)
float _UseORM_2aa3605b11694e28a61f126281dcd8f3_Out_0_Float = _UseCoverage_58ec20069d0a4e0cb494b690fec0b60b_Out_0_Float;
#else
float _UseORM_2aa3605b11694e28a61f126281dcd8f3_Out_0_Float = _OneMinus_3b59d96fc25b4b00a0364fda5bd47109_Out_1_Float;
#endif
float _Property_b578c28d617c46edb5452f0ee34e3698_Out_0_Float = _AO_Intensity;
float _Lerp_11f21031558c4c7db3e9b583ee8c5843_Out_3_Float;
Unity_Lerp_float(float(1), _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_R_4_Float, _Property_b578c28d617c46edb5452f0ee34e3698_Out_0_Float, _Lerp_11f21031558c4c7db3e9b583ee8c5843_Out_3_Float);
float _Lerp_f01b6ad8ed5e4f7aa0a392bc0bd1b199_Out_3_Float;
Unity_Lerp_float(_Lerp_11f21031558c4c7db3e9b583ee8c5843_Out_3_Float, _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_R_4_Float, _UseCoverageMaskTexture_f75c00eee68746bd80e2fa6c2bc28d4c_Out_0_Float, _Lerp_f01b6ad8ed5e4f7aa0a392bc0bd1b199_Out_3_Float);
#if defined(_USE_COVERAGE)
float _UseCoverage_ae6eaa1f25704217b662768dd49ad504_Out_0_Float = _Lerp_f01b6ad8ed5e4f7aa0a392bc0bd1b199_Out_3_Float;
#else
float _UseCoverage_ae6eaa1f25704217b662768dd49ad504_Out_0_Float = _Lerp_11f21031558c4c7db3e9b583ee8c5843_Out_3_Float;
#endif
#if defined(_USE_ORM)
float _UseORM_a8f4959fe3174af9bd41a7335d24f10e_Out_0_Float = _UseCoverage_ae6eaa1f25704217b662768dd49ad504_Out_0_Float;
#else
float _UseORM_a8f4959fe3174af9bd41a7335d24f10e_Out_0_Float = float(1);
#endif
surface.BaseColor = (_UseCoverage_c2f2c567080e4d5f9b652fbcbf667c54_Out_0_Vector4.xyz);
surface.NormalTS = _UseNormalMaps_cf0fe6c3e8224d69a92ff77f7f270e12_Out_0_Vector3;
surface.Emission = (_UseEmissive_fdf874f7398e43a7838e4f04fd255375_Out_0_Vector4.xyz);
surface.Metallic = _UseORM_11ec65ff6fc7436b9d5074e8f7a90024_Out_0_Float;
surface.Smoothness = _UseORM_2aa3605b11694e28a61f126281dcd8f3_Out_0_Float;
surface.Occlusion = _UseORM_a8f4959fe3174af9bd41a7335d24f10e_Out_0_Float;
float3 reactionUnusedColor = 0, reactionUnusedEmission = 0;
ClipVegetationFuel(TransformObjectToWorld(float3(0,0,0)));
ApplyVegetationReaction(TransformObjectToWorld(float3(0,0,0)),surface.BaseColor,surface.Emission);
return surface;
}

// --------------------------------------------------
// Build Graph Inputs

VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

    output.ObjectSpaceNormal =                          input.normalOS;
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
    output.AbsoluteWorldSpacePosition =                 GetAbsolutePositionWS(TransformObjectToWorld(input.positionOS));
    output.TimeParameters =                             _TimeParameters.xyz;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

    

    // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
    float3 unnormalizedNormalWS = input.normalWS;
    const float renormFactor = 1.0 / length(unnormalizedNormalWS);


    output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph
    output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);


    output.ObjectSpacePosition = TransformWorldToObject(input.positionWS);

    #if UNITY_UV_STARTS_AT_TOP
    #else
    #endif


    output.uv0 = input.texCoord0;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

void BuildAppDataFull(Attributes attributes, VertexDescription vertexDescription, inout appdata_full result)
{
    result.vertex     = float4(attributes.positionOS, 1);
    result.tangent    = attributes.tangentOS;
    result.normal     = attributes.normalOS;
    result.texcoord   = attributes.uv0;
    result.texcoord1  = attributes.uv1;
    result.vertex     = float4(vertexDescription.Position, 1);
    result.normal     = vertexDescription.Normal;
    result.tangent    = float4(vertexDescription.Tangent, 0);
    #if UNITY_ANY_INSTANCING_ENABLED
    #endif
}

void VaryingsToSurfaceVertex(Varyings varyings, inout v2f_surf result)
{
    result.pos = varyings.positionCS;
    result.worldPos = varyings.positionWS;
    result.worldNormal = varyings.normalWS;
    // World Tangent isn't an available input on v2f_surf

    result._ShadowCoord = varyings.shadowCoord;

    #if UNITY_ANY_INSTANCING_ENABLED
    #endif
    #if UNITY_SHOULD_SAMPLE_SH
    #if !defined(LIGHTMAP_ON)
    result.sh = varyings.sh;
    #endif
    #endif
    #if defined(LIGHTMAP_ON)
    result.lmap.xy = varyings.lightmapUV;
    #endif
    #ifdef VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
        result.fogCoord = varyings.fogFactorAndVertexLight.x;
        COPY_TO_LIGHT_COORDS(result, varyings.fogFactorAndVertexLight.yzw);
    #endif

    DEFAULT_UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(varyings, result);
}

void SurfaceVertexToVaryings(v2f_surf surfVertex, inout Varyings result)
{
    result.positionCS = surfVertex.pos;
    result.positionWS = surfVertex.worldPos;
    result.normalWS = surfVertex.worldNormal;
    // viewDirectionWS is never filled out in the legacy pass' function. Always use the value computed by SRP
    // World Tangent isn't an available input on v2f_surf
    result.shadowCoord = surfVertex._ShadowCoord;

    #if UNITY_ANY_INSTANCING_ENABLED
    #endif
    #if UNITY_SHOULD_SAMPLE_SH
    #if !defined(LIGHTMAP_ON)
    result.sh = surfVertex.sh;
    #endif
    #endif
    #if defined(LIGHTMAP_ON)
    result.lightmapUV = surfVertex.lmap.xy;
    #endif
    #ifdef VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
        result.fogFactorAndVertexLight.x = surfVertex.fogCoord;
        COPY_FROM_LIGHT_COORDS(result.fogFactorAndVertexLight.yzw, surfVertex);
    #endif

    DEFAULT_UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(surfVertex, result);
}

// --------------------------------------------------
// Main

#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/Varyings.hlsl"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/PBRForwardAddPass.hlsl"

ENDHLSL
}
Pass
{
    Name "BuiltIn Deferred"
    Tags
    {
        "LightMode" = "Deferred"
    }

// Render State
Cull Back
Blend One Zero
ZTest LEqual
ZWrite On

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 4.5
#pragma multi_compile_instancing
#pragma exclude_renderers nomrt
#pragma multi_compile_prepassfinal
#pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
#pragma vertex vert
#pragma fragment frag

// Keywords
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
#pragma multi_compile _ _SHADOWS_SOFT
#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
#pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
#pragma multi_compile _ _GBUFFER_NORMALS_OCT
#pragma shader_feature_local _ _USE_EMISSIVE
#pragma shader_feature_local _ _USE_WPO
#pragma shader_feature_local _ _USE_ORM
#pragma shader_feature_local _ _USE_NORMAL_MAPS
#pragma shader_feature_local _ _USE_COVERAGE
#pragma shader_feature_local _ _USE_COVERAGE_MASK_TEXTURE

#if defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_2
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_3
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_4
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_5
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_6
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM)
#define KEYWORD_PERMUTATION_7
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_8
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_9
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_10
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_11
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_12
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_13
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_14
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO)
#define KEYWORD_PERMUTATION_15
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_16
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_17
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_18
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_19
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_20
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_21
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_22
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM)
#define KEYWORD_PERMUTATION_23
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_24
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_25
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_26
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_27
#elif defined(_USE_EMISSIVE) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_28
#elif defined(_USE_EMISSIVE) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_29
#elif defined(_USE_EMISSIVE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_30
#elif defined(_USE_EMISSIVE)
#define KEYWORD_PERMUTATION_31
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_32
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_33
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_34
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_35
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_36
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_37
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_38
#elif defined(_USE_WPO) && defined(_USE_ORM)
#define KEYWORD_PERMUTATION_39
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_40
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_41
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_42
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_43
#elif defined(_USE_WPO) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_44
#elif defined(_USE_WPO) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_45
#elif defined(_USE_WPO) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_46
#elif defined(_USE_WPO)
#define KEYWORD_PERMUTATION_47
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_48
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_49
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_50
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_51
#elif defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_52
#elif defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_53
#elif defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_54
#elif defined(_USE_ORM)
#define KEYWORD_PERMUTATION_55
#elif defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_56
#elif defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_57
#elif defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_58
#elif defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_59
#elif defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_60
#elif defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_61
#elif defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_62
#else
#define KEYWORD_PERMUTATION_63
#endif


// Defines
#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_TEXCOORD1
#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_NORMAL_WS
#define VARYINGS_NEED_TANGENT_WS
#define VARYINGS_NEED_TEXCOORD0
#define VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_DEFERRED
#define BUILTIN_TARGET_API 1
#ifdef _BUILTIN_SURFACE_TYPE_TRANSPARENT
#define _SURFACE_TYPE_TRANSPARENT _BUILTIN_SURFACE_TYPE_TRANSPARENT
#endif
#ifdef _BUILTIN_ALPHATEST_ON
#define _ALPHATEST_ON _BUILTIN_ALPHATEST_ON
#endif
#ifdef _BUILTIN_AlphaClip
#define _AlphaClip _BUILTIN_AlphaClip
#endif
#ifdef _BUILTIN_ALPHAPREMULTIPLY_ON
#define _ALPHAPREMULTIPLY_ON _BUILTIN_ALPHAPREMULTIPLY_ON
#endif


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Shim/Shims.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/LegacySurfaceVertex.hlsl"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/ShaderGraphFunctions.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
 float3 positionOS : POSITION;
 float3 normalOS : NORMAL;
 float4 tangentOS : TANGENT;
 float4 uv0 : TEXCOORD0;
 float4 uv1 : TEXCOORD1;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
 float4 positionCS : SV_POSITION;
 float3 positionWS;
 float3 normalWS;
 float4 tangentWS;
 float4 texCoord0;
#if defined(LIGHTMAP_ON)
 float2 lightmapUV;
#endif
#if !defined(LIGHTMAP_ON)
 float3 sh;
#endif
 float4 fogFactorAndVertexLight;
 float4 shadowCoord;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};
struct SurfaceDescriptionInputs
{
 float3 WorldSpaceNormal;
 float3 TangentSpaceNormal;
 float3 ObjectSpacePosition;
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 AbsoluteWorldSpacePosition;
 float3 TimeParameters;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
#if defined(LIGHTMAP_ON)
 float2 lightmapUV : INTERP0;
#endif
#if !defined(LIGHTMAP_ON)
 float3 sh : INTERP1;
#endif
 float4 tangentWS : INTERP2;
 float4 texCoord0 : INTERP3;
 float4 fogFactorAndVertexLight : INTERP4;
 float4 shadowCoord : INTERP5;
 float3 positionWS : INTERP6;
 float3 normalWS : INTERP7;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};

PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.positionCS = input.positionCS;
#if defined(LIGHTMAP_ON)
output.lightmapUV = input.lightmapUV;
#endif
#if !defined(LIGHTMAP_ON)
output.sh = input.sh;
#endif
output.tangentWS.xyzw = input.tangentWS;
output.texCoord0.xyzw = input.texCoord0;
output.fogFactorAndVertexLight.xyzw = input.fogFactorAndVertexLight;
output.shadowCoord.xyzw = input.shadowCoord;
output.positionWS.xyz = input.positionWS;
output.normalWS.xyz = input.normalWS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.positionCS = input.positionCS;
#if defined(LIGHTMAP_ON)
output.lightmapUV = input.lightmapUV;
#endif
#if !defined(LIGHTMAP_ON)
output.sh = input.sh;
#endif
output.tangentWS = input.tangentWS.xyzw;
output.texCoord0 = input.texCoord0.xyzw;
output.fogFactorAndVertexLight = input.fogFactorAndVertexLight.xyzw;
output.shadowCoord = input.shadowCoord.xyzw;
output.positionWS = input.positionWS.xyz;
output.normalWS = input.normalWS.xyz;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}


// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)

float _EnvironmentReactionSupported;
float4 _EnvState0, _EnvState1;
float _FuelCoverage, _AllowFuelCoverage;
float4 _WetTint, _CharTint, _FireColor;
float4 _EnvironmentCellBounds;
float _UseEnvironmentCells;

float _Height_from_Ground_Randomness;
float _Height_from_Ground_Softness;
float _Normal_Pow;
float _Normal_Mul;
float _Mask_Map_Size;
float4 _Coverage_Normal_Map_TexelSize;
float _Mask_Map_Intensity;
float4 _Coverage_ORM_Map_TexelSize;
float _Height_Weight;
float _Height_Gradience;
float _Wind_Intensity;
float3 _Wind_Direction;
float _Wind_Speed;
float _Wind_Time_Random_Offset_Amount;
float _Texture_Size;
float4 _Color_Map_TexelSize;
float4 _Color_Tint;
float4 _Normal_Map_TexelSize;
float _Normal_Intensity;
float4 _ORM_Map_TexelSize;
float _Roughness_Intensity;
float _AO_Intensity;
float _Metallic_Intensity;
float4 _Emissive_Map_TexelSize;
float4 _Emissive_Color;
float4 _Coverage_Color_Map_TexelSize;
float _Coverage_Size;
float _Height_from_Ground;
float4 _Mask_Map_TexelSize;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Coverage_Normal_Map);
SAMPLER(sampler_Coverage_Normal_Map);
TEXTURE2D(_Coverage_ORM_Map);
SAMPLER(sampler_Coverage_ORM_Map);
TEXTURE2D(_Color_Map);
SAMPLER(sampler_Color_Map);
TEXTURE2D(_Normal_Map);
SAMPLER(sampler_Normal_Map);
TEXTURE2D(_ORM_Map);
SAMPLER(sampler_ORM_Map);
TEXTURE2D(_Emissive_Map);
SAMPLER(sampler_Emissive_Map);
TEXTURE2D(_Coverage_Color_Map);
SAMPLER(sampler_Coverage_Color_Map);
TEXTURE2D(_Mask_Map);
SAMPLER(sampler_Mask_Map);

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Includes
// GraphIncludes: <None>

// Graph Functions

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Sine_float(float In, out float Out)
{
    Out = sin(In);
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Length_float3(float3 In, out float Out)
{
    Out = length(In);
}

void Unity_Divide_float(float A, float B, out float Out)
{
    Out = A / B;
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
{
    Out = lerp(A, B, T);
}

struct Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float
{
float3 ObjectSpacePosition;
float3 TimeParameters;
};

void SG_WindSway_51c4027ae50a8cd49be53c4990b9f179_float(float _Sway_Wind_Speed, float3 _Sway_Wind_Direction, float _Sway_Wind_Intensity, float _Sway_Wind_Time_Random_Offset_Amount, Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float IN, out float3 OutVector3_1)
{
float _Property_5c4cd19861f64014ae8e16af9365ef49_Out_0_Float = _Sway_Wind_Time_Random_Offset_Amount;
float _Add_4b31033339dc4f8e8e71674d98e662a3_Out_2_Float;
Unity_Add_float(IN.TimeParameters.x, _Property_5c4cd19861f64014ae8e16af9365ef49_Out_0_Float, _Add_4b31033339dc4f8e8e71674d98e662a3_Out_2_Float);
float _Property_9a93fa589ac7409884a94247349a940f_Out_0_Float = _Sway_Wind_Speed;
float _Multiply_7418655003ae4fedb2bf68678629472c_Out_2_Float;
Unity_Multiply_float_float(_Add_4b31033339dc4f8e8e71674d98e662a3_Out_2_Float, _Property_9a93fa589ac7409884a94247349a940f_Out_0_Float, _Multiply_7418655003ae4fedb2bf68678629472c_Out_2_Float);
float _Sine_ebd14b0fc42b4b4cb4387f3a1b879c1f_Out_1_Float;
Unity_Sine_float(_Multiply_7418655003ae4fedb2bf68678629472c_Out_2_Float, _Sine_ebd14b0fc42b4b4cb4387f3a1b879c1f_Out_1_Float);
float _Add_660e92170c564f94a79277cd2fd0cf20_Out_2_Float;
Unity_Add_float(_Sine_ebd14b0fc42b4b4cb4387f3a1b879c1f_Out_1_Float, float(1), _Add_660e92170c564f94a79277cd2fd0cf20_Out_2_Float);
float _Multiply_b8fd654b771447aebafc1a6aad6957cf_Out_2_Float;
Unity_Multiply_float_float(_Add_660e92170c564f94a79277cd2fd0cf20_Out_2_Float, 0.5, _Multiply_b8fd654b771447aebafc1a6aad6957cf_Out_2_Float);
float3 _Property_fa2ee033b44946c59c3fa9aea4a167c8_Out_0_Vector3 = _Sway_Wind_Direction;
float3 _Multiply_a5500b7f7b8e43988a055eea7eb917f3_Out_2_Vector3;
Unity_Multiply_float3_float3((_Multiply_b8fd654b771447aebafc1a6aad6957cf_Out_2_Float.xxx), _Property_fa2ee033b44946c59c3fa9aea4a167c8_Out_0_Vector3, _Multiply_a5500b7f7b8e43988a055eea7eb917f3_Out_2_Vector3);
float _Property_66cba0a3223546238e18b58e017e756f_Out_0_Float = _Sway_Wind_Intensity;
float3 _Multiply_475f81423e39483cba045bef08b9f967_Out_2_Vector3;
Unity_Multiply_float3_float3(_Multiply_a5500b7f7b8e43988a055eea7eb917f3_Out_2_Vector3, (_Property_66cba0a3223546238e18b58e017e756f_Out_0_Float.xxx), _Multiply_475f81423e39483cba045bef08b9f967_Out_2_Vector3);
float3 _Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3;
Unity_Add_float3(_Multiply_475f81423e39483cba045bef08b9f967_Out_2_Vector3, IN.ObjectSpacePosition, _Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3);
float _Length_53965ffb38de43359ab019a247194557_Out_1_Float;
Unity_Length_float3(IN.ObjectSpacePosition, _Length_53965ffb38de43359ab019a247194557_Out_1_Float);
float _Length_77b6b3d742104f2abf3c6968dea9a342_Out_1_Float;
Unity_Length_float3(_Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3, _Length_77b6b3d742104f2abf3c6968dea9a342_Out_1_Float);
float _Divide_f5dd43de79e343638d2743ee93ce7326_Out_2_Float;
Unity_Divide_float(_Length_53965ffb38de43359ab019a247194557_Out_1_Float, _Length_77b6b3d742104f2abf3c6968dea9a342_Out_1_Float, _Divide_f5dd43de79e343638d2743ee93ce7326_Out_2_Float);
float3 _Lerp_7574daf550e1428cb9d35656c7bb9603_Out_3_Vector3;
Unity_Lerp_float3(float3(0, 0, 0), _Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3, (_Divide_f5dd43de79e343638d2743ee93ce7326_Out_2_Float.xxx), _Lerp_7574daf550e1428cb9d35656c7bb9603_Out_3_Vector3);
OutVector3_1 = _Lerp_7574daf550e1428cb9d35656c7bb9603_Out_3_Vector3;
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

void Unity_Power_float(float A, float B, out float Out)
{
    Out = pow(A, B);
}

void Unity_Divide_float4(float4 A, float4 B, out float4 Out)
{
    Out = A / B;
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

void Unity_Normalize_float3(float3 In, out float3 Out)
{
    Out = normalize(In);
}

void Unity_RandomRange_float(float2 Seed, float Min, float Max, out float Out)
{
     float randomno =  frac(sin(dot(Seed, float2(12.9898, 78.233)))*43758.5453);
     Out = lerp(Min, Max, randomno);
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
}

void Unity_DotProduct_float3(float3 A, float3 B, out float Out)
{
    Out = dot(A, B);
}

void Unity_Step_float(float Edge, float In, out float Out)
{
    Out = step(Edge, In);
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_NormalStrength_float(float3 In, float Strength, out float3 Out)
{
    Out = float3(In.rg * Strength, lerp(1, In.b, saturate(Strength)));
}

void Unity_Lerp_float(float A, float B, float T, out float Out)
{
    Out = lerp(A, B, T);
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 Position;
float3 Normal;
float3 Tangent;
};

VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
float _Property_b2e048f6276d4371b6ad931f5d1baa87_Out_0_Float = _Wind_Speed;
float3 _Property_7133a0551bc7429cb8bbfcbad044bf97_Out_0_Vector3 = _Wind_Direction;
float _Property_5aa2352867cd4da79dc9f9f9979925c7_Out_0_Float = _Wind_Intensity;
float _Property_e0c6ebff99e349d6966115347961a70c_Out_0_Float = _Wind_Time_Random_Offset_Amount;
Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf;
_WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf.ObjectSpacePosition = IN.ObjectSpacePosition;
_WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf.TimeParameters = IN.TimeParameters;
float3 _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf_OutVector3_1_Vector3;
SG_WindSway_51c4027ae50a8cd49be53c4990b9f179_float(_Property_b2e048f6276d4371b6ad931f5d1baa87_Out_0_Float, _Property_7133a0551bc7429cb8bbfcbad044bf97_Out_0_Vector3, _Property_5aa2352867cd4da79dc9f9f9979925c7_Out_0_Float, _Property_e0c6ebff99e349d6966115347961a70c_Out_0_Float, _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf, _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf_OutVector3_1_Vector3);
float _Split_38420bd2034746a1878048f0ead2a7b2_R_1_Float = IN.AbsoluteWorldSpacePosition[0];
float _Split_38420bd2034746a1878048f0ead2a7b2_G_2_Float = IN.AbsoluteWorldSpacePosition[1];
float _Split_38420bd2034746a1878048f0ead2a7b2_B_3_Float = IN.AbsoluteWorldSpacePosition[2];
float _Split_38420bd2034746a1878048f0ead2a7b2_A_4_Float = 0;
float3 _Vector3_0f8475b3a0df451ea79f6ea2e4e3f673_Out_0_Vector3 = float3(float(0), float(0), float(0));
float3 _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3;
{
// Converting Position from Object to AbsoluteWorld via world space
float3 world;
world = TransformObjectToWorld(_Vector3_0f8475b3a0df451ea79f6ea2e4e3f673_Out_0_Vector3.xyz);
_Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3 = GetAbsolutePositionWS(world);
}
float _Split_fb1b2d84926449e498da6c90f1436311_R_1_Float = _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3[0];
float _Split_fb1b2d84926449e498da6c90f1436311_G_2_Float = _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3[1];
float _Split_fb1b2d84926449e498da6c90f1436311_B_3_Float = _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3[2];
float _Split_fb1b2d84926449e498da6c90f1436311_A_4_Float = 0;
float _Subtract_18ddaa9851a547f1bc3eeafa55f3227b_Out_2_Float;
Unity_Subtract_float(_Split_38420bd2034746a1878048f0ead2a7b2_G_2_Float, _Split_fb1b2d84926449e498da6c90f1436311_G_2_Float, _Subtract_18ddaa9851a547f1bc3eeafa55f3227b_Out_2_Float);
float _Property_1db6873c977b47f186e42eef2cd75a57_Out_0_Float = _Height_Weight;
float _Split_bc5987163dba418d8de90e985de1247b_R_1_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[0];
float _Split_bc5987163dba418d8de90e985de1247b_G_2_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[1];
float _Split_bc5987163dba418d8de90e985de1247b_B_3_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[2];
float _Split_bc5987163dba418d8de90e985de1247b_A_4_Float = 0;
float _Multiply_89de5a5fb3cd451abb1227deba019c38_Out_2_Float;
Unity_Multiply_float_float(_Property_1db6873c977b47f186e42eef2cd75a57_Out_0_Float, _Split_bc5987163dba418d8de90e985de1247b_G_2_Float, _Multiply_89de5a5fb3cd451abb1227deba019c38_Out_2_Float);
float _Divide_2c406de6c63e463f9013e4faf1db1ae8_Out_2_Float;
Unity_Divide_float(_Subtract_18ddaa9851a547f1bc3eeafa55f3227b_Out_2_Float, _Multiply_89de5a5fb3cd451abb1227deba019c38_Out_2_Float, _Divide_2c406de6c63e463f9013e4faf1db1ae8_Out_2_Float);
float _Saturate_e9ea645ca89f482a9d0315df54a77ee5_Out_1_Float;
Unity_Saturate_float(_Divide_2c406de6c63e463f9013e4faf1db1ae8_Out_2_Float, _Saturate_e9ea645ca89f482a9d0315df54a77ee5_Out_1_Float);
float _Property_e96fd1920dff4533951da684129df200_Out_0_Float = _Height_Gradience;
float _Power_b88c5abc85424f17b03c60c98e9bc16c_Out_2_Float;
Unity_Power_float(_Saturate_e9ea645ca89f482a9d0315df54a77ee5_Out_1_Float, _Property_e96fd1920dff4533951da684129df200_Out_0_Float, _Power_b88c5abc85424f17b03c60c98e9bc16c_Out_2_Float);
float _Saturate_6c35d0fa108b483783bde88e06ff5ce0_Out_1_Float;
Unity_Saturate_float(_Power_b88c5abc85424f17b03c60c98e9bc16c_Out_2_Float, _Saturate_6c35d0fa108b483783bde88e06ff5ce0_Out_1_Float);
float3 _Lerp_8461a67d418941ee81ed9eb0f80e8a38_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf_OutVector3_1_Vector3, (_Saturate_6c35d0fa108b483783bde88e06ff5ce0_Out_1_Float.xxx), _Lerp_8461a67d418941ee81ed9eb0f80e8a38_Out_3_Vector3);
#if defined(_USE_WPO)
float3 _UseWPO_81cb92bff33547fbbfaa5ce2f60ba97c_Out_0_Vector3 = _Lerp_8461a67d418941ee81ed9eb0f80e8a38_Out_3_Vector3;
#else
float3 _UseWPO_81cb92bff33547fbbfaa5ce2f60ba97c_Out_0_Vector3 = IN.ObjectSpacePosition;
#endif
description.Position = _UseWPO_81cb92bff33547fbbfaa5ce2f60ba97c_Out_0_Vector3;
description.Normal = IN.ObjectSpaceNormal;
description.Tangent = IN.ObjectSpaceTangent;
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
float3 BaseColor;
float3 NormalTS;
float3 Emission;
float Metallic;
float Smoothness;
float Occlusion;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
UnityTexture2D _Property_0c79649b4042462682b4a7423715f1a1_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Color_Map, sampler_Color_Map, _Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_a9551365617a4a66b75c6a49ab8f9c5f_Out_0_Vector4 = IN.uv0;
float _Property_ac4a3a6f20bd42789825abf989f1a228_Out_0_Float = _Texture_Size;
float4 _Divide_1ff147fe44c540028660935d59272eaa_Out_2_Vector4;
Unity_Divide_float4(_UV_a9551365617a4a66b75c6a49ab8f9c5f_Out_0_Vector4, (_Property_ac4a3a6f20bd42789825abf989f1a228_Out_0_Float.xxxx), _Divide_1ff147fe44c540028660935d59272eaa_Out_2_Vector4);
float4 _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_0c79649b4042462682b4a7423715f1a1_Out_0_Texture2D.tex, _Property_0c79649b4042462682b4a7423715f1a1_Out_0_Texture2D.samplerstate, _Property_0c79649b4042462682b4a7423715f1a1_Out_0_Texture2D.GetTransformedUV((_Divide_1ff147fe44c540028660935d59272eaa_Out_2_Vector4.xy)) );
if (_Property_0c79649b4042462682b4a7423715f1a1_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4, _Property_0c79649b4042462682b4a7423715f1a1_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_R_4_Float = _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4.r;
float _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_G_5_Float = _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4.g;
float _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_B_6_Float = _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4.b;
float _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_A_7_Float = _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4.a;
float4 _Property_11be52d8078b41908ba72022455e8b98_Out_0_Vector4 = _Color_Tint;
float4 _Multiply_e3ef32e4a4e8442188dfd3c93394960c_Out_2_Vector4;
Unity_Multiply_float4_float4(_SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4, _Property_11be52d8078b41908ba72022455e8b98_Out_0_Vector4, _Multiply_e3ef32e4a4e8442188dfd3c93394960c_Out_2_Vector4);
UnityTexture2D _Property_415df38a141841e6aa11a7b1e336c379_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Coverage_Color_Map, sampler_Coverage_Color_Map, _Coverage_Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_0805a5601f5b49beb942e50ab3714e03_Out_0_Vector4 = IN.uv0;
float _Property_eb7418710f3f4dc6981c01445b1a809e_Out_0_Float = _Coverage_Size;
float4 _Divide_71dd940938164d5a9e7020e44b1cddf3_Out_2_Vector4;
Unity_Divide_float4(_UV_0805a5601f5b49beb942e50ab3714e03_Out_0_Vector4, (_Property_eb7418710f3f4dc6981c01445b1a809e_Out_0_Float.xxxx), _Divide_71dd940938164d5a9e7020e44b1cddf3_Out_2_Vector4);
float4 _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_415df38a141841e6aa11a7b1e336c379_Out_0_Texture2D.tex, _Property_415df38a141841e6aa11a7b1e336c379_Out_0_Texture2D.samplerstate, _Property_415df38a141841e6aa11a7b1e336c379_Out_0_Texture2D.GetTransformedUV((_Divide_71dd940938164d5a9e7020e44b1cddf3_Out_2_Vector4.xy)) );
if (_Property_415df38a141841e6aa11a7b1e336c379_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4, _Property_415df38a141841e6aa11a7b1e336c379_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_R_4_Float = _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4.r;
float _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_G_5_Float = _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4.g;
float _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_B_6_Float = _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4.b;
float _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_A_7_Float = _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4.a;
UnityTexture2D _Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Mask_Map, sampler_Mask_Map, _Mask_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_c2c5030522744aceb603ff104c9ccde0_Out_0_Vector4 = IN.uv0;
float _Property_a975da4683af46ab85b4a95b01cb9f32_Out_0_Float = _Mask_Map_Size;
float4 _Divide_3dd829d125dc4ec1b32772766d56a17b_Out_2_Vector4;
Unity_Divide_float4(_UV_c2c5030522744aceb603ff104c9ccde0_Out_0_Vector4, (_Property_a975da4683af46ab85b4a95b01cb9f32_Out_0_Float.xxxx), _Divide_3dd829d125dc4ec1b32772766d56a17b_Out_2_Vector4);
float4 _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D.tex, _Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D.samplerstate, _Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D.GetTransformedUV((_Divide_3dd829d125dc4ec1b32772766d56a17b_Out_2_Vector4.xy)) );
if (_Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4, _Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_R_4_Float = _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4.r;
float _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_G_5_Float = _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4.g;
float _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_B_6_Float = _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4.b;
float _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_A_7_Float = _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4.a;
float _Multiply_81f5f8bc82304adbae345a5fe4b0024b_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_R_4_Float, 0.1, _Multiply_81f5f8bc82304adbae345a5fe4b0024b_Out_2_Float);
float _Add_33507c032b9e48bfb61763d507bb031c_Out_2_Float;
Unity_Add_float(_Multiply_81f5f8bc82304adbae345a5fe4b0024b_Out_2_Float, float(0.9), _Add_33507c032b9e48bfb61763d507bb031c_Out_2_Float);
float _Property_883f8e1288fd43f1a3674ca1f5364253_Out_0_Float = _Mask_Map_Intensity;
float _Power_b45fec9ffb074e40b5cb5452c0fb982d_Out_2_Float;
Unity_Power_float(_Add_33507c032b9e48bfb61763d507bb031c_Out_2_Float, _Property_883f8e1288fd43f1a3674ca1f5364253_Out_0_Float, _Power_b45fec9ffb074e40b5cb5452c0fb982d_Out_2_Float);
float _Split_b6a9cc69e69c417ba235626d7beed2eb_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_b6a9cc69e69c417ba235626d7beed2eb_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_b6a9cc69e69c417ba235626d7beed2eb_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_b6a9cc69e69c417ba235626d7beed2eb_A_4_Float = 0;
float _Property_73ac42b2cd3d4b43976ac13e73b6de40_Out_0_Float = _Height_from_Ground;
float3 _Transform_425aee5b4b5e409eb94b431642b515fb_Out_1_Vector3;
_Transform_425aee5b4b5e409eb94b431642b515fb_Out_1_Vector3 = TransformObjectToWorld(float3 (0, 0, 0).xyz);
float3 _Normalize_960b1f24c996479a8c1a9a81cabcb835_Out_1_Vector3;
Unity_Normalize_float3(_Transform_425aee5b4b5e409eb94b431642b515fb_Out_1_Vector3, _Normalize_960b1f24c996479a8c1a9a81cabcb835_Out_1_Vector3);
float _Property_11f1157c9f364521bf765c261e8bb93e_Out_0_Float = _Height_from_Ground_Randomness;
float _Multiply_b7a3ed973f104b9a84af846ce299d95f_Out_2_Float;
Unity_Multiply_float_float(_Property_11f1157c9f364521bf765c261e8bb93e_Out_0_Float, -1, _Multiply_b7a3ed973f104b9a84af846ce299d95f_Out_2_Float);
float _RandomRange_82fa55b7f4da4f6ebc597a38d2ca2e6d_Out_3_Float;
Unity_RandomRange_float((_Normalize_960b1f24c996479a8c1a9a81cabcb835_Out_1_Vector3.xy), _Multiply_b7a3ed973f104b9a84af846ce299d95f_Out_2_Float, _Property_11f1157c9f364521bf765c261e8bb93e_Out_0_Float, _RandomRange_82fa55b7f4da4f6ebc597a38d2ca2e6d_Out_3_Float);
float _Add_109ea06d643c4826b0138a85becc2fa9_Out_2_Float;
Unity_Add_float(_Property_73ac42b2cd3d4b43976ac13e73b6de40_Out_0_Float, _RandomRange_82fa55b7f4da4f6ebc597a38d2ca2e6d_Out_3_Float, _Add_109ea06d643c4826b0138a85becc2fa9_Out_2_Float);
float _Subtract_fe60462bdbdd4a22a207774a6a8cb1b8_Out_2_Float;
Unity_Subtract_float(_Split_b6a9cc69e69c417ba235626d7beed2eb_G_2_Float, _Add_109ea06d643c4826b0138a85becc2fa9_Out_2_Float, _Subtract_fe60462bdbdd4a22a207774a6a8cb1b8_Out_2_Float);
float _Property_1487773737e44e78a7c514c7f470e603_Out_0_Float = _Height_from_Ground_Softness;
float _Divide_6eb043a8aee54d87a0c73169597bdcba_Out_2_Float;
Unity_Divide_float(_Subtract_fe60462bdbdd4a22a207774a6a8cb1b8_Out_2_Float, _Property_1487773737e44e78a7c514c7f470e603_Out_0_Float, _Divide_6eb043a8aee54d87a0c73169597bdcba_Out_2_Float);
float _Saturate_d8ec125670414d1888aeec654fcb04e2_Out_1_Float;
Unity_Saturate_float(_Divide_6eb043a8aee54d87a0c73169597bdcba_Out_2_Float, _Saturate_d8ec125670414d1888aeec654fcb04e2_Out_1_Float);
float _OneMinus_de85154fa1b742e3b6c9e0f92b85d96c_Out_1_Float;
Unity_OneMinus_float(_Saturate_d8ec125670414d1888aeec654fcb04e2_Out_1_Float, _OneMinus_de85154fa1b742e3b6c9e0f92b85d96c_Out_1_Float);
float _DotProduct_a5624fdd7fd94a4c82ad8c039922e638_Out_2_Float;
Unity_DotProduct_float3(IN.WorldSpaceNormal, float3(0, 1, 0), _DotProduct_a5624fdd7fd94a4c82ad8c039922e638_Out_2_Float);
float _Property_1d95c04e88f443bc91fe7ed217b674b8_Out_0_Float = _Normal_Pow;
float _Power_5b713b10f3e049f8a97be81969b52b7d_Out_2_Float;
Unity_Power_float(_DotProduct_a5624fdd7fd94a4c82ad8c039922e638_Out_2_Float, _Property_1d95c04e88f443bc91fe7ed217b674b8_Out_0_Float, _Power_5b713b10f3e049f8a97be81969b52b7d_Out_2_Float);
float _Property_6314206eb47742698a7d2833f119f39e_Out_0_Float = _Normal_Mul;
float _Multiply_6e6bcfc2a4df414e86d241952840e972_Out_2_Float;
Unity_Multiply_float_float(_Power_5b713b10f3e049f8a97be81969b52b7d_Out_2_Float, _Property_6314206eb47742698a7d2833f119f39e_Out_0_Float, _Multiply_6e6bcfc2a4df414e86d241952840e972_Out_2_Float);
float _Saturate_77d41005224e417eb678b78b9a10771a_Out_1_Float;
Unity_Saturate_float(_Multiply_6e6bcfc2a4df414e86d241952840e972_Out_2_Float, _Saturate_77d41005224e417eb678b78b9a10771a_Out_1_Float);
float _Add_8395f84ea8e546988dd02e5b922d32ee_Out_2_Float;
Unity_Add_float(_OneMinus_de85154fa1b742e3b6c9e0f92b85d96c_Out_1_Float, _Saturate_77d41005224e417eb678b78b9a10771a_Out_1_Float, _Add_8395f84ea8e546988dd02e5b922d32ee_Out_2_Float);
float _Multiply_b61ead58dd024fd7aacad7c0b68f3be7_Out_2_Float;
Unity_Multiply_float_float(5, _Add_8395f84ea8e546988dd02e5b922d32ee_Out_2_Float, _Multiply_b61ead58dd024fd7aacad7c0b68f3be7_Out_2_Float);
float _Multiply_b0c3a49303544ccaa1d15379e6b1e9d2_Out_2_Float;
Unity_Multiply_float_float(_Power_b45fec9ffb074e40b5cb5452c0fb982d_Out_2_Float, _Multiply_b61ead58dd024fd7aacad7c0b68f3be7_Out_2_Float, _Multiply_b0c3a49303544ccaa1d15379e6b1e9d2_Out_2_Float);
float _Step_9e404f0c811346c88c531f42e623243f_Out_2_Float;
Unity_Step_float(_Subtract_fe60462bdbdd4a22a207774a6a8cb1b8_Out_2_Float, float(-0.1), _Step_9e404f0c811346c88c531f42e623243f_Out_2_Float);
float _Add_0328340190574386b2d37a0d035b66fb_Out_2_Float;
Unity_Add_float(_Multiply_b0c3a49303544ccaa1d15379e6b1e9d2_Out_2_Float, _Step_9e404f0c811346c88c531f42e623243f_Out_2_Float, _Add_0328340190574386b2d37a0d035b66fb_Out_2_Float);
float _Power_3bfc30934311465db3ba28e64b49097d_Out_2_Float;
Unity_Power_float(_Add_0328340190574386b2d37a0d035b66fb_Out_2_Float, float(3), _Power_3bfc30934311465db3ba28e64b49097d_Out_2_Float);
float _Saturate_3ba4a70af5214b4086803937a3f8b09f_Out_1_Float;
Unity_Saturate_float(_Power_3bfc30934311465db3ba28e64b49097d_Out_2_Float, _Saturate_3ba4a70af5214b4086803937a3f8b09f_Out_1_Float);
float _Saturate_6e423c9f5f1249088750a2b84a42b764_Out_1_Float;
Unity_Saturate_float(_Add_8395f84ea8e546988dd02e5b922d32ee_Out_2_Float, _Saturate_6e423c9f5f1249088750a2b84a42b764_Out_1_Float);
#if defined(_USE_COVERAGE_MASK_TEXTURE)
float _UseCoverageMaskTexture_f75c00eee68746bd80e2fa6c2bc28d4c_Out_0_Float = _Saturate_3ba4a70af5214b4086803937a3f8b09f_Out_1_Float;
#else
float _UseCoverageMaskTexture_f75c00eee68746bd80e2fa6c2bc28d4c_Out_0_Float = _Saturate_6e423c9f5f1249088750a2b84a42b764_Out_1_Float;
#endif
float4 _Lerp_c6c7a7472707418491f2fa10989f05b9_Out_3_Vector4;
Unity_Lerp_float4(_Multiply_e3ef32e4a4e8442188dfd3c93394960c_Out_2_Vector4, _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4, (_UseCoverageMaskTexture_f75c00eee68746bd80e2fa6c2bc28d4c_Out_0_Float.xxxx), _Lerp_c6c7a7472707418491f2fa10989f05b9_Out_3_Vector4);
#if defined(_USE_COVERAGE)
float4 _UseCoverage_c2f2c567080e4d5f9b652fbcbf667c54_Out_0_Vector4 = _Lerp_c6c7a7472707418491f2fa10989f05b9_Out_3_Vector4;
#else
float4 _UseCoverage_c2f2c567080e4d5f9b652fbcbf667c54_Out_0_Vector4 = _Multiply_e3ef32e4a4e8442188dfd3c93394960c_Out_2_Vector4;
#endif
UnityTexture2D _Property_a38bf1e56c364aa0add1f01a99eb6c3e_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Normal_Map, sampler_Normal_Map, _Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_a38bf1e56c364aa0add1f01a99eb6c3e_Out_0_Texture2D.tex, _Property_a38bf1e56c364aa0add1f01a99eb6c3e_Out_0_Texture2D.samplerstate, _Property_a38bf1e56c364aa0add1f01a99eb6c3e_Out_0_Texture2D.GetTransformedUV((_Divide_1ff147fe44c540028660935d59272eaa_Out_2_Vector4.xy)) );
if (_Property_a38bf1e56c364aa0add1f01a99eb6c3e_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4, _Property_a38bf1e56c364aa0add1f01a99eb6c3e_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4);
float _SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_R_4_Float = _SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4.r;
float _SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_G_5_Float = _SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4.g;
float _SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_B_6_Float = _SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4.b;
float _SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_A_7_Float = _SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4.a;
float _Property_ff6f57141462418790b8f0ef1d7d69cc_Out_0_Float = _Normal_Intensity;
float3 _NormalStrength_6e1c84dc8cbd47f988a654d24e83669f_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_86fd9e94e3cb42efb6254090066bc50d_RGBA_0_Vector4.xyz), _Property_ff6f57141462418790b8f0ef1d7d69cc_Out_0_Float, _NormalStrength_6e1c84dc8cbd47f988a654d24e83669f_Out_2_Vector3);
#if defined(_USE_NORMAL_MAPS)
float3 _UseNormalMaps_f01546d53fa54c3191638b3fe3736688_Out_0_Vector3 = _NormalStrength_6e1c84dc8cbd47f988a654d24e83669f_Out_2_Vector3;
#else
float3 _UseNormalMaps_f01546d53fa54c3191638b3fe3736688_Out_0_Vector3 = float3(0, 0, 1);
#endif
UnityTexture2D _Property_15070824f15143aea7ede9b130164716_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Coverage_Normal_Map, sampler_Coverage_Normal_Map, _Coverage_Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_15070824f15143aea7ede9b130164716_Out_0_Texture2D.tex, _Property_15070824f15143aea7ede9b130164716_Out_0_Texture2D.samplerstate, _Property_15070824f15143aea7ede9b130164716_Out_0_Texture2D.GetTransformedUV((_Divide_71dd940938164d5a9e7020e44b1cddf3_Out_2_Vector4.xy)) );
if (_Property_15070824f15143aea7ede9b130164716_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4, _Property_15070824f15143aea7ede9b130164716_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4);
float _SampleTexture2D_26c139222fa1483d83940748db6d922f_R_4_Float = _SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4.r;
float _SampleTexture2D_26c139222fa1483d83940748db6d922f_G_5_Float = _SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4.g;
float _SampleTexture2D_26c139222fa1483d83940748db6d922f_B_6_Float = _SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4.b;
float _SampleTexture2D_26c139222fa1483d83940748db6d922f_A_7_Float = _SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4.a;
float3 _Lerp_a5163d5f017c46b9b347647a7d361156_Out_3_Vector3;
Unity_Lerp_float3(_UseNormalMaps_f01546d53fa54c3191638b3fe3736688_Out_0_Vector3, (_SampleTexture2D_26c139222fa1483d83940748db6d922f_RGBA_0_Vector4.xyz), (_UseCoverageMaskTexture_f75c00eee68746bd80e2fa6c2bc28d4c_Out_0_Float.xxx), _Lerp_a5163d5f017c46b9b347647a7d361156_Out_3_Vector3);
#if defined(_USE_COVERAGE)
float3 _UseCoverage_d6109394a0154e89890cf6ca228f700f_Out_0_Vector3 = _Lerp_a5163d5f017c46b9b347647a7d361156_Out_3_Vector3;
#else
float3 _UseCoverage_d6109394a0154e89890cf6ca228f700f_Out_0_Vector3 = _UseNormalMaps_f01546d53fa54c3191638b3fe3736688_Out_0_Vector3;
#endif
#if defined(_USE_NORMAL_MAPS)
float3 _UseNormalMaps_cf0fe6c3e8224d69a92ff77f7f270e12_Out_0_Vector3 = _UseCoverage_d6109394a0154e89890cf6ca228f700f_Out_0_Vector3;
#else
float3 _UseNormalMaps_cf0fe6c3e8224d69a92ff77f7f270e12_Out_0_Vector3 = float3(0, 0, 1);
#endif
UnityTexture2D _Property_c7dc38eaeea545aaada0e67cc32d9d25_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Emissive_Map, sampler_Emissive_Map, _Emissive_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_c7dc38eaeea545aaada0e67cc32d9d25_Out_0_Texture2D.tex, _Property_c7dc38eaeea545aaada0e67cc32d9d25_Out_0_Texture2D.samplerstate, _Property_c7dc38eaeea545aaada0e67cc32d9d25_Out_0_Texture2D.GetTransformedUV((_Divide_1ff147fe44c540028660935d59272eaa_Out_2_Vector4.xy)) );
if (_Property_c7dc38eaeea545aaada0e67cc32d9d25_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4, _Property_c7dc38eaeea545aaada0e67cc32d9d25_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_R_4_Float = _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4.r;
float _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_G_5_Float = _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4.g;
float _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_B_6_Float = _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4.b;
float _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_A_7_Float = _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4.a;
float4 _Property_857cbfa6469047f8904e0adc3eefe6b8_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_Emissive_Color) : _Emissive_Color;
float4 _Multiply_293fcf7127ab419cbcdff4183cccdba0_Out_2_Vector4;
Unity_Multiply_float4_float4(_SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4, _Property_857cbfa6469047f8904e0adc3eefe6b8_Out_0_Vector4, _Multiply_293fcf7127ab419cbcdff4183cccdba0_Out_2_Vector4);
#if defined(_USE_EMISSIVE)
float4 _UseEmissive_fdf874f7398e43a7838e4f04fd255375_Out_0_Vector4 = _Multiply_293fcf7127ab419cbcdff4183cccdba0_Out_2_Vector4;
#else
float4 _UseEmissive_fdf874f7398e43a7838e4f04fd255375_Out_0_Vector4 = float4(0, 0, 0, 0);
#endif
float _Property_3cef5a13363c421c89eb005848414161_Out_0_Float = _Metallic_Intensity;
UnityTexture2D _Property_6ced109efdc542d6868e0f24e23a1736_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_ORM_Map, sampler_ORM_Map, _ORM_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_6ced109efdc542d6868e0f24e23a1736_Out_0_Texture2D.tex, _Property_6ced109efdc542d6868e0f24e23a1736_Out_0_Texture2D.samplerstate, _Property_6ced109efdc542d6868e0f24e23a1736_Out_0_Texture2D.GetTransformedUV((_Divide_1ff147fe44c540028660935d59272eaa_Out_2_Vector4.xy)) );
if (_Property_6ced109efdc542d6868e0f24e23a1736_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_RGBA_0_Vector4, _Property_6ced109efdc542d6868e0f24e23a1736_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_R_4_Float = _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_RGBA_0_Vector4.r;
float _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_G_5_Float = _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_RGBA_0_Vector4.g;
float _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_B_6_Float = _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_RGBA_0_Vector4.b;
float _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_A_7_Float = _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_RGBA_0_Vector4.a;
float _Multiply_ffdbad83b5e74ce1b5ca4b9e7e162c40_Out_2_Float;
Unity_Multiply_float_float(_Property_3cef5a13363c421c89eb005848414161_Out_0_Float, _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_B_6_Float, _Multiply_ffdbad83b5e74ce1b5ca4b9e7e162c40_Out_2_Float);
UnityTexture2D _Property_75507ddd24ab4c63b88da2bbaec94f2c_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Coverage_ORM_Map, sampler_Coverage_ORM_Map, _Coverage_ORM_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_75507ddd24ab4c63b88da2bbaec94f2c_Out_0_Texture2D.tex, _Property_75507ddd24ab4c63b88da2bbaec94f2c_Out_0_Texture2D.samplerstate, _Property_75507ddd24ab4c63b88da2bbaec94f2c_Out_0_Texture2D.GetTransformedUV((_Divide_71dd940938164d5a9e7020e44b1cddf3_Out_2_Vector4.xy)) );
if (_Property_75507ddd24ab4c63b88da2bbaec94f2c_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_RGBA_0_Vector4, _Property_75507ddd24ab4c63b88da2bbaec94f2c_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_R_4_Float = _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_RGBA_0_Vector4.r;
float _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_G_5_Float = _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_RGBA_0_Vector4.g;
float _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_B_6_Float = _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_RGBA_0_Vector4.b;
float _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_A_7_Float = _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_RGBA_0_Vector4.a;
float _Lerp_3101e882d6a14372973d46e58ded048f_Out_3_Float;
Unity_Lerp_float(_Multiply_ffdbad83b5e74ce1b5ca4b9e7e162c40_Out_2_Float, _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_B_6_Float, _UseCoverageMaskTexture_f75c00eee68746bd80e2fa6c2bc28d4c_Out_0_Float, _Lerp_3101e882d6a14372973d46e58ded048f_Out_3_Float);
#if defined(_USE_COVERAGE)
float _UseCoverage_5268f0597a8b4df2a05cf3d5da317f6b_Out_0_Float = _Lerp_3101e882d6a14372973d46e58ded048f_Out_3_Float;
#else
float _UseCoverage_5268f0597a8b4df2a05cf3d5da317f6b_Out_0_Float = _Multiply_ffdbad83b5e74ce1b5ca4b9e7e162c40_Out_2_Float;
#endif
float _Property_d066edd24a97480b83f88a08c9a274b8_Out_0_Float = _Metallic_Intensity;
#if defined(_USE_ORM)
float _UseORM_11ec65ff6fc7436b9d5074e8f7a90024_Out_0_Float = _UseCoverage_5268f0597a8b4df2a05cf3d5da317f6b_Out_0_Float;
#else
float _UseORM_11ec65ff6fc7436b9d5074e8f7a90024_Out_0_Float = _Property_d066edd24a97480b83f88a08c9a274b8_Out_0_Float;
#endif
float _Property_ef0d0e2cdec043359496bb1a0782cda9_Out_0_Float = _Roughness_Intensity;
float _Multiply_4b10161d594b4863a7b31e2a1f1c1cf1_Out_2_Float;
Unity_Multiply_float_float(_Property_ef0d0e2cdec043359496bb1a0782cda9_Out_0_Float, _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_G_5_Float, _Multiply_4b10161d594b4863a7b31e2a1f1c1cf1_Out_2_Float);
float _OneMinus_d7dd0fb677e94540bbc094136ca963f5_Out_1_Float;
Unity_OneMinus_float(_Multiply_4b10161d594b4863a7b31e2a1f1c1cf1_Out_2_Float, _OneMinus_d7dd0fb677e94540bbc094136ca963f5_Out_1_Float);
float _OneMinus_bae441d794cd4092861f8242b9f96358_Out_1_Float;
Unity_OneMinus_float(_SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_G_5_Float, _OneMinus_bae441d794cd4092861f8242b9f96358_Out_1_Float);
float _Lerp_377c896eaf3243859da5c9b1446d27f5_Out_3_Float;
Unity_Lerp_float(_OneMinus_d7dd0fb677e94540bbc094136ca963f5_Out_1_Float, _OneMinus_bae441d794cd4092861f8242b9f96358_Out_1_Float, _UseCoverageMaskTexture_f75c00eee68746bd80e2fa6c2bc28d4c_Out_0_Float, _Lerp_377c896eaf3243859da5c9b1446d27f5_Out_3_Float);
#if defined(_USE_COVERAGE)
float _UseCoverage_58ec20069d0a4e0cb494b690fec0b60b_Out_0_Float = _Lerp_377c896eaf3243859da5c9b1446d27f5_Out_3_Float;
#else
float _UseCoverage_58ec20069d0a4e0cb494b690fec0b60b_Out_0_Float = _OneMinus_d7dd0fb677e94540bbc094136ca963f5_Out_1_Float;
#endif
float _Property_a7403d2892854317b50b8fa0268d0f27_Out_0_Float = _Roughness_Intensity;
float _OneMinus_3b59d96fc25b4b00a0364fda5bd47109_Out_1_Float;
Unity_OneMinus_float(_Property_a7403d2892854317b50b8fa0268d0f27_Out_0_Float, _OneMinus_3b59d96fc25b4b00a0364fda5bd47109_Out_1_Float);
#if defined(_USE_ORM)
float _UseORM_2aa3605b11694e28a61f126281dcd8f3_Out_0_Float = _UseCoverage_58ec20069d0a4e0cb494b690fec0b60b_Out_0_Float;
#else
float _UseORM_2aa3605b11694e28a61f126281dcd8f3_Out_0_Float = _OneMinus_3b59d96fc25b4b00a0364fda5bd47109_Out_1_Float;
#endif
float _Property_b578c28d617c46edb5452f0ee34e3698_Out_0_Float = _AO_Intensity;
float _Lerp_11f21031558c4c7db3e9b583ee8c5843_Out_3_Float;
Unity_Lerp_float(float(1), _SampleTexture2D_18778b4863e940d1b1bbfea6647dfebc_R_4_Float, _Property_b578c28d617c46edb5452f0ee34e3698_Out_0_Float, _Lerp_11f21031558c4c7db3e9b583ee8c5843_Out_3_Float);
float _Lerp_f01b6ad8ed5e4f7aa0a392bc0bd1b199_Out_3_Float;
Unity_Lerp_float(_Lerp_11f21031558c4c7db3e9b583ee8c5843_Out_3_Float, _SampleTexture2D_803efc3d0a7c44c0b8031b00782d5be4_R_4_Float, _UseCoverageMaskTexture_f75c00eee68746bd80e2fa6c2bc28d4c_Out_0_Float, _Lerp_f01b6ad8ed5e4f7aa0a392bc0bd1b199_Out_3_Float);
#if defined(_USE_COVERAGE)
float _UseCoverage_ae6eaa1f25704217b662768dd49ad504_Out_0_Float = _Lerp_f01b6ad8ed5e4f7aa0a392bc0bd1b199_Out_3_Float;
#else
float _UseCoverage_ae6eaa1f25704217b662768dd49ad504_Out_0_Float = _Lerp_11f21031558c4c7db3e9b583ee8c5843_Out_3_Float;
#endif
#if defined(_USE_ORM)
float _UseORM_a8f4959fe3174af9bd41a7335d24f10e_Out_0_Float = _UseCoverage_ae6eaa1f25704217b662768dd49ad504_Out_0_Float;
#else
float _UseORM_a8f4959fe3174af9bd41a7335d24f10e_Out_0_Float = float(1);
#endif
surface.BaseColor = (_UseCoverage_c2f2c567080e4d5f9b652fbcbf667c54_Out_0_Vector4.xyz);
surface.NormalTS = _UseNormalMaps_cf0fe6c3e8224d69a92ff77f7f270e12_Out_0_Vector3;
surface.Emission = (_UseEmissive_fdf874f7398e43a7838e4f04fd255375_Out_0_Vector4.xyz);
surface.Metallic = _UseORM_11ec65ff6fc7436b9d5074e8f7a90024_Out_0_Float;
surface.Smoothness = _UseORM_2aa3605b11694e28a61f126281dcd8f3_Out_0_Float;
surface.Occlusion = _UseORM_a8f4959fe3174af9bd41a7335d24f10e_Out_0_Float;
float3 reactionUnusedColor = 0, reactionUnusedEmission = 0;
ClipVegetationFuel(TransformObjectToWorld(float3(0,0,0)));
ApplyVegetationReaction(TransformObjectToWorld(float3(0,0,0)),surface.BaseColor,surface.Emission);
return surface;
}

// --------------------------------------------------
// Build Graph Inputs

VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

    output.ObjectSpaceNormal =                          input.normalOS;
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
    output.AbsoluteWorldSpacePosition =                 GetAbsolutePositionWS(TransformObjectToWorld(input.positionOS));
    output.TimeParameters =                             _TimeParameters.xyz;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

    

    // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
    float3 unnormalizedNormalWS = input.normalWS;
    const float renormFactor = 1.0 / length(unnormalizedNormalWS);


    output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph
    output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);


    output.ObjectSpacePosition = TransformWorldToObject(input.positionWS);

    #if UNITY_UV_STARTS_AT_TOP
    #else
    #endif


    output.uv0 = input.texCoord0;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

void BuildAppDataFull(Attributes attributes, VertexDescription vertexDescription, inout appdata_full result)
{
    result.vertex     = float4(attributes.positionOS, 1);
    result.tangent    = attributes.tangentOS;
    result.normal     = attributes.normalOS;
    result.texcoord   = attributes.uv0;
    result.texcoord1  = attributes.uv1;
    result.vertex     = float4(vertexDescription.Position, 1);
    result.normal     = vertexDescription.Normal;
    result.tangent    = float4(vertexDescription.Tangent, 0);
    #if UNITY_ANY_INSTANCING_ENABLED
    #endif
}

void VaryingsToSurfaceVertex(Varyings varyings, inout v2f_surf result)
{
    result.pos = varyings.positionCS;
    result.worldPos = varyings.positionWS;
    result.worldNormal = varyings.normalWS;
    // World Tangent isn't an available input on v2f_surf

    result._ShadowCoord = varyings.shadowCoord;

    #if UNITY_ANY_INSTANCING_ENABLED
    #endif
    #if UNITY_SHOULD_SAMPLE_SH
    #if !defined(LIGHTMAP_ON)
    result.sh = varyings.sh;
    #endif
    #endif
    #if defined(LIGHTMAP_ON)
    result.lmap.xy = varyings.lightmapUV;
    #endif
    #ifdef VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
        result.fogCoord = varyings.fogFactorAndVertexLight.x;
        COPY_TO_LIGHT_COORDS(result, varyings.fogFactorAndVertexLight.yzw);
    #endif

    DEFAULT_UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(varyings, result);
}

void SurfaceVertexToVaryings(v2f_surf surfVertex, inout Varyings result)
{
    result.positionCS = surfVertex.pos;
    result.positionWS = surfVertex.worldPos;
    result.normalWS = surfVertex.worldNormal;
    // viewDirectionWS is never filled out in the legacy pass' function. Always use the value computed by SRP
    // World Tangent isn't an available input on v2f_surf
    result.shadowCoord = surfVertex._ShadowCoord;

    #if UNITY_ANY_INSTANCING_ENABLED
    #endif
    #if UNITY_SHOULD_SAMPLE_SH
    #if !defined(LIGHTMAP_ON)
    result.sh = surfVertex.sh;
    #endif
    #endif
    #if defined(LIGHTMAP_ON)
    result.lightmapUV = surfVertex.lmap.xy;
    #endif
    #ifdef VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
        result.fogFactorAndVertexLight.x = surfVertex.fogCoord;
        COPY_FROM_LIGHT_COORDS(result.fogFactorAndVertexLight.yzw, surfVertex);
    #endif

    DEFAULT_UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(surfVertex, result);
}

// --------------------------------------------------
// Main

#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/Varyings.hlsl"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/PBRDeferredPass.hlsl"

ENDHLSL
}
Pass
{
    Name "ShadowCaster"
    Tags
    {
        "LightMode" = "ShadowCaster"
    }

// Render State
Cull Back
Blend One Zero
ZTest LEqual
ZWrite On
ColorMask 0

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 3.0
#pragma multi_compile_shadowcaster
#pragma vertex vert
#pragma fragment frag

// Keywords
#pragma multi_compile _ _CASTING_PUNCTUAL_LIGHT_SHADOW
#pragma shader_feature_local _ _USE_EMISSIVE
#pragma shader_feature_local _ _USE_WPO
#pragma shader_feature_local _ _USE_ORM
#pragma shader_feature_local _ _USE_NORMAL_MAPS
#pragma shader_feature_local _ _USE_COVERAGE
#pragma shader_feature_local _ _USE_COVERAGE_MASK_TEXTURE

#if defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_2
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_3
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_4
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_5
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_6
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM)
#define KEYWORD_PERMUTATION_7
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_8
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_9
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_10
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_11
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_12
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_13
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_14
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO)
#define KEYWORD_PERMUTATION_15
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_16
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_17
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_18
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_19
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_20
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_21
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_22
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM)
#define KEYWORD_PERMUTATION_23
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_24
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_25
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_26
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_27
#elif defined(_USE_EMISSIVE) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_28
#elif defined(_USE_EMISSIVE) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_29
#elif defined(_USE_EMISSIVE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_30
#elif defined(_USE_EMISSIVE)
#define KEYWORD_PERMUTATION_31
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_32
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_33
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_34
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_35
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_36
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_37
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_38
#elif defined(_USE_WPO) && defined(_USE_ORM)
#define KEYWORD_PERMUTATION_39
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_40
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_41
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_42
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_43
#elif defined(_USE_WPO) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_44
#elif defined(_USE_WPO) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_45
#elif defined(_USE_WPO) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_46
#elif defined(_USE_WPO)
#define KEYWORD_PERMUTATION_47
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_48
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_49
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_50
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_51
#elif defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_52
#elif defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_53
#elif defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_54
#elif defined(_USE_ORM)
#define KEYWORD_PERMUTATION_55
#elif defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_56
#elif defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_57
#elif defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_58
#elif defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_59
#elif defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_60
#elif defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_61
#elif defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_62
#else
#define KEYWORD_PERMUTATION_63
#endif


// Defines
#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_SHADOWCASTER
#define BUILTIN_TARGET_API 1
#ifdef _BUILTIN_SURFACE_TYPE_TRANSPARENT
#define _SURFACE_TYPE_TRANSPARENT _BUILTIN_SURFACE_TYPE_TRANSPARENT
#endif
#ifdef _BUILTIN_ALPHATEST_ON
#define _ALPHATEST_ON _BUILTIN_ALPHATEST_ON
#endif
#ifdef _BUILTIN_AlphaClip
#define _AlphaClip _BUILTIN_AlphaClip
#endif
#ifdef _BUILTIN_ALPHAPREMULTIPLY_ON
#define _ALPHAPREMULTIPLY_ON _BUILTIN_ALPHAPREMULTIPLY_ON
#endif


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Shim/Shims.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/LegacySurfaceVertex.hlsl"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/ShaderGraphFunctions.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
 float3 positionOS : POSITION;
 float3 normalOS : NORMAL;
 float4 tangentOS : TANGENT;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
 float4 positionCS : SV_POSITION;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};
struct SurfaceDescriptionInputs
{
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 AbsoluteWorldSpacePosition;
 float3 TimeParameters;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};

PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.positionCS = input.positionCS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.positionCS = input.positionCS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}


// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)

float _EnvironmentReactionSupported;
float4 _EnvState0, _EnvState1;
float _FuelCoverage, _AllowFuelCoverage;
float4 _WetTint, _CharTint, _FireColor;
float4 _EnvironmentCellBounds;
float _UseEnvironmentCells;

float _Height_from_Ground_Randomness;
float _Height_from_Ground_Softness;
float _Normal_Pow;
float _Normal_Mul;
float _Mask_Map_Size;
float4 _Coverage_Normal_Map_TexelSize;
float _Mask_Map_Intensity;
float4 _Coverage_ORM_Map_TexelSize;
float _Height_Weight;
float _Height_Gradience;
float _Wind_Intensity;
float3 _Wind_Direction;
float _Wind_Speed;
float _Wind_Time_Random_Offset_Amount;
float _Texture_Size;
float4 _Color_Map_TexelSize;
float4 _Color_Tint;
float4 _Normal_Map_TexelSize;
float _Normal_Intensity;
float4 _ORM_Map_TexelSize;
float _Roughness_Intensity;
float _AO_Intensity;
float _Metallic_Intensity;
float4 _Emissive_Map_TexelSize;
float4 _Emissive_Color;
float4 _Coverage_Color_Map_TexelSize;
float _Coverage_Size;
float _Height_from_Ground;
float4 _Mask_Map_TexelSize;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Coverage_Normal_Map);
SAMPLER(sampler_Coverage_Normal_Map);
TEXTURE2D(_Coverage_ORM_Map);
SAMPLER(sampler_Coverage_ORM_Map);
TEXTURE2D(_Color_Map);
SAMPLER(sampler_Color_Map);
TEXTURE2D(_Normal_Map);
SAMPLER(sampler_Normal_Map);
TEXTURE2D(_ORM_Map);
SAMPLER(sampler_ORM_Map);
TEXTURE2D(_Emissive_Map);
SAMPLER(sampler_Emissive_Map);
TEXTURE2D(_Coverage_Color_Map);
SAMPLER(sampler_Coverage_Color_Map);
TEXTURE2D(_Mask_Map);
SAMPLER(sampler_Mask_Map);

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Includes
// GraphIncludes: <None>

// Graph Functions

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Sine_float(float In, out float Out)
{
    Out = sin(In);
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Length_float3(float3 In, out float Out)
{
    Out = length(In);
}

void Unity_Divide_float(float A, float B, out float Out)
{
    Out = A / B;
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
{
    Out = lerp(A, B, T);
}

struct Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float
{
float3 ObjectSpacePosition;
float3 TimeParameters;
};

void SG_WindSway_51c4027ae50a8cd49be53c4990b9f179_float(float _Sway_Wind_Speed, float3 _Sway_Wind_Direction, float _Sway_Wind_Intensity, float _Sway_Wind_Time_Random_Offset_Amount, Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float IN, out float3 OutVector3_1)
{
float _Property_5c4cd19861f64014ae8e16af9365ef49_Out_0_Float = _Sway_Wind_Time_Random_Offset_Amount;
float _Add_4b31033339dc4f8e8e71674d98e662a3_Out_2_Float;
Unity_Add_float(IN.TimeParameters.x, _Property_5c4cd19861f64014ae8e16af9365ef49_Out_0_Float, _Add_4b31033339dc4f8e8e71674d98e662a3_Out_2_Float);
float _Property_9a93fa589ac7409884a94247349a940f_Out_0_Float = _Sway_Wind_Speed;
float _Multiply_7418655003ae4fedb2bf68678629472c_Out_2_Float;
Unity_Multiply_float_float(_Add_4b31033339dc4f8e8e71674d98e662a3_Out_2_Float, _Property_9a93fa589ac7409884a94247349a940f_Out_0_Float, _Multiply_7418655003ae4fedb2bf68678629472c_Out_2_Float);
float _Sine_ebd14b0fc42b4b4cb4387f3a1b879c1f_Out_1_Float;
Unity_Sine_float(_Multiply_7418655003ae4fedb2bf68678629472c_Out_2_Float, _Sine_ebd14b0fc42b4b4cb4387f3a1b879c1f_Out_1_Float);
float _Add_660e92170c564f94a79277cd2fd0cf20_Out_2_Float;
Unity_Add_float(_Sine_ebd14b0fc42b4b4cb4387f3a1b879c1f_Out_1_Float, float(1), _Add_660e92170c564f94a79277cd2fd0cf20_Out_2_Float);
float _Multiply_b8fd654b771447aebafc1a6aad6957cf_Out_2_Float;
Unity_Multiply_float_float(_Add_660e92170c564f94a79277cd2fd0cf20_Out_2_Float, 0.5, _Multiply_b8fd654b771447aebafc1a6aad6957cf_Out_2_Float);
float3 _Property_fa2ee033b44946c59c3fa9aea4a167c8_Out_0_Vector3 = _Sway_Wind_Direction;
float3 _Multiply_a5500b7f7b8e43988a055eea7eb917f3_Out_2_Vector3;
Unity_Multiply_float3_float3((_Multiply_b8fd654b771447aebafc1a6aad6957cf_Out_2_Float.xxx), _Property_fa2ee033b44946c59c3fa9aea4a167c8_Out_0_Vector3, _Multiply_a5500b7f7b8e43988a055eea7eb917f3_Out_2_Vector3);
float _Property_66cba0a3223546238e18b58e017e756f_Out_0_Float = _Sway_Wind_Intensity;
float3 _Multiply_475f81423e39483cba045bef08b9f967_Out_2_Vector3;
Unity_Multiply_float3_float3(_Multiply_a5500b7f7b8e43988a055eea7eb917f3_Out_2_Vector3, (_Property_66cba0a3223546238e18b58e017e756f_Out_0_Float.xxx), _Multiply_475f81423e39483cba045bef08b9f967_Out_2_Vector3);
float3 _Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3;
Unity_Add_float3(_Multiply_475f81423e39483cba045bef08b9f967_Out_2_Vector3, IN.ObjectSpacePosition, _Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3);
float _Length_53965ffb38de43359ab019a247194557_Out_1_Float;
Unity_Length_float3(IN.ObjectSpacePosition, _Length_53965ffb38de43359ab019a247194557_Out_1_Float);
float _Length_77b6b3d742104f2abf3c6968dea9a342_Out_1_Float;
Unity_Length_float3(_Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3, _Length_77b6b3d742104f2abf3c6968dea9a342_Out_1_Float);
float _Divide_f5dd43de79e343638d2743ee93ce7326_Out_2_Float;
Unity_Divide_float(_Length_53965ffb38de43359ab019a247194557_Out_1_Float, _Length_77b6b3d742104f2abf3c6968dea9a342_Out_1_Float, _Divide_f5dd43de79e343638d2743ee93ce7326_Out_2_Float);
float3 _Lerp_7574daf550e1428cb9d35656c7bb9603_Out_3_Vector3;
Unity_Lerp_float3(float3(0, 0, 0), _Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3, (_Divide_f5dd43de79e343638d2743ee93ce7326_Out_2_Float.xxx), _Lerp_7574daf550e1428cb9d35656c7bb9603_Out_3_Vector3);
OutVector3_1 = _Lerp_7574daf550e1428cb9d35656c7bb9603_Out_3_Vector3;
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

void Unity_Power_float(float A, float B, out float Out)
{
    Out = pow(A, B);
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 Position;
float3 Normal;
float3 Tangent;
};

VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
float _Property_b2e048f6276d4371b6ad931f5d1baa87_Out_0_Float = _Wind_Speed;
float3 _Property_7133a0551bc7429cb8bbfcbad044bf97_Out_0_Vector3 = _Wind_Direction;
float _Property_5aa2352867cd4da79dc9f9f9979925c7_Out_0_Float = _Wind_Intensity;
float _Property_e0c6ebff99e349d6966115347961a70c_Out_0_Float = _Wind_Time_Random_Offset_Amount;
Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf;
_WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf.ObjectSpacePosition = IN.ObjectSpacePosition;
_WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf.TimeParameters = IN.TimeParameters;
float3 _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf_OutVector3_1_Vector3;
SG_WindSway_51c4027ae50a8cd49be53c4990b9f179_float(_Property_b2e048f6276d4371b6ad931f5d1baa87_Out_0_Float, _Property_7133a0551bc7429cb8bbfcbad044bf97_Out_0_Vector3, _Property_5aa2352867cd4da79dc9f9f9979925c7_Out_0_Float, _Property_e0c6ebff99e349d6966115347961a70c_Out_0_Float, _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf, _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf_OutVector3_1_Vector3);
float _Split_38420bd2034746a1878048f0ead2a7b2_R_1_Float = IN.AbsoluteWorldSpacePosition[0];
float _Split_38420bd2034746a1878048f0ead2a7b2_G_2_Float = IN.AbsoluteWorldSpacePosition[1];
float _Split_38420bd2034746a1878048f0ead2a7b2_B_3_Float = IN.AbsoluteWorldSpacePosition[2];
float _Split_38420bd2034746a1878048f0ead2a7b2_A_4_Float = 0;
float3 _Vector3_0f8475b3a0df451ea79f6ea2e4e3f673_Out_0_Vector3 = float3(float(0), float(0), float(0));
float3 _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3;
{
// Converting Position from Object to AbsoluteWorld via world space
float3 world;
world = TransformObjectToWorld(_Vector3_0f8475b3a0df451ea79f6ea2e4e3f673_Out_0_Vector3.xyz);
_Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3 = GetAbsolutePositionWS(world);
}
float _Split_fb1b2d84926449e498da6c90f1436311_R_1_Float = _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3[0];
float _Split_fb1b2d84926449e498da6c90f1436311_G_2_Float = _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3[1];
float _Split_fb1b2d84926449e498da6c90f1436311_B_3_Float = _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3[2];
float _Split_fb1b2d84926449e498da6c90f1436311_A_4_Float = 0;
float _Subtract_18ddaa9851a547f1bc3eeafa55f3227b_Out_2_Float;
Unity_Subtract_float(_Split_38420bd2034746a1878048f0ead2a7b2_G_2_Float, _Split_fb1b2d84926449e498da6c90f1436311_G_2_Float, _Subtract_18ddaa9851a547f1bc3eeafa55f3227b_Out_2_Float);
float _Property_1db6873c977b47f186e42eef2cd75a57_Out_0_Float = _Height_Weight;
float _Split_bc5987163dba418d8de90e985de1247b_R_1_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[0];
float _Split_bc5987163dba418d8de90e985de1247b_G_2_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[1];
float _Split_bc5987163dba418d8de90e985de1247b_B_3_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[2];
float _Split_bc5987163dba418d8de90e985de1247b_A_4_Float = 0;
float _Multiply_89de5a5fb3cd451abb1227deba019c38_Out_2_Float;
Unity_Multiply_float_float(_Property_1db6873c977b47f186e42eef2cd75a57_Out_0_Float, _Split_bc5987163dba418d8de90e985de1247b_G_2_Float, _Multiply_89de5a5fb3cd451abb1227deba019c38_Out_2_Float);
float _Divide_2c406de6c63e463f9013e4faf1db1ae8_Out_2_Float;
Unity_Divide_float(_Subtract_18ddaa9851a547f1bc3eeafa55f3227b_Out_2_Float, _Multiply_89de5a5fb3cd451abb1227deba019c38_Out_2_Float, _Divide_2c406de6c63e463f9013e4faf1db1ae8_Out_2_Float);
float _Saturate_e9ea645ca89f482a9d0315df54a77ee5_Out_1_Float;
Unity_Saturate_float(_Divide_2c406de6c63e463f9013e4faf1db1ae8_Out_2_Float, _Saturate_e9ea645ca89f482a9d0315df54a77ee5_Out_1_Float);
float _Property_e96fd1920dff4533951da684129df200_Out_0_Float = _Height_Gradience;
float _Power_b88c5abc85424f17b03c60c98e9bc16c_Out_2_Float;
Unity_Power_float(_Saturate_e9ea645ca89f482a9d0315df54a77ee5_Out_1_Float, _Property_e96fd1920dff4533951da684129df200_Out_0_Float, _Power_b88c5abc85424f17b03c60c98e9bc16c_Out_2_Float);
float _Saturate_6c35d0fa108b483783bde88e06ff5ce0_Out_1_Float;
Unity_Saturate_float(_Power_b88c5abc85424f17b03c60c98e9bc16c_Out_2_Float, _Saturate_6c35d0fa108b483783bde88e06ff5ce0_Out_1_Float);
float3 _Lerp_8461a67d418941ee81ed9eb0f80e8a38_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf_OutVector3_1_Vector3, (_Saturate_6c35d0fa108b483783bde88e06ff5ce0_Out_1_Float.xxx), _Lerp_8461a67d418941ee81ed9eb0f80e8a38_Out_3_Vector3);
#if defined(_USE_WPO)
float3 _UseWPO_81cb92bff33547fbbfaa5ce2f60ba97c_Out_0_Vector3 = _Lerp_8461a67d418941ee81ed9eb0f80e8a38_Out_3_Vector3;
#else
float3 _UseWPO_81cb92bff33547fbbfaa5ce2f60ba97c_Out_0_Vector3 = IN.ObjectSpacePosition;
#endif
description.Position = _UseWPO_81cb92bff33547fbbfaa5ce2f60ba97c_Out_0_Vector3;
description.Normal = IN.ObjectSpaceNormal;
description.Tangent = IN.ObjectSpaceTangent;
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
float3 reactionUnusedColor = 0, reactionUnusedEmission = 0;
ClipVegetationFuel(TransformObjectToWorld(float3(0,0,0)));
return surface;
}

// --------------------------------------------------
// Build Graph Inputs

VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

    output.ObjectSpaceNormal =                          input.normalOS;
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
    output.AbsoluteWorldSpacePosition =                 GetAbsolutePositionWS(TransformObjectToWorld(input.positionOS));
    output.TimeParameters =                             _TimeParameters.xyz;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

    






    #if UNITY_UV_STARTS_AT_TOP
    #else
    #endif


#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

void BuildAppDataFull(Attributes attributes, VertexDescription vertexDescription, inout appdata_full result)
{
    result.vertex     = float4(attributes.positionOS, 1);
    result.tangent    = attributes.tangentOS;
    result.normal     = attributes.normalOS;
    result.vertex     = float4(vertexDescription.Position, 1);
    result.normal     = vertexDescription.Normal;
    result.tangent    = float4(vertexDescription.Tangent, 0);
    #if UNITY_ANY_INSTANCING_ENABLED
    #endif
}

void VaryingsToSurfaceVertex(Varyings varyings, inout v2f_surf result)
{
    result.pos = varyings.positionCS;
    // World Tangent isn't an available input on v2f_surf


    #if UNITY_ANY_INSTANCING_ENABLED
    #endif
    #if UNITY_SHOULD_SAMPLE_SH
    #if !defined(LIGHTMAP_ON)
    #endif
    #endif
    #if defined(LIGHTMAP_ON)
    #endif
    #ifdef VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
        result.fogCoord = varyings.fogFactorAndVertexLight.x;
        COPY_TO_LIGHT_COORDS(result, varyings.fogFactorAndVertexLight.yzw);
    #endif

    DEFAULT_UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(varyings, result);
}

void SurfaceVertexToVaryings(v2f_surf surfVertex, inout Varyings result)
{
    result.positionCS = surfVertex.pos;
    // viewDirectionWS is never filled out in the legacy pass' function. Always use the value computed by SRP
    // World Tangent isn't an available input on v2f_surf

    #if UNITY_ANY_INSTANCING_ENABLED
    #endif
    #if UNITY_SHOULD_SAMPLE_SH
    #if !defined(LIGHTMAP_ON)
    #endif
    #endif
    #if defined(LIGHTMAP_ON)
    #endif
    #ifdef VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
        result.fogFactorAndVertexLight.x = surfVertex.fogCoord;
        COPY_FROM_LIGHT_COORDS(result.fogFactorAndVertexLight.yzw, surfVertex);
    #endif

    DEFAULT_UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(surfVertex, result);
}

// --------------------------------------------------
// Main

#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/Varyings.hlsl"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/ShadowCasterPass.hlsl"

ENDHLSL
}
Pass
{
    Name "DepthOnly"
    Tags
    {
        "LightMode" = "DepthOnly"
    }

// Render State
Cull Back
Blend One Zero
ZTest LEqual
ZWrite On
ColorMask 0

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 3.0
#pragma multi_compile_instancing
#pragma vertex vert
#pragma fragment frag

// Keywords
// PassKeywords: <None>
#pragma shader_feature_local _ _USE_EMISSIVE
#pragma shader_feature_local _ _USE_WPO
#pragma shader_feature_local _ _USE_ORM
#pragma shader_feature_local _ _USE_NORMAL_MAPS
#pragma shader_feature_local _ _USE_COVERAGE
#pragma shader_feature_local _ _USE_COVERAGE_MASK_TEXTURE

#if defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_2
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_3
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_4
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_5
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_6
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM)
#define KEYWORD_PERMUTATION_7
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_8
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_9
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_10
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_11
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_12
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_13
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_14
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO)
#define KEYWORD_PERMUTATION_15
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_16
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_17
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_18
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_19
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_20
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_21
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_22
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM)
#define KEYWORD_PERMUTATION_23
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_24
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_25
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_26
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_27
#elif defined(_USE_EMISSIVE) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_28
#elif defined(_USE_EMISSIVE) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_29
#elif defined(_USE_EMISSIVE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_30
#elif defined(_USE_EMISSIVE)
#define KEYWORD_PERMUTATION_31
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_32
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_33
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_34
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_35
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_36
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_37
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_38
#elif defined(_USE_WPO) && defined(_USE_ORM)
#define KEYWORD_PERMUTATION_39
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_40
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_41
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_42
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_43
#elif defined(_USE_WPO) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_44
#elif defined(_USE_WPO) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_45
#elif defined(_USE_WPO) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_46
#elif defined(_USE_WPO)
#define KEYWORD_PERMUTATION_47
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_48
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_49
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_50
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_51
#elif defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_52
#elif defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_53
#elif defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_54
#elif defined(_USE_ORM)
#define KEYWORD_PERMUTATION_55
#elif defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_56
#elif defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_57
#elif defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_58
#elif defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_59
#elif defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_60
#elif defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_61
#elif defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_62
#else
#define KEYWORD_PERMUTATION_63
#endif


// Defines
#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_DEPTHONLY
#define BUILTIN_TARGET_API 1
#ifdef _BUILTIN_SURFACE_TYPE_TRANSPARENT
#define _SURFACE_TYPE_TRANSPARENT _BUILTIN_SURFACE_TYPE_TRANSPARENT
#endif
#ifdef _BUILTIN_ALPHATEST_ON
#define _ALPHATEST_ON _BUILTIN_ALPHATEST_ON
#endif
#ifdef _BUILTIN_AlphaClip
#define _AlphaClip _BUILTIN_AlphaClip
#endif
#ifdef _BUILTIN_ALPHAPREMULTIPLY_ON
#define _ALPHAPREMULTIPLY_ON _BUILTIN_ALPHAPREMULTIPLY_ON
#endif


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Shim/Shims.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/LegacySurfaceVertex.hlsl"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/ShaderGraphFunctions.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
 float3 positionOS : POSITION;
 float3 normalOS : NORMAL;
 float4 tangentOS : TANGENT;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
 float4 positionCS : SV_POSITION;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};
struct SurfaceDescriptionInputs
{
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 AbsoluteWorldSpacePosition;
 float3 TimeParameters;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};

PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.positionCS = input.positionCS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.positionCS = input.positionCS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}


// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)

float _EnvironmentReactionSupported;
float4 _EnvState0, _EnvState1;
float _FuelCoverage, _AllowFuelCoverage;
float4 _WetTint, _CharTint, _FireColor;
float4 _EnvironmentCellBounds;
float _UseEnvironmentCells;

float _Height_from_Ground_Randomness;
float _Height_from_Ground_Softness;
float _Normal_Pow;
float _Normal_Mul;
float _Mask_Map_Size;
float4 _Coverage_Normal_Map_TexelSize;
float _Mask_Map_Intensity;
float4 _Coverage_ORM_Map_TexelSize;
float _Height_Weight;
float _Height_Gradience;
float _Wind_Intensity;
float3 _Wind_Direction;
float _Wind_Speed;
float _Wind_Time_Random_Offset_Amount;
float _Texture_Size;
float4 _Color_Map_TexelSize;
float4 _Color_Tint;
float4 _Normal_Map_TexelSize;
float _Normal_Intensity;
float4 _ORM_Map_TexelSize;
float _Roughness_Intensity;
float _AO_Intensity;
float _Metallic_Intensity;
float4 _Emissive_Map_TexelSize;
float4 _Emissive_Color;
float4 _Coverage_Color_Map_TexelSize;
float _Coverage_Size;
float _Height_from_Ground;
float4 _Mask_Map_TexelSize;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Coverage_Normal_Map);
SAMPLER(sampler_Coverage_Normal_Map);
TEXTURE2D(_Coverage_ORM_Map);
SAMPLER(sampler_Coverage_ORM_Map);
TEXTURE2D(_Color_Map);
SAMPLER(sampler_Color_Map);
TEXTURE2D(_Normal_Map);
SAMPLER(sampler_Normal_Map);
TEXTURE2D(_ORM_Map);
SAMPLER(sampler_ORM_Map);
TEXTURE2D(_Emissive_Map);
SAMPLER(sampler_Emissive_Map);
TEXTURE2D(_Coverage_Color_Map);
SAMPLER(sampler_Coverage_Color_Map);
TEXTURE2D(_Mask_Map);
SAMPLER(sampler_Mask_Map);

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Includes
// GraphIncludes: <None>

// Graph Functions

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Sine_float(float In, out float Out)
{
    Out = sin(In);
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Length_float3(float3 In, out float Out)
{
    Out = length(In);
}

void Unity_Divide_float(float A, float B, out float Out)
{
    Out = A / B;
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
{
    Out = lerp(A, B, T);
}

struct Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float
{
float3 ObjectSpacePosition;
float3 TimeParameters;
};

void SG_WindSway_51c4027ae50a8cd49be53c4990b9f179_float(float _Sway_Wind_Speed, float3 _Sway_Wind_Direction, float _Sway_Wind_Intensity, float _Sway_Wind_Time_Random_Offset_Amount, Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float IN, out float3 OutVector3_1)
{
float _Property_5c4cd19861f64014ae8e16af9365ef49_Out_0_Float = _Sway_Wind_Time_Random_Offset_Amount;
float _Add_4b31033339dc4f8e8e71674d98e662a3_Out_2_Float;
Unity_Add_float(IN.TimeParameters.x, _Property_5c4cd19861f64014ae8e16af9365ef49_Out_0_Float, _Add_4b31033339dc4f8e8e71674d98e662a3_Out_2_Float);
float _Property_9a93fa589ac7409884a94247349a940f_Out_0_Float = _Sway_Wind_Speed;
float _Multiply_7418655003ae4fedb2bf68678629472c_Out_2_Float;
Unity_Multiply_float_float(_Add_4b31033339dc4f8e8e71674d98e662a3_Out_2_Float, _Property_9a93fa589ac7409884a94247349a940f_Out_0_Float, _Multiply_7418655003ae4fedb2bf68678629472c_Out_2_Float);
float _Sine_ebd14b0fc42b4b4cb4387f3a1b879c1f_Out_1_Float;
Unity_Sine_float(_Multiply_7418655003ae4fedb2bf68678629472c_Out_2_Float, _Sine_ebd14b0fc42b4b4cb4387f3a1b879c1f_Out_1_Float);
float _Add_660e92170c564f94a79277cd2fd0cf20_Out_2_Float;
Unity_Add_float(_Sine_ebd14b0fc42b4b4cb4387f3a1b879c1f_Out_1_Float, float(1), _Add_660e92170c564f94a79277cd2fd0cf20_Out_2_Float);
float _Multiply_b8fd654b771447aebafc1a6aad6957cf_Out_2_Float;
Unity_Multiply_float_float(_Add_660e92170c564f94a79277cd2fd0cf20_Out_2_Float, 0.5, _Multiply_b8fd654b771447aebafc1a6aad6957cf_Out_2_Float);
float3 _Property_fa2ee033b44946c59c3fa9aea4a167c8_Out_0_Vector3 = _Sway_Wind_Direction;
float3 _Multiply_a5500b7f7b8e43988a055eea7eb917f3_Out_2_Vector3;
Unity_Multiply_float3_float3((_Multiply_b8fd654b771447aebafc1a6aad6957cf_Out_2_Float.xxx), _Property_fa2ee033b44946c59c3fa9aea4a167c8_Out_0_Vector3, _Multiply_a5500b7f7b8e43988a055eea7eb917f3_Out_2_Vector3);
float _Property_66cba0a3223546238e18b58e017e756f_Out_0_Float = _Sway_Wind_Intensity;
float3 _Multiply_475f81423e39483cba045bef08b9f967_Out_2_Vector3;
Unity_Multiply_float3_float3(_Multiply_a5500b7f7b8e43988a055eea7eb917f3_Out_2_Vector3, (_Property_66cba0a3223546238e18b58e017e756f_Out_0_Float.xxx), _Multiply_475f81423e39483cba045bef08b9f967_Out_2_Vector3);
float3 _Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3;
Unity_Add_float3(_Multiply_475f81423e39483cba045bef08b9f967_Out_2_Vector3, IN.ObjectSpacePosition, _Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3);
float _Length_53965ffb38de43359ab019a247194557_Out_1_Float;
Unity_Length_float3(IN.ObjectSpacePosition, _Length_53965ffb38de43359ab019a247194557_Out_1_Float);
float _Length_77b6b3d742104f2abf3c6968dea9a342_Out_1_Float;
Unity_Length_float3(_Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3, _Length_77b6b3d742104f2abf3c6968dea9a342_Out_1_Float);
float _Divide_f5dd43de79e343638d2743ee93ce7326_Out_2_Float;
Unity_Divide_float(_Length_53965ffb38de43359ab019a247194557_Out_1_Float, _Length_77b6b3d742104f2abf3c6968dea9a342_Out_1_Float, _Divide_f5dd43de79e343638d2743ee93ce7326_Out_2_Float);
float3 _Lerp_7574daf550e1428cb9d35656c7bb9603_Out_3_Vector3;
Unity_Lerp_float3(float3(0, 0, 0), _Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3, (_Divide_f5dd43de79e343638d2743ee93ce7326_Out_2_Float.xxx), _Lerp_7574daf550e1428cb9d35656c7bb9603_Out_3_Vector3);
OutVector3_1 = _Lerp_7574daf550e1428cb9d35656c7bb9603_Out_3_Vector3;
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

void Unity_Power_float(float A, float B, out float Out)
{
    Out = pow(A, B);
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 Position;
float3 Normal;
float3 Tangent;
};

VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
float _Property_b2e048f6276d4371b6ad931f5d1baa87_Out_0_Float = _Wind_Speed;
float3 _Property_7133a0551bc7429cb8bbfcbad044bf97_Out_0_Vector3 = _Wind_Direction;
float _Property_5aa2352867cd4da79dc9f9f9979925c7_Out_0_Float = _Wind_Intensity;
float _Property_e0c6ebff99e349d6966115347961a70c_Out_0_Float = _Wind_Time_Random_Offset_Amount;
Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf;
_WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf.ObjectSpacePosition = IN.ObjectSpacePosition;
_WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf.TimeParameters = IN.TimeParameters;
float3 _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf_OutVector3_1_Vector3;
SG_WindSway_51c4027ae50a8cd49be53c4990b9f179_float(_Property_b2e048f6276d4371b6ad931f5d1baa87_Out_0_Float, _Property_7133a0551bc7429cb8bbfcbad044bf97_Out_0_Vector3, _Property_5aa2352867cd4da79dc9f9f9979925c7_Out_0_Float, _Property_e0c6ebff99e349d6966115347961a70c_Out_0_Float, _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf, _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf_OutVector3_1_Vector3);
float _Split_38420bd2034746a1878048f0ead2a7b2_R_1_Float = IN.AbsoluteWorldSpacePosition[0];
float _Split_38420bd2034746a1878048f0ead2a7b2_G_2_Float = IN.AbsoluteWorldSpacePosition[1];
float _Split_38420bd2034746a1878048f0ead2a7b2_B_3_Float = IN.AbsoluteWorldSpacePosition[2];
float _Split_38420bd2034746a1878048f0ead2a7b2_A_4_Float = 0;
float3 _Vector3_0f8475b3a0df451ea79f6ea2e4e3f673_Out_0_Vector3 = float3(float(0), float(0), float(0));
float3 _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3;
{
// Converting Position from Object to AbsoluteWorld via world space
float3 world;
world = TransformObjectToWorld(_Vector3_0f8475b3a0df451ea79f6ea2e4e3f673_Out_0_Vector3.xyz);
_Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3 = GetAbsolutePositionWS(world);
}
float _Split_fb1b2d84926449e498da6c90f1436311_R_1_Float = _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3[0];
float _Split_fb1b2d84926449e498da6c90f1436311_G_2_Float = _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3[1];
float _Split_fb1b2d84926449e498da6c90f1436311_B_3_Float = _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3[2];
float _Split_fb1b2d84926449e498da6c90f1436311_A_4_Float = 0;
float _Subtract_18ddaa9851a547f1bc3eeafa55f3227b_Out_2_Float;
Unity_Subtract_float(_Split_38420bd2034746a1878048f0ead2a7b2_G_2_Float, _Split_fb1b2d84926449e498da6c90f1436311_G_2_Float, _Subtract_18ddaa9851a547f1bc3eeafa55f3227b_Out_2_Float);
float _Property_1db6873c977b47f186e42eef2cd75a57_Out_0_Float = _Height_Weight;
float _Split_bc5987163dba418d8de90e985de1247b_R_1_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[0];
float _Split_bc5987163dba418d8de90e985de1247b_G_2_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[1];
float _Split_bc5987163dba418d8de90e985de1247b_B_3_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[2];
float _Split_bc5987163dba418d8de90e985de1247b_A_4_Float = 0;
float _Multiply_89de5a5fb3cd451abb1227deba019c38_Out_2_Float;
Unity_Multiply_float_float(_Property_1db6873c977b47f186e42eef2cd75a57_Out_0_Float, _Split_bc5987163dba418d8de90e985de1247b_G_2_Float, _Multiply_89de5a5fb3cd451abb1227deba019c38_Out_2_Float);
float _Divide_2c406de6c63e463f9013e4faf1db1ae8_Out_2_Float;
Unity_Divide_float(_Subtract_18ddaa9851a547f1bc3eeafa55f3227b_Out_2_Float, _Multiply_89de5a5fb3cd451abb1227deba019c38_Out_2_Float, _Divide_2c406de6c63e463f9013e4faf1db1ae8_Out_2_Float);
float _Saturate_e9ea645ca89f482a9d0315df54a77ee5_Out_1_Float;
Unity_Saturate_float(_Divide_2c406de6c63e463f9013e4faf1db1ae8_Out_2_Float, _Saturate_e9ea645ca89f482a9d0315df54a77ee5_Out_1_Float);
float _Property_e96fd1920dff4533951da684129df200_Out_0_Float = _Height_Gradience;
float _Power_b88c5abc85424f17b03c60c98e9bc16c_Out_2_Float;
Unity_Power_float(_Saturate_e9ea645ca89f482a9d0315df54a77ee5_Out_1_Float, _Property_e96fd1920dff4533951da684129df200_Out_0_Float, _Power_b88c5abc85424f17b03c60c98e9bc16c_Out_2_Float);
float _Saturate_6c35d0fa108b483783bde88e06ff5ce0_Out_1_Float;
Unity_Saturate_float(_Power_b88c5abc85424f17b03c60c98e9bc16c_Out_2_Float, _Saturate_6c35d0fa108b483783bde88e06ff5ce0_Out_1_Float);
float3 _Lerp_8461a67d418941ee81ed9eb0f80e8a38_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf_OutVector3_1_Vector3, (_Saturate_6c35d0fa108b483783bde88e06ff5ce0_Out_1_Float.xxx), _Lerp_8461a67d418941ee81ed9eb0f80e8a38_Out_3_Vector3);
#if defined(_USE_WPO)
float3 _UseWPO_81cb92bff33547fbbfaa5ce2f60ba97c_Out_0_Vector3 = _Lerp_8461a67d418941ee81ed9eb0f80e8a38_Out_3_Vector3;
#else
float3 _UseWPO_81cb92bff33547fbbfaa5ce2f60ba97c_Out_0_Vector3 = IN.ObjectSpacePosition;
#endif
description.Position = _UseWPO_81cb92bff33547fbbfaa5ce2f60ba97c_Out_0_Vector3;
description.Normal = IN.ObjectSpaceNormal;
description.Tangent = IN.ObjectSpaceTangent;
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
float3 reactionUnusedColor = 0, reactionUnusedEmission = 0;
ClipVegetationFuel(TransformObjectToWorld(float3(0,0,0)));
return surface;
}

// --------------------------------------------------
// Build Graph Inputs

VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

    output.ObjectSpaceNormal =                          input.normalOS;
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
    output.AbsoluteWorldSpacePosition =                 GetAbsolutePositionWS(TransformObjectToWorld(input.positionOS));
    output.TimeParameters =                             _TimeParameters.xyz;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

    






    #if UNITY_UV_STARTS_AT_TOP
    #else
    #endif


#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

void BuildAppDataFull(Attributes attributes, VertexDescription vertexDescription, inout appdata_full result)
{
    result.vertex     = float4(attributes.positionOS, 1);
    result.tangent    = attributes.tangentOS;
    result.normal     = attributes.normalOS;
    result.vertex     = float4(vertexDescription.Position, 1);
    result.normal     = vertexDescription.Normal;
    result.tangent    = float4(vertexDescription.Tangent, 0);
    #if UNITY_ANY_INSTANCING_ENABLED
    #endif
}

void VaryingsToSurfaceVertex(Varyings varyings, inout v2f_surf result)
{
    result.pos = varyings.positionCS;
    // World Tangent isn't an available input on v2f_surf


    #if UNITY_ANY_INSTANCING_ENABLED
    #endif
    #if UNITY_SHOULD_SAMPLE_SH
    #if !defined(LIGHTMAP_ON)
    #endif
    #endif
    #if defined(LIGHTMAP_ON)
    #endif
    #ifdef VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
        result.fogCoord = varyings.fogFactorAndVertexLight.x;
        COPY_TO_LIGHT_COORDS(result, varyings.fogFactorAndVertexLight.yzw);
    #endif

    DEFAULT_UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(varyings, result);
}

void SurfaceVertexToVaryings(v2f_surf surfVertex, inout Varyings result)
{
    result.positionCS = surfVertex.pos;
    // viewDirectionWS is never filled out in the legacy pass' function. Always use the value computed by SRP
    // World Tangent isn't an available input on v2f_surf

    #if UNITY_ANY_INSTANCING_ENABLED
    #endif
    #if UNITY_SHOULD_SAMPLE_SH
    #if !defined(LIGHTMAP_ON)
    #endif
    #endif
    #if defined(LIGHTMAP_ON)
    #endif
    #ifdef VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
        result.fogFactorAndVertexLight.x = surfVertex.fogCoord;
        COPY_FROM_LIGHT_COORDS(result.fogFactorAndVertexLight.yzw, surfVertex);
    #endif

    DEFAULT_UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(surfVertex, result);
}

// --------------------------------------------------
// Main

#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/Varyings.hlsl"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/DepthOnlyPass.hlsl"

ENDHLSL
}
Pass
{
    Name "Meta"
    Tags
    {
        "LightMode" = "Meta"
    }

// Render State
Cull Off

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 3.0
#pragma vertex vert
#pragma fragment frag

// Keywords
#pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
#pragma shader_feature_local _ _USE_EMISSIVE
#pragma shader_feature_local _ _USE_WPO
#pragma shader_feature_local _ _USE_ORM
#pragma shader_feature_local _ _USE_NORMAL_MAPS
#pragma shader_feature_local _ _USE_COVERAGE
#pragma shader_feature_local _ _USE_COVERAGE_MASK_TEXTURE

#if defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_2
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_3
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_4
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_5
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_6
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM)
#define KEYWORD_PERMUTATION_7
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_8
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_9
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_10
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_11
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_12
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_13
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_14
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO)
#define KEYWORD_PERMUTATION_15
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_16
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_17
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_18
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_19
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_20
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_21
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_22
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM)
#define KEYWORD_PERMUTATION_23
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_24
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_25
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_26
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_27
#elif defined(_USE_EMISSIVE) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_28
#elif defined(_USE_EMISSIVE) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_29
#elif defined(_USE_EMISSIVE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_30
#elif defined(_USE_EMISSIVE)
#define KEYWORD_PERMUTATION_31
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_32
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_33
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_34
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_35
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_36
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_37
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_38
#elif defined(_USE_WPO) && defined(_USE_ORM)
#define KEYWORD_PERMUTATION_39
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_40
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_41
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_42
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_43
#elif defined(_USE_WPO) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_44
#elif defined(_USE_WPO) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_45
#elif defined(_USE_WPO) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_46
#elif defined(_USE_WPO)
#define KEYWORD_PERMUTATION_47
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_48
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_49
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_50
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_51
#elif defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_52
#elif defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_53
#elif defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_54
#elif defined(_USE_ORM)
#define KEYWORD_PERMUTATION_55
#elif defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_56
#elif defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_57
#elif defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_58
#elif defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_59
#elif defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_60
#elif defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_61
#elif defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_62
#else
#define KEYWORD_PERMUTATION_63
#endif


// Defines
#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_TEXCOORD1
#define ATTRIBUTES_NEED_TEXCOORD2
#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_NORMAL_WS
#define VARYINGS_NEED_TEXCOORD0
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_META
#define BUILTIN_TARGET_API 1
#ifdef _BUILTIN_SURFACE_TYPE_TRANSPARENT
#define _SURFACE_TYPE_TRANSPARENT _BUILTIN_SURFACE_TYPE_TRANSPARENT
#endif
#ifdef _BUILTIN_ALPHATEST_ON
#define _ALPHATEST_ON _BUILTIN_ALPHATEST_ON
#endif
#ifdef _BUILTIN_AlphaClip
#define _AlphaClip _BUILTIN_AlphaClip
#endif
#ifdef _BUILTIN_ALPHAPREMULTIPLY_ON
#define _ALPHAPREMULTIPLY_ON _BUILTIN_ALPHAPREMULTIPLY_ON
#endif


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Shim/Shims.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/LegacySurfaceVertex.hlsl"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/ShaderGraphFunctions.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
 float3 positionOS : POSITION;
 float3 normalOS : NORMAL;
 float4 tangentOS : TANGENT;
 float4 uv0 : TEXCOORD0;
 float4 uv1 : TEXCOORD1;
 float4 uv2 : TEXCOORD2;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
 float4 positionCS : SV_POSITION;
 float3 positionWS;
 float3 normalWS;
 float4 texCoord0;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};
struct SurfaceDescriptionInputs
{
 float3 WorldSpaceNormal;
 float3 ObjectSpacePosition;
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 AbsoluteWorldSpacePosition;
 float3 TimeParameters;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
 float4 texCoord0 : INTERP0;
 float3 positionWS : INTERP1;
 float3 normalWS : INTERP2;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};

PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.positionCS = input.positionCS;
output.texCoord0.xyzw = input.texCoord0;
output.positionWS.xyz = input.positionWS;
output.normalWS.xyz = input.normalWS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.positionCS = input.positionCS;
output.texCoord0 = input.texCoord0.xyzw;
output.positionWS = input.positionWS.xyz;
output.normalWS = input.normalWS.xyz;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}


// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)

float _EnvironmentReactionSupported;
float4 _EnvState0, _EnvState1;
float _FuelCoverage, _AllowFuelCoverage;
float4 _WetTint, _CharTint, _FireColor;
float4 _EnvironmentCellBounds;
float _UseEnvironmentCells;

float _Height_from_Ground_Randomness;
float _Height_from_Ground_Softness;
float _Normal_Pow;
float _Normal_Mul;
float _Mask_Map_Size;
float4 _Coverage_Normal_Map_TexelSize;
float _Mask_Map_Intensity;
float4 _Coverage_ORM_Map_TexelSize;
float _Height_Weight;
float _Height_Gradience;
float _Wind_Intensity;
float3 _Wind_Direction;
float _Wind_Speed;
float _Wind_Time_Random_Offset_Amount;
float _Texture_Size;
float4 _Color_Map_TexelSize;
float4 _Color_Tint;
float4 _Normal_Map_TexelSize;
float _Normal_Intensity;
float4 _ORM_Map_TexelSize;
float _Roughness_Intensity;
float _AO_Intensity;
float _Metallic_Intensity;
float4 _Emissive_Map_TexelSize;
float4 _Emissive_Color;
float4 _Coverage_Color_Map_TexelSize;
float _Coverage_Size;
float _Height_from_Ground;
float4 _Mask_Map_TexelSize;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Coverage_Normal_Map);
SAMPLER(sampler_Coverage_Normal_Map);
TEXTURE2D(_Coverage_ORM_Map);
SAMPLER(sampler_Coverage_ORM_Map);
TEXTURE2D(_Color_Map);
SAMPLER(sampler_Color_Map);
TEXTURE2D(_Normal_Map);
SAMPLER(sampler_Normal_Map);
TEXTURE2D(_ORM_Map);
SAMPLER(sampler_ORM_Map);
TEXTURE2D(_Emissive_Map);
SAMPLER(sampler_Emissive_Map);
TEXTURE2D(_Coverage_Color_Map);
SAMPLER(sampler_Coverage_Color_Map);
TEXTURE2D(_Mask_Map);
SAMPLER(sampler_Mask_Map);

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Includes
// GraphIncludes: <None>

// Graph Functions

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Sine_float(float In, out float Out)
{
    Out = sin(In);
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Length_float3(float3 In, out float Out)
{
    Out = length(In);
}

void Unity_Divide_float(float A, float B, out float Out)
{
    Out = A / B;
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
{
    Out = lerp(A, B, T);
}

struct Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float
{
float3 ObjectSpacePosition;
float3 TimeParameters;
};

void SG_WindSway_51c4027ae50a8cd49be53c4990b9f179_float(float _Sway_Wind_Speed, float3 _Sway_Wind_Direction, float _Sway_Wind_Intensity, float _Sway_Wind_Time_Random_Offset_Amount, Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float IN, out float3 OutVector3_1)
{
float _Property_5c4cd19861f64014ae8e16af9365ef49_Out_0_Float = _Sway_Wind_Time_Random_Offset_Amount;
float _Add_4b31033339dc4f8e8e71674d98e662a3_Out_2_Float;
Unity_Add_float(IN.TimeParameters.x, _Property_5c4cd19861f64014ae8e16af9365ef49_Out_0_Float, _Add_4b31033339dc4f8e8e71674d98e662a3_Out_2_Float);
float _Property_9a93fa589ac7409884a94247349a940f_Out_0_Float = _Sway_Wind_Speed;
float _Multiply_7418655003ae4fedb2bf68678629472c_Out_2_Float;
Unity_Multiply_float_float(_Add_4b31033339dc4f8e8e71674d98e662a3_Out_2_Float, _Property_9a93fa589ac7409884a94247349a940f_Out_0_Float, _Multiply_7418655003ae4fedb2bf68678629472c_Out_2_Float);
float _Sine_ebd14b0fc42b4b4cb4387f3a1b879c1f_Out_1_Float;
Unity_Sine_float(_Multiply_7418655003ae4fedb2bf68678629472c_Out_2_Float, _Sine_ebd14b0fc42b4b4cb4387f3a1b879c1f_Out_1_Float);
float _Add_660e92170c564f94a79277cd2fd0cf20_Out_2_Float;
Unity_Add_float(_Sine_ebd14b0fc42b4b4cb4387f3a1b879c1f_Out_1_Float, float(1), _Add_660e92170c564f94a79277cd2fd0cf20_Out_2_Float);
float _Multiply_b8fd654b771447aebafc1a6aad6957cf_Out_2_Float;
Unity_Multiply_float_float(_Add_660e92170c564f94a79277cd2fd0cf20_Out_2_Float, 0.5, _Multiply_b8fd654b771447aebafc1a6aad6957cf_Out_2_Float);
float3 _Property_fa2ee033b44946c59c3fa9aea4a167c8_Out_0_Vector3 = _Sway_Wind_Direction;
float3 _Multiply_a5500b7f7b8e43988a055eea7eb917f3_Out_2_Vector3;
Unity_Multiply_float3_float3((_Multiply_b8fd654b771447aebafc1a6aad6957cf_Out_2_Float.xxx), _Property_fa2ee033b44946c59c3fa9aea4a167c8_Out_0_Vector3, _Multiply_a5500b7f7b8e43988a055eea7eb917f3_Out_2_Vector3);
float _Property_66cba0a3223546238e18b58e017e756f_Out_0_Float = _Sway_Wind_Intensity;
float3 _Multiply_475f81423e39483cba045bef08b9f967_Out_2_Vector3;
Unity_Multiply_float3_float3(_Multiply_a5500b7f7b8e43988a055eea7eb917f3_Out_2_Vector3, (_Property_66cba0a3223546238e18b58e017e756f_Out_0_Float.xxx), _Multiply_475f81423e39483cba045bef08b9f967_Out_2_Vector3);
float3 _Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3;
Unity_Add_float3(_Multiply_475f81423e39483cba045bef08b9f967_Out_2_Vector3, IN.ObjectSpacePosition, _Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3);
float _Length_53965ffb38de43359ab019a247194557_Out_1_Float;
Unity_Length_float3(IN.ObjectSpacePosition, _Length_53965ffb38de43359ab019a247194557_Out_1_Float);
float _Length_77b6b3d742104f2abf3c6968dea9a342_Out_1_Float;
Unity_Length_float3(_Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3, _Length_77b6b3d742104f2abf3c6968dea9a342_Out_1_Float);
float _Divide_f5dd43de79e343638d2743ee93ce7326_Out_2_Float;
Unity_Divide_float(_Length_53965ffb38de43359ab019a247194557_Out_1_Float, _Length_77b6b3d742104f2abf3c6968dea9a342_Out_1_Float, _Divide_f5dd43de79e343638d2743ee93ce7326_Out_2_Float);
float3 _Lerp_7574daf550e1428cb9d35656c7bb9603_Out_3_Vector3;
Unity_Lerp_float3(float3(0, 0, 0), _Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3, (_Divide_f5dd43de79e343638d2743ee93ce7326_Out_2_Float.xxx), _Lerp_7574daf550e1428cb9d35656c7bb9603_Out_3_Vector3);
OutVector3_1 = _Lerp_7574daf550e1428cb9d35656c7bb9603_Out_3_Vector3;
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

void Unity_Power_float(float A, float B, out float Out)
{
    Out = pow(A, B);
}

void Unity_Divide_float4(float4 A, float4 B, out float4 Out)
{
    Out = A / B;
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

void Unity_Normalize_float3(float3 In, out float3 Out)
{
    Out = normalize(In);
}

void Unity_RandomRange_float(float2 Seed, float Min, float Max, out float Out)
{
     float randomno =  frac(sin(dot(Seed, float2(12.9898, 78.233)))*43758.5453);
     Out = lerp(Min, Max, randomno);
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
}

void Unity_DotProduct_float3(float3 A, float3 B, out float Out)
{
    Out = dot(A, B);
}

void Unity_Step_float(float Edge, float In, out float Out)
{
    Out = step(Edge, In);
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 Position;
float3 Normal;
float3 Tangent;
};

VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
float _Property_b2e048f6276d4371b6ad931f5d1baa87_Out_0_Float = _Wind_Speed;
float3 _Property_7133a0551bc7429cb8bbfcbad044bf97_Out_0_Vector3 = _Wind_Direction;
float _Property_5aa2352867cd4da79dc9f9f9979925c7_Out_0_Float = _Wind_Intensity;
float _Property_e0c6ebff99e349d6966115347961a70c_Out_0_Float = _Wind_Time_Random_Offset_Amount;
Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf;
_WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf.ObjectSpacePosition = IN.ObjectSpacePosition;
_WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf.TimeParameters = IN.TimeParameters;
float3 _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf_OutVector3_1_Vector3;
SG_WindSway_51c4027ae50a8cd49be53c4990b9f179_float(_Property_b2e048f6276d4371b6ad931f5d1baa87_Out_0_Float, _Property_7133a0551bc7429cb8bbfcbad044bf97_Out_0_Vector3, _Property_5aa2352867cd4da79dc9f9f9979925c7_Out_0_Float, _Property_e0c6ebff99e349d6966115347961a70c_Out_0_Float, _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf, _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf_OutVector3_1_Vector3);
float _Split_38420bd2034746a1878048f0ead2a7b2_R_1_Float = IN.AbsoluteWorldSpacePosition[0];
float _Split_38420bd2034746a1878048f0ead2a7b2_G_2_Float = IN.AbsoluteWorldSpacePosition[1];
float _Split_38420bd2034746a1878048f0ead2a7b2_B_3_Float = IN.AbsoluteWorldSpacePosition[2];
float _Split_38420bd2034746a1878048f0ead2a7b2_A_4_Float = 0;
float3 _Vector3_0f8475b3a0df451ea79f6ea2e4e3f673_Out_0_Vector3 = float3(float(0), float(0), float(0));
float3 _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3;
{
// Converting Position from Object to AbsoluteWorld via world space
float3 world;
world = TransformObjectToWorld(_Vector3_0f8475b3a0df451ea79f6ea2e4e3f673_Out_0_Vector3.xyz);
_Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3 = GetAbsolutePositionWS(world);
}
float _Split_fb1b2d84926449e498da6c90f1436311_R_1_Float = _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3[0];
float _Split_fb1b2d84926449e498da6c90f1436311_G_2_Float = _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3[1];
float _Split_fb1b2d84926449e498da6c90f1436311_B_3_Float = _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3[2];
float _Split_fb1b2d84926449e498da6c90f1436311_A_4_Float = 0;
float _Subtract_18ddaa9851a547f1bc3eeafa55f3227b_Out_2_Float;
Unity_Subtract_float(_Split_38420bd2034746a1878048f0ead2a7b2_G_2_Float, _Split_fb1b2d84926449e498da6c90f1436311_G_2_Float, _Subtract_18ddaa9851a547f1bc3eeafa55f3227b_Out_2_Float);
float _Property_1db6873c977b47f186e42eef2cd75a57_Out_0_Float = _Height_Weight;
float _Split_bc5987163dba418d8de90e985de1247b_R_1_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[0];
float _Split_bc5987163dba418d8de90e985de1247b_G_2_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[1];
float _Split_bc5987163dba418d8de90e985de1247b_B_3_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[2];
float _Split_bc5987163dba418d8de90e985de1247b_A_4_Float = 0;
float _Multiply_89de5a5fb3cd451abb1227deba019c38_Out_2_Float;
Unity_Multiply_float_float(_Property_1db6873c977b47f186e42eef2cd75a57_Out_0_Float, _Split_bc5987163dba418d8de90e985de1247b_G_2_Float, _Multiply_89de5a5fb3cd451abb1227deba019c38_Out_2_Float);
float _Divide_2c406de6c63e463f9013e4faf1db1ae8_Out_2_Float;
Unity_Divide_float(_Subtract_18ddaa9851a547f1bc3eeafa55f3227b_Out_2_Float, _Multiply_89de5a5fb3cd451abb1227deba019c38_Out_2_Float, _Divide_2c406de6c63e463f9013e4faf1db1ae8_Out_2_Float);
float _Saturate_e9ea645ca89f482a9d0315df54a77ee5_Out_1_Float;
Unity_Saturate_float(_Divide_2c406de6c63e463f9013e4faf1db1ae8_Out_2_Float, _Saturate_e9ea645ca89f482a9d0315df54a77ee5_Out_1_Float);
float _Property_e96fd1920dff4533951da684129df200_Out_0_Float = _Height_Gradience;
float _Power_b88c5abc85424f17b03c60c98e9bc16c_Out_2_Float;
Unity_Power_float(_Saturate_e9ea645ca89f482a9d0315df54a77ee5_Out_1_Float, _Property_e96fd1920dff4533951da684129df200_Out_0_Float, _Power_b88c5abc85424f17b03c60c98e9bc16c_Out_2_Float);
float _Saturate_6c35d0fa108b483783bde88e06ff5ce0_Out_1_Float;
Unity_Saturate_float(_Power_b88c5abc85424f17b03c60c98e9bc16c_Out_2_Float, _Saturate_6c35d0fa108b483783bde88e06ff5ce0_Out_1_Float);
float3 _Lerp_8461a67d418941ee81ed9eb0f80e8a38_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf_OutVector3_1_Vector3, (_Saturate_6c35d0fa108b483783bde88e06ff5ce0_Out_1_Float.xxx), _Lerp_8461a67d418941ee81ed9eb0f80e8a38_Out_3_Vector3);
#if defined(_USE_WPO)
float3 _UseWPO_81cb92bff33547fbbfaa5ce2f60ba97c_Out_0_Vector3 = _Lerp_8461a67d418941ee81ed9eb0f80e8a38_Out_3_Vector3;
#else
float3 _UseWPO_81cb92bff33547fbbfaa5ce2f60ba97c_Out_0_Vector3 = IN.ObjectSpacePosition;
#endif
description.Position = _UseWPO_81cb92bff33547fbbfaa5ce2f60ba97c_Out_0_Vector3;
description.Normal = IN.ObjectSpaceNormal;
description.Tangent = IN.ObjectSpaceTangent;
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
float3 BaseColor;
float3 Emission;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
UnityTexture2D _Property_0c79649b4042462682b4a7423715f1a1_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Color_Map, sampler_Color_Map, _Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_a9551365617a4a66b75c6a49ab8f9c5f_Out_0_Vector4 = IN.uv0;
float _Property_ac4a3a6f20bd42789825abf989f1a228_Out_0_Float = _Texture_Size;
float4 _Divide_1ff147fe44c540028660935d59272eaa_Out_2_Vector4;
Unity_Divide_float4(_UV_a9551365617a4a66b75c6a49ab8f9c5f_Out_0_Vector4, (_Property_ac4a3a6f20bd42789825abf989f1a228_Out_0_Float.xxxx), _Divide_1ff147fe44c540028660935d59272eaa_Out_2_Vector4);
float4 _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_0c79649b4042462682b4a7423715f1a1_Out_0_Texture2D.tex, _Property_0c79649b4042462682b4a7423715f1a1_Out_0_Texture2D.samplerstate, _Property_0c79649b4042462682b4a7423715f1a1_Out_0_Texture2D.GetTransformedUV((_Divide_1ff147fe44c540028660935d59272eaa_Out_2_Vector4.xy)) );
if (_Property_0c79649b4042462682b4a7423715f1a1_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4, _Property_0c79649b4042462682b4a7423715f1a1_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_R_4_Float = _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4.r;
float _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_G_5_Float = _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4.g;
float _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_B_6_Float = _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4.b;
float _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_A_7_Float = _SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4.a;
float4 _Property_11be52d8078b41908ba72022455e8b98_Out_0_Vector4 = _Color_Tint;
float4 _Multiply_e3ef32e4a4e8442188dfd3c93394960c_Out_2_Vector4;
Unity_Multiply_float4_float4(_SampleTexture2D_fca1d8710acb497cbb28bc6fd5585d85_RGBA_0_Vector4, _Property_11be52d8078b41908ba72022455e8b98_Out_0_Vector4, _Multiply_e3ef32e4a4e8442188dfd3c93394960c_Out_2_Vector4);
UnityTexture2D _Property_415df38a141841e6aa11a7b1e336c379_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Coverage_Color_Map, sampler_Coverage_Color_Map, _Coverage_Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_0805a5601f5b49beb942e50ab3714e03_Out_0_Vector4 = IN.uv0;
float _Property_eb7418710f3f4dc6981c01445b1a809e_Out_0_Float = _Coverage_Size;
float4 _Divide_71dd940938164d5a9e7020e44b1cddf3_Out_2_Vector4;
Unity_Divide_float4(_UV_0805a5601f5b49beb942e50ab3714e03_Out_0_Vector4, (_Property_eb7418710f3f4dc6981c01445b1a809e_Out_0_Float.xxxx), _Divide_71dd940938164d5a9e7020e44b1cddf3_Out_2_Vector4);
float4 _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_415df38a141841e6aa11a7b1e336c379_Out_0_Texture2D.tex, _Property_415df38a141841e6aa11a7b1e336c379_Out_0_Texture2D.samplerstate, _Property_415df38a141841e6aa11a7b1e336c379_Out_0_Texture2D.GetTransformedUV((_Divide_71dd940938164d5a9e7020e44b1cddf3_Out_2_Vector4.xy)) );
if (_Property_415df38a141841e6aa11a7b1e336c379_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4, _Property_415df38a141841e6aa11a7b1e336c379_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_R_4_Float = _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4.r;
float _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_G_5_Float = _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4.g;
float _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_B_6_Float = _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4.b;
float _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_A_7_Float = _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4.a;
UnityTexture2D _Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Mask_Map, sampler_Mask_Map, _Mask_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_c2c5030522744aceb603ff104c9ccde0_Out_0_Vector4 = IN.uv0;
float _Property_a975da4683af46ab85b4a95b01cb9f32_Out_0_Float = _Mask_Map_Size;
float4 _Divide_3dd829d125dc4ec1b32772766d56a17b_Out_2_Vector4;
Unity_Divide_float4(_UV_c2c5030522744aceb603ff104c9ccde0_Out_0_Vector4, (_Property_a975da4683af46ab85b4a95b01cb9f32_Out_0_Float.xxxx), _Divide_3dd829d125dc4ec1b32772766d56a17b_Out_2_Vector4);
float4 _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D.tex, _Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D.samplerstate, _Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D.GetTransformedUV((_Divide_3dd829d125dc4ec1b32772766d56a17b_Out_2_Vector4.xy)) );
if (_Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4, _Property_3c475bf2105348d2b322ec33ddcf8e95_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_R_4_Float = _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4.r;
float _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_G_5_Float = _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4.g;
float _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_B_6_Float = _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4.b;
float _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_A_7_Float = _SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_RGBA_0_Vector4.a;
float _Multiply_81f5f8bc82304adbae345a5fe4b0024b_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_125bf01f36ad46b6b871896b541dc7ce_R_4_Float, 0.1, _Multiply_81f5f8bc82304adbae345a5fe4b0024b_Out_2_Float);
float _Add_33507c032b9e48bfb61763d507bb031c_Out_2_Float;
Unity_Add_float(_Multiply_81f5f8bc82304adbae345a5fe4b0024b_Out_2_Float, float(0.9), _Add_33507c032b9e48bfb61763d507bb031c_Out_2_Float);
float _Property_883f8e1288fd43f1a3674ca1f5364253_Out_0_Float = _Mask_Map_Intensity;
float _Power_b45fec9ffb074e40b5cb5452c0fb982d_Out_2_Float;
Unity_Power_float(_Add_33507c032b9e48bfb61763d507bb031c_Out_2_Float, _Property_883f8e1288fd43f1a3674ca1f5364253_Out_0_Float, _Power_b45fec9ffb074e40b5cb5452c0fb982d_Out_2_Float);
float _Split_b6a9cc69e69c417ba235626d7beed2eb_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_b6a9cc69e69c417ba235626d7beed2eb_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_b6a9cc69e69c417ba235626d7beed2eb_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_b6a9cc69e69c417ba235626d7beed2eb_A_4_Float = 0;
float _Property_73ac42b2cd3d4b43976ac13e73b6de40_Out_0_Float = _Height_from_Ground;
float3 _Transform_425aee5b4b5e409eb94b431642b515fb_Out_1_Vector3;
_Transform_425aee5b4b5e409eb94b431642b515fb_Out_1_Vector3 = TransformObjectToWorld(float3 (0, 0, 0).xyz);
float3 _Normalize_960b1f24c996479a8c1a9a81cabcb835_Out_1_Vector3;
Unity_Normalize_float3(_Transform_425aee5b4b5e409eb94b431642b515fb_Out_1_Vector3, _Normalize_960b1f24c996479a8c1a9a81cabcb835_Out_1_Vector3);
float _Property_11f1157c9f364521bf765c261e8bb93e_Out_0_Float = _Height_from_Ground_Randomness;
float _Multiply_b7a3ed973f104b9a84af846ce299d95f_Out_2_Float;
Unity_Multiply_float_float(_Property_11f1157c9f364521bf765c261e8bb93e_Out_0_Float, -1, _Multiply_b7a3ed973f104b9a84af846ce299d95f_Out_2_Float);
float _RandomRange_82fa55b7f4da4f6ebc597a38d2ca2e6d_Out_3_Float;
Unity_RandomRange_float((_Normalize_960b1f24c996479a8c1a9a81cabcb835_Out_1_Vector3.xy), _Multiply_b7a3ed973f104b9a84af846ce299d95f_Out_2_Float, _Property_11f1157c9f364521bf765c261e8bb93e_Out_0_Float, _RandomRange_82fa55b7f4da4f6ebc597a38d2ca2e6d_Out_3_Float);
float _Add_109ea06d643c4826b0138a85becc2fa9_Out_2_Float;
Unity_Add_float(_Property_73ac42b2cd3d4b43976ac13e73b6de40_Out_0_Float, _RandomRange_82fa55b7f4da4f6ebc597a38d2ca2e6d_Out_3_Float, _Add_109ea06d643c4826b0138a85becc2fa9_Out_2_Float);
float _Subtract_fe60462bdbdd4a22a207774a6a8cb1b8_Out_2_Float;
Unity_Subtract_float(_Split_b6a9cc69e69c417ba235626d7beed2eb_G_2_Float, _Add_109ea06d643c4826b0138a85becc2fa9_Out_2_Float, _Subtract_fe60462bdbdd4a22a207774a6a8cb1b8_Out_2_Float);
float _Property_1487773737e44e78a7c514c7f470e603_Out_0_Float = _Height_from_Ground_Softness;
float _Divide_6eb043a8aee54d87a0c73169597bdcba_Out_2_Float;
Unity_Divide_float(_Subtract_fe60462bdbdd4a22a207774a6a8cb1b8_Out_2_Float, _Property_1487773737e44e78a7c514c7f470e603_Out_0_Float, _Divide_6eb043a8aee54d87a0c73169597bdcba_Out_2_Float);
float _Saturate_d8ec125670414d1888aeec654fcb04e2_Out_1_Float;
Unity_Saturate_float(_Divide_6eb043a8aee54d87a0c73169597bdcba_Out_2_Float, _Saturate_d8ec125670414d1888aeec654fcb04e2_Out_1_Float);
float _OneMinus_de85154fa1b742e3b6c9e0f92b85d96c_Out_1_Float;
Unity_OneMinus_float(_Saturate_d8ec125670414d1888aeec654fcb04e2_Out_1_Float, _OneMinus_de85154fa1b742e3b6c9e0f92b85d96c_Out_1_Float);
float _DotProduct_a5624fdd7fd94a4c82ad8c039922e638_Out_2_Float;
Unity_DotProduct_float3(IN.WorldSpaceNormal, float3(0, 1, 0), _DotProduct_a5624fdd7fd94a4c82ad8c039922e638_Out_2_Float);
float _Property_1d95c04e88f443bc91fe7ed217b674b8_Out_0_Float = _Normal_Pow;
float _Power_5b713b10f3e049f8a97be81969b52b7d_Out_2_Float;
Unity_Power_float(_DotProduct_a5624fdd7fd94a4c82ad8c039922e638_Out_2_Float, _Property_1d95c04e88f443bc91fe7ed217b674b8_Out_0_Float, _Power_5b713b10f3e049f8a97be81969b52b7d_Out_2_Float);
float _Property_6314206eb47742698a7d2833f119f39e_Out_0_Float = _Normal_Mul;
float _Multiply_6e6bcfc2a4df414e86d241952840e972_Out_2_Float;
Unity_Multiply_float_float(_Power_5b713b10f3e049f8a97be81969b52b7d_Out_2_Float, _Property_6314206eb47742698a7d2833f119f39e_Out_0_Float, _Multiply_6e6bcfc2a4df414e86d241952840e972_Out_2_Float);
float _Saturate_77d41005224e417eb678b78b9a10771a_Out_1_Float;
Unity_Saturate_float(_Multiply_6e6bcfc2a4df414e86d241952840e972_Out_2_Float, _Saturate_77d41005224e417eb678b78b9a10771a_Out_1_Float);
float _Add_8395f84ea8e546988dd02e5b922d32ee_Out_2_Float;
Unity_Add_float(_OneMinus_de85154fa1b742e3b6c9e0f92b85d96c_Out_1_Float, _Saturate_77d41005224e417eb678b78b9a10771a_Out_1_Float, _Add_8395f84ea8e546988dd02e5b922d32ee_Out_2_Float);
float _Multiply_b61ead58dd024fd7aacad7c0b68f3be7_Out_2_Float;
Unity_Multiply_float_float(5, _Add_8395f84ea8e546988dd02e5b922d32ee_Out_2_Float, _Multiply_b61ead58dd024fd7aacad7c0b68f3be7_Out_2_Float);
float _Multiply_b0c3a49303544ccaa1d15379e6b1e9d2_Out_2_Float;
Unity_Multiply_float_float(_Power_b45fec9ffb074e40b5cb5452c0fb982d_Out_2_Float, _Multiply_b61ead58dd024fd7aacad7c0b68f3be7_Out_2_Float, _Multiply_b0c3a49303544ccaa1d15379e6b1e9d2_Out_2_Float);
float _Step_9e404f0c811346c88c531f42e623243f_Out_2_Float;
Unity_Step_float(_Subtract_fe60462bdbdd4a22a207774a6a8cb1b8_Out_2_Float, float(-0.1), _Step_9e404f0c811346c88c531f42e623243f_Out_2_Float);
float _Add_0328340190574386b2d37a0d035b66fb_Out_2_Float;
Unity_Add_float(_Multiply_b0c3a49303544ccaa1d15379e6b1e9d2_Out_2_Float, _Step_9e404f0c811346c88c531f42e623243f_Out_2_Float, _Add_0328340190574386b2d37a0d035b66fb_Out_2_Float);
float _Power_3bfc30934311465db3ba28e64b49097d_Out_2_Float;
Unity_Power_float(_Add_0328340190574386b2d37a0d035b66fb_Out_2_Float, float(3), _Power_3bfc30934311465db3ba28e64b49097d_Out_2_Float);
float _Saturate_3ba4a70af5214b4086803937a3f8b09f_Out_1_Float;
Unity_Saturate_float(_Power_3bfc30934311465db3ba28e64b49097d_Out_2_Float, _Saturate_3ba4a70af5214b4086803937a3f8b09f_Out_1_Float);
float _Saturate_6e423c9f5f1249088750a2b84a42b764_Out_1_Float;
Unity_Saturate_float(_Add_8395f84ea8e546988dd02e5b922d32ee_Out_2_Float, _Saturate_6e423c9f5f1249088750a2b84a42b764_Out_1_Float);
#if defined(_USE_COVERAGE_MASK_TEXTURE)
float _UseCoverageMaskTexture_f75c00eee68746bd80e2fa6c2bc28d4c_Out_0_Float = _Saturate_3ba4a70af5214b4086803937a3f8b09f_Out_1_Float;
#else
float _UseCoverageMaskTexture_f75c00eee68746bd80e2fa6c2bc28d4c_Out_0_Float = _Saturate_6e423c9f5f1249088750a2b84a42b764_Out_1_Float;
#endif
float4 _Lerp_c6c7a7472707418491f2fa10989f05b9_Out_3_Vector4;
Unity_Lerp_float4(_Multiply_e3ef32e4a4e8442188dfd3c93394960c_Out_2_Vector4, _SampleTexture2D_2335cf1d3d824b70bcd18c1f24d84c25_RGBA_0_Vector4, (_UseCoverageMaskTexture_f75c00eee68746bd80e2fa6c2bc28d4c_Out_0_Float.xxxx), _Lerp_c6c7a7472707418491f2fa10989f05b9_Out_3_Vector4);
#if defined(_USE_COVERAGE)
float4 _UseCoverage_c2f2c567080e4d5f9b652fbcbf667c54_Out_0_Vector4 = _Lerp_c6c7a7472707418491f2fa10989f05b9_Out_3_Vector4;
#else
float4 _UseCoverage_c2f2c567080e4d5f9b652fbcbf667c54_Out_0_Vector4 = _Multiply_e3ef32e4a4e8442188dfd3c93394960c_Out_2_Vector4;
#endif
UnityTexture2D _Property_c7dc38eaeea545aaada0e67cc32d9d25_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Emissive_Map, sampler_Emissive_Map, _Emissive_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_c7dc38eaeea545aaada0e67cc32d9d25_Out_0_Texture2D.tex, _Property_c7dc38eaeea545aaada0e67cc32d9d25_Out_0_Texture2D.samplerstate, _Property_c7dc38eaeea545aaada0e67cc32d9d25_Out_0_Texture2D.GetTransformedUV((_Divide_1ff147fe44c540028660935d59272eaa_Out_2_Vector4.xy)) );
if (_Property_c7dc38eaeea545aaada0e67cc32d9d25_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4, _Property_c7dc38eaeea545aaada0e67cc32d9d25_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_R_4_Float = _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4.r;
float _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_G_5_Float = _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4.g;
float _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_B_6_Float = _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4.b;
float _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_A_7_Float = _SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4.a;
float4 _Property_857cbfa6469047f8904e0adc3eefe6b8_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_Emissive_Color) : _Emissive_Color;
float4 _Multiply_293fcf7127ab419cbcdff4183cccdba0_Out_2_Vector4;
Unity_Multiply_float4_float4(_SampleTexture2D_bd277ee564ff46dd9d6c0d7ea700fb9d_RGBA_0_Vector4, _Property_857cbfa6469047f8904e0adc3eefe6b8_Out_0_Vector4, _Multiply_293fcf7127ab419cbcdff4183cccdba0_Out_2_Vector4);
#if defined(_USE_EMISSIVE)
float4 _UseEmissive_fdf874f7398e43a7838e4f04fd255375_Out_0_Vector4 = _Multiply_293fcf7127ab419cbcdff4183cccdba0_Out_2_Vector4;
#else
float4 _UseEmissive_fdf874f7398e43a7838e4f04fd255375_Out_0_Vector4 = float4(0, 0, 0, 0);
#endif
surface.BaseColor = (_UseCoverage_c2f2c567080e4d5f9b652fbcbf667c54_Out_0_Vector4.xyz);
surface.Emission = (_UseEmissive_fdf874f7398e43a7838e4f04fd255375_Out_0_Vector4.xyz);
float3 reactionUnusedColor = 0, reactionUnusedEmission = 0;
ClipVegetationFuel(TransformObjectToWorld(float3(0,0,0)));
ApplyVegetationReaction(TransformObjectToWorld(float3(0,0,0)),surface.BaseColor,surface.Emission);
return surface;
}

// --------------------------------------------------
// Build Graph Inputs

VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

    output.ObjectSpaceNormal =                          input.normalOS;
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
    output.AbsoluteWorldSpacePosition =                 GetAbsolutePositionWS(TransformObjectToWorld(input.positionOS));
    output.TimeParameters =                             _TimeParameters.xyz;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

    

    // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
    float3 unnormalizedNormalWS = input.normalWS;
    const float renormFactor = 1.0 / length(unnormalizedNormalWS);


    output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph


    output.ObjectSpacePosition = TransformWorldToObject(input.positionWS);

    #if UNITY_UV_STARTS_AT_TOP
    #else
    #endif


    output.uv0 = input.texCoord0;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

void BuildAppDataFull(Attributes attributes, VertexDescription vertexDescription, inout appdata_full result)
{
    result.vertex     = float4(attributes.positionOS, 1);
    result.tangent    = attributes.tangentOS;
    result.normal     = attributes.normalOS;
    result.texcoord   = attributes.uv0;
    result.texcoord1  = attributes.uv1;
    result.texcoord2  = attributes.uv2;
    result.vertex     = float4(vertexDescription.Position, 1);
    result.normal     = vertexDescription.Normal;
    result.tangent    = float4(vertexDescription.Tangent, 0);
    #if UNITY_ANY_INSTANCING_ENABLED
    #endif
}

void VaryingsToSurfaceVertex(Varyings varyings, inout v2f_surf result)
{
    result.pos = varyings.positionCS;
    result.worldPos = varyings.positionWS;
    result.worldNormal = varyings.normalWS;
    // World Tangent isn't an available input on v2f_surf


    #if UNITY_ANY_INSTANCING_ENABLED
    #endif
    #if UNITY_SHOULD_SAMPLE_SH
    #if !defined(LIGHTMAP_ON)
    #endif
    #endif
    #if defined(LIGHTMAP_ON)
    #endif
    #ifdef VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
        result.fogCoord = varyings.fogFactorAndVertexLight.x;
        COPY_TO_LIGHT_COORDS(result, varyings.fogFactorAndVertexLight.yzw);
    #endif

    DEFAULT_UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(varyings, result);
}

void SurfaceVertexToVaryings(v2f_surf surfVertex, inout Varyings result)
{
    result.positionCS = surfVertex.pos;
    result.positionWS = surfVertex.worldPos;
    result.normalWS = surfVertex.worldNormal;
    // viewDirectionWS is never filled out in the legacy pass' function. Always use the value computed by SRP
    // World Tangent isn't an available input on v2f_surf

    #if UNITY_ANY_INSTANCING_ENABLED
    #endif
    #if UNITY_SHOULD_SAMPLE_SH
    #if !defined(LIGHTMAP_ON)
    #endif
    #endif
    #if defined(LIGHTMAP_ON)
    #endif
    #ifdef VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
        result.fogFactorAndVertexLight.x = surfVertex.fogCoord;
        COPY_FROM_LIGHT_COORDS(result.fogFactorAndVertexLight.yzw, surfVertex);
    #endif

    DEFAULT_UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(surfVertex, result);
}

// --------------------------------------------------
// Main

#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/Varyings.hlsl"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/LightingMetaPass.hlsl"

ENDHLSL
}
Pass
{
    Name "SceneSelectionPass"
    Tags
    {
        "LightMode" = "SceneSelectionPass"
    }

// Render State
Cull Off

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 3.0
#pragma multi_compile_instancing
#pragma vertex vert
#pragma fragment frag

// Keywords
// PassKeywords: <None>
#pragma shader_feature_local _ _USE_EMISSIVE
#pragma shader_feature_local _ _USE_WPO
#pragma shader_feature_local _ _USE_ORM
#pragma shader_feature_local _ _USE_NORMAL_MAPS
#pragma shader_feature_local _ _USE_COVERAGE
#pragma shader_feature_local _ _USE_COVERAGE_MASK_TEXTURE

#if defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_2
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_3
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_4
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_5
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_6
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM)
#define KEYWORD_PERMUTATION_7
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_8
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_9
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_10
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_11
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_12
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_13
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_14
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO)
#define KEYWORD_PERMUTATION_15
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_16
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_17
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_18
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_19
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_20
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_21
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_22
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM)
#define KEYWORD_PERMUTATION_23
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_24
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_25
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_26
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_27
#elif defined(_USE_EMISSIVE) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_28
#elif defined(_USE_EMISSIVE) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_29
#elif defined(_USE_EMISSIVE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_30
#elif defined(_USE_EMISSIVE)
#define KEYWORD_PERMUTATION_31
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_32
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_33
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_34
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_35
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_36
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_37
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_38
#elif defined(_USE_WPO) && defined(_USE_ORM)
#define KEYWORD_PERMUTATION_39
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_40
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_41
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_42
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_43
#elif defined(_USE_WPO) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_44
#elif defined(_USE_WPO) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_45
#elif defined(_USE_WPO) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_46
#elif defined(_USE_WPO)
#define KEYWORD_PERMUTATION_47
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_48
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_49
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_50
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_51
#elif defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_52
#elif defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_53
#elif defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_54
#elif defined(_USE_ORM)
#define KEYWORD_PERMUTATION_55
#elif defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_56
#elif defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_57
#elif defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_58
#elif defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_59
#elif defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_60
#elif defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_61
#elif defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_62
#else
#define KEYWORD_PERMUTATION_63
#endif


// Defines
#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SceneSelectionPass
#define BUILTIN_TARGET_API 1
#define SCENESELECTIONPASS 1
#ifdef _BUILTIN_SURFACE_TYPE_TRANSPARENT
#define _SURFACE_TYPE_TRANSPARENT _BUILTIN_SURFACE_TYPE_TRANSPARENT
#endif
#ifdef _BUILTIN_ALPHATEST_ON
#define _ALPHATEST_ON _BUILTIN_ALPHATEST_ON
#endif
#ifdef _BUILTIN_AlphaClip
#define _AlphaClip _BUILTIN_AlphaClip
#endif
#ifdef _BUILTIN_ALPHAPREMULTIPLY_ON
#define _ALPHAPREMULTIPLY_ON _BUILTIN_ALPHAPREMULTIPLY_ON
#endif


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Shim/Shims.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/LegacySurfaceVertex.hlsl"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/ShaderGraphFunctions.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
 float3 positionOS : POSITION;
 float3 normalOS : NORMAL;
 float4 tangentOS : TANGENT;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
 float4 positionCS : SV_POSITION;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};
struct SurfaceDescriptionInputs
{
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 AbsoluteWorldSpacePosition;
 float3 TimeParameters;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};

PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.positionCS = input.positionCS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.positionCS = input.positionCS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}


// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)

float _EnvironmentReactionSupported;
float4 _EnvState0, _EnvState1;
float _FuelCoverage, _AllowFuelCoverage;
float4 _WetTint, _CharTint, _FireColor;
float4 _EnvironmentCellBounds;
float _UseEnvironmentCells;

float _Height_from_Ground_Randomness;
float _Height_from_Ground_Softness;
float _Normal_Pow;
float _Normal_Mul;
float _Mask_Map_Size;
float4 _Coverage_Normal_Map_TexelSize;
float _Mask_Map_Intensity;
float4 _Coverage_ORM_Map_TexelSize;
float _Height_Weight;
float _Height_Gradience;
float _Wind_Intensity;
float3 _Wind_Direction;
float _Wind_Speed;
float _Wind_Time_Random_Offset_Amount;
float _Texture_Size;
float4 _Color_Map_TexelSize;
float4 _Color_Tint;
float4 _Normal_Map_TexelSize;
float _Normal_Intensity;
float4 _ORM_Map_TexelSize;
float _Roughness_Intensity;
float _AO_Intensity;
float _Metallic_Intensity;
float4 _Emissive_Map_TexelSize;
float4 _Emissive_Color;
float4 _Coverage_Color_Map_TexelSize;
float _Coverage_Size;
float _Height_from_Ground;
float4 _Mask_Map_TexelSize;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Coverage_Normal_Map);
SAMPLER(sampler_Coverage_Normal_Map);
TEXTURE2D(_Coverage_ORM_Map);
SAMPLER(sampler_Coverage_ORM_Map);
TEXTURE2D(_Color_Map);
SAMPLER(sampler_Color_Map);
TEXTURE2D(_Normal_Map);
SAMPLER(sampler_Normal_Map);
TEXTURE2D(_ORM_Map);
SAMPLER(sampler_ORM_Map);
TEXTURE2D(_Emissive_Map);
SAMPLER(sampler_Emissive_Map);
TEXTURE2D(_Coverage_Color_Map);
SAMPLER(sampler_Coverage_Color_Map);
TEXTURE2D(_Mask_Map);
SAMPLER(sampler_Mask_Map);

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Includes
// GraphIncludes: <None>

// Graph Functions

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Sine_float(float In, out float Out)
{
    Out = sin(In);
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Length_float3(float3 In, out float Out)
{
    Out = length(In);
}

void Unity_Divide_float(float A, float B, out float Out)
{
    Out = A / B;
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
{
    Out = lerp(A, B, T);
}

struct Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float
{
float3 ObjectSpacePosition;
float3 TimeParameters;
};

void SG_WindSway_51c4027ae50a8cd49be53c4990b9f179_float(float _Sway_Wind_Speed, float3 _Sway_Wind_Direction, float _Sway_Wind_Intensity, float _Sway_Wind_Time_Random_Offset_Amount, Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float IN, out float3 OutVector3_1)
{
float _Property_5c4cd19861f64014ae8e16af9365ef49_Out_0_Float = _Sway_Wind_Time_Random_Offset_Amount;
float _Add_4b31033339dc4f8e8e71674d98e662a3_Out_2_Float;
Unity_Add_float(IN.TimeParameters.x, _Property_5c4cd19861f64014ae8e16af9365ef49_Out_0_Float, _Add_4b31033339dc4f8e8e71674d98e662a3_Out_2_Float);
float _Property_9a93fa589ac7409884a94247349a940f_Out_0_Float = _Sway_Wind_Speed;
float _Multiply_7418655003ae4fedb2bf68678629472c_Out_2_Float;
Unity_Multiply_float_float(_Add_4b31033339dc4f8e8e71674d98e662a3_Out_2_Float, _Property_9a93fa589ac7409884a94247349a940f_Out_0_Float, _Multiply_7418655003ae4fedb2bf68678629472c_Out_2_Float);
float _Sine_ebd14b0fc42b4b4cb4387f3a1b879c1f_Out_1_Float;
Unity_Sine_float(_Multiply_7418655003ae4fedb2bf68678629472c_Out_2_Float, _Sine_ebd14b0fc42b4b4cb4387f3a1b879c1f_Out_1_Float);
float _Add_660e92170c564f94a79277cd2fd0cf20_Out_2_Float;
Unity_Add_float(_Sine_ebd14b0fc42b4b4cb4387f3a1b879c1f_Out_1_Float, float(1), _Add_660e92170c564f94a79277cd2fd0cf20_Out_2_Float);
float _Multiply_b8fd654b771447aebafc1a6aad6957cf_Out_2_Float;
Unity_Multiply_float_float(_Add_660e92170c564f94a79277cd2fd0cf20_Out_2_Float, 0.5, _Multiply_b8fd654b771447aebafc1a6aad6957cf_Out_2_Float);
float3 _Property_fa2ee033b44946c59c3fa9aea4a167c8_Out_0_Vector3 = _Sway_Wind_Direction;
float3 _Multiply_a5500b7f7b8e43988a055eea7eb917f3_Out_2_Vector3;
Unity_Multiply_float3_float3((_Multiply_b8fd654b771447aebafc1a6aad6957cf_Out_2_Float.xxx), _Property_fa2ee033b44946c59c3fa9aea4a167c8_Out_0_Vector3, _Multiply_a5500b7f7b8e43988a055eea7eb917f3_Out_2_Vector3);
float _Property_66cba0a3223546238e18b58e017e756f_Out_0_Float = _Sway_Wind_Intensity;
float3 _Multiply_475f81423e39483cba045bef08b9f967_Out_2_Vector3;
Unity_Multiply_float3_float3(_Multiply_a5500b7f7b8e43988a055eea7eb917f3_Out_2_Vector3, (_Property_66cba0a3223546238e18b58e017e756f_Out_0_Float.xxx), _Multiply_475f81423e39483cba045bef08b9f967_Out_2_Vector3);
float3 _Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3;
Unity_Add_float3(_Multiply_475f81423e39483cba045bef08b9f967_Out_2_Vector3, IN.ObjectSpacePosition, _Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3);
float _Length_53965ffb38de43359ab019a247194557_Out_1_Float;
Unity_Length_float3(IN.ObjectSpacePosition, _Length_53965ffb38de43359ab019a247194557_Out_1_Float);
float _Length_77b6b3d742104f2abf3c6968dea9a342_Out_1_Float;
Unity_Length_float3(_Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3, _Length_77b6b3d742104f2abf3c6968dea9a342_Out_1_Float);
float _Divide_f5dd43de79e343638d2743ee93ce7326_Out_2_Float;
Unity_Divide_float(_Length_53965ffb38de43359ab019a247194557_Out_1_Float, _Length_77b6b3d742104f2abf3c6968dea9a342_Out_1_Float, _Divide_f5dd43de79e343638d2743ee93ce7326_Out_2_Float);
float3 _Lerp_7574daf550e1428cb9d35656c7bb9603_Out_3_Vector3;
Unity_Lerp_float3(float3(0, 0, 0), _Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3, (_Divide_f5dd43de79e343638d2743ee93ce7326_Out_2_Float.xxx), _Lerp_7574daf550e1428cb9d35656c7bb9603_Out_3_Vector3);
OutVector3_1 = _Lerp_7574daf550e1428cb9d35656c7bb9603_Out_3_Vector3;
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

void Unity_Power_float(float A, float B, out float Out)
{
    Out = pow(A, B);
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 Position;
float3 Normal;
float3 Tangent;
};

VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
float _Property_b2e048f6276d4371b6ad931f5d1baa87_Out_0_Float = _Wind_Speed;
float3 _Property_7133a0551bc7429cb8bbfcbad044bf97_Out_0_Vector3 = _Wind_Direction;
float _Property_5aa2352867cd4da79dc9f9f9979925c7_Out_0_Float = _Wind_Intensity;
float _Property_e0c6ebff99e349d6966115347961a70c_Out_0_Float = _Wind_Time_Random_Offset_Amount;
Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf;
_WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf.ObjectSpacePosition = IN.ObjectSpacePosition;
_WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf.TimeParameters = IN.TimeParameters;
float3 _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf_OutVector3_1_Vector3;
SG_WindSway_51c4027ae50a8cd49be53c4990b9f179_float(_Property_b2e048f6276d4371b6ad931f5d1baa87_Out_0_Float, _Property_7133a0551bc7429cb8bbfcbad044bf97_Out_0_Vector3, _Property_5aa2352867cd4da79dc9f9f9979925c7_Out_0_Float, _Property_e0c6ebff99e349d6966115347961a70c_Out_0_Float, _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf, _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf_OutVector3_1_Vector3);
float _Split_38420bd2034746a1878048f0ead2a7b2_R_1_Float = IN.AbsoluteWorldSpacePosition[0];
float _Split_38420bd2034746a1878048f0ead2a7b2_G_2_Float = IN.AbsoluteWorldSpacePosition[1];
float _Split_38420bd2034746a1878048f0ead2a7b2_B_3_Float = IN.AbsoluteWorldSpacePosition[2];
float _Split_38420bd2034746a1878048f0ead2a7b2_A_4_Float = 0;
float3 _Vector3_0f8475b3a0df451ea79f6ea2e4e3f673_Out_0_Vector3 = float3(float(0), float(0), float(0));
float3 _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3;
{
// Converting Position from Object to AbsoluteWorld via world space
float3 world;
world = TransformObjectToWorld(_Vector3_0f8475b3a0df451ea79f6ea2e4e3f673_Out_0_Vector3.xyz);
_Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3 = GetAbsolutePositionWS(world);
}
float _Split_fb1b2d84926449e498da6c90f1436311_R_1_Float = _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3[0];
float _Split_fb1b2d84926449e498da6c90f1436311_G_2_Float = _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3[1];
float _Split_fb1b2d84926449e498da6c90f1436311_B_3_Float = _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3[2];
float _Split_fb1b2d84926449e498da6c90f1436311_A_4_Float = 0;
float _Subtract_18ddaa9851a547f1bc3eeafa55f3227b_Out_2_Float;
Unity_Subtract_float(_Split_38420bd2034746a1878048f0ead2a7b2_G_2_Float, _Split_fb1b2d84926449e498da6c90f1436311_G_2_Float, _Subtract_18ddaa9851a547f1bc3eeafa55f3227b_Out_2_Float);
float _Property_1db6873c977b47f186e42eef2cd75a57_Out_0_Float = _Height_Weight;
float _Split_bc5987163dba418d8de90e985de1247b_R_1_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[0];
float _Split_bc5987163dba418d8de90e985de1247b_G_2_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[1];
float _Split_bc5987163dba418d8de90e985de1247b_B_3_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[2];
float _Split_bc5987163dba418d8de90e985de1247b_A_4_Float = 0;
float _Multiply_89de5a5fb3cd451abb1227deba019c38_Out_2_Float;
Unity_Multiply_float_float(_Property_1db6873c977b47f186e42eef2cd75a57_Out_0_Float, _Split_bc5987163dba418d8de90e985de1247b_G_2_Float, _Multiply_89de5a5fb3cd451abb1227deba019c38_Out_2_Float);
float _Divide_2c406de6c63e463f9013e4faf1db1ae8_Out_2_Float;
Unity_Divide_float(_Subtract_18ddaa9851a547f1bc3eeafa55f3227b_Out_2_Float, _Multiply_89de5a5fb3cd451abb1227deba019c38_Out_2_Float, _Divide_2c406de6c63e463f9013e4faf1db1ae8_Out_2_Float);
float _Saturate_e9ea645ca89f482a9d0315df54a77ee5_Out_1_Float;
Unity_Saturate_float(_Divide_2c406de6c63e463f9013e4faf1db1ae8_Out_2_Float, _Saturate_e9ea645ca89f482a9d0315df54a77ee5_Out_1_Float);
float _Property_e96fd1920dff4533951da684129df200_Out_0_Float = _Height_Gradience;
float _Power_b88c5abc85424f17b03c60c98e9bc16c_Out_2_Float;
Unity_Power_float(_Saturate_e9ea645ca89f482a9d0315df54a77ee5_Out_1_Float, _Property_e96fd1920dff4533951da684129df200_Out_0_Float, _Power_b88c5abc85424f17b03c60c98e9bc16c_Out_2_Float);
float _Saturate_6c35d0fa108b483783bde88e06ff5ce0_Out_1_Float;
Unity_Saturate_float(_Power_b88c5abc85424f17b03c60c98e9bc16c_Out_2_Float, _Saturate_6c35d0fa108b483783bde88e06ff5ce0_Out_1_Float);
float3 _Lerp_8461a67d418941ee81ed9eb0f80e8a38_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf_OutVector3_1_Vector3, (_Saturate_6c35d0fa108b483783bde88e06ff5ce0_Out_1_Float.xxx), _Lerp_8461a67d418941ee81ed9eb0f80e8a38_Out_3_Vector3);
#if defined(_USE_WPO)
float3 _UseWPO_81cb92bff33547fbbfaa5ce2f60ba97c_Out_0_Vector3 = _Lerp_8461a67d418941ee81ed9eb0f80e8a38_Out_3_Vector3;
#else
float3 _UseWPO_81cb92bff33547fbbfaa5ce2f60ba97c_Out_0_Vector3 = IN.ObjectSpacePosition;
#endif
description.Position = _UseWPO_81cb92bff33547fbbfaa5ce2f60ba97c_Out_0_Vector3;
description.Normal = IN.ObjectSpaceNormal;
description.Tangent = IN.ObjectSpaceTangent;
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
float3 reactionUnusedColor = 0, reactionUnusedEmission = 0;
ClipVegetationFuel(TransformObjectToWorld(float3(0,0,0)));
return surface;
}

// --------------------------------------------------
// Build Graph Inputs

VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

    output.ObjectSpaceNormal =                          input.normalOS;
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
    output.AbsoluteWorldSpacePosition =                 GetAbsolutePositionWS(TransformObjectToWorld(input.positionOS));
    output.TimeParameters =                             _TimeParameters.xyz;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

    






    #if UNITY_UV_STARTS_AT_TOP
    #else
    #endif


#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

void BuildAppDataFull(Attributes attributes, VertexDescription vertexDescription, inout appdata_full result)
{
    result.vertex     = float4(attributes.positionOS, 1);
    result.tangent    = attributes.tangentOS;
    result.normal     = attributes.normalOS;
    result.vertex     = float4(vertexDescription.Position, 1);
    result.normal     = vertexDescription.Normal;
    result.tangent    = float4(vertexDescription.Tangent, 0);
    #if UNITY_ANY_INSTANCING_ENABLED
    #endif
}

void VaryingsToSurfaceVertex(Varyings varyings, inout v2f_surf result)
{
    result.pos = varyings.positionCS;
    // World Tangent isn't an available input on v2f_surf


    #if UNITY_ANY_INSTANCING_ENABLED
    #endif
    #if UNITY_SHOULD_SAMPLE_SH
    #if !defined(LIGHTMAP_ON)
    #endif
    #endif
    #if defined(LIGHTMAP_ON)
    #endif
    #ifdef VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
        result.fogCoord = varyings.fogFactorAndVertexLight.x;
        COPY_TO_LIGHT_COORDS(result, varyings.fogFactorAndVertexLight.yzw);
    #endif

    DEFAULT_UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(varyings, result);
}

void SurfaceVertexToVaryings(v2f_surf surfVertex, inout Varyings result)
{
    result.positionCS = surfVertex.pos;
    // viewDirectionWS is never filled out in the legacy pass' function. Always use the value computed by SRP
    // World Tangent isn't an available input on v2f_surf

    #if UNITY_ANY_INSTANCING_ENABLED
    #endif
    #if UNITY_SHOULD_SAMPLE_SH
    #if !defined(LIGHTMAP_ON)
    #endif
    #endif
    #if defined(LIGHTMAP_ON)
    #endif
    #ifdef VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
        result.fogFactorAndVertexLight.x = surfVertex.fogCoord;
        COPY_FROM_LIGHT_COORDS(result.fogFactorAndVertexLight.yzw, surfVertex);
    #endif

    DEFAULT_UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(surfVertex, result);
}

// --------------------------------------------------
// Main

#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/Varyings.hlsl"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/DepthOnlyPass.hlsl"

ENDHLSL
}
Pass
{
    Name "ScenePickingPass"
    Tags
    {
        "LightMode" = "Picking"
    }

// Render State
Cull Back

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 3.0
#pragma multi_compile_instancing
#pragma vertex vert
#pragma fragment frag

// Keywords
// PassKeywords: <None>
#pragma shader_feature_local _ _USE_EMISSIVE
#pragma shader_feature_local _ _USE_WPO
#pragma shader_feature_local _ _USE_ORM
#pragma shader_feature_local _ _USE_NORMAL_MAPS
#pragma shader_feature_local _ _USE_COVERAGE
#pragma shader_feature_local _ _USE_COVERAGE_MASK_TEXTURE

#if defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_2
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_3
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_4
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_5
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_6
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_ORM)
#define KEYWORD_PERMUTATION_7
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_8
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_9
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_10
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_11
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_12
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_13
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_14
#elif defined(_USE_EMISSIVE) && defined(_USE_WPO)
#define KEYWORD_PERMUTATION_15
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_16
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_17
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_18
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_19
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_20
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_21
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_22
#elif defined(_USE_EMISSIVE) && defined(_USE_ORM)
#define KEYWORD_PERMUTATION_23
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_24
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_25
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_26
#elif defined(_USE_EMISSIVE) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_27
#elif defined(_USE_EMISSIVE) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_28
#elif defined(_USE_EMISSIVE) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_29
#elif defined(_USE_EMISSIVE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_30
#elif defined(_USE_EMISSIVE)
#define KEYWORD_PERMUTATION_31
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_32
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_33
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_34
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_35
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_36
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_37
#elif defined(_USE_WPO) && defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_38
#elif defined(_USE_WPO) && defined(_USE_ORM)
#define KEYWORD_PERMUTATION_39
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_40
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_41
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_42
#elif defined(_USE_WPO) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_43
#elif defined(_USE_WPO) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_44
#elif defined(_USE_WPO) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_45
#elif defined(_USE_WPO) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_46
#elif defined(_USE_WPO)
#define KEYWORD_PERMUTATION_47
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_48
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_49
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_50
#elif defined(_USE_ORM) && defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_51
#elif defined(_USE_ORM) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_52
#elif defined(_USE_ORM) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_53
#elif defined(_USE_ORM) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_54
#elif defined(_USE_ORM)
#define KEYWORD_PERMUTATION_55
#elif defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_56
#elif defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_57
#elif defined(_USE_NORMAL_MAPS) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_58
#elif defined(_USE_NORMAL_MAPS)
#define KEYWORD_PERMUTATION_59
#elif defined(_USE_COVERAGE) && defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_60
#elif defined(_USE_COVERAGE)
#define KEYWORD_PERMUTATION_61
#elif defined(_USE_COVERAGE_MASK_TEXTURE)
#define KEYWORD_PERMUTATION_62
#else
#define KEYWORD_PERMUTATION_63
#endif


// Defines
#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS ScenePickingPass
#define BUILTIN_TARGET_API 1
#define SCENEPICKINGPASS 1
#ifdef _BUILTIN_SURFACE_TYPE_TRANSPARENT
#define _SURFACE_TYPE_TRANSPARENT _BUILTIN_SURFACE_TYPE_TRANSPARENT
#endif
#ifdef _BUILTIN_ALPHATEST_ON
#define _ALPHATEST_ON _BUILTIN_ALPHATEST_ON
#endif
#ifdef _BUILTIN_AlphaClip
#define _AlphaClip _BUILTIN_AlphaClip
#endif
#ifdef _BUILTIN_ALPHAPREMULTIPLY_ON
#define _ALPHAPREMULTIPLY_ON _BUILTIN_ALPHAPREMULTIPLY_ON
#endif


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Shim/Shims.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/LegacySurfaceVertex.hlsl"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/ShaderGraphFunctions.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
 float3 positionOS : POSITION;
 float3 normalOS : NORMAL;
 float4 tangentOS : TANGENT;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
 float4 positionCS : SV_POSITION;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};
struct SurfaceDescriptionInputs
{
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 AbsoluteWorldSpacePosition;
 float3 TimeParameters;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};

PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.positionCS = input.positionCS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.positionCS = input.positionCS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}


// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)

float _EnvironmentReactionSupported;
float4 _EnvState0, _EnvState1;
float _FuelCoverage, _AllowFuelCoverage;
float4 _WetTint, _CharTint, _FireColor;
float4 _EnvironmentCellBounds;
float _UseEnvironmentCells;

float _Height_from_Ground_Randomness;
float _Height_from_Ground_Softness;
float _Normal_Pow;
float _Normal_Mul;
float _Mask_Map_Size;
float4 _Coverage_Normal_Map_TexelSize;
float _Mask_Map_Intensity;
float4 _Coverage_ORM_Map_TexelSize;
float _Height_Weight;
float _Height_Gradience;
float _Wind_Intensity;
float3 _Wind_Direction;
float _Wind_Speed;
float _Wind_Time_Random_Offset_Amount;
float _Texture_Size;
float4 _Color_Map_TexelSize;
float4 _Color_Tint;
float4 _Normal_Map_TexelSize;
float _Normal_Intensity;
float4 _ORM_Map_TexelSize;
float _Roughness_Intensity;
float _AO_Intensity;
float _Metallic_Intensity;
float4 _Emissive_Map_TexelSize;
float4 _Emissive_Color;
float4 _Coverage_Color_Map_TexelSize;
float _Coverage_Size;
float _Height_from_Ground;
float4 _Mask_Map_TexelSize;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Coverage_Normal_Map);
SAMPLER(sampler_Coverage_Normal_Map);
TEXTURE2D(_Coverage_ORM_Map);
SAMPLER(sampler_Coverage_ORM_Map);
TEXTURE2D(_Color_Map);
SAMPLER(sampler_Color_Map);
TEXTURE2D(_Normal_Map);
SAMPLER(sampler_Normal_Map);
TEXTURE2D(_ORM_Map);
SAMPLER(sampler_ORM_Map);
TEXTURE2D(_Emissive_Map);
SAMPLER(sampler_Emissive_Map);
TEXTURE2D(_Coverage_Color_Map);
SAMPLER(sampler_Coverage_Color_Map);
TEXTURE2D(_Mask_Map);
SAMPLER(sampler_Mask_Map);

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Includes
// GraphIncludes: <None>

// Graph Functions

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Sine_float(float In, out float Out)
{
    Out = sin(In);
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Length_float3(float3 In, out float Out)
{
    Out = length(In);
}

void Unity_Divide_float(float A, float B, out float Out)
{
    Out = A / B;
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
{
    Out = lerp(A, B, T);
}

struct Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float
{
float3 ObjectSpacePosition;
float3 TimeParameters;
};

void SG_WindSway_51c4027ae50a8cd49be53c4990b9f179_float(float _Sway_Wind_Speed, float3 _Sway_Wind_Direction, float _Sway_Wind_Intensity, float _Sway_Wind_Time_Random_Offset_Amount, Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float IN, out float3 OutVector3_1)
{
float _Property_5c4cd19861f64014ae8e16af9365ef49_Out_0_Float = _Sway_Wind_Time_Random_Offset_Amount;
float _Add_4b31033339dc4f8e8e71674d98e662a3_Out_2_Float;
Unity_Add_float(IN.TimeParameters.x, _Property_5c4cd19861f64014ae8e16af9365ef49_Out_0_Float, _Add_4b31033339dc4f8e8e71674d98e662a3_Out_2_Float);
float _Property_9a93fa589ac7409884a94247349a940f_Out_0_Float = _Sway_Wind_Speed;
float _Multiply_7418655003ae4fedb2bf68678629472c_Out_2_Float;
Unity_Multiply_float_float(_Add_4b31033339dc4f8e8e71674d98e662a3_Out_2_Float, _Property_9a93fa589ac7409884a94247349a940f_Out_0_Float, _Multiply_7418655003ae4fedb2bf68678629472c_Out_2_Float);
float _Sine_ebd14b0fc42b4b4cb4387f3a1b879c1f_Out_1_Float;
Unity_Sine_float(_Multiply_7418655003ae4fedb2bf68678629472c_Out_2_Float, _Sine_ebd14b0fc42b4b4cb4387f3a1b879c1f_Out_1_Float);
float _Add_660e92170c564f94a79277cd2fd0cf20_Out_2_Float;
Unity_Add_float(_Sine_ebd14b0fc42b4b4cb4387f3a1b879c1f_Out_1_Float, float(1), _Add_660e92170c564f94a79277cd2fd0cf20_Out_2_Float);
float _Multiply_b8fd654b771447aebafc1a6aad6957cf_Out_2_Float;
Unity_Multiply_float_float(_Add_660e92170c564f94a79277cd2fd0cf20_Out_2_Float, 0.5, _Multiply_b8fd654b771447aebafc1a6aad6957cf_Out_2_Float);
float3 _Property_fa2ee033b44946c59c3fa9aea4a167c8_Out_0_Vector3 = _Sway_Wind_Direction;
float3 _Multiply_a5500b7f7b8e43988a055eea7eb917f3_Out_2_Vector3;
Unity_Multiply_float3_float3((_Multiply_b8fd654b771447aebafc1a6aad6957cf_Out_2_Float.xxx), _Property_fa2ee033b44946c59c3fa9aea4a167c8_Out_0_Vector3, _Multiply_a5500b7f7b8e43988a055eea7eb917f3_Out_2_Vector3);
float _Property_66cba0a3223546238e18b58e017e756f_Out_0_Float = _Sway_Wind_Intensity;
float3 _Multiply_475f81423e39483cba045bef08b9f967_Out_2_Vector3;
Unity_Multiply_float3_float3(_Multiply_a5500b7f7b8e43988a055eea7eb917f3_Out_2_Vector3, (_Property_66cba0a3223546238e18b58e017e756f_Out_0_Float.xxx), _Multiply_475f81423e39483cba045bef08b9f967_Out_2_Vector3);
float3 _Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3;
Unity_Add_float3(_Multiply_475f81423e39483cba045bef08b9f967_Out_2_Vector3, IN.ObjectSpacePosition, _Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3);
float _Length_53965ffb38de43359ab019a247194557_Out_1_Float;
Unity_Length_float3(IN.ObjectSpacePosition, _Length_53965ffb38de43359ab019a247194557_Out_1_Float);
float _Length_77b6b3d742104f2abf3c6968dea9a342_Out_1_Float;
Unity_Length_float3(_Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3, _Length_77b6b3d742104f2abf3c6968dea9a342_Out_1_Float);
float _Divide_f5dd43de79e343638d2743ee93ce7326_Out_2_Float;
Unity_Divide_float(_Length_53965ffb38de43359ab019a247194557_Out_1_Float, _Length_77b6b3d742104f2abf3c6968dea9a342_Out_1_Float, _Divide_f5dd43de79e343638d2743ee93ce7326_Out_2_Float);
float3 _Lerp_7574daf550e1428cb9d35656c7bb9603_Out_3_Vector3;
Unity_Lerp_float3(float3(0, 0, 0), _Add_4d4a16bbe1b24356bb869aafc376c5b4_Out_2_Vector3, (_Divide_f5dd43de79e343638d2743ee93ce7326_Out_2_Float.xxx), _Lerp_7574daf550e1428cb9d35656c7bb9603_Out_3_Vector3);
OutVector3_1 = _Lerp_7574daf550e1428cb9d35656c7bb9603_Out_3_Vector3;
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

void Unity_Power_float(float A, float B, out float Out)
{
    Out = pow(A, B);
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 Position;
float3 Normal;
float3 Tangent;
};

VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
float _Property_b2e048f6276d4371b6ad931f5d1baa87_Out_0_Float = _Wind_Speed;
float3 _Property_7133a0551bc7429cb8bbfcbad044bf97_Out_0_Vector3 = _Wind_Direction;
float _Property_5aa2352867cd4da79dc9f9f9979925c7_Out_0_Float = _Wind_Intensity;
float _Property_e0c6ebff99e349d6966115347961a70c_Out_0_Float = _Wind_Time_Random_Offset_Amount;
Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf;
_WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf.ObjectSpacePosition = IN.ObjectSpacePosition;
_WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf.TimeParameters = IN.TimeParameters;
float3 _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf_OutVector3_1_Vector3;
SG_WindSway_51c4027ae50a8cd49be53c4990b9f179_float(_Property_b2e048f6276d4371b6ad931f5d1baa87_Out_0_Float, _Property_7133a0551bc7429cb8bbfcbad044bf97_Out_0_Vector3, _Property_5aa2352867cd4da79dc9f9f9979925c7_Out_0_Float, _Property_e0c6ebff99e349d6966115347961a70c_Out_0_Float, _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf, _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf_OutVector3_1_Vector3);
float _Split_38420bd2034746a1878048f0ead2a7b2_R_1_Float = IN.AbsoluteWorldSpacePosition[0];
float _Split_38420bd2034746a1878048f0ead2a7b2_G_2_Float = IN.AbsoluteWorldSpacePosition[1];
float _Split_38420bd2034746a1878048f0ead2a7b2_B_3_Float = IN.AbsoluteWorldSpacePosition[2];
float _Split_38420bd2034746a1878048f0ead2a7b2_A_4_Float = 0;
float3 _Vector3_0f8475b3a0df451ea79f6ea2e4e3f673_Out_0_Vector3 = float3(float(0), float(0), float(0));
float3 _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3;
{
// Converting Position from Object to AbsoluteWorld via world space
float3 world;
world = TransformObjectToWorld(_Vector3_0f8475b3a0df451ea79f6ea2e4e3f673_Out_0_Vector3.xyz);
_Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3 = GetAbsolutePositionWS(world);
}
float _Split_fb1b2d84926449e498da6c90f1436311_R_1_Float = _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3[0];
float _Split_fb1b2d84926449e498da6c90f1436311_G_2_Float = _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3[1];
float _Split_fb1b2d84926449e498da6c90f1436311_B_3_Float = _Transform_df3a93ae4d1f42db8d6b81315b3788ad_Out_1_Vector3[2];
float _Split_fb1b2d84926449e498da6c90f1436311_A_4_Float = 0;
float _Subtract_18ddaa9851a547f1bc3eeafa55f3227b_Out_2_Float;
Unity_Subtract_float(_Split_38420bd2034746a1878048f0ead2a7b2_G_2_Float, _Split_fb1b2d84926449e498da6c90f1436311_G_2_Float, _Subtract_18ddaa9851a547f1bc3eeafa55f3227b_Out_2_Float);
float _Property_1db6873c977b47f186e42eef2cd75a57_Out_0_Float = _Height_Weight;
float _Split_bc5987163dba418d8de90e985de1247b_R_1_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[0];
float _Split_bc5987163dba418d8de90e985de1247b_G_2_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[1];
float _Split_bc5987163dba418d8de90e985de1247b_B_3_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[2];
float _Split_bc5987163dba418d8de90e985de1247b_A_4_Float = 0;
float _Multiply_89de5a5fb3cd451abb1227deba019c38_Out_2_Float;
Unity_Multiply_float_float(_Property_1db6873c977b47f186e42eef2cd75a57_Out_0_Float, _Split_bc5987163dba418d8de90e985de1247b_G_2_Float, _Multiply_89de5a5fb3cd451abb1227deba019c38_Out_2_Float);
float _Divide_2c406de6c63e463f9013e4faf1db1ae8_Out_2_Float;
Unity_Divide_float(_Subtract_18ddaa9851a547f1bc3eeafa55f3227b_Out_2_Float, _Multiply_89de5a5fb3cd451abb1227deba019c38_Out_2_Float, _Divide_2c406de6c63e463f9013e4faf1db1ae8_Out_2_Float);
float _Saturate_e9ea645ca89f482a9d0315df54a77ee5_Out_1_Float;
Unity_Saturate_float(_Divide_2c406de6c63e463f9013e4faf1db1ae8_Out_2_Float, _Saturate_e9ea645ca89f482a9d0315df54a77ee5_Out_1_Float);
float _Property_e96fd1920dff4533951da684129df200_Out_0_Float = _Height_Gradience;
float _Power_b88c5abc85424f17b03c60c98e9bc16c_Out_2_Float;
Unity_Power_float(_Saturate_e9ea645ca89f482a9d0315df54a77ee5_Out_1_Float, _Property_e96fd1920dff4533951da684129df200_Out_0_Float, _Power_b88c5abc85424f17b03c60c98e9bc16c_Out_2_Float);
float _Saturate_6c35d0fa108b483783bde88e06ff5ce0_Out_1_Float;
Unity_Saturate_float(_Power_b88c5abc85424f17b03c60c98e9bc16c_Out_2_Float, _Saturate_6c35d0fa108b483783bde88e06ff5ce0_Out_1_Float);
float3 _Lerp_8461a67d418941ee81ed9eb0f80e8a38_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _WindSway_6e77ee286e2e4e9888c00e7c4d0ce4cf_OutVector3_1_Vector3, (_Saturate_6c35d0fa108b483783bde88e06ff5ce0_Out_1_Float.xxx), _Lerp_8461a67d418941ee81ed9eb0f80e8a38_Out_3_Vector3);
#if defined(_USE_WPO)
float3 _UseWPO_81cb92bff33547fbbfaa5ce2f60ba97c_Out_0_Vector3 = _Lerp_8461a67d418941ee81ed9eb0f80e8a38_Out_3_Vector3;
#else
float3 _UseWPO_81cb92bff33547fbbfaa5ce2f60ba97c_Out_0_Vector3 = IN.ObjectSpacePosition;
#endif
description.Position = _UseWPO_81cb92bff33547fbbfaa5ce2f60ba97c_Out_0_Vector3;
description.Normal = IN.ObjectSpaceNormal;
description.Tangent = IN.ObjectSpaceTangent;
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
float3 reactionUnusedColor = 0, reactionUnusedEmission = 0;
ClipVegetationFuel(TransformObjectToWorld(float3(0,0,0)));
return surface;
}

// --------------------------------------------------
// Build Graph Inputs

VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

    output.ObjectSpaceNormal =                          input.normalOS;
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
    output.AbsoluteWorldSpacePosition =                 GetAbsolutePositionWS(TransformObjectToWorld(input.positionOS));
    output.TimeParameters =                             _TimeParameters.xyz;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

    






    #if UNITY_UV_STARTS_AT_TOP
    #else
    #endif


#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

void BuildAppDataFull(Attributes attributes, VertexDescription vertexDescription, inout appdata_full result)
{
    result.vertex     = float4(attributes.positionOS, 1);
    result.tangent    = attributes.tangentOS;
    result.normal     = attributes.normalOS;
    result.vertex     = float4(vertexDescription.Position, 1);
    result.normal     = vertexDescription.Normal;
    result.tangent    = float4(vertexDescription.Tangent, 0);
    #if UNITY_ANY_INSTANCING_ENABLED
    #endif
}

void VaryingsToSurfaceVertex(Varyings varyings, inout v2f_surf result)
{
    result.pos = varyings.positionCS;
    // World Tangent isn't an available input on v2f_surf


    #if UNITY_ANY_INSTANCING_ENABLED
    #endif
    #if UNITY_SHOULD_SAMPLE_SH
    #if !defined(LIGHTMAP_ON)
    #endif
    #endif
    #if defined(LIGHTMAP_ON)
    #endif
    #ifdef VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
        result.fogCoord = varyings.fogFactorAndVertexLight.x;
        COPY_TO_LIGHT_COORDS(result, varyings.fogFactorAndVertexLight.yzw);
    #endif

    DEFAULT_UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(varyings, result);
}

void SurfaceVertexToVaryings(v2f_surf surfVertex, inout Varyings result)
{
    result.positionCS = surfVertex.pos;
    // viewDirectionWS is never filled out in the legacy pass' function. Always use the value computed by SRP
    // World Tangent isn't an available input on v2f_surf

    #if UNITY_ANY_INSTANCING_ENABLED
    #endif
    #if UNITY_SHOULD_SAMPLE_SH
    #if !defined(LIGHTMAP_ON)
    #endif
    #endif
    #if defined(LIGHTMAP_ON)
    #endif
    #ifdef VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
        result.fogFactorAndVertexLight.x = surfVertex.fogCoord;
        COPY_FROM_LIGHT_COORDS(result.fogFactorAndVertexLight.yzw, surfVertex);
    #endif

    DEFAULT_UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(surfVertex, result);
}

// --------------------------------------------------
// Main

#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/Varyings.hlsl"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/DepthOnlyPass.hlsl"

ENDHLSL
}
}
CustomEditor "UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI"
CustomEditorForRenderPipeline "UnityEditor.Rendering.BuiltIn.ShaderGraph.BuiltInLitGUI" ""
CustomEditorForRenderPipeline "UnityEditor.ShaderGraphLitGUI" "UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset"
FallBack "Hidden/Shader Graph/FallbackError"
}