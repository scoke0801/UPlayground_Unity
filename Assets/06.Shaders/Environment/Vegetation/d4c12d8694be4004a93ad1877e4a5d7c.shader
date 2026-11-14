Shader "UPlayGround/Environment/Vegetation/d4c12d8694be4004a93ad1877e4a5d7c"
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

[NoScaleOffset]_Normal_Map("Normal Map", 2D) = "white" {}
_Normal_Strength("Normal Strength", Float) = 1
[NoScaleOffset]_RGB_Map("RGB Map", 2D) = "white" {}
_Scale_UVs_with_Object("Scale UVs with Object", Range(0, 1)) = 0
[NoScaleOffset]_Edge_Map("Edge Map", 2D) = "white" {}
_Stylization_Mask_Contrast("Stylization Mask Contrast", Float) = 0
_Stylization_Color("Stylization Color", Color) = (0.4528302, 0.4528302, 0.4528302, 0)
_AO_Mask_Contrast("AO Mask Contrast", Float) = 0
_AO_Color("AO Color", Color) = (0.2169811, 0.2169811, 0.2169811, 0)
_Dirt_Mask_Contrast("Dirt Mask Contrast", Float) = 0
_Dirt_Color("Dirt Color", Color) = (0.1650053, 0.3396226, 0.1929441, 1)
_Edgewear_Mask_Contrast("Edgewear Mask Contrast", Float) = 0
_Edgewear_Color("Edgewear Color", Color) = (0.7169812, 0.7169812, 0.7169812, 0)
[Toggle(_USE_LAYER_1)]_USE_LAYER_1("Use Layer 1", Float) = 0
_Layer_1_Tiling("Layer 1 Tiling", Float) = 1
[NoScaleOffset]_Layer_1_Color_Map("Layer 1 Color Map", 2D) = "white" {}
_Layer_1_Hue("Layer 1 Hue", Range(0, 1)) = 0
_Layer_1_Brightness("Layer 1 Brightness", Range(0, 3)) = 1
_Layer_1_Smoothness_Strength("Layer 1 Smoothness Strength", Range(0, 2)) = 1
[Normal][NoScaleOffset]_Layer_1_Normal_Map("Layer 1 Normal Map", 2D) = "bump" {}
_Layer_1_Normal_Strength("Layer 1 Normal Strength", Range(0, 3)) = 1
[Toggle(_USE_LAYER_2)]_USE_LAYER_2("Use Layer 2", Float) = 0
_Layer_2_Tiling("Layer 2 Tiling", Float) = 1
[NoScaleOffset]_Layer_2_Color_Map("Layer 2 Color Map", 2D) = "white" {}
_Layer_2_Hue("Layer 2 Hue", Range(0, 1)) = 0
_Layer_2_Brightness("Layer 2 Brightness", Range(0, 3)) = 1
_Layer_2_Smoothness_Strength("Layer 2 Smoothness Strength", Range(0, 2)) = 1
[Normal][NoScaleOffset]_Layer_2_Normal_Map("Layer 2 Normal Map", 2D) = "bump" {}
_Layer_2_Normal_Strength("Layer 2 Normal Strength", Range(0, 3)) = 1
[Toggle(_USE_LAYER_3)]_USE_LAYER_3("Use Layer 3", Float) = 0
_Layer_3_Tiling("Layer 3 Tiling", Float) = 1
[NoScaleOffset]_Layer_3_Color_Map("Layer 3 Color Map", 2D) = "white" {}
_Layer_3_Hue("Layer 3 Hue", Range(0, 1)) = 0
_Layer_3_Brightness("Layer 3 Brightness", Range(0, 3)) = 1
_Layer_3_Smoothness_Strength("Layer 3 Smoothness Strength", Range(0, 2)) = 1
[Normal][NoScaleOffset]_Layer_3_Normal_Map("Layer 3 Normal Map", 2D) = "bump" {}
_Layer_3_Normal_Strength("Layer 3 Normal Strength", Range(0, 3)) = 1
[Toggle(_USE_COVERAGE_LAYER)]_USE_COVERAGE_LAYER("Use Coverage Layer", Float) = 0
_Coverage_Normal_Mask_Slope("Coverage Normal Mask Slope", Float) = 0.5
_Coverage_Normal_Mask_Gradience("Coverage Normal Mask Gradience", Float) = 0.2
_Coverage_Height_Mask_Pivot_Offset("Coverage Height Mask Pivot Offset", Float) = 0
_Coverage_Height_Mask_Length("Coverage Height Mask Length", Float) = 5
_Coverage_Tiling("Coverage Tiling", Float) = 1
[NoScaleOffset]_Coverage_Color_Map("Coverage Color Map", 2D) = "white" {}
_Coverage_Hue("Coverage Hue", Range(0, 1)) = 0
_Coverage_Brightness("Coverage Brightness", Range(0, 3)) = 1
_Coverage_Smoothness_Strength("Coverage Smoothness Strength", Range(0, 2)) = 1
[Normal][NoScaleOffset]_Coverage_Normal_Map("Coverage Normal Map", 2D) = "bump" {}
_Coverage_Normal_Strength("Coverage Normal Strength", Range(0, 3)) = 1
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
#pragma shader_feature_local _ _USE_LAYER_2
#pragma shader_feature_local _ _USE_LAYER_3
#pragma shader_feature_local _ _USE_COVERAGE_LAYER
#pragma shader_feature_local _ _USE_LAYER_1

#if defined(_USE_LAYER_2) && defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_3) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_2
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_3)
#define KEYWORD_PERMUTATION_3
#elif defined(_USE_LAYER_2) && defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_4
#elif defined(_USE_LAYER_2) && defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_5
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_6
#elif defined(_USE_LAYER_2)
#define KEYWORD_PERMUTATION_7
#elif defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_8
#elif defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_9
#elif defined(_USE_LAYER_3) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_10
#elif defined(_USE_LAYER_3)
#define KEYWORD_PERMUTATION_11
#elif defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_12
#elif defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_13
#elif defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_14
#else
#define KEYWORD_PERMUTATION_15
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
 float3 WorldSpaceTangent;
 float3 WorldSpaceBiTangent;
 float3 WorldSpacePosition;
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

float4 _Layer_1_Color_Map_TexelSize;
float _Layer_1_Hue;
float _Layer_1_Brightness;
float _Coverage_Height_Mask_Length;
float4 _Layer_1_Normal_Map_TexelSize;
float _Layer_1_Normal_Strength;
float _Layer_1_Tiling;
float _Layer_1_Smoothness_Strength;
float _Layer_2_Tiling;
float4 _Layer_2_Color_Map_TexelSize;
float _Layer_2_Hue;
float _Layer_2_Brightness;
float _Layer_2_Smoothness_Strength;
float4 _Layer_2_Normal_Map_TexelSize;
float _Layer_2_Normal_Strength;
float _Layer_3_Tiling;
float4 _Layer_3_Color_Map_TexelSize;
float _Layer_3_Hue;
float _Layer_3_Brightness;
float _Layer_3_Smoothness_Strength;
float4 _Layer_3_Normal_Map_TexelSize;
float _Layer_3_Normal_Strength;
float4 _RGB_Map_TexelSize;
float4 _Normal_Map_TexelSize;
float _Normal_Strength;
float _Coverage_Tiling;
float4 _Coverage_Color_Map_TexelSize;
float _Coverage_Hue;
float _Coverage_Brightness;
float _Coverage_Smoothness_Strength;
float4 _Coverage_Normal_Map_TexelSize;
float _Coverage_Normal_Strength;
float4 _Edge_Map_TexelSize;
float _Dirt_Mask_Contrast;
float _AO_Mask_Contrast;
float _Edgewear_Mask_Contrast;
float _Stylization_Mask_Contrast;
float4 _Stylization_Color;
float4 _AO_Color;
float4 _Dirt_Color;
float4 _Edgewear_Color;
float _Coverage_Normal_Mask_Slope;
float _Coverage_Normal_Mask_Gradience;
float _Coverage_Height_Mask_Pivot_Offset;
float _Scale_UVs_with_Object;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Layer_1_Color_Map);
SAMPLER(sampler_Layer_1_Color_Map);
TEXTURE2D(_Layer_1_Normal_Map);
SAMPLER(sampler_Layer_1_Normal_Map);
TEXTURE2D(_Layer_2_Color_Map);
SAMPLER(sampler_Layer_2_Color_Map);
TEXTURE2D(_Layer_2_Normal_Map);
SAMPLER(sampler_Layer_2_Normal_Map);
TEXTURE2D(_Layer_3_Color_Map);
SAMPLER(sampler_Layer_3_Color_Map);
TEXTURE2D(_Layer_3_Normal_Map);
SAMPLER(sampler_Layer_3_Normal_Map);
TEXTURE2D(_RGB_Map);
SAMPLER(sampler_RGB_Map);
TEXTURE2D(_Normal_Map);
SAMPLER(sampler_Normal_Map);
TEXTURE2D(_Coverage_Color_Map);
SAMPLER(sampler_Coverage_Color_Map);
TEXTURE2D(_Coverage_Normal_Map);
SAMPLER(sampler_Coverage_Normal_Map);
TEXTURE2D(_Edge_Map);
SAMPLER(sampler_Edge_Map);

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

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Hue_Normalized_float(float3 In, float Offset, out float3 Out)
{
    // RGB to HSV
    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 P = lerp(float4(In.bg, K.wz), float4(In.gb, K.xy), step(In.b, In.g));
    float4 Q = lerp(float4(P.xyw, In.r), float4(In.r, P.yzx), step(P.x, In.r));
    float D = Q.x - min(Q.w, Q.y);
    float E = 1e-10;
    float V = (D == 0) ? Q.x : (Q.x + E);
    float3 hsv = float3(abs(Q.z + (Q.w - Q.y)/(6.0 * D + E)), D / (Q.x + E), V);

    float hue = hsv.x + Offset;
    hsv.x = (hue < 0)
            ? hue + 1
            : (hue > 1)
                ? hue - 1
                : hue;

    // HSV to RGB
    float4 K2 = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 P2 = abs(frac(hsv.xxx + K2.xyz) * 6.0 - K2.www);
    Out = hsv.z * lerp(K2.xxx, saturate(P2 - K2.xxx), hsv.y);
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Lerp_float(float A, float B, float T, out float Out)
{
    Out = lerp(A, B, T);
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

struct Bindings_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float
{
};

void SG_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float(float _Input, float _Contrast, Bindings_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float IN, out float Out_1)
{
float _Property_f17f2d608b764773bc9f33ea28c3e14e_Out_0_Float = _Contrast;
float _Add_59838b3eb9ec437bbd93b6fd8db7a0c4_Out_2_Float;
Unity_Add_float(_Property_f17f2d608b764773bc9f33ea28c3e14e_Out_0_Float, float(-1), _Add_59838b3eb9ec437bbd93b6fd8db7a0c4_Out_2_Float);
float _Add_524e73a4aa0b4e7982442fdf0d39fe18_Out_2_Float;
Unity_Add_float(_Property_f17f2d608b764773bc9f33ea28c3e14e_Out_0_Float, float(1), _Add_524e73a4aa0b4e7982442fdf0d39fe18_Out_2_Float);
float _Property_df60db08578f4909bad9a81c68245e45_Out_0_Float = _Input;
float _Lerp_6af4dfb2e0f0426a81a1669c55ec8196_Out_3_Float;
Unity_Lerp_float(_Add_59838b3eb9ec437bbd93b6fd8db7a0c4_Out_2_Float, _Add_524e73a4aa0b4e7982442fdf0d39fe18_Out_2_Float, _Property_df60db08578f4909bad9a81c68245e45_Out_0_Float, _Lerp_6af4dfb2e0f0426a81a1669c55ec8196_Out_3_Float);
float _Saturate_59b070c1298d4279be831e68d2cd96c8_Out_1_Float;
Unity_Saturate_float(_Lerp_6af4dfb2e0f0426a81a1669c55ec8196_Out_3_Float, _Saturate_59b070c1298d4279be831e68d2cd96c8_Out_1_Float);
Out_1 = _Saturate_59b070c1298d4279be831e68d2cd96c8_Out_1_Float;
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Blend_Multiply_float3(float3 Base, float3 Blend, out float3 Out, float Opacity)
{
    Out = Base * Blend;
    Out = lerp(Base, Out, Opacity);
}

void Unity_Blend_Overlay_float3(float3 Base, float3 Blend, out float3 Out, float Opacity)
{
    float3 result1 = 1.0 - 2.0 * (1.0 - Base) * (1.0 - Blend);
    float3 result2 = 2.0 * Base * Blend;
    float3 zeroOrOne = step(Base, 0.5);
    Out = result2 * zeroOrOne + (1 - zeroOrOne) * result1;
    Out = lerp(Base, Out, Opacity);
}

void Unity_NormalStrength_float(float3 In, float Strength, out float3 Out)
{
    Out = float3(In.rg * Strength, lerp(1, In.b, saturate(Strength)));
}

void Unity_NormalBlend_float(float3 A, float3 B, out float3 Out)
{
    Out = SafeNormalize(float3(A.rg + B.rg, A.b * B.b));
}

void Unity_DotProduct_float3(float3 A, float3 B, out float Out)
{
    Out = dot(A, B);
}

void Unity_Smoothstep_float(float Edge1, float Edge2, float In, out float Out)
{
    Out = smoothstep(Edge1, Edge2, In);
}

void Unity_Subtract_float3(float3 A, float3 B, out float3 Out)
{
    Out = A - B;
}

struct Bindings_MFPivotVerticalGradience_c9e909c1845b8ae4b9a11aa715963787_float
{
float3 WorldSpacePosition;
};

void SG_MFPivotVerticalGradience_c9e909c1845b8ae4b9a11aa715963787_float(float _Offset, float _Length, Bindings_MFPivotVerticalGradience_c9e909c1845b8ae4b9a11aa715963787_float IN, out float Mask_1)
{
float _Property_4680dca2dee245e7aa77915a0b17cce9_Out_0_Float = _Offset;
float _Property_c763914b9bbb4da5abad7a3db51adcb2_Out_0_Float = _Length;
float _Add_ad1531c9af874822b29fd9a13421289e_Out_2_Float;
Unity_Add_float(_Property_4680dca2dee245e7aa77915a0b17cce9_Out_0_Float, _Property_c763914b9bbb4da5abad7a3db51adcb2_Out_0_Float, _Add_ad1531c9af874822b29fd9a13421289e_Out_2_Float);
float3 _Subtract_69e80d3fb1bb4d71b4be84a010b280ff_Out_2_Vector3;
Unity_Subtract_float3(IN.WorldSpacePosition, SHADERGRAPH_OBJECT_POSITION, _Subtract_69e80d3fb1bb4d71b4be84a010b280ff_Out_2_Vector3);
float _Split_bceff54ddef34fab8cfa9a2a0b8bbc58_R_1_Float = _Subtract_69e80d3fb1bb4d71b4be84a010b280ff_Out_2_Vector3[0];
float _Split_bceff54ddef34fab8cfa9a2a0b8bbc58_G_2_Float = _Subtract_69e80d3fb1bb4d71b4be84a010b280ff_Out_2_Vector3[1];
float _Split_bceff54ddef34fab8cfa9a2a0b8bbc58_B_3_Float = _Subtract_69e80d3fb1bb4d71b4be84a010b280ff_Out_2_Vector3[2];
float _Split_bceff54ddef34fab8cfa9a2a0b8bbc58_A_4_Float = 0;
float _Smoothstep_0109929c8f9e41888dfa7ee4fd85c377_Out_3_Float;
Unity_Smoothstep_float(_Property_4680dca2dee245e7aa77915a0b17cce9_Out_0_Float, _Add_ad1531c9af874822b29fd9a13421289e_Out_2_Float, _Split_bceff54ddef34fab8cfa9a2a0b8bbc58_G_2_Float, _Smoothstep_0109929c8f9e41888dfa7ee4fd85c377_Out_3_Float);
Mask_1 = _Smoothstep_0109929c8f9e41888dfa7ee4fd85c377_Out_3_Float;
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
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
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
UnityTexture2D _Property_82391a4213d745e08202065048659634_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_1_Color_Map, sampler_Layer_1_Color_Map, _Layer_1_Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_7afe6a7edf62460887e09c4746d796b6_Out_0_Vector4 = IN.uv0;
float3 _Multiply_15b5d23699554ab298d90333ce57d387_Out_2_Vector3;
Unity_Multiply_float3_float3((_UV_7afe6a7edf62460887e09c4746d796b6_Out_0_Vector4.xyz), float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z))), _Multiply_15b5d23699554ab298d90333ce57d387_Out_2_Vector3);
float _Property_9a5b0bf1bf584876a406e2b633741c94_Out_0_Float = _Scale_UVs_with_Object;
float3 _Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3;
Unity_Lerp_float3((_UV_7afe6a7edf62460887e09c4746d796b6_Out_0_Vector4.xyz), _Multiply_15b5d23699554ab298d90333ce57d387_Out_2_Vector3, (_Property_9a5b0bf1bf584876a406e2b633741c94_Out_0_Float.xxx), _Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3);
float _Property_460238447f6b4170aa7397df1262be96_Out_0_Float = _Layer_1_Tiling;
float3 _Multiply_94cb4adbcc9a4b25980974e6be235287_Out_2_Vector3;
Unity_Multiply_float3_float3(_Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3, (_Property_460238447f6b4170aa7397df1262be96_Out_0_Float.xxx), _Multiply_94cb4adbcc9a4b25980974e6be235287_Out_2_Vector3);
float4 _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_82391a4213d745e08202065048659634_Out_0_Texture2D.tex, _Property_82391a4213d745e08202065048659634_Out_0_Texture2D.samplerstate, _Property_82391a4213d745e08202065048659634_Out_0_Texture2D.GetTransformedUV((_Multiply_94cb4adbcc9a4b25980974e6be235287_Out_2_Vector3.xy)) );
if (_Property_82391a4213d745e08202065048659634_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4, _Property_82391a4213d745e08202065048659634_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_R_4_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4.r;
float _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_G_5_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4.g;
float _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_B_6_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4.b;
float _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_A_7_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4.a;
float _Split_03d41d27a027425fa76a6c910dfbed96_R_1_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4[0];
float _Split_03d41d27a027425fa76a6c910dfbed96_G_2_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4[1];
float _Split_03d41d27a027425fa76a6c910dfbed96_B_3_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4[2];
float _Split_03d41d27a027425fa76a6c910dfbed96_A_4_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4[3];
float3 _Vector3_29d44982f90c486b8d58d91423294375_Out_0_Vector3 = float3(_Split_03d41d27a027425fa76a6c910dfbed96_R_1_Float, _Split_03d41d27a027425fa76a6c910dfbed96_G_2_Float, _Split_03d41d27a027425fa76a6c910dfbed96_B_3_Float);
float _Property_c55f5c80dc494683850ea3803571112b_Out_0_Float = _Layer_1_Hue;
float3 _Hue_4528903dcb5b4b15be0e273df247bfd9_Out_2_Vector3;
Unity_Hue_Normalized_float(_Vector3_29d44982f90c486b8d58d91423294375_Out_0_Vector3, _Property_c55f5c80dc494683850ea3803571112b_Out_0_Float, _Hue_4528903dcb5b4b15be0e273df247bfd9_Out_2_Vector3);
float _Property_ba7720a9d8134eb58030eb416c6a024a_Out_0_Float = _Layer_1_Brightness;
float3 _Multiply_01d7b815489a446fa8f5fbd7bf096d4f_Out_2_Vector3;
Unity_Multiply_float3_float3(_Hue_4528903dcb5b4b15be0e273df247bfd9_Out_2_Vector3, (_Property_ba7720a9d8134eb58030eb416c6a024a_Out_0_Float.xxx), _Multiply_01d7b815489a446fa8f5fbd7bf096d4f_Out_2_Vector3);
UnityTexture2D _Property_f37e4daa8ff94f5eb2ca9f6f83a20bb3_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_2_Color_Map, sampler_Layer_2_Color_Map, _Layer_2_Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_812f9793e99740fcb534b8c8d8af0ff2_Out_0_Float = _Layer_2_Tiling;
float3 _Multiply_7719c519f67d4b1aaaf2cca1cc2d4d79_Out_2_Vector3;
Unity_Multiply_float3_float3(_Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3, (_Property_812f9793e99740fcb534b8c8d8af0ff2_Out_0_Float.xxx), _Multiply_7719c519f67d4b1aaaf2cca1cc2d4d79_Out_2_Vector3);
float4 _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_f37e4daa8ff94f5eb2ca9f6f83a20bb3_Out_0_Texture2D.tex, _Property_f37e4daa8ff94f5eb2ca9f6f83a20bb3_Out_0_Texture2D.samplerstate, _Property_f37e4daa8ff94f5eb2ca9f6f83a20bb3_Out_0_Texture2D.GetTransformedUV((_Multiply_7719c519f67d4b1aaaf2cca1cc2d4d79_Out_2_Vector3.xy)) );
if (_Property_f37e4daa8ff94f5eb2ca9f6f83a20bb3_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4, _Property_f37e4daa8ff94f5eb2ca9f6f83a20bb3_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_R_4_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4.r;
float _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_G_5_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4.g;
float _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_B_6_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4.b;
float _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_A_7_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4.a;
float _Split_d38832e5c62c44d3a795b83acceab016_R_1_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4[0];
float _Split_d38832e5c62c44d3a795b83acceab016_G_2_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4[1];
float _Split_d38832e5c62c44d3a795b83acceab016_B_3_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4[2];
float _Split_d38832e5c62c44d3a795b83acceab016_A_4_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4[3];
float3 _Vector3_cb2069d38949469a976ac3a5a2003bd7_Out_0_Vector3 = float3(_Split_d38832e5c62c44d3a795b83acceab016_R_1_Float, _Split_d38832e5c62c44d3a795b83acceab016_G_2_Float, _Split_d38832e5c62c44d3a795b83acceab016_B_3_Float);
float _Property_8970d5f05c7d4b5783a57e1c656ee4a9_Out_0_Float = _Layer_2_Hue;
float3 _Hue_d8383543ffa64d25a0efd5fc36520be4_Out_2_Vector3;
Unity_Hue_Normalized_float(_Vector3_cb2069d38949469a976ac3a5a2003bd7_Out_0_Vector3, _Property_8970d5f05c7d4b5783a57e1c656ee4a9_Out_0_Float, _Hue_d8383543ffa64d25a0efd5fc36520be4_Out_2_Vector3);
float _Property_40475d05242a4efaa3e83e117ecd268d_Out_0_Float = _Layer_2_Brightness;
float3 _Multiply_9a8ec1bf43a149a7a5fb1571321b8200_Out_2_Vector3;
Unity_Multiply_float3_float3(_Hue_d8383543ffa64d25a0efd5fc36520be4_Out_2_Vector3, (_Property_40475d05242a4efaa3e83e117ecd268d_Out_0_Float.xxx), _Multiply_9a8ec1bf43a149a7a5fb1571321b8200_Out_2_Vector3);
UnityTexture2D _Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_RGB_Map, sampler_RGB_Map, _RGB_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D.tex, _Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D.samplerstate, _Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4, _Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_R_4_Float = _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4.r;
float _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_G_5_Float = _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4.g;
float _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_B_6_Float = _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4.b;
float _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_A_7_Float = _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4.a;
float3 _Lerp_754b59c21e034e87848bb65af4294c76_Out_3_Vector3;
Unity_Lerp_float3(_Multiply_01d7b815489a446fa8f5fbd7bf096d4f_Out_2_Vector3, _Multiply_9a8ec1bf43a149a7a5fb1571321b8200_Out_2_Vector3, (_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_G_5_Float.xxx), _Lerp_754b59c21e034e87848bb65af4294c76_Out_3_Vector3);
UnityTexture2D _Property_fef134089dc1401db12f51526b8ed988_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_3_Color_Map, sampler_Layer_3_Color_Map, _Layer_3_Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_8d20917a655e4062a5ce52f974969006_Out_0_Float = _Layer_3_Tiling;
float3 _Multiply_04b9ab405b2d4c27b6dd0696d9a5eacf_Out_2_Vector3;
Unity_Multiply_float3_float3(_Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3, (_Property_8d20917a655e4062a5ce52f974969006_Out_0_Float.xxx), _Multiply_04b9ab405b2d4c27b6dd0696d9a5eacf_Out_2_Vector3);
float4 _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_fef134089dc1401db12f51526b8ed988_Out_0_Texture2D.tex, _Property_fef134089dc1401db12f51526b8ed988_Out_0_Texture2D.samplerstate, _Property_fef134089dc1401db12f51526b8ed988_Out_0_Texture2D.GetTransformedUV((_Multiply_04b9ab405b2d4c27b6dd0696d9a5eacf_Out_2_Vector3.xy)) );
if (_Property_fef134089dc1401db12f51526b8ed988_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4, _Property_fef134089dc1401db12f51526b8ed988_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_R_4_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4.r;
float _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_G_5_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4.g;
float _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_B_6_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4.b;
float _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_A_7_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4.a;
float _Split_838946d3ce6f42cfbaf41fff836d6985_R_1_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4[0];
float _Split_838946d3ce6f42cfbaf41fff836d6985_G_2_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4[1];
float _Split_838946d3ce6f42cfbaf41fff836d6985_B_3_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4[2];
float _Split_838946d3ce6f42cfbaf41fff836d6985_A_4_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4[3];
float3 _Vector3_2a694d435907494fb07f2f9db09d031a_Out_0_Vector3 = float3(_Split_838946d3ce6f42cfbaf41fff836d6985_R_1_Float, _Split_838946d3ce6f42cfbaf41fff836d6985_G_2_Float, _Split_838946d3ce6f42cfbaf41fff836d6985_B_3_Float);
float _Property_6ba5eab2fbda4899981bd9c4c9c6317e_Out_0_Float = _Layer_3_Hue;
float3 _Hue_0a936fcae1974948a815da96c316b7e3_Out_2_Vector3;
Unity_Hue_Normalized_float(_Vector3_2a694d435907494fb07f2f9db09d031a_Out_0_Vector3, _Property_6ba5eab2fbda4899981bd9c4c9c6317e_Out_0_Float, _Hue_0a936fcae1974948a815da96c316b7e3_Out_2_Vector3);
float _Property_c749f04d24d9423fa1a2859f800dc893_Out_0_Float = _Layer_3_Brightness;
float3 _Multiply_db73d490dc9d432e80685ef18e5427fb_Out_2_Vector3;
Unity_Multiply_float3_float3(_Hue_0a936fcae1974948a815da96c316b7e3_Out_2_Vector3, (_Property_c749f04d24d9423fa1a2859f800dc893_Out_0_Float.xxx), _Multiply_db73d490dc9d432e80685ef18e5427fb_Out_2_Vector3);
float3 _Lerp_66f917f82ed14fd89c6c2fe066b5e834_Out_3_Vector3;
Unity_Lerp_float3(_Lerp_754b59c21e034e87848bb65af4294c76_Out_3_Vector3, _Multiply_db73d490dc9d432e80685ef18e5427fb_Out_2_Vector3, (_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_B_6_Float.xxx), _Lerp_66f917f82ed14fd89c6c2fe066b5e834_Out_3_Vector3);
#if defined(_USE_LAYER_3)
float3 _UseLayer3_ee2b565dd7774fa7a5c88d0b7bfd05b8_Out_0_Vector3 = _Lerp_66f917f82ed14fd89c6c2fe066b5e834_Out_3_Vector3;
#else
float3 _UseLayer3_ee2b565dd7774fa7a5c88d0b7bfd05b8_Out_0_Vector3 = _Lerp_754b59c21e034e87848bb65af4294c76_Out_3_Vector3;
#endif
#if defined(_USE_LAYER_2)
float3 _UseLayer2_22b88f361b80488fbbec2df7f45c4924_Out_0_Vector3 = _UseLayer3_ee2b565dd7774fa7a5c88d0b7bfd05b8_Out_0_Vector3;
#else
float3 _UseLayer2_22b88f361b80488fbbec2df7f45c4924_Out_0_Vector3 = _Multiply_01d7b815489a446fa8f5fbd7bf096d4f_Out_2_Vector3;
#endif
float4 _Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4 = _Stylization_Color;
float _Split_42e867e97f454925a658dba60e21864f_R_1_Float = _Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4[0];
float _Split_42e867e97f454925a658dba60e21864f_G_2_Float = _Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4[1];
float _Split_42e867e97f454925a658dba60e21864f_B_3_Float = _Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4[2];
float _Split_42e867e97f454925a658dba60e21864f_A_4_Float = _Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4[3];
UnityTexture2D _Property_c02e202098944eebbd44663588c64e3b_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Edge_Map, sampler_Edge_Map, _Edge_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_c02e202098944eebbd44663588c64e3b_Out_0_Texture2D.tex, _Property_c02e202098944eebbd44663588c64e3b_Out_0_Texture2D.samplerstate, _Property_c02e202098944eebbd44663588c64e3b_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_c02e202098944eebbd44663588c64e3b_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4, _Property_c02e202098944eebbd44663588c64e3b_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_R_4_Float = _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4.r;
float _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_G_5_Float = _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4.g;
float _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_B_6_Float = _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4.b;
float _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_A_7_Float = _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4.a;
float _Property_a4422d37671342bdab8d700fe4c2424a_Out_0_Float = _Stylization_Mask_Contrast;
Bindings_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float _MFCheapContrast_2e3888093b39403781e2ccbee9980699;
float _MFCheapContrast_2e3888093b39403781e2ccbee9980699_Out_1_Float;
SG_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float(_SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_A_7_Float, _Property_a4422d37671342bdab8d700fe4c2424a_Out_0_Float, _MFCheapContrast_2e3888093b39403781e2ccbee9980699, _MFCheapContrast_2e3888093b39403781e2ccbee9980699_Out_1_Float);
float _Multiply_364f0f8b2ff74453b42f050a8b1cb526_Out_2_Float;
Unity_Multiply_float_float(_Split_42e867e97f454925a658dba60e21864f_A_4_Float, _MFCheapContrast_2e3888093b39403781e2ccbee9980699_Out_1_Float, _Multiply_364f0f8b2ff74453b42f050a8b1cb526_Out_2_Float);
float3 _Lerp_0a1e51abb320477ab63b42e1a32052cc_Out_3_Vector3;
Unity_Lerp_float3(_UseLayer2_22b88f361b80488fbbec2df7f45c4924_Out_0_Vector3, (_Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4.xyz), (_Multiply_364f0f8b2ff74453b42f050a8b1cb526_Out_2_Float.xxx), _Lerp_0a1e51abb320477ab63b42e1a32052cc_Out_3_Vector3);
#if defined(_USE_LAYER_1)
float3 _UseLayer1_f5f8cf003ae64c4187983dc616ecbd3b_Out_0_Vector3 = _Lerp_0a1e51abb320477ab63b42e1a32052cc_Out_3_Vector3;
#else
float3 _UseLayer1_f5f8cf003ae64c4187983dc616ecbd3b_Out_0_Vector3 = (_Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4.xyz);
#endif
float4 _Property_0be88f8e5ba94765a3623024be14bbbf_Out_0_Vector4 = _AO_Color;
float _Split_07ec24369bd1411c82179cabd24aabe5_R_1_Float = _Property_0be88f8e5ba94765a3623024be14bbbf_Out_0_Vector4[0];
float _Split_07ec24369bd1411c82179cabd24aabe5_G_2_Float = _Property_0be88f8e5ba94765a3623024be14bbbf_Out_0_Vector4[1];
float _Split_07ec24369bd1411c82179cabd24aabe5_B_3_Float = _Property_0be88f8e5ba94765a3623024be14bbbf_Out_0_Vector4[2];
float _Split_07ec24369bd1411c82179cabd24aabe5_A_4_Float = _Property_0be88f8e5ba94765a3623024be14bbbf_Out_0_Vector4[3];
float _Property_781496b67f424345bca22ad901512267_Out_0_Float = _AO_Mask_Contrast;
Bindings_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float _MFCheapContrast_74522f75ddab433f9ec9f709fe5a6faf;
float _MFCheapContrast_74522f75ddab433f9ec9f709fe5a6faf_Out_1_Float;
SG_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float(_SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_G_5_Float, _Property_781496b67f424345bca22ad901512267_Out_0_Float, _MFCheapContrast_74522f75ddab433f9ec9f709fe5a6faf, _MFCheapContrast_74522f75ddab433f9ec9f709fe5a6faf_Out_1_Float);
float _Multiply_27eebb10636649c3aefb6ad13580fd9e_Out_2_Float;
Unity_Multiply_float_float(_Split_07ec24369bd1411c82179cabd24aabe5_A_4_Float, _MFCheapContrast_74522f75ddab433f9ec9f709fe5a6faf_Out_1_Float, _Multiply_27eebb10636649c3aefb6ad13580fd9e_Out_2_Float);
float3 _Blend_5baeb7dcebe145dcb53bba406ea82ebd_Out_2_Vector3;
Unity_Blend_Multiply_float3(_UseLayer1_f5f8cf003ae64c4187983dc616ecbd3b_Out_0_Vector3, (_Property_0be88f8e5ba94765a3623024be14bbbf_Out_0_Vector4.xyz), _Blend_5baeb7dcebe145dcb53bba406ea82ebd_Out_2_Vector3, _Multiply_27eebb10636649c3aefb6ad13580fd9e_Out_2_Float);
float4 _Property_64075cb2946e4bc89c0928961fa8e759_Out_0_Vector4 = _Dirt_Color;
float _Split_102b0005351f49a0a7ec214d4c4e879e_R_1_Float = _Property_64075cb2946e4bc89c0928961fa8e759_Out_0_Vector4[0];
float _Split_102b0005351f49a0a7ec214d4c4e879e_G_2_Float = _Property_64075cb2946e4bc89c0928961fa8e759_Out_0_Vector4[1];
float _Split_102b0005351f49a0a7ec214d4c4e879e_B_3_Float = _Property_64075cb2946e4bc89c0928961fa8e759_Out_0_Vector4[2];
float _Split_102b0005351f49a0a7ec214d4c4e879e_A_4_Float = _Property_64075cb2946e4bc89c0928961fa8e759_Out_0_Vector4[3];
float _Property_089f6755c08b483d971238d917dd8b3a_Out_0_Float = _Dirt_Mask_Contrast;
Bindings_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float _MFCheapContrast_96f7295ada8247d48955ac898a7aba11;
float _MFCheapContrast_96f7295ada8247d48955ac898a7aba11_Out_1_Float;
SG_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float(_SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_R_4_Float, _Property_089f6755c08b483d971238d917dd8b3a_Out_0_Float, _MFCheapContrast_96f7295ada8247d48955ac898a7aba11, _MFCheapContrast_96f7295ada8247d48955ac898a7aba11_Out_1_Float);
float _Multiply_bc33d3e78b104e93a60b60cea85cebac_Out_2_Float;
Unity_Multiply_float_float(_Split_102b0005351f49a0a7ec214d4c4e879e_A_4_Float, _MFCheapContrast_96f7295ada8247d48955ac898a7aba11_Out_1_Float, _Multiply_bc33d3e78b104e93a60b60cea85cebac_Out_2_Float);
float3 _Lerp_ef18ae06dc3e4fd18201e3925bc55a85_Out_3_Vector3;
Unity_Lerp_float3(_Blend_5baeb7dcebe145dcb53bba406ea82ebd_Out_2_Vector3, (_Property_64075cb2946e4bc89c0928961fa8e759_Out_0_Vector4.xyz), (_Multiply_bc33d3e78b104e93a60b60cea85cebac_Out_2_Float.xxx), _Lerp_ef18ae06dc3e4fd18201e3925bc55a85_Out_3_Vector3);
float4 _Property_dbda4c8e93f54f92a18b3390cbaf06c0_Out_0_Vector4 = _Edgewear_Color;
float _Split_925676a8ce21420aad811d255e389512_R_1_Float = _Property_dbda4c8e93f54f92a18b3390cbaf06c0_Out_0_Vector4[0];
float _Split_925676a8ce21420aad811d255e389512_G_2_Float = _Property_dbda4c8e93f54f92a18b3390cbaf06c0_Out_0_Vector4[1];
float _Split_925676a8ce21420aad811d255e389512_B_3_Float = _Property_dbda4c8e93f54f92a18b3390cbaf06c0_Out_0_Vector4[2];
float _Split_925676a8ce21420aad811d255e389512_A_4_Float = _Property_dbda4c8e93f54f92a18b3390cbaf06c0_Out_0_Vector4[3];
float _Property_4bef2cb688474ef8909e6dc3f2439616_Out_0_Float = _Edgewear_Mask_Contrast;
Bindings_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float _MFCheapContrast_c8d787e56cf842869980c82103f39754;
float _MFCheapContrast_c8d787e56cf842869980c82103f39754_Out_1_Float;
SG_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float(_SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_B_6_Float, _Property_4bef2cb688474ef8909e6dc3f2439616_Out_0_Float, _MFCheapContrast_c8d787e56cf842869980c82103f39754, _MFCheapContrast_c8d787e56cf842869980c82103f39754_Out_1_Float);
float _Multiply_38b6bc16bfef4cd3bec7370aabb6a836_Out_2_Float;
Unity_Multiply_float_float(_Split_925676a8ce21420aad811d255e389512_A_4_Float, _MFCheapContrast_c8d787e56cf842869980c82103f39754_Out_1_Float, _Multiply_38b6bc16bfef4cd3bec7370aabb6a836_Out_2_Float);
float3 _Blend_e613dc790cd741d3b367cb48289cffd5_Out_2_Vector3;
Unity_Blend_Overlay_float3(_Lerp_ef18ae06dc3e4fd18201e3925bc55a85_Out_3_Vector3, (_Property_dbda4c8e93f54f92a18b3390cbaf06c0_Out_0_Vector4.xyz), _Blend_e613dc790cd741d3b367cb48289cffd5_Out_2_Vector3, _Multiply_38b6bc16bfef4cd3bec7370aabb6a836_Out_2_Float);
UnityTexture2D _Property_003a6ee6678543e5964d3e811c19f789_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Coverage_Color_Map, sampler_Coverage_Color_Map, _Coverage_Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_d9f12d2b2464447a82f581c14778bbf8_Out_0_Float = _Coverage_Tiling;
float3 _Multiply_282d032113614cb58779c3a33671ad6c_Out_2_Vector3;
Unity_Multiply_float3_float3(_Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3, (_Property_d9f12d2b2464447a82f581c14778bbf8_Out_0_Float.xxx), _Multiply_282d032113614cb58779c3a33671ad6c_Out_2_Vector3);
float4 _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_003a6ee6678543e5964d3e811c19f789_Out_0_Texture2D.tex, _Property_003a6ee6678543e5964d3e811c19f789_Out_0_Texture2D.samplerstate, _Property_003a6ee6678543e5964d3e811c19f789_Out_0_Texture2D.GetTransformedUV((_Multiply_282d032113614cb58779c3a33671ad6c_Out_2_Vector3.xy)) );
if (_Property_003a6ee6678543e5964d3e811c19f789_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4, _Property_003a6ee6678543e5964d3e811c19f789_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_R_4_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4.r;
float _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_G_5_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4.g;
float _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_B_6_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4.b;
float _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_A_7_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4.a;
float _Split_42bc736ed19d41c2bc8a58c7f653b802_R_1_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4[0];
float _Split_42bc736ed19d41c2bc8a58c7f653b802_G_2_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4[1];
float _Split_42bc736ed19d41c2bc8a58c7f653b802_B_3_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4[2];
float _Split_42bc736ed19d41c2bc8a58c7f653b802_A_4_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4[3];
float3 _Vector3_dd63cc8dd9a443a99a6ecc681ff6961e_Out_0_Vector3 = float3(_Split_42bc736ed19d41c2bc8a58c7f653b802_R_1_Float, _Split_42bc736ed19d41c2bc8a58c7f653b802_G_2_Float, _Split_42bc736ed19d41c2bc8a58c7f653b802_B_3_Float);
float _Property_86cbded0425a4bf2b5441e665a7f5551_Out_0_Float = _Coverage_Hue;
float3 _Hue_5e168e8c10044fe09120668e4b870b44_Out_2_Vector3;
Unity_Hue_Normalized_float(_Vector3_dd63cc8dd9a443a99a6ecc681ff6961e_Out_0_Vector3, _Property_86cbded0425a4bf2b5441e665a7f5551_Out_0_Float, _Hue_5e168e8c10044fe09120668e4b870b44_Out_2_Vector3);
float _Property_dccedc571d6d411291da660d59f31596_Out_0_Float = _Coverage_Brightness;
float3 _Multiply_c28f28e86afd43f584c04c39cc44c8c4_Out_2_Vector3;
Unity_Multiply_float3_float3(_Hue_5e168e8c10044fe09120668e4b870b44_Out_2_Vector3, (_Property_dccedc571d6d411291da660d59f31596_Out_0_Float.xxx), _Multiply_c28f28e86afd43f584c04c39cc44c8c4_Out_2_Vector3);
float _Property_f13be210d8b049a9a1e6c0e219f5a53f_Out_0_Float = _Coverage_Normal_Mask_Slope;
float _Property_1f786f31d0924f6ba0904b90319c56e3_Out_0_Float = _Coverage_Normal_Mask_Gradience;
float _Add_4177ca63c9724385910d37f97d0e8c48_Out_2_Float;
Unity_Add_float(_Property_f13be210d8b049a9a1e6c0e219f5a53f_Out_0_Float, _Property_1f786f31d0924f6ba0904b90319c56e3_Out_0_Float, _Add_4177ca63c9724385910d37f97d0e8c48_Out_2_Float);
UnityTexture2D _Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_1_Normal_Map, sampler_Layer_1_Normal_Map, _Layer_1_Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D.tex, _Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D.samplerstate, _Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D.GetTransformedUV((_Multiply_94cb4adbcc9a4b25980974e6be235287_Out_2_Vector3.xy)) );
if (_Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4, _Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4);
float _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_R_4_Float = _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.r;
float _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_G_5_Float = _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.g;
float _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_B_6_Float = _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.b;
float _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_A_7_Float = _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.a;
float _Property_7cfa9a8e94684810a4cd4c70a2257b6c_Out_0_Float = _Layer_1_Normal_Strength;
float3 _NormalStrength_9428e53c24e74ce189625604c4b60275_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.xyz), _Property_7cfa9a8e94684810a4cd4c70a2257b6c_Out_0_Float, _NormalStrength_9428e53c24e74ce189625604c4b60275_Out_2_Vector3);
UnityTexture2D _Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_2_Normal_Map, sampler_Layer_2_Normal_Map, _Layer_2_Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D.tex, _Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D.samplerstate, _Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D.GetTransformedUV((_Multiply_7719c519f67d4b1aaaf2cca1cc2d4d79_Out_2_Vector3.xy)) );
if (_Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4, _Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4);
float _SampleTexture2D_ae5b372472874ad28eac881821ce553c_R_4_Float = _SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.r;
float _SampleTexture2D_ae5b372472874ad28eac881821ce553c_G_5_Float = _SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.g;
float _SampleTexture2D_ae5b372472874ad28eac881821ce553c_B_6_Float = _SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.b;
float _SampleTexture2D_ae5b372472874ad28eac881821ce553c_A_7_Float = _SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.a;
float _Property_12c65401b09f44daa763974826741097_Out_0_Float = _Layer_2_Normal_Strength;
float3 _NormalStrength_93ad91a06836481ca4f36c947e62a393_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.xyz), _Property_12c65401b09f44daa763974826741097_Out_0_Float, _NormalStrength_93ad91a06836481ca4f36c947e62a393_Out_2_Vector3);
float3 _Lerp_7325f1720917490cb13328e58ddd8efe_Out_3_Vector3;
Unity_Lerp_float3(_NormalStrength_9428e53c24e74ce189625604c4b60275_Out_2_Vector3, _NormalStrength_93ad91a06836481ca4f36c947e62a393_Out_2_Vector3, (_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_G_5_Float.xxx), _Lerp_7325f1720917490cb13328e58ddd8efe_Out_3_Vector3);
UnityTexture2D _Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_3_Normal_Map, sampler_Layer_3_Normal_Map, _Layer_3_Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D.tex, _Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D.samplerstate, _Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D.GetTransformedUV((_Multiply_04b9ab405b2d4c27b6dd0696d9a5eacf_Out_2_Vector3.xy)) );
if (_Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4, _Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4);
float _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_R_4_Float = _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.r;
float _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_G_5_Float = _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.g;
float _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_B_6_Float = _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.b;
float _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_A_7_Float = _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.a;
float _Property_02480e6b3805449397b2d5a15e416673_Out_0_Float = _Layer_3_Normal_Strength;
float3 _NormalStrength_8e30a22d9ed147ca9ef200087a7f43aa_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.xyz), _Property_02480e6b3805449397b2d5a15e416673_Out_0_Float, _NormalStrength_8e30a22d9ed147ca9ef200087a7f43aa_Out_2_Vector3);
float3 _Lerp_642d324ee8d74cf888dc943377359b6d_Out_3_Vector3;
Unity_Lerp_float3(_Lerp_7325f1720917490cb13328e58ddd8efe_Out_3_Vector3, _NormalStrength_8e30a22d9ed147ca9ef200087a7f43aa_Out_2_Vector3, (_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_B_6_Float.xxx), _Lerp_642d324ee8d74cf888dc943377359b6d_Out_3_Vector3);
#if defined(_USE_LAYER_3)
float3 _UseLayer3_3e504be39f154559868343947f457c24_Out_0_Vector3 = _Lerp_642d324ee8d74cf888dc943377359b6d_Out_3_Vector3;
#else
float3 _UseLayer3_3e504be39f154559868343947f457c24_Out_0_Vector3 = _Lerp_7325f1720917490cb13328e58ddd8efe_Out_3_Vector3;
#endif
#if defined(_USE_LAYER_2)
float3 _UseLayer2_c90f92a0b0134057867d0810829f779e_Out_0_Vector3 = _UseLayer3_3e504be39f154559868343947f457c24_Out_0_Vector3;
#else
float3 _UseLayer2_c90f92a0b0134057867d0810829f779e_Out_0_Vector3 = _NormalStrength_9428e53c24e74ce189625604c4b60275_Out_2_Vector3;
#endif
UnityTexture2D _Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Normal_Map, sampler_Normal_Map, _Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D.tex, _Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D.samplerstate, _Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4, _Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4);
float _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_R_4_Float = _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.r;
float _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_G_5_Float = _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.g;
float _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_B_6_Float = _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.b;
float _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_A_7_Float = _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.a;
float _Property_758b3d19a0c14ff099921db6234664b2_Out_0_Float = _Normal_Strength;
float3 _NormalStrength_3eb407934b854455a959c26453776991_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.xyz), _Property_758b3d19a0c14ff099921db6234664b2_Out_0_Float, _NormalStrength_3eb407934b854455a959c26453776991_Out_2_Vector3);
float3 _NormalBlend_cb4851ae94454d539b1004bdc1f2604f_Out_2_Vector3;
Unity_NormalBlend_float(_UseLayer2_c90f92a0b0134057867d0810829f779e_Out_0_Vector3, _NormalStrength_3eb407934b854455a959c26453776991_Out_2_Vector3, _NormalBlend_cb4851ae94454d539b1004bdc1f2604f_Out_2_Vector3);
#if defined(_USE_LAYER_1)
float3 _UseLayer1_7da71e644bf2434f99e8005bad9e8f14_Out_0_Vector3 = _NormalBlend_cb4851ae94454d539b1004bdc1f2604f_Out_2_Vector3;
#else
float3 _UseLayer1_7da71e644bf2434f99e8005bad9e8f14_Out_0_Vector3 = _NormalStrength_3eb407934b854455a959c26453776991_Out_2_Vector3;
#endif
float3 _Transform_04398ac1bc954fb19c62ce3f5f6d670c_Out_1_Vector3;
{
float3x3 tangentTransform = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
_Transform_04398ac1bc954fb19c62ce3f5f6d670c_Out_1_Vector3 = TransformTangentToWorld(_UseLayer1_7da71e644bf2434f99e8005bad9e8f14_Out_0_Vector3.xyz, tangentTransform, true);
}
float _DotProduct_ae13069c870e4539be3826642299bede_Out_2_Float;
Unity_DotProduct_float3(_Transform_04398ac1bc954fb19c62ce3f5f6d670c_Out_1_Vector3, float3(0, 1, 0), _DotProduct_ae13069c870e4539be3826642299bede_Out_2_Float);
float _Smoothstep_652b53e75f9e481e8c829d3bd3d9c24e_Out_3_Float;
Unity_Smoothstep_float(_Property_f13be210d8b049a9a1e6c0e219f5a53f_Out_0_Float, _Add_4177ca63c9724385910d37f97d0e8c48_Out_2_Float, _DotProduct_ae13069c870e4539be3826642299bede_Out_2_Float, _Smoothstep_652b53e75f9e481e8c829d3bd3d9c24e_Out_3_Float);
float _Property_64938267e5134c5b9a4256d11aa25705_Out_0_Float = _Coverage_Height_Mask_Pivot_Offset;
float _Property_cfcce6f026924b048f8c33bdc73c14f6_Out_0_Float = _Coverage_Height_Mask_Length;
Bindings_MFPivotVerticalGradience_c9e909c1845b8ae4b9a11aa715963787_float _MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c;
_MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c.WorldSpacePosition = IN.WorldSpacePosition;
float _MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c_Mask_1_Float;
SG_MFPivotVerticalGradience_c9e909c1845b8ae4b9a11aa715963787_float(_Property_64938267e5134c5b9a4256d11aa25705_Out_0_Float, _Property_cfcce6f026924b048f8c33bdc73c14f6_Out_0_Float, _MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c, _MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c_Mask_1_Float);
float _Multiply_f6a21f2d15b7472e8173002af669cfdf_Out_2_Float;
Unity_Multiply_float_float(_Smoothstep_652b53e75f9e481e8c829d3bd3d9c24e_Out_3_Float, _MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c_Mask_1_Float, _Multiply_f6a21f2d15b7472e8173002af669cfdf_Out_2_Float);
float3 _Lerp_90a44aeed76b4e67b245ea63960c28fc_Out_3_Vector3;
Unity_Lerp_float3(_Blend_e613dc790cd741d3b367cb48289cffd5_Out_2_Vector3, _Multiply_c28f28e86afd43f584c04c39cc44c8c4_Out_2_Vector3, (_Multiply_f6a21f2d15b7472e8173002af669cfdf_Out_2_Float.xxx), _Lerp_90a44aeed76b4e67b245ea63960c28fc_Out_3_Vector3);
#if defined(_USE_COVERAGE_LAYER)
float3 _UseCoverageLayer_402efe74563f417bb22b7997bdfc5c1e_Out_0_Vector3 = _Lerp_90a44aeed76b4e67b245ea63960c28fc_Out_3_Vector3;
#else
float3 _UseCoverageLayer_402efe74563f417bb22b7997bdfc5c1e_Out_0_Vector3 = _Blend_e613dc790cd741d3b367cb48289cffd5_Out_2_Vector3;
#endif
UnityTexture2D _Property_022d04960c1e41229b03a237b2bf022a_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Coverage_Normal_Map, sampler_Coverage_Normal_Map, _Coverage_Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_022d04960c1e41229b03a237b2bf022a_Out_0_Texture2D.tex, _Property_022d04960c1e41229b03a237b2bf022a_Out_0_Texture2D.samplerstate, _Property_022d04960c1e41229b03a237b2bf022a_Out_0_Texture2D.GetTransformedUV((_Multiply_282d032113614cb58779c3a33671ad6c_Out_2_Vector3.xy)) );
if (_Property_022d04960c1e41229b03a237b2bf022a_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4, _Property_022d04960c1e41229b03a237b2bf022a_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4);
float _SampleTexture2D_046783cfeeed4edebbc5805118c09db7_R_4_Float = _SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4.r;
float _SampleTexture2D_046783cfeeed4edebbc5805118c09db7_G_5_Float = _SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4.g;
float _SampleTexture2D_046783cfeeed4edebbc5805118c09db7_B_6_Float = _SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4.b;
float _SampleTexture2D_046783cfeeed4edebbc5805118c09db7_A_7_Float = _SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4.a;
float _Property_aff65d79d743476f9e681da55383046d_Out_0_Float = _Coverage_Normal_Strength;
float3 _NormalStrength_f478d3c35fc741e78611a44ac09caa44_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4.xyz), _Property_aff65d79d743476f9e681da55383046d_Out_0_Float, _NormalStrength_f478d3c35fc741e78611a44ac09caa44_Out_2_Vector3);
float3 _Lerp_0e62040e3ecb4bf3b09d8424f7147818_Out_3_Vector3;
Unity_Lerp_float3(float3(0, 0, 1), _NormalStrength_f478d3c35fc741e78611a44ac09caa44_Out_2_Vector3, (_Multiply_f6a21f2d15b7472e8173002af669cfdf_Out_2_Float.xxx), _Lerp_0e62040e3ecb4bf3b09d8424f7147818_Out_3_Vector3);
float3 _NormalBlend_4ffbea1bfb5d43979177bcfb52b4abcf_Out_2_Vector3;
Unity_NormalBlend_float(_Lerp_0e62040e3ecb4bf3b09d8424f7147818_Out_3_Vector3, _UseLayer1_7da71e644bf2434f99e8005bad9e8f14_Out_0_Vector3, _NormalBlend_4ffbea1bfb5d43979177bcfb52b4abcf_Out_2_Vector3);
#if defined(_USE_COVERAGE_LAYER)
float3 _UseCoverageLayer_b0f12be34bf24ec7ba2137cef5a254cb_Out_0_Vector3 = _NormalBlend_4ffbea1bfb5d43979177bcfb52b4abcf_Out_2_Vector3;
#else
float3 _UseCoverageLayer_b0f12be34bf24ec7ba2137cef5a254cb_Out_0_Vector3 = _UseLayer1_7da71e644bf2434f99e8005bad9e8f14_Out_0_Vector3;
#endif
float _OneMinus_7a2b995f5d6a4a619d8a66eddf498446_Out_1_Float;
Unity_OneMinus_float(_Split_03d41d27a027425fa76a6c910dfbed96_A_4_Float, _OneMinus_7a2b995f5d6a4a619d8a66eddf498446_Out_1_Float);
float _Property_3f3b47f88ff24548ba31a6777c3b2a85_Out_0_Float = _Layer_1_Smoothness_Strength;
float _Lerp_f0ccf02f1b3a423eb14231584191b608_Out_3_Float;
Unity_Lerp_float(float(0), _OneMinus_7a2b995f5d6a4a619d8a66eddf498446_Out_1_Float, _Property_3f3b47f88ff24548ba31a6777c3b2a85_Out_0_Float, _Lerp_f0ccf02f1b3a423eb14231584191b608_Out_3_Float);
float _OneMinus_16665b1602fc4acaa17e0caa00dd6ac7_Out_1_Float;
Unity_OneMinus_float(_Split_d38832e5c62c44d3a795b83acceab016_A_4_Float, _OneMinus_16665b1602fc4acaa17e0caa00dd6ac7_Out_1_Float);
float _Property_3bfd1b8cbe534165807a6d38b20b8b3f_Out_0_Float = _Layer_2_Smoothness_Strength;
float _Lerp_5f73c06703364fa1b14c25a300968493_Out_3_Float;
Unity_Lerp_float(float(0), _OneMinus_16665b1602fc4acaa17e0caa00dd6ac7_Out_1_Float, _Property_3bfd1b8cbe534165807a6d38b20b8b3f_Out_0_Float, _Lerp_5f73c06703364fa1b14c25a300968493_Out_3_Float);
float _Lerp_b5d123a1ad4c4307979f2143cfd5d3ac_Out_3_Float;
Unity_Lerp_float(_Lerp_f0ccf02f1b3a423eb14231584191b608_Out_3_Float, _Lerp_5f73c06703364fa1b14c25a300968493_Out_3_Float, _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_G_5_Float, _Lerp_b5d123a1ad4c4307979f2143cfd5d3ac_Out_3_Float);
float _OneMinus_afad5b1133a04d90b4af7d2e859ee909_Out_1_Float;
Unity_OneMinus_float(_Split_838946d3ce6f42cfbaf41fff836d6985_A_4_Float, _OneMinus_afad5b1133a04d90b4af7d2e859ee909_Out_1_Float);
float _Property_269daba898014930a8cc45eae3b32a0d_Out_0_Float = _Layer_3_Smoothness_Strength;
float _Lerp_73575fb5baba4126bfd6048130db9abc_Out_3_Float;
Unity_Lerp_float(float(0), _OneMinus_afad5b1133a04d90b4af7d2e859ee909_Out_1_Float, _Property_269daba898014930a8cc45eae3b32a0d_Out_0_Float, _Lerp_73575fb5baba4126bfd6048130db9abc_Out_3_Float);
float _Lerp_0ed87b2c815a4b6bb1c07d0ba2ad7f9e_Out_3_Float;
Unity_Lerp_float(_Lerp_b5d123a1ad4c4307979f2143cfd5d3ac_Out_3_Float, _Lerp_73575fb5baba4126bfd6048130db9abc_Out_3_Float, _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_B_6_Float, _Lerp_0ed87b2c815a4b6bb1c07d0ba2ad7f9e_Out_3_Float);
#if defined(_USE_LAYER_3)
float _UseLayer3_38584d454ac2471687bb98edbed983aa_Out_0_Float = _Lerp_0ed87b2c815a4b6bb1c07d0ba2ad7f9e_Out_3_Float;
#else
float _UseLayer3_38584d454ac2471687bb98edbed983aa_Out_0_Float = _Lerp_b5d123a1ad4c4307979f2143cfd5d3ac_Out_3_Float;
#endif
#if defined(_USE_LAYER_2)
float _UseLayer2_66f28f7fecc141028a5f15673f047917_Out_0_Float = _UseLayer3_38584d454ac2471687bb98edbed983aa_Out_0_Float;
#else
float _UseLayer2_66f28f7fecc141028a5f15673f047917_Out_0_Float = _Lerp_f0ccf02f1b3a423eb14231584191b608_Out_3_Float;
#endif
#if defined(_USE_LAYER_1)
float _UseLayer1_6040c5b1bc15438dae7a97e54a8e3324_Out_0_Float = _UseLayer2_66f28f7fecc141028a5f15673f047917_Out_0_Float;
#else
float _UseLayer1_6040c5b1bc15438dae7a97e54a8e3324_Out_0_Float = float(0);
#endif
float _OneMinus_f65c9822794443558d97581fc4537919_Out_1_Float;
Unity_OneMinus_float(_Split_42bc736ed19d41c2bc8a58c7f653b802_A_4_Float, _OneMinus_f65c9822794443558d97581fc4537919_Out_1_Float);
float _Property_eb619aeb69fd4914bebabe935f6e2c25_Out_0_Float = _Coverage_Smoothness_Strength;
float _Lerp_2c28e4b743af489e941e7226cc40512b_Out_3_Float;
Unity_Lerp_float(float(0), _OneMinus_f65c9822794443558d97581fc4537919_Out_1_Float, _Property_eb619aeb69fd4914bebabe935f6e2c25_Out_0_Float, _Lerp_2c28e4b743af489e941e7226cc40512b_Out_3_Float);
float _Lerp_1237b11b7e0241aba05ae87b4cdb9167_Out_3_Float;
Unity_Lerp_float(_UseLayer1_6040c5b1bc15438dae7a97e54a8e3324_Out_0_Float, _Lerp_2c28e4b743af489e941e7226cc40512b_Out_3_Float, _Multiply_f6a21f2d15b7472e8173002af669cfdf_Out_2_Float, _Lerp_1237b11b7e0241aba05ae87b4cdb9167_Out_3_Float);
#if defined(_USE_COVERAGE_LAYER)
float _UseCoverageLayer_3e91540d12544fcfa939ef997f0c72e0_Out_0_Float = _Lerp_1237b11b7e0241aba05ae87b4cdb9167_Out_3_Float;
#else
float _UseCoverageLayer_3e91540d12544fcfa939ef997f0c72e0_Out_0_Float = _UseLayer1_6040c5b1bc15438dae7a97e54a8e3324_Out_0_Float;
#endif
surface.BaseColor = _UseCoverageLayer_402efe74563f417bb22b7997bdfc5c1e_Out_0_Vector3;
surface.NormalTS = _UseCoverageLayer_b0f12be34bf24ec7ba2137cef5a254cb_Out_0_Vector3;
surface.Emission = float3(0, 0, 0);
surface.Metallic = float(0);
surface.Smoothness = _UseCoverageLayer_3e91540d12544fcfa939ef997f0c72e0_Out_0_Float;
surface.Occlusion = float(1);
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
    output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);

    // to pr               eserve mikktspace compliance we use same scale renormFactor as was used on the normal.
    // This                is explained in section 2.2 in "surface gradient based bump mapping framework"
    output.WorldSpaceTangent = renormFactor * input.tangentWS.xyz;
    output.WorldSpaceBiTangent = renormFactor * bitang;

    output.WorldSpacePosition = input.positionWS;

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
#pragma shader_feature_local _ _USE_LAYER_2
#pragma shader_feature_local _ _USE_LAYER_3
#pragma shader_feature_local _ _USE_COVERAGE_LAYER
#pragma shader_feature_local _ _USE_LAYER_1

#if defined(_USE_LAYER_2) && defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_3) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_2
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_3)
#define KEYWORD_PERMUTATION_3
#elif defined(_USE_LAYER_2) && defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_4
#elif defined(_USE_LAYER_2) && defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_5
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_6
#elif defined(_USE_LAYER_2)
#define KEYWORD_PERMUTATION_7
#elif defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_8
#elif defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_9
#elif defined(_USE_LAYER_3) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_10
#elif defined(_USE_LAYER_3)
#define KEYWORD_PERMUTATION_11
#elif defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_12
#elif defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_13
#elif defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_14
#else
#define KEYWORD_PERMUTATION_15
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
 float3 WorldSpaceTangent;
 float3 WorldSpaceBiTangent;
 float3 WorldSpacePosition;
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

float4 _Layer_1_Color_Map_TexelSize;
float _Layer_1_Hue;
float _Layer_1_Brightness;
float _Coverage_Height_Mask_Length;
float4 _Layer_1_Normal_Map_TexelSize;
float _Layer_1_Normal_Strength;
float _Layer_1_Tiling;
float _Layer_1_Smoothness_Strength;
float _Layer_2_Tiling;
float4 _Layer_2_Color_Map_TexelSize;
float _Layer_2_Hue;
float _Layer_2_Brightness;
float _Layer_2_Smoothness_Strength;
float4 _Layer_2_Normal_Map_TexelSize;
float _Layer_2_Normal_Strength;
float _Layer_3_Tiling;
float4 _Layer_3_Color_Map_TexelSize;
float _Layer_3_Hue;
float _Layer_3_Brightness;
float _Layer_3_Smoothness_Strength;
float4 _Layer_3_Normal_Map_TexelSize;
float _Layer_3_Normal_Strength;
float4 _RGB_Map_TexelSize;
float4 _Normal_Map_TexelSize;
float _Normal_Strength;
float _Coverage_Tiling;
float4 _Coverage_Color_Map_TexelSize;
float _Coverage_Hue;
float _Coverage_Brightness;
float _Coverage_Smoothness_Strength;
float4 _Coverage_Normal_Map_TexelSize;
float _Coverage_Normal_Strength;
float4 _Edge_Map_TexelSize;
float _Dirt_Mask_Contrast;
float _AO_Mask_Contrast;
float _Edgewear_Mask_Contrast;
float _Stylization_Mask_Contrast;
float4 _Stylization_Color;
float4 _AO_Color;
float4 _Dirt_Color;
float4 _Edgewear_Color;
float _Coverage_Normal_Mask_Slope;
float _Coverage_Normal_Mask_Gradience;
float _Coverage_Height_Mask_Pivot_Offset;
float _Scale_UVs_with_Object;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Layer_1_Color_Map);
SAMPLER(sampler_Layer_1_Color_Map);
TEXTURE2D(_Layer_1_Normal_Map);
SAMPLER(sampler_Layer_1_Normal_Map);
TEXTURE2D(_Layer_2_Color_Map);
SAMPLER(sampler_Layer_2_Color_Map);
TEXTURE2D(_Layer_2_Normal_Map);
SAMPLER(sampler_Layer_2_Normal_Map);
TEXTURE2D(_Layer_3_Color_Map);
SAMPLER(sampler_Layer_3_Color_Map);
TEXTURE2D(_Layer_3_Normal_Map);
SAMPLER(sampler_Layer_3_Normal_Map);
TEXTURE2D(_RGB_Map);
SAMPLER(sampler_RGB_Map);
TEXTURE2D(_Normal_Map);
SAMPLER(sampler_Normal_Map);
TEXTURE2D(_Coverage_Color_Map);
SAMPLER(sampler_Coverage_Color_Map);
TEXTURE2D(_Coverage_Normal_Map);
SAMPLER(sampler_Coverage_Normal_Map);
TEXTURE2D(_Edge_Map);
SAMPLER(sampler_Edge_Map);

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

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Hue_Normalized_float(float3 In, float Offset, out float3 Out)
{
    // RGB to HSV
    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 P = lerp(float4(In.bg, K.wz), float4(In.gb, K.xy), step(In.b, In.g));
    float4 Q = lerp(float4(P.xyw, In.r), float4(In.r, P.yzx), step(P.x, In.r));
    float D = Q.x - min(Q.w, Q.y);
    float E = 1e-10;
    float V = (D == 0) ? Q.x : (Q.x + E);
    float3 hsv = float3(abs(Q.z + (Q.w - Q.y)/(6.0 * D + E)), D / (Q.x + E), V);

    float hue = hsv.x + Offset;
    hsv.x = (hue < 0)
            ? hue + 1
            : (hue > 1)
                ? hue - 1
                : hue;

    // HSV to RGB
    float4 K2 = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 P2 = abs(frac(hsv.xxx + K2.xyz) * 6.0 - K2.www);
    Out = hsv.z * lerp(K2.xxx, saturate(P2 - K2.xxx), hsv.y);
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Lerp_float(float A, float B, float T, out float Out)
{
    Out = lerp(A, B, T);
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

struct Bindings_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float
{
};

void SG_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float(float _Input, float _Contrast, Bindings_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float IN, out float Out_1)
{
float _Property_f17f2d608b764773bc9f33ea28c3e14e_Out_0_Float = _Contrast;
float _Add_59838b3eb9ec437bbd93b6fd8db7a0c4_Out_2_Float;
Unity_Add_float(_Property_f17f2d608b764773bc9f33ea28c3e14e_Out_0_Float, float(-1), _Add_59838b3eb9ec437bbd93b6fd8db7a0c4_Out_2_Float);
float _Add_524e73a4aa0b4e7982442fdf0d39fe18_Out_2_Float;
Unity_Add_float(_Property_f17f2d608b764773bc9f33ea28c3e14e_Out_0_Float, float(1), _Add_524e73a4aa0b4e7982442fdf0d39fe18_Out_2_Float);
float _Property_df60db08578f4909bad9a81c68245e45_Out_0_Float = _Input;
float _Lerp_6af4dfb2e0f0426a81a1669c55ec8196_Out_3_Float;
Unity_Lerp_float(_Add_59838b3eb9ec437bbd93b6fd8db7a0c4_Out_2_Float, _Add_524e73a4aa0b4e7982442fdf0d39fe18_Out_2_Float, _Property_df60db08578f4909bad9a81c68245e45_Out_0_Float, _Lerp_6af4dfb2e0f0426a81a1669c55ec8196_Out_3_Float);
float _Saturate_59b070c1298d4279be831e68d2cd96c8_Out_1_Float;
Unity_Saturate_float(_Lerp_6af4dfb2e0f0426a81a1669c55ec8196_Out_3_Float, _Saturate_59b070c1298d4279be831e68d2cd96c8_Out_1_Float);
Out_1 = _Saturate_59b070c1298d4279be831e68d2cd96c8_Out_1_Float;
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Blend_Multiply_float3(float3 Base, float3 Blend, out float3 Out, float Opacity)
{
    Out = Base * Blend;
    Out = lerp(Base, Out, Opacity);
}

void Unity_Blend_Overlay_float3(float3 Base, float3 Blend, out float3 Out, float Opacity)
{
    float3 result1 = 1.0 - 2.0 * (1.0 - Base) * (1.0 - Blend);
    float3 result2 = 2.0 * Base * Blend;
    float3 zeroOrOne = step(Base, 0.5);
    Out = result2 * zeroOrOne + (1 - zeroOrOne) * result1;
    Out = lerp(Base, Out, Opacity);
}

void Unity_NormalStrength_float(float3 In, float Strength, out float3 Out)
{
    Out = float3(In.rg * Strength, lerp(1, In.b, saturate(Strength)));
}

void Unity_NormalBlend_float(float3 A, float3 B, out float3 Out)
{
    Out = SafeNormalize(float3(A.rg + B.rg, A.b * B.b));
}

void Unity_DotProduct_float3(float3 A, float3 B, out float Out)
{
    Out = dot(A, B);
}

void Unity_Smoothstep_float(float Edge1, float Edge2, float In, out float Out)
{
    Out = smoothstep(Edge1, Edge2, In);
}

void Unity_Subtract_float3(float3 A, float3 B, out float3 Out)
{
    Out = A - B;
}

struct Bindings_MFPivotVerticalGradience_c9e909c1845b8ae4b9a11aa715963787_float
{
float3 WorldSpacePosition;
};

void SG_MFPivotVerticalGradience_c9e909c1845b8ae4b9a11aa715963787_float(float _Offset, float _Length, Bindings_MFPivotVerticalGradience_c9e909c1845b8ae4b9a11aa715963787_float IN, out float Mask_1)
{
float _Property_4680dca2dee245e7aa77915a0b17cce9_Out_0_Float = _Offset;
float _Property_c763914b9bbb4da5abad7a3db51adcb2_Out_0_Float = _Length;
float _Add_ad1531c9af874822b29fd9a13421289e_Out_2_Float;
Unity_Add_float(_Property_4680dca2dee245e7aa77915a0b17cce9_Out_0_Float, _Property_c763914b9bbb4da5abad7a3db51adcb2_Out_0_Float, _Add_ad1531c9af874822b29fd9a13421289e_Out_2_Float);
float3 _Subtract_69e80d3fb1bb4d71b4be84a010b280ff_Out_2_Vector3;
Unity_Subtract_float3(IN.WorldSpacePosition, SHADERGRAPH_OBJECT_POSITION, _Subtract_69e80d3fb1bb4d71b4be84a010b280ff_Out_2_Vector3);
float _Split_bceff54ddef34fab8cfa9a2a0b8bbc58_R_1_Float = _Subtract_69e80d3fb1bb4d71b4be84a010b280ff_Out_2_Vector3[0];
float _Split_bceff54ddef34fab8cfa9a2a0b8bbc58_G_2_Float = _Subtract_69e80d3fb1bb4d71b4be84a010b280ff_Out_2_Vector3[1];
float _Split_bceff54ddef34fab8cfa9a2a0b8bbc58_B_3_Float = _Subtract_69e80d3fb1bb4d71b4be84a010b280ff_Out_2_Vector3[2];
float _Split_bceff54ddef34fab8cfa9a2a0b8bbc58_A_4_Float = 0;
float _Smoothstep_0109929c8f9e41888dfa7ee4fd85c377_Out_3_Float;
Unity_Smoothstep_float(_Property_4680dca2dee245e7aa77915a0b17cce9_Out_0_Float, _Add_ad1531c9af874822b29fd9a13421289e_Out_2_Float, _Split_bceff54ddef34fab8cfa9a2a0b8bbc58_G_2_Float, _Smoothstep_0109929c8f9e41888dfa7ee4fd85c377_Out_3_Float);
Mask_1 = _Smoothstep_0109929c8f9e41888dfa7ee4fd85c377_Out_3_Float;
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
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
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
UnityTexture2D _Property_82391a4213d745e08202065048659634_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_1_Color_Map, sampler_Layer_1_Color_Map, _Layer_1_Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_7afe6a7edf62460887e09c4746d796b6_Out_0_Vector4 = IN.uv0;
float3 _Multiply_15b5d23699554ab298d90333ce57d387_Out_2_Vector3;
Unity_Multiply_float3_float3((_UV_7afe6a7edf62460887e09c4746d796b6_Out_0_Vector4.xyz), float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z))), _Multiply_15b5d23699554ab298d90333ce57d387_Out_2_Vector3);
float _Property_9a5b0bf1bf584876a406e2b633741c94_Out_0_Float = _Scale_UVs_with_Object;
float3 _Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3;
Unity_Lerp_float3((_UV_7afe6a7edf62460887e09c4746d796b6_Out_0_Vector4.xyz), _Multiply_15b5d23699554ab298d90333ce57d387_Out_2_Vector3, (_Property_9a5b0bf1bf584876a406e2b633741c94_Out_0_Float.xxx), _Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3);
float _Property_460238447f6b4170aa7397df1262be96_Out_0_Float = _Layer_1_Tiling;
float3 _Multiply_94cb4adbcc9a4b25980974e6be235287_Out_2_Vector3;
Unity_Multiply_float3_float3(_Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3, (_Property_460238447f6b4170aa7397df1262be96_Out_0_Float.xxx), _Multiply_94cb4adbcc9a4b25980974e6be235287_Out_2_Vector3);
float4 _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_82391a4213d745e08202065048659634_Out_0_Texture2D.tex, _Property_82391a4213d745e08202065048659634_Out_0_Texture2D.samplerstate, _Property_82391a4213d745e08202065048659634_Out_0_Texture2D.GetTransformedUV((_Multiply_94cb4adbcc9a4b25980974e6be235287_Out_2_Vector3.xy)) );
if (_Property_82391a4213d745e08202065048659634_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4, _Property_82391a4213d745e08202065048659634_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_R_4_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4.r;
float _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_G_5_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4.g;
float _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_B_6_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4.b;
float _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_A_7_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4.a;
float _Split_03d41d27a027425fa76a6c910dfbed96_R_1_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4[0];
float _Split_03d41d27a027425fa76a6c910dfbed96_G_2_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4[1];
float _Split_03d41d27a027425fa76a6c910dfbed96_B_3_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4[2];
float _Split_03d41d27a027425fa76a6c910dfbed96_A_4_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4[3];
float3 _Vector3_29d44982f90c486b8d58d91423294375_Out_0_Vector3 = float3(_Split_03d41d27a027425fa76a6c910dfbed96_R_1_Float, _Split_03d41d27a027425fa76a6c910dfbed96_G_2_Float, _Split_03d41d27a027425fa76a6c910dfbed96_B_3_Float);
float _Property_c55f5c80dc494683850ea3803571112b_Out_0_Float = _Layer_1_Hue;
float3 _Hue_4528903dcb5b4b15be0e273df247bfd9_Out_2_Vector3;
Unity_Hue_Normalized_float(_Vector3_29d44982f90c486b8d58d91423294375_Out_0_Vector3, _Property_c55f5c80dc494683850ea3803571112b_Out_0_Float, _Hue_4528903dcb5b4b15be0e273df247bfd9_Out_2_Vector3);
float _Property_ba7720a9d8134eb58030eb416c6a024a_Out_0_Float = _Layer_1_Brightness;
float3 _Multiply_01d7b815489a446fa8f5fbd7bf096d4f_Out_2_Vector3;
Unity_Multiply_float3_float3(_Hue_4528903dcb5b4b15be0e273df247bfd9_Out_2_Vector3, (_Property_ba7720a9d8134eb58030eb416c6a024a_Out_0_Float.xxx), _Multiply_01d7b815489a446fa8f5fbd7bf096d4f_Out_2_Vector3);
UnityTexture2D _Property_f37e4daa8ff94f5eb2ca9f6f83a20bb3_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_2_Color_Map, sampler_Layer_2_Color_Map, _Layer_2_Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_812f9793e99740fcb534b8c8d8af0ff2_Out_0_Float = _Layer_2_Tiling;
float3 _Multiply_7719c519f67d4b1aaaf2cca1cc2d4d79_Out_2_Vector3;
Unity_Multiply_float3_float3(_Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3, (_Property_812f9793e99740fcb534b8c8d8af0ff2_Out_0_Float.xxx), _Multiply_7719c519f67d4b1aaaf2cca1cc2d4d79_Out_2_Vector3);
float4 _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_f37e4daa8ff94f5eb2ca9f6f83a20bb3_Out_0_Texture2D.tex, _Property_f37e4daa8ff94f5eb2ca9f6f83a20bb3_Out_0_Texture2D.samplerstate, _Property_f37e4daa8ff94f5eb2ca9f6f83a20bb3_Out_0_Texture2D.GetTransformedUV((_Multiply_7719c519f67d4b1aaaf2cca1cc2d4d79_Out_2_Vector3.xy)) );
if (_Property_f37e4daa8ff94f5eb2ca9f6f83a20bb3_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4, _Property_f37e4daa8ff94f5eb2ca9f6f83a20bb3_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_R_4_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4.r;
float _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_G_5_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4.g;
float _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_B_6_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4.b;
float _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_A_7_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4.a;
float _Split_d38832e5c62c44d3a795b83acceab016_R_1_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4[0];
float _Split_d38832e5c62c44d3a795b83acceab016_G_2_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4[1];
float _Split_d38832e5c62c44d3a795b83acceab016_B_3_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4[2];
float _Split_d38832e5c62c44d3a795b83acceab016_A_4_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4[3];
float3 _Vector3_cb2069d38949469a976ac3a5a2003bd7_Out_0_Vector3 = float3(_Split_d38832e5c62c44d3a795b83acceab016_R_1_Float, _Split_d38832e5c62c44d3a795b83acceab016_G_2_Float, _Split_d38832e5c62c44d3a795b83acceab016_B_3_Float);
float _Property_8970d5f05c7d4b5783a57e1c656ee4a9_Out_0_Float = _Layer_2_Hue;
float3 _Hue_d8383543ffa64d25a0efd5fc36520be4_Out_2_Vector3;
Unity_Hue_Normalized_float(_Vector3_cb2069d38949469a976ac3a5a2003bd7_Out_0_Vector3, _Property_8970d5f05c7d4b5783a57e1c656ee4a9_Out_0_Float, _Hue_d8383543ffa64d25a0efd5fc36520be4_Out_2_Vector3);
float _Property_40475d05242a4efaa3e83e117ecd268d_Out_0_Float = _Layer_2_Brightness;
float3 _Multiply_9a8ec1bf43a149a7a5fb1571321b8200_Out_2_Vector3;
Unity_Multiply_float3_float3(_Hue_d8383543ffa64d25a0efd5fc36520be4_Out_2_Vector3, (_Property_40475d05242a4efaa3e83e117ecd268d_Out_0_Float.xxx), _Multiply_9a8ec1bf43a149a7a5fb1571321b8200_Out_2_Vector3);
UnityTexture2D _Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_RGB_Map, sampler_RGB_Map, _RGB_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D.tex, _Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D.samplerstate, _Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4, _Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_R_4_Float = _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4.r;
float _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_G_5_Float = _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4.g;
float _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_B_6_Float = _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4.b;
float _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_A_7_Float = _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4.a;
float3 _Lerp_754b59c21e034e87848bb65af4294c76_Out_3_Vector3;
Unity_Lerp_float3(_Multiply_01d7b815489a446fa8f5fbd7bf096d4f_Out_2_Vector3, _Multiply_9a8ec1bf43a149a7a5fb1571321b8200_Out_2_Vector3, (_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_G_5_Float.xxx), _Lerp_754b59c21e034e87848bb65af4294c76_Out_3_Vector3);
UnityTexture2D _Property_fef134089dc1401db12f51526b8ed988_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_3_Color_Map, sampler_Layer_3_Color_Map, _Layer_3_Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_8d20917a655e4062a5ce52f974969006_Out_0_Float = _Layer_3_Tiling;
float3 _Multiply_04b9ab405b2d4c27b6dd0696d9a5eacf_Out_2_Vector3;
Unity_Multiply_float3_float3(_Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3, (_Property_8d20917a655e4062a5ce52f974969006_Out_0_Float.xxx), _Multiply_04b9ab405b2d4c27b6dd0696d9a5eacf_Out_2_Vector3);
float4 _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_fef134089dc1401db12f51526b8ed988_Out_0_Texture2D.tex, _Property_fef134089dc1401db12f51526b8ed988_Out_0_Texture2D.samplerstate, _Property_fef134089dc1401db12f51526b8ed988_Out_0_Texture2D.GetTransformedUV((_Multiply_04b9ab405b2d4c27b6dd0696d9a5eacf_Out_2_Vector3.xy)) );
if (_Property_fef134089dc1401db12f51526b8ed988_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4, _Property_fef134089dc1401db12f51526b8ed988_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_R_4_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4.r;
float _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_G_5_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4.g;
float _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_B_6_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4.b;
float _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_A_7_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4.a;
float _Split_838946d3ce6f42cfbaf41fff836d6985_R_1_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4[0];
float _Split_838946d3ce6f42cfbaf41fff836d6985_G_2_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4[1];
float _Split_838946d3ce6f42cfbaf41fff836d6985_B_3_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4[2];
float _Split_838946d3ce6f42cfbaf41fff836d6985_A_4_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4[3];
float3 _Vector3_2a694d435907494fb07f2f9db09d031a_Out_0_Vector3 = float3(_Split_838946d3ce6f42cfbaf41fff836d6985_R_1_Float, _Split_838946d3ce6f42cfbaf41fff836d6985_G_2_Float, _Split_838946d3ce6f42cfbaf41fff836d6985_B_3_Float);
float _Property_6ba5eab2fbda4899981bd9c4c9c6317e_Out_0_Float = _Layer_3_Hue;
float3 _Hue_0a936fcae1974948a815da96c316b7e3_Out_2_Vector3;
Unity_Hue_Normalized_float(_Vector3_2a694d435907494fb07f2f9db09d031a_Out_0_Vector3, _Property_6ba5eab2fbda4899981bd9c4c9c6317e_Out_0_Float, _Hue_0a936fcae1974948a815da96c316b7e3_Out_2_Vector3);
float _Property_c749f04d24d9423fa1a2859f800dc893_Out_0_Float = _Layer_3_Brightness;
float3 _Multiply_db73d490dc9d432e80685ef18e5427fb_Out_2_Vector3;
Unity_Multiply_float3_float3(_Hue_0a936fcae1974948a815da96c316b7e3_Out_2_Vector3, (_Property_c749f04d24d9423fa1a2859f800dc893_Out_0_Float.xxx), _Multiply_db73d490dc9d432e80685ef18e5427fb_Out_2_Vector3);
float3 _Lerp_66f917f82ed14fd89c6c2fe066b5e834_Out_3_Vector3;
Unity_Lerp_float3(_Lerp_754b59c21e034e87848bb65af4294c76_Out_3_Vector3, _Multiply_db73d490dc9d432e80685ef18e5427fb_Out_2_Vector3, (_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_B_6_Float.xxx), _Lerp_66f917f82ed14fd89c6c2fe066b5e834_Out_3_Vector3);
#if defined(_USE_LAYER_3)
float3 _UseLayer3_ee2b565dd7774fa7a5c88d0b7bfd05b8_Out_0_Vector3 = _Lerp_66f917f82ed14fd89c6c2fe066b5e834_Out_3_Vector3;
#else
float3 _UseLayer3_ee2b565dd7774fa7a5c88d0b7bfd05b8_Out_0_Vector3 = _Lerp_754b59c21e034e87848bb65af4294c76_Out_3_Vector3;
#endif
#if defined(_USE_LAYER_2)
float3 _UseLayer2_22b88f361b80488fbbec2df7f45c4924_Out_0_Vector3 = _UseLayer3_ee2b565dd7774fa7a5c88d0b7bfd05b8_Out_0_Vector3;
#else
float3 _UseLayer2_22b88f361b80488fbbec2df7f45c4924_Out_0_Vector3 = _Multiply_01d7b815489a446fa8f5fbd7bf096d4f_Out_2_Vector3;
#endif
float4 _Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4 = _Stylization_Color;
float _Split_42e867e97f454925a658dba60e21864f_R_1_Float = _Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4[0];
float _Split_42e867e97f454925a658dba60e21864f_G_2_Float = _Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4[1];
float _Split_42e867e97f454925a658dba60e21864f_B_3_Float = _Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4[2];
float _Split_42e867e97f454925a658dba60e21864f_A_4_Float = _Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4[3];
UnityTexture2D _Property_c02e202098944eebbd44663588c64e3b_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Edge_Map, sampler_Edge_Map, _Edge_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_c02e202098944eebbd44663588c64e3b_Out_0_Texture2D.tex, _Property_c02e202098944eebbd44663588c64e3b_Out_0_Texture2D.samplerstate, _Property_c02e202098944eebbd44663588c64e3b_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_c02e202098944eebbd44663588c64e3b_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4, _Property_c02e202098944eebbd44663588c64e3b_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_R_4_Float = _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4.r;
float _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_G_5_Float = _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4.g;
float _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_B_6_Float = _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4.b;
float _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_A_7_Float = _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4.a;
float _Property_a4422d37671342bdab8d700fe4c2424a_Out_0_Float = _Stylization_Mask_Contrast;
Bindings_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float _MFCheapContrast_2e3888093b39403781e2ccbee9980699;
float _MFCheapContrast_2e3888093b39403781e2ccbee9980699_Out_1_Float;
SG_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float(_SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_A_7_Float, _Property_a4422d37671342bdab8d700fe4c2424a_Out_0_Float, _MFCheapContrast_2e3888093b39403781e2ccbee9980699, _MFCheapContrast_2e3888093b39403781e2ccbee9980699_Out_1_Float);
float _Multiply_364f0f8b2ff74453b42f050a8b1cb526_Out_2_Float;
Unity_Multiply_float_float(_Split_42e867e97f454925a658dba60e21864f_A_4_Float, _MFCheapContrast_2e3888093b39403781e2ccbee9980699_Out_1_Float, _Multiply_364f0f8b2ff74453b42f050a8b1cb526_Out_2_Float);
float3 _Lerp_0a1e51abb320477ab63b42e1a32052cc_Out_3_Vector3;
Unity_Lerp_float3(_UseLayer2_22b88f361b80488fbbec2df7f45c4924_Out_0_Vector3, (_Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4.xyz), (_Multiply_364f0f8b2ff74453b42f050a8b1cb526_Out_2_Float.xxx), _Lerp_0a1e51abb320477ab63b42e1a32052cc_Out_3_Vector3);
#if defined(_USE_LAYER_1)
float3 _UseLayer1_f5f8cf003ae64c4187983dc616ecbd3b_Out_0_Vector3 = _Lerp_0a1e51abb320477ab63b42e1a32052cc_Out_3_Vector3;
#else
float3 _UseLayer1_f5f8cf003ae64c4187983dc616ecbd3b_Out_0_Vector3 = (_Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4.xyz);
#endif
float4 _Property_0be88f8e5ba94765a3623024be14bbbf_Out_0_Vector4 = _AO_Color;
float _Split_07ec24369bd1411c82179cabd24aabe5_R_1_Float = _Property_0be88f8e5ba94765a3623024be14bbbf_Out_0_Vector4[0];
float _Split_07ec24369bd1411c82179cabd24aabe5_G_2_Float = _Property_0be88f8e5ba94765a3623024be14bbbf_Out_0_Vector4[1];
float _Split_07ec24369bd1411c82179cabd24aabe5_B_3_Float = _Property_0be88f8e5ba94765a3623024be14bbbf_Out_0_Vector4[2];
float _Split_07ec24369bd1411c82179cabd24aabe5_A_4_Float = _Property_0be88f8e5ba94765a3623024be14bbbf_Out_0_Vector4[3];
float _Property_781496b67f424345bca22ad901512267_Out_0_Float = _AO_Mask_Contrast;
Bindings_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float _MFCheapContrast_74522f75ddab433f9ec9f709fe5a6faf;
float _MFCheapContrast_74522f75ddab433f9ec9f709fe5a6faf_Out_1_Float;
SG_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float(_SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_G_5_Float, _Property_781496b67f424345bca22ad901512267_Out_0_Float, _MFCheapContrast_74522f75ddab433f9ec9f709fe5a6faf, _MFCheapContrast_74522f75ddab433f9ec9f709fe5a6faf_Out_1_Float);
float _Multiply_27eebb10636649c3aefb6ad13580fd9e_Out_2_Float;
Unity_Multiply_float_float(_Split_07ec24369bd1411c82179cabd24aabe5_A_4_Float, _MFCheapContrast_74522f75ddab433f9ec9f709fe5a6faf_Out_1_Float, _Multiply_27eebb10636649c3aefb6ad13580fd9e_Out_2_Float);
float3 _Blend_5baeb7dcebe145dcb53bba406ea82ebd_Out_2_Vector3;
Unity_Blend_Multiply_float3(_UseLayer1_f5f8cf003ae64c4187983dc616ecbd3b_Out_0_Vector3, (_Property_0be88f8e5ba94765a3623024be14bbbf_Out_0_Vector4.xyz), _Blend_5baeb7dcebe145dcb53bba406ea82ebd_Out_2_Vector3, _Multiply_27eebb10636649c3aefb6ad13580fd9e_Out_2_Float);
float4 _Property_64075cb2946e4bc89c0928961fa8e759_Out_0_Vector4 = _Dirt_Color;
float _Split_102b0005351f49a0a7ec214d4c4e879e_R_1_Float = _Property_64075cb2946e4bc89c0928961fa8e759_Out_0_Vector4[0];
float _Split_102b0005351f49a0a7ec214d4c4e879e_G_2_Float = _Property_64075cb2946e4bc89c0928961fa8e759_Out_0_Vector4[1];
float _Split_102b0005351f49a0a7ec214d4c4e879e_B_3_Float = _Property_64075cb2946e4bc89c0928961fa8e759_Out_0_Vector4[2];
float _Split_102b0005351f49a0a7ec214d4c4e879e_A_4_Float = _Property_64075cb2946e4bc89c0928961fa8e759_Out_0_Vector4[3];
float _Property_089f6755c08b483d971238d917dd8b3a_Out_0_Float = _Dirt_Mask_Contrast;
Bindings_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float _MFCheapContrast_96f7295ada8247d48955ac898a7aba11;
float _MFCheapContrast_96f7295ada8247d48955ac898a7aba11_Out_1_Float;
SG_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float(_SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_R_4_Float, _Property_089f6755c08b483d971238d917dd8b3a_Out_0_Float, _MFCheapContrast_96f7295ada8247d48955ac898a7aba11, _MFCheapContrast_96f7295ada8247d48955ac898a7aba11_Out_1_Float);
float _Multiply_bc33d3e78b104e93a60b60cea85cebac_Out_2_Float;
Unity_Multiply_float_float(_Split_102b0005351f49a0a7ec214d4c4e879e_A_4_Float, _MFCheapContrast_96f7295ada8247d48955ac898a7aba11_Out_1_Float, _Multiply_bc33d3e78b104e93a60b60cea85cebac_Out_2_Float);
float3 _Lerp_ef18ae06dc3e4fd18201e3925bc55a85_Out_3_Vector3;
Unity_Lerp_float3(_Blend_5baeb7dcebe145dcb53bba406ea82ebd_Out_2_Vector3, (_Property_64075cb2946e4bc89c0928961fa8e759_Out_0_Vector4.xyz), (_Multiply_bc33d3e78b104e93a60b60cea85cebac_Out_2_Float.xxx), _Lerp_ef18ae06dc3e4fd18201e3925bc55a85_Out_3_Vector3);
float4 _Property_dbda4c8e93f54f92a18b3390cbaf06c0_Out_0_Vector4 = _Edgewear_Color;
float _Split_925676a8ce21420aad811d255e389512_R_1_Float = _Property_dbda4c8e93f54f92a18b3390cbaf06c0_Out_0_Vector4[0];
float _Split_925676a8ce21420aad811d255e389512_G_2_Float = _Property_dbda4c8e93f54f92a18b3390cbaf06c0_Out_0_Vector4[1];
float _Split_925676a8ce21420aad811d255e389512_B_3_Float = _Property_dbda4c8e93f54f92a18b3390cbaf06c0_Out_0_Vector4[2];
float _Split_925676a8ce21420aad811d255e389512_A_4_Float = _Property_dbda4c8e93f54f92a18b3390cbaf06c0_Out_0_Vector4[3];
float _Property_4bef2cb688474ef8909e6dc3f2439616_Out_0_Float = _Edgewear_Mask_Contrast;
Bindings_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float _MFCheapContrast_c8d787e56cf842869980c82103f39754;
float _MFCheapContrast_c8d787e56cf842869980c82103f39754_Out_1_Float;
SG_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float(_SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_B_6_Float, _Property_4bef2cb688474ef8909e6dc3f2439616_Out_0_Float, _MFCheapContrast_c8d787e56cf842869980c82103f39754, _MFCheapContrast_c8d787e56cf842869980c82103f39754_Out_1_Float);
float _Multiply_38b6bc16bfef4cd3bec7370aabb6a836_Out_2_Float;
Unity_Multiply_float_float(_Split_925676a8ce21420aad811d255e389512_A_4_Float, _MFCheapContrast_c8d787e56cf842869980c82103f39754_Out_1_Float, _Multiply_38b6bc16bfef4cd3bec7370aabb6a836_Out_2_Float);
float3 _Blend_e613dc790cd741d3b367cb48289cffd5_Out_2_Vector3;
Unity_Blend_Overlay_float3(_Lerp_ef18ae06dc3e4fd18201e3925bc55a85_Out_3_Vector3, (_Property_dbda4c8e93f54f92a18b3390cbaf06c0_Out_0_Vector4.xyz), _Blend_e613dc790cd741d3b367cb48289cffd5_Out_2_Vector3, _Multiply_38b6bc16bfef4cd3bec7370aabb6a836_Out_2_Float);
UnityTexture2D _Property_003a6ee6678543e5964d3e811c19f789_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Coverage_Color_Map, sampler_Coverage_Color_Map, _Coverage_Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_d9f12d2b2464447a82f581c14778bbf8_Out_0_Float = _Coverage_Tiling;
float3 _Multiply_282d032113614cb58779c3a33671ad6c_Out_2_Vector3;
Unity_Multiply_float3_float3(_Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3, (_Property_d9f12d2b2464447a82f581c14778bbf8_Out_0_Float.xxx), _Multiply_282d032113614cb58779c3a33671ad6c_Out_2_Vector3);
float4 _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_003a6ee6678543e5964d3e811c19f789_Out_0_Texture2D.tex, _Property_003a6ee6678543e5964d3e811c19f789_Out_0_Texture2D.samplerstate, _Property_003a6ee6678543e5964d3e811c19f789_Out_0_Texture2D.GetTransformedUV((_Multiply_282d032113614cb58779c3a33671ad6c_Out_2_Vector3.xy)) );
if (_Property_003a6ee6678543e5964d3e811c19f789_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4, _Property_003a6ee6678543e5964d3e811c19f789_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_R_4_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4.r;
float _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_G_5_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4.g;
float _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_B_6_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4.b;
float _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_A_7_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4.a;
float _Split_42bc736ed19d41c2bc8a58c7f653b802_R_1_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4[0];
float _Split_42bc736ed19d41c2bc8a58c7f653b802_G_2_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4[1];
float _Split_42bc736ed19d41c2bc8a58c7f653b802_B_3_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4[2];
float _Split_42bc736ed19d41c2bc8a58c7f653b802_A_4_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4[3];
float3 _Vector3_dd63cc8dd9a443a99a6ecc681ff6961e_Out_0_Vector3 = float3(_Split_42bc736ed19d41c2bc8a58c7f653b802_R_1_Float, _Split_42bc736ed19d41c2bc8a58c7f653b802_G_2_Float, _Split_42bc736ed19d41c2bc8a58c7f653b802_B_3_Float);
float _Property_86cbded0425a4bf2b5441e665a7f5551_Out_0_Float = _Coverage_Hue;
float3 _Hue_5e168e8c10044fe09120668e4b870b44_Out_2_Vector3;
Unity_Hue_Normalized_float(_Vector3_dd63cc8dd9a443a99a6ecc681ff6961e_Out_0_Vector3, _Property_86cbded0425a4bf2b5441e665a7f5551_Out_0_Float, _Hue_5e168e8c10044fe09120668e4b870b44_Out_2_Vector3);
float _Property_dccedc571d6d411291da660d59f31596_Out_0_Float = _Coverage_Brightness;
float3 _Multiply_c28f28e86afd43f584c04c39cc44c8c4_Out_2_Vector3;
Unity_Multiply_float3_float3(_Hue_5e168e8c10044fe09120668e4b870b44_Out_2_Vector3, (_Property_dccedc571d6d411291da660d59f31596_Out_0_Float.xxx), _Multiply_c28f28e86afd43f584c04c39cc44c8c4_Out_2_Vector3);
float _Property_f13be210d8b049a9a1e6c0e219f5a53f_Out_0_Float = _Coverage_Normal_Mask_Slope;
float _Property_1f786f31d0924f6ba0904b90319c56e3_Out_0_Float = _Coverage_Normal_Mask_Gradience;
float _Add_4177ca63c9724385910d37f97d0e8c48_Out_2_Float;
Unity_Add_float(_Property_f13be210d8b049a9a1e6c0e219f5a53f_Out_0_Float, _Property_1f786f31d0924f6ba0904b90319c56e3_Out_0_Float, _Add_4177ca63c9724385910d37f97d0e8c48_Out_2_Float);
UnityTexture2D _Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_1_Normal_Map, sampler_Layer_1_Normal_Map, _Layer_1_Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D.tex, _Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D.samplerstate, _Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D.GetTransformedUV((_Multiply_94cb4adbcc9a4b25980974e6be235287_Out_2_Vector3.xy)) );
if (_Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4, _Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4);
float _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_R_4_Float = _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.r;
float _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_G_5_Float = _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.g;
float _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_B_6_Float = _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.b;
float _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_A_7_Float = _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.a;
float _Property_7cfa9a8e94684810a4cd4c70a2257b6c_Out_0_Float = _Layer_1_Normal_Strength;
float3 _NormalStrength_9428e53c24e74ce189625604c4b60275_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.xyz), _Property_7cfa9a8e94684810a4cd4c70a2257b6c_Out_0_Float, _NormalStrength_9428e53c24e74ce189625604c4b60275_Out_2_Vector3);
UnityTexture2D _Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_2_Normal_Map, sampler_Layer_2_Normal_Map, _Layer_2_Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D.tex, _Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D.samplerstate, _Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D.GetTransformedUV((_Multiply_7719c519f67d4b1aaaf2cca1cc2d4d79_Out_2_Vector3.xy)) );
if (_Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4, _Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4);
float _SampleTexture2D_ae5b372472874ad28eac881821ce553c_R_4_Float = _SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.r;
float _SampleTexture2D_ae5b372472874ad28eac881821ce553c_G_5_Float = _SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.g;
float _SampleTexture2D_ae5b372472874ad28eac881821ce553c_B_6_Float = _SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.b;
float _SampleTexture2D_ae5b372472874ad28eac881821ce553c_A_7_Float = _SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.a;
float _Property_12c65401b09f44daa763974826741097_Out_0_Float = _Layer_2_Normal_Strength;
float3 _NormalStrength_93ad91a06836481ca4f36c947e62a393_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.xyz), _Property_12c65401b09f44daa763974826741097_Out_0_Float, _NormalStrength_93ad91a06836481ca4f36c947e62a393_Out_2_Vector3);
float3 _Lerp_7325f1720917490cb13328e58ddd8efe_Out_3_Vector3;
Unity_Lerp_float3(_NormalStrength_9428e53c24e74ce189625604c4b60275_Out_2_Vector3, _NormalStrength_93ad91a06836481ca4f36c947e62a393_Out_2_Vector3, (_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_G_5_Float.xxx), _Lerp_7325f1720917490cb13328e58ddd8efe_Out_3_Vector3);
UnityTexture2D _Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_3_Normal_Map, sampler_Layer_3_Normal_Map, _Layer_3_Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D.tex, _Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D.samplerstate, _Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D.GetTransformedUV((_Multiply_04b9ab405b2d4c27b6dd0696d9a5eacf_Out_2_Vector3.xy)) );
if (_Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4, _Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4);
float _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_R_4_Float = _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.r;
float _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_G_5_Float = _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.g;
float _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_B_6_Float = _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.b;
float _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_A_7_Float = _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.a;
float _Property_02480e6b3805449397b2d5a15e416673_Out_0_Float = _Layer_3_Normal_Strength;
float3 _NormalStrength_8e30a22d9ed147ca9ef200087a7f43aa_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.xyz), _Property_02480e6b3805449397b2d5a15e416673_Out_0_Float, _NormalStrength_8e30a22d9ed147ca9ef200087a7f43aa_Out_2_Vector3);
float3 _Lerp_642d324ee8d74cf888dc943377359b6d_Out_3_Vector3;
Unity_Lerp_float3(_Lerp_7325f1720917490cb13328e58ddd8efe_Out_3_Vector3, _NormalStrength_8e30a22d9ed147ca9ef200087a7f43aa_Out_2_Vector3, (_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_B_6_Float.xxx), _Lerp_642d324ee8d74cf888dc943377359b6d_Out_3_Vector3);
#if defined(_USE_LAYER_3)
float3 _UseLayer3_3e504be39f154559868343947f457c24_Out_0_Vector3 = _Lerp_642d324ee8d74cf888dc943377359b6d_Out_3_Vector3;
#else
float3 _UseLayer3_3e504be39f154559868343947f457c24_Out_0_Vector3 = _Lerp_7325f1720917490cb13328e58ddd8efe_Out_3_Vector3;
#endif
#if defined(_USE_LAYER_2)
float3 _UseLayer2_c90f92a0b0134057867d0810829f779e_Out_0_Vector3 = _UseLayer3_3e504be39f154559868343947f457c24_Out_0_Vector3;
#else
float3 _UseLayer2_c90f92a0b0134057867d0810829f779e_Out_0_Vector3 = _NormalStrength_9428e53c24e74ce189625604c4b60275_Out_2_Vector3;
#endif
UnityTexture2D _Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Normal_Map, sampler_Normal_Map, _Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D.tex, _Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D.samplerstate, _Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4, _Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4);
float _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_R_4_Float = _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.r;
float _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_G_5_Float = _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.g;
float _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_B_6_Float = _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.b;
float _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_A_7_Float = _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.a;
float _Property_758b3d19a0c14ff099921db6234664b2_Out_0_Float = _Normal_Strength;
float3 _NormalStrength_3eb407934b854455a959c26453776991_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.xyz), _Property_758b3d19a0c14ff099921db6234664b2_Out_0_Float, _NormalStrength_3eb407934b854455a959c26453776991_Out_2_Vector3);
float3 _NormalBlend_cb4851ae94454d539b1004bdc1f2604f_Out_2_Vector3;
Unity_NormalBlend_float(_UseLayer2_c90f92a0b0134057867d0810829f779e_Out_0_Vector3, _NormalStrength_3eb407934b854455a959c26453776991_Out_2_Vector3, _NormalBlend_cb4851ae94454d539b1004bdc1f2604f_Out_2_Vector3);
#if defined(_USE_LAYER_1)
float3 _UseLayer1_7da71e644bf2434f99e8005bad9e8f14_Out_0_Vector3 = _NormalBlend_cb4851ae94454d539b1004bdc1f2604f_Out_2_Vector3;
#else
float3 _UseLayer1_7da71e644bf2434f99e8005bad9e8f14_Out_0_Vector3 = _NormalStrength_3eb407934b854455a959c26453776991_Out_2_Vector3;
#endif
float3 _Transform_04398ac1bc954fb19c62ce3f5f6d670c_Out_1_Vector3;
{
float3x3 tangentTransform = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
_Transform_04398ac1bc954fb19c62ce3f5f6d670c_Out_1_Vector3 = TransformTangentToWorld(_UseLayer1_7da71e644bf2434f99e8005bad9e8f14_Out_0_Vector3.xyz, tangentTransform, true);
}
float _DotProduct_ae13069c870e4539be3826642299bede_Out_2_Float;
Unity_DotProduct_float3(_Transform_04398ac1bc954fb19c62ce3f5f6d670c_Out_1_Vector3, float3(0, 1, 0), _DotProduct_ae13069c870e4539be3826642299bede_Out_2_Float);
float _Smoothstep_652b53e75f9e481e8c829d3bd3d9c24e_Out_3_Float;
Unity_Smoothstep_float(_Property_f13be210d8b049a9a1e6c0e219f5a53f_Out_0_Float, _Add_4177ca63c9724385910d37f97d0e8c48_Out_2_Float, _DotProduct_ae13069c870e4539be3826642299bede_Out_2_Float, _Smoothstep_652b53e75f9e481e8c829d3bd3d9c24e_Out_3_Float);
float _Property_64938267e5134c5b9a4256d11aa25705_Out_0_Float = _Coverage_Height_Mask_Pivot_Offset;
float _Property_cfcce6f026924b048f8c33bdc73c14f6_Out_0_Float = _Coverage_Height_Mask_Length;
Bindings_MFPivotVerticalGradience_c9e909c1845b8ae4b9a11aa715963787_float _MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c;
_MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c.WorldSpacePosition = IN.WorldSpacePosition;
float _MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c_Mask_1_Float;
SG_MFPivotVerticalGradience_c9e909c1845b8ae4b9a11aa715963787_float(_Property_64938267e5134c5b9a4256d11aa25705_Out_0_Float, _Property_cfcce6f026924b048f8c33bdc73c14f6_Out_0_Float, _MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c, _MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c_Mask_1_Float);
float _Multiply_f6a21f2d15b7472e8173002af669cfdf_Out_2_Float;
Unity_Multiply_float_float(_Smoothstep_652b53e75f9e481e8c829d3bd3d9c24e_Out_3_Float, _MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c_Mask_1_Float, _Multiply_f6a21f2d15b7472e8173002af669cfdf_Out_2_Float);
float3 _Lerp_90a44aeed76b4e67b245ea63960c28fc_Out_3_Vector3;
Unity_Lerp_float3(_Blend_e613dc790cd741d3b367cb48289cffd5_Out_2_Vector3, _Multiply_c28f28e86afd43f584c04c39cc44c8c4_Out_2_Vector3, (_Multiply_f6a21f2d15b7472e8173002af669cfdf_Out_2_Float.xxx), _Lerp_90a44aeed76b4e67b245ea63960c28fc_Out_3_Vector3);
#if defined(_USE_COVERAGE_LAYER)
float3 _UseCoverageLayer_402efe74563f417bb22b7997bdfc5c1e_Out_0_Vector3 = _Lerp_90a44aeed76b4e67b245ea63960c28fc_Out_3_Vector3;
#else
float3 _UseCoverageLayer_402efe74563f417bb22b7997bdfc5c1e_Out_0_Vector3 = _Blend_e613dc790cd741d3b367cb48289cffd5_Out_2_Vector3;
#endif
UnityTexture2D _Property_022d04960c1e41229b03a237b2bf022a_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Coverage_Normal_Map, sampler_Coverage_Normal_Map, _Coverage_Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_022d04960c1e41229b03a237b2bf022a_Out_0_Texture2D.tex, _Property_022d04960c1e41229b03a237b2bf022a_Out_0_Texture2D.samplerstate, _Property_022d04960c1e41229b03a237b2bf022a_Out_0_Texture2D.GetTransformedUV((_Multiply_282d032113614cb58779c3a33671ad6c_Out_2_Vector3.xy)) );
if (_Property_022d04960c1e41229b03a237b2bf022a_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4, _Property_022d04960c1e41229b03a237b2bf022a_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4);
float _SampleTexture2D_046783cfeeed4edebbc5805118c09db7_R_4_Float = _SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4.r;
float _SampleTexture2D_046783cfeeed4edebbc5805118c09db7_G_5_Float = _SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4.g;
float _SampleTexture2D_046783cfeeed4edebbc5805118c09db7_B_6_Float = _SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4.b;
float _SampleTexture2D_046783cfeeed4edebbc5805118c09db7_A_7_Float = _SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4.a;
float _Property_aff65d79d743476f9e681da55383046d_Out_0_Float = _Coverage_Normal_Strength;
float3 _NormalStrength_f478d3c35fc741e78611a44ac09caa44_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4.xyz), _Property_aff65d79d743476f9e681da55383046d_Out_0_Float, _NormalStrength_f478d3c35fc741e78611a44ac09caa44_Out_2_Vector3);
float3 _Lerp_0e62040e3ecb4bf3b09d8424f7147818_Out_3_Vector3;
Unity_Lerp_float3(float3(0, 0, 1), _NormalStrength_f478d3c35fc741e78611a44ac09caa44_Out_2_Vector3, (_Multiply_f6a21f2d15b7472e8173002af669cfdf_Out_2_Float.xxx), _Lerp_0e62040e3ecb4bf3b09d8424f7147818_Out_3_Vector3);
float3 _NormalBlend_4ffbea1bfb5d43979177bcfb52b4abcf_Out_2_Vector3;
Unity_NormalBlend_float(_Lerp_0e62040e3ecb4bf3b09d8424f7147818_Out_3_Vector3, _UseLayer1_7da71e644bf2434f99e8005bad9e8f14_Out_0_Vector3, _NormalBlend_4ffbea1bfb5d43979177bcfb52b4abcf_Out_2_Vector3);
#if defined(_USE_COVERAGE_LAYER)
float3 _UseCoverageLayer_b0f12be34bf24ec7ba2137cef5a254cb_Out_0_Vector3 = _NormalBlend_4ffbea1bfb5d43979177bcfb52b4abcf_Out_2_Vector3;
#else
float3 _UseCoverageLayer_b0f12be34bf24ec7ba2137cef5a254cb_Out_0_Vector3 = _UseLayer1_7da71e644bf2434f99e8005bad9e8f14_Out_0_Vector3;
#endif
float _OneMinus_7a2b995f5d6a4a619d8a66eddf498446_Out_1_Float;
Unity_OneMinus_float(_Split_03d41d27a027425fa76a6c910dfbed96_A_4_Float, _OneMinus_7a2b995f5d6a4a619d8a66eddf498446_Out_1_Float);
float _Property_3f3b47f88ff24548ba31a6777c3b2a85_Out_0_Float = _Layer_1_Smoothness_Strength;
float _Lerp_f0ccf02f1b3a423eb14231584191b608_Out_3_Float;
Unity_Lerp_float(float(0), _OneMinus_7a2b995f5d6a4a619d8a66eddf498446_Out_1_Float, _Property_3f3b47f88ff24548ba31a6777c3b2a85_Out_0_Float, _Lerp_f0ccf02f1b3a423eb14231584191b608_Out_3_Float);
float _OneMinus_16665b1602fc4acaa17e0caa00dd6ac7_Out_1_Float;
Unity_OneMinus_float(_Split_d38832e5c62c44d3a795b83acceab016_A_4_Float, _OneMinus_16665b1602fc4acaa17e0caa00dd6ac7_Out_1_Float);
float _Property_3bfd1b8cbe534165807a6d38b20b8b3f_Out_0_Float = _Layer_2_Smoothness_Strength;
float _Lerp_5f73c06703364fa1b14c25a300968493_Out_3_Float;
Unity_Lerp_float(float(0), _OneMinus_16665b1602fc4acaa17e0caa00dd6ac7_Out_1_Float, _Property_3bfd1b8cbe534165807a6d38b20b8b3f_Out_0_Float, _Lerp_5f73c06703364fa1b14c25a300968493_Out_3_Float);
float _Lerp_b5d123a1ad4c4307979f2143cfd5d3ac_Out_3_Float;
Unity_Lerp_float(_Lerp_f0ccf02f1b3a423eb14231584191b608_Out_3_Float, _Lerp_5f73c06703364fa1b14c25a300968493_Out_3_Float, _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_G_5_Float, _Lerp_b5d123a1ad4c4307979f2143cfd5d3ac_Out_3_Float);
float _OneMinus_afad5b1133a04d90b4af7d2e859ee909_Out_1_Float;
Unity_OneMinus_float(_Split_838946d3ce6f42cfbaf41fff836d6985_A_4_Float, _OneMinus_afad5b1133a04d90b4af7d2e859ee909_Out_1_Float);
float _Property_269daba898014930a8cc45eae3b32a0d_Out_0_Float = _Layer_3_Smoothness_Strength;
float _Lerp_73575fb5baba4126bfd6048130db9abc_Out_3_Float;
Unity_Lerp_float(float(0), _OneMinus_afad5b1133a04d90b4af7d2e859ee909_Out_1_Float, _Property_269daba898014930a8cc45eae3b32a0d_Out_0_Float, _Lerp_73575fb5baba4126bfd6048130db9abc_Out_3_Float);
float _Lerp_0ed87b2c815a4b6bb1c07d0ba2ad7f9e_Out_3_Float;
Unity_Lerp_float(_Lerp_b5d123a1ad4c4307979f2143cfd5d3ac_Out_3_Float, _Lerp_73575fb5baba4126bfd6048130db9abc_Out_3_Float, _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_B_6_Float, _Lerp_0ed87b2c815a4b6bb1c07d0ba2ad7f9e_Out_3_Float);
#if defined(_USE_LAYER_3)
float _UseLayer3_38584d454ac2471687bb98edbed983aa_Out_0_Float = _Lerp_0ed87b2c815a4b6bb1c07d0ba2ad7f9e_Out_3_Float;
#else
float _UseLayer3_38584d454ac2471687bb98edbed983aa_Out_0_Float = _Lerp_b5d123a1ad4c4307979f2143cfd5d3ac_Out_3_Float;
#endif
#if defined(_USE_LAYER_2)
float _UseLayer2_66f28f7fecc141028a5f15673f047917_Out_0_Float = _UseLayer3_38584d454ac2471687bb98edbed983aa_Out_0_Float;
#else
float _UseLayer2_66f28f7fecc141028a5f15673f047917_Out_0_Float = _Lerp_f0ccf02f1b3a423eb14231584191b608_Out_3_Float;
#endif
#if defined(_USE_LAYER_1)
float _UseLayer1_6040c5b1bc15438dae7a97e54a8e3324_Out_0_Float = _UseLayer2_66f28f7fecc141028a5f15673f047917_Out_0_Float;
#else
float _UseLayer1_6040c5b1bc15438dae7a97e54a8e3324_Out_0_Float = float(0);
#endif
float _OneMinus_f65c9822794443558d97581fc4537919_Out_1_Float;
Unity_OneMinus_float(_Split_42bc736ed19d41c2bc8a58c7f653b802_A_4_Float, _OneMinus_f65c9822794443558d97581fc4537919_Out_1_Float);
float _Property_eb619aeb69fd4914bebabe935f6e2c25_Out_0_Float = _Coverage_Smoothness_Strength;
float _Lerp_2c28e4b743af489e941e7226cc40512b_Out_3_Float;
Unity_Lerp_float(float(0), _OneMinus_f65c9822794443558d97581fc4537919_Out_1_Float, _Property_eb619aeb69fd4914bebabe935f6e2c25_Out_0_Float, _Lerp_2c28e4b743af489e941e7226cc40512b_Out_3_Float);
float _Lerp_1237b11b7e0241aba05ae87b4cdb9167_Out_3_Float;
Unity_Lerp_float(_UseLayer1_6040c5b1bc15438dae7a97e54a8e3324_Out_0_Float, _Lerp_2c28e4b743af489e941e7226cc40512b_Out_3_Float, _Multiply_f6a21f2d15b7472e8173002af669cfdf_Out_2_Float, _Lerp_1237b11b7e0241aba05ae87b4cdb9167_Out_3_Float);
#if defined(_USE_COVERAGE_LAYER)
float _UseCoverageLayer_3e91540d12544fcfa939ef997f0c72e0_Out_0_Float = _Lerp_1237b11b7e0241aba05ae87b4cdb9167_Out_3_Float;
#else
float _UseCoverageLayer_3e91540d12544fcfa939ef997f0c72e0_Out_0_Float = _UseLayer1_6040c5b1bc15438dae7a97e54a8e3324_Out_0_Float;
#endif
surface.BaseColor = _UseCoverageLayer_402efe74563f417bb22b7997bdfc5c1e_Out_0_Vector3;
surface.NormalTS = _UseCoverageLayer_b0f12be34bf24ec7ba2137cef5a254cb_Out_0_Vector3;
surface.Emission = float3(0, 0, 0);
surface.Metallic = float(0);
surface.Smoothness = _UseCoverageLayer_3e91540d12544fcfa939ef997f0c72e0_Out_0_Float;
surface.Occlusion = float(1);
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
    output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);

    // to pr               eserve mikktspace compliance we use same scale renormFactor as was used on the normal.
    // This                is explained in section 2.2 in "surface gradient based bump mapping framework"
    output.WorldSpaceTangent = renormFactor * input.tangentWS.xyz;
    output.WorldSpaceBiTangent = renormFactor * bitang;

    output.WorldSpacePosition = input.positionWS;

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
#pragma shader_feature_local _ _USE_LAYER_2
#pragma shader_feature_local _ _USE_LAYER_3
#pragma shader_feature_local _ _USE_COVERAGE_LAYER
#pragma shader_feature_local _ _USE_LAYER_1

#if defined(_USE_LAYER_2) && defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_3) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_2
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_3)
#define KEYWORD_PERMUTATION_3
#elif defined(_USE_LAYER_2) && defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_4
#elif defined(_USE_LAYER_2) && defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_5
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_6
#elif defined(_USE_LAYER_2)
#define KEYWORD_PERMUTATION_7
#elif defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_8
#elif defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_9
#elif defined(_USE_LAYER_3) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_10
#elif defined(_USE_LAYER_3)
#define KEYWORD_PERMUTATION_11
#elif defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_12
#elif defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_13
#elif defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_14
#else
#define KEYWORD_PERMUTATION_15
#endif


// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
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

float4 _Layer_1_Color_Map_TexelSize;
float _Layer_1_Hue;
float _Layer_1_Brightness;
float _Coverage_Height_Mask_Length;
float4 _Layer_1_Normal_Map_TexelSize;
float _Layer_1_Normal_Strength;
float _Layer_1_Tiling;
float _Layer_1_Smoothness_Strength;
float _Layer_2_Tiling;
float4 _Layer_2_Color_Map_TexelSize;
float _Layer_2_Hue;
float _Layer_2_Brightness;
float _Layer_2_Smoothness_Strength;
float4 _Layer_2_Normal_Map_TexelSize;
float _Layer_2_Normal_Strength;
float _Layer_3_Tiling;
float4 _Layer_3_Color_Map_TexelSize;
float _Layer_3_Hue;
float _Layer_3_Brightness;
float _Layer_3_Smoothness_Strength;
float4 _Layer_3_Normal_Map_TexelSize;
float _Layer_3_Normal_Strength;
float4 _RGB_Map_TexelSize;
float4 _Normal_Map_TexelSize;
float _Normal_Strength;
float _Coverage_Tiling;
float4 _Coverage_Color_Map_TexelSize;
float _Coverage_Hue;
float _Coverage_Brightness;
float _Coverage_Smoothness_Strength;
float4 _Coverage_Normal_Map_TexelSize;
float _Coverage_Normal_Strength;
float4 _Edge_Map_TexelSize;
float _Dirt_Mask_Contrast;
float _AO_Mask_Contrast;
float _Edgewear_Mask_Contrast;
float _Stylization_Mask_Contrast;
float4 _Stylization_Color;
float4 _AO_Color;
float4 _Dirt_Color;
float4 _Edgewear_Color;
float _Coverage_Normal_Mask_Slope;
float _Coverage_Normal_Mask_Gradience;
float _Coverage_Height_Mask_Pivot_Offset;
float _Scale_UVs_with_Object;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Layer_1_Color_Map);
SAMPLER(sampler_Layer_1_Color_Map);
TEXTURE2D(_Layer_1_Normal_Map);
SAMPLER(sampler_Layer_1_Normal_Map);
TEXTURE2D(_Layer_2_Color_Map);
SAMPLER(sampler_Layer_2_Color_Map);
TEXTURE2D(_Layer_2_Normal_Map);
SAMPLER(sampler_Layer_2_Normal_Map);
TEXTURE2D(_Layer_3_Color_Map);
SAMPLER(sampler_Layer_3_Color_Map);
TEXTURE2D(_Layer_3_Normal_Map);
SAMPLER(sampler_Layer_3_Normal_Map);
TEXTURE2D(_RGB_Map);
SAMPLER(sampler_RGB_Map);
TEXTURE2D(_Normal_Map);
SAMPLER(sampler_Normal_Map);
TEXTURE2D(_Coverage_Color_Map);
SAMPLER(sampler_Coverage_Color_Map);
TEXTURE2D(_Coverage_Normal_Map);
SAMPLER(sampler_Coverage_Normal_Map);
TEXTURE2D(_Edge_Map);
SAMPLER(sampler_Edge_Map);

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
#pragma shader_feature_local _ _USE_LAYER_2
#pragma shader_feature_local _ _USE_LAYER_3
#pragma shader_feature_local _ _USE_COVERAGE_LAYER
#pragma shader_feature_local _ _USE_LAYER_1

#if defined(_USE_LAYER_2) && defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_3) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_2
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_3)
#define KEYWORD_PERMUTATION_3
#elif defined(_USE_LAYER_2) && defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_4
#elif defined(_USE_LAYER_2) && defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_5
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_6
#elif defined(_USE_LAYER_2)
#define KEYWORD_PERMUTATION_7
#elif defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_8
#elif defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_9
#elif defined(_USE_LAYER_3) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_10
#elif defined(_USE_LAYER_3)
#define KEYWORD_PERMUTATION_11
#elif defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_12
#elif defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_13
#elif defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_14
#else
#define KEYWORD_PERMUTATION_15
#endif


// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
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

float4 _Layer_1_Color_Map_TexelSize;
float _Layer_1_Hue;
float _Layer_1_Brightness;
float _Coverage_Height_Mask_Length;
float4 _Layer_1_Normal_Map_TexelSize;
float _Layer_1_Normal_Strength;
float _Layer_1_Tiling;
float _Layer_1_Smoothness_Strength;
float _Layer_2_Tiling;
float4 _Layer_2_Color_Map_TexelSize;
float _Layer_2_Hue;
float _Layer_2_Brightness;
float _Layer_2_Smoothness_Strength;
float4 _Layer_2_Normal_Map_TexelSize;
float _Layer_2_Normal_Strength;
float _Layer_3_Tiling;
float4 _Layer_3_Color_Map_TexelSize;
float _Layer_3_Hue;
float _Layer_3_Brightness;
float _Layer_3_Smoothness_Strength;
float4 _Layer_3_Normal_Map_TexelSize;
float _Layer_3_Normal_Strength;
float4 _RGB_Map_TexelSize;
float4 _Normal_Map_TexelSize;
float _Normal_Strength;
float _Coverage_Tiling;
float4 _Coverage_Color_Map_TexelSize;
float _Coverage_Hue;
float _Coverage_Brightness;
float _Coverage_Smoothness_Strength;
float4 _Coverage_Normal_Map_TexelSize;
float _Coverage_Normal_Strength;
float4 _Edge_Map_TexelSize;
float _Dirt_Mask_Contrast;
float _AO_Mask_Contrast;
float _Edgewear_Mask_Contrast;
float _Stylization_Mask_Contrast;
float4 _Stylization_Color;
float4 _AO_Color;
float4 _Dirt_Color;
float4 _Edgewear_Color;
float _Coverage_Normal_Mask_Slope;
float _Coverage_Normal_Mask_Gradience;
float _Coverage_Height_Mask_Pivot_Offset;
float _Scale_UVs_with_Object;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Layer_1_Color_Map);
SAMPLER(sampler_Layer_1_Color_Map);
TEXTURE2D(_Layer_1_Normal_Map);
SAMPLER(sampler_Layer_1_Normal_Map);
TEXTURE2D(_Layer_2_Color_Map);
SAMPLER(sampler_Layer_2_Color_Map);
TEXTURE2D(_Layer_2_Normal_Map);
SAMPLER(sampler_Layer_2_Normal_Map);
TEXTURE2D(_Layer_3_Color_Map);
SAMPLER(sampler_Layer_3_Color_Map);
TEXTURE2D(_Layer_3_Normal_Map);
SAMPLER(sampler_Layer_3_Normal_Map);
TEXTURE2D(_RGB_Map);
SAMPLER(sampler_RGB_Map);
TEXTURE2D(_Normal_Map);
SAMPLER(sampler_Normal_Map);
TEXTURE2D(_Coverage_Color_Map);
SAMPLER(sampler_Coverage_Color_Map);
TEXTURE2D(_Coverage_Normal_Map);
SAMPLER(sampler_Coverage_Normal_Map);
TEXTURE2D(_Edge_Map);
SAMPLER(sampler_Edge_Map);

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
#pragma shader_feature_local _ _USE_LAYER_2
#pragma shader_feature_local _ _USE_LAYER_3
#pragma shader_feature_local _ _USE_COVERAGE_LAYER
#pragma shader_feature_local _ _USE_LAYER_1

#if defined(_USE_LAYER_2) && defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_3) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_2
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_3)
#define KEYWORD_PERMUTATION_3
#elif defined(_USE_LAYER_2) && defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_4
#elif defined(_USE_LAYER_2) && defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_5
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_6
#elif defined(_USE_LAYER_2)
#define KEYWORD_PERMUTATION_7
#elif defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_8
#elif defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_9
#elif defined(_USE_LAYER_3) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_10
#elif defined(_USE_LAYER_3)
#define KEYWORD_PERMUTATION_11
#elif defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_12
#elif defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_13
#elif defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_14
#else
#define KEYWORD_PERMUTATION_15
#endif


// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
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

float4 _Layer_1_Color_Map_TexelSize;
float _Layer_1_Hue;
float _Layer_1_Brightness;
float _Coverage_Height_Mask_Length;
float4 _Layer_1_Normal_Map_TexelSize;
float _Layer_1_Normal_Strength;
float _Layer_1_Tiling;
float _Layer_1_Smoothness_Strength;
float _Layer_2_Tiling;
float4 _Layer_2_Color_Map_TexelSize;
float _Layer_2_Hue;
float _Layer_2_Brightness;
float _Layer_2_Smoothness_Strength;
float4 _Layer_2_Normal_Map_TexelSize;
float _Layer_2_Normal_Strength;
float _Layer_3_Tiling;
float4 _Layer_3_Color_Map_TexelSize;
float _Layer_3_Hue;
float _Layer_3_Brightness;
float _Layer_3_Smoothness_Strength;
float4 _Layer_3_Normal_Map_TexelSize;
float _Layer_3_Normal_Strength;
float4 _RGB_Map_TexelSize;
float4 _Normal_Map_TexelSize;
float _Normal_Strength;
float _Coverage_Tiling;
float4 _Coverage_Color_Map_TexelSize;
float _Coverage_Hue;
float _Coverage_Brightness;
float _Coverage_Smoothness_Strength;
float4 _Coverage_Normal_Map_TexelSize;
float _Coverage_Normal_Strength;
float4 _Edge_Map_TexelSize;
float _Dirt_Mask_Contrast;
float _AO_Mask_Contrast;
float _Edgewear_Mask_Contrast;
float _Stylization_Mask_Contrast;
float4 _Stylization_Color;
float4 _AO_Color;
float4 _Dirt_Color;
float4 _Edgewear_Color;
float _Coverage_Normal_Mask_Slope;
float _Coverage_Normal_Mask_Gradience;
float _Coverage_Height_Mask_Pivot_Offset;
float _Scale_UVs_with_Object;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Layer_1_Color_Map);
SAMPLER(sampler_Layer_1_Color_Map);
TEXTURE2D(_Layer_1_Normal_Map);
SAMPLER(sampler_Layer_1_Normal_Map);
TEXTURE2D(_Layer_2_Color_Map);
SAMPLER(sampler_Layer_2_Color_Map);
TEXTURE2D(_Layer_2_Normal_Map);
SAMPLER(sampler_Layer_2_Normal_Map);
TEXTURE2D(_Layer_3_Color_Map);
SAMPLER(sampler_Layer_3_Color_Map);
TEXTURE2D(_Layer_3_Normal_Map);
SAMPLER(sampler_Layer_3_Normal_Map);
TEXTURE2D(_RGB_Map);
SAMPLER(sampler_RGB_Map);
TEXTURE2D(_Normal_Map);
SAMPLER(sampler_Normal_Map);
TEXTURE2D(_Coverage_Color_Map);
SAMPLER(sampler_Coverage_Color_Map);
TEXTURE2D(_Coverage_Normal_Map);
SAMPLER(sampler_Coverage_Normal_Map);
TEXTURE2D(_Edge_Map);
SAMPLER(sampler_Edge_Map);

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
#pragma shader_feature_local _ _USE_LAYER_2
#pragma shader_feature_local _ _USE_LAYER_3
#pragma shader_feature_local _ _USE_COVERAGE_LAYER
#pragma shader_feature_local _ _USE_LAYER_1

#if defined(_USE_LAYER_2) && defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_3) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_2
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_3)
#define KEYWORD_PERMUTATION_3
#elif defined(_USE_LAYER_2) && defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_4
#elif defined(_USE_LAYER_2) && defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_5
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_6
#elif defined(_USE_LAYER_2)
#define KEYWORD_PERMUTATION_7
#elif defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_8
#elif defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_9
#elif defined(_USE_LAYER_3) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_10
#elif defined(_USE_LAYER_3)
#define KEYWORD_PERMUTATION_11
#elif defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_12
#elif defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_13
#elif defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_14
#else
#define KEYWORD_PERMUTATION_15
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
 float3 WorldSpaceTangent;
 float3 WorldSpaceBiTangent;
 float3 WorldSpacePosition;
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

float4 _Layer_1_Color_Map_TexelSize;
float _Layer_1_Hue;
float _Layer_1_Brightness;
float _Coverage_Height_Mask_Length;
float4 _Layer_1_Normal_Map_TexelSize;
float _Layer_1_Normal_Strength;
float _Layer_1_Tiling;
float _Layer_1_Smoothness_Strength;
float _Layer_2_Tiling;
float4 _Layer_2_Color_Map_TexelSize;
float _Layer_2_Hue;
float _Layer_2_Brightness;
float _Layer_2_Smoothness_Strength;
float4 _Layer_2_Normal_Map_TexelSize;
float _Layer_2_Normal_Strength;
float _Layer_3_Tiling;
float4 _Layer_3_Color_Map_TexelSize;
float _Layer_3_Hue;
float _Layer_3_Brightness;
float _Layer_3_Smoothness_Strength;
float4 _Layer_3_Normal_Map_TexelSize;
float _Layer_3_Normal_Strength;
float4 _RGB_Map_TexelSize;
float4 _Normal_Map_TexelSize;
float _Normal_Strength;
float _Coverage_Tiling;
float4 _Coverage_Color_Map_TexelSize;
float _Coverage_Hue;
float _Coverage_Brightness;
float _Coverage_Smoothness_Strength;
float4 _Coverage_Normal_Map_TexelSize;
float _Coverage_Normal_Strength;
float4 _Edge_Map_TexelSize;
float _Dirt_Mask_Contrast;
float _AO_Mask_Contrast;
float _Edgewear_Mask_Contrast;
float _Stylization_Mask_Contrast;
float4 _Stylization_Color;
float4 _AO_Color;
float4 _Dirt_Color;
float4 _Edgewear_Color;
float _Coverage_Normal_Mask_Slope;
float _Coverage_Normal_Mask_Gradience;
float _Coverage_Height_Mask_Pivot_Offset;
float _Scale_UVs_with_Object;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Layer_1_Color_Map);
SAMPLER(sampler_Layer_1_Color_Map);
TEXTURE2D(_Layer_1_Normal_Map);
SAMPLER(sampler_Layer_1_Normal_Map);
TEXTURE2D(_Layer_2_Color_Map);
SAMPLER(sampler_Layer_2_Color_Map);
TEXTURE2D(_Layer_2_Normal_Map);
SAMPLER(sampler_Layer_2_Normal_Map);
TEXTURE2D(_Layer_3_Color_Map);
SAMPLER(sampler_Layer_3_Color_Map);
TEXTURE2D(_Layer_3_Normal_Map);
SAMPLER(sampler_Layer_3_Normal_Map);
TEXTURE2D(_RGB_Map);
SAMPLER(sampler_RGB_Map);
TEXTURE2D(_Normal_Map);
SAMPLER(sampler_Normal_Map);
TEXTURE2D(_Coverage_Color_Map);
SAMPLER(sampler_Coverage_Color_Map);
TEXTURE2D(_Coverage_Normal_Map);
SAMPLER(sampler_Coverage_Normal_Map);
TEXTURE2D(_Edge_Map);
SAMPLER(sampler_Edge_Map);

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

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
{
    Out = lerp(A, B, T);
}

void Unity_NormalStrength_float(float3 In, float Strength, out float3 Out)
{
    Out = float3(In.rg * Strength, lerp(1, In.b, saturate(Strength)));
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_NormalBlend_float(float3 A, float3 B, out float3 Out)
{
    Out = SafeNormalize(float3(A.rg + B.rg, A.b * B.b));
}

void Unity_DotProduct_float3(float3 A, float3 B, out float Out)
{
    Out = dot(A, B);
}

void Unity_Smoothstep_float(float Edge1, float Edge2, float In, out float Out)
{
    Out = smoothstep(Edge1, Edge2, In);
}

void Unity_Subtract_float3(float3 A, float3 B, out float3 Out)
{
    Out = A - B;
}

struct Bindings_MFPivotVerticalGradience_c9e909c1845b8ae4b9a11aa715963787_float
{
float3 WorldSpacePosition;
};

void SG_MFPivotVerticalGradience_c9e909c1845b8ae4b9a11aa715963787_float(float _Offset, float _Length, Bindings_MFPivotVerticalGradience_c9e909c1845b8ae4b9a11aa715963787_float IN, out float Mask_1)
{
float _Property_4680dca2dee245e7aa77915a0b17cce9_Out_0_Float = _Offset;
float _Property_c763914b9bbb4da5abad7a3db51adcb2_Out_0_Float = _Length;
float _Add_ad1531c9af874822b29fd9a13421289e_Out_2_Float;
Unity_Add_float(_Property_4680dca2dee245e7aa77915a0b17cce9_Out_0_Float, _Property_c763914b9bbb4da5abad7a3db51adcb2_Out_0_Float, _Add_ad1531c9af874822b29fd9a13421289e_Out_2_Float);
float3 _Subtract_69e80d3fb1bb4d71b4be84a010b280ff_Out_2_Vector3;
Unity_Subtract_float3(IN.WorldSpacePosition, SHADERGRAPH_OBJECT_POSITION, _Subtract_69e80d3fb1bb4d71b4be84a010b280ff_Out_2_Vector3);
float _Split_bceff54ddef34fab8cfa9a2a0b8bbc58_R_1_Float = _Subtract_69e80d3fb1bb4d71b4be84a010b280ff_Out_2_Vector3[0];
float _Split_bceff54ddef34fab8cfa9a2a0b8bbc58_G_2_Float = _Subtract_69e80d3fb1bb4d71b4be84a010b280ff_Out_2_Vector3[1];
float _Split_bceff54ddef34fab8cfa9a2a0b8bbc58_B_3_Float = _Subtract_69e80d3fb1bb4d71b4be84a010b280ff_Out_2_Vector3[2];
float _Split_bceff54ddef34fab8cfa9a2a0b8bbc58_A_4_Float = 0;
float _Smoothstep_0109929c8f9e41888dfa7ee4fd85c377_Out_3_Float;
Unity_Smoothstep_float(_Property_4680dca2dee245e7aa77915a0b17cce9_Out_0_Float, _Add_ad1531c9af874822b29fd9a13421289e_Out_2_Float, _Split_bceff54ddef34fab8cfa9a2a0b8bbc58_G_2_Float, _Smoothstep_0109929c8f9e41888dfa7ee4fd85c377_Out_3_Float);
Mask_1 = _Smoothstep_0109929c8f9e41888dfa7ee4fd85c377_Out_3_Float;
}

void Unity_Multiply_float_float(float A, float B, out float Out)
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
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
UnityTexture2D _Property_022d04960c1e41229b03a237b2bf022a_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Coverage_Normal_Map, sampler_Coverage_Normal_Map, _Coverage_Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_7afe6a7edf62460887e09c4746d796b6_Out_0_Vector4 = IN.uv0;
float3 _Multiply_15b5d23699554ab298d90333ce57d387_Out_2_Vector3;
Unity_Multiply_float3_float3((_UV_7afe6a7edf62460887e09c4746d796b6_Out_0_Vector4.xyz), float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z))), _Multiply_15b5d23699554ab298d90333ce57d387_Out_2_Vector3);
float _Property_9a5b0bf1bf584876a406e2b633741c94_Out_0_Float = _Scale_UVs_with_Object;
float3 _Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3;
Unity_Lerp_float3((_UV_7afe6a7edf62460887e09c4746d796b6_Out_0_Vector4.xyz), _Multiply_15b5d23699554ab298d90333ce57d387_Out_2_Vector3, (_Property_9a5b0bf1bf584876a406e2b633741c94_Out_0_Float.xxx), _Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3);
float _Property_d9f12d2b2464447a82f581c14778bbf8_Out_0_Float = _Coverage_Tiling;
float3 _Multiply_282d032113614cb58779c3a33671ad6c_Out_2_Vector3;
Unity_Multiply_float3_float3(_Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3, (_Property_d9f12d2b2464447a82f581c14778bbf8_Out_0_Float.xxx), _Multiply_282d032113614cb58779c3a33671ad6c_Out_2_Vector3);
float4 _SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_022d04960c1e41229b03a237b2bf022a_Out_0_Texture2D.tex, _Property_022d04960c1e41229b03a237b2bf022a_Out_0_Texture2D.samplerstate, _Property_022d04960c1e41229b03a237b2bf022a_Out_0_Texture2D.GetTransformedUV((_Multiply_282d032113614cb58779c3a33671ad6c_Out_2_Vector3.xy)) );
if (_Property_022d04960c1e41229b03a237b2bf022a_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4, _Property_022d04960c1e41229b03a237b2bf022a_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4);
float _SampleTexture2D_046783cfeeed4edebbc5805118c09db7_R_4_Float = _SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4.r;
float _SampleTexture2D_046783cfeeed4edebbc5805118c09db7_G_5_Float = _SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4.g;
float _SampleTexture2D_046783cfeeed4edebbc5805118c09db7_B_6_Float = _SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4.b;
float _SampleTexture2D_046783cfeeed4edebbc5805118c09db7_A_7_Float = _SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4.a;
float _Property_aff65d79d743476f9e681da55383046d_Out_0_Float = _Coverage_Normal_Strength;
float3 _NormalStrength_f478d3c35fc741e78611a44ac09caa44_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4.xyz), _Property_aff65d79d743476f9e681da55383046d_Out_0_Float, _NormalStrength_f478d3c35fc741e78611a44ac09caa44_Out_2_Vector3);
float _Property_f13be210d8b049a9a1e6c0e219f5a53f_Out_0_Float = _Coverage_Normal_Mask_Slope;
float _Property_1f786f31d0924f6ba0904b90319c56e3_Out_0_Float = _Coverage_Normal_Mask_Gradience;
float _Add_4177ca63c9724385910d37f97d0e8c48_Out_2_Float;
Unity_Add_float(_Property_f13be210d8b049a9a1e6c0e219f5a53f_Out_0_Float, _Property_1f786f31d0924f6ba0904b90319c56e3_Out_0_Float, _Add_4177ca63c9724385910d37f97d0e8c48_Out_2_Float);
UnityTexture2D _Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_1_Normal_Map, sampler_Layer_1_Normal_Map, _Layer_1_Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_460238447f6b4170aa7397df1262be96_Out_0_Float = _Layer_1_Tiling;
float3 _Multiply_94cb4adbcc9a4b25980974e6be235287_Out_2_Vector3;
Unity_Multiply_float3_float3(_Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3, (_Property_460238447f6b4170aa7397df1262be96_Out_0_Float.xxx), _Multiply_94cb4adbcc9a4b25980974e6be235287_Out_2_Vector3);
float4 _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D.tex, _Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D.samplerstate, _Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D.GetTransformedUV((_Multiply_94cb4adbcc9a4b25980974e6be235287_Out_2_Vector3.xy)) );
if (_Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4, _Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4);
float _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_R_4_Float = _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.r;
float _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_G_5_Float = _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.g;
float _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_B_6_Float = _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.b;
float _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_A_7_Float = _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.a;
float _Property_7cfa9a8e94684810a4cd4c70a2257b6c_Out_0_Float = _Layer_1_Normal_Strength;
float3 _NormalStrength_9428e53c24e74ce189625604c4b60275_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.xyz), _Property_7cfa9a8e94684810a4cd4c70a2257b6c_Out_0_Float, _NormalStrength_9428e53c24e74ce189625604c4b60275_Out_2_Vector3);
UnityTexture2D _Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_2_Normal_Map, sampler_Layer_2_Normal_Map, _Layer_2_Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_812f9793e99740fcb534b8c8d8af0ff2_Out_0_Float = _Layer_2_Tiling;
float3 _Multiply_7719c519f67d4b1aaaf2cca1cc2d4d79_Out_2_Vector3;
Unity_Multiply_float3_float3(_Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3, (_Property_812f9793e99740fcb534b8c8d8af0ff2_Out_0_Float.xxx), _Multiply_7719c519f67d4b1aaaf2cca1cc2d4d79_Out_2_Vector3);
float4 _SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D.tex, _Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D.samplerstate, _Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D.GetTransformedUV((_Multiply_7719c519f67d4b1aaaf2cca1cc2d4d79_Out_2_Vector3.xy)) );
if (_Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4, _Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4);
float _SampleTexture2D_ae5b372472874ad28eac881821ce553c_R_4_Float = _SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.r;
float _SampleTexture2D_ae5b372472874ad28eac881821ce553c_G_5_Float = _SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.g;
float _SampleTexture2D_ae5b372472874ad28eac881821ce553c_B_6_Float = _SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.b;
float _SampleTexture2D_ae5b372472874ad28eac881821ce553c_A_7_Float = _SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.a;
float _Property_12c65401b09f44daa763974826741097_Out_0_Float = _Layer_2_Normal_Strength;
float3 _NormalStrength_93ad91a06836481ca4f36c947e62a393_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.xyz), _Property_12c65401b09f44daa763974826741097_Out_0_Float, _NormalStrength_93ad91a06836481ca4f36c947e62a393_Out_2_Vector3);
UnityTexture2D _Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_RGB_Map, sampler_RGB_Map, _RGB_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D.tex, _Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D.samplerstate, _Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4, _Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_R_4_Float = _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4.r;
float _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_G_5_Float = _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4.g;
float _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_B_6_Float = _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4.b;
float _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_A_7_Float = _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4.a;
float3 _Lerp_7325f1720917490cb13328e58ddd8efe_Out_3_Vector3;
Unity_Lerp_float3(_NormalStrength_9428e53c24e74ce189625604c4b60275_Out_2_Vector3, _NormalStrength_93ad91a06836481ca4f36c947e62a393_Out_2_Vector3, (_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_G_5_Float.xxx), _Lerp_7325f1720917490cb13328e58ddd8efe_Out_3_Vector3);
UnityTexture2D _Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_3_Normal_Map, sampler_Layer_3_Normal_Map, _Layer_3_Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_8d20917a655e4062a5ce52f974969006_Out_0_Float = _Layer_3_Tiling;
float3 _Multiply_04b9ab405b2d4c27b6dd0696d9a5eacf_Out_2_Vector3;
Unity_Multiply_float3_float3(_Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3, (_Property_8d20917a655e4062a5ce52f974969006_Out_0_Float.xxx), _Multiply_04b9ab405b2d4c27b6dd0696d9a5eacf_Out_2_Vector3);
float4 _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D.tex, _Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D.samplerstate, _Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D.GetTransformedUV((_Multiply_04b9ab405b2d4c27b6dd0696d9a5eacf_Out_2_Vector3.xy)) );
if (_Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4, _Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4);
float _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_R_4_Float = _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.r;
float _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_G_5_Float = _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.g;
float _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_B_6_Float = _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.b;
float _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_A_7_Float = _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.a;
float _Property_02480e6b3805449397b2d5a15e416673_Out_0_Float = _Layer_3_Normal_Strength;
float3 _NormalStrength_8e30a22d9ed147ca9ef200087a7f43aa_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.xyz), _Property_02480e6b3805449397b2d5a15e416673_Out_0_Float, _NormalStrength_8e30a22d9ed147ca9ef200087a7f43aa_Out_2_Vector3);
float3 _Lerp_642d324ee8d74cf888dc943377359b6d_Out_3_Vector3;
Unity_Lerp_float3(_Lerp_7325f1720917490cb13328e58ddd8efe_Out_3_Vector3, _NormalStrength_8e30a22d9ed147ca9ef200087a7f43aa_Out_2_Vector3, (_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_B_6_Float.xxx), _Lerp_642d324ee8d74cf888dc943377359b6d_Out_3_Vector3);
#if defined(_USE_LAYER_3)
float3 _UseLayer3_3e504be39f154559868343947f457c24_Out_0_Vector3 = _Lerp_642d324ee8d74cf888dc943377359b6d_Out_3_Vector3;
#else
float3 _UseLayer3_3e504be39f154559868343947f457c24_Out_0_Vector3 = _Lerp_7325f1720917490cb13328e58ddd8efe_Out_3_Vector3;
#endif
#if defined(_USE_LAYER_2)
float3 _UseLayer2_c90f92a0b0134057867d0810829f779e_Out_0_Vector3 = _UseLayer3_3e504be39f154559868343947f457c24_Out_0_Vector3;
#else
float3 _UseLayer2_c90f92a0b0134057867d0810829f779e_Out_0_Vector3 = _NormalStrength_9428e53c24e74ce189625604c4b60275_Out_2_Vector3;
#endif
UnityTexture2D _Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Normal_Map, sampler_Normal_Map, _Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D.tex, _Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D.samplerstate, _Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4, _Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4);
float _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_R_4_Float = _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.r;
float _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_G_5_Float = _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.g;
float _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_B_6_Float = _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.b;
float _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_A_7_Float = _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.a;
float _Property_758b3d19a0c14ff099921db6234664b2_Out_0_Float = _Normal_Strength;
float3 _NormalStrength_3eb407934b854455a959c26453776991_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.xyz), _Property_758b3d19a0c14ff099921db6234664b2_Out_0_Float, _NormalStrength_3eb407934b854455a959c26453776991_Out_2_Vector3);
float3 _NormalBlend_cb4851ae94454d539b1004bdc1f2604f_Out_2_Vector3;
Unity_NormalBlend_float(_UseLayer2_c90f92a0b0134057867d0810829f779e_Out_0_Vector3, _NormalStrength_3eb407934b854455a959c26453776991_Out_2_Vector3, _NormalBlend_cb4851ae94454d539b1004bdc1f2604f_Out_2_Vector3);
#if defined(_USE_LAYER_1)
float3 _UseLayer1_7da71e644bf2434f99e8005bad9e8f14_Out_0_Vector3 = _NormalBlend_cb4851ae94454d539b1004bdc1f2604f_Out_2_Vector3;
#else
float3 _UseLayer1_7da71e644bf2434f99e8005bad9e8f14_Out_0_Vector3 = _NormalStrength_3eb407934b854455a959c26453776991_Out_2_Vector3;
#endif
float3 _Transform_04398ac1bc954fb19c62ce3f5f6d670c_Out_1_Vector3;
{
float3x3 tangentTransform = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
_Transform_04398ac1bc954fb19c62ce3f5f6d670c_Out_1_Vector3 = TransformTangentToWorld(_UseLayer1_7da71e644bf2434f99e8005bad9e8f14_Out_0_Vector3.xyz, tangentTransform, true);
}
float _DotProduct_ae13069c870e4539be3826642299bede_Out_2_Float;
Unity_DotProduct_float3(_Transform_04398ac1bc954fb19c62ce3f5f6d670c_Out_1_Vector3, float3(0, 1, 0), _DotProduct_ae13069c870e4539be3826642299bede_Out_2_Float);
float _Smoothstep_652b53e75f9e481e8c829d3bd3d9c24e_Out_3_Float;
Unity_Smoothstep_float(_Property_f13be210d8b049a9a1e6c0e219f5a53f_Out_0_Float, _Add_4177ca63c9724385910d37f97d0e8c48_Out_2_Float, _DotProduct_ae13069c870e4539be3826642299bede_Out_2_Float, _Smoothstep_652b53e75f9e481e8c829d3bd3d9c24e_Out_3_Float);
float _Property_64938267e5134c5b9a4256d11aa25705_Out_0_Float = _Coverage_Height_Mask_Pivot_Offset;
float _Property_cfcce6f026924b048f8c33bdc73c14f6_Out_0_Float = _Coverage_Height_Mask_Length;
Bindings_MFPivotVerticalGradience_c9e909c1845b8ae4b9a11aa715963787_float _MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c;
_MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c.WorldSpacePosition = IN.WorldSpacePosition;
float _MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c_Mask_1_Float;
SG_MFPivotVerticalGradience_c9e909c1845b8ae4b9a11aa715963787_float(_Property_64938267e5134c5b9a4256d11aa25705_Out_0_Float, _Property_cfcce6f026924b048f8c33bdc73c14f6_Out_0_Float, _MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c, _MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c_Mask_1_Float);
float _Multiply_f6a21f2d15b7472e8173002af669cfdf_Out_2_Float;
Unity_Multiply_float_float(_Smoothstep_652b53e75f9e481e8c829d3bd3d9c24e_Out_3_Float, _MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c_Mask_1_Float, _Multiply_f6a21f2d15b7472e8173002af669cfdf_Out_2_Float);
float3 _Lerp_0e62040e3ecb4bf3b09d8424f7147818_Out_3_Vector3;
Unity_Lerp_float3(float3(0, 0, 1), _NormalStrength_f478d3c35fc741e78611a44ac09caa44_Out_2_Vector3, (_Multiply_f6a21f2d15b7472e8173002af669cfdf_Out_2_Float.xxx), _Lerp_0e62040e3ecb4bf3b09d8424f7147818_Out_3_Vector3);
float3 _NormalBlend_4ffbea1bfb5d43979177bcfb52b4abcf_Out_2_Vector3;
Unity_NormalBlend_float(_Lerp_0e62040e3ecb4bf3b09d8424f7147818_Out_3_Vector3, _UseLayer1_7da71e644bf2434f99e8005bad9e8f14_Out_0_Vector3, _NormalBlend_4ffbea1bfb5d43979177bcfb52b4abcf_Out_2_Vector3);
#if defined(_USE_COVERAGE_LAYER)
float3 _UseCoverageLayer_b0f12be34bf24ec7ba2137cef5a254cb_Out_0_Vector3 = _NormalBlend_4ffbea1bfb5d43979177bcfb52b4abcf_Out_2_Vector3;
#else
float3 _UseCoverageLayer_b0f12be34bf24ec7ba2137cef5a254cb_Out_0_Vector3 = _UseLayer1_7da71e644bf2434f99e8005bad9e8f14_Out_0_Vector3;
#endif
surface.NormalTS = _UseCoverageLayer_b0f12be34bf24ec7ba2137cef5a254cb_Out_0_Vector3;
float3 reactionUnusedColor = 0, reactionUnusedEmission = 0;
ClipVegetationFuel(IN.WorldSpacePosition);
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
    output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);

    // to pr               eserve mikktspace compliance we use same scale renormFactor as was used on the normal.
    // This                is explained in section 2.2 in "surface gradient based bump mapping framework"
    output.WorldSpaceTangent = renormFactor * input.tangentWS.xyz;
    output.WorldSpaceBiTangent = renormFactor * bitang;

    output.WorldSpacePosition = input.positionWS;

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
#pragma shader_feature_local _ _USE_LAYER_2
#pragma shader_feature_local _ _USE_LAYER_3
#pragma shader_feature_local _ _USE_COVERAGE_LAYER
#pragma shader_feature_local _ _USE_LAYER_1

#if defined(_USE_LAYER_2) && defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_3) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_2
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_3)
#define KEYWORD_PERMUTATION_3
#elif defined(_USE_LAYER_2) && defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_4
#elif defined(_USE_LAYER_2) && defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_5
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_6
#elif defined(_USE_LAYER_2)
#define KEYWORD_PERMUTATION_7
#elif defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_8
#elif defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_9
#elif defined(_USE_LAYER_3) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_10
#elif defined(_USE_LAYER_3)
#define KEYWORD_PERMUTATION_11
#elif defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_12
#elif defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_13
#elif defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_14
#else
#define KEYWORD_PERMUTATION_15
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
 float3 WorldSpaceNormal;
 float3 WorldSpaceTangent;
 float3 WorldSpaceBiTangent;
 float3 WorldSpacePosition;
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

float4 _Layer_1_Color_Map_TexelSize;
float _Layer_1_Hue;
float _Layer_1_Brightness;
float _Coverage_Height_Mask_Length;
float4 _Layer_1_Normal_Map_TexelSize;
float _Layer_1_Normal_Strength;
float _Layer_1_Tiling;
float _Layer_1_Smoothness_Strength;
float _Layer_2_Tiling;
float4 _Layer_2_Color_Map_TexelSize;
float _Layer_2_Hue;
float _Layer_2_Brightness;
float _Layer_2_Smoothness_Strength;
float4 _Layer_2_Normal_Map_TexelSize;
float _Layer_2_Normal_Strength;
float _Layer_3_Tiling;
float4 _Layer_3_Color_Map_TexelSize;
float _Layer_3_Hue;
float _Layer_3_Brightness;
float _Layer_3_Smoothness_Strength;
float4 _Layer_3_Normal_Map_TexelSize;
float _Layer_3_Normal_Strength;
float4 _RGB_Map_TexelSize;
float4 _Normal_Map_TexelSize;
float _Normal_Strength;
float _Coverage_Tiling;
float4 _Coverage_Color_Map_TexelSize;
float _Coverage_Hue;
float _Coverage_Brightness;
float _Coverage_Smoothness_Strength;
float4 _Coverage_Normal_Map_TexelSize;
float _Coverage_Normal_Strength;
float4 _Edge_Map_TexelSize;
float _Dirt_Mask_Contrast;
float _AO_Mask_Contrast;
float _Edgewear_Mask_Contrast;
float _Stylization_Mask_Contrast;
float4 _Stylization_Color;
float4 _AO_Color;
float4 _Dirt_Color;
float4 _Edgewear_Color;
float _Coverage_Normal_Mask_Slope;
float _Coverage_Normal_Mask_Gradience;
float _Coverage_Height_Mask_Pivot_Offset;
float _Scale_UVs_with_Object;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Layer_1_Color_Map);
SAMPLER(sampler_Layer_1_Color_Map);
TEXTURE2D(_Layer_1_Normal_Map);
SAMPLER(sampler_Layer_1_Normal_Map);
TEXTURE2D(_Layer_2_Color_Map);
SAMPLER(sampler_Layer_2_Color_Map);
TEXTURE2D(_Layer_2_Normal_Map);
SAMPLER(sampler_Layer_2_Normal_Map);
TEXTURE2D(_Layer_3_Color_Map);
SAMPLER(sampler_Layer_3_Color_Map);
TEXTURE2D(_Layer_3_Normal_Map);
SAMPLER(sampler_Layer_3_Normal_Map);
TEXTURE2D(_RGB_Map);
SAMPLER(sampler_RGB_Map);
TEXTURE2D(_Normal_Map);
SAMPLER(sampler_Normal_Map);
TEXTURE2D(_Coverage_Color_Map);
SAMPLER(sampler_Coverage_Color_Map);
TEXTURE2D(_Coverage_Normal_Map);
SAMPLER(sampler_Coverage_Normal_Map);
TEXTURE2D(_Edge_Map);
SAMPLER(sampler_Edge_Map);

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

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Hue_Normalized_float(float3 In, float Offset, out float3 Out)
{
    // RGB to HSV
    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 P = lerp(float4(In.bg, K.wz), float4(In.gb, K.xy), step(In.b, In.g));
    float4 Q = lerp(float4(P.xyw, In.r), float4(In.r, P.yzx), step(P.x, In.r));
    float D = Q.x - min(Q.w, Q.y);
    float E = 1e-10;
    float V = (D == 0) ? Q.x : (Q.x + E);
    float3 hsv = float3(abs(Q.z + (Q.w - Q.y)/(6.0 * D + E)), D / (Q.x + E), V);

    float hue = hsv.x + Offset;
    hsv.x = (hue < 0)
            ? hue + 1
            : (hue > 1)
                ? hue - 1
                : hue;

    // HSV to RGB
    float4 K2 = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 P2 = abs(frac(hsv.xxx + K2.xyz) * 6.0 - K2.www);
    Out = hsv.z * lerp(K2.xxx, saturate(P2 - K2.xxx), hsv.y);
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Lerp_float(float A, float B, float T, out float Out)
{
    Out = lerp(A, B, T);
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

struct Bindings_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float
{
};

void SG_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float(float _Input, float _Contrast, Bindings_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float IN, out float Out_1)
{
float _Property_f17f2d608b764773bc9f33ea28c3e14e_Out_0_Float = _Contrast;
float _Add_59838b3eb9ec437bbd93b6fd8db7a0c4_Out_2_Float;
Unity_Add_float(_Property_f17f2d608b764773bc9f33ea28c3e14e_Out_0_Float, float(-1), _Add_59838b3eb9ec437bbd93b6fd8db7a0c4_Out_2_Float);
float _Add_524e73a4aa0b4e7982442fdf0d39fe18_Out_2_Float;
Unity_Add_float(_Property_f17f2d608b764773bc9f33ea28c3e14e_Out_0_Float, float(1), _Add_524e73a4aa0b4e7982442fdf0d39fe18_Out_2_Float);
float _Property_df60db08578f4909bad9a81c68245e45_Out_0_Float = _Input;
float _Lerp_6af4dfb2e0f0426a81a1669c55ec8196_Out_3_Float;
Unity_Lerp_float(_Add_59838b3eb9ec437bbd93b6fd8db7a0c4_Out_2_Float, _Add_524e73a4aa0b4e7982442fdf0d39fe18_Out_2_Float, _Property_df60db08578f4909bad9a81c68245e45_Out_0_Float, _Lerp_6af4dfb2e0f0426a81a1669c55ec8196_Out_3_Float);
float _Saturate_59b070c1298d4279be831e68d2cd96c8_Out_1_Float;
Unity_Saturate_float(_Lerp_6af4dfb2e0f0426a81a1669c55ec8196_Out_3_Float, _Saturate_59b070c1298d4279be831e68d2cd96c8_Out_1_Float);
Out_1 = _Saturate_59b070c1298d4279be831e68d2cd96c8_Out_1_Float;
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Blend_Multiply_float3(float3 Base, float3 Blend, out float3 Out, float Opacity)
{
    Out = Base * Blend;
    Out = lerp(Base, Out, Opacity);
}

void Unity_Blend_Overlay_float3(float3 Base, float3 Blend, out float3 Out, float Opacity)
{
    float3 result1 = 1.0 - 2.0 * (1.0 - Base) * (1.0 - Blend);
    float3 result2 = 2.0 * Base * Blend;
    float3 zeroOrOne = step(Base, 0.5);
    Out = result2 * zeroOrOne + (1 - zeroOrOne) * result1;
    Out = lerp(Base, Out, Opacity);
}

void Unity_NormalStrength_float(float3 In, float Strength, out float3 Out)
{
    Out = float3(In.rg * Strength, lerp(1, In.b, saturate(Strength)));
}

void Unity_NormalBlend_float(float3 A, float3 B, out float3 Out)
{
    Out = SafeNormalize(float3(A.rg + B.rg, A.b * B.b));
}

void Unity_DotProduct_float3(float3 A, float3 B, out float Out)
{
    Out = dot(A, B);
}

void Unity_Smoothstep_float(float Edge1, float Edge2, float In, out float Out)
{
    Out = smoothstep(Edge1, Edge2, In);
}

void Unity_Subtract_float3(float3 A, float3 B, out float3 Out)
{
    Out = A - B;
}

struct Bindings_MFPivotVerticalGradience_c9e909c1845b8ae4b9a11aa715963787_float
{
float3 WorldSpacePosition;
};

void SG_MFPivotVerticalGradience_c9e909c1845b8ae4b9a11aa715963787_float(float _Offset, float _Length, Bindings_MFPivotVerticalGradience_c9e909c1845b8ae4b9a11aa715963787_float IN, out float Mask_1)
{
float _Property_4680dca2dee245e7aa77915a0b17cce9_Out_0_Float = _Offset;
float _Property_c763914b9bbb4da5abad7a3db51adcb2_Out_0_Float = _Length;
float _Add_ad1531c9af874822b29fd9a13421289e_Out_2_Float;
Unity_Add_float(_Property_4680dca2dee245e7aa77915a0b17cce9_Out_0_Float, _Property_c763914b9bbb4da5abad7a3db51adcb2_Out_0_Float, _Add_ad1531c9af874822b29fd9a13421289e_Out_2_Float);
float3 _Subtract_69e80d3fb1bb4d71b4be84a010b280ff_Out_2_Vector3;
Unity_Subtract_float3(IN.WorldSpacePosition, SHADERGRAPH_OBJECT_POSITION, _Subtract_69e80d3fb1bb4d71b4be84a010b280ff_Out_2_Vector3);
float _Split_bceff54ddef34fab8cfa9a2a0b8bbc58_R_1_Float = _Subtract_69e80d3fb1bb4d71b4be84a010b280ff_Out_2_Vector3[0];
float _Split_bceff54ddef34fab8cfa9a2a0b8bbc58_G_2_Float = _Subtract_69e80d3fb1bb4d71b4be84a010b280ff_Out_2_Vector3[1];
float _Split_bceff54ddef34fab8cfa9a2a0b8bbc58_B_3_Float = _Subtract_69e80d3fb1bb4d71b4be84a010b280ff_Out_2_Vector3[2];
float _Split_bceff54ddef34fab8cfa9a2a0b8bbc58_A_4_Float = 0;
float _Smoothstep_0109929c8f9e41888dfa7ee4fd85c377_Out_3_Float;
Unity_Smoothstep_float(_Property_4680dca2dee245e7aa77915a0b17cce9_Out_0_Float, _Add_ad1531c9af874822b29fd9a13421289e_Out_2_Float, _Split_bceff54ddef34fab8cfa9a2a0b8bbc58_G_2_Float, _Smoothstep_0109929c8f9e41888dfa7ee4fd85c377_Out_3_Float);
Mask_1 = _Smoothstep_0109929c8f9e41888dfa7ee4fd85c377_Out_3_Float;
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
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
UnityTexture2D _Property_82391a4213d745e08202065048659634_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_1_Color_Map, sampler_Layer_1_Color_Map, _Layer_1_Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_7afe6a7edf62460887e09c4746d796b6_Out_0_Vector4 = IN.uv0;
float3 _Multiply_15b5d23699554ab298d90333ce57d387_Out_2_Vector3;
Unity_Multiply_float3_float3((_UV_7afe6a7edf62460887e09c4746d796b6_Out_0_Vector4.xyz), float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z))), _Multiply_15b5d23699554ab298d90333ce57d387_Out_2_Vector3);
float _Property_9a5b0bf1bf584876a406e2b633741c94_Out_0_Float = _Scale_UVs_with_Object;
float3 _Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3;
Unity_Lerp_float3((_UV_7afe6a7edf62460887e09c4746d796b6_Out_0_Vector4.xyz), _Multiply_15b5d23699554ab298d90333ce57d387_Out_2_Vector3, (_Property_9a5b0bf1bf584876a406e2b633741c94_Out_0_Float.xxx), _Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3);
float _Property_460238447f6b4170aa7397df1262be96_Out_0_Float = _Layer_1_Tiling;
float3 _Multiply_94cb4adbcc9a4b25980974e6be235287_Out_2_Vector3;
Unity_Multiply_float3_float3(_Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3, (_Property_460238447f6b4170aa7397df1262be96_Out_0_Float.xxx), _Multiply_94cb4adbcc9a4b25980974e6be235287_Out_2_Vector3);
float4 _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_82391a4213d745e08202065048659634_Out_0_Texture2D.tex, _Property_82391a4213d745e08202065048659634_Out_0_Texture2D.samplerstate, _Property_82391a4213d745e08202065048659634_Out_0_Texture2D.GetTransformedUV((_Multiply_94cb4adbcc9a4b25980974e6be235287_Out_2_Vector3.xy)) );
if (_Property_82391a4213d745e08202065048659634_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4, _Property_82391a4213d745e08202065048659634_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_R_4_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4.r;
float _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_G_5_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4.g;
float _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_B_6_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4.b;
float _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_A_7_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4.a;
float _Split_03d41d27a027425fa76a6c910dfbed96_R_1_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4[0];
float _Split_03d41d27a027425fa76a6c910dfbed96_G_2_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4[1];
float _Split_03d41d27a027425fa76a6c910dfbed96_B_3_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4[2];
float _Split_03d41d27a027425fa76a6c910dfbed96_A_4_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4[3];
float3 _Vector3_29d44982f90c486b8d58d91423294375_Out_0_Vector3 = float3(_Split_03d41d27a027425fa76a6c910dfbed96_R_1_Float, _Split_03d41d27a027425fa76a6c910dfbed96_G_2_Float, _Split_03d41d27a027425fa76a6c910dfbed96_B_3_Float);
float _Property_c55f5c80dc494683850ea3803571112b_Out_0_Float = _Layer_1_Hue;
float3 _Hue_4528903dcb5b4b15be0e273df247bfd9_Out_2_Vector3;
Unity_Hue_Normalized_float(_Vector3_29d44982f90c486b8d58d91423294375_Out_0_Vector3, _Property_c55f5c80dc494683850ea3803571112b_Out_0_Float, _Hue_4528903dcb5b4b15be0e273df247bfd9_Out_2_Vector3);
float _Property_ba7720a9d8134eb58030eb416c6a024a_Out_0_Float = _Layer_1_Brightness;
float3 _Multiply_01d7b815489a446fa8f5fbd7bf096d4f_Out_2_Vector3;
Unity_Multiply_float3_float3(_Hue_4528903dcb5b4b15be0e273df247bfd9_Out_2_Vector3, (_Property_ba7720a9d8134eb58030eb416c6a024a_Out_0_Float.xxx), _Multiply_01d7b815489a446fa8f5fbd7bf096d4f_Out_2_Vector3);
UnityTexture2D _Property_f37e4daa8ff94f5eb2ca9f6f83a20bb3_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_2_Color_Map, sampler_Layer_2_Color_Map, _Layer_2_Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_812f9793e99740fcb534b8c8d8af0ff2_Out_0_Float = _Layer_2_Tiling;
float3 _Multiply_7719c519f67d4b1aaaf2cca1cc2d4d79_Out_2_Vector3;
Unity_Multiply_float3_float3(_Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3, (_Property_812f9793e99740fcb534b8c8d8af0ff2_Out_0_Float.xxx), _Multiply_7719c519f67d4b1aaaf2cca1cc2d4d79_Out_2_Vector3);
float4 _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_f37e4daa8ff94f5eb2ca9f6f83a20bb3_Out_0_Texture2D.tex, _Property_f37e4daa8ff94f5eb2ca9f6f83a20bb3_Out_0_Texture2D.samplerstate, _Property_f37e4daa8ff94f5eb2ca9f6f83a20bb3_Out_0_Texture2D.GetTransformedUV((_Multiply_7719c519f67d4b1aaaf2cca1cc2d4d79_Out_2_Vector3.xy)) );
if (_Property_f37e4daa8ff94f5eb2ca9f6f83a20bb3_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4, _Property_f37e4daa8ff94f5eb2ca9f6f83a20bb3_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_R_4_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4.r;
float _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_G_5_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4.g;
float _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_B_6_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4.b;
float _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_A_7_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4.a;
float _Split_d38832e5c62c44d3a795b83acceab016_R_1_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4[0];
float _Split_d38832e5c62c44d3a795b83acceab016_G_2_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4[1];
float _Split_d38832e5c62c44d3a795b83acceab016_B_3_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4[2];
float _Split_d38832e5c62c44d3a795b83acceab016_A_4_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4[3];
float3 _Vector3_cb2069d38949469a976ac3a5a2003bd7_Out_0_Vector3 = float3(_Split_d38832e5c62c44d3a795b83acceab016_R_1_Float, _Split_d38832e5c62c44d3a795b83acceab016_G_2_Float, _Split_d38832e5c62c44d3a795b83acceab016_B_3_Float);
float _Property_8970d5f05c7d4b5783a57e1c656ee4a9_Out_0_Float = _Layer_2_Hue;
float3 _Hue_d8383543ffa64d25a0efd5fc36520be4_Out_2_Vector3;
Unity_Hue_Normalized_float(_Vector3_cb2069d38949469a976ac3a5a2003bd7_Out_0_Vector3, _Property_8970d5f05c7d4b5783a57e1c656ee4a9_Out_0_Float, _Hue_d8383543ffa64d25a0efd5fc36520be4_Out_2_Vector3);
float _Property_40475d05242a4efaa3e83e117ecd268d_Out_0_Float = _Layer_2_Brightness;
float3 _Multiply_9a8ec1bf43a149a7a5fb1571321b8200_Out_2_Vector3;
Unity_Multiply_float3_float3(_Hue_d8383543ffa64d25a0efd5fc36520be4_Out_2_Vector3, (_Property_40475d05242a4efaa3e83e117ecd268d_Out_0_Float.xxx), _Multiply_9a8ec1bf43a149a7a5fb1571321b8200_Out_2_Vector3);
UnityTexture2D _Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_RGB_Map, sampler_RGB_Map, _RGB_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D.tex, _Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D.samplerstate, _Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4, _Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_R_4_Float = _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4.r;
float _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_G_5_Float = _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4.g;
float _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_B_6_Float = _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4.b;
float _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_A_7_Float = _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4.a;
float3 _Lerp_754b59c21e034e87848bb65af4294c76_Out_3_Vector3;
Unity_Lerp_float3(_Multiply_01d7b815489a446fa8f5fbd7bf096d4f_Out_2_Vector3, _Multiply_9a8ec1bf43a149a7a5fb1571321b8200_Out_2_Vector3, (_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_G_5_Float.xxx), _Lerp_754b59c21e034e87848bb65af4294c76_Out_3_Vector3);
UnityTexture2D _Property_fef134089dc1401db12f51526b8ed988_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_3_Color_Map, sampler_Layer_3_Color_Map, _Layer_3_Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_8d20917a655e4062a5ce52f974969006_Out_0_Float = _Layer_3_Tiling;
float3 _Multiply_04b9ab405b2d4c27b6dd0696d9a5eacf_Out_2_Vector3;
Unity_Multiply_float3_float3(_Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3, (_Property_8d20917a655e4062a5ce52f974969006_Out_0_Float.xxx), _Multiply_04b9ab405b2d4c27b6dd0696d9a5eacf_Out_2_Vector3);
float4 _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_fef134089dc1401db12f51526b8ed988_Out_0_Texture2D.tex, _Property_fef134089dc1401db12f51526b8ed988_Out_0_Texture2D.samplerstate, _Property_fef134089dc1401db12f51526b8ed988_Out_0_Texture2D.GetTransformedUV((_Multiply_04b9ab405b2d4c27b6dd0696d9a5eacf_Out_2_Vector3.xy)) );
if (_Property_fef134089dc1401db12f51526b8ed988_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4, _Property_fef134089dc1401db12f51526b8ed988_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_R_4_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4.r;
float _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_G_5_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4.g;
float _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_B_6_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4.b;
float _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_A_7_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4.a;
float _Split_838946d3ce6f42cfbaf41fff836d6985_R_1_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4[0];
float _Split_838946d3ce6f42cfbaf41fff836d6985_G_2_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4[1];
float _Split_838946d3ce6f42cfbaf41fff836d6985_B_3_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4[2];
float _Split_838946d3ce6f42cfbaf41fff836d6985_A_4_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4[3];
float3 _Vector3_2a694d435907494fb07f2f9db09d031a_Out_0_Vector3 = float3(_Split_838946d3ce6f42cfbaf41fff836d6985_R_1_Float, _Split_838946d3ce6f42cfbaf41fff836d6985_G_2_Float, _Split_838946d3ce6f42cfbaf41fff836d6985_B_3_Float);
float _Property_6ba5eab2fbda4899981bd9c4c9c6317e_Out_0_Float = _Layer_3_Hue;
float3 _Hue_0a936fcae1974948a815da96c316b7e3_Out_2_Vector3;
Unity_Hue_Normalized_float(_Vector3_2a694d435907494fb07f2f9db09d031a_Out_0_Vector3, _Property_6ba5eab2fbda4899981bd9c4c9c6317e_Out_0_Float, _Hue_0a936fcae1974948a815da96c316b7e3_Out_2_Vector3);
float _Property_c749f04d24d9423fa1a2859f800dc893_Out_0_Float = _Layer_3_Brightness;
float3 _Multiply_db73d490dc9d432e80685ef18e5427fb_Out_2_Vector3;
Unity_Multiply_float3_float3(_Hue_0a936fcae1974948a815da96c316b7e3_Out_2_Vector3, (_Property_c749f04d24d9423fa1a2859f800dc893_Out_0_Float.xxx), _Multiply_db73d490dc9d432e80685ef18e5427fb_Out_2_Vector3);
float3 _Lerp_66f917f82ed14fd89c6c2fe066b5e834_Out_3_Vector3;
Unity_Lerp_float3(_Lerp_754b59c21e034e87848bb65af4294c76_Out_3_Vector3, _Multiply_db73d490dc9d432e80685ef18e5427fb_Out_2_Vector3, (_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_B_6_Float.xxx), _Lerp_66f917f82ed14fd89c6c2fe066b5e834_Out_3_Vector3);
#if defined(_USE_LAYER_3)
float3 _UseLayer3_ee2b565dd7774fa7a5c88d0b7bfd05b8_Out_0_Vector3 = _Lerp_66f917f82ed14fd89c6c2fe066b5e834_Out_3_Vector3;
#else
float3 _UseLayer3_ee2b565dd7774fa7a5c88d0b7bfd05b8_Out_0_Vector3 = _Lerp_754b59c21e034e87848bb65af4294c76_Out_3_Vector3;
#endif
#if defined(_USE_LAYER_2)
float3 _UseLayer2_22b88f361b80488fbbec2df7f45c4924_Out_0_Vector3 = _UseLayer3_ee2b565dd7774fa7a5c88d0b7bfd05b8_Out_0_Vector3;
#else
float3 _UseLayer2_22b88f361b80488fbbec2df7f45c4924_Out_0_Vector3 = _Multiply_01d7b815489a446fa8f5fbd7bf096d4f_Out_2_Vector3;
#endif
float4 _Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4 = _Stylization_Color;
float _Split_42e867e97f454925a658dba60e21864f_R_1_Float = _Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4[0];
float _Split_42e867e97f454925a658dba60e21864f_G_2_Float = _Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4[1];
float _Split_42e867e97f454925a658dba60e21864f_B_3_Float = _Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4[2];
float _Split_42e867e97f454925a658dba60e21864f_A_4_Float = _Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4[3];
UnityTexture2D _Property_c02e202098944eebbd44663588c64e3b_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Edge_Map, sampler_Edge_Map, _Edge_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_c02e202098944eebbd44663588c64e3b_Out_0_Texture2D.tex, _Property_c02e202098944eebbd44663588c64e3b_Out_0_Texture2D.samplerstate, _Property_c02e202098944eebbd44663588c64e3b_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_c02e202098944eebbd44663588c64e3b_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4, _Property_c02e202098944eebbd44663588c64e3b_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_R_4_Float = _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4.r;
float _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_G_5_Float = _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4.g;
float _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_B_6_Float = _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4.b;
float _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_A_7_Float = _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4.a;
float _Property_a4422d37671342bdab8d700fe4c2424a_Out_0_Float = _Stylization_Mask_Contrast;
Bindings_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float _MFCheapContrast_2e3888093b39403781e2ccbee9980699;
float _MFCheapContrast_2e3888093b39403781e2ccbee9980699_Out_1_Float;
SG_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float(_SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_A_7_Float, _Property_a4422d37671342bdab8d700fe4c2424a_Out_0_Float, _MFCheapContrast_2e3888093b39403781e2ccbee9980699, _MFCheapContrast_2e3888093b39403781e2ccbee9980699_Out_1_Float);
float _Multiply_364f0f8b2ff74453b42f050a8b1cb526_Out_2_Float;
Unity_Multiply_float_float(_Split_42e867e97f454925a658dba60e21864f_A_4_Float, _MFCheapContrast_2e3888093b39403781e2ccbee9980699_Out_1_Float, _Multiply_364f0f8b2ff74453b42f050a8b1cb526_Out_2_Float);
float3 _Lerp_0a1e51abb320477ab63b42e1a32052cc_Out_3_Vector3;
Unity_Lerp_float3(_UseLayer2_22b88f361b80488fbbec2df7f45c4924_Out_0_Vector3, (_Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4.xyz), (_Multiply_364f0f8b2ff74453b42f050a8b1cb526_Out_2_Float.xxx), _Lerp_0a1e51abb320477ab63b42e1a32052cc_Out_3_Vector3);
#if defined(_USE_LAYER_1)
float3 _UseLayer1_f5f8cf003ae64c4187983dc616ecbd3b_Out_0_Vector3 = _Lerp_0a1e51abb320477ab63b42e1a32052cc_Out_3_Vector3;
#else
float3 _UseLayer1_f5f8cf003ae64c4187983dc616ecbd3b_Out_0_Vector3 = (_Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4.xyz);
#endif
float4 _Property_0be88f8e5ba94765a3623024be14bbbf_Out_0_Vector4 = _AO_Color;
float _Split_07ec24369bd1411c82179cabd24aabe5_R_1_Float = _Property_0be88f8e5ba94765a3623024be14bbbf_Out_0_Vector4[0];
float _Split_07ec24369bd1411c82179cabd24aabe5_G_2_Float = _Property_0be88f8e5ba94765a3623024be14bbbf_Out_0_Vector4[1];
float _Split_07ec24369bd1411c82179cabd24aabe5_B_3_Float = _Property_0be88f8e5ba94765a3623024be14bbbf_Out_0_Vector4[2];
float _Split_07ec24369bd1411c82179cabd24aabe5_A_4_Float = _Property_0be88f8e5ba94765a3623024be14bbbf_Out_0_Vector4[3];
float _Property_781496b67f424345bca22ad901512267_Out_0_Float = _AO_Mask_Contrast;
Bindings_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float _MFCheapContrast_74522f75ddab433f9ec9f709fe5a6faf;
float _MFCheapContrast_74522f75ddab433f9ec9f709fe5a6faf_Out_1_Float;
SG_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float(_SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_G_5_Float, _Property_781496b67f424345bca22ad901512267_Out_0_Float, _MFCheapContrast_74522f75ddab433f9ec9f709fe5a6faf, _MFCheapContrast_74522f75ddab433f9ec9f709fe5a6faf_Out_1_Float);
float _Multiply_27eebb10636649c3aefb6ad13580fd9e_Out_2_Float;
Unity_Multiply_float_float(_Split_07ec24369bd1411c82179cabd24aabe5_A_4_Float, _MFCheapContrast_74522f75ddab433f9ec9f709fe5a6faf_Out_1_Float, _Multiply_27eebb10636649c3aefb6ad13580fd9e_Out_2_Float);
float3 _Blend_5baeb7dcebe145dcb53bba406ea82ebd_Out_2_Vector3;
Unity_Blend_Multiply_float3(_UseLayer1_f5f8cf003ae64c4187983dc616ecbd3b_Out_0_Vector3, (_Property_0be88f8e5ba94765a3623024be14bbbf_Out_0_Vector4.xyz), _Blend_5baeb7dcebe145dcb53bba406ea82ebd_Out_2_Vector3, _Multiply_27eebb10636649c3aefb6ad13580fd9e_Out_2_Float);
float4 _Property_64075cb2946e4bc89c0928961fa8e759_Out_0_Vector4 = _Dirt_Color;
float _Split_102b0005351f49a0a7ec214d4c4e879e_R_1_Float = _Property_64075cb2946e4bc89c0928961fa8e759_Out_0_Vector4[0];
float _Split_102b0005351f49a0a7ec214d4c4e879e_G_2_Float = _Property_64075cb2946e4bc89c0928961fa8e759_Out_0_Vector4[1];
float _Split_102b0005351f49a0a7ec214d4c4e879e_B_3_Float = _Property_64075cb2946e4bc89c0928961fa8e759_Out_0_Vector4[2];
float _Split_102b0005351f49a0a7ec214d4c4e879e_A_4_Float = _Property_64075cb2946e4bc89c0928961fa8e759_Out_0_Vector4[3];
float _Property_089f6755c08b483d971238d917dd8b3a_Out_0_Float = _Dirt_Mask_Contrast;
Bindings_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float _MFCheapContrast_96f7295ada8247d48955ac898a7aba11;
float _MFCheapContrast_96f7295ada8247d48955ac898a7aba11_Out_1_Float;
SG_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float(_SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_R_4_Float, _Property_089f6755c08b483d971238d917dd8b3a_Out_0_Float, _MFCheapContrast_96f7295ada8247d48955ac898a7aba11, _MFCheapContrast_96f7295ada8247d48955ac898a7aba11_Out_1_Float);
float _Multiply_bc33d3e78b104e93a60b60cea85cebac_Out_2_Float;
Unity_Multiply_float_float(_Split_102b0005351f49a0a7ec214d4c4e879e_A_4_Float, _MFCheapContrast_96f7295ada8247d48955ac898a7aba11_Out_1_Float, _Multiply_bc33d3e78b104e93a60b60cea85cebac_Out_2_Float);
float3 _Lerp_ef18ae06dc3e4fd18201e3925bc55a85_Out_3_Vector3;
Unity_Lerp_float3(_Blend_5baeb7dcebe145dcb53bba406ea82ebd_Out_2_Vector3, (_Property_64075cb2946e4bc89c0928961fa8e759_Out_0_Vector4.xyz), (_Multiply_bc33d3e78b104e93a60b60cea85cebac_Out_2_Float.xxx), _Lerp_ef18ae06dc3e4fd18201e3925bc55a85_Out_3_Vector3);
float4 _Property_dbda4c8e93f54f92a18b3390cbaf06c0_Out_0_Vector4 = _Edgewear_Color;
float _Split_925676a8ce21420aad811d255e389512_R_1_Float = _Property_dbda4c8e93f54f92a18b3390cbaf06c0_Out_0_Vector4[0];
float _Split_925676a8ce21420aad811d255e389512_G_2_Float = _Property_dbda4c8e93f54f92a18b3390cbaf06c0_Out_0_Vector4[1];
float _Split_925676a8ce21420aad811d255e389512_B_3_Float = _Property_dbda4c8e93f54f92a18b3390cbaf06c0_Out_0_Vector4[2];
float _Split_925676a8ce21420aad811d255e389512_A_4_Float = _Property_dbda4c8e93f54f92a18b3390cbaf06c0_Out_0_Vector4[3];
float _Property_4bef2cb688474ef8909e6dc3f2439616_Out_0_Float = _Edgewear_Mask_Contrast;
Bindings_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float _MFCheapContrast_c8d787e56cf842869980c82103f39754;
float _MFCheapContrast_c8d787e56cf842869980c82103f39754_Out_1_Float;
SG_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float(_SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_B_6_Float, _Property_4bef2cb688474ef8909e6dc3f2439616_Out_0_Float, _MFCheapContrast_c8d787e56cf842869980c82103f39754, _MFCheapContrast_c8d787e56cf842869980c82103f39754_Out_1_Float);
float _Multiply_38b6bc16bfef4cd3bec7370aabb6a836_Out_2_Float;
Unity_Multiply_float_float(_Split_925676a8ce21420aad811d255e389512_A_4_Float, _MFCheapContrast_c8d787e56cf842869980c82103f39754_Out_1_Float, _Multiply_38b6bc16bfef4cd3bec7370aabb6a836_Out_2_Float);
float3 _Blend_e613dc790cd741d3b367cb48289cffd5_Out_2_Vector3;
Unity_Blend_Overlay_float3(_Lerp_ef18ae06dc3e4fd18201e3925bc55a85_Out_3_Vector3, (_Property_dbda4c8e93f54f92a18b3390cbaf06c0_Out_0_Vector4.xyz), _Blend_e613dc790cd741d3b367cb48289cffd5_Out_2_Vector3, _Multiply_38b6bc16bfef4cd3bec7370aabb6a836_Out_2_Float);
UnityTexture2D _Property_003a6ee6678543e5964d3e811c19f789_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Coverage_Color_Map, sampler_Coverage_Color_Map, _Coverage_Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_d9f12d2b2464447a82f581c14778bbf8_Out_0_Float = _Coverage_Tiling;
float3 _Multiply_282d032113614cb58779c3a33671ad6c_Out_2_Vector3;
Unity_Multiply_float3_float3(_Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3, (_Property_d9f12d2b2464447a82f581c14778bbf8_Out_0_Float.xxx), _Multiply_282d032113614cb58779c3a33671ad6c_Out_2_Vector3);
float4 _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_003a6ee6678543e5964d3e811c19f789_Out_0_Texture2D.tex, _Property_003a6ee6678543e5964d3e811c19f789_Out_0_Texture2D.samplerstate, _Property_003a6ee6678543e5964d3e811c19f789_Out_0_Texture2D.GetTransformedUV((_Multiply_282d032113614cb58779c3a33671ad6c_Out_2_Vector3.xy)) );
if (_Property_003a6ee6678543e5964d3e811c19f789_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4, _Property_003a6ee6678543e5964d3e811c19f789_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_R_4_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4.r;
float _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_G_5_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4.g;
float _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_B_6_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4.b;
float _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_A_7_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4.a;
float _Split_42bc736ed19d41c2bc8a58c7f653b802_R_1_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4[0];
float _Split_42bc736ed19d41c2bc8a58c7f653b802_G_2_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4[1];
float _Split_42bc736ed19d41c2bc8a58c7f653b802_B_3_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4[2];
float _Split_42bc736ed19d41c2bc8a58c7f653b802_A_4_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4[3];
float3 _Vector3_dd63cc8dd9a443a99a6ecc681ff6961e_Out_0_Vector3 = float3(_Split_42bc736ed19d41c2bc8a58c7f653b802_R_1_Float, _Split_42bc736ed19d41c2bc8a58c7f653b802_G_2_Float, _Split_42bc736ed19d41c2bc8a58c7f653b802_B_3_Float);
float _Property_86cbded0425a4bf2b5441e665a7f5551_Out_0_Float = _Coverage_Hue;
float3 _Hue_5e168e8c10044fe09120668e4b870b44_Out_2_Vector3;
Unity_Hue_Normalized_float(_Vector3_dd63cc8dd9a443a99a6ecc681ff6961e_Out_0_Vector3, _Property_86cbded0425a4bf2b5441e665a7f5551_Out_0_Float, _Hue_5e168e8c10044fe09120668e4b870b44_Out_2_Vector3);
float _Property_dccedc571d6d411291da660d59f31596_Out_0_Float = _Coverage_Brightness;
float3 _Multiply_c28f28e86afd43f584c04c39cc44c8c4_Out_2_Vector3;
Unity_Multiply_float3_float3(_Hue_5e168e8c10044fe09120668e4b870b44_Out_2_Vector3, (_Property_dccedc571d6d411291da660d59f31596_Out_0_Float.xxx), _Multiply_c28f28e86afd43f584c04c39cc44c8c4_Out_2_Vector3);
float _Property_f13be210d8b049a9a1e6c0e219f5a53f_Out_0_Float = _Coverage_Normal_Mask_Slope;
float _Property_1f786f31d0924f6ba0904b90319c56e3_Out_0_Float = _Coverage_Normal_Mask_Gradience;
float _Add_4177ca63c9724385910d37f97d0e8c48_Out_2_Float;
Unity_Add_float(_Property_f13be210d8b049a9a1e6c0e219f5a53f_Out_0_Float, _Property_1f786f31d0924f6ba0904b90319c56e3_Out_0_Float, _Add_4177ca63c9724385910d37f97d0e8c48_Out_2_Float);
UnityTexture2D _Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_1_Normal_Map, sampler_Layer_1_Normal_Map, _Layer_1_Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D.tex, _Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D.samplerstate, _Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D.GetTransformedUV((_Multiply_94cb4adbcc9a4b25980974e6be235287_Out_2_Vector3.xy)) );
if (_Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4, _Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4);
float _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_R_4_Float = _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.r;
float _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_G_5_Float = _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.g;
float _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_B_6_Float = _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.b;
float _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_A_7_Float = _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.a;
float _Property_7cfa9a8e94684810a4cd4c70a2257b6c_Out_0_Float = _Layer_1_Normal_Strength;
float3 _NormalStrength_9428e53c24e74ce189625604c4b60275_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.xyz), _Property_7cfa9a8e94684810a4cd4c70a2257b6c_Out_0_Float, _NormalStrength_9428e53c24e74ce189625604c4b60275_Out_2_Vector3);
UnityTexture2D _Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_2_Normal_Map, sampler_Layer_2_Normal_Map, _Layer_2_Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D.tex, _Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D.samplerstate, _Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D.GetTransformedUV((_Multiply_7719c519f67d4b1aaaf2cca1cc2d4d79_Out_2_Vector3.xy)) );
if (_Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4, _Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4);
float _SampleTexture2D_ae5b372472874ad28eac881821ce553c_R_4_Float = _SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.r;
float _SampleTexture2D_ae5b372472874ad28eac881821ce553c_G_5_Float = _SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.g;
float _SampleTexture2D_ae5b372472874ad28eac881821ce553c_B_6_Float = _SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.b;
float _SampleTexture2D_ae5b372472874ad28eac881821ce553c_A_7_Float = _SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.a;
float _Property_12c65401b09f44daa763974826741097_Out_0_Float = _Layer_2_Normal_Strength;
float3 _NormalStrength_93ad91a06836481ca4f36c947e62a393_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.xyz), _Property_12c65401b09f44daa763974826741097_Out_0_Float, _NormalStrength_93ad91a06836481ca4f36c947e62a393_Out_2_Vector3);
float3 _Lerp_7325f1720917490cb13328e58ddd8efe_Out_3_Vector3;
Unity_Lerp_float3(_NormalStrength_9428e53c24e74ce189625604c4b60275_Out_2_Vector3, _NormalStrength_93ad91a06836481ca4f36c947e62a393_Out_2_Vector3, (_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_G_5_Float.xxx), _Lerp_7325f1720917490cb13328e58ddd8efe_Out_3_Vector3);
UnityTexture2D _Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_3_Normal_Map, sampler_Layer_3_Normal_Map, _Layer_3_Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D.tex, _Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D.samplerstate, _Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D.GetTransformedUV((_Multiply_04b9ab405b2d4c27b6dd0696d9a5eacf_Out_2_Vector3.xy)) );
if (_Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4, _Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4);
float _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_R_4_Float = _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.r;
float _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_G_5_Float = _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.g;
float _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_B_6_Float = _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.b;
float _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_A_7_Float = _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.a;
float _Property_02480e6b3805449397b2d5a15e416673_Out_0_Float = _Layer_3_Normal_Strength;
float3 _NormalStrength_8e30a22d9ed147ca9ef200087a7f43aa_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.xyz), _Property_02480e6b3805449397b2d5a15e416673_Out_0_Float, _NormalStrength_8e30a22d9ed147ca9ef200087a7f43aa_Out_2_Vector3);
float3 _Lerp_642d324ee8d74cf888dc943377359b6d_Out_3_Vector3;
Unity_Lerp_float3(_Lerp_7325f1720917490cb13328e58ddd8efe_Out_3_Vector3, _NormalStrength_8e30a22d9ed147ca9ef200087a7f43aa_Out_2_Vector3, (_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_B_6_Float.xxx), _Lerp_642d324ee8d74cf888dc943377359b6d_Out_3_Vector3);
#if defined(_USE_LAYER_3)
float3 _UseLayer3_3e504be39f154559868343947f457c24_Out_0_Vector3 = _Lerp_642d324ee8d74cf888dc943377359b6d_Out_3_Vector3;
#else
float3 _UseLayer3_3e504be39f154559868343947f457c24_Out_0_Vector3 = _Lerp_7325f1720917490cb13328e58ddd8efe_Out_3_Vector3;
#endif
#if defined(_USE_LAYER_2)
float3 _UseLayer2_c90f92a0b0134057867d0810829f779e_Out_0_Vector3 = _UseLayer3_3e504be39f154559868343947f457c24_Out_0_Vector3;
#else
float3 _UseLayer2_c90f92a0b0134057867d0810829f779e_Out_0_Vector3 = _NormalStrength_9428e53c24e74ce189625604c4b60275_Out_2_Vector3;
#endif
UnityTexture2D _Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Normal_Map, sampler_Normal_Map, _Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D.tex, _Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D.samplerstate, _Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4, _Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4);
float _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_R_4_Float = _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.r;
float _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_G_5_Float = _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.g;
float _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_B_6_Float = _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.b;
float _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_A_7_Float = _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.a;
float _Property_758b3d19a0c14ff099921db6234664b2_Out_0_Float = _Normal_Strength;
float3 _NormalStrength_3eb407934b854455a959c26453776991_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.xyz), _Property_758b3d19a0c14ff099921db6234664b2_Out_0_Float, _NormalStrength_3eb407934b854455a959c26453776991_Out_2_Vector3);
float3 _NormalBlend_cb4851ae94454d539b1004bdc1f2604f_Out_2_Vector3;
Unity_NormalBlend_float(_UseLayer2_c90f92a0b0134057867d0810829f779e_Out_0_Vector3, _NormalStrength_3eb407934b854455a959c26453776991_Out_2_Vector3, _NormalBlend_cb4851ae94454d539b1004bdc1f2604f_Out_2_Vector3);
#if defined(_USE_LAYER_1)
float3 _UseLayer1_7da71e644bf2434f99e8005bad9e8f14_Out_0_Vector3 = _NormalBlend_cb4851ae94454d539b1004bdc1f2604f_Out_2_Vector3;
#else
float3 _UseLayer1_7da71e644bf2434f99e8005bad9e8f14_Out_0_Vector3 = _NormalStrength_3eb407934b854455a959c26453776991_Out_2_Vector3;
#endif
float3 _Transform_04398ac1bc954fb19c62ce3f5f6d670c_Out_1_Vector3;
{
float3x3 tangentTransform = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
_Transform_04398ac1bc954fb19c62ce3f5f6d670c_Out_1_Vector3 = TransformTangentToWorld(_UseLayer1_7da71e644bf2434f99e8005bad9e8f14_Out_0_Vector3.xyz, tangentTransform, true);
}
float _DotProduct_ae13069c870e4539be3826642299bede_Out_2_Float;
Unity_DotProduct_float3(_Transform_04398ac1bc954fb19c62ce3f5f6d670c_Out_1_Vector3, float3(0, 1, 0), _DotProduct_ae13069c870e4539be3826642299bede_Out_2_Float);
float _Smoothstep_652b53e75f9e481e8c829d3bd3d9c24e_Out_3_Float;
Unity_Smoothstep_float(_Property_f13be210d8b049a9a1e6c0e219f5a53f_Out_0_Float, _Add_4177ca63c9724385910d37f97d0e8c48_Out_2_Float, _DotProduct_ae13069c870e4539be3826642299bede_Out_2_Float, _Smoothstep_652b53e75f9e481e8c829d3bd3d9c24e_Out_3_Float);
float _Property_64938267e5134c5b9a4256d11aa25705_Out_0_Float = _Coverage_Height_Mask_Pivot_Offset;
float _Property_cfcce6f026924b048f8c33bdc73c14f6_Out_0_Float = _Coverage_Height_Mask_Length;
Bindings_MFPivotVerticalGradience_c9e909c1845b8ae4b9a11aa715963787_float _MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c;
_MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c.WorldSpacePosition = IN.WorldSpacePosition;
float _MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c_Mask_1_Float;
SG_MFPivotVerticalGradience_c9e909c1845b8ae4b9a11aa715963787_float(_Property_64938267e5134c5b9a4256d11aa25705_Out_0_Float, _Property_cfcce6f026924b048f8c33bdc73c14f6_Out_0_Float, _MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c, _MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c_Mask_1_Float);
float _Multiply_f6a21f2d15b7472e8173002af669cfdf_Out_2_Float;
Unity_Multiply_float_float(_Smoothstep_652b53e75f9e481e8c829d3bd3d9c24e_Out_3_Float, _MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c_Mask_1_Float, _Multiply_f6a21f2d15b7472e8173002af669cfdf_Out_2_Float);
float3 _Lerp_90a44aeed76b4e67b245ea63960c28fc_Out_3_Vector3;
Unity_Lerp_float3(_Blend_e613dc790cd741d3b367cb48289cffd5_Out_2_Vector3, _Multiply_c28f28e86afd43f584c04c39cc44c8c4_Out_2_Vector3, (_Multiply_f6a21f2d15b7472e8173002af669cfdf_Out_2_Float.xxx), _Lerp_90a44aeed76b4e67b245ea63960c28fc_Out_3_Vector3);
#if defined(_USE_COVERAGE_LAYER)
float3 _UseCoverageLayer_402efe74563f417bb22b7997bdfc5c1e_Out_0_Vector3 = _Lerp_90a44aeed76b4e67b245ea63960c28fc_Out_3_Vector3;
#else
float3 _UseCoverageLayer_402efe74563f417bb22b7997bdfc5c1e_Out_0_Vector3 = _Blend_e613dc790cd741d3b367cb48289cffd5_Out_2_Vector3;
#endif
surface.BaseColor = _UseCoverageLayer_402efe74563f417bb22b7997bdfc5c1e_Out_0_Vector3;
surface.Emission = float3(0, 0, 0);
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

    // to pr               eserve mikktspace compliance we use same scale renormFactor as was used on the normal.
    // This                is explained in section 2.2 in "surface gradient based bump mapping framework"
    output.WorldSpaceTangent = renormFactor * input.tangentWS.xyz;
    output.WorldSpaceBiTangent = renormFactor * bitang;

    output.WorldSpacePosition = input.positionWS;

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
#pragma shader_feature_local _ _USE_LAYER_2
#pragma shader_feature_local _ _USE_LAYER_3
#pragma shader_feature_local _ _USE_COVERAGE_LAYER
#pragma shader_feature_local _ _USE_LAYER_1

#if defined(_USE_LAYER_2) && defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_3) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_2
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_3)
#define KEYWORD_PERMUTATION_3
#elif defined(_USE_LAYER_2) && defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_4
#elif defined(_USE_LAYER_2) && defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_5
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_6
#elif defined(_USE_LAYER_2)
#define KEYWORD_PERMUTATION_7
#elif defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_8
#elif defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_9
#elif defined(_USE_LAYER_3) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_10
#elif defined(_USE_LAYER_3)
#define KEYWORD_PERMUTATION_11
#elif defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_12
#elif defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_13
#elif defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_14
#else
#define KEYWORD_PERMUTATION_15
#endif


// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
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

float4 _Layer_1_Color_Map_TexelSize;
float _Layer_1_Hue;
float _Layer_1_Brightness;
float _Coverage_Height_Mask_Length;
float4 _Layer_1_Normal_Map_TexelSize;
float _Layer_1_Normal_Strength;
float _Layer_1_Tiling;
float _Layer_1_Smoothness_Strength;
float _Layer_2_Tiling;
float4 _Layer_2_Color_Map_TexelSize;
float _Layer_2_Hue;
float _Layer_2_Brightness;
float _Layer_2_Smoothness_Strength;
float4 _Layer_2_Normal_Map_TexelSize;
float _Layer_2_Normal_Strength;
float _Layer_3_Tiling;
float4 _Layer_3_Color_Map_TexelSize;
float _Layer_3_Hue;
float _Layer_3_Brightness;
float _Layer_3_Smoothness_Strength;
float4 _Layer_3_Normal_Map_TexelSize;
float _Layer_3_Normal_Strength;
float4 _RGB_Map_TexelSize;
float4 _Normal_Map_TexelSize;
float _Normal_Strength;
float _Coverage_Tiling;
float4 _Coverage_Color_Map_TexelSize;
float _Coverage_Hue;
float _Coverage_Brightness;
float _Coverage_Smoothness_Strength;
float4 _Coverage_Normal_Map_TexelSize;
float _Coverage_Normal_Strength;
float4 _Edge_Map_TexelSize;
float _Dirt_Mask_Contrast;
float _AO_Mask_Contrast;
float _Edgewear_Mask_Contrast;
float _Stylization_Mask_Contrast;
float4 _Stylization_Color;
float4 _AO_Color;
float4 _Dirt_Color;
float4 _Edgewear_Color;
float _Coverage_Normal_Mask_Slope;
float _Coverage_Normal_Mask_Gradience;
float _Coverage_Height_Mask_Pivot_Offset;
float _Scale_UVs_with_Object;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Layer_1_Color_Map);
SAMPLER(sampler_Layer_1_Color_Map);
TEXTURE2D(_Layer_1_Normal_Map);
SAMPLER(sampler_Layer_1_Normal_Map);
TEXTURE2D(_Layer_2_Color_Map);
SAMPLER(sampler_Layer_2_Color_Map);
TEXTURE2D(_Layer_2_Normal_Map);
SAMPLER(sampler_Layer_2_Normal_Map);
TEXTURE2D(_Layer_3_Color_Map);
SAMPLER(sampler_Layer_3_Color_Map);
TEXTURE2D(_Layer_3_Normal_Map);
SAMPLER(sampler_Layer_3_Normal_Map);
TEXTURE2D(_RGB_Map);
SAMPLER(sampler_RGB_Map);
TEXTURE2D(_Normal_Map);
SAMPLER(sampler_Normal_Map);
TEXTURE2D(_Coverage_Color_Map);
SAMPLER(sampler_Coverage_Color_Map);
TEXTURE2D(_Coverage_Normal_Map);
SAMPLER(sampler_Coverage_Normal_Map);
TEXTURE2D(_Edge_Map);
SAMPLER(sampler_Edge_Map);

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
#pragma shader_feature_local _ _USE_LAYER_2
#pragma shader_feature_local _ _USE_LAYER_3
#pragma shader_feature_local _ _USE_COVERAGE_LAYER
#pragma shader_feature_local _ _USE_LAYER_1

#if defined(_USE_LAYER_2) && defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_3) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_2
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_3)
#define KEYWORD_PERMUTATION_3
#elif defined(_USE_LAYER_2) && defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_4
#elif defined(_USE_LAYER_2) && defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_5
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_6
#elif defined(_USE_LAYER_2)
#define KEYWORD_PERMUTATION_7
#elif defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_8
#elif defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_9
#elif defined(_USE_LAYER_3) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_10
#elif defined(_USE_LAYER_3)
#define KEYWORD_PERMUTATION_11
#elif defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_12
#elif defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_13
#elif defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_14
#else
#define KEYWORD_PERMUTATION_15
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
 float3 WorldSpaceNormal;
 float3 WorldSpaceTangent;
 float3 WorldSpaceBiTangent;
 float3 WorldSpacePosition;
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

float4 _Layer_1_Color_Map_TexelSize;
float _Layer_1_Hue;
float _Layer_1_Brightness;
float _Coverage_Height_Mask_Length;
float4 _Layer_1_Normal_Map_TexelSize;
float _Layer_1_Normal_Strength;
float _Layer_1_Tiling;
float _Layer_1_Smoothness_Strength;
float _Layer_2_Tiling;
float4 _Layer_2_Color_Map_TexelSize;
float _Layer_2_Hue;
float _Layer_2_Brightness;
float _Layer_2_Smoothness_Strength;
float4 _Layer_2_Normal_Map_TexelSize;
float _Layer_2_Normal_Strength;
float _Layer_3_Tiling;
float4 _Layer_3_Color_Map_TexelSize;
float _Layer_3_Hue;
float _Layer_3_Brightness;
float _Layer_3_Smoothness_Strength;
float4 _Layer_3_Normal_Map_TexelSize;
float _Layer_3_Normal_Strength;
float4 _RGB_Map_TexelSize;
float4 _Normal_Map_TexelSize;
float _Normal_Strength;
float _Coverage_Tiling;
float4 _Coverage_Color_Map_TexelSize;
float _Coverage_Hue;
float _Coverage_Brightness;
float _Coverage_Smoothness_Strength;
float4 _Coverage_Normal_Map_TexelSize;
float _Coverage_Normal_Strength;
float4 _Edge_Map_TexelSize;
float _Dirt_Mask_Contrast;
float _AO_Mask_Contrast;
float _Edgewear_Mask_Contrast;
float _Stylization_Mask_Contrast;
float4 _Stylization_Color;
float4 _AO_Color;
float4 _Dirt_Color;
float4 _Edgewear_Color;
float _Coverage_Normal_Mask_Slope;
float _Coverage_Normal_Mask_Gradience;
float _Coverage_Height_Mask_Pivot_Offset;
float _Scale_UVs_with_Object;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Layer_1_Color_Map);
SAMPLER(sampler_Layer_1_Color_Map);
TEXTURE2D(_Layer_1_Normal_Map);
SAMPLER(sampler_Layer_1_Normal_Map);
TEXTURE2D(_Layer_2_Color_Map);
SAMPLER(sampler_Layer_2_Color_Map);
TEXTURE2D(_Layer_2_Normal_Map);
SAMPLER(sampler_Layer_2_Normal_Map);
TEXTURE2D(_Layer_3_Color_Map);
SAMPLER(sampler_Layer_3_Color_Map);
TEXTURE2D(_Layer_3_Normal_Map);
SAMPLER(sampler_Layer_3_Normal_Map);
TEXTURE2D(_RGB_Map);
SAMPLER(sampler_RGB_Map);
TEXTURE2D(_Normal_Map);
SAMPLER(sampler_Normal_Map);
TEXTURE2D(_Coverage_Color_Map);
SAMPLER(sampler_Coverage_Color_Map);
TEXTURE2D(_Coverage_Normal_Map);
SAMPLER(sampler_Coverage_Normal_Map);
TEXTURE2D(_Edge_Map);
SAMPLER(sampler_Edge_Map);

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

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Hue_Normalized_float(float3 In, float Offset, out float3 Out)
{
    // RGB to HSV
    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 P = lerp(float4(In.bg, K.wz), float4(In.gb, K.xy), step(In.b, In.g));
    float4 Q = lerp(float4(P.xyw, In.r), float4(In.r, P.yzx), step(P.x, In.r));
    float D = Q.x - min(Q.w, Q.y);
    float E = 1e-10;
    float V = (D == 0) ? Q.x : (Q.x + E);
    float3 hsv = float3(abs(Q.z + (Q.w - Q.y)/(6.0 * D + E)), D / (Q.x + E), V);

    float hue = hsv.x + Offset;
    hsv.x = (hue < 0)
            ? hue + 1
            : (hue > 1)
                ? hue - 1
                : hue;

    // HSV to RGB
    float4 K2 = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 P2 = abs(frac(hsv.xxx + K2.xyz) * 6.0 - K2.www);
    Out = hsv.z * lerp(K2.xxx, saturate(P2 - K2.xxx), hsv.y);
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Lerp_float(float A, float B, float T, out float Out)
{
    Out = lerp(A, B, T);
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

struct Bindings_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float
{
};

void SG_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float(float _Input, float _Contrast, Bindings_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float IN, out float Out_1)
{
float _Property_f17f2d608b764773bc9f33ea28c3e14e_Out_0_Float = _Contrast;
float _Add_59838b3eb9ec437bbd93b6fd8db7a0c4_Out_2_Float;
Unity_Add_float(_Property_f17f2d608b764773bc9f33ea28c3e14e_Out_0_Float, float(-1), _Add_59838b3eb9ec437bbd93b6fd8db7a0c4_Out_2_Float);
float _Add_524e73a4aa0b4e7982442fdf0d39fe18_Out_2_Float;
Unity_Add_float(_Property_f17f2d608b764773bc9f33ea28c3e14e_Out_0_Float, float(1), _Add_524e73a4aa0b4e7982442fdf0d39fe18_Out_2_Float);
float _Property_df60db08578f4909bad9a81c68245e45_Out_0_Float = _Input;
float _Lerp_6af4dfb2e0f0426a81a1669c55ec8196_Out_3_Float;
Unity_Lerp_float(_Add_59838b3eb9ec437bbd93b6fd8db7a0c4_Out_2_Float, _Add_524e73a4aa0b4e7982442fdf0d39fe18_Out_2_Float, _Property_df60db08578f4909bad9a81c68245e45_Out_0_Float, _Lerp_6af4dfb2e0f0426a81a1669c55ec8196_Out_3_Float);
float _Saturate_59b070c1298d4279be831e68d2cd96c8_Out_1_Float;
Unity_Saturate_float(_Lerp_6af4dfb2e0f0426a81a1669c55ec8196_Out_3_Float, _Saturate_59b070c1298d4279be831e68d2cd96c8_Out_1_Float);
Out_1 = _Saturate_59b070c1298d4279be831e68d2cd96c8_Out_1_Float;
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Blend_Multiply_float3(float3 Base, float3 Blend, out float3 Out, float Opacity)
{
    Out = Base * Blend;
    Out = lerp(Base, Out, Opacity);
}

void Unity_Blend_Overlay_float3(float3 Base, float3 Blend, out float3 Out, float Opacity)
{
    float3 result1 = 1.0 - 2.0 * (1.0 - Base) * (1.0 - Blend);
    float3 result2 = 2.0 * Base * Blend;
    float3 zeroOrOne = step(Base, 0.5);
    Out = result2 * zeroOrOne + (1 - zeroOrOne) * result1;
    Out = lerp(Base, Out, Opacity);
}

void Unity_NormalStrength_float(float3 In, float Strength, out float3 Out)
{
    Out = float3(In.rg * Strength, lerp(1, In.b, saturate(Strength)));
}

void Unity_NormalBlend_float(float3 A, float3 B, out float3 Out)
{
    Out = SafeNormalize(float3(A.rg + B.rg, A.b * B.b));
}

void Unity_DotProduct_float3(float3 A, float3 B, out float Out)
{
    Out = dot(A, B);
}

void Unity_Smoothstep_float(float Edge1, float Edge2, float In, out float Out)
{
    Out = smoothstep(Edge1, Edge2, In);
}

void Unity_Subtract_float3(float3 A, float3 B, out float3 Out)
{
    Out = A - B;
}

struct Bindings_MFPivotVerticalGradience_c9e909c1845b8ae4b9a11aa715963787_float
{
float3 WorldSpacePosition;
};

void SG_MFPivotVerticalGradience_c9e909c1845b8ae4b9a11aa715963787_float(float _Offset, float _Length, Bindings_MFPivotVerticalGradience_c9e909c1845b8ae4b9a11aa715963787_float IN, out float Mask_1)
{
float _Property_4680dca2dee245e7aa77915a0b17cce9_Out_0_Float = _Offset;
float _Property_c763914b9bbb4da5abad7a3db51adcb2_Out_0_Float = _Length;
float _Add_ad1531c9af874822b29fd9a13421289e_Out_2_Float;
Unity_Add_float(_Property_4680dca2dee245e7aa77915a0b17cce9_Out_0_Float, _Property_c763914b9bbb4da5abad7a3db51adcb2_Out_0_Float, _Add_ad1531c9af874822b29fd9a13421289e_Out_2_Float);
float3 _Subtract_69e80d3fb1bb4d71b4be84a010b280ff_Out_2_Vector3;
Unity_Subtract_float3(IN.WorldSpacePosition, SHADERGRAPH_OBJECT_POSITION, _Subtract_69e80d3fb1bb4d71b4be84a010b280ff_Out_2_Vector3);
float _Split_bceff54ddef34fab8cfa9a2a0b8bbc58_R_1_Float = _Subtract_69e80d3fb1bb4d71b4be84a010b280ff_Out_2_Vector3[0];
float _Split_bceff54ddef34fab8cfa9a2a0b8bbc58_G_2_Float = _Subtract_69e80d3fb1bb4d71b4be84a010b280ff_Out_2_Vector3[1];
float _Split_bceff54ddef34fab8cfa9a2a0b8bbc58_B_3_Float = _Subtract_69e80d3fb1bb4d71b4be84a010b280ff_Out_2_Vector3[2];
float _Split_bceff54ddef34fab8cfa9a2a0b8bbc58_A_4_Float = 0;
float _Smoothstep_0109929c8f9e41888dfa7ee4fd85c377_Out_3_Float;
Unity_Smoothstep_float(_Property_4680dca2dee245e7aa77915a0b17cce9_Out_0_Float, _Add_ad1531c9af874822b29fd9a13421289e_Out_2_Float, _Split_bceff54ddef34fab8cfa9a2a0b8bbc58_G_2_Float, _Smoothstep_0109929c8f9e41888dfa7ee4fd85c377_Out_3_Float);
Mask_1 = _Smoothstep_0109929c8f9e41888dfa7ee4fd85c377_Out_3_Float;
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
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
UnityTexture2D _Property_82391a4213d745e08202065048659634_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_1_Color_Map, sampler_Layer_1_Color_Map, _Layer_1_Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_7afe6a7edf62460887e09c4746d796b6_Out_0_Vector4 = IN.uv0;
float3 _Multiply_15b5d23699554ab298d90333ce57d387_Out_2_Vector3;
Unity_Multiply_float3_float3((_UV_7afe6a7edf62460887e09c4746d796b6_Out_0_Vector4.xyz), float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z))), _Multiply_15b5d23699554ab298d90333ce57d387_Out_2_Vector3);
float _Property_9a5b0bf1bf584876a406e2b633741c94_Out_0_Float = _Scale_UVs_with_Object;
float3 _Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3;
Unity_Lerp_float3((_UV_7afe6a7edf62460887e09c4746d796b6_Out_0_Vector4.xyz), _Multiply_15b5d23699554ab298d90333ce57d387_Out_2_Vector3, (_Property_9a5b0bf1bf584876a406e2b633741c94_Out_0_Float.xxx), _Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3);
float _Property_460238447f6b4170aa7397df1262be96_Out_0_Float = _Layer_1_Tiling;
float3 _Multiply_94cb4adbcc9a4b25980974e6be235287_Out_2_Vector3;
Unity_Multiply_float3_float3(_Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3, (_Property_460238447f6b4170aa7397df1262be96_Out_0_Float.xxx), _Multiply_94cb4adbcc9a4b25980974e6be235287_Out_2_Vector3);
float4 _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_82391a4213d745e08202065048659634_Out_0_Texture2D.tex, _Property_82391a4213d745e08202065048659634_Out_0_Texture2D.samplerstate, _Property_82391a4213d745e08202065048659634_Out_0_Texture2D.GetTransformedUV((_Multiply_94cb4adbcc9a4b25980974e6be235287_Out_2_Vector3.xy)) );
if (_Property_82391a4213d745e08202065048659634_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4, _Property_82391a4213d745e08202065048659634_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_R_4_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4.r;
float _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_G_5_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4.g;
float _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_B_6_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4.b;
float _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_A_7_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4.a;
float _Split_03d41d27a027425fa76a6c910dfbed96_R_1_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4[0];
float _Split_03d41d27a027425fa76a6c910dfbed96_G_2_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4[1];
float _Split_03d41d27a027425fa76a6c910dfbed96_B_3_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4[2];
float _Split_03d41d27a027425fa76a6c910dfbed96_A_4_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4[3];
float3 _Vector3_29d44982f90c486b8d58d91423294375_Out_0_Vector3 = float3(_Split_03d41d27a027425fa76a6c910dfbed96_R_1_Float, _Split_03d41d27a027425fa76a6c910dfbed96_G_2_Float, _Split_03d41d27a027425fa76a6c910dfbed96_B_3_Float);
float _Property_c55f5c80dc494683850ea3803571112b_Out_0_Float = _Layer_1_Hue;
float3 _Hue_4528903dcb5b4b15be0e273df247bfd9_Out_2_Vector3;
Unity_Hue_Normalized_float(_Vector3_29d44982f90c486b8d58d91423294375_Out_0_Vector3, _Property_c55f5c80dc494683850ea3803571112b_Out_0_Float, _Hue_4528903dcb5b4b15be0e273df247bfd9_Out_2_Vector3);
float _Property_ba7720a9d8134eb58030eb416c6a024a_Out_0_Float = _Layer_1_Brightness;
float3 _Multiply_01d7b815489a446fa8f5fbd7bf096d4f_Out_2_Vector3;
Unity_Multiply_float3_float3(_Hue_4528903dcb5b4b15be0e273df247bfd9_Out_2_Vector3, (_Property_ba7720a9d8134eb58030eb416c6a024a_Out_0_Float.xxx), _Multiply_01d7b815489a446fa8f5fbd7bf096d4f_Out_2_Vector3);
UnityTexture2D _Property_f37e4daa8ff94f5eb2ca9f6f83a20bb3_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_2_Color_Map, sampler_Layer_2_Color_Map, _Layer_2_Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_812f9793e99740fcb534b8c8d8af0ff2_Out_0_Float = _Layer_2_Tiling;
float3 _Multiply_7719c519f67d4b1aaaf2cca1cc2d4d79_Out_2_Vector3;
Unity_Multiply_float3_float3(_Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3, (_Property_812f9793e99740fcb534b8c8d8af0ff2_Out_0_Float.xxx), _Multiply_7719c519f67d4b1aaaf2cca1cc2d4d79_Out_2_Vector3);
float4 _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_f37e4daa8ff94f5eb2ca9f6f83a20bb3_Out_0_Texture2D.tex, _Property_f37e4daa8ff94f5eb2ca9f6f83a20bb3_Out_0_Texture2D.samplerstate, _Property_f37e4daa8ff94f5eb2ca9f6f83a20bb3_Out_0_Texture2D.GetTransformedUV((_Multiply_7719c519f67d4b1aaaf2cca1cc2d4d79_Out_2_Vector3.xy)) );
if (_Property_f37e4daa8ff94f5eb2ca9f6f83a20bb3_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4, _Property_f37e4daa8ff94f5eb2ca9f6f83a20bb3_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_R_4_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4.r;
float _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_G_5_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4.g;
float _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_B_6_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4.b;
float _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_A_7_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4.a;
float _Split_d38832e5c62c44d3a795b83acceab016_R_1_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4[0];
float _Split_d38832e5c62c44d3a795b83acceab016_G_2_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4[1];
float _Split_d38832e5c62c44d3a795b83acceab016_B_3_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4[2];
float _Split_d38832e5c62c44d3a795b83acceab016_A_4_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4[3];
float3 _Vector3_cb2069d38949469a976ac3a5a2003bd7_Out_0_Vector3 = float3(_Split_d38832e5c62c44d3a795b83acceab016_R_1_Float, _Split_d38832e5c62c44d3a795b83acceab016_G_2_Float, _Split_d38832e5c62c44d3a795b83acceab016_B_3_Float);
float _Property_8970d5f05c7d4b5783a57e1c656ee4a9_Out_0_Float = _Layer_2_Hue;
float3 _Hue_d8383543ffa64d25a0efd5fc36520be4_Out_2_Vector3;
Unity_Hue_Normalized_float(_Vector3_cb2069d38949469a976ac3a5a2003bd7_Out_0_Vector3, _Property_8970d5f05c7d4b5783a57e1c656ee4a9_Out_0_Float, _Hue_d8383543ffa64d25a0efd5fc36520be4_Out_2_Vector3);
float _Property_40475d05242a4efaa3e83e117ecd268d_Out_0_Float = _Layer_2_Brightness;
float3 _Multiply_9a8ec1bf43a149a7a5fb1571321b8200_Out_2_Vector3;
Unity_Multiply_float3_float3(_Hue_d8383543ffa64d25a0efd5fc36520be4_Out_2_Vector3, (_Property_40475d05242a4efaa3e83e117ecd268d_Out_0_Float.xxx), _Multiply_9a8ec1bf43a149a7a5fb1571321b8200_Out_2_Vector3);
UnityTexture2D _Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_RGB_Map, sampler_RGB_Map, _RGB_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D.tex, _Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D.samplerstate, _Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4, _Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_R_4_Float = _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4.r;
float _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_G_5_Float = _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4.g;
float _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_B_6_Float = _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4.b;
float _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_A_7_Float = _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4.a;
float3 _Lerp_754b59c21e034e87848bb65af4294c76_Out_3_Vector3;
Unity_Lerp_float3(_Multiply_01d7b815489a446fa8f5fbd7bf096d4f_Out_2_Vector3, _Multiply_9a8ec1bf43a149a7a5fb1571321b8200_Out_2_Vector3, (_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_G_5_Float.xxx), _Lerp_754b59c21e034e87848bb65af4294c76_Out_3_Vector3);
UnityTexture2D _Property_fef134089dc1401db12f51526b8ed988_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_3_Color_Map, sampler_Layer_3_Color_Map, _Layer_3_Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_8d20917a655e4062a5ce52f974969006_Out_0_Float = _Layer_3_Tiling;
float3 _Multiply_04b9ab405b2d4c27b6dd0696d9a5eacf_Out_2_Vector3;
Unity_Multiply_float3_float3(_Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3, (_Property_8d20917a655e4062a5ce52f974969006_Out_0_Float.xxx), _Multiply_04b9ab405b2d4c27b6dd0696d9a5eacf_Out_2_Vector3);
float4 _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_fef134089dc1401db12f51526b8ed988_Out_0_Texture2D.tex, _Property_fef134089dc1401db12f51526b8ed988_Out_0_Texture2D.samplerstate, _Property_fef134089dc1401db12f51526b8ed988_Out_0_Texture2D.GetTransformedUV((_Multiply_04b9ab405b2d4c27b6dd0696d9a5eacf_Out_2_Vector3.xy)) );
if (_Property_fef134089dc1401db12f51526b8ed988_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4, _Property_fef134089dc1401db12f51526b8ed988_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_R_4_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4.r;
float _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_G_5_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4.g;
float _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_B_6_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4.b;
float _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_A_7_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4.a;
float _Split_838946d3ce6f42cfbaf41fff836d6985_R_1_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4[0];
float _Split_838946d3ce6f42cfbaf41fff836d6985_G_2_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4[1];
float _Split_838946d3ce6f42cfbaf41fff836d6985_B_3_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4[2];
float _Split_838946d3ce6f42cfbaf41fff836d6985_A_4_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4[3];
float3 _Vector3_2a694d435907494fb07f2f9db09d031a_Out_0_Vector3 = float3(_Split_838946d3ce6f42cfbaf41fff836d6985_R_1_Float, _Split_838946d3ce6f42cfbaf41fff836d6985_G_2_Float, _Split_838946d3ce6f42cfbaf41fff836d6985_B_3_Float);
float _Property_6ba5eab2fbda4899981bd9c4c9c6317e_Out_0_Float = _Layer_3_Hue;
float3 _Hue_0a936fcae1974948a815da96c316b7e3_Out_2_Vector3;
Unity_Hue_Normalized_float(_Vector3_2a694d435907494fb07f2f9db09d031a_Out_0_Vector3, _Property_6ba5eab2fbda4899981bd9c4c9c6317e_Out_0_Float, _Hue_0a936fcae1974948a815da96c316b7e3_Out_2_Vector3);
float _Property_c749f04d24d9423fa1a2859f800dc893_Out_0_Float = _Layer_3_Brightness;
float3 _Multiply_db73d490dc9d432e80685ef18e5427fb_Out_2_Vector3;
Unity_Multiply_float3_float3(_Hue_0a936fcae1974948a815da96c316b7e3_Out_2_Vector3, (_Property_c749f04d24d9423fa1a2859f800dc893_Out_0_Float.xxx), _Multiply_db73d490dc9d432e80685ef18e5427fb_Out_2_Vector3);
float3 _Lerp_66f917f82ed14fd89c6c2fe066b5e834_Out_3_Vector3;
Unity_Lerp_float3(_Lerp_754b59c21e034e87848bb65af4294c76_Out_3_Vector3, _Multiply_db73d490dc9d432e80685ef18e5427fb_Out_2_Vector3, (_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_B_6_Float.xxx), _Lerp_66f917f82ed14fd89c6c2fe066b5e834_Out_3_Vector3);
#if defined(_USE_LAYER_3)
float3 _UseLayer3_ee2b565dd7774fa7a5c88d0b7bfd05b8_Out_0_Vector3 = _Lerp_66f917f82ed14fd89c6c2fe066b5e834_Out_3_Vector3;
#else
float3 _UseLayer3_ee2b565dd7774fa7a5c88d0b7bfd05b8_Out_0_Vector3 = _Lerp_754b59c21e034e87848bb65af4294c76_Out_3_Vector3;
#endif
#if defined(_USE_LAYER_2)
float3 _UseLayer2_22b88f361b80488fbbec2df7f45c4924_Out_0_Vector3 = _UseLayer3_ee2b565dd7774fa7a5c88d0b7bfd05b8_Out_0_Vector3;
#else
float3 _UseLayer2_22b88f361b80488fbbec2df7f45c4924_Out_0_Vector3 = _Multiply_01d7b815489a446fa8f5fbd7bf096d4f_Out_2_Vector3;
#endif
float4 _Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4 = _Stylization_Color;
float _Split_42e867e97f454925a658dba60e21864f_R_1_Float = _Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4[0];
float _Split_42e867e97f454925a658dba60e21864f_G_2_Float = _Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4[1];
float _Split_42e867e97f454925a658dba60e21864f_B_3_Float = _Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4[2];
float _Split_42e867e97f454925a658dba60e21864f_A_4_Float = _Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4[3];
UnityTexture2D _Property_c02e202098944eebbd44663588c64e3b_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Edge_Map, sampler_Edge_Map, _Edge_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_c02e202098944eebbd44663588c64e3b_Out_0_Texture2D.tex, _Property_c02e202098944eebbd44663588c64e3b_Out_0_Texture2D.samplerstate, _Property_c02e202098944eebbd44663588c64e3b_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_c02e202098944eebbd44663588c64e3b_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4, _Property_c02e202098944eebbd44663588c64e3b_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_R_4_Float = _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4.r;
float _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_G_5_Float = _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4.g;
float _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_B_6_Float = _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4.b;
float _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_A_7_Float = _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4.a;
float _Property_a4422d37671342bdab8d700fe4c2424a_Out_0_Float = _Stylization_Mask_Contrast;
Bindings_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float _MFCheapContrast_2e3888093b39403781e2ccbee9980699;
float _MFCheapContrast_2e3888093b39403781e2ccbee9980699_Out_1_Float;
SG_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float(_SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_A_7_Float, _Property_a4422d37671342bdab8d700fe4c2424a_Out_0_Float, _MFCheapContrast_2e3888093b39403781e2ccbee9980699, _MFCheapContrast_2e3888093b39403781e2ccbee9980699_Out_1_Float);
float _Multiply_364f0f8b2ff74453b42f050a8b1cb526_Out_2_Float;
Unity_Multiply_float_float(_Split_42e867e97f454925a658dba60e21864f_A_4_Float, _MFCheapContrast_2e3888093b39403781e2ccbee9980699_Out_1_Float, _Multiply_364f0f8b2ff74453b42f050a8b1cb526_Out_2_Float);
float3 _Lerp_0a1e51abb320477ab63b42e1a32052cc_Out_3_Vector3;
Unity_Lerp_float3(_UseLayer2_22b88f361b80488fbbec2df7f45c4924_Out_0_Vector3, (_Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4.xyz), (_Multiply_364f0f8b2ff74453b42f050a8b1cb526_Out_2_Float.xxx), _Lerp_0a1e51abb320477ab63b42e1a32052cc_Out_3_Vector3);
#if defined(_USE_LAYER_1)
float3 _UseLayer1_f5f8cf003ae64c4187983dc616ecbd3b_Out_0_Vector3 = _Lerp_0a1e51abb320477ab63b42e1a32052cc_Out_3_Vector3;
#else
float3 _UseLayer1_f5f8cf003ae64c4187983dc616ecbd3b_Out_0_Vector3 = (_Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4.xyz);
#endif
float4 _Property_0be88f8e5ba94765a3623024be14bbbf_Out_0_Vector4 = _AO_Color;
float _Split_07ec24369bd1411c82179cabd24aabe5_R_1_Float = _Property_0be88f8e5ba94765a3623024be14bbbf_Out_0_Vector4[0];
float _Split_07ec24369bd1411c82179cabd24aabe5_G_2_Float = _Property_0be88f8e5ba94765a3623024be14bbbf_Out_0_Vector4[1];
float _Split_07ec24369bd1411c82179cabd24aabe5_B_3_Float = _Property_0be88f8e5ba94765a3623024be14bbbf_Out_0_Vector4[2];
float _Split_07ec24369bd1411c82179cabd24aabe5_A_4_Float = _Property_0be88f8e5ba94765a3623024be14bbbf_Out_0_Vector4[3];
float _Property_781496b67f424345bca22ad901512267_Out_0_Float = _AO_Mask_Contrast;
Bindings_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float _MFCheapContrast_74522f75ddab433f9ec9f709fe5a6faf;
float _MFCheapContrast_74522f75ddab433f9ec9f709fe5a6faf_Out_1_Float;
SG_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float(_SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_G_5_Float, _Property_781496b67f424345bca22ad901512267_Out_0_Float, _MFCheapContrast_74522f75ddab433f9ec9f709fe5a6faf, _MFCheapContrast_74522f75ddab433f9ec9f709fe5a6faf_Out_1_Float);
float _Multiply_27eebb10636649c3aefb6ad13580fd9e_Out_2_Float;
Unity_Multiply_float_float(_Split_07ec24369bd1411c82179cabd24aabe5_A_4_Float, _MFCheapContrast_74522f75ddab433f9ec9f709fe5a6faf_Out_1_Float, _Multiply_27eebb10636649c3aefb6ad13580fd9e_Out_2_Float);
float3 _Blend_5baeb7dcebe145dcb53bba406ea82ebd_Out_2_Vector3;
Unity_Blend_Multiply_float3(_UseLayer1_f5f8cf003ae64c4187983dc616ecbd3b_Out_0_Vector3, (_Property_0be88f8e5ba94765a3623024be14bbbf_Out_0_Vector4.xyz), _Blend_5baeb7dcebe145dcb53bba406ea82ebd_Out_2_Vector3, _Multiply_27eebb10636649c3aefb6ad13580fd9e_Out_2_Float);
float4 _Property_64075cb2946e4bc89c0928961fa8e759_Out_0_Vector4 = _Dirt_Color;
float _Split_102b0005351f49a0a7ec214d4c4e879e_R_1_Float = _Property_64075cb2946e4bc89c0928961fa8e759_Out_0_Vector4[0];
float _Split_102b0005351f49a0a7ec214d4c4e879e_G_2_Float = _Property_64075cb2946e4bc89c0928961fa8e759_Out_0_Vector4[1];
float _Split_102b0005351f49a0a7ec214d4c4e879e_B_3_Float = _Property_64075cb2946e4bc89c0928961fa8e759_Out_0_Vector4[2];
float _Split_102b0005351f49a0a7ec214d4c4e879e_A_4_Float = _Property_64075cb2946e4bc89c0928961fa8e759_Out_0_Vector4[3];
float _Property_089f6755c08b483d971238d917dd8b3a_Out_0_Float = _Dirt_Mask_Contrast;
Bindings_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float _MFCheapContrast_96f7295ada8247d48955ac898a7aba11;
float _MFCheapContrast_96f7295ada8247d48955ac898a7aba11_Out_1_Float;
SG_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float(_SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_R_4_Float, _Property_089f6755c08b483d971238d917dd8b3a_Out_0_Float, _MFCheapContrast_96f7295ada8247d48955ac898a7aba11, _MFCheapContrast_96f7295ada8247d48955ac898a7aba11_Out_1_Float);
float _Multiply_bc33d3e78b104e93a60b60cea85cebac_Out_2_Float;
Unity_Multiply_float_float(_Split_102b0005351f49a0a7ec214d4c4e879e_A_4_Float, _MFCheapContrast_96f7295ada8247d48955ac898a7aba11_Out_1_Float, _Multiply_bc33d3e78b104e93a60b60cea85cebac_Out_2_Float);
float3 _Lerp_ef18ae06dc3e4fd18201e3925bc55a85_Out_3_Vector3;
Unity_Lerp_float3(_Blend_5baeb7dcebe145dcb53bba406ea82ebd_Out_2_Vector3, (_Property_64075cb2946e4bc89c0928961fa8e759_Out_0_Vector4.xyz), (_Multiply_bc33d3e78b104e93a60b60cea85cebac_Out_2_Float.xxx), _Lerp_ef18ae06dc3e4fd18201e3925bc55a85_Out_3_Vector3);
float4 _Property_dbda4c8e93f54f92a18b3390cbaf06c0_Out_0_Vector4 = _Edgewear_Color;
float _Split_925676a8ce21420aad811d255e389512_R_1_Float = _Property_dbda4c8e93f54f92a18b3390cbaf06c0_Out_0_Vector4[0];
float _Split_925676a8ce21420aad811d255e389512_G_2_Float = _Property_dbda4c8e93f54f92a18b3390cbaf06c0_Out_0_Vector4[1];
float _Split_925676a8ce21420aad811d255e389512_B_3_Float = _Property_dbda4c8e93f54f92a18b3390cbaf06c0_Out_0_Vector4[2];
float _Split_925676a8ce21420aad811d255e389512_A_4_Float = _Property_dbda4c8e93f54f92a18b3390cbaf06c0_Out_0_Vector4[3];
float _Property_4bef2cb688474ef8909e6dc3f2439616_Out_0_Float = _Edgewear_Mask_Contrast;
Bindings_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float _MFCheapContrast_c8d787e56cf842869980c82103f39754;
float _MFCheapContrast_c8d787e56cf842869980c82103f39754_Out_1_Float;
SG_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float(_SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_B_6_Float, _Property_4bef2cb688474ef8909e6dc3f2439616_Out_0_Float, _MFCheapContrast_c8d787e56cf842869980c82103f39754, _MFCheapContrast_c8d787e56cf842869980c82103f39754_Out_1_Float);
float _Multiply_38b6bc16bfef4cd3bec7370aabb6a836_Out_2_Float;
Unity_Multiply_float_float(_Split_925676a8ce21420aad811d255e389512_A_4_Float, _MFCheapContrast_c8d787e56cf842869980c82103f39754_Out_1_Float, _Multiply_38b6bc16bfef4cd3bec7370aabb6a836_Out_2_Float);
float3 _Blend_e613dc790cd741d3b367cb48289cffd5_Out_2_Vector3;
Unity_Blend_Overlay_float3(_Lerp_ef18ae06dc3e4fd18201e3925bc55a85_Out_3_Vector3, (_Property_dbda4c8e93f54f92a18b3390cbaf06c0_Out_0_Vector4.xyz), _Blend_e613dc790cd741d3b367cb48289cffd5_Out_2_Vector3, _Multiply_38b6bc16bfef4cd3bec7370aabb6a836_Out_2_Float);
UnityTexture2D _Property_003a6ee6678543e5964d3e811c19f789_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Coverage_Color_Map, sampler_Coverage_Color_Map, _Coverage_Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_d9f12d2b2464447a82f581c14778bbf8_Out_0_Float = _Coverage_Tiling;
float3 _Multiply_282d032113614cb58779c3a33671ad6c_Out_2_Vector3;
Unity_Multiply_float3_float3(_Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3, (_Property_d9f12d2b2464447a82f581c14778bbf8_Out_0_Float.xxx), _Multiply_282d032113614cb58779c3a33671ad6c_Out_2_Vector3);
float4 _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_003a6ee6678543e5964d3e811c19f789_Out_0_Texture2D.tex, _Property_003a6ee6678543e5964d3e811c19f789_Out_0_Texture2D.samplerstate, _Property_003a6ee6678543e5964d3e811c19f789_Out_0_Texture2D.GetTransformedUV((_Multiply_282d032113614cb58779c3a33671ad6c_Out_2_Vector3.xy)) );
if (_Property_003a6ee6678543e5964d3e811c19f789_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4, _Property_003a6ee6678543e5964d3e811c19f789_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_R_4_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4.r;
float _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_G_5_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4.g;
float _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_B_6_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4.b;
float _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_A_7_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4.a;
float _Split_42bc736ed19d41c2bc8a58c7f653b802_R_1_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4[0];
float _Split_42bc736ed19d41c2bc8a58c7f653b802_G_2_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4[1];
float _Split_42bc736ed19d41c2bc8a58c7f653b802_B_3_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4[2];
float _Split_42bc736ed19d41c2bc8a58c7f653b802_A_4_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4[3];
float3 _Vector3_dd63cc8dd9a443a99a6ecc681ff6961e_Out_0_Vector3 = float3(_Split_42bc736ed19d41c2bc8a58c7f653b802_R_1_Float, _Split_42bc736ed19d41c2bc8a58c7f653b802_G_2_Float, _Split_42bc736ed19d41c2bc8a58c7f653b802_B_3_Float);
float _Property_86cbded0425a4bf2b5441e665a7f5551_Out_0_Float = _Coverage_Hue;
float3 _Hue_5e168e8c10044fe09120668e4b870b44_Out_2_Vector3;
Unity_Hue_Normalized_float(_Vector3_dd63cc8dd9a443a99a6ecc681ff6961e_Out_0_Vector3, _Property_86cbded0425a4bf2b5441e665a7f5551_Out_0_Float, _Hue_5e168e8c10044fe09120668e4b870b44_Out_2_Vector3);
float _Property_dccedc571d6d411291da660d59f31596_Out_0_Float = _Coverage_Brightness;
float3 _Multiply_c28f28e86afd43f584c04c39cc44c8c4_Out_2_Vector3;
Unity_Multiply_float3_float3(_Hue_5e168e8c10044fe09120668e4b870b44_Out_2_Vector3, (_Property_dccedc571d6d411291da660d59f31596_Out_0_Float.xxx), _Multiply_c28f28e86afd43f584c04c39cc44c8c4_Out_2_Vector3);
float _Property_f13be210d8b049a9a1e6c0e219f5a53f_Out_0_Float = _Coverage_Normal_Mask_Slope;
float _Property_1f786f31d0924f6ba0904b90319c56e3_Out_0_Float = _Coverage_Normal_Mask_Gradience;
float _Add_4177ca63c9724385910d37f97d0e8c48_Out_2_Float;
Unity_Add_float(_Property_f13be210d8b049a9a1e6c0e219f5a53f_Out_0_Float, _Property_1f786f31d0924f6ba0904b90319c56e3_Out_0_Float, _Add_4177ca63c9724385910d37f97d0e8c48_Out_2_Float);
UnityTexture2D _Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_1_Normal_Map, sampler_Layer_1_Normal_Map, _Layer_1_Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D.tex, _Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D.samplerstate, _Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D.GetTransformedUV((_Multiply_94cb4adbcc9a4b25980974e6be235287_Out_2_Vector3.xy)) );
if (_Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4, _Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4);
float _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_R_4_Float = _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.r;
float _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_G_5_Float = _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.g;
float _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_B_6_Float = _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.b;
float _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_A_7_Float = _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.a;
float _Property_7cfa9a8e94684810a4cd4c70a2257b6c_Out_0_Float = _Layer_1_Normal_Strength;
float3 _NormalStrength_9428e53c24e74ce189625604c4b60275_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.xyz), _Property_7cfa9a8e94684810a4cd4c70a2257b6c_Out_0_Float, _NormalStrength_9428e53c24e74ce189625604c4b60275_Out_2_Vector3);
UnityTexture2D _Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_2_Normal_Map, sampler_Layer_2_Normal_Map, _Layer_2_Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D.tex, _Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D.samplerstate, _Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D.GetTransformedUV((_Multiply_7719c519f67d4b1aaaf2cca1cc2d4d79_Out_2_Vector3.xy)) );
if (_Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4, _Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4);
float _SampleTexture2D_ae5b372472874ad28eac881821ce553c_R_4_Float = _SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.r;
float _SampleTexture2D_ae5b372472874ad28eac881821ce553c_G_5_Float = _SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.g;
float _SampleTexture2D_ae5b372472874ad28eac881821ce553c_B_6_Float = _SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.b;
float _SampleTexture2D_ae5b372472874ad28eac881821ce553c_A_7_Float = _SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.a;
float _Property_12c65401b09f44daa763974826741097_Out_0_Float = _Layer_2_Normal_Strength;
float3 _NormalStrength_93ad91a06836481ca4f36c947e62a393_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.xyz), _Property_12c65401b09f44daa763974826741097_Out_0_Float, _NormalStrength_93ad91a06836481ca4f36c947e62a393_Out_2_Vector3);
float3 _Lerp_7325f1720917490cb13328e58ddd8efe_Out_3_Vector3;
Unity_Lerp_float3(_NormalStrength_9428e53c24e74ce189625604c4b60275_Out_2_Vector3, _NormalStrength_93ad91a06836481ca4f36c947e62a393_Out_2_Vector3, (_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_G_5_Float.xxx), _Lerp_7325f1720917490cb13328e58ddd8efe_Out_3_Vector3);
UnityTexture2D _Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_3_Normal_Map, sampler_Layer_3_Normal_Map, _Layer_3_Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D.tex, _Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D.samplerstate, _Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D.GetTransformedUV((_Multiply_04b9ab405b2d4c27b6dd0696d9a5eacf_Out_2_Vector3.xy)) );
if (_Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4, _Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4);
float _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_R_4_Float = _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.r;
float _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_G_5_Float = _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.g;
float _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_B_6_Float = _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.b;
float _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_A_7_Float = _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.a;
float _Property_02480e6b3805449397b2d5a15e416673_Out_0_Float = _Layer_3_Normal_Strength;
float3 _NormalStrength_8e30a22d9ed147ca9ef200087a7f43aa_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.xyz), _Property_02480e6b3805449397b2d5a15e416673_Out_0_Float, _NormalStrength_8e30a22d9ed147ca9ef200087a7f43aa_Out_2_Vector3);
float3 _Lerp_642d324ee8d74cf888dc943377359b6d_Out_3_Vector3;
Unity_Lerp_float3(_Lerp_7325f1720917490cb13328e58ddd8efe_Out_3_Vector3, _NormalStrength_8e30a22d9ed147ca9ef200087a7f43aa_Out_2_Vector3, (_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_B_6_Float.xxx), _Lerp_642d324ee8d74cf888dc943377359b6d_Out_3_Vector3);
#if defined(_USE_LAYER_3)
float3 _UseLayer3_3e504be39f154559868343947f457c24_Out_0_Vector3 = _Lerp_642d324ee8d74cf888dc943377359b6d_Out_3_Vector3;
#else
float3 _UseLayer3_3e504be39f154559868343947f457c24_Out_0_Vector3 = _Lerp_7325f1720917490cb13328e58ddd8efe_Out_3_Vector3;
#endif
#if defined(_USE_LAYER_2)
float3 _UseLayer2_c90f92a0b0134057867d0810829f779e_Out_0_Vector3 = _UseLayer3_3e504be39f154559868343947f457c24_Out_0_Vector3;
#else
float3 _UseLayer2_c90f92a0b0134057867d0810829f779e_Out_0_Vector3 = _NormalStrength_9428e53c24e74ce189625604c4b60275_Out_2_Vector3;
#endif
UnityTexture2D _Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Normal_Map, sampler_Normal_Map, _Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D.tex, _Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D.samplerstate, _Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4, _Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4);
float _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_R_4_Float = _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.r;
float _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_G_5_Float = _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.g;
float _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_B_6_Float = _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.b;
float _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_A_7_Float = _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.a;
float _Property_758b3d19a0c14ff099921db6234664b2_Out_0_Float = _Normal_Strength;
float3 _NormalStrength_3eb407934b854455a959c26453776991_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.xyz), _Property_758b3d19a0c14ff099921db6234664b2_Out_0_Float, _NormalStrength_3eb407934b854455a959c26453776991_Out_2_Vector3);
float3 _NormalBlend_cb4851ae94454d539b1004bdc1f2604f_Out_2_Vector3;
Unity_NormalBlend_float(_UseLayer2_c90f92a0b0134057867d0810829f779e_Out_0_Vector3, _NormalStrength_3eb407934b854455a959c26453776991_Out_2_Vector3, _NormalBlend_cb4851ae94454d539b1004bdc1f2604f_Out_2_Vector3);
#if defined(_USE_LAYER_1)
float3 _UseLayer1_7da71e644bf2434f99e8005bad9e8f14_Out_0_Vector3 = _NormalBlend_cb4851ae94454d539b1004bdc1f2604f_Out_2_Vector3;
#else
float3 _UseLayer1_7da71e644bf2434f99e8005bad9e8f14_Out_0_Vector3 = _NormalStrength_3eb407934b854455a959c26453776991_Out_2_Vector3;
#endif
float3 _Transform_04398ac1bc954fb19c62ce3f5f6d670c_Out_1_Vector3;
{
float3x3 tangentTransform = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
_Transform_04398ac1bc954fb19c62ce3f5f6d670c_Out_1_Vector3 = TransformTangentToWorld(_UseLayer1_7da71e644bf2434f99e8005bad9e8f14_Out_0_Vector3.xyz, tangentTransform, true);
}
float _DotProduct_ae13069c870e4539be3826642299bede_Out_2_Float;
Unity_DotProduct_float3(_Transform_04398ac1bc954fb19c62ce3f5f6d670c_Out_1_Vector3, float3(0, 1, 0), _DotProduct_ae13069c870e4539be3826642299bede_Out_2_Float);
float _Smoothstep_652b53e75f9e481e8c829d3bd3d9c24e_Out_3_Float;
Unity_Smoothstep_float(_Property_f13be210d8b049a9a1e6c0e219f5a53f_Out_0_Float, _Add_4177ca63c9724385910d37f97d0e8c48_Out_2_Float, _DotProduct_ae13069c870e4539be3826642299bede_Out_2_Float, _Smoothstep_652b53e75f9e481e8c829d3bd3d9c24e_Out_3_Float);
float _Property_64938267e5134c5b9a4256d11aa25705_Out_0_Float = _Coverage_Height_Mask_Pivot_Offset;
float _Property_cfcce6f026924b048f8c33bdc73c14f6_Out_0_Float = _Coverage_Height_Mask_Length;
Bindings_MFPivotVerticalGradience_c9e909c1845b8ae4b9a11aa715963787_float _MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c;
_MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c.WorldSpacePosition = IN.WorldSpacePosition;
float _MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c_Mask_1_Float;
SG_MFPivotVerticalGradience_c9e909c1845b8ae4b9a11aa715963787_float(_Property_64938267e5134c5b9a4256d11aa25705_Out_0_Float, _Property_cfcce6f026924b048f8c33bdc73c14f6_Out_0_Float, _MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c, _MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c_Mask_1_Float);
float _Multiply_f6a21f2d15b7472e8173002af669cfdf_Out_2_Float;
Unity_Multiply_float_float(_Smoothstep_652b53e75f9e481e8c829d3bd3d9c24e_Out_3_Float, _MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c_Mask_1_Float, _Multiply_f6a21f2d15b7472e8173002af669cfdf_Out_2_Float);
float3 _Lerp_90a44aeed76b4e67b245ea63960c28fc_Out_3_Vector3;
Unity_Lerp_float3(_Blend_e613dc790cd741d3b367cb48289cffd5_Out_2_Vector3, _Multiply_c28f28e86afd43f584c04c39cc44c8c4_Out_2_Vector3, (_Multiply_f6a21f2d15b7472e8173002af669cfdf_Out_2_Float.xxx), _Lerp_90a44aeed76b4e67b245ea63960c28fc_Out_3_Vector3);
#if defined(_USE_COVERAGE_LAYER)
float3 _UseCoverageLayer_402efe74563f417bb22b7997bdfc5c1e_Out_0_Vector3 = _Lerp_90a44aeed76b4e67b245ea63960c28fc_Out_3_Vector3;
#else
float3 _UseCoverageLayer_402efe74563f417bb22b7997bdfc5c1e_Out_0_Vector3 = _Blend_e613dc790cd741d3b367cb48289cffd5_Out_2_Vector3;
#endif
surface.BaseColor = _UseCoverageLayer_402efe74563f417bb22b7997bdfc5c1e_Out_0_Vector3;
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

    // to pr               eserve mikktspace compliance we use same scale renormFactor as was used on the normal.
    // This                is explained in section 2.2 in "surface gradient based bump mapping framework"
    output.WorldSpaceTangent = renormFactor * input.tangentWS.xyz;
    output.WorldSpaceBiTangent = renormFactor * bitang;

    output.WorldSpacePosition = input.positionWS;

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
#pragma shader_feature_local _ _USE_LAYER_2
#pragma shader_feature_local _ _USE_LAYER_3
#pragma shader_feature_local _ _USE_COVERAGE_LAYER
#pragma shader_feature_local _ _USE_LAYER_1

#if defined(_USE_LAYER_2) && defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_3) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_2
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_3)
#define KEYWORD_PERMUTATION_3
#elif defined(_USE_LAYER_2) && defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_4
#elif defined(_USE_LAYER_2) && defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_5
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_6
#elif defined(_USE_LAYER_2)
#define KEYWORD_PERMUTATION_7
#elif defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_8
#elif defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_9
#elif defined(_USE_LAYER_3) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_10
#elif defined(_USE_LAYER_3)
#define KEYWORD_PERMUTATION_11
#elif defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_12
#elif defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_13
#elif defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_14
#else
#define KEYWORD_PERMUTATION_15
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
 float3 WorldSpaceNormal;
 float3 WorldSpaceTangent;
 float3 WorldSpaceBiTangent;
 float3 WorldSpacePosition;
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

float4 _Layer_1_Color_Map_TexelSize;
float _Layer_1_Hue;
float _Layer_1_Brightness;
float _Coverage_Height_Mask_Length;
float4 _Layer_1_Normal_Map_TexelSize;
float _Layer_1_Normal_Strength;
float _Layer_1_Tiling;
float _Layer_1_Smoothness_Strength;
float _Layer_2_Tiling;
float4 _Layer_2_Color_Map_TexelSize;
float _Layer_2_Hue;
float _Layer_2_Brightness;
float _Layer_2_Smoothness_Strength;
float4 _Layer_2_Normal_Map_TexelSize;
float _Layer_2_Normal_Strength;
float _Layer_3_Tiling;
float4 _Layer_3_Color_Map_TexelSize;
float _Layer_3_Hue;
float _Layer_3_Brightness;
float _Layer_3_Smoothness_Strength;
float4 _Layer_3_Normal_Map_TexelSize;
float _Layer_3_Normal_Strength;
float4 _RGB_Map_TexelSize;
float4 _Normal_Map_TexelSize;
float _Normal_Strength;
float _Coverage_Tiling;
float4 _Coverage_Color_Map_TexelSize;
float _Coverage_Hue;
float _Coverage_Brightness;
float _Coverage_Smoothness_Strength;
float4 _Coverage_Normal_Map_TexelSize;
float _Coverage_Normal_Strength;
float4 _Edge_Map_TexelSize;
float _Dirt_Mask_Contrast;
float _AO_Mask_Contrast;
float _Edgewear_Mask_Contrast;
float _Stylization_Mask_Contrast;
float4 _Stylization_Color;
float4 _AO_Color;
float4 _Dirt_Color;
float4 _Edgewear_Color;
float _Coverage_Normal_Mask_Slope;
float _Coverage_Normal_Mask_Gradience;
float _Coverage_Height_Mask_Pivot_Offset;
float _Scale_UVs_with_Object;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Layer_1_Color_Map);
SAMPLER(sampler_Layer_1_Color_Map);
TEXTURE2D(_Layer_1_Normal_Map);
SAMPLER(sampler_Layer_1_Normal_Map);
TEXTURE2D(_Layer_2_Color_Map);
SAMPLER(sampler_Layer_2_Color_Map);
TEXTURE2D(_Layer_2_Normal_Map);
SAMPLER(sampler_Layer_2_Normal_Map);
TEXTURE2D(_Layer_3_Color_Map);
SAMPLER(sampler_Layer_3_Color_Map);
TEXTURE2D(_Layer_3_Normal_Map);
SAMPLER(sampler_Layer_3_Normal_Map);
TEXTURE2D(_RGB_Map);
SAMPLER(sampler_RGB_Map);
TEXTURE2D(_Normal_Map);
SAMPLER(sampler_Normal_Map);
TEXTURE2D(_Coverage_Color_Map);
SAMPLER(sampler_Coverage_Color_Map);
TEXTURE2D(_Coverage_Normal_Map);
SAMPLER(sampler_Coverage_Normal_Map);
TEXTURE2D(_Edge_Map);
SAMPLER(sampler_Edge_Map);

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

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Hue_Normalized_float(float3 In, float Offset, out float3 Out)
{
    // RGB to HSV
    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 P = lerp(float4(In.bg, K.wz), float4(In.gb, K.xy), step(In.b, In.g));
    float4 Q = lerp(float4(P.xyw, In.r), float4(In.r, P.yzx), step(P.x, In.r));
    float D = Q.x - min(Q.w, Q.y);
    float E = 1e-10;
    float V = (D == 0) ? Q.x : (Q.x + E);
    float3 hsv = float3(abs(Q.z + (Q.w - Q.y)/(6.0 * D + E)), D / (Q.x + E), V);

    float hue = hsv.x + Offset;
    hsv.x = (hue < 0)
            ? hue + 1
            : (hue > 1)
                ? hue - 1
                : hue;

    // HSV to RGB
    float4 K2 = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 P2 = abs(frac(hsv.xxx + K2.xyz) * 6.0 - K2.www);
    Out = hsv.z * lerp(K2.xxx, saturate(P2 - K2.xxx), hsv.y);
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Lerp_float(float A, float B, float T, out float Out)
{
    Out = lerp(A, B, T);
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

struct Bindings_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float
{
};

void SG_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float(float _Input, float _Contrast, Bindings_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float IN, out float Out_1)
{
float _Property_f17f2d608b764773bc9f33ea28c3e14e_Out_0_Float = _Contrast;
float _Add_59838b3eb9ec437bbd93b6fd8db7a0c4_Out_2_Float;
Unity_Add_float(_Property_f17f2d608b764773bc9f33ea28c3e14e_Out_0_Float, float(-1), _Add_59838b3eb9ec437bbd93b6fd8db7a0c4_Out_2_Float);
float _Add_524e73a4aa0b4e7982442fdf0d39fe18_Out_2_Float;
Unity_Add_float(_Property_f17f2d608b764773bc9f33ea28c3e14e_Out_0_Float, float(1), _Add_524e73a4aa0b4e7982442fdf0d39fe18_Out_2_Float);
float _Property_df60db08578f4909bad9a81c68245e45_Out_0_Float = _Input;
float _Lerp_6af4dfb2e0f0426a81a1669c55ec8196_Out_3_Float;
Unity_Lerp_float(_Add_59838b3eb9ec437bbd93b6fd8db7a0c4_Out_2_Float, _Add_524e73a4aa0b4e7982442fdf0d39fe18_Out_2_Float, _Property_df60db08578f4909bad9a81c68245e45_Out_0_Float, _Lerp_6af4dfb2e0f0426a81a1669c55ec8196_Out_3_Float);
float _Saturate_59b070c1298d4279be831e68d2cd96c8_Out_1_Float;
Unity_Saturate_float(_Lerp_6af4dfb2e0f0426a81a1669c55ec8196_Out_3_Float, _Saturate_59b070c1298d4279be831e68d2cd96c8_Out_1_Float);
Out_1 = _Saturate_59b070c1298d4279be831e68d2cd96c8_Out_1_Float;
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Blend_Multiply_float3(float3 Base, float3 Blend, out float3 Out, float Opacity)
{
    Out = Base * Blend;
    Out = lerp(Base, Out, Opacity);
}

void Unity_Blend_Overlay_float3(float3 Base, float3 Blend, out float3 Out, float Opacity)
{
    float3 result1 = 1.0 - 2.0 * (1.0 - Base) * (1.0 - Blend);
    float3 result2 = 2.0 * Base * Blend;
    float3 zeroOrOne = step(Base, 0.5);
    Out = result2 * zeroOrOne + (1 - zeroOrOne) * result1;
    Out = lerp(Base, Out, Opacity);
}

void Unity_NormalStrength_float(float3 In, float Strength, out float3 Out)
{
    Out = float3(In.rg * Strength, lerp(1, In.b, saturate(Strength)));
}

void Unity_NormalBlend_float(float3 A, float3 B, out float3 Out)
{
    Out = SafeNormalize(float3(A.rg + B.rg, A.b * B.b));
}

void Unity_DotProduct_float3(float3 A, float3 B, out float Out)
{
    Out = dot(A, B);
}

void Unity_Smoothstep_float(float Edge1, float Edge2, float In, out float Out)
{
    Out = smoothstep(Edge1, Edge2, In);
}

void Unity_Subtract_float3(float3 A, float3 B, out float3 Out)
{
    Out = A - B;
}

struct Bindings_MFPivotVerticalGradience_c9e909c1845b8ae4b9a11aa715963787_float
{
float3 WorldSpacePosition;
};

void SG_MFPivotVerticalGradience_c9e909c1845b8ae4b9a11aa715963787_float(float _Offset, float _Length, Bindings_MFPivotVerticalGradience_c9e909c1845b8ae4b9a11aa715963787_float IN, out float Mask_1)
{
float _Property_4680dca2dee245e7aa77915a0b17cce9_Out_0_Float = _Offset;
float _Property_c763914b9bbb4da5abad7a3db51adcb2_Out_0_Float = _Length;
float _Add_ad1531c9af874822b29fd9a13421289e_Out_2_Float;
Unity_Add_float(_Property_4680dca2dee245e7aa77915a0b17cce9_Out_0_Float, _Property_c763914b9bbb4da5abad7a3db51adcb2_Out_0_Float, _Add_ad1531c9af874822b29fd9a13421289e_Out_2_Float);
float3 _Subtract_69e80d3fb1bb4d71b4be84a010b280ff_Out_2_Vector3;
Unity_Subtract_float3(IN.WorldSpacePosition, SHADERGRAPH_OBJECT_POSITION, _Subtract_69e80d3fb1bb4d71b4be84a010b280ff_Out_2_Vector3);
float _Split_bceff54ddef34fab8cfa9a2a0b8bbc58_R_1_Float = _Subtract_69e80d3fb1bb4d71b4be84a010b280ff_Out_2_Vector3[0];
float _Split_bceff54ddef34fab8cfa9a2a0b8bbc58_G_2_Float = _Subtract_69e80d3fb1bb4d71b4be84a010b280ff_Out_2_Vector3[1];
float _Split_bceff54ddef34fab8cfa9a2a0b8bbc58_B_3_Float = _Subtract_69e80d3fb1bb4d71b4be84a010b280ff_Out_2_Vector3[2];
float _Split_bceff54ddef34fab8cfa9a2a0b8bbc58_A_4_Float = 0;
float _Smoothstep_0109929c8f9e41888dfa7ee4fd85c377_Out_3_Float;
Unity_Smoothstep_float(_Property_4680dca2dee245e7aa77915a0b17cce9_Out_0_Float, _Add_ad1531c9af874822b29fd9a13421289e_Out_2_Float, _Split_bceff54ddef34fab8cfa9a2a0b8bbc58_G_2_Float, _Smoothstep_0109929c8f9e41888dfa7ee4fd85c377_Out_3_Float);
Mask_1 = _Smoothstep_0109929c8f9e41888dfa7ee4fd85c377_Out_3_Float;
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
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
UnityTexture2D _Property_82391a4213d745e08202065048659634_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_1_Color_Map, sampler_Layer_1_Color_Map, _Layer_1_Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_7afe6a7edf62460887e09c4746d796b6_Out_0_Vector4 = IN.uv0;
float3 _Multiply_15b5d23699554ab298d90333ce57d387_Out_2_Vector3;
Unity_Multiply_float3_float3((_UV_7afe6a7edf62460887e09c4746d796b6_Out_0_Vector4.xyz), float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z))), _Multiply_15b5d23699554ab298d90333ce57d387_Out_2_Vector3);
float _Property_9a5b0bf1bf584876a406e2b633741c94_Out_0_Float = _Scale_UVs_with_Object;
float3 _Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3;
Unity_Lerp_float3((_UV_7afe6a7edf62460887e09c4746d796b6_Out_0_Vector4.xyz), _Multiply_15b5d23699554ab298d90333ce57d387_Out_2_Vector3, (_Property_9a5b0bf1bf584876a406e2b633741c94_Out_0_Float.xxx), _Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3);
float _Property_460238447f6b4170aa7397df1262be96_Out_0_Float = _Layer_1_Tiling;
float3 _Multiply_94cb4adbcc9a4b25980974e6be235287_Out_2_Vector3;
Unity_Multiply_float3_float3(_Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3, (_Property_460238447f6b4170aa7397df1262be96_Out_0_Float.xxx), _Multiply_94cb4adbcc9a4b25980974e6be235287_Out_2_Vector3);
float4 _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_82391a4213d745e08202065048659634_Out_0_Texture2D.tex, _Property_82391a4213d745e08202065048659634_Out_0_Texture2D.samplerstate, _Property_82391a4213d745e08202065048659634_Out_0_Texture2D.GetTransformedUV((_Multiply_94cb4adbcc9a4b25980974e6be235287_Out_2_Vector3.xy)) );
if (_Property_82391a4213d745e08202065048659634_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4, _Property_82391a4213d745e08202065048659634_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_R_4_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4.r;
float _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_G_5_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4.g;
float _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_B_6_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4.b;
float _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_A_7_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4.a;
float _Split_03d41d27a027425fa76a6c910dfbed96_R_1_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4[0];
float _Split_03d41d27a027425fa76a6c910dfbed96_G_2_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4[1];
float _Split_03d41d27a027425fa76a6c910dfbed96_B_3_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4[2];
float _Split_03d41d27a027425fa76a6c910dfbed96_A_4_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4[3];
float3 _Vector3_29d44982f90c486b8d58d91423294375_Out_0_Vector3 = float3(_Split_03d41d27a027425fa76a6c910dfbed96_R_1_Float, _Split_03d41d27a027425fa76a6c910dfbed96_G_2_Float, _Split_03d41d27a027425fa76a6c910dfbed96_B_3_Float);
float _Property_c55f5c80dc494683850ea3803571112b_Out_0_Float = _Layer_1_Hue;
float3 _Hue_4528903dcb5b4b15be0e273df247bfd9_Out_2_Vector3;
Unity_Hue_Normalized_float(_Vector3_29d44982f90c486b8d58d91423294375_Out_0_Vector3, _Property_c55f5c80dc494683850ea3803571112b_Out_0_Float, _Hue_4528903dcb5b4b15be0e273df247bfd9_Out_2_Vector3);
float _Property_ba7720a9d8134eb58030eb416c6a024a_Out_0_Float = _Layer_1_Brightness;
float3 _Multiply_01d7b815489a446fa8f5fbd7bf096d4f_Out_2_Vector3;
Unity_Multiply_float3_float3(_Hue_4528903dcb5b4b15be0e273df247bfd9_Out_2_Vector3, (_Property_ba7720a9d8134eb58030eb416c6a024a_Out_0_Float.xxx), _Multiply_01d7b815489a446fa8f5fbd7bf096d4f_Out_2_Vector3);
UnityTexture2D _Property_f37e4daa8ff94f5eb2ca9f6f83a20bb3_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_2_Color_Map, sampler_Layer_2_Color_Map, _Layer_2_Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_812f9793e99740fcb534b8c8d8af0ff2_Out_0_Float = _Layer_2_Tiling;
float3 _Multiply_7719c519f67d4b1aaaf2cca1cc2d4d79_Out_2_Vector3;
Unity_Multiply_float3_float3(_Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3, (_Property_812f9793e99740fcb534b8c8d8af0ff2_Out_0_Float.xxx), _Multiply_7719c519f67d4b1aaaf2cca1cc2d4d79_Out_2_Vector3);
float4 _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_f37e4daa8ff94f5eb2ca9f6f83a20bb3_Out_0_Texture2D.tex, _Property_f37e4daa8ff94f5eb2ca9f6f83a20bb3_Out_0_Texture2D.samplerstate, _Property_f37e4daa8ff94f5eb2ca9f6f83a20bb3_Out_0_Texture2D.GetTransformedUV((_Multiply_7719c519f67d4b1aaaf2cca1cc2d4d79_Out_2_Vector3.xy)) );
if (_Property_f37e4daa8ff94f5eb2ca9f6f83a20bb3_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4, _Property_f37e4daa8ff94f5eb2ca9f6f83a20bb3_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_R_4_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4.r;
float _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_G_5_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4.g;
float _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_B_6_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4.b;
float _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_A_7_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4.a;
float _Split_d38832e5c62c44d3a795b83acceab016_R_1_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4[0];
float _Split_d38832e5c62c44d3a795b83acceab016_G_2_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4[1];
float _Split_d38832e5c62c44d3a795b83acceab016_B_3_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4[2];
float _Split_d38832e5c62c44d3a795b83acceab016_A_4_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4[3];
float3 _Vector3_cb2069d38949469a976ac3a5a2003bd7_Out_0_Vector3 = float3(_Split_d38832e5c62c44d3a795b83acceab016_R_1_Float, _Split_d38832e5c62c44d3a795b83acceab016_G_2_Float, _Split_d38832e5c62c44d3a795b83acceab016_B_3_Float);
float _Property_8970d5f05c7d4b5783a57e1c656ee4a9_Out_0_Float = _Layer_2_Hue;
float3 _Hue_d8383543ffa64d25a0efd5fc36520be4_Out_2_Vector3;
Unity_Hue_Normalized_float(_Vector3_cb2069d38949469a976ac3a5a2003bd7_Out_0_Vector3, _Property_8970d5f05c7d4b5783a57e1c656ee4a9_Out_0_Float, _Hue_d8383543ffa64d25a0efd5fc36520be4_Out_2_Vector3);
float _Property_40475d05242a4efaa3e83e117ecd268d_Out_0_Float = _Layer_2_Brightness;
float3 _Multiply_9a8ec1bf43a149a7a5fb1571321b8200_Out_2_Vector3;
Unity_Multiply_float3_float3(_Hue_d8383543ffa64d25a0efd5fc36520be4_Out_2_Vector3, (_Property_40475d05242a4efaa3e83e117ecd268d_Out_0_Float.xxx), _Multiply_9a8ec1bf43a149a7a5fb1571321b8200_Out_2_Vector3);
UnityTexture2D _Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_RGB_Map, sampler_RGB_Map, _RGB_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D.tex, _Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D.samplerstate, _Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4, _Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_R_4_Float = _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4.r;
float _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_G_5_Float = _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4.g;
float _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_B_6_Float = _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4.b;
float _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_A_7_Float = _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4.a;
float3 _Lerp_754b59c21e034e87848bb65af4294c76_Out_3_Vector3;
Unity_Lerp_float3(_Multiply_01d7b815489a446fa8f5fbd7bf096d4f_Out_2_Vector3, _Multiply_9a8ec1bf43a149a7a5fb1571321b8200_Out_2_Vector3, (_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_G_5_Float.xxx), _Lerp_754b59c21e034e87848bb65af4294c76_Out_3_Vector3);
UnityTexture2D _Property_fef134089dc1401db12f51526b8ed988_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_3_Color_Map, sampler_Layer_3_Color_Map, _Layer_3_Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_8d20917a655e4062a5ce52f974969006_Out_0_Float = _Layer_3_Tiling;
float3 _Multiply_04b9ab405b2d4c27b6dd0696d9a5eacf_Out_2_Vector3;
Unity_Multiply_float3_float3(_Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3, (_Property_8d20917a655e4062a5ce52f974969006_Out_0_Float.xxx), _Multiply_04b9ab405b2d4c27b6dd0696d9a5eacf_Out_2_Vector3);
float4 _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_fef134089dc1401db12f51526b8ed988_Out_0_Texture2D.tex, _Property_fef134089dc1401db12f51526b8ed988_Out_0_Texture2D.samplerstate, _Property_fef134089dc1401db12f51526b8ed988_Out_0_Texture2D.GetTransformedUV((_Multiply_04b9ab405b2d4c27b6dd0696d9a5eacf_Out_2_Vector3.xy)) );
if (_Property_fef134089dc1401db12f51526b8ed988_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4, _Property_fef134089dc1401db12f51526b8ed988_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_R_4_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4.r;
float _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_G_5_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4.g;
float _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_B_6_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4.b;
float _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_A_7_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4.a;
float _Split_838946d3ce6f42cfbaf41fff836d6985_R_1_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4[0];
float _Split_838946d3ce6f42cfbaf41fff836d6985_G_2_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4[1];
float _Split_838946d3ce6f42cfbaf41fff836d6985_B_3_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4[2];
float _Split_838946d3ce6f42cfbaf41fff836d6985_A_4_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4[3];
float3 _Vector3_2a694d435907494fb07f2f9db09d031a_Out_0_Vector3 = float3(_Split_838946d3ce6f42cfbaf41fff836d6985_R_1_Float, _Split_838946d3ce6f42cfbaf41fff836d6985_G_2_Float, _Split_838946d3ce6f42cfbaf41fff836d6985_B_3_Float);
float _Property_6ba5eab2fbda4899981bd9c4c9c6317e_Out_0_Float = _Layer_3_Hue;
float3 _Hue_0a936fcae1974948a815da96c316b7e3_Out_2_Vector3;
Unity_Hue_Normalized_float(_Vector3_2a694d435907494fb07f2f9db09d031a_Out_0_Vector3, _Property_6ba5eab2fbda4899981bd9c4c9c6317e_Out_0_Float, _Hue_0a936fcae1974948a815da96c316b7e3_Out_2_Vector3);
float _Property_c749f04d24d9423fa1a2859f800dc893_Out_0_Float = _Layer_3_Brightness;
float3 _Multiply_db73d490dc9d432e80685ef18e5427fb_Out_2_Vector3;
Unity_Multiply_float3_float3(_Hue_0a936fcae1974948a815da96c316b7e3_Out_2_Vector3, (_Property_c749f04d24d9423fa1a2859f800dc893_Out_0_Float.xxx), _Multiply_db73d490dc9d432e80685ef18e5427fb_Out_2_Vector3);
float3 _Lerp_66f917f82ed14fd89c6c2fe066b5e834_Out_3_Vector3;
Unity_Lerp_float3(_Lerp_754b59c21e034e87848bb65af4294c76_Out_3_Vector3, _Multiply_db73d490dc9d432e80685ef18e5427fb_Out_2_Vector3, (_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_B_6_Float.xxx), _Lerp_66f917f82ed14fd89c6c2fe066b5e834_Out_3_Vector3);
#if defined(_USE_LAYER_3)
float3 _UseLayer3_ee2b565dd7774fa7a5c88d0b7bfd05b8_Out_0_Vector3 = _Lerp_66f917f82ed14fd89c6c2fe066b5e834_Out_3_Vector3;
#else
float3 _UseLayer3_ee2b565dd7774fa7a5c88d0b7bfd05b8_Out_0_Vector3 = _Lerp_754b59c21e034e87848bb65af4294c76_Out_3_Vector3;
#endif
#if defined(_USE_LAYER_2)
float3 _UseLayer2_22b88f361b80488fbbec2df7f45c4924_Out_0_Vector3 = _UseLayer3_ee2b565dd7774fa7a5c88d0b7bfd05b8_Out_0_Vector3;
#else
float3 _UseLayer2_22b88f361b80488fbbec2df7f45c4924_Out_0_Vector3 = _Multiply_01d7b815489a446fa8f5fbd7bf096d4f_Out_2_Vector3;
#endif
float4 _Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4 = _Stylization_Color;
float _Split_42e867e97f454925a658dba60e21864f_R_1_Float = _Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4[0];
float _Split_42e867e97f454925a658dba60e21864f_G_2_Float = _Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4[1];
float _Split_42e867e97f454925a658dba60e21864f_B_3_Float = _Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4[2];
float _Split_42e867e97f454925a658dba60e21864f_A_4_Float = _Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4[3];
UnityTexture2D _Property_c02e202098944eebbd44663588c64e3b_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Edge_Map, sampler_Edge_Map, _Edge_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_c02e202098944eebbd44663588c64e3b_Out_0_Texture2D.tex, _Property_c02e202098944eebbd44663588c64e3b_Out_0_Texture2D.samplerstate, _Property_c02e202098944eebbd44663588c64e3b_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_c02e202098944eebbd44663588c64e3b_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4, _Property_c02e202098944eebbd44663588c64e3b_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_R_4_Float = _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4.r;
float _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_G_5_Float = _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4.g;
float _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_B_6_Float = _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4.b;
float _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_A_7_Float = _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4.a;
float _Property_a4422d37671342bdab8d700fe4c2424a_Out_0_Float = _Stylization_Mask_Contrast;
Bindings_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float _MFCheapContrast_2e3888093b39403781e2ccbee9980699;
float _MFCheapContrast_2e3888093b39403781e2ccbee9980699_Out_1_Float;
SG_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float(_SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_A_7_Float, _Property_a4422d37671342bdab8d700fe4c2424a_Out_0_Float, _MFCheapContrast_2e3888093b39403781e2ccbee9980699, _MFCheapContrast_2e3888093b39403781e2ccbee9980699_Out_1_Float);
float _Multiply_364f0f8b2ff74453b42f050a8b1cb526_Out_2_Float;
Unity_Multiply_float_float(_Split_42e867e97f454925a658dba60e21864f_A_4_Float, _MFCheapContrast_2e3888093b39403781e2ccbee9980699_Out_1_Float, _Multiply_364f0f8b2ff74453b42f050a8b1cb526_Out_2_Float);
float3 _Lerp_0a1e51abb320477ab63b42e1a32052cc_Out_3_Vector3;
Unity_Lerp_float3(_UseLayer2_22b88f361b80488fbbec2df7f45c4924_Out_0_Vector3, (_Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4.xyz), (_Multiply_364f0f8b2ff74453b42f050a8b1cb526_Out_2_Float.xxx), _Lerp_0a1e51abb320477ab63b42e1a32052cc_Out_3_Vector3);
#if defined(_USE_LAYER_1)
float3 _UseLayer1_f5f8cf003ae64c4187983dc616ecbd3b_Out_0_Vector3 = _Lerp_0a1e51abb320477ab63b42e1a32052cc_Out_3_Vector3;
#else
float3 _UseLayer1_f5f8cf003ae64c4187983dc616ecbd3b_Out_0_Vector3 = (_Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4.xyz);
#endif
float4 _Property_0be88f8e5ba94765a3623024be14bbbf_Out_0_Vector4 = _AO_Color;
float _Split_07ec24369bd1411c82179cabd24aabe5_R_1_Float = _Property_0be88f8e5ba94765a3623024be14bbbf_Out_0_Vector4[0];
float _Split_07ec24369bd1411c82179cabd24aabe5_G_2_Float = _Property_0be88f8e5ba94765a3623024be14bbbf_Out_0_Vector4[1];
float _Split_07ec24369bd1411c82179cabd24aabe5_B_3_Float = _Property_0be88f8e5ba94765a3623024be14bbbf_Out_0_Vector4[2];
float _Split_07ec24369bd1411c82179cabd24aabe5_A_4_Float = _Property_0be88f8e5ba94765a3623024be14bbbf_Out_0_Vector4[3];
float _Property_781496b67f424345bca22ad901512267_Out_0_Float = _AO_Mask_Contrast;
Bindings_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float _MFCheapContrast_74522f75ddab433f9ec9f709fe5a6faf;
float _MFCheapContrast_74522f75ddab433f9ec9f709fe5a6faf_Out_1_Float;
SG_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float(_SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_G_5_Float, _Property_781496b67f424345bca22ad901512267_Out_0_Float, _MFCheapContrast_74522f75ddab433f9ec9f709fe5a6faf, _MFCheapContrast_74522f75ddab433f9ec9f709fe5a6faf_Out_1_Float);
float _Multiply_27eebb10636649c3aefb6ad13580fd9e_Out_2_Float;
Unity_Multiply_float_float(_Split_07ec24369bd1411c82179cabd24aabe5_A_4_Float, _MFCheapContrast_74522f75ddab433f9ec9f709fe5a6faf_Out_1_Float, _Multiply_27eebb10636649c3aefb6ad13580fd9e_Out_2_Float);
float3 _Blend_5baeb7dcebe145dcb53bba406ea82ebd_Out_2_Vector3;
Unity_Blend_Multiply_float3(_UseLayer1_f5f8cf003ae64c4187983dc616ecbd3b_Out_0_Vector3, (_Property_0be88f8e5ba94765a3623024be14bbbf_Out_0_Vector4.xyz), _Blend_5baeb7dcebe145dcb53bba406ea82ebd_Out_2_Vector3, _Multiply_27eebb10636649c3aefb6ad13580fd9e_Out_2_Float);
float4 _Property_64075cb2946e4bc89c0928961fa8e759_Out_0_Vector4 = _Dirt_Color;
float _Split_102b0005351f49a0a7ec214d4c4e879e_R_1_Float = _Property_64075cb2946e4bc89c0928961fa8e759_Out_0_Vector4[0];
float _Split_102b0005351f49a0a7ec214d4c4e879e_G_2_Float = _Property_64075cb2946e4bc89c0928961fa8e759_Out_0_Vector4[1];
float _Split_102b0005351f49a0a7ec214d4c4e879e_B_3_Float = _Property_64075cb2946e4bc89c0928961fa8e759_Out_0_Vector4[2];
float _Split_102b0005351f49a0a7ec214d4c4e879e_A_4_Float = _Property_64075cb2946e4bc89c0928961fa8e759_Out_0_Vector4[3];
float _Property_089f6755c08b483d971238d917dd8b3a_Out_0_Float = _Dirt_Mask_Contrast;
Bindings_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float _MFCheapContrast_96f7295ada8247d48955ac898a7aba11;
float _MFCheapContrast_96f7295ada8247d48955ac898a7aba11_Out_1_Float;
SG_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float(_SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_R_4_Float, _Property_089f6755c08b483d971238d917dd8b3a_Out_0_Float, _MFCheapContrast_96f7295ada8247d48955ac898a7aba11, _MFCheapContrast_96f7295ada8247d48955ac898a7aba11_Out_1_Float);
float _Multiply_bc33d3e78b104e93a60b60cea85cebac_Out_2_Float;
Unity_Multiply_float_float(_Split_102b0005351f49a0a7ec214d4c4e879e_A_4_Float, _MFCheapContrast_96f7295ada8247d48955ac898a7aba11_Out_1_Float, _Multiply_bc33d3e78b104e93a60b60cea85cebac_Out_2_Float);
float3 _Lerp_ef18ae06dc3e4fd18201e3925bc55a85_Out_3_Vector3;
Unity_Lerp_float3(_Blend_5baeb7dcebe145dcb53bba406ea82ebd_Out_2_Vector3, (_Property_64075cb2946e4bc89c0928961fa8e759_Out_0_Vector4.xyz), (_Multiply_bc33d3e78b104e93a60b60cea85cebac_Out_2_Float.xxx), _Lerp_ef18ae06dc3e4fd18201e3925bc55a85_Out_3_Vector3);
float4 _Property_dbda4c8e93f54f92a18b3390cbaf06c0_Out_0_Vector4 = _Edgewear_Color;
float _Split_925676a8ce21420aad811d255e389512_R_1_Float = _Property_dbda4c8e93f54f92a18b3390cbaf06c0_Out_0_Vector4[0];
float _Split_925676a8ce21420aad811d255e389512_G_2_Float = _Property_dbda4c8e93f54f92a18b3390cbaf06c0_Out_0_Vector4[1];
float _Split_925676a8ce21420aad811d255e389512_B_3_Float = _Property_dbda4c8e93f54f92a18b3390cbaf06c0_Out_0_Vector4[2];
float _Split_925676a8ce21420aad811d255e389512_A_4_Float = _Property_dbda4c8e93f54f92a18b3390cbaf06c0_Out_0_Vector4[3];
float _Property_4bef2cb688474ef8909e6dc3f2439616_Out_0_Float = _Edgewear_Mask_Contrast;
Bindings_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float _MFCheapContrast_c8d787e56cf842869980c82103f39754;
float _MFCheapContrast_c8d787e56cf842869980c82103f39754_Out_1_Float;
SG_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float(_SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_B_6_Float, _Property_4bef2cb688474ef8909e6dc3f2439616_Out_0_Float, _MFCheapContrast_c8d787e56cf842869980c82103f39754, _MFCheapContrast_c8d787e56cf842869980c82103f39754_Out_1_Float);
float _Multiply_38b6bc16bfef4cd3bec7370aabb6a836_Out_2_Float;
Unity_Multiply_float_float(_Split_925676a8ce21420aad811d255e389512_A_4_Float, _MFCheapContrast_c8d787e56cf842869980c82103f39754_Out_1_Float, _Multiply_38b6bc16bfef4cd3bec7370aabb6a836_Out_2_Float);
float3 _Blend_e613dc790cd741d3b367cb48289cffd5_Out_2_Vector3;
Unity_Blend_Overlay_float3(_Lerp_ef18ae06dc3e4fd18201e3925bc55a85_Out_3_Vector3, (_Property_dbda4c8e93f54f92a18b3390cbaf06c0_Out_0_Vector4.xyz), _Blend_e613dc790cd741d3b367cb48289cffd5_Out_2_Vector3, _Multiply_38b6bc16bfef4cd3bec7370aabb6a836_Out_2_Float);
UnityTexture2D _Property_003a6ee6678543e5964d3e811c19f789_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Coverage_Color_Map, sampler_Coverage_Color_Map, _Coverage_Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_d9f12d2b2464447a82f581c14778bbf8_Out_0_Float = _Coverage_Tiling;
float3 _Multiply_282d032113614cb58779c3a33671ad6c_Out_2_Vector3;
Unity_Multiply_float3_float3(_Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3, (_Property_d9f12d2b2464447a82f581c14778bbf8_Out_0_Float.xxx), _Multiply_282d032113614cb58779c3a33671ad6c_Out_2_Vector3);
float4 _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_003a6ee6678543e5964d3e811c19f789_Out_0_Texture2D.tex, _Property_003a6ee6678543e5964d3e811c19f789_Out_0_Texture2D.samplerstate, _Property_003a6ee6678543e5964d3e811c19f789_Out_0_Texture2D.GetTransformedUV((_Multiply_282d032113614cb58779c3a33671ad6c_Out_2_Vector3.xy)) );
if (_Property_003a6ee6678543e5964d3e811c19f789_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4, _Property_003a6ee6678543e5964d3e811c19f789_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_R_4_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4.r;
float _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_G_5_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4.g;
float _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_B_6_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4.b;
float _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_A_7_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4.a;
float _Split_42bc736ed19d41c2bc8a58c7f653b802_R_1_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4[0];
float _Split_42bc736ed19d41c2bc8a58c7f653b802_G_2_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4[1];
float _Split_42bc736ed19d41c2bc8a58c7f653b802_B_3_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4[2];
float _Split_42bc736ed19d41c2bc8a58c7f653b802_A_4_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4[3];
float3 _Vector3_dd63cc8dd9a443a99a6ecc681ff6961e_Out_0_Vector3 = float3(_Split_42bc736ed19d41c2bc8a58c7f653b802_R_1_Float, _Split_42bc736ed19d41c2bc8a58c7f653b802_G_2_Float, _Split_42bc736ed19d41c2bc8a58c7f653b802_B_3_Float);
float _Property_86cbded0425a4bf2b5441e665a7f5551_Out_0_Float = _Coverage_Hue;
float3 _Hue_5e168e8c10044fe09120668e4b870b44_Out_2_Vector3;
Unity_Hue_Normalized_float(_Vector3_dd63cc8dd9a443a99a6ecc681ff6961e_Out_0_Vector3, _Property_86cbded0425a4bf2b5441e665a7f5551_Out_0_Float, _Hue_5e168e8c10044fe09120668e4b870b44_Out_2_Vector3);
float _Property_dccedc571d6d411291da660d59f31596_Out_0_Float = _Coverage_Brightness;
float3 _Multiply_c28f28e86afd43f584c04c39cc44c8c4_Out_2_Vector3;
Unity_Multiply_float3_float3(_Hue_5e168e8c10044fe09120668e4b870b44_Out_2_Vector3, (_Property_dccedc571d6d411291da660d59f31596_Out_0_Float.xxx), _Multiply_c28f28e86afd43f584c04c39cc44c8c4_Out_2_Vector3);
float _Property_f13be210d8b049a9a1e6c0e219f5a53f_Out_0_Float = _Coverage_Normal_Mask_Slope;
float _Property_1f786f31d0924f6ba0904b90319c56e3_Out_0_Float = _Coverage_Normal_Mask_Gradience;
float _Add_4177ca63c9724385910d37f97d0e8c48_Out_2_Float;
Unity_Add_float(_Property_f13be210d8b049a9a1e6c0e219f5a53f_Out_0_Float, _Property_1f786f31d0924f6ba0904b90319c56e3_Out_0_Float, _Add_4177ca63c9724385910d37f97d0e8c48_Out_2_Float);
UnityTexture2D _Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_1_Normal_Map, sampler_Layer_1_Normal_Map, _Layer_1_Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D.tex, _Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D.samplerstate, _Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D.GetTransformedUV((_Multiply_94cb4adbcc9a4b25980974e6be235287_Out_2_Vector3.xy)) );
if (_Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4, _Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4);
float _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_R_4_Float = _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.r;
float _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_G_5_Float = _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.g;
float _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_B_6_Float = _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.b;
float _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_A_7_Float = _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.a;
float _Property_7cfa9a8e94684810a4cd4c70a2257b6c_Out_0_Float = _Layer_1_Normal_Strength;
float3 _NormalStrength_9428e53c24e74ce189625604c4b60275_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.xyz), _Property_7cfa9a8e94684810a4cd4c70a2257b6c_Out_0_Float, _NormalStrength_9428e53c24e74ce189625604c4b60275_Out_2_Vector3);
UnityTexture2D _Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_2_Normal_Map, sampler_Layer_2_Normal_Map, _Layer_2_Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D.tex, _Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D.samplerstate, _Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D.GetTransformedUV((_Multiply_7719c519f67d4b1aaaf2cca1cc2d4d79_Out_2_Vector3.xy)) );
if (_Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4, _Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4);
float _SampleTexture2D_ae5b372472874ad28eac881821ce553c_R_4_Float = _SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.r;
float _SampleTexture2D_ae5b372472874ad28eac881821ce553c_G_5_Float = _SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.g;
float _SampleTexture2D_ae5b372472874ad28eac881821ce553c_B_6_Float = _SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.b;
float _SampleTexture2D_ae5b372472874ad28eac881821ce553c_A_7_Float = _SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.a;
float _Property_12c65401b09f44daa763974826741097_Out_0_Float = _Layer_2_Normal_Strength;
float3 _NormalStrength_93ad91a06836481ca4f36c947e62a393_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.xyz), _Property_12c65401b09f44daa763974826741097_Out_0_Float, _NormalStrength_93ad91a06836481ca4f36c947e62a393_Out_2_Vector3);
float3 _Lerp_7325f1720917490cb13328e58ddd8efe_Out_3_Vector3;
Unity_Lerp_float3(_NormalStrength_9428e53c24e74ce189625604c4b60275_Out_2_Vector3, _NormalStrength_93ad91a06836481ca4f36c947e62a393_Out_2_Vector3, (_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_G_5_Float.xxx), _Lerp_7325f1720917490cb13328e58ddd8efe_Out_3_Vector3);
UnityTexture2D _Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_3_Normal_Map, sampler_Layer_3_Normal_Map, _Layer_3_Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D.tex, _Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D.samplerstate, _Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D.GetTransformedUV((_Multiply_04b9ab405b2d4c27b6dd0696d9a5eacf_Out_2_Vector3.xy)) );
if (_Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4, _Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4);
float _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_R_4_Float = _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.r;
float _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_G_5_Float = _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.g;
float _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_B_6_Float = _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.b;
float _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_A_7_Float = _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.a;
float _Property_02480e6b3805449397b2d5a15e416673_Out_0_Float = _Layer_3_Normal_Strength;
float3 _NormalStrength_8e30a22d9ed147ca9ef200087a7f43aa_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.xyz), _Property_02480e6b3805449397b2d5a15e416673_Out_0_Float, _NormalStrength_8e30a22d9ed147ca9ef200087a7f43aa_Out_2_Vector3);
float3 _Lerp_642d324ee8d74cf888dc943377359b6d_Out_3_Vector3;
Unity_Lerp_float3(_Lerp_7325f1720917490cb13328e58ddd8efe_Out_3_Vector3, _NormalStrength_8e30a22d9ed147ca9ef200087a7f43aa_Out_2_Vector3, (_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_B_6_Float.xxx), _Lerp_642d324ee8d74cf888dc943377359b6d_Out_3_Vector3);
#if defined(_USE_LAYER_3)
float3 _UseLayer3_3e504be39f154559868343947f457c24_Out_0_Vector3 = _Lerp_642d324ee8d74cf888dc943377359b6d_Out_3_Vector3;
#else
float3 _UseLayer3_3e504be39f154559868343947f457c24_Out_0_Vector3 = _Lerp_7325f1720917490cb13328e58ddd8efe_Out_3_Vector3;
#endif
#if defined(_USE_LAYER_2)
float3 _UseLayer2_c90f92a0b0134057867d0810829f779e_Out_0_Vector3 = _UseLayer3_3e504be39f154559868343947f457c24_Out_0_Vector3;
#else
float3 _UseLayer2_c90f92a0b0134057867d0810829f779e_Out_0_Vector3 = _NormalStrength_9428e53c24e74ce189625604c4b60275_Out_2_Vector3;
#endif
UnityTexture2D _Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Normal_Map, sampler_Normal_Map, _Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D.tex, _Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D.samplerstate, _Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4, _Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4);
float _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_R_4_Float = _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.r;
float _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_G_5_Float = _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.g;
float _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_B_6_Float = _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.b;
float _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_A_7_Float = _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.a;
float _Property_758b3d19a0c14ff099921db6234664b2_Out_0_Float = _Normal_Strength;
float3 _NormalStrength_3eb407934b854455a959c26453776991_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.xyz), _Property_758b3d19a0c14ff099921db6234664b2_Out_0_Float, _NormalStrength_3eb407934b854455a959c26453776991_Out_2_Vector3);
float3 _NormalBlend_cb4851ae94454d539b1004bdc1f2604f_Out_2_Vector3;
Unity_NormalBlend_float(_UseLayer2_c90f92a0b0134057867d0810829f779e_Out_0_Vector3, _NormalStrength_3eb407934b854455a959c26453776991_Out_2_Vector3, _NormalBlend_cb4851ae94454d539b1004bdc1f2604f_Out_2_Vector3);
#if defined(_USE_LAYER_1)
float3 _UseLayer1_7da71e644bf2434f99e8005bad9e8f14_Out_0_Vector3 = _NormalBlend_cb4851ae94454d539b1004bdc1f2604f_Out_2_Vector3;
#else
float3 _UseLayer1_7da71e644bf2434f99e8005bad9e8f14_Out_0_Vector3 = _NormalStrength_3eb407934b854455a959c26453776991_Out_2_Vector3;
#endif
float3 _Transform_04398ac1bc954fb19c62ce3f5f6d670c_Out_1_Vector3;
{
float3x3 tangentTransform = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
_Transform_04398ac1bc954fb19c62ce3f5f6d670c_Out_1_Vector3 = TransformTangentToWorld(_UseLayer1_7da71e644bf2434f99e8005bad9e8f14_Out_0_Vector3.xyz, tangentTransform, true);
}
float _DotProduct_ae13069c870e4539be3826642299bede_Out_2_Float;
Unity_DotProduct_float3(_Transform_04398ac1bc954fb19c62ce3f5f6d670c_Out_1_Vector3, float3(0, 1, 0), _DotProduct_ae13069c870e4539be3826642299bede_Out_2_Float);
float _Smoothstep_652b53e75f9e481e8c829d3bd3d9c24e_Out_3_Float;
Unity_Smoothstep_float(_Property_f13be210d8b049a9a1e6c0e219f5a53f_Out_0_Float, _Add_4177ca63c9724385910d37f97d0e8c48_Out_2_Float, _DotProduct_ae13069c870e4539be3826642299bede_Out_2_Float, _Smoothstep_652b53e75f9e481e8c829d3bd3d9c24e_Out_3_Float);
float _Property_64938267e5134c5b9a4256d11aa25705_Out_0_Float = _Coverage_Height_Mask_Pivot_Offset;
float _Property_cfcce6f026924b048f8c33bdc73c14f6_Out_0_Float = _Coverage_Height_Mask_Length;
Bindings_MFPivotVerticalGradience_c9e909c1845b8ae4b9a11aa715963787_float _MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c;
_MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c.WorldSpacePosition = IN.WorldSpacePosition;
float _MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c_Mask_1_Float;
SG_MFPivotVerticalGradience_c9e909c1845b8ae4b9a11aa715963787_float(_Property_64938267e5134c5b9a4256d11aa25705_Out_0_Float, _Property_cfcce6f026924b048f8c33bdc73c14f6_Out_0_Float, _MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c, _MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c_Mask_1_Float);
float _Multiply_f6a21f2d15b7472e8173002af669cfdf_Out_2_Float;
Unity_Multiply_float_float(_Smoothstep_652b53e75f9e481e8c829d3bd3d9c24e_Out_3_Float, _MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c_Mask_1_Float, _Multiply_f6a21f2d15b7472e8173002af669cfdf_Out_2_Float);
float3 _Lerp_90a44aeed76b4e67b245ea63960c28fc_Out_3_Vector3;
Unity_Lerp_float3(_Blend_e613dc790cd741d3b367cb48289cffd5_Out_2_Vector3, _Multiply_c28f28e86afd43f584c04c39cc44c8c4_Out_2_Vector3, (_Multiply_f6a21f2d15b7472e8173002af669cfdf_Out_2_Float.xxx), _Lerp_90a44aeed76b4e67b245ea63960c28fc_Out_3_Vector3);
#if defined(_USE_COVERAGE_LAYER)
float3 _UseCoverageLayer_402efe74563f417bb22b7997bdfc5c1e_Out_0_Vector3 = _Lerp_90a44aeed76b4e67b245ea63960c28fc_Out_3_Vector3;
#else
float3 _UseCoverageLayer_402efe74563f417bb22b7997bdfc5c1e_Out_0_Vector3 = _Blend_e613dc790cd741d3b367cb48289cffd5_Out_2_Vector3;
#endif
surface.BaseColor = _UseCoverageLayer_402efe74563f417bb22b7997bdfc5c1e_Out_0_Vector3;
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

    // to pr               eserve mikktspace compliance we use same scale renormFactor as was used on the normal.
    // This                is explained in section 2.2 in "surface gradient based bump mapping framework"
    output.WorldSpaceTangent = renormFactor * input.tangentWS.xyz;
    output.WorldSpaceBiTangent = renormFactor * bitang;

    output.WorldSpacePosition = input.positionWS;

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
#pragma shader_feature_local _ _USE_LAYER_2
#pragma shader_feature_local _ _USE_LAYER_3
#pragma shader_feature_local _ _USE_COVERAGE_LAYER
#pragma shader_feature_local _ _USE_LAYER_1

#if defined(_USE_LAYER_2) && defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_3) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_2
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_3)
#define KEYWORD_PERMUTATION_3
#elif defined(_USE_LAYER_2) && defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_4
#elif defined(_USE_LAYER_2) && defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_5
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_6
#elif defined(_USE_LAYER_2)
#define KEYWORD_PERMUTATION_7
#elif defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_8
#elif defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_9
#elif defined(_USE_LAYER_3) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_10
#elif defined(_USE_LAYER_3)
#define KEYWORD_PERMUTATION_11
#elif defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_12
#elif defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_13
#elif defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_14
#else
#define KEYWORD_PERMUTATION_15
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
 float3 WorldSpaceTangent;
 float3 WorldSpaceBiTangent;
 float3 WorldSpacePosition;
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

float4 _Layer_1_Color_Map_TexelSize;
float _Layer_1_Hue;
float _Layer_1_Brightness;
float _Coverage_Height_Mask_Length;
float4 _Layer_1_Normal_Map_TexelSize;
float _Layer_1_Normal_Strength;
float _Layer_1_Tiling;
float _Layer_1_Smoothness_Strength;
float _Layer_2_Tiling;
float4 _Layer_2_Color_Map_TexelSize;
float _Layer_2_Hue;
float _Layer_2_Brightness;
float _Layer_2_Smoothness_Strength;
float4 _Layer_2_Normal_Map_TexelSize;
float _Layer_2_Normal_Strength;
float _Layer_3_Tiling;
float4 _Layer_3_Color_Map_TexelSize;
float _Layer_3_Hue;
float _Layer_3_Brightness;
float _Layer_3_Smoothness_Strength;
float4 _Layer_3_Normal_Map_TexelSize;
float _Layer_3_Normal_Strength;
float4 _RGB_Map_TexelSize;
float4 _Normal_Map_TexelSize;
float _Normal_Strength;
float _Coverage_Tiling;
float4 _Coverage_Color_Map_TexelSize;
float _Coverage_Hue;
float _Coverage_Brightness;
float _Coverage_Smoothness_Strength;
float4 _Coverage_Normal_Map_TexelSize;
float _Coverage_Normal_Strength;
float4 _Edge_Map_TexelSize;
float _Dirt_Mask_Contrast;
float _AO_Mask_Contrast;
float _Edgewear_Mask_Contrast;
float _Stylization_Mask_Contrast;
float4 _Stylization_Color;
float4 _AO_Color;
float4 _Dirt_Color;
float4 _Edgewear_Color;
float _Coverage_Normal_Mask_Slope;
float _Coverage_Normal_Mask_Gradience;
float _Coverage_Height_Mask_Pivot_Offset;
float _Scale_UVs_with_Object;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Layer_1_Color_Map);
SAMPLER(sampler_Layer_1_Color_Map);
TEXTURE2D(_Layer_1_Normal_Map);
SAMPLER(sampler_Layer_1_Normal_Map);
TEXTURE2D(_Layer_2_Color_Map);
SAMPLER(sampler_Layer_2_Color_Map);
TEXTURE2D(_Layer_2_Normal_Map);
SAMPLER(sampler_Layer_2_Normal_Map);
TEXTURE2D(_Layer_3_Color_Map);
SAMPLER(sampler_Layer_3_Color_Map);
TEXTURE2D(_Layer_3_Normal_Map);
SAMPLER(sampler_Layer_3_Normal_Map);
TEXTURE2D(_RGB_Map);
SAMPLER(sampler_RGB_Map);
TEXTURE2D(_Normal_Map);
SAMPLER(sampler_Normal_Map);
TEXTURE2D(_Coverage_Color_Map);
SAMPLER(sampler_Coverage_Color_Map);
TEXTURE2D(_Coverage_Normal_Map);
SAMPLER(sampler_Coverage_Normal_Map);
TEXTURE2D(_Edge_Map);
SAMPLER(sampler_Edge_Map);

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

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Hue_Normalized_float(float3 In, float Offset, out float3 Out)
{
    // RGB to HSV
    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 P = lerp(float4(In.bg, K.wz), float4(In.gb, K.xy), step(In.b, In.g));
    float4 Q = lerp(float4(P.xyw, In.r), float4(In.r, P.yzx), step(P.x, In.r));
    float D = Q.x - min(Q.w, Q.y);
    float E = 1e-10;
    float V = (D == 0) ? Q.x : (Q.x + E);
    float3 hsv = float3(abs(Q.z + (Q.w - Q.y)/(6.0 * D + E)), D / (Q.x + E), V);

    float hue = hsv.x + Offset;
    hsv.x = (hue < 0)
            ? hue + 1
            : (hue > 1)
                ? hue - 1
                : hue;

    // HSV to RGB
    float4 K2 = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 P2 = abs(frac(hsv.xxx + K2.xyz) * 6.0 - K2.www);
    Out = hsv.z * lerp(K2.xxx, saturate(P2 - K2.xxx), hsv.y);
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Lerp_float(float A, float B, float T, out float Out)
{
    Out = lerp(A, B, T);
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

struct Bindings_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float
{
};

void SG_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float(float _Input, float _Contrast, Bindings_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float IN, out float Out_1)
{
float _Property_f17f2d608b764773bc9f33ea28c3e14e_Out_0_Float = _Contrast;
float _Add_59838b3eb9ec437bbd93b6fd8db7a0c4_Out_2_Float;
Unity_Add_float(_Property_f17f2d608b764773bc9f33ea28c3e14e_Out_0_Float, float(-1), _Add_59838b3eb9ec437bbd93b6fd8db7a0c4_Out_2_Float);
float _Add_524e73a4aa0b4e7982442fdf0d39fe18_Out_2_Float;
Unity_Add_float(_Property_f17f2d608b764773bc9f33ea28c3e14e_Out_0_Float, float(1), _Add_524e73a4aa0b4e7982442fdf0d39fe18_Out_2_Float);
float _Property_df60db08578f4909bad9a81c68245e45_Out_0_Float = _Input;
float _Lerp_6af4dfb2e0f0426a81a1669c55ec8196_Out_3_Float;
Unity_Lerp_float(_Add_59838b3eb9ec437bbd93b6fd8db7a0c4_Out_2_Float, _Add_524e73a4aa0b4e7982442fdf0d39fe18_Out_2_Float, _Property_df60db08578f4909bad9a81c68245e45_Out_0_Float, _Lerp_6af4dfb2e0f0426a81a1669c55ec8196_Out_3_Float);
float _Saturate_59b070c1298d4279be831e68d2cd96c8_Out_1_Float;
Unity_Saturate_float(_Lerp_6af4dfb2e0f0426a81a1669c55ec8196_Out_3_Float, _Saturate_59b070c1298d4279be831e68d2cd96c8_Out_1_Float);
Out_1 = _Saturate_59b070c1298d4279be831e68d2cd96c8_Out_1_Float;
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Blend_Multiply_float3(float3 Base, float3 Blend, out float3 Out, float Opacity)
{
    Out = Base * Blend;
    Out = lerp(Base, Out, Opacity);
}

void Unity_Blend_Overlay_float3(float3 Base, float3 Blend, out float3 Out, float Opacity)
{
    float3 result1 = 1.0 - 2.0 * (1.0 - Base) * (1.0 - Blend);
    float3 result2 = 2.0 * Base * Blend;
    float3 zeroOrOne = step(Base, 0.5);
    Out = result2 * zeroOrOne + (1 - zeroOrOne) * result1;
    Out = lerp(Base, Out, Opacity);
}

void Unity_NormalStrength_float(float3 In, float Strength, out float3 Out)
{
    Out = float3(In.rg * Strength, lerp(1, In.b, saturate(Strength)));
}

void Unity_NormalBlend_float(float3 A, float3 B, out float3 Out)
{
    Out = SafeNormalize(float3(A.rg + B.rg, A.b * B.b));
}

void Unity_DotProduct_float3(float3 A, float3 B, out float Out)
{
    Out = dot(A, B);
}

void Unity_Smoothstep_float(float Edge1, float Edge2, float In, out float Out)
{
    Out = smoothstep(Edge1, Edge2, In);
}

void Unity_Subtract_float3(float3 A, float3 B, out float3 Out)
{
    Out = A - B;
}

struct Bindings_MFPivotVerticalGradience_c9e909c1845b8ae4b9a11aa715963787_float
{
float3 WorldSpacePosition;
};

void SG_MFPivotVerticalGradience_c9e909c1845b8ae4b9a11aa715963787_float(float _Offset, float _Length, Bindings_MFPivotVerticalGradience_c9e909c1845b8ae4b9a11aa715963787_float IN, out float Mask_1)
{
float _Property_4680dca2dee245e7aa77915a0b17cce9_Out_0_Float = _Offset;
float _Property_c763914b9bbb4da5abad7a3db51adcb2_Out_0_Float = _Length;
float _Add_ad1531c9af874822b29fd9a13421289e_Out_2_Float;
Unity_Add_float(_Property_4680dca2dee245e7aa77915a0b17cce9_Out_0_Float, _Property_c763914b9bbb4da5abad7a3db51adcb2_Out_0_Float, _Add_ad1531c9af874822b29fd9a13421289e_Out_2_Float);
float3 _Subtract_69e80d3fb1bb4d71b4be84a010b280ff_Out_2_Vector3;
Unity_Subtract_float3(IN.WorldSpacePosition, SHADERGRAPH_OBJECT_POSITION, _Subtract_69e80d3fb1bb4d71b4be84a010b280ff_Out_2_Vector3);
float _Split_bceff54ddef34fab8cfa9a2a0b8bbc58_R_1_Float = _Subtract_69e80d3fb1bb4d71b4be84a010b280ff_Out_2_Vector3[0];
float _Split_bceff54ddef34fab8cfa9a2a0b8bbc58_G_2_Float = _Subtract_69e80d3fb1bb4d71b4be84a010b280ff_Out_2_Vector3[1];
float _Split_bceff54ddef34fab8cfa9a2a0b8bbc58_B_3_Float = _Subtract_69e80d3fb1bb4d71b4be84a010b280ff_Out_2_Vector3[2];
float _Split_bceff54ddef34fab8cfa9a2a0b8bbc58_A_4_Float = 0;
float _Smoothstep_0109929c8f9e41888dfa7ee4fd85c377_Out_3_Float;
Unity_Smoothstep_float(_Property_4680dca2dee245e7aa77915a0b17cce9_Out_0_Float, _Add_ad1531c9af874822b29fd9a13421289e_Out_2_Float, _Split_bceff54ddef34fab8cfa9a2a0b8bbc58_G_2_Float, _Smoothstep_0109929c8f9e41888dfa7ee4fd85c377_Out_3_Float);
Mask_1 = _Smoothstep_0109929c8f9e41888dfa7ee4fd85c377_Out_3_Float;
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
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
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
UnityTexture2D _Property_82391a4213d745e08202065048659634_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_1_Color_Map, sampler_Layer_1_Color_Map, _Layer_1_Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_7afe6a7edf62460887e09c4746d796b6_Out_0_Vector4 = IN.uv0;
float3 _Multiply_15b5d23699554ab298d90333ce57d387_Out_2_Vector3;
Unity_Multiply_float3_float3((_UV_7afe6a7edf62460887e09c4746d796b6_Out_0_Vector4.xyz), float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z))), _Multiply_15b5d23699554ab298d90333ce57d387_Out_2_Vector3);
float _Property_9a5b0bf1bf584876a406e2b633741c94_Out_0_Float = _Scale_UVs_with_Object;
float3 _Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3;
Unity_Lerp_float3((_UV_7afe6a7edf62460887e09c4746d796b6_Out_0_Vector4.xyz), _Multiply_15b5d23699554ab298d90333ce57d387_Out_2_Vector3, (_Property_9a5b0bf1bf584876a406e2b633741c94_Out_0_Float.xxx), _Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3);
float _Property_460238447f6b4170aa7397df1262be96_Out_0_Float = _Layer_1_Tiling;
float3 _Multiply_94cb4adbcc9a4b25980974e6be235287_Out_2_Vector3;
Unity_Multiply_float3_float3(_Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3, (_Property_460238447f6b4170aa7397df1262be96_Out_0_Float.xxx), _Multiply_94cb4adbcc9a4b25980974e6be235287_Out_2_Vector3);
float4 _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_82391a4213d745e08202065048659634_Out_0_Texture2D.tex, _Property_82391a4213d745e08202065048659634_Out_0_Texture2D.samplerstate, _Property_82391a4213d745e08202065048659634_Out_0_Texture2D.GetTransformedUV((_Multiply_94cb4adbcc9a4b25980974e6be235287_Out_2_Vector3.xy)) );
if (_Property_82391a4213d745e08202065048659634_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4, _Property_82391a4213d745e08202065048659634_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_R_4_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4.r;
float _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_G_5_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4.g;
float _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_B_6_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4.b;
float _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_A_7_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4.a;
float _Split_03d41d27a027425fa76a6c910dfbed96_R_1_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4[0];
float _Split_03d41d27a027425fa76a6c910dfbed96_G_2_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4[1];
float _Split_03d41d27a027425fa76a6c910dfbed96_B_3_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4[2];
float _Split_03d41d27a027425fa76a6c910dfbed96_A_4_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4[3];
float3 _Vector3_29d44982f90c486b8d58d91423294375_Out_0_Vector3 = float3(_Split_03d41d27a027425fa76a6c910dfbed96_R_1_Float, _Split_03d41d27a027425fa76a6c910dfbed96_G_2_Float, _Split_03d41d27a027425fa76a6c910dfbed96_B_3_Float);
float _Property_c55f5c80dc494683850ea3803571112b_Out_0_Float = _Layer_1_Hue;
float3 _Hue_4528903dcb5b4b15be0e273df247bfd9_Out_2_Vector3;
Unity_Hue_Normalized_float(_Vector3_29d44982f90c486b8d58d91423294375_Out_0_Vector3, _Property_c55f5c80dc494683850ea3803571112b_Out_0_Float, _Hue_4528903dcb5b4b15be0e273df247bfd9_Out_2_Vector3);
float _Property_ba7720a9d8134eb58030eb416c6a024a_Out_0_Float = _Layer_1_Brightness;
float3 _Multiply_01d7b815489a446fa8f5fbd7bf096d4f_Out_2_Vector3;
Unity_Multiply_float3_float3(_Hue_4528903dcb5b4b15be0e273df247bfd9_Out_2_Vector3, (_Property_ba7720a9d8134eb58030eb416c6a024a_Out_0_Float.xxx), _Multiply_01d7b815489a446fa8f5fbd7bf096d4f_Out_2_Vector3);
UnityTexture2D _Property_f37e4daa8ff94f5eb2ca9f6f83a20bb3_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_2_Color_Map, sampler_Layer_2_Color_Map, _Layer_2_Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_812f9793e99740fcb534b8c8d8af0ff2_Out_0_Float = _Layer_2_Tiling;
float3 _Multiply_7719c519f67d4b1aaaf2cca1cc2d4d79_Out_2_Vector3;
Unity_Multiply_float3_float3(_Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3, (_Property_812f9793e99740fcb534b8c8d8af0ff2_Out_0_Float.xxx), _Multiply_7719c519f67d4b1aaaf2cca1cc2d4d79_Out_2_Vector3);
float4 _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_f37e4daa8ff94f5eb2ca9f6f83a20bb3_Out_0_Texture2D.tex, _Property_f37e4daa8ff94f5eb2ca9f6f83a20bb3_Out_0_Texture2D.samplerstate, _Property_f37e4daa8ff94f5eb2ca9f6f83a20bb3_Out_0_Texture2D.GetTransformedUV((_Multiply_7719c519f67d4b1aaaf2cca1cc2d4d79_Out_2_Vector3.xy)) );
if (_Property_f37e4daa8ff94f5eb2ca9f6f83a20bb3_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4, _Property_f37e4daa8ff94f5eb2ca9f6f83a20bb3_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_R_4_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4.r;
float _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_G_5_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4.g;
float _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_B_6_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4.b;
float _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_A_7_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4.a;
float _Split_d38832e5c62c44d3a795b83acceab016_R_1_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4[0];
float _Split_d38832e5c62c44d3a795b83acceab016_G_2_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4[1];
float _Split_d38832e5c62c44d3a795b83acceab016_B_3_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4[2];
float _Split_d38832e5c62c44d3a795b83acceab016_A_4_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4[3];
float3 _Vector3_cb2069d38949469a976ac3a5a2003bd7_Out_0_Vector3 = float3(_Split_d38832e5c62c44d3a795b83acceab016_R_1_Float, _Split_d38832e5c62c44d3a795b83acceab016_G_2_Float, _Split_d38832e5c62c44d3a795b83acceab016_B_3_Float);
float _Property_8970d5f05c7d4b5783a57e1c656ee4a9_Out_0_Float = _Layer_2_Hue;
float3 _Hue_d8383543ffa64d25a0efd5fc36520be4_Out_2_Vector3;
Unity_Hue_Normalized_float(_Vector3_cb2069d38949469a976ac3a5a2003bd7_Out_0_Vector3, _Property_8970d5f05c7d4b5783a57e1c656ee4a9_Out_0_Float, _Hue_d8383543ffa64d25a0efd5fc36520be4_Out_2_Vector3);
float _Property_40475d05242a4efaa3e83e117ecd268d_Out_0_Float = _Layer_2_Brightness;
float3 _Multiply_9a8ec1bf43a149a7a5fb1571321b8200_Out_2_Vector3;
Unity_Multiply_float3_float3(_Hue_d8383543ffa64d25a0efd5fc36520be4_Out_2_Vector3, (_Property_40475d05242a4efaa3e83e117ecd268d_Out_0_Float.xxx), _Multiply_9a8ec1bf43a149a7a5fb1571321b8200_Out_2_Vector3);
UnityTexture2D _Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_RGB_Map, sampler_RGB_Map, _RGB_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D.tex, _Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D.samplerstate, _Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4, _Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_R_4_Float = _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4.r;
float _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_G_5_Float = _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4.g;
float _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_B_6_Float = _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4.b;
float _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_A_7_Float = _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4.a;
float3 _Lerp_754b59c21e034e87848bb65af4294c76_Out_3_Vector3;
Unity_Lerp_float3(_Multiply_01d7b815489a446fa8f5fbd7bf096d4f_Out_2_Vector3, _Multiply_9a8ec1bf43a149a7a5fb1571321b8200_Out_2_Vector3, (_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_G_5_Float.xxx), _Lerp_754b59c21e034e87848bb65af4294c76_Out_3_Vector3);
UnityTexture2D _Property_fef134089dc1401db12f51526b8ed988_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_3_Color_Map, sampler_Layer_3_Color_Map, _Layer_3_Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_8d20917a655e4062a5ce52f974969006_Out_0_Float = _Layer_3_Tiling;
float3 _Multiply_04b9ab405b2d4c27b6dd0696d9a5eacf_Out_2_Vector3;
Unity_Multiply_float3_float3(_Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3, (_Property_8d20917a655e4062a5ce52f974969006_Out_0_Float.xxx), _Multiply_04b9ab405b2d4c27b6dd0696d9a5eacf_Out_2_Vector3);
float4 _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_fef134089dc1401db12f51526b8ed988_Out_0_Texture2D.tex, _Property_fef134089dc1401db12f51526b8ed988_Out_0_Texture2D.samplerstate, _Property_fef134089dc1401db12f51526b8ed988_Out_0_Texture2D.GetTransformedUV((_Multiply_04b9ab405b2d4c27b6dd0696d9a5eacf_Out_2_Vector3.xy)) );
if (_Property_fef134089dc1401db12f51526b8ed988_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4, _Property_fef134089dc1401db12f51526b8ed988_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_R_4_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4.r;
float _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_G_5_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4.g;
float _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_B_6_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4.b;
float _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_A_7_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4.a;
float _Split_838946d3ce6f42cfbaf41fff836d6985_R_1_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4[0];
float _Split_838946d3ce6f42cfbaf41fff836d6985_G_2_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4[1];
float _Split_838946d3ce6f42cfbaf41fff836d6985_B_3_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4[2];
float _Split_838946d3ce6f42cfbaf41fff836d6985_A_4_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4[3];
float3 _Vector3_2a694d435907494fb07f2f9db09d031a_Out_0_Vector3 = float3(_Split_838946d3ce6f42cfbaf41fff836d6985_R_1_Float, _Split_838946d3ce6f42cfbaf41fff836d6985_G_2_Float, _Split_838946d3ce6f42cfbaf41fff836d6985_B_3_Float);
float _Property_6ba5eab2fbda4899981bd9c4c9c6317e_Out_0_Float = _Layer_3_Hue;
float3 _Hue_0a936fcae1974948a815da96c316b7e3_Out_2_Vector3;
Unity_Hue_Normalized_float(_Vector3_2a694d435907494fb07f2f9db09d031a_Out_0_Vector3, _Property_6ba5eab2fbda4899981bd9c4c9c6317e_Out_0_Float, _Hue_0a936fcae1974948a815da96c316b7e3_Out_2_Vector3);
float _Property_c749f04d24d9423fa1a2859f800dc893_Out_0_Float = _Layer_3_Brightness;
float3 _Multiply_db73d490dc9d432e80685ef18e5427fb_Out_2_Vector3;
Unity_Multiply_float3_float3(_Hue_0a936fcae1974948a815da96c316b7e3_Out_2_Vector3, (_Property_c749f04d24d9423fa1a2859f800dc893_Out_0_Float.xxx), _Multiply_db73d490dc9d432e80685ef18e5427fb_Out_2_Vector3);
float3 _Lerp_66f917f82ed14fd89c6c2fe066b5e834_Out_3_Vector3;
Unity_Lerp_float3(_Lerp_754b59c21e034e87848bb65af4294c76_Out_3_Vector3, _Multiply_db73d490dc9d432e80685ef18e5427fb_Out_2_Vector3, (_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_B_6_Float.xxx), _Lerp_66f917f82ed14fd89c6c2fe066b5e834_Out_3_Vector3);
#if defined(_USE_LAYER_3)
float3 _UseLayer3_ee2b565dd7774fa7a5c88d0b7bfd05b8_Out_0_Vector3 = _Lerp_66f917f82ed14fd89c6c2fe066b5e834_Out_3_Vector3;
#else
float3 _UseLayer3_ee2b565dd7774fa7a5c88d0b7bfd05b8_Out_0_Vector3 = _Lerp_754b59c21e034e87848bb65af4294c76_Out_3_Vector3;
#endif
#if defined(_USE_LAYER_2)
float3 _UseLayer2_22b88f361b80488fbbec2df7f45c4924_Out_0_Vector3 = _UseLayer3_ee2b565dd7774fa7a5c88d0b7bfd05b8_Out_0_Vector3;
#else
float3 _UseLayer2_22b88f361b80488fbbec2df7f45c4924_Out_0_Vector3 = _Multiply_01d7b815489a446fa8f5fbd7bf096d4f_Out_2_Vector3;
#endif
float4 _Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4 = _Stylization_Color;
float _Split_42e867e97f454925a658dba60e21864f_R_1_Float = _Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4[0];
float _Split_42e867e97f454925a658dba60e21864f_G_2_Float = _Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4[1];
float _Split_42e867e97f454925a658dba60e21864f_B_3_Float = _Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4[2];
float _Split_42e867e97f454925a658dba60e21864f_A_4_Float = _Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4[3];
UnityTexture2D _Property_c02e202098944eebbd44663588c64e3b_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Edge_Map, sampler_Edge_Map, _Edge_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_c02e202098944eebbd44663588c64e3b_Out_0_Texture2D.tex, _Property_c02e202098944eebbd44663588c64e3b_Out_0_Texture2D.samplerstate, _Property_c02e202098944eebbd44663588c64e3b_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_c02e202098944eebbd44663588c64e3b_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4, _Property_c02e202098944eebbd44663588c64e3b_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_R_4_Float = _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4.r;
float _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_G_5_Float = _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4.g;
float _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_B_6_Float = _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4.b;
float _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_A_7_Float = _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4.a;
float _Property_a4422d37671342bdab8d700fe4c2424a_Out_0_Float = _Stylization_Mask_Contrast;
Bindings_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float _MFCheapContrast_2e3888093b39403781e2ccbee9980699;
float _MFCheapContrast_2e3888093b39403781e2ccbee9980699_Out_1_Float;
SG_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float(_SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_A_7_Float, _Property_a4422d37671342bdab8d700fe4c2424a_Out_0_Float, _MFCheapContrast_2e3888093b39403781e2ccbee9980699, _MFCheapContrast_2e3888093b39403781e2ccbee9980699_Out_1_Float);
float _Multiply_364f0f8b2ff74453b42f050a8b1cb526_Out_2_Float;
Unity_Multiply_float_float(_Split_42e867e97f454925a658dba60e21864f_A_4_Float, _MFCheapContrast_2e3888093b39403781e2ccbee9980699_Out_1_Float, _Multiply_364f0f8b2ff74453b42f050a8b1cb526_Out_2_Float);
float3 _Lerp_0a1e51abb320477ab63b42e1a32052cc_Out_3_Vector3;
Unity_Lerp_float3(_UseLayer2_22b88f361b80488fbbec2df7f45c4924_Out_0_Vector3, (_Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4.xyz), (_Multiply_364f0f8b2ff74453b42f050a8b1cb526_Out_2_Float.xxx), _Lerp_0a1e51abb320477ab63b42e1a32052cc_Out_3_Vector3);
#if defined(_USE_LAYER_1)
float3 _UseLayer1_f5f8cf003ae64c4187983dc616ecbd3b_Out_0_Vector3 = _Lerp_0a1e51abb320477ab63b42e1a32052cc_Out_3_Vector3;
#else
float3 _UseLayer1_f5f8cf003ae64c4187983dc616ecbd3b_Out_0_Vector3 = (_Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4.xyz);
#endif
float4 _Property_0be88f8e5ba94765a3623024be14bbbf_Out_0_Vector4 = _AO_Color;
float _Split_07ec24369bd1411c82179cabd24aabe5_R_1_Float = _Property_0be88f8e5ba94765a3623024be14bbbf_Out_0_Vector4[0];
float _Split_07ec24369bd1411c82179cabd24aabe5_G_2_Float = _Property_0be88f8e5ba94765a3623024be14bbbf_Out_0_Vector4[1];
float _Split_07ec24369bd1411c82179cabd24aabe5_B_3_Float = _Property_0be88f8e5ba94765a3623024be14bbbf_Out_0_Vector4[2];
float _Split_07ec24369bd1411c82179cabd24aabe5_A_4_Float = _Property_0be88f8e5ba94765a3623024be14bbbf_Out_0_Vector4[3];
float _Property_781496b67f424345bca22ad901512267_Out_0_Float = _AO_Mask_Contrast;
Bindings_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float _MFCheapContrast_74522f75ddab433f9ec9f709fe5a6faf;
float _MFCheapContrast_74522f75ddab433f9ec9f709fe5a6faf_Out_1_Float;
SG_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float(_SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_G_5_Float, _Property_781496b67f424345bca22ad901512267_Out_0_Float, _MFCheapContrast_74522f75ddab433f9ec9f709fe5a6faf, _MFCheapContrast_74522f75ddab433f9ec9f709fe5a6faf_Out_1_Float);
float _Multiply_27eebb10636649c3aefb6ad13580fd9e_Out_2_Float;
Unity_Multiply_float_float(_Split_07ec24369bd1411c82179cabd24aabe5_A_4_Float, _MFCheapContrast_74522f75ddab433f9ec9f709fe5a6faf_Out_1_Float, _Multiply_27eebb10636649c3aefb6ad13580fd9e_Out_2_Float);
float3 _Blend_5baeb7dcebe145dcb53bba406ea82ebd_Out_2_Vector3;
Unity_Blend_Multiply_float3(_UseLayer1_f5f8cf003ae64c4187983dc616ecbd3b_Out_0_Vector3, (_Property_0be88f8e5ba94765a3623024be14bbbf_Out_0_Vector4.xyz), _Blend_5baeb7dcebe145dcb53bba406ea82ebd_Out_2_Vector3, _Multiply_27eebb10636649c3aefb6ad13580fd9e_Out_2_Float);
float4 _Property_64075cb2946e4bc89c0928961fa8e759_Out_0_Vector4 = _Dirt_Color;
float _Split_102b0005351f49a0a7ec214d4c4e879e_R_1_Float = _Property_64075cb2946e4bc89c0928961fa8e759_Out_0_Vector4[0];
float _Split_102b0005351f49a0a7ec214d4c4e879e_G_2_Float = _Property_64075cb2946e4bc89c0928961fa8e759_Out_0_Vector4[1];
float _Split_102b0005351f49a0a7ec214d4c4e879e_B_3_Float = _Property_64075cb2946e4bc89c0928961fa8e759_Out_0_Vector4[2];
float _Split_102b0005351f49a0a7ec214d4c4e879e_A_4_Float = _Property_64075cb2946e4bc89c0928961fa8e759_Out_0_Vector4[3];
float _Property_089f6755c08b483d971238d917dd8b3a_Out_0_Float = _Dirt_Mask_Contrast;
Bindings_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float _MFCheapContrast_96f7295ada8247d48955ac898a7aba11;
float _MFCheapContrast_96f7295ada8247d48955ac898a7aba11_Out_1_Float;
SG_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float(_SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_R_4_Float, _Property_089f6755c08b483d971238d917dd8b3a_Out_0_Float, _MFCheapContrast_96f7295ada8247d48955ac898a7aba11, _MFCheapContrast_96f7295ada8247d48955ac898a7aba11_Out_1_Float);
float _Multiply_bc33d3e78b104e93a60b60cea85cebac_Out_2_Float;
Unity_Multiply_float_float(_Split_102b0005351f49a0a7ec214d4c4e879e_A_4_Float, _MFCheapContrast_96f7295ada8247d48955ac898a7aba11_Out_1_Float, _Multiply_bc33d3e78b104e93a60b60cea85cebac_Out_2_Float);
float3 _Lerp_ef18ae06dc3e4fd18201e3925bc55a85_Out_3_Vector3;
Unity_Lerp_float3(_Blend_5baeb7dcebe145dcb53bba406ea82ebd_Out_2_Vector3, (_Property_64075cb2946e4bc89c0928961fa8e759_Out_0_Vector4.xyz), (_Multiply_bc33d3e78b104e93a60b60cea85cebac_Out_2_Float.xxx), _Lerp_ef18ae06dc3e4fd18201e3925bc55a85_Out_3_Vector3);
float4 _Property_dbda4c8e93f54f92a18b3390cbaf06c0_Out_0_Vector4 = _Edgewear_Color;
float _Split_925676a8ce21420aad811d255e389512_R_1_Float = _Property_dbda4c8e93f54f92a18b3390cbaf06c0_Out_0_Vector4[0];
float _Split_925676a8ce21420aad811d255e389512_G_2_Float = _Property_dbda4c8e93f54f92a18b3390cbaf06c0_Out_0_Vector4[1];
float _Split_925676a8ce21420aad811d255e389512_B_3_Float = _Property_dbda4c8e93f54f92a18b3390cbaf06c0_Out_0_Vector4[2];
float _Split_925676a8ce21420aad811d255e389512_A_4_Float = _Property_dbda4c8e93f54f92a18b3390cbaf06c0_Out_0_Vector4[3];
float _Property_4bef2cb688474ef8909e6dc3f2439616_Out_0_Float = _Edgewear_Mask_Contrast;
Bindings_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float _MFCheapContrast_c8d787e56cf842869980c82103f39754;
float _MFCheapContrast_c8d787e56cf842869980c82103f39754_Out_1_Float;
SG_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float(_SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_B_6_Float, _Property_4bef2cb688474ef8909e6dc3f2439616_Out_0_Float, _MFCheapContrast_c8d787e56cf842869980c82103f39754, _MFCheapContrast_c8d787e56cf842869980c82103f39754_Out_1_Float);
float _Multiply_38b6bc16bfef4cd3bec7370aabb6a836_Out_2_Float;
Unity_Multiply_float_float(_Split_925676a8ce21420aad811d255e389512_A_4_Float, _MFCheapContrast_c8d787e56cf842869980c82103f39754_Out_1_Float, _Multiply_38b6bc16bfef4cd3bec7370aabb6a836_Out_2_Float);
float3 _Blend_e613dc790cd741d3b367cb48289cffd5_Out_2_Vector3;
Unity_Blend_Overlay_float3(_Lerp_ef18ae06dc3e4fd18201e3925bc55a85_Out_3_Vector3, (_Property_dbda4c8e93f54f92a18b3390cbaf06c0_Out_0_Vector4.xyz), _Blend_e613dc790cd741d3b367cb48289cffd5_Out_2_Vector3, _Multiply_38b6bc16bfef4cd3bec7370aabb6a836_Out_2_Float);
UnityTexture2D _Property_003a6ee6678543e5964d3e811c19f789_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Coverage_Color_Map, sampler_Coverage_Color_Map, _Coverage_Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_d9f12d2b2464447a82f581c14778bbf8_Out_0_Float = _Coverage_Tiling;
float3 _Multiply_282d032113614cb58779c3a33671ad6c_Out_2_Vector3;
Unity_Multiply_float3_float3(_Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3, (_Property_d9f12d2b2464447a82f581c14778bbf8_Out_0_Float.xxx), _Multiply_282d032113614cb58779c3a33671ad6c_Out_2_Vector3);
float4 _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_003a6ee6678543e5964d3e811c19f789_Out_0_Texture2D.tex, _Property_003a6ee6678543e5964d3e811c19f789_Out_0_Texture2D.samplerstate, _Property_003a6ee6678543e5964d3e811c19f789_Out_0_Texture2D.GetTransformedUV((_Multiply_282d032113614cb58779c3a33671ad6c_Out_2_Vector3.xy)) );
if (_Property_003a6ee6678543e5964d3e811c19f789_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4, _Property_003a6ee6678543e5964d3e811c19f789_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_R_4_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4.r;
float _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_G_5_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4.g;
float _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_B_6_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4.b;
float _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_A_7_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4.a;
float _Split_42bc736ed19d41c2bc8a58c7f653b802_R_1_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4[0];
float _Split_42bc736ed19d41c2bc8a58c7f653b802_G_2_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4[1];
float _Split_42bc736ed19d41c2bc8a58c7f653b802_B_3_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4[2];
float _Split_42bc736ed19d41c2bc8a58c7f653b802_A_4_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4[3];
float3 _Vector3_dd63cc8dd9a443a99a6ecc681ff6961e_Out_0_Vector3 = float3(_Split_42bc736ed19d41c2bc8a58c7f653b802_R_1_Float, _Split_42bc736ed19d41c2bc8a58c7f653b802_G_2_Float, _Split_42bc736ed19d41c2bc8a58c7f653b802_B_3_Float);
float _Property_86cbded0425a4bf2b5441e665a7f5551_Out_0_Float = _Coverage_Hue;
float3 _Hue_5e168e8c10044fe09120668e4b870b44_Out_2_Vector3;
Unity_Hue_Normalized_float(_Vector3_dd63cc8dd9a443a99a6ecc681ff6961e_Out_0_Vector3, _Property_86cbded0425a4bf2b5441e665a7f5551_Out_0_Float, _Hue_5e168e8c10044fe09120668e4b870b44_Out_2_Vector3);
float _Property_dccedc571d6d411291da660d59f31596_Out_0_Float = _Coverage_Brightness;
float3 _Multiply_c28f28e86afd43f584c04c39cc44c8c4_Out_2_Vector3;
Unity_Multiply_float3_float3(_Hue_5e168e8c10044fe09120668e4b870b44_Out_2_Vector3, (_Property_dccedc571d6d411291da660d59f31596_Out_0_Float.xxx), _Multiply_c28f28e86afd43f584c04c39cc44c8c4_Out_2_Vector3);
float _Property_f13be210d8b049a9a1e6c0e219f5a53f_Out_0_Float = _Coverage_Normal_Mask_Slope;
float _Property_1f786f31d0924f6ba0904b90319c56e3_Out_0_Float = _Coverage_Normal_Mask_Gradience;
float _Add_4177ca63c9724385910d37f97d0e8c48_Out_2_Float;
Unity_Add_float(_Property_f13be210d8b049a9a1e6c0e219f5a53f_Out_0_Float, _Property_1f786f31d0924f6ba0904b90319c56e3_Out_0_Float, _Add_4177ca63c9724385910d37f97d0e8c48_Out_2_Float);
UnityTexture2D _Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_1_Normal_Map, sampler_Layer_1_Normal_Map, _Layer_1_Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D.tex, _Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D.samplerstate, _Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D.GetTransformedUV((_Multiply_94cb4adbcc9a4b25980974e6be235287_Out_2_Vector3.xy)) );
if (_Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4, _Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4);
float _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_R_4_Float = _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.r;
float _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_G_5_Float = _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.g;
float _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_B_6_Float = _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.b;
float _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_A_7_Float = _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.a;
float _Property_7cfa9a8e94684810a4cd4c70a2257b6c_Out_0_Float = _Layer_1_Normal_Strength;
float3 _NormalStrength_9428e53c24e74ce189625604c4b60275_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.xyz), _Property_7cfa9a8e94684810a4cd4c70a2257b6c_Out_0_Float, _NormalStrength_9428e53c24e74ce189625604c4b60275_Out_2_Vector3);
UnityTexture2D _Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_2_Normal_Map, sampler_Layer_2_Normal_Map, _Layer_2_Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D.tex, _Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D.samplerstate, _Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D.GetTransformedUV((_Multiply_7719c519f67d4b1aaaf2cca1cc2d4d79_Out_2_Vector3.xy)) );
if (_Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4, _Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4);
float _SampleTexture2D_ae5b372472874ad28eac881821ce553c_R_4_Float = _SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.r;
float _SampleTexture2D_ae5b372472874ad28eac881821ce553c_G_5_Float = _SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.g;
float _SampleTexture2D_ae5b372472874ad28eac881821ce553c_B_6_Float = _SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.b;
float _SampleTexture2D_ae5b372472874ad28eac881821ce553c_A_7_Float = _SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.a;
float _Property_12c65401b09f44daa763974826741097_Out_0_Float = _Layer_2_Normal_Strength;
float3 _NormalStrength_93ad91a06836481ca4f36c947e62a393_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.xyz), _Property_12c65401b09f44daa763974826741097_Out_0_Float, _NormalStrength_93ad91a06836481ca4f36c947e62a393_Out_2_Vector3);
float3 _Lerp_7325f1720917490cb13328e58ddd8efe_Out_3_Vector3;
Unity_Lerp_float3(_NormalStrength_9428e53c24e74ce189625604c4b60275_Out_2_Vector3, _NormalStrength_93ad91a06836481ca4f36c947e62a393_Out_2_Vector3, (_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_G_5_Float.xxx), _Lerp_7325f1720917490cb13328e58ddd8efe_Out_3_Vector3);
UnityTexture2D _Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_3_Normal_Map, sampler_Layer_3_Normal_Map, _Layer_3_Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D.tex, _Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D.samplerstate, _Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D.GetTransformedUV((_Multiply_04b9ab405b2d4c27b6dd0696d9a5eacf_Out_2_Vector3.xy)) );
if (_Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4, _Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4);
float _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_R_4_Float = _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.r;
float _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_G_5_Float = _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.g;
float _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_B_6_Float = _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.b;
float _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_A_7_Float = _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.a;
float _Property_02480e6b3805449397b2d5a15e416673_Out_0_Float = _Layer_3_Normal_Strength;
float3 _NormalStrength_8e30a22d9ed147ca9ef200087a7f43aa_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.xyz), _Property_02480e6b3805449397b2d5a15e416673_Out_0_Float, _NormalStrength_8e30a22d9ed147ca9ef200087a7f43aa_Out_2_Vector3);
float3 _Lerp_642d324ee8d74cf888dc943377359b6d_Out_3_Vector3;
Unity_Lerp_float3(_Lerp_7325f1720917490cb13328e58ddd8efe_Out_3_Vector3, _NormalStrength_8e30a22d9ed147ca9ef200087a7f43aa_Out_2_Vector3, (_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_B_6_Float.xxx), _Lerp_642d324ee8d74cf888dc943377359b6d_Out_3_Vector3);
#if defined(_USE_LAYER_3)
float3 _UseLayer3_3e504be39f154559868343947f457c24_Out_0_Vector3 = _Lerp_642d324ee8d74cf888dc943377359b6d_Out_3_Vector3;
#else
float3 _UseLayer3_3e504be39f154559868343947f457c24_Out_0_Vector3 = _Lerp_7325f1720917490cb13328e58ddd8efe_Out_3_Vector3;
#endif
#if defined(_USE_LAYER_2)
float3 _UseLayer2_c90f92a0b0134057867d0810829f779e_Out_0_Vector3 = _UseLayer3_3e504be39f154559868343947f457c24_Out_0_Vector3;
#else
float3 _UseLayer2_c90f92a0b0134057867d0810829f779e_Out_0_Vector3 = _NormalStrength_9428e53c24e74ce189625604c4b60275_Out_2_Vector3;
#endif
UnityTexture2D _Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Normal_Map, sampler_Normal_Map, _Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D.tex, _Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D.samplerstate, _Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4, _Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4);
float _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_R_4_Float = _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.r;
float _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_G_5_Float = _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.g;
float _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_B_6_Float = _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.b;
float _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_A_7_Float = _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.a;
float _Property_758b3d19a0c14ff099921db6234664b2_Out_0_Float = _Normal_Strength;
float3 _NormalStrength_3eb407934b854455a959c26453776991_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.xyz), _Property_758b3d19a0c14ff099921db6234664b2_Out_0_Float, _NormalStrength_3eb407934b854455a959c26453776991_Out_2_Vector3);
float3 _NormalBlend_cb4851ae94454d539b1004bdc1f2604f_Out_2_Vector3;
Unity_NormalBlend_float(_UseLayer2_c90f92a0b0134057867d0810829f779e_Out_0_Vector3, _NormalStrength_3eb407934b854455a959c26453776991_Out_2_Vector3, _NormalBlend_cb4851ae94454d539b1004bdc1f2604f_Out_2_Vector3);
#if defined(_USE_LAYER_1)
float3 _UseLayer1_7da71e644bf2434f99e8005bad9e8f14_Out_0_Vector3 = _NormalBlend_cb4851ae94454d539b1004bdc1f2604f_Out_2_Vector3;
#else
float3 _UseLayer1_7da71e644bf2434f99e8005bad9e8f14_Out_0_Vector3 = _NormalStrength_3eb407934b854455a959c26453776991_Out_2_Vector3;
#endif
float3 _Transform_04398ac1bc954fb19c62ce3f5f6d670c_Out_1_Vector3;
{
float3x3 tangentTransform = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
_Transform_04398ac1bc954fb19c62ce3f5f6d670c_Out_1_Vector3 = TransformTangentToWorld(_UseLayer1_7da71e644bf2434f99e8005bad9e8f14_Out_0_Vector3.xyz, tangentTransform, true);
}
float _DotProduct_ae13069c870e4539be3826642299bede_Out_2_Float;
Unity_DotProduct_float3(_Transform_04398ac1bc954fb19c62ce3f5f6d670c_Out_1_Vector3, float3(0, 1, 0), _DotProduct_ae13069c870e4539be3826642299bede_Out_2_Float);
float _Smoothstep_652b53e75f9e481e8c829d3bd3d9c24e_Out_3_Float;
Unity_Smoothstep_float(_Property_f13be210d8b049a9a1e6c0e219f5a53f_Out_0_Float, _Add_4177ca63c9724385910d37f97d0e8c48_Out_2_Float, _DotProduct_ae13069c870e4539be3826642299bede_Out_2_Float, _Smoothstep_652b53e75f9e481e8c829d3bd3d9c24e_Out_3_Float);
float _Property_64938267e5134c5b9a4256d11aa25705_Out_0_Float = _Coverage_Height_Mask_Pivot_Offset;
float _Property_cfcce6f026924b048f8c33bdc73c14f6_Out_0_Float = _Coverage_Height_Mask_Length;
Bindings_MFPivotVerticalGradience_c9e909c1845b8ae4b9a11aa715963787_float _MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c;
_MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c.WorldSpacePosition = IN.WorldSpacePosition;
float _MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c_Mask_1_Float;
SG_MFPivotVerticalGradience_c9e909c1845b8ae4b9a11aa715963787_float(_Property_64938267e5134c5b9a4256d11aa25705_Out_0_Float, _Property_cfcce6f026924b048f8c33bdc73c14f6_Out_0_Float, _MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c, _MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c_Mask_1_Float);
float _Multiply_f6a21f2d15b7472e8173002af669cfdf_Out_2_Float;
Unity_Multiply_float_float(_Smoothstep_652b53e75f9e481e8c829d3bd3d9c24e_Out_3_Float, _MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c_Mask_1_Float, _Multiply_f6a21f2d15b7472e8173002af669cfdf_Out_2_Float);
float3 _Lerp_90a44aeed76b4e67b245ea63960c28fc_Out_3_Vector3;
Unity_Lerp_float3(_Blend_e613dc790cd741d3b367cb48289cffd5_Out_2_Vector3, _Multiply_c28f28e86afd43f584c04c39cc44c8c4_Out_2_Vector3, (_Multiply_f6a21f2d15b7472e8173002af669cfdf_Out_2_Float.xxx), _Lerp_90a44aeed76b4e67b245ea63960c28fc_Out_3_Vector3);
#if defined(_USE_COVERAGE_LAYER)
float3 _UseCoverageLayer_402efe74563f417bb22b7997bdfc5c1e_Out_0_Vector3 = _Lerp_90a44aeed76b4e67b245ea63960c28fc_Out_3_Vector3;
#else
float3 _UseCoverageLayer_402efe74563f417bb22b7997bdfc5c1e_Out_0_Vector3 = _Blend_e613dc790cd741d3b367cb48289cffd5_Out_2_Vector3;
#endif
UnityTexture2D _Property_022d04960c1e41229b03a237b2bf022a_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Coverage_Normal_Map, sampler_Coverage_Normal_Map, _Coverage_Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_022d04960c1e41229b03a237b2bf022a_Out_0_Texture2D.tex, _Property_022d04960c1e41229b03a237b2bf022a_Out_0_Texture2D.samplerstate, _Property_022d04960c1e41229b03a237b2bf022a_Out_0_Texture2D.GetTransformedUV((_Multiply_282d032113614cb58779c3a33671ad6c_Out_2_Vector3.xy)) );
if (_Property_022d04960c1e41229b03a237b2bf022a_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4, _Property_022d04960c1e41229b03a237b2bf022a_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4);
float _SampleTexture2D_046783cfeeed4edebbc5805118c09db7_R_4_Float = _SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4.r;
float _SampleTexture2D_046783cfeeed4edebbc5805118c09db7_G_5_Float = _SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4.g;
float _SampleTexture2D_046783cfeeed4edebbc5805118c09db7_B_6_Float = _SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4.b;
float _SampleTexture2D_046783cfeeed4edebbc5805118c09db7_A_7_Float = _SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4.a;
float _Property_aff65d79d743476f9e681da55383046d_Out_0_Float = _Coverage_Normal_Strength;
float3 _NormalStrength_f478d3c35fc741e78611a44ac09caa44_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4.xyz), _Property_aff65d79d743476f9e681da55383046d_Out_0_Float, _NormalStrength_f478d3c35fc741e78611a44ac09caa44_Out_2_Vector3);
float3 _Lerp_0e62040e3ecb4bf3b09d8424f7147818_Out_3_Vector3;
Unity_Lerp_float3(float3(0, 0, 1), _NormalStrength_f478d3c35fc741e78611a44ac09caa44_Out_2_Vector3, (_Multiply_f6a21f2d15b7472e8173002af669cfdf_Out_2_Float.xxx), _Lerp_0e62040e3ecb4bf3b09d8424f7147818_Out_3_Vector3);
float3 _NormalBlend_4ffbea1bfb5d43979177bcfb52b4abcf_Out_2_Vector3;
Unity_NormalBlend_float(_Lerp_0e62040e3ecb4bf3b09d8424f7147818_Out_3_Vector3, _UseLayer1_7da71e644bf2434f99e8005bad9e8f14_Out_0_Vector3, _NormalBlend_4ffbea1bfb5d43979177bcfb52b4abcf_Out_2_Vector3);
#if defined(_USE_COVERAGE_LAYER)
float3 _UseCoverageLayer_b0f12be34bf24ec7ba2137cef5a254cb_Out_0_Vector3 = _NormalBlend_4ffbea1bfb5d43979177bcfb52b4abcf_Out_2_Vector3;
#else
float3 _UseCoverageLayer_b0f12be34bf24ec7ba2137cef5a254cb_Out_0_Vector3 = _UseLayer1_7da71e644bf2434f99e8005bad9e8f14_Out_0_Vector3;
#endif
float _OneMinus_7a2b995f5d6a4a619d8a66eddf498446_Out_1_Float;
Unity_OneMinus_float(_Split_03d41d27a027425fa76a6c910dfbed96_A_4_Float, _OneMinus_7a2b995f5d6a4a619d8a66eddf498446_Out_1_Float);
float _Property_3f3b47f88ff24548ba31a6777c3b2a85_Out_0_Float = _Layer_1_Smoothness_Strength;
float _Lerp_f0ccf02f1b3a423eb14231584191b608_Out_3_Float;
Unity_Lerp_float(float(0), _OneMinus_7a2b995f5d6a4a619d8a66eddf498446_Out_1_Float, _Property_3f3b47f88ff24548ba31a6777c3b2a85_Out_0_Float, _Lerp_f0ccf02f1b3a423eb14231584191b608_Out_3_Float);
float _OneMinus_16665b1602fc4acaa17e0caa00dd6ac7_Out_1_Float;
Unity_OneMinus_float(_Split_d38832e5c62c44d3a795b83acceab016_A_4_Float, _OneMinus_16665b1602fc4acaa17e0caa00dd6ac7_Out_1_Float);
float _Property_3bfd1b8cbe534165807a6d38b20b8b3f_Out_0_Float = _Layer_2_Smoothness_Strength;
float _Lerp_5f73c06703364fa1b14c25a300968493_Out_3_Float;
Unity_Lerp_float(float(0), _OneMinus_16665b1602fc4acaa17e0caa00dd6ac7_Out_1_Float, _Property_3bfd1b8cbe534165807a6d38b20b8b3f_Out_0_Float, _Lerp_5f73c06703364fa1b14c25a300968493_Out_3_Float);
float _Lerp_b5d123a1ad4c4307979f2143cfd5d3ac_Out_3_Float;
Unity_Lerp_float(_Lerp_f0ccf02f1b3a423eb14231584191b608_Out_3_Float, _Lerp_5f73c06703364fa1b14c25a300968493_Out_3_Float, _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_G_5_Float, _Lerp_b5d123a1ad4c4307979f2143cfd5d3ac_Out_3_Float);
float _OneMinus_afad5b1133a04d90b4af7d2e859ee909_Out_1_Float;
Unity_OneMinus_float(_Split_838946d3ce6f42cfbaf41fff836d6985_A_4_Float, _OneMinus_afad5b1133a04d90b4af7d2e859ee909_Out_1_Float);
float _Property_269daba898014930a8cc45eae3b32a0d_Out_0_Float = _Layer_3_Smoothness_Strength;
float _Lerp_73575fb5baba4126bfd6048130db9abc_Out_3_Float;
Unity_Lerp_float(float(0), _OneMinus_afad5b1133a04d90b4af7d2e859ee909_Out_1_Float, _Property_269daba898014930a8cc45eae3b32a0d_Out_0_Float, _Lerp_73575fb5baba4126bfd6048130db9abc_Out_3_Float);
float _Lerp_0ed87b2c815a4b6bb1c07d0ba2ad7f9e_Out_3_Float;
Unity_Lerp_float(_Lerp_b5d123a1ad4c4307979f2143cfd5d3ac_Out_3_Float, _Lerp_73575fb5baba4126bfd6048130db9abc_Out_3_Float, _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_B_6_Float, _Lerp_0ed87b2c815a4b6bb1c07d0ba2ad7f9e_Out_3_Float);
#if defined(_USE_LAYER_3)
float _UseLayer3_38584d454ac2471687bb98edbed983aa_Out_0_Float = _Lerp_0ed87b2c815a4b6bb1c07d0ba2ad7f9e_Out_3_Float;
#else
float _UseLayer3_38584d454ac2471687bb98edbed983aa_Out_0_Float = _Lerp_b5d123a1ad4c4307979f2143cfd5d3ac_Out_3_Float;
#endif
#if defined(_USE_LAYER_2)
float _UseLayer2_66f28f7fecc141028a5f15673f047917_Out_0_Float = _UseLayer3_38584d454ac2471687bb98edbed983aa_Out_0_Float;
#else
float _UseLayer2_66f28f7fecc141028a5f15673f047917_Out_0_Float = _Lerp_f0ccf02f1b3a423eb14231584191b608_Out_3_Float;
#endif
#if defined(_USE_LAYER_1)
float _UseLayer1_6040c5b1bc15438dae7a97e54a8e3324_Out_0_Float = _UseLayer2_66f28f7fecc141028a5f15673f047917_Out_0_Float;
#else
float _UseLayer1_6040c5b1bc15438dae7a97e54a8e3324_Out_0_Float = float(0);
#endif
float _OneMinus_f65c9822794443558d97581fc4537919_Out_1_Float;
Unity_OneMinus_float(_Split_42bc736ed19d41c2bc8a58c7f653b802_A_4_Float, _OneMinus_f65c9822794443558d97581fc4537919_Out_1_Float);
float _Property_eb619aeb69fd4914bebabe935f6e2c25_Out_0_Float = _Coverage_Smoothness_Strength;
float _Lerp_2c28e4b743af489e941e7226cc40512b_Out_3_Float;
Unity_Lerp_float(float(0), _OneMinus_f65c9822794443558d97581fc4537919_Out_1_Float, _Property_eb619aeb69fd4914bebabe935f6e2c25_Out_0_Float, _Lerp_2c28e4b743af489e941e7226cc40512b_Out_3_Float);
float _Lerp_1237b11b7e0241aba05ae87b4cdb9167_Out_3_Float;
Unity_Lerp_float(_UseLayer1_6040c5b1bc15438dae7a97e54a8e3324_Out_0_Float, _Lerp_2c28e4b743af489e941e7226cc40512b_Out_3_Float, _Multiply_f6a21f2d15b7472e8173002af669cfdf_Out_2_Float, _Lerp_1237b11b7e0241aba05ae87b4cdb9167_Out_3_Float);
#if defined(_USE_COVERAGE_LAYER)
float _UseCoverageLayer_3e91540d12544fcfa939ef997f0c72e0_Out_0_Float = _Lerp_1237b11b7e0241aba05ae87b4cdb9167_Out_3_Float;
#else
float _UseCoverageLayer_3e91540d12544fcfa939ef997f0c72e0_Out_0_Float = _UseLayer1_6040c5b1bc15438dae7a97e54a8e3324_Out_0_Float;
#endif
surface.BaseColor = _UseCoverageLayer_402efe74563f417bb22b7997bdfc5c1e_Out_0_Vector3;
surface.NormalTS = _UseCoverageLayer_b0f12be34bf24ec7ba2137cef5a254cb_Out_0_Vector3;
surface.Emission = float3(0, 0, 0);
surface.Metallic = float(0);
surface.Smoothness = _UseCoverageLayer_3e91540d12544fcfa939ef997f0c72e0_Out_0_Float;
surface.Occlusion = float(1);
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
    output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);

    // to preserve mikktspace compliance we use same scale renormFactor as was used on the normal.
    // This is explained in section 2.2 in "surface gradient based bump mapping framework"
    output.WorldSpaceTangent = renormFactor * input.tangentWS.xyz;
    output.WorldSpaceBiTangent = renormFactor * bitang;

    output.WorldSpacePosition = input.positionWS;

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
#pragma shader_feature_local _ _USE_LAYER_2
#pragma shader_feature_local _ _USE_LAYER_3
#pragma shader_feature_local _ _USE_COVERAGE_LAYER
#pragma shader_feature_local _ _USE_LAYER_1

#if defined(_USE_LAYER_2) && defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_3) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_2
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_3)
#define KEYWORD_PERMUTATION_3
#elif defined(_USE_LAYER_2) && defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_4
#elif defined(_USE_LAYER_2) && defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_5
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_6
#elif defined(_USE_LAYER_2)
#define KEYWORD_PERMUTATION_7
#elif defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_8
#elif defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_9
#elif defined(_USE_LAYER_3) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_10
#elif defined(_USE_LAYER_3)
#define KEYWORD_PERMUTATION_11
#elif defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_12
#elif defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_13
#elif defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_14
#else
#define KEYWORD_PERMUTATION_15
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
 float3 WorldSpaceTangent;
 float3 WorldSpaceBiTangent;
 float3 WorldSpacePosition;
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

float4 _Layer_1_Color_Map_TexelSize;
float _Layer_1_Hue;
float _Layer_1_Brightness;
float _Coverage_Height_Mask_Length;
float4 _Layer_1_Normal_Map_TexelSize;
float _Layer_1_Normal_Strength;
float _Layer_1_Tiling;
float _Layer_1_Smoothness_Strength;
float _Layer_2_Tiling;
float4 _Layer_2_Color_Map_TexelSize;
float _Layer_2_Hue;
float _Layer_2_Brightness;
float _Layer_2_Smoothness_Strength;
float4 _Layer_2_Normal_Map_TexelSize;
float _Layer_2_Normal_Strength;
float _Layer_3_Tiling;
float4 _Layer_3_Color_Map_TexelSize;
float _Layer_3_Hue;
float _Layer_3_Brightness;
float _Layer_3_Smoothness_Strength;
float4 _Layer_3_Normal_Map_TexelSize;
float _Layer_3_Normal_Strength;
float4 _RGB_Map_TexelSize;
float4 _Normal_Map_TexelSize;
float _Normal_Strength;
float _Coverage_Tiling;
float4 _Coverage_Color_Map_TexelSize;
float _Coverage_Hue;
float _Coverage_Brightness;
float _Coverage_Smoothness_Strength;
float4 _Coverage_Normal_Map_TexelSize;
float _Coverage_Normal_Strength;
float4 _Edge_Map_TexelSize;
float _Dirt_Mask_Contrast;
float _AO_Mask_Contrast;
float _Edgewear_Mask_Contrast;
float _Stylization_Mask_Contrast;
float4 _Stylization_Color;
float4 _AO_Color;
float4 _Dirt_Color;
float4 _Edgewear_Color;
float _Coverage_Normal_Mask_Slope;
float _Coverage_Normal_Mask_Gradience;
float _Coverage_Height_Mask_Pivot_Offset;
float _Scale_UVs_with_Object;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Layer_1_Color_Map);
SAMPLER(sampler_Layer_1_Color_Map);
TEXTURE2D(_Layer_1_Normal_Map);
SAMPLER(sampler_Layer_1_Normal_Map);
TEXTURE2D(_Layer_2_Color_Map);
SAMPLER(sampler_Layer_2_Color_Map);
TEXTURE2D(_Layer_2_Normal_Map);
SAMPLER(sampler_Layer_2_Normal_Map);
TEXTURE2D(_Layer_3_Color_Map);
SAMPLER(sampler_Layer_3_Color_Map);
TEXTURE2D(_Layer_3_Normal_Map);
SAMPLER(sampler_Layer_3_Normal_Map);
TEXTURE2D(_RGB_Map);
SAMPLER(sampler_RGB_Map);
TEXTURE2D(_Normal_Map);
SAMPLER(sampler_Normal_Map);
TEXTURE2D(_Coverage_Color_Map);
SAMPLER(sampler_Coverage_Color_Map);
TEXTURE2D(_Coverage_Normal_Map);
SAMPLER(sampler_Coverage_Normal_Map);
TEXTURE2D(_Edge_Map);
SAMPLER(sampler_Edge_Map);

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

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Hue_Normalized_float(float3 In, float Offset, out float3 Out)
{
    // RGB to HSV
    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 P = lerp(float4(In.bg, K.wz), float4(In.gb, K.xy), step(In.b, In.g));
    float4 Q = lerp(float4(P.xyw, In.r), float4(In.r, P.yzx), step(P.x, In.r));
    float D = Q.x - min(Q.w, Q.y);
    float E = 1e-10;
    float V = (D == 0) ? Q.x : (Q.x + E);
    float3 hsv = float3(abs(Q.z + (Q.w - Q.y)/(6.0 * D + E)), D / (Q.x + E), V);

    float hue = hsv.x + Offset;
    hsv.x = (hue < 0)
            ? hue + 1
            : (hue > 1)
                ? hue - 1
                : hue;

    // HSV to RGB
    float4 K2 = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 P2 = abs(frac(hsv.xxx + K2.xyz) * 6.0 - K2.www);
    Out = hsv.z * lerp(K2.xxx, saturate(P2 - K2.xxx), hsv.y);
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Lerp_float(float A, float B, float T, out float Out)
{
    Out = lerp(A, B, T);
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

struct Bindings_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float
{
};

void SG_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float(float _Input, float _Contrast, Bindings_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float IN, out float Out_1)
{
float _Property_f17f2d608b764773bc9f33ea28c3e14e_Out_0_Float = _Contrast;
float _Add_59838b3eb9ec437bbd93b6fd8db7a0c4_Out_2_Float;
Unity_Add_float(_Property_f17f2d608b764773bc9f33ea28c3e14e_Out_0_Float, float(-1), _Add_59838b3eb9ec437bbd93b6fd8db7a0c4_Out_2_Float);
float _Add_524e73a4aa0b4e7982442fdf0d39fe18_Out_2_Float;
Unity_Add_float(_Property_f17f2d608b764773bc9f33ea28c3e14e_Out_0_Float, float(1), _Add_524e73a4aa0b4e7982442fdf0d39fe18_Out_2_Float);
float _Property_df60db08578f4909bad9a81c68245e45_Out_0_Float = _Input;
float _Lerp_6af4dfb2e0f0426a81a1669c55ec8196_Out_3_Float;
Unity_Lerp_float(_Add_59838b3eb9ec437bbd93b6fd8db7a0c4_Out_2_Float, _Add_524e73a4aa0b4e7982442fdf0d39fe18_Out_2_Float, _Property_df60db08578f4909bad9a81c68245e45_Out_0_Float, _Lerp_6af4dfb2e0f0426a81a1669c55ec8196_Out_3_Float);
float _Saturate_59b070c1298d4279be831e68d2cd96c8_Out_1_Float;
Unity_Saturate_float(_Lerp_6af4dfb2e0f0426a81a1669c55ec8196_Out_3_Float, _Saturate_59b070c1298d4279be831e68d2cd96c8_Out_1_Float);
Out_1 = _Saturate_59b070c1298d4279be831e68d2cd96c8_Out_1_Float;
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Blend_Multiply_float3(float3 Base, float3 Blend, out float3 Out, float Opacity)
{
    Out = Base * Blend;
    Out = lerp(Base, Out, Opacity);
}

void Unity_Blend_Overlay_float3(float3 Base, float3 Blend, out float3 Out, float Opacity)
{
    float3 result1 = 1.0 - 2.0 * (1.0 - Base) * (1.0 - Blend);
    float3 result2 = 2.0 * Base * Blend;
    float3 zeroOrOne = step(Base, 0.5);
    Out = result2 * zeroOrOne + (1 - zeroOrOne) * result1;
    Out = lerp(Base, Out, Opacity);
}

void Unity_NormalStrength_float(float3 In, float Strength, out float3 Out)
{
    Out = float3(In.rg * Strength, lerp(1, In.b, saturate(Strength)));
}

void Unity_NormalBlend_float(float3 A, float3 B, out float3 Out)
{
    Out = SafeNormalize(float3(A.rg + B.rg, A.b * B.b));
}

void Unity_DotProduct_float3(float3 A, float3 B, out float Out)
{
    Out = dot(A, B);
}

void Unity_Smoothstep_float(float Edge1, float Edge2, float In, out float Out)
{
    Out = smoothstep(Edge1, Edge2, In);
}

void Unity_Subtract_float3(float3 A, float3 B, out float3 Out)
{
    Out = A - B;
}

struct Bindings_MFPivotVerticalGradience_c9e909c1845b8ae4b9a11aa715963787_float
{
float3 WorldSpacePosition;
};

void SG_MFPivotVerticalGradience_c9e909c1845b8ae4b9a11aa715963787_float(float _Offset, float _Length, Bindings_MFPivotVerticalGradience_c9e909c1845b8ae4b9a11aa715963787_float IN, out float Mask_1)
{
float _Property_4680dca2dee245e7aa77915a0b17cce9_Out_0_Float = _Offset;
float _Property_c763914b9bbb4da5abad7a3db51adcb2_Out_0_Float = _Length;
float _Add_ad1531c9af874822b29fd9a13421289e_Out_2_Float;
Unity_Add_float(_Property_4680dca2dee245e7aa77915a0b17cce9_Out_0_Float, _Property_c763914b9bbb4da5abad7a3db51adcb2_Out_0_Float, _Add_ad1531c9af874822b29fd9a13421289e_Out_2_Float);
float3 _Subtract_69e80d3fb1bb4d71b4be84a010b280ff_Out_2_Vector3;
Unity_Subtract_float3(IN.WorldSpacePosition, SHADERGRAPH_OBJECT_POSITION, _Subtract_69e80d3fb1bb4d71b4be84a010b280ff_Out_2_Vector3);
float _Split_bceff54ddef34fab8cfa9a2a0b8bbc58_R_1_Float = _Subtract_69e80d3fb1bb4d71b4be84a010b280ff_Out_2_Vector3[0];
float _Split_bceff54ddef34fab8cfa9a2a0b8bbc58_G_2_Float = _Subtract_69e80d3fb1bb4d71b4be84a010b280ff_Out_2_Vector3[1];
float _Split_bceff54ddef34fab8cfa9a2a0b8bbc58_B_3_Float = _Subtract_69e80d3fb1bb4d71b4be84a010b280ff_Out_2_Vector3[2];
float _Split_bceff54ddef34fab8cfa9a2a0b8bbc58_A_4_Float = 0;
float _Smoothstep_0109929c8f9e41888dfa7ee4fd85c377_Out_3_Float;
Unity_Smoothstep_float(_Property_4680dca2dee245e7aa77915a0b17cce9_Out_0_Float, _Add_ad1531c9af874822b29fd9a13421289e_Out_2_Float, _Split_bceff54ddef34fab8cfa9a2a0b8bbc58_G_2_Float, _Smoothstep_0109929c8f9e41888dfa7ee4fd85c377_Out_3_Float);
Mask_1 = _Smoothstep_0109929c8f9e41888dfa7ee4fd85c377_Out_3_Float;
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
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
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
UnityTexture2D _Property_82391a4213d745e08202065048659634_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_1_Color_Map, sampler_Layer_1_Color_Map, _Layer_1_Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_7afe6a7edf62460887e09c4746d796b6_Out_0_Vector4 = IN.uv0;
float3 _Multiply_15b5d23699554ab298d90333ce57d387_Out_2_Vector3;
Unity_Multiply_float3_float3((_UV_7afe6a7edf62460887e09c4746d796b6_Out_0_Vector4.xyz), float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z))), _Multiply_15b5d23699554ab298d90333ce57d387_Out_2_Vector3);
float _Property_9a5b0bf1bf584876a406e2b633741c94_Out_0_Float = _Scale_UVs_with_Object;
float3 _Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3;
Unity_Lerp_float3((_UV_7afe6a7edf62460887e09c4746d796b6_Out_0_Vector4.xyz), _Multiply_15b5d23699554ab298d90333ce57d387_Out_2_Vector3, (_Property_9a5b0bf1bf584876a406e2b633741c94_Out_0_Float.xxx), _Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3);
float _Property_460238447f6b4170aa7397df1262be96_Out_0_Float = _Layer_1_Tiling;
float3 _Multiply_94cb4adbcc9a4b25980974e6be235287_Out_2_Vector3;
Unity_Multiply_float3_float3(_Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3, (_Property_460238447f6b4170aa7397df1262be96_Out_0_Float.xxx), _Multiply_94cb4adbcc9a4b25980974e6be235287_Out_2_Vector3);
float4 _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_82391a4213d745e08202065048659634_Out_0_Texture2D.tex, _Property_82391a4213d745e08202065048659634_Out_0_Texture2D.samplerstate, _Property_82391a4213d745e08202065048659634_Out_0_Texture2D.GetTransformedUV((_Multiply_94cb4adbcc9a4b25980974e6be235287_Out_2_Vector3.xy)) );
if (_Property_82391a4213d745e08202065048659634_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4, _Property_82391a4213d745e08202065048659634_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_R_4_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4.r;
float _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_G_5_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4.g;
float _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_B_6_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4.b;
float _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_A_7_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4.a;
float _Split_03d41d27a027425fa76a6c910dfbed96_R_1_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4[0];
float _Split_03d41d27a027425fa76a6c910dfbed96_G_2_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4[1];
float _Split_03d41d27a027425fa76a6c910dfbed96_B_3_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4[2];
float _Split_03d41d27a027425fa76a6c910dfbed96_A_4_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4[3];
float3 _Vector3_29d44982f90c486b8d58d91423294375_Out_0_Vector3 = float3(_Split_03d41d27a027425fa76a6c910dfbed96_R_1_Float, _Split_03d41d27a027425fa76a6c910dfbed96_G_2_Float, _Split_03d41d27a027425fa76a6c910dfbed96_B_3_Float);
float _Property_c55f5c80dc494683850ea3803571112b_Out_0_Float = _Layer_1_Hue;
float3 _Hue_4528903dcb5b4b15be0e273df247bfd9_Out_2_Vector3;
Unity_Hue_Normalized_float(_Vector3_29d44982f90c486b8d58d91423294375_Out_0_Vector3, _Property_c55f5c80dc494683850ea3803571112b_Out_0_Float, _Hue_4528903dcb5b4b15be0e273df247bfd9_Out_2_Vector3);
float _Property_ba7720a9d8134eb58030eb416c6a024a_Out_0_Float = _Layer_1_Brightness;
float3 _Multiply_01d7b815489a446fa8f5fbd7bf096d4f_Out_2_Vector3;
Unity_Multiply_float3_float3(_Hue_4528903dcb5b4b15be0e273df247bfd9_Out_2_Vector3, (_Property_ba7720a9d8134eb58030eb416c6a024a_Out_0_Float.xxx), _Multiply_01d7b815489a446fa8f5fbd7bf096d4f_Out_2_Vector3);
UnityTexture2D _Property_f37e4daa8ff94f5eb2ca9f6f83a20bb3_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_2_Color_Map, sampler_Layer_2_Color_Map, _Layer_2_Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_812f9793e99740fcb534b8c8d8af0ff2_Out_0_Float = _Layer_2_Tiling;
float3 _Multiply_7719c519f67d4b1aaaf2cca1cc2d4d79_Out_2_Vector3;
Unity_Multiply_float3_float3(_Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3, (_Property_812f9793e99740fcb534b8c8d8af0ff2_Out_0_Float.xxx), _Multiply_7719c519f67d4b1aaaf2cca1cc2d4d79_Out_2_Vector3);
float4 _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_f37e4daa8ff94f5eb2ca9f6f83a20bb3_Out_0_Texture2D.tex, _Property_f37e4daa8ff94f5eb2ca9f6f83a20bb3_Out_0_Texture2D.samplerstate, _Property_f37e4daa8ff94f5eb2ca9f6f83a20bb3_Out_0_Texture2D.GetTransformedUV((_Multiply_7719c519f67d4b1aaaf2cca1cc2d4d79_Out_2_Vector3.xy)) );
if (_Property_f37e4daa8ff94f5eb2ca9f6f83a20bb3_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4, _Property_f37e4daa8ff94f5eb2ca9f6f83a20bb3_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_R_4_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4.r;
float _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_G_5_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4.g;
float _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_B_6_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4.b;
float _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_A_7_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4.a;
float _Split_d38832e5c62c44d3a795b83acceab016_R_1_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4[0];
float _Split_d38832e5c62c44d3a795b83acceab016_G_2_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4[1];
float _Split_d38832e5c62c44d3a795b83acceab016_B_3_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4[2];
float _Split_d38832e5c62c44d3a795b83acceab016_A_4_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4[3];
float3 _Vector3_cb2069d38949469a976ac3a5a2003bd7_Out_0_Vector3 = float3(_Split_d38832e5c62c44d3a795b83acceab016_R_1_Float, _Split_d38832e5c62c44d3a795b83acceab016_G_2_Float, _Split_d38832e5c62c44d3a795b83acceab016_B_3_Float);
float _Property_8970d5f05c7d4b5783a57e1c656ee4a9_Out_0_Float = _Layer_2_Hue;
float3 _Hue_d8383543ffa64d25a0efd5fc36520be4_Out_2_Vector3;
Unity_Hue_Normalized_float(_Vector3_cb2069d38949469a976ac3a5a2003bd7_Out_0_Vector3, _Property_8970d5f05c7d4b5783a57e1c656ee4a9_Out_0_Float, _Hue_d8383543ffa64d25a0efd5fc36520be4_Out_2_Vector3);
float _Property_40475d05242a4efaa3e83e117ecd268d_Out_0_Float = _Layer_2_Brightness;
float3 _Multiply_9a8ec1bf43a149a7a5fb1571321b8200_Out_2_Vector3;
Unity_Multiply_float3_float3(_Hue_d8383543ffa64d25a0efd5fc36520be4_Out_2_Vector3, (_Property_40475d05242a4efaa3e83e117ecd268d_Out_0_Float.xxx), _Multiply_9a8ec1bf43a149a7a5fb1571321b8200_Out_2_Vector3);
UnityTexture2D _Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_RGB_Map, sampler_RGB_Map, _RGB_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D.tex, _Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D.samplerstate, _Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4, _Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_R_4_Float = _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4.r;
float _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_G_5_Float = _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4.g;
float _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_B_6_Float = _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4.b;
float _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_A_7_Float = _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4.a;
float3 _Lerp_754b59c21e034e87848bb65af4294c76_Out_3_Vector3;
Unity_Lerp_float3(_Multiply_01d7b815489a446fa8f5fbd7bf096d4f_Out_2_Vector3, _Multiply_9a8ec1bf43a149a7a5fb1571321b8200_Out_2_Vector3, (_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_G_5_Float.xxx), _Lerp_754b59c21e034e87848bb65af4294c76_Out_3_Vector3);
UnityTexture2D _Property_fef134089dc1401db12f51526b8ed988_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_3_Color_Map, sampler_Layer_3_Color_Map, _Layer_3_Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_8d20917a655e4062a5ce52f974969006_Out_0_Float = _Layer_3_Tiling;
float3 _Multiply_04b9ab405b2d4c27b6dd0696d9a5eacf_Out_2_Vector3;
Unity_Multiply_float3_float3(_Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3, (_Property_8d20917a655e4062a5ce52f974969006_Out_0_Float.xxx), _Multiply_04b9ab405b2d4c27b6dd0696d9a5eacf_Out_2_Vector3);
float4 _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_fef134089dc1401db12f51526b8ed988_Out_0_Texture2D.tex, _Property_fef134089dc1401db12f51526b8ed988_Out_0_Texture2D.samplerstate, _Property_fef134089dc1401db12f51526b8ed988_Out_0_Texture2D.GetTransformedUV((_Multiply_04b9ab405b2d4c27b6dd0696d9a5eacf_Out_2_Vector3.xy)) );
if (_Property_fef134089dc1401db12f51526b8ed988_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4, _Property_fef134089dc1401db12f51526b8ed988_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_R_4_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4.r;
float _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_G_5_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4.g;
float _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_B_6_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4.b;
float _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_A_7_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4.a;
float _Split_838946d3ce6f42cfbaf41fff836d6985_R_1_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4[0];
float _Split_838946d3ce6f42cfbaf41fff836d6985_G_2_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4[1];
float _Split_838946d3ce6f42cfbaf41fff836d6985_B_3_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4[2];
float _Split_838946d3ce6f42cfbaf41fff836d6985_A_4_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4[3];
float3 _Vector3_2a694d435907494fb07f2f9db09d031a_Out_0_Vector3 = float3(_Split_838946d3ce6f42cfbaf41fff836d6985_R_1_Float, _Split_838946d3ce6f42cfbaf41fff836d6985_G_2_Float, _Split_838946d3ce6f42cfbaf41fff836d6985_B_3_Float);
float _Property_6ba5eab2fbda4899981bd9c4c9c6317e_Out_0_Float = _Layer_3_Hue;
float3 _Hue_0a936fcae1974948a815da96c316b7e3_Out_2_Vector3;
Unity_Hue_Normalized_float(_Vector3_2a694d435907494fb07f2f9db09d031a_Out_0_Vector3, _Property_6ba5eab2fbda4899981bd9c4c9c6317e_Out_0_Float, _Hue_0a936fcae1974948a815da96c316b7e3_Out_2_Vector3);
float _Property_c749f04d24d9423fa1a2859f800dc893_Out_0_Float = _Layer_3_Brightness;
float3 _Multiply_db73d490dc9d432e80685ef18e5427fb_Out_2_Vector3;
Unity_Multiply_float3_float3(_Hue_0a936fcae1974948a815da96c316b7e3_Out_2_Vector3, (_Property_c749f04d24d9423fa1a2859f800dc893_Out_0_Float.xxx), _Multiply_db73d490dc9d432e80685ef18e5427fb_Out_2_Vector3);
float3 _Lerp_66f917f82ed14fd89c6c2fe066b5e834_Out_3_Vector3;
Unity_Lerp_float3(_Lerp_754b59c21e034e87848bb65af4294c76_Out_3_Vector3, _Multiply_db73d490dc9d432e80685ef18e5427fb_Out_2_Vector3, (_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_B_6_Float.xxx), _Lerp_66f917f82ed14fd89c6c2fe066b5e834_Out_3_Vector3);
#if defined(_USE_LAYER_3)
float3 _UseLayer3_ee2b565dd7774fa7a5c88d0b7bfd05b8_Out_0_Vector3 = _Lerp_66f917f82ed14fd89c6c2fe066b5e834_Out_3_Vector3;
#else
float3 _UseLayer3_ee2b565dd7774fa7a5c88d0b7bfd05b8_Out_0_Vector3 = _Lerp_754b59c21e034e87848bb65af4294c76_Out_3_Vector3;
#endif
#if defined(_USE_LAYER_2)
float3 _UseLayer2_22b88f361b80488fbbec2df7f45c4924_Out_0_Vector3 = _UseLayer3_ee2b565dd7774fa7a5c88d0b7bfd05b8_Out_0_Vector3;
#else
float3 _UseLayer2_22b88f361b80488fbbec2df7f45c4924_Out_0_Vector3 = _Multiply_01d7b815489a446fa8f5fbd7bf096d4f_Out_2_Vector3;
#endif
float4 _Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4 = _Stylization_Color;
float _Split_42e867e97f454925a658dba60e21864f_R_1_Float = _Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4[0];
float _Split_42e867e97f454925a658dba60e21864f_G_2_Float = _Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4[1];
float _Split_42e867e97f454925a658dba60e21864f_B_3_Float = _Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4[2];
float _Split_42e867e97f454925a658dba60e21864f_A_4_Float = _Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4[3];
UnityTexture2D _Property_c02e202098944eebbd44663588c64e3b_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Edge_Map, sampler_Edge_Map, _Edge_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_c02e202098944eebbd44663588c64e3b_Out_0_Texture2D.tex, _Property_c02e202098944eebbd44663588c64e3b_Out_0_Texture2D.samplerstate, _Property_c02e202098944eebbd44663588c64e3b_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_c02e202098944eebbd44663588c64e3b_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4, _Property_c02e202098944eebbd44663588c64e3b_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_R_4_Float = _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4.r;
float _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_G_5_Float = _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4.g;
float _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_B_6_Float = _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4.b;
float _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_A_7_Float = _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4.a;
float _Property_a4422d37671342bdab8d700fe4c2424a_Out_0_Float = _Stylization_Mask_Contrast;
Bindings_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float _MFCheapContrast_2e3888093b39403781e2ccbee9980699;
float _MFCheapContrast_2e3888093b39403781e2ccbee9980699_Out_1_Float;
SG_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float(_SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_A_7_Float, _Property_a4422d37671342bdab8d700fe4c2424a_Out_0_Float, _MFCheapContrast_2e3888093b39403781e2ccbee9980699, _MFCheapContrast_2e3888093b39403781e2ccbee9980699_Out_1_Float);
float _Multiply_364f0f8b2ff74453b42f050a8b1cb526_Out_2_Float;
Unity_Multiply_float_float(_Split_42e867e97f454925a658dba60e21864f_A_4_Float, _MFCheapContrast_2e3888093b39403781e2ccbee9980699_Out_1_Float, _Multiply_364f0f8b2ff74453b42f050a8b1cb526_Out_2_Float);
float3 _Lerp_0a1e51abb320477ab63b42e1a32052cc_Out_3_Vector3;
Unity_Lerp_float3(_UseLayer2_22b88f361b80488fbbec2df7f45c4924_Out_0_Vector3, (_Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4.xyz), (_Multiply_364f0f8b2ff74453b42f050a8b1cb526_Out_2_Float.xxx), _Lerp_0a1e51abb320477ab63b42e1a32052cc_Out_3_Vector3);
#if defined(_USE_LAYER_1)
float3 _UseLayer1_f5f8cf003ae64c4187983dc616ecbd3b_Out_0_Vector3 = _Lerp_0a1e51abb320477ab63b42e1a32052cc_Out_3_Vector3;
#else
float3 _UseLayer1_f5f8cf003ae64c4187983dc616ecbd3b_Out_0_Vector3 = (_Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4.xyz);
#endif
float4 _Property_0be88f8e5ba94765a3623024be14bbbf_Out_0_Vector4 = _AO_Color;
float _Split_07ec24369bd1411c82179cabd24aabe5_R_1_Float = _Property_0be88f8e5ba94765a3623024be14bbbf_Out_0_Vector4[0];
float _Split_07ec24369bd1411c82179cabd24aabe5_G_2_Float = _Property_0be88f8e5ba94765a3623024be14bbbf_Out_0_Vector4[1];
float _Split_07ec24369bd1411c82179cabd24aabe5_B_3_Float = _Property_0be88f8e5ba94765a3623024be14bbbf_Out_0_Vector4[2];
float _Split_07ec24369bd1411c82179cabd24aabe5_A_4_Float = _Property_0be88f8e5ba94765a3623024be14bbbf_Out_0_Vector4[3];
float _Property_781496b67f424345bca22ad901512267_Out_0_Float = _AO_Mask_Contrast;
Bindings_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float _MFCheapContrast_74522f75ddab433f9ec9f709fe5a6faf;
float _MFCheapContrast_74522f75ddab433f9ec9f709fe5a6faf_Out_1_Float;
SG_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float(_SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_G_5_Float, _Property_781496b67f424345bca22ad901512267_Out_0_Float, _MFCheapContrast_74522f75ddab433f9ec9f709fe5a6faf, _MFCheapContrast_74522f75ddab433f9ec9f709fe5a6faf_Out_1_Float);
float _Multiply_27eebb10636649c3aefb6ad13580fd9e_Out_2_Float;
Unity_Multiply_float_float(_Split_07ec24369bd1411c82179cabd24aabe5_A_4_Float, _MFCheapContrast_74522f75ddab433f9ec9f709fe5a6faf_Out_1_Float, _Multiply_27eebb10636649c3aefb6ad13580fd9e_Out_2_Float);
float3 _Blend_5baeb7dcebe145dcb53bba406ea82ebd_Out_2_Vector3;
Unity_Blend_Multiply_float3(_UseLayer1_f5f8cf003ae64c4187983dc616ecbd3b_Out_0_Vector3, (_Property_0be88f8e5ba94765a3623024be14bbbf_Out_0_Vector4.xyz), _Blend_5baeb7dcebe145dcb53bba406ea82ebd_Out_2_Vector3, _Multiply_27eebb10636649c3aefb6ad13580fd9e_Out_2_Float);
float4 _Property_64075cb2946e4bc89c0928961fa8e759_Out_0_Vector4 = _Dirt_Color;
float _Split_102b0005351f49a0a7ec214d4c4e879e_R_1_Float = _Property_64075cb2946e4bc89c0928961fa8e759_Out_0_Vector4[0];
float _Split_102b0005351f49a0a7ec214d4c4e879e_G_2_Float = _Property_64075cb2946e4bc89c0928961fa8e759_Out_0_Vector4[1];
float _Split_102b0005351f49a0a7ec214d4c4e879e_B_3_Float = _Property_64075cb2946e4bc89c0928961fa8e759_Out_0_Vector4[2];
float _Split_102b0005351f49a0a7ec214d4c4e879e_A_4_Float = _Property_64075cb2946e4bc89c0928961fa8e759_Out_0_Vector4[3];
float _Property_089f6755c08b483d971238d917dd8b3a_Out_0_Float = _Dirt_Mask_Contrast;
Bindings_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float _MFCheapContrast_96f7295ada8247d48955ac898a7aba11;
float _MFCheapContrast_96f7295ada8247d48955ac898a7aba11_Out_1_Float;
SG_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float(_SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_R_4_Float, _Property_089f6755c08b483d971238d917dd8b3a_Out_0_Float, _MFCheapContrast_96f7295ada8247d48955ac898a7aba11, _MFCheapContrast_96f7295ada8247d48955ac898a7aba11_Out_1_Float);
float _Multiply_bc33d3e78b104e93a60b60cea85cebac_Out_2_Float;
Unity_Multiply_float_float(_Split_102b0005351f49a0a7ec214d4c4e879e_A_4_Float, _MFCheapContrast_96f7295ada8247d48955ac898a7aba11_Out_1_Float, _Multiply_bc33d3e78b104e93a60b60cea85cebac_Out_2_Float);
float3 _Lerp_ef18ae06dc3e4fd18201e3925bc55a85_Out_3_Vector3;
Unity_Lerp_float3(_Blend_5baeb7dcebe145dcb53bba406ea82ebd_Out_2_Vector3, (_Property_64075cb2946e4bc89c0928961fa8e759_Out_0_Vector4.xyz), (_Multiply_bc33d3e78b104e93a60b60cea85cebac_Out_2_Float.xxx), _Lerp_ef18ae06dc3e4fd18201e3925bc55a85_Out_3_Vector3);
float4 _Property_dbda4c8e93f54f92a18b3390cbaf06c0_Out_0_Vector4 = _Edgewear_Color;
float _Split_925676a8ce21420aad811d255e389512_R_1_Float = _Property_dbda4c8e93f54f92a18b3390cbaf06c0_Out_0_Vector4[0];
float _Split_925676a8ce21420aad811d255e389512_G_2_Float = _Property_dbda4c8e93f54f92a18b3390cbaf06c0_Out_0_Vector4[1];
float _Split_925676a8ce21420aad811d255e389512_B_3_Float = _Property_dbda4c8e93f54f92a18b3390cbaf06c0_Out_0_Vector4[2];
float _Split_925676a8ce21420aad811d255e389512_A_4_Float = _Property_dbda4c8e93f54f92a18b3390cbaf06c0_Out_0_Vector4[3];
float _Property_4bef2cb688474ef8909e6dc3f2439616_Out_0_Float = _Edgewear_Mask_Contrast;
Bindings_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float _MFCheapContrast_c8d787e56cf842869980c82103f39754;
float _MFCheapContrast_c8d787e56cf842869980c82103f39754_Out_1_Float;
SG_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float(_SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_B_6_Float, _Property_4bef2cb688474ef8909e6dc3f2439616_Out_0_Float, _MFCheapContrast_c8d787e56cf842869980c82103f39754, _MFCheapContrast_c8d787e56cf842869980c82103f39754_Out_1_Float);
float _Multiply_38b6bc16bfef4cd3bec7370aabb6a836_Out_2_Float;
Unity_Multiply_float_float(_Split_925676a8ce21420aad811d255e389512_A_4_Float, _MFCheapContrast_c8d787e56cf842869980c82103f39754_Out_1_Float, _Multiply_38b6bc16bfef4cd3bec7370aabb6a836_Out_2_Float);
float3 _Blend_e613dc790cd741d3b367cb48289cffd5_Out_2_Vector3;
Unity_Blend_Overlay_float3(_Lerp_ef18ae06dc3e4fd18201e3925bc55a85_Out_3_Vector3, (_Property_dbda4c8e93f54f92a18b3390cbaf06c0_Out_0_Vector4.xyz), _Blend_e613dc790cd741d3b367cb48289cffd5_Out_2_Vector3, _Multiply_38b6bc16bfef4cd3bec7370aabb6a836_Out_2_Float);
UnityTexture2D _Property_003a6ee6678543e5964d3e811c19f789_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Coverage_Color_Map, sampler_Coverage_Color_Map, _Coverage_Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_d9f12d2b2464447a82f581c14778bbf8_Out_0_Float = _Coverage_Tiling;
float3 _Multiply_282d032113614cb58779c3a33671ad6c_Out_2_Vector3;
Unity_Multiply_float3_float3(_Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3, (_Property_d9f12d2b2464447a82f581c14778bbf8_Out_0_Float.xxx), _Multiply_282d032113614cb58779c3a33671ad6c_Out_2_Vector3);
float4 _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_003a6ee6678543e5964d3e811c19f789_Out_0_Texture2D.tex, _Property_003a6ee6678543e5964d3e811c19f789_Out_0_Texture2D.samplerstate, _Property_003a6ee6678543e5964d3e811c19f789_Out_0_Texture2D.GetTransformedUV((_Multiply_282d032113614cb58779c3a33671ad6c_Out_2_Vector3.xy)) );
if (_Property_003a6ee6678543e5964d3e811c19f789_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4, _Property_003a6ee6678543e5964d3e811c19f789_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_R_4_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4.r;
float _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_G_5_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4.g;
float _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_B_6_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4.b;
float _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_A_7_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4.a;
float _Split_42bc736ed19d41c2bc8a58c7f653b802_R_1_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4[0];
float _Split_42bc736ed19d41c2bc8a58c7f653b802_G_2_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4[1];
float _Split_42bc736ed19d41c2bc8a58c7f653b802_B_3_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4[2];
float _Split_42bc736ed19d41c2bc8a58c7f653b802_A_4_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4[3];
float3 _Vector3_dd63cc8dd9a443a99a6ecc681ff6961e_Out_0_Vector3 = float3(_Split_42bc736ed19d41c2bc8a58c7f653b802_R_1_Float, _Split_42bc736ed19d41c2bc8a58c7f653b802_G_2_Float, _Split_42bc736ed19d41c2bc8a58c7f653b802_B_3_Float);
float _Property_86cbded0425a4bf2b5441e665a7f5551_Out_0_Float = _Coverage_Hue;
float3 _Hue_5e168e8c10044fe09120668e4b870b44_Out_2_Vector3;
Unity_Hue_Normalized_float(_Vector3_dd63cc8dd9a443a99a6ecc681ff6961e_Out_0_Vector3, _Property_86cbded0425a4bf2b5441e665a7f5551_Out_0_Float, _Hue_5e168e8c10044fe09120668e4b870b44_Out_2_Vector3);
float _Property_dccedc571d6d411291da660d59f31596_Out_0_Float = _Coverage_Brightness;
float3 _Multiply_c28f28e86afd43f584c04c39cc44c8c4_Out_2_Vector3;
Unity_Multiply_float3_float3(_Hue_5e168e8c10044fe09120668e4b870b44_Out_2_Vector3, (_Property_dccedc571d6d411291da660d59f31596_Out_0_Float.xxx), _Multiply_c28f28e86afd43f584c04c39cc44c8c4_Out_2_Vector3);
float _Property_f13be210d8b049a9a1e6c0e219f5a53f_Out_0_Float = _Coverage_Normal_Mask_Slope;
float _Property_1f786f31d0924f6ba0904b90319c56e3_Out_0_Float = _Coverage_Normal_Mask_Gradience;
float _Add_4177ca63c9724385910d37f97d0e8c48_Out_2_Float;
Unity_Add_float(_Property_f13be210d8b049a9a1e6c0e219f5a53f_Out_0_Float, _Property_1f786f31d0924f6ba0904b90319c56e3_Out_0_Float, _Add_4177ca63c9724385910d37f97d0e8c48_Out_2_Float);
UnityTexture2D _Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_1_Normal_Map, sampler_Layer_1_Normal_Map, _Layer_1_Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D.tex, _Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D.samplerstate, _Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D.GetTransformedUV((_Multiply_94cb4adbcc9a4b25980974e6be235287_Out_2_Vector3.xy)) );
if (_Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4, _Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4);
float _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_R_4_Float = _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.r;
float _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_G_5_Float = _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.g;
float _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_B_6_Float = _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.b;
float _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_A_7_Float = _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.a;
float _Property_7cfa9a8e94684810a4cd4c70a2257b6c_Out_0_Float = _Layer_1_Normal_Strength;
float3 _NormalStrength_9428e53c24e74ce189625604c4b60275_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.xyz), _Property_7cfa9a8e94684810a4cd4c70a2257b6c_Out_0_Float, _NormalStrength_9428e53c24e74ce189625604c4b60275_Out_2_Vector3);
UnityTexture2D _Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_2_Normal_Map, sampler_Layer_2_Normal_Map, _Layer_2_Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D.tex, _Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D.samplerstate, _Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D.GetTransformedUV((_Multiply_7719c519f67d4b1aaaf2cca1cc2d4d79_Out_2_Vector3.xy)) );
if (_Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4, _Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4);
float _SampleTexture2D_ae5b372472874ad28eac881821ce553c_R_4_Float = _SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.r;
float _SampleTexture2D_ae5b372472874ad28eac881821ce553c_G_5_Float = _SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.g;
float _SampleTexture2D_ae5b372472874ad28eac881821ce553c_B_6_Float = _SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.b;
float _SampleTexture2D_ae5b372472874ad28eac881821ce553c_A_7_Float = _SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.a;
float _Property_12c65401b09f44daa763974826741097_Out_0_Float = _Layer_2_Normal_Strength;
float3 _NormalStrength_93ad91a06836481ca4f36c947e62a393_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.xyz), _Property_12c65401b09f44daa763974826741097_Out_0_Float, _NormalStrength_93ad91a06836481ca4f36c947e62a393_Out_2_Vector3);
float3 _Lerp_7325f1720917490cb13328e58ddd8efe_Out_3_Vector3;
Unity_Lerp_float3(_NormalStrength_9428e53c24e74ce189625604c4b60275_Out_2_Vector3, _NormalStrength_93ad91a06836481ca4f36c947e62a393_Out_2_Vector3, (_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_G_5_Float.xxx), _Lerp_7325f1720917490cb13328e58ddd8efe_Out_3_Vector3);
UnityTexture2D _Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_3_Normal_Map, sampler_Layer_3_Normal_Map, _Layer_3_Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D.tex, _Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D.samplerstate, _Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D.GetTransformedUV((_Multiply_04b9ab405b2d4c27b6dd0696d9a5eacf_Out_2_Vector3.xy)) );
if (_Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4, _Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4);
float _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_R_4_Float = _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.r;
float _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_G_5_Float = _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.g;
float _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_B_6_Float = _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.b;
float _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_A_7_Float = _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.a;
float _Property_02480e6b3805449397b2d5a15e416673_Out_0_Float = _Layer_3_Normal_Strength;
float3 _NormalStrength_8e30a22d9ed147ca9ef200087a7f43aa_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.xyz), _Property_02480e6b3805449397b2d5a15e416673_Out_0_Float, _NormalStrength_8e30a22d9ed147ca9ef200087a7f43aa_Out_2_Vector3);
float3 _Lerp_642d324ee8d74cf888dc943377359b6d_Out_3_Vector3;
Unity_Lerp_float3(_Lerp_7325f1720917490cb13328e58ddd8efe_Out_3_Vector3, _NormalStrength_8e30a22d9ed147ca9ef200087a7f43aa_Out_2_Vector3, (_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_B_6_Float.xxx), _Lerp_642d324ee8d74cf888dc943377359b6d_Out_3_Vector3);
#if defined(_USE_LAYER_3)
float3 _UseLayer3_3e504be39f154559868343947f457c24_Out_0_Vector3 = _Lerp_642d324ee8d74cf888dc943377359b6d_Out_3_Vector3;
#else
float3 _UseLayer3_3e504be39f154559868343947f457c24_Out_0_Vector3 = _Lerp_7325f1720917490cb13328e58ddd8efe_Out_3_Vector3;
#endif
#if defined(_USE_LAYER_2)
float3 _UseLayer2_c90f92a0b0134057867d0810829f779e_Out_0_Vector3 = _UseLayer3_3e504be39f154559868343947f457c24_Out_0_Vector3;
#else
float3 _UseLayer2_c90f92a0b0134057867d0810829f779e_Out_0_Vector3 = _NormalStrength_9428e53c24e74ce189625604c4b60275_Out_2_Vector3;
#endif
UnityTexture2D _Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Normal_Map, sampler_Normal_Map, _Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D.tex, _Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D.samplerstate, _Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4, _Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4);
float _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_R_4_Float = _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.r;
float _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_G_5_Float = _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.g;
float _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_B_6_Float = _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.b;
float _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_A_7_Float = _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.a;
float _Property_758b3d19a0c14ff099921db6234664b2_Out_0_Float = _Normal_Strength;
float3 _NormalStrength_3eb407934b854455a959c26453776991_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.xyz), _Property_758b3d19a0c14ff099921db6234664b2_Out_0_Float, _NormalStrength_3eb407934b854455a959c26453776991_Out_2_Vector3);
float3 _NormalBlend_cb4851ae94454d539b1004bdc1f2604f_Out_2_Vector3;
Unity_NormalBlend_float(_UseLayer2_c90f92a0b0134057867d0810829f779e_Out_0_Vector3, _NormalStrength_3eb407934b854455a959c26453776991_Out_2_Vector3, _NormalBlend_cb4851ae94454d539b1004bdc1f2604f_Out_2_Vector3);
#if defined(_USE_LAYER_1)
float3 _UseLayer1_7da71e644bf2434f99e8005bad9e8f14_Out_0_Vector3 = _NormalBlend_cb4851ae94454d539b1004bdc1f2604f_Out_2_Vector3;
#else
float3 _UseLayer1_7da71e644bf2434f99e8005bad9e8f14_Out_0_Vector3 = _NormalStrength_3eb407934b854455a959c26453776991_Out_2_Vector3;
#endif
float3 _Transform_04398ac1bc954fb19c62ce3f5f6d670c_Out_1_Vector3;
{
float3x3 tangentTransform = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
_Transform_04398ac1bc954fb19c62ce3f5f6d670c_Out_1_Vector3 = TransformTangentToWorld(_UseLayer1_7da71e644bf2434f99e8005bad9e8f14_Out_0_Vector3.xyz, tangentTransform, true);
}
float _DotProduct_ae13069c870e4539be3826642299bede_Out_2_Float;
Unity_DotProduct_float3(_Transform_04398ac1bc954fb19c62ce3f5f6d670c_Out_1_Vector3, float3(0, 1, 0), _DotProduct_ae13069c870e4539be3826642299bede_Out_2_Float);
float _Smoothstep_652b53e75f9e481e8c829d3bd3d9c24e_Out_3_Float;
Unity_Smoothstep_float(_Property_f13be210d8b049a9a1e6c0e219f5a53f_Out_0_Float, _Add_4177ca63c9724385910d37f97d0e8c48_Out_2_Float, _DotProduct_ae13069c870e4539be3826642299bede_Out_2_Float, _Smoothstep_652b53e75f9e481e8c829d3bd3d9c24e_Out_3_Float);
float _Property_64938267e5134c5b9a4256d11aa25705_Out_0_Float = _Coverage_Height_Mask_Pivot_Offset;
float _Property_cfcce6f026924b048f8c33bdc73c14f6_Out_0_Float = _Coverage_Height_Mask_Length;
Bindings_MFPivotVerticalGradience_c9e909c1845b8ae4b9a11aa715963787_float _MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c;
_MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c.WorldSpacePosition = IN.WorldSpacePosition;
float _MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c_Mask_1_Float;
SG_MFPivotVerticalGradience_c9e909c1845b8ae4b9a11aa715963787_float(_Property_64938267e5134c5b9a4256d11aa25705_Out_0_Float, _Property_cfcce6f026924b048f8c33bdc73c14f6_Out_0_Float, _MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c, _MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c_Mask_1_Float);
float _Multiply_f6a21f2d15b7472e8173002af669cfdf_Out_2_Float;
Unity_Multiply_float_float(_Smoothstep_652b53e75f9e481e8c829d3bd3d9c24e_Out_3_Float, _MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c_Mask_1_Float, _Multiply_f6a21f2d15b7472e8173002af669cfdf_Out_2_Float);
float3 _Lerp_90a44aeed76b4e67b245ea63960c28fc_Out_3_Vector3;
Unity_Lerp_float3(_Blend_e613dc790cd741d3b367cb48289cffd5_Out_2_Vector3, _Multiply_c28f28e86afd43f584c04c39cc44c8c4_Out_2_Vector3, (_Multiply_f6a21f2d15b7472e8173002af669cfdf_Out_2_Float.xxx), _Lerp_90a44aeed76b4e67b245ea63960c28fc_Out_3_Vector3);
#if defined(_USE_COVERAGE_LAYER)
float3 _UseCoverageLayer_402efe74563f417bb22b7997bdfc5c1e_Out_0_Vector3 = _Lerp_90a44aeed76b4e67b245ea63960c28fc_Out_3_Vector3;
#else
float3 _UseCoverageLayer_402efe74563f417bb22b7997bdfc5c1e_Out_0_Vector3 = _Blend_e613dc790cd741d3b367cb48289cffd5_Out_2_Vector3;
#endif
UnityTexture2D _Property_022d04960c1e41229b03a237b2bf022a_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Coverage_Normal_Map, sampler_Coverage_Normal_Map, _Coverage_Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_022d04960c1e41229b03a237b2bf022a_Out_0_Texture2D.tex, _Property_022d04960c1e41229b03a237b2bf022a_Out_0_Texture2D.samplerstate, _Property_022d04960c1e41229b03a237b2bf022a_Out_0_Texture2D.GetTransformedUV((_Multiply_282d032113614cb58779c3a33671ad6c_Out_2_Vector3.xy)) );
if (_Property_022d04960c1e41229b03a237b2bf022a_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4, _Property_022d04960c1e41229b03a237b2bf022a_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4);
float _SampleTexture2D_046783cfeeed4edebbc5805118c09db7_R_4_Float = _SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4.r;
float _SampleTexture2D_046783cfeeed4edebbc5805118c09db7_G_5_Float = _SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4.g;
float _SampleTexture2D_046783cfeeed4edebbc5805118c09db7_B_6_Float = _SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4.b;
float _SampleTexture2D_046783cfeeed4edebbc5805118c09db7_A_7_Float = _SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4.a;
float _Property_aff65d79d743476f9e681da55383046d_Out_0_Float = _Coverage_Normal_Strength;
float3 _NormalStrength_f478d3c35fc741e78611a44ac09caa44_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4.xyz), _Property_aff65d79d743476f9e681da55383046d_Out_0_Float, _NormalStrength_f478d3c35fc741e78611a44ac09caa44_Out_2_Vector3);
float3 _Lerp_0e62040e3ecb4bf3b09d8424f7147818_Out_3_Vector3;
Unity_Lerp_float3(float3(0, 0, 1), _NormalStrength_f478d3c35fc741e78611a44ac09caa44_Out_2_Vector3, (_Multiply_f6a21f2d15b7472e8173002af669cfdf_Out_2_Float.xxx), _Lerp_0e62040e3ecb4bf3b09d8424f7147818_Out_3_Vector3);
float3 _NormalBlend_4ffbea1bfb5d43979177bcfb52b4abcf_Out_2_Vector3;
Unity_NormalBlend_float(_Lerp_0e62040e3ecb4bf3b09d8424f7147818_Out_3_Vector3, _UseLayer1_7da71e644bf2434f99e8005bad9e8f14_Out_0_Vector3, _NormalBlend_4ffbea1bfb5d43979177bcfb52b4abcf_Out_2_Vector3);
#if defined(_USE_COVERAGE_LAYER)
float3 _UseCoverageLayer_b0f12be34bf24ec7ba2137cef5a254cb_Out_0_Vector3 = _NormalBlend_4ffbea1bfb5d43979177bcfb52b4abcf_Out_2_Vector3;
#else
float3 _UseCoverageLayer_b0f12be34bf24ec7ba2137cef5a254cb_Out_0_Vector3 = _UseLayer1_7da71e644bf2434f99e8005bad9e8f14_Out_0_Vector3;
#endif
float _OneMinus_7a2b995f5d6a4a619d8a66eddf498446_Out_1_Float;
Unity_OneMinus_float(_Split_03d41d27a027425fa76a6c910dfbed96_A_4_Float, _OneMinus_7a2b995f5d6a4a619d8a66eddf498446_Out_1_Float);
float _Property_3f3b47f88ff24548ba31a6777c3b2a85_Out_0_Float = _Layer_1_Smoothness_Strength;
float _Lerp_f0ccf02f1b3a423eb14231584191b608_Out_3_Float;
Unity_Lerp_float(float(0), _OneMinus_7a2b995f5d6a4a619d8a66eddf498446_Out_1_Float, _Property_3f3b47f88ff24548ba31a6777c3b2a85_Out_0_Float, _Lerp_f0ccf02f1b3a423eb14231584191b608_Out_3_Float);
float _OneMinus_16665b1602fc4acaa17e0caa00dd6ac7_Out_1_Float;
Unity_OneMinus_float(_Split_d38832e5c62c44d3a795b83acceab016_A_4_Float, _OneMinus_16665b1602fc4acaa17e0caa00dd6ac7_Out_1_Float);
float _Property_3bfd1b8cbe534165807a6d38b20b8b3f_Out_0_Float = _Layer_2_Smoothness_Strength;
float _Lerp_5f73c06703364fa1b14c25a300968493_Out_3_Float;
Unity_Lerp_float(float(0), _OneMinus_16665b1602fc4acaa17e0caa00dd6ac7_Out_1_Float, _Property_3bfd1b8cbe534165807a6d38b20b8b3f_Out_0_Float, _Lerp_5f73c06703364fa1b14c25a300968493_Out_3_Float);
float _Lerp_b5d123a1ad4c4307979f2143cfd5d3ac_Out_3_Float;
Unity_Lerp_float(_Lerp_f0ccf02f1b3a423eb14231584191b608_Out_3_Float, _Lerp_5f73c06703364fa1b14c25a300968493_Out_3_Float, _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_G_5_Float, _Lerp_b5d123a1ad4c4307979f2143cfd5d3ac_Out_3_Float);
float _OneMinus_afad5b1133a04d90b4af7d2e859ee909_Out_1_Float;
Unity_OneMinus_float(_Split_838946d3ce6f42cfbaf41fff836d6985_A_4_Float, _OneMinus_afad5b1133a04d90b4af7d2e859ee909_Out_1_Float);
float _Property_269daba898014930a8cc45eae3b32a0d_Out_0_Float = _Layer_3_Smoothness_Strength;
float _Lerp_73575fb5baba4126bfd6048130db9abc_Out_3_Float;
Unity_Lerp_float(float(0), _OneMinus_afad5b1133a04d90b4af7d2e859ee909_Out_1_Float, _Property_269daba898014930a8cc45eae3b32a0d_Out_0_Float, _Lerp_73575fb5baba4126bfd6048130db9abc_Out_3_Float);
float _Lerp_0ed87b2c815a4b6bb1c07d0ba2ad7f9e_Out_3_Float;
Unity_Lerp_float(_Lerp_b5d123a1ad4c4307979f2143cfd5d3ac_Out_3_Float, _Lerp_73575fb5baba4126bfd6048130db9abc_Out_3_Float, _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_B_6_Float, _Lerp_0ed87b2c815a4b6bb1c07d0ba2ad7f9e_Out_3_Float);
#if defined(_USE_LAYER_3)
float _UseLayer3_38584d454ac2471687bb98edbed983aa_Out_0_Float = _Lerp_0ed87b2c815a4b6bb1c07d0ba2ad7f9e_Out_3_Float;
#else
float _UseLayer3_38584d454ac2471687bb98edbed983aa_Out_0_Float = _Lerp_b5d123a1ad4c4307979f2143cfd5d3ac_Out_3_Float;
#endif
#if defined(_USE_LAYER_2)
float _UseLayer2_66f28f7fecc141028a5f15673f047917_Out_0_Float = _UseLayer3_38584d454ac2471687bb98edbed983aa_Out_0_Float;
#else
float _UseLayer2_66f28f7fecc141028a5f15673f047917_Out_0_Float = _Lerp_f0ccf02f1b3a423eb14231584191b608_Out_3_Float;
#endif
#if defined(_USE_LAYER_1)
float _UseLayer1_6040c5b1bc15438dae7a97e54a8e3324_Out_0_Float = _UseLayer2_66f28f7fecc141028a5f15673f047917_Out_0_Float;
#else
float _UseLayer1_6040c5b1bc15438dae7a97e54a8e3324_Out_0_Float = float(0);
#endif
float _OneMinus_f65c9822794443558d97581fc4537919_Out_1_Float;
Unity_OneMinus_float(_Split_42bc736ed19d41c2bc8a58c7f653b802_A_4_Float, _OneMinus_f65c9822794443558d97581fc4537919_Out_1_Float);
float _Property_eb619aeb69fd4914bebabe935f6e2c25_Out_0_Float = _Coverage_Smoothness_Strength;
float _Lerp_2c28e4b743af489e941e7226cc40512b_Out_3_Float;
Unity_Lerp_float(float(0), _OneMinus_f65c9822794443558d97581fc4537919_Out_1_Float, _Property_eb619aeb69fd4914bebabe935f6e2c25_Out_0_Float, _Lerp_2c28e4b743af489e941e7226cc40512b_Out_3_Float);
float _Lerp_1237b11b7e0241aba05ae87b4cdb9167_Out_3_Float;
Unity_Lerp_float(_UseLayer1_6040c5b1bc15438dae7a97e54a8e3324_Out_0_Float, _Lerp_2c28e4b743af489e941e7226cc40512b_Out_3_Float, _Multiply_f6a21f2d15b7472e8173002af669cfdf_Out_2_Float, _Lerp_1237b11b7e0241aba05ae87b4cdb9167_Out_3_Float);
#if defined(_USE_COVERAGE_LAYER)
float _UseCoverageLayer_3e91540d12544fcfa939ef997f0c72e0_Out_0_Float = _Lerp_1237b11b7e0241aba05ae87b4cdb9167_Out_3_Float;
#else
float _UseCoverageLayer_3e91540d12544fcfa939ef997f0c72e0_Out_0_Float = _UseLayer1_6040c5b1bc15438dae7a97e54a8e3324_Out_0_Float;
#endif
surface.BaseColor = _UseCoverageLayer_402efe74563f417bb22b7997bdfc5c1e_Out_0_Vector3;
surface.NormalTS = _UseCoverageLayer_b0f12be34bf24ec7ba2137cef5a254cb_Out_0_Vector3;
surface.Emission = float3(0, 0, 0);
surface.Metallic = float(0);
surface.Smoothness = _UseCoverageLayer_3e91540d12544fcfa939ef997f0c72e0_Out_0_Float;
surface.Occlusion = float(1);
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
    output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);

    // to preserve mikktspace compliance we use same scale renormFactor as was used on the normal.
    // This is explained in section 2.2 in "surface gradient based bump mapping framework"
    output.WorldSpaceTangent = renormFactor * input.tangentWS.xyz;
    output.WorldSpaceBiTangent = renormFactor * bitang;

    output.WorldSpacePosition = input.positionWS;

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
#pragma shader_feature_local _ _USE_LAYER_2
#pragma shader_feature_local _ _USE_LAYER_3
#pragma shader_feature_local _ _USE_COVERAGE_LAYER
#pragma shader_feature_local _ _USE_LAYER_1

#if defined(_USE_LAYER_2) && defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_3) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_2
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_3)
#define KEYWORD_PERMUTATION_3
#elif defined(_USE_LAYER_2) && defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_4
#elif defined(_USE_LAYER_2) && defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_5
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_6
#elif defined(_USE_LAYER_2)
#define KEYWORD_PERMUTATION_7
#elif defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_8
#elif defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_9
#elif defined(_USE_LAYER_3) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_10
#elif defined(_USE_LAYER_3)
#define KEYWORD_PERMUTATION_11
#elif defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_12
#elif defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_13
#elif defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_14
#else
#define KEYWORD_PERMUTATION_15
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
 float3 WorldSpaceTangent;
 float3 WorldSpaceBiTangent;
 float3 WorldSpacePosition;
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

float4 _Layer_1_Color_Map_TexelSize;
float _Layer_1_Hue;
float _Layer_1_Brightness;
float _Coverage_Height_Mask_Length;
float4 _Layer_1_Normal_Map_TexelSize;
float _Layer_1_Normal_Strength;
float _Layer_1_Tiling;
float _Layer_1_Smoothness_Strength;
float _Layer_2_Tiling;
float4 _Layer_2_Color_Map_TexelSize;
float _Layer_2_Hue;
float _Layer_2_Brightness;
float _Layer_2_Smoothness_Strength;
float4 _Layer_2_Normal_Map_TexelSize;
float _Layer_2_Normal_Strength;
float _Layer_3_Tiling;
float4 _Layer_3_Color_Map_TexelSize;
float _Layer_3_Hue;
float _Layer_3_Brightness;
float _Layer_3_Smoothness_Strength;
float4 _Layer_3_Normal_Map_TexelSize;
float _Layer_3_Normal_Strength;
float4 _RGB_Map_TexelSize;
float4 _Normal_Map_TexelSize;
float _Normal_Strength;
float _Coverage_Tiling;
float4 _Coverage_Color_Map_TexelSize;
float _Coverage_Hue;
float _Coverage_Brightness;
float _Coverage_Smoothness_Strength;
float4 _Coverage_Normal_Map_TexelSize;
float _Coverage_Normal_Strength;
float4 _Edge_Map_TexelSize;
float _Dirt_Mask_Contrast;
float _AO_Mask_Contrast;
float _Edgewear_Mask_Contrast;
float _Stylization_Mask_Contrast;
float4 _Stylization_Color;
float4 _AO_Color;
float4 _Dirt_Color;
float4 _Edgewear_Color;
float _Coverage_Normal_Mask_Slope;
float _Coverage_Normal_Mask_Gradience;
float _Coverage_Height_Mask_Pivot_Offset;
float _Scale_UVs_with_Object;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Layer_1_Color_Map);
SAMPLER(sampler_Layer_1_Color_Map);
TEXTURE2D(_Layer_1_Normal_Map);
SAMPLER(sampler_Layer_1_Normal_Map);
TEXTURE2D(_Layer_2_Color_Map);
SAMPLER(sampler_Layer_2_Color_Map);
TEXTURE2D(_Layer_2_Normal_Map);
SAMPLER(sampler_Layer_2_Normal_Map);
TEXTURE2D(_Layer_3_Color_Map);
SAMPLER(sampler_Layer_3_Color_Map);
TEXTURE2D(_Layer_3_Normal_Map);
SAMPLER(sampler_Layer_3_Normal_Map);
TEXTURE2D(_RGB_Map);
SAMPLER(sampler_RGB_Map);
TEXTURE2D(_Normal_Map);
SAMPLER(sampler_Normal_Map);
TEXTURE2D(_Coverage_Color_Map);
SAMPLER(sampler_Coverage_Color_Map);
TEXTURE2D(_Coverage_Normal_Map);
SAMPLER(sampler_Coverage_Normal_Map);
TEXTURE2D(_Edge_Map);
SAMPLER(sampler_Edge_Map);

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

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Hue_Normalized_float(float3 In, float Offset, out float3 Out)
{
    // RGB to HSV
    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 P = lerp(float4(In.bg, K.wz), float4(In.gb, K.xy), step(In.b, In.g));
    float4 Q = lerp(float4(P.xyw, In.r), float4(In.r, P.yzx), step(P.x, In.r));
    float D = Q.x - min(Q.w, Q.y);
    float E = 1e-10;
    float V = (D == 0) ? Q.x : (Q.x + E);
    float3 hsv = float3(abs(Q.z + (Q.w - Q.y)/(6.0 * D + E)), D / (Q.x + E), V);

    float hue = hsv.x + Offset;
    hsv.x = (hue < 0)
            ? hue + 1
            : (hue > 1)
                ? hue - 1
                : hue;

    // HSV to RGB
    float4 K2 = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 P2 = abs(frac(hsv.xxx + K2.xyz) * 6.0 - K2.www);
    Out = hsv.z * lerp(K2.xxx, saturate(P2 - K2.xxx), hsv.y);
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Lerp_float(float A, float B, float T, out float Out)
{
    Out = lerp(A, B, T);
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

struct Bindings_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float
{
};

void SG_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float(float _Input, float _Contrast, Bindings_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float IN, out float Out_1)
{
float _Property_f17f2d608b764773bc9f33ea28c3e14e_Out_0_Float = _Contrast;
float _Add_59838b3eb9ec437bbd93b6fd8db7a0c4_Out_2_Float;
Unity_Add_float(_Property_f17f2d608b764773bc9f33ea28c3e14e_Out_0_Float, float(-1), _Add_59838b3eb9ec437bbd93b6fd8db7a0c4_Out_2_Float);
float _Add_524e73a4aa0b4e7982442fdf0d39fe18_Out_2_Float;
Unity_Add_float(_Property_f17f2d608b764773bc9f33ea28c3e14e_Out_0_Float, float(1), _Add_524e73a4aa0b4e7982442fdf0d39fe18_Out_2_Float);
float _Property_df60db08578f4909bad9a81c68245e45_Out_0_Float = _Input;
float _Lerp_6af4dfb2e0f0426a81a1669c55ec8196_Out_3_Float;
Unity_Lerp_float(_Add_59838b3eb9ec437bbd93b6fd8db7a0c4_Out_2_Float, _Add_524e73a4aa0b4e7982442fdf0d39fe18_Out_2_Float, _Property_df60db08578f4909bad9a81c68245e45_Out_0_Float, _Lerp_6af4dfb2e0f0426a81a1669c55ec8196_Out_3_Float);
float _Saturate_59b070c1298d4279be831e68d2cd96c8_Out_1_Float;
Unity_Saturate_float(_Lerp_6af4dfb2e0f0426a81a1669c55ec8196_Out_3_Float, _Saturate_59b070c1298d4279be831e68d2cd96c8_Out_1_Float);
Out_1 = _Saturate_59b070c1298d4279be831e68d2cd96c8_Out_1_Float;
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Blend_Multiply_float3(float3 Base, float3 Blend, out float3 Out, float Opacity)
{
    Out = Base * Blend;
    Out = lerp(Base, Out, Opacity);
}

void Unity_Blend_Overlay_float3(float3 Base, float3 Blend, out float3 Out, float Opacity)
{
    float3 result1 = 1.0 - 2.0 * (1.0 - Base) * (1.0 - Blend);
    float3 result2 = 2.0 * Base * Blend;
    float3 zeroOrOne = step(Base, 0.5);
    Out = result2 * zeroOrOne + (1 - zeroOrOne) * result1;
    Out = lerp(Base, Out, Opacity);
}

void Unity_NormalStrength_float(float3 In, float Strength, out float3 Out)
{
    Out = float3(In.rg * Strength, lerp(1, In.b, saturate(Strength)));
}

void Unity_NormalBlend_float(float3 A, float3 B, out float3 Out)
{
    Out = SafeNormalize(float3(A.rg + B.rg, A.b * B.b));
}

void Unity_DotProduct_float3(float3 A, float3 B, out float Out)
{
    Out = dot(A, B);
}

void Unity_Smoothstep_float(float Edge1, float Edge2, float In, out float Out)
{
    Out = smoothstep(Edge1, Edge2, In);
}

void Unity_Subtract_float3(float3 A, float3 B, out float3 Out)
{
    Out = A - B;
}

struct Bindings_MFPivotVerticalGradience_c9e909c1845b8ae4b9a11aa715963787_float
{
float3 WorldSpacePosition;
};

void SG_MFPivotVerticalGradience_c9e909c1845b8ae4b9a11aa715963787_float(float _Offset, float _Length, Bindings_MFPivotVerticalGradience_c9e909c1845b8ae4b9a11aa715963787_float IN, out float Mask_1)
{
float _Property_4680dca2dee245e7aa77915a0b17cce9_Out_0_Float = _Offset;
float _Property_c763914b9bbb4da5abad7a3db51adcb2_Out_0_Float = _Length;
float _Add_ad1531c9af874822b29fd9a13421289e_Out_2_Float;
Unity_Add_float(_Property_4680dca2dee245e7aa77915a0b17cce9_Out_0_Float, _Property_c763914b9bbb4da5abad7a3db51adcb2_Out_0_Float, _Add_ad1531c9af874822b29fd9a13421289e_Out_2_Float);
float3 _Subtract_69e80d3fb1bb4d71b4be84a010b280ff_Out_2_Vector3;
Unity_Subtract_float3(IN.WorldSpacePosition, SHADERGRAPH_OBJECT_POSITION, _Subtract_69e80d3fb1bb4d71b4be84a010b280ff_Out_2_Vector3);
float _Split_bceff54ddef34fab8cfa9a2a0b8bbc58_R_1_Float = _Subtract_69e80d3fb1bb4d71b4be84a010b280ff_Out_2_Vector3[0];
float _Split_bceff54ddef34fab8cfa9a2a0b8bbc58_G_2_Float = _Subtract_69e80d3fb1bb4d71b4be84a010b280ff_Out_2_Vector3[1];
float _Split_bceff54ddef34fab8cfa9a2a0b8bbc58_B_3_Float = _Subtract_69e80d3fb1bb4d71b4be84a010b280ff_Out_2_Vector3[2];
float _Split_bceff54ddef34fab8cfa9a2a0b8bbc58_A_4_Float = 0;
float _Smoothstep_0109929c8f9e41888dfa7ee4fd85c377_Out_3_Float;
Unity_Smoothstep_float(_Property_4680dca2dee245e7aa77915a0b17cce9_Out_0_Float, _Add_ad1531c9af874822b29fd9a13421289e_Out_2_Float, _Split_bceff54ddef34fab8cfa9a2a0b8bbc58_G_2_Float, _Smoothstep_0109929c8f9e41888dfa7ee4fd85c377_Out_3_Float);
Mask_1 = _Smoothstep_0109929c8f9e41888dfa7ee4fd85c377_Out_3_Float;
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
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
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
UnityTexture2D _Property_82391a4213d745e08202065048659634_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_1_Color_Map, sampler_Layer_1_Color_Map, _Layer_1_Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_7afe6a7edf62460887e09c4746d796b6_Out_0_Vector4 = IN.uv0;
float3 _Multiply_15b5d23699554ab298d90333ce57d387_Out_2_Vector3;
Unity_Multiply_float3_float3((_UV_7afe6a7edf62460887e09c4746d796b6_Out_0_Vector4.xyz), float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z))), _Multiply_15b5d23699554ab298d90333ce57d387_Out_2_Vector3);
float _Property_9a5b0bf1bf584876a406e2b633741c94_Out_0_Float = _Scale_UVs_with_Object;
float3 _Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3;
Unity_Lerp_float3((_UV_7afe6a7edf62460887e09c4746d796b6_Out_0_Vector4.xyz), _Multiply_15b5d23699554ab298d90333ce57d387_Out_2_Vector3, (_Property_9a5b0bf1bf584876a406e2b633741c94_Out_0_Float.xxx), _Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3);
float _Property_460238447f6b4170aa7397df1262be96_Out_0_Float = _Layer_1_Tiling;
float3 _Multiply_94cb4adbcc9a4b25980974e6be235287_Out_2_Vector3;
Unity_Multiply_float3_float3(_Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3, (_Property_460238447f6b4170aa7397df1262be96_Out_0_Float.xxx), _Multiply_94cb4adbcc9a4b25980974e6be235287_Out_2_Vector3);
float4 _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_82391a4213d745e08202065048659634_Out_0_Texture2D.tex, _Property_82391a4213d745e08202065048659634_Out_0_Texture2D.samplerstate, _Property_82391a4213d745e08202065048659634_Out_0_Texture2D.GetTransformedUV((_Multiply_94cb4adbcc9a4b25980974e6be235287_Out_2_Vector3.xy)) );
if (_Property_82391a4213d745e08202065048659634_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4, _Property_82391a4213d745e08202065048659634_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_R_4_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4.r;
float _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_G_5_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4.g;
float _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_B_6_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4.b;
float _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_A_7_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4.a;
float _Split_03d41d27a027425fa76a6c910dfbed96_R_1_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4[0];
float _Split_03d41d27a027425fa76a6c910dfbed96_G_2_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4[1];
float _Split_03d41d27a027425fa76a6c910dfbed96_B_3_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4[2];
float _Split_03d41d27a027425fa76a6c910dfbed96_A_4_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4[3];
float3 _Vector3_29d44982f90c486b8d58d91423294375_Out_0_Vector3 = float3(_Split_03d41d27a027425fa76a6c910dfbed96_R_1_Float, _Split_03d41d27a027425fa76a6c910dfbed96_G_2_Float, _Split_03d41d27a027425fa76a6c910dfbed96_B_3_Float);
float _Property_c55f5c80dc494683850ea3803571112b_Out_0_Float = _Layer_1_Hue;
float3 _Hue_4528903dcb5b4b15be0e273df247bfd9_Out_2_Vector3;
Unity_Hue_Normalized_float(_Vector3_29d44982f90c486b8d58d91423294375_Out_0_Vector3, _Property_c55f5c80dc494683850ea3803571112b_Out_0_Float, _Hue_4528903dcb5b4b15be0e273df247bfd9_Out_2_Vector3);
float _Property_ba7720a9d8134eb58030eb416c6a024a_Out_0_Float = _Layer_1_Brightness;
float3 _Multiply_01d7b815489a446fa8f5fbd7bf096d4f_Out_2_Vector3;
Unity_Multiply_float3_float3(_Hue_4528903dcb5b4b15be0e273df247bfd9_Out_2_Vector3, (_Property_ba7720a9d8134eb58030eb416c6a024a_Out_0_Float.xxx), _Multiply_01d7b815489a446fa8f5fbd7bf096d4f_Out_2_Vector3);
UnityTexture2D _Property_f37e4daa8ff94f5eb2ca9f6f83a20bb3_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_2_Color_Map, sampler_Layer_2_Color_Map, _Layer_2_Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_812f9793e99740fcb534b8c8d8af0ff2_Out_0_Float = _Layer_2_Tiling;
float3 _Multiply_7719c519f67d4b1aaaf2cca1cc2d4d79_Out_2_Vector3;
Unity_Multiply_float3_float3(_Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3, (_Property_812f9793e99740fcb534b8c8d8af0ff2_Out_0_Float.xxx), _Multiply_7719c519f67d4b1aaaf2cca1cc2d4d79_Out_2_Vector3);
float4 _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_f37e4daa8ff94f5eb2ca9f6f83a20bb3_Out_0_Texture2D.tex, _Property_f37e4daa8ff94f5eb2ca9f6f83a20bb3_Out_0_Texture2D.samplerstate, _Property_f37e4daa8ff94f5eb2ca9f6f83a20bb3_Out_0_Texture2D.GetTransformedUV((_Multiply_7719c519f67d4b1aaaf2cca1cc2d4d79_Out_2_Vector3.xy)) );
if (_Property_f37e4daa8ff94f5eb2ca9f6f83a20bb3_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4, _Property_f37e4daa8ff94f5eb2ca9f6f83a20bb3_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_R_4_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4.r;
float _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_G_5_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4.g;
float _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_B_6_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4.b;
float _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_A_7_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4.a;
float _Split_d38832e5c62c44d3a795b83acceab016_R_1_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4[0];
float _Split_d38832e5c62c44d3a795b83acceab016_G_2_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4[1];
float _Split_d38832e5c62c44d3a795b83acceab016_B_3_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4[2];
float _Split_d38832e5c62c44d3a795b83acceab016_A_4_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4[3];
float3 _Vector3_cb2069d38949469a976ac3a5a2003bd7_Out_0_Vector3 = float3(_Split_d38832e5c62c44d3a795b83acceab016_R_1_Float, _Split_d38832e5c62c44d3a795b83acceab016_G_2_Float, _Split_d38832e5c62c44d3a795b83acceab016_B_3_Float);
float _Property_8970d5f05c7d4b5783a57e1c656ee4a9_Out_0_Float = _Layer_2_Hue;
float3 _Hue_d8383543ffa64d25a0efd5fc36520be4_Out_2_Vector3;
Unity_Hue_Normalized_float(_Vector3_cb2069d38949469a976ac3a5a2003bd7_Out_0_Vector3, _Property_8970d5f05c7d4b5783a57e1c656ee4a9_Out_0_Float, _Hue_d8383543ffa64d25a0efd5fc36520be4_Out_2_Vector3);
float _Property_40475d05242a4efaa3e83e117ecd268d_Out_0_Float = _Layer_2_Brightness;
float3 _Multiply_9a8ec1bf43a149a7a5fb1571321b8200_Out_2_Vector3;
Unity_Multiply_float3_float3(_Hue_d8383543ffa64d25a0efd5fc36520be4_Out_2_Vector3, (_Property_40475d05242a4efaa3e83e117ecd268d_Out_0_Float.xxx), _Multiply_9a8ec1bf43a149a7a5fb1571321b8200_Out_2_Vector3);
UnityTexture2D _Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_RGB_Map, sampler_RGB_Map, _RGB_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D.tex, _Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D.samplerstate, _Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4, _Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_R_4_Float = _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4.r;
float _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_G_5_Float = _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4.g;
float _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_B_6_Float = _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4.b;
float _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_A_7_Float = _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4.a;
float3 _Lerp_754b59c21e034e87848bb65af4294c76_Out_3_Vector3;
Unity_Lerp_float3(_Multiply_01d7b815489a446fa8f5fbd7bf096d4f_Out_2_Vector3, _Multiply_9a8ec1bf43a149a7a5fb1571321b8200_Out_2_Vector3, (_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_G_5_Float.xxx), _Lerp_754b59c21e034e87848bb65af4294c76_Out_3_Vector3);
UnityTexture2D _Property_fef134089dc1401db12f51526b8ed988_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_3_Color_Map, sampler_Layer_3_Color_Map, _Layer_3_Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_8d20917a655e4062a5ce52f974969006_Out_0_Float = _Layer_3_Tiling;
float3 _Multiply_04b9ab405b2d4c27b6dd0696d9a5eacf_Out_2_Vector3;
Unity_Multiply_float3_float3(_Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3, (_Property_8d20917a655e4062a5ce52f974969006_Out_0_Float.xxx), _Multiply_04b9ab405b2d4c27b6dd0696d9a5eacf_Out_2_Vector3);
float4 _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_fef134089dc1401db12f51526b8ed988_Out_0_Texture2D.tex, _Property_fef134089dc1401db12f51526b8ed988_Out_0_Texture2D.samplerstate, _Property_fef134089dc1401db12f51526b8ed988_Out_0_Texture2D.GetTransformedUV((_Multiply_04b9ab405b2d4c27b6dd0696d9a5eacf_Out_2_Vector3.xy)) );
if (_Property_fef134089dc1401db12f51526b8ed988_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4, _Property_fef134089dc1401db12f51526b8ed988_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_R_4_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4.r;
float _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_G_5_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4.g;
float _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_B_6_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4.b;
float _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_A_7_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4.a;
float _Split_838946d3ce6f42cfbaf41fff836d6985_R_1_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4[0];
float _Split_838946d3ce6f42cfbaf41fff836d6985_G_2_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4[1];
float _Split_838946d3ce6f42cfbaf41fff836d6985_B_3_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4[2];
float _Split_838946d3ce6f42cfbaf41fff836d6985_A_4_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4[3];
float3 _Vector3_2a694d435907494fb07f2f9db09d031a_Out_0_Vector3 = float3(_Split_838946d3ce6f42cfbaf41fff836d6985_R_1_Float, _Split_838946d3ce6f42cfbaf41fff836d6985_G_2_Float, _Split_838946d3ce6f42cfbaf41fff836d6985_B_3_Float);
float _Property_6ba5eab2fbda4899981bd9c4c9c6317e_Out_0_Float = _Layer_3_Hue;
float3 _Hue_0a936fcae1974948a815da96c316b7e3_Out_2_Vector3;
Unity_Hue_Normalized_float(_Vector3_2a694d435907494fb07f2f9db09d031a_Out_0_Vector3, _Property_6ba5eab2fbda4899981bd9c4c9c6317e_Out_0_Float, _Hue_0a936fcae1974948a815da96c316b7e3_Out_2_Vector3);
float _Property_c749f04d24d9423fa1a2859f800dc893_Out_0_Float = _Layer_3_Brightness;
float3 _Multiply_db73d490dc9d432e80685ef18e5427fb_Out_2_Vector3;
Unity_Multiply_float3_float3(_Hue_0a936fcae1974948a815da96c316b7e3_Out_2_Vector3, (_Property_c749f04d24d9423fa1a2859f800dc893_Out_0_Float.xxx), _Multiply_db73d490dc9d432e80685ef18e5427fb_Out_2_Vector3);
float3 _Lerp_66f917f82ed14fd89c6c2fe066b5e834_Out_3_Vector3;
Unity_Lerp_float3(_Lerp_754b59c21e034e87848bb65af4294c76_Out_3_Vector3, _Multiply_db73d490dc9d432e80685ef18e5427fb_Out_2_Vector3, (_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_B_6_Float.xxx), _Lerp_66f917f82ed14fd89c6c2fe066b5e834_Out_3_Vector3);
#if defined(_USE_LAYER_3)
float3 _UseLayer3_ee2b565dd7774fa7a5c88d0b7bfd05b8_Out_0_Vector3 = _Lerp_66f917f82ed14fd89c6c2fe066b5e834_Out_3_Vector3;
#else
float3 _UseLayer3_ee2b565dd7774fa7a5c88d0b7bfd05b8_Out_0_Vector3 = _Lerp_754b59c21e034e87848bb65af4294c76_Out_3_Vector3;
#endif
#if defined(_USE_LAYER_2)
float3 _UseLayer2_22b88f361b80488fbbec2df7f45c4924_Out_0_Vector3 = _UseLayer3_ee2b565dd7774fa7a5c88d0b7bfd05b8_Out_0_Vector3;
#else
float3 _UseLayer2_22b88f361b80488fbbec2df7f45c4924_Out_0_Vector3 = _Multiply_01d7b815489a446fa8f5fbd7bf096d4f_Out_2_Vector3;
#endif
float4 _Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4 = _Stylization_Color;
float _Split_42e867e97f454925a658dba60e21864f_R_1_Float = _Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4[0];
float _Split_42e867e97f454925a658dba60e21864f_G_2_Float = _Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4[1];
float _Split_42e867e97f454925a658dba60e21864f_B_3_Float = _Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4[2];
float _Split_42e867e97f454925a658dba60e21864f_A_4_Float = _Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4[3];
UnityTexture2D _Property_c02e202098944eebbd44663588c64e3b_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Edge_Map, sampler_Edge_Map, _Edge_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_c02e202098944eebbd44663588c64e3b_Out_0_Texture2D.tex, _Property_c02e202098944eebbd44663588c64e3b_Out_0_Texture2D.samplerstate, _Property_c02e202098944eebbd44663588c64e3b_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_c02e202098944eebbd44663588c64e3b_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4, _Property_c02e202098944eebbd44663588c64e3b_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_R_4_Float = _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4.r;
float _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_G_5_Float = _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4.g;
float _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_B_6_Float = _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4.b;
float _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_A_7_Float = _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4.a;
float _Property_a4422d37671342bdab8d700fe4c2424a_Out_0_Float = _Stylization_Mask_Contrast;
Bindings_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float _MFCheapContrast_2e3888093b39403781e2ccbee9980699;
float _MFCheapContrast_2e3888093b39403781e2ccbee9980699_Out_1_Float;
SG_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float(_SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_A_7_Float, _Property_a4422d37671342bdab8d700fe4c2424a_Out_0_Float, _MFCheapContrast_2e3888093b39403781e2ccbee9980699, _MFCheapContrast_2e3888093b39403781e2ccbee9980699_Out_1_Float);
float _Multiply_364f0f8b2ff74453b42f050a8b1cb526_Out_2_Float;
Unity_Multiply_float_float(_Split_42e867e97f454925a658dba60e21864f_A_4_Float, _MFCheapContrast_2e3888093b39403781e2ccbee9980699_Out_1_Float, _Multiply_364f0f8b2ff74453b42f050a8b1cb526_Out_2_Float);
float3 _Lerp_0a1e51abb320477ab63b42e1a32052cc_Out_3_Vector3;
Unity_Lerp_float3(_UseLayer2_22b88f361b80488fbbec2df7f45c4924_Out_0_Vector3, (_Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4.xyz), (_Multiply_364f0f8b2ff74453b42f050a8b1cb526_Out_2_Float.xxx), _Lerp_0a1e51abb320477ab63b42e1a32052cc_Out_3_Vector3);
#if defined(_USE_LAYER_1)
float3 _UseLayer1_f5f8cf003ae64c4187983dc616ecbd3b_Out_0_Vector3 = _Lerp_0a1e51abb320477ab63b42e1a32052cc_Out_3_Vector3;
#else
float3 _UseLayer1_f5f8cf003ae64c4187983dc616ecbd3b_Out_0_Vector3 = (_Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4.xyz);
#endif
float4 _Property_0be88f8e5ba94765a3623024be14bbbf_Out_0_Vector4 = _AO_Color;
float _Split_07ec24369bd1411c82179cabd24aabe5_R_1_Float = _Property_0be88f8e5ba94765a3623024be14bbbf_Out_0_Vector4[0];
float _Split_07ec24369bd1411c82179cabd24aabe5_G_2_Float = _Property_0be88f8e5ba94765a3623024be14bbbf_Out_0_Vector4[1];
float _Split_07ec24369bd1411c82179cabd24aabe5_B_3_Float = _Property_0be88f8e5ba94765a3623024be14bbbf_Out_0_Vector4[2];
float _Split_07ec24369bd1411c82179cabd24aabe5_A_4_Float = _Property_0be88f8e5ba94765a3623024be14bbbf_Out_0_Vector4[3];
float _Property_781496b67f424345bca22ad901512267_Out_0_Float = _AO_Mask_Contrast;
Bindings_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float _MFCheapContrast_74522f75ddab433f9ec9f709fe5a6faf;
float _MFCheapContrast_74522f75ddab433f9ec9f709fe5a6faf_Out_1_Float;
SG_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float(_SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_G_5_Float, _Property_781496b67f424345bca22ad901512267_Out_0_Float, _MFCheapContrast_74522f75ddab433f9ec9f709fe5a6faf, _MFCheapContrast_74522f75ddab433f9ec9f709fe5a6faf_Out_1_Float);
float _Multiply_27eebb10636649c3aefb6ad13580fd9e_Out_2_Float;
Unity_Multiply_float_float(_Split_07ec24369bd1411c82179cabd24aabe5_A_4_Float, _MFCheapContrast_74522f75ddab433f9ec9f709fe5a6faf_Out_1_Float, _Multiply_27eebb10636649c3aefb6ad13580fd9e_Out_2_Float);
float3 _Blend_5baeb7dcebe145dcb53bba406ea82ebd_Out_2_Vector3;
Unity_Blend_Multiply_float3(_UseLayer1_f5f8cf003ae64c4187983dc616ecbd3b_Out_0_Vector3, (_Property_0be88f8e5ba94765a3623024be14bbbf_Out_0_Vector4.xyz), _Blend_5baeb7dcebe145dcb53bba406ea82ebd_Out_2_Vector3, _Multiply_27eebb10636649c3aefb6ad13580fd9e_Out_2_Float);
float4 _Property_64075cb2946e4bc89c0928961fa8e759_Out_0_Vector4 = _Dirt_Color;
float _Split_102b0005351f49a0a7ec214d4c4e879e_R_1_Float = _Property_64075cb2946e4bc89c0928961fa8e759_Out_0_Vector4[0];
float _Split_102b0005351f49a0a7ec214d4c4e879e_G_2_Float = _Property_64075cb2946e4bc89c0928961fa8e759_Out_0_Vector4[1];
float _Split_102b0005351f49a0a7ec214d4c4e879e_B_3_Float = _Property_64075cb2946e4bc89c0928961fa8e759_Out_0_Vector4[2];
float _Split_102b0005351f49a0a7ec214d4c4e879e_A_4_Float = _Property_64075cb2946e4bc89c0928961fa8e759_Out_0_Vector4[3];
float _Property_089f6755c08b483d971238d917dd8b3a_Out_0_Float = _Dirt_Mask_Contrast;
Bindings_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float _MFCheapContrast_96f7295ada8247d48955ac898a7aba11;
float _MFCheapContrast_96f7295ada8247d48955ac898a7aba11_Out_1_Float;
SG_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float(_SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_R_4_Float, _Property_089f6755c08b483d971238d917dd8b3a_Out_0_Float, _MFCheapContrast_96f7295ada8247d48955ac898a7aba11, _MFCheapContrast_96f7295ada8247d48955ac898a7aba11_Out_1_Float);
float _Multiply_bc33d3e78b104e93a60b60cea85cebac_Out_2_Float;
Unity_Multiply_float_float(_Split_102b0005351f49a0a7ec214d4c4e879e_A_4_Float, _MFCheapContrast_96f7295ada8247d48955ac898a7aba11_Out_1_Float, _Multiply_bc33d3e78b104e93a60b60cea85cebac_Out_2_Float);
float3 _Lerp_ef18ae06dc3e4fd18201e3925bc55a85_Out_3_Vector3;
Unity_Lerp_float3(_Blend_5baeb7dcebe145dcb53bba406ea82ebd_Out_2_Vector3, (_Property_64075cb2946e4bc89c0928961fa8e759_Out_0_Vector4.xyz), (_Multiply_bc33d3e78b104e93a60b60cea85cebac_Out_2_Float.xxx), _Lerp_ef18ae06dc3e4fd18201e3925bc55a85_Out_3_Vector3);
float4 _Property_dbda4c8e93f54f92a18b3390cbaf06c0_Out_0_Vector4 = _Edgewear_Color;
float _Split_925676a8ce21420aad811d255e389512_R_1_Float = _Property_dbda4c8e93f54f92a18b3390cbaf06c0_Out_0_Vector4[0];
float _Split_925676a8ce21420aad811d255e389512_G_2_Float = _Property_dbda4c8e93f54f92a18b3390cbaf06c0_Out_0_Vector4[1];
float _Split_925676a8ce21420aad811d255e389512_B_3_Float = _Property_dbda4c8e93f54f92a18b3390cbaf06c0_Out_0_Vector4[2];
float _Split_925676a8ce21420aad811d255e389512_A_4_Float = _Property_dbda4c8e93f54f92a18b3390cbaf06c0_Out_0_Vector4[3];
float _Property_4bef2cb688474ef8909e6dc3f2439616_Out_0_Float = _Edgewear_Mask_Contrast;
Bindings_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float _MFCheapContrast_c8d787e56cf842869980c82103f39754;
float _MFCheapContrast_c8d787e56cf842869980c82103f39754_Out_1_Float;
SG_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float(_SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_B_6_Float, _Property_4bef2cb688474ef8909e6dc3f2439616_Out_0_Float, _MFCheapContrast_c8d787e56cf842869980c82103f39754, _MFCheapContrast_c8d787e56cf842869980c82103f39754_Out_1_Float);
float _Multiply_38b6bc16bfef4cd3bec7370aabb6a836_Out_2_Float;
Unity_Multiply_float_float(_Split_925676a8ce21420aad811d255e389512_A_4_Float, _MFCheapContrast_c8d787e56cf842869980c82103f39754_Out_1_Float, _Multiply_38b6bc16bfef4cd3bec7370aabb6a836_Out_2_Float);
float3 _Blend_e613dc790cd741d3b367cb48289cffd5_Out_2_Vector3;
Unity_Blend_Overlay_float3(_Lerp_ef18ae06dc3e4fd18201e3925bc55a85_Out_3_Vector3, (_Property_dbda4c8e93f54f92a18b3390cbaf06c0_Out_0_Vector4.xyz), _Blend_e613dc790cd741d3b367cb48289cffd5_Out_2_Vector3, _Multiply_38b6bc16bfef4cd3bec7370aabb6a836_Out_2_Float);
UnityTexture2D _Property_003a6ee6678543e5964d3e811c19f789_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Coverage_Color_Map, sampler_Coverage_Color_Map, _Coverage_Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_d9f12d2b2464447a82f581c14778bbf8_Out_0_Float = _Coverage_Tiling;
float3 _Multiply_282d032113614cb58779c3a33671ad6c_Out_2_Vector3;
Unity_Multiply_float3_float3(_Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3, (_Property_d9f12d2b2464447a82f581c14778bbf8_Out_0_Float.xxx), _Multiply_282d032113614cb58779c3a33671ad6c_Out_2_Vector3);
float4 _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_003a6ee6678543e5964d3e811c19f789_Out_0_Texture2D.tex, _Property_003a6ee6678543e5964d3e811c19f789_Out_0_Texture2D.samplerstate, _Property_003a6ee6678543e5964d3e811c19f789_Out_0_Texture2D.GetTransformedUV((_Multiply_282d032113614cb58779c3a33671ad6c_Out_2_Vector3.xy)) );
if (_Property_003a6ee6678543e5964d3e811c19f789_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4, _Property_003a6ee6678543e5964d3e811c19f789_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_R_4_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4.r;
float _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_G_5_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4.g;
float _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_B_6_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4.b;
float _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_A_7_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4.a;
float _Split_42bc736ed19d41c2bc8a58c7f653b802_R_1_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4[0];
float _Split_42bc736ed19d41c2bc8a58c7f653b802_G_2_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4[1];
float _Split_42bc736ed19d41c2bc8a58c7f653b802_B_3_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4[2];
float _Split_42bc736ed19d41c2bc8a58c7f653b802_A_4_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4[3];
float3 _Vector3_dd63cc8dd9a443a99a6ecc681ff6961e_Out_0_Vector3 = float3(_Split_42bc736ed19d41c2bc8a58c7f653b802_R_1_Float, _Split_42bc736ed19d41c2bc8a58c7f653b802_G_2_Float, _Split_42bc736ed19d41c2bc8a58c7f653b802_B_3_Float);
float _Property_86cbded0425a4bf2b5441e665a7f5551_Out_0_Float = _Coverage_Hue;
float3 _Hue_5e168e8c10044fe09120668e4b870b44_Out_2_Vector3;
Unity_Hue_Normalized_float(_Vector3_dd63cc8dd9a443a99a6ecc681ff6961e_Out_0_Vector3, _Property_86cbded0425a4bf2b5441e665a7f5551_Out_0_Float, _Hue_5e168e8c10044fe09120668e4b870b44_Out_2_Vector3);
float _Property_dccedc571d6d411291da660d59f31596_Out_0_Float = _Coverage_Brightness;
float3 _Multiply_c28f28e86afd43f584c04c39cc44c8c4_Out_2_Vector3;
Unity_Multiply_float3_float3(_Hue_5e168e8c10044fe09120668e4b870b44_Out_2_Vector3, (_Property_dccedc571d6d411291da660d59f31596_Out_0_Float.xxx), _Multiply_c28f28e86afd43f584c04c39cc44c8c4_Out_2_Vector3);
float _Property_f13be210d8b049a9a1e6c0e219f5a53f_Out_0_Float = _Coverage_Normal_Mask_Slope;
float _Property_1f786f31d0924f6ba0904b90319c56e3_Out_0_Float = _Coverage_Normal_Mask_Gradience;
float _Add_4177ca63c9724385910d37f97d0e8c48_Out_2_Float;
Unity_Add_float(_Property_f13be210d8b049a9a1e6c0e219f5a53f_Out_0_Float, _Property_1f786f31d0924f6ba0904b90319c56e3_Out_0_Float, _Add_4177ca63c9724385910d37f97d0e8c48_Out_2_Float);
UnityTexture2D _Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_1_Normal_Map, sampler_Layer_1_Normal_Map, _Layer_1_Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D.tex, _Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D.samplerstate, _Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D.GetTransformedUV((_Multiply_94cb4adbcc9a4b25980974e6be235287_Out_2_Vector3.xy)) );
if (_Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4, _Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4);
float _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_R_4_Float = _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.r;
float _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_G_5_Float = _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.g;
float _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_B_6_Float = _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.b;
float _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_A_7_Float = _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.a;
float _Property_7cfa9a8e94684810a4cd4c70a2257b6c_Out_0_Float = _Layer_1_Normal_Strength;
float3 _NormalStrength_9428e53c24e74ce189625604c4b60275_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.xyz), _Property_7cfa9a8e94684810a4cd4c70a2257b6c_Out_0_Float, _NormalStrength_9428e53c24e74ce189625604c4b60275_Out_2_Vector3);
UnityTexture2D _Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_2_Normal_Map, sampler_Layer_2_Normal_Map, _Layer_2_Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D.tex, _Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D.samplerstate, _Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D.GetTransformedUV((_Multiply_7719c519f67d4b1aaaf2cca1cc2d4d79_Out_2_Vector3.xy)) );
if (_Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4, _Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4);
float _SampleTexture2D_ae5b372472874ad28eac881821ce553c_R_4_Float = _SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.r;
float _SampleTexture2D_ae5b372472874ad28eac881821ce553c_G_5_Float = _SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.g;
float _SampleTexture2D_ae5b372472874ad28eac881821ce553c_B_6_Float = _SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.b;
float _SampleTexture2D_ae5b372472874ad28eac881821ce553c_A_7_Float = _SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.a;
float _Property_12c65401b09f44daa763974826741097_Out_0_Float = _Layer_2_Normal_Strength;
float3 _NormalStrength_93ad91a06836481ca4f36c947e62a393_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.xyz), _Property_12c65401b09f44daa763974826741097_Out_0_Float, _NormalStrength_93ad91a06836481ca4f36c947e62a393_Out_2_Vector3);
float3 _Lerp_7325f1720917490cb13328e58ddd8efe_Out_3_Vector3;
Unity_Lerp_float3(_NormalStrength_9428e53c24e74ce189625604c4b60275_Out_2_Vector3, _NormalStrength_93ad91a06836481ca4f36c947e62a393_Out_2_Vector3, (_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_G_5_Float.xxx), _Lerp_7325f1720917490cb13328e58ddd8efe_Out_3_Vector3);
UnityTexture2D _Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_3_Normal_Map, sampler_Layer_3_Normal_Map, _Layer_3_Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D.tex, _Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D.samplerstate, _Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D.GetTransformedUV((_Multiply_04b9ab405b2d4c27b6dd0696d9a5eacf_Out_2_Vector3.xy)) );
if (_Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4, _Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4);
float _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_R_4_Float = _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.r;
float _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_G_5_Float = _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.g;
float _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_B_6_Float = _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.b;
float _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_A_7_Float = _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.a;
float _Property_02480e6b3805449397b2d5a15e416673_Out_0_Float = _Layer_3_Normal_Strength;
float3 _NormalStrength_8e30a22d9ed147ca9ef200087a7f43aa_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.xyz), _Property_02480e6b3805449397b2d5a15e416673_Out_0_Float, _NormalStrength_8e30a22d9ed147ca9ef200087a7f43aa_Out_2_Vector3);
float3 _Lerp_642d324ee8d74cf888dc943377359b6d_Out_3_Vector3;
Unity_Lerp_float3(_Lerp_7325f1720917490cb13328e58ddd8efe_Out_3_Vector3, _NormalStrength_8e30a22d9ed147ca9ef200087a7f43aa_Out_2_Vector3, (_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_B_6_Float.xxx), _Lerp_642d324ee8d74cf888dc943377359b6d_Out_3_Vector3);
#if defined(_USE_LAYER_3)
float3 _UseLayer3_3e504be39f154559868343947f457c24_Out_0_Vector3 = _Lerp_642d324ee8d74cf888dc943377359b6d_Out_3_Vector3;
#else
float3 _UseLayer3_3e504be39f154559868343947f457c24_Out_0_Vector3 = _Lerp_7325f1720917490cb13328e58ddd8efe_Out_3_Vector3;
#endif
#if defined(_USE_LAYER_2)
float3 _UseLayer2_c90f92a0b0134057867d0810829f779e_Out_0_Vector3 = _UseLayer3_3e504be39f154559868343947f457c24_Out_0_Vector3;
#else
float3 _UseLayer2_c90f92a0b0134057867d0810829f779e_Out_0_Vector3 = _NormalStrength_9428e53c24e74ce189625604c4b60275_Out_2_Vector3;
#endif
UnityTexture2D _Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Normal_Map, sampler_Normal_Map, _Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D.tex, _Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D.samplerstate, _Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4, _Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4);
float _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_R_4_Float = _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.r;
float _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_G_5_Float = _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.g;
float _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_B_6_Float = _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.b;
float _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_A_7_Float = _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.a;
float _Property_758b3d19a0c14ff099921db6234664b2_Out_0_Float = _Normal_Strength;
float3 _NormalStrength_3eb407934b854455a959c26453776991_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.xyz), _Property_758b3d19a0c14ff099921db6234664b2_Out_0_Float, _NormalStrength_3eb407934b854455a959c26453776991_Out_2_Vector3);
float3 _NormalBlend_cb4851ae94454d539b1004bdc1f2604f_Out_2_Vector3;
Unity_NormalBlend_float(_UseLayer2_c90f92a0b0134057867d0810829f779e_Out_0_Vector3, _NormalStrength_3eb407934b854455a959c26453776991_Out_2_Vector3, _NormalBlend_cb4851ae94454d539b1004bdc1f2604f_Out_2_Vector3);
#if defined(_USE_LAYER_1)
float3 _UseLayer1_7da71e644bf2434f99e8005bad9e8f14_Out_0_Vector3 = _NormalBlend_cb4851ae94454d539b1004bdc1f2604f_Out_2_Vector3;
#else
float3 _UseLayer1_7da71e644bf2434f99e8005bad9e8f14_Out_0_Vector3 = _NormalStrength_3eb407934b854455a959c26453776991_Out_2_Vector3;
#endif
float3 _Transform_04398ac1bc954fb19c62ce3f5f6d670c_Out_1_Vector3;
{
float3x3 tangentTransform = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
_Transform_04398ac1bc954fb19c62ce3f5f6d670c_Out_1_Vector3 = TransformTangentToWorld(_UseLayer1_7da71e644bf2434f99e8005bad9e8f14_Out_0_Vector3.xyz, tangentTransform, true);
}
float _DotProduct_ae13069c870e4539be3826642299bede_Out_2_Float;
Unity_DotProduct_float3(_Transform_04398ac1bc954fb19c62ce3f5f6d670c_Out_1_Vector3, float3(0, 1, 0), _DotProduct_ae13069c870e4539be3826642299bede_Out_2_Float);
float _Smoothstep_652b53e75f9e481e8c829d3bd3d9c24e_Out_3_Float;
Unity_Smoothstep_float(_Property_f13be210d8b049a9a1e6c0e219f5a53f_Out_0_Float, _Add_4177ca63c9724385910d37f97d0e8c48_Out_2_Float, _DotProduct_ae13069c870e4539be3826642299bede_Out_2_Float, _Smoothstep_652b53e75f9e481e8c829d3bd3d9c24e_Out_3_Float);
float _Property_64938267e5134c5b9a4256d11aa25705_Out_0_Float = _Coverage_Height_Mask_Pivot_Offset;
float _Property_cfcce6f026924b048f8c33bdc73c14f6_Out_0_Float = _Coverage_Height_Mask_Length;
Bindings_MFPivotVerticalGradience_c9e909c1845b8ae4b9a11aa715963787_float _MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c;
_MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c.WorldSpacePosition = IN.WorldSpacePosition;
float _MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c_Mask_1_Float;
SG_MFPivotVerticalGradience_c9e909c1845b8ae4b9a11aa715963787_float(_Property_64938267e5134c5b9a4256d11aa25705_Out_0_Float, _Property_cfcce6f026924b048f8c33bdc73c14f6_Out_0_Float, _MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c, _MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c_Mask_1_Float);
float _Multiply_f6a21f2d15b7472e8173002af669cfdf_Out_2_Float;
Unity_Multiply_float_float(_Smoothstep_652b53e75f9e481e8c829d3bd3d9c24e_Out_3_Float, _MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c_Mask_1_Float, _Multiply_f6a21f2d15b7472e8173002af669cfdf_Out_2_Float);
float3 _Lerp_90a44aeed76b4e67b245ea63960c28fc_Out_3_Vector3;
Unity_Lerp_float3(_Blend_e613dc790cd741d3b367cb48289cffd5_Out_2_Vector3, _Multiply_c28f28e86afd43f584c04c39cc44c8c4_Out_2_Vector3, (_Multiply_f6a21f2d15b7472e8173002af669cfdf_Out_2_Float.xxx), _Lerp_90a44aeed76b4e67b245ea63960c28fc_Out_3_Vector3);
#if defined(_USE_COVERAGE_LAYER)
float3 _UseCoverageLayer_402efe74563f417bb22b7997bdfc5c1e_Out_0_Vector3 = _Lerp_90a44aeed76b4e67b245ea63960c28fc_Out_3_Vector3;
#else
float3 _UseCoverageLayer_402efe74563f417bb22b7997bdfc5c1e_Out_0_Vector3 = _Blend_e613dc790cd741d3b367cb48289cffd5_Out_2_Vector3;
#endif
UnityTexture2D _Property_022d04960c1e41229b03a237b2bf022a_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Coverage_Normal_Map, sampler_Coverage_Normal_Map, _Coverage_Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_022d04960c1e41229b03a237b2bf022a_Out_0_Texture2D.tex, _Property_022d04960c1e41229b03a237b2bf022a_Out_0_Texture2D.samplerstate, _Property_022d04960c1e41229b03a237b2bf022a_Out_0_Texture2D.GetTransformedUV((_Multiply_282d032113614cb58779c3a33671ad6c_Out_2_Vector3.xy)) );
if (_Property_022d04960c1e41229b03a237b2bf022a_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4, _Property_022d04960c1e41229b03a237b2bf022a_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4);
float _SampleTexture2D_046783cfeeed4edebbc5805118c09db7_R_4_Float = _SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4.r;
float _SampleTexture2D_046783cfeeed4edebbc5805118c09db7_G_5_Float = _SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4.g;
float _SampleTexture2D_046783cfeeed4edebbc5805118c09db7_B_6_Float = _SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4.b;
float _SampleTexture2D_046783cfeeed4edebbc5805118c09db7_A_7_Float = _SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4.a;
float _Property_aff65d79d743476f9e681da55383046d_Out_0_Float = _Coverage_Normal_Strength;
float3 _NormalStrength_f478d3c35fc741e78611a44ac09caa44_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_046783cfeeed4edebbc5805118c09db7_RGBA_0_Vector4.xyz), _Property_aff65d79d743476f9e681da55383046d_Out_0_Float, _NormalStrength_f478d3c35fc741e78611a44ac09caa44_Out_2_Vector3);
float3 _Lerp_0e62040e3ecb4bf3b09d8424f7147818_Out_3_Vector3;
Unity_Lerp_float3(float3(0, 0, 1), _NormalStrength_f478d3c35fc741e78611a44ac09caa44_Out_2_Vector3, (_Multiply_f6a21f2d15b7472e8173002af669cfdf_Out_2_Float.xxx), _Lerp_0e62040e3ecb4bf3b09d8424f7147818_Out_3_Vector3);
float3 _NormalBlend_4ffbea1bfb5d43979177bcfb52b4abcf_Out_2_Vector3;
Unity_NormalBlend_float(_Lerp_0e62040e3ecb4bf3b09d8424f7147818_Out_3_Vector3, _UseLayer1_7da71e644bf2434f99e8005bad9e8f14_Out_0_Vector3, _NormalBlend_4ffbea1bfb5d43979177bcfb52b4abcf_Out_2_Vector3);
#if defined(_USE_COVERAGE_LAYER)
float3 _UseCoverageLayer_b0f12be34bf24ec7ba2137cef5a254cb_Out_0_Vector3 = _NormalBlend_4ffbea1bfb5d43979177bcfb52b4abcf_Out_2_Vector3;
#else
float3 _UseCoverageLayer_b0f12be34bf24ec7ba2137cef5a254cb_Out_0_Vector3 = _UseLayer1_7da71e644bf2434f99e8005bad9e8f14_Out_0_Vector3;
#endif
float _OneMinus_7a2b995f5d6a4a619d8a66eddf498446_Out_1_Float;
Unity_OneMinus_float(_Split_03d41d27a027425fa76a6c910dfbed96_A_4_Float, _OneMinus_7a2b995f5d6a4a619d8a66eddf498446_Out_1_Float);
float _Property_3f3b47f88ff24548ba31a6777c3b2a85_Out_0_Float = _Layer_1_Smoothness_Strength;
float _Lerp_f0ccf02f1b3a423eb14231584191b608_Out_3_Float;
Unity_Lerp_float(float(0), _OneMinus_7a2b995f5d6a4a619d8a66eddf498446_Out_1_Float, _Property_3f3b47f88ff24548ba31a6777c3b2a85_Out_0_Float, _Lerp_f0ccf02f1b3a423eb14231584191b608_Out_3_Float);
float _OneMinus_16665b1602fc4acaa17e0caa00dd6ac7_Out_1_Float;
Unity_OneMinus_float(_Split_d38832e5c62c44d3a795b83acceab016_A_4_Float, _OneMinus_16665b1602fc4acaa17e0caa00dd6ac7_Out_1_Float);
float _Property_3bfd1b8cbe534165807a6d38b20b8b3f_Out_0_Float = _Layer_2_Smoothness_Strength;
float _Lerp_5f73c06703364fa1b14c25a300968493_Out_3_Float;
Unity_Lerp_float(float(0), _OneMinus_16665b1602fc4acaa17e0caa00dd6ac7_Out_1_Float, _Property_3bfd1b8cbe534165807a6d38b20b8b3f_Out_0_Float, _Lerp_5f73c06703364fa1b14c25a300968493_Out_3_Float);
float _Lerp_b5d123a1ad4c4307979f2143cfd5d3ac_Out_3_Float;
Unity_Lerp_float(_Lerp_f0ccf02f1b3a423eb14231584191b608_Out_3_Float, _Lerp_5f73c06703364fa1b14c25a300968493_Out_3_Float, _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_G_5_Float, _Lerp_b5d123a1ad4c4307979f2143cfd5d3ac_Out_3_Float);
float _OneMinus_afad5b1133a04d90b4af7d2e859ee909_Out_1_Float;
Unity_OneMinus_float(_Split_838946d3ce6f42cfbaf41fff836d6985_A_4_Float, _OneMinus_afad5b1133a04d90b4af7d2e859ee909_Out_1_Float);
float _Property_269daba898014930a8cc45eae3b32a0d_Out_0_Float = _Layer_3_Smoothness_Strength;
float _Lerp_73575fb5baba4126bfd6048130db9abc_Out_3_Float;
Unity_Lerp_float(float(0), _OneMinus_afad5b1133a04d90b4af7d2e859ee909_Out_1_Float, _Property_269daba898014930a8cc45eae3b32a0d_Out_0_Float, _Lerp_73575fb5baba4126bfd6048130db9abc_Out_3_Float);
float _Lerp_0ed87b2c815a4b6bb1c07d0ba2ad7f9e_Out_3_Float;
Unity_Lerp_float(_Lerp_b5d123a1ad4c4307979f2143cfd5d3ac_Out_3_Float, _Lerp_73575fb5baba4126bfd6048130db9abc_Out_3_Float, _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_B_6_Float, _Lerp_0ed87b2c815a4b6bb1c07d0ba2ad7f9e_Out_3_Float);
#if defined(_USE_LAYER_3)
float _UseLayer3_38584d454ac2471687bb98edbed983aa_Out_0_Float = _Lerp_0ed87b2c815a4b6bb1c07d0ba2ad7f9e_Out_3_Float;
#else
float _UseLayer3_38584d454ac2471687bb98edbed983aa_Out_0_Float = _Lerp_b5d123a1ad4c4307979f2143cfd5d3ac_Out_3_Float;
#endif
#if defined(_USE_LAYER_2)
float _UseLayer2_66f28f7fecc141028a5f15673f047917_Out_0_Float = _UseLayer3_38584d454ac2471687bb98edbed983aa_Out_0_Float;
#else
float _UseLayer2_66f28f7fecc141028a5f15673f047917_Out_0_Float = _Lerp_f0ccf02f1b3a423eb14231584191b608_Out_3_Float;
#endif
#if defined(_USE_LAYER_1)
float _UseLayer1_6040c5b1bc15438dae7a97e54a8e3324_Out_0_Float = _UseLayer2_66f28f7fecc141028a5f15673f047917_Out_0_Float;
#else
float _UseLayer1_6040c5b1bc15438dae7a97e54a8e3324_Out_0_Float = float(0);
#endif
float _OneMinus_f65c9822794443558d97581fc4537919_Out_1_Float;
Unity_OneMinus_float(_Split_42bc736ed19d41c2bc8a58c7f653b802_A_4_Float, _OneMinus_f65c9822794443558d97581fc4537919_Out_1_Float);
float _Property_eb619aeb69fd4914bebabe935f6e2c25_Out_0_Float = _Coverage_Smoothness_Strength;
float _Lerp_2c28e4b743af489e941e7226cc40512b_Out_3_Float;
Unity_Lerp_float(float(0), _OneMinus_f65c9822794443558d97581fc4537919_Out_1_Float, _Property_eb619aeb69fd4914bebabe935f6e2c25_Out_0_Float, _Lerp_2c28e4b743af489e941e7226cc40512b_Out_3_Float);
float _Lerp_1237b11b7e0241aba05ae87b4cdb9167_Out_3_Float;
Unity_Lerp_float(_UseLayer1_6040c5b1bc15438dae7a97e54a8e3324_Out_0_Float, _Lerp_2c28e4b743af489e941e7226cc40512b_Out_3_Float, _Multiply_f6a21f2d15b7472e8173002af669cfdf_Out_2_Float, _Lerp_1237b11b7e0241aba05ae87b4cdb9167_Out_3_Float);
#if defined(_USE_COVERAGE_LAYER)
float _UseCoverageLayer_3e91540d12544fcfa939ef997f0c72e0_Out_0_Float = _Lerp_1237b11b7e0241aba05ae87b4cdb9167_Out_3_Float;
#else
float _UseCoverageLayer_3e91540d12544fcfa939ef997f0c72e0_Out_0_Float = _UseLayer1_6040c5b1bc15438dae7a97e54a8e3324_Out_0_Float;
#endif
surface.BaseColor = _UseCoverageLayer_402efe74563f417bb22b7997bdfc5c1e_Out_0_Vector3;
surface.NormalTS = _UseCoverageLayer_b0f12be34bf24ec7ba2137cef5a254cb_Out_0_Vector3;
surface.Emission = float3(0, 0, 0);
surface.Metallic = float(0);
surface.Smoothness = _UseCoverageLayer_3e91540d12544fcfa939ef997f0c72e0_Out_0_Float;
surface.Occlusion = float(1);
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
    output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);

    // to preserve mikktspace compliance we use same scale renormFactor as was used on the normal.
    // This is explained in section 2.2 in "surface gradient based bump mapping framework"
    output.WorldSpaceTangent = renormFactor * input.tangentWS.xyz;
    output.WorldSpaceBiTangent = renormFactor * bitang;

    output.WorldSpacePosition = input.positionWS;

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
#pragma shader_feature_local _ _USE_LAYER_2
#pragma shader_feature_local _ _USE_LAYER_3
#pragma shader_feature_local _ _USE_COVERAGE_LAYER
#pragma shader_feature_local _ _USE_LAYER_1

#if defined(_USE_LAYER_2) && defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_3) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_2
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_3)
#define KEYWORD_PERMUTATION_3
#elif defined(_USE_LAYER_2) && defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_4
#elif defined(_USE_LAYER_2) && defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_5
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_6
#elif defined(_USE_LAYER_2)
#define KEYWORD_PERMUTATION_7
#elif defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_8
#elif defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_9
#elif defined(_USE_LAYER_3) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_10
#elif defined(_USE_LAYER_3)
#define KEYWORD_PERMUTATION_11
#elif defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_12
#elif defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_13
#elif defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_14
#else
#define KEYWORD_PERMUTATION_15
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

float4 _Layer_1_Color_Map_TexelSize;
float _Layer_1_Hue;
float _Layer_1_Brightness;
float _Coverage_Height_Mask_Length;
float4 _Layer_1_Normal_Map_TexelSize;
float _Layer_1_Normal_Strength;
float _Layer_1_Tiling;
float _Layer_1_Smoothness_Strength;
float _Layer_2_Tiling;
float4 _Layer_2_Color_Map_TexelSize;
float _Layer_2_Hue;
float _Layer_2_Brightness;
float _Layer_2_Smoothness_Strength;
float4 _Layer_2_Normal_Map_TexelSize;
float _Layer_2_Normal_Strength;
float _Layer_3_Tiling;
float4 _Layer_3_Color_Map_TexelSize;
float _Layer_3_Hue;
float _Layer_3_Brightness;
float _Layer_3_Smoothness_Strength;
float4 _Layer_3_Normal_Map_TexelSize;
float _Layer_3_Normal_Strength;
float4 _RGB_Map_TexelSize;
float4 _Normal_Map_TexelSize;
float _Normal_Strength;
float _Coverage_Tiling;
float4 _Coverage_Color_Map_TexelSize;
float _Coverage_Hue;
float _Coverage_Brightness;
float _Coverage_Smoothness_Strength;
float4 _Coverage_Normal_Map_TexelSize;
float _Coverage_Normal_Strength;
float4 _Edge_Map_TexelSize;
float _Dirt_Mask_Contrast;
float _AO_Mask_Contrast;
float _Edgewear_Mask_Contrast;
float _Stylization_Mask_Contrast;
float4 _Stylization_Color;
float4 _AO_Color;
float4 _Dirt_Color;
float4 _Edgewear_Color;
float _Coverage_Normal_Mask_Slope;
float _Coverage_Normal_Mask_Gradience;
float _Coverage_Height_Mask_Pivot_Offset;
float _Scale_UVs_with_Object;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Layer_1_Color_Map);
SAMPLER(sampler_Layer_1_Color_Map);
TEXTURE2D(_Layer_1_Normal_Map);
SAMPLER(sampler_Layer_1_Normal_Map);
TEXTURE2D(_Layer_2_Color_Map);
SAMPLER(sampler_Layer_2_Color_Map);
TEXTURE2D(_Layer_2_Normal_Map);
SAMPLER(sampler_Layer_2_Normal_Map);
TEXTURE2D(_Layer_3_Color_Map);
SAMPLER(sampler_Layer_3_Color_Map);
TEXTURE2D(_Layer_3_Normal_Map);
SAMPLER(sampler_Layer_3_Normal_Map);
TEXTURE2D(_RGB_Map);
SAMPLER(sampler_RGB_Map);
TEXTURE2D(_Normal_Map);
SAMPLER(sampler_Normal_Map);
TEXTURE2D(_Coverage_Color_Map);
SAMPLER(sampler_Coverage_Color_Map);
TEXTURE2D(_Coverage_Normal_Map);
SAMPLER(sampler_Coverage_Normal_Map);
TEXTURE2D(_Edge_Map);
SAMPLER(sampler_Edge_Map);

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
#pragma shader_feature_local _ _USE_LAYER_2
#pragma shader_feature_local _ _USE_LAYER_3
#pragma shader_feature_local _ _USE_COVERAGE_LAYER
#pragma shader_feature_local _ _USE_LAYER_1

#if defined(_USE_LAYER_2) && defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_3) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_2
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_3)
#define KEYWORD_PERMUTATION_3
#elif defined(_USE_LAYER_2) && defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_4
#elif defined(_USE_LAYER_2) && defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_5
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_6
#elif defined(_USE_LAYER_2)
#define KEYWORD_PERMUTATION_7
#elif defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_8
#elif defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_9
#elif defined(_USE_LAYER_3) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_10
#elif defined(_USE_LAYER_3)
#define KEYWORD_PERMUTATION_11
#elif defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_12
#elif defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_13
#elif defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_14
#else
#define KEYWORD_PERMUTATION_15
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

float4 _Layer_1_Color_Map_TexelSize;
float _Layer_1_Hue;
float _Layer_1_Brightness;
float _Coverage_Height_Mask_Length;
float4 _Layer_1_Normal_Map_TexelSize;
float _Layer_1_Normal_Strength;
float _Layer_1_Tiling;
float _Layer_1_Smoothness_Strength;
float _Layer_2_Tiling;
float4 _Layer_2_Color_Map_TexelSize;
float _Layer_2_Hue;
float _Layer_2_Brightness;
float _Layer_2_Smoothness_Strength;
float4 _Layer_2_Normal_Map_TexelSize;
float _Layer_2_Normal_Strength;
float _Layer_3_Tiling;
float4 _Layer_3_Color_Map_TexelSize;
float _Layer_3_Hue;
float _Layer_3_Brightness;
float _Layer_3_Smoothness_Strength;
float4 _Layer_3_Normal_Map_TexelSize;
float _Layer_3_Normal_Strength;
float4 _RGB_Map_TexelSize;
float4 _Normal_Map_TexelSize;
float _Normal_Strength;
float _Coverage_Tiling;
float4 _Coverage_Color_Map_TexelSize;
float _Coverage_Hue;
float _Coverage_Brightness;
float _Coverage_Smoothness_Strength;
float4 _Coverage_Normal_Map_TexelSize;
float _Coverage_Normal_Strength;
float4 _Edge_Map_TexelSize;
float _Dirt_Mask_Contrast;
float _AO_Mask_Contrast;
float _Edgewear_Mask_Contrast;
float _Stylization_Mask_Contrast;
float4 _Stylization_Color;
float4 _AO_Color;
float4 _Dirt_Color;
float4 _Edgewear_Color;
float _Coverage_Normal_Mask_Slope;
float _Coverage_Normal_Mask_Gradience;
float _Coverage_Height_Mask_Pivot_Offset;
float _Scale_UVs_with_Object;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Layer_1_Color_Map);
SAMPLER(sampler_Layer_1_Color_Map);
TEXTURE2D(_Layer_1_Normal_Map);
SAMPLER(sampler_Layer_1_Normal_Map);
TEXTURE2D(_Layer_2_Color_Map);
SAMPLER(sampler_Layer_2_Color_Map);
TEXTURE2D(_Layer_2_Normal_Map);
SAMPLER(sampler_Layer_2_Normal_Map);
TEXTURE2D(_Layer_3_Color_Map);
SAMPLER(sampler_Layer_3_Color_Map);
TEXTURE2D(_Layer_3_Normal_Map);
SAMPLER(sampler_Layer_3_Normal_Map);
TEXTURE2D(_RGB_Map);
SAMPLER(sampler_RGB_Map);
TEXTURE2D(_Normal_Map);
SAMPLER(sampler_Normal_Map);
TEXTURE2D(_Coverage_Color_Map);
SAMPLER(sampler_Coverage_Color_Map);
TEXTURE2D(_Coverage_Normal_Map);
SAMPLER(sampler_Coverage_Normal_Map);
TEXTURE2D(_Edge_Map);
SAMPLER(sampler_Edge_Map);

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
#pragma shader_feature_local _ _USE_LAYER_2
#pragma shader_feature_local _ _USE_LAYER_3
#pragma shader_feature_local _ _USE_COVERAGE_LAYER
#pragma shader_feature_local _ _USE_LAYER_1

#if defined(_USE_LAYER_2) && defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_3) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_2
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_3)
#define KEYWORD_PERMUTATION_3
#elif defined(_USE_LAYER_2) && defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_4
#elif defined(_USE_LAYER_2) && defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_5
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_6
#elif defined(_USE_LAYER_2)
#define KEYWORD_PERMUTATION_7
#elif defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_8
#elif defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_9
#elif defined(_USE_LAYER_3) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_10
#elif defined(_USE_LAYER_3)
#define KEYWORD_PERMUTATION_11
#elif defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_12
#elif defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_13
#elif defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_14
#else
#define KEYWORD_PERMUTATION_15
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
 float3 WorldSpaceNormal;
 float3 WorldSpaceTangent;
 float3 WorldSpaceBiTangent;
 float3 WorldSpacePosition;
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

float4 _Layer_1_Color_Map_TexelSize;
float _Layer_1_Hue;
float _Layer_1_Brightness;
float _Coverage_Height_Mask_Length;
float4 _Layer_1_Normal_Map_TexelSize;
float _Layer_1_Normal_Strength;
float _Layer_1_Tiling;
float _Layer_1_Smoothness_Strength;
float _Layer_2_Tiling;
float4 _Layer_2_Color_Map_TexelSize;
float _Layer_2_Hue;
float _Layer_2_Brightness;
float _Layer_2_Smoothness_Strength;
float4 _Layer_2_Normal_Map_TexelSize;
float _Layer_2_Normal_Strength;
float _Layer_3_Tiling;
float4 _Layer_3_Color_Map_TexelSize;
float _Layer_3_Hue;
float _Layer_3_Brightness;
float _Layer_3_Smoothness_Strength;
float4 _Layer_3_Normal_Map_TexelSize;
float _Layer_3_Normal_Strength;
float4 _RGB_Map_TexelSize;
float4 _Normal_Map_TexelSize;
float _Normal_Strength;
float _Coverage_Tiling;
float4 _Coverage_Color_Map_TexelSize;
float _Coverage_Hue;
float _Coverage_Brightness;
float _Coverage_Smoothness_Strength;
float4 _Coverage_Normal_Map_TexelSize;
float _Coverage_Normal_Strength;
float4 _Edge_Map_TexelSize;
float _Dirt_Mask_Contrast;
float _AO_Mask_Contrast;
float _Edgewear_Mask_Contrast;
float _Stylization_Mask_Contrast;
float4 _Stylization_Color;
float4 _AO_Color;
float4 _Dirt_Color;
float4 _Edgewear_Color;
float _Coverage_Normal_Mask_Slope;
float _Coverage_Normal_Mask_Gradience;
float _Coverage_Height_Mask_Pivot_Offset;
float _Scale_UVs_with_Object;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Layer_1_Color_Map);
SAMPLER(sampler_Layer_1_Color_Map);
TEXTURE2D(_Layer_1_Normal_Map);
SAMPLER(sampler_Layer_1_Normal_Map);
TEXTURE2D(_Layer_2_Color_Map);
SAMPLER(sampler_Layer_2_Color_Map);
TEXTURE2D(_Layer_2_Normal_Map);
SAMPLER(sampler_Layer_2_Normal_Map);
TEXTURE2D(_Layer_3_Color_Map);
SAMPLER(sampler_Layer_3_Color_Map);
TEXTURE2D(_Layer_3_Normal_Map);
SAMPLER(sampler_Layer_3_Normal_Map);
TEXTURE2D(_RGB_Map);
SAMPLER(sampler_RGB_Map);
TEXTURE2D(_Normal_Map);
SAMPLER(sampler_Normal_Map);
TEXTURE2D(_Coverage_Color_Map);
SAMPLER(sampler_Coverage_Color_Map);
TEXTURE2D(_Coverage_Normal_Map);
SAMPLER(sampler_Coverage_Normal_Map);
TEXTURE2D(_Edge_Map);
SAMPLER(sampler_Edge_Map);

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

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Hue_Normalized_float(float3 In, float Offset, out float3 Out)
{
    // RGB to HSV
    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 P = lerp(float4(In.bg, K.wz), float4(In.gb, K.xy), step(In.b, In.g));
    float4 Q = lerp(float4(P.xyw, In.r), float4(In.r, P.yzx), step(P.x, In.r));
    float D = Q.x - min(Q.w, Q.y);
    float E = 1e-10;
    float V = (D == 0) ? Q.x : (Q.x + E);
    float3 hsv = float3(abs(Q.z + (Q.w - Q.y)/(6.0 * D + E)), D / (Q.x + E), V);

    float hue = hsv.x + Offset;
    hsv.x = (hue < 0)
            ? hue + 1
            : (hue > 1)
                ? hue - 1
                : hue;

    // HSV to RGB
    float4 K2 = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 P2 = abs(frac(hsv.xxx + K2.xyz) * 6.0 - K2.www);
    Out = hsv.z * lerp(K2.xxx, saturate(P2 - K2.xxx), hsv.y);
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Lerp_float(float A, float B, float T, out float Out)
{
    Out = lerp(A, B, T);
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

struct Bindings_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float
{
};

void SG_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float(float _Input, float _Contrast, Bindings_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float IN, out float Out_1)
{
float _Property_f17f2d608b764773bc9f33ea28c3e14e_Out_0_Float = _Contrast;
float _Add_59838b3eb9ec437bbd93b6fd8db7a0c4_Out_2_Float;
Unity_Add_float(_Property_f17f2d608b764773bc9f33ea28c3e14e_Out_0_Float, float(-1), _Add_59838b3eb9ec437bbd93b6fd8db7a0c4_Out_2_Float);
float _Add_524e73a4aa0b4e7982442fdf0d39fe18_Out_2_Float;
Unity_Add_float(_Property_f17f2d608b764773bc9f33ea28c3e14e_Out_0_Float, float(1), _Add_524e73a4aa0b4e7982442fdf0d39fe18_Out_2_Float);
float _Property_df60db08578f4909bad9a81c68245e45_Out_0_Float = _Input;
float _Lerp_6af4dfb2e0f0426a81a1669c55ec8196_Out_3_Float;
Unity_Lerp_float(_Add_59838b3eb9ec437bbd93b6fd8db7a0c4_Out_2_Float, _Add_524e73a4aa0b4e7982442fdf0d39fe18_Out_2_Float, _Property_df60db08578f4909bad9a81c68245e45_Out_0_Float, _Lerp_6af4dfb2e0f0426a81a1669c55ec8196_Out_3_Float);
float _Saturate_59b070c1298d4279be831e68d2cd96c8_Out_1_Float;
Unity_Saturate_float(_Lerp_6af4dfb2e0f0426a81a1669c55ec8196_Out_3_Float, _Saturate_59b070c1298d4279be831e68d2cd96c8_Out_1_Float);
Out_1 = _Saturate_59b070c1298d4279be831e68d2cd96c8_Out_1_Float;
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Blend_Multiply_float3(float3 Base, float3 Blend, out float3 Out, float Opacity)
{
    Out = Base * Blend;
    Out = lerp(Base, Out, Opacity);
}

void Unity_Blend_Overlay_float3(float3 Base, float3 Blend, out float3 Out, float Opacity)
{
    float3 result1 = 1.0 - 2.0 * (1.0 - Base) * (1.0 - Blend);
    float3 result2 = 2.0 * Base * Blend;
    float3 zeroOrOne = step(Base, 0.5);
    Out = result2 * zeroOrOne + (1 - zeroOrOne) * result1;
    Out = lerp(Base, Out, Opacity);
}

void Unity_NormalStrength_float(float3 In, float Strength, out float3 Out)
{
    Out = float3(In.rg * Strength, lerp(1, In.b, saturate(Strength)));
}

void Unity_NormalBlend_float(float3 A, float3 B, out float3 Out)
{
    Out = SafeNormalize(float3(A.rg + B.rg, A.b * B.b));
}

void Unity_DotProduct_float3(float3 A, float3 B, out float Out)
{
    Out = dot(A, B);
}

void Unity_Smoothstep_float(float Edge1, float Edge2, float In, out float Out)
{
    Out = smoothstep(Edge1, Edge2, In);
}

void Unity_Subtract_float3(float3 A, float3 B, out float3 Out)
{
    Out = A - B;
}

struct Bindings_MFPivotVerticalGradience_c9e909c1845b8ae4b9a11aa715963787_float
{
float3 WorldSpacePosition;
};

void SG_MFPivotVerticalGradience_c9e909c1845b8ae4b9a11aa715963787_float(float _Offset, float _Length, Bindings_MFPivotVerticalGradience_c9e909c1845b8ae4b9a11aa715963787_float IN, out float Mask_1)
{
float _Property_4680dca2dee245e7aa77915a0b17cce9_Out_0_Float = _Offset;
float _Property_c763914b9bbb4da5abad7a3db51adcb2_Out_0_Float = _Length;
float _Add_ad1531c9af874822b29fd9a13421289e_Out_2_Float;
Unity_Add_float(_Property_4680dca2dee245e7aa77915a0b17cce9_Out_0_Float, _Property_c763914b9bbb4da5abad7a3db51adcb2_Out_0_Float, _Add_ad1531c9af874822b29fd9a13421289e_Out_2_Float);
float3 _Subtract_69e80d3fb1bb4d71b4be84a010b280ff_Out_2_Vector3;
Unity_Subtract_float3(IN.WorldSpacePosition, SHADERGRAPH_OBJECT_POSITION, _Subtract_69e80d3fb1bb4d71b4be84a010b280ff_Out_2_Vector3);
float _Split_bceff54ddef34fab8cfa9a2a0b8bbc58_R_1_Float = _Subtract_69e80d3fb1bb4d71b4be84a010b280ff_Out_2_Vector3[0];
float _Split_bceff54ddef34fab8cfa9a2a0b8bbc58_G_2_Float = _Subtract_69e80d3fb1bb4d71b4be84a010b280ff_Out_2_Vector3[1];
float _Split_bceff54ddef34fab8cfa9a2a0b8bbc58_B_3_Float = _Subtract_69e80d3fb1bb4d71b4be84a010b280ff_Out_2_Vector3[2];
float _Split_bceff54ddef34fab8cfa9a2a0b8bbc58_A_4_Float = 0;
float _Smoothstep_0109929c8f9e41888dfa7ee4fd85c377_Out_3_Float;
Unity_Smoothstep_float(_Property_4680dca2dee245e7aa77915a0b17cce9_Out_0_Float, _Add_ad1531c9af874822b29fd9a13421289e_Out_2_Float, _Split_bceff54ddef34fab8cfa9a2a0b8bbc58_G_2_Float, _Smoothstep_0109929c8f9e41888dfa7ee4fd85c377_Out_3_Float);
Mask_1 = _Smoothstep_0109929c8f9e41888dfa7ee4fd85c377_Out_3_Float;
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
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
UnityTexture2D _Property_82391a4213d745e08202065048659634_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_1_Color_Map, sampler_Layer_1_Color_Map, _Layer_1_Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_7afe6a7edf62460887e09c4746d796b6_Out_0_Vector4 = IN.uv0;
float3 _Multiply_15b5d23699554ab298d90333ce57d387_Out_2_Vector3;
Unity_Multiply_float3_float3((_UV_7afe6a7edf62460887e09c4746d796b6_Out_0_Vector4.xyz), float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z))), _Multiply_15b5d23699554ab298d90333ce57d387_Out_2_Vector3);
float _Property_9a5b0bf1bf584876a406e2b633741c94_Out_0_Float = _Scale_UVs_with_Object;
float3 _Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3;
Unity_Lerp_float3((_UV_7afe6a7edf62460887e09c4746d796b6_Out_0_Vector4.xyz), _Multiply_15b5d23699554ab298d90333ce57d387_Out_2_Vector3, (_Property_9a5b0bf1bf584876a406e2b633741c94_Out_0_Float.xxx), _Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3);
float _Property_460238447f6b4170aa7397df1262be96_Out_0_Float = _Layer_1_Tiling;
float3 _Multiply_94cb4adbcc9a4b25980974e6be235287_Out_2_Vector3;
Unity_Multiply_float3_float3(_Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3, (_Property_460238447f6b4170aa7397df1262be96_Out_0_Float.xxx), _Multiply_94cb4adbcc9a4b25980974e6be235287_Out_2_Vector3);
float4 _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_82391a4213d745e08202065048659634_Out_0_Texture2D.tex, _Property_82391a4213d745e08202065048659634_Out_0_Texture2D.samplerstate, _Property_82391a4213d745e08202065048659634_Out_0_Texture2D.GetTransformedUV((_Multiply_94cb4adbcc9a4b25980974e6be235287_Out_2_Vector3.xy)) );
if (_Property_82391a4213d745e08202065048659634_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4, _Property_82391a4213d745e08202065048659634_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_R_4_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4.r;
float _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_G_5_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4.g;
float _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_B_6_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4.b;
float _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_A_7_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4.a;
float _Split_03d41d27a027425fa76a6c910dfbed96_R_1_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4[0];
float _Split_03d41d27a027425fa76a6c910dfbed96_G_2_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4[1];
float _Split_03d41d27a027425fa76a6c910dfbed96_B_3_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4[2];
float _Split_03d41d27a027425fa76a6c910dfbed96_A_4_Float = _SampleTexture2D_342caace1a0045498fc6f29f77238ca3_RGBA_0_Vector4[3];
float3 _Vector3_29d44982f90c486b8d58d91423294375_Out_0_Vector3 = float3(_Split_03d41d27a027425fa76a6c910dfbed96_R_1_Float, _Split_03d41d27a027425fa76a6c910dfbed96_G_2_Float, _Split_03d41d27a027425fa76a6c910dfbed96_B_3_Float);
float _Property_c55f5c80dc494683850ea3803571112b_Out_0_Float = _Layer_1_Hue;
float3 _Hue_4528903dcb5b4b15be0e273df247bfd9_Out_2_Vector3;
Unity_Hue_Normalized_float(_Vector3_29d44982f90c486b8d58d91423294375_Out_0_Vector3, _Property_c55f5c80dc494683850ea3803571112b_Out_0_Float, _Hue_4528903dcb5b4b15be0e273df247bfd9_Out_2_Vector3);
float _Property_ba7720a9d8134eb58030eb416c6a024a_Out_0_Float = _Layer_1_Brightness;
float3 _Multiply_01d7b815489a446fa8f5fbd7bf096d4f_Out_2_Vector3;
Unity_Multiply_float3_float3(_Hue_4528903dcb5b4b15be0e273df247bfd9_Out_2_Vector3, (_Property_ba7720a9d8134eb58030eb416c6a024a_Out_0_Float.xxx), _Multiply_01d7b815489a446fa8f5fbd7bf096d4f_Out_2_Vector3);
UnityTexture2D _Property_f37e4daa8ff94f5eb2ca9f6f83a20bb3_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_2_Color_Map, sampler_Layer_2_Color_Map, _Layer_2_Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_812f9793e99740fcb534b8c8d8af0ff2_Out_0_Float = _Layer_2_Tiling;
float3 _Multiply_7719c519f67d4b1aaaf2cca1cc2d4d79_Out_2_Vector3;
Unity_Multiply_float3_float3(_Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3, (_Property_812f9793e99740fcb534b8c8d8af0ff2_Out_0_Float.xxx), _Multiply_7719c519f67d4b1aaaf2cca1cc2d4d79_Out_2_Vector3);
float4 _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_f37e4daa8ff94f5eb2ca9f6f83a20bb3_Out_0_Texture2D.tex, _Property_f37e4daa8ff94f5eb2ca9f6f83a20bb3_Out_0_Texture2D.samplerstate, _Property_f37e4daa8ff94f5eb2ca9f6f83a20bb3_Out_0_Texture2D.GetTransformedUV((_Multiply_7719c519f67d4b1aaaf2cca1cc2d4d79_Out_2_Vector3.xy)) );
if (_Property_f37e4daa8ff94f5eb2ca9f6f83a20bb3_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4, _Property_f37e4daa8ff94f5eb2ca9f6f83a20bb3_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_R_4_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4.r;
float _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_G_5_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4.g;
float _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_B_6_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4.b;
float _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_A_7_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4.a;
float _Split_d38832e5c62c44d3a795b83acceab016_R_1_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4[0];
float _Split_d38832e5c62c44d3a795b83acceab016_G_2_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4[1];
float _Split_d38832e5c62c44d3a795b83acceab016_B_3_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4[2];
float _Split_d38832e5c62c44d3a795b83acceab016_A_4_Float = _SampleTexture2D_60aca6c13a5c4982a91dfc056ce4f294_RGBA_0_Vector4[3];
float3 _Vector3_cb2069d38949469a976ac3a5a2003bd7_Out_0_Vector3 = float3(_Split_d38832e5c62c44d3a795b83acceab016_R_1_Float, _Split_d38832e5c62c44d3a795b83acceab016_G_2_Float, _Split_d38832e5c62c44d3a795b83acceab016_B_3_Float);
float _Property_8970d5f05c7d4b5783a57e1c656ee4a9_Out_0_Float = _Layer_2_Hue;
float3 _Hue_d8383543ffa64d25a0efd5fc36520be4_Out_2_Vector3;
Unity_Hue_Normalized_float(_Vector3_cb2069d38949469a976ac3a5a2003bd7_Out_0_Vector3, _Property_8970d5f05c7d4b5783a57e1c656ee4a9_Out_0_Float, _Hue_d8383543ffa64d25a0efd5fc36520be4_Out_2_Vector3);
float _Property_40475d05242a4efaa3e83e117ecd268d_Out_0_Float = _Layer_2_Brightness;
float3 _Multiply_9a8ec1bf43a149a7a5fb1571321b8200_Out_2_Vector3;
Unity_Multiply_float3_float3(_Hue_d8383543ffa64d25a0efd5fc36520be4_Out_2_Vector3, (_Property_40475d05242a4efaa3e83e117ecd268d_Out_0_Float.xxx), _Multiply_9a8ec1bf43a149a7a5fb1571321b8200_Out_2_Vector3);
UnityTexture2D _Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_RGB_Map, sampler_RGB_Map, _RGB_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D.tex, _Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D.samplerstate, _Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4, _Property_d340afa9d40c4deab5fe8c40953b95fa_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_R_4_Float = _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4.r;
float _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_G_5_Float = _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4.g;
float _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_B_6_Float = _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4.b;
float _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_A_7_Float = _SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_RGBA_0_Vector4.a;
float3 _Lerp_754b59c21e034e87848bb65af4294c76_Out_3_Vector3;
Unity_Lerp_float3(_Multiply_01d7b815489a446fa8f5fbd7bf096d4f_Out_2_Vector3, _Multiply_9a8ec1bf43a149a7a5fb1571321b8200_Out_2_Vector3, (_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_G_5_Float.xxx), _Lerp_754b59c21e034e87848bb65af4294c76_Out_3_Vector3);
UnityTexture2D _Property_fef134089dc1401db12f51526b8ed988_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_3_Color_Map, sampler_Layer_3_Color_Map, _Layer_3_Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_8d20917a655e4062a5ce52f974969006_Out_0_Float = _Layer_3_Tiling;
float3 _Multiply_04b9ab405b2d4c27b6dd0696d9a5eacf_Out_2_Vector3;
Unity_Multiply_float3_float3(_Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3, (_Property_8d20917a655e4062a5ce52f974969006_Out_0_Float.xxx), _Multiply_04b9ab405b2d4c27b6dd0696d9a5eacf_Out_2_Vector3);
float4 _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_fef134089dc1401db12f51526b8ed988_Out_0_Texture2D.tex, _Property_fef134089dc1401db12f51526b8ed988_Out_0_Texture2D.samplerstate, _Property_fef134089dc1401db12f51526b8ed988_Out_0_Texture2D.GetTransformedUV((_Multiply_04b9ab405b2d4c27b6dd0696d9a5eacf_Out_2_Vector3.xy)) );
if (_Property_fef134089dc1401db12f51526b8ed988_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4, _Property_fef134089dc1401db12f51526b8ed988_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_R_4_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4.r;
float _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_G_5_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4.g;
float _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_B_6_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4.b;
float _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_A_7_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4.a;
float _Split_838946d3ce6f42cfbaf41fff836d6985_R_1_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4[0];
float _Split_838946d3ce6f42cfbaf41fff836d6985_G_2_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4[1];
float _Split_838946d3ce6f42cfbaf41fff836d6985_B_3_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4[2];
float _Split_838946d3ce6f42cfbaf41fff836d6985_A_4_Float = _SampleTexture2D_6897f69c3621426a86667e91ad1bedba_RGBA_0_Vector4[3];
float3 _Vector3_2a694d435907494fb07f2f9db09d031a_Out_0_Vector3 = float3(_Split_838946d3ce6f42cfbaf41fff836d6985_R_1_Float, _Split_838946d3ce6f42cfbaf41fff836d6985_G_2_Float, _Split_838946d3ce6f42cfbaf41fff836d6985_B_3_Float);
float _Property_6ba5eab2fbda4899981bd9c4c9c6317e_Out_0_Float = _Layer_3_Hue;
float3 _Hue_0a936fcae1974948a815da96c316b7e3_Out_2_Vector3;
Unity_Hue_Normalized_float(_Vector3_2a694d435907494fb07f2f9db09d031a_Out_0_Vector3, _Property_6ba5eab2fbda4899981bd9c4c9c6317e_Out_0_Float, _Hue_0a936fcae1974948a815da96c316b7e3_Out_2_Vector3);
float _Property_c749f04d24d9423fa1a2859f800dc893_Out_0_Float = _Layer_3_Brightness;
float3 _Multiply_db73d490dc9d432e80685ef18e5427fb_Out_2_Vector3;
Unity_Multiply_float3_float3(_Hue_0a936fcae1974948a815da96c316b7e3_Out_2_Vector3, (_Property_c749f04d24d9423fa1a2859f800dc893_Out_0_Float.xxx), _Multiply_db73d490dc9d432e80685ef18e5427fb_Out_2_Vector3);
float3 _Lerp_66f917f82ed14fd89c6c2fe066b5e834_Out_3_Vector3;
Unity_Lerp_float3(_Lerp_754b59c21e034e87848bb65af4294c76_Out_3_Vector3, _Multiply_db73d490dc9d432e80685ef18e5427fb_Out_2_Vector3, (_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_B_6_Float.xxx), _Lerp_66f917f82ed14fd89c6c2fe066b5e834_Out_3_Vector3);
#if defined(_USE_LAYER_3)
float3 _UseLayer3_ee2b565dd7774fa7a5c88d0b7bfd05b8_Out_0_Vector3 = _Lerp_66f917f82ed14fd89c6c2fe066b5e834_Out_3_Vector3;
#else
float3 _UseLayer3_ee2b565dd7774fa7a5c88d0b7bfd05b8_Out_0_Vector3 = _Lerp_754b59c21e034e87848bb65af4294c76_Out_3_Vector3;
#endif
#if defined(_USE_LAYER_2)
float3 _UseLayer2_22b88f361b80488fbbec2df7f45c4924_Out_0_Vector3 = _UseLayer3_ee2b565dd7774fa7a5c88d0b7bfd05b8_Out_0_Vector3;
#else
float3 _UseLayer2_22b88f361b80488fbbec2df7f45c4924_Out_0_Vector3 = _Multiply_01d7b815489a446fa8f5fbd7bf096d4f_Out_2_Vector3;
#endif
float4 _Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4 = _Stylization_Color;
float _Split_42e867e97f454925a658dba60e21864f_R_1_Float = _Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4[0];
float _Split_42e867e97f454925a658dba60e21864f_G_2_Float = _Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4[1];
float _Split_42e867e97f454925a658dba60e21864f_B_3_Float = _Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4[2];
float _Split_42e867e97f454925a658dba60e21864f_A_4_Float = _Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4[3];
UnityTexture2D _Property_c02e202098944eebbd44663588c64e3b_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Edge_Map, sampler_Edge_Map, _Edge_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_c02e202098944eebbd44663588c64e3b_Out_0_Texture2D.tex, _Property_c02e202098944eebbd44663588c64e3b_Out_0_Texture2D.samplerstate, _Property_c02e202098944eebbd44663588c64e3b_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_c02e202098944eebbd44663588c64e3b_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4, _Property_c02e202098944eebbd44663588c64e3b_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_R_4_Float = _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4.r;
float _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_G_5_Float = _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4.g;
float _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_B_6_Float = _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4.b;
float _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_A_7_Float = _SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_RGBA_0_Vector4.a;
float _Property_a4422d37671342bdab8d700fe4c2424a_Out_0_Float = _Stylization_Mask_Contrast;
Bindings_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float _MFCheapContrast_2e3888093b39403781e2ccbee9980699;
float _MFCheapContrast_2e3888093b39403781e2ccbee9980699_Out_1_Float;
SG_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float(_SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_A_7_Float, _Property_a4422d37671342bdab8d700fe4c2424a_Out_0_Float, _MFCheapContrast_2e3888093b39403781e2ccbee9980699, _MFCheapContrast_2e3888093b39403781e2ccbee9980699_Out_1_Float);
float _Multiply_364f0f8b2ff74453b42f050a8b1cb526_Out_2_Float;
Unity_Multiply_float_float(_Split_42e867e97f454925a658dba60e21864f_A_4_Float, _MFCheapContrast_2e3888093b39403781e2ccbee9980699_Out_1_Float, _Multiply_364f0f8b2ff74453b42f050a8b1cb526_Out_2_Float);
float3 _Lerp_0a1e51abb320477ab63b42e1a32052cc_Out_3_Vector3;
Unity_Lerp_float3(_UseLayer2_22b88f361b80488fbbec2df7f45c4924_Out_0_Vector3, (_Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4.xyz), (_Multiply_364f0f8b2ff74453b42f050a8b1cb526_Out_2_Float.xxx), _Lerp_0a1e51abb320477ab63b42e1a32052cc_Out_3_Vector3);
#if defined(_USE_LAYER_1)
float3 _UseLayer1_f5f8cf003ae64c4187983dc616ecbd3b_Out_0_Vector3 = _Lerp_0a1e51abb320477ab63b42e1a32052cc_Out_3_Vector3;
#else
float3 _UseLayer1_f5f8cf003ae64c4187983dc616ecbd3b_Out_0_Vector3 = (_Property_5739c2f2ad3c4ab6b122149e5a52bd3c_Out_0_Vector4.xyz);
#endif
float4 _Property_0be88f8e5ba94765a3623024be14bbbf_Out_0_Vector4 = _AO_Color;
float _Split_07ec24369bd1411c82179cabd24aabe5_R_1_Float = _Property_0be88f8e5ba94765a3623024be14bbbf_Out_0_Vector4[0];
float _Split_07ec24369bd1411c82179cabd24aabe5_G_2_Float = _Property_0be88f8e5ba94765a3623024be14bbbf_Out_0_Vector4[1];
float _Split_07ec24369bd1411c82179cabd24aabe5_B_3_Float = _Property_0be88f8e5ba94765a3623024be14bbbf_Out_0_Vector4[2];
float _Split_07ec24369bd1411c82179cabd24aabe5_A_4_Float = _Property_0be88f8e5ba94765a3623024be14bbbf_Out_0_Vector4[3];
float _Property_781496b67f424345bca22ad901512267_Out_0_Float = _AO_Mask_Contrast;
Bindings_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float _MFCheapContrast_74522f75ddab433f9ec9f709fe5a6faf;
float _MFCheapContrast_74522f75ddab433f9ec9f709fe5a6faf_Out_1_Float;
SG_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float(_SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_G_5_Float, _Property_781496b67f424345bca22ad901512267_Out_0_Float, _MFCheapContrast_74522f75ddab433f9ec9f709fe5a6faf, _MFCheapContrast_74522f75ddab433f9ec9f709fe5a6faf_Out_1_Float);
float _Multiply_27eebb10636649c3aefb6ad13580fd9e_Out_2_Float;
Unity_Multiply_float_float(_Split_07ec24369bd1411c82179cabd24aabe5_A_4_Float, _MFCheapContrast_74522f75ddab433f9ec9f709fe5a6faf_Out_1_Float, _Multiply_27eebb10636649c3aefb6ad13580fd9e_Out_2_Float);
float3 _Blend_5baeb7dcebe145dcb53bba406ea82ebd_Out_2_Vector3;
Unity_Blend_Multiply_float3(_UseLayer1_f5f8cf003ae64c4187983dc616ecbd3b_Out_0_Vector3, (_Property_0be88f8e5ba94765a3623024be14bbbf_Out_0_Vector4.xyz), _Blend_5baeb7dcebe145dcb53bba406ea82ebd_Out_2_Vector3, _Multiply_27eebb10636649c3aefb6ad13580fd9e_Out_2_Float);
float4 _Property_64075cb2946e4bc89c0928961fa8e759_Out_0_Vector4 = _Dirt_Color;
float _Split_102b0005351f49a0a7ec214d4c4e879e_R_1_Float = _Property_64075cb2946e4bc89c0928961fa8e759_Out_0_Vector4[0];
float _Split_102b0005351f49a0a7ec214d4c4e879e_G_2_Float = _Property_64075cb2946e4bc89c0928961fa8e759_Out_0_Vector4[1];
float _Split_102b0005351f49a0a7ec214d4c4e879e_B_3_Float = _Property_64075cb2946e4bc89c0928961fa8e759_Out_0_Vector4[2];
float _Split_102b0005351f49a0a7ec214d4c4e879e_A_4_Float = _Property_64075cb2946e4bc89c0928961fa8e759_Out_0_Vector4[3];
float _Property_089f6755c08b483d971238d917dd8b3a_Out_0_Float = _Dirt_Mask_Contrast;
Bindings_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float _MFCheapContrast_96f7295ada8247d48955ac898a7aba11;
float _MFCheapContrast_96f7295ada8247d48955ac898a7aba11_Out_1_Float;
SG_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float(_SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_R_4_Float, _Property_089f6755c08b483d971238d917dd8b3a_Out_0_Float, _MFCheapContrast_96f7295ada8247d48955ac898a7aba11, _MFCheapContrast_96f7295ada8247d48955ac898a7aba11_Out_1_Float);
float _Multiply_bc33d3e78b104e93a60b60cea85cebac_Out_2_Float;
Unity_Multiply_float_float(_Split_102b0005351f49a0a7ec214d4c4e879e_A_4_Float, _MFCheapContrast_96f7295ada8247d48955ac898a7aba11_Out_1_Float, _Multiply_bc33d3e78b104e93a60b60cea85cebac_Out_2_Float);
float3 _Lerp_ef18ae06dc3e4fd18201e3925bc55a85_Out_3_Vector3;
Unity_Lerp_float3(_Blend_5baeb7dcebe145dcb53bba406ea82ebd_Out_2_Vector3, (_Property_64075cb2946e4bc89c0928961fa8e759_Out_0_Vector4.xyz), (_Multiply_bc33d3e78b104e93a60b60cea85cebac_Out_2_Float.xxx), _Lerp_ef18ae06dc3e4fd18201e3925bc55a85_Out_3_Vector3);
float4 _Property_dbda4c8e93f54f92a18b3390cbaf06c0_Out_0_Vector4 = _Edgewear_Color;
float _Split_925676a8ce21420aad811d255e389512_R_1_Float = _Property_dbda4c8e93f54f92a18b3390cbaf06c0_Out_0_Vector4[0];
float _Split_925676a8ce21420aad811d255e389512_G_2_Float = _Property_dbda4c8e93f54f92a18b3390cbaf06c0_Out_0_Vector4[1];
float _Split_925676a8ce21420aad811d255e389512_B_3_Float = _Property_dbda4c8e93f54f92a18b3390cbaf06c0_Out_0_Vector4[2];
float _Split_925676a8ce21420aad811d255e389512_A_4_Float = _Property_dbda4c8e93f54f92a18b3390cbaf06c0_Out_0_Vector4[3];
float _Property_4bef2cb688474ef8909e6dc3f2439616_Out_0_Float = _Edgewear_Mask_Contrast;
Bindings_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float _MFCheapContrast_c8d787e56cf842869980c82103f39754;
float _MFCheapContrast_c8d787e56cf842869980c82103f39754_Out_1_Float;
SG_MFCheapContrast_1739ac00543c01c42b199b2dbbf6fab3_float(_SampleTexture2D_6ceeb4f93b444a1ea7b2655021b2296e_B_6_Float, _Property_4bef2cb688474ef8909e6dc3f2439616_Out_0_Float, _MFCheapContrast_c8d787e56cf842869980c82103f39754, _MFCheapContrast_c8d787e56cf842869980c82103f39754_Out_1_Float);
float _Multiply_38b6bc16bfef4cd3bec7370aabb6a836_Out_2_Float;
Unity_Multiply_float_float(_Split_925676a8ce21420aad811d255e389512_A_4_Float, _MFCheapContrast_c8d787e56cf842869980c82103f39754_Out_1_Float, _Multiply_38b6bc16bfef4cd3bec7370aabb6a836_Out_2_Float);
float3 _Blend_e613dc790cd741d3b367cb48289cffd5_Out_2_Vector3;
Unity_Blend_Overlay_float3(_Lerp_ef18ae06dc3e4fd18201e3925bc55a85_Out_3_Vector3, (_Property_dbda4c8e93f54f92a18b3390cbaf06c0_Out_0_Vector4.xyz), _Blend_e613dc790cd741d3b367cb48289cffd5_Out_2_Vector3, _Multiply_38b6bc16bfef4cd3bec7370aabb6a836_Out_2_Float);
UnityTexture2D _Property_003a6ee6678543e5964d3e811c19f789_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Coverage_Color_Map, sampler_Coverage_Color_Map, _Coverage_Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_d9f12d2b2464447a82f581c14778bbf8_Out_0_Float = _Coverage_Tiling;
float3 _Multiply_282d032113614cb58779c3a33671ad6c_Out_2_Vector3;
Unity_Multiply_float3_float3(_Lerp_4f2464e201e0479fa8c39db141c950c1_Out_3_Vector3, (_Property_d9f12d2b2464447a82f581c14778bbf8_Out_0_Float.xxx), _Multiply_282d032113614cb58779c3a33671ad6c_Out_2_Vector3);
float4 _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_003a6ee6678543e5964d3e811c19f789_Out_0_Texture2D.tex, _Property_003a6ee6678543e5964d3e811c19f789_Out_0_Texture2D.samplerstate, _Property_003a6ee6678543e5964d3e811c19f789_Out_0_Texture2D.GetTransformedUV((_Multiply_282d032113614cb58779c3a33671ad6c_Out_2_Vector3.xy)) );
if (_Property_003a6ee6678543e5964d3e811c19f789_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4, _Property_003a6ee6678543e5964d3e811c19f789_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_R_4_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4.r;
float _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_G_5_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4.g;
float _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_B_6_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4.b;
float _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_A_7_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4.a;
float _Split_42bc736ed19d41c2bc8a58c7f653b802_R_1_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4[0];
float _Split_42bc736ed19d41c2bc8a58c7f653b802_G_2_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4[1];
float _Split_42bc736ed19d41c2bc8a58c7f653b802_B_3_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4[2];
float _Split_42bc736ed19d41c2bc8a58c7f653b802_A_4_Float = _SampleTexture2D_f0f12a6177ed47e598c3d4d7ca3bfe2a_RGBA_0_Vector4[3];
float3 _Vector3_dd63cc8dd9a443a99a6ecc681ff6961e_Out_0_Vector3 = float3(_Split_42bc736ed19d41c2bc8a58c7f653b802_R_1_Float, _Split_42bc736ed19d41c2bc8a58c7f653b802_G_2_Float, _Split_42bc736ed19d41c2bc8a58c7f653b802_B_3_Float);
float _Property_86cbded0425a4bf2b5441e665a7f5551_Out_0_Float = _Coverage_Hue;
float3 _Hue_5e168e8c10044fe09120668e4b870b44_Out_2_Vector3;
Unity_Hue_Normalized_float(_Vector3_dd63cc8dd9a443a99a6ecc681ff6961e_Out_0_Vector3, _Property_86cbded0425a4bf2b5441e665a7f5551_Out_0_Float, _Hue_5e168e8c10044fe09120668e4b870b44_Out_2_Vector3);
float _Property_dccedc571d6d411291da660d59f31596_Out_0_Float = _Coverage_Brightness;
float3 _Multiply_c28f28e86afd43f584c04c39cc44c8c4_Out_2_Vector3;
Unity_Multiply_float3_float3(_Hue_5e168e8c10044fe09120668e4b870b44_Out_2_Vector3, (_Property_dccedc571d6d411291da660d59f31596_Out_0_Float.xxx), _Multiply_c28f28e86afd43f584c04c39cc44c8c4_Out_2_Vector3);
float _Property_f13be210d8b049a9a1e6c0e219f5a53f_Out_0_Float = _Coverage_Normal_Mask_Slope;
float _Property_1f786f31d0924f6ba0904b90319c56e3_Out_0_Float = _Coverage_Normal_Mask_Gradience;
float _Add_4177ca63c9724385910d37f97d0e8c48_Out_2_Float;
Unity_Add_float(_Property_f13be210d8b049a9a1e6c0e219f5a53f_Out_0_Float, _Property_1f786f31d0924f6ba0904b90319c56e3_Out_0_Float, _Add_4177ca63c9724385910d37f97d0e8c48_Out_2_Float);
UnityTexture2D _Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_1_Normal_Map, sampler_Layer_1_Normal_Map, _Layer_1_Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D.tex, _Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D.samplerstate, _Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D.GetTransformedUV((_Multiply_94cb4adbcc9a4b25980974e6be235287_Out_2_Vector3.xy)) );
if (_Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4, _Property_c8645c4bf998466eb3fa9474e1a6099f_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4);
float _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_R_4_Float = _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.r;
float _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_G_5_Float = _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.g;
float _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_B_6_Float = _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.b;
float _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_A_7_Float = _SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.a;
float _Property_7cfa9a8e94684810a4cd4c70a2257b6c_Out_0_Float = _Layer_1_Normal_Strength;
float3 _NormalStrength_9428e53c24e74ce189625604c4b60275_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_1f354b51f5644ceaaf4b18a6f4d90591_RGBA_0_Vector4.xyz), _Property_7cfa9a8e94684810a4cd4c70a2257b6c_Out_0_Float, _NormalStrength_9428e53c24e74ce189625604c4b60275_Out_2_Vector3);
UnityTexture2D _Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_2_Normal_Map, sampler_Layer_2_Normal_Map, _Layer_2_Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D.tex, _Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D.samplerstate, _Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D.GetTransformedUV((_Multiply_7719c519f67d4b1aaaf2cca1cc2d4d79_Out_2_Vector3.xy)) );
if (_Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4, _Property_8a13d8ac5d78447189186f3bcd90eb7f_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4);
float _SampleTexture2D_ae5b372472874ad28eac881821ce553c_R_4_Float = _SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.r;
float _SampleTexture2D_ae5b372472874ad28eac881821ce553c_G_5_Float = _SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.g;
float _SampleTexture2D_ae5b372472874ad28eac881821ce553c_B_6_Float = _SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.b;
float _SampleTexture2D_ae5b372472874ad28eac881821ce553c_A_7_Float = _SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.a;
float _Property_12c65401b09f44daa763974826741097_Out_0_Float = _Layer_2_Normal_Strength;
float3 _NormalStrength_93ad91a06836481ca4f36c947e62a393_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_ae5b372472874ad28eac881821ce553c_RGBA_0_Vector4.xyz), _Property_12c65401b09f44daa763974826741097_Out_0_Float, _NormalStrength_93ad91a06836481ca4f36c947e62a393_Out_2_Vector3);
float3 _Lerp_7325f1720917490cb13328e58ddd8efe_Out_3_Vector3;
Unity_Lerp_float3(_NormalStrength_9428e53c24e74ce189625604c4b60275_Out_2_Vector3, _NormalStrength_93ad91a06836481ca4f36c947e62a393_Out_2_Vector3, (_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_G_5_Float.xxx), _Lerp_7325f1720917490cb13328e58ddd8efe_Out_3_Vector3);
UnityTexture2D _Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Layer_3_Normal_Map, sampler_Layer_3_Normal_Map, _Layer_3_Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D.tex, _Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D.samplerstate, _Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D.GetTransformedUV((_Multiply_04b9ab405b2d4c27b6dd0696d9a5eacf_Out_2_Vector3.xy)) );
if (_Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4, _Property_8382c07e1f9040b8938313418aa00252_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4);
float _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_R_4_Float = _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.r;
float _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_G_5_Float = _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.g;
float _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_B_6_Float = _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.b;
float _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_A_7_Float = _SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.a;
float _Property_02480e6b3805449397b2d5a15e416673_Out_0_Float = _Layer_3_Normal_Strength;
float3 _NormalStrength_8e30a22d9ed147ca9ef200087a7f43aa_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_abf53bed72e847049251f108f6e91ec1_RGBA_0_Vector4.xyz), _Property_02480e6b3805449397b2d5a15e416673_Out_0_Float, _NormalStrength_8e30a22d9ed147ca9ef200087a7f43aa_Out_2_Vector3);
float3 _Lerp_642d324ee8d74cf888dc943377359b6d_Out_3_Vector3;
Unity_Lerp_float3(_Lerp_7325f1720917490cb13328e58ddd8efe_Out_3_Vector3, _NormalStrength_8e30a22d9ed147ca9ef200087a7f43aa_Out_2_Vector3, (_SampleTexture2D_e7e6ebb6af204f90b8cd715269c8a604_B_6_Float.xxx), _Lerp_642d324ee8d74cf888dc943377359b6d_Out_3_Vector3);
#if defined(_USE_LAYER_3)
float3 _UseLayer3_3e504be39f154559868343947f457c24_Out_0_Vector3 = _Lerp_642d324ee8d74cf888dc943377359b6d_Out_3_Vector3;
#else
float3 _UseLayer3_3e504be39f154559868343947f457c24_Out_0_Vector3 = _Lerp_7325f1720917490cb13328e58ddd8efe_Out_3_Vector3;
#endif
#if defined(_USE_LAYER_2)
float3 _UseLayer2_c90f92a0b0134057867d0810829f779e_Out_0_Vector3 = _UseLayer3_3e504be39f154559868343947f457c24_Out_0_Vector3;
#else
float3 _UseLayer2_c90f92a0b0134057867d0810829f779e_Out_0_Vector3 = _NormalStrength_9428e53c24e74ce189625604c4b60275_Out_2_Vector3;
#endif
UnityTexture2D _Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Normal_Map, sampler_Normal_Map, _Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D.tex, _Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D.samplerstate, _Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4, _Property_b1e9a5fc0351462b810996d5f917f04e_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4);
float _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_R_4_Float = _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.r;
float _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_G_5_Float = _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.g;
float _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_B_6_Float = _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.b;
float _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_A_7_Float = _SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.a;
float _Property_758b3d19a0c14ff099921db6234664b2_Out_0_Float = _Normal_Strength;
float3 _NormalStrength_3eb407934b854455a959c26453776991_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_9839df68356c4c41a95c5e5e2876e661_RGBA_0_Vector4.xyz), _Property_758b3d19a0c14ff099921db6234664b2_Out_0_Float, _NormalStrength_3eb407934b854455a959c26453776991_Out_2_Vector3);
float3 _NormalBlend_cb4851ae94454d539b1004bdc1f2604f_Out_2_Vector3;
Unity_NormalBlend_float(_UseLayer2_c90f92a0b0134057867d0810829f779e_Out_0_Vector3, _NormalStrength_3eb407934b854455a959c26453776991_Out_2_Vector3, _NormalBlend_cb4851ae94454d539b1004bdc1f2604f_Out_2_Vector3);
#if defined(_USE_LAYER_1)
float3 _UseLayer1_7da71e644bf2434f99e8005bad9e8f14_Out_0_Vector3 = _NormalBlend_cb4851ae94454d539b1004bdc1f2604f_Out_2_Vector3;
#else
float3 _UseLayer1_7da71e644bf2434f99e8005bad9e8f14_Out_0_Vector3 = _NormalStrength_3eb407934b854455a959c26453776991_Out_2_Vector3;
#endif
float3 _Transform_04398ac1bc954fb19c62ce3f5f6d670c_Out_1_Vector3;
{
float3x3 tangentTransform = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
_Transform_04398ac1bc954fb19c62ce3f5f6d670c_Out_1_Vector3 = TransformTangentToWorld(_UseLayer1_7da71e644bf2434f99e8005bad9e8f14_Out_0_Vector3.xyz, tangentTransform, true);
}
float _DotProduct_ae13069c870e4539be3826642299bede_Out_2_Float;
Unity_DotProduct_float3(_Transform_04398ac1bc954fb19c62ce3f5f6d670c_Out_1_Vector3, float3(0, 1, 0), _DotProduct_ae13069c870e4539be3826642299bede_Out_2_Float);
float _Smoothstep_652b53e75f9e481e8c829d3bd3d9c24e_Out_3_Float;
Unity_Smoothstep_float(_Property_f13be210d8b049a9a1e6c0e219f5a53f_Out_0_Float, _Add_4177ca63c9724385910d37f97d0e8c48_Out_2_Float, _DotProduct_ae13069c870e4539be3826642299bede_Out_2_Float, _Smoothstep_652b53e75f9e481e8c829d3bd3d9c24e_Out_3_Float);
float _Property_64938267e5134c5b9a4256d11aa25705_Out_0_Float = _Coverage_Height_Mask_Pivot_Offset;
float _Property_cfcce6f026924b048f8c33bdc73c14f6_Out_0_Float = _Coverage_Height_Mask_Length;
Bindings_MFPivotVerticalGradience_c9e909c1845b8ae4b9a11aa715963787_float _MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c;
_MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c.WorldSpacePosition = IN.WorldSpacePosition;
float _MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c_Mask_1_Float;
SG_MFPivotVerticalGradience_c9e909c1845b8ae4b9a11aa715963787_float(_Property_64938267e5134c5b9a4256d11aa25705_Out_0_Float, _Property_cfcce6f026924b048f8c33bdc73c14f6_Out_0_Float, _MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c, _MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c_Mask_1_Float);
float _Multiply_f6a21f2d15b7472e8173002af669cfdf_Out_2_Float;
Unity_Multiply_float_float(_Smoothstep_652b53e75f9e481e8c829d3bd3d9c24e_Out_3_Float, _MFPivotVerticalGradience_949a273431d14e1285701f9be91dfd8c_Mask_1_Float, _Multiply_f6a21f2d15b7472e8173002af669cfdf_Out_2_Float);
float3 _Lerp_90a44aeed76b4e67b245ea63960c28fc_Out_3_Vector3;
Unity_Lerp_float3(_Blend_e613dc790cd741d3b367cb48289cffd5_Out_2_Vector3, _Multiply_c28f28e86afd43f584c04c39cc44c8c4_Out_2_Vector3, (_Multiply_f6a21f2d15b7472e8173002af669cfdf_Out_2_Float.xxx), _Lerp_90a44aeed76b4e67b245ea63960c28fc_Out_3_Vector3);
#if defined(_USE_COVERAGE_LAYER)
float3 _UseCoverageLayer_402efe74563f417bb22b7997bdfc5c1e_Out_0_Vector3 = _Lerp_90a44aeed76b4e67b245ea63960c28fc_Out_3_Vector3;
#else
float3 _UseCoverageLayer_402efe74563f417bb22b7997bdfc5c1e_Out_0_Vector3 = _Blend_e613dc790cd741d3b367cb48289cffd5_Out_2_Vector3;
#endif
surface.BaseColor = _UseCoverageLayer_402efe74563f417bb22b7997bdfc5c1e_Out_0_Vector3;
surface.Emission = float3(0, 0, 0);
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

    // to preserve mikktspace compliance we use same scale renormFactor as was used on the normal.
    // This is explained in section 2.2 in "surface gradient based bump mapping framework"
    output.WorldSpaceTangent = renormFactor * input.tangentWS.xyz;
    output.WorldSpaceBiTangent = renormFactor * bitang;

    output.WorldSpacePosition = input.positionWS;

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
#pragma shader_feature_local _ _USE_LAYER_2
#pragma shader_feature_local _ _USE_LAYER_3
#pragma shader_feature_local _ _USE_COVERAGE_LAYER
#pragma shader_feature_local _ _USE_LAYER_1

#if defined(_USE_LAYER_2) && defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_3) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_2
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_3)
#define KEYWORD_PERMUTATION_3
#elif defined(_USE_LAYER_2) && defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_4
#elif defined(_USE_LAYER_2) && defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_5
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_6
#elif defined(_USE_LAYER_2)
#define KEYWORD_PERMUTATION_7
#elif defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_8
#elif defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_9
#elif defined(_USE_LAYER_3) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_10
#elif defined(_USE_LAYER_3)
#define KEYWORD_PERMUTATION_11
#elif defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_12
#elif defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_13
#elif defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_14
#else
#define KEYWORD_PERMUTATION_15
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

float4 _Layer_1_Color_Map_TexelSize;
float _Layer_1_Hue;
float _Layer_1_Brightness;
float _Coverage_Height_Mask_Length;
float4 _Layer_1_Normal_Map_TexelSize;
float _Layer_1_Normal_Strength;
float _Layer_1_Tiling;
float _Layer_1_Smoothness_Strength;
float _Layer_2_Tiling;
float4 _Layer_2_Color_Map_TexelSize;
float _Layer_2_Hue;
float _Layer_2_Brightness;
float _Layer_2_Smoothness_Strength;
float4 _Layer_2_Normal_Map_TexelSize;
float _Layer_2_Normal_Strength;
float _Layer_3_Tiling;
float4 _Layer_3_Color_Map_TexelSize;
float _Layer_3_Hue;
float _Layer_3_Brightness;
float _Layer_3_Smoothness_Strength;
float4 _Layer_3_Normal_Map_TexelSize;
float _Layer_3_Normal_Strength;
float4 _RGB_Map_TexelSize;
float4 _Normal_Map_TexelSize;
float _Normal_Strength;
float _Coverage_Tiling;
float4 _Coverage_Color_Map_TexelSize;
float _Coverage_Hue;
float _Coverage_Brightness;
float _Coverage_Smoothness_Strength;
float4 _Coverage_Normal_Map_TexelSize;
float _Coverage_Normal_Strength;
float4 _Edge_Map_TexelSize;
float _Dirt_Mask_Contrast;
float _AO_Mask_Contrast;
float _Edgewear_Mask_Contrast;
float _Stylization_Mask_Contrast;
float4 _Stylization_Color;
float4 _AO_Color;
float4 _Dirt_Color;
float4 _Edgewear_Color;
float _Coverage_Normal_Mask_Slope;
float _Coverage_Normal_Mask_Gradience;
float _Coverage_Height_Mask_Pivot_Offset;
float _Scale_UVs_with_Object;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Layer_1_Color_Map);
SAMPLER(sampler_Layer_1_Color_Map);
TEXTURE2D(_Layer_1_Normal_Map);
SAMPLER(sampler_Layer_1_Normal_Map);
TEXTURE2D(_Layer_2_Color_Map);
SAMPLER(sampler_Layer_2_Color_Map);
TEXTURE2D(_Layer_2_Normal_Map);
SAMPLER(sampler_Layer_2_Normal_Map);
TEXTURE2D(_Layer_3_Color_Map);
SAMPLER(sampler_Layer_3_Color_Map);
TEXTURE2D(_Layer_3_Normal_Map);
SAMPLER(sampler_Layer_3_Normal_Map);
TEXTURE2D(_RGB_Map);
SAMPLER(sampler_RGB_Map);
TEXTURE2D(_Normal_Map);
SAMPLER(sampler_Normal_Map);
TEXTURE2D(_Coverage_Color_Map);
SAMPLER(sampler_Coverage_Color_Map);
TEXTURE2D(_Coverage_Normal_Map);
SAMPLER(sampler_Coverage_Normal_Map);
TEXTURE2D(_Edge_Map);
SAMPLER(sampler_Edge_Map);

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
#pragma shader_feature_local _ _USE_LAYER_2
#pragma shader_feature_local _ _USE_LAYER_3
#pragma shader_feature_local _ _USE_COVERAGE_LAYER
#pragma shader_feature_local _ _USE_LAYER_1

#if defined(_USE_LAYER_2) && defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_3) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_2
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_3)
#define KEYWORD_PERMUTATION_3
#elif defined(_USE_LAYER_2) && defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_4
#elif defined(_USE_LAYER_2) && defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_5
#elif defined(_USE_LAYER_2) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_6
#elif defined(_USE_LAYER_2)
#define KEYWORD_PERMUTATION_7
#elif defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_8
#elif defined(_USE_LAYER_3) && defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_9
#elif defined(_USE_LAYER_3) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_10
#elif defined(_USE_LAYER_3)
#define KEYWORD_PERMUTATION_11
#elif defined(_USE_COVERAGE_LAYER) && defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_12
#elif defined(_USE_COVERAGE_LAYER)
#define KEYWORD_PERMUTATION_13
#elif defined(_USE_LAYER_1)
#define KEYWORD_PERMUTATION_14
#else
#define KEYWORD_PERMUTATION_15
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

float4 _Layer_1_Color_Map_TexelSize;
float _Layer_1_Hue;
float _Layer_1_Brightness;
float _Coverage_Height_Mask_Length;
float4 _Layer_1_Normal_Map_TexelSize;
float _Layer_1_Normal_Strength;
float _Layer_1_Tiling;
float _Layer_1_Smoothness_Strength;
float _Layer_2_Tiling;
float4 _Layer_2_Color_Map_TexelSize;
float _Layer_2_Hue;
float _Layer_2_Brightness;
float _Layer_2_Smoothness_Strength;
float4 _Layer_2_Normal_Map_TexelSize;
float _Layer_2_Normal_Strength;
float _Layer_3_Tiling;
float4 _Layer_3_Color_Map_TexelSize;
float _Layer_3_Hue;
float _Layer_3_Brightness;
float _Layer_3_Smoothness_Strength;
float4 _Layer_3_Normal_Map_TexelSize;
float _Layer_3_Normal_Strength;
float4 _RGB_Map_TexelSize;
float4 _Normal_Map_TexelSize;
float _Normal_Strength;
float _Coverage_Tiling;
float4 _Coverage_Color_Map_TexelSize;
float _Coverage_Hue;
float _Coverage_Brightness;
float _Coverage_Smoothness_Strength;
float4 _Coverage_Normal_Map_TexelSize;
float _Coverage_Normal_Strength;
float4 _Edge_Map_TexelSize;
float _Dirt_Mask_Contrast;
float _AO_Mask_Contrast;
float _Edgewear_Mask_Contrast;
float _Stylization_Mask_Contrast;
float4 _Stylization_Color;
float4 _AO_Color;
float4 _Dirt_Color;
float4 _Edgewear_Color;
float _Coverage_Normal_Mask_Slope;
float _Coverage_Normal_Mask_Gradience;
float _Coverage_Height_Mask_Pivot_Offset;
float _Scale_UVs_with_Object;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Layer_1_Color_Map);
SAMPLER(sampler_Layer_1_Color_Map);
TEXTURE2D(_Layer_1_Normal_Map);
SAMPLER(sampler_Layer_1_Normal_Map);
TEXTURE2D(_Layer_2_Color_Map);
SAMPLER(sampler_Layer_2_Color_Map);
TEXTURE2D(_Layer_2_Normal_Map);
SAMPLER(sampler_Layer_2_Normal_Map);
TEXTURE2D(_Layer_3_Color_Map);
SAMPLER(sampler_Layer_3_Color_Map);
TEXTURE2D(_Layer_3_Normal_Map);
SAMPLER(sampler_Layer_3_Normal_Map);
TEXTURE2D(_RGB_Map);
SAMPLER(sampler_RGB_Map);
TEXTURE2D(_Normal_Map);
SAMPLER(sampler_Normal_Map);
TEXTURE2D(_Coverage_Color_Map);
SAMPLER(sampler_Coverage_Color_Map);
TEXTURE2D(_Coverage_Normal_Map);
SAMPLER(sampler_Coverage_Normal_Map);
TEXTURE2D(_Edge_Map);
SAMPLER(sampler_Edge_Map);

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
CustomEditorForRenderPipeline "Polyart.RGB_RockCustomInspector" ""
CustomEditorForRenderPipeline "UnityEditor.ShaderGraphLitGUI" "UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset"
FallBack "Hidden/Shader Graph/FallbackError"
}