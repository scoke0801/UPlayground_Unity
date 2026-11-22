Shader "Hidden/UPlayGround/Toon Fog Math Probe"
{
    SubShader
    {
        Pass
        {
            ZWrite Off ZTest Always Cull Off
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "FogEvaluation.hlsl"
            float4 _ProbeA, _ProbeB, _ProbeParameters;
            float4x4 _ProbeInverse;

            float4 Vert(uint id : SV_VertexID) : SV_POSITION { return GetFullScreenTriangleVertexPosition(id); }

            float4 Frag() : SV_Target
            {
                if (_ProbeParameters.w < 0.5)
                {
                    float integral = ToonFogHeightIntegral(_ProbeA.x, _ProbeA.y, _ProbeA.z, _ProbeB.x, _ProbeB.y);
                    return float4(integral, exp(-integral * _ProbeB.z), 0, 1);
                }
                if (_ProbeParameters.w < 2.5)
                {
                    float3 direction = normalize(_ProbeB.xyz - _ProbeA.xyz);
                    float3 localOrigin = mul(_ProbeInverse, float4(_ProbeA.xyz, 1)).xyz;
                    float3 localDirection = mul((float3x3)_ProbeInverse, direction);
                    float start = _ProbeParameters.x, end = distance(_ProbeA.xyz, _ProbeB.xyz);
                    bool hit;
                    if (_ProbeParameters.w < 1.5) hit = ToonFogClipBox(localOrigin, localDirection, start, end);
                    else hit = ToonFogClipSphere(localOrigin, localDirection, start, end);
                    return float4(hit ? end - start : 0, start, end, 1);
                }
                ToonFogResult fog = EvaluateToonFog(_ProbeA.xyz, _ProbeB.xyz, TOON_FOG_ALL);
                return float4(fog.scattering, fog.transmittance);
            }
            ENDHLSL
        }
    }
}
