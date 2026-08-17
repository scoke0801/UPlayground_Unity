using NUnit.Framework;
using UPlayGround.Components;
using UPlayGround.Data.Combat;

namespace UPlayGround.Combat.Tests
{
    public sealed class CombatFactionAndThreatPolicyTests
    {
        [TestCase(CombatFactionRules.PlayerPartyId, CombatFactionRules.PlayerPartyId, CombatRelation.Ally)]
        [TestCase(CombatFactionRules.WorldHostileId, CombatFactionRules.WorldHostileId, CombatRelation.Ally)]
        [TestCase(CombatFactionRules.PlayerPartyId, CombatFactionRules.WorldHostileId, CombatRelation.Hostile)]
        [TestCase(CombatFactionRules.PlayerPartyId, CombatFactionRules.WorldNeutralId, CombatRelation.Neutral)]
        public void DefaultRelation_IsSymmetricAndExplicit(
            string source,
            string target,
            CombatRelation expected)
        {
            Assert.That(CombatFactionRules.ResolveDefaultRelation(source, target), Is.EqualTo(expected));
            Assert.That(CombatFactionRules.ResolveDefaultRelation(target, source), Is.EqualTo(expected));
        }

        [Test]
        public void TargetPolicy_DistinguishesSelfAllyAndHostile()
        {
            Assert.That(CombatFactionRules.MatchesPolicy(
                CombatRelation.Ally,
                true,
                CombatTargetPolicy.Self), Is.True);
            Assert.That(CombatFactionRules.MatchesPolicy(
                CombatRelation.Ally,
                false,
                CombatTargetPolicy.Ally), Is.True);
            Assert.That(CombatFactionRules.MatchesPolicy(
                CombatRelation.Hostile,
                false,
                CombatTargetPolicy.Hostile), Is.True);
            Assert.That(CombatFactionRules.MatchesPolicy(
                CombatRelation.Ally,
                false,
                CombatTargetPolicy.Hostile), Is.False);
        }

        [TestCase(10f, 12.5f, 1.25f, false)]
        [TestCase(10f, 12.51f, 1.25f, true)]
        [TestCase(0f, 0.1f, 1.25f, true)]
        public void ThreatSwitch_RequiresHysteresisMargin(
            float current,
            float candidate,
            float multiplier,
            bool expected)
        {
            Assert.That(
                EnemyAggroPolicy.ShouldSwitchTarget(current, candidate, multiplier),
                Is.EqualTo(expected));
        }
    }
}
