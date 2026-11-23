#ifndef UPG_SAND_FOOTPRINTS
#define UPG_SAND_FOOTPRINTS

Texture2DArray<float2> _UPGSandCache;
Texture2D<float> _UPGSandMask;
SamplerState sampler_UPGSandCache;
SamplerState sampler_UPGSandMask;
float _UPGSandEnabled;
float4 _UPGSandTiles[25]; // 원점 XZ, 감쇠, 슬롯 유효 여부
float4 _UPGSandReceiver; // 월드 원점 XZ, 크기의 역수
float4 _UPGSandGeometry; // 타일 크기, 텍셀 크기, 내부 해상도, 패딩 해상도의 역수
float4 _UPGSandRelief;   // 눌림 깊이, 테두리 높이, 알베도·거칠기 보정
float4 _UPGSandMaskSize;
float3 _UPGSandMinTile;  // 첫 타일 XZ와 격자 한 변의 타일 수
float2 _UPGSandLogicalOffset;
float4 _UPGSandView;    // 논리 추적 위치 XZ, 완전 표시 거리, 표시 종료 거리

float SandHeight(float2 channels)
{
    return dot(channels, float2(-_UPGSandRelief.x, _UPGSandRelief.y));
}

void ApplySandFootprints(float2 worldXZ, inout half3 normalWS, inout half3 albedo, inout half smoothness)
{
    [branch] if (_UPGSandEnabled < 0.5) return;
    float2 receiver = (worldXZ - _UPGSandReceiver.xy) * _UPGSandReceiver.zw;
    if (any(receiver < 0) || any(receiver > 1)) return;
    float2 logicalXZ = worldXZ + _UPGSandLogicalOffset;
    float distanceFromAnchor = distance(logicalXZ, _UPGSandView.xy);
    if (distanceFromAnchor >= _UPGSandView.w) return;
    int grid = (int)_UPGSandMinTile.z;
    int2 tile = (int2)floor(logicalXZ / _UPGSandGeometry.x);
    int2 relative = tile - (int2)_UPGSandMinTile.xy;
    if (any(relative < 0) || any(relative >= grid)) return;
    int2 wrapped = (tile % grid + grid) % grid;
    int slot = wrapped.y * grid + wrapped.x;
    float4 info = _UPGSandTiles[slot];
    if (info.w == 0) return;
    float2 maskUV = (receiver * (_UPGSandMaskSize.xy - 1) + 0.5) * _UPGSandMaskSize.zw;
    float mask = smoothstep(0.35, 0.65, _UPGSandMask.SampleLevel(sampler_UPGSandMask, maskUV, 0));
    mask *= 1 - smoothstep(_UPGSandView.z, _UPGSandView.w, distanceFromAnchor);
    if (mask <= 0) return;
    float texel = _UPGSandGeometry.w;
    float2 uv = ((logicalXZ - info.xy) / _UPGSandGeometry.x * _UPGSandGeometry.z + 2) * texel;
    float3 sampleAt = float3(uv, slot);
    float2 dr = _UPGSandCache.SampleLevel(sampler_UPGSandCache, sampleAt, 0) * (info.z * mask);
    float right = SandHeight(_UPGSandCache.SampleLevel(sampler_UPGSandCache, sampleAt + float3(texel, 0, 0), 0));
    float left = SandHeight(_UPGSandCache.SampleLevel(sampler_UPGSandCache, sampleAt - float3(texel, 0, 0), 0));
    float front = SandHeight(_UPGSandCache.SampleLevel(sampler_UPGSandCache, sampleAt + float3(0, texel, 0), 0));
    float back = SandHeight(_UPGSandCache.SampleLevel(sampler_UPGSandCache, sampleAt - float3(0, texel, 0), 0));
    float2 slope = float2(right - left, front - back) * (info.z * mask / (2 * _UPGSandGeometry.y));
    float3 gradient = float3(slope.x, 0, slope.y);
    normalWS = normalize(normalWS - (gradient - normalWS * dot(gradient, normalWS)));
    albedo *= 1 + _UPGSandRelief.z * (0.5 * dr.y - dr.x);
    smoothness = saturate(smoothness + _UPGSandRelief.w * dr.x);
}
#endif
