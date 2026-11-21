Shader "UPlayGround/Environment/Vegetation/ef2aa9333fbc23b40ae6289389ebd8fb"
{
Properties
{

        [HideInInspector] _EnvironmentReactionSupported("환경 반응 계약", Float) = 1
        [HideInInspector] _EnvironmentReactionBufferSupported("공유 표현 버퍼", Float) = 1
        [PerRendererData] [HideInInspector] _EnvironmentReactionIndex("표현 상태 인덱스", Float) = -1
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

_Color("Color", Color) = (0, 0, 0, 0)
_Base_Map_Scale("Base Map Scale", Float) = 1
[NoScaleOffset]_Base_Map("Base Map", 2D) = "white" {}
[NoScaleOffset]_Normal_Map("Normal Map", 2D) = "white" {}
_Normal_Strength("Normal Strength", Range(0, 2)) = 1
[NoScaleOffset]_Metallic_Map("Metallic Map", 2D) = "white" {}
_Metallic("Metallic", Range(0, 1)) = 0
_Smoothness("Smoothness", Range(0, 1)) = 0
[NoScaleOffset]_Coverage_Base_Map("Coverage Base Map", 2D) = "white" {}
[NoScaleOffset]_Coverage_Normal_Map("Coverage Normal Map", 2D) = "white" {}
_Coverage_Normal_Strength("Coverage Normal Strength", Range(0, 2)) = 1
[NoScaleOffset]_Coverage_Metallic_Map("Coverage Metallic Map", 2D) = "white" {}
_Coverage_Smoothness("Coverage Smoothness", Range(0, 1)) = 0
[NoScaleOffset]_Coverage_Mask("Coverage Mask", 2D) = "white" {}
_Coverage_Mask_Scale("Coverage Mask Scale", Float) = 6
_Coverage_Map_Scale("Coverage Map Scale", Float) = 1
[Toggle(_ENABLE_COVERAGE)]_ENABLE_COVERAGE("Enable Coverage", Float) = 0
[Toggle(_BLEND_NORMALS)]_BLEND_NORMALS("Blend Normals", Float) = 0
_Coverage_Color("Coverage Color", Color) = (0, 0, 0, 0)
_Level("Level", Float) = 0
_Contrast("Contrast", Range(0.1, 1)) = 0.1
_Fade("Fade", Range(-1, 1)) = 0
[KeywordEnum(Vertex Position, Normal Vector)]_COVERAGE_OVERLAY("Coverage Overlay", Float) = 0
[HideInInspector]_WorkflowMode("_WorkflowMode", Float) = 1
[HideInInspector]_CastShadows("_CastShadows", Float) = 1
[HideInInspector]_ReceiveShadows("_ReceiveShadows", Float) = 1
[HideInInspector]_Surface("_Surface", Float) = 0
[HideInInspector]_Blend("_Blend", Float) = 0
[HideInInspector]_AlphaClip("_AlphaClip", Float) = 0
[HideInInspector]_BlendModePreserveSpecular("_BlendModePreserveSpecular", Float) = 1
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
#pragma multi_compile _ _ENVIRONMENT_REACTION_BUFFER
#pragma target 4.5 _ENVIRONMENT_REACTION_BUFFER


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
#pragma shader_feature_local _COVERAGE_OVERLAY_VERTEX_POSITION _COVERAGE_OVERLAY_NORMAL_VECTOR
#pragma shader_feature_local _ _ENABLE_COVERAGE
#pragma shader_feature_local _ _BLEND_NORMALS

#if defined(_COVERAGE_OVERLAY_VERTEX_POSITION) && defined(_ENABLE_COVERAGE) && defined(_BLEND_NORMALS)
#define KEYWORD_PERMUTATION_0
#elif defined(_COVERAGE_OVERLAY_VERTEX_POSITION) && defined(_ENABLE_COVERAGE)
#define KEYWORD_PERMUTATION_1
#elif defined(_COVERAGE_OVERLAY_VERTEX_POSITION) && defined(_BLEND_NORMALS)
#define KEYWORD_PERMUTATION_2
#elif defined(_COVERAGE_OVERLAY_VERTEX_POSITION)
#define KEYWORD_PERMUTATION_3
#elif defined(_COVERAGE_OVERLAY_NORMAL_VECTOR) && defined(_ENABLE_COVERAGE) && defined(_BLEND_NORMALS)
#define KEYWORD_PERMUTATION_4
#elif defined(_COVERAGE_OVERLAY_NORMAL_VECTOR) && defined(_ENABLE_COVERAGE)
#define KEYWORD_PERMUTATION_5
#elif defined(_COVERAGE_OVERLAY_NORMAL_VECTOR) && defined(_BLEND_NORMALS)
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
float3 WorldSpacePosition;
 float3 WorldSpaceNormal;
 float3 TangentSpaceNormal;
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
float _EnvironmentReactionBufferSupported, _EnvironmentReactionIndex;
float4 _EnvState0, _EnvState1;
float _FuelCoverage, _AllowFuelCoverage;
float4 _WetTint, _CharTint, _FireColor;
float4 _EnvironmentCellBounds;
float _UseEnvironmentCells;

float4 _Coverage_Base_Map_TexelSize;
float4 _Base_Map_TexelSize;
float _Contrast;
float _Fade;
float4 _Normal_Map_TexelSize;
float4 _Coverage_Normal_Map_TexelSize;
float _Normal_Strength;
float _Coverage_Normal_Strength;
float _Smoothness;
float _Coverage_Smoothness;
float4 _Color;
float _Base_Map_Scale;
float _Level;
float4 _Coverage_Mask_TexelSize;
float _Coverage_Mask_Scale;
float4 _Coverage_Color;
float _Coverage_Map_Scale;
float4 _Metallic_Map_TexelSize;
float4 _Coverage_Metallic_Map_TexelSize;
float _Metallic;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Coverage_Base_Map);
SAMPLER(sampler_Coverage_Base_Map);
TEXTURE2D(_Base_Map);
SAMPLER(sampler_Base_Map);
TEXTURE2D(_Normal_Map);
SAMPLER(sampler_Normal_Map);
TEXTURE2D(_Coverage_Normal_Map);
SAMPLER(sampler_Coverage_Normal_Map);
TEXTURE2D(_Coverage_Mask);
SAMPLER(sampler_Coverage_Mask);
TEXTURE2D(_Metallic_Map);
SAMPLER(sampler_Metallic_Map);
TEXTURE2D(_Coverage_Metallic_Map);
SAMPLER(sampler_Coverage_Metallic_Map);

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

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

void Unity_Negate_float(float In, out float Out)
{
    Out = -1 * In;
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Power_float(float A, float B, out float Out)
{
    Out = pow(A, B);
}

void Unity_Contrast_float(float3 In, float Contrast, out float3 Out)
{
    float midpoint = pow(0.5, 2.2);
    Out =  (In - midpoint) * Contrast + midpoint;
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Saturate_float3(float3 In, out float3 Out)
{
    Out = saturate(In);
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
{
    Out = lerp(A, B, T);
}

void Unity_NormalStrength_float(float3 In, float Strength, out float3 Out)
{
    Out = float3(In.rg * Strength, lerp(1, In.b, saturate(Strength)));
}

void Unity_NormalBlend_float(float3 A, float3 B, out float3 Out)
{
    Out = SafeNormalize(float3(A.rg + B.rg, A.b * B.b));
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
UnityTexture2D _Property_962e37e0ac5244c4a3ea6b170425e6e4_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Base_Map, sampler_Base_Map, _Base_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_837a954a3e08436195bd267068cb4b1a_Out_0_Float = _Base_Map_Scale;
float2 _TilingAndOffset_96e68e44c54540839992559f96312049_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, (_Property_837a954a3e08436195bd267068cb4b1a_Out_0_Float.xx), float2 (0, 0), _TilingAndOffset_96e68e44c54540839992559f96312049_Out_3_Vector2);
float4 _SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_962e37e0ac5244c4a3ea6b170425e6e4_Out_0_Texture2D.tex, _Property_962e37e0ac5244c4a3ea6b170425e6e4_Out_0_Texture2D.samplerstate, _Property_962e37e0ac5244c4a3ea6b170425e6e4_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_96e68e44c54540839992559f96312049_Out_3_Vector2) );
if (_Property_962e37e0ac5244c4a3ea6b170425e6e4_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_RGBA_0_Vector4, _Property_962e37e0ac5244c4a3ea6b170425e6e4_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_R_4_Float = _SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_RGBA_0_Vector4.r;
float _SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_G_5_Float = _SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_RGBA_0_Vector4.g;
float _SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_B_6_Float = _SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_RGBA_0_Vector4.b;
float _SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_A_7_Float = _SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_RGBA_0_Vector4.a;
float4 _Property_8bc5cedd8b5144aba79ab296bf97f4b8_Out_0_Vector4 = _Color;
float4 _Multiply_d62734e54c4a4b258cc66ff106c782ea_Out_2_Vector4;
Unity_Multiply_float4_float4(_SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_RGBA_0_Vector4, _Property_8bc5cedd8b5144aba79ab296bf97f4b8_Out_0_Vector4, _Multiply_d62734e54c4a4b258cc66ff106c782ea_Out_2_Vector4);
UnityTexture2D _Property_06c1669821ff42c3bf719660b6f40626_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Coverage_Base_Map, sampler_Coverage_Base_Map, _Coverage_Base_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_4fea0d4cff68413d90a1875fb9c320e7_Out_0_Float = _Coverage_Map_Scale;
float2 _TilingAndOffset_264e68c10eca41918924efa9fb9469af_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, (_Property_4fea0d4cff68413d90a1875fb9c320e7_Out_0_Float.xx), float2 (0, 0), _TilingAndOffset_264e68c10eca41918924efa9fb9469af_Out_3_Vector2);
float4 _SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_06c1669821ff42c3bf719660b6f40626_Out_0_Texture2D.tex, _Property_06c1669821ff42c3bf719660b6f40626_Out_0_Texture2D.samplerstate, _Property_06c1669821ff42c3bf719660b6f40626_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_264e68c10eca41918924efa9fb9469af_Out_3_Vector2) );
if (_Property_06c1669821ff42c3bf719660b6f40626_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_RGBA_0_Vector4, _Property_06c1669821ff42c3bf719660b6f40626_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_R_4_Float = _SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_RGBA_0_Vector4.r;
float _SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_G_5_Float = _SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_RGBA_0_Vector4.g;
float _SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_B_6_Float = _SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_RGBA_0_Vector4.b;
float _SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_A_7_Float = _SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_RGBA_0_Vector4.a;
float4 _Property_c8d8bceb4b8a46b589dd57d79ecd9bf9_Out_0_Vector4 = _Coverage_Color;
float4 _Multiply_548a83f07d1f491d858cdae4602c6b4d_Out_2_Vector4;
Unity_Multiply_float4_float4(_SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_RGBA_0_Vector4, _Property_c8d8bceb4b8a46b589dd57d79ecd9bf9_Out_0_Vector4, _Multiply_548a83f07d1f491d858cdae4602c6b4d_Out_2_Vector4);
UnityTexture2D _Property_f89138a4a5294f5eacb84b4409275065_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Coverage_Mask, sampler_Coverage_Mask, _Coverage_Mask_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_25a54da52bb3450c8dd76359ada2a358_Out_0_Float = _Coverage_Mask_Scale;
float2 _TilingAndOffset_e8100e9136c347e2a346b1af40d87a52_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, (_Property_25a54da52bb3450c8dd76359ada2a358_Out_0_Float.xx), float2 (0, 0), _TilingAndOffset_e8100e9136c347e2a346b1af40d87a52_Out_3_Vector2);
float4 _SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_f89138a4a5294f5eacb84b4409275065_Out_0_Texture2D.tex, _Property_f89138a4a5294f5eacb84b4409275065_Out_0_Texture2D.samplerstate, _Property_f89138a4a5294f5eacb84b4409275065_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_e8100e9136c347e2a346b1af40d87a52_Out_3_Vector2) );
if (_Property_f89138a4a5294f5eacb84b4409275065_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_RGBA_0_Vector4, _Property_f89138a4a5294f5eacb84b4409275065_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_R_4_Float = _SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_RGBA_0_Vector4.r;
float _SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_G_5_Float = _SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_RGBA_0_Vector4.g;
float _SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_B_6_Float = _SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_RGBA_0_Vector4.b;
float _SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_A_7_Float = _SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_RGBA_0_Vector4.a;
float _Property_d8db892f807f4cdbb860c5f763c37576_Out_0_Float = _Level;
float _Split_dddb7fd39dd142f6a79faaf42e2f31fa_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_dddb7fd39dd142f6a79faaf42e2f31fa_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_dddb7fd39dd142f6a79faaf42e2f31fa_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_dddb7fd39dd142f6a79faaf42e2f31fa_A_4_Float = 0;
float _Negate_5d8cf946d6a44b0688a978f991ce300e_Out_1_Float;
Unity_Negate_float(_Split_dddb7fd39dd142f6a79faaf42e2f31fa_G_2_Float, _Negate_5d8cf946d6a44b0688a978f991ce300e_Out_1_Float);
UnityTexture2D _Property_6fa683e6dce24ba8a1a7b81715c96867_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Normal_Map, sampler_Normal_Map, _Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_6fa683e6dce24ba8a1a7b81715c96867_Out_0_Texture2D.tex, _Property_6fa683e6dce24ba8a1a7b81715c96867_Out_0_Texture2D.samplerstate, _Property_6fa683e6dce24ba8a1a7b81715c96867_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_96e68e44c54540839992559f96312049_Out_3_Vector2) );
if (_Property_6fa683e6dce24ba8a1a7b81715c96867_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4, _Property_6fa683e6dce24ba8a1a7b81715c96867_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4);
float _SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_R_4_Float = _SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4.r;
float _SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_G_5_Float = _SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4.g;
float _SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_B_6_Float = _SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4.b;
float _SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_A_7_Float = _SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4.a;
float3 _Transform_f92b73ad085c4c319bc399803f810059_Out_1_Vector3;
{
float3x3 tangentTransform = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
_Transform_f92b73ad085c4c319bc399803f810059_Out_1_Vector3 = TransformTangentToWorldDir((_SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4.xyz).xyz, tangentTransform, true).xyz;
}
#if defined(_COVERAGE_OVERLAY_VERTEX_POSITION)
float3 _CoverageOverlay_6c350da1b1bc48d8bd322a639af8a114_Out_0_Vector3 = (_Negate_5d8cf946d6a44b0688a978f991ce300e_Out_1_Float.xxx);
#else
float3 _CoverageOverlay_6c350da1b1bc48d8bd322a639af8a114_Out_0_Vector3 = _Transform_f92b73ad085c4c319bc399803f810059_Out_1_Vector3;
#endif
float _Split_5e81b1d12c8f426d99b755defa3f1129_R_1_Float = _CoverageOverlay_6c350da1b1bc48d8bd322a639af8a114_Out_0_Vector3[0];
float _Split_5e81b1d12c8f426d99b755defa3f1129_G_2_Float = _CoverageOverlay_6c350da1b1bc48d8bd322a639af8a114_Out_0_Vector3[1];
float _Split_5e81b1d12c8f426d99b755defa3f1129_B_3_Float = _CoverageOverlay_6c350da1b1bc48d8bd322a639af8a114_Out_0_Vector3[2];
float _Split_5e81b1d12c8f426d99b755defa3f1129_A_4_Float = 0;
float _Add_42c3350036d24612b752658957f0e996_Out_2_Float;
Unity_Add_float(_Property_d8db892f807f4cdbb860c5f763c37576_Out_0_Float, _Split_5e81b1d12c8f426d99b755defa3f1129_G_2_Float, _Add_42c3350036d24612b752658957f0e996_Out_2_Float);
float _Property_0b2c6c92abcd44f6ac12c213b29b4591_Out_0_Float = _Fade;
float _Multiply_e63d550088ac44d9aef2a43850ee5aa9_Out_2_Float;
Unity_Multiply_float_float(_Property_0b2c6c92abcd44f6ac12c213b29b4591_Out_0_Float, 5, _Multiply_e63d550088ac44d9aef2a43850ee5aa9_Out_2_Float);
float _Power_78c8b863b05d4425b22c06b245577c57_Out_2_Float;
Unity_Power_float(_Add_42c3350036d24612b752658957f0e996_Out_2_Float, _Multiply_e63d550088ac44d9aef2a43850ee5aa9_Out_2_Float, _Power_78c8b863b05d4425b22c06b245577c57_Out_2_Float);
float _Property_4a3dba7d791f4ae8afbe0ba166c8d15d_Out_0_Float = _Contrast;
float _Multiply_df64950968a547a495e68042783a7510_Out_2_Float;
Unity_Multiply_float_float(_Property_4a3dba7d791f4ae8afbe0ba166c8d15d_Out_0_Float, 5, _Multiply_df64950968a547a495e68042783a7510_Out_2_Float);
float3 _Contrast_d9aab63f6f4e464aaab650dfe18fa9c8_Out_2_Vector3;
Unity_Contrast_float((_Power_78c8b863b05d4425b22c06b245577c57_Out_2_Float.xxx), _Multiply_df64950968a547a495e68042783a7510_Out_2_Float, _Contrast_d9aab63f6f4e464aaab650dfe18fa9c8_Out_2_Vector3);
float3 _Multiply_cc22e2488cfa4aabab92d688a9b358a8_Out_2_Vector3;
Unity_Multiply_float3_float3((_SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_RGBA_0_Vector4.xyz), _Contrast_d9aab63f6f4e464aaab650dfe18fa9c8_Out_2_Vector3, _Multiply_cc22e2488cfa4aabab92d688a9b358a8_Out_2_Vector3);
#if defined(_ENABLE_COVERAGE)
float3 _EnableCoverage_8bf33e8bc98a4d1285a2c507af050ad2_Out_0_Vector3 = _Multiply_cc22e2488cfa4aabab92d688a9b358a8_Out_2_Vector3;
#else
float3 _EnableCoverage_8bf33e8bc98a4d1285a2c507af050ad2_Out_0_Vector3 = float3(0, 0, 0);
#endif
float3 _Saturate_9537e4d4f5274dbf9b1a191c2de9a0bd_Out_1_Vector3;
Unity_Saturate_float3(_EnableCoverage_8bf33e8bc98a4d1285a2c507af050ad2_Out_0_Vector3, _Saturate_9537e4d4f5274dbf9b1a191c2de9a0bd_Out_1_Vector3);
float3 _Lerp_7bbb7508500f4123a0db757387918fb8_Out_3_Vector3;
Unity_Lerp_float3((_Multiply_d62734e54c4a4b258cc66ff106c782ea_Out_2_Vector4.xyz), (_Multiply_548a83f07d1f491d858cdae4602c6b4d_Out_2_Vector4.xyz), _Saturate_9537e4d4f5274dbf9b1a191c2de9a0bd_Out_1_Vector3, _Lerp_7bbb7508500f4123a0db757387918fb8_Out_3_Vector3);
#if defined(_ENABLE_COVERAGE)
float3 _EnableCoverage_64958d03a7354be9a2dc2affb31f53a4_Out_0_Vector3 = _Lerp_7bbb7508500f4123a0db757387918fb8_Out_3_Vector3;
#else
float3 _EnableCoverage_64958d03a7354be9a2dc2affb31f53a4_Out_0_Vector3 = (_Multiply_d62734e54c4a4b258cc66ff106c782ea_Out_2_Vector4.xyz);
#endif
float _Property_c2af067f0d3e4d07b9aed4dfa65bc2f1_Out_0_Float = _Normal_Strength;
float3 _NormalStrength_5dc2d21824794b5da9897819e8ace0ee_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4.xyz), _Property_c2af067f0d3e4d07b9aed4dfa65bc2f1_Out_0_Float, _NormalStrength_5dc2d21824794b5da9897819e8ace0ee_Out_2_Vector3);
UnityTexture2D _Property_667d6ae9b714402ba1c363d17875b673_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Coverage_Normal_Map, sampler_Coverage_Normal_Map, _Coverage_Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_723c2d27d99b43b9a9ddec036c755090_Out_0_Float = _Coverage_Map_Scale;
float2 _TilingAndOffset_575abb976cd44768aeb58d322379cd59_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, (_Property_723c2d27d99b43b9a9ddec036c755090_Out_0_Float.xx), float2 (0, 0), _TilingAndOffset_575abb976cd44768aeb58d322379cd59_Out_3_Vector2);
float4 _SampleTexture2D_cfbdfb6f203f43b09e26762bf514022a_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_667d6ae9b714402ba1c363d17875b673_Out_0_Texture2D.tex, _Property_667d6ae9b714402ba1c363d17875b673_Out_0_Texture2D.samplerstate, _Property_667d6ae9b714402ba1c363d17875b673_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_575abb976cd44768aeb58d322379cd59_Out_3_Vector2) );
if (_Property_667d6ae9b714402ba1c363d17875b673_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_cfbdfb6f203f43b09e26762bf514022a_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_cfbdfb6f203f43b09e26762bf514022a_RGBA_0_Vector4, _Property_667d6ae9b714402ba1c363d17875b673_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_cfbdfb6f203f43b09e26762bf514022a_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_cfbdfb6f203f43b09e26762bf514022a_RGBA_0_Vector4);
float _SampleTexture2D_cfbdfb6f203f43b09e26762bf514022a_R_4_Float = _SampleTexture2D_cfbdfb6f203f43b09e26762bf514022a_RGBA_0_Vector4.r;
float _SampleTexture2D_cfbdfb6f203f43b09e26762bf514022a_G_5_Float = _SampleTexture2D_cfbdfb6f203f43b09e26762bf514022a_RGBA_0_Vector4.g;
float _SampleTexture2D_cfbdfb6f203f43b09e26762bf514022a_B_6_Float = _SampleTexture2D_cfbdfb6f203f43b09e26762bf514022a_RGBA_0_Vector4.b;
float _SampleTexture2D_cfbdfb6f203f43b09e26762bf514022a_A_7_Float = _SampleTexture2D_cfbdfb6f203f43b09e26762bf514022a_RGBA_0_Vector4.a;
float _Property_2459308922da4207b1e08d83995283f8_Out_0_Float = _Coverage_Normal_Strength;
float3 _NormalStrength_fb236b808ff24064af2bdfee4b5a5210_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_cfbdfb6f203f43b09e26762bf514022a_RGBA_0_Vector4.xyz), _Property_2459308922da4207b1e08d83995283f8_Out_0_Float, _NormalStrength_fb236b808ff24064af2bdfee4b5a5210_Out_2_Vector3);
float3 _NormalBlend_447b28fead15487ab5e4f6beee537fa1_Out_2_Vector3;
Unity_NormalBlend_float(_NormalStrength_5dc2d21824794b5da9897819e8ace0ee_Out_2_Vector3, _NormalStrength_fb236b808ff24064af2bdfee4b5a5210_Out_2_Vector3, _NormalBlend_447b28fead15487ab5e4f6beee537fa1_Out_2_Vector3);
#if defined(_BLEND_NORMALS)
float3 _BlendNormals_3488543c00894b85be4235ce5144518e_Out_0_Vector3 = _NormalBlend_447b28fead15487ab5e4f6beee537fa1_Out_2_Vector3;
#else
float3 _BlendNormals_3488543c00894b85be4235ce5144518e_Out_0_Vector3 = _NormalStrength_fb236b808ff24064af2bdfee4b5a5210_Out_2_Vector3;
#endif
float3 _Lerp_16fa4c8d40ce4aed9cdf811577b4af07_Out_3_Vector3;
Unity_Lerp_float3(_NormalStrength_5dc2d21824794b5da9897819e8ace0ee_Out_2_Vector3, _BlendNormals_3488543c00894b85be4235ce5144518e_Out_0_Vector3, _Saturate_9537e4d4f5274dbf9b1a191c2de9a0bd_Out_1_Vector3, _Lerp_16fa4c8d40ce4aed9cdf811577b4af07_Out_3_Vector3);
#if defined(_ENABLE_COVERAGE)
float3 _EnableCoverage_e950d03b1e3b47748347c719515869e0_Out_0_Vector3 = _Lerp_16fa4c8d40ce4aed9cdf811577b4af07_Out_3_Vector3;
#else
float3 _EnableCoverage_e950d03b1e3b47748347c719515869e0_Out_0_Vector3 = _NormalStrength_5dc2d21824794b5da9897819e8ace0ee_Out_2_Vector3;
#endif
float _Property_9dcf0d1a1c8e4358809cf5018c29d652_Out_0_Float = _Metallic;
UnityTexture2D _Property_b52589fca859421eae552875337b9b58_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Metallic_Map, sampler_Metallic_Map, _Metallic_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_6faf713add5e49bebeb0422c7c38698e_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_b52589fca859421eae552875337b9b58_Out_0_Texture2D.tex, _Property_b52589fca859421eae552875337b9b58_Out_0_Texture2D.samplerstate, _Property_b52589fca859421eae552875337b9b58_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_96e68e44c54540839992559f96312049_Out_3_Vector2) );
if (_Property_b52589fca859421eae552875337b9b58_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_6faf713add5e49bebeb0422c7c38698e_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_6faf713add5e49bebeb0422c7c38698e_RGBA_0_Vector4, _Property_b52589fca859421eae552875337b9b58_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_6faf713add5e49bebeb0422c7c38698e_R_4_Float = _SampleTexture2D_6faf713add5e49bebeb0422c7c38698e_RGBA_0_Vector4.r;
float _SampleTexture2D_6faf713add5e49bebeb0422c7c38698e_G_5_Float = _SampleTexture2D_6faf713add5e49bebeb0422c7c38698e_RGBA_0_Vector4.g;
float _SampleTexture2D_6faf713add5e49bebeb0422c7c38698e_B_6_Float = _SampleTexture2D_6faf713add5e49bebeb0422c7c38698e_RGBA_0_Vector4.b;
float _SampleTexture2D_6faf713add5e49bebeb0422c7c38698e_A_7_Float = _SampleTexture2D_6faf713add5e49bebeb0422c7c38698e_RGBA_0_Vector4.a;
float _Property_58f725dc6c13412db96022ff940321fa_Out_0_Float = _Smoothness;
float4 _Multiply_f11f4ead375b49c98b0e920e23d1c889_Out_2_Vector4;
Unity_Multiply_float4_float4(_SampleTexture2D_6faf713add5e49bebeb0422c7c38698e_RGBA_0_Vector4, (_Property_58f725dc6c13412db96022ff940321fa_Out_0_Float.xxxx), _Multiply_f11f4ead375b49c98b0e920e23d1c889_Out_2_Vector4);
UnityTexture2D _Property_15c31d2b844844d2855375009ab5f1c5_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Coverage_Metallic_Map, sampler_Coverage_Metallic_Map, _Coverage_Metallic_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_0ccf459549e44c1f948d796e9fae5808_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_15c31d2b844844d2855375009ab5f1c5_Out_0_Texture2D.tex, _Property_15c31d2b844844d2855375009ab5f1c5_Out_0_Texture2D.samplerstate, _Property_15c31d2b844844d2855375009ab5f1c5_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_264e68c10eca41918924efa9fb9469af_Out_3_Vector2) );
if (_Property_15c31d2b844844d2855375009ab5f1c5_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_0ccf459549e44c1f948d796e9fae5808_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_0ccf459549e44c1f948d796e9fae5808_RGBA_0_Vector4, _Property_15c31d2b844844d2855375009ab5f1c5_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_0ccf459549e44c1f948d796e9fae5808_R_4_Float = _SampleTexture2D_0ccf459549e44c1f948d796e9fae5808_RGBA_0_Vector4.r;
float _SampleTexture2D_0ccf459549e44c1f948d796e9fae5808_G_5_Float = _SampleTexture2D_0ccf459549e44c1f948d796e9fae5808_RGBA_0_Vector4.g;
float _SampleTexture2D_0ccf459549e44c1f948d796e9fae5808_B_6_Float = _SampleTexture2D_0ccf459549e44c1f948d796e9fae5808_RGBA_0_Vector4.b;
float _SampleTexture2D_0ccf459549e44c1f948d796e9fae5808_A_7_Float = _SampleTexture2D_0ccf459549e44c1f948d796e9fae5808_RGBA_0_Vector4.a;
float _Property_ad1d4bc3e7b24acdab99db2c1bf65f13_Out_0_Float = _Coverage_Smoothness;
float4 _Multiply_3edb3b3dfa874229bad0926e31102b93_Out_2_Vector4;
Unity_Multiply_float4_float4(_SampleTexture2D_0ccf459549e44c1f948d796e9fae5808_RGBA_0_Vector4, (_Property_ad1d4bc3e7b24acdab99db2c1bf65f13_Out_0_Float.xxxx), _Multiply_3edb3b3dfa874229bad0926e31102b93_Out_2_Vector4);
float3 _Lerp_93a10260905147e3813b7badb175d8c5_Out_3_Vector3;
Unity_Lerp_float3((_Multiply_f11f4ead375b49c98b0e920e23d1c889_Out_2_Vector4.xyz), (_Multiply_3edb3b3dfa874229bad0926e31102b93_Out_2_Vector4.xyz), _Saturate_9537e4d4f5274dbf9b1a191c2de9a0bd_Out_1_Vector3, _Lerp_93a10260905147e3813b7badb175d8c5_Out_3_Vector3);
surface.BaseColor = _EnableCoverage_64958d03a7354be9a2dc2affb31f53a4_Out_0_Vector3;
surface.NormalTS = _EnableCoverage_e950d03b1e3b47748347c719515869e0_Out_0_Vector3;
surface.Emission = float3(0, 0, 0);
surface.Metallic = _Property_9dcf0d1a1c8e4358809cf5018c29d652_Out_0_Float;
surface.Specular = IsGammaSpace() ? float3(0.5, 0.5, 0.5) : SRGBToLinear(float3(0.5, 0.5, 0.5));
surface.Smoothness = (_Lerp_93a10260905147e3813b7badb175d8c5_Out_3_Vector3).x;
surface.Occlusion = float(1);
surface.Alpha = float(1);
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
Cull [_Cull]
Blend [_SrcBlend] [_DstBlend], [_SrcBlendAlpha] [_DstBlendAlpha]
ZTest [_ZTest]
ZWrite [_ZWrite]

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM
#pragma multi_compile _ _ENVIRONMENT_REACTION_BUFFER
#pragma target 4.5 _ENVIRONMENT_REACTION_BUFFER


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
#pragma shader_feature_local _COVERAGE_OVERLAY_VERTEX_POSITION _COVERAGE_OVERLAY_NORMAL_VECTOR
#pragma shader_feature_local _ _ENABLE_COVERAGE
#pragma shader_feature_local _ _BLEND_NORMALS

#if defined(_COVERAGE_OVERLAY_VERTEX_POSITION) && defined(_ENABLE_COVERAGE) && defined(_BLEND_NORMALS)
#define KEYWORD_PERMUTATION_0
#elif defined(_COVERAGE_OVERLAY_VERTEX_POSITION) && defined(_ENABLE_COVERAGE)
#define KEYWORD_PERMUTATION_1
#elif defined(_COVERAGE_OVERLAY_VERTEX_POSITION) && defined(_BLEND_NORMALS)
#define KEYWORD_PERMUTATION_2
#elif defined(_COVERAGE_OVERLAY_VERTEX_POSITION)
#define KEYWORD_PERMUTATION_3
#elif defined(_COVERAGE_OVERLAY_NORMAL_VECTOR) && defined(_ENABLE_COVERAGE) && defined(_BLEND_NORMALS)
#define KEYWORD_PERMUTATION_4
#elif defined(_COVERAGE_OVERLAY_NORMAL_VECTOR) && defined(_ENABLE_COVERAGE)
#define KEYWORD_PERMUTATION_5
#elif defined(_COVERAGE_OVERLAY_NORMAL_VECTOR) && defined(_BLEND_NORMALS)
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
float3 WorldSpacePosition;
 float3 WorldSpaceNormal;
 float3 TangentSpaceNormal;
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
float _EnvironmentReactionBufferSupported, _EnvironmentReactionIndex;
float4 _EnvState0, _EnvState1;
float _FuelCoverage, _AllowFuelCoverage;
float4 _WetTint, _CharTint, _FireColor;
float4 _EnvironmentCellBounds;
float _UseEnvironmentCells;

float4 _Coverage_Base_Map_TexelSize;
float4 _Base_Map_TexelSize;
float _Contrast;
float _Fade;
float4 _Normal_Map_TexelSize;
float4 _Coverage_Normal_Map_TexelSize;
float _Normal_Strength;
float _Coverage_Normal_Strength;
float _Smoothness;
float _Coverage_Smoothness;
float4 _Color;
float _Base_Map_Scale;
float _Level;
float4 _Coverage_Mask_TexelSize;
float _Coverage_Mask_Scale;
float4 _Coverage_Color;
float _Coverage_Map_Scale;
float4 _Metallic_Map_TexelSize;
float4 _Coverage_Metallic_Map_TexelSize;
float _Metallic;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Coverage_Base_Map);
SAMPLER(sampler_Coverage_Base_Map);
TEXTURE2D(_Base_Map);
SAMPLER(sampler_Base_Map);
TEXTURE2D(_Normal_Map);
SAMPLER(sampler_Normal_Map);
TEXTURE2D(_Coverage_Normal_Map);
SAMPLER(sampler_Coverage_Normal_Map);
TEXTURE2D(_Coverage_Mask);
SAMPLER(sampler_Coverage_Mask);
TEXTURE2D(_Metallic_Map);
SAMPLER(sampler_Metallic_Map);
TEXTURE2D(_Coverage_Metallic_Map);
SAMPLER(sampler_Coverage_Metallic_Map);

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

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

void Unity_Negate_float(float In, out float Out)
{
    Out = -1 * In;
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Power_float(float A, float B, out float Out)
{
    Out = pow(A, B);
}

void Unity_Contrast_float(float3 In, float Contrast, out float3 Out)
{
    float midpoint = pow(0.5, 2.2);
    Out =  (In - midpoint) * Contrast + midpoint;
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Saturate_float3(float3 In, out float3 Out)
{
    Out = saturate(In);
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
{
    Out = lerp(A, B, T);
}

void Unity_NormalStrength_float(float3 In, float Strength, out float3 Out)
{
    Out = float3(In.rg * Strength, lerp(1, In.b, saturate(Strength)));
}

void Unity_NormalBlend_float(float3 A, float3 B, out float3 Out)
{
    Out = SafeNormalize(float3(A.rg + B.rg, A.b * B.b));
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
UnityTexture2D _Property_962e37e0ac5244c4a3ea6b170425e6e4_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Base_Map, sampler_Base_Map, _Base_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_837a954a3e08436195bd267068cb4b1a_Out_0_Float = _Base_Map_Scale;
float2 _TilingAndOffset_96e68e44c54540839992559f96312049_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, (_Property_837a954a3e08436195bd267068cb4b1a_Out_0_Float.xx), float2 (0, 0), _TilingAndOffset_96e68e44c54540839992559f96312049_Out_3_Vector2);
float4 _SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_962e37e0ac5244c4a3ea6b170425e6e4_Out_0_Texture2D.tex, _Property_962e37e0ac5244c4a3ea6b170425e6e4_Out_0_Texture2D.samplerstate, _Property_962e37e0ac5244c4a3ea6b170425e6e4_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_96e68e44c54540839992559f96312049_Out_3_Vector2) );
if (_Property_962e37e0ac5244c4a3ea6b170425e6e4_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_RGBA_0_Vector4, _Property_962e37e0ac5244c4a3ea6b170425e6e4_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_R_4_Float = _SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_RGBA_0_Vector4.r;
float _SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_G_5_Float = _SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_RGBA_0_Vector4.g;
float _SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_B_6_Float = _SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_RGBA_0_Vector4.b;
float _SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_A_7_Float = _SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_RGBA_0_Vector4.a;
float4 _Property_8bc5cedd8b5144aba79ab296bf97f4b8_Out_0_Vector4 = _Color;
float4 _Multiply_d62734e54c4a4b258cc66ff106c782ea_Out_2_Vector4;
Unity_Multiply_float4_float4(_SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_RGBA_0_Vector4, _Property_8bc5cedd8b5144aba79ab296bf97f4b8_Out_0_Vector4, _Multiply_d62734e54c4a4b258cc66ff106c782ea_Out_2_Vector4);
UnityTexture2D _Property_06c1669821ff42c3bf719660b6f40626_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Coverage_Base_Map, sampler_Coverage_Base_Map, _Coverage_Base_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_4fea0d4cff68413d90a1875fb9c320e7_Out_0_Float = _Coverage_Map_Scale;
float2 _TilingAndOffset_264e68c10eca41918924efa9fb9469af_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, (_Property_4fea0d4cff68413d90a1875fb9c320e7_Out_0_Float.xx), float2 (0, 0), _TilingAndOffset_264e68c10eca41918924efa9fb9469af_Out_3_Vector2);
float4 _SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_06c1669821ff42c3bf719660b6f40626_Out_0_Texture2D.tex, _Property_06c1669821ff42c3bf719660b6f40626_Out_0_Texture2D.samplerstate, _Property_06c1669821ff42c3bf719660b6f40626_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_264e68c10eca41918924efa9fb9469af_Out_3_Vector2) );
if (_Property_06c1669821ff42c3bf719660b6f40626_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_RGBA_0_Vector4, _Property_06c1669821ff42c3bf719660b6f40626_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_R_4_Float = _SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_RGBA_0_Vector4.r;
float _SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_G_5_Float = _SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_RGBA_0_Vector4.g;
float _SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_B_6_Float = _SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_RGBA_0_Vector4.b;
float _SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_A_7_Float = _SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_RGBA_0_Vector4.a;
float4 _Property_c8d8bceb4b8a46b589dd57d79ecd9bf9_Out_0_Vector4 = _Coverage_Color;
float4 _Multiply_548a83f07d1f491d858cdae4602c6b4d_Out_2_Vector4;
Unity_Multiply_float4_float4(_SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_RGBA_0_Vector4, _Property_c8d8bceb4b8a46b589dd57d79ecd9bf9_Out_0_Vector4, _Multiply_548a83f07d1f491d858cdae4602c6b4d_Out_2_Vector4);
UnityTexture2D _Property_f89138a4a5294f5eacb84b4409275065_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Coverage_Mask, sampler_Coverage_Mask, _Coverage_Mask_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_25a54da52bb3450c8dd76359ada2a358_Out_0_Float = _Coverage_Mask_Scale;
float2 _TilingAndOffset_e8100e9136c347e2a346b1af40d87a52_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, (_Property_25a54da52bb3450c8dd76359ada2a358_Out_0_Float.xx), float2 (0, 0), _TilingAndOffset_e8100e9136c347e2a346b1af40d87a52_Out_3_Vector2);
float4 _SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_f89138a4a5294f5eacb84b4409275065_Out_0_Texture2D.tex, _Property_f89138a4a5294f5eacb84b4409275065_Out_0_Texture2D.samplerstate, _Property_f89138a4a5294f5eacb84b4409275065_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_e8100e9136c347e2a346b1af40d87a52_Out_3_Vector2) );
if (_Property_f89138a4a5294f5eacb84b4409275065_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_RGBA_0_Vector4, _Property_f89138a4a5294f5eacb84b4409275065_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_R_4_Float = _SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_RGBA_0_Vector4.r;
float _SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_G_5_Float = _SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_RGBA_0_Vector4.g;
float _SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_B_6_Float = _SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_RGBA_0_Vector4.b;
float _SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_A_7_Float = _SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_RGBA_0_Vector4.a;
float _Property_d8db892f807f4cdbb860c5f763c37576_Out_0_Float = _Level;
float _Split_dddb7fd39dd142f6a79faaf42e2f31fa_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_dddb7fd39dd142f6a79faaf42e2f31fa_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_dddb7fd39dd142f6a79faaf42e2f31fa_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_dddb7fd39dd142f6a79faaf42e2f31fa_A_4_Float = 0;
float _Negate_5d8cf946d6a44b0688a978f991ce300e_Out_1_Float;
Unity_Negate_float(_Split_dddb7fd39dd142f6a79faaf42e2f31fa_G_2_Float, _Negate_5d8cf946d6a44b0688a978f991ce300e_Out_1_Float);
UnityTexture2D _Property_6fa683e6dce24ba8a1a7b81715c96867_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Normal_Map, sampler_Normal_Map, _Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_6fa683e6dce24ba8a1a7b81715c96867_Out_0_Texture2D.tex, _Property_6fa683e6dce24ba8a1a7b81715c96867_Out_0_Texture2D.samplerstate, _Property_6fa683e6dce24ba8a1a7b81715c96867_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_96e68e44c54540839992559f96312049_Out_3_Vector2) );
if (_Property_6fa683e6dce24ba8a1a7b81715c96867_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4, _Property_6fa683e6dce24ba8a1a7b81715c96867_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4);
float _SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_R_4_Float = _SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4.r;
float _SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_G_5_Float = _SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4.g;
float _SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_B_6_Float = _SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4.b;
float _SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_A_7_Float = _SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4.a;
float3 _Transform_f92b73ad085c4c319bc399803f810059_Out_1_Vector3;
{
float3x3 tangentTransform = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
_Transform_f92b73ad085c4c319bc399803f810059_Out_1_Vector3 = TransformTangentToWorldDir((_SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4.xyz).xyz, tangentTransform, true).xyz;
}
#if defined(_COVERAGE_OVERLAY_VERTEX_POSITION)
float3 _CoverageOverlay_6c350da1b1bc48d8bd322a639af8a114_Out_0_Vector3 = (_Negate_5d8cf946d6a44b0688a978f991ce300e_Out_1_Float.xxx);
#else
float3 _CoverageOverlay_6c350da1b1bc48d8bd322a639af8a114_Out_0_Vector3 = _Transform_f92b73ad085c4c319bc399803f810059_Out_1_Vector3;
#endif
float _Split_5e81b1d12c8f426d99b755defa3f1129_R_1_Float = _CoverageOverlay_6c350da1b1bc48d8bd322a639af8a114_Out_0_Vector3[0];
float _Split_5e81b1d12c8f426d99b755defa3f1129_G_2_Float = _CoverageOverlay_6c350da1b1bc48d8bd322a639af8a114_Out_0_Vector3[1];
float _Split_5e81b1d12c8f426d99b755defa3f1129_B_3_Float = _CoverageOverlay_6c350da1b1bc48d8bd322a639af8a114_Out_0_Vector3[2];
float _Split_5e81b1d12c8f426d99b755defa3f1129_A_4_Float = 0;
float _Add_42c3350036d24612b752658957f0e996_Out_2_Float;
Unity_Add_float(_Property_d8db892f807f4cdbb860c5f763c37576_Out_0_Float, _Split_5e81b1d12c8f426d99b755defa3f1129_G_2_Float, _Add_42c3350036d24612b752658957f0e996_Out_2_Float);
float _Property_0b2c6c92abcd44f6ac12c213b29b4591_Out_0_Float = _Fade;
float _Multiply_e63d550088ac44d9aef2a43850ee5aa9_Out_2_Float;
Unity_Multiply_float_float(_Property_0b2c6c92abcd44f6ac12c213b29b4591_Out_0_Float, 5, _Multiply_e63d550088ac44d9aef2a43850ee5aa9_Out_2_Float);
float _Power_78c8b863b05d4425b22c06b245577c57_Out_2_Float;
Unity_Power_float(_Add_42c3350036d24612b752658957f0e996_Out_2_Float, _Multiply_e63d550088ac44d9aef2a43850ee5aa9_Out_2_Float, _Power_78c8b863b05d4425b22c06b245577c57_Out_2_Float);
float _Property_4a3dba7d791f4ae8afbe0ba166c8d15d_Out_0_Float = _Contrast;
float _Multiply_df64950968a547a495e68042783a7510_Out_2_Float;
Unity_Multiply_float_float(_Property_4a3dba7d791f4ae8afbe0ba166c8d15d_Out_0_Float, 5, _Multiply_df64950968a547a495e68042783a7510_Out_2_Float);
float3 _Contrast_d9aab63f6f4e464aaab650dfe18fa9c8_Out_2_Vector3;
Unity_Contrast_float((_Power_78c8b863b05d4425b22c06b245577c57_Out_2_Float.xxx), _Multiply_df64950968a547a495e68042783a7510_Out_2_Float, _Contrast_d9aab63f6f4e464aaab650dfe18fa9c8_Out_2_Vector3);
float3 _Multiply_cc22e2488cfa4aabab92d688a9b358a8_Out_2_Vector3;
Unity_Multiply_float3_float3((_SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_RGBA_0_Vector4.xyz), _Contrast_d9aab63f6f4e464aaab650dfe18fa9c8_Out_2_Vector3, _Multiply_cc22e2488cfa4aabab92d688a9b358a8_Out_2_Vector3);
#if defined(_ENABLE_COVERAGE)
float3 _EnableCoverage_8bf33e8bc98a4d1285a2c507af050ad2_Out_0_Vector3 = _Multiply_cc22e2488cfa4aabab92d688a9b358a8_Out_2_Vector3;
#else
float3 _EnableCoverage_8bf33e8bc98a4d1285a2c507af050ad2_Out_0_Vector3 = float3(0, 0, 0);
#endif
float3 _Saturate_9537e4d4f5274dbf9b1a191c2de9a0bd_Out_1_Vector3;
Unity_Saturate_float3(_EnableCoverage_8bf33e8bc98a4d1285a2c507af050ad2_Out_0_Vector3, _Saturate_9537e4d4f5274dbf9b1a191c2de9a0bd_Out_1_Vector3);
float3 _Lerp_7bbb7508500f4123a0db757387918fb8_Out_3_Vector3;
Unity_Lerp_float3((_Multiply_d62734e54c4a4b258cc66ff106c782ea_Out_2_Vector4.xyz), (_Multiply_548a83f07d1f491d858cdae4602c6b4d_Out_2_Vector4.xyz), _Saturate_9537e4d4f5274dbf9b1a191c2de9a0bd_Out_1_Vector3, _Lerp_7bbb7508500f4123a0db757387918fb8_Out_3_Vector3);
#if defined(_ENABLE_COVERAGE)
float3 _EnableCoverage_64958d03a7354be9a2dc2affb31f53a4_Out_0_Vector3 = _Lerp_7bbb7508500f4123a0db757387918fb8_Out_3_Vector3;
#else
float3 _EnableCoverage_64958d03a7354be9a2dc2affb31f53a4_Out_0_Vector3 = (_Multiply_d62734e54c4a4b258cc66ff106c782ea_Out_2_Vector4.xyz);
#endif
float _Property_c2af067f0d3e4d07b9aed4dfa65bc2f1_Out_0_Float = _Normal_Strength;
float3 _NormalStrength_5dc2d21824794b5da9897819e8ace0ee_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4.xyz), _Property_c2af067f0d3e4d07b9aed4dfa65bc2f1_Out_0_Float, _NormalStrength_5dc2d21824794b5da9897819e8ace0ee_Out_2_Vector3);
UnityTexture2D _Property_667d6ae9b714402ba1c363d17875b673_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Coverage_Normal_Map, sampler_Coverage_Normal_Map, _Coverage_Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_723c2d27d99b43b9a9ddec036c755090_Out_0_Float = _Coverage_Map_Scale;
float2 _TilingAndOffset_575abb976cd44768aeb58d322379cd59_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, (_Property_723c2d27d99b43b9a9ddec036c755090_Out_0_Float.xx), float2 (0, 0), _TilingAndOffset_575abb976cd44768aeb58d322379cd59_Out_3_Vector2);
float4 _SampleTexture2D_cfbdfb6f203f43b09e26762bf514022a_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_667d6ae9b714402ba1c363d17875b673_Out_0_Texture2D.tex, _Property_667d6ae9b714402ba1c363d17875b673_Out_0_Texture2D.samplerstate, _Property_667d6ae9b714402ba1c363d17875b673_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_575abb976cd44768aeb58d322379cd59_Out_3_Vector2) );
if (_Property_667d6ae9b714402ba1c363d17875b673_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_cfbdfb6f203f43b09e26762bf514022a_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_cfbdfb6f203f43b09e26762bf514022a_RGBA_0_Vector4, _Property_667d6ae9b714402ba1c363d17875b673_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_cfbdfb6f203f43b09e26762bf514022a_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_cfbdfb6f203f43b09e26762bf514022a_RGBA_0_Vector4);
float _SampleTexture2D_cfbdfb6f203f43b09e26762bf514022a_R_4_Float = _SampleTexture2D_cfbdfb6f203f43b09e26762bf514022a_RGBA_0_Vector4.r;
float _SampleTexture2D_cfbdfb6f203f43b09e26762bf514022a_G_5_Float = _SampleTexture2D_cfbdfb6f203f43b09e26762bf514022a_RGBA_0_Vector4.g;
float _SampleTexture2D_cfbdfb6f203f43b09e26762bf514022a_B_6_Float = _SampleTexture2D_cfbdfb6f203f43b09e26762bf514022a_RGBA_0_Vector4.b;
float _SampleTexture2D_cfbdfb6f203f43b09e26762bf514022a_A_7_Float = _SampleTexture2D_cfbdfb6f203f43b09e26762bf514022a_RGBA_0_Vector4.a;
float _Property_2459308922da4207b1e08d83995283f8_Out_0_Float = _Coverage_Normal_Strength;
float3 _NormalStrength_fb236b808ff24064af2bdfee4b5a5210_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_cfbdfb6f203f43b09e26762bf514022a_RGBA_0_Vector4.xyz), _Property_2459308922da4207b1e08d83995283f8_Out_0_Float, _NormalStrength_fb236b808ff24064af2bdfee4b5a5210_Out_2_Vector3);
float3 _NormalBlend_447b28fead15487ab5e4f6beee537fa1_Out_2_Vector3;
Unity_NormalBlend_float(_NormalStrength_5dc2d21824794b5da9897819e8ace0ee_Out_2_Vector3, _NormalStrength_fb236b808ff24064af2bdfee4b5a5210_Out_2_Vector3, _NormalBlend_447b28fead15487ab5e4f6beee537fa1_Out_2_Vector3);
#if defined(_BLEND_NORMALS)
float3 _BlendNormals_3488543c00894b85be4235ce5144518e_Out_0_Vector3 = _NormalBlend_447b28fead15487ab5e4f6beee537fa1_Out_2_Vector3;
#else
float3 _BlendNormals_3488543c00894b85be4235ce5144518e_Out_0_Vector3 = _NormalStrength_fb236b808ff24064af2bdfee4b5a5210_Out_2_Vector3;
#endif
float3 _Lerp_16fa4c8d40ce4aed9cdf811577b4af07_Out_3_Vector3;
Unity_Lerp_float3(_NormalStrength_5dc2d21824794b5da9897819e8ace0ee_Out_2_Vector3, _BlendNormals_3488543c00894b85be4235ce5144518e_Out_0_Vector3, _Saturate_9537e4d4f5274dbf9b1a191c2de9a0bd_Out_1_Vector3, _Lerp_16fa4c8d40ce4aed9cdf811577b4af07_Out_3_Vector3);
#if defined(_ENABLE_COVERAGE)
float3 _EnableCoverage_e950d03b1e3b47748347c719515869e0_Out_0_Vector3 = _Lerp_16fa4c8d40ce4aed9cdf811577b4af07_Out_3_Vector3;
#else
float3 _EnableCoverage_e950d03b1e3b47748347c719515869e0_Out_0_Vector3 = _NormalStrength_5dc2d21824794b5da9897819e8ace0ee_Out_2_Vector3;
#endif
float _Property_9dcf0d1a1c8e4358809cf5018c29d652_Out_0_Float = _Metallic;
UnityTexture2D _Property_b52589fca859421eae552875337b9b58_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Metallic_Map, sampler_Metallic_Map, _Metallic_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_6faf713add5e49bebeb0422c7c38698e_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_b52589fca859421eae552875337b9b58_Out_0_Texture2D.tex, _Property_b52589fca859421eae552875337b9b58_Out_0_Texture2D.samplerstate, _Property_b52589fca859421eae552875337b9b58_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_96e68e44c54540839992559f96312049_Out_3_Vector2) );
if (_Property_b52589fca859421eae552875337b9b58_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_6faf713add5e49bebeb0422c7c38698e_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_6faf713add5e49bebeb0422c7c38698e_RGBA_0_Vector4, _Property_b52589fca859421eae552875337b9b58_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_6faf713add5e49bebeb0422c7c38698e_R_4_Float = _SampleTexture2D_6faf713add5e49bebeb0422c7c38698e_RGBA_0_Vector4.r;
float _SampleTexture2D_6faf713add5e49bebeb0422c7c38698e_G_5_Float = _SampleTexture2D_6faf713add5e49bebeb0422c7c38698e_RGBA_0_Vector4.g;
float _SampleTexture2D_6faf713add5e49bebeb0422c7c38698e_B_6_Float = _SampleTexture2D_6faf713add5e49bebeb0422c7c38698e_RGBA_0_Vector4.b;
float _SampleTexture2D_6faf713add5e49bebeb0422c7c38698e_A_7_Float = _SampleTexture2D_6faf713add5e49bebeb0422c7c38698e_RGBA_0_Vector4.a;
float _Property_58f725dc6c13412db96022ff940321fa_Out_0_Float = _Smoothness;
float4 _Multiply_f11f4ead375b49c98b0e920e23d1c889_Out_2_Vector4;
Unity_Multiply_float4_float4(_SampleTexture2D_6faf713add5e49bebeb0422c7c38698e_RGBA_0_Vector4, (_Property_58f725dc6c13412db96022ff940321fa_Out_0_Float.xxxx), _Multiply_f11f4ead375b49c98b0e920e23d1c889_Out_2_Vector4);
UnityTexture2D _Property_15c31d2b844844d2855375009ab5f1c5_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Coverage_Metallic_Map, sampler_Coverage_Metallic_Map, _Coverage_Metallic_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_0ccf459549e44c1f948d796e9fae5808_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_15c31d2b844844d2855375009ab5f1c5_Out_0_Texture2D.tex, _Property_15c31d2b844844d2855375009ab5f1c5_Out_0_Texture2D.samplerstate, _Property_15c31d2b844844d2855375009ab5f1c5_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_264e68c10eca41918924efa9fb9469af_Out_3_Vector2) );
if (_Property_15c31d2b844844d2855375009ab5f1c5_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_0ccf459549e44c1f948d796e9fae5808_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_0ccf459549e44c1f948d796e9fae5808_RGBA_0_Vector4, _Property_15c31d2b844844d2855375009ab5f1c5_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_0ccf459549e44c1f948d796e9fae5808_R_4_Float = _SampleTexture2D_0ccf459549e44c1f948d796e9fae5808_RGBA_0_Vector4.r;
float _SampleTexture2D_0ccf459549e44c1f948d796e9fae5808_G_5_Float = _SampleTexture2D_0ccf459549e44c1f948d796e9fae5808_RGBA_0_Vector4.g;
float _SampleTexture2D_0ccf459549e44c1f948d796e9fae5808_B_6_Float = _SampleTexture2D_0ccf459549e44c1f948d796e9fae5808_RGBA_0_Vector4.b;
float _SampleTexture2D_0ccf459549e44c1f948d796e9fae5808_A_7_Float = _SampleTexture2D_0ccf459549e44c1f948d796e9fae5808_RGBA_0_Vector4.a;
float _Property_ad1d4bc3e7b24acdab99db2c1bf65f13_Out_0_Float = _Coverage_Smoothness;
float4 _Multiply_3edb3b3dfa874229bad0926e31102b93_Out_2_Vector4;
Unity_Multiply_float4_float4(_SampleTexture2D_0ccf459549e44c1f948d796e9fae5808_RGBA_0_Vector4, (_Property_ad1d4bc3e7b24acdab99db2c1bf65f13_Out_0_Float.xxxx), _Multiply_3edb3b3dfa874229bad0926e31102b93_Out_2_Vector4);
float3 _Lerp_93a10260905147e3813b7badb175d8c5_Out_3_Vector3;
Unity_Lerp_float3((_Multiply_f11f4ead375b49c98b0e920e23d1c889_Out_2_Vector4.xyz), (_Multiply_3edb3b3dfa874229bad0926e31102b93_Out_2_Vector4.xyz), _Saturate_9537e4d4f5274dbf9b1a191c2de9a0bd_Out_1_Vector3, _Lerp_93a10260905147e3813b7badb175d8c5_Out_3_Vector3);
surface.BaseColor = _EnableCoverage_64958d03a7354be9a2dc2affb31f53a4_Out_0_Vector3;
surface.NormalTS = _EnableCoverage_e950d03b1e3b47748347c719515869e0_Out_0_Vector3;
surface.Emission = float3(0, 0, 0);
surface.Metallic = _Property_9dcf0d1a1c8e4358809cf5018c29d652_Out_0_Float;
surface.Specular = IsGammaSpace() ? float3(0.5, 0.5, 0.5) : SRGBToLinear(float3(0.5, 0.5, 0.5));
surface.Smoothness = (_Lerp_93a10260905147e3813b7badb175d8c5_Out_3_Vector3).x;
surface.Occlusion = float(1);
surface.Alpha = float(1);
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
Cull [_Cull]
ZTest LEqual
ZWrite On
ColorMask 0

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM
#pragma multi_compile _ _ENVIRONMENT_REACTION_BUFFER
#pragma target 4.5 _ENVIRONMENT_REACTION_BUFFER


// Pragmas
#pragma target 2.0
#pragma multi_compile_instancing
#pragma vertex vert
#pragma fragment frag

// Keywords
#pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
#pragma shader_feature_local_fragment _ _ALPHATEST_ON
#pragma shader_feature_local _COVERAGE_OVERLAY_VERTEX_POSITION _COVERAGE_OVERLAY_NORMAL_VECTOR
#pragma shader_feature_local _ _ENABLE_COVERAGE
#pragma shader_feature_local _ _BLEND_NORMALS

#if defined(_COVERAGE_OVERLAY_VERTEX_POSITION) && defined(_ENABLE_COVERAGE) && defined(_BLEND_NORMALS)
#define KEYWORD_PERMUTATION_0
#elif defined(_COVERAGE_OVERLAY_VERTEX_POSITION) && defined(_ENABLE_COVERAGE)
#define KEYWORD_PERMUTATION_1
#elif defined(_COVERAGE_OVERLAY_VERTEX_POSITION) && defined(_BLEND_NORMALS)
#define KEYWORD_PERMUTATION_2
#elif defined(_COVERAGE_OVERLAY_VERTEX_POSITION)
#define KEYWORD_PERMUTATION_3
#elif defined(_COVERAGE_OVERLAY_NORMAL_VECTOR) && defined(_ENABLE_COVERAGE) && defined(_BLEND_NORMALS)
#define KEYWORD_PERMUTATION_4
#elif defined(_COVERAGE_OVERLAY_NORMAL_VECTOR) && defined(_ENABLE_COVERAGE)
#define KEYWORD_PERMUTATION_5
#elif defined(_COVERAGE_OVERLAY_NORMAL_VECTOR) && defined(_BLEND_NORMALS)
#define KEYWORD_PERMUTATION_6
#else
#define KEYWORD_PERMUTATION_7
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
#define VARYINGS_NEED_POSITION_WS
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
float3 positionWS;
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
float3 WorldSpacePosition;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
};
struct PackedVaryings
{
float3 completionPositionWS : INTERP1;
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
output.completionPositionWS = input.positionWS;
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
output.positionWS = input.completionPositionWS;
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
float _EnvironmentReactionBufferSupported, _EnvironmentReactionIndex;
float4 _EnvState0, _EnvState1;
float _FuelCoverage, _AllowFuelCoverage;
float4 _WetTint, _CharTint, _FireColor;
float4 _EnvironmentCellBounds;
float _UseEnvironmentCells;

float4 _Coverage_Base_Map_TexelSize;
float4 _Base_Map_TexelSize;
float _Contrast;
float _Fade;
float4 _Normal_Map_TexelSize;
float4 _Coverage_Normal_Map_TexelSize;
float _Normal_Strength;
float _Coverage_Normal_Strength;
float _Smoothness;
float _Coverage_Smoothness;
float4 _Color;
float _Base_Map_Scale;
float _Level;
float4 _Coverage_Mask_TexelSize;
float _Coverage_Mask_Scale;
float4 _Coverage_Color;
float _Coverage_Map_Scale;
float4 _Metallic_Map_TexelSize;
float4 _Coverage_Metallic_Map_TexelSize;
float _Metallic;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Coverage_Base_Map);
SAMPLER(sampler_Coverage_Base_Map);
TEXTURE2D(_Base_Map);
SAMPLER(sampler_Base_Map);
TEXTURE2D(_Normal_Map);
SAMPLER(sampler_Normal_Map);
TEXTURE2D(_Coverage_Normal_Map);
SAMPLER(sampler_Coverage_Normal_Map);
TEXTURE2D(_Coverage_Mask);
SAMPLER(sampler_Coverage_Mask);
TEXTURE2D(_Metallic_Map);
SAMPLER(sampler_Metallic_Map);
TEXTURE2D(_Coverage_Metallic_Map);
SAMPLER(sampler_Coverage_Metallic_Map);

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
surface.Alpha = float(1);
surface.AlphaClipThreshold = float(0.5);
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
output.WorldSpacePosition = input.positionWS;

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
Cull [_Cull]
ZTest LEqual
ZWrite On
ColorMask RG

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM
#pragma multi_compile _ _ENVIRONMENT_REACTION_BUFFER
#pragma target 4.5 _ENVIRONMENT_REACTION_BUFFER


// Pragmas
#pragma target 3.5
#pragma multi_compile_instancing
#pragma vertex vert
#pragma fragment frag

// Keywords
#pragma shader_feature_local_fragment _ _ALPHATEST_ON
#pragma shader_feature_local _COVERAGE_OVERLAY_VERTEX_POSITION _COVERAGE_OVERLAY_NORMAL_VECTOR
#pragma shader_feature_local _ _ENABLE_COVERAGE
#pragma shader_feature_local _ _BLEND_NORMALS

#if defined(_COVERAGE_OVERLAY_VERTEX_POSITION) && defined(_ENABLE_COVERAGE) && defined(_BLEND_NORMALS)
#define KEYWORD_PERMUTATION_0
#elif defined(_COVERAGE_OVERLAY_VERTEX_POSITION) && defined(_ENABLE_COVERAGE)
#define KEYWORD_PERMUTATION_1
#elif defined(_COVERAGE_OVERLAY_VERTEX_POSITION) && defined(_BLEND_NORMALS)
#define KEYWORD_PERMUTATION_2
#elif defined(_COVERAGE_OVERLAY_VERTEX_POSITION)
#define KEYWORD_PERMUTATION_3
#elif defined(_COVERAGE_OVERLAY_NORMAL_VECTOR) && defined(_ENABLE_COVERAGE) && defined(_BLEND_NORMALS)
#define KEYWORD_PERMUTATION_4
#elif defined(_COVERAGE_OVERLAY_NORMAL_VECTOR) && defined(_ENABLE_COVERAGE)
#define KEYWORD_PERMUTATION_5
#elif defined(_COVERAGE_OVERLAY_NORMAL_VECTOR) && defined(_BLEND_NORMALS)
#define KEYWORD_PERMUTATION_6
#else
#define KEYWORD_PERMUTATION_7
#endif


// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define VARYINGS_NEED_POSITION_WS
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
float3 positionWS;
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
float3 WorldSpacePosition;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpacePosition;
};
struct PackedVaryings
{
float3 completionPositionWS : INTERP0;
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
output.completionPositionWS = input.positionWS;
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
output.positionWS = input.completionPositionWS;
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
float _EnvironmentReactionBufferSupported, _EnvironmentReactionIndex;
float4 _EnvState0, _EnvState1;
float _FuelCoverage, _AllowFuelCoverage;
float4 _WetTint, _CharTint, _FireColor;
float4 _EnvironmentCellBounds;
float _UseEnvironmentCells;

float4 _Coverage_Base_Map_TexelSize;
float4 _Base_Map_TexelSize;
float _Contrast;
float _Fade;
float4 _Normal_Map_TexelSize;
float4 _Coverage_Normal_Map_TexelSize;
float _Normal_Strength;
float _Coverage_Normal_Strength;
float _Smoothness;
float _Coverage_Smoothness;
float4 _Color;
float _Base_Map_Scale;
float _Level;
float4 _Coverage_Mask_TexelSize;
float _Coverage_Mask_Scale;
float4 _Coverage_Color;
float _Coverage_Map_Scale;
float4 _Metallic_Map_TexelSize;
float4 _Coverage_Metallic_Map_TexelSize;
float _Metallic;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Coverage_Base_Map);
SAMPLER(sampler_Coverage_Base_Map);
TEXTURE2D(_Base_Map);
SAMPLER(sampler_Base_Map);
TEXTURE2D(_Normal_Map);
SAMPLER(sampler_Normal_Map);
TEXTURE2D(_Coverage_Normal_Map);
SAMPLER(sampler_Coverage_Normal_Map);
TEXTURE2D(_Coverage_Mask);
SAMPLER(sampler_Coverage_Mask);
TEXTURE2D(_Metallic_Map);
SAMPLER(sampler_Metallic_Map);
TEXTURE2D(_Coverage_Metallic_Map);
SAMPLER(sampler_Coverage_Metallic_Map);

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
surface.Alpha = float(1);
surface.AlphaClipThreshold = float(0.5);
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
output.WorldSpacePosition = input.positionWS;

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
Cull [_Cull]
ZTest LEqual
ZWrite On
ColorMask R

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM
#pragma multi_compile _ _ENVIRONMENT_REACTION_BUFFER
#pragma target 4.5 _ENVIRONMENT_REACTION_BUFFER


// Pragmas
#pragma target 2.0
#pragma multi_compile_instancing
#pragma vertex vert
#pragma fragment frag

// Keywords
#pragma shader_feature_local_fragment _ _ALPHATEST_ON
#pragma shader_feature_local _COVERAGE_OVERLAY_VERTEX_POSITION _COVERAGE_OVERLAY_NORMAL_VECTOR
#pragma shader_feature_local _ _ENABLE_COVERAGE
#pragma shader_feature_local _ _BLEND_NORMALS

#if defined(_COVERAGE_OVERLAY_VERTEX_POSITION) && defined(_ENABLE_COVERAGE) && defined(_BLEND_NORMALS)
#define KEYWORD_PERMUTATION_0
#elif defined(_COVERAGE_OVERLAY_VERTEX_POSITION) && defined(_ENABLE_COVERAGE)
#define KEYWORD_PERMUTATION_1
#elif defined(_COVERAGE_OVERLAY_VERTEX_POSITION) && defined(_BLEND_NORMALS)
#define KEYWORD_PERMUTATION_2
#elif defined(_COVERAGE_OVERLAY_VERTEX_POSITION)
#define KEYWORD_PERMUTATION_3
#elif defined(_COVERAGE_OVERLAY_NORMAL_VECTOR) && defined(_ENABLE_COVERAGE) && defined(_BLEND_NORMALS)
#define KEYWORD_PERMUTATION_4
#elif defined(_COVERAGE_OVERLAY_NORMAL_VECTOR) && defined(_ENABLE_COVERAGE)
#define KEYWORD_PERMUTATION_5
#elif defined(_COVERAGE_OVERLAY_NORMAL_VECTOR) && defined(_BLEND_NORMALS)
#define KEYWORD_PERMUTATION_6
#else
#define KEYWORD_PERMUTATION_7
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
#define VARYINGS_NEED_POSITION_WS
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
float3 positionWS;
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
float3 WorldSpacePosition;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
};
struct PackedVaryings
{
float3 completionPositionWS : INTERP0;
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
output.completionPositionWS = input.positionWS;
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
output.positionWS = input.completionPositionWS;
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
float _EnvironmentReactionBufferSupported, _EnvironmentReactionIndex;
float4 _EnvState0, _EnvState1;
float _FuelCoverage, _AllowFuelCoverage;
float4 _WetTint, _CharTint, _FireColor;
float4 _EnvironmentCellBounds;
float _UseEnvironmentCells;

float4 _Coverage_Base_Map_TexelSize;
float4 _Base_Map_TexelSize;
float _Contrast;
float _Fade;
float4 _Normal_Map_TexelSize;
float4 _Coverage_Normal_Map_TexelSize;
float _Normal_Strength;
float _Coverage_Normal_Strength;
float _Smoothness;
float _Coverage_Smoothness;
float4 _Color;
float _Base_Map_Scale;
float _Level;
float4 _Coverage_Mask_TexelSize;
float _Coverage_Mask_Scale;
float4 _Coverage_Color;
float _Coverage_Map_Scale;
float4 _Metallic_Map_TexelSize;
float4 _Coverage_Metallic_Map_TexelSize;
float _Metallic;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Coverage_Base_Map);
SAMPLER(sampler_Coverage_Base_Map);
TEXTURE2D(_Base_Map);
SAMPLER(sampler_Base_Map);
TEXTURE2D(_Normal_Map);
SAMPLER(sampler_Normal_Map);
TEXTURE2D(_Coverage_Normal_Map);
SAMPLER(sampler_Coverage_Normal_Map);
TEXTURE2D(_Coverage_Mask);
SAMPLER(sampler_Coverage_Mask);
TEXTURE2D(_Metallic_Map);
SAMPLER(sampler_Metallic_Map);
TEXTURE2D(_Coverage_Metallic_Map);
SAMPLER(sampler_Coverage_Metallic_Map);

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
surface.Alpha = float(1);
surface.AlphaClipThreshold = float(0.5);
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
output.WorldSpacePosition = input.positionWS;

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
Cull [_Cull]
ZTest LEqual
ZWrite On

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM
#pragma multi_compile _ _ENVIRONMENT_REACTION_BUFFER
#pragma target 4.5 _ENVIRONMENT_REACTION_BUFFER


// Pragmas
#pragma target 2.0
#pragma multi_compile_instancing
#pragma vertex vert
#pragma fragment frag

// Keywords
#pragma shader_feature_local_fragment _ _ALPHATEST_ON
#pragma shader_feature_local _COVERAGE_OVERLAY_VERTEX_POSITION _COVERAGE_OVERLAY_NORMAL_VECTOR
#pragma shader_feature_local _ _ENABLE_COVERAGE
#pragma shader_feature_local _ _BLEND_NORMALS

#if defined(_COVERAGE_OVERLAY_VERTEX_POSITION) && defined(_ENABLE_COVERAGE) && defined(_BLEND_NORMALS)
#define KEYWORD_PERMUTATION_0
#elif defined(_COVERAGE_OVERLAY_VERTEX_POSITION) && defined(_ENABLE_COVERAGE)
#define KEYWORD_PERMUTATION_1
#elif defined(_COVERAGE_OVERLAY_VERTEX_POSITION) && defined(_BLEND_NORMALS)
#define KEYWORD_PERMUTATION_2
#elif defined(_COVERAGE_OVERLAY_VERTEX_POSITION)
#define KEYWORD_PERMUTATION_3
#elif defined(_COVERAGE_OVERLAY_NORMAL_VECTOR) && defined(_ENABLE_COVERAGE) && defined(_BLEND_NORMALS)
#define KEYWORD_PERMUTATION_4
#elif defined(_COVERAGE_OVERLAY_NORMAL_VECTOR) && defined(_ENABLE_COVERAGE)
#define KEYWORD_PERMUTATION_5
#elif defined(_COVERAGE_OVERLAY_NORMAL_VECTOR) && defined(_BLEND_NORMALS)
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
float3 WorldSpacePosition;
 float3 WorldSpaceNormal;
 float3 TangentSpaceNormal;
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
float _EnvironmentReactionBufferSupported, _EnvironmentReactionIndex;
float4 _EnvState0, _EnvState1;
float _FuelCoverage, _AllowFuelCoverage;
float4 _WetTint, _CharTint, _FireColor;
float4 _EnvironmentCellBounds;
float _UseEnvironmentCells;

float4 _Coverage_Base_Map_TexelSize;
float4 _Base_Map_TexelSize;
float _Contrast;
float _Fade;
float4 _Normal_Map_TexelSize;
float4 _Coverage_Normal_Map_TexelSize;
float _Normal_Strength;
float _Coverage_Normal_Strength;
float _Smoothness;
float _Coverage_Smoothness;
float4 _Color;
float _Base_Map_Scale;
float _Level;
float4 _Coverage_Mask_TexelSize;
float _Coverage_Mask_Scale;
float4 _Coverage_Color;
float _Coverage_Map_Scale;
float4 _Metallic_Map_TexelSize;
float4 _Coverage_Metallic_Map_TexelSize;
float _Metallic;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Coverage_Base_Map);
SAMPLER(sampler_Coverage_Base_Map);
TEXTURE2D(_Base_Map);
SAMPLER(sampler_Base_Map);
TEXTURE2D(_Normal_Map);
SAMPLER(sampler_Normal_Map);
TEXTURE2D(_Coverage_Normal_Map);
SAMPLER(sampler_Coverage_Normal_Map);
TEXTURE2D(_Coverage_Mask);
SAMPLER(sampler_Coverage_Mask);
TEXTURE2D(_Metallic_Map);
SAMPLER(sampler_Metallic_Map);
TEXTURE2D(_Coverage_Metallic_Map);
SAMPLER(sampler_Coverage_Metallic_Map);

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

void Unity_NormalStrength_float(float3 In, float Strength, out float3 Out)
{
    Out = float3(In.rg * Strength, lerp(1, In.b, saturate(Strength)));
}

void Unity_NormalBlend_float(float3 A, float3 B, out float3 Out)
{
    Out = SafeNormalize(float3(A.rg + B.rg, A.b * B.b));
}

void Unity_Negate_float(float In, out float Out)
{
    Out = -1 * In;
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Power_float(float A, float B, out float Out)
{
    Out = pow(A, B);
}

void Unity_Contrast_float(float3 In, float Contrast, out float3 Out)
{
    float midpoint = pow(0.5, 2.2);
    Out =  (In - midpoint) * Contrast + midpoint;
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Saturate_float3(float3 In, out float3 Out)
{
    Out = saturate(In);
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
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
UnityTexture2D _Property_6fa683e6dce24ba8a1a7b81715c96867_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Normal_Map, sampler_Normal_Map, _Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_837a954a3e08436195bd267068cb4b1a_Out_0_Float = _Base_Map_Scale;
float2 _TilingAndOffset_96e68e44c54540839992559f96312049_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, (_Property_837a954a3e08436195bd267068cb4b1a_Out_0_Float.xx), float2 (0, 0), _TilingAndOffset_96e68e44c54540839992559f96312049_Out_3_Vector2);
float4 _SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_6fa683e6dce24ba8a1a7b81715c96867_Out_0_Texture2D.tex, _Property_6fa683e6dce24ba8a1a7b81715c96867_Out_0_Texture2D.samplerstate, _Property_6fa683e6dce24ba8a1a7b81715c96867_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_96e68e44c54540839992559f96312049_Out_3_Vector2) );
if (_Property_6fa683e6dce24ba8a1a7b81715c96867_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4, _Property_6fa683e6dce24ba8a1a7b81715c96867_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4);
float _SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_R_4_Float = _SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4.r;
float _SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_G_5_Float = _SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4.g;
float _SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_B_6_Float = _SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4.b;
float _SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_A_7_Float = _SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4.a;
float _Property_c2af067f0d3e4d07b9aed4dfa65bc2f1_Out_0_Float = _Normal_Strength;
float3 _NormalStrength_5dc2d21824794b5da9897819e8ace0ee_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4.xyz), _Property_c2af067f0d3e4d07b9aed4dfa65bc2f1_Out_0_Float, _NormalStrength_5dc2d21824794b5da9897819e8ace0ee_Out_2_Vector3);
UnityTexture2D _Property_667d6ae9b714402ba1c363d17875b673_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Coverage_Normal_Map, sampler_Coverage_Normal_Map, _Coverage_Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_723c2d27d99b43b9a9ddec036c755090_Out_0_Float = _Coverage_Map_Scale;
float2 _TilingAndOffset_575abb976cd44768aeb58d322379cd59_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, (_Property_723c2d27d99b43b9a9ddec036c755090_Out_0_Float.xx), float2 (0, 0), _TilingAndOffset_575abb976cd44768aeb58d322379cd59_Out_3_Vector2);
float4 _SampleTexture2D_cfbdfb6f203f43b09e26762bf514022a_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_667d6ae9b714402ba1c363d17875b673_Out_0_Texture2D.tex, _Property_667d6ae9b714402ba1c363d17875b673_Out_0_Texture2D.samplerstate, _Property_667d6ae9b714402ba1c363d17875b673_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_575abb976cd44768aeb58d322379cd59_Out_3_Vector2) );
if (_Property_667d6ae9b714402ba1c363d17875b673_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_cfbdfb6f203f43b09e26762bf514022a_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_cfbdfb6f203f43b09e26762bf514022a_RGBA_0_Vector4, _Property_667d6ae9b714402ba1c363d17875b673_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_cfbdfb6f203f43b09e26762bf514022a_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_cfbdfb6f203f43b09e26762bf514022a_RGBA_0_Vector4);
float _SampleTexture2D_cfbdfb6f203f43b09e26762bf514022a_R_4_Float = _SampleTexture2D_cfbdfb6f203f43b09e26762bf514022a_RGBA_0_Vector4.r;
float _SampleTexture2D_cfbdfb6f203f43b09e26762bf514022a_G_5_Float = _SampleTexture2D_cfbdfb6f203f43b09e26762bf514022a_RGBA_0_Vector4.g;
float _SampleTexture2D_cfbdfb6f203f43b09e26762bf514022a_B_6_Float = _SampleTexture2D_cfbdfb6f203f43b09e26762bf514022a_RGBA_0_Vector4.b;
float _SampleTexture2D_cfbdfb6f203f43b09e26762bf514022a_A_7_Float = _SampleTexture2D_cfbdfb6f203f43b09e26762bf514022a_RGBA_0_Vector4.a;
float _Property_2459308922da4207b1e08d83995283f8_Out_0_Float = _Coverage_Normal_Strength;
float3 _NormalStrength_fb236b808ff24064af2bdfee4b5a5210_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_cfbdfb6f203f43b09e26762bf514022a_RGBA_0_Vector4.xyz), _Property_2459308922da4207b1e08d83995283f8_Out_0_Float, _NormalStrength_fb236b808ff24064af2bdfee4b5a5210_Out_2_Vector3);
float3 _NormalBlend_447b28fead15487ab5e4f6beee537fa1_Out_2_Vector3;
Unity_NormalBlend_float(_NormalStrength_5dc2d21824794b5da9897819e8ace0ee_Out_2_Vector3, _NormalStrength_fb236b808ff24064af2bdfee4b5a5210_Out_2_Vector3, _NormalBlend_447b28fead15487ab5e4f6beee537fa1_Out_2_Vector3);
#if defined(_BLEND_NORMALS)
float3 _BlendNormals_3488543c00894b85be4235ce5144518e_Out_0_Vector3 = _NormalBlend_447b28fead15487ab5e4f6beee537fa1_Out_2_Vector3;
#else
float3 _BlendNormals_3488543c00894b85be4235ce5144518e_Out_0_Vector3 = _NormalStrength_fb236b808ff24064af2bdfee4b5a5210_Out_2_Vector3;
#endif
UnityTexture2D _Property_f89138a4a5294f5eacb84b4409275065_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Coverage_Mask, sampler_Coverage_Mask, _Coverage_Mask_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_25a54da52bb3450c8dd76359ada2a358_Out_0_Float = _Coverage_Mask_Scale;
float2 _TilingAndOffset_e8100e9136c347e2a346b1af40d87a52_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, (_Property_25a54da52bb3450c8dd76359ada2a358_Out_0_Float.xx), float2 (0, 0), _TilingAndOffset_e8100e9136c347e2a346b1af40d87a52_Out_3_Vector2);
float4 _SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_f89138a4a5294f5eacb84b4409275065_Out_0_Texture2D.tex, _Property_f89138a4a5294f5eacb84b4409275065_Out_0_Texture2D.samplerstate, _Property_f89138a4a5294f5eacb84b4409275065_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_e8100e9136c347e2a346b1af40d87a52_Out_3_Vector2) );
if (_Property_f89138a4a5294f5eacb84b4409275065_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_RGBA_0_Vector4, _Property_f89138a4a5294f5eacb84b4409275065_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_R_4_Float = _SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_RGBA_0_Vector4.r;
float _SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_G_5_Float = _SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_RGBA_0_Vector4.g;
float _SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_B_6_Float = _SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_RGBA_0_Vector4.b;
float _SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_A_7_Float = _SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_RGBA_0_Vector4.a;
float _Property_d8db892f807f4cdbb860c5f763c37576_Out_0_Float = _Level;
float _Split_dddb7fd39dd142f6a79faaf42e2f31fa_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_dddb7fd39dd142f6a79faaf42e2f31fa_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_dddb7fd39dd142f6a79faaf42e2f31fa_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_dddb7fd39dd142f6a79faaf42e2f31fa_A_4_Float = 0;
float _Negate_5d8cf946d6a44b0688a978f991ce300e_Out_1_Float;
Unity_Negate_float(_Split_dddb7fd39dd142f6a79faaf42e2f31fa_G_2_Float, _Negate_5d8cf946d6a44b0688a978f991ce300e_Out_1_Float);
float3 _Transform_f92b73ad085c4c319bc399803f810059_Out_1_Vector3;
{
float3x3 tangentTransform = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
_Transform_f92b73ad085c4c319bc399803f810059_Out_1_Vector3 = TransformTangentToWorldDir((_SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4.xyz).xyz, tangentTransform, true).xyz;
}
#if defined(_COVERAGE_OVERLAY_VERTEX_POSITION)
float3 _CoverageOverlay_6c350da1b1bc48d8bd322a639af8a114_Out_0_Vector3 = (_Negate_5d8cf946d6a44b0688a978f991ce300e_Out_1_Float.xxx);
#else
float3 _CoverageOverlay_6c350da1b1bc48d8bd322a639af8a114_Out_0_Vector3 = _Transform_f92b73ad085c4c319bc399803f810059_Out_1_Vector3;
#endif
float _Split_5e81b1d12c8f426d99b755defa3f1129_R_1_Float = _CoverageOverlay_6c350da1b1bc48d8bd322a639af8a114_Out_0_Vector3[0];
float _Split_5e81b1d12c8f426d99b755defa3f1129_G_2_Float = _CoverageOverlay_6c350da1b1bc48d8bd322a639af8a114_Out_0_Vector3[1];
float _Split_5e81b1d12c8f426d99b755defa3f1129_B_3_Float = _CoverageOverlay_6c350da1b1bc48d8bd322a639af8a114_Out_0_Vector3[2];
float _Split_5e81b1d12c8f426d99b755defa3f1129_A_4_Float = 0;
float _Add_42c3350036d24612b752658957f0e996_Out_2_Float;
Unity_Add_float(_Property_d8db892f807f4cdbb860c5f763c37576_Out_0_Float, _Split_5e81b1d12c8f426d99b755defa3f1129_G_2_Float, _Add_42c3350036d24612b752658957f0e996_Out_2_Float);
float _Property_0b2c6c92abcd44f6ac12c213b29b4591_Out_0_Float = _Fade;
float _Multiply_e63d550088ac44d9aef2a43850ee5aa9_Out_2_Float;
Unity_Multiply_float_float(_Property_0b2c6c92abcd44f6ac12c213b29b4591_Out_0_Float, 5, _Multiply_e63d550088ac44d9aef2a43850ee5aa9_Out_2_Float);
float _Power_78c8b863b05d4425b22c06b245577c57_Out_2_Float;
Unity_Power_float(_Add_42c3350036d24612b752658957f0e996_Out_2_Float, _Multiply_e63d550088ac44d9aef2a43850ee5aa9_Out_2_Float, _Power_78c8b863b05d4425b22c06b245577c57_Out_2_Float);
float _Property_4a3dba7d791f4ae8afbe0ba166c8d15d_Out_0_Float = _Contrast;
float _Multiply_df64950968a547a495e68042783a7510_Out_2_Float;
Unity_Multiply_float_float(_Property_4a3dba7d791f4ae8afbe0ba166c8d15d_Out_0_Float, 5, _Multiply_df64950968a547a495e68042783a7510_Out_2_Float);
float3 _Contrast_d9aab63f6f4e464aaab650dfe18fa9c8_Out_2_Vector3;
Unity_Contrast_float((_Power_78c8b863b05d4425b22c06b245577c57_Out_2_Float.xxx), _Multiply_df64950968a547a495e68042783a7510_Out_2_Float, _Contrast_d9aab63f6f4e464aaab650dfe18fa9c8_Out_2_Vector3);
float3 _Multiply_cc22e2488cfa4aabab92d688a9b358a8_Out_2_Vector3;
Unity_Multiply_float3_float3((_SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_RGBA_0_Vector4.xyz), _Contrast_d9aab63f6f4e464aaab650dfe18fa9c8_Out_2_Vector3, _Multiply_cc22e2488cfa4aabab92d688a9b358a8_Out_2_Vector3);
#if defined(_ENABLE_COVERAGE)
float3 _EnableCoverage_8bf33e8bc98a4d1285a2c507af050ad2_Out_0_Vector3 = _Multiply_cc22e2488cfa4aabab92d688a9b358a8_Out_2_Vector3;
#else
float3 _EnableCoverage_8bf33e8bc98a4d1285a2c507af050ad2_Out_0_Vector3 = float3(0, 0, 0);
#endif
float3 _Saturate_9537e4d4f5274dbf9b1a191c2de9a0bd_Out_1_Vector3;
Unity_Saturate_float3(_EnableCoverage_8bf33e8bc98a4d1285a2c507af050ad2_Out_0_Vector3, _Saturate_9537e4d4f5274dbf9b1a191c2de9a0bd_Out_1_Vector3);
float3 _Lerp_16fa4c8d40ce4aed9cdf811577b4af07_Out_3_Vector3;
Unity_Lerp_float3(_NormalStrength_5dc2d21824794b5da9897819e8ace0ee_Out_2_Vector3, _BlendNormals_3488543c00894b85be4235ce5144518e_Out_0_Vector3, _Saturate_9537e4d4f5274dbf9b1a191c2de9a0bd_Out_1_Vector3, _Lerp_16fa4c8d40ce4aed9cdf811577b4af07_Out_3_Vector3);
#if defined(_ENABLE_COVERAGE)
float3 _EnableCoverage_e950d03b1e3b47748347c719515869e0_Out_0_Vector3 = _Lerp_16fa4c8d40ce4aed9cdf811577b4af07_Out_3_Vector3;
#else
float3 _EnableCoverage_e950d03b1e3b47748347c719515869e0_Out_0_Vector3 = _NormalStrength_5dc2d21824794b5da9897819e8ace0ee_Out_2_Vector3;
#endif
surface.NormalTS = _EnableCoverage_e950d03b1e3b47748347c719515869e0_Out_0_Vector3;
surface.Alpha = float(1);
surface.AlphaClipThreshold = float(0.5);
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
output.WorldSpacePosition = input.positionWS;

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
#pragma multi_compile _ _ENVIRONMENT_REACTION_BUFFER
#pragma target 4.5 _ENVIRONMENT_REACTION_BUFFER


// Pragmas
#pragma target 2.0
#pragma vertex vert
#pragma fragment frag

// Keywords
#pragma shader_feature _ EDITOR_VISUALIZATION
#pragma shader_feature_local_fragment _ _ALPHATEST_ON
#pragma shader_feature_local _COVERAGE_OVERLAY_VERTEX_POSITION _COVERAGE_OVERLAY_NORMAL_VECTOR
#pragma shader_feature_local _ _ENABLE_COVERAGE
#pragma shader_feature_local _ _BLEND_NORMALS

#if defined(_COVERAGE_OVERLAY_VERTEX_POSITION) && defined(_ENABLE_COVERAGE) && defined(_BLEND_NORMALS)
#define KEYWORD_PERMUTATION_0
#elif defined(_COVERAGE_OVERLAY_VERTEX_POSITION) && defined(_ENABLE_COVERAGE)
#define KEYWORD_PERMUTATION_1
#elif defined(_COVERAGE_OVERLAY_VERTEX_POSITION) && defined(_BLEND_NORMALS)
#define KEYWORD_PERMUTATION_2
#elif defined(_COVERAGE_OVERLAY_VERTEX_POSITION)
#define KEYWORD_PERMUTATION_3
#elif defined(_COVERAGE_OVERLAY_NORMAL_VECTOR) && defined(_ENABLE_COVERAGE) && defined(_BLEND_NORMALS)
#define KEYWORD_PERMUTATION_4
#elif defined(_COVERAGE_OVERLAY_NORMAL_VECTOR) && defined(_ENABLE_COVERAGE)
#define KEYWORD_PERMUTATION_5
#elif defined(_COVERAGE_OVERLAY_NORMAL_VECTOR) && defined(_BLEND_NORMALS)
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
float3 WorldSpacePosition;
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
float _EnvironmentReactionBufferSupported, _EnvironmentReactionIndex;
float4 _EnvState0, _EnvState1;
float _FuelCoverage, _AllowFuelCoverage;
float4 _WetTint, _CharTint, _FireColor;
float4 _EnvironmentCellBounds;
float _UseEnvironmentCells;

float4 _Coverage_Base_Map_TexelSize;
float4 _Base_Map_TexelSize;
float _Contrast;
float _Fade;
float4 _Normal_Map_TexelSize;
float4 _Coverage_Normal_Map_TexelSize;
float _Normal_Strength;
float _Coverage_Normal_Strength;
float _Smoothness;
float _Coverage_Smoothness;
float4 _Color;
float _Base_Map_Scale;
float _Level;
float4 _Coverage_Mask_TexelSize;
float _Coverage_Mask_Scale;
float4 _Coverage_Color;
float _Coverage_Map_Scale;
float4 _Metallic_Map_TexelSize;
float4 _Coverage_Metallic_Map_TexelSize;
float _Metallic;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Coverage_Base_Map);
SAMPLER(sampler_Coverage_Base_Map);
TEXTURE2D(_Base_Map);
SAMPLER(sampler_Base_Map);
TEXTURE2D(_Normal_Map);
SAMPLER(sampler_Normal_Map);
TEXTURE2D(_Coverage_Normal_Map);
SAMPLER(sampler_Coverage_Normal_Map);
TEXTURE2D(_Coverage_Mask);
SAMPLER(sampler_Coverage_Mask);
TEXTURE2D(_Metallic_Map);
SAMPLER(sampler_Metallic_Map);
TEXTURE2D(_Coverage_Metallic_Map);
SAMPLER(sampler_Coverage_Metallic_Map);

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

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

void Unity_Negate_float(float In, out float Out)
{
    Out = -1 * In;
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Power_float(float A, float B, out float Out)
{
    Out = pow(A, B);
}

void Unity_Contrast_float(float3 In, float Contrast, out float3 Out)
{
    float midpoint = pow(0.5, 2.2);
    Out =  (In - midpoint) * Contrast + midpoint;
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Saturate_float3(float3 In, out float3 Out)
{
    Out = saturate(In);
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
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
UnityTexture2D _Property_962e37e0ac5244c4a3ea6b170425e6e4_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Base_Map, sampler_Base_Map, _Base_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_837a954a3e08436195bd267068cb4b1a_Out_0_Float = _Base_Map_Scale;
float2 _TilingAndOffset_96e68e44c54540839992559f96312049_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, (_Property_837a954a3e08436195bd267068cb4b1a_Out_0_Float.xx), float2 (0, 0), _TilingAndOffset_96e68e44c54540839992559f96312049_Out_3_Vector2);
float4 _SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_962e37e0ac5244c4a3ea6b170425e6e4_Out_0_Texture2D.tex, _Property_962e37e0ac5244c4a3ea6b170425e6e4_Out_0_Texture2D.samplerstate, _Property_962e37e0ac5244c4a3ea6b170425e6e4_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_96e68e44c54540839992559f96312049_Out_3_Vector2) );
if (_Property_962e37e0ac5244c4a3ea6b170425e6e4_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_RGBA_0_Vector4, _Property_962e37e0ac5244c4a3ea6b170425e6e4_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_R_4_Float = _SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_RGBA_0_Vector4.r;
float _SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_G_5_Float = _SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_RGBA_0_Vector4.g;
float _SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_B_6_Float = _SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_RGBA_0_Vector4.b;
float _SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_A_7_Float = _SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_RGBA_0_Vector4.a;
float4 _Property_8bc5cedd8b5144aba79ab296bf97f4b8_Out_0_Vector4 = _Color;
float4 _Multiply_d62734e54c4a4b258cc66ff106c782ea_Out_2_Vector4;
Unity_Multiply_float4_float4(_SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_RGBA_0_Vector4, _Property_8bc5cedd8b5144aba79ab296bf97f4b8_Out_0_Vector4, _Multiply_d62734e54c4a4b258cc66ff106c782ea_Out_2_Vector4);
UnityTexture2D _Property_06c1669821ff42c3bf719660b6f40626_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Coverage_Base_Map, sampler_Coverage_Base_Map, _Coverage_Base_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_4fea0d4cff68413d90a1875fb9c320e7_Out_0_Float = _Coverage_Map_Scale;
float2 _TilingAndOffset_264e68c10eca41918924efa9fb9469af_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, (_Property_4fea0d4cff68413d90a1875fb9c320e7_Out_0_Float.xx), float2 (0, 0), _TilingAndOffset_264e68c10eca41918924efa9fb9469af_Out_3_Vector2);
float4 _SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_06c1669821ff42c3bf719660b6f40626_Out_0_Texture2D.tex, _Property_06c1669821ff42c3bf719660b6f40626_Out_0_Texture2D.samplerstate, _Property_06c1669821ff42c3bf719660b6f40626_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_264e68c10eca41918924efa9fb9469af_Out_3_Vector2) );
if (_Property_06c1669821ff42c3bf719660b6f40626_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_RGBA_0_Vector4, _Property_06c1669821ff42c3bf719660b6f40626_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_R_4_Float = _SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_RGBA_0_Vector4.r;
float _SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_G_5_Float = _SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_RGBA_0_Vector4.g;
float _SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_B_6_Float = _SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_RGBA_0_Vector4.b;
float _SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_A_7_Float = _SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_RGBA_0_Vector4.a;
float4 _Property_c8d8bceb4b8a46b589dd57d79ecd9bf9_Out_0_Vector4 = _Coverage_Color;
float4 _Multiply_548a83f07d1f491d858cdae4602c6b4d_Out_2_Vector4;
Unity_Multiply_float4_float4(_SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_RGBA_0_Vector4, _Property_c8d8bceb4b8a46b589dd57d79ecd9bf9_Out_0_Vector4, _Multiply_548a83f07d1f491d858cdae4602c6b4d_Out_2_Vector4);
UnityTexture2D _Property_f89138a4a5294f5eacb84b4409275065_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Coverage_Mask, sampler_Coverage_Mask, _Coverage_Mask_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_25a54da52bb3450c8dd76359ada2a358_Out_0_Float = _Coverage_Mask_Scale;
float2 _TilingAndOffset_e8100e9136c347e2a346b1af40d87a52_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, (_Property_25a54da52bb3450c8dd76359ada2a358_Out_0_Float.xx), float2 (0, 0), _TilingAndOffset_e8100e9136c347e2a346b1af40d87a52_Out_3_Vector2);
float4 _SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_f89138a4a5294f5eacb84b4409275065_Out_0_Texture2D.tex, _Property_f89138a4a5294f5eacb84b4409275065_Out_0_Texture2D.samplerstate, _Property_f89138a4a5294f5eacb84b4409275065_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_e8100e9136c347e2a346b1af40d87a52_Out_3_Vector2) );
if (_Property_f89138a4a5294f5eacb84b4409275065_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_RGBA_0_Vector4, _Property_f89138a4a5294f5eacb84b4409275065_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_R_4_Float = _SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_RGBA_0_Vector4.r;
float _SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_G_5_Float = _SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_RGBA_0_Vector4.g;
float _SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_B_6_Float = _SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_RGBA_0_Vector4.b;
float _SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_A_7_Float = _SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_RGBA_0_Vector4.a;
float _Property_d8db892f807f4cdbb860c5f763c37576_Out_0_Float = _Level;
float _Split_dddb7fd39dd142f6a79faaf42e2f31fa_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_dddb7fd39dd142f6a79faaf42e2f31fa_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_dddb7fd39dd142f6a79faaf42e2f31fa_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_dddb7fd39dd142f6a79faaf42e2f31fa_A_4_Float = 0;
float _Negate_5d8cf946d6a44b0688a978f991ce300e_Out_1_Float;
Unity_Negate_float(_Split_dddb7fd39dd142f6a79faaf42e2f31fa_G_2_Float, _Negate_5d8cf946d6a44b0688a978f991ce300e_Out_1_Float);
UnityTexture2D _Property_6fa683e6dce24ba8a1a7b81715c96867_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Normal_Map, sampler_Normal_Map, _Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_6fa683e6dce24ba8a1a7b81715c96867_Out_0_Texture2D.tex, _Property_6fa683e6dce24ba8a1a7b81715c96867_Out_0_Texture2D.samplerstate, _Property_6fa683e6dce24ba8a1a7b81715c96867_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_96e68e44c54540839992559f96312049_Out_3_Vector2) );
if (_Property_6fa683e6dce24ba8a1a7b81715c96867_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4, _Property_6fa683e6dce24ba8a1a7b81715c96867_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4);
float _SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_R_4_Float = _SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4.r;
float _SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_G_5_Float = _SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4.g;
float _SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_B_6_Float = _SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4.b;
float _SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_A_7_Float = _SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4.a;
float3 _Transform_f92b73ad085c4c319bc399803f810059_Out_1_Vector3;
{
float3x3 tangentTransform = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
_Transform_f92b73ad085c4c319bc399803f810059_Out_1_Vector3 = TransformTangentToWorldDir((_SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4.xyz).xyz, tangentTransform, true).xyz;
}
#if defined(_COVERAGE_OVERLAY_VERTEX_POSITION)
float3 _CoverageOverlay_6c350da1b1bc48d8bd322a639af8a114_Out_0_Vector3 = (_Negate_5d8cf946d6a44b0688a978f991ce300e_Out_1_Float.xxx);
#else
float3 _CoverageOverlay_6c350da1b1bc48d8bd322a639af8a114_Out_0_Vector3 = _Transform_f92b73ad085c4c319bc399803f810059_Out_1_Vector3;
#endif
float _Split_5e81b1d12c8f426d99b755defa3f1129_R_1_Float = _CoverageOverlay_6c350da1b1bc48d8bd322a639af8a114_Out_0_Vector3[0];
float _Split_5e81b1d12c8f426d99b755defa3f1129_G_2_Float = _CoverageOverlay_6c350da1b1bc48d8bd322a639af8a114_Out_0_Vector3[1];
float _Split_5e81b1d12c8f426d99b755defa3f1129_B_3_Float = _CoverageOverlay_6c350da1b1bc48d8bd322a639af8a114_Out_0_Vector3[2];
float _Split_5e81b1d12c8f426d99b755defa3f1129_A_4_Float = 0;
float _Add_42c3350036d24612b752658957f0e996_Out_2_Float;
Unity_Add_float(_Property_d8db892f807f4cdbb860c5f763c37576_Out_0_Float, _Split_5e81b1d12c8f426d99b755defa3f1129_G_2_Float, _Add_42c3350036d24612b752658957f0e996_Out_2_Float);
float _Property_0b2c6c92abcd44f6ac12c213b29b4591_Out_0_Float = _Fade;
float _Multiply_e63d550088ac44d9aef2a43850ee5aa9_Out_2_Float;
Unity_Multiply_float_float(_Property_0b2c6c92abcd44f6ac12c213b29b4591_Out_0_Float, 5, _Multiply_e63d550088ac44d9aef2a43850ee5aa9_Out_2_Float);
float _Power_78c8b863b05d4425b22c06b245577c57_Out_2_Float;
Unity_Power_float(_Add_42c3350036d24612b752658957f0e996_Out_2_Float, _Multiply_e63d550088ac44d9aef2a43850ee5aa9_Out_2_Float, _Power_78c8b863b05d4425b22c06b245577c57_Out_2_Float);
float _Property_4a3dba7d791f4ae8afbe0ba166c8d15d_Out_0_Float = _Contrast;
float _Multiply_df64950968a547a495e68042783a7510_Out_2_Float;
Unity_Multiply_float_float(_Property_4a3dba7d791f4ae8afbe0ba166c8d15d_Out_0_Float, 5, _Multiply_df64950968a547a495e68042783a7510_Out_2_Float);
float3 _Contrast_d9aab63f6f4e464aaab650dfe18fa9c8_Out_2_Vector3;
Unity_Contrast_float((_Power_78c8b863b05d4425b22c06b245577c57_Out_2_Float.xxx), _Multiply_df64950968a547a495e68042783a7510_Out_2_Float, _Contrast_d9aab63f6f4e464aaab650dfe18fa9c8_Out_2_Vector3);
float3 _Multiply_cc22e2488cfa4aabab92d688a9b358a8_Out_2_Vector3;
Unity_Multiply_float3_float3((_SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_RGBA_0_Vector4.xyz), _Contrast_d9aab63f6f4e464aaab650dfe18fa9c8_Out_2_Vector3, _Multiply_cc22e2488cfa4aabab92d688a9b358a8_Out_2_Vector3);
#if defined(_ENABLE_COVERAGE)
float3 _EnableCoverage_8bf33e8bc98a4d1285a2c507af050ad2_Out_0_Vector3 = _Multiply_cc22e2488cfa4aabab92d688a9b358a8_Out_2_Vector3;
#else
float3 _EnableCoverage_8bf33e8bc98a4d1285a2c507af050ad2_Out_0_Vector3 = float3(0, 0, 0);
#endif
float3 _Saturate_9537e4d4f5274dbf9b1a191c2de9a0bd_Out_1_Vector3;
Unity_Saturate_float3(_EnableCoverage_8bf33e8bc98a4d1285a2c507af050ad2_Out_0_Vector3, _Saturate_9537e4d4f5274dbf9b1a191c2de9a0bd_Out_1_Vector3);
float3 _Lerp_7bbb7508500f4123a0db757387918fb8_Out_3_Vector3;
Unity_Lerp_float3((_Multiply_d62734e54c4a4b258cc66ff106c782ea_Out_2_Vector4.xyz), (_Multiply_548a83f07d1f491d858cdae4602c6b4d_Out_2_Vector4.xyz), _Saturate_9537e4d4f5274dbf9b1a191c2de9a0bd_Out_1_Vector3, _Lerp_7bbb7508500f4123a0db757387918fb8_Out_3_Vector3);
#if defined(_ENABLE_COVERAGE)
float3 _EnableCoverage_64958d03a7354be9a2dc2affb31f53a4_Out_0_Vector3 = _Lerp_7bbb7508500f4123a0db757387918fb8_Out_3_Vector3;
#else
float3 _EnableCoverage_64958d03a7354be9a2dc2affb31f53a4_Out_0_Vector3 = (_Multiply_d62734e54c4a4b258cc66ff106c782ea_Out_2_Vector4.xyz);
#endif
surface.BaseColor = _EnableCoverage_64958d03a7354be9a2dc2affb31f53a4_Out_0_Vector3;
surface.Emission = float3(0, 0, 0);
surface.Alpha = float(1);
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
#pragma multi_compile _ _ENVIRONMENT_REACTION_BUFFER
#pragma target 4.5 _ENVIRONMENT_REACTION_BUFFER


// Pragmas
#pragma target 2.0
#pragma vertex vert
#pragma fragment frag

// Keywords
#pragma shader_feature_local_fragment _ _ALPHATEST_ON
#pragma shader_feature_local _COVERAGE_OVERLAY_VERTEX_POSITION _COVERAGE_OVERLAY_NORMAL_VECTOR
#pragma shader_feature_local _ _ENABLE_COVERAGE
#pragma shader_feature_local _ _BLEND_NORMALS

#if defined(_COVERAGE_OVERLAY_VERTEX_POSITION) && defined(_ENABLE_COVERAGE) && defined(_BLEND_NORMALS)
#define KEYWORD_PERMUTATION_0
#elif defined(_COVERAGE_OVERLAY_VERTEX_POSITION) && defined(_ENABLE_COVERAGE)
#define KEYWORD_PERMUTATION_1
#elif defined(_COVERAGE_OVERLAY_VERTEX_POSITION) && defined(_BLEND_NORMALS)
#define KEYWORD_PERMUTATION_2
#elif defined(_COVERAGE_OVERLAY_VERTEX_POSITION)
#define KEYWORD_PERMUTATION_3
#elif defined(_COVERAGE_OVERLAY_NORMAL_VECTOR) && defined(_ENABLE_COVERAGE) && defined(_BLEND_NORMALS)
#define KEYWORD_PERMUTATION_4
#elif defined(_COVERAGE_OVERLAY_NORMAL_VECTOR) && defined(_ENABLE_COVERAGE)
#define KEYWORD_PERMUTATION_5
#elif defined(_COVERAGE_OVERLAY_NORMAL_VECTOR) && defined(_BLEND_NORMALS)
#define KEYWORD_PERMUTATION_6
#else
#define KEYWORD_PERMUTATION_7
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
#define VARYINGS_NEED_POSITION_WS
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
float3 positionWS;
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
float3 WorldSpacePosition;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
};
struct PackedVaryings
{
float3 completionPositionWS : INTERP0;
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
output.completionPositionWS = input.positionWS;
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
output.positionWS = input.completionPositionWS;
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
float _EnvironmentReactionBufferSupported, _EnvironmentReactionIndex;
float4 _EnvState0, _EnvState1;
float _FuelCoverage, _AllowFuelCoverage;
float4 _WetTint, _CharTint, _FireColor;
float4 _EnvironmentCellBounds;
float _UseEnvironmentCells;

float4 _Coverage_Base_Map_TexelSize;
float4 _Base_Map_TexelSize;
float _Contrast;
float _Fade;
float4 _Normal_Map_TexelSize;
float4 _Coverage_Normal_Map_TexelSize;
float _Normal_Strength;
float _Coverage_Normal_Strength;
float _Smoothness;
float _Coverage_Smoothness;
float4 _Color;
float _Base_Map_Scale;
float _Level;
float4 _Coverage_Mask_TexelSize;
float _Coverage_Mask_Scale;
float4 _Coverage_Color;
float _Coverage_Map_Scale;
float4 _Metallic_Map_TexelSize;
float4 _Coverage_Metallic_Map_TexelSize;
float _Metallic;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Coverage_Base_Map);
SAMPLER(sampler_Coverage_Base_Map);
TEXTURE2D(_Base_Map);
SAMPLER(sampler_Base_Map);
TEXTURE2D(_Normal_Map);
SAMPLER(sampler_Normal_Map);
TEXTURE2D(_Coverage_Normal_Map);
SAMPLER(sampler_Coverage_Normal_Map);
TEXTURE2D(_Coverage_Mask);
SAMPLER(sampler_Coverage_Mask);
TEXTURE2D(_Metallic_Map);
SAMPLER(sampler_Metallic_Map);
TEXTURE2D(_Coverage_Metallic_Map);
SAMPLER(sampler_Coverage_Metallic_Map);

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
surface.Alpha = float(1);
surface.AlphaClipThreshold = float(0.5);
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
output.WorldSpacePosition = input.positionWS;

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
Cull [_Cull]

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM
#pragma multi_compile _ _ENVIRONMENT_REACTION_BUFFER
#pragma target 4.5 _ENVIRONMENT_REACTION_BUFFER


// Pragmas
#pragma target 2.0
#pragma vertex vert
#pragma fragment frag

// Keywords
#pragma shader_feature_local_fragment _ _ALPHATEST_ON
#pragma shader_feature_local _COVERAGE_OVERLAY_VERTEX_POSITION _COVERAGE_OVERLAY_NORMAL_VECTOR
#pragma shader_feature_local _ _ENABLE_COVERAGE
#pragma shader_feature_local _ _BLEND_NORMALS

#if defined(_COVERAGE_OVERLAY_VERTEX_POSITION) && defined(_ENABLE_COVERAGE) && defined(_BLEND_NORMALS)
#define KEYWORD_PERMUTATION_0
#elif defined(_COVERAGE_OVERLAY_VERTEX_POSITION) && defined(_ENABLE_COVERAGE)
#define KEYWORD_PERMUTATION_1
#elif defined(_COVERAGE_OVERLAY_VERTEX_POSITION) && defined(_BLEND_NORMALS)
#define KEYWORD_PERMUTATION_2
#elif defined(_COVERAGE_OVERLAY_VERTEX_POSITION)
#define KEYWORD_PERMUTATION_3
#elif defined(_COVERAGE_OVERLAY_NORMAL_VECTOR) && defined(_ENABLE_COVERAGE) && defined(_BLEND_NORMALS)
#define KEYWORD_PERMUTATION_4
#elif defined(_COVERAGE_OVERLAY_NORMAL_VECTOR) && defined(_ENABLE_COVERAGE)
#define KEYWORD_PERMUTATION_5
#elif defined(_COVERAGE_OVERLAY_NORMAL_VECTOR) && defined(_BLEND_NORMALS)
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
float3 WorldSpacePosition;
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
float _EnvironmentReactionBufferSupported, _EnvironmentReactionIndex;
float4 _EnvState0, _EnvState1;
float _FuelCoverage, _AllowFuelCoverage;
float4 _WetTint, _CharTint, _FireColor;
float4 _EnvironmentCellBounds;
float _UseEnvironmentCells;

float4 _Coverage_Base_Map_TexelSize;
float4 _Base_Map_TexelSize;
float _Contrast;
float _Fade;
float4 _Normal_Map_TexelSize;
float4 _Coverage_Normal_Map_TexelSize;
float _Normal_Strength;
float _Coverage_Normal_Strength;
float _Smoothness;
float _Coverage_Smoothness;
float4 _Color;
float _Base_Map_Scale;
float _Level;
float4 _Coverage_Mask_TexelSize;
float _Coverage_Mask_Scale;
float4 _Coverage_Color;
float _Coverage_Map_Scale;
float4 _Metallic_Map_TexelSize;
float4 _Coverage_Metallic_Map_TexelSize;
float _Metallic;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Coverage_Base_Map);
SAMPLER(sampler_Coverage_Base_Map);
TEXTURE2D(_Base_Map);
SAMPLER(sampler_Base_Map);
TEXTURE2D(_Normal_Map);
SAMPLER(sampler_Normal_Map);
TEXTURE2D(_Coverage_Normal_Map);
SAMPLER(sampler_Coverage_Normal_Map);
TEXTURE2D(_Coverage_Mask);
SAMPLER(sampler_Coverage_Mask);
TEXTURE2D(_Metallic_Map);
SAMPLER(sampler_Metallic_Map);
TEXTURE2D(_Coverage_Metallic_Map);
SAMPLER(sampler_Coverage_Metallic_Map);

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

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

void Unity_Negate_float(float In, out float Out)
{
    Out = -1 * In;
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Power_float(float A, float B, out float Out)
{
    Out = pow(A, B);
}

void Unity_Contrast_float(float3 In, float Contrast, out float3 Out)
{
    float midpoint = pow(0.5, 2.2);
    Out =  (In - midpoint) * Contrast + midpoint;
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Saturate_float3(float3 In, out float3 Out)
{
    Out = saturate(In);
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
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
UnityTexture2D _Property_962e37e0ac5244c4a3ea6b170425e6e4_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Base_Map, sampler_Base_Map, _Base_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_837a954a3e08436195bd267068cb4b1a_Out_0_Float = _Base_Map_Scale;
float2 _TilingAndOffset_96e68e44c54540839992559f96312049_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, (_Property_837a954a3e08436195bd267068cb4b1a_Out_0_Float.xx), float2 (0, 0), _TilingAndOffset_96e68e44c54540839992559f96312049_Out_3_Vector2);
float4 _SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_962e37e0ac5244c4a3ea6b170425e6e4_Out_0_Texture2D.tex, _Property_962e37e0ac5244c4a3ea6b170425e6e4_Out_0_Texture2D.samplerstate, _Property_962e37e0ac5244c4a3ea6b170425e6e4_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_96e68e44c54540839992559f96312049_Out_3_Vector2) );
if (_Property_962e37e0ac5244c4a3ea6b170425e6e4_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_RGBA_0_Vector4, _Property_962e37e0ac5244c4a3ea6b170425e6e4_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_R_4_Float = _SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_RGBA_0_Vector4.r;
float _SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_G_5_Float = _SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_RGBA_0_Vector4.g;
float _SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_B_6_Float = _SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_RGBA_0_Vector4.b;
float _SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_A_7_Float = _SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_RGBA_0_Vector4.a;
float4 _Property_8bc5cedd8b5144aba79ab296bf97f4b8_Out_0_Vector4 = _Color;
float4 _Multiply_d62734e54c4a4b258cc66ff106c782ea_Out_2_Vector4;
Unity_Multiply_float4_float4(_SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_RGBA_0_Vector4, _Property_8bc5cedd8b5144aba79ab296bf97f4b8_Out_0_Vector4, _Multiply_d62734e54c4a4b258cc66ff106c782ea_Out_2_Vector4);
UnityTexture2D _Property_06c1669821ff42c3bf719660b6f40626_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Coverage_Base_Map, sampler_Coverage_Base_Map, _Coverage_Base_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_4fea0d4cff68413d90a1875fb9c320e7_Out_0_Float = _Coverage_Map_Scale;
float2 _TilingAndOffset_264e68c10eca41918924efa9fb9469af_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, (_Property_4fea0d4cff68413d90a1875fb9c320e7_Out_0_Float.xx), float2 (0, 0), _TilingAndOffset_264e68c10eca41918924efa9fb9469af_Out_3_Vector2);
float4 _SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_06c1669821ff42c3bf719660b6f40626_Out_0_Texture2D.tex, _Property_06c1669821ff42c3bf719660b6f40626_Out_0_Texture2D.samplerstate, _Property_06c1669821ff42c3bf719660b6f40626_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_264e68c10eca41918924efa9fb9469af_Out_3_Vector2) );
if (_Property_06c1669821ff42c3bf719660b6f40626_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_RGBA_0_Vector4, _Property_06c1669821ff42c3bf719660b6f40626_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_R_4_Float = _SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_RGBA_0_Vector4.r;
float _SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_G_5_Float = _SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_RGBA_0_Vector4.g;
float _SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_B_6_Float = _SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_RGBA_0_Vector4.b;
float _SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_A_7_Float = _SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_RGBA_0_Vector4.a;
float4 _Property_c8d8bceb4b8a46b589dd57d79ecd9bf9_Out_0_Vector4 = _Coverage_Color;
float4 _Multiply_548a83f07d1f491d858cdae4602c6b4d_Out_2_Vector4;
Unity_Multiply_float4_float4(_SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_RGBA_0_Vector4, _Property_c8d8bceb4b8a46b589dd57d79ecd9bf9_Out_0_Vector4, _Multiply_548a83f07d1f491d858cdae4602c6b4d_Out_2_Vector4);
UnityTexture2D _Property_f89138a4a5294f5eacb84b4409275065_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Coverage_Mask, sampler_Coverage_Mask, _Coverage_Mask_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_25a54da52bb3450c8dd76359ada2a358_Out_0_Float = _Coverage_Mask_Scale;
float2 _TilingAndOffset_e8100e9136c347e2a346b1af40d87a52_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, (_Property_25a54da52bb3450c8dd76359ada2a358_Out_0_Float.xx), float2 (0, 0), _TilingAndOffset_e8100e9136c347e2a346b1af40d87a52_Out_3_Vector2);
float4 _SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_f89138a4a5294f5eacb84b4409275065_Out_0_Texture2D.tex, _Property_f89138a4a5294f5eacb84b4409275065_Out_0_Texture2D.samplerstate, _Property_f89138a4a5294f5eacb84b4409275065_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_e8100e9136c347e2a346b1af40d87a52_Out_3_Vector2) );
if (_Property_f89138a4a5294f5eacb84b4409275065_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_RGBA_0_Vector4, _Property_f89138a4a5294f5eacb84b4409275065_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_R_4_Float = _SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_RGBA_0_Vector4.r;
float _SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_G_5_Float = _SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_RGBA_0_Vector4.g;
float _SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_B_6_Float = _SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_RGBA_0_Vector4.b;
float _SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_A_7_Float = _SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_RGBA_0_Vector4.a;
float _Property_d8db892f807f4cdbb860c5f763c37576_Out_0_Float = _Level;
float _Split_dddb7fd39dd142f6a79faaf42e2f31fa_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_dddb7fd39dd142f6a79faaf42e2f31fa_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_dddb7fd39dd142f6a79faaf42e2f31fa_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_dddb7fd39dd142f6a79faaf42e2f31fa_A_4_Float = 0;
float _Negate_5d8cf946d6a44b0688a978f991ce300e_Out_1_Float;
Unity_Negate_float(_Split_dddb7fd39dd142f6a79faaf42e2f31fa_G_2_Float, _Negate_5d8cf946d6a44b0688a978f991ce300e_Out_1_Float);
UnityTexture2D _Property_6fa683e6dce24ba8a1a7b81715c96867_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Normal_Map, sampler_Normal_Map, _Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_6fa683e6dce24ba8a1a7b81715c96867_Out_0_Texture2D.tex, _Property_6fa683e6dce24ba8a1a7b81715c96867_Out_0_Texture2D.samplerstate, _Property_6fa683e6dce24ba8a1a7b81715c96867_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_96e68e44c54540839992559f96312049_Out_3_Vector2) );
if (_Property_6fa683e6dce24ba8a1a7b81715c96867_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4, _Property_6fa683e6dce24ba8a1a7b81715c96867_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4);
float _SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_R_4_Float = _SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4.r;
float _SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_G_5_Float = _SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4.g;
float _SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_B_6_Float = _SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4.b;
float _SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_A_7_Float = _SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4.a;
float3 _Transform_f92b73ad085c4c319bc399803f810059_Out_1_Vector3;
{
float3x3 tangentTransform = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
_Transform_f92b73ad085c4c319bc399803f810059_Out_1_Vector3 = TransformTangentToWorldDir((_SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4.xyz).xyz, tangentTransform, true).xyz;
}
#if defined(_COVERAGE_OVERLAY_VERTEX_POSITION)
float3 _CoverageOverlay_6c350da1b1bc48d8bd322a639af8a114_Out_0_Vector3 = (_Negate_5d8cf946d6a44b0688a978f991ce300e_Out_1_Float.xxx);
#else
float3 _CoverageOverlay_6c350da1b1bc48d8bd322a639af8a114_Out_0_Vector3 = _Transform_f92b73ad085c4c319bc399803f810059_Out_1_Vector3;
#endif
float _Split_5e81b1d12c8f426d99b755defa3f1129_R_1_Float = _CoverageOverlay_6c350da1b1bc48d8bd322a639af8a114_Out_0_Vector3[0];
float _Split_5e81b1d12c8f426d99b755defa3f1129_G_2_Float = _CoverageOverlay_6c350da1b1bc48d8bd322a639af8a114_Out_0_Vector3[1];
float _Split_5e81b1d12c8f426d99b755defa3f1129_B_3_Float = _CoverageOverlay_6c350da1b1bc48d8bd322a639af8a114_Out_0_Vector3[2];
float _Split_5e81b1d12c8f426d99b755defa3f1129_A_4_Float = 0;
float _Add_42c3350036d24612b752658957f0e996_Out_2_Float;
Unity_Add_float(_Property_d8db892f807f4cdbb860c5f763c37576_Out_0_Float, _Split_5e81b1d12c8f426d99b755defa3f1129_G_2_Float, _Add_42c3350036d24612b752658957f0e996_Out_2_Float);
float _Property_0b2c6c92abcd44f6ac12c213b29b4591_Out_0_Float = _Fade;
float _Multiply_e63d550088ac44d9aef2a43850ee5aa9_Out_2_Float;
Unity_Multiply_float_float(_Property_0b2c6c92abcd44f6ac12c213b29b4591_Out_0_Float, 5, _Multiply_e63d550088ac44d9aef2a43850ee5aa9_Out_2_Float);
float _Power_78c8b863b05d4425b22c06b245577c57_Out_2_Float;
Unity_Power_float(_Add_42c3350036d24612b752658957f0e996_Out_2_Float, _Multiply_e63d550088ac44d9aef2a43850ee5aa9_Out_2_Float, _Power_78c8b863b05d4425b22c06b245577c57_Out_2_Float);
float _Property_4a3dba7d791f4ae8afbe0ba166c8d15d_Out_0_Float = _Contrast;
float _Multiply_df64950968a547a495e68042783a7510_Out_2_Float;
Unity_Multiply_float_float(_Property_4a3dba7d791f4ae8afbe0ba166c8d15d_Out_0_Float, 5, _Multiply_df64950968a547a495e68042783a7510_Out_2_Float);
float3 _Contrast_d9aab63f6f4e464aaab650dfe18fa9c8_Out_2_Vector3;
Unity_Contrast_float((_Power_78c8b863b05d4425b22c06b245577c57_Out_2_Float.xxx), _Multiply_df64950968a547a495e68042783a7510_Out_2_Float, _Contrast_d9aab63f6f4e464aaab650dfe18fa9c8_Out_2_Vector3);
float3 _Multiply_cc22e2488cfa4aabab92d688a9b358a8_Out_2_Vector3;
Unity_Multiply_float3_float3((_SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_RGBA_0_Vector4.xyz), _Contrast_d9aab63f6f4e464aaab650dfe18fa9c8_Out_2_Vector3, _Multiply_cc22e2488cfa4aabab92d688a9b358a8_Out_2_Vector3);
#if defined(_ENABLE_COVERAGE)
float3 _EnableCoverage_8bf33e8bc98a4d1285a2c507af050ad2_Out_0_Vector3 = _Multiply_cc22e2488cfa4aabab92d688a9b358a8_Out_2_Vector3;
#else
float3 _EnableCoverage_8bf33e8bc98a4d1285a2c507af050ad2_Out_0_Vector3 = float3(0, 0, 0);
#endif
float3 _Saturate_9537e4d4f5274dbf9b1a191c2de9a0bd_Out_1_Vector3;
Unity_Saturate_float3(_EnableCoverage_8bf33e8bc98a4d1285a2c507af050ad2_Out_0_Vector3, _Saturate_9537e4d4f5274dbf9b1a191c2de9a0bd_Out_1_Vector3);
float3 _Lerp_7bbb7508500f4123a0db757387918fb8_Out_3_Vector3;
Unity_Lerp_float3((_Multiply_d62734e54c4a4b258cc66ff106c782ea_Out_2_Vector4.xyz), (_Multiply_548a83f07d1f491d858cdae4602c6b4d_Out_2_Vector4.xyz), _Saturate_9537e4d4f5274dbf9b1a191c2de9a0bd_Out_1_Vector3, _Lerp_7bbb7508500f4123a0db757387918fb8_Out_3_Vector3);
#if defined(_ENABLE_COVERAGE)
float3 _EnableCoverage_64958d03a7354be9a2dc2affb31f53a4_Out_0_Vector3 = _Lerp_7bbb7508500f4123a0db757387918fb8_Out_3_Vector3;
#else
float3 _EnableCoverage_64958d03a7354be9a2dc2affb31f53a4_Out_0_Vector3 = (_Multiply_d62734e54c4a4b258cc66ff106c782ea_Out_2_Vector4.xyz);
#endif
surface.BaseColor = _EnableCoverage_64958d03a7354be9a2dc2affb31f53a4_Out_0_Vector3;
surface.Alpha = float(1);
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
#pragma multi_compile _ _ENVIRONMENT_REACTION_BUFFER
#pragma target 4.5 _ENVIRONMENT_REACTION_BUFFER


// Pragmas
#pragma target 2.0
#pragma vertex vert
#pragma fragment frag

// Keywords
#pragma shader_feature_local_fragment _ _ALPHATEST_ON
#pragma shader_feature_local _COVERAGE_OVERLAY_VERTEX_POSITION _COVERAGE_OVERLAY_NORMAL_VECTOR
#pragma shader_feature_local _ _ENABLE_COVERAGE
#pragma shader_feature_local _ _BLEND_NORMALS

#if defined(_COVERAGE_OVERLAY_VERTEX_POSITION) && defined(_ENABLE_COVERAGE) && defined(_BLEND_NORMALS)
#define KEYWORD_PERMUTATION_0
#elif defined(_COVERAGE_OVERLAY_VERTEX_POSITION) && defined(_ENABLE_COVERAGE)
#define KEYWORD_PERMUTATION_1
#elif defined(_COVERAGE_OVERLAY_VERTEX_POSITION) && defined(_BLEND_NORMALS)
#define KEYWORD_PERMUTATION_2
#elif defined(_COVERAGE_OVERLAY_VERTEX_POSITION)
#define KEYWORD_PERMUTATION_3
#elif defined(_COVERAGE_OVERLAY_NORMAL_VECTOR) && defined(_ENABLE_COVERAGE) && defined(_BLEND_NORMALS)
#define KEYWORD_PERMUTATION_4
#elif defined(_COVERAGE_OVERLAY_NORMAL_VECTOR) && defined(_ENABLE_COVERAGE)
#define KEYWORD_PERMUTATION_5
#elif defined(_COVERAGE_OVERLAY_NORMAL_VECTOR) && defined(_BLEND_NORMALS)
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
float3 WorldSpacePosition;
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
float _EnvironmentReactionBufferSupported, _EnvironmentReactionIndex;
float4 _EnvState0, _EnvState1;
float _FuelCoverage, _AllowFuelCoverage;
float4 _WetTint, _CharTint, _FireColor;
float4 _EnvironmentCellBounds;
float _UseEnvironmentCells;

float4 _Coverage_Base_Map_TexelSize;
float4 _Base_Map_TexelSize;
float _Contrast;
float _Fade;
float4 _Normal_Map_TexelSize;
float4 _Coverage_Normal_Map_TexelSize;
float _Normal_Strength;
float _Coverage_Normal_Strength;
float _Smoothness;
float _Coverage_Smoothness;
float4 _Color;
float _Base_Map_Scale;
float _Level;
float4 _Coverage_Mask_TexelSize;
float _Coverage_Mask_Scale;
float4 _Coverage_Color;
float _Coverage_Map_Scale;
float4 _Metallic_Map_TexelSize;
float4 _Coverage_Metallic_Map_TexelSize;
float _Metallic;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Coverage_Base_Map);
SAMPLER(sampler_Coverage_Base_Map);
TEXTURE2D(_Base_Map);
SAMPLER(sampler_Base_Map);
TEXTURE2D(_Normal_Map);
SAMPLER(sampler_Normal_Map);
TEXTURE2D(_Coverage_Normal_Map);
SAMPLER(sampler_Coverage_Normal_Map);
TEXTURE2D(_Coverage_Mask);
SAMPLER(sampler_Coverage_Mask);
TEXTURE2D(_Metallic_Map);
SAMPLER(sampler_Metallic_Map);
TEXTURE2D(_Coverage_Metallic_Map);
SAMPLER(sampler_Coverage_Metallic_Map);

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

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

void Unity_Negate_float(float In, out float Out)
{
    Out = -1 * In;
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Power_float(float A, float B, out float Out)
{
    Out = pow(A, B);
}

void Unity_Contrast_float(float3 In, float Contrast, out float3 Out)
{
    float midpoint = pow(0.5, 2.2);
    Out =  (In - midpoint) * Contrast + midpoint;
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Saturate_float3(float3 In, out float3 Out)
{
    Out = saturate(In);
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
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
UnityTexture2D _Property_962e37e0ac5244c4a3ea6b170425e6e4_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Base_Map, sampler_Base_Map, _Base_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_837a954a3e08436195bd267068cb4b1a_Out_0_Float = _Base_Map_Scale;
float2 _TilingAndOffset_96e68e44c54540839992559f96312049_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, (_Property_837a954a3e08436195bd267068cb4b1a_Out_0_Float.xx), float2 (0, 0), _TilingAndOffset_96e68e44c54540839992559f96312049_Out_3_Vector2);
float4 _SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_962e37e0ac5244c4a3ea6b170425e6e4_Out_0_Texture2D.tex, _Property_962e37e0ac5244c4a3ea6b170425e6e4_Out_0_Texture2D.samplerstate, _Property_962e37e0ac5244c4a3ea6b170425e6e4_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_96e68e44c54540839992559f96312049_Out_3_Vector2) );
if (_Property_962e37e0ac5244c4a3ea6b170425e6e4_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_RGBA_0_Vector4, _Property_962e37e0ac5244c4a3ea6b170425e6e4_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_R_4_Float = _SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_RGBA_0_Vector4.r;
float _SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_G_5_Float = _SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_RGBA_0_Vector4.g;
float _SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_B_6_Float = _SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_RGBA_0_Vector4.b;
float _SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_A_7_Float = _SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_RGBA_0_Vector4.a;
float4 _Property_8bc5cedd8b5144aba79ab296bf97f4b8_Out_0_Vector4 = _Color;
float4 _Multiply_d62734e54c4a4b258cc66ff106c782ea_Out_2_Vector4;
Unity_Multiply_float4_float4(_SampleTexture2D_d9bb768690614234b2aac5e19bd2a178_RGBA_0_Vector4, _Property_8bc5cedd8b5144aba79ab296bf97f4b8_Out_0_Vector4, _Multiply_d62734e54c4a4b258cc66ff106c782ea_Out_2_Vector4);
UnityTexture2D _Property_06c1669821ff42c3bf719660b6f40626_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Coverage_Base_Map, sampler_Coverage_Base_Map, _Coverage_Base_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_4fea0d4cff68413d90a1875fb9c320e7_Out_0_Float = _Coverage_Map_Scale;
float2 _TilingAndOffset_264e68c10eca41918924efa9fb9469af_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, (_Property_4fea0d4cff68413d90a1875fb9c320e7_Out_0_Float.xx), float2 (0, 0), _TilingAndOffset_264e68c10eca41918924efa9fb9469af_Out_3_Vector2);
float4 _SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_06c1669821ff42c3bf719660b6f40626_Out_0_Texture2D.tex, _Property_06c1669821ff42c3bf719660b6f40626_Out_0_Texture2D.samplerstate, _Property_06c1669821ff42c3bf719660b6f40626_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_264e68c10eca41918924efa9fb9469af_Out_3_Vector2) );
if (_Property_06c1669821ff42c3bf719660b6f40626_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_RGBA_0_Vector4, _Property_06c1669821ff42c3bf719660b6f40626_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_R_4_Float = _SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_RGBA_0_Vector4.r;
float _SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_G_5_Float = _SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_RGBA_0_Vector4.g;
float _SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_B_6_Float = _SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_RGBA_0_Vector4.b;
float _SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_A_7_Float = _SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_RGBA_0_Vector4.a;
float4 _Property_c8d8bceb4b8a46b589dd57d79ecd9bf9_Out_0_Vector4 = _Coverage_Color;
float4 _Multiply_548a83f07d1f491d858cdae4602c6b4d_Out_2_Vector4;
Unity_Multiply_float4_float4(_SampleTexture2D_6183ffbf7eb947eb8523e20351e472ad_RGBA_0_Vector4, _Property_c8d8bceb4b8a46b589dd57d79ecd9bf9_Out_0_Vector4, _Multiply_548a83f07d1f491d858cdae4602c6b4d_Out_2_Vector4);
UnityTexture2D _Property_f89138a4a5294f5eacb84b4409275065_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Coverage_Mask, sampler_Coverage_Mask, _Coverage_Mask_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Property_25a54da52bb3450c8dd76359ada2a358_Out_0_Float = _Coverage_Mask_Scale;
float2 _TilingAndOffset_e8100e9136c347e2a346b1af40d87a52_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, (_Property_25a54da52bb3450c8dd76359ada2a358_Out_0_Float.xx), float2 (0, 0), _TilingAndOffset_e8100e9136c347e2a346b1af40d87a52_Out_3_Vector2);
float4 _SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_f89138a4a5294f5eacb84b4409275065_Out_0_Texture2D.tex, _Property_f89138a4a5294f5eacb84b4409275065_Out_0_Texture2D.samplerstate, _Property_f89138a4a5294f5eacb84b4409275065_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_e8100e9136c347e2a346b1af40d87a52_Out_3_Vector2) );
if (_Property_f89138a4a5294f5eacb84b4409275065_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_RGBA_0_Vector4, _Property_f89138a4a5294f5eacb84b4409275065_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_R_4_Float = _SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_RGBA_0_Vector4.r;
float _SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_G_5_Float = _SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_RGBA_0_Vector4.g;
float _SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_B_6_Float = _SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_RGBA_0_Vector4.b;
float _SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_A_7_Float = _SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_RGBA_0_Vector4.a;
float _Property_d8db892f807f4cdbb860c5f763c37576_Out_0_Float = _Level;
float _Split_dddb7fd39dd142f6a79faaf42e2f31fa_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_dddb7fd39dd142f6a79faaf42e2f31fa_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_dddb7fd39dd142f6a79faaf42e2f31fa_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_dddb7fd39dd142f6a79faaf42e2f31fa_A_4_Float = 0;
float _Negate_5d8cf946d6a44b0688a978f991ce300e_Out_1_Float;
Unity_Negate_float(_Split_dddb7fd39dd142f6a79faaf42e2f31fa_G_2_Float, _Negate_5d8cf946d6a44b0688a978f991ce300e_Out_1_Float);
UnityTexture2D _Property_6fa683e6dce24ba8a1a7b81715c96867_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Normal_Map, sampler_Normal_Map, _Normal_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_6fa683e6dce24ba8a1a7b81715c96867_Out_0_Texture2D.tex, _Property_6fa683e6dce24ba8a1a7b81715c96867_Out_0_Texture2D.samplerstate, _Property_6fa683e6dce24ba8a1a7b81715c96867_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_96e68e44c54540839992559f96312049_Out_3_Vector2) );
if (_Property_6fa683e6dce24ba8a1a7b81715c96867_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4, _Property_6fa683e6dce24ba8a1a7b81715c96867_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4);
float _SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_R_4_Float = _SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4.r;
float _SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_G_5_Float = _SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4.g;
float _SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_B_6_Float = _SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4.b;
float _SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_A_7_Float = _SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4.a;
float3 _Transform_f92b73ad085c4c319bc399803f810059_Out_1_Vector3;
{
float3x3 tangentTransform = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
_Transform_f92b73ad085c4c319bc399803f810059_Out_1_Vector3 = TransformTangentToWorldDir((_SampleTexture2D_f96cb44c15fe42778e00e2d23d047fe4_RGBA_0_Vector4.xyz).xyz, tangentTransform, true).xyz;
}
#if defined(_COVERAGE_OVERLAY_VERTEX_POSITION)
float3 _CoverageOverlay_6c350da1b1bc48d8bd322a639af8a114_Out_0_Vector3 = (_Negate_5d8cf946d6a44b0688a978f991ce300e_Out_1_Float.xxx);
#else
float3 _CoverageOverlay_6c350da1b1bc48d8bd322a639af8a114_Out_0_Vector3 = _Transform_f92b73ad085c4c319bc399803f810059_Out_1_Vector3;
#endif
float _Split_5e81b1d12c8f426d99b755defa3f1129_R_1_Float = _CoverageOverlay_6c350da1b1bc48d8bd322a639af8a114_Out_0_Vector3[0];
float _Split_5e81b1d12c8f426d99b755defa3f1129_G_2_Float = _CoverageOverlay_6c350da1b1bc48d8bd322a639af8a114_Out_0_Vector3[1];
float _Split_5e81b1d12c8f426d99b755defa3f1129_B_3_Float = _CoverageOverlay_6c350da1b1bc48d8bd322a639af8a114_Out_0_Vector3[2];
float _Split_5e81b1d12c8f426d99b755defa3f1129_A_4_Float = 0;
float _Add_42c3350036d24612b752658957f0e996_Out_2_Float;
Unity_Add_float(_Property_d8db892f807f4cdbb860c5f763c37576_Out_0_Float, _Split_5e81b1d12c8f426d99b755defa3f1129_G_2_Float, _Add_42c3350036d24612b752658957f0e996_Out_2_Float);
float _Property_0b2c6c92abcd44f6ac12c213b29b4591_Out_0_Float = _Fade;
float _Multiply_e63d550088ac44d9aef2a43850ee5aa9_Out_2_Float;
Unity_Multiply_float_float(_Property_0b2c6c92abcd44f6ac12c213b29b4591_Out_0_Float, 5, _Multiply_e63d550088ac44d9aef2a43850ee5aa9_Out_2_Float);
float _Power_78c8b863b05d4425b22c06b245577c57_Out_2_Float;
Unity_Power_float(_Add_42c3350036d24612b752658957f0e996_Out_2_Float, _Multiply_e63d550088ac44d9aef2a43850ee5aa9_Out_2_Float, _Power_78c8b863b05d4425b22c06b245577c57_Out_2_Float);
float _Property_4a3dba7d791f4ae8afbe0ba166c8d15d_Out_0_Float = _Contrast;
float _Multiply_df64950968a547a495e68042783a7510_Out_2_Float;
Unity_Multiply_float_float(_Property_4a3dba7d791f4ae8afbe0ba166c8d15d_Out_0_Float, 5, _Multiply_df64950968a547a495e68042783a7510_Out_2_Float);
float3 _Contrast_d9aab63f6f4e464aaab650dfe18fa9c8_Out_2_Vector3;
Unity_Contrast_float((_Power_78c8b863b05d4425b22c06b245577c57_Out_2_Float.xxx), _Multiply_df64950968a547a495e68042783a7510_Out_2_Float, _Contrast_d9aab63f6f4e464aaab650dfe18fa9c8_Out_2_Vector3);
float3 _Multiply_cc22e2488cfa4aabab92d688a9b358a8_Out_2_Vector3;
Unity_Multiply_float3_float3((_SampleTexture2D_87bc9f6b27a94614a195c14f7470be52_RGBA_0_Vector4.xyz), _Contrast_d9aab63f6f4e464aaab650dfe18fa9c8_Out_2_Vector3, _Multiply_cc22e2488cfa4aabab92d688a9b358a8_Out_2_Vector3);
#if defined(_ENABLE_COVERAGE)
float3 _EnableCoverage_8bf33e8bc98a4d1285a2c507af050ad2_Out_0_Vector3 = _Multiply_cc22e2488cfa4aabab92d688a9b358a8_Out_2_Vector3;
#else
float3 _EnableCoverage_8bf33e8bc98a4d1285a2c507af050ad2_Out_0_Vector3 = float3(0, 0, 0);
#endif
float3 _Saturate_9537e4d4f5274dbf9b1a191c2de9a0bd_Out_1_Vector3;
Unity_Saturate_float3(_EnableCoverage_8bf33e8bc98a4d1285a2c507af050ad2_Out_0_Vector3, _Saturate_9537e4d4f5274dbf9b1a191c2de9a0bd_Out_1_Vector3);
float3 _Lerp_7bbb7508500f4123a0db757387918fb8_Out_3_Vector3;
Unity_Lerp_float3((_Multiply_d62734e54c4a4b258cc66ff106c782ea_Out_2_Vector4.xyz), (_Multiply_548a83f07d1f491d858cdae4602c6b4d_Out_2_Vector4.xyz), _Saturate_9537e4d4f5274dbf9b1a191c2de9a0bd_Out_1_Vector3, _Lerp_7bbb7508500f4123a0db757387918fb8_Out_3_Vector3);
#if defined(_ENABLE_COVERAGE)
float3 _EnableCoverage_64958d03a7354be9a2dc2affb31f53a4_Out_0_Vector3 = _Lerp_7bbb7508500f4123a0db757387918fb8_Out_3_Vector3;
#else
float3 _EnableCoverage_64958d03a7354be9a2dc2affb31f53a4_Out_0_Vector3 = (_Multiply_d62734e54c4a4b258cc66ff106c782ea_Out_2_Vector4.xyz);
#endif
surface.BaseColor = _EnableCoverage_64958d03a7354be9a2dc2affb31f53a4_Out_0_Vector3;
surface.Alpha = float(1);
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
CustomEditor "UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI"
CustomEditorForRenderPipeline "UnityEditor.ShaderGraphLitGUI" "UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset"
FallBack "Hidden/Shader Graph/FallbackError"
}