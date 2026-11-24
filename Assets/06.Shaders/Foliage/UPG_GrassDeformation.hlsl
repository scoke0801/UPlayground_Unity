#ifndef UPG_GRASS_DEFORMATION_INCLUDED
#define UPG_GRASS_DEFORMATION_INCLUDED

#include "Assets/06.Shaders/Foliage/UPG_FoliageInteraction.hlsl"

float4 _LW_Wind;
float4 _LW_PreviousWind;
sampler2D _WindNoiseTexture;
float WindNoise01;
float WindNoise01Multiplier;
float WindNoise02;
float WindNoise02Multiplier;

float UPG_GrassRootMask(float uvHeight, float vertexRed)
{
    #if defined(_USEVERTEXCOLOR_ON)
        return pow(abs(vertexRed + _VertexColorOffset), _VertexColorGradient);
    #else
        float height = saturate(uvHeight / 0.6294294);
        #ifndef UNITY_COLORSPACE_GAMMA
            height = SRGBToLinear(height);
        #endif
        return pow(height, _LockPositionGradient);
    #endif
}

float3 UPG_GrassWindOffsetWS(float3 positionWS, float rootMask, float4 wind, float time, float4x4 objectToWorld)
{
    #if defined(_USEGLOBALWINDSETTINGS_ON)
        float2 noiseSize = float2(WindNoise01, WindNoise02);
        float2 noiseAmount = float2(WindNoise01Multiplier, WindNoise02Multiplier);
    #else
        float2 noiseSize = float2(_WindNoise01Size, _WindNoise02Size);
        float2 noiseAmount = float2(_WindNoise01Multiplier, _WindNoise02Multiplier);
    #endif

    float2 uv1 = positionWS.xz / (abs(noiseSize.x) < 0.001 ? 0.001 : noiseSize.x) + float2(0, 0.2) * time;
    float2 uv2 = positionWS.xz / (abs(noiseSize.y) < 0.001 ? 0.001 : noiseSize.y) + float2(0, 0.1) * time;
    float3 noiseOS = 0.5 * rootMask * (tex2Dlod(_WindNoiseTexture, float4(uv1, 0, 0)).rgb * noiseAmount.x +
                                      tex2Dlod(_WindNoiseTexture, float4(uv2, 0, 0)).rgb * noiseAmount.y);
    float3 noiseWS = mul((float3x3)objectToWorld, noiseOS);
    float strength = saturate(wind.w);
    [branch] if (_WindDirectionalBend > 0 && strength > 0.0001)
    {
        float2 direction = wind.xz / max(length(wind.xz), 0.0001);
        float phase = dot(positionWS.xz, direction) * 0.24 - time * 2.1;
        float crossPhase = dot(positionWS.xz, float2(-direction.y, direction.x)) * 0.13;
        // 같은 풍향으로 기울고, 공간 위상이 다른 돌풍이 풀밭을 지나간다.
        float gust = 0.68 + 0.24 * sin(phase + 0.35 * sin(crossPhase)) + 0.08 * sin(0.5 * phase - crossPhase);
        float weight = saturate(rootMask);
        float bend = _WindDirectionalBend * strength * gust * weight * weight;
        noiseWS = noiseWS * (1 - 0.8 * strength) + float3(direction.x, -0.17 * weight, direction.y) * bend;
    }
    return noiseWS;
}

float3 UPG_GrassCurrentOffsetOS(float3 positionOS, float uvHeight, float vertexRed)
{
    float3 positionWS = TransformObjectToWorld(positionOS);
    float rootMask = UPG_GrassRootMask(uvHeight, vertexRed);
    float3 windWS = UPG_GrassWindOffsetWS(positionWS, rootMask, _LW_Wind, _TimeParameters.x, GetObjectToWorldMatrix());
    float3 contactWS = UPG_FoliageBendOffsetWS(positionWS, rootMask);
    return TransformWorldToObjectDir(windWS + contactWS, false);
}

#endif
