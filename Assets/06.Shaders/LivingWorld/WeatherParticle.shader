Shader "UPlayGround/Environment/Weather Particle"
{
    Properties
    {
        _BaseMap("Particle Texture", 2D) = "white" {}
        _BaseColor("Tint", Color) = (1,1,1,0.15)
        _SoftDistance("Intersection Fade (m)", Float) = 0.5
        _CameraFadeNear("Camera Fade Start (m)", Float) = 1
        _CameraFadeFar("Camera Fade End (m)", Float) = 3
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float _SoftDistance, _CameraFadeNear, _CameraFadeFar;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; half4 color:COLOR; float2 uv:TEXCOORD0; };
            struct Varyings
            {
                float4 positionCS:SV_POSITION;
                half4 color:COLOR;
                float2 uv:TEXCOORD0;
                float3 positionWS:TEXCOORD1;
                half fog:TEXCOORD2;
            };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = p.positionCS;
                output.positionWS = p.positionWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.color = input.color * _BaseColor;
                output.fog = ComputeFogFactor(p.positionCS.z);
                return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                float2 screenUV = input.positionCS.xy / _ScaledScreenParams.xy;
                float rawDepth = SampleSceneDepth(screenUV);
                #if !UNITY_REVERSED_Z
                    rawDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1, rawDepth);
                #endif
                float3 scenePosition = ComputeWorldSpacePosition(screenUV, rawDepth, UNITY_MATRIX_I_VP);
                float surfaceDepth = -TransformWorldToView(scenePosition).z;
                float particleDepth = -TransformWorldToView(input.positionWS).z;
                half soft = saturate((surfaceDepth - particleDepth) / max(_SoftDistance, 0.001));
                half nearFade = smoothstep(_CameraFadeNear, max(_CameraFadeFar, _CameraFadeNear + 0.001),
                    distance(_WorldSpaceCameraPos, input.positionWS));
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * input.color;
                color.rgb = MixFog(color.rgb, input.fog);
                color.a *= soft * nearFade;
                return color;
            }
            ENDHLSL
        }
    }
}
