using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Manager;

namespace UPlayGround.Combat
{
    /// <summary>명시한 환경 피격 Collider만 허용한다.</summary>
    public interface IEnvironmentColliderFilter
    {
        bool CanReceiveCollider(Collider collider);
    }

    /// <summary>물리 Collider 없이 공격 형상 안의 환경 셀을 같은 타격 목록에 수집한다.</summary>
    public interface IEnvironmentCellHitService : IGameService
    {
        void CollectCellHits(Transform owner, in CombatHitboxShape shape, ISet<IDamageable> ignored,
            ISet<IDamageable> collected, List<CombatHit> results);
    }
}
