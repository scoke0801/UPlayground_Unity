using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Manager;

namespace UPlayGround.Components
{
    /// <summary>
    /// 플레이어를 노리는(어그로 중인) 몬스터 위치를 주기적으로 한 번만 스캔해 모든 NPC가 공유한다.
    /// NPC마다 물리 질의나 액터 순회를 하지 않게 해 주민 수가 늘어도 비용이 거의 늘지 않는다.
    /// </summary>
    public static class NpcThreatScanner
    {
        private const float RefreshInterval = 0.25f;
        private const int InitialCapacity = 16;

        private static readonly List<Vector3> s_positions = new List<Vector3>(InitialCapacity);
        private static float s_nextScanTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_positions.Clear();
            s_nextScanTime = 0f;
        }

        /// <summary>origin에서 수평 거리로 가장 가까운 위협을 찾는다. 위협이 없으면 false.</summary>
        public static bool TryGetNearest(Vector3 origin, out Vector3 threatPosition, out float horizontalDistance)
        {
            RefreshIfStale();

            threatPosition = default;
            horizontalDistance = float.MaxValue;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < s_positions.Count; i++)
            {
                Vector3 offset = s_positions[i] - origin;
                offset.y = 0f;
                float sqr = offset.sqrMagnitude;
                if (sqr >= bestSqr) continue;
                bestSqr = sqr;
                threatPosition = s_positions[i];
            }

            if (bestSqr == float.MaxValue) return false;
            horizontalDistance = Mathf.Sqrt(bestSqr);
            return true;
        }

        private static void RefreshIfStale()
        {
            if (Time.time < s_nextScanTime) return;
            s_nextScanTime = Time.time + RefreshInterval;
            s_positions.Clear();

            if (!Services.TryGet<IActorQueryService>(out var query)) return;
            if (query.AllActors is not IReadOnlyList<IWorldActor> actors) return;

            for (int i = 0; i < actors.Count; i++)
            {
                if (actors[i] is not MonsterActor monster || !monster.isActiveAndEnabled || !monster.IsAlive())
                    continue;
                if (monster.AIController == null || !monster.AIController.HasAggroTarget)
                    continue;
                s_positions.Add(monster.transform.position);
            }
        }
    }
}
