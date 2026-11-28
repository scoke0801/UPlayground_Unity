#ifndef UPG_SETTLED_SNOW_FORWARD_INCLUDED
#define UPG_SETTLED_SNOW_FORWARD_INCLUDED

#define REQUIRES_WORLD_SPACE_POS_INTERPOLATOR
#include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"

TEXTURE2D(_SnowAlbedo);
SAMPLER(sampler_SnowAlbedo);

float SnowHash(float2 cell)
{
    float3 p = frac(cell.xyx * 0.1031);
    p += dot(p, p.yzx + 33.33);
    return frac((p.x + p.y) * p.z);
}

float SnowNoise(float2 position)
{
    float2 cell = floor(position);
    float2 uv = frac(position);
    uv = uv * uv * (3.0 - 2.0 * uv);
    return lerp(lerp(SnowHash(cell), SnowHash(cell + float2(1, 0)), uv.x),
                lerp(SnowHash(cell + float2(0, 1)), SnowHash(cell + 1), uv.x), uv.y);
}

void ApplySettledSnow(float3 positionWS, half3 geometricNormal, inout SurfaceData surface, inout InputData lighting)
{
    float2 ground = positionWS.xz;
    float drift = SnowNoise(ground * 0.43) * 0.68 + SnowNoise(ground * 1.38) * 0.24;
    float grains = SnowNoise(ground * 6.5) * 0.08;
    float exposure = drift + grains + geometricNormal.y * 0.13;
    float edge = max(0.12, fwidth(drift + grains));
    half settled = smoothstep(0.51 - edge, 0.51 + edge, exposure)
                 * smoothstep(0.16, 0.53, geometricNormal.y);
    half3 snow = SAMPLE_TEXTURE2D(_SnowAlbedo, sampler_SnowAlbedo, ground * 0.37).rgb;
    surface.albedo = lerp(surface.albedo, snow * lerp(0.76, 1.0, saturate(drift * 2.5)), settled);
    surface.metallic = lerp(surface.metallic, 0.0, settled);
    surface.specular = lerp(surface.specular, half3(0.045, 0.045, 0.045), settled);
    surface.smoothness = lerp(surface.smoothness, 0.19, settled);
    surface.occlusion = lerp(surface.occlusion, 1.0, settled * 0.7);
    lighting.normalWS = normalize(lerp(lighting.normalWS, geometricNormal, settled * 0.82));
}

void SettledSnowFragment(
    Varyings input
    , out half4 outColor : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out uint outRenderingLayers : SV_Target1
#endif
)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

#if defined(_PARALLAXMAP)
#if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
    half3 viewDirTS = input.viewDirTS;
#else
    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
    half3 viewDirTS = GetViewDirectionTangentSpace(input.tangentWS, input.normalWS, viewDirWS);
#endif
    ApplyPerPixelDisplacement(viewDirTS, input.uv);
#endif

    SurfaceData surfaceData;
    InitializeStandardLitSurfaceData(input.uv, surfaceData);

#ifdef LOD_FADE_CROSSFADE
    LODFadeCrossFade(input.positionCS);
#endif

    InputData inputData;
    InitializeInputData(input, surfaceData.normalTS, inputData);
    SETUP_DEBUG_TEXTURE_DATA(inputData, UNDO_TRANSFORM_TEX(input.uv, _BaseMap));

#if defined(_DBUFFER)
    ApplyDecalToSurfaceData(input.positionCS, surfaceData, inputData);
#endif

    ApplySettledSnow(input.positionWS, normalize(input.normalWS), surfaceData, inputData);
    InitializeBakedGIData(input, inputData);

    half4 color = UniversalFragmentPBR(inputData, surfaceData);
    color.rgb = MixFog(color.rgb, inputData.fogCoord);
    color.a = OutputAlpha(color.a, IsSurfaceTypeTransparent(_Surface));

    outColor = color;

#ifdef _WRITE_RENDERING_LAYERS
    outRenderingLayers = EncodeMeshRenderingLayer();
#endif
}

#endif
