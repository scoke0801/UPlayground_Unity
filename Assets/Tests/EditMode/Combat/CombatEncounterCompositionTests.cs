using NUnit.Framework;
using UPlayGround.Combat;
using UPlayGround.Data.Combat;

namespace UPlayGround.Combat.Tests
{
    public sealed class CombatEncounterCompositionTests
    {
        [Test]
        public void BossExit_HoldsThenReturnsToSweep()
        {
            var state = new CombatEncounterState();
            Assert.IsTrue(state.Evaluate(true, false, true, false, 2f, 0.1f));
            Assert.AreEqual(CombatEncounterKind.Duel, state.Current);
            state.Evaluate(true, false, false, false, 2f, 1.9f);
            Assert.AreEqual(CombatEncounterKind.Duel, state.Current);
            state.Evaluate(true, false, false, false, 2f, 0.2f);
            Assert.AreEqual(CombatEncounterKind.Sweep, state.Current);
        }

        [Test]
        public void BossAttack_DefersEntryAndRelease()
        {
            var state = new CombatEncounterState();
            state.Evaluate(true, false, true, true, 2f, 3f);
            Assert.AreEqual(CombatEncounterKind.Explore, state.Current);
            state.Evaluate(true, false, true, false, 2f, 0.1f);
            state.Evaluate(false, false, false, true, 2f, 20f);
            Assert.AreEqual(CombatEncounterKind.Duel, state.Current);
            state.Evaluate(false, false, false, false, 2f, 2.1f);
            Assert.AreEqual(CombatEncounterKind.Explore, state.Current);
        }

        [Test]
        public void ArenaWinsAndResetClearsHold()
        {
            var state = new CombatEncounterState();
            state.Evaluate(false, true, false, false, 2f, 0.1f);
            Assert.AreEqual(CombatEncounterKind.Duel, state.Current);
            state.Reset();
            state.Evaluate(false, false, false, false, 2f, 0.1f);
            Assert.AreEqual(CombatEncounterKind.Explore, state.Current);
        }

        [Test]
        public void Metrics_SeparateModesAndCountKillToNewTargetOnce()
        {
            var metrics = new EncounterCompositionMetrics();
            metrics.RecordAttack(CombatEncounterKind.Sweep, 0f, 1.5f, false, true);
            metrics.RecordAttack(CombatEncounterKind.Sweep, 1f, 1.5f, false, true);
            metrics.RecordKill(CombatEncounterKind.Sweep, 1f, 10);
            metrics.RecordOutgoingHit(CombatEncounterKind.Sweep, 1.2f, 10, 1f);
            metrics.RecordOutgoingHit(CombatEncounterKind.Sweep, 1.8f, 20, 1f);
            metrics.RecordOutgoingHit(CombatEncounterKind.Sweep, 2f, 20, 1f);
            metrics.RecordAttack(CombatEncounterKind.Duel, 2f, 1.5f, true, false);
            Assert.AreEqual(2f, metrics.Records[1].AverageComboLength);
            Assert.AreEqual(0.8f, metrics.Records[1].AverageTargetTransition, 0.001f);
            Assert.AreEqual(1, metrics.Records[1].targetTransitions);
            Assert.AreEqual(1, metrics.Records[2].comboStarts);
        }

        [Test]
        public void Metrics_DeathWindowIncludesBoundaryAndExcludesOldHit()
        {
            var metrics = new EncounterCompositionMetrics();
            metrics.RecordIncoming(CombatEncounterKind.Duel, new IncomingHitCause { time = 0f, offscreen = true });
            metrics.RecordIncoming(CombatEncounterKind.Duel, new IncomingHitCause { time = 1f, telegraphed = true });
            metrics.RecordIncoming(CombatEncounterKind.Duel, new IncomingHitCause { time = 6f, telegraphed = true });
            metrics.RecordDeath(6f);
            Assert.AreEqual(2, metrics.Records[2].deathHitCount);
            Assert.AreEqual(0, metrics.Records[2].deathOffscreenHits);
            Assert.AreEqual(2, metrics.Records[2].lastDeathHitCount);
        }

        [Test]
        public void Metrics_ReportsMissingRatioAndDoesNotInventVoluntaryIntent()
        {
            var metrics = new EncounterCompositionMetrics();
            metrics.RecordAttack(CombatEncounterKind.Duel, 0f, 1f, false, true);
            metrics.EndCombo(true);
            Assert.AreEqual(-1f, metrics.Records[2].CounterDamageRatio);
            Assert.AreEqual(1, metrics.Records[2].defensiveCancels);
            Assert.AreEqual(0, metrics.Records[2].voluntaryThreatStops);
        }
    }
}
