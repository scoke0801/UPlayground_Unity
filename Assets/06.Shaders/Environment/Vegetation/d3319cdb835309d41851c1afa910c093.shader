Shader "UPlayGround/Environment/Vegetation/d3319cdb835309d41851c1afa910c093"
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

_Smoothness("Smoothness", Float) = 0.5
_Color_Tint_Top("Color Tint Top", Color) = (0.04907036, 1, 0, 0)
_Color_Tint_Bottom("Color Tint Bottom", Color) = (0.04907036, 1, 0, 0)
[NoScaleOffset]_Base_Color("Base Color", 2D) = "white" {}
_Terrain_Blend_Pow("Terrain Blend Pow", Float) = 8.4
_Terrain_Blend_Mul("Terrain Blend Mul", Float) = 2
_Cloud_Color("Cloud Color", Color) = (0, 0, 0, 0)
_Cloud_Scale("Cloud Scale", Float) = 3000
_Cloud_Speed("Cloud Speed", Vector, 2) = (0.4, 0.13, 0, 0)
_Grass_Pivot_Lock_Strength("Grass Pivot Lock Strength", Float) = 1.5
_Wind_Small_Speed("Wind Small Speed", Vector, 2) = (0, 2, 0, 0)
_Wind_Small_Scale("Wind Small Scale", Float) = 500
_Wind_Small_Multiplier("Wind Small Multiplier", Float) = 0.1
_Wind_Large_Speed("Wind Large Speed", Vector, 2) = (0, 2, 0, 0)
_Wind_Large_Scale("Wind Large Scale", Float) = 500
_Wind_Large_Multiplier("Wind Large Multiplier", Float) = 0.1
[HDR]_Emissive_Color("Emissive Color", Color) = (0.04091521, 0.09084173, 0, 1)
[HDR]_Emissive_Clouds("Emissive Clouds", Color) = (0.2196079, 0.3333333, 0.227451, 1)
_Emissive_Intenstiy("Emissive Intenstiy", Float) = 0.5
_Emissive_Intenstiy_Clouds("Emissive Intenstiy Clouds", Float) = 0.15
_Gradience_Mul("Gradience Mul", Float) = 1
_Gradience_Pow("Gradience Pow", Float) = 1
[NonModifiableTextureData][NoScaleOffset]_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D("Texture2D", 2D) = "white" {}
[NonModifiableTextureData][NoScaleOffset]_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D("Texture2D", 2D) = "white" {}
[NonModifiableTextureData][NoScaleOffset]_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D("Texture2D", 2D) = "white" {}
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
[HideInInspector]_BUILTIN_CullMode("Float", Float) = 0
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
Cull Off
Blend One Zero
ZTest LEqual
ZWrite On
AlphaToMask On

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
// GraphKeywords: <None>

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
#define _ALPHATEST_ON 1


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
 float3 TangentSpaceNormal;
 float3 WorldSpacePosition;
 float4 uv0;
 float3 TimeParameters;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 WorldSpacePosition;
 float4 uv0;
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

float4 _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D_TexelSize;
float4 _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D_TexelSize;
float4 _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D_TexelSize;
float4 _Color_Tint_Bottom;
float2 _Cloud_Speed;
float _Emissive_Intenstiy_Clouds;
float _Wind_Large_Scale;
float _Terrain_Blend_Mul;
float _Cloud_Scale;
float2 _Wind_Large_Speed;
float _Wind_Small_Multiplier;
float4 _Cloud_Color;
float4 _Color_Tint_Top;
float4 _Base_Color_TexelSize;
float _Grass_Pivot_Lock_Strength;
float _Wind_Large_Multiplier;
float2 _Wind_Small_Speed;
float _Wind_Small_Scale;
float _Terrain_Blend_Pow;
float _Smoothness;
float4 _Emissive_Color;
float4 _Emissive_Clouds;
float _Gradience_Mul;
float _Gradience_Pow;
float _Emissive_Intenstiy;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D);
TEXTURE2D(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D);
TEXTURE2D(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D);
TEXTURE2D(_Base_Color);
SAMPLER(sampler_Base_Color);

// Graph Includes
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

void Unity_Divide_float3(float3 A, float3 B, out float3 Out)
{
    Out = A / B;
}

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}

void Unity_Divide_float2(float2 A, float2 B, out float2 Out)
{
    Out = A / B;
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_Add_float2(float2 A, float2 B, out float2 Out)
{
    Out = A + B;
}

struct Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float
{
float3 WorldSpacePosition;
float3 TimeParameters;
};

void SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(float _Cloud_Scale, float2 _Cloud_Speed, Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float IN, out float2 OutVector2_1)
{
float _Property_da571d91c08d469b8fcdb0a2a1aa2cc1_Out_0_Float = _Cloud_Scale;
float3 _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3;
Unity_Divide_float3(IN.WorldSpacePosition, (_Property_da571d91c08d469b8fcdb0a2a1aa2cc1_Out_0_Float.xxx), _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3);
float _Split_c9bf902bb4344b6a837c002876a4a742_R_1_Float = _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3[0];
float _Split_c9bf902bb4344b6a837c002876a4a742_G_2_Float = _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3[1];
float _Split_c9bf902bb4344b6a837c002876a4a742_B_3_Float = _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3[2];
float _Split_c9bf902bb4344b6a837c002876a4a742_A_4_Float = 0;
float4 _Combine_b2458545efe84f8488335a6a0e9829f0_RGBA_4_Vector4;
float3 _Combine_b2458545efe84f8488335a6a0e9829f0_RGB_5_Vector3;
float2 _Combine_b2458545efe84f8488335a6a0e9829f0_RG_6_Vector2;
Unity_Combine_float(_Split_c9bf902bb4344b6a837c002876a4a742_R_1_Float, _Split_c9bf902bb4344b6a837c002876a4a742_B_3_Float, float(0), float(0), _Combine_b2458545efe84f8488335a6a0e9829f0_RGBA_4_Vector4, _Combine_b2458545efe84f8488335a6a0e9829f0_RGB_5_Vector3, _Combine_b2458545efe84f8488335a6a0e9829f0_RG_6_Vector2);
float2 _Property_36f0a343bbd443b0ba9ba96752bd5419_Out_0_Vector2 = _Cloud_Speed;
float2 _Divide_4d6b2552e0254de6a213dc9020221a26_Out_2_Vector2;
Unity_Divide_float2(_Property_36f0a343bbd443b0ba9ba96752bd5419_Out_0_Vector2, float2(100, 100), _Divide_4d6b2552e0254de6a213dc9020221a26_Out_2_Vector2);
float2 _Multiply_fb765c606df846c29196161ce8a5c5fe_Out_2_Vector2;
Unity_Multiply_float2_float2((IN.TimeParameters.x.xx), _Divide_4d6b2552e0254de6a213dc9020221a26_Out_2_Vector2, _Multiply_fb765c606df846c29196161ce8a5c5fe_Out_2_Vector2);
float2 _Add_4e43f841f4184513872472164ca37f4a_Out_2_Vector2;
Unity_Add_float2(_Combine_b2458545efe84f8488335a6a0e9829f0_RG_6_Vector2, _Multiply_fb765c606df846c29196161ce8a5c5fe_Out_2_Vector2, _Add_4e43f841f4184513872472164ca37f4a_Out_2_Vector2);
OutVector2_1 = _Add_4e43f841f4184513872472164ca37f4a_Out_2_Vector2;
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Power_float(float A, float B, out float Out)
{
    Out = pow(A, B);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
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
float _Property_4116819b6d874ed78027c7f0b237cb76_Out_0_Float = _Wind_Large_Scale;
float2 _Property_7855d6ce09864024b8c68f043f8cbdb4_Out_0_Vector2 = _Wind_Large_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45;
_PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_4116819b6d874ed78027c7f0b237cb76_Out_0_Float, _Property_7855d6ce09864024b8c68f043f8cbdb4_Out_0_Vector2, _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45, _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_R_5_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_G_6_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_B_7_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_A_8_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.a;
float _Subtract_ab6f6b551d064bb8b607c9a9bc381611_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_R_5_Float, float(0.5), _Subtract_ab6f6b551d064bb8b607c9a9bc381611_Out_2_Float);
float4 _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4 = IN.uv0;
float _Split_6ffb9a554e6e4ede9196adfc250975db_R_1_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[0];
float _Split_6ffb9a554e6e4ede9196adfc250975db_G_2_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[1];
float _Split_6ffb9a554e6e4ede9196adfc250975db_B_3_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[2];
float _Split_6ffb9a554e6e4ede9196adfc250975db_A_4_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[3];
float _Property_6eb67bc74e6b4fb288d6164ab2c7cdd8_Out_0_Float = _Grass_Pivot_Lock_Strength;
float _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float;
Unity_Power_float(_Split_6ffb9a554e6e4ede9196adfc250975db_G_2_Float, _Property_6eb67bc74e6b4fb288d6164ab2c7cdd8_Out_0_Float, _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float);
float _Multiply_219e592917cc4b659707ce8e75b39d9e_Out_2_Float;
Unity_Multiply_float_float(_Subtract_ab6f6b551d064bb8b607c9a9bc381611_Out_2_Float, _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float, _Multiply_219e592917cc4b659707ce8e75b39d9e_Out_2_Float);
float _Property_20c86c0cde6c4232a79793b50e170771_Out_0_Float = _Wind_Large_Multiplier;
float _Multiply_91c05a4257804a449cb4304b1f563d42_Out_2_Float;
Unity_Multiply_float_float(_Multiply_219e592917cc4b659707ce8e75b39d9e_Out_2_Float, _Property_20c86c0cde6c4232a79793b50e170771_Out_0_Float, _Multiply_91c05a4257804a449cb4304b1f563d42_Out_2_Float);
float _Property_fe4ba3b7d8f14c98b41d8948b19f735c_Out_0_Float = _Wind_Small_Scale;
float2 _Property_793e0429eb224e7694ee6cb87c6f31b9_Out_0_Vector2 = _Wind_Small_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3;
_PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_fe4ba3b7d8f14c98b41d8948b19f735c_Out_0_Float, _Property_793e0429eb224e7694ee6cb87c6f31b9_Out_0_Vector2, _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3, _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_R_5_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_G_6_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_B_7_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_A_8_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.a;
float _Subtract_95ef509ca9e2441ca6e08508d02ef96e_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_R_5_Float, float(0.5), _Subtract_95ef509ca9e2441ca6e08508d02ef96e_Out_2_Float);
float _Multiply_344852ef2f3546caa8c4f1f3695c3f0d_Out_2_Float;
Unity_Multiply_float_float(_Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float, _Subtract_95ef509ca9e2441ca6e08508d02ef96e_Out_2_Float, _Multiply_344852ef2f3546caa8c4f1f3695c3f0d_Out_2_Float);
float _Property_0943d2961c4c4e2ba15fdd94ac828776_Out_0_Float = _Wind_Small_Multiplier;
float _Multiply_4d6a0142972c48e889dfdbf32703e73e_Out_2_Float;
Unity_Multiply_float_float(_Multiply_344852ef2f3546caa8c4f1f3695c3f0d_Out_2_Float, _Property_0943d2961c4c4e2ba15fdd94ac828776_Out_0_Float, _Multiply_4d6a0142972c48e889dfdbf32703e73e_Out_2_Float);
float3 _Add_b8880c59f8304f648cf8583ead4fb728_Out_2_Vector3;
Unity_Add_float3(IN.WorldSpacePosition, (_Multiply_4d6a0142972c48e889dfdbf32703e73e_Out_2_Float.xxx), _Add_b8880c59f8304f648cf8583ead4fb728_Out_2_Vector3);
float3 _Add_48a8fd066e7e49e4891d1ae97e03bb65_Out_2_Vector3;
Unity_Add_float3((_Multiply_91c05a4257804a449cb4304b1f563d42_Out_2_Float.xxx), _Add_b8880c59f8304f648cf8583ead4fb728_Out_2_Vector3, _Add_48a8fd066e7e49e4891d1ae97e03bb65_Out_2_Vector3);
float3 _UPGFoliageBendWSCustomFunction_dc96fa4b2cc8432d856f03b9268115fe_offsetWS_2_Vector3;
UPG_FoliageBendWS_float(IN.WorldSpacePosition, _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float, _UPGFoliageBendWSCustomFunction_dc96fa4b2cc8432d856f03b9268115fe_offsetWS_2_Vector3);
float3 _Add_c1034f24720f4c5f9634821d7037f0ae_Out_2_Vector3;
Unity_Add_float3(_Add_48a8fd066e7e49e4891d1ae97e03bb65_Out_2_Vector3, _UPGFoliageBendWSCustomFunction_dc96fa4b2cc8432d856f03b9268115fe_offsetWS_2_Vector3, _Add_c1034f24720f4c5f9634821d7037f0ae_Out_2_Vector3);
float3 _Transform_583ec64076274514891c3c1e7f9946aa_Out_1_Vector3;
_Transform_583ec64076274514891c3c1e7f9946aa_Out_1_Vector3 = TransformWorldToObject(_Add_c1034f24720f4c5f9634821d7037f0ae_Out_2_Vector3.xyz);
float3 _Vector3_c086b8a0e95a4c27b6fc4d4d3843bd3b_Out_0_Vector3 = float3(float(0), float(1), float(0));
description.Position = _Transform_583ec64076274514891c3c1e7f9946aa_Out_1_Vector3;
description.Normal = _Vector3_c086b8a0e95a4c27b6fc4d4d3843bd3b_Out_0_Vector3;
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
float4 _Property_80081465fc9e42c78d044b56c43e5e1f_Out_0_Vector4 = _Color_Tint_Bottom;
float4 _Property_39c423b8c80d4d97946dc0b2c79b23c9_Out_0_Vector4 = _Color_Tint_Top;
float4 _UV_0a040733d8e9420d9054f7edb74c7682_Out_0_Vector4 = IN.uv0;
float _Split_e789d7c2ff9f4972bdb3462091aa8bfb_R_1_Float = _UV_0a040733d8e9420d9054f7edb74c7682_Out_0_Vector4[0];
float _Split_e789d7c2ff9f4972bdb3462091aa8bfb_G_2_Float = _UV_0a040733d8e9420d9054f7edb74c7682_Out_0_Vector4[1];
float _Split_e789d7c2ff9f4972bdb3462091aa8bfb_B_3_Float = _UV_0a040733d8e9420d9054f7edb74c7682_Out_0_Vector4[2];
float _Split_e789d7c2ff9f4972bdb3462091aa8bfb_A_4_Float = _UV_0a040733d8e9420d9054f7edb74c7682_Out_0_Vector4[3];
float _Property_5bf30ba4b92c4b7d81755e8764fdd331_Out_0_Float = _Gradience_Mul;
float _Multiply_0e8bd3c9e0bb453f8ddc49e301bbbe74_Out_2_Float;
Unity_Multiply_float_float(_Split_e789d7c2ff9f4972bdb3462091aa8bfb_G_2_Float, _Property_5bf30ba4b92c4b7d81755e8764fdd331_Out_0_Float, _Multiply_0e8bd3c9e0bb453f8ddc49e301bbbe74_Out_2_Float);
float _Property_92651d8cf9f8486f97f96477fee69822_Out_0_Float = _Gradience_Pow;
float _Power_21a1ac3e3c704dc2a230b301a4fd1bc3_Out_2_Float;
Unity_Power_float(_Multiply_0e8bd3c9e0bb453f8ddc49e301bbbe74_Out_2_Float, _Property_92651d8cf9f8486f97f96477fee69822_Out_0_Float, _Power_21a1ac3e3c704dc2a230b301a4fd1bc3_Out_2_Float);
float _Saturate_df6319cd6a8f44b8a2f98a45b86570e3_Out_1_Float;
Unity_Saturate_float(_Power_21a1ac3e3c704dc2a230b301a4fd1bc3_Out_2_Float, _Saturate_df6319cd6a8f44b8a2f98a45b86570e3_Out_1_Float);
float4 _Lerp_aef25ae41dbd4bb899f648885331bf52_Out_3_Vector4;
Unity_Lerp_float4(_Property_80081465fc9e42c78d044b56c43e5e1f_Out_0_Vector4, _Property_39c423b8c80d4d97946dc0b2c79b23c9_Out_0_Vector4, (_Saturate_df6319cd6a8f44b8a2f98a45b86570e3_Out_1_Float.xxxx), _Lerp_aef25ae41dbd4bb899f648885331bf52_Out_3_Vector4);
UnityTexture2D _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Base_Color, sampler_Base_Color, _Base_Color_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.tex, _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.samplerstate, _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4, _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_R_4_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.r;
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_G_5_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.g;
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_B_6_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.b;
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_A_7_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.a;
float4 _Multiply_fcd43956ee594716aa91c7691ce699cf_Out_2_Vector4;
Unity_Multiply_float4_float4(_Lerp_aef25ae41dbd4bb899f648885331bf52_Out_3_Vector4, _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4, _Multiply_fcd43956ee594716aa91c7691ce699cf_Out_2_Vector4);
float4 _Property_1d0cacb8faa34e6580302336f32b8591_Out_0_Vector4 = _Cloud_Color;
float4 _Multiply_17a3d0b90ebb4b1b9cab04c57f32aed2_Out_2_Vector4;
Unity_Multiply_float4_float4(_Multiply_fcd43956ee594716aa91c7691ce699cf_Out_2_Vector4, _Property_1d0cacb8faa34e6580302336f32b8591_Out_0_Vector4, _Multiply_17a3d0b90ebb4b1b9cab04c57f32aed2_Out_2_Vector4);
float _Property_8a8a73164ab1462fbb02778830bd9a39_Out_0_Float = _Cloud_Scale;
float2 _Property_b7fc6981bb03499fb69f338ec3fe9525_Out_0_Vector2 = _Cloud_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_dc86dca30b514354a0931aeb1998f16e;
_PositionalUVs_dc86dca30b514354a0931aeb1998f16e.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_dc86dca30b514354a0931aeb1998f16e.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_dc86dca30b514354a0931aeb1998f16e_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_8a8a73164ab1462fbb02778830bd9a39_Out_0_Float, _Property_b7fc6981bb03499fb69f338ec3fe9525_Out_0_Vector2, _PositionalUVs_dc86dca30b514354a0931aeb1998f16e, _PositionalUVs_dc86dca30b514354a0931aeb1998f16e_OutVector2_1_Vector2);
float4 _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(UnityBuildTexture2DStructNoScale(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_dc86dca30b514354a0931aeb1998f16e_OutVector2_1_Vector2) );
if (UnityBuildTexture2DStructNoScale(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D).hdrDecode.x > 0)
_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4, UnityBuildTexture2DStructNoScale(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D).hdrDecode);
float _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_R_4_Float = _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4.r;
float _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_G_5_Float = _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4.g;
float _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_B_6_Float = _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4.b;
float _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_A_7_Float = _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4.a;
float4 _Lerp_3bb853efd4d6410a8b63501c276cee08_Out_3_Vector4;
Unity_Lerp_float4(_Multiply_fcd43956ee594716aa91c7691ce699cf_Out_2_Vector4, _Multiply_17a3d0b90ebb4b1b9cab04c57f32aed2_Out_2_Vector4, (_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_R_4_Float.xxxx), _Lerp_3bb853efd4d6410a8b63501c276cee08_Out_3_Vector4);
float _Property_1336c01c0e674957b651c4aa288cb981_Out_0_Float = _Emissive_Intenstiy;
float _Property_f2ab9978dd8748fc98c66f03d7d2d5d4_Out_0_Float = _Emissive_Intenstiy_Clouds;
float _Lerp_b7e37fa8b0714a3b953fa8ba9bfe3919_Out_3_Float;
Unity_Lerp_float(_Property_1336c01c0e674957b651c4aa288cb981_Out_0_Float, _Property_f2ab9978dd8748fc98c66f03d7d2d5d4_Out_0_Float, _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_R_4_Float, _Lerp_b7e37fa8b0714a3b953fa8ba9bfe3919_Out_3_Float);
float4 _Multiply_c9eb85122f184daa97bd477002a538c5_Out_2_Vector4;
Unity_Multiply_float4_float4(_Lerp_3bb853efd4d6410a8b63501c276cee08_Out_3_Vector4, (_Lerp_b7e37fa8b0714a3b953fa8ba9bfe3919_Out_3_Float.xxxx), _Multiply_c9eb85122f184daa97bd477002a538c5_Out_2_Vector4);
float _Property_54e4601fd6944f448e3ee0aeac26aefa_Out_0_Float = _Smoothness;
surface.BaseColor = (_Lerp_3bb853efd4d6410a8b63501c276cee08_Out_3_Vector4.xyz);
surface.NormalTS = IN.TangentSpaceNormal;
surface.Emission = (_Multiply_c9eb85122f184daa97bd477002a538c5_Out_2_Vector4.xyz);
surface.Metallic = float(0);
surface.Smoothness = _Property_54e4601fd6944f448e3ee0aeac26aefa_Out_0_Float;
surface.Occlusion = float(1);
surface.Alpha = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_A_7_Float;
surface.AlphaClipThreshold = float(0.5);
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
    output.uv0 =                                        input.uv0;
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


    output.WorldSpacePosition = input.positionWS;

    #if UNITY_UV_STARTS_AT_TOP
    #else
    #endif


    output.uv0 = input.texCoord0;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
    output.TimeParameters = _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
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
Cull Off
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
// GraphKeywords: <None>

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
#define _ALPHATEST_ON 1


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
 float3 TangentSpaceNormal;
 float3 WorldSpacePosition;
 float4 uv0;
 float3 TimeParameters;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 WorldSpacePosition;
 float4 uv0;
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

float4 _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D_TexelSize;
float4 _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D_TexelSize;
float4 _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D_TexelSize;
float4 _Color_Tint_Bottom;
float2 _Cloud_Speed;
float _Emissive_Intenstiy_Clouds;
float _Wind_Large_Scale;
float _Terrain_Blend_Mul;
float _Cloud_Scale;
float2 _Wind_Large_Speed;
float _Wind_Small_Multiplier;
float4 _Cloud_Color;
float4 _Color_Tint_Top;
float4 _Base_Color_TexelSize;
float _Grass_Pivot_Lock_Strength;
float _Wind_Large_Multiplier;
float2 _Wind_Small_Speed;
float _Wind_Small_Scale;
float _Terrain_Blend_Pow;
float _Smoothness;
float4 _Emissive_Color;
float4 _Emissive_Clouds;
float _Gradience_Mul;
float _Gradience_Pow;
float _Emissive_Intenstiy;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D);
TEXTURE2D(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D);
TEXTURE2D(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D);
TEXTURE2D(_Base_Color);
SAMPLER(sampler_Base_Color);

// Graph Includes
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

void Unity_Divide_float3(float3 A, float3 B, out float3 Out)
{
    Out = A / B;
}

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}

void Unity_Divide_float2(float2 A, float2 B, out float2 Out)
{
    Out = A / B;
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_Add_float2(float2 A, float2 B, out float2 Out)
{
    Out = A + B;
}

struct Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float
{
float3 WorldSpacePosition;
float3 TimeParameters;
};

void SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(float _Cloud_Scale, float2 _Cloud_Speed, Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float IN, out float2 OutVector2_1)
{
float _Property_da571d91c08d469b8fcdb0a2a1aa2cc1_Out_0_Float = _Cloud_Scale;
float3 _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3;
Unity_Divide_float3(IN.WorldSpacePosition, (_Property_da571d91c08d469b8fcdb0a2a1aa2cc1_Out_0_Float.xxx), _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3);
float _Split_c9bf902bb4344b6a837c002876a4a742_R_1_Float = _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3[0];
float _Split_c9bf902bb4344b6a837c002876a4a742_G_2_Float = _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3[1];
float _Split_c9bf902bb4344b6a837c002876a4a742_B_3_Float = _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3[2];
float _Split_c9bf902bb4344b6a837c002876a4a742_A_4_Float = 0;
float4 _Combine_b2458545efe84f8488335a6a0e9829f0_RGBA_4_Vector4;
float3 _Combine_b2458545efe84f8488335a6a0e9829f0_RGB_5_Vector3;
float2 _Combine_b2458545efe84f8488335a6a0e9829f0_RG_6_Vector2;
Unity_Combine_float(_Split_c9bf902bb4344b6a837c002876a4a742_R_1_Float, _Split_c9bf902bb4344b6a837c002876a4a742_B_3_Float, float(0), float(0), _Combine_b2458545efe84f8488335a6a0e9829f0_RGBA_4_Vector4, _Combine_b2458545efe84f8488335a6a0e9829f0_RGB_5_Vector3, _Combine_b2458545efe84f8488335a6a0e9829f0_RG_6_Vector2);
float2 _Property_36f0a343bbd443b0ba9ba96752bd5419_Out_0_Vector2 = _Cloud_Speed;
float2 _Divide_4d6b2552e0254de6a213dc9020221a26_Out_2_Vector2;
Unity_Divide_float2(_Property_36f0a343bbd443b0ba9ba96752bd5419_Out_0_Vector2, float2(100, 100), _Divide_4d6b2552e0254de6a213dc9020221a26_Out_2_Vector2);
float2 _Multiply_fb765c606df846c29196161ce8a5c5fe_Out_2_Vector2;
Unity_Multiply_float2_float2((IN.TimeParameters.x.xx), _Divide_4d6b2552e0254de6a213dc9020221a26_Out_2_Vector2, _Multiply_fb765c606df846c29196161ce8a5c5fe_Out_2_Vector2);
float2 _Add_4e43f841f4184513872472164ca37f4a_Out_2_Vector2;
Unity_Add_float2(_Combine_b2458545efe84f8488335a6a0e9829f0_RG_6_Vector2, _Multiply_fb765c606df846c29196161ce8a5c5fe_Out_2_Vector2, _Add_4e43f841f4184513872472164ca37f4a_Out_2_Vector2);
OutVector2_1 = _Add_4e43f841f4184513872472164ca37f4a_Out_2_Vector2;
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Power_float(float A, float B, out float Out)
{
    Out = pow(A, B);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
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
float _Property_4116819b6d874ed78027c7f0b237cb76_Out_0_Float = _Wind_Large_Scale;
float2 _Property_7855d6ce09864024b8c68f043f8cbdb4_Out_0_Vector2 = _Wind_Large_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45;
_PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_4116819b6d874ed78027c7f0b237cb76_Out_0_Float, _Property_7855d6ce09864024b8c68f043f8cbdb4_Out_0_Vector2, _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45, _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_R_5_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_G_6_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_B_7_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_A_8_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.a;
float _Subtract_ab6f6b551d064bb8b607c9a9bc381611_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_R_5_Float, float(0.5), _Subtract_ab6f6b551d064bb8b607c9a9bc381611_Out_2_Float);
float4 _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4 = IN.uv0;
float _Split_6ffb9a554e6e4ede9196adfc250975db_R_1_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[0];
float _Split_6ffb9a554e6e4ede9196adfc250975db_G_2_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[1];
float _Split_6ffb9a554e6e4ede9196adfc250975db_B_3_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[2];
float _Split_6ffb9a554e6e4ede9196adfc250975db_A_4_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[3];
float _Property_6eb67bc74e6b4fb288d6164ab2c7cdd8_Out_0_Float = _Grass_Pivot_Lock_Strength;
float _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float;
Unity_Power_float(_Split_6ffb9a554e6e4ede9196adfc250975db_G_2_Float, _Property_6eb67bc74e6b4fb288d6164ab2c7cdd8_Out_0_Float, _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float);
float _Multiply_219e592917cc4b659707ce8e75b39d9e_Out_2_Float;
Unity_Multiply_float_float(_Subtract_ab6f6b551d064bb8b607c9a9bc381611_Out_2_Float, _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float, _Multiply_219e592917cc4b659707ce8e75b39d9e_Out_2_Float);
float _Property_20c86c0cde6c4232a79793b50e170771_Out_0_Float = _Wind_Large_Multiplier;
float _Multiply_91c05a4257804a449cb4304b1f563d42_Out_2_Float;
Unity_Multiply_float_float(_Multiply_219e592917cc4b659707ce8e75b39d9e_Out_2_Float, _Property_20c86c0cde6c4232a79793b50e170771_Out_0_Float, _Multiply_91c05a4257804a449cb4304b1f563d42_Out_2_Float);
float _Property_fe4ba3b7d8f14c98b41d8948b19f735c_Out_0_Float = _Wind_Small_Scale;
float2 _Property_793e0429eb224e7694ee6cb87c6f31b9_Out_0_Vector2 = _Wind_Small_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3;
_PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_fe4ba3b7d8f14c98b41d8948b19f735c_Out_0_Float, _Property_793e0429eb224e7694ee6cb87c6f31b9_Out_0_Vector2, _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3, _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_R_5_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_G_6_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_B_7_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_A_8_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.a;
float _Subtract_95ef509ca9e2441ca6e08508d02ef96e_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_R_5_Float, float(0.5), _Subtract_95ef509ca9e2441ca6e08508d02ef96e_Out_2_Float);
float _Multiply_344852ef2f3546caa8c4f1f3695c3f0d_Out_2_Float;
Unity_Multiply_float_float(_Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float, _Subtract_95ef509ca9e2441ca6e08508d02ef96e_Out_2_Float, _Multiply_344852ef2f3546caa8c4f1f3695c3f0d_Out_2_Float);
float _Property_0943d2961c4c4e2ba15fdd94ac828776_Out_0_Float = _Wind_Small_Multiplier;
float _Multiply_4d6a0142972c48e889dfdbf32703e73e_Out_2_Float;
Unity_Multiply_float_float(_Multiply_344852ef2f3546caa8c4f1f3695c3f0d_Out_2_Float, _Property_0943d2961c4c4e2ba15fdd94ac828776_Out_0_Float, _Multiply_4d6a0142972c48e889dfdbf32703e73e_Out_2_Float);
float3 _Add_b8880c59f8304f648cf8583ead4fb728_Out_2_Vector3;
Unity_Add_float3(IN.WorldSpacePosition, (_Multiply_4d6a0142972c48e889dfdbf32703e73e_Out_2_Float.xxx), _Add_b8880c59f8304f648cf8583ead4fb728_Out_2_Vector3);
float3 _Add_48a8fd066e7e49e4891d1ae97e03bb65_Out_2_Vector3;
Unity_Add_float3((_Multiply_91c05a4257804a449cb4304b1f563d42_Out_2_Float.xxx), _Add_b8880c59f8304f648cf8583ead4fb728_Out_2_Vector3, _Add_48a8fd066e7e49e4891d1ae97e03bb65_Out_2_Vector3);
float3 _UPGFoliageBendWSCustomFunction_dc96fa4b2cc8432d856f03b9268115fe_offsetWS_2_Vector3;
UPG_FoliageBendWS_float(IN.WorldSpacePosition, _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float, _UPGFoliageBendWSCustomFunction_dc96fa4b2cc8432d856f03b9268115fe_offsetWS_2_Vector3);
float3 _Add_c1034f24720f4c5f9634821d7037f0ae_Out_2_Vector3;
Unity_Add_float3(_Add_48a8fd066e7e49e4891d1ae97e03bb65_Out_2_Vector3, _UPGFoliageBendWSCustomFunction_dc96fa4b2cc8432d856f03b9268115fe_offsetWS_2_Vector3, _Add_c1034f24720f4c5f9634821d7037f0ae_Out_2_Vector3);
float3 _Transform_583ec64076274514891c3c1e7f9946aa_Out_1_Vector3;
_Transform_583ec64076274514891c3c1e7f9946aa_Out_1_Vector3 = TransformWorldToObject(_Add_c1034f24720f4c5f9634821d7037f0ae_Out_2_Vector3.xyz);
float3 _Vector3_c086b8a0e95a4c27b6fc4d4d3843bd3b_Out_0_Vector3 = float3(float(0), float(1), float(0));
description.Position = _Transform_583ec64076274514891c3c1e7f9946aa_Out_1_Vector3;
description.Normal = _Vector3_c086b8a0e95a4c27b6fc4d4d3843bd3b_Out_0_Vector3;
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
float4 _Property_80081465fc9e42c78d044b56c43e5e1f_Out_0_Vector4 = _Color_Tint_Bottom;
float4 _Property_39c423b8c80d4d97946dc0b2c79b23c9_Out_0_Vector4 = _Color_Tint_Top;
float4 _UV_0a040733d8e9420d9054f7edb74c7682_Out_0_Vector4 = IN.uv0;
float _Split_e789d7c2ff9f4972bdb3462091aa8bfb_R_1_Float = _UV_0a040733d8e9420d9054f7edb74c7682_Out_0_Vector4[0];
float _Split_e789d7c2ff9f4972bdb3462091aa8bfb_G_2_Float = _UV_0a040733d8e9420d9054f7edb74c7682_Out_0_Vector4[1];
float _Split_e789d7c2ff9f4972bdb3462091aa8bfb_B_3_Float = _UV_0a040733d8e9420d9054f7edb74c7682_Out_0_Vector4[2];
float _Split_e789d7c2ff9f4972bdb3462091aa8bfb_A_4_Float = _UV_0a040733d8e9420d9054f7edb74c7682_Out_0_Vector4[3];
float _Property_5bf30ba4b92c4b7d81755e8764fdd331_Out_0_Float = _Gradience_Mul;
float _Multiply_0e8bd3c9e0bb453f8ddc49e301bbbe74_Out_2_Float;
Unity_Multiply_float_float(_Split_e789d7c2ff9f4972bdb3462091aa8bfb_G_2_Float, _Property_5bf30ba4b92c4b7d81755e8764fdd331_Out_0_Float, _Multiply_0e8bd3c9e0bb453f8ddc49e301bbbe74_Out_2_Float);
float _Property_92651d8cf9f8486f97f96477fee69822_Out_0_Float = _Gradience_Pow;
float _Power_21a1ac3e3c704dc2a230b301a4fd1bc3_Out_2_Float;
Unity_Power_float(_Multiply_0e8bd3c9e0bb453f8ddc49e301bbbe74_Out_2_Float, _Property_92651d8cf9f8486f97f96477fee69822_Out_0_Float, _Power_21a1ac3e3c704dc2a230b301a4fd1bc3_Out_2_Float);
float _Saturate_df6319cd6a8f44b8a2f98a45b86570e3_Out_1_Float;
Unity_Saturate_float(_Power_21a1ac3e3c704dc2a230b301a4fd1bc3_Out_2_Float, _Saturate_df6319cd6a8f44b8a2f98a45b86570e3_Out_1_Float);
float4 _Lerp_aef25ae41dbd4bb899f648885331bf52_Out_3_Vector4;
Unity_Lerp_float4(_Property_80081465fc9e42c78d044b56c43e5e1f_Out_0_Vector4, _Property_39c423b8c80d4d97946dc0b2c79b23c9_Out_0_Vector4, (_Saturate_df6319cd6a8f44b8a2f98a45b86570e3_Out_1_Float.xxxx), _Lerp_aef25ae41dbd4bb899f648885331bf52_Out_3_Vector4);
UnityTexture2D _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Base_Color, sampler_Base_Color, _Base_Color_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.tex, _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.samplerstate, _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4, _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_R_4_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.r;
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_G_5_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.g;
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_B_6_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.b;
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_A_7_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.a;
float4 _Multiply_fcd43956ee594716aa91c7691ce699cf_Out_2_Vector4;
Unity_Multiply_float4_float4(_Lerp_aef25ae41dbd4bb899f648885331bf52_Out_3_Vector4, _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4, _Multiply_fcd43956ee594716aa91c7691ce699cf_Out_2_Vector4);
float4 _Property_1d0cacb8faa34e6580302336f32b8591_Out_0_Vector4 = _Cloud_Color;
float4 _Multiply_17a3d0b90ebb4b1b9cab04c57f32aed2_Out_2_Vector4;
Unity_Multiply_float4_float4(_Multiply_fcd43956ee594716aa91c7691ce699cf_Out_2_Vector4, _Property_1d0cacb8faa34e6580302336f32b8591_Out_0_Vector4, _Multiply_17a3d0b90ebb4b1b9cab04c57f32aed2_Out_2_Vector4);
float _Property_8a8a73164ab1462fbb02778830bd9a39_Out_0_Float = _Cloud_Scale;
float2 _Property_b7fc6981bb03499fb69f338ec3fe9525_Out_0_Vector2 = _Cloud_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_dc86dca30b514354a0931aeb1998f16e;
_PositionalUVs_dc86dca30b514354a0931aeb1998f16e.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_dc86dca30b514354a0931aeb1998f16e.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_dc86dca30b514354a0931aeb1998f16e_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_8a8a73164ab1462fbb02778830bd9a39_Out_0_Float, _Property_b7fc6981bb03499fb69f338ec3fe9525_Out_0_Vector2, _PositionalUVs_dc86dca30b514354a0931aeb1998f16e, _PositionalUVs_dc86dca30b514354a0931aeb1998f16e_OutVector2_1_Vector2);
float4 _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(UnityBuildTexture2DStructNoScale(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_dc86dca30b514354a0931aeb1998f16e_OutVector2_1_Vector2) );
if (UnityBuildTexture2DStructNoScale(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D).hdrDecode.x > 0)
_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4, UnityBuildTexture2DStructNoScale(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D).hdrDecode);
float _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_R_4_Float = _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4.r;
float _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_G_5_Float = _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4.g;
float _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_B_6_Float = _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4.b;
float _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_A_7_Float = _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4.a;
float4 _Lerp_3bb853efd4d6410a8b63501c276cee08_Out_3_Vector4;
Unity_Lerp_float4(_Multiply_fcd43956ee594716aa91c7691ce699cf_Out_2_Vector4, _Multiply_17a3d0b90ebb4b1b9cab04c57f32aed2_Out_2_Vector4, (_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_R_4_Float.xxxx), _Lerp_3bb853efd4d6410a8b63501c276cee08_Out_3_Vector4);
float _Property_1336c01c0e674957b651c4aa288cb981_Out_0_Float = _Emissive_Intenstiy;
float _Property_f2ab9978dd8748fc98c66f03d7d2d5d4_Out_0_Float = _Emissive_Intenstiy_Clouds;
float _Lerp_b7e37fa8b0714a3b953fa8ba9bfe3919_Out_3_Float;
Unity_Lerp_float(_Property_1336c01c0e674957b651c4aa288cb981_Out_0_Float, _Property_f2ab9978dd8748fc98c66f03d7d2d5d4_Out_0_Float, _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_R_4_Float, _Lerp_b7e37fa8b0714a3b953fa8ba9bfe3919_Out_3_Float);
float4 _Multiply_c9eb85122f184daa97bd477002a538c5_Out_2_Vector4;
Unity_Multiply_float4_float4(_Lerp_3bb853efd4d6410a8b63501c276cee08_Out_3_Vector4, (_Lerp_b7e37fa8b0714a3b953fa8ba9bfe3919_Out_3_Float.xxxx), _Multiply_c9eb85122f184daa97bd477002a538c5_Out_2_Vector4);
float _Property_54e4601fd6944f448e3ee0aeac26aefa_Out_0_Float = _Smoothness;
surface.BaseColor = (_Lerp_3bb853efd4d6410a8b63501c276cee08_Out_3_Vector4.xyz);
surface.NormalTS = IN.TangentSpaceNormal;
surface.Emission = (_Multiply_c9eb85122f184daa97bd477002a538c5_Out_2_Vector4.xyz);
surface.Metallic = float(0);
surface.Smoothness = _Property_54e4601fd6944f448e3ee0aeac26aefa_Out_0_Float;
surface.Occlusion = float(1);
surface.Alpha = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_A_7_Float;
surface.AlphaClipThreshold = float(0.5);
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
    output.uv0 =                                        input.uv0;
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


    output.WorldSpacePosition = input.positionWS;

    #if UNITY_UV_STARTS_AT_TOP
    #else
    #endif


    output.uv0 = input.texCoord0;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
    output.TimeParameters = _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
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
    Name "MotionVectors"
    Tags
    {
        "LightMode" = "MotionVectors"
    }

// Render State
Cull Off
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
// GraphKeywords: <None>

// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_TEXCOORD0
#define GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT
#define VARYINGS_NEED_TEXCOORD0
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_MOTION_VECTORS
#define _ALPHATEST_ON 1


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
 float3 WorldSpacePosition;
 float4 uv0;
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

float4 _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D_TexelSize;
float4 _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D_TexelSize;
float4 _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D_TexelSize;
float4 _Color_Tint_Bottom;
float2 _Cloud_Speed;
float _Emissive_Intenstiy_Clouds;
float _Wind_Large_Scale;
float _Terrain_Blend_Mul;
float _Cloud_Scale;
float2 _Wind_Large_Speed;
float _Wind_Small_Multiplier;
float4 _Cloud_Color;
float4 _Color_Tint_Top;
float4 _Base_Color_TexelSize;
float _Grass_Pivot_Lock_Strength;
float _Wind_Large_Multiplier;
float2 _Wind_Small_Speed;
float _Wind_Small_Scale;
float _Terrain_Blend_Pow;
float _Smoothness;
float4 _Emissive_Color;
float4 _Emissive_Clouds;
float _Gradience_Mul;
float _Gradience_Pow;
float _Emissive_Intenstiy;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D);
TEXTURE2D(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D);
TEXTURE2D(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D);
TEXTURE2D(_Base_Color);
SAMPLER(sampler_Base_Color);

// Graph Includes
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

void Unity_Divide_float3(float3 A, float3 B, out float3 Out)
{
    Out = A / B;
}

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}

void Unity_Divide_float2(float2 A, float2 B, out float2 Out)
{
    Out = A / B;
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_Add_float2(float2 A, float2 B, out float2 Out)
{
    Out = A + B;
}

struct Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float
{
float3 WorldSpacePosition;
float3 TimeParameters;
};

void SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(float _Cloud_Scale, float2 _Cloud_Speed, Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float IN, out float2 OutVector2_1)
{
float _Property_da571d91c08d469b8fcdb0a2a1aa2cc1_Out_0_Float = _Cloud_Scale;
float3 _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3;
Unity_Divide_float3(IN.WorldSpacePosition, (_Property_da571d91c08d469b8fcdb0a2a1aa2cc1_Out_0_Float.xxx), _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3);
float _Split_c9bf902bb4344b6a837c002876a4a742_R_1_Float = _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3[0];
float _Split_c9bf902bb4344b6a837c002876a4a742_G_2_Float = _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3[1];
float _Split_c9bf902bb4344b6a837c002876a4a742_B_3_Float = _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3[2];
float _Split_c9bf902bb4344b6a837c002876a4a742_A_4_Float = 0;
float4 _Combine_b2458545efe84f8488335a6a0e9829f0_RGBA_4_Vector4;
float3 _Combine_b2458545efe84f8488335a6a0e9829f0_RGB_5_Vector3;
float2 _Combine_b2458545efe84f8488335a6a0e9829f0_RG_6_Vector2;
Unity_Combine_float(_Split_c9bf902bb4344b6a837c002876a4a742_R_1_Float, _Split_c9bf902bb4344b6a837c002876a4a742_B_3_Float, float(0), float(0), _Combine_b2458545efe84f8488335a6a0e9829f0_RGBA_4_Vector4, _Combine_b2458545efe84f8488335a6a0e9829f0_RGB_5_Vector3, _Combine_b2458545efe84f8488335a6a0e9829f0_RG_6_Vector2);
float2 _Property_36f0a343bbd443b0ba9ba96752bd5419_Out_0_Vector2 = _Cloud_Speed;
float2 _Divide_4d6b2552e0254de6a213dc9020221a26_Out_2_Vector2;
Unity_Divide_float2(_Property_36f0a343bbd443b0ba9ba96752bd5419_Out_0_Vector2, float2(100, 100), _Divide_4d6b2552e0254de6a213dc9020221a26_Out_2_Vector2);
float2 _Multiply_fb765c606df846c29196161ce8a5c5fe_Out_2_Vector2;
Unity_Multiply_float2_float2((IN.TimeParameters.x.xx), _Divide_4d6b2552e0254de6a213dc9020221a26_Out_2_Vector2, _Multiply_fb765c606df846c29196161ce8a5c5fe_Out_2_Vector2);
float2 _Add_4e43f841f4184513872472164ca37f4a_Out_2_Vector2;
Unity_Add_float2(_Combine_b2458545efe84f8488335a6a0e9829f0_RG_6_Vector2, _Multiply_fb765c606df846c29196161ce8a5c5fe_Out_2_Vector2, _Add_4e43f841f4184513872472164ca37f4a_Out_2_Vector2);
OutVector2_1 = _Add_4e43f841f4184513872472164ca37f4a_Out_2_Vector2;
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Power_float(float A, float B, out float Out)
{
    Out = pow(A, B);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
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
float _Property_4116819b6d874ed78027c7f0b237cb76_Out_0_Float = _Wind_Large_Scale;
float2 _Property_7855d6ce09864024b8c68f043f8cbdb4_Out_0_Vector2 = _Wind_Large_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45;
_PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_4116819b6d874ed78027c7f0b237cb76_Out_0_Float, _Property_7855d6ce09864024b8c68f043f8cbdb4_Out_0_Vector2, _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45, _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_R_5_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_G_6_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_B_7_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_A_8_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.a;
float _Subtract_ab6f6b551d064bb8b607c9a9bc381611_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_R_5_Float, float(0.5), _Subtract_ab6f6b551d064bb8b607c9a9bc381611_Out_2_Float);
float4 _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4 = IN.uv0;
float _Split_6ffb9a554e6e4ede9196adfc250975db_R_1_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[0];
float _Split_6ffb9a554e6e4ede9196adfc250975db_G_2_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[1];
float _Split_6ffb9a554e6e4ede9196adfc250975db_B_3_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[2];
float _Split_6ffb9a554e6e4ede9196adfc250975db_A_4_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[3];
float _Property_6eb67bc74e6b4fb288d6164ab2c7cdd8_Out_0_Float = _Grass_Pivot_Lock_Strength;
float _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float;
Unity_Power_float(_Split_6ffb9a554e6e4ede9196adfc250975db_G_2_Float, _Property_6eb67bc74e6b4fb288d6164ab2c7cdd8_Out_0_Float, _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float);
float _Multiply_219e592917cc4b659707ce8e75b39d9e_Out_2_Float;
Unity_Multiply_float_float(_Subtract_ab6f6b551d064bb8b607c9a9bc381611_Out_2_Float, _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float, _Multiply_219e592917cc4b659707ce8e75b39d9e_Out_2_Float);
float _Property_20c86c0cde6c4232a79793b50e170771_Out_0_Float = _Wind_Large_Multiplier;
float _Multiply_91c05a4257804a449cb4304b1f563d42_Out_2_Float;
Unity_Multiply_float_float(_Multiply_219e592917cc4b659707ce8e75b39d9e_Out_2_Float, _Property_20c86c0cde6c4232a79793b50e170771_Out_0_Float, _Multiply_91c05a4257804a449cb4304b1f563d42_Out_2_Float);
float _Property_fe4ba3b7d8f14c98b41d8948b19f735c_Out_0_Float = _Wind_Small_Scale;
float2 _Property_793e0429eb224e7694ee6cb87c6f31b9_Out_0_Vector2 = _Wind_Small_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3;
_PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_fe4ba3b7d8f14c98b41d8948b19f735c_Out_0_Float, _Property_793e0429eb224e7694ee6cb87c6f31b9_Out_0_Vector2, _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3, _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_R_5_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_G_6_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_B_7_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_A_8_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.a;
float _Subtract_95ef509ca9e2441ca6e08508d02ef96e_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_R_5_Float, float(0.5), _Subtract_95ef509ca9e2441ca6e08508d02ef96e_Out_2_Float);
float _Multiply_344852ef2f3546caa8c4f1f3695c3f0d_Out_2_Float;
Unity_Multiply_float_float(_Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float, _Subtract_95ef509ca9e2441ca6e08508d02ef96e_Out_2_Float, _Multiply_344852ef2f3546caa8c4f1f3695c3f0d_Out_2_Float);
float _Property_0943d2961c4c4e2ba15fdd94ac828776_Out_0_Float = _Wind_Small_Multiplier;
float _Multiply_4d6a0142972c48e889dfdbf32703e73e_Out_2_Float;
Unity_Multiply_float_float(_Multiply_344852ef2f3546caa8c4f1f3695c3f0d_Out_2_Float, _Property_0943d2961c4c4e2ba15fdd94ac828776_Out_0_Float, _Multiply_4d6a0142972c48e889dfdbf32703e73e_Out_2_Float);
float3 _Add_b8880c59f8304f648cf8583ead4fb728_Out_2_Vector3;
Unity_Add_float3(IN.WorldSpacePosition, (_Multiply_4d6a0142972c48e889dfdbf32703e73e_Out_2_Float.xxx), _Add_b8880c59f8304f648cf8583ead4fb728_Out_2_Vector3);
float3 _Add_48a8fd066e7e49e4891d1ae97e03bb65_Out_2_Vector3;
Unity_Add_float3((_Multiply_91c05a4257804a449cb4304b1f563d42_Out_2_Float.xxx), _Add_b8880c59f8304f648cf8583ead4fb728_Out_2_Vector3, _Add_48a8fd066e7e49e4891d1ae97e03bb65_Out_2_Vector3);
float3 _UPGFoliageBendWSCustomFunction_dc96fa4b2cc8432d856f03b9268115fe_offsetWS_2_Vector3;
UPG_FoliageBendWS_float(IN.WorldSpacePosition, _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float, _UPGFoliageBendWSCustomFunction_dc96fa4b2cc8432d856f03b9268115fe_offsetWS_2_Vector3);
float3 _Add_c1034f24720f4c5f9634821d7037f0ae_Out_2_Vector3;
Unity_Add_float3(_Add_48a8fd066e7e49e4891d1ae97e03bb65_Out_2_Vector3, _UPGFoliageBendWSCustomFunction_dc96fa4b2cc8432d856f03b9268115fe_offsetWS_2_Vector3, _Add_c1034f24720f4c5f9634821d7037f0ae_Out_2_Vector3);
float3 _Transform_583ec64076274514891c3c1e7f9946aa_Out_1_Vector3;
_Transform_583ec64076274514891c3c1e7f9946aa_Out_1_Vector3 = TransformWorldToObject(_Add_c1034f24720f4c5f9634821d7037f0ae_Out_2_Vector3.xyz);
description.Position = _Transform_583ec64076274514891c3c1e7f9946aa_Out_1_Vector3;
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
UnityTexture2D _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Base_Color, sampler_Base_Color, _Base_Color_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.tex, _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.samplerstate, _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4, _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_R_4_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.r;
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_G_5_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.g;
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_B_6_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.b;
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_A_7_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.a;
surface.Alpha = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_A_7_Float;
surface.AlphaClipThreshold = float(0.5);
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
    output.uv0 =                                        input.uv0;
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
Cull Off
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
// GraphKeywords: <None>

// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#define VARYINGS_NEED_TEXCOORD0
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_DEPTHONLY
#define _ALPHATEST_ON 1


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
 float3 WorldSpacePosition;
 float4 uv0;
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

float4 _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D_TexelSize;
float4 _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D_TexelSize;
float4 _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D_TexelSize;
float4 _Color_Tint_Bottom;
float2 _Cloud_Speed;
float _Emissive_Intenstiy_Clouds;
float _Wind_Large_Scale;
float _Terrain_Blend_Mul;
float _Cloud_Scale;
float2 _Wind_Large_Speed;
float _Wind_Small_Multiplier;
float4 _Cloud_Color;
float4 _Color_Tint_Top;
float4 _Base_Color_TexelSize;
float _Grass_Pivot_Lock_Strength;
float _Wind_Large_Multiplier;
float2 _Wind_Small_Speed;
float _Wind_Small_Scale;
float _Terrain_Blend_Pow;
float _Smoothness;
float4 _Emissive_Color;
float4 _Emissive_Clouds;
float _Gradience_Mul;
float _Gradience_Pow;
float _Emissive_Intenstiy;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D);
TEXTURE2D(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D);
TEXTURE2D(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D);
TEXTURE2D(_Base_Color);
SAMPLER(sampler_Base_Color);

// Graph Includes
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

void Unity_Divide_float3(float3 A, float3 B, out float3 Out)
{
    Out = A / B;
}

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}

void Unity_Divide_float2(float2 A, float2 B, out float2 Out)
{
    Out = A / B;
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_Add_float2(float2 A, float2 B, out float2 Out)
{
    Out = A + B;
}

struct Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float
{
float3 WorldSpacePosition;
float3 TimeParameters;
};

void SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(float _Cloud_Scale, float2 _Cloud_Speed, Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float IN, out float2 OutVector2_1)
{
float _Property_da571d91c08d469b8fcdb0a2a1aa2cc1_Out_0_Float = _Cloud_Scale;
float3 _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3;
Unity_Divide_float3(IN.WorldSpacePosition, (_Property_da571d91c08d469b8fcdb0a2a1aa2cc1_Out_0_Float.xxx), _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3);
float _Split_c9bf902bb4344b6a837c002876a4a742_R_1_Float = _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3[0];
float _Split_c9bf902bb4344b6a837c002876a4a742_G_2_Float = _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3[1];
float _Split_c9bf902bb4344b6a837c002876a4a742_B_3_Float = _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3[2];
float _Split_c9bf902bb4344b6a837c002876a4a742_A_4_Float = 0;
float4 _Combine_b2458545efe84f8488335a6a0e9829f0_RGBA_4_Vector4;
float3 _Combine_b2458545efe84f8488335a6a0e9829f0_RGB_5_Vector3;
float2 _Combine_b2458545efe84f8488335a6a0e9829f0_RG_6_Vector2;
Unity_Combine_float(_Split_c9bf902bb4344b6a837c002876a4a742_R_1_Float, _Split_c9bf902bb4344b6a837c002876a4a742_B_3_Float, float(0), float(0), _Combine_b2458545efe84f8488335a6a0e9829f0_RGBA_4_Vector4, _Combine_b2458545efe84f8488335a6a0e9829f0_RGB_5_Vector3, _Combine_b2458545efe84f8488335a6a0e9829f0_RG_6_Vector2);
float2 _Property_36f0a343bbd443b0ba9ba96752bd5419_Out_0_Vector2 = _Cloud_Speed;
float2 _Divide_4d6b2552e0254de6a213dc9020221a26_Out_2_Vector2;
Unity_Divide_float2(_Property_36f0a343bbd443b0ba9ba96752bd5419_Out_0_Vector2, float2(100, 100), _Divide_4d6b2552e0254de6a213dc9020221a26_Out_2_Vector2);
float2 _Multiply_fb765c606df846c29196161ce8a5c5fe_Out_2_Vector2;
Unity_Multiply_float2_float2((IN.TimeParameters.x.xx), _Divide_4d6b2552e0254de6a213dc9020221a26_Out_2_Vector2, _Multiply_fb765c606df846c29196161ce8a5c5fe_Out_2_Vector2);
float2 _Add_4e43f841f4184513872472164ca37f4a_Out_2_Vector2;
Unity_Add_float2(_Combine_b2458545efe84f8488335a6a0e9829f0_RG_6_Vector2, _Multiply_fb765c606df846c29196161ce8a5c5fe_Out_2_Vector2, _Add_4e43f841f4184513872472164ca37f4a_Out_2_Vector2);
OutVector2_1 = _Add_4e43f841f4184513872472164ca37f4a_Out_2_Vector2;
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Power_float(float A, float B, out float Out)
{
    Out = pow(A, B);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
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
float _Property_4116819b6d874ed78027c7f0b237cb76_Out_0_Float = _Wind_Large_Scale;
float2 _Property_7855d6ce09864024b8c68f043f8cbdb4_Out_0_Vector2 = _Wind_Large_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45;
_PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_4116819b6d874ed78027c7f0b237cb76_Out_0_Float, _Property_7855d6ce09864024b8c68f043f8cbdb4_Out_0_Vector2, _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45, _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_R_5_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_G_6_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_B_7_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_A_8_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.a;
float _Subtract_ab6f6b551d064bb8b607c9a9bc381611_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_R_5_Float, float(0.5), _Subtract_ab6f6b551d064bb8b607c9a9bc381611_Out_2_Float);
float4 _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4 = IN.uv0;
float _Split_6ffb9a554e6e4ede9196adfc250975db_R_1_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[0];
float _Split_6ffb9a554e6e4ede9196adfc250975db_G_2_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[1];
float _Split_6ffb9a554e6e4ede9196adfc250975db_B_3_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[2];
float _Split_6ffb9a554e6e4ede9196adfc250975db_A_4_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[3];
float _Property_6eb67bc74e6b4fb288d6164ab2c7cdd8_Out_0_Float = _Grass_Pivot_Lock_Strength;
float _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float;
Unity_Power_float(_Split_6ffb9a554e6e4ede9196adfc250975db_G_2_Float, _Property_6eb67bc74e6b4fb288d6164ab2c7cdd8_Out_0_Float, _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float);
float _Multiply_219e592917cc4b659707ce8e75b39d9e_Out_2_Float;
Unity_Multiply_float_float(_Subtract_ab6f6b551d064bb8b607c9a9bc381611_Out_2_Float, _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float, _Multiply_219e592917cc4b659707ce8e75b39d9e_Out_2_Float);
float _Property_20c86c0cde6c4232a79793b50e170771_Out_0_Float = _Wind_Large_Multiplier;
float _Multiply_91c05a4257804a449cb4304b1f563d42_Out_2_Float;
Unity_Multiply_float_float(_Multiply_219e592917cc4b659707ce8e75b39d9e_Out_2_Float, _Property_20c86c0cde6c4232a79793b50e170771_Out_0_Float, _Multiply_91c05a4257804a449cb4304b1f563d42_Out_2_Float);
float _Property_fe4ba3b7d8f14c98b41d8948b19f735c_Out_0_Float = _Wind_Small_Scale;
float2 _Property_793e0429eb224e7694ee6cb87c6f31b9_Out_0_Vector2 = _Wind_Small_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3;
_PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_fe4ba3b7d8f14c98b41d8948b19f735c_Out_0_Float, _Property_793e0429eb224e7694ee6cb87c6f31b9_Out_0_Vector2, _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3, _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_R_5_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_G_6_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_B_7_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_A_8_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.a;
float _Subtract_95ef509ca9e2441ca6e08508d02ef96e_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_R_5_Float, float(0.5), _Subtract_95ef509ca9e2441ca6e08508d02ef96e_Out_2_Float);
float _Multiply_344852ef2f3546caa8c4f1f3695c3f0d_Out_2_Float;
Unity_Multiply_float_float(_Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float, _Subtract_95ef509ca9e2441ca6e08508d02ef96e_Out_2_Float, _Multiply_344852ef2f3546caa8c4f1f3695c3f0d_Out_2_Float);
float _Property_0943d2961c4c4e2ba15fdd94ac828776_Out_0_Float = _Wind_Small_Multiplier;
float _Multiply_4d6a0142972c48e889dfdbf32703e73e_Out_2_Float;
Unity_Multiply_float_float(_Multiply_344852ef2f3546caa8c4f1f3695c3f0d_Out_2_Float, _Property_0943d2961c4c4e2ba15fdd94ac828776_Out_0_Float, _Multiply_4d6a0142972c48e889dfdbf32703e73e_Out_2_Float);
float3 _Add_b8880c59f8304f648cf8583ead4fb728_Out_2_Vector3;
Unity_Add_float3(IN.WorldSpacePosition, (_Multiply_4d6a0142972c48e889dfdbf32703e73e_Out_2_Float.xxx), _Add_b8880c59f8304f648cf8583ead4fb728_Out_2_Vector3);
float3 _Add_48a8fd066e7e49e4891d1ae97e03bb65_Out_2_Vector3;
Unity_Add_float3((_Multiply_91c05a4257804a449cb4304b1f563d42_Out_2_Float.xxx), _Add_b8880c59f8304f648cf8583ead4fb728_Out_2_Vector3, _Add_48a8fd066e7e49e4891d1ae97e03bb65_Out_2_Vector3);
float3 _UPGFoliageBendWSCustomFunction_dc96fa4b2cc8432d856f03b9268115fe_offsetWS_2_Vector3;
UPG_FoliageBendWS_float(IN.WorldSpacePosition, _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float, _UPGFoliageBendWSCustomFunction_dc96fa4b2cc8432d856f03b9268115fe_offsetWS_2_Vector3);
float3 _Add_c1034f24720f4c5f9634821d7037f0ae_Out_2_Vector3;
Unity_Add_float3(_Add_48a8fd066e7e49e4891d1ae97e03bb65_Out_2_Vector3, _UPGFoliageBendWSCustomFunction_dc96fa4b2cc8432d856f03b9268115fe_offsetWS_2_Vector3, _Add_c1034f24720f4c5f9634821d7037f0ae_Out_2_Vector3);
float3 _Transform_583ec64076274514891c3c1e7f9946aa_Out_1_Vector3;
_Transform_583ec64076274514891c3c1e7f9946aa_Out_1_Vector3 = TransformWorldToObject(_Add_c1034f24720f4c5f9634821d7037f0ae_Out_2_Vector3.xyz);
float3 _Vector3_c086b8a0e95a4c27b6fc4d4d3843bd3b_Out_0_Vector3 = float3(float(0), float(1), float(0));
description.Position = _Transform_583ec64076274514891c3c1e7f9946aa_Out_1_Vector3;
description.Normal = _Vector3_c086b8a0e95a4c27b6fc4d4d3843bd3b_Out_0_Vector3;
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
UnityTexture2D _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Base_Color, sampler_Base_Color, _Base_Color_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.tex, _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.samplerstate, _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4, _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_R_4_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.r;
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_G_5_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.g;
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_B_6_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.b;
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_A_7_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.a;
surface.Alpha = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_A_7_Float;
surface.AlphaClipThreshold = float(0.5);
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
    output.uv0 =                                        input.uv0;
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
Cull Off
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
// GraphKeywords: <None>

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
#define VARYINGS_NEED_NORMAL_WS
#define VARYINGS_NEED_TANGENT_WS
#define VARYINGS_NEED_TEXCOORD0
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_DEPTHNORMALS
#define _ALPHATEST_ON 1


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
 float4 uv0;
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

float4 _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D_TexelSize;
float4 _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D_TexelSize;
float4 _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D_TexelSize;
float4 _Color_Tint_Bottom;
float2 _Cloud_Speed;
float _Emissive_Intenstiy_Clouds;
float _Wind_Large_Scale;
float _Terrain_Blend_Mul;
float _Cloud_Scale;
float2 _Wind_Large_Speed;
float _Wind_Small_Multiplier;
float4 _Cloud_Color;
float4 _Color_Tint_Top;
float4 _Base_Color_TexelSize;
float _Grass_Pivot_Lock_Strength;
float _Wind_Large_Multiplier;
float2 _Wind_Small_Speed;
float _Wind_Small_Scale;
float _Terrain_Blend_Pow;
float _Smoothness;
float4 _Emissive_Color;
float4 _Emissive_Clouds;
float _Gradience_Mul;
float _Gradience_Pow;
float _Emissive_Intenstiy;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D);
TEXTURE2D(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D);
TEXTURE2D(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D);
TEXTURE2D(_Base_Color);
SAMPLER(sampler_Base_Color);

// Graph Includes
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

void Unity_Divide_float3(float3 A, float3 B, out float3 Out)
{
    Out = A / B;
}

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}

void Unity_Divide_float2(float2 A, float2 B, out float2 Out)
{
    Out = A / B;
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_Add_float2(float2 A, float2 B, out float2 Out)
{
    Out = A + B;
}

struct Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float
{
float3 WorldSpacePosition;
float3 TimeParameters;
};

void SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(float _Cloud_Scale, float2 _Cloud_Speed, Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float IN, out float2 OutVector2_1)
{
float _Property_da571d91c08d469b8fcdb0a2a1aa2cc1_Out_0_Float = _Cloud_Scale;
float3 _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3;
Unity_Divide_float3(IN.WorldSpacePosition, (_Property_da571d91c08d469b8fcdb0a2a1aa2cc1_Out_0_Float.xxx), _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3);
float _Split_c9bf902bb4344b6a837c002876a4a742_R_1_Float = _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3[0];
float _Split_c9bf902bb4344b6a837c002876a4a742_G_2_Float = _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3[1];
float _Split_c9bf902bb4344b6a837c002876a4a742_B_3_Float = _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3[2];
float _Split_c9bf902bb4344b6a837c002876a4a742_A_4_Float = 0;
float4 _Combine_b2458545efe84f8488335a6a0e9829f0_RGBA_4_Vector4;
float3 _Combine_b2458545efe84f8488335a6a0e9829f0_RGB_5_Vector3;
float2 _Combine_b2458545efe84f8488335a6a0e9829f0_RG_6_Vector2;
Unity_Combine_float(_Split_c9bf902bb4344b6a837c002876a4a742_R_1_Float, _Split_c9bf902bb4344b6a837c002876a4a742_B_3_Float, float(0), float(0), _Combine_b2458545efe84f8488335a6a0e9829f0_RGBA_4_Vector4, _Combine_b2458545efe84f8488335a6a0e9829f0_RGB_5_Vector3, _Combine_b2458545efe84f8488335a6a0e9829f0_RG_6_Vector2);
float2 _Property_36f0a343bbd443b0ba9ba96752bd5419_Out_0_Vector2 = _Cloud_Speed;
float2 _Divide_4d6b2552e0254de6a213dc9020221a26_Out_2_Vector2;
Unity_Divide_float2(_Property_36f0a343bbd443b0ba9ba96752bd5419_Out_0_Vector2, float2(100, 100), _Divide_4d6b2552e0254de6a213dc9020221a26_Out_2_Vector2);
float2 _Multiply_fb765c606df846c29196161ce8a5c5fe_Out_2_Vector2;
Unity_Multiply_float2_float2((IN.TimeParameters.x.xx), _Divide_4d6b2552e0254de6a213dc9020221a26_Out_2_Vector2, _Multiply_fb765c606df846c29196161ce8a5c5fe_Out_2_Vector2);
float2 _Add_4e43f841f4184513872472164ca37f4a_Out_2_Vector2;
Unity_Add_float2(_Combine_b2458545efe84f8488335a6a0e9829f0_RG_6_Vector2, _Multiply_fb765c606df846c29196161ce8a5c5fe_Out_2_Vector2, _Add_4e43f841f4184513872472164ca37f4a_Out_2_Vector2);
OutVector2_1 = _Add_4e43f841f4184513872472164ca37f4a_Out_2_Vector2;
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Power_float(float A, float B, out float Out)
{
    Out = pow(A, B);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
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
float _Property_4116819b6d874ed78027c7f0b237cb76_Out_0_Float = _Wind_Large_Scale;
float2 _Property_7855d6ce09864024b8c68f043f8cbdb4_Out_0_Vector2 = _Wind_Large_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45;
_PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_4116819b6d874ed78027c7f0b237cb76_Out_0_Float, _Property_7855d6ce09864024b8c68f043f8cbdb4_Out_0_Vector2, _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45, _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_R_5_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_G_6_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_B_7_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_A_8_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.a;
float _Subtract_ab6f6b551d064bb8b607c9a9bc381611_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_R_5_Float, float(0.5), _Subtract_ab6f6b551d064bb8b607c9a9bc381611_Out_2_Float);
float4 _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4 = IN.uv0;
float _Split_6ffb9a554e6e4ede9196adfc250975db_R_1_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[0];
float _Split_6ffb9a554e6e4ede9196adfc250975db_G_2_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[1];
float _Split_6ffb9a554e6e4ede9196adfc250975db_B_3_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[2];
float _Split_6ffb9a554e6e4ede9196adfc250975db_A_4_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[3];
float _Property_6eb67bc74e6b4fb288d6164ab2c7cdd8_Out_0_Float = _Grass_Pivot_Lock_Strength;
float _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float;
Unity_Power_float(_Split_6ffb9a554e6e4ede9196adfc250975db_G_2_Float, _Property_6eb67bc74e6b4fb288d6164ab2c7cdd8_Out_0_Float, _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float);
float _Multiply_219e592917cc4b659707ce8e75b39d9e_Out_2_Float;
Unity_Multiply_float_float(_Subtract_ab6f6b551d064bb8b607c9a9bc381611_Out_2_Float, _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float, _Multiply_219e592917cc4b659707ce8e75b39d9e_Out_2_Float);
float _Property_20c86c0cde6c4232a79793b50e170771_Out_0_Float = _Wind_Large_Multiplier;
float _Multiply_91c05a4257804a449cb4304b1f563d42_Out_2_Float;
Unity_Multiply_float_float(_Multiply_219e592917cc4b659707ce8e75b39d9e_Out_2_Float, _Property_20c86c0cde6c4232a79793b50e170771_Out_0_Float, _Multiply_91c05a4257804a449cb4304b1f563d42_Out_2_Float);
float _Property_fe4ba3b7d8f14c98b41d8948b19f735c_Out_0_Float = _Wind_Small_Scale;
float2 _Property_793e0429eb224e7694ee6cb87c6f31b9_Out_0_Vector2 = _Wind_Small_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3;
_PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_fe4ba3b7d8f14c98b41d8948b19f735c_Out_0_Float, _Property_793e0429eb224e7694ee6cb87c6f31b9_Out_0_Vector2, _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3, _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_R_5_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_G_6_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_B_7_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_A_8_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.a;
float _Subtract_95ef509ca9e2441ca6e08508d02ef96e_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_R_5_Float, float(0.5), _Subtract_95ef509ca9e2441ca6e08508d02ef96e_Out_2_Float);
float _Multiply_344852ef2f3546caa8c4f1f3695c3f0d_Out_2_Float;
Unity_Multiply_float_float(_Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float, _Subtract_95ef509ca9e2441ca6e08508d02ef96e_Out_2_Float, _Multiply_344852ef2f3546caa8c4f1f3695c3f0d_Out_2_Float);
float _Property_0943d2961c4c4e2ba15fdd94ac828776_Out_0_Float = _Wind_Small_Multiplier;
float _Multiply_4d6a0142972c48e889dfdbf32703e73e_Out_2_Float;
Unity_Multiply_float_float(_Multiply_344852ef2f3546caa8c4f1f3695c3f0d_Out_2_Float, _Property_0943d2961c4c4e2ba15fdd94ac828776_Out_0_Float, _Multiply_4d6a0142972c48e889dfdbf32703e73e_Out_2_Float);
float3 _Add_b8880c59f8304f648cf8583ead4fb728_Out_2_Vector3;
Unity_Add_float3(IN.WorldSpacePosition, (_Multiply_4d6a0142972c48e889dfdbf32703e73e_Out_2_Float.xxx), _Add_b8880c59f8304f648cf8583ead4fb728_Out_2_Vector3);
float3 _Add_48a8fd066e7e49e4891d1ae97e03bb65_Out_2_Vector3;
Unity_Add_float3((_Multiply_91c05a4257804a449cb4304b1f563d42_Out_2_Float.xxx), _Add_b8880c59f8304f648cf8583ead4fb728_Out_2_Vector3, _Add_48a8fd066e7e49e4891d1ae97e03bb65_Out_2_Vector3);
float3 _UPGFoliageBendWSCustomFunction_dc96fa4b2cc8432d856f03b9268115fe_offsetWS_2_Vector3;
UPG_FoliageBendWS_float(IN.WorldSpacePosition, _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float, _UPGFoliageBendWSCustomFunction_dc96fa4b2cc8432d856f03b9268115fe_offsetWS_2_Vector3);
float3 _Add_c1034f24720f4c5f9634821d7037f0ae_Out_2_Vector3;
Unity_Add_float3(_Add_48a8fd066e7e49e4891d1ae97e03bb65_Out_2_Vector3, _UPGFoliageBendWSCustomFunction_dc96fa4b2cc8432d856f03b9268115fe_offsetWS_2_Vector3, _Add_c1034f24720f4c5f9634821d7037f0ae_Out_2_Vector3);
float3 _Transform_583ec64076274514891c3c1e7f9946aa_Out_1_Vector3;
_Transform_583ec64076274514891c3c1e7f9946aa_Out_1_Vector3 = TransformWorldToObject(_Add_c1034f24720f4c5f9634821d7037f0ae_Out_2_Vector3.xyz);
float3 _Vector3_c086b8a0e95a4c27b6fc4d4d3843bd3b_Out_0_Vector3 = float3(float(0), float(1), float(0));
description.Position = _Transform_583ec64076274514891c3c1e7f9946aa_Out_1_Vector3;
description.Normal = _Vector3_c086b8a0e95a4c27b6fc4d4d3843bd3b_Out_0_Vector3;
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
UnityTexture2D _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Base_Color, sampler_Base_Color, _Base_Color_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.tex, _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.samplerstate, _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4, _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_R_4_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.r;
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_G_5_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.g;
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_B_6_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.b;
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_A_7_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.a;
surface.NormalTS = IN.TangentSpaceNormal;
surface.Alpha = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_A_7_Float;
surface.AlphaClipThreshold = float(0.5);
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
    output.uv0 =                                        input.uv0;
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
// GraphKeywords: <None>

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
#define VARYINGS_NEED_TEXCOORD0
#define VARYINGS_NEED_TEXCOORD1
#define VARYINGS_NEED_TEXCOORD2
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_META
#define _FOG_FRAGMENT 1
#define _ALPHATEST_ON 1


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
 float3 WorldSpacePosition;
 float4 uv0;
 float3 TimeParameters;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 WorldSpacePosition;
 float4 uv0;
 float3 TimeParameters;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
 float4 texCoord0 : INTERP0;
 float4 texCoord1 : INTERP1;
 float4 texCoord2 : INTERP2;
 float3 positionWS : INTERP3;
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

float4 _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D_TexelSize;
float4 _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D_TexelSize;
float4 _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D_TexelSize;
float4 _Color_Tint_Bottom;
float2 _Cloud_Speed;
float _Emissive_Intenstiy_Clouds;
float _Wind_Large_Scale;
float _Terrain_Blend_Mul;
float _Cloud_Scale;
float2 _Wind_Large_Speed;
float _Wind_Small_Multiplier;
float4 _Cloud_Color;
float4 _Color_Tint_Top;
float4 _Base_Color_TexelSize;
float _Grass_Pivot_Lock_Strength;
float _Wind_Large_Multiplier;
float2 _Wind_Small_Speed;
float _Wind_Small_Scale;
float _Terrain_Blend_Pow;
float _Smoothness;
float4 _Emissive_Color;
float4 _Emissive_Clouds;
float _Gradience_Mul;
float _Gradience_Pow;
float _Emissive_Intenstiy;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D);
TEXTURE2D(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D);
TEXTURE2D(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D);
TEXTURE2D(_Base_Color);
SAMPLER(sampler_Base_Color);

// Graph Includes
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

void Unity_Divide_float3(float3 A, float3 B, out float3 Out)
{
    Out = A / B;
}

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}

void Unity_Divide_float2(float2 A, float2 B, out float2 Out)
{
    Out = A / B;
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_Add_float2(float2 A, float2 B, out float2 Out)
{
    Out = A + B;
}

struct Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float
{
float3 WorldSpacePosition;
float3 TimeParameters;
};

void SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(float _Cloud_Scale, float2 _Cloud_Speed, Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float IN, out float2 OutVector2_1)
{
float _Property_da571d91c08d469b8fcdb0a2a1aa2cc1_Out_0_Float = _Cloud_Scale;
float3 _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3;
Unity_Divide_float3(IN.WorldSpacePosition, (_Property_da571d91c08d469b8fcdb0a2a1aa2cc1_Out_0_Float.xxx), _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3);
float _Split_c9bf902bb4344b6a837c002876a4a742_R_1_Float = _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3[0];
float _Split_c9bf902bb4344b6a837c002876a4a742_G_2_Float = _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3[1];
float _Split_c9bf902bb4344b6a837c002876a4a742_B_3_Float = _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3[2];
float _Split_c9bf902bb4344b6a837c002876a4a742_A_4_Float = 0;
float4 _Combine_b2458545efe84f8488335a6a0e9829f0_RGBA_4_Vector4;
float3 _Combine_b2458545efe84f8488335a6a0e9829f0_RGB_5_Vector3;
float2 _Combine_b2458545efe84f8488335a6a0e9829f0_RG_6_Vector2;
Unity_Combine_float(_Split_c9bf902bb4344b6a837c002876a4a742_R_1_Float, _Split_c9bf902bb4344b6a837c002876a4a742_B_3_Float, float(0), float(0), _Combine_b2458545efe84f8488335a6a0e9829f0_RGBA_4_Vector4, _Combine_b2458545efe84f8488335a6a0e9829f0_RGB_5_Vector3, _Combine_b2458545efe84f8488335a6a0e9829f0_RG_6_Vector2);
float2 _Property_36f0a343bbd443b0ba9ba96752bd5419_Out_0_Vector2 = _Cloud_Speed;
float2 _Divide_4d6b2552e0254de6a213dc9020221a26_Out_2_Vector2;
Unity_Divide_float2(_Property_36f0a343bbd443b0ba9ba96752bd5419_Out_0_Vector2, float2(100, 100), _Divide_4d6b2552e0254de6a213dc9020221a26_Out_2_Vector2);
float2 _Multiply_fb765c606df846c29196161ce8a5c5fe_Out_2_Vector2;
Unity_Multiply_float2_float2((IN.TimeParameters.x.xx), _Divide_4d6b2552e0254de6a213dc9020221a26_Out_2_Vector2, _Multiply_fb765c606df846c29196161ce8a5c5fe_Out_2_Vector2);
float2 _Add_4e43f841f4184513872472164ca37f4a_Out_2_Vector2;
Unity_Add_float2(_Combine_b2458545efe84f8488335a6a0e9829f0_RG_6_Vector2, _Multiply_fb765c606df846c29196161ce8a5c5fe_Out_2_Vector2, _Add_4e43f841f4184513872472164ca37f4a_Out_2_Vector2);
OutVector2_1 = _Add_4e43f841f4184513872472164ca37f4a_Out_2_Vector2;
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Power_float(float A, float B, out float Out)
{
    Out = pow(A, B);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
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
float _Property_4116819b6d874ed78027c7f0b237cb76_Out_0_Float = _Wind_Large_Scale;
float2 _Property_7855d6ce09864024b8c68f043f8cbdb4_Out_0_Vector2 = _Wind_Large_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45;
_PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_4116819b6d874ed78027c7f0b237cb76_Out_0_Float, _Property_7855d6ce09864024b8c68f043f8cbdb4_Out_0_Vector2, _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45, _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_R_5_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_G_6_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_B_7_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_A_8_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.a;
float _Subtract_ab6f6b551d064bb8b607c9a9bc381611_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_R_5_Float, float(0.5), _Subtract_ab6f6b551d064bb8b607c9a9bc381611_Out_2_Float);
float4 _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4 = IN.uv0;
float _Split_6ffb9a554e6e4ede9196adfc250975db_R_1_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[0];
float _Split_6ffb9a554e6e4ede9196adfc250975db_G_2_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[1];
float _Split_6ffb9a554e6e4ede9196adfc250975db_B_3_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[2];
float _Split_6ffb9a554e6e4ede9196adfc250975db_A_4_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[3];
float _Property_6eb67bc74e6b4fb288d6164ab2c7cdd8_Out_0_Float = _Grass_Pivot_Lock_Strength;
float _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float;
Unity_Power_float(_Split_6ffb9a554e6e4ede9196adfc250975db_G_2_Float, _Property_6eb67bc74e6b4fb288d6164ab2c7cdd8_Out_0_Float, _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float);
float _Multiply_219e592917cc4b659707ce8e75b39d9e_Out_2_Float;
Unity_Multiply_float_float(_Subtract_ab6f6b551d064bb8b607c9a9bc381611_Out_2_Float, _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float, _Multiply_219e592917cc4b659707ce8e75b39d9e_Out_2_Float);
float _Property_20c86c0cde6c4232a79793b50e170771_Out_0_Float = _Wind_Large_Multiplier;
float _Multiply_91c05a4257804a449cb4304b1f563d42_Out_2_Float;
Unity_Multiply_float_float(_Multiply_219e592917cc4b659707ce8e75b39d9e_Out_2_Float, _Property_20c86c0cde6c4232a79793b50e170771_Out_0_Float, _Multiply_91c05a4257804a449cb4304b1f563d42_Out_2_Float);
float _Property_fe4ba3b7d8f14c98b41d8948b19f735c_Out_0_Float = _Wind_Small_Scale;
float2 _Property_793e0429eb224e7694ee6cb87c6f31b9_Out_0_Vector2 = _Wind_Small_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3;
_PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_fe4ba3b7d8f14c98b41d8948b19f735c_Out_0_Float, _Property_793e0429eb224e7694ee6cb87c6f31b9_Out_0_Vector2, _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3, _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_R_5_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_G_6_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_B_7_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_A_8_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.a;
float _Subtract_95ef509ca9e2441ca6e08508d02ef96e_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_R_5_Float, float(0.5), _Subtract_95ef509ca9e2441ca6e08508d02ef96e_Out_2_Float);
float _Multiply_344852ef2f3546caa8c4f1f3695c3f0d_Out_2_Float;
Unity_Multiply_float_float(_Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float, _Subtract_95ef509ca9e2441ca6e08508d02ef96e_Out_2_Float, _Multiply_344852ef2f3546caa8c4f1f3695c3f0d_Out_2_Float);
float _Property_0943d2961c4c4e2ba15fdd94ac828776_Out_0_Float = _Wind_Small_Multiplier;
float _Multiply_4d6a0142972c48e889dfdbf32703e73e_Out_2_Float;
Unity_Multiply_float_float(_Multiply_344852ef2f3546caa8c4f1f3695c3f0d_Out_2_Float, _Property_0943d2961c4c4e2ba15fdd94ac828776_Out_0_Float, _Multiply_4d6a0142972c48e889dfdbf32703e73e_Out_2_Float);
float3 _Add_b8880c59f8304f648cf8583ead4fb728_Out_2_Vector3;
Unity_Add_float3(IN.WorldSpacePosition, (_Multiply_4d6a0142972c48e889dfdbf32703e73e_Out_2_Float.xxx), _Add_b8880c59f8304f648cf8583ead4fb728_Out_2_Vector3);
float3 _Add_48a8fd066e7e49e4891d1ae97e03bb65_Out_2_Vector3;
Unity_Add_float3((_Multiply_91c05a4257804a449cb4304b1f563d42_Out_2_Float.xxx), _Add_b8880c59f8304f648cf8583ead4fb728_Out_2_Vector3, _Add_48a8fd066e7e49e4891d1ae97e03bb65_Out_2_Vector3);
float3 _UPGFoliageBendWSCustomFunction_dc96fa4b2cc8432d856f03b9268115fe_offsetWS_2_Vector3;
UPG_FoliageBendWS_float(IN.WorldSpacePosition, _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float, _UPGFoliageBendWSCustomFunction_dc96fa4b2cc8432d856f03b9268115fe_offsetWS_2_Vector3);
float3 _Add_c1034f24720f4c5f9634821d7037f0ae_Out_2_Vector3;
Unity_Add_float3(_Add_48a8fd066e7e49e4891d1ae97e03bb65_Out_2_Vector3, _UPGFoliageBendWSCustomFunction_dc96fa4b2cc8432d856f03b9268115fe_offsetWS_2_Vector3, _Add_c1034f24720f4c5f9634821d7037f0ae_Out_2_Vector3);
float3 _Transform_583ec64076274514891c3c1e7f9946aa_Out_1_Vector3;
_Transform_583ec64076274514891c3c1e7f9946aa_Out_1_Vector3 = TransformWorldToObject(_Add_c1034f24720f4c5f9634821d7037f0ae_Out_2_Vector3.xyz);
float3 _Vector3_c086b8a0e95a4c27b6fc4d4d3843bd3b_Out_0_Vector3 = float3(float(0), float(1), float(0));
description.Position = _Transform_583ec64076274514891c3c1e7f9946aa_Out_1_Vector3;
description.Normal = _Vector3_c086b8a0e95a4c27b6fc4d4d3843bd3b_Out_0_Vector3;
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
float4 _Property_80081465fc9e42c78d044b56c43e5e1f_Out_0_Vector4 = _Color_Tint_Bottom;
float4 _Property_39c423b8c80d4d97946dc0b2c79b23c9_Out_0_Vector4 = _Color_Tint_Top;
float4 _UV_0a040733d8e9420d9054f7edb74c7682_Out_0_Vector4 = IN.uv0;
float _Split_e789d7c2ff9f4972bdb3462091aa8bfb_R_1_Float = _UV_0a040733d8e9420d9054f7edb74c7682_Out_0_Vector4[0];
float _Split_e789d7c2ff9f4972bdb3462091aa8bfb_G_2_Float = _UV_0a040733d8e9420d9054f7edb74c7682_Out_0_Vector4[1];
float _Split_e789d7c2ff9f4972bdb3462091aa8bfb_B_3_Float = _UV_0a040733d8e9420d9054f7edb74c7682_Out_0_Vector4[2];
float _Split_e789d7c2ff9f4972bdb3462091aa8bfb_A_4_Float = _UV_0a040733d8e9420d9054f7edb74c7682_Out_0_Vector4[3];
float _Property_5bf30ba4b92c4b7d81755e8764fdd331_Out_0_Float = _Gradience_Mul;
float _Multiply_0e8bd3c9e0bb453f8ddc49e301bbbe74_Out_2_Float;
Unity_Multiply_float_float(_Split_e789d7c2ff9f4972bdb3462091aa8bfb_G_2_Float, _Property_5bf30ba4b92c4b7d81755e8764fdd331_Out_0_Float, _Multiply_0e8bd3c9e0bb453f8ddc49e301bbbe74_Out_2_Float);
float _Property_92651d8cf9f8486f97f96477fee69822_Out_0_Float = _Gradience_Pow;
float _Power_21a1ac3e3c704dc2a230b301a4fd1bc3_Out_2_Float;
Unity_Power_float(_Multiply_0e8bd3c9e0bb453f8ddc49e301bbbe74_Out_2_Float, _Property_92651d8cf9f8486f97f96477fee69822_Out_0_Float, _Power_21a1ac3e3c704dc2a230b301a4fd1bc3_Out_2_Float);
float _Saturate_df6319cd6a8f44b8a2f98a45b86570e3_Out_1_Float;
Unity_Saturate_float(_Power_21a1ac3e3c704dc2a230b301a4fd1bc3_Out_2_Float, _Saturate_df6319cd6a8f44b8a2f98a45b86570e3_Out_1_Float);
float4 _Lerp_aef25ae41dbd4bb899f648885331bf52_Out_3_Vector4;
Unity_Lerp_float4(_Property_80081465fc9e42c78d044b56c43e5e1f_Out_0_Vector4, _Property_39c423b8c80d4d97946dc0b2c79b23c9_Out_0_Vector4, (_Saturate_df6319cd6a8f44b8a2f98a45b86570e3_Out_1_Float.xxxx), _Lerp_aef25ae41dbd4bb899f648885331bf52_Out_3_Vector4);
UnityTexture2D _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Base_Color, sampler_Base_Color, _Base_Color_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.tex, _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.samplerstate, _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4, _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_R_4_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.r;
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_G_5_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.g;
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_B_6_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.b;
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_A_7_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.a;
float4 _Multiply_fcd43956ee594716aa91c7691ce699cf_Out_2_Vector4;
Unity_Multiply_float4_float4(_Lerp_aef25ae41dbd4bb899f648885331bf52_Out_3_Vector4, _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4, _Multiply_fcd43956ee594716aa91c7691ce699cf_Out_2_Vector4);
float4 _Property_1d0cacb8faa34e6580302336f32b8591_Out_0_Vector4 = _Cloud_Color;
float4 _Multiply_17a3d0b90ebb4b1b9cab04c57f32aed2_Out_2_Vector4;
Unity_Multiply_float4_float4(_Multiply_fcd43956ee594716aa91c7691ce699cf_Out_2_Vector4, _Property_1d0cacb8faa34e6580302336f32b8591_Out_0_Vector4, _Multiply_17a3d0b90ebb4b1b9cab04c57f32aed2_Out_2_Vector4);
float _Property_8a8a73164ab1462fbb02778830bd9a39_Out_0_Float = _Cloud_Scale;
float2 _Property_b7fc6981bb03499fb69f338ec3fe9525_Out_0_Vector2 = _Cloud_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_dc86dca30b514354a0931aeb1998f16e;
_PositionalUVs_dc86dca30b514354a0931aeb1998f16e.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_dc86dca30b514354a0931aeb1998f16e.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_dc86dca30b514354a0931aeb1998f16e_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_8a8a73164ab1462fbb02778830bd9a39_Out_0_Float, _Property_b7fc6981bb03499fb69f338ec3fe9525_Out_0_Vector2, _PositionalUVs_dc86dca30b514354a0931aeb1998f16e, _PositionalUVs_dc86dca30b514354a0931aeb1998f16e_OutVector2_1_Vector2);
float4 _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(UnityBuildTexture2DStructNoScale(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_dc86dca30b514354a0931aeb1998f16e_OutVector2_1_Vector2) );
if (UnityBuildTexture2DStructNoScale(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D).hdrDecode.x > 0)
_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4, UnityBuildTexture2DStructNoScale(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D).hdrDecode);
float _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_R_4_Float = _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4.r;
float _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_G_5_Float = _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4.g;
float _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_B_6_Float = _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4.b;
float _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_A_7_Float = _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4.a;
float4 _Lerp_3bb853efd4d6410a8b63501c276cee08_Out_3_Vector4;
Unity_Lerp_float4(_Multiply_fcd43956ee594716aa91c7691ce699cf_Out_2_Vector4, _Multiply_17a3d0b90ebb4b1b9cab04c57f32aed2_Out_2_Vector4, (_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_R_4_Float.xxxx), _Lerp_3bb853efd4d6410a8b63501c276cee08_Out_3_Vector4);
float _Property_1336c01c0e674957b651c4aa288cb981_Out_0_Float = _Emissive_Intenstiy;
float _Property_f2ab9978dd8748fc98c66f03d7d2d5d4_Out_0_Float = _Emissive_Intenstiy_Clouds;
float _Lerp_b7e37fa8b0714a3b953fa8ba9bfe3919_Out_3_Float;
Unity_Lerp_float(_Property_1336c01c0e674957b651c4aa288cb981_Out_0_Float, _Property_f2ab9978dd8748fc98c66f03d7d2d5d4_Out_0_Float, _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_R_4_Float, _Lerp_b7e37fa8b0714a3b953fa8ba9bfe3919_Out_3_Float);
float4 _Multiply_c9eb85122f184daa97bd477002a538c5_Out_2_Vector4;
Unity_Multiply_float4_float4(_Lerp_3bb853efd4d6410a8b63501c276cee08_Out_3_Vector4, (_Lerp_b7e37fa8b0714a3b953fa8ba9bfe3919_Out_3_Float.xxxx), _Multiply_c9eb85122f184daa97bd477002a538c5_Out_2_Vector4);
surface.BaseColor = (_Lerp_3bb853efd4d6410a8b63501c276cee08_Out_3_Vector4.xyz);
surface.Emission = (_Multiply_c9eb85122f184daa97bd477002a538c5_Out_2_Vector4.xyz);
surface.Alpha = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_A_7_Float;
surface.AlphaClipThreshold = float(0.5);
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
    output.uv0 =                                        input.uv0;
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

    





    output.WorldSpacePosition = input.positionWS;

    #if UNITY_UV_STARTS_AT_TOP
    #else
    #endif


    output.uv0 = input.texCoord0;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
    output.TimeParameters = _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
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
// GraphKeywords: <None>

// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#define VARYINGS_NEED_TEXCOORD0
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_DEPTHONLY
#define SCENESELECTIONPASS 1
#define ALPHA_CLIP_THRESHOLD 1
#define _ALPHATEST_ON 1


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
 float3 WorldSpacePosition;
 float4 uv0;
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

float4 _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D_TexelSize;
float4 _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D_TexelSize;
float4 _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D_TexelSize;
float4 _Color_Tint_Bottom;
float2 _Cloud_Speed;
float _Emissive_Intenstiy_Clouds;
float _Wind_Large_Scale;
float _Terrain_Blend_Mul;
float _Cloud_Scale;
float2 _Wind_Large_Speed;
float _Wind_Small_Multiplier;
float4 _Cloud_Color;
float4 _Color_Tint_Top;
float4 _Base_Color_TexelSize;
float _Grass_Pivot_Lock_Strength;
float _Wind_Large_Multiplier;
float2 _Wind_Small_Speed;
float _Wind_Small_Scale;
float _Terrain_Blend_Pow;
float _Smoothness;
float4 _Emissive_Color;
float4 _Emissive_Clouds;
float _Gradience_Mul;
float _Gradience_Pow;
float _Emissive_Intenstiy;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D);
TEXTURE2D(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D);
TEXTURE2D(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D);
TEXTURE2D(_Base_Color);
SAMPLER(sampler_Base_Color);

// Graph Includes
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

void Unity_Divide_float3(float3 A, float3 B, out float3 Out)
{
    Out = A / B;
}

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}

void Unity_Divide_float2(float2 A, float2 B, out float2 Out)
{
    Out = A / B;
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_Add_float2(float2 A, float2 B, out float2 Out)
{
    Out = A + B;
}

struct Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float
{
float3 WorldSpacePosition;
float3 TimeParameters;
};

void SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(float _Cloud_Scale, float2 _Cloud_Speed, Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float IN, out float2 OutVector2_1)
{
float _Property_da571d91c08d469b8fcdb0a2a1aa2cc1_Out_0_Float = _Cloud_Scale;
float3 _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3;
Unity_Divide_float3(IN.WorldSpacePosition, (_Property_da571d91c08d469b8fcdb0a2a1aa2cc1_Out_0_Float.xxx), _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3);
float _Split_c9bf902bb4344b6a837c002876a4a742_R_1_Float = _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3[0];
float _Split_c9bf902bb4344b6a837c002876a4a742_G_2_Float = _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3[1];
float _Split_c9bf902bb4344b6a837c002876a4a742_B_3_Float = _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3[2];
float _Split_c9bf902bb4344b6a837c002876a4a742_A_4_Float = 0;
float4 _Combine_b2458545efe84f8488335a6a0e9829f0_RGBA_4_Vector4;
float3 _Combine_b2458545efe84f8488335a6a0e9829f0_RGB_5_Vector3;
float2 _Combine_b2458545efe84f8488335a6a0e9829f0_RG_6_Vector2;
Unity_Combine_float(_Split_c9bf902bb4344b6a837c002876a4a742_R_1_Float, _Split_c9bf902bb4344b6a837c002876a4a742_B_3_Float, float(0), float(0), _Combine_b2458545efe84f8488335a6a0e9829f0_RGBA_4_Vector4, _Combine_b2458545efe84f8488335a6a0e9829f0_RGB_5_Vector3, _Combine_b2458545efe84f8488335a6a0e9829f0_RG_6_Vector2);
float2 _Property_36f0a343bbd443b0ba9ba96752bd5419_Out_0_Vector2 = _Cloud_Speed;
float2 _Divide_4d6b2552e0254de6a213dc9020221a26_Out_2_Vector2;
Unity_Divide_float2(_Property_36f0a343bbd443b0ba9ba96752bd5419_Out_0_Vector2, float2(100, 100), _Divide_4d6b2552e0254de6a213dc9020221a26_Out_2_Vector2);
float2 _Multiply_fb765c606df846c29196161ce8a5c5fe_Out_2_Vector2;
Unity_Multiply_float2_float2((IN.TimeParameters.x.xx), _Divide_4d6b2552e0254de6a213dc9020221a26_Out_2_Vector2, _Multiply_fb765c606df846c29196161ce8a5c5fe_Out_2_Vector2);
float2 _Add_4e43f841f4184513872472164ca37f4a_Out_2_Vector2;
Unity_Add_float2(_Combine_b2458545efe84f8488335a6a0e9829f0_RG_6_Vector2, _Multiply_fb765c606df846c29196161ce8a5c5fe_Out_2_Vector2, _Add_4e43f841f4184513872472164ca37f4a_Out_2_Vector2);
OutVector2_1 = _Add_4e43f841f4184513872472164ca37f4a_Out_2_Vector2;
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Power_float(float A, float B, out float Out)
{
    Out = pow(A, B);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
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
float _Property_4116819b6d874ed78027c7f0b237cb76_Out_0_Float = _Wind_Large_Scale;
float2 _Property_7855d6ce09864024b8c68f043f8cbdb4_Out_0_Vector2 = _Wind_Large_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45;
_PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_4116819b6d874ed78027c7f0b237cb76_Out_0_Float, _Property_7855d6ce09864024b8c68f043f8cbdb4_Out_0_Vector2, _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45, _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_R_5_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_G_6_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_B_7_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_A_8_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.a;
float _Subtract_ab6f6b551d064bb8b607c9a9bc381611_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_R_5_Float, float(0.5), _Subtract_ab6f6b551d064bb8b607c9a9bc381611_Out_2_Float);
float4 _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4 = IN.uv0;
float _Split_6ffb9a554e6e4ede9196adfc250975db_R_1_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[0];
float _Split_6ffb9a554e6e4ede9196adfc250975db_G_2_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[1];
float _Split_6ffb9a554e6e4ede9196adfc250975db_B_3_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[2];
float _Split_6ffb9a554e6e4ede9196adfc250975db_A_4_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[3];
float _Property_6eb67bc74e6b4fb288d6164ab2c7cdd8_Out_0_Float = _Grass_Pivot_Lock_Strength;
float _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float;
Unity_Power_float(_Split_6ffb9a554e6e4ede9196adfc250975db_G_2_Float, _Property_6eb67bc74e6b4fb288d6164ab2c7cdd8_Out_0_Float, _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float);
float _Multiply_219e592917cc4b659707ce8e75b39d9e_Out_2_Float;
Unity_Multiply_float_float(_Subtract_ab6f6b551d064bb8b607c9a9bc381611_Out_2_Float, _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float, _Multiply_219e592917cc4b659707ce8e75b39d9e_Out_2_Float);
float _Property_20c86c0cde6c4232a79793b50e170771_Out_0_Float = _Wind_Large_Multiplier;
float _Multiply_91c05a4257804a449cb4304b1f563d42_Out_2_Float;
Unity_Multiply_float_float(_Multiply_219e592917cc4b659707ce8e75b39d9e_Out_2_Float, _Property_20c86c0cde6c4232a79793b50e170771_Out_0_Float, _Multiply_91c05a4257804a449cb4304b1f563d42_Out_2_Float);
float _Property_fe4ba3b7d8f14c98b41d8948b19f735c_Out_0_Float = _Wind_Small_Scale;
float2 _Property_793e0429eb224e7694ee6cb87c6f31b9_Out_0_Vector2 = _Wind_Small_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3;
_PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_fe4ba3b7d8f14c98b41d8948b19f735c_Out_0_Float, _Property_793e0429eb224e7694ee6cb87c6f31b9_Out_0_Vector2, _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3, _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_R_5_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_G_6_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_B_7_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_A_8_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.a;
float _Subtract_95ef509ca9e2441ca6e08508d02ef96e_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_R_5_Float, float(0.5), _Subtract_95ef509ca9e2441ca6e08508d02ef96e_Out_2_Float);
float _Multiply_344852ef2f3546caa8c4f1f3695c3f0d_Out_2_Float;
Unity_Multiply_float_float(_Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float, _Subtract_95ef509ca9e2441ca6e08508d02ef96e_Out_2_Float, _Multiply_344852ef2f3546caa8c4f1f3695c3f0d_Out_2_Float);
float _Property_0943d2961c4c4e2ba15fdd94ac828776_Out_0_Float = _Wind_Small_Multiplier;
float _Multiply_4d6a0142972c48e889dfdbf32703e73e_Out_2_Float;
Unity_Multiply_float_float(_Multiply_344852ef2f3546caa8c4f1f3695c3f0d_Out_2_Float, _Property_0943d2961c4c4e2ba15fdd94ac828776_Out_0_Float, _Multiply_4d6a0142972c48e889dfdbf32703e73e_Out_2_Float);
float3 _Add_b8880c59f8304f648cf8583ead4fb728_Out_2_Vector3;
Unity_Add_float3(IN.WorldSpacePosition, (_Multiply_4d6a0142972c48e889dfdbf32703e73e_Out_2_Float.xxx), _Add_b8880c59f8304f648cf8583ead4fb728_Out_2_Vector3);
float3 _Add_48a8fd066e7e49e4891d1ae97e03bb65_Out_2_Vector3;
Unity_Add_float3((_Multiply_91c05a4257804a449cb4304b1f563d42_Out_2_Float.xxx), _Add_b8880c59f8304f648cf8583ead4fb728_Out_2_Vector3, _Add_48a8fd066e7e49e4891d1ae97e03bb65_Out_2_Vector3);
float3 _UPGFoliageBendWSCustomFunction_dc96fa4b2cc8432d856f03b9268115fe_offsetWS_2_Vector3;
UPG_FoliageBendWS_float(IN.WorldSpacePosition, _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float, _UPGFoliageBendWSCustomFunction_dc96fa4b2cc8432d856f03b9268115fe_offsetWS_2_Vector3);
float3 _Add_c1034f24720f4c5f9634821d7037f0ae_Out_2_Vector3;
Unity_Add_float3(_Add_48a8fd066e7e49e4891d1ae97e03bb65_Out_2_Vector3, _UPGFoliageBendWSCustomFunction_dc96fa4b2cc8432d856f03b9268115fe_offsetWS_2_Vector3, _Add_c1034f24720f4c5f9634821d7037f0ae_Out_2_Vector3);
float3 _Transform_583ec64076274514891c3c1e7f9946aa_Out_1_Vector3;
_Transform_583ec64076274514891c3c1e7f9946aa_Out_1_Vector3 = TransformWorldToObject(_Add_c1034f24720f4c5f9634821d7037f0ae_Out_2_Vector3.xyz);
float3 _Vector3_c086b8a0e95a4c27b6fc4d4d3843bd3b_Out_0_Vector3 = float3(float(0), float(1), float(0));
description.Position = _Transform_583ec64076274514891c3c1e7f9946aa_Out_1_Vector3;
description.Normal = _Vector3_c086b8a0e95a4c27b6fc4d4d3843bd3b_Out_0_Vector3;
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
UnityTexture2D _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Base_Color, sampler_Base_Color, _Base_Color_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.tex, _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.samplerstate, _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4, _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_R_4_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.r;
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_G_5_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.g;
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_B_6_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.b;
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_A_7_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.a;
surface.Alpha = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_A_7_Float;
surface.AlphaClipThreshold = float(0.5);
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
    output.uv0 =                                        input.uv0;
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
// GraphKeywords: <None>

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
#define VARYINGS_NEED_TEXCOORD0
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_DEPTHONLY
#define SCENEPICKINGPASS 1
#define ALPHA_CLIP_THRESHOLD 1
#define _ALPHATEST_ON 1


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
 float3 WorldSpacePosition;
 float4 uv0;
 float3 TimeParameters;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 WorldSpacePosition;
 float4 uv0;
 float3 TimeParameters;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
 float4 texCoord0 : INTERP0;
 float3 positionWS : INTERP1;
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

float4 _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D_TexelSize;
float4 _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D_TexelSize;
float4 _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D_TexelSize;
float4 _Color_Tint_Bottom;
float2 _Cloud_Speed;
float _Emissive_Intenstiy_Clouds;
float _Wind_Large_Scale;
float _Terrain_Blend_Mul;
float _Cloud_Scale;
float2 _Wind_Large_Speed;
float _Wind_Small_Multiplier;
float4 _Cloud_Color;
float4 _Color_Tint_Top;
float4 _Base_Color_TexelSize;
float _Grass_Pivot_Lock_Strength;
float _Wind_Large_Multiplier;
float2 _Wind_Small_Speed;
float _Wind_Small_Scale;
float _Terrain_Blend_Pow;
float _Smoothness;
float4 _Emissive_Color;
float4 _Emissive_Clouds;
float _Gradience_Mul;
float _Gradience_Pow;
float _Emissive_Intenstiy;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D);
TEXTURE2D(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D);
TEXTURE2D(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D);
TEXTURE2D(_Base_Color);
SAMPLER(sampler_Base_Color);

// Graph Includes
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

void Unity_Divide_float3(float3 A, float3 B, out float3 Out)
{
    Out = A / B;
}

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}

void Unity_Divide_float2(float2 A, float2 B, out float2 Out)
{
    Out = A / B;
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_Add_float2(float2 A, float2 B, out float2 Out)
{
    Out = A + B;
}

struct Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float
{
float3 WorldSpacePosition;
float3 TimeParameters;
};

void SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(float _Cloud_Scale, float2 _Cloud_Speed, Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float IN, out float2 OutVector2_1)
{
float _Property_da571d91c08d469b8fcdb0a2a1aa2cc1_Out_0_Float = _Cloud_Scale;
float3 _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3;
Unity_Divide_float3(IN.WorldSpacePosition, (_Property_da571d91c08d469b8fcdb0a2a1aa2cc1_Out_0_Float.xxx), _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3);
float _Split_c9bf902bb4344b6a837c002876a4a742_R_1_Float = _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3[0];
float _Split_c9bf902bb4344b6a837c002876a4a742_G_2_Float = _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3[1];
float _Split_c9bf902bb4344b6a837c002876a4a742_B_3_Float = _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3[2];
float _Split_c9bf902bb4344b6a837c002876a4a742_A_4_Float = 0;
float4 _Combine_b2458545efe84f8488335a6a0e9829f0_RGBA_4_Vector4;
float3 _Combine_b2458545efe84f8488335a6a0e9829f0_RGB_5_Vector3;
float2 _Combine_b2458545efe84f8488335a6a0e9829f0_RG_6_Vector2;
Unity_Combine_float(_Split_c9bf902bb4344b6a837c002876a4a742_R_1_Float, _Split_c9bf902bb4344b6a837c002876a4a742_B_3_Float, float(0), float(0), _Combine_b2458545efe84f8488335a6a0e9829f0_RGBA_4_Vector4, _Combine_b2458545efe84f8488335a6a0e9829f0_RGB_5_Vector3, _Combine_b2458545efe84f8488335a6a0e9829f0_RG_6_Vector2);
float2 _Property_36f0a343bbd443b0ba9ba96752bd5419_Out_0_Vector2 = _Cloud_Speed;
float2 _Divide_4d6b2552e0254de6a213dc9020221a26_Out_2_Vector2;
Unity_Divide_float2(_Property_36f0a343bbd443b0ba9ba96752bd5419_Out_0_Vector2, float2(100, 100), _Divide_4d6b2552e0254de6a213dc9020221a26_Out_2_Vector2);
float2 _Multiply_fb765c606df846c29196161ce8a5c5fe_Out_2_Vector2;
Unity_Multiply_float2_float2((IN.TimeParameters.x.xx), _Divide_4d6b2552e0254de6a213dc9020221a26_Out_2_Vector2, _Multiply_fb765c606df846c29196161ce8a5c5fe_Out_2_Vector2);
float2 _Add_4e43f841f4184513872472164ca37f4a_Out_2_Vector2;
Unity_Add_float2(_Combine_b2458545efe84f8488335a6a0e9829f0_RG_6_Vector2, _Multiply_fb765c606df846c29196161ce8a5c5fe_Out_2_Vector2, _Add_4e43f841f4184513872472164ca37f4a_Out_2_Vector2);
OutVector2_1 = _Add_4e43f841f4184513872472164ca37f4a_Out_2_Vector2;
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Power_float(float A, float B, out float Out)
{
    Out = pow(A, B);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
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
float _Property_4116819b6d874ed78027c7f0b237cb76_Out_0_Float = _Wind_Large_Scale;
float2 _Property_7855d6ce09864024b8c68f043f8cbdb4_Out_0_Vector2 = _Wind_Large_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45;
_PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_4116819b6d874ed78027c7f0b237cb76_Out_0_Float, _Property_7855d6ce09864024b8c68f043f8cbdb4_Out_0_Vector2, _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45, _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_R_5_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_G_6_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_B_7_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_A_8_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.a;
float _Subtract_ab6f6b551d064bb8b607c9a9bc381611_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_R_5_Float, float(0.5), _Subtract_ab6f6b551d064bb8b607c9a9bc381611_Out_2_Float);
float4 _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4 = IN.uv0;
float _Split_6ffb9a554e6e4ede9196adfc250975db_R_1_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[0];
float _Split_6ffb9a554e6e4ede9196adfc250975db_G_2_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[1];
float _Split_6ffb9a554e6e4ede9196adfc250975db_B_3_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[2];
float _Split_6ffb9a554e6e4ede9196adfc250975db_A_4_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[3];
float _Property_6eb67bc74e6b4fb288d6164ab2c7cdd8_Out_0_Float = _Grass_Pivot_Lock_Strength;
float _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float;
Unity_Power_float(_Split_6ffb9a554e6e4ede9196adfc250975db_G_2_Float, _Property_6eb67bc74e6b4fb288d6164ab2c7cdd8_Out_0_Float, _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float);
float _Multiply_219e592917cc4b659707ce8e75b39d9e_Out_2_Float;
Unity_Multiply_float_float(_Subtract_ab6f6b551d064bb8b607c9a9bc381611_Out_2_Float, _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float, _Multiply_219e592917cc4b659707ce8e75b39d9e_Out_2_Float);
float _Property_20c86c0cde6c4232a79793b50e170771_Out_0_Float = _Wind_Large_Multiplier;
float _Multiply_91c05a4257804a449cb4304b1f563d42_Out_2_Float;
Unity_Multiply_float_float(_Multiply_219e592917cc4b659707ce8e75b39d9e_Out_2_Float, _Property_20c86c0cde6c4232a79793b50e170771_Out_0_Float, _Multiply_91c05a4257804a449cb4304b1f563d42_Out_2_Float);
float _Property_fe4ba3b7d8f14c98b41d8948b19f735c_Out_0_Float = _Wind_Small_Scale;
float2 _Property_793e0429eb224e7694ee6cb87c6f31b9_Out_0_Vector2 = _Wind_Small_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3;
_PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_fe4ba3b7d8f14c98b41d8948b19f735c_Out_0_Float, _Property_793e0429eb224e7694ee6cb87c6f31b9_Out_0_Vector2, _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3, _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_R_5_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_G_6_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_B_7_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_A_8_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.a;
float _Subtract_95ef509ca9e2441ca6e08508d02ef96e_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_R_5_Float, float(0.5), _Subtract_95ef509ca9e2441ca6e08508d02ef96e_Out_2_Float);
float _Multiply_344852ef2f3546caa8c4f1f3695c3f0d_Out_2_Float;
Unity_Multiply_float_float(_Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float, _Subtract_95ef509ca9e2441ca6e08508d02ef96e_Out_2_Float, _Multiply_344852ef2f3546caa8c4f1f3695c3f0d_Out_2_Float);
float _Property_0943d2961c4c4e2ba15fdd94ac828776_Out_0_Float = _Wind_Small_Multiplier;
float _Multiply_4d6a0142972c48e889dfdbf32703e73e_Out_2_Float;
Unity_Multiply_float_float(_Multiply_344852ef2f3546caa8c4f1f3695c3f0d_Out_2_Float, _Property_0943d2961c4c4e2ba15fdd94ac828776_Out_0_Float, _Multiply_4d6a0142972c48e889dfdbf32703e73e_Out_2_Float);
float3 _Add_b8880c59f8304f648cf8583ead4fb728_Out_2_Vector3;
Unity_Add_float3(IN.WorldSpacePosition, (_Multiply_4d6a0142972c48e889dfdbf32703e73e_Out_2_Float.xxx), _Add_b8880c59f8304f648cf8583ead4fb728_Out_2_Vector3);
float3 _Add_48a8fd066e7e49e4891d1ae97e03bb65_Out_2_Vector3;
Unity_Add_float3((_Multiply_91c05a4257804a449cb4304b1f563d42_Out_2_Float.xxx), _Add_b8880c59f8304f648cf8583ead4fb728_Out_2_Vector3, _Add_48a8fd066e7e49e4891d1ae97e03bb65_Out_2_Vector3);
float3 _UPGFoliageBendWSCustomFunction_dc96fa4b2cc8432d856f03b9268115fe_offsetWS_2_Vector3;
UPG_FoliageBendWS_float(IN.WorldSpacePosition, _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float, _UPGFoliageBendWSCustomFunction_dc96fa4b2cc8432d856f03b9268115fe_offsetWS_2_Vector3);
float3 _Add_c1034f24720f4c5f9634821d7037f0ae_Out_2_Vector3;
Unity_Add_float3(_Add_48a8fd066e7e49e4891d1ae97e03bb65_Out_2_Vector3, _UPGFoliageBendWSCustomFunction_dc96fa4b2cc8432d856f03b9268115fe_offsetWS_2_Vector3, _Add_c1034f24720f4c5f9634821d7037f0ae_Out_2_Vector3);
float3 _Transform_583ec64076274514891c3c1e7f9946aa_Out_1_Vector3;
_Transform_583ec64076274514891c3c1e7f9946aa_Out_1_Vector3 = TransformWorldToObject(_Add_c1034f24720f4c5f9634821d7037f0ae_Out_2_Vector3.xyz);
float3 _Vector3_c086b8a0e95a4c27b6fc4d4d3843bd3b_Out_0_Vector3 = float3(float(0), float(1), float(0));
description.Position = _Transform_583ec64076274514891c3c1e7f9946aa_Out_1_Vector3;
description.Normal = _Vector3_c086b8a0e95a4c27b6fc4d4d3843bd3b_Out_0_Vector3;
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
float4 _Property_80081465fc9e42c78d044b56c43e5e1f_Out_0_Vector4 = _Color_Tint_Bottom;
float4 _Property_39c423b8c80d4d97946dc0b2c79b23c9_Out_0_Vector4 = _Color_Tint_Top;
float4 _UV_0a040733d8e9420d9054f7edb74c7682_Out_0_Vector4 = IN.uv0;
float _Split_e789d7c2ff9f4972bdb3462091aa8bfb_R_1_Float = _UV_0a040733d8e9420d9054f7edb74c7682_Out_0_Vector4[0];
float _Split_e789d7c2ff9f4972bdb3462091aa8bfb_G_2_Float = _UV_0a040733d8e9420d9054f7edb74c7682_Out_0_Vector4[1];
float _Split_e789d7c2ff9f4972bdb3462091aa8bfb_B_3_Float = _UV_0a040733d8e9420d9054f7edb74c7682_Out_0_Vector4[2];
float _Split_e789d7c2ff9f4972bdb3462091aa8bfb_A_4_Float = _UV_0a040733d8e9420d9054f7edb74c7682_Out_0_Vector4[3];
float _Property_5bf30ba4b92c4b7d81755e8764fdd331_Out_0_Float = _Gradience_Mul;
float _Multiply_0e8bd3c9e0bb453f8ddc49e301bbbe74_Out_2_Float;
Unity_Multiply_float_float(_Split_e789d7c2ff9f4972bdb3462091aa8bfb_G_2_Float, _Property_5bf30ba4b92c4b7d81755e8764fdd331_Out_0_Float, _Multiply_0e8bd3c9e0bb453f8ddc49e301bbbe74_Out_2_Float);
float _Property_92651d8cf9f8486f97f96477fee69822_Out_0_Float = _Gradience_Pow;
float _Power_21a1ac3e3c704dc2a230b301a4fd1bc3_Out_2_Float;
Unity_Power_float(_Multiply_0e8bd3c9e0bb453f8ddc49e301bbbe74_Out_2_Float, _Property_92651d8cf9f8486f97f96477fee69822_Out_0_Float, _Power_21a1ac3e3c704dc2a230b301a4fd1bc3_Out_2_Float);
float _Saturate_df6319cd6a8f44b8a2f98a45b86570e3_Out_1_Float;
Unity_Saturate_float(_Power_21a1ac3e3c704dc2a230b301a4fd1bc3_Out_2_Float, _Saturate_df6319cd6a8f44b8a2f98a45b86570e3_Out_1_Float);
float4 _Lerp_aef25ae41dbd4bb899f648885331bf52_Out_3_Vector4;
Unity_Lerp_float4(_Property_80081465fc9e42c78d044b56c43e5e1f_Out_0_Vector4, _Property_39c423b8c80d4d97946dc0b2c79b23c9_Out_0_Vector4, (_Saturate_df6319cd6a8f44b8a2f98a45b86570e3_Out_1_Float.xxxx), _Lerp_aef25ae41dbd4bb899f648885331bf52_Out_3_Vector4);
UnityTexture2D _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Base_Color, sampler_Base_Color, _Base_Color_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.tex, _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.samplerstate, _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4, _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_R_4_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.r;
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_G_5_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.g;
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_B_6_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.b;
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_A_7_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.a;
float4 _Multiply_fcd43956ee594716aa91c7691ce699cf_Out_2_Vector4;
Unity_Multiply_float4_float4(_Lerp_aef25ae41dbd4bb899f648885331bf52_Out_3_Vector4, _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4, _Multiply_fcd43956ee594716aa91c7691ce699cf_Out_2_Vector4);
float4 _Property_1d0cacb8faa34e6580302336f32b8591_Out_0_Vector4 = _Cloud_Color;
float4 _Multiply_17a3d0b90ebb4b1b9cab04c57f32aed2_Out_2_Vector4;
Unity_Multiply_float4_float4(_Multiply_fcd43956ee594716aa91c7691ce699cf_Out_2_Vector4, _Property_1d0cacb8faa34e6580302336f32b8591_Out_0_Vector4, _Multiply_17a3d0b90ebb4b1b9cab04c57f32aed2_Out_2_Vector4);
float _Property_8a8a73164ab1462fbb02778830bd9a39_Out_0_Float = _Cloud_Scale;
float2 _Property_b7fc6981bb03499fb69f338ec3fe9525_Out_0_Vector2 = _Cloud_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_dc86dca30b514354a0931aeb1998f16e;
_PositionalUVs_dc86dca30b514354a0931aeb1998f16e.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_dc86dca30b514354a0931aeb1998f16e.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_dc86dca30b514354a0931aeb1998f16e_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_8a8a73164ab1462fbb02778830bd9a39_Out_0_Float, _Property_b7fc6981bb03499fb69f338ec3fe9525_Out_0_Vector2, _PositionalUVs_dc86dca30b514354a0931aeb1998f16e, _PositionalUVs_dc86dca30b514354a0931aeb1998f16e_OutVector2_1_Vector2);
float4 _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(UnityBuildTexture2DStructNoScale(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_dc86dca30b514354a0931aeb1998f16e_OutVector2_1_Vector2) );
if (UnityBuildTexture2DStructNoScale(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D).hdrDecode.x > 0)
_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4, UnityBuildTexture2DStructNoScale(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D).hdrDecode);
float _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_R_4_Float = _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4.r;
float _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_G_5_Float = _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4.g;
float _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_B_6_Float = _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4.b;
float _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_A_7_Float = _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4.a;
float4 _Lerp_3bb853efd4d6410a8b63501c276cee08_Out_3_Vector4;
Unity_Lerp_float4(_Multiply_fcd43956ee594716aa91c7691ce699cf_Out_2_Vector4, _Multiply_17a3d0b90ebb4b1b9cab04c57f32aed2_Out_2_Vector4, (_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_R_4_Float.xxxx), _Lerp_3bb853efd4d6410a8b63501c276cee08_Out_3_Vector4);
surface.BaseColor = (_Lerp_3bb853efd4d6410a8b63501c276cee08_Out_3_Vector4.xyz);
surface.Alpha = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_A_7_Float;
surface.AlphaClipThreshold = float(0.5);
float3 reactionUnusedColor = 0, reactionUnusedEmission = 0;
ClipVegetationFuel(IN.WorldSpacePosition);
ApplyVegetationReaction(IN.WorldSpacePosition,surface.BaseColor,reactionUnusedEmission);
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
    output.uv0 =                                        input.uv0;
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

    





    output.WorldSpacePosition = input.positionWS;

    #if UNITY_UV_STARTS_AT_TOP
    #else
    #endif


    output.uv0 = input.texCoord0;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
    output.TimeParameters = _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
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
Cull Off
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
// GraphKeywords: <None>

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
#define VARYINGS_NEED_TEXCOORD0
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_2D
#define _ALPHATEST_ON 1


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
 float3 WorldSpacePosition;
 float4 uv0;
 float3 TimeParameters;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 WorldSpacePosition;
 float4 uv0;
 float3 TimeParameters;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
 float4 texCoord0 : INTERP0;
 float3 positionWS : INTERP1;
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

float4 _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D_TexelSize;
float4 _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D_TexelSize;
float4 _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D_TexelSize;
float4 _Color_Tint_Bottom;
float2 _Cloud_Speed;
float _Emissive_Intenstiy_Clouds;
float _Wind_Large_Scale;
float _Terrain_Blend_Mul;
float _Cloud_Scale;
float2 _Wind_Large_Speed;
float _Wind_Small_Multiplier;
float4 _Cloud_Color;
float4 _Color_Tint_Top;
float4 _Base_Color_TexelSize;
float _Grass_Pivot_Lock_Strength;
float _Wind_Large_Multiplier;
float2 _Wind_Small_Speed;
float _Wind_Small_Scale;
float _Terrain_Blend_Pow;
float _Smoothness;
float4 _Emissive_Color;
float4 _Emissive_Clouds;
float _Gradience_Mul;
float _Gradience_Pow;
float _Emissive_Intenstiy;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D);
TEXTURE2D(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D);
TEXTURE2D(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D);
TEXTURE2D(_Base_Color);
SAMPLER(sampler_Base_Color);

// Graph Includes
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

void Unity_Divide_float3(float3 A, float3 B, out float3 Out)
{
    Out = A / B;
}

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}

void Unity_Divide_float2(float2 A, float2 B, out float2 Out)
{
    Out = A / B;
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_Add_float2(float2 A, float2 B, out float2 Out)
{
    Out = A + B;
}

struct Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float
{
float3 WorldSpacePosition;
float3 TimeParameters;
};

void SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(float _Cloud_Scale, float2 _Cloud_Speed, Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float IN, out float2 OutVector2_1)
{
float _Property_da571d91c08d469b8fcdb0a2a1aa2cc1_Out_0_Float = _Cloud_Scale;
float3 _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3;
Unity_Divide_float3(IN.WorldSpacePosition, (_Property_da571d91c08d469b8fcdb0a2a1aa2cc1_Out_0_Float.xxx), _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3);
float _Split_c9bf902bb4344b6a837c002876a4a742_R_1_Float = _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3[0];
float _Split_c9bf902bb4344b6a837c002876a4a742_G_2_Float = _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3[1];
float _Split_c9bf902bb4344b6a837c002876a4a742_B_3_Float = _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3[2];
float _Split_c9bf902bb4344b6a837c002876a4a742_A_4_Float = 0;
float4 _Combine_b2458545efe84f8488335a6a0e9829f0_RGBA_4_Vector4;
float3 _Combine_b2458545efe84f8488335a6a0e9829f0_RGB_5_Vector3;
float2 _Combine_b2458545efe84f8488335a6a0e9829f0_RG_6_Vector2;
Unity_Combine_float(_Split_c9bf902bb4344b6a837c002876a4a742_R_1_Float, _Split_c9bf902bb4344b6a837c002876a4a742_B_3_Float, float(0), float(0), _Combine_b2458545efe84f8488335a6a0e9829f0_RGBA_4_Vector4, _Combine_b2458545efe84f8488335a6a0e9829f0_RGB_5_Vector3, _Combine_b2458545efe84f8488335a6a0e9829f0_RG_6_Vector2);
float2 _Property_36f0a343bbd443b0ba9ba96752bd5419_Out_0_Vector2 = _Cloud_Speed;
float2 _Divide_4d6b2552e0254de6a213dc9020221a26_Out_2_Vector2;
Unity_Divide_float2(_Property_36f0a343bbd443b0ba9ba96752bd5419_Out_0_Vector2, float2(100, 100), _Divide_4d6b2552e0254de6a213dc9020221a26_Out_2_Vector2);
float2 _Multiply_fb765c606df846c29196161ce8a5c5fe_Out_2_Vector2;
Unity_Multiply_float2_float2((IN.TimeParameters.x.xx), _Divide_4d6b2552e0254de6a213dc9020221a26_Out_2_Vector2, _Multiply_fb765c606df846c29196161ce8a5c5fe_Out_2_Vector2);
float2 _Add_4e43f841f4184513872472164ca37f4a_Out_2_Vector2;
Unity_Add_float2(_Combine_b2458545efe84f8488335a6a0e9829f0_RG_6_Vector2, _Multiply_fb765c606df846c29196161ce8a5c5fe_Out_2_Vector2, _Add_4e43f841f4184513872472164ca37f4a_Out_2_Vector2);
OutVector2_1 = _Add_4e43f841f4184513872472164ca37f4a_Out_2_Vector2;
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Power_float(float A, float B, out float Out)
{
    Out = pow(A, B);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
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
float _Property_4116819b6d874ed78027c7f0b237cb76_Out_0_Float = _Wind_Large_Scale;
float2 _Property_7855d6ce09864024b8c68f043f8cbdb4_Out_0_Vector2 = _Wind_Large_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45;
_PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_4116819b6d874ed78027c7f0b237cb76_Out_0_Float, _Property_7855d6ce09864024b8c68f043f8cbdb4_Out_0_Vector2, _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45, _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_R_5_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_G_6_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_B_7_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_A_8_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.a;
float _Subtract_ab6f6b551d064bb8b607c9a9bc381611_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_R_5_Float, float(0.5), _Subtract_ab6f6b551d064bb8b607c9a9bc381611_Out_2_Float);
float4 _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4 = IN.uv0;
float _Split_6ffb9a554e6e4ede9196adfc250975db_R_1_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[0];
float _Split_6ffb9a554e6e4ede9196adfc250975db_G_2_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[1];
float _Split_6ffb9a554e6e4ede9196adfc250975db_B_3_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[2];
float _Split_6ffb9a554e6e4ede9196adfc250975db_A_4_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[3];
float _Property_6eb67bc74e6b4fb288d6164ab2c7cdd8_Out_0_Float = _Grass_Pivot_Lock_Strength;
float _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float;
Unity_Power_float(_Split_6ffb9a554e6e4ede9196adfc250975db_G_2_Float, _Property_6eb67bc74e6b4fb288d6164ab2c7cdd8_Out_0_Float, _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float);
float _Multiply_219e592917cc4b659707ce8e75b39d9e_Out_2_Float;
Unity_Multiply_float_float(_Subtract_ab6f6b551d064bb8b607c9a9bc381611_Out_2_Float, _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float, _Multiply_219e592917cc4b659707ce8e75b39d9e_Out_2_Float);
float _Property_20c86c0cde6c4232a79793b50e170771_Out_0_Float = _Wind_Large_Multiplier;
float _Multiply_91c05a4257804a449cb4304b1f563d42_Out_2_Float;
Unity_Multiply_float_float(_Multiply_219e592917cc4b659707ce8e75b39d9e_Out_2_Float, _Property_20c86c0cde6c4232a79793b50e170771_Out_0_Float, _Multiply_91c05a4257804a449cb4304b1f563d42_Out_2_Float);
float _Property_fe4ba3b7d8f14c98b41d8948b19f735c_Out_0_Float = _Wind_Small_Scale;
float2 _Property_793e0429eb224e7694ee6cb87c6f31b9_Out_0_Vector2 = _Wind_Small_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3;
_PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_fe4ba3b7d8f14c98b41d8948b19f735c_Out_0_Float, _Property_793e0429eb224e7694ee6cb87c6f31b9_Out_0_Vector2, _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3, _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_R_5_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_G_6_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_B_7_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_A_8_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.a;
float _Subtract_95ef509ca9e2441ca6e08508d02ef96e_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_R_5_Float, float(0.5), _Subtract_95ef509ca9e2441ca6e08508d02ef96e_Out_2_Float);
float _Multiply_344852ef2f3546caa8c4f1f3695c3f0d_Out_2_Float;
Unity_Multiply_float_float(_Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float, _Subtract_95ef509ca9e2441ca6e08508d02ef96e_Out_2_Float, _Multiply_344852ef2f3546caa8c4f1f3695c3f0d_Out_2_Float);
float _Property_0943d2961c4c4e2ba15fdd94ac828776_Out_0_Float = _Wind_Small_Multiplier;
float _Multiply_4d6a0142972c48e889dfdbf32703e73e_Out_2_Float;
Unity_Multiply_float_float(_Multiply_344852ef2f3546caa8c4f1f3695c3f0d_Out_2_Float, _Property_0943d2961c4c4e2ba15fdd94ac828776_Out_0_Float, _Multiply_4d6a0142972c48e889dfdbf32703e73e_Out_2_Float);
float3 _Add_b8880c59f8304f648cf8583ead4fb728_Out_2_Vector3;
Unity_Add_float3(IN.WorldSpacePosition, (_Multiply_4d6a0142972c48e889dfdbf32703e73e_Out_2_Float.xxx), _Add_b8880c59f8304f648cf8583ead4fb728_Out_2_Vector3);
float3 _Add_48a8fd066e7e49e4891d1ae97e03bb65_Out_2_Vector3;
Unity_Add_float3((_Multiply_91c05a4257804a449cb4304b1f563d42_Out_2_Float.xxx), _Add_b8880c59f8304f648cf8583ead4fb728_Out_2_Vector3, _Add_48a8fd066e7e49e4891d1ae97e03bb65_Out_2_Vector3);
float3 _UPGFoliageBendWSCustomFunction_dc96fa4b2cc8432d856f03b9268115fe_offsetWS_2_Vector3;
UPG_FoliageBendWS_float(IN.WorldSpacePosition, _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float, _UPGFoliageBendWSCustomFunction_dc96fa4b2cc8432d856f03b9268115fe_offsetWS_2_Vector3);
float3 _Add_c1034f24720f4c5f9634821d7037f0ae_Out_2_Vector3;
Unity_Add_float3(_Add_48a8fd066e7e49e4891d1ae97e03bb65_Out_2_Vector3, _UPGFoliageBendWSCustomFunction_dc96fa4b2cc8432d856f03b9268115fe_offsetWS_2_Vector3, _Add_c1034f24720f4c5f9634821d7037f0ae_Out_2_Vector3);
float3 _Transform_583ec64076274514891c3c1e7f9946aa_Out_1_Vector3;
_Transform_583ec64076274514891c3c1e7f9946aa_Out_1_Vector3 = TransformWorldToObject(_Add_c1034f24720f4c5f9634821d7037f0ae_Out_2_Vector3.xyz);
float3 _Vector3_c086b8a0e95a4c27b6fc4d4d3843bd3b_Out_0_Vector3 = float3(float(0), float(1), float(0));
description.Position = _Transform_583ec64076274514891c3c1e7f9946aa_Out_1_Vector3;
description.Normal = _Vector3_c086b8a0e95a4c27b6fc4d4d3843bd3b_Out_0_Vector3;
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
float4 _Property_80081465fc9e42c78d044b56c43e5e1f_Out_0_Vector4 = _Color_Tint_Bottom;
float4 _Property_39c423b8c80d4d97946dc0b2c79b23c9_Out_0_Vector4 = _Color_Tint_Top;
float4 _UV_0a040733d8e9420d9054f7edb74c7682_Out_0_Vector4 = IN.uv0;
float _Split_e789d7c2ff9f4972bdb3462091aa8bfb_R_1_Float = _UV_0a040733d8e9420d9054f7edb74c7682_Out_0_Vector4[0];
float _Split_e789d7c2ff9f4972bdb3462091aa8bfb_G_2_Float = _UV_0a040733d8e9420d9054f7edb74c7682_Out_0_Vector4[1];
float _Split_e789d7c2ff9f4972bdb3462091aa8bfb_B_3_Float = _UV_0a040733d8e9420d9054f7edb74c7682_Out_0_Vector4[2];
float _Split_e789d7c2ff9f4972bdb3462091aa8bfb_A_4_Float = _UV_0a040733d8e9420d9054f7edb74c7682_Out_0_Vector4[3];
float _Property_5bf30ba4b92c4b7d81755e8764fdd331_Out_0_Float = _Gradience_Mul;
float _Multiply_0e8bd3c9e0bb453f8ddc49e301bbbe74_Out_2_Float;
Unity_Multiply_float_float(_Split_e789d7c2ff9f4972bdb3462091aa8bfb_G_2_Float, _Property_5bf30ba4b92c4b7d81755e8764fdd331_Out_0_Float, _Multiply_0e8bd3c9e0bb453f8ddc49e301bbbe74_Out_2_Float);
float _Property_92651d8cf9f8486f97f96477fee69822_Out_0_Float = _Gradience_Pow;
float _Power_21a1ac3e3c704dc2a230b301a4fd1bc3_Out_2_Float;
Unity_Power_float(_Multiply_0e8bd3c9e0bb453f8ddc49e301bbbe74_Out_2_Float, _Property_92651d8cf9f8486f97f96477fee69822_Out_0_Float, _Power_21a1ac3e3c704dc2a230b301a4fd1bc3_Out_2_Float);
float _Saturate_df6319cd6a8f44b8a2f98a45b86570e3_Out_1_Float;
Unity_Saturate_float(_Power_21a1ac3e3c704dc2a230b301a4fd1bc3_Out_2_Float, _Saturate_df6319cd6a8f44b8a2f98a45b86570e3_Out_1_Float);
float4 _Lerp_aef25ae41dbd4bb899f648885331bf52_Out_3_Vector4;
Unity_Lerp_float4(_Property_80081465fc9e42c78d044b56c43e5e1f_Out_0_Vector4, _Property_39c423b8c80d4d97946dc0b2c79b23c9_Out_0_Vector4, (_Saturate_df6319cd6a8f44b8a2f98a45b86570e3_Out_1_Float.xxxx), _Lerp_aef25ae41dbd4bb899f648885331bf52_Out_3_Vector4);
UnityTexture2D _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Base_Color, sampler_Base_Color, _Base_Color_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.tex, _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.samplerstate, _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4, _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_R_4_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.r;
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_G_5_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.g;
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_B_6_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.b;
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_A_7_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.a;
float4 _Multiply_fcd43956ee594716aa91c7691ce699cf_Out_2_Vector4;
Unity_Multiply_float4_float4(_Lerp_aef25ae41dbd4bb899f648885331bf52_Out_3_Vector4, _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4, _Multiply_fcd43956ee594716aa91c7691ce699cf_Out_2_Vector4);
float4 _Property_1d0cacb8faa34e6580302336f32b8591_Out_0_Vector4 = _Cloud_Color;
float4 _Multiply_17a3d0b90ebb4b1b9cab04c57f32aed2_Out_2_Vector4;
Unity_Multiply_float4_float4(_Multiply_fcd43956ee594716aa91c7691ce699cf_Out_2_Vector4, _Property_1d0cacb8faa34e6580302336f32b8591_Out_0_Vector4, _Multiply_17a3d0b90ebb4b1b9cab04c57f32aed2_Out_2_Vector4);
float _Property_8a8a73164ab1462fbb02778830bd9a39_Out_0_Float = _Cloud_Scale;
float2 _Property_b7fc6981bb03499fb69f338ec3fe9525_Out_0_Vector2 = _Cloud_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_dc86dca30b514354a0931aeb1998f16e;
_PositionalUVs_dc86dca30b514354a0931aeb1998f16e.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_dc86dca30b514354a0931aeb1998f16e.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_dc86dca30b514354a0931aeb1998f16e_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_8a8a73164ab1462fbb02778830bd9a39_Out_0_Float, _Property_b7fc6981bb03499fb69f338ec3fe9525_Out_0_Vector2, _PositionalUVs_dc86dca30b514354a0931aeb1998f16e, _PositionalUVs_dc86dca30b514354a0931aeb1998f16e_OutVector2_1_Vector2);
float4 _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(UnityBuildTexture2DStructNoScale(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_dc86dca30b514354a0931aeb1998f16e_OutVector2_1_Vector2) );
if (UnityBuildTexture2DStructNoScale(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D).hdrDecode.x > 0)
_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4, UnityBuildTexture2DStructNoScale(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D).hdrDecode);
float _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_R_4_Float = _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4.r;
float _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_G_5_Float = _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4.g;
float _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_B_6_Float = _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4.b;
float _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_A_7_Float = _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4.a;
float4 _Lerp_3bb853efd4d6410a8b63501c276cee08_Out_3_Vector4;
Unity_Lerp_float4(_Multiply_fcd43956ee594716aa91c7691ce699cf_Out_2_Vector4, _Multiply_17a3d0b90ebb4b1b9cab04c57f32aed2_Out_2_Vector4, (_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_R_4_Float.xxxx), _Lerp_3bb853efd4d6410a8b63501c276cee08_Out_3_Vector4);
surface.BaseColor = (_Lerp_3bb853efd4d6410a8b63501c276cee08_Out_3_Vector4.xyz);
surface.Alpha = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_A_7_Float;
surface.AlphaClipThreshold = float(0.5);
float3 reactionUnusedColor = 0, reactionUnusedEmission = 0;
ClipVegetationFuel(IN.WorldSpacePosition);
ApplyVegetationReaction(IN.WorldSpacePosition,surface.BaseColor,reactionUnusedEmission);
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
    output.uv0 =                                        input.uv0;
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

    





    output.WorldSpacePosition = input.positionWS;

    #if UNITY_UV_STARTS_AT_TOP
    #else
    #endif


    output.uv0 = input.texCoord0;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
    output.TimeParameters = _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
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
// GraphKeywords: <None>

// Defines
#define _NORMALMAP 1
#define _NORMAL_DROPOFF_OS 1
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
 float3 ObjectSpaceNormal;
 float3 WorldSpaceNormal;
 float3 WorldSpacePosition;
 float4 uv0;
 float3 TimeParameters;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 WorldSpacePosition;
 float4 uv0;
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

float4 _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D_TexelSize;
float4 _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D_TexelSize;
float4 _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D_TexelSize;
float4 _Color_Tint_Bottom;
float2 _Cloud_Speed;
float _Emissive_Intenstiy_Clouds;
float _Wind_Large_Scale;
float _Terrain_Blend_Mul;
float _Cloud_Scale;
float2 _Wind_Large_Speed;
float _Wind_Small_Multiplier;
float4 _Cloud_Color;
float4 _Color_Tint_Top;
float4 _Base_Color_TexelSize;
float _Grass_Pivot_Lock_Strength;
float _Wind_Large_Multiplier;
float2 _Wind_Small_Speed;
float _Wind_Small_Scale;
float _Terrain_Blend_Pow;
float _Smoothness;
float4 _Emissive_Color;
float4 _Emissive_Clouds;
float _Gradience_Mul;
float _Gradience_Pow;
float _Emissive_Intenstiy;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D);
TEXTURE2D(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D);
TEXTURE2D(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D);
TEXTURE2D(_Base_Color);
SAMPLER(sampler_Base_Color);

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
#include_with_pragmas "Assets/06.Shaders/Foliage/UPG_FoliageInteraction.hlsl"

// Graph Functions

void Unity_Divide_float3(float3 A, float3 B, out float3 Out)
{
    Out = A / B;
}

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}

void Unity_Divide_float2(float2 A, float2 B, out float2 Out)
{
    Out = A / B;
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_Add_float2(float2 A, float2 B, out float2 Out)
{
    Out = A + B;
}

struct Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float
{
float3 WorldSpacePosition;
float3 TimeParameters;
};

void SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(float _Cloud_Scale, float2 _Cloud_Speed, Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float IN, out float2 OutVector2_1)
{
float _Property_da571d91c08d469b8fcdb0a2a1aa2cc1_Out_0_Float = _Cloud_Scale;
float3 _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3;
Unity_Divide_float3(IN.WorldSpacePosition, (_Property_da571d91c08d469b8fcdb0a2a1aa2cc1_Out_0_Float.xxx), _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3);
float _Split_c9bf902bb4344b6a837c002876a4a742_R_1_Float = _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3[0];
float _Split_c9bf902bb4344b6a837c002876a4a742_G_2_Float = _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3[1];
float _Split_c9bf902bb4344b6a837c002876a4a742_B_3_Float = _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3[2];
float _Split_c9bf902bb4344b6a837c002876a4a742_A_4_Float = 0;
float4 _Combine_b2458545efe84f8488335a6a0e9829f0_RGBA_4_Vector4;
float3 _Combine_b2458545efe84f8488335a6a0e9829f0_RGB_5_Vector3;
float2 _Combine_b2458545efe84f8488335a6a0e9829f0_RG_6_Vector2;
Unity_Combine_float(_Split_c9bf902bb4344b6a837c002876a4a742_R_1_Float, _Split_c9bf902bb4344b6a837c002876a4a742_B_3_Float, float(0), float(0), _Combine_b2458545efe84f8488335a6a0e9829f0_RGBA_4_Vector4, _Combine_b2458545efe84f8488335a6a0e9829f0_RGB_5_Vector3, _Combine_b2458545efe84f8488335a6a0e9829f0_RG_6_Vector2);
float2 _Property_36f0a343bbd443b0ba9ba96752bd5419_Out_0_Vector2 = _Cloud_Speed;
float2 _Divide_4d6b2552e0254de6a213dc9020221a26_Out_2_Vector2;
Unity_Divide_float2(_Property_36f0a343bbd443b0ba9ba96752bd5419_Out_0_Vector2, float2(100, 100), _Divide_4d6b2552e0254de6a213dc9020221a26_Out_2_Vector2);
float2 _Multiply_fb765c606df846c29196161ce8a5c5fe_Out_2_Vector2;
Unity_Multiply_float2_float2((IN.TimeParameters.x.xx), _Divide_4d6b2552e0254de6a213dc9020221a26_Out_2_Vector2, _Multiply_fb765c606df846c29196161ce8a5c5fe_Out_2_Vector2);
float2 _Add_4e43f841f4184513872472164ca37f4a_Out_2_Vector2;
Unity_Add_float2(_Combine_b2458545efe84f8488335a6a0e9829f0_RG_6_Vector2, _Multiply_fb765c606df846c29196161ce8a5c5fe_Out_2_Vector2, _Add_4e43f841f4184513872472164ca37f4a_Out_2_Vector2);
OutVector2_1 = _Add_4e43f841f4184513872472164ca37f4a_Out_2_Vector2;
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Power_float(float A, float B, out float Out)
{
    Out = pow(A, B);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
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
float _Property_4116819b6d874ed78027c7f0b237cb76_Out_0_Float = _Wind_Large_Scale;
float2 _Property_7855d6ce09864024b8c68f043f8cbdb4_Out_0_Vector2 = _Wind_Large_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45;
_PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_4116819b6d874ed78027c7f0b237cb76_Out_0_Float, _Property_7855d6ce09864024b8c68f043f8cbdb4_Out_0_Vector2, _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45, _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_R_5_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_G_6_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_B_7_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_A_8_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.a;
float _Subtract_ab6f6b551d064bb8b607c9a9bc381611_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_R_5_Float, float(0.5), _Subtract_ab6f6b551d064bb8b607c9a9bc381611_Out_2_Float);
float4 _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4 = IN.uv0;
float _Split_6ffb9a554e6e4ede9196adfc250975db_R_1_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[0];
float _Split_6ffb9a554e6e4ede9196adfc250975db_G_2_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[1];
float _Split_6ffb9a554e6e4ede9196adfc250975db_B_3_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[2];
float _Split_6ffb9a554e6e4ede9196adfc250975db_A_4_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[3];
float _Property_6eb67bc74e6b4fb288d6164ab2c7cdd8_Out_0_Float = _Grass_Pivot_Lock_Strength;
float _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float;
Unity_Power_float(_Split_6ffb9a554e6e4ede9196adfc250975db_G_2_Float, _Property_6eb67bc74e6b4fb288d6164ab2c7cdd8_Out_0_Float, _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float);
float _Multiply_219e592917cc4b659707ce8e75b39d9e_Out_2_Float;
Unity_Multiply_float_float(_Subtract_ab6f6b551d064bb8b607c9a9bc381611_Out_2_Float, _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float, _Multiply_219e592917cc4b659707ce8e75b39d9e_Out_2_Float);
float _Property_20c86c0cde6c4232a79793b50e170771_Out_0_Float = _Wind_Large_Multiplier;
float _Multiply_91c05a4257804a449cb4304b1f563d42_Out_2_Float;
Unity_Multiply_float_float(_Multiply_219e592917cc4b659707ce8e75b39d9e_Out_2_Float, _Property_20c86c0cde6c4232a79793b50e170771_Out_0_Float, _Multiply_91c05a4257804a449cb4304b1f563d42_Out_2_Float);
float _Property_fe4ba3b7d8f14c98b41d8948b19f735c_Out_0_Float = _Wind_Small_Scale;
float2 _Property_793e0429eb224e7694ee6cb87c6f31b9_Out_0_Vector2 = _Wind_Small_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3;
_PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_fe4ba3b7d8f14c98b41d8948b19f735c_Out_0_Float, _Property_793e0429eb224e7694ee6cb87c6f31b9_Out_0_Vector2, _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3, _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_R_5_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_G_6_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_B_7_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_A_8_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.a;
float _Subtract_95ef509ca9e2441ca6e08508d02ef96e_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_R_5_Float, float(0.5), _Subtract_95ef509ca9e2441ca6e08508d02ef96e_Out_2_Float);
float _Multiply_344852ef2f3546caa8c4f1f3695c3f0d_Out_2_Float;
Unity_Multiply_float_float(_Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float, _Subtract_95ef509ca9e2441ca6e08508d02ef96e_Out_2_Float, _Multiply_344852ef2f3546caa8c4f1f3695c3f0d_Out_2_Float);
float _Property_0943d2961c4c4e2ba15fdd94ac828776_Out_0_Float = _Wind_Small_Multiplier;
float _Multiply_4d6a0142972c48e889dfdbf32703e73e_Out_2_Float;
Unity_Multiply_float_float(_Multiply_344852ef2f3546caa8c4f1f3695c3f0d_Out_2_Float, _Property_0943d2961c4c4e2ba15fdd94ac828776_Out_0_Float, _Multiply_4d6a0142972c48e889dfdbf32703e73e_Out_2_Float);
float3 _Add_b8880c59f8304f648cf8583ead4fb728_Out_2_Vector3;
Unity_Add_float3(IN.WorldSpacePosition, (_Multiply_4d6a0142972c48e889dfdbf32703e73e_Out_2_Float.xxx), _Add_b8880c59f8304f648cf8583ead4fb728_Out_2_Vector3);
float3 _Add_48a8fd066e7e49e4891d1ae97e03bb65_Out_2_Vector3;
Unity_Add_float3((_Multiply_91c05a4257804a449cb4304b1f563d42_Out_2_Float.xxx), _Add_b8880c59f8304f648cf8583ead4fb728_Out_2_Vector3, _Add_48a8fd066e7e49e4891d1ae97e03bb65_Out_2_Vector3);
float3 _UPGFoliageBendWSCustomFunction_dc96fa4b2cc8432d856f03b9268115fe_offsetWS_2_Vector3;
UPG_FoliageBendWS_float(IN.WorldSpacePosition, _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float, _UPGFoliageBendWSCustomFunction_dc96fa4b2cc8432d856f03b9268115fe_offsetWS_2_Vector3);
float3 _Add_c1034f24720f4c5f9634821d7037f0ae_Out_2_Vector3;
Unity_Add_float3(_Add_48a8fd066e7e49e4891d1ae97e03bb65_Out_2_Vector3, _UPGFoliageBendWSCustomFunction_dc96fa4b2cc8432d856f03b9268115fe_offsetWS_2_Vector3, _Add_c1034f24720f4c5f9634821d7037f0ae_Out_2_Vector3);
float3 _Transform_583ec64076274514891c3c1e7f9946aa_Out_1_Vector3;
_Transform_583ec64076274514891c3c1e7f9946aa_Out_1_Vector3 = TransformWorldToObject(_Add_c1034f24720f4c5f9634821d7037f0ae_Out_2_Vector3.xyz);
float3 _Vector3_c086b8a0e95a4c27b6fc4d4d3843bd3b_Out_0_Vector3 = float3(float(0), float(1), float(0));
description.Position = _Transform_583ec64076274514891c3c1e7f9946aa_Out_1_Vector3;
description.Normal = _Vector3_c086b8a0e95a4c27b6fc4d4d3843bd3b_Out_0_Vector3;
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
float3 NormalOS;
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
float4 _Property_80081465fc9e42c78d044b56c43e5e1f_Out_0_Vector4 = _Color_Tint_Bottom;
float4 _Property_39c423b8c80d4d97946dc0b2c79b23c9_Out_0_Vector4 = _Color_Tint_Top;
float4 _UV_0a040733d8e9420d9054f7edb74c7682_Out_0_Vector4 = IN.uv0;
float _Split_e789d7c2ff9f4972bdb3462091aa8bfb_R_1_Float = _UV_0a040733d8e9420d9054f7edb74c7682_Out_0_Vector4[0];
float _Split_e789d7c2ff9f4972bdb3462091aa8bfb_G_2_Float = _UV_0a040733d8e9420d9054f7edb74c7682_Out_0_Vector4[1];
float _Split_e789d7c2ff9f4972bdb3462091aa8bfb_B_3_Float = _UV_0a040733d8e9420d9054f7edb74c7682_Out_0_Vector4[2];
float _Split_e789d7c2ff9f4972bdb3462091aa8bfb_A_4_Float = _UV_0a040733d8e9420d9054f7edb74c7682_Out_0_Vector4[3];
float _Property_5bf30ba4b92c4b7d81755e8764fdd331_Out_0_Float = _Gradience_Mul;
float _Multiply_0e8bd3c9e0bb453f8ddc49e301bbbe74_Out_2_Float;
Unity_Multiply_float_float(_Split_e789d7c2ff9f4972bdb3462091aa8bfb_G_2_Float, _Property_5bf30ba4b92c4b7d81755e8764fdd331_Out_0_Float, _Multiply_0e8bd3c9e0bb453f8ddc49e301bbbe74_Out_2_Float);
float _Property_92651d8cf9f8486f97f96477fee69822_Out_0_Float = _Gradience_Pow;
float _Power_21a1ac3e3c704dc2a230b301a4fd1bc3_Out_2_Float;
Unity_Power_float(_Multiply_0e8bd3c9e0bb453f8ddc49e301bbbe74_Out_2_Float, _Property_92651d8cf9f8486f97f96477fee69822_Out_0_Float, _Power_21a1ac3e3c704dc2a230b301a4fd1bc3_Out_2_Float);
float _Saturate_df6319cd6a8f44b8a2f98a45b86570e3_Out_1_Float;
Unity_Saturate_float(_Power_21a1ac3e3c704dc2a230b301a4fd1bc3_Out_2_Float, _Saturate_df6319cd6a8f44b8a2f98a45b86570e3_Out_1_Float);
float4 _Lerp_aef25ae41dbd4bb899f648885331bf52_Out_3_Vector4;
Unity_Lerp_float4(_Property_80081465fc9e42c78d044b56c43e5e1f_Out_0_Vector4, _Property_39c423b8c80d4d97946dc0b2c79b23c9_Out_0_Vector4, (_Saturate_df6319cd6a8f44b8a2f98a45b86570e3_Out_1_Float.xxxx), _Lerp_aef25ae41dbd4bb899f648885331bf52_Out_3_Vector4);
UnityTexture2D _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Base_Color, sampler_Base_Color, _Base_Color_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.tex, _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.samplerstate, _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4, _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_R_4_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.r;
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_G_5_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.g;
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_B_6_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.b;
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_A_7_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.a;
float4 _Multiply_fcd43956ee594716aa91c7691ce699cf_Out_2_Vector4;
Unity_Multiply_float4_float4(_Lerp_aef25ae41dbd4bb899f648885331bf52_Out_3_Vector4, _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4, _Multiply_fcd43956ee594716aa91c7691ce699cf_Out_2_Vector4);
float4 _Property_1d0cacb8faa34e6580302336f32b8591_Out_0_Vector4 = _Cloud_Color;
float4 _Multiply_17a3d0b90ebb4b1b9cab04c57f32aed2_Out_2_Vector4;
Unity_Multiply_float4_float4(_Multiply_fcd43956ee594716aa91c7691ce699cf_Out_2_Vector4, _Property_1d0cacb8faa34e6580302336f32b8591_Out_0_Vector4, _Multiply_17a3d0b90ebb4b1b9cab04c57f32aed2_Out_2_Vector4);
float _Property_8a8a73164ab1462fbb02778830bd9a39_Out_0_Float = _Cloud_Scale;
float2 _Property_b7fc6981bb03499fb69f338ec3fe9525_Out_0_Vector2 = _Cloud_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_dc86dca30b514354a0931aeb1998f16e;
_PositionalUVs_dc86dca30b514354a0931aeb1998f16e.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_dc86dca30b514354a0931aeb1998f16e.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_dc86dca30b514354a0931aeb1998f16e_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_8a8a73164ab1462fbb02778830bd9a39_Out_0_Float, _Property_b7fc6981bb03499fb69f338ec3fe9525_Out_0_Vector2, _PositionalUVs_dc86dca30b514354a0931aeb1998f16e, _PositionalUVs_dc86dca30b514354a0931aeb1998f16e_OutVector2_1_Vector2);
float4 _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(UnityBuildTexture2DStructNoScale(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_dc86dca30b514354a0931aeb1998f16e_OutVector2_1_Vector2) );
if (UnityBuildTexture2DStructNoScale(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D).hdrDecode.x > 0)
_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4, UnityBuildTexture2DStructNoScale(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D).hdrDecode);
float _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_R_4_Float = _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4.r;
float _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_G_5_Float = _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4.g;
float _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_B_6_Float = _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4.b;
float _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_A_7_Float = _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4.a;
float4 _Lerp_3bb853efd4d6410a8b63501c276cee08_Out_3_Vector4;
Unity_Lerp_float4(_Multiply_fcd43956ee594716aa91c7691ce699cf_Out_2_Vector4, _Multiply_17a3d0b90ebb4b1b9cab04c57f32aed2_Out_2_Vector4, (_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_R_4_Float.xxxx), _Lerp_3bb853efd4d6410a8b63501c276cee08_Out_3_Vector4);
float3 _Vector3_bf89382b9b774865b176e5e6ac1c1e0e_Out_0_Vector3 = float3(float(0), float(1), float(0));
float _Property_1336c01c0e674957b651c4aa288cb981_Out_0_Float = _Emissive_Intenstiy;
float _Property_f2ab9978dd8748fc98c66f03d7d2d5d4_Out_0_Float = _Emissive_Intenstiy_Clouds;
float _Lerp_b7e37fa8b0714a3b953fa8ba9bfe3919_Out_3_Float;
Unity_Lerp_float(_Property_1336c01c0e674957b651c4aa288cb981_Out_0_Float, _Property_f2ab9978dd8748fc98c66f03d7d2d5d4_Out_0_Float, _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_R_4_Float, _Lerp_b7e37fa8b0714a3b953fa8ba9bfe3919_Out_3_Float);
float4 _Multiply_c9eb85122f184daa97bd477002a538c5_Out_2_Vector4;
Unity_Multiply_float4_float4(_Lerp_3bb853efd4d6410a8b63501c276cee08_Out_3_Vector4, (_Lerp_b7e37fa8b0714a3b953fa8ba9bfe3919_Out_3_Float.xxxx), _Multiply_c9eb85122f184daa97bd477002a538c5_Out_2_Vector4);
float _Property_54e4601fd6944f448e3ee0aeac26aefa_Out_0_Float = _Smoothness;
surface.BaseColor = (_Lerp_3bb853efd4d6410a8b63501c276cee08_Out_3_Vector4.xyz);
surface.NormalOS = _Vector3_bf89382b9b774865b176e5e6ac1c1e0e_Out_0_Vector3;
surface.Emission = (_Multiply_c9eb85122f184daa97bd477002a538c5_Out_2_Vector4.xyz);
surface.Metallic = float(0);
surface.Smoothness = _Property_54e4601fd6944f448e3ee0aeac26aefa_Out_0_Float;
surface.Occlusion = float(1);
surface.Alpha = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_A_7_Float;
surface.AlphaClipThreshold = float(0.5);
float3 reactionUnusedColor = 0, reactionUnusedEmission = 0;
ClipVegetationFuel(IN.WorldSpacePosition);
ApplyVegetationReaction(IN.WorldSpacePosition,surface.BaseColor,surface.Emission);
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
    output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
    output.uv0 =                                        input.uv0;
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
    output.ObjectSpaceNormal = normalize(mul(output.WorldSpaceNormal, (float3x3) UNITY_MATRIX_M));           // transposed multiplication by inverse matrix to handle normal scale


    output.WorldSpacePosition = input.positionWS;

    #if UNITY_UV_STARTS_AT_TOP
    #else
    #endif


    output.uv0 = input.texCoord0;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
    output.TimeParameters = _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
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
#pragma shader_feature_local_fragment _ _BUILTIN_SURFACE_TYPE_TRANSPARENT
#pragma shader_feature_local_fragment _ _BUILTIN_AlphaClip
#pragma shader_feature_local_fragment _ _BUILTIN_ALPHATEST_ON
// GraphKeywords: <None>

// Defines
#define _NORMALMAP 1
#define _NORMAL_DROPOFF_OS 1
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
 float3 ObjectSpaceNormal;
 float3 WorldSpaceNormal;
 float3 WorldSpacePosition;
 float4 uv0;
 float3 TimeParameters;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 WorldSpacePosition;
 float4 uv0;
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

float4 _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D_TexelSize;
float4 _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D_TexelSize;
float4 _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D_TexelSize;
float4 _Color_Tint_Bottom;
float2 _Cloud_Speed;
float _Emissive_Intenstiy_Clouds;
float _Wind_Large_Scale;
float _Terrain_Blend_Mul;
float _Cloud_Scale;
float2 _Wind_Large_Speed;
float _Wind_Small_Multiplier;
float4 _Cloud_Color;
float4 _Color_Tint_Top;
float4 _Base_Color_TexelSize;
float _Grass_Pivot_Lock_Strength;
float _Wind_Large_Multiplier;
float2 _Wind_Small_Speed;
float _Wind_Small_Scale;
float _Terrain_Blend_Pow;
float _Smoothness;
float4 _Emissive_Color;
float4 _Emissive_Clouds;
float _Gradience_Mul;
float _Gradience_Pow;
float _Emissive_Intenstiy;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D);
TEXTURE2D(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D);
TEXTURE2D(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D);
TEXTURE2D(_Base_Color);
SAMPLER(sampler_Base_Color);

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
#include_with_pragmas "Assets/06.Shaders/Foliage/UPG_FoliageInteraction.hlsl"

// Graph Functions

void Unity_Divide_float3(float3 A, float3 B, out float3 Out)
{
    Out = A / B;
}

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}

void Unity_Divide_float2(float2 A, float2 B, out float2 Out)
{
    Out = A / B;
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_Add_float2(float2 A, float2 B, out float2 Out)
{
    Out = A + B;
}

struct Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float
{
float3 WorldSpacePosition;
float3 TimeParameters;
};

void SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(float _Cloud_Scale, float2 _Cloud_Speed, Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float IN, out float2 OutVector2_1)
{
float _Property_da571d91c08d469b8fcdb0a2a1aa2cc1_Out_0_Float = _Cloud_Scale;
float3 _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3;
Unity_Divide_float3(IN.WorldSpacePosition, (_Property_da571d91c08d469b8fcdb0a2a1aa2cc1_Out_0_Float.xxx), _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3);
float _Split_c9bf902bb4344b6a837c002876a4a742_R_1_Float = _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3[0];
float _Split_c9bf902bb4344b6a837c002876a4a742_G_2_Float = _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3[1];
float _Split_c9bf902bb4344b6a837c002876a4a742_B_3_Float = _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3[2];
float _Split_c9bf902bb4344b6a837c002876a4a742_A_4_Float = 0;
float4 _Combine_b2458545efe84f8488335a6a0e9829f0_RGBA_4_Vector4;
float3 _Combine_b2458545efe84f8488335a6a0e9829f0_RGB_5_Vector3;
float2 _Combine_b2458545efe84f8488335a6a0e9829f0_RG_6_Vector2;
Unity_Combine_float(_Split_c9bf902bb4344b6a837c002876a4a742_R_1_Float, _Split_c9bf902bb4344b6a837c002876a4a742_B_3_Float, float(0), float(0), _Combine_b2458545efe84f8488335a6a0e9829f0_RGBA_4_Vector4, _Combine_b2458545efe84f8488335a6a0e9829f0_RGB_5_Vector3, _Combine_b2458545efe84f8488335a6a0e9829f0_RG_6_Vector2);
float2 _Property_36f0a343bbd443b0ba9ba96752bd5419_Out_0_Vector2 = _Cloud_Speed;
float2 _Divide_4d6b2552e0254de6a213dc9020221a26_Out_2_Vector2;
Unity_Divide_float2(_Property_36f0a343bbd443b0ba9ba96752bd5419_Out_0_Vector2, float2(100, 100), _Divide_4d6b2552e0254de6a213dc9020221a26_Out_2_Vector2);
float2 _Multiply_fb765c606df846c29196161ce8a5c5fe_Out_2_Vector2;
Unity_Multiply_float2_float2((IN.TimeParameters.x.xx), _Divide_4d6b2552e0254de6a213dc9020221a26_Out_2_Vector2, _Multiply_fb765c606df846c29196161ce8a5c5fe_Out_2_Vector2);
float2 _Add_4e43f841f4184513872472164ca37f4a_Out_2_Vector2;
Unity_Add_float2(_Combine_b2458545efe84f8488335a6a0e9829f0_RG_6_Vector2, _Multiply_fb765c606df846c29196161ce8a5c5fe_Out_2_Vector2, _Add_4e43f841f4184513872472164ca37f4a_Out_2_Vector2);
OutVector2_1 = _Add_4e43f841f4184513872472164ca37f4a_Out_2_Vector2;
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Power_float(float A, float B, out float Out)
{
    Out = pow(A, B);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
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
float _Property_4116819b6d874ed78027c7f0b237cb76_Out_0_Float = _Wind_Large_Scale;
float2 _Property_7855d6ce09864024b8c68f043f8cbdb4_Out_0_Vector2 = _Wind_Large_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45;
_PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_4116819b6d874ed78027c7f0b237cb76_Out_0_Float, _Property_7855d6ce09864024b8c68f043f8cbdb4_Out_0_Vector2, _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45, _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_R_5_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_G_6_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_B_7_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_A_8_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.a;
float _Subtract_ab6f6b551d064bb8b607c9a9bc381611_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_R_5_Float, float(0.5), _Subtract_ab6f6b551d064bb8b607c9a9bc381611_Out_2_Float);
float4 _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4 = IN.uv0;
float _Split_6ffb9a554e6e4ede9196adfc250975db_R_1_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[0];
float _Split_6ffb9a554e6e4ede9196adfc250975db_G_2_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[1];
float _Split_6ffb9a554e6e4ede9196adfc250975db_B_3_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[2];
float _Split_6ffb9a554e6e4ede9196adfc250975db_A_4_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[3];
float _Property_6eb67bc74e6b4fb288d6164ab2c7cdd8_Out_0_Float = _Grass_Pivot_Lock_Strength;
float _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float;
Unity_Power_float(_Split_6ffb9a554e6e4ede9196adfc250975db_G_2_Float, _Property_6eb67bc74e6b4fb288d6164ab2c7cdd8_Out_0_Float, _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float);
float _Multiply_219e592917cc4b659707ce8e75b39d9e_Out_2_Float;
Unity_Multiply_float_float(_Subtract_ab6f6b551d064bb8b607c9a9bc381611_Out_2_Float, _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float, _Multiply_219e592917cc4b659707ce8e75b39d9e_Out_2_Float);
float _Property_20c86c0cde6c4232a79793b50e170771_Out_0_Float = _Wind_Large_Multiplier;
float _Multiply_91c05a4257804a449cb4304b1f563d42_Out_2_Float;
Unity_Multiply_float_float(_Multiply_219e592917cc4b659707ce8e75b39d9e_Out_2_Float, _Property_20c86c0cde6c4232a79793b50e170771_Out_0_Float, _Multiply_91c05a4257804a449cb4304b1f563d42_Out_2_Float);
float _Property_fe4ba3b7d8f14c98b41d8948b19f735c_Out_0_Float = _Wind_Small_Scale;
float2 _Property_793e0429eb224e7694ee6cb87c6f31b9_Out_0_Vector2 = _Wind_Small_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3;
_PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_fe4ba3b7d8f14c98b41d8948b19f735c_Out_0_Float, _Property_793e0429eb224e7694ee6cb87c6f31b9_Out_0_Vector2, _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3, _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_R_5_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_G_6_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_B_7_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_A_8_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.a;
float _Subtract_95ef509ca9e2441ca6e08508d02ef96e_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_R_5_Float, float(0.5), _Subtract_95ef509ca9e2441ca6e08508d02ef96e_Out_2_Float);
float _Multiply_344852ef2f3546caa8c4f1f3695c3f0d_Out_2_Float;
Unity_Multiply_float_float(_Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float, _Subtract_95ef509ca9e2441ca6e08508d02ef96e_Out_2_Float, _Multiply_344852ef2f3546caa8c4f1f3695c3f0d_Out_2_Float);
float _Property_0943d2961c4c4e2ba15fdd94ac828776_Out_0_Float = _Wind_Small_Multiplier;
float _Multiply_4d6a0142972c48e889dfdbf32703e73e_Out_2_Float;
Unity_Multiply_float_float(_Multiply_344852ef2f3546caa8c4f1f3695c3f0d_Out_2_Float, _Property_0943d2961c4c4e2ba15fdd94ac828776_Out_0_Float, _Multiply_4d6a0142972c48e889dfdbf32703e73e_Out_2_Float);
float3 _Add_b8880c59f8304f648cf8583ead4fb728_Out_2_Vector3;
Unity_Add_float3(IN.WorldSpacePosition, (_Multiply_4d6a0142972c48e889dfdbf32703e73e_Out_2_Float.xxx), _Add_b8880c59f8304f648cf8583ead4fb728_Out_2_Vector3);
float3 _Add_48a8fd066e7e49e4891d1ae97e03bb65_Out_2_Vector3;
Unity_Add_float3((_Multiply_91c05a4257804a449cb4304b1f563d42_Out_2_Float.xxx), _Add_b8880c59f8304f648cf8583ead4fb728_Out_2_Vector3, _Add_48a8fd066e7e49e4891d1ae97e03bb65_Out_2_Vector3);
float3 _UPGFoliageBendWSCustomFunction_dc96fa4b2cc8432d856f03b9268115fe_offsetWS_2_Vector3;
UPG_FoliageBendWS_float(IN.WorldSpacePosition, _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float, _UPGFoliageBendWSCustomFunction_dc96fa4b2cc8432d856f03b9268115fe_offsetWS_2_Vector3);
float3 _Add_c1034f24720f4c5f9634821d7037f0ae_Out_2_Vector3;
Unity_Add_float3(_Add_48a8fd066e7e49e4891d1ae97e03bb65_Out_2_Vector3, _UPGFoliageBendWSCustomFunction_dc96fa4b2cc8432d856f03b9268115fe_offsetWS_2_Vector3, _Add_c1034f24720f4c5f9634821d7037f0ae_Out_2_Vector3);
float3 _Transform_583ec64076274514891c3c1e7f9946aa_Out_1_Vector3;
_Transform_583ec64076274514891c3c1e7f9946aa_Out_1_Vector3 = TransformWorldToObject(_Add_c1034f24720f4c5f9634821d7037f0ae_Out_2_Vector3.xyz);
float3 _Vector3_c086b8a0e95a4c27b6fc4d4d3843bd3b_Out_0_Vector3 = float3(float(0), float(1), float(0));
description.Position = _Transform_583ec64076274514891c3c1e7f9946aa_Out_1_Vector3;
description.Normal = _Vector3_c086b8a0e95a4c27b6fc4d4d3843bd3b_Out_0_Vector3;
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
float3 NormalOS;
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
float4 _Property_80081465fc9e42c78d044b56c43e5e1f_Out_0_Vector4 = _Color_Tint_Bottom;
float4 _Property_39c423b8c80d4d97946dc0b2c79b23c9_Out_0_Vector4 = _Color_Tint_Top;
float4 _UV_0a040733d8e9420d9054f7edb74c7682_Out_0_Vector4 = IN.uv0;
float _Split_e789d7c2ff9f4972bdb3462091aa8bfb_R_1_Float = _UV_0a040733d8e9420d9054f7edb74c7682_Out_0_Vector4[0];
float _Split_e789d7c2ff9f4972bdb3462091aa8bfb_G_2_Float = _UV_0a040733d8e9420d9054f7edb74c7682_Out_0_Vector4[1];
float _Split_e789d7c2ff9f4972bdb3462091aa8bfb_B_3_Float = _UV_0a040733d8e9420d9054f7edb74c7682_Out_0_Vector4[2];
float _Split_e789d7c2ff9f4972bdb3462091aa8bfb_A_4_Float = _UV_0a040733d8e9420d9054f7edb74c7682_Out_0_Vector4[3];
float _Property_5bf30ba4b92c4b7d81755e8764fdd331_Out_0_Float = _Gradience_Mul;
float _Multiply_0e8bd3c9e0bb453f8ddc49e301bbbe74_Out_2_Float;
Unity_Multiply_float_float(_Split_e789d7c2ff9f4972bdb3462091aa8bfb_G_2_Float, _Property_5bf30ba4b92c4b7d81755e8764fdd331_Out_0_Float, _Multiply_0e8bd3c9e0bb453f8ddc49e301bbbe74_Out_2_Float);
float _Property_92651d8cf9f8486f97f96477fee69822_Out_0_Float = _Gradience_Pow;
float _Power_21a1ac3e3c704dc2a230b301a4fd1bc3_Out_2_Float;
Unity_Power_float(_Multiply_0e8bd3c9e0bb453f8ddc49e301bbbe74_Out_2_Float, _Property_92651d8cf9f8486f97f96477fee69822_Out_0_Float, _Power_21a1ac3e3c704dc2a230b301a4fd1bc3_Out_2_Float);
float _Saturate_df6319cd6a8f44b8a2f98a45b86570e3_Out_1_Float;
Unity_Saturate_float(_Power_21a1ac3e3c704dc2a230b301a4fd1bc3_Out_2_Float, _Saturate_df6319cd6a8f44b8a2f98a45b86570e3_Out_1_Float);
float4 _Lerp_aef25ae41dbd4bb899f648885331bf52_Out_3_Vector4;
Unity_Lerp_float4(_Property_80081465fc9e42c78d044b56c43e5e1f_Out_0_Vector4, _Property_39c423b8c80d4d97946dc0b2c79b23c9_Out_0_Vector4, (_Saturate_df6319cd6a8f44b8a2f98a45b86570e3_Out_1_Float.xxxx), _Lerp_aef25ae41dbd4bb899f648885331bf52_Out_3_Vector4);
UnityTexture2D _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Base_Color, sampler_Base_Color, _Base_Color_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.tex, _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.samplerstate, _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4, _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_R_4_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.r;
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_G_5_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.g;
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_B_6_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.b;
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_A_7_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.a;
float4 _Multiply_fcd43956ee594716aa91c7691ce699cf_Out_2_Vector4;
Unity_Multiply_float4_float4(_Lerp_aef25ae41dbd4bb899f648885331bf52_Out_3_Vector4, _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4, _Multiply_fcd43956ee594716aa91c7691ce699cf_Out_2_Vector4);
float4 _Property_1d0cacb8faa34e6580302336f32b8591_Out_0_Vector4 = _Cloud_Color;
float4 _Multiply_17a3d0b90ebb4b1b9cab04c57f32aed2_Out_2_Vector4;
Unity_Multiply_float4_float4(_Multiply_fcd43956ee594716aa91c7691ce699cf_Out_2_Vector4, _Property_1d0cacb8faa34e6580302336f32b8591_Out_0_Vector4, _Multiply_17a3d0b90ebb4b1b9cab04c57f32aed2_Out_2_Vector4);
float _Property_8a8a73164ab1462fbb02778830bd9a39_Out_0_Float = _Cloud_Scale;
float2 _Property_b7fc6981bb03499fb69f338ec3fe9525_Out_0_Vector2 = _Cloud_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_dc86dca30b514354a0931aeb1998f16e;
_PositionalUVs_dc86dca30b514354a0931aeb1998f16e.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_dc86dca30b514354a0931aeb1998f16e.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_dc86dca30b514354a0931aeb1998f16e_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_8a8a73164ab1462fbb02778830bd9a39_Out_0_Float, _Property_b7fc6981bb03499fb69f338ec3fe9525_Out_0_Vector2, _PositionalUVs_dc86dca30b514354a0931aeb1998f16e, _PositionalUVs_dc86dca30b514354a0931aeb1998f16e_OutVector2_1_Vector2);
float4 _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(UnityBuildTexture2DStructNoScale(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_dc86dca30b514354a0931aeb1998f16e_OutVector2_1_Vector2) );
if (UnityBuildTexture2DStructNoScale(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D).hdrDecode.x > 0)
_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4, UnityBuildTexture2DStructNoScale(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D).hdrDecode);
float _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_R_4_Float = _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4.r;
float _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_G_5_Float = _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4.g;
float _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_B_6_Float = _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4.b;
float _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_A_7_Float = _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4.a;
float4 _Lerp_3bb853efd4d6410a8b63501c276cee08_Out_3_Vector4;
Unity_Lerp_float4(_Multiply_fcd43956ee594716aa91c7691ce699cf_Out_2_Vector4, _Multiply_17a3d0b90ebb4b1b9cab04c57f32aed2_Out_2_Vector4, (_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_R_4_Float.xxxx), _Lerp_3bb853efd4d6410a8b63501c276cee08_Out_3_Vector4);
float3 _Vector3_bf89382b9b774865b176e5e6ac1c1e0e_Out_0_Vector3 = float3(float(0), float(1), float(0));
float _Property_1336c01c0e674957b651c4aa288cb981_Out_0_Float = _Emissive_Intenstiy;
float _Property_f2ab9978dd8748fc98c66f03d7d2d5d4_Out_0_Float = _Emissive_Intenstiy_Clouds;
float _Lerp_b7e37fa8b0714a3b953fa8ba9bfe3919_Out_3_Float;
Unity_Lerp_float(_Property_1336c01c0e674957b651c4aa288cb981_Out_0_Float, _Property_f2ab9978dd8748fc98c66f03d7d2d5d4_Out_0_Float, _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_R_4_Float, _Lerp_b7e37fa8b0714a3b953fa8ba9bfe3919_Out_3_Float);
float4 _Multiply_c9eb85122f184daa97bd477002a538c5_Out_2_Vector4;
Unity_Multiply_float4_float4(_Lerp_3bb853efd4d6410a8b63501c276cee08_Out_3_Vector4, (_Lerp_b7e37fa8b0714a3b953fa8ba9bfe3919_Out_3_Float.xxxx), _Multiply_c9eb85122f184daa97bd477002a538c5_Out_2_Vector4);
float _Property_54e4601fd6944f448e3ee0aeac26aefa_Out_0_Float = _Smoothness;
surface.BaseColor = (_Lerp_3bb853efd4d6410a8b63501c276cee08_Out_3_Vector4.xyz);
surface.NormalOS = _Vector3_bf89382b9b774865b176e5e6ac1c1e0e_Out_0_Vector3;
surface.Emission = (_Multiply_c9eb85122f184daa97bd477002a538c5_Out_2_Vector4.xyz);
surface.Metallic = float(0);
surface.Smoothness = _Property_54e4601fd6944f448e3ee0aeac26aefa_Out_0_Float;
surface.Occlusion = float(1);
surface.Alpha = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_A_7_Float;
surface.AlphaClipThreshold = float(0.5);
float3 reactionUnusedColor = 0, reactionUnusedEmission = 0;
ClipVegetationFuel(IN.WorldSpacePosition);
ApplyVegetationReaction(IN.WorldSpacePosition,surface.BaseColor,surface.Emission);
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
    output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
    output.uv0 =                                        input.uv0;
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
    output.ObjectSpaceNormal = normalize(mul(output.WorldSpaceNormal, (float3x3) UNITY_MATRIX_M));           // transposed multiplication by inverse matrix to handle normal scale


    output.WorldSpacePosition = input.positionWS;

    #if UNITY_UV_STARTS_AT_TOP
    #else
    #endif


    output.uv0 = input.texCoord0;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
    output.TimeParameters = _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
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
// GraphKeywords: <None>

// Defines
#define _NORMALMAP 1
#define _NORMAL_DROPOFF_OS 1
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
 float3 ObjectSpaceNormal;
 float3 WorldSpaceNormal;
 float3 WorldSpacePosition;
 float4 uv0;
 float3 TimeParameters;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 WorldSpacePosition;
 float4 uv0;
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

float4 _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D_TexelSize;
float4 _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D_TexelSize;
float4 _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D_TexelSize;
float4 _Color_Tint_Bottom;
float2 _Cloud_Speed;
float _Emissive_Intenstiy_Clouds;
float _Wind_Large_Scale;
float _Terrain_Blend_Mul;
float _Cloud_Scale;
float2 _Wind_Large_Speed;
float _Wind_Small_Multiplier;
float4 _Cloud_Color;
float4 _Color_Tint_Top;
float4 _Base_Color_TexelSize;
float _Grass_Pivot_Lock_Strength;
float _Wind_Large_Multiplier;
float2 _Wind_Small_Speed;
float _Wind_Small_Scale;
float _Terrain_Blend_Pow;
float _Smoothness;
float4 _Emissive_Color;
float4 _Emissive_Clouds;
float _Gradience_Mul;
float _Gradience_Pow;
float _Emissive_Intenstiy;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D);
TEXTURE2D(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D);
TEXTURE2D(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D);
TEXTURE2D(_Base_Color);
SAMPLER(sampler_Base_Color);

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
#include_with_pragmas "Assets/06.Shaders/Foliage/UPG_FoliageInteraction.hlsl"

// Graph Functions

void Unity_Divide_float3(float3 A, float3 B, out float3 Out)
{
    Out = A / B;
}

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}

void Unity_Divide_float2(float2 A, float2 B, out float2 Out)
{
    Out = A / B;
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_Add_float2(float2 A, float2 B, out float2 Out)
{
    Out = A + B;
}

struct Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float
{
float3 WorldSpacePosition;
float3 TimeParameters;
};

void SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(float _Cloud_Scale, float2 _Cloud_Speed, Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float IN, out float2 OutVector2_1)
{
float _Property_da571d91c08d469b8fcdb0a2a1aa2cc1_Out_0_Float = _Cloud_Scale;
float3 _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3;
Unity_Divide_float3(IN.WorldSpacePosition, (_Property_da571d91c08d469b8fcdb0a2a1aa2cc1_Out_0_Float.xxx), _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3);
float _Split_c9bf902bb4344b6a837c002876a4a742_R_1_Float = _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3[0];
float _Split_c9bf902bb4344b6a837c002876a4a742_G_2_Float = _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3[1];
float _Split_c9bf902bb4344b6a837c002876a4a742_B_3_Float = _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3[2];
float _Split_c9bf902bb4344b6a837c002876a4a742_A_4_Float = 0;
float4 _Combine_b2458545efe84f8488335a6a0e9829f0_RGBA_4_Vector4;
float3 _Combine_b2458545efe84f8488335a6a0e9829f0_RGB_5_Vector3;
float2 _Combine_b2458545efe84f8488335a6a0e9829f0_RG_6_Vector2;
Unity_Combine_float(_Split_c9bf902bb4344b6a837c002876a4a742_R_1_Float, _Split_c9bf902bb4344b6a837c002876a4a742_B_3_Float, float(0), float(0), _Combine_b2458545efe84f8488335a6a0e9829f0_RGBA_4_Vector4, _Combine_b2458545efe84f8488335a6a0e9829f0_RGB_5_Vector3, _Combine_b2458545efe84f8488335a6a0e9829f0_RG_6_Vector2);
float2 _Property_36f0a343bbd443b0ba9ba96752bd5419_Out_0_Vector2 = _Cloud_Speed;
float2 _Divide_4d6b2552e0254de6a213dc9020221a26_Out_2_Vector2;
Unity_Divide_float2(_Property_36f0a343bbd443b0ba9ba96752bd5419_Out_0_Vector2, float2(100, 100), _Divide_4d6b2552e0254de6a213dc9020221a26_Out_2_Vector2);
float2 _Multiply_fb765c606df846c29196161ce8a5c5fe_Out_2_Vector2;
Unity_Multiply_float2_float2((IN.TimeParameters.x.xx), _Divide_4d6b2552e0254de6a213dc9020221a26_Out_2_Vector2, _Multiply_fb765c606df846c29196161ce8a5c5fe_Out_2_Vector2);
float2 _Add_4e43f841f4184513872472164ca37f4a_Out_2_Vector2;
Unity_Add_float2(_Combine_b2458545efe84f8488335a6a0e9829f0_RG_6_Vector2, _Multiply_fb765c606df846c29196161ce8a5c5fe_Out_2_Vector2, _Add_4e43f841f4184513872472164ca37f4a_Out_2_Vector2);
OutVector2_1 = _Add_4e43f841f4184513872472164ca37f4a_Out_2_Vector2;
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Power_float(float A, float B, out float Out)
{
    Out = pow(A, B);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
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
float _Property_4116819b6d874ed78027c7f0b237cb76_Out_0_Float = _Wind_Large_Scale;
float2 _Property_7855d6ce09864024b8c68f043f8cbdb4_Out_0_Vector2 = _Wind_Large_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45;
_PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_4116819b6d874ed78027c7f0b237cb76_Out_0_Float, _Property_7855d6ce09864024b8c68f043f8cbdb4_Out_0_Vector2, _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45, _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_R_5_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_G_6_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_B_7_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_A_8_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.a;
float _Subtract_ab6f6b551d064bb8b607c9a9bc381611_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_R_5_Float, float(0.5), _Subtract_ab6f6b551d064bb8b607c9a9bc381611_Out_2_Float);
float4 _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4 = IN.uv0;
float _Split_6ffb9a554e6e4ede9196adfc250975db_R_1_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[0];
float _Split_6ffb9a554e6e4ede9196adfc250975db_G_2_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[1];
float _Split_6ffb9a554e6e4ede9196adfc250975db_B_3_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[2];
float _Split_6ffb9a554e6e4ede9196adfc250975db_A_4_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[3];
float _Property_6eb67bc74e6b4fb288d6164ab2c7cdd8_Out_0_Float = _Grass_Pivot_Lock_Strength;
float _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float;
Unity_Power_float(_Split_6ffb9a554e6e4ede9196adfc250975db_G_2_Float, _Property_6eb67bc74e6b4fb288d6164ab2c7cdd8_Out_0_Float, _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float);
float _Multiply_219e592917cc4b659707ce8e75b39d9e_Out_2_Float;
Unity_Multiply_float_float(_Subtract_ab6f6b551d064bb8b607c9a9bc381611_Out_2_Float, _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float, _Multiply_219e592917cc4b659707ce8e75b39d9e_Out_2_Float);
float _Property_20c86c0cde6c4232a79793b50e170771_Out_0_Float = _Wind_Large_Multiplier;
float _Multiply_91c05a4257804a449cb4304b1f563d42_Out_2_Float;
Unity_Multiply_float_float(_Multiply_219e592917cc4b659707ce8e75b39d9e_Out_2_Float, _Property_20c86c0cde6c4232a79793b50e170771_Out_0_Float, _Multiply_91c05a4257804a449cb4304b1f563d42_Out_2_Float);
float _Property_fe4ba3b7d8f14c98b41d8948b19f735c_Out_0_Float = _Wind_Small_Scale;
float2 _Property_793e0429eb224e7694ee6cb87c6f31b9_Out_0_Vector2 = _Wind_Small_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3;
_PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_fe4ba3b7d8f14c98b41d8948b19f735c_Out_0_Float, _Property_793e0429eb224e7694ee6cb87c6f31b9_Out_0_Vector2, _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3, _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_R_5_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_G_6_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_B_7_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_A_8_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.a;
float _Subtract_95ef509ca9e2441ca6e08508d02ef96e_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_R_5_Float, float(0.5), _Subtract_95ef509ca9e2441ca6e08508d02ef96e_Out_2_Float);
float _Multiply_344852ef2f3546caa8c4f1f3695c3f0d_Out_2_Float;
Unity_Multiply_float_float(_Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float, _Subtract_95ef509ca9e2441ca6e08508d02ef96e_Out_2_Float, _Multiply_344852ef2f3546caa8c4f1f3695c3f0d_Out_2_Float);
float _Property_0943d2961c4c4e2ba15fdd94ac828776_Out_0_Float = _Wind_Small_Multiplier;
float _Multiply_4d6a0142972c48e889dfdbf32703e73e_Out_2_Float;
Unity_Multiply_float_float(_Multiply_344852ef2f3546caa8c4f1f3695c3f0d_Out_2_Float, _Property_0943d2961c4c4e2ba15fdd94ac828776_Out_0_Float, _Multiply_4d6a0142972c48e889dfdbf32703e73e_Out_2_Float);
float3 _Add_b8880c59f8304f648cf8583ead4fb728_Out_2_Vector3;
Unity_Add_float3(IN.WorldSpacePosition, (_Multiply_4d6a0142972c48e889dfdbf32703e73e_Out_2_Float.xxx), _Add_b8880c59f8304f648cf8583ead4fb728_Out_2_Vector3);
float3 _Add_48a8fd066e7e49e4891d1ae97e03bb65_Out_2_Vector3;
Unity_Add_float3((_Multiply_91c05a4257804a449cb4304b1f563d42_Out_2_Float.xxx), _Add_b8880c59f8304f648cf8583ead4fb728_Out_2_Vector3, _Add_48a8fd066e7e49e4891d1ae97e03bb65_Out_2_Vector3);
float3 _UPGFoliageBendWSCustomFunction_dc96fa4b2cc8432d856f03b9268115fe_offsetWS_2_Vector3;
UPG_FoliageBendWS_float(IN.WorldSpacePosition, _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float, _UPGFoliageBendWSCustomFunction_dc96fa4b2cc8432d856f03b9268115fe_offsetWS_2_Vector3);
float3 _Add_c1034f24720f4c5f9634821d7037f0ae_Out_2_Vector3;
Unity_Add_float3(_Add_48a8fd066e7e49e4891d1ae97e03bb65_Out_2_Vector3, _UPGFoliageBendWSCustomFunction_dc96fa4b2cc8432d856f03b9268115fe_offsetWS_2_Vector3, _Add_c1034f24720f4c5f9634821d7037f0ae_Out_2_Vector3);
float3 _Transform_583ec64076274514891c3c1e7f9946aa_Out_1_Vector3;
_Transform_583ec64076274514891c3c1e7f9946aa_Out_1_Vector3 = TransformWorldToObject(_Add_c1034f24720f4c5f9634821d7037f0ae_Out_2_Vector3.xyz);
float3 _Vector3_c086b8a0e95a4c27b6fc4d4d3843bd3b_Out_0_Vector3 = float3(float(0), float(1), float(0));
description.Position = _Transform_583ec64076274514891c3c1e7f9946aa_Out_1_Vector3;
description.Normal = _Vector3_c086b8a0e95a4c27b6fc4d4d3843bd3b_Out_0_Vector3;
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
float3 NormalOS;
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
float4 _Property_80081465fc9e42c78d044b56c43e5e1f_Out_0_Vector4 = _Color_Tint_Bottom;
float4 _Property_39c423b8c80d4d97946dc0b2c79b23c9_Out_0_Vector4 = _Color_Tint_Top;
float4 _UV_0a040733d8e9420d9054f7edb74c7682_Out_0_Vector4 = IN.uv0;
float _Split_e789d7c2ff9f4972bdb3462091aa8bfb_R_1_Float = _UV_0a040733d8e9420d9054f7edb74c7682_Out_0_Vector4[0];
float _Split_e789d7c2ff9f4972bdb3462091aa8bfb_G_2_Float = _UV_0a040733d8e9420d9054f7edb74c7682_Out_0_Vector4[1];
float _Split_e789d7c2ff9f4972bdb3462091aa8bfb_B_3_Float = _UV_0a040733d8e9420d9054f7edb74c7682_Out_0_Vector4[2];
float _Split_e789d7c2ff9f4972bdb3462091aa8bfb_A_4_Float = _UV_0a040733d8e9420d9054f7edb74c7682_Out_0_Vector4[3];
float _Property_5bf30ba4b92c4b7d81755e8764fdd331_Out_0_Float = _Gradience_Mul;
float _Multiply_0e8bd3c9e0bb453f8ddc49e301bbbe74_Out_2_Float;
Unity_Multiply_float_float(_Split_e789d7c2ff9f4972bdb3462091aa8bfb_G_2_Float, _Property_5bf30ba4b92c4b7d81755e8764fdd331_Out_0_Float, _Multiply_0e8bd3c9e0bb453f8ddc49e301bbbe74_Out_2_Float);
float _Property_92651d8cf9f8486f97f96477fee69822_Out_0_Float = _Gradience_Pow;
float _Power_21a1ac3e3c704dc2a230b301a4fd1bc3_Out_2_Float;
Unity_Power_float(_Multiply_0e8bd3c9e0bb453f8ddc49e301bbbe74_Out_2_Float, _Property_92651d8cf9f8486f97f96477fee69822_Out_0_Float, _Power_21a1ac3e3c704dc2a230b301a4fd1bc3_Out_2_Float);
float _Saturate_df6319cd6a8f44b8a2f98a45b86570e3_Out_1_Float;
Unity_Saturate_float(_Power_21a1ac3e3c704dc2a230b301a4fd1bc3_Out_2_Float, _Saturate_df6319cd6a8f44b8a2f98a45b86570e3_Out_1_Float);
float4 _Lerp_aef25ae41dbd4bb899f648885331bf52_Out_3_Vector4;
Unity_Lerp_float4(_Property_80081465fc9e42c78d044b56c43e5e1f_Out_0_Vector4, _Property_39c423b8c80d4d97946dc0b2c79b23c9_Out_0_Vector4, (_Saturate_df6319cd6a8f44b8a2f98a45b86570e3_Out_1_Float.xxxx), _Lerp_aef25ae41dbd4bb899f648885331bf52_Out_3_Vector4);
UnityTexture2D _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Base_Color, sampler_Base_Color, _Base_Color_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.tex, _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.samplerstate, _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4, _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_R_4_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.r;
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_G_5_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.g;
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_B_6_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.b;
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_A_7_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.a;
float4 _Multiply_fcd43956ee594716aa91c7691ce699cf_Out_2_Vector4;
Unity_Multiply_float4_float4(_Lerp_aef25ae41dbd4bb899f648885331bf52_Out_3_Vector4, _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4, _Multiply_fcd43956ee594716aa91c7691ce699cf_Out_2_Vector4);
float4 _Property_1d0cacb8faa34e6580302336f32b8591_Out_0_Vector4 = _Cloud_Color;
float4 _Multiply_17a3d0b90ebb4b1b9cab04c57f32aed2_Out_2_Vector4;
Unity_Multiply_float4_float4(_Multiply_fcd43956ee594716aa91c7691ce699cf_Out_2_Vector4, _Property_1d0cacb8faa34e6580302336f32b8591_Out_0_Vector4, _Multiply_17a3d0b90ebb4b1b9cab04c57f32aed2_Out_2_Vector4);
float _Property_8a8a73164ab1462fbb02778830bd9a39_Out_0_Float = _Cloud_Scale;
float2 _Property_b7fc6981bb03499fb69f338ec3fe9525_Out_0_Vector2 = _Cloud_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_dc86dca30b514354a0931aeb1998f16e;
_PositionalUVs_dc86dca30b514354a0931aeb1998f16e.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_dc86dca30b514354a0931aeb1998f16e.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_dc86dca30b514354a0931aeb1998f16e_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_8a8a73164ab1462fbb02778830bd9a39_Out_0_Float, _Property_b7fc6981bb03499fb69f338ec3fe9525_Out_0_Vector2, _PositionalUVs_dc86dca30b514354a0931aeb1998f16e, _PositionalUVs_dc86dca30b514354a0931aeb1998f16e_OutVector2_1_Vector2);
float4 _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(UnityBuildTexture2DStructNoScale(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_dc86dca30b514354a0931aeb1998f16e_OutVector2_1_Vector2) );
if (UnityBuildTexture2DStructNoScale(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D).hdrDecode.x > 0)
_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4, UnityBuildTexture2DStructNoScale(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D).hdrDecode);
float _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_R_4_Float = _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4.r;
float _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_G_5_Float = _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4.g;
float _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_B_6_Float = _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4.b;
float _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_A_7_Float = _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4.a;
float4 _Lerp_3bb853efd4d6410a8b63501c276cee08_Out_3_Vector4;
Unity_Lerp_float4(_Multiply_fcd43956ee594716aa91c7691ce699cf_Out_2_Vector4, _Multiply_17a3d0b90ebb4b1b9cab04c57f32aed2_Out_2_Vector4, (_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_R_4_Float.xxxx), _Lerp_3bb853efd4d6410a8b63501c276cee08_Out_3_Vector4);
float3 _Vector3_bf89382b9b774865b176e5e6ac1c1e0e_Out_0_Vector3 = float3(float(0), float(1), float(0));
float _Property_1336c01c0e674957b651c4aa288cb981_Out_0_Float = _Emissive_Intenstiy;
float _Property_f2ab9978dd8748fc98c66f03d7d2d5d4_Out_0_Float = _Emissive_Intenstiy_Clouds;
float _Lerp_b7e37fa8b0714a3b953fa8ba9bfe3919_Out_3_Float;
Unity_Lerp_float(_Property_1336c01c0e674957b651c4aa288cb981_Out_0_Float, _Property_f2ab9978dd8748fc98c66f03d7d2d5d4_Out_0_Float, _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_R_4_Float, _Lerp_b7e37fa8b0714a3b953fa8ba9bfe3919_Out_3_Float);
float4 _Multiply_c9eb85122f184daa97bd477002a538c5_Out_2_Vector4;
Unity_Multiply_float4_float4(_Lerp_3bb853efd4d6410a8b63501c276cee08_Out_3_Vector4, (_Lerp_b7e37fa8b0714a3b953fa8ba9bfe3919_Out_3_Float.xxxx), _Multiply_c9eb85122f184daa97bd477002a538c5_Out_2_Vector4);
float _Property_54e4601fd6944f448e3ee0aeac26aefa_Out_0_Float = _Smoothness;
surface.BaseColor = (_Lerp_3bb853efd4d6410a8b63501c276cee08_Out_3_Vector4.xyz);
surface.NormalOS = _Vector3_bf89382b9b774865b176e5e6ac1c1e0e_Out_0_Vector3;
surface.Emission = (_Multiply_c9eb85122f184daa97bd477002a538c5_Out_2_Vector4.xyz);
surface.Metallic = float(0);
surface.Smoothness = _Property_54e4601fd6944f448e3ee0aeac26aefa_Out_0_Float;
surface.Occlusion = float(1);
surface.Alpha = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_A_7_Float;
surface.AlphaClipThreshold = float(0.5);
float3 reactionUnusedColor = 0, reactionUnusedEmission = 0;
ClipVegetationFuel(IN.WorldSpacePosition);
ApplyVegetationReaction(IN.WorldSpacePosition,surface.BaseColor,surface.Emission);
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
    output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
    output.uv0 =                                        input.uv0;
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
    output.ObjectSpaceNormal = normalize(mul(output.WorldSpaceNormal, (float3x3) UNITY_MATRIX_M));           // transposed multiplication by inverse matrix to handle normal scale


    output.WorldSpacePosition = input.positionWS;

    #if UNITY_UV_STARTS_AT_TOP
    #else
    #endif


    output.uv0 = input.texCoord0;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
    output.TimeParameters = _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
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
// GraphKeywords: <None>

// Defines
#define _NORMALMAP 1
#define _NORMAL_DROPOFF_OS 1
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
 float3 WorldSpacePosition;
 float4 uv0;
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

float4 _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D_TexelSize;
float4 _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D_TexelSize;
float4 _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D_TexelSize;
float4 _Color_Tint_Bottom;
float2 _Cloud_Speed;
float _Emissive_Intenstiy_Clouds;
float _Wind_Large_Scale;
float _Terrain_Blend_Mul;
float _Cloud_Scale;
float2 _Wind_Large_Speed;
float _Wind_Small_Multiplier;
float4 _Cloud_Color;
float4 _Color_Tint_Top;
float4 _Base_Color_TexelSize;
float _Grass_Pivot_Lock_Strength;
float _Wind_Large_Multiplier;
float2 _Wind_Small_Speed;
float _Wind_Small_Scale;
float _Terrain_Blend_Pow;
float _Smoothness;
float4 _Emissive_Color;
float4 _Emissive_Clouds;
float _Gradience_Mul;
float _Gradience_Pow;
float _Emissive_Intenstiy;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D);
TEXTURE2D(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D);
TEXTURE2D(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D);
TEXTURE2D(_Base_Color);
SAMPLER(sampler_Base_Color);

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
#include_with_pragmas "Assets/06.Shaders/Foliage/UPG_FoliageInteraction.hlsl"

// Graph Functions

void Unity_Divide_float3(float3 A, float3 B, out float3 Out)
{
    Out = A / B;
}

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}

void Unity_Divide_float2(float2 A, float2 B, out float2 Out)
{
    Out = A / B;
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_Add_float2(float2 A, float2 B, out float2 Out)
{
    Out = A + B;
}

struct Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float
{
float3 WorldSpacePosition;
float3 TimeParameters;
};

void SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(float _Cloud_Scale, float2 _Cloud_Speed, Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float IN, out float2 OutVector2_1)
{
float _Property_da571d91c08d469b8fcdb0a2a1aa2cc1_Out_0_Float = _Cloud_Scale;
float3 _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3;
Unity_Divide_float3(IN.WorldSpacePosition, (_Property_da571d91c08d469b8fcdb0a2a1aa2cc1_Out_0_Float.xxx), _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3);
float _Split_c9bf902bb4344b6a837c002876a4a742_R_1_Float = _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3[0];
float _Split_c9bf902bb4344b6a837c002876a4a742_G_2_Float = _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3[1];
float _Split_c9bf902bb4344b6a837c002876a4a742_B_3_Float = _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3[2];
float _Split_c9bf902bb4344b6a837c002876a4a742_A_4_Float = 0;
float4 _Combine_b2458545efe84f8488335a6a0e9829f0_RGBA_4_Vector4;
float3 _Combine_b2458545efe84f8488335a6a0e9829f0_RGB_5_Vector3;
float2 _Combine_b2458545efe84f8488335a6a0e9829f0_RG_6_Vector2;
Unity_Combine_float(_Split_c9bf902bb4344b6a837c002876a4a742_R_1_Float, _Split_c9bf902bb4344b6a837c002876a4a742_B_3_Float, float(0), float(0), _Combine_b2458545efe84f8488335a6a0e9829f0_RGBA_4_Vector4, _Combine_b2458545efe84f8488335a6a0e9829f0_RGB_5_Vector3, _Combine_b2458545efe84f8488335a6a0e9829f0_RG_6_Vector2);
float2 _Property_36f0a343bbd443b0ba9ba96752bd5419_Out_0_Vector2 = _Cloud_Speed;
float2 _Divide_4d6b2552e0254de6a213dc9020221a26_Out_2_Vector2;
Unity_Divide_float2(_Property_36f0a343bbd443b0ba9ba96752bd5419_Out_0_Vector2, float2(100, 100), _Divide_4d6b2552e0254de6a213dc9020221a26_Out_2_Vector2);
float2 _Multiply_fb765c606df846c29196161ce8a5c5fe_Out_2_Vector2;
Unity_Multiply_float2_float2((IN.TimeParameters.x.xx), _Divide_4d6b2552e0254de6a213dc9020221a26_Out_2_Vector2, _Multiply_fb765c606df846c29196161ce8a5c5fe_Out_2_Vector2);
float2 _Add_4e43f841f4184513872472164ca37f4a_Out_2_Vector2;
Unity_Add_float2(_Combine_b2458545efe84f8488335a6a0e9829f0_RG_6_Vector2, _Multiply_fb765c606df846c29196161ce8a5c5fe_Out_2_Vector2, _Add_4e43f841f4184513872472164ca37f4a_Out_2_Vector2);
OutVector2_1 = _Add_4e43f841f4184513872472164ca37f4a_Out_2_Vector2;
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Power_float(float A, float B, out float Out)
{
    Out = pow(A, B);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
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
float _Property_4116819b6d874ed78027c7f0b237cb76_Out_0_Float = _Wind_Large_Scale;
float2 _Property_7855d6ce09864024b8c68f043f8cbdb4_Out_0_Vector2 = _Wind_Large_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45;
_PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_4116819b6d874ed78027c7f0b237cb76_Out_0_Float, _Property_7855d6ce09864024b8c68f043f8cbdb4_Out_0_Vector2, _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45, _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_R_5_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_G_6_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_B_7_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_A_8_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.a;
float _Subtract_ab6f6b551d064bb8b607c9a9bc381611_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_R_5_Float, float(0.5), _Subtract_ab6f6b551d064bb8b607c9a9bc381611_Out_2_Float);
float4 _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4 = IN.uv0;
float _Split_6ffb9a554e6e4ede9196adfc250975db_R_1_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[0];
float _Split_6ffb9a554e6e4ede9196adfc250975db_G_2_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[1];
float _Split_6ffb9a554e6e4ede9196adfc250975db_B_3_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[2];
float _Split_6ffb9a554e6e4ede9196adfc250975db_A_4_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[3];
float _Property_6eb67bc74e6b4fb288d6164ab2c7cdd8_Out_0_Float = _Grass_Pivot_Lock_Strength;
float _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float;
Unity_Power_float(_Split_6ffb9a554e6e4ede9196adfc250975db_G_2_Float, _Property_6eb67bc74e6b4fb288d6164ab2c7cdd8_Out_0_Float, _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float);
float _Multiply_219e592917cc4b659707ce8e75b39d9e_Out_2_Float;
Unity_Multiply_float_float(_Subtract_ab6f6b551d064bb8b607c9a9bc381611_Out_2_Float, _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float, _Multiply_219e592917cc4b659707ce8e75b39d9e_Out_2_Float);
float _Property_20c86c0cde6c4232a79793b50e170771_Out_0_Float = _Wind_Large_Multiplier;
float _Multiply_91c05a4257804a449cb4304b1f563d42_Out_2_Float;
Unity_Multiply_float_float(_Multiply_219e592917cc4b659707ce8e75b39d9e_Out_2_Float, _Property_20c86c0cde6c4232a79793b50e170771_Out_0_Float, _Multiply_91c05a4257804a449cb4304b1f563d42_Out_2_Float);
float _Property_fe4ba3b7d8f14c98b41d8948b19f735c_Out_0_Float = _Wind_Small_Scale;
float2 _Property_793e0429eb224e7694ee6cb87c6f31b9_Out_0_Vector2 = _Wind_Small_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3;
_PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_fe4ba3b7d8f14c98b41d8948b19f735c_Out_0_Float, _Property_793e0429eb224e7694ee6cb87c6f31b9_Out_0_Vector2, _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3, _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_R_5_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_G_6_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_B_7_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_A_8_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.a;
float _Subtract_95ef509ca9e2441ca6e08508d02ef96e_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_R_5_Float, float(0.5), _Subtract_95ef509ca9e2441ca6e08508d02ef96e_Out_2_Float);
float _Multiply_344852ef2f3546caa8c4f1f3695c3f0d_Out_2_Float;
Unity_Multiply_float_float(_Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float, _Subtract_95ef509ca9e2441ca6e08508d02ef96e_Out_2_Float, _Multiply_344852ef2f3546caa8c4f1f3695c3f0d_Out_2_Float);
float _Property_0943d2961c4c4e2ba15fdd94ac828776_Out_0_Float = _Wind_Small_Multiplier;
float _Multiply_4d6a0142972c48e889dfdbf32703e73e_Out_2_Float;
Unity_Multiply_float_float(_Multiply_344852ef2f3546caa8c4f1f3695c3f0d_Out_2_Float, _Property_0943d2961c4c4e2ba15fdd94ac828776_Out_0_Float, _Multiply_4d6a0142972c48e889dfdbf32703e73e_Out_2_Float);
float3 _Add_b8880c59f8304f648cf8583ead4fb728_Out_2_Vector3;
Unity_Add_float3(IN.WorldSpacePosition, (_Multiply_4d6a0142972c48e889dfdbf32703e73e_Out_2_Float.xxx), _Add_b8880c59f8304f648cf8583ead4fb728_Out_2_Vector3);
float3 _Add_48a8fd066e7e49e4891d1ae97e03bb65_Out_2_Vector3;
Unity_Add_float3((_Multiply_91c05a4257804a449cb4304b1f563d42_Out_2_Float.xxx), _Add_b8880c59f8304f648cf8583ead4fb728_Out_2_Vector3, _Add_48a8fd066e7e49e4891d1ae97e03bb65_Out_2_Vector3);
float3 _UPGFoliageBendWSCustomFunction_dc96fa4b2cc8432d856f03b9268115fe_offsetWS_2_Vector3;
UPG_FoliageBendWS_float(IN.WorldSpacePosition, _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float, _UPGFoliageBendWSCustomFunction_dc96fa4b2cc8432d856f03b9268115fe_offsetWS_2_Vector3);
float3 _Add_c1034f24720f4c5f9634821d7037f0ae_Out_2_Vector3;
Unity_Add_float3(_Add_48a8fd066e7e49e4891d1ae97e03bb65_Out_2_Vector3, _UPGFoliageBendWSCustomFunction_dc96fa4b2cc8432d856f03b9268115fe_offsetWS_2_Vector3, _Add_c1034f24720f4c5f9634821d7037f0ae_Out_2_Vector3);
float3 _Transform_583ec64076274514891c3c1e7f9946aa_Out_1_Vector3;
_Transform_583ec64076274514891c3c1e7f9946aa_Out_1_Vector3 = TransformWorldToObject(_Add_c1034f24720f4c5f9634821d7037f0ae_Out_2_Vector3.xyz);
float3 _Vector3_c086b8a0e95a4c27b6fc4d4d3843bd3b_Out_0_Vector3 = float3(float(0), float(1), float(0));
description.Position = _Transform_583ec64076274514891c3c1e7f9946aa_Out_1_Vector3;
description.Normal = _Vector3_c086b8a0e95a4c27b6fc4d4d3843bd3b_Out_0_Vector3;
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
UnityTexture2D _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Base_Color, sampler_Base_Color, _Base_Color_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.tex, _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.samplerstate, _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4, _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_R_4_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.r;
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_G_5_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.g;
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_B_6_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.b;
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_A_7_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.a;
surface.Alpha = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_A_7_Float;
surface.AlphaClipThreshold = float(0.5);
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
    output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
    output.uv0 =                                        input.uv0;
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
// GraphKeywords: <None>

// Defines
#define _NORMALMAP 1
#define _NORMAL_DROPOFF_OS 1
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
 float3 WorldSpacePosition;
 float4 uv0;
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

float4 _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D_TexelSize;
float4 _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D_TexelSize;
float4 _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D_TexelSize;
float4 _Color_Tint_Bottom;
float2 _Cloud_Speed;
float _Emissive_Intenstiy_Clouds;
float _Wind_Large_Scale;
float _Terrain_Blend_Mul;
float _Cloud_Scale;
float2 _Wind_Large_Speed;
float _Wind_Small_Multiplier;
float4 _Cloud_Color;
float4 _Color_Tint_Top;
float4 _Base_Color_TexelSize;
float _Grass_Pivot_Lock_Strength;
float _Wind_Large_Multiplier;
float2 _Wind_Small_Speed;
float _Wind_Small_Scale;
float _Terrain_Blend_Pow;
float _Smoothness;
float4 _Emissive_Color;
float4 _Emissive_Clouds;
float _Gradience_Mul;
float _Gradience_Pow;
float _Emissive_Intenstiy;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D);
TEXTURE2D(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D);
TEXTURE2D(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D);
TEXTURE2D(_Base_Color);
SAMPLER(sampler_Base_Color);

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
#include_with_pragmas "Assets/06.Shaders/Foliage/UPG_FoliageInteraction.hlsl"

// Graph Functions

void Unity_Divide_float3(float3 A, float3 B, out float3 Out)
{
    Out = A / B;
}

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}

void Unity_Divide_float2(float2 A, float2 B, out float2 Out)
{
    Out = A / B;
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_Add_float2(float2 A, float2 B, out float2 Out)
{
    Out = A + B;
}

struct Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float
{
float3 WorldSpacePosition;
float3 TimeParameters;
};

void SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(float _Cloud_Scale, float2 _Cloud_Speed, Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float IN, out float2 OutVector2_1)
{
float _Property_da571d91c08d469b8fcdb0a2a1aa2cc1_Out_0_Float = _Cloud_Scale;
float3 _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3;
Unity_Divide_float3(IN.WorldSpacePosition, (_Property_da571d91c08d469b8fcdb0a2a1aa2cc1_Out_0_Float.xxx), _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3);
float _Split_c9bf902bb4344b6a837c002876a4a742_R_1_Float = _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3[0];
float _Split_c9bf902bb4344b6a837c002876a4a742_G_2_Float = _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3[1];
float _Split_c9bf902bb4344b6a837c002876a4a742_B_3_Float = _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3[2];
float _Split_c9bf902bb4344b6a837c002876a4a742_A_4_Float = 0;
float4 _Combine_b2458545efe84f8488335a6a0e9829f0_RGBA_4_Vector4;
float3 _Combine_b2458545efe84f8488335a6a0e9829f0_RGB_5_Vector3;
float2 _Combine_b2458545efe84f8488335a6a0e9829f0_RG_6_Vector2;
Unity_Combine_float(_Split_c9bf902bb4344b6a837c002876a4a742_R_1_Float, _Split_c9bf902bb4344b6a837c002876a4a742_B_3_Float, float(0), float(0), _Combine_b2458545efe84f8488335a6a0e9829f0_RGBA_4_Vector4, _Combine_b2458545efe84f8488335a6a0e9829f0_RGB_5_Vector3, _Combine_b2458545efe84f8488335a6a0e9829f0_RG_6_Vector2);
float2 _Property_36f0a343bbd443b0ba9ba96752bd5419_Out_0_Vector2 = _Cloud_Speed;
float2 _Divide_4d6b2552e0254de6a213dc9020221a26_Out_2_Vector2;
Unity_Divide_float2(_Property_36f0a343bbd443b0ba9ba96752bd5419_Out_0_Vector2, float2(100, 100), _Divide_4d6b2552e0254de6a213dc9020221a26_Out_2_Vector2);
float2 _Multiply_fb765c606df846c29196161ce8a5c5fe_Out_2_Vector2;
Unity_Multiply_float2_float2((IN.TimeParameters.x.xx), _Divide_4d6b2552e0254de6a213dc9020221a26_Out_2_Vector2, _Multiply_fb765c606df846c29196161ce8a5c5fe_Out_2_Vector2);
float2 _Add_4e43f841f4184513872472164ca37f4a_Out_2_Vector2;
Unity_Add_float2(_Combine_b2458545efe84f8488335a6a0e9829f0_RG_6_Vector2, _Multiply_fb765c606df846c29196161ce8a5c5fe_Out_2_Vector2, _Add_4e43f841f4184513872472164ca37f4a_Out_2_Vector2);
OutVector2_1 = _Add_4e43f841f4184513872472164ca37f4a_Out_2_Vector2;
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Power_float(float A, float B, out float Out)
{
    Out = pow(A, B);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
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
float _Property_4116819b6d874ed78027c7f0b237cb76_Out_0_Float = _Wind_Large_Scale;
float2 _Property_7855d6ce09864024b8c68f043f8cbdb4_Out_0_Vector2 = _Wind_Large_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45;
_PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_4116819b6d874ed78027c7f0b237cb76_Out_0_Float, _Property_7855d6ce09864024b8c68f043f8cbdb4_Out_0_Vector2, _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45, _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_R_5_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_G_6_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_B_7_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_A_8_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.a;
float _Subtract_ab6f6b551d064bb8b607c9a9bc381611_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_R_5_Float, float(0.5), _Subtract_ab6f6b551d064bb8b607c9a9bc381611_Out_2_Float);
float4 _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4 = IN.uv0;
float _Split_6ffb9a554e6e4ede9196adfc250975db_R_1_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[0];
float _Split_6ffb9a554e6e4ede9196adfc250975db_G_2_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[1];
float _Split_6ffb9a554e6e4ede9196adfc250975db_B_3_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[2];
float _Split_6ffb9a554e6e4ede9196adfc250975db_A_4_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[3];
float _Property_6eb67bc74e6b4fb288d6164ab2c7cdd8_Out_0_Float = _Grass_Pivot_Lock_Strength;
float _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float;
Unity_Power_float(_Split_6ffb9a554e6e4ede9196adfc250975db_G_2_Float, _Property_6eb67bc74e6b4fb288d6164ab2c7cdd8_Out_0_Float, _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float);
float _Multiply_219e592917cc4b659707ce8e75b39d9e_Out_2_Float;
Unity_Multiply_float_float(_Subtract_ab6f6b551d064bb8b607c9a9bc381611_Out_2_Float, _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float, _Multiply_219e592917cc4b659707ce8e75b39d9e_Out_2_Float);
float _Property_20c86c0cde6c4232a79793b50e170771_Out_0_Float = _Wind_Large_Multiplier;
float _Multiply_91c05a4257804a449cb4304b1f563d42_Out_2_Float;
Unity_Multiply_float_float(_Multiply_219e592917cc4b659707ce8e75b39d9e_Out_2_Float, _Property_20c86c0cde6c4232a79793b50e170771_Out_0_Float, _Multiply_91c05a4257804a449cb4304b1f563d42_Out_2_Float);
float _Property_fe4ba3b7d8f14c98b41d8948b19f735c_Out_0_Float = _Wind_Small_Scale;
float2 _Property_793e0429eb224e7694ee6cb87c6f31b9_Out_0_Vector2 = _Wind_Small_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3;
_PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_fe4ba3b7d8f14c98b41d8948b19f735c_Out_0_Float, _Property_793e0429eb224e7694ee6cb87c6f31b9_Out_0_Vector2, _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3, _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_R_5_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_G_6_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_B_7_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_A_8_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.a;
float _Subtract_95ef509ca9e2441ca6e08508d02ef96e_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_R_5_Float, float(0.5), _Subtract_95ef509ca9e2441ca6e08508d02ef96e_Out_2_Float);
float _Multiply_344852ef2f3546caa8c4f1f3695c3f0d_Out_2_Float;
Unity_Multiply_float_float(_Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float, _Subtract_95ef509ca9e2441ca6e08508d02ef96e_Out_2_Float, _Multiply_344852ef2f3546caa8c4f1f3695c3f0d_Out_2_Float);
float _Property_0943d2961c4c4e2ba15fdd94ac828776_Out_0_Float = _Wind_Small_Multiplier;
float _Multiply_4d6a0142972c48e889dfdbf32703e73e_Out_2_Float;
Unity_Multiply_float_float(_Multiply_344852ef2f3546caa8c4f1f3695c3f0d_Out_2_Float, _Property_0943d2961c4c4e2ba15fdd94ac828776_Out_0_Float, _Multiply_4d6a0142972c48e889dfdbf32703e73e_Out_2_Float);
float3 _Add_b8880c59f8304f648cf8583ead4fb728_Out_2_Vector3;
Unity_Add_float3(IN.WorldSpacePosition, (_Multiply_4d6a0142972c48e889dfdbf32703e73e_Out_2_Float.xxx), _Add_b8880c59f8304f648cf8583ead4fb728_Out_2_Vector3);
float3 _Add_48a8fd066e7e49e4891d1ae97e03bb65_Out_2_Vector3;
Unity_Add_float3((_Multiply_91c05a4257804a449cb4304b1f563d42_Out_2_Float.xxx), _Add_b8880c59f8304f648cf8583ead4fb728_Out_2_Vector3, _Add_48a8fd066e7e49e4891d1ae97e03bb65_Out_2_Vector3);
float3 _UPGFoliageBendWSCustomFunction_dc96fa4b2cc8432d856f03b9268115fe_offsetWS_2_Vector3;
UPG_FoliageBendWS_float(IN.WorldSpacePosition, _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float, _UPGFoliageBendWSCustomFunction_dc96fa4b2cc8432d856f03b9268115fe_offsetWS_2_Vector3);
float3 _Add_c1034f24720f4c5f9634821d7037f0ae_Out_2_Vector3;
Unity_Add_float3(_Add_48a8fd066e7e49e4891d1ae97e03bb65_Out_2_Vector3, _UPGFoliageBendWSCustomFunction_dc96fa4b2cc8432d856f03b9268115fe_offsetWS_2_Vector3, _Add_c1034f24720f4c5f9634821d7037f0ae_Out_2_Vector3);
float3 _Transform_583ec64076274514891c3c1e7f9946aa_Out_1_Vector3;
_Transform_583ec64076274514891c3c1e7f9946aa_Out_1_Vector3 = TransformWorldToObject(_Add_c1034f24720f4c5f9634821d7037f0ae_Out_2_Vector3.xyz);
float3 _Vector3_c086b8a0e95a4c27b6fc4d4d3843bd3b_Out_0_Vector3 = float3(float(0), float(1), float(0));
description.Position = _Transform_583ec64076274514891c3c1e7f9946aa_Out_1_Vector3;
description.Normal = _Vector3_c086b8a0e95a4c27b6fc4d4d3843bd3b_Out_0_Vector3;
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
UnityTexture2D _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Base_Color, sampler_Base_Color, _Base_Color_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.tex, _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.samplerstate, _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4, _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_R_4_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.r;
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_G_5_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.g;
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_B_6_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.b;
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_A_7_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.a;
surface.Alpha = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_A_7_Float;
surface.AlphaClipThreshold = float(0.5);
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
    output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
    output.uv0 =                                        input.uv0;
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
// GraphKeywords: <None>

// Defines
#define _NORMALMAP 1
#define _NORMAL_DROPOFF_OS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_TEXCOORD1
#define ATTRIBUTES_NEED_TEXCOORD2
#define VARYINGS_NEED_POSITION_WS
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
 float3 WorldSpacePosition;
 float4 uv0;
 float3 TimeParameters;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 WorldSpacePosition;
 float4 uv0;
 float3 TimeParameters;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
 float4 texCoord0 : INTERP0;
 float3 positionWS : INTERP1;
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

float4 _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D_TexelSize;
float4 _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D_TexelSize;
float4 _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D_TexelSize;
float4 _Color_Tint_Bottom;
float2 _Cloud_Speed;
float _Emissive_Intenstiy_Clouds;
float _Wind_Large_Scale;
float _Terrain_Blend_Mul;
float _Cloud_Scale;
float2 _Wind_Large_Speed;
float _Wind_Small_Multiplier;
float4 _Cloud_Color;
float4 _Color_Tint_Top;
float4 _Base_Color_TexelSize;
float _Grass_Pivot_Lock_Strength;
float _Wind_Large_Multiplier;
float2 _Wind_Small_Speed;
float _Wind_Small_Scale;
float _Terrain_Blend_Pow;
float _Smoothness;
float4 _Emissive_Color;
float4 _Emissive_Clouds;
float _Gradience_Mul;
float _Gradience_Pow;
float _Emissive_Intenstiy;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D);
TEXTURE2D(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D);
TEXTURE2D(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D);
TEXTURE2D(_Base_Color);
SAMPLER(sampler_Base_Color);

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
#include_with_pragmas "Assets/06.Shaders/Foliage/UPG_FoliageInteraction.hlsl"

// Graph Functions

void Unity_Divide_float3(float3 A, float3 B, out float3 Out)
{
    Out = A / B;
}

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}

void Unity_Divide_float2(float2 A, float2 B, out float2 Out)
{
    Out = A / B;
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_Add_float2(float2 A, float2 B, out float2 Out)
{
    Out = A + B;
}

struct Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float
{
float3 WorldSpacePosition;
float3 TimeParameters;
};

void SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(float _Cloud_Scale, float2 _Cloud_Speed, Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float IN, out float2 OutVector2_1)
{
float _Property_da571d91c08d469b8fcdb0a2a1aa2cc1_Out_0_Float = _Cloud_Scale;
float3 _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3;
Unity_Divide_float3(IN.WorldSpacePosition, (_Property_da571d91c08d469b8fcdb0a2a1aa2cc1_Out_0_Float.xxx), _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3);
float _Split_c9bf902bb4344b6a837c002876a4a742_R_1_Float = _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3[0];
float _Split_c9bf902bb4344b6a837c002876a4a742_G_2_Float = _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3[1];
float _Split_c9bf902bb4344b6a837c002876a4a742_B_3_Float = _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3[2];
float _Split_c9bf902bb4344b6a837c002876a4a742_A_4_Float = 0;
float4 _Combine_b2458545efe84f8488335a6a0e9829f0_RGBA_4_Vector4;
float3 _Combine_b2458545efe84f8488335a6a0e9829f0_RGB_5_Vector3;
float2 _Combine_b2458545efe84f8488335a6a0e9829f0_RG_6_Vector2;
Unity_Combine_float(_Split_c9bf902bb4344b6a837c002876a4a742_R_1_Float, _Split_c9bf902bb4344b6a837c002876a4a742_B_3_Float, float(0), float(0), _Combine_b2458545efe84f8488335a6a0e9829f0_RGBA_4_Vector4, _Combine_b2458545efe84f8488335a6a0e9829f0_RGB_5_Vector3, _Combine_b2458545efe84f8488335a6a0e9829f0_RG_6_Vector2);
float2 _Property_36f0a343bbd443b0ba9ba96752bd5419_Out_0_Vector2 = _Cloud_Speed;
float2 _Divide_4d6b2552e0254de6a213dc9020221a26_Out_2_Vector2;
Unity_Divide_float2(_Property_36f0a343bbd443b0ba9ba96752bd5419_Out_0_Vector2, float2(100, 100), _Divide_4d6b2552e0254de6a213dc9020221a26_Out_2_Vector2);
float2 _Multiply_fb765c606df846c29196161ce8a5c5fe_Out_2_Vector2;
Unity_Multiply_float2_float2((IN.TimeParameters.x.xx), _Divide_4d6b2552e0254de6a213dc9020221a26_Out_2_Vector2, _Multiply_fb765c606df846c29196161ce8a5c5fe_Out_2_Vector2);
float2 _Add_4e43f841f4184513872472164ca37f4a_Out_2_Vector2;
Unity_Add_float2(_Combine_b2458545efe84f8488335a6a0e9829f0_RG_6_Vector2, _Multiply_fb765c606df846c29196161ce8a5c5fe_Out_2_Vector2, _Add_4e43f841f4184513872472164ca37f4a_Out_2_Vector2);
OutVector2_1 = _Add_4e43f841f4184513872472164ca37f4a_Out_2_Vector2;
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Power_float(float A, float B, out float Out)
{
    Out = pow(A, B);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
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
float _Property_4116819b6d874ed78027c7f0b237cb76_Out_0_Float = _Wind_Large_Scale;
float2 _Property_7855d6ce09864024b8c68f043f8cbdb4_Out_0_Vector2 = _Wind_Large_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45;
_PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_4116819b6d874ed78027c7f0b237cb76_Out_0_Float, _Property_7855d6ce09864024b8c68f043f8cbdb4_Out_0_Vector2, _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45, _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_R_5_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_G_6_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_B_7_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_A_8_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.a;
float _Subtract_ab6f6b551d064bb8b607c9a9bc381611_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_R_5_Float, float(0.5), _Subtract_ab6f6b551d064bb8b607c9a9bc381611_Out_2_Float);
float4 _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4 = IN.uv0;
float _Split_6ffb9a554e6e4ede9196adfc250975db_R_1_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[0];
float _Split_6ffb9a554e6e4ede9196adfc250975db_G_2_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[1];
float _Split_6ffb9a554e6e4ede9196adfc250975db_B_3_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[2];
float _Split_6ffb9a554e6e4ede9196adfc250975db_A_4_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[3];
float _Property_6eb67bc74e6b4fb288d6164ab2c7cdd8_Out_0_Float = _Grass_Pivot_Lock_Strength;
float _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float;
Unity_Power_float(_Split_6ffb9a554e6e4ede9196adfc250975db_G_2_Float, _Property_6eb67bc74e6b4fb288d6164ab2c7cdd8_Out_0_Float, _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float);
float _Multiply_219e592917cc4b659707ce8e75b39d9e_Out_2_Float;
Unity_Multiply_float_float(_Subtract_ab6f6b551d064bb8b607c9a9bc381611_Out_2_Float, _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float, _Multiply_219e592917cc4b659707ce8e75b39d9e_Out_2_Float);
float _Property_20c86c0cde6c4232a79793b50e170771_Out_0_Float = _Wind_Large_Multiplier;
float _Multiply_91c05a4257804a449cb4304b1f563d42_Out_2_Float;
Unity_Multiply_float_float(_Multiply_219e592917cc4b659707ce8e75b39d9e_Out_2_Float, _Property_20c86c0cde6c4232a79793b50e170771_Out_0_Float, _Multiply_91c05a4257804a449cb4304b1f563d42_Out_2_Float);
float _Property_fe4ba3b7d8f14c98b41d8948b19f735c_Out_0_Float = _Wind_Small_Scale;
float2 _Property_793e0429eb224e7694ee6cb87c6f31b9_Out_0_Vector2 = _Wind_Small_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3;
_PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_fe4ba3b7d8f14c98b41d8948b19f735c_Out_0_Float, _Property_793e0429eb224e7694ee6cb87c6f31b9_Out_0_Vector2, _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3, _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_R_5_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_G_6_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_B_7_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_A_8_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.a;
float _Subtract_95ef509ca9e2441ca6e08508d02ef96e_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_R_5_Float, float(0.5), _Subtract_95ef509ca9e2441ca6e08508d02ef96e_Out_2_Float);
float _Multiply_344852ef2f3546caa8c4f1f3695c3f0d_Out_2_Float;
Unity_Multiply_float_float(_Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float, _Subtract_95ef509ca9e2441ca6e08508d02ef96e_Out_2_Float, _Multiply_344852ef2f3546caa8c4f1f3695c3f0d_Out_2_Float);
float _Property_0943d2961c4c4e2ba15fdd94ac828776_Out_0_Float = _Wind_Small_Multiplier;
float _Multiply_4d6a0142972c48e889dfdbf32703e73e_Out_2_Float;
Unity_Multiply_float_float(_Multiply_344852ef2f3546caa8c4f1f3695c3f0d_Out_2_Float, _Property_0943d2961c4c4e2ba15fdd94ac828776_Out_0_Float, _Multiply_4d6a0142972c48e889dfdbf32703e73e_Out_2_Float);
float3 _Add_b8880c59f8304f648cf8583ead4fb728_Out_2_Vector3;
Unity_Add_float3(IN.WorldSpacePosition, (_Multiply_4d6a0142972c48e889dfdbf32703e73e_Out_2_Float.xxx), _Add_b8880c59f8304f648cf8583ead4fb728_Out_2_Vector3);
float3 _Add_48a8fd066e7e49e4891d1ae97e03bb65_Out_2_Vector3;
Unity_Add_float3((_Multiply_91c05a4257804a449cb4304b1f563d42_Out_2_Float.xxx), _Add_b8880c59f8304f648cf8583ead4fb728_Out_2_Vector3, _Add_48a8fd066e7e49e4891d1ae97e03bb65_Out_2_Vector3);
float3 _UPGFoliageBendWSCustomFunction_dc96fa4b2cc8432d856f03b9268115fe_offsetWS_2_Vector3;
UPG_FoliageBendWS_float(IN.WorldSpacePosition, _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float, _UPGFoliageBendWSCustomFunction_dc96fa4b2cc8432d856f03b9268115fe_offsetWS_2_Vector3);
float3 _Add_c1034f24720f4c5f9634821d7037f0ae_Out_2_Vector3;
Unity_Add_float3(_Add_48a8fd066e7e49e4891d1ae97e03bb65_Out_2_Vector3, _UPGFoliageBendWSCustomFunction_dc96fa4b2cc8432d856f03b9268115fe_offsetWS_2_Vector3, _Add_c1034f24720f4c5f9634821d7037f0ae_Out_2_Vector3);
float3 _Transform_583ec64076274514891c3c1e7f9946aa_Out_1_Vector3;
_Transform_583ec64076274514891c3c1e7f9946aa_Out_1_Vector3 = TransformWorldToObject(_Add_c1034f24720f4c5f9634821d7037f0ae_Out_2_Vector3.xyz);
float3 _Vector3_c086b8a0e95a4c27b6fc4d4d3843bd3b_Out_0_Vector3 = float3(float(0), float(1), float(0));
description.Position = _Transform_583ec64076274514891c3c1e7f9946aa_Out_1_Vector3;
description.Normal = _Vector3_c086b8a0e95a4c27b6fc4d4d3843bd3b_Out_0_Vector3;
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
float4 _Property_80081465fc9e42c78d044b56c43e5e1f_Out_0_Vector4 = _Color_Tint_Bottom;
float4 _Property_39c423b8c80d4d97946dc0b2c79b23c9_Out_0_Vector4 = _Color_Tint_Top;
float4 _UV_0a040733d8e9420d9054f7edb74c7682_Out_0_Vector4 = IN.uv0;
float _Split_e789d7c2ff9f4972bdb3462091aa8bfb_R_1_Float = _UV_0a040733d8e9420d9054f7edb74c7682_Out_0_Vector4[0];
float _Split_e789d7c2ff9f4972bdb3462091aa8bfb_G_2_Float = _UV_0a040733d8e9420d9054f7edb74c7682_Out_0_Vector4[1];
float _Split_e789d7c2ff9f4972bdb3462091aa8bfb_B_3_Float = _UV_0a040733d8e9420d9054f7edb74c7682_Out_0_Vector4[2];
float _Split_e789d7c2ff9f4972bdb3462091aa8bfb_A_4_Float = _UV_0a040733d8e9420d9054f7edb74c7682_Out_0_Vector4[3];
float _Property_5bf30ba4b92c4b7d81755e8764fdd331_Out_0_Float = _Gradience_Mul;
float _Multiply_0e8bd3c9e0bb453f8ddc49e301bbbe74_Out_2_Float;
Unity_Multiply_float_float(_Split_e789d7c2ff9f4972bdb3462091aa8bfb_G_2_Float, _Property_5bf30ba4b92c4b7d81755e8764fdd331_Out_0_Float, _Multiply_0e8bd3c9e0bb453f8ddc49e301bbbe74_Out_2_Float);
float _Property_92651d8cf9f8486f97f96477fee69822_Out_0_Float = _Gradience_Pow;
float _Power_21a1ac3e3c704dc2a230b301a4fd1bc3_Out_2_Float;
Unity_Power_float(_Multiply_0e8bd3c9e0bb453f8ddc49e301bbbe74_Out_2_Float, _Property_92651d8cf9f8486f97f96477fee69822_Out_0_Float, _Power_21a1ac3e3c704dc2a230b301a4fd1bc3_Out_2_Float);
float _Saturate_df6319cd6a8f44b8a2f98a45b86570e3_Out_1_Float;
Unity_Saturate_float(_Power_21a1ac3e3c704dc2a230b301a4fd1bc3_Out_2_Float, _Saturate_df6319cd6a8f44b8a2f98a45b86570e3_Out_1_Float);
float4 _Lerp_aef25ae41dbd4bb899f648885331bf52_Out_3_Vector4;
Unity_Lerp_float4(_Property_80081465fc9e42c78d044b56c43e5e1f_Out_0_Vector4, _Property_39c423b8c80d4d97946dc0b2c79b23c9_Out_0_Vector4, (_Saturate_df6319cd6a8f44b8a2f98a45b86570e3_Out_1_Float.xxxx), _Lerp_aef25ae41dbd4bb899f648885331bf52_Out_3_Vector4);
UnityTexture2D _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Base_Color, sampler_Base_Color, _Base_Color_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.tex, _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.samplerstate, _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4, _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_R_4_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.r;
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_G_5_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.g;
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_B_6_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.b;
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_A_7_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.a;
float4 _Multiply_fcd43956ee594716aa91c7691ce699cf_Out_2_Vector4;
Unity_Multiply_float4_float4(_Lerp_aef25ae41dbd4bb899f648885331bf52_Out_3_Vector4, _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4, _Multiply_fcd43956ee594716aa91c7691ce699cf_Out_2_Vector4);
float4 _Property_1d0cacb8faa34e6580302336f32b8591_Out_0_Vector4 = _Cloud_Color;
float4 _Multiply_17a3d0b90ebb4b1b9cab04c57f32aed2_Out_2_Vector4;
Unity_Multiply_float4_float4(_Multiply_fcd43956ee594716aa91c7691ce699cf_Out_2_Vector4, _Property_1d0cacb8faa34e6580302336f32b8591_Out_0_Vector4, _Multiply_17a3d0b90ebb4b1b9cab04c57f32aed2_Out_2_Vector4);
float _Property_8a8a73164ab1462fbb02778830bd9a39_Out_0_Float = _Cloud_Scale;
float2 _Property_b7fc6981bb03499fb69f338ec3fe9525_Out_0_Vector2 = _Cloud_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_dc86dca30b514354a0931aeb1998f16e;
_PositionalUVs_dc86dca30b514354a0931aeb1998f16e.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_dc86dca30b514354a0931aeb1998f16e.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_dc86dca30b514354a0931aeb1998f16e_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_8a8a73164ab1462fbb02778830bd9a39_Out_0_Float, _Property_b7fc6981bb03499fb69f338ec3fe9525_Out_0_Vector2, _PositionalUVs_dc86dca30b514354a0931aeb1998f16e, _PositionalUVs_dc86dca30b514354a0931aeb1998f16e_OutVector2_1_Vector2);
float4 _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(UnityBuildTexture2DStructNoScale(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_dc86dca30b514354a0931aeb1998f16e_OutVector2_1_Vector2) );
if (UnityBuildTexture2DStructNoScale(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D).hdrDecode.x > 0)
_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4, UnityBuildTexture2DStructNoScale(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D).hdrDecode);
float _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_R_4_Float = _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4.r;
float _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_G_5_Float = _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4.g;
float _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_B_6_Float = _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4.b;
float _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_A_7_Float = _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_RGBA_0_Vector4.a;
float4 _Lerp_3bb853efd4d6410a8b63501c276cee08_Out_3_Vector4;
Unity_Lerp_float4(_Multiply_fcd43956ee594716aa91c7691ce699cf_Out_2_Vector4, _Multiply_17a3d0b90ebb4b1b9cab04c57f32aed2_Out_2_Vector4, (_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_R_4_Float.xxxx), _Lerp_3bb853efd4d6410a8b63501c276cee08_Out_3_Vector4);
float _Property_1336c01c0e674957b651c4aa288cb981_Out_0_Float = _Emissive_Intenstiy;
float _Property_f2ab9978dd8748fc98c66f03d7d2d5d4_Out_0_Float = _Emissive_Intenstiy_Clouds;
float _Lerp_b7e37fa8b0714a3b953fa8ba9bfe3919_Out_3_Float;
Unity_Lerp_float(_Property_1336c01c0e674957b651c4aa288cb981_Out_0_Float, _Property_f2ab9978dd8748fc98c66f03d7d2d5d4_Out_0_Float, _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_R_4_Float, _Lerp_b7e37fa8b0714a3b953fa8ba9bfe3919_Out_3_Float);
float4 _Multiply_c9eb85122f184daa97bd477002a538c5_Out_2_Vector4;
Unity_Multiply_float4_float4(_Lerp_3bb853efd4d6410a8b63501c276cee08_Out_3_Vector4, (_Lerp_b7e37fa8b0714a3b953fa8ba9bfe3919_Out_3_Float.xxxx), _Multiply_c9eb85122f184daa97bd477002a538c5_Out_2_Vector4);
surface.BaseColor = (_Lerp_3bb853efd4d6410a8b63501c276cee08_Out_3_Vector4.xyz);
surface.Emission = (_Multiply_c9eb85122f184daa97bd477002a538c5_Out_2_Vector4.xyz);
surface.Alpha = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_A_7_Float;
surface.AlphaClipThreshold = float(0.5);
float3 reactionUnusedColor = 0, reactionUnusedEmission = 0;
ClipVegetationFuel(IN.WorldSpacePosition);
ApplyVegetationReaction(IN.WorldSpacePosition,surface.BaseColor,surface.Emission);
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
    output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
    output.uv0 =                                        input.uv0;
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

    





    output.WorldSpacePosition = input.positionWS;

    #if UNITY_UV_STARTS_AT_TOP
    #else
    #endif


    output.uv0 = input.texCoord0;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
    output.TimeParameters = _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
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
// GraphKeywords: <None>

// Defines
#define _NORMALMAP 1
#define _NORMAL_DROPOFF_OS 1
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
 float3 WorldSpacePosition;
 float4 uv0;
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

float4 _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D_TexelSize;
float4 _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D_TexelSize;
float4 _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D_TexelSize;
float4 _Color_Tint_Bottom;
float2 _Cloud_Speed;
float _Emissive_Intenstiy_Clouds;
float _Wind_Large_Scale;
float _Terrain_Blend_Mul;
float _Cloud_Scale;
float2 _Wind_Large_Speed;
float _Wind_Small_Multiplier;
float4 _Cloud_Color;
float4 _Color_Tint_Top;
float4 _Base_Color_TexelSize;
float _Grass_Pivot_Lock_Strength;
float _Wind_Large_Multiplier;
float2 _Wind_Small_Speed;
float _Wind_Small_Scale;
float _Terrain_Blend_Pow;
float _Smoothness;
float4 _Emissive_Color;
float4 _Emissive_Clouds;
float _Gradience_Mul;
float _Gradience_Pow;
float _Emissive_Intenstiy;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D);
TEXTURE2D(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D);
TEXTURE2D(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D);
TEXTURE2D(_Base_Color);
SAMPLER(sampler_Base_Color);

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
#include_with_pragmas "Assets/06.Shaders/Foliage/UPG_FoliageInteraction.hlsl"

// Graph Functions

void Unity_Divide_float3(float3 A, float3 B, out float3 Out)
{
    Out = A / B;
}

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}

void Unity_Divide_float2(float2 A, float2 B, out float2 Out)
{
    Out = A / B;
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_Add_float2(float2 A, float2 B, out float2 Out)
{
    Out = A + B;
}

struct Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float
{
float3 WorldSpacePosition;
float3 TimeParameters;
};

void SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(float _Cloud_Scale, float2 _Cloud_Speed, Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float IN, out float2 OutVector2_1)
{
float _Property_da571d91c08d469b8fcdb0a2a1aa2cc1_Out_0_Float = _Cloud_Scale;
float3 _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3;
Unity_Divide_float3(IN.WorldSpacePosition, (_Property_da571d91c08d469b8fcdb0a2a1aa2cc1_Out_0_Float.xxx), _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3);
float _Split_c9bf902bb4344b6a837c002876a4a742_R_1_Float = _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3[0];
float _Split_c9bf902bb4344b6a837c002876a4a742_G_2_Float = _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3[1];
float _Split_c9bf902bb4344b6a837c002876a4a742_B_3_Float = _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3[2];
float _Split_c9bf902bb4344b6a837c002876a4a742_A_4_Float = 0;
float4 _Combine_b2458545efe84f8488335a6a0e9829f0_RGBA_4_Vector4;
float3 _Combine_b2458545efe84f8488335a6a0e9829f0_RGB_5_Vector3;
float2 _Combine_b2458545efe84f8488335a6a0e9829f0_RG_6_Vector2;
Unity_Combine_float(_Split_c9bf902bb4344b6a837c002876a4a742_R_1_Float, _Split_c9bf902bb4344b6a837c002876a4a742_B_3_Float, float(0), float(0), _Combine_b2458545efe84f8488335a6a0e9829f0_RGBA_4_Vector4, _Combine_b2458545efe84f8488335a6a0e9829f0_RGB_5_Vector3, _Combine_b2458545efe84f8488335a6a0e9829f0_RG_6_Vector2);
float2 _Property_36f0a343bbd443b0ba9ba96752bd5419_Out_0_Vector2 = _Cloud_Speed;
float2 _Divide_4d6b2552e0254de6a213dc9020221a26_Out_2_Vector2;
Unity_Divide_float2(_Property_36f0a343bbd443b0ba9ba96752bd5419_Out_0_Vector2, float2(100, 100), _Divide_4d6b2552e0254de6a213dc9020221a26_Out_2_Vector2);
float2 _Multiply_fb765c606df846c29196161ce8a5c5fe_Out_2_Vector2;
Unity_Multiply_float2_float2((IN.TimeParameters.x.xx), _Divide_4d6b2552e0254de6a213dc9020221a26_Out_2_Vector2, _Multiply_fb765c606df846c29196161ce8a5c5fe_Out_2_Vector2);
float2 _Add_4e43f841f4184513872472164ca37f4a_Out_2_Vector2;
Unity_Add_float2(_Combine_b2458545efe84f8488335a6a0e9829f0_RG_6_Vector2, _Multiply_fb765c606df846c29196161ce8a5c5fe_Out_2_Vector2, _Add_4e43f841f4184513872472164ca37f4a_Out_2_Vector2);
OutVector2_1 = _Add_4e43f841f4184513872472164ca37f4a_Out_2_Vector2;
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Power_float(float A, float B, out float Out)
{
    Out = pow(A, B);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
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
float _Property_4116819b6d874ed78027c7f0b237cb76_Out_0_Float = _Wind_Large_Scale;
float2 _Property_7855d6ce09864024b8c68f043f8cbdb4_Out_0_Vector2 = _Wind_Large_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45;
_PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_4116819b6d874ed78027c7f0b237cb76_Out_0_Float, _Property_7855d6ce09864024b8c68f043f8cbdb4_Out_0_Vector2, _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45, _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_R_5_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_G_6_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_B_7_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_A_8_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.a;
float _Subtract_ab6f6b551d064bb8b607c9a9bc381611_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_R_5_Float, float(0.5), _Subtract_ab6f6b551d064bb8b607c9a9bc381611_Out_2_Float);
float4 _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4 = IN.uv0;
float _Split_6ffb9a554e6e4ede9196adfc250975db_R_1_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[0];
float _Split_6ffb9a554e6e4ede9196adfc250975db_G_2_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[1];
float _Split_6ffb9a554e6e4ede9196adfc250975db_B_3_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[2];
float _Split_6ffb9a554e6e4ede9196adfc250975db_A_4_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[3];
float _Property_6eb67bc74e6b4fb288d6164ab2c7cdd8_Out_0_Float = _Grass_Pivot_Lock_Strength;
float _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float;
Unity_Power_float(_Split_6ffb9a554e6e4ede9196adfc250975db_G_2_Float, _Property_6eb67bc74e6b4fb288d6164ab2c7cdd8_Out_0_Float, _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float);
float _Multiply_219e592917cc4b659707ce8e75b39d9e_Out_2_Float;
Unity_Multiply_float_float(_Subtract_ab6f6b551d064bb8b607c9a9bc381611_Out_2_Float, _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float, _Multiply_219e592917cc4b659707ce8e75b39d9e_Out_2_Float);
float _Property_20c86c0cde6c4232a79793b50e170771_Out_0_Float = _Wind_Large_Multiplier;
float _Multiply_91c05a4257804a449cb4304b1f563d42_Out_2_Float;
Unity_Multiply_float_float(_Multiply_219e592917cc4b659707ce8e75b39d9e_Out_2_Float, _Property_20c86c0cde6c4232a79793b50e170771_Out_0_Float, _Multiply_91c05a4257804a449cb4304b1f563d42_Out_2_Float);
float _Property_fe4ba3b7d8f14c98b41d8948b19f735c_Out_0_Float = _Wind_Small_Scale;
float2 _Property_793e0429eb224e7694ee6cb87c6f31b9_Out_0_Vector2 = _Wind_Small_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3;
_PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_fe4ba3b7d8f14c98b41d8948b19f735c_Out_0_Float, _Property_793e0429eb224e7694ee6cb87c6f31b9_Out_0_Vector2, _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3, _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_R_5_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_G_6_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_B_7_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_A_8_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.a;
float _Subtract_95ef509ca9e2441ca6e08508d02ef96e_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_R_5_Float, float(0.5), _Subtract_95ef509ca9e2441ca6e08508d02ef96e_Out_2_Float);
float _Multiply_344852ef2f3546caa8c4f1f3695c3f0d_Out_2_Float;
Unity_Multiply_float_float(_Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float, _Subtract_95ef509ca9e2441ca6e08508d02ef96e_Out_2_Float, _Multiply_344852ef2f3546caa8c4f1f3695c3f0d_Out_2_Float);
float _Property_0943d2961c4c4e2ba15fdd94ac828776_Out_0_Float = _Wind_Small_Multiplier;
float _Multiply_4d6a0142972c48e889dfdbf32703e73e_Out_2_Float;
Unity_Multiply_float_float(_Multiply_344852ef2f3546caa8c4f1f3695c3f0d_Out_2_Float, _Property_0943d2961c4c4e2ba15fdd94ac828776_Out_0_Float, _Multiply_4d6a0142972c48e889dfdbf32703e73e_Out_2_Float);
float3 _Add_b8880c59f8304f648cf8583ead4fb728_Out_2_Vector3;
Unity_Add_float3(IN.WorldSpacePosition, (_Multiply_4d6a0142972c48e889dfdbf32703e73e_Out_2_Float.xxx), _Add_b8880c59f8304f648cf8583ead4fb728_Out_2_Vector3);
float3 _Add_48a8fd066e7e49e4891d1ae97e03bb65_Out_2_Vector3;
Unity_Add_float3((_Multiply_91c05a4257804a449cb4304b1f563d42_Out_2_Float.xxx), _Add_b8880c59f8304f648cf8583ead4fb728_Out_2_Vector3, _Add_48a8fd066e7e49e4891d1ae97e03bb65_Out_2_Vector3);
float3 _UPGFoliageBendWSCustomFunction_dc96fa4b2cc8432d856f03b9268115fe_offsetWS_2_Vector3;
UPG_FoliageBendWS_float(IN.WorldSpacePosition, _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float, _UPGFoliageBendWSCustomFunction_dc96fa4b2cc8432d856f03b9268115fe_offsetWS_2_Vector3);
float3 _Add_c1034f24720f4c5f9634821d7037f0ae_Out_2_Vector3;
Unity_Add_float3(_Add_48a8fd066e7e49e4891d1ae97e03bb65_Out_2_Vector3, _UPGFoliageBendWSCustomFunction_dc96fa4b2cc8432d856f03b9268115fe_offsetWS_2_Vector3, _Add_c1034f24720f4c5f9634821d7037f0ae_Out_2_Vector3);
float3 _Transform_583ec64076274514891c3c1e7f9946aa_Out_1_Vector3;
_Transform_583ec64076274514891c3c1e7f9946aa_Out_1_Vector3 = TransformWorldToObject(_Add_c1034f24720f4c5f9634821d7037f0ae_Out_2_Vector3.xyz);
float3 _Vector3_c086b8a0e95a4c27b6fc4d4d3843bd3b_Out_0_Vector3 = float3(float(0), float(1), float(0));
description.Position = _Transform_583ec64076274514891c3c1e7f9946aa_Out_1_Vector3;
description.Normal = _Vector3_c086b8a0e95a4c27b6fc4d4d3843bd3b_Out_0_Vector3;
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
UnityTexture2D _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Base_Color, sampler_Base_Color, _Base_Color_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.tex, _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.samplerstate, _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4, _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_R_4_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.r;
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_G_5_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.g;
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_B_6_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.b;
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_A_7_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.a;
surface.Alpha = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_A_7_Float;
surface.AlphaClipThreshold = float(0.5);
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
    output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
    output.uv0 =                                        input.uv0;
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
// GraphKeywords: <None>

// Defines
#define _NORMALMAP 1
#define _NORMAL_DROPOFF_OS 1
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
 float3 WorldSpacePosition;
 float4 uv0;
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

float4 _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D_TexelSize;
float4 _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D_TexelSize;
float4 _SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D_TexelSize;
float4 _Color_Tint_Bottom;
float2 _Cloud_Speed;
float _Emissive_Intenstiy_Clouds;
float _Wind_Large_Scale;
float _Terrain_Blend_Mul;
float _Cloud_Scale;
float2 _Wind_Large_Speed;
float _Wind_Small_Multiplier;
float4 _Cloud_Color;
float4 _Color_Tint_Top;
float4 _Base_Color_TexelSize;
float _Grass_Pivot_Lock_Strength;
float _Wind_Large_Multiplier;
float2 _Wind_Small_Speed;
float _Wind_Small_Scale;
float _Terrain_Blend_Pow;
float _Smoothness;
float4 _Emissive_Color;
float4 _Emissive_Clouds;
float _Gradience_Mul;
float _Gradience_Pow;
float _Emissive_Intenstiy;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D);
TEXTURE2D(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D);
TEXTURE2D(_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2D_c59e2d0f76504489a1ba208b96c41c19_Texture_1_Texture2D);
TEXTURE2D(_Base_Color);
SAMPLER(sampler_Base_Color);

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
#include_with_pragmas "Assets/06.Shaders/Foliage/UPG_FoliageInteraction.hlsl"

// Graph Functions

void Unity_Divide_float3(float3 A, float3 B, out float3 Out)
{
    Out = A / B;
}

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}

void Unity_Divide_float2(float2 A, float2 B, out float2 Out)
{
    Out = A / B;
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_Add_float2(float2 A, float2 B, out float2 Out)
{
    Out = A + B;
}

struct Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float
{
float3 WorldSpacePosition;
float3 TimeParameters;
};

void SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(float _Cloud_Scale, float2 _Cloud_Speed, Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float IN, out float2 OutVector2_1)
{
float _Property_da571d91c08d469b8fcdb0a2a1aa2cc1_Out_0_Float = _Cloud_Scale;
float3 _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3;
Unity_Divide_float3(IN.WorldSpacePosition, (_Property_da571d91c08d469b8fcdb0a2a1aa2cc1_Out_0_Float.xxx), _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3);
float _Split_c9bf902bb4344b6a837c002876a4a742_R_1_Float = _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3[0];
float _Split_c9bf902bb4344b6a837c002876a4a742_G_2_Float = _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3[1];
float _Split_c9bf902bb4344b6a837c002876a4a742_B_3_Float = _Divide_e508b98c49904ca19d3959c3d4b0116e_Out_2_Vector3[2];
float _Split_c9bf902bb4344b6a837c002876a4a742_A_4_Float = 0;
float4 _Combine_b2458545efe84f8488335a6a0e9829f0_RGBA_4_Vector4;
float3 _Combine_b2458545efe84f8488335a6a0e9829f0_RGB_5_Vector3;
float2 _Combine_b2458545efe84f8488335a6a0e9829f0_RG_6_Vector2;
Unity_Combine_float(_Split_c9bf902bb4344b6a837c002876a4a742_R_1_Float, _Split_c9bf902bb4344b6a837c002876a4a742_B_3_Float, float(0), float(0), _Combine_b2458545efe84f8488335a6a0e9829f0_RGBA_4_Vector4, _Combine_b2458545efe84f8488335a6a0e9829f0_RGB_5_Vector3, _Combine_b2458545efe84f8488335a6a0e9829f0_RG_6_Vector2);
float2 _Property_36f0a343bbd443b0ba9ba96752bd5419_Out_0_Vector2 = _Cloud_Speed;
float2 _Divide_4d6b2552e0254de6a213dc9020221a26_Out_2_Vector2;
Unity_Divide_float2(_Property_36f0a343bbd443b0ba9ba96752bd5419_Out_0_Vector2, float2(100, 100), _Divide_4d6b2552e0254de6a213dc9020221a26_Out_2_Vector2);
float2 _Multiply_fb765c606df846c29196161ce8a5c5fe_Out_2_Vector2;
Unity_Multiply_float2_float2((IN.TimeParameters.x.xx), _Divide_4d6b2552e0254de6a213dc9020221a26_Out_2_Vector2, _Multiply_fb765c606df846c29196161ce8a5c5fe_Out_2_Vector2);
float2 _Add_4e43f841f4184513872472164ca37f4a_Out_2_Vector2;
Unity_Add_float2(_Combine_b2458545efe84f8488335a6a0e9829f0_RG_6_Vector2, _Multiply_fb765c606df846c29196161ce8a5c5fe_Out_2_Vector2, _Add_4e43f841f4184513872472164ca37f4a_Out_2_Vector2);
OutVector2_1 = _Add_4e43f841f4184513872472164ca37f4a_Out_2_Vector2;
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Power_float(float A, float B, out float Out)
{
    Out = pow(A, B);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
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
float _Property_4116819b6d874ed78027c7f0b237cb76_Out_0_Float = _Wind_Large_Scale;
float2 _Property_7855d6ce09864024b8c68f043f8cbdb4_Out_0_Vector2 = _Wind_Large_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45;
_PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_4116819b6d874ed78027c7f0b237cb76_Out_0_Float, _Property_7855d6ce09864024b8c68f043f8cbdb4_Out_0_Vector2, _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45, _PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_ef8065070e4846afb22db2e1c8cb3e45_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_R_5_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_G_6_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_B_7_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_A_8_Float = _SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_RGBA_0_Vector4.a;
float _Subtract_ab6f6b551d064bb8b607c9a9bc381611_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_307c4dddc74d4b238b11ed5a38473445_R_5_Float, float(0.5), _Subtract_ab6f6b551d064bb8b607c9a9bc381611_Out_2_Float);
float4 _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4 = IN.uv0;
float _Split_6ffb9a554e6e4ede9196adfc250975db_R_1_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[0];
float _Split_6ffb9a554e6e4ede9196adfc250975db_G_2_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[1];
float _Split_6ffb9a554e6e4ede9196adfc250975db_B_3_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[2];
float _Split_6ffb9a554e6e4ede9196adfc250975db_A_4_Float = _UV_46486cd619024857bf075d7a4b862f70_Out_0_Vector4[3];
float _Property_6eb67bc74e6b4fb288d6164ab2c7cdd8_Out_0_Float = _Grass_Pivot_Lock_Strength;
float _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float;
Unity_Power_float(_Split_6ffb9a554e6e4ede9196adfc250975db_G_2_Float, _Property_6eb67bc74e6b4fb288d6164ab2c7cdd8_Out_0_Float, _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float);
float _Multiply_219e592917cc4b659707ce8e75b39d9e_Out_2_Float;
Unity_Multiply_float_float(_Subtract_ab6f6b551d064bb8b607c9a9bc381611_Out_2_Float, _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float, _Multiply_219e592917cc4b659707ce8e75b39d9e_Out_2_Float);
float _Property_20c86c0cde6c4232a79793b50e170771_Out_0_Float = _Wind_Large_Multiplier;
float _Multiply_91c05a4257804a449cb4304b1f563d42_Out_2_Float;
Unity_Multiply_float_float(_Multiply_219e592917cc4b659707ce8e75b39d9e_Out_2_Float, _Property_20c86c0cde6c4232a79793b50e170771_Out_0_Float, _Multiply_91c05a4257804a449cb4304b1f563d42_Out_2_Float);
float _Property_fe4ba3b7d8f14c98b41d8948b19f735c_Out_0_Float = _Wind_Small_Scale;
float2 _Property_793e0429eb224e7694ee6cb87c6f31b9_Out_0_Vector2 = _Wind_Small_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3;
_PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_fe4ba3b7d8f14c98b41d8948b19f735c_Out_0_Float, _Property_793e0429eb224e7694ee6cb87c6f31b9_Out_0_Vector2, _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3, _PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_421b40acdf5d4067bb58b0f014aef4a3_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_R_5_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_G_6_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_B_7_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_A_8_Float = _SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_RGBA_0_Vector4.a;
float _Subtract_95ef509ca9e2441ca6e08508d02ef96e_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_b6680ed9fba74708b41a2ac8be5bda61_R_5_Float, float(0.5), _Subtract_95ef509ca9e2441ca6e08508d02ef96e_Out_2_Float);
float _Multiply_344852ef2f3546caa8c4f1f3695c3f0d_Out_2_Float;
Unity_Multiply_float_float(_Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float, _Subtract_95ef509ca9e2441ca6e08508d02ef96e_Out_2_Float, _Multiply_344852ef2f3546caa8c4f1f3695c3f0d_Out_2_Float);
float _Property_0943d2961c4c4e2ba15fdd94ac828776_Out_0_Float = _Wind_Small_Multiplier;
float _Multiply_4d6a0142972c48e889dfdbf32703e73e_Out_2_Float;
Unity_Multiply_float_float(_Multiply_344852ef2f3546caa8c4f1f3695c3f0d_Out_2_Float, _Property_0943d2961c4c4e2ba15fdd94ac828776_Out_0_Float, _Multiply_4d6a0142972c48e889dfdbf32703e73e_Out_2_Float);
float3 _Add_b8880c59f8304f648cf8583ead4fb728_Out_2_Vector3;
Unity_Add_float3(IN.WorldSpacePosition, (_Multiply_4d6a0142972c48e889dfdbf32703e73e_Out_2_Float.xxx), _Add_b8880c59f8304f648cf8583ead4fb728_Out_2_Vector3);
float3 _Add_48a8fd066e7e49e4891d1ae97e03bb65_Out_2_Vector3;
Unity_Add_float3((_Multiply_91c05a4257804a449cb4304b1f563d42_Out_2_Float.xxx), _Add_b8880c59f8304f648cf8583ead4fb728_Out_2_Vector3, _Add_48a8fd066e7e49e4891d1ae97e03bb65_Out_2_Vector3);
float3 _UPGFoliageBendWSCustomFunction_dc96fa4b2cc8432d856f03b9268115fe_offsetWS_2_Vector3;
UPG_FoliageBendWS_float(IN.WorldSpacePosition, _Power_073c4df92d1a438bb28309551eaf49e1_Out_2_Float, _UPGFoliageBendWSCustomFunction_dc96fa4b2cc8432d856f03b9268115fe_offsetWS_2_Vector3);
float3 _Add_c1034f24720f4c5f9634821d7037f0ae_Out_2_Vector3;
Unity_Add_float3(_Add_48a8fd066e7e49e4891d1ae97e03bb65_Out_2_Vector3, _UPGFoliageBendWSCustomFunction_dc96fa4b2cc8432d856f03b9268115fe_offsetWS_2_Vector3, _Add_c1034f24720f4c5f9634821d7037f0ae_Out_2_Vector3);
float3 _Transform_583ec64076274514891c3c1e7f9946aa_Out_1_Vector3;
_Transform_583ec64076274514891c3c1e7f9946aa_Out_1_Vector3 = TransformWorldToObject(_Add_c1034f24720f4c5f9634821d7037f0ae_Out_2_Vector3.xyz);
float3 _Vector3_c086b8a0e95a4c27b6fc4d4d3843bd3b_Out_0_Vector3 = float3(float(0), float(1), float(0));
description.Position = _Transform_583ec64076274514891c3c1e7f9946aa_Out_1_Vector3;
description.Normal = _Vector3_c086b8a0e95a4c27b6fc4d4d3843bd3b_Out_0_Vector3;
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
UnityTexture2D _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Base_Color, sampler_Base_Color, _Base_Color_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.tex, _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.samplerstate, _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4, _Property_28ce86611b7c4b79829b9c5764ae76eb_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_R_4_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.r;
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_G_5_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.g;
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_B_6_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.b;
float _SampleTexture2D_459bc13736f944e18971d0902c1b921c_A_7_Float = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_RGBA_0_Vector4.a;
surface.Alpha = _SampleTexture2D_459bc13736f944e18971d0902c1b921c_A_7_Float;
surface.AlphaClipThreshold = float(0.5);
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
    output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
    output.uv0 =                                        input.uv0;
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