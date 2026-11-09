Shader "UPlayGround/UI/UnderwaterBreathOverlay"
{
    // 전체 화면 오버레이. 수중에서는 위에서 비치는 빛 무늬와 가장자리 기포가 있는 청록 색조를,
    // 숨(스태미나)이 바닥나 갈수록 심해색으로 시야가 좁아지며 심박처럼 수축하는 암전을 그린다.
    // 위험 색을 붉게 하지 않는 이유: 붉은 화면은 피격 신호와 겹치고, 익사는 어두워지는 쪽이 직관적이다.
    // 구동은 UI_HUD_BreathWarning이 _UnderwaterBlend/_Danger/_UnscaledTime을 매 프레임 갱신한다.
    Properties
    {
        [HideInInspector] _MainTex ("Sprite", 2D) = "white" {}
        _UnderwaterTint ("수중 색조(수면 쪽)", Color) = (0.10, 0.45, 0.52, 1)
        _DeepTint ("수중 색조(아래쪽)", Color) = (0.01, 0.10, 0.20, 1)
        _DangerColor ("위험 색(암전)", Color) = (0.0, 0.01, 0.04, 1)
        _WashStrength ("수중 색조 세기", Range(0, 1)) = 0.34
        _CausticStrength ("빛 무늬 세기", Range(0, 1)) = 0.22
        _BubbleStrength ("기포 세기", Range(0, 1)) = 0.5
        _UnderwaterBlend ("수중 비율", Range(0, 1)) = 0
        _Danger ("위험도", Range(0, 1)) = 0
        _BeatRateCalm ("박동 속도(여유, Hz)", Range(0.3, 3)) = 0.8
        _BeatRatePanic ("박동 속도(위급, Hz)", Range(0.5, 6)) = 2.0
        _TunnelOpenRadius ("터널 반경(여유)", Range(0.6, 2)) = 1.5
        _TunnelClosedRadius ("터널 반경(위급)", Range(0.1, 1.2)) = 0.38
        _UnscaledTime ("UI 시간", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        // 미리 곱한 알파: 어둡게 덮는 층과 빛을 더하는 층을 한 패스에서 함께 낸다.
        Blend One OneMinusSrcAlpha
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };

            half4 _UnderwaterTint;
            half4 _DeepTint;
            half4 _DangerColor;
            half _WashStrength;
            half _CausticStrength;
            half _BubbleStrength;
            half _UnderwaterBlend;
            half _Danger;
            half _BeatRateCalm;
            half _BeatRatePanic;
            half _TunnelOpenRadius;
            half _TunnelClosedRadius;
            float _UnscaledTime;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            float Hash11(float n) { return frac(sin(n * 127.1) * 43758.5453); }

            float Hash21(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // 수면에서 일렁이며 내려오는 빛 무늬(타일 가능한 물 caustic 근사).
            float Caustic(float2 uv, float time)
            {
                float2 p = uv * 6.2831853 * 1.3;
                float2 i = p;
                float c = 1.0;
                const float inten = 0.005;
                [unroll]
                for (int n = 0; n < 3; n++)
                {
                    float t = time * (1.0 - 3.5 / (n + 1));
                    i = p + float2(cos(t - i.x) + sin(t + i.y), sin(t - i.y) + cos(t + i.x));
                    c += 1.0 / length(float2(p.x / (sin(i.x + t) / inten), p.y / (cos(i.y + t) / inten)));
                }
                c /= 3.0;
                c = 1.17 - pow(c, 1.4);
                return saturate(pow(abs(c), 8.0));
            }

            // 가장자리에서 천천히 올라가는 기포. 열마다 크기·속도·위상이 다르다.
            float Bubbles(float2 centered, float time, float sideMask)
            {
                const float columns = 9.0;
                float x = (centered.x * 0.5 + 0.5) * columns;
                float column = floor(x);
                float seed = Hash11(column + 1.0);
                float speed = 0.05 + seed * 0.07;
                float y = frac(time * speed + seed * 7.0);
                float wobble = sin(time * (0.8 + seed) + seed * 20.0) * 0.12;
                float2 cell = float2(frac(x) - (0.5 + (Hash11(column + 9.0) - 0.5) * 0.4 + wobble), (y * 2.0 - 1.0 - centered.y) * 0.5 * columns * 0.5);
                float radius = 0.1 + Hash11(column + 4.0) * 0.14;
                float d = length(cell);
                float rim = smoothstep(radius, radius * 0.82, d) - smoothstep(radius * 0.82, radius * 0.55, d);
                float fill = smoothstep(radius, radius * 0.4, d) * 0.12;
                return saturate(rim + fill) * sideMask;
            }

            // 느리고 무겁게 뛰는 심박: 강한 박동 뒤 약한 박동이 이어진다.
            float HeartBeat(float phase)
            {
                float first = exp(-phase * 9.0);
                float secondPhase = max(phase - 0.26, 0.0);
                float second = step(0.26, phase) * exp(-secondPhase * 10.0) * 0.5;
                return saturate(first + second);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 centered = input.uv * 2.0 - 1.0;
                float radius = length(centered);
                float time = _UnscaledTime;

                // 수중 색조: 위는 밝은 청록, 아래는 깊은 남색. 가장자리일수록 짙다.
                float edge = smoothstep(0.35, 1.3, radius);
                float depthGradient = saturate(0.5 - centered.y * 0.5);
                half3 washColor = lerp(_UnderwaterTint.rgb, _DeepTint.rgb, depthGradient);
                float washAlpha = _UnderwaterBlend * _WashStrength * (0.5 + 0.5 * edge) * (0.7 + 0.5 * depthGradient);

                // 빛 무늬: 화면 위쪽에서 강하고 아래로 갈수록 사라진다. 위험할수록 빛이 죽는다.
                float2 causticUv = input.uv * float2(1.6, 1.0) + float2(0.0, time * 0.02);
                float caustic = Caustic(causticUv, time * 0.35);
                float causticMask = saturate(1.0 - depthGradient * 1.1) * (1.0 - _Danger * 0.8);
                half3 light = half3(0.55, 0.95, 1.0) * caustic * _CausticStrength * causticMask * _UnderwaterBlend;

                // 기포: 좌우 가장자리에만 있어 시야 중앙을 가리지 않는다.
                float sideMask = smoothstep(0.55, 1.0, abs(centered.x));
                float bubble = Bubbles(centered, time, sideMask) * _BubbleStrength * _UnderwaterBlend;
                half3 bubbleLight = half3(0.7, 0.95, 1.0) * bubble * 0.6;

                // 위험: 반경이 좁아지는 암전. 경계는 노이즈로 일렁여 번지듯 어두워지고, 심박에 맞춰 수축한다.
                float beatRate = lerp(_BeatRateCalm, _BeatRatePanic, _Danger);
                float pulse = HeartBeat(frac(time * beatRate)) * _Danger;
                float wobble = (ValueNoise(centered * 2.2 + time * 0.25) - 0.5) * 0.28 * _Danger;
                float tunnelRadius = lerp(_TunnelOpenRadius, _TunnelClosedRadius, _Danger) - pulse * 0.12 + wobble;
                float tunnel = smoothstep(tunnelRadius, tunnelRadius + 0.65, radius);
                float dangerAlpha = _Danger * saturate(tunnel * 0.97 + pulse * 0.12 * edge);
                // 위험할수록 화면 전체가 조금씩 가라앉는다.
                dangerAlpha = saturate(dangerAlpha + _Danger * _Danger * 0.25);

                // 어두운 층을 먼저 합성하고 그 위에 빛을 더한다(기포·빛 무늬는 암전에 가려진다).
                float visibleLight = 1.0 - dangerAlpha;
                half3 premultiplied = washColor * washAlpha * (1.0 - dangerAlpha)
                                    + _DangerColor.rgb * dangerAlpha
                                    + (light + bubbleLight) * visibleLight;
                half alpha = saturate(washAlpha * (1.0 - dangerAlpha) + dangerAlpha);

                half fade = input.color.a;
                return half4(premultiplied * fade, alpha * fade);
            }
            ENDHLSL
        }
    }
}
