// WorldToon 공통 툰 라이팅. Mesh·Foliage·Terrain4가 같은 명암 규칙을 쓰도록 표면 입력과 분리한다.
// 규칙: 명암 단계(band)는 N·L만으로 정하고, 그림자맵 가시성은 band와 한 번만 결합한다.
//       그림자를 band에 섞은 뒤 다시 곱하면 그림자가 이중으로 어두워진다.
#ifndef WORLDTOON_LIGHTING_INCLUDED
#define WORLDTOON_LIGHTING_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

struct WorldToonStyle
{
    half3 litTint;
    half3 shadeTint;
    half bandThreshold;
    half bandSoftness;
    half indirectStrength;
    half additionalLightStrength;
};

// 툰 명암 계수. 0=어두운 면, 1=밝은 면.
half WorldToonBand(half3 normalWS, half3 lightDirWS, WorldToonStyle style)
{
    half ndl = dot(normalWS, lightDirWS);
    half edge = max(style.bandSoftness, half(0.001));
    return smoothstep(style.bandThreshold - edge, style.bandThreshold + edge, ndl);
}

// 주광: 어두운 면과 그림자 영역은 같은 Shade Tint로 수렴한다. 광원이 꺼지면(color=0) 기여도 0이다.
half3 WorldToonMainLight(Light light, half3 normalWS, WorldToonStyle style)
{
    half band = WorldToonBand(normalWS, light.direction, style);
    half lit = band * light.shadowAttenuation;
    half3 tint = lerp(style.shadeTint, style.litTint, lit);
    return light.color * light.distanceAttenuation * tint;
}

// 추가 광원은 단순 확산광이다. 광원마다 다단 램프를 반복하지 않는다.
half3 WorldToonAdditionalLight(Light light, half3 normalWS, WorldToonStyle style)
{
    half ndl = saturate(dot(normalWS, light.direction));
    half visibility = light.distanceAttenuation * light.shadowAttenuation;
    return light.color * (ndl * visibility * style.additionalLightStrength);
}

// 최종 색. albedo는 선형 공간 BaseMap × BaseColor. 발광은 v1 범위 밖이다.
half3 WorldToonShade(InputData inputData, half3 albedo, WorldToonStyle style)
{
    uint meshRenderingLayers = GetMeshRenderingLayer();
    half4 shadowMask = CalculateShadowMask(inputData);
    AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData.normalizedScreenSpaceUV, half(1.0));
    Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);

    half3 bakedGI = inputData.bakedGI;
    MixRealtimeAndBakedGI(mainLight, inputData.normalWS, bakedGI, aoFactor);

    half3 lighting = bakedGI * style.indirectStrength * aoFactor.indirectAmbientOcclusion;

#ifdef _LIGHT_LAYERS
    if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
#endif
    {
        lighting += WorldToonMainLight(mainLight, inputData.normalWS, style);
    }

#if defined(_WT_ADDITIONAL_LIGHTS)
    #if defined(_ADDITIONAL_LIGHTS)
    uint pixelLightCount = GetAdditionalLightsCount();

    #if USE_CLUSTER_LIGHT_LOOP
    // Forward+: 주광이 아닌 방향광은 클러스터 루프 밖에서 먼저 순회한다(설치 URP의 공식 순서).
    [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
    {
        CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK
        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
    #ifdef _LIGHT_LAYERS
        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
    #endif
        {
            lighting += WorldToonAdditionalLight(light, inputData.normalWS, style);
        }
    }
    #endif

    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
    #ifdef _LIGHT_LAYERS
        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
    #endif
        {
            lighting += WorldToonAdditionalLight(light, inputData.normalWS, style);
        }
    LIGHT_LOOP_END
    #endif

    #if defined(_ADDITIONAL_LIGHTS_VERTEX)
    lighting += inputData.vertexLighting * style.additionalLightStrength;
    #endif
#endif

    return albedo * lighting;
}

#endif
