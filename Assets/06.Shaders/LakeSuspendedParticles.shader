Shader "UPlayGround/Water/SuspendedParticles"
{
    Properties { _Color ("Color", Color) = (.3,.5,.42,.18) _SurfaceY ("Surface Height", Float) = 77 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off ZWrite Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _SurfaceY;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; float height:TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 world=TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS=TransformWorldToHClip(world);
                output.height=world.y;
                output.uv=input.uv;
                output.color=input.color*_Color;
                return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                clip(_SurfaceY-input.height);
                float radius=length(input.uv*2-1);
                return half4(input.color.rgb,input.color.a*saturate(1-radius)*saturate(1-radius));
            }
            ENDHLSL
        }
    }
}
