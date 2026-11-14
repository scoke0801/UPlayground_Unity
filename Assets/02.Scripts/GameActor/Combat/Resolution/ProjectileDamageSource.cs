using UPlayGround.Data;
using UPlayGround.Data.Combat;
using UPlayGround.Manager;
using UnityEngine;

namespace UPlayGround.Combat
{
    /// <summary>소멸한 액터를 조회하지 않고 발사 수치와 확정 기여도를 보존한다.</summary>
    public sealed class ProjectileDamageSource : ICombatAffiliationView
    {
        public readonly HitRequest Launch;
        public readonly float AttackPower;
        public readonly CombatElement Element;
        public readonly float ElementAdvantage;
        public readonly int OriginalCombatantId;
        public readonly bool IsReflected;
        public readonly UPlayGround.Data.EnumType.CharacterActorType CreditCharacter;
        public int CombatantRuntimeId { get; }
        public string CombatFactionId { get; }
        public CombatCreditOwner CombatCreditOwner { get; }
        public bool IsCombatAvailable => true;

        public ProjectileDamageSource(AttackData attack)
        {
            Launch = HitRequest.FromAttackData(attack);
            GameActor owner = attack.attacker;
            AttackPower = owner?.AbilitySystem != null && owner.AbilitySystem.TryGetAttribute(
                global::UPlayGround.Data.Stat.Attributes.Combat.AttackPower, true, out float power) ? power : 1f;
            Element = owner != null ? owner.CurrentElement : default;
            ElementAdvantage = owner != null ? owner.ElementalAdvantageMultiplier : 1f;
            OriginalCombatantId = owner != null ? owner.CombatantRuntimeId : 0;
            CombatantRuntimeId = OriginalCombatantId;
            CreditCharacter = owner != null ? owner.CharacterType : default;
            CombatFactionId = owner != null ? owner.CombatFactionId : null;
            CombatCreditOwner = owner != null ? CombatRelationUtility.GetCreditOwner(owner) : CombatCreditOwner.None;
        }

        private ProjectileDamageSource(ProjectileDamageSource launch, GameActor reflector)
        {
            Launch = launch.Launch;
            AttackPower = launch.AttackPower;
            Element = launch.Element;
            ElementAdvantage = launch.ElementAdvantage;
            OriginalCombatantId = launch.OriginalCombatantId;
            IsReflected = true;
            CombatantRuntimeId = reflector.CombatantRuntimeId;
            CreditCharacter = reflector.CharacterType;
            CombatFactionId = reflector.CombatFactionId;
            CombatCreditOwner = CombatRelationUtility.GetCreditOwner(reflector);
        }

        public ProjectileDamageSource ReflectedBy(GameActor reflector) => new(this, reflector);
    }
}
