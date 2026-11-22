#ifndef UPG_TOON_FOG_INTEGRATION
#define UPG_TOON_FOG_INTEGRATION

float ToonFogPhi(float q)
{
    return q < 0.001 ? 1 - q * (0.5 - q * (1.0 / 6 - q / 24)) : (1 - exp(-q)) / q;
}

float ToonFogHeightIntegral(float y0, float y1, float segmentLength, float baseHeight, float scale)
{
    // 양 끝을 정렬하면 상향과 하향에서 같은 적분을 얻는다.
    float lower = min(y0, y1) - baseHeight;
    float upper = max(y0, y1) - baseHeight;
    if (upper <= 0) return segmentLength;
    float above = lower < 0 ? upper / (upper - lower) : 1;
    return segmentLength * ((1 - above) +
        above * exp(-max(lower, 0) / scale) * ToonFogPhi((upper - max(lower, 0)) / scale));
}

bool ToonFogClipBox(float3 origin, float3 direction, inout float start, inout float end)
{
    [unroll] for (int axis = 0; axis < 3; axis++)
    {
        if (abs(direction[axis]) < 1e-12)
        {
            if (abs(origin[axis]) > 1) return false;
        }
        else
        {
            float2 hits = (float2(-1, 1) - origin[axis]) / direction[axis];
            start = max(start, min(hits.x, hits.y));
            end = min(end, max(hits.x, hits.y));
        }
    }
    return end > start;
}

bool ToonFogClipSphere(float3 origin, float3 direction, inout float start, inout float end)
{
    float directionLength = length(direction);
    if (directionLength < 1e-12) return false;
    float3 axis = direction / directionLength;
    float projection = -dot(origin, axis);
    float3 nearest = origin + axis * projection;
    float halfChordSquared = 1 - dot(nearest, nearest);
    if (halfChordSquared <= 0) return false;
    float halfChord = sqrt(halfChordSquared);
    start = max(start, (projection - halfChord) / directionLength);
    end = min(end, (projection + halfChord) / directionLength);
    return end > start;
}

#endif
