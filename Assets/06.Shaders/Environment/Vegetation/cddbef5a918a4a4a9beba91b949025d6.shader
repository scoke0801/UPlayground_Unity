Shader "UPlayGround/Environment/Vegetation/cddbef5a918a4a4a9beba91b949025d6"
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

_ree("ree", Float) = 2
_Foliage_Color_Top("Foliage Color Top", Color) = (0.3843138, 0.6156863, 0.2078432, 1)
_Foliage_Color_Bottom("Foliage Color Bottom", Color) = (0, 0.4862745, 0.1294118, 1)
_Tree_Height("Tree Height", Float) = 10
_Gradience_Power("Gradience Power", Float) = 1
[NoScaleOffset]_Color_Map("Color Map", 2D) = "white" {}
_Color_Map_Tiling("Color Map Tiling", Float) = 1
_Alpha_Clip("Alpha Clip", Range(0, 1)) = 0.5
_Fresnel_Pow("Fresnel Pow", Float) = 5
_Fresnel_Mul("Fresnel Mul", Float) = 0
_Always_On_Fresnel_Pow("Always On Fresnel Pow", Float) = 3
_Always_On_Fresnel_Intensity("Always On Fresnel Intensity", Float) = 1
_Always_On_Fresnel_Color("Always On Fresnel Color", Color) = (0.4956178, 0.794, 0.2680382, 0)
_Subsurface_Intensity("Subsurface Intensity", Float) = 1
_Subsurface_Transition_Smoothness("Subsurface Transition Smoothness", Float) = 2
_Subsurface_Fresnel_Transition_Smoothness("Subsurface Fresnel Transition Smoothness", Float) = 2
_Subsurface_Fresnel_Pow("Subsurface Fresnel Pow", Float) = 2
_Subsurface_Fresnel_Intensity("Subsurface Fresnel Intensity", Float) = 2
_Roughness("Roughness", Range(0, 1)) = 1
_Height_Weight("Height Weight", Float) = 0
_Height_Gradience("Height Gradience", Float) = 0
_Sway_Wind_Intensity("Sway Wind Intensity", Float) = 0
_Sway_Wind_Speed("Sway Wind Speed", Float) = 0
_Sway_Wind_Direction("Sway Wind Direction", Vector, 3) = (0, 0, 0, 0)
_Sway_Wind_Time_Random_Offset_Amount("Sway Wind Time Random Offset Amount", Float) = 0
_Wiggle_Wind_Size("Wiggle Wind Size", Range(0, 1)) = 0.025
_Wiggle_Wind_Speed_Small("Wiggle Wind Speed Small", Float) = 0.03
_Wiggle_Wind_Speed_Large("Wiggle Wind Speed Large", Float) = 0.03
_Wiggle_Wind_Intensity("Wiggle Wind Intensity", Float) = 0.1
[Toggle(_USE_WPO)]_USE_WPO("Use WPO?", Float) = 1
[Toggle(_USE_HEIGHT_GRADIENCE)]_USE_HEIGHT_GRADIENCE("Use Height Gradience?", Float) = 1
[NonModifiableTextureData][NoScaleOffset]_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D("Texture2D", 2D) = "white" {}
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
#pragma shader_feature_local _ _USE_WPO
#pragma shader_feature_local _ _USE_HEIGHT_GRADIENCE

#if defined(_USE_WPO) && defined(_USE_HEIGHT_GRADIENCE)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_WPO)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_HEIGHT_GRADIENCE)
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
 float3 WorldSpaceNormal;
 float3 TangentSpaceNormal;
 float3 WorldSpaceViewDirection;
 float3 WorldSpacePosition;
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 WorldSpacePosition;
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

float4 _Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D_TexelSize;
float _Subsurface_Fresnel_Transition_Smoothness;
float _Subsurface_Fresnel_Pow;
float _Subsurface_Fresnel_Intensity;
float _Subsurface_Transition_Smoothness;
float _Wiggle_Wind_Speed_Large;
float _Wiggle_Wind_Intensity;
float _Roughness;
float4 _Foliage_Color_Bottom;
float _Sway_Wind_Speed;
float _Gradience_Power;
float _Fresnel_Mul;
float4 _Foliage_Color_Top;
float _Always_On_Fresnel_Intensity;
float _Tree_Height;
float4 _Color_Map_TexelSize;
float _Height_Weight;
float _Height_Gradience;
float _Sway_Wind_Time_Random_Offset_Amount;
float _Sway_Wind_Intensity;
float3 _Sway_Wind_Direction;
float _Color_Map_Tiling;
float _Subsurface_Intensity;
float _Alpha_Clip;
float _Wiggle_Wind_Size;
float _Wiggle_Wind_Speed_Small;
float _Fresnel_Pow;
float _Always_On_Fresnel_Pow;
float4 _Always_On_Fresnel_Color;
float _ree;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D);
SAMPLER(sampler_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D);
TEXTURE2D(_Color_Map);
SAMPLER(sampler_Color_Map);

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

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Saturate_float4(float4 In, out float4 Out)
{
    Out = saturate(In);
}

void Unity_Normalize_float3(float3 In, out float3 Out)
{
    Out = normalize(In);
}

void Unity_DotProduct_float3(float3 A, float3 B, out float Out)
{
    Out = dot(A, B);
}

void Unity_Absolute_float(float In, out float Out)
{
    Out = abs(In);
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
}

// unity-custom-func-begin
void MainLight_float(out float3 Direction){

#ifdef UNITY_SHADER_GRAPH
	Direction = _WorldSpaceLightPos0.xyz;
#else
	Direction = float3(1,1,1);
#endif
}
// unity-custom-func-end

void Unity_DotProduct_float2(float2 A, float2 B, out float Out)
{
    Out = dot(A, B);
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
float _Property_7689f3a0523b4af9912879a05d1d5206_Out_0_Float = _Sway_Wind_Speed;
float3 _Property_e0531013c46845b6b036ac453118c744_Out_0_Vector3 = _Sway_Wind_Direction;
float _Property_35588e6a16b5457695dba39b1644d669_Out_0_Float = _Sway_Wind_Intensity;
float _Property_c8d3d7e6e6f64d409f010d988b96e327_Out_0_Float = _Sway_Wind_Time_Random_Offset_Amount;
Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float _WindSway_662bcd4f8b2b4607b138cb44de00a708;
_WindSway_662bcd4f8b2b4607b138cb44de00a708.ObjectSpacePosition = IN.ObjectSpacePosition;
_WindSway_662bcd4f8b2b4607b138cb44de00a708.TimeParameters = IN.TimeParameters;
float3 _WindSway_662bcd4f8b2b4607b138cb44de00a708_OutVector3_1_Vector3;
SG_WindSway_51c4027ae50a8cd49be53c4990b9f179_float(_Property_7689f3a0523b4af9912879a05d1d5206_Out_0_Float, _Property_e0531013c46845b6b036ac453118c744_Out_0_Vector3, _Property_35588e6a16b5457695dba39b1644d669_Out_0_Float, _Property_c8d3d7e6e6f64d409f010d988b96e327_Out_0_Float, _WindSway_662bcd4f8b2b4607b138cb44de00a708, _WindSway_662bcd4f8b2b4607b138cb44de00a708_OutVector3_1_Vector3);
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_R_1_Float = IN.WorldSpacePosition[0];
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_G_2_Float = IN.WorldSpacePosition[1];
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_B_3_Float = IN.WorldSpacePosition[2];
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_A_4_Float = 0;
float3 _Vector3_e154b524054546d2bbeab293c0e20519_Out_0_Vector3 = float3(float(0), float(0), float(0));
float3 _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3;
_Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3 = TransformObjectToWorld(_Vector3_e154b524054546d2bbeab293c0e20519_Out_0_Vector3.xyz);
float _Split_5eb48153bba44ea2b128583819f09a11_R_1_Float = _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3[0];
float _Split_5eb48153bba44ea2b128583819f09a11_G_2_Float = _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3[1];
float _Split_5eb48153bba44ea2b128583819f09a11_B_3_Float = _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3[2];
float _Split_5eb48153bba44ea2b128583819f09a11_A_4_Float = 0;
float _Subtract_d6a99abdf4e74644acffc1718d1735bf_Out_2_Float;
Unity_Subtract_float(_Split_8fe962eb6f0f4d6fba734f9edcaf9067_G_2_Float, _Split_5eb48153bba44ea2b128583819f09a11_G_2_Float, _Subtract_d6a99abdf4e74644acffc1718d1735bf_Out_2_Float);
float _Property_1cf3cda52e094e638df491b003607dfd_Out_0_Float = _Height_Weight;
float _Split_145cf2161d4c4510b08248cad819f655_R_1_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[0];
float _Split_145cf2161d4c4510b08248cad819f655_G_2_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[1];
float _Split_145cf2161d4c4510b08248cad819f655_B_3_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[2];
float _Split_145cf2161d4c4510b08248cad819f655_A_4_Float = 0;
float _Multiply_2c140d573b9447aab058bd863fe2f4f7_Out_2_Float;
Unity_Multiply_float_float(_Property_1cf3cda52e094e638df491b003607dfd_Out_0_Float, _Split_145cf2161d4c4510b08248cad819f655_G_2_Float, _Multiply_2c140d573b9447aab058bd863fe2f4f7_Out_2_Float);
float _Divide_0f6a637143d841d0b1c94fa2ae3e164d_Out_2_Float;
Unity_Divide_float(_Subtract_d6a99abdf4e74644acffc1718d1735bf_Out_2_Float, _Multiply_2c140d573b9447aab058bd863fe2f4f7_Out_2_Float, _Divide_0f6a637143d841d0b1c94fa2ae3e164d_Out_2_Float);
float _Saturate_1e4e29fb5c074565a1f941c046957b05_Out_1_Float;
Unity_Saturate_float(_Divide_0f6a637143d841d0b1c94fa2ae3e164d_Out_2_Float, _Saturate_1e4e29fb5c074565a1f941c046957b05_Out_1_Float);
float _Property_3d64a0c8dd104507b9a0762a778fe585_Out_0_Float = _Height_Gradience;
float _Power_f1d74728398a45f9993fe3b3affb6584_Out_2_Float;
Unity_Power_float(_Saturate_1e4e29fb5c074565a1f941c046957b05_Out_1_Float, _Property_3d64a0c8dd104507b9a0762a778fe585_Out_0_Float, _Power_f1d74728398a45f9993fe3b3affb6584_Out_2_Float);
float _Saturate_57b130350452418fa1d09d4a0ddd64ac_Out_1_Float;
Unity_Saturate_float(_Power_f1d74728398a45f9993fe3b3affb6584_Out_2_Float, _Saturate_57b130350452418fa1d09d4a0ddd64ac_Out_1_Float);
float3 _Lerp_1fbf8d0a7f5a4e2ab381967ac90385c7_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _WindSway_662bcd4f8b2b4607b138cb44de00a708_OutVector3_1_Vector3, (_Saturate_57b130350452418fa1d09d4a0ddd64ac_Out_1_Float.xxx), _Lerp_1fbf8d0a7f5a4e2ab381967ac90385c7_Out_3_Vector3);
float _Split_bd04916788224fa2bb71be3f27eb839a_R_1_Float = IN.WorldSpacePosition[0];
float _Split_bd04916788224fa2bb71be3f27eb839a_G_2_Float = IN.WorldSpacePosition[1];
float _Split_bd04916788224fa2bb71be3f27eb839a_B_3_Float = IN.WorldSpacePosition[2];
float _Split_bd04916788224fa2bb71be3f27eb839a_A_4_Float = 0;
float4 _Combine_45cdab9c20414d03862100967f56c78d_RGBA_4_Vector4;
float3 _Combine_45cdab9c20414d03862100967f56c78d_RGB_5_Vector3;
float2 _Combine_45cdab9c20414d03862100967f56c78d_RG_6_Vector2;
Unity_Combine_float(_Split_bd04916788224fa2bb71be3f27eb839a_R_1_Float, _Split_bd04916788224fa2bb71be3f27eb839a_B_3_Float, float(0), float(0), _Combine_45cdab9c20414d03862100967f56c78d_RGBA_4_Vector4, _Combine_45cdab9c20414d03862100967f56c78d_RGB_5_Vector3, _Combine_45cdab9c20414d03862100967f56c78d_RG_6_Vector2);
float _Property_9b35f6244c0247c7a9be7af2671f58e7_Out_0_Float = _Wiggle_Wind_Size;
float2 _Multiply_ba4d7f376f904143acc9eb33d8414ff8_Out_2_Vector2;
Unity_Multiply_float2_float2(_Combine_45cdab9c20414d03862100967f56c78d_RG_6_Vector2, (_Property_9b35f6244c0247c7a9be7af2671f58e7_Out_0_Float.xx), _Multiply_ba4d7f376f904143acc9eb33d8414ff8_Out_2_Vector2);
float _Property_6eae25110c184e7a94293b95ddc9ecef_Out_0_Float = _Wiggle_Wind_Speed_Small;
float _Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Property_6eae25110c184e7a94293b95ddc9ecef_Out_0_Float, _Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float);
float _Multiply_7510d5a95e6c4d3986bb83059d23d2fa_Out_2_Float;
Unity_Multiply_float_float(_Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float, 0.8, _Multiply_7510d5a95e6c4d3986bb83059d23d2fa_Out_2_Float);
float2 _Vector2_cb26bc8c681b455c81073f0dc3c8f6b4_Out_0_Vector2 = float2(_Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float, _Multiply_7510d5a95e6c4d3986bb83059d23d2fa_Out_2_Float);
float2 _TilingAndOffset_e5f47c2cf9d846859a72aadcdac7fc3b_Out_3_Vector2;
Unity_TilingAndOffset_float(_Multiply_ba4d7f376f904143acc9eb33d8414ff8_Out_2_Vector2, float2 (1, 1), _Vector2_cb26bc8c681b455c81073f0dc3c8f6b4_Out_0_Vector2, _TilingAndOffset_e5f47c2cf9d846859a72aadcdac7fc3b_Out_3_Vector2);
  float4 _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).tex, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).GetTransformedUV(_TilingAndOffset_e5f47c2cf9d846859a72aadcdac7fc3b_Out_3_Vector2), float(2));
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_R_5_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_G_6_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_B_7_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_A_8_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.a;
float _Float_3f1a7b5619414946b13e7cef19336d45_Out_0_Float = float(0.3);
float _Subtract_3105ef040f344481afb342cd9ef65e9f_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_R_5_Float, _Float_3f1a7b5619414946b13e7cef19336d45_Out_0_Float, _Subtract_3105ef040f344481afb342cd9ef65e9f_Out_2_Float);
float _Split_18495110217342bc80044d9345d98fbd_R_1_Float = IN.WorldSpacePosition[0];
float _Split_18495110217342bc80044d9345d98fbd_G_2_Float = IN.WorldSpacePosition[1];
float _Split_18495110217342bc80044d9345d98fbd_B_3_Float = IN.WorldSpacePosition[2];
float _Split_18495110217342bc80044d9345d98fbd_A_4_Float = 0;
float4 _Combine_d1ee3e49696840daa7cfd43df2287807_RGBA_4_Vector4;
float3 _Combine_d1ee3e49696840daa7cfd43df2287807_RGB_5_Vector3;
float2 _Combine_d1ee3e49696840daa7cfd43df2287807_RG_6_Vector2;
Unity_Combine_float(_Split_18495110217342bc80044d9345d98fbd_R_1_Float, _Split_18495110217342bc80044d9345d98fbd_G_2_Float, float(0), float(0), _Combine_d1ee3e49696840daa7cfd43df2287807_RGBA_4_Vector4, _Combine_d1ee3e49696840daa7cfd43df2287807_RGB_5_Vector3, _Combine_d1ee3e49696840daa7cfd43df2287807_RG_6_Vector2);
float _Property_9efe7c2851414ea39b6971daac6143e5_Out_0_Float = _Wiggle_Wind_Size;
float _Multiply_4f6b361114004341aeae12987f071375_Out_2_Float;
Unity_Multiply_float_float(_Property_9efe7c2851414ea39b6971daac6143e5_Out_0_Float, 0.763, _Multiply_4f6b361114004341aeae12987f071375_Out_2_Float);
float2 _Multiply_0d88e33790f0458b9e6e7f49946e6344_Out_2_Vector2;
Unity_Multiply_float2_float2(_Combine_d1ee3e49696840daa7cfd43df2287807_RG_6_Vector2, (_Multiply_4f6b361114004341aeae12987f071375_Out_2_Float.xx), _Multiply_0d88e33790f0458b9e6e7f49946e6344_Out_2_Vector2);
float _Property_56c994a79361438d88ea30b1e3ccf2aa_Out_0_Float = _Wiggle_Wind_Speed_Large;
float _Multiply_c5e5feed518642678424c6f0d45b4377_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Property_56c994a79361438d88ea30b1e3ccf2aa_Out_0_Float, _Multiply_c5e5feed518642678424c6f0d45b4377_Out_2_Float);
float2 _TilingAndOffset_7dac505987c144edad083d13ebca67e6_Out_3_Vector2;
Unity_TilingAndOffset_float(_Multiply_0d88e33790f0458b9e6e7f49946e6344_Out_2_Vector2, float2 (1, 1), (_Multiply_c5e5feed518642678424c6f0d45b4377_Out_2_Float.xx), _TilingAndOffset_7dac505987c144edad083d13ebca67e6_Out_3_Vector2);
  float4 _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).tex, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).GetTransformedUV(_TilingAndOffset_7dac505987c144edad083d13ebca67e6_Out_3_Vector2), float(2));
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_R_5_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_G_6_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_B_7_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_A_8_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.a;
float _Subtract_e29fd5b0daea41a1a38ac52280e6db88_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_G_6_Float, _Float_3f1a7b5619414946b13e7cef19336d45_Out_0_Float, _Subtract_e29fd5b0daea41a1a38ac52280e6db88_Out_2_Float);
float _Add_6d51be6104ee4e23a093dfb9c3d993f7_Out_2_Float;
Unity_Add_float(_Subtract_3105ef040f344481afb342cd9ef65e9f_Out_2_Float, _Subtract_e29fd5b0daea41a1a38ac52280e6db88_Out_2_Float, _Add_6d51be6104ee4e23a093dfb9c3d993f7_Out_2_Float);
float4 _Multiply_db043a8b3fc34edb8bee433085c7e061_Out_2_Vector4;
Unity_Multiply_float4_float4((_Add_6d51be6104ee4e23a093dfb9c3d993f7_Out_2_Float.xxxx), IN.VertexColor, _Multiply_db043a8b3fc34edb8bee433085c7e061_Out_2_Vector4);
float _Property_46c8c648052246a4a32e80905ed7e9df_Out_0_Float = _Wiggle_Wind_Intensity;
float4 _Multiply_4baa80310c414483a2aada72baa7a883_Out_2_Vector4;
Unity_Multiply_float4_float4(_Multiply_db043a8b3fc34edb8bee433085c7e061_Out_2_Vector4, (_Property_46c8c648052246a4a32e80905ed7e9df_Out_0_Float.xxxx), _Multiply_4baa80310c414483a2aada72baa7a883_Out_2_Vector4);
float3 _Multiply_2c95a93020c44a20be82a31b985c35b8_Out_2_Vector3;
Unity_Multiply_float3_float3((_Multiply_4baa80310c414483a2aada72baa7a883_Out_2_Vector4.xyz), IN.ObjectSpaceNormal, _Multiply_2c95a93020c44a20be82a31b985c35b8_Out_2_Vector3);
float3 _Add_5a143eeddea74bceb3ddd5b059f49da5_Out_2_Vector3;
Unity_Add_float3(_Lerp_1fbf8d0a7f5a4e2ab381967ac90385c7_Out_3_Vector3, _Multiply_2c95a93020c44a20be82a31b985c35b8_Out_2_Vector3, _Add_5a143eeddea74bceb3ddd5b059f49da5_Out_2_Vector3);
#if defined(_USE_WPO)
float3 _UseWPO_1f28bdd134c9432e812aded965021562_Out_0_Vector3 = _Add_5a143eeddea74bceb3ddd5b059f49da5_Out_2_Vector3;
#else
float3 _UseWPO_1f28bdd134c9432e812aded965021562_Out_0_Vector3 = IN.ObjectSpacePosition;
#endif
float3 _UPGFoliageContactPositionCustomFunction_3446fb1a0c894696bd903b7eb184e241_bentPositionOS_1_Vector3;
UPG_FoliageContactPosition_float(_UseWPO_1f28bdd134c9432e812aded965021562_Out_0_Vector3, _UPGFoliageContactPositionCustomFunction_3446fb1a0c894696bd903b7eb184e241_bentPositionOS_1_Vector3);
description.Position = _UPGFoliageContactPositionCustomFunction_3446fb1a0c894696bd903b7eb184e241_bentPositionOS_1_Vector3;
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
float4 _Property_a9c1982af1c74efb9ac25b0326ae7979_Out_0_Vector4 = _Foliage_Color_Bottom;
float4 _Property_50eb1cb960084765a0771df7f40a6d5d_Out_0_Vector4 = _Foliage_Color_Top;
float _Split_1540f44067c24640b4968355fa26a295_R_1_Float = IN.WorldSpacePosition[0];
float _Split_1540f44067c24640b4968355fa26a295_G_2_Float = IN.WorldSpacePosition[1];
float _Split_1540f44067c24640b4968355fa26a295_B_3_Float = IN.WorldSpacePosition[2];
float _Split_1540f44067c24640b4968355fa26a295_A_4_Float = 0;
float3 _Vector3_54c605479a824dd3a62abb1919f39c57_Out_0_Vector3 = float3(float(0), float(0), float(0));
float3 _Transform_f3f68ad0eeb04ce9ac459f2859353c06_Out_1_Vector3;
_Transform_f3f68ad0eeb04ce9ac459f2859353c06_Out_1_Vector3 = TransformObjectToWorld(_Vector3_54c605479a824dd3a62abb1919f39c57_Out_0_Vector3.xyz);
float _Split_15d5002ae66545318962a44958be8e00_R_1_Float = _Transform_f3f68ad0eeb04ce9ac459f2859353c06_Out_1_Vector3[0];
float _Split_15d5002ae66545318962a44958be8e00_G_2_Float = _Transform_f3f68ad0eeb04ce9ac459f2859353c06_Out_1_Vector3[1];
float _Split_15d5002ae66545318962a44958be8e00_B_3_Float = _Transform_f3f68ad0eeb04ce9ac459f2859353c06_Out_1_Vector3[2];
float _Split_15d5002ae66545318962a44958be8e00_A_4_Float = 0;
float _Subtract_3131eba61d024496834f2933dbde6855_Out_2_Float;
Unity_Subtract_float(_Split_1540f44067c24640b4968355fa26a295_G_2_Float, _Split_15d5002ae66545318962a44958be8e00_G_2_Float, _Subtract_3131eba61d024496834f2933dbde6855_Out_2_Float);
float _Property_605b997dac5a4228881635945f995c72_Out_0_Float = _Tree_Height;
float _Divide_8e4152d2ed4943778a44dc39efa65611_Out_2_Float;
Unity_Divide_float(_Subtract_3131eba61d024496834f2933dbde6855_Out_2_Float, _Property_605b997dac5a4228881635945f995c72_Out_0_Float, _Divide_8e4152d2ed4943778a44dc39efa65611_Out_2_Float);
float _Saturate_086c13233c3d43fab6a33a1e2ed498c2_Out_1_Float;
Unity_Saturate_float(_Divide_8e4152d2ed4943778a44dc39efa65611_Out_2_Float, _Saturate_086c13233c3d43fab6a33a1e2ed498c2_Out_1_Float);
float _Property_0e5bff248b2341909121bb16666f3468_Out_0_Float = _Gradience_Power;
float _Power_be0c59cc158d492f99ea24f3b1001e1f_Out_2_Float;
Unity_Power_float(_Saturate_086c13233c3d43fab6a33a1e2ed498c2_Out_1_Float, _Property_0e5bff248b2341909121bb16666f3468_Out_0_Float, _Power_be0c59cc158d492f99ea24f3b1001e1f_Out_2_Float);
float _Saturate_26bbc2447b5a402c9562a9e5d012fd6c_Out_1_Float;
Unity_Saturate_float(_Power_be0c59cc158d492f99ea24f3b1001e1f_Out_2_Float, _Saturate_26bbc2447b5a402c9562a9e5d012fd6c_Out_1_Float);
float4 _Lerp_795c581446834fe0bee35971bc8f0c5c_Out_3_Vector4;
Unity_Lerp_float4(_Property_a9c1982af1c74efb9ac25b0326ae7979_Out_0_Vector4, _Property_50eb1cb960084765a0771df7f40a6d5d_Out_0_Vector4, (_Saturate_26bbc2447b5a402c9562a9e5d012fd6c_Out_1_Float.xxxx), _Lerp_795c581446834fe0bee35971bc8f0c5c_Out_3_Vector4);
float4 _Property_4036fdaeaa4647adb3dd84d70ff1161e_Out_0_Vector4 = _Foliage_Color_Top;
#if defined(_USE_HEIGHT_GRADIENCE)
float4 _UseHeightGradience_d4326b9500c14f11a9090b7e8f107721_Out_0_Vector4 = _Lerp_795c581446834fe0bee35971bc8f0c5c_Out_3_Vector4;
#else
float4 _UseHeightGradience_d4326b9500c14f11a9090b7e8f107721_Out_0_Vector4 = _Property_4036fdaeaa4647adb3dd84d70ff1161e_Out_0_Vector4;
#endif
UnityTexture2D _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Color_Map, sampler_Color_Map, _Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_eaea7ba5e1e74519a1bc7833f0103ab8_Out_0_Vector4 = IN.uv0;
float _Property_95352575ae4140008287fd58138e8eb5_Out_0_Float = _Color_Map_Tiling;
float4 _Multiply_86fafeae394745fe880655d03cafd9e1_Out_2_Vector4;
Unity_Multiply_float4_float4(_UV_eaea7ba5e1e74519a1bc7833f0103ab8_Out_0_Vector4, (_Property_95352575ae4140008287fd58138e8eb5_Out_0_Float.xxxx), _Multiply_86fafeae394745fe880655d03cafd9e1_Out_2_Vector4);
float4 _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.tex, _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.samplerstate, _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.GetTransformedUV((_Multiply_86fafeae394745fe880655d03cafd9e1_Out_2_Vector4.xy)) );
if (_Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4, _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_R_4_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.r;
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_G_5_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.g;
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_B_6_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.b;
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_A_7_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.a;
float4 _Multiply_a05088f8e3434d54920d012fc47555a7_Out_2_Vector4;
Unity_Multiply_float4_float4(_UseHeightGradience_d4326b9500c14f11a9090b7e8f107721_Out_0_Vector4, _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4, _Multiply_a05088f8e3434d54920d012fc47555a7_Out_2_Vector4);
float4 _Property_ee5875e8bc214caba4dbea1511d06cd2_Out_0_Vector4 = _Always_On_Fresnel_Color;
float _Property_be2ed593ceb345f89b57d123af3db203_Out_0_Float = _Always_On_Fresnel_Intensity;
float4 _Multiply_dcf6e90c046c4f72b77b81df5b922468_Out_2_Vector4;
Unity_Multiply_float4_float4(_Property_ee5875e8bc214caba4dbea1511d06cd2_Out_0_Vector4, (_Property_be2ed593ceb345f89b57d123af3db203_Out_0_Float.xxxx), _Multiply_dcf6e90c046c4f72b77b81df5b922468_Out_2_Vector4);
float4 _Saturate_26f2848ceb0a494094065a9387275032_Out_1_Vector4;
Unity_Saturate_float4(_Multiply_dcf6e90c046c4f72b77b81df5b922468_Out_2_Vector4, _Saturate_26f2848ceb0a494094065a9387275032_Out_1_Vector4);
float3 _Normalize_764cced5d0894f3d89f5911d50d5f4a2_Out_1_Vector3;
Unity_Normalize_float3(IN.WorldSpaceViewDirection, _Normalize_764cced5d0894f3d89f5911d50d5f4a2_Out_1_Vector3);
float _DotProduct_eb716c3ca301415688a52e76b310a65c_Out_2_Float;
Unity_DotProduct_float3(IN.WorldSpaceNormal, _Normalize_764cced5d0894f3d89f5911d50d5f4a2_Out_1_Vector3, _DotProduct_eb716c3ca301415688a52e76b310a65c_Out_2_Float);
float _Absolute_470cf4f0298a44a695f47aa5ea61c0ab_Out_1_Float;
Unity_Absolute_float(_DotProduct_eb716c3ca301415688a52e76b310a65c_Out_2_Float, _Absolute_470cf4f0298a44a695f47aa5ea61c0ab_Out_1_Float);
float _OneMinus_fa832b719fc24001be421507d395a592_Out_1_Float;
Unity_OneMinus_float(_Absolute_470cf4f0298a44a695f47aa5ea61c0ab_Out_1_Float, _OneMinus_fa832b719fc24001be421507d395a592_Out_1_Float);
float _Property_d9cfcacb90204bdfa36d0c1b7c0da452_Out_0_Float = _Always_On_Fresnel_Pow;
float _Power_7841b1ae85554473b96cce132c7ff7e1_Out_2_Float;
Unity_Power_float(_OneMinus_fa832b719fc24001be421507d395a592_Out_1_Float, _Property_d9cfcacb90204bdfa36d0c1b7c0da452_Out_0_Float, _Power_7841b1ae85554473b96cce132c7ff7e1_Out_2_Float);
float _Saturate_908f6307f948471ca95c0e7507cd7e43_Out_1_Float;
Unity_Saturate_float(_Power_7841b1ae85554473b96cce132c7ff7e1_Out_2_Float, _Saturate_908f6307f948471ca95c0e7507cd7e43_Out_1_Float);
float4 _Lerp_3406aa7c52b14c818ac9eb76bf068e2d_Out_3_Vector4;
Unity_Lerp_float4(_Multiply_a05088f8e3434d54920d012fc47555a7_Out_2_Vector4, _Saturate_26f2848ceb0a494094065a9387275032_Out_1_Vector4, (_Saturate_908f6307f948471ca95c0e7507cd7e43_Out_1_Float.xxxx), _Lerp_3406aa7c52b14c818ac9eb76bf068e2d_Out_3_Vector4);
float _Property_e8b3335851f84d91b28234460148cd0f_Out_0_Float = _Subsurface_Intensity;
float4 _Multiply_94df75e4ad734308a11d784250fc0b0c_Out_2_Vector4;
Unity_Multiply_float4_float4(_Lerp_3406aa7c52b14c818ac9eb76bf068e2d_Out_3_Vector4, (_Property_e8b3335851f84d91b28234460148cd0f_Out_0_Float.xxxx), _Multiply_94df75e4ad734308a11d784250fc0b0c_Out_2_Vector4);
float4 _Saturate_f0090edc5c664784a336737069276109_Out_1_Vector4;
Unity_Saturate_float4(_Multiply_94df75e4ad734308a11d784250fc0b0c_Out_2_Vector4, _Saturate_f0090edc5c664784a336737069276109_Out_1_Vector4);
float3 _MainLightCustomFunction_461af74b29b6456da89a2348455c2483_Direction_0_Vector3;
MainLight_float(_MainLightCustomFunction_461af74b29b6456da89a2348455c2483_Direction_0_Vector3);
float3 _Normalize_8644c278d597434b8871eb11a7a52cfe_Out_1_Vector3;
Unity_Normalize_float3(_MainLightCustomFunction_461af74b29b6456da89a2348455c2483_Direction_0_Vector3, _Normalize_8644c278d597434b8871eb11a7a52cfe_Out_1_Vector3);
float _Split_8a104625401f4a048b1160e17800fd11_R_1_Float = _Normalize_8644c278d597434b8871eb11a7a52cfe_Out_1_Vector3[0];
float _Split_8a104625401f4a048b1160e17800fd11_G_2_Float = _Normalize_8644c278d597434b8871eb11a7a52cfe_Out_1_Vector3[1];
float _Split_8a104625401f4a048b1160e17800fd11_B_3_Float = _Normalize_8644c278d597434b8871eb11a7a52cfe_Out_1_Vector3[2];
float _Split_8a104625401f4a048b1160e17800fd11_A_4_Float = 0;
float4 _Combine_87a90606ca4545b2983770732c0ed1b7_RGBA_4_Vector4;
float3 _Combine_87a90606ca4545b2983770732c0ed1b7_RGB_5_Vector3;
float2 _Combine_87a90606ca4545b2983770732c0ed1b7_RG_6_Vector2;
Unity_Combine_float(_Split_8a104625401f4a048b1160e17800fd11_R_1_Float, _Split_8a104625401f4a048b1160e17800fd11_B_3_Float, float(0), float(0), _Combine_87a90606ca4545b2983770732c0ed1b7_RGBA_4_Vector4, _Combine_87a90606ca4545b2983770732c0ed1b7_RGB_5_Vector3, _Combine_87a90606ca4545b2983770732c0ed1b7_RG_6_Vector2);
float3 _Normalize_f1ad7ea0af7b45ee88df143397ba6727_Out_1_Vector3;
Unity_Normalize_float3(-UNITY_MATRIX_V[2].xyz, _Normalize_f1ad7ea0af7b45ee88df143397ba6727_Out_1_Vector3);
float3 _Multiply_5b2d23749b2347b7af4472236c1dfe22_Out_2_Vector3;
Unity_Multiply_float3_float3(_Normalize_f1ad7ea0af7b45ee88df143397ba6727_Out_1_Vector3, float3(-1, -1, -1), _Multiply_5b2d23749b2347b7af4472236c1dfe22_Out_2_Vector3);
float _Split_6ebebe9410ea41bd8f8a327255848dc9_R_1_Float = _Multiply_5b2d23749b2347b7af4472236c1dfe22_Out_2_Vector3[0];
float _Split_6ebebe9410ea41bd8f8a327255848dc9_G_2_Float = _Multiply_5b2d23749b2347b7af4472236c1dfe22_Out_2_Vector3[1];
float _Split_6ebebe9410ea41bd8f8a327255848dc9_B_3_Float = _Multiply_5b2d23749b2347b7af4472236c1dfe22_Out_2_Vector3[2];
float _Split_6ebebe9410ea41bd8f8a327255848dc9_A_4_Float = 0;
float4 _Combine_ce9284087dd446409babf9c154f18fe4_RGBA_4_Vector4;
float3 _Combine_ce9284087dd446409babf9c154f18fe4_RGB_5_Vector3;
float2 _Combine_ce9284087dd446409babf9c154f18fe4_RG_6_Vector2;
Unity_Combine_float(_Split_6ebebe9410ea41bd8f8a327255848dc9_R_1_Float, _Split_6ebebe9410ea41bd8f8a327255848dc9_B_3_Float, float(0), float(0), _Combine_ce9284087dd446409babf9c154f18fe4_RGBA_4_Vector4, _Combine_ce9284087dd446409babf9c154f18fe4_RGB_5_Vector3, _Combine_ce9284087dd446409babf9c154f18fe4_RG_6_Vector2);
float _DotProduct_9e4f6525393a4171b6519e347a4b7a37_Out_2_Float;
Unity_DotProduct_float2(_Combine_87a90606ca4545b2983770732c0ed1b7_RG_6_Vector2, _Combine_ce9284087dd446409babf9c154f18fe4_RG_6_Vector2, _DotProduct_9e4f6525393a4171b6519e347a4b7a37_Out_2_Float);
float _Saturate_f76f8f4113e0468b9b20a6abf253d76e_Out_1_Float;
Unity_Saturate_float(_DotProduct_9e4f6525393a4171b6519e347a4b7a37_Out_2_Float, _Saturate_f76f8f4113e0468b9b20a6abf253d76e_Out_1_Float);
float _Property_bd14cfb26b074dbabde1a5a9b7f95d97_Out_0_Float = _Subsurface_Transition_Smoothness;
float _Power_2bc1d129029a4da69b2993ea07ff412f_Out_2_Float;
Unity_Power_float(_Saturate_f76f8f4113e0468b9b20a6abf253d76e_Out_1_Float, _Property_bd14cfb26b074dbabde1a5a9b7f95d97_Out_0_Float, _Power_2bc1d129029a4da69b2993ea07ff412f_Out_2_Float);
float _Saturate_44588f923e1b48ff97cd55ea2d69aa2b_Out_1_Float;
Unity_Saturate_float(_Power_2bc1d129029a4da69b2993ea07ff412f_Out_2_Float, _Saturate_44588f923e1b48ff97cd55ea2d69aa2b_Out_1_Float);
float4 _Lerp_98f83471835040419ec38e468d855fe3_Out_3_Vector4;
Unity_Lerp_float4(_Lerp_3406aa7c52b14c818ac9eb76bf068e2d_Out_3_Vector4, _Saturate_f0090edc5c664784a336737069276109_Out_1_Vector4, (_Saturate_44588f923e1b48ff97cd55ea2d69aa2b_Out_1_Float.xxxx), _Lerp_98f83471835040419ec38e468d855fe3_Out_3_Vector4);
float _Property_e00e00901e414a40a25484eb2c276584_Out_0_Float = _Subsurface_Fresnel_Intensity;
float4 _Multiply_6ed60680651f4e8cbff9e6f2ed97f0b9_Out_2_Vector4;
Unity_Multiply_float4_float4(_Lerp_98f83471835040419ec38e468d855fe3_Out_3_Vector4, (_Property_e00e00901e414a40a25484eb2c276584_Out_0_Float.xxxx), _Multiply_6ed60680651f4e8cbff9e6f2ed97f0b9_Out_2_Vector4);
float4 _Saturate_5cea81f1b9b5405fb4a571a97e57df52_Out_1_Vector4;
Unity_Saturate_float4(_Multiply_6ed60680651f4e8cbff9e6f2ed97f0b9_Out_2_Vector4, _Saturate_5cea81f1b9b5405fb4a571a97e57df52_Out_1_Vector4);
float _Saturate_10ad388197014e2fa2d0bd8261d82207_Out_1_Float;
Unity_Saturate_float(_DotProduct_9e4f6525393a4171b6519e347a4b7a37_Out_2_Float, _Saturate_10ad388197014e2fa2d0bd8261d82207_Out_1_Float);
float _Property_fe3bfd62823b45bf868d2c88982c1f64_Out_0_Float = _Subsurface_Fresnel_Transition_Smoothness;
float _Power_1ad124a5d5b74c13848df4f8f171d448_Out_2_Float;
Unity_Power_float(_Saturate_10ad388197014e2fa2d0bd8261d82207_Out_1_Float, _Property_fe3bfd62823b45bf868d2c88982c1f64_Out_0_Float, _Power_1ad124a5d5b74c13848df4f8f171d448_Out_2_Float);
float3 _Normalize_7e92e50b98274c508fa061a32d7a99b7_Out_1_Vector3;
Unity_Normalize_float3(IN.WorldSpaceViewDirection, _Normalize_7e92e50b98274c508fa061a32d7a99b7_Out_1_Vector3);
float _DotProduct_3170aef4b99740d083af85a5aa294edd_Out_2_Float;
Unity_DotProduct_float3(IN.WorldSpaceNormal, _Normalize_7e92e50b98274c508fa061a32d7a99b7_Out_1_Vector3, _DotProduct_3170aef4b99740d083af85a5aa294edd_Out_2_Float);
float _Absolute_111753c15e794f4b8e885c2c19d93d82_Out_1_Float;
Unity_Absolute_float(_DotProduct_3170aef4b99740d083af85a5aa294edd_Out_2_Float, _Absolute_111753c15e794f4b8e885c2c19d93d82_Out_1_Float);
float _OneMinus_2a02f0d378e1454c9aeda0ff447d4d86_Out_1_Float;
Unity_OneMinus_float(_Absolute_111753c15e794f4b8e885c2c19d93d82_Out_1_Float, _OneMinus_2a02f0d378e1454c9aeda0ff447d4d86_Out_1_Float);
float _Property_49c0203b77734a36b9ba06f7bcd723ee_Out_0_Float = _Subsurface_Fresnel_Pow;
float _Power_71121b48d5934b11a14aeadcad656816_Out_2_Float;
Unity_Power_float(_OneMinus_2a02f0d378e1454c9aeda0ff447d4d86_Out_1_Float, _Property_49c0203b77734a36b9ba06f7bcd723ee_Out_0_Float, _Power_71121b48d5934b11a14aeadcad656816_Out_2_Float);
float _Saturate_29e69580558f40c49a883332cdf25357_Out_1_Float;
Unity_Saturate_float(_Power_71121b48d5934b11a14aeadcad656816_Out_2_Float, _Saturate_29e69580558f40c49a883332cdf25357_Out_1_Float);
float _Multiply_328c604c9492493ba1dc1999ef6314fd_Out_2_Float;
Unity_Multiply_float_float(_Power_1ad124a5d5b74c13848df4f8f171d448_Out_2_Float, _Saturate_29e69580558f40c49a883332cdf25357_Out_1_Float, _Multiply_328c604c9492493ba1dc1999ef6314fd_Out_2_Float);
float4 _Lerp_10a8be366b974da7b00eab9c55f88a8a_Out_3_Vector4;
Unity_Lerp_float4(_Lerp_98f83471835040419ec38e468d855fe3_Out_3_Vector4, _Saturate_5cea81f1b9b5405fb4a571a97e57df52_Out_1_Vector4, (_Multiply_328c604c9492493ba1dc1999ef6314fd_Out_2_Float.xxxx), _Lerp_10a8be366b974da7b00eab9c55f88a8a_Out_3_Vector4);
float _Property_e5d876e9ad8e4cf1ab7db7d850c55d8c_Out_0_Float = _Roughness;
float _OneMinus_200145be127f401eae78bc05b8a1fc88_Out_1_Float;
Unity_OneMinus_float(_Property_e5d876e9ad8e4cf1ab7db7d850c55d8c_Out_0_Float, _OneMinus_200145be127f401eae78bc05b8a1fc88_Out_1_Float);
float _Multiply_cf1ea983d11e4ab7b4bdbbb064ad94b5_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_A_7_Float, 1, _Multiply_cf1ea983d11e4ab7b4bdbbb064ad94b5_Out_2_Float);
float _Property_99bb0b22edbb4ea0b1b7844414b876a7_Out_0_Float = _Alpha_Clip;
surface.BaseColor = (_Lerp_10a8be366b974da7b00eab9c55f88a8a_Out_3_Vector4.xyz);
surface.NormalTS = IN.TangentSpaceNormal;
surface.Emission = float3(0, 0, 0);
surface.Metallic = float(0);
surface.Smoothness = _OneMinus_200145be127f401eae78bc05b8a1fc88_Out_1_Float;
surface.Occlusion = float(1);
surface.Alpha = _Multiply_cf1ea983d11e4ab7b4bdbbb064ad94b5_Out_2_Float;
surface.AlphaClipThreshold = _Property_99bb0b22edbb4ea0b1b7844414b876a7_Out_0_Float;
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


    output.WorldSpaceViewDirection = GetWorldSpaceNormalizeViewDir(input.positionWS);
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
#pragma shader_feature_local _ _USE_WPO
#pragma shader_feature_local _ _USE_HEIGHT_GRADIENCE

#if defined(_USE_WPO) && defined(_USE_HEIGHT_GRADIENCE)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_WPO)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_HEIGHT_GRADIENCE)
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
 float3 WorldSpaceNormal;
 float3 TangentSpaceNormal;
 float3 WorldSpaceViewDirection;
 float3 WorldSpacePosition;
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 WorldSpacePosition;
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

float4 _Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D_TexelSize;
float _Subsurface_Fresnel_Transition_Smoothness;
float _Subsurface_Fresnel_Pow;
float _Subsurface_Fresnel_Intensity;
float _Subsurface_Transition_Smoothness;
float _Wiggle_Wind_Speed_Large;
float _Wiggle_Wind_Intensity;
float _Roughness;
float4 _Foliage_Color_Bottom;
float _Sway_Wind_Speed;
float _Gradience_Power;
float _Fresnel_Mul;
float4 _Foliage_Color_Top;
float _Always_On_Fresnel_Intensity;
float _Tree_Height;
float4 _Color_Map_TexelSize;
float _Height_Weight;
float _Height_Gradience;
float _Sway_Wind_Time_Random_Offset_Amount;
float _Sway_Wind_Intensity;
float3 _Sway_Wind_Direction;
float _Color_Map_Tiling;
float _Subsurface_Intensity;
float _Alpha_Clip;
float _Wiggle_Wind_Size;
float _Wiggle_Wind_Speed_Small;
float _Fresnel_Pow;
float _Always_On_Fresnel_Pow;
float4 _Always_On_Fresnel_Color;
float _ree;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D);
SAMPLER(sampler_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D);
TEXTURE2D(_Color_Map);
SAMPLER(sampler_Color_Map);

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

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Saturate_float4(float4 In, out float4 Out)
{
    Out = saturate(In);
}

void Unity_Normalize_float3(float3 In, out float3 Out)
{
    Out = normalize(In);
}

void Unity_DotProduct_float3(float3 A, float3 B, out float Out)
{
    Out = dot(A, B);
}

void Unity_Absolute_float(float In, out float Out)
{
    Out = abs(In);
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
}

// unity-custom-func-begin
void MainLight_float(out float3 Direction){

#ifdef UNITY_SHADER_GRAPH
	Direction = _WorldSpaceLightPos0.xyz;
#else
	Direction = float3(1,1,1);
#endif
}
// unity-custom-func-end

void Unity_DotProduct_float2(float2 A, float2 B, out float Out)
{
    Out = dot(A, B);
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
float _Property_7689f3a0523b4af9912879a05d1d5206_Out_0_Float = _Sway_Wind_Speed;
float3 _Property_e0531013c46845b6b036ac453118c744_Out_0_Vector3 = _Sway_Wind_Direction;
float _Property_35588e6a16b5457695dba39b1644d669_Out_0_Float = _Sway_Wind_Intensity;
float _Property_c8d3d7e6e6f64d409f010d988b96e327_Out_0_Float = _Sway_Wind_Time_Random_Offset_Amount;
Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float _WindSway_662bcd4f8b2b4607b138cb44de00a708;
_WindSway_662bcd4f8b2b4607b138cb44de00a708.ObjectSpacePosition = IN.ObjectSpacePosition;
_WindSway_662bcd4f8b2b4607b138cb44de00a708.TimeParameters = IN.TimeParameters;
float3 _WindSway_662bcd4f8b2b4607b138cb44de00a708_OutVector3_1_Vector3;
SG_WindSway_51c4027ae50a8cd49be53c4990b9f179_float(_Property_7689f3a0523b4af9912879a05d1d5206_Out_0_Float, _Property_e0531013c46845b6b036ac453118c744_Out_0_Vector3, _Property_35588e6a16b5457695dba39b1644d669_Out_0_Float, _Property_c8d3d7e6e6f64d409f010d988b96e327_Out_0_Float, _WindSway_662bcd4f8b2b4607b138cb44de00a708, _WindSway_662bcd4f8b2b4607b138cb44de00a708_OutVector3_1_Vector3);
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_R_1_Float = IN.WorldSpacePosition[0];
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_G_2_Float = IN.WorldSpacePosition[1];
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_B_3_Float = IN.WorldSpacePosition[2];
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_A_4_Float = 0;
float3 _Vector3_e154b524054546d2bbeab293c0e20519_Out_0_Vector3 = float3(float(0), float(0), float(0));
float3 _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3;
_Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3 = TransformObjectToWorld(_Vector3_e154b524054546d2bbeab293c0e20519_Out_0_Vector3.xyz);
float _Split_5eb48153bba44ea2b128583819f09a11_R_1_Float = _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3[0];
float _Split_5eb48153bba44ea2b128583819f09a11_G_2_Float = _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3[1];
float _Split_5eb48153bba44ea2b128583819f09a11_B_3_Float = _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3[2];
float _Split_5eb48153bba44ea2b128583819f09a11_A_4_Float = 0;
float _Subtract_d6a99abdf4e74644acffc1718d1735bf_Out_2_Float;
Unity_Subtract_float(_Split_8fe962eb6f0f4d6fba734f9edcaf9067_G_2_Float, _Split_5eb48153bba44ea2b128583819f09a11_G_2_Float, _Subtract_d6a99abdf4e74644acffc1718d1735bf_Out_2_Float);
float _Property_1cf3cda52e094e638df491b003607dfd_Out_0_Float = _Height_Weight;
float _Split_145cf2161d4c4510b08248cad819f655_R_1_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[0];
float _Split_145cf2161d4c4510b08248cad819f655_G_2_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[1];
float _Split_145cf2161d4c4510b08248cad819f655_B_3_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[2];
float _Split_145cf2161d4c4510b08248cad819f655_A_4_Float = 0;
float _Multiply_2c140d573b9447aab058bd863fe2f4f7_Out_2_Float;
Unity_Multiply_float_float(_Property_1cf3cda52e094e638df491b003607dfd_Out_0_Float, _Split_145cf2161d4c4510b08248cad819f655_G_2_Float, _Multiply_2c140d573b9447aab058bd863fe2f4f7_Out_2_Float);
float _Divide_0f6a637143d841d0b1c94fa2ae3e164d_Out_2_Float;
Unity_Divide_float(_Subtract_d6a99abdf4e74644acffc1718d1735bf_Out_2_Float, _Multiply_2c140d573b9447aab058bd863fe2f4f7_Out_2_Float, _Divide_0f6a637143d841d0b1c94fa2ae3e164d_Out_2_Float);
float _Saturate_1e4e29fb5c074565a1f941c046957b05_Out_1_Float;
Unity_Saturate_float(_Divide_0f6a637143d841d0b1c94fa2ae3e164d_Out_2_Float, _Saturate_1e4e29fb5c074565a1f941c046957b05_Out_1_Float);
float _Property_3d64a0c8dd104507b9a0762a778fe585_Out_0_Float = _Height_Gradience;
float _Power_f1d74728398a45f9993fe3b3affb6584_Out_2_Float;
Unity_Power_float(_Saturate_1e4e29fb5c074565a1f941c046957b05_Out_1_Float, _Property_3d64a0c8dd104507b9a0762a778fe585_Out_0_Float, _Power_f1d74728398a45f9993fe3b3affb6584_Out_2_Float);
float _Saturate_57b130350452418fa1d09d4a0ddd64ac_Out_1_Float;
Unity_Saturate_float(_Power_f1d74728398a45f9993fe3b3affb6584_Out_2_Float, _Saturate_57b130350452418fa1d09d4a0ddd64ac_Out_1_Float);
float3 _Lerp_1fbf8d0a7f5a4e2ab381967ac90385c7_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _WindSway_662bcd4f8b2b4607b138cb44de00a708_OutVector3_1_Vector3, (_Saturate_57b130350452418fa1d09d4a0ddd64ac_Out_1_Float.xxx), _Lerp_1fbf8d0a7f5a4e2ab381967ac90385c7_Out_3_Vector3);
float _Split_bd04916788224fa2bb71be3f27eb839a_R_1_Float = IN.WorldSpacePosition[0];
float _Split_bd04916788224fa2bb71be3f27eb839a_G_2_Float = IN.WorldSpacePosition[1];
float _Split_bd04916788224fa2bb71be3f27eb839a_B_3_Float = IN.WorldSpacePosition[2];
float _Split_bd04916788224fa2bb71be3f27eb839a_A_4_Float = 0;
float4 _Combine_45cdab9c20414d03862100967f56c78d_RGBA_4_Vector4;
float3 _Combine_45cdab9c20414d03862100967f56c78d_RGB_5_Vector3;
float2 _Combine_45cdab9c20414d03862100967f56c78d_RG_6_Vector2;
Unity_Combine_float(_Split_bd04916788224fa2bb71be3f27eb839a_R_1_Float, _Split_bd04916788224fa2bb71be3f27eb839a_B_3_Float, float(0), float(0), _Combine_45cdab9c20414d03862100967f56c78d_RGBA_4_Vector4, _Combine_45cdab9c20414d03862100967f56c78d_RGB_5_Vector3, _Combine_45cdab9c20414d03862100967f56c78d_RG_6_Vector2);
float _Property_9b35f6244c0247c7a9be7af2671f58e7_Out_0_Float = _Wiggle_Wind_Size;
float2 _Multiply_ba4d7f376f904143acc9eb33d8414ff8_Out_2_Vector2;
Unity_Multiply_float2_float2(_Combine_45cdab9c20414d03862100967f56c78d_RG_6_Vector2, (_Property_9b35f6244c0247c7a9be7af2671f58e7_Out_0_Float.xx), _Multiply_ba4d7f376f904143acc9eb33d8414ff8_Out_2_Vector2);
float _Property_6eae25110c184e7a94293b95ddc9ecef_Out_0_Float = _Wiggle_Wind_Speed_Small;
float _Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Property_6eae25110c184e7a94293b95ddc9ecef_Out_0_Float, _Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float);
float _Multiply_7510d5a95e6c4d3986bb83059d23d2fa_Out_2_Float;
Unity_Multiply_float_float(_Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float, 0.8, _Multiply_7510d5a95e6c4d3986bb83059d23d2fa_Out_2_Float);
float2 _Vector2_cb26bc8c681b455c81073f0dc3c8f6b4_Out_0_Vector2 = float2(_Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float, _Multiply_7510d5a95e6c4d3986bb83059d23d2fa_Out_2_Float);
float2 _TilingAndOffset_e5f47c2cf9d846859a72aadcdac7fc3b_Out_3_Vector2;
Unity_TilingAndOffset_float(_Multiply_ba4d7f376f904143acc9eb33d8414ff8_Out_2_Vector2, float2 (1, 1), _Vector2_cb26bc8c681b455c81073f0dc3c8f6b4_Out_0_Vector2, _TilingAndOffset_e5f47c2cf9d846859a72aadcdac7fc3b_Out_3_Vector2);
  float4 _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).tex, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).GetTransformedUV(_TilingAndOffset_e5f47c2cf9d846859a72aadcdac7fc3b_Out_3_Vector2), float(2));
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_R_5_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_G_6_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_B_7_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_A_8_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.a;
float _Float_3f1a7b5619414946b13e7cef19336d45_Out_0_Float = float(0.3);
float _Subtract_3105ef040f344481afb342cd9ef65e9f_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_R_5_Float, _Float_3f1a7b5619414946b13e7cef19336d45_Out_0_Float, _Subtract_3105ef040f344481afb342cd9ef65e9f_Out_2_Float);
float _Split_18495110217342bc80044d9345d98fbd_R_1_Float = IN.WorldSpacePosition[0];
float _Split_18495110217342bc80044d9345d98fbd_G_2_Float = IN.WorldSpacePosition[1];
float _Split_18495110217342bc80044d9345d98fbd_B_3_Float = IN.WorldSpacePosition[2];
float _Split_18495110217342bc80044d9345d98fbd_A_4_Float = 0;
float4 _Combine_d1ee3e49696840daa7cfd43df2287807_RGBA_4_Vector4;
float3 _Combine_d1ee3e49696840daa7cfd43df2287807_RGB_5_Vector3;
float2 _Combine_d1ee3e49696840daa7cfd43df2287807_RG_6_Vector2;
Unity_Combine_float(_Split_18495110217342bc80044d9345d98fbd_R_1_Float, _Split_18495110217342bc80044d9345d98fbd_G_2_Float, float(0), float(0), _Combine_d1ee3e49696840daa7cfd43df2287807_RGBA_4_Vector4, _Combine_d1ee3e49696840daa7cfd43df2287807_RGB_5_Vector3, _Combine_d1ee3e49696840daa7cfd43df2287807_RG_6_Vector2);
float _Property_9efe7c2851414ea39b6971daac6143e5_Out_0_Float = _Wiggle_Wind_Size;
float _Multiply_4f6b361114004341aeae12987f071375_Out_2_Float;
Unity_Multiply_float_float(_Property_9efe7c2851414ea39b6971daac6143e5_Out_0_Float, 0.763, _Multiply_4f6b361114004341aeae12987f071375_Out_2_Float);
float2 _Multiply_0d88e33790f0458b9e6e7f49946e6344_Out_2_Vector2;
Unity_Multiply_float2_float2(_Combine_d1ee3e49696840daa7cfd43df2287807_RG_6_Vector2, (_Multiply_4f6b361114004341aeae12987f071375_Out_2_Float.xx), _Multiply_0d88e33790f0458b9e6e7f49946e6344_Out_2_Vector2);
float _Property_56c994a79361438d88ea30b1e3ccf2aa_Out_0_Float = _Wiggle_Wind_Speed_Large;
float _Multiply_c5e5feed518642678424c6f0d45b4377_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Property_56c994a79361438d88ea30b1e3ccf2aa_Out_0_Float, _Multiply_c5e5feed518642678424c6f0d45b4377_Out_2_Float);
float2 _TilingAndOffset_7dac505987c144edad083d13ebca67e6_Out_3_Vector2;
Unity_TilingAndOffset_float(_Multiply_0d88e33790f0458b9e6e7f49946e6344_Out_2_Vector2, float2 (1, 1), (_Multiply_c5e5feed518642678424c6f0d45b4377_Out_2_Float.xx), _TilingAndOffset_7dac505987c144edad083d13ebca67e6_Out_3_Vector2);
  float4 _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).tex, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).GetTransformedUV(_TilingAndOffset_7dac505987c144edad083d13ebca67e6_Out_3_Vector2), float(2));
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_R_5_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_G_6_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_B_7_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_A_8_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.a;
float _Subtract_e29fd5b0daea41a1a38ac52280e6db88_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_G_6_Float, _Float_3f1a7b5619414946b13e7cef19336d45_Out_0_Float, _Subtract_e29fd5b0daea41a1a38ac52280e6db88_Out_2_Float);
float _Add_6d51be6104ee4e23a093dfb9c3d993f7_Out_2_Float;
Unity_Add_float(_Subtract_3105ef040f344481afb342cd9ef65e9f_Out_2_Float, _Subtract_e29fd5b0daea41a1a38ac52280e6db88_Out_2_Float, _Add_6d51be6104ee4e23a093dfb9c3d993f7_Out_2_Float);
float4 _Multiply_db043a8b3fc34edb8bee433085c7e061_Out_2_Vector4;
Unity_Multiply_float4_float4((_Add_6d51be6104ee4e23a093dfb9c3d993f7_Out_2_Float.xxxx), IN.VertexColor, _Multiply_db043a8b3fc34edb8bee433085c7e061_Out_2_Vector4);
float _Property_46c8c648052246a4a32e80905ed7e9df_Out_0_Float = _Wiggle_Wind_Intensity;
float4 _Multiply_4baa80310c414483a2aada72baa7a883_Out_2_Vector4;
Unity_Multiply_float4_float4(_Multiply_db043a8b3fc34edb8bee433085c7e061_Out_2_Vector4, (_Property_46c8c648052246a4a32e80905ed7e9df_Out_0_Float.xxxx), _Multiply_4baa80310c414483a2aada72baa7a883_Out_2_Vector4);
float3 _Multiply_2c95a93020c44a20be82a31b985c35b8_Out_2_Vector3;
Unity_Multiply_float3_float3((_Multiply_4baa80310c414483a2aada72baa7a883_Out_2_Vector4.xyz), IN.ObjectSpaceNormal, _Multiply_2c95a93020c44a20be82a31b985c35b8_Out_2_Vector3);
float3 _Add_5a143eeddea74bceb3ddd5b059f49da5_Out_2_Vector3;
Unity_Add_float3(_Lerp_1fbf8d0a7f5a4e2ab381967ac90385c7_Out_3_Vector3, _Multiply_2c95a93020c44a20be82a31b985c35b8_Out_2_Vector3, _Add_5a143eeddea74bceb3ddd5b059f49da5_Out_2_Vector3);
#if defined(_USE_WPO)
float3 _UseWPO_1f28bdd134c9432e812aded965021562_Out_0_Vector3 = _Add_5a143eeddea74bceb3ddd5b059f49da5_Out_2_Vector3;
#else
float3 _UseWPO_1f28bdd134c9432e812aded965021562_Out_0_Vector3 = IN.ObjectSpacePosition;
#endif
float3 _UPGFoliageContactPositionCustomFunction_3446fb1a0c894696bd903b7eb184e241_bentPositionOS_1_Vector3;
UPG_FoliageContactPosition_float(_UseWPO_1f28bdd134c9432e812aded965021562_Out_0_Vector3, _UPGFoliageContactPositionCustomFunction_3446fb1a0c894696bd903b7eb184e241_bentPositionOS_1_Vector3);
description.Position = _UPGFoliageContactPositionCustomFunction_3446fb1a0c894696bd903b7eb184e241_bentPositionOS_1_Vector3;
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
float4 _Property_a9c1982af1c74efb9ac25b0326ae7979_Out_0_Vector4 = _Foliage_Color_Bottom;
float4 _Property_50eb1cb960084765a0771df7f40a6d5d_Out_0_Vector4 = _Foliage_Color_Top;
float _Split_1540f44067c24640b4968355fa26a295_R_1_Float = IN.WorldSpacePosition[0];
float _Split_1540f44067c24640b4968355fa26a295_G_2_Float = IN.WorldSpacePosition[1];
float _Split_1540f44067c24640b4968355fa26a295_B_3_Float = IN.WorldSpacePosition[2];
float _Split_1540f44067c24640b4968355fa26a295_A_4_Float = 0;
float3 _Vector3_54c605479a824dd3a62abb1919f39c57_Out_0_Vector3 = float3(float(0), float(0), float(0));
float3 _Transform_f3f68ad0eeb04ce9ac459f2859353c06_Out_1_Vector3;
_Transform_f3f68ad0eeb04ce9ac459f2859353c06_Out_1_Vector3 = TransformObjectToWorld(_Vector3_54c605479a824dd3a62abb1919f39c57_Out_0_Vector3.xyz);
float _Split_15d5002ae66545318962a44958be8e00_R_1_Float = _Transform_f3f68ad0eeb04ce9ac459f2859353c06_Out_1_Vector3[0];
float _Split_15d5002ae66545318962a44958be8e00_G_2_Float = _Transform_f3f68ad0eeb04ce9ac459f2859353c06_Out_1_Vector3[1];
float _Split_15d5002ae66545318962a44958be8e00_B_3_Float = _Transform_f3f68ad0eeb04ce9ac459f2859353c06_Out_1_Vector3[2];
float _Split_15d5002ae66545318962a44958be8e00_A_4_Float = 0;
float _Subtract_3131eba61d024496834f2933dbde6855_Out_2_Float;
Unity_Subtract_float(_Split_1540f44067c24640b4968355fa26a295_G_2_Float, _Split_15d5002ae66545318962a44958be8e00_G_2_Float, _Subtract_3131eba61d024496834f2933dbde6855_Out_2_Float);
float _Property_605b997dac5a4228881635945f995c72_Out_0_Float = _Tree_Height;
float _Divide_8e4152d2ed4943778a44dc39efa65611_Out_2_Float;
Unity_Divide_float(_Subtract_3131eba61d024496834f2933dbde6855_Out_2_Float, _Property_605b997dac5a4228881635945f995c72_Out_0_Float, _Divide_8e4152d2ed4943778a44dc39efa65611_Out_2_Float);
float _Saturate_086c13233c3d43fab6a33a1e2ed498c2_Out_1_Float;
Unity_Saturate_float(_Divide_8e4152d2ed4943778a44dc39efa65611_Out_2_Float, _Saturate_086c13233c3d43fab6a33a1e2ed498c2_Out_1_Float);
float _Property_0e5bff248b2341909121bb16666f3468_Out_0_Float = _Gradience_Power;
float _Power_be0c59cc158d492f99ea24f3b1001e1f_Out_2_Float;
Unity_Power_float(_Saturate_086c13233c3d43fab6a33a1e2ed498c2_Out_1_Float, _Property_0e5bff248b2341909121bb16666f3468_Out_0_Float, _Power_be0c59cc158d492f99ea24f3b1001e1f_Out_2_Float);
float _Saturate_26bbc2447b5a402c9562a9e5d012fd6c_Out_1_Float;
Unity_Saturate_float(_Power_be0c59cc158d492f99ea24f3b1001e1f_Out_2_Float, _Saturate_26bbc2447b5a402c9562a9e5d012fd6c_Out_1_Float);
float4 _Lerp_795c581446834fe0bee35971bc8f0c5c_Out_3_Vector4;
Unity_Lerp_float4(_Property_a9c1982af1c74efb9ac25b0326ae7979_Out_0_Vector4, _Property_50eb1cb960084765a0771df7f40a6d5d_Out_0_Vector4, (_Saturate_26bbc2447b5a402c9562a9e5d012fd6c_Out_1_Float.xxxx), _Lerp_795c581446834fe0bee35971bc8f0c5c_Out_3_Vector4);
float4 _Property_4036fdaeaa4647adb3dd84d70ff1161e_Out_0_Vector4 = _Foliage_Color_Top;
#if defined(_USE_HEIGHT_GRADIENCE)
float4 _UseHeightGradience_d4326b9500c14f11a9090b7e8f107721_Out_0_Vector4 = _Lerp_795c581446834fe0bee35971bc8f0c5c_Out_3_Vector4;
#else
float4 _UseHeightGradience_d4326b9500c14f11a9090b7e8f107721_Out_0_Vector4 = _Property_4036fdaeaa4647adb3dd84d70ff1161e_Out_0_Vector4;
#endif
UnityTexture2D _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Color_Map, sampler_Color_Map, _Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_eaea7ba5e1e74519a1bc7833f0103ab8_Out_0_Vector4 = IN.uv0;
float _Property_95352575ae4140008287fd58138e8eb5_Out_0_Float = _Color_Map_Tiling;
float4 _Multiply_86fafeae394745fe880655d03cafd9e1_Out_2_Vector4;
Unity_Multiply_float4_float4(_UV_eaea7ba5e1e74519a1bc7833f0103ab8_Out_0_Vector4, (_Property_95352575ae4140008287fd58138e8eb5_Out_0_Float.xxxx), _Multiply_86fafeae394745fe880655d03cafd9e1_Out_2_Vector4);
float4 _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.tex, _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.samplerstate, _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.GetTransformedUV((_Multiply_86fafeae394745fe880655d03cafd9e1_Out_2_Vector4.xy)) );
if (_Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4, _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_R_4_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.r;
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_G_5_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.g;
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_B_6_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.b;
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_A_7_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.a;
float4 _Multiply_a05088f8e3434d54920d012fc47555a7_Out_2_Vector4;
Unity_Multiply_float4_float4(_UseHeightGradience_d4326b9500c14f11a9090b7e8f107721_Out_0_Vector4, _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4, _Multiply_a05088f8e3434d54920d012fc47555a7_Out_2_Vector4);
float4 _Property_ee5875e8bc214caba4dbea1511d06cd2_Out_0_Vector4 = _Always_On_Fresnel_Color;
float _Property_be2ed593ceb345f89b57d123af3db203_Out_0_Float = _Always_On_Fresnel_Intensity;
float4 _Multiply_dcf6e90c046c4f72b77b81df5b922468_Out_2_Vector4;
Unity_Multiply_float4_float4(_Property_ee5875e8bc214caba4dbea1511d06cd2_Out_0_Vector4, (_Property_be2ed593ceb345f89b57d123af3db203_Out_0_Float.xxxx), _Multiply_dcf6e90c046c4f72b77b81df5b922468_Out_2_Vector4);
float4 _Saturate_26f2848ceb0a494094065a9387275032_Out_1_Vector4;
Unity_Saturate_float4(_Multiply_dcf6e90c046c4f72b77b81df5b922468_Out_2_Vector4, _Saturate_26f2848ceb0a494094065a9387275032_Out_1_Vector4);
float3 _Normalize_764cced5d0894f3d89f5911d50d5f4a2_Out_1_Vector3;
Unity_Normalize_float3(IN.WorldSpaceViewDirection, _Normalize_764cced5d0894f3d89f5911d50d5f4a2_Out_1_Vector3);
float _DotProduct_eb716c3ca301415688a52e76b310a65c_Out_2_Float;
Unity_DotProduct_float3(IN.WorldSpaceNormal, _Normalize_764cced5d0894f3d89f5911d50d5f4a2_Out_1_Vector3, _DotProduct_eb716c3ca301415688a52e76b310a65c_Out_2_Float);
float _Absolute_470cf4f0298a44a695f47aa5ea61c0ab_Out_1_Float;
Unity_Absolute_float(_DotProduct_eb716c3ca301415688a52e76b310a65c_Out_2_Float, _Absolute_470cf4f0298a44a695f47aa5ea61c0ab_Out_1_Float);
float _OneMinus_fa832b719fc24001be421507d395a592_Out_1_Float;
Unity_OneMinus_float(_Absolute_470cf4f0298a44a695f47aa5ea61c0ab_Out_1_Float, _OneMinus_fa832b719fc24001be421507d395a592_Out_1_Float);
float _Property_d9cfcacb90204bdfa36d0c1b7c0da452_Out_0_Float = _Always_On_Fresnel_Pow;
float _Power_7841b1ae85554473b96cce132c7ff7e1_Out_2_Float;
Unity_Power_float(_OneMinus_fa832b719fc24001be421507d395a592_Out_1_Float, _Property_d9cfcacb90204bdfa36d0c1b7c0da452_Out_0_Float, _Power_7841b1ae85554473b96cce132c7ff7e1_Out_2_Float);
float _Saturate_908f6307f948471ca95c0e7507cd7e43_Out_1_Float;
Unity_Saturate_float(_Power_7841b1ae85554473b96cce132c7ff7e1_Out_2_Float, _Saturate_908f6307f948471ca95c0e7507cd7e43_Out_1_Float);
float4 _Lerp_3406aa7c52b14c818ac9eb76bf068e2d_Out_3_Vector4;
Unity_Lerp_float4(_Multiply_a05088f8e3434d54920d012fc47555a7_Out_2_Vector4, _Saturate_26f2848ceb0a494094065a9387275032_Out_1_Vector4, (_Saturate_908f6307f948471ca95c0e7507cd7e43_Out_1_Float.xxxx), _Lerp_3406aa7c52b14c818ac9eb76bf068e2d_Out_3_Vector4);
float _Property_e8b3335851f84d91b28234460148cd0f_Out_0_Float = _Subsurface_Intensity;
float4 _Multiply_94df75e4ad734308a11d784250fc0b0c_Out_2_Vector4;
Unity_Multiply_float4_float4(_Lerp_3406aa7c52b14c818ac9eb76bf068e2d_Out_3_Vector4, (_Property_e8b3335851f84d91b28234460148cd0f_Out_0_Float.xxxx), _Multiply_94df75e4ad734308a11d784250fc0b0c_Out_2_Vector4);
float4 _Saturate_f0090edc5c664784a336737069276109_Out_1_Vector4;
Unity_Saturate_float4(_Multiply_94df75e4ad734308a11d784250fc0b0c_Out_2_Vector4, _Saturate_f0090edc5c664784a336737069276109_Out_1_Vector4);
float3 _MainLightCustomFunction_461af74b29b6456da89a2348455c2483_Direction_0_Vector3;
MainLight_float(_MainLightCustomFunction_461af74b29b6456da89a2348455c2483_Direction_0_Vector3);
float3 _Normalize_8644c278d597434b8871eb11a7a52cfe_Out_1_Vector3;
Unity_Normalize_float3(_MainLightCustomFunction_461af74b29b6456da89a2348455c2483_Direction_0_Vector3, _Normalize_8644c278d597434b8871eb11a7a52cfe_Out_1_Vector3);
float _Split_8a104625401f4a048b1160e17800fd11_R_1_Float = _Normalize_8644c278d597434b8871eb11a7a52cfe_Out_1_Vector3[0];
float _Split_8a104625401f4a048b1160e17800fd11_G_2_Float = _Normalize_8644c278d597434b8871eb11a7a52cfe_Out_1_Vector3[1];
float _Split_8a104625401f4a048b1160e17800fd11_B_3_Float = _Normalize_8644c278d597434b8871eb11a7a52cfe_Out_1_Vector3[2];
float _Split_8a104625401f4a048b1160e17800fd11_A_4_Float = 0;
float4 _Combine_87a90606ca4545b2983770732c0ed1b7_RGBA_4_Vector4;
float3 _Combine_87a90606ca4545b2983770732c0ed1b7_RGB_5_Vector3;
float2 _Combine_87a90606ca4545b2983770732c0ed1b7_RG_6_Vector2;
Unity_Combine_float(_Split_8a104625401f4a048b1160e17800fd11_R_1_Float, _Split_8a104625401f4a048b1160e17800fd11_B_3_Float, float(0), float(0), _Combine_87a90606ca4545b2983770732c0ed1b7_RGBA_4_Vector4, _Combine_87a90606ca4545b2983770732c0ed1b7_RGB_5_Vector3, _Combine_87a90606ca4545b2983770732c0ed1b7_RG_6_Vector2);
float3 _Normalize_f1ad7ea0af7b45ee88df143397ba6727_Out_1_Vector3;
Unity_Normalize_float3(-UNITY_MATRIX_V[2].xyz, _Normalize_f1ad7ea0af7b45ee88df143397ba6727_Out_1_Vector3);
float3 _Multiply_5b2d23749b2347b7af4472236c1dfe22_Out_2_Vector3;
Unity_Multiply_float3_float3(_Normalize_f1ad7ea0af7b45ee88df143397ba6727_Out_1_Vector3, float3(-1, -1, -1), _Multiply_5b2d23749b2347b7af4472236c1dfe22_Out_2_Vector3);
float _Split_6ebebe9410ea41bd8f8a327255848dc9_R_1_Float = _Multiply_5b2d23749b2347b7af4472236c1dfe22_Out_2_Vector3[0];
float _Split_6ebebe9410ea41bd8f8a327255848dc9_G_2_Float = _Multiply_5b2d23749b2347b7af4472236c1dfe22_Out_2_Vector3[1];
float _Split_6ebebe9410ea41bd8f8a327255848dc9_B_3_Float = _Multiply_5b2d23749b2347b7af4472236c1dfe22_Out_2_Vector3[2];
float _Split_6ebebe9410ea41bd8f8a327255848dc9_A_4_Float = 0;
float4 _Combine_ce9284087dd446409babf9c154f18fe4_RGBA_4_Vector4;
float3 _Combine_ce9284087dd446409babf9c154f18fe4_RGB_5_Vector3;
float2 _Combine_ce9284087dd446409babf9c154f18fe4_RG_6_Vector2;
Unity_Combine_float(_Split_6ebebe9410ea41bd8f8a327255848dc9_R_1_Float, _Split_6ebebe9410ea41bd8f8a327255848dc9_B_3_Float, float(0), float(0), _Combine_ce9284087dd446409babf9c154f18fe4_RGBA_4_Vector4, _Combine_ce9284087dd446409babf9c154f18fe4_RGB_5_Vector3, _Combine_ce9284087dd446409babf9c154f18fe4_RG_6_Vector2);
float _DotProduct_9e4f6525393a4171b6519e347a4b7a37_Out_2_Float;
Unity_DotProduct_float2(_Combine_87a90606ca4545b2983770732c0ed1b7_RG_6_Vector2, _Combine_ce9284087dd446409babf9c154f18fe4_RG_6_Vector2, _DotProduct_9e4f6525393a4171b6519e347a4b7a37_Out_2_Float);
float _Saturate_f76f8f4113e0468b9b20a6abf253d76e_Out_1_Float;
Unity_Saturate_float(_DotProduct_9e4f6525393a4171b6519e347a4b7a37_Out_2_Float, _Saturate_f76f8f4113e0468b9b20a6abf253d76e_Out_1_Float);
float _Property_bd14cfb26b074dbabde1a5a9b7f95d97_Out_0_Float = _Subsurface_Transition_Smoothness;
float _Power_2bc1d129029a4da69b2993ea07ff412f_Out_2_Float;
Unity_Power_float(_Saturate_f76f8f4113e0468b9b20a6abf253d76e_Out_1_Float, _Property_bd14cfb26b074dbabde1a5a9b7f95d97_Out_0_Float, _Power_2bc1d129029a4da69b2993ea07ff412f_Out_2_Float);
float _Saturate_44588f923e1b48ff97cd55ea2d69aa2b_Out_1_Float;
Unity_Saturate_float(_Power_2bc1d129029a4da69b2993ea07ff412f_Out_2_Float, _Saturate_44588f923e1b48ff97cd55ea2d69aa2b_Out_1_Float);
float4 _Lerp_98f83471835040419ec38e468d855fe3_Out_3_Vector4;
Unity_Lerp_float4(_Lerp_3406aa7c52b14c818ac9eb76bf068e2d_Out_3_Vector4, _Saturate_f0090edc5c664784a336737069276109_Out_1_Vector4, (_Saturate_44588f923e1b48ff97cd55ea2d69aa2b_Out_1_Float.xxxx), _Lerp_98f83471835040419ec38e468d855fe3_Out_3_Vector4);
float _Property_e00e00901e414a40a25484eb2c276584_Out_0_Float = _Subsurface_Fresnel_Intensity;
float4 _Multiply_6ed60680651f4e8cbff9e6f2ed97f0b9_Out_2_Vector4;
Unity_Multiply_float4_float4(_Lerp_98f83471835040419ec38e468d855fe3_Out_3_Vector4, (_Property_e00e00901e414a40a25484eb2c276584_Out_0_Float.xxxx), _Multiply_6ed60680651f4e8cbff9e6f2ed97f0b9_Out_2_Vector4);
float4 _Saturate_5cea81f1b9b5405fb4a571a97e57df52_Out_1_Vector4;
Unity_Saturate_float4(_Multiply_6ed60680651f4e8cbff9e6f2ed97f0b9_Out_2_Vector4, _Saturate_5cea81f1b9b5405fb4a571a97e57df52_Out_1_Vector4);
float _Saturate_10ad388197014e2fa2d0bd8261d82207_Out_1_Float;
Unity_Saturate_float(_DotProduct_9e4f6525393a4171b6519e347a4b7a37_Out_2_Float, _Saturate_10ad388197014e2fa2d0bd8261d82207_Out_1_Float);
float _Property_fe3bfd62823b45bf868d2c88982c1f64_Out_0_Float = _Subsurface_Fresnel_Transition_Smoothness;
float _Power_1ad124a5d5b74c13848df4f8f171d448_Out_2_Float;
Unity_Power_float(_Saturate_10ad388197014e2fa2d0bd8261d82207_Out_1_Float, _Property_fe3bfd62823b45bf868d2c88982c1f64_Out_0_Float, _Power_1ad124a5d5b74c13848df4f8f171d448_Out_2_Float);
float3 _Normalize_7e92e50b98274c508fa061a32d7a99b7_Out_1_Vector3;
Unity_Normalize_float3(IN.WorldSpaceViewDirection, _Normalize_7e92e50b98274c508fa061a32d7a99b7_Out_1_Vector3);
float _DotProduct_3170aef4b99740d083af85a5aa294edd_Out_2_Float;
Unity_DotProduct_float3(IN.WorldSpaceNormal, _Normalize_7e92e50b98274c508fa061a32d7a99b7_Out_1_Vector3, _DotProduct_3170aef4b99740d083af85a5aa294edd_Out_2_Float);
float _Absolute_111753c15e794f4b8e885c2c19d93d82_Out_1_Float;
Unity_Absolute_float(_DotProduct_3170aef4b99740d083af85a5aa294edd_Out_2_Float, _Absolute_111753c15e794f4b8e885c2c19d93d82_Out_1_Float);
float _OneMinus_2a02f0d378e1454c9aeda0ff447d4d86_Out_1_Float;
Unity_OneMinus_float(_Absolute_111753c15e794f4b8e885c2c19d93d82_Out_1_Float, _OneMinus_2a02f0d378e1454c9aeda0ff447d4d86_Out_1_Float);
float _Property_49c0203b77734a36b9ba06f7bcd723ee_Out_0_Float = _Subsurface_Fresnel_Pow;
float _Power_71121b48d5934b11a14aeadcad656816_Out_2_Float;
Unity_Power_float(_OneMinus_2a02f0d378e1454c9aeda0ff447d4d86_Out_1_Float, _Property_49c0203b77734a36b9ba06f7bcd723ee_Out_0_Float, _Power_71121b48d5934b11a14aeadcad656816_Out_2_Float);
float _Saturate_29e69580558f40c49a883332cdf25357_Out_1_Float;
Unity_Saturate_float(_Power_71121b48d5934b11a14aeadcad656816_Out_2_Float, _Saturate_29e69580558f40c49a883332cdf25357_Out_1_Float);
float _Multiply_328c604c9492493ba1dc1999ef6314fd_Out_2_Float;
Unity_Multiply_float_float(_Power_1ad124a5d5b74c13848df4f8f171d448_Out_2_Float, _Saturate_29e69580558f40c49a883332cdf25357_Out_1_Float, _Multiply_328c604c9492493ba1dc1999ef6314fd_Out_2_Float);
float4 _Lerp_10a8be366b974da7b00eab9c55f88a8a_Out_3_Vector4;
Unity_Lerp_float4(_Lerp_98f83471835040419ec38e468d855fe3_Out_3_Vector4, _Saturate_5cea81f1b9b5405fb4a571a97e57df52_Out_1_Vector4, (_Multiply_328c604c9492493ba1dc1999ef6314fd_Out_2_Float.xxxx), _Lerp_10a8be366b974da7b00eab9c55f88a8a_Out_3_Vector4);
float _Property_e5d876e9ad8e4cf1ab7db7d850c55d8c_Out_0_Float = _Roughness;
float _OneMinus_200145be127f401eae78bc05b8a1fc88_Out_1_Float;
Unity_OneMinus_float(_Property_e5d876e9ad8e4cf1ab7db7d850c55d8c_Out_0_Float, _OneMinus_200145be127f401eae78bc05b8a1fc88_Out_1_Float);
float _Multiply_cf1ea983d11e4ab7b4bdbbb064ad94b5_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_A_7_Float, 1, _Multiply_cf1ea983d11e4ab7b4bdbbb064ad94b5_Out_2_Float);
float _Property_99bb0b22edbb4ea0b1b7844414b876a7_Out_0_Float = _Alpha_Clip;
surface.BaseColor = (_Lerp_10a8be366b974da7b00eab9c55f88a8a_Out_3_Vector4.xyz);
surface.NormalTS = IN.TangentSpaceNormal;
surface.Emission = float3(0, 0, 0);
surface.Metallic = float(0);
surface.Smoothness = _OneMinus_200145be127f401eae78bc05b8a1fc88_Out_1_Float;
surface.Occlusion = float(1);
surface.Alpha = _Multiply_cf1ea983d11e4ab7b4bdbbb064ad94b5_Out_2_Float;
surface.AlphaClipThreshold = _Property_99bb0b22edbb4ea0b1b7844414b876a7_Out_0_Float;
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


    output.WorldSpaceViewDirection = GetWorldSpaceNormalizeViewDir(input.positionWS);
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
#pragma shader_feature_local _ _USE_WPO
#pragma shader_feature_local _ _USE_HEIGHT_GRADIENCE

#if defined(_USE_WPO) && defined(_USE_HEIGHT_GRADIENCE)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_WPO)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_HEIGHT_GRADIENCE)
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

float4 _Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D_TexelSize;
float _Subsurface_Fresnel_Transition_Smoothness;
float _Subsurface_Fresnel_Pow;
float _Subsurface_Fresnel_Intensity;
float _Subsurface_Transition_Smoothness;
float _Wiggle_Wind_Speed_Large;
float _Wiggle_Wind_Intensity;
float _Roughness;
float4 _Foliage_Color_Bottom;
float _Sway_Wind_Speed;
float _Gradience_Power;
float _Fresnel_Mul;
float4 _Foliage_Color_Top;
float _Always_On_Fresnel_Intensity;
float _Tree_Height;
float4 _Color_Map_TexelSize;
float _Height_Weight;
float _Height_Gradience;
float _Sway_Wind_Time_Random_Offset_Amount;
float _Sway_Wind_Intensity;
float3 _Sway_Wind_Direction;
float _Color_Map_Tiling;
float _Subsurface_Intensity;
float _Alpha_Clip;
float _Wiggle_Wind_Size;
float _Wiggle_Wind_Speed_Small;
float _Fresnel_Pow;
float _Always_On_Fresnel_Pow;
float4 _Always_On_Fresnel_Color;
float _ree;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D);
SAMPLER(sampler_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D);
TEXTURE2D(_Color_Map);
SAMPLER(sampler_Color_Map);

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

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
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
float _Property_7689f3a0523b4af9912879a05d1d5206_Out_0_Float = _Sway_Wind_Speed;
float3 _Property_e0531013c46845b6b036ac453118c744_Out_0_Vector3 = _Sway_Wind_Direction;
float _Property_35588e6a16b5457695dba39b1644d669_Out_0_Float = _Sway_Wind_Intensity;
float _Property_c8d3d7e6e6f64d409f010d988b96e327_Out_0_Float = _Sway_Wind_Time_Random_Offset_Amount;
Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float _WindSway_662bcd4f8b2b4607b138cb44de00a708;
_WindSway_662bcd4f8b2b4607b138cb44de00a708.ObjectSpacePosition = IN.ObjectSpacePosition;
_WindSway_662bcd4f8b2b4607b138cb44de00a708.TimeParameters = IN.TimeParameters;
float3 _WindSway_662bcd4f8b2b4607b138cb44de00a708_OutVector3_1_Vector3;
SG_WindSway_51c4027ae50a8cd49be53c4990b9f179_float(_Property_7689f3a0523b4af9912879a05d1d5206_Out_0_Float, _Property_e0531013c46845b6b036ac453118c744_Out_0_Vector3, _Property_35588e6a16b5457695dba39b1644d669_Out_0_Float, _Property_c8d3d7e6e6f64d409f010d988b96e327_Out_0_Float, _WindSway_662bcd4f8b2b4607b138cb44de00a708, _WindSway_662bcd4f8b2b4607b138cb44de00a708_OutVector3_1_Vector3);
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_R_1_Float = IN.WorldSpacePosition[0];
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_G_2_Float = IN.WorldSpacePosition[1];
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_B_3_Float = IN.WorldSpacePosition[2];
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_A_4_Float = 0;
float3 _Vector3_e154b524054546d2bbeab293c0e20519_Out_0_Vector3 = float3(float(0), float(0), float(0));
float3 _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3;
_Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3 = TransformObjectToWorld(_Vector3_e154b524054546d2bbeab293c0e20519_Out_0_Vector3.xyz);
float _Split_5eb48153bba44ea2b128583819f09a11_R_1_Float = _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3[0];
float _Split_5eb48153bba44ea2b128583819f09a11_G_2_Float = _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3[1];
float _Split_5eb48153bba44ea2b128583819f09a11_B_3_Float = _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3[2];
float _Split_5eb48153bba44ea2b128583819f09a11_A_4_Float = 0;
float _Subtract_d6a99abdf4e74644acffc1718d1735bf_Out_2_Float;
Unity_Subtract_float(_Split_8fe962eb6f0f4d6fba734f9edcaf9067_G_2_Float, _Split_5eb48153bba44ea2b128583819f09a11_G_2_Float, _Subtract_d6a99abdf4e74644acffc1718d1735bf_Out_2_Float);
float _Property_1cf3cda52e094e638df491b003607dfd_Out_0_Float = _Height_Weight;
float _Split_145cf2161d4c4510b08248cad819f655_R_1_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[0];
float _Split_145cf2161d4c4510b08248cad819f655_G_2_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[1];
float _Split_145cf2161d4c4510b08248cad819f655_B_3_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[2];
float _Split_145cf2161d4c4510b08248cad819f655_A_4_Float = 0;
float _Multiply_2c140d573b9447aab058bd863fe2f4f7_Out_2_Float;
Unity_Multiply_float_float(_Property_1cf3cda52e094e638df491b003607dfd_Out_0_Float, _Split_145cf2161d4c4510b08248cad819f655_G_2_Float, _Multiply_2c140d573b9447aab058bd863fe2f4f7_Out_2_Float);
float _Divide_0f6a637143d841d0b1c94fa2ae3e164d_Out_2_Float;
Unity_Divide_float(_Subtract_d6a99abdf4e74644acffc1718d1735bf_Out_2_Float, _Multiply_2c140d573b9447aab058bd863fe2f4f7_Out_2_Float, _Divide_0f6a637143d841d0b1c94fa2ae3e164d_Out_2_Float);
float _Saturate_1e4e29fb5c074565a1f941c046957b05_Out_1_Float;
Unity_Saturate_float(_Divide_0f6a637143d841d0b1c94fa2ae3e164d_Out_2_Float, _Saturate_1e4e29fb5c074565a1f941c046957b05_Out_1_Float);
float _Property_3d64a0c8dd104507b9a0762a778fe585_Out_0_Float = _Height_Gradience;
float _Power_f1d74728398a45f9993fe3b3affb6584_Out_2_Float;
Unity_Power_float(_Saturate_1e4e29fb5c074565a1f941c046957b05_Out_1_Float, _Property_3d64a0c8dd104507b9a0762a778fe585_Out_0_Float, _Power_f1d74728398a45f9993fe3b3affb6584_Out_2_Float);
float _Saturate_57b130350452418fa1d09d4a0ddd64ac_Out_1_Float;
Unity_Saturate_float(_Power_f1d74728398a45f9993fe3b3affb6584_Out_2_Float, _Saturate_57b130350452418fa1d09d4a0ddd64ac_Out_1_Float);
float3 _Lerp_1fbf8d0a7f5a4e2ab381967ac90385c7_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _WindSway_662bcd4f8b2b4607b138cb44de00a708_OutVector3_1_Vector3, (_Saturate_57b130350452418fa1d09d4a0ddd64ac_Out_1_Float.xxx), _Lerp_1fbf8d0a7f5a4e2ab381967ac90385c7_Out_3_Vector3);
float _Split_bd04916788224fa2bb71be3f27eb839a_R_1_Float = IN.WorldSpacePosition[0];
float _Split_bd04916788224fa2bb71be3f27eb839a_G_2_Float = IN.WorldSpacePosition[1];
float _Split_bd04916788224fa2bb71be3f27eb839a_B_3_Float = IN.WorldSpacePosition[2];
float _Split_bd04916788224fa2bb71be3f27eb839a_A_4_Float = 0;
float4 _Combine_45cdab9c20414d03862100967f56c78d_RGBA_4_Vector4;
float3 _Combine_45cdab9c20414d03862100967f56c78d_RGB_5_Vector3;
float2 _Combine_45cdab9c20414d03862100967f56c78d_RG_6_Vector2;
Unity_Combine_float(_Split_bd04916788224fa2bb71be3f27eb839a_R_1_Float, _Split_bd04916788224fa2bb71be3f27eb839a_B_3_Float, float(0), float(0), _Combine_45cdab9c20414d03862100967f56c78d_RGBA_4_Vector4, _Combine_45cdab9c20414d03862100967f56c78d_RGB_5_Vector3, _Combine_45cdab9c20414d03862100967f56c78d_RG_6_Vector2);
float _Property_9b35f6244c0247c7a9be7af2671f58e7_Out_0_Float = _Wiggle_Wind_Size;
float2 _Multiply_ba4d7f376f904143acc9eb33d8414ff8_Out_2_Vector2;
Unity_Multiply_float2_float2(_Combine_45cdab9c20414d03862100967f56c78d_RG_6_Vector2, (_Property_9b35f6244c0247c7a9be7af2671f58e7_Out_0_Float.xx), _Multiply_ba4d7f376f904143acc9eb33d8414ff8_Out_2_Vector2);
float _Property_6eae25110c184e7a94293b95ddc9ecef_Out_0_Float = _Wiggle_Wind_Speed_Small;
float _Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Property_6eae25110c184e7a94293b95ddc9ecef_Out_0_Float, _Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float);
float _Multiply_7510d5a95e6c4d3986bb83059d23d2fa_Out_2_Float;
Unity_Multiply_float_float(_Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float, 0.8, _Multiply_7510d5a95e6c4d3986bb83059d23d2fa_Out_2_Float);
float2 _Vector2_cb26bc8c681b455c81073f0dc3c8f6b4_Out_0_Vector2 = float2(_Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float, _Multiply_7510d5a95e6c4d3986bb83059d23d2fa_Out_2_Float);
float2 _TilingAndOffset_e5f47c2cf9d846859a72aadcdac7fc3b_Out_3_Vector2;
Unity_TilingAndOffset_float(_Multiply_ba4d7f376f904143acc9eb33d8414ff8_Out_2_Vector2, float2 (1, 1), _Vector2_cb26bc8c681b455c81073f0dc3c8f6b4_Out_0_Vector2, _TilingAndOffset_e5f47c2cf9d846859a72aadcdac7fc3b_Out_3_Vector2);
  float4 _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).tex, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).GetTransformedUV(_TilingAndOffset_e5f47c2cf9d846859a72aadcdac7fc3b_Out_3_Vector2), float(2));
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_R_5_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_G_6_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_B_7_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_A_8_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.a;
float _Float_3f1a7b5619414946b13e7cef19336d45_Out_0_Float = float(0.3);
float _Subtract_3105ef040f344481afb342cd9ef65e9f_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_R_5_Float, _Float_3f1a7b5619414946b13e7cef19336d45_Out_0_Float, _Subtract_3105ef040f344481afb342cd9ef65e9f_Out_2_Float);
float _Split_18495110217342bc80044d9345d98fbd_R_1_Float = IN.WorldSpacePosition[0];
float _Split_18495110217342bc80044d9345d98fbd_G_2_Float = IN.WorldSpacePosition[1];
float _Split_18495110217342bc80044d9345d98fbd_B_3_Float = IN.WorldSpacePosition[2];
float _Split_18495110217342bc80044d9345d98fbd_A_4_Float = 0;
float4 _Combine_d1ee3e49696840daa7cfd43df2287807_RGBA_4_Vector4;
float3 _Combine_d1ee3e49696840daa7cfd43df2287807_RGB_5_Vector3;
float2 _Combine_d1ee3e49696840daa7cfd43df2287807_RG_6_Vector2;
Unity_Combine_float(_Split_18495110217342bc80044d9345d98fbd_R_1_Float, _Split_18495110217342bc80044d9345d98fbd_G_2_Float, float(0), float(0), _Combine_d1ee3e49696840daa7cfd43df2287807_RGBA_4_Vector4, _Combine_d1ee3e49696840daa7cfd43df2287807_RGB_5_Vector3, _Combine_d1ee3e49696840daa7cfd43df2287807_RG_6_Vector2);
float _Property_9efe7c2851414ea39b6971daac6143e5_Out_0_Float = _Wiggle_Wind_Size;
float _Multiply_4f6b361114004341aeae12987f071375_Out_2_Float;
Unity_Multiply_float_float(_Property_9efe7c2851414ea39b6971daac6143e5_Out_0_Float, 0.763, _Multiply_4f6b361114004341aeae12987f071375_Out_2_Float);
float2 _Multiply_0d88e33790f0458b9e6e7f49946e6344_Out_2_Vector2;
Unity_Multiply_float2_float2(_Combine_d1ee3e49696840daa7cfd43df2287807_RG_6_Vector2, (_Multiply_4f6b361114004341aeae12987f071375_Out_2_Float.xx), _Multiply_0d88e33790f0458b9e6e7f49946e6344_Out_2_Vector2);
float _Property_56c994a79361438d88ea30b1e3ccf2aa_Out_0_Float = _Wiggle_Wind_Speed_Large;
float _Multiply_c5e5feed518642678424c6f0d45b4377_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Property_56c994a79361438d88ea30b1e3ccf2aa_Out_0_Float, _Multiply_c5e5feed518642678424c6f0d45b4377_Out_2_Float);
float2 _TilingAndOffset_7dac505987c144edad083d13ebca67e6_Out_3_Vector2;
Unity_TilingAndOffset_float(_Multiply_0d88e33790f0458b9e6e7f49946e6344_Out_2_Vector2, float2 (1, 1), (_Multiply_c5e5feed518642678424c6f0d45b4377_Out_2_Float.xx), _TilingAndOffset_7dac505987c144edad083d13ebca67e6_Out_3_Vector2);
  float4 _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).tex, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).GetTransformedUV(_TilingAndOffset_7dac505987c144edad083d13ebca67e6_Out_3_Vector2), float(2));
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_R_5_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_G_6_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_B_7_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_A_8_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.a;
float _Subtract_e29fd5b0daea41a1a38ac52280e6db88_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_G_6_Float, _Float_3f1a7b5619414946b13e7cef19336d45_Out_0_Float, _Subtract_e29fd5b0daea41a1a38ac52280e6db88_Out_2_Float);
float _Add_6d51be6104ee4e23a093dfb9c3d993f7_Out_2_Float;
Unity_Add_float(_Subtract_3105ef040f344481afb342cd9ef65e9f_Out_2_Float, _Subtract_e29fd5b0daea41a1a38ac52280e6db88_Out_2_Float, _Add_6d51be6104ee4e23a093dfb9c3d993f7_Out_2_Float);
float4 _Multiply_db043a8b3fc34edb8bee433085c7e061_Out_2_Vector4;
Unity_Multiply_float4_float4((_Add_6d51be6104ee4e23a093dfb9c3d993f7_Out_2_Float.xxxx), IN.VertexColor, _Multiply_db043a8b3fc34edb8bee433085c7e061_Out_2_Vector4);
float _Property_46c8c648052246a4a32e80905ed7e9df_Out_0_Float = _Wiggle_Wind_Intensity;
float4 _Multiply_4baa80310c414483a2aada72baa7a883_Out_2_Vector4;
Unity_Multiply_float4_float4(_Multiply_db043a8b3fc34edb8bee433085c7e061_Out_2_Vector4, (_Property_46c8c648052246a4a32e80905ed7e9df_Out_0_Float.xxxx), _Multiply_4baa80310c414483a2aada72baa7a883_Out_2_Vector4);
float3 _Multiply_2c95a93020c44a20be82a31b985c35b8_Out_2_Vector3;
Unity_Multiply_float3_float3((_Multiply_4baa80310c414483a2aada72baa7a883_Out_2_Vector4.xyz), IN.ObjectSpaceNormal, _Multiply_2c95a93020c44a20be82a31b985c35b8_Out_2_Vector3);
float3 _Add_5a143eeddea74bceb3ddd5b059f49da5_Out_2_Vector3;
Unity_Add_float3(_Lerp_1fbf8d0a7f5a4e2ab381967ac90385c7_Out_3_Vector3, _Multiply_2c95a93020c44a20be82a31b985c35b8_Out_2_Vector3, _Add_5a143eeddea74bceb3ddd5b059f49da5_Out_2_Vector3);
#if defined(_USE_WPO)
float3 _UseWPO_1f28bdd134c9432e812aded965021562_Out_0_Vector3 = _Add_5a143eeddea74bceb3ddd5b059f49da5_Out_2_Vector3;
#else
float3 _UseWPO_1f28bdd134c9432e812aded965021562_Out_0_Vector3 = IN.ObjectSpacePosition;
#endif
float3 _UPGFoliageContactPositionCustomFunction_3446fb1a0c894696bd903b7eb184e241_bentPositionOS_1_Vector3;
UPG_FoliageContactPosition_float(_UseWPO_1f28bdd134c9432e812aded965021562_Out_0_Vector3, _UPGFoliageContactPositionCustomFunction_3446fb1a0c894696bd903b7eb184e241_bentPositionOS_1_Vector3);
description.Position = _UPGFoliageContactPositionCustomFunction_3446fb1a0c894696bd903b7eb184e241_bentPositionOS_1_Vector3;
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
UnityTexture2D _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Color_Map, sampler_Color_Map, _Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_eaea7ba5e1e74519a1bc7833f0103ab8_Out_0_Vector4 = IN.uv0;
float _Property_95352575ae4140008287fd58138e8eb5_Out_0_Float = _Color_Map_Tiling;
float4 _Multiply_86fafeae394745fe880655d03cafd9e1_Out_2_Vector4;
Unity_Multiply_float4_float4(_UV_eaea7ba5e1e74519a1bc7833f0103ab8_Out_0_Vector4, (_Property_95352575ae4140008287fd58138e8eb5_Out_0_Float.xxxx), _Multiply_86fafeae394745fe880655d03cafd9e1_Out_2_Vector4);
float4 _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.tex, _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.samplerstate, _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.GetTransformedUV((_Multiply_86fafeae394745fe880655d03cafd9e1_Out_2_Vector4.xy)) );
if (_Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4, _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_R_4_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.r;
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_G_5_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.g;
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_B_6_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.b;
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_A_7_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.a;
float _Multiply_cf1ea983d11e4ab7b4bdbbb064ad94b5_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_A_7_Float, 1, _Multiply_cf1ea983d11e4ab7b4bdbbb064ad94b5_Out_2_Float);
float _Property_99bb0b22edbb4ea0b1b7844414b876a7_Out_0_Float = _Alpha_Clip;
surface.Alpha = _Multiply_cf1ea983d11e4ab7b4bdbbb064ad94b5_Out_2_Float;
surface.AlphaClipThreshold = _Property_99bb0b22edbb4ea0b1b7844414b876a7_Out_0_Float;
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
#pragma shader_feature_local _ _USE_WPO
#pragma shader_feature_local _ _USE_HEIGHT_GRADIENCE

#if defined(_USE_WPO) && defined(_USE_HEIGHT_GRADIENCE)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_WPO)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_HEIGHT_GRADIENCE)
#define KEYWORD_PERMUTATION_2
#else
#define KEYWORD_PERMUTATION_3
#endif


// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_COLOR
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
 float3 ObjectSpacePosition;
 float3 WorldSpacePosition;
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

float4 _Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D_TexelSize;
float _Subsurface_Fresnel_Transition_Smoothness;
float _Subsurface_Fresnel_Pow;
float _Subsurface_Fresnel_Intensity;
float _Subsurface_Transition_Smoothness;
float _Wiggle_Wind_Speed_Large;
float _Wiggle_Wind_Intensity;
float _Roughness;
float4 _Foliage_Color_Bottom;
float _Sway_Wind_Speed;
float _Gradience_Power;
float _Fresnel_Mul;
float4 _Foliage_Color_Top;
float _Always_On_Fresnel_Intensity;
float _Tree_Height;
float4 _Color_Map_TexelSize;
float _Height_Weight;
float _Height_Gradience;
float _Sway_Wind_Time_Random_Offset_Amount;
float _Sway_Wind_Intensity;
float3 _Sway_Wind_Direction;
float _Color_Map_Tiling;
float _Subsurface_Intensity;
float _Alpha_Clip;
float _Wiggle_Wind_Size;
float _Wiggle_Wind_Speed_Small;
float _Fresnel_Pow;
float _Always_On_Fresnel_Pow;
float4 _Always_On_Fresnel_Color;
float _ree;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D);
SAMPLER(sampler_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D);
TEXTURE2D(_Color_Map);
SAMPLER(sampler_Color_Map);

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

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
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
};

VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
float _Property_7689f3a0523b4af9912879a05d1d5206_Out_0_Float = _Sway_Wind_Speed;
float3 _Property_e0531013c46845b6b036ac453118c744_Out_0_Vector3 = _Sway_Wind_Direction;
float _Property_35588e6a16b5457695dba39b1644d669_Out_0_Float = _Sway_Wind_Intensity;
float _Property_c8d3d7e6e6f64d409f010d988b96e327_Out_0_Float = _Sway_Wind_Time_Random_Offset_Amount;
Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float _WindSway_662bcd4f8b2b4607b138cb44de00a708;
_WindSway_662bcd4f8b2b4607b138cb44de00a708.ObjectSpacePosition = IN.ObjectSpacePosition;
_WindSway_662bcd4f8b2b4607b138cb44de00a708.TimeParameters = IN.TimeParameters;
float3 _WindSway_662bcd4f8b2b4607b138cb44de00a708_OutVector3_1_Vector3;
SG_WindSway_51c4027ae50a8cd49be53c4990b9f179_float(_Property_7689f3a0523b4af9912879a05d1d5206_Out_0_Float, _Property_e0531013c46845b6b036ac453118c744_Out_0_Vector3, _Property_35588e6a16b5457695dba39b1644d669_Out_0_Float, _Property_c8d3d7e6e6f64d409f010d988b96e327_Out_0_Float, _WindSway_662bcd4f8b2b4607b138cb44de00a708, _WindSway_662bcd4f8b2b4607b138cb44de00a708_OutVector3_1_Vector3);
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_R_1_Float = IN.WorldSpacePosition[0];
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_G_2_Float = IN.WorldSpacePosition[1];
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_B_3_Float = IN.WorldSpacePosition[2];
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_A_4_Float = 0;
float3 _Vector3_e154b524054546d2bbeab293c0e20519_Out_0_Vector3 = float3(float(0), float(0), float(0));
float3 _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3;
_Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3 = TransformObjectToWorld(_Vector3_e154b524054546d2bbeab293c0e20519_Out_0_Vector3.xyz);
float _Split_5eb48153bba44ea2b128583819f09a11_R_1_Float = _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3[0];
float _Split_5eb48153bba44ea2b128583819f09a11_G_2_Float = _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3[1];
float _Split_5eb48153bba44ea2b128583819f09a11_B_3_Float = _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3[2];
float _Split_5eb48153bba44ea2b128583819f09a11_A_4_Float = 0;
float _Subtract_d6a99abdf4e74644acffc1718d1735bf_Out_2_Float;
Unity_Subtract_float(_Split_8fe962eb6f0f4d6fba734f9edcaf9067_G_2_Float, _Split_5eb48153bba44ea2b128583819f09a11_G_2_Float, _Subtract_d6a99abdf4e74644acffc1718d1735bf_Out_2_Float);
float _Property_1cf3cda52e094e638df491b003607dfd_Out_0_Float = _Height_Weight;
float _Split_145cf2161d4c4510b08248cad819f655_R_1_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[0];
float _Split_145cf2161d4c4510b08248cad819f655_G_2_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[1];
float _Split_145cf2161d4c4510b08248cad819f655_B_3_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[2];
float _Split_145cf2161d4c4510b08248cad819f655_A_4_Float = 0;
float _Multiply_2c140d573b9447aab058bd863fe2f4f7_Out_2_Float;
Unity_Multiply_float_float(_Property_1cf3cda52e094e638df491b003607dfd_Out_0_Float, _Split_145cf2161d4c4510b08248cad819f655_G_2_Float, _Multiply_2c140d573b9447aab058bd863fe2f4f7_Out_2_Float);
float _Divide_0f6a637143d841d0b1c94fa2ae3e164d_Out_2_Float;
Unity_Divide_float(_Subtract_d6a99abdf4e74644acffc1718d1735bf_Out_2_Float, _Multiply_2c140d573b9447aab058bd863fe2f4f7_Out_2_Float, _Divide_0f6a637143d841d0b1c94fa2ae3e164d_Out_2_Float);
float _Saturate_1e4e29fb5c074565a1f941c046957b05_Out_1_Float;
Unity_Saturate_float(_Divide_0f6a637143d841d0b1c94fa2ae3e164d_Out_2_Float, _Saturate_1e4e29fb5c074565a1f941c046957b05_Out_1_Float);
float _Property_3d64a0c8dd104507b9a0762a778fe585_Out_0_Float = _Height_Gradience;
float _Power_f1d74728398a45f9993fe3b3affb6584_Out_2_Float;
Unity_Power_float(_Saturate_1e4e29fb5c074565a1f941c046957b05_Out_1_Float, _Property_3d64a0c8dd104507b9a0762a778fe585_Out_0_Float, _Power_f1d74728398a45f9993fe3b3affb6584_Out_2_Float);
float _Saturate_57b130350452418fa1d09d4a0ddd64ac_Out_1_Float;
Unity_Saturate_float(_Power_f1d74728398a45f9993fe3b3affb6584_Out_2_Float, _Saturate_57b130350452418fa1d09d4a0ddd64ac_Out_1_Float);
float3 _Lerp_1fbf8d0a7f5a4e2ab381967ac90385c7_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _WindSway_662bcd4f8b2b4607b138cb44de00a708_OutVector3_1_Vector3, (_Saturate_57b130350452418fa1d09d4a0ddd64ac_Out_1_Float.xxx), _Lerp_1fbf8d0a7f5a4e2ab381967ac90385c7_Out_3_Vector3);
float _Split_bd04916788224fa2bb71be3f27eb839a_R_1_Float = IN.WorldSpacePosition[0];
float _Split_bd04916788224fa2bb71be3f27eb839a_G_2_Float = IN.WorldSpacePosition[1];
float _Split_bd04916788224fa2bb71be3f27eb839a_B_3_Float = IN.WorldSpacePosition[2];
float _Split_bd04916788224fa2bb71be3f27eb839a_A_4_Float = 0;
float4 _Combine_45cdab9c20414d03862100967f56c78d_RGBA_4_Vector4;
float3 _Combine_45cdab9c20414d03862100967f56c78d_RGB_5_Vector3;
float2 _Combine_45cdab9c20414d03862100967f56c78d_RG_6_Vector2;
Unity_Combine_float(_Split_bd04916788224fa2bb71be3f27eb839a_R_1_Float, _Split_bd04916788224fa2bb71be3f27eb839a_B_3_Float, float(0), float(0), _Combine_45cdab9c20414d03862100967f56c78d_RGBA_4_Vector4, _Combine_45cdab9c20414d03862100967f56c78d_RGB_5_Vector3, _Combine_45cdab9c20414d03862100967f56c78d_RG_6_Vector2);
float _Property_9b35f6244c0247c7a9be7af2671f58e7_Out_0_Float = _Wiggle_Wind_Size;
float2 _Multiply_ba4d7f376f904143acc9eb33d8414ff8_Out_2_Vector2;
Unity_Multiply_float2_float2(_Combine_45cdab9c20414d03862100967f56c78d_RG_6_Vector2, (_Property_9b35f6244c0247c7a9be7af2671f58e7_Out_0_Float.xx), _Multiply_ba4d7f376f904143acc9eb33d8414ff8_Out_2_Vector2);
float _Property_6eae25110c184e7a94293b95ddc9ecef_Out_0_Float = _Wiggle_Wind_Speed_Small;
float _Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Property_6eae25110c184e7a94293b95ddc9ecef_Out_0_Float, _Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float);
float _Multiply_7510d5a95e6c4d3986bb83059d23d2fa_Out_2_Float;
Unity_Multiply_float_float(_Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float, 0.8, _Multiply_7510d5a95e6c4d3986bb83059d23d2fa_Out_2_Float);
float2 _Vector2_cb26bc8c681b455c81073f0dc3c8f6b4_Out_0_Vector2 = float2(_Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float, _Multiply_7510d5a95e6c4d3986bb83059d23d2fa_Out_2_Float);
float2 _TilingAndOffset_e5f47c2cf9d846859a72aadcdac7fc3b_Out_3_Vector2;
Unity_TilingAndOffset_float(_Multiply_ba4d7f376f904143acc9eb33d8414ff8_Out_2_Vector2, float2 (1, 1), _Vector2_cb26bc8c681b455c81073f0dc3c8f6b4_Out_0_Vector2, _TilingAndOffset_e5f47c2cf9d846859a72aadcdac7fc3b_Out_3_Vector2);
  float4 _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).tex, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).GetTransformedUV(_TilingAndOffset_e5f47c2cf9d846859a72aadcdac7fc3b_Out_3_Vector2), float(2));
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_R_5_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_G_6_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_B_7_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_A_8_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.a;
float _Float_3f1a7b5619414946b13e7cef19336d45_Out_0_Float = float(0.3);
float _Subtract_3105ef040f344481afb342cd9ef65e9f_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_R_5_Float, _Float_3f1a7b5619414946b13e7cef19336d45_Out_0_Float, _Subtract_3105ef040f344481afb342cd9ef65e9f_Out_2_Float);
float _Split_18495110217342bc80044d9345d98fbd_R_1_Float = IN.WorldSpacePosition[0];
float _Split_18495110217342bc80044d9345d98fbd_G_2_Float = IN.WorldSpacePosition[1];
float _Split_18495110217342bc80044d9345d98fbd_B_3_Float = IN.WorldSpacePosition[2];
float _Split_18495110217342bc80044d9345d98fbd_A_4_Float = 0;
float4 _Combine_d1ee3e49696840daa7cfd43df2287807_RGBA_4_Vector4;
float3 _Combine_d1ee3e49696840daa7cfd43df2287807_RGB_5_Vector3;
float2 _Combine_d1ee3e49696840daa7cfd43df2287807_RG_6_Vector2;
Unity_Combine_float(_Split_18495110217342bc80044d9345d98fbd_R_1_Float, _Split_18495110217342bc80044d9345d98fbd_G_2_Float, float(0), float(0), _Combine_d1ee3e49696840daa7cfd43df2287807_RGBA_4_Vector4, _Combine_d1ee3e49696840daa7cfd43df2287807_RGB_5_Vector3, _Combine_d1ee3e49696840daa7cfd43df2287807_RG_6_Vector2);
float _Property_9efe7c2851414ea39b6971daac6143e5_Out_0_Float = _Wiggle_Wind_Size;
float _Multiply_4f6b361114004341aeae12987f071375_Out_2_Float;
Unity_Multiply_float_float(_Property_9efe7c2851414ea39b6971daac6143e5_Out_0_Float, 0.763, _Multiply_4f6b361114004341aeae12987f071375_Out_2_Float);
float2 _Multiply_0d88e33790f0458b9e6e7f49946e6344_Out_2_Vector2;
Unity_Multiply_float2_float2(_Combine_d1ee3e49696840daa7cfd43df2287807_RG_6_Vector2, (_Multiply_4f6b361114004341aeae12987f071375_Out_2_Float.xx), _Multiply_0d88e33790f0458b9e6e7f49946e6344_Out_2_Vector2);
float _Property_56c994a79361438d88ea30b1e3ccf2aa_Out_0_Float = _Wiggle_Wind_Speed_Large;
float _Multiply_c5e5feed518642678424c6f0d45b4377_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Property_56c994a79361438d88ea30b1e3ccf2aa_Out_0_Float, _Multiply_c5e5feed518642678424c6f0d45b4377_Out_2_Float);
float2 _TilingAndOffset_7dac505987c144edad083d13ebca67e6_Out_3_Vector2;
Unity_TilingAndOffset_float(_Multiply_0d88e33790f0458b9e6e7f49946e6344_Out_2_Vector2, float2 (1, 1), (_Multiply_c5e5feed518642678424c6f0d45b4377_Out_2_Float.xx), _TilingAndOffset_7dac505987c144edad083d13ebca67e6_Out_3_Vector2);
  float4 _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).tex, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).GetTransformedUV(_TilingAndOffset_7dac505987c144edad083d13ebca67e6_Out_3_Vector2), float(2));
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_R_5_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_G_6_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_B_7_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_A_8_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.a;
float _Subtract_e29fd5b0daea41a1a38ac52280e6db88_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_G_6_Float, _Float_3f1a7b5619414946b13e7cef19336d45_Out_0_Float, _Subtract_e29fd5b0daea41a1a38ac52280e6db88_Out_2_Float);
float _Add_6d51be6104ee4e23a093dfb9c3d993f7_Out_2_Float;
Unity_Add_float(_Subtract_3105ef040f344481afb342cd9ef65e9f_Out_2_Float, _Subtract_e29fd5b0daea41a1a38ac52280e6db88_Out_2_Float, _Add_6d51be6104ee4e23a093dfb9c3d993f7_Out_2_Float);
float4 _Multiply_db043a8b3fc34edb8bee433085c7e061_Out_2_Vector4;
Unity_Multiply_float4_float4((_Add_6d51be6104ee4e23a093dfb9c3d993f7_Out_2_Float.xxxx), IN.VertexColor, _Multiply_db043a8b3fc34edb8bee433085c7e061_Out_2_Vector4);
float _Property_46c8c648052246a4a32e80905ed7e9df_Out_0_Float = _Wiggle_Wind_Intensity;
float4 _Multiply_4baa80310c414483a2aada72baa7a883_Out_2_Vector4;
Unity_Multiply_float4_float4(_Multiply_db043a8b3fc34edb8bee433085c7e061_Out_2_Vector4, (_Property_46c8c648052246a4a32e80905ed7e9df_Out_0_Float.xxxx), _Multiply_4baa80310c414483a2aada72baa7a883_Out_2_Vector4);
float3 _Multiply_2c95a93020c44a20be82a31b985c35b8_Out_2_Vector3;
Unity_Multiply_float3_float3((_Multiply_4baa80310c414483a2aada72baa7a883_Out_2_Vector4.xyz), IN.ObjectSpaceNormal, _Multiply_2c95a93020c44a20be82a31b985c35b8_Out_2_Vector3);
float3 _Add_5a143eeddea74bceb3ddd5b059f49da5_Out_2_Vector3;
Unity_Add_float3(_Lerp_1fbf8d0a7f5a4e2ab381967ac90385c7_Out_3_Vector3, _Multiply_2c95a93020c44a20be82a31b985c35b8_Out_2_Vector3, _Add_5a143eeddea74bceb3ddd5b059f49da5_Out_2_Vector3);
#if defined(_USE_WPO)
float3 _UseWPO_1f28bdd134c9432e812aded965021562_Out_0_Vector3 = _Add_5a143eeddea74bceb3ddd5b059f49da5_Out_2_Vector3;
#else
float3 _UseWPO_1f28bdd134c9432e812aded965021562_Out_0_Vector3 = IN.ObjectSpacePosition;
#endif
float3 _UPGFoliageContactPositionCustomFunction_3446fb1a0c894696bd903b7eb184e241_bentPositionOS_1_Vector3;
UPG_FoliageContactPosition_float(_UseWPO_1f28bdd134c9432e812aded965021562_Out_0_Vector3, _UPGFoliageContactPositionCustomFunction_3446fb1a0c894696bd903b7eb184e241_bentPositionOS_1_Vector3);
description.Position = _UPGFoliageContactPositionCustomFunction_3446fb1a0c894696bd903b7eb184e241_bentPositionOS_1_Vector3;
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
UnityTexture2D _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Color_Map, sampler_Color_Map, _Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_eaea7ba5e1e74519a1bc7833f0103ab8_Out_0_Vector4 = IN.uv0;
float _Property_95352575ae4140008287fd58138e8eb5_Out_0_Float = _Color_Map_Tiling;
float4 _Multiply_86fafeae394745fe880655d03cafd9e1_Out_2_Vector4;
Unity_Multiply_float4_float4(_UV_eaea7ba5e1e74519a1bc7833f0103ab8_Out_0_Vector4, (_Property_95352575ae4140008287fd58138e8eb5_Out_0_Float.xxxx), _Multiply_86fafeae394745fe880655d03cafd9e1_Out_2_Vector4);
float4 _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.tex, _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.samplerstate, _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.GetTransformedUV((_Multiply_86fafeae394745fe880655d03cafd9e1_Out_2_Vector4.xy)) );
if (_Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4, _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_R_4_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.r;
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_G_5_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.g;
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_B_6_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.b;
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_A_7_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.a;
float _Multiply_cf1ea983d11e4ab7b4bdbbb064ad94b5_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_A_7_Float, 1, _Multiply_cf1ea983d11e4ab7b4bdbbb064ad94b5_Out_2_Float);
float _Property_99bb0b22edbb4ea0b1b7844414b876a7_Out_0_Float = _Alpha_Clip;
surface.Alpha = _Multiply_cf1ea983d11e4ab7b4bdbbb064ad94b5_Out_2_Float;
surface.AlphaClipThreshold = _Property_99bb0b22edbb4ea0b1b7844414b876a7_Out_0_Float;
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
    output.ObjectSpacePosition =                        input.positionOS;
    output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
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
#pragma shader_feature_local _ _USE_WPO
#pragma shader_feature_local _ _USE_HEIGHT_GRADIENCE

#if defined(_USE_WPO) && defined(_USE_HEIGHT_GRADIENCE)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_WPO)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_HEIGHT_GRADIENCE)
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

float4 _Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D_TexelSize;
float _Subsurface_Fresnel_Transition_Smoothness;
float _Subsurface_Fresnel_Pow;
float _Subsurface_Fresnel_Intensity;
float _Subsurface_Transition_Smoothness;
float _Wiggle_Wind_Speed_Large;
float _Wiggle_Wind_Intensity;
float _Roughness;
float4 _Foliage_Color_Bottom;
float _Sway_Wind_Speed;
float _Gradience_Power;
float _Fresnel_Mul;
float4 _Foliage_Color_Top;
float _Always_On_Fresnel_Intensity;
float _Tree_Height;
float4 _Color_Map_TexelSize;
float _Height_Weight;
float _Height_Gradience;
float _Sway_Wind_Time_Random_Offset_Amount;
float _Sway_Wind_Intensity;
float3 _Sway_Wind_Direction;
float _Color_Map_Tiling;
float _Subsurface_Intensity;
float _Alpha_Clip;
float _Wiggle_Wind_Size;
float _Wiggle_Wind_Speed_Small;
float _Fresnel_Pow;
float _Always_On_Fresnel_Pow;
float4 _Always_On_Fresnel_Color;
float _ree;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D);
SAMPLER(sampler_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D);
TEXTURE2D(_Color_Map);
SAMPLER(sampler_Color_Map);

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

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
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
float _Property_7689f3a0523b4af9912879a05d1d5206_Out_0_Float = _Sway_Wind_Speed;
float3 _Property_e0531013c46845b6b036ac453118c744_Out_0_Vector3 = _Sway_Wind_Direction;
float _Property_35588e6a16b5457695dba39b1644d669_Out_0_Float = _Sway_Wind_Intensity;
float _Property_c8d3d7e6e6f64d409f010d988b96e327_Out_0_Float = _Sway_Wind_Time_Random_Offset_Amount;
Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float _WindSway_662bcd4f8b2b4607b138cb44de00a708;
_WindSway_662bcd4f8b2b4607b138cb44de00a708.ObjectSpacePosition = IN.ObjectSpacePosition;
_WindSway_662bcd4f8b2b4607b138cb44de00a708.TimeParameters = IN.TimeParameters;
float3 _WindSway_662bcd4f8b2b4607b138cb44de00a708_OutVector3_1_Vector3;
SG_WindSway_51c4027ae50a8cd49be53c4990b9f179_float(_Property_7689f3a0523b4af9912879a05d1d5206_Out_0_Float, _Property_e0531013c46845b6b036ac453118c744_Out_0_Vector3, _Property_35588e6a16b5457695dba39b1644d669_Out_0_Float, _Property_c8d3d7e6e6f64d409f010d988b96e327_Out_0_Float, _WindSway_662bcd4f8b2b4607b138cb44de00a708, _WindSway_662bcd4f8b2b4607b138cb44de00a708_OutVector3_1_Vector3);
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_R_1_Float = IN.WorldSpacePosition[0];
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_G_2_Float = IN.WorldSpacePosition[1];
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_B_3_Float = IN.WorldSpacePosition[2];
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_A_4_Float = 0;
float3 _Vector3_e154b524054546d2bbeab293c0e20519_Out_0_Vector3 = float3(float(0), float(0), float(0));
float3 _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3;
_Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3 = TransformObjectToWorld(_Vector3_e154b524054546d2bbeab293c0e20519_Out_0_Vector3.xyz);
float _Split_5eb48153bba44ea2b128583819f09a11_R_1_Float = _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3[0];
float _Split_5eb48153bba44ea2b128583819f09a11_G_2_Float = _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3[1];
float _Split_5eb48153bba44ea2b128583819f09a11_B_3_Float = _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3[2];
float _Split_5eb48153bba44ea2b128583819f09a11_A_4_Float = 0;
float _Subtract_d6a99abdf4e74644acffc1718d1735bf_Out_2_Float;
Unity_Subtract_float(_Split_8fe962eb6f0f4d6fba734f9edcaf9067_G_2_Float, _Split_5eb48153bba44ea2b128583819f09a11_G_2_Float, _Subtract_d6a99abdf4e74644acffc1718d1735bf_Out_2_Float);
float _Property_1cf3cda52e094e638df491b003607dfd_Out_0_Float = _Height_Weight;
float _Split_145cf2161d4c4510b08248cad819f655_R_1_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[0];
float _Split_145cf2161d4c4510b08248cad819f655_G_2_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[1];
float _Split_145cf2161d4c4510b08248cad819f655_B_3_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[2];
float _Split_145cf2161d4c4510b08248cad819f655_A_4_Float = 0;
float _Multiply_2c140d573b9447aab058bd863fe2f4f7_Out_2_Float;
Unity_Multiply_float_float(_Property_1cf3cda52e094e638df491b003607dfd_Out_0_Float, _Split_145cf2161d4c4510b08248cad819f655_G_2_Float, _Multiply_2c140d573b9447aab058bd863fe2f4f7_Out_2_Float);
float _Divide_0f6a637143d841d0b1c94fa2ae3e164d_Out_2_Float;
Unity_Divide_float(_Subtract_d6a99abdf4e74644acffc1718d1735bf_Out_2_Float, _Multiply_2c140d573b9447aab058bd863fe2f4f7_Out_2_Float, _Divide_0f6a637143d841d0b1c94fa2ae3e164d_Out_2_Float);
float _Saturate_1e4e29fb5c074565a1f941c046957b05_Out_1_Float;
Unity_Saturate_float(_Divide_0f6a637143d841d0b1c94fa2ae3e164d_Out_2_Float, _Saturate_1e4e29fb5c074565a1f941c046957b05_Out_1_Float);
float _Property_3d64a0c8dd104507b9a0762a778fe585_Out_0_Float = _Height_Gradience;
float _Power_f1d74728398a45f9993fe3b3affb6584_Out_2_Float;
Unity_Power_float(_Saturate_1e4e29fb5c074565a1f941c046957b05_Out_1_Float, _Property_3d64a0c8dd104507b9a0762a778fe585_Out_0_Float, _Power_f1d74728398a45f9993fe3b3affb6584_Out_2_Float);
float _Saturate_57b130350452418fa1d09d4a0ddd64ac_Out_1_Float;
Unity_Saturate_float(_Power_f1d74728398a45f9993fe3b3affb6584_Out_2_Float, _Saturate_57b130350452418fa1d09d4a0ddd64ac_Out_1_Float);
float3 _Lerp_1fbf8d0a7f5a4e2ab381967ac90385c7_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _WindSway_662bcd4f8b2b4607b138cb44de00a708_OutVector3_1_Vector3, (_Saturate_57b130350452418fa1d09d4a0ddd64ac_Out_1_Float.xxx), _Lerp_1fbf8d0a7f5a4e2ab381967ac90385c7_Out_3_Vector3);
float _Split_bd04916788224fa2bb71be3f27eb839a_R_1_Float = IN.WorldSpacePosition[0];
float _Split_bd04916788224fa2bb71be3f27eb839a_G_2_Float = IN.WorldSpacePosition[1];
float _Split_bd04916788224fa2bb71be3f27eb839a_B_3_Float = IN.WorldSpacePosition[2];
float _Split_bd04916788224fa2bb71be3f27eb839a_A_4_Float = 0;
float4 _Combine_45cdab9c20414d03862100967f56c78d_RGBA_4_Vector4;
float3 _Combine_45cdab9c20414d03862100967f56c78d_RGB_5_Vector3;
float2 _Combine_45cdab9c20414d03862100967f56c78d_RG_6_Vector2;
Unity_Combine_float(_Split_bd04916788224fa2bb71be3f27eb839a_R_1_Float, _Split_bd04916788224fa2bb71be3f27eb839a_B_3_Float, float(0), float(0), _Combine_45cdab9c20414d03862100967f56c78d_RGBA_4_Vector4, _Combine_45cdab9c20414d03862100967f56c78d_RGB_5_Vector3, _Combine_45cdab9c20414d03862100967f56c78d_RG_6_Vector2);
float _Property_9b35f6244c0247c7a9be7af2671f58e7_Out_0_Float = _Wiggle_Wind_Size;
float2 _Multiply_ba4d7f376f904143acc9eb33d8414ff8_Out_2_Vector2;
Unity_Multiply_float2_float2(_Combine_45cdab9c20414d03862100967f56c78d_RG_6_Vector2, (_Property_9b35f6244c0247c7a9be7af2671f58e7_Out_0_Float.xx), _Multiply_ba4d7f376f904143acc9eb33d8414ff8_Out_2_Vector2);
float _Property_6eae25110c184e7a94293b95ddc9ecef_Out_0_Float = _Wiggle_Wind_Speed_Small;
float _Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Property_6eae25110c184e7a94293b95ddc9ecef_Out_0_Float, _Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float);
float _Multiply_7510d5a95e6c4d3986bb83059d23d2fa_Out_2_Float;
Unity_Multiply_float_float(_Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float, 0.8, _Multiply_7510d5a95e6c4d3986bb83059d23d2fa_Out_2_Float);
float2 _Vector2_cb26bc8c681b455c81073f0dc3c8f6b4_Out_0_Vector2 = float2(_Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float, _Multiply_7510d5a95e6c4d3986bb83059d23d2fa_Out_2_Float);
float2 _TilingAndOffset_e5f47c2cf9d846859a72aadcdac7fc3b_Out_3_Vector2;
Unity_TilingAndOffset_float(_Multiply_ba4d7f376f904143acc9eb33d8414ff8_Out_2_Vector2, float2 (1, 1), _Vector2_cb26bc8c681b455c81073f0dc3c8f6b4_Out_0_Vector2, _TilingAndOffset_e5f47c2cf9d846859a72aadcdac7fc3b_Out_3_Vector2);
  float4 _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).tex, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).GetTransformedUV(_TilingAndOffset_e5f47c2cf9d846859a72aadcdac7fc3b_Out_3_Vector2), float(2));
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_R_5_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_G_6_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_B_7_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_A_8_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.a;
float _Float_3f1a7b5619414946b13e7cef19336d45_Out_0_Float = float(0.3);
float _Subtract_3105ef040f344481afb342cd9ef65e9f_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_R_5_Float, _Float_3f1a7b5619414946b13e7cef19336d45_Out_0_Float, _Subtract_3105ef040f344481afb342cd9ef65e9f_Out_2_Float);
float _Split_18495110217342bc80044d9345d98fbd_R_1_Float = IN.WorldSpacePosition[0];
float _Split_18495110217342bc80044d9345d98fbd_G_2_Float = IN.WorldSpacePosition[1];
float _Split_18495110217342bc80044d9345d98fbd_B_3_Float = IN.WorldSpacePosition[2];
float _Split_18495110217342bc80044d9345d98fbd_A_4_Float = 0;
float4 _Combine_d1ee3e49696840daa7cfd43df2287807_RGBA_4_Vector4;
float3 _Combine_d1ee3e49696840daa7cfd43df2287807_RGB_5_Vector3;
float2 _Combine_d1ee3e49696840daa7cfd43df2287807_RG_6_Vector2;
Unity_Combine_float(_Split_18495110217342bc80044d9345d98fbd_R_1_Float, _Split_18495110217342bc80044d9345d98fbd_G_2_Float, float(0), float(0), _Combine_d1ee3e49696840daa7cfd43df2287807_RGBA_4_Vector4, _Combine_d1ee3e49696840daa7cfd43df2287807_RGB_5_Vector3, _Combine_d1ee3e49696840daa7cfd43df2287807_RG_6_Vector2);
float _Property_9efe7c2851414ea39b6971daac6143e5_Out_0_Float = _Wiggle_Wind_Size;
float _Multiply_4f6b361114004341aeae12987f071375_Out_2_Float;
Unity_Multiply_float_float(_Property_9efe7c2851414ea39b6971daac6143e5_Out_0_Float, 0.763, _Multiply_4f6b361114004341aeae12987f071375_Out_2_Float);
float2 _Multiply_0d88e33790f0458b9e6e7f49946e6344_Out_2_Vector2;
Unity_Multiply_float2_float2(_Combine_d1ee3e49696840daa7cfd43df2287807_RG_6_Vector2, (_Multiply_4f6b361114004341aeae12987f071375_Out_2_Float.xx), _Multiply_0d88e33790f0458b9e6e7f49946e6344_Out_2_Vector2);
float _Property_56c994a79361438d88ea30b1e3ccf2aa_Out_0_Float = _Wiggle_Wind_Speed_Large;
float _Multiply_c5e5feed518642678424c6f0d45b4377_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Property_56c994a79361438d88ea30b1e3ccf2aa_Out_0_Float, _Multiply_c5e5feed518642678424c6f0d45b4377_Out_2_Float);
float2 _TilingAndOffset_7dac505987c144edad083d13ebca67e6_Out_3_Vector2;
Unity_TilingAndOffset_float(_Multiply_0d88e33790f0458b9e6e7f49946e6344_Out_2_Vector2, float2 (1, 1), (_Multiply_c5e5feed518642678424c6f0d45b4377_Out_2_Float.xx), _TilingAndOffset_7dac505987c144edad083d13ebca67e6_Out_3_Vector2);
  float4 _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).tex, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).GetTransformedUV(_TilingAndOffset_7dac505987c144edad083d13ebca67e6_Out_3_Vector2), float(2));
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_R_5_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_G_6_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_B_7_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_A_8_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.a;
float _Subtract_e29fd5b0daea41a1a38ac52280e6db88_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_G_6_Float, _Float_3f1a7b5619414946b13e7cef19336d45_Out_0_Float, _Subtract_e29fd5b0daea41a1a38ac52280e6db88_Out_2_Float);
float _Add_6d51be6104ee4e23a093dfb9c3d993f7_Out_2_Float;
Unity_Add_float(_Subtract_3105ef040f344481afb342cd9ef65e9f_Out_2_Float, _Subtract_e29fd5b0daea41a1a38ac52280e6db88_Out_2_Float, _Add_6d51be6104ee4e23a093dfb9c3d993f7_Out_2_Float);
float4 _Multiply_db043a8b3fc34edb8bee433085c7e061_Out_2_Vector4;
Unity_Multiply_float4_float4((_Add_6d51be6104ee4e23a093dfb9c3d993f7_Out_2_Float.xxxx), IN.VertexColor, _Multiply_db043a8b3fc34edb8bee433085c7e061_Out_2_Vector4);
float _Property_46c8c648052246a4a32e80905ed7e9df_Out_0_Float = _Wiggle_Wind_Intensity;
float4 _Multiply_4baa80310c414483a2aada72baa7a883_Out_2_Vector4;
Unity_Multiply_float4_float4(_Multiply_db043a8b3fc34edb8bee433085c7e061_Out_2_Vector4, (_Property_46c8c648052246a4a32e80905ed7e9df_Out_0_Float.xxxx), _Multiply_4baa80310c414483a2aada72baa7a883_Out_2_Vector4);
float3 _Multiply_2c95a93020c44a20be82a31b985c35b8_Out_2_Vector3;
Unity_Multiply_float3_float3((_Multiply_4baa80310c414483a2aada72baa7a883_Out_2_Vector4.xyz), IN.ObjectSpaceNormal, _Multiply_2c95a93020c44a20be82a31b985c35b8_Out_2_Vector3);
float3 _Add_5a143eeddea74bceb3ddd5b059f49da5_Out_2_Vector3;
Unity_Add_float3(_Lerp_1fbf8d0a7f5a4e2ab381967ac90385c7_Out_3_Vector3, _Multiply_2c95a93020c44a20be82a31b985c35b8_Out_2_Vector3, _Add_5a143eeddea74bceb3ddd5b059f49da5_Out_2_Vector3);
#if defined(_USE_WPO)
float3 _UseWPO_1f28bdd134c9432e812aded965021562_Out_0_Vector3 = _Add_5a143eeddea74bceb3ddd5b059f49da5_Out_2_Vector3;
#else
float3 _UseWPO_1f28bdd134c9432e812aded965021562_Out_0_Vector3 = IN.ObjectSpacePosition;
#endif
float3 _UPGFoliageContactPositionCustomFunction_3446fb1a0c894696bd903b7eb184e241_bentPositionOS_1_Vector3;
UPG_FoliageContactPosition_float(_UseWPO_1f28bdd134c9432e812aded965021562_Out_0_Vector3, _UPGFoliageContactPositionCustomFunction_3446fb1a0c894696bd903b7eb184e241_bentPositionOS_1_Vector3);
description.Position = _UPGFoliageContactPositionCustomFunction_3446fb1a0c894696bd903b7eb184e241_bentPositionOS_1_Vector3;
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
UnityTexture2D _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Color_Map, sampler_Color_Map, _Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_eaea7ba5e1e74519a1bc7833f0103ab8_Out_0_Vector4 = IN.uv0;
float _Property_95352575ae4140008287fd58138e8eb5_Out_0_Float = _Color_Map_Tiling;
float4 _Multiply_86fafeae394745fe880655d03cafd9e1_Out_2_Vector4;
Unity_Multiply_float4_float4(_UV_eaea7ba5e1e74519a1bc7833f0103ab8_Out_0_Vector4, (_Property_95352575ae4140008287fd58138e8eb5_Out_0_Float.xxxx), _Multiply_86fafeae394745fe880655d03cafd9e1_Out_2_Vector4);
float4 _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.tex, _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.samplerstate, _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.GetTransformedUV((_Multiply_86fafeae394745fe880655d03cafd9e1_Out_2_Vector4.xy)) );
if (_Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4, _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_R_4_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.r;
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_G_5_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.g;
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_B_6_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.b;
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_A_7_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.a;
float _Multiply_cf1ea983d11e4ab7b4bdbbb064ad94b5_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_A_7_Float, 1, _Multiply_cf1ea983d11e4ab7b4bdbbb064ad94b5_Out_2_Float);
float _Property_99bb0b22edbb4ea0b1b7844414b876a7_Out_0_Float = _Alpha_Clip;
surface.Alpha = _Multiply_cf1ea983d11e4ab7b4bdbbb064ad94b5_Out_2_Float;
surface.AlphaClipThreshold = _Property_99bb0b22edbb4ea0b1b7844414b876a7_Out_0_Float;
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
#pragma shader_feature_local _ _USE_WPO
#pragma shader_feature_local _ _USE_HEIGHT_GRADIENCE

#if defined(_USE_WPO) && defined(_USE_HEIGHT_GRADIENCE)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_WPO)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_HEIGHT_GRADIENCE)
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

float4 _Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D_TexelSize;
float _Subsurface_Fresnel_Transition_Smoothness;
float _Subsurface_Fresnel_Pow;
float _Subsurface_Fresnel_Intensity;
float _Subsurface_Transition_Smoothness;
float _Wiggle_Wind_Speed_Large;
float _Wiggle_Wind_Intensity;
float _Roughness;
float4 _Foliage_Color_Bottom;
float _Sway_Wind_Speed;
float _Gradience_Power;
float _Fresnel_Mul;
float4 _Foliage_Color_Top;
float _Always_On_Fresnel_Intensity;
float _Tree_Height;
float4 _Color_Map_TexelSize;
float _Height_Weight;
float _Height_Gradience;
float _Sway_Wind_Time_Random_Offset_Amount;
float _Sway_Wind_Intensity;
float3 _Sway_Wind_Direction;
float _Color_Map_Tiling;
float _Subsurface_Intensity;
float _Alpha_Clip;
float _Wiggle_Wind_Size;
float _Wiggle_Wind_Speed_Small;
float _Fresnel_Pow;
float _Always_On_Fresnel_Pow;
float4 _Always_On_Fresnel_Color;
float _ree;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D);
SAMPLER(sampler_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D);
TEXTURE2D(_Color_Map);
SAMPLER(sampler_Color_Map);

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

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
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
float _Property_7689f3a0523b4af9912879a05d1d5206_Out_0_Float = _Sway_Wind_Speed;
float3 _Property_e0531013c46845b6b036ac453118c744_Out_0_Vector3 = _Sway_Wind_Direction;
float _Property_35588e6a16b5457695dba39b1644d669_Out_0_Float = _Sway_Wind_Intensity;
float _Property_c8d3d7e6e6f64d409f010d988b96e327_Out_0_Float = _Sway_Wind_Time_Random_Offset_Amount;
Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float _WindSway_662bcd4f8b2b4607b138cb44de00a708;
_WindSway_662bcd4f8b2b4607b138cb44de00a708.ObjectSpacePosition = IN.ObjectSpacePosition;
_WindSway_662bcd4f8b2b4607b138cb44de00a708.TimeParameters = IN.TimeParameters;
float3 _WindSway_662bcd4f8b2b4607b138cb44de00a708_OutVector3_1_Vector3;
SG_WindSway_51c4027ae50a8cd49be53c4990b9f179_float(_Property_7689f3a0523b4af9912879a05d1d5206_Out_0_Float, _Property_e0531013c46845b6b036ac453118c744_Out_0_Vector3, _Property_35588e6a16b5457695dba39b1644d669_Out_0_Float, _Property_c8d3d7e6e6f64d409f010d988b96e327_Out_0_Float, _WindSway_662bcd4f8b2b4607b138cb44de00a708, _WindSway_662bcd4f8b2b4607b138cb44de00a708_OutVector3_1_Vector3);
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_R_1_Float = IN.WorldSpacePosition[0];
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_G_2_Float = IN.WorldSpacePosition[1];
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_B_3_Float = IN.WorldSpacePosition[2];
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_A_4_Float = 0;
float3 _Vector3_e154b524054546d2bbeab293c0e20519_Out_0_Vector3 = float3(float(0), float(0), float(0));
float3 _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3;
_Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3 = TransformObjectToWorld(_Vector3_e154b524054546d2bbeab293c0e20519_Out_0_Vector3.xyz);
float _Split_5eb48153bba44ea2b128583819f09a11_R_1_Float = _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3[0];
float _Split_5eb48153bba44ea2b128583819f09a11_G_2_Float = _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3[1];
float _Split_5eb48153bba44ea2b128583819f09a11_B_3_Float = _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3[2];
float _Split_5eb48153bba44ea2b128583819f09a11_A_4_Float = 0;
float _Subtract_d6a99abdf4e74644acffc1718d1735bf_Out_2_Float;
Unity_Subtract_float(_Split_8fe962eb6f0f4d6fba734f9edcaf9067_G_2_Float, _Split_5eb48153bba44ea2b128583819f09a11_G_2_Float, _Subtract_d6a99abdf4e74644acffc1718d1735bf_Out_2_Float);
float _Property_1cf3cda52e094e638df491b003607dfd_Out_0_Float = _Height_Weight;
float _Split_145cf2161d4c4510b08248cad819f655_R_1_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[0];
float _Split_145cf2161d4c4510b08248cad819f655_G_2_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[1];
float _Split_145cf2161d4c4510b08248cad819f655_B_3_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[2];
float _Split_145cf2161d4c4510b08248cad819f655_A_4_Float = 0;
float _Multiply_2c140d573b9447aab058bd863fe2f4f7_Out_2_Float;
Unity_Multiply_float_float(_Property_1cf3cda52e094e638df491b003607dfd_Out_0_Float, _Split_145cf2161d4c4510b08248cad819f655_G_2_Float, _Multiply_2c140d573b9447aab058bd863fe2f4f7_Out_2_Float);
float _Divide_0f6a637143d841d0b1c94fa2ae3e164d_Out_2_Float;
Unity_Divide_float(_Subtract_d6a99abdf4e74644acffc1718d1735bf_Out_2_Float, _Multiply_2c140d573b9447aab058bd863fe2f4f7_Out_2_Float, _Divide_0f6a637143d841d0b1c94fa2ae3e164d_Out_2_Float);
float _Saturate_1e4e29fb5c074565a1f941c046957b05_Out_1_Float;
Unity_Saturate_float(_Divide_0f6a637143d841d0b1c94fa2ae3e164d_Out_2_Float, _Saturate_1e4e29fb5c074565a1f941c046957b05_Out_1_Float);
float _Property_3d64a0c8dd104507b9a0762a778fe585_Out_0_Float = _Height_Gradience;
float _Power_f1d74728398a45f9993fe3b3affb6584_Out_2_Float;
Unity_Power_float(_Saturate_1e4e29fb5c074565a1f941c046957b05_Out_1_Float, _Property_3d64a0c8dd104507b9a0762a778fe585_Out_0_Float, _Power_f1d74728398a45f9993fe3b3affb6584_Out_2_Float);
float _Saturate_57b130350452418fa1d09d4a0ddd64ac_Out_1_Float;
Unity_Saturate_float(_Power_f1d74728398a45f9993fe3b3affb6584_Out_2_Float, _Saturate_57b130350452418fa1d09d4a0ddd64ac_Out_1_Float);
float3 _Lerp_1fbf8d0a7f5a4e2ab381967ac90385c7_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _WindSway_662bcd4f8b2b4607b138cb44de00a708_OutVector3_1_Vector3, (_Saturate_57b130350452418fa1d09d4a0ddd64ac_Out_1_Float.xxx), _Lerp_1fbf8d0a7f5a4e2ab381967ac90385c7_Out_3_Vector3);
float _Split_bd04916788224fa2bb71be3f27eb839a_R_1_Float = IN.WorldSpacePosition[0];
float _Split_bd04916788224fa2bb71be3f27eb839a_G_2_Float = IN.WorldSpacePosition[1];
float _Split_bd04916788224fa2bb71be3f27eb839a_B_3_Float = IN.WorldSpacePosition[2];
float _Split_bd04916788224fa2bb71be3f27eb839a_A_4_Float = 0;
float4 _Combine_45cdab9c20414d03862100967f56c78d_RGBA_4_Vector4;
float3 _Combine_45cdab9c20414d03862100967f56c78d_RGB_5_Vector3;
float2 _Combine_45cdab9c20414d03862100967f56c78d_RG_6_Vector2;
Unity_Combine_float(_Split_bd04916788224fa2bb71be3f27eb839a_R_1_Float, _Split_bd04916788224fa2bb71be3f27eb839a_B_3_Float, float(0), float(0), _Combine_45cdab9c20414d03862100967f56c78d_RGBA_4_Vector4, _Combine_45cdab9c20414d03862100967f56c78d_RGB_5_Vector3, _Combine_45cdab9c20414d03862100967f56c78d_RG_6_Vector2);
float _Property_9b35f6244c0247c7a9be7af2671f58e7_Out_0_Float = _Wiggle_Wind_Size;
float2 _Multiply_ba4d7f376f904143acc9eb33d8414ff8_Out_2_Vector2;
Unity_Multiply_float2_float2(_Combine_45cdab9c20414d03862100967f56c78d_RG_6_Vector2, (_Property_9b35f6244c0247c7a9be7af2671f58e7_Out_0_Float.xx), _Multiply_ba4d7f376f904143acc9eb33d8414ff8_Out_2_Vector2);
float _Property_6eae25110c184e7a94293b95ddc9ecef_Out_0_Float = _Wiggle_Wind_Speed_Small;
float _Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Property_6eae25110c184e7a94293b95ddc9ecef_Out_0_Float, _Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float);
float _Multiply_7510d5a95e6c4d3986bb83059d23d2fa_Out_2_Float;
Unity_Multiply_float_float(_Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float, 0.8, _Multiply_7510d5a95e6c4d3986bb83059d23d2fa_Out_2_Float);
float2 _Vector2_cb26bc8c681b455c81073f0dc3c8f6b4_Out_0_Vector2 = float2(_Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float, _Multiply_7510d5a95e6c4d3986bb83059d23d2fa_Out_2_Float);
float2 _TilingAndOffset_e5f47c2cf9d846859a72aadcdac7fc3b_Out_3_Vector2;
Unity_TilingAndOffset_float(_Multiply_ba4d7f376f904143acc9eb33d8414ff8_Out_2_Vector2, float2 (1, 1), _Vector2_cb26bc8c681b455c81073f0dc3c8f6b4_Out_0_Vector2, _TilingAndOffset_e5f47c2cf9d846859a72aadcdac7fc3b_Out_3_Vector2);
  float4 _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).tex, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).GetTransformedUV(_TilingAndOffset_e5f47c2cf9d846859a72aadcdac7fc3b_Out_3_Vector2), float(2));
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_R_5_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_G_6_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_B_7_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_A_8_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.a;
float _Float_3f1a7b5619414946b13e7cef19336d45_Out_0_Float = float(0.3);
float _Subtract_3105ef040f344481afb342cd9ef65e9f_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_R_5_Float, _Float_3f1a7b5619414946b13e7cef19336d45_Out_0_Float, _Subtract_3105ef040f344481afb342cd9ef65e9f_Out_2_Float);
float _Split_18495110217342bc80044d9345d98fbd_R_1_Float = IN.WorldSpacePosition[0];
float _Split_18495110217342bc80044d9345d98fbd_G_2_Float = IN.WorldSpacePosition[1];
float _Split_18495110217342bc80044d9345d98fbd_B_3_Float = IN.WorldSpacePosition[2];
float _Split_18495110217342bc80044d9345d98fbd_A_4_Float = 0;
float4 _Combine_d1ee3e49696840daa7cfd43df2287807_RGBA_4_Vector4;
float3 _Combine_d1ee3e49696840daa7cfd43df2287807_RGB_5_Vector3;
float2 _Combine_d1ee3e49696840daa7cfd43df2287807_RG_6_Vector2;
Unity_Combine_float(_Split_18495110217342bc80044d9345d98fbd_R_1_Float, _Split_18495110217342bc80044d9345d98fbd_G_2_Float, float(0), float(0), _Combine_d1ee3e49696840daa7cfd43df2287807_RGBA_4_Vector4, _Combine_d1ee3e49696840daa7cfd43df2287807_RGB_5_Vector3, _Combine_d1ee3e49696840daa7cfd43df2287807_RG_6_Vector2);
float _Property_9efe7c2851414ea39b6971daac6143e5_Out_0_Float = _Wiggle_Wind_Size;
float _Multiply_4f6b361114004341aeae12987f071375_Out_2_Float;
Unity_Multiply_float_float(_Property_9efe7c2851414ea39b6971daac6143e5_Out_0_Float, 0.763, _Multiply_4f6b361114004341aeae12987f071375_Out_2_Float);
float2 _Multiply_0d88e33790f0458b9e6e7f49946e6344_Out_2_Vector2;
Unity_Multiply_float2_float2(_Combine_d1ee3e49696840daa7cfd43df2287807_RG_6_Vector2, (_Multiply_4f6b361114004341aeae12987f071375_Out_2_Float.xx), _Multiply_0d88e33790f0458b9e6e7f49946e6344_Out_2_Vector2);
float _Property_56c994a79361438d88ea30b1e3ccf2aa_Out_0_Float = _Wiggle_Wind_Speed_Large;
float _Multiply_c5e5feed518642678424c6f0d45b4377_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Property_56c994a79361438d88ea30b1e3ccf2aa_Out_0_Float, _Multiply_c5e5feed518642678424c6f0d45b4377_Out_2_Float);
float2 _TilingAndOffset_7dac505987c144edad083d13ebca67e6_Out_3_Vector2;
Unity_TilingAndOffset_float(_Multiply_0d88e33790f0458b9e6e7f49946e6344_Out_2_Vector2, float2 (1, 1), (_Multiply_c5e5feed518642678424c6f0d45b4377_Out_2_Float.xx), _TilingAndOffset_7dac505987c144edad083d13ebca67e6_Out_3_Vector2);
  float4 _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).tex, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).GetTransformedUV(_TilingAndOffset_7dac505987c144edad083d13ebca67e6_Out_3_Vector2), float(2));
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_R_5_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_G_6_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_B_7_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_A_8_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.a;
float _Subtract_e29fd5b0daea41a1a38ac52280e6db88_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_G_6_Float, _Float_3f1a7b5619414946b13e7cef19336d45_Out_0_Float, _Subtract_e29fd5b0daea41a1a38ac52280e6db88_Out_2_Float);
float _Add_6d51be6104ee4e23a093dfb9c3d993f7_Out_2_Float;
Unity_Add_float(_Subtract_3105ef040f344481afb342cd9ef65e9f_Out_2_Float, _Subtract_e29fd5b0daea41a1a38ac52280e6db88_Out_2_Float, _Add_6d51be6104ee4e23a093dfb9c3d993f7_Out_2_Float);
float4 _Multiply_db043a8b3fc34edb8bee433085c7e061_Out_2_Vector4;
Unity_Multiply_float4_float4((_Add_6d51be6104ee4e23a093dfb9c3d993f7_Out_2_Float.xxxx), IN.VertexColor, _Multiply_db043a8b3fc34edb8bee433085c7e061_Out_2_Vector4);
float _Property_46c8c648052246a4a32e80905ed7e9df_Out_0_Float = _Wiggle_Wind_Intensity;
float4 _Multiply_4baa80310c414483a2aada72baa7a883_Out_2_Vector4;
Unity_Multiply_float4_float4(_Multiply_db043a8b3fc34edb8bee433085c7e061_Out_2_Vector4, (_Property_46c8c648052246a4a32e80905ed7e9df_Out_0_Float.xxxx), _Multiply_4baa80310c414483a2aada72baa7a883_Out_2_Vector4);
float3 _Multiply_2c95a93020c44a20be82a31b985c35b8_Out_2_Vector3;
Unity_Multiply_float3_float3((_Multiply_4baa80310c414483a2aada72baa7a883_Out_2_Vector4.xyz), IN.ObjectSpaceNormal, _Multiply_2c95a93020c44a20be82a31b985c35b8_Out_2_Vector3);
float3 _Add_5a143eeddea74bceb3ddd5b059f49da5_Out_2_Vector3;
Unity_Add_float3(_Lerp_1fbf8d0a7f5a4e2ab381967ac90385c7_Out_3_Vector3, _Multiply_2c95a93020c44a20be82a31b985c35b8_Out_2_Vector3, _Add_5a143eeddea74bceb3ddd5b059f49da5_Out_2_Vector3);
#if defined(_USE_WPO)
float3 _UseWPO_1f28bdd134c9432e812aded965021562_Out_0_Vector3 = _Add_5a143eeddea74bceb3ddd5b059f49da5_Out_2_Vector3;
#else
float3 _UseWPO_1f28bdd134c9432e812aded965021562_Out_0_Vector3 = IN.ObjectSpacePosition;
#endif
float3 _UPGFoliageContactPositionCustomFunction_3446fb1a0c894696bd903b7eb184e241_bentPositionOS_1_Vector3;
UPG_FoliageContactPosition_float(_UseWPO_1f28bdd134c9432e812aded965021562_Out_0_Vector3, _UPGFoliageContactPositionCustomFunction_3446fb1a0c894696bd903b7eb184e241_bentPositionOS_1_Vector3);
description.Position = _UPGFoliageContactPositionCustomFunction_3446fb1a0c894696bd903b7eb184e241_bentPositionOS_1_Vector3;
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
UnityTexture2D _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Color_Map, sampler_Color_Map, _Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_eaea7ba5e1e74519a1bc7833f0103ab8_Out_0_Vector4 = IN.uv0;
float _Property_95352575ae4140008287fd58138e8eb5_Out_0_Float = _Color_Map_Tiling;
float4 _Multiply_86fafeae394745fe880655d03cafd9e1_Out_2_Vector4;
Unity_Multiply_float4_float4(_UV_eaea7ba5e1e74519a1bc7833f0103ab8_Out_0_Vector4, (_Property_95352575ae4140008287fd58138e8eb5_Out_0_Float.xxxx), _Multiply_86fafeae394745fe880655d03cafd9e1_Out_2_Vector4);
float4 _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.tex, _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.samplerstate, _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.GetTransformedUV((_Multiply_86fafeae394745fe880655d03cafd9e1_Out_2_Vector4.xy)) );
if (_Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4, _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_R_4_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.r;
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_G_5_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.g;
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_B_6_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.b;
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_A_7_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.a;
float _Multiply_cf1ea983d11e4ab7b4bdbbb064ad94b5_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_A_7_Float, 1, _Multiply_cf1ea983d11e4ab7b4bdbbb064ad94b5_Out_2_Float);
float _Property_99bb0b22edbb4ea0b1b7844414b876a7_Out_0_Float = _Alpha_Clip;
surface.NormalTS = IN.TangentSpaceNormal;
surface.Alpha = _Multiply_cf1ea983d11e4ab7b4bdbbb064ad94b5_Out_2_Float;
surface.AlphaClipThreshold = _Property_99bb0b22edbb4ea0b1b7844414b876a7_Out_0_Float;
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
#pragma shader_feature_local _ _USE_WPO
#pragma shader_feature_local _ _USE_HEIGHT_GRADIENCE

#if defined(_USE_WPO) && defined(_USE_HEIGHT_GRADIENCE)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_WPO)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_HEIGHT_GRADIENCE)
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
 float3 WorldSpaceViewDirection;
 float3 WorldSpacePosition;
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 WorldSpacePosition;
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

float4 _Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D_TexelSize;
float _Subsurface_Fresnel_Transition_Smoothness;
float _Subsurface_Fresnel_Pow;
float _Subsurface_Fresnel_Intensity;
float _Subsurface_Transition_Smoothness;
float _Wiggle_Wind_Speed_Large;
float _Wiggle_Wind_Intensity;
float _Roughness;
float4 _Foliage_Color_Bottom;
float _Sway_Wind_Speed;
float _Gradience_Power;
float _Fresnel_Mul;
float4 _Foliage_Color_Top;
float _Always_On_Fresnel_Intensity;
float _Tree_Height;
float4 _Color_Map_TexelSize;
float _Height_Weight;
float _Height_Gradience;
float _Sway_Wind_Time_Random_Offset_Amount;
float _Sway_Wind_Intensity;
float3 _Sway_Wind_Direction;
float _Color_Map_Tiling;
float _Subsurface_Intensity;
float _Alpha_Clip;
float _Wiggle_Wind_Size;
float _Wiggle_Wind_Speed_Small;
float _Fresnel_Pow;
float _Always_On_Fresnel_Pow;
float4 _Always_On_Fresnel_Color;
float _ree;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D);
SAMPLER(sampler_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D);
TEXTURE2D(_Color_Map);
SAMPLER(sampler_Color_Map);

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

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Saturate_float4(float4 In, out float4 Out)
{
    Out = saturate(In);
}

void Unity_Normalize_float3(float3 In, out float3 Out)
{
    Out = normalize(In);
}

void Unity_DotProduct_float3(float3 A, float3 B, out float Out)
{
    Out = dot(A, B);
}

void Unity_Absolute_float(float In, out float Out)
{
    Out = abs(In);
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
}

// unity-custom-func-begin
void MainLight_float(out float3 Direction){

#ifdef UNITY_SHADER_GRAPH
	Direction = _WorldSpaceLightPos0.xyz;
#else
	Direction = float3(1,1,1);
#endif
}
// unity-custom-func-end

void Unity_DotProduct_float2(float2 A, float2 B, out float Out)
{
    Out = dot(A, B);
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
float _Property_7689f3a0523b4af9912879a05d1d5206_Out_0_Float = _Sway_Wind_Speed;
float3 _Property_e0531013c46845b6b036ac453118c744_Out_0_Vector3 = _Sway_Wind_Direction;
float _Property_35588e6a16b5457695dba39b1644d669_Out_0_Float = _Sway_Wind_Intensity;
float _Property_c8d3d7e6e6f64d409f010d988b96e327_Out_0_Float = _Sway_Wind_Time_Random_Offset_Amount;
Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float _WindSway_662bcd4f8b2b4607b138cb44de00a708;
_WindSway_662bcd4f8b2b4607b138cb44de00a708.ObjectSpacePosition = IN.ObjectSpacePosition;
_WindSway_662bcd4f8b2b4607b138cb44de00a708.TimeParameters = IN.TimeParameters;
float3 _WindSway_662bcd4f8b2b4607b138cb44de00a708_OutVector3_1_Vector3;
SG_WindSway_51c4027ae50a8cd49be53c4990b9f179_float(_Property_7689f3a0523b4af9912879a05d1d5206_Out_0_Float, _Property_e0531013c46845b6b036ac453118c744_Out_0_Vector3, _Property_35588e6a16b5457695dba39b1644d669_Out_0_Float, _Property_c8d3d7e6e6f64d409f010d988b96e327_Out_0_Float, _WindSway_662bcd4f8b2b4607b138cb44de00a708, _WindSway_662bcd4f8b2b4607b138cb44de00a708_OutVector3_1_Vector3);
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_R_1_Float = IN.WorldSpacePosition[0];
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_G_2_Float = IN.WorldSpacePosition[1];
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_B_3_Float = IN.WorldSpacePosition[2];
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_A_4_Float = 0;
float3 _Vector3_e154b524054546d2bbeab293c0e20519_Out_0_Vector3 = float3(float(0), float(0), float(0));
float3 _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3;
_Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3 = TransformObjectToWorld(_Vector3_e154b524054546d2bbeab293c0e20519_Out_0_Vector3.xyz);
float _Split_5eb48153bba44ea2b128583819f09a11_R_1_Float = _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3[0];
float _Split_5eb48153bba44ea2b128583819f09a11_G_2_Float = _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3[1];
float _Split_5eb48153bba44ea2b128583819f09a11_B_3_Float = _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3[2];
float _Split_5eb48153bba44ea2b128583819f09a11_A_4_Float = 0;
float _Subtract_d6a99abdf4e74644acffc1718d1735bf_Out_2_Float;
Unity_Subtract_float(_Split_8fe962eb6f0f4d6fba734f9edcaf9067_G_2_Float, _Split_5eb48153bba44ea2b128583819f09a11_G_2_Float, _Subtract_d6a99abdf4e74644acffc1718d1735bf_Out_2_Float);
float _Property_1cf3cda52e094e638df491b003607dfd_Out_0_Float = _Height_Weight;
float _Split_145cf2161d4c4510b08248cad819f655_R_1_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[0];
float _Split_145cf2161d4c4510b08248cad819f655_G_2_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[1];
float _Split_145cf2161d4c4510b08248cad819f655_B_3_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[2];
float _Split_145cf2161d4c4510b08248cad819f655_A_4_Float = 0;
float _Multiply_2c140d573b9447aab058bd863fe2f4f7_Out_2_Float;
Unity_Multiply_float_float(_Property_1cf3cda52e094e638df491b003607dfd_Out_0_Float, _Split_145cf2161d4c4510b08248cad819f655_G_2_Float, _Multiply_2c140d573b9447aab058bd863fe2f4f7_Out_2_Float);
float _Divide_0f6a637143d841d0b1c94fa2ae3e164d_Out_2_Float;
Unity_Divide_float(_Subtract_d6a99abdf4e74644acffc1718d1735bf_Out_2_Float, _Multiply_2c140d573b9447aab058bd863fe2f4f7_Out_2_Float, _Divide_0f6a637143d841d0b1c94fa2ae3e164d_Out_2_Float);
float _Saturate_1e4e29fb5c074565a1f941c046957b05_Out_1_Float;
Unity_Saturate_float(_Divide_0f6a637143d841d0b1c94fa2ae3e164d_Out_2_Float, _Saturate_1e4e29fb5c074565a1f941c046957b05_Out_1_Float);
float _Property_3d64a0c8dd104507b9a0762a778fe585_Out_0_Float = _Height_Gradience;
float _Power_f1d74728398a45f9993fe3b3affb6584_Out_2_Float;
Unity_Power_float(_Saturate_1e4e29fb5c074565a1f941c046957b05_Out_1_Float, _Property_3d64a0c8dd104507b9a0762a778fe585_Out_0_Float, _Power_f1d74728398a45f9993fe3b3affb6584_Out_2_Float);
float _Saturate_57b130350452418fa1d09d4a0ddd64ac_Out_1_Float;
Unity_Saturate_float(_Power_f1d74728398a45f9993fe3b3affb6584_Out_2_Float, _Saturate_57b130350452418fa1d09d4a0ddd64ac_Out_1_Float);
float3 _Lerp_1fbf8d0a7f5a4e2ab381967ac90385c7_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _WindSway_662bcd4f8b2b4607b138cb44de00a708_OutVector3_1_Vector3, (_Saturate_57b130350452418fa1d09d4a0ddd64ac_Out_1_Float.xxx), _Lerp_1fbf8d0a7f5a4e2ab381967ac90385c7_Out_3_Vector3);
float _Split_bd04916788224fa2bb71be3f27eb839a_R_1_Float = IN.WorldSpacePosition[0];
float _Split_bd04916788224fa2bb71be3f27eb839a_G_2_Float = IN.WorldSpacePosition[1];
float _Split_bd04916788224fa2bb71be3f27eb839a_B_3_Float = IN.WorldSpacePosition[2];
float _Split_bd04916788224fa2bb71be3f27eb839a_A_4_Float = 0;
float4 _Combine_45cdab9c20414d03862100967f56c78d_RGBA_4_Vector4;
float3 _Combine_45cdab9c20414d03862100967f56c78d_RGB_5_Vector3;
float2 _Combine_45cdab9c20414d03862100967f56c78d_RG_6_Vector2;
Unity_Combine_float(_Split_bd04916788224fa2bb71be3f27eb839a_R_1_Float, _Split_bd04916788224fa2bb71be3f27eb839a_B_3_Float, float(0), float(0), _Combine_45cdab9c20414d03862100967f56c78d_RGBA_4_Vector4, _Combine_45cdab9c20414d03862100967f56c78d_RGB_5_Vector3, _Combine_45cdab9c20414d03862100967f56c78d_RG_6_Vector2);
float _Property_9b35f6244c0247c7a9be7af2671f58e7_Out_0_Float = _Wiggle_Wind_Size;
float2 _Multiply_ba4d7f376f904143acc9eb33d8414ff8_Out_2_Vector2;
Unity_Multiply_float2_float2(_Combine_45cdab9c20414d03862100967f56c78d_RG_6_Vector2, (_Property_9b35f6244c0247c7a9be7af2671f58e7_Out_0_Float.xx), _Multiply_ba4d7f376f904143acc9eb33d8414ff8_Out_2_Vector2);
float _Property_6eae25110c184e7a94293b95ddc9ecef_Out_0_Float = _Wiggle_Wind_Speed_Small;
float _Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Property_6eae25110c184e7a94293b95ddc9ecef_Out_0_Float, _Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float);
float _Multiply_7510d5a95e6c4d3986bb83059d23d2fa_Out_2_Float;
Unity_Multiply_float_float(_Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float, 0.8, _Multiply_7510d5a95e6c4d3986bb83059d23d2fa_Out_2_Float);
float2 _Vector2_cb26bc8c681b455c81073f0dc3c8f6b4_Out_0_Vector2 = float2(_Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float, _Multiply_7510d5a95e6c4d3986bb83059d23d2fa_Out_2_Float);
float2 _TilingAndOffset_e5f47c2cf9d846859a72aadcdac7fc3b_Out_3_Vector2;
Unity_TilingAndOffset_float(_Multiply_ba4d7f376f904143acc9eb33d8414ff8_Out_2_Vector2, float2 (1, 1), _Vector2_cb26bc8c681b455c81073f0dc3c8f6b4_Out_0_Vector2, _TilingAndOffset_e5f47c2cf9d846859a72aadcdac7fc3b_Out_3_Vector2);
  float4 _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).tex, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).GetTransformedUV(_TilingAndOffset_e5f47c2cf9d846859a72aadcdac7fc3b_Out_3_Vector2), float(2));
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_R_5_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_G_6_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_B_7_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_A_8_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.a;
float _Float_3f1a7b5619414946b13e7cef19336d45_Out_0_Float = float(0.3);
float _Subtract_3105ef040f344481afb342cd9ef65e9f_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_R_5_Float, _Float_3f1a7b5619414946b13e7cef19336d45_Out_0_Float, _Subtract_3105ef040f344481afb342cd9ef65e9f_Out_2_Float);
float _Split_18495110217342bc80044d9345d98fbd_R_1_Float = IN.WorldSpacePosition[0];
float _Split_18495110217342bc80044d9345d98fbd_G_2_Float = IN.WorldSpacePosition[1];
float _Split_18495110217342bc80044d9345d98fbd_B_3_Float = IN.WorldSpacePosition[2];
float _Split_18495110217342bc80044d9345d98fbd_A_4_Float = 0;
float4 _Combine_d1ee3e49696840daa7cfd43df2287807_RGBA_4_Vector4;
float3 _Combine_d1ee3e49696840daa7cfd43df2287807_RGB_5_Vector3;
float2 _Combine_d1ee3e49696840daa7cfd43df2287807_RG_6_Vector2;
Unity_Combine_float(_Split_18495110217342bc80044d9345d98fbd_R_1_Float, _Split_18495110217342bc80044d9345d98fbd_G_2_Float, float(0), float(0), _Combine_d1ee3e49696840daa7cfd43df2287807_RGBA_4_Vector4, _Combine_d1ee3e49696840daa7cfd43df2287807_RGB_5_Vector3, _Combine_d1ee3e49696840daa7cfd43df2287807_RG_6_Vector2);
float _Property_9efe7c2851414ea39b6971daac6143e5_Out_0_Float = _Wiggle_Wind_Size;
float _Multiply_4f6b361114004341aeae12987f071375_Out_2_Float;
Unity_Multiply_float_float(_Property_9efe7c2851414ea39b6971daac6143e5_Out_0_Float, 0.763, _Multiply_4f6b361114004341aeae12987f071375_Out_2_Float);
float2 _Multiply_0d88e33790f0458b9e6e7f49946e6344_Out_2_Vector2;
Unity_Multiply_float2_float2(_Combine_d1ee3e49696840daa7cfd43df2287807_RG_6_Vector2, (_Multiply_4f6b361114004341aeae12987f071375_Out_2_Float.xx), _Multiply_0d88e33790f0458b9e6e7f49946e6344_Out_2_Vector2);
float _Property_56c994a79361438d88ea30b1e3ccf2aa_Out_0_Float = _Wiggle_Wind_Speed_Large;
float _Multiply_c5e5feed518642678424c6f0d45b4377_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Property_56c994a79361438d88ea30b1e3ccf2aa_Out_0_Float, _Multiply_c5e5feed518642678424c6f0d45b4377_Out_2_Float);
float2 _TilingAndOffset_7dac505987c144edad083d13ebca67e6_Out_3_Vector2;
Unity_TilingAndOffset_float(_Multiply_0d88e33790f0458b9e6e7f49946e6344_Out_2_Vector2, float2 (1, 1), (_Multiply_c5e5feed518642678424c6f0d45b4377_Out_2_Float.xx), _TilingAndOffset_7dac505987c144edad083d13ebca67e6_Out_3_Vector2);
  float4 _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).tex, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).GetTransformedUV(_TilingAndOffset_7dac505987c144edad083d13ebca67e6_Out_3_Vector2), float(2));
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_R_5_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_G_6_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_B_7_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_A_8_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.a;
float _Subtract_e29fd5b0daea41a1a38ac52280e6db88_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_G_6_Float, _Float_3f1a7b5619414946b13e7cef19336d45_Out_0_Float, _Subtract_e29fd5b0daea41a1a38ac52280e6db88_Out_2_Float);
float _Add_6d51be6104ee4e23a093dfb9c3d993f7_Out_2_Float;
Unity_Add_float(_Subtract_3105ef040f344481afb342cd9ef65e9f_Out_2_Float, _Subtract_e29fd5b0daea41a1a38ac52280e6db88_Out_2_Float, _Add_6d51be6104ee4e23a093dfb9c3d993f7_Out_2_Float);
float4 _Multiply_db043a8b3fc34edb8bee433085c7e061_Out_2_Vector4;
Unity_Multiply_float4_float4((_Add_6d51be6104ee4e23a093dfb9c3d993f7_Out_2_Float.xxxx), IN.VertexColor, _Multiply_db043a8b3fc34edb8bee433085c7e061_Out_2_Vector4);
float _Property_46c8c648052246a4a32e80905ed7e9df_Out_0_Float = _Wiggle_Wind_Intensity;
float4 _Multiply_4baa80310c414483a2aada72baa7a883_Out_2_Vector4;
Unity_Multiply_float4_float4(_Multiply_db043a8b3fc34edb8bee433085c7e061_Out_2_Vector4, (_Property_46c8c648052246a4a32e80905ed7e9df_Out_0_Float.xxxx), _Multiply_4baa80310c414483a2aada72baa7a883_Out_2_Vector4);
float3 _Multiply_2c95a93020c44a20be82a31b985c35b8_Out_2_Vector3;
Unity_Multiply_float3_float3((_Multiply_4baa80310c414483a2aada72baa7a883_Out_2_Vector4.xyz), IN.ObjectSpaceNormal, _Multiply_2c95a93020c44a20be82a31b985c35b8_Out_2_Vector3);
float3 _Add_5a143eeddea74bceb3ddd5b059f49da5_Out_2_Vector3;
Unity_Add_float3(_Lerp_1fbf8d0a7f5a4e2ab381967ac90385c7_Out_3_Vector3, _Multiply_2c95a93020c44a20be82a31b985c35b8_Out_2_Vector3, _Add_5a143eeddea74bceb3ddd5b059f49da5_Out_2_Vector3);
#if defined(_USE_WPO)
float3 _UseWPO_1f28bdd134c9432e812aded965021562_Out_0_Vector3 = _Add_5a143eeddea74bceb3ddd5b059f49da5_Out_2_Vector3;
#else
float3 _UseWPO_1f28bdd134c9432e812aded965021562_Out_0_Vector3 = IN.ObjectSpacePosition;
#endif
float3 _UPGFoliageContactPositionCustomFunction_3446fb1a0c894696bd903b7eb184e241_bentPositionOS_1_Vector3;
UPG_FoliageContactPosition_float(_UseWPO_1f28bdd134c9432e812aded965021562_Out_0_Vector3, _UPGFoliageContactPositionCustomFunction_3446fb1a0c894696bd903b7eb184e241_bentPositionOS_1_Vector3);
description.Position = _UPGFoliageContactPositionCustomFunction_3446fb1a0c894696bd903b7eb184e241_bentPositionOS_1_Vector3;
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
float4 _Property_a9c1982af1c74efb9ac25b0326ae7979_Out_0_Vector4 = _Foliage_Color_Bottom;
float4 _Property_50eb1cb960084765a0771df7f40a6d5d_Out_0_Vector4 = _Foliage_Color_Top;
float _Split_1540f44067c24640b4968355fa26a295_R_1_Float = IN.WorldSpacePosition[0];
float _Split_1540f44067c24640b4968355fa26a295_G_2_Float = IN.WorldSpacePosition[1];
float _Split_1540f44067c24640b4968355fa26a295_B_3_Float = IN.WorldSpacePosition[2];
float _Split_1540f44067c24640b4968355fa26a295_A_4_Float = 0;
float3 _Vector3_54c605479a824dd3a62abb1919f39c57_Out_0_Vector3 = float3(float(0), float(0), float(0));
float3 _Transform_f3f68ad0eeb04ce9ac459f2859353c06_Out_1_Vector3;
_Transform_f3f68ad0eeb04ce9ac459f2859353c06_Out_1_Vector3 = TransformObjectToWorld(_Vector3_54c605479a824dd3a62abb1919f39c57_Out_0_Vector3.xyz);
float _Split_15d5002ae66545318962a44958be8e00_R_1_Float = _Transform_f3f68ad0eeb04ce9ac459f2859353c06_Out_1_Vector3[0];
float _Split_15d5002ae66545318962a44958be8e00_G_2_Float = _Transform_f3f68ad0eeb04ce9ac459f2859353c06_Out_1_Vector3[1];
float _Split_15d5002ae66545318962a44958be8e00_B_3_Float = _Transform_f3f68ad0eeb04ce9ac459f2859353c06_Out_1_Vector3[2];
float _Split_15d5002ae66545318962a44958be8e00_A_4_Float = 0;
float _Subtract_3131eba61d024496834f2933dbde6855_Out_2_Float;
Unity_Subtract_float(_Split_1540f44067c24640b4968355fa26a295_G_2_Float, _Split_15d5002ae66545318962a44958be8e00_G_2_Float, _Subtract_3131eba61d024496834f2933dbde6855_Out_2_Float);
float _Property_605b997dac5a4228881635945f995c72_Out_0_Float = _Tree_Height;
float _Divide_8e4152d2ed4943778a44dc39efa65611_Out_2_Float;
Unity_Divide_float(_Subtract_3131eba61d024496834f2933dbde6855_Out_2_Float, _Property_605b997dac5a4228881635945f995c72_Out_0_Float, _Divide_8e4152d2ed4943778a44dc39efa65611_Out_2_Float);
float _Saturate_086c13233c3d43fab6a33a1e2ed498c2_Out_1_Float;
Unity_Saturate_float(_Divide_8e4152d2ed4943778a44dc39efa65611_Out_2_Float, _Saturate_086c13233c3d43fab6a33a1e2ed498c2_Out_1_Float);
float _Property_0e5bff248b2341909121bb16666f3468_Out_0_Float = _Gradience_Power;
float _Power_be0c59cc158d492f99ea24f3b1001e1f_Out_2_Float;
Unity_Power_float(_Saturate_086c13233c3d43fab6a33a1e2ed498c2_Out_1_Float, _Property_0e5bff248b2341909121bb16666f3468_Out_0_Float, _Power_be0c59cc158d492f99ea24f3b1001e1f_Out_2_Float);
float _Saturate_26bbc2447b5a402c9562a9e5d012fd6c_Out_1_Float;
Unity_Saturate_float(_Power_be0c59cc158d492f99ea24f3b1001e1f_Out_2_Float, _Saturate_26bbc2447b5a402c9562a9e5d012fd6c_Out_1_Float);
float4 _Lerp_795c581446834fe0bee35971bc8f0c5c_Out_3_Vector4;
Unity_Lerp_float4(_Property_a9c1982af1c74efb9ac25b0326ae7979_Out_0_Vector4, _Property_50eb1cb960084765a0771df7f40a6d5d_Out_0_Vector4, (_Saturate_26bbc2447b5a402c9562a9e5d012fd6c_Out_1_Float.xxxx), _Lerp_795c581446834fe0bee35971bc8f0c5c_Out_3_Vector4);
float4 _Property_4036fdaeaa4647adb3dd84d70ff1161e_Out_0_Vector4 = _Foliage_Color_Top;
#if defined(_USE_HEIGHT_GRADIENCE)
float4 _UseHeightGradience_d4326b9500c14f11a9090b7e8f107721_Out_0_Vector4 = _Lerp_795c581446834fe0bee35971bc8f0c5c_Out_3_Vector4;
#else
float4 _UseHeightGradience_d4326b9500c14f11a9090b7e8f107721_Out_0_Vector4 = _Property_4036fdaeaa4647adb3dd84d70ff1161e_Out_0_Vector4;
#endif
UnityTexture2D _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Color_Map, sampler_Color_Map, _Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_eaea7ba5e1e74519a1bc7833f0103ab8_Out_0_Vector4 = IN.uv0;
float _Property_95352575ae4140008287fd58138e8eb5_Out_0_Float = _Color_Map_Tiling;
float4 _Multiply_86fafeae394745fe880655d03cafd9e1_Out_2_Vector4;
Unity_Multiply_float4_float4(_UV_eaea7ba5e1e74519a1bc7833f0103ab8_Out_0_Vector4, (_Property_95352575ae4140008287fd58138e8eb5_Out_0_Float.xxxx), _Multiply_86fafeae394745fe880655d03cafd9e1_Out_2_Vector4);
float4 _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.tex, _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.samplerstate, _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.GetTransformedUV((_Multiply_86fafeae394745fe880655d03cafd9e1_Out_2_Vector4.xy)) );
if (_Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4, _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_R_4_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.r;
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_G_5_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.g;
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_B_6_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.b;
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_A_7_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.a;
float4 _Multiply_a05088f8e3434d54920d012fc47555a7_Out_2_Vector4;
Unity_Multiply_float4_float4(_UseHeightGradience_d4326b9500c14f11a9090b7e8f107721_Out_0_Vector4, _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4, _Multiply_a05088f8e3434d54920d012fc47555a7_Out_2_Vector4);
float4 _Property_ee5875e8bc214caba4dbea1511d06cd2_Out_0_Vector4 = _Always_On_Fresnel_Color;
float _Property_be2ed593ceb345f89b57d123af3db203_Out_0_Float = _Always_On_Fresnel_Intensity;
float4 _Multiply_dcf6e90c046c4f72b77b81df5b922468_Out_2_Vector4;
Unity_Multiply_float4_float4(_Property_ee5875e8bc214caba4dbea1511d06cd2_Out_0_Vector4, (_Property_be2ed593ceb345f89b57d123af3db203_Out_0_Float.xxxx), _Multiply_dcf6e90c046c4f72b77b81df5b922468_Out_2_Vector4);
float4 _Saturate_26f2848ceb0a494094065a9387275032_Out_1_Vector4;
Unity_Saturate_float4(_Multiply_dcf6e90c046c4f72b77b81df5b922468_Out_2_Vector4, _Saturate_26f2848ceb0a494094065a9387275032_Out_1_Vector4);
float3 _Normalize_764cced5d0894f3d89f5911d50d5f4a2_Out_1_Vector3;
Unity_Normalize_float3(IN.WorldSpaceViewDirection, _Normalize_764cced5d0894f3d89f5911d50d5f4a2_Out_1_Vector3);
float _DotProduct_eb716c3ca301415688a52e76b310a65c_Out_2_Float;
Unity_DotProduct_float3(IN.WorldSpaceNormal, _Normalize_764cced5d0894f3d89f5911d50d5f4a2_Out_1_Vector3, _DotProduct_eb716c3ca301415688a52e76b310a65c_Out_2_Float);
float _Absolute_470cf4f0298a44a695f47aa5ea61c0ab_Out_1_Float;
Unity_Absolute_float(_DotProduct_eb716c3ca301415688a52e76b310a65c_Out_2_Float, _Absolute_470cf4f0298a44a695f47aa5ea61c0ab_Out_1_Float);
float _OneMinus_fa832b719fc24001be421507d395a592_Out_1_Float;
Unity_OneMinus_float(_Absolute_470cf4f0298a44a695f47aa5ea61c0ab_Out_1_Float, _OneMinus_fa832b719fc24001be421507d395a592_Out_1_Float);
float _Property_d9cfcacb90204bdfa36d0c1b7c0da452_Out_0_Float = _Always_On_Fresnel_Pow;
float _Power_7841b1ae85554473b96cce132c7ff7e1_Out_2_Float;
Unity_Power_float(_OneMinus_fa832b719fc24001be421507d395a592_Out_1_Float, _Property_d9cfcacb90204bdfa36d0c1b7c0da452_Out_0_Float, _Power_7841b1ae85554473b96cce132c7ff7e1_Out_2_Float);
float _Saturate_908f6307f948471ca95c0e7507cd7e43_Out_1_Float;
Unity_Saturate_float(_Power_7841b1ae85554473b96cce132c7ff7e1_Out_2_Float, _Saturate_908f6307f948471ca95c0e7507cd7e43_Out_1_Float);
float4 _Lerp_3406aa7c52b14c818ac9eb76bf068e2d_Out_3_Vector4;
Unity_Lerp_float4(_Multiply_a05088f8e3434d54920d012fc47555a7_Out_2_Vector4, _Saturate_26f2848ceb0a494094065a9387275032_Out_1_Vector4, (_Saturate_908f6307f948471ca95c0e7507cd7e43_Out_1_Float.xxxx), _Lerp_3406aa7c52b14c818ac9eb76bf068e2d_Out_3_Vector4);
float _Property_e8b3335851f84d91b28234460148cd0f_Out_0_Float = _Subsurface_Intensity;
float4 _Multiply_94df75e4ad734308a11d784250fc0b0c_Out_2_Vector4;
Unity_Multiply_float4_float4(_Lerp_3406aa7c52b14c818ac9eb76bf068e2d_Out_3_Vector4, (_Property_e8b3335851f84d91b28234460148cd0f_Out_0_Float.xxxx), _Multiply_94df75e4ad734308a11d784250fc0b0c_Out_2_Vector4);
float4 _Saturate_f0090edc5c664784a336737069276109_Out_1_Vector4;
Unity_Saturate_float4(_Multiply_94df75e4ad734308a11d784250fc0b0c_Out_2_Vector4, _Saturate_f0090edc5c664784a336737069276109_Out_1_Vector4);
float3 _MainLightCustomFunction_461af74b29b6456da89a2348455c2483_Direction_0_Vector3;
MainLight_float(_MainLightCustomFunction_461af74b29b6456da89a2348455c2483_Direction_0_Vector3);
float3 _Normalize_8644c278d597434b8871eb11a7a52cfe_Out_1_Vector3;
Unity_Normalize_float3(_MainLightCustomFunction_461af74b29b6456da89a2348455c2483_Direction_0_Vector3, _Normalize_8644c278d597434b8871eb11a7a52cfe_Out_1_Vector3);
float _Split_8a104625401f4a048b1160e17800fd11_R_1_Float = _Normalize_8644c278d597434b8871eb11a7a52cfe_Out_1_Vector3[0];
float _Split_8a104625401f4a048b1160e17800fd11_G_2_Float = _Normalize_8644c278d597434b8871eb11a7a52cfe_Out_1_Vector3[1];
float _Split_8a104625401f4a048b1160e17800fd11_B_3_Float = _Normalize_8644c278d597434b8871eb11a7a52cfe_Out_1_Vector3[2];
float _Split_8a104625401f4a048b1160e17800fd11_A_4_Float = 0;
float4 _Combine_87a90606ca4545b2983770732c0ed1b7_RGBA_4_Vector4;
float3 _Combine_87a90606ca4545b2983770732c0ed1b7_RGB_5_Vector3;
float2 _Combine_87a90606ca4545b2983770732c0ed1b7_RG_6_Vector2;
Unity_Combine_float(_Split_8a104625401f4a048b1160e17800fd11_R_1_Float, _Split_8a104625401f4a048b1160e17800fd11_B_3_Float, float(0), float(0), _Combine_87a90606ca4545b2983770732c0ed1b7_RGBA_4_Vector4, _Combine_87a90606ca4545b2983770732c0ed1b7_RGB_5_Vector3, _Combine_87a90606ca4545b2983770732c0ed1b7_RG_6_Vector2);
float3 _Normalize_f1ad7ea0af7b45ee88df143397ba6727_Out_1_Vector3;
Unity_Normalize_float3(-UNITY_MATRIX_V[2].xyz, _Normalize_f1ad7ea0af7b45ee88df143397ba6727_Out_1_Vector3);
float3 _Multiply_5b2d23749b2347b7af4472236c1dfe22_Out_2_Vector3;
Unity_Multiply_float3_float3(_Normalize_f1ad7ea0af7b45ee88df143397ba6727_Out_1_Vector3, float3(-1, -1, -1), _Multiply_5b2d23749b2347b7af4472236c1dfe22_Out_2_Vector3);
float _Split_6ebebe9410ea41bd8f8a327255848dc9_R_1_Float = _Multiply_5b2d23749b2347b7af4472236c1dfe22_Out_2_Vector3[0];
float _Split_6ebebe9410ea41bd8f8a327255848dc9_G_2_Float = _Multiply_5b2d23749b2347b7af4472236c1dfe22_Out_2_Vector3[1];
float _Split_6ebebe9410ea41bd8f8a327255848dc9_B_3_Float = _Multiply_5b2d23749b2347b7af4472236c1dfe22_Out_2_Vector3[2];
float _Split_6ebebe9410ea41bd8f8a327255848dc9_A_4_Float = 0;
float4 _Combine_ce9284087dd446409babf9c154f18fe4_RGBA_4_Vector4;
float3 _Combine_ce9284087dd446409babf9c154f18fe4_RGB_5_Vector3;
float2 _Combine_ce9284087dd446409babf9c154f18fe4_RG_6_Vector2;
Unity_Combine_float(_Split_6ebebe9410ea41bd8f8a327255848dc9_R_1_Float, _Split_6ebebe9410ea41bd8f8a327255848dc9_B_3_Float, float(0), float(0), _Combine_ce9284087dd446409babf9c154f18fe4_RGBA_4_Vector4, _Combine_ce9284087dd446409babf9c154f18fe4_RGB_5_Vector3, _Combine_ce9284087dd446409babf9c154f18fe4_RG_6_Vector2);
float _DotProduct_9e4f6525393a4171b6519e347a4b7a37_Out_2_Float;
Unity_DotProduct_float2(_Combine_87a90606ca4545b2983770732c0ed1b7_RG_6_Vector2, _Combine_ce9284087dd446409babf9c154f18fe4_RG_6_Vector2, _DotProduct_9e4f6525393a4171b6519e347a4b7a37_Out_2_Float);
float _Saturate_f76f8f4113e0468b9b20a6abf253d76e_Out_1_Float;
Unity_Saturate_float(_DotProduct_9e4f6525393a4171b6519e347a4b7a37_Out_2_Float, _Saturate_f76f8f4113e0468b9b20a6abf253d76e_Out_1_Float);
float _Property_bd14cfb26b074dbabde1a5a9b7f95d97_Out_0_Float = _Subsurface_Transition_Smoothness;
float _Power_2bc1d129029a4da69b2993ea07ff412f_Out_2_Float;
Unity_Power_float(_Saturate_f76f8f4113e0468b9b20a6abf253d76e_Out_1_Float, _Property_bd14cfb26b074dbabde1a5a9b7f95d97_Out_0_Float, _Power_2bc1d129029a4da69b2993ea07ff412f_Out_2_Float);
float _Saturate_44588f923e1b48ff97cd55ea2d69aa2b_Out_1_Float;
Unity_Saturate_float(_Power_2bc1d129029a4da69b2993ea07ff412f_Out_2_Float, _Saturate_44588f923e1b48ff97cd55ea2d69aa2b_Out_1_Float);
float4 _Lerp_98f83471835040419ec38e468d855fe3_Out_3_Vector4;
Unity_Lerp_float4(_Lerp_3406aa7c52b14c818ac9eb76bf068e2d_Out_3_Vector4, _Saturate_f0090edc5c664784a336737069276109_Out_1_Vector4, (_Saturate_44588f923e1b48ff97cd55ea2d69aa2b_Out_1_Float.xxxx), _Lerp_98f83471835040419ec38e468d855fe3_Out_3_Vector4);
float _Property_e00e00901e414a40a25484eb2c276584_Out_0_Float = _Subsurface_Fresnel_Intensity;
float4 _Multiply_6ed60680651f4e8cbff9e6f2ed97f0b9_Out_2_Vector4;
Unity_Multiply_float4_float4(_Lerp_98f83471835040419ec38e468d855fe3_Out_3_Vector4, (_Property_e00e00901e414a40a25484eb2c276584_Out_0_Float.xxxx), _Multiply_6ed60680651f4e8cbff9e6f2ed97f0b9_Out_2_Vector4);
float4 _Saturate_5cea81f1b9b5405fb4a571a97e57df52_Out_1_Vector4;
Unity_Saturate_float4(_Multiply_6ed60680651f4e8cbff9e6f2ed97f0b9_Out_2_Vector4, _Saturate_5cea81f1b9b5405fb4a571a97e57df52_Out_1_Vector4);
float _Saturate_10ad388197014e2fa2d0bd8261d82207_Out_1_Float;
Unity_Saturate_float(_DotProduct_9e4f6525393a4171b6519e347a4b7a37_Out_2_Float, _Saturate_10ad388197014e2fa2d0bd8261d82207_Out_1_Float);
float _Property_fe3bfd62823b45bf868d2c88982c1f64_Out_0_Float = _Subsurface_Fresnel_Transition_Smoothness;
float _Power_1ad124a5d5b74c13848df4f8f171d448_Out_2_Float;
Unity_Power_float(_Saturate_10ad388197014e2fa2d0bd8261d82207_Out_1_Float, _Property_fe3bfd62823b45bf868d2c88982c1f64_Out_0_Float, _Power_1ad124a5d5b74c13848df4f8f171d448_Out_2_Float);
float3 _Normalize_7e92e50b98274c508fa061a32d7a99b7_Out_1_Vector3;
Unity_Normalize_float3(IN.WorldSpaceViewDirection, _Normalize_7e92e50b98274c508fa061a32d7a99b7_Out_1_Vector3);
float _DotProduct_3170aef4b99740d083af85a5aa294edd_Out_2_Float;
Unity_DotProduct_float3(IN.WorldSpaceNormal, _Normalize_7e92e50b98274c508fa061a32d7a99b7_Out_1_Vector3, _DotProduct_3170aef4b99740d083af85a5aa294edd_Out_2_Float);
float _Absolute_111753c15e794f4b8e885c2c19d93d82_Out_1_Float;
Unity_Absolute_float(_DotProduct_3170aef4b99740d083af85a5aa294edd_Out_2_Float, _Absolute_111753c15e794f4b8e885c2c19d93d82_Out_1_Float);
float _OneMinus_2a02f0d378e1454c9aeda0ff447d4d86_Out_1_Float;
Unity_OneMinus_float(_Absolute_111753c15e794f4b8e885c2c19d93d82_Out_1_Float, _OneMinus_2a02f0d378e1454c9aeda0ff447d4d86_Out_1_Float);
float _Property_49c0203b77734a36b9ba06f7bcd723ee_Out_0_Float = _Subsurface_Fresnel_Pow;
float _Power_71121b48d5934b11a14aeadcad656816_Out_2_Float;
Unity_Power_float(_OneMinus_2a02f0d378e1454c9aeda0ff447d4d86_Out_1_Float, _Property_49c0203b77734a36b9ba06f7bcd723ee_Out_0_Float, _Power_71121b48d5934b11a14aeadcad656816_Out_2_Float);
float _Saturate_29e69580558f40c49a883332cdf25357_Out_1_Float;
Unity_Saturate_float(_Power_71121b48d5934b11a14aeadcad656816_Out_2_Float, _Saturate_29e69580558f40c49a883332cdf25357_Out_1_Float);
float _Multiply_328c604c9492493ba1dc1999ef6314fd_Out_2_Float;
Unity_Multiply_float_float(_Power_1ad124a5d5b74c13848df4f8f171d448_Out_2_Float, _Saturate_29e69580558f40c49a883332cdf25357_Out_1_Float, _Multiply_328c604c9492493ba1dc1999ef6314fd_Out_2_Float);
float4 _Lerp_10a8be366b974da7b00eab9c55f88a8a_Out_3_Vector4;
Unity_Lerp_float4(_Lerp_98f83471835040419ec38e468d855fe3_Out_3_Vector4, _Saturate_5cea81f1b9b5405fb4a571a97e57df52_Out_1_Vector4, (_Multiply_328c604c9492493ba1dc1999ef6314fd_Out_2_Float.xxxx), _Lerp_10a8be366b974da7b00eab9c55f88a8a_Out_3_Vector4);
float _Multiply_cf1ea983d11e4ab7b4bdbbb064ad94b5_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_A_7_Float, 1, _Multiply_cf1ea983d11e4ab7b4bdbbb064ad94b5_Out_2_Float);
float _Property_99bb0b22edbb4ea0b1b7844414b876a7_Out_0_Float = _Alpha_Clip;
surface.BaseColor = (_Lerp_10a8be366b974da7b00eab9c55f88a8a_Out_3_Vector4.xyz);
surface.Emission = float3(0, 0, 0);
surface.Alpha = _Multiply_cf1ea983d11e4ab7b4bdbbb064ad94b5_Out_2_Float;
surface.AlphaClipThreshold = _Property_99bb0b22edbb4ea0b1b7844414b876a7_Out_0_Float;
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


    output.WorldSpaceViewDirection = GetWorldSpaceNormalizeViewDir(input.positionWS);
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
#pragma shader_feature_local _ _USE_WPO
#pragma shader_feature_local _ _USE_HEIGHT_GRADIENCE

#if defined(_USE_WPO) && defined(_USE_HEIGHT_GRADIENCE)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_WPO)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_HEIGHT_GRADIENCE)
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

float4 _Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D_TexelSize;
float _Subsurface_Fresnel_Transition_Smoothness;
float _Subsurface_Fresnel_Pow;
float _Subsurface_Fresnel_Intensity;
float _Subsurface_Transition_Smoothness;
float _Wiggle_Wind_Speed_Large;
float _Wiggle_Wind_Intensity;
float _Roughness;
float4 _Foliage_Color_Bottom;
float _Sway_Wind_Speed;
float _Gradience_Power;
float _Fresnel_Mul;
float4 _Foliage_Color_Top;
float _Always_On_Fresnel_Intensity;
float _Tree_Height;
float4 _Color_Map_TexelSize;
float _Height_Weight;
float _Height_Gradience;
float _Sway_Wind_Time_Random_Offset_Amount;
float _Sway_Wind_Intensity;
float3 _Sway_Wind_Direction;
float _Color_Map_Tiling;
float _Subsurface_Intensity;
float _Alpha_Clip;
float _Wiggle_Wind_Size;
float _Wiggle_Wind_Speed_Small;
float _Fresnel_Pow;
float _Always_On_Fresnel_Pow;
float4 _Always_On_Fresnel_Color;
float _ree;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D);
SAMPLER(sampler_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D);
TEXTURE2D(_Color_Map);
SAMPLER(sampler_Color_Map);

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

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
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
float _Property_7689f3a0523b4af9912879a05d1d5206_Out_0_Float = _Sway_Wind_Speed;
float3 _Property_e0531013c46845b6b036ac453118c744_Out_0_Vector3 = _Sway_Wind_Direction;
float _Property_35588e6a16b5457695dba39b1644d669_Out_0_Float = _Sway_Wind_Intensity;
float _Property_c8d3d7e6e6f64d409f010d988b96e327_Out_0_Float = _Sway_Wind_Time_Random_Offset_Amount;
Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float _WindSway_662bcd4f8b2b4607b138cb44de00a708;
_WindSway_662bcd4f8b2b4607b138cb44de00a708.ObjectSpacePosition = IN.ObjectSpacePosition;
_WindSway_662bcd4f8b2b4607b138cb44de00a708.TimeParameters = IN.TimeParameters;
float3 _WindSway_662bcd4f8b2b4607b138cb44de00a708_OutVector3_1_Vector3;
SG_WindSway_51c4027ae50a8cd49be53c4990b9f179_float(_Property_7689f3a0523b4af9912879a05d1d5206_Out_0_Float, _Property_e0531013c46845b6b036ac453118c744_Out_0_Vector3, _Property_35588e6a16b5457695dba39b1644d669_Out_0_Float, _Property_c8d3d7e6e6f64d409f010d988b96e327_Out_0_Float, _WindSway_662bcd4f8b2b4607b138cb44de00a708, _WindSway_662bcd4f8b2b4607b138cb44de00a708_OutVector3_1_Vector3);
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_R_1_Float = IN.WorldSpacePosition[0];
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_G_2_Float = IN.WorldSpacePosition[1];
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_B_3_Float = IN.WorldSpacePosition[2];
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_A_4_Float = 0;
float3 _Vector3_e154b524054546d2bbeab293c0e20519_Out_0_Vector3 = float3(float(0), float(0), float(0));
float3 _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3;
_Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3 = TransformObjectToWorld(_Vector3_e154b524054546d2bbeab293c0e20519_Out_0_Vector3.xyz);
float _Split_5eb48153bba44ea2b128583819f09a11_R_1_Float = _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3[0];
float _Split_5eb48153bba44ea2b128583819f09a11_G_2_Float = _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3[1];
float _Split_5eb48153bba44ea2b128583819f09a11_B_3_Float = _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3[2];
float _Split_5eb48153bba44ea2b128583819f09a11_A_4_Float = 0;
float _Subtract_d6a99abdf4e74644acffc1718d1735bf_Out_2_Float;
Unity_Subtract_float(_Split_8fe962eb6f0f4d6fba734f9edcaf9067_G_2_Float, _Split_5eb48153bba44ea2b128583819f09a11_G_2_Float, _Subtract_d6a99abdf4e74644acffc1718d1735bf_Out_2_Float);
float _Property_1cf3cda52e094e638df491b003607dfd_Out_0_Float = _Height_Weight;
float _Split_145cf2161d4c4510b08248cad819f655_R_1_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[0];
float _Split_145cf2161d4c4510b08248cad819f655_G_2_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[1];
float _Split_145cf2161d4c4510b08248cad819f655_B_3_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[2];
float _Split_145cf2161d4c4510b08248cad819f655_A_4_Float = 0;
float _Multiply_2c140d573b9447aab058bd863fe2f4f7_Out_2_Float;
Unity_Multiply_float_float(_Property_1cf3cda52e094e638df491b003607dfd_Out_0_Float, _Split_145cf2161d4c4510b08248cad819f655_G_2_Float, _Multiply_2c140d573b9447aab058bd863fe2f4f7_Out_2_Float);
float _Divide_0f6a637143d841d0b1c94fa2ae3e164d_Out_2_Float;
Unity_Divide_float(_Subtract_d6a99abdf4e74644acffc1718d1735bf_Out_2_Float, _Multiply_2c140d573b9447aab058bd863fe2f4f7_Out_2_Float, _Divide_0f6a637143d841d0b1c94fa2ae3e164d_Out_2_Float);
float _Saturate_1e4e29fb5c074565a1f941c046957b05_Out_1_Float;
Unity_Saturate_float(_Divide_0f6a637143d841d0b1c94fa2ae3e164d_Out_2_Float, _Saturate_1e4e29fb5c074565a1f941c046957b05_Out_1_Float);
float _Property_3d64a0c8dd104507b9a0762a778fe585_Out_0_Float = _Height_Gradience;
float _Power_f1d74728398a45f9993fe3b3affb6584_Out_2_Float;
Unity_Power_float(_Saturate_1e4e29fb5c074565a1f941c046957b05_Out_1_Float, _Property_3d64a0c8dd104507b9a0762a778fe585_Out_0_Float, _Power_f1d74728398a45f9993fe3b3affb6584_Out_2_Float);
float _Saturate_57b130350452418fa1d09d4a0ddd64ac_Out_1_Float;
Unity_Saturate_float(_Power_f1d74728398a45f9993fe3b3affb6584_Out_2_Float, _Saturate_57b130350452418fa1d09d4a0ddd64ac_Out_1_Float);
float3 _Lerp_1fbf8d0a7f5a4e2ab381967ac90385c7_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _WindSway_662bcd4f8b2b4607b138cb44de00a708_OutVector3_1_Vector3, (_Saturate_57b130350452418fa1d09d4a0ddd64ac_Out_1_Float.xxx), _Lerp_1fbf8d0a7f5a4e2ab381967ac90385c7_Out_3_Vector3);
float _Split_bd04916788224fa2bb71be3f27eb839a_R_1_Float = IN.WorldSpacePosition[0];
float _Split_bd04916788224fa2bb71be3f27eb839a_G_2_Float = IN.WorldSpacePosition[1];
float _Split_bd04916788224fa2bb71be3f27eb839a_B_3_Float = IN.WorldSpacePosition[2];
float _Split_bd04916788224fa2bb71be3f27eb839a_A_4_Float = 0;
float4 _Combine_45cdab9c20414d03862100967f56c78d_RGBA_4_Vector4;
float3 _Combine_45cdab9c20414d03862100967f56c78d_RGB_5_Vector3;
float2 _Combine_45cdab9c20414d03862100967f56c78d_RG_6_Vector2;
Unity_Combine_float(_Split_bd04916788224fa2bb71be3f27eb839a_R_1_Float, _Split_bd04916788224fa2bb71be3f27eb839a_B_3_Float, float(0), float(0), _Combine_45cdab9c20414d03862100967f56c78d_RGBA_4_Vector4, _Combine_45cdab9c20414d03862100967f56c78d_RGB_5_Vector3, _Combine_45cdab9c20414d03862100967f56c78d_RG_6_Vector2);
float _Property_9b35f6244c0247c7a9be7af2671f58e7_Out_0_Float = _Wiggle_Wind_Size;
float2 _Multiply_ba4d7f376f904143acc9eb33d8414ff8_Out_2_Vector2;
Unity_Multiply_float2_float2(_Combine_45cdab9c20414d03862100967f56c78d_RG_6_Vector2, (_Property_9b35f6244c0247c7a9be7af2671f58e7_Out_0_Float.xx), _Multiply_ba4d7f376f904143acc9eb33d8414ff8_Out_2_Vector2);
float _Property_6eae25110c184e7a94293b95ddc9ecef_Out_0_Float = _Wiggle_Wind_Speed_Small;
float _Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Property_6eae25110c184e7a94293b95ddc9ecef_Out_0_Float, _Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float);
float _Multiply_7510d5a95e6c4d3986bb83059d23d2fa_Out_2_Float;
Unity_Multiply_float_float(_Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float, 0.8, _Multiply_7510d5a95e6c4d3986bb83059d23d2fa_Out_2_Float);
float2 _Vector2_cb26bc8c681b455c81073f0dc3c8f6b4_Out_0_Vector2 = float2(_Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float, _Multiply_7510d5a95e6c4d3986bb83059d23d2fa_Out_2_Float);
float2 _TilingAndOffset_e5f47c2cf9d846859a72aadcdac7fc3b_Out_3_Vector2;
Unity_TilingAndOffset_float(_Multiply_ba4d7f376f904143acc9eb33d8414ff8_Out_2_Vector2, float2 (1, 1), _Vector2_cb26bc8c681b455c81073f0dc3c8f6b4_Out_0_Vector2, _TilingAndOffset_e5f47c2cf9d846859a72aadcdac7fc3b_Out_3_Vector2);
  float4 _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).tex, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).GetTransformedUV(_TilingAndOffset_e5f47c2cf9d846859a72aadcdac7fc3b_Out_3_Vector2), float(2));
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_R_5_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_G_6_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_B_7_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_A_8_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.a;
float _Float_3f1a7b5619414946b13e7cef19336d45_Out_0_Float = float(0.3);
float _Subtract_3105ef040f344481afb342cd9ef65e9f_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_R_5_Float, _Float_3f1a7b5619414946b13e7cef19336d45_Out_0_Float, _Subtract_3105ef040f344481afb342cd9ef65e9f_Out_2_Float);
float _Split_18495110217342bc80044d9345d98fbd_R_1_Float = IN.WorldSpacePosition[0];
float _Split_18495110217342bc80044d9345d98fbd_G_2_Float = IN.WorldSpacePosition[1];
float _Split_18495110217342bc80044d9345d98fbd_B_3_Float = IN.WorldSpacePosition[2];
float _Split_18495110217342bc80044d9345d98fbd_A_4_Float = 0;
float4 _Combine_d1ee3e49696840daa7cfd43df2287807_RGBA_4_Vector4;
float3 _Combine_d1ee3e49696840daa7cfd43df2287807_RGB_5_Vector3;
float2 _Combine_d1ee3e49696840daa7cfd43df2287807_RG_6_Vector2;
Unity_Combine_float(_Split_18495110217342bc80044d9345d98fbd_R_1_Float, _Split_18495110217342bc80044d9345d98fbd_G_2_Float, float(0), float(0), _Combine_d1ee3e49696840daa7cfd43df2287807_RGBA_4_Vector4, _Combine_d1ee3e49696840daa7cfd43df2287807_RGB_5_Vector3, _Combine_d1ee3e49696840daa7cfd43df2287807_RG_6_Vector2);
float _Property_9efe7c2851414ea39b6971daac6143e5_Out_0_Float = _Wiggle_Wind_Size;
float _Multiply_4f6b361114004341aeae12987f071375_Out_2_Float;
Unity_Multiply_float_float(_Property_9efe7c2851414ea39b6971daac6143e5_Out_0_Float, 0.763, _Multiply_4f6b361114004341aeae12987f071375_Out_2_Float);
float2 _Multiply_0d88e33790f0458b9e6e7f49946e6344_Out_2_Vector2;
Unity_Multiply_float2_float2(_Combine_d1ee3e49696840daa7cfd43df2287807_RG_6_Vector2, (_Multiply_4f6b361114004341aeae12987f071375_Out_2_Float.xx), _Multiply_0d88e33790f0458b9e6e7f49946e6344_Out_2_Vector2);
float _Property_56c994a79361438d88ea30b1e3ccf2aa_Out_0_Float = _Wiggle_Wind_Speed_Large;
float _Multiply_c5e5feed518642678424c6f0d45b4377_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Property_56c994a79361438d88ea30b1e3ccf2aa_Out_0_Float, _Multiply_c5e5feed518642678424c6f0d45b4377_Out_2_Float);
float2 _TilingAndOffset_7dac505987c144edad083d13ebca67e6_Out_3_Vector2;
Unity_TilingAndOffset_float(_Multiply_0d88e33790f0458b9e6e7f49946e6344_Out_2_Vector2, float2 (1, 1), (_Multiply_c5e5feed518642678424c6f0d45b4377_Out_2_Float.xx), _TilingAndOffset_7dac505987c144edad083d13ebca67e6_Out_3_Vector2);
  float4 _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).tex, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).GetTransformedUV(_TilingAndOffset_7dac505987c144edad083d13ebca67e6_Out_3_Vector2), float(2));
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_R_5_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_G_6_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_B_7_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_A_8_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.a;
float _Subtract_e29fd5b0daea41a1a38ac52280e6db88_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_G_6_Float, _Float_3f1a7b5619414946b13e7cef19336d45_Out_0_Float, _Subtract_e29fd5b0daea41a1a38ac52280e6db88_Out_2_Float);
float _Add_6d51be6104ee4e23a093dfb9c3d993f7_Out_2_Float;
Unity_Add_float(_Subtract_3105ef040f344481afb342cd9ef65e9f_Out_2_Float, _Subtract_e29fd5b0daea41a1a38ac52280e6db88_Out_2_Float, _Add_6d51be6104ee4e23a093dfb9c3d993f7_Out_2_Float);
float4 _Multiply_db043a8b3fc34edb8bee433085c7e061_Out_2_Vector4;
Unity_Multiply_float4_float4((_Add_6d51be6104ee4e23a093dfb9c3d993f7_Out_2_Float.xxxx), IN.VertexColor, _Multiply_db043a8b3fc34edb8bee433085c7e061_Out_2_Vector4);
float _Property_46c8c648052246a4a32e80905ed7e9df_Out_0_Float = _Wiggle_Wind_Intensity;
float4 _Multiply_4baa80310c414483a2aada72baa7a883_Out_2_Vector4;
Unity_Multiply_float4_float4(_Multiply_db043a8b3fc34edb8bee433085c7e061_Out_2_Vector4, (_Property_46c8c648052246a4a32e80905ed7e9df_Out_0_Float.xxxx), _Multiply_4baa80310c414483a2aada72baa7a883_Out_2_Vector4);
float3 _Multiply_2c95a93020c44a20be82a31b985c35b8_Out_2_Vector3;
Unity_Multiply_float3_float3((_Multiply_4baa80310c414483a2aada72baa7a883_Out_2_Vector4.xyz), IN.ObjectSpaceNormal, _Multiply_2c95a93020c44a20be82a31b985c35b8_Out_2_Vector3);
float3 _Add_5a143eeddea74bceb3ddd5b059f49da5_Out_2_Vector3;
Unity_Add_float3(_Lerp_1fbf8d0a7f5a4e2ab381967ac90385c7_Out_3_Vector3, _Multiply_2c95a93020c44a20be82a31b985c35b8_Out_2_Vector3, _Add_5a143eeddea74bceb3ddd5b059f49da5_Out_2_Vector3);
#if defined(_USE_WPO)
float3 _UseWPO_1f28bdd134c9432e812aded965021562_Out_0_Vector3 = _Add_5a143eeddea74bceb3ddd5b059f49da5_Out_2_Vector3;
#else
float3 _UseWPO_1f28bdd134c9432e812aded965021562_Out_0_Vector3 = IN.ObjectSpacePosition;
#endif
float3 _UPGFoliageContactPositionCustomFunction_3446fb1a0c894696bd903b7eb184e241_bentPositionOS_1_Vector3;
UPG_FoliageContactPosition_float(_UseWPO_1f28bdd134c9432e812aded965021562_Out_0_Vector3, _UPGFoliageContactPositionCustomFunction_3446fb1a0c894696bd903b7eb184e241_bentPositionOS_1_Vector3);
description.Position = _UPGFoliageContactPositionCustomFunction_3446fb1a0c894696bd903b7eb184e241_bentPositionOS_1_Vector3;
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
UnityTexture2D _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Color_Map, sampler_Color_Map, _Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_eaea7ba5e1e74519a1bc7833f0103ab8_Out_0_Vector4 = IN.uv0;
float _Property_95352575ae4140008287fd58138e8eb5_Out_0_Float = _Color_Map_Tiling;
float4 _Multiply_86fafeae394745fe880655d03cafd9e1_Out_2_Vector4;
Unity_Multiply_float4_float4(_UV_eaea7ba5e1e74519a1bc7833f0103ab8_Out_0_Vector4, (_Property_95352575ae4140008287fd58138e8eb5_Out_0_Float.xxxx), _Multiply_86fafeae394745fe880655d03cafd9e1_Out_2_Vector4);
float4 _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.tex, _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.samplerstate, _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.GetTransformedUV((_Multiply_86fafeae394745fe880655d03cafd9e1_Out_2_Vector4.xy)) );
if (_Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4, _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_R_4_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.r;
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_G_5_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.g;
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_B_6_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.b;
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_A_7_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.a;
float _Multiply_cf1ea983d11e4ab7b4bdbbb064ad94b5_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_A_7_Float, 1, _Multiply_cf1ea983d11e4ab7b4bdbbb064ad94b5_Out_2_Float);
float _Property_99bb0b22edbb4ea0b1b7844414b876a7_Out_0_Float = _Alpha_Clip;
surface.Alpha = _Multiply_cf1ea983d11e4ab7b4bdbbb064ad94b5_Out_2_Float;
surface.AlphaClipThreshold = _Property_99bb0b22edbb4ea0b1b7844414b876a7_Out_0_Float;
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
#pragma shader_feature_local _ _USE_WPO
#pragma shader_feature_local _ _USE_HEIGHT_GRADIENCE

#if defined(_USE_WPO) && defined(_USE_HEIGHT_GRADIENCE)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_WPO)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_HEIGHT_GRADIENCE)
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
#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_NORMAL_WS
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
 float3 WorldSpaceViewDirection;
 float3 WorldSpacePosition;
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 WorldSpacePosition;
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

float4 _Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D_TexelSize;
float _Subsurface_Fresnel_Transition_Smoothness;
float _Subsurface_Fresnel_Pow;
float _Subsurface_Fresnel_Intensity;
float _Subsurface_Transition_Smoothness;
float _Wiggle_Wind_Speed_Large;
float _Wiggle_Wind_Intensity;
float _Roughness;
float4 _Foliage_Color_Bottom;
float _Sway_Wind_Speed;
float _Gradience_Power;
float _Fresnel_Mul;
float4 _Foliage_Color_Top;
float _Always_On_Fresnel_Intensity;
float _Tree_Height;
float4 _Color_Map_TexelSize;
float _Height_Weight;
float _Height_Gradience;
float _Sway_Wind_Time_Random_Offset_Amount;
float _Sway_Wind_Intensity;
float3 _Sway_Wind_Direction;
float _Color_Map_Tiling;
float _Subsurface_Intensity;
float _Alpha_Clip;
float _Wiggle_Wind_Size;
float _Wiggle_Wind_Speed_Small;
float _Fresnel_Pow;
float _Always_On_Fresnel_Pow;
float4 _Always_On_Fresnel_Color;
float _ree;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D);
SAMPLER(sampler_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D);
TEXTURE2D(_Color_Map);
SAMPLER(sampler_Color_Map);

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

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Saturate_float4(float4 In, out float4 Out)
{
    Out = saturate(In);
}

void Unity_Normalize_float3(float3 In, out float3 Out)
{
    Out = normalize(In);
}

void Unity_DotProduct_float3(float3 A, float3 B, out float Out)
{
    Out = dot(A, B);
}

void Unity_Absolute_float(float In, out float Out)
{
    Out = abs(In);
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
}

// unity-custom-func-begin
void MainLight_float(out float3 Direction){

#ifdef UNITY_SHADER_GRAPH
	Direction = _WorldSpaceLightPos0.xyz;
#else
	Direction = float3(1,1,1);
#endif
}
// unity-custom-func-end

void Unity_DotProduct_float2(float2 A, float2 B, out float Out)
{
    Out = dot(A, B);
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
float _Property_7689f3a0523b4af9912879a05d1d5206_Out_0_Float = _Sway_Wind_Speed;
float3 _Property_e0531013c46845b6b036ac453118c744_Out_0_Vector3 = _Sway_Wind_Direction;
float _Property_35588e6a16b5457695dba39b1644d669_Out_0_Float = _Sway_Wind_Intensity;
float _Property_c8d3d7e6e6f64d409f010d988b96e327_Out_0_Float = _Sway_Wind_Time_Random_Offset_Amount;
Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float _WindSway_662bcd4f8b2b4607b138cb44de00a708;
_WindSway_662bcd4f8b2b4607b138cb44de00a708.ObjectSpacePosition = IN.ObjectSpacePosition;
_WindSway_662bcd4f8b2b4607b138cb44de00a708.TimeParameters = IN.TimeParameters;
float3 _WindSway_662bcd4f8b2b4607b138cb44de00a708_OutVector3_1_Vector3;
SG_WindSway_51c4027ae50a8cd49be53c4990b9f179_float(_Property_7689f3a0523b4af9912879a05d1d5206_Out_0_Float, _Property_e0531013c46845b6b036ac453118c744_Out_0_Vector3, _Property_35588e6a16b5457695dba39b1644d669_Out_0_Float, _Property_c8d3d7e6e6f64d409f010d988b96e327_Out_0_Float, _WindSway_662bcd4f8b2b4607b138cb44de00a708, _WindSway_662bcd4f8b2b4607b138cb44de00a708_OutVector3_1_Vector3);
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_R_1_Float = IN.WorldSpacePosition[0];
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_G_2_Float = IN.WorldSpacePosition[1];
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_B_3_Float = IN.WorldSpacePosition[2];
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_A_4_Float = 0;
float3 _Vector3_e154b524054546d2bbeab293c0e20519_Out_0_Vector3 = float3(float(0), float(0), float(0));
float3 _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3;
_Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3 = TransformObjectToWorld(_Vector3_e154b524054546d2bbeab293c0e20519_Out_0_Vector3.xyz);
float _Split_5eb48153bba44ea2b128583819f09a11_R_1_Float = _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3[0];
float _Split_5eb48153bba44ea2b128583819f09a11_G_2_Float = _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3[1];
float _Split_5eb48153bba44ea2b128583819f09a11_B_3_Float = _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3[2];
float _Split_5eb48153bba44ea2b128583819f09a11_A_4_Float = 0;
float _Subtract_d6a99abdf4e74644acffc1718d1735bf_Out_2_Float;
Unity_Subtract_float(_Split_8fe962eb6f0f4d6fba734f9edcaf9067_G_2_Float, _Split_5eb48153bba44ea2b128583819f09a11_G_2_Float, _Subtract_d6a99abdf4e74644acffc1718d1735bf_Out_2_Float);
float _Property_1cf3cda52e094e638df491b003607dfd_Out_0_Float = _Height_Weight;
float _Split_145cf2161d4c4510b08248cad819f655_R_1_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[0];
float _Split_145cf2161d4c4510b08248cad819f655_G_2_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[1];
float _Split_145cf2161d4c4510b08248cad819f655_B_3_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[2];
float _Split_145cf2161d4c4510b08248cad819f655_A_4_Float = 0;
float _Multiply_2c140d573b9447aab058bd863fe2f4f7_Out_2_Float;
Unity_Multiply_float_float(_Property_1cf3cda52e094e638df491b003607dfd_Out_0_Float, _Split_145cf2161d4c4510b08248cad819f655_G_2_Float, _Multiply_2c140d573b9447aab058bd863fe2f4f7_Out_2_Float);
float _Divide_0f6a637143d841d0b1c94fa2ae3e164d_Out_2_Float;
Unity_Divide_float(_Subtract_d6a99abdf4e74644acffc1718d1735bf_Out_2_Float, _Multiply_2c140d573b9447aab058bd863fe2f4f7_Out_2_Float, _Divide_0f6a637143d841d0b1c94fa2ae3e164d_Out_2_Float);
float _Saturate_1e4e29fb5c074565a1f941c046957b05_Out_1_Float;
Unity_Saturate_float(_Divide_0f6a637143d841d0b1c94fa2ae3e164d_Out_2_Float, _Saturate_1e4e29fb5c074565a1f941c046957b05_Out_1_Float);
float _Property_3d64a0c8dd104507b9a0762a778fe585_Out_0_Float = _Height_Gradience;
float _Power_f1d74728398a45f9993fe3b3affb6584_Out_2_Float;
Unity_Power_float(_Saturate_1e4e29fb5c074565a1f941c046957b05_Out_1_Float, _Property_3d64a0c8dd104507b9a0762a778fe585_Out_0_Float, _Power_f1d74728398a45f9993fe3b3affb6584_Out_2_Float);
float _Saturate_57b130350452418fa1d09d4a0ddd64ac_Out_1_Float;
Unity_Saturate_float(_Power_f1d74728398a45f9993fe3b3affb6584_Out_2_Float, _Saturate_57b130350452418fa1d09d4a0ddd64ac_Out_1_Float);
float3 _Lerp_1fbf8d0a7f5a4e2ab381967ac90385c7_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _WindSway_662bcd4f8b2b4607b138cb44de00a708_OutVector3_1_Vector3, (_Saturate_57b130350452418fa1d09d4a0ddd64ac_Out_1_Float.xxx), _Lerp_1fbf8d0a7f5a4e2ab381967ac90385c7_Out_3_Vector3);
float _Split_bd04916788224fa2bb71be3f27eb839a_R_1_Float = IN.WorldSpacePosition[0];
float _Split_bd04916788224fa2bb71be3f27eb839a_G_2_Float = IN.WorldSpacePosition[1];
float _Split_bd04916788224fa2bb71be3f27eb839a_B_3_Float = IN.WorldSpacePosition[2];
float _Split_bd04916788224fa2bb71be3f27eb839a_A_4_Float = 0;
float4 _Combine_45cdab9c20414d03862100967f56c78d_RGBA_4_Vector4;
float3 _Combine_45cdab9c20414d03862100967f56c78d_RGB_5_Vector3;
float2 _Combine_45cdab9c20414d03862100967f56c78d_RG_6_Vector2;
Unity_Combine_float(_Split_bd04916788224fa2bb71be3f27eb839a_R_1_Float, _Split_bd04916788224fa2bb71be3f27eb839a_B_3_Float, float(0), float(0), _Combine_45cdab9c20414d03862100967f56c78d_RGBA_4_Vector4, _Combine_45cdab9c20414d03862100967f56c78d_RGB_5_Vector3, _Combine_45cdab9c20414d03862100967f56c78d_RG_6_Vector2);
float _Property_9b35f6244c0247c7a9be7af2671f58e7_Out_0_Float = _Wiggle_Wind_Size;
float2 _Multiply_ba4d7f376f904143acc9eb33d8414ff8_Out_2_Vector2;
Unity_Multiply_float2_float2(_Combine_45cdab9c20414d03862100967f56c78d_RG_6_Vector2, (_Property_9b35f6244c0247c7a9be7af2671f58e7_Out_0_Float.xx), _Multiply_ba4d7f376f904143acc9eb33d8414ff8_Out_2_Vector2);
float _Property_6eae25110c184e7a94293b95ddc9ecef_Out_0_Float = _Wiggle_Wind_Speed_Small;
float _Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Property_6eae25110c184e7a94293b95ddc9ecef_Out_0_Float, _Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float);
float _Multiply_7510d5a95e6c4d3986bb83059d23d2fa_Out_2_Float;
Unity_Multiply_float_float(_Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float, 0.8, _Multiply_7510d5a95e6c4d3986bb83059d23d2fa_Out_2_Float);
float2 _Vector2_cb26bc8c681b455c81073f0dc3c8f6b4_Out_0_Vector2 = float2(_Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float, _Multiply_7510d5a95e6c4d3986bb83059d23d2fa_Out_2_Float);
float2 _TilingAndOffset_e5f47c2cf9d846859a72aadcdac7fc3b_Out_3_Vector2;
Unity_TilingAndOffset_float(_Multiply_ba4d7f376f904143acc9eb33d8414ff8_Out_2_Vector2, float2 (1, 1), _Vector2_cb26bc8c681b455c81073f0dc3c8f6b4_Out_0_Vector2, _TilingAndOffset_e5f47c2cf9d846859a72aadcdac7fc3b_Out_3_Vector2);
  float4 _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).tex, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).GetTransformedUV(_TilingAndOffset_e5f47c2cf9d846859a72aadcdac7fc3b_Out_3_Vector2), float(2));
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_R_5_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_G_6_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_B_7_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_A_8_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.a;
float _Float_3f1a7b5619414946b13e7cef19336d45_Out_0_Float = float(0.3);
float _Subtract_3105ef040f344481afb342cd9ef65e9f_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_R_5_Float, _Float_3f1a7b5619414946b13e7cef19336d45_Out_0_Float, _Subtract_3105ef040f344481afb342cd9ef65e9f_Out_2_Float);
float _Split_18495110217342bc80044d9345d98fbd_R_1_Float = IN.WorldSpacePosition[0];
float _Split_18495110217342bc80044d9345d98fbd_G_2_Float = IN.WorldSpacePosition[1];
float _Split_18495110217342bc80044d9345d98fbd_B_3_Float = IN.WorldSpacePosition[2];
float _Split_18495110217342bc80044d9345d98fbd_A_4_Float = 0;
float4 _Combine_d1ee3e49696840daa7cfd43df2287807_RGBA_4_Vector4;
float3 _Combine_d1ee3e49696840daa7cfd43df2287807_RGB_5_Vector3;
float2 _Combine_d1ee3e49696840daa7cfd43df2287807_RG_6_Vector2;
Unity_Combine_float(_Split_18495110217342bc80044d9345d98fbd_R_1_Float, _Split_18495110217342bc80044d9345d98fbd_G_2_Float, float(0), float(0), _Combine_d1ee3e49696840daa7cfd43df2287807_RGBA_4_Vector4, _Combine_d1ee3e49696840daa7cfd43df2287807_RGB_5_Vector3, _Combine_d1ee3e49696840daa7cfd43df2287807_RG_6_Vector2);
float _Property_9efe7c2851414ea39b6971daac6143e5_Out_0_Float = _Wiggle_Wind_Size;
float _Multiply_4f6b361114004341aeae12987f071375_Out_2_Float;
Unity_Multiply_float_float(_Property_9efe7c2851414ea39b6971daac6143e5_Out_0_Float, 0.763, _Multiply_4f6b361114004341aeae12987f071375_Out_2_Float);
float2 _Multiply_0d88e33790f0458b9e6e7f49946e6344_Out_2_Vector2;
Unity_Multiply_float2_float2(_Combine_d1ee3e49696840daa7cfd43df2287807_RG_6_Vector2, (_Multiply_4f6b361114004341aeae12987f071375_Out_2_Float.xx), _Multiply_0d88e33790f0458b9e6e7f49946e6344_Out_2_Vector2);
float _Property_56c994a79361438d88ea30b1e3ccf2aa_Out_0_Float = _Wiggle_Wind_Speed_Large;
float _Multiply_c5e5feed518642678424c6f0d45b4377_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Property_56c994a79361438d88ea30b1e3ccf2aa_Out_0_Float, _Multiply_c5e5feed518642678424c6f0d45b4377_Out_2_Float);
float2 _TilingAndOffset_7dac505987c144edad083d13ebca67e6_Out_3_Vector2;
Unity_TilingAndOffset_float(_Multiply_0d88e33790f0458b9e6e7f49946e6344_Out_2_Vector2, float2 (1, 1), (_Multiply_c5e5feed518642678424c6f0d45b4377_Out_2_Float.xx), _TilingAndOffset_7dac505987c144edad083d13ebca67e6_Out_3_Vector2);
  float4 _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).tex, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).GetTransformedUV(_TilingAndOffset_7dac505987c144edad083d13ebca67e6_Out_3_Vector2), float(2));
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_R_5_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_G_6_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_B_7_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_A_8_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.a;
float _Subtract_e29fd5b0daea41a1a38ac52280e6db88_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_G_6_Float, _Float_3f1a7b5619414946b13e7cef19336d45_Out_0_Float, _Subtract_e29fd5b0daea41a1a38ac52280e6db88_Out_2_Float);
float _Add_6d51be6104ee4e23a093dfb9c3d993f7_Out_2_Float;
Unity_Add_float(_Subtract_3105ef040f344481afb342cd9ef65e9f_Out_2_Float, _Subtract_e29fd5b0daea41a1a38ac52280e6db88_Out_2_Float, _Add_6d51be6104ee4e23a093dfb9c3d993f7_Out_2_Float);
float4 _Multiply_db043a8b3fc34edb8bee433085c7e061_Out_2_Vector4;
Unity_Multiply_float4_float4((_Add_6d51be6104ee4e23a093dfb9c3d993f7_Out_2_Float.xxxx), IN.VertexColor, _Multiply_db043a8b3fc34edb8bee433085c7e061_Out_2_Vector4);
float _Property_46c8c648052246a4a32e80905ed7e9df_Out_0_Float = _Wiggle_Wind_Intensity;
float4 _Multiply_4baa80310c414483a2aada72baa7a883_Out_2_Vector4;
Unity_Multiply_float4_float4(_Multiply_db043a8b3fc34edb8bee433085c7e061_Out_2_Vector4, (_Property_46c8c648052246a4a32e80905ed7e9df_Out_0_Float.xxxx), _Multiply_4baa80310c414483a2aada72baa7a883_Out_2_Vector4);
float3 _Multiply_2c95a93020c44a20be82a31b985c35b8_Out_2_Vector3;
Unity_Multiply_float3_float3((_Multiply_4baa80310c414483a2aada72baa7a883_Out_2_Vector4.xyz), IN.ObjectSpaceNormal, _Multiply_2c95a93020c44a20be82a31b985c35b8_Out_2_Vector3);
float3 _Add_5a143eeddea74bceb3ddd5b059f49da5_Out_2_Vector3;
Unity_Add_float3(_Lerp_1fbf8d0a7f5a4e2ab381967ac90385c7_Out_3_Vector3, _Multiply_2c95a93020c44a20be82a31b985c35b8_Out_2_Vector3, _Add_5a143eeddea74bceb3ddd5b059f49da5_Out_2_Vector3);
#if defined(_USE_WPO)
float3 _UseWPO_1f28bdd134c9432e812aded965021562_Out_0_Vector3 = _Add_5a143eeddea74bceb3ddd5b059f49da5_Out_2_Vector3;
#else
float3 _UseWPO_1f28bdd134c9432e812aded965021562_Out_0_Vector3 = IN.ObjectSpacePosition;
#endif
float3 _UPGFoliageContactPositionCustomFunction_3446fb1a0c894696bd903b7eb184e241_bentPositionOS_1_Vector3;
UPG_FoliageContactPosition_float(_UseWPO_1f28bdd134c9432e812aded965021562_Out_0_Vector3, _UPGFoliageContactPositionCustomFunction_3446fb1a0c894696bd903b7eb184e241_bentPositionOS_1_Vector3);
description.Position = _UPGFoliageContactPositionCustomFunction_3446fb1a0c894696bd903b7eb184e241_bentPositionOS_1_Vector3;
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
float4 _Property_a9c1982af1c74efb9ac25b0326ae7979_Out_0_Vector4 = _Foliage_Color_Bottom;
float4 _Property_50eb1cb960084765a0771df7f40a6d5d_Out_0_Vector4 = _Foliage_Color_Top;
float _Split_1540f44067c24640b4968355fa26a295_R_1_Float = IN.WorldSpacePosition[0];
float _Split_1540f44067c24640b4968355fa26a295_G_2_Float = IN.WorldSpacePosition[1];
float _Split_1540f44067c24640b4968355fa26a295_B_3_Float = IN.WorldSpacePosition[2];
float _Split_1540f44067c24640b4968355fa26a295_A_4_Float = 0;
float3 _Vector3_54c605479a824dd3a62abb1919f39c57_Out_0_Vector3 = float3(float(0), float(0), float(0));
float3 _Transform_f3f68ad0eeb04ce9ac459f2859353c06_Out_1_Vector3;
_Transform_f3f68ad0eeb04ce9ac459f2859353c06_Out_1_Vector3 = TransformObjectToWorld(_Vector3_54c605479a824dd3a62abb1919f39c57_Out_0_Vector3.xyz);
float _Split_15d5002ae66545318962a44958be8e00_R_1_Float = _Transform_f3f68ad0eeb04ce9ac459f2859353c06_Out_1_Vector3[0];
float _Split_15d5002ae66545318962a44958be8e00_G_2_Float = _Transform_f3f68ad0eeb04ce9ac459f2859353c06_Out_1_Vector3[1];
float _Split_15d5002ae66545318962a44958be8e00_B_3_Float = _Transform_f3f68ad0eeb04ce9ac459f2859353c06_Out_1_Vector3[2];
float _Split_15d5002ae66545318962a44958be8e00_A_4_Float = 0;
float _Subtract_3131eba61d024496834f2933dbde6855_Out_2_Float;
Unity_Subtract_float(_Split_1540f44067c24640b4968355fa26a295_G_2_Float, _Split_15d5002ae66545318962a44958be8e00_G_2_Float, _Subtract_3131eba61d024496834f2933dbde6855_Out_2_Float);
float _Property_605b997dac5a4228881635945f995c72_Out_0_Float = _Tree_Height;
float _Divide_8e4152d2ed4943778a44dc39efa65611_Out_2_Float;
Unity_Divide_float(_Subtract_3131eba61d024496834f2933dbde6855_Out_2_Float, _Property_605b997dac5a4228881635945f995c72_Out_0_Float, _Divide_8e4152d2ed4943778a44dc39efa65611_Out_2_Float);
float _Saturate_086c13233c3d43fab6a33a1e2ed498c2_Out_1_Float;
Unity_Saturate_float(_Divide_8e4152d2ed4943778a44dc39efa65611_Out_2_Float, _Saturate_086c13233c3d43fab6a33a1e2ed498c2_Out_1_Float);
float _Property_0e5bff248b2341909121bb16666f3468_Out_0_Float = _Gradience_Power;
float _Power_be0c59cc158d492f99ea24f3b1001e1f_Out_2_Float;
Unity_Power_float(_Saturate_086c13233c3d43fab6a33a1e2ed498c2_Out_1_Float, _Property_0e5bff248b2341909121bb16666f3468_Out_0_Float, _Power_be0c59cc158d492f99ea24f3b1001e1f_Out_2_Float);
float _Saturate_26bbc2447b5a402c9562a9e5d012fd6c_Out_1_Float;
Unity_Saturate_float(_Power_be0c59cc158d492f99ea24f3b1001e1f_Out_2_Float, _Saturate_26bbc2447b5a402c9562a9e5d012fd6c_Out_1_Float);
float4 _Lerp_795c581446834fe0bee35971bc8f0c5c_Out_3_Vector4;
Unity_Lerp_float4(_Property_a9c1982af1c74efb9ac25b0326ae7979_Out_0_Vector4, _Property_50eb1cb960084765a0771df7f40a6d5d_Out_0_Vector4, (_Saturate_26bbc2447b5a402c9562a9e5d012fd6c_Out_1_Float.xxxx), _Lerp_795c581446834fe0bee35971bc8f0c5c_Out_3_Vector4);
float4 _Property_4036fdaeaa4647adb3dd84d70ff1161e_Out_0_Vector4 = _Foliage_Color_Top;
#if defined(_USE_HEIGHT_GRADIENCE)
float4 _UseHeightGradience_d4326b9500c14f11a9090b7e8f107721_Out_0_Vector4 = _Lerp_795c581446834fe0bee35971bc8f0c5c_Out_3_Vector4;
#else
float4 _UseHeightGradience_d4326b9500c14f11a9090b7e8f107721_Out_0_Vector4 = _Property_4036fdaeaa4647adb3dd84d70ff1161e_Out_0_Vector4;
#endif
UnityTexture2D _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Color_Map, sampler_Color_Map, _Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_eaea7ba5e1e74519a1bc7833f0103ab8_Out_0_Vector4 = IN.uv0;
float _Property_95352575ae4140008287fd58138e8eb5_Out_0_Float = _Color_Map_Tiling;
float4 _Multiply_86fafeae394745fe880655d03cafd9e1_Out_2_Vector4;
Unity_Multiply_float4_float4(_UV_eaea7ba5e1e74519a1bc7833f0103ab8_Out_0_Vector4, (_Property_95352575ae4140008287fd58138e8eb5_Out_0_Float.xxxx), _Multiply_86fafeae394745fe880655d03cafd9e1_Out_2_Vector4);
float4 _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.tex, _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.samplerstate, _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.GetTransformedUV((_Multiply_86fafeae394745fe880655d03cafd9e1_Out_2_Vector4.xy)) );
if (_Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4, _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_R_4_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.r;
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_G_5_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.g;
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_B_6_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.b;
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_A_7_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.a;
float4 _Multiply_a05088f8e3434d54920d012fc47555a7_Out_2_Vector4;
Unity_Multiply_float4_float4(_UseHeightGradience_d4326b9500c14f11a9090b7e8f107721_Out_0_Vector4, _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4, _Multiply_a05088f8e3434d54920d012fc47555a7_Out_2_Vector4);
float4 _Property_ee5875e8bc214caba4dbea1511d06cd2_Out_0_Vector4 = _Always_On_Fresnel_Color;
float _Property_be2ed593ceb345f89b57d123af3db203_Out_0_Float = _Always_On_Fresnel_Intensity;
float4 _Multiply_dcf6e90c046c4f72b77b81df5b922468_Out_2_Vector4;
Unity_Multiply_float4_float4(_Property_ee5875e8bc214caba4dbea1511d06cd2_Out_0_Vector4, (_Property_be2ed593ceb345f89b57d123af3db203_Out_0_Float.xxxx), _Multiply_dcf6e90c046c4f72b77b81df5b922468_Out_2_Vector4);
float4 _Saturate_26f2848ceb0a494094065a9387275032_Out_1_Vector4;
Unity_Saturate_float4(_Multiply_dcf6e90c046c4f72b77b81df5b922468_Out_2_Vector4, _Saturate_26f2848ceb0a494094065a9387275032_Out_1_Vector4);
float3 _Normalize_764cced5d0894f3d89f5911d50d5f4a2_Out_1_Vector3;
Unity_Normalize_float3(IN.WorldSpaceViewDirection, _Normalize_764cced5d0894f3d89f5911d50d5f4a2_Out_1_Vector3);
float _DotProduct_eb716c3ca301415688a52e76b310a65c_Out_2_Float;
Unity_DotProduct_float3(IN.WorldSpaceNormal, _Normalize_764cced5d0894f3d89f5911d50d5f4a2_Out_1_Vector3, _DotProduct_eb716c3ca301415688a52e76b310a65c_Out_2_Float);
float _Absolute_470cf4f0298a44a695f47aa5ea61c0ab_Out_1_Float;
Unity_Absolute_float(_DotProduct_eb716c3ca301415688a52e76b310a65c_Out_2_Float, _Absolute_470cf4f0298a44a695f47aa5ea61c0ab_Out_1_Float);
float _OneMinus_fa832b719fc24001be421507d395a592_Out_1_Float;
Unity_OneMinus_float(_Absolute_470cf4f0298a44a695f47aa5ea61c0ab_Out_1_Float, _OneMinus_fa832b719fc24001be421507d395a592_Out_1_Float);
float _Property_d9cfcacb90204bdfa36d0c1b7c0da452_Out_0_Float = _Always_On_Fresnel_Pow;
float _Power_7841b1ae85554473b96cce132c7ff7e1_Out_2_Float;
Unity_Power_float(_OneMinus_fa832b719fc24001be421507d395a592_Out_1_Float, _Property_d9cfcacb90204bdfa36d0c1b7c0da452_Out_0_Float, _Power_7841b1ae85554473b96cce132c7ff7e1_Out_2_Float);
float _Saturate_908f6307f948471ca95c0e7507cd7e43_Out_1_Float;
Unity_Saturate_float(_Power_7841b1ae85554473b96cce132c7ff7e1_Out_2_Float, _Saturate_908f6307f948471ca95c0e7507cd7e43_Out_1_Float);
float4 _Lerp_3406aa7c52b14c818ac9eb76bf068e2d_Out_3_Vector4;
Unity_Lerp_float4(_Multiply_a05088f8e3434d54920d012fc47555a7_Out_2_Vector4, _Saturate_26f2848ceb0a494094065a9387275032_Out_1_Vector4, (_Saturate_908f6307f948471ca95c0e7507cd7e43_Out_1_Float.xxxx), _Lerp_3406aa7c52b14c818ac9eb76bf068e2d_Out_3_Vector4);
float _Property_e8b3335851f84d91b28234460148cd0f_Out_0_Float = _Subsurface_Intensity;
float4 _Multiply_94df75e4ad734308a11d784250fc0b0c_Out_2_Vector4;
Unity_Multiply_float4_float4(_Lerp_3406aa7c52b14c818ac9eb76bf068e2d_Out_3_Vector4, (_Property_e8b3335851f84d91b28234460148cd0f_Out_0_Float.xxxx), _Multiply_94df75e4ad734308a11d784250fc0b0c_Out_2_Vector4);
float4 _Saturate_f0090edc5c664784a336737069276109_Out_1_Vector4;
Unity_Saturate_float4(_Multiply_94df75e4ad734308a11d784250fc0b0c_Out_2_Vector4, _Saturate_f0090edc5c664784a336737069276109_Out_1_Vector4);
float3 _MainLightCustomFunction_461af74b29b6456da89a2348455c2483_Direction_0_Vector3;
MainLight_float(_MainLightCustomFunction_461af74b29b6456da89a2348455c2483_Direction_0_Vector3);
float3 _Normalize_8644c278d597434b8871eb11a7a52cfe_Out_1_Vector3;
Unity_Normalize_float3(_MainLightCustomFunction_461af74b29b6456da89a2348455c2483_Direction_0_Vector3, _Normalize_8644c278d597434b8871eb11a7a52cfe_Out_1_Vector3);
float _Split_8a104625401f4a048b1160e17800fd11_R_1_Float = _Normalize_8644c278d597434b8871eb11a7a52cfe_Out_1_Vector3[0];
float _Split_8a104625401f4a048b1160e17800fd11_G_2_Float = _Normalize_8644c278d597434b8871eb11a7a52cfe_Out_1_Vector3[1];
float _Split_8a104625401f4a048b1160e17800fd11_B_3_Float = _Normalize_8644c278d597434b8871eb11a7a52cfe_Out_1_Vector3[2];
float _Split_8a104625401f4a048b1160e17800fd11_A_4_Float = 0;
float4 _Combine_87a90606ca4545b2983770732c0ed1b7_RGBA_4_Vector4;
float3 _Combine_87a90606ca4545b2983770732c0ed1b7_RGB_5_Vector3;
float2 _Combine_87a90606ca4545b2983770732c0ed1b7_RG_6_Vector2;
Unity_Combine_float(_Split_8a104625401f4a048b1160e17800fd11_R_1_Float, _Split_8a104625401f4a048b1160e17800fd11_B_3_Float, float(0), float(0), _Combine_87a90606ca4545b2983770732c0ed1b7_RGBA_4_Vector4, _Combine_87a90606ca4545b2983770732c0ed1b7_RGB_5_Vector3, _Combine_87a90606ca4545b2983770732c0ed1b7_RG_6_Vector2);
float3 _Normalize_f1ad7ea0af7b45ee88df143397ba6727_Out_1_Vector3;
Unity_Normalize_float3(-UNITY_MATRIX_V[2].xyz, _Normalize_f1ad7ea0af7b45ee88df143397ba6727_Out_1_Vector3);
float3 _Multiply_5b2d23749b2347b7af4472236c1dfe22_Out_2_Vector3;
Unity_Multiply_float3_float3(_Normalize_f1ad7ea0af7b45ee88df143397ba6727_Out_1_Vector3, float3(-1, -1, -1), _Multiply_5b2d23749b2347b7af4472236c1dfe22_Out_2_Vector3);
float _Split_6ebebe9410ea41bd8f8a327255848dc9_R_1_Float = _Multiply_5b2d23749b2347b7af4472236c1dfe22_Out_2_Vector3[0];
float _Split_6ebebe9410ea41bd8f8a327255848dc9_G_2_Float = _Multiply_5b2d23749b2347b7af4472236c1dfe22_Out_2_Vector3[1];
float _Split_6ebebe9410ea41bd8f8a327255848dc9_B_3_Float = _Multiply_5b2d23749b2347b7af4472236c1dfe22_Out_2_Vector3[2];
float _Split_6ebebe9410ea41bd8f8a327255848dc9_A_4_Float = 0;
float4 _Combine_ce9284087dd446409babf9c154f18fe4_RGBA_4_Vector4;
float3 _Combine_ce9284087dd446409babf9c154f18fe4_RGB_5_Vector3;
float2 _Combine_ce9284087dd446409babf9c154f18fe4_RG_6_Vector2;
Unity_Combine_float(_Split_6ebebe9410ea41bd8f8a327255848dc9_R_1_Float, _Split_6ebebe9410ea41bd8f8a327255848dc9_B_3_Float, float(0), float(0), _Combine_ce9284087dd446409babf9c154f18fe4_RGBA_4_Vector4, _Combine_ce9284087dd446409babf9c154f18fe4_RGB_5_Vector3, _Combine_ce9284087dd446409babf9c154f18fe4_RG_6_Vector2);
float _DotProduct_9e4f6525393a4171b6519e347a4b7a37_Out_2_Float;
Unity_DotProduct_float2(_Combine_87a90606ca4545b2983770732c0ed1b7_RG_6_Vector2, _Combine_ce9284087dd446409babf9c154f18fe4_RG_6_Vector2, _DotProduct_9e4f6525393a4171b6519e347a4b7a37_Out_2_Float);
float _Saturate_f76f8f4113e0468b9b20a6abf253d76e_Out_1_Float;
Unity_Saturate_float(_DotProduct_9e4f6525393a4171b6519e347a4b7a37_Out_2_Float, _Saturate_f76f8f4113e0468b9b20a6abf253d76e_Out_1_Float);
float _Property_bd14cfb26b074dbabde1a5a9b7f95d97_Out_0_Float = _Subsurface_Transition_Smoothness;
float _Power_2bc1d129029a4da69b2993ea07ff412f_Out_2_Float;
Unity_Power_float(_Saturate_f76f8f4113e0468b9b20a6abf253d76e_Out_1_Float, _Property_bd14cfb26b074dbabde1a5a9b7f95d97_Out_0_Float, _Power_2bc1d129029a4da69b2993ea07ff412f_Out_2_Float);
float _Saturate_44588f923e1b48ff97cd55ea2d69aa2b_Out_1_Float;
Unity_Saturate_float(_Power_2bc1d129029a4da69b2993ea07ff412f_Out_2_Float, _Saturate_44588f923e1b48ff97cd55ea2d69aa2b_Out_1_Float);
float4 _Lerp_98f83471835040419ec38e468d855fe3_Out_3_Vector4;
Unity_Lerp_float4(_Lerp_3406aa7c52b14c818ac9eb76bf068e2d_Out_3_Vector4, _Saturate_f0090edc5c664784a336737069276109_Out_1_Vector4, (_Saturate_44588f923e1b48ff97cd55ea2d69aa2b_Out_1_Float.xxxx), _Lerp_98f83471835040419ec38e468d855fe3_Out_3_Vector4);
float _Property_e00e00901e414a40a25484eb2c276584_Out_0_Float = _Subsurface_Fresnel_Intensity;
float4 _Multiply_6ed60680651f4e8cbff9e6f2ed97f0b9_Out_2_Vector4;
Unity_Multiply_float4_float4(_Lerp_98f83471835040419ec38e468d855fe3_Out_3_Vector4, (_Property_e00e00901e414a40a25484eb2c276584_Out_0_Float.xxxx), _Multiply_6ed60680651f4e8cbff9e6f2ed97f0b9_Out_2_Vector4);
float4 _Saturate_5cea81f1b9b5405fb4a571a97e57df52_Out_1_Vector4;
Unity_Saturate_float4(_Multiply_6ed60680651f4e8cbff9e6f2ed97f0b9_Out_2_Vector4, _Saturate_5cea81f1b9b5405fb4a571a97e57df52_Out_1_Vector4);
float _Saturate_10ad388197014e2fa2d0bd8261d82207_Out_1_Float;
Unity_Saturate_float(_DotProduct_9e4f6525393a4171b6519e347a4b7a37_Out_2_Float, _Saturate_10ad388197014e2fa2d0bd8261d82207_Out_1_Float);
float _Property_fe3bfd62823b45bf868d2c88982c1f64_Out_0_Float = _Subsurface_Fresnel_Transition_Smoothness;
float _Power_1ad124a5d5b74c13848df4f8f171d448_Out_2_Float;
Unity_Power_float(_Saturate_10ad388197014e2fa2d0bd8261d82207_Out_1_Float, _Property_fe3bfd62823b45bf868d2c88982c1f64_Out_0_Float, _Power_1ad124a5d5b74c13848df4f8f171d448_Out_2_Float);
float3 _Normalize_7e92e50b98274c508fa061a32d7a99b7_Out_1_Vector3;
Unity_Normalize_float3(IN.WorldSpaceViewDirection, _Normalize_7e92e50b98274c508fa061a32d7a99b7_Out_1_Vector3);
float _DotProduct_3170aef4b99740d083af85a5aa294edd_Out_2_Float;
Unity_DotProduct_float3(IN.WorldSpaceNormal, _Normalize_7e92e50b98274c508fa061a32d7a99b7_Out_1_Vector3, _DotProduct_3170aef4b99740d083af85a5aa294edd_Out_2_Float);
float _Absolute_111753c15e794f4b8e885c2c19d93d82_Out_1_Float;
Unity_Absolute_float(_DotProduct_3170aef4b99740d083af85a5aa294edd_Out_2_Float, _Absolute_111753c15e794f4b8e885c2c19d93d82_Out_1_Float);
float _OneMinus_2a02f0d378e1454c9aeda0ff447d4d86_Out_1_Float;
Unity_OneMinus_float(_Absolute_111753c15e794f4b8e885c2c19d93d82_Out_1_Float, _OneMinus_2a02f0d378e1454c9aeda0ff447d4d86_Out_1_Float);
float _Property_49c0203b77734a36b9ba06f7bcd723ee_Out_0_Float = _Subsurface_Fresnel_Pow;
float _Power_71121b48d5934b11a14aeadcad656816_Out_2_Float;
Unity_Power_float(_OneMinus_2a02f0d378e1454c9aeda0ff447d4d86_Out_1_Float, _Property_49c0203b77734a36b9ba06f7bcd723ee_Out_0_Float, _Power_71121b48d5934b11a14aeadcad656816_Out_2_Float);
float _Saturate_29e69580558f40c49a883332cdf25357_Out_1_Float;
Unity_Saturate_float(_Power_71121b48d5934b11a14aeadcad656816_Out_2_Float, _Saturate_29e69580558f40c49a883332cdf25357_Out_1_Float);
float _Multiply_328c604c9492493ba1dc1999ef6314fd_Out_2_Float;
Unity_Multiply_float_float(_Power_1ad124a5d5b74c13848df4f8f171d448_Out_2_Float, _Saturate_29e69580558f40c49a883332cdf25357_Out_1_Float, _Multiply_328c604c9492493ba1dc1999ef6314fd_Out_2_Float);
float4 _Lerp_10a8be366b974da7b00eab9c55f88a8a_Out_3_Vector4;
Unity_Lerp_float4(_Lerp_98f83471835040419ec38e468d855fe3_Out_3_Vector4, _Saturate_5cea81f1b9b5405fb4a571a97e57df52_Out_1_Vector4, (_Multiply_328c604c9492493ba1dc1999ef6314fd_Out_2_Float.xxxx), _Lerp_10a8be366b974da7b00eab9c55f88a8a_Out_3_Vector4);
float _Multiply_cf1ea983d11e4ab7b4bdbbb064ad94b5_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_A_7_Float, 1, _Multiply_cf1ea983d11e4ab7b4bdbbb064ad94b5_Out_2_Float);
float _Property_99bb0b22edbb4ea0b1b7844414b876a7_Out_0_Float = _Alpha_Clip;
surface.BaseColor = (_Lerp_10a8be366b974da7b00eab9c55f88a8a_Out_3_Vector4.xyz);
surface.Alpha = _Multiply_cf1ea983d11e4ab7b4bdbbb064ad94b5_Out_2_Float;
surface.AlphaClipThreshold = _Property_99bb0b22edbb4ea0b1b7844414b876a7_Out_0_Float;
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


    output.WorldSpaceViewDirection = GetWorldSpaceNormalizeViewDir(input.positionWS);
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
#pragma shader_feature_local _ _USE_WPO
#pragma shader_feature_local _ _USE_HEIGHT_GRADIENCE

#if defined(_USE_WPO) && defined(_USE_HEIGHT_GRADIENCE)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_WPO)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_HEIGHT_GRADIENCE)
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
#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_NORMAL_WS
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
 float3 WorldSpaceViewDirection;
 float3 WorldSpacePosition;
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 WorldSpacePosition;
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

float4 _Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D_TexelSize;
float _Subsurface_Fresnel_Transition_Smoothness;
float _Subsurface_Fresnel_Pow;
float _Subsurface_Fresnel_Intensity;
float _Subsurface_Transition_Smoothness;
float _Wiggle_Wind_Speed_Large;
float _Wiggle_Wind_Intensity;
float _Roughness;
float4 _Foliage_Color_Bottom;
float _Sway_Wind_Speed;
float _Gradience_Power;
float _Fresnel_Mul;
float4 _Foliage_Color_Top;
float _Always_On_Fresnel_Intensity;
float _Tree_Height;
float4 _Color_Map_TexelSize;
float _Height_Weight;
float _Height_Gradience;
float _Sway_Wind_Time_Random_Offset_Amount;
float _Sway_Wind_Intensity;
float3 _Sway_Wind_Direction;
float _Color_Map_Tiling;
float _Subsurface_Intensity;
float _Alpha_Clip;
float _Wiggle_Wind_Size;
float _Wiggle_Wind_Speed_Small;
float _Fresnel_Pow;
float _Always_On_Fresnel_Pow;
float4 _Always_On_Fresnel_Color;
float _ree;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D);
SAMPLER(sampler_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D);
TEXTURE2D(_Color_Map);
SAMPLER(sampler_Color_Map);

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

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Saturate_float4(float4 In, out float4 Out)
{
    Out = saturate(In);
}

void Unity_Normalize_float3(float3 In, out float3 Out)
{
    Out = normalize(In);
}

void Unity_DotProduct_float3(float3 A, float3 B, out float Out)
{
    Out = dot(A, B);
}

void Unity_Absolute_float(float In, out float Out)
{
    Out = abs(In);
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
}

// unity-custom-func-begin
void MainLight_float(out float3 Direction){

#ifdef UNITY_SHADER_GRAPH
	Direction = _WorldSpaceLightPos0.xyz;
#else
	Direction = float3(1,1,1);
#endif
}
// unity-custom-func-end

void Unity_DotProduct_float2(float2 A, float2 B, out float Out)
{
    Out = dot(A, B);
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
float _Property_7689f3a0523b4af9912879a05d1d5206_Out_0_Float = _Sway_Wind_Speed;
float3 _Property_e0531013c46845b6b036ac453118c744_Out_0_Vector3 = _Sway_Wind_Direction;
float _Property_35588e6a16b5457695dba39b1644d669_Out_0_Float = _Sway_Wind_Intensity;
float _Property_c8d3d7e6e6f64d409f010d988b96e327_Out_0_Float = _Sway_Wind_Time_Random_Offset_Amount;
Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float _WindSway_662bcd4f8b2b4607b138cb44de00a708;
_WindSway_662bcd4f8b2b4607b138cb44de00a708.ObjectSpacePosition = IN.ObjectSpacePosition;
_WindSway_662bcd4f8b2b4607b138cb44de00a708.TimeParameters = IN.TimeParameters;
float3 _WindSway_662bcd4f8b2b4607b138cb44de00a708_OutVector3_1_Vector3;
SG_WindSway_51c4027ae50a8cd49be53c4990b9f179_float(_Property_7689f3a0523b4af9912879a05d1d5206_Out_0_Float, _Property_e0531013c46845b6b036ac453118c744_Out_0_Vector3, _Property_35588e6a16b5457695dba39b1644d669_Out_0_Float, _Property_c8d3d7e6e6f64d409f010d988b96e327_Out_0_Float, _WindSway_662bcd4f8b2b4607b138cb44de00a708, _WindSway_662bcd4f8b2b4607b138cb44de00a708_OutVector3_1_Vector3);
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_R_1_Float = IN.WorldSpacePosition[0];
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_G_2_Float = IN.WorldSpacePosition[1];
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_B_3_Float = IN.WorldSpacePosition[2];
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_A_4_Float = 0;
float3 _Vector3_e154b524054546d2bbeab293c0e20519_Out_0_Vector3 = float3(float(0), float(0), float(0));
float3 _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3;
_Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3 = TransformObjectToWorld(_Vector3_e154b524054546d2bbeab293c0e20519_Out_0_Vector3.xyz);
float _Split_5eb48153bba44ea2b128583819f09a11_R_1_Float = _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3[0];
float _Split_5eb48153bba44ea2b128583819f09a11_G_2_Float = _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3[1];
float _Split_5eb48153bba44ea2b128583819f09a11_B_3_Float = _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3[2];
float _Split_5eb48153bba44ea2b128583819f09a11_A_4_Float = 0;
float _Subtract_d6a99abdf4e74644acffc1718d1735bf_Out_2_Float;
Unity_Subtract_float(_Split_8fe962eb6f0f4d6fba734f9edcaf9067_G_2_Float, _Split_5eb48153bba44ea2b128583819f09a11_G_2_Float, _Subtract_d6a99abdf4e74644acffc1718d1735bf_Out_2_Float);
float _Property_1cf3cda52e094e638df491b003607dfd_Out_0_Float = _Height_Weight;
float _Split_145cf2161d4c4510b08248cad819f655_R_1_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[0];
float _Split_145cf2161d4c4510b08248cad819f655_G_2_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[1];
float _Split_145cf2161d4c4510b08248cad819f655_B_3_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[2];
float _Split_145cf2161d4c4510b08248cad819f655_A_4_Float = 0;
float _Multiply_2c140d573b9447aab058bd863fe2f4f7_Out_2_Float;
Unity_Multiply_float_float(_Property_1cf3cda52e094e638df491b003607dfd_Out_0_Float, _Split_145cf2161d4c4510b08248cad819f655_G_2_Float, _Multiply_2c140d573b9447aab058bd863fe2f4f7_Out_2_Float);
float _Divide_0f6a637143d841d0b1c94fa2ae3e164d_Out_2_Float;
Unity_Divide_float(_Subtract_d6a99abdf4e74644acffc1718d1735bf_Out_2_Float, _Multiply_2c140d573b9447aab058bd863fe2f4f7_Out_2_Float, _Divide_0f6a637143d841d0b1c94fa2ae3e164d_Out_2_Float);
float _Saturate_1e4e29fb5c074565a1f941c046957b05_Out_1_Float;
Unity_Saturate_float(_Divide_0f6a637143d841d0b1c94fa2ae3e164d_Out_2_Float, _Saturate_1e4e29fb5c074565a1f941c046957b05_Out_1_Float);
float _Property_3d64a0c8dd104507b9a0762a778fe585_Out_0_Float = _Height_Gradience;
float _Power_f1d74728398a45f9993fe3b3affb6584_Out_2_Float;
Unity_Power_float(_Saturate_1e4e29fb5c074565a1f941c046957b05_Out_1_Float, _Property_3d64a0c8dd104507b9a0762a778fe585_Out_0_Float, _Power_f1d74728398a45f9993fe3b3affb6584_Out_2_Float);
float _Saturate_57b130350452418fa1d09d4a0ddd64ac_Out_1_Float;
Unity_Saturate_float(_Power_f1d74728398a45f9993fe3b3affb6584_Out_2_Float, _Saturate_57b130350452418fa1d09d4a0ddd64ac_Out_1_Float);
float3 _Lerp_1fbf8d0a7f5a4e2ab381967ac90385c7_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _WindSway_662bcd4f8b2b4607b138cb44de00a708_OutVector3_1_Vector3, (_Saturate_57b130350452418fa1d09d4a0ddd64ac_Out_1_Float.xxx), _Lerp_1fbf8d0a7f5a4e2ab381967ac90385c7_Out_3_Vector3);
float _Split_bd04916788224fa2bb71be3f27eb839a_R_1_Float = IN.WorldSpacePosition[0];
float _Split_bd04916788224fa2bb71be3f27eb839a_G_2_Float = IN.WorldSpacePosition[1];
float _Split_bd04916788224fa2bb71be3f27eb839a_B_3_Float = IN.WorldSpacePosition[2];
float _Split_bd04916788224fa2bb71be3f27eb839a_A_4_Float = 0;
float4 _Combine_45cdab9c20414d03862100967f56c78d_RGBA_4_Vector4;
float3 _Combine_45cdab9c20414d03862100967f56c78d_RGB_5_Vector3;
float2 _Combine_45cdab9c20414d03862100967f56c78d_RG_6_Vector2;
Unity_Combine_float(_Split_bd04916788224fa2bb71be3f27eb839a_R_1_Float, _Split_bd04916788224fa2bb71be3f27eb839a_B_3_Float, float(0), float(0), _Combine_45cdab9c20414d03862100967f56c78d_RGBA_4_Vector4, _Combine_45cdab9c20414d03862100967f56c78d_RGB_5_Vector3, _Combine_45cdab9c20414d03862100967f56c78d_RG_6_Vector2);
float _Property_9b35f6244c0247c7a9be7af2671f58e7_Out_0_Float = _Wiggle_Wind_Size;
float2 _Multiply_ba4d7f376f904143acc9eb33d8414ff8_Out_2_Vector2;
Unity_Multiply_float2_float2(_Combine_45cdab9c20414d03862100967f56c78d_RG_6_Vector2, (_Property_9b35f6244c0247c7a9be7af2671f58e7_Out_0_Float.xx), _Multiply_ba4d7f376f904143acc9eb33d8414ff8_Out_2_Vector2);
float _Property_6eae25110c184e7a94293b95ddc9ecef_Out_0_Float = _Wiggle_Wind_Speed_Small;
float _Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Property_6eae25110c184e7a94293b95ddc9ecef_Out_0_Float, _Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float);
float _Multiply_7510d5a95e6c4d3986bb83059d23d2fa_Out_2_Float;
Unity_Multiply_float_float(_Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float, 0.8, _Multiply_7510d5a95e6c4d3986bb83059d23d2fa_Out_2_Float);
float2 _Vector2_cb26bc8c681b455c81073f0dc3c8f6b4_Out_0_Vector2 = float2(_Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float, _Multiply_7510d5a95e6c4d3986bb83059d23d2fa_Out_2_Float);
float2 _TilingAndOffset_e5f47c2cf9d846859a72aadcdac7fc3b_Out_3_Vector2;
Unity_TilingAndOffset_float(_Multiply_ba4d7f376f904143acc9eb33d8414ff8_Out_2_Vector2, float2 (1, 1), _Vector2_cb26bc8c681b455c81073f0dc3c8f6b4_Out_0_Vector2, _TilingAndOffset_e5f47c2cf9d846859a72aadcdac7fc3b_Out_3_Vector2);
  float4 _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).tex, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).GetTransformedUV(_TilingAndOffset_e5f47c2cf9d846859a72aadcdac7fc3b_Out_3_Vector2), float(2));
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_R_5_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_G_6_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_B_7_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_A_8_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.a;
float _Float_3f1a7b5619414946b13e7cef19336d45_Out_0_Float = float(0.3);
float _Subtract_3105ef040f344481afb342cd9ef65e9f_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_R_5_Float, _Float_3f1a7b5619414946b13e7cef19336d45_Out_0_Float, _Subtract_3105ef040f344481afb342cd9ef65e9f_Out_2_Float);
float _Split_18495110217342bc80044d9345d98fbd_R_1_Float = IN.WorldSpacePosition[0];
float _Split_18495110217342bc80044d9345d98fbd_G_2_Float = IN.WorldSpacePosition[1];
float _Split_18495110217342bc80044d9345d98fbd_B_3_Float = IN.WorldSpacePosition[2];
float _Split_18495110217342bc80044d9345d98fbd_A_4_Float = 0;
float4 _Combine_d1ee3e49696840daa7cfd43df2287807_RGBA_4_Vector4;
float3 _Combine_d1ee3e49696840daa7cfd43df2287807_RGB_5_Vector3;
float2 _Combine_d1ee3e49696840daa7cfd43df2287807_RG_6_Vector2;
Unity_Combine_float(_Split_18495110217342bc80044d9345d98fbd_R_1_Float, _Split_18495110217342bc80044d9345d98fbd_G_2_Float, float(0), float(0), _Combine_d1ee3e49696840daa7cfd43df2287807_RGBA_4_Vector4, _Combine_d1ee3e49696840daa7cfd43df2287807_RGB_5_Vector3, _Combine_d1ee3e49696840daa7cfd43df2287807_RG_6_Vector2);
float _Property_9efe7c2851414ea39b6971daac6143e5_Out_0_Float = _Wiggle_Wind_Size;
float _Multiply_4f6b361114004341aeae12987f071375_Out_2_Float;
Unity_Multiply_float_float(_Property_9efe7c2851414ea39b6971daac6143e5_Out_0_Float, 0.763, _Multiply_4f6b361114004341aeae12987f071375_Out_2_Float);
float2 _Multiply_0d88e33790f0458b9e6e7f49946e6344_Out_2_Vector2;
Unity_Multiply_float2_float2(_Combine_d1ee3e49696840daa7cfd43df2287807_RG_6_Vector2, (_Multiply_4f6b361114004341aeae12987f071375_Out_2_Float.xx), _Multiply_0d88e33790f0458b9e6e7f49946e6344_Out_2_Vector2);
float _Property_56c994a79361438d88ea30b1e3ccf2aa_Out_0_Float = _Wiggle_Wind_Speed_Large;
float _Multiply_c5e5feed518642678424c6f0d45b4377_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Property_56c994a79361438d88ea30b1e3ccf2aa_Out_0_Float, _Multiply_c5e5feed518642678424c6f0d45b4377_Out_2_Float);
float2 _TilingAndOffset_7dac505987c144edad083d13ebca67e6_Out_3_Vector2;
Unity_TilingAndOffset_float(_Multiply_0d88e33790f0458b9e6e7f49946e6344_Out_2_Vector2, float2 (1, 1), (_Multiply_c5e5feed518642678424c6f0d45b4377_Out_2_Float.xx), _TilingAndOffset_7dac505987c144edad083d13ebca67e6_Out_3_Vector2);
  float4 _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).tex, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).GetTransformedUV(_TilingAndOffset_7dac505987c144edad083d13ebca67e6_Out_3_Vector2), float(2));
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_R_5_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_G_6_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_B_7_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_A_8_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.a;
float _Subtract_e29fd5b0daea41a1a38ac52280e6db88_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_G_6_Float, _Float_3f1a7b5619414946b13e7cef19336d45_Out_0_Float, _Subtract_e29fd5b0daea41a1a38ac52280e6db88_Out_2_Float);
float _Add_6d51be6104ee4e23a093dfb9c3d993f7_Out_2_Float;
Unity_Add_float(_Subtract_3105ef040f344481afb342cd9ef65e9f_Out_2_Float, _Subtract_e29fd5b0daea41a1a38ac52280e6db88_Out_2_Float, _Add_6d51be6104ee4e23a093dfb9c3d993f7_Out_2_Float);
float4 _Multiply_db043a8b3fc34edb8bee433085c7e061_Out_2_Vector4;
Unity_Multiply_float4_float4((_Add_6d51be6104ee4e23a093dfb9c3d993f7_Out_2_Float.xxxx), IN.VertexColor, _Multiply_db043a8b3fc34edb8bee433085c7e061_Out_2_Vector4);
float _Property_46c8c648052246a4a32e80905ed7e9df_Out_0_Float = _Wiggle_Wind_Intensity;
float4 _Multiply_4baa80310c414483a2aada72baa7a883_Out_2_Vector4;
Unity_Multiply_float4_float4(_Multiply_db043a8b3fc34edb8bee433085c7e061_Out_2_Vector4, (_Property_46c8c648052246a4a32e80905ed7e9df_Out_0_Float.xxxx), _Multiply_4baa80310c414483a2aada72baa7a883_Out_2_Vector4);
float3 _Multiply_2c95a93020c44a20be82a31b985c35b8_Out_2_Vector3;
Unity_Multiply_float3_float3((_Multiply_4baa80310c414483a2aada72baa7a883_Out_2_Vector4.xyz), IN.ObjectSpaceNormal, _Multiply_2c95a93020c44a20be82a31b985c35b8_Out_2_Vector3);
float3 _Add_5a143eeddea74bceb3ddd5b059f49da5_Out_2_Vector3;
Unity_Add_float3(_Lerp_1fbf8d0a7f5a4e2ab381967ac90385c7_Out_3_Vector3, _Multiply_2c95a93020c44a20be82a31b985c35b8_Out_2_Vector3, _Add_5a143eeddea74bceb3ddd5b059f49da5_Out_2_Vector3);
#if defined(_USE_WPO)
float3 _UseWPO_1f28bdd134c9432e812aded965021562_Out_0_Vector3 = _Add_5a143eeddea74bceb3ddd5b059f49da5_Out_2_Vector3;
#else
float3 _UseWPO_1f28bdd134c9432e812aded965021562_Out_0_Vector3 = IN.ObjectSpacePosition;
#endif
float3 _UPGFoliageContactPositionCustomFunction_3446fb1a0c894696bd903b7eb184e241_bentPositionOS_1_Vector3;
UPG_FoliageContactPosition_float(_UseWPO_1f28bdd134c9432e812aded965021562_Out_0_Vector3, _UPGFoliageContactPositionCustomFunction_3446fb1a0c894696bd903b7eb184e241_bentPositionOS_1_Vector3);
description.Position = _UPGFoliageContactPositionCustomFunction_3446fb1a0c894696bd903b7eb184e241_bentPositionOS_1_Vector3;
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
float4 _Property_a9c1982af1c74efb9ac25b0326ae7979_Out_0_Vector4 = _Foliage_Color_Bottom;
float4 _Property_50eb1cb960084765a0771df7f40a6d5d_Out_0_Vector4 = _Foliage_Color_Top;
float _Split_1540f44067c24640b4968355fa26a295_R_1_Float = IN.WorldSpacePosition[0];
float _Split_1540f44067c24640b4968355fa26a295_G_2_Float = IN.WorldSpacePosition[1];
float _Split_1540f44067c24640b4968355fa26a295_B_3_Float = IN.WorldSpacePosition[2];
float _Split_1540f44067c24640b4968355fa26a295_A_4_Float = 0;
float3 _Vector3_54c605479a824dd3a62abb1919f39c57_Out_0_Vector3 = float3(float(0), float(0), float(0));
float3 _Transform_f3f68ad0eeb04ce9ac459f2859353c06_Out_1_Vector3;
_Transform_f3f68ad0eeb04ce9ac459f2859353c06_Out_1_Vector3 = TransformObjectToWorld(_Vector3_54c605479a824dd3a62abb1919f39c57_Out_0_Vector3.xyz);
float _Split_15d5002ae66545318962a44958be8e00_R_1_Float = _Transform_f3f68ad0eeb04ce9ac459f2859353c06_Out_1_Vector3[0];
float _Split_15d5002ae66545318962a44958be8e00_G_2_Float = _Transform_f3f68ad0eeb04ce9ac459f2859353c06_Out_1_Vector3[1];
float _Split_15d5002ae66545318962a44958be8e00_B_3_Float = _Transform_f3f68ad0eeb04ce9ac459f2859353c06_Out_1_Vector3[2];
float _Split_15d5002ae66545318962a44958be8e00_A_4_Float = 0;
float _Subtract_3131eba61d024496834f2933dbde6855_Out_2_Float;
Unity_Subtract_float(_Split_1540f44067c24640b4968355fa26a295_G_2_Float, _Split_15d5002ae66545318962a44958be8e00_G_2_Float, _Subtract_3131eba61d024496834f2933dbde6855_Out_2_Float);
float _Property_605b997dac5a4228881635945f995c72_Out_0_Float = _Tree_Height;
float _Divide_8e4152d2ed4943778a44dc39efa65611_Out_2_Float;
Unity_Divide_float(_Subtract_3131eba61d024496834f2933dbde6855_Out_2_Float, _Property_605b997dac5a4228881635945f995c72_Out_0_Float, _Divide_8e4152d2ed4943778a44dc39efa65611_Out_2_Float);
float _Saturate_086c13233c3d43fab6a33a1e2ed498c2_Out_1_Float;
Unity_Saturate_float(_Divide_8e4152d2ed4943778a44dc39efa65611_Out_2_Float, _Saturate_086c13233c3d43fab6a33a1e2ed498c2_Out_1_Float);
float _Property_0e5bff248b2341909121bb16666f3468_Out_0_Float = _Gradience_Power;
float _Power_be0c59cc158d492f99ea24f3b1001e1f_Out_2_Float;
Unity_Power_float(_Saturate_086c13233c3d43fab6a33a1e2ed498c2_Out_1_Float, _Property_0e5bff248b2341909121bb16666f3468_Out_0_Float, _Power_be0c59cc158d492f99ea24f3b1001e1f_Out_2_Float);
float _Saturate_26bbc2447b5a402c9562a9e5d012fd6c_Out_1_Float;
Unity_Saturate_float(_Power_be0c59cc158d492f99ea24f3b1001e1f_Out_2_Float, _Saturate_26bbc2447b5a402c9562a9e5d012fd6c_Out_1_Float);
float4 _Lerp_795c581446834fe0bee35971bc8f0c5c_Out_3_Vector4;
Unity_Lerp_float4(_Property_a9c1982af1c74efb9ac25b0326ae7979_Out_0_Vector4, _Property_50eb1cb960084765a0771df7f40a6d5d_Out_0_Vector4, (_Saturate_26bbc2447b5a402c9562a9e5d012fd6c_Out_1_Float.xxxx), _Lerp_795c581446834fe0bee35971bc8f0c5c_Out_3_Vector4);
float4 _Property_4036fdaeaa4647adb3dd84d70ff1161e_Out_0_Vector4 = _Foliage_Color_Top;
#if defined(_USE_HEIGHT_GRADIENCE)
float4 _UseHeightGradience_d4326b9500c14f11a9090b7e8f107721_Out_0_Vector4 = _Lerp_795c581446834fe0bee35971bc8f0c5c_Out_3_Vector4;
#else
float4 _UseHeightGradience_d4326b9500c14f11a9090b7e8f107721_Out_0_Vector4 = _Property_4036fdaeaa4647adb3dd84d70ff1161e_Out_0_Vector4;
#endif
UnityTexture2D _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Color_Map, sampler_Color_Map, _Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_eaea7ba5e1e74519a1bc7833f0103ab8_Out_0_Vector4 = IN.uv0;
float _Property_95352575ae4140008287fd58138e8eb5_Out_0_Float = _Color_Map_Tiling;
float4 _Multiply_86fafeae394745fe880655d03cafd9e1_Out_2_Vector4;
Unity_Multiply_float4_float4(_UV_eaea7ba5e1e74519a1bc7833f0103ab8_Out_0_Vector4, (_Property_95352575ae4140008287fd58138e8eb5_Out_0_Float.xxxx), _Multiply_86fafeae394745fe880655d03cafd9e1_Out_2_Vector4);
float4 _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.tex, _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.samplerstate, _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.GetTransformedUV((_Multiply_86fafeae394745fe880655d03cafd9e1_Out_2_Vector4.xy)) );
if (_Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4, _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_R_4_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.r;
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_G_5_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.g;
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_B_6_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.b;
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_A_7_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.a;
float4 _Multiply_a05088f8e3434d54920d012fc47555a7_Out_2_Vector4;
Unity_Multiply_float4_float4(_UseHeightGradience_d4326b9500c14f11a9090b7e8f107721_Out_0_Vector4, _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4, _Multiply_a05088f8e3434d54920d012fc47555a7_Out_2_Vector4);
float4 _Property_ee5875e8bc214caba4dbea1511d06cd2_Out_0_Vector4 = _Always_On_Fresnel_Color;
float _Property_be2ed593ceb345f89b57d123af3db203_Out_0_Float = _Always_On_Fresnel_Intensity;
float4 _Multiply_dcf6e90c046c4f72b77b81df5b922468_Out_2_Vector4;
Unity_Multiply_float4_float4(_Property_ee5875e8bc214caba4dbea1511d06cd2_Out_0_Vector4, (_Property_be2ed593ceb345f89b57d123af3db203_Out_0_Float.xxxx), _Multiply_dcf6e90c046c4f72b77b81df5b922468_Out_2_Vector4);
float4 _Saturate_26f2848ceb0a494094065a9387275032_Out_1_Vector4;
Unity_Saturate_float4(_Multiply_dcf6e90c046c4f72b77b81df5b922468_Out_2_Vector4, _Saturate_26f2848ceb0a494094065a9387275032_Out_1_Vector4);
float3 _Normalize_764cced5d0894f3d89f5911d50d5f4a2_Out_1_Vector3;
Unity_Normalize_float3(IN.WorldSpaceViewDirection, _Normalize_764cced5d0894f3d89f5911d50d5f4a2_Out_1_Vector3);
float _DotProduct_eb716c3ca301415688a52e76b310a65c_Out_2_Float;
Unity_DotProduct_float3(IN.WorldSpaceNormal, _Normalize_764cced5d0894f3d89f5911d50d5f4a2_Out_1_Vector3, _DotProduct_eb716c3ca301415688a52e76b310a65c_Out_2_Float);
float _Absolute_470cf4f0298a44a695f47aa5ea61c0ab_Out_1_Float;
Unity_Absolute_float(_DotProduct_eb716c3ca301415688a52e76b310a65c_Out_2_Float, _Absolute_470cf4f0298a44a695f47aa5ea61c0ab_Out_1_Float);
float _OneMinus_fa832b719fc24001be421507d395a592_Out_1_Float;
Unity_OneMinus_float(_Absolute_470cf4f0298a44a695f47aa5ea61c0ab_Out_1_Float, _OneMinus_fa832b719fc24001be421507d395a592_Out_1_Float);
float _Property_d9cfcacb90204bdfa36d0c1b7c0da452_Out_0_Float = _Always_On_Fresnel_Pow;
float _Power_7841b1ae85554473b96cce132c7ff7e1_Out_2_Float;
Unity_Power_float(_OneMinus_fa832b719fc24001be421507d395a592_Out_1_Float, _Property_d9cfcacb90204bdfa36d0c1b7c0da452_Out_0_Float, _Power_7841b1ae85554473b96cce132c7ff7e1_Out_2_Float);
float _Saturate_908f6307f948471ca95c0e7507cd7e43_Out_1_Float;
Unity_Saturate_float(_Power_7841b1ae85554473b96cce132c7ff7e1_Out_2_Float, _Saturate_908f6307f948471ca95c0e7507cd7e43_Out_1_Float);
float4 _Lerp_3406aa7c52b14c818ac9eb76bf068e2d_Out_3_Vector4;
Unity_Lerp_float4(_Multiply_a05088f8e3434d54920d012fc47555a7_Out_2_Vector4, _Saturate_26f2848ceb0a494094065a9387275032_Out_1_Vector4, (_Saturate_908f6307f948471ca95c0e7507cd7e43_Out_1_Float.xxxx), _Lerp_3406aa7c52b14c818ac9eb76bf068e2d_Out_3_Vector4);
float _Property_e8b3335851f84d91b28234460148cd0f_Out_0_Float = _Subsurface_Intensity;
float4 _Multiply_94df75e4ad734308a11d784250fc0b0c_Out_2_Vector4;
Unity_Multiply_float4_float4(_Lerp_3406aa7c52b14c818ac9eb76bf068e2d_Out_3_Vector4, (_Property_e8b3335851f84d91b28234460148cd0f_Out_0_Float.xxxx), _Multiply_94df75e4ad734308a11d784250fc0b0c_Out_2_Vector4);
float4 _Saturate_f0090edc5c664784a336737069276109_Out_1_Vector4;
Unity_Saturate_float4(_Multiply_94df75e4ad734308a11d784250fc0b0c_Out_2_Vector4, _Saturate_f0090edc5c664784a336737069276109_Out_1_Vector4);
float3 _MainLightCustomFunction_461af74b29b6456da89a2348455c2483_Direction_0_Vector3;
MainLight_float(_MainLightCustomFunction_461af74b29b6456da89a2348455c2483_Direction_0_Vector3);
float3 _Normalize_8644c278d597434b8871eb11a7a52cfe_Out_1_Vector3;
Unity_Normalize_float3(_MainLightCustomFunction_461af74b29b6456da89a2348455c2483_Direction_0_Vector3, _Normalize_8644c278d597434b8871eb11a7a52cfe_Out_1_Vector3);
float _Split_8a104625401f4a048b1160e17800fd11_R_1_Float = _Normalize_8644c278d597434b8871eb11a7a52cfe_Out_1_Vector3[0];
float _Split_8a104625401f4a048b1160e17800fd11_G_2_Float = _Normalize_8644c278d597434b8871eb11a7a52cfe_Out_1_Vector3[1];
float _Split_8a104625401f4a048b1160e17800fd11_B_3_Float = _Normalize_8644c278d597434b8871eb11a7a52cfe_Out_1_Vector3[2];
float _Split_8a104625401f4a048b1160e17800fd11_A_4_Float = 0;
float4 _Combine_87a90606ca4545b2983770732c0ed1b7_RGBA_4_Vector4;
float3 _Combine_87a90606ca4545b2983770732c0ed1b7_RGB_5_Vector3;
float2 _Combine_87a90606ca4545b2983770732c0ed1b7_RG_6_Vector2;
Unity_Combine_float(_Split_8a104625401f4a048b1160e17800fd11_R_1_Float, _Split_8a104625401f4a048b1160e17800fd11_B_3_Float, float(0), float(0), _Combine_87a90606ca4545b2983770732c0ed1b7_RGBA_4_Vector4, _Combine_87a90606ca4545b2983770732c0ed1b7_RGB_5_Vector3, _Combine_87a90606ca4545b2983770732c0ed1b7_RG_6_Vector2);
float3 _Normalize_f1ad7ea0af7b45ee88df143397ba6727_Out_1_Vector3;
Unity_Normalize_float3(-UNITY_MATRIX_V[2].xyz, _Normalize_f1ad7ea0af7b45ee88df143397ba6727_Out_1_Vector3);
float3 _Multiply_5b2d23749b2347b7af4472236c1dfe22_Out_2_Vector3;
Unity_Multiply_float3_float3(_Normalize_f1ad7ea0af7b45ee88df143397ba6727_Out_1_Vector3, float3(-1, -1, -1), _Multiply_5b2d23749b2347b7af4472236c1dfe22_Out_2_Vector3);
float _Split_6ebebe9410ea41bd8f8a327255848dc9_R_1_Float = _Multiply_5b2d23749b2347b7af4472236c1dfe22_Out_2_Vector3[0];
float _Split_6ebebe9410ea41bd8f8a327255848dc9_G_2_Float = _Multiply_5b2d23749b2347b7af4472236c1dfe22_Out_2_Vector3[1];
float _Split_6ebebe9410ea41bd8f8a327255848dc9_B_3_Float = _Multiply_5b2d23749b2347b7af4472236c1dfe22_Out_2_Vector3[2];
float _Split_6ebebe9410ea41bd8f8a327255848dc9_A_4_Float = 0;
float4 _Combine_ce9284087dd446409babf9c154f18fe4_RGBA_4_Vector4;
float3 _Combine_ce9284087dd446409babf9c154f18fe4_RGB_5_Vector3;
float2 _Combine_ce9284087dd446409babf9c154f18fe4_RG_6_Vector2;
Unity_Combine_float(_Split_6ebebe9410ea41bd8f8a327255848dc9_R_1_Float, _Split_6ebebe9410ea41bd8f8a327255848dc9_B_3_Float, float(0), float(0), _Combine_ce9284087dd446409babf9c154f18fe4_RGBA_4_Vector4, _Combine_ce9284087dd446409babf9c154f18fe4_RGB_5_Vector3, _Combine_ce9284087dd446409babf9c154f18fe4_RG_6_Vector2);
float _DotProduct_9e4f6525393a4171b6519e347a4b7a37_Out_2_Float;
Unity_DotProduct_float2(_Combine_87a90606ca4545b2983770732c0ed1b7_RG_6_Vector2, _Combine_ce9284087dd446409babf9c154f18fe4_RG_6_Vector2, _DotProduct_9e4f6525393a4171b6519e347a4b7a37_Out_2_Float);
float _Saturate_f76f8f4113e0468b9b20a6abf253d76e_Out_1_Float;
Unity_Saturate_float(_DotProduct_9e4f6525393a4171b6519e347a4b7a37_Out_2_Float, _Saturate_f76f8f4113e0468b9b20a6abf253d76e_Out_1_Float);
float _Property_bd14cfb26b074dbabde1a5a9b7f95d97_Out_0_Float = _Subsurface_Transition_Smoothness;
float _Power_2bc1d129029a4da69b2993ea07ff412f_Out_2_Float;
Unity_Power_float(_Saturate_f76f8f4113e0468b9b20a6abf253d76e_Out_1_Float, _Property_bd14cfb26b074dbabde1a5a9b7f95d97_Out_0_Float, _Power_2bc1d129029a4da69b2993ea07ff412f_Out_2_Float);
float _Saturate_44588f923e1b48ff97cd55ea2d69aa2b_Out_1_Float;
Unity_Saturate_float(_Power_2bc1d129029a4da69b2993ea07ff412f_Out_2_Float, _Saturate_44588f923e1b48ff97cd55ea2d69aa2b_Out_1_Float);
float4 _Lerp_98f83471835040419ec38e468d855fe3_Out_3_Vector4;
Unity_Lerp_float4(_Lerp_3406aa7c52b14c818ac9eb76bf068e2d_Out_3_Vector4, _Saturate_f0090edc5c664784a336737069276109_Out_1_Vector4, (_Saturate_44588f923e1b48ff97cd55ea2d69aa2b_Out_1_Float.xxxx), _Lerp_98f83471835040419ec38e468d855fe3_Out_3_Vector4);
float _Property_e00e00901e414a40a25484eb2c276584_Out_0_Float = _Subsurface_Fresnel_Intensity;
float4 _Multiply_6ed60680651f4e8cbff9e6f2ed97f0b9_Out_2_Vector4;
Unity_Multiply_float4_float4(_Lerp_98f83471835040419ec38e468d855fe3_Out_3_Vector4, (_Property_e00e00901e414a40a25484eb2c276584_Out_0_Float.xxxx), _Multiply_6ed60680651f4e8cbff9e6f2ed97f0b9_Out_2_Vector4);
float4 _Saturate_5cea81f1b9b5405fb4a571a97e57df52_Out_1_Vector4;
Unity_Saturate_float4(_Multiply_6ed60680651f4e8cbff9e6f2ed97f0b9_Out_2_Vector4, _Saturate_5cea81f1b9b5405fb4a571a97e57df52_Out_1_Vector4);
float _Saturate_10ad388197014e2fa2d0bd8261d82207_Out_1_Float;
Unity_Saturate_float(_DotProduct_9e4f6525393a4171b6519e347a4b7a37_Out_2_Float, _Saturate_10ad388197014e2fa2d0bd8261d82207_Out_1_Float);
float _Property_fe3bfd62823b45bf868d2c88982c1f64_Out_0_Float = _Subsurface_Fresnel_Transition_Smoothness;
float _Power_1ad124a5d5b74c13848df4f8f171d448_Out_2_Float;
Unity_Power_float(_Saturate_10ad388197014e2fa2d0bd8261d82207_Out_1_Float, _Property_fe3bfd62823b45bf868d2c88982c1f64_Out_0_Float, _Power_1ad124a5d5b74c13848df4f8f171d448_Out_2_Float);
float3 _Normalize_7e92e50b98274c508fa061a32d7a99b7_Out_1_Vector3;
Unity_Normalize_float3(IN.WorldSpaceViewDirection, _Normalize_7e92e50b98274c508fa061a32d7a99b7_Out_1_Vector3);
float _DotProduct_3170aef4b99740d083af85a5aa294edd_Out_2_Float;
Unity_DotProduct_float3(IN.WorldSpaceNormal, _Normalize_7e92e50b98274c508fa061a32d7a99b7_Out_1_Vector3, _DotProduct_3170aef4b99740d083af85a5aa294edd_Out_2_Float);
float _Absolute_111753c15e794f4b8e885c2c19d93d82_Out_1_Float;
Unity_Absolute_float(_DotProduct_3170aef4b99740d083af85a5aa294edd_Out_2_Float, _Absolute_111753c15e794f4b8e885c2c19d93d82_Out_1_Float);
float _OneMinus_2a02f0d378e1454c9aeda0ff447d4d86_Out_1_Float;
Unity_OneMinus_float(_Absolute_111753c15e794f4b8e885c2c19d93d82_Out_1_Float, _OneMinus_2a02f0d378e1454c9aeda0ff447d4d86_Out_1_Float);
float _Property_49c0203b77734a36b9ba06f7bcd723ee_Out_0_Float = _Subsurface_Fresnel_Pow;
float _Power_71121b48d5934b11a14aeadcad656816_Out_2_Float;
Unity_Power_float(_OneMinus_2a02f0d378e1454c9aeda0ff447d4d86_Out_1_Float, _Property_49c0203b77734a36b9ba06f7bcd723ee_Out_0_Float, _Power_71121b48d5934b11a14aeadcad656816_Out_2_Float);
float _Saturate_29e69580558f40c49a883332cdf25357_Out_1_Float;
Unity_Saturate_float(_Power_71121b48d5934b11a14aeadcad656816_Out_2_Float, _Saturate_29e69580558f40c49a883332cdf25357_Out_1_Float);
float _Multiply_328c604c9492493ba1dc1999ef6314fd_Out_2_Float;
Unity_Multiply_float_float(_Power_1ad124a5d5b74c13848df4f8f171d448_Out_2_Float, _Saturate_29e69580558f40c49a883332cdf25357_Out_1_Float, _Multiply_328c604c9492493ba1dc1999ef6314fd_Out_2_Float);
float4 _Lerp_10a8be366b974da7b00eab9c55f88a8a_Out_3_Vector4;
Unity_Lerp_float4(_Lerp_98f83471835040419ec38e468d855fe3_Out_3_Vector4, _Saturate_5cea81f1b9b5405fb4a571a97e57df52_Out_1_Vector4, (_Multiply_328c604c9492493ba1dc1999ef6314fd_Out_2_Float.xxxx), _Lerp_10a8be366b974da7b00eab9c55f88a8a_Out_3_Vector4);
float _Multiply_cf1ea983d11e4ab7b4bdbbb064ad94b5_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_A_7_Float, 1, _Multiply_cf1ea983d11e4ab7b4bdbbb064ad94b5_Out_2_Float);
float _Property_99bb0b22edbb4ea0b1b7844414b876a7_Out_0_Float = _Alpha_Clip;
surface.BaseColor = (_Lerp_10a8be366b974da7b00eab9c55f88a8a_Out_3_Vector4.xyz);
surface.Alpha = _Multiply_cf1ea983d11e4ab7b4bdbbb064ad94b5_Out_2_Float;
surface.AlphaClipThreshold = _Property_99bb0b22edbb4ea0b1b7844414b876a7_Out_0_Float;
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


    output.WorldSpaceViewDirection = GetWorldSpaceNormalizeViewDir(input.positionWS);
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
#pragma shader_feature_local _ _USE_WPO
#pragma shader_feature_local _ _USE_HEIGHT_GRADIENCE

#if defined(_USE_WPO) && defined(_USE_HEIGHT_GRADIENCE)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_WPO)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_HEIGHT_GRADIENCE)
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
#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_NORMAL_WS
#define VARYINGS_NEED_TANGENT_WS
#define VARYINGS_NEED_TEXCOORD0
#define VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_FORWARD
#define BUILTIN_TARGET_API 1
#define _BUILTIN_AlphaClip 1
#define _BUILTIN_ALPHATEST_ON 1
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
 float3 WorldSpaceNormal;
 float3 TangentSpaceNormal;
 float3 WorldSpaceViewDirection;
 float3 WorldSpacePosition;
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 WorldSpacePosition;
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

float4 _Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D_TexelSize;
float _Subsurface_Fresnel_Transition_Smoothness;
float _Subsurface_Fresnel_Pow;
float _Subsurface_Fresnel_Intensity;
float _Subsurface_Transition_Smoothness;
float _Wiggle_Wind_Speed_Large;
float _Wiggle_Wind_Intensity;
float _Roughness;
float4 _Foliage_Color_Bottom;
float _Sway_Wind_Speed;
float _Gradience_Power;
float _Fresnel_Mul;
float4 _Foliage_Color_Top;
float _Always_On_Fresnel_Intensity;
float _Tree_Height;
float4 _Color_Map_TexelSize;
float _Height_Weight;
float _Height_Gradience;
float _Sway_Wind_Time_Random_Offset_Amount;
float _Sway_Wind_Intensity;
float3 _Sway_Wind_Direction;
float _Color_Map_Tiling;
float _Subsurface_Intensity;
float _Alpha_Clip;
float _Wiggle_Wind_Size;
float _Wiggle_Wind_Speed_Small;
float _Fresnel_Pow;
float _Always_On_Fresnel_Pow;
float4 _Always_On_Fresnel_Color;
float _ree;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D);
SAMPLER(sampler_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D);
TEXTURE2D(_Color_Map);
SAMPLER(sampler_Color_Map);

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

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Saturate_float4(float4 In, out float4 Out)
{
    Out = saturate(In);
}

void Unity_Normalize_float3(float3 In, out float3 Out)
{
    Out = normalize(In);
}

void Unity_DotProduct_float3(float3 A, float3 B, out float Out)
{
    Out = dot(A, B);
}

void Unity_Absolute_float(float In, out float Out)
{
    Out = abs(In);
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
}

// unity-custom-func-begin
void MainLight_float(out float3 Direction){

#ifdef UNITY_SHADER_GRAPH
	Direction = _WorldSpaceLightPos0.xyz;
#else
	Direction = float3(1,1,1);
#endif
}
// unity-custom-func-end

void Unity_DotProduct_float2(float2 A, float2 B, out float Out)
{
    Out = dot(A, B);
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
float _Property_7689f3a0523b4af9912879a05d1d5206_Out_0_Float = _Sway_Wind_Speed;
float3 _Property_e0531013c46845b6b036ac453118c744_Out_0_Vector3 = _Sway_Wind_Direction;
float _Property_35588e6a16b5457695dba39b1644d669_Out_0_Float = _Sway_Wind_Intensity;
float _Property_c8d3d7e6e6f64d409f010d988b96e327_Out_0_Float = _Sway_Wind_Time_Random_Offset_Amount;
Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float _WindSway_662bcd4f8b2b4607b138cb44de00a708;
_WindSway_662bcd4f8b2b4607b138cb44de00a708.ObjectSpacePosition = IN.ObjectSpacePosition;
_WindSway_662bcd4f8b2b4607b138cb44de00a708.TimeParameters = IN.TimeParameters;
float3 _WindSway_662bcd4f8b2b4607b138cb44de00a708_OutVector3_1_Vector3;
SG_WindSway_51c4027ae50a8cd49be53c4990b9f179_float(_Property_7689f3a0523b4af9912879a05d1d5206_Out_0_Float, _Property_e0531013c46845b6b036ac453118c744_Out_0_Vector3, _Property_35588e6a16b5457695dba39b1644d669_Out_0_Float, _Property_c8d3d7e6e6f64d409f010d988b96e327_Out_0_Float, _WindSway_662bcd4f8b2b4607b138cb44de00a708, _WindSway_662bcd4f8b2b4607b138cb44de00a708_OutVector3_1_Vector3);
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_R_1_Float = IN.WorldSpacePosition[0];
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_G_2_Float = IN.WorldSpacePosition[1];
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_B_3_Float = IN.WorldSpacePosition[2];
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_A_4_Float = 0;
float3 _Vector3_e154b524054546d2bbeab293c0e20519_Out_0_Vector3 = float3(float(0), float(0), float(0));
float3 _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3;
_Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3 = TransformObjectToWorld(_Vector3_e154b524054546d2bbeab293c0e20519_Out_0_Vector3.xyz);
float _Split_5eb48153bba44ea2b128583819f09a11_R_1_Float = _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3[0];
float _Split_5eb48153bba44ea2b128583819f09a11_G_2_Float = _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3[1];
float _Split_5eb48153bba44ea2b128583819f09a11_B_3_Float = _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3[2];
float _Split_5eb48153bba44ea2b128583819f09a11_A_4_Float = 0;
float _Subtract_d6a99abdf4e74644acffc1718d1735bf_Out_2_Float;
Unity_Subtract_float(_Split_8fe962eb6f0f4d6fba734f9edcaf9067_G_2_Float, _Split_5eb48153bba44ea2b128583819f09a11_G_2_Float, _Subtract_d6a99abdf4e74644acffc1718d1735bf_Out_2_Float);
float _Property_1cf3cda52e094e638df491b003607dfd_Out_0_Float = _Height_Weight;
float _Split_145cf2161d4c4510b08248cad819f655_R_1_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[0];
float _Split_145cf2161d4c4510b08248cad819f655_G_2_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[1];
float _Split_145cf2161d4c4510b08248cad819f655_B_3_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[2];
float _Split_145cf2161d4c4510b08248cad819f655_A_4_Float = 0;
float _Multiply_2c140d573b9447aab058bd863fe2f4f7_Out_2_Float;
Unity_Multiply_float_float(_Property_1cf3cda52e094e638df491b003607dfd_Out_0_Float, _Split_145cf2161d4c4510b08248cad819f655_G_2_Float, _Multiply_2c140d573b9447aab058bd863fe2f4f7_Out_2_Float);
float _Divide_0f6a637143d841d0b1c94fa2ae3e164d_Out_2_Float;
Unity_Divide_float(_Subtract_d6a99abdf4e74644acffc1718d1735bf_Out_2_Float, _Multiply_2c140d573b9447aab058bd863fe2f4f7_Out_2_Float, _Divide_0f6a637143d841d0b1c94fa2ae3e164d_Out_2_Float);
float _Saturate_1e4e29fb5c074565a1f941c046957b05_Out_1_Float;
Unity_Saturate_float(_Divide_0f6a637143d841d0b1c94fa2ae3e164d_Out_2_Float, _Saturate_1e4e29fb5c074565a1f941c046957b05_Out_1_Float);
float _Property_3d64a0c8dd104507b9a0762a778fe585_Out_0_Float = _Height_Gradience;
float _Power_f1d74728398a45f9993fe3b3affb6584_Out_2_Float;
Unity_Power_float(_Saturate_1e4e29fb5c074565a1f941c046957b05_Out_1_Float, _Property_3d64a0c8dd104507b9a0762a778fe585_Out_0_Float, _Power_f1d74728398a45f9993fe3b3affb6584_Out_2_Float);
float _Saturate_57b130350452418fa1d09d4a0ddd64ac_Out_1_Float;
Unity_Saturate_float(_Power_f1d74728398a45f9993fe3b3affb6584_Out_2_Float, _Saturate_57b130350452418fa1d09d4a0ddd64ac_Out_1_Float);
float3 _Lerp_1fbf8d0a7f5a4e2ab381967ac90385c7_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _WindSway_662bcd4f8b2b4607b138cb44de00a708_OutVector3_1_Vector3, (_Saturate_57b130350452418fa1d09d4a0ddd64ac_Out_1_Float.xxx), _Lerp_1fbf8d0a7f5a4e2ab381967ac90385c7_Out_3_Vector3);
float _Split_bd04916788224fa2bb71be3f27eb839a_R_1_Float = IN.WorldSpacePosition[0];
float _Split_bd04916788224fa2bb71be3f27eb839a_G_2_Float = IN.WorldSpacePosition[1];
float _Split_bd04916788224fa2bb71be3f27eb839a_B_3_Float = IN.WorldSpacePosition[2];
float _Split_bd04916788224fa2bb71be3f27eb839a_A_4_Float = 0;
float4 _Combine_45cdab9c20414d03862100967f56c78d_RGBA_4_Vector4;
float3 _Combine_45cdab9c20414d03862100967f56c78d_RGB_5_Vector3;
float2 _Combine_45cdab9c20414d03862100967f56c78d_RG_6_Vector2;
Unity_Combine_float(_Split_bd04916788224fa2bb71be3f27eb839a_R_1_Float, _Split_bd04916788224fa2bb71be3f27eb839a_B_3_Float, float(0), float(0), _Combine_45cdab9c20414d03862100967f56c78d_RGBA_4_Vector4, _Combine_45cdab9c20414d03862100967f56c78d_RGB_5_Vector3, _Combine_45cdab9c20414d03862100967f56c78d_RG_6_Vector2);
float _Property_9b35f6244c0247c7a9be7af2671f58e7_Out_0_Float = _Wiggle_Wind_Size;
float2 _Multiply_ba4d7f376f904143acc9eb33d8414ff8_Out_2_Vector2;
Unity_Multiply_float2_float2(_Combine_45cdab9c20414d03862100967f56c78d_RG_6_Vector2, (_Property_9b35f6244c0247c7a9be7af2671f58e7_Out_0_Float.xx), _Multiply_ba4d7f376f904143acc9eb33d8414ff8_Out_2_Vector2);
float _Property_6eae25110c184e7a94293b95ddc9ecef_Out_0_Float = _Wiggle_Wind_Speed_Small;
float _Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Property_6eae25110c184e7a94293b95ddc9ecef_Out_0_Float, _Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float);
float _Multiply_7510d5a95e6c4d3986bb83059d23d2fa_Out_2_Float;
Unity_Multiply_float_float(_Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float, 0.8, _Multiply_7510d5a95e6c4d3986bb83059d23d2fa_Out_2_Float);
float2 _Vector2_cb26bc8c681b455c81073f0dc3c8f6b4_Out_0_Vector2 = float2(_Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float, _Multiply_7510d5a95e6c4d3986bb83059d23d2fa_Out_2_Float);
float2 _TilingAndOffset_e5f47c2cf9d846859a72aadcdac7fc3b_Out_3_Vector2;
Unity_TilingAndOffset_float(_Multiply_ba4d7f376f904143acc9eb33d8414ff8_Out_2_Vector2, float2 (1, 1), _Vector2_cb26bc8c681b455c81073f0dc3c8f6b4_Out_0_Vector2, _TilingAndOffset_e5f47c2cf9d846859a72aadcdac7fc3b_Out_3_Vector2);
  float4 _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).tex, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).GetTransformedUV(_TilingAndOffset_e5f47c2cf9d846859a72aadcdac7fc3b_Out_3_Vector2), float(2));
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_R_5_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_G_6_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_B_7_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_A_8_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.a;
float _Float_3f1a7b5619414946b13e7cef19336d45_Out_0_Float = float(0.3);
float _Subtract_3105ef040f344481afb342cd9ef65e9f_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_R_5_Float, _Float_3f1a7b5619414946b13e7cef19336d45_Out_0_Float, _Subtract_3105ef040f344481afb342cd9ef65e9f_Out_2_Float);
float _Split_18495110217342bc80044d9345d98fbd_R_1_Float = IN.WorldSpacePosition[0];
float _Split_18495110217342bc80044d9345d98fbd_G_2_Float = IN.WorldSpacePosition[1];
float _Split_18495110217342bc80044d9345d98fbd_B_3_Float = IN.WorldSpacePosition[2];
float _Split_18495110217342bc80044d9345d98fbd_A_4_Float = 0;
float4 _Combine_d1ee3e49696840daa7cfd43df2287807_RGBA_4_Vector4;
float3 _Combine_d1ee3e49696840daa7cfd43df2287807_RGB_5_Vector3;
float2 _Combine_d1ee3e49696840daa7cfd43df2287807_RG_6_Vector2;
Unity_Combine_float(_Split_18495110217342bc80044d9345d98fbd_R_1_Float, _Split_18495110217342bc80044d9345d98fbd_G_2_Float, float(0), float(0), _Combine_d1ee3e49696840daa7cfd43df2287807_RGBA_4_Vector4, _Combine_d1ee3e49696840daa7cfd43df2287807_RGB_5_Vector3, _Combine_d1ee3e49696840daa7cfd43df2287807_RG_6_Vector2);
float _Property_9efe7c2851414ea39b6971daac6143e5_Out_0_Float = _Wiggle_Wind_Size;
float _Multiply_4f6b361114004341aeae12987f071375_Out_2_Float;
Unity_Multiply_float_float(_Property_9efe7c2851414ea39b6971daac6143e5_Out_0_Float, 0.763, _Multiply_4f6b361114004341aeae12987f071375_Out_2_Float);
float2 _Multiply_0d88e33790f0458b9e6e7f49946e6344_Out_2_Vector2;
Unity_Multiply_float2_float2(_Combine_d1ee3e49696840daa7cfd43df2287807_RG_6_Vector2, (_Multiply_4f6b361114004341aeae12987f071375_Out_2_Float.xx), _Multiply_0d88e33790f0458b9e6e7f49946e6344_Out_2_Vector2);
float _Property_56c994a79361438d88ea30b1e3ccf2aa_Out_0_Float = _Wiggle_Wind_Speed_Large;
float _Multiply_c5e5feed518642678424c6f0d45b4377_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Property_56c994a79361438d88ea30b1e3ccf2aa_Out_0_Float, _Multiply_c5e5feed518642678424c6f0d45b4377_Out_2_Float);
float2 _TilingAndOffset_7dac505987c144edad083d13ebca67e6_Out_3_Vector2;
Unity_TilingAndOffset_float(_Multiply_0d88e33790f0458b9e6e7f49946e6344_Out_2_Vector2, float2 (1, 1), (_Multiply_c5e5feed518642678424c6f0d45b4377_Out_2_Float.xx), _TilingAndOffset_7dac505987c144edad083d13ebca67e6_Out_3_Vector2);
  float4 _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).tex, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).GetTransformedUV(_TilingAndOffset_7dac505987c144edad083d13ebca67e6_Out_3_Vector2), float(2));
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_R_5_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_G_6_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_B_7_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_A_8_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.a;
float _Subtract_e29fd5b0daea41a1a38ac52280e6db88_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_G_6_Float, _Float_3f1a7b5619414946b13e7cef19336d45_Out_0_Float, _Subtract_e29fd5b0daea41a1a38ac52280e6db88_Out_2_Float);
float _Add_6d51be6104ee4e23a093dfb9c3d993f7_Out_2_Float;
Unity_Add_float(_Subtract_3105ef040f344481afb342cd9ef65e9f_Out_2_Float, _Subtract_e29fd5b0daea41a1a38ac52280e6db88_Out_2_Float, _Add_6d51be6104ee4e23a093dfb9c3d993f7_Out_2_Float);
float4 _Multiply_db043a8b3fc34edb8bee433085c7e061_Out_2_Vector4;
Unity_Multiply_float4_float4((_Add_6d51be6104ee4e23a093dfb9c3d993f7_Out_2_Float.xxxx), IN.VertexColor, _Multiply_db043a8b3fc34edb8bee433085c7e061_Out_2_Vector4);
float _Property_46c8c648052246a4a32e80905ed7e9df_Out_0_Float = _Wiggle_Wind_Intensity;
float4 _Multiply_4baa80310c414483a2aada72baa7a883_Out_2_Vector4;
Unity_Multiply_float4_float4(_Multiply_db043a8b3fc34edb8bee433085c7e061_Out_2_Vector4, (_Property_46c8c648052246a4a32e80905ed7e9df_Out_0_Float.xxxx), _Multiply_4baa80310c414483a2aada72baa7a883_Out_2_Vector4);
float3 _Multiply_2c95a93020c44a20be82a31b985c35b8_Out_2_Vector3;
Unity_Multiply_float3_float3((_Multiply_4baa80310c414483a2aada72baa7a883_Out_2_Vector4.xyz), IN.ObjectSpaceNormal, _Multiply_2c95a93020c44a20be82a31b985c35b8_Out_2_Vector3);
float3 _Add_5a143eeddea74bceb3ddd5b059f49da5_Out_2_Vector3;
Unity_Add_float3(_Lerp_1fbf8d0a7f5a4e2ab381967ac90385c7_Out_3_Vector3, _Multiply_2c95a93020c44a20be82a31b985c35b8_Out_2_Vector3, _Add_5a143eeddea74bceb3ddd5b059f49da5_Out_2_Vector3);
#if defined(_USE_WPO)
float3 _UseWPO_1f28bdd134c9432e812aded965021562_Out_0_Vector3 = _Add_5a143eeddea74bceb3ddd5b059f49da5_Out_2_Vector3;
#else
float3 _UseWPO_1f28bdd134c9432e812aded965021562_Out_0_Vector3 = IN.ObjectSpacePosition;
#endif
float3 _UPGFoliageContactPositionCustomFunction_3446fb1a0c894696bd903b7eb184e241_bentPositionOS_1_Vector3;
UPG_FoliageContactPosition_float(_UseWPO_1f28bdd134c9432e812aded965021562_Out_0_Vector3, _UPGFoliageContactPositionCustomFunction_3446fb1a0c894696bd903b7eb184e241_bentPositionOS_1_Vector3);
description.Position = _UPGFoliageContactPositionCustomFunction_3446fb1a0c894696bd903b7eb184e241_bentPositionOS_1_Vector3;
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
float4 _Property_a9c1982af1c74efb9ac25b0326ae7979_Out_0_Vector4 = _Foliage_Color_Bottom;
float4 _Property_50eb1cb960084765a0771df7f40a6d5d_Out_0_Vector4 = _Foliage_Color_Top;
float _Split_1540f44067c24640b4968355fa26a295_R_1_Float = IN.WorldSpacePosition[0];
float _Split_1540f44067c24640b4968355fa26a295_G_2_Float = IN.WorldSpacePosition[1];
float _Split_1540f44067c24640b4968355fa26a295_B_3_Float = IN.WorldSpacePosition[2];
float _Split_1540f44067c24640b4968355fa26a295_A_4_Float = 0;
float3 _Vector3_54c605479a824dd3a62abb1919f39c57_Out_0_Vector3 = float3(float(0), float(0), float(0));
float3 _Transform_f3f68ad0eeb04ce9ac459f2859353c06_Out_1_Vector3;
_Transform_f3f68ad0eeb04ce9ac459f2859353c06_Out_1_Vector3 = TransformObjectToWorld(_Vector3_54c605479a824dd3a62abb1919f39c57_Out_0_Vector3.xyz);
float _Split_15d5002ae66545318962a44958be8e00_R_1_Float = _Transform_f3f68ad0eeb04ce9ac459f2859353c06_Out_1_Vector3[0];
float _Split_15d5002ae66545318962a44958be8e00_G_2_Float = _Transform_f3f68ad0eeb04ce9ac459f2859353c06_Out_1_Vector3[1];
float _Split_15d5002ae66545318962a44958be8e00_B_3_Float = _Transform_f3f68ad0eeb04ce9ac459f2859353c06_Out_1_Vector3[2];
float _Split_15d5002ae66545318962a44958be8e00_A_4_Float = 0;
float _Subtract_3131eba61d024496834f2933dbde6855_Out_2_Float;
Unity_Subtract_float(_Split_1540f44067c24640b4968355fa26a295_G_2_Float, _Split_15d5002ae66545318962a44958be8e00_G_2_Float, _Subtract_3131eba61d024496834f2933dbde6855_Out_2_Float);
float _Property_605b997dac5a4228881635945f995c72_Out_0_Float = _Tree_Height;
float _Divide_8e4152d2ed4943778a44dc39efa65611_Out_2_Float;
Unity_Divide_float(_Subtract_3131eba61d024496834f2933dbde6855_Out_2_Float, _Property_605b997dac5a4228881635945f995c72_Out_0_Float, _Divide_8e4152d2ed4943778a44dc39efa65611_Out_2_Float);
float _Saturate_086c13233c3d43fab6a33a1e2ed498c2_Out_1_Float;
Unity_Saturate_float(_Divide_8e4152d2ed4943778a44dc39efa65611_Out_2_Float, _Saturate_086c13233c3d43fab6a33a1e2ed498c2_Out_1_Float);
float _Property_0e5bff248b2341909121bb16666f3468_Out_0_Float = _Gradience_Power;
float _Power_be0c59cc158d492f99ea24f3b1001e1f_Out_2_Float;
Unity_Power_float(_Saturate_086c13233c3d43fab6a33a1e2ed498c2_Out_1_Float, _Property_0e5bff248b2341909121bb16666f3468_Out_0_Float, _Power_be0c59cc158d492f99ea24f3b1001e1f_Out_2_Float);
float _Saturate_26bbc2447b5a402c9562a9e5d012fd6c_Out_1_Float;
Unity_Saturate_float(_Power_be0c59cc158d492f99ea24f3b1001e1f_Out_2_Float, _Saturate_26bbc2447b5a402c9562a9e5d012fd6c_Out_1_Float);
float4 _Lerp_795c581446834fe0bee35971bc8f0c5c_Out_3_Vector4;
Unity_Lerp_float4(_Property_a9c1982af1c74efb9ac25b0326ae7979_Out_0_Vector4, _Property_50eb1cb960084765a0771df7f40a6d5d_Out_0_Vector4, (_Saturate_26bbc2447b5a402c9562a9e5d012fd6c_Out_1_Float.xxxx), _Lerp_795c581446834fe0bee35971bc8f0c5c_Out_3_Vector4);
float4 _Property_4036fdaeaa4647adb3dd84d70ff1161e_Out_0_Vector4 = _Foliage_Color_Top;
#if defined(_USE_HEIGHT_GRADIENCE)
float4 _UseHeightGradience_d4326b9500c14f11a9090b7e8f107721_Out_0_Vector4 = _Lerp_795c581446834fe0bee35971bc8f0c5c_Out_3_Vector4;
#else
float4 _UseHeightGradience_d4326b9500c14f11a9090b7e8f107721_Out_0_Vector4 = _Property_4036fdaeaa4647adb3dd84d70ff1161e_Out_0_Vector4;
#endif
UnityTexture2D _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Color_Map, sampler_Color_Map, _Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_eaea7ba5e1e74519a1bc7833f0103ab8_Out_0_Vector4 = IN.uv0;
float _Property_95352575ae4140008287fd58138e8eb5_Out_0_Float = _Color_Map_Tiling;
float4 _Multiply_86fafeae394745fe880655d03cafd9e1_Out_2_Vector4;
Unity_Multiply_float4_float4(_UV_eaea7ba5e1e74519a1bc7833f0103ab8_Out_0_Vector4, (_Property_95352575ae4140008287fd58138e8eb5_Out_0_Float.xxxx), _Multiply_86fafeae394745fe880655d03cafd9e1_Out_2_Vector4);
float4 _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.tex, _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.samplerstate, _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.GetTransformedUV((_Multiply_86fafeae394745fe880655d03cafd9e1_Out_2_Vector4.xy)) );
if (_Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4, _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_R_4_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.r;
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_G_5_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.g;
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_B_6_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.b;
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_A_7_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.a;
float4 _Multiply_a05088f8e3434d54920d012fc47555a7_Out_2_Vector4;
Unity_Multiply_float4_float4(_UseHeightGradience_d4326b9500c14f11a9090b7e8f107721_Out_0_Vector4, _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4, _Multiply_a05088f8e3434d54920d012fc47555a7_Out_2_Vector4);
float4 _Property_ee5875e8bc214caba4dbea1511d06cd2_Out_0_Vector4 = _Always_On_Fresnel_Color;
float _Property_be2ed593ceb345f89b57d123af3db203_Out_0_Float = _Always_On_Fresnel_Intensity;
float4 _Multiply_dcf6e90c046c4f72b77b81df5b922468_Out_2_Vector4;
Unity_Multiply_float4_float4(_Property_ee5875e8bc214caba4dbea1511d06cd2_Out_0_Vector4, (_Property_be2ed593ceb345f89b57d123af3db203_Out_0_Float.xxxx), _Multiply_dcf6e90c046c4f72b77b81df5b922468_Out_2_Vector4);
float4 _Saturate_26f2848ceb0a494094065a9387275032_Out_1_Vector4;
Unity_Saturate_float4(_Multiply_dcf6e90c046c4f72b77b81df5b922468_Out_2_Vector4, _Saturate_26f2848ceb0a494094065a9387275032_Out_1_Vector4);
float3 _Normalize_764cced5d0894f3d89f5911d50d5f4a2_Out_1_Vector3;
Unity_Normalize_float3(IN.WorldSpaceViewDirection, _Normalize_764cced5d0894f3d89f5911d50d5f4a2_Out_1_Vector3);
float _DotProduct_eb716c3ca301415688a52e76b310a65c_Out_2_Float;
Unity_DotProduct_float3(IN.WorldSpaceNormal, _Normalize_764cced5d0894f3d89f5911d50d5f4a2_Out_1_Vector3, _DotProduct_eb716c3ca301415688a52e76b310a65c_Out_2_Float);
float _Absolute_470cf4f0298a44a695f47aa5ea61c0ab_Out_1_Float;
Unity_Absolute_float(_DotProduct_eb716c3ca301415688a52e76b310a65c_Out_2_Float, _Absolute_470cf4f0298a44a695f47aa5ea61c0ab_Out_1_Float);
float _OneMinus_fa832b719fc24001be421507d395a592_Out_1_Float;
Unity_OneMinus_float(_Absolute_470cf4f0298a44a695f47aa5ea61c0ab_Out_1_Float, _OneMinus_fa832b719fc24001be421507d395a592_Out_1_Float);
float _Property_d9cfcacb90204bdfa36d0c1b7c0da452_Out_0_Float = _Always_On_Fresnel_Pow;
float _Power_7841b1ae85554473b96cce132c7ff7e1_Out_2_Float;
Unity_Power_float(_OneMinus_fa832b719fc24001be421507d395a592_Out_1_Float, _Property_d9cfcacb90204bdfa36d0c1b7c0da452_Out_0_Float, _Power_7841b1ae85554473b96cce132c7ff7e1_Out_2_Float);
float _Saturate_908f6307f948471ca95c0e7507cd7e43_Out_1_Float;
Unity_Saturate_float(_Power_7841b1ae85554473b96cce132c7ff7e1_Out_2_Float, _Saturate_908f6307f948471ca95c0e7507cd7e43_Out_1_Float);
float4 _Lerp_3406aa7c52b14c818ac9eb76bf068e2d_Out_3_Vector4;
Unity_Lerp_float4(_Multiply_a05088f8e3434d54920d012fc47555a7_Out_2_Vector4, _Saturate_26f2848ceb0a494094065a9387275032_Out_1_Vector4, (_Saturate_908f6307f948471ca95c0e7507cd7e43_Out_1_Float.xxxx), _Lerp_3406aa7c52b14c818ac9eb76bf068e2d_Out_3_Vector4);
float _Property_e8b3335851f84d91b28234460148cd0f_Out_0_Float = _Subsurface_Intensity;
float4 _Multiply_94df75e4ad734308a11d784250fc0b0c_Out_2_Vector4;
Unity_Multiply_float4_float4(_Lerp_3406aa7c52b14c818ac9eb76bf068e2d_Out_3_Vector4, (_Property_e8b3335851f84d91b28234460148cd0f_Out_0_Float.xxxx), _Multiply_94df75e4ad734308a11d784250fc0b0c_Out_2_Vector4);
float4 _Saturate_f0090edc5c664784a336737069276109_Out_1_Vector4;
Unity_Saturate_float4(_Multiply_94df75e4ad734308a11d784250fc0b0c_Out_2_Vector4, _Saturate_f0090edc5c664784a336737069276109_Out_1_Vector4);
float3 _MainLightCustomFunction_461af74b29b6456da89a2348455c2483_Direction_0_Vector3;
MainLight_float(_MainLightCustomFunction_461af74b29b6456da89a2348455c2483_Direction_0_Vector3);
float3 _Normalize_8644c278d597434b8871eb11a7a52cfe_Out_1_Vector3;
Unity_Normalize_float3(_MainLightCustomFunction_461af74b29b6456da89a2348455c2483_Direction_0_Vector3, _Normalize_8644c278d597434b8871eb11a7a52cfe_Out_1_Vector3);
float _Split_8a104625401f4a048b1160e17800fd11_R_1_Float = _Normalize_8644c278d597434b8871eb11a7a52cfe_Out_1_Vector3[0];
float _Split_8a104625401f4a048b1160e17800fd11_G_2_Float = _Normalize_8644c278d597434b8871eb11a7a52cfe_Out_1_Vector3[1];
float _Split_8a104625401f4a048b1160e17800fd11_B_3_Float = _Normalize_8644c278d597434b8871eb11a7a52cfe_Out_1_Vector3[2];
float _Split_8a104625401f4a048b1160e17800fd11_A_4_Float = 0;
float4 _Combine_87a90606ca4545b2983770732c0ed1b7_RGBA_4_Vector4;
float3 _Combine_87a90606ca4545b2983770732c0ed1b7_RGB_5_Vector3;
float2 _Combine_87a90606ca4545b2983770732c0ed1b7_RG_6_Vector2;
Unity_Combine_float(_Split_8a104625401f4a048b1160e17800fd11_R_1_Float, _Split_8a104625401f4a048b1160e17800fd11_B_3_Float, float(0), float(0), _Combine_87a90606ca4545b2983770732c0ed1b7_RGBA_4_Vector4, _Combine_87a90606ca4545b2983770732c0ed1b7_RGB_5_Vector3, _Combine_87a90606ca4545b2983770732c0ed1b7_RG_6_Vector2);
float3 _Normalize_f1ad7ea0af7b45ee88df143397ba6727_Out_1_Vector3;
Unity_Normalize_float3(-UNITY_MATRIX_V[2].xyz, _Normalize_f1ad7ea0af7b45ee88df143397ba6727_Out_1_Vector3);
float3 _Multiply_5b2d23749b2347b7af4472236c1dfe22_Out_2_Vector3;
Unity_Multiply_float3_float3(_Normalize_f1ad7ea0af7b45ee88df143397ba6727_Out_1_Vector3, float3(-1, -1, -1), _Multiply_5b2d23749b2347b7af4472236c1dfe22_Out_2_Vector3);
float _Split_6ebebe9410ea41bd8f8a327255848dc9_R_1_Float = _Multiply_5b2d23749b2347b7af4472236c1dfe22_Out_2_Vector3[0];
float _Split_6ebebe9410ea41bd8f8a327255848dc9_G_2_Float = _Multiply_5b2d23749b2347b7af4472236c1dfe22_Out_2_Vector3[1];
float _Split_6ebebe9410ea41bd8f8a327255848dc9_B_3_Float = _Multiply_5b2d23749b2347b7af4472236c1dfe22_Out_2_Vector3[2];
float _Split_6ebebe9410ea41bd8f8a327255848dc9_A_4_Float = 0;
float4 _Combine_ce9284087dd446409babf9c154f18fe4_RGBA_4_Vector4;
float3 _Combine_ce9284087dd446409babf9c154f18fe4_RGB_5_Vector3;
float2 _Combine_ce9284087dd446409babf9c154f18fe4_RG_6_Vector2;
Unity_Combine_float(_Split_6ebebe9410ea41bd8f8a327255848dc9_R_1_Float, _Split_6ebebe9410ea41bd8f8a327255848dc9_B_3_Float, float(0), float(0), _Combine_ce9284087dd446409babf9c154f18fe4_RGBA_4_Vector4, _Combine_ce9284087dd446409babf9c154f18fe4_RGB_5_Vector3, _Combine_ce9284087dd446409babf9c154f18fe4_RG_6_Vector2);
float _DotProduct_9e4f6525393a4171b6519e347a4b7a37_Out_2_Float;
Unity_DotProduct_float2(_Combine_87a90606ca4545b2983770732c0ed1b7_RG_6_Vector2, _Combine_ce9284087dd446409babf9c154f18fe4_RG_6_Vector2, _DotProduct_9e4f6525393a4171b6519e347a4b7a37_Out_2_Float);
float _Saturate_f76f8f4113e0468b9b20a6abf253d76e_Out_1_Float;
Unity_Saturate_float(_DotProduct_9e4f6525393a4171b6519e347a4b7a37_Out_2_Float, _Saturate_f76f8f4113e0468b9b20a6abf253d76e_Out_1_Float);
float _Property_bd14cfb26b074dbabde1a5a9b7f95d97_Out_0_Float = _Subsurface_Transition_Smoothness;
float _Power_2bc1d129029a4da69b2993ea07ff412f_Out_2_Float;
Unity_Power_float(_Saturate_f76f8f4113e0468b9b20a6abf253d76e_Out_1_Float, _Property_bd14cfb26b074dbabde1a5a9b7f95d97_Out_0_Float, _Power_2bc1d129029a4da69b2993ea07ff412f_Out_2_Float);
float _Saturate_44588f923e1b48ff97cd55ea2d69aa2b_Out_1_Float;
Unity_Saturate_float(_Power_2bc1d129029a4da69b2993ea07ff412f_Out_2_Float, _Saturate_44588f923e1b48ff97cd55ea2d69aa2b_Out_1_Float);
float4 _Lerp_98f83471835040419ec38e468d855fe3_Out_3_Vector4;
Unity_Lerp_float4(_Lerp_3406aa7c52b14c818ac9eb76bf068e2d_Out_3_Vector4, _Saturate_f0090edc5c664784a336737069276109_Out_1_Vector4, (_Saturate_44588f923e1b48ff97cd55ea2d69aa2b_Out_1_Float.xxxx), _Lerp_98f83471835040419ec38e468d855fe3_Out_3_Vector4);
float _Property_e00e00901e414a40a25484eb2c276584_Out_0_Float = _Subsurface_Fresnel_Intensity;
float4 _Multiply_6ed60680651f4e8cbff9e6f2ed97f0b9_Out_2_Vector4;
Unity_Multiply_float4_float4(_Lerp_98f83471835040419ec38e468d855fe3_Out_3_Vector4, (_Property_e00e00901e414a40a25484eb2c276584_Out_0_Float.xxxx), _Multiply_6ed60680651f4e8cbff9e6f2ed97f0b9_Out_2_Vector4);
float4 _Saturate_5cea81f1b9b5405fb4a571a97e57df52_Out_1_Vector4;
Unity_Saturate_float4(_Multiply_6ed60680651f4e8cbff9e6f2ed97f0b9_Out_2_Vector4, _Saturate_5cea81f1b9b5405fb4a571a97e57df52_Out_1_Vector4);
float _Saturate_10ad388197014e2fa2d0bd8261d82207_Out_1_Float;
Unity_Saturate_float(_DotProduct_9e4f6525393a4171b6519e347a4b7a37_Out_2_Float, _Saturate_10ad388197014e2fa2d0bd8261d82207_Out_1_Float);
float _Property_fe3bfd62823b45bf868d2c88982c1f64_Out_0_Float = _Subsurface_Fresnel_Transition_Smoothness;
float _Power_1ad124a5d5b74c13848df4f8f171d448_Out_2_Float;
Unity_Power_float(_Saturate_10ad388197014e2fa2d0bd8261d82207_Out_1_Float, _Property_fe3bfd62823b45bf868d2c88982c1f64_Out_0_Float, _Power_1ad124a5d5b74c13848df4f8f171d448_Out_2_Float);
float3 _Normalize_7e92e50b98274c508fa061a32d7a99b7_Out_1_Vector3;
Unity_Normalize_float3(IN.WorldSpaceViewDirection, _Normalize_7e92e50b98274c508fa061a32d7a99b7_Out_1_Vector3);
float _DotProduct_3170aef4b99740d083af85a5aa294edd_Out_2_Float;
Unity_DotProduct_float3(IN.WorldSpaceNormal, _Normalize_7e92e50b98274c508fa061a32d7a99b7_Out_1_Vector3, _DotProduct_3170aef4b99740d083af85a5aa294edd_Out_2_Float);
float _Absolute_111753c15e794f4b8e885c2c19d93d82_Out_1_Float;
Unity_Absolute_float(_DotProduct_3170aef4b99740d083af85a5aa294edd_Out_2_Float, _Absolute_111753c15e794f4b8e885c2c19d93d82_Out_1_Float);
float _OneMinus_2a02f0d378e1454c9aeda0ff447d4d86_Out_1_Float;
Unity_OneMinus_float(_Absolute_111753c15e794f4b8e885c2c19d93d82_Out_1_Float, _OneMinus_2a02f0d378e1454c9aeda0ff447d4d86_Out_1_Float);
float _Property_49c0203b77734a36b9ba06f7bcd723ee_Out_0_Float = _Subsurface_Fresnel_Pow;
float _Power_71121b48d5934b11a14aeadcad656816_Out_2_Float;
Unity_Power_float(_OneMinus_2a02f0d378e1454c9aeda0ff447d4d86_Out_1_Float, _Property_49c0203b77734a36b9ba06f7bcd723ee_Out_0_Float, _Power_71121b48d5934b11a14aeadcad656816_Out_2_Float);
float _Saturate_29e69580558f40c49a883332cdf25357_Out_1_Float;
Unity_Saturate_float(_Power_71121b48d5934b11a14aeadcad656816_Out_2_Float, _Saturate_29e69580558f40c49a883332cdf25357_Out_1_Float);
float _Multiply_328c604c9492493ba1dc1999ef6314fd_Out_2_Float;
Unity_Multiply_float_float(_Power_1ad124a5d5b74c13848df4f8f171d448_Out_2_Float, _Saturate_29e69580558f40c49a883332cdf25357_Out_1_Float, _Multiply_328c604c9492493ba1dc1999ef6314fd_Out_2_Float);
float4 _Lerp_10a8be366b974da7b00eab9c55f88a8a_Out_3_Vector4;
Unity_Lerp_float4(_Lerp_98f83471835040419ec38e468d855fe3_Out_3_Vector4, _Saturate_5cea81f1b9b5405fb4a571a97e57df52_Out_1_Vector4, (_Multiply_328c604c9492493ba1dc1999ef6314fd_Out_2_Float.xxxx), _Lerp_10a8be366b974da7b00eab9c55f88a8a_Out_3_Vector4);
float _Property_e5d876e9ad8e4cf1ab7db7d850c55d8c_Out_0_Float = _Roughness;
float _OneMinus_200145be127f401eae78bc05b8a1fc88_Out_1_Float;
Unity_OneMinus_float(_Property_e5d876e9ad8e4cf1ab7db7d850c55d8c_Out_0_Float, _OneMinus_200145be127f401eae78bc05b8a1fc88_Out_1_Float);
float _Multiply_cf1ea983d11e4ab7b4bdbbb064ad94b5_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_A_7_Float, 1, _Multiply_cf1ea983d11e4ab7b4bdbbb064ad94b5_Out_2_Float);
float _Property_99bb0b22edbb4ea0b1b7844414b876a7_Out_0_Float = _Alpha_Clip;
surface.BaseColor = (_Lerp_10a8be366b974da7b00eab9c55f88a8a_Out_3_Vector4.xyz);
surface.NormalTS = IN.TangentSpaceNormal;
surface.Emission = float3(0, 0, 0);
surface.Metallic = float(0);
surface.Smoothness = _OneMinus_200145be127f401eae78bc05b8a1fc88_Out_1_Float;
surface.Occlusion = float(1);
surface.Alpha = _Multiply_cf1ea983d11e4ab7b4bdbbb064ad94b5_Out_2_Float;
surface.AlphaClipThreshold = _Property_99bb0b22edbb4ea0b1b7844414b876a7_Out_0_Float;
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

    

    // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
    float3 unnormalizedNormalWS = input.normalWS;
    const float renormFactor = 1.0 / length(unnormalizedNormalWS);


    output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph
    output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);


    output.WorldSpaceViewDirection = GetWorldSpaceNormalizeViewDir(input.positionWS);
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
#pragma shader_feature_local _ _USE_WPO
#pragma shader_feature_local _ _USE_HEIGHT_GRADIENCE

#if defined(_USE_WPO) && defined(_USE_HEIGHT_GRADIENCE)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_WPO)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_HEIGHT_GRADIENCE)
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
#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_NORMAL_WS
#define VARYINGS_NEED_TANGENT_WS
#define VARYINGS_NEED_TEXCOORD0
#define VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_FORWARD_ADD
#define BUILTIN_TARGET_API 1
#define _BUILTIN_AlphaClip 1
#define _BUILTIN_ALPHATEST_ON 1
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
 float3 WorldSpaceNormal;
 float3 TangentSpaceNormal;
 float3 WorldSpaceViewDirection;
 float3 WorldSpacePosition;
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 WorldSpacePosition;
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

float4 _Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D_TexelSize;
float _Subsurface_Fresnel_Transition_Smoothness;
float _Subsurface_Fresnel_Pow;
float _Subsurface_Fresnel_Intensity;
float _Subsurface_Transition_Smoothness;
float _Wiggle_Wind_Speed_Large;
float _Wiggle_Wind_Intensity;
float _Roughness;
float4 _Foliage_Color_Bottom;
float _Sway_Wind_Speed;
float _Gradience_Power;
float _Fresnel_Mul;
float4 _Foliage_Color_Top;
float _Always_On_Fresnel_Intensity;
float _Tree_Height;
float4 _Color_Map_TexelSize;
float _Height_Weight;
float _Height_Gradience;
float _Sway_Wind_Time_Random_Offset_Amount;
float _Sway_Wind_Intensity;
float3 _Sway_Wind_Direction;
float _Color_Map_Tiling;
float _Subsurface_Intensity;
float _Alpha_Clip;
float _Wiggle_Wind_Size;
float _Wiggle_Wind_Speed_Small;
float _Fresnel_Pow;
float _Always_On_Fresnel_Pow;
float4 _Always_On_Fresnel_Color;
float _ree;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D);
SAMPLER(sampler_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D);
TEXTURE2D(_Color_Map);
SAMPLER(sampler_Color_Map);

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

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Saturate_float4(float4 In, out float4 Out)
{
    Out = saturate(In);
}

void Unity_Normalize_float3(float3 In, out float3 Out)
{
    Out = normalize(In);
}

void Unity_DotProduct_float3(float3 A, float3 B, out float Out)
{
    Out = dot(A, B);
}

void Unity_Absolute_float(float In, out float Out)
{
    Out = abs(In);
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
}

// unity-custom-func-begin
void MainLight_float(out float3 Direction){

#ifdef UNITY_SHADER_GRAPH
	Direction = _WorldSpaceLightPos0.xyz;
#else
	Direction = float3(1,1,1);
#endif
}
// unity-custom-func-end

void Unity_DotProduct_float2(float2 A, float2 B, out float Out)
{
    Out = dot(A, B);
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
float _Property_7689f3a0523b4af9912879a05d1d5206_Out_0_Float = _Sway_Wind_Speed;
float3 _Property_e0531013c46845b6b036ac453118c744_Out_0_Vector3 = _Sway_Wind_Direction;
float _Property_35588e6a16b5457695dba39b1644d669_Out_0_Float = _Sway_Wind_Intensity;
float _Property_c8d3d7e6e6f64d409f010d988b96e327_Out_0_Float = _Sway_Wind_Time_Random_Offset_Amount;
Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float _WindSway_662bcd4f8b2b4607b138cb44de00a708;
_WindSway_662bcd4f8b2b4607b138cb44de00a708.ObjectSpacePosition = IN.ObjectSpacePosition;
_WindSway_662bcd4f8b2b4607b138cb44de00a708.TimeParameters = IN.TimeParameters;
float3 _WindSway_662bcd4f8b2b4607b138cb44de00a708_OutVector3_1_Vector3;
SG_WindSway_51c4027ae50a8cd49be53c4990b9f179_float(_Property_7689f3a0523b4af9912879a05d1d5206_Out_0_Float, _Property_e0531013c46845b6b036ac453118c744_Out_0_Vector3, _Property_35588e6a16b5457695dba39b1644d669_Out_0_Float, _Property_c8d3d7e6e6f64d409f010d988b96e327_Out_0_Float, _WindSway_662bcd4f8b2b4607b138cb44de00a708, _WindSway_662bcd4f8b2b4607b138cb44de00a708_OutVector3_1_Vector3);
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_R_1_Float = IN.WorldSpacePosition[0];
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_G_2_Float = IN.WorldSpacePosition[1];
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_B_3_Float = IN.WorldSpacePosition[2];
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_A_4_Float = 0;
float3 _Vector3_e154b524054546d2bbeab293c0e20519_Out_0_Vector3 = float3(float(0), float(0), float(0));
float3 _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3;
_Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3 = TransformObjectToWorld(_Vector3_e154b524054546d2bbeab293c0e20519_Out_0_Vector3.xyz);
float _Split_5eb48153bba44ea2b128583819f09a11_R_1_Float = _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3[0];
float _Split_5eb48153bba44ea2b128583819f09a11_G_2_Float = _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3[1];
float _Split_5eb48153bba44ea2b128583819f09a11_B_3_Float = _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3[2];
float _Split_5eb48153bba44ea2b128583819f09a11_A_4_Float = 0;
float _Subtract_d6a99abdf4e74644acffc1718d1735bf_Out_2_Float;
Unity_Subtract_float(_Split_8fe962eb6f0f4d6fba734f9edcaf9067_G_2_Float, _Split_5eb48153bba44ea2b128583819f09a11_G_2_Float, _Subtract_d6a99abdf4e74644acffc1718d1735bf_Out_2_Float);
float _Property_1cf3cda52e094e638df491b003607dfd_Out_0_Float = _Height_Weight;
float _Split_145cf2161d4c4510b08248cad819f655_R_1_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[0];
float _Split_145cf2161d4c4510b08248cad819f655_G_2_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[1];
float _Split_145cf2161d4c4510b08248cad819f655_B_3_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[2];
float _Split_145cf2161d4c4510b08248cad819f655_A_4_Float = 0;
float _Multiply_2c140d573b9447aab058bd863fe2f4f7_Out_2_Float;
Unity_Multiply_float_float(_Property_1cf3cda52e094e638df491b003607dfd_Out_0_Float, _Split_145cf2161d4c4510b08248cad819f655_G_2_Float, _Multiply_2c140d573b9447aab058bd863fe2f4f7_Out_2_Float);
float _Divide_0f6a637143d841d0b1c94fa2ae3e164d_Out_2_Float;
Unity_Divide_float(_Subtract_d6a99abdf4e74644acffc1718d1735bf_Out_2_Float, _Multiply_2c140d573b9447aab058bd863fe2f4f7_Out_2_Float, _Divide_0f6a637143d841d0b1c94fa2ae3e164d_Out_2_Float);
float _Saturate_1e4e29fb5c074565a1f941c046957b05_Out_1_Float;
Unity_Saturate_float(_Divide_0f6a637143d841d0b1c94fa2ae3e164d_Out_2_Float, _Saturate_1e4e29fb5c074565a1f941c046957b05_Out_1_Float);
float _Property_3d64a0c8dd104507b9a0762a778fe585_Out_0_Float = _Height_Gradience;
float _Power_f1d74728398a45f9993fe3b3affb6584_Out_2_Float;
Unity_Power_float(_Saturate_1e4e29fb5c074565a1f941c046957b05_Out_1_Float, _Property_3d64a0c8dd104507b9a0762a778fe585_Out_0_Float, _Power_f1d74728398a45f9993fe3b3affb6584_Out_2_Float);
float _Saturate_57b130350452418fa1d09d4a0ddd64ac_Out_1_Float;
Unity_Saturate_float(_Power_f1d74728398a45f9993fe3b3affb6584_Out_2_Float, _Saturate_57b130350452418fa1d09d4a0ddd64ac_Out_1_Float);
float3 _Lerp_1fbf8d0a7f5a4e2ab381967ac90385c7_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _WindSway_662bcd4f8b2b4607b138cb44de00a708_OutVector3_1_Vector3, (_Saturate_57b130350452418fa1d09d4a0ddd64ac_Out_1_Float.xxx), _Lerp_1fbf8d0a7f5a4e2ab381967ac90385c7_Out_3_Vector3);
float _Split_bd04916788224fa2bb71be3f27eb839a_R_1_Float = IN.WorldSpacePosition[0];
float _Split_bd04916788224fa2bb71be3f27eb839a_G_2_Float = IN.WorldSpacePosition[1];
float _Split_bd04916788224fa2bb71be3f27eb839a_B_3_Float = IN.WorldSpacePosition[2];
float _Split_bd04916788224fa2bb71be3f27eb839a_A_4_Float = 0;
float4 _Combine_45cdab9c20414d03862100967f56c78d_RGBA_4_Vector4;
float3 _Combine_45cdab9c20414d03862100967f56c78d_RGB_5_Vector3;
float2 _Combine_45cdab9c20414d03862100967f56c78d_RG_6_Vector2;
Unity_Combine_float(_Split_bd04916788224fa2bb71be3f27eb839a_R_1_Float, _Split_bd04916788224fa2bb71be3f27eb839a_B_3_Float, float(0), float(0), _Combine_45cdab9c20414d03862100967f56c78d_RGBA_4_Vector4, _Combine_45cdab9c20414d03862100967f56c78d_RGB_5_Vector3, _Combine_45cdab9c20414d03862100967f56c78d_RG_6_Vector2);
float _Property_9b35f6244c0247c7a9be7af2671f58e7_Out_0_Float = _Wiggle_Wind_Size;
float2 _Multiply_ba4d7f376f904143acc9eb33d8414ff8_Out_2_Vector2;
Unity_Multiply_float2_float2(_Combine_45cdab9c20414d03862100967f56c78d_RG_6_Vector2, (_Property_9b35f6244c0247c7a9be7af2671f58e7_Out_0_Float.xx), _Multiply_ba4d7f376f904143acc9eb33d8414ff8_Out_2_Vector2);
float _Property_6eae25110c184e7a94293b95ddc9ecef_Out_0_Float = _Wiggle_Wind_Speed_Small;
float _Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Property_6eae25110c184e7a94293b95ddc9ecef_Out_0_Float, _Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float);
float _Multiply_7510d5a95e6c4d3986bb83059d23d2fa_Out_2_Float;
Unity_Multiply_float_float(_Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float, 0.8, _Multiply_7510d5a95e6c4d3986bb83059d23d2fa_Out_2_Float);
float2 _Vector2_cb26bc8c681b455c81073f0dc3c8f6b4_Out_0_Vector2 = float2(_Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float, _Multiply_7510d5a95e6c4d3986bb83059d23d2fa_Out_2_Float);
float2 _TilingAndOffset_e5f47c2cf9d846859a72aadcdac7fc3b_Out_3_Vector2;
Unity_TilingAndOffset_float(_Multiply_ba4d7f376f904143acc9eb33d8414ff8_Out_2_Vector2, float2 (1, 1), _Vector2_cb26bc8c681b455c81073f0dc3c8f6b4_Out_0_Vector2, _TilingAndOffset_e5f47c2cf9d846859a72aadcdac7fc3b_Out_3_Vector2);
  float4 _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).tex, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).GetTransformedUV(_TilingAndOffset_e5f47c2cf9d846859a72aadcdac7fc3b_Out_3_Vector2), float(2));
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_R_5_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_G_6_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_B_7_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_A_8_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.a;
float _Float_3f1a7b5619414946b13e7cef19336d45_Out_0_Float = float(0.3);
float _Subtract_3105ef040f344481afb342cd9ef65e9f_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_R_5_Float, _Float_3f1a7b5619414946b13e7cef19336d45_Out_0_Float, _Subtract_3105ef040f344481afb342cd9ef65e9f_Out_2_Float);
float _Split_18495110217342bc80044d9345d98fbd_R_1_Float = IN.WorldSpacePosition[0];
float _Split_18495110217342bc80044d9345d98fbd_G_2_Float = IN.WorldSpacePosition[1];
float _Split_18495110217342bc80044d9345d98fbd_B_3_Float = IN.WorldSpacePosition[2];
float _Split_18495110217342bc80044d9345d98fbd_A_4_Float = 0;
float4 _Combine_d1ee3e49696840daa7cfd43df2287807_RGBA_4_Vector4;
float3 _Combine_d1ee3e49696840daa7cfd43df2287807_RGB_5_Vector3;
float2 _Combine_d1ee3e49696840daa7cfd43df2287807_RG_6_Vector2;
Unity_Combine_float(_Split_18495110217342bc80044d9345d98fbd_R_1_Float, _Split_18495110217342bc80044d9345d98fbd_G_2_Float, float(0), float(0), _Combine_d1ee3e49696840daa7cfd43df2287807_RGBA_4_Vector4, _Combine_d1ee3e49696840daa7cfd43df2287807_RGB_5_Vector3, _Combine_d1ee3e49696840daa7cfd43df2287807_RG_6_Vector2);
float _Property_9efe7c2851414ea39b6971daac6143e5_Out_0_Float = _Wiggle_Wind_Size;
float _Multiply_4f6b361114004341aeae12987f071375_Out_2_Float;
Unity_Multiply_float_float(_Property_9efe7c2851414ea39b6971daac6143e5_Out_0_Float, 0.763, _Multiply_4f6b361114004341aeae12987f071375_Out_2_Float);
float2 _Multiply_0d88e33790f0458b9e6e7f49946e6344_Out_2_Vector2;
Unity_Multiply_float2_float2(_Combine_d1ee3e49696840daa7cfd43df2287807_RG_6_Vector2, (_Multiply_4f6b361114004341aeae12987f071375_Out_2_Float.xx), _Multiply_0d88e33790f0458b9e6e7f49946e6344_Out_2_Vector2);
float _Property_56c994a79361438d88ea30b1e3ccf2aa_Out_0_Float = _Wiggle_Wind_Speed_Large;
float _Multiply_c5e5feed518642678424c6f0d45b4377_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Property_56c994a79361438d88ea30b1e3ccf2aa_Out_0_Float, _Multiply_c5e5feed518642678424c6f0d45b4377_Out_2_Float);
float2 _TilingAndOffset_7dac505987c144edad083d13ebca67e6_Out_3_Vector2;
Unity_TilingAndOffset_float(_Multiply_0d88e33790f0458b9e6e7f49946e6344_Out_2_Vector2, float2 (1, 1), (_Multiply_c5e5feed518642678424c6f0d45b4377_Out_2_Float.xx), _TilingAndOffset_7dac505987c144edad083d13ebca67e6_Out_3_Vector2);
  float4 _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).tex, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).GetTransformedUV(_TilingAndOffset_7dac505987c144edad083d13ebca67e6_Out_3_Vector2), float(2));
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_R_5_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_G_6_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_B_7_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_A_8_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.a;
float _Subtract_e29fd5b0daea41a1a38ac52280e6db88_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_G_6_Float, _Float_3f1a7b5619414946b13e7cef19336d45_Out_0_Float, _Subtract_e29fd5b0daea41a1a38ac52280e6db88_Out_2_Float);
float _Add_6d51be6104ee4e23a093dfb9c3d993f7_Out_2_Float;
Unity_Add_float(_Subtract_3105ef040f344481afb342cd9ef65e9f_Out_2_Float, _Subtract_e29fd5b0daea41a1a38ac52280e6db88_Out_2_Float, _Add_6d51be6104ee4e23a093dfb9c3d993f7_Out_2_Float);
float4 _Multiply_db043a8b3fc34edb8bee433085c7e061_Out_2_Vector4;
Unity_Multiply_float4_float4((_Add_6d51be6104ee4e23a093dfb9c3d993f7_Out_2_Float.xxxx), IN.VertexColor, _Multiply_db043a8b3fc34edb8bee433085c7e061_Out_2_Vector4);
float _Property_46c8c648052246a4a32e80905ed7e9df_Out_0_Float = _Wiggle_Wind_Intensity;
float4 _Multiply_4baa80310c414483a2aada72baa7a883_Out_2_Vector4;
Unity_Multiply_float4_float4(_Multiply_db043a8b3fc34edb8bee433085c7e061_Out_2_Vector4, (_Property_46c8c648052246a4a32e80905ed7e9df_Out_0_Float.xxxx), _Multiply_4baa80310c414483a2aada72baa7a883_Out_2_Vector4);
float3 _Multiply_2c95a93020c44a20be82a31b985c35b8_Out_2_Vector3;
Unity_Multiply_float3_float3((_Multiply_4baa80310c414483a2aada72baa7a883_Out_2_Vector4.xyz), IN.ObjectSpaceNormal, _Multiply_2c95a93020c44a20be82a31b985c35b8_Out_2_Vector3);
float3 _Add_5a143eeddea74bceb3ddd5b059f49da5_Out_2_Vector3;
Unity_Add_float3(_Lerp_1fbf8d0a7f5a4e2ab381967ac90385c7_Out_3_Vector3, _Multiply_2c95a93020c44a20be82a31b985c35b8_Out_2_Vector3, _Add_5a143eeddea74bceb3ddd5b059f49da5_Out_2_Vector3);
#if defined(_USE_WPO)
float3 _UseWPO_1f28bdd134c9432e812aded965021562_Out_0_Vector3 = _Add_5a143eeddea74bceb3ddd5b059f49da5_Out_2_Vector3;
#else
float3 _UseWPO_1f28bdd134c9432e812aded965021562_Out_0_Vector3 = IN.ObjectSpacePosition;
#endif
float3 _UPGFoliageContactPositionCustomFunction_3446fb1a0c894696bd903b7eb184e241_bentPositionOS_1_Vector3;
UPG_FoliageContactPosition_float(_UseWPO_1f28bdd134c9432e812aded965021562_Out_0_Vector3, _UPGFoliageContactPositionCustomFunction_3446fb1a0c894696bd903b7eb184e241_bentPositionOS_1_Vector3);
description.Position = _UPGFoliageContactPositionCustomFunction_3446fb1a0c894696bd903b7eb184e241_bentPositionOS_1_Vector3;
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
float4 _Property_a9c1982af1c74efb9ac25b0326ae7979_Out_0_Vector4 = _Foliage_Color_Bottom;
float4 _Property_50eb1cb960084765a0771df7f40a6d5d_Out_0_Vector4 = _Foliage_Color_Top;
float _Split_1540f44067c24640b4968355fa26a295_R_1_Float = IN.WorldSpacePosition[0];
float _Split_1540f44067c24640b4968355fa26a295_G_2_Float = IN.WorldSpacePosition[1];
float _Split_1540f44067c24640b4968355fa26a295_B_3_Float = IN.WorldSpacePosition[2];
float _Split_1540f44067c24640b4968355fa26a295_A_4_Float = 0;
float3 _Vector3_54c605479a824dd3a62abb1919f39c57_Out_0_Vector3 = float3(float(0), float(0), float(0));
float3 _Transform_f3f68ad0eeb04ce9ac459f2859353c06_Out_1_Vector3;
_Transform_f3f68ad0eeb04ce9ac459f2859353c06_Out_1_Vector3 = TransformObjectToWorld(_Vector3_54c605479a824dd3a62abb1919f39c57_Out_0_Vector3.xyz);
float _Split_15d5002ae66545318962a44958be8e00_R_1_Float = _Transform_f3f68ad0eeb04ce9ac459f2859353c06_Out_1_Vector3[0];
float _Split_15d5002ae66545318962a44958be8e00_G_2_Float = _Transform_f3f68ad0eeb04ce9ac459f2859353c06_Out_1_Vector3[1];
float _Split_15d5002ae66545318962a44958be8e00_B_3_Float = _Transform_f3f68ad0eeb04ce9ac459f2859353c06_Out_1_Vector3[2];
float _Split_15d5002ae66545318962a44958be8e00_A_4_Float = 0;
float _Subtract_3131eba61d024496834f2933dbde6855_Out_2_Float;
Unity_Subtract_float(_Split_1540f44067c24640b4968355fa26a295_G_2_Float, _Split_15d5002ae66545318962a44958be8e00_G_2_Float, _Subtract_3131eba61d024496834f2933dbde6855_Out_2_Float);
float _Property_605b997dac5a4228881635945f995c72_Out_0_Float = _Tree_Height;
float _Divide_8e4152d2ed4943778a44dc39efa65611_Out_2_Float;
Unity_Divide_float(_Subtract_3131eba61d024496834f2933dbde6855_Out_2_Float, _Property_605b997dac5a4228881635945f995c72_Out_0_Float, _Divide_8e4152d2ed4943778a44dc39efa65611_Out_2_Float);
float _Saturate_086c13233c3d43fab6a33a1e2ed498c2_Out_1_Float;
Unity_Saturate_float(_Divide_8e4152d2ed4943778a44dc39efa65611_Out_2_Float, _Saturate_086c13233c3d43fab6a33a1e2ed498c2_Out_1_Float);
float _Property_0e5bff248b2341909121bb16666f3468_Out_0_Float = _Gradience_Power;
float _Power_be0c59cc158d492f99ea24f3b1001e1f_Out_2_Float;
Unity_Power_float(_Saturate_086c13233c3d43fab6a33a1e2ed498c2_Out_1_Float, _Property_0e5bff248b2341909121bb16666f3468_Out_0_Float, _Power_be0c59cc158d492f99ea24f3b1001e1f_Out_2_Float);
float _Saturate_26bbc2447b5a402c9562a9e5d012fd6c_Out_1_Float;
Unity_Saturate_float(_Power_be0c59cc158d492f99ea24f3b1001e1f_Out_2_Float, _Saturate_26bbc2447b5a402c9562a9e5d012fd6c_Out_1_Float);
float4 _Lerp_795c581446834fe0bee35971bc8f0c5c_Out_3_Vector4;
Unity_Lerp_float4(_Property_a9c1982af1c74efb9ac25b0326ae7979_Out_0_Vector4, _Property_50eb1cb960084765a0771df7f40a6d5d_Out_0_Vector4, (_Saturate_26bbc2447b5a402c9562a9e5d012fd6c_Out_1_Float.xxxx), _Lerp_795c581446834fe0bee35971bc8f0c5c_Out_3_Vector4);
float4 _Property_4036fdaeaa4647adb3dd84d70ff1161e_Out_0_Vector4 = _Foliage_Color_Top;
#if defined(_USE_HEIGHT_GRADIENCE)
float4 _UseHeightGradience_d4326b9500c14f11a9090b7e8f107721_Out_0_Vector4 = _Lerp_795c581446834fe0bee35971bc8f0c5c_Out_3_Vector4;
#else
float4 _UseHeightGradience_d4326b9500c14f11a9090b7e8f107721_Out_0_Vector4 = _Property_4036fdaeaa4647adb3dd84d70ff1161e_Out_0_Vector4;
#endif
UnityTexture2D _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Color_Map, sampler_Color_Map, _Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_eaea7ba5e1e74519a1bc7833f0103ab8_Out_0_Vector4 = IN.uv0;
float _Property_95352575ae4140008287fd58138e8eb5_Out_0_Float = _Color_Map_Tiling;
float4 _Multiply_86fafeae394745fe880655d03cafd9e1_Out_2_Vector4;
Unity_Multiply_float4_float4(_UV_eaea7ba5e1e74519a1bc7833f0103ab8_Out_0_Vector4, (_Property_95352575ae4140008287fd58138e8eb5_Out_0_Float.xxxx), _Multiply_86fafeae394745fe880655d03cafd9e1_Out_2_Vector4);
float4 _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.tex, _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.samplerstate, _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.GetTransformedUV((_Multiply_86fafeae394745fe880655d03cafd9e1_Out_2_Vector4.xy)) );
if (_Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4, _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_R_4_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.r;
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_G_5_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.g;
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_B_6_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.b;
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_A_7_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.a;
float4 _Multiply_a05088f8e3434d54920d012fc47555a7_Out_2_Vector4;
Unity_Multiply_float4_float4(_UseHeightGradience_d4326b9500c14f11a9090b7e8f107721_Out_0_Vector4, _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4, _Multiply_a05088f8e3434d54920d012fc47555a7_Out_2_Vector4);
float4 _Property_ee5875e8bc214caba4dbea1511d06cd2_Out_0_Vector4 = _Always_On_Fresnel_Color;
float _Property_be2ed593ceb345f89b57d123af3db203_Out_0_Float = _Always_On_Fresnel_Intensity;
float4 _Multiply_dcf6e90c046c4f72b77b81df5b922468_Out_2_Vector4;
Unity_Multiply_float4_float4(_Property_ee5875e8bc214caba4dbea1511d06cd2_Out_0_Vector4, (_Property_be2ed593ceb345f89b57d123af3db203_Out_0_Float.xxxx), _Multiply_dcf6e90c046c4f72b77b81df5b922468_Out_2_Vector4);
float4 _Saturate_26f2848ceb0a494094065a9387275032_Out_1_Vector4;
Unity_Saturate_float4(_Multiply_dcf6e90c046c4f72b77b81df5b922468_Out_2_Vector4, _Saturate_26f2848ceb0a494094065a9387275032_Out_1_Vector4);
float3 _Normalize_764cced5d0894f3d89f5911d50d5f4a2_Out_1_Vector3;
Unity_Normalize_float3(IN.WorldSpaceViewDirection, _Normalize_764cced5d0894f3d89f5911d50d5f4a2_Out_1_Vector3);
float _DotProduct_eb716c3ca301415688a52e76b310a65c_Out_2_Float;
Unity_DotProduct_float3(IN.WorldSpaceNormal, _Normalize_764cced5d0894f3d89f5911d50d5f4a2_Out_1_Vector3, _DotProduct_eb716c3ca301415688a52e76b310a65c_Out_2_Float);
float _Absolute_470cf4f0298a44a695f47aa5ea61c0ab_Out_1_Float;
Unity_Absolute_float(_DotProduct_eb716c3ca301415688a52e76b310a65c_Out_2_Float, _Absolute_470cf4f0298a44a695f47aa5ea61c0ab_Out_1_Float);
float _OneMinus_fa832b719fc24001be421507d395a592_Out_1_Float;
Unity_OneMinus_float(_Absolute_470cf4f0298a44a695f47aa5ea61c0ab_Out_1_Float, _OneMinus_fa832b719fc24001be421507d395a592_Out_1_Float);
float _Property_d9cfcacb90204bdfa36d0c1b7c0da452_Out_0_Float = _Always_On_Fresnel_Pow;
float _Power_7841b1ae85554473b96cce132c7ff7e1_Out_2_Float;
Unity_Power_float(_OneMinus_fa832b719fc24001be421507d395a592_Out_1_Float, _Property_d9cfcacb90204bdfa36d0c1b7c0da452_Out_0_Float, _Power_7841b1ae85554473b96cce132c7ff7e1_Out_2_Float);
float _Saturate_908f6307f948471ca95c0e7507cd7e43_Out_1_Float;
Unity_Saturate_float(_Power_7841b1ae85554473b96cce132c7ff7e1_Out_2_Float, _Saturate_908f6307f948471ca95c0e7507cd7e43_Out_1_Float);
float4 _Lerp_3406aa7c52b14c818ac9eb76bf068e2d_Out_3_Vector4;
Unity_Lerp_float4(_Multiply_a05088f8e3434d54920d012fc47555a7_Out_2_Vector4, _Saturate_26f2848ceb0a494094065a9387275032_Out_1_Vector4, (_Saturate_908f6307f948471ca95c0e7507cd7e43_Out_1_Float.xxxx), _Lerp_3406aa7c52b14c818ac9eb76bf068e2d_Out_3_Vector4);
float _Property_e8b3335851f84d91b28234460148cd0f_Out_0_Float = _Subsurface_Intensity;
float4 _Multiply_94df75e4ad734308a11d784250fc0b0c_Out_2_Vector4;
Unity_Multiply_float4_float4(_Lerp_3406aa7c52b14c818ac9eb76bf068e2d_Out_3_Vector4, (_Property_e8b3335851f84d91b28234460148cd0f_Out_0_Float.xxxx), _Multiply_94df75e4ad734308a11d784250fc0b0c_Out_2_Vector4);
float4 _Saturate_f0090edc5c664784a336737069276109_Out_1_Vector4;
Unity_Saturate_float4(_Multiply_94df75e4ad734308a11d784250fc0b0c_Out_2_Vector4, _Saturate_f0090edc5c664784a336737069276109_Out_1_Vector4);
float3 _MainLightCustomFunction_461af74b29b6456da89a2348455c2483_Direction_0_Vector3;
MainLight_float(_MainLightCustomFunction_461af74b29b6456da89a2348455c2483_Direction_0_Vector3);
float3 _Normalize_8644c278d597434b8871eb11a7a52cfe_Out_1_Vector3;
Unity_Normalize_float3(_MainLightCustomFunction_461af74b29b6456da89a2348455c2483_Direction_0_Vector3, _Normalize_8644c278d597434b8871eb11a7a52cfe_Out_1_Vector3);
float _Split_8a104625401f4a048b1160e17800fd11_R_1_Float = _Normalize_8644c278d597434b8871eb11a7a52cfe_Out_1_Vector3[0];
float _Split_8a104625401f4a048b1160e17800fd11_G_2_Float = _Normalize_8644c278d597434b8871eb11a7a52cfe_Out_1_Vector3[1];
float _Split_8a104625401f4a048b1160e17800fd11_B_3_Float = _Normalize_8644c278d597434b8871eb11a7a52cfe_Out_1_Vector3[2];
float _Split_8a104625401f4a048b1160e17800fd11_A_4_Float = 0;
float4 _Combine_87a90606ca4545b2983770732c0ed1b7_RGBA_4_Vector4;
float3 _Combine_87a90606ca4545b2983770732c0ed1b7_RGB_5_Vector3;
float2 _Combine_87a90606ca4545b2983770732c0ed1b7_RG_6_Vector2;
Unity_Combine_float(_Split_8a104625401f4a048b1160e17800fd11_R_1_Float, _Split_8a104625401f4a048b1160e17800fd11_B_3_Float, float(0), float(0), _Combine_87a90606ca4545b2983770732c0ed1b7_RGBA_4_Vector4, _Combine_87a90606ca4545b2983770732c0ed1b7_RGB_5_Vector3, _Combine_87a90606ca4545b2983770732c0ed1b7_RG_6_Vector2);
float3 _Normalize_f1ad7ea0af7b45ee88df143397ba6727_Out_1_Vector3;
Unity_Normalize_float3(-UNITY_MATRIX_V[2].xyz, _Normalize_f1ad7ea0af7b45ee88df143397ba6727_Out_1_Vector3);
float3 _Multiply_5b2d23749b2347b7af4472236c1dfe22_Out_2_Vector3;
Unity_Multiply_float3_float3(_Normalize_f1ad7ea0af7b45ee88df143397ba6727_Out_1_Vector3, float3(-1, -1, -1), _Multiply_5b2d23749b2347b7af4472236c1dfe22_Out_2_Vector3);
float _Split_6ebebe9410ea41bd8f8a327255848dc9_R_1_Float = _Multiply_5b2d23749b2347b7af4472236c1dfe22_Out_2_Vector3[0];
float _Split_6ebebe9410ea41bd8f8a327255848dc9_G_2_Float = _Multiply_5b2d23749b2347b7af4472236c1dfe22_Out_2_Vector3[1];
float _Split_6ebebe9410ea41bd8f8a327255848dc9_B_3_Float = _Multiply_5b2d23749b2347b7af4472236c1dfe22_Out_2_Vector3[2];
float _Split_6ebebe9410ea41bd8f8a327255848dc9_A_4_Float = 0;
float4 _Combine_ce9284087dd446409babf9c154f18fe4_RGBA_4_Vector4;
float3 _Combine_ce9284087dd446409babf9c154f18fe4_RGB_5_Vector3;
float2 _Combine_ce9284087dd446409babf9c154f18fe4_RG_6_Vector2;
Unity_Combine_float(_Split_6ebebe9410ea41bd8f8a327255848dc9_R_1_Float, _Split_6ebebe9410ea41bd8f8a327255848dc9_B_3_Float, float(0), float(0), _Combine_ce9284087dd446409babf9c154f18fe4_RGBA_4_Vector4, _Combine_ce9284087dd446409babf9c154f18fe4_RGB_5_Vector3, _Combine_ce9284087dd446409babf9c154f18fe4_RG_6_Vector2);
float _DotProduct_9e4f6525393a4171b6519e347a4b7a37_Out_2_Float;
Unity_DotProduct_float2(_Combine_87a90606ca4545b2983770732c0ed1b7_RG_6_Vector2, _Combine_ce9284087dd446409babf9c154f18fe4_RG_6_Vector2, _DotProduct_9e4f6525393a4171b6519e347a4b7a37_Out_2_Float);
float _Saturate_f76f8f4113e0468b9b20a6abf253d76e_Out_1_Float;
Unity_Saturate_float(_DotProduct_9e4f6525393a4171b6519e347a4b7a37_Out_2_Float, _Saturate_f76f8f4113e0468b9b20a6abf253d76e_Out_1_Float);
float _Property_bd14cfb26b074dbabde1a5a9b7f95d97_Out_0_Float = _Subsurface_Transition_Smoothness;
float _Power_2bc1d129029a4da69b2993ea07ff412f_Out_2_Float;
Unity_Power_float(_Saturate_f76f8f4113e0468b9b20a6abf253d76e_Out_1_Float, _Property_bd14cfb26b074dbabde1a5a9b7f95d97_Out_0_Float, _Power_2bc1d129029a4da69b2993ea07ff412f_Out_2_Float);
float _Saturate_44588f923e1b48ff97cd55ea2d69aa2b_Out_1_Float;
Unity_Saturate_float(_Power_2bc1d129029a4da69b2993ea07ff412f_Out_2_Float, _Saturate_44588f923e1b48ff97cd55ea2d69aa2b_Out_1_Float);
float4 _Lerp_98f83471835040419ec38e468d855fe3_Out_3_Vector4;
Unity_Lerp_float4(_Lerp_3406aa7c52b14c818ac9eb76bf068e2d_Out_3_Vector4, _Saturate_f0090edc5c664784a336737069276109_Out_1_Vector4, (_Saturate_44588f923e1b48ff97cd55ea2d69aa2b_Out_1_Float.xxxx), _Lerp_98f83471835040419ec38e468d855fe3_Out_3_Vector4);
float _Property_e00e00901e414a40a25484eb2c276584_Out_0_Float = _Subsurface_Fresnel_Intensity;
float4 _Multiply_6ed60680651f4e8cbff9e6f2ed97f0b9_Out_2_Vector4;
Unity_Multiply_float4_float4(_Lerp_98f83471835040419ec38e468d855fe3_Out_3_Vector4, (_Property_e00e00901e414a40a25484eb2c276584_Out_0_Float.xxxx), _Multiply_6ed60680651f4e8cbff9e6f2ed97f0b9_Out_2_Vector4);
float4 _Saturate_5cea81f1b9b5405fb4a571a97e57df52_Out_1_Vector4;
Unity_Saturate_float4(_Multiply_6ed60680651f4e8cbff9e6f2ed97f0b9_Out_2_Vector4, _Saturate_5cea81f1b9b5405fb4a571a97e57df52_Out_1_Vector4);
float _Saturate_10ad388197014e2fa2d0bd8261d82207_Out_1_Float;
Unity_Saturate_float(_DotProduct_9e4f6525393a4171b6519e347a4b7a37_Out_2_Float, _Saturate_10ad388197014e2fa2d0bd8261d82207_Out_1_Float);
float _Property_fe3bfd62823b45bf868d2c88982c1f64_Out_0_Float = _Subsurface_Fresnel_Transition_Smoothness;
float _Power_1ad124a5d5b74c13848df4f8f171d448_Out_2_Float;
Unity_Power_float(_Saturate_10ad388197014e2fa2d0bd8261d82207_Out_1_Float, _Property_fe3bfd62823b45bf868d2c88982c1f64_Out_0_Float, _Power_1ad124a5d5b74c13848df4f8f171d448_Out_2_Float);
float3 _Normalize_7e92e50b98274c508fa061a32d7a99b7_Out_1_Vector3;
Unity_Normalize_float3(IN.WorldSpaceViewDirection, _Normalize_7e92e50b98274c508fa061a32d7a99b7_Out_1_Vector3);
float _DotProduct_3170aef4b99740d083af85a5aa294edd_Out_2_Float;
Unity_DotProduct_float3(IN.WorldSpaceNormal, _Normalize_7e92e50b98274c508fa061a32d7a99b7_Out_1_Vector3, _DotProduct_3170aef4b99740d083af85a5aa294edd_Out_2_Float);
float _Absolute_111753c15e794f4b8e885c2c19d93d82_Out_1_Float;
Unity_Absolute_float(_DotProduct_3170aef4b99740d083af85a5aa294edd_Out_2_Float, _Absolute_111753c15e794f4b8e885c2c19d93d82_Out_1_Float);
float _OneMinus_2a02f0d378e1454c9aeda0ff447d4d86_Out_1_Float;
Unity_OneMinus_float(_Absolute_111753c15e794f4b8e885c2c19d93d82_Out_1_Float, _OneMinus_2a02f0d378e1454c9aeda0ff447d4d86_Out_1_Float);
float _Property_49c0203b77734a36b9ba06f7bcd723ee_Out_0_Float = _Subsurface_Fresnel_Pow;
float _Power_71121b48d5934b11a14aeadcad656816_Out_2_Float;
Unity_Power_float(_OneMinus_2a02f0d378e1454c9aeda0ff447d4d86_Out_1_Float, _Property_49c0203b77734a36b9ba06f7bcd723ee_Out_0_Float, _Power_71121b48d5934b11a14aeadcad656816_Out_2_Float);
float _Saturate_29e69580558f40c49a883332cdf25357_Out_1_Float;
Unity_Saturate_float(_Power_71121b48d5934b11a14aeadcad656816_Out_2_Float, _Saturate_29e69580558f40c49a883332cdf25357_Out_1_Float);
float _Multiply_328c604c9492493ba1dc1999ef6314fd_Out_2_Float;
Unity_Multiply_float_float(_Power_1ad124a5d5b74c13848df4f8f171d448_Out_2_Float, _Saturate_29e69580558f40c49a883332cdf25357_Out_1_Float, _Multiply_328c604c9492493ba1dc1999ef6314fd_Out_2_Float);
float4 _Lerp_10a8be366b974da7b00eab9c55f88a8a_Out_3_Vector4;
Unity_Lerp_float4(_Lerp_98f83471835040419ec38e468d855fe3_Out_3_Vector4, _Saturate_5cea81f1b9b5405fb4a571a97e57df52_Out_1_Vector4, (_Multiply_328c604c9492493ba1dc1999ef6314fd_Out_2_Float.xxxx), _Lerp_10a8be366b974da7b00eab9c55f88a8a_Out_3_Vector4);
float _Property_e5d876e9ad8e4cf1ab7db7d850c55d8c_Out_0_Float = _Roughness;
float _OneMinus_200145be127f401eae78bc05b8a1fc88_Out_1_Float;
Unity_OneMinus_float(_Property_e5d876e9ad8e4cf1ab7db7d850c55d8c_Out_0_Float, _OneMinus_200145be127f401eae78bc05b8a1fc88_Out_1_Float);
float _Multiply_cf1ea983d11e4ab7b4bdbbb064ad94b5_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_A_7_Float, 1, _Multiply_cf1ea983d11e4ab7b4bdbbb064ad94b5_Out_2_Float);
float _Property_99bb0b22edbb4ea0b1b7844414b876a7_Out_0_Float = _Alpha_Clip;
surface.BaseColor = (_Lerp_10a8be366b974da7b00eab9c55f88a8a_Out_3_Vector4.xyz);
surface.NormalTS = IN.TangentSpaceNormal;
surface.Emission = float3(0, 0, 0);
surface.Metallic = float(0);
surface.Smoothness = _OneMinus_200145be127f401eae78bc05b8a1fc88_Out_1_Float;
surface.Occlusion = float(1);
surface.Alpha = _Multiply_cf1ea983d11e4ab7b4bdbbb064ad94b5_Out_2_Float;
surface.AlphaClipThreshold = _Property_99bb0b22edbb4ea0b1b7844414b876a7_Out_0_Float;
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

    

    // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
    float3 unnormalizedNormalWS = input.normalWS;
    const float renormFactor = 1.0 / length(unnormalizedNormalWS);


    output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph
    output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);


    output.WorldSpaceViewDirection = GetWorldSpaceNormalizeViewDir(input.positionWS);
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
#pragma shader_feature_local _ _USE_WPO
#pragma shader_feature_local _ _USE_HEIGHT_GRADIENCE

#if defined(_USE_WPO) && defined(_USE_HEIGHT_GRADIENCE)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_WPO)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_HEIGHT_GRADIENCE)
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
#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_NORMAL_WS
#define VARYINGS_NEED_TANGENT_WS
#define VARYINGS_NEED_TEXCOORD0
#define VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_DEFERRED
#define BUILTIN_TARGET_API 1
#define _BUILTIN_AlphaClip 1
#define _BUILTIN_ALPHATEST_ON 1
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
 float3 WorldSpaceNormal;
 float3 TangentSpaceNormal;
 float3 WorldSpaceViewDirection;
 float3 WorldSpacePosition;
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 WorldSpacePosition;
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

float4 _Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D_TexelSize;
float _Subsurface_Fresnel_Transition_Smoothness;
float _Subsurface_Fresnel_Pow;
float _Subsurface_Fresnel_Intensity;
float _Subsurface_Transition_Smoothness;
float _Wiggle_Wind_Speed_Large;
float _Wiggle_Wind_Intensity;
float _Roughness;
float4 _Foliage_Color_Bottom;
float _Sway_Wind_Speed;
float _Gradience_Power;
float _Fresnel_Mul;
float4 _Foliage_Color_Top;
float _Always_On_Fresnel_Intensity;
float _Tree_Height;
float4 _Color_Map_TexelSize;
float _Height_Weight;
float _Height_Gradience;
float _Sway_Wind_Time_Random_Offset_Amount;
float _Sway_Wind_Intensity;
float3 _Sway_Wind_Direction;
float _Color_Map_Tiling;
float _Subsurface_Intensity;
float _Alpha_Clip;
float _Wiggle_Wind_Size;
float _Wiggle_Wind_Speed_Small;
float _Fresnel_Pow;
float _Always_On_Fresnel_Pow;
float4 _Always_On_Fresnel_Color;
float _ree;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D);
SAMPLER(sampler_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D);
TEXTURE2D(_Color_Map);
SAMPLER(sampler_Color_Map);

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

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Saturate_float4(float4 In, out float4 Out)
{
    Out = saturate(In);
}

void Unity_Normalize_float3(float3 In, out float3 Out)
{
    Out = normalize(In);
}

void Unity_DotProduct_float3(float3 A, float3 B, out float Out)
{
    Out = dot(A, B);
}

void Unity_Absolute_float(float In, out float Out)
{
    Out = abs(In);
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
}

// unity-custom-func-begin
void MainLight_float(out float3 Direction){

#ifdef UNITY_SHADER_GRAPH
	Direction = _WorldSpaceLightPos0.xyz;
#else
	Direction = float3(1,1,1);
#endif
}
// unity-custom-func-end

void Unity_DotProduct_float2(float2 A, float2 B, out float Out)
{
    Out = dot(A, B);
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
float _Property_7689f3a0523b4af9912879a05d1d5206_Out_0_Float = _Sway_Wind_Speed;
float3 _Property_e0531013c46845b6b036ac453118c744_Out_0_Vector3 = _Sway_Wind_Direction;
float _Property_35588e6a16b5457695dba39b1644d669_Out_0_Float = _Sway_Wind_Intensity;
float _Property_c8d3d7e6e6f64d409f010d988b96e327_Out_0_Float = _Sway_Wind_Time_Random_Offset_Amount;
Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float _WindSway_662bcd4f8b2b4607b138cb44de00a708;
_WindSway_662bcd4f8b2b4607b138cb44de00a708.ObjectSpacePosition = IN.ObjectSpacePosition;
_WindSway_662bcd4f8b2b4607b138cb44de00a708.TimeParameters = IN.TimeParameters;
float3 _WindSway_662bcd4f8b2b4607b138cb44de00a708_OutVector3_1_Vector3;
SG_WindSway_51c4027ae50a8cd49be53c4990b9f179_float(_Property_7689f3a0523b4af9912879a05d1d5206_Out_0_Float, _Property_e0531013c46845b6b036ac453118c744_Out_0_Vector3, _Property_35588e6a16b5457695dba39b1644d669_Out_0_Float, _Property_c8d3d7e6e6f64d409f010d988b96e327_Out_0_Float, _WindSway_662bcd4f8b2b4607b138cb44de00a708, _WindSway_662bcd4f8b2b4607b138cb44de00a708_OutVector3_1_Vector3);
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_R_1_Float = IN.WorldSpacePosition[0];
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_G_2_Float = IN.WorldSpacePosition[1];
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_B_3_Float = IN.WorldSpacePosition[2];
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_A_4_Float = 0;
float3 _Vector3_e154b524054546d2bbeab293c0e20519_Out_0_Vector3 = float3(float(0), float(0), float(0));
float3 _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3;
_Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3 = TransformObjectToWorld(_Vector3_e154b524054546d2bbeab293c0e20519_Out_0_Vector3.xyz);
float _Split_5eb48153bba44ea2b128583819f09a11_R_1_Float = _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3[0];
float _Split_5eb48153bba44ea2b128583819f09a11_G_2_Float = _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3[1];
float _Split_5eb48153bba44ea2b128583819f09a11_B_3_Float = _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3[2];
float _Split_5eb48153bba44ea2b128583819f09a11_A_4_Float = 0;
float _Subtract_d6a99abdf4e74644acffc1718d1735bf_Out_2_Float;
Unity_Subtract_float(_Split_8fe962eb6f0f4d6fba734f9edcaf9067_G_2_Float, _Split_5eb48153bba44ea2b128583819f09a11_G_2_Float, _Subtract_d6a99abdf4e74644acffc1718d1735bf_Out_2_Float);
float _Property_1cf3cda52e094e638df491b003607dfd_Out_0_Float = _Height_Weight;
float _Split_145cf2161d4c4510b08248cad819f655_R_1_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[0];
float _Split_145cf2161d4c4510b08248cad819f655_G_2_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[1];
float _Split_145cf2161d4c4510b08248cad819f655_B_3_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[2];
float _Split_145cf2161d4c4510b08248cad819f655_A_4_Float = 0;
float _Multiply_2c140d573b9447aab058bd863fe2f4f7_Out_2_Float;
Unity_Multiply_float_float(_Property_1cf3cda52e094e638df491b003607dfd_Out_0_Float, _Split_145cf2161d4c4510b08248cad819f655_G_2_Float, _Multiply_2c140d573b9447aab058bd863fe2f4f7_Out_2_Float);
float _Divide_0f6a637143d841d0b1c94fa2ae3e164d_Out_2_Float;
Unity_Divide_float(_Subtract_d6a99abdf4e74644acffc1718d1735bf_Out_2_Float, _Multiply_2c140d573b9447aab058bd863fe2f4f7_Out_2_Float, _Divide_0f6a637143d841d0b1c94fa2ae3e164d_Out_2_Float);
float _Saturate_1e4e29fb5c074565a1f941c046957b05_Out_1_Float;
Unity_Saturate_float(_Divide_0f6a637143d841d0b1c94fa2ae3e164d_Out_2_Float, _Saturate_1e4e29fb5c074565a1f941c046957b05_Out_1_Float);
float _Property_3d64a0c8dd104507b9a0762a778fe585_Out_0_Float = _Height_Gradience;
float _Power_f1d74728398a45f9993fe3b3affb6584_Out_2_Float;
Unity_Power_float(_Saturate_1e4e29fb5c074565a1f941c046957b05_Out_1_Float, _Property_3d64a0c8dd104507b9a0762a778fe585_Out_0_Float, _Power_f1d74728398a45f9993fe3b3affb6584_Out_2_Float);
float _Saturate_57b130350452418fa1d09d4a0ddd64ac_Out_1_Float;
Unity_Saturate_float(_Power_f1d74728398a45f9993fe3b3affb6584_Out_2_Float, _Saturate_57b130350452418fa1d09d4a0ddd64ac_Out_1_Float);
float3 _Lerp_1fbf8d0a7f5a4e2ab381967ac90385c7_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _WindSway_662bcd4f8b2b4607b138cb44de00a708_OutVector3_1_Vector3, (_Saturate_57b130350452418fa1d09d4a0ddd64ac_Out_1_Float.xxx), _Lerp_1fbf8d0a7f5a4e2ab381967ac90385c7_Out_3_Vector3);
float _Split_bd04916788224fa2bb71be3f27eb839a_R_1_Float = IN.WorldSpacePosition[0];
float _Split_bd04916788224fa2bb71be3f27eb839a_G_2_Float = IN.WorldSpacePosition[1];
float _Split_bd04916788224fa2bb71be3f27eb839a_B_3_Float = IN.WorldSpacePosition[2];
float _Split_bd04916788224fa2bb71be3f27eb839a_A_4_Float = 0;
float4 _Combine_45cdab9c20414d03862100967f56c78d_RGBA_4_Vector4;
float3 _Combine_45cdab9c20414d03862100967f56c78d_RGB_5_Vector3;
float2 _Combine_45cdab9c20414d03862100967f56c78d_RG_6_Vector2;
Unity_Combine_float(_Split_bd04916788224fa2bb71be3f27eb839a_R_1_Float, _Split_bd04916788224fa2bb71be3f27eb839a_B_3_Float, float(0), float(0), _Combine_45cdab9c20414d03862100967f56c78d_RGBA_4_Vector4, _Combine_45cdab9c20414d03862100967f56c78d_RGB_5_Vector3, _Combine_45cdab9c20414d03862100967f56c78d_RG_6_Vector2);
float _Property_9b35f6244c0247c7a9be7af2671f58e7_Out_0_Float = _Wiggle_Wind_Size;
float2 _Multiply_ba4d7f376f904143acc9eb33d8414ff8_Out_2_Vector2;
Unity_Multiply_float2_float2(_Combine_45cdab9c20414d03862100967f56c78d_RG_6_Vector2, (_Property_9b35f6244c0247c7a9be7af2671f58e7_Out_0_Float.xx), _Multiply_ba4d7f376f904143acc9eb33d8414ff8_Out_2_Vector2);
float _Property_6eae25110c184e7a94293b95ddc9ecef_Out_0_Float = _Wiggle_Wind_Speed_Small;
float _Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Property_6eae25110c184e7a94293b95ddc9ecef_Out_0_Float, _Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float);
float _Multiply_7510d5a95e6c4d3986bb83059d23d2fa_Out_2_Float;
Unity_Multiply_float_float(_Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float, 0.8, _Multiply_7510d5a95e6c4d3986bb83059d23d2fa_Out_2_Float);
float2 _Vector2_cb26bc8c681b455c81073f0dc3c8f6b4_Out_0_Vector2 = float2(_Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float, _Multiply_7510d5a95e6c4d3986bb83059d23d2fa_Out_2_Float);
float2 _TilingAndOffset_e5f47c2cf9d846859a72aadcdac7fc3b_Out_3_Vector2;
Unity_TilingAndOffset_float(_Multiply_ba4d7f376f904143acc9eb33d8414ff8_Out_2_Vector2, float2 (1, 1), _Vector2_cb26bc8c681b455c81073f0dc3c8f6b4_Out_0_Vector2, _TilingAndOffset_e5f47c2cf9d846859a72aadcdac7fc3b_Out_3_Vector2);
  float4 _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).tex, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).GetTransformedUV(_TilingAndOffset_e5f47c2cf9d846859a72aadcdac7fc3b_Out_3_Vector2), float(2));
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_R_5_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_G_6_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_B_7_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_A_8_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.a;
float _Float_3f1a7b5619414946b13e7cef19336d45_Out_0_Float = float(0.3);
float _Subtract_3105ef040f344481afb342cd9ef65e9f_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_R_5_Float, _Float_3f1a7b5619414946b13e7cef19336d45_Out_0_Float, _Subtract_3105ef040f344481afb342cd9ef65e9f_Out_2_Float);
float _Split_18495110217342bc80044d9345d98fbd_R_1_Float = IN.WorldSpacePosition[0];
float _Split_18495110217342bc80044d9345d98fbd_G_2_Float = IN.WorldSpacePosition[1];
float _Split_18495110217342bc80044d9345d98fbd_B_3_Float = IN.WorldSpacePosition[2];
float _Split_18495110217342bc80044d9345d98fbd_A_4_Float = 0;
float4 _Combine_d1ee3e49696840daa7cfd43df2287807_RGBA_4_Vector4;
float3 _Combine_d1ee3e49696840daa7cfd43df2287807_RGB_5_Vector3;
float2 _Combine_d1ee3e49696840daa7cfd43df2287807_RG_6_Vector2;
Unity_Combine_float(_Split_18495110217342bc80044d9345d98fbd_R_1_Float, _Split_18495110217342bc80044d9345d98fbd_G_2_Float, float(0), float(0), _Combine_d1ee3e49696840daa7cfd43df2287807_RGBA_4_Vector4, _Combine_d1ee3e49696840daa7cfd43df2287807_RGB_5_Vector3, _Combine_d1ee3e49696840daa7cfd43df2287807_RG_6_Vector2);
float _Property_9efe7c2851414ea39b6971daac6143e5_Out_0_Float = _Wiggle_Wind_Size;
float _Multiply_4f6b361114004341aeae12987f071375_Out_2_Float;
Unity_Multiply_float_float(_Property_9efe7c2851414ea39b6971daac6143e5_Out_0_Float, 0.763, _Multiply_4f6b361114004341aeae12987f071375_Out_2_Float);
float2 _Multiply_0d88e33790f0458b9e6e7f49946e6344_Out_2_Vector2;
Unity_Multiply_float2_float2(_Combine_d1ee3e49696840daa7cfd43df2287807_RG_6_Vector2, (_Multiply_4f6b361114004341aeae12987f071375_Out_2_Float.xx), _Multiply_0d88e33790f0458b9e6e7f49946e6344_Out_2_Vector2);
float _Property_56c994a79361438d88ea30b1e3ccf2aa_Out_0_Float = _Wiggle_Wind_Speed_Large;
float _Multiply_c5e5feed518642678424c6f0d45b4377_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Property_56c994a79361438d88ea30b1e3ccf2aa_Out_0_Float, _Multiply_c5e5feed518642678424c6f0d45b4377_Out_2_Float);
float2 _TilingAndOffset_7dac505987c144edad083d13ebca67e6_Out_3_Vector2;
Unity_TilingAndOffset_float(_Multiply_0d88e33790f0458b9e6e7f49946e6344_Out_2_Vector2, float2 (1, 1), (_Multiply_c5e5feed518642678424c6f0d45b4377_Out_2_Float.xx), _TilingAndOffset_7dac505987c144edad083d13ebca67e6_Out_3_Vector2);
  float4 _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).tex, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).GetTransformedUV(_TilingAndOffset_7dac505987c144edad083d13ebca67e6_Out_3_Vector2), float(2));
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_R_5_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_G_6_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_B_7_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_A_8_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.a;
float _Subtract_e29fd5b0daea41a1a38ac52280e6db88_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_G_6_Float, _Float_3f1a7b5619414946b13e7cef19336d45_Out_0_Float, _Subtract_e29fd5b0daea41a1a38ac52280e6db88_Out_2_Float);
float _Add_6d51be6104ee4e23a093dfb9c3d993f7_Out_2_Float;
Unity_Add_float(_Subtract_3105ef040f344481afb342cd9ef65e9f_Out_2_Float, _Subtract_e29fd5b0daea41a1a38ac52280e6db88_Out_2_Float, _Add_6d51be6104ee4e23a093dfb9c3d993f7_Out_2_Float);
float4 _Multiply_db043a8b3fc34edb8bee433085c7e061_Out_2_Vector4;
Unity_Multiply_float4_float4((_Add_6d51be6104ee4e23a093dfb9c3d993f7_Out_2_Float.xxxx), IN.VertexColor, _Multiply_db043a8b3fc34edb8bee433085c7e061_Out_2_Vector4);
float _Property_46c8c648052246a4a32e80905ed7e9df_Out_0_Float = _Wiggle_Wind_Intensity;
float4 _Multiply_4baa80310c414483a2aada72baa7a883_Out_2_Vector4;
Unity_Multiply_float4_float4(_Multiply_db043a8b3fc34edb8bee433085c7e061_Out_2_Vector4, (_Property_46c8c648052246a4a32e80905ed7e9df_Out_0_Float.xxxx), _Multiply_4baa80310c414483a2aada72baa7a883_Out_2_Vector4);
float3 _Multiply_2c95a93020c44a20be82a31b985c35b8_Out_2_Vector3;
Unity_Multiply_float3_float3((_Multiply_4baa80310c414483a2aada72baa7a883_Out_2_Vector4.xyz), IN.ObjectSpaceNormal, _Multiply_2c95a93020c44a20be82a31b985c35b8_Out_2_Vector3);
float3 _Add_5a143eeddea74bceb3ddd5b059f49da5_Out_2_Vector3;
Unity_Add_float3(_Lerp_1fbf8d0a7f5a4e2ab381967ac90385c7_Out_3_Vector3, _Multiply_2c95a93020c44a20be82a31b985c35b8_Out_2_Vector3, _Add_5a143eeddea74bceb3ddd5b059f49da5_Out_2_Vector3);
#if defined(_USE_WPO)
float3 _UseWPO_1f28bdd134c9432e812aded965021562_Out_0_Vector3 = _Add_5a143eeddea74bceb3ddd5b059f49da5_Out_2_Vector3;
#else
float3 _UseWPO_1f28bdd134c9432e812aded965021562_Out_0_Vector3 = IN.ObjectSpacePosition;
#endif
float3 _UPGFoliageContactPositionCustomFunction_3446fb1a0c894696bd903b7eb184e241_bentPositionOS_1_Vector3;
UPG_FoliageContactPosition_float(_UseWPO_1f28bdd134c9432e812aded965021562_Out_0_Vector3, _UPGFoliageContactPositionCustomFunction_3446fb1a0c894696bd903b7eb184e241_bentPositionOS_1_Vector3);
description.Position = _UPGFoliageContactPositionCustomFunction_3446fb1a0c894696bd903b7eb184e241_bentPositionOS_1_Vector3;
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
float4 _Property_a9c1982af1c74efb9ac25b0326ae7979_Out_0_Vector4 = _Foliage_Color_Bottom;
float4 _Property_50eb1cb960084765a0771df7f40a6d5d_Out_0_Vector4 = _Foliage_Color_Top;
float _Split_1540f44067c24640b4968355fa26a295_R_1_Float = IN.WorldSpacePosition[0];
float _Split_1540f44067c24640b4968355fa26a295_G_2_Float = IN.WorldSpacePosition[1];
float _Split_1540f44067c24640b4968355fa26a295_B_3_Float = IN.WorldSpacePosition[2];
float _Split_1540f44067c24640b4968355fa26a295_A_4_Float = 0;
float3 _Vector3_54c605479a824dd3a62abb1919f39c57_Out_0_Vector3 = float3(float(0), float(0), float(0));
float3 _Transform_f3f68ad0eeb04ce9ac459f2859353c06_Out_1_Vector3;
_Transform_f3f68ad0eeb04ce9ac459f2859353c06_Out_1_Vector3 = TransformObjectToWorld(_Vector3_54c605479a824dd3a62abb1919f39c57_Out_0_Vector3.xyz);
float _Split_15d5002ae66545318962a44958be8e00_R_1_Float = _Transform_f3f68ad0eeb04ce9ac459f2859353c06_Out_1_Vector3[0];
float _Split_15d5002ae66545318962a44958be8e00_G_2_Float = _Transform_f3f68ad0eeb04ce9ac459f2859353c06_Out_1_Vector3[1];
float _Split_15d5002ae66545318962a44958be8e00_B_3_Float = _Transform_f3f68ad0eeb04ce9ac459f2859353c06_Out_1_Vector3[2];
float _Split_15d5002ae66545318962a44958be8e00_A_4_Float = 0;
float _Subtract_3131eba61d024496834f2933dbde6855_Out_2_Float;
Unity_Subtract_float(_Split_1540f44067c24640b4968355fa26a295_G_2_Float, _Split_15d5002ae66545318962a44958be8e00_G_2_Float, _Subtract_3131eba61d024496834f2933dbde6855_Out_2_Float);
float _Property_605b997dac5a4228881635945f995c72_Out_0_Float = _Tree_Height;
float _Divide_8e4152d2ed4943778a44dc39efa65611_Out_2_Float;
Unity_Divide_float(_Subtract_3131eba61d024496834f2933dbde6855_Out_2_Float, _Property_605b997dac5a4228881635945f995c72_Out_0_Float, _Divide_8e4152d2ed4943778a44dc39efa65611_Out_2_Float);
float _Saturate_086c13233c3d43fab6a33a1e2ed498c2_Out_1_Float;
Unity_Saturate_float(_Divide_8e4152d2ed4943778a44dc39efa65611_Out_2_Float, _Saturate_086c13233c3d43fab6a33a1e2ed498c2_Out_1_Float);
float _Property_0e5bff248b2341909121bb16666f3468_Out_0_Float = _Gradience_Power;
float _Power_be0c59cc158d492f99ea24f3b1001e1f_Out_2_Float;
Unity_Power_float(_Saturate_086c13233c3d43fab6a33a1e2ed498c2_Out_1_Float, _Property_0e5bff248b2341909121bb16666f3468_Out_0_Float, _Power_be0c59cc158d492f99ea24f3b1001e1f_Out_2_Float);
float _Saturate_26bbc2447b5a402c9562a9e5d012fd6c_Out_1_Float;
Unity_Saturate_float(_Power_be0c59cc158d492f99ea24f3b1001e1f_Out_2_Float, _Saturate_26bbc2447b5a402c9562a9e5d012fd6c_Out_1_Float);
float4 _Lerp_795c581446834fe0bee35971bc8f0c5c_Out_3_Vector4;
Unity_Lerp_float4(_Property_a9c1982af1c74efb9ac25b0326ae7979_Out_0_Vector4, _Property_50eb1cb960084765a0771df7f40a6d5d_Out_0_Vector4, (_Saturate_26bbc2447b5a402c9562a9e5d012fd6c_Out_1_Float.xxxx), _Lerp_795c581446834fe0bee35971bc8f0c5c_Out_3_Vector4);
float4 _Property_4036fdaeaa4647adb3dd84d70ff1161e_Out_0_Vector4 = _Foliage_Color_Top;
#if defined(_USE_HEIGHT_GRADIENCE)
float4 _UseHeightGradience_d4326b9500c14f11a9090b7e8f107721_Out_0_Vector4 = _Lerp_795c581446834fe0bee35971bc8f0c5c_Out_3_Vector4;
#else
float4 _UseHeightGradience_d4326b9500c14f11a9090b7e8f107721_Out_0_Vector4 = _Property_4036fdaeaa4647adb3dd84d70ff1161e_Out_0_Vector4;
#endif
UnityTexture2D _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Color_Map, sampler_Color_Map, _Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_eaea7ba5e1e74519a1bc7833f0103ab8_Out_0_Vector4 = IN.uv0;
float _Property_95352575ae4140008287fd58138e8eb5_Out_0_Float = _Color_Map_Tiling;
float4 _Multiply_86fafeae394745fe880655d03cafd9e1_Out_2_Vector4;
Unity_Multiply_float4_float4(_UV_eaea7ba5e1e74519a1bc7833f0103ab8_Out_0_Vector4, (_Property_95352575ae4140008287fd58138e8eb5_Out_0_Float.xxxx), _Multiply_86fafeae394745fe880655d03cafd9e1_Out_2_Vector4);
float4 _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.tex, _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.samplerstate, _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.GetTransformedUV((_Multiply_86fafeae394745fe880655d03cafd9e1_Out_2_Vector4.xy)) );
if (_Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4, _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_R_4_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.r;
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_G_5_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.g;
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_B_6_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.b;
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_A_7_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.a;
float4 _Multiply_a05088f8e3434d54920d012fc47555a7_Out_2_Vector4;
Unity_Multiply_float4_float4(_UseHeightGradience_d4326b9500c14f11a9090b7e8f107721_Out_0_Vector4, _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4, _Multiply_a05088f8e3434d54920d012fc47555a7_Out_2_Vector4);
float4 _Property_ee5875e8bc214caba4dbea1511d06cd2_Out_0_Vector4 = _Always_On_Fresnel_Color;
float _Property_be2ed593ceb345f89b57d123af3db203_Out_0_Float = _Always_On_Fresnel_Intensity;
float4 _Multiply_dcf6e90c046c4f72b77b81df5b922468_Out_2_Vector4;
Unity_Multiply_float4_float4(_Property_ee5875e8bc214caba4dbea1511d06cd2_Out_0_Vector4, (_Property_be2ed593ceb345f89b57d123af3db203_Out_0_Float.xxxx), _Multiply_dcf6e90c046c4f72b77b81df5b922468_Out_2_Vector4);
float4 _Saturate_26f2848ceb0a494094065a9387275032_Out_1_Vector4;
Unity_Saturate_float4(_Multiply_dcf6e90c046c4f72b77b81df5b922468_Out_2_Vector4, _Saturate_26f2848ceb0a494094065a9387275032_Out_1_Vector4);
float3 _Normalize_764cced5d0894f3d89f5911d50d5f4a2_Out_1_Vector3;
Unity_Normalize_float3(IN.WorldSpaceViewDirection, _Normalize_764cced5d0894f3d89f5911d50d5f4a2_Out_1_Vector3);
float _DotProduct_eb716c3ca301415688a52e76b310a65c_Out_2_Float;
Unity_DotProduct_float3(IN.WorldSpaceNormal, _Normalize_764cced5d0894f3d89f5911d50d5f4a2_Out_1_Vector3, _DotProduct_eb716c3ca301415688a52e76b310a65c_Out_2_Float);
float _Absolute_470cf4f0298a44a695f47aa5ea61c0ab_Out_1_Float;
Unity_Absolute_float(_DotProduct_eb716c3ca301415688a52e76b310a65c_Out_2_Float, _Absolute_470cf4f0298a44a695f47aa5ea61c0ab_Out_1_Float);
float _OneMinus_fa832b719fc24001be421507d395a592_Out_1_Float;
Unity_OneMinus_float(_Absolute_470cf4f0298a44a695f47aa5ea61c0ab_Out_1_Float, _OneMinus_fa832b719fc24001be421507d395a592_Out_1_Float);
float _Property_d9cfcacb90204bdfa36d0c1b7c0da452_Out_0_Float = _Always_On_Fresnel_Pow;
float _Power_7841b1ae85554473b96cce132c7ff7e1_Out_2_Float;
Unity_Power_float(_OneMinus_fa832b719fc24001be421507d395a592_Out_1_Float, _Property_d9cfcacb90204bdfa36d0c1b7c0da452_Out_0_Float, _Power_7841b1ae85554473b96cce132c7ff7e1_Out_2_Float);
float _Saturate_908f6307f948471ca95c0e7507cd7e43_Out_1_Float;
Unity_Saturate_float(_Power_7841b1ae85554473b96cce132c7ff7e1_Out_2_Float, _Saturate_908f6307f948471ca95c0e7507cd7e43_Out_1_Float);
float4 _Lerp_3406aa7c52b14c818ac9eb76bf068e2d_Out_3_Vector4;
Unity_Lerp_float4(_Multiply_a05088f8e3434d54920d012fc47555a7_Out_2_Vector4, _Saturate_26f2848ceb0a494094065a9387275032_Out_1_Vector4, (_Saturate_908f6307f948471ca95c0e7507cd7e43_Out_1_Float.xxxx), _Lerp_3406aa7c52b14c818ac9eb76bf068e2d_Out_3_Vector4);
float _Property_e8b3335851f84d91b28234460148cd0f_Out_0_Float = _Subsurface_Intensity;
float4 _Multiply_94df75e4ad734308a11d784250fc0b0c_Out_2_Vector4;
Unity_Multiply_float4_float4(_Lerp_3406aa7c52b14c818ac9eb76bf068e2d_Out_3_Vector4, (_Property_e8b3335851f84d91b28234460148cd0f_Out_0_Float.xxxx), _Multiply_94df75e4ad734308a11d784250fc0b0c_Out_2_Vector4);
float4 _Saturate_f0090edc5c664784a336737069276109_Out_1_Vector4;
Unity_Saturate_float4(_Multiply_94df75e4ad734308a11d784250fc0b0c_Out_2_Vector4, _Saturate_f0090edc5c664784a336737069276109_Out_1_Vector4);
float3 _MainLightCustomFunction_461af74b29b6456da89a2348455c2483_Direction_0_Vector3;
MainLight_float(_MainLightCustomFunction_461af74b29b6456da89a2348455c2483_Direction_0_Vector3);
float3 _Normalize_8644c278d597434b8871eb11a7a52cfe_Out_1_Vector3;
Unity_Normalize_float3(_MainLightCustomFunction_461af74b29b6456da89a2348455c2483_Direction_0_Vector3, _Normalize_8644c278d597434b8871eb11a7a52cfe_Out_1_Vector3);
float _Split_8a104625401f4a048b1160e17800fd11_R_1_Float = _Normalize_8644c278d597434b8871eb11a7a52cfe_Out_1_Vector3[0];
float _Split_8a104625401f4a048b1160e17800fd11_G_2_Float = _Normalize_8644c278d597434b8871eb11a7a52cfe_Out_1_Vector3[1];
float _Split_8a104625401f4a048b1160e17800fd11_B_3_Float = _Normalize_8644c278d597434b8871eb11a7a52cfe_Out_1_Vector3[2];
float _Split_8a104625401f4a048b1160e17800fd11_A_4_Float = 0;
float4 _Combine_87a90606ca4545b2983770732c0ed1b7_RGBA_4_Vector4;
float3 _Combine_87a90606ca4545b2983770732c0ed1b7_RGB_5_Vector3;
float2 _Combine_87a90606ca4545b2983770732c0ed1b7_RG_6_Vector2;
Unity_Combine_float(_Split_8a104625401f4a048b1160e17800fd11_R_1_Float, _Split_8a104625401f4a048b1160e17800fd11_B_3_Float, float(0), float(0), _Combine_87a90606ca4545b2983770732c0ed1b7_RGBA_4_Vector4, _Combine_87a90606ca4545b2983770732c0ed1b7_RGB_5_Vector3, _Combine_87a90606ca4545b2983770732c0ed1b7_RG_6_Vector2);
float3 _Normalize_f1ad7ea0af7b45ee88df143397ba6727_Out_1_Vector3;
Unity_Normalize_float3(-UNITY_MATRIX_V[2].xyz, _Normalize_f1ad7ea0af7b45ee88df143397ba6727_Out_1_Vector3);
float3 _Multiply_5b2d23749b2347b7af4472236c1dfe22_Out_2_Vector3;
Unity_Multiply_float3_float3(_Normalize_f1ad7ea0af7b45ee88df143397ba6727_Out_1_Vector3, float3(-1, -1, -1), _Multiply_5b2d23749b2347b7af4472236c1dfe22_Out_2_Vector3);
float _Split_6ebebe9410ea41bd8f8a327255848dc9_R_1_Float = _Multiply_5b2d23749b2347b7af4472236c1dfe22_Out_2_Vector3[0];
float _Split_6ebebe9410ea41bd8f8a327255848dc9_G_2_Float = _Multiply_5b2d23749b2347b7af4472236c1dfe22_Out_2_Vector3[1];
float _Split_6ebebe9410ea41bd8f8a327255848dc9_B_3_Float = _Multiply_5b2d23749b2347b7af4472236c1dfe22_Out_2_Vector3[2];
float _Split_6ebebe9410ea41bd8f8a327255848dc9_A_4_Float = 0;
float4 _Combine_ce9284087dd446409babf9c154f18fe4_RGBA_4_Vector4;
float3 _Combine_ce9284087dd446409babf9c154f18fe4_RGB_5_Vector3;
float2 _Combine_ce9284087dd446409babf9c154f18fe4_RG_6_Vector2;
Unity_Combine_float(_Split_6ebebe9410ea41bd8f8a327255848dc9_R_1_Float, _Split_6ebebe9410ea41bd8f8a327255848dc9_B_3_Float, float(0), float(0), _Combine_ce9284087dd446409babf9c154f18fe4_RGBA_4_Vector4, _Combine_ce9284087dd446409babf9c154f18fe4_RGB_5_Vector3, _Combine_ce9284087dd446409babf9c154f18fe4_RG_6_Vector2);
float _DotProduct_9e4f6525393a4171b6519e347a4b7a37_Out_2_Float;
Unity_DotProduct_float2(_Combine_87a90606ca4545b2983770732c0ed1b7_RG_6_Vector2, _Combine_ce9284087dd446409babf9c154f18fe4_RG_6_Vector2, _DotProduct_9e4f6525393a4171b6519e347a4b7a37_Out_2_Float);
float _Saturate_f76f8f4113e0468b9b20a6abf253d76e_Out_1_Float;
Unity_Saturate_float(_DotProduct_9e4f6525393a4171b6519e347a4b7a37_Out_2_Float, _Saturate_f76f8f4113e0468b9b20a6abf253d76e_Out_1_Float);
float _Property_bd14cfb26b074dbabde1a5a9b7f95d97_Out_0_Float = _Subsurface_Transition_Smoothness;
float _Power_2bc1d129029a4da69b2993ea07ff412f_Out_2_Float;
Unity_Power_float(_Saturate_f76f8f4113e0468b9b20a6abf253d76e_Out_1_Float, _Property_bd14cfb26b074dbabde1a5a9b7f95d97_Out_0_Float, _Power_2bc1d129029a4da69b2993ea07ff412f_Out_2_Float);
float _Saturate_44588f923e1b48ff97cd55ea2d69aa2b_Out_1_Float;
Unity_Saturate_float(_Power_2bc1d129029a4da69b2993ea07ff412f_Out_2_Float, _Saturate_44588f923e1b48ff97cd55ea2d69aa2b_Out_1_Float);
float4 _Lerp_98f83471835040419ec38e468d855fe3_Out_3_Vector4;
Unity_Lerp_float4(_Lerp_3406aa7c52b14c818ac9eb76bf068e2d_Out_3_Vector4, _Saturate_f0090edc5c664784a336737069276109_Out_1_Vector4, (_Saturate_44588f923e1b48ff97cd55ea2d69aa2b_Out_1_Float.xxxx), _Lerp_98f83471835040419ec38e468d855fe3_Out_3_Vector4);
float _Property_e00e00901e414a40a25484eb2c276584_Out_0_Float = _Subsurface_Fresnel_Intensity;
float4 _Multiply_6ed60680651f4e8cbff9e6f2ed97f0b9_Out_2_Vector4;
Unity_Multiply_float4_float4(_Lerp_98f83471835040419ec38e468d855fe3_Out_3_Vector4, (_Property_e00e00901e414a40a25484eb2c276584_Out_0_Float.xxxx), _Multiply_6ed60680651f4e8cbff9e6f2ed97f0b9_Out_2_Vector4);
float4 _Saturate_5cea81f1b9b5405fb4a571a97e57df52_Out_1_Vector4;
Unity_Saturate_float4(_Multiply_6ed60680651f4e8cbff9e6f2ed97f0b9_Out_2_Vector4, _Saturate_5cea81f1b9b5405fb4a571a97e57df52_Out_1_Vector4);
float _Saturate_10ad388197014e2fa2d0bd8261d82207_Out_1_Float;
Unity_Saturate_float(_DotProduct_9e4f6525393a4171b6519e347a4b7a37_Out_2_Float, _Saturate_10ad388197014e2fa2d0bd8261d82207_Out_1_Float);
float _Property_fe3bfd62823b45bf868d2c88982c1f64_Out_0_Float = _Subsurface_Fresnel_Transition_Smoothness;
float _Power_1ad124a5d5b74c13848df4f8f171d448_Out_2_Float;
Unity_Power_float(_Saturate_10ad388197014e2fa2d0bd8261d82207_Out_1_Float, _Property_fe3bfd62823b45bf868d2c88982c1f64_Out_0_Float, _Power_1ad124a5d5b74c13848df4f8f171d448_Out_2_Float);
float3 _Normalize_7e92e50b98274c508fa061a32d7a99b7_Out_1_Vector3;
Unity_Normalize_float3(IN.WorldSpaceViewDirection, _Normalize_7e92e50b98274c508fa061a32d7a99b7_Out_1_Vector3);
float _DotProduct_3170aef4b99740d083af85a5aa294edd_Out_2_Float;
Unity_DotProduct_float3(IN.WorldSpaceNormal, _Normalize_7e92e50b98274c508fa061a32d7a99b7_Out_1_Vector3, _DotProduct_3170aef4b99740d083af85a5aa294edd_Out_2_Float);
float _Absolute_111753c15e794f4b8e885c2c19d93d82_Out_1_Float;
Unity_Absolute_float(_DotProduct_3170aef4b99740d083af85a5aa294edd_Out_2_Float, _Absolute_111753c15e794f4b8e885c2c19d93d82_Out_1_Float);
float _OneMinus_2a02f0d378e1454c9aeda0ff447d4d86_Out_1_Float;
Unity_OneMinus_float(_Absolute_111753c15e794f4b8e885c2c19d93d82_Out_1_Float, _OneMinus_2a02f0d378e1454c9aeda0ff447d4d86_Out_1_Float);
float _Property_49c0203b77734a36b9ba06f7bcd723ee_Out_0_Float = _Subsurface_Fresnel_Pow;
float _Power_71121b48d5934b11a14aeadcad656816_Out_2_Float;
Unity_Power_float(_OneMinus_2a02f0d378e1454c9aeda0ff447d4d86_Out_1_Float, _Property_49c0203b77734a36b9ba06f7bcd723ee_Out_0_Float, _Power_71121b48d5934b11a14aeadcad656816_Out_2_Float);
float _Saturate_29e69580558f40c49a883332cdf25357_Out_1_Float;
Unity_Saturate_float(_Power_71121b48d5934b11a14aeadcad656816_Out_2_Float, _Saturate_29e69580558f40c49a883332cdf25357_Out_1_Float);
float _Multiply_328c604c9492493ba1dc1999ef6314fd_Out_2_Float;
Unity_Multiply_float_float(_Power_1ad124a5d5b74c13848df4f8f171d448_Out_2_Float, _Saturate_29e69580558f40c49a883332cdf25357_Out_1_Float, _Multiply_328c604c9492493ba1dc1999ef6314fd_Out_2_Float);
float4 _Lerp_10a8be366b974da7b00eab9c55f88a8a_Out_3_Vector4;
Unity_Lerp_float4(_Lerp_98f83471835040419ec38e468d855fe3_Out_3_Vector4, _Saturate_5cea81f1b9b5405fb4a571a97e57df52_Out_1_Vector4, (_Multiply_328c604c9492493ba1dc1999ef6314fd_Out_2_Float.xxxx), _Lerp_10a8be366b974da7b00eab9c55f88a8a_Out_3_Vector4);
float _Property_e5d876e9ad8e4cf1ab7db7d850c55d8c_Out_0_Float = _Roughness;
float _OneMinus_200145be127f401eae78bc05b8a1fc88_Out_1_Float;
Unity_OneMinus_float(_Property_e5d876e9ad8e4cf1ab7db7d850c55d8c_Out_0_Float, _OneMinus_200145be127f401eae78bc05b8a1fc88_Out_1_Float);
float _Multiply_cf1ea983d11e4ab7b4bdbbb064ad94b5_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_A_7_Float, 1, _Multiply_cf1ea983d11e4ab7b4bdbbb064ad94b5_Out_2_Float);
float _Property_99bb0b22edbb4ea0b1b7844414b876a7_Out_0_Float = _Alpha_Clip;
surface.BaseColor = (_Lerp_10a8be366b974da7b00eab9c55f88a8a_Out_3_Vector4.xyz);
surface.NormalTS = IN.TangentSpaceNormal;
surface.Emission = float3(0, 0, 0);
surface.Metallic = float(0);
surface.Smoothness = _OneMinus_200145be127f401eae78bc05b8a1fc88_Out_1_Float;
surface.Occlusion = float(1);
surface.Alpha = _Multiply_cf1ea983d11e4ab7b4bdbbb064ad94b5_Out_2_Float;
surface.AlphaClipThreshold = _Property_99bb0b22edbb4ea0b1b7844414b876a7_Out_0_Float;
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

    

    // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
    float3 unnormalizedNormalWS = input.normalWS;
    const float renormFactor = 1.0 / length(unnormalizedNormalWS);


    output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph
    output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);


    output.WorldSpaceViewDirection = GetWorldSpaceNormalizeViewDir(input.positionWS);
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
Cull Off
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
#pragma shader_feature_local _ _USE_WPO
#pragma shader_feature_local _ _USE_HEIGHT_GRADIENCE

#if defined(_USE_WPO) && defined(_USE_HEIGHT_GRADIENCE)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_WPO)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_HEIGHT_GRADIENCE)
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
#define VARYINGS_NEED_TEXCOORD0
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_SHADOWCASTER
#define BUILTIN_TARGET_API 1
#define _BUILTIN_AlphaClip 1
#define _BUILTIN_ALPHATEST_ON 1
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

float4 _Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D_TexelSize;
float _Subsurface_Fresnel_Transition_Smoothness;
float _Subsurface_Fresnel_Pow;
float _Subsurface_Fresnel_Intensity;
float _Subsurface_Transition_Smoothness;
float _Wiggle_Wind_Speed_Large;
float _Wiggle_Wind_Intensity;
float _Roughness;
float4 _Foliage_Color_Bottom;
float _Sway_Wind_Speed;
float _Gradience_Power;
float _Fresnel_Mul;
float4 _Foliage_Color_Top;
float _Always_On_Fresnel_Intensity;
float _Tree_Height;
float4 _Color_Map_TexelSize;
float _Height_Weight;
float _Height_Gradience;
float _Sway_Wind_Time_Random_Offset_Amount;
float _Sway_Wind_Intensity;
float3 _Sway_Wind_Direction;
float _Color_Map_Tiling;
float _Subsurface_Intensity;
float _Alpha_Clip;
float _Wiggle_Wind_Size;
float _Wiggle_Wind_Speed_Small;
float _Fresnel_Pow;
float _Always_On_Fresnel_Pow;
float4 _Always_On_Fresnel_Color;
float _ree;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D);
SAMPLER(sampler_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D);
TEXTURE2D(_Color_Map);
SAMPLER(sampler_Color_Map);

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

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
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
float _Property_7689f3a0523b4af9912879a05d1d5206_Out_0_Float = _Sway_Wind_Speed;
float3 _Property_e0531013c46845b6b036ac453118c744_Out_0_Vector3 = _Sway_Wind_Direction;
float _Property_35588e6a16b5457695dba39b1644d669_Out_0_Float = _Sway_Wind_Intensity;
float _Property_c8d3d7e6e6f64d409f010d988b96e327_Out_0_Float = _Sway_Wind_Time_Random_Offset_Amount;
Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float _WindSway_662bcd4f8b2b4607b138cb44de00a708;
_WindSway_662bcd4f8b2b4607b138cb44de00a708.ObjectSpacePosition = IN.ObjectSpacePosition;
_WindSway_662bcd4f8b2b4607b138cb44de00a708.TimeParameters = IN.TimeParameters;
float3 _WindSway_662bcd4f8b2b4607b138cb44de00a708_OutVector3_1_Vector3;
SG_WindSway_51c4027ae50a8cd49be53c4990b9f179_float(_Property_7689f3a0523b4af9912879a05d1d5206_Out_0_Float, _Property_e0531013c46845b6b036ac453118c744_Out_0_Vector3, _Property_35588e6a16b5457695dba39b1644d669_Out_0_Float, _Property_c8d3d7e6e6f64d409f010d988b96e327_Out_0_Float, _WindSway_662bcd4f8b2b4607b138cb44de00a708, _WindSway_662bcd4f8b2b4607b138cb44de00a708_OutVector3_1_Vector3);
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_R_1_Float = IN.WorldSpacePosition[0];
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_G_2_Float = IN.WorldSpacePosition[1];
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_B_3_Float = IN.WorldSpacePosition[2];
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_A_4_Float = 0;
float3 _Vector3_e154b524054546d2bbeab293c0e20519_Out_0_Vector3 = float3(float(0), float(0), float(0));
float3 _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3;
_Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3 = TransformObjectToWorld(_Vector3_e154b524054546d2bbeab293c0e20519_Out_0_Vector3.xyz);
float _Split_5eb48153bba44ea2b128583819f09a11_R_1_Float = _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3[0];
float _Split_5eb48153bba44ea2b128583819f09a11_G_2_Float = _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3[1];
float _Split_5eb48153bba44ea2b128583819f09a11_B_3_Float = _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3[2];
float _Split_5eb48153bba44ea2b128583819f09a11_A_4_Float = 0;
float _Subtract_d6a99abdf4e74644acffc1718d1735bf_Out_2_Float;
Unity_Subtract_float(_Split_8fe962eb6f0f4d6fba734f9edcaf9067_G_2_Float, _Split_5eb48153bba44ea2b128583819f09a11_G_2_Float, _Subtract_d6a99abdf4e74644acffc1718d1735bf_Out_2_Float);
float _Property_1cf3cda52e094e638df491b003607dfd_Out_0_Float = _Height_Weight;
float _Split_145cf2161d4c4510b08248cad819f655_R_1_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[0];
float _Split_145cf2161d4c4510b08248cad819f655_G_2_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[1];
float _Split_145cf2161d4c4510b08248cad819f655_B_3_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[2];
float _Split_145cf2161d4c4510b08248cad819f655_A_4_Float = 0;
float _Multiply_2c140d573b9447aab058bd863fe2f4f7_Out_2_Float;
Unity_Multiply_float_float(_Property_1cf3cda52e094e638df491b003607dfd_Out_0_Float, _Split_145cf2161d4c4510b08248cad819f655_G_2_Float, _Multiply_2c140d573b9447aab058bd863fe2f4f7_Out_2_Float);
float _Divide_0f6a637143d841d0b1c94fa2ae3e164d_Out_2_Float;
Unity_Divide_float(_Subtract_d6a99abdf4e74644acffc1718d1735bf_Out_2_Float, _Multiply_2c140d573b9447aab058bd863fe2f4f7_Out_2_Float, _Divide_0f6a637143d841d0b1c94fa2ae3e164d_Out_2_Float);
float _Saturate_1e4e29fb5c074565a1f941c046957b05_Out_1_Float;
Unity_Saturate_float(_Divide_0f6a637143d841d0b1c94fa2ae3e164d_Out_2_Float, _Saturate_1e4e29fb5c074565a1f941c046957b05_Out_1_Float);
float _Property_3d64a0c8dd104507b9a0762a778fe585_Out_0_Float = _Height_Gradience;
float _Power_f1d74728398a45f9993fe3b3affb6584_Out_2_Float;
Unity_Power_float(_Saturate_1e4e29fb5c074565a1f941c046957b05_Out_1_Float, _Property_3d64a0c8dd104507b9a0762a778fe585_Out_0_Float, _Power_f1d74728398a45f9993fe3b3affb6584_Out_2_Float);
float _Saturate_57b130350452418fa1d09d4a0ddd64ac_Out_1_Float;
Unity_Saturate_float(_Power_f1d74728398a45f9993fe3b3affb6584_Out_2_Float, _Saturate_57b130350452418fa1d09d4a0ddd64ac_Out_1_Float);
float3 _Lerp_1fbf8d0a7f5a4e2ab381967ac90385c7_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _WindSway_662bcd4f8b2b4607b138cb44de00a708_OutVector3_1_Vector3, (_Saturate_57b130350452418fa1d09d4a0ddd64ac_Out_1_Float.xxx), _Lerp_1fbf8d0a7f5a4e2ab381967ac90385c7_Out_3_Vector3);
float _Split_bd04916788224fa2bb71be3f27eb839a_R_1_Float = IN.WorldSpacePosition[0];
float _Split_bd04916788224fa2bb71be3f27eb839a_G_2_Float = IN.WorldSpacePosition[1];
float _Split_bd04916788224fa2bb71be3f27eb839a_B_3_Float = IN.WorldSpacePosition[2];
float _Split_bd04916788224fa2bb71be3f27eb839a_A_4_Float = 0;
float4 _Combine_45cdab9c20414d03862100967f56c78d_RGBA_4_Vector4;
float3 _Combine_45cdab9c20414d03862100967f56c78d_RGB_5_Vector3;
float2 _Combine_45cdab9c20414d03862100967f56c78d_RG_6_Vector2;
Unity_Combine_float(_Split_bd04916788224fa2bb71be3f27eb839a_R_1_Float, _Split_bd04916788224fa2bb71be3f27eb839a_B_3_Float, float(0), float(0), _Combine_45cdab9c20414d03862100967f56c78d_RGBA_4_Vector4, _Combine_45cdab9c20414d03862100967f56c78d_RGB_5_Vector3, _Combine_45cdab9c20414d03862100967f56c78d_RG_6_Vector2);
float _Property_9b35f6244c0247c7a9be7af2671f58e7_Out_0_Float = _Wiggle_Wind_Size;
float2 _Multiply_ba4d7f376f904143acc9eb33d8414ff8_Out_2_Vector2;
Unity_Multiply_float2_float2(_Combine_45cdab9c20414d03862100967f56c78d_RG_6_Vector2, (_Property_9b35f6244c0247c7a9be7af2671f58e7_Out_0_Float.xx), _Multiply_ba4d7f376f904143acc9eb33d8414ff8_Out_2_Vector2);
float _Property_6eae25110c184e7a94293b95ddc9ecef_Out_0_Float = _Wiggle_Wind_Speed_Small;
float _Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Property_6eae25110c184e7a94293b95ddc9ecef_Out_0_Float, _Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float);
float _Multiply_7510d5a95e6c4d3986bb83059d23d2fa_Out_2_Float;
Unity_Multiply_float_float(_Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float, 0.8, _Multiply_7510d5a95e6c4d3986bb83059d23d2fa_Out_2_Float);
float2 _Vector2_cb26bc8c681b455c81073f0dc3c8f6b4_Out_0_Vector2 = float2(_Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float, _Multiply_7510d5a95e6c4d3986bb83059d23d2fa_Out_2_Float);
float2 _TilingAndOffset_e5f47c2cf9d846859a72aadcdac7fc3b_Out_3_Vector2;
Unity_TilingAndOffset_float(_Multiply_ba4d7f376f904143acc9eb33d8414ff8_Out_2_Vector2, float2 (1, 1), _Vector2_cb26bc8c681b455c81073f0dc3c8f6b4_Out_0_Vector2, _TilingAndOffset_e5f47c2cf9d846859a72aadcdac7fc3b_Out_3_Vector2);
  float4 _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).tex, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).GetTransformedUV(_TilingAndOffset_e5f47c2cf9d846859a72aadcdac7fc3b_Out_3_Vector2), float(2));
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_R_5_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_G_6_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_B_7_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_A_8_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.a;
float _Float_3f1a7b5619414946b13e7cef19336d45_Out_0_Float = float(0.3);
float _Subtract_3105ef040f344481afb342cd9ef65e9f_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_R_5_Float, _Float_3f1a7b5619414946b13e7cef19336d45_Out_0_Float, _Subtract_3105ef040f344481afb342cd9ef65e9f_Out_2_Float);
float _Split_18495110217342bc80044d9345d98fbd_R_1_Float = IN.WorldSpacePosition[0];
float _Split_18495110217342bc80044d9345d98fbd_G_2_Float = IN.WorldSpacePosition[1];
float _Split_18495110217342bc80044d9345d98fbd_B_3_Float = IN.WorldSpacePosition[2];
float _Split_18495110217342bc80044d9345d98fbd_A_4_Float = 0;
float4 _Combine_d1ee3e49696840daa7cfd43df2287807_RGBA_4_Vector4;
float3 _Combine_d1ee3e49696840daa7cfd43df2287807_RGB_5_Vector3;
float2 _Combine_d1ee3e49696840daa7cfd43df2287807_RG_6_Vector2;
Unity_Combine_float(_Split_18495110217342bc80044d9345d98fbd_R_1_Float, _Split_18495110217342bc80044d9345d98fbd_G_2_Float, float(0), float(0), _Combine_d1ee3e49696840daa7cfd43df2287807_RGBA_4_Vector4, _Combine_d1ee3e49696840daa7cfd43df2287807_RGB_5_Vector3, _Combine_d1ee3e49696840daa7cfd43df2287807_RG_6_Vector2);
float _Property_9efe7c2851414ea39b6971daac6143e5_Out_0_Float = _Wiggle_Wind_Size;
float _Multiply_4f6b361114004341aeae12987f071375_Out_2_Float;
Unity_Multiply_float_float(_Property_9efe7c2851414ea39b6971daac6143e5_Out_0_Float, 0.763, _Multiply_4f6b361114004341aeae12987f071375_Out_2_Float);
float2 _Multiply_0d88e33790f0458b9e6e7f49946e6344_Out_2_Vector2;
Unity_Multiply_float2_float2(_Combine_d1ee3e49696840daa7cfd43df2287807_RG_6_Vector2, (_Multiply_4f6b361114004341aeae12987f071375_Out_2_Float.xx), _Multiply_0d88e33790f0458b9e6e7f49946e6344_Out_2_Vector2);
float _Property_56c994a79361438d88ea30b1e3ccf2aa_Out_0_Float = _Wiggle_Wind_Speed_Large;
float _Multiply_c5e5feed518642678424c6f0d45b4377_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Property_56c994a79361438d88ea30b1e3ccf2aa_Out_0_Float, _Multiply_c5e5feed518642678424c6f0d45b4377_Out_2_Float);
float2 _TilingAndOffset_7dac505987c144edad083d13ebca67e6_Out_3_Vector2;
Unity_TilingAndOffset_float(_Multiply_0d88e33790f0458b9e6e7f49946e6344_Out_2_Vector2, float2 (1, 1), (_Multiply_c5e5feed518642678424c6f0d45b4377_Out_2_Float.xx), _TilingAndOffset_7dac505987c144edad083d13ebca67e6_Out_3_Vector2);
  float4 _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).tex, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).GetTransformedUV(_TilingAndOffset_7dac505987c144edad083d13ebca67e6_Out_3_Vector2), float(2));
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_R_5_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_G_6_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_B_7_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_A_8_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.a;
float _Subtract_e29fd5b0daea41a1a38ac52280e6db88_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_G_6_Float, _Float_3f1a7b5619414946b13e7cef19336d45_Out_0_Float, _Subtract_e29fd5b0daea41a1a38ac52280e6db88_Out_2_Float);
float _Add_6d51be6104ee4e23a093dfb9c3d993f7_Out_2_Float;
Unity_Add_float(_Subtract_3105ef040f344481afb342cd9ef65e9f_Out_2_Float, _Subtract_e29fd5b0daea41a1a38ac52280e6db88_Out_2_Float, _Add_6d51be6104ee4e23a093dfb9c3d993f7_Out_2_Float);
float4 _Multiply_db043a8b3fc34edb8bee433085c7e061_Out_2_Vector4;
Unity_Multiply_float4_float4((_Add_6d51be6104ee4e23a093dfb9c3d993f7_Out_2_Float.xxxx), IN.VertexColor, _Multiply_db043a8b3fc34edb8bee433085c7e061_Out_2_Vector4);
float _Property_46c8c648052246a4a32e80905ed7e9df_Out_0_Float = _Wiggle_Wind_Intensity;
float4 _Multiply_4baa80310c414483a2aada72baa7a883_Out_2_Vector4;
Unity_Multiply_float4_float4(_Multiply_db043a8b3fc34edb8bee433085c7e061_Out_2_Vector4, (_Property_46c8c648052246a4a32e80905ed7e9df_Out_0_Float.xxxx), _Multiply_4baa80310c414483a2aada72baa7a883_Out_2_Vector4);
float3 _Multiply_2c95a93020c44a20be82a31b985c35b8_Out_2_Vector3;
Unity_Multiply_float3_float3((_Multiply_4baa80310c414483a2aada72baa7a883_Out_2_Vector4.xyz), IN.ObjectSpaceNormal, _Multiply_2c95a93020c44a20be82a31b985c35b8_Out_2_Vector3);
float3 _Add_5a143eeddea74bceb3ddd5b059f49da5_Out_2_Vector3;
Unity_Add_float3(_Lerp_1fbf8d0a7f5a4e2ab381967ac90385c7_Out_3_Vector3, _Multiply_2c95a93020c44a20be82a31b985c35b8_Out_2_Vector3, _Add_5a143eeddea74bceb3ddd5b059f49da5_Out_2_Vector3);
#if defined(_USE_WPO)
float3 _UseWPO_1f28bdd134c9432e812aded965021562_Out_0_Vector3 = _Add_5a143eeddea74bceb3ddd5b059f49da5_Out_2_Vector3;
#else
float3 _UseWPO_1f28bdd134c9432e812aded965021562_Out_0_Vector3 = IN.ObjectSpacePosition;
#endif
float3 _UPGFoliageContactPositionCustomFunction_3446fb1a0c894696bd903b7eb184e241_bentPositionOS_1_Vector3;
UPG_FoliageContactPosition_float(_UseWPO_1f28bdd134c9432e812aded965021562_Out_0_Vector3, _UPGFoliageContactPositionCustomFunction_3446fb1a0c894696bd903b7eb184e241_bentPositionOS_1_Vector3);
description.Position = _UPGFoliageContactPositionCustomFunction_3446fb1a0c894696bd903b7eb184e241_bentPositionOS_1_Vector3;
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
UnityTexture2D _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Color_Map, sampler_Color_Map, _Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_eaea7ba5e1e74519a1bc7833f0103ab8_Out_0_Vector4 = IN.uv0;
float _Property_95352575ae4140008287fd58138e8eb5_Out_0_Float = _Color_Map_Tiling;
float4 _Multiply_86fafeae394745fe880655d03cafd9e1_Out_2_Vector4;
Unity_Multiply_float4_float4(_UV_eaea7ba5e1e74519a1bc7833f0103ab8_Out_0_Vector4, (_Property_95352575ae4140008287fd58138e8eb5_Out_0_Float.xxxx), _Multiply_86fafeae394745fe880655d03cafd9e1_Out_2_Vector4);
float4 _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.tex, _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.samplerstate, _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.GetTransformedUV((_Multiply_86fafeae394745fe880655d03cafd9e1_Out_2_Vector4.xy)) );
if (_Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4, _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_R_4_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.r;
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_G_5_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.g;
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_B_6_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.b;
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_A_7_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.a;
float _Multiply_cf1ea983d11e4ab7b4bdbbb064ad94b5_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_A_7_Float, 1, _Multiply_cf1ea983d11e4ab7b4bdbbb064ad94b5_Out_2_Float);
float _Property_99bb0b22edbb4ea0b1b7844414b876a7_Out_0_Float = _Alpha_Clip;
surface.Alpha = _Multiply_cf1ea983d11e4ab7b4bdbbb064ad94b5_Out_2_Float;
surface.AlphaClipThreshold = _Property_99bb0b22edbb4ea0b1b7844414b876a7_Out_0_Float;
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
Cull Off
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
#pragma shader_feature_local _ _USE_WPO
#pragma shader_feature_local _ _USE_HEIGHT_GRADIENCE

#if defined(_USE_WPO) && defined(_USE_HEIGHT_GRADIENCE)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_WPO)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_HEIGHT_GRADIENCE)
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
#define VARYINGS_NEED_TEXCOORD0
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_DEPTHONLY
#define BUILTIN_TARGET_API 1
#define _BUILTIN_AlphaClip 1
#define _BUILTIN_ALPHATEST_ON 1
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

float4 _Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D_TexelSize;
float _Subsurface_Fresnel_Transition_Smoothness;
float _Subsurface_Fresnel_Pow;
float _Subsurface_Fresnel_Intensity;
float _Subsurface_Transition_Smoothness;
float _Wiggle_Wind_Speed_Large;
float _Wiggle_Wind_Intensity;
float _Roughness;
float4 _Foliage_Color_Bottom;
float _Sway_Wind_Speed;
float _Gradience_Power;
float _Fresnel_Mul;
float4 _Foliage_Color_Top;
float _Always_On_Fresnel_Intensity;
float _Tree_Height;
float4 _Color_Map_TexelSize;
float _Height_Weight;
float _Height_Gradience;
float _Sway_Wind_Time_Random_Offset_Amount;
float _Sway_Wind_Intensity;
float3 _Sway_Wind_Direction;
float _Color_Map_Tiling;
float _Subsurface_Intensity;
float _Alpha_Clip;
float _Wiggle_Wind_Size;
float _Wiggle_Wind_Speed_Small;
float _Fresnel_Pow;
float _Always_On_Fresnel_Pow;
float4 _Always_On_Fresnel_Color;
float _ree;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D);
SAMPLER(sampler_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D);
TEXTURE2D(_Color_Map);
SAMPLER(sampler_Color_Map);

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

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
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
float _Property_7689f3a0523b4af9912879a05d1d5206_Out_0_Float = _Sway_Wind_Speed;
float3 _Property_e0531013c46845b6b036ac453118c744_Out_0_Vector3 = _Sway_Wind_Direction;
float _Property_35588e6a16b5457695dba39b1644d669_Out_0_Float = _Sway_Wind_Intensity;
float _Property_c8d3d7e6e6f64d409f010d988b96e327_Out_0_Float = _Sway_Wind_Time_Random_Offset_Amount;
Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float _WindSway_662bcd4f8b2b4607b138cb44de00a708;
_WindSway_662bcd4f8b2b4607b138cb44de00a708.ObjectSpacePosition = IN.ObjectSpacePosition;
_WindSway_662bcd4f8b2b4607b138cb44de00a708.TimeParameters = IN.TimeParameters;
float3 _WindSway_662bcd4f8b2b4607b138cb44de00a708_OutVector3_1_Vector3;
SG_WindSway_51c4027ae50a8cd49be53c4990b9f179_float(_Property_7689f3a0523b4af9912879a05d1d5206_Out_0_Float, _Property_e0531013c46845b6b036ac453118c744_Out_0_Vector3, _Property_35588e6a16b5457695dba39b1644d669_Out_0_Float, _Property_c8d3d7e6e6f64d409f010d988b96e327_Out_0_Float, _WindSway_662bcd4f8b2b4607b138cb44de00a708, _WindSway_662bcd4f8b2b4607b138cb44de00a708_OutVector3_1_Vector3);
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_R_1_Float = IN.WorldSpacePosition[0];
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_G_2_Float = IN.WorldSpacePosition[1];
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_B_3_Float = IN.WorldSpacePosition[2];
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_A_4_Float = 0;
float3 _Vector3_e154b524054546d2bbeab293c0e20519_Out_0_Vector3 = float3(float(0), float(0), float(0));
float3 _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3;
_Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3 = TransformObjectToWorld(_Vector3_e154b524054546d2bbeab293c0e20519_Out_0_Vector3.xyz);
float _Split_5eb48153bba44ea2b128583819f09a11_R_1_Float = _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3[0];
float _Split_5eb48153bba44ea2b128583819f09a11_G_2_Float = _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3[1];
float _Split_5eb48153bba44ea2b128583819f09a11_B_3_Float = _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3[2];
float _Split_5eb48153bba44ea2b128583819f09a11_A_4_Float = 0;
float _Subtract_d6a99abdf4e74644acffc1718d1735bf_Out_2_Float;
Unity_Subtract_float(_Split_8fe962eb6f0f4d6fba734f9edcaf9067_G_2_Float, _Split_5eb48153bba44ea2b128583819f09a11_G_2_Float, _Subtract_d6a99abdf4e74644acffc1718d1735bf_Out_2_Float);
float _Property_1cf3cda52e094e638df491b003607dfd_Out_0_Float = _Height_Weight;
float _Split_145cf2161d4c4510b08248cad819f655_R_1_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[0];
float _Split_145cf2161d4c4510b08248cad819f655_G_2_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[1];
float _Split_145cf2161d4c4510b08248cad819f655_B_3_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[2];
float _Split_145cf2161d4c4510b08248cad819f655_A_4_Float = 0;
float _Multiply_2c140d573b9447aab058bd863fe2f4f7_Out_2_Float;
Unity_Multiply_float_float(_Property_1cf3cda52e094e638df491b003607dfd_Out_0_Float, _Split_145cf2161d4c4510b08248cad819f655_G_2_Float, _Multiply_2c140d573b9447aab058bd863fe2f4f7_Out_2_Float);
float _Divide_0f6a637143d841d0b1c94fa2ae3e164d_Out_2_Float;
Unity_Divide_float(_Subtract_d6a99abdf4e74644acffc1718d1735bf_Out_2_Float, _Multiply_2c140d573b9447aab058bd863fe2f4f7_Out_2_Float, _Divide_0f6a637143d841d0b1c94fa2ae3e164d_Out_2_Float);
float _Saturate_1e4e29fb5c074565a1f941c046957b05_Out_1_Float;
Unity_Saturate_float(_Divide_0f6a637143d841d0b1c94fa2ae3e164d_Out_2_Float, _Saturate_1e4e29fb5c074565a1f941c046957b05_Out_1_Float);
float _Property_3d64a0c8dd104507b9a0762a778fe585_Out_0_Float = _Height_Gradience;
float _Power_f1d74728398a45f9993fe3b3affb6584_Out_2_Float;
Unity_Power_float(_Saturate_1e4e29fb5c074565a1f941c046957b05_Out_1_Float, _Property_3d64a0c8dd104507b9a0762a778fe585_Out_0_Float, _Power_f1d74728398a45f9993fe3b3affb6584_Out_2_Float);
float _Saturate_57b130350452418fa1d09d4a0ddd64ac_Out_1_Float;
Unity_Saturate_float(_Power_f1d74728398a45f9993fe3b3affb6584_Out_2_Float, _Saturate_57b130350452418fa1d09d4a0ddd64ac_Out_1_Float);
float3 _Lerp_1fbf8d0a7f5a4e2ab381967ac90385c7_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _WindSway_662bcd4f8b2b4607b138cb44de00a708_OutVector3_1_Vector3, (_Saturate_57b130350452418fa1d09d4a0ddd64ac_Out_1_Float.xxx), _Lerp_1fbf8d0a7f5a4e2ab381967ac90385c7_Out_3_Vector3);
float _Split_bd04916788224fa2bb71be3f27eb839a_R_1_Float = IN.WorldSpacePosition[0];
float _Split_bd04916788224fa2bb71be3f27eb839a_G_2_Float = IN.WorldSpacePosition[1];
float _Split_bd04916788224fa2bb71be3f27eb839a_B_3_Float = IN.WorldSpacePosition[2];
float _Split_bd04916788224fa2bb71be3f27eb839a_A_4_Float = 0;
float4 _Combine_45cdab9c20414d03862100967f56c78d_RGBA_4_Vector4;
float3 _Combine_45cdab9c20414d03862100967f56c78d_RGB_5_Vector3;
float2 _Combine_45cdab9c20414d03862100967f56c78d_RG_6_Vector2;
Unity_Combine_float(_Split_bd04916788224fa2bb71be3f27eb839a_R_1_Float, _Split_bd04916788224fa2bb71be3f27eb839a_B_3_Float, float(0), float(0), _Combine_45cdab9c20414d03862100967f56c78d_RGBA_4_Vector4, _Combine_45cdab9c20414d03862100967f56c78d_RGB_5_Vector3, _Combine_45cdab9c20414d03862100967f56c78d_RG_6_Vector2);
float _Property_9b35f6244c0247c7a9be7af2671f58e7_Out_0_Float = _Wiggle_Wind_Size;
float2 _Multiply_ba4d7f376f904143acc9eb33d8414ff8_Out_2_Vector2;
Unity_Multiply_float2_float2(_Combine_45cdab9c20414d03862100967f56c78d_RG_6_Vector2, (_Property_9b35f6244c0247c7a9be7af2671f58e7_Out_0_Float.xx), _Multiply_ba4d7f376f904143acc9eb33d8414ff8_Out_2_Vector2);
float _Property_6eae25110c184e7a94293b95ddc9ecef_Out_0_Float = _Wiggle_Wind_Speed_Small;
float _Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Property_6eae25110c184e7a94293b95ddc9ecef_Out_0_Float, _Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float);
float _Multiply_7510d5a95e6c4d3986bb83059d23d2fa_Out_2_Float;
Unity_Multiply_float_float(_Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float, 0.8, _Multiply_7510d5a95e6c4d3986bb83059d23d2fa_Out_2_Float);
float2 _Vector2_cb26bc8c681b455c81073f0dc3c8f6b4_Out_0_Vector2 = float2(_Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float, _Multiply_7510d5a95e6c4d3986bb83059d23d2fa_Out_2_Float);
float2 _TilingAndOffset_e5f47c2cf9d846859a72aadcdac7fc3b_Out_3_Vector2;
Unity_TilingAndOffset_float(_Multiply_ba4d7f376f904143acc9eb33d8414ff8_Out_2_Vector2, float2 (1, 1), _Vector2_cb26bc8c681b455c81073f0dc3c8f6b4_Out_0_Vector2, _TilingAndOffset_e5f47c2cf9d846859a72aadcdac7fc3b_Out_3_Vector2);
  float4 _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).tex, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).GetTransformedUV(_TilingAndOffset_e5f47c2cf9d846859a72aadcdac7fc3b_Out_3_Vector2), float(2));
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_R_5_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_G_6_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_B_7_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_A_8_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.a;
float _Float_3f1a7b5619414946b13e7cef19336d45_Out_0_Float = float(0.3);
float _Subtract_3105ef040f344481afb342cd9ef65e9f_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_R_5_Float, _Float_3f1a7b5619414946b13e7cef19336d45_Out_0_Float, _Subtract_3105ef040f344481afb342cd9ef65e9f_Out_2_Float);
float _Split_18495110217342bc80044d9345d98fbd_R_1_Float = IN.WorldSpacePosition[0];
float _Split_18495110217342bc80044d9345d98fbd_G_2_Float = IN.WorldSpacePosition[1];
float _Split_18495110217342bc80044d9345d98fbd_B_3_Float = IN.WorldSpacePosition[2];
float _Split_18495110217342bc80044d9345d98fbd_A_4_Float = 0;
float4 _Combine_d1ee3e49696840daa7cfd43df2287807_RGBA_4_Vector4;
float3 _Combine_d1ee3e49696840daa7cfd43df2287807_RGB_5_Vector3;
float2 _Combine_d1ee3e49696840daa7cfd43df2287807_RG_6_Vector2;
Unity_Combine_float(_Split_18495110217342bc80044d9345d98fbd_R_1_Float, _Split_18495110217342bc80044d9345d98fbd_G_2_Float, float(0), float(0), _Combine_d1ee3e49696840daa7cfd43df2287807_RGBA_4_Vector4, _Combine_d1ee3e49696840daa7cfd43df2287807_RGB_5_Vector3, _Combine_d1ee3e49696840daa7cfd43df2287807_RG_6_Vector2);
float _Property_9efe7c2851414ea39b6971daac6143e5_Out_0_Float = _Wiggle_Wind_Size;
float _Multiply_4f6b361114004341aeae12987f071375_Out_2_Float;
Unity_Multiply_float_float(_Property_9efe7c2851414ea39b6971daac6143e5_Out_0_Float, 0.763, _Multiply_4f6b361114004341aeae12987f071375_Out_2_Float);
float2 _Multiply_0d88e33790f0458b9e6e7f49946e6344_Out_2_Vector2;
Unity_Multiply_float2_float2(_Combine_d1ee3e49696840daa7cfd43df2287807_RG_6_Vector2, (_Multiply_4f6b361114004341aeae12987f071375_Out_2_Float.xx), _Multiply_0d88e33790f0458b9e6e7f49946e6344_Out_2_Vector2);
float _Property_56c994a79361438d88ea30b1e3ccf2aa_Out_0_Float = _Wiggle_Wind_Speed_Large;
float _Multiply_c5e5feed518642678424c6f0d45b4377_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Property_56c994a79361438d88ea30b1e3ccf2aa_Out_0_Float, _Multiply_c5e5feed518642678424c6f0d45b4377_Out_2_Float);
float2 _TilingAndOffset_7dac505987c144edad083d13ebca67e6_Out_3_Vector2;
Unity_TilingAndOffset_float(_Multiply_0d88e33790f0458b9e6e7f49946e6344_Out_2_Vector2, float2 (1, 1), (_Multiply_c5e5feed518642678424c6f0d45b4377_Out_2_Float.xx), _TilingAndOffset_7dac505987c144edad083d13ebca67e6_Out_3_Vector2);
  float4 _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).tex, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).GetTransformedUV(_TilingAndOffset_7dac505987c144edad083d13ebca67e6_Out_3_Vector2), float(2));
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_R_5_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_G_6_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_B_7_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_A_8_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.a;
float _Subtract_e29fd5b0daea41a1a38ac52280e6db88_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_G_6_Float, _Float_3f1a7b5619414946b13e7cef19336d45_Out_0_Float, _Subtract_e29fd5b0daea41a1a38ac52280e6db88_Out_2_Float);
float _Add_6d51be6104ee4e23a093dfb9c3d993f7_Out_2_Float;
Unity_Add_float(_Subtract_3105ef040f344481afb342cd9ef65e9f_Out_2_Float, _Subtract_e29fd5b0daea41a1a38ac52280e6db88_Out_2_Float, _Add_6d51be6104ee4e23a093dfb9c3d993f7_Out_2_Float);
float4 _Multiply_db043a8b3fc34edb8bee433085c7e061_Out_2_Vector4;
Unity_Multiply_float4_float4((_Add_6d51be6104ee4e23a093dfb9c3d993f7_Out_2_Float.xxxx), IN.VertexColor, _Multiply_db043a8b3fc34edb8bee433085c7e061_Out_2_Vector4);
float _Property_46c8c648052246a4a32e80905ed7e9df_Out_0_Float = _Wiggle_Wind_Intensity;
float4 _Multiply_4baa80310c414483a2aada72baa7a883_Out_2_Vector4;
Unity_Multiply_float4_float4(_Multiply_db043a8b3fc34edb8bee433085c7e061_Out_2_Vector4, (_Property_46c8c648052246a4a32e80905ed7e9df_Out_0_Float.xxxx), _Multiply_4baa80310c414483a2aada72baa7a883_Out_2_Vector4);
float3 _Multiply_2c95a93020c44a20be82a31b985c35b8_Out_2_Vector3;
Unity_Multiply_float3_float3((_Multiply_4baa80310c414483a2aada72baa7a883_Out_2_Vector4.xyz), IN.ObjectSpaceNormal, _Multiply_2c95a93020c44a20be82a31b985c35b8_Out_2_Vector3);
float3 _Add_5a143eeddea74bceb3ddd5b059f49da5_Out_2_Vector3;
Unity_Add_float3(_Lerp_1fbf8d0a7f5a4e2ab381967ac90385c7_Out_3_Vector3, _Multiply_2c95a93020c44a20be82a31b985c35b8_Out_2_Vector3, _Add_5a143eeddea74bceb3ddd5b059f49da5_Out_2_Vector3);
#if defined(_USE_WPO)
float3 _UseWPO_1f28bdd134c9432e812aded965021562_Out_0_Vector3 = _Add_5a143eeddea74bceb3ddd5b059f49da5_Out_2_Vector3;
#else
float3 _UseWPO_1f28bdd134c9432e812aded965021562_Out_0_Vector3 = IN.ObjectSpacePosition;
#endif
float3 _UPGFoliageContactPositionCustomFunction_3446fb1a0c894696bd903b7eb184e241_bentPositionOS_1_Vector3;
UPG_FoliageContactPosition_float(_UseWPO_1f28bdd134c9432e812aded965021562_Out_0_Vector3, _UPGFoliageContactPositionCustomFunction_3446fb1a0c894696bd903b7eb184e241_bentPositionOS_1_Vector3);
description.Position = _UPGFoliageContactPositionCustomFunction_3446fb1a0c894696bd903b7eb184e241_bentPositionOS_1_Vector3;
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
UnityTexture2D _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Color_Map, sampler_Color_Map, _Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_eaea7ba5e1e74519a1bc7833f0103ab8_Out_0_Vector4 = IN.uv0;
float _Property_95352575ae4140008287fd58138e8eb5_Out_0_Float = _Color_Map_Tiling;
float4 _Multiply_86fafeae394745fe880655d03cafd9e1_Out_2_Vector4;
Unity_Multiply_float4_float4(_UV_eaea7ba5e1e74519a1bc7833f0103ab8_Out_0_Vector4, (_Property_95352575ae4140008287fd58138e8eb5_Out_0_Float.xxxx), _Multiply_86fafeae394745fe880655d03cafd9e1_Out_2_Vector4);
float4 _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.tex, _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.samplerstate, _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.GetTransformedUV((_Multiply_86fafeae394745fe880655d03cafd9e1_Out_2_Vector4.xy)) );
if (_Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4, _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_R_4_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.r;
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_G_5_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.g;
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_B_6_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.b;
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_A_7_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.a;
float _Multiply_cf1ea983d11e4ab7b4bdbbb064ad94b5_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_A_7_Float, 1, _Multiply_cf1ea983d11e4ab7b4bdbbb064ad94b5_Out_2_Float);
float _Property_99bb0b22edbb4ea0b1b7844414b876a7_Out_0_Float = _Alpha_Clip;
surface.Alpha = _Multiply_cf1ea983d11e4ab7b4bdbbb064ad94b5_Out_2_Float;
surface.AlphaClipThreshold = _Property_99bb0b22edbb4ea0b1b7844414b876a7_Out_0_Float;
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
#pragma shader_feature_local _ _USE_WPO
#pragma shader_feature_local _ _USE_HEIGHT_GRADIENCE

#if defined(_USE_WPO) && defined(_USE_HEIGHT_GRADIENCE)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_WPO)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_HEIGHT_GRADIENCE)
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
#define ATTRIBUTES_NEED_COLOR
#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_NORMAL_WS
#define VARYINGS_NEED_TEXCOORD0
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_META
#define BUILTIN_TARGET_API 1
#define _BUILTIN_AlphaClip 1
#define _BUILTIN_ALPHATEST_ON 1
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
 float3 WorldSpaceViewDirection;
 float3 WorldSpacePosition;
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 WorldSpacePosition;
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

float4 _Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D_TexelSize;
float _Subsurface_Fresnel_Transition_Smoothness;
float _Subsurface_Fresnel_Pow;
float _Subsurface_Fresnel_Intensity;
float _Subsurface_Transition_Smoothness;
float _Wiggle_Wind_Speed_Large;
float _Wiggle_Wind_Intensity;
float _Roughness;
float4 _Foliage_Color_Bottom;
float _Sway_Wind_Speed;
float _Gradience_Power;
float _Fresnel_Mul;
float4 _Foliage_Color_Top;
float _Always_On_Fresnel_Intensity;
float _Tree_Height;
float4 _Color_Map_TexelSize;
float _Height_Weight;
float _Height_Gradience;
float _Sway_Wind_Time_Random_Offset_Amount;
float _Sway_Wind_Intensity;
float3 _Sway_Wind_Direction;
float _Color_Map_Tiling;
float _Subsurface_Intensity;
float _Alpha_Clip;
float _Wiggle_Wind_Size;
float _Wiggle_Wind_Speed_Small;
float _Fresnel_Pow;
float _Always_On_Fresnel_Pow;
float4 _Always_On_Fresnel_Color;
float _ree;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D);
SAMPLER(sampler_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D);
TEXTURE2D(_Color_Map);
SAMPLER(sampler_Color_Map);

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

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Saturate_float4(float4 In, out float4 Out)
{
    Out = saturate(In);
}

void Unity_Normalize_float3(float3 In, out float3 Out)
{
    Out = normalize(In);
}

void Unity_DotProduct_float3(float3 A, float3 B, out float Out)
{
    Out = dot(A, B);
}

void Unity_Absolute_float(float In, out float Out)
{
    Out = abs(In);
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
}

// unity-custom-func-begin
void MainLight_float(out float3 Direction){

#ifdef UNITY_SHADER_GRAPH
	Direction = _WorldSpaceLightPos0.xyz;
#else
	Direction = float3(1,1,1);
#endif
}
// unity-custom-func-end

void Unity_DotProduct_float2(float2 A, float2 B, out float Out)
{
    Out = dot(A, B);
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
float _Property_7689f3a0523b4af9912879a05d1d5206_Out_0_Float = _Sway_Wind_Speed;
float3 _Property_e0531013c46845b6b036ac453118c744_Out_0_Vector3 = _Sway_Wind_Direction;
float _Property_35588e6a16b5457695dba39b1644d669_Out_0_Float = _Sway_Wind_Intensity;
float _Property_c8d3d7e6e6f64d409f010d988b96e327_Out_0_Float = _Sway_Wind_Time_Random_Offset_Amount;
Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float _WindSway_662bcd4f8b2b4607b138cb44de00a708;
_WindSway_662bcd4f8b2b4607b138cb44de00a708.ObjectSpacePosition = IN.ObjectSpacePosition;
_WindSway_662bcd4f8b2b4607b138cb44de00a708.TimeParameters = IN.TimeParameters;
float3 _WindSway_662bcd4f8b2b4607b138cb44de00a708_OutVector3_1_Vector3;
SG_WindSway_51c4027ae50a8cd49be53c4990b9f179_float(_Property_7689f3a0523b4af9912879a05d1d5206_Out_0_Float, _Property_e0531013c46845b6b036ac453118c744_Out_0_Vector3, _Property_35588e6a16b5457695dba39b1644d669_Out_0_Float, _Property_c8d3d7e6e6f64d409f010d988b96e327_Out_0_Float, _WindSway_662bcd4f8b2b4607b138cb44de00a708, _WindSway_662bcd4f8b2b4607b138cb44de00a708_OutVector3_1_Vector3);
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_R_1_Float = IN.WorldSpacePosition[0];
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_G_2_Float = IN.WorldSpacePosition[1];
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_B_3_Float = IN.WorldSpacePosition[2];
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_A_4_Float = 0;
float3 _Vector3_e154b524054546d2bbeab293c0e20519_Out_0_Vector3 = float3(float(0), float(0), float(0));
float3 _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3;
_Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3 = TransformObjectToWorld(_Vector3_e154b524054546d2bbeab293c0e20519_Out_0_Vector3.xyz);
float _Split_5eb48153bba44ea2b128583819f09a11_R_1_Float = _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3[0];
float _Split_5eb48153bba44ea2b128583819f09a11_G_2_Float = _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3[1];
float _Split_5eb48153bba44ea2b128583819f09a11_B_3_Float = _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3[2];
float _Split_5eb48153bba44ea2b128583819f09a11_A_4_Float = 0;
float _Subtract_d6a99abdf4e74644acffc1718d1735bf_Out_2_Float;
Unity_Subtract_float(_Split_8fe962eb6f0f4d6fba734f9edcaf9067_G_2_Float, _Split_5eb48153bba44ea2b128583819f09a11_G_2_Float, _Subtract_d6a99abdf4e74644acffc1718d1735bf_Out_2_Float);
float _Property_1cf3cda52e094e638df491b003607dfd_Out_0_Float = _Height_Weight;
float _Split_145cf2161d4c4510b08248cad819f655_R_1_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[0];
float _Split_145cf2161d4c4510b08248cad819f655_G_2_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[1];
float _Split_145cf2161d4c4510b08248cad819f655_B_3_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[2];
float _Split_145cf2161d4c4510b08248cad819f655_A_4_Float = 0;
float _Multiply_2c140d573b9447aab058bd863fe2f4f7_Out_2_Float;
Unity_Multiply_float_float(_Property_1cf3cda52e094e638df491b003607dfd_Out_0_Float, _Split_145cf2161d4c4510b08248cad819f655_G_2_Float, _Multiply_2c140d573b9447aab058bd863fe2f4f7_Out_2_Float);
float _Divide_0f6a637143d841d0b1c94fa2ae3e164d_Out_2_Float;
Unity_Divide_float(_Subtract_d6a99abdf4e74644acffc1718d1735bf_Out_2_Float, _Multiply_2c140d573b9447aab058bd863fe2f4f7_Out_2_Float, _Divide_0f6a637143d841d0b1c94fa2ae3e164d_Out_2_Float);
float _Saturate_1e4e29fb5c074565a1f941c046957b05_Out_1_Float;
Unity_Saturate_float(_Divide_0f6a637143d841d0b1c94fa2ae3e164d_Out_2_Float, _Saturate_1e4e29fb5c074565a1f941c046957b05_Out_1_Float);
float _Property_3d64a0c8dd104507b9a0762a778fe585_Out_0_Float = _Height_Gradience;
float _Power_f1d74728398a45f9993fe3b3affb6584_Out_2_Float;
Unity_Power_float(_Saturate_1e4e29fb5c074565a1f941c046957b05_Out_1_Float, _Property_3d64a0c8dd104507b9a0762a778fe585_Out_0_Float, _Power_f1d74728398a45f9993fe3b3affb6584_Out_2_Float);
float _Saturate_57b130350452418fa1d09d4a0ddd64ac_Out_1_Float;
Unity_Saturate_float(_Power_f1d74728398a45f9993fe3b3affb6584_Out_2_Float, _Saturate_57b130350452418fa1d09d4a0ddd64ac_Out_1_Float);
float3 _Lerp_1fbf8d0a7f5a4e2ab381967ac90385c7_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _WindSway_662bcd4f8b2b4607b138cb44de00a708_OutVector3_1_Vector3, (_Saturate_57b130350452418fa1d09d4a0ddd64ac_Out_1_Float.xxx), _Lerp_1fbf8d0a7f5a4e2ab381967ac90385c7_Out_3_Vector3);
float _Split_bd04916788224fa2bb71be3f27eb839a_R_1_Float = IN.WorldSpacePosition[0];
float _Split_bd04916788224fa2bb71be3f27eb839a_G_2_Float = IN.WorldSpacePosition[1];
float _Split_bd04916788224fa2bb71be3f27eb839a_B_3_Float = IN.WorldSpacePosition[2];
float _Split_bd04916788224fa2bb71be3f27eb839a_A_4_Float = 0;
float4 _Combine_45cdab9c20414d03862100967f56c78d_RGBA_4_Vector4;
float3 _Combine_45cdab9c20414d03862100967f56c78d_RGB_5_Vector3;
float2 _Combine_45cdab9c20414d03862100967f56c78d_RG_6_Vector2;
Unity_Combine_float(_Split_bd04916788224fa2bb71be3f27eb839a_R_1_Float, _Split_bd04916788224fa2bb71be3f27eb839a_B_3_Float, float(0), float(0), _Combine_45cdab9c20414d03862100967f56c78d_RGBA_4_Vector4, _Combine_45cdab9c20414d03862100967f56c78d_RGB_5_Vector3, _Combine_45cdab9c20414d03862100967f56c78d_RG_6_Vector2);
float _Property_9b35f6244c0247c7a9be7af2671f58e7_Out_0_Float = _Wiggle_Wind_Size;
float2 _Multiply_ba4d7f376f904143acc9eb33d8414ff8_Out_2_Vector2;
Unity_Multiply_float2_float2(_Combine_45cdab9c20414d03862100967f56c78d_RG_6_Vector2, (_Property_9b35f6244c0247c7a9be7af2671f58e7_Out_0_Float.xx), _Multiply_ba4d7f376f904143acc9eb33d8414ff8_Out_2_Vector2);
float _Property_6eae25110c184e7a94293b95ddc9ecef_Out_0_Float = _Wiggle_Wind_Speed_Small;
float _Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Property_6eae25110c184e7a94293b95ddc9ecef_Out_0_Float, _Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float);
float _Multiply_7510d5a95e6c4d3986bb83059d23d2fa_Out_2_Float;
Unity_Multiply_float_float(_Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float, 0.8, _Multiply_7510d5a95e6c4d3986bb83059d23d2fa_Out_2_Float);
float2 _Vector2_cb26bc8c681b455c81073f0dc3c8f6b4_Out_0_Vector2 = float2(_Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float, _Multiply_7510d5a95e6c4d3986bb83059d23d2fa_Out_2_Float);
float2 _TilingAndOffset_e5f47c2cf9d846859a72aadcdac7fc3b_Out_3_Vector2;
Unity_TilingAndOffset_float(_Multiply_ba4d7f376f904143acc9eb33d8414ff8_Out_2_Vector2, float2 (1, 1), _Vector2_cb26bc8c681b455c81073f0dc3c8f6b4_Out_0_Vector2, _TilingAndOffset_e5f47c2cf9d846859a72aadcdac7fc3b_Out_3_Vector2);
  float4 _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).tex, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).GetTransformedUV(_TilingAndOffset_e5f47c2cf9d846859a72aadcdac7fc3b_Out_3_Vector2), float(2));
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_R_5_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_G_6_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_B_7_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_A_8_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.a;
float _Float_3f1a7b5619414946b13e7cef19336d45_Out_0_Float = float(0.3);
float _Subtract_3105ef040f344481afb342cd9ef65e9f_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_R_5_Float, _Float_3f1a7b5619414946b13e7cef19336d45_Out_0_Float, _Subtract_3105ef040f344481afb342cd9ef65e9f_Out_2_Float);
float _Split_18495110217342bc80044d9345d98fbd_R_1_Float = IN.WorldSpacePosition[0];
float _Split_18495110217342bc80044d9345d98fbd_G_2_Float = IN.WorldSpacePosition[1];
float _Split_18495110217342bc80044d9345d98fbd_B_3_Float = IN.WorldSpacePosition[2];
float _Split_18495110217342bc80044d9345d98fbd_A_4_Float = 0;
float4 _Combine_d1ee3e49696840daa7cfd43df2287807_RGBA_4_Vector4;
float3 _Combine_d1ee3e49696840daa7cfd43df2287807_RGB_5_Vector3;
float2 _Combine_d1ee3e49696840daa7cfd43df2287807_RG_6_Vector2;
Unity_Combine_float(_Split_18495110217342bc80044d9345d98fbd_R_1_Float, _Split_18495110217342bc80044d9345d98fbd_G_2_Float, float(0), float(0), _Combine_d1ee3e49696840daa7cfd43df2287807_RGBA_4_Vector4, _Combine_d1ee3e49696840daa7cfd43df2287807_RGB_5_Vector3, _Combine_d1ee3e49696840daa7cfd43df2287807_RG_6_Vector2);
float _Property_9efe7c2851414ea39b6971daac6143e5_Out_0_Float = _Wiggle_Wind_Size;
float _Multiply_4f6b361114004341aeae12987f071375_Out_2_Float;
Unity_Multiply_float_float(_Property_9efe7c2851414ea39b6971daac6143e5_Out_0_Float, 0.763, _Multiply_4f6b361114004341aeae12987f071375_Out_2_Float);
float2 _Multiply_0d88e33790f0458b9e6e7f49946e6344_Out_2_Vector2;
Unity_Multiply_float2_float2(_Combine_d1ee3e49696840daa7cfd43df2287807_RG_6_Vector2, (_Multiply_4f6b361114004341aeae12987f071375_Out_2_Float.xx), _Multiply_0d88e33790f0458b9e6e7f49946e6344_Out_2_Vector2);
float _Property_56c994a79361438d88ea30b1e3ccf2aa_Out_0_Float = _Wiggle_Wind_Speed_Large;
float _Multiply_c5e5feed518642678424c6f0d45b4377_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Property_56c994a79361438d88ea30b1e3ccf2aa_Out_0_Float, _Multiply_c5e5feed518642678424c6f0d45b4377_Out_2_Float);
float2 _TilingAndOffset_7dac505987c144edad083d13ebca67e6_Out_3_Vector2;
Unity_TilingAndOffset_float(_Multiply_0d88e33790f0458b9e6e7f49946e6344_Out_2_Vector2, float2 (1, 1), (_Multiply_c5e5feed518642678424c6f0d45b4377_Out_2_Float.xx), _TilingAndOffset_7dac505987c144edad083d13ebca67e6_Out_3_Vector2);
  float4 _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).tex, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).GetTransformedUV(_TilingAndOffset_7dac505987c144edad083d13ebca67e6_Out_3_Vector2), float(2));
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_R_5_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_G_6_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_B_7_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_A_8_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.a;
float _Subtract_e29fd5b0daea41a1a38ac52280e6db88_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_G_6_Float, _Float_3f1a7b5619414946b13e7cef19336d45_Out_0_Float, _Subtract_e29fd5b0daea41a1a38ac52280e6db88_Out_2_Float);
float _Add_6d51be6104ee4e23a093dfb9c3d993f7_Out_2_Float;
Unity_Add_float(_Subtract_3105ef040f344481afb342cd9ef65e9f_Out_2_Float, _Subtract_e29fd5b0daea41a1a38ac52280e6db88_Out_2_Float, _Add_6d51be6104ee4e23a093dfb9c3d993f7_Out_2_Float);
float4 _Multiply_db043a8b3fc34edb8bee433085c7e061_Out_2_Vector4;
Unity_Multiply_float4_float4((_Add_6d51be6104ee4e23a093dfb9c3d993f7_Out_2_Float.xxxx), IN.VertexColor, _Multiply_db043a8b3fc34edb8bee433085c7e061_Out_2_Vector4);
float _Property_46c8c648052246a4a32e80905ed7e9df_Out_0_Float = _Wiggle_Wind_Intensity;
float4 _Multiply_4baa80310c414483a2aada72baa7a883_Out_2_Vector4;
Unity_Multiply_float4_float4(_Multiply_db043a8b3fc34edb8bee433085c7e061_Out_2_Vector4, (_Property_46c8c648052246a4a32e80905ed7e9df_Out_0_Float.xxxx), _Multiply_4baa80310c414483a2aada72baa7a883_Out_2_Vector4);
float3 _Multiply_2c95a93020c44a20be82a31b985c35b8_Out_2_Vector3;
Unity_Multiply_float3_float3((_Multiply_4baa80310c414483a2aada72baa7a883_Out_2_Vector4.xyz), IN.ObjectSpaceNormal, _Multiply_2c95a93020c44a20be82a31b985c35b8_Out_2_Vector3);
float3 _Add_5a143eeddea74bceb3ddd5b059f49da5_Out_2_Vector3;
Unity_Add_float3(_Lerp_1fbf8d0a7f5a4e2ab381967ac90385c7_Out_3_Vector3, _Multiply_2c95a93020c44a20be82a31b985c35b8_Out_2_Vector3, _Add_5a143eeddea74bceb3ddd5b059f49da5_Out_2_Vector3);
#if defined(_USE_WPO)
float3 _UseWPO_1f28bdd134c9432e812aded965021562_Out_0_Vector3 = _Add_5a143eeddea74bceb3ddd5b059f49da5_Out_2_Vector3;
#else
float3 _UseWPO_1f28bdd134c9432e812aded965021562_Out_0_Vector3 = IN.ObjectSpacePosition;
#endif
float3 _UPGFoliageContactPositionCustomFunction_3446fb1a0c894696bd903b7eb184e241_bentPositionOS_1_Vector3;
UPG_FoliageContactPosition_float(_UseWPO_1f28bdd134c9432e812aded965021562_Out_0_Vector3, _UPGFoliageContactPositionCustomFunction_3446fb1a0c894696bd903b7eb184e241_bentPositionOS_1_Vector3);
description.Position = _UPGFoliageContactPositionCustomFunction_3446fb1a0c894696bd903b7eb184e241_bentPositionOS_1_Vector3;
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
float4 _Property_a9c1982af1c74efb9ac25b0326ae7979_Out_0_Vector4 = _Foliage_Color_Bottom;
float4 _Property_50eb1cb960084765a0771df7f40a6d5d_Out_0_Vector4 = _Foliage_Color_Top;
float _Split_1540f44067c24640b4968355fa26a295_R_1_Float = IN.WorldSpacePosition[0];
float _Split_1540f44067c24640b4968355fa26a295_G_2_Float = IN.WorldSpacePosition[1];
float _Split_1540f44067c24640b4968355fa26a295_B_3_Float = IN.WorldSpacePosition[2];
float _Split_1540f44067c24640b4968355fa26a295_A_4_Float = 0;
float3 _Vector3_54c605479a824dd3a62abb1919f39c57_Out_0_Vector3 = float3(float(0), float(0), float(0));
float3 _Transform_f3f68ad0eeb04ce9ac459f2859353c06_Out_1_Vector3;
_Transform_f3f68ad0eeb04ce9ac459f2859353c06_Out_1_Vector3 = TransformObjectToWorld(_Vector3_54c605479a824dd3a62abb1919f39c57_Out_0_Vector3.xyz);
float _Split_15d5002ae66545318962a44958be8e00_R_1_Float = _Transform_f3f68ad0eeb04ce9ac459f2859353c06_Out_1_Vector3[0];
float _Split_15d5002ae66545318962a44958be8e00_G_2_Float = _Transform_f3f68ad0eeb04ce9ac459f2859353c06_Out_1_Vector3[1];
float _Split_15d5002ae66545318962a44958be8e00_B_3_Float = _Transform_f3f68ad0eeb04ce9ac459f2859353c06_Out_1_Vector3[2];
float _Split_15d5002ae66545318962a44958be8e00_A_4_Float = 0;
float _Subtract_3131eba61d024496834f2933dbde6855_Out_2_Float;
Unity_Subtract_float(_Split_1540f44067c24640b4968355fa26a295_G_2_Float, _Split_15d5002ae66545318962a44958be8e00_G_2_Float, _Subtract_3131eba61d024496834f2933dbde6855_Out_2_Float);
float _Property_605b997dac5a4228881635945f995c72_Out_0_Float = _Tree_Height;
float _Divide_8e4152d2ed4943778a44dc39efa65611_Out_2_Float;
Unity_Divide_float(_Subtract_3131eba61d024496834f2933dbde6855_Out_2_Float, _Property_605b997dac5a4228881635945f995c72_Out_0_Float, _Divide_8e4152d2ed4943778a44dc39efa65611_Out_2_Float);
float _Saturate_086c13233c3d43fab6a33a1e2ed498c2_Out_1_Float;
Unity_Saturate_float(_Divide_8e4152d2ed4943778a44dc39efa65611_Out_2_Float, _Saturate_086c13233c3d43fab6a33a1e2ed498c2_Out_1_Float);
float _Property_0e5bff248b2341909121bb16666f3468_Out_0_Float = _Gradience_Power;
float _Power_be0c59cc158d492f99ea24f3b1001e1f_Out_2_Float;
Unity_Power_float(_Saturate_086c13233c3d43fab6a33a1e2ed498c2_Out_1_Float, _Property_0e5bff248b2341909121bb16666f3468_Out_0_Float, _Power_be0c59cc158d492f99ea24f3b1001e1f_Out_2_Float);
float _Saturate_26bbc2447b5a402c9562a9e5d012fd6c_Out_1_Float;
Unity_Saturate_float(_Power_be0c59cc158d492f99ea24f3b1001e1f_Out_2_Float, _Saturate_26bbc2447b5a402c9562a9e5d012fd6c_Out_1_Float);
float4 _Lerp_795c581446834fe0bee35971bc8f0c5c_Out_3_Vector4;
Unity_Lerp_float4(_Property_a9c1982af1c74efb9ac25b0326ae7979_Out_0_Vector4, _Property_50eb1cb960084765a0771df7f40a6d5d_Out_0_Vector4, (_Saturate_26bbc2447b5a402c9562a9e5d012fd6c_Out_1_Float.xxxx), _Lerp_795c581446834fe0bee35971bc8f0c5c_Out_3_Vector4);
float4 _Property_4036fdaeaa4647adb3dd84d70ff1161e_Out_0_Vector4 = _Foliage_Color_Top;
#if defined(_USE_HEIGHT_GRADIENCE)
float4 _UseHeightGradience_d4326b9500c14f11a9090b7e8f107721_Out_0_Vector4 = _Lerp_795c581446834fe0bee35971bc8f0c5c_Out_3_Vector4;
#else
float4 _UseHeightGradience_d4326b9500c14f11a9090b7e8f107721_Out_0_Vector4 = _Property_4036fdaeaa4647adb3dd84d70ff1161e_Out_0_Vector4;
#endif
UnityTexture2D _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Color_Map, sampler_Color_Map, _Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_eaea7ba5e1e74519a1bc7833f0103ab8_Out_0_Vector4 = IN.uv0;
float _Property_95352575ae4140008287fd58138e8eb5_Out_0_Float = _Color_Map_Tiling;
float4 _Multiply_86fafeae394745fe880655d03cafd9e1_Out_2_Vector4;
Unity_Multiply_float4_float4(_UV_eaea7ba5e1e74519a1bc7833f0103ab8_Out_0_Vector4, (_Property_95352575ae4140008287fd58138e8eb5_Out_0_Float.xxxx), _Multiply_86fafeae394745fe880655d03cafd9e1_Out_2_Vector4);
float4 _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.tex, _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.samplerstate, _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.GetTransformedUV((_Multiply_86fafeae394745fe880655d03cafd9e1_Out_2_Vector4.xy)) );
if (_Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4, _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_R_4_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.r;
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_G_5_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.g;
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_B_6_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.b;
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_A_7_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.a;
float4 _Multiply_a05088f8e3434d54920d012fc47555a7_Out_2_Vector4;
Unity_Multiply_float4_float4(_UseHeightGradience_d4326b9500c14f11a9090b7e8f107721_Out_0_Vector4, _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4, _Multiply_a05088f8e3434d54920d012fc47555a7_Out_2_Vector4);
float4 _Property_ee5875e8bc214caba4dbea1511d06cd2_Out_0_Vector4 = _Always_On_Fresnel_Color;
float _Property_be2ed593ceb345f89b57d123af3db203_Out_0_Float = _Always_On_Fresnel_Intensity;
float4 _Multiply_dcf6e90c046c4f72b77b81df5b922468_Out_2_Vector4;
Unity_Multiply_float4_float4(_Property_ee5875e8bc214caba4dbea1511d06cd2_Out_0_Vector4, (_Property_be2ed593ceb345f89b57d123af3db203_Out_0_Float.xxxx), _Multiply_dcf6e90c046c4f72b77b81df5b922468_Out_2_Vector4);
float4 _Saturate_26f2848ceb0a494094065a9387275032_Out_1_Vector4;
Unity_Saturate_float4(_Multiply_dcf6e90c046c4f72b77b81df5b922468_Out_2_Vector4, _Saturate_26f2848ceb0a494094065a9387275032_Out_1_Vector4);
float3 _Normalize_764cced5d0894f3d89f5911d50d5f4a2_Out_1_Vector3;
Unity_Normalize_float3(IN.WorldSpaceViewDirection, _Normalize_764cced5d0894f3d89f5911d50d5f4a2_Out_1_Vector3);
float _DotProduct_eb716c3ca301415688a52e76b310a65c_Out_2_Float;
Unity_DotProduct_float3(IN.WorldSpaceNormal, _Normalize_764cced5d0894f3d89f5911d50d5f4a2_Out_1_Vector3, _DotProduct_eb716c3ca301415688a52e76b310a65c_Out_2_Float);
float _Absolute_470cf4f0298a44a695f47aa5ea61c0ab_Out_1_Float;
Unity_Absolute_float(_DotProduct_eb716c3ca301415688a52e76b310a65c_Out_2_Float, _Absolute_470cf4f0298a44a695f47aa5ea61c0ab_Out_1_Float);
float _OneMinus_fa832b719fc24001be421507d395a592_Out_1_Float;
Unity_OneMinus_float(_Absolute_470cf4f0298a44a695f47aa5ea61c0ab_Out_1_Float, _OneMinus_fa832b719fc24001be421507d395a592_Out_1_Float);
float _Property_d9cfcacb90204bdfa36d0c1b7c0da452_Out_0_Float = _Always_On_Fresnel_Pow;
float _Power_7841b1ae85554473b96cce132c7ff7e1_Out_2_Float;
Unity_Power_float(_OneMinus_fa832b719fc24001be421507d395a592_Out_1_Float, _Property_d9cfcacb90204bdfa36d0c1b7c0da452_Out_0_Float, _Power_7841b1ae85554473b96cce132c7ff7e1_Out_2_Float);
float _Saturate_908f6307f948471ca95c0e7507cd7e43_Out_1_Float;
Unity_Saturate_float(_Power_7841b1ae85554473b96cce132c7ff7e1_Out_2_Float, _Saturate_908f6307f948471ca95c0e7507cd7e43_Out_1_Float);
float4 _Lerp_3406aa7c52b14c818ac9eb76bf068e2d_Out_3_Vector4;
Unity_Lerp_float4(_Multiply_a05088f8e3434d54920d012fc47555a7_Out_2_Vector4, _Saturate_26f2848ceb0a494094065a9387275032_Out_1_Vector4, (_Saturate_908f6307f948471ca95c0e7507cd7e43_Out_1_Float.xxxx), _Lerp_3406aa7c52b14c818ac9eb76bf068e2d_Out_3_Vector4);
float _Property_e8b3335851f84d91b28234460148cd0f_Out_0_Float = _Subsurface_Intensity;
float4 _Multiply_94df75e4ad734308a11d784250fc0b0c_Out_2_Vector4;
Unity_Multiply_float4_float4(_Lerp_3406aa7c52b14c818ac9eb76bf068e2d_Out_3_Vector4, (_Property_e8b3335851f84d91b28234460148cd0f_Out_0_Float.xxxx), _Multiply_94df75e4ad734308a11d784250fc0b0c_Out_2_Vector4);
float4 _Saturate_f0090edc5c664784a336737069276109_Out_1_Vector4;
Unity_Saturate_float4(_Multiply_94df75e4ad734308a11d784250fc0b0c_Out_2_Vector4, _Saturate_f0090edc5c664784a336737069276109_Out_1_Vector4);
float3 _MainLightCustomFunction_461af74b29b6456da89a2348455c2483_Direction_0_Vector3;
MainLight_float(_MainLightCustomFunction_461af74b29b6456da89a2348455c2483_Direction_0_Vector3);
float3 _Normalize_8644c278d597434b8871eb11a7a52cfe_Out_1_Vector3;
Unity_Normalize_float3(_MainLightCustomFunction_461af74b29b6456da89a2348455c2483_Direction_0_Vector3, _Normalize_8644c278d597434b8871eb11a7a52cfe_Out_1_Vector3);
float _Split_8a104625401f4a048b1160e17800fd11_R_1_Float = _Normalize_8644c278d597434b8871eb11a7a52cfe_Out_1_Vector3[0];
float _Split_8a104625401f4a048b1160e17800fd11_G_2_Float = _Normalize_8644c278d597434b8871eb11a7a52cfe_Out_1_Vector3[1];
float _Split_8a104625401f4a048b1160e17800fd11_B_3_Float = _Normalize_8644c278d597434b8871eb11a7a52cfe_Out_1_Vector3[2];
float _Split_8a104625401f4a048b1160e17800fd11_A_4_Float = 0;
float4 _Combine_87a90606ca4545b2983770732c0ed1b7_RGBA_4_Vector4;
float3 _Combine_87a90606ca4545b2983770732c0ed1b7_RGB_5_Vector3;
float2 _Combine_87a90606ca4545b2983770732c0ed1b7_RG_6_Vector2;
Unity_Combine_float(_Split_8a104625401f4a048b1160e17800fd11_R_1_Float, _Split_8a104625401f4a048b1160e17800fd11_B_3_Float, float(0), float(0), _Combine_87a90606ca4545b2983770732c0ed1b7_RGBA_4_Vector4, _Combine_87a90606ca4545b2983770732c0ed1b7_RGB_5_Vector3, _Combine_87a90606ca4545b2983770732c0ed1b7_RG_6_Vector2);
float3 _Normalize_f1ad7ea0af7b45ee88df143397ba6727_Out_1_Vector3;
Unity_Normalize_float3(-UNITY_MATRIX_V[2].xyz, _Normalize_f1ad7ea0af7b45ee88df143397ba6727_Out_1_Vector3);
float3 _Multiply_5b2d23749b2347b7af4472236c1dfe22_Out_2_Vector3;
Unity_Multiply_float3_float3(_Normalize_f1ad7ea0af7b45ee88df143397ba6727_Out_1_Vector3, float3(-1, -1, -1), _Multiply_5b2d23749b2347b7af4472236c1dfe22_Out_2_Vector3);
float _Split_6ebebe9410ea41bd8f8a327255848dc9_R_1_Float = _Multiply_5b2d23749b2347b7af4472236c1dfe22_Out_2_Vector3[0];
float _Split_6ebebe9410ea41bd8f8a327255848dc9_G_2_Float = _Multiply_5b2d23749b2347b7af4472236c1dfe22_Out_2_Vector3[1];
float _Split_6ebebe9410ea41bd8f8a327255848dc9_B_3_Float = _Multiply_5b2d23749b2347b7af4472236c1dfe22_Out_2_Vector3[2];
float _Split_6ebebe9410ea41bd8f8a327255848dc9_A_4_Float = 0;
float4 _Combine_ce9284087dd446409babf9c154f18fe4_RGBA_4_Vector4;
float3 _Combine_ce9284087dd446409babf9c154f18fe4_RGB_5_Vector3;
float2 _Combine_ce9284087dd446409babf9c154f18fe4_RG_6_Vector2;
Unity_Combine_float(_Split_6ebebe9410ea41bd8f8a327255848dc9_R_1_Float, _Split_6ebebe9410ea41bd8f8a327255848dc9_B_3_Float, float(0), float(0), _Combine_ce9284087dd446409babf9c154f18fe4_RGBA_4_Vector4, _Combine_ce9284087dd446409babf9c154f18fe4_RGB_5_Vector3, _Combine_ce9284087dd446409babf9c154f18fe4_RG_6_Vector2);
float _DotProduct_9e4f6525393a4171b6519e347a4b7a37_Out_2_Float;
Unity_DotProduct_float2(_Combine_87a90606ca4545b2983770732c0ed1b7_RG_6_Vector2, _Combine_ce9284087dd446409babf9c154f18fe4_RG_6_Vector2, _DotProduct_9e4f6525393a4171b6519e347a4b7a37_Out_2_Float);
float _Saturate_f76f8f4113e0468b9b20a6abf253d76e_Out_1_Float;
Unity_Saturate_float(_DotProduct_9e4f6525393a4171b6519e347a4b7a37_Out_2_Float, _Saturate_f76f8f4113e0468b9b20a6abf253d76e_Out_1_Float);
float _Property_bd14cfb26b074dbabde1a5a9b7f95d97_Out_0_Float = _Subsurface_Transition_Smoothness;
float _Power_2bc1d129029a4da69b2993ea07ff412f_Out_2_Float;
Unity_Power_float(_Saturate_f76f8f4113e0468b9b20a6abf253d76e_Out_1_Float, _Property_bd14cfb26b074dbabde1a5a9b7f95d97_Out_0_Float, _Power_2bc1d129029a4da69b2993ea07ff412f_Out_2_Float);
float _Saturate_44588f923e1b48ff97cd55ea2d69aa2b_Out_1_Float;
Unity_Saturate_float(_Power_2bc1d129029a4da69b2993ea07ff412f_Out_2_Float, _Saturate_44588f923e1b48ff97cd55ea2d69aa2b_Out_1_Float);
float4 _Lerp_98f83471835040419ec38e468d855fe3_Out_3_Vector4;
Unity_Lerp_float4(_Lerp_3406aa7c52b14c818ac9eb76bf068e2d_Out_3_Vector4, _Saturate_f0090edc5c664784a336737069276109_Out_1_Vector4, (_Saturate_44588f923e1b48ff97cd55ea2d69aa2b_Out_1_Float.xxxx), _Lerp_98f83471835040419ec38e468d855fe3_Out_3_Vector4);
float _Property_e00e00901e414a40a25484eb2c276584_Out_0_Float = _Subsurface_Fresnel_Intensity;
float4 _Multiply_6ed60680651f4e8cbff9e6f2ed97f0b9_Out_2_Vector4;
Unity_Multiply_float4_float4(_Lerp_98f83471835040419ec38e468d855fe3_Out_3_Vector4, (_Property_e00e00901e414a40a25484eb2c276584_Out_0_Float.xxxx), _Multiply_6ed60680651f4e8cbff9e6f2ed97f0b9_Out_2_Vector4);
float4 _Saturate_5cea81f1b9b5405fb4a571a97e57df52_Out_1_Vector4;
Unity_Saturate_float4(_Multiply_6ed60680651f4e8cbff9e6f2ed97f0b9_Out_2_Vector4, _Saturate_5cea81f1b9b5405fb4a571a97e57df52_Out_1_Vector4);
float _Saturate_10ad388197014e2fa2d0bd8261d82207_Out_1_Float;
Unity_Saturate_float(_DotProduct_9e4f6525393a4171b6519e347a4b7a37_Out_2_Float, _Saturate_10ad388197014e2fa2d0bd8261d82207_Out_1_Float);
float _Property_fe3bfd62823b45bf868d2c88982c1f64_Out_0_Float = _Subsurface_Fresnel_Transition_Smoothness;
float _Power_1ad124a5d5b74c13848df4f8f171d448_Out_2_Float;
Unity_Power_float(_Saturate_10ad388197014e2fa2d0bd8261d82207_Out_1_Float, _Property_fe3bfd62823b45bf868d2c88982c1f64_Out_0_Float, _Power_1ad124a5d5b74c13848df4f8f171d448_Out_2_Float);
float3 _Normalize_7e92e50b98274c508fa061a32d7a99b7_Out_1_Vector3;
Unity_Normalize_float3(IN.WorldSpaceViewDirection, _Normalize_7e92e50b98274c508fa061a32d7a99b7_Out_1_Vector3);
float _DotProduct_3170aef4b99740d083af85a5aa294edd_Out_2_Float;
Unity_DotProduct_float3(IN.WorldSpaceNormal, _Normalize_7e92e50b98274c508fa061a32d7a99b7_Out_1_Vector3, _DotProduct_3170aef4b99740d083af85a5aa294edd_Out_2_Float);
float _Absolute_111753c15e794f4b8e885c2c19d93d82_Out_1_Float;
Unity_Absolute_float(_DotProduct_3170aef4b99740d083af85a5aa294edd_Out_2_Float, _Absolute_111753c15e794f4b8e885c2c19d93d82_Out_1_Float);
float _OneMinus_2a02f0d378e1454c9aeda0ff447d4d86_Out_1_Float;
Unity_OneMinus_float(_Absolute_111753c15e794f4b8e885c2c19d93d82_Out_1_Float, _OneMinus_2a02f0d378e1454c9aeda0ff447d4d86_Out_1_Float);
float _Property_49c0203b77734a36b9ba06f7bcd723ee_Out_0_Float = _Subsurface_Fresnel_Pow;
float _Power_71121b48d5934b11a14aeadcad656816_Out_2_Float;
Unity_Power_float(_OneMinus_2a02f0d378e1454c9aeda0ff447d4d86_Out_1_Float, _Property_49c0203b77734a36b9ba06f7bcd723ee_Out_0_Float, _Power_71121b48d5934b11a14aeadcad656816_Out_2_Float);
float _Saturate_29e69580558f40c49a883332cdf25357_Out_1_Float;
Unity_Saturate_float(_Power_71121b48d5934b11a14aeadcad656816_Out_2_Float, _Saturate_29e69580558f40c49a883332cdf25357_Out_1_Float);
float _Multiply_328c604c9492493ba1dc1999ef6314fd_Out_2_Float;
Unity_Multiply_float_float(_Power_1ad124a5d5b74c13848df4f8f171d448_Out_2_Float, _Saturate_29e69580558f40c49a883332cdf25357_Out_1_Float, _Multiply_328c604c9492493ba1dc1999ef6314fd_Out_2_Float);
float4 _Lerp_10a8be366b974da7b00eab9c55f88a8a_Out_3_Vector4;
Unity_Lerp_float4(_Lerp_98f83471835040419ec38e468d855fe3_Out_3_Vector4, _Saturate_5cea81f1b9b5405fb4a571a97e57df52_Out_1_Vector4, (_Multiply_328c604c9492493ba1dc1999ef6314fd_Out_2_Float.xxxx), _Lerp_10a8be366b974da7b00eab9c55f88a8a_Out_3_Vector4);
float _Multiply_cf1ea983d11e4ab7b4bdbbb064ad94b5_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_A_7_Float, 1, _Multiply_cf1ea983d11e4ab7b4bdbbb064ad94b5_Out_2_Float);
float _Property_99bb0b22edbb4ea0b1b7844414b876a7_Out_0_Float = _Alpha_Clip;
surface.BaseColor = (_Lerp_10a8be366b974da7b00eab9c55f88a8a_Out_3_Vector4.xyz);
surface.Emission = float3(0, 0, 0);
surface.Alpha = _Multiply_cf1ea983d11e4ab7b4bdbbb064ad94b5_Out_2_Float;
surface.AlphaClipThreshold = _Property_99bb0b22edbb4ea0b1b7844414b876a7_Out_0_Float;
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

    

    // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
    float3 unnormalizedNormalWS = input.normalWS;
    const float renormFactor = 1.0 / length(unnormalizedNormalWS);


    output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph


    output.WorldSpaceViewDirection = GetWorldSpaceNormalizeViewDir(input.positionWS);
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
#pragma shader_feature_local _ _USE_WPO
#pragma shader_feature_local _ _USE_HEIGHT_GRADIENCE

#if defined(_USE_WPO) && defined(_USE_HEIGHT_GRADIENCE)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_WPO)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_HEIGHT_GRADIENCE)
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
#define VARYINGS_NEED_TEXCOORD0
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SceneSelectionPass
#define BUILTIN_TARGET_API 1
#define SCENESELECTIONPASS 1
#define _BUILTIN_AlphaClip 1
#define _BUILTIN_ALPHATEST_ON 1
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

float4 _Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D_TexelSize;
float _Subsurface_Fresnel_Transition_Smoothness;
float _Subsurface_Fresnel_Pow;
float _Subsurface_Fresnel_Intensity;
float _Subsurface_Transition_Smoothness;
float _Wiggle_Wind_Speed_Large;
float _Wiggle_Wind_Intensity;
float _Roughness;
float4 _Foliage_Color_Bottom;
float _Sway_Wind_Speed;
float _Gradience_Power;
float _Fresnel_Mul;
float4 _Foliage_Color_Top;
float _Always_On_Fresnel_Intensity;
float _Tree_Height;
float4 _Color_Map_TexelSize;
float _Height_Weight;
float _Height_Gradience;
float _Sway_Wind_Time_Random_Offset_Amount;
float _Sway_Wind_Intensity;
float3 _Sway_Wind_Direction;
float _Color_Map_Tiling;
float _Subsurface_Intensity;
float _Alpha_Clip;
float _Wiggle_Wind_Size;
float _Wiggle_Wind_Speed_Small;
float _Fresnel_Pow;
float _Always_On_Fresnel_Pow;
float4 _Always_On_Fresnel_Color;
float _ree;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D);
SAMPLER(sampler_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D);
TEXTURE2D(_Color_Map);
SAMPLER(sampler_Color_Map);

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

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
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
float _Property_7689f3a0523b4af9912879a05d1d5206_Out_0_Float = _Sway_Wind_Speed;
float3 _Property_e0531013c46845b6b036ac453118c744_Out_0_Vector3 = _Sway_Wind_Direction;
float _Property_35588e6a16b5457695dba39b1644d669_Out_0_Float = _Sway_Wind_Intensity;
float _Property_c8d3d7e6e6f64d409f010d988b96e327_Out_0_Float = _Sway_Wind_Time_Random_Offset_Amount;
Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float _WindSway_662bcd4f8b2b4607b138cb44de00a708;
_WindSway_662bcd4f8b2b4607b138cb44de00a708.ObjectSpacePosition = IN.ObjectSpacePosition;
_WindSway_662bcd4f8b2b4607b138cb44de00a708.TimeParameters = IN.TimeParameters;
float3 _WindSway_662bcd4f8b2b4607b138cb44de00a708_OutVector3_1_Vector3;
SG_WindSway_51c4027ae50a8cd49be53c4990b9f179_float(_Property_7689f3a0523b4af9912879a05d1d5206_Out_0_Float, _Property_e0531013c46845b6b036ac453118c744_Out_0_Vector3, _Property_35588e6a16b5457695dba39b1644d669_Out_0_Float, _Property_c8d3d7e6e6f64d409f010d988b96e327_Out_0_Float, _WindSway_662bcd4f8b2b4607b138cb44de00a708, _WindSway_662bcd4f8b2b4607b138cb44de00a708_OutVector3_1_Vector3);
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_R_1_Float = IN.WorldSpacePosition[0];
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_G_2_Float = IN.WorldSpacePosition[1];
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_B_3_Float = IN.WorldSpacePosition[2];
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_A_4_Float = 0;
float3 _Vector3_e154b524054546d2bbeab293c0e20519_Out_0_Vector3 = float3(float(0), float(0), float(0));
float3 _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3;
_Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3 = TransformObjectToWorld(_Vector3_e154b524054546d2bbeab293c0e20519_Out_0_Vector3.xyz);
float _Split_5eb48153bba44ea2b128583819f09a11_R_1_Float = _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3[0];
float _Split_5eb48153bba44ea2b128583819f09a11_G_2_Float = _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3[1];
float _Split_5eb48153bba44ea2b128583819f09a11_B_3_Float = _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3[2];
float _Split_5eb48153bba44ea2b128583819f09a11_A_4_Float = 0;
float _Subtract_d6a99abdf4e74644acffc1718d1735bf_Out_2_Float;
Unity_Subtract_float(_Split_8fe962eb6f0f4d6fba734f9edcaf9067_G_2_Float, _Split_5eb48153bba44ea2b128583819f09a11_G_2_Float, _Subtract_d6a99abdf4e74644acffc1718d1735bf_Out_2_Float);
float _Property_1cf3cda52e094e638df491b003607dfd_Out_0_Float = _Height_Weight;
float _Split_145cf2161d4c4510b08248cad819f655_R_1_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[0];
float _Split_145cf2161d4c4510b08248cad819f655_G_2_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[1];
float _Split_145cf2161d4c4510b08248cad819f655_B_3_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[2];
float _Split_145cf2161d4c4510b08248cad819f655_A_4_Float = 0;
float _Multiply_2c140d573b9447aab058bd863fe2f4f7_Out_2_Float;
Unity_Multiply_float_float(_Property_1cf3cda52e094e638df491b003607dfd_Out_0_Float, _Split_145cf2161d4c4510b08248cad819f655_G_2_Float, _Multiply_2c140d573b9447aab058bd863fe2f4f7_Out_2_Float);
float _Divide_0f6a637143d841d0b1c94fa2ae3e164d_Out_2_Float;
Unity_Divide_float(_Subtract_d6a99abdf4e74644acffc1718d1735bf_Out_2_Float, _Multiply_2c140d573b9447aab058bd863fe2f4f7_Out_2_Float, _Divide_0f6a637143d841d0b1c94fa2ae3e164d_Out_2_Float);
float _Saturate_1e4e29fb5c074565a1f941c046957b05_Out_1_Float;
Unity_Saturate_float(_Divide_0f6a637143d841d0b1c94fa2ae3e164d_Out_2_Float, _Saturate_1e4e29fb5c074565a1f941c046957b05_Out_1_Float);
float _Property_3d64a0c8dd104507b9a0762a778fe585_Out_0_Float = _Height_Gradience;
float _Power_f1d74728398a45f9993fe3b3affb6584_Out_2_Float;
Unity_Power_float(_Saturate_1e4e29fb5c074565a1f941c046957b05_Out_1_Float, _Property_3d64a0c8dd104507b9a0762a778fe585_Out_0_Float, _Power_f1d74728398a45f9993fe3b3affb6584_Out_2_Float);
float _Saturate_57b130350452418fa1d09d4a0ddd64ac_Out_1_Float;
Unity_Saturate_float(_Power_f1d74728398a45f9993fe3b3affb6584_Out_2_Float, _Saturate_57b130350452418fa1d09d4a0ddd64ac_Out_1_Float);
float3 _Lerp_1fbf8d0a7f5a4e2ab381967ac90385c7_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _WindSway_662bcd4f8b2b4607b138cb44de00a708_OutVector3_1_Vector3, (_Saturate_57b130350452418fa1d09d4a0ddd64ac_Out_1_Float.xxx), _Lerp_1fbf8d0a7f5a4e2ab381967ac90385c7_Out_3_Vector3);
float _Split_bd04916788224fa2bb71be3f27eb839a_R_1_Float = IN.WorldSpacePosition[0];
float _Split_bd04916788224fa2bb71be3f27eb839a_G_2_Float = IN.WorldSpacePosition[1];
float _Split_bd04916788224fa2bb71be3f27eb839a_B_3_Float = IN.WorldSpacePosition[2];
float _Split_bd04916788224fa2bb71be3f27eb839a_A_4_Float = 0;
float4 _Combine_45cdab9c20414d03862100967f56c78d_RGBA_4_Vector4;
float3 _Combine_45cdab9c20414d03862100967f56c78d_RGB_5_Vector3;
float2 _Combine_45cdab9c20414d03862100967f56c78d_RG_6_Vector2;
Unity_Combine_float(_Split_bd04916788224fa2bb71be3f27eb839a_R_1_Float, _Split_bd04916788224fa2bb71be3f27eb839a_B_3_Float, float(0), float(0), _Combine_45cdab9c20414d03862100967f56c78d_RGBA_4_Vector4, _Combine_45cdab9c20414d03862100967f56c78d_RGB_5_Vector3, _Combine_45cdab9c20414d03862100967f56c78d_RG_6_Vector2);
float _Property_9b35f6244c0247c7a9be7af2671f58e7_Out_0_Float = _Wiggle_Wind_Size;
float2 _Multiply_ba4d7f376f904143acc9eb33d8414ff8_Out_2_Vector2;
Unity_Multiply_float2_float2(_Combine_45cdab9c20414d03862100967f56c78d_RG_6_Vector2, (_Property_9b35f6244c0247c7a9be7af2671f58e7_Out_0_Float.xx), _Multiply_ba4d7f376f904143acc9eb33d8414ff8_Out_2_Vector2);
float _Property_6eae25110c184e7a94293b95ddc9ecef_Out_0_Float = _Wiggle_Wind_Speed_Small;
float _Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Property_6eae25110c184e7a94293b95ddc9ecef_Out_0_Float, _Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float);
float _Multiply_7510d5a95e6c4d3986bb83059d23d2fa_Out_2_Float;
Unity_Multiply_float_float(_Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float, 0.8, _Multiply_7510d5a95e6c4d3986bb83059d23d2fa_Out_2_Float);
float2 _Vector2_cb26bc8c681b455c81073f0dc3c8f6b4_Out_0_Vector2 = float2(_Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float, _Multiply_7510d5a95e6c4d3986bb83059d23d2fa_Out_2_Float);
float2 _TilingAndOffset_e5f47c2cf9d846859a72aadcdac7fc3b_Out_3_Vector2;
Unity_TilingAndOffset_float(_Multiply_ba4d7f376f904143acc9eb33d8414ff8_Out_2_Vector2, float2 (1, 1), _Vector2_cb26bc8c681b455c81073f0dc3c8f6b4_Out_0_Vector2, _TilingAndOffset_e5f47c2cf9d846859a72aadcdac7fc3b_Out_3_Vector2);
  float4 _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).tex, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).GetTransformedUV(_TilingAndOffset_e5f47c2cf9d846859a72aadcdac7fc3b_Out_3_Vector2), float(2));
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_R_5_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_G_6_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_B_7_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_A_8_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.a;
float _Float_3f1a7b5619414946b13e7cef19336d45_Out_0_Float = float(0.3);
float _Subtract_3105ef040f344481afb342cd9ef65e9f_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_R_5_Float, _Float_3f1a7b5619414946b13e7cef19336d45_Out_0_Float, _Subtract_3105ef040f344481afb342cd9ef65e9f_Out_2_Float);
float _Split_18495110217342bc80044d9345d98fbd_R_1_Float = IN.WorldSpacePosition[0];
float _Split_18495110217342bc80044d9345d98fbd_G_2_Float = IN.WorldSpacePosition[1];
float _Split_18495110217342bc80044d9345d98fbd_B_3_Float = IN.WorldSpacePosition[2];
float _Split_18495110217342bc80044d9345d98fbd_A_4_Float = 0;
float4 _Combine_d1ee3e49696840daa7cfd43df2287807_RGBA_4_Vector4;
float3 _Combine_d1ee3e49696840daa7cfd43df2287807_RGB_5_Vector3;
float2 _Combine_d1ee3e49696840daa7cfd43df2287807_RG_6_Vector2;
Unity_Combine_float(_Split_18495110217342bc80044d9345d98fbd_R_1_Float, _Split_18495110217342bc80044d9345d98fbd_G_2_Float, float(0), float(0), _Combine_d1ee3e49696840daa7cfd43df2287807_RGBA_4_Vector4, _Combine_d1ee3e49696840daa7cfd43df2287807_RGB_5_Vector3, _Combine_d1ee3e49696840daa7cfd43df2287807_RG_6_Vector2);
float _Property_9efe7c2851414ea39b6971daac6143e5_Out_0_Float = _Wiggle_Wind_Size;
float _Multiply_4f6b361114004341aeae12987f071375_Out_2_Float;
Unity_Multiply_float_float(_Property_9efe7c2851414ea39b6971daac6143e5_Out_0_Float, 0.763, _Multiply_4f6b361114004341aeae12987f071375_Out_2_Float);
float2 _Multiply_0d88e33790f0458b9e6e7f49946e6344_Out_2_Vector2;
Unity_Multiply_float2_float2(_Combine_d1ee3e49696840daa7cfd43df2287807_RG_6_Vector2, (_Multiply_4f6b361114004341aeae12987f071375_Out_2_Float.xx), _Multiply_0d88e33790f0458b9e6e7f49946e6344_Out_2_Vector2);
float _Property_56c994a79361438d88ea30b1e3ccf2aa_Out_0_Float = _Wiggle_Wind_Speed_Large;
float _Multiply_c5e5feed518642678424c6f0d45b4377_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Property_56c994a79361438d88ea30b1e3ccf2aa_Out_0_Float, _Multiply_c5e5feed518642678424c6f0d45b4377_Out_2_Float);
float2 _TilingAndOffset_7dac505987c144edad083d13ebca67e6_Out_3_Vector2;
Unity_TilingAndOffset_float(_Multiply_0d88e33790f0458b9e6e7f49946e6344_Out_2_Vector2, float2 (1, 1), (_Multiply_c5e5feed518642678424c6f0d45b4377_Out_2_Float.xx), _TilingAndOffset_7dac505987c144edad083d13ebca67e6_Out_3_Vector2);
  float4 _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).tex, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).GetTransformedUV(_TilingAndOffset_7dac505987c144edad083d13ebca67e6_Out_3_Vector2), float(2));
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_R_5_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_G_6_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_B_7_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_A_8_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.a;
float _Subtract_e29fd5b0daea41a1a38ac52280e6db88_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_G_6_Float, _Float_3f1a7b5619414946b13e7cef19336d45_Out_0_Float, _Subtract_e29fd5b0daea41a1a38ac52280e6db88_Out_2_Float);
float _Add_6d51be6104ee4e23a093dfb9c3d993f7_Out_2_Float;
Unity_Add_float(_Subtract_3105ef040f344481afb342cd9ef65e9f_Out_2_Float, _Subtract_e29fd5b0daea41a1a38ac52280e6db88_Out_2_Float, _Add_6d51be6104ee4e23a093dfb9c3d993f7_Out_2_Float);
float4 _Multiply_db043a8b3fc34edb8bee433085c7e061_Out_2_Vector4;
Unity_Multiply_float4_float4((_Add_6d51be6104ee4e23a093dfb9c3d993f7_Out_2_Float.xxxx), IN.VertexColor, _Multiply_db043a8b3fc34edb8bee433085c7e061_Out_2_Vector4);
float _Property_46c8c648052246a4a32e80905ed7e9df_Out_0_Float = _Wiggle_Wind_Intensity;
float4 _Multiply_4baa80310c414483a2aada72baa7a883_Out_2_Vector4;
Unity_Multiply_float4_float4(_Multiply_db043a8b3fc34edb8bee433085c7e061_Out_2_Vector4, (_Property_46c8c648052246a4a32e80905ed7e9df_Out_0_Float.xxxx), _Multiply_4baa80310c414483a2aada72baa7a883_Out_2_Vector4);
float3 _Multiply_2c95a93020c44a20be82a31b985c35b8_Out_2_Vector3;
Unity_Multiply_float3_float3((_Multiply_4baa80310c414483a2aada72baa7a883_Out_2_Vector4.xyz), IN.ObjectSpaceNormal, _Multiply_2c95a93020c44a20be82a31b985c35b8_Out_2_Vector3);
float3 _Add_5a143eeddea74bceb3ddd5b059f49da5_Out_2_Vector3;
Unity_Add_float3(_Lerp_1fbf8d0a7f5a4e2ab381967ac90385c7_Out_3_Vector3, _Multiply_2c95a93020c44a20be82a31b985c35b8_Out_2_Vector3, _Add_5a143eeddea74bceb3ddd5b059f49da5_Out_2_Vector3);
#if defined(_USE_WPO)
float3 _UseWPO_1f28bdd134c9432e812aded965021562_Out_0_Vector3 = _Add_5a143eeddea74bceb3ddd5b059f49da5_Out_2_Vector3;
#else
float3 _UseWPO_1f28bdd134c9432e812aded965021562_Out_0_Vector3 = IN.ObjectSpacePosition;
#endif
float3 _UPGFoliageContactPositionCustomFunction_3446fb1a0c894696bd903b7eb184e241_bentPositionOS_1_Vector3;
UPG_FoliageContactPosition_float(_UseWPO_1f28bdd134c9432e812aded965021562_Out_0_Vector3, _UPGFoliageContactPositionCustomFunction_3446fb1a0c894696bd903b7eb184e241_bentPositionOS_1_Vector3);
description.Position = _UPGFoliageContactPositionCustomFunction_3446fb1a0c894696bd903b7eb184e241_bentPositionOS_1_Vector3;
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
UnityTexture2D _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Color_Map, sampler_Color_Map, _Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_eaea7ba5e1e74519a1bc7833f0103ab8_Out_0_Vector4 = IN.uv0;
float _Property_95352575ae4140008287fd58138e8eb5_Out_0_Float = _Color_Map_Tiling;
float4 _Multiply_86fafeae394745fe880655d03cafd9e1_Out_2_Vector4;
Unity_Multiply_float4_float4(_UV_eaea7ba5e1e74519a1bc7833f0103ab8_Out_0_Vector4, (_Property_95352575ae4140008287fd58138e8eb5_Out_0_Float.xxxx), _Multiply_86fafeae394745fe880655d03cafd9e1_Out_2_Vector4);
float4 _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.tex, _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.samplerstate, _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.GetTransformedUV((_Multiply_86fafeae394745fe880655d03cafd9e1_Out_2_Vector4.xy)) );
if (_Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4, _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_R_4_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.r;
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_G_5_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.g;
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_B_6_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.b;
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_A_7_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.a;
float _Multiply_cf1ea983d11e4ab7b4bdbbb064ad94b5_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_A_7_Float, 1, _Multiply_cf1ea983d11e4ab7b4bdbbb064ad94b5_Out_2_Float);
float _Property_99bb0b22edbb4ea0b1b7844414b876a7_Out_0_Float = _Alpha_Clip;
surface.Alpha = _Multiply_cf1ea983d11e4ab7b4bdbbb064ad94b5_Out_2_Float;
surface.AlphaClipThreshold = _Property_99bb0b22edbb4ea0b1b7844414b876a7_Out_0_Float;
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
#pragma shader_feature_local _ _USE_WPO
#pragma shader_feature_local _ _USE_HEIGHT_GRADIENCE

#if defined(_USE_WPO) && defined(_USE_HEIGHT_GRADIENCE)
#define KEYWORD_PERMUTATION_0
#elif defined(_USE_WPO)
#define KEYWORD_PERMUTATION_1
#elif defined(_USE_HEIGHT_GRADIENCE)
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
#define VARYINGS_NEED_TEXCOORD0
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS ScenePickingPass
#define BUILTIN_TARGET_API 1
#define SCENEPICKINGPASS 1
#define _BUILTIN_AlphaClip 1
#define _BUILTIN_ALPHATEST_ON 1
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

float4 _Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D_TexelSize;
float _Subsurface_Fresnel_Transition_Smoothness;
float _Subsurface_Fresnel_Pow;
float _Subsurface_Fresnel_Intensity;
float _Subsurface_Transition_Smoothness;
float _Wiggle_Wind_Speed_Large;
float _Wiggle_Wind_Intensity;
float _Roughness;
float4 _Foliage_Color_Bottom;
float _Sway_Wind_Speed;
float _Gradience_Power;
float _Fresnel_Mul;
float4 _Foliage_Color_Top;
float _Always_On_Fresnel_Intensity;
float _Tree_Height;
float4 _Color_Map_TexelSize;
float _Height_Weight;
float _Height_Gradience;
float _Sway_Wind_Time_Random_Offset_Amount;
float _Sway_Wind_Intensity;
float3 _Sway_Wind_Direction;
float _Color_Map_Tiling;
float _Subsurface_Intensity;
float _Alpha_Clip;
float _Wiggle_Wind_Size;
float _Wiggle_Wind_Speed_Small;
float _Fresnel_Pow;
float _Always_On_Fresnel_Pow;
float4 _Always_On_Fresnel_Color;
float _ree;
CBUFFER_END
#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D);
SAMPLER(sampler_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D);
TEXTURE2D(_Color_Map);
SAMPLER(sampler_Color_Map);

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

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
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
float _Property_7689f3a0523b4af9912879a05d1d5206_Out_0_Float = _Sway_Wind_Speed;
float3 _Property_e0531013c46845b6b036ac453118c744_Out_0_Vector3 = _Sway_Wind_Direction;
float _Property_35588e6a16b5457695dba39b1644d669_Out_0_Float = _Sway_Wind_Intensity;
float _Property_c8d3d7e6e6f64d409f010d988b96e327_Out_0_Float = _Sway_Wind_Time_Random_Offset_Amount;
Bindings_WindSway_51c4027ae50a8cd49be53c4990b9f179_float _WindSway_662bcd4f8b2b4607b138cb44de00a708;
_WindSway_662bcd4f8b2b4607b138cb44de00a708.ObjectSpacePosition = IN.ObjectSpacePosition;
_WindSway_662bcd4f8b2b4607b138cb44de00a708.TimeParameters = IN.TimeParameters;
float3 _WindSway_662bcd4f8b2b4607b138cb44de00a708_OutVector3_1_Vector3;
SG_WindSway_51c4027ae50a8cd49be53c4990b9f179_float(_Property_7689f3a0523b4af9912879a05d1d5206_Out_0_Float, _Property_e0531013c46845b6b036ac453118c744_Out_0_Vector3, _Property_35588e6a16b5457695dba39b1644d669_Out_0_Float, _Property_c8d3d7e6e6f64d409f010d988b96e327_Out_0_Float, _WindSway_662bcd4f8b2b4607b138cb44de00a708, _WindSway_662bcd4f8b2b4607b138cb44de00a708_OutVector3_1_Vector3);
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_R_1_Float = IN.WorldSpacePosition[0];
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_G_2_Float = IN.WorldSpacePosition[1];
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_B_3_Float = IN.WorldSpacePosition[2];
float _Split_8fe962eb6f0f4d6fba734f9edcaf9067_A_4_Float = 0;
float3 _Vector3_e154b524054546d2bbeab293c0e20519_Out_0_Vector3 = float3(float(0), float(0), float(0));
float3 _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3;
_Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3 = TransformObjectToWorld(_Vector3_e154b524054546d2bbeab293c0e20519_Out_0_Vector3.xyz);
float _Split_5eb48153bba44ea2b128583819f09a11_R_1_Float = _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3[0];
float _Split_5eb48153bba44ea2b128583819f09a11_G_2_Float = _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3[1];
float _Split_5eb48153bba44ea2b128583819f09a11_B_3_Float = _Transform_86ff7ad7b96c4aa8ac1dd2ab9d243b51_Out_1_Vector3[2];
float _Split_5eb48153bba44ea2b128583819f09a11_A_4_Float = 0;
float _Subtract_d6a99abdf4e74644acffc1718d1735bf_Out_2_Float;
Unity_Subtract_float(_Split_8fe962eb6f0f4d6fba734f9edcaf9067_G_2_Float, _Split_5eb48153bba44ea2b128583819f09a11_G_2_Float, _Subtract_d6a99abdf4e74644acffc1718d1735bf_Out_2_Float);
float _Property_1cf3cda52e094e638df491b003607dfd_Out_0_Float = _Height_Weight;
float _Split_145cf2161d4c4510b08248cad819f655_R_1_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[0];
float _Split_145cf2161d4c4510b08248cad819f655_G_2_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[1];
float _Split_145cf2161d4c4510b08248cad819f655_B_3_Float = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))[2];
float _Split_145cf2161d4c4510b08248cad819f655_A_4_Float = 0;
float _Multiply_2c140d573b9447aab058bd863fe2f4f7_Out_2_Float;
Unity_Multiply_float_float(_Property_1cf3cda52e094e638df491b003607dfd_Out_0_Float, _Split_145cf2161d4c4510b08248cad819f655_G_2_Float, _Multiply_2c140d573b9447aab058bd863fe2f4f7_Out_2_Float);
float _Divide_0f6a637143d841d0b1c94fa2ae3e164d_Out_2_Float;
Unity_Divide_float(_Subtract_d6a99abdf4e74644acffc1718d1735bf_Out_2_Float, _Multiply_2c140d573b9447aab058bd863fe2f4f7_Out_2_Float, _Divide_0f6a637143d841d0b1c94fa2ae3e164d_Out_2_Float);
float _Saturate_1e4e29fb5c074565a1f941c046957b05_Out_1_Float;
Unity_Saturate_float(_Divide_0f6a637143d841d0b1c94fa2ae3e164d_Out_2_Float, _Saturate_1e4e29fb5c074565a1f941c046957b05_Out_1_Float);
float _Property_3d64a0c8dd104507b9a0762a778fe585_Out_0_Float = _Height_Gradience;
float _Power_f1d74728398a45f9993fe3b3affb6584_Out_2_Float;
Unity_Power_float(_Saturate_1e4e29fb5c074565a1f941c046957b05_Out_1_Float, _Property_3d64a0c8dd104507b9a0762a778fe585_Out_0_Float, _Power_f1d74728398a45f9993fe3b3affb6584_Out_2_Float);
float _Saturate_57b130350452418fa1d09d4a0ddd64ac_Out_1_Float;
Unity_Saturate_float(_Power_f1d74728398a45f9993fe3b3affb6584_Out_2_Float, _Saturate_57b130350452418fa1d09d4a0ddd64ac_Out_1_Float);
float3 _Lerp_1fbf8d0a7f5a4e2ab381967ac90385c7_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _WindSway_662bcd4f8b2b4607b138cb44de00a708_OutVector3_1_Vector3, (_Saturate_57b130350452418fa1d09d4a0ddd64ac_Out_1_Float.xxx), _Lerp_1fbf8d0a7f5a4e2ab381967ac90385c7_Out_3_Vector3);
float _Split_bd04916788224fa2bb71be3f27eb839a_R_1_Float = IN.WorldSpacePosition[0];
float _Split_bd04916788224fa2bb71be3f27eb839a_G_2_Float = IN.WorldSpacePosition[1];
float _Split_bd04916788224fa2bb71be3f27eb839a_B_3_Float = IN.WorldSpacePosition[2];
float _Split_bd04916788224fa2bb71be3f27eb839a_A_4_Float = 0;
float4 _Combine_45cdab9c20414d03862100967f56c78d_RGBA_4_Vector4;
float3 _Combine_45cdab9c20414d03862100967f56c78d_RGB_5_Vector3;
float2 _Combine_45cdab9c20414d03862100967f56c78d_RG_6_Vector2;
Unity_Combine_float(_Split_bd04916788224fa2bb71be3f27eb839a_R_1_Float, _Split_bd04916788224fa2bb71be3f27eb839a_B_3_Float, float(0), float(0), _Combine_45cdab9c20414d03862100967f56c78d_RGBA_4_Vector4, _Combine_45cdab9c20414d03862100967f56c78d_RGB_5_Vector3, _Combine_45cdab9c20414d03862100967f56c78d_RG_6_Vector2);
float _Property_9b35f6244c0247c7a9be7af2671f58e7_Out_0_Float = _Wiggle_Wind_Size;
float2 _Multiply_ba4d7f376f904143acc9eb33d8414ff8_Out_2_Vector2;
Unity_Multiply_float2_float2(_Combine_45cdab9c20414d03862100967f56c78d_RG_6_Vector2, (_Property_9b35f6244c0247c7a9be7af2671f58e7_Out_0_Float.xx), _Multiply_ba4d7f376f904143acc9eb33d8414ff8_Out_2_Vector2);
float _Property_6eae25110c184e7a94293b95ddc9ecef_Out_0_Float = _Wiggle_Wind_Speed_Small;
float _Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Property_6eae25110c184e7a94293b95ddc9ecef_Out_0_Float, _Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float);
float _Multiply_7510d5a95e6c4d3986bb83059d23d2fa_Out_2_Float;
Unity_Multiply_float_float(_Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float, 0.8, _Multiply_7510d5a95e6c4d3986bb83059d23d2fa_Out_2_Float);
float2 _Vector2_cb26bc8c681b455c81073f0dc3c8f6b4_Out_0_Vector2 = float2(_Multiply_fc5349594b6e43ac8aa50c0e68341508_Out_2_Float, _Multiply_7510d5a95e6c4d3986bb83059d23d2fa_Out_2_Float);
float2 _TilingAndOffset_e5f47c2cf9d846859a72aadcdac7fc3b_Out_3_Vector2;
Unity_TilingAndOffset_float(_Multiply_ba4d7f376f904143acc9eb33d8414ff8_Out_2_Vector2, float2 (1, 1), _Vector2_cb26bc8c681b455c81073f0dc3c8f6b4_Out_0_Vector2, _TilingAndOffset_e5f47c2cf9d846859a72aadcdac7fc3b_Out_3_Vector2);
  float4 _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).tex, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).GetTransformedUV(_TilingAndOffset_e5f47c2cf9d846859a72aadcdac7fc3b_Out_3_Vector2), float(2));
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_R_5_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_G_6_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_B_7_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_A_8_Float = _SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_RGBA_0_Vector4.a;
float _Float_3f1a7b5619414946b13e7cef19336d45_Out_0_Float = float(0.3);
float _Subtract_3105ef040f344481afb342cd9ef65e9f_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_bc373572426f4056a6e95cd3b5877c1a_R_5_Float, _Float_3f1a7b5619414946b13e7cef19336d45_Out_0_Float, _Subtract_3105ef040f344481afb342cd9ef65e9f_Out_2_Float);
float _Split_18495110217342bc80044d9345d98fbd_R_1_Float = IN.WorldSpacePosition[0];
float _Split_18495110217342bc80044d9345d98fbd_G_2_Float = IN.WorldSpacePosition[1];
float _Split_18495110217342bc80044d9345d98fbd_B_3_Float = IN.WorldSpacePosition[2];
float _Split_18495110217342bc80044d9345d98fbd_A_4_Float = 0;
float4 _Combine_d1ee3e49696840daa7cfd43df2287807_RGBA_4_Vector4;
float3 _Combine_d1ee3e49696840daa7cfd43df2287807_RGB_5_Vector3;
float2 _Combine_d1ee3e49696840daa7cfd43df2287807_RG_6_Vector2;
Unity_Combine_float(_Split_18495110217342bc80044d9345d98fbd_R_1_Float, _Split_18495110217342bc80044d9345d98fbd_G_2_Float, float(0), float(0), _Combine_d1ee3e49696840daa7cfd43df2287807_RGBA_4_Vector4, _Combine_d1ee3e49696840daa7cfd43df2287807_RGB_5_Vector3, _Combine_d1ee3e49696840daa7cfd43df2287807_RG_6_Vector2);
float _Property_9efe7c2851414ea39b6971daac6143e5_Out_0_Float = _Wiggle_Wind_Size;
float _Multiply_4f6b361114004341aeae12987f071375_Out_2_Float;
Unity_Multiply_float_float(_Property_9efe7c2851414ea39b6971daac6143e5_Out_0_Float, 0.763, _Multiply_4f6b361114004341aeae12987f071375_Out_2_Float);
float2 _Multiply_0d88e33790f0458b9e6e7f49946e6344_Out_2_Vector2;
Unity_Multiply_float2_float2(_Combine_d1ee3e49696840daa7cfd43df2287807_RG_6_Vector2, (_Multiply_4f6b361114004341aeae12987f071375_Out_2_Float.xx), _Multiply_0d88e33790f0458b9e6e7f49946e6344_Out_2_Vector2);
float _Property_56c994a79361438d88ea30b1e3ccf2aa_Out_0_Float = _Wiggle_Wind_Speed_Large;
float _Multiply_c5e5feed518642678424c6f0d45b4377_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Property_56c994a79361438d88ea30b1e3ccf2aa_Out_0_Float, _Multiply_c5e5feed518642678424c6f0d45b4377_Out_2_Float);
float2 _TilingAndOffset_7dac505987c144edad083d13ebca67e6_Out_3_Vector2;
Unity_TilingAndOffset_float(_Multiply_0d88e33790f0458b9e6e7f49946e6344_Out_2_Vector2, float2 (1, 1), (_Multiply_c5e5feed518642678424c6f0d45b4377_Out_2_Float.xx), _TilingAndOffset_7dac505987c144edad083d13ebca67e6_Out_3_Vector2);
  float4 _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4 = SAMPLE_TEXTURE2D_LOD(UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).tex, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_Texture2DAsset_cf3a9ecb0d7940bab8a91bbf89e6edbf_Out_0_Texture2D).GetTransformedUV(_TilingAndOffset_7dac505987c144edad083d13ebca67e6_Out_3_Vector2), float(2));
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_R_5_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.r;
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_G_6_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.g;
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_B_7_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.b;
float _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_A_8_Float = _SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_RGBA_0_Vector4.a;
float _Subtract_e29fd5b0daea41a1a38ac52280e6db88_Out_2_Float;
Unity_Subtract_float(_SampleTexture2DLOD_c98164d5fbb44b9c862178fb71c0a5ec_G_6_Float, _Float_3f1a7b5619414946b13e7cef19336d45_Out_0_Float, _Subtract_e29fd5b0daea41a1a38ac52280e6db88_Out_2_Float);
float _Add_6d51be6104ee4e23a093dfb9c3d993f7_Out_2_Float;
Unity_Add_float(_Subtract_3105ef040f344481afb342cd9ef65e9f_Out_2_Float, _Subtract_e29fd5b0daea41a1a38ac52280e6db88_Out_2_Float, _Add_6d51be6104ee4e23a093dfb9c3d993f7_Out_2_Float);
float4 _Multiply_db043a8b3fc34edb8bee433085c7e061_Out_2_Vector4;
Unity_Multiply_float4_float4((_Add_6d51be6104ee4e23a093dfb9c3d993f7_Out_2_Float.xxxx), IN.VertexColor, _Multiply_db043a8b3fc34edb8bee433085c7e061_Out_2_Vector4);
float _Property_46c8c648052246a4a32e80905ed7e9df_Out_0_Float = _Wiggle_Wind_Intensity;
float4 _Multiply_4baa80310c414483a2aada72baa7a883_Out_2_Vector4;
Unity_Multiply_float4_float4(_Multiply_db043a8b3fc34edb8bee433085c7e061_Out_2_Vector4, (_Property_46c8c648052246a4a32e80905ed7e9df_Out_0_Float.xxxx), _Multiply_4baa80310c414483a2aada72baa7a883_Out_2_Vector4);
float3 _Multiply_2c95a93020c44a20be82a31b985c35b8_Out_2_Vector3;
Unity_Multiply_float3_float3((_Multiply_4baa80310c414483a2aada72baa7a883_Out_2_Vector4.xyz), IN.ObjectSpaceNormal, _Multiply_2c95a93020c44a20be82a31b985c35b8_Out_2_Vector3);
float3 _Add_5a143eeddea74bceb3ddd5b059f49da5_Out_2_Vector3;
Unity_Add_float3(_Lerp_1fbf8d0a7f5a4e2ab381967ac90385c7_Out_3_Vector3, _Multiply_2c95a93020c44a20be82a31b985c35b8_Out_2_Vector3, _Add_5a143eeddea74bceb3ddd5b059f49da5_Out_2_Vector3);
#if defined(_USE_WPO)
float3 _UseWPO_1f28bdd134c9432e812aded965021562_Out_0_Vector3 = _Add_5a143eeddea74bceb3ddd5b059f49da5_Out_2_Vector3;
#else
float3 _UseWPO_1f28bdd134c9432e812aded965021562_Out_0_Vector3 = IN.ObjectSpacePosition;
#endif
float3 _UPGFoliageContactPositionCustomFunction_3446fb1a0c894696bd903b7eb184e241_bentPositionOS_1_Vector3;
UPG_FoliageContactPosition_float(_UseWPO_1f28bdd134c9432e812aded965021562_Out_0_Vector3, _UPGFoliageContactPositionCustomFunction_3446fb1a0c894696bd903b7eb184e241_bentPositionOS_1_Vector3);
description.Position = _UPGFoliageContactPositionCustomFunction_3446fb1a0c894696bd903b7eb184e241_bentPositionOS_1_Vector3;
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
UnityTexture2D _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Color_Map, sampler_Color_Map, _Color_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_eaea7ba5e1e74519a1bc7833f0103ab8_Out_0_Vector4 = IN.uv0;
float _Property_95352575ae4140008287fd58138e8eb5_Out_0_Float = _Color_Map_Tiling;
float4 _Multiply_86fafeae394745fe880655d03cafd9e1_Out_2_Vector4;
Unity_Multiply_float4_float4(_UV_eaea7ba5e1e74519a1bc7833f0103ab8_Out_0_Vector4, (_Property_95352575ae4140008287fd58138e8eb5_Out_0_Float.xxxx), _Multiply_86fafeae394745fe880655d03cafd9e1_Out_2_Vector4);
float4 _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.tex, _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.samplerstate, _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.GetTransformedUV((_Multiply_86fafeae394745fe880655d03cafd9e1_Out_2_Vector4.xy)) );
if (_Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4, _Property_d8dfe326212f4ae28329187fe9596f51_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_R_4_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.r;
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_G_5_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.g;
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_B_6_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.b;
float _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_A_7_Float = _SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_RGBA_0_Vector4.a;
float _Multiply_cf1ea983d11e4ab7b4bdbbb064ad94b5_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_070f951cbdd3468483625e2cc0f8fe24_A_7_Float, 1, _Multiply_cf1ea983d11e4ab7b4bdbbb064ad94b5_Out_2_Float);
float _Property_99bb0b22edbb4ea0b1b7844414b876a7_Out_0_Float = _Alpha_Clip;
surface.Alpha = _Multiply_cf1ea983d11e4ab7b4bdbbb064ad94b5_Out_2_Float;
surface.AlphaClipThreshold = _Property_99bb0b22edbb4ea0b1b7844414b876a7_Out_0_Float;
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