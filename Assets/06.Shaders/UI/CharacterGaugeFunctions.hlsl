#ifndef UPLAYGROUND_CHARACTER_GAUGE_FUNCTIONS
#define UPLAYGROUND_CHARACTER_GAUGE_FUNCTIONS

// 이미지 알파는 윤곽만 담당한다. 다른 문양용 흐름 맵의 알파로 새 이미지를 잘라내지 않는다.
void GaugeArtwork_float(UnityTexture2D Artwork, UnityTexture2D FlowMap, UnityTexture2D FillMask,
    float2 UV, float4 SpriteRect, float FillMode,
    out float4 Pixel, out float Order, out float Phase, out float Coverage)
{
    float2 localUV = saturate((UV - SpriteRect.xy) / max(SpriteRect.zw, 0.00001));
    Pixel = SAMPLE_TEXTURE2D(Artwork.tex, Artwork.samplerstate, UV);
    float4 flow = SAMPLE_TEXTURE2D(FlowMap.tex, FlowMap.samplerstate, localUV);
    Coverage = SAMPLE_TEXTURE2D(FillMask.tex, FillMask.samplerstate, localUV).a;
    Order = localUV.x;
    if (FillMode > 0.5 && FillMode < 1.5) Order = localUV.y;
    if (FillMode > 1.5 && FillMode < 2.5) Order = abs(localUV.x * 2.0 - 1.0);
    if (FillMode > 2.5 && FillMode < 3.5)
        Order = frac(atan2(localUV.x - 0.5, localUV.y - 0.5) / 6.2831853 + 1.0);
    Phase = Order + localUV.y * 0.35;
    if (FillMode > 3.5) { Order = flow.r; Phase = flow.g; }
}

float GaugeCoverage(float amount, float order, float width)
{
    // 0%와 100%에서는 경계 흔들림으로 에너지가 남거나 빈 틈이 생기지 않는다.
    if (amount <= 0.0) return 0.0;
    if (amount >= 1.0) return 1.0;
    return 1.0 - smoothstep(amount - width, amount + width, order);
}

void GaugeCharge_float(float Order, float Phase, float Coverage, float Resource, float Trail,
    float Clock, float FlowSpeed, float FlowFrequency, float EdgeWidth, float NoiseStrength,
    out float Fill, out float Afterimage, out float Edge, out float Flow)
{
    float wave = (Phase * FlowFrequency - Clock * FlowSpeed) * 6.2831853;
    float order = Order + sin(wave) * NoiseStrength;
    float width = max(EdgeWidth, 0.0001);
    // 획득은 부드럽게 차오르고 소비는 즉시 반영하며 이전 값만 잔상으로 남긴다.
    float shown = min(saturate(Resource), saturate(Trail));
    Fill = GaugeCoverage(shown, order, width) * Coverage;
    Afterimage = max(0.0, GaugeCoverage(Trail, Order, width) * Coverage - Fill);
    Edge = saturate(1.0 - abs(order - shown) / (width * 2.0));
    Edge *= step(0.00001, shown) * (1.0 - step(0.99999, shown)) * Coverage;
    Flow = pow(saturate(sin(wave) * 0.5 + 0.5), 8.0) * Fill;
}

void GaugeColor_float(float4 Pixel, float Fill, float Afterimage, float Edge, float Flow,
    float4 BaseColor, float4 EnergyColor, float4 ReadyColor, float ReadyPulse, float Locked,
    float GlowIntensity, float DetailStrength, float ImageColorInfluence,
    out float3 Color, out float Alpha)
{
    float3 artwork = lerp(float3(1, 1, 1), Pixel.rgb, saturate(ImageColorInfluence));
    float luminance = dot(artwork, float3(0.2126, 0.7152, 0.0722));
    // 원본 금속 테두리와 중앙 문장은 남기고, 빈 내부는 중성색 명암으로 구분한다.
    // 어두운 픽셀의 최소 명도를 보장하며 배경 대비는 별도 외곽선이 담당한다.
    float3 empty = BaseColor.rgb * lerp(0.25, 1.0, luminance * luminance);
    float3 energy = artwork * EnergyColor.rgb;
    Color = lerp(empty, energy, Fill);
    Color = lerp(Color, ReadyColor.rgb, saturate(Afterimage * 0.22));
    float highlight = saturate(Edge * GlowIntensity + Flow * DetailStrength
        + ReadyPulse * GlowIntensity * Fill);
    Color = lerp(Color, ReadyColor.rgb, highlight * (1.0 - saturate(Locked)));
    // 사용 불가는 충전 강조를 억제하되 문양 자체를 배경 속으로 어둡게 만들지 않는다.
    Alpha = Pixel.a * lerp(BaseColor.a, EnergyColor.a, Fill);
}
#endif
