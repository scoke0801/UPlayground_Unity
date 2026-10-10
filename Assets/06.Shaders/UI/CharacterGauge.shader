Shader "UPlayGround/UI/CharacterGauge"
{
    Properties
    {
        [PerRendererData] _MainTex ("Silhouette", 2D) = "white" {}
        _FlowMap ("Flow Map", 2D) = "white" {}
        _FillMask ("Fill Mask", 2D) = "white" {}
        _SpriteRect ("Sprite UV Rect", Vector) = (0,0,1,1)
        _Color ("Tint", Color) = (1,1,1,1)
        _BaseColor ("Base", Color) = (.35,.42,.37,1)
        _EnergyColor ("Energy", Color) = (.72,.96,.84,1)
        _ReadyColor ("Ready", Color) = (.95,1,.87,1)
        _Resource ("Resource", Range(0,1)) = 0
        _Trail ("Trail", Range(0,1)) = 0
        _ReadyPulse ("Ready Pulse", Float) = 0
        _Locked ("Locked", Float) = 0
        _UnscaledTime ("Unscaled Time", Float) = 0
        _EdgeWidth ("Edge Width", Float) = .02
        _NoiseStrength ("Noise", Float) = .004
        _FlowSpeed ("Flow Speed", Float) = .2
        _GlowIntensity ("Glow", Float) = .2
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="False" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; float4 mask:TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
            sampler2D _MainTex, _FlowMap, _FillMask;
            float4 _MainTex_TexelSize, _SpriteRect, _ClipRect;
            fixed4 _Color, _BaseColor, _EnergyColor, _ReadyColor;
            float _Resource, _Trail, _ReadyPulse, _Locked, _UnscaledTime;
            float _EdgeWidth, _NoiseStrength, _FlowSpeed, _GlowIntensity;
            float _UIMaskSoftnessX, _UIMaskSoftnessY;
            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                float2 pixelSize = o.vertex.w / abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));
                float4 rect = clamp(_ClipRect, -2e10, 2e10);
                o.mask = float4(v.vertex.xy * 2 - rect.xy - rect.zw,
                    .25 / (.25 * float2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize)));
                return o;
            }
            float Fill(float value, float order)
            {
                if (value <= 0) return 0;
                if (value >= 1) return 1;
                return 1 - smoothstep(value - _EdgeWidth, value + _EdgeWidth, order);
            }
            fixed4 frag(v2f i):SV_Target
            {
                float2 localUV = (i.uv - _SpriteRect.xy) / max(_SpriteRect.zw, .00001);
                float4 flow = tex2D(_FlowMap, localUV);
                float alpha = tex2D(_MainTex, i.uv).a;
                float mask = tex2D(_FillMask, localUV).a * flow.a;
                float phase = flow.g * 6.283185 + _UnscaledTime * _FlowSpeed;
                float boundaryNoise = sin(phase * 5) * _NoiseStrength;
                float fill = Fill(_Resource, flow.r + boundaryNoise) * mask;
                float trail = max(0, Fill(_Trail, flow.r) * mask - fill);
                float edge = saturate(1 - abs(flow.r - _Resource) / max(_EdgeWidth, .001));
                edge *= step(.00001, _Resource) * (1 - step(.99999, _Resource));
                float brightness = 1 + _GlowIntensity * (.08 * sin(phase) + edge + _ReadyPulse);
                fixed3 energy = lerp(_EnergyColor.rgb, _ReadyColor.rgb, _ReadyPulse);
                fixed3 rgb = lerp(_BaseColor.rgb, energy * brightness, fill);
                rgb += trail * _EnergyColor.rgb * .25;
                rgb = lerp(rgb, rgb * .5, _Locked);
                float2 delta = _MainTex_TexelSize.xy;
                float border = max(max(tex2D(_MainTex, i.uv + float2(delta.x,0)).a,
                    tex2D(_MainTex, i.uv - float2(delta.x,0)).a),
                    max(tex2D(_MainTex, i.uv + float2(0,delta.y)).a,
                    tex2D(_MainTex, i.uv - float2(0,delta.y)).a));
                rgb = lerp(fixed3(.025,.04,.03), rgb, saturate(alpha * 4));
                fixed4 color = fixed4(rgb * i.color.rgb, max(alpha, border * .8) * i.color.a);
                #ifdef UNITY_UI_CLIP_RECT
                float2 clipMask = saturate((_ClipRect.zw - _ClipRect.xy - abs(i.mask.xy)) * i.mask.zw);
                color.a *= clipMask.x * clipMask.y;
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - .001);
                #endif
                return color;
            }
            ENDCG
        }
    }
}
