#ifndef UPG_TOON_FOG_EVALUATION
#define UPG_TOON_FOG_EVALUATION

#include "FogIntegration.hlsl"

#define TOON_FOG_DISTANCE 1u
#define TOON_FOG_HEIGHT 2u
#define TOON_FOG_LOCAL 4u
#define TOON_FOG_ALL 7u
#define TOON_FOG_SLOTS 8

TEXTURE2D(_UPGToonFogPalette);
TEXTURE2D(_UPGToonFogNoise);
SAMPLER(sampler_UPGToonFogPalette);
SAMPLER(sampler_UPGToonFogNoise);

float _UPGToonFogEnabled;
int _UPGToonFogLocalCount, _UPGToonFogSamples, _UPGToonFogDebug;
float4 _UPGToonFogDistance; // 밀도, 보호 거리, 평가 상한, 최대 불투명도
float4 _UPGToonFogHeight;   // 밀도, 기준 고도, 높이 스케일
float4 _UPGToonFogColors;   // 거리 팔레트 시작/끝
float4 _UPGToonFogHeightTint;
float4 _UPGToonFogEnvironmentTint;
float4x4 _UPGToonFogWorldToLocal[TOON_FOG_SLOTS];
float4 _UPGToonFogLocalSettings[TOON_FOG_SLOTS]; // 밀도, 형상, 경계, 노이즈 비율
float4 _UPGToonFogLocalTint[TOON_FOG_SLOTS];     // RGB, 진단 ID
float4 _UPGToonFogLocalNoise[TOON_FOG_SLOTS];    // 주파수, 바람 이동 UV

struct ToonFogResult
{
    float transmittance;
    float3 scattering;
    float3 tau;
    float hitCount;
    float volumeId;
};

ToonFogResult EvaluateToonFogRange(float3 origin, float3 direction, float start, float end, uint layers)
{
    ToonFogResult fog = (ToonFogResult)0;
    fog.transmittance = 1;
    if (_UPGToonFogEnabled < 0.5) return fog;
    float surfaceDistance = end;
    start = max(start, _UPGToonFogDistance.y);
    end = min(end, _UPGToonFogDistance.z);
    if (end <= start) return fog;

    float3 weighted = 0;
    if ((layers & TOON_FOG_DISTANCE) != 0 && _UPGToonFogDistance.x > 0)
    {
        fog.tau.x = (end - start) * _UPGToonFogDistance.x;
        float u = saturate((surfaceDistance - _UPGToonFogColors.x) /
            (_UPGToonFogColors.y - _UPGToonFogColors.x));
        weighted += fog.tau.x * SAMPLE_TEXTURE2D_LOD(_UPGToonFogPalette,
            sampler_UPGToonFogPalette, float2((u * 255 + 0.5) / 256, 0.5), 0).rgb;
    }
    if ((layers & TOON_FOG_HEIGHT) != 0 && _UPGToonFogHeight.x > 0)
    {
        fog.tau.y = _UPGToonFogHeight.x * ToonFogHeightIntegral(
            origin.y + direction.y * start, origin.y + direction.y * end, end - start,
            _UPGToonFogHeight.y, _UPGToonFogHeight.z);
        weighted += fog.tau.y * _UPGToonFogHeightTint.rgb;
    }
    if ((layers & TOON_FOG_LOCAL) != 0)
    {
        float strongest = 0;
        [loop] for (int i = 0; i < _UPGToonFogLocalCount; i++)
        {
            float4 settings = _UPGToonFogLocalSettings[i];
            if (settings.x <= 0) continue;
            float3 localOrigin = mul(_UPGToonFogWorldToLocal[i], float4(origin, 1)).xyz;
            float3 localDirection = mul((float3x3)_UPGToonFogWorldToLocal[i], direction);
            float entry = start, exit = end;
            bool intersects;
            if (settings.y < 0.5)
                intersects = ToonFogClipBox(localOrigin, localDirection, entry, exit);
            else
                intersects = ToonFogClipSphere(localOrigin, localDirection, entry, exit);
            if (!intersects) continue;

            float integral = exit - entry;
            if (settings.z > 0 || settings.w > 0)
            {
                float stepLength = integral / _UPGToonFogSamples;
                integral = 0;
                [loop] for (int j = 0; j < _UPGToonFogSamples; j++)
                {
                    float t = entry + (j + 0.5) * stepLength;
                    float3 p = abs(localOrigin + localDirection * t);
                    float edge = 1 - (settings.y < 0.5 ? max(p.x, max(p.y, p.z)) : length(p));
                    float mask = settings.z > 0 ? smoothstep(0, settings.z, edge) : 1;
                    if (settings.w > 0)
                    {
                        float2 uv = (origin.xz + direction.xz * t) * _UPGToonFogLocalNoise[i].x +
                            _UPGToonFogLocalNoise[i].yz;
                        float noise = SAMPLE_TEXTURE2D_LOD(_UPGToonFogNoise, sampler_UPGToonFogNoise, uv, 0).r;
                        mask *= lerp(1, noise * 2, settings.w);
                    }
                    integral += mask * stepLength;
                }
            }
            float tau = settings.x * integral;
            fog.tau.z += tau;
            weighted += tau * _UPGToonFogLocalTint[i].rgb;
            fog.hitCount += 1;
            if (tau > strongest)
            {
                strongest = tau;
                fog.volumeId = _UPGToonFogLocalTint[i].a;
            }
        }
    }
    float totalTau = dot(fog.tau, float3(1, 1, 1));
    if (totalTau <= 1e-7) return fog;
    float opacity = min(1 - exp(-min(totalTau, 80)), _UPGToonFogDistance.w);
    fog.transmittance = 1 - opacity;
    fog.scattering = (weighted / totalTau) * _UPGToonFogEnvironmentTint.rgb * opacity;
    return fog;
}

ToonFogResult EvaluateToonFog(float3 origin, float3 surface, uint layers)
{
    float3 delta = surface - origin;
    float distanceToSurface = length(delta);
    return EvaluateToonFogRange(origin, delta / max(distanceToSurface, 1e-6), 0, distanceToSurface, layers);
}

#endif
