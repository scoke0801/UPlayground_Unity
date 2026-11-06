#ifndef UPG_FOLIAGE_INTERACTION_INCLUDED
#define UPG_FOLIAGE_INTERACTION_INCLUDED

// FoliageInteractionManager가 매 프레임 채우는 전역 값. 액터가 없으면 Count가 0이라 루프를 돌지 않는다.
// 전역 배열은 SRP Batcher의 UnityPerMaterial CBUFFER 밖에 선언해야 배칭이 유지된다.
#define UPG_FOLIAGE_MAX_ACTORS 8

float4 _UPG_FoliageActors[UPG_FOLIAGE_MAX_ACTORS];        // xyz = 월드 위치, w = 영향 반경(m)
float4 _UPG_FoliageActorParams[UPG_FOLIAGE_MAX_ACTORS];   // x = 눕힘 강도(0~1)
float _UPG_FoliageActorCount;
float4 _UPG_FoliageBend;                                  // x = 최대 수평 변위(m), y = 눕힘에 따른 하강 비율

// 뿌리 마스크(0=고정, 1=자유)가 클수록 액터에서 멀어지는 방향으로 많이 눕는다. 결과는 월드 공간 변위(m).
float3 UPG_FoliageBendOffsetWS(float3 positionWS, float rootMask)
{
    float3 offset = 0;
    int count = min((int)_UPG_FoliageActorCount, UPG_FOLIAGE_MAX_ACTORS);

    [loop]
    for (int i = 0; i < count; i++)
    {
        float3 away = positionWS - _UPG_FoliageActors[i].xyz;
        away.y = 0;
        float distanceToActor = length(away);
        float falloff = saturate(1.0 - distanceToActor / max(_UPG_FoliageActors[i].w, 0.001));
        falloff *= falloff;
        offset += (away / max(distanceToActor, 0.001)) * (falloff * _UPG_FoliageActorParams[i].x);
    }

    offset *= rootMask * _UPG_FoliageBend.x;
    offset.y -= length(offset) * _UPG_FoliageBend.y;
    return offset;
}

// 오브젝트 공간 정점 오프셋에 더할 값. Shader Graph Custom Function(File)에서도 이 이름 규칙으로 호출한다.
float3 UPG_FoliageBendOffsetOS(float3 positionWS, float rootMask)
{
    return TransformWorldToObjectDir(UPG_FoliageBendOffsetWS(positionWS, rootMask), false);
}

void UPG_FoliageBend_float(float3 positionWS, float rootMask, out float3 offsetOS)
{
    offsetOS = UPG_FoliageBendOffsetOS(positionWS, rootMask);
}

// Shader Graph Custom Function(File)용. 입력과 출력이 모두 월드 공간이라 그래프의 World→Object 변환 앞단에 더한다.
void UPG_FoliageBendWS_float(float3 positionWS, float rootMask, out float3 offsetWS)
{
    offsetWS = UPG_FoliageBendOffsetWS(positionWS, rootMask);
}

#endif
