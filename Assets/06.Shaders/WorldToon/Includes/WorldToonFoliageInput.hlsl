// WorldToon Foliage 재질 입력. 모든 패스가 같은 UnityPerMaterial 레이아웃을 쓴다.
// 환경 반응 계약 필드는 기존 식생 파생 셰이더와 같은 이름·형식이다. VegetationReaction.hlsl과
// ReactiveEnvironmentSurface(C#)가 이 이름으로 상태를 읽고 쓴다.
#ifndef WORLDTOON_FOLIAGE_INPUT_INCLUDED
#define WORLDTOON_FOLIAGE_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"

CBUFFER_START(UnityPerMaterial)
    float _EnvironmentReactionSupported;
    float _EnvironmentReactionBufferSupported, _EnvironmentReactionIndex;
    float4 _EnvState0, _EnvState1;
    float _FuelCoverage, _AllowFuelCoverage;
    float4 _WetTint, _CharTint, _FireColor;
    float4 _EnvironmentCellBounds;
    float _UseEnvironmentCells;

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
    half4 _GradientTop;
    half4 _GradientBottom;
    float _HeightScale;   // 정규화 높이 h = saturate(positionOS.y * _HeightScale + _HeightOffset)
    float _HeightOffset;  // 높이 바람 가중치와 높이 그라데이션이 함께 쓴다
    UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END

#include "Assets/06.Shaders/Environment/VegetationReaction.hlsl"

#endif
