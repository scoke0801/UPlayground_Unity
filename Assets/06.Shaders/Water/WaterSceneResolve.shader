Shader "Hidden/UPlayGround/WaterSceneResolve"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "WaterSceneResolve"
            ZWrite Off ZTest Always Cull Off
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "WaterCommon.hlsl"
            TEXTURE2D_X_FLOAT(_WaterOpaqueDepth);
            int _WaterDebugView;

            struct WaterRay { float3 origin, direction; float nearDistance; };
            struct WaterHit { float3 position; float distance; bool sky; };

            float WaterDeviceDepth(float raw)
            {
                #if !UNITY_REVERSED_Z
                    return lerp(UNITY_NEAR_CLIP_VALUE, 1, raw);
                #else
                    return raw;
                #endif
            }

            WaterRay BuildWaterRay(float2 uv)
            {
                #if UNITY_REVERSED_Z
                    float nearDepth = 1, farDepth = 0;
                #else
                    float nearDepth = UNITY_NEAR_CLIP_VALUE, farDepth = 1;
                #endif
                float3 nearPosition = ComputeWorldSpacePosition(uv, nearDepth, UNITY_MATRIX_I_VP);
                float3 farPosition = ComputeWorldSpacePosition(uv, farDepth, UNITY_MATRIX_I_VP);
                WaterRay ray;
                ray.direction = normalize(farPosition - nearPosition);
                ray.origin = unity_OrthoParams.w > 0.5 ? nearPosition - ray.direction * _ProjectionParams.y : GetCameraPositionWS();
                ray.nearDistance = distance(ray.origin, nearPosition);
                return ray;
            }

            WaterHit ReadWaterHit(float2 uv, WaterRay ray)
            {
                float raw = SAMPLE_TEXTURE2D_X_LOD(_WaterOpaqueDepth, sampler_PointClamp, uv, 0).r;
                WaterHit hit;
                #if UNITY_REVERSED_Z
                    hit.sky = raw < 0.000001;
                #else
                    hit.sky = raw > 0.999999;
                #endif
                hit.position = ComputeWorldSpacePosition(uv, WaterDeviceDepth(raw), UNITY_MATRIX_I_VP);
                hit.distance = max(0, dot(hit.position - ray.origin, ray.direction));
                return hit;
            }

            float4 RepairWaterNearClip(WaterRay ray, float4 information)
            {
                float nearest = ray.nearDistance;
                float4 synthetic = information;
                [loop] for (uint bodyIndex = 0; bodyIndex < (uint)_WaterBodyCount; bodyIndex++)
                {
                    WaterBody body = _WaterBodies[bodyIndex];
                    [loop] for (uint i = 0; i < (uint)body.range.y; i++)
                    {
                        WaterCell cell = _WaterCells[(uint)body.range.x + i];
                        float startValue = ray.origin.y - WaterSurfaceHeight(cell, body, ray.origin.xz);
                        float3 baseNormal = WaterCellNormal(cell);
                        baseNormal /= max(baseNormal.y, 0.00001);
                        float waveSlope = abs(body.wave0.z) * length(body.wave0.xy) + abs(body.wave1.z) * length(body.wave1.xy);
                        float heightRate = abs(ray.direction.y) + dot(abs(baseNormal.xz), abs(ray.direction.xz)) + waveSlope * length(ray.direction.xz);
                        if (abs(startValue) > heightRate * ray.nearDistance + 0.00001) continue;
                        float previousDistance = 0;
                        [loop] for (uint nearStep = 1; nearStep <= 8; nearStep++)
                        {
                            float nextDistance = ray.nearDistance * (nearStep / 8.0);
                            float3 endPoint = ray.origin + ray.direction * nextDistance;
                            float endValue = endPoint.y - WaterSurfaceHeight(cell, body, endPoint.xz);
                            if (startValue * endValue <= 0)
                            {
                                float crossing = WaterFindCrossing(cell, body, ray.origin, ray.direction, previousDistance, nextDistance, startValue <= 0);
                                float3 samplePosition = ray.origin + ray.direction * crossing;
                                if (crossing < nearest && WaterPointInCell(cell, samplePosition.xz) && WaterPointInVolume(body, samplePosition))
                                {
                                    float2 gradient = WaterWaveGradient(samplePosition.xz, _WaterClock, body.wave0) + WaterWaveGradient(samplePosition.xz, _WaterClock, body.wave1);
                                    float3 normal = normalize(baseNormal + float3(-gradient.x, 0, -gradient.y));
                                    nearest = crossing;
                                    synthetic = float4(-TransformWorldToView(samplePosition).z, WaterEncodeNormal(normal),
                                        (dot(ray.direction, normal) < 0 ? 1 : -1) * (bodyIndex + 1));
                                }
                            }
                            previousDistance = nextDistance;
                            startValue = endValue;
                        }
                    }
                }
                return synthetic;
            }

            float3 AddWaterCaustics(float3 color, float3 position, uint bodyIndex, bool sky)
            {
                if (bodyIndex == WATER_NO_BODY || sky) return color;
                WaterBody body = _WaterBodies[bodyIndex];
                float3 dx = ddx(position), dy = ddy(position);
                float3 normal = normalize(cross(dx, dy));
                float continuity = 1 - saturate(abs(length(dx) - length(dy)) / max(0.01, min(length(dx), length(dy)) * 4));
                float depth = WaterBodyHeight(body, position.xz) - position.y;
                float fade = saturate(depth * 2) * exp(-max(body.extinction.x, body.extinction.y) * max(0, depth));
                Light light = GetMainLight(TransformWorldToShadowCoord(position));
                float sun = saturate(abs(normal.y)) * saturate(light.direction.y) * light.shadowAttenuation;
                float2 p = position.xz * body.caustic.y;
                float time = _WaterClock * body.caustic.z;
                float a = sin(p.x + sin(p.y * 1.3 + time)) + sin(p.y - time + sin(p.x * 0.7));
                float b = sin(p.x * 0.83 - time * 0.6) + sin(p.y * 1.17 + time * 0.8);
                float pattern = pow(saturate(1 - abs(a) * 1.8), 5) * pow(saturate(1 - abs(b) * 0.4), 2);
                return color * (1 + pattern * body.caustic.x * fade * sun * continuity);
            }

            float3 DebugWater(float3 color, float4 surface, WaterOptics optics)
            {
                if (optics.overflow > 0) return float3(1, 0, 1);
                if (_WaterDebugView == 1)
                {
                    float id = abs(surface.w) > 0.5 ? abs(surface.w) : (optics.endBody == WATER_NO_BODY ? 0 : optics.endBody + 1);
                    return id > 0 ? frac(id * float3(0.37, 0.61, 0.83)) : 0;
                }
                if (_WaterDebugView == 2) return float3(saturate(optics.length / 20), saturate(optics.length / 5), 0);
                if (_WaterDebugView == 3) return abs(surface.w) > 0.5 ? WaterDecodeNormal(surface.yz) * 0.5 + 0.5 : 0;
                if (_WaterDebugView == 4) return optics.length > 0 ? float3(0.05, 0.65, 0.9) : float3(0.5, 0.5, 0.5);
                if (_WaterDebugView == 5) return float3(0, 0.5, 0);
                return color;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                WaterRay ray = BuildWaterRay(uv);
                WaterHit opaque = ReadWaterHit(uv, ray);
                half4 original = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv, 0);
                float4 surface = SAMPLE_TEXTURE2D_X_LOD(_WaterSurfaceInfo, sampler_PointClamp, uv, 0);
                surface = RepairWaterNearClip(ray, surface);
                float eyeScale = max(abs(TransformWorldToViewDir(ray.direction, false).z), 0.00001);
                float surfaceDistance = surface.x / eyeScale;
                float airDistance = 0;
                if (abs(surface.w) < 0.5 || surfaceDistance >= opaque.distance || surfaceDistance < 0)
                {
                    WaterOptics optics = WaterEvaluateMedium(ray.origin, ray.direction, 0, opaque.distance, airDistance, !opaque.sky);
                    float3 scene = AddWaterCaustics(original.rgb, opaque.position, optics.endBody, opaque.sky);
                    float3 color = WaterApplyOptics(scene, optics);
                    return half4(DebugWater(color, surface, optics), original.a);
                }

                uint bodyIndex = (uint)abs(surface.w) - 1;
                WaterBody body = _WaterBodies[bodyIndex];
                float3 surfacePosition = ray.origin + ray.direction * surfaceDistance;
                float3 outwardNormal = WaterDecodeNormal(surface.yz);
                bool above = surface.w > 0;
                float3 normal = above ? outwardNormal : -outwardNormal;
                float edge = saturate(distance(surfacePosition, opaque.position) / max(body.style2.z, 0.001));
                float2 refractedUV = saturate(uv + TransformWorldToViewDir(normal, true).xy * body.style0.x * edge);
                WaterRay refractedRay = BuildWaterRay(refractedUV);
                WaterHit refracted = ReadWaterHit(refractedUV, refractedRay);
                float4 shiftedSurface = SAMPLE_TEXTURE2D_X_LOD(_WaterSurfaceInfo, sampler_PointClamp, refractedUV, 0);
                // UV를 되돌릴 때 깊이도 함께 되돌려 수면 앞 물체와 다른 높이의 수역이 번지지 않게 한다.
                if (-TransformWorldToView(refracted.position).z <= surface.x + 0.002
                    || (abs(shiftedSurface.w) > 0.5 && abs(shiftedSurface.w) != abs(surface.w)))
                {
                    refractedUV = uv;
                    refracted = opaque;
                }
                WaterOptics front = WaterEvaluateMedium(ray.origin, ray.direction, 0, surfaceDistance, airDistance, true);
                float3 toBackground = refracted.position - surfacePosition;
                float backgroundDistance = length(toBackground);
                WaterOptics back = WaterEvaluateMedium(surfacePosition, toBackground / max(backgroundDistance, 0.00001), 0, backgroundDistance, airDistance, !refracted.sky);
                float3 background = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, refractedUV, 0).rgb;
                background = AddWaterCaustics(background, refracted.position, back.endBody, refracted.sky);
                float3 transmitted = WaterApplyOptics(background, back);

                Light sun = GetMainLight(TransformWorldToShadowCoord(surfacePosition));
                float3 light = max(SampleSH(normal), body.style0.w.xxx) + sun.color * saturate(dot(normal, sun.direction)) * sun.shadowAttenuation;
                // 광학 감쇠와 별개로 기존 재질의 수면 색/불투명도를 유지해 얕은 물도 읽히게 한다.
                float waterDepth = max(0, surfacePosition.y - refracted.position.y);
                float depthBlend = 1 - exp2(-waterDepth / max(body.style2.w, 0.001));
                if (above)
                    transmitted = lerp(transmitted, lerp(body.shallow.rgb, body.deep.rgb, depthBlend) * light, saturate(body.style1.x));
                float facing = saturate(dot(-ray.direction, normal));
                float fresnel = 0.02 + 0.98 * pow(1 - facing, 5);
                if (!above && facing < 0.6614) fresnel = 1;
                float reflectionWeight = above ? body.style0.y * fresnel : fresnel;
                float3 reflected = GlossyEnvironmentReflection(reflect(ray.direction, normal), 1 - body.style0.z, 1);
                // 수중 반사는 공기 쪽 하늘을 그대로 비추지 않도록 매체의 원거리 방사휘도로 제한한다.
                if (!above) reflected = body.scattering.rgb * max(body.scattering.w, saturate(sun.color.r + sun.color.g + sun.color.b));
                reflected *= lerp(float3(1, 1, 1), body.shallow.rgb, 0.2);
                float3 colorAtSurface = lerp(transmitted, reflected, reflectionWeight);
                float3 halfway = SafeNormalize(-ray.direction + sun.direction);
                float glint = pow(saturate(dot(normal, halfway)), exp2(3 + body.style0.z * 7));
                colorAtSurface += sun.color * sun.shadowAttenuation * glint * body.style0.y;
                float foamPattern = sin(waterDepth * body.style2.x - _WaterClock * body.style2.y + dot(normal.xz, float2(3, 4)));
                float foam = above ? (1 - smoothstep(0, max(body.style1.z, 0.001), waterDepth))
                    * smoothstep(-0.35, 0.65, foamPattern) * body.style1.w * edge : 0;
                float2 stampGradient = WaterStampGradient(surfacePosition, bodyIndex);
                foam += saturate(length(stampGradient) * 2) * body.style1.w;
                colorAtSurface = lerp(colorAtSurface, body.foam.rgb * light, saturate(foam));
                float3 color = WaterApplyOptics(colorAtSurface, front);
                front.length += back.length;
                front.overflow |= back.overflow;
                return half4(DebugWater(color, surface, front), original.a);
            }
            ENDHLSL
        }
    }
}
