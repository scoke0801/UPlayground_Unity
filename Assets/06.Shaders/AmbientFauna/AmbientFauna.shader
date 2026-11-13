Shader "UPlayGround/Ambient Fauna"
{
    Properties
    {
        _BaseMap("날개 텍스처", 2D) = "white" {}
        _Tint("날개 색", Color) = (1,0.65,0.25,1)
        _BodyColor("몸통 색", Color) = (0.07,0.045,0.025,1)
        _Cutoff("알파 경계", Range(0,1)) = 0.35
        _Emission("발광", Range(0,5)) = 0
        _GlowFrequency("발광 맥동", Range(0.1,3)) = 0.7
        [Enum(Butterfly,0,Gnat,1,Firefly,2,AirMote,3)] _Appearance("외형", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        Cull Off
        ZWrite On
        AlphaToMask On
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex FaunaVertex
            #pragma fragment FaunaFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #include "AmbientFaunaRender.hlsl"
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ColorMask R
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex FaunaVertex
            #pragma fragment FaunaDepth
            #pragma multi_compile_instancing
            #include "AmbientFaunaRender.hlsl"
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormalsOnly" }
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex FaunaVertex
            #pragma fragment FaunaDepthNormals
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #include "AmbientFaunaRender.hlsl"
            ENDHLSL
        }
    }
    Fallback Off
}
