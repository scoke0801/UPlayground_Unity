Shader "Hidden/UPlayGround/Toon Fog Composite"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Toon Fog Composite"
            Blend One OneMinusSrcAlpha
            ColorMask RGB
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "FogEvaluation.hlsl"

            float4 Vert(uint vertexID : SV_VertexID) : SV_POSITION
            {
                return GetFullScreenTriangleVertexPosition(vertexID);
            }

            float4 Frag(float4 screenPosition : SV_POSITION) : SV_Target
            {
                float2 uv = screenPosition.xy / _ScaledScreenParams.xy;
                float depth = SampleSceneDepth(uv);
                bool sky;
                #if UNITY_REVERSED_Z
                    sky = depth <= 0;
                    depth = sky ? 0.5 : depth;
                #else
                    sky = depth >= 1;
                    depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, sky ? 0.5 : depth);
                #endif
                float3 origin = GetCameraPositionWS();
                float3 surface = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);
                if (sky) surface = origin + normalize(surface - origin) * _UPGToonFogDistance.z;
                ToonFogResult fog = EvaluateToonFog(origin, surface, sky ? TOON_FOG_LOCAL : TOON_FOG_ALL);
                if (_UPGToonFogDebug == 0) return float4(fog.scattering, 1 - fog.transmittance);

                float value = 0;
                switch (_UPGToonFogDebug)
                {
                    case 1: value = distance(origin, surface) / _UPGToonFogDistance.z; break;
                    case 2: value = 0.5 + (surface.y - _UPGToonFogHeight.y) / (2 * _UPGToonFogHeight.z); break;
                    case 3: value = fog.transmittance; break;
                    case 4: value = 1 - exp(-fog.tau.x); break;
                    case 5: value = 1 - exp(-fog.tau.y); break;
                    case 6: value = 1 - exp(-fog.tau.z); break;
                    case 7: value = fog.hitCount / max(1, _UPGToonFogLocalCount); break;
                    case 8: return float4(frac(fog.volumeId * float3(0.37, 0.61, 0.83)), 1);
                }
                return float4(saturate(value).xxx, 1);
            }
            ENDHLSL
        }
    }
}
