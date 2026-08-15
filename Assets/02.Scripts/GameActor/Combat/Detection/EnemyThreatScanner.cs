using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Components;

namespace UPlayGround.Combat
{
    /// <summary>
    /// 주변 적의 "임박했거나 활성 중인 공격 위협"을 찾는 공용 스캐너.
    ///
    /// 피격(HitRequest) 기반 판정은 히트박스와 실제로 겹쳐야만 성립하므로,
    /// 빠르게 빠져나가는 이동(대시 등)에서는 판정이 아예 발화하지 않는다.
    /// 이 스캐너는 겹침이 아니라 <see cref="EnemyCombat.TryGetSwapEvadeThreat"/>의
    /// 위협 반경/텔레그래프 시간으로 판정하므로 "스쳐 지나간" 회피도 잡아낸다.
    ///
    /// 스왑 회피(PartyManager)와 대시 회피(PlayerDashState)가 같은 규칙을 공유한다.
    /// </summary>
    public static class EnemyThreatScanner
    {
        /// <summary>
        /// origin 주변 searchRange 안에서 가장 임박한 위협을 찾는다.
        /// 호출자가 버퍼와 중복 제거용 집합을 소유해 프레임 할당을 피한다.
        /// </summary>
        /// <param name="beforeHitWindow">히트박스가 켜지기 전 몇 초까지 위협으로 볼지</param>
        /// <param name="afterHitGrace">히트박스가 켜진 뒤 몇 초까지 위협으로 볼지</param>
        /// <param name="radiusPadding">위협 반경에 더할 여유(미터)</param>
        public static bool TryFindBestThreat(
            Vector3 origin,
            float searchRange,
            LayerMask threatLayer,
            float beforeHitWindow,
            float afterHitGrace,
            float radiusPadding,
            Collider[] overlapBuffer,
            HashSet<MonsterActor> evaluatedScratch,
            out EnemyAttackThreat bestThreat)
        {
            bestThreat = default;

            if (searchRange <= 0f || overlapBuffer == null || overlapBuffer.Length == 0)
                return false;

            int hitCount = Physics.OverlapSphereNonAlloc(
                origin,
                searchRange,
                overlapBuffer,
                threatLayer,
                QueryTriggerInteraction.Ignore);

            bool found = false;
            float bestScore = float.MaxValue;
            evaluatedScratch?.Clear();

            for (int i = 0; i < hitCount; i++)
            {
                var hit = overlapBuffer[i];
                if (hit == null) continue;

                var monster = hit.GetComponentInParent<MonsterActor>();
                if (monster == null) continue;

                // 한 몬스터에 콜라이더가 여러 개여도 한 번만 평가한다.
                if (evaluatedScratch != null && !evaluatedScratch.Add(monster))
                    continue;

                var combat = monster.Combat;
                if (combat == null) continue;

                if (!combat.TryGetSwapEvadeThreat(
                        origin,
                        beforeHitWindow,
                        afterHitGrace,
                        radiusPadding,
                        out EnemyAttackThreat threat))
                    continue;

                // 이미 히트박스가 켜진 위협을 텔레그래프 단계보다 우선한다.
                float score = threat.IsCollisionActive
                    ? -1f
                    : Mathf.Max(0f, threat.TimeToHit);
                if (score >= bestScore)
                    continue;

                bestScore = score;
                bestThreat = threat;
                found = true;
            }

            return found;
        }
    }
}
