using System.Collections.Generic;
using NUnit.Framework;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Story;

namespace UPlayGround.Content.Tests
{
    public sealed class RecruitmentEncounterStateStoreTests
    {
        [TestCase(
            RecruitmentIncapacitationRule.AnyFatalDamage,
            AttackKind.NormalAttack,
            false,
            true)]
        [TestCase(
            RecruitmentIncapacitationRule.FinishAttack,
            AttackKind.NormalAttack,
            false,
            false)]
        [TestCase(
            RecruitmentIncapacitationRule.FinishAttack,
            AttackKind.FinishAttack,
            true,
            false)]
        [TestCase(
            RecruitmentIncapacitationRule.FinishAttack,
            AttackKind.FinishAttack,
            false,
            true)]
        public void IncapacitationRule_RequiresConfiguredFinishingAction(
            RecruitmentIncapacitationRule rule,
            AttackKind attackKind,
            bool isSpecialBreak,
            bool expected)
        {
            Assert.That(
                RecruitmentIncapacitationRuleEvaluator.IsSatisfied(
                    rule,
                    attackKind,
                    isSpecialBreak),
                Is.EqualTo(expected));
        }

        [Test]
        public void NormalFlow_PreservesIdempotentDefeatedParticipants()
        {
            var store = new RecruitmentEncounterStateStore();

            Assert.That(store.TryRegisterDefinition(
                "encounter.recruit.test",
                CharacterActorType.Hwarin,
                RecruitmentEncounterResetScope.PersistUntilNewGame), Is.True);
            Assert.That(store.TryStartCombat("encounter.recruit.test"), Is.True);
            Assert.That(store.RecordHostileDefeated("encounter.recruit.test", "hostile.01"), Is.True);
            Assert.That(store.RecordHostileDefeated("encounter.recruit.test", "hostile.01"), Is.True);
            Assert.That(store.GetDefeatedHostileIds("encounter.recruit.test"),
                Is.EqualTo(new[] { "hostile.01" }));
            Assert.That(store.TryResolveCombat("encounter.recruit.test"), Is.True);
            Assert.That(store.TryCommitRecruitment("encounter.recruit.test"), Is.True);
            Assert.That(store.GetPhase("encounter.recruit.test"),
                Is.EqualTo(RecruitmentEncounterPhase.RecruitmentCommitted));
            Assert.That(store.TryComplete("encounter.recruit.test"), Is.True);
            Assert.That(store.GetPhase("encounter.recruit.test"),
                Is.EqualTo(RecruitmentEncounterPhase.Completed));
        }

        [Test]
        public void HostileRecruitTargetFlow_PersistsIntroductionBeforeCombat()
        {
            var store = new RecruitmentEncounterStateStore();
            store.TryRegisterDefinition(
                "encounter.recruit.rival",
                CharacterActorType.Hwarin,
                RecruitmentEncounterResetScope.PersistUntilNewGame);

            Assert.That(store.TryBeginIntroduction("encounter.recruit.rival"), Is.True);
            Assert.That(store.GetPhase("encounter.recruit.rival"),
                Is.EqualTo(RecruitmentEncounterPhase.IntroductionPending));
            Assert.That(store.TryBeginIntroduction("encounter.recruit.rival"), Is.False);
            Assert.That(store.TryStartCombat("encounter.recruit.rival"), Is.True);
            Assert.That(store.GetPhase("encounter.recruit.rival"),
                Is.EqualTo(RecruitmentEncounterPhase.CombatActive));
        }

        [Test]
        public void Import_PreservesPartialKills_AndRejectsChangedRecruitCharacter()
        {
            var store = new RecruitmentEncounterStateStore();
            store.Import(new[]
            {
                new RecruitmentEncounterSaveEntry
                {
                    encounterId = "encounter.recruit.saved",
                    recruitCharacter = CharacterActorType.Reine,
                    phase = RecruitmentEncounterPhase.CombatActive,
                    defeatedHostileIds = new List<string> { "hostile.a", "hostile.b" },
                },
            });

            Assert.That(store.TryRegisterDefinition(
                "encounter.recruit.saved",
                CharacterActorType.Hwarin,
                RecruitmentEncounterResetScope.PersistUntilNewGame), Is.False);
            Assert.That(store.GetPhase("encounter.recruit.saved"),
                Is.EqualTo(RecruitmentEncounterPhase.CombatActive));
            Assert.That(store.GetDefeatedHostileIds("encounter.recruit.saved"),
                Is.EquivalentTo(new[] { "hostile.a", "hostile.b" }));
        }

        [Test]
        public void IllegalTransitions_DoNotCreateOrCompleteUnknownEncounter()
        {
            var store = new RecruitmentEncounterStateStore();

            Assert.That(store.TryStartCombat("unknown"), Is.False);
            Assert.That(store.TryComplete("unknown"), Is.False);
            Assert.That(store.Contains("unknown"), Is.False);
        }

        [Test]
        public void Complete_RequiresCommittedRecruitment_AndRemainsIdempotent()
        {
            var store = new RecruitmentEncounterStateStore();
            store.TryRegisterDefinition(
                "encounter.recruit.staged",
                CharacterActorType.LianLian,
                RecruitmentEncounterResetScope.PersistUntilNewGame);
            store.TryStartCombat("encounter.recruit.staged");
            store.TryResolveCombat("encounter.recruit.staged");

            Assert.That(store.TryComplete("encounter.recruit.staged"), Is.False);
            Assert.That(store.TryCommitRecruitment("encounter.recruit.staged"), Is.True);
            Assert.That(store.TryComplete("encounter.recruit.staged"), Is.True);
            Assert.That(store.TryComplete("encounter.recruit.staged"), Is.True);
            Assert.That(store.IsCompleted("encounter.recruit.staged"), Is.True);
        }

        [Test]
        public void Import_NormalizesParticipantIds_AndInvalidPhase()
        {
            var store = new RecruitmentEncounterStateStore();
            store.Import(new[]
            {
                new RecruitmentEncounterSaveEntry
                {
                    encounterId = " encounter.recruit.normalized ",
                    recruitCharacter = CharacterActorType.Hwarin,
                    phase = (RecruitmentEncounterPhase)999,
                    defeatedHostileIds = new List<string>
                    {
                        " hostile.a ",
                        "hostile.a",
                        " ",
                    },
                },
            });

            Assert.That(store.GetPhase("encounter.recruit.normalized"),
                Is.EqualTo(RecruitmentEncounterPhase.Dormant));
            Assert.That(store.GetDefeatedHostileIds("encounter.recruit.normalized"),
                Is.EqualTo(new[] { "hostile.a" }));
        }
    }
}
