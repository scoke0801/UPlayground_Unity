Shader "UPlayGround/Environment/Toon Waterfall"
{
    Properties
    {
        _WaterColor("Water",Color)=(0.08,0.38,0.43,1)
        _FoamColor("Foam",Color)=(0.72,0.88,0.87,1)
        _FlowSpeed("Flow m/s",Float)=3.5
        _BandFrequency("Bands per m",Float)=1.8
        _Wobble("Lateral Wobble m",Float)=0.06
        _Cutoff("Edge Cutoff",Range(0,1))=0.18
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float4 _WaterColor,_FoamColor;
            float _FlowSpeed,_BandFrequency,_Wobble,_Cutoff;
        CBUFFER_END
        float _WaterClock,_WaterSceneActive;
        struct Attributes {float4 positionOS:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
        struct Varyings {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;float2 uv:TEXCOORD1;float fog:TEXCOORD2;};
        Varyings Vert(Attributes input)
        {
            Varyings output;
            float clock=_WaterSceneActive>0.5?_WaterClock:_Time.y;
            input.positionOS.x+=sin(input.uv.y*1.7-clock*2)*_Wobble*input.color.r;
            output.positionWS=TransformObjectToWorld(input.positionOS.xyz);
            output.positionCS=TransformWorldToHClip(output.positionWS);
            output.uv=input.uv;
            output.fog=ComputeFogFactor(output.positionCS.z);
            return output;
        }
        float WaterfallBand(float2 uv)
        {
            float clock=_WaterSceneActive>0.5?_WaterClock:_Time.y;
            float stripe=sin(uv.x*31+sin(uv.y*1.7-clock)*0.7);
            float band=sin((uv.y-clock*_FlowSpeed)*_BandFrequency*6.283185+stripe*0.5);
            float edge=saturate((1-abs(uv.x*2-1))*8);
            clip(edge-(0.04+0.1*stripe)-_Cutoff);
            return smoothstep(0.35,0.75,band)*0.6+pow(abs(uv.x*2-1),4)*0.3;
        }
        ENDHLSL
        Pass
        {
            Name "WaterfallForward"
            Tags { "LightMode"="UniversalForwardOnly" }
            Cull Off ZWrite On
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            half4 Frag(Varyings input):SV_Target
            {
                float band=WaterfallBand(input.uv);
                Light sun=GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float3 color=lerp(_WaterColor.rgb,_FoamColor.rgb,band);
                color*=max(SampleSH(float3(0,1,0)),0.3)+sun.color*sun.shadowAttenuation*0.45;
                return half4(MixFog(color,input.fog),1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            Cull Off ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment FragDepth
            float4 FragDepth(Varyings input):SV_Target {WaterfallBand(input.uv);return 0;}
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormalsOnly"
            Tags { "LightMode"="DepthNormalsOnly" }
            Cull Off ZWrite On
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment FragNormals
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            float4 FragNormals(Varyings input):SV_Target
            {
                WaterfallBand(input.uv);
                float3 normal = normalize(cross(ddy(input.positionWS), ddx(input.positionWS)));
                #if defined(_GBUFFER_NORMALS_OCT)
                    float2 encoded = saturate(PackNormalOctQuadEncode(normal) * 0.5 + 0.5);
                    return float4(PackFloat2To888(encoded), 0);
                #else
                    return float4(normal, 0);
                #endif
            }
            ENDHLSL
        }
    }
}
