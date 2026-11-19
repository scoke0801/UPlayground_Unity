// WorldToon 정점 바람. 바람 입력의 유일한 작성자는 EnvironmentShaderState(_LW_Wind, _LW_PreviousWind)이며 여기서는 읽기만 한다.
// _LW_Wind = (방향.x, 0, 방향.z, 세기). 세기는 0~1 범위로 쓰인다.
// 위치 변형은 컬러·그림자·깊이·노멀·모션벡터 패스가 모두 이 함수 하나로 계산해 실루엣을 일치시킨다.
#ifndef WORLDTOON_WIND_INCLUDED
#define WORLDTOON_WIND_INCLUDED

#ifndef WORLDTOON_LW_WIND_DECLARED
#define WORLDTOON_LW_WIND_DECLARED
float4 _LW_Wind;
float4 _LW_PreviousWind;
#endif

// 바람 가중치: 정점 색 R(뿌리 0, 끝 1). G·A는 예약, B는 개체 위상 보조.
// 바람 세기가 0이어도 재질의 _WindIdle만큼은 흔들려 소유자가 없는 씬에서 식생이 굳지 않게 한다.
float3 WorldToonWindOffsetWS(float3 positionWS, float3 pivotWS, half4 vertexColor, float4 wind, float time)
{
    float magnitude = saturate(wind.w);
    float2 direction = magnitude > 1e-4 ? wind.xz / max(length(wind.xz), 1e-4) : float2(0.7071, 0.7071);
    float strength = max(magnitude, _WindIdle);

    float phase = dot(pivotWS.xz, float2(0.173, 0.131)) + vertexColor.b * 6.2831;
    float gust = sin(time * _WindFrequency + phase);
    float flutter = sin(time * _WindFrequency * 2.37 + phase * 1.7 + dot(positionWS.xz, float2(0.9, 0.7)));
    // 바람 방향으로 기운 상태(lean)를 중심으로 흔들린다.
    float sway = strength * (0.35 + 0.45 * gust) + 0.2 * flutter * strength;

    float weight = vertexColor.r;
    float amount = sway * _WindAmplitude * weight;
    float3 offset = float3(direction.x, 0, direction.y) * amount;
    offset.y = -abs(amount) * 0.15 * weight; // 휘어질 때 끝이 약간 내려간다(길이 보존 근사)
    return offset;
}

float3 WorldToonWindPositionWS(float3 positionWS, half4 vertexColor)
{
    float3 pivotWS = TransformObjectToWorld(float3(0, 0, 0));
    return positionWS + WorldToonWindOffsetWS(positionWS, pivotWS, vertexColor, _LW_Wind, _Time.y);
}

// 이전 프레임 위치. 모션벡터가 바람 이동을 잔상 없이 표현하도록 이전 바람 값과 이전 시간을 쓴다.
float3 WorldToonWindPreviousPositionWS(float3 previousPositionWS, half4 vertexColor)
{
    float3 pivotWS = mul(UNITY_PREV_MATRIX_M, float4(0, 0, 0, 1)).xyz;
    return previousPositionWS + WorldToonWindOffsetWS(previousPositionWS, pivotWS, vertexColor, _LW_PreviousWind, _Time.y - unity_DeltaTime.x);
}

#endif
