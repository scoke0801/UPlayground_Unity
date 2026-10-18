Shader "UPlayGround/Environment/Creek Ground"
{
    Properties
    {
        _BaseMap("Soil Albedo", 2D) = "white" {}
        _BaseColor("Dry Soil Tint", Color) = (1,1,1,1)
        _WetColor("Wet Silt Tint", Color) = (0.4,0.37,0.3,1)
        _WetSmoothness("Wet Smoothness", Range(0,1)) = 0.28
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor, _WetColor;
            half _WetSmoothness;
        CBUFFER_END
        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float2 uv : TEXCOORD0;
            half4 color : COLOR;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
            half3 normalWS : TEXCOORD1;
            float2 uv : TEXCOORD2;
            half4 color : COLOR;
            half fog : TEXCOORD3;
            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
        };
        Varyings Vert(Attributes input)
        {
            Varyings o = (Varyings)0;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_TRANSFER_INSTANCE_ID(input,o);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
            VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
            o.positionCS = p.positionCS;
            o.positionWS = p.positionWS;
            o.normalWS = TransformObjectToWorldNormal(input.normalOS);
            o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
            o.color = input.color;
            o.fog = ComputeFogFactor(p.positionCS.z);
            return o;
        }
        void ClipBank(Varyings input)
        {
            // Deferred 깊이 선행 패스와 같은 픽셀을 잘라 실제 지형이 가장자리에 드러나게 한다.
            float grain = InterleavedGradientNoise(input.positionCS.xy,0);
            clip(input.color.a - max(grain,0.001));
        }
        ENDHLSL
        Pass
        {
            Name "CreekGround"
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                ClipBank(input);
                half wet = input.color.r;
                half3 normal = normalize(input.normalWS);
                Light sun = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv).rgb;
                albedo *= lerp(_BaseColor.rgb,_WetColor.rgb,wet);
                half3 illumination = SampleSH(normal) + sun.color * saturate(dot(normal,sun.direction)) * sun.shadowAttenuation;
                half3 view = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half shine = pow(saturate(dot(normal,SafeNormalize(view+sun.direction))),32);
                half3 color = albedo * illumination + shine * wet * _WetSmoothness * sun.color * sun.shadowAttenuation;
                return half4(MixFog(color,input.fog),1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormalsOnly"
            Tags { "LightMode"="DepthNormalsOnly" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment DepthNormals
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            half4 DepthNormals(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                ClipBank(input);
                half3 normal = normalize(input.normalWS);
                #if defined(_GBUFFER_NORMALS_OCT)
                    float2 oct = PackNormalOctQuadEncode(normal);
                    return half4(PackFloat2To888(saturate(oct * 0.5 + 0.5)),0);
                #else
                    return half4(normal,0);
                #endif
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ColorMask R
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Depth
            #pragma multi_compile_instancing
            half4 Depth(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                ClipBank(input);
                return 0;
            }
            ENDHLSL
        }
    }
}
