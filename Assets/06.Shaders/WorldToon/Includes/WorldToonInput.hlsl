// WorldToon Mesh 계열 공통 재질 입력.
// 모든 패스가 같은 UnityPerMaterial 레이아웃을 써야 SRP Batcher 호환이 유지된다.
// _BaseMap/_BaseColor/_Cutoff 이름은 URP의 ShadowCaster·DepthOnly·DepthNormals·ObjectMotionVectors 패스가 그대로 읽는 계약이다.
#ifndef WORLDTOON_INPUT_INCLUDED
#define WORLDTOON_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    float4 _BaseMap_TexelSize;
    half4 _BaseColor;
    // 스타일 값은 WorldToonStyleProfileSO가 변환 도구를 통해 재질에 기록한다(전역 상태를 두지 않는 v1 결정).
    half4 _LitTint;
    half4 _ShadeTint;
    half _BandThreshold;
    half _BandSoftness;
    half _IndirectStrength;
    half _AdditionalLightStrength;
    half _Cutoff;
    half _Surface;
    UNITY_TEXTURE_STREAMING_DEBUG_VARS; // 렌더링 디버거(DEBUG_DISPLAY)의 밉맵 스트리밍 뷰가 요구한다.
CBUFFER_END

#endif
