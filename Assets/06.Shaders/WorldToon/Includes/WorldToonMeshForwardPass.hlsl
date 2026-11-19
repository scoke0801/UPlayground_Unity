// WorldToon Mesh 컬러 패스(UniversalForward). 정점 구조·GI·Fog·데칼 연결은 설치 URP 17.3 SimpleLit 패스를 따른다.
#ifndef WORLDTOON_MESH_FORWARD_PASS_INCLUDED
#define WORLDTOON_MESH_FORWARD_PASS_INCLUDED

#include "WorldToonLighting.hlsl"
#if defined(LOD_FADE_CROSSFADE)
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif
// DBuffer.hlsl(ApplyDecalToSurfaceData, _DBUFFER)은 Lighting.hlsl이 포함한다.

struct Attributes
{
    float4 positionOS        : POSITION;
    float3 normalOS          : NORMAL;
    float4 tangentOS         : TANGENT;
    float2 texcoord          : TEXCOORD0;
    float2 staticLightmapUV  : TEXCOORD1;
    float2 dynamicLightmapUV : TEXCOORD2;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float2 uv                     : TEXCOORD0;
    float3 positionWS             : TEXCOORD1;
    half3 normalWS                : TEXCOORD2;
#ifdef _ADDITIONAL_LIGHTS_VERTEX
    half4 fogFactorAndVertexLight : TEXCOORD5;
#else
    half fogFactor                : TEXCOORD5;
#endif
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    float4 shadowCoord            : TEXCOORD6;
#endif
    DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 7);
#ifdef DYNAMICLIGHTMAP_ON
    float2 dynamicLightmapUV      : TEXCOORD8;
#endif
#ifdef USE_APV_PROBE_OCCLUSION
    float4 probeOcclusion         : TEXCOORD9;
#endif
    float4 positionCS             : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

WorldToonStyle LoadWorldToonStyle()
{
    WorldToonStyle style;
    style.litTint = _LitTint.rgb;
    style.shadeTint = _ShadeTint.rgb;
    style.bandThreshold = _BandThreshold;
    style.bandSoftness = _BandSoftness;
    style.indirectStrength = _IndirectStrength;
    style.additionalLightStrength = _AdditionalLightStrength;
    return style;
}

void InitializeWorldToonInputData(Varyings input, out InputData inputData)
{
    inputData = (InputData)0;
    inputData.positionWS = input.positionWS;
#if defined(DEBUG_DISPLAY)
    inputData.positionCS = input.positionCS;
#endif
    inputData.normalWS = NormalizeNormalPerPixel(input.normalWS);
    inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    inputData.shadowCoord = input.shadowCoord;
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
#else
    inputData.shadowCoord = float4(0, 0, 0, 0);
#endif

#ifdef _ADDITIONAL_LIGHTS_VERTEX
    inputData.fogCoord = InitializeInputDataFog(float4(inputData.positionWS, 1.0), input.fogFactorAndVertexLight.x);
    inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
#else
    inputData.fogCoord = InitializeInputDataFog(float4(inputData.positionWS, 1.0), input.fogFactor);
    inputData.vertexLighting = half3(0, 0, 0);
#endif

    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

#if defined(DEBUG_DISPLAY)
    #if defined(DYNAMICLIGHTMAP_ON)
    inputData.dynamicLightmapUV = input.dynamicLightmapUV.xy;
    #endif
    #if defined(LIGHTMAP_ON)
    inputData.staticLightmapUV = input.staticLightmapUV;
    #else
    inputData.vertexSH = input.vertexSH;
    #endif
#endif
}

// GI는 프로젝트가 실제로 쓰는 경로 하나만 고른다(SH/Lightmap/APV 중복 합산 금지).
void InitializeWorldToonBakedGI(Varyings input, inout InputData inputData)
{
#if defined(_SCREEN_SPACE_IRRADIANCE)
    inputData.bakedGI = SAMPLE_GI(_ScreenSpaceIrradiance, input.positionCS.xy);
#elif defined(DYNAMICLIGHTMAP_ON)
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.dynamicLightmapUV, input.vertexSH, inputData.normalWS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
#elif !defined(LIGHTMAP_ON) && (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2))
    inputData.bakedGI = SAMPLE_GI(input.vertexSH,
        GetAbsolutePositionWS(inputData.positionWS),
        inputData.normalWS,
        inputData.viewDirectionWS,
        input.positionCS.xy,
        input.probeOcclusion,
        inputData.shadowMask);
#else
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, inputData.normalWS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
#endif
}

Varyings WorldToonMeshVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

#if defined(_FOG_FRAGMENT)
    half fogFactor = 0;
#else
    half fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
#endif

    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
    output.positionWS = vertexInput.positionWS;
    output.positionCS = vertexInput.positionCS;
    output.normalWS = NormalizeNormalPerVertex(normalInput.normalWS);

    OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
#ifdef DYNAMICLIGHTMAP_ON
    output.dynamicLightmapUV = input.dynamicLightmapUV.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
#endif
    OUTPUT_SH4(vertexInput.positionWS, output.normalWS.xyz, GetWorldSpaceNormalizeViewDir(vertexInput.positionWS), output.vertexSH, output.probeOcclusion);

#ifdef _ADDITIONAL_LIGHTS_VERTEX
    half3 vertexLight = VertexLighting(vertexInput.positionWS, normalInput.normalWS);
    output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
#else
    output.fogFactor = fogFactor;
#endif

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    output.shadowCoord = GetShadowCoord(vertexInput);
#endif
    return output;
}

void WorldToonMeshFragment(
    Varyings input
    , out half4 outColor : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out uint outRenderingLayers : SV_Target1
#endif
)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    half4 baseSample = SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
    half alpha = Alpha(baseSample.a, _BaseColor, _Cutoff);

    SurfaceData surfaceData = (SurfaceData)0;
    surfaceData.albedo = baseSample.rgb * _BaseColor.rgb;
    surfaceData.alpha = alpha;
    surfaceData.occlusion = 1;

#ifdef LOD_FADE_CROSSFADE
    LODFadeCrossFade(input.positionCS);
#endif

    InputData inputData;
    InitializeWorldToonInputData(input, inputData);
    SETUP_DEBUG_TEXTURE_DATA(inputData, UNDO_TRANSFORM_TEX(input.uv, _BaseMap));

#if defined(_DBUFFER)
    // 스킬 바닥 표시 등 DBuffer 데칼을 라이팅 전에 표면에 합성한다.
    ApplyDecalToSurfaceData(input.positionCS, surfaceData, inputData);
#endif

    InitializeWorldToonBakedGI(input, inputData);

    half3 color = WorldToonShade(inputData, surfaceData.albedo, LoadWorldToonStyle());
    color = MixFog(color, inputData.fogCoord);
    outColor = half4(color, 1.0);

#ifdef _WRITE_RENDERING_LAYERS
    outRenderingLayers = EncodeMeshRenderingLayer();
#endif
}

#endif
