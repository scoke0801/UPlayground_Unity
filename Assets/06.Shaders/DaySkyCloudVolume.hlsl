#ifndef UPLAYGROUND_SKY_CLOUD_VOLUME_INCLUDED
#define UPLAYGROUND_SKY_CLOUD_VOLUME_INCLUDED

float GetCloudExtinction()
{
    return _CloudDensity * lerp(0.75, 1.25, saturate(_CloudThickness))
        * lerp(1, _RainCloudDensityMultiplier, _RainCloudIntensity) * 8 / max(_CloudDepth, 1);
}

float2 IntersectCloudSphere(float3 origin, float3 direction, float radius)
{
    float projection = dot(origin, direction);
    float discriminant = projection * projection + radius * radius - dot(origin, origin);
    if (discriminant < 0) return float2(-1, -1);
    float root = sqrt(discriminant);
    return float2(-projection - root, -projection + root);
}

float SampleCloudVolumeDetailed(float3 position, float2 travelUV, float clock, float lod, out float surfaceDistance)
{
    surfaceDistance = -2000;
    float radius = max(_PlanetRadius, 1);
    float altitude = length(position + float3(0, radius, 0)) - radius - _CloudHeight;
    float height = altitude / max(_CloudDepth, 1);
    if (height <= 0 || height >= 1) return 0;

    float2 uv = position.xz / max(_CloudScale, 1) + travelUV;
    float2 weatherUV = uv * 0.38;
    float2 weatherShape = SAMPLE_TEXTURE2D_LOD(_CloudMap, sampler_CloudMap, weatherUV, 0).rg;
    float weather = weatherShape.r;
    if (_CloudSilhouetteStrength > 0)
    {
        float mask = SAMPLE_TEXTURE2D_LOD(_CloudSilhouetteMask, sampler_CloudSilhouetteMask,
            uv * _CloudSilhouetteScale, 0).r;
        weather = lerp(weather, mask, _CloudSilhouetteStrength);
    }
    float2 warped = uv + (weatherShape - 0.5) * 0.4;
    float3 volumeUV = float3(warped.x * 0.93 + warped.y * 0.27, height * 0.65,
        warped.y * 0.93 - warped.x * 0.27);
    volumeUV.y += clock * _CloudEvolution * 0.06;
    float4 shape = SAMPLE_TEXTURE3D_LOD(_CloudVolume, sampler_CloudVolume, volumeUV, max(0, lod));
    float coverage = saturate(_CloudCoverage);
    if (_CloudFormationEnabled > 0.5)
    {
        float formationScale = max(_CloudFormationScale, 1);
        float motionScale = _CloudMotionScale > 0 ? _CloudMotionScale : max(_CloudScale, 1);
        float2 formationUV = (position.xz + travelUV * motionScale) / formationScale;
        // 응결 고도에 가까운 밑면은 고정하고 윗면에서만 부풀어 오르는 변형을 허용한다.
        float baseStability = smoothstep(0.03, 0.2, height);
        float3 distortion = (shape.rgb - 0.5) * float3(900, 280 * baseStability, 900) * _CloudErosion;
        formationUV += distortion.xz / formationScale;
        float formationHeight = (altitude + distortion.y) / max(_CloudDepth, 1);
        float formationLod = max(0, lod + 1 + log2(max(_CloudScale, 1) / formationScale));
        float4 formation = SAMPLE_TEXTURE3D_LOD(_CloudFormation, sampler_CloudFormation,
            float3(formationUV.x, saturate(formationHeight), formationUV.y), formationLod);
        // 거리 부호와 기준 크기는 오프라인 밀도장과 공유하는 인코딩 계약이다.
        float distanceScale = min(formationScale / 24000, _CloudDepth / 4200);
        surfaceDistance = (formation.r * 2 - 1) * 2000 * distanceScale;
        float blanket = saturate((shape.r - 0.35) * 2) * smoothstep(0, 0.08, height)
            * (1 - smoothstep(0.15, 0.4, height)) * smoothstep(0.65, 1, coverage);
        blanket *= lerp(1, 1.5, _RainCloudIntensity);
        if (surfaceDistance < -(_CloudErosion * 1500 + _CloudEdgeWidth)) return blanket;
        float3 fine = SAMPLE_TEXTURE3D_LOD(_CloudVolume, sampler_CloudVolume,
            volumeUV * 3.1 + 0.17, max(0, lod + 1.63)).gba;
        float detail = dot(fine, float3(0.625, 0.25, 0.125));
        float3 microNoise = SAMPLE_TEXTURE3D_LOD(_CloudVolume, sampler_CloudVolume,
            volumeUV * 11.9 + 0.43, max(0, lod + 3.57)).gba;
        float micro = dot(microNoise, float3(0.625, 0.25, 0.125));
        float warpedDistance = surfaceDistance + ((shape.r - 0.5) * 700
            + (detail - 0.5) * 450 + (micro - 0.5) * 120) * _CloudErosion * baseStability;
        float activation = smoothstep(formation.g - 0.08, formation.g + 0.08, coverage * 1.7 + 0.12);
        float edge = max(_CloudEdgeWidth, exp2(max(0, lod)) * _CloudScale / 128);
        float mass = smoothstep(-edge, edge, warpedDistance) * activation
            * lerp(0.7, 1, shape.r) * lerp(0.85, 1, detail);
        // 폭풍 때만 낮은 층운을 보태 적운 사이의 여백을 점진적으로 메운다.
        if (activation <= 0) surfaceDistance = -2000;
        return max(mass, blanket) * smoothstep(0, 0.04, coverage);
    }
    float billows = dot(shape.gba, float3(0.625, 0.25, 0.125));
    float field = saturate((shape.r + billows) / (1 + billows));
    float threshold = lerp(0.88, 0.38, coverage) - (weather - 0.5) * 0.2;
    float density = saturate((field - threshold) / max(0.12, 1 - threshold));
    if (density <= 0) return 0;
    // 윗면은 부풀고 바닥은 완만하게 모이도록 높이별 질량을 제한한다.
    float bottom = smoothstep(0, 0.12, height);
    float top = 1 - smoothstep(0.45 + shape.g * 0.25, 1, height);
    float3 fine = SAMPLE_TEXTURE3D_LOD(_CloudVolume, sampler_CloudVolume, volumeUV * 3.1 + 0.17, max(0, lod + 1.63)).gba;
    float detail = dot(fine, float3(0.625, 0.25, 0.125));
    float erosion = (1 - detail) * _CloudErosion * 0.5;
    density = saturate((density - erosion) / max(0.1, 1 - erosion)) * bottom * top;
    return density * smoothstep(0, 0.04, coverage);
}

float SampleCloudVolume(float3 position, float2 travelUV, float clock, float lod)
{
    float surfaceDistance;
    return SampleCloudVolumeDetailed(position, travelUV, clock, lod, surfaceDistance);
}

float EvaluateCloudLight(float3 position, float3 lightDirection, float2 travelUV, float clock)
{
    float depth = max(_CloudDepth, 1);
    float opticalDepth = 0;
    float distanceToLight = 0;
    float stride = depth * 0.06;
    [unroll] for (int sampleIndex = 0; sampleIndex < 4; sampleIndex++)
    {
        float lod = log2(max(1, stride * 128 / max(_CloudScale, 1))) - 0.5;
        // 각 적분 구간의 중앙을 읽어 표면 바로 뒤의 질량이 빛 계산에서 빠지지 않게 한다.
        opticalDepth += SampleCloudVolume(position + lightDirection * (distanceToLight + stride * 0.5), travelUV, clock, lod) * stride;
        distanceToLight += stride;
        stride *= 2;
    }
    return exp(-opticalDepth * GetCloudExtinction() * _CloudLightAbsorption);
}

half3 RenderCloudVolume(float3 view, float3 sun, half3 background, float nightPalette,
    float2 travelUV, float clock, float2 pixel)
{
    float angularFootprint = max(length(fwidth(view)), 0.00001);
    if (_CloudCoverage <= 0 || view.y < -0.02) return background;
    float radius = max(_PlanetRadius, 1);
    float inner = radius + _CloudHeight;
    float outer = inner + max(_CloudDepth, 1);
    float3 origin = _WorldSpaceCameraPos;
    float3 sphereOrigin = origin + float3(0, radius, 0);
    float2 outerHit = IntersectCloudSphere(sphereOrigin, view, outer);
    if (outerHit.y <= 0) return background;
    float start = max(0, outerHit.x);
    float end = min(outerHit.y, max(_CloudMaxDistance, 1));
    float2 innerHit = IntersectCloudSphere(sphereOrigin, view, inner);
    if (length(sphereOrigin) < inner) start = max(start, innerHit.y);
    else if (innerHit.x > 0) end = min(end, innerHit.x);
    if (end <= start) return background;

    int steps = (int)clamp(_CloudRaySteps, 16, 128);
    float stride = (end - start) / steps;
    // 프레임 번호를 섞지 않아 시간 재투영 없이도 입자가 반짝이지 않는다.
    float jitter = frac(52.9829189 * frac(dot(pixel, float2(0.06711056, 0.00583715))));
    float distanceAlongRay = start + stride * (0.25 + jitter * 0.5);
    float3 lightDirection = sun.y < -0.04 ? -sun : sun;
    float alignment = saturate(dot(view, lightDirection));
    float scattering = 0.65 + pow(alignment, 12) * _CloudForwardScattering;
    float rainDaylight = lerp(0.06, 1, smoothstep(-0.12, 0.12, sun.y));
    half3 lightColor = lerp(lerp(_CloudLightColor.rgb, _RainCloudLightColor.rgb * rainDaylight, _RainCloudIntensity), _NightHorizon.rgb, nightPalette);
    half3 shadowColor = lerp(lerp(_CloudShadowColor.rgb, _RainCloudShadowColor.rgb * rainDaylight, _RainCloudIntensity), _NightZenith.rgb, nightPalette);
    float transmission = 1;
    half3 radiance = 0;
    float flashDirection = 0.2 + 0.8 * pow(saturate(dot(view, _Lightning.xyz)), 5);

    [loop] for (int stepIndex = 0; stepIndex < steps; stepIndex++)
    {
        float3 position = origin + view * distanceAlongRay;
        // 픽셀뿐 아니라 적분 간격보다 작은 요철도 거르면 원경의 층무늬와 입자가 줄어든다.
        float footprint = max(distanceAlongRay * angularFootprint, stride * 0.35);
        float lod = log2(max(0.01, footprint * 128 / max(_CloudScale, 1))) - 0.5;
        float surfaceDistance;
        float density = SampleCloudVolumeDetailed(position, travelUV, clock, lod, surfaceDistance);
        float sampleLength = stride;
        if (_CloudFormationEnabled > 0.5)
        {
            // 빈 공간은 건너뛰고 구름 표면에 표본을 모아 얇은 층무늬를 막는다.
            float safeDistance = max(0, abs(surfaceDistance) - _CloudErosion * 1500 - _CloudEdgeWidth);
            sampleLength = clamp(safeDistance * 0.65, max(35, _CloudEdgeWidth * 0.5), stride * 4);
            sampleLength = max(sampleLength, (end - distanceAlongRay) / (steps - stepIndex));
            sampleLength = min(sampleLength, end - distanceAlongRay);
        }
        if (density > 0.001)
        {
            float lightTransmission = EvaluateCloudLight(position, lightDirection, travelUV, clock);
            float height = saturate((length(position + float3(0, radius, 0)) - inner) / max(_CloudDepth, 1));
            float skyOcclusion = SampleCloudVolume(position + float3(0, _CloudDepth * 0.12, 0), travelUV, clock, lod)
                + SampleCloudVolume(position + float3(0, _CloudDepth * 0.4, 0), travelUV, clock, lod + 1);
            float ambientTransmission = exp(-skyOcclusion * _CloudDensity * 2);
            float ambient = _CloudAmbientStrength * lerp(0.55, 1, ambientTransmission) * lerp(0.85, 1, height);
            // 직접광과 간접광을 더해 중심의 차폐를 보존하고 밝은 윤곽만 빛을 받게 한다.
            // 감쇠가 약한 산란 차수를 더해 두꺼운 내부에도 부드러운 빛이 돌아오게 한다.
            float multipleScattering = _CloudMultipleScattering * (1 - lightTransmission)
                * (0.6 * pow(lightTransmission, 0.25) + 0.4 * pow(lightTransmission, 0.0625));
            half3 cloudColor = shadowColor * ambient + lightColor * (lightTransmission * scattering + multipleScattering
                + _RainCloudIntensity * _CloudAmbientStrength * 0.35);
            float silver = pow(alignment, 24) * lightTransmission * _CloudRimStrength;
            cloudColor += lightColor * silver * (1 - _RainCloudIntensity * 0.7);
            cloudColor += half3(0.63, 0.75, 1) * _Lightning.w * flashDirection * 0.25;
            float haze = 1 - exp(-distanceAlongRay / max(_CloudMaxDistance * 0.32, 1));
            half3 atmosphere = lerp(_HorizonColor.rgb, _NightHorizon.rgb, nightPalette);
            atmosphere = lerp(atmosphere, _OvercastSkyColor.rgb * rainDaylight, _RainCloudIntensity);
            cloudColor = lerp(cloudColor, atmosphere, haze * 0.4);
            float extinction = density * sampleLength * GetCloudExtinction();
            float sampleOpacity = 1 - exp(-extinction);
            radiance += transmission * sampleOpacity * cloudColor;
            transmission *= 1 - sampleOpacity;
            if (transmission < 0.015) break;
        }
        distanceAlongRay += sampleLength;
        if (distanceAlongRay >= end) break;
    }
    float horizonFade = smoothstep(-0.01, 0.025, view.y);
    half3 color = radiance + background * transmission;
    color += half3(0.63, 0.75, 1) * _Lightning.w * flashDirection * 0.04;
    return lerp(background, color, horizonFade);
}

#endif
