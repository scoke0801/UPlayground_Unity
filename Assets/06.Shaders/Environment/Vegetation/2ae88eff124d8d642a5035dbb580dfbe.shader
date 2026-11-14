Shader "UPlayGround/Environment/Vegetation/2ae88eff124d8d642a5035dbb580dfbe"
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

_Wind_Large_Scale("Wind Large Scale", Float) = 60
_Wind_Large_Speed("Wind Large Speed", Vector, 2) = (0.1, 2, 0, 0)
_Wind_Large_Intensity("Wind Large Intensity", Float) = 0.6
_Wind_Small_Scale("Wind Small Scale", Float) = 40
_Wind_Small_Speed("Wind Small Speed", Vector, 2) = (-0.15, 1.8, 0, 0)
_Wind_Small_Intensity("Wind Small Intensity", Float) = 0.5
_UV_Pivot_Lock_Strangth("UV Pivot Lock Strangth", Float) = 1
[Toggle(_USE_VERTEX_COLOR)]_USE_VERTEX_COLOR("Use Vertex Color?", Float) = 0
[Toggle(_USE_DITHER)]_USE_DITHER("Use Dither?", Float) = 0
_Dither_Bottom_Level("Dither Bottom Level", Float) = 0
_Dither_Fade("Dither Fade", Float) = 2
[NoScaleOffset]_Foliage_Map("Foliage Map", 2D) = "white" {}
_Alpha_Clip("Alpha Clip", Float) = 1
_Color_Tint("Color Tint", Color) = (0.654902, 0.7529413, 0.2666667, 1)
[Toggle(_USE_GRADIENT_MAP)]_USE_GRADIENT_MAP("Use Gradient Map?", Float) = 0
[NoScaleOffset]_Gradient_Map("Gradient Map", 2D) = "white" {}
_Color_Top("Color Top", Color) = (0.4401, 0.6603774, 0.1214845, 1)
_Color_Bottom("Color Bottom", Color) = (0.2713315, 0.4528302, 0.1644713, 1)
_Smoothness("Smoothness", Float) = 0
[NonModifiableTextureData][NoScaleOffset]_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D("Texture2D", 2D) = "white" {}
[NonModifiableTextureData][NoScaleOffset]_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D("Texture2D", 2D) = "white" {}
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
#pragma shader_feature_local _ _USE_DITHER
#pragma shader_feature_local _ _USE_GRADIENT_MAP
#pragma shader_feature_local _ _USE_VERTEX_COLOR

#if defined(_USE_DITHER) && defined(_USE_GRADIENT_MAP) && defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_DITHER) && defined(_USE_GRADIENT_MAP)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_DITHER) && defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_2
#elif defined(_USE_DITHER)
#define KEYWORD_PERMUTATION_3
#elif defined(_USE_GRADIENT_MAP) && defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_4
#elif defined(_USE_GRADIENT_MAP)
#define KEYWORD_PERMUTATION_5
#elif defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_6
#else
#define KEYWORD_PERMUTATION_7
#endif


// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_TEXCOORD1
#define ATTRIBUTES_NEED_TEXCOORD2
#define ATTRIBUTES_NEED_COLOR
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
 float3 ObjectSpacePosition;
 float2 NDCPosition;
 float2 PixelPosition;
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 WorldSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 WorldSpacePosition;
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

float4 _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D_TexelSize;
float4 _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D_TexelSize;
float _Wind_Small_Scale;
float2 _Wind_Small_Speed;
float _Wind_Small_Intensity;
float _Wind_Large_Scale;
float2 _Wind_Large_Speed;
float4 _Color_Bottom;
float _Wind_Large_Intensity;
float _UV_Pivot_Lock_Strangth;
float _Dither_Bottom_Level;
float _Dither_Fade;
float4 _Foliage_Map_TexelSize;
float _Alpha_Clip;
float4 _Color_Tint;
float4 _Gradient_Map_TexelSize;
float4 _Color_Top;
float _Smoothness;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D);
TEXTURE2D(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D);
TEXTURE2D(_Foliage_Map);
SAMPLER(sampler_Foliage_Map);
TEXTURE2D(_Gradient_Map);
SAMPLER(sampler_Gradient_Map);

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

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

void Unity_Dither_float(float In, float4 ScreenPosition, out float Out)
{
    float2 uv = ScreenPosition.xy * _ScreenParams.xy;
    static const float DITHER_THRESHOLDS[16] =
    {
        1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
        13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
        4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
        16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
    };
    uint index = (uint(uv.x) % 4) * 4 + uint(uv.y) % 4;
    Out = In - DITHER_THRESHOLDS[index];
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
float _Property_ad4aa287fed64295a604d3291d1959a2_Out_0_Float = _Wind_Large_Scale;
float2 _Property_c2f76a94f588455e9b4c07b23d9ef2ff_Out_0_Vector2 = _Wind_Large_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_bad92adcb02c4fc28767432489546115;
_PositionalUVs_bad92adcb02c4fc28767432489546115.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_bad92adcb02c4fc28767432489546115.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_bad92adcb02c4fc28767432489546115_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_ad4aa287fed64295a604d3291d1959a2_Out_0_Float, _Property_c2f76a94f588455e9b4c07b23d9ef2ff_Out_0_Vector2, _PositionalUVs_bad92adcb02c4fc28767432489546115, _PositionalUVs_bad92adcb02c4fc28767432489546115_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_bad92adcb02c4fc28767432489546115_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_R_5_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_G_6_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_B_7_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_A_8_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.a;
float _Subtract_218acd7847974a5cbb60429ba8096400_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_R_5_Float, float(0.5), _Subtract_218acd7847974a5cbb60429ba8096400_Out_2_Float);
float _Split_9060b9716ec549aeacc51b6fd9846e1f_R_1_Float = IN.VertexColor[0];
float _Split_9060b9716ec549aeacc51b6fd9846e1f_G_2_Float = IN.VertexColor[1];
float _Split_9060b9716ec549aeacc51b6fd9846e1f_B_3_Float = IN.VertexColor[2];
float _Split_9060b9716ec549aeacc51b6fd9846e1f_A_4_Float = IN.VertexColor[3];
float4 _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4 = IN.uv0;
float _Split_cd4e6782516b4c4eba58883246f59068_R_1_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[0];
float _Split_cd4e6782516b4c4eba58883246f59068_G_2_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[1];
float _Split_cd4e6782516b4c4eba58883246f59068_B_3_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[2];
float _Split_cd4e6782516b4c4eba58883246f59068_A_4_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[3];
float _Property_8494919c92594e95b9e93cc374ab477d_Out_0_Float = _UV_Pivot_Lock_Strangth;
float _Power_a22a61dbae424f388acc9bbf24d84825_Out_2_Float;
Unity_Power_float(_Split_cd4e6782516b4c4eba58883246f59068_G_2_Float, _Property_8494919c92594e95b9e93cc374ab477d_Out_0_Float, _Power_a22a61dbae424f388acc9bbf24d84825_Out_2_Float);
#if defined(_USE_VERTEX_COLOR)
float _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float = _Split_9060b9716ec549aeacc51b6fd9846e1f_R_1_Float;
#else
float _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float = _Power_a22a61dbae424f388acc9bbf24d84825_Out_2_Float;
#endif
float _Multiply_ea0e0a96337840389ee116bcbf36e668_Out_2_Float;
Unity_Multiply_float_float(_Subtract_218acd7847974a5cbb60429ba8096400_Out_2_Float, _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float, _Multiply_ea0e0a96337840389ee116bcbf36e668_Out_2_Float);
float _Property_dbbb11cdd2654ae79939bef8b5ecd875_Out_0_Float = _Wind_Large_Intensity;
float _Multiply_6869889d47d046e8b5ad71f9c3a9adb4_Out_2_Float;
Unity_Multiply_float_float(_Multiply_ea0e0a96337840389ee116bcbf36e668_Out_2_Float, _Property_dbbb11cdd2654ae79939bef8b5ecd875_Out_0_Float, _Multiply_6869889d47d046e8b5ad71f9c3a9adb4_Out_2_Float);
float _Property_c327c41c045b40c483eb7da471e0c3fd_Out_0_Float = _Wind_Small_Scale;
float2 _Property_92349f3d399742e5b93d3ec2912b6d0b_Out_0_Vector2 = _Wind_Small_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_61edec0a11644d95b38796bd3e28715a;
_PositionalUVs_61edec0a11644d95b38796bd3e28715a.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_61edec0a11644d95b38796bd3e28715a.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_61edec0a11644d95b38796bd3e28715a_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_c327c41c045b40c483eb7da471e0c3fd_Out_0_Float, _Property_92349f3d399742e5b93d3ec2912b6d0b_Out_0_Vector2, _PositionalUVs_61edec0a11644d95b38796bd3e28715a, _PositionalUVs_61edec0a11644d95b38796bd3e28715a_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_61edec0a11644d95b38796bd3e28715a_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_R_5_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_G_6_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_B_7_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_A_8_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.a;
float _Subtract_38999ff8e5a04271b3794daa96fc9cca_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_R_5_Float, float(0.5), _Subtract_38999ff8e5a04271b3794daa96fc9cca_Out_2_Float);
float _Multiply_4209b0f1a88b424992ddafb6610c2968_Out_2_Float;
Unity_Multiply_float_float(_UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float, _Subtract_38999ff8e5a04271b3794daa96fc9cca_Out_2_Float, _Multiply_4209b0f1a88b424992ddafb6610c2968_Out_2_Float);
float _Property_2bd20e0e45834464a28f6379b9e8225c_Out_0_Float = _Wind_Small_Intensity;
float _Multiply_1c0b4b06455e4d31b5ad6f8c28d20004_Out_2_Float;
Unity_Multiply_float_float(_Multiply_4209b0f1a88b424992ddafb6610c2968_Out_2_Float, _Property_2bd20e0e45834464a28f6379b9e8225c_Out_0_Float, _Multiply_1c0b4b06455e4d31b5ad6f8c28d20004_Out_2_Float);
float _Add_0407a76ed876497f9c558311ba6b5eb8_Out_2_Float;
Unity_Add_float(_Multiply_6869889d47d046e8b5ad71f9c3a9adb4_Out_2_Float, _Multiply_1c0b4b06455e4d31b5ad6f8c28d20004_Out_2_Float, _Add_0407a76ed876497f9c558311ba6b5eb8_Out_2_Float);
float3 _Multiply_5d424720302c4e55afef4b8a9f76d3ef_Out_2_Vector3;
Unity_Multiply_float3_float3((_Add_0407a76ed876497f9c558311ba6b5eb8_Out_2_Float.xxx), IN.WorldSpaceNormal, _Multiply_5d424720302c4e55afef4b8a9f76d3ef_Out_2_Vector3);
float3 _Add_0a8e7ddaf6aa4278a2f9912909eba29d_Out_2_Vector3;
Unity_Add_float3(_Multiply_5d424720302c4e55afef4b8a9f76d3ef_Out_2_Vector3, IN.WorldSpacePosition, _Add_0a8e7ddaf6aa4278a2f9912909eba29d_Out_2_Vector3);
float3 _UPGFoliageBendWSCustomFunction_3d8d6250d27b4f3b96cbc7767f9ae45d_offsetWS_2_Vector3;
UPG_FoliageBendWS_float(IN.WorldSpacePosition, _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float, _UPGFoliageBendWSCustomFunction_3d8d6250d27b4f3b96cbc7767f9ae45d_offsetWS_2_Vector3);
float3 _Add_a2f1ce3eccd04a348677bce71231c28f_Out_2_Vector3;
Unity_Add_float3(_Add_0a8e7ddaf6aa4278a2f9912909eba29d_Out_2_Vector3, _UPGFoliageBendWSCustomFunction_3d8d6250d27b4f3b96cbc7767f9ae45d_offsetWS_2_Vector3, _Add_a2f1ce3eccd04a348677bce71231c28f_Out_2_Vector3);
float3 _Transform_cd30c5b4b2e5405bbe4451c6c589718b_Out_1_Vector3;
_Transform_cd30c5b4b2e5405bbe4451c6c589718b_Out_1_Vector3 = TransformWorldToObject(_Add_a2f1ce3eccd04a348677bce71231c28f_Out_2_Vector3.xyz);
description.Position = _Transform_cd30c5b4b2e5405bbe4451c6c589718b_Out_1_Vector3;
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
float4 _Property_597353718570472cb903eabbac10e98e_Out_0_Vector4 = _Color_Bottom;
float4 _Property_acf5ade3ddc74df2bb7b72f3fb5027e6_Out_0_Vector4 = _Color_Top;
UnityTexture2D _Property_0dd8d95b31054753b15cbea49c4dd049_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Gradient_Map, sampler_Gradient_Map, _Gradient_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_0dd8d95b31054753b15cbea49c4dd049_Out_0_Texture2D.tex, _Property_0dd8d95b31054753b15cbea49c4dd049_Out_0_Texture2D.samplerstate, _Property_0dd8d95b31054753b15cbea49c4dd049_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_0dd8d95b31054753b15cbea49c4dd049_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4, _Property_0dd8d95b31054753b15cbea49c4dd049_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_R_4_Float = _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4.r;
float _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_G_5_Float = _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4.g;
float _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_B_6_Float = _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4.b;
float _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_A_7_Float = _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4.a;
float4 _Lerp_45e933526a9e47429463b3d3ae7bcb99_Out_3_Vector4;
Unity_Lerp_float4(_Property_597353718570472cb903eabbac10e98e_Out_0_Vector4, _Property_acf5ade3ddc74df2bb7b72f3fb5027e6_Out_0_Vector4, (_SampleTexture2D_0139bf195dc74dea98a5142201287ca0_R_4_Float.xxxx), _Lerp_45e933526a9e47429463b3d3ae7bcb99_Out_3_Vector4);
float4 _Property_d51fad0917634f61855f006ac3a3036a_Out_0_Vector4 = _Color_Tint;
#if defined(_USE_GRADIENT_MAP)
float4 _UseGradientMap_ab913e91a75042bfa88ab1093a041f33_Out_0_Vector4 = _Lerp_45e933526a9e47429463b3d3ae7bcb99_Out_3_Vector4;
#else
float4 _UseGradientMap_ab913e91a75042bfa88ab1093a041f33_Out_0_Vector4 = _Property_d51fad0917634f61855f006ac3a3036a_Out_0_Vector4;
#endif
UnityTexture2D _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Foliage_Map, sampler_Foliage_Map, _Foliage_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.tex, _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.samplerstate, _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4, _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_R_4_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.r;
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_G_5_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.g;
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_B_6_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.b;
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_A_7_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.a;
float4 _Multiply_2d1489dbc4f84047b8a1330c23f185d7_Out_2_Vector4;
Unity_Multiply_float4_float4(_UseGradientMap_ab913e91a75042bfa88ab1093a041f33_Out_0_Vector4, _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4, _Multiply_2d1489dbc4f84047b8a1330c23f185d7_Out_2_Vector4);
float _Property_8dc6bc562c024ba58adfbb32ab0956a5_Out_0_Float = _Smoothness;
float _Property_ce8925e5051d4745a4cf3b2d4fb8db48_Out_0_Float = _Alpha_Clip;
float _Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_A_7_Float, _Property_ce8925e5051d4745a4cf3b2d4fb8db48_Out_0_Float, _Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float);
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_A_4_Float = 0;
float _Property_496eeb327965485cb724cb0c285f7a6c_Out_0_Float = _Dither_Bottom_Level;
float _Add_9a95e02d9c734b69a1bd64099eab897d_Out_2_Float;
Unity_Add_float(_Split_b2c67f57ffb14ffab00206eeffb0e2d5_G_2_Float, _Property_496eeb327965485cb724cb0c285f7a6c_Out_0_Float, _Add_9a95e02d9c734b69a1bd64099eab897d_Out_2_Float);
float _Property_ccb0e250db5745fe928b0ac7bf4d8af7_Out_0_Float = _Dither_Fade;
float _Multiply_38741f482df44a6584b848549f368772_Out_2_Float;
Unity_Multiply_float_float(_Add_9a95e02d9c734b69a1bd64099eab897d_Out_2_Float, _Property_ccb0e250db5745fe928b0ac7bf4d8af7_Out_0_Float, _Multiply_38741f482df44a6584b848549f368772_Out_2_Float);
float _Dither_22e77e4623774a7fa94d00b2ae4ff606_Out_2_Float;
Unity_Dither_float(_Multiply_38741f482df44a6584b848549f368772_Out_2_Float, float4(IN.NDCPosition.xy, 0, 0), _Dither_22e77e4623774a7fa94d00b2ae4ff606_Out_2_Float);
float _Multiply_9214bd931fea4bc98db2b9a46d79c8f8_Out_2_Float;
Unity_Multiply_float_float(_Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float, _Dither_22e77e4623774a7fa94d00b2ae4ff606_Out_2_Float, _Multiply_9214bd931fea4bc98db2b9a46d79c8f8_Out_2_Float);
#if defined(_USE_DITHER)
float _UseDither_4bfe8155f1434a3d9ded012dece1de0e_Out_0_Float = _Multiply_9214bd931fea4bc98db2b9a46d79c8f8_Out_2_Float;
#else
float _UseDither_4bfe8155f1434a3d9ded012dece1de0e_Out_0_Float = _Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float;
#endif
surface.BaseColor = (_Multiply_2d1489dbc4f84047b8a1330c23f185d7_Out_2_Vector4.xyz);
surface.NormalTS = IN.TangentSpaceNormal;
surface.Emission = float3(0, 0, 0);
surface.Metallic = float(0);
surface.Smoothness = _Property_8dc6bc562c024ba58adfbb32ab0956a5_Out_0_Float;
surface.Occlusion = float(1);
surface.Alpha = _UseDither_4bfe8155f1434a3d9ded012dece1de0e_Out_0_Float;
surface.AlphaClipThreshold = float(0.5);
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
    output.WorldSpaceNormal =                           TransformObjectToWorldNormal(input.normalOS);
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
    output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
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


    output.ObjectSpacePosition = TransformWorldToObject(input.positionWS);

    #if UNITY_UV_STARTS_AT_TOP
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #else
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #endif

    output.NDCPosition = output.PixelPosition.xy / _ScaledScreenParams.xy;
    output.NDCPosition.y = 1.0f - output.NDCPosition.y;

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
#pragma shader_feature_local _ _USE_DITHER
#pragma shader_feature_local _ _USE_GRADIENT_MAP
#pragma shader_feature_local _ _USE_VERTEX_COLOR

#if defined(_USE_DITHER) && defined(_USE_GRADIENT_MAP) && defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_DITHER) && defined(_USE_GRADIENT_MAP)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_DITHER) && defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_2
#elif defined(_USE_DITHER)
#define KEYWORD_PERMUTATION_3
#elif defined(_USE_GRADIENT_MAP) && defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_4
#elif defined(_USE_GRADIENT_MAP)
#define KEYWORD_PERMUTATION_5
#elif defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_6
#else
#define KEYWORD_PERMUTATION_7
#endif


// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_TEXCOORD1
#define ATTRIBUTES_NEED_TEXCOORD2
#define ATTRIBUTES_NEED_COLOR
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
 float3 ObjectSpacePosition;
 float2 NDCPosition;
 float2 PixelPosition;
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 WorldSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 WorldSpacePosition;
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

float4 _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D_TexelSize;
float4 _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D_TexelSize;
float _Wind_Small_Scale;
float2 _Wind_Small_Speed;
float _Wind_Small_Intensity;
float _Wind_Large_Scale;
float2 _Wind_Large_Speed;
float4 _Color_Bottom;
float _Wind_Large_Intensity;
float _UV_Pivot_Lock_Strangth;
float _Dither_Bottom_Level;
float _Dither_Fade;
float4 _Foliage_Map_TexelSize;
float _Alpha_Clip;
float4 _Color_Tint;
float4 _Gradient_Map_TexelSize;
float4 _Color_Top;
float _Smoothness;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D);
TEXTURE2D(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D);
TEXTURE2D(_Foliage_Map);
SAMPLER(sampler_Foliage_Map);
TEXTURE2D(_Gradient_Map);
SAMPLER(sampler_Gradient_Map);

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

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

void Unity_Dither_float(float In, float4 ScreenPosition, out float Out)
{
    float2 uv = ScreenPosition.xy * _ScreenParams.xy;
    static const float DITHER_THRESHOLDS[16] =
    {
        1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
        13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
        4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
        16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
    };
    uint index = (uint(uv.x) % 4) * 4 + uint(uv.y) % 4;
    Out = In - DITHER_THRESHOLDS[index];
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
float _Property_ad4aa287fed64295a604d3291d1959a2_Out_0_Float = _Wind_Large_Scale;
float2 _Property_c2f76a94f588455e9b4c07b23d9ef2ff_Out_0_Vector2 = _Wind_Large_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_bad92adcb02c4fc28767432489546115;
_PositionalUVs_bad92adcb02c4fc28767432489546115.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_bad92adcb02c4fc28767432489546115.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_bad92adcb02c4fc28767432489546115_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_ad4aa287fed64295a604d3291d1959a2_Out_0_Float, _Property_c2f76a94f588455e9b4c07b23d9ef2ff_Out_0_Vector2, _PositionalUVs_bad92adcb02c4fc28767432489546115, _PositionalUVs_bad92adcb02c4fc28767432489546115_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_bad92adcb02c4fc28767432489546115_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_R_5_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_G_6_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_B_7_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_A_8_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.a;
float _Subtract_218acd7847974a5cbb60429ba8096400_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_R_5_Float, float(0.5), _Subtract_218acd7847974a5cbb60429ba8096400_Out_2_Float);
float _Split_9060b9716ec549aeacc51b6fd9846e1f_R_1_Float = IN.VertexColor[0];
float _Split_9060b9716ec549aeacc51b6fd9846e1f_G_2_Float = IN.VertexColor[1];
float _Split_9060b9716ec549aeacc51b6fd9846e1f_B_3_Float = IN.VertexColor[2];
float _Split_9060b9716ec549aeacc51b6fd9846e1f_A_4_Float = IN.VertexColor[3];
float4 _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4 = IN.uv0;
float _Split_cd4e6782516b4c4eba58883246f59068_R_1_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[0];
float _Split_cd4e6782516b4c4eba58883246f59068_G_2_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[1];
float _Split_cd4e6782516b4c4eba58883246f59068_B_3_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[2];
float _Split_cd4e6782516b4c4eba58883246f59068_A_4_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[3];
float _Property_8494919c92594e95b9e93cc374ab477d_Out_0_Float = _UV_Pivot_Lock_Strangth;
float _Power_a22a61dbae424f388acc9bbf24d84825_Out_2_Float;
Unity_Power_float(_Split_cd4e6782516b4c4eba58883246f59068_G_2_Float, _Property_8494919c92594e95b9e93cc374ab477d_Out_0_Float, _Power_a22a61dbae424f388acc9bbf24d84825_Out_2_Float);
#if defined(_USE_VERTEX_COLOR)
float _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float = _Split_9060b9716ec549aeacc51b6fd9846e1f_R_1_Float;
#else
float _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float = _Power_a22a61dbae424f388acc9bbf24d84825_Out_2_Float;
#endif
float _Multiply_ea0e0a96337840389ee116bcbf36e668_Out_2_Float;
Unity_Multiply_float_float(_Subtract_218acd7847974a5cbb60429ba8096400_Out_2_Float, _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float, _Multiply_ea0e0a96337840389ee116bcbf36e668_Out_2_Float);
float _Property_dbbb11cdd2654ae79939bef8b5ecd875_Out_0_Float = _Wind_Large_Intensity;
float _Multiply_6869889d47d046e8b5ad71f9c3a9adb4_Out_2_Float;
Unity_Multiply_float_float(_Multiply_ea0e0a96337840389ee116bcbf36e668_Out_2_Float, _Property_dbbb11cdd2654ae79939bef8b5ecd875_Out_0_Float, _Multiply_6869889d47d046e8b5ad71f9c3a9adb4_Out_2_Float);
float _Property_c327c41c045b40c483eb7da471e0c3fd_Out_0_Float = _Wind_Small_Scale;
float2 _Property_92349f3d399742e5b93d3ec2912b6d0b_Out_0_Vector2 = _Wind_Small_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_61edec0a11644d95b38796bd3e28715a;
_PositionalUVs_61edec0a11644d95b38796bd3e28715a.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_61edec0a11644d95b38796bd3e28715a.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_61edec0a11644d95b38796bd3e28715a_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_c327c41c045b40c483eb7da471e0c3fd_Out_0_Float, _Property_92349f3d399742e5b93d3ec2912b6d0b_Out_0_Vector2, _PositionalUVs_61edec0a11644d95b38796bd3e28715a, _PositionalUVs_61edec0a11644d95b38796bd3e28715a_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_61edec0a11644d95b38796bd3e28715a_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_R_5_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_G_6_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_B_7_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_A_8_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.a;
float _Subtract_38999ff8e5a04271b3794daa96fc9cca_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_R_5_Float, float(0.5), _Subtract_38999ff8e5a04271b3794daa96fc9cca_Out_2_Float);
float _Multiply_4209b0f1a88b424992ddafb6610c2968_Out_2_Float;
Unity_Multiply_float_float(_UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float, _Subtract_38999ff8e5a04271b3794daa96fc9cca_Out_2_Float, _Multiply_4209b0f1a88b424992ddafb6610c2968_Out_2_Float);
float _Property_2bd20e0e45834464a28f6379b9e8225c_Out_0_Float = _Wind_Small_Intensity;
float _Multiply_1c0b4b06455e4d31b5ad6f8c28d20004_Out_2_Float;
Unity_Multiply_float_float(_Multiply_4209b0f1a88b424992ddafb6610c2968_Out_2_Float, _Property_2bd20e0e45834464a28f6379b9e8225c_Out_0_Float, _Multiply_1c0b4b06455e4d31b5ad6f8c28d20004_Out_2_Float);
float _Add_0407a76ed876497f9c558311ba6b5eb8_Out_2_Float;
Unity_Add_float(_Multiply_6869889d47d046e8b5ad71f9c3a9adb4_Out_2_Float, _Multiply_1c0b4b06455e4d31b5ad6f8c28d20004_Out_2_Float, _Add_0407a76ed876497f9c558311ba6b5eb8_Out_2_Float);
float3 _Multiply_5d424720302c4e55afef4b8a9f76d3ef_Out_2_Vector3;
Unity_Multiply_float3_float3((_Add_0407a76ed876497f9c558311ba6b5eb8_Out_2_Float.xxx), IN.WorldSpaceNormal, _Multiply_5d424720302c4e55afef4b8a9f76d3ef_Out_2_Vector3);
float3 _Add_0a8e7ddaf6aa4278a2f9912909eba29d_Out_2_Vector3;
Unity_Add_float3(_Multiply_5d424720302c4e55afef4b8a9f76d3ef_Out_2_Vector3, IN.WorldSpacePosition, _Add_0a8e7ddaf6aa4278a2f9912909eba29d_Out_2_Vector3);
float3 _UPGFoliageBendWSCustomFunction_3d8d6250d27b4f3b96cbc7767f9ae45d_offsetWS_2_Vector3;
UPG_FoliageBendWS_float(IN.WorldSpacePosition, _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float, _UPGFoliageBendWSCustomFunction_3d8d6250d27b4f3b96cbc7767f9ae45d_offsetWS_2_Vector3);
float3 _Add_a2f1ce3eccd04a348677bce71231c28f_Out_2_Vector3;
Unity_Add_float3(_Add_0a8e7ddaf6aa4278a2f9912909eba29d_Out_2_Vector3, _UPGFoliageBendWSCustomFunction_3d8d6250d27b4f3b96cbc7767f9ae45d_offsetWS_2_Vector3, _Add_a2f1ce3eccd04a348677bce71231c28f_Out_2_Vector3);
float3 _Transform_cd30c5b4b2e5405bbe4451c6c589718b_Out_1_Vector3;
_Transform_cd30c5b4b2e5405bbe4451c6c589718b_Out_1_Vector3 = TransformWorldToObject(_Add_a2f1ce3eccd04a348677bce71231c28f_Out_2_Vector3.xyz);
description.Position = _Transform_cd30c5b4b2e5405bbe4451c6c589718b_Out_1_Vector3;
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
float4 _Property_597353718570472cb903eabbac10e98e_Out_0_Vector4 = _Color_Bottom;
float4 _Property_acf5ade3ddc74df2bb7b72f3fb5027e6_Out_0_Vector4 = _Color_Top;
UnityTexture2D _Property_0dd8d95b31054753b15cbea49c4dd049_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Gradient_Map, sampler_Gradient_Map, _Gradient_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_0dd8d95b31054753b15cbea49c4dd049_Out_0_Texture2D.tex, _Property_0dd8d95b31054753b15cbea49c4dd049_Out_0_Texture2D.samplerstate, _Property_0dd8d95b31054753b15cbea49c4dd049_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_0dd8d95b31054753b15cbea49c4dd049_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4, _Property_0dd8d95b31054753b15cbea49c4dd049_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_R_4_Float = _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4.r;
float _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_G_5_Float = _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4.g;
float _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_B_6_Float = _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4.b;
float _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_A_7_Float = _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4.a;
float4 _Lerp_45e933526a9e47429463b3d3ae7bcb99_Out_3_Vector4;
Unity_Lerp_float4(_Property_597353718570472cb903eabbac10e98e_Out_0_Vector4, _Property_acf5ade3ddc74df2bb7b72f3fb5027e6_Out_0_Vector4, (_SampleTexture2D_0139bf195dc74dea98a5142201287ca0_R_4_Float.xxxx), _Lerp_45e933526a9e47429463b3d3ae7bcb99_Out_3_Vector4);
float4 _Property_d51fad0917634f61855f006ac3a3036a_Out_0_Vector4 = _Color_Tint;
#if defined(_USE_GRADIENT_MAP)
float4 _UseGradientMap_ab913e91a75042bfa88ab1093a041f33_Out_0_Vector4 = _Lerp_45e933526a9e47429463b3d3ae7bcb99_Out_3_Vector4;
#else
float4 _UseGradientMap_ab913e91a75042bfa88ab1093a041f33_Out_0_Vector4 = _Property_d51fad0917634f61855f006ac3a3036a_Out_0_Vector4;
#endif
UnityTexture2D _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Foliage_Map, sampler_Foliage_Map, _Foliage_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.tex, _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.samplerstate, _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4, _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_R_4_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.r;
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_G_5_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.g;
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_B_6_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.b;
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_A_7_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.a;
float4 _Multiply_2d1489dbc4f84047b8a1330c23f185d7_Out_2_Vector4;
Unity_Multiply_float4_float4(_UseGradientMap_ab913e91a75042bfa88ab1093a041f33_Out_0_Vector4, _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4, _Multiply_2d1489dbc4f84047b8a1330c23f185d7_Out_2_Vector4);
float _Property_8dc6bc562c024ba58adfbb32ab0956a5_Out_0_Float = _Smoothness;
float _Property_ce8925e5051d4745a4cf3b2d4fb8db48_Out_0_Float = _Alpha_Clip;
float _Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_A_7_Float, _Property_ce8925e5051d4745a4cf3b2d4fb8db48_Out_0_Float, _Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float);
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_A_4_Float = 0;
float _Property_496eeb327965485cb724cb0c285f7a6c_Out_0_Float = _Dither_Bottom_Level;
float _Add_9a95e02d9c734b69a1bd64099eab897d_Out_2_Float;
Unity_Add_float(_Split_b2c67f57ffb14ffab00206eeffb0e2d5_G_2_Float, _Property_496eeb327965485cb724cb0c285f7a6c_Out_0_Float, _Add_9a95e02d9c734b69a1bd64099eab897d_Out_2_Float);
float _Property_ccb0e250db5745fe928b0ac7bf4d8af7_Out_0_Float = _Dither_Fade;
float _Multiply_38741f482df44a6584b848549f368772_Out_2_Float;
Unity_Multiply_float_float(_Add_9a95e02d9c734b69a1bd64099eab897d_Out_2_Float, _Property_ccb0e250db5745fe928b0ac7bf4d8af7_Out_0_Float, _Multiply_38741f482df44a6584b848549f368772_Out_2_Float);
float _Dither_22e77e4623774a7fa94d00b2ae4ff606_Out_2_Float;
Unity_Dither_float(_Multiply_38741f482df44a6584b848549f368772_Out_2_Float, float4(IN.NDCPosition.xy, 0, 0), _Dither_22e77e4623774a7fa94d00b2ae4ff606_Out_2_Float);
float _Multiply_9214bd931fea4bc98db2b9a46d79c8f8_Out_2_Float;
Unity_Multiply_float_float(_Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float, _Dither_22e77e4623774a7fa94d00b2ae4ff606_Out_2_Float, _Multiply_9214bd931fea4bc98db2b9a46d79c8f8_Out_2_Float);
#if defined(_USE_DITHER)
float _UseDither_4bfe8155f1434a3d9ded012dece1de0e_Out_0_Float = _Multiply_9214bd931fea4bc98db2b9a46d79c8f8_Out_2_Float;
#else
float _UseDither_4bfe8155f1434a3d9ded012dece1de0e_Out_0_Float = _Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float;
#endif
surface.BaseColor = (_Multiply_2d1489dbc4f84047b8a1330c23f185d7_Out_2_Vector4.xyz);
surface.NormalTS = IN.TangentSpaceNormal;
surface.Emission = float3(0, 0, 0);
surface.Metallic = float(0);
surface.Smoothness = _Property_8dc6bc562c024ba58adfbb32ab0956a5_Out_0_Float;
surface.Occlusion = float(1);
surface.Alpha = _UseDither_4bfe8155f1434a3d9ded012dece1de0e_Out_0_Float;
surface.AlphaClipThreshold = float(0.5);
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
    output.WorldSpaceNormal =                           TransformObjectToWorldNormal(input.normalOS);
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
    output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
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


    output.ObjectSpacePosition = TransformWorldToObject(input.positionWS);

    #if UNITY_UV_STARTS_AT_TOP
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #else
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #endif

    output.NDCPosition = output.PixelPosition.xy / _ScaledScreenParams.xy;
    output.NDCPosition.y = 1.0f - output.NDCPosition.y;

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
Cull Off
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
#pragma shader_feature_local _ _USE_DITHER
#pragma shader_feature_local _ _USE_GRADIENT_MAP
#pragma shader_feature_local _ _USE_VERTEX_COLOR

#if defined(_USE_DITHER) && defined(_USE_GRADIENT_MAP) && defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_DITHER) && defined(_USE_GRADIENT_MAP)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_DITHER) && defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_2
#elif defined(_USE_DITHER)
#define KEYWORD_PERMUTATION_3
#elif defined(_USE_GRADIENT_MAP) && defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_4
#elif defined(_USE_GRADIENT_MAP)
#define KEYWORD_PERMUTATION_5
#elif defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_6
#else
#define KEYWORD_PERMUTATION_7
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
#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_NORMAL_WS
#define VARYINGS_NEED_TEXCOORD0
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_SHADOWCASTER
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
 float3 ObjectSpacePosition;
 float2 NDCPosition;
 float2 PixelPosition;
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 WorldSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 WorldSpacePosition;
 float4 uv0;
 float4 VertexColor;
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

float4 _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D_TexelSize;
float4 _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D_TexelSize;
float _Wind_Small_Scale;
float2 _Wind_Small_Speed;
float _Wind_Small_Intensity;
float _Wind_Large_Scale;
float2 _Wind_Large_Speed;
float4 _Color_Bottom;
float _Wind_Large_Intensity;
float _UV_Pivot_Lock_Strangth;
float _Dither_Bottom_Level;
float _Dither_Fade;
float4 _Foliage_Map_TexelSize;
float _Alpha_Clip;
float4 _Color_Tint;
float4 _Gradient_Map_TexelSize;
float4 _Color_Top;
float _Smoothness;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D);
TEXTURE2D(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D);
TEXTURE2D(_Foliage_Map);
SAMPLER(sampler_Foliage_Map);
TEXTURE2D(_Gradient_Map);
SAMPLER(sampler_Gradient_Map);

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

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Dither_float(float In, float4 ScreenPosition, out float Out)
{
    float2 uv = ScreenPosition.xy * _ScreenParams.xy;
    static const float DITHER_THRESHOLDS[16] =
    {
        1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
        13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
        4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
        16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
    };
    uint index = (uint(uv.x) % 4) * 4 + uint(uv.y) % 4;
    Out = In - DITHER_THRESHOLDS[index];
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
float _Property_ad4aa287fed64295a604d3291d1959a2_Out_0_Float = _Wind_Large_Scale;
float2 _Property_c2f76a94f588455e9b4c07b23d9ef2ff_Out_0_Vector2 = _Wind_Large_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_bad92adcb02c4fc28767432489546115;
_PositionalUVs_bad92adcb02c4fc28767432489546115.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_bad92adcb02c4fc28767432489546115.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_bad92adcb02c4fc28767432489546115_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_ad4aa287fed64295a604d3291d1959a2_Out_0_Float, _Property_c2f76a94f588455e9b4c07b23d9ef2ff_Out_0_Vector2, _PositionalUVs_bad92adcb02c4fc28767432489546115, _PositionalUVs_bad92adcb02c4fc28767432489546115_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_bad92adcb02c4fc28767432489546115_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_R_5_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_G_6_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_B_7_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_A_8_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.a;
float _Subtract_218acd7847974a5cbb60429ba8096400_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_R_5_Float, float(0.5), _Subtract_218acd7847974a5cbb60429ba8096400_Out_2_Float);
float _Split_9060b9716ec549aeacc51b6fd9846e1f_R_1_Float = IN.VertexColor[0];
float _Split_9060b9716ec549aeacc51b6fd9846e1f_G_2_Float = IN.VertexColor[1];
float _Split_9060b9716ec549aeacc51b6fd9846e1f_B_3_Float = IN.VertexColor[2];
float _Split_9060b9716ec549aeacc51b6fd9846e1f_A_4_Float = IN.VertexColor[3];
float4 _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4 = IN.uv0;
float _Split_cd4e6782516b4c4eba58883246f59068_R_1_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[0];
float _Split_cd4e6782516b4c4eba58883246f59068_G_2_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[1];
float _Split_cd4e6782516b4c4eba58883246f59068_B_3_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[2];
float _Split_cd4e6782516b4c4eba58883246f59068_A_4_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[3];
float _Property_8494919c92594e95b9e93cc374ab477d_Out_0_Float = _UV_Pivot_Lock_Strangth;
float _Power_a22a61dbae424f388acc9bbf24d84825_Out_2_Float;
Unity_Power_float(_Split_cd4e6782516b4c4eba58883246f59068_G_2_Float, _Property_8494919c92594e95b9e93cc374ab477d_Out_0_Float, _Power_a22a61dbae424f388acc9bbf24d84825_Out_2_Float);
#if defined(_USE_VERTEX_COLOR)
float _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float = _Split_9060b9716ec549aeacc51b6fd9846e1f_R_1_Float;
#else
float _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float = _Power_a22a61dbae424f388acc9bbf24d84825_Out_2_Float;
#endif
float _Multiply_ea0e0a96337840389ee116bcbf36e668_Out_2_Float;
Unity_Multiply_float_float(_Subtract_218acd7847974a5cbb60429ba8096400_Out_2_Float, _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float, _Multiply_ea0e0a96337840389ee116bcbf36e668_Out_2_Float);
float _Property_dbbb11cdd2654ae79939bef8b5ecd875_Out_0_Float = _Wind_Large_Intensity;
float _Multiply_6869889d47d046e8b5ad71f9c3a9adb4_Out_2_Float;
Unity_Multiply_float_float(_Multiply_ea0e0a96337840389ee116bcbf36e668_Out_2_Float, _Property_dbbb11cdd2654ae79939bef8b5ecd875_Out_0_Float, _Multiply_6869889d47d046e8b5ad71f9c3a9adb4_Out_2_Float);
float _Property_c327c41c045b40c483eb7da471e0c3fd_Out_0_Float = _Wind_Small_Scale;
float2 _Property_92349f3d399742e5b93d3ec2912b6d0b_Out_0_Vector2 = _Wind_Small_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_61edec0a11644d95b38796bd3e28715a;
_PositionalUVs_61edec0a11644d95b38796bd3e28715a.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_61edec0a11644d95b38796bd3e28715a.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_61edec0a11644d95b38796bd3e28715a_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_c327c41c045b40c483eb7da471e0c3fd_Out_0_Float, _Property_92349f3d399742e5b93d3ec2912b6d0b_Out_0_Vector2, _PositionalUVs_61edec0a11644d95b38796bd3e28715a, _PositionalUVs_61edec0a11644d95b38796bd3e28715a_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_61edec0a11644d95b38796bd3e28715a_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_R_5_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_G_6_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_B_7_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_A_8_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.a;
float _Subtract_38999ff8e5a04271b3794daa96fc9cca_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_R_5_Float, float(0.5), _Subtract_38999ff8e5a04271b3794daa96fc9cca_Out_2_Float);
float _Multiply_4209b0f1a88b424992ddafb6610c2968_Out_2_Float;
Unity_Multiply_float_float(_UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float, _Subtract_38999ff8e5a04271b3794daa96fc9cca_Out_2_Float, _Multiply_4209b0f1a88b424992ddafb6610c2968_Out_2_Float);
float _Property_2bd20e0e45834464a28f6379b9e8225c_Out_0_Float = _Wind_Small_Intensity;
float _Multiply_1c0b4b06455e4d31b5ad6f8c28d20004_Out_2_Float;
Unity_Multiply_float_float(_Multiply_4209b0f1a88b424992ddafb6610c2968_Out_2_Float, _Property_2bd20e0e45834464a28f6379b9e8225c_Out_0_Float, _Multiply_1c0b4b06455e4d31b5ad6f8c28d20004_Out_2_Float);
float _Add_0407a76ed876497f9c558311ba6b5eb8_Out_2_Float;
Unity_Add_float(_Multiply_6869889d47d046e8b5ad71f9c3a9adb4_Out_2_Float, _Multiply_1c0b4b06455e4d31b5ad6f8c28d20004_Out_2_Float, _Add_0407a76ed876497f9c558311ba6b5eb8_Out_2_Float);
float3 _Multiply_5d424720302c4e55afef4b8a9f76d3ef_Out_2_Vector3;
Unity_Multiply_float3_float3((_Add_0407a76ed876497f9c558311ba6b5eb8_Out_2_Float.xxx), IN.WorldSpaceNormal, _Multiply_5d424720302c4e55afef4b8a9f76d3ef_Out_2_Vector3);
float3 _Add_0a8e7ddaf6aa4278a2f9912909eba29d_Out_2_Vector3;
Unity_Add_float3(_Multiply_5d424720302c4e55afef4b8a9f76d3ef_Out_2_Vector3, IN.WorldSpacePosition, _Add_0a8e7ddaf6aa4278a2f9912909eba29d_Out_2_Vector3);
float3 _UPGFoliageBendWSCustomFunction_3d8d6250d27b4f3b96cbc7767f9ae45d_offsetWS_2_Vector3;
UPG_FoliageBendWS_float(IN.WorldSpacePosition, _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float, _UPGFoliageBendWSCustomFunction_3d8d6250d27b4f3b96cbc7767f9ae45d_offsetWS_2_Vector3);
float3 _Add_a2f1ce3eccd04a348677bce71231c28f_Out_2_Vector3;
Unity_Add_float3(_Add_0a8e7ddaf6aa4278a2f9912909eba29d_Out_2_Vector3, _UPGFoliageBendWSCustomFunction_3d8d6250d27b4f3b96cbc7767f9ae45d_offsetWS_2_Vector3, _Add_a2f1ce3eccd04a348677bce71231c28f_Out_2_Vector3);
float3 _Transform_cd30c5b4b2e5405bbe4451c6c589718b_Out_1_Vector3;
_Transform_cd30c5b4b2e5405bbe4451c6c589718b_Out_1_Vector3 = TransformWorldToObject(_Add_a2f1ce3eccd04a348677bce71231c28f_Out_2_Vector3.xyz);
description.Position = _Transform_cd30c5b4b2e5405bbe4451c6c589718b_Out_1_Vector3;
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
UnityTexture2D _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Foliage_Map, sampler_Foliage_Map, _Foliage_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.tex, _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.samplerstate, _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4, _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_R_4_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.r;
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_G_5_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.g;
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_B_6_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.b;
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_A_7_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.a;
float _Property_ce8925e5051d4745a4cf3b2d4fb8db48_Out_0_Float = _Alpha_Clip;
float _Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_A_7_Float, _Property_ce8925e5051d4745a4cf3b2d4fb8db48_Out_0_Float, _Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float);
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_A_4_Float = 0;
float _Property_496eeb327965485cb724cb0c285f7a6c_Out_0_Float = _Dither_Bottom_Level;
float _Add_9a95e02d9c734b69a1bd64099eab897d_Out_2_Float;
Unity_Add_float(_Split_b2c67f57ffb14ffab00206eeffb0e2d5_G_2_Float, _Property_496eeb327965485cb724cb0c285f7a6c_Out_0_Float, _Add_9a95e02d9c734b69a1bd64099eab897d_Out_2_Float);
float _Property_ccb0e250db5745fe928b0ac7bf4d8af7_Out_0_Float = _Dither_Fade;
float _Multiply_38741f482df44a6584b848549f368772_Out_2_Float;
Unity_Multiply_float_float(_Add_9a95e02d9c734b69a1bd64099eab897d_Out_2_Float, _Property_ccb0e250db5745fe928b0ac7bf4d8af7_Out_0_Float, _Multiply_38741f482df44a6584b848549f368772_Out_2_Float);
float _Dither_22e77e4623774a7fa94d00b2ae4ff606_Out_2_Float;
Unity_Dither_float(_Multiply_38741f482df44a6584b848549f368772_Out_2_Float, float4(IN.NDCPosition.xy, 0, 0), _Dither_22e77e4623774a7fa94d00b2ae4ff606_Out_2_Float);
float _Multiply_9214bd931fea4bc98db2b9a46d79c8f8_Out_2_Float;
Unity_Multiply_float_float(_Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float, _Dither_22e77e4623774a7fa94d00b2ae4ff606_Out_2_Float, _Multiply_9214bd931fea4bc98db2b9a46d79c8f8_Out_2_Float);
#if defined(_USE_DITHER)
float _UseDither_4bfe8155f1434a3d9ded012dece1de0e_Out_0_Float = _Multiply_9214bd931fea4bc98db2b9a46d79c8f8_Out_2_Float;
#else
float _UseDither_4bfe8155f1434a3d9ded012dece1de0e_Out_0_Float = _Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float;
#endif
surface.Alpha = _UseDither_4bfe8155f1434a3d9ded012dece1de0e_Out_0_Float;
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
    output.WorldSpaceNormal =                           TransformObjectToWorldNormal(input.normalOS);
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
    output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
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

    





    output.ObjectSpacePosition = TransformWorldToObject(input.positionWS);

    #if UNITY_UV_STARTS_AT_TOP
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #else
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #endif

    output.NDCPosition = output.PixelPosition.xy / _ScaledScreenParams.xy;
    output.NDCPosition.y = 1.0f - output.NDCPosition.y;

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
#pragma shader_feature_local _ _USE_DITHER
#pragma shader_feature_local _ _USE_GRADIENT_MAP
#pragma shader_feature_local _ _USE_VERTEX_COLOR

#if defined(_USE_DITHER) && defined(_USE_GRADIENT_MAP) && defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_DITHER) && defined(_USE_GRADIENT_MAP)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_DITHER) && defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_2
#elif defined(_USE_DITHER)
#define KEYWORD_PERMUTATION_3
#elif defined(_USE_GRADIENT_MAP) && defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_4
#elif defined(_USE_GRADIENT_MAP)
#define KEYWORD_PERMUTATION_5
#elif defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_6
#else
#define KEYWORD_PERMUTATION_7
#endif


// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_COLOR
#define GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT
#define VARYINGS_NEED_POSITION_WS
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
 float3 normalOS : NORMAL;
 float4 uv0 : TEXCOORD0;
 float4 color : COLOR;
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
 float3 ObjectSpacePosition;
 float2 NDCPosition;
 float2 PixelPosition;
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 WorldSpaceNormal;
 float3 ObjectSpacePosition;
 float3 WorldSpacePosition;
 float4 uv0;
 float4 VertexColor;
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

float4 _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D_TexelSize;
float4 _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D_TexelSize;
float _Wind_Small_Scale;
float2 _Wind_Small_Speed;
float _Wind_Small_Intensity;
float _Wind_Large_Scale;
float2 _Wind_Large_Speed;
float4 _Color_Bottom;
float _Wind_Large_Intensity;
float _UV_Pivot_Lock_Strangth;
float _Dither_Bottom_Level;
float _Dither_Fade;
float4 _Foliage_Map_TexelSize;
float _Alpha_Clip;
float4 _Color_Tint;
float4 _Gradient_Map_TexelSize;
float4 _Color_Top;
float _Smoothness;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D);
TEXTURE2D(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D);
TEXTURE2D(_Foliage_Map);
SAMPLER(sampler_Foliage_Map);
TEXTURE2D(_Gradient_Map);
SAMPLER(sampler_Gradient_Map);

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

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Dither_float(float In, float4 ScreenPosition, out float Out)
{
    float2 uv = ScreenPosition.xy * _ScreenParams.xy;
    static const float DITHER_THRESHOLDS[16] =
    {
        1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
        13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
        4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
        16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
    };
    uint index = (uint(uv.x) % 4) * 4 + uint(uv.y) % 4;
    Out = In - DITHER_THRESHOLDS[index];
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
float _Property_ad4aa287fed64295a604d3291d1959a2_Out_0_Float = _Wind_Large_Scale;
float2 _Property_c2f76a94f588455e9b4c07b23d9ef2ff_Out_0_Vector2 = _Wind_Large_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_bad92adcb02c4fc28767432489546115;
_PositionalUVs_bad92adcb02c4fc28767432489546115.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_bad92adcb02c4fc28767432489546115.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_bad92adcb02c4fc28767432489546115_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_ad4aa287fed64295a604d3291d1959a2_Out_0_Float, _Property_c2f76a94f588455e9b4c07b23d9ef2ff_Out_0_Vector2, _PositionalUVs_bad92adcb02c4fc28767432489546115, _PositionalUVs_bad92adcb02c4fc28767432489546115_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_bad92adcb02c4fc28767432489546115_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_R_5_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_G_6_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_B_7_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_A_8_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.a;
float _Subtract_218acd7847974a5cbb60429ba8096400_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_R_5_Float, float(0.5), _Subtract_218acd7847974a5cbb60429ba8096400_Out_2_Float);
float _Split_9060b9716ec549aeacc51b6fd9846e1f_R_1_Float = IN.VertexColor[0];
float _Split_9060b9716ec549aeacc51b6fd9846e1f_G_2_Float = IN.VertexColor[1];
float _Split_9060b9716ec549aeacc51b6fd9846e1f_B_3_Float = IN.VertexColor[2];
float _Split_9060b9716ec549aeacc51b6fd9846e1f_A_4_Float = IN.VertexColor[3];
float4 _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4 = IN.uv0;
float _Split_cd4e6782516b4c4eba58883246f59068_R_1_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[0];
float _Split_cd4e6782516b4c4eba58883246f59068_G_2_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[1];
float _Split_cd4e6782516b4c4eba58883246f59068_B_3_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[2];
float _Split_cd4e6782516b4c4eba58883246f59068_A_4_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[3];
float _Property_8494919c92594e95b9e93cc374ab477d_Out_0_Float = _UV_Pivot_Lock_Strangth;
float _Power_a22a61dbae424f388acc9bbf24d84825_Out_2_Float;
Unity_Power_float(_Split_cd4e6782516b4c4eba58883246f59068_G_2_Float, _Property_8494919c92594e95b9e93cc374ab477d_Out_0_Float, _Power_a22a61dbae424f388acc9bbf24d84825_Out_2_Float);
#if defined(_USE_VERTEX_COLOR)
float _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float = _Split_9060b9716ec549aeacc51b6fd9846e1f_R_1_Float;
#else
float _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float = _Power_a22a61dbae424f388acc9bbf24d84825_Out_2_Float;
#endif
float _Multiply_ea0e0a96337840389ee116bcbf36e668_Out_2_Float;
Unity_Multiply_float_float(_Subtract_218acd7847974a5cbb60429ba8096400_Out_2_Float, _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float, _Multiply_ea0e0a96337840389ee116bcbf36e668_Out_2_Float);
float _Property_dbbb11cdd2654ae79939bef8b5ecd875_Out_0_Float = _Wind_Large_Intensity;
float _Multiply_6869889d47d046e8b5ad71f9c3a9adb4_Out_2_Float;
Unity_Multiply_float_float(_Multiply_ea0e0a96337840389ee116bcbf36e668_Out_2_Float, _Property_dbbb11cdd2654ae79939bef8b5ecd875_Out_0_Float, _Multiply_6869889d47d046e8b5ad71f9c3a9adb4_Out_2_Float);
float _Property_c327c41c045b40c483eb7da471e0c3fd_Out_0_Float = _Wind_Small_Scale;
float2 _Property_92349f3d399742e5b93d3ec2912b6d0b_Out_0_Vector2 = _Wind_Small_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_61edec0a11644d95b38796bd3e28715a;
_PositionalUVs_61edec0a11644d95b38796bd3e28715a.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_61edec0a11644d95b38796bd3e28715a.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_61edec0a11644d95b38796bd3e28715a_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_c327c41c045b40c483eb7da471e0c3fd_Out_0_Float, _Property_92349f3d399742e5b93d3ec2912b6d0b_Out_0_Vector2, _PositionalUVs_61edec0a11644d95b38796bd3e28715a, _PositionalUVs_61edec0a11644d95b38796bd3e28715a_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_61edec0a11644d95b38796bd3e28715a_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_R_5_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_G_6_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_B_7_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_A_8_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.a;
float _Subtract_38999ff8e5a04271b3794daa96fc9cca_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_R_5_Float, float(0.5), _Subtract_38999ff8e5a04271b3794daa96fc9cca_Out_2_Float);
float _Multiply_4209b0f1a88b424992ddafb6610c2968_Out_2_Float;
Unity_Multiply_float_float(_UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float, _Subtract_38999ff8e5a04271b3794daa96fc9cca_Out_2_Float, _Multiply_4209b0f1a88b424992ddafb6610c2968_Out_2_Float);
float _Property_2bd20e0e45834464a28f6379b9e8225c_Out_0_Float = _Wind_Small_Intensity;
float _Multiply_1c0b4b06455e4d31b5ad6f8c28d20004_Out_2_Float;
Unity_Multiply_float_float(_Multiply_4209b0f1a88b424992ddafb6610c2968_Out_2_Float, _Property_2bd20e0e45834464a28f6379b9e8225c_Out_0_Float, _Multiply_1c0b4b06455e4d31b5ad6f8c28d20004_Out_2_Float);
float _Add_0407a76ed876497f9c558311ba6b5eb8_Out_2_Float;
Unity_Add_float(_Multiply_6869889d47d046e8b5ad71f9c3a9adb4_Out_2_Float, _Multiply_1c0b4b06455e4d31b5ad6f8c28d20004_Out_2_Float, _Add_0407a76ed876497f9c558311ba6b5eb8_Out_2_Float);
float3 _Multiply_5d424720302c4e55afef4b8a9f76d3ef_Out_2_Vector3;
Unity_Multiply_float3_float3((_Add_0407a76ed876497f9c558311ba6b5eb8_Out_2_Float.xxx), IN.WorldSpaceNormal, _Multiply_5d424720302c4e55afef4b8a9f76d3ef_Out_2_Vector3);
float3 _Add_0a8e7ddaf6aa4278a2f9912909eba29d_Out_2_Vector3;
Unity_Add_float3(_Multiply_5d424720302c4e55afef4b8a9f76d3ef_Out_2_Vector3, IN.WorldSpacePosition, _Add_0a8e7ddaf6aa4278a2f9912909eba29d_Out_2_Vector3);
float3 _UPGFoliageBendWSCustomFunction_3d8d6250d27b4f3b96cbc7767f9ae45d_offsetWS_2_Vector3;
UPG_FoliageBendWS_float(IN.WorldSpacePosition, _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float, _UPGFoliageBendWSCustomFunction_3d8d6250d27b4f3b96cbc7767f9ae45d_offsetWS_2_Vector3);
float3 _Add_a2f1ce3eccd04a348677bce71231c28f_Out_2_Vector3;
Unity_Add_float3(_Add_0a8e7ddaf6aa4278a2f9912909eba29d_Out_2_Vector3, _UPGFoliageBendWSCustomFunction_3d8d6250d27b4f3b96cbc7767f9ae45d_offsetWS_2_Vector3, _Add_a2f1ce3eccd04a348677bce71231c28f_Out_2_Vector3);
float3 _Transform_cd30c5b4b2e5405bbe4451c6c589718b_Out_1_Vector3;
_Transform_cd30c5b4b2e5405bbe4451c6c589718b_Out_1_Vector3 = TransformWorldToObject(_Add_a2f1ce3eccd04a348677bce71231c28f_Out_2_Vector3.xyz);
description.Position = _Transform_cd30c5b4b2e5405bbe4451c6c589718b_Out_1_Vector3;
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
UnityTexture2D _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Foliage_Map, sampler_Foliage_Map, _Foliage_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.tex, _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.samplerstate, _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4, _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_R_4_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.r;
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_G_5_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.g;
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_B_6_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.b;
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_A_7_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.a;
float _Property_ce8925e5051d4745a4cf3b2d4fb8db48_Out_0_Float = _Alpha_Clip;
float _Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_A_7_Float, _Property_ce8925e5051d4745a4cf3b2d4fb8db48_Out_0_Float, _Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float);
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_A_4_Float = 0;
float _Property_496eeb327965485cb724cb0c285f7a6c_Out_0_Float = _Dither_Bottom_Level;
float _Add_9a95e02d9c734b69a1bd64099eab897d_Out_2_Float;
Unity_Add_float(_Split_b2c67f57ffb14ffab00206eeffb0e2d5_G_2_Float, _Property_496eeb327965485cb724cb0c285f7a6c_Out_0_Float, _Add_9a95e02d9c734b69a1bd64099eab897d_Out_2_Float);
float _Property_ccb0e250db5745fe928b0ac7bf4d8af7_Out_0_Float = _Dither_Fade;
float _Multiply_38741f482df44a6584b848549f368772_Out_2_Float;
Unity_Multiply_float_float(_Add_9a95e02d9c734b69a1bd64099eab897d_Out_2_Float, _Property_ccb0e250db5745fe928b0ac7bf4d8af7_Out_0_Float, _Multiply_38741f482df44a6584b848549f368772_Out_2_Float);
float _Dither_22e77e4623774a7fa94d00b2ae4ff606_Out_2_Float;
Unity_Dither_float(_Multiply_38741f482df44a6584b848549f368772_Out_2_Float, float4(IN.NDCPosition.xy, 0, 0), _Dither_22e77e4623774a7fa94d00b2ae4ff606_Out_2_Float);
float _Multiply_9214bd931fea4bc98db2b9a46d79c8f8_Out_2_Float;
Unity_Multiply_float_float(_Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float, _Dither_22e77e4623774a7fa94d00b2ae4ff606_Out_2_Float, _Multiply_9214bd931fea4bc98db2b9a46d79c8f8_Out_2_Float);
#if defined(_USE_DITHER)
float _UseDither_4bfe8155f1434a3d9ded012dece1de0e_Out_0_Float = _Multiply_9214bd931fea4bc98db2b9a46d79c8f8_Out_2_Float;
#else
float _UseDither_4bfe8155f1434a3d9ded012dece1de0e_Out_0_Float = _Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float;
#endif
surface.Alpha = _UseDither_4bfe8155f1434a3d9ded012dece1de0e_Out_0_Float;
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

    output.WorldSpaceNormal =                           TransformObjectToWorldNormal(input.normalOS);
    output.ObjectSpacePosition =                        input.positionOS;
    output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
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

    





    output.ObjectSpacePosition = TransformWorldToObject(input.positionWS);

    #if UNITY_UV_STARTS_AT_TOP
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #else
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #endif

    output.NDCPosition = output.PixelPosition.xy / _ScaledScreenParams.xy;
    output.NDCPosition.y = 1.0f - output.NDCPosition.y;

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
#pragma shader_feature_local _ _USE_DITHER
#pragma shader_feature_local _ _USE_GRADIENT_MAP
#pragma shader_feature_local _ _USE_VERTEX_COLOR

#if defined(_USE_DITHER) && defined(_USE_GRADIENT_MAP) && defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_DITHER) && defined(_USE_GRADIENT_MAP)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_DITHER) && defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_2
#elif defined(_USE_DITHER)
#define KEYWORD_PERMUTATION_3
#elif defined(_USE_GRADIENT_MAP) && defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_4
#elif defined(_USE_GRADIENT_MAP)
#define KEYWORD_PERMUTATION_5
#elif defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_6
#else
#define KEYWORD_PERMUTATION_7
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
#define VARYINGS_NEED_POSITION_WS
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
 float4 color : COLOR;
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
 float3 ObjectSpacePosition;
 float2 NDCPosition;
 float2 PixelPosition;
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 WorldSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 WorldSpacePosition;
 float4 uv0;
 float4 VertexColor;
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

float4 _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D_TexelSize;
float4 _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D_TexelSize;
float _Wind_Small_Scale;
float2 _Wind_Small_Speed;
float _Wind_Small_Intensity;
float _Wind_Large_Scale;
float2 _Wind_Large_Speed;
float4 _Color_Bottom;
float _Wind_Large_Intensity;
float _UV_Pivot_Lock_Strangth;
float _Dither_Bottom_Level;
float _Dither_Fade;
float4 _Foliage_Map_TexelSize;
float _Alpha_Clip;
float4 _Color_Tint;
float4 _Gradient_Map_TexelSize;
float4 _Color_Top;
float _Smoothness;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D);
TEXTURE2D(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D);
TEXTURE2D(_Foliage_Map);
SAMPLER(sampler_Foliage_Map);
TEXTURE2D(_Gradient_Map);
SAMPLER(sampler_Gradient_Map);

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

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Dither_float(float In, float4 ScreenPosition, out float Out)
{
    float2 uv = ScreenPosition.xy * _ScreenParams.xy;
    static const float DITHER_THRESHOLDS[16] =
    {
        1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
        13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
        4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
        16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
    };
    uint index = (uint(uv.x) % 4) * 4 + uint(uv.y) % 4;
    Out = In - DITHER_THRESHOLDS[index];
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
float _Property_ad4aa287fed64295a604d3291d1959a2_Out_0_Float = _Wind_Large_Scale;
float2 _Property_c2f76a94f588455e9b4c07b23d9ef2ff_Out_0_Vector2 = _Wind_Large_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_bad92adcb02c4fc28767432489546115;
_PositionalUVs_bad92adcb02c4fc28767432489546115.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_bad92adcb02c4fc28767432489546115.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_bad92adcb02c4fc28767432489546115_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_ad4aa287fed64295a604d3291d1959a2_Out_0_Float, _Property_c2f76a94f588455e9b4c07b23d9ef2ff_Out_0_Vector2, _PositionalUVs_bad92adcb02c4fc28767432489546115, _PositionalUVs_bad92adcb02c4fc28767432489546115_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_bad92adcb02c4fc28767432489546115_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_R_5_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_G_6_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_B_7_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_A_8_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.a;
float _Subtract_218acd7847974a5cbb60429ba8096400_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_R_5_Float, float(0.5), _Subtract_218acd7847974a5cbb60429ba8096400_Out_2_Float);
float _Split_9060b9716ec549aeacc51b6fd9846e1f_R_1_Float = IN.VertexColor[0];
float _Split_9060b9716ec549aeacc51b6fd9846e1f_G_2_Float = IN.VertexColor[1];
float _Split_9060b9716ec549aeacc51b6fd9846e1f_B_3_Float = IN.VertexColor[2];
float _Split_9060b9716ec549aeacc51b6fd9846e1f_A_4_Float = IN.VertexColor[3];
float4 _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4 = IN.uv0;
float _Split_cd4e6782516b4c4eba58883246f59068_R_1_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[0];
float _Split_cd4e6782516b4c4eba58883246f59068_G_2_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[1];
float _Split_cd4e6782516b4c4eba58883246f59068_B_3_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[2];
float _Split_cd4e6782516b4c4eba58883246f59068_A_4_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[3];
float _Property_8494919c92594e95b9e93cc374ab477d_Out_0_Float = _UV_Pivot_Lock_Strangth;
float _Power_a22a61dbae424f388acc9bbf24d84825_Out_2_Float;
Unity_Power_float(_Split_cd4e6782516b4c4eba58883246f59068_G_2_Float, _Property_8494919c92594e95b9e93cc374ab477d_Out_0_Float, _Power_a22a61dbae424f388acc9bbf24d84825_Out_2_Float);
#if defined(_USE_VERTEX_COLOR)
float _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float = _Split_9060b9716ec549aeacc51b6fd9846e1f_R_1_Float;
#else
float _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float = _Power_a22a61dbae424f388acc9bbf24d84825_Out_2_Float;
#endif
float _Multiply_ea0e0a96337840389ee116bcbf36e668_Out_2_Float;
Unity_Multiply_float_float(_Subtract_218acd7847974a5cbb60429ba8096400_Out_2_Float, _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float, _Multiply_ea0e0a96337840389ee116bcbf36e668_Out_2_Float);
float _Property_dbbb11cdd2654ae79939bef8b5ecd875_Out_0_Float = _Wind_Large_Intensity;
float _Multiply_6869889d47d046e8b5ad71f9c3a9adb4_Out_2_Float;
Unity_Multiply_float_float(_Multiply_ea0e0a96337840389ee116bcbf36e668_Out_2_Float, _Property_dbbb11cdd2654ae79939bef8b5ecd875_Out_0_Float, _Multiply_6869889d47d046e8b5ad71f9c3a9adb4_Out_2_Float);
float _Property_c327c41c045b40c483eb7da471e0c3fd_Out_0_Float = _Wind_Small_Scale;
float2 _Property_92349f3d399742e5b93d3ec2912b6d0b_Out_0_Vector2 = _Wind_Small_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_61edec0a11644d95b38796bd3e28715a;
_PositionalUVs_61edec0a11644d95b38796bd3e28715a.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_61edec0a11644d95b38796bd3e28715a.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_61edec0a11644d95b38796bd3e28715a_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_c327c41c045b40c483eb7da471e0c3fd_Out_0_Float, _Property_92349f3d399742e5b93d3ec2912b6d0b_Out_0_Vector2, _PositionalUVs_61edec0a11644d95b38796bd3e28715a, _PositionalUVs_61edec0a11644d95b38796bd3e28715a_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_61edec0a11644d95b38796bd3e28715a_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_R_5_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_G_6_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_B_7_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_A_8_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.a;
float _Subtract_38999ff8e5a04271b3794daa96fc9cca_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_R_5_Float, float(0.5), _Subtract_38999ff8e5a04271b3794daa96fc9cca_Out_2_Float);
float _Multiply_4209b0f1a88b424992ddafb6610c2968_Out_2_Float;
Unity_Multiply_float_float(_UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float, _Subtract_38999ff8e5a04271b3794daa96fc9cca_Out_2_Float, _Multiply_4209b0f1a88b424992ddafb6610c2968_Out_2_Float);
float _Property_2bd20e0e45834464a28f6379b9e8225c_Out_0_Float = _Wind_Small_Intensity;
float _Multiply_1c0b4b06455e4d31b5ad6f8c28d20004_Out_2_Float;
Unity_Multiply_float_float(_Multiply_4209b0f1a88b424992ddafb6610c2968_Out_2_Float, _Property_2bd20e0e45834464a28f6379b9e8225c_Out_0_Float, _Multiply_1c0b4b06455e4d31b5ad6f8c28d20004_Out_2_Float);
float _Add_0407a76ed876497f9c558311ba6b5eb8_Out_2_Float;
Unity_Add_float(_Multiply_6869889d47d046e8b5ad71f9c3a9adb4_Out_2_Float, _Multiply_1c0b4b06455e4d31b5ad6f8c28d20004_Out_2_Float, _Add_0407a76ed876497f9c558311ba6b5eb8_Out_2_Float);
float3 _Multiply_5d424720302c4e55afef4b8a9f76d3ef_Out_2_Vector3;
Unity_Multiply_float3_float3((_Add_0407a76ed876497f9c558311ba6b5eb8_Out_2_Float.xxx), IN.WorldSpaceNormal, _Multiply_5d424720302c4e55afef4b8a9f76d3ef_Out_2_Vector3);
float3 _Add_0a8e7ddaf6aa4278a2f9912909eba29d_Out_2_Vector3;
Unity_Add_float3(_Multiply_5d424720302c4e55afef4b8a9f76d3ef_Out_2_Vector3, IN.WorldSpacePosition, _Add_0a8e7ddaf6aa4278a2f9912909eba29d_Out_2_Vector3);
float3 _UPGFoliageBendWSCustomFunction_3d8d6250d27b4f3b96cbc7767f9ae45d_offsetWS_2_Vector3;
UPG_FoliageBendWS_float(IN.WorldSpacePosition, _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float, _UPGFoliageBendWSCustomFunction_3d8d6250d27b4f3b96cbc7767f9ae45d_offsetWS_2_Vector3);
float3 _Add_a2f1ce3eccd04a348677bce71231c28f_Out_2_Vector3;
Unity_Add_float3(_Add_0a8e7ddaf6aa4278a2f9912909eba29d_Out_2_Vector3, _UPGFoliageBendWSCustomFunction_3d8d6250d27b4f3b96cbc7767f9ae45d_offsetWS_2_Vector3, _Add_a2f1ce3eccd04a348677bce71231c28f_Out_2_Vector3);
float3 _Transform_cd30c5b4b2e5405bbe4451c6c589718b_Out_1_Vector3;
_Transform_cd30c5b4b2e5405bbe4451c6c589718b_Out_1_Vector3 = TransformWorldToObject(_Add_a2f1ce3eccd04a348677bce71231c28f_Out_2_Vector3.xyz);
description.Position = _Transform_cd30c5b4b2e5405bbe4451c6c589718b_Out_1_Vector3;
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
UnityTexture2D _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Foliage_Map, sampler_Foliage_Map, _Foliage_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.tex, _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.samplerstate, _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4, _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_R_4_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.r;
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_G_5_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.g;
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_B_6_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.b;
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_A_7_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.a;
float _Property_ce8925e5051d4745a4cf3b2d4fb8db48_Out_0_Float = _Alpha_Clip;
float _Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_A_7_Float, _Property_ce8925e5051d4745a4cf3b2d4fb8db48_Out_0_Float, _Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float);
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_A_4_Float = 0;
float _Property_496eeb327965485cb724cb0c285f7a6c_Out_0_Float = _Dither_Bottom_Level;
float _Add_9a95e02d9c734b69a1bd64099eab897d_Out_2_Float;
Unity_Add_float(_Split_b2c67f57ffb14ffab00206eeffb0e2d5_G_2_Float, _Property_496eeb327965485cb724cb0c285f7a6c_Out_0_Float, _Add_9a95e02d9c734b69a1bd64099eab897d_Out_2_Float);
float _Property_ccb0e250db5745fe928b0ac7bf4d8af7_Out_0_Float = _Dither_Fade;
float _Multiply_38741f482df44a6584b848549f368772_Out_2_Float;
Unity_Multiply_float_float(_Add_9a95e02d9c734b69a1bd64099eab897d_Out_2_Float, _Property_ccb0e250db5745fe928b0ac7bf4d8af7_Out_0_Float, _Multiply_38741f482df44a6584b848549f368772_Out_2_Float);
float _Dither_22e77e4623774a7fa94d00b2ae4ff606_Out_2_Float;
Unity_Dither_float(_Multiply_38741f482df44a6584b848549f368772_Out_2_Float, float4(IN.NDCPosition.xy, 0, 0), _Dither_22e77e4623774a7fa94d00b2ae4ff606_Out_2_Float);
float _Multiply_9214bd931fea4bc98db2b9a46d79c8f8_Out_2_Float;
Unity_Multiply_float_float(_Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float, _Dither_22e77e4623774a7fa94d00b2ae4ff606_Out_2_Float, _Multiply_9214bd931fea4bc98db2b9a46d79c8f8_Out_2_Float);
#if defined(_USE_DITHER)
float _UseDither_4bfe8155f1434a3d9ded012dece1de0e_Out_0_Float = _Multiply_9214bd931fea4bc98db2b9a46d79c8f8_Out_2_Float;
#else
float _UseDither_4bfe8155f1434a3d9ded012dece1de0e_Out_0_Float = _Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float;
#endif
surface.Alpha = _UseDither_4bfe8155f1434a3d9ded012dece1de0e_Out_0_Float;
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
    output.WorldSpaceNormal =                           TransformObjectToWorldNormal(input.normalOS);
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
    output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
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

    





    output.ObjectSpacePosition = TransformWorldToObject(input.positionWS);

    #if UNITY_UV_STARTS_AT_TOP
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #else
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #endif

    output.NDCPosition = output.PixelPosition.xy / _ScaledScreenParams.xy;
    output.NDCPosition.y = 1.0f - output.NDCPosition.y;

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
#pragma shader_feature_local _ _USE_DITHER
#pragma shader_feature_local _ _USE_GRADIENT_MAP
#pragma shader_feature_local _ _USE_VERTEX_COLOR

#if defined(_USE_DITHER) && defined(_USE_GRADIENT_MAP) && defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_DITHER) && defined(_USE_GRADIENT_MAP)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_DITHER) && defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_2
#elif defined(_USE_DITHER)
#define KEYWORD_PERMUTATION_3
#elif defined(_USE_GRADIENT_MAP) && defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_4
#elif defined(_USE_GRADIENT_MAP)
#define KEYWORD_PERMUTATION_5
#elif defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_6
#else
#define KEYWORD_PERMUTATION_7
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
#define VARYINGS_NEED_POSITION_WS
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
 float3 ObjectSpacePosition;
 float2 NDCPosition;
 float2 PixelPosition;
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 WorldSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 WorldSpacePosition;
 float4 uv0;
 float4 VertexColor;
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

float4 _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D_TexelSize;
float4 _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D_TexelSize;
float _Wind_Small_Scale;
float2 _Wind_Small_Speed;
float _Wind_Small_Intensity;
float _Wind_Large_Scale;
float2 _Wind_Large_Speed;
float4 _Color_Bottom;
float _Wind_Large_Intensity;
float _UV_Pivot_Lock_Strangth;
float _Dither_Bottom_Level;
float _Dither_Fade;
float4 _Foliage_Map_TexelSize;
float _Alpha_Clip;
float4 _Color_Tint;
float4 _Gradient_Map_TexelSize;
float4 _Color_Top;
float _Smoothness;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D);
TEXTURE2D(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D);
TEXTURE2D(_Foliage_Map);
SAMPLER(sampler_Foliage_Map);
TEXTURE2D(_Gradient_Map);
SAMPLER(sampler_Gradient_Map);

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

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Dither_float(float In, float4 ScreenPosition, out float Out)
{
    float2 uv = ScreenPosition.xy * _ScreenParams.xy;
    static const float DITHER_THRESHOLDS[16] =
    {
        1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
        13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
        4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
        16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
    };
    uint index = (uint(uv.x) % 4) * 4 + uint(uv.y) % 4;
    Out = In - DITHER_THRESHOLDS[index];
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
float _Property_ad4aa287fed64295a604d3291d1959a2_Out_0_Float = _Wind_Large_Scale;
float2 _Property_c2f76a94f588455e9b4c07b23d9ef2ff_Out_0_Vector2 = _Wind_Large_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_bad92adcb02c4fc28767432489546115;
_PositionalUVs_bad92adcb02c4fc28767432489546115.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_bad92adcb02c4fc28767432489546115.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_bad92adcb02c4fc28767432489546115_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_ad4aa287fed64295a604d3291d1959a2_Out_0_Float, _Property_c2f76a94f588455e9b4c07b23d9ef2ff_Out_0_Vector2, _PositionalUVs_bad92adcb02c4fc28767432489546115, _PositionalUVs_bad92adcb02c4fc28767432489546115_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_bad92adcb02c4fc28767432489546115_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_R_5_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_G_6_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_B_7_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_A_8_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.a;
float _Subtract_218acd7847974a5cbb60429ba8096400_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_R_5_Float, float(0.5), _Subtract_218acd7847974a5cbb60429ba8096400_Out_2_Float);
float _Split_9060b9716ec549aeacc51b6fd9846e1f_R_1_Float = IN.VertexColor[0];
float _Split_9060b9716ec549aeacc51b6fd9846e1f_G_2_Float = IN.VertexColor[1];
float _Split_9060b9716ec549aeacc51b6fd9846e1f_B_3_Float = IN.VertexColor[2];
float _Split_9060b9716ec549aeacc51b6fd9846e1f_A_4_Float = IN.VertexColor[3];
float4 _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4 = IN.uv0;
float _Split_cd4e6782516b4c4eba58883246f59068_R_1_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[0];
float _Split_cd4e6782516b4c4eba58883246f59068_G_2_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[1];
float _Split_cd4e6782516b4c4eba58883246f59068_B_3_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[2];
float _Split_cd4e6782516b4c4eba58883246f59068_A_4_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[3];
float _Property_8494919c92594e95b9e93cc374ab477d_Out_0_Float = _UV_Pivot_Lock_Strangth;
float _Power_a22a61dbae424f388acc9bbf24d84825_Out_2_Float;
Unity_Power_float(_Split_cd4e6782516b4c4eba58883246f59068_G_2_Float, _Property_8494919c92594e95b9e93cc374ab477d_Out_0_Float, _Power_a22a61dbae424f388acc9bbf24d84825_Out_2_Float);
#if defined(_USE_VERTEX_COLOR)
float _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float = _Split_9060b9716ec549aeacc51b6fd9846e1f_R_1_Float;
#else
float _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float = _Power_a22a61dbae424f388acc9bbf24d84825_Out_2_Float;
#endif
float _Multiply_ea0e0a96337840389ee116bcbf36e668_Out_2_Float;
Unity_Multiply_float_float(_Subtract_218acd7847974a5cbb60429ba8096400_Out_2_Float, _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float, _Multiply_ea0e0a96337840389ee116bcbf36e668_Out_2_Float);
float _Property_dbbb11cdd2654ae79939bef8b5ecd875_Out_0_Float = _Wind_Large_Intensity;
float _Multiply_6869889d47d046e8b5ad71f9c3a9adb4_Out_2_Float;
Unity_Multiply_float_float(_Multiply_ea0e0a96337840389ee116bcbf36e668_Out_2_Float, _Property_dbbb11cdd2654ae79939bef8b5ecd875_Out_0_Float, _Multiply_6869889d47d046e8b5ad71f9c3a9adb4_Out_2_Float);
float _Property_c327c41c045b40c483eb7da471e0c3fd_Out_0_Float = _Wind_Small_Scale;
float2 _Property_92349f3d399742e5b93d3ec2912b6d0b_Out_0_Vector2 = _Wind_Small_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_61edec0a11644d95b38796bd3e28715a;
_PositionalUVs_61edec0a11644d95b38796bd3e28715a.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_61edec0a11644d95b38796bd3e28715a.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_61edec0a11644d95b38796bd3e28715a_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_c327c41c045b40c483eb7da471e0c3fd_Out_0_Float, _Property_92349f3d399742e5b93d3ec2912b6d0b_Out_0_Vector2, _PositionalUVs_61edec0a11644d95b38796bd3e28715a, _PositionalUVs_61edec0a11644d95b38796bd3e28715a_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_61edec0a11644d95b38796bd3e28715a_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_R_5_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_G_6_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_B_7_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_A_8_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.a;
float _Subtract_38999ff8e5a04271b3794daa96fc9cca_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_R_5_Float, float(0.5), _Subtract_38999ff8e5a04271b3794daa96fc9cca_Out_2_Float);
float _Multiply_4209b0f1a88b424992ddafb6610c2968_Out_2_Float;
Unity_Multiply_float_float(_UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float, _Subtract_38999ff8e5a04271b3794daa96fc9cca_Out_2_Float, _Multiply_4209b0f1a88b424992ddafb6610c2968_Out_2_Float);
float _Property_2bd20e0e45834464a28f6379b9e8225c_Out_0_Float = _Wind_Small_Intensity;
float _Multiply_1c0b4b06455e4d31b5ad6f8c28d20004_Out_2_Float;
Unity_Multiply_float_float(_Multiply_4209b0f1a88b424992ddafb6610c2968_Out_2_Float, _Property_2bd20e0e45834464a28f6379b9e8225c_Out_0_Float, _Multiply_1c0b4b06455e4d31b5ad6f8c28d20004_Out_2_Float);
float _Add_0407a76ed876497f9c558311ba6b5eb8_Out_2_Float;
Unity_Add_float(_Multiply_6869889d47d046e8b5ad71f9c3a9adb4_Out_2_Float, _Multiply_1c0b4b06455e4d31b5ad6f8c28d20004_Out_2_Float, _Add_0407a76ed876497f9c558311ba6b5eb8_Out_2_Float);
float3 _Multiply_5d424720302c4e55afef4b8a9f76d3ef_Out_2_Vector3;
Unity_Multiply_float3_float3((_Add_0407a76ed876497f9c558311ba6b5eb8_Out_2_Float.xxx), IN.WorldSpaceNormal, _Multiply_5d424720302c4e55afef4b8a9f76d3ef_Out_2_Vector3);
float3 _Add_0a8e7ddaf6aa4278a2f9912909eba29d_Out_2_Vector3;
Unity_Add_float3(_Multiply_5d424720302c4e55afef4b8a9f76d3ef_Out_2_Vector3, IN.WorldSpacePosition, _Add_0a8e7ddaf6aa4278a2f9912909eba29d_Out_2_Vector3);
float3 _UPGFoliageBendWSCustomFunction_3d8d6250d27b4f3b96cbc7767f9ae45d_offsetWS_2_Vector3;
UPG_FoliageBendWS_float(IN.WorldSpacePosition, _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float, _UPGFoliageBendWSCustomFunction_3d8d6250d27b4f3b96cbc7767f9ae45d_offsetWS_2_Vector3);
float3 _Add_a2f1ce3eccd04a348677bce71231c28f_Out_2_Vector3;
Unity_Add_float3(_Add_0a8e7ddaf6aa4278a2f9912909eba29d_Out_2_Vector3, _UPGFoliageBendWSCustomFunction_3d8d6250d27b4f3b96cbc7767f9ae45d_offsetWS_2_Vector3, _Add_a2f1ce3eccd04a348677bce71231c28f_Out_2_Vector3);
float3 _Transform_cd30c5b4b2e5405bbe4451c6c589718b_Out_1_Vector3;
_Transform_cd30c5b4b2e5405bbe4451c6c589718b_Out_1_Vector3 = TransformWorldToObject(_Add_a2f1ce3eccd04a348677bce71231c28f_Out_2_Vector3.xyz);
description.Position = _Transform_cd30c5b4b2e5405bbe4451c6c589718b_Out_1_Vector3;
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
UnityTexture2D _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Foliage_Map, sampler_Foliage_Map, _Foliage_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.tex, _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.samplerstate, _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4, _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_R_4_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.r;
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_G_5_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.g;
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_B_6_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.b;
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_A_7_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.a;
float _Property_ce8925e5051d4745a4cf3b2d4fb8db48_Out_0_Float = _Alpha_Clip;
float _Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_A_7_Float, _Property_ce8925e5051d4745a4cf3b2d4fb8db48_Out_0_Float, _Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float);
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_A_4_Float = 0;
float _Property_496eeb327965485cb724cb0c285f7a6c_Out_0_Float = _Dither_Bottom_Level;
float _Add_9a95e02d9c734b69a1bd64099eab897d_Out_2_Float;
Unity_Add_float(_Split_b2c67f57ffb14ffab00206eeffb0e2d5_G_2_Float, _Property_496eeb327965485cb724cb0c285f7a6c_Out_0_Float, _Add_9a95e02d9c734b69a1bd64099eab897d_Out_2_Float);
float _Property_ccb0e250db5745fe928b0ac7bf4d8af7_Out_0_Float = _Dither_Fade;
float _Multiply_38741f482df44a6584b848549f368772_Out_2_Float;
Unity_Multiply_float_float(_Add_9a95e02d9c734b69a1bd64099eab897d_Out_2_Float, _Property_ccb0e250db5745fe928b0ac7bf4d8af7_Out_0_Float, _Multiply_38741f482df44a6584b848549f368772_Out_2_Float);
float _Dither_22e77e4623774a7fa94d00b2ae4ff606_Out_2_Float;
Unity_Dither_float(_Multiply_38741f482df44a6584b848549f368772_Out_2_Float, float4(IN.NDCPosition.xy, 0, 0), _Dither_22e77e4623774a7fa94d00b2ae4ff606_Out_2_Float);
float _Multiply_9214bd931fea4bc98db2b9a46d79c8f8_Out_2_Float;
Unity_Multiply_float_float(_Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float, _Dither_22e77e4623774a7fa94d00b2ae4ff606_Out_2_Float, _Multiply_9214bd931fea4bc98db2b9a46d79c8f8_Out_2_Float);
#if defined(_USE_DITHER)
float _UseDither_4bfe8155f1434a3d9ded012dece1de0e_Out_0_Float = _Multiply_9214bd931fea4bc98db2b9a46d79c8f8_Out_2_Float;
#else
float _UseDither_4bfe8155f1434a3d9ded012dece1de0e_Out_0_Float = _Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float;
#endif
surface.NormalTS = IN.TangentSpaceNormal;
surface.Alpha = _UseDither_4bfe8155f1434a3d9ded012dece1de0e_Out_0_Float;
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
    output.WorldSpaceNormal =                           TransformObjectToWorldNormal(input.normalOS);
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
    output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
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


    output.ObjectSpacePosition = TransformWorldToObject(input.positionWS);

    #if UNITY_UV_STARTS_AT_TOP
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #else
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #endif

    output.NDCPosition = output.PixelPosition.xy / _ScaledScreenParams.xy;
    output.NDCPosition.y = 1.0f - output.NDCPosition.y;

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
#pragma shader_feature_local _ _USE_DITHER
#pragma shader_feature_local _ _USE_GRADIENT_MAP
#pragma shader_feature_local _ _USE_VERTEX_COLOR

#if defined(_USE_DITHER) && defined(_USE_GRADIENT_MAP) && defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_DITHER) && defined(_USE_GRADIENT_MAP)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_DITHER) && defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_2
#elif defined(_USE_DITHER)
#define KEYWORD_PERMUTATION_3
#elif defined(_USE_GRADIENT_MAP) && defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_4
#elif defined(_USE_GRADIENT_MAP)
#define KEYWORD_PERMUTATION_5
#elif defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_6
#else
#define KEYWORD_PERMUTATION_7
#endif


// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_TEXCOORD1
#define ATTRIBUTES_NEED_TEXCOORD2
#define ATTRIBUTES_NEED_COLOR
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
 float4 color : COLOR;
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
 float3 ObjectSpacePosition;
 float2 NDCPosition;
 float2 PixelPosition;
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 WorldSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 WorldSpacePosition;
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

float4 _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D_TexelSize;
float4 _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D_TexelSize;
float _Wind_Small_Scale;
float2 _Wind_Small_Speed;
float _Wind_Small_Intensity;
float _Wind_Large_Scale;
float2 _Wind_Large_Speed;
float4 _Color_Bottom;
float _Wind_Large_Intensity;
float _UV_Pivot_Lock_Strangth;
float _Dither_Bottom_Level;
float _Dither_Fade;
float4 _Foliage_Map_TexelSize;
float _Alpha_Clip;
float4 _Color_Tint;
float4 _Gradient_Map_TexelSize;
float4 _Color_Top;
float _Smoothness;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D);
TEXTURE2D(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D);
TEXTURE2D(_Foliage_Map);
SAMPLER(sampler_Foliage_Map);
TEXTURE2D(_Gradient_Map);
SAMPLER(sampler_Gradient_Map);

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

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

void Unity_Dither_float(float In, float4 ScreenPosition, out float Out)
{
    float2 uv = ScreenPosition.xy * _ScreenParams.xy;
    static const float DITHER_THRESHOLDS[16] =
    {
        1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
        13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
        4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
        16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
    };
    uint index = (uint(uv.x) % 4) * 4 + uint(uv.y) % 4;
    Out = In - DITHER_THRESHOLDS[index];
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
float _Property_ad4aa287fed64295a604d3291d1959a2_Out_0_Float = _Wind_Large_Scale;
float2 _Property_c2f76a94f588455e9b4c07b23d9ef2ff_Out_0_Vector2 = _Wind_Large_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_bad92adcb02c4fc28767432489546115;
_PositionalUVs_bad92adcb02c4fc28767432489546115.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_bad92adcb02c4fc28767432489546115.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_bad92adcb02c4fc28767432489546115_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_ad4aa287fed64295a604d3291d1959a2_Out_0_Float, _Property_c2f76a94f588455e9b4c07b23d9ef2ff_Out_0_Vector2, _PositionalUVs_bad92adcb02c4fc28767432489546115, _PositionalUVs_bad92adcb02c4fc28767432489546115_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_bad92adcb02c4fc28767432489546115_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_R_5_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_G_6_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_B_7_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_A_8_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.a;
float _Subtract_218acd7847974a5cbb60429ba8096400_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_R_5_Float, float(0.5), _Subtract_218acd7847974a5cbb60429ba8096400_Out_2_Float);
float _Split_9060b9716ec549aeacc51b6fd9846e1f_R_1_Float = IN.VertexColor[0];
float _Split_9060b9716ec549aeacc51b6fd9846e1f_G_2_Float = IN.VertexColor[1];
float _Split_9060b9716ec549aeacc51b6fd9846e1f_B_3_Float = IN.VertexColor[2];
float _Split_9060b9716ec549aeacc51b6fd9846e1f_A_4_Float = IN.VertexColor[3];
float4 _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4 = IN.uv0;
float _Split_cd4e6782516b4c4eba58883246f59068_R_1_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[0];
float _Split_cd4e6782516b4c4eba58883246f59068_G_2_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[1];
float _Split_cd4e6782516b4c4eba58883246f59068_B_3_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[2];
float _Split_cd4e6782516b4c4eba58883246f59068_A_4_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[3];
float _Property_8494919c92594e95b9e93cc374ab477d_Out_0_Float = _UV_Pivot_Lock_Strangth;
float _Power_a22a61dbae424f388acc9bbf24d84825_Out_2_Float;
Unity_Power_float(_Split_cd4e6782516b4c4eba58883246f59068_G_2_Float, _Property_8494919c92594e95b9e93cc374ab477d_Out_0_Float, _Power_a22a61dbae424f388acc9bbf24d84825_Out_2_Float);
#if defined(_USE_VERTEX_COLOR)
float _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float = _Split_9060b9716ec549aeacc51b6fd9846e1f_R_1_Float;
#else
float _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float = _Power_a22a61dbae424f388acc9bbf24d84825_Out_2_Float;
#endif
float _Multiply_ea0e0a96337840389ee116bcbf36e668_Out_2_Float;
Unity_Multiply_float_float(_Subtract_218acd7847974a5cbb60429ba8096400_Out_2_Float, _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float, _Multiply_ea0e0a96337840389ee116bcbf36e668_Out_2_Float);
float _Property_dbbb11cdd2654ae79939bef8b5ecd875_Out_0_Float = _Wind_Large_Intensity;
float _Multiply_6869889d47d046e8b5ad71f9c3a9adb4_Out_2_Float;
Unity_Multiply_float_float(_Multiply_ea0e0a96337840389ee116bcbf36e668_Out_2_Float, _Property_dbbb11cdd2654ae79939bef8b5ecd875_Out_0_Float, _Multiply_6869889d47d046e8b5ad71f9c3a9adb4_Out_2_Float);
float _Property_c327c41c045b40c483eb7da471e0c3fd_Out_0_Float = _Wind_Small_Scale;
float2 _Property_92349f3d399742e5b93d3ec2912b6d0b_Out_0_Vector2 = _Wind_Small_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_61edec0a11644d95b38796bd3e28715a;
_PositionalUVs_61edec0a11644d95b38796bd3e28715a.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_61edec0a11644d95b38796bd3e28715a.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_61edec0a11644d95b38796bd3e28715a_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_c327c41c045b40c483eb7da471e0c3fd_Out_0_Float, _Property_92349f3d399742e5b93d3ec2912b6d0b_Out_0_Vector2, _PositionalUVs_61edec0a11644d95b38796bd3e28715a, _PositionalUVs_61edec0a11644d95b38796bd3e28715a_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_61edec0a11644d95b38796bd3e28715a_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_R_5_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_G_6_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_B_7_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_A_8_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.a;
float _Subtract_38999ff8e5a04271b3794daa96fc9cca_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_R_5_Float, float(0.5), _Subtract_38999ff8e5a04271b3794daa96fc9cca_Out_2_Float);
float _Multiply_4209b0f1a88b424992ddafb6610c2968_Out_2_Float;
Unity_Multiply_float_float(_UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float, _Subtract_38999ff8e5a04271b3794daa96fc9cca_Out_2_Float, _Multiply_4209b0f1a88b424992ddafb6610c2968_Out_2_Float);
float _Property_2bd20e0e45834464a28f6379b9e8225c_Out_0_Float = _Wind_Small_Intensity;
float _Multiply_1c0b4b06455e4d31b5ad6f8c28d20004_Out_2_Float;
Unity_Multiply_float_float(_Multiply_4209b0f1a88b424992ddafb6610c2968_Out_2_Float, _Property_2bd20e0e45834464a28f6379b9e8225c_Out_0_Float, _Multiply_1c0b4b06455e4d31b5ad6f8c28d20004_Out_2_Float);
float _Add_0407a76ed876497f9c558311ba6b5eb8_Out_2_Float;
Unity_Add_float(_Multiply_6869889d47d046e8b5ad71f9c3a9adb4_Out_2_Float, _Multiply_1c0b4b06455e4d31b5ad6f8c28d20004_Out_2_Float, _Add_0407a76ed876497f9c558311ba6b5eb8_Out_2_Float);
float3 _Multiply_5d424720302c4e55afef4b8a9f76d3ef_Out_2_Vector3;
Unity_Multiply_float3_float3((_Add_0407a76ed876497f9c558311ba6b5eb8_Out_2_Float.xxx), IN.WorldSpaceNormal, _Multiply_5d424720302c4e55afef4b8a9f76d3ef_Out_2_Vector3);
float3 _Add_0a8e7ddaf6aa4278a2f9912909eba29d_Out_2_Vector3;
Unity_Add_float3(_Multiply_5d424720302c4e55afef4b8a9f76d3ef_Out_2_Vector3, IN.WorldSpacePosition, _Add_0a8e7ddaf6aa4278a2f9912909eba29d_Out_2_Vector3);
float3 _UPGFoliageBendWSCustomFunction_3d8d6250d27b4f3b96cbc7767f9ae45d_offsetWS_2_Vector3;
UPG_FoliageBendWS_float(IN.WorldSpacePosition, _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float, _UPGFoliageBendWSCustomFunction_3d8d6250d27b4f3b96cbc7767f9ae45d_offsetWS_2_Vector3);
float3 _Add_a2f1ce3eccd04a348677bce71231c28f_Out_2_Vector3;
Unity_Add_float3(_Add_0a8e7ddaf6aa4278a2f9912909eba29d_Out_2_Vector3, _UPGFoliageBendWSCustomFunction_3d8d6250d27b4f3b96cbc7767f9ae45d_offsetWS_2_Vector3, _Add_a2f1ce3eccd04a348677bce71231c28f_Out_2_Vector3);
float3 _Transform_cd30c5b4b2e5405bbe4451c6c589718b_Out_1_Vector3;
_Transform_cd30c5b4b2e5405bbe4451c6c589718b_Out_1_Vector3 = TransformWorldToObject(_Add_a2f1ce3eccd04a348677bce71231c28f_Out_2_Vector3.xyz);
description.Position = _Transform_cd30c5b4b2e5405bbe4451c6c589718b_Out_1_Vector3;
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
float4 _Property_597353718570472cb903eabbac10e98e_Out_0_Vector4 = _Color_Bottom;
float4 _Property_acf5ade3ddc74df2bb7b72f3fb5027e6_Out_0_Vector4 = _Color_Top;
UnityTexture2D _Property_0dd8d95b31054753b15cbea49c4dd049_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Gradient_Map, sampler_Gradient_Map, _Gradient_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_0dd8d95b31054753b15cbea49c4dd049_Out_0_Texture2D.tex, _Property_0dd8d95b31054753b15cbea49c4dd049_Out_0_Texture2D.samplerstate, _Property_0dd8d95b31054753b15cbea49c4dd049_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_0dd8d95b31054753b15cbea49c4dd049_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4, _Property_0dd8d95b31054753b15cbea49c4dd049_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_R_4_Float = _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4.r;
float _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_G_5_Float = _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4.g;
float _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_B_6_Float = _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4.b;
float _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_A_7_Float = _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4.a;
float4 _Lerp_45e933526a9e47429463b3d3ae7bcb99_Out_3_Vector4;
Unity_Lerp_float4(_Property_597353718570472cb903eabbac10e98e_Out_0_Vector4, _Property_acf5ade3ddc74df2bb7b72f3fb5027e6_Out_0_Vector4, (_SampleTexture2D_0139bf195dc74dea98a5142201287ca0_R_4_Float.xxxx), _Lerp_45e933526a9e47429463b3d3ae7bcb99_Out_3_Vector4);
float4 _Property_d51fad0917634f61855f006ac3a3036a_Out_0_Vector4 = _Color_Tint;
#if defined(_USE_GRADIENT_MAP)
float4 _UseGradientMap_ab913e91a75042bfa88ab1093a041f33_Out_0_Vector4 = _Lerp_45e933526a9e47429463b3d3ae7bcb99_Out_3_Vector4;
#else
float4 _UseGradientMap_ab913e91a75042bfa88ab1093a041f33_Out_0_Vector4 = _Property_d51fad0917634f61855f006ac3a3036a_Out_0_Vector4;
#endif
UnityTexture2D _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Foliage_Map, sampler_Foliage_Map, _Foliage_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.tex, _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.samplerstate, _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4, _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_R_4_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.r;
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_G_5_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.g;
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_B_6_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.b;
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_A_7_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.a;
float4 _Multiply_2d1489dbc4f84047b8a1330c23f185d7_Out_2_Vector4;
Unity_Multiply_float4_float4(_UseGradientMap_ab913e91a75042bfa88ab1093a041f33_Out_0_Vector4, _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4, _Multiply_2d1489dbc4f84047b8a1330c23f185d7_Out_2_Vector4);
float _Property_ce8925e5051d4745a4cf3b2d4fb8db48_Out_0_Float = _Alpha_Clip;
float _Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_A_7_Float, _Property_ce8925e5051d4745a4cf3b2d4fb8db48_Out_0_Float, _Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float);
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_A_4_Float = 0;
float _Property_496eeb327965485cb724cb0c285f7a6c_Out_0_Float = _Dither_Bottom_Level;
float _Add_9a95e02d9c734b69a1bd64099eab897d_Out_2_Float;
Unity_Add_float(_Split_b2c67f57ffb14ffab00206eeffb0e2d5_G_2_Float, _Property_496eeb327965485cb724cb0c285f7a6c_Out_0_Float, _Add_9a95e02d9c734b69a1bd64099eab897d_Out_2_Float);
float _Property_ccb0e250db5745fe928b0ac7bf4d8af7_Out_0_Float = _Dither_Fade;
float _Multiply_38741f482df44a6584b848549f368772_Out_2_Float;
Unity_Multiply_float_float(_Add_9a95e02d9c734b69a1bd64099eab897d_Out_2_Float, _Property_ccb0e250db5745fe928b0ac7bf4d8af7_Out_0_Float, _Multiply_38741f482df44a6584b848549f368772_Out_2_Float);
float _Dither_22e77e4623774a7fa94d00b2ae4ff606_Out_2_Float;
Unity_Dither_float(_Multiply_38741f482df44a6584b848549f368772_Out_2_Float, float4(IN.NDCPosition.xy, 0, 0), _Dither_22e77e4623774a7fa94d00b2ae4ff606_Out_2_Float);
float _Multiply_9214bd931fea4bc98db2b9a46d79c8f8_Out_2_Float;
Unity_Multiply_float_float(_Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float, _Dither_22e77e4623774a7fa94d00b2ae4ff606_Out_2_Float, _Multiply_9214bd931fea4bc98db2b9a46d79c8f8_Out_2_Float);
#if defined(_USE_DITHER)
float _UseDither_4bfe8155f1434a3d9ded012dece1de0e_Out_0_Float = _Multiply_9214bd931fea4bc98db2b9a46d79c8f8_Out_2_Float;
#else
float _UseDither_4bfe8155f1434a3d9ded012dece1de0e_Out_0_Float = _Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float;
#endif
surface.BaseColor = (_Multiply_2d1489dbc4f84047b8a1330c23f185d7_Out_2_Vector4.xyz);
surface.Emission = float3(0, 0, 0);
surface.Alpha = _UseDither_4bfe8155f1434a3d9ded012dece1de0e_Out_0_Float;
surface.AlphaClipThreshold = float(0.5);
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
    output.WorldSpaceNormal =                           TransformObjectToWorldNormal(input.normalOS);
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
    output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
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

    





    output.ObjectSpacePosition = TransformWorldToObject(input.positionWS);

    #if UNITY_UV_STARTS_AT_TOP
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #else
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #endif

    output.NDCPosition = output.PixelPosition.xy / _ScaledScreenParams.xy;
    output.NDCPosition.y = 1.0f - output.NDCPosition.y;

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
#pragma shader_feature_local _ _USE_DITHER
#pragma shader_feature_local _ _USE_GRADIENT_MAP
#pragma shader_feature_local _ _USE_VERTEX_COLOR

#if defined(_USE_DITHER) && defined(_USE_GRADIENT_MAP) && defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_DITHER) && defined(_USE_GRADIENT_MAP)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_DITHER) && defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_2
#elif defined(_USE_DITHER)
#define KEYWORD_PERMUTATION_3
#elif defined(_USE_GRADIENT_MAP) && defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_4
#elif defined(_USE_GRADIENT_MAP)
#define KEYWORD_PERMUTATION_5
#elif defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_6
#else
#define KEYWORD_PERMUTATION_7
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
#define VARYINGS_NEED_POSITION_WS
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
 float4 color : COLOR;
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
 float3 ObjectSpacePosition;
 float2 NDCPosition;
 float2 PixelPosition;
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 WorldSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 WorldSpacePosition;
 float4 uv0;
 float4 VertexColor;
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

float4 _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D_TexelSize;
float4 _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D_TexelSize;
float _Wind_Small_Scale;
float2 _Wind_Small_Speed;
float _Wind_Small_Intensity;
float _Wind_Large_Scale;
float2 _Wind_Large_Speed;
float4 _Color_Bottom;
float _Wind_Large_Intensity;
float _UV_Pivot_Lock_Strangth;
float _Dither_Bottom_Level;
float _Dither_Fade;
float4 _Foliage_Map_TexelSize;
float _Alpha_Clip;
float4 _Color_Tint;
float4 _Gradient_Map_TexelSize;
float4 _Color_Top;
float _Smoothness;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D);
TEXTURE2D(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D);
TEXTURE2D(_Foliage_Map);
SAMPLER(sampler_Foliage_Map);
TEXTURE2D(_Gradient_Map);
SAMPLER(sampler_Gradient_Map);

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

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Dither_float(float In, float4 ScreenPosition, out float Out)
{
    float2 uv = ScreenPosition.xy * _ScreenParams.xy;
    static const float DITHER_THRESHOLDS[16] =
    {
        1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
        13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
        4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
        16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
    };
    uint index = (uint(uv.x) % 4) * 4 + uint(uv.y) % 4;
    Out = In - DITHER_THRESHOLDS[index];
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
float _Property_ad4aa287fed64295a604d3291d1959a2_Out_0_Float = _Wind_Large_Scale;
float2 _Property_c2f76a94f588455e9b4c07b23d9ef2ff_Out_0_Vector2 = _Wind_Large_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_bad92adcb02c4fc28767432489546115;
_PositionalUVs_bad92adcb02c4fc28767432489546115.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_bad92adcb02c4fc28767432489546115.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_bad92adcb02c4fc28767432489546115_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_ad4aa287fed64295a604d3291d1959a2_Out_0_Float, _Property_c2f76a94f588455e9b4c07b23d9ef2ff_Out_0_Vector2, _PositionalUVs_bad92adcb02c4fc28767432489546115, _PositionalUVs_bad92adcb02c4fc28767432489546115_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_bad92adcb02c4fc28767432489546115_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_R_5_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_G_6_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_B_7_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_A_8_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.a;
float _Subtract_218acd7847974a5cbb60429ba8096400_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_R_5_Float, float(0.5), _Subtract_218acd7847974a5cbb60429ba8096400_Out_2_Float);
float _Split_9060b9716ec549aeacc51b6fd9846e1f_R_1_Float = IN.VertexColor[0];
float _Split_9060b9716ec549aeacc51b6fd9846e1f_G_2_Float = IN.VertexColor[1];
float _Split_9060b9716ec549aeacc51b6fd9846e1f_B_3_Float = IN.VertexColor[2];
float _Split_9060b9716ec549aeacc51b6fd9846e1f_A_4_Float = IN.VertexColor[3];
float4 _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4 = IN.uv0;
float _Split_cd4e6782516b4c4eba58883246f59068_R_1_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[0];
float _Split_cd4e6782516b4c4eba58883246f59068_G_2_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[1];
float _Split_cd4e6782516b4c4eba58883246f59068_B_3_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[2];
float _Split_cd4e6782516b4c4eba58883246f59068_A_4_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[3];
float _Property_8494919c92594e95b9e93cc374ab477d_Out_0_Float = _UV_Pivot_Lock_Strangth;
float _Power_a22a61dbae424f388acc9bbf24d84825_Out_2_Float;
Unity_Power_float(_Split_cd4e6782516b4c4eba58883246f59068_G_2_Float, _Property_8494919c92594e95b9e93cc374ab477d_Out_0_Float, _Power_a22a61dbae424f388acc9bbf24d84825_Out_2_Float);
#if defined(_USE_VERTEX_COLOR)
float _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float = _Split_9060b9716ec549aeacc51b6fd9846e1f_R_1_Float;
#else
float _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float = _Power_a22a61dbae424f388acc9bbf24d84825_Out_2_Float;
#endif
float _Multiply_ea0e0a96337840389ee116bcbf36e668_Out_2_Float;
Unity_Multiply_float_float(_Subtract_218acd7847974a5cbb60429ba8096400_Out_2_Float, _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float, _Multiply_ea0e0a96337840389ee116bcbf36e668_Out_2_Float);
float _Property_dbbb11cdd2654ae79939bef8b5ecd875_Out_0_Float = _Wind_Large_Intensity;
float _Multiply_6869889d47d046e8b5ad71f9c3a9adb4_Out_2_Float;
Unity_Multiply_float_float(_Multiply_ea0e0a96337840389ee116bcbf36e668_Out_2_Float, _Property_dbbb11cdd2654ae79939bef8b5ecd875_Out_0_Float, _Multiply_6869889d47d046e8b5ad71f9c3a9adb4_Out_2_Float);
float _Property_c327c41c045b40c483eb7da471e0c3fd_Out_0_Float = _Wind_Small_Scale;
float2 _Property_92349f3d399742e5b93d3ec2912b6d0b_Out_0_Vector2 = _Wind_Small_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_61edec0a11644d95b38796bd3e28715a;
_PositionalUVs_61edec0a11644d95b38796bd3e28715a.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_61edec0a11644d95b38796bd3e28715a.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_61edec0a11644d95b38796bd3e28715a_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_c327c41c045b40c483eb7da471e0c3fd_Out_0_Float, _Property_92349f3d399742e5b93d3ec2912b6d0b_Out_0_Vector2, _PositionalUVs_61edec0a11644d95b38796bd3e28715a, _PositionalUVs_61edec0a11644d95b38796bd3e28715a_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_61edec0a11644d95b38796bd3e28715a_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_R_5_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_G_6_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_B_7_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_A_8_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.a;
float _Subtract_38999ff8e5a04271b3794daa96fc9cca_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_R_5_Float, float(0.5), _Subtract_38999ff8e5a04271b3794daa96fc9cca_Out_2_Float);
float _Multiply_4209b0f1a88b424992ddafb6610c2968_Out_2_Float;
Unity_Multiply_float_float(_UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float, _Subtract_38999ff8e5a04271b3794daa96fc9cca_Out_2_Float, _Multiply_4209b0f1a88b424992ddafb6610c2968_Out_2_Float);
float _Property_2bd20e0e45834464a28f6379b9e8225c_Out_0_Float = _Wind_Small_Intensity;
float _Multiply_1c0b4b06455e4d31b5ad6f8c28d20004_Out_2_Float;
Unity_Multiply_float_float(_Multiply_4209b0f1a88b424992ddafb6610c2968_Out_2_Float, _Property_2bd20e0e45834464a28f6379b9e8225c_Out_0_Float, _Multiply_1c0b4b06455e4d31b5ad6f8c28d20004_Out_2_Float);
float _Add_0407a76ed876497f9c558311ba6b5eb8_Out_2_Float;
Unity_Add_float(_Multiply_6869889d47d046e8b5ad71f9c3a9adb4_Out_2_Float, _Multiply_1c0b4b06455e4d31b5ad6f8c28d20004_Out_2_Float, _Add_0407a76ed876497f9c558311ba6b5eb8_Out_2_Float);
float3 _Multiply_5d424720302c4e55afef4b8a9f76d3ef_Out_2_Vector3;
Unity_Multiply_float3_float3((_Add_0407a76ed876497f9c558311ba6b5eb8_Out_2_Float.xxx), IN.WorldSpaceNormal, _Multiply_5d424720302c4e55afef4b8a9f76d3ef_Out_2_Vector3);
float3 _Add_0a8e7ddaf6aa4278a2f9912909eba29d_Out_2_Vector3;
Unity_Add_float3(_Multiply_5d424720302c4e55afef4b8a9f76d3ef_Out_2_Vector3, IN.WorldSpacePosition, _Add_0a8e7ddaf6aa4278a2f9912909eba29d_Out_2_Vector3);
float3 _UPGFoliageBendWSCustomFunction_3d8d6250d27b4f3b96cbc7767f9ae45d_offsetWS_2_Vector3;
UPG_FoliageBendWS_float(IN.WorldSpacePosition, _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float, _UPGFoliageBendWSCustomFunction_3d8d6250d27b4f3b96cbc7767f9ae45d_offsetWS_2_Vector3);
float3 _Add_a2f1ce3eccd04a348677bce71231c28f_Out_2_Vector3;
Unity_Add_float3(_Add_0a8e7ddaf6aa4278a2f9912909eba29d_Out_2_Vector3, _UPGFoliageBendWSCustomFunction_3d8d6250d27b4f3b96cbc7767f9ae45d_offsetWS_2_Vector3, _Add_a2f1ce3eccd04a348677bce71231c28f_Out_2_Vector3);
float3 _Transform_cd30c5b4b2e5405bbe4451c6c589718b_Out_1_Vector3;
_Transform_cd30c5b4b2e5405bbe4451c6c589718b_Out_1_Vector3 = TransformWorldToObject(_Add_a2f1ce3eccd04a348677bce71231c28f_Out_2_Vector3.xyz);
description.Position = _Transform_cd30c5b4b2e5405bbe4451c6c589718b_Out_1_Vector3;
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
UnityTexture2D _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Foliage_Map, sampler_Foliage_Map, _Foliage_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.tex, _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.samplerstate, _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4, _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_R_4_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.r;
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_G_5_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.g;
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_B_6_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.b;
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_A_7_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.a;
float _Property_ce8925e5051d4745a4cf3b2d4fb8db48_Out_0_Float = _Alpha_Clip;
float _Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_A_7_Float, _Property_ce8925e5051d4745a4cf3b2d4fb8db48_Out_0_Float, _Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float);
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_A_4_Float = 0;
float _Property_496eeb327965485cb724cb0c285f7a6c_Out_0_Float = _Dither_Bottom_Level;
float _Add_9a95e02d9c734b69a1bd64099eab897d_Out_2_Float;
Unity_Add_float(_Split_b2c67f57ffb14ffab00206eeffb0e2d5_G_2_Float, _Property_496eeb327965485cb724cb0c285f7a6c_Out_0_Float, _Add_9a95e02d9c734b69a1bd64099eab897d_Out_2_Float);
float _Property_ccb0e250db5745fe928b0ac7bf4d8af7_Out_0_Float = _Dither_Fade;
float _Multiply_38741f482df44a6584b848549f368772_Out_2_Float;
Unity_Multiply_float_float(_Add_9a95e02d9c734b69a1bd64099eab897d_Out_2_Float, _Property_ccb0e250db5745fe928b0ac7bf4d8af7_Out_0_Float, _Multiply_38741f482df44a6584b848549f368772_Out_2_Float);
float _Dither_22e77e4623774a7fa94d00b2ae4ff606_Out_2_Float;
Unity_Dither_float(_Multiply_38741f482df44a6584b848549f368772_Out_2_Float, float4(IN.NDCPosition.xy, 0, 0), _Dither_22e77e4623774a7fa94d00b2ae4ff606_Out_2_Float);
float _Multiply_9214bd931fea4bc98db2b9a46d79c8f8_Out_2_Float;
Unity_Multiply_float_float(_Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float, _Dither_22e77e4623774a7fa94d00b2ae4ff606_Out_2_Float, _Multiply_9214bd931fea4bc98db2b9a46d79c8f8_Out_2_Float);
#if defined(_USE_DITHER)
float _UseDither_4bfe8155f1434a3d9ded012dece1de0e_Out_0_Float = _Multiply_9214bd931fea4bc98db2b9a46d79c8f8_Out_2_Float;
#else
float _UseDither_4bfe8155f1434a3d9ded012dece1de0e_Out_0_Float = _Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float;
#endif
surface.Alpha = _UseDither_4bfe8155f1434a3d9ded012dece1de0e_Out_0_Float;
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
    output.WorldSpaceNormal =                           TransformObjectToWorldNormal(input.normalOS);
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
    output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
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

    





    output.ObjectSpacePosition = TransformWorldToObject(input.positionWS);

    #if UNITY_UV_STARTS_AT_TOP
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #else
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #endif

    output.NDCPosition = output.PixelPosition.xy / _ScaledScreenParams.xy;
    output.NDCPosition.y = 1.0f - output.NDCPosition.y;

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
#pragma shader_feature_local _ _USE_DITHER
#pragma shader_feature_local _ _USE_GRADIENT_MAP
#pragma shader_feature_local _ _USE_VERTEX_COLOR

#if defined(_USE_DITHER) && defined(_USE_GRADIENT_MAP) && defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_DITHER) && defined(_USE_GRADIENT_MAP)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_DITHER) && defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_2
#elif defined(_USE_DITHER)
#define KEYWORD_PERMUTATION_3
#elif defined(_USE_GRADIENT_MAP) && defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_4
#elif defined(_USE_GRADIENT_MAP)
#define KEYWORD_PERMUTATION_5
#elif defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_6
#else
#define KEYWORD_PERMUTATION_7
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
 float4 color : COLOR;
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
 float3 ObjectSpacePosition;
 float2 NDCPosition;
 float2 PixelPosition;
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 WorldSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 WorldSpacePosition;
 float4 uv0;
 float4 VertexColor;
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

float4 _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D_TexelSize;
float4 _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D_TexelSize;
float _Wind_Small_Scale;
float2 _Wind_Small_Speed;
float _Wind_Small_Intensity;
float _Wind_Large_Scale;
float2 _Wind_Large_Speed;
float4 _Color_Bottom;
float _Wind_Large_Intensity;
float _UV_Pivot_Lock_Strangth;
float _Dither_Bottom_Level;
float _Dither_Fade;
float4 _Foliage_Map_TexelSize;
float _Alpha_Clip;
float4 _Color_Tint;
float4 _Gradient_Map_TexelSize;
float4 _Color_Top;
float _Smoothness;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D);
TEXTURE2D(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D);
TEXTURE2D(_Foliage_Map);
SAMPLER(sampler_Foliage_Map);
TEXTURE2D(_Gradient_Map);
SAMPLER(sampler_Gradient_Map);

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

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

void Unity_Dither_float(float In, float4 ScreenPosition, out float Out)
{
    float2 uv = ScreenPosition.xy * _ScreenParams.xy;
    static const float DITHER_THRESHOLDS[16] =
    {
        1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
        13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
        4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
        16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
    };
    uint index = (uint(uv.x) % 4) * 4 + uint(uv.y) % 4;
    Out = In - DITHER_THRESHOLDS[index];
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
float _Property_ad4aa287fed64295a604d3291d1959a2_Out_0_Float = _Wind_Large_Scale;
float2 _Property_c2f76a94f588455e9b4c07b23d9ef2ff_Out_0_Vector2 = _Wind_Large_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_bad92adcb02c4fc28767432489546115;
_PositionalUVs_bad92adcb02c4fc28767432489546115.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_bad92adcb02c4fc28767432489546115.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_bad92adcb02c4fc28767432489546115_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_ad4aa287fed64295a604d3291d1959a2_Out_0_Float, _Property_c2f76a94f588455e9b4c07b23d9ef2ff_Out_0_Vector2, _PositionalUVs_bad92adcb02c4fc28767432489546115, _PositionalUVs_bad92adcb02c4fc28767432489546115_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_bad92adcb02c4fc28767432489546115_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_R_5_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_G_6_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_B_7_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_A_8_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.a;
float _Subtract_218acd7847974a5cbb60429ba8096400_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_R_5_Float, float(0.5), _Subtract_218acd7847974a5cbb60429ba8096400_Out_2_Float);
float _Split_9060b9716ec549aeacc51b6fd9846e1f_R_1_Float = IN.VertexColor[0];
float _Split_9060b9716ec549aeacc51b6fd9846e1f_G_2_Float = IN.VertexColor[1];
float _Split_9060b9716ec549aeacc51b6fd9846e1f_B_3_Float = IN.VertexColor[2];
float _Split_9060b9716ec549aeacc51b6fd9846e1f_A_4_Float = IN.VertexColor[3];
float4 _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4 = IN.uv0;
float _Split_cd4e6782516b4c4eba58883246f59068_R_1_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[0];
float _Split_cd4e6782516b4c4eba58883246f59068_G_2_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[1];
float _Split_cd4e6782516b4c4eba58883246f59068_B_3_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[2];
float _Split_cd4e6782516b4c4eba58883246f59068_A_4_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[3];
float _Property_8494919c92594e95b9e93cc374ab477d_Out_0_Float = _UV_Pivot_Lock_Strangth;
float _Power_a22a61dbae424f388acc9bbf24d84825_Out_2_Float;
Unity_Power_float(_Split_cd4e6782516b4c4eba58883246f59068_G_2_Float, _Property_8494919c92594e95b9e93cc374ab477d_Out_0_Float, _Power_a22a61dbae424f388acc9bbf24d84825_Out_2_Float);
#if defined(_USE_VERTEX_COLOR)
float _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float = _Split_9060b9716ec549aeacc51b6fd9846e1f_R_1_Float;
#else
float _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float = _Power_a22a61dbae424f388acc9bbf24d84825_Out_2_Float;
#endif
float _Multiply_ea0e0a96337840389ee116bcbf36e668_Out_2_Float;
Unity_Multiply_float_float(_Subtract_218acd7847974a5cbb60429ba8096400_Out_2_Float, _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float, _Multiply_ea0e0a96337840389ee116bcbf36e668_Out_2_Float);
float _Property_dbbb11cdd2654ae79939bef8b5ecd875_Out_0_Float = _Wind_Large_Intensity;
float _Multiply_6869889d47d046e8b5ad71f9c3a9adb4_Out_2_Float;
Unity_Multiply_float_float(_Multiply_ea0e0a96337840389ee116bcbf36e668_Out_2_Float, _Property_dbbb11cdd2654ae79939bef8b5ecd875_Out_0_Float, _Multiply_6869889d47d046e8b5ad71f9c3a9adb4_Out_2_Float);
float _Property_c327c41c045b40c483eb7da471e0c3fd_Out_0_Float = _Wind_Small_Scale;
float2 _Property_92349f3d399742e5b93d3ec2912b6d0b_Out_0_Vector2 = _Wind_Small_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_61edec0a11644d95b38796bd3e28715a;
_PositionalUVs_61edec0a11644d95b38796bd3e28715a.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_61edec0a11644d95b38796bd3e28715a.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_61edec0a11644d95b38796bd3e28715a_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_c327c41c045b40c483eb7da471e0c3fd_Out_0_Float, _Property_92349f3d399742e5b93d3ec2912b6d0b_Out_0_Vector2, _PositionalUVs_61edec0a11644d95b38796bd3e28715a, _PositionalUVs_61edec0a11644d95b38796bd3e28715a_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_61edec0a11644d95b38796bd3e28715a_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_R_5_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_G_6_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_B_7_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_A_8_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.a;
float _Subtract_38999ff8e5a04271b3794daa96fc9cca_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_R_5_Float, float(0.5), _Subtract_38999ff8e5a04271b3794daa96fc9cca_Out_2_Float);
float _Multiply_4209b0f1a88b424992ddafb6610c2968_Out_2_Float;
Unity_Multiply_float_float(_UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float, _Subtract_38999ff8e5a04271b3794daa96fc9cca_Out_2_Float, _Multiply_4209b0f1a88b424992ddafb6610c2968_Out_2_Float);
float _Property_2bd20e0e45834464a28f6379b9e8225c_Out_0_Float = _Wind_Small_Intensity;
float _Multiply_1c0b4b06455e4d31b5ad6f8c28d20004_Out_2_Float;
Unity_Multiply_float_float(_Multiply_4209b0f1a88b424992ddafb6610c2968_Out_2_Float, _Property_2bd20e0e45834464a28f6379b9e8225c_Out_0_Float, _Multiply_1c0b4b06455e4d31b5ad6f8c28d20004_Out_2_Float);
float _Add_0407a76ed876497f9c558311ba6b5eb8_Out_2_Float;
Unity_Add_float(_Multiply_6869889d47d046e8b5ad71f9c3a9adb4_Out_2_Float, _Multiply_1c0b4b06455e4d31b5ad6f8c28d20004_Out_2_Float, _Add_0407a76ed876497f9c558311ba6b5eb8_Out_2_Float);
float3 _Multiply_5d424720302c4e55afef4b8a9f76d3ef_Out_2_Vector3;
Unity_Multiply_float3_float3((_Add_0407a76ed876497f9c558311ba6b5eb8_Out_2_Float.xxx), IN.WorldSpaceNormal, _Multiply_5d424720302c4e55afef4b8a9f76d3ef_Out_2_Vector3);
float3 _Add_0a8e7ddaf6aa4278a2f9912909eba29d_Out_2_Vector3;
Unity_Add_float3(_Multiply_5d424720302c4e55afef4b8a9f76d3ef_Out_2_Vector3, IN.WorldSpacePosition, _Add_0a8e7ddaf6aa4278a2f9912909eba29d_Out_2_Vector3);
float3 _UPGFoliageBendWSCustomFunction_3d8d6250d27b4f3b96cbc7767f9ae45d_offsetWS_2_Vector3;
UPG_FoliageBendWS_float(IN.WorldSpacePosition, _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float, _UPGFoliageBendWSCustomFunction_3d8d6250d27b4f3b96cbc7767f9ae45d_offsetWS_2_Vector3);
float3 _Add_a2f1ce3eccd04a348677bce71231c28f_Out_2_Vector3;
Unity_Add_float3(_Add_0a8e7ddaf6aa4278a2f9912909eba29d_Out_2_Vector3, _UPGFoliageBendWSCustomFunction_3d8d6250d27b4f3b96cbc7767f9ae45d_offsetWS_2_Vector3, _Add_a2f1ce3eccd04a348677bce71231c28f_Out_2_Vector3);
float3 _Transform_cd30c5b4b2e5405bbe4451c6c589718b_Out_1_Vector3;
_Transform_cd30c5b4b2e5405bbe4451c6c589718b_Out_1_Vector3 = TransformWorldToObject(_Add_a2f1ce3eccd04a348677bce71231c28f_Out_2_Vector3.xyz);
description.Position = _Transform_cd30c5b4b2e5405bbe4451c6c589718b_Out_1_Vector3;
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
float4 _Property_597353718570472cb903eabbac10e98e_Out_0_Vector4 = _Color_Bottom;
float4 _Property_acf5ade3ddc74df2bb7b72f3fb5027e6_Out_0_Vector4 = _Color_Top;
UnityTexture2D _Property_0dd8d95b31054753b15cbea49c4dd049_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Gradient_Map, sampler_Gradient_Map, _Gradient_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_0dd8d95b31054753b15cbea49c4dd049_Out_0_Texture2D.tex, _Property_0dd8d95b31054753b15cbea49c4dd049_Out_0_Texture2D.samplerstate, _Property_0dd8d95b31054753b15cbea49c4dd049_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_0dd8d95b31054753b15cbea49c4dd049_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4, _Property_0dd8d95b31054753b15cbea49c4dd049_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_R_4_Float = _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4.r;
float _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_G_5_Float = _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4.g;
float _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_B_6_Float = _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4.b;
float _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_A_7_Float = _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4.a;
float4 _Lerp_45e933526a9e47429463b3d3ae7bcb99_Out_3_Vector4;
Unity_Lerp_float4(_Property_597353718570472cb903eabbac10e98e_Out_0_Vector4, _Property_acf5ade3ddc74df2bb7b72f3fb5027e6_Out_0_Vector4, (_SampleTexture2D_0139bf195dc74dea98a5142201287ca0_R_4_Float.xxxx), _Lerp_45e933526a9e47429463b3d3ae7bcb99_Out_3_Vector4);
float4 _Property_d51fad0917634f61855f006ac3a3036a_Out_0_Vector4 = _Color_Tint;
#if defined(_USE_GRADIENT_MAP)
float4 _UseGradientMap_ab913e91a75042bfa88ab1093a041f33_Out_0_Vector4 = _Lerp_45e933526a9e47429463b3d3ae7bcb99_Out_3_Vector4;
#else
float4 _UseGradientMap_ab913e91a75042bfa88ab1093a041f33_Out_0_Vector4 = _Property_d51fad0917634f61855f006ac3a3036a_Out_0_Vector4;
#endif
UnityTexture2D _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Foliage_Map, sampler_Foliage_Map, _Foliage_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.tex, _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.samplerstate, _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4, _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_R_4_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.r;
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_G_5_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.g;
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_B_6_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.b;
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_A_7_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.a;
float4 _Multiply_2d1489dbc4f84047b8a1330c23f185d7_Out_2_Vector4;
Unity_Multiply_float4_float4(_UseGradientMap_ab913e91a75042bfa88ab1093a041f33_Out_0_Vector4, _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4, _Multiply_2d1489dbc4f84047b8a1330c23f185d7_Out_2_Vector4);
float _Property_ce8925e5051d4745a4cf3b2d4fb8db48_Out_0_Float = _Alpha_Clip;
float _Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_A_7_Float, _Property_ce8925e5051d4745a4cf3b2d4fb8db48_Out_0_Float, _Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float);
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_A_4_Float = 0;
float _Property_496eeb327965485cb724cb0c285f7a6c_Out_0_Float = _Dither_Bottom_Level;
float _Add_9a95e02d9c734b69a1bd64099eab897d_Out_2_Float;
Unity_Add_float(_Split_b2c67f57ffb14ffab00206eeffb0e2d5_G_2_Float, _Property_496eeb327965485cb724cb0c285f7a6c_Out_0_Float, _Add_9a95e02d9c734b69a1bd64099eab897d_Out_2_Float);
float _Property_ccb0e250db5745fe928b0ac7bf4d8af7_Out_0_Float = _Dither_Fade;
float _Multiply_38741f482df44a6584b848549f368772_Out_2_Float;
Unity_Multiply_float_float(_Add_9a95e02d9c734b69a1bd64099eab897d_Out_2_Float, _Property_ccb0e250db5745fe928b0ac7bf4d8af7_Out_0_Float, _Multiply_38741f482df44a6584b848549f368772_Out_2_Float);
float _Dither_22e77e4623774a7fa94d00b2ae4ff606_Out_2_Float;
Unity_Dither_float(_Multiply_38741f482df44a6584b848549f368772_Out_2_Float, float4(IN.NDCPosition.xy, 0, 0), _Dither_22e77e4623774a7fa94d00b2ae4ff606_Out_2_Float);
float _Multiply_9214bd931fea4bc98db2b9a46d79c8f8_Out_2_Float;
Unity_Multiply_float_float(_Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float, _Dither_22e77e4623774a7fa94d00b2ae4ff606_Out_2_Float, _Multiply_9214bd931fea4bc98db2b9a46d79c8f8_Out_2_Float);
#if defined(_USE_DITHER)
float _UseDither_4bfe8155f1434a3d9ded012dece1de0e_Out_0_Float = _Multiply_9214bd931fea4bc98db2b9a46d79c8f8_Out_2_Float;
#else
float _UseDither_4bfe8155f1434a3d9ded012dece1de0e_Out_0_Float = _Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float;
#endif
surface.BaseColor = (_Multiply_2d1489dbc4f84047b8a1330c23f185d7_Out_2_Vector4.xyz);
surface.Alpha = _UseDither_4bfe8155f1434a3d9ded012dece1de0e_Out_0_Float;
surface.AlphaClipThreshold = float(0.5);
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
    output.WorldSpaceNormal =                           TransformObjectToWorldNormal(input.normalOS);
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
    output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
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

    





    output.ObjectSpacePosition = TransformWorldToObject(input.positionWS);

    #if UNITY_UV_STARTS_AT_TOP
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #else
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #endif

    output.NDCPosition = output.PixelPosition.xy / _ScaledScreenParams.xy;
    output.NDCPosition.y = 1.0f - output.NDCPosition.y;

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
#pragma shader_feature_local _ _USE_DITHER
#pragma shader_feature_local _ _USE_GRADIENT_MAP
#pragma shader_feature_local _ _USE_VERTEX_COLOR

#if defined(_USE_DITHER) && defined(_USE_GRADIENT_MAP) && defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_DITHER) && defined(_USE_GRADIENT_MAP)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_DITHER) && defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_2
#elif defined(_USE_DITHER)
#define KEYWORD_PERMUTATION_3
#elif defined(_USE_GRADIENT_MAP) && defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_4
#elif defined(_USE_GRADIENT_MAP)
#define KEYWORD_PERMUTATION_5
#elif defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_6
#else
#define KEYWORD_PERMUTATION_7
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
 float4 color : COLOR;
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
 float3 ObjectSpacePosition;
 float2 NDCPosition;
 float2 PixelPosition;
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 WorldSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 WorldSpacePosition;
 float4 uv0;
 float4 VertexColor;
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

float4 _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D_TexelSize;
float4 _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D_TexelSize;
float _Wind_Small_Scale;
float2 _Wind_Small_Speed;
float _Wind_Small_Intensity;
float _Wind_Large_Scale;
float2 _Wind_Large_Speed;
float4 _Color_Bottom;
float _Wind_Large_Intensity;
float _UV_Pivot_Lock_Strangth;
float _Dither_Bottom_Level;
float _Dither_Fade;
float4 _Foliage_Map_TexelSize;
float _Alpha_Clip;
float4 _Color_Tint;
float4 _Gradient_Map_TexelSize;
float4 _Color_Top;
float _Smoothness;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D);
TEXTURE2D(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D);
TEXTURE2D(_Foliage_Map);
SAMPLER(sampler_Foliage_Map);
TEXTURE2D(_Gradient_Map);
SAMPLER(sampler_Gradient_Map);

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

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

void Unity_Dither_float(float In, float4 ScreenPosition, out float Out)
{
    float2 uv = ScreenPosition.xy * _ScreenParams.xy;
    static const float DITHER_THRESHOLDS[16] =
    {
        1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
        13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
        4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
        16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
    };
    uint index = (uint(uv.x) % 4) * 4 + uint(uv.y) % 4;
    Out = In - DITHER_THRESHOLDS[index];
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
float _Property_ad4aa287fed64295a604d3291d1959a2_Out_0_Float = _Wind_Large_Scale;
float2 _Property_c2f76a94f588455e9b4c07b23d9ef2ff_Out_0_Vector2 = _Wind_Large_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_bad92adcb02c4fc28767432489546115;
_PositionalUVs_bad92adcb02c4fc28767432489546115.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_bad92adcb02c4fc28767432489546115.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_bad92adcb02c4fc28767432489546115_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_ad4aa287fed64295a604d3291d1959a2_Out_0_Float, _Property_c2f76a94f588455e9b4c07b23d9ef2ff_Out_0_Vector2, _PositionalUVs_bad92adcb02c4fc28767432489546115, _PositionalUVs_bad92adcb02c4fc28767432489546115_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_bad92adcb02c4fc28767432489546115_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_R_5_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_G_6_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_B_7_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_A_8_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.a;
float _Subtract_218acd7847974a5cbb60429ba8096400_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_R_5_Float, float(0.5), _Subtract_218acd7847974a5cbb60429ba8096400_Out_2_Float);
float _Split_9060b9716ec549aeacc51b6fd9846e1f_R_1_Float = IN.VertexColor[0];
float _Split_9060b9716ec549aeacc51b6fd9846e1f_G_2_Float = IN.VertexColor[1];
float _Split_9060b9716ec549aeacc51b6fd9846e1f_B_3_Float = IN.VertexColor[2];
float _Split_9060b9716ec549aeacc51b6fd9846e1f_A_4_Float = IN.VertexColor[3];
float4 _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4 = IN.uv0;
float _Split_cd4e6782516b4c4eba58883246f59068_R_1_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[0];
float _Split_cd4e6782516b4c4eba58883246f59068_G_2_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[1];
float _Split_cd4e6782516b4c4eba58883246f59068_B_3_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[2];
float _Split_cd4e6782516b4c4eba58883246f59068_A_4_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[3];
float _Property_8494919c92594e95b9e93cc374ab477d_Out_0_Float = _UV_Pivot_Lock_Strangth;
float _Power_a22a61dbae424f388acc9bbf24d84825_Out_2_Float;
Unity_Power_float(_Split_cd4e6782516b4c4eba58883246f59068_G_2_Float, _Property_8494919c92594e95b9e93cc374ab477d_Out_0_Float, _Power_a22a61dbae424f388acc9bbf24d84825_Out_2_Float);
#if defined(_USE_VERTEX_COLOR)
float _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float = _Split_9060b9716ec549aeacc51b6fd9846e1f_R_1_Float;
#else
float _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float = _Power_a22a61dbae424f388acc9bbf24d84825_Out_2_Float;
#endif
float _Multiply_ea0e0a96337840389ee116bcbf36e668_Out_2_Float;
Unity_Multiply_float_float(_Subtract_218acd7847974a5cbb60429ba8096400_Out_2_Float, _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float, _Multiply_ea0e0a96337840389ee116bcbf36e668_Out_2_Float);
float _Property_dbbb11cdd2654ae79939bef8b5ecd875_Out_0_Float = _Wind_Large_Intensity;
float _Multiply_6869889d47d046e8b5ad71f9c3a9adb4_Out_2_Float;
Unity_Multiply_float_float(_Multiply_ea0e0a96337840389ee116bcbf36e668_Out_2_Float, _Property_dbbb11cdd2654ae79939bef8b5ecd875_Out_0_Float, _Multiply_6869889d47d046e8b5ad71f9c3a9adb4_Out_2_Float);
float _Property_c327c41c045b40c483eb7da471e0c3fd_Out_0_Float = _Wind_Small_Scale;
float2 _Property_92349f3d399742e5b93d3ec2912b6d0b_Out_0_Vector2 = _Wind_Small_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_61edec0a11644d95b38796bd3e28715a;
_PositionalUVs_61edec0a11644d95b38796bd3e28715a.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_61edec0a11644d95b38796bd3e28715a.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_61edec0a11644d95b38796bd3e28715a_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_c327c41c045b40c483eb7da471e0c3fd_Out_0_Float, _Property_92349f3d399742e5b93d3ec2912b6d0b_Out_0_Vector2, _PositionalUVs_61edec0a11644d95b38796bd3e28715a, _PositionalUVs_61edec0a11644d95b38796bd3e28715a_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_61edec0a11644d95b38796bd3e28715a_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_R_5_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_G_6_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_B_7_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_A_8_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.a;
float _Subtract_38999ff8e5a04271b3794daa96fc9cca_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_R_5_Float, float(0.5), _Subtract_38999ff8e5a04271b3794daa96fc9cca_Out_2_Float);
float _Multiply_4209b0f1a88b424992ddafb6610c2968_Out_2_Float;
Unity_Multiply_float_float(_UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float, _Subtract_38999ff8e5a04271b3794daa96fc9cca_Out_2_Float, _Multiply_4209b0f1a88b424992ddafb6610c2968_Out_2_Float);
float _Property_2bd20e0e45834464a28f6379b9e8225c_Out_0_Float = _Wind_Small_Intensity;
float _Multiply_1c0b4b06455e4d31b5ad6f8c28d20004_Out_2_Float;
Unity_Multiply_float_float(_Multiply_4209b0f1a88b424992ddafb6610c2968_Out_2_Float, _Property_2bd20e0e45834464a28f6379b9e8225c_Out_0_Float, _Multiply_1c0b4b06455e4d31b5ad6f8c28d20004_Out_2_Float);
float _Add_0407a76ed876497f9c558311ba6b5eb8_Out_2_Float;
Unity_Add_float(_Multiply_6869889d47d046e8b5ad71f9c3a9adb4_Out_2_Float, _Multiply_1c0b4b06455e4d31b5ad6f8c28d20004_Out_2_Float, _Add_0407a76ed876497f9c558311ba6b5eb8_Out_2_Float);
float3 _Multiply_5d424720302c4e55afef4b8a9f76d3ef_Out_2_Vector3;
Unity_Multiply_float3_float3((_Add_0407a76ed876497f9c558311ba6b5eb8_Out_2_Float.xxx), IN.WorldSpaceNormal, _Multiply_5d424720302c4e55afef4b8a9f76d3ef_Out_2_Vector3);
float3 _Add_0a8e7ddaf6aa4278a2f9912909eba29d_Out_2_Vector3;
Unity_Add_float3(_Multiply_5d424720302c4e55afef4b8a9f76d3ef_Out_2_Vector3, IN.WorldSpacePosition, _Add_0a8e7ddaf6aa4278a2f9912909eba29d_Out_2_Vector3);
float3 _UPGFoliageBendWSCustomFunction_3d8d6250d27b4f3b96cbc7767f9ae45d_offsetWS_2_Vector3;
UPG_FoliageBendWS_float(IN.WorldSpacePosition, _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float, _UPGFoliageBendWSCustomFunction_3d8d6250d27b4f3b96cbc7767f9ae45d_offsetWS_2_Vector3);
float3 _Add_a2f1ce3eccd04a348677bce71231c28f_Out_2_Vector3;
Unity_Add_float3(_Add_0a8e7ddaf6aa4278a2f9912909eba29d_Out_2_Vector3, _UPGFoliageBendWSCustomFunction_3d8d6250d27b4f3b96cbc7767f9ae45d_offsetWS_2_Vector3, _Add_a2f1ce3eccd04a348677bce71231c28f_Out_2_Vector3);
float3 _Transform_cd30c5b4b2e5405bbe4451c6c589718b_Out_1_Vector3;
_Transform_cd30c5b4b2e5405bbe4451c6c589718b_Out_1_Vector3 = TransformWorldToObject(_Add_a2f1ce3eccd04a348677bce71231c28f_Out_2_Vector3.xyz);
description.Position = _Transform_cd30c5b4b2e5405bbe4451c6c589718b_Out_1_Vector3;
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
float4 _Property_597353718570472cb903eabbac10e98e_Out_0_Vector4 = _Color_Bottom;
float4 _Property_acf5ade3ddc74df2bb7b72f3fb5027e6_Out_0_Vector4 = _Color_Top;
UnityTexture2D _Property_0dd8d95b31054753b15cbea49c4dd049_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Gradient_Map, sampler_Gradient_Map, _Gradient_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_0dd8d95b31054753b15cbea49c4dd049_Out_0_Texture2D.tex, _Property_0dd8d95b31054753b15cbea49c4dd049_Out_0_Texture2D.samplerstate, _Property_0dd8d95b31054753b15cbea49c4dd049_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_0dd8d95b31054753b15cbea49c4dd049_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4, _Property_0dd8d95b31054753b15cbea49c4dd049_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_R_4_Float = _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4.r;
float _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_G_5_Float = _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4.g;
float _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_B_6_Float = _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4.b;
float _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_A_7_Float = _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4.a;
float4 _Lerp_45e933526a9e47429463b3d3ae7bcb99_Out_3_Vector4;
Unity_Lerp_float4(_Property_597353718570472cb903eabbac10e98e_Out_0_Vector4, _Property_acf5ade3ddc74df2bb7b72f3fb5027e6_Out_0_Vector4, (_SampleTexture2D_0139bf195dc74dea98a5142201287ca0_R_4_Float.xxxx), _Lerp_45e933526a9e47429463b3d3ae7bcb99_Out_3_Vector4);
float4 _Property_d51fad0917634f61855f006ac3a3036a_Out_0_Vector4 = _Color_Tint;
#if defined(_USE_GRADIENT_MAP)
float4 _UseGradientMap_ab913e91a75042bfa88ab1093a041f33_Out_0_Vector4 = _Lerp_45e933526a9e47429463b3d3ae7bcb99_Out_3_Vector4;
#else
float4 _UseGradientMap_ab913e91a75042bfa88ab1093a041f33_Out_0_Vector4 = _Property_d51fad0917634f61855f006ac3a3036a_Out_0_Vector4;
#endif
UnityTexture2D _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Foliage_Map, sampler_Foliage_Map, _Foliage_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.tex, _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.samplerstate, _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4, _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_R_4_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.r;
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_G_5_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.g;
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_B_6_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.b;
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_A_7_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.a;
float4 _Multiply_2d1489dbc4f84047b8a1330c23f185d7_Out_2_Vector4;
Unity_Multiply_float4_float4(_UseGradientMap_ab913e91a75042bfa88ab1093a041f33_Out_0_Vector4, _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4, _Multiply_2d1489dbc4f84047b8a1330c23f185d7_Out_2_Vector4);
float _Property_ce8925e5051d4745a4cf3b2d4fb8db48_Out_0_Float = _Alpha_Clip;
float _Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_A_7_Float, _Property_ce8925e5051d4745a4cf3b2d4fb8db48_Out_0_Float, _Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float);
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_A_4_Float = 0;
float _Property_496eeb327965485cb724cb0c285f7a6c_Out_0_Float = _Dither_Bottom_Level;
float _Add_9a95e02d9c734b69a1bd64099eab897d_Out_2_Float;
Unity_Add_float(_Split_b2c67f57ffb14ffab00206eeffb0e2d5_G_2_Float, _Property_496eeb327965485cb724cb0c285f7a6c_Out_0_Float, _Add_9a95e02d9c734b69a1bd64099eab897d_Out_2_Float);
float _Property_ccb0e250db5745fe928b0ac7bf4d8af7_Out_0_Float = _Dither_Fade;
float _Multiply_38741f482df44a6584b848549f368772_Out_2_Float;
Unity_Multiply_float_float(_Add_9a95e02d9c734b69a1bd64099eab897d_Out_2_Float, _Property_ccb0e250db5745fe928b0ac7bf4d8af7_Out_0_Float, _Multiply_38741f482df44a6584b848549f368772_Out_2_Float);
float _Dither_22e77e4623774a7fa94d00b2ae4ff606_Out_2_Float;
Unity_Dither_float(_Multiply_38741f482df44a6584b848549f368772_Out_2_Float, float4(IN.NDCPosition.xy, 0, 0), _Dither_22e77e4623774a7fa94d00b2ae4ff606_Out_2_Float);
float _Multiply_9214bd931fea4bc98db2b9a46d79c8f8_Out_2_Float;
Unity_Multiply_float_float(_Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float, _Dither_22e77e4623774a7fa94d00b2ae4ff606_Out_2_Float, _Multiply_9214bd931fea4bc98db2b9a46d79c8f8_Out_2_Float);
#if defined(_USE_DITHER)
float _UseDither_4bfe8155f1434a3d9ded012dece1de0e_Out_0_Float = _Multiply_9214bd931fea4bc98db2b9a46d79c8f8_Out_2_Float;
#else
float _UseDither_4bfe8155f1434a3d9ded012dece1de0e_Out_0_Float = _Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float;
#endif
surface.BaseColor = (_Multiply_2d1489dbc4f84047b8a1330c23f185d7_Out_2_Vector4.xyz);
surface.Alpha = _UseDither_4bfe8155f1434a3d9ded012dece1de0e_Out_0_Float;
surface.AlphaClipThreshold = float(0.5);
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
    output.WorldSpaceNormal =                           TransformObjectToWorldNormal(input.normalOS);
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
    output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
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

    





    output.ObjectSpacePosition = TransformWorldToObject(input.positionWS);

    #if UNITY_UV_STARTS_AT_TOP
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #else
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #endif

    output.NDCPosition = output.PixelPosition.xy / _ScaledScreenParams.xy;
    output.NDCPosition.y = 1.0f - output.NDCPosition.y;

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
#pragma shader_feature_local _ _USE_DITHER
#pragma shader_feature_local _ _USE_GRADIENT_MAP
#pragma shader_feature_local _ _USE_VERTEX_COLOR

#if defined(_USE_DITHER) && defined(_USE_GRADIENT_MAP) && defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_DITHER) && defined(_USE_GRADIENT_MAP)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_DITHER) && defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_2
#elif defined(_USE_DITHER)
#define KEYWORD_PERMUTATION_3
#elif defined(_USE_GRADIENT_MAP) && defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_4
#elif defined(_USE_GRADIENT_MAP)
#define KEYWORD_PERMUTATION_5
#elif defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_6
#else
#define KEYWORD_PERMUTATION_7
#endif


// Defines
#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_TEXCOORD1
#define ATTRIBUTES_NEED_COLOR
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
 float3 TangentSpaceNormal;
 float3 ObjectSpacePosition;
 float2 NDCPosition;
 float2 PixelPosition;
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 WorldSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 WorldSpacePosition;
 float4 uv0;
 float4 VertexColor;
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

float4 _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D_TexelSize;
float4 _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D_TexelSize;
float _Wind_Small_Scale;
float2 _Wind_Small_Speed;
float _Wind_Small_Intensity;
float _Wind_Large_Scale;
float2 _Wind_Large_Speed;
float4 _Color_Bottom;
float _Wind_Large_Intensity;
float _UV_Pivot_Lock_Strangth;
float _Dither_Bottom_Level;
float _Dither_Fade;
float4 _Foliage_Map_TexelSize;
float _Alpha_Clip;
float4 _Color_Tint;
float4 _Gradient_Map_TexelSize;
float4 _Color_Top;
float _Smoothness;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D);
TEXTURE2D(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D);
TEXTURE2D(_Foliage_Map);
SAMPLER(sampler_Foliage_Map);
TEXTURE2D(_Gradient_Map);
SAMPLER(sampler_Gradient_Map);

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

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

void Unity_Dither_float(float In, float4 ScreenPosition, out float Out)
{
    float2 uv = ScreenPosition.xy * _ScreenParams.xy;
    static const float DITHER_THRESHOLDS[16] =
    {
        1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
        13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
        4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
        16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
    };
    uint index = (uint(uv.x) % 4) * 4 + uint(uv.y) % 4;
    Out = In - DITHER_THRESHOLDS[index];
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
float _Property_ad4aa287fed64295a604d3291d1959a2_Out_0_Float = _Wind_Large_Scale;
float2 _Property_c2f76a94f588455e9b4c07b23d9ef2ff_Out_0_Vector2 = _Wind_Large_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_bad92adcb02c4fc28767432489546115;
_PositionalUVs_bad92adcb02c4fc28767432489546115.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_bad92adcb02c4fc28767432489546115.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_bad92adcb02c4fc28767432489546115_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_ad4aa287fed64295a604d3291d1959a2_Out_0_Float, _Property_c2f76a94f588455e9b4c07b23d9ef2ff_Out_0_Vector2, _PositionalUVs_bad92adcb02c4fc28767432489546115, _PositionalUVs_bad92adcb02c4fc28767432489546115_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_bad92adcb02c4fc28767432489546115_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_R_5_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_G_6_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_B_7_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_A_8_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.a;
float _Subtract_218acd7847974a5cbb60429ba8096400_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_R_5_Float, float(0.5), _Subtract_218acd7847974a5cbb60429ba8096400_Out_2_Float);
float _Split_9060b9716ec549aeacc51b6fd9846e1f_R_1_Float = IN.VertexColor[0];
float _Split_9060b9716ec549aeacc51b6fd9846e1f_G_2_Float = IN.VertexColor[1];
float _Split_9060b9716ec549aeacc51b6fd9846e1f_B_3_Float = IN.VertexColor[2];
float _Split_9060b9716ec549aeacc51b6fd9846e1f_A_4_Float = IN.VertexColor[3];
float4 _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4 = IN.uv0;
float _Split_cd4e6782516b4c4eba58883246f59068_R_1_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[0];
float _Split_cd4e6782516b4c4eba58883246f59068_G_2_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[1];
float _Split_cd4e6782516b4c4eba58883246f59068_B_3_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[2];
float _Split_cd4e6782516b4c4eba58883246f59068_A_4_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[3];
float _Property_8494919c92594e95b9e93cc374ab477d_Out_0_Float = _UV_Pivot_Lock_Strangth;
float _Power_a22a61dbae424f388acc9bbf24d84825_Out_2_Float;
Unity_Power_float(_Split_cd4e6782516b4c4eba58883246f59068_G_2_Float, _Property_8494919c92594e95b9e93cc374ab477d_Out_0_Float, _Power_a22a61dbae424f388acc9bbf24d84825_Out_2_Float);
#if defined(_USE_VERTEX_COLOR)
float _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float = _Split_9060b9716ec549aeacc51b6fd9846e1f_R_1_Float;
#else
float _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float = _Power_a22a61dbae424f388acc9bbf24d84825_Out_2_Float;
#endif
float _Multiply_ea0e0a96337840389ee116bcbf36e668_Out_2_Float;
Unity_Multiply_float_float(_Subtract_218acd7847974a5cbb60429ba8096400_Out_2_Float, _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float, _Multiply_ea0e0a96337840389ee116bcbf36e668_Out_2_Float);
float _Property_dbbb11cdd2654ae79939bef8b5ecd875_Out_0_Float = _Wind_Large_Intensity;
float _Multiply_6869889d47d046e8b5ad71f9c3a9adb4_Out_2_Float;
Unity_Multiply_float_float(_Multiply_ea0e0a96337840389ee116bcbf36e668_Out_2_Float, _Property_dbbb11cdd2654ae79939bef8b5ecd875_Out_0_Float, _Multiply_6869889d47d046e8b5ad71f9c3a9adb4_Out_2_Float);
float _Property_c327c41c045b40c483eb7da471e0c3fd_Out_0_Float = _Wind_Small_Scale;
float2 _Property_92349f3d399742e5b93d3ec2912b6d0b_Out_0_Vector2 = _Wind_Small_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_61edec0a11644d95b38796bd3e28715a;
_PositionalUVs_61edec0a11644d95b38796bd3e28715a.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_61edec0a11644d95b38796bd3e28715a.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_61edec0a11644d95b38796bd3e28715a_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_c327c41c045b40c483eb7da471e0c3fd_Out_0_Float, _Property_92349f3d399742e5b93d3ec2912b6d0b_Out_0_Vector2, _PositionalUVs_61edec0a11644d95b38796bd3e28715a, _PositionalUVs_61edec0a11644d95b38796bd3e28715a_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_61edec0a11644d95b38796bd3e28715a_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_R_5_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_G_6_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_B_7_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_A_8_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.a;
float _Subtract_38999ff8e5a04271b3794daa96fc9cca_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_R_5_Float, float(0.5), _Subtract_38999ff8e5a04271b3794daa96fc9cca_Out_2_Float);
float _Multiply_4209b0f1a88b424992ddafb6610c2968_Out_2_Float;
Unity_Multiply_float_float(_UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float, _Subtract_38999ff8e5a04271b3794daa96fc9cca_Out_2_Float, _Multiply_4209b0f1a88b424992ddafb6610c2968_Out_2_Float);
float _Property_2bd20e0e45834464a28f6379b9e8225c_Out_0_Float = _Wind_Small_Intensity;
float _Multiply_1c0b4b06455e4d31b5ad6f8c28d20004_Out_2_Float;
Unity_Multiply_float_float(_Multiply_4209b0f1a88b424992ddafb6610c2968_Out_2_Float, _Property_2bd20e0e45834464a28f6379b9e8225c_Out_0_Float, _Multiply_1c0b4b06455e4d31b5ad6f8c28d20004_Out_2_Float);
float _Add_0407a76ed876497f9c558311ba6b5eb8_Out_2_Float;
Unity_Add_float(_Multiply_6869889d47d046e8b5ad71f9c3a9adb4_Out_2_Float, _Multiply_1c0b4b06455e4d31b5ad6f8c28d20004_Out_2_Float, _Add_0407a76ed876497f9c558311ba6b5eb8_Out_2_Float);
float3 _Multiply_5d424720302c4e55afef4b8a9f76d3ef_Out_2_Vector3;
Unity_Multiply_float3_float3((_Add_0407a76ed876497f9c558311ba6b5eb8_Out_2_Float.xxx), IN.WorldSpaceNormal, _Multiply_5d424720302c4e55afef4b8a9f76d3ef_Out_2_Vector3);
float3 _Add_0a8e7ddaf6aa4278a2f9912909eba29d_Out_2_Vector3;
Unity_Add_float3(_Multiply_5d424720302c4e55afef4b8a9f76d3ef_Out_2_Vector3, IN.WorldSpacePosition, _Add_0a8e7ddaf6aa4278a2f9912909eba29d_Out_2_Vector3);
float3 _UPGFoliageBendWSCustomFunction_3d8d6250d27b4f3b96cbc7767f9ae45d_offsetWS_2_Vector3;
UPG_FoliageBendWS_float(IN.WorldSpacePosition, _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float, _UPGFoliageBendWSCustomFunction_3d8d6250d27b4f3b96cbc7767f9ae45d_offsetWS_2_Vector3);
float3 _Add_a2f1ce3eccd04a348677bce71231c28f_Out_2_Vector3;
Unity_Add_float3(_Add_0a8e7ddaf6aa4278a2f9912909eba29d_Out_2_Vector3, _UPGFoliageBendWSCustomFunction_3d8d6250d27b4f3b96cbc7767f9ae45d_offsetWS_2_Vector3, _Add_a2f1ce3eccd04a348677bce71231c28f_Out_2_Vector3);
float3 _Transform_cd30c5b4b2e5405bbe4451c6c589718b_Out_1_Vector3;
_Transform_cd30c5b4b2e5405bbe4451c6c589718b_Out_1_Vector3 = TransformWorldToObject(_Add_a2f1ce3eccd04a348677bce71231c28f_Out_2_Vector3.xyz);
description.Position = _Transform_cd30c5b4b2e5405bbe4451c6c589718b_Out_1_Vector3;
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
float4 _Property_597353718570472cb903eabbac10e98e_Out_0_Vector4 = _Color_Bottom;
float4 _Property_acf5ade3ddc74df2bb7b72f3fb5027e6_Out_0_Vector4 = _Color_Top;
UnityTexture2D _Property_0dd8d95b31054753b15cbea49c4dd049_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Gradient_Map, sampler_Gradient_Map, _Gradient_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_0dd8d95b31054753b15cbea49c4dd049_Out_0_Texture2D.tex, _Property_0dd8d95b31054753b15cbea49c4dd049_Out_0_Texture2D.samplerstate, _Property_0dd8d95b31054753b15cbea49c4dd049_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_0dd8d95b31054753b15cbea49c4dd049_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4, _Property_0dd8d95b31054753b15cbea49c4dd049_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_R_4_Float = _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4.r;
float _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_G_5_Float = _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4.g;
float _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_B_6_Float = _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4.b;
float _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_A_7_Float = _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4.a;
float4 _Lerp_45e933526a9e47429463b3d3ae7bcb99_Out_3_Vector4;
Unity_Lerp_float4(_Property_597353718570472cb903eabbac10e98e_Out_0_Vector4, _Property_acf5ade3ddc74df2bb7b72f3fb5027e6_Out_0_Vector4, (_SampleTexture2D_0139bf195dc74dea98a5142201287ca0_R_4_Float.xxxx), _Lerp_45e933526a9e47429463b3d3ae7bcb99_Out_3_Vector4);
float4 _Property_d51fad0917634f61855f006ac3a3036a_Out_0_Vector4 = _Color_Tint;
#if defined(_USE_GRADIENT_MAP)
float4 _UseGradientMap_ab913e91a75042bfa88ab1093a041f33_Out_0_Vector4 = _Lerp_45e933526a9e47429463b3d3ae7bcb99_Out_3_Vector4;
#else
float4 _UseGradientMap_ab913e91a75042bfa88ab1093a041f33_Out_0_Vector4 = _Property_d51fad0917634f61855f006ac3a3036a_Out_0_Vector4;
#endif
UnityTexture2D _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Foliage_Map, sampler_Foliage_Map, _Foliage_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.tex, _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.samplerstate, _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4, _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_R_4_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.r;
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_G_5_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.g;
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_B_6_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.b;
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_A_7_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.a;
float4 _Multiply_2d1489dbc4f84047b8a1330c23f185d7_Out_2_Vector4;
Unity_Multiply_float4_float4(_UseGradientMap_ab913e91a75042bfa88ab1093a041f33_Out_0_Vector4, _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4, _Multiply_2d1489dbc4f84047b8a1330c23f185d7_Out_2_Vector4);
float _Property_8dc6bc562c024ba58adfbb32ab0956a5_Out_0_Float = _Smoothness;
float _Property_ce8925e5051d4745a4cf3b2d4fb8db48_Out_0_Float = _Alpha_Clip;
float _Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_A_7_Float, _Property_ce8925e5051d4745a4cf3b2d4fb8db48_Out_0_Float, _Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float);
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_A_4_Float = 0;
float _Property_496eeb327965485cb724cb0c285f7a6c_Out_0_Float = _Dither_Bottom_Level;
float _Add_9a95e02d9c734b69a1bd64099eab897d_Out_2_Float;
Unity_Add_float(_Split_b2c67f57ffb14ffab00206eeffb0e2d5_G_2_Float, _Property_496eeb327965485cb724cb0c285f7a6c_Out_0_Float, _Add_9a95e02d9c734b69a1bd64099eab897d_Out_2_Float);
float _Property_ccb0e250db5745fe928b0ac7bf4d8af7_Out_0_Float = _Dither_Fade;
float _Multiply_38741f482df44a6584b848549f368772_Out_2_Float;
Unity_Multiply_float_float(_Add_9a95e02d9c734b69a1bd64099eab897d_Out_2_Float, _Property_ccb0e250db5745fe928b0ac7bf4d8af7_Out_0_Float, _Multiply_38741f482df44a6584b848549f368772_Out_2_Float);
float _Dither_22e77e4623774a7fa94d00b2ae4ff606_Out_2_Float;
Unity_Dither_float(_Multiply_38741f482df44a6584b848549f368772_Out_2_Float, float4(IN.NDCPosition.xy, 0, 0), _Dither_22e77e4623774a7fa94d00b2ae4ff606_Out_2_Float);
float _Multiply_9214bd931fea4bc98db2b9a46d79c8f8_Out_2_Float;
Unity_Multiply_float_float(_Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float, _Dither_22e77e4623774a7fa94d00b2ae4ff606_Out_2_Float, _Multiply_9214bd931fea4bc98db2b9a46d79c8f8_Out_2_Float);
#if defined(_USE_DITHER)
float _UseDither_4bfe8155f1434a3d9ded012dece1de0e_Out_0_Float = _Multiply_9214bd931fea4bc98db2b9a46d79c8f8_Out_2_Float;
#else
float _UseDither_4bfe8155f1434a3d9ded012dece1de0e_Out_0_Float = _Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float;
#endif
surface.BaseColor = (_Multiply_2d1489dbc4f84047b8a1330c23f185d7_Out_2_Vector4.xyz);
surface.NormalTS = IN.TangentSpaceNormal;
surface.Emission = float3(0, 0, 0);
surface.Metallic = float(0);
surface.Smoothness = _Property_8dc6bc562c024ba58adfbb32ab0956a5_Out_0_Float;
surface.Occlusion = float(1);
surface.Alpha = _UseDither_4bfe8155f1434a3d9ded012dece1de0e_Out_0_Float;
surface.AlphaClipThreshold = float(0.5);
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
    output.WorldSpaceNormal =                           TransformObjectToWorldNormal(input.normalOS);
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
    output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
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

    



    output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);


    output.ObjectSpacePosition = TransformWorldToObject(input.positionWS);

    #if UNITY_UV_STARTS_AT_TOP
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #else
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #endif

    output.NDCPosition = output.PixelPosition.xy / _ScreenParams.xy;
    output.NDCPosition.y = 1.0f - output.NDCPosition.y;

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
    result.color      = attributes.color;
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
#pragma shader_feature_local _ _USE_DITHER
#pragma shader_feature_local _ _USE_GRADIENT_MAP
#pragma shader_feature_local _ _USE_VERTEX_COLOR

#if defined(_USE_DITHER) && defined(_USE_GRADIENT_MAP) && defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_DITHER) && defined(_USE_GRADIENT_MAP)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_DITHER) && defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_2
#elif defined(_USE_DITHER)
#define KEYWORD_PERMUTATION_3
#elif defined(_USE_GRADIENT_MAP) && defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_4
#elif defined(_USE_GRADIENT_MAP)
#define KEYWORD_PERMUTATION_5
#elif defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_6
#else
#define KEYWORD_PERMUTATION_7
#endif


// Defines
#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_TEXCOORD1
#define ATTRIBUTES_NEED_COLOR
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
 float3 TangentSpaceNormal;
 float3 ObjectSpacePosition;
 float2 NDCPosition;
 float2 PixelPosition;
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 WorldSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 WorldSpacePosition;
 float4 uv0;
 float4 VertexColor;
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

float4 _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D_TexelSize;
float4 _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D_TexelSize;
float _Wind_Small_Scale;
float2 _Wind_Small_Speed;
float _Wind_Small_Intensity;
float _Wind_Large_Scale;
float2 _Wind_Large_Speed;
float4 _Color_Bottom;
float _Wind_Large_Intensity;
float _UV_Pivot_Lock_Strangth;
float _Dither_Bottom_Level;
float _Dither_Fade;
float4 _Foliage_Map_TexelSize;
float _Alpha_Clip;
float4 _Color_Tint;
float4 _Gradient_Map_TexelSize;
float4 _Color_Top;
float _Smoothness;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D);
TEXTURE2D(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D);
TEXTURE2D(_Foliage_Map);
SAMPLER(sampler_Foliage_Map);
TEXTURE2D(_Gradient_Map);
SAMPLER(sampler_Gradient_Map);

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

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

void Unity_Dither_float(float In, float4 ScreenPosition, out float Out)
{
    float2 uv = ScreenPosition.xy * _ScreenParams.xy;
    static const float DITHER_THRESHOLDS[16] =
    {
        1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
        13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
        4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
        16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
    };
    uint index = (uint(uv.x) % 4) * 4 + uint(uv.y) % 4;
    Out = In - DITHER_THRESHOLDS[index];
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
float _Property_ad4aa287fed64295a604d3291d1959a2_Out_0_Float = _Wind_Large_Scale;
float2 _Property_c2f76a94f588455e9b4c07b23d9ef2ff_Out_0_Vector2 = _Wind_Large_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_bad92adcb02c4fc28767432489546115;
_PositionalUVs_bad92adcb02c4fc28767432489546115.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_bad92adcb02c4fc28767432489546115.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_bad92adcb02c4fc28767432489546115_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_ad4aa287fed64295a604d3291d1959a2_Out_0_Float, _Property_c2f76a94f588455e9b4c07b23d9ef2ff_Out_0_Vector2, _PositionalUVs_bad92adcb02c4fc28767432489546115, _PositionalUVs_bad92adcb02c4fc28767432489546115_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_bad92adcb02c4fc28767432489546115_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_R_5_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_G_6_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_B_7_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_A_8_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.a;
float _Subtract_218acd7847974a5cbb60429ba8096400_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_R_5_Float, float(0.5), _Subtract_218acd7847974a5cbb60429ba8096400_Out_2_Float);
float _Split_9060b9716ec549aeacc51b6fd9846e1f_R_1_Float = IN.VertexColor[0];
float _Split_9060b9716ec549aeacc51b6fd9846e1f_G_2_Float = IN.VertexColor[1];
float _Split_9060b9716ec549aeacc51b6fd9846e1f_B_3_Float = IN.VertexColor[2];
float _Split_9060b9716ec549aeacc51b6fd9846e1f_A_4_Float = IN.VertexColor[3];
float4 _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4 = IN.uv0;
float _Split_cd4e6782516b4c4eba58883246f59068_R_1_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[0];
float _Split_cd4e6782516b4c4eba58883246f59068_G_2_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[1];
float _Split_cd4e6782516b4c4eba58883246f59068_B_3_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[2];
float _Split_cd4e6782516b4c4eba58883246f59068_A_4_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[3];
float _Property_8494919c92594e95b9e93cc374ab477d_Out_0_Float = _UV_Pivot_Lock_Strangth;
float _Power_a22a61dbae424f388acc9bbf24d84825_Out_2_Float;
Unity_Power_float(_Split_cd4e6782516b4c4eba58883246f59068_G_2_Float, _Property_8494919c92594e95b9e93cc374ab477d_Out_0_Float, _Power_a22a61dbae424f388acc9bbf24d84825_Out_2_Float);
#if defined(_USE_VERTEX_COLOR)
float _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float = _Split_9060b9716ec549aeacc51b6fd9846e1f_R_1_Float;
#else
float _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float = _Power_a22a61dbae424f388acc9bbf24d84825_Out_2_Float;
#endif
float _Multiply_ea0e0a96337840389ee116bcbf36e668_Out_2_Float;
Unity_Multiply_float_float(_Subtract_218acd7847974a5cbb60429ba8096400_Out_2_Float, _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float, _Multiply_ea0e0a96337840389ee116bcbf36e668_Out_2_Float);
float _Property_dbbb11cdd2654ae79939bef8b5ecd875_Out_0_Float = _Wind_Large_Intensity;
float _Multiply_6869889d47d046e8b5ad71f9c3a9adb4_Out_2_Float;
Unity_Multiply_float_float(_Multiply_ea0e0a96337840389ee116bcbf36e668_Out_2_Float, _Property_dbbb11cdd2654ae79939bef8b5ecd875_Out_0_Float, _Multiply_6869889d47d046e8b5ad71f9c3a9adb4_Out_2_Float);
float _Property_c327c41c045b40c483eb7da471e0c3fd_Out_0_Float = _Wind_Small_Scale;
float2 _Property_92349f3d399742e5b93d3ec2912b6d0b_Out_0_Vector2 = _Wind_Small_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_61edec0a11644d95b38796bd3e28715a;
_PositionalUVs_61edec0a11644d95b38796bd3e28715a.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_61edec0a11644d95b38796bd3e28715a.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_61edec0a11644d95b38796bd3e28715a_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_c327c41c045b40c483eb7da471e0c3fd_Out_0_Float, _Property_92349f3d399742e5b93d3ec2912b6d0b_Out_0_Vector2, _PositionalUVs_61edec0a11644d95b38796bd3e28715a, _PositionalUVs_61edec0a11644d95b38796bd3e28715a_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_61edec0a11644d95b38796bd3e28715a_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_R_5_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_G_6_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_B_7_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_A_8_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.a;
float _Subtract_38999ff8e5a04271b3794daa96fc9cca_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_R_5_Float, float(0.5), _Subtract_38999ff8e5a04271b3794daa96fc9cca_Out_2_Float);
float _Multiply_4209b0f1a88b424992ddafb6610c2968_Out_2_Float;
Unity_Multiply_float_float(_UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float, _Subtract_38999ff8e5a04271b3794daa96fc9cca_Out_2_Float, _Multiply_4209b0f1a88b424992ddafb6610c2968_Out_2_Float);
float _Property_2bd20e0e45834464a28f6379b9e8225c_Out_0_Float = _Wind_Small_Intensity;
float _Multiply_1c0b4b06455e4d31b5ad6f8c28d20004_Out_2_Float;
Unity_Multiply_float_float(_Multiply_4209b0f1a88b424992ddafb6610c2968_Out_2_Float, _Property_2bd20e0e45834464a28f6379b9e8225c_Out_0_Float, _Multiply_1c0b4b06455e4d31b5ad6f8c28d20004_Out_2_Float);
float _Add_0407a76ed876497f9c558311ba6b5eb8_Out_2_Float;
Unity_Add_float(_Multiply_6869889d47d046e8b5ad71f9c3a9adb4_Out_2_Float, _Multiply_1c0b4b06455e4d31b5ad6f8c28d20004_Out_2_Float, _Add_0407a76ed876497f9c558311ba6b5eb8_Out_2_Float);
float3 _Multiply_5d424720302c4e55afef4b8a9f76d3ef_Out_2_Vector3;
Unity_Multiply_float3_float3((_Add_0407a76ed876497f9c558311ba6b5eb8_Out_2_Float.xxx), IN.WorldSpaceNormal, _Multiply_5d424720302c4e55afef4b8a9f76d3ef_Out_2_Vector3);
float3 _Add_0a8e7ddaf6aa4278a2f9912909eba29d_Out_2_Vector3;
Unity_Add_float3(_Multiply_5d424720302c4e55afef4b8a9f76d3ef_Out_2_Vector3, IN.WorldSpacePosition, _Add_0a8e7ddaf6aa4278a2f9912909eba29d_Out_2_Vector3);
float3 _UPGFoliageBendWSCustomFunction_3d8d6250d27b4f3b96cbc7767f9ae45d_offsetWS_2_Vector3;
UPG_FoliageBendWS_float(IN.WorldSpacePosition, _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float, _UPGFoliageBendWSCustomFunction_3d8d6250d27b4f3b96cbc7767f9ae45d_offsetWS_2_Vector3);
float3 _Add_a2f1ce3eccd04a348677bce71231c28f_Out_2_Vector3;
Unity_Add_float3(_Add_0a8e7ddaf6aa4278a2f9912909eba29d_Out_2_Vector3, _UPGFoliageBendWSCustomFunction_3d8d6250d27b4f3b96cbc7767f9ae45d_offsetWS_2_Vector3, _Add_a2f1ce3eccd04a348677bce71231c28f_Out_2_Vector3);
float3 _Transform_cd30c5b4b2e5405bbe4451c6c589718b_Out_1_Vector3;
_Transform_cd30c5b4b2e5405bbe4451c6c589718b_Out_1_Vector3 = TransformWorldToObject(_Add_a2f1ce3eccd04a348677bce71231c28f_Out_2_Vector3.xyz);
description.Position = _Transform_cd30c5b4b2e5405bbe4451c6c589718b_Out_1_Vector3;
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
float4 _Property_597353718570472cb903eabbac10e98e_Out_0_Vector4 = _Color_Bottom;
float4 _Property_acf5ade3ddc74df2bb7b72f3fb5027e6_Out_0_Vector4 = _Color_Top;
UnityTexture2D _Property_0dd8d95b31054753b15cbea49c4dd049_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Gradient_Map, sampler_Gradient_Map, _Gradient_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_0dd8d95b31054753b15cbea49c4dd049_Out_0_Texture2D.tex, _Property_0dd8d95b31054753b15cbea49c4dd049_Out_0_Texture2D.samplerstate, _Property_0dd8d95b31054753b15cbea49c4dd049_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_0dd8d95b31054753b15cbea49c4dd049_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4, _Property_0dd8d95b31054753b15cbea49c4dd049_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_R_4_Float = _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4.r;
float _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_G_5_Float = _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4.g;
float _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_B_6_Float = _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4.b;
float _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_A_7_Float = _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4.a;
float4 _Lerp_45e933526a9e47429463b3d3ae7bcb99_Out_3_Vector4;
Unity_Lerp_float4(_Property_597353718570472cb903eabbac10e98e_Out_0_Vector4, _Property_acf5ade3ddc74df2bb7b72f3fb5027e6_Out_0_Vector4, (_SampleTexture2D_0139bf195dc74dea98a5142201287ca0_R_4_Float.xxxx), _Lerp_45e933526a9e47429463b3d3ae7bcb99_Out_3_Vector4);
float4 _Property_d51fad0917634f61855f006ac3a3036a_Out_0_Vector4 = _Color_Tint;
#if defined(_USE_GRADIENT_MAP)
float4 _UseGradientMap_ab913e91a75042bfa88ab1093a041f33_Out_0_Vector4 = _Lerp_45e933526a9e47429463b3d3ae7bcb99_Out_3_Vector4;
#else
float4 _UseGradientMap_ab913e91a75042bfa88ab1093a041f33_Out_0_Vector4 = _Property_d51fad0917634f61855f006ac3a3036a_Out_0_Vector4;
#endif
UnityTexture2D _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Foliage_Map, sampler_Foliage_Map, _Foliage_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.tex, _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.samplerstate, _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4, _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_R_4_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.r;
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_G_5_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.g;
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_B_6_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.b;
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_A_7_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.a;
float4 _Multiply_2d1489dbc4f84047b8a1330c23f185d7_Out_2_Vector4;
Unity_Multiply_float4_float4(_UseGradientMap_ab913e91a75042bfa88ab1093a041f33_Out_0_Vector4, _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4, _Multiply_2d1489dbc4f84047b8a1330c23f185d7_Out_2_Vector4);
float _Property_8dc6bc562c024ba58adfbb32ab0956a5_Out_0_Float = _Smoothness;
float _Property_ce8925e5051d4745a4cf3b2d4fb8db48_Out_0_Float = _Alpha_Clip;
float _Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_A_7_Float, _Property_ce8925e5051d4745a4cf3b2d4fb8db48_Out_0_Float, _Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float);
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_A_4_Float = 0;
float _Property_496eeb327965485cb724cb0c285f7a6c_Out_0_Float = _Dither_Bottom_Level;
float _Add_9a95e02d9c734b69a1bd64099eab897d_Out_2_Float;
Unity_Add_float(_Split_b2c67f57ffb14ffab00206eeffb0e2d5_G_2_Float, _Property_496eeb327965485cb724cb0c285f7a6c_Out_0_Float, _Add_9a95e02d9c734b69a1bd64099eab897d_Out_2_Float);
float _Property_ccb0e250db5745fe928b0ac7bf4d8af7_Out_0_Float = _Dither_Fade;
float _Multiply_38741f482df44a6584b848549f368772_Out_2_Float;
Unity_Multiply_float_float(_Add_9a95e02d9c734b69a1bd64099eab897d_Out_2_Float, _Property_ccb0e250db5745fe928b0ac7bf4d8af7_Out_0_Float, _Multiply_38741f482df44a6584b848549f368772_Out_2_Float);
float _Dither_22e77e4623774a7fa94d00b2ae4ff606_Out_2_Float;
Unity_Dither_float(_Multiply_38741f482df44a6584b848549f368772_Out_2_Float, float4(IN.NDCPosition.xy, 0, 0), _Dither_22e77e4623774a7fa94d00b2ae4ff606_Out_2_Float);
float _Multiply_9214bd931fea4bc98db2b9a46d79c8f8_Out_2_Float;
Unity_Multiply_float_float(_Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float, _Dither_22e77e4623774a7fa94d00b2ae4ff606_Out_2_Float, _Multiply_9214bd931fea4bc98db2b9a46d79c8f8_Out_2_Float);
#if defined(_USE_DITHER)
float _UseDither_4bfe8155f1434a3d9ded012dece1de0e_Out_0_Float = _Multiply_9214bd931fea4bc98db2b9a46d79c8f8_Out_2_Float;
#else
float _UseDither_4bfe8155f1434a3d9ded012dece1de0e_Out_0_Float = _Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float;
#endif
surface.BaseColor = (_Multiply_2d1489dbc4f84047b8a1330c23f185d7_Out_2_Vector4.xyz);
surface.NormalTS = IN.TangentSpaceNormal;
surface.Emission = float3(0, 0, 0);
surface.Metallic = float(0);
surface.Smoothness = _Property_8dc6bc562c024ba58adfbb32ab0956a5_Out_0_Float;
surface.Occlusion = float(1);
surface.Alpha = _UseDither_4bfe8155f1434a3d9ded012dece1de0e_Out_0_Float;
surface.AlphaClipThreshold = float(0.5);
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
    output.WorldSpaceNormal =                           TransformObjectToWorldNormal(input.normalOS);
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
    output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
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

    



    output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);


    output.ObjectSpacePosition = TransformWorldToObject(input.positionWS);

    #if UNITY_UV_STARTS_AT_TOP
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #else
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #endif

    output.NDCPosition = output.PixelPosition.xy / _ScreenParams.xy;
    output.NDCPosition.y = 1.0f - output.NDCPosition.y;

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
    result.color      = attributes.color;
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
#pragma shader_feature_local _ _USE_DITHER
#pragma shader_feature_local _ _USE_GRADIENT_MAP
#pragma shader_feature_local _ _USE_VERTEX_COLOR

#if defined(_USE_DITHER) && defined(_USE_GRADIENT_MAP) && defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_DITHER) && defined(_USE_GRADIENT_MAP)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_DITHER) && defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_2
#elif defined(_USE_DITHER)
#define KEYWORD_PERMUTATION_3
#elif defined(_USE_GRADIENT_MAP) && defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_4
#elif defined(_USE_GRADIENT_MAP)
#define KEYWORD_PERMUTATION_5
#elif defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_6
#else
#define KEYWORD_PERMUTATION_7
#endif


// Defines
#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_TEXCOORD1
#define ATTRIBUTES_NEED_COLOR
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
 float3 TangentSpaceNormal;
 float3 ObjectSpacePosition;
 float2 NDCPosition;
 float2 PixelPosition;
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 WorldSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 WorldSpacePosition;
 float4 uv0;
 float4 VertexColor;
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

float4 _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D_TexelSize;
float4 _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D_TexelSize;
float _Wind_Small_Scale;
float2 _Wind_Small_Speed;
float _Wind_Small_Intensity;
float _Wind_Large_Scale;
float2 _Wind_Large_Speed;
float4 _Color_Bottom;
float _Wind_Large_Intensity;
float _UV_Pivot_Lock_Strangth;
float _Dither_Bottom_Level;
float _Dither_Fade;
float4 _Foliage_Map_TexelSize;
float _Alpha_Clip;
float4 _Color_Tint;
float4 _Gradient_Map_TexelSize;
float4 _Color_Top;
float _Smoothness;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D);
TEXTURE2D(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D);
TEXTURE2D(_Foliage_Map);
SAMPLER(sampler_Foliage_Map);
TEXTURE2D(_Gradient_Map);
SAMPLER(sampler_Gradient_Map);

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

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

void Unity_Dither_float(float In, float4 ScreenPosition, out float Out)
{
    float2 uv = ScreenPosition.xy * _ScreenParams.xy;
    static const float DITHER_THRESHOLDS[16] =
    {
        1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
        13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
        4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
        16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
    };
    uint index = (uint(uv.x) % 4) * 4 + uint(uv.y) % 4;
    Out = In - DITHER_THRESHOLDS[index];
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
float _Property_ad4aa287fed64295a604d3291d1959a2_Out_0_Float = _Wind_Large_Scale;
float2 _Property_c2f76a94f588455e9b4c07b23d9ef2ff_Out_0_Vector2 = _Wind_Large_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_bad92adcb02c4fc28767432489546115;
_PositionalUVs_bad92adcb02c4fc28767432489546115.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_bad92adcb02c4fc28767432489546115.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_bad92adcb02c4fc28767432489546115_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_ad4aa287fed64295a604d3291d1959a2_Out_0_Float, _Property_c2f76a94f588455e9b4c07b23d9ef2ff_Out_0_Vector2, _PositionalUVs_bad92adcb02c4fc28767432489546115, _PositionalUVs_bad92adcb02c4fc28767432489546115_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_bad92adcb02c4fc28767432489546115_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_R_5_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_G_6_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_B_7_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_A_8_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.a;
float _Subtract_218acd7847974a5cbb60429ba8096400_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_R_5_Float, float(0.5), _Subtract_218acd7847974a5cbb60429ba8096400_Out_2_Float);
float _Split_9060b9716ec549aeacc51b6fd9846e1f_R_1_Float = IN.VertexColor[0];
float _Split_9060b9716ec549aeacc51b6fd9846e1f_G_2_Float = IN.VertexColor[1];
float _Split_9060b9716ec549aeacc51b6fd9846e1f_B_3_Float = IN.VertexColor[2];
float _Split_9060b9716ec549aeacc51b6fd9846e1f_A_4_Float = IN.VertexColor[3];
float4 _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4 = IN.uv0;
float _Split_cd4e6782516b4c4eba58883246f59068_R_1_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[0];
float _Split_cd4e6782516b4c4eba58883246f59068_G_2_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[1];
float _Split_cd4e6782516b4c4eba58883246f59068_B_3_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[2];
float _Split_cd4e6782516b4c4eba58883246f59068_A_4_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[3];
float _Property_8494919c92594e95b9e93cc374ab477d_Out_0_Float = _UV_Pivot_Lock_Strangth;
float _Power_a22a61dbae424f388acc9bbf24d84825_Out_2_Float;
Unity_Power_float(_Split_cd4e6782516b4c4eba58883246f59068_G_2_Float, _Property_8494919c92594e95b9e93cc374ab477d_Out_0_Float, _Power_a22a61dbae424f388acc9bbf24d84825_Out_2_Float);
#if defined(_USE_VERTEX_COLOR)
float _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float = _Split_9060b9716ec549aeacc51b6fd9846e1f_R_1_Float;
#else
float _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float = _Power_a22a61dbae424f388acc9bbf24d84825_Out_2_Float;
#endif
float _Multiply_ea0e0a96337840389ee116bcbf36e668_Out_2_Float;
Unity_Multiply_float_float(_Subtract_218acd7847974a5cbb60429ba8096400_Out_2_Float, _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float, _Multiply_ea0e0a96337840389ee116bcbf36e668_Out_2_Float);
float _Property_dbbb11cdd2654ae79939bef8b5ecd875_Out_0_Float = _Wind_Large_Intensity;
float _Multiply_6869889d47d046e8b5ad71f9c3a9adb4_Out_2_Float;
Unity_Multiply_float_float(_Multiply_ea0e0a96337840389ee116bcbf36e668_Out_2_Float, _Property_dbbb11cdd2654ae79939bef8b5ecd875_Out_0_Float, _Multiply_6869889d47d046e8b5ad71f9c3a9adb4_Out_2_Float);
float _Property_c327c41c045b40c483eb7da471e0c3fd_Out_0_Float = _Wind_Small_Scale;
float2 _Property_92349f3d399742e5b93d3ec2912b6d0b_Out_0_Vector2 = _Wind_Small_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_61edec0a11644d95b38796bd3e28715a;
_PositionalUVs_61edec0a11644d95b38796bd3e28715a.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_61edec0a11644d95b38796bd3e28715a.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_61edec0a11644d95b38796bd3e28715a_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_c327c41c045b40c483eb7da471e0c3fd_Out_0_Float, _Property_92349f3d399742e5b93d3ec2912b6d0b_Out_0_Vector2, _PositionalUVs_61edec0a11644d95b38796bd3e28715a, _PositionalUVs_61edec0a11644d95b38796bd3e28715a_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_61edec0a11644d95b38796bd3e28715a_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_R_5_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_G_6_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_B_7_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_A_8_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.a;
float _Subtract_38999ff8e5a04271b3794daa96fc9cca_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_R_5_Float, float(0.5), _Subtract_38999ff8e5a04271b3794daa96fc9cca_Out_2_Float);
float _Multiply_4209b0f1a88b424992ddafb6610c2968_Out_2_Float;
Unity_Multiply_float_float(_UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float, _Subtract_38999ff8e5a04271b3794daa96fc9cca_Out_2_Float, _Multiply_4209b0f1a88b424992ddafb6610c2968_Out_2_Float);
float _Property_2bd20e0e45834464a28f6379b9e8225c_Out_0_Float = _Wind_Small_Intensity;
float _Multiply_1c0b4b06455e4d31b5ad6f8c28d20004_Out_2_Float;
Unity_Multiply_float_float(_Multiply_4209b0f1a88b424992ddafb6610c2968_Out_2_Float, _Property_2bd20e0e45834464a28f6379b9e8225c_Out_0_Float, _Multiply_1c0b4b06455e4d31b5ad6f8c28d20004_Out_2_Float);
float _Add_0407a76ed876497f9c558311ba6b5eb8_Out_2_Float;
Unity_Add_float(_Multiply_6869889d47d046e8b5ad71f9c3a9adb4_Out_2_Float, _Multiply_1c0b4b06455e4d31b5ad6f8c28d20004_Out_2_Float, _Add_0407a76ed876497f9c558311ba6b5eb8_Out_2_Float);
float3 _Multiply_5d424720302c4e55afef4b8a9f76d3ef_Out_2_Vector3;
Unity_Multiply_float3_float3((_Add_0407a76ed876497f9c558311ba6b5eb8_Out_2_Float.xxx), IN.WorldSpaceNormal, _Multiply_5d424720302c4e55afef4b8a9f76d3ef_Out_2_Vector3);
float3 _Add_0a8e7ddaf6aa4278a2f9912909eba29d_Out_2_Vector3;
Unity_Add_float3(_Multiply_5d424720302c4e55afef4b8a9f76d3ef_Out_2_Vector3, IN.WorldSpacePosition, _Add_0a8e7ddaf6aa4278a2f9912909eba29d_Out_2_Vector3);
float3 _UPGFoliageBendWSCustomFunction_3d8d6250d27b4f3b96cbc7767f9ae45d_offsetWS_2_Vector3;
UPG_FoliageBendWS_float(IN.WorldSpacePosition, _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float, _UPGFoliageBendWSCustomFunction_3d8d6250d27b4f3b96cbc7767f9ae45d_offsetWS_2_Vector3);
float3 _Add_a2f1ce3eccd04a348677bce71231c28f_Out_2_Vector3;
Unity_Add_float3(_Add_0a8e7ddaf6aa4278a2f9912909eba29d_Out_2_Vector3, _UPGFoliageBendWSCustomFunction_3d8d6250d27b4f3b96cbc7767f9ae45d_offsetWS_2_Vector3, _Add_a2f1ce3eccd04a348677bce71231c28f_Out_2_Vector3);
float3 _Transform_cd30c5b4b2e5405bbe4451c6c589718b_Out_1_Vector3;
_Transform_cd30c5b4b2e5405bbe4451c6c589718b_Out_1_Vector3 = TransformWorldToObject(_Add_a2f1ce3eccd04a348677bce71231c28f_Out_2_Vector3.xyz);
description.Position = _Transform_cd30c5b4b2e5405bbe4451c6c589718b_Out_1_Vector3;
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
float4 _Property_597353718570472cb903eabbac10e98e_Out_0_Vector4 = _Color_Bottom;
float4 _Property_acf5ade3ddc74df2bb7b72f3fb5027e6_Out_0_Vector4 = _Color_Top;
UnityTexture2D _Property_0dd8d95b31054753b15cbea49c4dd049_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Gradient_Map, sampler_Gradient_Map, _Gradient_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_0dd8d95b31054753b15cbea49c4dd049_Out_0_Texture2D.tex, _Property_0dd8d95b31054753b15cbea49c4dd049_Out_0_Texture2D.samplerstate, _Property_0dd8d95b31054753b15cbea49c4dd049_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_0dd8d95b31054753b15cbea49c4dd049_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4, _Property_0dd8d95b31054753b15cbea49c4dd049_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_R_4_Float = _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4.r;
float _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_G_5_Float = _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4.g;
float _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_B_6_Float = _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4.b;
float _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_A_7_Float = _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4.a;
float4 _Lerp_45e933526a9e47429463b3d3ae7bcb99_Out_3_Vector4;
Unity_Lerp_float4(_Property_597353718570472cb903eabbac10e98e_Out_0_Vector4, _Property_acf5ade3ddc74df2bb7b72f3fb5027e6_Out_0_Vector4, (_SampleTexture2D_0139bf195dc74dea98a5142201287ca0_R_4_Float.xxxx), _Lerp_45e933526a9e47429463b3d3ae7bcb99_Out_3_Vector4);
float4 _Property_d51fad0917634f61855f006ac3a3036a_Out_0_Vector4 = _Color_Tint;
#if defined(_USE_GRADIENT_MAP)
float4 _UseGradientMap_ab913e91a75042bfa88ab1093a041f33_Out_0_Vector4 = _Lerp_45e933526a9e47429463b3d3ae7bcb99_Out_3_Vector4;
#else
float4 _UseGradientMap_ab913e91a75042bfa88ab1093a041f33_Out_0_Vector4 = _Property_d51fad0917634f61855f006ac3a3036a_Out_0_Vector4;
#endif
UnityTexture2D _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Foliage_Map, sampler_Foliage_Map, _Foliage_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.tex, _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.samplerstate, _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4, _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_R_4_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.r;
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_G_5_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.g;
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_B_6_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.b;
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_A_7_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.a;
float4 _Multiply_2d1489dbc4f84047b8a1330c23f185d7_Out_2_Vector4;
Unity_Multiply_float4_float4(_UseGradientMap_ab913e91a75042bfa88ab1093a041f33_Out_0_Vector4, _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4, _Multiply_2d1489dbc4f84047b8a1330c23f185d7_Out_2_Vector4);
float _Property_8dc6bc562c024ba58adfbb32ab0956a5_Out_0_Float = _Smoothness;
float _Property_ce8925e5051d4745a4cf3b2d4fb8db48_Out_0_Float = _Alpha_Clip;
float _Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_A_7_Float, _Property_ce8925e5051d4745a4cf3b2d4fb8db48_Out_0_Float, _Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float);
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_A_4_Float = 0;
float _Property_496eeb327965485cb724cb0c285f7a6c_Out_0_Float = _Dither_Bottom_Level;
float _Add_9a95e02d9c734b69a1bd64099eab897d_Out_2_Float;
Unity_Add_float(_Split_b2c67f57ffb14ffab00206eeffb0e2d5_G_2_Float, _Property_496eeb327965485cb724cb0c285f7a6c_Out_0_Float, _Add_9a95e02d9c734b69a1bd64099eab897d_Out_2_Float);
float _Property_ccb0e250db5745fe928b0ac7bf4d8af7_Out_0_Float = _Dither_Fade;
float _Multiply_38741f482df44a6584b848549f368772_Out_2_Float;
Unity_Multiply_float_float(_Add_9a95e02d9c734b69a1bd64099eab897d_Out_2_Float, _Property_ccb0e250db5745fe928b0ac7bf4d8af7_Out_0_Float, _Multiply_38741f482df44a6584b848549f368772_Out_2_Float);
float _Dither_22e77e4623774a7fa94d00b2ae4ff606_Out_2_Float;
Unity_Dither_float(_Multiply_38741f482df44a6584b848549f368772_Out_2_Float, float4(IN.NDCPosition.xy, 0, 0), _Dither_22e77e4623774a7fa94d00b2ae4ff606_Out_2_Float);
float _Multiply_9214bd931fea4bc98db2b9a46d79c8f8_Out_2_Float;
Unity_Multiply_float_float(_Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float, _Dither_22e77e4623774a7fa94d00b2ae4ff606_Out_2_Float, _Multiply_9214bd931fea4bc98db2b9a46d79c8f8_Out_2_Float);
#if defined(_USE_DITHER)
float _UseDither_4bfe8155f1434a3d9ded012dece1de0e_Out_0_Float = _Multiply_9214bd931fea4bc98db2b9a46d79c8f8_Out_2_Float;
#else
float _UseDither_4bfe8155f1434a3d9ded012dece1de0e_Out_0_Float = _Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float;
#endif
surface.BaseColor = (_Multiply_2d1489dbc4f84047b8a1330c23f185d7_Out_2_Vector4.xyz);
surface.NormalTS = IN.TangentSpaceNormal;
surface.Emission = float3(0, 0, 0);
surface.Metallic = float(0);
surface.Smoothness = _Property_8dc6bc562c024ba58adfbb32ab0956a5_Out_0_Float;
surface.Occlusion = float(1);
surface.Alpha = _UseDither_4bfe8155f1434a3d9ded012dece1de0e_Out_0_Float;
surface.AlphaClipThreshold = float(0.5);
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
    output.WorldSpaceNormal =                           TransformObjectToWorldNormal(input.normalOS);
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
    output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
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

    



    output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);


    output.ObjectSpacePosition = TransformWorldToObject(input.positionWS);

    #if UNITY_UV_STARTS_AT_TOP
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #else
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #endif

    output.NDCPosition = output.PixelPosition.xy / _ScreenParams.xy;
    output.NDCPosition.y = 1.0f - output.NDCPosition.y;

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
    result.color      = attributes.color;
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
#pragma shader_feature_local _ _USE_DITHER
#pragma shader_feature_local _ _USE_GRADIENT_MAP
#pragma shader_feature_local _ _USE_VERTEX_COLOR

#if defined(_USE_DITHER) && defined(_USE_GRADIENT_MAP) && defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_DITHER) && defined(_USE_GRADIENT_MAP)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_DITHER) && defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_2
#elif defined(_USE_DITHER)
#define KEYWORD_PERMUTATION_3
#elif defined(_USE_GRADIENT_MAP) && defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_4
#elif defined(_USE_GRADIENT_MAP)
#define KEYWORD_PERMUTATION_5
#elif defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_6
#else
#define KEYWORD_PERMUTATION_7
#endif


// Defines
#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_COLOR
#define VARYINGS_NEED_POSITION_WS
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
 float4 color : COLOR;
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
 float3 ObjectSpacePosition;
 float2 NDCPosition;
 float2 PixelPosition;
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 WorldSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 WorldSpacePosition;
 float4 uv0;
 float4 VertexColor;
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

float4 _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D_TexelSize;
float4 _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D_TexelSize;
float _Wind_Small_Scale;
float2 _Wind_Small_Speed;
float _Wind_Small_Intensity;
float _Wind_Large_Scale;
float2 _Wind_Large_Speed;
float4 _Color_Bottom;
float _Wind_Large_Intensity;
float _UV_Pivot_Lock_Strangth;
float _Dither_Bottom_Level;
float _Dither_Fade;
float4 _Foliage_Map_TexelSize;
float _Alpha_Clip;
float4 _Color_Tint;
float4 _Gradient_Map_TexelSize;
float4 _Color_Top;
float _Smoothness;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D);
TEXTURE2D(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D);
TEXTURE2D(_Foliage_Map);
SAMPLER(sampler_Foliage_Map);
TEXTURE2D(_Gradient_Map);
SAMPLER(sampler_Gradient_Map);

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

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Dither_float(float In, float4 ScreenPosition, out float Out)
{
    float2 uv = ScreenPosition.xy * _ScreenParams.xy;
    static const float DITHER_THRESHOLDS[16] =
    {
        1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
        13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
        4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
        16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
    };
    uint index = (uint(uv.x) % 4) * 4 + uint(uv.y) % 4;
    Out = In - DITHER_THRESHOLDS[index];
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
float _Property_ad4aa287fed64295a604d3291d1959a2_Out_0_Float = _Wind_Large_Scale;
float2 _Property_c2f76a94f588455e9b4c07b23d9ef2ff_Out_0_Vector2 = _Wind_Large_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_bad92adcb02c4fc28767432489546115;
_PositionalUVs_bad92adcb02c4fc28767432489546115.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_bad92adcb02c4fc28767432489546115.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_bad92adcb02c4fc28767432489546115_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_ad4aa287fed64295a604d3291d1959a2_Out_0_Float, _Property_c2f76a94f588455e9b4c07b23d9ef2ff_Out_0_Vector2, _PositionalUVs_bad92adcb02c4fc28767432489546115, _PositionalUVs_bad92adcb02c4fc28767432489546115_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_bad92adcb02c4fc28767432489546115_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_R_5_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_G_6_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_B_7_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_A_8_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.a;
float _Subtract_218acd7847974a5cbb60429ba8096400_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_R_5_Float, float(0.5), _Subtract_218acd7847974a5cbb60429ba8096400_Out_2_Float);
float _Split_9060b9716ec549aeacc51b6fd9846e1f_R_1_Float = IN.VertexColor[0];
float _Split_9060b9716ec549aeacc51b6fd9846e1f_G_2_Float = IN.VertexColor[1];
float _Split_9060b9716ec549aeacc51b6fd9846e1f_B_3_Float = IN.VertexColor[2];
float _Split_9060b9716ec549aeacc51b6fd9846e1f_A_4_Float = IN.VertexColor[3];
float4 _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4 = IN.uv0;
float _Split_cd4e6782516b4c4eba58883246f59068_R_1_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[0];
float _Split_cd4e6782516b4c4eba58883246f59068_G_2_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[1];
float _Split_cd4e6782516b4c4eba58883246f59068_B_3_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[2];
float _Split_cd4e6782516b4c4eba58883246f59068_A_4_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[3];
float _Property_8494919c92594e95b9e93cc374ab477d_Out_0_Float = _UV_Pivot_Lock_Strangth;
float _Power_a22a61dbae424f388acc9bbf24d84825_Out_2_Float;
Unity_Power_float(_Split_cd4e6782516b4c4eba58883246f59068_G_2_Float, _Property_8494919c92594e95b9e93cc374ab477d_Out_0_Float, _Power_a22a61dbae424f388acc9bbf24d84825_Out_2_Float);
#if defined(_USE_VERTEX_COLOR)
float _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float = _Split_9060b9716ec549aeacc51b6fd9846e1f_R_1_Float;
#else
float _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float = _Power_a22a61dbae424f388acc9bbf24d84825_Out_2_Float;
#endif
float _Multiply_ea0e0a96337840389ee116bcbf36e668_Out_2_Float;
Unity_Multiply_float_float(_Subtract_218acd7847974a5cbb60429ba8096400_Out_2_Float, _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float, _Multiply_ea0e0a96337840389ee116bcbf36e668_Out_2_Float);
float _Property_dbbb11cdd2654ae79939bef8b5ecd875_Out_0_Float = _Wind_Large_Intensity;
float _Multiply_6869889d47d046e8b5ad71f9c3a9adb4_Out_2_Float;
Unity_Multiply_float_float(_Multiply_ea0e0a96337840389ee116bcbf36e668_Out_2_Float, _Property_dbbb11cdd2654ae79939bef8b5ecd875_Out_0_Float, _Multiply_6869889d47d046e8b5ad71f9c3a9adb4_Out_2_Float);
float _Property_c327c41c045b40c483eb7da471e0c3fd_Out_0_Float = _Wind_Small_Scale;
float2 _Property_92349f3d399742e5b93d3ec2912b6d0b_Out_0_Vector2 = _Wind_Small_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_61edec0a11644d95b38796bd3e28715a;
_PositionalUVs_61edec0a11644d95b38796bd3e28715a.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_61edec0a11644d95b38796bd3e28715a.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_61edec0a11644d95b38796bd3e28715a_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_c327c41c045b40c483eb7da471e0c3fd_Out_0_Float, _Property_92349f3d399742e5b93d3ec2912b6d0b_Out_0_Vector2, _PositionalUVs_61edec0a11644d95b38796bd3e28715a, _PositionalUVs_61edec0a11644d95b38796bd3e28715a_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_61edec0a11644d95b38796bd3e28715a_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_R_5_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_G_6_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_B_7_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_A_8_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.a;
float _Subtract_38999ff8e5a04271b3794daa96fc9cca_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_R_5_Float, float(0.5), _Subtract_38999ff8e5a04271b3794daa96fc9cca_Out_2_Float);
float _Multiply_4209b0f1a88b424992ddafb6610c2968_Out_2_Float;
Unity_Multiply_float_float(_UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float, _Subtract_38999ff8e5a04271b3794daa96fc9cca_Out_2_Float, _Multiply_4209b0f1a88b424992ddafb6610c2968_Out_2_Float);
float _Property_2bd20e0e45834464a28f6379b9e8225c_Out_0_Float = _Wind_Small_Intensity;
float _Multiply_1c0b4b06455e4d31b5ad6f8c28d20004_Out_2_Float;
Unity_Multiply_float_float(_Multiply_4209b0f1a88b424992ddafb6610c2968_Out_2_Float, _Property_2bd20e0e45834464a28f6379b9e8225c_Out_0_Float, _Multiply_1c0b4b06455e4d31b5ad6f8c28d20004_Out_2_Float);
float _Add_0407a76ed876497f9c558311ba6b5eb8_Out_2_Float;
Unity_Add_float(_Multiply_6869889d47d046e8b5ad71f9c3a9adb4_Out_2_Float, _Multiply_1c0b4b06455e4d31b5ad6f8c28d20004_Out_2_Float, _Add_0407a76ed876497f9c558311ba6b5eb8_Out_2_Float);
float3 _Multiply_5d424720302c4e55afef4b8a9f76d3ef_Out_2_Vector3;
Unity_Multiply_float3_float3((_Add_0407a76ed876497f9c558311ba6b5eb8_Out_2_Float.xxx), IN.WorldSpaceNormal, _Multiply_5d424720302c4e55afef4b8a9f76d3ef_Out_2_Vector3);
float3 _Add_0a8e7ddaf6aa4278a2f9912909eba29d_Out_2_Vector3;
Unity_Add_float3(_Multiply_5d424720302c4e55afef4b8a9f76d3ef_Out_2_Vector3, IN.WorldSpacePosition, _Add_0a8e7ddaf6aa4278a2f9912909eba29d_Out_2_Vector3);
float3 _UPGFoliageBendWSCustomFunction_3d8d6250d27b4f3b96cbc7767f9ae45d_offsetWS_2_Vector3;
UPG_FoliageBendWS_float(IN.WorldSpacePosition, _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float, _UPGFoliageBendWSCustomFunction_3d8d6250d27b4f3b96cbc7767f9ae45d_offsetWS_2_Vector3);
float3 _Add_a2f1ce3eccd04a348677bce71231c28f_Out_2_Vector3;
Unity_Add_float3(_Add_0a8e7ddaf6aa4278a2f9912909eba29d_Out_2_Vector3, _UPGFoliageBendWSCustomFunction_3d8d6250d27b4f3b96cbc7767f9ae45d_offsetWS_2_Vector3, _Add_a2f1ce3eccd04a348677bce71231c28f_Out_2_Vector3);
float3 _Transform_cd30c5b4b2e5405bbe4451c6c589718b_Out_1_Vector3;
_Transform_cd30c5b4b2e5405bbe4451c6c589718b_Out_1_Vector3 = TransformWorldToObject(_Add_a2f1ce3eccd04a348677bce71231c28f_Out_2_Vector3.xyz);
description.Position = _Transform_cd30c5b4b2e5405bbe4451c6c589718b_Out_1_Vector3;
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
UnityTexture2D _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Foliage_Map, sampler_Foliage_Map, _Foliage_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.tex, _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.samplerstate, _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4, _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_R_4_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.r;
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_G_5_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.g;
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_B_6_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.b;
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_A_7_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.a;
float _Property_ce8925e5051d4745a4cf3b2d4fb8db48_Out_0_Float = _Alpha_Clip;
float _Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_A_7_Float, _Property_ce8925e5051d4745a4cf3b2d4fb8db48_Out_0_Float, _Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float);
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_A_4_Float = 0;
float _Property_496eeb327965485cb724cb0c285f7a6c_Out_0_Float = _Dither_Bottom_Level;
float _Add_9a95e02d9c734b69a1bd64099eab897d_Out_2_Float;
Unity_Add_float(_Split_b2c67f57ffb14ffab00206eeffb0e2d5_G_2_Float, _Property_496eeb327965485cb724cb0c285f7a6c_Out_0_Float, _Add_9a95e02d9c734b69a1bd64099eab897d_Out_2_Float);
float _Property_ccb0e250db5745fe928b0ac7bf4d8af7_Out_0_Float = _Dither_Fade;
float _Multiply_38741f482df44a6584b848549f368772_Out_2_Float;
Unity_Multiply_float_float(_Add_9a95e02d9c734b69a1bd64099eab897d_Out_2_Float, _Property_ccb0e250db5745fe928b0ac7bf4d8af7_Out_0_Float, _Multiply_38741f482df44a6584b848549f368772_Out_2_Float);
float _Dither_22e77e4623774a7fa94d00b2ae4ff606_Out_2_Float;
Unity_Dither_float(_Multiply_38741f482df44a6584b848549f368772_Out_2_Float, float4(IN.NDCPosition.xy, 0, 0), _Dither_22e77e4623774a7fa94d00b2ae4ff606_Out_2_Float);
float _Multiply_9214bd931fea4bc98db2b9a46d79c8f8_Out_2_Float;
Unity_Multiply_float_float(_Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float, _Dither_22e77e4623774a7fa94d00b2ae4ff606_Out_2_Float, _Multiply_9214bd931fea4bc98db2b9a46d79c8f8_Out_2_Float);
#if defined(_USE_DITHER)
float _UseDither_4bfe8155f1434a3d9ded012dece1de0e_Out_0_Float = _Multiply_9214bd931fea4bc98db2b9a46d79c8f8_Out_2_Float;
#else
float _UseDither_4bfe8155f1434a3d9ded012dece1de0e_Out_0_Float = _Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float;
#endif
surface.Alpha = _UseDither_4bfe8155f1434a3d9ded012dece1de0e_Out_0_Float;
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
    output.WorldSpaceNormal =                           TransformObjectToWorldNormal(input.normalOS);
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
    output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
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

    





    output.ObjectSpacePosition = TransformWorldToObject(input.positionWS);

    #if UNITY_UV_STARTS_AT_TOP
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #else
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #endif

    output.NDCPosition = output.PixelPosition.xy / _ScreenParams.xy;
    output.NDCPosition.y = 1.0f - output.NDCPosition.y;

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
    result.color      = attributes.color;
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
#pragma shader_feature_local _ _USE_DITHER
#pragma shader_feature_local _ _USE_GRADIENT_MAP
#pragma shader_feature_local _ _USE_VERTEX_COLOR

#if defined(_USE_DITHER) && defined(_USE_GRADIENT_MAP) && defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_DITHER) && defined(_USE_GRADIENT_MAP)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_DITHER) && defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_2
#elif defined(_USE_DITHER)
#define KEYWORD_PERMUTATION_3
#elif defined(_USE_GRADIENT_MAP) && defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_4
#elif defined(_USE_GRADIENT_MAP)
#define KEYWORD_PERMUTATION_5
#elif defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_6
#else
#define KEYWORD_PERMUTATION_7
#endif


// Defines
#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_COLOR
#define VARYINGS_NEED_POSITION_WS
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
 float4 color : COLOR;
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
 float3 ObjectSpacePosition;
 float2 NDCPosition;
 float2 PixelPosition;
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 WorldSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 WorldSpacePosition;
 float4 uv0;
 float4 VertexColor;
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

float4 _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D_TexelSize;
float4 _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D_TexelSize;
float _Wind_Small_Scale;
float2 _Wind_Small_Speed;
float _Wind_Small_Intensity;
float _Wind_Large_Scale;
float2 _Wind_Large_Speed;
float4 _Color_Bottom;
float _Wind_Large_Intensity;
float _UV_Pivot_Lock_Strangth;
float _Dither_Bottom_Level;
float _Dither_Fade;
float4 _Foliage_Map_TexelSize;
float _Alpha_Clip;
float4 _Color_Tint;
float4 _Gradient_Map_TexelSize;
float4 _Color_Top;
float _Smoothness;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D);
TEXTURE2D(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D);
TEXTURE2D(_Foliage_Map);
SAMPLER(sampler_Foliage_Map);
TEXTURE2D(_Gradient_Map);
SAMPLER(sampler_Gradient_Map);

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

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Dither_float(float In, float4 ScreenPosition, out float Out)
{
    float2 uv = ScreenPosition.xy * _ScreenParams.xy;
    static const float DITHER_THRESHOLDS[16] =
    {
        1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
        13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
        4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
        16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
    };
    uint index = (uint(uv.x) % 4) * 4 + uint(uv.y) % 4;
    Out = In - DITHER_THRESHOLDS[index];
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
float _Property_ad4aa287fed64295a604d3291d1959a2_Out_0_Float = _Wind_Large_Scale;
float2 _Property_c2f76a94f588455e9b4c07b23d9ef2ff_Out_0_Vector2 = _Wind_Large_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_bad92adcb02c4fc28767432489546115;
_PositionalUVs_bad92adcb02c4fc28767432489546115.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_bad92adcb02c4fc28767432489546115.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_bad92adcb02c4fc28767432489546115_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_ad4aa287fed64295a604d3291d1959a2_Out_0_Float, _Property_c2f76a94f588455e9b4c07b23d9ef2ff_Out_0_Vector2, _PositionalUVs_bad92adcb02c4fc28767432489546115, _PositionalUVs_bad92adcb02c4fc28767432489546115_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_bad92adcb02c4fc28767432489546115_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_R_5_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_G_6_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_B_7_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_A_8_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.a;
float _Subtract_218acd7847974a5cbb60429ba8096400_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_R_5_Float, float(0.5), _Subtract_218acd7847974a5cbb60429ba8096400_Out_2_Float);
float _Split_9060b9716ec549aeacc51b6fd9846e1f_R_1_Float = IN.VertexColor[0];
float _Split_9060b9716ec549aeacc51b6fd9846e1f_G_2_Float = IN.VertexColor[1];
float _Split_9060b9716ec549aeacc51b6fd9846e1f_B_3_Float = IN.VertexColor[2];
float _Split_9060b9716ec549aeacc51b6fd9846e1f_A_4_Float = IN.VertexColor[3];
float4 _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4 = IN.uv0;
float _Split_cd4e6782516b4c4eba58883246f59068_R_1_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[0];
float _Split_cd4e6782516b4c4eba58883246f59068_G_2_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[1];
float _Split_cd4e6782516b4c4eba58883246f59068_B_3_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[2];
float _Split_cd4e6782516b4c4eba58883246f59068_A_4_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[3];
float _Property_8494919c92594e95b9e93cc374ab477d_Out_0_Float = _UV_Pivot_Lock_Strangth;
float _Power_a22a61dbae424f388acc9bbf24d84825_Out_2_Float;
Unity_Power_float(_Split_cd4e6782516b4c4eba58883246f59068_G_2_Float, _Property_8494919c92594e95b9e93cc374ab477d_Out_0_Float, _Power_a22a61dbae424f388acc9bbf24d84825_Out_2_Float);
#if defined(_USE_VERTEX_COLOR)
float _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float = _Split_9060b9716ec549aeacc51b6fd9846e1f_R_1_Float;
#else
float _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float = _Power_a22a61dbae424f388acc9bbf24d84825_Out_2_Float;
#endif
float _Multiply_ea0e0a96337840389ee116bcbf36e668_Out_2_Float;
Unity_Multiply_float_float(_Subtract_218acd7847974a5cbb60429ba8096400_Out_2_Float, _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float, _Multiply_ea0e0a96337840389ee116bcbf36e668_Out_2_Float);
float _Property_dbbb11cdd2654ae79939bef8b5ecd875_Out_0_Float = _Wind_Large_Intensity;
float _Multiply_6869889d47d046e8b5ad71f9c3a9adb4_Out_2_Float;
Unity_Multiply_float_float(_Multiply_ea0e0a96337840389ee116bcbf36e668_Out_2_Float, _Property_dbbb11cdd2654ae79939bef8b5ecd875_Out_0_Float, _Multiply_6869889d47d046e8b5ad71f9c3a9adb4_Out_2_Float);
float _Property_c327c41c045b40c483eb7da471e0c3fd_Out_0_Float = _Wind_Small_Scale;
float2 _Property_92349f3d399742e5b93d3ec2912b6d0b_Out_0_Vector2 = _Wind_Small_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_61edec0a11644d95b38796bd3e28715a;
_PositionalUVs_61edec0a11644d95b38796bd3e28715a.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_61edec0a11644d95b38796bd3e28715a.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_61edec0a11644d95b38796bd3e28715a_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_c327c41c045b40c483eb7da471e0c3fd_Out_0_Float, _Property_92349f3d399742e5b93d3ec2912b6d0b_Out_0_Vector2, _PositionalUVs_61edec0a11644d95b38796bd3e28715a, _PositionalUVs_61edec0a11644d95b38796bd3e28715a_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_61edec0a11644d95b38796bd3e28715a_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_R_5_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_G_6_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_B_7_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_A_8_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.a;
float _Subtract_38999ff8e5a04271b3794daa96fc9cca_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_R_5_Float, float(0.5), _Subtract_38999ff8e5a04271b3794daa96fc9cca_Out_2_Float);
float _Multiply_4209b0f1a88b424992ddafb6610c2968_Out_2_Float;
Unity_Multiply_float_float(_UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float, _Subtract_38999ff8e5a04271b3794daa96fc9cca_Out_2_Float, _Multiply_4209b0f1a88b424992ddafb6610c2968_Out_2_Float);
float _Property_2bd20e0e45834464a28f6379b9e8225c_Out_0_Float = _Wind_Small_Intensity;
float _Multiply_1c0b4b06455e4d31b5ad6f8c28d20004_Out_2_Float;
Unity_Multiply_float_float(_Multiply_4209b0f1a88b424992ddafb6610c2968_Out_2_Float, _Property_2bd20e0e45834464a28f6379b9e8225c_Out_0_Float, _Multiply_1c0b4b06455e4d31b5ad6f8c28d20004_Out_2_Float);
float _Add_0407a76ed876497f9c558311ba6b5eb8_Out_2_Float;
Unity_Add_float(_Multiply_6869889d47d046e8b5ad71f9c3a9adb4_Out_2_Float, _Multiply_1c0b4b06455e4d31b5ad6f8c28d20004_Out_2_Float, _Add_0407a76ed876497f9c558311ba6b5eb8_Out_2_Float);
float3 _Multiply_5d424720302c4e55afef4b8a9f76d3ef_Out_2_Vector3;
Unity_Multiply_float3_float3((_Add_0407a76ed876497f9c558311ba6b5eb8_Out_2_Float.xxx), IN.WorldSpaceNormal, _Multiply_5d424720302c4e55afef4b8a9f76d3ef_Out_2_Vector3);
float3 _Add_0a8e7ddaf6aa4278a2f9912909eba29d_Out_2_Vector3;
Unity_Add_float3(_Multiply_5d424720302c4e55afef4b8a9f76d3ef_Out_2_Vector3, IN.WorldSpacePosition, _Add_0a8e7ddaf6aa4278a2f9912909eba29d_Out_2_Vector3);
float3 _UPGFoliageBendWSCustomFunction_3d8d6250d27b4f3b96cbc7767f9ae45d_offsetWS_2_Vector3;
UPG_FoliageBendWS_float(IN.WorldSpacePosition, _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float, _UPGFoliageBendWSCustomFunction_3d8d6250d27b4f3b96cbc7767f9ae45d_offsetWS_2_Vector3);
float3 _Add_a2f1ce3eccd04a348677bce71231c28f_Out_2_Vector3;
Unity_Add_float3(_Add_0a8e7ddaf6aa4278a2f9912909eba29d_Out_2_Vector3, _UPGFoliageBendWSCustomFunction_3d8d6250d27b4f3b96cbc7767f9ae45d_offsetWS_2_Vector3, _Add_a2f1ce3eccd04a348677bce71231c28f_Out_2_Vector3);
float3 _Transform_cd30c5b4b2e5405bbe4451c6c589718b_Out_1_Vector3;
_Transform_cd30c5b4b2e5405bbe4451c6c589718b_Out_1_Vector3 = TransformWorldToObject(_Add_a2f1ce3eccd04a348677bce71231c28f_Out_2_Vector3.xyz);
description.Position = _Transform_cd30c5b4b2e5405bbe4451c6c589718b_Out_1_Vector3;
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
UnityTexture2D _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Foliage_Map, sampler_Foliage_Map, _Foliage_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.tex, _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.samplerstate, _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4, _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_R_4_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.r;
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_G_5_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.g;
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_B_6_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.b;
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_A_7_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.a;
float _Property_ce8925e5051d4745a4cf3b2d4fb8db48_Out_0_Float = _Alpha_Clip;
float _Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_A_7_Float, _Property_ce8925e5051d4745a4cf3b2d4fb8db48_Out_0_Float, _Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float);
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_A_4_Float = 0;
float _Property_496eeb327965485cb724cb0c285f7a6c_Out_0_Float = _Dither_Bottom_Level;
float _Add_9a95e02d9c734b69a1bd64099eab897d_Out_2_Float;
Unity_Add_float(_Split_b2c67f57ffb14ffab00206eeffb0e2d5_G_2_Float, _Property_496eeb327965485cb724cb0c285f7a6c_Out_0_Float, _Add_9a95e02d9c734b69a1bd64099eab897d_Out_2_Float);
float _Property_ccb0e250db5745fe928b0ac7bf4d8af7_Out_0_Float = _Dither_Fade;
float _Multiply_38741f482df44a6584b848549f368772_Out_2_Float;
Unity_Multiply_float_float(_Add_9a95e02d9c734b69a1bd64099eab897d_Out_2_Float, _Property_ccb0e250db5745fe928b0ac7bf4d8af7_Out_0_Float, _Multiply_38741f482df44a6584b848549f368772_Out_2_Float);
float _Dither_22e77e4623774a7fa94d00b2ae4ff606_Out_2_Float;
Unity_Dither_float(_Multiply_38741f482df44a6584b848549f368772_Out_2_Float, float4(IN.NDCPosition.xy, 0, 0), _Dither_22e77e4623774a7fa94d00b2ae4ff606_Out_2_Float);
float _Multiply_9214bd931fea4bc98db2b9a46d79c8f8_Out_2_Float;
Unity_Multiply_float_float(_Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float, _Dither_22e77e4623774a7fa94d00b2ae4ff606_Out_2_Float, _Multiply_9214bd931fea4bc98db2b9a46d79c8f8_Out_2_Float);
#if defined(_USE_DITHER)
float _UseDither_4bfe8155f1434a3d9ded012dece1de0e_Out_0_Float = _Multiply_9214bd931fea4bc98db2b9a46d79c8f8_Out_2_Float;
#else
float _UseDither_4bfe8155f1434a3d9ded012dece1de0e_Out_0_Float = _Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float;
#endif
surface.Alpha = _UseDither_4bfe8155f1434a3d9ded012dece1de0e_Out_0_Float;
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
    output.WorldSpaceNormal =                           TransformObjectToWorldNormal(input.normalOS);
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
    output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
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

    





    output.ObjectSpacePosition = TransformWorldToObject(input.positionWS);

    #if UNITY_UV_STARTS_AT_TOP
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #else
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #endif

    output.NDCPosition = output.PixelPosition.xy / _ScreenParams.xy;
    output.NDCPosition.y = 1.0f - output.NDCPosition.y;

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
    result.color      = attributes.color;
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
#pragma shader_feature_local _ _USE_DITHER
#pragma shader_feature_local _ _USE_GRADIENT_MAP
#pragma shader_feature_local _ _USE_VERTEX_COLOR

#if defined(_USE_DITHER) && defined(_USE_GRADIENT_MAP) && defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_DITHER) && defined(_USE_GRADIENT_MAP)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_DITHER) && defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_2
#elif defined(_USE_DITHER)
#define KEYWORD_PERMUTATION_3
#elif defined(_USE_GRADIENT_MAP) && defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_4
#elif defined(_USE_GRADIENT_MAP)
#define KEYWORD_PERMUTATION_5
#elif defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_6
#else
#define KEYWORD_PERMUTATION_7
#endif


// Defines
#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_TEXCOORD1
#define ATTRIBUTES_NEED_TEXCOORD2
#define ATTRIBUTES_NEED_COLOR
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
 float4 color : COLOR;
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
 float3 ObjectSpacePosition;
 float2 NDCPosition;
 float2 PixelPosition;
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 WorldSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 WorldSpacePosition;
 float4 uv0;
 float4 VertexColor;
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

float4 _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D_TexelSize;
float4 _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D_TexelSize;
float _Wind_Small_Scale;
float2 _Wind_Small_Speed;
float _Wind_Small_Intensity;
float _Wind_Large_Scale;
float2 _Wind_Large_Speed;
float4 _Color_Bottom;
float _Wind_Large_Intensity;
float _UV_Pivot_Lock_Strangth;
float _Dither_Bottom_Level;
float _Dither_Fade;
float4 _Foliage_Map_TexelSize;
float _Alpha_Clip;
float4 _Color_Tint;
float4 _Gradient_Map_TexelSize;
float4 _Color_Top;
float _Smoothness;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D);
TEXTURE2D(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D);
TEXTURE2D(_Foliage_Map);
SAMPLER(sampler_Foliage_Map);
TEXTURE2D(_Gradient_Map);
SAMPLER(sampler_Gradient_Map);

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

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

void Unity_Dither_float(float In, float4 ScreenPosition, out float Out)
{
    float2 uv = ScreenPosition.xy * _ScreenParams.xy;
    static const float DITHER_THRESHOLDS[16] =
    {
        1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
        13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
        4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
        16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
    };
    uint index = (uint(uv.x) % 4) * 4 + uint(uv.y) % 4;
    Out = In - DITHER_THRESHOLDS[index];
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
float _Property_ad4aa287fed64295a604d3291d1959a2_Out_0_Float = _Wind_Large_Scale;
float2 _Property_c2f76a94f588455e9b4c07b23d9ef2ff_Out_0_Vector2 = _Wind_Large_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_bad92adcb02c4fc28767432489546115;
_PositionalUVs_bad92adcb02c4fc28767432489546115.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_bad92adcb02c4fc28767432489546115.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_bad92adcb02c4fc28767432489546115_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_ad4aa287fed64295a604d3291d1959a2_Out_0_Float, _Property_c2f76a94f588455e9b4c07b23d9ef2ff_Out_0_Vector2, _PositionalUVs_bad92adcb02c4fc28767432489546115, _PositionalUVs_bad92adcb02c4fc28767432489546115_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_bad92adcb02c4fc28767432489546115_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_R_5_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_G_6_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_B_7_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_A_8_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.a;
float _Subtract_218acd7847974a5cbb60429ba8096400_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_R_5_Float, float(0.5), _Subtract_218acd7847974a5cbb60429ba8096400_Out_2_Float);
float _Split_9060b9716ec549aeacc51b6fd9846e1f_R_1_Float = IN.VertexColor[0];
float _Split_9060b9716ec549aeacc51b6fd9846e1f_G_2_Float = IN.VertexColor[1];
float _Split_9060b9716ec549aeacc51b6fd9846e1f_B_3_Float = IN.VertexColor[2];
float _Split_9060b9716ec549aeacc51b6fd9846e1f_A_4_Float = IN.VertexColor[3];
float4 _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4 = IN.uv0;
float _Split_cd4e6782516b4c4eba58883246f59068_R_1_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[0];
float _Split_cd4e6782516b4c4eba58883246f59068_G_2_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[1];
float _Split_cd4e6782516b4c4eba58883246f59068_B_3_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[2];
float _Split_cd4e6782516b4c4eba58883246f59068_A_4_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[3];
float _Property_8494919c92594e95b9e93cc374ab477d_Out_0_Float = _UV_Pivot_Lock_Strangth;
float _Power_a22a61dbae424f388acc9bbf24d84825_Out_2_Float;
Unity_Power_float(_Split_cd4e6782516b4c4eba58883246f59068_G_2_Float, _Property_8494919c92594e95b9e93cc374ab477d_Out_0_Float, _Power_a22a61dbae424f388acc9bbf24d84825_Out_2_Float);
#if defined(_USE_VERTEX_COLOR)
float _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float = _Split_9060b9716ec549aeacc51b6fd9846e1f_R_1_Float;
#else
float _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float = _Power_a22a61dbae424f388acc9bbf24d84825_Out_2_Float;
#endif
float _Multiply_ea0e0a96337840389ee116bcbf36e668_Out_2_Float;
Unity_Multiply_float_float(_Subtract_218acd7847974a5cbb60429ba8096400_Out_2_Float, _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float, _Multiply_ea0e0a96337840389ee116bcbf36e668_Out_2_Float);
float _Property_dbbb11cdd2654ae79939bef8b5ecd875_Out_0_Float = _Wind_Large_Intensity;
float _Multiply_6869889d47d046e8b5ad71f9c3a9adb4_Out_2_Float;
Unity_Multiply_float_float(_Multiply_ea0e0a96337840389ee116bcbf36e668_Out_2_Float, _Property_dbbb11cdd2654ae79939bef8b5ecd875_Out_0_Float, _Multiply_6869889d47d046e8b5ad71f9c3a9adb4_Out_2_Float);
float _Property_c327c41c045b40c483eb7da471e0c3fd_Out_0_Float = _Wind_Small_Scale;
float2 _Property_92349f3d399742e5b93d3ec2912b6d0b_Out_0_Vector2 = _Wind_Small_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_61edec0a11644d95b38796bd3e28715a;
_PositionalUVs_61edec0a11644d95b38796bd3e28715a.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_61edec0a11644d95b38796bd3e28715a.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_61edec0a11644d95b38796bd3e28715a_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_c327c41c045b40c483eb7da471e0c3fd_Out_0_Float, _Property_92349f3d399742e5b93d3ec2912b6d0b_Out_0_Vector2, _PositionalUVs_61edec0a11644d95b38796bd3e28715a, _PositionalUVs_61edec0a11644d95b38796bd3e28715a_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_61edec0a11644d95b38796bd3e28715a_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_R_5_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_G_6_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_B_7_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_A_8_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.a;
float _Subtract_38999ff8e5a04271b3794daa96fc9cca_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_R_5_Float, float(0.5), _Subtract_38999ff8e5a04271b3794daa96fc9cca_Out_2_Float);
float _Multiply_4209b0f1a88b424992ddafb6610c2968_Out_2_Float;
Unity_Multiply_float_float(_UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float, _Subtract_38999ff8e5a04271b3794daa96fc9cca_Out_2_Float, _Multiply_4209b0f1a88b424992ddafb6610c2968_Out_2_Float);
float _Property_2bd20e0e45834464a28f6379b9e8225c_Out_0_Float = _Wind_Small_Intensity;
float _Multiply_1c0b4b06455e4d31b5ad6f8c28d20004_Out_2_Float;
Unity_Multiply_float_float(_Multiply_4209b0f1a88b424992ddafb6610c2968_Out_2_Float, _Property_2bd20e0e45834464a28f6379b9e8225c_Out_0_Float, _Multiply_1c0b4b06455e4d31b5ad6f8c28d20004_Out_2_Float);
float _Add_0407a76ed876497f9c558311ba6b5eb8_Out_2_Float;
Unity_Add_float(_Multiply_6869889d47d046e8b5ad71f9c3a9adb4_Out_2_Float, _Multiply_1c0b4b06455e4d31b5ad6f8c28d20004_Out_2_Float, _Add_0407a76ed876497f9c558311ba6b5eb8_Out_2_Float);
float3 _Multiply_5d424720302c4e55afef4b8a9f76d3ef_Out_2_Vector3;
Unity_Multiply_float3_float3((_Add_0407a76ed876497f9c558311ba6b5eb8_Out_2_Float.xxx), IN.WorldSpaceNormal, _Multiply_5d424720302c4e55afef4b8a9f76d3ef_Out_2_Vector3);
float3 _Add_0a8e7ddaf6aa4278a2f9912909eba29d_Out_2_Vector3;
Unity_Add_float3(_Multiply_5d424720302c4e55afef4b8a9f76d3ef_Out_2_Vector3, IN.WorldSpacePosition, _Add_0a8e7ddaf6aa4278a2f9912909eba29d_Out_2_Vector3);
float3 _UPGFoliageBendWSCustomFunction_3d8d6250d27b4f3b96cbc7767f9ae45d_offsetWS_2_Vector3;
UPG_FoliageBendWS_float(IN.WorldSpacePosition, _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float, _UPGFoliageBendWSCustomFunction_3d8d6250d27b4f3b96cbc7767f9ae45d_offsetWS_2_Vector3);
float3 _Add_a2f1ce3eccd04a348677bce71231c28f_Out_2_Vector3;
Unity_Add_float3(_Add_0a8e7ddaf6aa4278a2f9912909eba29d_Out_2_Vector3, _UPGFoliageBendWSCustomFunction_3d8d6250d27b4f3b96cbc7767f9ae45d_offsetWS_2_Vector3, _Add_a2f1ce3eccd04a348677bce71231c28f_Out_2_Vector3);
float3 _Transform_cd30c5b4b2e5405bbe4451c6c589718b_Out_1_Vector3;
_Transform_cd30c5b4b2e5405bbe4451c6c589718b_Out_1_Vector3 = TransformWorldToObject(_Add_a2f1ce3eccd04a348677bce71231c28f_Out_2_Vector3.xyz);
description.Position = _Transform_cd30c5b4b2e5405bbe4451c6c589718b_Out_1_Vector3;
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
float4 _Property_597353718570472cb903eabbac10e98e_Out_0_Vector4 = _Color_Bottom;
float4 _Property_acf5ade3ddc74df2bb7b72f3fb5027e6_Out_0_Vector4 = _Color_Top;
UnityTexture2D _Property_0dd8d95b31054753b15cbea49c4dd049_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Gradient_Map, sampler_Gradient_Map, _Gradient_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_0dd8d95b31054753b15cbea49c4dd049_Out_0_Texture2D.tex, _Property_0dd8d95b31054753b15cbea49c4dd049_Out_0_Texture2D.samplerstate, _Property_0dd8d95b31054753b15cbea49c4dd049_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_0dd8d95b31054753b15cbea49c4dd049_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4, _Property_0dd8d95b31054753b15cbea49c4dd049_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_R_4_Float = _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4.r;
float _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_G_5_Float = _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4.g;
float _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_B_6_Float = _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4.b;
float _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_A_7_Float = _SampleTexture2D_0139bf195dc74dea98a5142201287ca0_RGBA_0_Vector4.a;
float4 _Lerp_45e933526a9e47429463b3d3ae7bcb99_Out_3_Vector4;
Unity_Lerp_float4(_Property_597353718570472cb903eabbac10e98e_Out_0_Vector4, _Property_acf5ade3ddc74df2bb7b72f3fb5027e6_Out_0_Vector4, (_SampleTexture2D_0139bf195dc74dea98a5142201287ca0_R_4_Float.xxxx), _Lerp_45e933526a9e47429463b3d3ae7bcb99_Out_3_Vector4);
float4 _Property_d51fad0917634f61855f006ac3a3036a_Out_0_Vector4 = _Color_Tint;
#if defined(_USE_GRADIENT_MAP)
float4 _UseGradientMap_ab913e91a75042bfa88ab1093a041f33_Out_0_Vector4 = _Lerp_45e933526a9e47429463b3d3ae7bcb99_Out_3_Vector4;
#else
float4 _UseGradientMap_ab913e91a75042bfa88ab1093a041f33_Out_0_Vector4 = _Property_d51fad0917634f61855f006ac3a3036a_Out_0_Vector4;
#endif
UnityTexture2D _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Foliage_Map, sampler_Foliage_Map, _Foliage_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.tex, _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.samplerstate, _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4, _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_R_4_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.r;
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_G_5_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.g;
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_B_6_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.b;
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_A_7_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.a;
float4 _Multiply_2d1489dbc4f84047b8a1330c23f185d7_Out_2_Vector4;
Unity_Multiply_float4_float4(_UseGradientMap_ab913e91a75042bfa88ab1093a041f33_Out_0_Vector4, _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4, _Multiply_2d1489dbc4f84047b8a1330c23f185d7_Out_2_Vector4);
float _Property_ce8925e5051d4745a4cf3b2d4fb8db48_Out_0_Float = _Alpha_Clip;
float _Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_A_7_Float, _Property_ce8925e5051d4745a4cf3b2d4fb8db48_Out_0_Float, _Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float);
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_A_4_Float = 0;
float _Property_496eeb327965485cb724cb0c285f7a6c_Out_0_Float = _Dither_Bottom_Level;
float _Add_9a95e02d9c734b69a1bd64099eab897d_Out_2_Float;
Unity_Add_float(_Split_b2c67f57ffb14ffab00206eeffb0e2d5_G_2_Float, _Property_496eeb327965485cb724cb0c285f7a6c_Out_0_Float, _Add_9a95e02d9c734b69a1bd64099eab897d_Out_2_Float);
float _Property_ccb0e250db5745fe928b0ac7bf4d8af7_Out_0_Float = _Dither_Fade;
float _Multiply_38741f482df44a6584b848549f368772_Out_2_Float;
Unity_Multiply_float_float(_Add_9a95e02d9c734b69a1bd64099eab897d_Out_2_Float, _Property_ccb0e250db5745fe928b0ac7bf4d8af7_Out_0_Float, _Multiply_38741f482df44a6584b848549f368772_Out_2_Float);
float _Dither_22e77e4623774a7fa94d00b2ae4ff606_Out_2_Float;
Unity_Dither_float(_Multiply_38741f482df44a6584b848549f368772_Out_2_Float, float4(IN.NDCPosition.xy, 0, 0), _Dither_22e77e4623774a7fa94d00b2ae4ff606_Out_2_Float);
float _Multiply_9214bd931fea4bc98db2b9a46d79c8f8_Out_2_Float;
Unity_Multiply_float_float(_Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float, _Dither_22e77e4623774a7fa94d00b2ae4ff606_Out_2_Float, _Multiply_9214bd931fea4bc98db2b9a46d79c8f8_Out_2_Float);
#if defined(_USE_DITHER)
float _UseDither_4bfe8155f1434a3d9ded012dece1de0e_Out_0_Float = _Multiply_9214bd931fea4bc98db2b9a46d79c8f8_Out_2_Float;
#else
float _UseDither_4bfe8155f1434a3d9ded012dece1de0e_Out_0_Float = _Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float;
#endif
surface.BaseColor = (_Multiply_2d1489dbc4f84047b8a1330c23f185d7_Out_2_Vector4.xyz);
surface.Emission = float3(0, 0, 0);
surface.Alpha = _UseDither_4bfe8155f1434a3d9ded012dece1de0e_Out_0_Float;
surface.AlphaClipThreshold = float(0.5);
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
    output.WorldSpaceNormal =                           TransformObjectToWorldNormal(input.normalOS);
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
    output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
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

    





    output.ObjectSpacePosition = TransformWorldToObject(input.positionWS);

    #if UNITY_UV_STARTS_AT_TOP
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #else
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #endif

    output.NDCPosition = output.PixelPosition.xy / _ScreenParams.xy;
    output.NDCPosition.y = 1.0f - output.NDCPosition.y;

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
    result.color      = attributes.color;
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
#pragma shader_feature_local _ _USE_DITHER
#pragma shader_feature_local _ _USE_GRADIENT_MAP
#pragma shader_feature_local _ _USE_VERTEX_COLOR

#if defined(_USE_DITHER) && defined(_USE_GRADIENT_MAP) && defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_DITHER) && defined(_USE_GRADIENT_MAP)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_DITHER) && defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_2
#elif defined(_USE_DITHER)
#define KEYWORD_PERMUTATION_3
#elif defined(_USE_GRADIENT_MAP) && defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_4
#elif defined(_USE_GRADIENT_MAP)
#define KEYWORD_PERMUTATION_5
#elif defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_6
#else
#define KEYWORD_PERMUTATION_7
#endif


// Defines
#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_COLOR
#define VARYINGS_NEED_POSITION_WS
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
 float4 color : COLOR;
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
 float3 ObjectSpacePosition;
 float2 NDCPosition;
 float2 PixelPosition;
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 WorldSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 WorldSpacePosition;
 float4 uv0;
 float4 VertexColor;
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

float4 _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D_TexelSize;
float4 _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D_TexelSize;
float _Wind_Small_Scale;
float2 _Wind_Small_Speed;
float _Wind_Small_Intensity;
float _Wind_Large_Scale;
float2 _Wind_Large_Speed;
float4 _Color_Bottom;
float _Wind_Large_Intensity;
float _UV_Pivot_Lock_Strangth;
float _Dither_Bottom_Level;
float _Dither_Fade;
float4 _Foliage_Map_TexelSize;
float _Alpha_Clip;
float4 _Color_Tint;
float4 _Gradient_Map_TexelSize;
float4 _Color_Top;
float _Smoothness;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D);
TEXTURE2D(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D);
TEXTURE2D(_Foliage_Map);
SAMPLER(sampler_Foliage_Map);
TEXTURE2D(_Gradient_Map);
SAMPLER(sampler_Gradient_Map);

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

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Dither_float(float In, float4 ScreenPosition, out float Out)
{
    float2 uv = ScreenPosition.xy * _ScreenParams.xy;
    static const float DITHER_THRESHOLDS[16] =
    {
        1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
        13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
        4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
        16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
    };
    uint index = (uint(uv.x) % 4) * 4 + uint(uv.y) % 4;
    Out = In - DITHER_THRESHOLDS[index];
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
float _Property_ad4aa287fed64295a604d3291d1959a2_Out_0_Float = _Wind_Large_Scale;
float2 _Property_c2f76a94f588455e9b4c07b23d9ef2ff_Out_0_Vector2 = _Wind_Large_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_bad92adcb02c4fc28767432489546115;
_PositionalUVs_bad92adcb02c4fc28767432489546115.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_bad92adcb02c4fc28767432489546115.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_bad92adcb02c4fc28767432489546115_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_ad4aa287fed64295a604d3291d1959a2_Out_0_Float, _Property_c2f76a94f588455e9b4c07b23d9ef2ff_Out_0_Vector2, _PositionalUVs_bad92adcb02c4fc28767432489546115, _PositionalUVs_bad92adcb02c4fc28767432489546115_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_bad92adcb02c4fc28767432489546115_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_R_5_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_G_6_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_B_7_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_A_8_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.a;
float _Subtract_218acd7847974a5cbb60429ba8096400_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_R_5_Float, float(0.5), _Subtract_218acd7847974a5cbb60429ba8096400_Out_2_Float);
float _Split_9060b9716ec549aeacc51b6fd9846e1f_R_1_Float = IN.VertexColor[0];
float _Split_9060b9716ec549aeacc51b6fd9846e1f_G_2_Float = IN.VertexColor[1];
float _Split_9060b9716ec549aeacc51b6fd9846e1f_B_3_Float = IN.VertexColor[2];
float _Split_9060b9716ec549aeacc51b6fd9846e1f_A_4_Float = IN.VertexColor[3];
float4 _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4 = IN.uv0;
float _Split_cd4e6782516b4c4eba58883246f59068_R_1_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[0];
float _Split_cd4e6782516b4c4eba58883246f59068_G_2_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[1];
float _Split_cd4e6782516b4c4eba58883246f59068_B_3_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[2];
float _Split_cd4e6782516b4c4eba58883246f59068_A_4_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[3];
float _Property_8494919c92594e95b9e93cc374ab477d_Out_0_Float = _UV_Pivot_Lock_Strangth;
float _Power_a22a61dbae424f388acc9bbf24d84825_Out_2_Float;
Unity_Power_float(_Split_cd4e6782516b4c4eba58883246f59068_G_2_Float, _Property_8494919c92594e95b9e93cc374ab477d_Out_0_Float, _Power_a22a61dbae424f388acc9bbf24d84825_Out_2_Float);
#if defined(_USE_VERTEX_COLOR)
float _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float = _Split_9060b9716ec549aeacc51b6fd9846e1f_R_1_Float;
#else
float _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float = _Power_a22a61dbae424f388acc9bbf24d84825_Out_2_Float;
#endif
float _Multiply_ea0e0a96337840389ee116bcbf36e668_Out_2_Float;
Unity_Multiply_float_float(_Subtract_218acd7847974a5cbb60429ba8096400_Out_2_Float, _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float, _Multiply_ea0e0a96337840389ee116bcbf36e668_Out_2_Float);
float _Property_dbbb11cdd2654ae79939bef8b5ecd875_Out_0_Float = _Wind_Large_Intensity;
float _Multiply_6869889d47d046e8b5ad71f9c3a9adb4_Out_2_Float;
Unity_Multiply_float_float(_Multiply_ea0e0a96337840389ee116bcbf36e668_Out_2_Float, _Property_dbbb11cdd2654ae79939bef8b5ecd875_Out_0_Float, _Multiply_6869889d47d046e8b5ad71f9c3a9adb4_Out_2_Float);
float _Property_c327c41c045b40c483eb7da471e0c3fd_Out_0_Float = _Wind_Small_Scale;
float2 _Property_92349f3d399742e5b93d3ec2912b6d0b_Out_0_Vector2 = _Wind_Small_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_61edec0a11644d95b38796bd3e28715a;
_PositionalUVs_61edec0a11644d95b38796bd3e28715a.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_61edec0a11644d95b38796bd3e28715a.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_61edec0a11644d95b38796bd3e28715a_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_c327c41c045b40c483eb7da471e0c3fd_Out_0_Float, _Property_92349f3d399742e5b93d3ec2912b6d0b_Out_0_Vector2, _PositionalUVs_61edec0a11644d95b38796bd3e28715a, _PositionalUVs_61edec0a11644d95b38796bd3e28715a_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_61edec0a11644d95b38796bd3e28715a_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_R_5_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_G_6_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_B_7_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_A_8_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.a;
float _Subtract_38999ff8e5a04271b3794daa96fc9cca_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_R_5_Float, float(0.5), _Subtract_38999ff8e5a04271b3794daa96fc9cca_Out_2_Float);
float _Multiply_4209b0f1a88b424992ddafb6610c2968_Out_2_Float;
Unity_Multiply_float_float(_UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float, _Subtract_38999ff8e5a04271b3794daa96fc9cca_Out_2_Float, _Multiply_4209b0f1a88b424992ddafb6610c2968_Out_2_Float);
float _Property_2bd20e0e45834464a28f6379b9e8225c_Out_0_Float = _Wind_Small_Intensity;
float _Multiply_1c0b4b06455e4d31b5ad6f8c28d20004_Out_2_Float;
Unity_Multiply_float_float(_Multiply_4209b0f1a88b424992ddafb6610c2968_Out_2_Float, _Property_2bd20e0e45834464a28f6379b9e8225c_Out_0_Float, _Multiply_1c0b4b06455e4d31b5ad6f8c28d20004_Out_2_Float);
float _Add_0407a76ed876497f9c558311ba6b5eb8_Out_2_Float;
Unity_Add_float(_Multiply_6869889d47d046e8b5ad71f9c3a9adb4_Out_2_Float, _Multiply_1c0b4b06455e4d31b5ad6f8c28d20004_Out_2_Float, _Add_0407a76ed876497f9c558311ba6b5eb8_Out_2_Float);
float3 _Multiply_5d424720302c4e55afef4b8a9f76d3ef_Out_2_Vector3;
Unity_Multiply_float3_float3((_Add_0407a76ed876497f9c558311ba6b5eb8_Out_2_Float.xxx), IN.WorldSpaceNormal, _Multiply_5d424720302c4e55afef4b8a9f76d3ef_Out_2_Vector3);
float3 _Add_0a8e7ddaf6aa4278a2f9912909eba29d_Out_2_Vector3;
Unity_Add_float3(_Multiply_5d424720302c4e55afef4b8a9f76d3ef_Out_2_Vector3, IN.WorldSpacePosition, _Add_0a8e7ddaf6aa4278a2f9912909eba29d_Out_2_Vector3);
float3 _UPGFoliageBendWSCustomFunction_3d8d6250d27b4f3b96cbc7767f9ae45d_offsetWS_2_Vector3;
UPG_FoliageBendWS_float(IN.WorldSpacePosition, _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float, _UPGFoliageBendWSCustomFunction_3d8d6250d27b4f3b96cbc7767f9ae45d_offsetWS_2_Vector3);
float3 _Add_a2f1ce3eccd04a348677bce71231c28f_Out_2_Vector3;
Unity_Add_float3(_Add_0a8e7ddaf6aa4278a2f9912909eba29d_Out_2_Vector3, _UPGFoliageBendWSCustomFunction_3d8d6250d27b4f3b96cbc7767f9ae45d_offsetWS_2_Vector3, _Add_a2f1ce3eccd04a348677bce71231c28f_Out_2_Vector3);
float3 _Transform_cd30c5b4b2e5405bbe4451c6c589718b_Out_1_Vector3;
_Transform_cd30c5b4b2e5405bbe4451c6c589718b_Out_1_Vector3 = TransformWorldToObject(_Add_a2f1ce3eccd04a348677bce71231c28f_Out_2_Vector3.xyz);
description.Position = _Transform_cd30c5b4b2e5405bbe4451c6c589718b_Out_1_Vector3;
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
UnityTexture2D _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Foliage_Map, sampler_Foliage_Map, _Foliage_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.tex, _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.samplerstate, _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4, _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_R_4_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.r;
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_G_5_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.g;
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_B_6_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.b;
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_A_7_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.a;
float _Property_ce8925e5051d4745a4cf3b2d4fb8db48_Out_0_Float = _Alpha_Clip;
float _Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_A_7_Float, _Property_ce8925e5051d4745a4cf3b2d4fb8db48_Out_0_Float, _Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float);
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_A_4_Float = 0;
float _Property_496eeb327965485cb724cb0c285f7a6c_Out_0_Float = _Dither_Bottom_Level;
float _Add_9a95e02d9c734b69a1bd64099eab897d_Out_2_Float;
Unity_Add_float(_Split_b2c67f57ffb14ffab00206eeffb0e2d5_G_2_Float, _Property_496eeb327965485cb724cb0c285f7a6c_Out_0_Float, _Add_9a95e02d9c734b69a1bd64099eab897d_Out_2_Float);
float _Property_ccb0e250db5745fe928b0ac7bf4d8af7_Out_0_Float = _Dither_Fade;
float _Multiply_38741f482df44a6584b848549f368772_Out_2_Float;
Unity_Multiply_float_float(_Add_9a95e02d9c734b69a1bd64099eab897d_Out_2_Float, _Property_ccb0e250db5745fe928b0ac7bf4d8af7_Out_0_Float, _Multiply_38741f482df44a6584b848549f368772_Out_2_Float);
float _Dither_22e77e4623774a7fa94d00b2ae4ff606_Out_2_Float;
Unity_Dither_float(_Multiply_38741f482df44a6584b848549f368772_Out_2_Float, float4(IN.NDCPosition.xy, 0, 0), _Dither_22e77e4623774a7fa94d00b2ae4ff606_Out_2_Float);
float _Multiply_9214bd931fea4bc98db2b9a46d79c8f8_Out_2_Float;
Unity_Multiply_float_float(_Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float, _Dither_22e77e4623774a7fa94d00b2ae4ff606_Out_2_Float, _Multiply_9214bd931fea4bc98db2b9a46d79c8f8_Out_2_Float);
#if defined(_USE_DITHER)
float _UseDither_4bfe8155f1434a3d9ded012dece1de0e_Out_0_Float = _Multiply_9214bd931fea4bc98db2b9a46d79c8f8_Out_2_Float;
#else
float _UseDither_4bfe8155f1434a3d9ded012dece1de0e_Out_0_Float = _Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float;
#endif
surface.Alpha = _UseDither_4bfe8155f1434a3d9ded012dece1de0e_Out_0_Float;
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
    output.WorldSpaceNormal =                           TransformObjectToWorldNormal(input.normalOS);
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
    output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
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

    





    output.ObjectSpacePosition = TransformWorldToObject(input.positionWS);

    #if UNITY_UV_STARTS_AT_TOP
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #else
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #endif

    output.NDCPosition = output.PixelPosition.xy / _ScreenParams.xy;
    output.NDCPosition.y = 1.0f - output.NDCPosition.y;

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
    result.color      = attributes.color;
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
#pragma shader_feature_local _ _USE_DITHER
#pragma shader_feature_local _ _USE_GRADIENT_MAP
#pragma shader_feature_local _ _USE_VERTEX_COLOR

#if defined(_USE_DITHER) && defined(_USE_GRADIENT_MAP) && defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_DITHER) && defined(_USE_GRADIENT_MAP)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_DITHER) && defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_2
#elif defined(_USE_DITHER)
#define KEYWORD_PERMUTATION_3
#elif defined(_USE_GRADIENT_MAP) && defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_4
#elif defined(_USE_GRADIENT_MAP)
#define KEYWORD_PERMUTATION_5
#elif defined(_USE_VERTEX_COLOR)
#define KEYWORD_PERMUTATION_6
#else
#define KEYWORD_PERMUTATION_7
#endif


// Defines
#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_COLOR
#define VARYINGS_NEED_POSITION_WS
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
 float4 color : COLOR;
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
 float3 ObjectSpacePosition;
 float2 NDCPosition;
 float2 PixelPosition;
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 WorldSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 WorldSpacePosition;
 float4 uv0;
 float4 VertexColor;
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

float4 _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D_TexelSize;
float4 _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D_TexelSize;
float _Wind_Small_Scale;
float2 _Wind_Small_Speed;
float _Wind_Small_Intensity;
float _Wind_Large_Scale;
float2 _Wind_Large_Speed;
float4 _Color_Bottom;
float _Wind_Large_Intensity;
float _UV_Pivot_Lock_Strangth;
float _Dither_Bottom_Level;
float _Dither_Fade;
float4 _Foliage_Map_TexelSize;
float _Alpha_Clip;
float4 _Color_Tint;
float4 _Gradient_Map_TexelSize;
float4 _Color_Top;
float _Smoothness;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D);
TEXTURE2D(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D);
SAMPLER(sampler_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D);
TEXTURE2D(_Foliage_Map);
SAMPLER(sampler_Foliage_Map);
TEXTURE2D(_Gradient_Map);
SAMPLER(sampler_Gradient_Map);

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

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Dither_float(float In, float4 ScreenPosition, out float Out)
{
    float2 uv = ScreenPosition.xy * _ScreenParams.xy;
    static const float DITHER_THRESHOLDS[16] =
    {
        1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
        13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
        4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
        16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
    };
    uint index = (uint(uv.x) % 4) * 4 + uint(uv.y) % 4;
    Out = In - DITHER_THRESHOLDS[index];
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
float _Property_ad4aa287fed64295a604d3291d1959a2_Out_0_Float = _Wind_Large_Scale;
float2 _Property_c2f76a94f588455e9b4c07b23d9ef2ff_Out_0_Vector2 = _Wind_Large_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_bad92adcb02c4fc28767432489546115;
_PositionalUVs_bad92adcb02c4fc28767432489546115.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_bad92adcb02c4fc28767432489546115.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_bad92adcb02c4fc28767432489546115_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_ad4aa287fed64295a604d3291d1959a2_Out_0_Float, _Property_c2f76a94f588455e9b4c07b23d9ef2ff_Out_0_Vector2, _PositionalUVs_bad92adcb02c4fc28767432489546115, _PositionalUVs_bad92adcb02c4fc28767432489546115_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_bad92adcb02c4fc28767432489546115_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_R_5_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_G_6_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_B_7_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_A_8_Float = _SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_RGBA_0_Vector4.a;
float _Subtract_218acd7847974a5cbb60429ba8096400_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_R_5_Float, float(0.5), _Subtract_218acd7847974a5cbb60429ba8096400_Out_2_Float);
float _Split_9060b9716ec549aeacc51b6fd9846e1f_R_1_Float = IN.VertexColor[0];
float _Split_9060b9716ec549aeacc51b6fd9846e1f_G_2_Float = IN.VertexColor[1];
float _Split_9060b9716ec549aeacc51b6fd9846e1f_B_3_Float = IN.VertexColor[2];
float _Split_9060b9716ec549aeacc51b6fd9846e1f_A_4_Float = IN.VertexColor[3];
float4 _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4 = IN.uv0;
float _Split_cd4e6782516b4c4eba58883246f59068_R_1_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[0];
float _Split_cd4e6782516b4c4eba58883246f59068_G_2_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[1];
float _Split_cd4e6782516b4c4eba58883246f59068_B_3_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[2];
float _Split_cd4e6782516b4c4eba58883246f59068_A_4_Float = _UV_91bf920d63c646bbba3c36a0a2f0d41e_Out_0_Vector4[3];
float _Property_8494919c92594e95b9e93cc374ab477d_Out_0_Float = _UV_Pivot_Lock_Strangth;
float _Power_a22a61dbae424f388acc9bbf24d84825_Out_2_Float;
Unity_Power_float(_Split_cd4e6782516b4c4eba58883246f59068_G_2_Float, _Property_8494919c92594e95b9e93cc374ab477d_Out_0_Float, _Power_a22a61dbae424f388acc9bbf24d84825_Out_2_Float);
#if defined(_USE_VERTEX_COLOR)
float _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float = _Split_9060b9716ec549aeacc51b6fd9846e1f_R_1_Float;
#else
float _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float = _Power_a22a61dbae424f388acc9bbf24d84825_Out_2_Float;
#endif
float _Multiply_ea0e0a96337840389ee116bcbf36e668_Out_2_Float;
Unity_Multiply_float_float(_Subtract_218acd7847974a5cbb60429ba8096400_Out_2_Float, _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float, _Multiply_ea0e0a96337840389ee116bcbf36e668_Out_2_Float);
float _Property_dbbb11cdd2654ae79939bef8b5ecd875_Out_0_Float = _Wind_Large_Intensity;
float _Multiply_6869889d47d046e8b5ad71f9c3a9adb4_Out_2_Float;
Unity_Multiply_float_float(_Multiply_ea0e0a96337840389ee116bcbf36e668_Out_2_Float, _Property_dbbb11cdd2654ae79939bef8b5ecd875_Out_0_Float, _Multiply_6869889d47d046e8b5ad71f9c3a9adb4_Out_2_Float);
float _Property_c327c41c045b40c483eb7da471e0c3fd_Out_0_Float = _Wind_Small_Scale;
float2 _Property_92349f3d399742e5b93d3ec2912b6d0b_Out_0_Vector2 = _Wind_Small_Speed;
Bindings_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float _PositionalUVs_61edec0a11644d95b38796bd3e28715a;
_PositionalUVs_61edec0a11644d95b38796bd3e28715a.WorldSpacePosition = IN.WorldSpacePosition;
_PositionalUVs_61edec0a11644d95b38796bd3e28715a.TimeParameters = IN.TimeParameters;
float2 _PositionalUVs_61edec0a11644d95b38796bd3e28715a_OutVector2_1_Vector2;
SG_PositionalUVs_0f74d9239760ce74e81e30c0ddc0cdb6_float(_Property_c327c41c045b40c483eb7da471e0c3fd_Out_0_Float, _Property_92349f3d399742e5b93d3ec2912b6d0b_Out_0_Vector2, _PositionalUVs_61edec0a11644d95b38796bd3e28715a, _PositionalUVs_61edec0a11644d95b38796bd3e28715a_OutVector2_1_Vector2);
  float4 _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_Texture_1_Texture2D).GetTransformedUV(_PositionalUVs_61edec0a11644d95b38796bd3e28715a_OutVector2_1_Vector2), float(0));
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_R_5_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_G_6_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_B_7_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_A_8_Float = _SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_RGBA_0_Vector4.a;
float _Subtract_38999ff8e5a04271b3794daa96fc9cca_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_f4fa5a54ad7c4fa2b81c271b138a1420_R_5_Float, float(0.5), _Subtract_38999ff8e5a04271b3794daa96fc9cca_Out_2_Float);
float _Multiply_4209b0f1a88b424992ddafb6610c2968_Out_2_Float;
Unity_Multiply_float_float(_UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float, _Subtract_38999ff8e5a04271b3794daa96fc9cca_Out_2_Float, _Multiply_4209b0f1a88b424992ddafb6610c2968_Out_2_Float);
float _Property_2bd20e0e45834464a28f6379b9e8225c_Out_0_Float = _Wind_Small_Intensity;
float _Multiply_1c0b4b06455e4d31b5ad6f8c28d20004_Out_2_Float;
Unity_Multiply_float_float(_Multiply_4209b0f1a88b424992ddafb6610c2968_Out_2_Float, _Property_2bd20e0e45834464a28f6379b9e8225c_Out_0_Float, _Multiply_1c0b4b06455e4d31b5ad6f8c28d20004_Out_2_Float);
float _Add_0407a76ed876497f9c558311ba6b5eb8_Out_2_Float;
Unity_Add_float(_Multiply_6869889d47d046e8b5ad71f9c3a9adb4_Out_2_Float, _Multiply_1c0b4b06455e4d31b5ad6f8c28d20004_Out_2_Float, _Add_0407a76ed876497f9c558311ba6b5eb8_Out_2_Float);
float3 _Multiply_5d424720302c4e55afef4b8a9f76d3ef_Out_2_Vector3;
Unity_Multiply_float3_float3((_Add_0407a76ed876497f9c558311ba6b5eb8_Out_2_Float.xxx), IN.WorldSpaceNormal, _Multiply_5d424720302c4e55afef4b8a9f76d3ef_Out_2_Vector3);
float3 _Add_0a8e7ddaf6aa4278a2f9912909eba29d_Out_2_Vector3;
Unity_Add_float3(_Multiply_5d424720302c4e55afef4b8a9f76d3ef_Out_2_Vector3, IN.WorldSpacePosition, _Add_0a8e7ddaf6aa4278a2f9912909eba29d_Out_2_Vector3);
float3 _UPGFoliageBendWSCustomFunction_3d8d6250d27b4f3b96cbc7767f9ae45d_offsetWS_2_Vector3;
UPG_FoliageBendWS_float(IN.WorldSpacePosition, _UseVertexColor_8997aab7f38344f19459a94e42d46de0_Out_0_Float, _UPGFoliageBendWSCustomFunction_3d8d6250d27b4f3b96cbc7767f9ae45d_offsetWS_2_Vector3);
float3 _Add_a2f1ce3eccd04a348677bce71231c28f_Out_2_Vector3;
Unity_Add_float3(_Add_0a8e7ddaf6aa4278a2f9912909eba29d_Out_2_Vector3, _UPGFoliageBendWSCustomFunction_3d8d6250d27b4f3b96cbc7767f9ae45d_offsetWS_2_Vector3, _Add_a2f1ce3eccd04a348677bce71231c28f_Out_2_Vector3);
float3 _Transform_cd30c5b4b2e5405bbe4451c6c589718b_Out_1_Vector3;
_Transform_cd30c5b4b2e5405bbe4451c6c589718b_Out_1_Vector3 = TransformWorldToObject(_Add_a2f1ce3eccd04a348677bce71231c28f_Out_2_Vector3.xyz);
description.Position = _Transform_cd30c5b4b2e5405bbe4451c6c589718b_Out_1_Vector3;
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
UnityTexture2D _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Foliage_Map, sampler_Foliage_Map, _Foliage_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.tex, _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.samplerstate, _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4, _Property_1f0091734c17406598c8e8ea88328bd7_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_R_4_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.r;
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_G_5_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.g;
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_B_6_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.b;
float _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_A_7_Float = _SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_RGBA_0_Vector4.a;
float _Property_ce8925e5051d4745a4cf3b2d4fb8db48_Out_0_Float = _Alpha_Clip;
float _Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_dc9111d7ac364ad5a009478842dd31b2_A_7_Float, _Property_ce8925e5051d4745a4cf3b2d4fb8db48_Out_0_Float, _Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float);
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_b2c67f57ffb14ffab00206eeffb0e2d5_A_4_Float = 0;
float _Property_496eeb327965485cb724cb0c285f7a6c_Out_0_Float = _Dither_Bottom_Level;
float _Add_9a95e02d9c734b69a1bd64099eab897d_Out_2_Float;
Unity_Add_float(_Split_b2c67f57ffb14ffab00206eeffb0e2d5_G_2_Float, _Property_496eeb327965485cb724cb0c285f7a6c_Out_0_Float, _Add_9a95e02d9c734b69a1bd64099eab897d_Out_2_Float);
float _Property_ccb0e250db5745fe928b0ac7bf4d8af7_Out_0_Float = _Dither_Fade;
float _Multiply_38741f482df44a6584b848549f368772_Out_2_Float;
Unity_Multiply_float_float(_Add_9a95e02d9c734b69a1bd64099eab897d_Out_2_Float, _Property_ccb0e250db5745fe928b0ac7bf4d8af7_Out_0_Float, _Multiply_38741f482df44a6584b848549f368772_Out_2_Float);
float _Dither_22e77e4623774a7fa94d00b2ae4ff606_Out_2_Float;
Unity_Dither_float(_Multiply_38741f482df44a6584b848549f368772_Out_2_Float, float4(IN.NDCPosition.xy, 0, 0), _Dither_22e77e4623774a7fa94d00b2ae4ff606_Out_2_Float);
float _Multiply_9214bd931fea4bc98db2b9a46d79c8f8_Out_2_Float;
Unity_Multiply_float_float(_Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float, _Dither_22e77e4623774a7fa94d00b2ae4ff606_Out_2_Float, _Multiply_9214bd931fea4bc98db2b9a46d79c8f8_Out_2_Float);
#if defined(_USE_DITHER)
float _UseDither_4bfe8155f1434a3d9ded012dece1de0e_Out_0_Float = _Multiply_9214bd931fea4bc98db2b9a46d79c8f8_Out_2_Float;
#else
float _UseDither_4bfe8155f1434a3d9ded012dece1de0e_Out_0_Float = _Multiply_18b32e8f246b4071964bd714766c8001_Out_2_Float;
#endif
surface.Alpha = _UseDither_4bfe8155f1434a3d9ded012dece1de0e_Out_0_Float;
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
    output.WorldSpaceNormal =                           TransformObjectToWorldNormal(input.normalOS);
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
    output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
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

    





    output.ObjectSpacePosition = TransformWorldToObject(input.positionWS);

    #if UNITY_UV_STARTS_AT_TOP
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #else
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #endif

    output.NDCPosition = output.PixelPosition.xy / _ScreenParams.xy;
    output.NDCPosition.y = 1.0f - output.NDCPosition.y;

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
    result.color      = attributes.color;
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
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/DepthOnlyPass.hlsl"

ENDHLSL
}
}
CustomEditor "UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI"
CustomEditorForRenderPipeline "UnityEditor.Rendering.BuiltIn.ShaderGraph.BuiltInLitGUI" ""
CustomEditorForRenderPipeline "UnityEditor.ShaderGraphLitGUI" "UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset"
FallBack "Hidden/Shader Graph/FallbackError"
}