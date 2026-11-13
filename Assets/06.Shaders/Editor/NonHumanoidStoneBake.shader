Shader "Hidden/UPlayGround/NonHumanoidStoneBake"
{
    Properties
    {
        _Packed ("합성 마스크", 2D) = "white" {}
        _StoneAO ("원본 표면 명도", 2D) = "white" {}
        _Down ("하체 마스크", 2D) = "black" {}
        _Eyes ("눈", 2D) = "black" {}
        _SnowTex ("눈 표면", 2D) = "black" {}
        _WebTex ("거미줄", 2D) = "black" {}
        _RoughTex ("원본 매끄러움", 2D) = "white" {}
        _Base ("바탕", Color) = (1,1,1,1)
        _cavity_C ("틈", Color) = (0,0,0,1)
        _dirt_C ("먼지", Color) = (0,0,0,1)
        _Curvature_C ("모서리", Color) = (1,1,1,1)
        _down_leg_C ("하체", Color) = (0,0,0,1)
        _eye_C ("눈 색", Color) = (1,1,1,1)
        _Color_emisive ("표면 발광 색", Color) = (1,1,1,1)
        _cavity ("틈 강도", Float) = 1
        _dirt ("먼지 강도", Float) = 0
        _Curvature ("모서리 강도", Float) = 1
        _down_leg ("하체 강도", Float) = 1
        _eye ("눈 강도", Float) = 20
        _Snow ("눈 표면 강도", Float) = 0
        _web ("거미줄 강도", Float) = 0
        _Roug ("원본 매끄러움 배율", Float) = 1
        _Output ("출력 0 바탕 1 발광 2 매끄러움", Float) = 0
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _Packed, _StoneAO, _Down, _Eyes, _SnowTex, _WebTex, _RoughTex;
            float4 _Base, _cavity_C, _dirt_C, _Curvature_C, _down_leg_C, _eye_C, _Color_emisive;
            float _cavity, _dirt, _Curvature, _down_leg, _eye, _Snow, _web, _Roug, _Output;
            float4 frag(v2f_img i) : SV_Target
            {
                // Graph의 Lerp는 clamp하지 않는다. 눈·거미줄·적설의 HDR 합산도 유지한다.
                if (_Output < .5)
                {
                    float4 mask = tex2D(_Packed, i.uv);
                    float3 col = lerp(_Base.rgb, _cavity_C.rgb, mask.r * _cavity);
                    col = lerp(col, _dirt_C.rgb, mask.g * _dirt);
                    col = lerp(col, _Curvature_C.rgb, mask.b * _Curvature);
                    col = lerp(col, _down_leg_C.rgb, tex2D(_Down, i.uv).g * _down_leg);
                    return float4(col * tex2D(_StoneAO, i.uv).rgb, 1);
                }
                if (_Output < 1.5)
                {
                    float3 eyes = tex2D(_Eyes, i.uv).rgb * _eye * _eye_C.rgb;
                    float3 snow = tex2D(_SnowTex, i.uv).rgb * _Snow * _Color_emisive.rgb;
                    float3 web = tex2D(_WebTex, i.uv).rgb * _web * _Color_emisive.rgb * _Color_emisive.rgb;
                    return float4(eyes + snow + web, 1);
                }
                return float4((tex2D(_RoughTex, i.uv).g * _Roug).xxx, 1);
            }
            ENDHLSL
        }
    }
}
