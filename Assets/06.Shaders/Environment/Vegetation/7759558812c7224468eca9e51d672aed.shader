Shader "UPlayGround/Environment/Vegetation/7759558812c7224468eca9e51d672aed"
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

_UV_Size("UV Size", Float) = 1
[MainTexture][NoScaleOffset]_ColorMap("Color Map", 2D) = "white" {}
[Normal][NoScaleOffset]_NormalMap("Normal Map", 2D) = "bump" {}
[NoScaleOffset]_ORMMap("ORM Map", 2D) = "white" {}
[NoScaleOffset]_Emissive_Map("Emissive Map", 2D) = "black" {}
_ColorTint("Color Tint", Color) = (1, 1, 1, 0)
_NormalIntensity("Normal Intensity", Range(0, 1)) = 1
_RoughnessIntensity("Roughness Intensity", Range(0, 2)) = 1
_MetallicIntensity("Metallic Intensity", Range(0, 1)) = 0
_EmissiveColor("Emissive Color", Color) = (1, 1, 1, 0)
_AOIntensity("AO Intensity", Range(0, 2)) = 1
_EmissiveIntensity("Emissive Intensity", Float) = 0
[NoScaleOffset]_ColorMap_1("Opacity Map", 2D) = "white" {}
_EmissiveIntensity_1("Alpha Clip", Float) = 0.5
[ToggleUI]_EnableCoverage("Enable Coverage?", Float) = 0
_CoverageSize("Coverage Size", Float) = 0
[NoScaleOffset]_CoverageColorMap("Coverage Color Map", 2D) = "white" {}
[Normal][NoScaleOffset]_CoverageNormalMap("Coverage Normal Map", 2D) = "bump" {}
[NoScaleOffset]_CoverageORMMap("Coverage ORM Map", 2D) = "white" {}
_CoverageNormalIntensity("Coverage Normal Intensity", Range(0, 1)) = 0
_CoverageRoughnessIntensity("Coverage Roughness Intensity", Range(0, 2)) = 0
_CoverageMetallicIntensity("Coverage Metallic Intensity", Range(0, 1)) = 0
[NoScaleOffset]_SlopeNoiseTexture("Slope Noise Texture", 2D) = "white" {}
_NoiseTextureSize("Noise Texture Size", Float) = 100
_SloapOffset("Sloap Offset", Float) = 0
_SloapContrast("Sloap Contrast", Float) = 1
[KeywordEnum(NormalBlending, VertexBlending, NoiseBlending)]_EDGEBLENDING("Edge Blending", Float) = 0
[NonModifiableTextureData][NoScaleOffset]_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D("Texture2D", 2D) = "white" {}
[HideInInspector]_WorkflowMode("_WorkflowMode", Float) = 1
[HideInInspector]_CastShadows("_CastShadows", Float) = 1
[HideInInspector]_ReceiveShadows("_ReceiveShadows", Float) = 1
[HideInInspector]_Surface("_Surface", Float) = 0
[HideInInspector]_Blend("_Blend", Float) = 0
[HideInInspector]_AlphaClip("_AlphaClip", Float) = 0
[HideInInspector]_BlendModePreserveSpecular("_BlendModePreserveSpecular", Float) = 0
[HideInInspector]_SrcBlend("_SrcBlend", Float) = 1
[HideInInspector]_DstBlend("_DstBlend", Float) = 0
[HideInInspector]_SrcBlendAlpha("_SrcBlendAlpha", Float) = 1
[HideInInspector]_DstBlendAlpha("_DstBlendAlpha", Float) = 0
[HideInInspector][ToggleUI]_ZWrite("_ZWrite", Float) = 1
[HideInInspector]_ZWriteControl("_ZWriteControl", Float) = 0
[HideInInspector]_ZTest("_ZTest", Float) = 4
[HideInInspector]_Cull("_Cull", Float) = 2
[HideInInspector]_AlphaToMask("_AlphaToMask", Float) = 0
[HideInInspector]_QueueOffset("_QueueOffset", Float) = 0
[HideInInspector]_QueueControl("_QueueControl", Float) = -1
[HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
[HideInInspector][NoScaleOffset]unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
[HideInInspector][NoScaleOffset]unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
[HideInInspector]_BUILTIN_Surface("Float", Float) = 0
[HideInInspector]_BUILTIN_Blend("Float", Float) = 0
[HideInInspector]_BUILTIN_AlphaClip("Float", Float) = 1
[HideInInspector]_BUILTIN_SrcBlend("Float", Float) = 1
[HideInInspector]_BUILTIN_DstBlend("Float", Float) = 0
[HideInInspector]_BUILTIN_ZWrite("Float", Float) = 1
[HideInInspector]_BUILTIN_ZWriteControl("Float", Float) = 0
[HideInInspector]_BUILTIN_ZTest("Float", Float) = 4
[HideInInspector]_BUILTIN_CullMode("Float", Float) = 2
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
Cull [_Cull]
Blend [_SrcBlend] [_DstBlend], [_SrcBlendAlpha] [_DstBlendAlpha]
ZTest [_ZTest]
ZWrite [_ZWrite]
AlphaToMask [_AlphaToMask]

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
#pragma shader_feature_fragment _ _SURFACE_TYPE_TRANSPARENT
#pragma shader_feature_local_fragment _ _ALPHAPREMULTIPLY_ON
#pragma shader_feature_local_fragment _ _ALPHAMODULATE_ON
#pragma shader_feature_local_fragment _ _ALPHATEST_ON
#pragma shader_feature_local_fragment _ _SPECULAR_SETUP
#pragma shader_feature_local _ _RECEIVE_SHADOWS_OFF
#pragma shader_feature_local _EDGEBLENDING_NORMALBLENDING _EDGEBLENDING_VERTEXBLENDING _EDGEBLENDING_NOISEBLENDING

#if defined(_EDGEBLENDING_NORMALBLENDING)
#define KEYWORD_PERMUTATION_0
#elif defined(_EDGEBLENDING_VERTEXBLENDING)
#define KEYWORD_PERMUTATION_1
#else
#define KEYWORD_PERMUTATION_2
#endif


// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_TEXCOORD1
#define ATTRIBUTES_NEED_TEXCOORD2
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_NORMAL_WS
#define VARYINGS_NEED_TANGENT_WS
#define VARYINGS_NEED_TEXCOORD0
#define VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
#define VARYINGS_NEED_SHADOW_COORD
#define VARYINGS_NEED_CULLFACE
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
 float3 ObjectSpaceNormal;
 float3 WorldSpaceNormal;
 float3 TangentSpaceNormal;
 float3 WorldSpaceTangent;
 float3 WorldSpaceBiTangent;
 float3 ObjectSpacePosition;
 float4 uv0;
 float FaceSign;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
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

float4 _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D_TexelSize;
float4 _ColorMap_TexelSize;
float4 _ColorMap_1_TexelSize;
float4 _NormalMap_TexelSize;
float4 _ColorTint;
float _NormalIntensity;
float _UV_Size;
float4 _ORMMap_TexelSize;
float _RoughnessIntensity;
float _AOIntensity;
float _MetallicIntensity;
float4 _EmissiveColor;
float _EmissiveIntensity_1;
float4 _Emissive_Map_TexelSize;
float _EmissiveIntensity;
float _EnableCoverage;
float _CoverageSize;
float _CoverageNormalIntensity;
float _CoverageRoughnessIntensity;
float _CoverageMetallicIntensity;
float _NoiseTextureSize;
float4 _SlopeNoiseTexture_TexelSize;
float4 _CoverageColorMap_TexelSize;
float4 _CoverageORMMap_TexelSize;
float4 _CoverageNormalMap_TexelSize;
float _SloapOffset;
float _SloapContrast;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D);
SAMPLER(sampler_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D);
TEXTURE2D(_ColorMap);
SAMPLER(sampler_ColorMap);
TEXTURE2D(_ColorMap_1);
SAMPLER(sampler_ColorMap_1);
TEXTURE2D(_NormalMap);
SAMPLER(sampler_NormalMap);
TEXTURE2D(_ORMMap);
SAMPLER(sampler_ORMMap);
TEXTURE2D(_Emissive_Map);
SAMPLER(sampler_Emissive_Map);
TEXTURE2D(_SlopeNoiseTexture);
SAMPLER(sampler_SlopeNoiseTexture);
TEXTURE2D(_CoverageColorMap);
SAMPLER(sampler_CoverageColorMap);
TEXTURE2D(_CoverageORMMap);
SAMPLER(sampler_CoverageORMMap);
TEXTURE2D(_CoverageNormalMap);
SAMPLER(sampler_CoverageNormalMap);

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

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

void Unity_NormalStrength_float(float3 In, float Strength, out float3 Out)
{
    Out = float3(In.rg * Strength, lerp(1, In.b, saturate(Strength)));
}

void Unity_Lerp_float(float A, float B, float T, out float Out)
{
    Out = lerp(A, B, T);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
}

struct Bindings_PASGORMUnpacker_abcd41e335c426a4bb0f8de17b4b5f96_float
{
};

void SG_PASGORMUnpacker_abcd41e335c426a4bb0f8de17b4b5f96_float(float2 _UV, UnityTexture2D _ORM_Map, float _AO_Intensity, float _Roughness_Intensity, float _Metallic_Intensity, Bindings_PASGORMUnpacker_abcd41e335c426a4bb0f8de17b4b5f96_float IN, out float Ambient_Occlusion_1, out float Roughness_2, out float Metallic_3)
{
float _Float_2c47c335e2304faa927a789be883bb19_Out_0_Float = float(1);
UnityTexture2D _Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D = _ORM_Map;
float2 _Property_ad3893fa315b474ea60e7ee3e265957c_Out_0_Vector2 = _UV;
float4 _SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D.tex, _Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D.samplerstate, _Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D.GetTransformedUV(_Property_ad3893fa315b474ea60e7ee3e265957c_Out_0_Vector2) );
if (_Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4, _Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_719055d2c0184958bd1329f59130cce0_R_4_Float = _SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4.r;
float _SampleTexture2D_719055d2c0184958bd1329f59130cce0_G_5_Float = _SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4.g;
float _SampleTexture2D_719055d2c0184958bd1329f59130cce0_B_6_Float = _SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4.b;
float _SampleTexture2D_719055d2c0184958bd1329f59130cce0_A_7_Float = _SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4.a;
float _Property_90898696aae3458aa180c6f15d96aaee_Out_0_Float = _AO_Intensity;
float _Lerp_bf670034cefc4c6d9e02b57f5394811d_Out_3_Float;
Unity_Lerp_float(_Float_2c47c335e2304faa927a789be883bb19_Out_0_Float, _SampleTexture2D_719055d2c0184958bd1329f59130cce0_R_4_Float, _Property_90898696aae3458aa180c6f15d96aaee_Out_0_Float, _Lerp_bf670034cefc4c6d9e02b57f5394811d_Out_3_Float);
float _Property_e77944e4743744198e931506300c92ca_Out_0_Float = _Roughness_Intensity;
float _Multiply_43e873dc525645aab065eb6fc56ad77f_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_719055d2c0184958bd1329f59130cce0_G_5_Float, _Property_e77944e4743744198e931506300c92ca_Out_0_Float, _Multiply_43e873dc525645aab065eb6fc56ad77f_Out_2_Float);
float _OneMinus_3225b57b02ed4eaf9c118419fbefc479_Out_1_Float;
Unity_OneMinus_float(_Multiply_43e873dc525645aab065eb6fc56ad77f_Out_2_Float, _OneMinus_3225b57b02ed4eaf9c118419fbefc479_Out_1_Float);
float _Property_c0c103f0dfcd475fa428b0e043905944_Out_0_Float = _Metallic_Intensity;
float _Multiply_38b5ca9e78ba40d9b861ffc15889bd7f_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_719055d2c0184958bd1329f59130cce0_B_6_Float, _Property_c0c103f0dfcd475fa428b0e043905944_Out_0_Float, _Multiply_38b5ca9e78ba40d9b861ffc15889bd7f_Out_2_Float);
Ambient_Occlusion_1 = _Lerp_bf670034cefc4c6d9e02b57f5394811d_Out_3_Float;
Roughness_2 = _OneMinus_3225b57b02ed4eaf9c118419fbefc479_Out_1_Float;
Metallic_3 = _Multiply_38b5ca9e78ba40d9b861ffc15889bd7f_Out_2_Float;
}

struct Bindings_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float
{
half4 uv0;
};

void SG_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float(float _Texture_Size, UnityTexture2D _Color_Map, float4 _Color_Tint, UnityTexture2D _Normal_Map, float _Normal_Intensity, UnityTexture2D _ORM_Map, float _Roughness_Intensity, float _AO_Intensity, float _Metallic_Intensity, UnityTexture2D _Emissive_Map, float4 _Emissive_Color, float _Emissive_Intensity, Bindings_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float IN, out float4 Base_Color_5, out float3 Normal_1, out float4 Emissive_6, out float Ambient_Occlusion_4, out float Roughness_3, out float Metallic_2)
{
UnityTexture2D _Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D = _Color_Map;
float _Property_6e866f25d28140918dd5249261b96e76_Out_0_Float = _Texture_Size;
float2 _TilingAndOffset_49c249d74ea54ccf979206ad2c3c0c00_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, (_Property_6e866f25d28140918dd5249261b96e76_Out_0_Float.xx), float2 (0, 0), _TilingAndOffset_49c249d74ea54ccf979206ad2c3c0c00_Out_3_Vector2);
float _Property_94852b2b32f549138996f0dfb35b0486_Out_0_Float = _Texture_Size;
float2 _Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2;
Unity_Multiply_float2_float2(_TilingAndOffset_49c249d74ea54ccf979206ad2c3c0c00_Out_3_Vector2, (_Property_94852b2b32f549138996f0dfb35b0486_Out_0_Float.xx), _Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2);
float4 _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D.tex, _Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D.samplerstate, _Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D.GetTransformedUV(_Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2) );
if (_Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4, _Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_R_4_Float = _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4.r;
float _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_G_5_Float = _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4.g;
float _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_B_6_Float = _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4.b;
float _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_A_7_Float = _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4.a;
float4 _Property_c081d1852d284e4fa4bbb04ea3adf35e_Out_0_Vector4 = _Color_Tint;
float4 _Multiply_ff3b38102028498db7cc80acf5e42362_Out_2_Vector4;
Unity_Multiply_float4_float4(_SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4, _Property_c081d1852d284e4fa4bbb04ea3adf35e_Out_0_Vector4, _Multiply_ff3b38102028498db7cc80acf5e42362_Out_2_Vector4);
UnityTexture2D _Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D = _Normal_Map;
float4 _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D.tex, _Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D.samplerstate, _Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D.GetTransformedUV(_Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2) );
if (_Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4, _Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4);
float _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_R_4_Float = _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.r;
float _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_G_5_Float = _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.g;
float _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_B_6_Float = _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.b;
float _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_A_7_Float = _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.a;
float _Property_8b1f839dfbcd4481b201427fbc7310ad_Out_0_Float = _Normal_Intensity;
float3 _NormalStrength_a218ef03cc884bf0b0962d85b628afe7_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.xyz), _Property_8b1f839dfbcd4481b201427fbc7310ad_Out_0_Float, _NormalStrength_a218ef03cc884bf0b0962d85b628afe7_Out_2_Vector3);
UnityTexture2D _Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D = _Emissive_Map;
float4 _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D.tex, _Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D.samplerstate, _Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D.GetTransformedUV(_Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2) );
if (_Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4, _Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_R_4_Float = _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4.r;
float _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_G_5_Float = _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4.g;
float _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_B_6_Float = _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4.b;
float _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_A_7_Float = _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4.a;
float4 _Property_3a2f8dbe365d427e89744f8eb8dabd44_Out_0_Vector4 = _Emissive_Color;
float4 _Multiply_1efa7dc8f3994d8ea213351379d48448_Out_2_Vector4;
Unity_Multiply_float4_float4(_SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4, _Property_3a2f8dbe365d427e89744f8eb8dabd44_Out_0_Vector4, _Multiply_1efa7dc8f3994d8ea213351379d48448_Out_2_Vector4);
float _Property_09c3fe7e22be43caa45c324f52ea8137_Out_0_Float = _Emissive_Intensity;
float4 _Multiply_43a1c0123fb34a25a5d21581f08926f8_Out_2_Vector4;
Unity_Multiply_float4_float4(_Multiply_1efa7dc8f3994d8ea213351379d48448_Out_2_Vector4, (_Property_09c3fe7e22be43caa45c324f52ea8137_Out_0_Float.xxxx), _Multiply_43a1c0123fb34a25a5d21581f08926f8_Out_2_Vector4);
UnityTexture2D _Property_ecbd8a4e938a458cabefe2480cada231_Out_0_Texture2D = _ORM_Map;
float _Property_2ca0dc7be16d47bfb0faf210c4d70757_Out_0_Float = _AO_Intensity;
float _Property_fb245dde136f4b2486e260e720f83c83_Out_0_Float = _Roughness_Intensity;
float _Property_0fcde0f68f364243886b868db18a8d27_Out_0_Float = _Metallic_Intensity;
Bindings_PASGORMUnpacker_abcd41e335c426a4bb0f8de17b4b5f96_float _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94;
half _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_AmbientOcclusion_1_Float;
half _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Roughness_2_Float;
half _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Metallic_3_Float;
SG_PASGORMUnpacker_abcd41e335c426a4bb0f8de17b4b5f96_float(_Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2, _Property_ecbd8a4e938a458cabefe2480cada231_Out_0_Texture2D, _Property_2ca0dc7be16d47bfb0faf210c4d70757_Out_0_Float, _Property_fb245dde136f4b2486e260e720f83c83_Out_0_Float, _Property_0fcde0f68f364243886b868db18a8d27_Out_0_Float, _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94, _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_AmbientOcclusion_1_Float, _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Roughness_2_Float, _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Metallic_3_Float);
Base_Color_5 = _Multiply_ff3b38102028498db7cc80acf5e42362_Out_2_Vector4;
Normal_1 = _NormalStrength_a218ef03cc884bf0b0962d85b628afe7_Out_2_Vector3;
Emissive_6 = _Multiply_43a1c0123fb34a25a5d21581f08926f8_Out_2_Vector4;
Ambient_Occlusion_4 = _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_AmbientOcclusion_1_Float;
Roughness_3 = _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Roughness_2_Float;
Metallic_2 = _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Metallic_3_Float;
}

void Unity_DotProduct_float3(float3 A, float3 B, out float Out)
{
    Out = dot(A, B);
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Clamp_float(float In, float Min, float Max, out float Out)
{
    Out = clamp(In, Min, Max);
}

void Unity_Contrast_float(float3 In, float Contrast, out float3 Out)
{
    float midpoint = pow(0.5, 2.2);
    Out =  (In - midpoint) * Contrast + midpoint;
}

void Unity_Clamp_float3(float3 In, float3 Min, float3 Max, out float3 Out)
{
    Out = clamp(In, Min, Max);
}

struct Bindings_PASGVerticalBlend_e9f0b2d5820244846982772499370842_float
{
float3 ObjectSpaceNormal;
float3 WorldSpaceNormal;
float3 WorldSpaceTangent;
float3 WorldSpaceBiTangent;
float3 ObjectSpacePosition;
};

void SG_PASGVerticalBlend_e9f0b2d5820244846982772499370842_float(float _Sloap_Contrast, UnityTexture2D _Slope_Noise_Texture, float _Noise_Texture_Size, float3 _Explicit_Normal, float _Sloap_Offset, Bindings_PASGVerticalBlend_e9f0b2d5820244846982772499370842_float IN, out float3 OutVector3_1)
{
float3 _Property_5509c7fb30e445229f496a71c6e9c192_Out_0_Vector3 = _Explicit_Normal;
float3 _Transform_9c9f921cd0934012ac0d281c8e40a727_Out_1_Vector3;
{
float3x3 tangentTransform = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
_Transform_9c9f921cd0934012ac0d281c8e40a727_Out_1_Vector3 = TransformTangentToWorld(_Property_5509c7fb30e445229f496a71c6e9c192_Out_0_Vector3.xyz, tangentTransform, true);
}
float _DotProduct_4bc69611266041259c8da1b387474ec9_Out_2_Float;
Unity_DotProduct_float3(_Transform_9c9f921cd0934012ac0d281c8e40a727_Out_1_Vector3, float3(0, 1, 0), _DotProduct_4bc69611266041259c8da1b387474ec9_Out_2_Float);
float _Property_6a995c4e80824d6fa7d458ddcdc4eec3_Out_0_Float = _Sloap_Offset;
float _Add_a196acc146d1466fb309ec050e77feb2_Out_2_Float;
Unity_Add_float(_DotProduct_4bc69611266041259c8da1b387474ec9_Out_2_Float, _Property_6a995c4e80824d6fa7d458ddcdc4eec3_Out_0_Float, _Add_a196acc146d1466fb309ec050e77feb2_Out_2_Float);
float _Lerp_a1fed41c0847449ab72ae8fe4775270c_Out_3_Float;
Unity_Lerp_float(float(0), float(3), _Add_a196acc146d1466fb309ec050e77feb2_Out_2_Float, _Lerp_a1fed41c0847449ab72ae8fe4775270c_Out_3_Float);
float _Clamp_d4d6e762b7b1422d936b98d7f9429754_Out_3_Float;
Unity_Clamp_float(_Lerp_a1fed41c0847449ab72ae8fe4775270c_Out_3_Float, float(0), float(1), _Clamp_d4d6e762b7b1422d936b98d7f9429754_Out_3_Float);
float _Property_985b04ddd40847029dbfa829fbcaeed1_Out_0_Float = _Sloap_Contrast;
float3 _Contrast_2864897542064adcb4b61fcccadaf026_Out_2_Vector3;
Unity_Contrast_float((_Clamp_d4d6e762b7b1422d936b98d7f9429754_Out_3_Float.xxx), _Property_985b04ddd40847029dbfa829fbcaeed1_Out_0_Float, _Contrast_2864897542064adcb4b61fcccadaf026_Out_2_Vector3);
float3 _Clamp_b8f1f82d045f437a910a54b62bb2df81_Out_3_Vector3;
Unity_Clamp_float3(_Contrast_2864897542064adcb4b61fcccadaf026_Out_2_Vector3, float3(0, 0, 0), float3(1, 1, 1), _Clamp_b8f1f82d045f437a910a54b62bb2df81_Out_3_Vector3);
float3 _Transform_229ffe25002343a39c8caaa3d7f6df91_Out_1_Vector3;
_Transform_229ffe25002343a39c8caaa3d7f6df91_Out_1_Vector3 = TransformObjectToWorldDir(IN.ObjectSpacePosition.xyz, true);
float3 _Vector3_ec86905b34c54695bed5b7d1975cb6ce_Out_0_Vector3 = float3(float(0), float(2), float(0));
float _DotProduct_af3cb3caf6e4473c8161868b15c0255b_Out_2_Float;
Unity_DotProduct_float3(_Transform_229ffe25002343a39c8caaa3d7f6df91_Out_1_Vector3, _Vector3_ec86905b34c54695bed5b7d1975cb6ce_Out_0_Vector3, _DotProduct_af3cb3caf6e4473c8161868b15c0255b_Out_2_Float);
float _Add_47be9b48b4c94dd28e8c3db7f6ba1a05_Out_2_Float;
Unity_Add_float(_Property_6a995c4e80824d6fa7d458ddcdc4eec3_Out_0_Float, _DotProduct_af3cb3caf6e4473c8161868b15c0255b_Out_2_Float, _Add_47be9b48b4c94dd28e8c3db7f6ba1a05_Out_2_Float);
float _Multiply_35491cd334cb4e34a76b48de2bc5814a_Out_2_Float;
Unity_Multiply_float_float(_Add_47be9b48b4c94dd28e8c3db7f6ba1a05_Out_2_Float, 2, _Multiply_35491cd334cb4e34a76b48de2bc5814a_Out_2_Float);
float _Clamp_ccd0969e9da6420f94e7113654fd480e_Out_3_Float;
Unity_Clamp_float(_Multiply_35491cd334cb4e34a76b48de2bc5814a_Out_2_Float, float(0), float(1), _Clamp_ccd0969e9da6420f94e7113654fd480e_Out_3_Float);
float3 _Contrast_454ba7c8fb3a4d03951b073867fa5f14_Out_2_Vector3;
Unity_Contrast_float((_Clamp_ccd0969e9da6420f94e7113654fd480e_Out_3_Float.xxx), _Property_985b04ddd40847029dbfa829fbcaeed1_Out_0_Float, _Contrast_454ba7c8fb3a4d03951b073867fa5f14_Out_2_Vector3);
float3 _Clamp_1ba5cf0aee8245bd9dca155954c3df81_Out_3_Vector3;
Unity_Clamp_float3(_Contrast_454ba7c8fb3a4d03951b073867fa5f14_Out_2_Vector3, float3(0, 0, 0), float3(1, 1, 1), _Clamp_1ba5cf0aee8245bd9dca155954c3df81_Out_3_Vector3);
UnityTexture2D _Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D = _Slope_Noise_Texture;
float _Property_8e663ddd1476448d948a9de1bf1f83a9_Out_0_Float = _Noise_Texture_Size;
float3 Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_UV = IN.ObjectSpacePosition * _Property_8e663ddd1476448d948a9de1bf1f83a9_Out_0_Float;
float3 Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend = SafePositivePow_float(IN.ObjectSpaceNormal, min(float(1), floor(log2(Min_float())/log2(1/sqrt(3)))) );
Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend /= dot(Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend, 1.0);
float4 Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_X = SAMPLE_TEXTURE2D(_Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.tex, _Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.samplerstate, Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_UV.zy);
float4 Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Y = SAMPLE_TEXTURE2D(_Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.tex, _Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.samplerstate, Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_UV.xz);
float4 Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Z = SAMPLE_TEXTURE2D(_Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.tex, _Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.samplerstate, Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_UV.xy);
float4 _Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Out_0_Vector4 = Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_X * Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend.x + Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Y * Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend.y + Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Z * Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend.z;
float _Swizzle_90fb1902a2bd4070b4f9e27ac287ca97_Out_1_Float = (_Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Out_0_Vector4.xy).y;
float _Clamp_f32c97c6ccbf44649bd757503ccdbe71_Out_3_Float;
Unity_Clamp_float(_Swizzle_90fb1902a2bd4070b4f9e27ac287ca97_Out_1_Float, float(0), float(1), _Clamp_f32c97c6ccbf44649bd757503ccdbe71_Out_3_Float);
float _Add_92d15430b3c042719d65f1252bc35d7a_Out_2_Float;
Unity_Add_float(_Multiply_35491cd334cb4e34a76b48de2bc5814a_Out_2_Float, _Clamp_f32c97c6ccbf44649bd757503ccdbe71_Out_3_Float, _Add_92d15430b3c042719d65f1252bc35d7a_Out_2_Float);
float3 _Contrast_c2fe106c51f0498e987cf31090ec563d_Out_2_Vector3;
Unity_Contrast_float((_Add_92d15430b3c042719d65f1252bc35d7a_Out_2_Float.xxx), _Property_985b04ddd40847029dbfa829fbcaeed1_Out_0_Float, _Contrast_c2fe106c51f0498e987cf31090ec563d_Out_2_Vector3);
float3 _Clamp_05438405442c47cb98e571d128277bbe_Out_3_Vector3;
Unity_Clamp_float3(_Contrast_c2fe106c51f0498e987cf31090ec563d_Out_2_Vector3, float3(0, 0, 0), float3(1, 1, 1), _Clamp_05438405442c47cb98e571d128277bbe_Out_3_Vector3);
#if defined(_EDGEBLENDING_NORMALBLENDING)
float3 _EdgeBlending_cdbe33a15cf6452b8881ecea22adcb84_Out_0_Vector3 = _Clamp_b8f1f82d045f437a910a54b62bb2df81_Out_3_Vector3;
#elif defined(_EDGEBLENDING_VERTEXBLENDING)
float3 _EdgeBlending_cdbe33a15cf6452b8881ecea22adcb84_Out_0_Vector3 = _Clamp_1ba5cf0aee8245bd9dca155954c3df81_Out_3_Vector3;
#else
float3 _EdgeBlending_cdbe33a15cf6452b8881ecea22adcb84_Out_0_Vector3 = _Clamp_05438405442c47cb98e571d128277bbe_Out_3_Vector3;
#endif
OutVector3_1 = _EdgeBlending_cdbe33a15cf6452b8881ecea22adcb84_Out_0_Vector3;
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Branch_float3(float Predicate, float3 True, float3 False, out float3 Out)
{
    Out = Predicate ? True : False;
}

void Unity_Negate_float3(float3 In, out float3 Out)
{
    Out = -1 * In;
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
description.Position = IN.ObjectSpacePosition;
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
float3 Specular;
float Smoothness;
float Occlusion;
float Alpha;
float AlphaClipThreshold;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
float _Property_868c674f556d42be9071980dc60f4aed_Out_0_Boolean = _EnableCoverage;
float _Property_a0b21575a96c42a2b05aa3518645b2f4_Out_0_Float = _UV_Size;
UnityTexture2D _Property_4d248f4a9de94d7bb9a5c3c7c4c10a68_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_ColorMap, sampler_ColorMap, _ColorMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _Property_061dd20995c54e4b8c0357aa709c6e56_Out_0_Vector4 = _ColorTint;
UnityTexture2D _Property_cc904da6b4714cd6a9559b3bcf6ba7a6_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_NormalMap, sampler_NormalMap, _NormalMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_49f412000c0b4f18863fb7afae8b43fc_Out_0_Float = _NormalIntensity;
UnityTexture2D _Property_3fcfe917c75d4a46bfe50da31a24c74f_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_ORMMap, sampler_ORMMap, _ORMMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_bd35b765c62349f285447de81728f0c8_Out_0_Float = _RoughnessIntensity;
float _Property_f125a72836c24b0fa5ef72823c8e6452_Out_0_Float = _AOIntensity;
float _Property_0251a196075947af9d3ad28d6875a5ef_Out_0_Float = _MetallicIntensity;
UnityTexture2D _Property_47ecc092fbd144218fe6440250e367b3_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Emissive_Map, sampler_Emissive_Map, _Emissive_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _Property_c065f0d4df8c40528116ee8e516b2221_Out_0_Vector4 = _EmissiveColor;
float _Property_0038e41f028740e4abc1958ca58302a1_Out_0_Float = _EmissiveIntensity;
Bindings_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe;
_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe.uv0 = IN.uv0;
float4 _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_BaseColor_5_Vector4;
float3 _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Normal_1_Vector3;
float4 _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Emissive_6_Vector4;
float _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_AmbientOcclusion_4_Float;
float _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Roughness_3_Float;
float _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Metallic_2_Float;
SG_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float(_Property_a0b21575a96c42a2b05aa3518645b2f4_Out_0_Float, _Property_4d248f4a9de94d7bb9a5c3c7c4c10a68_Out_0_Texture2D, _Property_061dd20995c54e4b8c0357aa709c6e56_Out_0_Vector4, _Property_cc904da6b4714cd6a9559b3bcf6ba7a6_Out_0_Texture2D, _Property_49f412000c0b4f18863fb7afae8b43fc_Out_0_Float, _Property_3fcfe917c75d4a46bfe50da31a24c74f_Out_0_Texture2D, _Property_bd35b765c62349f285447de81728f0c8_Out_0_Float, _Property_f125a72836c24b0fa5ef72823c8e6452_Out_0_Float, _Property_0251a196075947af9d3ad28d6875a5ef_Out_0_Float, _Property_47ecc092fbd144218fe6440250e367b3_Out_0_Texture2D, _Property_c065f0d4df8c40528116ee8e516b2221_Out_0_Vector4, _Property_0038e41f028740e4abc1958ca58302a1_Out_0_Float, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_BaseColor_5_Vector4, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Normal_1_Vector3, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Emissive_6_Vector4, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_AmbientOcclusion_4_Float, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Roughness_3_Float, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Metallic_2_Float);
float _Property_623a0c9d694c4c33b6256bde952c53e5_Out_0_Float = _CoverageSize;
UnityTexture2D _Property_58d380b2034145c8b7d2eda637dbc296_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_CoverageColorMap, sampler_CoverageColorMap, _CoverageColorMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
UnityTexture2D _Property_f324dd6628ea45b1a630d750fa3d6b66_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_CoverageNormalMap, sampler_CoverageNormalMap, _CoverageNormalMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_b00b34515282450295cc4f4eb7b4a0ce_Out_0_Float = _CoverageNormalIntensity;
UnityTexture2D _Property_24ae6469bfeb4dada6fb7c7d39985cb3_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_CoverageORMMap, sampler_CoverageORMMap, _CoverageORMMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_03c29a27092f4bbb8a4fcc3b6e7b2909_Out_0_Float = _CoverageRoughnessIntensity;
float _Property_07cad64e572e4602a346b7dd90c3eb21_Out_0_Float = _CoverageMetallicIntensity;
Bindings_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c;
_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c.uv0 = IN.uv0;
float4 _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_BaseColor_5_Vector4;
float3 _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Normal_1_Vector3;
float4 _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Emissive_6_Vector4;
float _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_AmbientOcclusion_4_Float;
float _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Roughness_3_Float;
float _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Metallic_2_Float;
SG_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float(_Property_623a0c9d694c4c33b6256bde952c53e5_Out_0_Float, _Property_58d380b2034145c8b7d2eda637dbc296_Out_0_Texture2D, float4 (1, 1, 1, 1), _Property_f324dd6628ea45b1a630d750fa3d6b66_Out_0_Texture2D, _Property_b00b34515282450295cc4f4eb7b4a0ce_Out_0_Float, _Property_24ae6469bfeb4dada6fb7c7d39985cb3_Out_0_Texture2D, _Property_03c29a27092f4bbb8a4fcc3b6e7b2909_Out_0_Float, float(0), _Property_07cad64e572e4602a346b7dd90c3eb21_Out_0_Float, UnityBuildTexture2DStructNoScale(_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D), float4 (0, 0, 0, 0), float(0), _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_BaseColor_5_Vector4, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Normal_1_Vector3, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Emissive_6_Vector4, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_AmbientOcclusion_4_Float, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Roughness_3_Float, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Metallic_2_Float);
float _Property_0785fc58d71d4d63abb8c1102aa1a1b7_Out_0_Float = _SloapContrast;
UnityTexture2D _Property_12921d1841154c59b11aefccb2cbca31_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_SlopeNoiseTexture, sampler_SlopeNoiseTexture, _SlopeNoiseTexture_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_10dd46128137476fb3fd49de6154e94c_Out_0_Float = _NoiseTextureSize;
float _Property_a654ef8b1e2a4529ab82f92b3ae202ec_Out_0_Float = _SloapOffset;
Bindings_PASGVerticalBlend_e9f0b2d5820244846982772499370842_float _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d;
_PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d.ObjectSpaceNormal = IN.ObjectSpaceNormal;
_PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d.WorldSpaceNormal = IN.WorldSpaceNormal;
_PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d.WorldSpaceTangent = IN.WorldSpaceTangent;
_PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d.WorldSpaceBiTangent = IN.WorldSpaceBiTangent;
_PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d.ObjectSpacePosition = IN.ObjectSpacePosition;
float3 _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d_OutVector3_1_Vector3;
SG_PASGVerticalBlend_e9f0b2d5820244846982772499370842_float(_Property_0785fc58d71d4d63abb8c1102aa1a1b7_Out_0_Float, _Property_12921d1841154c59b11aefccb2cbca31_Out_0_Texture2D, _Property_10dd46128137476fb3fd49de6154e94c_Out_0_Float, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Normal_1_Vector3, _Property_a654ef8b1e2a4529ab82f92b3ae202ec_Out_0_Float, _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d, _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d_OutVector3_1_Vector3);
float3 _Lerp_8ef5a2e39e934fb09e1bc6858ba97959_Out_3_Vector3;
Unity_Lerp_float3((_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_BaseColor_5_Vector4.xyz), (_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_BaseColor_5_Vector4.xyz), _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d_OutVector3_1_Vector3, _Lerp_8ef5a2e39e934fb09e1bc6858ba97959_Out_3_Vector3);
float3 _Branch_72adc4bf3fd64427a9a9035afa6f856a_Out_3_Vector3;
Unity_Branch_float3(_Property_868c674f556d42be9071980dc60f4aed_Out_0_Boolean, _Lerp_8ef5a2e39e934fb09e1bc6858ba97959_Out_3_Vector3, (_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_BaseColor_5_Vector4.xyz), _Branch_72adc4bf3fd64427a9a9035afa6f856a_Out_3_Vector3);
float _IsFrontFace_8b8f9781e7a946f18c8c49da20d0925c_Out_0_Boolean = max(0, IN.FaceSign.x);
float3 _Lerp_e69be27d4f0d4e26a37c622c629ca56d_Out_3_Vector3;
Unity_Lerp_float3(_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Normal_1_Vector3, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Normal_1_Vector3, _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d_OutVector3_1_Vector3, _Lerp_e69be27d4f0d4e26a37c622c629ca56d_Out_3_Vector3);
float3 _Branch_3213119f26d54fc08b755c491d9dd6c4_Out_3_Vector3;
Unity_Branch_float3(_Property_868c674f556d42be9071980dc60f4aed_Out_0_Boolean, _Lerp_e69be27d4f0d4e26a37c622c629ca56d_Out_3_Vector3, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Normal_1_Vector3, _Branch_3213119f26d54fc08b755c491d9dd6c4_Out_3_Vector3);
float3 _Negate_c9035ab4407c48978c2cd379c7ed3db2_Out_1_Vector3;
Unity_Negate_float3(_Branch_3213119f26d54fc08b755c491d9dd6c4_Out_3_Vector3, _Negate_c9035ab4407c48978c2cd379c7ed3db2_Out_1_Vector3);
float3 _Branch_6a68e6f7151b40c684e82041ff10474e_Out_3_Vector3;
Unity_Branch_float3(_IsFrontFace_8b8f9781e7a946f18c8c49da20d0925c_Out_0_Boolean, _Branch_3213119f26d54fc08b755c491d9dd6c4_Out_3_Vector3, _Negate_c9035ab4407c48978c2cd379c7ed3db2_Out_1_Vector3, _Branch_6a68e6f7151b40c684e82041ff10474e_Out_3_Vector3);
float3 _Lerp_e6a3b112a78740919a21e1a67f8a9012_Out_3_Vector3;
Unity_Lerp_float3((_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Roughness_3_Float.xxx), (_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Roughness_3_Float.xxx), _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d_OutVector3_1_Vector3, _Lerp_e6a3b112a78740919a21e1a67f8a9012_Out_3_Vector3);
float3 _Branch_e3527157b2634e17853db32690e6cf9f_Out_3_Vector3;
Unity_Branch_float3(_Property_868c674f556d42be9071980dc60f4aed_Out_0_Boolean, _Lerp_e6a3b112a78740919a21e1a67f8a9012_Out_3_Vector3, (_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Metallic_2_Float.xxx), _Branch_e3527157b2634e17853db32690e6cf9f_Out_3_Vector3);
float3 _Lerp_0bf96dce18b642f4ab3b848bb1fe45d6_Out_3_Vector3;
Unity_Lerp_float3((_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Metallic_2_Float.xxx), (_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Metallic_2_Float.xxx), _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d_OutVector3_1_Vector3, _Lerp_0bf96dce18b642f4ab3b848bb1fe45d6_Out_3_Vector3);
float3 _Branch_bcdc5fe84435477ab329331520e86c7d_Out_3_Vector3;
Unity_Branch_float3(_Property_868c674f556d42be9071980dc60f4aed_Out_0_Boolean, _Lerp_0bf96dce18b642f4ab3b848bb1fe45d6_Out_3_Vector3, (_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Roughness_3_Float.xxx), _Branch_bcdc5fe84435477ab329331520e86c7d_Out_3_Vector3);
float3 _Lerp_1ffb4b0a9a70465db54818dd6df0be7c_Out_3_Vector3;
Unity_Lerp_float3((_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_AmbientOcclusion_4_Float.xxx), (_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_AmbientOcclusion_4_Float.xxx), _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d_OutVector3_1_Vector3, _Lerp_1ffb4b0a9a70465db54818dd6df0be7c_Out_3_Vector3);
float3 _Branch_c19f53b6fc08465e849902625a3db36e_Out_3_Vector3;
Unity_Branch_float3(_Property_868c674f556d42be9071980dc60f4aed_Out_0_Boolean, _Lerp_1ffb4b0a9a70465db54818dd6df0be7c_Out_3_Vector3, (_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_AmbientOcclusion_4_Float.xxx), _Branch_c19f53b6fc08465e849902625a3db36e_Out_3_Vector3);
UnityTexture2D _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_ColorMap_1, sampler_ColorMap_1, _ColorMap_1_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.tex, _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.samplerstate, _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4, _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_R_4_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.r;
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_G_5_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.g;
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_B_6_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.b;
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_A_7_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.a;
float _Property_7a43d5f1d3df46c2869bc70eebee1f91_Out_0_Float = _EmissiveIntensity_1;
surface.BaseColor = _Branch_72adc4bf3fd64427a9a9035afa6f856a_Out_3_Vector3;
surface.NormalTS = _Branch_6a68e6f7151b40c684e82041ff10474e_Out_3_Vector3;
surface.Emission = (_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Emissive_6_Vector4.xyz);
surface.Metallic = (_Branch_e3527157b2634e17853db32690e6cf9f_Out_3_Vector3).x;
surface.Specular = IsGammaSpace() ? float3(0.5, 0.5, 0.5) : SRGBToLinear(float3(0.5, 0.5, 0.5));
surface.Smoothness = (_Branch_bcdc5fe84435477ab329331520e86c7d_Out_3_Vector3).x;
surface.Occlusion = (_Branch_c19f53b6fc08465e849902625a3db36e_Out_3_Vector3).x;
surface.Alpha = (_SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4).x;
surface.AlphaClipThreshold = _Property_7a43d5f1d3df46c2869bc70eebee1f91_Out_0_Float;
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

    // use bitangent on the fly like in hdrp
    // IMPORTANT! If we ever support Flip on double sided materials ensure bitangent and tangent are NOT flipped.
    float crossSign = (input.tangentWS.w > 0.0 ? 1.0 : -1.0)* GetOddNegativeScale();
    float3 bitang = crossSign * cross(input.normalWS.xyz, input.tangentWS.xyz);

    output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph
    output.ObjectSpaceNormal = normalize(mul(output.WorldSpaceNormal, (float3x3) UNITY_MATRIX_M));           // transposed multiplication by inverse matrix to handle normal scale
    output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);

    // to pr               eserve mikktspace compliance we use same scale renormFactor as was used on the normal.
    // This                is explained in section 2.2 in "surface gradient based bump mapping framework"
    output.WorldSpaceTangent = renormFactor * input.tangentWS.xyz;
    output.WorldSpaceBiTangent = renormFactor * bitang;

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
    BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
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
Cull [_Cull]
Blend [_SrcBlend] [_DstBlend], [_SrcBlendAlpha] [_DstBlendAlpha]
ZTest [_ZTest]
ZWrite [_ZWrite]

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
#pragma shader_feature_fragment _ _SURFACE_TYPE_TRANSPARENT
#pragma shader_feature_local_fragment _ _ALPHAPREMULTIPLY_ON
#pragma shader_feature_local_fragment _ _ALPHAMODULATE_ON
#pragma shader_feature_local_fragment _ _ALPHATEST_ON
#pragma shader_feature_local_fragment _ _SPECULAR_SETUP
#pragma shader_feature_local _ _RECEIVE_SHADOWS_OFF
#pragma shader_feature_local _EDGEBLENDING_NORMALBLENDING _EDGEBLENDING_VERTEXBLENDING _EDGEBLENDING_NOISEBLENDING

#if defined(_EDGEBLENDING_NORMALBLENDING)
#define KEYWORD_PERMUTATION_0
#elif defined(_EDGEBLENDING_VERTEXBLENDING)
#define KEYWORD_PERMUTATION_1
#else
#define KEYWORD_PERMUTATION_2
#endif


// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_TEXCOORD1
#define ATTRIBUTES_NEED_TEXCOORD2
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_NORMAL_WS
#define VARYINGS_NEED_TANGENT_WS
#define VARYINGS_NEED_TEXCOORD0
#define VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
#define VARYINGS_NEED_SHADOW_COORD
#define VARYINGS_NEED_CULLFACE
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
 float3 ObjectSpaceNormal;
 float3 WorldSpaceNormal;
 float3 TangentSpaceNormal;
 float3 WorldSpaceTangent;
 float3 WorldSpaceBiTangent;
 float3 ObjectSpacePosition;
 float4 uv0;
 float FaceSign;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
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

float4 _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D_TexelSize;
float4 _ColorMap_TexelSize;
float4 _ColorMap_1_TexelSize;
float4 _NormalMap_TexelSize;
float4 _ColorTint;
float _NormalIntensity;
float _UV_Size;
float4 _ORMMap_TexelSize;
float _RoughnessIntensity;
float _AOIntensity;
float _MetallicIntensity;
float4 _EmissiveColor;
float _EmissiveIntensity_1;
float4 _Emissive_Map_TexelSize;
float _EmissiveIntensity;
float _EnableCoverage;
float _CoverageSize;
float _CoverageNormalIntensity;
float _CoverageRoughnessIntensity;
float _CoverageMetallicIntensity;
float _NoiseTextureSize;
float4 _SlopeNoiseTexture_TexelSize;
float4 _CoverageColorMap_TexelSize;
float4 _CoverageORMMap_TexelSize;
float4 _CoverageNormalMap_TexelSize;
float _SloapOffset;
float _SloapContrast;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D);
SAMPLER(sampler_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D);
TEXTURE2D(_ColorMap);
SAMPLER(sampler_ColorMap);
TEXTURE2D(_ColorMap_1);
SAMPLER(sampler_ColorMap_1);
TEXTURE2D(_NormalMap);
SAMPLER(sampler_NormalMap);
TEXTURE2D(_ORMMap);
SAMPLER(sampler_ORMMap);
TEXTURE2D(_Emissive_Map);
SAMPLER(sampler_Emissive_Map);
TEXTURE2D(_SlopeNoiseTexture);
SAMPLER(sampler_SlopeNoiseTexture);
TEXTURE2D(_CoverageColorMap);
SAMPLER(sampler_CoverageColorMap);
TEXTURE2D(_CoverageORMMap);
SAMPLER(sampler_CoverageORMMap);
TEXTURE2D(_CoverageNormalMap);
SAMPLER(sampler_CoverageNormalMap);

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

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

void Unity_NormalStrength_float(float3 In, float Strength, out float3 Out)
{
    Out = float3(In.rg * Strength, lerp(1, In.b, saturate(Strength)));
}

void Unity_Lerp_float(float A, float B, float T, out float Out)
{
    Out = lerp(A, B, T);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
}

struct Bindings_PASGORMUnpacker_abcd41e335c426a4bb0f8de17b4b5f96_float
{
};

void SG_PASGORMUnpacker_abcd41e335c426a4bb0f8de17b4b5f96_float(float2 _UV, UnityTexture2D _ORM_Map, float _AO_Intensity, float _Roughness_Intensity, float _Metallic_Intensity, Bindings_PASGORMUnpacker_abcd41e335c426a4bb0f8de17b4b5f96_float IN, out float Ambient_Occlusion_1, out float Roughness_2, out float Metallic_3)
{
float _Float_2c47c335e2304faa927a789be883bb19_Out_0_Float = float(1);
UnityTexture2D _Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D = _ORM_Map;
float2 _Property_ad3893fa315b474ea60e7ee3e265957c_Out_0_Vector2 = _UV;
float4 _SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D.tex, _Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D.samplerstate, _Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D.GetTransformedUV(_Property_ad3893fa315b474ea60e7ee3e265957c_Out_0_Vector2) );
if (_Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4, _Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_719055d2c0184958bd1329f59130cce0_R_4_Float = _SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4.r;
float _SampleTexture2D_719055d2c0184958bd1329f59130cce0_G_5_Float = _SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4.g;
float _SampleTexture2D_719055d2c0184958bd1329f59130cce0_B_6_Float = _SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4.b;
float _SampleTexture2D_719055d2c0184958bd1329f59130cce0_A_7_Float = _SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4.a;
float _Property_90898696aae3458aa180c6f15d96aaee_Out_0_Float = _AO_Intensity;
float _Lerp_bf670034cefc4c6d9e02b57f5394811d_Out_3_Float;
Unity_Lerp_float(_Float_2c47c335e2304faa927a789be883bb19_Out_0_Float, _SampleTexture2D_719055d2c0184958bd1329f59130cce0_R_4_Float, _Property_90898696aae3458aa180c6f15d96aaee_Out_0_Float, _Lerp_bf670034cefc4c6d9e02b57f5394811d_Out_3_Float);
float _Property_e77944e4743744198e931506300c92ca_Out_0_Float = _Roughness_Intensity;
float _Multiply_43e873dc525645aab065eb6fc56ad77f_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_719055d2c0184958bd1329f59130cce0_G_5_Float, _Property_e77944e4743744198e931506300c92ca_Out_0_Float, _Multiply_43e873dc525645aab065eb6fc56ad77f_Out_2_Float);
float _OneMinus_3225b57b02ed4eaf9c118419fbefc479_Out_1_Float;
Unity_OneMinus_float(_Multiply_43e873dc525645aab065eb6fc56ad77f_Out_2_Float, _OneMinus_3225b57b02ed4eaf9c118419fbefc479_Out_1_Float);
float _Property_c0c103f0dfcd475fa428b0e043905944_Out_0_Float = _Metallic_Intensity;
float _Multiply_38b5ca9e78ba40d9b861ffc15889bd7f_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_719055d2c0184958bd1329f59130cce0_B_6_Float, _Property_c0c103f0dfcd475fa428b0e043905944_Out_0_Float, _Multiply_38b5ca9e78ba40d9b861ffc15889bd7f_Out_2_Float);
Ambient_Occlusion_1 = _Lerp_bf670034cefc4c6d9e02b57f5394811d_Out_3_Float;
Roughness_2 = _OneMinus_3225b57b02ed4eaf9c118419fbefc479_Out_1_Float;
Metallic_3 = _Multiply_38b5ca9e78ba40d9b861ffc15889bd7f_Out_2_Float;
}

struct Bindings_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float
{
half4 uv0;
};

void SG_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float(float _Texture_Size, UnityTexture2D _Color_Map, float4 _Color_Tint, UnityTexture2D _Normal_Map, float _Normal_Intensity, UnityTexture2D _ORM_Map, float _Roughness_Intensity, float _AO_Intensity, float _Metallic_Intensity, UnityTexture2D _Emissive_Map, float4 _Emissive_Color, float _Emissive_Intensity, Bindings_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float IN, out float4 Base_Color_5, out float3 Normal_1, out float4 Emissive_6, out float Ambient_Occlusion_4, out float Roughness_3, out float Metallic_2)
{
UnityTexture2D _Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D = _Color_Map;
float _Property_6e866f25d28140918dd5249261b96e76_Out_0_Float = _Texture_Size;
float2 _TilingAndOffset_49c249d74ea54ccf979206ad2c3c0c00_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, (_Property_6e866f25d28140918dd5249261b96e76_Out_0_Float.xx), float2 (0, 0), _TilingAndOffset_49c249d74ea54ccf979206ad2c3c0c00_Out_3_Vector2);
float _Property_94852b2b32f549138996f0dfb35b0486_Out_0_Float = _Texture_Size;
float2 _Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2;
Unity_Multiply_float2_float2(_TilingAndOffset_49c249d74ea54ccf979206ad2c3c0c00_Out_3_Vector2, (_Property_94852b2b32f549138996f0dfb35b0486_Out_0_Float.xx), _Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2);
float4 _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D.tex, _Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D.samplerstate, _Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D.GetTransformedUV(_Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2) );
if (_Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4, _Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_R_4_Float = _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4.r;
float _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_G_5_Float = _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4.g;
float _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_B_6_Float = _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4.b;
float _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_A_7_Float = _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4.a;
float4 _Property_c081d1852d284e4fa4bbb04ea3adf35e_Out_0_Vector4 = _Color_Tint;
float4 _Multiply_ff3b38102028498db7cc80acf5e42362_Out_2_Vector4;
Unity_Multiply_float4_float4(_SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4, _Property_c081d1852d284e4fa4bbb04ea3adf35e_Out_0_Vector4, _Multiply_ff3b38102028498db7cc80acf5e42362_Out_2_Vector4);
UnityTexture2D _Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D = _Normal_Map;
float4 _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D.tex, _Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D.samplerstate, _Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D.GetTransformedUV(_Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2) );
if (_Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4, _Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4);
float _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_R_4_Float = _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.r;
float _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_G_5_Float = _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.g;
float _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_B_6_Float = _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.b;
float _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_A_7_Float = _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.a;
float _Property_8b1f839dfbcd4481b201427fbc7310ad_Out_0_Float = _Normal_Intensity;
float3 _NormalStrength_a218ef03cc884bf0b0962d85b628afe7_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.xyz), _Property_8b1f839dfbcd4481b201427fbc7310ad_Out_0_Float, _NormalStrength_a218ef03cc884bf0b0962d85b628afe7_Out_2_Vector3);
UnityTexture2D _Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D = _Emissive_Map;
float4 _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D.tex, _Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D.samplerstate, _Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D.GetTransformedUV(_Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2) );
if (_Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4, _Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_R_4_Float = _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4.r;
float _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_G_5_Float = _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4.g;
float _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_B_6_Float = _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4.b;
float _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_A_7_Float = _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4.a;
float4 _Property_3a2f8dbe365d427e89744f8eb8dabd44_Out_0_Vector4 = _Emissive_Color;
float4 _Multiply_1efa7dc8f3994d8ea213351379d48448_Out_2_Vector4;
Unity_Multiply_float4_float4(_SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4, _Property_3a2f8dbe365d427e89744f8eb8dabd44_Out_0_Vector4, _Multiply_1efa7dc8f3994d8ea213351379d48448_Out_2_Vector4);
float _Property_09c3fe7e22be43caa45c324f52ea8137_Out_0_Float = _Emissive_Intensity;
float4 _Multiply_43a1c0123fb34a25a5d21581f08926f8_Out_2_Vector4;
Unity_Multiply_float4_float4(_Multiply_1efa7dc8f3994d8ea213351379d48448_Out_2_Vector4, (_Property_09c3fe7e22be43caa45c324f52ea8137_Out_0_Float.xxxx), _Multiply_43a1c0123fb34a25a5d21581f08926f8_Out_2_Vector4);
UnityTexture2D _Property_ecbd8a4e938a458cabefe2480cada231_Out_0_Texture2D = _ORM_Map;
float _Property_2ca0dc7be16d47bfb0faf210c4d70757_Out_0_Float = _AO_Intensity;
float _Property_fb245dde136f4b2486e260e720f83c83_Out_0_Float = _Roughness_Intensity;
float _Property_0fcde0f68f364243886b868db18a8d27_Out_0_Float = _Metallic_Intensity;
Bindings_PASGORMUnpacker_abcd41e335c426a4bb0f8de17b4b5f96_float _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94;
half _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_AmbientOcclusion_1_Float;
half _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Roughness_2_Float;
half _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Metallic_3_Float;
SG_PASGORMUnpacker_abcd41e335c426a4bb0f8de17b4b5f96_float(_Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2, _Property_ecbd8a4e938a458cabefe2480cada231_Out_0_Texture2D, _Property_2ca0dc7be16d47bfb0faf210c4d70757_Out_0_Float, _Property_fb245dde136f4b2486e260e720f83c83_Out_0_Float, _Property_0fcde0f68f364243886b868db18a8d27_Out_0_Float, _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94, _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_AmbientOcclusion_1_Float, _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Roughness_2_Float, _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Metallic_3_Float);
Base_Color_5 = _Multiply_ff3b38102028498db7cc80acf5e42362_Out_2_Vector4;
Normal_1 = _NormalStrength_a218ef03cc884bf0b0962d85b628afe7_Out_2_Vector3;
Emissive_6 = _Multiply_43a1c0123fb34a25a5d21581f08926f8_Out_2_Vector4;
Ambient_Occlusion_4 = _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_AmbientOcclusion_1_Float;
Roughness_3 = _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Roughness_2_Float;
Metallic_2 = _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Metallic_3_Float;
}

void Unity_DotProduct_float3(float3 A, float3 B, out float Out)
{
    Out = dot(A, B);
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Clamp_float(float In, float Min, float Max, out float Out)
{
    Out = clamp(In, Min, Max);
}

void Unity_Contrast_float(float3 In, float Contrast, out float3 Out)
{
    float midpoint = pow(0.5, 2.2);
    Out =  (In - midpoint) * Contrast + midpoint;
}

void Unity_Clamp_float3(float3 In, float3 Min, float3 Max, out float3 Out)
{
    Out = clamp(In, Min, Max);
}

struct Bindings_PASGVerticalBlend_e9f0b2d5820244846982772499370842_float
{
float3 ObjectSpaceNormal;
float3 WorldSpaceNormal;
float3 WorldSpaceTangent;
float3 WorldSpaceBiTangent;
float3 ObjectSpacePosition;
};

void SG_PASGVerticalBlend_e9f0b2d5820244846982772499370842_float(float _Sloap_Contrast, UnityTexture2D _Slope_Noise_Texture, float _Noise_Texture_Size, float3 _Explicit_Normal, float _Sloap_Offset, Bindings_PASGVerticalBlend_e9f0b2d5820244846982772499370842_float IN, out float3 OutVector3_1)
{
float3 _Property_5509c7fb30e445229f496a71c6e9c192_Out_0_Vector3 = _Explicit_Normal;
float3 _Transform_9c9f921cd0934012ac0d281c8e40a727_Out_1_Vector3;
{
float3x3 tangentTransform = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
_Transform_9c9f921cd0934012ac0d281c8e40a727_Out_1_Vector3 = TransformTangentToWorld(_Property_5509c7fb30e445229f496a71c6e9c192_Out_0_Vector3.xyz, tangentTransform, true);
}
float _DotProduct_4bc69611266041259c8da1b387474ec9_Out_2_Float;
Unity_DotProduct_float3(_Transform_9c9f921cd0934012ac0d281c8e40a727_Out_1_Vector3, float3(0, 1, 0), _DotProduct_4bc69611266041259c8da1b387474ec9_Out_2_Float);
float _Property_6a995c4e80824d6fa7d458ddcdc4eec3_Out_0_Float = _Sloap_Offset;
float _Add_a196acc146d1466fb309ec050e77feb2_Out_2_Float;
Unity_Add_float(_DotProduct_4bc69611266041259c8da1b387474ec9_Out_2_Float, _Property_6a995c4e80824d6fa7d458ddcdc4eec3_Out_0_Float, _Add_a196acc146d1466fb309ec050e77feb2_Out_2_Float);
float _Lerp_a1fed41c0847449ab72ae8fe4775270c_Out_3_Float;
Unity_Lerp_float(float(0), float(3), _Add_a196acc146d1466fb309ec050e77feb2_Out_2_Float, _Lerp_a1fed41c0847449ab72ae8fe4775270c_Out_3_Float);
float _Clamp_d4d6e762b7b1422d936b98d7f9429754_Out_3_Float;
Unity_Clamp_float(_Lerp_a1fed41c0847449ab72ae8fe4775270c_Out_3_Float, float(0), float(1), _Clamp_d4d6e762b7b1422d936b98d7f9429754_Out_3_Float);
float _Property_985b04ddd40847029dbfa829fbcaeed1_Out_0_Float = _Sloap_Contrast;
float3 _Contrast_2864897542064adcb4b61fcccadaf026_Out_2_Vector3;
Unity_Contrast_float((_Clamp_d4d6e762b7b1422d936b98d7f9429754_Out_3_Float.xxx), _Property_985b04ddd40847029dbfa829fbcaeed1_Out_0_Float, _Contrast_2864897542064adcb4b61fcccadaf026_Out_2_Vector3);
float3 _Clamp_b8f1f82d045f437a910a54b62bb2df81_Out_3_Vector3;
Unity_Clamp_float3(_Contrast_2864897542064adcb4b61fcccadaf026_Out_2_Vector3, float3(0, 0, 0), float3(1, 1, 1), _Clamp_b8f1f82d045f437a910a54b62bb2df81_Out_3_Vector3);
float3 _Transform_229ffe25002343a39c8caaa3d7f6df91_Out_1_Vector3;
_Transform_229ffe25002343a39c8caaa3d7f6df91_Out_1_Vector3 = TransformObjectToWorldDir(IN.ObjectSpacePosition.xyz, true);
float3 _Vector3_ec86905b34c54695bed5b7d1975cb6ce_Out_0_Vector3 = float3(float(0), float(2), float(0));
float _DotProduct_af3cb3caf6e4473c8161868b15c0255b_Out_2_Float;
Unity_DotProduct_float3(_Transform_229ffe25002343a39c8caaa3d7f6df91_Out_1_Vector3, _Vector3_ec86905b34c54695bed5b7d1975cb6ce_Out_0_Vector3, _DotProduct_af3cb3caf6e4473c8161868b15c0255b_Out_2_Float);
float _Add_47be9b48b4c94dd28e8c3db7f6ba1a05_Out_2_Float;
Unity_Add_float(_Property_6a995c4e80824d6fa7d458ddcdc4eec3_Out_0_Float, _DotProduct_af3cb3caf6e4473c8161868b15c0255b_Out_2_Float, _Add_47be9b48b4c94dd28e8c3db7f6ba1a05_Out_2_Float);
float _Multiply_35491cd334cb4e34a76b48de2bc5814a_Out_2_Float;
Unity_Multiply_float_float(_Add_47be9b48b4c94dd28e8c3db7f6ba1a05_Out_2_Float, 2, _Multiply_35491cd334cb4e34a76b48de2bc5814a_Out_2_Float);
float _Clamp_ccd0969e9da6420f94e7113654fd480e_Out_3_Float;
Unity_Clamp_float(_Multiply_35491cd334cb4e34a76b48de2bc5814a_Out_2_Float, float(0), float(1), _Clamp_ccd0969e9da6420f94e7113654fd480e_Out_3_Float);
float3 _Contrast_454ba7c8fb3a4d03951b073867fa5f14_Out_2_Vector3;
Unity_Contrast_float((_Clamp_ccd0969e9da6420f94e7113654fd480e_Out_3_Float.xxx), _Property_985b04ddd40847029dbfa829fbcaeed1_Out_0_Float, _Contrast_454ba7c8fb3a4d03951b073867fa5f14_Out_2_Vector3);
float3 _Clamp_1ba5cf0aee8245bd9dca155954c3df81_Out_3_Vector3;
Unity_Clamp_float3(_Contrast_454ba7c8fb3a4d03951b073867fa5f14_Out_2_Vector3, float3(0, 0, 0), float3(1, 1, 1), _Clamp_1ba5cf0aee8245bd9dca155954c3df81_Out_3_Vector3);
UnityTexture2D _Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D = _Slope_Noise_Texture;
float _Property_8e663ddd1476448d948a9de1bf1f83a9_Out_0_Float = _Noise_Texture_Size;
float3 Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_UV = IN.ObjectSpacePosition * _Property_8e663ddd1476448d948a9de1bf1f83a9_Out_0_Float;
float3 Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend = SafePositivePow_float(IN.ObjectSpaceNormal, min(float(1), floor(log2(Min_float())/log2(1/sqrt(3)))) );
Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend /= dot(Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend, 1.0);
float4 Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_X = SAMPLE_TEXTURE2D(_Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.tex, _Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.samplerstate, Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_UV.zy);
float4 Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Y = SAMPLE_TEXTURE2D(_Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.tex, _Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.samplerstate, Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_UV.xz);
float4 Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Z = SAMPLE_TEXTURE2D(_Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.tex, _Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.samplerstate, Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_UV.xy);
float4 _Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Out_0_Vector4 = Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_X * Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend.x + Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Y * Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend.y + Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Z * Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend.z;
float _Swizzle_90fb1902a2bd4070b4f9e27ac287ca97_Out_1_Float = (_Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Out_0_Vector4.xy).y;
float _Clamp_f32c97c6ccbf44649bd757503ccdbe71_Out_3_Float;
Unity_Clamp_float(_Swizzle_90fb1902a2bd4070b4f9e27ac287ca97_Out_1_Float, float(0), float(1), _Clamp_f32c97c6ccbf44649bd757503ccdbe71_Out_3_Float);
float _Add_92d15430b3c042719d65f1252bc35d7a_Out_2_Float;
Unity_Add_float(_Multiply_35491cd334cb4e34a76b48de2bc5814a_Out_2_Float, _Clamp_f32c97c6ccbf44649bd757503ccdbe71_Out_3_Float, _Add_92d15430b3c042719d65f1252bc35d7a_Out_2_Float);
float3 _Contrast_c2fe106c51f0498e987cf31090ec563d_Out_2_Vector3;
Unity_Contrast_float((_Add_92d15430b3c042719d65f1252bc35d7a_Out_2_Float.xxx), _Property_985b04ddd40847029dbfa829fbcaeed1_Out_0_Float, _Contrast_c2fe106c51f0498e987cf31090ec563d_Out_2_Vector3);
float3 _Clamp_05438405442c47cb98e571d128277bbe_Out_3_Vector3;
Unity_Clamp_float3(_Contrast_c2fe106c51f0498e987cf31090ec563d_Out_2_Vector3, float3(0, 0, 0), float3(1, 1, 1), _Clamp_05438405442c47cb98e571d128277bbe_Out_3_Vector3);
#if defined(_EDGEBLENDING_NORMALBLENDING)
float3 _EdgeBlending_cdbe33a15cf6452b8881ecea22adcb84_Out_0_Vector3 = _Clamp_b8f1f82d045f437a910a54b62bb2df81_Out_3_Vector3;
#elif defined(_EDGEBLENDING_VERTEXBLENDING)
float3 _EdgeBlending_cdbe33a15cf6452b8881ecea22adcb84_Out_0_Vector3 = _Clamp_1ba5cf0aee8245bd9dca155954c3df81_Out_3_Vector3;
#else
float3 _EdgeBlending_cdbe33a15cf6452b8881ecea22adcb84_Out_0_Vector3 = _Clamp_05438405442c47cb98e571d128277bbe_Out_3_Vector3;
#endif
OutVector3_1 = _EdgeBlending_cdbe33a15cf6452b8881ecea22adcb84_Out_0_Vector3;
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Branch_float3(float Predicate, float3 True, float3 False, out float3 Out)
{
    Out = Predicate ? True : False;
}

void Unity_Negate_float3(float3 In, out float3 Out)
{
    Out = -1 * In;
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
description.Position = IN.ObjectSpacePosition;
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
float3 Specular;
float Smoothness;
float Occlusion;
float Alpha;
float AlphaClipThreshold;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
float _Property_868c674f556d42be9071980dc60f4aed_Out_0_Boolean = _EnableCoverage;
float _Property_a0b21575a96c42a2b05aa3518645b2f4_Out_0_Float = _UV_Size;
UnityTexture2D _Property_4d248f4a9de94d7bb9a5c3c7c4c10a68_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_ColorMap, sampler_ColorMap, _ColorMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _Property_061dd20995c54e4b8c0357aa709c6e56_Out_0_Vector4 = _ColorTint;
UnityTexture2D _Property_cc904da6b4714cd6a9559b3bcf6ba7a6_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_NormalMap, sampler_NormalMap, _NormalMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_49f412000c0b4f18863fb7afae8b43fc_Out_0_Float = _NormalIntensity;
UnityTexture2D _Property_3fcfe917c75d4a46bfe50da31a24c74f_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_ORMMap, sampler_ORMMap, _ORMMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_bd35b765c62349f285447de81728f0c8_Out_0_Float = _RoughnessIntensity;
float _Property_f125a72836c24b0fa5ef72823c8e6452_Out_0_Float = _AOIntensity;
float _Property_0251a196075947af9d3ad28d6875a5ef_Out_0_Float = _MetallicIntensity;
UnityTexture2D _Property_47ecc092fbd144218fe6440250e367b3_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Emissive_Map, sampler_Emissive_Map, _Emissive_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _Property_c065f0d4df8c40528116ee8e516b2221_Out_0_Vector4 = _EmissiveColor;
float _Property_0038e41f028740e4abc1958ca58302a1_Out_0_Float = _EmissiveIntensity;
Bindings_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe;
_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe.uv0 = IN.uv0;
float4 _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_BaseColor_5_Vector4;
float3 _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Normal_1_Vector3;
float4 _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Emissive_6_Vector4;
float _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_AmbientOcclusion_4_Float;
float _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Roughness_3_Float;
float _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Metallic_2_Float;
SG_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float(_Property_a0b21575a96c42a2b05aa3518645b2f4_Out_0_Float, _Property_4d248f4a9de94d7bb9a5c3c7c4c10a68_Out_0_Texture2D, _Property_061dd20995c54e4b8c0357aa709c6e56_Out_0_Vector4, _Property_cc904da6b4714cd6a9559b3bcf6ba7a6_Out_0_Texture2D, _Property_49f412000c0b4f18863fb7afae8b43fc_Out_0_Float, _Property_3fcfe917c75d4a46bfe50da31a24c74f_Out_0_Texture2D, _Property_bd35b765c62349f285447de81728f0c8_Out_0_Float, _Property_f125a72836c24b0fa5ef72823c8e6452_Out_0_Float, _Property_0251a196075947af9d3ad28d6875a5ef_Out_0_Float, _Property_47ecc092fbd144218fe6440250e367b3_Out_0_Texture2D, _Property_c065f0d4df8c40528116ee8e516b2221_Out_0_Vector4, _Property_0038e41f028740e4abc1958ca58302a1_Out_0_Float, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_BaseColor_5_Vector4, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Normal_1_Vector3, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Emissive_6_Vector4, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_AmbientOcclusion_4_Float, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Roughness_3_Float, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Metallic_2_Float);
float _Property_623a0c9d694c4c33b6256bde952c53e5_Out_0_Float = _CoverageSize;
UnityTexture2D _Property_58d380b2034145c8b7d2eda637dbc296_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_CoverageColorMap, sampler_CoverageColorMap, _CoverageColorMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
UnityTexture2D _Property_f324dd6628ea45b1a630d750fa3d6b66_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_CoverageNormalMap, sampler_CoverageNormalMap, _CoverageNormalMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_b00b34515282450295cc4f4eb7b4a0ce_Out_0_Float = _CoverageNormalIntensity;
UnityTexture2D _Property_24ae6469bfeb4dada6fb7c7d39985cb3_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_CoverageORMMap, sampler_CoverageORMMap, _CoverageORMMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_03c29a27092f4bbb8a4fcc3b6e7b2909_Out_0_Float = _CoverageRoughnessIntensity;
float _Property_07cad64e572e4602a346b7dd90c3eb21_Out_0_Float = _CoverageMetallicIntensity;
Bindings_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c;
_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c.uv0 = IN.uv0;
float4 _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_BaseColor_5_Vector4;
float3 _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Normal_1_Vector3;
float4 _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Emissive_6_Vector4;
float _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_AmbientOcclusion_4_Float;
float _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Roughness_3_Float;
float _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Metallic_2_Float;
SG_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float(_Property_623a0c9d694c4c33b6256bde952c53e5_Out_0_Float, _Property_58d380b2034145c8b7d2eda637dbc296_Out_0_Texture2D, float4 (1, 1, 1, 1), _Property_f324dd6628ea45b1a630d750fa3d6b66_Out_0_Texture2D, _Property_b00b34515282450295cc4f4eb7b4a0ce_Out_0_Float, _Property_24ae6469bfeb4dada6fb7c7d39985cb3_Out_0_Texture2D, _Property_03c29a27092f4bbb8a4fcc3b6e7b2909_Out_0_Float, float(0), _Property_07cad64e572e4602a346b7dd90c3eb21_Out_0_Float, UnityBuildTexture2DStructNoScale(_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D), float4 (0, 0, 0, 0), float(0), _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_BaseColor_5_Vector4, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Normal_1_Vector3, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Emissive_6_Vector4, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_AmbientOcclusion_4_Float, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Roughness_3_Float, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Metallic_2_Float);
float _Property_0785fc58d71d4d63abb8c1102aa1a1b7_Out_0_Float = _SloapContrast;
UnityTexture2D _Property_12921d1841154c59b11aefccb2cbca31_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_SlopeNoiseTexture, sampler_SlopeNoiseTexture, _SlopeNoiseTexture_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_10dd46128137476fb3fd49de6154e94c_Out_0_Float = _NoiseTextureSize;
float _Property_a654ef8b1e2a4529ab82f92b3ae202ec_Out_0_Float = _SloapOffset;
Bindings_PASGVerticalBlend_e9f0b2d5820244846982772499370842_float _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d;
_PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d.ObjectSpaceNormal = IN.ObjectSpaceNormal;
_PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d.WorldSpaceNormal = IN.WorldSpaceNormal;
_PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d.WorldSpaceTangent = IN.WorldSpaceTangent;
_PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d.WorldSpaceBiTangent = IN.WorldSpaceBiTangent;
_PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d.ObjectSpacePosition = IN.ObjectSpacePosition;
float3 _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d_OutVector3_1_Vector3;
SG_PASGVerticalBlend_e9f0b2d5820244846982772499370842_float(_Property_0785fc58d71d4d63abb8c1102aa1a1b7_Out_0_Float, _Property_12921d1841154c59b11aefccb2cbca31_Out_0_Texture2D, _Property_10dd46128137476fb3fd49de6154e94c_Out_0_Float, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Normal_1_Vector3, _Property_a654ef8b1e2a4529ab82f92b3ae202ec_Out_0_Float, _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d, _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d_OutVector3_1_Vector3);
float3 _Lerp_8ef5a2e39e934fb09e1bc6858ba97959_Out_3_Vector3;
Unity_Lerp_float3((_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_BaseColor_5_Vector4.xyz), (_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_BaseColor_5_Vector4.xyz), _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d_OutVector3_1_Vector3, _Lerp_8ef5a2e39e934fb09e1bc6858ba97959_Out_3_Vector3);
float3 _Branch_72adc4bf3fd64427a9a9035afa6f856a_Out_3_Vector3;
Unity_Branch_float3(_Property_868c674f556d42be9071980dc60f4aed_Out_0_Boolean, _Lerp_8ef5a2e39e934fb09e1bc6858ba97959_Out_3_Vector3, (_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_BaseColor_5_Vector4.xyz), _Branch_72adc4bf3fd64427a9a9035afa6f856a_Out_3_Vector3);
float _IsFrontFace_8b8f9781e7a946f18c8c49da20d0925c_Out_0_Boolean = max(0, IN.FaceSign.x);
float3 _Lerp_e69be27d4f0d4e26a37c622c629ca56d_Out_3_Vector3;
Unity_Lerp_float3(_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Normal_1_Vector3, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Normal_1_Vector3, _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d_OutVector3_1_Vector3, _Lerp_e69be27d4f0d4e26a37c622c629ca56d_Out_3_Vector3);
float3 _Branch_3213119f26d54fc08b755c491d9dd6c4_Out_3_Vector3;
Unity_Branch_float3(_Property_868c674f556d42be9071980dc60f4aed_Out_0_Boolean, _Lerp_e69be27d4f0d4e26a37c622c629ca56d_Out_3_Vector3, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Normal_1_Vector3, _Branch_3213119f26d54fc08b755c491d9dd6c4_Out_3_Vector3);
float3 _Negate_c9035ab4407c48978c2cd379c7ed3db2_Out_1_Vector3;
Unity_Negate_float3(_Branch_3213119f26d54fc08b755c491d9dd6c4_Out_3_Vector3, _Negate_c9035ab4407c48978c2cd379c7ed3db2_Out_1_Vector3);
float3 _Branch_6a68e6f7151b40c684e82041ff10474e_Out_3_Vector3;
Unity_Branch_float3(_IsFrontFace_8b8f9781e7a946f18c8c49da20d0925c_Out_0_Boolean, _Branch_3213119f26d54fc08b755c491d9dd6c4_Out_3_Vector3, _Negate_c9035ab4407c48978c2cd379c7ed3db2_Out_1_Vector3, _Branch_6a68e6f7151b40c684e82041ff10474e_Out_3_Vector3);
float3 _Lerp_e6a3b112a78740919a21e1a67f8a9012_Out_3_Vector3;
Unity_Lerp_float3((_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Roughness_3_Float.xxx), (_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Roughness_3_Float.xxx), _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d_OutVector3_1_Vector3, _Lerp_e6a3b112a78740919a21e1a67f8a9012_Out_3_Vector3);
float3 _Branch_e3527157b2634e17853db32690e6cf9f_Out_3_Vector3;
Unity_Branch_float3(_Property_868c674f556d42be9071980dc60f4aed_Out_0_Boolean, _Lerp_e6a3b112a78740919a21e1a67f8a9012_Out_3_Vector3, (_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Metallic_2_Float.xxx), _Branch_e3527157b2634e17853db32690e6cf9f_Out_3_Vector3);
float3 _Lerp_0bf96dce18b642f4ab3b848bb1fe45d6_Out_3_Vector3;
Unity_Lerp_float3((_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Metallic_2_Float.xxx), (_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Metallic_2_Float.xxx), _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d_OutVector3_1_Vector3, _Lerp_0bf96dce18b642f4ab3b848bb1fe45d6_Out_3_Vector3);
float3 _Branch_bcdc5fe84435477ab329331520e86c7d_Out_3_Vector3;
Unity_Branch_float3(_Property_868c674f556d42be9071980dc60f4aed_Out_0_Boolean, _Lerp_0bf96dce18b642f4ab3b848bb1fe45d6_Out_3_Vector3, (_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Roughness_3_Float.xxx), _Branch_bcdc5fe84435477ab329331520e86c7d_Out_3_Vector3);
float3 _Lerp_1ffb4b0a9a70465db54818dd6df0be7c_Out_3_Vector3;
Unity_Lerp_float3((_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_AmbientOcclusion_4_Float.xxx), (_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_AmbientOcclusion_4_Float.xxx), _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d_OutVector3_1_Vector3, _Lerp_1ffb4b0a9a70465db54818dd6df0be7c_Out_3_Vector3);
float3 _Branch_c19f53b6fc08465e849902625a3db36e_Out_3_Vector3;
Unity_Branch_float3(_Property_868c674f556d42be9071980dc60f4aed_Out_0_Boolean, _Lerp_1ffb4b0a9a70465db54818dd6df0be7c_Out_3_Vector3, (_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_AmbientOcclusion_4_Float.xxx), _Branch_c19f53b6fc08465e849902625a3db36e_Out_3_Vector3);
UnityTexture2D _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_ColorMap_1, sampler_ColorMap_1, _ColorMap_1_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.tex, _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.samplerstate, _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4, _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_R_4_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.r;
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_G_5_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.g;
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_B_6_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.b;
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_A_7_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.a;
float _Property_7a43d5f1d3df46c2869bc70eebee1f91_Out_0_Float = _EmissiveIntensity_1;
surface.BaseColor = _Branch_72adc4bf3fd64427a9a9035afa6f856a_Out_3_Vector3;
surface.NormalTS = _Branch_6a68e6f7151b40c684e82041ff10474e_Out_3_Vector3;
surface.Emission = (_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Emissive_6_Vector4.xyz);
surface.Metallic = (_Branch_e3527157b2634e17853db32690e6cf9f_Out_3_Vector3).x;
surface.Specular = IsGammaSpace() ? float3(0.5, 0.5, 0.5) : SRGBToLinear(float3(0.5, 0.5, 0.5));
surface.Smoothness = (_Branch_bcdc5fe84435477ab329331520e86c7d_Out_3_Vector3).x;
surface.Occlusion = (_Branch_c19f53b6fc08465e849902625a3db36e_Out_3_Vector3).x;
surface.Alpha = (_SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4).x;
surface.AlphaClipThreshold = _Property_7a43d5f1d3df46c2869bc70eebee1f91_Out_0_Float;
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

    // use bitangent on the fly like in hdrp
    // IMPORTANT! If we ever support Flip on double sided materials ensure bitangent and tangent are NOT flipped.
    float crossSign = (input.tangentWS.w > 0.0 ? 1.0 : -1.0)* GetOddNegativeScale();
    float3 bitang = crossSign * cross(input.normalWS.xyz, input.tangentWS.xyz);

    output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph
    output.ObjectSpaceNormal = normalize(mul(output.WorldSpaceNormal, (float3x3) UNITY_MATRIX_M));           // transposed multiplication by inverse matrix to handle normal scale
    output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);

    // to pr               eserve mikktspace compliance we use same scale renormFactor as was used on the normal.
    // This                is explained in section 2.2 in "surface gradient based bump mapping framework"
    output.WorldSpaceTangent = renormFactor * input.tangentWS.xyz;
    output.WorldSpaceBiTangent = renormFactor * bitang;

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
    BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
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
Cull [_Cull]
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
#pragma shader_feature_local_fragment _ _ALPHATEST_ON
#pragma shader_feature_local _EDGEBLENDING_NORMALBLENDING _EDGEBLENDING_VERTEXBLENDING _EDGEBLENDING_NOISEBLENDING

#if defined(_EDGEBLENDING_NORMALBLENDING)
#define KEYWORD_PERMUTATION_0
#elif defined(_EDGEBLENDING_VERTEXBLENDING)
#define KEYWORD_PERMUTATION_1
#else
#define KEYWORD_PERMUTATION_2
#endif


// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#define VARYINGS_NEED_NORMAL_WS
#define VARYINGS_NEED_TEXCOORD0
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
 float4 uv0 : TEXCOORD0;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
 float4 positionCS : SV_POSITION;
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
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
 float4 texCoord0 : INTERP0;
 float3 normalWS : INTERP1;
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

float4 _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D_TexelSize;
float4 _ColorMap_TexelSize;
float4 _ColorMap_1_TexelSize;
float4 _NormalMap_TexelSize;
float4 _ColorTint;
float _NormalIntensity;
float _UV_Size;
float4 _ORMMap_TexelSize;
float _RoughnessIntensity;
float _AOIntensity;
float _MetallicIntensity;
float4 _EmissiveColor;
float _EmissiveIntensity_1;
float4 _Emissive_Map_TexelSize;
float _EmissiveIntensity;
float _EnableCoverage;
float _CoverageSize;
float _CoverageNormalIntensity;
float _CoverageRoughnessIntensity;
float _CoverageMetallicIntensity;
float _NoiseTextureSize;
float4 _SlopeNoiseTexture_TexelSize;
float4 _CoverageColorMap_TexelSize;
float4 _CoverageORMMap_TexelSize;
float4 _CoverageNormalMap_TexelSize;
float _SloapOffset;
float _SloapContrast;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D);
SAMPLER(sampler_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D);
TEXTURE2D(_ColorMap);
SAMPLER(sampler_ColorMap);
TEXTURE2D(_ColorMap_1);
SAMPLER(sampler_ColorMap_1);
TEXTURE2D(_NormalMap);
SAMPLER(sampler_NormalMap);
TEXTURE2D(_ORMMap);
SAMPLER(sampler_ORMMap);
TEXTURE2D(_Emissive_Map);
SAMPLER(sampler_Emissive_Map);
TEXTURE2D(_SlopeNoiseTexture);
SAMPLER(sampler_SlopeNoiseTexture);
TEXTURE2D(_CoverageColorMap);
SAMPLER(sampler_CoverageColorMap);
TEXTURE2D(_CoverageORMMap);
SAMPLER(sampler_CoverageORMMap);
TEXTURE2D(_CoverageNormalMap);
SAMPLER(sampler_CoverageNormalMap);

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
// GraphFunctions: <None>

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
description.Position = IN.ObjectSpacePosition;
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
float Alpha;
float AlphaClipThreshold;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
UnityTexture2D _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_ColorMap_1, sampler_ColorMap_1, _ColorMap_1_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.tex, _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.samplerstate, _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4, _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_R_4_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.r;
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_G_5_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.g;
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_B_6_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.b;
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_A_7_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.a;
float _Property_7a43d5f1d3df46c2869bc70eebee1f91_Out_0_Float = _EmissiveIntensity_1;
surface.Alpha = (_SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4).x;
surface.AlphaClipThreshold = _Property_7a43d5f1d3df46c2869bc70eebee1f91_Out_0_Float;
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
Cull [_Cull]
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
#pragma shader_feature_local_fragment _ _ALPHATEST_ON
#pragma shader_feature_local _EDGEBLENDING_NORMALBLENDING _EDGEBLENDING_VERTEXBLENDING _EDGEBLENDING_NOISEBLENDING

#if defined(_EDGEBLENDING_NORMALBLENDING)
#define KEYWORD_PERMUTATION_0
#elif defined(_EDGEBLENDING_VERTEXBLENDING)
#define KEYWORD_PERMUTATION_1
#else
#define KEYWORD_PERMUTATION_2
#endif


// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_TEXCOORD0
#define VARYINGS_NEED_TEXCOORD0
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
 float4 uv0 : TEXCOORD0;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
 float4 positionCS : SV_POSITION;
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
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpacePosition;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
 float4 texCoord0 : INTERP0;
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

float4 _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D_TexelSize;
float4 _ColorMap_TexelSize;
float4 _ColorMap_1_TexelSize;
float4 _NormalMap_TexelSize;
float4 _ColorTint;
float _NormalIntensity;
float _UV_Size;
float4 _ORMMap_TexelSize;
float _RoughnessIntensity;
float _AOIntensity;
float _MetallicIntensity;
float4 _EmissiveColor;
float _EmissiveIntensity_1;
float4 _Emissive_Map_TexelSize;
float _EmissiveIntensity;
float _EnableCoverage;
float _CoverageSize;
float _CoverageNormalIntensity;
float _CoverageRoughnessIntensity;
float _CoverageMetallicIntensity;
float _NoiseTextureSize;
float4 _SlopeNoiseTexture_TexelSize;
float4 _CoverageColorMap_TexelSize;
float4 _CoverageORMMap_TexelSize;
float4 _CoverageNormalMap_TexelSize;
float _SloapOffset;
float _SloapContrast;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D);
SAMPLER(sampler_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D);
TEXTURE2D(_ColorMap);
SAMPLER(sampler_ColorMap);
TEXTURE2D(_ColorMap_1);
SAMPLER(sampler_ColorMap_1);
TEXTURE2D(_NormalMap);
SAMPLER(sampler_NormalMap);
TEXTURE2D(_ORMMap);
SAMPLER(sampler_ORMMap);
TEXTURE2D(_Emissive_Map);
SAMPLER(sampler_Emissive_Map);
TEXTURE2D(_SlopeNoiseTexture);
SAMPLER(sampler_SlopeNoiseTexture);
TEXTURE2D(_CoverageColorMap);
SAMPLER(sampler_CoverageColorMap);
TEXTURE2D(_CoverageORMMap);
SAMPLER(sampler_CoverageORMMap);
TEXTURE2D(_CoverageNormalMap);
SAMPLER(sampler_CoverageNormalMap);

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
// GraphFunctions: <None>

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
description.Position = IN.ObjectSpacePosition;
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
float Alpha;
float AlphaClipThreshold;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
UnityTexture2D _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_ColorMap_1, sampler_ColorMap_1, _ColorMap_1_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.tex, _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.samplerstate, _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4, _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_R_4_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.r;
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_G_5_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.g;
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_B_6_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.b;
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_A_7_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.a;
float _Property_7a43d5f1d3df46c2869bc70eebee1f91_Out_0_Float = _EmissiveIntensity_1;
surface.Alpha = (_SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4).x;
surface.AlphaClipThreshold = _Property_7a43d5f1d3df46c2869bc70eebee1f91_Out_0_Float;
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
Cull [_Cull]
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
#pragma shader_feature_local_fragment _ _ALPHATEST_ON
#pragma shader_feature_local _EDGEBLENDING_NORMALBLENDING _EDGEBLENDING_VERTEXBLENDING _EDGEBLENDING_NOISEBLENDING

#if defined(_EDGEBLENDING_NORMALBLENDING)
#define KEYWORD_PERMUTATION_0
#elif defined(_EDGEBLENDING_VERTEXBLENDING)
#define KEYWORD_PERMUTATION_1
#else
#define KEYWORD_PERMUTATION_2
#endif


// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#define VARYINGS_NEED_TEXCOORD0
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
 float4 uv0 : TEXCOORD0;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
 float4 positionCS : SV_POSITION;
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
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
 float4 texCoord0 : INTERP0;
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

float4 _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D_TexelSize;
float4 _ColorMap_TexelSize;
float4 _ColorMap_1_TexelSize;
float4 _NormalMap_TexelSize;
float4 _ColorTint;
float _NormalIntensity;
float _UV_Size;
float4 _ORMMap_TexelSize;
float _RoughnessIntensity;
float _AOIntensity;
float _MetallicIntensity;
float4 _EmissiveColor;
float _EmissiveIntensity_1;
float4 _Emissive_Map_TexelSize;
float _EmissiveIntensity;
float _EnableCoverage;
float _CoverageSize;
float _CoverageNormalIntensity;
float _CoverageRoughnessIntensity;
float _CoverageMetallicIntensity;
float _NoiseTextureSize;
float4 _SlopeNoiseTexture_TexelSize;
float4 _CoverageColorMap_TexelSize;
float4 _CoverageORMMap_TexelSize;
float4 _CoverageNormalMap_TexelSize;
float _SloapOffset;
float _SloapContrast;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D);
SAMPLER(sampler_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D);
TEXTURE2D(_ColorMap);
SAMPLER(sampler_ColorMap);
TEXTURE2D(_ColorMap_1);
SAMPLER(sampler_ColorMap_1);
TEXTURE2D(_NormalMap);
SAMPLER(sampler_NormalMap);
TEXTURE2D(_ORMMap);
SAMPLER(sampler_ORMMap);
TEXTURE2D(_Emissive_Map);
SAMPLER(sampler_Emissive_Map);
TEXTURE2D(_SlopeNoiseTexture);
SAMPLER(sampler_SlopeNoiseTexture);
TEXTURE2D(_CoverageColorMap);
SAMPLER(sampler_CoverageColorMap);
TEXTURE2D(_CoverageORMMap);
SAMPLER(sampler_CoverageORMMap);
TEXTURE2D(_CoverageNormalMap);
SAMPLER(sampler_CoverageNormalMap);

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
// GraphFunctions: <None>

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
description.Position = IN.ObjectSpacePosition;
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
float Alpha;
float AlphaClipThreshold;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
UnityTexture2D _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_ColorMap_1, sampler_ColorMap_1, _ColorMap_1_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.tex, _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.samplerstate, _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4, _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_R_4_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.r;
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_G_5_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.g;
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_B_6_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.b;
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_A_7_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.a;
float _Property_7a43d5f1d3df46c2869bc70eebee1f91_Out_0_Float = _EmissiveIntensity_1;
surface.Alpha = (_SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4).x;
surface.AlphaClipThreshold = _Property_7a43d5f1d3df46c2869bc70eebee1f91_Out_0_Float;
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
Cull [_Cull]
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
#pragma shader_feature_local_fragment _ _ALPHATEST_ON
#pragma shader_feature_local _EDGEBLENDING_NORMALBLENDING _EDGEBLENDING_VERTEXBLENDING _EDGEBLENDING_NOISEBLENDING

#if defined(_EDGEBLENDING_NORMALBLENDING)
#define KEYWORD_PERMUTATION_0
#elif defined(_EDGEBLENDING_VERTEXBLENDING)
#define KEYWORD_PERMUTATION_1
#else
#define KEYWORD_PERMUTATION_2
#endif


// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_TEXCOORD1
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_NORMAL_WS
#define VARYINGS_NEED_TANGENT_WS
#define VARYINGS_NEED_TEXCOORD0
#define VARYINGS_NEED_CULLFACE
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
 float3 ObjectSpaceNormal;
 float3 WorldSpaceNormal;
 float3 TangentSpaceNormal;
 float3 WorldSpaceTangent;
 float3 WorldSpaceBiTangent;
 float3 ObjectSpacePosition;
 float4 uv0;
 float FaceSign;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
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

float4 _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D_TexelSize;
float4 _ColorMap_TexelSize;
float4 _ColorMap_1_TexelSize;
float4 _NormalMap_TexelSize;
float4 _ColorTint;
float _NormalIntensity;
float _UV_Size;
float4 _ORMMap_TexelSize;
float _RoughnessIntensity;
float _AOIntensity;
float _MetallicIntensity;
float4 _EmissiveColor;
float _EmissiveIntensity_1;
float4 _Emissive_Map_TexelSize;
float _EmissiveIntensity;
float _EnableCoverage;
float _CoverageSize;
float _CoverageNormalIntensity;
float _CoverageRoughnessIntensity;
float _CoverageMetallicIntensity;
float _NoiseTextureSize;
float4 _SlopeNoiseTexture_TexelSize;
float4 _CoverageColorMap_TexelSize;
float4 _CoverageORMMap_TexelSize;
float4 _CoverageNormalMap_TexelSize;
float _SloapOffset;
float _SloapContrast;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D);
SAMPLER(sampler_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D);
TEXTURE2D(_ColorMap);
SAMPLER(sampler_ColorMap);
TEXTURE2D(_ColorMap_1);
SAMPLER(sampler_ColorMap_1);
TEXTURE2D(_NormalMap);
SAMPLER(sampler_NormalMap);
TEXTURE2D(_ORMMap);
SAMPLER(sampler_ORMMap);
TEXTURE2D(_Emissive_Map);
SAMPLER(sampler_Emissive_Map);
TEXTURE2D(_SlopeNoiseTexture);
SAMPLER(sampler_SlopeNoiseTexture);
TEXTURE2D(_CoverageColorMap);
SAMPLER(sampler_CoverageColorMap);
TEXTURE2D(_CoverageORMMap);
SAMPLER(sampler_CoverageORMMap);
TEXTURE2D(_CoverageNormalMap);
SAMPLER(sampler_CoverageNormalMap);

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

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

void Unity_NormalStrength_float(float3 In, float Strength, out float3 Out)
{
    Out = float3(In.rg * Strength, lerp(1, In.b, saturate(Strength)));
}

void Unity_Lerp_float(float A, float B, float T, out float Out)
{
    Out = lerp(A, B, T);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
}

struct Bindings_PASGORMUnpacker_abcd41e335c426a4bb0f8de17b4b5f96_float
{
};

void SG_PASGORMUnpacker_abcd41e335c426a4bb0f8de17b4b5f96_float(float2 _UV, UnityTexture2D _ORM_Map, float _AO_Intensity, float _Roughness_Intensity, float _Metallic_Intensity, Bindings_PASGORMUnpacker_abcd41e335c426a4bb0f8de17b4b5f96_float IN, out float Ambient_Occlusion_1, out float Roughness_2, out float Metallic_3)
{
float _Float_2c47c335e2304faa927a789be883bb19_Out_0_Float = float(1);
UnityTexture2D _Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D = _ORM_Map;
float2 _Property_ad3893fa315b474ea60e7ee3e265957c_Out_0_Vector2 = _UV;
float4 _SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D.tex, _Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D.samplerstate, _Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D.GetTransformedUV(_Property_ad3893fa315b474ea60e7ee3e265957c_Out_0_Vector2) );
if (_Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4, _Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_719055d2c0184958bd1329f59130cce0_R_4_Float = _SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4.r;
float _SampleTexture2D_719055d2c0184958bd1329f59130cce0_G_5_Float = _SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4.g;
float _SampleTexture2D_719055d2c0184958bd1329f59130cce0_B_6_Float = _SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4.b;
float _SampleTexture2D_719055d2c0184958bd1329f59130cce0_A_7_Float = _SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4.a;
float _Property_90898696aae3458aa180c6f15d96aaee_Out_0_Float = _AO_Intensity;
float _Lerp_bf670034cefc4c6d9e02b57f5394811d_Out_3_Float;
Unity_Lerp_float(_Float_2c47c335e2304faa927a789be883bb19_Out_0_Float, _SampleTexture2D_719055d2c0184958bd1329f59130cce0_R_4_Float, _Property_90898696aae3458aa180c6f15d96aaee_Out_0_Float, _Lerp_bf670034cefc4c6d9e02b57f5394811d_Out_3_Float);
float _Property_e77944e4743744198e931506300c92ca_Out_0_Float = _Roughness_Intensity;
float _Multiply_43e873dc525645aab065eb6fc56ad77f_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_719055d2c0184958bd1329f59130cce0_G_5_Float, _Property_e77944e4743744198e931506300c92ca_Out_0_Float, _Multiply_43e873dc525645aab065eb6fc56ad77f_Out_2_Float);
float _OneMinus_3225b57b02ed4eaf9c118419fbefc479_Out_1_Float;
Unity_OneMinus_float(_Multiply_43e873dc525645aab065eb6fc56ad77f_Out_2_Float, _OneMinus_3225b57b02ed4eaf9c118419fbefc479_Out_1_Float);
float _Property_c0c103f0dfcd475fa428b0e043905944_Out_0_Float = _Metallic_Intensity;
float _Multiply_38b5ca9e78ba40d9b861ffc15889bd7f_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_719055d2c0184958bd1329f59130cce0_B_6_Float, _Property_c0c103f0dfcd475fa428b0e043905944_Out_0_Float, _Multiply_38b5ca9e78ba40d9b861ffc15889bd7f_Out_2_Float);
Ambient_Occlusion_1 = _Lerp_bf670034cefc4c6d9e02b57f5394811d_Out_3_Float;
Roughness_2 = _OneMinus_3225b57b02ed4eaf9c118419fbefc479_Out_1_Float;
Metallic_3 = _Multiply_38b5ca9e78ba40d9b861ffc15889bd7f_Out_2_Float;
}

struct Bindings_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float
{
half4 uv0;
};

void SG_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float(float _Texture_Size, UnityTexture2D _Color_Map, float4 _Color_Tint, UnityTexture2D _Normal_Map, float _Normal_Intensity, UnityTexture2D _ORM_Map, float _Roughness_Intensity, float _AO_Intensity, float _Metallic_Intensity, UnityTexture2D _Emissive_Map, float4 _Emissive_Color, float _Emissive_Intensity, Bindings_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float IN, out float4 Base_Color_5, out float3 Normal_1, out float4 Emissive_6, out float Ambient_Occlusion_4, out float Roughness_3, out float Metallic_2)
{
UnityTexture2D _Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D = _Color_Map;
float _Property_6e866f25d28140918dd5249261b96e76_Out_0_Float = _Texture_Size;
float2 _TilingAndOffset_49c249d74ea54ccf979206ad2c3c0c00_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, (_Property_6e866f25d28140918dd5249261b96e76_Out_0_Float.xx), float2 (0, 0), _TilingAndOffset_49c249d74ea54ccf979206ad2c3c0c00_Out_3_Vector2);
float _Property_94852b2b32f549138996f0dfb35b0486_Out_0_Float = _Texture_Size;
float2 _Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2;
Unity_Multiply_float2_float2(_TilingAndOffset_49c249d74ea54ccf979206ad2c3c0c00_Out_3_Vector2, (_Property_94852b2b32f549138996f0dfb35b0486_Out_0_Float.xx), _Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2);
float4 _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D.tex, _Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D.samplerstate, _Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D.GetTransformedUV(_Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2) );
if (_Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4, _Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_R_4_Float = _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4.r;
float _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_G_5_Float = _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4.g;
float _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_B_6_Float = _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4.b;
float _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_A_7_Float = _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4.a;
float4 _Property_c081d1852d284e4fa4bbb04ea3adf35e_Out_0_Vector4 = _Color_Tint;
float4 _Multiply_ff3b38102028498db7cc80acf5e42362_Out_2_Vector4;
Unity_Multiply_float4_float4(_SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4, _Property_c081d1852d284e4fa4bbb04ea3adf35e_Out_0_Vector4, _Multiply_ff3b38102028498db7cc80acf5e42362_Out_2_Vector4);
UnityTexture2D _Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D = _Normal_Map;
float4 _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D.tex, _Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D.samplerstate, _Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D.GetTransformedUV(_Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2) );
if (_Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4, _Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4);
float _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_R_4_Float = _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.r;
float _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_G_5_Float = _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.g;
float _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_B_6_Float = _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.b;
float _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_A_7_Float = _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.a;
float _Property_8b1f839dfbcd4481b201427fbc7310ad_Out_0_Float = _Normal_Intensity;
float3 _NormalStrength_a218ef03cc884bf0b0962d85b628afe7_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.xyz), _Property_8b1f839dfbcd4481b201427fbc7310ad_Out_0_Float, _NormalStrength_a218ef03cc884bf0b0962d85b628afe7_Out_2_Vector3);
UnityTexture2D _Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D = _Emissive_Map;
float4 _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D.tex, _Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D.samplerstate, _Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D.GetTransformedUV(_Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2) );
if (_Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4, _Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_R_4_Float = _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4.r;
float _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_G_5_Float = _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4.g;
float _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_B_6_Float = _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4.b;
float _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_A_7_Float = _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4.a;
float4 _Property_3a2f8dbe365d427e89744f8eb8dabd44_Out_0_Vector4 = _Emissive_Color;
float4 _Multiply_1efa7dc8f3994d8ea213351379d48448_Out_2_Vector4;
Unity_Multiply_float4_float4(_SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4, _Property_3a2f8dbe365d427e89744f8eb8dabd44_Out_0_Vector4, _Multiply_1efa7dc8f3994d8ea213351379d48448_Out_2_Vector4);
float _Property_09c3fe7e22be43caa45c324f52ea8137_Out_0_Float = _Emissive_Intensity;
float4 _Multiply_43a1c0123fb34a25a5d21581f08926f8_Out_2_Vector4;
Unity_Multiply_float4_float4(_Multiply_1efa7dc8f3994d8ea213351379d48448_Out_2_Vector4, (_Property_09c3fe7e22be43caa45c324f52ea8137_Out_0_Float.xxxx), _Multiply_43a1c0123fb34a25a5d21581f08926f8_Out_2_Vector4);
UnityTexture2D _Property_ecbd8a4e938a458cabefe2480cada231_Out_0_Texture2D = _ORM_Map;
float _Property_2ca0dc7be16d47bfb0faf210c4d70757_Out_0_Float = _AO_Intensity;
float _Property_fb245dde136f4b2486e260e720f83c83_Out_0_Float = _Roughness_Intensity;
float _Property_0fcde0f68f364243886b868db18a8d27_Out_0_Float = _Metallic_Intensity;
Bindings_PASGORMUnpacker_abcd41e335c426a4bb0f8de17b4b5f96_float _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94;
half _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_AmbientOcclusion_1_Float;
half _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Roughness_2_Float;
half _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Metallic_3_Float;
SG_PASGORMUnpacker_abcd41e335c426a4bb0f8de17b4b5f96_float(_Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2, _Property_ecbd8a4e938a458cabefe2480cada231_Out_0_Texture2D, _Property_2ca0dc7be16d47bfb0faf210c4d70757_Out_0_Float, _Property_fb245dde136f4b2486e260e720f83c83_Out_0_Float, _Property_0fcde0f68f364243886b868db18a8d27_Out_0_Float, _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94, _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_AmbientOcclusion_1_Float, _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Roughness_2_Float, _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Metallic_3_Float);
Base_Color_5 = _Multiply_ff3b38102028498db7cc80acf5e42362_Out_2_Vector4;
Normal_1 = _NormalStrength_a218ef03cc884bf0b0962d85b628afe7_Out_2_Vector3;
Emissive_6 = _Multiply_43a1c0123fb34a25a5d21581f08926f8_Out_2_Vector4;
Ambient_Occlusion_4 = _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_AmbientOcclusion_1_Float;
Roughness_3 = _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Roughness_2_Float;
Metallic_2 = _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Metallic_3_Float;
}

void Unity_DotProduct_float3(float3 A, float3 B, out float Out)
{
    Out = dot(A, B);
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Clamp_float(float In, float Min, float Max, out float Out)
{
    Out = clamp(In, Min, Max);
}

void Unity_Contrast_float(float3 In, float Contrast, out float3 Out)
{
    float midpoint = pow(0.5, 2.2);
    Out =  (In - midpoint) * Contrast + midpoint;
}

void Unity_Clamp_float3(float3 In, float3 Min, float3 Max, out float3 Out)
{
    Out = clamp(In, Min, Max);
}

struct Bindings_PASGVerticalBlend_e9f0b2d5820244846982772499370842_float
{
float3 ObjectSpaceNormal;
float3 WorldSpaceNormal;
float3 WorldSpaceTangent;
float3 WorldSpaceBiTangent;
float3 ObjectSpacePosition;
};

void SG_PASGVerticalBlend_e9f0b2d5820244846982772499370842_float(float _Sloap_Contrast, UnityTexture2D _Slope_Noise_Texture, float _Noise_Texture_Size, float3 _Explicit_Normal, float _Sloap_Offset, Bindings_PASGVerticalBlend_e9f0b2d5820244846982772499370842_float IN, out float3 OutVector3_1)
{
float3 _Property_5509c7fb30e445229f496a71c6e9c192_Out_0_Vector3 = _Explicit_Normal;
float3 _Transform_9c9f921cd0934012ac0d281c8e40a727_Out_1_Vector3;
{
float3x3 tangentTransform = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
_Transform_9c9f921cd0934012ac0d281c8e40a727_Out_1_Vector3 = TransformTangentToWorld(_Property_5509c7fb30e445229f496a71c6e9c192_Out_0_Vector3.xyz, tangentTransform, true);
}
float _DotProduct_4bc69611266041259c8da1b387474ec9_Out_2_Float;
Unity_DotProduct_float3(_Transform_9c9f921cd0934012ac0d281c8e40a727_Out_1_Vector3, float3(0, 1, 0), _DotProduct_4bc69611266041259c8da1b387474ec9_Out_2_Float);
float _Property_6a995c4e80824d6fa7d458ddcdc4eec3_Out_0_Float = _Sloap_Offset;
float _Add_a196acc146d1466fb309ec050e77feb2_Out_2_Float;
Unity_Add_float(_DotProduct_4bc69611266041259c8da1b387474ec9_Out_2_Float, _Property_6a995c4e80824d6fa7d458ddcdc4eec3_Out_0_Float, _Add_a196acc146d1466fb309ec050e77feb2_Out_2_Float);
float _Lerp_a1fed41c0847449ab72ae8fe4775270c_Out_3_Float;
Unity_Lerp_float(float(0), float(3), _Add_a196acc146d1466fb309ec050e77feb2_Out_2_Float, _Lerp_a1fed41c0847449ab72ae8fe4775270c_Out_3_Float);
float _Clamp_d4d6e762b7b1422d936b98d7f9429754_Out_3_Float;
Unity_Clamp_float(_Lerp_a1fed41c0847449ab72ae8fe4775270c_Out_3_Float, float(0), float(1), _Clamp_d4d6e762b7b1422d936b98d7f9429754_Out_3_Float);
float _Property_985b04ddd40847029dbfa829fbcaeed1_Out_0_Float = _Sloap_Contrast;
float3 _Contrast_2864897542064adcb4b61fcccadaf026_Out_2_Vector3;
Unity_Contrast_float((_Clamp_d4d6e762b7b1422d936b98d7f9429754_Out_3_Float.xxx), _Property_985b04ddd40847029dbfa829fbcaeed1_Out_0_Float, _Contrast_2864897542064adcb4b61fcccadaf026_Out_2_Vector3);
float3 _Clamp_b8f1f82d045f437a910a54b62bb2df81_Out_3_Vector3;
Unity_Clamp_float3(_Contrast_2864897542064adcb4b61fcccadaf026_Out_2_Vector3, float3(0, 0, 0), float3(1, 1, 1), _Clamp_b8f1f82d045f437a910a54b62bb2df81_Out_3_Vector3);
float3 _Transform_229ffe25002343a39c8caaa3d7f6df91_Out_1_Vector3;
_Transform_229ffe25002343a39c8caaa3d7f6df91_Out_1_Vector3 = TransformObjectToWorldDir(IN.ObjectSpacePosition.xyz, true);
float3 _Vector3_ec86905b34c54695bed5b7d1975cb6ce_Out_0_Vector3 = float3(float(0), float(2), float(0));
float _DotProduct_af3cb3caf6e4473c8161868b15c0255b_Out_2_Float;
Unity_DotProduct_float3(_Transform_229ffe25002343a39c8caaa3d7f6df91_Out_1_Vector3, _Vector3_ec86905b34c54695bed5b7d1975cb6ce_Out_0_Vector3, _DotProduct_af3cb3caf6e4473c8161868b15c0255b_Out_2_Float);
float _Add_47be9b48b4c94dd28e8c3db7f6ba1a05_Out_2_Float;
Unity_Add_float(_Property_6a995c4e80824d6fa7d458ddcdc4eec3_Out_0_Float, _DotProduct_af3cb3caf6e4473c8161868b15c0255b_Out_2_Float, _Add_47be9b48b4c94dd28e8c3db7f6ba1a05_Out_2_Float);
float _Multiply_35491cd334cb4e34a76b48de2bc5814a_Out_2_Float;
Unity_Multiply_float_float(_Add_47be9b48b4c94dd28e8c3db7f6ba1a05_Out_2_Float, 2, _Multiply_35491cd334cb4e34a76b48de2bc5814a_Out_2_Float);
float _Clamp_ccd0969e9da6420f94e7113654fd480e_Out_3_Float;
Unity_Clamp_float(_Multiply_35491cd334cb4e34a76b48de2bc5814a_Out_2_Float, float(0), float(1), _Clamp_ccd0969e9da6420f94e7113654fd480e_Out_3_Float);
float3 _Contrast_454ba7c8fb3a4d03951b073867fa5f14_Out_2_Vector3;
Unity_Contrast_float((_Clamp_ccd0969e9da6420f94e7113654fd480e_Out_3_Float.xxx), _Property_985b04ddd40847029dbfa829fbcaeed1_Out_0_Float, _Contrast_454ba7c8fb3a4d03951b073867fa5f14_Out_2_Vector3);
float3 _Clamp_1ba5cf0aee8245bd9dca155954c3df81_Out_3_Vector3;
Unity_Clamp_float3(_Contrast_454ba7c8fb3a4d03951b073867fa5f14_Out_2_Vector3, float3(0, 0, 0), float3(1, 1, 1), _Clamp_1ba5cf0aee8245bd9dca155954c3df81_Out_3_Vector3);
UnityTexture2D _Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D = _Slope_Noise_Texture;
float _Property_8e663ddd1476448d948a9de1bf1f83a9_Out_0_Float = _Noise_Texture_Size;
float3 Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_UV = IN.ObjectSpacePosition * _Property_8e663ddd1476448d948a9de1bf1f83a9_Out_0_Float;
float3 Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend = SafePositivePow_float(IN.ObjectSpaceNormal, min(float(1), floor(log2(Min_float())/log2(1/sqrt(3)))) );
Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend /= dot(Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend, 1.0);
float4 Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_X = SAMPLE_TEXTURE2D(_Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.tex, _Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.samplerstate, Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_UV.zy);
float4 Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Y = SAMPLE_TEXTURE2D(_Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.tex, _Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.samplerstate, Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_UV.xz);
float4 Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Z = SAMPLE_TEXTURE2D(_Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.tex, _Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.samplerstate, Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_UV.xy);
float4 _Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Out_0_Vector4 = Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_X * Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend.x + Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Y * Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend.y + Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Z * Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend.z;
float _Swizzle_90fb1902a2bd4070b4f9e27ac287ca97_Out_1_Float = (_Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Out_0_Vector4.xy).y;
float _Clamp_f32c97c6ccbf44649bd757503ccdbe71_Out_3_Float;
Unity_Clamp_float(_Swizzle_90fb1902a2bd4070b4f9e27ac287ca97_Out_1_Float, float(0), float(1), _Clamp_f32c97c6ccbf44649bd757503ccdbe71_Out_3_Float);
float _Add_92d15430b3c042719d65f1252bc35d7a_Out_2_Float;
Unity_Add_float(_Multiply_35491cd334cb4e34a76b48de2bc5814a_Out_2_Float, _Clamp_f32c97c6ccbf44649bd757503ccdbe71_Out_3_Float, _Add_92d15430b3c042719d65f1252bc35d7a_Out_2_Float);
float3 _Contrast_c2fe106c51f0498e987cf31090ec563d_Out_2_Vector3;
Unity_Contrast_float((_Add_92d15430b3c042719d65f1252bc35d7a_Out_2_Float.xxx), _Property_985b04ddd40847029dbfa829fbcaeed1_Out_0_Float, _Contrast_c2fe106c51f0498e987cf31090ec563d_Out_2_Vector3);
float3 _Clamp_05438405442c47cb98e571d128277bbe_Out_3_Vector3;
Unity_Clamp_float3(_Contrast_c2fe106c51f0498e987cf31090ec563d_Out_2_Vector3, float3(0, 0, 0), float3(1, 1, 1), _Clamp_05438405442c47cb98e571d128277bbe_Out_3_Vector3);
#if defined(_EDGEBLENDING_NORMALBLENDING)
float3 _EdgeBlending_cdbe33a15cf6452b8881ecea22adcb84_Out_0_Vector3 = _Clamp_b8f1f82d045f437a910a54b62bb2df81_Out_3_Vector3;
#elif defined(_EDGEBLENDING_VERTEXBLENDING)
float3 _EdgeBlending_cdbe33a15cf6452b8881ecea22adcb84_Out_0_Vector3 = _Clamp_1ba5cf0aee8245bd9dca155954c3df81_Out_3_Vector3;
#else
float3 _EdgeBlending_cdbe33a15cf6452b8881ecea22adcb84_Out_0_Vector3 = _Clamp_05438405442c47cb98e571d128277bbe_Out_3_Vector3;
#endif
OutVector3_1 = _EdgeBlending_cdbe33a15cf6452b8881ecea22adcb84_Out_0_Vector3;
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Branch_float3(float Predicate, float3 True, float3 False, out float3 Out)
{
    Out = Predicate ? True : False;
}

void Unity_Negate_float3(float3 In, out float3 Out)
{
    Out = -1 * In;
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
description.Position = IN.ObjectSpacePosition;
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
float Alpha;
float AlphaClipThreshold;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
float _IsFrontFace_8b8f9781e7a946f18c8c49da20d0925c_Out_0_Boolean = max(0, IN.FaceSign.x);
float _Property_868c674f556d42be9071980dc60f4aed_Out_0_Boolean = _EnableCoverage;
float _Property_a0b21575a96c42a2b05aa3518645b2f4_Out_0_Float = _UV_Size;
UnityTexture2D _Property_4d248f4a9de94d7bb9a5c3c7c4c10a68_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_ColorMap, sampler_ColorMap, _ColorMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _Property_061dd20995c54e4b8c0357aa709c6e56_Out_0_Vector4 = _ColorTint;
UnityTexture2D _Property_cc904da6b4714cd6a9559b3bcf6ba7a6_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_NormalMap, sampler_NormalMap, _NormalMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_49f412000c0b4f18863fb7afae8b43fc_Out_0_Float = _NormalIntensity;
UnityTexture2D _Property_3fcfe917c75d4a46bfe50da31a24c74f_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_ORMMap, sampler_ORMMap, _ORMMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_bd35b765c62349f285447de81728f0c8_Out_0_Float = _RoughnessIntensity;
float _Property_f125a72836c24b0fa5ef72823c8e6452_Out_0_Float = _AOIntensity;
float _Property_0251a196075947af9d3ad28d6875a5ef_Out_0_Float = _MetallicIntensity;
UnityTexture2D _Property_47ecc092fbd144218fe6440250e367b3_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Emissive_Map, sampler_Emissive_Map, _Emissive_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _Property_c065f0d4df8c40528116ee8e516b2221_Out_0_Vector4 = _EmissiveColor;
float _Property_0038e41f028740e4abc1958ca58302a1_Out_0_Float = _EmissiveIntensity;
Bindings_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe;
_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe.uv0 = IN.uv0;
float4 _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_BaseColor_5_Vector4;
float3 _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Normal_1_Vector3;
float4 _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Emissive_6_Vector4;
float _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_AmbientOcclusion_4_Float;
float _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Roughness_3_Float;
float _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Metallic_2_Float;
SG_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float(_Property_a0b21575a96c42a2b05aa3518645b2f4_Out_0_Float, _Property_4d248f4a9de94d7bb9a5c3c7c4c10a68_Out_0_Texture2D, _Property_061dd20995c54e4b8c0357aa709c6e56_Out_0_Vector4, _Property_cc904da6b4714cd6a9559b3bcf6ba7a6_Out_0_Texture2D, _Property_49f412000c0b4f18863fb7afae8b43fc_Out_0_Float, _Property_3fcfe917c75d4a46bfe50da31a24c74f_Out_0_Texture2D, _Property_bd35b765c62349f285447de81728f0c8_Out_0_Float, _Property_f125a72836c24b0fa5ef72823c8e6452_Out_0_Float, _Property_0251a196075947af9d3ad28d6875a5ef_Out_0_Float, _Property_47ecc092fbd144218fe6440250e367b3_Out_0_Texture2D, _Property_c065f0d4df8c40528116ee8e516b2221_Out_0_Vector4, _Property_0038e41f028740e4abc1958ca58302a1_Out_0_Float, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_BaseColor_5_Vector4, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Normal_1_Vector3, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Emissive_6_Vector4, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_AmbientOcclusion_4_Float, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Roughness_3_Float, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Metallic_2_Float);
float _Property_623a0c9d694c4c33b6256bde952c53e5_Out_0_Float = _CoverageSize;
UnityTexture2D _Property_58d380b2034145c8b7d2eda637dbc296_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_CoverageColorMap, sampler_CoverageColorMap, _CoverageColorMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
UnityTexture2D _Property_f324dd6628ea45b1a630d750fa3d6b66_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_CoverageNormalMap, sampler_CoverageNormalMap, _CoverageNormalMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_b00b34515282450295cc4f4eb7b4a0ce_Out_0_Float = _CoverageNormalIntensity;
UnityTexture2D _Property_24ae6469bfeb4dada6fb7c7d39985cb3_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_CoverageORMMap, sampler_CoverageORMMap, _CoverageORMMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_03c29a27092f4bbb8a4fcc3b6e7b2909_Out_0_Float = _CoverageRoughnessIntensity;
float _Property_07cad64e572e4602a346b7dd90c3eb21_Out_0_Float = _CoverageMetallicIntensity;
Bindings_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c;
_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c.uv0 = IN.uv0;
float4 _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_BaseColor_5_Vector4;
float3 _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Normal_1_Vector3;
float4 _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Emissive_6_Vector4;
float _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_AmbientOcclusion_4_Float;
float _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Roughness_3_Float;
float _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Metallic_2_Float;
SG_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float(_Property_623a0c9d694c4c33b6256bde952c53e5_Out_0_Float, _Property_58d380b2034145c8b7d2eda637dbc296_Out_0_Texture2D, float4 (1, 1, 1, 1), _Property_f324dd6628ea45b1a630d750fa3d6b66_Out_0_Texture2D, _Property_b00b34515282450295cc4f4eb7b4a0ce_Out_0_Float, _Property_24ae6469bfeb4dada6fb7c7d39985cb3_Out_0_Texture2D, _Property_03c29a27092f4bbb8a4fcc3b6e7b2909_Out_0_Float, float(0), _Property_07cad64e572e4602a346b7dd90c3eb21_Out_0_Float, UnityBuildTexture2DStructNoScale(_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D), float4 (0, 0, 0, 0), float(0), _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_BaseColor_5_Vector4, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Normal_1_Vector3, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Emissive_6_Vector4, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_AmbientOcclusion_4_Float, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Roughness_3_Float, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Metallic_2_Float);
float _Property_0785fc58d71d4d63abb8c1102aa1a1b7_Out_0_Float = _SloapContrast;
UnityTexture2D _Property_12921d1841154c59b11aefccb2cbca31_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_SlopeNoiseTexture, sampler_SlopeNoiseTexture, _SlopeNoiseTexture_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_10dd46128137476fb3fd49de6154e94c_Out_0_Float = _NoiseTextureSize;
float _Property_a654ef8b1e2a4529ab82f92b3ae202ec_Out_0_Float = _SloapOffset;
Bindings_PASGVerticalBlend_e9f0b2d5820244846982772499370842_float _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d;
_PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d.ObjectSpaceNormal = IN.ObjectSpaceNormal;
_PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d.WorldSpaceNormal = IN.WorldSpaceNormal;
_PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d.WorldSpaceTangent = IN.WorldSpaceTangent;
_PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d.WorldSpaceBiTangent = IN.WorldSpaceBiTangent;
_PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d.ObjectSpacePosition = IN.ObjectSpacePosition;
float3 _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d_OutVector3_1_Vector3;
SG_PASGVerticalBlend_e9f0b2d5820244846982772499370842_float(_Property_0785fc58d71d4d63abb8c1102aa1a1b7_Out_0_Float, _Property_12921d1841154c59b11aefccb2cbca31_Out_0_Texture2D, _Property_10dd46128137476fb3fd49de6154e94c_Out_0_Float, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Normal_1_Vector3, _Property_a654ef8b1e2a4529ab82f92b3ae202ec_Out_0_Float, _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d, _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d_OutVector3_1_Vector3);
float3 _Lerp_e69be27d4f0d4e26a37c622c629ca56d_Out_3_Vector3;
Unity_Lerp_float3(_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Normal_1_Vector3, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Normal_1_Vector3, _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d_OutVector3_1_Vector3, _Lerp_e69be27d4f0d4e26a37c622c629ca56d_Out_3_Vector3);
float3 _Branch_3213119f26d54fc08b755c491d9dd6c4_Out_3_Vector3;
Unity_Branch_float3(_Property_868c674f556d42be9071980dc60f4aed_Out_0_Boolean, _Lerp_e69be27d4f0d4e26a37c622c629ca56d_Out_3_Vector3, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Normal_1_Vector3, _Branch_3213119f26d54fc08b755c491d9dd6c4_Out_3_Vector3);
float3 _Negate_c9035ab4407c48978c2cd379c7ed3db2_Out_1_Vector3;
Unity_Negate_float3(_Branch_3213119f26d54fc08b755c491d9dd6c4_Out_3_Vector3, _Negate_c9035ab4407c48978c2cd379c7ed3db2_Out_1_Vector3);
float3 _Branch_6a68e6f7151b40c684e82041ff10474e_Out_3_Vector3;
Unity_Branch_float3(_IsFrontFace_8b8f9781e7a946f18c8c49da20d0925c_Out_0_Boolean, _Branch_3213119f26d54fc08b755c491d9dd6c4_Out_3_Vector3, _Negate_c9035ab4407c48978c2cd379c7ed3db2_Out_1_Vector3, _Branch_6a68e6f7151b40c684e82041ff10474e_Out_3_Vector3);
UnityTexture2D _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_ColorMap_1, sampler_ColorMap_1, _ColorMap_1_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.tex, _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.samplerstate, _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4, _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_R_4_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.r;
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_G_5_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.g;
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_B_6_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.b;
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_A_7_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.a;
float _Property_7a43d5f1d3df46c2869bc70eebee1f91_Out_0_Float = _EmissiveIntensity_1;
surface.NormalTS = _Branch_6a68e6f7151b40c684e82041ff10474e_Out_3_Vector3;
surface.Alpha = (_SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4).x;
surface.AlphaClipThreshold = _Property_7a43d5f1d3df46c2869bc70eebee1f91_Out_0_Float;
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

    // use bitangent on the fly like in hdrp
    // IMPORTANT! If we ever support Flip on double sided materials ensure bitangent and tangent are NOT flipped.
    float crossSign = (input.tangentWS.w > 0.0 ? 1.0 : -1.0)* GetOddNegativeScale();
    float3 bitang = crossSign * cross(input.normalWS.xyz, input.tangentWS.xyz);

    output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph
    output.ObjectSpaceNormal = normalize(mul(output.WorldSpaceNormal, (float3x3) UNITY_MATRIX_M));           // transposed multiplication by inverse matrix to handle normal scale
    output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);

    // to pr               eserve mikktspace compliance we use same scale renormFactor as was used on the normal.
    // This                is explained in section 2.2 in "surface gradient based bump mapping framework"
    output.WorldSpaceTangent = renormFactor * input.tangentWS.xyz;
    output.WorldSpaceBiTangent = renormFactor * bitang;

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
    BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
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
#pragma shader_feature_local_fragment _ _ALPHATEST_ON
#pragma shader_feature_local _EDGEBLENDING_NORMALBLENDING _EDGEBLENDING_VERTEXBLENDING _EDGEBLENDING_NOISEBLENDING

#if defined(_EDGEBLENDING_NORMALBLENDING)
#define KEYWORD_PERMUTATION_0
#elif defined(_EDGEBLENDING_VERTEXBLENDING)
#define KEYWORD_PERMUTATION_1
#else
#define KEYWORD_PERMUTATION_2
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
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_NORMAL_WS
#define VARYINGS_NEED_TANGENT_WS
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
 float4 tangentWS;
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
 float3 ObjectSpaceNormal;
 float3 WorldSpaceNormal;
 float3 WorldSpaceTangent;
 float3 WorldSpaceBiTangent;
 float3 ObjectSpacePosition;
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
 float4 tangentWS : INTERP0;
 float4 texCoord0 : INTERP1;
 float4 texCoord1 : INTERP2;
 float4 texCoord2 : INTERP3;
 float3 positionWS : INTERP4;
 float3 normalWS : INTERP5;
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
output.tangentWS = input.tangentWS.xyzw;
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

float4 _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D_TexelSize;
float4 _ColorMap_TexelSize;
float4 _ColorMap_1_TexelSize;
float4 _NormalMap_TexelSize;
float4 _ColorTint;
float _NormalIntensity;
float _UV_Size;
float4 _ORMMap_TexelSize;
float _RoughnessIntensity;
float _AOIntensity;
float _MetallicIntensity;
float4 _EmissiveColor;
float _EmissiveIntensity_1;
float4 _Emissive_Map_TexelSize;
float _EmissiveIntensity;
float _EnableCoverage;
float _CoverageSize;
float _CoverageNormalIntensity;
float _CoverageRoughnessIntensity;
float _CoverageMetallicIntensity;
float _NoiseTextureSize;
float4 _SlopeNoiseTexture_TexelSize;
float4 _CoverageColorMap_TexelSize;
float4 _CoverageORMMap_TexelSize;
float4 _CoverageNormalMap_TexelSize;
float _SloapOffset;
float _SloapContrast;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D);
SAMPLER(sampler_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D);
TEXTURE2D(_ColorMap);
SAMPLER(sampler_ColorMap);
TEXTURE2D(_ColorMap_1);
SAMPLER(sampler_ColorMap_1);
TEXTURE2D(_NormalMap);
SAMPLER(sampler_NormalMap);
TEXTURE2D(_ORMMap);
SAMPLER(sampler_ORMMap);
TEXTURE2D(_Emissive_Map);
SAMPLER(sampler_Emissive_Map);
TEXTURE2D(_SlopeNoiseTexture);
SAMPLER(sampler_SlopeNoiseTexture);
TEXTURE2D(_CoverageColorMap);
SAMPLER(sampler_CoverageColorMap);
TEXTURE2D(_CoverageORMMap);
SAMPLER(sampler_CoverageORMMap);
TEXTURE2D(_CoverageNormalMap);
SAMPLER(sampler_CoverageNormalMap);

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

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

void Unity_NormalStrength_float(float3 In, float Strength, out float3 Out)
{
    Out = float3(In.rg * Strength, lerp(1, In.b, saturate(Strength)));
}

void Unity_Lerp_float(float A, float B, float T, out float Out)
{
    Out = lerp(A, B, T);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
}

struct Bindings_PASGORMUnpacker_abcd41e335c426a4bb0f8de17b4b5f96_float
{
};

void SG_PASGORMUnpacker_abcd41e335c426a4bb0f8de17b4b5f96_float(float2 _UV, UnityTexture2D _ORM_Map, float _AO_Intensity, float _Roughness_Intensity, float _Metallic_Intensity, Bindings_PASGORMUnpacker_abcd41e335c426a4bb0f8de17b4b5f96_float IN, out float Ambient_Occlusion_1, out float Roughness_2, out float Metallic_3)
{
float _Float_2c47c335e2304faa927a789be883bb19_Out_0_Float = float(1);
UnityTexture2D _Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D = _ORM_Map;
float2 _Property_ad3893fa315b474ea60e7ee3e265957c_Out_0_Vector2 = _UV;
float4 _SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D.tex, _Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D.samplerstate, _Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D.GetTransformedUV(_Property_ad3893fa315b474ea60e7ee3e265957c_Out_0_Vector2) );
if (_Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4, _Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_719055d2c0184958bd1329f59130cce0_R_4_Float = _SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4.r;
float _SampleTexture2D_719055d2c0184958bd1329f59130cce0_G_5_Float = _SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4.g;
float _SampleTexture2D_719055d2c0184958bd1329f59130cce0_B_6_Float = _SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4.b;
float _SampleTexture2D_719055d2c0184958bd1329f59130cce0_A_7_Float = _SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4.a;
float _Property_90898696aae3458aa180c6f15d96aaee_Out_0_Float = _AO_Intensity;
float _Lerp_bf670034cefc4c6d9e02b57f5394811d_Out_3_Float;
Unity_Lerp_float(_Float_2c47c335e2304faa927a789be883bb19_Out_0_Float, _SampleTexture2D_719055d2c0184958bd1329f59130cce0_R_4_Float, _Property_90898696aae3458aa180c6f15d96aaee_Out_0_Float, _Lerp_bf670034cefc4c6d9e02b57f5394811d_Out_3_Float);
float _Property_e77944e4743744198e931506300c92ca_Out_0_Float = _Roughness_Intensity;
float _Multiply_43e873dc525645aab065eb6fc56ad77f_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_719055d2c0184958bd1329f59130cce0_G_5_Float, _Property_e77944e4743744198e931506300c92ca_Out_0_Float, _Multiply_43e873dc525645aab065eb6fc56ad77f_Out_2_Float);
float _OneMinus_3225b57b02ed4eaf9c118419fbefc479_Out_1_Float;
Unity_OneMinus_float(_Multiply_43e873dc525645aab065eb6fc56ad77f_Out_2_Float, _OneMinus_3225b57b02ed4eaf9c118419fbefc479_Out_1_Float);
float _Property_c0c103f0dfcd475fa428b0e043905944_Out_0_Float = _Metallic_Intensity;
float _Multiply_38b5ca9e78ba40d9b861ffc15889bd7f_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_719055d2c0184958bd1329f59130cce0_B_6_Float, _Property_c0c103f0dfcd475fa428b0e043905944_Out_0_Float, _Multiply_38b5ca9e78ba40d9b861ffc15889bd7f_Out_2_Float);
Ambient_Occlusion_1 = _Lerp_bf670034cefc4c6d9e02b57f5394811d_Out_3_Float;
Roughness_2 = _OneMinus_3225b57b02ed4eaf9c118419fbefc479_Out_1_Float;
Metallic_3 = _Multiply_38b5ca9e78ba40d9b861ffc15889bd7f_Out_2_Float;
}

struct Bindings_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float
{
half4 uv0;
};

void SG_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float(float _Texture_Size, UnityTexture2D _Color_Map, float4 _Color_Tint, UnityTexture2D _Normal_Map, float _Normal_Intensity, UnityTexture2D _ORM_Map, float _Roughness_Intensity, float _AO_Intensity, float _Metallic_Intensity, UnityTexture2D _Emissive_Map, float4 _Emissive_Color, float _Emissive_Intensity, Bindings_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float IN, out float4 Base_Color_5, out float3 Normal_1, out float4 Emissive_6, out float Ambient_Occlusion_4, out float Roughness_3, out float Metallic_2)
{
UnityTexture2D _Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D = _Color_Map;
float _Property_6e866f25d28140918dd5249261b96e76_Out_0_Float = _Texture_Size;
float2 _TilingAndOffset_49c249d74ea54ccf979206ad2c3c0c00_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, (_Property_6e866f25d28140918dd5249261b96e76_Out_0_Float.xx), float2 (0, 0), _TilingAndOffset_49c249d74ea54ccf979206ad2c3c0c00_Out_3_Vector2);
float _Property_94852b2b32f549138996f0dfb35b0486_Out_0_Float = _Texture_Size;
float2 _Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2;
Unity_Multiply_float2_float2(_TilingAndOffset_49c249d74ea54ccf979206ad2c3c0c00_Out_3_Vector2, (_Property_94852b2b32f549138996f0dfb35b0486_Out_0_Float.xx), _Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2);
float4 _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D.tex, _Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D.samplerstate, _Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D.GetTransformedUV(_Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2) );
if (_Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4, _Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_R_4_Float = _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4.r;
float _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_G_5_Float = _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4.g;
float _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_B_6_Float = _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4.b;
float _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_A_7_Float = _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4.a;
float4 _Property_c081d1852d284e4fa4bbb04ea3adf35e_Out_0_Vector4 = _Color_Tint;
float4 _Multiply_ff3b38102028498db7cc80acf5e42362_Out_2_Vector4;
Unity_Multiply_float4_float4(_SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4, _Property_c081d1852d284e4fa4bbb04ea3adf35e_Out_0_Vector4, _Multiply_ff3b38102028498db7cc80acf5e42362_Out_2_Vector4);
UnityTexture2D _Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D = _Normal_Map;
float4 _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D.tex, _Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D.samplerstate, _Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D.GetTransformedUV(_Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2) );
if (_Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4, _Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4);
float _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_R_4_Float = _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.r;
float _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_G_5_Float = _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.g;
float _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_B_6_Float = _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.b;
float _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_A_7_Float = _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.a;
float _Property_8b1f839dfbcd4481b201427fbc7310ad_Out_0_Float = _Normal_Intensity;
float3 _NormalStrength_a218ef03cc884bf0b0962d85b628afe7_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.xyz), _Property_8b1f839dfbcd4481b201427fbc7310ad_Out_0_Float, _NormalStrength_a218ef03cc884bf0b0962d85b628afe7_Out_2_Vector3);
UnityTexture2D _Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D = _Emissive_Map;
float4 _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D.tex, _Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D.samplerstate, _Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D.GetTransformedUV(_Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2) );
if (_Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4, _Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_R_4_Float = _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4.r;
float _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_G_5_Float = _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4.g;
float _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_B_6_Float = _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4.b;
float _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_A_7_Float = _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4.a;
float4 _Property_3a2f8dbe365d427e89744f8eb8dabd44_Out_0_Vector4 = _Emissive_Color;
float4 _Multiply_1efa7dc8f3994d8ea213351379d48448_Out_2_Vector4;
Unity_Multiply_float4_float4(_SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4, _Property_3a2f8dbe365d427e89744f8eb8dabd44_Out_0_Vector4, _Multiply_1efa7dc8f3994d8ea213351379d48448_Out_2_Vector4);
float _Property_09c3fe7e22be43caa45c324f52ea8137_Out_0_Float = _Emissive_Intensity;
float4 _Multiply_43a1c0123fb34a25a5d21581f08926f8_Out_2_Vector4;
Unity_Multiply_float4_float4(_Multiply_1efa7dc8f3994d8ea213351379d48448_Out_2_Vector4, (_Property_09c3fe7e22be43caa45c324f52ea8137_Out_0_Float.xxxx), _Multiply_43a1c0123fb34a25a5d21581f08926f8_Out_2_Vector4);
UnityTexture2D _Property_ecbd8a4e938a458cabefe2480cada231_Out_0_Texture2D = _ORM_Map;
float _Property_2ca0dc7be16d47bfb0faf210c4d70757_Out_0_Float = _AO_Intensity;
float _Property_fb245dde136f4b2486e260e720f83c83_Out_0_Float = _Roughness_Intensity;
float _Property_0fcde0f68f364243886b868db18a8d27_Out_0_Float = _Metallic_Intensity;
Bindings_PASGORMUnpacker_abcd41e335c426a4bb0f8de17b4b5f96_float _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94;
half _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_AmbientOcclusion_1_Float;
half _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Roughness_2_Float;
half _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Metallic_3_Float;
SG_PASGORMUnpacker_abcd41e335c426a4bb0f8de17b4b5f96_float(_Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2, _Property_ecbd8a4e938a458cabefe2480cada231_Out_0_Texture2D, _Property_2ca0dc7be16d47bfb0faf210c4d70757_Out_0_Float, _Property_fb245dde136f4b2486e260e720f83c83_Out_0_Float, _Property_0fcde0f68f364243886b868db18a8d27_Out_0_Float, _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94, _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_AmbientOcclusion_1_Float, _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Roughness_2_Float, _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Metallic_3_Float);
Base_Color_5 = _Multiply_ff3b38102028498db7cc80acf5e42362_Out_2_Vector4;
Normal_1 = _NormalStrength_a218ef03cc884bf0b0962d85b628afe7_Out_2_Vector3;
Emissive_6 = _Multiply_43a1c0123fb34a25a5d21581f08926f8_Out_2_Vector4;
Ambient_Occlusion_4 = _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_AmbientOcclusion_1_Float;
Roughness_3 = _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Roughness_2_Float;
Metallic_2 = _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Metallic_3_Float;
}

void Unity_DotProduct_float3(float3 A, float3 B, out float Out)
{
    Out = dot(A, B);
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Clamp_float(float In, float Min, float Max, out float Out)
{
    Out = clamp(In, Min, Max);
}

void Unity_Contrast_float(float3 In, float Contrast, out float3 Out)
{
    float midpoint = pow(0.5, 2.2);
    Out =  (In - midpoint) * Contrast + midpoint;
}

void Unity_Clamp_float3(float3 In, float3 Min, float3 Max, out float3 Out)
{
    Out = clamp(In, Min, Max);
}

struct Bindings_PASGVerticalBlend_e9f0b2d5820244846982772499370842_float
{
float3 ObjectSpaceNormal;
float3 WorldSpaceNormal;
float3 WorldSpaceTangent;
float3 WorldSpaceBiTangent;
float3 ObjectSpacePosition;
};

void SG_PASGVerticalBlend_e9f0b2d5820244846982772499370842_float(float _Sloap_Contrast, UnityTexture2D _Slope_Noise_Texture, float _Noise_Texture_Size, float3 _Explicit_Normal, float _Sloap_Offset, Bindings_PASGVerticalBlend_e9f0b2d5820244846982772499370842_float IN, out float3 OutVector3_1)
{
float3 _Property_5509c7fb30e445229f496a71c6e9c192_Out_0_Vector3 = _Explicit_Normal;
float3 _Transform_9c9f921cd0934012ac0d281c8e40a727_Out_1_Vector3;
{
float3x3 tangentTransform = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
_Transform_9c9f921cd0934012ac0d281c8e40a727_Out_1_Vector3 = TransformTangentToWorld(_Property_5509c7fb30e445229f496a71c6e9c192_Out_0_Vector3.xyz, tangentTransform, true);
}
float _DotProduct_4bc69611266041259c8da1b387474ec9_Out_2_Float;
Unity_DotProduct_float3(_Transform_9c9f921cd0934012ac0d281c8e40a727_Out_1_Vector3, float3(0, 1, 0), _DotProduct_4bc69611266041259c8da1b387474ec9_Out_2_Float);
float _Property_6a995c4e80824d6fa7d458ddcdc4eec3_Out_0_Float = _Sloap_Offset;
float _Add_a196acc146d1466fb309ec050e77feb2_Out_2_Float;
Unity_Add_float(_DotProduct_4bc69611266041259c8da1b387474ec9_Out_2_Float, _Property_6a995c4e80824d6fa7d458ddcdc4eec3_Out_0_Float, _Add_a196acc146d1466fb309ec050e77feb2_Out_2_Float);
float _Lerp_a1fed41c0847449ab72ae8fe4775270c_Out_3_Float;
Unity_Lerp_float(float(0), float(3), _Add_a196acc146d1466fb309ec050e77feb2_Out_2_Float, _Lerp_a1fed41c0847449ab72ae8fe4775270c_Out_3_Float);
float _Clamp_d4d6e762b7b1422d936b98d7f9429754_Out_3_Float;
Unity_Clamp_float(_Lerp_a1fed41c0847449ab72ae8fe4775270c_Out_3_Float, float(0), float(1), _Clamp_d4d6e762b7b1422d936b98d7f9429754_Out_3_Float);
float _Property_985b04ddd40847029dbfa829fbcaeed1_Out_0_Float = _Sloap_Contrast;
float3 _Contrast_2864897542064adcb4b61fcccadaf026_Out_2_Vector3;
Unity_Contrast_float((_Clamp_d4d6e762b7b1422d936b98d7f9429754_Out_3_Float.xxx), _Property_985b04ddd40847029dbfa829fbcaeed1_Out_0_Float, _Contrast_2864897542064adcb4b61fcccadaf026_Out_2_Vector3);
float3 _Clamp_b8f1f82d045f437a910a54b62bb2df81_Out_3_Vector3;
Unity_Clamp_float3(_Contrast_2864897542064adcb4b61fcccadaf026_Out_2_Vector3, float3(0, 0, 0), float3(1, 1, 1), _Clamp_b8f1f82d045f437a910a54b62bb2df81_Out_3_Vector3);
float3 _Transform_229ffe25002343a39c8caaa3d7f6df91_Out_1_Vector3;
_Transform_229ffe25002343a39c8caaa3d7f6df91_Out_1_Vector3 = TransformObjectToWorldDir(IN.ObjectSpacePosition.xyz, true);
float3 _Vector3_ec86905b34c54695bed5b7d1975cb6ce_Out_0_Vector3 = float3(float(0), float(2), float(0));
float _DotProduct_af3cb3caf6e4473c8161868b15c0255b_Out_2_Float;
Unity_DotProduct_float3(_Transform_229ffe25002343a39c8caaa3d7f6df91_Out_1_Vector3, _Vector3_ec86905b34c54695bed5b7d1975cb6ce_Out_0_Vector3, _DotProduct_af3cb3caf6e4473c8161868b15c0255b_Out_2_Float);
float _Add_47be9b48b4c94dd28e8c3db7f6ba1a05_Out_2_Float;
Unity_Add_float(_Property_6a995c4e80824d6fa7d458ddcdc4eec3_Out_0_Float, _DotProduct_af3cb3caf6e4473c8161868b15c0255b_Out_2_Float, _Add_47be9b48b4c94dd28e8c3db7f6ba1a05_Out_2_Float);
float _Multiply_35491cd334cb4e34a76b48de2bc5814a_Out_2_Float;
Unity_Multiply_float_float(_Add_47be9b48b4c94dd28e8c3db7f6ba1a05_Out_2_Float, 2, _Multiply_35491cd334cb4e34a76b48de2bc5814a_Out_2_Float);
float _Clamp_ccd0969e9da6420f94e7113654fd480e_Out_3_Float;
Unity_Clamp_float(_Multiply_35491cd334cb4e34a76b48de2bc5814a_Out_2_Float, float(0), float(1), _Clamp_ccd0969e9da6420f94e7113654fd480e_Out_3_Float);
float3 _Contrast_454ba7c8fb3a4d03951b073867fa5f14_Out_2_Vector3;
Unity_Contrast_float((_Clamp_ccd0969e9da6420f94e7113654fd480e_Out_3_Float.xxx), _Property_985b04ddd40847029dbfa829fbcaeed1_Out_0_Float, _Contrast_454ba7c8fb3a4d03951b073867fa5f14_Out_2_Vector3);
float3 _Clamp_1ba5cf0aee8245bd9dca155954c3df81_Out_3_Vector3;
Unity_Clamp_float3(_Contrast_454ba7c8fb3a4d03951b073867fa5f14_Out_2_Vector3, float3(0, 0, 0), float3(1, 1, 1), _Clamp_1ba5cf0aee8245bd9dca155954c3df81_Out_3_Vector3);
UnityTexture2D _Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D = _Slope_Noise_Texture;
float _Property_8e663ddd1476448d948a9de1bf1f83a9_Out_0_Float = _Noise_Texture_Size;
float3 Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_UV = IN.ObjectSpacePosition * _Property_8e663ddd1476448d948a9de1bf1f83a9_Out_0_Float;
float3 Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend = SafePositivePow_float(IN.ObjectSpaceNormal, min(float(1), floor(log2(Min_float())/log2(1/sqrt(3)))) );
Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend /= dot(Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend, 1.0);
float4 Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_X = SAMPLE_TEXTURE2D(_Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.tex, _Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.samplerstate, Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_UV.zy);
float4 Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Y = SAMPLE_TEXTURE2D(_Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.tex, _Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.samplerstate, Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_UV.xz);
float4 Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Z = SAMPLE_TEXTURE2D(_Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.tex, _Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.samplerstate, Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_UV.xy);
float4 _Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Out_0_Vector4 = Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_X * Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend.x + Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Y * Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend.y + Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Z * Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend.z;
float _Swizzle_90fb1902a2bd4070b4f9e27ac287ca97_Out_1_Float = (_Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Out_0_Vector4.xy).y;
float _Clamp_f32c97c6ccbf44649bd757503ccdbe71_Out_3_Float;
Unity_Clamp_float(_Swizzle_90fb1902a2bd4070b4f9e27ac287ca97_Out_1_Float, float(0), float(1), _Clamp_f32c97c6ccbf44649bd757503ccdbe71_Out_3_Float);
float _Add_92d15430b3c042719d65f1252bc35d7a_Out_2_Float;
Unity_Add_float(_Multiply_35491cd334cb4e34a76b48de2bc5814a_Out_2_Float, _Clamp_f32c97c6ccbf44649bd757503ccdbe71_Out_3_Float, _Add_92d15430b3c042719d65f1252bc35d7a_Out_2_Float);
float3 _Contrast_c2fe106c51f0498e987cf31090ec563d_Out_2_Vector3;
Unity_Contrast_float((_Add_92d15430b3c042719d65f1252bc35d7a_Out_2_Float.xxx), _Property_985b04ddd40847029dbfa829fbcaeed1_Out_0_Float, _Contrast_c2fe106c51f0498e987cf31090ec563d_Out_2_Vector3);
float3 _Clamp_05438405442c47cb98e571d128277bbe_Out_3_Vector3;
Unity_Clamp_float3(_Contrast_c2fe106c51f0498e987cf31090ec563d_Out_2_Vector3, float3(0, 0, 0), float3(1, 1, 1), _Clamp_05438405442c47cb98e571d128277bbe_Out_3_Vector3);
#if defined(_EDGEBLENDING_NORMALBLENDING)
float3 _EdgeBlending_cdbe33a15cf6452b8881ecea22adcb84_Out_0_Vector3 = _Clamp_b8f1f82d045f437a910a54b62bb2df81_Out_3_Vector3;
#elif defined(_EDGEBLENDING_VERTEXBLENDING)
float3 _EdgeBlending_cdbe33a15cf6452b8881ecea22adcb84_Out_0_Vector3 = _Clamp_1ba5cf0aee8245bd9dca155954c3df81_Out_3_Vector3;
#else
float3 _EdgeBlending_cdbe33a15cf6452b8881ecea22adcb84_Out_0_Vector3 = _Clamp_05438405442c47cb98e571d128277bbe_Out_3_Vector3;
#endif
OutVector3_1 = _EdgeBlending_cdbe33a15cf6452b8881ecea22adcb84_Out_0_Vector3;
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Branch_float3(float Predicate, float3 True, float3 False, out float3 Out)
{
    Out = Predicate ? True : False;
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
description.Position = IN.ObjectSpacePosition;
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
float Alpha;
float AlphaClipThreshold;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
float _Property_868c674f556d42be9071980dc60f4aed_Out_0_Boolean = _EnableCoverage;
float _Property_a0b21575a96c42a2b05aa3518645b2f4_Out_0_Float = _UV_Size;
UnityTexture2D _Property_4d248f4a9de94d7bb9a5c3c7c4c10a68_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_ColorMap, sampler_ColorMap, _ColorMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _Property_061dd20995c54e4b8c0357aa709c6e56_Out_0_Vector4 = _ColorTint;
UnityTexture2D _Property_cc904da6b4714cd6a9559b3bcf6ba7a6_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_NormalMap, sampler_NormalMap, _NormalMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_49f412000c0b4f18863fb7afae8b43fc_Out_0_Float = _NormalIntensity;
UnityTexture2D _Property_3fcfe917c75d4a46bfe50da31a24c74f_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_ORMMap, sampler_ORMMap, _ORMMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_bd35b765c62349f285447de81728f0c8_Out_0_Float = _RoughnessIntensity;
float _Property_f125a72836c24b0fa5ef72823c8e6452_Out_0_Float = _AOIntensity;
float _Property_0251a196075947af9d3ad28d6875a5ef_Out_0_Float = _MetallicIntensity;
UnityTexture2D _Property_47ecc092fbd144218fe6440250e367b3_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Emissive_Map, sampler_Emissive_Map, _Emissive_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _Property_c065f0d4df8c40528116ee8e516b2221_Out_0_Vector4 = _EmissiveColor;
float _Property_0038e41f028740e4abc1958ca58302a1_Out_0_Float = _EmissiveIntensity;
Bindings_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe;
_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe.uv0 = IN.uv0;
float4 _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_BaseColor_5_Vector4;
float3 _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Normal_1_Vector3;
float4 _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Emissive_6_Vector4;
float _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_AmbientOcclusion_4_Float;
float _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Roughness_3_Float;
float _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Metallic_2_Float;
SG_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float(_Property_a0b21575a96c42a2b05aa3518645b2f4_Out_0_Float, _Property_4d248f4a9de94d7bb9a5c3c7c4c10a68_Out_0_Texture2D, _Property_061dd20995c54e4b8c0357aa709c6e56_Out_0_Vector4, _Property_cc904da6b4714cd6a9559b3bcf6ba7a6_Out_0_Texture2D, _Property_49f412000c0b4f18863fb7afae8b43fc_Out_0_Float, _Property_3fcfe917c75d4a46bfe50da31a24c74f_Out_0_Texture2D, _Property_bd35b765c62349f285447de81728f0c8_Out_0_Float, _Property_f125a72836c24b0fa5ef72823c8e6452_Out_0_Float, _Property_0251a196075947af9d3ad28d6875a5ef_Out_0_Float, _Property_47ecc092fbd144218fe6440250e367b3_Out_0_Texture2D, _Property_c065f0d4df8c40528116ee8e516b2221_Out_0_Vector4, _Property_0038e41f028740e4abc1958ca58302a1_Out_0_Float, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_BaseColor_5_Vector4, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Normal_1_Vector3, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Emissive_6_Vector4, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_AmbientOcclusion_4_Float, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Roughness_3_Float, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Metallic_2_Float);
float _Property_623a0c9d694c4c33b6256bde952c53e5_Out_0_Float = _CoverageSize;
UnityTexture2D _Property_58d380b2034145c8b7d2eda637dbc296_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_CoverageColorMap, sampler_CoverageColorMap, _CoverageColorMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
UnityTexture2D _Property_f324dd6628ea45b1a630d750fa3d6b66_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_CoverageNormalMap, sampler_CoverageNormalMap, _CoverageNormalMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_b00b34515282450295cc4f4eb7b4a0ce_Out_0_Float = _CoverageNormalIntensity;
UnityTexture2D _Property_24ae6469bfeb4dada6fb7c7d39985cb3_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_CoverageORMMap, sampler_CoverageORMMap, _CoverageORMMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_03c29a27092f4bbb8a4fcc3b6e7b2909_Out_0_Float = _CoverageRoughnessIntensity;
float _Property_07cad64e572e4602a346b7dd90c3eb21_Out_0_Float = _CoverageMetallicIntensity;
Bindings_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c;
_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c.uv0 = IN.uv0;
float4 _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_BaseColor_5_Vector4;
float3 _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Normal_1_Vector3;
float4 _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Emissive_6_Vector4;
float _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_AmbientOcclusion_4_Float;
float _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Roughness_3_Float;
float _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Metallic_2_Float;
SG_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float(_Property_623a0c9d694c4c33b6256bde952c53e5_Out_0_Float, _Property_58d380b2034145c8b7d2eda637dbc296_Out_0_Texture2D, float4 (1, 1, 1, 1), _Property_f324dd6628ea45b1a630d750fa3d6b66_Out_0_Texture2D, _Property_b00b34515282450295cc4f4eb7b4a0ce_Out_0_Float, _Property_24ae6469bfeb4dada6fb7c7d39985cb3_Out_0_Texture2D, _Property_03c29a27092f4bbb8a4fcc3b6e7b2909_Out_0_Float, float(0), _Property_07cad64e572e4602a346b7dd90c3eb21_Out_0_Float, UnityBuildTexture2DStructNoScale(_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D), float4 (0, 0, 0, 0), float(0), _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_BaseColor_5_Vector4, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Normal_1_Vector3, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Emissive_6_Vector4, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_AmbientOcclusion_4_Float, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Roughness_3_Float, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Metallic_2_Float);
float _Property_0785fc58d71d4d63abb8c1102aa1a1b7_Out_0_Float = _SloapContrast;
UnityTexture2D _Property_12921d1841154c59b11aefccb2cbca31_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_SlopeNoiseTexture, sampler_SlopeNoiseTexture, _SlopeNoiseTexture_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_10dd46128137476fb3fd49de6154e94c_Out_0_Float = _NoiseTextureSize;
float _Property_a654ef8b1e2a4529ab82f92b3ae202ec_Out_0_Float = _SloapOffset;
Bindings_PASGVerticalBlend_e9f0b2d5820244846982772499370842_float _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d;
_PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d.ObjectSpaceNormal = IN.ObjectSpaceNormal;
_PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d.WorldSpaceNormal = IN.WorldSpaceNormal;
_PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d.WorldSpaceTangent = IN.WorldSpaceTangent;
_PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d.WorldSpaceBiTangent = IN.WorldSpaceBiTangent;
_PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d.ObjectSpacePosition = IN.ObjectSpacePosition;
float3 _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d_OutVector3_1_Vector3;
SG_PASGVerticalBlend_e9f0b2d5820244846982772499370842_float(_Property_0785fc58d71d4d63abb8c1102aa1a1b7_Out_0_Float, _Property_12921d1841154c59b11aefccb2cbca31_Out_0_Texture2D, _Property_10dd46128137476fb3fd49de6154e94c_Out_0_Float, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Normal_1_Vector3, _Property_a654ef8b1e2a4529ab82f92b3ae202ec_Out_0_Float, _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d, _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d_OutVector3_1_Vector3);
float3 _Lerp_8ef5a2e39e934fb09e1bc6858ba97959_Out_3_Vector3;
Unity_Lerp_float3((_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_BaseColor_5_Vector4.xyz), (_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_BaseColor_5_Vector4.xyz), _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d_OutVector3_1_Vector3, _Lerp_8ef5a2e39e934fb09e1bc6858ba97959_Out_3_Vector3);
float3 _Branch_72adc4bf3fd64427a9a9035afa6f856a_Out_3_Vector3;
Unity_Branch_float3(_Property_868c674f556d42be9071980dc60f4aed_Out_0_Boolean, _Lerp_8ef5a2e39e934fb09e1bc6858ba97959_Out_3_Vector3, (_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_BaseColor_5_Vector4.xyz), _Branch_72adc4bf3fd64427a9a9035afa6f856a_Out_3_Vector3);
UnityTexture2D _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_ColorMap_1, sampler_ColorMap_1, _ColorMap_1_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.tex, _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.samplerstate, _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4, _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_R_4_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.r;
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_G_5_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.g;
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_B_6_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.b;
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_A_7_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.a;
float _Property_7a43d5f1d3df46c2869bc70eebee1f91_Out_0_Float = _EmissiveIntensity_1;
surface.BaseColor = _Branch_72adc4bf3fd64427a9a9035afa6f856a_Out_3_Vector3;
surface.Emission = (_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Emissive_6_Vector4.xyz);
surface.Alpha = (_SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4).x;
surface.AlphaClipThreshold = _Property_7a43d5f1d3df46c2869bc70eebee1f91_Out_0_Float;
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

    // use bitangent on the fly like in hdrp
    // IMPORTANT! If we ever support Flip on double sided materials ensure bitangent and tangent are NOT flipped.
    float crossSign = (input.tangentWS.w > 0.0 ? 1.0 : -1.0)* GetOddNegativeScale();
    float3 bitang = crossSign * cross(input.normalWS.xyz, input.tangentWS.xyz);

    output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph
    output.ObjectSpaceNormal = normalize(mul(output.WorldSpaceNormal, (float3x3) UNITY_MATRIX_M));           // transposed multiplication by inverse matrix to handle normal scale

    // to pr               eserve mikktspace compliance we use same scale renormFactor as was used on the normal.
    // This                is explained in section 2.2 in "surface gradient based bump mapping framework"
    output.WorldSpaceTangent = renormFactor * input.tangentWS.xyz;
    output.WorldSpaceBiTangent = renormFactor * bitang;

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
#pragma shader_feature_local_fragment _ _ALPHATEST_ON
#pragma shader_feature_local _EDGEBLENDING_NORMALBLENDING _EDGEBLENDING_VERTEXBLENDING _EDGEBLENDING_NOISEBLENDING

#if defined(_EDGEBLENDING_NORMALBLENDING)
#define KEYWORD_PERMUTATION_0
#elif defined(_EDGEBLENDING_VERTEXBLENDING)
#define KEYWORD_PERMUTATION_1
#else
#define KEYWORD_PERMUTATION_2
#endif


// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#define VARYINGS_NEED_TEXCOORD0
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
 float4 uv0 : TEXCOORD0;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
 float4 positionCS : SV_POSITION;
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
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
 float4 texCoord0 : INTERP0;
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

float4 _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D_TexelSize;
float4 _ColorMap_TexelSize;
float4 _ColorMap_1_TexelSize;
float4 _NormalMap_TexelSize;
float4 _ColorTint;
float _NormalIntensity;
float _UV_Size;
float4 _ORMMap_TexelSize;
float _RoughnessIntensity;
float _AOIntensity;
float _MetallicIntensity;
float4 _EmissiveColor;
float _EmissiveIntensity_1;
float4 _Emissive_Map_TexelSize;
float _EmissiveIntensity;
float _EnableCoverage;
float _CoverageSize;
float _CoverageNormalIntensity;
float _CoverageRoughnessIntensity;
float _CoverageMetallicIntensity;
float _NoiseTextureSize;
float4 _SlopeNoiseTexture_TexelSize;
float4 _CoverageColorMap_TexelSize;
float4 _CoverageORMMap_TexelSize;
float4 _CoverageNormalMap_TexelSize;
float _SloapOffset;
float _SloapContrast;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D);
SAMPLER(sampler_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D);
TEXTURE2D(_ColorMap);
SAMPLER(sampler_ColorMap);
TEXTURE2D(_ColorMap_1);
SAMPLER(sampler_ColorMap_1);
TEXTURE2D(_NormalMap);
SAMPLER(sampler_NormalMap);
TEXTURE2D(_ORMMap);
SAMPLER(sampler_ORMMap);
TEXTURE2D(_Emissive_Map);
SAMPLER(sampler_Emissive_Map);
TEXTURE2D(_SlopeNoiseTexture);
SAMPLER(sampler_SlopeNoiseTexture);
TEXTURE2D(_CoverageColorMap);
SAMPLER(sampler_CoverageColorMap);
TEXTURE2D(_CoverageORMMap);
SAMPLER(sampler_CoverageORMMap);
TEXTURE2D(_CoverageNormalMap);
SAMPLER(sampler_CoverageNormalMap);

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
// GraphFunctions: <None>

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
description.Position = IN.ObjectSpacePosition;
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
float Alpha;
float AlphaClipThreshold;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
UnityTexture2D _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_ColorMap_1, sampler_ColorMap_1, _ColorMap_1_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.tex, _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.samplerstate, _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4, _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_R_4_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.r;
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_G_5_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.g;
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_B_6_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.b;
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_A_7_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.a;
float _Property_7a43d5f1d3df46c2869bc70eebee1f91_Out_0_Float = _EmissiveIntensity_1;
surface.Alpha = (_SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4).x;
surface.AlphaClipThreshold = _Property_7a43d5f1d3df46c2869bc70eebee1f91_Out_0_Float;
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
    Name "ScenePickingPass"
    Tags
    {
        "LightMode" = "Picking"
    }

// Render State
Cull [_Cull]

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
#pragma shader_feature_local_fragment _ _ALPHATEST_ON
#pragma shader_feature_local _EDGEBLENDING_NORMALBLENDING _EDGEBLENDING_VERTEXBLENDING _EDGEBLENDING_NOISEBLENDING

#if defined(_EDGEBLENDING_NORMALBLENDING)
#define KEYWORD_PERMUTATION_0
#elif defined(_EDGEBLENDING_VERTEXBLENDING)
#define KEYWORD_PERMUTATION_1
#else
#define KEYWORD_PERMUTATION_2
#endif


// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_NORMAL_WS
#define VARYINGS_NEED_TANGENT_WS
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
 float3 ObjectSpaceNormal;
 float3 WorldSpaceNormal;
 float3 WorldSpaceTangent;
 float3 WorldSpaceBiTangent;
 float3 ObjectSpacePosition;
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
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

float4 _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D_TexelSize;
float4 _ColorMap_TexelSize;
float4 _ColorMap_1_TexelSize;
float4 _NormalMap_TexelSize;
float4 _ColorTint;
float _NormalIntensity;
float _UV_Size;
float4 _ORMMap_TexelSize;
float _RoughnessIntensity;
float _AOIntensity;
float _MetallicIntensity;
float4 _EmissiveColor;
float _EmissiveIntensity_1;
float4 _Emissive_Map_TexelSize;
float _EmissiveIntensity;
float _EnableCoverage;
float _CoverageSize;
float _CoverageNormalIntensity;
float _CoverageRoughnessIntensity;
float _CoverageMetallicIntensity;
float _NoiseTextureSize;
float4 _SlopeNoiseTexture_TexelSize;
float4 _CoverageColorMap_TexelSize;
float4 _CoverageORMMap_TexelSize;
float4 _CoverageNormalMap_TexelSize;
float _SloapOffset;
float _SloapContrast;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D);
SAMPLER(sampler_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D);
TEXTURE2D(_ColorMap);
SAMPLER(sampler_ColorMap);
TEXTURE2D(_ColorMap_1);
SAMPLER(sampler_ColorMap_1);
TEXTURE2D(_NormalMap);
SAMPLER(sampler_NormalMap);
TEXTURE2D(_ORMMap);
SAMPLER(sampler_ORMMap);
TEXTURE2D(_Emissive_Map);
SAMPLER(sampler_Emissive_Map);
TEXTURE2D(_SlopeNoiseTexture);
SAMPLER(sampler_SlopeNoiseTexture);
TEXTURE2D(_CoverageColorMap);
SAMPLER(sampler_CoverageColorMap);
TEXTURE2D(_CoverageORMMap);
SAMPLER(sampler_CoverageORMMap);
TEXTURE2D(_CoverageNormalMap);
SAMPLER(sampler_CoverageNormalMap);

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

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

void Unity_NormalStrength_float(float3 In, float Strength, out float3 Out)
{
    Out = float3(In.rg * Strength, lerp(1, In.b, saturate(Strength)));
}

void Unity_Lerp_float(float A, float B, float T, out float Out)
{
    Out = lerp(A, B, T);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
}

struct Bindings_PASGORMUnpacker_abcd41e335c426a4bb0f8de17b4b5f96_float
{
};

void SG_PASGORMUnpacker_abcd41e335c426a4bb0f8de17b4b5f96_float(float2 _UV, UnityTexture2D _ORM_Map, float _AO_Intensity, float _Roughness_Intensity, float _Metallic_Intensity, Bindings_PASGORMUnpacker_abcd41e335c426a4bb0f8de17b4b5f96_float IN, out float Ambient_Occlusion_1, out float Roughness_2, out float Metallic_3)
{
float _Float_2c47c335e2304faa927a789be883bb19_Out_0_Float = float(1);
UnityTexture2D _Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D = _ORM_Map;
float2 _Property_ad3893fa315b474ea60e7ee3e265957c_Out_0_Vector2 = _UV;
float4 _SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D.tex, _Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D.samplerstate, _Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D.GetTransformedUV(_Property_ad3893fa315b474ea60e7ee3e265957c_Out_0_Vector2) );
if (_Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4, _Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_719055d2c0184958bd1329f59130cce0_R_4_Float = _SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4.r;
float _SampleTexture2D_719055d2c0184958bd1329f59130cce0_G_5_Float = _SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4.g;
float _SampleTexture2D_719055d2c0184958bd1329f59130cce0_B_6_Float = _SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4.b;
float _SampleTexture2D_719055d2c0184958bd1329f59130cce0_A_7_Float = _SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4.a;
float _Property_90898696aae3458aa180c6f15d96aaee_Out_0_Float = _AO_Intensity;
float _Lerp_bf670034cefc4c6d9e02b57f5394811d_Out_3_Float;
Unity_Lerp_float(_Float_2c47c335e2304faa927a789be883bb19_Out_0_Float, _SampleTexture2D_719055d2c0184958bd1329f59130cce0_R_4_Float, _Property_90898696aae3458aa180c6f15d96aaee_Out_0_Float, _Lerp_bf670034cefc4c6d9e02b57f5394811d_Out_3_Float);
float _Property_e77944e4743744198e931506300c92ca_Out_0_Float = _Roughness_Intensity;
float _Multiply_43e873dc525645aab065eb6fc56ad77f_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_719055d2c0184958bd1329f59130cce0_G_5_Float, _Property_e77944e4743744198e931506300c92ca_Out_0_Float, _Multiply_43e873dc525645aab065eb6fc56ad77f_Out_2_Float);
float _OneMinus_3225b57b02ed4eaf9c118419fbefc479_Out_1_Float;
Unity_OneMinus_float(_Multiply_43e873dc525645aab065eb6fc56ad77f_Out_2_Float, _OneMinus_3225b57b02ed4eaf9c118419fbefc479_Out_1_Float);
float _Property_c0c103f0dfcd475fa428b0e043905944_Out_0_Float = _Metallic_Intensity;
float _Multiply_38b5ca9e78ba40d9b861ffc15889bd7f_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_719055d2c0184958bd1329f59130cce0_B_6_Float, _Property_c0c103f0dfcd475fa428b0e043905944_Out_0_Float, _Multiply_38b5ca9e78ba40d9b861ffc15889bd7f_Out_2_Float);
Ambient_Occlusion_1 = _Lerp_bf670034cefc4c6d9e02b57f5394811d_Out_3_Float;
Roughness_2 = _OneMinus_3225b57b02ed4eaf9c118419fbefc479_Out_1_Float;
Metallic_3 = _Multiply_38b5ca9e78ba40d9b861ffc15889bd7f_Out_2_Float;
}

struct Bindings_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float
{
half4 uv0;
};

void SG_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float(float _Texture_Size, UnityTexture2D _Color_Map, float4 _Color_Tint, UnityTexture2D _Normal_Map, float _Normal_Intensity, UnityTexture2D _ORM_Map, float _Roughness_Intensity, float _AO_Intensity, float _Metallic_Intensity, UnityTexture2D _Emissive_Map, float4 _Emissive_Color, float _Emissive_Intensity, Bindings_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float IN, out float4 Base_Color_5, out float3 Normal_1, out float4 Emissive_6, out float Ambient_Occlusion_4, out float Roughness_3, out float Metallic_2)
{
UnityTexture2D _Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D = _Color_Map;
float _Property_6e866f25d28140918dd5249261b96e76_Out_0_Float = _Texture_Size;
float2 _TilingAndOffset_49c249d74ea54ccf979206ad2c3c0c00_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, (_Property_6e866f25d28140918dd5249261b96e76_Out_0_Float.xx), float2 (0, 0), _TilingAndOffset_49c249d74ea54ccf979206ad2c3c0c00_Out_3_Vector2);
float _Property_94852b2b32f549138996f0dfb35b0486_Out_0_Float = _Texture_Size;
float2 _Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2;
Unity_Multiply_float2_float2(_TilingAndOffset_49c249d74ea54ccf979206ad2c3c0c00_Out_3_Vector2, (_Property_94852b2b32f549138996f0dfb35b0486_Out_0_Float.xx), _Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2);
float4 _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D.tex, _Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D.samplerstate, _Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D.GetTransformedUV(_Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2) );
if (_Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4, _Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_R_4_Float = _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4.r;
float _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_G_5_Float = _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4.g;
float _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_B_6_Float = _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4.b;
float _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_A_7_Float = _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4.a;
float4 _Property_c081d1852d284e4fa4bbb04ea3adf35e_Out_0_Vector4 = _Color_Tint;
float4 _Multiply_ff3b38102028498db7cc80acf5e42362_Out_2_Vector4;
Unity_Multiply_float4_float4(_SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4, _Property_c081d1852d284e4fa4bbb04ea3adf35e_Out_0_Vector4, _Multiply_ff3b38102028498db7cc80acf5e42362_Out_2_Vector4);
UnityTexture2D _Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D = _Normal_Map;
float4 _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D.tex, _Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D.samplerstate, _Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D.GetTransformedUV(_Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2) );
if (_Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4, _Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4);
float _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_R_4_Float = _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.r;
float _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_G_5_Float = _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.g;
float _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_B_6_Float = _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.b;
float _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_A_7_Float = _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.a;
float _Property_8b1f839dfbcd4481b201427fbc7310ad_Out_0_Float = _Normal_Intensity;
float3 _NormalStrength_a218ef03cc884bf0b0962d85b628afe7_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.xyz), _Property_8b1f839dfbcd4481b201427fbc7310ad_Out_0_Float, _NormalStrength_a218ef03cc884bf0b0962d85b628afe7_Out_2_Vector3);
UnityTexture2D _Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D = _Emissive_Map;
float4 _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D.tex, _Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D.samplerstate, _Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D.GetTransformedUV(_Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2) );
if (_Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4, _Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_R_4_Float = _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4.r;
float _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_G_5_Float = _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4.g;
float _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_B_6_Float = _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4.b;
float _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_A_7_Float = _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4.a;
float4 _Property_3a2f8dbe365d427e89744f8eb8dabd44_Out_0_Vector4 = _Emissive_Color;
float4 _Multiply_1efa7dc8f3994d8ea213351379d48448_Out_2_Vector4;
Unity_Multiply_float4_float4(_SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4, _Property_3a2f8dbe365d427e89744f8eb8dabd44_Out_0_Vector4, _Multiply_1efa7dc8f3994d8ea213351379d48448_Out_2_Vector4);
float _Property_09c3fe7e22be43caa45c324f52ea8137_Out_0_Float = _Emissive_Intensity;
float4 _Multiply_43a1c0123fb34a25a5d21581f08926f8_Out_2_Vector4;
Unity_Multiply_float4_float4(_Multiply_1efa7dc8f3994d8ea213351379d48448_Out_2_Vector4, (_Property_09c3fe7e22be43caa45c324f52ea8137_Out_0_Float.xxxx), _Multiply_43a1c0123fb34a25a5d21581f08926f8_Out_2_Vector4);
UnityTexture2D _Property_ecbd8a4e938a458cabefe2480cada231_Out_0_Texture2D = _ORM_Map;
float _Property_2ca0dc7be16d47bfb0faf210c4d70757_Out_0_Float = _AO_Intensity;
float _Property_fb245dde136f4b2486e260e720f83c83_Out_0_Float = _Roughness_Intensity;
float _Property_0fcde0f68f364243886b868db18a8d27_Out_0_Float = _Metallic_Intensity;
Bindings_PASGORMUnpacker_abcd41e335c426a4bb0f8de17b4b5f96_float _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94;
half _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_AmbientOcclusion_1_Float;
half _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Roughness_2_Float;
half _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Metallic_3_Float;
SG_PASGORMUnpacker_abcd41e335c426a4bb0f8de17b4b5f96_float(_Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2, _Property_ecbd8a4e938a458cabefe2480cada231_Out_0_Texture2D, _Property_2ca0dc7be16d47bfb0faf210c4d70757_Out_0_Float, _Property_fb245dde136f4b2486e260e720f83c83_Out_0_Float, _Property_0fcde0f68f364243886b868db18a8d27_Out_0_Float, _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94, _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_AmbientOcclusion_1_Float, _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Roughness_2_Float, _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Metallic_3_Float);
Base_Color_5 = _Multiply_ff3b38102028498db7cc80acf5e42362_Out_2_Vector4;
Normal_1 = _NormalStrength_a218ef03cc884bf0b0962d85b628afe7_Out_2_Vector3;
Emissive_6 = _Multiply_43a1c0123fb34a25a5d21581f08926f8_Out_2_Vector4;
Ambient_Occlusion_4 = _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_AmbientOcclusion_1_Float;
Roughness_3 = _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Roughness_2_Float;
Metallic_2 = _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Metallic_3_Float;
}

void Unity_DotProduct_float3(float3 A, float3 B, out float Out)
{
    Out = dot(A, B);
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Clamp_float(float In, float Min, float Max, out float Out)
{
    Out = clamp(In, Min, Max);
}

void Unity_Contrast_float(float3 In, float Contrast, out float3 Out)
{
    float midpoint = pow(0.5, 2.2);
    Out =  (In - midpoint) * Contrast + midpoint;
}

void Unity_Clamp_float3(float3 In, float3 Min, float3 Max, out float3 Out)
{
    Out = clamp(In, Min, Max);
}

struct Bindings_PASGVerticalBlend_e9f0b2d5820244846982772499370842_float
{
float3 ObjectSpaceNormal;
float3 WorldSpaceNormal;
float3 WorldSpaceTangent;
float3 WorldSpaceBiTangent;
float3 ObjectSpacePosition;
};

void SG_PASGVerticalBlend_e9f0b2d5820244846982772499370842_float(float _Sloap_Contrast, UnityTexture2D _Slope_Noise_Texture, float _Noise_Texture_Size, float3 _Explicit_Normal, float _Sloap_Offset, Bindings_PASGVerticalBlend_e9f0b2d5820244846982772499370842_float IN, out float3 OutVector3_1)
{
float3 _Property_5509c7fb30e445229f496a71c6e9c192_Out_0_Vector3 = _Explicit_Normal;
float3 _Transform_9c9f921cd0934012ac0d281c8e40a727_Out_1_Vector3;
{
float3x3 tangentTransform = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
_Transform_9c9f921cd0934012ac0d281c8e40a727_Out_1_Vector3 = TransformTangentToWorld(_Property_5509c7fb30e445229f496a71c6e9c192_Out_0_Vector3.xyz, tangentTransform, true);
}
float _DotProduct_4bc69611266041259c8da1b387474ec9_Out_2_Float;
Unity_DotProduct_float3(_Transform_9c9f921cd0934012ac0d281c8e40a727_Out_1_Vector3, float3(0, 1, 0), _DotProduct_4bc69611266041259c8da1b387474ec9_Out_2_Float);
float _Property_6a995c4e80824d6fa7d458ddcdc4eec3_Out_0_Float = _Sloap_Offset;
float _Add_a196acc146d1466fb309ec050e77feb2_Out_2_Float;
Unity_Add_float(_DotProduct_4bc69611266041259c8da1b387474ec9_Out_2_Float, _Property_6a995c4e80824d6fa7d458ddcdc4eec3_Out_0_Float, _Add_a196acc146d1466fb309ec050e77feb2_Out_2_Float);
float _Lerp_a1fed41c0847449ab72ae8fe4775270c_Out_3_Float;
Unity_Lerp_float(float(0), float(3), _Add_a196acc146d1466fb309ec050e77feb2_Out_2_Float, _Lerp_a1fed41c0847449ab72ae8fe4775270c_Out_3_Float);
float _Clamp_d4d6e762b7b1422d936b98d7f9429754_Out_3_Float;
Unity_Clamp_float(_Lerp_a1fed41c0847449ab72ae8fe4775270c_Out_3_Float, float(0), float(1), _Clamp_d4d6e762b7b1422d936b98d7f9429754_Out_3_Float);
float _Property_985b04ddd40847029dbfa829fbcaeed1_Out_0_Float = _Sloap_Contrast;
float3 _Contrast_2864897542064adcb4b61fcccadaf026_Out_2_Vector3;
Unity_Contrast_float((_Clamp_d4d6e762b7b1422d936b98d7f9429754_Out_3_Float.xxx), _Property_985b04ddd40847029dbfa829fbcaeed1_Out_0_Float, _Contrast_2864897542064adcb4b61fcccadaf026_Out_2_Vector3);
float3 _Clamp_b8f1f82d045f437a910a54b62bb2df81_Out_3_Vector3;
Unity_Clamp_float3(_Contrast_2864897542064adcb4b61fcccadaf026_Out_2_Vector3, float3(0, 0, 0), float3(1, 1, 1), _Clamp_b8f1f82d045f437a910a54b62bb2df81_Out_3_Vector3);
float3 _Transform_229ffe25002343a39c8caaa3d7f6df91_Out_1_Vector3;
_Transform_229ffe25002343a39c8caaa3d7f6df91_Out_1_Vector3 = TransformObjectToWorldDir(IN.ObjectSpacePosition.xyz, true);
float3 _Vector3_ec86905b34c54695bed5b7d1975cb6ce_Out_0_Vector3 = float3(float(0), float(2), float(0));
float _DotProduct_af3cb3caf6e4473c8161868b15c0255b_Out_2_Float;
Unity_DotProduct_float3(_Transform_229ffe25002343a39c8caaa3d7f6df91_Out_1_Vector3, _Vector3_ec86905b34c54695bed5b7d1975cb6ce_Out_0_Vector3, _DotProduct_af3cb3caf6e4473c8161868b15c0255b_Out_2_Float);
float _Add_47be9b48b4c94dd28e8c3db7f6ba1a05_Out_2_Float;
Unity_Add_float(_Property_6a995c4e80824d6fa7d458ddcdc4eec3_Out_0_Float, _DotProduct_af3cb3caf6e4473c8161868b15c0255b_Out_2_Float, _Add_47be9b48b4c94dd28e8c3db7f6ba1a05_Out_2_Float);
float _Multiply_35491cd334cb4e34a76b48de2bc5814a_Out_2_Float;
Unity_Multiply_float_float(_Add_47be9b48b4c94dd28e8c3db7f6ba1a05_Out_2_Float, 2, _Multiply_35491cd334cb4e34a76b48de2bc5814a_Out_2_Float);
float _Clamp_ccd0969e9da6420f94e7113654fd480e_Out_3_Float;
Unity_Clamp_float(_Multiply_35491cd334cb4e34a76b48de2bc5814a_Out_2_Float, float(0), float(1), _Clamp_ccd0969e9da6420f94e7113654fd480e_Out_3_Float);
float3 _Contrast_454ba7c8fb3a4d03951b073867fa5f14_Out_2_Vector3;
Unity_Contrast_float((_Clamp_ccd0969e9da6420f94e7113654fd480e_Out_3_Float.xxx), _Property_985b04ddd40847029dbfa829fbcaeed1_Out_0_Float, _Contrast_454ba7c8fb3a4d03951b073867fa5f14_Out_2_Vector3);
float3 _Clamp_1ba5cf0aee8245bd9dca155954c3df81_Out_3_Vector3;
Unity_Clamp_float3(_Contrast_454ba7c8fb3a4d03951b073867fa5f14_Out_2_Vector3, float3(0, 0, 0), float3(1, 1, 1), _Clamp_1ba5cf0aee8245bd9dca155954c3df81_Out_3_Vector3);
UnityTexture2D _Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D = _Slope_Noise_Texture;
float _Property_8e663ddd1476448d948a9de1bf1f83a9_Out_0_Float = _Noise_Texture_Size;
float3 Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_UV = IN.ObjectSpacePosition * _Property_8e663ddd1476448d948a9de1bf1f83a9_Out_0_Float;
float3 Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend = SafePositivePow_float(IN.ObjectSpaceNormal, min(float(1), floor(log2(Min_float())/log2(1/sqrt(3)))) );
Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend /= dot(Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend, 1.0);
float4 Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_X = SAMPLE_TEXTURE2D(_Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.tex, _Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.samplerstate, Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_UV.zy);
float4 Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Y = SAMPLE_TEXTURE2D(_Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.tex, _Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.samplerstate, Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_UV.xz);
float4 Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Z = SAMPLE_TEXTURE2D(_Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.tex, _Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.samplerstate, Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_UV.xy);
float4 _Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Out_0_Vector4 = Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_X * Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend.x + Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Y * Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend.y + Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Z * Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend.z;
float _Swizzle_90fb1902a2bd4070b4f9e27ac287ca97_Out_1_Float = (_Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Out_0_Vector4.xy).y;
float _Clamp_f32c97c6ccbf44649bd757503ccdbe71_Out_3_Float;
Unity_Clamp_float(_Swizzle_90fb1902a2bd4070b4f9e27ac287ca97_Out_1_Float, float(0), float(1), _Clamp_f32c97c6ccbf44649bd757503ccdbe71_Out_3_Float);
float _Add_92d15430b3c042719d65f1252bc35d7a_Out_2_Float;
Unity_Add_float(_Multiply_35491cd334cb4e34a76b48de2bc5814a_Out_2_Float, _Clamp_f32c97c6ccbf44649bd757503ccdbe71_Out_3_Float, _Add_92d15430b3c042719d65f1252bc35d7a_Out_2_Float);
float3 _Contrast_c2fe106c51f0498e987cf31090ec563d_Out_2_Vector3;
Unity_Contrast_float((_Add_92d15430b3c042719d65f1252bc35d7a_Out_2_Float.xxx), _Property_985b04ddd40847029dbfa829fbcaeed1_Out_0_Float, _Contrast_c2fe106c51f0498e987cf31090ec563d_Out_2_Vector3);
float3 _Clamp_05438405442c47cb98e571d128277bbe_Out_3_Vector3;
Unity_Clamp_float3(_Contrast_c2fe106c51f0498e987cf31090ec563d_Out_2_Vector3, float3(0, 0, 0), float3(1, 1, 1), _Clamp_05438405442c47cb98e571d128277bbe_Out_3_Vector3);
#if defined(_EDGEBLENDING_NORMALBLENDING)
float3 _EdgeBlending_cdbe33a15cf6452b8881ecea22adcb84_Out_0_Vector3 = _Clamp_b8f1f82d045f437a910a54b62bb2df81_Out_3_Vector3;
#elif defined(_EDGEBLENDING_VERTEXBLENDING)
float3 _EdgeBlending_cdbe33a15cf6452b8881ecea22adcb84_Out_0_Vector3 = _Clamp_1ba5cf0aee8245bd9dca155954c3df81_Out_3_Vector3;
#else
float3 _EdgeBlending_cdbe33a15cf6452b8881ecea22adcb84_Out_0_Vector3 = _Clamp_05438405442c47cb98e571d128277bbe_Out_3_Vector3;
#endif
OutVector3_1 = _EdgeBlending_cdbe33a15cf6452b8881ecea22adcb84_Out_0_Vector3;
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Branch_float3(float Predicate, float3 True, float3 False, out float3 Out)
{
    Out = Predicate ? True : False;
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
description.Position = IN.ObjectSpacePosition;
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
float Alpha;
float AlphaClipThreshold;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
float _Property_868c674f556d42be9071980dc60f4aed_Out_0_Boolean = _EnableCoverage;
float _Property_a0b21575a96c42a2b05aa3518645b2f4_Out_0_Float = _UV_Size;
UnityTexture2D _Property_4d248f4a9de94d7bb9a5c3c7c4c10a68_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_ColorMap, sampler_ColorMap, _ColorMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _Property_061dd20995c54e4b8c0357aa709c6e56_Out_0_Vector4 = _ColorTint;
UnityTexture2D _Property_cc904da6b4714cd6a9559b3bcf6ba7a6_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_NormalMap, sampler_NormalMap, _NormalMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_49f412000c0b4f18863fb7afae8b43fc_Out_0_Float = _NormalIntensity;
UnityTexture2D _Property_3fcfe917c75d4a46bfe50da31a24c74f_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_ORMMap, sampler_ORMMap, _ORMMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_bd35b765c62349f285447de81728f0c8_Out_0_Float = _RoughnessIntensity;
float _Property_f125a72836c24b0fa5ef72823c8e6452_Out_0_Float = _AOIntensity;
float _Property_0251a196075947af9d3ad28d6875a5ef_Out_0_Float = _MetallicIntensity;
UnityTexture2D _Property_47ecc092fbd144218fe6440250e367b3_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Emissive_Map, sampler_Emissive_Map, _Emissive_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _Property_c065f0d4df8c40528116ee8e516b2221_Out_0_Vector4 = _EmissiveColor;
float _Property_0038e41f028740e4abc1958ca58302a1_Out_0_Float = _EmissiveIntensity;
Bindings_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe;
_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe.uv0 = IN.uv0;
float4 _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_BaseColor_5_Vector4;
float3 _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Normal_1_Vector3;
float4 _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Emissive_6_Vector4;
float _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_AmbientOcclusion_4_Float;
float _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Roughness_3_Float;
float _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Metallic_2_Float;
SG_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float(_Property_a0b21575a96c42a2b05aa3518645b2f4_Out_0_Float, _Property_4d248f4a9de94d7bb9a5c3c7c4c10a68_Out_0_Texture2D, _Property_061dd20995c54e4b8c0357aa709c6e56_Out_0_Vector4, _Property_cc904da6b4714cd6a9559b3bcf6ba7a6_Out_0_Texture2D, _Property_49f412000c0b4f18863fb7afae8b43fc_Out_0_Float, _Property_3fcfe917c75d4a46bfe50da31a24c74f_Out_0_Texture2D, _Property_bd35b765c62349f285447de81728f0c8_Out_0_Float, _Property_f125a72836c24b0fa5ef72823c8e6452_Out_0_Float, _Property_0251a196075947af9d3ad28d6875a5ef_Out_0_Float, _Property_47ecc092fbd144218fe6440250e367b3_Out_0_Texture2D, _Property_c065f0d4df8c40528116ee8e516b2221_Out_0_Vector4, _Property_0038e41f028740e4abc1958ca58302a1_Out_0_Float, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_BaseColor_5_Vector4, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Normal_1_Vector3, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Emissive_6_Vector4, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_AmbientOcclusion_4_Float, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Roughness_3_Float, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Metallic_2_Float);
float _Property_623a0c9d694c4c33b6256bde952c53e5_Out_0_Float = _CoverageSize;
UnityTexture2D _Property_58d380b2034145c8b7d2eda637dbc296_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_CoverageColorMap, sampler_CoverageColorMap, _CoverageColorMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
UnityTexture2D _Property_f324dd6628ea45b1a630d750fa3d6b66_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_CoverageNormalMap, sampler_CoverageNormalMap, _CoverageNormalMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_b00b34515282450295cc4f4eb7b4a0ce_Out_0_Float = _CoverageNormalIntensity;
UnityTexture2D _Property_24ae6469bfeb4dada6fb7c7d39985cb3_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_CoverageORMMap, sampler_CoverageORMMap, _CoverageORMMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_03c29a27092f4bbb8a4fcc3b6e7b2909_Out_0_Float = _CoverageRoughnessIntensity;
float _Property_07cad64e572e4602a346b7dd90c3eb21_Out_0_Float = _CoverageMetallicIntensity;
Bindings_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c;
_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c.uv0 = IN.uv0;
float4 _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_BaseColor_5_Vector4;
float3 _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Normal_1_Vector3;
float4 _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Emissive_6_Vector4;
float _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_AmbientOcclusion_4_Float;
float _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Roughness_3_Float;
float _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Metallic_2_Float;
SG_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float(_Property_623a0c9d694c4c33b6256bde952c53e5_Out_0_Float, _Property_58d380b2034145c8b7d2eda637dbc296_Out_0_Texture2D, float4 (1, 1, 1, 1), _Property_f324dd6628ea45b1a630d750fa3d6b66_Out_0_Texture2D, _Property_b00b34515282450295cc4f4eb7b4a0ce_Out_0_Float, _Property_24ae6469bfeb4dada6fb7c7d39985cb3_Out_0_Texture2D, _Property_03c29a27092f4bbb8a4fcc3b6e7b2909_Out_0_Float, float(0), _Property_07cad64e572e4602a346b7dd90c3eb21_Out_0_Float, UnityBuildTexture2DStructNoScale(_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D), float4 (0, 0, 0, 0), float(0), _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_BaseColor_5_Vector4, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Normal_1_Vector3, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Emissive_6_Vector4, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_AmbientOcclusion_4_Float, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Roughness_3_Float, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Metallic_2_Float);
float _Property_0785fc58d71d4d63abb8c1102aa1a1b7_Out_0_Float = _SloapContrast;
UnityTexture2D _Property_12921d1841154c59b11aefccb2cbca31_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_SlopeNoiseTexture, sampler_SlopeNoiseTexture, _SlopeNoiseTexture_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_10dd46128137476fb3fd49de6154e94c_Out_0_Float = _NoiseTextureSize;
float _Property_a654ef8b1e2a4529ab82f92b3ae202ec_Out_0_Float = _SloapOffset;
Bindings_PASGVerticalBlend_e9f0b2d5820244846982772499370842_float _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d;
_PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d.ObjectSpaceNormal = IN.ObjectSpaceNormal;
_PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d.WorldSpaceNormal = IN.WorldSpaceNormal;
_PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d.WorldSpaceTangent = IN.WorldSpaceTangent;
_PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d.WorldSpaceBiTangent = IN.WorldSpaceBiTangent;
_PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d.ObjectSpacePosition = IN.ObjectSpacePosition;
float3 _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d_OutVector3_1_Vector3;
SG_PASGVerticalBlend_e9f0b2d5820244846982772499370842_float(_Property_0785fc58d71d4d63abb8c1102aa1a1b7_Out_0_Float, _Property_12921d1841154c59b11aefccb2cbca31_Out_0_Texture2D, _Property_10dd46128137476fb3fd49de6154e94c_Out_0_Float, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Normal_1_Vector3, _Property_a654ef8b1e2a4529ab82f92b3ae202ec_Out_0_Float, _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d, _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d_OutVector3_1_Vector3);
float3 _Lerp_8ef5a2e39e934fb09e1bc6858ba97959_Out_3_Vector3;
Unity_Lerp_float3((_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_BaseColor_5_Vector4.xyz), (_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_BaseColor_5_Vector4.xyz), _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d_OutVector3_1_Vector3, _Lerp_8ef5a2e39e934fb09e1bc6858ba97959_Out_3_Vector3);
float3 _Branch_72adc4bf3fd64427a9a9035afa6f856a_Out_3_Vector3;
Unity_Branch_float3(_Property_868c674f556d42be9071980dc60f4aed_Out_0_Boolean, _Lerp_8ef5a2e39e934fb09e1bc6858ba97959_Out_3_Vector3, (_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_BaseColor_5_Vector4.xyz), _Branch_72adc4bf3fd64427a9a9035afa6f856a_Out_3_Vector3);
UnityTexture2D _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_ColorMap_1, sampler_ColorMap_1, _ColorMap_1_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.tex, _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.samplerstate, _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4, _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_R_4_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.r;
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_G_5_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.g;
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_B_6_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.b;
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_A_7_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.a;
float _Property_7a43d5f1d3df46c2869bc70eebee1f91_Out_0_Float = _EmissiveIntensity_1;
surface.BaseColor = _Branch_72adc4bf3fd64427a9a9035afa6f856a_Out_3_Vector3;
surface.Alpha = (_SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4).x;
surface.AlphaClipThreshold = _Property_7a43d5f1d3df46c2869bc70eebee1f91_Out_0_Float;
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

    // use bitangent on the fly like in hdrp
    // IMPORTANT! If we ever support Flip on double sided materials ensure bitangent and tangent are NOT flipped.
    float crossSign = (input.tangentWS.w > 0.0 ? 1.0 : -1.0)* GetOddNegativeScale();
    float3 bitang = crossSign * cross(input.normalWS.xyz, input.tangentWS.xyz);

    output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph
    output.ObjectSpaceNormal = normalize(mul(output.WorldSpaceNormal, (float3x3) UNITY_MATRIX_M));           // transposed multiplication by inverse matrix to handle normal scale

    // to pr               eserve mikktspace compliance we use same scale renormFactor as was used on the normal.
    // This                is explained in section 2.2 in "surface gradient based bump mapping framework"
    output.WorldSpaceTangent = renormFactor * input.tangentWS.xyz;
    output.WorldSpaceBiTangent = renormFactor * bitang;

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
Cull [_Cull]
Blend [_SrcBlend] [_DstBlend], [_SrcBlendAlpha] [_DstBlendAlpha]
ZTest [_ZTest]
ZWrite [_ZWrite]

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
#pragma shader_feature_local_fragment _ _ALPHATEST_ON
#pragma shader_feature_local _EDGEBLENDING_NORMALBLENDING _EDGEBLENDING_VERTEXBLENDING _EDGEBLENDING_NOISEBLENDING

#if defined(_EDGEBLENDING_NORMALBLENDING)
#define KEYWORD_PERMUTATION_0
#elif defined(_EDGEBLENDING_VERTEXBLENDING)
#define KEYWORD_PERMUTATION_1
#else
#define KEYWORD_PERMUTATION_2
#endif


// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_NORMAL_WS
#define VARYINGS_NEED_TANGENT_WS
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
 float3 ObjectSpaceNormal;
 float3 WorldSpaceNormal;
 float3 WorldSpaceTangent;
 float3 WorldSpaceBiTangent;
 float3 ObjectSpacePosition;
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
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

float4 _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D_TexelSize;
float4 _ColorMap_TexelSize;
float4 _ColorMap_1_TexelSize;
float4 _NormalMap_TexelSize;
float4 _ColorTint;
float _NormalIntensity;
float _UV_Size;
float4 _ORMMap_TexelSize;
float _RoughnessIntensity;
float _AOIntensity;
float _MetallicIntensity;
float4 _EmissiveColor;
float _EmissiveIntensity_1;
float4 _Emissive_Map_TexelSize;
float _EmissiveIntensity;
float _EnableCoverage;
float _CoverageSize;
float _CoverageNormalIntensity;
float _CoverageRoughnessIntensity;
float _CoverageMetallicIntensity;
float _NoiseTextureSize;
float4 _SlopeNoiseTexture_TexelSize;
float4 _CoverageColorMap_TexelSize;
float4 _CoverageORMMap_TexelSize;
float4 _CoverageNormalMap_TexelSize;
float _SloapOffset;
float _SloapContrast;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D);
SAMPLER(sampler_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D);
TEXTURE2D(_ColorMap);
SAMPLER(sampler_ColorMap);
TEXTURE2D(_ColorMap_1);
SAMPLER(sampler_ColorMap_1);
TEXTURE2D(_NormalMap);
SAMPLER(sampler_NormalMap);
TEXTURE2D(_ORMMap);
SAMPLER(sampler_ORMMap);
TEXTURE2D(_Emissive_Map);
SAMPLER(sampler_Emissive_Map);
TEXTURE2D(_SlopeNoiseTexture);
SAMPLER(sampler_SlopeNoiseTexture);
TEXTURE2D(_CoverageColorMap);
SAMPLER(sampler_CoverageColorMap);
TEXTURE2D(_CoverageORMMap);
SAMPLER(sampler_CoverageORMMap);
TEXTURE2D(_CoverageNormalMap);
SAMPLER(sampler_CoverageNormalMap);

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

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

void Unity_NormalStrength_float(float3 In, float Strength, out float3 Out)
{
    Out = float3(In.rg * Strength, lerp(1, In.b, saturate(Strength)));
}

void Unity_Lerp_float(float A, float B, float T, out float Out)
{
    Out = lerp(A, B, T);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
}

struct Bindings_PASGORMUnpacker_abcd41e335c426a4bb0f8de17b4b5f96_float
{
};

void SG_PASGORMUnpacker_abcd41e335c426a4bb0f8de17b4b5f96_float(float2 _UV, UnityTexture2D _ORM_Map, float _AO_Intensity, float _Roughness_Intensity, float _Metallic_Intensity, Bindings_PASGORMUnpacker_abcd41e335c426a4bb0f8de17b4b5f96_float IN, out float Ambient_Occlusion_1, out float Roughness_2, out float Metallic_3)
{
float _Float_2c47c335e2304faa927a789be883bb19_Out_0_Float = float(1);
UnityTexture2D _Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D = _ORM_Map;
float2 _Property_ad3893fa315b474ea60e7ee3e265957c_Out_0_Vector2 = _UV;
float4 _SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D.tex, _Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D.samplerstate, _Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D.GetTransformedUV(_Property_ad3893fa315b474ea60e7ee3e265957c_Out_0_Vector2) );
if (_Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4, _Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_719055d2c0184958bd1329f59130cce0_R_4_Float = _SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4.r;
float _SampleTexture2D_719055d2c0184958bd1329f59130cce0_G_5_Float = _SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4.g;
float _SampleTexture2D_719055d2c0184958bd1329f59130cce0_B_6_Float = _SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4.b;
float _SampleTexture2D_719055d2c0184958bd1329f59130cce0_A_7_Float = _SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4.a;
float _Property_90898696aae3458aa180c6f15d96aaee_Out_0_Float = _AO_Intensity;
float _Lerp_bf670034cefc4c6d9e02b57f5394811d_Out_3_Float;
Unity_Lerp_float(_Float_2c47c335e2304faa927a789be883bb19_Out_0_Float, _SampleTexture2D_719055d2c0184958bd1329f59130cce0_R_4_Float, _Property_90898696aae3458aa180c6f15d96aaee_Out_0_Float, _Lerp_bf670034cefc4c6d9e02b57f5394811d_Out_3_Float);
float _Property_e77944e4743744198e931506300c92ca_Out_0_Float = _Roughness_Intensity;
float _Multiply_43e873dc525645aab065eb6fc56ad77f_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_719055d2c0184958bd1329f59130cce0_G_5_Float, _Property_e77944e4743744198e931506300c92ca_Out_0_Float, _Multiply_43e873dc525645aab065eb6fc56ad77f_Out_2_Float);
float _OneMinus_3225b57b02ed4eaf9c118419fbefc479_Out_1_Float;
Unity_OneMinus_float(_Multiply_43e873dc525645aab065eb6fc56ad77f_Out_2_Float, _OneMinus_3225b57b02ed4eaf9c118419fbefc479_Out_1_Float);
float _Property_c0c103f0dfcd475fa428b0e043905944_Out_0_Float = _Metallic_Intensity;
float _Multiply_38b5ca9e78ba40d9b861ffc15889bd7f_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_719055d2c0184958bd1329f59130cce0_B_6_Float, _Property_c0c103f0dfcd475fa428b0e043905944_Out_0_Float, _Multiply_38b5ca9e78ba40d9b861ffc15889bd7f_Out_2_Float);
Ambient_Occlusion_1 = _Lerp_bf670034cefc4c6d9e02b57f5394811d_Out_3_Float;
Roughness_2 = _OneMinus_3225b57b02ed4eaf9c118419fbefc479_Out_1_Float;
Metallic_3 = _Multiply_38b5ca9e78ba40d9b861ffc15889bd7f_Out_2_Float;
}

struct Bindings_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float
{
half4 uv0;
};

void SG_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float(float _Texture_Size, UnityTexture2D _Color_Map, float4 _Color_Tint, UnityTexture2D _Normal_Map, float _Normal_Intensity, UnityTexture2D _ORM_Map, float _Roughness_Intensity, float _AO_Intensity, float _Metallic_Intensity, UnityTexture2D _Emissive_Map, float4 _Emissive_Color, float _Emissive_Intensity, Bindings_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float IN, out float4 Base_Color_5, out float3 Normal_1, out float4 Emissive_6, out float Ambient_Occlusion_4, out float Roughness_3, out float Metallic_2)
{
UnityTexture2D _Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D = _Color_Map;
float _Property_6e866f25d28140918dd5249261b96e76_Out_0_Float = _Texture_Size;
float2 _TilingAndOffset_49c249d74ea54ccf979206ad2c3c0c00_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, (_Property_6e866f25d28140918dd5249261b96e76_Out_0_Float.xx), float2 (0, 0), _TilingAndOffset_49c249d74ea54ccf979206ad2c3c0c00_Out_3_Vector2);
float _Property_94852b2b32f549138996f0dfb35b0486_Out_0_Float = _Texture_Size;
float2 _Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2;
Unity_Multiply_float2_float2(_TilingAndOffset_49c249d74ea54ccf979206ad2c3c0c00_Out_3_Vector2, (_Property_94852b2b32f549138996f0dfb35b0486_Out_0_Float.xx), _Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2);
float4 _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D.tex, _Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D.samplerstate, _Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D.GetTransformedUV(_Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2) );
if (_Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4, _Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_R_4_Float = _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4.r;
float _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_G_5_Float = _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4.g;
float _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_B_6_Float = _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4.b;
float _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_A_7_Float = _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4.a;
float4 _Property_c081d1852d284e4fa4bbb04ea3adf35e_Out_0_Vector4 = _Color_Tint;
float4 _Multiply_ff3b38102028498db7cc80acf5e42362_Out_2_Vector4;
Unity_Multiply_float4_float4(_SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4, _Property_c081d1852d284e4fa4bbb04ea3adf35e_Out_0_Vector4, _Multiply_ff3b38102028498db7cc80acf5e42362_Out_2_Vector4);
UnityTexture2D _Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D = _Normal_Map;
float4 _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D.tex, _Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D.samplerstate, _Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D.GetTransformedUV(_Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2) );
if (_Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4, _Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4);
float _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_R_4_Float = _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.r;
float _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_G_5_Float = _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.g;
float _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_B_6_Float = _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.b;
float _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_A_7_Float = _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.a;
float _Property_8b1f839dfbcd4481b201427fbc7310ad_Out_0_Float = _Normal_Intensity;
float3 _NormalStrength_a218ef03cc884bf0b0962d85b628afe7_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.xyz), _Property_8b1f839dfbcd4481b201427fbc7310ad_Out_0_Float, _NormalStrength_a218ef03cc884bf0b0962d85b628afe7_Out_2_Vector3);
UnityTexture2D _Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D = _Emissive_Map;
float4 _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D.tex, _Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D.samplerstate, _Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D.GetTransformedUV(_Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2) );
if (_Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4, _Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_R_4_Float = _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4.r;
float _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_G_5_Float = _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4.g;
float _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_B_6_Float = _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4.b;
float _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_A_7_Float = _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4.a;
float4 _Property_3a2f8dbe365d427e89744f8eb8dabd44_Out_0_Vector4 = _Emissive_Color;
float4 _Multiply_1efa7dc8f3994d8ea213351379d48448_Out_2_Vector4;
Unity_Multiply_float4_float4(_SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4, _Property_3a2f8dbe365d427e89744f8eb8dabd44_Out_0_Vector4, _Multiply_1efa7dc8f3994d8ea213351379d48448_Out_2_Vector4);
float _Property_09c3fe7e22be43caa45c324f52ea8137_Out_0_Float = _Emissive_Intensity;
float4 _Multiply_43a1c0123fb34a25a5d21581f08926f8_Out_2_Vector4;
Unity_Multiply_float4_float4(_Multiply_1efa7dc8f3994d8ea213351379d48448_Out_2_Vector4, (_Property_09c3fe7e22be43caa45c324f52ea8137_Out_0_Float.xxxx), _Multiply_43a1c0123fb34a25a5d21581f08926f8_Out_2_Vector4);
UnityTexture2D _Property_ecbd8a4e938a458cabefe2480cada231_Out_0_Texture2D = _ORM_Map;
float _Property_2ca0dc7be16d47bfb0faf210c4d70757_Out_0_Float = _AO_Intensity;
float _Property_fb245dde136f4b2486e260e720f83c83_Out_0_Float = _Roughness_Intensity;
float _Property_0fcde0f68f364243886b868db18a8d27_Out_0_Float = _Metallic_Intensity;
Bindings_PASGORMUnpacker_abcd41e335c426a4bb0f8de17b4b5f96_float _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94;
half _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_AmbientOcclusion_1_Float;
half _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Roughness_2_Float;
half _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Metallic_3_Float;
SG_PASGORMUnpacker_abcd41e335c426a4bb0f8de17b4b5f96_float(_Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2, _Property_ecbd8a4e938a458cabefe2480cada231_Out_0_Texture2D, _Property_2ca0dc7be16d47bfb0faf210c4d70757_Out_0_Float, _Property_fb245dde136f4b2486e260e720f83c83_Out_0_Float, _Property_0fcde0f68f364243886b868db18a8d27_Out_0_Float, _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94, _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_AmbientOcclusion_1_Float, _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Roughness_2_Float, _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Metallic_3_Float);
Base_Color_5 = _Multiply_ff3b38102028498db7cc80acf5e42362_Out_2_Vector4;
Normal_1 = _NormalStrength_a218ef03cc884bf0b0962d85b628afe7_Out_2_Vector3;
Emissive_6 = _Multiply_43a1c0123fb34a25a5d21581f08926f8_Out_2_Vector4;
Ambient_Occlusion_4 = _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_AmbientOcclusion_1_Float;
Roughness_3 = _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Roughness_2_Float;
Metallic_2 = _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Metallic_3_Float;
}

void Unity_DotProduct_float3(float3 A, float3 B, out float Out)
{
    Out = dot(A, B);
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Clamp_float(float In, float Min, float Max, out float Out)
{
    Out = clamp(In, Min, Max);
}

void Unity_Contrast_float(float3 In, float Contrast, out float3 Out)
{
    float midpoint = pow(0.5, 2.2);
    Out =  (In - midpoint) * Contrast + midpoint;
}

void Unity_Clamp_float3(float3 In, float3 Min, float3 Max, out float3 Out)
{
    Out = clamp(In, Min, Max);
}

struct Bindings_PASGVerticalBlend_e9f0b2d5820244846982772499370842_float
{
float3 ObjectSpaceNormal;
float3 WorldSpaceNormal;
float3 WorldSpaceTangent;
float3 WorldSpaceBiTangent;
float3 ObjectSpacePosition;
};

void SG_PASGVerticalBlend_e9f0b2d5820244846982772499370842_float(float _Sloap_Contrast, UnityTexture2D _Slope_Noise_Texture, float _Noise_Texture_Size, float3 _Explicit_Normal, float _Sloap_Offset, Bindings_PASGVerticalBlend_e9f0b2d5820244846982772499370842_float IN, out float3 OutVector3_1)
{
float3 _Property_5509c7fb30e445229f496a71c6e9c192_Out_0_Vector3 = _Explicit_Normal;
float3 _Transform_9c9f921cd0934012ac0d281c8e40a727_Out_1_Vector3;
{
float3x3 tangentTransform = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
_Transform_9c9f921cd0934012ac0d281c8e40a727_Out_1_Vector3 = TransformTangentToWorld(_Property_5509c7fb30e445229f496a71c6e9c192_Out_0_Vector3.xyz, tangentTransform, true);
}
float _DotProduct_4bc69611266041259c8da1b387474ec9_Out_2_Float;
Unity_DotProduct_float3(_Transform_9c9f921cd0934012ac0d281c8e40a727_Out_1_Vector3, float3(0, 1, 0), _DotProduct_4bc69611266041259c8da1b387474ec9_Out_2_Float);
float _Property_6a995c4e80824d6fa7d458ddcdc4eec3_Out_0_Float = _Sloap_Offset;
float _Add_a196acc146d1466fb309ec050e77feb2_Out_2_Float;
Unity_Add_float(_DotProduct_4bc69611266041259c8da1b387474ec9_Out_2_Float, _Property_6a995c4e80824d6fa7d458ddcdc4eec3_Out_0_Float, _Add_a196acc146d1466fb309ec050e77feb2_Out_2_Float);
float _Lerp_a1fed41c0847449ab72ae8fe4775270c_Out_3_Float;
Unity_Lerp_float(float(0), float(3), _Add_a196acc146d1466fb309ec050e77feb2_Out_2_Float, _Lerp_a1fed41c0847449ab72ae8fe4775270c_Out_3_Float);
float _Clamp_d4d6e762b7b1422d936b98d7f9429754_Out_3_Float;
Unity_Clamp_float(_Lerp_a1fed41c0847449ab72ae8fe4775270c_Out_3_Float, float(0), float(1), _Clamp_d4d6e762b7b1422d936b98d7f9429754_Out_3_Float);
float _Property_985b04ddd40847029dbfa829fbcaeed1_Out_0_Float = _Sloap_Contrast;
float3 _Contrast_2864897542064adcb4b61fcccadaf026_Out_2_Vector3;
Unity_Contrast_float((_Clamp_d4d6e762b7b1422d936b98d7f9429754_Out_3_Float.xxx), _Property_985b04ddd40847029dbfa829fbcaeed1_Out_0_Float, _Contrast_2864897542064adcb4b61fcccadaf026_Out_2_Vector3);
float3 _Clamp_b8f1f82d045f437a910a54b62bb2df81_Out_3_Vector3;
Unity_Clamp_float3(_Contrast_2864897542064adcb4b61fcccadaf026_Out_2_Vector3, float3(0, 0, 0), float3(1, 1, 1), _Clamp_b8f1f82d045f437a910a54b62bb2df81_Out_3_Vector3);
float3 _Transform_229ffe25002343a39c8caaa3d7f6df91_Out_1_Vector3;
_Transform_229ffe25002343a39c8caaa3d7f6df91_Out_1_Vector3 = TransformObjectToWorldDir(IN.ObjectSpacePosition.xyz, true);
float3 _Vector3_ec86905b34c54695bed5b7d1975cb6ce_Out_0_Vector3 = float3(float(0), float(2), float(0));
float _DotProduct_af3cb3caf6e4473c8161868b15c0255b_Out_2_Float;
Unity_DotProduct_float3(_Transform_229ffe25002343a39c8caaa3d7f6df91_Out_1_Vector3, _Vector3_ec86905b34c54695bed5b7d1975cb6ce_Out_0_Vector3, _DotProduct_af3cb3caf6e4473c8161868b15c0255b_Out_2_Float);
float _Add_47be9b48b4c94dd28e8c3db7f6ba1a05_Out_2_Float;
Unity_Add_float(_Property_6a995c4e80824d6fa7d458ddcdc4eec3_Out_0_Float, _DotProduct_af3cb3caf6e4473c8161868b15c0255b_Out_2_Float, _Add_47be9b48b4c94dd28e8c3db7f6ba1a05_Out_2_Float);
float _Multiply_35491cd334cb4e34a76b48de2bc5814a_Out_2_Float;
Unity_Multiply_float_float(_Add_47be9b48b4c94dd28e8c3db7f6ba1a05_Out_2_Float, 2, _Multiply_35491cd334cb4e34a76b48de2bc5814a_Out_2_Float);
float _Clamp_ccd0969e9da6420f94e7113654fd480e_Out_3_Float;
Unity_Clamp_float(_Multiply_35491cd334cb4e34a76b48de2bc5814a_Out_2_Float, float(0), float(1), _Clamp_ccd0969e9da6420f94e7113654fd480e_Out_3_Float);
float3 _Contrast_454ba7c8fb3a4d03951b073867fa5f14_Out_2_Vector3;
Unity_Contrast_float((_Clamp_ccd0969e9da6420f94e7113654fd480e_Out_3_Float.xxx), _Property_985b04ddd40847029dbfa829fbcaeed1_Out_0_Float, _Contrast_454ba7c8fb3a4d03951b073867fa5f14_Out_2_Vector3);
float3 _Clamp_1ba5cf0aee8245bd9dca155954c3df81_Out_3_Vector3;
Unity_Clamp_float3(_Contrast_454ba7c8fb3a4d03951b073867fa5f14_Out_2_Vector3, float3(0, 0, 0), float3(1, 1, 1), _Clamp_1ba5cf0aee8245bd9dca155954c3df81_Out_3_Vector3);
UnityTexture2D _Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D = _Slope_Noise_Texture;
float _Property_8e663ddd1476448d948a9de1bf1f83a9_Out_0_Float = _Noise_Texture_Size;
float3 Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_UV = IN.ObjectSpacePosition * _Property_8e663ddd1476448d948a9de1bf1f83a9_Out_0_Float;
float3 Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend = SafePositivePow_float(IN.ObjectSpaceNormal, min(float(1), floor(log2(Min_float())/log2(1/sqrt(3)))) );
Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend /= dot(Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend, 1.0);
float4 Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_X = SAMPLE_TEXTURE2D(_Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.tex, _Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.samplerstate, Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_UV.zy);
float4 Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Y = SAMPLE_TEXTURE2D(_Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.tex, _Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.samplerstate, Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_UV.xz);
float4 Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Z = SAMPLE_TEXTURE2D(_Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.tex, _Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.samplerstate, Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_UV.xy);
float4 _Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Out_0_Vector4 = Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_X * Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend.x + Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Y * Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend.y + Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Z * Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend.z;
float _Swizzle_90fb1902a2bd4070b4f9e27ac287ca97_Out_1_Float = (_Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Out_0_Vector4.xy).y;
float _Clamp_f32c97c6ccbf44649bd757503ccdbe71_Out_3_Float;
Unity_Clamp_float(_Swizzle_90fb1902a2bd4070b4f9e27ac287ca97_Out_1_Float, float(0), float(1), _Clamp_f32c97c6ccbf44649bd757503ccdbe71_Out_3_Float);
float _Add_92d15430b3c042719d65f1252bc35d7a_Out_2_Float;
Unity_Add_float(_Multiply_35491cd334cb4e34a76b48de2bc5814a_Out_2_Float, _Clamp_f32c97c6ccbf44649bd757503ccdbe71_Out_3_Float, _Add_92d15430b3c042719d65f1252bc35d7a_Out_2_Float);
float3 _Contrast_c2fe106c51f0498e987cf31090ec563d_Out_2_Vector3;
Unity_Contrast_float((_Add_92d15430b3c042719d65f1252bc35d7a_Out_2_Float.xxx), _Property_985b04ddd40847029dbfa829fbcaeed1_Out_0_Float, _Contrast_c2fe106c51f0498e987cf31090ec563d_Out_2_Vector3);
float3 _Clamp_05438405442c47cb98e571d128277bbe_Out_3_Vector3;
Unity_Clamp_float3(_Contrast_c2fe106c51f0498e987cf31090ec563d_Out_2_Vector3, float3(0, 0, 0), float3(1, 1, 1), _Clamp_05438405442c47cb98e571d128277bbe_Out_3_Vector3);
#if defined(_EDGEBLENDING_NORMALBLENDING)
float3 _EdgeBlending_cdbe33a15cf6452b8881ecea22adcb84_Out_0_Vector3 = _Clamp_b8f1f82d045f437a910a54b62bb2df81_Out_3_Vector3;
#elif defined(_EDGEBLENDING_VERTEXBLENDING)
float3 _EdgeBlending_cdbe33a15cf6452b8881ecea22adcb84_Out_0_Vector3 = _Clamp_1ba5cf0aee8245bd9dca155954c3df81_Out_3_Vector3;
#else
float3 _EdgeBlending_cdbe33a15cf6452b8881ecea22adcb84_Out_0_Vector3 = _Clamp_05438405442c47cb98e571d128277bbe_Out_3_Vector3;
#endif
OutVector3_1 = _EdgeBlending_cdbe33a15cf6452b8881ecea22adcb84_Out_0_Vector3;
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Branch_float3(float Predicate, float3 True, float3 False, out float3 Out)
{
    Out = Predicate ? True : False;
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
description.Position = IN.ObjectSpacePosition;
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
float Alpha;
float AlphaClipThreshold;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
float _Property_868c674f556d42be9071980dc60f4aed_Out_0_Boolean = _EnableCoverage;
float _Property_a0b21575a96c42a2b05aa3518645b2f4_Out_0_Float = _UV_Size;
UnityTexture2D _Property_4d248f4a9de94d7bb9a5c3c7c4c10a68_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_ColorMap, sampler_ColorMap, _ColorMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _Property_061dd20995c54e4b8c0357aa709c6e56_Out_0_Vector4 = _ColorTint;
UnityTexture2D _Property_cc904da6b4714cd6a9559b3bcf6ba7a6_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_NormalMap, sampler_NormalMap, _NormalMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_49f412000c0b4f18863fb7afae8b43fc_Out_0_Float = _NormalIntensity;
UnityTexture2D _Property_3fcfe917c75d4a46bfe50da31a24c74f_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_ORMMap, sampler_ORMMap, _ORMMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_bd35b765c62349f285447de81728f0c8_Out_0_Float = _RoughnessIntensity;
float _Property_f125a72836c24b0fa5ef72823c8e6452_Out_0_Float = _AOIntensity;
float _Property_0251a196075947af9d3ad28d6875a5ef_Out_0_Float = _MetallicIntensity;
UnityTexture2D _Property_47ecc092fbd144218fe6440250e367b3_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Emissive_Map, sampler_Emissive_Map, _Emissive_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _Property_c065f0d4df8c40528116ee8e516b2221_Out_0_Vector4 = _EmissiveColor;
float _Property_0038e41f028740e4abc1958ca58302a1_Out_0_Float = _EmissiveIntensity;
Bindings_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe;
_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe.uv0 = IN.uv0;
float4 _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_BaseColor_5_Vector4;
float3 _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Normal_1_Vector3;
float4 _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Emissive_6_Vector4;
float _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_AmbientOcclusion_4_Float;
float _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Roughness_3_Float;
float _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Metallic_2_Float;
SG_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float(_Property_a0b21575a96c42a2b05aa3518645b2f4_Out_0_Float, _Property_4d248f4a9de94d7bb9a5c3c7c4c10a68_Out_0_Texture2D, _Property_061dd20995c54e4b8c0357aa709c6e56_Out_0_Vector4, _Property_cc904da6b4714cd6a9559b3bcf6ba7a6_Out_0_Texture2D, _Property_49f412000c0b4f18863fb7afae8b43fc_Out_0_Float, _Property_3fcfe917c75d4a46bfe50da31a24c74f_Out_0_Texture2D, _Property_bd35b765c62349f285447de81728f0c8_Out_0_Float, _Property_f125a72836c24b0fa5ef72823c8e6452_Out_0_Float, _Property_0251a196075947af9d3ad28d6875a5ef_Out_0_Float, _Property_47ecc092fbd144218fe6440250e367b3_Out_0_Texture2D, _Property_c065f0d4df8c40528116ee8e516b2221_Out_0_Vector4, _Property_0038e41f028740e4abc1958ca58302a1_Out_0_Float, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_BaseColor_5_Vector4, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Normal_1_Vector3, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Emissive_6_Vector4, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_AmbientOcclusion_4_Float, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Roughness_3_Float, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Metallic_2_Float);
float _Property_623a0c9d694c4c33b6256bde952c53e5_Out_0_Float = _CoverageSize;
UnityTexture2D _Property_58d380b2034145c8b7d2eda637dbc296_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_CoverageColorMap, sampler_CoverageColorMap, _CoverageColorMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
UnityTexture2D _Property_f324dd6628ea45b1a630d750fa3d6b66_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_CoverageNormalMap, sampler_CoverageNormalMap, _CoverageNormalMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_b00b34515282450295cc4f4eb7b4a0ce_Out_0_Float = _CoverageNormalIntensity;
UnityTexture2D _Property_24ae6469bfeb4dada6fb7c7d39985cb3_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_CoverageORMMap, sampler_CoverageORMMap, _CoverageORMMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_03c29a27092f4bbb8a4fcc3b6e7b2909_Out_0_Float = _CoverageRoughnessIntensity;
float _Property_07cad64e572e4602a346b7dd90c3eb21_Out_0_Float = _CoverageMetallicIntensity;
Bindings_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c;
_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c.uv0 = IN.uv0;
float4 _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_BaseColor_5_Vector4;
float3 _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Normal_1_Vector3;
float4 _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Emissive_6_Vector4;
float _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_AmbientOcclusion_4_Float;
float _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Roughness_3_Float;
float _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Metallic_2_Float;
SG_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float(_Property_623a0c9d694c4c33b6256bde952c53e5_Out_0_Float, _Property_58d380b2034145c8b7d2eda637dbc296_Out_0_Texture2D, float4 (1, 1, 1, 1), _Property_f324dd6628ea45b1a630d750fa3d6b66_Out_0_Texture2D, _Property_b00b34515282450295cc4f4eb7b4a0ce_Out_0_Float, _Property_24ae6469bfeb4dada6fb7c7d39985cb3_Out_0_Texture2D, _Property_03c29a27092f4bbb8a4fcc3b6e7b2909_Out_0_Float, float(0), _Property_07cad64e572e4602a346b7dd90c3eb21_Out_0_Float, UnityBuildTexture2DStructNoScale(_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D), float4 (0, 0, 0, 0), float(0), _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_BaseColor_5_Vector4, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Normal_1_Vector3, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Emissive_6_Vector4, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_AmbientOcclusion_4_Float, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Roughness_3_Float, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Metallic_2_Float);
float _Property_0785fc58d71d4d63abb8c1102aa1a1b7_Out_0_Float = _SloapContrast;
UnityTexture2D _Property_12921d1841154c59b11aefccb2cbca31_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_SlopeNoiseTexture, sampler_SlopeNoiseTexture, _SlopeNoiseTexture_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_10dd46128137476fb3fd49de6154e94c_Out_0_Float = _NoiseTextureSize;
float _Property_a654ef8b1e2a4529ab82f92b3ae202ec_Out_0_Float = _SloapOffset;
Bindings_PASGVerticalBlend_e9f0b2d5820244846982772499370842_float _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d;
_PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d.ObjectSpaceNormal = IN.ObjectSpaceNormal;
_PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d.WorldSpaceNormal = IN.WorldSpaceNormal;
_PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d.WorldSpaceTangent = IN.WorldSpaceTangent;
_PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d.WorldSpaceBiTangent = IN.WorldSpaceBiTangent;
_PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d.ObjectSpacePosition = IN.ObjectSpacePosition;
float3 _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d_OutVector3_1_Vector3;
SG_PASGVerticalBlend_e9f0b2d5820244846982772499370842_float(_Property_0785fc58d71d4d63abb8c1102aa1a1b7_Out_0_Float, _Property_12921d1841154c59b11aefccb2cbca31_Out_0_Texture2D, _Property_10dd46128137476fb3fd49de6154e94c_Out_0_Float, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Normal_1_Vector3, _Property_a654ef8b1e2a4529ab82f92b3ae202ec_Out_0_Float, _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d, _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d_OutVector3_1_Vector3);
float3 _Lerp_8ef5a2e39e934fb09e1bc6858ba97959_Out_3_Vector3;
Unity_Lerp_float3((_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_BaseColor_5_Vector4.xyz), (_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_BaseColor_5_Vector4.xyz), _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d_OutVector3_1_Vector3, _Lerp_8ef5a2e39e934fb09e1bc6858ba97959_Out_3_Vector3);
float3 _Branch_72adc4bf3fd64427a9a9035afa6f856a_Out_3_Vector3;
Unity_Branch_float3(_Property_868c674f556d42be9071980dc60f4aed_Out_0_Boolean, _Lerp_8ef5a2e39e934fb09e1bc6858ba97959_Out_3_Vector3, (_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_BaseColor_5_Vector4.xyz), _Branch_72adc4bf3fd64427a9a9035afa6f856a_Out_3_Vector3);
UnityTexture2D _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_ColorMap_1, sampler_ColorMap_1, _ColorMap_1_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.tex, _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.samplerstate, _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4, _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_R_4_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.r;
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_G_5_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.g;
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_B_6_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.b;
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_A_7_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.a;
float _Property_7a43d5f1d3df46c2869bc70eebee1f91_Out_0_Float = _EmissiveIntensity_1;
surface.BaseColor = _Branch_72adc4bf3fd64427a9a9035afa6f856a_Out_3_Vector3;
surface.Alpha = (_SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4).x;
surface.AlphaClipThreshold = _Property_7a43d5f1d3df46c2869bc70eebee1f91_Out_0_Float;
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

    // use bitangent on the fly like in hdrp
    // IMPORTANT! If we ever support Flip on double sided materials ensure bitangent and tangent are NOT flipped.
    float crossSign = (input.tangentWS.w > 0.0 ? 1.0 : -1.0)* GetOddNegativeScale();
    float3 bitang = crossSign * cross(input.normalWS.xyz, input.tangentWS.xyz);

    output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph
    output.ObjectSpaceNormal = normalize(mul(output.WorldSpaceNormal, (float3x3) UNITY_MATRIX_M));           // transposed multiplication by inverse matrix to handle normal scale

    // to pr               eserve mikktspace compliance we use same scale renormFactor as was used on the normal.
    // This                is explained in section 2.2 in "surface gradient based bump mapping framework"
    output.WorldSpaceTangent = renormFactor * input.tangentWS.xyz;
    output.WorldSpaceBiTangent = renormFactor * bitang;

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
"Queue"="AlphaTest"
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
Cull [_BUILTIN_CullMode]
Blend [_BUILTIN_SrcBlend] [_BUILTIN_DstBlend]
ZTest [_BUILTIN_ZTest]
ZWrite [_BUILTIN_ZWrite]

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
#pragma shader_feature_local_fragment _ _BUILTIN_SURFACE_TYPE_TRANSPARENT
#pragma shader_feature_local_fragment _ _BUILTIN_ALPHAPREMULTIPLY_ON
#pragma shader_feature_local_fragment _ _BUILTIN_AlphaClip
#pragma shader_feature_local_fragment _ _BUILTIN_ALPHATEST_ON
#pragma shader_feature_local _EDGEBLENDING_NORMALBLENDING _EDGEBLENDING_VERTEXBLENDING _EDGEBLENDING_NOISEBLENDING

#if defined(_EDGEBLENDING_NORMALBLENDING)
#define KEYWORD_PERMUTATION_0
#elif defined(_EDGEBLENDING_VERTEXBLENDING)
#define KEYWORD_PERMUTATION_1
#else
#define KEYWORD_PERMUTATION_2
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
#define VARYINGS_NEED_CULLFACE
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
 float3 ObjectSpaceNormal;
 float3 WorldSpaceNormal;
 float3 TangentSpaceNormal;
 float3 WorldSpaceTangent;
 float3 WorldSpaceBiTangent;
 float3 ObjectSpacePosition;
 float4 uv0;
 float FaceSign;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
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

float4 _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D_TexelSize;
float4 _ColorMap_TexelSize;
float4 _ColorMap_1_TexelSize;
float4 _NormalMap_TexelSize;
float4 _ColorTint;
float _NormalIntensity;
float _UV_Size;
float4 _ORMMap_TexelSize;
float _RoughnessIntensity;
float _AOIntensity;
float _MetallicIntensity;
float4 _EmissiveColor;
float _EmissiveIntensity_1;
float4 _Emissive_Map_TexelSize;
float _EmissiveIntensity;
float _EnableCoverage;
float _CoverageSize;
float _CoverageNormalIntensity;
float _CoverageRoughnessIntensity;
float _CoverageMetallicIntensity;
float _NoiseTextureSize;
float4 _SlopeNoiseTexture_TexelSize;
float4 _CoverageColorMap_TexelSize;
float4 _CoverageORMMap_TexelSize;
float4 _CoverageNormalMap_TexelSize;
float _SloapOffset;
float _SloapContrast;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D);
SAMPLER(sampler_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D);
TEXTURE2D(_ColorMap);
SAMPLER(sampler_ColorMap);
TEXTURE2D(_ColorMap_1);
SAMPLER(sampler_ColorMap_1);
TEXTURE2D(_NormalMap);
SAMPLER(sampler_NormalMap);
TEXTURE2D(_ORMMap);
SAMPLER(sampler_ORMMap);
TEXTURE2D(_Emissive_Map);
SAMPLER(sampler_Emissive_Map);
TEXTURE2D(_SlopeNoiseTexture);
SAMPLER(sampler_SlopeNoiseTexture);
TEXTURE2D(_CoverageColorMap);
SAMPLER(sampler_CoverageColorMap);
TEXTURE2D(_CoverageORMMap);
SAMPLER(sampler_CoverageORMMap);
TEXTURE2D(_CoverageNormalMap);
SAMPLER(sampler_CoverageNormalMap);

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

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

void Unity_NormalStrength_float(float3 In, float Strength, out float3 Out)
{
    Out = float3(In.rg * Strength, lerp(1, In.b, saturate(Strength)));
}

void Unity_Lerp_float(float A, float B, float T, out float Out)
{
    Out = lerp(A, B, T);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
}

struct Bindings_PASGORMUnpacker_abcd41e335c426a4bb0f8de17b4b5f96_float
{
};

void SG_PASGORMUnpacker_abcd41e335c426a4bb0f8de17b4b5f96_float(float2 _UV, UnityTexture2D _ORM_Map, float _AO_Intensity, float _Roughness_Intensity, float _Metallic_Intensity, Bindings_PASGORMUnpacker_abcd41e335c426a4bb0f8de17b4b5f96_float IN, out float Ambient_Occlusion_1, out float Roughness_2, out float Metallic_3)
{
float _Float_2c47c335e2304faa927a789be883bb19_Out_0_Float = float(1);
UnityTexture2D _Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D = _ORM_Map;
float2 _Property_ad3893fa315b474ea60e7ee3e265957c_Out_0_Vector2 = _UV;
float4 _SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D.tex, _Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D.samplerstate, _Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D.GetTransformedUV(_Property_ad3893fa315b474ea60e7ee3e265957c_Out_0_Vector2) );
if (_Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4, _Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_719055d2c0184958bd1329f59130cce0_R_4_Float = _SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4.r;
float _SampleTexture2D_719055d2c0184958bd1329f59130cce0_G_5_Float = _SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4.g;
float _SampleTexture2D_719055d2c0184958bd1329f59130cce0_B_6_Float = _SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4.b;
float _SampleTexture2D_719055d2c0184958bd1329f59130cce0_A_7_Float = _SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4.a;
float _Property_90898696aae3458aa180c6f15d96aaee_Out_0_Float = _AO_Intensity;
float _Lerp_bf670034cefc4c6d9e02b57f5394811d_Out_3_Float;
Unity_Lerp_float(_Float_2c47c335e2304faa927a789be883bb19_Out_0_Float, _SampleTexture2D_719055d2c0184958bd1329f59130cce0_R_4_Float, _Property_90898696aae3458aa180c6f15d96aaee_Out_0_Float, _Lerp_bf670034cefc4c6d9e02b57f5394811d_Out_3_Float);
float _Property_e77944e4743744198e931506300c92ca_Out_0_Float = _Roughness_Intensity;
float _Multiply_43e873dc525645aab065eb6fc56ad77f_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_719055d2c0184958bd1329f59130cce0_G_5_Float, _Property_e77944e4743744198e931506300c92ca_Out_0_Float, _Multiply_43e873dc525645aab065eb6fc56ad77f_Out_2_Float);
float _OneMinus_3225b57b02ed4eaf9c118419fbefc479_Out_1_Float;
Unity_OneMinus_float(_Multiply_43e873dc525645aab065eb6fc56ad77f_Out_2_Float, _OneMinus_3225b57b02ed4eaf9c118419fbefc479_Out_1_Float);
float _Property_c0c103f0dfcd475fa428b0e043905944_Out_0_Float = _Metallic_Intensity;
float _Multiply_38b5ca9e78ba40d9b861ffc15889bd7f_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_719055d2c0184958bd1329f59130cce0_B_6_Float, _Property_c0c103f0dfcd475fa428b0e043905944_Out_0_Float, _Multiply_38b5ca9e78ba40d9b861ffc15889bd7f_Out_2_Float);
Ambient_Occlusion_1 = _Lerp_bf670034cefc4c6d9e02b57f5394811d_Out_3_Float;
Roughness_2 = _OneMinus_3225b57b02ed4eaf9c118419fbefc479_Out_1_Float;
Metallic_3 = _Multiply_38b5ca9e78ba40d9b861ffc15889bd7f_Out_2_Float;
}

struct Bindings_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float
{
half4 uv0;
};

void SG_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float(float _Texture_Size, UnityTexture2D _Color_Map, float4 _Color_Tint, UnityTexture2D _Normal_Map, float _Normal_Intensity, UnityTexture2D _ORM_Map, float _Roughness_Intensity, float _AO_Intensity, float _Metallic_Intensity, UnityTexture2D _Emissive_Map, float4 _Emissive_Color, float _Emissive_Intensity, Bindings_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float IN, out float4 Base_Color_5, out float3 Normal_1, out float4 Emissive_6, out float Ambient_Occlusion_4, out float Roughness_3, out float Metallic_2)
{
UnityTexture2D _Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D = _Color_Map;
float _Property_6e866f25d28140918dd5249261b96e76_Out_0_Float = _Texture_Size;
float2 _TilingAndOffset_49c249d74ea54ccf979206ad2c3c0c00_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, (_Property_6e866f25d28140918dd5249261b96e76_Out_0_Float.xx), float2 (0, 0), _TilingAndOffset_49c249d74ea54ccf979206ad2c3c0c00_Out_3_Vector2);
float _Property_94852b2b32f549138996f0dfb35b0486_Out_0_Float = _Texture_Size;
float2 _Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2;
Unity_Multiply_float2_float2(_TilingAndOffset_49c249d74ea54ccf979206ad2c3c0c00_Out_3_Vector2, (_Property_94852b2b32f549138996f0dfb35b0486_Out_0_Float.xx), _Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2);
float4 _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D.tex, _Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D.samplerstate, _Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D.GetTransformedUV(_Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2) );
if (_Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4, _Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_R_4_Float = _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4.r;
float _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_G_5_Float = _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4.g;
float _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_B_6_Float = _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4.b;
float _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_A_7_Float = _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4.a;
float4 _Property_c081d1852d284e4fa4bbb04ea3adf35e_Out_0_Vector4 = _Color_Tint;
float4 _Multiply_ff3b38102028498db7cc80acf5e42362_Out_2_Vector4;
Unity_Multiply_float4_float4(_SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4, _Property_c081d1852d284e4fa4bbb04ea3adf35e_Out_0_Vector4, _Multiply_ff3b38102028498db7cc80acf5e42362_Out_2_Vector4);
UnityTexture2D _Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D = _Normal_Map;
float4 _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D.tex, _Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D.samplerstate, _Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D.GetTransformedUV(_Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2) );
if (_Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4, _Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4);
float _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_R_4_Float = _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.r;
float _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_G_5_Float = _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.g;
float _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_B_6_Float = _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.b;
float _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_A_7_Float = _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.a;
float _Property_8b1f839dfbcd4481b201427fbc7310ad_Out_0_Float = _Normal_Intensity;
float3 _NormalStrength_a218ef03cc884bf0b0962d85b628afe7_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.xyz), _Property_8b1f839dfbcd4481b201427fbc7310ad_Out_0_Float, _NormalStrength_a218ef03cc884bf0b0962d85b628afe7_Out_2_Vector3);
UnityTexture2D _Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D = _Emissive_Map;
float4 _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D.tex, _Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D.samplerstate, _Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D.GetTransformedUV(_Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2) );
if (_Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4, _Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_R_4_Float = _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4.r;
float _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_G_5_Float = _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4.g;
float _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_B_6_Float = _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4.b;
float _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_A_7_Float = _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4.a;
float4 _Property_3a2f8dbe365d427e89744f8eb8dabd44_Out_0_Vector4 = _Emissive_Color;
float4 _Multiply_1efa7dc8f3994d8ea213351379d48448_Out_2_Vector4;
Unity_Multiply_float4_float4(_SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4, _Property_3a2f8dbe365d427e89744f8eb8dabd44_Out_0_Vector4, _Multiply_1efa7dc8f3994d8ea213351379d48448_Out_2_Vector4);
float _Property_09c3fe7e22be43caa45c324f52ea8137_Out_0_Float = _Emissive_Intensity;
float4 _Multiply_43a1c0123fb34a25a5d21581f08926f8_Out_2_Vector4;
Unity_Multiply_float4_float4(_Multiply_1efa7dc8f3994d8ea213351379d48448_Out_2_Vector4, (_Property_09c3fe7e22be43caa45c324f52ea8137_Out_0_Float.xxxx), _Multiply_43a1c0123fb34a25a5d21581f08926f8_Out_2_Vector4);
UnityTexture2D _Property_ecbd8a4e938a458cabefe2480cada231_Out_0_Texture2D = _ORM_Map;
float _Property_2ca0dc7be16d47bfb0faf210c4d70757_Out_0_Float = _AO_Intensity;
float _Property_fb245dde136f4b2486e260e720f83c83_Out_0_Float = _Roughness_Intensity;
float _Property_0fcde0f68f364243886b868db18a8d27_Out_0_Float = _Metallic_Intensity;
Bindings_PASGORMUnpacker_abcd41e335c426a4bb0f8de17b4b5f96_float _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94;
half _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_AmbientOcclusion_1_Float;
half _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Roughness_2_Float;
half _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Metallic_3_Float;
SG_PASGORMUnpacker_abcd41e335c426a4bb0f8de17b4b5f96_float(_Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2, _Property_ecbd8a4e938a458cabefe2480cada231_Out_0_Texture2D, _Property_2ca0dc7be16d47bfb0faf210c4d70757_Out_0_Float, _Property_fb245dde136f4b2486e260e720f83c83_Out_0_Float, _Property_0fcde0f68f364243886b868db18a8d27_Out_0_Float, _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94, _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_AmbientOcclusion_1_Float, _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Roughness_2_Float, _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Metallic_3_Float);
Base_Color_5 = _Multiply_ff3b38102028498db7cc80acf5e42362_Out_2_Vector4;
Normal_1 = _NormalStrength_a218ef03cc884bf0b0962d85b628afe7_Out_2_Vector3;
Emissive_6 = _Multiply_43a1c0123fb34a25a5d21581f08926f8_Out_2_Vector4;
Ambient_Occlusion_4 = _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_AmbientOcclusion_1_Float;
Roughness_3 = _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Roughness_2_Float;
Metallic_2 = _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Metallic_3_Float;
}

void Unity_DotProduct_float3(float3 A, float3 B, out float Out)
{
    Out = dot(A, B);
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Clamp_float(float In, float Min, float Max, out float Out)
{
    Out = clamp(In, Min, Max);
}

void Unity_Contrast_float(float3 In, float Contrast, out float3 Out)
{
    float midpoint = pow(0.5, 2.2);
    Out =  (In - midpoint) * Contrast + midpoint;
}

void Unity_Clamp_float3(float3 In, float3 Min, float3 Max, out float3 Out)
{
    Out = clamp(In, Min, Max);
}

struct Bindings_PASGVerticalBlend_e9f0b2d5820244846982772499370842_float
{
float3 ObjectSpaceNormal;
float3 WorldSpaceNormal;
float3 WorldSpaceTangent;
float3 WorldSpaceBiTangent;
float3 ObjectSpacePosition;
};

void SG_PASGVerticalBlend_e9f0b2d5820244846982772499370842_float(float _Sloap_Contrast, UnityTexture2D _Slope_Noise_Texture, float _Noise_Texture_Size, float3 _Explicit_Normal, float _Sloap_Offset, Bindings_PASGVerticalBlend_e9f0b2d5820244846982772499370842_float IN, out float3 OutVector3_1)
{
float3 _Property_5509c7fb30e445229f496a71c6e9c192_Out_0_Vector3 = _Explicit_Normal;
float3 _Transform_9c9f921cd0934012ac0d281c8e40a727_Out_1_Vector3;
{
float3x3 tangentTransform = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
_Transform_9c9f921cd0934012ac0d281c8e40a727_Out_1_Vector3 = TransformTangentToWorld(_Property_5509c7fb30e445229f496a71c6e9c192_Out_0_Vector3.xyz, tangentTransform, true);
}
float _DotProduct_4bc69611266041259c8da1b387474ec9_Out_2_Float;
Unity_DotProduct_float3(_Transform_9c9f921cd0934012ac0d281c8e40a727_Out_1_Vector3, float3(0, 1, 0), _DotProduct_4bc69611266041259c8da1b387474ec9_Out_2_Float);
float _Property_6a995c4e80824d6fa7d458ddcdc4eec3_Out_0_Float = _Sloap_Offset;
float _Add_a196acc146d1466fb309ec050e77feb2_Out_2_Float;
Unity_Add_float(_DotProduct_4bc69611266041259c8da1b387474ec9_Out_2_Float, _Property_6a995c4e80824d6fa7d458ddcdc4eec3_Out_0_Float, _Add_a196acc146d1466fb309ec050e77feb2_Out_2_Float);
float _Lerp_a1fed41c0847449ab72ae8fe4775270c_Out_3_Float;
Unity_Lerp_float(float(0), float(3), _Add_a196acc146d1466fb309ec050e77feb2_Out_2_Float, _Lerp_a1fed41c0847449ab72ae8fe4775270c_Out_3_Float);
float _Clamp_d4d6e762b7b1422d936b98d7f9429754_Out_3_Float;
Unity_Clamp_float(_Lerp_a1fed41c0847449ab72ae8fe4775270c_Out_3_Float, float(0), float(1), _Clamp_d4d6e762b7b1422d936b98d7f9429754_Out_3_Float);
float _Property_985b04ddd40847029dbfa829fbcaeed1_Out_0_Float = _Sloap_Contrast;
float3 _Contrast_2864897542064adcb4b61fcccadaf026_Out_2_Vector3;
Unity_Contrast_float((_Clamp_d4d6e762b7b1422d936b98d7f9429754_Out_3_Float.xxx), _Property_985b04ddd40847029dbfa829fbcaeed1_Out_0_Float, _Contrast_2864897542064adcb4b61fcccadaf026_Out_2_Vector3);
float3 _Clamp_b8f1f82d045f437a910a54b62bb2df81_Out_3_Vector3;
Unity_Clamp_float3(_Contrast_2864897542064adcb4b61fcccadaf026_Out_2_Vector3, float3(0, 0, 0), float3(1, 1, 1), _Clamp_b8f1f82d045f437a910a54b62bb2df81_Out_3_Vector3);
float3 _Transform_229ffe25002343a39c8caaa3d7f6df91_Out_1_Vector3;
_Transform_229ffe25002343a39c8caaa3d7f6df91_Out_1_Vector3 = TransformObjectToWorldDir(IN.ObjectSpacePosition.xyz, true);
float3 _Vector3_ec86905b34c54695bed5b7d1975cb6ce_Out_0_Vector3 = float3(float(0), float(2), float(0));
float _DotProduct_af3cb3caf6e4473c8161868b15c0255b_Out_2_Float;
Unity_DotProduct_float3(_Transform_229ffe25002343a39c8caaa3d7f6df91_Out_1_Vector3, _Vector3_ec86905b34c54695bed5b7d1975cb6ce_Out_0_Vector3, _DotProduct_af3cb3caf6e4473c8161868b15c0255b_Out_2_Float);
float _Add_47be9b48b4c94dd28e8c3db7f6ba1a05_Out_2_Float;
Unity_Add_float(_Property_6a995c4e80824d6fa7d458ddcdc4eec3_Out_0_Float, _DotProduct_af3cb3caf6e4473c8161868b15c0255b_Out_2_Float, _Add_47be9b48b4c94dd28e8c3db7f6ba1a05_Out_2_Float);
float _Multiply_35491cd334cb4e34a76b48de2bc5814a_Out_2_Float;
Unity_Multiply_float_float(_Add_47be9b48b4c94dd28e8c3db7f6ba1a05_Out_2_Float, 2, _Multiply_35491cd334cb4e34a76b48de2bc5814a_Out_2_Float);
float _Clamp_ccd0969e9da6420f94e7113654fd480e_Out_3_Float;
Unity_Clamp_float(_Multiply_35491cd334cb4e34a76b48de2bc5814a_Out_2_Float, float(0), float(1), _Clamp_ccd0969e9da6420f94e7113654fd480e_Out_3_Float);
float3 _Contrast_454ba7c8fb3a4d03951b073867fa5f14_Out_2_Vector3;
Unity_Contrast_float((_Clamp_ccd0969e9da6420f94e7113654fd480e_Out_3_Float.xxx), _Property_985b04ddd40847029dbfa829fbcaeed1_Out_0_Float, _Contrast_454ba7c8fb3a4d03951b073867fa5f14_Out_2_Vector3);
float3 _Clamp_1ba5cf0aee8245bd9dca155954c3df81_Out_3_Vector3;
Unity_Clamp_float3(_Contrast_454ba7c8fb3a4d03951b073867fa5f14_Out_2_Vector3, float3(0, 0, 0), float3(1, 1, 1), _Clamp_1ba5cf0aee8245bd9dca155954c3df81_Out_3_Vector3);
UnityTexture2D _Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D = _Slope_Noise_Texture;
float _Property_8e663ddd1476448d948a9de1bf1f83a9_Out_0_Float = _Noise_Texture_Size;
float3 Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_UV = IN.ObjectSpacePosition * _Property_8e663ddd1476448d948a9de1bf1f83a9_Out_0_Float;
float3 Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend = SafePositivePow_float(IN.ObjectSpaceNormal, min(float(1), floor(log2(Min_float())/log2(1/sqrt(3)))) );
Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend /= dot(Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend, 1.0);
float4 Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_X = SAMPLE_TEXTURE2D(_Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.tex, _Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.samplerstate, Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_UV.zy);
float4 Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Y = SAMPLE_TEXTURE2D(_Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.tex, _Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.samplerstate, Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_UV.xz);
float4 Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Z = SAMPLE_TEXTURE2D(_Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.tex, _Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.samplerstate, Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_UV.xy);
float4 _Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Out_0_Vector4 = Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_X * Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend.x + Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Y * Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend.y + Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Z * Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend.z;
float _Swizzle_90fb1902a2bd4070b4f9e27ac287ca97_Out_1_Float = (_Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Out_0_Vector4.xy).y;
float _Clamp_f32c97c6ccbf44649bd757503ccdbe71_Out_3_Float;
Unity_Clamp_float(_Swizzle_90fb1902a2bd4070b4f9e27ac287ca97_Out_1_Float, float(0), float(1), _Clamp_f32c97c6ccbf44649bd757503ccdbe71_Out_3_Float);
float _Add_92d15430b3c042719d65f1252bc35d7a_Out_2_Float;
Unity_Add_float(_Multiply_35491cd334cb4e34a76b48de2bc5814a_Out_2_Float, _Clamp_f32c97c6ccbf44649bd757503ccdbe71_Out_3_Float, _Add_92d15430b3c042719d65f1252bc35d7a_Out_2_Float);
float3 _Contrast_c2fe106c51f0498e987cf31090ec563d_Out_2_Vector3;
Unity_Contrast_float((_Add_92d15430b3c042719d65f1252bc35d7a_Out_2_Float.xxx), _Property_985b04ddd40847029dbfa829fbcaeed1_Out_0_Float, _Contrast_c2fe106c51f0498e987cf31090ec563d_Out_2_Vector3);
float3 _Clamp_05438405442c47cb98e571d128277bbe_Out_3_Vector3;
Unity_Clamp_float3(_Contrast_c2fe106c51f0498e987cf31090ec563d_Out_2_Vector3, float3(0, 0, 0), float3(1, 1, 1), _Clamp_05438405442c47cb98e571d128277bbe_Out_3_Vector3);
#if defined(_EDGEBLENDING_NORMALBLENDING)
float3 _EdgeBlending_cdbe33a15cf6452b8881ecea22adcb84_Out_0_Vector3 = _Clamp_b8f1f82d045f437a910a54b62bb2df81_Out_3_Vector3;
#elif defined(_EDGEBLENDING_VERTEXBLENDING)
float3 _EdgeBlending_cdbe33a15cf6452b8881ecea22adcb84_Out_0_Vector3 = _Clamp_1ba5cf0aee8245bd9dca155954c3df81_Out_3_Vector3;
#else
float3 _EdgeBlending_cdbe33a15cf6452b8881ecea22adcb84_Out_0_Vector3 = _Clamp_05438405442c47cb98e571d128277bbe_Out_3_Vector3;
#endif
OutVector3_1 = _EdgeBlending_cdbe33a15cf6452b8881ecea22adcb84_Out_0_Vector3;
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Branch_float3(float Predicate, float3 True, float3 False, out float3 Out)
{
    Out = Predicate ? True : False;
}

void Unity_Negate_float3(float3 In, out float3 Out)
{
    Out = -1 * In;
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
description.Position = IN.ObjectSpacePosition;
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
float Alpha;
float AlphaClipThreshold;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
float _Property_868c674f556d42be9071980dc60f4aed_Out_0_Boolean = _EnableCoverage;
float _Property_a0b21575a96c42a2b05aa3518645b2f4_Out_0_Float = _UV_Size;
UnityTexture2D _Property_4d248f4a9de94d7bb9a5c3c7c4c10a68_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_ColorMap, sampler_ColorMap, _ColorMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _Property_061dd20995c54e4b8c0357aa709c6e56_Out_0_Vector4 = _ColorTint;
UnityTexture2D _Property_cc904da6b4714cd6a9559b3bcf6ba7a6_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_NormalMap, sampler_NormalMap, _NormalMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_49f412000c0b4f18863fb7afae8b43fc_Out_0_Float = _NormalIntensity;
UnityTexture2D _Property_3fcfe917c75d4a46bfe50da31a24c74f_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_ORMMap, sampler_ORMMap, _ORMMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_bd35b765c62349f285447de81728f0c8_Out_0_Float = _RoughnessIntensity;
float _Property_f125a72836c24b0fa5ef72823c8e6452_Out_0_Float = _AOIntensity;
float _Property_0251a196075947af9d3ad28d6875a5ef_Out_0_Float = _MetallicIntensity;
UnityTexture2D _Property_47ecc092fbd144218fe6440250e367b3_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Emissive_Map, sampler_Emissive_Map, _Emissive_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _Property_c065f0d4df8c40528116ee8e516b2221_Out_0_Vector4 = _EmissiveColor;
float _Property_0038e41f028740e4abc1958ca58302a1_Out_0_Float = _EmissiveIntensity;
Bindings_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe;
_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe.uv0 = IN.uv0;
float4 _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_BaseColor_5_Vector4;
float3 _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Normal_1_Vector3;
float4 _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Emissive_6_Vector4;
float _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_AmbientOcclusion_4_Float;
float _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Roughness_3_Float;
float _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Metallic_2_Float;
SG_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float(_Property_a0b21575a96c42a2b05aa3518645b2f4_Out_0_Float, _Property_4d248f4a9de94d7bb9a5c3c7c4c10a68_Out_0_Texture2D, _Property_061dd20995c54e4b8c0357aa709c6e56_Out_0_Vector4, _Property_cc904da6b4714cd6a9559b3bcf6ba7a6_Out_0_Texture2D, _Property_49f412000c0b4f18863fb7afae8b43fc_Out_0_Float, _Property_3fcfe917c75d4a46bfe50da31a24c74f_Out_0_Texture2D, _Property_bd35b765c62349f285447de81728f0c8_Out_0_Float, _Property_f125a72836c24b0fa5ef72823c8e6452_Out_0_Float, _Property_0251a196075947af9d3ad28d6875a5ef_Out_0_Float, _Property_47ecc092fbd144218fe6440250e367b3_Out_0_Texture2D, _Property_c065f0d4df8c40528116ee8e516b2221_Out_0_Vector4, _Property_0038e41f028740e4abc1958ca58302a1_Out_0_Float, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_BaseColor_5_Vector4, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Normal_1_Vector3, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Emissive_6_Vector4, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_AmbientOcclusion_4_Float, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Roughness_3_Float, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Metallic_2_Float);
float _Property_623a0c9d694c4c33b6256bde952c53e5_Out_0_Float = _CoverageSize;
UnityTexture2D _Property_58d380b2034145c8b7d2eda637dbc296_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_CoverageColorMap, sampler_CoverageColorMap, _CoverageColorMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
UnityTexture2D _Property_f324dd6628ea45b1a630d750fa3d6b66_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_CoverageNormalMap, sampler_CoverageNormalMap, _CoverageNormalMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_b00b34515282450295cc4f4eb7b4a0ce_Out_0_Float = _CoverageNormalIntensity;
UnityTexture2D _Property_24ae6469bfeb4dada6fb7c7d39985cb3_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_CoverageORMMap, sampler_CoverageORMMap, _CoverageORMMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_03c29a27092f4bbb8a4fcc3b6e7b2909_Out_0_Float = _CoverageRoughnessIntensity;
float _Property_07cad64e572e4602a346b7dd90c3eb21_Out_0_Float = _CoverageMetallicIntensity;
Bindings_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c;
_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c.uv0 = IN.uv0;
float4 _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_BaseColor_5_Vector4;
float3 _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Normal_1_Vector3;
float4 _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Emissive_6_Vector4;
float _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_AmbientOcclusion_4_Float;
float _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Roughness_3_Float;
float _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Metallic_2_Float;
SG_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float(_Property_623a0c9d694c4c33b6256bde952c53e5_Out_0_Float, _Property_58d380b2034145c8b7d2eda637dbc296_Out_0_Texture2D, float4 (1, 1, 1, 1), _Property_f324dd6628ea45b1a630d750fa3d6b66_Out_0_Texture2D, _Property_b00b34515282450295cc4f4eb7b4a0ce_Out_0_Float, _Property_24ae6469bfeb4dada6fb7c7d39985cb3_Out_0_Texture2D, _Property_03c29a27092f4bbb8a4fcc3b6e7b2909_Out_0_Float, float(0), _Property_07cad64e572e4602a346b7dd90c3eb21_Out_0_Float, UnityBuildTexture2DStructNoScale(_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D), float4 (0, 0, 0, 0), float(0), _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_BaseColor_5_Vector4, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Normal_1_Vector3, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Emissive_6_Vector4, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_AmbientOcclusion_4_Float, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Roughness_3_Float, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Metallic_2_Float);
float _Property_0785fc58d71d4d63abb8c1102aa1a1b7_Out_0_Float = _SloapContrast;
UnityTexture2D _Property_12921d1841154c59b11aefccb2cbca31_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_SlopeNoiseTexture, sampler_SlopeNoiseTexture, _SlopeNoiseTexture_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_10dd46128137476fb3fd49de6154e94c_Out_0_Float = _NoiseTextureSize;
float _Property_a654ef8b1e2a4529ab82f92b3ae202ec_Out_0_Float = _SloapOffset;
Bindings_PASGVerticalBlend_e9f0b2d5820244846982772499370842_float _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d;
_PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d.ObjectSpaceNormal = IN.ObjectSpaceNormal;
_PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d.WorldSpaceNormal = IN.WorldSpaceNormal;
_PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d.WorldSpaceTangent = IN.WorldSpaceTangent;
_PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d.WorldSpaceBiTangent = IN.WorldSpaceBiTangent;
_PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d.ObjectSpacePosition = IN.ObjectSpacePosition;
float3 _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d_OutVector3_1_Vector3;
SG_PASGVerticalBlend_e9f0b2d5820244846982772499370842_float(_Property_0785fc58d71d4d63abb8c1102aa1a1b7_Out_0_Float, _Property_12921d1841154c59b11aefccb2cbca31_Out_0_Texture2D, _Property_10dd46128137476fb3fd49de6154e94c_Out_0_Float, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Normal_1_Vector3, _Property_a654ef8b1e2a4529ab82f92b3ae202ec_Out_0_Float, _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d, _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d_OutVector3_1_Vector3);
float3 _Lerp_8ef5a2e39e934fb09e1bc6858ba97959_Out_3_Vector3;
Unity_Lerp_float3((_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_BaseColor_5_Vector4.xyz), (_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_BaseColor_5_Vector4.xyz), _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d_OutVector3_1_Vector3, _Lerp_8ef5a2e39e934fb09e1bc6858ba97959_Out_3_Vector3);
float3 _Branch_72adc4bf3fd64427a9a9035afa6f856a_Out_3_Vector3;
Unity_Branch_float3(_Property_868c674f556d42be9071980dc60f4aed_Out_0_Boolean, _Lerp_8ef5a2e39e934fb09e1bc6858ba97959_Out_3_Vector3, (_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_BaseColor_5_Vector4.xyz), _Branch_72adc4bf3fd64427a9a9035afa6f856a_Out_3_Vector3);
float _IsFrontFace_8b8f9781e7a946f18c8c49da20d0925c_Out_0_Boolean = max(0, IN.FaceSign.x);
float3 _Lerp_e69be27d4f0d4e26a37c622c629ca56d_Out_3_Vector3;
Unity_Lerp_float3(_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Normal_1_Vector3, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Normal_1_Vector3, _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d_OutVector3_1_Vector3, _Lerp_e69be27d4f0d4e26a37c622c629ca56d_Out_3_Vector3);
float3 _Branch_3213119f26d54fc08b755c491d9dd6c4_Out_3_Vector3;
Unity_Branch_float3(_Property_868c674f556d42be9071980dc60f4aed_Out_0_Boolean, _Lerp_e69be27d4f0d4e26a37c622c629ca56d_Out_3_Vector3, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Normal_1_Vector3, _Branch_3213119f26d54fc08b755c491d9dd6c4_Out_3_Vector3);
float3 _Negate_c9035ab4407c48978c2cd379c7ed3db2_Out_1_Vector3;
Unity_Negate_float3(_Branch_3213119f26d54fc08b755c491d9dd6c4_Out_3_Vector3, _Negate_c9035ab4407c48978c2cd379c7ed3db2_Out_1_Vector3);
float3 _Branch_6a68e6f7151b40c684e82041ff10474e_Out_3_Vector3;
Unity_Branch_float3(_IsFrontFace_8b8f9781e7a946f18c8c49da20d0925c_Out_0_Boolean, _Branch_3213119f26d54fc08b755c491d9dd6c4_Out_3_Vector3, _Negate_c9035ab4407c48978c2cd379c7ed3db2_Out_1_Vector3, _Branch_6a68e6f7151b40c684e82041ff10474e_Out_3_Vector3);
float3 _Lerp_e6a3b112a78740919a21e1a67f8a9012_Out_3_Vector3;
Unity_Lerp_float3((_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Roughness_3_Float.xxx), (_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Roughness_3_Float.xxx), _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d_OutVector3_1_Vector3, _Lerp_e6a3b112a78740919a21e1a67f8a9012_Out_3_Vector3);
float3 _Branch_e3527157b2634e17853db32690e6cf9f_Out_3_Vector3;
Unity_Branch_float3(_Property_868c674f556d42be9071980dc60f4aed_Out_0_Boolean, _Lerp_e6a3b112a78740919a21e1a67f8a9012_Out_3_Vector3, (_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Metallic_2_Float.xxx), _Branch_e3527157b2634e17853db32690e6cf9f_Out_3_Vector3);
float3 _Lerp_0bf96dce18b642f4ab3b848bb1fe45d6_Out_3_Vector3;
Unity_Lerp_float3((_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Metallic_2_Float.xxx), (_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Metallic_2_Float.xxx), _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d_OutVector3_1_Vector3, _Lerp_0bf96dce18b642f4ab3b848bb1fe45d6_Out_3_Vector3);
float3 _Branch_bcdc5fe84435477ab329331520e86c7d_Out_3_Vector3;
Unity_Branch_float3(_Property_868c674f556d42be9071980dc60f4aed_Out_0_Boolean, _Lerp_0bf96dce18b642f4ab3b848bb1fe45d6_Out_3_Vector3, (_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Roughness_3_Float.xxx), _Branch_bcdc5fe84435477ab329331520e86c7d_Out_3_Vector3);
float3 _Lerp_1ffb4b0a9a70465db54818dd6df0be7c_Out_3_Vector3;
Unity_Lerp_float3((_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_AmbientOcclusion_4_Float.xxx), (_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_AmbientOcclusion_4_Float.xxx), _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d_OutVector3_1_Vector3, _Lerp_1ffb4b0a9a70465db54818dd6df0be7c_Out_3_Vector3);
float3 _Branch_c19f53b6fc08465e849902625a3db36e_Out_3_Vector3;
Unity_Branch_float3(_Property_868c674f556d42be9071980dc60f4aed_Out_0_Boolean, _Lerp_1ffb4b0a9a70465db54818dd6df0be7c_Out_3_Vector3, (_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_AmbientOcclusion_4_Float.xxx), _Branch_c19f53b6fc08465e849902625a3db36e_Out_3_Vector3);
UnityTexture2D _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_ColorMap_1, sampler_ColorMap_1, _ColorMap_1_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.tex, _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.samplerstate, _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4, _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_R_4_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.r;
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_G_5_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.g;
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_B_6_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.b;
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_A_7_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.a;
float _Property_7a43d5f1d3df46c2869bc70eebee1f91_Out_0_Float = _EmissiveIntensity_1;
surface.BaseColor = _Branch_72adc4bf3fd64427a9a9035afa6f856a_Out_3_Vector3;
surface.NormalTS = _Branch_6a68e6f7151b40c684e82041ff10474e_Out_3_Vector3;
surface.Emission = (_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Emissive_6_Vector4.xyz);
surface.Metallic = (_Branch_e3527157b2634e17853db32690e6cf9f_Out_3_Vector3).x;
surface.Smoothness = (_Branch_bcdc5fe84435477ab329331520e86c7d_Out_3_Vector3).x;
surface.Occlusion = (_Branch_c19f53b6fc08465e849902625a3db36e_Out_3_Vector3).x;
surface.Alpha = (_SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4).x;
surface.AlphaClipThreshold = _Property_7a43d5f1d3df46c2869bc70eebee1f91_Out_0_Float;
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

    // use bitangent on the fly like in hdrp
    // IMPORTANT! If we ever support Flip on double sided materials ensure bitangent and tangent are NOT flipped.
    float crossSign = (input.tangentWS.w > 0.0 ? 1.0 : -1.0)* GetOddNegativeScale();
    float3 bitang = crossSign * cross(input.normalWS.xyz, input.tangentWS.xyz);

    output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph
    output.ObjectSpaceNormal = normalize(mul(output.WorldSpaceNormal, (float3x3) UNITY_MATRIX_M));           // transposed multiplication by inverse matrix to handle normal scale
    output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);

    // to preserve mikktspace compliance we use same scale renormFactor as was used on the normal.
    // This is explained in section 2.2 in "surface gradient based bump mapping framework"
    output.WorldSpaceTangent = renormFactor * input.tangentWS.xyz;
    output.WorldSpaceBiTangent = renormFactor * bitang;

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
    BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
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
#pragma shader_feature_local_fragment _ _BUILTIN_SURFACE_TYPE_TRANSPARENT
#pragma shader_feature_local_fragment _ _BUILTIN_AlphaClip
#pragma shader_feature_local_fragment _ _BUILTIN_ALPHATEST_ON
#pragma shader_feature_local _EDGEBLENDING_NORMALBLENDING _EDGEBLENDING_VERTEXBLENDING _EDGEBLENDING_NOISEBLENDING

#if defined(_EDGEBLENDING_NORMALBLENDING)
#define KEYWORD_PERMUTATION_0
#elif defined(_EDGEBLENDING_VERTEXBLENDING)
#define KEYWORD_PERMUTATION_1
#else
#define KEYWORD_PERMUTATION_2
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
#define VARYINGS_NEED_CULLFACE
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
 float3 ObjectSpaceNormal;
 float3 WorldSpaceNormal;
 float3 TangentSpaceNormal;
 float3 WorldSpaceTangent;
 float3 WorldSpaceBiTangent;
 float3 ObjectSpacePosition;
 float4 uv0;
 float FaceSign;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
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

float4 _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D_TexelSize;
float4 _ColorMap_TexelSize;
float4 _ColorMap_1_TexelSize;
float4 _NormalMap_TexelSize;
float4 _ColorTint;
float _NormalIntensity;
float _UV_Size;
float4 _ORMMap_TexelSize;
float _RoughnessIntensity;
float _AOIntensity;
float _MetallicIntensity;
float4 _EmissiveColor;
float _EmissiveIntensity_1;
float4 _Emissive_Map_TexelSize;
float _EmissiveIntensity;
float _EnableCoverage;
float _CoverageSize;
float _CoverageNormalIntensity;
float _CoverageRoughnessIntensity;
float _CoverageMetallicIntensity;
float _NoiseTextureSize;
float4 _SlopeNoiseTexture_TexelSize;
float4 _CoverageColorMap_TexelSize;
float4 _CoverageORMMap_TexelSize;
float4 _CoverageNormalMap_TexelSize;
float _SloapOffset;
float _SloapContrast;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D);
SAMPLER(sampler_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D);
TEXTURE2D(_ColorMap);
SAMPLER(sampler_ColorMap);
TEXTURE2D(_ColorMap_1);
SAMPLER(sampler_ColorMap_1);
TEXTURE2D(_NormalMap);
SAMPLER(sampler_NormalMap);
TEXTURE2D(_ORMMap);
SAMPLER(sampler_ORMMap);
TEXTURE2D(_Emissive_Map);
SAMPLER(sampler_Emissive_Map);
TEXTURE2D(_SlopeNoiseTexture);
SAMPLER(sampler_SlopeNoiseTexture);
TEXTURE2D(_CoverageColorMap);
SAMPLER(sampler_CoverageColorMap);
TEXTURE2D(_CoverageORMMap);
SAMPLER(sampler_CoverageORMMap);
TEXTURE2D(_CoverageNormalMap);
SAMPLER(sampler_CoverageNormalMap);

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

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

void Unity_NormalStrength_float(float3 In, float Strength, out float3 Out)
{
    Out = float3(In.rg * Strength, lerp(1, In.b, saturate(Strength)));
}

void Unity_Lerp_float(float A, float B, float T, out float Out)
{
    Out = lerp(A, B, T);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
}

struct Bindings_PASGORMUnpacker_abcd41e335c426a4bb0f8de17b4b5f96_float
{
};

void SG_PASGORMUnpacker_abcd41e335c426a4bb0f8de17b4b5f96_float(float2 _UV, UnityTexture2D _ORM_Map, float _AO_Intensity, float _Roughness_Intensity, float _Metallic_Intensity, Bindings_PASGORMUnpacker_abcd41e335c426a4bb0f8de17b4b5f96_float IN, out float Ambient_Occlusion_1, out float Roughness_2, out float Metallic_3)
{
float _Float_2c47c335e2304faa927a789be883bb19_Out_0_Float = float(1);
UnityTexture2D _Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D = _ORM_Map;
float2 _Property_ad3893fa315b474ea60e7ee3e265957c_Out_0_Vector2 = _UV;
float4 _SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D.tex, _Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D.samplerstate, _Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D.GetTransformedUV(_Property_ad3893fa315b474ea60e7ee3e265957c_Out_0_Vector2) );
if (_Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4, _Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_719055d2c0184958bd1329f59130cce0_R_4_Float = _SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4.r;
float _SampleTexture2D_719055d2c0184958bd1329f59130cce0_G_5_Float = _SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4.g;
float _SampleTexture2D_719055d2c0184958bd1329f59130cce0_B_6_Float = _SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4.b;
float _SampleTexture2D_719055d2c0184958bd1329f59130cce0_A_7_Float = _SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4.a;
float _Property_90898696aae3458aa180c6f15d96aaee_Out_0_Float = _AO_Intensity;
float _Lerp_bf670034cefc4c6d9e02b57f5394811d_Out_3_Float;
Unity_Lerp_float(_Float_2c47c335e2304faa927a789be883bb19_Out_0_Float, _SampleTexture2D_719055d2c0184958bd1329f59130cce0_R_4_Float, _Property_90898696aae3458aa180c6f15d96aaee_Out_0_Float, _Lerp_bf670034cefc4c6d9e02b57f5394811d_Out_3_Float);
float _Property_e77944e4743744198e931506300c92ca_Out_0_Float = _Roughness_Intensity;
float _Multiply_43e873dc525645aab065eb6fc56ad77f_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_719055d2c0184958bd1329f59130cce0_G_5_Float, _Property_e77944e4743744198e931506300c92ca_Out_0_Float, _Multiply_43e873dc525645aab065eb6fc56ad77f_Out_2_Float);
float _OneMinus_3225b57b02ed4eaf9c118419fbefc479_Out_1_Float;
Unity_OneMinus_float(_Multiply_43e873dc525645aab065eb6fc56ad77f_Out_2_Float, _OneMinus_3225b57b02ed4eaf9c118419fbefc479_Out_1_Float);
float _Property_c0c103f0dfcd475fa428b0e043905944_Out_0_Float = _Metallic_Intensity;
float _Multiply_38b5ca9e78ba40d9b861ffc15889bd7f_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_719055d2c0184958bd1329f59130cce0_B_6_Float, _Property_c0c103f0dfcd475fa428b0e043905944_Out_0_Float, _Multiply_38b5ca9e78ba40d9b861ffc15889bd7f_Out_2_Float);
Ambient_Occlusion_1 = _Lerp_bf670034cefc4c6d9e02b57f5394811d_Out_3_Float;
Roughness_2 = _OneMinus_3225b57b02ed4eaf9c118419fbefc479_Out_1_Float;
Metallic_3 = _Multiply_38b5ca9e78ba40d9b861ffc15889bd7f_Out_2_Float;
}

struct Bindings_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float
{
half4 uv0;
};

void SG_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float(float _Texture_Size, UnityTexture2D _Color_Map, float4 _Color_Tint, UnityTexture2D _Normal_Map, float _Normal_Intensity, UnityTexture2D _ORM_Map, float _Roughness_Intensity, float _AO_Intensity, float _Metallic_Intensity, UnityTexture2D _Emissive_Map, float4 _Emissive_Color, float _Emissive_Intensity, Bindings_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float IN, out float4 Base_Color_5, out float3 Normal_1, out float4 Emissive_6, out float Ambient_Occlusion_4, out float Roughness_3, out float Metallic_2)
{
UnityTexture2D _Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D = _Color_Map;
float _Property_6e866f25d28140918dd5249261b96e76_Out_0_Float = _Texture_Size;
float2 _TilingAndOffset_49c249d74ea54ccf979206ad2c3c0c00_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, (_Property_6e866f25d28140918dd5249261b96e76_Out_0_Float.xx), float2 (0, 0), _TilingAndOffset_49c249d74ea54ccf979206ad2c3c0c00_Out_3_Vector2);
float _Property_94852b2b32f549138996f0dfb35b0486_Out_0_Float = _Texture_Size;
float2 _Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2;
Unity_Multiply_float2_float2(_TilingAndOffset_49c249d74ea54ccf979206ad2c3c0c00_Out_3_Vector2, (_Property_94852b2b32f549138996f0dfb35b0486_Out_0_Float.xx), _Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2);
float4 _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D.tex, _Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D.samplerstate, _Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D.GetTransformedUV(_Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2) );
if (_Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4, _Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_R_4_Float = _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4.r;
float _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_G_5_Float = _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4.g;
float _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_B_6_Float = _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4.b;
float _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_A_7_Float = _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4.a;
float4 _Property_c081d1852d284e4fa4bbb04ea3adf35e_Out_0_Vector4 = _Color_Tint;
float4 _Multiply_ff3b38102028498db7cc80acf5e42362_Out_2_Vector4;
Unity_Multiply_float4_float4(_SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4, _Property_c081d1852d284e4fa4bbb04ea3adf35e_Out_0_Vector4, _Multiply_ff3b38102028498db7cc80acf5e42362_Out_2_Vector4);
UnityTexture2D _Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D = _Normal_Map;
float4 _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D.tex, _Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D.samplerstate, _Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D.GetTransformedUV(_Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2) );
if (_Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4, _Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4);
float _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_R_4_Float = _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.r;
float _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_G_5_Float = _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.g;
float _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_B_6_Float = _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.b;
float _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_A_7_Float = _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.a;
float _Property_8b1f839dfbcd4481b201427fbc7310ad_Out_0_Float = _Normal_Intensity;
float3 _NormalStrength_a218ef03cc884bf0b0962d85b628afe7_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.xyz), _Property_8b1f839dfbcd4481b201427fbc7310ad_Out_0_Float, _NormalStrength_a218ef03cc884bf0b0962d85b628afe7_Out_2_Vector3);
UnityTexture2D _Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D = _Emissive_Map;
float4 _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D.tex, _Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D.samplerstate, _Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D.GetTransformedUV(_Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2) );
if (_Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4, _Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_R_4_Float = _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4.r;
float _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_G_5_Float = _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4.g;
float _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_B_6_Float = _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4.b;
float _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_A_7_Float = _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4.a;
float4 _Property_3a2f8dbe365d427e89744f8eb8dabd44_Out_0_Vector4 = _Emissive_Color;
float4 _Multiply_1efa7dc8f3994d8ea213351379d48448_Out_2_Vector4;
Unity_Multiply_float4_float4(_SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4, _Property_3a2f8dbe365d427e89744f8eb8dabd44_Out_0_Vector4, _Multiply_1efa7dc8f3994d8ea213351379d48448_Out_2_Vector4);
float _Property_09c3fe7e22be43caa45c324f52ea8137_Out_0_Float = _Emissive_Intensity;
float4 _Multiply_43a1c0123fb34a25a5d21581f08926f8_Out_2_Vector4;
Unity_Multiply_float4_float4(_Multiply_1efa7dc8f3994d8ea213351379d48448_Out_2_Vector4, (_Property_09c3fe7e22be43caa45c324f52ea8137_Out_0_Float.xxxx), _Multiply_43a1c0123fb34a25a5d21581f08926f8_Out_2_Vector4);
UnityTexture2D _Property_ecbd8a4e938a458cabefe2480cada231_Out_0_Texture2D = _ORM_Map;
float _Property_2ca0dc7be16d47bfb0faf210c4d70757_Out_0_Float = _AO_Intensity;
float _Property_fb245dde136f4b2486e260e720f83c83_Out_0_Float = _Roughness_Intensity;
float _Property_0fcde0f68f364243886b868db18a8d27_Out_0_Float = _Metallic_Intensity;
Bindings_PASGORMUnpacker_abcd41e335c426a4bb0f8de17b4b5f96_float _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94;
half _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_AmbientOcclusion_1_Float;
half _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Roughness_2_Float;
half _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Metallic_3_Float;
SG_PASGORMUnpacker_abcd41e335c426a4bb0f8de17b4b5f96_float(_Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2, _Property_ecbd8a4e938a458cabefe2480cada231_Out_0_Texture2D, _Property_2ca0dc7be16d47bfb0faf210c4d70757_Out_0_Float, _Property_fb245dde136f4b2486e260e720f83c83_Out_0_Float, _Property_0fcde0f68f364243886b868db18a8d27_Out_0_Float, _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94, _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_AmbientOcclusion_1_Float, _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Roughness_2_Float, _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Metallic_3_Float);
Base_Color_5 = _Multiply_ff3b38102028498db7cc80acf5e42362_Out_2_Vector4;
Normal_1 = _NormalStrength_a218ef03cc884bf0b0962d85b628afe7_Out_2_Vector3;
Emissive_6 = _Multiply_43a1c0123fb34a25a5d21581f08926f8_Out_2_Vector4;
Ambient_Occlusion_4 = _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_AmbientOcclusion_1_Float;
Roughness_3 = _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Roughness_2_Float;
Metallic_2 = _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Metallic_3_Float;
}

void Unity_DotProduct_float3(float3 A, float3 B, out float Out)
{
    Out = dot(A, B);
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Clamp_float(float In, float Min, float Max, out float Out)
{
    Out = clamp(In, Min, Max);
}

void Unity_Contrast_float(float3 In, float Contrast, out float3 Out)
{
    float midpoint = pow(0.5, 2.2);
    Out =  (In - midpoint) * Contrast + midpoint;
}

void Unity_Clamp_float3(float3 In, float3 Min, float3 Max, out float3 Out)
{
    Out = clamp(In, Min, Max);
}

struct Bindings_PASGVerticalBlend_e9f0b2d5820244846982772499370842_float
{
float3 ObjectSpaceNormal;
float3 WorldSpaceNormal;
float3 WorldSpaceTangent;
float3 WorldSpaceBiTangent;
float3 ObjectSpacePosition;
};

void SG_PASGVerticalBlend_e9f0b2d5820244846982772499370842_float(float _Sloap_Contrast, UnityTexture2D _Slope_Noise_Texture, float _Noise_Texture_Size, float3 _Explicit_Normal, float _Sloap_Offset, Bindings_PASGVerticalBlend_e9f0b2d5820244846982772499370842_float IN, out float3 OutVector3_1)
{
float3 _Property_5509c7fb30e445229f496a71c6e9c192_Out_0_Vector3 = _Explicit_Normal;
float3 _Transform_9c9f921cd0934012ac0d281c8e40a727_Out_1_Vector3;
{
float3x3 tangentTransform = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
_Transform_9c9f921cd0934012ac0d281c8e40a727_Out_1_Vector3 = TransformTangentToWorld(_Property_5509c7fb30e445229f496a71c6e9c192_Out_0_Vector3.xyz, tangentTransform, true);
}
float _DotProduct_4bc69611266041259c8da1b387474ec9_Out_2_Float;
Unity_DotProduct_float3(_Transform_9c9f921cd0934012ac0d281c8e40a727_Out_1_Vector3, float3(0, 1, 0), _DotProduct_4bc69611266041259c8da1b387474ec9_Out_2_Float);
float _Property_6a995c4e80824d6fa7d458ddcdc4eec3_Out_0_Float = _Sloap_Offset;
float _Add_a196acc146d1466fb309ec050e77feb2_Out_2_Float;
Unity_Add_float(_DotProduct_4bc69611266041259c8da1b387474ec9_Out_2_Float, _Property_6a995c4e80824d6fa7d458ddcdc4eec3_Out_0_Float, _Add_a196acc146d1466fb309ec050e77feb2_Out_2_Float);
float _Lerp_a1fed41c0847449ab72ae8fe4775270c_Out_3_Float;
Unity_Lerp_float(float(0), float(3), _Add_a196acc146d1466fb309ec050e77feb2_Out_2_Float, _Lerp_a1fed41c0847449ab72ae8fe4775270c_Out_3_Float);
float _Clamp_d4d6e762b7b1422d936b98d7f9429754_Out_3_Float;
Unity_Clamp_float(_Lerp_a1fed41c0847449ab72ae8fe4775270c_Out_3_Float, float(0), float(1), _Clamp_d4d6e762b7b1422d936b98d7f9429754_Out_3_Float);
float _Property_985b04ddd40847029dbfa829fbcaeed1_Out_0_Float = _Sloap_Contrast;
float3 _Contrast_2864897542064adcb4b61fcccadaf026_Out_2_Vector3;
Unity_Contrast_float((_Clamp_d4d6e762b7b1422d936b98d7f9429754_Out_3_Float.xxx), _Property_985b04ddd40847029dbfa829fbcaeed1_Out_0_Float, _Contrast_2864897542064adcb4b61fcccadaf026_Out_2_Vector3);
float3 _Clamp_b8f1f82d045f437a910a54b62bb2df81_Out_3_Vector3;
Unity_Clamp_float3(_Contrast_2864897542064adcb4b61fcccadaf026_Out_2_Vector3, float3(0, 0, 0), float3(1, 1, 1), _Clamp_b8f1f82d045f437a910a54b62bb2df81_Out_3_Vector3);
float3 _Transform_229ffe25002343a39c8caaa3d7f6df91_Out_1_Vector3;
_Transform_229ffe25002343a39c8caaa3d7f6df91_Out_1_Vector3 = TransformObjectToWorldDir(IN.ObjectSpacePosition.xyz, true);
float3 _Vector3_ec86905b34c54695bed5b7d1975cb6ce_Out_0_Vector3 = float3(float(0), float(2), float(0));
float _DotProduct_af3cb3caf6e4473c8161868b15c0255b_Out_2_Float;
Unity_DotProduct_float3(_Transform_229ffe25002343a39c8caaa3d7f6df91_Out_1_Vector3, _Vector3_ec86905b34c54695bed5b7d1975cb6ce_Out_0_Vector3, _DotProduct_af3cb3caf6e4473c8161868b15c0255b_Out_2_Float);
float _Add_47be9b48b4c94dd28e8c3db7f6ba1a05_Out_2_Float;
Unity_Add_float(_Property_6a995c4e80824d6fa7d458ddcdc4eec3_Out_0_Float, _DotProduct_af3cb3caf6e4473c8161868b15c0255b_Out_2_Float, _Add_47be9b48b4c94dd28e8c3db7f6ba1a05_Out_2_Float);
float _Multiply_35491cd334cb4e34a76b48de2bc5814a_Out_2_Float;
Unity_Multiply_float_float(_Add_47be9b48b4c94dd28e8c3db7f6ba1a05_Out_2_Float, 2, _Multiply_35491cd334cb4e34a76b48de2bc5814a_Out_2_Float);
float _Clamp_ccd0969e9da6420f94e7113654fd480e_Out_3_Float;
Unity_Clamp_float(_Multiply_35491cd334cb4e34a76b48de2bc5814a_Out_2_Float, float(0), float(1), _Clamp_ccd0969e9da6420f94e7113654fd480e_Out_3_Float);
float3 _Contrast_454ba7c8fb3a4d03951b073867fa5f14_Out_2_Vector3;
Unity_Contrast_float((_Clamp_ccd0969e9da6420f94e7113654fd480e_Out_3_Float.xxx), _Property_985b04ddd40847029dbfa829fbcaeed1_Out_0_Float, _Contrast_454ba7c8fb3a4d03951b073867fa5f14_Out_2_Vector3);
float3 _Clamp_1ba5cf0aee8245bd9dca155954c3df81_Out_3_Vector3;
Unity_Clamp_float3(_Contrast_454ba7c8fb3a4d03951b073867fa5f14_Out_2_Vector3, float3(0, 0, 0), float3(1, 1, 1), _Clamp_1ba5cf0aee8245bd9dca155954c3df81_Out_3_Vector3);
UnityTexture2D _Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D = _Slope_Noise_Texture;
float _Property_8e663ddd1476448d948a9de1bf1f83a9_Out_0_Float = _Noise_Texture_Size;
float3 Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_UV = IN.ObjectSpacePosition * _Property_8e663ddd1476448d948a9de1bf1f83a9_Out_0_Float;
float3 Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend = SafePositivePow_float(IN.ObjectSpaceNormal, min(float(1), floor(log2(Min_float())/log2(1/sqrt(3)))) );
Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend /= dot(Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend, 1.0);
float4 Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_X = SAMPLE_TEXTURE2D(_Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.tex, _Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.samplerstate, Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_UV.zy);
float4 Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Y = SAMPLE_TEXTURE2D(_Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.tex, _Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.samplerstate, Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_UV.xz);
float4 Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Z = SAMPLE_TEXTURE2D(_Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.tex, _Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.samplerstate, Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_UV.xy);
float4 _Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Out_0_Vector4 = Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_X * Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend.x + Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Y * Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend.y + Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Z * Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend.z;
float _Swizzle_90fb1902a2bd4070b4f9e27ac287ca97_Out_1_Float = (_Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Out_0_Vector4.xy).y;
float _Clamp_f32c97c6ccbf44649bd757503ccdbe71_Out_3_Float;
Unity_Clamp_float(_Swizzle_90fb1902a2bd4070b4f9e27ac287ca97_Out_1_Float, float(0), float(1), _Clamp_f32c97c6ccbf44649bd757503ccdbe71_Out_3_Float);
float _Add_92d15430b3c042719d65f1252bc35d7a_Out_2_Float;
Unity_Add_float(_Multiply_35491cd334cb4e34a76b48de2bc5814a_Out_2_Float, _Clamp_f32c97c6ccbf44649bd757503ccdbe71_Out_3_Float, _Add_92d15430b3c042719d65f1252bc35d7a_Out_2_Float);
float3 _Contrast_c2fe106c51f0498e987cf31090ec563d_Out_2_Vector3;
Unity_Contrast_float((_Add_92d15430b3c042719d65f1252bc35d7a_Out_2_Float.xxx), _Property_985b04ddd40847029dbfa829fbcaeed1_Out_0_Float, _Contrast_c2fe106c51f0498e987cf31090ec563d_Out_2_Vector3);
float3 _Clamp_05438405442c47cb98e571d128277bbe_Out_3_Vector3;
Unity_Clamp_float3(_Contrast_c2fe106c51f0498e987cf31090ec563d_Out_2_Vector3, float3(0, 0, 0), float3(1, 1, 1), _Clamp_05438405442c47cb98e571d128277bbe_Out_3_Vector3);
#if defined(_EDGEBLENDING_NORMALBLENDING)
float3 _EdgeBlending_cdbe33a15cf6452b8881ecea22adcb84_Out_0_Vector3 = _Clamp_b8f1f82d045f437a910a54b62bb2df81_Out_3_Vector3;
#elif defined(_EDGEBLENDING_VERTEXBLENDING)
float3 _EdgeBlending_cdbe33a15cf6452b8881ecea22adcb84_Out_0_Vector3 = _Clamp_1ba5cf0aee8245bd9dca155954c3df81_Out_3_Vector3;
#else
float3 _EdgeBlending_cdbe33a15cf6452b8881ecea22adcb84_Out_0_Vector3 = _Clamp_05438405442c47cb98e571d128277bbe_Out_3_Vector3;
#endif
OutVector3_1 = _EdgeBlending_cdbe33a15cf6452b8881ecea22adcb84_Out_0_Vector3;
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Branch_float3(float Predicate, float3 True, float3 False, out float3 Out)
{
    Out = Predicate ? True : False;
}

void Unity_Negate_float3(float3 In, out float3 Out)
{
    Out = -1 * In;
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
description.Position = IN.ObjectSpacePosition;
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
float Alpha;
float AlphaClipThreshold;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
float _Property_868c674f556d42be9071980dc60f4aed_Out_0_Boolean = _EnableCoverage;
float _Property_a0b21575a96c42a2b05aa3518645b2f4_Out_0_Float = _UV_Size;
UnityTexture2D _Property_4d248f4a9de94d7bb9a5c3c7c4c10a68_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_ColorMap, sampler_ColorMap, _ColorMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _Property_061dd20995c54e4b8c0357aa709c6e56_Out_0_Vector4 = _ColorTint;
UnityTexture2D _Property_cc904da6b4714cd6a9559b3bcf6ba7a6_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_NormalMap, sampler_NormalMap, _NormalMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_49f412000c0b4f18863fb7afae8b43fc_Out_0_Float = _NormalIntensity;
UnityTexture2D _Property_3fcfe917c75d4a46bfe50da31a24c74f_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_ORMMap, sampler_ORMMap, _ORMMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_bd35b765c62349f285447de81728f0c8_Out_0_Float = _RoughnessIntensity;
float _Property_f125a72836c24b0fa5ef72823c8e6452_Out_0_Float = _AOIntensity;
float _Property_0251a196075947af9d3ad28d6875a5ef_Out_0_Float = _MetallicIntensity;
UnityTexture2D _Property_47ecc092fbd144218fe6440250e367b3_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Emissive_Map, sampler_Emissive_Map, _Emissive_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _Property_c065f0d4df8c40528116ee8e516b2221_Out_0_Vector4 = _EmissiveColor;
float _Property_0038e41f028740e4abc1958ca58302a1_Out_0_Float = _EmissiveIntensity;
Bindings_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe;
_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe.uv0 = IN.uv0;
float4 _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_BaseColor_5_Vector4;
float3 _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Normal_1_Vector3;
float4 _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Emissive_6_Vector4;
float _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_AmbientOcclusion_4_Float;
float _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Roughness_3_Float;
float _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Metallic_2_Float;
SG_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float(_Property_a0b21575a96c42a2b05aa3518645b2f4_Out_0_Float, _Property_4d248f4a9de94d7bb9a5c3c7c4c10a68_Out_0_Texture2D, _Property_061dd20995c54e4b8c0357aa709c6e56_Out_0_Vector4, _Property_cc904da6b4714cd6a9559b3bcf6ba7a6_Out_0_Texture2D, _Property_49f412000c0b4f18863fb7afae8b43fc_Out_0_Float, _Property_3fcfe917c75d4a46bfe50da31a24c74f_Out_0_Texture2D, _Property_bd35b765c62349f285447de81728f0c8_Out_0_Float, _Property_f125a72836c24b0fa5ef72823c8e6452_Out_0_Float, _Property_0251a196075947af9d3ad28d6875a5ef_Out_0_Float, _Property_47ecc092fbd144218fe6440250e367b3_Out_0_Texture2D, _Property_c065f0d4df8c40528116ee8e516b2221_Out_0_Vector4, _Property_0038e41f028740e4abc1958ca58302a1_Out_0_Float, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_BaseColor_5_Vector4, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Normal_1_Vector3, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Emissive_6_Vector4, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_AmbientOcclusion_4_Float, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Roughness_3_Float, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Metallic_2_Float);
float _Property_623a0c9d694c4c33b6256bde952c53e5_Out_0_Float = _CoverageSize;
UnityTexture2D _Property_58d380b2034145c8b7d2eda637dbc296_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_CoverageColorMap, sampler_CoverageColorMap, _CoverageColorMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
UnityTexture2D _Property_f324dd6628ea45b1a630d750fa3d6b66_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_CoverageNormalMap, sampler_CoverageNormalMap, _CoverageNormalMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_b00b34515282450295cc4f4eb7b4a0ce_Out_0_Float = _CoverageNormalIntensity;
UnityTexture2D _Property_24ae6469bfeb4dada6fb7c7d39985cb3_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_CoverageORMMap, sampler_CoverageORMMap, _CoverageORMMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_03c29a27092f4bbb8a4fcc3b6e7b2909_Out_0_Float = _CoverageRoughnessIntensity;
float _Property_07cad64e572e4602a346b7dd90c3eb21_Out_0_Float = _CoverageMetallicIntensity;
Bindings_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c;
_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c.uv0 = IN.uv0;
float4 _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_BaseColor_5_Vector4;
float3 _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Normal_1_Vector3;
float4 _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Emissive_6_Vector4;
float _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_AmbientOcclusion_4_Float;
float _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Roughness_3_Float;
float _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Metallic_2_Float;
SG_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float(_Property_623a0c9d694c4c33b6256bde952c53e5_Out_0_Float, _Property_58d380b2034145c8b7d2eda637dbc296_Out_0_Texture2D, float4 (1, 1, 1, 1), _Property_f324dd6628ea45b1a630d750fa3d6b66_Out_0_Texture2D, _Property_b00b34515282450295cc4f4eb7b4a0ce_Out_0_Float, _Property_24ae6469bfeb4dada6fb7c7d39985cb3_Out_0_Texture2D, _Property_03c29a27092f4bbb8a4fcc3b6e7b2909_Out_0_Float, float(0), _Property_07cad64e572e4602a346b7dd90c3eb21_Out_0_Float, UnityBuildTexture2DStructNoScale(_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D), float4 (0, 0, 0, 0), float(0), _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_BaseColor_5_Vector4, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Normal_1_Vector3, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Emissive_6_Vector4, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_AmbientOcclusion_4_Float, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Roughness_3_Float, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Metallic_2_Float);
float _Property_0785fc58d71d4d63abb8c1102aa1a1b7_Out_0_Float = _SloapContrast;
UnityTexture2D _Property_12921d1841154c59b11aefccb2cbca31_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_SlopeNoiseTexture, sampler_SlopeNoiseTexture, _SlopeNoiseTexture_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_10dd46128137476fb3fd49de6154e94c_Out_0_Float = _NoiseTextureSize;
float _Property_a654ef8b1e2a4529ab82f92b3ae202ec_Out_0_Float = _SloapOffset;
Bindings_PASGVerticalBlend_e9f0b2d5820244846982772499370842_float _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d;
_PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d.ObjectSpaceNormal = IN.ObjectSpaceNormal;
_PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d.WorldSpaceNormal = IN.WorldSpaceNormal;
_PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d.WorldSpaceTangent = IN.WorldSpaceTangent;
_PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d.WorldSpaceBiTangent = IN.WorldSpaceBiTangent;
_PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d.ObjectSpacePosition = IN.ObjectSpacePosition;
float3 _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d_OutVector3_1_Vector3;
SG_PASGVerticalBlend_e9f0b2d5820244846982772499370842_float(_Property_0785fc58d71d4d63abb8c1102aa1a1b7_Out_0_Float, _Property_12921d1841154c59b11aefccb2cbca31_Out_0_Texture2D, _Property_10dd46128137476fb3fd49de6154e94c_Out_0_Float, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Normal_1_Vector3, _Property_a654ef8b1e2a4529ab82f92b3ae202ec_Out_0_Float, _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d, _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d_OutVector3_1_Vector3);
float3 _Lerp_8ef5a2e39e934fb09e1bc6858ba97959_Out_3_Vector3;
Unity_Lerp_float3((_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_BaseColor_5_Vector4.xyz), (_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_BaseColor_5_Vector4.xyz), _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d_OutVector3_1_Vector3, _Lerp_8ef5a2e39e934fb09e1bc6858ba97959_Out_3_Vector3);
float3 _Branch_72adc4bf3fd64427a9a9035afa6f856a_Out_3_Vector3;
Unity_Branch_float3(_Property_868c674f556d42be9071980dc60f4aed_Out_0_Boolean, _Lerp_8ef5a2e39e934fb09e1bc6858ba97959_Out_3_Vector3, (_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_BaseColor_5_Vector4.xyz), _Branch_72adc4bf3fd64427a9a9035afa6f856a_Out_3_Vector3);
float _IsFrontFace_8b8f9781e7a946f18c8c49da20d0925c_Out_0_Boolean = max(0, IN.FaceSign.x);
float3 _Lerp_e69be27d4f0d4e26a37c622c629ca56d_Out_3_Vector3;
Unity_Lerp_float3(_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Normal_1_Vector3, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Normal_1_Vector3, _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d_OutVector3_1_Vector3, _Lerp_e69be27d4f0d4e26a37c622c629ca56d_Out_3_Vector3);
float3 _Branch_3213119f26d54fc08b755c491d9dd6c4_Out_3_Vector3;
Unity_Branch_float3(_Property_868c674f556d42be9071980dc60f4aed_Out_0_Boolean, _Lerp_e69be27d4f0d4e26a37c622c629ca56d_Out_3_Vector3, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Normal_1_Vector3, _Branch_3213119f26d54fc08b755c491d9dd6c4_Out_3_Vector3);
float3 _Negate_c9035ab4407c48978c2cd379c7ed3db2_Out_1_Vector3;
Unity_Negate_float3(_Branch_3213119f26d54fc08b755c491d9dd6c4_Out_3_Vector3, _Negate_c9035ab4407c48978c2cd379c7ed3db2_Out_1_Vector3);
float3 _Branch_6a68e6f7151b40c684e82041ff10474e_Out_3_Vector3;
Unity_Branch_float3(_IsFrontFace_8b8f9781e7a946f18c8c49da20d0925c_Out_0_Boolean, _Branch_3213119f26d54fc08b755c491d9dd6c4_Out_3_Vector3, _Negate_c9035ab4407c48978c2cd379c7ed3db2_Out_1_Vector3, _Branch_6a68e6f7151b40c684e82041ff10474e_Out_3_Vector3);
float3 _Lerp_e6a3b112a78740919a21e1a67f8a9012_Out_3_Vector3;
Unity_Lerp_float3((_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Roughness_3_Float.xxx), (_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Roughness_3_Float.xxx), _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d_OutVector3_1_Vector3, _Lerp_e6a3b112a78740919a21e1a67f8a9012_Out_3_Vector3);
float3 _Branch_e3527157b2634e17853db32690e6cf9f_Out_3_Vector3;
Unity_Branch_float3(_Property_868c674f556d42be9071980dc60f4aed_Out_0_Boolean, _Lerp_e6a3b112a78740919a21e1a67f8a9012_Out_3_Vector3, (_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Metallic_2_Float.xxx), _Branch_e3527157b2634e17853db32690e6cf9f_Out_3_Vector3);
float3 _Lerp_0bf96dce18b642f4ab3b848bb1fe45d6_Out_3_Vector3;
Unity_Lerp_float3((_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Metallic_2_Float.xxx), (_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Metallic_2_Float.xxx), _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d_OutVector3_1_Vector3, _Lerp_0bf96dce18b642f4ab3b848bb1fe45d6_Out_3_Vector3);
float3 _Branch_bcdc5fe84435477ab329331520e86c7d_Out_3_Vector3;
Unity_Branch_float3(_Property_868c674f556d42be9071980dc60f4aed_Out_0_Boolean, _Lerp_0bf96dce18b642f4ab3b848bb1fe45d6_Out_3_Vector3, (_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Roughness_3_Float.xxx), _Branch_bcdc5fe84435477ab329331520e86c7d_Out_3_Vector3);
float3 _Lerp_1ffb4b0a9a70465db54818dd6df0be7c_Out_3_Vector3;
Unity_Lerp_float3((_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_AmbientOcclusion_4_Float.xxx), (_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_AmbientOcclusion_4_Float.xxx), _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d_OutVector3_1_Vector3, _Lerp_1ffb4b0a9a70465db54818dd6df0be7c_Out_3_Vector3);
float3 _Branch_c19f53b6fc08465e849902625a3db36e_Out_3_Vector3;
Unity_Branch_float3(_Property_868c674f556d42be9071980dc60f4aed_Out_0_Boolean, _Lerp_1ffb4b0a9a70465db54818dd6df0be7c_Out_3_Vector3, (_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_AmbientOcclusion_4_Float.xxx), _Branch_c19f53b6fc08465e849902625a3db36e_Out_3_Vector3);
UnityTexture2D _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_ColorMap_1, sampler_ColorMap_1, _ColorMap_1_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.tex, _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.samplerstate, _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4, _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_R_4_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.r;
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_G_5_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.g;
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_B_6_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.b;
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_A_7_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.a;
float _Property_7a43d5f1d3df46c2869bc70eebee1f91_Out_0_Float = _EmissiveIntensity_1;
surface.BaseColor = _Branch_72adc4bf3fd64427a9a9035afa6f856a_Out_3_Vector3;
surface.NormalTS = _Branch_6a68e6f7151b40c684e82041ff10474e_Out_3_Vector3;
surface.Emission = (_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Emissive_6_Vector4.xyz);
surface.Metallic = (_Branch_e3527157b2634e17853db32690e6cf9f_Out_3_Vector3).x;
surface.Smoothness = (_Branch_bcdc5fe84435477ab329331520e86c7d_Out_3_Vector3).x;
surface.Occlusion = (_Branch_c19f53b6fc08465e849902625a3db36e_Out_3_Vector3).x;
surface.Alpha = (_SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4).x;
surface.AlphaClipThreshold = _Property_7a43d5f1d3df46c2869bc70eebee1f91_Out_0_Float;
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

    // use bitangent on the fly like in hdrp
    // IMPORTANT! If we ever support Flip on double sided materials ensure bitangent and tangent are NOT flipped.
    float crossSign = (input.tangentWS.w > 0.0 ? 1.0 : -1.0)* GetOddNegativeScale();
    float3 bitang = crossSign * cross(input.normalWS.xyz, input.tangentWS.xyz);

    output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph
    output.ObjectSpaceNormal = normalize(mul(output.WorldSpaceNormal, (float3x3) UNITY_MATRIX_M));           // transposed multiplication by inverse matrix to handle normal scale
    output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);

    // to preserve mikktspace compliance we use same scale renormFactor as was used on the normal.
    // This is explained in section 2.2 in "surface gradient based bump mapping framework"
    output.WorldSpaceTangent = renormFactor * input.tangentWS.xyz;
    output.WorldSpaceBiTangent = renormFactor * bitang;

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
    BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
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
Cull [_BUILTIN_CullMode]
Blend [_BUILTIN_SrcBlend] [_BUILTIN_DstBlend]
ZTest [_BUILTIN_ZTest]
ZWrite [_BUILTIN_ZWrite]

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
#pragma shader_feature_local_fragment _ _BUILTIN_SURFACE_TYPE_TRANSPARENT
#pragma shader_feature_local_fragment _ _BUILTIN_ALPHAPREMULTIPLY_ON
#pragma shader_feature_local_fragment _ _BUILTIN_AlphaClip
#pragma shader_feature_local_fragment _ _BUILTIN_ALPHATEST_ON
#pragma shader_feature_local _EDGEBLENDING_NORMALBLENDING _EDGEBLENDING_VERTEXBLENDING _EDGEBLENDING_NOISEBLENDING

#if defined(_EDGEBLENDING_NORMALBLENDING)
#define KEYWORD_PERMUTATION_0
#elif defined(_EDGEBLENDING_VERTEXBLENDING)
#define KEYWORD_PERMUTATION_1
#else
#define KEYWORD_PERMUTATION_2
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
#define VARYINGS_NEED_CULLFACE
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
 float3 ObjectSpaceNormal;
 float3 WorldSpaceNormal;
 float3 TangentSpaceNormal;
 float3 WorldSpaceTangent;
 float3 WorldSpaceBiTangent;
 float3 ObjectSpacePosition;
 float4 uv0;
 float FaceSign;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
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

float4 _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D_TexelSize;
float4 _ColorMap_TexelSize;
float4 _ColorMap_1_TexelSize;
float4 _NormalMap_TexelSize;
float4 _ColorTint;
float _NormalIntensity;
float _UV_Size;
float4 _ORMMap_TexelSize;
float _RoughnessIntensity;
float _AOIntensity;
float _MetallicIntensity;
float4 _EmissiveColor;
float _EmissiveIntensity_1;
float4 _Emissive_Map_TexelSize;
float _EmissiveIntensity;
float _EnableCoverage;
float _CoverageSize;
float _CoverageNormalIntensity;
float _CoverageRoughnessIntensity;
float _CoverageMetallicIntensity;
float _NoiseTextureSize;
float4 _SlopeNoiseTexture_TexelSize;
float4 _CoverageColorMap_TexelSize;
float4 _CoverageORMMap_TexelSize;
float4 _CoverageNormalMap_TexelSize;
float _SloapOffset;
float _SloapContrast;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D);
SAMPLER(sampler_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D);
TEXTURE2D(_ColorMap);
SAMPLER(sampler_ColorMap);
TEXTURE2D(_ColorMap_1);
SAMPLER(sampler_ColorMap_1);
TEXTURE2D(_NormalMap);
SAMPLER(sampler_NormalMap);
TEXTURE2D(_ORMMap);
SAMPLER(sampler_ORMMap);
TEXTURE2D(_Emissive_Map);
SAMPLER(sampler_Emissive_Map);
TEXTURE2D(_SlopeNoiseTexture);
SAMPLER(sampler_SlopeNoiseTexture);
TEXTURE2D(_CoverageColorMap);
SAMPLER(sampler_CoverageColorMap);
TEXTURE2D(_CoverageORMMap);
SAMPLER(sampler_CoverageORMMap);
TEXTURE2D(_CoverageNormalMap);
SAMPLER(sampler_CoverageNormalMap);

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

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

void Unity_NormalStrength_float(float3 In, float Strength, out float3 Out)
{
    Out = float3(In.rg * Strength, lerp(1, In.b, saturate(Strength)));
}

void Unity_Lerp_float(float A, float B, float T, out float Out)
{
    Out = lerp(A, B, T);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
}

struct Bindings_PASGORMUnpacker_abcd41e335c426a4bb0f8de17b4b5f96_float
{
};

void SG_PASGORMUnpacker_abcd41e335c426a4bb0f8de17b4b5f96_float(float2 _UV, UnityTexture2D _ORM_Map, float _AO_Intensity, float _Roughness_Intensity, float _Metallic_Intensity, Bindings_PASGORMUnpacker_abcd41e335c426a4bb0f8de17b4b5f96_float IN, out float Ambient_Occlusion_1, out float Roughness_2, out float Metallic_3)
{
float _Float_2c47c335e2304faa927a789be883bb19_Out_0_Float = float(1);
UnityTexture2D _Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D = _ORM_Map;
float2 _Property_ad3893fa315b474ea60e7ee3e265957c_Out_0_Vector2 = _UV;
float4 _SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D.tex, _Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D.samplerstate, _Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D.GetTransformedUV(_Property_ad3893fa315b474ea60e7ee3e265957c_Out_0_Vector2) );
if (_Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4, _Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_719055d2c0184958bd1329f59130cce0_R_4_Float = _SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4.r;
float _SampleTexture2D_719055d2c0184958bd1329f59130cce0_G_5_Float = _SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4.g;
float _SampleTexture2D_719055d2c0184958bd1329f59130cce0_B_6_Float = _SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4.b;
float _SampleTexture2D_719055d2c0184958bd1329f59130cce0_A_7_Float = _SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4.a;
float _Property_90898696aae3458aa180c6f15d96aaee_Out_0_Float = _AO_Intensity;
float _Lerp_bf670034cefc4c6d9e02b57f5394811d_Out_3_Float;
Unity_Lerp_float(_Float_2c47c335e2304faa927a789be883bb19_Out_0_Float, _SampleTexture2D_719055d2c0184958bd1329f59130cce0_R_4_Float, _Property_90898696aae3458aa180c6f15d96aaee_Out_0_Float, _Lerp_bf670034cefc4c6d9e02b57f5394811d_Out_3_Float);
float _Property_e77944e4743744198e931506300c92ca_Out_0_Float = _Roughness_Intensity;
float _Multiply_43e873dc525645aab065eb6fc56ad77f_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_719055d2c0184958bd1329f59130cce0_G_5_Float, _Property_e77944e4743744198e931506300c92ca_Out_0_Float, _Multiply_43e873dc525645aab065eb6fc56ad77f_Out_2_Float);
float _OneMinus_3225b57b02ed4eaf9c118419fbefc479_Out_1_Float;
Unity_OneMinus_float(_Multiply_43e873dc525645aab065eb6fc56ad77f_Out_2_Float, _OneMinus_3225b57b02ed4eaf9c118419fbefc479_Out_1_Float);
float _Property_c0c103f0dfcd475fa428b0e043905944_Out_0_Float = _Metallic_Intensity;
float _Multiply_38b5ca9e78ba40d9b861ffc15889bd7f_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_719055d2c0184958bd1329f59130cce0_B_6_Float, _Property_c0c103f0dfcd475fa428b0e043905944_Out_0_Float, _Multiply_38b5ca9e78ba40d9b861ffc15889bd7f_Out_2_Float);
Ambient_Occlusion_1 = _Lerp_bf670034cefc4c6d9e02b57f5394811d_Out_3_Float;
Roughness_2 = _OneMinus_3225b57b02ed4eaf9c118419fbefc479_Out_1_Float;
Metallic_3 = _Multiply_38b5ca9e78ba40d9b861ffc15889bd7f_Out_2_Float;
}

struct Bindings_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float
{
half4 uv0;
};

void SG_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float(float _Texture_Size, UnityTexture2D _Color_Map, float4 _Color_Tint, UnityTexture2D _Normal_Map, float _Normal_Intensity, UnityTexture2D _ORM_Map, float _Roughness_Intensity, float _AO_Intensity, float _Metallic_Intensity, UnityTexture2D _Emissive_Map, float4 _Emissive_Color, float _Emissive_Intensity, Bindings_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float IN, out float4 Base_Color_5, out float3 Normal_1, out float4 Emissive_6, out float Ambient_Occlusion_4, out float Roughness_3, out float Metallic_2)
{
UnityTexture2D _Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D = _Color_Map;
float _Property_6e866f25d28140918dd5249261b96e76_Out_0_Float = _Texture_Size;
float2 _TilingAndOffset_49c249d74ea54ccf979206ad2c3c0c00_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, (_Property_6e866f25d28140918dd5249261b96e76_Out_0_Float.xx), float2 (0, 0), _TilingAndOffset_49c249d74ea54ccf979206ad2c3c0c00_Out_3_Vector2);
float _Property_94852b2b32f549138996f0dfb35b0486_Out_0_Float = _Texture_Size;
float2 _Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2;
Unity_Multiply_float2_float2(_TilingAndOffset_49c249d74ea54ccf979206ad2c3c0c00_Out_3_Vector2, (_Property_94852b2b32f549138996f0dfb35b0486_Out_0_Float.xx), _Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2);
float4 _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D.tex, _Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D.samplerstate, _Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D.GetTransformedUV(_Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2) );
if (_Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4, _Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_R_4_Float = _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4.r;
float _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_G_5_Float = _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4.g;
float _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_B_6_Float = _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4.b;
float _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_A_7_Float = _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4.a;
float4 _Property_c081d1852d284e4fa4bbb04ea3adf35e_Out_0_Vector4 = _Color_Tint;
float4 _Multiply_ff3b38102028498db7cc80acf5e42362_Out_2_Vector4;
Unity_Multiply_float4_float4(_SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4, _Property_c081d1852d284e4fa4bbb04ea3adf35e_Out_0_Vector4, _Multiply_ff3b38102028498db7cc80acf5e42362_Out_2_Vector4);
UnityTexture2D _Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D = _Normal_Map;
float4 _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D.tex, _Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D.samplerstate, _Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D.GetTransformedUV(_Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2) );
if (_Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4, _Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4);
float _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_R_4_Float = _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.r;
float _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_G_5_Float = _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.g;
float _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_B_6_Float = _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.b;
float _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_A_7_Float = _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.a;
float _Property_8b1f839dfbcd4481b201427fbc7310ad_Out_0_Float = _Normal_Intensity;
float3 _NormalStrength_a218ef03cc884bf0b0962d85b628afe7_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.xyz), _Property_8b1f839dfbcd4481b201427fbc7310ad_Out_0_Float, _NormalStrength_a218ef03cc884bf0b0962d85b628afe7_Out_2_Vector3);
UnityTexture2D _Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D = _Emissive_Map;
float4 _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D.tex, _Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D.samplerstate, _Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D.GetTransformedUV(_Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2) );
if (_Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4, _Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_R_4_Float = _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4.r;
float _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_G_5_Float = _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4.g;
float _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_B_6_Float = _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4.b;
float _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_A_7_Float = _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4.a;
float4 _Property_3a2f8dbe365d427e89744f8eb8dabd44_Out_0_Vector4 = _Emissive_Color;
float4 _Multiply_1efa7dc8f3994d8ea213351379d48448_Out_2_Vector4;
Unity_Multiply_float4_float4(_SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4, _Property_3a2f8dbe365d427e89744f8eb8dabd44_Out_0_Vector4, _Multiply_1efa7dc8f3994d8ea213351379d48448_Out_2_Vector4);
float _Property_09c3fe7e22be43caa45c324f52ea8137_Out_0_Float = _Emissive_Intensity;
float4 _Multiply_43a1c0123fb34a25a5d21581f08926f8_Out_2_Vector4;
Unity_Multiply_float4_float4(_Multiply_1efa7dc8f3994d8ea213351379d48448_Out_2_Vector4, (_Property_09c3fe7e22be43caa45c324f52ea8137_Out_0_Float.xxxx), _Multiply_43a1c0123fb34a25a5d21581f08926f8_Out_2_Vector4);
UnityTexture2D _Property_ecbd8a4e938a458cabefe2480cada231_Out_0_Texture2D = _ORM_Map;
float _Property_2ca0dc7be16d47bfb0faf210c4d70757_Out_0_Float = _AO_Intensity;
float _Property_fb245dde136f4b2486e260e720f83c83_Out_0_Float = _Roughness_Intensity;
float _Property_0fcde0f68f364243886b868db18a8d27_Out_0_Float = _Metallic_Intensity;
Bindings_PASGORMUnpacker_abcd41e335c426a4bb0f8de17b4b5f96_float _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94;
half _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_AmbientOcclusion_1_Float;
half _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Roughness_2_Float;
half _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Metallic_3_Float;
SG_PASGORMUnpacker_abcd41e335c426a4bb0f8de17b4b5f96_float(_Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2, _Property_ecbd8a4e938a458cabefe2480cada231_Out_0_Texture2D, _Property_2ca0dc7be16d47bfb0faf210c4d70757_Out_0_Float, _Property_fb245dde136f4b2486e260e720f83c83_Out_0_Float, _Property_0fcde0f68f364243886b868db18a8d27_Out_0_Float, _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94, _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_AmbientOcclusion_1_Float, _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Roughness_2_Float, _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Metallic_3_Float);
Base_Color_5 = _Multiply_ff3b38102028498db7cc80acf5e42362_Out_2_Vector4;
Normal_1 = _NormalStrength_a218ef03cc884bf0b0962d85b628afe7_Out_2_Vector3;
Emissive_6 = _Multiply_43a1c0123fb34a25a5d21581f08926f8_Out_2_Vector4;
Ambient_Occlusion_4 = _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_AmbientOcclusion_1_Float;
Roughness_3 = _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Roughness_2_Float;
Metallic_2 = _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Metallic_3_Float;
}

void Unity_DotProduct_float3(float3 A, float3 B, out float Out)
{
    Out = dot(A, B);
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Clamp_float(float In, float Min, float Max, out float Out)
{
    Out = clamp(In, Min, Max);
}

void Unity_Contrast_float(float3 In, float Contrast, out float3 Out)
{
    float midpoint = pow(0.5, 2.2);
    Out =  (In - midpoint) * Contrast + midpoint;
}

void Unity_Clamp_float3(float3 In, float3 Min, float3 Max, out float3 Out)
{
    Out = clamp(In, Min, Max);
}

struct Bindings_PASGVerticalBlend_e9f0b2d5820244846982772499370842_float
{
float3 ObjectSpaceNormal;
float3 WorldSpaceNormal;
float3 WorldSpaceTangent;
float3 WorldSpaceBiTangent;
float3 ObjectSpacePosition;
};

void SG_PASGVerticalBlend_e9f0b2d5820244846982772499370842_float(float _Sloap_Contrast, UnityTexture2D _Slope_Noise_Texture, float _Noise_Texture_Size, float3 _Explicit_Normal, float _Sloap_Offset, Bindings_PASGVerticalBlend_e9f0b2d5820244846982772499370842_float IN, out float3 OutVector3_1)
{
float3 _Property_5509c7fb30e445229f496a71c6e9c192_Out_0_Vector3 = _Explicit_Normal;
float3 _Transform_9c9f921cd0934012ac0d281c8e40a727_Out_1_Vector3;
{
float3x3 tangentTransform = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
_Transform_9c9f921cd0934012ac0d281c8e40a727_Out_1_Vector3 = TransformTangentToWorld(_Property_5509c7fb30e445229f496a71c6e9c192_Out_0_Vector3.xyz, tangentTransform, true);
}
float _DotProduct_4bc69611266041259c8da1b387474ec9_Out_2_Float;
Unity_DotProduct_float3(_Transform_9c9f921cd0934012ac0d281c8e40a727_Out_1_Vector3, float3(0, 1, 0), _DotProduct_4bc69611266041259c8da1b387474ec9_Out_2_Float);
float _Property_6a995c4e80824d6fa7d458ddcdc4eec3_Out_0_Float = _Sloap_Offset;
float _Add_a196acc146d1466fb309ec050e77feb2_Out_2_Float;
Unity_Add_float(_DotProduct_4bc69611266041259c8da1b387474ec9_Out_2_Float, _Property_6a995c4e80824d6fa7d458ddcdc4eec3_Out_0_Float, _Add_a196acc146d1466fb309ec050e77feb2_Out_2_Float);
float _Lerp_a1fed41c0847449ab72ae8fe4775270c_Out_3_Float;
Unity_Lerp_float(float(0), float(3), _Add_a196acc146d1466fb309ec050e77feb2_Out_2_Float, _Lerp_a1fed41c0847449ab72ae8fe4775270c_Out_3_Float);
float _Clamp_d4d6e762b7b1422d936b98d7f9429754_Out_3_Float;
Unity_Clamp_float(_Lerp_a1fed41c0847449ab72ae8fe4775270c_Out_3_Float, float(0), float(1), _Clamp_d4d6e762b7b1422d936b98d7f9429754_Out_3_Float);
float _Property_985b04ddd40847029dbfa829fbcaeed1_Out_0_Float = _Sloap_Contrast;
float3 _Contrast_2864897542064adcb4b61fcccadaf026_Out_2_Vector3;
Unity_Contrast_float((_Clamp_d4d6e762b7b1422d936b98d7f9429754_Out_3_Float.xxx), _Property_985b04ddd40847029dbfa829fbcaeed1_Out_0_Float, _Contrast_2864897542064adcb4b61fcccadaf026_Out_2_Vector3);
float3 _Clamp_b8f1f82d045f437a910a54b62bb2df81_Out_3_Vector3;
Unity_Clamp_float3(_Contrast_2864897542064adcb4b61fcccadaf026_Out_2_Vector3, float3(0, 0, 0), float3(1, 1, 1), _Clamp_b8f1f82d045f437a910a54b62bb2df81_Out_3_Vector3);
float3 _Transform_229ffe25002343a39c8caaa3d7f6df91_Out_1_Vector3;
_Transform_229ffe25002343a39c8caaa3d7f6df91_Out_1_Vector3 = TransformObjectToWorldDir(IN.ObjectSpacePosition.xyz, true);
float3 _Vector3_ec86905b34c54695bed5b7d1975cb6ce_Out_0_Vector3 = float3(float(0), float(2), float(0));
float _DotProduct_af3cb3caf6e4473c8161868b15c0255b_Out_2_Float;
Unity_DotProduct_float3(_Transform_229ffe25002343a39c8caaa3d7f6df91_Out_1_Vector3, _Vector3_ec86905b34c54695bed5b7d1975cb6ce_Out_0_Vector3, _DotProduct_af3cb3caf6e4473c8161868b15c0255b_Out_2_Float);
float _Add_47be9b48b4c94dd28e8c3db7f6ba1a05_Out_2_Float;
Unity_Add_float(_Property_6a995c4e80824d6fa7d458ddcdc4eec3_Out_0_Float, _DotProduct_af3cb3caf6e4473c8161868b15c0255b_Out_2_Float, _Add_47be9b48b4c94dd28e8c3db7f6ba1a05_Out_2_Float);
float _Multiply_35491cd334cb4e34a76b48de2bc5814a_Out_2_Float;
Unity_Multiply_float_float(_Add_47be9b48b4c94dd28e8c3db7f6ba1a05_Out_2_Float, 2, _Multiply_35491cd334cb4e34a76b48de2bc5814a_Out_2_Float);
float _Clamp_ccd0969e9da6420f94e7113654fd480e_Out_3_Float;
Unity_Clamp_float(_Multiply_35491cd334cb4e34a76b48de2bc5814a_Out_2_Float, float(0), float(1), _Clamp_ccd0969e9da6420f94e7113654fd480e_Out_3_Float);
float3 _Contrast_454ba7c8fb3a4d03951b073867fa5f14_Out_2_Vector3;
Unity_Contrast_float((_Clamp_ccd0969e9da6420f94e7113654fd480e_Out_3_Float.xxx), _Property_985b04ddd40847029dbfa829fbcaeed1_Out_0_Float, _Contrast_454ba7c8fb3a4d03951b073867fa5f14_Out_2_Vector3);
float3 _Clamp_1ba5cf0aee8245bd9dca155954c3df81_Out_3_Vector3;
Unity_Clamp_float3(_Contrast_454ba7c8fb3a4d03951b073867fa5f14_Out_2_Vector3, float3(0, 0, 0), float3(1, 1, 1), _Clamp_1ba5cf0aee8245bd9dca155954c3df81_Out_3_Vector3);
UnityTexture2D _Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D = _Slope_Noise_Texture;
float _Property_8e663ddd1476448d948a9de1bf1f83a9_Out_0_Float = _Noise_Texture_Size;
float3 Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_UV = IN.ObjectSpacePosition * _Property_8e663ddd1476448d948a9de1bf1f83a9_Out_0_Float;
float3 Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend = SafePositivePow_float(IN.ObjectSpaceNormal, min(float(1), floor(log2(Min_float())/log2(1/sqrt(3)))) );
Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend /= dot(Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend, 1.0);
float4 Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_X = SAMPLE_TEXTURE2D(_Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.tex, _Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.samplerstate, Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_UV.zy);
float4 Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Y = SAMPLE_TEXTURE2D(_Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.tex, _Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.samplerstate, Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_UV.xz);
float4 Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Z = SAMPLE_TEXTURE2D(_Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.tex, _Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.samplerstate, Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_UV.xy);
float4 _Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Out_0_Vector4 = Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_X * Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend.x + Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Y * Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend.y + Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Z * Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend.z;
float _Swizzle_90fb1902a2bd4070b4f9e27ac287ca97_Out_1_Float = (_Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Out_0_Vector4.xy).y;
float _Clamp_f32c97c6ccbf44649bd757503ccdbe71_Out_3_Float;
Unity_Clamp_float(_Swizzle_90fb1902a2bd4070b4f9e27ac287ca97_Out_1_Float, float(0), float(1), _Clamp_f32c97c6ccbf44649bd757503ccdbe71_Out_3_Float);
float _Add_92d15430b3c042719d65f1252bc35d7a_Out_2_Float;
Unity_Add_float(_Multiply_35491cd334cb4e34a76b48de2bc5814a_Out_2_Float, _Clamp_f32c97c6ccbf44649bd757503ccdbe71_Out_3_Float, _Add_92d15430b3c042719d65f1252bc35d7a_Out_2_Float);
float3 _Contrast_c2fe106c51f0498e987cf31090ec563d_Out_2_Vector3;
Unity_Contrast_float((_Add_92d15430b3c042719d65f1252bc35d7a_Out_2_Float.xxx), _Property_985b04ddd40847029dbfa829fbcaeed1_Out_0_Float, _Contrast_c2fe106c51f0498e987cf31090ec563d_Out_2_Vector3);
float3 _Clamp_05438405442c47cb98e571d128277bbe_Out_3_Vector3;
Unity_Clamp_float3(_Contrast_c2fe106c51f0498e987cf31090ec563d_Out_2_Vector3, float3(0, 0, 0), float3(1, 1, 1), _Clamp_05438405442c47cb98e571d128277bbe_Out_3_Vector3);
#if defined(_EDGEBLENDING_NORMALBLENDING)
float3 _EdgeBlending_cdbe33a15cf6452b8881ecea22adcb84_Out_0_Vector3 = _Clamp_b8f1f82d045f437a910a54b62bb2df81_Out_3_Vector3;
#elif defined(_EDGEBLENDING_VERTEXBLENDING)
float3 _EdgeBlending_cdbe33a15cf6452b8881ecea22adcb84_Out_0_Vector3 = _Clamp_1ba5cf0aee8245bd9dca155954c3df81_Out_3_Vector3;
#else
float3 _EdgeBlending_cdbe33a15cf6452b8881ecea22adcb84_Out_0_Vector3 = _Clamp_05438405442c47cb98e571d128277bbe_Out_3_Vector3;
#endif
OutVector3_1 = _EdgeBlending_cdbe33a15cf6452b8881ecea22adcb84_Out_0_Vector3;
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Branch_float3(float Predicate, float3 True, float3 False, out float3 Out)
{
    Out = Predicate ? True : False;
}

void Unity_Negate_float3(float3 In, out float3 Out)
{
    Out = -1 * In;
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
description.Position = IN.ObjectSpacePosition;
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
float Alpha;
float AlphaClipThreshold;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
float _Property_868c674f556d42be9071980dc60f4aed_Out_0_Boolean = _EnableCoverage;
float _Property_a0b21575a96c42a2b05aa3518645b2f4_Out_0_Float = _UV_Size;
UnityTexture2D _Property_4d248f4a9de94d7bb9a5c3c7c4c10a68_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_ColorMap, sampler_ColorMap, _ColorMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _Property_061dd20995c54e4b8c0357aa709c6e56_Out_0_Vector4 = _ColorTint;
UnityTexture2D _Property_cc904da6b4714cd6a9559b3bcf6ba7a6_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_NormalMap, sampler_NormalMap, _NormalMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_49f412000c0b4f18863fb7afae8b43fc_Out_0_Float = _NormalIntensity;
UnityTexture2D _Property_3fcfe917c75d4a46bfe50da31a24c74f_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_ORMMap, sampler_ORMMap, _ORMMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_bd35b765c62349f285447de81728f0c8_Out_0_Float = _RoughnessIntensity;
float _Property_f125a72836c24b0fa5ef72823c8e6452_Out_0_Float = _AOIntensity;
float _Property_0251a196075947af9d3ad28d6875a5ef_Out_0_Float = _MetallicIntensity;
UnityTexture2D _Property_47ecc092fbd144218fe6440250e367b3_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Emissive_Map, sampler_Emissive_Map, _Emissive_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _Property_c065f0d4df8c40528116ee8e516b2221_Out_0_Vector4 = _EmissiveColor;
float _Property_0038e41f028740e4abc1958ca58302a1_Out_0_Float = _EmissiveIntensity;
Bindings_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe;
_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe.uv0 = IN.uv0;
float4 _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_BaseColor_5_Vector4;
float3 _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Normal_1_Vector3;
float4 _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Emissive_6_Vector4;
float _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_AmbientOcclusion_4_Float;
float _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Roughness_3_Float;
float _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Metallic_2_Float;
SG_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float(_Property_a0b21575a96c42a2b05aa3518645b2f4_Out_0_Float, _Property_4d248f4a9de94d7bb9a5c3c7c4c10a68_Out_0_Texture2D, _Property_061dd20995c54e4b8c0357aa709c6e56_Out_0_Vector4, _Property_cc904da6b4714cd6a9559b3bcf6ba7a6_Out_0_Texture2D, _Property_49f412000c0b4f18863fb7afae8b43fc_Out_0_Float, _Property_3fcfe917c75d4a46bfe50da31a24c74f_Out_0_Texture2D, _Property_bd35b765c62349f285447de81728f0c8_Out_0_Float, _Property_f125a72836c24b0fa5ef72823c8e6452_Out_0_Float, _Property_0251a196075947af9d3ad28d6875a5ef_Out_0_Float, _Property_47ecc092fbd144218fe6440250e367b3_Out_0_Texture2D, _Property_c065f0d4df8c40528116ee8e516b2221_Out_0_Vector4, _Property_0038e41f028740e4abc1958ca58302a1_Out_0_Float, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_BaseColor_5_Vector4, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Normal_1_Vector3, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Emissive_6_Vector4, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_AmbientOcclusion_4_Float, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Roughness_3_Float, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Metallic_2_Float);
float _Property_623a0c9d694c4c33b6256bde952c53e5_Out_0_Float = _CoverageSize;
UnityTexture2D _Property_58d380b2034145c8b7d2eda637dbc296_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_CoverageColorMap, sampler_CoverageColorMap, _CoverageColorMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
UnityTexture2D _Property_f324dd6628ea45b1a630d750fa3d6b66_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_CoverageNormalMap, sampler_CoverageNormalMap, _CoverageNormalMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_b00b34515282450295cc4f4eb7b4a0ce_Out_0_Float = _CoverageNormalIntensity;
UnityTexture2D _Property_24ae6469bfeb4dada6fb7c7d39985cb3_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_CoverageORMMap, sampler_CoverageORMMap, _CoverageORMMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_03c29a27092f4bbb8a4fcc3b6e7b2909_Out_0_Float = _CoverageRoughnessIntensity;
float _Property_07cad64e572e4602a346b7dd90c3eb21_Out_0_Float = _CoverageMetallicIntensity;
Bindings_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c;
_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c.uv0 = IN.uv0;
float4 _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_BaseColor_5_Vector4;
float3 _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Normal_1_Vector3;
float4 _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Emissive_6_Vector4;
float _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_AmbientOcclusion_4_Float;
float _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Roughness_3_Float;
float _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Metallic_2_Float;
SG_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float(_Property_623a0c9d694c4c33b6256bde952c53e5_Out_0_Float, _Property_58d380b2034145c8b7d2eda637dbc296_Out_0_Texture2D, float4 (1, 1, 1, 1), _Property_f324dd6628ea45b1a630d750fa3d6b66_Out_0_Texture2D, _Property_b00b34515282450295cc4f4eb7b4a0ce_Out_0_Float, _Property_24ae6469bfeb4dada6fb7c7d39985cb3_Out_0_Texture2D, _Property_03c29a27092f4bbb8a4fcc3b6e7b2909_Out_0_Float, float(0), _Property_07cad64e572e4602a346b7dd90c3eb21_Out_0_Float, UnityBuildTexture2DStructNoScale(_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D), float4 (0, 0, 0, 0), float(0), _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_BaseColor_5_Vector4, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Normal_1_Vector3, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Emissive_6_Vector4, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_AmbientOcclusion_4_Float, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Roughness_3_Float, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Metallic_2_Float);
float _Property_0785fc58d71d4d63abb8c1102aa1a1b7_Out_0_Float = _SloapContrast;
UnityTexture2D _Property_12921d1841154c59b11aefccb2cbca31_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_SlopeNoiseTexture, sampler_SlopeNoiseTexture, _SlopeNoiseTexture_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_10dd46128137476fb3fd49de6154e94c_Out_0_Float = _NoiseTextureSize;
float _Property_a654ef8b1e2a4529ab82f92b3ae202ec_Out_0_Float = _SloapOffset;
Bindings_PASGVerticalBlend_e9f0b2d5820244846982772499370842_float _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d;
_PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d.ObjectSpaceNormal = IN.ObjectSpaceNormal;
_PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d.WorldSpaceNormal = IN.WorldSpaceNormal;
_PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d.WorldSpaceTangent = IN.WorldSpaceTangent;
_PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d.WorldSpaceBiTangent = IN.WorldSpaceBiTangent;
_PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d.ObjectSpacePosition = IN.ObjectSpacePosition;
float3 _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d_OutVector3_1_Vector3;
SG_PASGVerticalBlend_e9f0b2d5820244846982772499370842_float(_Property_0785fc58d71d4d63abb8c1102aa1a1b7_Out_0_Float, _Property_12921d1841154c59b11aefccb2cbca31_Out_0_Texture2D, _Property_10dd46128137476fb3fd49de6154e94c_Out_0_Float, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Normal_1_Vector3, _Property_a654ef8b1e2a4529ab82f92b3ae202ec_Out_0_Float, _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d, _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d_OutVector3_1_Vector3);
float3 _Lerp_8ef5a2e39e934fb09e1bc6858ba97959_Out_3_Vector3;
Unity_Lerp_float3((_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_BaseColor_5_Vector4.xyz), (_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_BaseColor_5_Vector4.xyz), _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d_OutVector3_1_Vector3, _Lerp_8ef5a2e39e934fb09e1bc6858ba97959_Out_3_Vector3);
float3 _Branch_72adc4bf3fd64427a9a9035afa6f856a_Out_3_Vector3;
Unity_Branch_float3(_Property_868c674f556d42be9071980dc60f4aed_Out_0_Boolean, _Lerp_8ef5a2e39e934fb09e1bc6858ba97959_Out_3_Vector3, (_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_BaseColor_5_Vector4.xyz), _Branch_72adc4bf3fd64427a9a9035afa6f856a_Out_3_Vector3);
float _IsFrontFace_8b8f9781e7a946f18c8c49da20d0925c_Out_0_Boolean = max(0, IN.FaceSign.x);
float3 _Lerp_e69be27d4f0d4e26a37c622c629ca56d_Out_3_Vector3;
Unity_Lerp_float3(_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Normal_1_Vector3, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Normal_1_Vector3, _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d_OutVector3_1_Vector3, _Lerp_e69be27d4f0d4e26a37c622c629ca56d_Out_3_Vector3);
float3 _Branch_3213119f26d54fc08b755c491d9dd6c4_Out_3_Vector3;
Unity_Branch_float3(_Property_868c674f556d42be9071980dc60f4aed_Out_0_Boolean, _Lerp_e69be27d4f0d4e26a37c622c629ca56d_Out_3_Vector3, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Normal_1_Vector3, _Branch_3213119f26d54fc08b755c491d9dd6c4_Out_3_Vector3);
float3 _Negate_c9035ab4407c48978c2cd379c7ed3db2_Out_1_Vector3;
Unity_Negate_float3(_Branch_3213119f26d54fc08b755c491d9dd6c4_Out_3_Vector3, _Negate_c9035ab4407c48978c2cd379c7ed3db2_Out_1_Vector3);
float3 _Branch_6a68e6f7151b40c684e82041ff10474e_Out_3_Vector3;
Unity_Branch_float3(_IsFrontFace_8b8f9781e7a946f18c8c49da20d0925c_Out_0_Boolean, _Branch_3213119f26d54fc08b755c491d9dd6c4_Out_3_Vector3, _Negate_c9035ab4407c48978c2cd379c7ed3db2_Out_1_Vector3, _Branch_6a68e6f7151b40c684e82041ff10474e_Out_3_Vector3);
float3 _Lerp_e6a3b112a78740919a21e1a67f8a9012_Out_3_Vector3;
Unity_Lerp_float3((_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Roughness_3_Float.xxx), (_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Roughness_3_Float.xxx), _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d_OutVector3_1_Vector3, _Lerp_e6a3b112a78740919a21e1a67f8a9012_Out_3_Vector3);
float3 _Branch_e3527157b2634e17853db32690e6cf9f_Out_3_Vector3;
Unity_Branch_float3(_Property_868c674f556d42be9071980dc60f4aed_Out_0_Boolean, _Lerp_e6a3b112a78740919a21e1a67f8a9012_Out_3_Vector3, (_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Metallic_2_Float.xxx), _Branch_e3527157b2634e17853db32690e6cf9f_Out_3_Vector3);
float3 _Lerp_0bf96dce18b642f4ab3b848bb1fe45d6_Out_3_Vector3;
Unity_Lerp_float3((_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Metallic_2_Float.xxx), (_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Metallic_2_Float.xxx), _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d_OutVector3_1_Vector3, _Lerp_0bf96dce18b642f4ab3b848bb1fe45d6_Out_3_Vector3);
float3 _Branch_bcdc5fe84435477ab329331520e86c7d_Out_3_Vector3;
Unity_Branch_float3(_Property_868c674f556d42be9071980dc60f4aed_Out_0_Boolean, _Lerp_0bf96dce18b642f4ab3b848bb1fe45d6_Out_3_Vector3, (_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Roughness_3_Float.xxx), _Branch_bcdc5fe84435477ab329331520e86c7d_Out_3_Vector3);
float3 _Lerp_1ffb4b0a9a70465db54818dd6df0be7c_Out_3_Vector3;
Unity_Lerp_float3((_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_AmbientOcclusion_4_Float.xxx), (_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_AmbientOcclusion_4_Float.xxx), _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d_OutVector3_1_Vector3, _Lerp_1ffb4b0a9a70465db54818dd6df0be7c_Out_3_Vector3);
float3 _Branch_c19f53b6fc08465e849902625a3db36e_Out_3_Vector3;
Unity_Branch_float3(_Property_868c674f556d42be9071980dc60f4aed_Out_0_Boolean, _Lerp_1ffb4b0a9a70465db54818dd6df0be7c_Out_3_Vector3, (_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_AmbientOcclusion_4_Float.xxx), _Branch_c19f53b6fc08465e849902625a3db36e_Out_3_Vector3);
UnityTexture2D _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_ColorMap_1, sampler_ColorMap_1, _ColorMap_1_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.tex, _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.samplerstate, _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4, _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_R_4_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.r;
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_G_5_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.g;
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_B_6_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.b;
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_A_7_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.a;
float _Property_7a43d5f1d3df46c2869bc70eebee1f91_Out_0_Float = _EmissiveIntensity_1;
surface.BaseColor = _Branch_72adc4bf3fd64427a9a9035afa6f856a_Out_3_Vector3;
surface.NormalTS = _Branch_6a68e6f7151b40c684e82041ff10474e_Out_3_Vector3;
surface.Emission = (_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Emissive_6_Vector4.xyz);
surface.Metallic = (_Branch_e3527157b2634e17853db32690e6cf9f_Out_3_Vector3).x;
surface.Smoothness = (_Branch_bcdc5fe84435477ab329331520e86c7d_Out_3_Vector3).x;
surface.Occlusion = (_Branch_c19f53b6fc08465e849902625a3db36e_Out_3_Vector3).x;
surface.Alpha = (_SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4).x;
surface.AlphaClipThreshold = _Property_7a43d5f1d3df46c2869bc70eebee1f91_Out_0_Float;
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

    // use bitangent on the fly like in hdrp
    // IMPORTANT! If we ever support Flip on double sided materials ensure bitangent and tangent are NOT flipped.
    float crossSign = (input.tangentWS.w > 0.0 ? 1.0 : -1.0)* GetOddNegativeScale();
    float3 bitang = crossSign * cross(input.normalWS.xyz, input.tangentWS.xyz);

    output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph
    output.ObjectSpaceNormal = normalize(mul(output.WorldSpaceNormal, (float3x3) UNITY_MATRIX_M));           // transposed multiplication by inverse matrix to handle normal scale
    output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);

    // to preserve mikktspace compliance we use same scale renormFactor as was used on the normal.
    // This is explained in section 2.2 in "surface gradient based bump mapping framework"
    output.WorldSpaceTangent = renormFactor * input.tangentWS.xyz;
    output.WorldSpaceBiTangent = renormFactor * bitang;

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
    BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
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
Cull [_BUILTIN_CullMode]
Blend [_BUILTIN_SrcBlend] [_BUILTIN_DstBlend]
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
#pragma shader_feature_local_fragment _ _BUILTIN_SURFACE_TYPE_TRANSPARENT
#pragma shader_feature_local_fragment _ _BUILTIN_AlphaClip
#pragma shader_feature_local_fragment _ _BUILTIN_ALPHATEST_ON
#pragma shader_feature_local _EDGEBLENDING_NORMALBLENDING _EDGEBLENDING_VERTEXBLENDING _EDGEBLENDING_NOISEBLENDING

#if defined(_EDGEBLENDING_NORMALBLENDING)
#define KEYWORD_PERMUTATION_0
#elif defined(_EDGEBLENDING_VERTEXBLENDING)
#define KEYWORD_PERMUTATION_1
#else
#define KEYWORD_PERMUTATION_2
#endif


// Defines
#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define VARYINGS_NEED_TEXCOORD0
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
 float4 uv0 : TEXCOORD0;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
 float4 positionCS : SV_POSITION;
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
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
 float4 texCoord0 : INTERP0;
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

float4 _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D_TexelSize;
float4 _ColorMap_TexelSize;
float4 _ColorMap_1_TexelSize;
float4 _NormalMap_TexelSize;
float4 _ColorTint;
float _NormalIntensity;
float _UV_Size;
float4 _ORMMap_TexelSize;
float _RoughnessIntensity;
float _AOIntensity;
float _MetallicIntensity;
float4 _EmissiveColor;
float _EmissiveIntensity_1;
float4 _Emissive_Map_TexelSize;
float _EmissiveIntensity;
float _EnableCoverage;
float _CoverageSize;
float _CoverageNormalIntensity;
float _CoverageRoughnessIntensity;
float _CoverageMetallicIntensity;
float _NoiseTextureSize;
float4 _SlopeNoiseTexture_TexelSize;
float4 _CoverageColorMap_TexelSize;
float4 _CoverageORMMap_TexelSize;
float4 _CoverageNormalMap_TexelSize;
float _SloapOffset;
float _SloapContrast;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D);
SAMPLER(sampler_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D);
TEXTURE2D(_ColorMap);
SAMPLER(sampler_ColorMap);
TEXTURE2D(_ColorMap_1);
SAMPLER(sampler_ColorMap_1);
TEXTURE2D(_NormalMap);
SAMPLER(sampler_NormalMap);
TEXTURE2D(_ORMMap);
SAMPLER(sampler_ORMMap);
TEXTURE2D(_Emissive_Map);
SAMPLER(sampler_Emissive_Map);
TEXTURE2D(_SlopeNoiseTexture);
SAMPLER(sampler_SlopeNoiseTexture);
TEXTURE2D(_CoverageColorMap);
SAMPLER(sampler_CoverageColorMap);
TEXTURE2D(_CoverageORMMap);
SAMPLER(sampler_CoverageORMMap);
TEXTURE2D(_CoverageNormalMap);
SAMPLER(sampler_CoverageNormalMap);

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
// GraphFunctions: <None>

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
description.Position = IN.ObjectSpacePosition;
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
float Alpha;
float AlphaClipThreshold;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
UnityTexture2D _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_ColorMap_1, sampler_ColorMap_1, _ColorMap_1_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.tex, _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.samplerstate, _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4, _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_R_4_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.r;
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_G_5_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.g;
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_B_6_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.b;
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_A_7_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.a;
float _Property_7a43d5f1d3df46c2869bc70eebee1f91_Out_0_Float = _EmissiveIntensity_1;
surface.Alpha = (_SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4).x;
surface.AlphaClipThreshold = _Property_7a43d5f1d3df46c2869bc70eebee1f91_Out_0_Float;
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
Cull [_BUILTIN_CullMode]
Blend [_BUILTIN_SrcBlend] [_BUILTIN_DstBlend]
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
#pragma shader_feature_local_fragment _ _BUILTIN_AlphaClip
#pragma shader_feature_local_fragment _ _BUILTIN_ALPHATEST_ON
#pragma shader_feature_local _EDGEBLENDING_NORMALBLENDING _EDGEBLENDING_VERTEXBLENDING _EDGEBLENDING_NOISEBLENDING

#if defined(_EDGEBLENDING_NORMALBLENDING)
#define KEYWORD_PERMUTATION_0
#elif defined(_EDGEBLENDING_VERTEXBLENDING)
#define KEYWORD_PERMUTATION_1
#else
#define KEYWORD_PERMUTATION_2
#endif


// Defines
#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define VARYINGS_NEED_TEXCOORD0
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
 float4 uv0 : TEXCOORD0;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
 float4 positionCS : SV_POSITION;
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
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
 float4 texCoord0 : INTERP0;
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

float4 _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D_TexelSize;
float4 _ColorMap_TexelSize;
float4 _ColorMap_1_TexelSize;
float4 _NormalMap_TexelSize;
float4 _ColorTint;
float _NormalIntensity;
float _UV_Size;
float4 _ORMMap_TexelSize;
float _RoughnessIntensity;
float _AOIntensity;
float _MetallicIntensity;
float4 _EmissiveColor;
float _EmissiveIntensity_1;
float4 _Emissive_Map_TexelSize;
float _EmissiveIntensity;
float _EnableCoverage;
float _CoverageSize;
float _CoverageNormalIntensity;
float _CoverageRoughnessIntensity;
float _CoverageMetallicIntensity;
float _NoiseTextureSize;
float4 _SlopeNoiseTexture_TexelSize;
float4 _CoverageColorMap_TexelSize;
float4 _CoverageORMMap_TexelSize;
float4 _CoverageNormalMap_TexelSize;
float _SloapOffset;
float _SloapContrast;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D);
SAMPLER(sampler_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D);
TEXTURE2D(_ColorMap);
SAMPLER(sampler_ColorMap);
TEXTURE2D(_ColorMap_1);
SAMPLER(sampler_ColorMap_1);
TEXTURE2D(_NormalMap);
SAMPLER(sampler_NormalMap);
TEXTURE2D(_ORMMap);
SAMPLER(sampler_ORMMap);
TEXTURE2D(_Emissive_Map);
SAMPLER(sampler_Emissive_Map);
TEXTURE2D(_SlopeNoiseTexture);
SAMPLER(sampler_SlopeNoiseTexture);
TEXTURE2D(_CoverageColorMap);
SAMPLER(sampler_CoverageColorMap);
TEXTURE2D(_CoverageORMMap);
SAMPLER(sampler_CoverageORMMap);
TEXTURE2D(_CoverageNormalMap);
SAMPLER(sampler_CoverageNormalMap);

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
// GraphFunctions: <None>

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
description.Position = IN.ObjectSpacePosition;
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
float Alpha;
float AlphaClipThreshold;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
UnityTexture2D _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_ColorMap_1, sampler_ColorMap_1, _ColorMap_1_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.tex, _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.samplerstate, _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4, _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_R_4_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.r;
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_G_5_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.g;
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_B_6_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.b;
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_A_7_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.a;
float _Property_7a43d5f1d3df46c2869bc70eebee1f91_Out_0_Float = _EmissiveIntensity_1;
surface.Alpha = (_SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4).x;
surface.AlphaClipThreshold = _Property_7a43d5f1d3df46c2869bc70eebee1f91_Out_0_Float;
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
#pragma shader_feature_local_fragment _ _BUILTIN_SURFACE_TYPE_TRANSPARENT
#pragma shader_feature_local_fragment _ _BUILTIN_AlphaClip
#pragma shader_feature_local_fragment _ _BUILTIN_ALPHATEST_ON
#pragma shader_feature_local _EDGEBLENDING_NORMALBLENDING _EDGEBLENDING_VERTEXBLENDING _EDGEBLENDING_NOISEBLENDING

#if defined(_EDGEBLENDING_NORMALBLENDING)
#define KEYWORD_PERMUTATION_0
#elif defined(_EDGEBLENDING_VERTEXBLENDING)
#define KEYWORD_PERMUTATION_1
#else
#define KEYWORD_PERMUTATION_2
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
#define VARYINGS_NEED_TANGENT_WS
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
 float3 ObjectSpaceNormal;
 float3 WorldSpaceNormal;
 float3 WorldSpaceTangent;
 float3 WorldSpaceBiTangent;
 float3 ObjectSpacePosition;
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
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

float4 _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D_TexelSize;
float4 _ColorMap_TexelSize;
float4 _ColorMap_1_TexelSize;
float4 _NormalMap_TexelSize;
float4 _ColorTint;
float _NormalIntensity;
float _UV_Size;
float4 _ORMMap_TexelSize;
float _RoughnessIntensity;
float _AOIntensity;
float _MetallicIntensity;
float4 _EmissiveColor;
float _EmissiveIntensity_1;
float4 _Emissive_Map_TexelSize;
float _EmissiveIntensity;
float _EnableCoverage;
float _CoverageSize;
float _CoverageNormalIntensity;
float _CoverageRoughnessIntensity;
float _CoverageMetallicIntensity;
float _NoiseTextureSize;
float4 _SlopeNoiseTexture_TexelSize;
float4 _CoverageColorMap_TexelSize;
float4 _CoverageORMMap_TexelSize;
float4 _CoverageNormalMap_TexelSize;
float _SloapOffset;
float _SloapContrast;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D);
SAMPLER(sampler_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D);
TEXTURE2D(_ColorMap);
SAMPLER(sampler_ColorMap);
TEXTURE2D(_ColorMap_1);
SAMPLER(sampler_ColorMap_1);
TEXTURE2D(_NormalMap);
SAMPLER(sampler_NormalMap);
TEXTURE2D(_ORMMap);
SAMPLER(sampler_ORMMap);
TEXTURE2D(_Emissive_Map);
SAMPLER(sampler_Emissive_Map);
TEXTURE2D(_SlopeNoiseTexture);
SAMPLER(sampler_SlopeNoiseTexture);
TEXTURE2D(_CoverageColorMap);
SAMPLER(sampler_CoverageColorMap);
TEXTURE2D(_CoverageORMMap);
SAMPLER(sampler_CoverageORMMap);
TEXTURE2D(_CoverageNormalMap);
SAMPLER(sampler_CoverageNormalMap);

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

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

void Unity_NormalStrength_float(float3 In, float Strength, out float3 Out)
{
    Out = float3(In.rg * Strength, lerp(1, In.b, saturate(Strength)));
}

void Unity_Lerp_float(float A, float B, float T, out float Out)
{
    Out = lerp(A, B, T);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
}

struct Bindings_PASGORMUnpacker_abcd41e335c426a4bb0f8de17b4b5f96_float
{
};

void SG_PASGORMUnpacker_abcd41e335c426a4bb0f8de17b4b5f96_float(float2 _UV, UnityTexture2D _ORM_Map, float _AO_Intensity, float _Roughness_Intensity, float _Metallic_Intensity, Bindings_PASGORMUnpacker_abcd41e335c426a4bb0f8de17b4b5f96_float IN, out float Ambient_Occlusion_1, out float Roughness_2, out float Metallic_3)
{
float _Float_2c47c335e2304faa927a789be883bb19_Out_0_Float = float(1);
UnityTexture2D _Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D = _ORM_Map;
float2 _Property_ad3893fa315b474ea60e7ee3e265957c_Out_0_Vector2 = _UV;
float4 _SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D.tex, _Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D.samplerstate, _Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D.GetTransformedUV(_Property_ad3893fa315b474ea60e7ee3e265957c_Out_0_Vector2) );
if (_Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4, _Property_a6b417ac8ded4d7cbc5c9c4f2204695c_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_719055d2c0184958bd1329f59130cce0_R_4_Float = _SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4.r;
float _SampleTexture2D_719055d2c0184958bd1329f59130cce0_G_5_Float = _SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4.g;
float _SampleTexture2D_719055d2c0184958bd1329f59130cce0_B_6_Float = _SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4.b;
float _SampleTexture2D_719055d2c0184958bd1329f59130cce0_A_7_Float = _SampleTexture2D_719055d2c0184958bd1329f59130cce0_RGBA_0_Vector4.a;
float _Property_90898696aae3458aa180c6f15d96aaee_Out_0_Float = _AO_Intensity;
float _Lerp_bf670034cefc4c6d9e02b57f5394811d_Out_3_Float;
Unity_Lerp_float(_Float_2c47c335e2304faa927a789be883bb19_Out_0_Float, _SampleTexture2D_719055d2c0184958bd1329f59130cce0_R_4_Float, _Property_90898696aae3458aa180c6f15d96aaee_Out_0_Float, _Lerp_bf670034cefc4c6d9e02b57f5394811d_Out_3_Float);
float _Property_e77944e4743744198e931506300c92ca_Out_0_Float = _Roughness_Intensity;
float _Multiply_43e873dc525645aab065eb6fc56ad77f_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_719055d2c0184958bd1329f59130cce0_G_5_Float, _Property_e77944e4743744198e931506300c92ca_Out_0_Float, _Multiply_43e873dc525645aab065eb6fc56ad77f_Out_2_Float);
float _OneMinus_3225b57b02ed4eaf9c118419fbefc479_Out_1_Float;
Unity_OneMinus_float(_Multiply_43e873dc525645aab065eb6fc56ad77f_Out_2_Float, _OneMinus_3225b57b02ed4eaf9c118419fbefc479_Out_1_Float);
float _Property_c0c103f0dfcd475fa428b0e043905944_Out_0_Float = _Metallic_Intensity;
float _Multiply_38b5ca9e78ba40d9b861ffc15889bd7f_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_719055d2c0184958bd1329f59130cce0_B_6_Float, _Property_c0c103f0dfcd475fa428b0e043905944_Out_0_Float, _Multiply_38b5ca9e78ba40d9b861ffc15889bd7f_Out_2_Float);
Ambient_Occlusion_1 = _Lerp_bf670034cefc4c6d9e02b57f5394811d_Out_3_Float;
Roughness_2 = _OneMinus_3225b57b02ed4eaf9c118419fbefc479_Out_1_Float;
Metallic_3 = _Multiply_38b5ca9e78ba40d9b861ffc15889bd7f_Out_2_Float;
}

struct Bindings_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float
{
half4 uv0;
};

void SG_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float(float _Texture_Size, UnityTexture2D _Color_Map, float4 _Color_Tint, UnityTexture2D _Normal_Map, float _Normal_Intensity, UnityTexture2D _ORM_Map, float _Roughness_Intensity, float _AO_Intensity, float _Metallic_Intensity, UnityTexture2D _Emissive_Map, float4 _Emissive_Color, float _Emissive_Intensity, Bindings_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float IN, out float4 Base_Color_5, out float3 Normal_1, out float4 Emissive_6, out float Ambient_Occlusion_4, out float Roughness_3, out float Metallic_2)
{
UnityTexture2D _Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D = _Color_Map;
float _Property_6e866f25d28140918dd5249261b96e76_Out_0_Float = _Texture_Size;
float2 _TilingAndOffset_49c249d74ea54ccf979206ad2c3c0c00_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, (_Property_6e866f25d28140918dd5249261b96e76_Out_0_Float.xx), float2 (0, 0), _TilingAndOffset_49c249d74ea54ccf979206ad2c3c0c00_Out_3_Vector2);
float _Property_94852b2b32f549138996f0dfb35b0486_Out_0_Float = _Texture_Size;
float2 _Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2;
Unity_Multiply_float2_float2(_TilingAndOffset_49c249d74ea54ccf979206ad2c3c0c00_Out_3_Vector2, (_Property_94852b2b32f549138996f0dfb35b0486_Out_0_Float.xx), _Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2);
float4 _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D.tex, _Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D.samplerstate, _Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D.GetTransformedUV(_Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2) );
if (_Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4, _Property_96d8ade8b2b345328f5322d241a90fe0_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_R_4_Float = _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4.r;
float _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_G_5_Float = _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4.g;
float _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_B_6_Float = _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4.b;
float _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_A_7_Float = _SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4.a;
float4 _Property_c081d1852d284e4fa4bbb04ea3adf35e_Out_0_Vector4 = _Color_Tint;
float4 _Multiply_ff3b38102028498db7cc80acf5e42362_Out_2_Vector4;
Unity_Multiply_float4_float4(_SampleTexture2D_26ce0ed679ea4be0ae36cb2a4ee15349_RGBA_0_Vector4, _Property_c081d1852d284e4fa4bbb04ea3adf35e_Out_0_Vector4, _Multiply_ff3b38102028498db7cc80acf5e42362_Out_2_Vector4);
UnityTexture2D _Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D = _Normal_Map;
float4 _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D.tex, _Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D.samplerstate, _Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D.GetTransformedUV(_Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2) );
if (_Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4, _Property_7710063f59e24cac8a925a68110c504e_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4);
float _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_R_4_Float = _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.r;
float _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_G_5_Float = _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.g;
float _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_B_6_Float = _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.b;
float _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_A_7_Float = _SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.a;
float _Property_8b1f839dfbcd4481b201427fbc7310ad_Out_0_Float = _Normal_Intensity;
float3 _NormalStrength_a218ef03cc884bf0b0962d85b628afe7_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_32cf7bd6b6a244169f096522e7390431_RGBA_0_Vector4.xyz), _Property_8b1f839dfbcd4481b201427fbc7310ad_Out_0_Float, _NormalStrength_a218ef03cc884bf0b0962d85b628afe7_Out_2_Vector3);
UnityTexture2D _Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D = _Emissive_Map;
float4 _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D.tex, _Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D.samplerstate, _Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D.GetTransformedUV(_Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2) );
if (_Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4, _Property_1dfec0b8c5b546fe886815eadcd19cfa_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_R_4_Float = _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4.r;
float _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_G_5_Float = _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4.g;
float _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_B_6_Float = _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4.b;
float _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_A_7_Float = _SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4.a;
float4 _Property_3a2f8dbe365d427e89744f8eb8dabd44_Out_0_Vector4 = _Emissive_Color;
float4 _Multiply_1efa7dc8f3994d8ea213351379d48448_Out_2_Vector4;
Unity_Multiply_float4_float4(_SampleTexture2D_12f826954a9e47dd80c4a17ca37730b4_RGBA_0_Vector4, _Property_3a2f8dbe365d427e89744f8eb8dabd44_Out_0_Vector4, _Multiply_1efa7dc8f3994d8ea213351379d48448_Out_2_Vector4);
float _Property_09c3fe7e22be43caa45c324f52ea8137_Out_0_Float = _Emissive_Intensity;
float4 _Multiply_43a1c0123fb34a25a5d21581f08926f8_Out_2_Vector4;
Unity_Multiply_float4_float4(_Multiply_1efa7dc8f3994d8ea213351379d48448_Out_2_Vector4, (_Property_09c3fe7e22be43caa45c324f52ea8137_Out_0_Float.xxxx), _Multiply_43a1c0123fb34a25a5d21581f08926f8_Out_2_Vector4);
UnityTexture2D _Property_ecbd8a4e938a458cabefe2480cada231_Out_0_Texture2D = _ORM_Map;
float _Property_2ca0dc7be16d47bfb0faf210c4d70757_Out_0_Float = _AO_Intensity;
float _Property_fb245dde136f4b2486e260e720f83c83_Out_0_Float = _Roughness_Intensity;
float _Property_0fcde0f68f364243886b868db18a8d27_Out_0_Float = _Metallic_Intensity;
Bindings_PASGORMUnpacker_abcd41e335c426a4bb0f8de17b4b5f96_float _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94;
half _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_AmbientOcclusion_1_Float;
half _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Roughness_2_Float;
half _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Metallic_3_Float;
SG_PASGORMUnpacker_abcd41e335c426a4bb0f8de17b4b5f96_float(_Multiply_4292391dea454add999ea1df3ab9bf1b_Out_2_Vector2, _Property_ecbd8a4e938a458cabefe2480cada231_Out_0_Texture2D, _Property_2ca0dc7be16d47bfb0faf210c4d70757_Out_0_Float, _Property_fb245dde136f4b2486e260e720f83c83_Out_0_Float, _Property_0fcde0f68f364243886b868db18a8d27_Out_0_Float, _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94, _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_AmbientOcclusion_1_Float, _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Roughness_2_Float, _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Metallic_3_Float);
Base_Color_5 = _Multiply_ff3b38102028498db7cc80acf5e42362_Out_2_Vector4;
Normal_1 = _NormalStrength_a218ef03cc884bf0b0962d85b628afe7_Out_2_Vector3;
Emissive_6 = _Multiply_43a1c0123fb34a25a5d21581f08926f8_Out_2_Vector4;
Ambient_Occlusion_4 = _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_AmbientOcclusion_1_Float;
Roughness_3 = _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Roughness_2_Float;
Metallic_2 = _PASGORMUnpacker_b2141cd5306546e19139b5d0f4d19f94_Metallic_3_Float;
}

void Unity_DotProduct_float3(float3 A, float3 B, out float Out)
{
    Out = dot(A, B);
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Clamp_float(float In, float Min, float Max, out float Out)
{
    Out = clamp(In, Min, Max);
}

void Unity_Contrast_float(float3 In, float Contrast, out float3 Out)
{
    float midpoint = pow(0.5, 2.2);
    Out =  (In - midpoint) * Contrast + midpoint;
}

void Unity_Clamp_float3(float3 In, float3 Min, float3 Max, out float3 Out)
{
    Out = clamp(In, Min, Max);
}

struct Bindings_PASGVerticalBlend_e9f0b2d5820244846982772499370842_float
{
float3 ObjectSpaceNormal;
float3 WorldSpaceNormal;
float3 WorldSpaceTangent;
float3 WorldSpaceBiTangent;
float3 ObjectSpacePosition;
};

void SG_PASGVerticalBlend_e9f0b2d5820244846982772499370842_float(float _Sloap_Contrast, UnityTexture2D _Slope_Noise_Texture, float _Noise_Texture_Size, float3 _Explicit_Normal, float _Sloap_Offset, Bindings_PASGVerticalBlend_e9f0b2d5820244846982772499370842_float IN, out float3 OutVector3_1)
{
float3 _Property_5509c7fb30e445229f496a71c6e9c192_Out_0_Vector3 = _Explicit_Normal;
float3 _Transform_9c9f921cd0934012ac0d281c8e40a727_Out_1_Vector3;
{
float3x3 tangentTransform = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
_Transform_9c9f921cd0934012ac0d281c8e40a727_Out_1_Vector3 = TransformTangentToWorld(_Property_5509c7fb30e445229f496a71c6e9c192_Out_0_Vector3.xyz, tangentTransform, true);
}
float _DotProduct_4bc69611266041259c8da1b387474ec9_Out_2_Float;
Unity_DotProduct_float3(_Transform_9c9f921cd0934012ac0d281c8e40a727_Out_1_Vector3, float3(0, 1, 0), _DotProduct_4bc69611266041259c8da1b387474ec9_Out_2_Float);
float _Property_6a995c4e80824d6fa7d458ddcdc4eec3_Out_0_Float = _Sloap_Offset;
float _Add_a196acc146d1466fb309ec050e77feb2_Out_2_Float;
Unity_Add_float(_DotProduct_4bc69611266041259c8da1b387474ec9_Out_2_Float, _Property_6a995c4e80824d6fa7d458ddcdc4eec3_Out_0_Float, _Add_a196acc146d1466fb309ec050e77feb2_Out_2_Float);
float _Lerp_a1fed41c0847449ab72ae8fe4775270c_Out_3_Float;
Unity_Lerp_float(float(0), float(3), _Add_a196acc146d1466fb309ec050e77feb2_Out_2_Float, _Lerp_a1fed41c0847449ab72ae8fe4775270c_Out_3_Float);
float _Clamp_d4d6e762b7b1422d936b98d7f9429754_Out_3_Float;
Unity_Clamp_float(_Lerp_a1fed41c0847449ab72ae8fe4775270c_Out_3_Float, float(0), float(1), _Clamp_d4d6e762b7b1422d936b98d7f9429754_Out_3_Float);
float _Property_985b04ddd40847029dbfa829fbcaeed1_Out_0_Float = _Sloap_Contrast;
float3 _Contrast_2864897542064adcb4b61fcccadaf026_Out_2_Vector3;
Unity_Contrast_float((_Clamp_d4d6e762b7b1422d936b98d7f9429754_Out_3_Float.xxx), _Property_985b04ddd40847029dbfa829fbcaeed1_Out_0_Float, _Contrast_2864897542064adcb4b61fcccadaf026_Out_2_Vector3);
float3 _Clamp_b8f1f82d045f437a910a54b62bb2df81_Out_3_Vector3;
Unity_Clamp_float3(_Contrast_2864897542064adcb4b61fcccadaf026_Out_2_Vector3, float3(0, 0, 0), float3(1, 1, 1), _Clamp_b8f1f82d045f437a910a54b62bb2df81_Out_3_Vector3);
float3 _Transform_229ffe25002343a39c8caaa3d7f6df91_Out_1_Vector3;
_Transform_229ffe25002343a39c8caaa3d7f6df91_Out_1_Vector3 = TransformObjectToWorldDir(IN.ObjectSpacePosition.xyz, true);
float3 _Vector3_ec86905b34c54695bed5b7d1975cb6ce_Out_0_Vector3 = float3(float(0), float(2), float(0));
float _DotProduct_af3cb3caf6e4473c8161868b15c0255b_Out_2_Float;
Unity_DotProduct_float3(_Transform_229ffe25002343a39c8caaa3d7f6df91_Out_1_Vector3, _Vector3_ec86905b34c54695bed5b7d1975cb6ce_Out_0_Vector3, _DotProduct_af3cb3caf6e4473c8161868b15c0255b_Out_2_Float);
float _Add_47be9b48b4c94dd28e8c3db7f6ba1a05_Out_2_Float;
Unity_Add_float(_Property_6a995c4e80824d6fa7d458ddcdc4eec3_Out_0_Float, _DotProduct_af3cb3caf6e4473c8161868b15c0255b_Out_2_Float, _Add_47be9b48b4c94dd28e8c3db7f6ba1a05_Out_2_Float);
float _Multiply_35491cd334cb4e34a76b48de2bc5814a_Out_2_Float;
Unity_Multiply_float_float(_Add_47be9b48b4c94dd28e8c3db7f6ba1a05_Out_2_Float, 2, _Multiply_35491cd334cb4e34a76b48de2bc5814a_Out_2_Float);
float _Clamp_ccd0969e9da6420f94e7113654fd480e_Out_3_Float;
Unity_Clamp_float(_Multiply_35491cd334cb4e34a76b48de2bc5814a_Out_2_Float, float(0), float(1), _Clamp_ccd0969e9da6420f94e7113654fd480e_Out_3_Float);
float3 _Contrast_454ba7c8fb3a4d03951b073867fa5f14_Out_2_Vector3;
Unity_Contrast_float((_Clamp_ccd0969e9da6420f94e7113654fd480e_Out_3_Float.xxx), _Property_985b04ddd40847029dbfa829fbcaeed1_Out_0_Float, _Contrast_454ba7c8fb3a4d03951b073867fa5f14_Out_2_Vector3);
float3 _Clamp_1ba5cf0aee8245bd9dca155954c3df81_Out_3_Vector3;
Unity_Clamp_float3(_Contrast_454ba7c8fb3a4d03951b073867fa5f14_Out_2_Vector3, float3(0, 0, 0), float3(1, 1, 1), _Clamp_1ba5cf0aee8245bd9dca155954c3df81_Out_3_Vector3);
UnityTexture2D _Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D = _Slope_Noise_Texture;
float _Property_8e663ddd1476448d948a9de1bf1f83a9_Out_0_Float = _Noise_Texture_Size;
float3 Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_UV = IN.ObjectSpacePosition * _Property_8e663ddd1476448d948a9de1bf1f83a9_Out_0_Float;
float3 Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend = SafePositivePow_float(IN.ObjectSpaceNormal, min(float(1), floor(log2(Min_float())/log2(1/sqrt(3)))) );
Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend /= dot(Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend, 1.0);
float4 Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_X = SAMPLE_TEXTURE2D(_Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.tex, _Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.samplerstate, Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_UV.zy);
float4 Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Y = SAMPLE_TEXTURE2D(_Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.tex, _Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.samplerstate, Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_UV.xz);
float4 Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Z = SAMPLE_TEXTURE2D(_Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.tex, _Property_0258c597023f486fba2540750b2ddba1_Out_0_Texture2D.samplerstate, Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_UV.xy);
float4 _Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Out_0_Vector4 = Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_X * Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend.x + Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Y * Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend.y + Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Z * Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Blend.z;
float _Swizzle_90fb1902a2bd4070b4f9e27ac287ca97_Out_1_Float = (_Triplanar_25d1a1d5ad4a4f0d95ecbf6d4556e7e7_Out_0_Vector4.xy).y;
float _Clamp_f32c97c6ccbf44649bd757503ccdbe71_Out_3_Float;
Unity_Clamp_float(_Swizzle_90fb1902a2bd4070b4f9e27ac287ca97_Out_1_Float, float(0), float(1), _Clamp_f32c97c6ccbf44649bd757503ccdbe71_Out_3_Float);
float _Add_92d15430b3c042719d65f1252bc35d7a_Out_2_Float;
Unity_Add_float(_Multiply_35491cd334cb4e34a76b48de2bc5814a_Out_2_Float, _Clamp_f32c97c6ccbf44649bd757503ccdbe71_Out_3_Float, _Add_92d15430b3c042719d65f1252bc35d7a_Out_2_Float);
float3 _Contrast_c2fe106c51f0498e987cf31090ec563d_Out_2_Vector3;
Unity_Contrast_float((_Add_92d15430b3c042719d65f1252bc35d7a_Out_2_Float.xxx), _Property_985b04ddd40847029dbfa829fbcaeed1_Out_0_Float, _Contrast_c2fe106c51f0498e987cf31090ec563d_Out_2_Vector3);
float3 _Clamp_05438405442c47cb98e571d128277bbe_Out_3_Vector3;
Unity_Clamp_float3(_Contrast_c2fe106c51f0498e987cf31090ec563d_Out_2_Vector3, float3(0, 0, 0), float3(1, 1, 1), _Clamp_05438405442c47cb98e571d128277bbe_Out_3_Vector3);
#if defined(_EDGEBLENDING_NORMALBLENDING)
float3 _EdgeBlending_cdbe33a15cf6452b8881ecea22adcb84_Out_0_Vector3 = _Clamp_b8f1f82d045f437a910a54b62bb2df81_Out_3_Vector3;
#elif defined(_EDGEBLENDING_VERTEXBLENDING)
float3 _EdgeBlending_cdbe33a15cf6452b8881ecea22adcb84_Out_0_Vector3 = _Clamp_1ba5cf0aee8245bd9dca155954c3df81_Out_3_Vector3;
#else
float3 _EdgeBlending_cdbe33a15cf6452b8881ecea22adcb84_Out_0_Vector3 = _Clamp_05438405442c47cb98e571d128277bbe_Out_3_Vector3;
#endif
OutVector3_1 = _EdgeBlending_cdbe33a15cf6452b8881ecea22adcb84_Out_0_Vector3;
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Branch_float3(float Predicate, float3 True, float3 False, out float3 Out)
{
    Out = Predicate ? True : False;
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
description.Position = IN.ObjectSpacePosition;
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
float Alpha;
float AlphaClipThreshold;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
float _Property_868c674f556d42be9071980dc60f4aed_Out_0_Boolean = _EnableCoverage;
float _Property_a0b21575a96c42a2b05aa3518645b2f4_Out_0_Float = _UV_Size;
UnityTexture2D _Property_4d248f4a9de94d7bb9a5c3c7c4c10a68_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_ColorMap, sampler_ColorMap, _ColorMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _Property_061dd20995c54e4b8c0357aa709c6e56_Out_0_Vector4 = _ColorTint;
UnityTexture2D _Property_cc904da6b4714cd6a9559b3bcf6ba7a6_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_NormalMap, sampler_NormalMap, _NormalMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_49f412000c0b4f18863fb7afae8b43fc_Out_0_Float = _NormalIntensity;
UnityTexture2D _Property_3fcfe917c75d4a46bfe50da31a24c74f_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_ORMMap, sampler_ORMMap, _ORMMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_bd35b765c62349f285447de81728f0c8_Out_0_Float = _RoughnessIntensity;
float _Property_f125a72836c24b0fa5ef72823c8e6452_Out_0_Float = _AOIntensity;
float _Property_0251a196075947af9d3ad28d6875a5ef_Out_0_Float = _MetallicIntensity;
UnityTexture2D _Property_47ecc092fbd144218fe6440250e367b3_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Emissive_Map, sampler_Emissive_Map, _Emissive_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _Property_c065f0d4df8c40528116ee8e516b2221_Out_0_Vector4 = _EmissiveColor;
float _Property_0038e41f028740e4abc1958ca58302a1_Out_0_Float = _EmissiveIntensity;
Bindings_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe;
_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe.uv0 = IN.uv0;
float4 _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_BaseColor_5_Vector4;
float3 _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Normal_1_Vector3;
float4 _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Emissive_6_Vector4;
float _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_AmbientOcclusion_4_Float;
float _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Roughness_3_Float;
float _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Metallic_2_Float;
SG_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float(_Property_a0b21575a96c42a2b05aa3518645b2f4_Out_0_Float, _Property_4d248f4a9de94d7bb9a5c3c7c4c10a68_Out_0_Texture2D, _Property_061dd20995c54e4b8c0357aa709c6e56_Out_0_Vector4, _Property_cc904da6b4714cd6a9559b3bcf6ba7a6_Out_0_Texture2D, _Property_49f412000c0b4f18863fb7afae8b43fc_Out_0_Float, _Property_3fcfe917c75d4a46bfe50da31a24c74f_Out_0_Texture2D, _Property_bd35b765c62349f285447de81728f0c8_Out_0_Float, _Property_f125a72836c24b0fa5ef72823c8e6452_Out_0_Float, _Property_0251a196075947af9d3ad28d6875a5ef_Out_0_Float, _Property_47ecc092fbd144218fe6440250e367b3_Out_0_Texture2D, _Property_c065f0d4df8c40528116ee8e516b2221_Out_0_Vector4, _Property_0038e41f028740e4abc1958ca58302a1_Out_0_Float, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_BaseColor_5_Vector4, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Normal_1_Vector3, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Emissive_6_Vector4, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_AmbientOcclusion_4_Float, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Roughness_3_Float, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Metallic_2_Float);
float _Property_623a0c9d694c4c33b6256bde952c53e5_Out_0_Float = _CoverageSize;
UnityTexture2D _Property_58d380b2034145c8b7d2eda637dbc296_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_CoverageColorMap, sampler_CoverageColorMap, _CoverageColorMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
UnityTexture2D _Property_f324dd6628ea45b1a630d750fa3d6b66_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_CoverageNormalMap, sampler_CoverageNormalMap, _CoverageNormalMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_b00b34515282450295cc4f4eb7b4a0ce_Out_0_Float = _CoverageNormalIntensity;
UnityTexture2D _Property_24ae6469bfeb4dada6fb7c7d39985cb3_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_CoverageORMMap, sampler_CoverageORMMap, _CoverageORMMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_03c29a27092f4bbb8a4fcc3b6e7b2909_Out_0_Float = _CoverageRoughnessIntensity;
float _Property_07cad64e572e4602a346b7dd90c3eb21_Out_0_Float = _CoverageMetallicIntensity;
Bindings_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c;
_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c.uv0 = IN.uv0;
float4 _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_BaseColor_5_Vector4;
float3 _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Normal_1_Vector3;
float4 _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Emissive_6_Vector4;
float _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_AmbientOcclusion_4_Float;
float _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Roughness_3_Float;
float _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Metallic_2_Float;
SG_PASGBasePBROpaqueORM_b84638f1462ad6248adaaf7ed5319e6d_float(_Property_623a0c9d694c4c33b6256bde952c53e5_Out_0_Float, _Property_58d380b2034145c8b7d2eda637dbc296_Out_0_Texture2D, float4 (1, 1, 1, 1), _Property_f324dd6628ea45b1a630d750fa3d6b66_Out_0_Texture2D, _Property_b00b34515282450295cc4f4eb7b4a0ce_Out_0_Float, _Property_24ae6469bfeb4dada6fb7c7d39985cb3_Out_0_Texture2D, _Property_03c29a27092f4bbb8a4fcc3b6e7b2909_Out_0_Float, float(0), _Property_07cad64e572e4602a346b7dd90c3eb21_Out_0_Float, UnityBuildTexture2DStructNoScale(_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D), float4 (0, 0, 0, 0), float(0), _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_BaseColor_5_Vector4, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Normal_1_Vector3, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Emissive_6_Vector4, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_AmbientOcclusion_4_Float, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Roughness_3_Float, _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_Metallic_2_Float);
float _Property_0785fc58d71d4d63abb8c1102aa1a1b7_Out_0_Float = _SloapContrast;
UnityTexture2D _Property_12921d1841154c59b11aefccb2cbca31_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_SlopeNoiseTexture, sampler_SlopeNoiseTexture, _SlopeNoiseTexture_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_10dd46128137476fb3fd49de6154e94c_Out_0_Float = _NoiseTextureSize;
float _Property_a654ef8b1e2a4529ab82f92b3ae202ec_Out_0_Float = _SloapOffset;
Bindings_PASGVerticalBlend_e9f0b2d5820244846982772499370842_float _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d;
_PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d.ObjectSpaceNormal = IN.ObjectSpaceNormal;
_PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d.WorldSpaceNormal = IN.WorldSpaceNormal;
_PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d.WorldSpaceTangent = IN.WorldSpaceTangent;
_PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d.WorldSpaceBiTangent = IN.WorldSpaceBiTangent;
_PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d.ObjectSpacePosition = IN.ObjectSpacePosition;
float3 _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d_OutVector3_1_Vector3;
SG_PASGVerticalBlend_e9f0b2d5820244846982772499370842_float(_Property_0785fc58d71d4d63abb8c1102aa1a1b7_Out_0_Float, _Property_12921d1841154c59b11aefccb2cbca31_Out_0_Texture2D, _Property_10dd46128137476fb3fd49de6154e94c_Out_0_Float, _PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Normal_1_Vector3, _Property_a654ef8b1e2a4529ab82f92b3ae202ec_Out_0_Float, _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d, _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d_OutVector3_1_Vector3);
float3 _Lerp_8ef5a2e39e934fb09e1bc6858ba97959_Out_3_Vector3;
Unity_Lerp_float3((_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_BaseColor_5_Vector4.xyz), (_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_BaseColor_5_Vector4.xyz), _PASGVerticalBlend_af82298301954d6ba61a73b1ae931f1d_OutVector3_1_Vector3, _Lerp_8ef5a2e39e934fb09e1bc6858ba97959_Out_3_Vector3);
float3 _Branch_72adc4bf3fd64427a9a9035afa6f856a_Out_3_Vector3;
Unity_Branch_float3(_Property_868c674f556d42be9071980dc60f4aed_Out_0_Boolean, _Lerp_8ef5a2e39e934fb09e1bc6858ba97959_Out_3_Vector3, (_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_BaseColor_5_Vector4.xyz), _Branch_72adc4bf3fd64427a9a9035afa6f856a_Out_3_Vector3);
UnityTexture2D _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_ColorMap_1, sampler_ColorMap_1, _ColorMap_1_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.tex, _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.samplerstate, _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4, _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_R_4_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.r;
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_G_5_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.g;
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_B_6_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.b;
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_A_7_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.a;
float _Property_7a43d5f1d3df46c2869bc70eebee1f91_Out_0_Float = _EmissiveIntensity_1;
surface.BaseColor = _Branch_72adc4bf3fd64427a9a9035afa6f856a_Out_3_Vector3;
surface.Emission = (_PASGBasePBROpaqueORM_f44b35e7a9c94bb9b00190082f8fddbe_Emissive_6_Vector4.xyz);
surface.Alpha = (_SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4).x;
surface.AlphaClipThreshold = _Property_7a43d5f1d3df46c2869bc70eebee1f91_Out_0_Float;
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

    // use bitangent on the fly like in hdrp
    // IMPORTANT! If we ever support Flip on double sided materials ensure bitangent and tangent are NOT flipped.
    float crossSign = (input.tangentWS.w > 0.0 ? 1.0 : -1.0)* GetOddNegativeScale();
    float3 bitang = crossSign * cross(input.normalWS.xyz, input.tangentWS.xyz);

    output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph
    output.ObjectSpaceNormal = normalize(mul(output.WorldSpaceNormal, (float3x3) UNITY_MATRIX_M));           // transposed multiplication by inverse matrix to handle normal scale

    // to preserve mikktspace compliance we use same scale renormFactor as was used on the normal.
    // This is explained in section 2.2 in "surface gradient based bump mapping framework"
    output.WorldSpaceTangent = renormFactor * input.tangentWS.xyz;
    output.WorldSpaceBiTangent = renormFactor * bitang;

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
#pragma shader_feature_local_fragment _ _BUILTIN_SURFACE_TYPE_TRANSPARENT
#pragma shader_feature_local_fragment _ _BUILTIN_AlphaClip
#pragma shader_feature_local_fragment _ _BUILTIN_ALPHATEST_ON
#pragma shader_feature_local _EDGEBLENDING_NORMALBLENDING _EDGEBLENDING_VERTEXBLENDING _EDGEBLENDING_NOISEBLENDING

#if defined(_EDGEBLENDING_NORMALBLENDING)
#define KEYWORD_PERMUTATION_0
#elif defined(_EDGEBLENDING_VERTEXBLENDING)
#define KEYWORD_PERMUTATION_1
#else
#define KEYWORD_PERMUTATION_2
#endif


// Defines
#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define VARYINGS_NEED_TEXCOORD0
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
 float4 uv0 : TEXCOORD0;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
 float4 positionCS : SV_POSITION;
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
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
 float4 texCoord0 : INTERP0;
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

float4 _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D_TexelSize;
float4 _ColorMap_TexelSize;
float4 _ColorMap_1_TexelSize;
float4 _NormalMap_TexelSize;
float4 _ColorTint;
float _NormalIntensity;
float _UV_Size;
float4 _ORMMap_TexelSize;
float _RoughnessIntensity;
float _AOIntensity;
float _MetallicIntensity;
float4 _EmissiveColor;
float _EmissiveIntensity_1;
float4 _Emissive_Map_TexelSize;
float _EmissiveIntensity;
float _EnableCoverage;
float _CoverageSize;
float _CoverageNormalIntensity;
float _CoverageRoughnessIntensity;
float _CoverageMetallicIntensity;
float _NoiseTextureSize;
float4 _SlopeNoiseTexture_TexelSize;
float4 _CoverageColorMap_TexelSize;
float4 _CoverageORMMap_TexelSize;
float4 _CoverageNormalMap_TexelSize;
float _SloapOffset;
float _SloapContrast;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D);
SAMPLER(sampler_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D);
TEXTURE2D(_ColorMap);
SAMPLER(sampler_ColorMap);
TEXTURE2D(_ColorMap_1);
SAMPLER(sampler_ColorMap_1);
TEXTURE2D(_NormalMap);
SAMPLER(sampler_NormalMap);
TEXTURE2D(_ORMMap);
SAMPLER(sampler_ORMMap);
TEXTURE2D(_Emissive_Map);
SAMPLER(sampler_Emissive_Map);
TEXTURE2D(_SlopeNoiseTexture);
SAMPLER(sampler_SlopeNoiseTexture);
TEXTURE2D(_CoverageColorMap);
SAMPLER(sampler_CoverageColorMap);
TEXTURE2D(_CoverageORMMap);
SAMPLER(sampler_CoverageORMMap);
TEXTURE2D(_CoverageNormalMap);
SAMPLER(sampler_CoverageNormalMap);

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
// GraphFunctions: <None>

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
description.Position = IN.ObjectSpacePosition;
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
float Alpha;
float AlphaClipThreshold;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
UnityTexture2D _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_ColorMap_1, sampler_ColorMap_1, _ColorMap_1_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.tex, _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.samplerstate, _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4, _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_R_4_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.r;
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_G_5_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.g;
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_B_6_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.b;
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_A_7_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.a;
float _Property_7a43d5f1d3df46c2869bc70eebee1f91_Out_0_Float = _EmissiveIntensity_1;
surface.Alpha = (_SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4).x;
surface.AlphaClipThreshold = _Property_7a43d5f1d3df46c2869bc70eebee1f91_Out_0_Float;
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
Cull [_BUILTIN_CullMode]

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
#pragma shader_feature_local_fragment _ _BUILTIN_SURFACE_TYPE_TRANSPARENT
#pragma shader_feature_local_fragment _ _BUILTIN_AlphaClip
#pragma shader_feature_local_fragment _ _BUILTIN_ALPHATEST_ON
#pragma shader_feature_local _EDGEBLENDING_NORMALBLENDING _EDGEBLENDING_VERTEXBLENDING _EDGEBLENDING_NOISEBLENDING

#if defined(_EDGEBLENDING_NORMALBLENDING)
#define KEYWORD_PERMUTATION_0
#elif defined(_EDGEBLENDING_VERTEXBLENDING)
#define KEYWORD_PERMUTATION_1
#else
#define KEYWORD_PERMUTATION_2
#endif


// Defines
#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define VARYINGS_NEED_TEXCOORD0
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
 float4 uv0 : TEXCOORD0;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
 float4 positionCS : SV_POSITION;
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
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
 float4 texCoord0 : INTERP0;
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

float4 _PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D_TexelSize;
float4 _ColorMap_TexelSize;
float4 _ColorMap_1_TexelSize;
float4 _NormalMap_TexelSize;
float4 _ColorTint;
float _NormalIntensity;
float _UV_Size;
float4 _ORMMap_TexelSize;
float _RoughnessIntensity;
float _AOIntensity;
float _MetallicIntensity;
float4 _EmissiveColor;
float _EmissiveIntensity_1;
float4 _Emissive_Map_TexelSize;
float _EmissiveIntensity;
float _EnableCoverage;
float _CoverageSize;
float _CoverageNormalIntensity;
float _CoverageRoughnessIntensity;
float _CoverageMetallicIntensity;
float _NoiseTextureSize;
float4 _SlopeNoiseTexture_TexelSize;
float4 _CoverageColorMap_TexelSize;
float4 _CoverageORMMap_TexelSize;
float4 _CoverageNormalMap_TexelSize;
float _SloapOffset;
float _SloapContrast;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D);
SAMPLER(sampler_PASGBasePBROpaqueORM_65a53f0dc42548d8af356cfcee93394c_EmissiveMap_3204051104_Texture2D);
TEXTURE2D(_ColorMap);
SAMPLER(sampler_ColorMap);
TEXTURE2D(_ColorMap_1);
SAMPLER(sampler_ColorMap_1);
TEXTURE2D(_NormalMap);
SAMPLER(sampler_NormalMap);
TEXTURE2D(_ORMMap);
SAMPLER(sampler_ORMMap);
TEXTURE2D(_Emissive_Map);
SAMPLER(sampler_Emissive_Map);
TEXTURE2D(_SlopeNoiseTexture);
SAMPLER(sampler_SlopeNoiseTexture);
TEXTURE2D(_CoverageColorMap);
SAMPLER(sampler_CoverageColorMap);
TEXTURE2D(_CoverageORMMap);
SAMPLER(sampler_CoverageORMMap);
TEXTURE2D(_CoverageNormalMap);
SAMPLER(sampler_CoverageNormalMap);

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
// GraphFunctions: <None>

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
description.Position = IN.ObjectSpacePosition;
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
float Alpha;
float AlphaClipThreshold;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
UnityTexture2D _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_ColorMap_1, sampler_ColorMap_1, _ColorMap_1_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.tex, _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.samplerstate, _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4, _Property_f800b2d841fc49d0aef60a05219616d6_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_R_4_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.r;
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_G_5_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.g;
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_B_6_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.b;
float _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_A_7_Float = _SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4.a;
float _Property_7a43d5f1d3df46c2869bc70eebee1f91_Out_0_Float = _EmissiveIntensity_1;
surface.Alpha = (_SampleTexture2D_4daf626e1a394877a54326ade11b67ca_RGBA_0_Vector4).x;
surface.AlphaClipThreshold = _Property_7a43d5f1d3df46c2869bc70eebee1f91_Out_0_Float;
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