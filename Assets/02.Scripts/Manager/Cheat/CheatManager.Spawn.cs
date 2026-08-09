#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Actor;

namespace UPlayGround.Manager
{
    /// <summary>CheatManager — 액터 스폰 치트(플레이어 전방 소환). 개발 빌드 전용.</summary>
    public partial class CheatManager
    {
        /// <summary>스폰 위치를 지면에 붙일 때 사용할 상향 오프셋/탐색 거리.</summary>
        private const float SpawnGroundProbeUp   = 3f;
        private const float SpawnGroundProbeDown = 30f;

        /// <summary>다중 스폰 시 액터끼리 겹치지 않도록 좌우로 벌리는 간격(m).</summary>
        private const float SpawnLateralSpacing = 2f;

        /// <summary>
        /// 활성 플레이어 캐릭터 전방에 actorId 액터를 스폰한다.
        /// <see cref="ActorSpawnManager.SpawnActor(string, Vector3, Quaternion, Group.MonsterGroupController, Transform)"/>
        /// 경로를 사용하므로 정의 주입(스탯/속성/AbilitySet)과 스폰 추적이 정상 동작한다.
        /// 스폰된 액터는 플레이어를 바라보도록 회전한다.
        /// </summary>
        /// <param name="actorId">ActorDatabase에 등록된 actorId.</param>
        /// <param name="count">스폰 마리 수(1 이상). 2 이상이면 좌우로 벌려 배치한다.</param>
        /// <param name="distance">플레이어 기준 전방 거리(m).</param>
        /// <param name="displayName">로그 표시용 이름. null이면 actorId를 사용한다.</param>
        /// <returns>실제로 스폰된 액터 수.</returns>
        public int SpawnActorInFrontOfPlayer(string actorId, int count = 1, float distance = 5f, string displayName = null)
        {
            string label = string.IsNullOrEmpty(displayName) ? actorId : displayName;

            if (string.IsNullOrEmpty(actorId))
            {
                Log(CheatCategory.Spawn, "스폰 실패: actorId가 비어 있음");
                return 0;
            }

            var spawner = ActorSpawnManager.Instance;
            if (spawner == null || !spawner.IsDBLoaded)
            {
                Log(CheatCategory.Spawn, $"스폰 실패: ActorDatabase 미로드 ({label})");
                return 0;
            }

            var player = PartyManager.Instance != null ? PartyManager.Instance.ActiveCharacter : null;
            if (player == null)
            {
                Log(CheatCategory.Spawn, $"스폰 실패: 활성 캐릭터 없음 ({label})");
                return 0;
            }

            count    = Mathf.Max(1, count);
            distance = Mathf.Max(0f, distance);

            Transform origin  = player.transform;
            Vector3   forward = origin.forward;
            forward.y = 0f;
            forward = forward.sqrMagnitude < 0.0001f ? Vector3.forward : forward.normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward);

            Vector3    center   = origin.position + forward * distance;
            Quaternion rotation = Quaternion.LookRotation(-forward, Vector3.up); // 플레이어를 바라보게

            int spawned = 0;
            for (int i = 0; i < count; i++)
            {
                // 0, +1, -1, +2, -2 … 순으로 좌우 교대 배치
                int   step   = (i + 1) / 2;
                float side   = (i % 2 == 0) ? step : -step;
                Vector3 pos  = SnapToGround(center + right * (side * SpawnLateralSpacing));

                if (spawner.SpawnActor(actorId, pos, rotation) != null)
                    spawned++;
            }

            Log(CheatCategory.Spawn,
                spawned > 0
                    ? $"스폰: {label} x{spawned} (전방 {distance:0.#}m)"
                    : $"스폰 실패: {label} (프리팹/정의 확인 필요)");
            return spawned;
        }

        /// <summary>지면을 찾아 스폰 위치를 보정한다. 지면을 못 찾으면 입력 위치를 그대로 쓴다.</summary>
        private static Vector3 SnapToGround(Vector3 position)
        {
            Vector3 probe = position + Vector3.up * SpawnGroundProbeUp;
            if (Physics.Raycast(probe, Vector3.down, out RaycastHit hit,
                    SpawnGroundProbeUp + SpawnGroundProbeDown, ~0, QueryTriggerInteraction.Ignore))
            {
                return hit.point;
            }
            return position;
        }

        /// <summary>치트 UI가 목록에 표시할 스폰 가능 액터 정의를 반환한다.</summary>
        public IReadOnlyList<ActorDefinitionSO> GetSpawnableDefinitions()
        {
            ActorDatabase db = ActorSpawnManager.Instance != null ? ActorSpawnManager.Instance.Database : null;
            return db != null ? db.All : System.Array.Empty<ActorDefinitionSO>();
        }
    }
}
#endif
