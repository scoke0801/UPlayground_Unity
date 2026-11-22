using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Manager;

namespace UPlayGround.Combat
{
    /// <summary>여러 표현의 Collider가 동일한 논리 타격 대상으로 해석되게 한다.</summary>
    public interface IEnvironmentHitRedirect
    {
        IDamageable HitTarget { get; }
    }

    /// <summary>약한 지면 식생과 달리 구조 피해를 받는 지형 배치다.</summary>
    public interface IEnvironmentStructureHit : IDamageable { }
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
