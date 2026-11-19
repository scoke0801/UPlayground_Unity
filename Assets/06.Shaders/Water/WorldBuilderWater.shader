Shader "UPlayGround/WorldBuilder/Water"
{
    Properties
    {
        _ShallowColor("얕은 물", Color) = (.13,.45,.39,.55)
        _DeepColor("깊은 물", Color) = (.025,.15,.19,.85)
        _NormalMap("물결 노멀", 2D) = "bump" {}
        _NormalScale("물결 세기", Float) = .25
        _NormalTiling("물결 반복", Float) = .15
        _ScrollSpeed("흐름 속도", Float) = .1
        _WaveDrift("잔물결 이동", Vector) = (.1,.05,0,0)
        _DepthFade("깊이 전이", Float) = 3
        _FoamWidth("물가 포말 폭", Float) = .6
        _FoamStrength("포말 세기", Float) = .45
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Name "Water"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
            TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);
            CBUFFER_START(UnityPerMaterial)
            float4 _ShallowColor, _DeepColor, _WaveDrift;
            float _NormalScale, _NormalTiling, _ScrollSpeed, _DepthFade, _FoamWidth, _FoamStrength;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; float2 flow:TEXCOORD1; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 world:TEXCOORD0; float2 uv:TEXCOORD1; float2 flow:TEXCOORD2; };
            Varyings Vert(Attributes v)
            {
                Varyings o; o.world=TransformObjectToWorld(v.positionOS.xyz); o.positionCS=TransformWorldToHClip(o.world); o.uv=v.uv; o.flow=v.flow; return o;
            }
            half4 Frag(Varyings v):SV_Target
            {
                float2 screen=v.positionCS.xy/_ScaledScreenParams.xy;
                float scene=LinearEyeDepth(SampleSceneDepth(screen),_ZBufferParams);
                float water=-TransformWorldToView(v.world).z;
                float depth=max(0,scene-water);
                float2 scroll=(v.flow+_WaveDrift.xy)*_Time.y*_ScrollSpeed;
                float3 n0=UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap,sampler_NormalMap,v.world.xz*_NormalTiling-scroll),1);
                float3 n1=UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap,sampler_NormalMap,v.world.zx*_NormalTiling*.71+scroll.yx*.63),1);
                float3 normal=normalize(float3((n0.x+n1.y)*_NormalScale,1,(n0.y+n1.x)*_NormalScale));
                Light light=GetMainLight(); float lambert=saturate(dot(normal,light.direction))*.35+.65;
                half4 color=lerp(_ShallowColor,_DeepColor,saturate(depth/max(.001,_DepthFade)));
                float ripple=.5+.5*sin(v.uv.y*18-_Time.y*2+n0.x*3);
                float foam=(1-smoothstep(0,max(.001,_FoamWidth),depth))*_FoamStrength*(.65+.35*ripple);
                color.rgb=lerp(color.rgb*lambert,half3(.85,.94,.91),foam);
                color.a*=saturate(depth/.12); color.a=max(color.a,foam*.8); return color;
            }
            ENDHLSL
        }
    }
}
