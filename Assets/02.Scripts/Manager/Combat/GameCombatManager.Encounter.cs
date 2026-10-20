using System;
using UPlayGround.Data.Combat;
using UPlayGround.Manager.Handler;

namespace UPlayGround.Manager.Combat
{
    public partial class GameCombatManager : ICombatEncounterService
    {
        private CombatEncounterHandler _encounter;
        public CombatEncounterKind CurrentEncounter => _encounter?.Current ?? CombatEncounterKind.Explore;
        public CombatEncounterProfileSO EncounterProfile => _encounter?.Profile;
        public bool IsBossAttacking => _encounter?.IsBossAttacking == true;
        public event Action<CombatEncounterKind> EncounterChanged;
        public void SetArenaActive(string arenaId, bool active) => _encounter?.SetArenaActive(arenaId, active);
        public void ResetEncounter() => _encounter?.Reset();
        private void NotifyEncounterChanged(CombatEncounterKind kind) => EncounterChanged?.Invoke(kind);
    }
}
