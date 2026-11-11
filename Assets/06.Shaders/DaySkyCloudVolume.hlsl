#ifndef UPLAYGROUND_SKY_CLOUD_VOLUME_INCLUDED
#define UPLAYGROUND_SKY_CLOUD_VOLUME_INCLUDED

float GetCloudExtinction()
{
    return _CloudDensity * lerp(0.75, 1.25, saturate(_CloudThickness)) * 8 / max(_CloudDepth, 1);
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
        float3 distortion = (shape.rgb - 0.5) * float3(1800, 900, 1800) * _CloudErosion;
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
        if (surfaceDistance < -(_CloudErosion * 1500 + _CloudEdgeWidth)) return blanket;
        float3 fine = SAMPLE_TEXTURE3D_LOD(_CloudVolume, sampler_CloudVolume,
            volumeUV * 3.1 + 0.17, max(0, lod + 1.63)).gba;
        float detail = dot(fine, float3(0.625, 0.25, 0.125));
        float3 microNoise = SAMPLE_TEXTURE3D_LOD(_CloudVolume, sampler_CloudVolume,
            volumeUV * 11.9 + 0.43, max(0, lod + 3.57)).gba;
        float micro = dot(microNoise, float3(0.625, 0.25, 0.125));
        float warpedDistance = surfaceDistance + ((shape.r - 0.5) * 700
            + (detail - 0.5) * 450 + (micro - 0.5) * 120) * _CloudErosion;
        float activation = smoothstep(formation.g - 0.08, formation.g + 0.08, coverage * 1.7 + 0.12);
        float edge = max(_CloudEdgeWidth, 1);
        float mass = smoothstep(-edge, edge, warpedDistance) * activation
            * lerp(0.4, 1, shape.r) * lerp(0.7, 1, detail) * lerp(0.35, 1, saturate(micro * 1.6));
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
    float distanceToLight = depth * 0.08;
    float stride = depth * 0.12;
    [unroll] for (int sampleIndex = 0; sampleIndex < 4; sampleIndex++)
    {
        float lod = log2(max(1, stride * 128 / max(_CloudScale, 1))) - 0.5;
        opticalDepth += SampleCloudVolume(position + lightDirection * distanceToLight, travelUV, clock, lod) * stride;
        distanceToLight += stride;
        stride *= 1.7;
    }
    return exp(-opticalDepth * GetCloudExtinction());
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
    half3 lightColor = lerp(_CloudLightColor.rgb, _NightHorizon.rgb, nightPalette);
    half3 shadowColor = lerp(_CloudShadowColor.rgb, _NightZenith.rgb, nightPalette);
    float transmission = 1;
    half3 radiance = 0;
    float flashDirection = 0.2 + 0.8 * pow(saturate(dot(view, _Lightning.xyz)), 5);

    [loop] for (int stepIndex = 0; stepIndex < steps; stepIndex++)
    {
        float3 position = origin + view * distanceAlongRay;
        // 광선의 적분 간격을 쓰면 표면 노이즈까지 평균화되어 매끈한 도형처럼 보인다.
        float lod = log2(max(0.01, distanceAlongRay * angularFootprint * 128 / max(_CloudScale, 1))) - 0.5;
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
            float skyOcclusion = SampleCloudVolume(position + float3(0, _CloudDepth * 0.35, 0), travelUV, clock, lod);
            float ambientTransmission = exp(-skyOcclusion * _CloudDensity * 3);
            float ambient = lerp(0.12, 0.3, height) + ambientTransmission * 0.2;
            // 간접 산란을 남겨 두꺼운 구름 뒷면이 검은 실루엣으로 뭉개지지 않게 한다.
            float powder = 1 - exp(-density * 3);
            float lighting = saturate(ambient + lightTransmission * (0.65 + powder * 0.35));
            half3 cloudColor = lerp(shadowColor * lerp(0.65, 1, ambientTransmission), lightColor * scattering, lighting);
            float silver = pow(alignment, 24) * lightTransmission * _CloudRimStrength;
            cloudColor += lightColor * silver;
            cloudColor += half3(0.63, 0.75, 1) * _Lightning.w * flashDirection * 0.25;
            float haze = 1 - exp(-distanceAlongRay / max(_CloudMaxDistance * 0.32, 1));
            half3 atmosphere = lerp(_HorizonColor.rgb, _NightHorizon.rgb, nightPalette);
            cloudColor = lerp(cloudColor, atmosphere, haze * 0.8);
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
