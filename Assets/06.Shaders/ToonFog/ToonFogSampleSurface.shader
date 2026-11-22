Shader "UPlayGround/Rendering/Toon Fog Sample Surface"
{
    Properties
    {
        _BaseColor("색", Color) = (0.9,0.7,0.2,1)
        [Enum(Opaque,0,Straight,1,Premultiplied,2,Additive,3,Cutout,4)] _Mode("표면", Float) = 0
        [Toggle] _AlphaRamp("알파 그라데이션", Float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _Src("Source Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _Dst("Destination Blend", Float) = 0
        [Toggle] _DepthWrite("ZWrite", Float) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Cull Off
        HLSLINCLUDE
        #pragma target 4.5
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "FogMaterialAdapters.hlsl"
        CBUFFER_START(UnityPerMaterial)
        float4 _BaseColor;
        float _Mode, _AlphaRamp;
        CBUFFER_END
        struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
        struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float2 uv : TEXCOORD1; };
        Varyings Vert(Attributes input)
        {
            Varyings output;
            output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
            output.positionCS = TransformWorldToHClip(output.positionWS);
            output.uv = input.uv;
            return output;
        }

        void ClipSurface(float2 uv)
        {
            if (_Mode == 4) clip(frac(uv.x * 6) * frac(uv.y * 6) - 0.23);
        }

        float4 Frag(Varyings input) : SV_Target
        {
            ClipSurface(input.uv);
            float alpha = _BaseColor.a * lerp(1, saturate(input.uv.x), _AlphaRamp);
            float3 color = _BaseColor.rgb;
            if (_Mode >= 1 && _Mode <= 3)
            {
                ToonFogResult fog = EvaluateToonFog(GetCameraPositionWS(), input.positionWS, TOON_FOG_ALL);
                if (_Mode == 1) color = ToonFogStraight(color, fog);
                if (_Mode == 2) color = ToonFogPremultiplied(color * alpha, alpha, fog);
                if (_Mode == 3) color = ToonFogAdditive(color * alpha, fog);
            }
            return float4(color, alpha);
        }

        float4 Depth(Varyings input) : SV_Target
        {
            ClipSurface(input.uv);
            return 0;
        }
        ENDHLSL

        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend [_Src] [_Dst]
            ZWrite [_DepthWrite]
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode"="DepthOnly" }
            ColorMask 0
            ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Depth
            ENDHLSL
        }
    }
}
