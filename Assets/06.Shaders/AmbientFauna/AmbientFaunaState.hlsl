#ifndef AMBIENT_FAUNA_STATE_INCLUDED
#define AMBIENT_FAUNA_STATE_INCLUDED

// C# ParticleState와 같은 48 byte 레이아웃을 유지한다.
struct FaunaParticle
{
    float4 positionSize;
    float4 velocityPhase;
    float4 headingSeed;
};

float FaunaHash(uint value)
{
    value ^= value >> 16;
    value *= 0x7feb352d;
    value ^= value >> 15;
    value *= 0x846ca68b;
    value ^= value >> 16;
    return (value & 0x00ffffff) / 16777216.0;
}

float3 FaunaSafeDirection(float3 value, float3 fallback)
{
    float squared = dot(value, value);
    return squared > 0.000001 ? value * rsqrt(squared) : fallback;
}
#endif
