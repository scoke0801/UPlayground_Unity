Shader "Hidden/UPlayGround/LakeUnderwater"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            ZWrite Off ZTest Always Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D_X_FLOAT(_LakeDepthTexture);
            float4 _LakeScattering, _LakeExtinction, _LakeSurface, _LakeCaustic;
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                half4 original = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                float depth = SAMPLE_TEXTURE2D_X(_LakeDepthTexture, sampler_PointClamp, uv).r;
                #if !UNITY_REVERSED_Z
                    depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, depth);
                #endif
                float3 world = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);
                float3 ray = world - _WorldSpaceCameraPos;
                float distanceInWater = min(length(ray), _LakeExtinction.w);
                // 수면 밖의 하늘까지 물속 거리로 계산하지 않는다.
                float3 direction = normalize(ray);
                if (direction.y > 0.0001)
                    distanceInWater = min(distanceInWater, max(0, (_LakeSurface.x - _WorldSpaceCameraPos.y) / direction.y));
                float3 transmission = exp(-max(_LakeExtinction.xyz, 0) * distanceInWater);
                Light sun = GetMainLight();
                float illumination = clamp(max(sun.color.r, max(sun.color.g, sun.color.b)), _LakeSurface.z, 1);
                float3 scattering = _LakeScattering.rgb * illumination;
                float2 p = world.xz * _LakeCaustic.y;
                float time = _Time.y * _LakeCaustic.z;
                float lines = sin(p.x + sin(p.y + time)) + sin(p.y * 1.3 - time + sin(p.x * .7));
                float caustic = pow(saturate(1 - abs(lines) * 2), 6);
                float3 normal = normalize(cross(ddx(world), ddy(world)));
                float bedMask = abs(normal.y) * saturate((_LakeSurface.x - world.y - .3) * 2);
                original.rgb *= 1 + caustic * bedMask * _LakeCaustic.x * illumination;
                float3 color = original.rgb * transmission + scattering * (1 - transmission);
                return half4(lerp(original.rgb, color, _LakeSurface.y), original.a);
            }
            ENDHLSL
        }
    }
}
