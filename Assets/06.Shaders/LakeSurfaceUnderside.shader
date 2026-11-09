Shader "UPlayGround/Water/LakeUnderside"
{
    Properties
    {
        _Color ("Water Color", Color) = (0.12,0.32,0.28,1)
        _RippleScale ("Ripple Scale", Float) = 1.8
        _RippleSpeed ("Ripple Speed", Float) = 0.4
        _RippleStrength ("Ripple Strength", Range(0,0.2)) = 0.015
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            Cull Front ZWrite On ColorMask 0
            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float4 DepthVert(float4 positionOS : POSITION) : SV_POSITION { return TransformObjectToHClip(positionOS.xyz); }
            half4 DepthFrag() : SV_Target { return 0; }
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormalsOnly" }
            Cull Front ZWrite On
            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment NormalFrag
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
            float4 DepthVert(float4 positionOS : POSITION) : SV_POSITION { return TransformObjectToHClip(positionOS.xyz); }
            half4 NormalFrag() : SV_Target
            {
                #if defined(_GBUFFER_NORMALS_OCT)
                    float2 normal = PackNormalOctQuadEncode(float3(0,-1,0));
                    return half4(PackFloat2To888(saturate(normal * .5 + .5)),0);
                #else
                    return half4(0,-1,0,0);
                #endif
            }
            ENDHLSL
        }
        Pass
        {
            Tags { "LightMode"="UniversalForwardOnly" }
            Cull Front ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _RippleScale, _RippleSpeed, _RippleStrength;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 world : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.world = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.world);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = input.world.xz * _RippleScale;
                float time = _Time.y * _RippleSpeed;
                float ripple = sin(p.x + sin(p.y * .7 + time) + time) * cos(p.y * .8 - time);
                float3 view = normalize(_WorldSpaceCameraPos - input.world);
                float window = pow(saturate(-view.y), 5);
                Light sun = GetMainLight();
                float light = clamp(max(sun.color.r,max(sun.color.g,sun.color.b)), .06, 1);
                return half4((_Color.rgb * (0.65 + window * .65) + ripple * _RippleStrength) * light, 1);
            }
            ENDHLSL
        }
    }
}
