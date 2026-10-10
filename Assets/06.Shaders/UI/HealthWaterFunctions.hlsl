#ifndef UPLAYGROUND_HEALTH_WATER_INCLUDED
#define UPLAYGROUND_HEALTH_WATER_INCLUDED

// 체력 경계는 Image.Filled가 담당하고, 물결은 내부 명암만 바꿉니다.
void HealthWater_float(UnityTexture2D MainTex, float4 UV, float4 SpriteRect,
    float Clock, float Speed, float WaveHeight, float Detail,
    float4 DeepColor, float4 SurfaceColor, out float3 Color, out float Alpha)
{
    float2 localUV = saturate((UV.xy - SpriteRect.xy) / max(SpriteRect.zw, 0.00001));
    float phase = Clock * Speed;
    float wave = sin(localUV.x * 18.0 - phase);
    float crossWave = sin(localUV.x * 31.0 + phase * 0.7);
    float surface = 0.80 + (wave + crossWave * 0.35) * WaveHeight;
    float depth = smoothstep(0.0, surface, localUV.y);
    float crest = 1.0 - smoothstep(0.025, 0.12, abs(localUV.y - surface));
    float ribbon = 1.0 - smoothstep(0.02, 0.18,
        abs(localUV.y - (0.40 + wave * 0.10 + crossWave * 0.06)));
    float bottom = smoothstep(0.0, 0.18, localUV.y);
    Color = lerp(DeepColor.rgb, SurfaceColor.rgb, depth * 0.72) * lerp(0.65, 1.0, bottom);
    Color += SurfaceColor.rgb * (crest * 0.32 + ribbon * Detail);
    Color = lerp(Color, DeepColor.rgb, smoothstep(surface, 1.0, localUV.y) * 0.35);
    Alpha = SAMPLE_TEXTURE2D(MainTex.tex, MainTex.samplerstate, UV.xy).a;
}
#endif
