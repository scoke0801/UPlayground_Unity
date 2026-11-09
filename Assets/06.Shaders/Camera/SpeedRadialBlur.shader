Shader "Hidden/UPlayGround/SpeedRadialBlur"
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
            float4 _SpeedRadialBlur;
            float4 _SpeedRadialBlurFlow;
            float4 _SpeedRadialBlurTextureSize;

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                float2 radial = uv - 0.5;
                float mask = smoothstep(_SpeedRadialBlur.y, _SpeedRadialBlur.z, length(radial * 2.0));
                if (mask <= 0.0)
                    return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                float2 flow = radial * _SpeedRadialBlurFlow.z + _SpeedRadialBlurFlow.xy;
                float2 offset = flow * (_SpeedRadialBlur.x * mask);
                float2 textureSize = max(_SpeedRadialBlurTextureSize.xy, 1.0);
                float pixelLength = length(offset * textureSize);
                float maxPixels = _SpeedRadialBlurFlow.w * textureSize.y;
                offset *= min(1.0, maxPixels / max(pixelLength, 0.0001));
                pixelLength = min(pixelLength, maxPixels);
                if (pixelLength < 0.5)
                    return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                int sampleCount = clamp((int)ceil(pixelLength) + 1, 4, 16);
                half4 color = 0;
                half weightSum = 0;
                [loop] for (int sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
                {
                    float progress = (float)sampleIndex / (sampleCount - 1);
                    // 본체 쪽을 더 남겨 화면 전체가 균일하게 뿌옇게 보이지 않도록 한다.
                    half weight = 1.0 - progress * 0.5;
                    color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv - offset * progress, 0) * weight;
                    weightSum += weight;
                }
                return color / weightSum;
            }
            ENDHLSL
        }
    }
}
