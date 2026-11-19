// WorldToon Foliage 패스 모음. 알파 Cutoff·UV·LOD 디더·바람 변형을 공통 함수로 공유해 패스별 실루엣이 같다.
#ifndef WORLDTOON_FOLIAGE_PASSES_INCLUDED
#define WORLDTOON_FOLIAGE_PASSES_INCLUDED

#if defined(LOD_FADE_CROSSFADE)
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif
#include "WorldToonWind.hlsl"

struct FoliageAttributes
{
    float4 positionOS        : POSITION;
    float3 normalOS          : NORMAL;
    float4 tangentOS         : TANGENT;
    half4 color              : COLOR;
    float2 texcoord          : TEXCOORD0;
    float2 staticLightmapUV  : TEXCOORD1;
    float3 positionOld       : TEXCOORD4;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

float3 FoliagePositionWS(FoliageAttributes input)
{
    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
#if defined(_WT_WIND)
    positionWS = WorldToonWindPositionWS(positionWS, input.color);
#endif
    return positionWS;
}

// 알파 Cutoff와 LOD CrossFade를 모든 패스에서 같은 순서로 적용한다.
half FoliageClip(float2 uv, float4 positionCS)
{
    half alpha = Alpha(SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap)).a, _BaseColor, _Cutoff);
#if defined(LOD_FADE_CROSSFADE)
    LODFadeCrossFade(positionCS);
#endif
    return alpha;
}

// 양면 카드: 뒷면은 노멀을 뒤집고, 선택적으로 위쪽 노멀과 섞어 덩어리감을 준다.
half3 FoliageNormalWS(half3 normalWS, bool isFrontFace)
{
    normalWS = isFrontFace ? normalWS : -normalWS;
    normalWS = normalize(lerp(normalWS, half3(0, 1, 0), _UpNormalBlend));
    return normalWS;
}

// ---------------------------------------------------------------- Forward
#if defined(WORLDTOON_FOLIAGE_FORWARD)
#include "WorldToonLighting.hlsl"

struct FoliageVaryings
{
    float2 uv          : TEXCOORD0;
    float3 positionWS  : TEXCOORD1;
    half3 normalWS     : TEXCOORD2;
    half fogFactor     : TEXCOORD5;
    DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 7);
#ifdef USE_APV_PROBE_OCCLUSION
    float4 probeOcclusion : TEXCOORD9;
#endif
    float4 positionCS  : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

FoliageVaryings FoliageForwardVertex(FoliageAttributes input)
{
    FoliageVaryings output = (FoliageVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float3 positionWS = FoliagePositionWS(input);
    output.positionWS = positionWS;
    output.positionCS = TransformWorldToHClip(positionWS);
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
    output.fogFactor = ComputeFogFactor(output.positionCS.z);
    OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
    OUTPUT_SH4(positionWS, output.normalWS, GetWorldSpaceNormalizeViewDir(positionWS), output.vertexSH, output.probeOcclusion);
    return output;
}

void FoliageForwardFragment(
    FoliageVaryings input
    , FRONT_FACE_TYPE frontFace : FRONT_FACE_SEMANTIC
    , out half4 outColor : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out uint outRenderingLayers : SV_Target1
#endif
)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    half4 baseSample = SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
    FoliageClip(input.uv, input.positionCS);

    SurfaceData surfaceData = (SurfaceData)0;
    surfaceData.albedo = baseSample.rgb * _BaseColor.rgb;
    surfaceData.alpha = 1;
    surfaceData.occlusion = 1;

    InputData inputData = (InputData)0;
    inputData.positionWS = input.positionWS;
    inputData.normalWS = FoliageNormalWS(NormalizeNormalPerPixel(input.normalWS), IS_FRONT_VFACE(frontFace, true, false));
    inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
#if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
#endif
    inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactor);
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
#if defined(LIGHTMAP_ON)
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, inputData.normalWS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
#elif defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)
    inputData.bakedGI = SAMPLE_GI(input.vertexSH, GetAbsolutePositionWS(inputData.positionWS), inputData.normalWS,
        inputData.viewDirectionWS, input.positionCS.xy, input.probeOcclusion, inputData.shadowMask);
#else
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, inputData.normalWS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
#endif

#if defined(_DBUFFER)
    ApplyDecalToSurfaceData(input.positionCS, surfaceData, inputData);
#endif

    WorldToonStyle style;
    style.litTint = _LitTint.rgb;
    style.shadeTint = _ShadeTint.rgb;
    style.bandThreshold = _BandThreshold;
    style.bandSoftness = _BandSoftness;
    style.indirectStrength = _IndirectStrength;
    style.additionalLightStrength = _AdditionalLightStrength;

    half3 color = WorldToonShade(inputData, surfaceData.albedo, style);
    // 잎 뒷면 투과: 주광을 등진 면에 Lit Tint 쪽으로 약하게 밝힌다(광원 없으면 0).
    Light mainLight = GetMainLight();
    half backLit = saturate(dot(-inputData.normalWS, mainLight.direction)) * _Translucency;
    color += surfaceData.albedo * mainLight.color * style.litTint * backLit * mainLight.distanceAttenuation;
    color = MixFog(color, inputData.fogCoord);
    outColor = half4(color, 1.0);
#ifdef _WRITE_RENDERING_LAYERS
    outRenderingLayers = EncodeMeshRenderingLayer();
#endif
}
#endif

// ---------------------------------------------------------------- ShadowCaster
#if defined(WORLDTOON_FOLIAGE_SHADOW)
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
float3 _LightDirection;
float3 _LightPosition;

struct FoliageShadowVaryings
{
    float2 uv         : TEXCOORD0;
    float4 positionCS : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

FoliageShadowVaryings FoliageShadowVertex(FoliageAttributes input)
{
    FoliageShadowVaryings output = (FoliageShadowVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    float3 positionWS = FoliagePositionWS(input);
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
#if _CASTING_PUNCTUAL_LIGHT_SHADOW
    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
#else
    float3 lightDirectionWS = _LightDirection;
#endif
    output.positionCS = ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS)));
    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
    return output;
}

half4 FoliageShadowFragment(FoliageShadowVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    FoliageClip(input.uv, input.positionCS);
    return 0;
}
#endif

// ---------------------------------------------------------------- DepthOnly / DepthNormals
#if defined(WORLDTOON_FOLIAGE_DEPTH)
struct FoliageDepthVaryings
{
    float2 uv         : TEXCOORD0;
    half3 normalWS    : TEXCOORD1;
    float4 positionCS : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

FoliageDepthVaryings FoliageDepthVertex(FoliageAttributes input)
{
    FoliageDepthVaryings output = (FoliageDepthVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    output.positionCS = TransformWorldToHClip(FoliagePositionWS(input));
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
    return output;
}

half FoliageDepthOnlyFragment(FoliageDepthVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    FoliageClip(input.uv, input.positionCS);
    return input.positionCS.z;
}

void FoliageDepthNormalsFragment(
    FoliageDepthVaryings input
    , FRONT_FACE_TYPE frontFace : FRONT_FACE_SEMANTIC
    , out half4 outNormalWS : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out uint outRenderingLayers : SV_Target1
#endif
)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    FoliageClip(input.uv, input.positionCS);
    // 컬러 패스와 같은 노멀(뒷면 반전·상향 혼합)을 써야 SSAO가 일치한다.
    float3 normalWS = FoliageNormalWS(NormalizeNormalPerPixel(input.normalWS), IS_FRONT_VFACE(frontFace, true, false));
#if defined(_GBUFFER_NORMALS_OCT)
    float2 octNormalWS = PackNormalOctQuadEncode(normalWS);
    outNormalWS = half4(PackFloat2To888(saturate(octNormalWS * 0.5 + 0.5)), 0.0);
#else
    outNormalWS = half4(normalWS, 0.0);
#endif
#ifdef _WRITE_RENDERING_LAYERS
    outRenderingLayers = EncodeMeshRenderingLayer();
#endif
}
#endif

// ---------------------------------------------------------------- MotionVectors
#if defined(WORLDTOON_FOLIAGE_MOTION)
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MotionVectorsCommon.hlsl"

struct FoliageMotionVaryings
{
    float4 positionCS                 : SV_POSITION;
    float4 positionCSNoJitter         : POSITION_CS_NO_JITTER;
    float4 previousPositionCSNoJitter : PREV_POSITION_CS_NO_JITTER;
    float2 uv                         : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

FoliageMotionVaryings FoliageMotionVertex(FoliageAttributes input)
{
    FoliageMotionVaryings output = (FoliageMotionVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float3 positionWS = FoliagePositionWS(input);
    output.positionCS = TransformWorldToHClip(positionWS);
    output.positionCSNoJitter = mul(_NonJitteredViewProjMatrix, float4(positionWS, 1));

    float4 previousPositionOS = (unity_MotionVectorsParams.x == 1) ? float4(input.positionOld, 1) : input.positionOS;
    float3 previousPositionWS = mul(UNITY_PREV_MATRIX_M, previousPositionOS).xyz;
#if defined(_WT_WIND)
    previousPositionWS = WorldToonWindPreviousPositionWS(previousPositionWS, input.color);
#endif
    output.previousPositionCSNoJitter = mul(_PrevViewProjMatrix, float4(previousPositionWS, 1));
    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
    return output;
}

float4 FoliageMotionFragment(FoliageMotionVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    FoliageClip(input.uv, input.positionCS);
    return float4(CalcNdcMotionVectorFromCsPositions(input.positionCSNoJitter, input.previousPositionCSNoJitter), 0, 0);
}
#endif

#endif
