#ifndef UPLAYGROUND_WATER_WAVES_INCLUDED
#define UPLAYGROUND_WATER_WAVES_INCLUDED

float WaterWaveHeight(float2 position, float time, float4 wave)
{
    return wave.z * sin(dot(position, wave.xy) - time * wave.w);
}

float2 WaterWaveGradient(float2 position, float time, float4 wave)
{
    return wave.z * wave.xy * cos(dot(position, wave.xy) - time * wave.w);
}

float WaterHeightOffset(float2 position, float time, float4 first, float4 second)
{
    return WaterWaveHeight(position, time, first) + WaterWaveHeight(position, time, second);
}

float2 WaterEncodeNormal(float3 normal)
{
    normal /= max(abs(normal.x) + abs(normal.y) + abs(normal.z), 0.00001);
    float2 encoded = normal.xz;
    return normal.y >= 0 ? encoded : (1 - abs(encoded.yx)) * (step(0, encoded) * 2 - 1);
}

float3 WaterDecodeNormal(float2 encoded)
{
    float3 normal = float3(encoded.x, 1 - abs(encoded.x) - abs(encoded.y), encoded.y);
    if (normal.y < 0) normal.xz = (1 - abs(normal.zx)) * (step(0, normal.xz) * 2 - 1);
    return normalize(normal);
}
#endif
