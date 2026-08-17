using System.Collections.Generic;
using UPlayGround.Data.Combat;

namespace UPlayGround.Combat
{
    /// <summary>사망 시 모든 소비자가 동일한 처치 귀속 판정을 사용하도록 고정한 결과다.</summary>
    public readonly struct CombatKillContext
    {
        public readonly GameActor Victim;
        public readonly GameActor Killer;
        public readonly CombatCreditOwner CreditOwner;
        public readonly float TotalRecordedDamage;

        public CombatKillContext(
            GameActor victim,
            GameActor killer,
            CombatCreditOwner creditOwner,
            float totalRecordedDamage)
        {
            Victim = victim;
            Killer = killer;
            CreditOwner = creditOwner;
            TotalRecordedDamage = totalRecordedDamage;
        }

        public bool GrantsPlayerRewards => CreditOwner == CombatCreditOwner.PlayerParty;
        public bool CommitsWorldDeath => CreditOwner != CombatCreditOwner.None;
    }

    /// <summary>피격 대상별 유효 공격자와 피해 기여를 기록해 처치 귀속을 결정한다.</summary>
    public sealed class CombatContributionLedger
    {
        private readonly Dictionary<int, float> _damageByCombatant = new();
        private GameActor _lastAttacker;
        private CombatCreditOwner _lastCreditOwner;
        private float _totalRecordedDamage;

        public void Record(GameActor attacker, float damage)
        {
            if (attacker == null || damage <= 0f)
                return;

            int combatantId = attacker.CombatantRuntimeId;
            _damageByCombatant.TryGetValue(combatantId, out float accumulatedDamage);
            _damageByCombatant[combatantId] = accumulatedDamage + damage;
            _totalRecordedDamage += damage;
            _lastAttacker = attacker;
            _lastCreditOwner = CombatRelationUtility.GetCreditOwner(attacker);
        }

        public CombatKillContext CreateKillContext(GameActor victim)
        {
            return new CombatKillContext(
                victim,
                _lastAttacker,
                _lastCreditOwner,
                _totalRecordedDamage);
        }

        public void Clear()
        {
            _damageByCombatant.Clear();
            _lastAttacker = null;
            _lastCreditOwner = CombatCreditOwner.None;
            _totalRecordedDamage = 0f;
        }
    }
}
