Shader "UPlayGround/Environment/Vegetation/90363b2158dd4a5b87d0dfe394cdde30"
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

[NoScaleOffset]_Texture("Texture", 2D) = "white" {}
[ToggleUI]_Custom_Color("Custom Color", Float) = 1
[ToggleUI]_Enable_Top_Color("Enable Top Color", Float) = 1
_Top_Color("Top Color", Color) = (1, 1, 1, 0)
_Bottom_Color("Bottom Color", Color) = (1, 1, 1, 0)
_Blend_Height("Blend Height", Range(0, 1)) = 0
_Alpha_Cutoff("Alpha Cutoff", Range(0, 1)) = 0.5
[KeywordEnum(Vertex Position, UV)]_COLOR_OVERLAY("Color Overlay", Float) = 1
_Wind_Strength("Wind Strength", Range(0, 1)) = 0.2
_Wind_Speed("Wind Speed", Range(0, 1)) = 0.2
_Wind_Variation("Wind Variation", Range(0, 1)) = 0.2
_Wave_Scale("Wave Scale", Range(0, 1)) = 1
_Wind_Direction("Wind Direction", Vector, 2) = (0.1, 0.1, 0, 0)
_Start_Bend_Range("Start Bend Range", Range(0.3, 1)) = 0.3
_Bend_Strength("Bend Strength", Range(0, 1)) = 0.5
[HideInInspector]_WorkflowMode("_WorkflowMode", Float) = 1
[HideInInspector]_CastShadows("_CastShadows", Float) = 1
[HideInInspector]_ReceiveShadows("_ReceiveShadows", Float) = 1
[HideInInspector]_Surface("_Surface", Float) = 0
[HideInInspector]_Blend("_Blend", Float) = 0
[HideInInspector]_AlphaClip("_AlphaClip", Float) = 1
[HideInInspector]_BlendModePreserveSpecular("_BlendModePreserveSpecular", Float) = 1
[HideInInspector]_SrcBlend("_SrcBlend", Float) = 1
[HideInInspector]_DstBlend("_DstBlend", Float) = 0
[HideInInspector]_SrcBlendAlpha("_SrcBlendAlpha", Float) = 1
[HideInInspector]_DstBlendAlpha("_DstBlendAlpha", Float) = 0
[HideInInspector][ToggleUI]_ZWrite("_ZWrite", Float) = 1
[HideInInspector]_ZWriteControl("_ZWriteControl", Float) = 0
[HideInInspector]_ZTest("_ZTest", Float) = 4
[HideInInspector]_Cull("_Cull", Float) = 0
[HideInInspector]_AlphaToMask("_AlphaToMask", Float) = 1
[HideInInspector]_QueueOffset("_QueueOffset", Float) = 0
[HideInInspector]_QueueControl("_QueueControl", Float) = -1
[HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
[HideInInspector][NoScaleOffset]unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
[HideInInspector][NoScaleOffset]unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
}
SubShader
{
Tags
{
"RenderPipeline"="UniversalPipeline"
"RenderType"="Opaque"
"UniversalMaterialType" = "Lit"
"Queue"="AlphaTest"
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
#pragma shader_feature _ _SOFT_SHADOWS
#pragma shader_feature_local _COLOR_OVERLAY_VERTEX_POSITION _COLOR_OVERLAY_UV

#if defined(_SOFT_SHADOWS) && defined(_COLOR_OVERLAY_VERTEX_POSITION)
#define KEYWORD_PERMUTATION_0
#elif defined(_SOFT_SHADOWS) && defined(_COLOR_OVERLAY_UV)
#define KEYWORD_PERMUTATION_1
#elif defined(_COLOR_OVERLAY_VERTEX_POSITION)
#define KEYWORD_PERMUTATION_2
#else
#define KEYWORD_PERMUTATION_3
#endif


// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_TEXCOORD1
#define ATTRIBUTES_NEED_TEXCOORD2
#define ATTRIBUTES_NEED_TEXCOORD3
#define ATTRIBUTES_NEED_COLOR
#define GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_NORMAL_WS
#define VARYINGS_NEED_TANGENT_WS
#define VARYINGS_NEED_TEXCOORD0
#define VARYINGS_NEED_TEXCOORD1
#define VARYINGS_NEED_TEXCOORD2
#define VARYINGS_NEED_TEXCOORD3
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
 float4 uv3 : TEXCOORD3;
 float4 color : COLOR;
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
 float4 texCoord3;
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
 float3 WorldSpacePosition;
 float2 PixelPosition;
 float4 uv0;
 float4 uv1;
 float4 uv2;
 float4 uv3;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 WorldSpacePosition;
 float3 AbsoluteWorldSpacePosition;
 float4 uv0;
 float4 VertexColor;
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
 float4 texCoord1 : INTERP7;
 float4 texCoord2 : INTERP8;
 float4 texCoord3 : INTERP9;
 float4 fogFactorAndVertexLight : INTERP10;
 float3 positionWS : INTERP11;
 float3 normalWS : INTERP12;
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
output.texCoord1.xyzw = input.texCoord1;
output.texCoord2.xyzw = input.texCoord2;
output.texCoord3.xyzw = input.texCoord3;
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
output.texCoord1 = input.texCoord1.xyzw;
output.texCoord2 = input.texCoord2.xyzw;
output.texCoord3 = input.texCoord3.xyzw;
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

float4 _Texture_TexelSize;
float4 _Top_Color;
float _Custom_Color;
float4 _Bottom_Color;
float _Blend_Height;
float _Wind_Strength;
float _Wind_Speed;
float _Wind_Variation;
float2 _Wind_Direction;
float _Alpha_Cutoff;
float _Enable_Top_Color;
float _Start_Bend_Range;
float _Wave_Scale;
float _Bend_Strength;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Texture);
SAMPLER(sampler_Texture);
float3 _Player_Position;

// Graph Includes
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"
#include_with_pragmas "Assets/06.Shaders/Foliage/UPG_FoliageInteraction.hlsl"
#include_with_pragmas "Assets/ExternalAssets/Environment/Idyllic Fantasy Nature/Shader/CustomLighting.hlsl"

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

void Unity_Normalize_float2(float2 In, out float2 Out)
{
    Out = normalize(In);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_Add_float2(float2 A, float2 B, out float2 Out)
{
    Out = A + B;
}

float2 Unity_GradientNoise_Deterministic_Dir_float(float2 p)
{
float x; Hash_Tchou_2_1_float(p, x);
return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
}

void Unity_GradientNoise_Deterministic_float (float2 UV, float3 Scale, out float Out)
{
float2 p = UV * Scale.xy;
float2 ip = floor(p);
float2 fp = frac(p);
float d00 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip), fp);
float d01 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(0, 1)), fp - float2(0, 1));
float d10 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(1, 0)), fp - float2(1, 0));
float d11 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(1, 1)), fp - float2(1, 1));
fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
Out = lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5;
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Normalize_float3(float3 In, out float3 Out)
{
    Out = normalize(In);
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
{
    Out = lerp(A, B, T);
}

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
}

void Unity_Subtract_float3(float3 A, float3 B, out float3 Out)
{
    Out = A - B;
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Distance_float3(float3 A, float3 B, out float Out)
{
    Out = distance(A, B);
}

void Unity_Clamp_float(float In, float Min, float Max, out float Out)
{
    Out = clamp(In, Min, Max);
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
}

void Unity_Step_float(float Edge, float In, out float Out)
{
    Out = step(Edge, In);
}

void Unity_Branch_float4(float Predicate, float4 True, float4 False, out float4 Out)
{
    Out = Predicate ? True : False;
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_SampleGradientV1_float(Gradient Gradient, float Time, out float4 Out)
{
    // convert to OkLab if we need perceptual color space.
    float3 color = lerp(Gradient.colors[0].rgb, LinearToOklab(Gradient.colors[0].rgb), Gradient.type == 2);

    [unroll]
    for (int c = 1; c < Gradient.colorsLength; c++)
    {
        float colorPos = saturate((Time - Gradient.colors[c - 1].w) / (Gradient.colors[c].w - Gradient.colors[c - 1].w)) * step(c, Gradient.colorsLength - 1);
        float3 color2 = lerp(Gradient.colors[c].rgb, LinearToOklab(Gradient.colors[c].rgb), Gradient.type == 2);
        color = lerp(color, color2, lerp(colorPos, step(0.01, colorPos), Gradient.type % 2)); // grad.type == 1 is fixed, 0 and 2 are blends.
    }
    color = lerp(color, OklabToLinear(color), Gradient.type == 2);

#ifdef UNITY_COLORSPACE_GAMMA
    color = LinearToSRGB(color);
#endif

    float alpha = Gradient.alphas[0].x;
    [unroll]
    for (int a = 1; a < Gradient.alphasLength; a++)
    {
        float alphaPos = saturate((Time - Gradient.alphas[a - 1].y) / (Gradient.alphas[a].y - Gradient.alphas[a - 1].y)) * step(a, Gradient.alphasLength - 1);
        alpha = lerp(alpha, Gradient.alphas[a].x, lerp(alphaPos, step(0.01, alphaPos), Gradient.type % 2));
    }

    Out = float4(color, alpha);
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
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
float _Split_f9cfb9a555e949379da9876f8d3844b2_R_1_Float = IN.WorldSpacePosition[0];
float _Split_f9cfb9a555e949379da9876f8d3844b2_G_2_Float = IN.WorldSpacePosition[1];
float _Split_f9cfb9a555e949379da9876f8d3844b2_B_3_Float = IN.WorldSpacePosition[2];
float _Split_f9cfb9a555e949379da9876f8d3844b2_A_4_Float = 0;
float2 _Vector2_feeecec8507c4619844c3ca15ea244be_Out_0_Vector2 = float2(_Split_f9cfb9a555e949379da9876f8d3844b2_R_1_Float, _Split_f9cfb9a555e949379da9876f8d3844b2_B_3_Float);
float2 _Property_186d136719274f03b9464e4e1d37a4e8_Out_0_Vector2 = _Wind_Direction;
float2 _Normalize_fb934df0b0c946039db4091949a06211_Out_1_Vector2;
Unity_Normalize_float2(_Property_186d136719274f03b9464e4e1d37a4e8_Out_0_Vector2, _Normalize_fb934df0b0c946039db4091949a06211_Out_1_Vector2);
float _Property_a98f7a88c8ac40109941c7f4f4211495_Out_0_Float = _Wind_Speed;
float _Multiply_880934a6c2a44f658fe3c3b3ed7cccac_Out_2_Float;
Unity_Multiply_float_float(10, _Property_a98f7a88c8ac40109941c7f4f4211495_Out_0_Float, _Multiply_880934a6c2a44f658fe3c3b3ed7cccac_Out_2_Float);
float2 _Multiply_892e7af0e8e54f869ff77c70575af5fc_Out_2_Vector2;
Unity_Multiply_float2_float2(_Normalize_fb934df0b0c946039db4091949a06211_Out_1_Vector2, (_Multiply_880934a6c2a44f658fe3c3b3ed7cccac_Out_2_Float.xx), _Multiply_892e7af0e8e54f869ff77c70575af5fc_Out_2_Vector2);
float2 _Multiply_d96cf1c14ac74322b4498e1849526399_Out_2_Vector2;
Unity_Multiply_float2_float2(_Multiply_892e7af0e8e54f869ff77c70575af5fc_Out_2_Vector2, (IN.TimeParameters.x.xx), _Multiply_d96cf1c14ac74322b4498e1849526399_Out_2_Vector2);
float2 _Add_c52cdcdd15b74d9a963511973a04621c_Out_2_Vector2;
Unity_Add_float2(_Vector2_feeecec8507c4619844c3ca15ea244be_Out_0_Vector2, _Multiply_d96cf1c14ac74322b4498e1849526399_Out_2_Vector2, _Add_c52cdcdd15b74d9a963511973a04621c_Out_2_Vector2);
float _Property_14515a2f6d834bd99002a2ccf491767f_Out_0_Float = _Wave_Scale;
float _Multiply_bbdcafe2ee654e3d8e4df25d9c66103c_Out_2_Float;
Unity_Multiply_float_float(3, _Property_14515a2f6d834bd99002a2ccf491767f_Out_0_Float, _Multiply_bbdcafe2ee654e3d8e4df25d9c66103c_Out_2_Float);
float _GradientNoise_32a9a6c418e44409a86a8238a6b4645c_Out_2_Float;
Unity_GradientNoise_Deterministic_float(_Add_c52cdcdd15b74d9a963511973a04621c_Out_2_Vector2, _Multiply_bbdcafe2ee654e3d8e4df25d9c66103c_Out_2_Float, _GradientNoise_32a9a6c418e44409a86a8238a6b4645c_Out_2_Float);
float _Subtract_3ec4867605194525898104109bc1e4a8_Out_2_Float;
Unity_Subtract_float(_GradientNoise_32a9a6c418e44409a86a8238a6b4645c_Out_2_Float, float(0.5), _Subtract_3ec4867605194525898104109bc1e4a8_Out_2_Float);
float _Property_a52d213096ea41398ec0a409bf7ca764_Out_0_Float = _Wind_Strength;
float _Multiply_444d89a5d2344a9d97e110a245567e62_Out_2_Float;
Unity_Multiply_float_float(2, _Property_a52d213096ea41398ec0a409bf7ca764_Out_0_Float, _Multiply_444d89a5d2344a9d97e110a245567e62_Out_2_Float);
float _Multiply_6a79300791ae4467bc6c5489c0164afa_Out_2_Float;
Unity_Multiply_float_float(_Subtract_3ec4867605194525898104109bc1e4a8_Out_2_Float, _Multiply_444d89a5d2344a9d97e110a245567e62_Out_2_Float, _Multiply_6a79300791ae4467bc6c5489c0164afa_Out_2_Float);
float3 _Normalize_ac3890981ce548259c746c56bd0ca30f_Out_1_Vector3;
Unity_Normalize_float3(IN.ObjectSpacePosition, _Normalize_ac3890981ce548259c746c56bd0ca30f_Out_1_Vector3);
float _Split_ce6b6b6587294130b97b66c5ec3b64a6_R_1_Float = _Normalize_ac3890981ce548259c746c56bd0ca30f_Out_1_Vector3[0];
float _Split_ce6b6b6587294130b97b66c5ec3b64a6_G_2_Float = _Normalize_ac3890981ce548259c746c56bd0ca30f_Out_1_Vector3[1];
float _Split_ce6b6b6587294130b97b66c5ec3b64a6_B_3_Float = _Normalize_ac3890981ce548259c746c56bd0ca30f_Out_1_Vector3[2];
float _Split_ce6b6b6587294130b97b66c5ec3b64a6_A_4_Float = 0;
float _Multiply_453e85026c7c4cc8b7c4661ac5495b5f_Out_2_Float;
Unity_Multiply_float_float(_Multiply_6a79300791ae4467bc6c5489c0164afa_Out_2_Float, _Split_ce6b6b6587294130b97b66c5ec3b64a6_G_2_Float, _Multiply_453e85026c7c4cc8b7c4661ac5495b5f_Out_2_Float);
float3 _Add_526cc646ad64464ba782c997c6d8a2e7_Out_2_Vector3;
Unity_Add_float3((_Multiply_453e85026c7c4cc8b7c4661ac5495b5f_Out_2_Float.xxx), IN.AbsoluteWorldSpacePosition, _Add_526cc646ad64464ba782c997c6d8a2e7_Out_2_Vector3);
float3 _Transform_26034a9482244d0db0cde719848b8793_Out_1_Vector3;
_Transform_26034a9482244d0db0cde719848b8793_Out_1_Vector3 = TransformWorldToObject(_Add_526cc646ad64464ba782c997c6d8a2e7_Out_2_Vector3.xyz);
float _Split_35ccc240162a41a796dd70233f88757c_R_1_Float = IN.VertexColor[0];
float _Split_35ccc240162a41a796dd70233f88757c_G_2_Float = IN.VertexColor[1];
float _Split_35ccc240162a41a796dd70233f88757c_B_3_Float = IN.VertexColor[2];
float _Split_35ccc240162a41a796dd70233f88757c_A_4_Float = IN.VertexColor[3];
float4 _Combine_bef8410c8e24482da5d3364b77236292_RGBA_4_Vector4;
float3 _Combine_bef8410c8e24482da5d3364b77236292_RGB_5_Vector3;
float2 _Combine_bef8410c8e24482da5d3364b77236292_RG_6_Vector2;
Unity_Combine_float(_Split_35ccc240162a41a796dd70233f88757c_R_1_Float, _Split_35ccc240162a41a796dd70233f88757c_G_2_Float, float(0), float(0), _Combine_bef8410c8e24482da5d3364b77236292_RGBA_4_Vector4, _Combine_bef8410c8e24482da5d3364b77236292_RGB_5_Vector3, _Combine_bef8410c8e24482da5d3364b77236292_RG_6_Vector2);
float3 _Lerp_7a7657270ab14fb2acad87e06eb37455_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _Transform_26034a9482244d0db0cde719848b8793_Out_1_Vector3, _Combine_bef8410c8e24482da5d3364b77236292_RGB_5_Vector3, _Lerp_7a7657270ab14fb2acad87e06eb37455_Out_3_Vector3);
float _Property_33225d4ce071408bbef8e7871ca2d298_Out_0_Float = _Wind_Speed;
float _Multiply_f56338cdfe18474dbabd15aebc9c5c2d_Out_2_Float;
Unity_Multiply_float_float(10, _Property_33225d4ce071408bbef8e7871ca2d298_Out_0_Float, _Multiply_f56338cdfe18474dbabd15aebc9c5c2d_Out_2_Float);
float _Multiply_7c3d48cd1d9e4ba6aaee9091a8d8a492_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Multiply_f56338cdfe18474dbabd15aebc9c5c2d_Out_2_Float, _Multiply_7c3d48cd1d9e4ba6aaee9091a8d8a492_Out_2_Float);
float2 _TilingAndOffset_ec707a81514142459d08db9978d86cf5_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, float2 (1, 1), (_Multiply_7c3d48cd1d9e4ba6aaee9091a8d8a492_Out_2_Float.xx), _TilingAndOffset_ec707a81514142459d08db9978d86cf5_Out_3_Vector2);
float _Property_52c7fe0910e04984b1e473c0627107b2_Out_0_Float = _Wind_Variation;
float _Multiply_a9622f7a126646aaa969ee30098cf924_Out_2_Float;
Unity_Multiply_float_float(0.5, _Property_52c7fe0910e04984b1e473c0627107b2_Out_0_Float, _Multiply_a9622f7a126646aaa969ee30098cf924_Out_2_Float);
float _GradientNoise_d9ac409c304041c49e680e55eb4d2c81_Out_2_Float;
Unity_GradientNoise_Deterministic_float(_TilingAndOffset_ec707a81514142459d08db9978d86cf5_Out_3_Vector2, _Multiply_a9622f7a126646aaa969ee30098cf924_Out_2_Float, _GradientNoise_d9ac409c304041c49e680e55eb4d2c81_Out_2_Float);
float3 _Lerp_1b4b768f79f34ddaa538834a53b8e33b_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _Lerp_7a7657270ab14fb2acad87e06eb37455_Out_3_Vector3, (_GradientNoise_d9ac409c304041c49e680e55eb4d2c81_Out_2_Float.xxx), _Lerp_1b4b768f79f34ddaa538834a53b8e33b_Out_3_Vector3);
float3 _Property_413c2dab0db145e686fd951a3cf9b06d_Out_0_Vector3 = _Player_Position;
float3 _Subtract_16caad2935f64d4ca2a5c41c122c2899_Out_2_Vector3;
Unity_Subtract_float3(IN.AbsoluteWorldSpacePosition, _Property_413c2dab0db145e686fd951a3cf9b06d_Out_0_Vector3, _Subtract_16caad2935f64d4ca2a5c41c122c2899_Out_2_Vector3);
float _Split_dd517b5d363349dfbcc6a80463d0a69c_R_1_Float = _Subtract_16caad2935f64d4ca2a5c41c122c2899_Out_2_Vector3[0];
float _Split_dd517b5d363349dfbcc6a80463d0a69c_G_2_Float = _Subtract_16caad2935f64d4ca2a5c41c122c2899_Out_2_Vector3[1];
float _Split_dd517b5d363349dfbcc6a80463d0a69c_B_3_Float = _Subtract_16caad2935f64d4ca2a5c41c122c2899_Out_2_Vector3[2];
float _Split_dd517b5d363349dfbcc6a80463d0a69c_A_4_Float = 0;
float _Property_f88a2d673c09471cad200fa3c1b6ae30_Out_0_Float = _Bend_Strength;
float _Multiply_6c5ecb62197a4848b2fabec6f920ae09_Out_2_Float;
Unity_Multiply_float_float(7, _Property_f88a2d673c09471cad200fa3c1b6ae30_Out_0_Float, _Multiply_6c5ecb62197a4848b2fabec6f920ae09_Out_2_Float);
float _Subtract_fabc7232020f481398d5e1c34e958838_Out_2_Float;
Unity_Subtract_float(_Split_dd517b5d363349dfbcc6a80463d0a69c_G_2_Float, _Multiply_6c5ecb62197a4848b2fabec6f920ae09_Out_2_Float, _Subtract_fabc7232020f481398d5e1c34e958838_Out_2_Float);
float4 _Combine_e1e6724c2316432f9d0cefb980576bd6_RGBA_4_Vector4;
float3 _Combine_e1e6724c2316432f9d0cefb980576bd6_RGB_5_Vector3;
float2 _Combine_e1e6724c2316432f9d0cefb980576bd6_RG_6_Vector2;
Unity_Combine_float(_Split_dd517b5d363349dfbcc6a80463d0a69c_R_1_Float, _Subtract_fabc7232020f481398d5e1c34e958838_Out_2_Float, _Split_dd517b5d363349dfbcc6a80463d0a69c_B_3_Float, float(0), _Combine_e1e6724c2316432f9d0cefb980576bd6_RGBA_4_Vector4, _Combine_e1e6724c2316432f9d0cefb980576bd6_RGB_5_Vector3, _Combine_e1e6724c2316432f9d0cefb980576bd6_RG_6_Vector2);
float3 _Multiply_42e953d7da1c4e7fbe777341488bae83_Out_2_Vector3;
Unity_Multiply_float3_float3(_Combine_e1e6724c2316432f9d0cefb980576bd6_RGB_5_Vector3, (_Property_f88a2d673c09471cad200fa3c1b6ae30_Out_0_Float.xxx), _Multiply_42e953d7da1c4e7fbe777341488bae83_Out_2_Vector3);
float _Property_ad55f894067547979f44b3a570bf7d49_Out_0_Float = _Start_Bend_Range;
float _Multiply_a7265b1673054e0996cb483d2ac3010d_Out_2_Float;
Unity_Multiply_float_float(_Property_ad55f894067547979f44b3a570bf7d49_Out_0_Float, 1.5, _Multiply_a7265b1673054e0996cb483d2ac3010d_Out_2_Float);
float _Distance_41bd00a9f1be4373a1003dc1e109849c_Out_2_Float;
Unity_Distance_float3(_Property_413c2dab0db145e686fd951a3cf9b06d_Out_0_Vector3, IN.AbsoluteWorldSpacePosition, _Distance_41bd00a9f1be4373a1003dc1e109849c_Out_2_Float);
float _Clamp_61013f1b8a7946cabf0f461c9754f564_Out_3_Float;
Unity_Clamp_float(_Distance_41bd00a9f1be4373a1003dc1e109849c_Out_2_Float, float(0), _Multiply_a7265b1673054e0996cb483d2ac3010d_Out_2_Float, _Clamp_61013f1b8a7946cabf0f461c9754f564_Out_3_Float);
float _Subtract_6193cd2daf004574816768560ab361b4_Out_2_Float;
Unity_Subtract_float(_Multiply_a7265b1673054e0996cb483d2ac3010d_Out_2_Float, _Clamp_61013f1b8a7946cabf0f461c9754f564_Out_3_Float, _Subtract_6193cd2daf004574816768560ab361b4_Out_2_Float);
float3 _Multiply_49122dc502124ec0b62c5e8d9df0b640_Out_2_Vector3;
Unity_Multiply_float3_float3(_Multiply_42e953d7da1c4e7fbe777341488bae83_Out_2_Vector3, (_Subtract_6193cd2daf004574816768560ab361b4_Out_2_Float.xxx), _Multiply_49122dc502124ec0b62c5e8d9df0b640_Out_2_Vector3);
float3 _Add_edf7ac44af404fdbbe1d0c50b7bc8b0f_Out_2_Vector3;
Unity_Add_float3(IN.ObjectSpacePosition, _Multiply_49122dc502124ec0b62c5e8d9df0b640_Out_2_Vector3, _Add_edf7ac44af404fdbbe1d0c50b7bc8b0f_Out_2_Vector3);
float3 _Normalize_0901e7953f394045adc844d92565cfba_Out_1_Vector3;
Unity_Normalize_float3(IN.ObjectSpacePosition, _Normalize_0901e7953f394045adc844d92565cfba_Out_1_Vector3);
float _Split_c58f3da36534495d8f4633fe4c40a74c_R_1_Float = _Normalize_0901e7953f394045adc844d92565cfba_Out_1_Vector3[0];
float _Split_c58f3da36534495d8f4633fe4c40a74c_G_2_Float = _Normalize_0901e7953f394045adc844d92565cfba_Out_1_Vector3[1];
float _Split_c58f3da36534495d8f4633fe4c40a74c_B_3_Float = _Normalize_0901e7953f394045adc844d92565cfba_Out_1_Vector3[2];
float _Split_c58f3da36534495d8f4633fe4c40a74c_A_4_Float = 0;
float _OneMinus_5a83b2c5314e45b2a835abb20d70bd22_Out_1_Float;
Unity_OneMinus_float(_Split_c58f3da36534495d8f4633fe4c40a74c_G_2_Float, _OneMinus_5a83b2c5314e45b2a835abb20d70bd22_Out_1_Float);
float _Step_428aa74b40a6492ba1e5a63ecfe4b53e_Out_2_Float;
Unity_Step_float(_OneMinus_5a83b2c5314e45b2a835abb20d70bd22_Out_1_Float, float(1), _Step_428aa74b40a6492ba1e5a63ecfe4b53e_Out_2_Float);
float3 _Lerp_7404d23a49244d1a8ca1c221f25d981c_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _Add_edf7ac44af404fdbbe1d0c50b7bc8b0f_Out_2_Vector3, (_Step_428aa74b40a6492ba1e5a63ecfe4b53e_Out_2_Float.xxx), _Lerp_7404d23a49244d1a8ca1c221f25d981c_Out_3_Vector3);
float3 _Lerp_93b3b392cffa4e688d1b32f6298c4907_Out_3_Vector3;
Unity_Lerp_float3(_Lerp_1b4b768f79f34ddaa538834a53b8e33b_Out_3_Vector3, _Lerp_7404d23a49244d1a8ca1c221f25d981c_Out_3_Vector3, (_GradientNoise_d9ac409c304041c49e680e55eb4d2c81_Out_2_Float.xxx), _Lerp_93b3b392cffa4e688d1b32f6298c4907_Out_3_Vector3);
float3 _UPGFoliageContactPositionCustomFunction_4d51d42c561a43cc976f6db5b768a1c6_bentPositionOS_1_Vector3;
UPG_FoliageContactPosition_float(_Lerp_93b3b392cffa4e688d1b32f6298c4907_Out_3_Vector3, _UPGFoliageContactPositionCustomFunction_4d51d42c561a43cc976f6db5b768a1c6_bentPositionOS_1_Vector3);
description.Position = _UPGFoliageContactPositionCustomFunction_4d51d42c561a43cc976f6db5b768a1c6_bentPositionOS_1_Vector3;
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
float _Property_8b643d04e791417ea5e53348201c0ea6_Out_0_Boolean = _Custom_Color;
UnityTexture2D _Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Texture, sampler_Texture, _Texture_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D.tex, _Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D.samplerstate, _Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4, _Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_R_4_Float = _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4.r;
float _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_G_5_Float = _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4.g;
float _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_B_6_Float = _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4.b;
float _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_A_7_Float = _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4.a;
float4 _Property_7982990a264d4e2dbcf380e5300b2e9a_Out_0_Vector4 = _Bottom_Color;
float _Property_9a45de6268b24c2cbb15e019aec9d0a8_Out_0_Boolean = _Enable_Top_Color;
float4 _Property_c49d73f7afd34ab2876785e930e60382_Out_0_Vector4 = _Top_Color;
float4 _Branch_5e35ed072da74a36a2b8cab58fc4a5ba_Out_3_Vector4;
Unity_Branch_float4(_Property_9a45de6268b24c2cbb15e019aec9d0a8_Out_0_Boolean, _Property_c49d73f7afd34ab2876785e930e60382_Out_0_Vector4, _Property_7982990a264d4e2dbcf380e5300b2e9a_Out_0_Vector4, _Branch_5e35ed072da74a36a2b8cab58fc4a5ba_Out_3_Vector4);
float _Split_c37efc3b7dc1496597aacca8227df00e_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_c37efc3b7dc1496597aacca8227df00e_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_c37efc3b7dc1496597aacca8227df00e_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_c37efc3b7dc1496597aacca8227df00e_A_4_Float = 0;
float _Property_05ac7e14eb254977a8f86421bf9d54b2_Out_0_Float = _Blend_Height;
float _Multiply_4757f6b5ea5c447a87e4cc7e8f0be506_Out_2_Float;
Unity_Multiply_float_float(_Property_05ac7e14eb254977a8f86421bf9d54b2_Out_0_Float, 2, _Multiply_4757f6b5ea5c447a87e4cc7e8f0be506_Out_2_Float);
float _Multiply_494d37dbb2a44ddab3ff80336acce43b_Out_2_Float;
Unity_Multiply_float_float(_Split_c37efc3b7dc1496597aacca8227df00e_G_2_Float, _Multiply_4757f6b5ea5c447a87e4cc7e8f0be506_Out_2_Float, _Multiply_494d37dbb2a44ddab3ff80336acce43b_Out_2_Float);
float4 _UV_01e7dab03cc840b880067e9fcb6e2eb4_Out_0_Vector4 = IN.uv3;
float _Split_20e6d7d5918d4a7e8602a4c891473154_R_1_Float = _UV_01e7dab03cc840b880067e9fcb6e2eb4_Out_0_Vector4[0];
float _Split_20e6d7d5918d4a7e8602a4c891473154_G_2_Float = _UV_01e7dab03cc840b880067e9fcb6e2eb4_Out_0_Vector4[1];
float _Split_20e6d7d5918d4a7e8602a4c891473154_B_3_Float = _UV_01e7dab03cc840b880067e9fcb6e2eb4_Out_0_Vector4[2];
float _Split_20e6d7d5918d4a7e8602a4c891473154_A_4_Float = _UV_01e7dab03cc840b880067e9fcb6e2eb4_Out_0_Vector4[3];
float _Property_437c7bb5f9284c4e8114d28bc5622db1_Out_0_Float = _Blend_Height;
float _Multiply_932db43fdbd54ea2aa00741583b905df_Out_2_Float;
Unity_Multiply_float_float(_Property_437c7bb5f9284c4e8114d28bc5622db1_Out_0_Float, 5, _Multiply_932db43fdbd54ea2aa00741583b905df_Out_2_Float);
float _Multiply_852392faaedd4eee838a104fc74a9c2c_Out_2_Float;
Unity_Multiply_float_float(_Split_20e6d7d5918d4a7e8602a4c891473154_G_2_Float, _Multiply_932db43fdbd54ea2aa00741583b905df_Out_2_Float, _Multiply_852392faaedd4eee838a104fc74a9c2c_Out_2_Float);
#if defined(_COLOR_OVERLAY_VERTEX_POSITION)
float _ColorOverlay_0a0435a559c340b58c2ac6b4f740704f_Out_0_Float = _Multiply_494d37dbb2a44ddab3ff80336acce43b_Out_2_Float;
#else
float _ColorOverlay_0a0435a559c340b58c2ac6b4f740704f_Out_0_Float = _Multiply_852392faaedd4eee838a104fc74a9c2c_Out_2_Float;
#endif
float4 _Lerp_e82167542650450cae46c81096c7eb35_Out_3_Vector4;
Unity_Lerp_float4(_Property_7982990a264d4e2dbcf380e5300b2e9a_Out_0_Vector4, _Branch_5e35ed072da74a36a2b8cab58fc4a5ba_Out_3_Vector4, (_ColorOverlay_0a0435a559c340b58c2ac6b4f740704f_Out_0_Float.xxxx), _Lerp_e82167542650450cae46c81096c7eb35_Out_3_Vector4);
float3 _BakedGI_2f01eef6099b4badbf6e4fa34812d46c_Out_1_Vector3 = SHADERGRAPH_BAKED_GI(IN.WorldSpacePosition, IN.WorldSpaceNormal, IN.PixelPosition.xy, IN.uv1.xy, IN.uv2.xy, true);
float4 _SampleGradient_f13af0799e57479ab4f197dc8eb051f0_Out_2_Vector4;
Unity_SampleGradientV1_float(NewGradient(1, 2, 2, float4(0.6981132, 0.6981132, 0.6981132, 0.4588235),float4(1, 1, 1, 1),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0), float2(1, 0),float2(1, 1),float2(0, 0),float2(0, 0),float2(0, 0),float2(0, 0),float2(0, 0),float2(0, 0)), (_BakedGI_2f01eef6099b4badbf6e4fa34812d46c_Out_1_Vector3).x, _SampleGradient_f13af0799e57479ab4f197dc8eb051f0_Out_2_Vector4);
half3 _MainLightCustomFunction_5e0203c89a8c45fbb4222b62bf493a45_Direction_1_Vector3;
half3 _MainLightCustomFunction_5e0203c89a8c45fbb4222b62bf493a45_Color_2_Vector3;
half _MainLightCustomFunction_5e0203c89a8c45fbb4222b62bf493a45_DistanceAtten_3_Float;
half _MainLightCustomFunction_5e0203c89a8c45fbb4222b62bf493a45_ShadowAtten_4_Float;
MainLight_half(IN.WorldSpacePosition, _MainLightCustomFunction_5e0203c89a8c45fbb4222b62bf493a45_Direction_1_Vector3, _MainLightCustomFunction_5e0203c89a8c45fbb4222b62bf493a45_Color_2_Vector3, _MainLightCustomFunction_5e0203c89a8c45fbb4222b62bf493a45_DistanceAtten_3_Float, _MainLightCustomFunction_5e0203c89a8c45fbb4222b62bf493a45_ShadowAtten_4_Float);
float4 _SampleGradient_3a0a1463fe4a4a6692d7fa4e255d7c3a_Out_2_Vector4;
Unity_SampleGradientV1_float(NewGradient(1, 2, 2, float4(0.254717, 0.254717, 0.254717, 0.4558785),float4(1, 1, 1, 1),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0), float2(1, 0),float2(1, 1),float2(0, 0),float2(0, 0),float2(0, 0),float2(0, 0),float2(0, 0),float2(0, 0)), _MainLightCustomFunction_5e0203c89a8c45fbb4222b62bf493a45_ShadowAtten_4_Float, _SampleGradient_3a0a1463fe4a4a6692d7fa4e255d7c3a_Out_2_Vector4);
float4 _Multiply_bcc809cfe08a42839838a46b671101d2_Out_2_Vector4;
Unity_Multiply_float4_float4(_SampleGradient_f13af0799e57479ab4f197dc8eb051f0_Out_2_Vector4, _SampleGradient_3a0a1463fe4a4a6692d7fa4e255d7c3a_Out_2_Vector4, _Multiply_bcc809cfe08a42839838a46b671101d2_Out_2_Vector4);
float4 _SampleGradient_c0f24675e9d44702be0700da614645ba_Out_2_Vector4;
Unity_SampleGradientV1_float(NewGradient(1, 2, 2, float4(0.3867925, 0.3867925, 0.3867925, 0.4911726),float4(1, 1, 1, 1),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0), float2(1, 0),float2(1, 1),float2(0, 0),float2(0, 0),float2(0, 0),float2(0, 0),float2(0, 0),float2(0, 0)), _MainLightCustomFunction_5e0203c89a8c45fbb4222b62bf493a45_ShadowAtten_4_Float, _SampleGradient_c0f24675e9d44702be0700da614645ba_Out_2_Vector4);
float3 _BakedGI_bb2649c5aa9a4f17aa739bb15e26f960_Out_1_Vector3 = SHADERGRAPH_BAKED_GI(IN.WorldSpacePosition, IN.WorldSpaceNormal, IN.PixelPosition.xy, IN.uv1.xy, IN.uv2.xy, true);
float3 _Multiply_fb1104e4e9c34a879c179852a1ae32b6_Out_2_Vector3;
Unity_Multiply_float3_float3((_SampleGradient_c0f24675e9d44702be0700da614645ba_Out_2_Vector4.xyz), _BakedGI_bb2649c5aa9a4f17aa739bb15e26f960_Out_1_Vector3, _Multiply_fb1104e4e9c34a879c179852a1ae32b6_Out_2_Vector3);
float3 _Add_2e5b88ada4614963848a47def889a017_Out_2_Vector3;
Unity_Add_float3((_Multiply_bcc809cfe08a42839838a46b671101d2_Out_2_Vector4.xyz), _Multiply_fb1104e4e9c34a879c179852a1ae32b6_Out_2_Vector3, _Add_2e5b88ada4614963848a47def889a017_Out_2_Vector3);
float3 _Multiply_d9af4f78e0384e598b9b64ba704f86ed_Out_2_Vector3;
Unity_Multiply_float3_float3((_Lerp_e82167542650450cae46c81096c7eb35_Out_3_Vector4.xyz), _Add_2e5b88ada4614963848a47def889a017_Out_2_Vector3, _Multiply_d9af4f78e0384e598b9b64ba704f86ed_Out_2_Vector3);
float3 _Multiply_db27607157ad40e1bc8f503d6917a1e4_Out_2_Vector3;
Unity_Multiply_float3_float3((_SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4.xyz), _Multiply_d9af4f78e0384e598b9b64ba704f86ed_Out_2_Vector3, _Multiply_db27607157ad40e1bc8f503d6917a1e4_Out_2_Vector3);
float3 _Multiply_117a3293c8724edbbbfb3e0e208d0dd8_Out_2_Vector3;
Unity_Multiply_float3_float3((_SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4.xyz), _Add_2e5b88ada4614963848a47def889a017_Out_2_Vector3, _Multiply_117a3293c8724edbbbfb3e0e208d0dd8_Out_2_Vector3);
float3 _Branch_c35c890039964b6d84d86513b7a90a46_Out_3_Vector3;
Unity_Branch_float3(_Property_8b643d04e791417ea5e53348201c0ea6_Out_0_Boolean, _Multiply_db27607157ad40e1bc8f503d6917a1e4_Out_2_Vector3, _Multiply_117a3293c8724edbbbfb3e0e208d0dd8_Out_2_Vector3, _Branch_c35c890039964b6d84d86513b7a90a46_Out_3_Vector3);
float _Property_ae6b5992b5874ce1b905e7962780a6be_Out_0_Float = _Alpha_Cutoff;
surface.BaseColor = IsGammaSpace() ? float3(0, 0, 0) : SRGBToLinear(float3(0, 0, 0));
surface.NormalTS = IN.TangentSpaceNormal;
surface.Emission = _Branch_c35c890039964b6d84d86513b7a90a46_Out_3_Vector3;
surface.Metallic = float(0);
surface.Specular = IsGammaSpace() ? float3(0, 0, 0) : SRGBToLinear(float3(0, 0, 0));
surface.Smoothness = float(0);
surface.Occlusion = float(2);
surface.Alpha = _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_A_7_Float;
surface.AlphaClipThreshold = _Property_ae6b5992b5874ce1b905e7962780a6be_Out_0_Float;
float3 reactionUnusedColor = 0, reactionUnusedEmission = 0;
ClipVegetationFuel(IN.WorldSpacePosition);
ApplyVegetationReaction(IN.WorldSpacePosition,surface.BaseColor,surface.Emission);
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
    output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
    output.AbsoluteWorldSpacePosition =                 GetAbsolutePositionWS(TransformObjectToWorld(input.positionOS));
    output.uv0 =                                        input.uv0;
    output.VertexColor =                                input.color;
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


    output.WorldSpacePosition = input.positionWS;
    output.ObjectSpacePosition = TransformWorldToObject(input.positionWS);

    #if UNITY_UV_STARTS_AT_TOP
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #else
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #endif


    output.uv0 = input.texCoord0;
    output.uv1 = input.texCoord1;
    output.uv2 = input.texCoord2;
    output.uv3 = input.texCoord3;
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
#pragma shader_feature _ _SOFT_SHADOWS
#pragma shader_feature_local _COLOR_OVERLAY_VERTEX_POSITION _COLOR_OVERLAY_UV

#if defined(_SOFT_SHADOWS) && defined(_COLOR_OVERLAY_VERTEX_POSITION)
#define KEYWORD_PERMUTATION_0
#elif defined(_SOFT_SHADOWS) && defined(_COLOR_OVERLAY_UV)
#define KEYWORD_PERMUTATION_1
#elif defined(_COLOR_OVERLAY_VERTEX_POSITION)
#define KEYWORD_PERMUTATION_2
#else
#define KEYWORD_PERMUTATION_3
#endif


// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_TEXCOORD1
#define ATTRIBUTES_NEED_TEXCOORD2
#define ATTRIBUTES_NEED_TEXCOORD3
#define ATTRIBUTES_NEED_COLOR
#define GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_NORMAL_WS
#define VARYINGS_NEED_TANGENT_WS
#define VARYINGS_NEED_TEXCOORD0
#define VARYINGS_NEED_TEXCOORD1
#define VARYINGS_NEED_TEXCOORD2
#define VARYINGS_NEED_TEXCOORD3
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
 float4 uv3 : TEXCOORD3;
 float4 color : COLOR;
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
 float4 texCoord3;
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
 float3 WorldSpacePosition;
 float2 PixelPosition;
 float4 uv0;
 float4 uv1;
 float4 uv2;
 float4 uv3;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 WorldSpacePosition;
 float3 AbsoluteWorldSpacePosition;
 float4 uv0;
 float4 VertexColor;
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
 float4 texCoord1 : INTERP7;
 float4 texCoord2 : INTERP8;
 float4 texCoord3 : INTERP9;
 float4 fogFactorAndVertexLight : INTERP10;
 float3 positionWS : INTERP11;
 float3 normalWS : INTERP12;
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
output.texCoord1.xyzw = input.texCoord1;
output.texCoord2.xyzw = input.texCoord2;
output.texCoord3.xyzw = input.texCoord3;
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
output.texCoord1 = input.texCoord1.xyzw;
output.texCoord2 = input.texCoord2.xyzw;
output.texCoord3 = input.texCoord3.xyzw;
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

float4 _Texture_TexelSize;
float4 _Top_Color;
float _Custom_Color;
float4 _Bottom_Color;
float _Blend_Height;
float _Wind_Strength;
float _Wind_Speed;
float _Wind_Variation;
float2 _Wind_Direction;
float _Alpha_Cutoff;
float _Enable_Top_Color;
float _Start_Bend_Range;
float _Wave_Scale;
float _Bend_Strength;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Texture);
SAMPLER(sampler_Texture);
float3 _Player_Position;

// Graph Includes
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"
#include_with_pragmas "Assets/06.Shaders/Foliage/UPG_FoliageInteraction.hlsl"
#include_with_pragmas "Assets/ExternalAssets/Environment/Idyllic Fantasy Nature/Shader/CustomLighting.hlsl"

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

void Unity_Normalize_float2(float2 In, out float2 Out)
{
    Out = normalize(In);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_Add_float2(float2 A, float2 B, out float2 Out)
{
    Out = A + B;
}

float2 Unity_GradientNoise_Deterministic_Dir_float(float2 p)
{
float x; Hash_Tchou_2_1_float(p, x);
return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
}

void Unity_GradientNoise_Deterministic_float (float2 UV, float3 Scale, out float Out)
{
float2 p = UV * Scale.xy;
float2 ip = floor(p);
float2 fp = frac(p);
float d00 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip), fp);
float d01 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(0, 1)), fp - float2(0, 1));
float d10 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(1, 0)), fp - float2(1, 0));
float d11 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(1, 1)), fp - float2(1, 1));
fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
Out = lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5;
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Normalize_float3(float3 In, out float3 Out)
{
    Out = normalize(In);
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
{
    Out = lerp(A, B, T);
}

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
}

void Unity_Subtract_float3(float3 A, float3 B, out float3 Out)
{
    Out = A - B;
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Distance_float3(float3 A, float3 B, out float Out)
{
    Out = distance(A, B);
}

void Unity_Clamp_float(float In, float Min, float Max, out float Out)
{
    Out = clamp(In, Min, Max);
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
}

void Unity_Step_float(float Edge, float In, out float Out)
{
    Out = step(Edge, In);
}

void Unity_Branch_float4(float Predicate, float4 True, float4 False, out float4 Out)
{
    Out = Predicate ? True : False;
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_SampleGradientV1_float(Gradient Gradient, float Time, out float4 Out)
{
    // convert to OkLab if we need perceptual color space.
    float3 color = lerp(Gradient.colors[0].rgb, LinearToOklab(Gradient.colors[0].rgb), Gradient.type == 2);

    [unroll]
    for (int c = 1; c < Gradient.colorsLength; c++)
    {
        float colorPos = saturate((Time - Gradient.colors[c - 1].w) / (Gradient.colors[c].w - Gradient.colors[c - 1].w)) * step(c, Gradient.colorsLength - 1);
        float3 color2 = lerp(Gradient.colors[c].rgb, LinearToOklab(Gradient.colors[c].rgb), Gradient.type == 2);
        color = lerp(color, color2, lerp(colorPos, step(0.01, colorPos), Gradient.type % 2)); // grad.type == 1 is fixed, 0 and 2 are blends.
    }
    color = lerp(color, OklabToLinear(color), Gradient.type == 2);

#ifdef UNITY_COLORSPACE_GAMMA
    color = LinearToSRGB(color);
#endif

    float alpha = Gradient.alphas[0].x;
    [unroll]
    for (int a = 1; a < Gradient.alphasLength; a++)
    {
        float alphaPos = saturate((Time - Gradient.alphas[a - 1].y) / (Gradient.alphas[a].y - Gradient.alphas[a - 1].y)) * step(a, Gradient.alphasLength - 1);
        alpha = lerp(alpha, Gradient.alphas[a].x, lerp(alphaPos, step(0.01, alphaPos), Gradient.type % 2));
    }

    Out = float4(color, alpha);
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
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
float _Split_f9cfb9a555e949379da9876f8d3844b2_R_1_Float = IN.WorldSpacePosition[0];
float _Split_f9cfb9a555e949379da9876f8d3844b2_G_2_Float = IN.WorldSpacePosition[1];
float _Split_f9cfb9a555e949379da9876f8d3844b2_B_3_Float = IN.WorldSpacePosition[2];
float _Split_f9cfb9a555e949379da9876f8d3844b2_A_4_Float = 0;
float2 _Vector2_feeecec8507c4619844c3ca15ea244be_Out_0_Vector2 = float2(_Split_f9cfb9a555e949379da9876f8d3844b2_R_1_Float, _Split_f9cfb9a555e949379da9876f8d3844b2_B_3_Float);
float2 _Property_186d136719274f03b9464e4e1d37a4e8_Out_0_Vector2 = _Wind_Direction;
float2 _Normalize_fb934df0b0c946039db4091949a06211_Out_1_Vector2;
Unity_Normalize_float2(_Property_186d136719274f03b9464e4e1d37a4e8_Out_0_Vector2, _Normalize_fb934df0b0c946039db4091949a06211_Out_1_Vector2);
float _Property_a98f7a88c8ac40109941c7f4f4211495_Out_0_Float = _Wind_Speed;
float _Multiply_880934a6c2a44f658fe3c3b3ed7cccac_Out_2_Float;
Unity_Multiply_float_float(10, _Property_a98f7a88c8ac40109941c7f4f4211495_Out_0_Float, _Multiply_880934a6c2a44f658fe3c3b3ed7cccac_Out_2_Float);
float2 _Multiply_892e7af0e8e54f869ff77c70575af5fc_Out_2_Vector2;
Unity_Multiply_float2_float2(_Normalize_fb934df0b0c946039db4091949a06211_Out_1_Vector2, (_Multiply_880934a6c2a44f658fe3c3b3ed7cccac_Out_2_Float.xx), _Multiply_892e7af0e8e54f869ff77c70575af5fc_Out_2_Vector2);
float2 _Multiply_d96cf1c14ac74322b4498e1849526399_Out_2_Vector2;
Unity_Multiply_float2_float2(_Multiply_892e7af0e8e54f869ff77c70575af5fc_Out_2_Vector2, (IN.TimeParameters.x.xx), _Multiply_d96cf1c14ac74322b4498e1849526399_Out_2_Vector2);
float2 _Add_c52cdcdd15b74d9a963511973a04621c_Out_2_Vector2;
Unity_Add_float2(_Vector2_feeecec8507c4619844c3ca15ea244be_Out_0_Vector2, _Multiply_d96cf1c14ac74322b4498e1849526399_Out_2_Vector2, _Add_c52cdcdd15b74d9a963511973a04621c_Out_2_Vector2);
float _Property_14515a2f6d834bd99002a2ccf491767f_Out_0_Float = _Wave_Scale;
float _Multiply_bbdcafe2ee654e3d8e4df25d9c66103c_Out_2_Float;
Unity_Multiply_float_float(3, _Property_14515a2f6d834bd99002a2ccf491767f_Out_0_Float, _Multiply_bbdcafe2ee654e3d8e4df25d9c66103c_Out_2_Float);
float _GradientNoise_32a9a6c418e44409a86a8238a6b4645c_Out_2_Float;
Unity_GradientNoise_Deterministic_float(_Add_c52cdcdd15b74d9a963511973a04621c_Out_2_Vector2, _Multiply_bbdcafe2ee654e3d8e4df25d9c66103c_Out_2_Float, _GradientNoise_32a9a6c418e44409a86a8238a6b4645c_Out_2_Float);
float _Subtract_3ec4867605194525898104109bc1e4a8_Out_2_Float;
Unity_Subtract_float(_GradientNoise_32a9a6c418e44409a86a8238a6b4645c_Out_2_Float, float(0.5), _Subtract_3ec4867605194525898104109bc1e4a8_Out_2_Float);
float _Property_a52d213096ea41398ec0a409bf7ca764_Out_0_Float = _Wind_Strength;
float _Multiply_444d89a5d2344a9d97e110a245567e62_Out_2_Float;
Unity_Multiply_float_float(2, _Property_a52d213096ea41398ec0a409bf7ca764_Out_0_Float, _Multiply_444d89a5d2344a9d97e110a245567e62_Out_2_Float);
float _Multiply_6a79300791ae4467bc6c5489c0164afa_Out_2_Float;
Unity_Multiply_float_float(_Subtract_3ec4867605194525898104109bc1e4a8_Out_2_Float, _Multiply_444d89a5d2344a9d97e110a245567e62_Out_2_Float, _Multiply_6a79300791ae4467bc6c5489c0164afa_Out_2_Float);
float3 _Normalize_ac3890981ce548259c746c56bd0ca30f_Out_1_Vector3;
Unity_Normalize_float3(IN.ObjectSpacePosition, _Normalize_ac3890981ce548259c746c56bd0ca30f_Out_1_Vector3);
float _Split_ce6b6b6587294130b97b66c5ec3b64a6_R_1_Float = _Normalize_ac3890981ce548259c746c56bd0ca30f_Out_1_Vector3[0];
float _Split_ce6b6b6587294130b97b66c5ec3b64a6_G_2_Float = _Normalize_ac3890981ce548259c746c56bd0ca30f_Out_1_Vector3[1];
float _Split_ce6b6b6587294130b97b66c5ec3b64a6_B_3_Float = _Normalize_ac3890981ce548259c746c56bd0ca30f_Out_1_Vector3[2];
float _Split_ce6b6b6587294130b97b66c5ec3b64a6_A_4_Float = 0;
float _Multiply_453e85026c7c4cc8b7c4661ac5495b5f_Out_2_Float;
Unity_Multiply_float_float(_Multiply_6a79300791ae4467bc6c5489c0164afa_Out_2_Float, _Split_ce6b6b6587294130b97b66c5ec3b64a6_G_2_Float, _Multiply_453e85026c7c4cc8b7c4661ac5495b5f_Out_2_Float);
float3 _Add_526cc646ad64464ba782c997c6d8a2e7_Out_2_Vector3;
Unity_Add_float3((_Multiply_453e85026c7c4cc8b7c4661ac5495b5f_Out_2_Float.xxx), IN.AbsoluteWorldSpacePosition, _Add_526cc646ad64464ba782c997c6d8a2e7_Out_2_Vector3);
float3 _Transform_26034a9482244d0db0cde719848b8793_Out_1_Vector3;
_Transform_26034a9482244d0db0cde719848b8793_Out_1_Vector3 = TransformWorldToObject(_Add_526cc646ad64464ba782c997c6d8a2e7_Out_2_Vector3.xyz);
float _Split_35ccc240162a41a796dd70233f88757c_R_1_Float = IN.VertexColor[0];
float _Split_35ccc240162a41a796dd70233f88757c_G_2_Float = IN.VertexColor[1];
float _Split_35ccc240162a41a796dd70233f88757c_B_3_Float = IN.VertexColor[2];
float _Split_35ccc240162a41a796dd70233f88757c_A_4_Float = IN.VertexColor[3];
float4 _Combine_bef8410c8e24482da5d3364b77236292_RGBA_4_Vector4;
float3 _Combine_bef8410c8e24482da5d3364b77236292_RGB_5_Vector3;
float2 _Combine_bef8410c8e24482da5d3364b77236292_RG_6_Vector2;
Unity_Combine_float(_Split_35ccc240162a41a796dd70233f88757c_R_1_Float, _Split_35ccc240162a41a796dd70233f88757c_G_2_Float, float(0), float(0), _Combine_bef8410c8e24482da5d3364b77236292_RGBA_4_Vector4, _Combine_bef8410c8e24482da5d3364b77236292_RGB_5_Vector3, _Combine_bef8410c8e24482da5d3364b77236292_RG_6_Vector2);
float3 _Lerp_7a7657270ab14fb2acad87e06eb37455_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _Transform_26034a9482244d0db0cde719848b8793_Out_1_Vector3, _Combine_bef8410c8e24482da5d3364b77236292_RGB_5_Vector3, _Lerp_7a7657270ab14fb2acad87e06eb37455_Out_3_Vector3);
float _Property_33225d4ce071408bbef8e7871ca2d298_Out_0_Float = _Wind_Speed;
float _Multiply_f56338cdfe18474dbabd15aebc9c5c2d_Out_2_Float;
Unity_Multiply_float_float(10, _Property_33225d4ce071408bbef8e7871ca2d298_Out_0_Float, _Multiply_f56338cdfe18474dbabd15aebc9c5c2d_Out_2_Float);
float _Multiply_7c3d48cd1d9e4ba6aaee9091a8d8a492_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Multiply_f56338cdfe18474dbabd15aebc9c5c2d_Out_2_Float, _Multiply_7c3d48cd1d9e4ba6aaee9091a8d8a492_Out_2_Float);
float2 _TilingAndOffset_ec707a81514142459d08db9978d86cf5_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, float2 (1, 1), (_Multiply_7c3d48cd1d9e4ba6aaee9091a8d8a492_Out_2_Float.xx), _TilingAndOffset_ec707a81514142459d08db9978d86cf5_Out_3_Vector2);
float _Property_52c7fe0910e04984b1e473c0627107b2_Out_0_Float = _Wind_Variation;
float _Multiply_a9622f7a126646aaa969ee30098cf924_Out_2_Float;
Unity_Multiply_float_float(0.5, _Property_52c7fe0910e04984b1e473c0627107b2_Out_0_Float, _Multiply_a9622f7a126646aaa969ee30098cf924_Out_2_Float);
float _GradientNoise_d9ac409c304041c49e680e55eb4d2c81_Out_2_Float;
Unity_GradientNoise_Deterministic_float(_TilingAndOffset_ec707a81514142459d08db9978d86cf5_Out_3_Vector2, _Multiply_a9622f7a126646aaa969ee30098cf924_Out_2_Float, _GradientNoise_d9ac409c304041c49e680e55eb4d2c81_Out_2_Float);
float3 _Lerp_1b4b768f79f34ddaa538834a53b8e33b_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _Lerp_7a7657270ab14fb2acad87e06eb37455_Out_3_Vector3, (_GradientNoise_d9ac409c304041c49e680e55eb4d2c81_Out_2_Float.xxx), _Lerp_1b4b768f79f34ddaa538834a53b8e33b_Out_3_Vector3);
float3 _Property_413c2dab0db145e686fd951a3cf9b06d_Out_0_Vector3 = _Player_Position;
float3 _Subtract_16caad2935f64d4ca2a5c41c122c2899_Out_2_Vector3;
Unity_Subtract_float3(IN.AbsoluteWorldSpacePosition, _Property_413c2dab0db145e686fd951a3cf9b06d_Out_0_Vector3, _Subtract_16caad2935f64d4ca2a5c41c122c2899_Out_2_Vector3);
float _Split_dd517b5d363349dfbcc6a80463d0a69c_R_1_Float = _Subtract_16caad2935f64d4ca2a5c41c122c2899_Out_2_Vector3[0];
float _Split_dd517b5d363349dfbcc6a80463d0a69c_G_2_Float = _Subtract_16caad2935f64d4ca2a5c41c122c2899_Out_2_Vector3[1];
float _Split_dd517b5d363349dfbcc6a80463d0a69c_B_3_Float = _Subtract_16caad2935f64d4ca2a5c41c122c2899_Out_2_Vector3[2];
float _Split_dd517b5d363349dfbcc6a80463d0a69c_A_4_Float = 0;
float _Property_f88a2d673c09471cad200fa3c1b6ae30_Out_0_Float = _Bend_Strength;
float _Multiply_6c5ecb62197a4848b2fabec6f920ae09_Out_2_Float;
Unity_Multiply_float_float(7, _Property_f88a2d673c09471cad200fa3c1b6ae30_Out_0_Float, _Multiply_6c5ecb62197a4848b2fabec6f920ae09_Out_2_Float);
float _Subtract_fabc7232020f481398d5e1c34e958838_Out_2_Float;
Unity_Subtract_float(_Split_dd517b5d363349dfbcc6a80463d0a69c_G_2_Float, _Multiply_6c5ecb62197a4848b2fabec6f920ae09_Out_2_Float, _Subtract_fabc7232020f481398d5e1c34e958838_Out_2_Float);
float4 _Combine_e1e6724c2316432f9d0cefb980576bd6_RGBA_4_Vector4;
float3 _Combine_e1e6724c2316432f9d0cefb980576bd6_RGB_5_Vector3;
float2 _Combine_e1e6724c2316432f9d0cefb980576bd6_RG_6_Vector2;
Unity_Combine_float(_Split_dd517b5d363349dfbcc6a80463d0a69c_R_1_Float, _Subtract_fabc7232020f481398d5e1c34e958838_Out_2_Float, _Split_dd517b5d363349dfbcc6a80463d0a69c_B_3_Float, float(0), _Combine_e1e6724c2316432f9d0cefb980576bd6_RGBA_4_Vector4, _Combine_e1e6724c2316432f9d0cefb980576bd6_RGB_5_Vector3, _Combine_e1e6724c2316432f9d0cefb980576bd6_RG_6_Vector2);
float3 _Multiply_42e953d7da1c4e7fbe777341488bae83_Out_2_Vector3;
Unity_Multiply_float3_float3(_Combine_e1e6724c2316432f9d0cefb980576bd6_RGB_5_Vector3, (_Property_f88a2d673c09471cad200fa3c1b6ae30_Out_0_Float.xxx), _Multiply_42e953d7da1c4e7fbe777341488bae83_Out_2_Vector3);
float _Property_ad55f894067547979f44b3a570bf7d49_Out_0_Float = _Start_Bend_Range;
float _Multiply_a7265b1673054e0996cb483d2ac3010d_Out_2_Float;
Unity_Multiply_float_float(_Property_ad55f894067547979f44b3a570bf7d49_Out_0_Float, 1.5, _Multiply_a7265b1673054e0996cb483d2ac3010d_Out_2_Float);
float _Distance_41bd00a9f1be4373a1003dc1e109849c_Out_2_Float;
Unity_Distance_float3(_Property_413c2dab0db145e686fd951a3cf9b06d_Out_0_Vector3, IN.AbsoluteWorldSpacePosition, _Distance_41bd00a9f1be4373a1003dc1e109849c_Out_2_Float);
float _Clamp_61013f1b8a7946cabf0f461c9754f564_Out_3_Float;
Unity_Clamp_float(_Distance_41bd00a9f1be4373a1003dc1e109849c_Out_2_Float, float(0), _Multiply_a7265b1673054e0996cb483d2ac3010d_Out_2_Float, _Clamp_61013f1b8a7946cabf0f461c9754f564_Out_3_Float);
float _Subtract_6193cd2daf004574816768560ab361b4_Out_2_Float;
Unity_Subtract_float(_Multiply_a7265b1673054e0996cb483d2ac3010d_Out_2_Float, _Clamp_61013f1b8a7946cabf0f461c9754f564_Out_3_Float, _Subtract_6193cd2daf004574816768560ab361b4_Out_2_Float);
float3 _Multiply_49122dc502124ec0b62c5e8d9df0b640_Out_2_Vector3;
Unity_Multiply_float3_float3(_Multiply_42e953d7da1c4e7fbe777341488bae83_Out_2_Vector3, (_Subtract_6193cd2daf004574816768560ab361b4_Out_2_Float.xxx), _Multiply_49122dc502124ec0b62c5e8d9df0b640_Out_2_Vector3);
float3 _Add_edf7ac44af404fdbbe1d0c50b7bc8b0f_Out_2_Vector3;
Unity_Add_float3(IN.ObjectSpacePosition, _Multiply_49122dc502124ec0b62c5e8d9df0b640_Out_2_Vector3, _Add_edf7ac44af404fdbbe1d0c50b7bc8b0f_Out_2_Vector3);
float3 _Normalize_0901e7953f394045adc844d92565cfba_Out_1_Vector3;
Unity_Normalize_float3(IN.ObjectSpacePosition, _Normalize_0901e7953f394045adc844d92565cfba_Out_1_Vector3);
float _Split_c58f3da36534495d8f4633fe4c40a74c_R_1_Float = _Normalize_0901e7953f394045adc844d92565cfba_Out_1_Vector3[0];
float _Split_c58f3da36534495d8f4633fe4c40a74c_G_2_Float = _Normalize_0901e7953f394045adc844d92565cfba_Out_1_Vector3[1];
float _Split_c58f3da36534495d8f4633fe4c40a74c_B_3_Float = _Normalize_0901e7953f394045adc844d92565cfba_Out_1_Vector3[2];
float _Split_c58f3da36534495d8f4633fe4c40a74c_A_4_Float = 0;
float _OneMinus_5a83b2c5314e45b2a835abb20d70bd22_Out_1_Float;
Unity_OneMinus_float(_Split_c58f3da36534495d8f4633fe4c40a74c_G_2_Float, _OneMinus_5a83b2c5314e45b2a835abb20d70bd22_Out_1_Float);
float _Step_428aa74b40a6492ba1e5a63ecfe4b53e_Out_2_Float;
Unity_Step_float(_OneMinus_5a83b2c5314e45b2a835abb20d70bd22_Out_1_Float, float(1), _Step_428aa74b40a6492ba1e5a63ecfe4b53e_Out_2_Float);
float3 _Lerp_7404d23a49244d1a8ca1c221f25d981c_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _Add_edf7ac44af404fdbbe1d0c50b7bc8b0f_Out_2_Vector3, (_Step_428aa74b40a6492ba1e5a63ecfe4b53e_Out_2_Float.xxx), _Lerp_7404d23a49244d1a8ca1c221f25d981c_Out_3_Vector3);
float3 _Lerp_93b3b392cffa4e688d1b32f6298c4907_Out_3_Vector3;
Unity_Lerp_float3(_Lerp_1b4b768f79f34ddaa538834a53b8e33b_Out_3_Vector3, _Lerp_7404d23a49244d1a8ca1c221f25d981c_Out_3_Vector3, (_GradientNoise_d9ac409c304041c49e680e55eb4d2c81_Out_2_Float.xxx), _Lerp_93b3b392cffa4e688d1b32f6298c4907_Out_3_Vector3);
float3 _UPGFoliageContactPositionCustomFunction_4d51d42c561a43cc976f6db5b768a1c6_bentPositionOS_1_Vector3;
UPG_FoliageContactPosition_float(_Lerp_93b3b392cffa4e688d1b32f6298c4907_Out_3_Vector3, _UPGFoliageContactPositionCustomFunction_4d51d42c561a43cc976f6db5b768a1c6_bentPositionOS_1_Vector3);
description.Position = _UPGFoliageContactPositionCustomFunction_4d51d42c561a43cc976f6db5b768a1c6_bentPositionOS_1_Vector3;
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
float _Property_8b643d04e791417ea5e53348201c0ea6_Out_0_Boolean = _Custom_Color;
UnityTexture2D _Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Texture, sampler_Texture, _Texture_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D.tex, _Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D.samplerstate, _Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4, _Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_R_4_Float = _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4.r;
float _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_G_5_Float = _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4.g;
float _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_B_6_Float = _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4.b;
float _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_A_7_Float = _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4.a;
float4 _Property_7982990a264d4e2dbcf380e5300b2e9a_Out_0_Vector4 = _Bottom_Color;
float _Property_9a45de6268b24c2cbb15e019aec9d0a8_Out_0_Boolean = _Enable_Top_Color;
float4 _Property_c49d73f7afd34ab2876785e930e60382_Out_0_Vector4 = _Top_Color;
float4 _Branch_5e35ed072da74a36a2b8cab58fc4a5ba_Out_3_Vector4;
Unity_Branch_float4(_Property_9a45de6268b24c2cbb15e019aec9d0a8_Out_0_Boolean, _Property_c49d73f7afd34ab2876785e930e60382_Out_0_Vector4, _Property_7982990a264d4e2dbcf380e5300b2e9a_Out_0_Vector4, _Branch_5e35ed072da74a36a2b8cab58fc4a5ba_Out_3_Vector4);
float _Split_c37efc3b7dc1496597aacca8227df00e_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_c37efc3b7dc1496597aacca8227df00e_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_c37efc3b7dc1496597aacca8227df00e_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_c37efc3b7dc1496597aacca8227df00e_A_4_Float = 0;
float _Property_05ac7e14eb254977a8f86421bf9d54b2_Out_0_Float = _Blend_Height;
float _Multiply_4757f6b5ea5c447a87e4cc7e8f0be506_Out_2_Float;
Unity_Multiply_float_float(_Property_05ac7e14eb254977a8f86421bf9d54b2_Out_0_Float, 2, _Multiply_4757f6b5ea5c447a87e4cc7e8f0be506_Out_2_Float);
float _Multiply_494d37dbb2a44ddab3ff80336acce43b_Out_2_Float;
Unity_Multiply_float_float(_Split_c37efc3b7dc1496597aacca8227df00e_G_2_Float, _Multiply_4757f6b5ea5c447a87e4cc7e8f0be506_Out_2_Float, _Multiply_494d37dbb2a44ddab3ff80336acce43b_Out_2_Float);
float4 _UV_01e7dab03cc840b880067e9fcb6e2eb4_Out_0_Vector4 = IN.uv3;
float _Split_20e6d7d5918d4a7e8602a4c891473154_R_1_Float = _UV_01e7dab03cc840b880067e9fcb6e2eb4_Out_0_Vector4[0];
float _Split_20e6d7d5918d4a7e8602a4c891473154_G_2_Float = _UV_01e7dab03cc840b880067e9fcb6e2eb4_Out_0_Vector4[1];
float _Split_20e6d7d5918d4a7e8602a4c891473154_B_3_Float = _UV_01e7dab03cc840b880067e9fcb6e2eb4_Out_0_Vector4[2];
float _Split_20e6d7d5918d4a7e8602a4c891473154_A_4_Float = _UV_01e7dab03cc840b880067e9fcb6e2eb4_Out_0_Vector4[3];
float _Property_437c7bb5f9284c4e8114d28bc5622db1_Out_0_Float = _Blend_Height;
float _Multiply_932db43fdbd54ea2aa00741583b905df_Out_2_Float;
Unity_Multiply_float_float(_Property_437c7bb5f9284c4e8114d28bc5622db1_Out_0_Float, 5, _Multiply_932db43fdbd54ea2aa00741583b905df_Out_2_Float);
float _Multiply_852392faaedd4eee838a104fc74a9c2c_Out_2_Float;
Unity_Multiply_float_float(_Split_20e6d7d5918d4a7e8602a4c891473154_G_2_Float, _Multiply_932db43fdbd54ea2aa00741583b905df_Out_2_Float, _Multiply_852392faaedd4eee838a104fc74a9c2c_Out_2_Float);
#if defined(_COLOR_OVERLAY_VERTEX_POSITION)
float _ColorOverlay_0a0435a559c340b58c2ac6b4f740704f_Out_0_Float = _Multiply_494d37dbb2a44ddab3ff80336acce43b_Out_2_Float;
#else
float _ColorOverlay_0a0435a559c340b58c2ac6b4f740704f_Out_0_Float = _Multiply_852392faaedd4eee838a104fc74a9c2c_Out_2_Float;
#endif
float4 _Lerp_e82167542650450cae46c81096c7eb35_Out_3_Vector4;
Unity_Lerp_float4(_Property_7982990a264d4e2dbcf380e5300b2e9a_Out_0_Vector4, _Branch_5e35ed072da74a36a2b8cab58fc4a5ba_Out_3_Vector4, (_ColorOverlay_0a0435a559c340b58c2ac6b4f740704f_Out_0_Float.xxxx), _Lerp_e82167542650450cae46c81096c7eb35_Out_3_Vector4);
float3 _BakedGI_2f01eef6099b4badbf6e4fa34812d46c_Out_1_Vector3 = SHADERGRAPH_BAKED_GI(IN.WorldSpacePosition, IN.WorldSpaceNormal, IN.PixelPosition.xy, IN.uv1.xy, IN.uv2.xy, true);
float4 _SampleGradient_f13af0799e57479ab4f197dc8eb051f0_Out_2_Vector4;
Unity_SampleGradientV1_float(NewGradient(1, 2, 2, float4(0.6981132, 0.6981132, 0.6981132, 0.4588235),float4(1, 1, 1, 1),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0), float2(1, 0),float2(1, 1),float2(0, 0),float2(0, 0),float2(0, 0),float2(0, 0),float2(0, 0),float2(0, 0)), (_BakedGI_2f01eef6099b4badbf6e4fa34812d46c_Out_1_Vector3).x, _SampleGradient_f13af0799e57479ab4f197dc8eb051f0_Out_2_Vector4);
half3 _MainLightCustomFunction_5e0203c89a8c45fbb4222b62bf493a45_Direction_1_Vector3;
half3 _MainLightCustomFunction_5e0203c89a8c45fbb4222b62bf493a45_Color_2_Vector3;
half _MainLightCustomFunction_5e0203c89a8c45fbb4222b62bf493a45_DistanceAtten_3_Float;
half _MainLightCustomFunction_5e0203c89a8c45fbb4222b62bf493a45_ShadowAtten_4_Float;
MainLight_half(IN.WorldSpacePosition, _MainLightCustomFunction_5e0203c89a8c45fbb4222b62bf493a45_Direction_1_Vector3, _MainLightCustomFunction_5e0203c89a8c45fbb4222b62bf493a45_Color_2_Vector3, _MainLightCustomFunction_5e0203c89a8c45fbb4222b62bf493a45_DistanceAtten_3_Float, _MainLightCustomFunction_5e0203c89a8c45fbb4222b62bf493a45_ShadowAtten_4_Float);
float4 _SampleGradient_3a0a1463fe4a4a6692d7fa4e255d7c3a_Out_2_Vector4;
Unity_SampleGradientV1_float(NewGradient(1, 2, 2, float4(0.254717, 0.254717, 0.254717, 0.4558785),float4(1, 1, 1, 1),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0), float2(1, 0),float2(1, 1),float2(0, 0),float2(0, 0),float2(0, 0),float2(0, 0),float2(0, 0),float2(0, 0)), _MainLightCustomFunction_5e0203c89a8c45fbb4222b62bf493a45_ShadowAtten_4_Float, _SampleGradient_3a0a1463fe4a4a6692d7fa4e255d7c3a_Out_2_Vector4);
float4 _Multiply_bcc809cfe08a42839838a46b671101d2_Out_2_Vector4;
Unity_Multiply_float4_float4(_SampleGradient_f13af0799e57479ab4f197dc8eb051f0_Out_2_Vector4, _SampleGradient_3a0a1463fe4a4a6692d7fa4e255d7c3a_Out_2_Vector4, _Multiply_bcc809cfe08a42839838a46b671101d2_Out_2_Vector4);
float4 _SampleGradient_c0f24675e9d44702be0700da614645ba_Out_2_Vector4;
Unity_SampleGradientV1_float(NewGradient(1, 2, 2, float4(0.3867925, 0.3867925, 0.3867925, 0.4911726),float4(1, 1, 1, 1),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0), float2(1, 0),float2(1, 1),float2(0, 0),float2(0, 0),float2(0, 0),float2(0, 0),float2(0, 0),float2(0, 0)), _MainLightCustomFunction_5e0203c89a8c45fbb4222b62bf493a45_ShadowAtten_4_Float, _SampleGradient_c0f24675e9d44702be0700da614645ba_Out_2_Vector4);
float3 _BakedGI_bb2649c5aa9a4f17aa739bb15e26f960_Out_1_Vector3 = SHADERGRAPH_BAKED_GI(IN.WorldSpacePosition, IN.WorldSpaceNormal, IN.PixelPosition.xy, IN.uv1.xy, IN.uv2.xy, true);
float3 _Multiply_fb1104e4e9c34a879c179852a1ae32b6_Out_2_Vector3;
Unity_Multiply_float3_float3((_SampleGradient_c0f24675e9d44702be0700da614645ba_Out_2_Vector4.xyz), _BakedGI_bb2649c5aa9a4f17aa739bb15e26f960_Out_1_Vector3, _Multiply_fb1104e4e9c34a879c179852a1ae32b6_Out_2_Vector3);
float3 _Add_2e5b88ada4614963848a47def889a017_Out_2_Vector3;
Unity_Add_float3((_Multiply_bcc809cfe08a42839838a46b671101d2_Out_2_Vector4.xyz), _Multiply_fb1104e4e9c34a879c179852a1ae32b6_Out_2_Vector3, _Add_2e5b88ada4614963848a47def889a017_Out_2_Vector3);
float3 _Multiply_d9af4f78e0384e598b9b64ba704f86ed_Out_2_Vector3;
Unity_Multiply_float3_float3((_Lerp_e82167542650450cae46c81096c7eb35_Out_3_Vector4.xyz), _Add_2e5b88ada4614963848a47def889a017_Out_2_Vector3, _Multiply_d9af4f78e0384e598b9b64ba704f86ed_Out_2_Vector3);
float3 _Multiply_db27607157ad40e1bc8f503d6917a1e4_Out_2_Vector3;
Unity_Multiply_float3_float3((_SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4.xyz), _Multiply_d9af4f78e0384e598b9b64ba704f86ed_Out_2_Vector3, _Multiply_db27607157ad40e1bc8f503d6917a1e4_Out_2_Vector3);
float3 _Multiply_117a3293c8724edbbbfb3e0e208d0dd8_Out_2_Vector3;
Unity_Multiply_float3_float3((_SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4.xyz), _Add_2e5b88ada4614963848a47def889a017_Out_2_Vector3, _Multiply_117a3293c8724edbbbfb3e0e208d0dd8_Out_2_Vector3);
float3 _Branch_c35c890039964b6d84d86513b7a90a46_Out_3_Vector3;
Unity_Branch_float3(_Property_8b643d04e791417ea5e53348201c0ea6_Out_0_Boolean, _Multiply_db27607157ad40e1bc8f503d6917a1e4_Out_2_Vector3, _Multiply_117a3293c8724edbbbfb3e0e208d0dd8_Out_2_Vector3, _Branch_c35c890039964b6d84d86513b7a90a46_Out_3_Vector3);
float _Property_ae6b5992b5874ce1b905e7962780a6be_Out_0_Float = _Alpha_Cutoff;
surface.BaseColor = IsGammaSpace() ? float3(0, 0, 0) : SRGBToLinear(float3(0, 0, 0));
surface.NormalTS = IN.TangentSpaceNormal;
surface.Emission = _Branch_c35c890039964b6d84d86513b7a90a46_Out_3_Vector3;
surface.Metallic = float(0);
surface.Specular = IsGammaSpace() ? float3(0, 0, 0) : SRGBToLinear(float3(0, 0, 0));
surface.Smoothness = float(0);
surface.Occlusion = float(2);
surface.Alpha = _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_A_7_Float;
surface.AlphaClipThreshold = _Property_ae6b5992b5874ce1b905e7962780a6be_Out_0_Float;
float3 reactionUnusedColor = 0, reactionUnusedEmission = 0;
ClipVegetationFuel(IN.WorldSpacePosition);
ApplyVegetationReaction(IN.WorldSpacePosition,surface.BaseColor,surface.Emission);
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
    output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
    output.AbsoluteWorldSpacePosition =                 GetAbsolutePositionWS(TransformObjectToWorld(input.positionOS));
    output.uv0 =                                        input.uv0;
    output.VertexColor =                                input.color;
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


    output.WorldSpacePosition = input.positionWS;
    output.ObjectSpacePosition = TransformWorldToObject(input.positionWS);

    #if UNITY_UV_STARTS_AT_TOP
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #else
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #endif


    output.uv0 = input.texCoord0;
    output.uv1 = input.texCoord1;
    output.uv2 = input.texCoord2;
    output.uv3 = input.texCoord3;
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
#pragma shader_feature _ _SOFT_SHADOWS
#pragma shader_feature_local _COLOR_OVERLAY_VERTEX_POSITION _COLOR_OVERLAY_UV

#if defined(_SOFT_SHADOWS) && defined(_COLOR_OVERLAY_VERTEX_POSITION)
#define KEYWORD_PERMUTATION_0
#elif defined(_SOFT_SHADOWS) && defined(_COLOR_OVERLAY_UV)
#define KEYWORD_PERMUTATION_1
#elif defined(_COLOR_OVERLAY_VERTEX_POSITION)
#define KEYWORD_PERMUTATION_2
#else
#define KEYWORD_PERMUTATION_3
#endif


// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_COLOR
#define GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT
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
 float4 color : COLOR;
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
 float3 WorldSpacePosition;
 float3 AbsoluteWorldSpacePosition;
 float4 uv0;
 float4 VertexColor;
 float3 TimeParameters;
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

float4 _Texture_TexelSize;
float4 _Top_Color;
float _Custom_Color;
float4 _Bottom_Color;
float _Blend_Height;
float _Wind_Strength;
float _Wind_Speed;
float _Wind_Variation;
float2 _Wind_Direction;
float _Alpha_Cutoff;
float _Enable_Top_Color;
float _Start_Bend_Range;
float _Wave_Scale;
float _Bend_Strength;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Texture);
SAMPLER(sampler_Texture);
float3 _Player_Position;

// Graph Includes
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"
#include_with_pragmas "Assets/06.Shaders/Foliage/UPG_FoliageInteraction.hlsl"

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

void Unity_Normalize_float2(float2 In, out float2 Out)
{
    Out = normalize(In);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_Add_float2(float2 A, float2 B, out float2 Out)
{
    Out = A + B;
}

float2 Unity_GradientNoise_Deterministic_Dir_float(float2 p)
{
float x; Hash_Tchou_2_1_float(p, x);
return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
}

void Unity_GradientNoise_Deterministic_float (float2 UV, float3 Scale, out float Out)
{
float2 p = UV * Scale.xy;
float2 ip = floor(p);
float2 fp = frac(p);
float d00 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip), fp);
float d01 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(0, 1)), fp - float2(0, 1));
float d10 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(1, 0)), fp - float2(1, 0));
float d11 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(1, 1)), fp - float2(1, 1));
fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
Out = lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5;
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Normalize_float3(float3 In, out float3 Out)
{
    Out = normalize(In);
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
{
    Out = lerp(A, B, T);
}

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
}

void Unity_Subtract_float3(float3 A, float3 B, out float3 Out)
{
    Out = A - B;
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Distance_float3(float3 A, float3 B, out float Out)
{
    Out = distance(A, B);
}

void Unity_Clamp_float(float In, float Min, float Max, out float Out)
{
    Out = clamp(In, Min, Max);
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
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
float _Split_f9cfb9a555e949379da9876f8d3844b2_R_1_Float = IN.WorldSpacePosition[0];
float _Split_f9cfb9a555e949379da9876f8d3844b2_G_2_Float = IN.WorldSpacePosition[1];
float _Split_f9cfb9a555e949379da9876f8d3844b2_B_3_Float = IN.WorldSpacePosition[2];
float _Split_f9cfb9a555e949379da9876f8d3844b2_A_4_Float = 0;
float2 _Vector2_feeecec8507c4619844c3ca15ea244be_Out_0_Vector2 = float2(_Split_f9cfb9a555e949379da9876f8d3844b2_R_1_Float, _Split_f9cfb9a555e949379da9876f8d3844b2_B_3_Float);
float2 _Property_186d136719274f03b9464e4e1d37a4e8_Out_0_Vector2 = _Wind_Direction;
float2 _Normalize_fb934df0b0c946039db4091949a06211_Out_1_Vector2;
Unity_Normalize_float2(_Property_186d136719274f03b9464e4e1d37a4e8_Out_0_Vector2, _Normalize_fb934df0b0c946039db4091949a06211_Out_1_Vector2);
float _Property_a98f7a88c8ac40109941c7f4f4211495_Out_0_Float = _Wind_Speed;
float _Multiply_880934a6c2a44f658fe3c3b3ed7cccac_Out_2_Float;
Unity_Multiply_float_float(10, _Property_a98f7a88c8ac40109941c7f4f4211495_Out_0_Float, _Multiply_880934a6c2a44f658fe3c3b3ed7cccac_Out_2_Float);
float2 _Multiply_892e7af0e8e54f869ff77c70575af5fc_Out_2_Vector2;
Unity_Multiply_float2_float2(_Normalize_fb934df0b0c946039db4091949a06211_Out_1_Vector2, (_Multiply_880934a6c2a44f658fe3c3b3ed7cccac_Out_2_Float.xx), _Multiply_892e7af0e8e54f869ff77c70575af5fc_Out_2_Vector2);
float2 _Multiply_d96cf1c14ac74322b4498e1849526399_Out_2_Vector2;
Unity_Multiply_float2_float2(_Multiply_892e7af0e8e54f869ff77c70575af5fc_Out_2_Vector2, (IN.TimeParameters.x.xx), _Multiply_d96cf1c14ac74322b4498e1849526399_Out_2_Vector2);
float2 _Add_c52cdcdd15b74d9a963511973a04621c_Out_2_Vector2;
Unity_Add_float2(_Vector2_feeecec8507c4619844c3ca15ea244be_Out_0_Vector2, _Multiply_d96cf1c14ac74322b4498e1849526399_Out_2_Vector2, _Add_c52cdcdd15b74d9a963511973a04621c_Out_2_Vector2);
float _Property_14515a2f6d834bd99002a2ccf491767f_Out_0_Float = _Wave_Scale;
float _Multiply_bbdcafe2ee654e3d8e4df25d9c66103c_Out_2_Float;
Unity_Multiply_float_float(3, _Property_14515a2f6d834bd99002a2ccf491767f_Out_0_Float, _Multiply_bbdcafe2ee654e3d8e4df25d9c66103c_Out_2_Float);
float _GradientNoise_32a9a6c418e44409a86a8238a6b4645c_Out_2_Float;
Unity_GradientNoise_Deterministic_float(_Add_c52cdcdd15b74d9a963511973a04621c_Out_2_Vector2, _Multiply_bbdcafe2ee654e3d8e4df25d9c66103c_Out_2_Float, _GradientNoise_32a9a6c418e44409a86a8238a6b4645c_Out_2_Float);
float _Subtract_3ec4867605194525898104109bc1e4a8_Out_2_Float;
Unity_Subtract_float(_GradientNoise_32a9a6c418e44409a86a8238a6b4645c_Out_2_Float, float(0.5), _Subtract_3ec4867605194525898104109bc1e4a8_Out_2_Float);
float _Property_a52d213096ea41398ec0a409bf7ca764_Out_0_Float = _Wind_Strength;
float _Multiply_444d89a5d2344a9d97e110a245567e62_Out_2_Float;
Unity_Multiply_float_float(2, _Property_a52d213096ea41398ec0a409bf7ca764_Out_0_Float, _Multiply_444d89a5d2344a9d97e110a245567e62_Out_2_Float);
float _Multiply_6a79300791ae4467bc6c5489c0164afa_Out_2_Float;
Unity_Multiply_float_float(_Subtract_3ec4867605194525898104109bc1e4a8_Out_2_Float, _Multiply_444d89a5d2344a9d97e110a245567e62_Out_2_Float, _Multiply_6a79300791ae4467bc6c5489c0164afa_Out_2_Float);
float3 _Normalize_ac3890981ce548259c746c56bd0ca30f_Out_1_Vector3;
Unity_Normalize_float3(IN.ObjectSpacePosition, _Normalize_ac3890981ce548259c746c56bd0ca30f_Out_1_Vector3);
float _Split_ce6b6b6587294130b97b66c5ec3b64a6_R_1_Float = _Normalize_ac3890981ce548259c746c56bd0ca30f_Out_1_Vector3[0];
float _Split_ce6b6b6587294130b97b66c5ec3b64a6_G_2_Float = _Normalize_ac3890981ce548259c746c56bd0ca30f_Out_1_Vector3[1];
float _Split_ce6b6b6587294130b97b66c5ec3b64a6_B_3_Float = _Normalize_ac3890981ce548259c746c56bd0ca30f_Out_1_Vector3[2];
float _Split_ce6b6b6587294130b97b66c5ec3b64a6_A_4_Float = 0;
float _Multiply_453e85026c7c4cc8b7c4661ac5495b5f_Out_2_Float;
Unity_Multiply_float_float(_Multiply_6a79300791ae4467bc6c5489c0164afa_Out_2_Float, _Split_ce6b6b6587294130b97b66c5ec3b64a6_G_2_Float, _Multiply_453e85026c7c4cc8b7c4661ac5495b5f_Out_2_Float);
float3 _Add_526cc646ad64464ba782c997c6d8a2e7_Out_2_Vector3;
Unity_Add_float3((_Multiply_453e85026c7c4cc8b7c4661ac5495b5f_Out_2_Float.xxx), IN.AbsoluteWorldSpacePosition, _Add_526cc646ad64464ba782c997c6d8a2e7_Out_2_Vector3);
float3 _Transform_26034a9482244d0db0cde719848b8793_Out_1_Vector3;
_Transform_26034a9482244d0db0cde719848b8793_Out_1_Vector3 = TransformWorldToObject(_Add_526cc646ad64464ba782c997c6d8a2e7_Out_2_Vector3.xyz);
float _Split_35ccc240162a41a796dd70233f88757c_R_1_Float = IN.VertexColor[0];
float _Split_35ccc240162a41a796dd70233f88757c_G_2_Float = IN.VertexColor[1];
float _Split_35ccc240162a41a796dd70233f88757c_B_3_Float = IN.VertexColor[2];
float _Split_35ccc240162a41a796dd70233f88757c_A_4_Float = IN.VertexColor[3];
float4 _Combine_bef8410c8e24482da5d3364b77236292_RGBA_4_Vector4;
float3 _Combine_bef8410c8e24482da5d3364b77236292_RGB_5_Vector3;
float2 _Combine_bef8410c8e24482da5d3364b77236292_RG_6_Vector2;
Unity_Combine_float(_Split_35ccc240162a41a796dd70233f88757c_R_1_Float, _Split_35ccc240162a41a796dd70233f88757c_G_2_Float, float(0), float(0), _Combine_bef8410c8e24482da5d3364b77236292_RGBA_4_Vector4, _Combine_bef8410c8e24482da5d3364b77236292_RGB_5_Vector3, _Combine_bef8410c8e24482da5d3364b77236292_RG_6_Vector2);
float3 _Lerp_7a7657270ab14fb2acad87e06eb37455_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _Transform_26034a9482244d0db0cde719848b8793_Out_1_Vector3, _Combine_bef8410c8e24482da5d3364b77236292_RGB_5_Vector3, _Lerp_7a7657270ab14fb2acad87e06eb37455_Out_3_Vector3);
float _Property_33225d4ce071408bbef8e7871ca2d298_Out_0_Float = _Wind_Speed;
float _Multiply_f56338cdfe18474dbabd15aebc9c5c2d_Out_2_Float;
Unity_Multiply_float_float(10, _Property_33225d4ce071408bbef8e7871ca2d298_Out_0_Float, _Multiply_f56338cdfe18474dbabd15aebc9c5c2d_Out_2_Float);
float _Multiply_7c3d48cd1d9e4ba6aaee9091a8d8a492_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Multiply_f56338cdfe18474dbabd15aebc9c5c2d_Out_2_Float, _Multiply_7c3d48cd1d9e4ba6aaee9091a8d8a492_Out_2_Float);
float2 _TilingAndOffset_ec707a81514142459d08db9978d86cf5_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, float2 (1, 1), (_Multiply_7c3d48cd1d9e4ba6aaee9091a8d8a492_Out_2_Float.xx), _TilingAndOffset_ec707a81514142459d08db9978d86cf5_Out_3_Vector2);
float _Property_52c7fe0910e04984b1e473c0627107b2_Out_0_Float = _Wind_Variation;
float _Multiply_a9622f7a126646aaa969ee30098cf924_Out_2_Float;
Unity_Multiply_float_float(0.5, _Property_52c7fe0910e04984b1e473c0627107b2_Out_0_Float, _Multiply_a9622f7a126646aaa969ee30098cf924_Out_2_Float);
float _GradientNoise_d9ac409c304041c49e680e55eb4d2c81_Out_2_Float;
Unity_GradientNoise_Deterministic_float(_TilingAndOffset_ec707a81514142459d08db9978d86cf5_Out_3_Vector2, _Multiply_a9622f7a126646aaa969ee30098cf924_Out_2_Float, _GradientNoise_d9ac409c304041c49e680e55eb4d2c81_Out_2_Float);
float3 _Lerp_1b4b768f79f34ddaa538834a53b8e33b_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _Lerp_7a7657270ab14fb2acad87e06eb37455_Out_3_Vector3, (_GradientNoise_d9ac409c304041c49e680e55eb4d2c81_Out_2_Float.xxx), _Lerp_1b4b768f79f34ddaa538834a53b8e33b_Out_3_Vector3);
float3 _Property_413c2dab0db145e686fd951a3cf9b06d_Out_0_Vector3 = _Player_Position;
float3 _Subtract_16caad2935f64d4ca2a5c41c122c2899_Out_2_Vector3;
Unity_Subtract_float3(IN.AbsoluteWorldSpacePosition, _Property_413c2dab0db145e686fd951a3cf9b06d_Out_0_Vector3, _Subtract_16caad2935f64d4ca2a5c41c122c2899_Out_2_Vector3);
float _Split_dd517b5d363349dfbcc6a80463d0a69c_R_1_Float = _Subtract_16caad2935f64d4ca2a5c41c122c2899_Out_2_Vector3[0];
float _Split_dd517b5d363349dfbcc6a80463d0a69c_G_2_Float = _Subtract_16caad2935f64d4ca2a5c41c122c2899_Out_2_Vector3[1];
float _Split_dd517b5d363349dfbcc6a80463d0a69c_B_3_Float = _Subtract_16caad2935f64d4ca2a5c41c122c2899_Out_2_Vector3[2];
float _Split_dd517b5d363349dfbcc6a80463d0a69c_A_4_Float = 0;
float _Property_f88a2d673c09471cad200fa3c1b6ae30_Out_0_Float = _Bend_Strength;
float _Multiply_6c5ecb62197a4848b2fabec6f920ae09_Out_2_Float;
Unity_Multiply_float_float(7, _Property_f88a2d673c09471cad200fa3c1b6ae30_Out_0_Float, _Multiply_6c5ecb62197a4848b2fabec6f920ae09_Out_2_Float);
float _Subtract_fabc7232020f481398d5e1c34e958838_Out_2_Float;
Unity_Subtract_float(_Split_dd517b5d363349dfbcc6a80463d0a69c_G_2_Float, _Multiply_6c5ecb62197a4848b2fabec6f920ae09_Out_2_Float, _Subtract_fabc7232020f481398d5e1c34e958838_Out_2_Float);
float4 _Combine_e1e6724c2316432f9d0cefb980576bd6_RGBA_4_Vector4;
float3 _Combine_e1e6724c2316432f9d0cefb980576bd6_RGB_5_Vector3;
float2 _Combine_e1e6724c2316432f9d0cefb980576bd6_RG_6_Vector2;
Unity_Combine_float(_Split_dd517b5d363349dfbcc6a80463d0a69c_R_1_Float, _Subtract_fabc7232020f481398d5e1c34e958838_Out_2_Float, _Split_dd517b5d363349dfbcc6a80463d0a69c_B_3_Float, float(0), _Combine_e1e6724c2316432f9d0cefb980576bd6_RGBA_4_Vector4, _Combine_e1e6724c2316432f9d0cefb980576bd6_RGB_5_Vector3, _Combine_e1e6724c2316432f9d0cefb980576bd6_RG_6_Vector2);
float3 _Multiply_42e953d7da1c4e7fbe777341488bae83_Out_2_Vector3;
Unity_Multiply_float3_float3(_Combine_e1e6724c2316432f9d0cefb980576bd6_RGB_5_Vector3, (_Property_f88a2d673c09471cad200fa3c1b6ae30_Out_0_Float.xxx), _Multiply_42e953d7da1c4e7fbe777341488bae83_Out_2_Vector3);
float _Property_ad55f894067547979f44b3a570bf7d49_Out_0_Float = _Start_Bend_Range;
float _Multiply_a7265b1673054e0996cb483d2ac3010d_Out_2_Float;
Unity_Multiply_float_float(_Property_ad55f894067547979f44b3a570bf7d49_Out_0_Float, 1.5, _Multiply_a7265b1673054e0996cb483d2ac3010d_Out_2_Float);
float _Distance_41bd00a9f1be4373a1003dc1e109849c_Out_2_Float;
Unity_Distance_float3(_Property_413c2dab0db145e686fd951a3cf9b06d_Out_0_Vector3, IN.AbsoluteWorldSpacePosition, _Distance_41bd00a9f1be4373a1003dc1e109849c_Out_2_Float);
float _Clamp_61013f1b8a7946cabf0f461c9754f564_Out_3_Float;
Unity_Clamp_float(_Distance_41bd00a9f1be4373a1003dc1e109849c_Out_2_Float, float(0), _Multiply_a7265b1673054e0996cb483d2ac3010d_Out_2_Float, _Clamp_61013f1b8a7946cabf0f461c9754f564_Out_3_Float);
float _Subtract_6193cd2daf004574816768560ab361b4_Out_2_Float;
Unity_Subtract_float(_Multiply_a7265b1673054e0996cb483d2ac3010d_Out_2_Float, _Clamp_61013f1b8a7946cabf0f461c9754f564_Out_3_Float, _Subtract_6193cd2daf004574816768560ab361b4_Out_2_Float);
float3 _Multiply_49122dc502124ec0b62c5e8d9df0b640_Out_2_Vector3;
Unity_Multiply_float3_float3(_Multiply_42e953d7da1c4e7fbe777341488bae83_Out_2_Vector3, (_Subtract_6193cd2daf004574816768560ab361b4_Out_2_Float.xxx), _Multiply_49122dc502124ec0b62c5e8d9df0b640_Out_2_Vector3);
float3 _Add_edf7ac44af404fdbbe1d0c50b7bc8b0f_Out_2_Vector3;
Unity_Add_float3(IN.ObjectSpacePosition, _Multiply_49122dc502124ec0b62c5e8d9df0b640_Out_2_Vector3, _Add_edf7ac44af404fdbbe1d0c50b7bc8b0f_Out_2_Vector3);
float3 _Normalize_0901e7953f394045adc844d92565cfba_Out_1_Vector3;
Unity_Normalize_float3(IN.ObjectSpacePosition, _Normalize_0901e7953f394045adc844d92565cfba_Out_1_Vector3);
float _Split_c58f3da36534495d8f4633fe4c40a74c_R_1_Float = _Normalize_0901e7953f394045adc844d92565cfba_Out_1_Vector3[0];
float _Split_c58f3da36534495d8f4633fe4c40a74c_G_2_Float = _Normalize_0901e7953f394045adc844d92565cfba_Out_1_Vector3[1];
float _Split_c58f3da36534495d8f4633fe4c40a74c_B_3_Float = _Normalize_0901e7953f394045adc844d92565cfba_Out_1_Vector3[2];
float _Split_c58f3da36534495d8f4633fe4c40a74c_A_4_Float = 0;
float _OneMinus_5a83b2c5314e45b2a835abb20d70bd22_Out_1_Float;
Unity_OneMinus_float(_Split_c58f3da36534495d8f4633fe4c40a74c_G_2_Float, _OneMinus_5a83b2c5314e45b2a835abb20d70bd22_Out_1_Float);
float _Step_428aa74b40a6492ba1e5a63ecfe4b53e_Out_2_Float;
Unity_Step_float(_OneMinus_5a83b2c5314e45b2a835abb20d70bd22_Out_1_Float, float(1), _Step_428aa74b40a6492ba1e5a63ecfe4b53e_Out_2_Float);
float3 _Lerp_7404d23a49244d1a8ca1c221f25d981c_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _Add_edf7ac44af404fdbbe1d0c50b7bc8b0f_Out_2_Vector3, (_Step_428aa74b40a6492ba1e5a63ecfe4b53e_Out_2_Float.xxx), _Lerp_7404d23a49244d1a8ca1c221f25d981c_Out_3_Vector3);
float3 _Lerp_93b3b392cffa4e688d1b32f6298c4907_Out_3_Vector3;
Unity_Lerp_float3(_Lerp_1b4b768f79f34ddaa538834a53b8e33b_Out_3_Vector3, _Lerp_7404d23a49244d1a8ca1c221f25d981c_Out_3_Vector3, (_GradientNoise_d9ac409c304041c49e680e55eb4d2c81_Out_2_Float.xxx), _Lerp_93b3b392cffa4e688d1b32f6298c4907_Out_3_Vector3);
float3 _UPGFoliageContactPositionCustomFunction_4d51d42c561a43cc976f6db5b768a1c6_bentPositionOS_1_Vector3;
UPG_FoliageContactPosition_float(_Lerp_93b3b392cffa4e688d1b32f6298c4907_Out_3_Vector3, _UPGFoliageContactPositionCustomFunction_4d51d42c561a43cc976f6db5b768a1c6_bentPositionOS_1_Vector3);
description.Position = _UPGFoliageContactPositionCustomFunction_4d51d42c561a43cc976f6db5b768a1c6_bentPositionOS_1_Vector3;
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
UnityTexture2D _Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Texture, sampler_Texture, _Texture_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D.tex, _Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D.samplerstate, _Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4, _Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_R_4_Float = _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4.r;
float _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_G_5_Float = _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4.g;
float _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_B_6_Float = _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4.b;
float _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_A_7_Float = _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4.a;
float _Property_ae6b5992b5874ce1b905e7962780a6be_Out_0_Float = _Alpha_Cutoff;
surface.Alpha = _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_A_7_Float;
surface.AlphaClipThreshold = _Property_ae6b5992b5874ce1b905e7962780a6be_Out_0_Float;
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
    output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
    output.AbsoluteWorldSpacePosition =                 GetAbsolutePositionWS(TransformObjectToWorld(input.positionOS));
    output.uv0 =                                        input.uv0;
    output.VertexColor =                                input.color;
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
#pragma shader_feature _ _SOFT_SHADOWS
#pragma shader_feature_local _COLOR_OVERLAY_VERTEX_POSITION _COLOR_OVERLAY_UV

#if defined(_SOFT_SHADOWS) && defined(_COLOR_OVERLAY_VERTEX_POSITION)
#define KEYWORD_PERMUTATION_0
#elif defined(_SOFT_SHADOWS) && defined(_COLOR_OVERLAY_UV)
#define KEYWORD_PERMUTATION_1
#elif defined(_COLOR_OVERLAY_VERTEX_POSITION)
#define KEYWORD_PERMUTATION_2
#else
#define KEYWORD_PERMUTATION_3
#endif


// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_COLOR
#define GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT
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
 float4 color : COLOR;
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
 float3 WorldSpacePosition;
 float3 AbsoluteWorldSpacePosition;
 float4 uv0;
 float4 VertexColor;
 float3 TimeParameters;
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

float4 _Texture_TexelSize;
float4 _Top_Color;
float _Custom_Color;
float4 _Bottom_Color;
float _Blend_Height;
float _Wind_Strength;
float _Wind_Speed;
float _Wind_Variation;
float2 _Wind_Direction;
float _Alpha_Cutoff;
float _Enable_Top_Color;
float _Start_Bend_Range;
float _Wave_Scale;
float _Bend_Strength;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Texture);
SAMPLER(sampler_Texture);
float3 _Player_Position;

// Graph Includes
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"
#include_with_pragmas "Assets/06.Shaders/Foliage/UPG_FoliageInteraction.hlsl"

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

void Unity_Normalize_float2(float2 In, out float2 Out)
{
    Out = normalize(In);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_Add_float2(float2 A, float2 B, out float2 Out)
{
    Out = A + B;
}

float2 Unity_GradientNoise_Deterministic_Dir_float(float2 p)
{
float x; Hash_Tchou_2_1_float(p, x);
return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
}

void Unity_GradientNoise_Deterministic_float (float2 UV, float3 Scale, out float Out)
{
float2 p = UV * Scale.xy;
float2 ip = floor(p);
float2 fp = frac(p);
float d00 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip), fp);
float d01 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(0, 1)), fp - float2(0, 1));
float d10 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(1, 0)), fp - float2(1, 0));
float d11 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(1, 1)), fp - float2(1, 1));
fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
Out = lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5;
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Normalize_float3(float3 In, out float3 Out)
{
    Out = normalize(In);
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
{
    Out = lerp(A, B, T);
}

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
}

void Unity_Subtract_float3(float3 A, float3 B, out float3 Out)
{
    Out = A - B;
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Distance_float3(float3 A, float3 B, out float Out)
{
    Out = distance(A, B);
}

void Unity_Clamp_float(float In, float Min, float Max, out float Out)
{
    Out = clamp(In, Min, Max);
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
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
};

VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
float _Split_f9cfb9a555e949379da9876f8d3844b2_R_1_Float = IN.WorldSpacePosition[0];
float _Split_f9cfb9a555e949379da9876f8d3844b2_G_2_Float = IN.WorldSpacePosition[1];
float _Split_f9cfb9a555e949379da9876f8d3844b2_B_3_Float = IN.WorldSpacePosition[2];
float _Split_f9cfb9a555e949379da9876f8d3844b2_A_4_Float = 0;
float2 _Vector2_feeecec8507c4619844c3ca15ea244be_Out_0_Vector2 = float2(_Split_f9cfb9a555e949379da9876f8d3844b2_R_1_Float, _Split_f9cfb9a555e949379da9876f8d3844b2_B_3_Float);
float2 _Property_186d136719274f03b9464e4e1d37a4e8_Out_0_Vector2 = _Wind_Direction;
float2 _Normalize_fb934df0b0c946039db4091949a06211_Out_1_Vector2;
Unity_Normalize_float2(_Property_186d136719274f03b9464e4e1d37a4e8_Out_0_Vector2, _Normalize_fb934df0b0c946039db4091949a06211_Out_1_Vector2);
float _Property_a98f7a88c8ac40109941c7f4f4211495_Out_0_Float = _Wind_Speed;
float _Multiply_880934a6c2a44f658fe3c3b3ed7cccac_Out_2_Float;
Unity_Multiply_float_float(10, _Property_a98f7a88c8ac40109941c7f4f4211495_Out_0_Float, _Multiply_880934a6c2a44f658fe3c3b3ed7cccac_Out_2_Float);
float2 _Multiply_892e7af0e8e54f869ff77c70575af5fc_Out_2_Vector2;
Unity_Multiply_float2_float2(_Normalize_fb934df0b0c946039db4091949a06211_Out_1_Vector2, (_Multiply_880934a6c2a44f658fe3c3b3ed7cccac_Out_2_Float.xx), _Multiply_892e7af0e8e54f869ff77c70575af5fc_Out_2_Vector2);
float2 _Multiply_d96cf1c14ac74322b4498e1849526399_Out_2_Vector2;
Unity_Multiply_float2_float2(_Multiply_892e7af0e8e54f869ff77c70575af5fc_Out_2_Vector2, (IN.TimeParameters.x.xx), _Multiply_d96cf1c14ac74322b4498e1849526399_Out_2_Vector2);
float2 _Add_c52cdcdd15b74d9a963511973a04621c_Out_2_Vector2;
Unity_Add_float2(_Vector2_feeecec8507c4619844c3ca15ea244be_Out_0_Vector2, _Multiply_d96cf1c14ac74322b4498e1849526399_Out_2_Vector2, _Add_c52cdcdd15b74d9a963511973a04621c_Out_2_Vector2);
float _Property_14515a2f6d834bd99002a2ccf491767f_Out_0_Float = _Wave_Scale;
float _Multiply_bbdcafe2ee654e3d8e4df25d9c66103c_Out_2_Float;
Unity_Multiply_float_float(3, _Property_14515a2f6d834bd99002a2ccf491767f_Out_0_Float, _Multiply_bbdcafe2ee654e3d8e4df25d9c66103c_Out_2_Float);
float _GradientNoise_32a9a6c418e44409a86a8238a6b4645c_Out_2_Float;
Unity_GradientNoise_Deterministic_float(_Add_c52cdcdd15b74d9a963511973a04621c_Out_2_Vector2, _Multiply_bbdcafe2ee654e3d8e4df25d9c66103c_Out_2_Float, _GradientNoise_32a9a6c418e44409a86a8238a6b4645c_Out_2_Float);
float _Subtract_3ec4867605194525898104109bc1e4a8_Out_2_Float;
Unity_Subtract_float(_GradientNoise_32a9a6c418e44409a86a8238a6b4645c_Out_2_Float, float(0.5), _Subtract_3ec4867605194525898104109bc1e4a8_Out_2_Float);
float _Property_a52d213096ea41398ec0a409bf7ca764_Out_0_Float = _Wind_Strength;
float _Multiply_444d89a5d2344a9d97e110a245567e62_Out_2_Float;
Unity_Multiply_float_float(2, _Property_a52d213096ea41398ec0a409bf7ca764_Out_0_Float, _Multiply_444d89a5d2344a9d97e110a245567e62_Out_2_Float);
float _Multiply_6a79300791ae4467bc6c5489c0164afa_Out_2_Float;
Unity_Multiply_float_float(_Subtract_3ec4867605194525898104109bc1e4a8_Out_2_Float, _Multiply_444d89a5d2344a9d97e110a245567e62_Out_2_Float, _Multiply_6a79300791ae4467bc6c5489c0164afa_Out_2_Float);
float3 _Normalize_ac3890981ce548259c746c56bd0ca30f_Out_1_Vector3;
Unity_Normalize_float3(IN.ObjectSpacePosition, _Normalize_ac3890981ce548259c746c56bd0ca30f_Out_1_Vector3);
float _Split_ce6b6b6587294130b97b66c5ec3b64a6_R_1_Float = _Normalize_ac3890981ce548259c746c56bd0ca30f_Out_1_Vector3[0];
float _Split_ce6b6b6587294130b97b66c5ec3b64a6_G_2_Float = _Normalize_ac3890981ce548259c746c56bd0ca30f_Out_1_Vector3[1];
float _Split_ce6b6b6587294130b97b66c5ec3b64a6_B_3_Float = _Normalize_ac3890981ce548259c746c56bd0ca30f_Out_1_Vector3[2];
float _Split_ce6b6b6587294130b97b66c5ec3b64a6_A_4_Float = 0;
float _Multiply_453e85026c7c4cc8b7c4661ac5495b5f_Out_2_Float;
Unity_Multiply_float_float(_Multiply_6a79300791ae4467bc6c5489c0164afa_Out_2_Float, _Split_ce6b6b6587294130b97b66c5ec3b64a6_G_2_Float, _Multiply_453e85026c7c4cc8b7c4661ac5495b5f_Out_2_Float);
float3 _Add_526cc646ad64464ba782c997c6d8a2e7_Out_2_Vector3;
Unity_Add_float3((_Multiply_453e85026c7c4cc8b7c4661ac5495b5f_Out_2_Float.xxx), IN.AbsoluteWorldSpacePosition, _Add_526cc646ad64464ba782c997c6d8a2e7_Out_2_Vector3);
float3 _Transform_26034a9482244d0db0cde719848b8793_Out_1_Vector3;
_Transform_26034a9482244d0db0cde719848b8793_Out_1_Vector3 = TransformWorldToObject(_Add_526cc646ad64464ba782c997c6d8a2e7_Out_2_Vector3.xyz);
float _Split_35ccc240162a41a796dd70233f88757c_R_1_Float = IN.VertexColor[0];
float _Split_35ccc240162a41a796dd70233f88757c_G_2_Float = IN.VertexColor[1];
float _Split_35ccc240162a41a796dd70233f88757c_B_3_Float = IN.VertexColor[2];
float _Split_35ccc240162a41a796dd70233f88757c_A_4_Float = IN.VertexColor[3];
float4 _Combine_bef8410c8e24482da5d3364b77236292_RGBA_4_Vector4;
float3 _Combine_bef8410c8e24482da5d3364b77236292_RGB_5_Vector3;
float2 _Combine_bef8410c8e24482da5d3364b77236292_RG_6_Vector2;
Unity_Combine_float(_Split_35ccc240162a41a796dd70233f88757c_R_1_Float, _Split_35ccc240162a41a796dd70233f88757c_G_2_Float, float(0), float(0), _Combine_bef8410c8e24482da5d3364b77236292_RGBA_4_Vector4, _Combine_bef8410c8e24482da5d3364b77236292_RGB_5_Vector3, _Combine_bef8410c8e24482da5d3364b77236292_RG_6_Vector2);
float3 _Lerp_7a7657270ab14fb2acad87e06eb37455_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _Transform_26034a9482244d0db0cde719848b8793_Out_1_Vector3, _Combine_bef8410c8e24482da5d3364b77236292_RGB_5_Vector3, _Lerp_7a7657270ab14fb2acad87e06eb37455_Out_3_Vector3);
float _Property_33225d4ce071408bbef8e7871ca2d298_Out_0_Float = _Wind_Speed;
float _Multiply_f56338cdfe18474dbabd15aebc9c5c2d_Out_2_Float;
Unity_Multiply_float_float(10, _Property_33225d4ce071408bbef8e7871ca2d298_Out_0_Float, _Multiply_f56338cdfe18474dbabd15aebc9c5c2d_Out_2_Float);
float _Multiply_7c3d48cd1d9e4ba6aaee9091a8d8a492_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Multiply_f56338cdfe18474dbabd15aebc9c5c2d_Out_2_Float, _Multiply_7c3d48cd1d9e4ba6aaee9091a8d8a492_Out_2_Float);
float2 _TilingAndOffset_ec707a81514142459d08db9978d86cf5_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, float2 (1, 1), (_Multiply_7c3d48cd1d9e4ba6aaee9091a8d8a492_Out_2_Float.xx), _TilingAndOffset_ec707a81514142459d08db9978d86cf5_Out_3_Vector2);
float _Property_52c7fe0910e04984b1e473c0627107b2_Out_0_Float = _Wind_Variation;
float _Multiply_a9622f7a126646aaa969ee30098cf924_Out_2_Float;
Unity_Multiply_float_float(0.5, _Property_52c7fe0910e04984b1e473c0627107b2_Out_0_Float, _Multiply_a9622f7a126646aaa969ee30098cf924_Out_2_Float);
float _GradientNoise_d9ac409c304041c49e680e55eb4d2c81_Out_2_Float;
Unity_GradientNoise_Deterministic_float(_TilingAndOffset_ec707a81514142459d08db9978d86cf5_Out_3_Vector2, _Multiply_a9622f7a126646aaa969ee30098cf924_Out_2_Float, _GradientNoise_d9ac409c304041c49e680e55eb4d2c81_Out_2_Float);
float3 _Lerp_1b4b768f79f34ddaa538834a53b8e33b_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _Lerp_7a7657270ab14fb2acad87e06eb37455_Out_3_Vector3, (_GradientNoise_d9ac409c304041c49e680e55eb4d2c81_Out_2_Float.xxx), _Lerp_1b4b768f79f34ddaa538834a53b8e33b_Out_3_Vector3);
float3 _Property_413c2dab0db145e686fd951a3cf9b06d_Out_0_Vector3 = _Player_Position;
float3 _Subtract_16caad2935f64d4ca2a5c41c122c2899_Out_2_Vector3;
Unity_Subtract_float3(IN.AbsoluteWorldSpacePosition, _Property_413c2dab0db145e686fd951a3cf9b06d_Out_0_Vector3, _Subtract_16caad2935f64d4ca2a5c41c122c2899_Out_2_Vector3);
float _Split_dd517b5d363349dfbcc6a80463d0a69c_R_1_Float = _Subtract_16caad2935f64d4ca2a5c41c122c2899_Out_2_Vector3[0];
float _Split_dd517b5d363349dfbcc6a80463d0a69c_G_2_Float = _Subtract_16caad2935f64d4ca2a5c41c122c2899_Out_2_Vector3[1];
float _Split_dd517b5d363349dfbcc6a80463d0a69c_B_3_Float = _Subtract_16caad2935f64d4ca2a5c41c122c2899_Out_2_Vector3[2];
float _Split_dd517b5d363349dfbcc6a80463d0a69c_A_4_Float = 0;
float _Property_f88a2d673c09471cad200fa3c1b6ae30_Out_0_Float = _Bend_Strength;
float _Multiply_6c5ecb62197a4848b2fabec6f920ae09_Out_2_Float;
Unity_Multiply_float_float(7, _Property_f88a2d673c09471cad200fa3c1b6ae30_Out_0_Float, _Multiply_6c5ecb62197a4848b2fabec6f920ae09_Out_2_Float);
float _Subtract_fabc7232020f481398d5e1c34e958838_Out_2_Float;
Unity_Subtract_float(_Split_dd517b5d363349dfbcc6a80463d0a69c_G_2_Float, _Multiply_6c5ecb62197a4848b2fabec6f920ae09_Out_2_Float, _Subtract_fabc7232020f481398d5e1c34e958838_Out_2_Float);
float4 _Combine_e1e6724c2316432f9d0cefb980576bd6_RGBA_4_Vector4;
float3 _Combine_e1e6724c2316432f9d0cefb980576bd6_RGB_5_Vector3;
float2 _Combine_e1e6724c2316432f9d0cefb980576bd6_RG_6_Vector2;
Unity_Combine_float(_Split_dd517b5d363349dfbcc6a80463d0a69c_R_1_Float, _Subtract_fabc7232020f481398d5e1c34e958838_Out_2_Float, _Split_dd517b5d363349dfbcc6a80463d0a69c_B_3_Float, float(0), _Combine_e1e6724c2316432f9d0cefb980576bd6_RGBA_4_Vector4, _Combine_e1e6724c2316432f9d0cefb980576bd6_RGB_5_Vector3, _Combine_e1e6724c2316432f9d0cefb980576bd6_RG_6_Vector2);
float3 _Multiply_42e953d7da1c4e7fbe777341488bae83_Out_2_Vector3;
Unity_Multiply_float3_float3(_Combine_e1e6724c2316432f9d0cefb980576bd6_RGB_5_Vector3, (_Property_f88a2d673c09471cad200fa3c1b6ae30_Out_0_Float.xxx), _Multiply_42e953d7da1c4e7fbe777341488bae83_Out_2_Vector3);
float _Property_ad55f894067547979f44b3a570bf7d49_Out_0_Float = _Start_Bend_Range;
float _Multiply_a7265b1673054e0996cb483d2ac3010d_Out_2_Float;
Unity_Multiply_float_float(_Property_ad55f894067547979f44b3a570bf7d49_Out_0_Float, 1.5, _Multiply_a7265b1673054e0996cb483d2ac3010d_Out_2_Float);
float _Distance_41bd00a9f1be4373a1003dc1e109849c_Out_2_Float;
Unity_Distance_float3(_Property_413c2dab0db145e686fd951a3cf9b06d_Out_0_Vector3, IN.AbsoluteWorldSpacePosition, _Distance_41bd00a9f1be4373a1003dc1e109849c_Out_2_Float);
float _Clamp_61013f1b8a7946cabf0f461c9754f564_Out_3_Float;
Unity_Clamp_float(_Distance_41bd00a9f1be4373a1003dc1e109849c_Out_2_Float, float(0), _Multiply_a7265b1673054e0996cb483d2ac3010d_Out_2_Float, _Clamp_61013f1b8a7946cabf0f461c9754f564_Out_3_Float);
float _Subtract_6193cd2daf004574816768560ab361b4_Out_2_Float;
Unity_Subtract_float(_Multiply_a7265b1673054e0996cb483d2ac3010d_Out_2_Float, _Clamp_61013f1b8a7946cabf0f461c9754f564_Out_3_Float, _Subtract_6193cd2daf004574816768560ab361b4_Out_2_Float);
float3 _Multiply_49122dc502124ec0b62c5e8d9df0b640_Out_2_Vector3;
Unity_Multiply_float3_float3(_Multiply_42e953d7da1c4e7fbe777341488bae83_Out_2_Vector3, (_Subtract_6193cd2daf004574816768560ab361b4_Out_2_Float.xxx), _Multiply_49122dc502124ec0b62c5e8d9df0b640_Out_2_Vector3);
float3 _Add_edf7ac44af404fdbbe1d0c50b7bc8b0f_Out_2_Vector3;
Unity_Add_float3(IN.ObjectSpacePosition, _Multiply_49122dc502124ec0b62c5e8d9df0b640_Out_2_Vector3, _Add_edf7ac44af404fdbbe1d0c50b7bc8b0f_Out_2_Vector3);
float3 _Normalize_0901e7953f394045adc844d92565cfba_Out_1_Vector3;
Unity_Normalize_float3(IN.ObjectSpacePosition, _Normalize_0901e7953f394045adc844d92565cfba_Out_1_Vector3);
float _Split_c58f3da36534495d8f4633fe4c40a74c_R_1_Float = _Normalize_0901e7953f394045adc844d92565cfba_Out_1_Vector3[0];
float _Split_c58f3da36534495d8f4633fe4c40a74c_G_2_Float = _Normalize_0901e7953f394045adc844d92565cfba_Out_1_Vector3[1];
float _Split_c58f3da36534495d8f4633fe4c40a74c_B_3_Float = _Normalize_0901e7953f394045adc844d92565cfba_Out_1_Vector3[2];
float _Split_c58f3da36534495d8f4633fe4c40a74c_A_4_Float = 0;
float _OneMinus_5a83b2c5314e45b2a835abb20d70bd22_Out_1_Float;
Unity_OneMinus_float(_Split_c58f3da36534495d8f4633fe4c40a74c_G_2_Float, _OneMinus_5a83b2c5314e45b2a835abb20d70bd22_Out_1_Float);
float _Step_428aa74b40a6492ba1e5a63ecfe4b53e_Out_2_Float;
Unity_Step_float(_OneMinus_5a83b2c5314e45b2a835abb20d70bd22_Out_1_Float, float(1), _Step_428aa74b40a6492ba1e5a63ecfe4b53e_Out_2_Float);
float3 _Lerp_7404d23a49244d1a8ca1c221f25d981c_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _Add_edf7ac44af404fdbbe1d0c50b7bc8b0f_Out_2_Vector3, (_Step_428aa74b40a6492ba1e5a63ecfe4b53e_Out_2_Float.xxx), _Lerp_7404d23a49244d1a8ca1c221f25d981c_Out_3_Vector3);
float3 _Lerp_93b3b392cffa4e688d1b32f6298c4907_Out_3_Vector3;
Unity_Lerp_float3(_Lerp_1b4b768f79f34ddaa538834a53b8e33b_Out_3_Vector3, _Lerp_7404d23a49244d1a8ca1c221f25d981c_Out_3_Vector3, (_GradientNoise_d9ac409c304041c49e680e55eb4d2c81_Out_2_Float.xxx), _Lerp_93b3b392cffa4e688d1b32f6298c4907_Out_3_Vector3);
float3 _UPGFoliageContactPositionCustomFunction_4d51d42c561a43cc976f6db5b768a1c6_bentPositionOS_1_Vector3;
UPG_FoliageContactPosition_float(_Lerp_93b3b392cffa4e688d1b32f6298c4907_Out_3_Vector3, _UPGFoliageContactPositionCustomFunction_4d51d42c561a43cc976f6db5b768a1c6_bentPositionOS_1_Vector3);
description.Position = _UPGFoliageContactPositionCustomFunction_4d51d42c561a43cc976f6db5b768a1c6_bentPositionOS_1_Vector3;
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
UnityTexture2D _Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Texture, sampler_Texture, _Texture_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D.tex, _Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D.samplerstate, _Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4, _Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_R_4_Float = _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4.r;
float _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_G_5_Float = _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4.g;
float _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_B_6_Float = _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4.b;
float _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_A_7_Float = _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4.a;
float _Property_ae6b5992b5874ce1b905e7962780a6be_Out_0_Float = _Alpha_Cutoff;
surface.Alpha = _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_A_7_Float;
surface.AlphaClipThreshold = _Property_ae6b5992b5874ce1b905e7962780a6be_Out_0_Float;
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
    output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
    output.AbsoluteWorldSpacePosition =                 GetAbsolutePositionWS(TransformObjectToWorld(input.positionOS));
    output.uv0 =                                        input.uv0;
    output.VertexColor =                                input.color;
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
#pragma shader_feature _ _SOFT_SHADOWS
#pragma shader_feature_local _COLOR_OVERLAY_VERTEX_POSITION _COLOR_OVERLAY_UV

#if defined(_SOFT_SHADOWS) && defined(_COLOR_OVERLAY_VERTEX_POSITION)
#define KEYWORD_PERMUTATION_0
#elif defined(_SOFT_SHADOWS) && defined(_COLOR_OVERLAY_UV)
#define KEYWORD_PERMUTATION_1
#elif defined(_COLOR_OVERLAY_VERTEX_POSITION)
#define KEYWORD_PERMUTATION_2
#else
#define KEYWORD_PERMUTATION_3
#endif


// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_COLOR
#define GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT
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
 float4 color : COLOR;
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
 float3 WorldSpacePosition;
 float3 AbsoluteWorldSpacePosition;
 float4 uv0;
 float4 VertexColor;
 float3 TimeParameters;
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

float4 _Texture_TexelSize;
float4 _Top_Color;
float _Custom_Color;
float4 _Bottom_Color;
float _Blend_Height;
float _Wind_Strength;
float _Wind_Speed;
float _Wind_Variation;
float2 _Wind_Direction;
float _Alpha_Cutoff;
float _Enable_Top_Color;
float _Start_Bend_Range;
float _Wave_Scale;
float _Bend_Strength;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Texture);
SAMPLER(sampler_Texture);
float3 _Player_Position;

// Graph Includes
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"
#include_with_pragmas "Assets/06.Shaders/Foliage/UPG_FoliageInteraction.hlsl"

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

void Unity_Normalize_float2(float2 In, out float2 Out)
{
    Out = normalize(In);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_Add_float2(float2 A, float2 B, out float2 Out)
{
    Out = A + B;
}

float2 Unity_GradientNoise_Deterministic_Dir_float(float2 p)
{
float x; Hash_Tchou_2_1_float(p, x);
return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
}

void Unity_GradientNoise_Deterministic_float (float2 UV, float3 Scale, out float Out)
{
float2 p = UV * Scale.xy;
float2 ip = floor(p);
float2 fp = frac(p);
float d00 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip), fp);
float d01 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(0, 1)), fp - float2(0, 1));
float d10 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(1, 0)), fp - float2(1, 0));
float d11 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(1, 1)), fp - float2(1, 1));
fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
Out = lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5;
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Normalize_float3(float3 In, out float3 Out)
{
    Out = normalize(In);
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
{
    Out = lerp(A, B, T);
}

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
}

void Unity_Subtract_float3(float3 A, float3 B, out float3 Out)
{
    Out = A - B;
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Distance_float3(float3 A, float3 B, out float Out)
{
    Out = distance(A, B);
}

void Unity_Clamp_float(float In, float Min, float Max, out float Out)
{
    Out = clamp(In, Min, Max);
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
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
float _Split_f9cfb9a555e949379da9876f8d3844b2_R_1_Float = IN.WorldSpacePosition[0];
float _Split_f9cfb9a555e949379da9876f8d3844b2_G_2_Float = IN.WorldSpacePosition[1];
float _Split_f9cfb9a555e949379da9876f8d3844b2_B_3_Float = IN.WorldSpacePosition[2];
float _Split_f9cfb9a555e949379da9876f8d3844b2_A_4_Float = 0;
float2 _Vector2_feeecec8507c4619844c3ca15ea244be_Out_0_Vector2 = float2(_Split_f9cfb9a555e949379da9876f8d3844b2_R_1_Float, _Split_f9cfb9a555e949379da9876f8d3844b2_B_3_Float);
float2 _Property_186d136719274f03b9464e4e1d37a4e8_Out_0_Vector2 = _Wind_Direction;
float2 _Normalize_fb934df0b0c946039db4091949a06211_Out_1_Vector2;
Unity_Normalize_float2(_Property_186d136719274f03b9464e4e1d37a4e8_Out_0_Vector2, _Normalize_fb934df0b0c946039db4091949a06211_Out_1_Vector2);
float _Property_a98f7a88c8ac40109941c7f4f4211495_Out_0_Float = _Wind_Speed;
float _Multiply_880934a6c2a44f658fe3c3b3ed7cccac_Out_2_Float;
Unity_Multiply_float_float(10, _Property_a98f7a88c8ac40109941c7f4f4211495_Out_0_Float, _Multiply_880934a6c2a44f658fe3c3b3ed7cccac_Out_2_Float);
float2 _Multiply_892e7af0e8e54f869ff77c70575af5fc_Out_2_Vector2;
Unity_Multiply_float2_float2(_Normalize_fb934df0b0c946039db4091949a06211_Out_1_Vector2, (_Multiply_880934a6c2a44f658fe3c3b3ed7cccac_Out_2_Float.xx), _Multiply_892e7af0e8e54f869ff77c70575af5fc_Out_2_Vector2);
float2 _Multiply_d96cf1c14ac74322b4498e1849526399_Out_2_Vector2;
Unity_Multiply_float2_float2(_Multiply_892e7af0e8e54f869ff77c70575af5fc_Out_2_Vector2, (IN.TimeParameters.x.xx), _Multiply_d96cf1c14ac74322b4498e1849526399_Out_2_Vector2);
float2 _Add_c52cdcdd15b74d9a963511973a04621c_Out_2_Vector2;
Unity_Add_float2(_Vector2_feeecec8507c4619844c3ca15ea244be_Out_0_Vector2, _Multiply_d96cf1c14ac74322b4498e1849526399_Out_2_Vector2, _Add_c52cdcdd15b74d9a963511973a04621c_Out_2_Vector2);
float _Property_14515a2f6d834bd99002a2ccf491767f_Out_0_Float = _Wave_Scale;
float _Multiply_bbdcafe2ee654e3d8e4df25d9c66103c_Out_2_Float;
Unity_Multiply_float_float(3, _Property_14515a2f6d834bd99002a2ccf491767f_Out_0_Float, _Multiply_bbdcafe2ee654e3d8e4df25d9c66103c_Out_2_Float);
float _GradientNoise_32a9a6c418e44409a86a8238a6b4645c_Out_2_Float;
Unity_GradientNoise_Deterministic_float(_Add_c52cdcdd15b74d9a963511973a04621c_Out_2_Vector2, _Multiply_bbdcafe2ee654e3d8e4df25d9c66103c_Out_2_Float, _GradientNoise_32a9a6c418e44409a86a8238a6b4645c_Out_2_Float);
float _Subtract_3ec4867605194525898104109bc1e4a8_Out_2_Float;
Unity_Subtract_float(_GradientNoise_32a9a6c418e44409a86a8238a6b4645c_Out_2_Float, float(0.5), _Subtract_3ec4867605194525898104109bc1e4a8_Out_2_Float);
float _Property_a52d213096ea41398ec0a409bf7ca764_Out_0_Float = _Wind_Strength;
float _Multiply_444d89a5d2344a9d97e110a245567e62_Out_2_Float;
Unity_Multiply_float_float(2, _Property_a52d213096ea41398ec0a409bf7ca764_Out_0_Float, _Multiply_444d89a5d2344a9d97e110a245567e62_Out_2_Float);
float _Multiply_6a79300791ae4467bc6c5489c0164afa_Out_2_Float;
Unity_Multiply_float_float(_Subtract_3ec4867605194525898104109bc1e4a8_Out_2_Float, _Multiply_444d89a5d2344a9d97e110a245567e62_Out_2_Float, _Multiply_6a79300791ae4467bc6c5489c0164afa_Out_2_Float);
float3 _Normalize_ac3890981ce548259c746c56bd0ca30f_Out_1_Vector3;
Unity_Normalize_float3(IN.ObjectSpacePosition, _Normalize_ac3890981ce548259c746c56bd0ca30f_Out_1_Vector3);
float _Split_ce6b6b6587294130b97b66c5ec3b64a6_R_1_Float = _Normalize_ac3890981ce548259c746c56bd0ca30f_Out_1_Vector3[0];
float _Split_ce6b6b6587294130b97b66c5ec3b64a6_G_2_Float = _Normalize_ac3890981ce548259c746c56bd0ca30f_Out_1_Vector3[1];
float _Split_ce6b6b6587294130b97b66c5ec3b64a6_B_3_Float = _Normalize_ac3890981ce548259c746c56bd0ca30f_Out_1_Vector3[2];
float _Split_ce6b6b6587294130b97b66c5ec3b64a6_A_4_Float = 0;
float _Multiply_453e85026c7c4cc8b7c4661ac5495b5f_Out_2_Float;
Unity_Multiply_float_float(_Multiply_6a79300791ae4467bc6c5489c0164afa_Out_2_Float, _Split_ce6b6b6587294130b97b66c5ec3b64a6_G_2_Float, _Multiply_453e85026c7c4cc8b7c4661ac5495b5f_Out_2_Float);
float3 _Add_526cc646ad64464ba782c997c6d8a2e7_Out_2_Vector3;
Unity_Add_float3((_Multiply_453e85026c7c4cc8b7c4661ac5495b5f_Out_2_Float.xxx), IN.AbsoluteWorldSpacePosition, _Add_526cc646ad64464ba782c997c6d8a2e7_Out_2_Vector3);
float3 _Transform_26034a9482244d0db0cde719848b8793_Out_1_Vector3;
_Transform_26034a9482244d0db0cde719848b8793_Out_1_Vector3 = TransformWorldToObject(_Add_526cc646ad64464ba782c997c6d8a2e7_Out_2_Vector3.xyz);
float _Split_35ccc240162a41a796dd70233f88757c_R_1_Float = IN.VertexColor[0];
float _Split_35ccc240162a41a796dd70233f88757c_G_2_Float = IN.VertexColor[1];
float _Split_35ccc240162a41a796dd70233f88757c_B_3_Float = IN.VertexColor[2];
float _Split_35ccc240162a41a796dd70233f88757c_A_4_Float = IN.VertexColor[3];
float4 _Combine_bef8410c8e24482da5d3364b77236292_RGBA_4_Vector4;
float3 _Combine_bef8410c8e24482da5d3364b77236292_RGB_5_Vector3;
float2 _Combine_bef8410c8e24482da5d3364b77236292_RG_6_Vector2;
Unity_Combine_float(_Split_35ccc240162a41a796dd70233f88757c_R_1_Float, _Split_35ccc240162a41a796dd70233f88757c_G_2_Float, float(0), float(0), _Combine_bef8410c8e24482da5d3364b77236292_RGBA_4_Vector4, _Combine_bef8410c8e24482da5d3364b77236292_RGB_5_Vector3, _Combine_bef8410c8e24482da5d3364b77236292_RG_6_Vector2);
float3 _Lerp_7a7657270ab14fb2acad87e06eb37455_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _Transform_26034a9482244d0db0cde719848b8793_Out_1_Vector3, _Combine_bef8410c8e24482da5d3364b77236292_RGB_5_Vector3, _Lerp_7a7657270ab14fb2acad87e06eb37455_Out_3_Vector3);
float _Property_33225d4ce071408bbef8e7871ca2d298_Out_0_Float = _Wind_Speed;
float _Multiply_f56338cdfe18474dbabd15aebc9c5c2d_Out_2_Float;
Unity_Multiply_float_float(10, _Property_33225d4ce071408bbef8e7871ca2d298_Out_0_Float, _Multiply_f56338cdfe18474dbabd15aebc9c5c2d_Out_2_Float);
float _Multiply_7c3d48cd1d9e4ba6aaee9091a8d8a492_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Multiply_f56338cdfe18474dbabd15aebc9c5c2d_Out_2_Float, _Multiply_7c3d48cd1d9e4ba6aaee9091a8d8a492_Out_2_Float);
float2 _TilingAndOffset_ec707a81514142459d08db9978d86cf5_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, float2 (1, 1), (_Multiply_7c3d48cd1d9e4ba6aaee9091a8d8a492_Out_2_Float.xx), _TilingAndOffset_ec707a81514142459d08db9978d86cf5_Out_3_Vector2);
float _Property_52c7fe0910e04984b1e473c0627107b2_Out_0_Float = _Wind_Variation;
float _Multiply_a9622f7a126646aaa969ee30098cf924_Out_2_Float;
Unity_Multiply_float_float(0.5, _Property_52c7fe0910e04984b1e473c0627107b2_Out_0_Float, _Multiply_a9622f7a126646aaa969ee30098cf924_Out_2_Float);
float _GradientNoise_d9ac409c304041c49e680e55eb4d2c81_Out_2_Float;
Unity_GradientNoise_Deterministic_float(_TilingAndOffset_ec707a81514142459d08db9978d86cf5_Out_3_Vector2, _Multiply_a9622f7a126646aaa969ee30098cf924_Out_2_Float, _GradientNoise_d9ac409c304041c49e680e55eb4d2c81_Out_2_Float);
float3 _Lerp_1b4b768f79f34ddaa538834a53b8e33b_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _Lerp_7a7657270ab14fb2acad87e06eb37455_Out_3_Vector3, (_GradientNoise_d9ac409c304041c49e680e55eb4d2c81_Out_2_Float.xxx), _Lerp_1b4b768f79f34ddaa538834a53b8e33b_Out_3_Vector3);
float3 _Property_413c2dab0db145e686fd951a3cf9b06d_Out_0_Vector3 = _Player_Position;
float3 _Subtract_16caad2935f64d4ca2a5c41c122c2899_Out_2_Vector3;
Unity_Subtract_float3(IN.AbsoluteWorldSpacePosition, _Property_413c2dab0db145e686fd951a3cf9b06d_Out_0_Vector3, _Subtract_16caad2935f64d4ca2a5c41c122c2899_Out_2_Vector3);
float _Split_dd517b5d363349dfbcc6a80463d0a69c_R_1_Float = _Subtract_16caad2935f64d4ca2a5c41c122c2899_Out_2_Vector3[0];
float _Split_dd517b5d363349dfbcc6a80463d0a69c_G_2_Float = _Subtract_16caad2935f64d4ca2a5c41c122c2899_Out_2_Vector3[1];
float _Split_dd517b5d363349dfbcc6a80463d0a69c_B_3_Float = _Subtract_16caad2935f64d4ca2a5c41c122c2899_Out_2_Vector3[2];
float _Split_dd517b5d363349dfbcc6a80463d0a69c_A_4_Float = 0;
float _Property_f88a2d673c09471cad200fa3c1b6ae30_Out_0_Float = _Bend_Strength;
float _Multiply_6c5ecb62197a4848b2fabec6f920ae09_Out_2_Float;
Unity_Multiply_float_float(7, _Property_f88a2d673c09471cad200fa3c1b6ae30_Out_0_Float, _Multiply_6c5ecb62197a4848b2fabec6f920ae09_Out_2_Float);
float _Subtract_fabc7232020f481398d5e1c34e958838_Out_2_Float;
Unity_Subtract_float(_Split_dd517b5d363349dfbcc6a80463d0a69c_G_2_Float, _Multiply_6c5ecb62197a4848b2fabec6f920ae09_Out_2_Float, _Subtract_fabc7232020f481398d5e1c34e958838_Out_2_Float);
float4 _Combine_e1e6724c2316432f9d0cefb980576bd6_RGBA_4_Vector4;
float3 _Combine_e1e6724c2316432f9d0cefb980576bd6_RGB_5_Vector3;
float2 _Combine_e1e6724c2316432f9d0cefb980576bd6_RG_6_Vector2;
Unity_Combine_float(_Split_dd517b5d363349dfbcc6a80463d0a69c_R_1_Float, _Subtract_fabc7232020f481398d5e1c34e958838_Out_2_Float, _Split_dd517b5d363349dfbcc6a80463d0a69c_B_3_Float, float(0), _Combine_e1e6724c2316432f9d0cefb980576bd6_RGBA_4_Vector4, _Combine_e1e6724c2316432f9d0cefb980576bd6_RGB_5_Vector3, _Combine_e1e6724c2316432f9d0cefb980576bd6_RG_6_Vector2);
float3 _Multiply_42e953d7da1c4e7fbe777341488bae83_Out_2_Vector3;
Unity_Multiply_float3_float3(_Combine_e1e6724c2316432f9d0cefb980576bd6_RGB_5_Vector3, (_Property_f88a2d673c09471cad200fa3c1b6ae30_Out_0_Float.xxx), _Multiply_42e953d7da1c4e7fbe777341488bae83_Out_2_Vector3);
float _Property_ad55f894067547979f44b3a570bf7d49_Out_0_Float = _Start_Bend_Range;
float _Multiply_a7265b1673054e0996cb483d2ac3010d_Out_2_Float;
Unity_Multiply_float_float(_Property_ad55f894067547979f44b3a570bf7d49_Out_0_Float, 1.5, _Multiply_a7265b1673054e0996cb483d2ac3010d_Out_2_Float);
float _Distance_41bd00a9f1be4373a1003dc1e109849c_Out_2_Float;
Unity_Distance_float3(_Property_413c2dab0db145e686fd951a3cf9b06d_Out_0_Vector3, IN.AbsoluteWorldSpacePosition, _Distance_41bd00a9f1be4373a1003dc1e109849c_Out_2_Float);
float _Clamp_61013f1b8a7946cabf0f461c9754f564_Out_3_Float;
Unity_Clamp_float(_Distance_41bd00a9f1be4373a1003dc1e109849c_Out_2_Float, float(0), _Multiply_a7265b1673054e0996cb483d2ac3010d_Out_2_Float, _Clamp_61013f1b8a7946cabf0f461c9754f564_Out_3_Float);
float _Subtract_6193cd2daf004574816768560ab361b4_Out_2_Float;
Unity_Subtract_float(_Multiply_a7265b1673054e0996cb483d2ac3010d_Out_2_Float, _Clamp_61013f1b8a7946cabf0f461c9754f564_Out_3_Float, _Subtract_6193cd2daf004574816768560ab361b4_Out_2_Float);
float3 _Multiply_49122dc502124ec0b62c5e8d9df0b640_Out_2_Vector3;
Unity_Multiply_float3_float3(_Multiply_42e953d7da1c4e7fbe777341488bae83_Out_2_Vector3, (_Subtract_6193cd2daf004574816768560ab361b4_Out_2_Float.xxx), _Multiply_49122dc502124ec0b62c5e8d9df0b640_Out_2_Vector3);
float3 _Add_edf7ac44af404fdbbe1d0c50b7bc8b0f_Out_2_Vector3;
Unity_Add_float3(IN.ObjectSpacePosition, _Multiply_49122dc502124ec0b62c5e8d9df0b640_Out_2_Vector3, _Add_edf7ac44af404fdbbe1d0c50b7bc8b0f_Out_2_Vector3);
float3 _Normalize_0901e7953f394045adc844d92565cfba_Out_1_Vector3;
Unity_Normalize_float3(IN.ObjectSpacePosition, _Normalize_0901e7953f394045adc844d92565cfba_Out_1_Vector3);
float _Split_c58f3da36534495d8f4633fe4c40a74c_R_1_Float = _Normalize_0901e7953f394045adc844d92565cfba_Out_1_Vector3[0];
float _Split_c58f3da36534495d8f4633fe4c40a74c_G_2_Float = _Normalize_0901e7953f394045adc844d92565cfba_Out_1_Vector3[1];
float _Split_c58f3da36534495d8f4633fe4c40a74c_B_3_Float = _Normalize_0901e7953f394045adc844d92565cfba_Out_1_Vector3[2];
float _Split_c58f3da36534495d8f4633fe4c40a74c_A_4_Float = 0;
float _OneMinus_5a83b2c5314e45b2a835abb20d70bd22_Out_1_Float;
Unity_OneMinus_float(_Split_c58f3da36534495d8f4633fe4c40a74c_G_2_Float, _OneMinus_5a83b2c5314e45b2a835abb20d70bd22_Out_1_Float);
float _Step_428aa74b40a6492ba1e5a63ecfe4b53e_Out_2_Float;
Unity_Step_float(_OneMinus_5a83b2c5314e45b2a835abb20d70bd22_Out_1_Float, float(1), _Step_428aa74b40a6492ba1e5a63ecfe4b53e_Out_2_Float);
float3 _Lerp_7404d23a49244d1a8ca1c221f25d981c_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _Add_edf7ac44af404fdbbe1d0c50b7bc8b0f_Out_2_Vector3, (_Step_428aa74b40a6492ba1e5a63ecfe4b53e_Out_2_Float.xxx), _Lerp_7404d23a49244d1a8ca1c221f25d981c_Out_3_Vector3);
float3 _Lerp_93b3b392cffa4e688d1b32f6298c4907_Out_3_Vector3;
Unity_Lerp_float3(_Lerp_1b4b768f79f34ddaa538834a53b8e33b_Out_3_Vector3, _Lerp_7404d23a49244d1a8ca1c221f25d981c_Out_3_Vector3, (_GradientNoise_d9ac409c304041c49e680e55eb4d2c81_Out_2_Float.xxx), _Lerp_93b3b392cffa4e688d1b32f6298c4907_Out_3_Vector3);
float3 _UPGFoliageContactPositionCustomFunction_4d51d42c561a43cc976f6db5b768a1c6_bentPositionOS_1_Vector3;
UPG_FoliageContactPosition_float(_Lerp_93b3b392cffa4e688d1b32f6298c4907_Out_3_Vector3, _UPGFoliageContactPositionCustomFunction_4d51d42c561a43cc976f6db5b768a1c6_bentPositionOS_1_Vector3);
description.Position = _UPGFoliageContactPositionCustomFunction_4d51d42c561a43cc976f6db5b768a1c6_bentPositionOS_1_Vector3;
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
UnityTexture2D _Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Texture, sampler_Texture, _Texture_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D.tex, _Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D.samplerstate, _Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4, _Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_R_4_Float = _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4.r;
float _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_G_5_Float = _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4.g;
float _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_B_6_Float = _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4.b;
float _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_A_7_Float = _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4.a;
float _Property_ae6b5992b5874ce1b905e7962780a6be_Out_0_Float = _Alpha_Cutoff;
surface.Alpha = _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_A_7_Float;
surface.AlphaClipThreshold = _Property_ae6b5992b5874ce1b905e7962780a6be_Out_0_Float;
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
    output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
    output.AbsoluteWorldSpacePosition =                 GetAbsolutePositionWS(TransformObjectToWorld(input.positionOS));
    output.uv0 =                                        input.uv0;
    output.VertexColor =                                input.color;
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
#pragma shader_feature _ _SOFT_SHADOWS
#pragma shader_feature_local _COLOR_OVERLAY_VERTEX_POSITION _COLOR_OVERLAY_UV

#if defined(_SOFT_SHADOWS) && defined(_COLOR_OVERLAY_VERTEX_POSITION)
#define KEYWORD_PERMUTATION_0
#elif defined(_SOFT_SHADOWS) && defined(_COLOR_OVERLAY_UV)
#define KEYWORD_PERMUTATION_1
#elif defined(_COLOR_OVERLAY_VERTEX_POSITION)
#define KEYWORD_PERMUTATION_2
#else
#define KEYWORD_PERMUTATION_3
#endif


// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_TEXCOORD1
#define ATTRIBUTES_NEED_COLOR
#define GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
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
 float4 color : COLOR;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
 float4 positionCS : SV_POSITION;
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
 float3 TangentSpaceNormal;
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 WorldSpacePosition;
 float3 AbsoluteWorldSpacePosition;
 float4 uv0;
 float4 VertexColor;
 float3 TimeParameters;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
 float4 tangentWS : INTERP0;
 float4 texCoord0 : INTERP1;
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
output.tangentWS.xyzw = input.tangentWS;
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
output.tangentWS = input.tangentWS.xyzw;
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

float4 _Texture_TexelSize;
float4 _Top_Color;
float _Custom_Color;
float4 _Bottom_Color;
float _Blend_Height;
float _Wind_Strength;
float _Wind_Speed;
float _Wind_Variation;
float2 _Wind_Direction;
float _Alpha_Cutoff;
float _Enable_Top_Color;
float _Start_Bend_Range;
float _Wave_Scale;
float _Bend_Strength;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Texture);
SAMPLER(sampler_Texture);
float3 _Player_Position;

// Graph Includes
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"
#include_with_pragmas "Assets/06.Shaders/Foliage/UPG_FoliageInteraction.hlsl"

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

void Unity_Normalize_float2(float2 In, out float2 Out)
{
    Out = normalize(In);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_Add_float2(float2 A, float2 B, out float2 Out)
{
    Out = A + B;
}

float2 Unity_GradientNoise_Deterministic_Dir_float(float2 p)
{
float x; Hash_Tchou_2_1_float(p, x);
return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
}

void Unity_GradientNoise_Deterministic_float (float2 UV, float3 Scale, out float Out)
{
float2 p = UV * Scale.xy;
float2 ip = floor(p);
float2 fp = frac(p);
float d00 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip), fp);
float d01 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(0, 1)), fp - float2(0, 1));
float d10 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(1, 0)), fp - float2(1, 0));
float d11 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(1, 1)), fp - float2(1, 1));
fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
Out = lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5;
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Normalize_float3(float3 In, out float3 Out)
{
    Out = normalize(In);
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
{
    Out = lerp(A, B, T);
}

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
}

void Unity_Subtract_float3(float3 A, float3 B, out float3 Out)
{
    Out = A - B;
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Distance_float3(float3 A, float3 B, out float Out)
{
    Out = distance(A, B);
}

void Unity_Clamp_float(float In, float Min, float Max, out float Out)
{
    Out = clamp(In, Min, Max);
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
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
float _Split_f9cfb9a555e949379da9876f8d3844b2_R_1_Float = IN.WorldSpacePosition[0];
float _Split_f9cfb9a555e949379da9876f8d3844b2_G_2_Float = IN.WorldSpacePosition[1];
float _Split_f9cfb9a555e949379da9876f8d3844b2_B_3_Float = IN.WorldSpacePosition[2];
float _Split_f9cfb9a555e949379da9876f8d3844b2_A_4_Float = 0;
float2 _Vector2_feeecec8507c4619844c3ca15ea244be_Out_0_Vector2 = float2(_Split_f9cfb9a555e949379da9876f8d3844b2_R_1_Float, _Split_f9cfb9a555e949379da9876f8d3844b2_B_3_Float);
float2 _Property_186d136719274f03b9464e4e1d37a4e8_Out_0_Vector2 = _Wind_Direction;
float2 _Normalize_fb934df0b0c946039db4091949a06211_Out_1_Vector2;
Unity_Normalize_float2(_Property_186d136719274f03b9464e4e1d37a4e8_Out_0_Vector2, _Normalize_fb934df0b0c946039db4091949a06211_Out_1_Vector2);
float _Property_a98f7a88c8ac40109941c7f4f4211495_Out_0_Float = _Wind_Speed;
float _Multiply_880934a6c2a44f658fe3c3b3ed7cccac_Out_2_Float;
Unity_Multiply_float_float(10, _Property_a98f7a88c8ac40109941c7f4f4211495_Out_0_Float, _Multiply_880934a6c2a44f658fe3c3b3ed7cccac_Out_2_Float);
float2 _Multiply_892e7af0e8e54f869ff77c70575af5fc_Out_2_Vector2;
Unity_Multiply_float2_float2(_Normalize_fb934df0b0c946039db4091949a06211_Out_1_Vector2, (_Multiply_880934a6c2a44f658fe3c3b3ed7cccac_Out_2_Float.xx), _Multiply_892e7af0e8e54f869ff77c70575af5fc_Out_2_Vector2);
float2 _Multiply_d96cf1c14ac74322b4498e1849526399_Out_2_Vector2;
Unity_Multiply_float2_float2(_Multiply_892e7af0e8e54f869ff77c70575af5fc_Out_2_Vector2, (IN.TimeParameters.x.xx), _Multiply_d96cf1c14ac74322b4498e1849526399_Out_2_Vector2);
float2 _Add_c52cdcdd15b74d9a963511973a04621c_Out_2_Vector2;
Unity_Add_float2(_Vector2_feeecec8507c4619844c3ca15ea244be_Out_0_Vector2, _Multiply_d96cf1c14ac74322b4498e1849526399_Out_2_Vector2, _Add_c52cdcdd15b74d9a963511973a04621c_Out_2_Vector2);
float _Property_14515a2f6d834bd99002a2ccf491767f_Out_0_Float = _Wave_Scale;
float _Multiply_bbdcafe2ee654e3d8e4df25d9c66103c_Out_2_Float;
Unity_Multiply_float_float(3, _Property_14515a2f6d834bd99002a2ccf491767f_Out_0_Float, _Multiply_bbdcafe2ee654e3d8e4df25d9c66103c_Out_2_Float);
float _GradientNoise_32a9a6c418e44409a86a8238a6b4645c_Out_2_Float;
Unity_GradientNoise_Deterministic_float(_Add_c52cdcdd15b74d9a963511973a04621c_Out_2_Vector2, _Multiply_bbdcafe2ee654e3d8e4df25d9c66103c_Out_2_Float, _GradientNoise_32a9a6c418e44409a86a8238a6b4645c_Out_2_Float);
float _Subtract_3ec4867605194525898104109bc1e4a8_Out_2_Float;
Unity_Subtract_float(_GradientNoise_32a9a6c418e44409a86a8238a6b4645c_Out_2_Float, float(0.5), _Subtract_3ec4867605194525898104109bc1e4a8_Out_2_Float);
float _Property_a52d213096ea41398ec0a409bf7ca764_Out_0_Float = _Wind_Strength;
float _Multiply_444d89a5d2344a9d97e110a245567e62_Out_2_Float;
Unity_Multiply_float_float(2, _Property_a52d213096ea41398ec0a409bf7ca764_Out_0_Float, _Multiply_444d89a5d2344a9d97e110a245567e62_Out_2_Float);
float _Multiply_6a79300791ae4467bc6c5489c0164afa_Out_2_Float;
Unity_Multiply_float_float(_Subtract_3ec4867605194525898104109bc1e4a8_Out_2_Float, _Multiply_444d89a5d2344a9d97e110a245567e62_Out_2_Float, _Multiply_6a79300791ae4467bc6c5489c0164afa_Out_2_Float);
float3 _Normalize_ac3890981ce548259c746c56bd0ca30f_Out_1_Vector3;
Unity_Normalize_float3(IN.ObjectSpacePosition, _Normalize_ac3890981ce548259c746c56bd0ca30f_Out_1_Vector3);
float _Split_ce6b6b6587294130b97b66c5ec3b64a6_R_1_Float = _Normalize_ac3890981ce548259c746c56bd0ca30f_Out_1_Vector3[0];
float _Split_ce6b6b6587294130b97b66c5ec3b64a6_G_2_Float = _Normalize_ac3890981ce548259c746c56bd0ca30f_Out_1_Vector3[1];
float _Split_ce6b6b6587294130b97b66c5ec3b64a6_B_3_Float = _Normalize_ac3890981ce548259c746c56bd0ca30f_Out_1_Vector3[2];
float _Split_ce6b6b6587294130b97b66c5ec3b64a6_A_4_Float = 0;
float _Multiply_453e85026c7c4cc8b7c4661ac5495b5f_Out_2_Float;
Unity_Multiply_float_float(_Multiply_6a79300791ae4467bc6c5489c0164afa_Out_2_Float, _Split_ce6b6b6587294130b97b66c5ec3b64a6_G_2_Float, _Multiply_453e85026c7c4cc8b7c4661ac5495b5f_Out_2_Float);
float3 _Add_526cc646ad64464ba782c997c6d8a2e7_Out_2_Vector3;
Unity_Add_float3((_Multiply_453e85026c7c4cc8b7c4661ac5495b5f_Out_2_Float.xxx), IN.AbsoluteWorldSpacePosition, _Add_526cc646ad64464ba782c997c6d8a2e7_Out_2_Vector3);
float3 _Transform_26034a9482244d0db0cde719848b8793_Out_1_Vector3;
_Transform_26034a9482244d0db0cde719848b8793_Out_1_Vector3 = TransformWorldToObject(_Add_526cc646ad64464ba782c997c6d8a2e7_Out_2_Vector3.xyz);
float _Split_35ccc240162a41a796dd70233f88757c_R_1_Float = IN.VertexColor[0];
float _Split_35ccc240162a41a796dd70233f88757c_G_2_Float = IN.VertexColor[1];
float _Split_35ccc240162a41a796dd70233f88757c_B_3_Float = IN.VertexColor[2];
float _Split_35ccc240162a41a796dd70233f88757c_A_4_Float = IN.VertexColor[3];
float4 _Combine_bef8410c8e24482da5d3364b77236292_RGBA_4_Vector4;
float3 _Combine_bef8410c8e24482da5d3364b77236292_RGB_5_Vector3;
float2 _Combine_bef8410c8e24482da5d3364b77236292_RG_6_Vector2;
Unity_Combine_float(_Split_35ccc240162a41a796dd70233f88757c_R_1_Float, _Split_35ccc240162a41a796dd70233f88757c_G_2_Float, float(0), float(0), _Combine_bef8410c8e24482da5d3364b77236292_RGBA_4_Vector4, _Combine_bef8410c8e24482da5d3364b77236292_RGB_5_Vector3, _Combine_bef8410c8e24482da5d3364b77236292_RG_6_Vector2);
float3 _Lerp_7a7657270ab14fb2acad87e06eb37455_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _Transform_26034a9482244d0db0cde719848b8793_Out_1_Vector3, _Combine_bef8410c8e24482da5d3364b77236292_RGB_5_Vector3, _Lerp_7a7657270ab14fb2acad87e06eb37455_Out_3_Vector3);
float _Property_33225d4ce071408bbef8e7871ca2d298_Out_0_Float = _Wind_Speed;
float _Multiply_f56338cdfe18474dbabd15aebc9c5c2d_Out_2_Float;
Unity_Multiply_float_float(10, _Property_33225d4ce071408bbef8e7871ca2d298_Out_0_Float, _Multiply_f56338cdfe18474dbabd15aebc9c5c2d_Out_2_Float);
float _Multiply_7c3d48cd1d9e4ba6aaee9091a8d8a492_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Multiply_f56338cdfe18474dbabd15aebc9c5c2d_Out_2_Float, _Multiply_7c3d48cd1d9e4ba6aaee9091a8d8a492_Out_2_Float);
float2 _TilingAndOffset_ec707a81514142459d08db9978d86cf5_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, float2 (1, 1), (_Multiply_7c3d48cd1d9e4ba6aaee9091a8d8a492_Out_2_Float.xx), _TilingAndOffset_ec707a81514142459d08db9978d86cf5_Out_3_Vector2);
float _Property_52c7fe0910e04984b1e473c0627107b2_Out_0_Float = _Wind_Variation;
float _Multiply_a9622f7a126646aaa969ee30098cf924_Out_2_Float;
Unity_Multiply_float_float(0.5, _Property_52c7fe0910e04984b1e473c0627107b2_Out_0_Float, _Multiply_a9622f7a126646aaa969ee30098cf924_Out_2_Float);
float _GradientNoise_d9ac409c304041c49e680e55eb4d2c81_Out_2_Float;
Unity_GradientNoise_Deterministic_float(_TilingAndOffset_ec707a81514142459d08db9978d86cf5_Out_3_Vector2, _Multiply_a9622f7a126646aaa969ee30098cf924_Out_2_Float, _GradientNoise_d9ac409c304041c49e680e55eb4d2c81_Out_2_Float);
float3 _Lerp_1b4b768f79f34ddaa538834a53b8e33b_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _Lerp_7a7657270ab14fb2acad87e06eb37455_Out_3_Vector3, (_GradientNoise_d9ac409c304041c49e680e55eb4d2c81_Out_2_Float.xxx), _Lerp_1b4b768f79f34ddaa538834a53b8e33b_Out_3_Vector3);
float3 _Property_413c2dab0db145e686fd951a3cf9b06d_Out_0_Vector3 = _Player_Position;
float3 _Subtract_16caad2935f64d4ca2a5c41c122c2899_Out_2_Vector3;
Unity_Subtract_float3(IN.AbsoluteWorldSpacePosition, _Property_413c2dab0db145e686fd951a3cf9b06d_Out_0_Vector3, _Subtract_16caad2935f64d4ca2a5c41c122c2899_Out_2_Vector3);
float _Split_dd517b5d363349dfbcc6a80463d0a69c_R_1_Float = _Subtract_16caad2935f64d4ca2a5c41c122c2899_Out_2_Vector3[0];
float _Split_dd517b5d363349dfbcc6a80463d0a69c_G_2_Float = _Subtract_16caad2935f64d4ca2a5c41c122c2899_Out_2_Vector3[1];
float _Split_dd517b5d363349dfbcc6a80463d0a69c_B_3_Float = _Subtract_16caad2935f64d4ca2a5c41c122c2899_Out_2_Vector3[2];
float _Split_dd517b5d363349dfbcc6a80463d0a69c_A_4_Float = 0;
float _Property_f88a2d673c09471cad200fa3c1b6ae30_Out_0_Float = _Bend_Strength;
float _Multiply_6c5ecb62197a4848b2fabec6f920ae09_Out_2_Float;
Unity_Multiply_float_float(7, _Property_f88a2d673c09471cad200fa3c1b6ae30_Out_0_Float, _Multiply_6c5ecb62197a4848b2fabec6f920ae09_Out_2_Float);
float _Subtract_fabc7232020f481398d5e1c34e958838_Out_2_Float;
Unity_Subtract_float(_Split_dd517b5d363349dfbcc6a80463d0a69c_G_2_Float, _Multiply_6c5ecb62197a4848b2fabec6f920ae09_Out_2_Float, _Subtract_fabc7232020f481398d5e1c34e958838_Out_2_Float);
float4 _Combine_e1e6724c2316432f9d0cefb980576bd6_RGBA_4_Vector4;
float3 _Combine_e1e6724c2316432f9d0cefb980576bd6_RGB_5_Vector3;
float2 _Combine_e1e6724c2316432f9d0cefb980576bd6_RG_6_Vector2;
Unity_Combine_float(_Split_dd517b5d363349dfbcc6a80463d0a69c_R_1_Float, _Subtract_fabc7232020f481398d5e1c34e958838_Out_2_Float, _Split_dd517b5d363349dfbcc6a80463d0a69c_B_3_Float, float(0), _Combine_e1e6724c2316432f9d0cefb980576bd6_RGBA_4_Vector4, _Combine_e1e6724c2316432f9d0cefb980576bd6_RGB_5_Vector3, _Combine_e1e6724c2316432f9d0cefb980576bd6_RG_6_Vector2);
float3 _Multiply_42e953d7da1c4e7fbe777341488bae83_Out_2_Vector3;
Unity_Multiply_float3_float3(_Combine_e1e6724c2316432f9d0cefb980576bd6_RGB_5_Vector3, (_Property_f88a2d673c09471cad200fa3c1b6ae30_Out_0_Float.xxx), _Multiply_42e953d7da1c4e7fbe777341488bae83_Out_2_Vector3);
float _Property_ad55f894067547979f44b3a570bf7d49_Out_0_Float = _Start_Bend_Range;
float _Multiply_a7265b1673054e0996cb483d2ac3010d_Out_2_Float;
Unity_Multiply_float_float(_Property_ad55f894067547979f44b3a570bf7d49_Out_0_Float, 1.5, _Multiply_a7265b1673054e0996cb483d2ac3010d_Out_2_Float);
float _Distance_41bd00a9f1be4373a1003dc1e109849c_Out_2_Float;
Unity_Distance_float3(_Property_413c2dab0db145e686fd951a3cf9b06d_Out_0_Vector3, IN.AbsoluteWorldSpacePosition, _Distance_41bd00a9f1be4373a1003dc1e109849c_Out_2_Float);
float _Clamp_61013f1b8a7946cabf0f461c9754f564_Out_3_Float;
Unity_Clamp_float(_Distance_41bd00a9f1be4373a1003dc1e109849c_Out_2_Float, float(0), _Multiply_a7265b1673054e0996cb483d2ac3010d_Out_2_Float, _Clamp_61013f1b8a7946cabf0f461c9754f564_Out_3_Float);
float _Subtract_6193cd2daf004574816768560ab361b4_Out_2_Float;
Unity_Subtract_float(_Multiply_a7265b1673054e0996cb483d2ac3010d_Out_2_Float, _Clamp_61013f1b8a7946cabf0f461c9754f564_Out_3_Float, _Subtract_6193cd2daf004574816768560ab361b4_Out_2_Float);
float3 _Multiply_49122dc502124ec0b62c5e8d9df0b640_Out_2_Vector3;
Unity_Multiply_float3_float3(_Multiply_42e953d7da1c4e7fbe777341488bae83_Out_2_Vector3, (_Subtract_6193cd2daf004574816768560ab361b4_Out_2_Float.xxx), _Multiply_49122dc502124ec0b62c5e8d9df0b640_Out_2_Vector3);
float3 _Add_edf7ac44af404fdbbe1d0c50b7bc8b0f_Out_2_Vector3;
Unity_Add_float3(IN.ObjectSpacePosition, _Multiply_49122dc502124ec0b62c5e8d9df0b640_Out_2_Vector3, _Add_edf7ac44af404fdbbe1d0c50b7bc8b0f_Out_2_Vector3);
float3 _Normalize_0901e7953f394045adc844d92565cfba_Out_1_Vector3;
Unity_Normalize_float3(IN.ObjectSpacePosition, _Normalize_0901e7953f394045adc844d92565cfba_Out_1_Vector3);
float _Split_c58f3da36534495d8f4633fe4c40a74c_R_1_Float = _Normalize_0901e7953f394045adc844d92565cfba_Out_1_Vector3[0];
float _Split_c58f3da36534495d8f4633fe4c40a74c_G_2_Float = _Normalize_0901e7953f394045adc844d92565cfba_Out_1_Vector3[1];
float _Split_c58f3da36534495d8f4633fe4c40a74c_B_3_Float = _Normalize_0901e7953f394045adc844d92565cfba_Out_1_Vector3[2];
float _Split_c58f3da36534495d8f4633fe4c40a74c_A_4_Float = 0;
float _OneMinus_5a83b2c5314e45b2a835abb20d70bd22_Out_1_Float;
Unity_OneMinus_float(_Split_c58f3da36534495d8f4633fe4c40a74c_G_2_Float, _OneMinus_5a83b2c5314e45b2a835abb20d70bd22_Out_1_Float);
float _Step_428aa74b40a6492ba1e5a63ecfe4b53e_Out_2_Float;
Unity_Step_float(_OneMinus_5a83b2c5314e45b2a835abb20d70bd22_Out_1_Float, float(1), _Step_428aa74b40a6492ba1e5a63ecfe4b53e_Out_2_Float);
float3 _Lerp_7404d23a49244d1a8ca1c221f25d981c_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _Add_edf7ac44af404fdbbe1d0c50b7bc8b0f_Out_2_Vector3, (_Step_428aa74b40a6492ba1e5a63ecfe4b53e_Out_2_Float.xxx), _Lerp_7404d23a49244d1a8ca1c221f25d981c_Out_3_Vector3);
float3 _Lerp_93b3b392cffa4e688d1b32f6298c4907_Out_3_Vector3;
Unity_Lerp_float3(_Lerp_1b4b768f79f34ddaa538834a53b8e33b_Out_3_Vector3, _Lerp_7404d23a49244d1a8ca1c221f25d981c_Out_3_Vector3, (_GradientNoise_d9ac409c304041c49e680e55eb4d2c81_Out_2_Float.xxx), _Lerp_93b3b392cffa4e688d1b32f6298c4907_Out_3_Vector3);
float3 _UPGFoliageContactPositionCustomFunction_4d51d42c561a43cc976f6db5b768a1c6_bentPositionOS_1_Vector3;
UPG_FoliageContactPosition_float(_Lerp_93b3b392cffa4e688d1b32f6298c4907_Out_3_Vector3, _UPGFoliageContactPositionCustomFunction_4d51d42c561a43cc976f6db5b768a1c6_bentPositionOS_1_Vector3);
description.Position = _UPGFoliageContactPositionCustomFunction_4d51d42c561a43cc976f6db5b768a1c6_bentPositionOS_1_Vector3;
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
UnityTexture2D _Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Texture, sampler_Texture, _Texture_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D.tex, _Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D.samplerstate, _Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4, _Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_R_4_Float = _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4.r;
float _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_G_5_Float = _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4.g;
float _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_B_6_Float = _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4.b;
float _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_A_7_Float = _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4.a;
float _Property_ae6b5992b5874ce1b905e7962780a6be_Out_0_Float = _Alpha_Cutoff;
surface.NormalTS = IN.TangentSpaceNormal;
surface.Alpha = _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_A_7_Float;
surface.AlphaClipThreshold = _Property_ae6b5992b5874ce1b905e7962780a6be_Out_0_Float;
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
    output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
    output.AbsoluteWorldSpacePosition =                 GetAbsolutePositionWS(TransformObjectToWorld(input.positionOS));
    output.uv0 =                                        input.uv0;
    output.VertexColor =                                input.color;
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

    



    output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);



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
#pragma shader_feature_local_fragment _ _ALPHATEST_ON
#pragma shader_feature _ _SOFT_SHADOWS
#pragma shader_feature_local _COLOR_OVERLAY_VERTEX_POSITION _COLOR_OVERLAY_UV

#if defined(_SOFT_SHADOWS) && defined(_COLOR_OVERLAY_VERTEX_POSITION)
#define KEYWORD_PERMUTATION_0
#elif defined(_SOFT_SHADOWS) && defined(_COLOR_OVERLAY_UV)
#define KEYWORD_PERMUTATION_1
#elif defined(_COLOR_OVERLAY_VERTEX_POSITION)
#define KEYWORD_PERMUTATION_2
#else
#define KEYWORD_PERMUTATION_3
#endif


// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_TEXCOORD1
#define ATTRIBUTES_NEED_TEXCOORD2
#define ATTRIBUTES_NEED_TEXCOORD3
#define ATTRIBUTES_NEED_COLOR
#define ATTRIBUTES_NEED_INSTANCEID
#define GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_NORMAL_WS
#define VARYINGS_NEED_TEXCOORD0
#define VARYINGS_NEED_TEXCOORD1
#define VARYINGS_NEED_TEXCOORD2
#define VARYINGS_NEED_TEXCOORD3
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
 float4 uv3 : TEXCOORD3;
 float4 color : COLOR;
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
 float4 texCoord3;
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
 float3 WorldSpacePosition;
 float2 PixelPosition;
 float4 uv0;
 float4 uv1;
 float4 uv2;
 float4 uv3;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 WorldSpacePosition;
 float3 AbsoluteWorldSpacePosition;
 float4 uv0;
 float4 VertexColor;
 float3 TimeParameters;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
 float4 texCoord0 : INTERP0;
 float4 texCoord1 : INTERP1;
 float4 texCoord2 : INTERP2;
 float4 texCoord3 : INTERP3;
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
output.texCoord0.xyzw = input.texCoord0;
output.texCoord1.xyzw = input.texCoord1;
output.texCoord2.xyzw = input.texCoord2;
output.texCoord3.xyzw = input.texCoord3;
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
output.texCoord3 = input.texCoord3.xyzw;
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

float4 _Texture_TexelSize;
float4 _Top_Color;
float _Custom_Color;
float4 _Bottom_Color;
float _Blend_Height;
float _Wind_Strength;
float _Wind_Speed;
float _Wind_Variation;
float2 _Wind_Direction;
float _Alpha_Cutoff;
float _Enable_Top_Color;
float _Start_Bend_Range;
float _Wave_Scale;
float _Bend_Strength;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Texture);
SAMPLER(sampler_Texture);
float3 _Player_Position;

// Graph Includes
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"
#include_with_pragmas "Assets/06.Shaders/Foliage/UPG_FoliageInteraction.hlsl"
#include_with_pragmas "Assets/ExternalAssets/Environment/Idyllic Fantasy Nature/Shader/CustomLighting.hlsl"

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

void Unity_Normalize_float2(float2 In, out float2 Out)
{
    Out = normalize(In);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_Add_float2(float2 A, float2 B, out float2 Out)
{
    Out = A + B;
}

float2 Unity_GradientNoise_Deterministic_Dir_float(float2 p)
{
float x; Hash_Tchou_2_1_float(p, x);
return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
}

void Unity_GradientNoise_Deterministic_float (float2 UV, float3 Scale, out float Out)
{
float2 p = UV * Scale.xy;
float2 ip = floor(p);
float2 fp = frac(p);
float d00 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip), fp);
float d01 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(0, 1)), fp - float2(0, 1));
float d10 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(1, 0)), fp - float2(1, 0));
float d11 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(1, 1)), fp - float2(1, 1));
fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
Out = lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5;
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Normalize_float3(float3 In, out float3 Out)
{
    Out = normalize(In);
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
{
    Out = lerp(A, B, T);
}

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
}

void Unity_Subtract_float3(float3 A, float3 B, out float3 Out)
{
    Out = A - B;
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Distance_float3(float3 A, float3 B, out float Out)
{
    Out = distance(A, B);
}

void Unity_Clamp_float(float In, float Min, float Max, out float Out)
{
    Out = clamp(In, Min, Max);
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
}

void Unity_Step_float(float Edge, float In, out float Out)
{
    Out = step(Edge, In);
}

void Unity_Branch_float4(float Predicate, float4 True, float4 False, out float4 Out)
{
    Out = Predicate ? True : False;
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_SampleGradientV1_float(Gradient Gradient, float Time, out float4 Out)
{
    // convert to OkLab if we need perceptual color space.
    float3 color = lerp(Gradient.colors[0].rgb, LinearToOklab(Gradient.colors[0].rgb), Gradient.type == 2);

    [unroll]
    for (int c = 1; c < Gradient.colorsLength; c++)
    {
        float colorPos = saturate((Time - Gradient.colors[c - 1].w) / (Gradient.colors[c].w - Gradient.colors[c - 1].w)) * step(c, Gradient.colorsLength - 1);
        float3 color2 = lerp(Gradient.colors[c].rgb, LinearToOklab(Gradient.colors[c].rgb), Gradient.type == 2);
        color = lerp(color, color2, lerp(colorPos, step(0.01, colorPos), Gradient.type % 2)); // grad.type == 1 is fixed, 0 and 2 are blends.
    }
    color = lerp(color, OklabToLinear(color), Gradient.type == 2);

#ifdef UNITY_COLORSPACE_GAMMA
    color = LinearToSRGB(color);
#endif

    float alpha = Gradient.alphas[0].x;
    [unroll]
    for (int a = 1; a < Gradient.alphasLength; a++)
    {
        float alphaPos = saturate((Time - Gradient.alphas[a - 1].y) / (Gradient.alphas[a].y - Gradient.alphas[a - 1].y)) * step(a, Gradient.alphasLength - 1);
        alpha = lerp(alpha, Gradient.alphas[a].x, lerp(alphaPos, step(0.01, alphaPos), Gradient.type % 2));
    }

    Out = float4(color, alpha);
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
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
float _Split_f9cfb9a555e949379da9876f8d3844b2_R_1_Float = IN.WorldSpacePosition[0];
float _Split_f9cfb9a555e949379da9876f8d3844b2_G_2_Float = IN.WorldSpacePosition[1];
float _Split_f9cfb9a555e949379da9876f8d3844b2_B_3_Float = IN.WorldSpacePosition[2];
float _Split_f9cfb9a555e949379da9876f8d3844b2_A_4_Float = 0;
float2 _Vector2_feeecec8507c4619844c3ca15ea244be_Out_0_Vector2 = float2(_Split_f9cfb9a555e949379da9876f8d3844b2_R_1_Float, _Split_f9cfb9a555e949379da9876f8d3844b2_B_3_Float);
float2 _Property_186d136719274f03b9464e4e1d37a4e8_Out_0_Vector2 = _Wind_Direction;
float2 _Normalize_fb934df0b0c946039db4091949a06211_Out_1_Vector2;
Unity_Normalize_float2(_Property_186d136719274f03b9464e4e1d37a4e8_Out_0_Vector2, _Normalize_fb934df0b0c946039db4091949a06211_Out_1_Vector2);
float _Property_a98f7a88c8ac40109941c7f4f4211495_Out_0_Float = _Wind_Speed;
float _Multiply_880934a6c2a44f658fe3c3b3ed7cccac_Out_2_Float;
Unity_Multiply_float_float(10, _Property_a98f7a88c8ac40109941c7f4f4211495_Out_0_Float, _Multiply_880934a6c2a44f658fe3c3b3ed7cccac_Out_2_Float);
float2 _Multiply_892e7af0e8e54f869ff77c70575af5fc_Out_2_Vector2;
Unity_Multiply_float2_float2(_Normalize_fb934df0b0c946039db4091949a06211_Out_1_Vector2, (_Multiply_880934a6c2a44f658fe3c3b3ed7cccac_Out_2_Float.xx), _Multiply_892e7af0e8e54f869ff77c70575af5fc_Out_2_Vector2);
float2 _Multiply_d96cf1c14ac74322b4498e1849526399_Out_2_Vector2;
Unity_Multiply_float2_float2(_Multiply_892e7af0e8e54f869ff77c70575af5fc_Out_2_Vector2, (IN.TimeParameters.x.xx), _Multiply_d96cf1c14ac74322b4498e1849526399_Out_2_Vector2);
float2 _Add_c52cdcdd15b74d9a963511973a04621c_Out_2_Vector2;
Unity_Add_float2(_Vector2_feeecec8507c4619844c3ca15ea244be_Out_0_Vector2, _Multiply_d96cf1c14ac74322b4498e1849526399_Out_2_Vector2, _Add_c52cdcdd15b74d9a963511973a04621c_Out_2_Vector2);
float _Property_14515a2f6d834bd99002a2ccf491767f_Out_0_Float = _Wave_Scale;
float _Multiply_bbdcafe2ee654e3d8e4df25d9c66103c_Out_2_Float;
Unity_Multiply_float_float(3, _Property_14515a2f6d834bd99002a2ccf491767f_Out_0_Float, _Multiply_bbdcafe2ee654e3d8e4df25d9c66103c_Out_2_Float);
float _GradientNoise_32a9a6c418e44409a86a8238a6b4645c_Out_2_Float;
Unity_GradientNoise_Deterministic_float(_Add_c52cdcdd15b74d9a963511973a04621c_Out_2_Vector2, _Multiply_bbdcafe2ee654e3d8e4df25d9c66103c_Out_2_Float, _GradientNoise_32a9a6c418e44409a86a8238a6b4645c_Out_2_Float);
float _Subtract_3ec4867605194525898104109bc1e4a8_Out_2_Float;
Unity_Subtract_float(_GradientNoise_32a9a6c418e44409a86a8238a6b4645c_Out_2_Float, float(0.5), _Subtract_3ec4867605194525898104109bc1e4a8_Out_2_Float);
float _Property_a52d213096ea41398ec0a409bf7ca764_Out_0_Float = _Wind_Strength;
float _Multiply_444d89a5d2344a9d97e110a245567e62_Out_2_Float;
Unity_Multiply_float_float(2, _Property_a52d213096ea41398ec0a409bf7ca764_Out_0_Float, _Multiply_444d89a5d2344a9d97e110a245567e62_Out_2_Float);
float _Multiply_6a79300791ae4467bc6c5489c0164afa_Out_2_Float;
Unity_Multiply_float_float(_Subtract_3ec4867605194525898104109bc1e4a8_Out_2_Float, _Multiply_444d89a5d2344a9d97e110a245567e62_Out_2_Float, _Multiply_6a79300791ae4467bc6c5489c0164afa_Out_2_Float);
float3 _Normalize_ac3890981ce548259c746c56bd0ca30f_Out_1_Vector3;
Unity_Normalize_float3(IN.ObjectSpacePosition, _Normalize_ac3890981ce548259c746c56bd0ca30f_Out_1_Vector3);
float _Split_ce6b6b6587294130b97b66c5ec3b64a6_R_1_Float = _Normalize_ac3890981ce548259c746c56bd0ca30f_Out_1_Vector3[0];
float _Split_ce6b6b6587294130b97b66c5ec3b64a6_G_2_Float = _Normalize_ac3890981ce548259c746c56bd0ca30f_Out_1_Vector3[1];
float _Split_ce6b6b6587294130b97b66c5ec3b64a6_B_3_Float = _Normalize_ac3890981ce548259c746c56bd0ca30f_Out_1_Vector3[2];
float _Split_ce6b6b6587294130b97b66c5ec3b64a6_A_4_Float = 0;
float _Multiply_453e85026c7c4cc8b7c4661ac5495b5f_Out_2_Float;
Unity_Multiply_float_float(_Multiply_6a79300791ae4467bc6c5489c0164afa_Out_2_Float, _Split_ce6b6b6587294130b97b66c5ec3b64a6_G_2_Float, _Multiply_453e85026c7c4cc8b7c4661ac5495b5f_Out_2_Float);
float3 _Add_526cc646ad64464ba782c997c6d8a2e7_Out_2_Vector3;
Unity_Add_float3((_Multiply_453e85026c7c4cc8b7c4661ac5495b5f_Out_2_Float.xxx), IN.AbsoluteWorldSpacePosition, _Add_526cc646ad64464ba782c997c6d8a2e7_Out_2_Vector3);
float3 _Transform_26034a9482244d0db0cde719848b8793_Out_1_Vector3;
_Transform_26034a9482244d0db0cde719848b8793_Out_1_Vector3 = TransformWorldToObject(_Add_526cc646ad64464ba782c997c6d8a2e7_Out_2_Vector3.xyz);
float _Split_35ccc240162a41a796dd70233f88757c_R_1_Float = IN.VertexColor[0];
float _Split_35ccc240162a41a796dd70233f88757c_G_2_Float = IN.VertexColor[1];
float _Split_35ccc240162a41a796dd70233f88757c_B_3_Float = IN.VertexColor[2];
float _Split_35ccc240162a41a796dd70233f88757c_A_4_Float = IN.VertexColor[3];
float4 _Combine_bef8410c8e24482da5d3364b77236292_RGBA_4_Vector4;
float3 _Combine_bef8410c8e24482da5d3364b77236292_RGB_5_Vector3;
float2 _Combine_bef8410c8e24482da5d3364b77236292_RG_6_Vector2;
Unity_Combine_float(_Split_35ccc240162a41a796dd70233f88757c_R_1_Float, _Split_35ccc240162a41a796dd70233f88757c_G_2_Float, float(0), float(0), _Combine_bef8410c8e24482da5d3364b77236292_RGBA_4_Vector4, _Combine_bef8410c8e24482da5d3364b77236292_RGB_5_Vector3, _Combine_bef8410c8e24482da5d3364b77236292_RG_6_Vector2);
float3 _Lerp_7a7657270ab14fb2acad87e06eb37455_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _Transform_26034a9482244d0db0cde719848b8793_Out_1_Vector3, _Combine_bef8410c8e24482da5d3364b77236292_RGB_5_Vector3, _Lerp_7a7657270ab14fb2acad87e06eb37455_Out_3_Vector3);
float _Property_33225d4ce071408bbef8e7871ca2d298_Out_0_Float = _Wind_Speed;
float _Multiply_f56338cdfe18474dbabd15aebc9c5c2d_Out_2_Float;
Unity_Multiply_float_float(10, _Property_33225d4ce071408bbef8e7871ca2d298_Out_0_Float, _Multiply_f56338cdfe18474dbabd15aebc9c5c2d_Out_2_Float);
float _Multiply_7c3d48cd1d9e4ba6aaee9091a8d8a492_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Multiply_f56338cdfe18474dbabd15aebc9c5c2d_Out_2_Float, _Multiply_7c3d48cd1d9e4ba6aaee9091a8d8a492_Out_2_Float);
float2 _TilingAndOffset_ec707a81514142459d08db9978d86cf5_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, float2 (1, 1), (_Multiply_7c3d48cd1d9e4ba6aaee9091a8d8a492_Out_2_Float.xx), _TilingAndOffset_ec707a81514142459d08db9978d86cf5_Out_3_Vector2);
float _Property_52c7fe0910e04984b1e473c0627107b2_Out_0_Float = _Wind_Variation;
float _Multiply_a9622f7a126646aaa969ee30098cf924_Out_2_Float;
Unity_Multiply_float_float(0.5, _Property_52c7fe0910e04984b1e473c0627107b2_Out_0_Float, _Multiply_a9622f7a126646aaa969ee30098cf924_Out_2_Float);
float _GradientNoise_d9ac409c304041c49e680e55eb4d2c81_Out_2_Float;
Unity_GradientNoise_Deterministic_float(_TilingAndOffset_ec707a81514142459d08db9978d86cf5_Out_3_Vector2, _Multiply_a9622f7a126646aaa969ee30098cf924_Out_2_Float, _GradientNoise_d9ac409c304041c49e680e55eb4d2c81_Out_2_Float);
float3 _Lerp_1b4b768f79f34ddaa538834a53b8e33b_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _Lerp_7a7657270ab14fb2acad87e06eb37455_Out_3_Vector3, (_GradientNoise_d9ac409c304041c49e680e55eb4d2c81_Out_2_Float.xxx), _Lerp_1b4b768f79f34ddaa538834a53b8e33b_Out_3_Vector3);
float3 _Property_413c2dab0db145e686fd951a3cf9b06d_Out_0_Vector3 = _Player_Position;
float3 _Subtract_16caad2935f64d4ca2a5c41c122c2899_Out_2_Vector3;
Unity_Subtract_float3(IN.AbsoluteWorldSpacePosition, _Property_413c2dab0db145e686fd951a3cf9b06d_Out_0_Vector3, _Subtract_16caad2935f64d4ca2a5c41c122c2899_Out_2_Vector3);
float _Split_dd517b5d363349dfbcc6a80463d0a69c_R_1_Float = _Subtract_16caad2935f64d4ca2a5c41c122c2899_Out_2_Vector3[0];
float _Split_dd517b5d363349dfbcc6a80463d0a69c_G_2_Float = _Subtract_16caad2935f64d4ca2a5c41c122c2899_Out_2_Vector3[1];
float _Split_dd517b5d363349dfbcc6a80463d0a69c_B_3_Float = _Subtract_16caad2935f64d4ca2a5c41c122c2899_Out_2_Vector3[2];
float _Split_dd517b5d363349dfbcc6a80463d0a69c_A_4_Float = 0;
float _Property_f88a2d673c09471cad200fa3c1b6ae30_Out_0_Float = _Bend_Strength;
float _Multiply_6c5ecb62197a4848b2fabec6f920ae09_Out_2_Float;
Unity_Multiply_float_float(7, _Property_f88a2d673c09471cad200fa3c1b6ae30_Out_0_Float, _Multiply_6c5ecb62197a4848b2fabec6f920ae09_Out_2_Float);
float _Subtract_fabc7232020f481398d5e1c34e958838_Out_2_Float;
Unity_Subtract_float(_Split_dd517b5d363349dfbcc6a80463d0a69c_G_2_Float, _Multiply_6c5ecb62197a4848b2fabec6f920ae09_Out_2_Float, _Subtract_fabc7232020f481398d5e1c34e958838_Out_2_Float);
float4 _Combine_e1e6724c2316432f9d0cefb980576bd6_RGBA_4_Vector4;
float3 _Combine_e1e6724c2316432f9d0cefb980576bd6_RGB_5_Vector3;
float2 _Combine_e1e6724c2316432f9d0cefb980576bd6_RG_6_Vector2;
Unity_Combine_float(_Split_dd517b5d363349dfbcc6a80463d0a69c_R_1_Float, _Subtract_fabc7232020f481398d5e1c34e958838_Out_2_Float, _Split_dd517b5d363349dfbcc6a80463d0a69c_B_3_Float, float(0), _Combine_e1e6724c2316432f9d0cefb980576bd6_RGBA_4_Vector4, _Combine_e1e6724c2316432f9d0cefb980576bd6_RGB_5_Vector3, _Combine_e1e6724c2316432f9d0cefb980576bd6_RG_6_Vector2);
float3 _Multiply_42e953d7da1c4e7fbe777341488bae83_Out_2_Vector3;
Unity_Multiply_float3_float3(_Combine_e1e6724c2316432f9d0cefb980576bd6_RGB_5_Vector3, (_Property_f88a2d673c09471cad200fa3c1b6ae30_Out_0_Float.xxx), _Multiply_42e953d7da1c4e7fbe777341488bae83_Out_2_Vector3);
float _Property_ad55f894067547979f44b3a570bf7d49_Out_0_Float = _Start_Bend_Range;
float _Multiply_a7265b1673054e0996cb483d2ac3010d_Out_2_Float;
Unity_Multiply_float_float(_Property_ad55f894067547979f44b3a570bf7d49_Out_0_Float, 1.5, _Multiply_a7265b1673054e0996cb483d2ac3010d_Out_2_Float);
float _Distance_41bd00a9f1be4373a1003dc1e109849c_Out_2_Float;
Unity_Distance_float3(_Property_413c2dab0db145e686fd951a3cf9b06d_Out_0_Vector3, IN.AbsoluteWorldSpacePosition, _Distance_41bd00a9f1be4373a1003dc1e109849c_Out_2_Float);
float _Clamp_61013f1b8a7946cabf0f461c9754f564_Out_3_Float;
Unity_Clamp_float(_Distance_41bd00a9f1be4373a1003dc1e109849c_Out_2_Float, float(0), _Multiply_a7265b1673054e0996cb483d2ac3010d_Out_2_Float, _Clamp_61013f1b8a7946cabf0f461c9754f564_Out_3_Float);
float _Subtract_6193cd2daf004574816768560ab361b4_Out_2_Float;
Unity_Subtract_float(_Multiply_a7265b1673054e0996cb483d2ac3010d_Out_2_Float, _Clamp_61013f1b8a7946cabf0f461c9754f564_Out_3_Float, _Subtract_6193cd2daf004574816768560ab361b4_Out_2_Float);
float3 _Multiply_49122dc502124ec0b62c5e8d9df0b640_Out_2_Vector3;
Unity_Multiply_float3_float3(_Multiply_42e953d7da1c4e7fbe777341488bae83_Out_2_Vector3, (_Subtract_6193cd2daf004574816768560ab361b4_Out_2_Float.xxx), _Multiply_49122dc502124ec0b62c5e8d9df0b640_Out_2_Vector3);
float3 _Add_edf7ac44af404fdbbe1d0c50b7bc8b0f_Out_2_Vector3;
Unity_Add_float3(IN.ObjectSpacePosition, _Multiply_49122dc502124ec0b62c5e8d9df0b640_Out_2_Vector3, _Add_edf7ac44af404fdbbe1d0c50b7bc8b0f_Out_2_Vector3);
float3 _Normalize_0901e7953f394045adc844d92565cfba_Out_1_Vector3;
Unity_Normalize_float3(IN.ObjectSpacePosition, _Normalize_0901e7953f394045adc844d92565cfba_Out_1_Vector3);
float _Split_c58f3da36534495d8f4633fe4c40a74c_R_1_Float = _Normalize_0901e7953f394045adc844d92565cfba_Out_1_Vector3[0];
float _Split_c58f3da36534495d8f4633fe4c40a74c_G_2_Float = _Normalize_0901e7953f394045adc844d92565cfba_Out_1_Vector3[1];
float _Split_c58f3da36534495d8f4633fe4c40a74c_B_3_Float = _Normalize_0901e7953f394045adc844d92565cfba_Out_1_Vector3[2];
float _Split_c58f3da36534495d8f4633fe4c40a74c_A_4_Float = 0;
float _OneMinus_5a83b2c5314e45b2a835abb20d70bd22_Out_1_Float;
Unity_OneMinus_float(_Split_c58f3da36534495d8f4633fe4c40a74c_G_2_Float, _OneMinus_5a83b2c5314e45b2a835abb20d70bd22_Out_1_Float);
float _Step_428aa74b40a6492ba1e5a63ecfe4b53e_Out_2_Float;
Unity_Step_float(_OneMinus_5a83b2c5314e45b2a835abb20d70bd22_Out_1_Float, float(1), _Step_428aa74b40a6492ba1e5a63ecfe4b53e_Out_2_Float);
float3 _Lerp_7404d23a49244d1a8ca1c221f25d981c_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _Add_edf7ac44af404fdbbe1d0c50b7bc8b0f_Out_2_Vector3, (_Step_428aa74b40a6492ba1e5a63ecfe4b53e_Out_2_Float.xxx), _Lerp_7404d23a49244d1a8ca1c221f25d981c_Out_3_Vector3);
float3 _Lerp_93b3b392cffa4e688d1b32f6298c4907_Out_3_Vector3;
Unity_Lerp_float3(_Lerp_1b4b768f79f34ddaa538834a53b8e33b_Out_3_Vector3, _Lerp_7404d23a49244d1a8ca1c221f25d981c_Out_3_Vector3, (_GradientNoise_d9ac409c304041c49e680e55eb4d2c81_Out_2_Float.xxx), _Lerp_93b3b392cffa4e688d1b32f6298c4907_Out_3_Vector3);
float3 _UPGFoliageContactPositionCustomFunction_4d51d42c561a43cc976f6db5b768a1c6_bentPositionOS_1_Vector3;
UPG_FoliageContactPosition_float(_Lerp_93b3b392cffa4e688d1b32f6298c4907_Out_3_Vector3, _UPGFoliageContactPositionCustomFunction_4d51d42c561a43cc976f6db5b768a1c6_bentPositionOS_1_Vector3);
description.Position = _UPGFoliageContactPositionCustomFunction_4d51d42c561a43cc976f6db5b768a1c6_bentPositionOS_1_Vector3;
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
float _Property_8b643d04e791417ea5e53348201c0ea6_Out_0_Boolean = _Custom_Color;
UnityTexture2D _Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Texture, sampler_Texture, _Texture_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D.tex, _Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D.samplerstate, _Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4, _Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_R_4_Float = _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4.r;
float _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_G_5_Float = _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4.g;
float _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_B_6_Float = _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4.b;
float _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_A_7_Float = _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4.a;
float4 _Property_7982990a264d4e2dbcf380e5300b2e9a_Out_0_Vector4 = _Bottom_Color;
float _Property_9a45de6268b24c2cbb15e019aec9d0a8_Out_0_Boolean = _Enable_Top_Color;
float4 _Property_c49d73f7afd34ab2876785e930e60382_Out_0_Vector4 = _Top_Color;
float4 _Branch_5e35ed072da74a36a2b8cab58fc4a5ba_Out_3_Vector4;
Unity_Branch_float4(_Property_9a45de6268b24c2cbb15e019aec9d0a8_Out_0_Boolean, _Property_c49d73f7afd34ab2876785e930e60382_Out_0_Vector4, _Property_7982990a264d4e2dbcf380e5300b2e9a_Out_0_Vector4, _Branch_5e35ed072da74a36a2b8cab58fc4a5ba_Out_3_Vector4);
float _Split_c37efc3b7dc1496597aacca8227df00e_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_c37efc3b7dc1496597aacca8227df00e_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_c37efc3b7dc1496597aacca8227df00e_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_c37efc3b7dc1496597aacca8227df00e_A_4_Float = 0;
float _Property_05ac7e14eb254977a8f86421bf9d54b2_Out_0_Float = _Blend_Height;
float _Multiply_4757f6b5ea5c447a87e4cc7e8f0be506_Out_2_Float;
Unity_Multiply_float_float(_Property_05ac7e14eb254977a8f86421bf9d54b2_Out_0_Float, 2, _Multiply_4757f6b5ea5c447a87e4cc7e8f0be506_Out_2_Float);
float _Multiply_494d37dbb2a44ddab3ff80336acce43b_Out_2_Float;
Unity_Multiply_float_float(_Split_c37efc3b7dc1496597aacca8227df00e_G_2_Float, _Multiply_4757f6b5ea5c447a87e4cc7e8f0be506_Out_2_Float, _Multiply_494d37dbb2a44ddab3ff80336acce43b_Out_2_Float);
float4 _UV_01e7dab03cc840b880067e9fcb6e2eb4_Out_0_Vector4 = IN.uv3;
float _Split_20e6d7d5918d4a7e8602a4c891473154_R_1_Float = _UV_01e7dab03cc840b880067e9fcb6e2eb4_Out_0_Vector4[0];
float _Split_20e6d7d5918d4a7e8602a4c891473154_G_2_Float = _UV_01e7dab03cc840b880067e9fcb6e2eb4_Out_0_Vector4[1];
float _Split_20e6d7d5918d4a7e8602a4c891473154_B_3_Float = _UV_01e7dab03cc840b880067e9fcb6e2eb4_Out_0_Vector4[2];
float _Split_20e6d7d5918d4a7e8602a4c891473154_A_4_Float = _UV_01e7dab03cc840b880067e9fcb6e2eb4_Out_0_Vector4[3];
float _Property_437c7bb5f9284c4e8114d28bc5622db1_Out_0_Float = _Blend_Height;
float _Multiply_932db43fdbd54ea2aa00741583b905df_Out_2_Float;
Unity_Multiply_float_float(_Property_437c7bb5f9284c4e8114d28bc5622db1_Out_0_Float, 5, _Multiply_932db43fdbd54ea2aa00741583b905df_Out_2_Float);
float _Multiply_852392faaedd4eee838a104fc74a9c2c_Out_2_Float;
Unity_Multiply_float_float(_Split_20e6d7d5918d4a7e8602a4c891473154_G_2_Float, _Multiply_932db43fdbd54ea2aa00741583b905df_Out_2_Float, _Multiply_852392faaedd4eee838a104fc74a9c2c_Out_2_Float);
#if defined(_COLOR_OVERLAY_VERTEX_POSITION)
float _ColorOverlay_0a0435a559c340b58c2ac6b4f740704f_Out_0_Float = _Multiply_494d37dbb2a44ddab3ff80336acce43b_Out_2_Float;
#else
float _ColorOverlay_0a0435a559c340b58c2ac6b4f740704f_Out_0_Float = _Multiply_852392faaedd4eee838a104fc74a9c2c_Out_2_Float;
#endif
float4 _Lerp_e82167542650450cae46c81096c7eb35_Out_3_Vector4;
Unity_Lerp_float4(_Property_7982990a264d4e2dbcf380e5300b2e9a_Out_0_Vector4, _Branch_5e35ed072da74a36a2b8cab58fc4a5ba_Out_3_Vector4, (_ColorOverlay_0a0435a559c340b58c2ac6b4f740704f_Out_0_Float.xxxx), _Lerp_e82167542650450cae46c81096c7eb35_Out_3_Vector4);
float3 _BakedGI_2f01eef6099b4badbf6e4fa34812d46c_Out_1_Vector3 = SHADERGRAPH_BAKED_GI(IN.WorldSpacePosition, IN.WorldSpaceNormal, IN.PixelPosition.xy, IN.uv1.xy, IN.uv2.xy, true);
float4 _SampleGradient_f13af0799e57479ab4f197dc8eb051f0_Out_2_Vector4;
Unity_SampleGradientV1_float(NewGradient(1, 2, 2, float4(0.6981132, 0.6981132, 0.6981132, 0.4588235),float4(1, 1, 1, 1),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0), float2(1, 0),float2(1, 1),float2(0, 0),float2(0, 0),float2(0, 0),float2(0, 0),float2(0, 0),float2(0, 0)), (_BakedGI_2f01eef6099b4badbf6e4fa34812d46c_Out_1_Vector3).x, _SampleGradient_f13af0799e57479ab4f197dc8eb051f0_Out_2_Vector4);
half3 _MainLightCustomFunction_5e0203c89a8c45fbb4222b62bf493a45_Direction_1_Vector3;
half3 _MainLightCustomFunction_5e0203c89a8c45fbb4222b62bf493a45_Color_2_Vector3;
half _MainLightCustomFunction_5e0203c89a8c45fbb4222b62bf493a45_DistanceAtten_3_Float;
half _MainLightCustomFunction_5e0203c89a8c45fbb4222b62bf493a45_ShadowAtten_4_Float;
MainLight_half(IN.WorldSpacePosition, _MainLightCustomFunction_5e0203c89a8c45fbb4222b62bf493a45_Direction_1_Vector3, _MainLightCustomFunction_5e0203c89a8c45fbb4222b62bf493a45_Color_2_Vector3, _MainLightCustomFunction_5e0203c89a8c45fbb4222b62bf493a45_DistanceAtten_3_Float, _MainLightCustomFunction_5e0203c89a8c45fbb4222b62bf493a45_ShadowAtten_4_Float);
float4 _SampleGradient_3a0a1463fe4a4a6692d7fa4e255d7c3a_Out_2_Vector4;
Unity_SampleGradientV1_float(NewGradient(1, 2, 2, float4(0.254717, 0.254717, 0.254717, 0.4558785),float4(1, 1, 1, 1),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0), float2(1, 0),float2(1, 1),float2(0, 0),float2(0, 0),float2(0, 0),float2(0, 0),float2(0, 0),float2(0, 0)), _MainLightCustomFunction_5e0203c89a8c45fbb4222b62bf493a45_ShadowAtten_4_Float, _SampleGradient_3a0a1463fe4a4a6692d7fa4e255d7c3a_Out_2_Vector4);
float4 _Multiply_bcc809cfe08a42839838a46b671101d2_Out_2_Vector4;
Unity_Multiply_float4_float4(_SampleGradient_f13af0799e57479ab4f197dc8eb051f0_Out_2_Vector4, _SampleGradient_3a0a1463fe4a4a6692d7fa4e255d7c3a_Out_2_Vector4, _Multiply_bcc809cfe08a42839838a46b671101d2_Out_2_Vector4);
float4 _SampleGradient_c0f24675e9d44702be0700da614645ba_Out_2_Vector4;
Unity_SampleGradientV1_float(NewGradient(1, 2, 2, float4(0.3867925, 0.3867925, 0.3867925, 0.4911726),float4(1, 1, 1, 1),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0), float2(1, 0),float2(1, 1),float2(0, 0),float2(0, 0),float2(0, 0),float2(0, 0),float2(0, 0),float2(0, 0)), _MainLightCustomFunction_5e0203c89a8c45fbb4222b62bf493a45_ShadowAtten_4_Float, _SampleGradient_c0f24675e9d44702be0700da614645ba_Out_2_Vector4);
float3 _BakedGI_bb2649c5aa9a4f17aa739bb15e26f960_Out_1_Vector3 = SHADERGRAPH_BAKED_GI(IN.WorldSpacePosition, IN.WorldSpaceNormal, IN.PixelPosition.xy, IN.uv1.xy, IN.uv2.xy, true);
float3 _Multiply_fb1104e4e9c34a879c179852a1ae32b6_Out_2_Vector3;
Unity_Multiply_float3_float3((_SampleGradient_c0f24675e9d44702be0700da614645ba_Out_2_Vector4.xyz), _BakedGI_bb2649c5aa9a4f17aa739bb15e26f960_Out_1_Vector3, _Multiply_fb1104e4e9c34a879c179852a1ae32b6_Out_2_Vector3);
float3 _Add_2e5b88ada4614963848a47def889a017_Out_2_Vector3;
Unity_Add_float3((_Multiply_bcc809cfe08a42839838a46b671101d2_Out_2_Vector4.xyz), _Multiply_fb1104e4e9c34a879c179852a1ae32b6_Out_2_Vector3, _Add_2e5b88ada4614963848a47def889a017_Out_2_Vector3);
float3 _Multiply_d9af4f78e0384e598b9b64ba704f86ed_Out_2_Vector3;
Unity_Multiply_float3_float3((_Lerp_e82167542650450cae46c81096c7eb35_Out_3_Vector4.xyz), _Add_2e5b88ada4614963848a47def889a017_Out_2_Vector3, _Multiply_d9af4f78e0384e598b9b64ba704f86ed_Out_2_Vector3);
float3 _Multiply_db27607157ad40e1bc8f503d6917a1e4_Out_2_Vector3;
Unity_Multiply_float3_float3((_SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4.xyz), _Multiply_d9af4f78e0384e598b9b64ba704f86ed_Out_2_Vector3, _Multiply_db27607157ad40e1bc8f503d6917a1e4_Out_2_Vector3);
float3 _Multiply_117a3293c8724edbbbfb3e0e208d0dd8_Out_2_Vector3;
Unity_Multiply_float3_float3((_SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4.xyz), _Add_2e5b88ada4614963848a47def889a017_Out_2_Vector3, _Multiply_117a3293c8724edbbbfb3e0e208d0dd8_Out_2_Vector3);
float3 _Branch_c35c890039964b6d84d86513b7a90a46_Out_3_Vector3;
Unity_Branch_float3(_Property_8b643d04e791417ea5e53348201c0ea6_Out_0_Boolean, _Multiply_db27607157ad40e1bc8f503d6917a1e4_Out_2_Vector3, _Multiply_117a3293c8724edbbbfb3e0e208d0dd8_Out_2_Vector3, _Branch_c35c890039964b6d84d86513b7a90a46_Out_3_Vector3);
float _Property_ae6b5992b5874ce1b905e7962780a6be_Out_0_Float = _Alpha_Cutoff;
surface.BaseColor = IsGammaSpace() ? float3(0, 0, 0) : SRGBToLinear(float3(0, 0, 0));
surface.Emission = _Branch_c35c890039964b6d84d86513b7a90a46_Out_3_Vector3;
surface.Alpha = _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_A_7_Float;
surface.AlphaClipThreshold = _Property_ae6b5992b5874ce1b905e7962780a6be_Out_0_Float;
float3 reactionUnusedColor = 0, reactionUnusedEmission = 0;
ClipVegetationFuel(IN.WorldSpacePosition);
ApplyVegetationReaction(IN.WorldSpacePosition,surface.BaseColor,surface.Emission);
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
    output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
    output.AbsoluteWorldSpacePosition =                 GetAbsolutePositionWS(TransformObjectToWorld(input.positionOS));
    output.uv0 =                                        input.uv0;
    output.VertexColor =                                input.color;
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


    output.WorldSpacePosition = input.positionWS;
    output.ObjectSpacePosition = TransformWorldToObject(input.positionWS);

    #if UNITY_UV_STARTS_AT_TOP
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #else
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #endif


    output.uv0 = input.texCoord0;
    output.uv1 = input.texCoord1;
    output.uv2 = input.texCoord2;
    output.uv3 = input.texCoord3;
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
#pragma shader_feature _ _SOFT_SHADOWS
#pragma shader_feature_local _COLOR_OVERLAY_VERTEX_POSITION _COLOR_OVERLAY_UV

#if defined(_SOFT_SHADOWS) && defined(_COLOR_OVERLAY_VERTEX_POSITION)
#define KEYWORD_PERMUTATION_0
#elif defined(_SOFT_SHADOWS) && defined(_COLOR_OVERLAY_UV)
#define KEYWORD_PERMUTATION_1
#elif defined(_COLOR_OVERLAY_VERTEX_POSITION)
#define KEYWORD_PERMUTATION_2
#else
#define KEYWORD_PERMUTATION_3
#endif


// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_COLOR
#define GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT
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
 float4 color : COLOR;
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
 float3 WorldSpacePosition;
 float3 AbsoluteWorldSpacePosition;
 float4 uv0;
 float4 VertexColor;
 float3 TimeParameters;
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

float4 _Texture_TexelSize;
float4 _Top_Color;
float _Custom_Color;
float4 _Bottom_Color;
float _Blend_Height;
float _Wind_Strength;
float _Wind_Speed;
float _Wind_Variation;
float2 _Wind_Direction;
float _Alpha_Cutoff;
float _Enable_Top_Color;
float _Start_Bend_Range;
float _Wave_Scale;
float _Bend_Strength;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Texture);
SAMPLER(sampler_Texture);
float3 _Player_Position;

// Graph Includes
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"
#include_with_pragmas "Assets/06.Shaders/Foliage/UPG_FoliageInteraction.hlsl"

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

void Unity_Normalize_float2(float2 In, out float2 Out)
{
    Out = normalize(In);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_Add_float2(float2 A, float2 B, out float2 Out)
{
    Out = A + B;
}

float2 Unity_GradientNoise_Deterministic_Dir_float(float2 p)
{
float x; Hash_Tchou_2_1_float(p, x);
return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
}

void Unity_GradientNoise_Deterministic_float (float2 UV, float3 Scale, out float Out)
{
float2 p = UV * Scale.xy;
float2 ip = floor(p);
float2 fp = frac(p);
float d00 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip), fp);
float d01 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(0, 1)), fp - float2(0, 1));
float d10 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(1, 0)), fp - float2(1, 0));
float d11 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(1, 1)), fp - float2(1, 1));
fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
Out = lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5;
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Normalize_float3(float3 In, out float3 Out)
{
    Out = normalize(In);
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
{
    Out = lerp(A, B, T);
}

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
}

void Unity_Subtract_float3(float3 A, float3 B, out float3 Out)
{
    Out = A - B;
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Distance_float3(float3 A, float3 B, out float Out)
{
    Out = distance(A, B);
}

void Unity_Clamp_float(float In, float Min, float Max, out float Out)
{
    Out = clamp(In, Min, Max);
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
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
float _Split_f9cfb9a555e949379da9876f8d3844b2_R_1_Float = IN.WorldSpacePosition[0];
float _Split_f9cfb9a555e949379da9876f8d3844b2_G_2_Float = IN.WorldSpacePosition[1];
float _Split_f9cfb9a555e949379da9876f8d3844b2_B_3_Float = IN.WorldSpacePosition[2];
float _Split_f9cfb9a555e949379da9876f8d3844b2_A_4_Float = 0;
float2 _Vector2_feeecec8507c4619844c3ca15ea244be_Out_0_Vector2 = float2(_Split_f9cfb9a555e949379da9876f8d3844b2_R_1_Float, _Split_f9cfb9a555e949379da9876f8d3844b2_B_3_Float);
float2 _Property_186d136719274f03b9464e4e1d37a4e8_Out_0_Vector2 = _Wind_Direction;
float2 _Normalize_fb934df0b0c946039db4091949a06211_Out_1_Vector2;
Unity_Normalize_float2(_Property_186d136719274f03b9464e4e1d37a4e8_Out_0_Vector2, _Normalize_fb934df0b0c946039db4091949a06211_Out_1_Vector2);
float _Property_a98f7a88c8ac40109941c7f4f4211495_Out_0_Float = _Wind_Speed;
float _Multiply_880934a6c2a44f658fe3c3b3ed7cccac_Out_2_Float;
Unity_Multiply_float_float(10, _Property_a98f7a88c8ac40109941c7f4f4211495_Out_0_Float, _Multiply_880934a6c2a44f658fe3c3b3ed7cccac_Out_2_Float);
float2 _Multiply_892e7af0e8e54f869ff77c70575af5fc_Out_2_Vector2;
Unity_Multiply_float2_float2(_Normalize_fb934df0b0c946039db4091949a06211_Out_1_Vector2, (_Multiply_880934a6c2a44f658fe3c3b3ed7cccac_Out_2_Float.xx), _Multiply_892e7af0e8e54f869ff77c70575af5fc_Out_2_Vector2);
float2 _Multiply_d96cf1c14ac74322b4498e1849526399_Out_2_Vector2;
Unity_Multiply_float2_float2(_Multiply_892e7af0e8e54f869ff77c70575af5fc_Out_2_Vector2, (IN.TimeParameters.x.xx), _Multiply_d96cf1c14ac74322b4498e1849526399_Out_2_Vector2);
float2 _Add_c52cdcdd15b74d9a963511973a04621c_Out_2_Vector2;
Unity_Add_float2(_Vector2_feeecec8507c4619844c3ca15ea244be_Out_0_Vector2, _Multiply_d96cf1c14ac74322b4498e1849526399_Out_2_Vector2, _Add_c52cdcdd15b74d9a963511973a04621c_Out_2_Vector2);
float _Property_14515a2f6d834bd99002a2ccf491767f_Out_0_Float = _Wave_Scale;
float _Multiply_bbdcafe2ee654e3d8e4df25d9c66103c_Out_2_Float;
Unity_Multiply_float_float(3, _Property_14515a2f6d834bd99002a2ccf491767f_Out_0_Float, _Multiply_bbdcafe2ee654e3d8e4df25d9c66103c_Out_2_Float);
float _GradientNoise_32a9a6c418e44409a86a8238a6b4645c_Out_2_Float;
Unity_GradientNoise_Deterministic_float(_Add_c52cdcdd15b74d9a963511973a04621c_Out_2_Vector2, _Multiply_bbdcafe2ee654e3d8e4df25d9c66103c_Out_2_Float, _GradientNoise_32a9a6c418e44409a86a8238a6b4645c_Out_2_Float);
float _Subtract_3ec4867605194525898104109bc1e4a8_Out_2_Float;
Unity_Subtract_float(_GradientNoise_32a9a6c418e44409a86a8238a6b4645c_Out_2_Float, float(0.5), _Subtract_3ec4867605194525898104109bc1e4a8_Out_2_Float);
float _Property_a52d213096ea41398ec0a409bf7ca764_Out_0_Float = _Wind_Strength;
float _Multiply_444d89a5d2344a9d97e110a245567e62_Out_2_Float;
Unity_Multiply_float_float(2, _Property_a52d213096ea41398ec0a409bf7ca764_Out_0_Float, _Multiply_444d89a5d2344a9d97e110a245567e62_Out_2_Float);
float _Multiply_6a79300791ae4467bc6c5489c0164afa_Out_2_Float;
Unity_Multiply_float_float(_Subtract_3ec4867605194525898104109bc1e4a8_Out_2_Float, _Multiply_444d89a5d2344a9d97e110a245567e62_Out_2_Float, _Multiply_6a79300791ae4467bc6c5489c0164afa_Out_2_Float);
float3 _Normalize_ac3890981ce548259c746c56bd0ca30f_Out_1_Vector3;
Unity_Normalize_float3(IN.ObjectSpacePosition, _Normalize_ac3890981ce548259c746c56bd0ca30f_Out_1_Vector3);
float _Split_ce6b6b6587294130b97b66c5ec3b64a6_R_1_Float = _Normalize_ac3890981ce548259c746c56bd0ca30f_Out_1_Vector3[0];
float _Split_ce6b6b6587294130b97b66c5ec3b64a6_G_2_Float = _Normalize_ac3890981ce548259c746c56bd0ca30f_Out_1_Vector3[1];
float _Split_ce6b6b6587294130b97b66c5ec3b64a6_B_3_Float = _Normalize_ac3890981ce548259c746c56bd0ca30f_Out_1_Vector3[2];
float _Split_ce6b6b6587294130b97b66c5ec3b64a6_A_4_Float = 0;
float _Multiply_453e85026c7c4cc8b7c4661ac5495b5f_Out_2_Float;
Unity_Multiply_float_float(_Multiply_6a79300791ae4467bc6c5489c0164afa_Out_2_Float, _Split_ce6b6b6587294130b97b66c5ec3b64a6_G_2_Float, _Multiply_453e85026c7c4cc8b7c4661ac5495b5f_Out_2_Float);
float3 _Add_526cc646ad64464ba782c997c6d8a2e7_Out_2_Vector3;
Unity_Add_float3((_Multiply_453e85026c7c4cc8b7c4661ac5495b5f_Out_2_Float.xxx), IN.AbsoluteWorldSpacePosition, _Add_526cc646ad64464ba782c997c6d8a2e7_Out_2_Vector3);
float3 _Transform_26034a9482244d0db0cde719848b8793_Out_1_Vector3;
_Transform_26034a9482244d0db0cde719848b8793_Out_1_Vector3 = TransformWorldToObject(_Add_526cc646ad64464ba782c997c6d8a2e7_Out_2_Vector3.xyz);
float _Split_35ccc240162a41a796dd70233f88757c_R_1_Float = IN.VertexColor[0];
float _Split_35ccc240162a41a796dd70233f88757c_G_2_Float = IN.VertexColor[1];
float _Split_35ccc240162a41a796dd70233f88757c_B_3_Float = IN.VertexColor[2];
float _Split_35ccc240162a41a796dd70233f88757c_A_4_Float = IN.VertexColor[3];
float4 _Combine_bef8410c8e24482da5d3364b77236292_RGBA_4_Vector4;
float3 _Combine_bef8410c8e24482da5d3364b77236292_RGB_5_Vector3;
float2 _Combine_bef8410c8e24482da5d3364b77236292_RG_6_Vector2;
Unity_Combine_float(_Split_35ccc240162a41a796dd70233f88757c_R_1_Float, _Split_35ccc240162a41a796dd70233f88757c_G_2_Float, float(0), float(0), _Combine_bef8410c8e24482da5d3364b77236292_RGBA_4_Vector4, _Combine_bef8410c8e24482da5d3364b77236292_RGB_5_Vector3, _Combine_bef8410c8e24482da5d3364b77236292_RG_6_Vector2);
float3 _Lerp_7a7657270ab14fb2acad87e06eb37455_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _Transform_26034a9482244d0db0cde719848b8793_Out_1_Vector3, _Combine_bef8410c8e24482da5d3364b77236292_RGB_5_Vector3, _Lerp_7a7657270ab14fb2acad87e06eb37455_Out_3_Vector3);
float _Property_33225d4ce071408bbef8e7871ca2d298_Out_0_Float = _Wind_Speed;
float _Multiply_f56338cdfe18474dbabd15aebc9c5c2d_Out_2_Float;
Unity_Multiply_float_float(10, _Property_33225d4ce071408bbef8e7871ca2d298_Out_0_Float, _Multiply_f56338cdfe18474dbabd15aebc9c5c2d_Out_2_Float);
float _Multiply_7c3d48cd1d9e4ba6aaee9091a8d8a492_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Multiply_f56338cdfe18474dbabd15aebc9c5c2d_Out_2_Float, _Multiply_7c3d48cd1d9e4ba6aaee9091a8d8a492_Out_2_Float);
float2 _TilingAndOffset_ec707a81514142459d08db9978d86cf5_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, float2 (1, 1), (_Multiply_7c3d48cd1d9e4ba6aaee9091a8d8a492_Out_2_Float.xx), _TilingAndOffset_ec707a81514142459d08db9978d86cf5_Out_3_Vector2);
float _Property_52c7fe0910e04984b1e473c0627107b2_Out_0_Float = _Wind_Variation;
float _Multiply_a9622f7a126646aaa969ee30098cf924_Out_2_Float;
Unity_Multiply_float_float(0.5, _Property_52c7fe0910e04984b1e473c0627107b2_Out_0_Float, _Multiply_a9622f7a126646aaa969ee30098cf924_Out_2_Float);
float _GradientNoise_d9ac409c304041c49e680e55eb4d2c81_Out_2_Float;
Unity_GradientNoise_Deterministic_float(_TilingAndOffset_ec707a81514142459d08db9978d86cf5_Out_3_Vector2, _Multiply_a9622f7a126646aaa969ee30098cf924_Out_2_Float, _GradientNoise_d9ac409c304041c49e680e55eb4d2c81_Out_2_Float);
float3 _Lerp_1b4b768f79f34ddaa538834a53b8e33b_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _Lerp_7a7657270ab14fb2acad87e06eb37455_Out_3_Vector3, (_GradientNoise_d9ac409c304041c49e680e55eb4d2c81_Out_2_Float.xxx), _Lerp_1b4b768f79f34ddaa538834a53b8e33b_Out_3_Vector3);
float3 _Property_413c2dab0db145e686fd951a3cf9b06d_Out_0_Vector3 = _Player_Position;
float3 _Subtract_16caad2935f64d4ca2a5c41c122c2899_Out_2_Vector3;
Unity_Subtract_float3(IN.AbsoluteWorldSpacePosition, _Property_413c2dab0db145e686fd951a3cf9b06d_Out_0_Vector3, _Subtract_16caad2935f64d4ca2a5c41c122c2899_Out_2_Vector3);
float _Split_dd517b5d363349dfbcc6a80463d0a69c_R_1_Float = _Subtract_16caad2935f64d4ca2a5c41c122c2899_Out_2_Vector3[0];
float _Split_dd517b5d363349dfbcc6a80463d0a69c_G_2_Float = _Subtract_16caad2935f64d4ca2a5c41c122c2899_Out_2_Vector3[1];
float _Split_dd517b5d363349dfbcc6a80463d0a69c_B_3_Float = _Subtract_16caad2935f64d4ca2a5c41c122c2899_Out_2_Vector3[2];
float _Split_dd517b5d363349dfbcc6a80463d0a69c_A_4_Float = 0;
float _Property_f88a2d673c09471cad200fa3c1b6ae30_Out_0_Float = _Bend_Strength;
float _Multiply_6c5ecb62197a4848b2fabec6f920ae09_Out_2_Float;
Unity_Multiply_float_float(7, _Property_f88a2d673c09471cad200fa3c1b6ae30_Out_0_Float, _Multiply_6c5ecb62197a4848b2fabec6f920ae09_Out_2_Float);
float _Subtract_fabc7232020f481398d5e1c34e958838_Out_2_Float;
Unity_Subtract_float(_Split_dd517b5d363349dfbcc6a80463d0a69c_G_2_Float, _Multiply_6c5ecb62197a4848b2fabec6f920ae09_Out_2_Float, _Subtract_fabc7232020f481398d5e1c34e958838_Out_2_Float);
float4 _Combine_e1e6724c2316432f9d0cefb980576bd6_RGBA_4_Vector4;
float3 _Combine_e1e6724c2316432f9d0cefb980576bd6_RGB_5_Vector3;
float2 _Combine_e1e6724c2316432f9d0cefb980576bd6_RG_6_Vector2;
Unity_Combine_float(_Split_dd517b5d363349dfbcc6a80463d0a69c_R_1_Float, _Subtract_fabc7232020f481398d5e1c34e958838_Out_2_Float, _Split_dd517b5d363349dfbcc6a80463d0a69c_B_3_Float, float(0), _Combine_e1e6724c2316432f9d0cefb980576bd6_RGBA_4_Vector4, _Combine_e1e6724c2316432f9d0cefb980576bd6_RGB_5_Vector3, _Combine_e1e6724c2316432f9d0cefb980576bd6_RG_6_Vector2);
float3 _Multiply_42e953d7da1c4e7fbe777341488bae83_Out_2_Vector3;
Unity_Multiply_float3_float3(_Combine_e1e6724c2316432f9d0cefb980576bd6_RGB_5_Vector3, (_Property_f88a2d673c09471cad200fa3c1b6ae30_Out_0_Float.xxx), _Multiply_42e953d7da1c4e7fbe777341488bae83_Out_2_Vector3);
float _Property_ad55f894067547979f44b3a570bf7d49_Out_0_Float = _Start_Bend_Range;
float _Multiply_a7265b1673054e0996cb483d2ac3010d_Out_2_Float;
Unity_Multiply_float_float(_Property_ad55f894067547979f44b3a570bf7d49_Out_0_Float, 1.5, _Multiply_a7265b1673054e0996cb483d2ac3010d_Out_2_Float);
float _Distance_41bd00a9f1be4373a1003dc1e109849c_Out_2_Float;
Unity_Distance_float3(_Property_413c2dab0db145e686fd951a3cf9b06d_Out_0_Vector3, IN.AbsoluteWorldSpacePosition, _Distance_41bd00a9f1be4373a1003dc1e109849c_Out_2_Float);
float _Clamp_61013f1b8a7946cabf0f461c9754f564_Out_3_Float;
Unity_Clamp_float(_Distance_41bd00a9f1be4373a1003dc1e109849c_Out_2_Float, float(0), _Multiply_a7265b1673054e0996cb483d2ac3010d_Out_2_Float, _Clamp_61013f1b8a7946cabf0f461c9754f564_Out_3_Float);
float _Subtract_6193cd2daf004574816768560ab361b4_Out_2_Float;
Unity_Subtract_float(_Multiply_a7265b1673054e0996cb483d2ac3010d_Out_2_Float, _Clamp_61013f1b8a7946cabf0f461c9754f564_Out_3_Float, _Subtract_6193cd2daf004574816768560ab361b4_Out_2_Float);
float3 _Multiply_49122dc502124ec0b62c5e8d9df0b640_Out_2_Vector3;
Unity_Multiply_float3_float3(_Multiply_42e953d7da1c4e7fbe777341488bae83_Out_2_Vector3, (_Subtract_6193cd2daf004574816768560ab361b4_Out_2_Float.xxx), _Multiply_49122dc502124ec0b62c5e8d9df0b640_Out_2_Vector3);
float3 _Add_edf7ac44af404fdbbe1d0c50b7bc8b0f_Out_2_Vector3;
Unity_Add_float3(IN.ObjectSpacePosition, _Multiply_49122dc502124ec0b62c5e8d9df0b640_Out_2_Vector3, _Add_edf7ac44af404fdbbe1d0c50b7bc8b0f_Out_2_Vector3);
float3 _Normalize_0901e7953f394045adc844d92565cfba_Out_1_Vector3;
Unity_Normalize_float3(IN.ObjectSpacePosition, _Normalize_0901e7953f394045adc844d92565cfba_Out_1_Vector3);
float _Split_c58f3da36534495d8f4633fe4c40a74c_R_1_Float = _Normalize_0901e7953f394045adc844d92565cfba_Out_1_Vector3[0];
float _Split_c58f3da36534495d8f4633fe4c40a74c_G_2_Float = _Normalize_0901e7953f394045adc844d92565cfba_Out_1_Vector3[1];
float _Split_c58f3da36534495d8f4633fe4c40a74c_B_3_Float = _Normalize_0901e7953f394045adc844d92565cfba_Out_1_Vector3[2];
float _Split_c58f3da36534495d8f4633fe4c40a74c_A_4_Float = 0;
float _OneMinus_5a83b2c5314e45b2a835abb20d70bd22_Out_1_Float;
Unity_OneMinus_float(_Split_c58f3da36534495d8f4633fe4c40a74c_G_2_Float, _OneMinus_5a83b2c5314e45b2a835abb20d70bd22_Out_1_Float);
float _Step_428aa74b40a6492ba1e5a63ecfe4b53e_Out_2_Float;
Unity_Step_float(_OneMinus_5a83b2c5314e45b2a835abb20d70bd22_Out_1_Float, float(1), _Step_428aa74b40a6492ba1e5a63ecfe4b53e_Out_2_Float);
float3 _Lerp_7404d23a49244d1a8ca1c221f25d981c_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _Add_edf7ac44af404fdbbe1d0c50b7bc8b0f_Out_2_Vector3, (_Step_428aa74b40a6492ba1e5a63ecfe4b53e_Out_2_Float.xxx), _Lerp_7404d23a49244d1a8ca1c221f25d981c_Out_3_Vector3);
float3 _Lerp_93b3b392cffa4e688d1b32f6298c4907_Out_3_Vector3;
Unity_Lerp_float3(_Lerp_1b4b768f79f34ddaa538834a53b8e33b_Out_3_Vector3, _Lerp_7404d23a49244d1a8ca1c221f25d981c_Out_3_Vector3, (_GradientNoise_d9ac409c304041c49e680e55eb4d2c81_Out_2_Float.xxx), _Lerp_93b3b392cffa4e688d1b32f6298c4907_Out_3_Vector3);
float3 _UPGFoliageContactPositionCustomFunction_4d51d42c561a43cc976f6db5b768a1c6_bentPositionOS_1_Vector3;
UPG_FoliageContactPosition_float(_Lerp_93b3b392cffa4e688d1b32f6298c4907_Out_3_Vector3, _UPGFoliageContactPositionCustomFunction_4d51d42c561a43cc976f6db5b768a1c6_bentPositionOS_1_Vector3);
description.Position = _UPGFoliageContactPositionCustomFunction_4d51d42c561a43cc976f6db5b768a1c6_bentPositionOS_1_Vector3;
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
UnityTexture2D _Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Texture, sampler_Texture, _Texture_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D.tex, _Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D.samplerstate, _Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4, _Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_R_4_Float = _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4.r;
float _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_G_5_Float = _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4.g;
float _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_B_6_Float = _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4.b;
float _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_A_7_Float = _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4.a;
float _Property_ae6b5992b5874ce1b905e7962780a6be_Out_0_Float = _Alpha_Cutoff;
surface.Alpha = _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_A_7_Float;
surface.AlphaClipThreshold = _Property_ae6b5992b5874ce1b905e7962780a6be_Out_0_Float;
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
    output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
    output.AbsoluteWorldSpacePosition =                 GetAbsolutePositionWS(TransformObjectToWorld(input.positionOS));
    output.uv0 =                                        input.uv0;
    output.VertexColor =                                input.color;
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
#pragma shader_feature _ _SOFT_SHADOWS
#pragma shader_feature_local _COLOR_OVERLAY_VERTEX_POSITION _COLOR_OVERLAY_UV

#if defined(_SOFT_SHADOWS) && defined(_COLOR_OVERLAY_VERTEX_POSITION)
#define KEYWORD_PERMUTATION_0
#elif defined(_SOFT_SHADOWS) && defined(_COLOR_OVERLAY_UV)
#define KEYWORD_PERMUTATION_1
#elif defined(_COLOR_OVERLAY_VERTEX_POSITION)
#define KEYWORD_PERMUTATION_2
#else
#define KEYWORD_PERMUTATION_3
#endif


// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_COLOR
#define GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
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
 float4 color : COLOR;
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
 float3 WorldSpacePosition;
 float3 AbsoluteWorldSpacePosition;
 float4 uv0;
 float4 VertexColor;
 float3 TimeParameters;
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

float4 _Texture_TexelSize;
float4 _Top_Color;
float _Custom_Color;
float4 _Bottom_Color;
float _Blend_Height;
float _Wind_Strength;
float _Wind_Speed;
float _Wind_Variation;
float2 _Wind_Direction;
float _Alpha_Cutoff;
float _Enable_Top_Color;
float _Start_Bend_Range;
float _Wave_Scale;
float _Bend_Strength;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Texture);
SAMPLER(sampler_Texture);
float3 _Player_Position;

// Graph Includes
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"
#include_with_pragmas "Assets/06.Shaders/Foliage/UPG_FoliageInteraction.hlsl"

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

void Unity_Normalize_float2(float2 In, out float2 Out)
{
    Out = normalize(In);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_Add_float2(float2 A, float2 B, out float2 Out)
{
    Out = A + B;
}

float2 Unity_GradientNoise_Deterministic_Dir_float(float2 p)
{
float x; Hash_Tchou_2_1_float(p, x);
return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
}

void Unity_GradientNoise_Deterministic_float (float2 UV, float3 Scale, out float Out)
{
float2 p = UV * Scale.xy;
float2 ip = floor(p);
float2 fp = frac(p);
float d00 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip), fp);
float d01 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(0, 1)), fp - float2(0, 1));
float d10 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(1, 0)), fp - float2(1, 0));
float d11 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(1, 1)), fp - float2(1, 1));
fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
Out = lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5;
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Normalize_float3(float3 In, out float3 Out)
{
    Out = normalize(In);
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
{
    Out = lerp(A, B, T);
}

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
}

void Unity_Subtract_float3(float3 A, float3 B, out float3 Out)
{
    Out = A - B;
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Distance_float3(float3 A, float3 B, out float Out)
{
    Out = distance(A, B);
}

void Unity_Clamp_float(float In, float Min, float Max, out float Out)
{
    Out = clamp(In, Min, Max);
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
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
float _Split_f9cfb9a555e949379da9876f8d3844b2_R_1_Float = IN.WorldSpacePosition[0];
float _Split_f9cfb9a555e949379da9876f8d3844b2_G_2_Float = IN.WorldSpacePosition[1];
float _Split_f9cfb9a555e949379da9876f8d3844b2_B_3_Float = IN.WorldSpacePosition[2];
float _Split_f9cfb9a555e949379da9876f8d3844b2_A_4_Float = 0;
float2 _Vector2_feeecec8507c4619844c3ca15ea244be_Out_0_Vector2 = float2(_Split_f9cfb9a555e949379da9876f8d3844b2_R_1_Float, _Split_f9cfb9a555e949379da9876f8d3844b2_B_3_Float);
float2 _Property_186d136719274f03b9464e4e1d37a4e8_Out_0_Vector2 = _Wind_Direction;
float2 _Normalize_fb934df0b0c946039db4091949a06211_Out_1_Vector2;
Unity_Normalize_float2(_Property_186d136719274f03b9464e4e1d37a4e8_Out_0_Vector2, _Normalize_fb934df0b0c946039db4091949a06211_Out_1_Vector2);
float _Property_a98f7a88c8ac40109941c7f4f4211495_Out_0_Float = _Wind_Speed;
float _Multiply_880934a6c2a44f658fe3c3b3ed7cccac_Out_2_Float;
Unity_Multiply_float_float(10, _Property_a98f7a88c8ac40109941c7f4f4211495_Out_0_Float, _Multiply_880934a6c2a44f658fe3c3b3ed7cccac_Out_2_Float);
float2 _Multiply_892e7af0e8e54f869ff77c70575af5fc_Out_2_Vector2;
Unity_Multiply_float2_float2(_Normalize_fb934df0b0c946039db4091949a06211_Out_1_Vector2, (_Multiply_880934a6c2a44f658fe3c3b3ed7cccac_Out_2_Float.xx), _Multiply_892e7af0e8e54f869ff77c70575af5fc_Out_2_Vector2);
float2 _Multiply_d96cf1c14ac74322b4498e1849526399_Out_2_Vector2;
Unity_Multiply_float2_float2(_Multiply_892e7af0e8e54f869ff77c70575af5fc_Out_2_Vector2, (IN.TimeParameters.x.xx), _Multiply_d96cf1c14ac74322b4498e1849526399_Out_2_Vector2);
float2 _Add_c52cdcdd15b74d9a963511973a04621c_Out_2_Vector2;
Unity_Add_float2(_Vector2_feeecec8507c4619844c3ca15ea244be_Out_0_Vector2, _Multiply_d96cf1c14ac74322b4498e1849526399_Out_2_Vector2, _Add_c52cdcdd15b74d9a963511973a04621c_Out_2_Vector2);
float _Property_14515a2f6d834bd99002a2ccf491767f_Out_0_Float = _Wave_Scale;
float _Multiply_bbdcafe2ee654e3d8e4df25d9c66103c_Out_2_Float;
Unity_Multiply_float_float(3, _Property_14515a2f6d834bd99002a2ccf491767f_Out_0_Float, _Multiply_bbdcafe2ee654e3d8e4df25d9c66103c_Out_2_Float);
float _GradientNoise_32a9a6c418e44409a86a8238a6b4645c_Out_2_Float;
Unity_GradientNoise_Deterministic_float(_Add_c52cdcdd15b74d9a963511973a04621c_Out_2_Vector2, _Multiply_bbdcafe2ee654e3d8e4df25d9c66103c_Out_2_Float, _GradientNoise_32a9a6c418e44409a86a8238a6b4645c_Out_2_Float);
float _Subtract_3ec4867605194525898104109bc1e4a8_Out_2_Float;
Unity_Subtract_float(_GradientNoise_32a9a6c418e44409a86a8238a6b4645c_Out_2_Float, float(0.5), _Subtract_3ec4867605194525898104109bc1e4a8_Out_2_Float);
float _Property_a52d213096ea41398ec0a409bf7ca764_Out_0_Float = _Wind_Strength;
float _Multiply_444d89a5d2344a9d97e110a245567e62_Out_2_Float;
Unity_Multiply_float_float(2, _Property_a52d213096ea41398ec0a409bf7ca764_Out_0_Float, _Multiply_444d89a5d2344a9d97e110a245567e62_Out_2_Float);
float _Multiply_6a79300791ae4467bc6c5489c0164afa_Out_2_Float;
Unity_Multiply_float_float(_Subtract_3ec4867605194525898104109bc1e4a8_Out_2_Float, _Multiply_444d89a5d2344a9d97e110a245567e62_Out_2_Float, _Multiply_6a79300791ae4467bc6c5489c0164afa_Out_2_Float);
float3 _Normalize_ac3890981ce548259c746c56bd0ca30f_Out_1_Vector3;
Unity_Normalize_float3(IN.ObjectSpacePosition, _Normalize_ac3890981ce548259c746c56bd0ca30f_Out_1_Vector3);
float _Split_ce6b6b6587294130b97b66c5ec3b64a6_R_1_Float = _Normalize_ac3890981ce548259c746c56bd0ca30f_Out_1_Vector3[0];
float _Split_ce6b6b6587294130b97b66c5ec3b64a6_G_2_Float = _Normalize_ac3890981ce548259c746c56bd0ca30f_Out_1_Vector3[1];
float _Split_ce6b6b6587294130b97b66c5ec3b64a6_B_3_Float = _Normalize_ac3890981ce548259c746c56bd0ca30f_Out_1_Vector3[2];
float _Split_ce6b6b6587294130b97b66c5ec3b64a6_A_4_Float = 0;
float _Multiply_453e85026c7c4cc8b7c4661ac5495b5f_Out_2_Float;
Unity_Multiply_float_float(_Multiply_6a79300791ae4467bc6c5489c0164afa_Out_2_Float, _Split_ce6b6b6587294130b97b66c5ec3b64a6_G_2_Float, _Multiply_453e85026c7c4cc8b7c4661ac5495b5f_Out_2_Float);
float3 _Add_526cc646ad64464ba782c997c6d8a2e7_Out_2_Vector3;
Unity_Add_float3((_Multiply_453e85026c7c4cc8b7c4661ac5495b5f_Out_2_Float.xxx), IN.AbsoluteWorldSpacePosition, _Add_526cc646ad64464ba782c997c6d8a2e7_Out_2_Vector3);
float3 _Transform_26034a9482244d0db0cde719848b8793_Out_1_Vector3;
_Transform_26034a9482244d0db0cde719848b8793_Out_1_Vector3 = TransformWorldToObject(_Add_526cc646ad64464ba782c997c6d8a2e7_Out_2_Vector3.xyz);
float _Split_35ccc240162a41a796dd70233f88757c_R_1_Float = IN.VertexColor[0];
float _Split_35ccc240162a41a796dd70233f88757c_G_2_Float = IN.VertexColor[1];
float _Split_35ccc240162a41a796dd70233f88757c_B_3_Float = IN.VertexColor[2];
float _Split_35ccc240162a41a796dd70233f88757c_A_4_Float = IN.VertexColor[3];
float4 _Combine_bef8410c8e24482da5d3364b77236292_RGBA_4_Vector4;
float3 _Combine_bef8410c8e24482da5d3364b77236292_RGB_5_Vector3;
float2 _Combine_bef8410c8e24482da5d3364b77236292_RG_6_Vector2;
Unity_Combine_float(_Split_35ccc240162a41a796dd70233f88757c_R_1_Float, _Split_35ccc240162a41a796dd70233f88757c_G_2_Float, float(0), float(0), _Combine_bef8410c8e24482da5d3364b77236292_RGBA_4_Vector4, _Combine_bef8410c8e24482da5d3364b77236292_RGB_5_Vector3, _Combine_bef8410c8e24482da5d3364b77236292_RG_6_Vector2);
float3 _Lerp_7a7657270ab14fb2acad87e06eb37455_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _Transform_26034a9482244d0db0cde719848b8793_Out_1_Vector3, _Combine_bef8410c8e24482da5d3364b77236292_RGB_5_Vector3, _Lerp_7a7657270ab14fb2acad87e06eb37455_Out_3_Vector3);
float _Property_33225d4ce071408bbef8e7871ca2d298_Out_0_Float = _Wind_Speed;
float _Multiply_f56338cdfe18474dbabd15aebc9c5c2d_Out_2_Float;
Unity_Multiply_float_float(10, _Property_33225d4ce071408bbef8e7871ca2d298_Out_0_Float, _Multiply_f56338cdfe18474dbabd15aebc9c5c2d_Out_2_Float);
float _Multiply_7c3d48cd1d9e4ba6aaee9091a8d8a492_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Multiply_f56338cdfe18474dbabd15aebc9c5c2d_Out_2_Float, _Multiply_7c3d48cd1d9e4ba6aaee9091a8d8a492_Out_2_Float);
float2 _TilingAndOffset_ec707a81514142459d08db9978d86cf5_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, float2 (1, 1), (_Multiply_7c3d48cd1d9e4ba6aaee9091a8d8a492_Out_2_Float.xx), _TilingAndOffset_ec707a81514142459d08db9978d86cf5_Out_3_Vector2);
float _Property_52c7fe0910e04984b1e473c0627107b2_Out_0_Float = _Wind_Variation;
float _Multiply_a9622f7a126646aaa969ee30098cf924_Out_2_Float;
Unity_Multiply_float_float(0.5, _Property_52c7fe0910e04984b1e473c0627107b2_Out_0_Float, _Multiply_a9622f7a126646aaa969ee30098cf924_Out_2_Float);
float _GradientNoise_d9ac409c304041c49e680e55eb4d2c81_Out_2_Float;
Unity_GradientNoise_Deterministic_float(_TilingAndOffset_ec707a81514142459d08db9978d86cf5_Out_3_Vector2, _Multiply_a9622f7a126646aaa969ee30098cf924_Out_2_Float, _GradientNoise_d9ac409c304041c49e680e55eb4d2c81_Out_2_Float);
float3 _Lerp_1b4b768f79f34ddaa538834a53b8e33b_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _Lerp_7a7657270ab14fb2acad87e06eb37455_Out_3_Vector3, (_GradientNoise_d9ac409c304041c49e680e55eb4d2c81_Out_2_Float.xxx), _Lerp_1b4b768f79f34ddaa538834a53b8e33b_Out_3_Vector3);
float3 _Property_413c2dab0db145e686fd951a3cf9b06d_Out_0_Vector3 = _Player_Position;
float3 _Subtract_16caad2935f64d4ca2a5c41c122c2899_Out_2_Vector3;
Unity_Subtract_float3(IN.AbsoluteWorldSpacePosition, _Property_413c2dab0db145e686fd951a3cf9b06d_Out_0_Vector3, _Subtract_16caad2935f64d4ca2a5c41c122c2899_Out_2_Vector3);
float _Split_dd517b5d363349dfbcc6a80463d0a69c_R_1_Float = _Subtract_16caad2935f64d4ca2a5c41c122c2899_Out_2_Vector3[0];
float _Split_dd517b5d363349dfbcc6a80463d0a69c_G_2_Float = _Subtract_16caad2935f64d4ca2a5c41c122c2899_Out_2_Vector3[1];
float _Split_dd517b5d363349dfbcc6a80463d0a69c_B_3_Float = _Subtract_16caad2935f64d4ca2a5c41c122c2899_Out_2_Vector3[2];
float _Split_dd517b5d363349dfbcc6a80463d0a69c_A_4_Float = 0;
float _Property_f88a2d673c09471cad200fa3c1b6ae30_Out_0_Float = _Bend_Strength;
float _Multiply_6c5ecb62197a4848b2fabec6f920ae09_Out_2_Float;
Unity_Multiply_float_float(7, _Property_f88a2d673c09471cad200fa3c1b6ae30_Out_0_Float, _Multiply_6c5ecb62197a4848b2fabec6f920ae09_Out_2_Float);
float _Subtract_fabc7232020f481398d5e1c34e958838_Out_2_Float;
Unity_Subtract_float(_Split_dd517b5d363349dfbcc6a80463d0a69c_G_2_Float, _Multiply_6c5ecb62197a4848b2fabec6f920ae09_Out_2_Float, _Subtract_fabc7232020f481398d5e1c34e958838_Out_2_Float);
float4 _Combine_e1e6724c2316432f9d0cefb980576bd6_RGBA_4_Vector4;
float3 _Combine_e1e6724c2316432f9d0cefb980576bd6_RGB_5_Vector3;
float2 _Combine_e1e6724c2316432f9d0cefb980576bd6_RG_6_Vector2;
Unity_Combine_float(_Split_dd517b5d363349dfbcc6a80463d0a69c_R_1_Float, _Subtract_fabc7232020f481398d5e1c34e958838_Out_2_Float, _Split_dd517b5d363349dfbcc6a80463d0a69c_B_3_Float, float(0), _Combine_e1e6724c2316432f9d0cefb980576bd6_RGBA_4_Vector4, _Combine_e1e6724c2316432f9d0cefb980576bd6_RGB_5_Vector3, _Combine_e1e6724c2316432f9d0cefb980576bd6_RG_6_Vector2);
float3 _Multiply_42e953d7da1c4e7fbe777341488bae83_Out_2_Vector3;
Unity_Multiply_float3_float3(_Combine_e1e6724c2316432f9d0cefb980576bd6_RGB_5_Vector3, (_Property_f88a2d673c09471cad200fa3c1b6ae30_Out_0_Float.xxx), _Multiply_42e953d7da1c4e7fbe777341488bae83_Out_2_Vector3);
float _Property_ad55f894067547979f44b3a570bf7d49_Out_0_Float = _Start_Bend_Range;
float _Multiply_a7265b1673054e0996cb483d2ac3010d_Out_2_Float;
Unity_Multiply_float_float(_Property_ad55f894067547979f44b3a570bf7d49_Out_0_Float, 1.5, _Multiply_a7265b1673054e0996cb483d2ac3010d_Out_2_Float);
float _Distance_41bd00a9f1be4373a1003dc1e109849c_Out_2_Float;
Unity_Distance_float3(_Property_413c2dab0db145e686fd951a3cf9b06d_Out_0_Vector3, IN.AbsoluteWorldSpacePosition, _Distance_41bd00a9f1be4373a1003dc1e109849c_Out_2_Float);
float _Clamp_61013f1b8a7946cabf0f461c9754f564_Out_3_Float;
Unity_Clamp_float(_Distance_41bd00a9f1be4373a1003dc1e109849c_Out_2_Float, float(0), _Multiply_a7265b1673054e0996cb483d2ac3010d_Out_2_Float, _Clamp_61013f1b8a7946cabf0f461c9754f564_Out_3_Float);
float _Subtract_6193cd2daf004574816768560ab361b4_Out_2_Float;
Unity_Subtract_float(_Multiply_a7265b1673054e0996cb483d2ac3010d_Out_2_Float, _Clamp_61013f1b8a7946cabf0f461c9754f564_Out_3_Float, _Subtract_6193cd2daf004574816768560ab361b4_Out_2_Float);
float3 _Multiply_49122dc502124ec0b62c5e8d9df0b640_Out_2_Vector3;
Unity_Multiply_float3_float3(_Multiply_42e953d7da1c4e7fbe777341488bae83_Out_2_Vector3, (_Subtract_6193cd2daf004574816768560ab361b4_Out_2_Float.xxx), _Multiply_49122dc502124ec0b62c5e8d9df0b640_Out_2_Vector3);
float3 _Add_edf7ac44af404fdbbe1d0c50b7bc8b0f_Out_2_Vector3;
Unity_Add_float3(IN.ObjectSpacePosition, _Multiply_49122dc502124ec0b62c5e8d9df0b640_Out_2_Vector3, _Add_edf7ac44af404fdbbe1d0c50b7bc8b0f_Out_2_Vector3);
float3 _Normalize_0901e7953f394045adc844d92565cfba_Out_1_Vector3;
Unity_Normalize_float3(IN.ObjectSpacePosition, _Normalize_0901e7953f394045adc844d92565cfba_Out_1_Vector3);
float _Split_c58f3da36534495d8f4633fe4c40a74c_R_1_Float = _Normalize_0901e7953f394045adc844d92565cfba_Out_1_Vector3[0];
float _Split_c58f3da36534495d8f4633fe4c40a74c_G_2_Float = _Normalize_0901e7953f394045adc844d92565cfba_Out_1_Vector3[1];
float _Split_c58f3da36534495d8f4633fe4c40a74c_B_3_Float = _Normalize_0901e7953f394045adc844d92565cfba_Out_1_Vector3[2];
float _Split_c58f3da36534495d8f4633fe4c40a74c_A_4_Float = 0;
float _OneMinus_5a83b2c5314e45b2a835abb20d70bd22_Out_1_Float;
Unity_OneMinus_float(_Split_c58f3da36534495d8f4633fe4c40a74c_G_2_Float, _OneMinus_5a83b2c5314e45b2a835abb20d70bd22_Out_1_Float);
float _Step_428aa74b40a6492ba1e5a63ecfe4b53e_Out_2_Float;
Unity_Step_float(_OneMinus_5a83b2c5314e45b2a835abb20d70bd22_Out_1_Float, float(1), _Step_428aa74b40a6492ba1e5a63ecfe4b53e_Out_2_Float);
float3 _Lerp_7404d23a49244d1a8ca1c221f25d981c_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _Add_edf7ac44af404fdbbe1d0c50b7bc8b0f_Out_2_Vector3, (_Step_428aa74b40a6492ba1e5a63ecfe4b53e_Out_2_Float.xxx), _Lerp_7404d23a49244d1a8ca1c221f25d981c_Out_3_Vector3);
float3 _Lerp_93b3b392cffa4e688d1b32f6298c4907_Out_3_Vector3;
Unity_Lerp_float3(_Lerp_1b4b768f79f34ddaa538834a53b8e33b_Out_3_Vector3, _Lerp_7404d23a49244d1a8ca1c221f25d981c_Out_3_Vector3, (_GradientNoise_d9ac409c304041c49e680e55eb4d2c81_Out_2_Float.xxx), _Lerp_93b3b392cffa4e688d1b32f6298c4907_Out_3_Vector3);
float3 _UPGFoliageContactPositionCustomFunction_4d51d42c561a43cc976f6db5b768a1c6_bentPositionOS_1_Vector3;
UPG_FoliageContactPosition_float(_Lerp_93b3b392cffa4e688d1b32f6298c4907_Out_3_Vector3, _UPGFoliageContactPositionCustomFunction_4d51d42c561a43cc976f6db5b768a1c6_bentPositionOS_1_Vector3);
description.Position = _UPGFoliageContactPositionCustomFunction_4d51d42c561a43cc976f6db5b768a1c6_bentPositionOS_1_Vector3;
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
UnityTexture2D _Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Texture, sampler_Texture, _Texture_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D.tex, _Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D.samplerstate, _Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4, _Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_R_4_Float = _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4.r;
float _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_G_5_Float = _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4.g;
float _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_B_6_Float = _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4.b;
float _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_A_7_Float = _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4.a;
float _Property_ae6b5992b5874ce1b905e7962780a6be_Out_0_Float = _Alpha_Cutoff;
surface.BaseColor = IsGammaSpace() ? float3(0, 0, 0) : SRGBToLinear(float3(0, 0, 0));
surface.Alpha = _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_A_7_Float;
surface.AlphaClipThreshold = _Property_ae6b5992b5874ce1b905e7962780a6be_Out_0_Float;
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
    output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
    output.AbsoluteWorldSpacePosition =                 GetAbsolutePositionWS(TransformObjectToWorld(input.positionOS));
    output.uv0 =                                        input.uv0;
    output.VertexColor =                                input.color;
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
#pragma shader_feature _ _SOFT_SHADOWS
#pragma shader_feature_local _COLOR_OVERLAY_VERTEX_POSITION _COLOR_OVERLAY_UV

#if defined(_SOFT_SHADOWS) && defined(_COLOR_OVERLAY_VERTEX_POSITION)
#define KEYWORD_PERMUTATION_0
#elif defined(_SOFT_SHADOWS) && defined(_COLOR_OVERLAY_UV)
#define KEYWORD_PERMUTATION_1
#elif defined(_COLOR_OVERLAY_VERTEX_POSITION)
#define KEYWORD_PERMUTATION_2
#else
#define KEYWORD_PERMUTATION_3
#endif


// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_COLOR
#define GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
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
 float4 color : COLOR;
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
 float3 WorldSpacePosition;
 float3 AbsoluteWorldSpacePosition;
 float4 uv0;
 float4 VertexColor;
 float3 TimeParameters;
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

float4 _Texture_TexelSize;
float4 _Top_Color;
float _Custom_Color;
float4 _Bottom_Color;
float _Blend_Height;
float _Wind_Strength;
float _Wind_Speed;
float _Wind_Variation;
float2 _Wind_Direction;
float _Alpha_Cutoff;
float _Enable_Top_Color;
float _Start_Bend_Range;
float _Wave_Scale;
float _Bend_Strength;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Texture);
SAMPLER(sampler_Texture);
float3 _Player_Position;

// Graph Includes
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"
#include_with_pragmas "Assets/06.Shaders/Foliage/UPG_FoliageInteraction.hlsl"

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

void Unity_Normalize_float2(float2 In, out float2 Out)
{
    Out = normalize(In);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_Add_float2(float2 A, float2 B, out float2 Out)
{
    Out = A + B;
}

float2 Unity_GradientNoise_Deterministic_Dir_float(float2 p)
{
float x; Hash_Tchou_2_1_float(p, x);
return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
}

void Unity_GradientNoise_Deterministic_float (float2 UV, float3 Scale, out float Out)
{
float2 p = UV * Scale.xy;
float2 ip = floor(p);
float2 fp = frac(p);
float d00 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip), fp);
float d01 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(0, 1)), fp - float2(0, 1));
float d10 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(1, 0)), fp - float2(1, 0));
float d11 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(1, 1)), fp - float2(1, 1));
fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
Out = lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5;
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Normalize_float3(float3 In, out float3 Out)
{
    Out = normalize(In);
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
{
    Out = lerp(A, B, T);
}

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
}

void Unity_Subtract_float3(float3 A, float3 B, out float3 Out)
{
    Out = A - B;
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Distance_float3(float3 A, float3 B, out float Out)
{
    Out = distance(A, B);
}

void Unity_Clamp_float(float In, float Min, float Max, out float Out)
{
    Out = clamp(In, Min, Max);
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
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
float _Split_f9cfb9a555e949379da9876f8d3844b2_R_1_Float = IN.WorldSpacePosition[0];
float _Split_f9cfb9a555e949379da9876f8d3844b2_G_2_Float = IN.WorldSpacePosition[1];
float _Split_f9cfb9a555e949379da9876f8d3844b2_B_3_Float = IN.WorldSpacePosition[2];
float _Split_f9cfb9a555e949379da9876f8d3844b2_A_4_Float = 0;
float2 _Vector2_feeecec8507c4619844c3ca15ea244be_Out_0_Vector2 = float2(_Split_f9cfb9a555e949379da9876f8d3844b2_R_1_Float, _Split_f9cfb9a555e949379da9876f8d3844b2_B_3_Float);
float2 _Property_186d136719274f03b9464e4e1d37a4e8_Out_0_Vector2 = _Wind_Direction;
float2 _Normalize_fb934df0b0c946039db4091949a06211_Out_1_Vector2;
Unity_Normalize_float2(_Property_186d136719274f03b9464e4e1d37a4e8_Out_0_Vector2, _Normalize_fb934df0b0c946039db4091949a06211_Out_1_Vector2);
float _Property_a98f7a88c8ac40109941c7f4f4211495_Out_0_Float = _Wind_Speed;
float _Multiply_880934a6c2a44f658fe3c3b3ed7cccac_Out_2_Float;
Unity_Multiply_float_float(10, _Property_a98f7a88c8ac40109941c7f4f4211495_Out_0_Float, _Multiply_880934a6c2a44f658fe3c3b3ed7cccac_Out_2_Float);
float2 _Multiply_892e7af0e8e54f869ff77c70575af5fc_Out_2_Vector2;
Unity_Multiply_float2_float2(_Normalize_fb934df0b0c946039db4091949a06211_Out_1_Vector2, (_Multiply_880934a6c2a44f658fe3c3b3ed7cccac_Out_2_Float.xx), _Multiply_892e7af0e8e54f869ff77c70575af5fc_Out_2_Vector2);
float2 _Multiply_d96cf1c14ac74322b4498e1849526399_Out_2_Vector2;
Unity_Multiply_float2_float2(_Multiply_892e7af0e8e54f869ff77c70575af5fc_Out_2_Vector2, (IN.TimeParameters.x.xx), _Multiply_d96cf1c14ac74322b4498e1849526399_Out_2_Vector2);
float2 _Add_c52cdcdd15b74d9a963511973a04621c_Out_2_Vector2;
Unity_Add_float2(_Vector2_feeecec8507c4619844c3ca15ea244be_Out_0_Vector2, _Multiply_d96cf1c14ac74322b4498e1849526399_Out_2_Vector2, _Add_c52cdcdd15b74d9a963511973a04621c_Out_2_Vector2);
float _Property_14515a2f6d834bd99002a2ccf491767f_Out_0_Float = _Wave_Scale;
float _Multiply_bbdcafe2ee654e3d8e4df25d9c66103c_Out_2_Float;
Unity_Multiply_float_float(3, _Property_14515a2f6d834bd99002a2ccf491767f_Out_0_Float, _Multiply_bbdcafe2ee654e3d8e4df25d9c66103c_Out_2_Float);
float _GradientNoise_32a9a6c418e44409a86a8238a6b4645c_Out_2_Float;
Unity_GradientNoise_Deterministic_float(_Add_c52cdcdd15b74d9a963511973a04621c_Out_2_Vector2, _Multiply_bbdcafe2ee654e3d8e4df25d9c66103c_Out_2_Float, _GradientNoise_32a9a6c418e44409a86a8238a6b4645c_Out_2_Float);
float _Subtract_3ec4867605194525898104109bc1e4a8_Out_2_Float;
Unity_Subtract_float(_GradientNoise_32a9a6c418e44409a86a8238a6b4645c_Out_2_Float, float(0.5), _Subtract_3ec4867605194525898104109bc1e4a8_Out_2_Float);
float _Property_a52d213096ea41398ec0a409bf7ca764_Out_0_Float = _Wind_Strength;
float _Multiply_444d89a5d2344a9d97e110a245567e62_Out_2_Float;
Unity_Multiply_float_float(2, _Property_a52d213096ea41398ec0a409bf7ca764_Out_0_Float, _Multiply_444d89a5d2344a9d97e110a245567e62_Out_2_Float);
float _Multiply_6a79300791ae4467bc6c5489c0164afa_Out_2_Float;
Unity_Multiply_float_float(_Subtract_3ec4867605194525898104109bc1e4a8_Out_2_Float, _Multiply_444d89a5d2344a9d97e110a245567e62_Out_2_Float, _Multiply_6a79300791ae4467bc6c5489c0164afa_Out_2_Float);
float3 _Normalize_ac3890981ce548259c746c56bd0ca30f_Out_1_Vector3;
Unity_Normalize_float3(IN.ObjectSpacePosition, _Normalize_ac3890981ce548259c746c56bd0ca30f_Out_1_Vector3);
float _Split_ce6b6b6587294130b97b66c5ec3b64a6_R_1_Float = _Normalize_ac3890981ce548259c746c56bd0ca30f_Out_1_Vector3[0];
float _Split_ce6b6b6587294130b97b66c5ec3b64a6_G_2_Float = _Normalize_ac3890981ce548259c746c56bd0ca30f_Out_1_Vector3[1];
float _Split_ce6b6b6587294130b97b66c5ec3b64a6_B_3_Float = _Normalize_ac3890981ce548259c746c56bd0ca30f_Out_1_Vector3[2];
float _Split_ce6b6b6587294130b97b66c5ec3b64a6_A_4_Float = 0;
float _Multiply_453e85026c7c4cc8b7c4661ac5495b5f_Out_2_Float;
Unity_Multiply_float_float(_Multiply_6a79300791ae4467bc6c5489c0164afa_Out_2_Float, _Split_ce6b6b6587294130b97b66c5ec3b64a6_G_2_Float, _Multiply_453e85026c7c4cc8b7c4661ac5495b5f_Out_2_Float);
float3 _Add_526cc646ad64464ba782c997c6d8a2e7_Out_2_Vector3;
Unity_Add_float3((_Multiply_453e85026c7c4cc8b7c4661ac5495b5f_Out_2_Float.xxx), IN.AbsoluteWorldSpacePosition, _Add_526cc646ad64464ba782c997c6d8a2e7_Out_2_Vector3);
float3 _Transform_26034a9482244d0db0cde719848b8793_Out_1_Vector3;
_Transform_26034a9482244d0db0cde719848b8793_Out_1_Vector3 = TransformWorldToObject(_Add_526cc646ad64464ba782c997c6d8a2e7_Out_2_Vector3.xyz);
float _Split_35ccc240162a41a796dd70233f88757c_R_1_Float = IN.VertexColor[0];
float _Split_35ccc240162a41a796dd70233f88757c_G_2_Float = IN.VertexColor[1];
float _Split_35ccc240162a41a796dd70233f88757c_B_3_Float = IN.VertexColor[2];
float _Split_35ccc240162a41a796dd70233f88757c_A_4_Float = IN.VertexColor[3];
float4 _Combine_bef8410c8e24482da5d3364b77236292_RGBA_4_Vector4;
float3 _Combine_bef8410c8e24482da5d3364b77236292_RGB_5_Vector3;
float2 _Combine_bef8410c8e24482da5d3364b77236292_RG_6_Vector2;
Unity_Combine_float(_Split_35ccc240162a41a796dd70233f88757c_R_1_Float, _Split_35ccc240162a41a796dd70233f88757c_G_2_Float, float(0), float(0), _Combine_bef8410c8e24482da5d3364b77236292_RGBA_4_Vector4, _Combine_bef8410c8e24482da5d3364b77236292_RGB_5_Vector3, _Combine_bef8410c8e24482da5d3364b77236292_RG_6_Vector2);
float3 _Lerp_7a7657270ab14fb2acad87e06eb37455_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _Transform_26034a9482244d0db0cde719848b8793_Out_1_Vector3, _Combine_bef8410c8e24482da5d3364b77236292_RGB_5_Vector3, _Lerp_7a7657270ab14fb2acad87e06eb37455_Out_3_Vector3);
float _Property_33225d4ce071408bbef8e7871ca2d298_Out_0_Float = _Wind_Speed;
float _Multiply_f56338cdfe18474dbabd15aebc9c5c2d_Out_2_Float;
Unity_Multiply_float_float(10, _Property_33225d4ce071408bbef8e7871ca2d298_Out_0_Float, _Multiply_f56338cdfe18474dbabd15aebc9c5c2d_Out_2_Float);
float _Multiply_7c3d48cd1d9e4ba6aaee9091a8d8a492_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Multiply_f56338cdfe18474dbabd15aebc9c5c2d_Out_2_Float, _Multiply_7c3d48cd1d9e4ba6aaee9091a8d8a492_Out_2_Float);
float2 _TilingAndOffset_ec707a81514142459d08db9978d86cf5_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, float2 (1, 1), (_Multiply_7c3d48cd1d9e4ba6aaee9091a8d8a492_Out_2_Float.xx), _TilingAndOffset_ec707a81514142459d08db9978d86cf5_Out_3_Vector2);
float _Property_52c7fe0910e04984b1e473c0627107b2_Out_0_Float = _Wind_Variation;
float _Multiply_a9622f7a126646aaa969ee30098cf924_Out_2_Float;
Unity_Multiply_float_float(0.5, _Property_52c7fe0910e04984b1e473c0627107b2_Out_0_Float, _Multiply_a9622f7a126646aaa969ee30098cf924_Out_2_Float);
float _GradientNoise_d9ac409c304041c49e680e55eb4d2c81_Out_2_Float;
Unity_GradientNoise_Deterministic_float(_TilingAndOffset_ec707a81514142459d08db9978d86cf5_Out_3_Vector2, _Multiply_a9622f7a126646aaa969ee30098cf924_Out_2_Float, _GradientNoise_d9ac409c304041c49e680e55eb4d2c81_Out_2_Float);
float3 _Lerp_1b4b768f79f34ddaa538834a53b8e33b_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _Lerp_7a7657270ab14fb2acad87e06eb37455_Out_3_Vector3, (_GradientNoise_d9ac409c304041c49e680e55eb4d2c81_Out_2_Float.xxx), _Lerp_1b4b768f79f34ddaa538834a53b8e33b_Out_3_Vector3);
float3 _Property_413c2dab0db145e686fd951a3cf9b06d_Out_0_Vector3 = _Player_Position;
float3 _Subtract_16caad2935f64d4ca2a5c41c122c2899_Out_2_Vector3;
Unity_Subtract_float3(IN.AbsoluteWorldSpacePosition, _Property_413c2dab0db145e686fd951a3cf9b06d_Out_0_Vector3, _Subtract_16caad2935f64d4ca2a5c41c122c2899_Out_2_Vector3);
float _Split_dd517b5d363349dfbcc6a80463d0a69c_R_1_Float = _Subtract_16caad2935f64d4ca2a5c41c122c2899_Out_2_Vector3[0];
float _Split_dd517b5d363349dfbcc6a80463d0a69c_G_2_Float = _Subtract_16caad2935f64d4ca2a5c41c122c2899_Out_2_Vector3[1];
float _Split_dd517b5d363349dfbcc6a80463d0a69c_B_3_Float = _Subtract_16caad2935f64d4ca2a5c41c122c2899_Out_2_Vector3[2];
float _Split_dd517b5d363349dfbcc6a80463d0a69c_A_4_Float = 0;
float _Property_f88a2d673c09471cad200fa3c1b6ae30_Out_0_Float = _Bend_Strength;
float _Multiply_6c5ecb62197a4848b2fabec6f920ae09_Out_2_Float;
Unity_Multiply_float_float(7, _Property_f88a2d673c09471cad200fa3c1b6ae30_Out_0_Float, _Multiply_6c5ecb62197a4848b2fabec6f920ae09_Out_2_Float);
float _Subtract_fabc7232020f481398d5e1c34e958838_Out_2_Float;
Unity_Subtract_float(_Split_dd517b5d363349dfbcc6a80463d0a69c_G_2_Float, _Multiply_6c5ecb62197a4848b2fabec6f920ae09_Out_2_Float, _Subtract_fabc7232020f481398d5e1c34e958838_Out_2_Float);
float4 _Combine_e1e6724c2316432f9d0cefb980576bd6_RGBA_4_Vector4;
float3 _Combine_e1e6724c2316432f9d0cefb980576bd6_RGB_5_Vector3;
float2 _Combine_e1e6724c2316432f9d0cefb980576bd6_RG_6_Vector2;
Unity_Combine_float(_Split_dd517b5d363349dfbcc6a80463d0a69c_R_1_Float, _Subtract_fabc7232020f481398d5e1c34e958838_Out_2_Float, _Split_dd517b5d363349dfbcc6a80463d0a69c_B_3_Float, float(0), _Combine_e1e6724c2316432f9d0cefb980576bd6_RGBA_4_Vector4, _Combine_e1e6724c2316432f9d0cefb980576bd6_RGB_5_Vector3, _Combine_e1e6724c2316432f9d0cefb980576bd6_RG_6_Vector2);
float3 _Multiply_42e953d7da1c4e7fbe777341488bae83_Out_2_Vector3;
Unity_Multiply_float3_float3(_Combine_e1e6724c2316432f9d0cefb980576bd6_RGB_5_Vector3, (_Property_f88a2d673c09471cad200fa3c1b6ae30_Out_0_Float.xxx), _Multiply_42e953d7da1c4e7fbe777341488bae83_Out_2_Vector3);
float _Property_ad55f894067547979f44b3a570bf7d49_Out_0_Float = _Start_Bend_Range;
float _Multiply_a7265b1673054e0996cb483d2ac3010d_Out_2_Float;
Unity_Multiply_float_float(_Property_ad55f894067547979f44b3a570bf7d49_Out_0_Float, 1.5, _Multiply_a7265b1673054e0996cb483d2ac3010d_Out_2_Float);
float _Distance_41bd00a9f1be4373a1003dc1e109849c_Out_2_Float;
Unity_Distance_float3(_Property_413c2dab0db145e686fd951a3cf9b06d_Out_0_Vector3, IN.AbsoluteWorldSpacePosition, _Distance_41bd00a9f1be4373a1003dc1e109849c_Out_2_Float);
float _Clamp_61013f1b8a7946cabf0f461c9754f564_Out_3_Float;
Unity_Clamp_float(_Distance_41bd00a9f1be4373a1003dc1e109849c_Out_2_Float, float(0), _Multiply_a7265b1673054e0996cb483d2ac3010d_Out_2_Float, _Clamp_61013f1b8a7946cabf0f461c9754f564_Out_3_Float);
float _Subtract_6193cd2daf004574816768560ab361b4_Out_2_Float;
Unity_Subtract_float(_Multiply_a7265b1673054e0996cb483d2ac3010d_Out_2_Float, _Clamp_61013f1b8a7946cabf0f461c9754f564_Out_3_Float, _Subtract_6193cd2daf004574816768560ab361b4_Out_2_Float);
float3 _Multiply_49122dc502124ec0b62c5e8d9df0b640_Out_2_Vector3;
Unity_Multiply_float3_float3(_Multiply_42e953d7da1c4e7fbe777341488bae83_Out_2_Vector3, (_Subtract_6193cd2daf004574816768560ab361b4_Out_2_Float.xxx), _Multiply_49122dc502124ec0b62c5e8d9df0b640_Out_2_Vector3);
float3 _Add_edf7ac44af404fdbbe1d0c50b7bc8b0f_Out_2_Vector3;
Unity_Add_float3(IN.ObjectSpacePosition, _Multiply_49122dc502124ec0b62c5e8d9df0b640_Out_2_Vector3, _Add_edf7ac44af404fdbbe1d0c50b7bc8b0f_Out_2_Vector3);
float3 _Normalize_0901e7953f394045adc844d92565cfba_Out_1_Vector3;
Unity_Normalize_float3(IN.ObjectSpacePosition, _Normalize_0901e7953f394045adc844d92565cfba_Out_1_Vector3);
float _Split_c58f3da36534495d8f4633fe4c40a74c_R_1_Float = _Normalize_0901e7953f394045adc844d92565cfba_Out_1_Vector3[0];
float _Split_c58f3da36534495d8f4633fe4c40a74c_G_2_Float = _Normalize_0901e7953f394045adc844d92565cfba_Out_1_Vector3[1];
float _Split_c58f3da36534495d8f4633fe4c40a74c_B_3_Float = _Normalize_0901e7953f394045adc844d92565cfba_Out_1_Vector3[2];
float _Split_c58f3da36534495d8f4633fe4c40a74c_A_4_Float = 0;
float _OneMinus_5a83b2c5314e45b2a835abb20d70bd22_Out_1_Float;
Unity_OneMinus_float(_Split_c58f3da36534495d8f4633fe4c40a74c_G_2_Float, _OneMinus_5a83b2c5314e45b2a835abb20d70bd22_Out_1_Float);
float _Step_428aa74b40a6492ba1e5a63ecfe4b53e_Out_2_Float;
Unity_Step_float(_OneMinus_5a83b2c5314e45b2a835abb20d70bd22_Out_1_Float, float(1), _Step_428aa74b40a6492ba1e5a63ecfe4b53e_Out_2_Float);
float3 _Lerp_7404d23a49244d1a8ca1c221f25d981c_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _Add_edf7ac44af404fdbbe1d0c50b7bc8b0f_Out_2_Vector3, (_Step_428aa74b40a6492ba1e5a63ecfe4b53e_Out_2_Float.xxx), _Lerp_7404d23a49244d1a8ca1c221f25d981c_Out_3_Vector3);
float3 _Lerp_93b3b392cffa4e688d1b32f6298c4907_Out_3_Vector3;
Unity_Lerp_float3(_Lerp_1b4b768f79f34ddaa538834a53b8e33b_Out_3_Vector3, _Lerp_7404d23a49244d1a8ca1c221f25d981c_Out_3_Vector3, (_GradientNoise_d9ac409c304041c49e680e55eb4d2c81_Out_2_Float.xxx), _Lerp_93b3b392cffa4e688d1b32f6298c4907_Out_3_Vector3);
float3 _UPGFoliageContactPositionCustomFunction_4d51d42c561a43cc976f6db5b768a1c6_bentPositionOS_1_Vector3;
UPG_FoliageContactPosition_float(_Lerp_93b3b392cffa4e688d1b32f6298c4907_Out_3_Vector3, _UPGFoliageContactPositionCustomFunction_4d51d42c561a43cc976f6db5b768a1c6_bentPositionOS_1_Vector3);
description.Position = _UPGFoliageContactPositionCustomFunction_4d51d42c561a43cc976f6db5b768a1c6_bentPositionOS_1_Vector3;
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
UnityTexture2D _Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Texture, sampler_Texture, _Texture_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D.tex, _Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D.samplerstate, _Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4, _Property_2bf35a7e94f443599024d5a3eb431991_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_R_4_Float = _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4.r;
float _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_G_5_Float = _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4.g;
float _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_B_6_Float = _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4.b;
float _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_A_7_Float = _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_RGBA_0_Vector4.a;
float _Property_ae6b5992b5874ce1b905e7962780a6be_Out_0_Float = _Alpha_Cutoff;
surface.BaseColor = IsGammaSpace() ? float3(0, 0, 0) : SRGBToLinear(float3(0, 0, 0));
surface.Alpha = _SampleTexture2D_1e4d0e9e122042b58cbdcf9251a6571a_A_7_Float;
surface.AlphaClipThreshold = _Property_ae6b5992b5874ce1b905e7962780a6be_Out_0_Float;
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
    output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
    output.AbsoluteWorldSpacePosition =                 GetAbsolutePositionWS(TransformObjectToWorld(input.positionOS));
    output.uv0 =                                        input.uv0;
    output.VertexColor =                                input.color;
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
CustomEditor "UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI"
CustomEditorForRenderPipeline "UnityEditor.ShaderGraphLitGUI" "UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset"
FallBack "Hidden/Shader Graph/FallbackError"
}