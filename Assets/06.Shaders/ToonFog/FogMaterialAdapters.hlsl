#ifndef UPG_TOON_FOG_MATERIAL_ADAPTERS
#define UPG_TOON_FOG_MATERIAL_ADAPTERS
#include "FogEvaluation.hlsl"

float3 ToonFogStraight(float3 color, ToonFogResult fog)
{
    return color * fog.transmittance + fog.scattering;
}

float3 ToonFogPremultiplied(float3 color, float alpha, ToonFogResult fog)
{
    return color * fog.transmittance + alpha * fog.scattering;
}

float3 ToonFogAdditive(float3 emission, ToonFogResult fog)
{
    return emission * fog.transmittance;
}
#endif
