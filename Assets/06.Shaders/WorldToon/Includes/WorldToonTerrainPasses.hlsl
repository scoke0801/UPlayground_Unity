// WorldToon Terrain4 컬러 패스. 정점·스플랫 혼합·Holes·인스턴싱·보조 패스는 설치 URP TerrainLitPasses.hlsl을 그대로 쓰고
// 최종 라이팅만 WorldToon으로 바꾼다. 4개 레이어를 한 패스에서 혼합한 뒤 툰 라이팅을 한 번만 한다(AddPass 없음).
#ifndef WORLDTOON_TERRAIN_PASSES_INCLUDED
#define WORLDTOON_TERRAIN_PASSES_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/Shaders/Terrain/TerrainLitPasses.hlsl"
#include "WorldToonLighting.hlsl"

void WorldToonTerrainFragment(
    Varyings IN
    , out half4 outColor : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out uint outRenderingLayers : SV_Target1
#endif
    )
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
#ifdef _ALPHATEST_ON
    ClipHoles(IN.uvMainAndLM.xy);
#endif

    half3 normalTS = half3(0.0h, 0.0h, 1.0h);
#ifdef TERRAIN_SPLAT_BASEPASS
    // 원거리: Basemap Gen이 만든 혼합 알베도. 시간대 라이팅은 굽지 않았으므로 근거리와 같은 라이팅을 한다.
    half3 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uvMainAndLM.xy).rgb;
#else
    float2 splatUV = (IN.uvMainAndLM.xy * (_Control_TexelSize.zw - 1.0f) + 0.5f) * _Control_TexelSize.xy;
    half4 splatControl = SAMPLE_TEXTURE2D(_Control, sampler_Control, splatUV);
    half weight;
    half4 mixedDiffuse;
    half4 defaultSmoothness;
    SplatmapMix(IN.uvMainAndLM, IN.uvSplat01, IN.uvSplat23, splatControl, weight, mixedDiffuse, defaultSmoothness, normalTS);
    half3 albedo = mixedDiffuse.rgb;
#endif

    InputData inputData;
    InitializeInputData(IN, normalTS, inputData);
    SetupTerrainDebugTextureData(inputData, IN.uvMainAndLM.xy);

#if defined(_DBUFFER)
    half3 specular = half3(0.0h, 0.0h, 0.0h);
    half metallic = 0;
    half occlusion = 1;
    half smoothness = 0;
    ApplyDecal(IN.clipPos, albedo, specular, inputData.normalWS, metallic, occlusion, smoothness);
#endif

    InitializeBakedGIData(IN, inputData);

    WorldToonStyle style;
    style.litTint = _LitTint.rgb;
    style.shadeTint = _ShadeTint.rgb;
    style.bandThreshold = _BandThreshold;
    style.bandSoftness = _BandSoftness;
    style.indirectStrength = _IndirectStrength;
    style.additionalLightStrength = _AdditionalLightStrength;

    half3 color = WorldToonShade(inputData, albedo, style);
    color = MixFog(color, inputData.fogCoord);
    outColor = half4(color, 1.0h);

#ifdef _WRITE_RENDERING_LAYERS
    outRenderingLayers = EncodeMeshRenderingLayer();
#endif
}

#endif
