using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UPlayGround.Components;
using UPlayGround.Data;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Gameplay.Ability;
using UPlayGround.Manager;
using UnityEngine;

namespace UPlayGround.Combat.Tests
{
    public sealed class CombatFactionAndThreatPolicyTests
    {
        [SetUp]
        public void SetUp()
        {
            Services.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            Services.Clear();
        }

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

        [TestCase(CombatRelation.Hostile, true)]
        [TestCase(CombatRelation.Ally, false)]
        [TestCase(CombatRelation.Neutral, false)]
        public void DangerRing은_현재_플레이어에게_적대적인_공격자만_표시한다(
            CombatRelation relation,
            bool expected)
        {
            var combatContext = new FakeCombatContext(relation);
            Services.Register(combatContext);

            var gameObject = new GameObject("DangerRingRelationTest");
            try
            {
                MonsterActor monster = gameObject.AddComponent<MonsterActor>();
                AbilitySystemComponent abilitySystem =
                    gameObject.AddComponent<AbilitySystemComponent>();
                abilitySystem.EnsureInitialized();
                PropertyInfo abilitySystemProperty = typeof(GameActor).GetProperty(
                    nameof(GameActor.AbilitySystem),
                    BindingFlags.Instance | BindingFlags.Public);
                Assert.That(abilitySystemProperty, Is.Not.Null);
                abilitySystemProperty.SetValue(monster, abilitySystem);
                abilitySystem.Attributes.SetBase(
                    global::UPlayGround.Data.Stat.Attributes.Vital.MaxHealth,
                    100f);
                abilitySystem.Attributes.SetBase(
                    global::UPlayGround.Data.Stat.Attributes.Vital.Health,
                    100f);
                EnemyCombat combat = gameObject.AddComponent<EnemyCombat>();
                FieldInfo ownerActor = typeof(EnemyCombat).GetField(
                    "_ownerActor",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(ownerActor, Is.Not.Null);
                ownerActor.SetValue(combat, monster);
                var skill = new AbilityAttackInfo { useDangerRing = true };
                MethodInfo shouldShow = typeof(EnemyCombat).GetMethod(
                    "ShouldShowDangerRing",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.That(shouldShow, Is.Not.Null);
                Assert.That(
                    shouldShow.Invoke(combat, new object[] { skill }),
                    Is.EqualTo(expected));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        private sealed class FakeCombatContext : IActorQueryService, ICombatRelationService
        {
            private readonly CombatRelation _relation;
            private readonly FakePlayerActor _player = new();

            public FakeCombatContext(CombatRelation relation)
            {
                _relation = relation;
            }

            public IWorldActor Player => _player;
            public Transform PlayerTransform => null;
            public IEnumerable<IWorldActor> AllActors => Array.Empty<IWorldActor>();

            public IWorldActor FindActor(string actorId) => null;

            public CombatRelation GetRelation(
                ICombatAffiliationView source,
                ICombatAffiliationView target) => _relation;

            public bool CanTarget(
                ICombatAffiliationView source,
                ICombatAffiliationView target) =>
                source != null
                && target != null
                && target.IsCombatAvailable
                && source.CombatantRuntimeId != target.CombatantRuntimeId
                && _relation == CombatRelation.Hostile;

            public bool CanDamage(
                ICombatAffiliationView source,
                ICombatAffiliationView target,
                CombatTargetPolicy policy = CombatTargetPolicy.Hostile)
            {
                bool isSelf = source?.CombatantRuntimeId == target?.CombatantRuntimeId;
                return CombatFactionRules.MatchesPolicy(_relation, isSelf, policy);
            }

            public CombatCreditOwner GetCreditOwner(ICombatAffiliationView actor) =>
                actor?.CombatCreditOwner ?? CombatCreditOwner.None;

            public IDisposable OverrideAffiliation(
                ICombatAffiliationView actor,
                CombatFactionSO faction,
                CombatCreditOwner creditOwner) => null;
        }

        private sealed class FakePlayerActor : IWorldActor, ICombatAffiliationView
        {
            public string ActorId => "Test.Player";
            public ActorType ActorType => ActorType.Player | ActorType.Combat;
            public MonsterActorGrade Grade => MonsterActorGrade.Normal;
            public Transform Transform => null;
            public bool IsAlive => true;
            public int CombatantRuntimeId => 1;
            public string CombatFactionId => CombatFactionRules.PlayerPartyId;
            public CombatCreditOwner CombatCreditOwner => CombatCreditOwner.PlayerParty;
            public bool IsCombatAvailable => true;

            public bool TryGetSocket(ActorSocketType socketType, out Transform socket)
            {
                socket = null;
                return false;
            }

            public void LockOn()
            {
            }

            public void UnLockOn()
            {
            }
        }
    }
}
