// WorldToon Foliage 재질 입력. 모든 패스가 같은 UnityPerMaterial 레이아웃을 쓴다.
#ifndef WORLDTOON_FOLIAGE_INPUT_INCLUDED
#define WORLDTOON_FOLIAGE_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    float4 _BaseMap_TexelSize;
    half4 _BaseColor;
    half4 _LitTint;
    half4 _ShadeTint;
    half _BandThreshold;
    half _BandSoftness;
    half _IndirectStrength;
    half _AdditionalLightStrength;
    half _Cutoff;
    half _Surface;
    half _UpNormalBlend;
    half _Translucency;
    float _WindAmplitude;
    float _WindFrequency;
    float _WindIdle;
    UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END

#endif
