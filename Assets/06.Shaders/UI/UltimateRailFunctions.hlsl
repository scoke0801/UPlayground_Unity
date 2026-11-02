#ifndef UPLAYGROUND_ULTIMATE_RAIL_INCLUDED
#define UPLAYGROUND_ULTIMATE_RAIL_INCLUDED

// 칸 번호로 고정 난수를 만든다. 불티 위치·위상이 프레임마다 흔들리지 않게 한다.
float UltimateRailHash(float n)
{
    return frac(sin(n * 127.1) * 43758.5453);
}

// 연속된 광선 대신 작은 밝기 덩어리가 흐르도록 보간한 2차원 노이즈를 쓴다.
float UltimateRailNoise(float2 position)
{
    float2 cell = floor(position);
    float2 blend = frac(position);
    blend = blend * blend * (3.0 - 2.0 * blend);
    float seed = cell.x + cell.y * 157.0;
    return lerp(lerp(UltimateRailHash(seed), UltimateRailHash(seed + 1.0), blend.x),
        lerp(UltimateRailHash(seed + 157.0), UltimateRailHash(seed + 158.0), blend.x), blend.y);
}

// UV.x는 레일 전체 기준 0~1이며 Image Filled가 채움 밖을 잘라 낸다.
// Aspect는 사각형 가로/세로 비로, 파형과 광택 주기를 레일 길이와 무관하게 픽셀 기준으로 맞춘다.
// Ready는 0(평상시 띠)~1(준비 발광) 사이를 스크립트가 보간한다.
// SweepPosition은 큰 충전 직후 지나가는 빛의 UV 위치이며 음수면 꺼진다.
void UltimateRailEnergy_float(float4 UV, float Clock, float FlowSpeed, float Aspect,
    float Ready, float WaveFrequency, float WaveAmplitude, float SweepPosition, float Intensity,
    out float3 Color, out float Alpha)
{
    float x = UV.x * Aspect;
    float y = UV.y * 2.0 - 1.0;
    float aa = max(fwidth(y), 0.001);

    float phase = Clock * FlowSpeed;
    float2 flowUv = float2(x * WaveFrequency, y * 2.5);
    float broad = UltimateRailNoise(flowUv + float2(-phase * 0.35, 0.0));
    float detail = UltimateRailNoise(flowUv * float2(1.7, 1.3) + float2(-phase * 0.57, 9.3));
    float noiseValue = broad * 0.6 + detail * 0.4;
    float glow = smoothstep(0.35, 0.8, noiseValue);
    // 선의 상하 이동을 없애고 면 내부의 명암만 흘려 연속된 광선처럼 보이지 않게 한다.
    float core = 1.0 - smoothstep(0.04, 0.52 + aa, abs(y));
    float contrast = saturate(WaveAmplitude);
    float ready = saturate(Ready);
    float shape = core * lerp(0.72, 1.0, glow);

    float sweep = (1.0 - smoothstep(0.0, 0.06, abs(UV.x - SweepPosition))) * step(0.0, SweepPosition);

    float brightness = (0.8 + (glow - 0.35) * contrast + ready * 0.15)
        * (Intensity + sweep * 0.4);
    Color = brightness.xxx;
    float endFade = smoothstep(0.0, 0.035, UV.x) * smoothstep(0.0, 0.035, 1.0 - UV.x);
    Alpha = saturate(shape + sweep * core * 0.8) * endFade;
}

// 레일 위로 솟아 사라지는 불티. 칸마다 불티 하나가 서로 다른 속도·위상으로 반복한다.
// 이웃 칸까지 함께 계산해 흔들림으로 칸 경계를 넘어도 잘리지 않게 한다.
void UltimateRailSparks_float(float4 UV, float Clock, float Aspect, float Density,
    float RiseSpeed, float Intensity, out float3 Color, out float Alpha)
{
    float cellWidth = 1.0 / max(Density, 0.01);
    float x = UV.x * Aspect;
    float cell = floor(x / cellWidth);
    float glow = 0.0;
    float core = 0.0;
    for (int i = -1; i <= 1; i++)
    {
        float c = cell + i;
        float seed = UltimateRailHash(c);
        float seed2 = UltimateRailHash(c + 17.31);
        float t = frac(Clock * RiseSpeed * (0.6 + seed2 * 0.8) + seed);
        float drift = sin(t * 6.2831 + seed * 12.0) * 0.15;
        float2 spark = float2((c + 0.2 + seed * 0.6 + drift) * cellWidth, t);
        float distance = length(float2(x - spark.x, (UV.y - spark.y) * 1.0));
        float radius = 0.04 + seed2 * 0.035;
        float fade = sin(saturate(t) * 3.14159) * (1.0 - t * 0.35);
        glow += (1.0 - smoothstep(0.0, radius * 3.0, distance)) * fade * 0.55;
        core += (1.0 - smoothstep(radius * 0.3, radius, distance)) * fade;
    }
    Color = lerp(0.7, 1.0, saturate(core)).xxx * Intensity;
    Alpha = saturate(core + glow);
}
#endif
