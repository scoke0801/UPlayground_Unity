#ifndef UPG_VEGETATION_REACTION_INCLUDED
#define UPG_VEGETATION_REACTION_INCLUDED

TEXTURE2D(_EnvironmentCellMask);
SAMPLER(sampler_EnvironmentCellMask);

#if defined(_ENVIRONMENT_REACTION_BUFFER)
struct VegetationBufferState
{
    float4 surface;
    float4 thermal;
};
StructuredBuffer<VegetationBufferState> _EnvironmentReactionStates;
#if defined(UNITY_INSTANCING_ENABLED)
UNITY_INSTANCING_BUFFER_START(EnvironmentReactionInstances)
    UNITY_DEFINE_INSTANCED_PROP(float, _EnvironmentReactionIndex)
UNITY_INSTANCING_BUFFER_END(EnvironmentReactionInstances)
#endif

float VegetationBufferIndex()
{
#if defined(UNITY_INSTANCING_ENABLED)
    float index = UNITY_ACCESS_INSTANCED_PROP(EnvironmentReactionInstances, _EnvironmentReactionIndex);
#else
    float index = _EnvironmentReactionIndex;
#endif
    if (index >= 0) return index;
    uint rendererValue = unity_RendererUserValue;
    return (rendererValue & 0x80000000u) != 0 ? (float)(rendererValue & 0x7fffffffu) : index;
}
#endif

// 원본의 조명·바람·알파 계산 뒤 상태만 합성해 마른 식생의 외형을 유지한다.
float4 VegetationReactionState(float3 positionWS)
{
    if (_UseEnvironmentCells > 0.5)
    {
        float2 uv = (positionWS.xz - _EnvironmentCellBounds.xy) * _EnvironmentCellBounds.zw;
        if (any(uv < 0) || any(uv >= 1)) return 0;
        return SAMPLE_TEXTURE2D_LOD(_EnvironmentCellMask, sampler_EnvironmentCellMask, uv, 0);
    }
#if defined(_ENVIRONMENT_REACTION_BUFFER)
    float index = VegetationBufferIndex();
    if (index >= 0) return _EnvironmentReactionStates[(uint)index].surface;
#endif
    return _EnvState0;
}

void ApplyVegetationReaction(float3 positionWS, inout float3 color, inout float3 emission)
{
    float4 state = VegetationReactionState(positionWS);
    if (state.x == 0 && state.y == 0 && state.w == 0) return;
    color *= lerp(1.0, _WetTint.rgb, saturate(state.x));
    color = lerp(color, color * _CharTint.rgb, saturate(state.y));
    emission *= lerp(1.0, _WetTint.rgb, saturate(state.x));
    emission = lerp(emission, emission * _CharTint.rgb, saturate(state.y));
    emission += _FireColor.rgb * saturate(state.w) * (0.2 + 0.8 * saturate(state.y));
}

void ClipVegetationFuel(float3 positionWS)
{
    if (_AllowFuelCoverage < 0.5) return;
    // Terrain의 연료 소진은 원본 밀도를 변경한다. 마스크 alpha는 연소 상태다.
    if (_UseEnvironmentCells > 0.5) return;
    float coverage = saturate(_FuelCoverage);
#if defined(_ENVIRONMENT_REACTION_BUFFER)
    float index = VegetationBufferIndex();
    if (index >= 0) coverage = saturate(_EnvironmentReactionStates[(uint)index].thermal.y);
#endif
    if (coverage >= 1) return;
    float noise = frac(sin(dot(floor(positionWS * 32), float3(12.9898, 78.233, 37.719))) * 43758.5453);
    clip(coverage - noise - 0.00001);
}
#endif
