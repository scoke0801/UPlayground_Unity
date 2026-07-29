using System;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UPlayGround.Components;
using UPlayGround.Data.EnumType;
using UPlayGround.Group;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace UPlayGround.Data.Event
{
    [Serializable]
    [MovedFrom(true, sourceAssembly: "Assembly-CSharp")]
    public class SpawnTargetData
    {
        public GameObject targetPrefab;
        public int spawnCount;
        public float spawnRadius;
        public float avoidanceRadius = 0.5f;
    }
    /// <summary>
    /// 투사체 발사 이벤트
    /// </summary>
    [Serializable]
    [MovedFrom(true, sourceAssembly: "Assembly-CSharp")]
    [MotionEventDescriptor("SpawnSkill", "VFX / SFX", 0, "스킬 오브젝트를 생성합니다.", "skill", "cast", "스킬", "소환")]
    public class SpawnSkillEvent : MotionEventBase
    {
        private const int OverlapBufferSize = 64;
        private const float OverlapResolveRadius = 2.0f;

        public List<SpawnTargetData> spawnTargetList;
        public int resolveIterations = 3; // 겹침을 해결하기 위한 반복 횟수

        [NonSerialized] private List<Collider> _spawnedColliders;
        [NonSerialized] private Collider[] _overlapBuffer;
        
        public override string GetDisplayName() => "SpawnSkill";

        public override string GetShortLabel()
        {
            return "HealSKill";
        }

        public override void Execute(GameObject target)
        {
            GameActor actor = target.GetComponent<GameActor>();
            if (actor == null)
            {
                return;
            }
            _spawnedColliders ??= new List<Collider>();
            _spawnedColliders.Clear();

            // 1. 일단 모두 생성 (랜덤 반경 내)
            foreach (var data in spawnTargetList)
            {
                for (int i = 0; i < data.spawnCount; ++i)
                {
                    Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * data.spawnRadius;
                    Vector3 spawnPos = target.transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);
                    
                    GameObject spawned = GameObject.Instantiate(data.targetPrefab, spawnPos, Quaternion.identity);
                    if (spawned.TryGetComponent<Collider>(out var col))
                    {
                        _spawnedColliders.Add(col);

                        if (actor.HasActorType(ActorType.Monster))
                        {
                            var spawnedMonster = spawned.GetComponent<MonsterActor>();
                            if (spawnedMonster != null)
                                HandleMonsterActor(actor as MonsterActor, spawnedMonster);
                        }
                    }
                }
            }
            
            // 생성된 항목들에 대해 밀어내기( 겹치지 않도록 )
            for (int iter = 0; iter < resolveIterations; iter++)
            {
                foreach (var col in _spawnedColliders)
                {
                    ResolveOverlap(col);
                }
            }
        }

        private void HandleMonsterActor(MonsterActor summoner, MonsterActor spawned)
        {
            if (summoner == null || summoner.Combat == null) return;

            summoner.Combat.RegisterSpawnedUnit(spawned.transform);

            // 소환된 유닛을 소환사의 그룹에 편입 (Summon 우선순위 — 슬롯 후순위)
            var group = summoner.AIController?.Group;
            if (group != null)
                group.RegisterMember(spawned, MemberPriority.Summon);
        }

        private void ResolveOverlap(Collider targetCol)
        {
            // 주변의 다른 콜라이더 탐색 (자신 제외)
            _overlapBuffer ??= new Collider[OverlapBufferSize];
            int overlapCount = Physics.OverlapSphereNonAlloc(
                targetCol.bounds.center,
                OverlapResolveRadius,
                _overlapBuffer);

            for (int i = 0; i < overlapCount; i++)
            {
                var otherCol = _overlapBuffer[i];
                if (targetCol == otherCol) continue;

                // 두 콜라이더 간의 겹침 정보 계산
                if (Physics.ComputePenetration(
                        targetCol, targetCol.transform.position, targetCol.transform.rotation,
                        otherCol, otherCol.transform.position, otherCol.transform.rotation,
                        out Vector3 direction, out float distance))
                {
                    // 겹친 만큼 방향으로 밀어내기 (Y축은 고정하고 싶다면 0으로 설정 가능)
                    Vector3 separation = direction * distance;
                    separation.y = 0; // 지면 아래로 박히거나 뜨는 것 방지
                    
                    targetCol.transform.position += separation;
                }
            }
        }
        
        public override void OnCompleteEvent(GameObject target)
        {
        }
    }

}
