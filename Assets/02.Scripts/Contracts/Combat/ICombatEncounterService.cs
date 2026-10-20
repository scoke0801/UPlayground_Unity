using System;
using UnityEngine;
using UPlayGround.Data.Combat;

namespace UPlayGround.Manager
{
    /// <summary>카메라·플레이어·그룹이 공유하는 교전 판정 계약.</summary>
    public interface ICombatEncounterService : IGameService
    {
        CombatEncounterKind CurrentEncounter { get; }
        CombatEncounterProfileSO EncounterProfile { get; }
        bool IsBossAttacking { get; }
        event Action<CombatEncounterKind> EncounterChanged;
        void SetArenaActive(string arenaId, bool active);
        void ResetEncounter();
    }

    /// <summary>공격 방향 정렬에 사용할 살아 있는 명시적 락온 대상.</summary>
    public interface ILockOnTargetService : IGameService
    {
        Transform GetLockOnTarget();
    }
}
