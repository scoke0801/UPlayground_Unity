using UPlayGround.Data.Combat;
using UnityEngine;

namespace UPlayGround.Combat
{
    /// <summary>보스 이탈 유예와 공격 도중 전환 보류를 적용하는 교전 상태.</summary>
    public sealed class CombatEncounterState
    {
        private float _releaseRemaining;
        public CombatEncounterKind Current { get; private set; }

        public bool Evaluate(bool inCombat, bool arenaActive, bool bossInRange, bool bossAttacking,
            float holdSeconds, float deltaTime)
        {
            bool duel = arenaActive || bossInRange;
            if (duel) _releaseRemaining = Mathf.Max(0f, holdSeconds);
            else if (!bossAttacking) _releaseRemaining = Mathf.Max(0f, _releaseRemaining - Mathf.Max(0f, deltaTime));
            var desired = duel || (Current == CombatEncounterKind.Duel && _releaseRemaining > 0f)
                ? CombatEncounterKind.Duel : inCombat ? CombatEncounterKind.Sweep : CombatEncounterKind.Explore;
            if (desired == Current || bossAttacking) return false;
            Current = desired;
            return true;
        }

        public void Reset() { Current = CombatEncounterKind.Explore; _releaseRemaining = 0f; }
    }
}
