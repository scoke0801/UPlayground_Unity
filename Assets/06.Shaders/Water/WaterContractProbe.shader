Shader "Hidden/UPlayGround/WaterContractProbe"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            ZTest Always ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "WaterCommon.hlsl"
            float4 _ProbeOrigin, _ProbeDirection, _ProbeWave0, _ProbeWave1;
            float _ProbeMode, _ProbeTime, _ProbeApplyAir, _ProbeReportOverflow;
            float4 Frag(Varyings input) : SV_Target
            {
                if (_ProbeMode > 0.5)
                    return float4(WaterHeightOffset(_ProbeOrigin.xz, _ProbeTime, _ProbeWave0, _ProbeWave1), 0, 0, 1);
                float air = 0;
                WaterOptics optics = WaterEvaluateMedium(_ProbeOrigin.xyz, normalize(_ProbeDirection.xyz), 0, _ProbeDirection.w, air, _ProbeApplyAir > 0.5);
                if (_ProbeReportOverflow > 0.5) return float4(optics.length, optics.overflow, 0, 1);
                return float4(optics.transmission, optics.length);
            }
            ENDHLSL
        }
    }
}
