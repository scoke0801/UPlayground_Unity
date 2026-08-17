using System.Collections.Generic;
using NUnit.Framework;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Story;

namespace UPlayGround.Content.Tests
{
    public sealed class RecruitmentEncounterStateStoreTests
    {
        [Test]
        public void NormalFlow_PreservesIdempotentDefeatedParticipants()
        {
            var store = new RecruitmentEncounterStateStore();

            Assert.That(store.TryRegisterDefinition(
                "encounter.recruit.test",
                CharacterActorType.Honoka,
                RecruitmentEncounterResetScope.PersistUntilNewGame), Is.True);
            Assert.That(store.TryStartCombat("encounter.recruit.test"), Is.True);
            Assert.That(store.RecordHostileDefeated("encounter.recruit.test", "hostile.01"), Is.True);
            Assert.That(store.RecordHostileDefeated("encounter.recruit.test", "hostile.01"), Is.True);
            Assert.That(store.GetDefeatedHostileIds("encounter.recruit.test"),
                Is.EqualTo(new[] { "hostile.01" }));
            Assert.That(store.TryResolveCombat("encounter.recruit.test"), Is.True);
            Assert.That(store.TryComplete("encounter.recruit.test"), Is.True);
            Assert.That(store.GetPhase("encounter.recruit.test"),
                Is.EqualTo(RecruitmentEncounterPhase.Completed));
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
                CharacterActorType.Honoka,
                RecruitmentEncounterResetScope.PersistUntilNewGame), Is.False);
            Assert.That(store.GetPhase("encounter.recruit.saved"),
                Is.EqualTo(RecruitmentEncounterPhase.CombatActive));
            Assert.That(store.GetDefeatedHostileIds("encounter.recruit.saved"),
                Is.EquivalentTo(new[] { "hostile.a", "hostile.b" }));
        }

        [Test]
        public void ResetOnCycle_ResetsOnlyIncompleteScopedEncounters()
        {
            var store = new RecruitmentEncounterStateStore();
            store.TryRegisterDefinition(
                "cycle.reset",
                CharacterActorType.Hichi,
                RecruitmentEncounterResetScope.ResetOnCycle);
            store.TryRegisterDefinition(
                "story.persist",
                CharacterActorType.Sera,
                RecruitmentEncounterResetScope.PersistUntilNewGame);
            store.TryStartCombat("cycle.reset");
            store.RecordHostileDefeated("cycle.reset", "hostile.01");
            store.TryStartCombat("story.persist");

            IReadOnlyList<string> reset = store.ResetForCycle();

            Assert.That(reset, Is.EqualTo(new[] { "cycle.reset" }));
            Assert.That(store.GetPhase("cycle.reset"),
                Is.EqualTo(RecruitmentEncounterPhase.Dormant));
            Assert.That(store.GetDefeatedHostileIds("cycle.reset"), Is.Empty);
            Assert.That(store.GetPhase("story.persist"),
                Is.EqualTo(RecruitmentEncounterPhase.CombatActive));
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
        public void Import_NormalizesParticipantIds_AndInvalidPhase()
        {
            var store = new RecruitmentEncounterStateStore();
            store.Import(new[]
            {
                new RecruitmentEncounterSaveEntry
                {
                    encounterId = " encounter.recruit.normalized ",
                    recruitCharacter = CharacterActorType.Honoka,
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
