using UnityEngine;

namespace UPlayGround.Combat
{
    /// <summary>
    /// 히트 방향을 넉백 임펄스용 수평 방향으로 변환한다.
    ///
    /// AttackDirection은 히트박스 스윕 델타에서 유도되므로(<see cref="CombatHitDetector"/>) 위/아래로 휘두르는
    /// 모션에서는 거의 수직에 가까운 값이 나온다. 이 값을 그대로 임펄스에 쓰면
    /// ActorMovementController.AddImpulse가 상승 성분을 감지해 ForceUnground를 호출하고,
    /// 피격자가 넉백 세기만큼 공중으로 솟구친다.
    /// 수직 변위는 Airborne 반응의 airborneForce가 단독으로 소유해야 하므로 넉백 방향은 항상 평탄화한다.
    /// </summary>
    public static class KnockbackDirectionResolver
    {
        private const float MinSqrMagnitude = 0.0001f;

        /// <summary>
        /// 수평 성분만 남긴 정규화 방향을 돌려준다.
        /// attackDirection이 수직에 가까워 수평 성분이 소실되면 공격자→피격자 방향으로,
        /// 그마저 없으면 피격자의 뒤쪽(등 방향)으로 폴백한다. 어떤 경우에도 0 벡터를 돌려주지 않는다.
        /// </summary>
        public static Vector3 ResolveHorizontal(
            Vector3 attackDirection,
            Transform attacker,
            Transform victim,
            Vector3 up)
        {
            if (up.sqrMagnitude < MinSqrMagnitude)
                up = Vector3.up;
            else
                up = up.normalized;

            Vector3 planar = Vector3.ProjectOnPlane(attackDirection, up);
            if (planar.sqrMagnitude >= MinSqrMagnitude)
                return planar.normalized;

            if (attacker != null && victim != null)
            {
                planar = Vector3.ProjectOnPlane(victim.position - attacker.position, up);
                if (planar.sqrMagnitude >= MinSqrMagnitude)
                    return planar.normalized;
            }

            if (victim != null)
            {
                planar = Vector3.ProjectOnPlane(-victim.forward, up);
                if (planar.sqrMagnitude >= MinSqrMagnitude)
                    return planar.normalized;
            }

            planar = Vector3.ProjectOnPlane(Vector3.forward, up);
            if (planar.sqrMagnitude < MinSqrMagnitude)
                planar = Vector3.ProjectOnPlane(Vector3.right, up);
            return planar.normalized;
        }
    }
}
