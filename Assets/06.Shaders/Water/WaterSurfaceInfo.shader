Shader "Hidden/UPlayGround/WaterSurfaceInfo"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "WaterSurfaceInfo"
            Tags { "LightMode"="WaterSurfaceInfo" }
            Cull Off ZWrite On ZTest LEqual
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "WaterCommon.hlsl"
            TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);
            TEXTURE2D(_WaterRainExposure); SAMPLER(sampler_WaterRainExposure);
            float4 _WaterWave0, _WaterWave1, _WaveVelocity, _WaterRain, _WaterExposureRect;
            float4 _LW_Wind;
            float _WaveScale, _WaveStrength, _WeatherWindResponse, _WaterBodyIndex;
            float _ShoreFade, _Recession;

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 baseNormal : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 position = TransformObjectToWorld(input.positionOS.xyz);
                position.y += WaterHeightOffset(position.xz, _WaterClock, _WaterWave0, _WaterWave1);
                output.positionCS = TransformWorldToHClip(position);
                output.positionWS = position;
                output.baseNormal = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                return output;
            }

            float WaterHash(float2 value)
            {
                float3 p = frac(float3(value.xyx) * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float2 RainGradient(float3 position)
            {
                if (_WaterRain.x <= 0) return 0;
                float2 gridPosition = position.xz / max(_WaterRain.y, 0.01);
                float footprint = max(length(ddx(gridPosition)), length(ddy(gridPosition)));
                float detailFade = 1 - smoothstep(0.035, 0.15, footprint);
                // 이미 완전히 감쇠된 원경·차폐 구역에서는 고리 9개의 계산을 생략한다.
                [branch] if (detailFade <= 0) return 0;
                float2 uv = (position.xz - _WaterExposureRect.xy) * _WaterExposureRect.zw;
                float exposure = SAMPLE_TEXTURE2D_LOD(_WaterRainExposure, sampler_WaterRainExposure, uv, 0).r;
                [branch] if (exposure <= 0) return 0;
                float2 cell = floor(gridPosition);
                float2 gradient = 0;
                [unroll] for (int x = -1; x <= 1; x++)
                [unroll] for (int y = -1; y <= 1; y++)
                {
                    float2 key = cell + float2(x, y);
                    float seed = WaterHash(key);
                    float clock = _WaterClock * _WaterRain.z + seed;
                    float generation = floor(clock);
                    float age = frac(clock);
                    float2 center = key + float2(WaterHash(key + generation), WaterHash(key + generation + 12.7));
                    float2 delta = gridPosition - center;
                    float distance = length(delta);
                    float phase = (distance - age * 0.45) * 18;
                    // 퍼지는 고리와 뒤따르는 약한 고리를 함께 그려 빗방울 착수점을 읽히게 한다.
                    float ring = -phase * exp(-phase * phase);
                    float trailingPhase = phase + 2.5;
                    ring -= trailingPhase * exp(-trailingPhase * trailingPhase) * 0.35;
                    gradient += delta / max(distance, 0.001) * ring * (1 - age);
                }
                return gradient * exposure * _WaterRain.x * detailFade;
            }

            float4 Frag(Varyings input, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
            {
                uint bodyIndex = (uint)_WaterBodyIndex - 1;
                WaterBody body = _WaterBodies[bodyIndex];
                if (!WaterPointInVolume(body, input.positionWS)) clip(-1);
                if (_ShoreFade > 0)
                {
                    float shore = 1 - smoothstep(1 - _Recession - _ShoreFade, 1 - _Recession, length(input.uv * 2 - 1));
                    clip(shore - 0.01);
                }
                float2 waveUV = input.positionWS.xz * _WaveScale;
                float2 current = (_LW_Wind.xz * _WeatherWindResponse + body.flow.xz) * _WaveScale;
                float3 smallA = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, waveUV + _WaterClock * (_WaveVelocity.xy + current)));
                float3 smallB = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, waveUV * 0.73 + _WaterClock * (_WaveVelocity.zw + current)));
                float2 gradient = WaterWaveGradient(input.positionWS.xz, _WaterClock, _WaterWave0)
                    + WaterWaveGradient(input.positionWS.xz, _WaterClock, _WaterWave1);
                float2 rainGradient = RainGradient(input.positionWS);
                gradient += WaterStampGradient(input.positionWS, bodyIndex) + rainGradient;
                float3 normal = input.baseNormal;
                if (normal.y < 0) normal = -normal;
                normal /= max(normal.y, 0.00001);
                normal = normalize(normal + float3(-gradient.x + (smallA.x + smallB.x) * _WaveStrength,
                    0, -gradient.y + (smallA.y + smallB.y) * _WaveStrength));
                // 정수부는 수역 ID, 소수부 0~0.25는 차폐·거리 감쇠를 적용한 비 파문이다. Point 샘플만 사용한다.
                float encodedBody = _WaterBodyIndex + saturate(length(rainGradient) * 3) * 0.25;
                float signedBody = IS_FRONT_VFACE(face, encodedBody, -encodedBody);
                return float4(-TransformWorldToView(input.positionWS).z, WaterEncodeNormal(normal), signedBody);
            }
            ENDHLSL
        }
    }
}
