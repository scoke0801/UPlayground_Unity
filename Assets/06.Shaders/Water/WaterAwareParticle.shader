Shader "UPlayGround/Environment/Water Aware Particle"
{
    Properties
    {
        _BaseMap("Texture", 2D) = "white" {}
        _BaseColor("Color", Color) = (1,1,1,0.25)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend One OneMinusSrcAlpha ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #define UPG_WATER_TRANSPARENT_SURFACE
            #include "WaterCommon.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST, _BaseColor;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; float2 uv:TEXCOORD1; float4 color:TEXCOORD2; float fog:TEXCOORD3; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS=TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS=TransformWorldToHClip(output.positionWS);
                output.uv=TRANSFORM_TEX(input.uv,_BaseMap);
                output.color=input.color*_BaseColor;
                output.fog=ComputeFogFactor(output.positionCS.z);
                return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                half4 color=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv)*input.color;
                float2 delta=input.uv*2-1;
                color.a*=saturate((1-dot(delta,delta))*3);
                bool waterPath = false;
                if (_WaterSceneActive > 0.5)
                    color.rgb = WaterApplyTransparent(input.positionWS, color.rgb, input.positionCS.xy / _ScaledScreenParams.xy, waterPath);
                else if (_UPGToonFogEnabled < 0.5)
                    color.rgb = MixFog(color.rgb, input.fog);
                // 수면 뒤 입자는 기존 수역 경계에서 잘린다. 남은 공기 쪽 입자에만 표면 안개를 적용한다.
                if (_UPGToonFogEnabled > 0.5 && !waterPath)
                    color.rgb = ToonFogStraight(color.rgb, EvaluateToonFog(GetCameraPositionWS(), input.positionWS, TOON_FOG_ALL));
                return half4(color.rgb*color.a,color.a);
            }
            ENDHLSL
        }
    }
}
