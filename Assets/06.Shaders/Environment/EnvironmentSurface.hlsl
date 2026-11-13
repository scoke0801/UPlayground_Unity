#ifndef UPLAYGROUND_ENVIRONMENT_SURFACE_INCLUDED
#define UPLAYGROUND_ENVIRONMENT_SURFACE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MotionVectorsCommon.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"
TEXTURE2D(_ReactionMask); SAMPLER(sampler_ReactionMask);

CBUFFER_START(UnityPerMaterial)
float4 _BaseMap_ST;
half4 _BaseColor, _WetTint, _CharTint, _CrackColor, _FireColor, _ShadeColor, _OutlineColor;
float4 _EnvState0, _EnvState1;
float _Cutoff, _AlphaClip, _Cull, _UseReactionMask, _SurfaceRole;
float _CharSoftness, _CrackSoftness, _CrackOpacity, _NoiseScale, _EdgeWidth, _GlowScale;
float _ShadowThreshold, _BandSoftness, _WetHighlight, _BumpScale, _AmbientFloor;
float _OutlineWidth, _WindAmplitude, _WindFrequency, _WindSpeed, _WindRootHeight;
float _AllowFuelCoverage, _FuelCoverage;
CBUFFER_END
float4 _LW_Wind, _LW_PreviousWind;
float3 _LightDirection, _LightPosition;

struct EnvAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    float2 uv : TEXCOORD0;
    float2 lightmapUV : TEXCOORD1;
    float2 dynamicLightmapUV : TEXCOORD2;
    half4 color : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};
struct EnvVaryings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    float3 positionOS : TEXCOORD1;
    half3 normalWS : TEXCOORD2;
    half4 tangentWS : TEXCOORD3;
    float2 uv : TEXCOORD4;
    half fog : TEXCOORD5;
    DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 6);
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

// 로컬 좌표 패턴은 이동·회전 시에도 흔적 위치를 유지한다.
float EnvHash(float3 p) { return frac(sin(dot(p, float3(127.1,311.7,74.7))) * 43758.5453); }
float EnvNoise(float3 p)
{
    float3 cell = floor(p), f = frac(p); f = f * f * (3 - 2 * f);
    return lerp(lerp(lerp(EnvHash(cell), EnvHash(cell+float3(1,0,0)), f.x),
                     lerp(EnvHash(cell+float3(0,1,0)), EnvHash(cell+float3(1,1,0)), f.x), f.y),
                lerp(lerp(EnvHash(cell+float3(0,0,1)), EnvHash(cell+float3(1,0,1)), f.x),
                     lerp(EnvHash(cell+float3(0,1,1)), EnvHash(cell+1), f.x), f.y), f.z);
}
float EnvCoverage(float progress, float pattern, float softness)
{
    if (progress <= 0) return 0;
    if (progress >= 1) return 1;
    float width = max(softness, 0.001);
    return smoothstep(pattern-width, pattern+width, progress);
}
float3 EnvDeform(float3 positionOS, float time, float3 wind)
{
    float weight = saturate(positionOS.y - _WindRootHeight);
    float phase = dot(TransformObjectToWorld(positionOS).xz, float2(0.31,0.19)) * _WindFrequency + time * _WindSpeed;
    float bend = sin(phase) + 0.25 * sin(phase * 1.73);
    float3 windOS = TransformWorldToObjectDir(wind, false);
    return positionOS + windOS * (_WindAmplitude * weight * bend);
}
EnvVaryings EnvVertex(EnvAttributes input)
{
    EnvVaryings output = (EnvVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    output.positionOS = input.positionOS.xyz;
    float3 positionOS = EnvDeform(input.positionOS.xyz, _Time.y, _LW_Wind.xyz);
    VertexPositionInputs position = GetVertexPositionInputs(positionOS);
    output.positionWS = position.positionWS;
    output.positionCS = position.positionCS;
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    output.tangentWS = half4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w * GetOddNegativeScale());
    output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
    output.fog = ComputeFogFactor(position.positionCS.z);
    OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, output.lightmapUV);
    OUTPUT_SH(output.normalWS, output.vertexSH);
    return output;
}
half4 EnvBase(EnvVaryings input) { return SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor; }
void EnvClip(EnvVaryings input)
{
    if (_AlphaClip > 0.5) clip(EnvBase(input).a - _Cutoff);
    // 실루엣 소거는 저작자가 허용한 잎에만 적용한다. 구조물 Collider는 셰이더가 바꾸지 않는다.
    if (_AllowFuelCoverage > 0.5 && _SurfaceRole > 0.5 && _SurfaceRole < 1.5)
    {
        if (_FuelCoverage <= 0) clip(-1);
        if (_FuelCoverage < 1) clip(_FuelCoverage - EnvNoise(input.positionOS * _NoiseScale));
    }
    #if defined(LOD_FADE_CROSSFADE)
        LODFadeCrossFade(input.positionCS);
    #endif
}
half3 EnvNormal(EnvVaryings input, bool frontFace)
{
    half3 normal = normalize(input.normalWS);
    #if defined(_NORMALMAP)
        half3 tangent = normalize(input.tangentWS.xyz);
        half3 bitangent = cross(normal, tangent) * input.tangentWS.w;
        half3 normalTS = SampleNormal(input.uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);
        normal = normalize(TransformTangentToWorld(normalTS, half3x3(tangent,bitangent,normal)));
    #endif
    return frontFace ? normal : -normal;
}
half3 EnvLight(half3 surface, half3 normal, Light light)
{
    half ndl = dot(normal, light.direction) * 0.5h + 0.5h;
    half band = smoothstep(_ShadowThreshold - _BandSoftness, _ShadowThreshold + _BandSoftness, ndl);
    half3 ramp = lerp(_ShadeColor.rgb, half3(1,1,1), band * light.shadowAttenuation);
    return surface * ramp * light.color * light.distanceAttenuation;
}
half4 EnvForward(EnvVaryings input, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    EnvClip(input);
    half3 normal = EnvNormal(input, IS_FRONT_VFACE(face, true, false));
    half3 view = GetWorldSpaceNormalizeViewDir(input.positionWS);
    half4 mask = SAMPLE_TEXTURE2D(_ReactionMask, sampler_ReactionMask, input.uv);
    float noise = EnvNoise(input.positionOS * _NoiseScale);
    float reaction = lerp(1, mask.r, _UseReactionMask);
    float charPattern = lerp(noise, mask.b, _UseReactionMask);
    float crackPattern = lerp(abs(sin(input.positionOS.x * 19 + noise * 7)), mask.g, _UseReactionMask);
    float charr = EnvCoverage(_EnvState0.y * reaction, charPattern, _CharSoftness);
    float crack = EnvCoverage(_EnvState0.z * reaction, crackPattern, _CrackSoftness) * _CrackOpacity;
    half wet = saturate(_EnvState0.x * reaction);
    half3 surface = lerp(EnvBase(input).rgb, _CharTint.rgb, charr);
    surface = lerp(surface, _CrackColor.rgb, crack);
    surface *= lerp(half3(1,1,1), _WetTint.rgb, wet);
    Light sun = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
    half3 gi = SAMPLE_GI(input.lightmapUV, input.vertexSH, normal);
    gi = max(gi, _AmbientFloor.xxx);
    half3 color = surface * gi + EnvLight(surface, normal, sun);
    #if defined(_ADDITIONAL_LIGHTS)
        InputData inputData = (InputData)0;
        inputData.positionWS = input.positionWS;
        inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
        #if USE_CLUSTER_LIGHT_LOOP
            UNITY_LOOP for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
            {
                Light light = GetAdditionalLight(lightIndex, input.positionWS, half4(1,1,1,1));
                light.color *= light.shadowAttenuation;
                color += EnvLight(surface, normal, light);
            }
        #endif
        uint lights = GetAdditionalLightsCount();
        LIGHT_LOOP_BEGIN(lights)
            Light light = GetAdditionalLight(lightIndex, input.positionWS, half4(1,1,1,1));
            light.color *= light.shadowAttenuation;
            color += EnvLight(surface, normal, light);
        LIGHT_LOOP_END
    #endif
    half highlight = smoothstep(0.85h, 0.95h, dot(normal, SafeNormalize(view + sun.direction)));
    color += highlight * wet * _WetHighlight * sun.color * sun.shadowAttenuation;
    float edge = 1 - smoothstep(0, max(_EdgeWidth, 0.001), abs(charPattern - _EnvState0.y));
    color += edge * saturate(_EnvState0.w) * _GlowScale * _FireColor.rgb;
    return half4(MixFog(color, input.fog), 1);
}
EnvVaryings EnvShadowVertex(EnvAttributes input)
{
    EnvVaryings output = EnvVertex(input);
    float3 lightDirection = _LightDirection;
    #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
        lightDirection = normalize(_LightPosition - output.positionWS);
    #endif
    output.positionCS = TransformWorldToHClip(ApplyShadowBias(output.positionWS, normalize(output.normalWS), lightDirection));
    output.positionCS = ApplyShadowClamping(output.positionCS);
    return output;
}
half4 EnvDepth(EnvVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    EnvClip(input); return 0;
}
half4 EnvDepthNormals(EnvVaryings input, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    EnvClip(input);
    half3 normal = EnvNormal(input, IS_FRONT_VFACE(face, true, false));
    #if defined(_GBUFFER_NORMALS_OCT)
        float2 oct = PackNormalOctQuadEncode(normal);
        return half4(PackFloat2To888(saturate(oct * 0.5 + 0.5)), 0);
    #else
        return half4(normal, 0);
    #endif
}
EnvVaryings EnvOutlineVertex(EnvAttributes input)
{
    EnvVaryings output = EnvVertex(input);
    output.positionWS += normalize(output.normalWS) * _OutlineWidth;
    output.positionCS = TransformWorldToHClip(output.positionWS);
    output.fog = ComputeFogFactor(output.positionCS.z);
    return output;
}
half4 EnvOutline(EnvVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    clip(_OutlineWidth - 0.00001);
    EnvClip(input);
    return half4(MixFog(_OutlineColor.rgb, input.fog), 1);
}
EnvVaryings EnvMetaVertex(EnvAttributes input)
{
    EnvVaryings output = EnvVertex(input);
    output.positionCS = MetaVertexPosition(input.positionOS, input.lightmapUV, input.dynamicLightmapUV, unity_LightmapST, unity_DynamicLightmapST);
    return output;
}
half4 EnvMeta(EnvVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    EnvClip(input);
    MetaInput surface = (MetaInput)0;
    surface.Albedo = EnvBase(input).rgb;
    surface.Emission = 0;
    return MetaFragment(surface);
}
struct EnvMotionVaryings
{
    EnvVaryings surface;
    float4 currentCS : TEXCOORD7;
    float4 previousCS : TEXCOORD8;
};
EnvMotionVaryings EnvMotionVertex(EnvAttributes input)
{
    EnvMotionVaryings output;
    output.surface = EnvVertex(input);
    output.currentCS = mul(_NonJitteredViewProjMatrix, float4(output.surface.positionWS,1));
    float3 previousOS = EnvDeform(input.positionOS.xyz, _Time.y - unity_DeltaTime.x, _LW_PreviousWind.xyz);
    output.previousCS = mul(_PrevViewProjMatrix, mul(UNITY_PREV_MATRIX_M, float4(previousOS,1)));
    return output;
}
half4 EnvMotion(EnvMotionVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input.surface);
    EnvClip(input.surface);
    return half4(CalcNdcMotionVectorFromCsPositions(input.currentCS, input.previousCS),0,0);
}
#endif
