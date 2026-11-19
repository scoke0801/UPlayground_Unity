// 라이트맵 베이크용 Meta 패스. 색 입력만 전달하며 툰 명암 자체는 굽지 않는다.
#ifndef WORLDTOON_META_PASS_INCLUDED
#define WORLDTOON_META_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UniversalMetaPass.hlsl"

half4 WorldToonFragmentMeta(Varyings input) : SV_Target
{
    half4 baseSample = SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
    MetaInput metaInput = (MetaInput)0;
    metaInput.Albedo = baseSample.rgb * _BaseColor.rgb;
    metaInput.Emission = half3(0, 0, 0);
    return UniversalFragmentMeta(input, metaInput);
}

#endif
