// 지도 UI 탐험 안개. _MainTex(알파 1 = 탐험한 곳)를 지도 이미지 UV에서 변환해 읽는다.
// 미니맵의 Mask(스텐실)와 전체 지도의 RectMask2D 클리핑을 모두 지원한다.
Shader "UPlayGround/UI/MapFog"
{
    Properties
    {
        [PerRendererData] _MainTex ("Mask", 2D) = "black" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _FogColorA ("안개 어두운 색", Color) = (0.09, 0.11, 0.15, 1)
        _FogColorB ("안개 밝은 색", Color) = (0.23, 0.26, 0.32, 1)
        _FogOpacity ("안개 불투명도", Range(0, 1)) = 0.97
        _NoiseScale ("구름 무늬 밀도", Float) = 11
        _EdgeWarpCells ("경계 흔들림(칸)", Range(0, 4)) = 1.4
        _EdgeColor ("경계 빛 색", Color) = (0.55, 0.62, 0.72, 1)
        _EdgeStrength ("경계 빛 세기", Range(0, 1)) = 0.35

        [HideInInspector] _MaskUvOrigin ("Mask UV Origin", Vector) = (0,0,0,0)
        [HideInInspector] _MaskUvAxisU ("Mask UV Axis U", Vector) = (1,0,0,0)
        [HideInInspector] _MaskUvAxisV ("Mask UV Axis V", Vector) = (0,1,0,0)
        [HideInInspector] _MaskTexel ("Mask Texel", Vector) = (0.004,0.004,0,0)
        [HideInInspector] _MapAspect ("Map Aspect", Float) = 1

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
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _FogColorA;
            fixed4 _FogColorB;
            fixed4 _EdgeColor;
            float _FogOpacity;
            float _NoiseScale;
            float _EdgeWarpCells;
            float _EdgeStrength;
            float4 _MaskUvOrigin;
            float4 _MaskUvAxisU;
            float4 _MaskUvAxisV;
            float4 _MaskTexel;
            float _MapAspect;
            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(
                    lerp(Hash(i), Hash(i + float2(1, 0)), f.x),
                    lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), f.x),
                    f.y);
            }

            float Fbm(float2 p)
            {
                return ValueNoise(p) * 0.57 + ValueNoise(p * 2.03) * 0.29 + ValueNoise(p * 4.11) * 0.14;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 무늬는 지도 UV에 고정해 줌·패닝 때 지도와 함께 움직인다.
                float2 noiseUv = i.uv * float2(_MapAspect, 1.0) * _NoiseScale;
                float cloud = Fbm(noiseUv);

                float2 maskUv = _MaskUvOrigin.xy
                    + i.uv.x * _MaskUvAxisU.xy
                    + i.uv.y * _MaskUvAxisV.xy;
                float2 warp = float2(Fbm(noiseUv * 3.1 + 19.7), Fbm(noiseUv * 3.1 + 47.3)) - 0.5;
                maskUv += warp * 2.0 * _EdgeWarpCells * _MaskTexel.xy;

                float explored = smoothstep(0.15, 0.85, tex2D(_MainTex, maskUv).a);
                float edge = saturate(1.0 - abs(explored - 0.35) * 2.6);

                fixed3 rgb = lerp(_FogColorA.rgb, _FogColorB.rgb, smoothstep(0.25, 0.8, cloud));
                rgb = lerp(rgb, _EdgeColor.rgb, edge * _EdgeStrength);

                fixed4 color = fixed4(rgb, (1.0 - explored) * _FogOpacity) * i.color;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
