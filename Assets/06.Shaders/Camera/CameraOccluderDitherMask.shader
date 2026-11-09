// 카메라 가림 페이드용 스텐실 디더 마스크.
// 화면 전체에 4x4 베이어 값(0~15)을 스텐실 상위 4비트(0xF0)에 비트 평면 4장으로 기록하고,
// 페이드 패스가 끝나면 같은 비트를 0으로 지운다.
// 가려진 오브젝트는 원본 셰이더 그대로 그려지되 스텐실 비교(ref > 베이어값)로 픽셀 단위 디더 소거된다.
Shader "Hidden/UPlayGround/CameraOccluderDitherMask"
{
    Properties
    {
        _PixelScale ("Pixel Scale", Float) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        ZTest Always
        ZWrite Off
        Cull Off
        ColorMask 0

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        float _PixelScale;

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
        };

        Varyings Vert(uint vertexID : SV_VertexID)
        {
            Varyings output;
            float2 uv = float2((vertexID << 1) & 2, vertexID & 2);
            output.positionCS = float4(uv * 2.0 - 1.0, 0.0, 1.0);
            return output;
        }

        // 4x4 베이어 행렬. 값이 낮을수록 먼저 보인다.
        uint Bayer4(float2 pixel)
        {
            static const uint kMatrix[16] =
            {
                0, 8, 2, 10,
                12, 4, 14, 6,
                3, 11, 1, 9,
                15, 7, 13, 5
            };
            uint2 p = (uint2)(pixel / max(_PixelScale, 1.0)) & 3u;
            return kMatrix[p.y * 4u + p.x];
        }

        half4 WritePlane(Varyings input, uint plane)
        {
            if (((Bayer4(input.positionCS.xy) >> plane) & 1u) == 0u)
                discard;
            return 0;
        }
        ENDHLSL

        Pass
        {
            Name "Plane0"
            Stencil { Ref 16 ReadMask 0 WriteMask 16 Comp Always Pass Replace }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            half4 Frag(Varyings input) : SV_Target { return WritePlane(input, 0u); }
            ENDHLSL
        }

        Pass
        {
            Name "Plane1"
            Stencil { Ref 32 ReadMask 0 WriteMask 32 Comp Always Pass Replace }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            half4 Frag(Varyings input) : SV_Target { return WritePlane(input, 1u); }
            ENDHLSL
        }

        Pass
        {
            Name "Plane2"
            Stencil { Ref 64 ReadMask 0 WriteMask 64 Comp Always Pass Replace }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            half4 Frag(Varyings input) : SV_Target { return WritePlane(input, 2u); }
            ENDHLSL
        }

        Pass
        {
            Name "Plane3"
            Stencil { Ref 128 ReadMask 0 WriteMask 128 Comp Always Pass Replace }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            half4 Frag(Varyings input) : SV_Target { return WritePlane(input, 3u); }
            ENDHLSL
        }

        Pass
        {
            Name "Clear"
            Stencil { Ref 0 ReadMask 0 WriteMask 240 Comp Always Pass Replace }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            half4 Frag(Varyings input) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
