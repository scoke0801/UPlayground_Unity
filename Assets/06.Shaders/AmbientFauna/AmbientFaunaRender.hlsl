#ifndef AMBIENT_FAUNA_RENDER_INCLUDED
#define AMBIENT_FAUNA_RENDER_INCLUDED
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "AmbientFaunaState.hlsl"

TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
CBUFFER_START(UnityPerMaterial)
float4 _BaseMap_ST, _Tint, _BodyColor;
float _Cutoff, _Emission, _Appearance, _GlowFrequency;
float4x4 _AF_LocalToWorld;
float _AF_FlapAngle, _AF_Fade, _AF_Density, _AF_Time;
float2 _AF_NearFade;
float4 _AF_AmbientColor;
CBUFFER_END
StructuredBuffer<FaunaParticle> _AF_Particles;

struct Attributes
{
    float3 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float2 uv : TEXCOORD0;
    float2 wing : TEXCOORD1;
    float2 hinge : TEXCOORD2;
    uint instanceID : SV_InstanceID;
};
struct Varyings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    float3 normalWS : TEXCOORD1;
    float2 uv : TEXCOORD2;
    float3 appearance : TEXCOORD3; // 날개 영역, 밀도·근접 fade, 발광 위상
    float fog : TEXCOORD4;
};

float3 RotateWing(float3 value, float angle)
{
    float sine, cosine;
    sincos(angle, sine, cosine);
    return float3(value.x * cosine - value.y * sine, value.x * sine + value.y * cosine, value.z);
}

Varyings FaunaVertex(Attributes input)
{
    // 한 command와 startInstance=0만 제출하므로 SV_InstanceID가 버퍼 인덱스다.
    uint id = input.instanceID;
    FaunaParticle particle = _AF_Particles[id];
    float angle = sin(particle.velocityPhase.w) * _AF_FlapAngle * input.wing.x * input.wing.y;
    float3 hinge = float3(input.hinge.x, 0, input.hinge.y);
    float3 vertex = RotateWing(input.positionOS - hinge, angle) + hinge;
    float3 normal = RotateWing(input.normalOS, angle);
    float3 forward = FaunaSafeDirection(float3(particle.headingSeed.x, clamp(particle.headingSeed.y, -0.6, 0.6), particle.headingSeed.z), float3(0, 0, 1));
    float3 right = FaunaSafeDirection(cross(float3(0, 1, 0), forward), float3(1, 0, 0));
    float3 up = cross(forward, right);
    float3 localPosition = particle.positionSize.xyz
        + (right * vertex.x + up * vertex.y + forward * vertex.z) * particle.positionSize.w;
    Varyings output;
    output.positionWS = mul(_AF_LocalToWorld, float4(localPosition, 1)).xyz;
    output.positionCS = TransformWorldToHClip(output.positionWS);
    output.normalWS = normalize(mul((float3x3)_AF_LocalToWorld, right * normal.x + up * normal.y + forward * normal.z));
    output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
    float3 centerWS = mul(_AF_LocalToWorld, float4(particle.positionSize.xyz, 1)).xyz;
    if (_Appearance > 2.5)
    {
        output.positionWS = centerWS + mul((float3x3)UNITY_MATRIX_I_V, float3(vertex.x, vertex.z, 0)) * particle.positionSize.w;
        output.normalWS = FaunaSafeDirection(_WorldSpaceCameraPos - centerWS, float3(0, 0, -1));
        output.positionCS = TransformWorldToHClip(output.positionWS);
    }
    float nearFade = smoothstep(_AF_NearFade.x, _AF_NearFade.y, distance(centerWS, _WorldSpaceCameraPos));
    float populationFade = _AF_Density >= 0.999 ? 1 : _AF_Density <= 0 ? 0
        : saturate((_AF_Density - particle.headingSeed.w) * 16);
    output.appearance = float3(input.wing.y, _AF_Fade * nearFade * populationFade,
        0.6 + 0.4 * sin(_AF_Time * _GlowFrequency * 6.2831853 + particle.headingSeed.w * 20));
    output.fog = ComputeFogFactor(output.positionCS.z);
    return output;
}

half4 SampleFauna(Varyings input)
{
    half4 texel = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
    half wing = saturate(input.appearance.x);
    half4 color = lerp(_BodyColor, texel * _Tint, wing);
    if (_Appearance > 2.5)
    {
        color = _Tint;
        color.a *= 1 - smoothstep(0.12, 0.5, length(input.uv - 0.5));
    }
    clip(color.a - _Cutoff);
    // Forward와 Depth에서 같은 화면 기준 dither를 써서 깊이 실루엣을 맞춘다.
    clip(input.appearance.y - InterleavedGradientNoise(input.positionCS.xy, 0));
    return color;
}

half4 FaunaFragment(Varyings input, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
{
    half4 color = SampleFauna(input);
    float3 normal = IS_FRONT_VFACE(face, input.normalWS, -input.normalWS);
    Light light = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
    half3 lighting = _AF_AmbientColor.rgb + light.color * light.shadowAttenuation
        * (0.25 + 0.75 * saturate(dot(normal, light.direction)));
    half emissionMask = _Appearance > 1.5 && _Appearance < 2.5 ? 1 - saturate(input.appearance.x) : 1;
    color.rgb = color.rgb * lighting + _Tint.rgb * (_Emission * emissionMask * input.appearance.z);
    color.rgb = MixFog(color.rgb, input.fog);
    color.a = 1;
    return color;
}
half4 FaunaDepth(Varyings input) : SV_Target { SampleFauna(input); return 0; }
half4 FaunaDepthNormals(Varyings input, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
{
    SampleFauna(input);
    float3 normal = normalize(IS_FRONT_VFACE(face, input.normalWS, -input.normalWS));
    #if defined(_GBUFFER_NORMALS_OCT)
        float2 octahedral = PackNormalOctQuadEncode(normal);
        return half4(PackFloat2To888(saturate(octahedral * 0.5 + 0.5)), 0);
    #else
        return half4(normal, 0);
    #endif
}
#endif
