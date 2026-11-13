#ifndef UPLAYGROUND_WATER_COMMON_INCLUDED
#define UPLAYGROUND_WATER_COMMON_INCLUDED
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "WaterWaves.hlsl"

#define WATER_MAX_INTERVALS 32
#define WATER_NO_BODY 65535u

struct WaterBody
{
    float4x4 worldToLocal;
    float4 volumeMin, volumeMax;
    float4 scattering, extinction, caustic, wave0, wave1;
    float4 shallow, deep, foam, style0, style1, style2;
    float4 ripple, rain, range, authority, flow;
};
struct WaterCell { float4 a, b, c; };
struct WaterDryVolume { float4 minimum, maximum; };
struct WaterStamp { float4 positionTime, parameters; };
StructuredBuffer<WaterBody> _WaterBodies;
StructuredBuffer<WaterCell> _WaterCells;
StructuredBuffer<WaterDryVolume> _WaterDryVolumes;
StructuredBuffer<WaterStamp> _WaterStamps;
int _WaterBodyCount, _WaterStampCount;
float _WaterClock, _WaterSceneActive;
float4 _WaterAirFog, _WaterAirFogColor;
TEXTURE2D_X_FLOAT(_WaterSurfaceInfo);

struct WaterInterval { float start, end; uint body, cell; };
struct WaterOptics
{
    float3 transmission, scattering;
    float length;
    uint endBody, overflow;
};

float WaterCross(float2 a, float2 b) { return a.x * b.y - a.y * b.x; }

bool WaterClipPlane(float position, float velocity, inout float start, inout float end)
{
    if (abs(velocity) < 0.000001) return position >= 0;
    float distance = -position / velocity;
    if (velocity > 0) start = max(start, distance);
    else end = min(end, distance);
    return end > start;
}

bool WaterClipBox(float3 origin, float3 direction, float3 minimum, float3 maximum, inout float start, inout float end)
{
    [unroll] for (int axis = 0; axis < 3; axis++)
    {
        if (!WaterClipPlane(origin[axis] - minimum[axis], direction[axis], start, end)
            || !WaterClipPlane(maximum[axis] - origin[axis], -direction[axis], start, end)) return false;
    }
    return true;
}

float3 WaterCellNormal(WaterCell cell)
{
    float3 normal = cross(cell.b.xyz - cell.a.xyz, cell.c.xyz - cell.a.xyz);
    return normal * (normal.y >= 0 ? 1 : -1);
}

float WaterBaseHeight(WaterCell cell, float2 position)
{
    float3 normal = WaterCellNormal(cell);
    return cell.a.y - dot(normal.xz, position - cell.a.xz) / max(normal.y, 0.000001);
}

float WaterSurfaceHeight(WaterCell cell, WaterBody body, float2 position)
{
    return WaterBaseHeight(cell, position) + WaterHeightOffset(position, _WaterClock, body.wave0, body.wave1);
}

bool WaterPointInCell(WaterCell cell, float2 samplePosition)
{
    float sign = WaterCross(cell.b.xz - cell.a.xz, cell.c.xz - cell.a.xz) >= 0 ? 1 : -1;
    return sign * WaterCross(cell.b.xz - cell.a.xz, samplePosition - cell.a.xz) >= -0.0001
        && sign * WaterCross(cell.c.xz - cell.b.xz, samplePosition - cell.b.xz) >= -0.0001
        && sign * WaterCross(cell.a.xz - cell.c.xz, samplePosition - cell.c.xz) >= -0.0001;
}

bool WaterPointInVolume(WaterBody body, float3 samplePosition)
{
    float3 local = mul(body.worldToLocal, float4(samplePosition, 1)).xyz;
    if (body.authority.y > 0 && (any(local < body.volumeMin.xyz) || any(local > body.volumeMax.xyz))) return false;
    [loop] for (uint i = 0; i < (uint)body.range.w; i++)
    {
        WaterDryVolume dry = _WaterDryVolumes[(uint)body.range.z + i];
        if (all(local >= dry.minimum.xyz) && all(local <= dry.maximum.xyz)) return false;
    }
    return true;
}

bool WaterClipCell(WaterCell cell, WaterBody body, float3 origin, float3 direction, inout float start, inout float end)
{
    float sign = WaterCross(cell.b.xz - cell.a.xz, cell.c.xz - cell.a.xz) >= 0 ? 1 : -1;
    float2 edge = cell.b.xz - cell.a.xz;
    if (!WaterClipPlane(sign * WaterCross(edge, origin.xz - cell.a.xz), sign * WaterCross(edge, direction.xz), start, end)) return false;
    edge = cell.c.xz - cell.b.xz;
    if (!WaterClipPlane(sign * WaterCross(edge, origin.xz - cell.b.xz), sign * WaterCross(edge, direction.xz), start, end)) return false;
    edge = cell.a.xz - cell.c.xz;
    if (!WaterClipPlane(sign * WaterCross(edge, origin.xz - cell.c.xz), sign * WaterCross(edge, direction.xz), start, end)) return false;
    float3 normal = WaterCellNormal(cell);
    float margin = abs(body.wave0.z) + abs(body.wave1.z);
    if (!WaterClipPlane(dot(normal, cell.a.xyz - origin) + margin * normal.y, -dot(normal, direction), start, end)) return false;
    if (body.authority.y > 0)
    {
        float3 local = mul(body.worldToLocal, float4(origin, 1)).xyz;
        float3 velocity = mul(body.worldToLocal, float4(direction, 0)).xyz;
        if (!WaterClipBox(local, velocity, body.volumeMin.xyz, body.volumeMax.xyz, start, end)) return false;
    }
    return end > start + 0.00001;
}

void WaterPushInterval(float start, float end, uint body, uint cell,
    inout WaterInterval intervals[WATER_MAX_INTERVALS], inout uint count, inout uint overflow)
{
    if (end <= start + 0.00001) return;
    WaterInterval segment;
    segment.start = start; segment.end = end; segment.body = body; segment.cell = cell;
    [loop] for (uint emptyIndex = 0; emptyIndex < count; emptyIndex++)
    {
        if (intervals[emptyIndex].end > intervals[emptyIndex].start + 0.00001) continue;
        intervals[emptyIndex] = segment;
        return;
    }
    if (count == WATER_MAX_INTERVALS) { overflow = 1; return; }
    intervals[count++] = segment;
}

void WaterAppendInterval(float start, float end, uint body, uint cell,
    inout WaterInterval intervals[WATER_MAX_INTERVALS], inout uint count, inout uint overflow)
{
    if (end <= start + 0.00001) return;
    // 큰 월드의 인접 프리즘 경계에는 부동소수점 틈이 생긴다. mm 단위 허용차로 완전한 합집합을 만든다.
    float tolerance = min(0.02, 0.001 + 0.00002 * max(abs(start), abs(end)));
    uint mergeIndex = 0;
    [loop] for (uint iteration = 0; iteration < WATER_MAX_INTERVALS * WATER_MAX_INTERVALS && mergeIndex < count; iteration++)
    {
        WaterInterval existing = intervals[mergeIndex];
        if (existing.body != body || start > existing.end + tolerance || end < existing.start - tolerance)
        { mergeIndex++; continue; }
        start = min(start, existing.start);
        end = max(end, existing.end);
        intervals[mergeIndex] = intervals[--count];
        mergeIndex = 0;
    }
    WaterPushInterval(start, end, body, cell, intervals, count, overflow);
}

float WaterFindCrossing(WaterCell cell, WaterBody body, float3 origin, float3 direction, float start, float end, bool startWet)
{
    [unroll] for (uint iteration = 0; iteration < 8; iteration++)
    {
        float middle = (start + end) * 0.5;
        float3 samplePosition = origin + direction * middle;
        bool wet = samplePosition.y <= WaterSurfaceHeight(cell, body, samplePosition.xz);
        if (wet == startWet) start = middle;
        else end = middle;
    }
    return (start + end) * 0.5;
}

uint WaterCollectIntervals(float3 origin, float3 direction, float minimum, float maximum,
    out WaterInterval intervals[WATER_MAX_INTERVALS], out uint overflow)
{
    uint count = 0;
    overflow = 0;
    [loop] for (uint bodyIndex = 0; bodyIndex < (uint)_WaterBodyCount; bodyIndex++)
    {
        WaterBody body = _WaterBodies[bodyIndex];
        [loop] for (uint i = 0; i < (uint)body.range.y; i++)
        {
            uint cellIndex = (uint)body.range.x + i;
            WaterCell cell = _WaterCells[cellIndex];
            float start = minimum, end = maximum;
            if (!WaterClipCell(cell, body, origin, direction, start, end)) continue;
            if (abs(body.wave0.z) + abs(body.wave1.z) < 0.000001)
            {
                WaterAppendInterval(start, end, bodyIndex, cellIndex, intervals, count, overflow);
                continue;
            }
            // V1의 낮은 경사 파면에만 제한된 탐색을 사용한다. 저작 Validator가 큰 경사를 거절한다.
            float3 first = origin + direction * start;
            bool startWet = first.y <= WaterSurfaceHeight(cell, body, first.xz);
            float cursor = start;
            [loop] for (uint stepIndex = 1; stepIndex <= 8; stepIndex++)
            {
                float next = lerp(start, end, stepIndex / 8.0);
                float3 samplePosition = origin + direction * next;
                bool endWet = samplePosition.y <= WaterSurfaceHeight(cell, body, samplePosition.xz);
                if (startWet == endWet)
                {
                    if (startWet) WaterAppendInterval(cursor, next, bodyIndex, cellIndex, intervals, count, overflow);
                }
                else
                {
                    float crossing = WaterFindCrossing(cell, body, origin, direction, cursor, next, startWet);
                    WaterAppendInterval(startWet ? cursor : crossing, startWet ? crossing : next,
                        bodyIndex, cellIndex, intervals, count, overflow);
                }
                cursor = next;
                startWet = endWet;
            }
        }
        float3 local = mul(body.worldToLocal, float4(origin, 1)).xyz;
        float3 velocity = mul(body.worldToLocal, float4(direction, 0)).xyz;
        [loop] for (uint dryIndex = 0; dryIndex < (uint)body.range.w; dryIndex++)
        {
            WaterDryVolume dry = _WaterDryVolumes[(uint)body.range.z + dryIndex];
            float dryStart = minimum, dryEnd = maximum;
            if (!WaterClipBox(local, velocity, dry.minimum.xyz, dry.maximum.xyz, dryStart, dryEnd)) continue;
            uint previousCount = count;
            [loop] for (uint interval = 0; interval < previousCount; interval++)
            {
                WaterInterval segment = intervals[interval];
                if (segment.body != bodyIndex || segment.end <= dryStart || segment.start >= dryEnd) continue;
                intervals[interval].end = max(segment.start, dryStart);
                if (segment.end > dryEnd)
                    WaterPushInterval(max(segment.start, dryEnd), segment.end, bodyIndex, segment.cell, intervals, count, overflow);
            }
        }
        uint liveCount = 0;
        [loop] for (uint compactIndex = 0; compactIndex < count; compactIndex++)
            if (intervals[compactIndex].end > intervals[compactIndex].start + 0.00001)
                intervals[liveCount++] = intervals[compactIndex];
        count = liveCount;
    }
    return count;
}

float WaterAirTransmission(float distance)
{
    if (_WaterAirFog.x < 0.5) return 1;
    if (_WaterAirFog.x < 1.5)
        return saturate((_WaterAirFog.w - distance) / max(_WaterAirFog.w - _WaterAirFog.z, 0.00001));
    if (_WaterAirFog.x < 2.5) return exp(-_WaterAirFog.y * distance);
    return exp(-pow(_WaterAirFog.y * distance, 2));
}

float WaterBodyHeight(WaterBody body, float2 samplePosition)
{
    [loop] for (uint i = 0; i < (uint)body.range.y; i++)
    {
        WaterCell cell = _WaterCells[(uint)body.range.x + i];
        if (WaterPointInCell(cell, samplePosition)) return WaterSurfaceHeight(cell, body, samplePosition);
    }
    return -3.4e38;
}

WaterOptics WaterEvaluateMedium(float3 origin, float3 direction, float minimum, float maximum, inout float airDistance, bool applyAirFog)
{
    WaterOptics result;
    result.transmission = 1; result.scattering = 0; result.length = 0;
    result.endBody = WATER_NO_BODY; result.overflow = 0;
    if (maximum <= minimum) return result;
    WaterInterval intervals[WATER_MAX_INTERVALS];
    uint count = WaterCollectIntervals(origin, direction, minimum, maximum, intervals, result.overflow);
    float cursor = minimum;
    [loop] for (uint stepIndex = 0; stepIndex <= WATER_MAX_INTERVALS * 2; stepIndex++)
    {
        float next = maximum;
        [loop] for (uint boundaryIndex = 0; boundaryIndex < count; boundaryIndex++)
        {
            if (intervals[boundaryIndex].start > cursor + 0.00001) next = min(next, intervals[boundaryIndex].start);
            if (intervals[boundaryIndex].end > cursor + 0.00001) next = min(next, intervals[boundaryIndex].end);
        }
        float middle = (cursor + next) * 0.5;
        uint selected = WATER_NO_BODY;
        float priority = -3.4e38;
        [loop] for (uint selectionIndex = 0; selectionIndex < count; selectionIndex++)
        {
            WaterInterval segment = intervals[selectionIndex];
            if (middle < segment.start || middle >= segment.end) continue;
            WaterBody candidate = _WaterBodies[segment.body];
            if (candidate.authority.x < priority) continue;
            if (candidate.authority.x == priority && selected != WATER_NO_BODY
                && WaterBodyHeight(candidate, (origin + direction * middle).xz)
                <= WaterBodyHeight(_WaterBodies[selected], (origin + direction * middle).xz)) continue;
            selected = segment.body;
            priority = candidate.authority.x;
        }
        float length = max(0, next - cursor);
        float3 transmission = 1, scatter = 0;
        if (selected != WATER_NO_BODY)
        {
            WaterBody body = _WaterBodies[selected];
            transmission = exp(-max(0, body.extinction.xyz) * length);
            float illumination = max(body.scattering.w, saturate(max(_MainLightColor.r, max(_MainLightColor.g, _MainLightColor.b))));
            scatter = body.scattering.rgb * illumination;
            result.length += length;
        }
        else
        {
            float before = WaterAirTransmission(airDistance);
            float eyeScale = abs(TransformWorldToViewDir(direction, false).z);
            airDistance += length * eyeScale;
            transmission = applyAirFog ? (before > 0.00001 ? WaterAirTransmission(airDistance) / before : 0) : 1;
            scatter = _WaterAirFogColor.rgb;
        }
        result.scattering += result.transmission * (1 - transmission) * scatter;
        result.transmission *= transmission;
        result.endBody = selected;
        cursor = next;
        if (cursor >= maximum - 0.00001) break;
    }
    return result;
}

float3 WaterApplyOptics(float3 color, WaterOptics optics)
{
    return color * optics.transmission + optics.scattering;
}

float2 WaterStampGradient(float3 position, uint bodyIndex)
{
    WaterBody body = _WaterBodies[bodyIndex];
    float2 gradient = 0;
    [loop] for (uint i = 0; i < (uint)_WaterStampCount; i++)
    {
        WaterStamp stamp = _WaterStamps[i];
        if ((uint)stamp.parameters.y != bodyIndex) continue;
        float age = _WaterClock - stamp.positionTime.w;
        if (age < 0 || age > body.ripple.x) continue;
        float2 delta = position.xz - stamp.positionTime.xz;
        float distance = length(delta);
        float phase = (distance - age * body.ripple.y) / max(body.ripple.z, 0.001);
        float envelope = exp(-phase * phase) * (1 - age / max(body.ripple.x, 0.001));
        gradient += delta / max(distance, 0.001) * (-2 * phase) * envelope * stamp.parameters.x * body.ripple.w;
    }
    return gradient;
}

// 일반 AlphaBlend는 수면 뒤 다중 투명층을 지원하지 않는다. V1은 가장 가까운 수면 앞 조각만 그린다.
float3 WaterApplyTransparent(float3 position, float3 color, float2 screenUV, bool additive)
{
    if (_WaterSceneActive < 0.5) return color;
    float4 surface = SAMPLE_TEXTURE2D_X_LOD(_WaterSurfaceInfo, sampler_PointClamp, screenUV, 0);
    float eyeDepth = -TransformWorldToView(position).z;
    if (abs(surface.w) > 0.5 && surface.x < eyeDepth - 0.002) clip(-1);
    float3 ray = position - GetCameraPositionWS();
    float distance = length(ray);
    float3 origin = GetCameraPositionWS();
    if (unity_OrthoParams.w > 0.5)
    {
        float3 direction = -GetWorldSpaceNormalizeViewDir(position);
        distance = max(0, eyeDepth);
        origin = position - direction * distance;
        ray = direction * distance;
    }
    float air = 0;
    WaterOptics optics = WaterEvaluateMedium(origin, ray / max(distance, 0.00001), 0, distance, air, true);
    return color * optics.transmission + (additive ? 0 : optics.scattering);
}
#endif
