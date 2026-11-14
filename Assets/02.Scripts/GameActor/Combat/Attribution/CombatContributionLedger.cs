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
        public readonly int CreditCombatantId;
        public readonly UPlayGround.Data.EnumType.CharacterActorType CreditCharacter;

        public CombatKillContext(
            GameActor victim,
            GameActor killer,
            CombatCreditOwner creditOwner,
            float totalRecordedDamage, int creditCombatantId = 0,
            UPlayGround.Data.EnumType.CharacterActorType creditCharacter = default)
        {
            Victim = victim;
            Killer = killer;
            CreditOwner = creditOwner;
            TotalRecordedDamage = totalRecordedDamage;
            CreditCombatantId = creditCombatantId;
            CreditCharacter = creditCharacter;
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
        private int _lastCombatantId;
        private UPlayGround.Data.EnumType.CharacterActorType _lastCharacter;

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
            _lastCombatantId = combatantId;
            _lastCharacter = attacker.CharacterType;
        }

        /// <summary>반환 비행 중 반사자가 사라져도 확정한 귀속 ID와 진영을 보존한다.</summary>
        public void Record(in HitRequest request, float damage)
        {
            ProjectileDamageSource source = request.ProjectileDamageSource;
            if (source?.IsReflected != true) { Record(request.Attacker, damage); return; }
            if (damage <= 0f) return;
            _damageByCombatant.TryGetValue(source.CombatantRuntimeId, out float accumulated);
            _damageByCombatant[source.CombatantRuntimeId] = accumulated + damage;
            _totalRecordedDamage += damage;
            _lastAttacker = request.Attacker;
            _lastCreditOwner = source.CombatCreditOwner;
            _lastCombatantId = source.CombatantRuntimeId;
            _lastCharacter = source.CreditCharacter;
        }

        public CombatKillContext CreateKillContext(GameActor victim)
        {
            return new CombatKillContext(
                victim,
                _lastAttacker,
                _lastCreditOwner,
                _totalRecordedDamage, _lastCombatantId, _lastCharacter);
        }

        public void Clear()
        {
            _damageByCombatant.Clear();
            _lastAttacker = null;
            _lastCreditOwner = CombatCreditOwner.None;
            _totalRecordedDamage = 0f;
            _lastCombatantId = 0;
            _lastCharacter = default;
        }
    }
}
