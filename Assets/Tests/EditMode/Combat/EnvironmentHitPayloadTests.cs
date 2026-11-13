using NUnit.Framework;
using UPlayGround.Components;
using UPlayGround.Data;
using UPlayGround.Data.ReactiveEnvironment;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Combat.Tests
{
    public sealed class EnvironmentHitPayloadTests
    {
        [Test]
        public void EnvironmentQueryAddsOnlyTheEnvironmentLayer()
        {
            int actors=UnityEngine.LayerMask.GetMask("Enemy");
            var hits=CombatHitDetector.IncludeEnvironmentTargets(actors);
            Assert.That(hits.value & actors,Is.EqualTo(actors));
            Assert.That(hits.value & UnityEngine.LayerMask.GetMask("InteractableObject"),Is.Not.Zero);
            Assert.That(hits.value & UnityEngine.LayerMask.GetMask("Player"),Is.Zero);
        }

        [Test]
        public void HitPhaseCloneAndImmutableRequestPreserveElements()
        {
            var phase = new HitPhaseData { environment = new EnvironmentHitPayload { heatImpulse = 0.8f, waterImpulse = 0.2f } };
            var attack = new AttackData();
            PlayerAttackController.ApplyHitPhase(attack, phase, 2);
            var copy = PlayerAttackController.Copy(attack);
            var request = HitRequest.FromAttackData(copy);
            copy.environment = default;
            Assert.That(request.EnvironmentHit.heatImpulse, Is.EqualTo(0.8f));
            Assert.That(request.EnvironmentHit.waterImpulse, Is.EqualTo(0.2f));
            Assert.That(request.ToReactionData().environment.heatImpulse, Is.EqualTo(0.8f));
            var method = typeof(ResidualPlayerCombat).GetMethod("CopyAttackData",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            var residual = (AttackData)method.Invoke(null,new object[] {attack});
            Assert.That(residual.environment.heatImpulse,Is.EqualTo(0.8f));
        }
    }
}
