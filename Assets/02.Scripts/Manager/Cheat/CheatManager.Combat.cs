#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

namespace UPlayGround.Manager
{
    /// <summary>CheatManager — 전투 치트(주변 몬스터 처치 등). 개발 빌드 전용.</summary>
    public partial class CheatManager
    {
        /// <summary>
        /// 활성 플레이어 캐릭터 반경 내의 살아있는 몬스터를 즉시 처치한다.
        /// <see cref="MonsterActor.SetHealth"/>(0) 경로를 사용하므로 실제 처치와 동일하게
        /// 드랍/경험치/골드/퀘스트·레시피 알림/파티 합류가 모두 발생한다.
        /// 무적 플래그·가드 상태는 우회한다(치트이므로 의도된 동작).
        /// </summary>
        /// <param name="radius">처치 반경(m).</param>
        /// <returns>처치한 몬스터 수.</returns>
        public int KillNearbyMonsters(float radius = 30f)
        {
            var player = PartyManager.Instance != null ? PartyManager.Instance.ActiveCharacter : null;
            if (player == null)
            {
                Log(CheatCategory.Combat, "주변 몬스터 처치 실패: 활성 캐릭터 없음");
                return 0;
            }

            Vector3 origin  = player.transform.position;
            float sqrRadius = radius * radius;
            int killed      = 0;

            var monsters = UnityEngine.Object.FindObjectsByType<MonsterActor>(FindObjectsSortMode.None);
            foreach (var monster in monsters)
            {
                if (monster == null || !monster.IsAlive())
                    continue;
                if ((monster.transform.position - origin).sqrMagnitude > sqrRadius)
                    continue;

                monster.SetHealth(0f);
                killed++;
            }

            Log(CheatCategory.Combat, $"주변 몬스터 처치: {killed}마리 (반경 {radius:0.#}m)");
            return killed;
        }
    }
}
#endif
