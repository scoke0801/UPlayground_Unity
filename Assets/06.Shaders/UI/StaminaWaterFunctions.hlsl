#ifndef STAMINA_WATER_FUNCTIONS_INCLUDED
#define STAMINA_WATER_FUNCTIONS_INCLUDED

// 원호의 가장 가까운 점을 사용해 잔량과 무관하게 둥근 끝을 유지합니다.
float StaminaArcDistance(float2 p, float2 center, float radius, float start, float end)
{
    float angle = clamp(atan2(p.y - center.y, p.x - center.x), start, end);
    return length(p - center - radius * float2(cos(angle), sin(angle)));
}

void StaminaWater_float(UnityTexture2D MainTex, float4 UV, float4 SpriteRect,
    float Clock, float Speed, float WaveHeight, float Detail,
    float4 DeepColor, float4 SurfaceColor, float Resource, float4 ArcShape,
    float4 FrameColor, float4 TrackColor, float4 EnergyTint,
    out float3 Color, out float Alpha)
{
    // 기존 스프라이트는 아틀라스 UV 전달에만 사용하며 형태는 거리장으로 그립니다.
    float2 p = (UV.xy - SpriteRect.xy) / max(SpriteRect.zw, 0.00001);
    float radius = max(ArcShape.x, 0.01);
    float halfAngle = clamp(ArcShape.y, 0.01, 1.4);
    float outerWidth = max(ArcShape.z, 0.002);
    float fillWidth = clamp(ArcShape.w, 0.001, outerWidth * 0.8);
    float2 center = float2(0.9 - radius, 0.5);
    float distance = StaminaArcDistance(p, center, radius, -halfAngle, halfAngle);
    float aa = max(fwidth(distance), 0.0005);
    float outer = 1.0 - smoothstep(outerWidth - aa, outerWidth + aa, distance);
    float track = 1.0 - smoothstep(outerWidth - 0.008 - aa, outerWidth - 0.008 + aa, distance);
    float shadow = (1.0 - smoothstep(outerWidth, outerWidth + 0.018, distance)) * 0.38;
    float endAngle = lerp(-halfAngle, halfAngle, saturate(Resource));
    float fillDistance = StaminaArcDistance(p, center, radius, -halfAngle, endAngle);
    float filled = (1.0 - smoothstep(fillWidth - aa, fillWidth + aa, fillDistance)) * step(0.0001, Resource);
    float radial = (length(p - center) - radius) / fillWidth;
    float bevel = saturate(1.0 - abs(radial + 0.25) * 0.65);
    float shimmer = sin(p.y * 20.0 - Clock * Speed) * WaveHeight * Detail;
    float3 energy = lerp(DeepColor.rgb, SurfaceColor.rgb, saturate(bevel + shimmer));
    float2 tip = center + radius * float2(cos(endAngle), sin(endAngle));
    float tipLight = 1.0 - smoothstep(0.0, fillWidth * 1.6, length(p - tip));
    energy = lerp(energy, SurfaceColor.rgb * 1.12, tipLight * 0.65) * EnergyTint.rgb;
    float frameAlpha = outer * FrameColor.a;
    float trackAlpha = track * TrackColor.a;
    Alpha = max(shadow, frameAlpha);
    float3 premultiplied = FrameColor.rgb * frameAlpha;
    premultiplied = lerp(premultiplied, TrackColor.rgb * trackAlpha, track);
    Alpha = lerp(Alpha, trackAlpha, track);
    premultiplied = lerp(premultiplied, energy, filled);
    Alpha = lerp(Alpha, 1.0, filled);
    Color = premultiplied / max(Alpha, 0.0001);
}
#endif
