using System;
using UnityEngine;
using UPlayGround.Ability.Core;

namespace UPlayGround.Data.Stat
{
    /// <summary>
    /// 프로젝트 데이터가 직렬화하는 검증 가능한 Attribute 참조.
    /// </summary>
    [Serializable]
    public struct AttributeReference : IEquatable<AttributeReference>
    {
        [SerializeField] private string _attributeId;

        public string AttributeId => _attributeId ?? string.Empty;
        public string Value => AttributeId;
        public bool IsValid() =>
            AttributeRegistry.IsRegistered(_attributeId);
        public AttributeId ToCoreId() => new(AttributeId);

        private AttributeReference(string attributeId)
        {
            _attributeId = attributeId?.Trim() ?? string.Empty;
        }

        public static AttributeReference CreateRegistered(
            string attributeId)
        {
            if (!AttributeRegistry.TryResolve(
                    attributeId,
                    out AttributeReference reference))
            {
                throw new ArgumentException(
                    $"AttributeRegistry에 등록되지 않은 ID입니다: '{attributeId}'",
                    nameof(attributeId));
            }

            return reference;
        }

        public static AttributeReference CreateCodeDefined(
            string attributeId) =>
            new(attributeId);

        internal static AttributeReference CreateResolved(
            string attributeId) =>
            new(attributeId);

        public bool Equals(AttributeReference other) =>
            string.Equals(
                AttributeId,
                other.AttributeId,
                StringComparison.Ordinal);
        public override bool Equals(object obj) =>
            obj is AttributeReference other && Equals(other);
        public override int GetHashCode() =>
            StringComparer.Ordinal.GetHashCode(AttributeId);
        public override string ToString() => AttributeId;
        public static bool operator ==(
            AttributeReference left,
            AttributeReference right) => left.Equals(right);
        public static bool operator !=(
            AttributeReference left,
            AttributeReference right) => !left.Equals(right);
        public static implicit operator AttributeId(
            AttributeReference reference) =>
            reference.ToCoreId();
    }

    /// <summary>
    /// 런타임 코드가 구조적으로 의존하는 프로젝트 표준 Attribute 슬롯.
    /// 손 작성 파일이며 레지스트리 검증기가 등록 일치를 강제한다.
    /// </summary>
    public static class Attributes
    {
        public static class Vital
        {
            public static readonly AttributeReference Health =
                AttributeReference.CreateCodeDefined("Vital.Health");
            public static readonly AttributeReference MaxHealth =
                AttributeReference.CreateCodeDefined("Vital.MaxHealth");
            public static readonly AttributeReference HealthRegenRate =
                AttributeReference.CreateCodeDefined("Vital.HealthRegenRate");
            public static readonly AttributeReference Poise =
                AttributeReference.CreateCodeDefined("Vital.Poise");
            public static readonly AttributeReference MaxPoise =
                AttributeReference.CreateCodeDefined("Vital.MaxPoise");
            public static readonly AttributeReference PoiseRecoveryRate =
                AttributeReference.CreateCodeDefined("Vital.PoiseRecoveryRate");
            public static readonly AttributeReference PoiseRecoveryDelay =
                AttributeReference.CreateCodeDefined("Vital.PoiseRecoveryDelay");
        }

        public static class Combat
        {
            public static readonly AttributeReference AttackPower =
                AttributeReference.CreateCodeDefined("Combat.AttackPower");
            public static readonly AttributeReference Defense =
                AttributeReference.CreateCodeDefined("Combat.Defense");
            public static readonly AttributeReference CritRate =
                AttributeReference.CreateCodeDefined("Combat.CritRate");
            public static readonly AttributeReference CritMultiplier =
                AttributeReference.CreateCodeDefined("Combat.CritMultiplier");
            public static readonly AttributeReference AttackSpeed =
                AttributeReference.CreateCodeDefined("Combat.AttackSpeed");
            public static readonly AttributeReference DamageTakenMultiplier =
                AttributeReference.CreateCodeDefined(
                    "Combat.DamageTakenMultiplier");
            public static readonly AttributeReference InvincibleDurationMultiplier =
                AttributeReference.CreateCodeDefined(
                    "Combat.InvincibleDurationMultiplier");
        }

        public static class Movement
        {
            public static readonly AttributeReference MoveSpeed =
                AttributeReference.CreateCodeDefined("Movement.MoveSpeed");
            public static readonly AttributeReference DashDistance =
                AttributeReference.CreateCodeDefined("Movement.DashDistance");
        }

        public static class Resource
        {
            public static readonly AttributeReference UltimateEnergy =
                AttributeReference.CreateCodeDefined(
                    "Resource.UltimateEnergy");
            public static readonly AttributeReference MaxUltimateEnergy =
                AttributeReference.CreateCodeDefined(
                    "Resource.MaxUltimateEnergy");
            public static readonly AttributeReference GenerationMultiplier =
                AttributeReference.CreateCodeDefined(
                    "Resource.GenerationMultiplier");
            public static readonly AttributeReference Forte =
                AttributeReference.CreateCodeDefined("Resource.Forte");
            public static readonly AttributeReference Concerto =
                AttributeReference.CreateCodeDefined("Resource.Concerto");
            public static readonly AttributeReference SkillCharge =
                AttributeReference.CreateCodeDefined("Resource.SkillCharge");
        }

        public static class Life
        {
            public static readonly AttributeReference GatheringPower =
                AttributeReference.CreateCodeDefined("Life.GatheringPower");
        }

        public static class Meta
        {
            public static readonly AttributeReference IncomingDamage =
                AttributeReference.CreateCodeDefined("Meta.IncomingDamage");
            public static readonly AttributeReference IncomingHealing =
                AttributeReference.CreateCodeDefined("Meta.IncomingHealing");
            public static readonly AttributeReference IncomingPoiseDamage =
                AttributeReference.CreateCodeDefined(
                    "Meta.IncomingPoiseDamage");
            public static readonly AttributeReference IncomingBreakDamage =
                AttributeReference.CreateCodeDefined(
                    "Meta.IncomingBreakDamage");
        }
    }
}
