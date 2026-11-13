using NUnit.Framework;
using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Data;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;

namespace UPlayGround.EditorTools.Tests
{
    public sealed class ImbuedEnvironmentHeatTests
    {
        private sealed class Actor : GameActor
        {
            protected override void Awake() { }
            protected override void OnDestroy() { }
        }

        [Test]
        public void WinningOverrideControlsHeatEvenWhenElementDoesNotChange()
        {
            var go = new GameObject("부여 열량 검사");
            try
            {
                var owner = go.AddComponent<Actor>();
                owner.AddElementOverride(1, CombatElement.Fire, 100, 2f);
                Assert.That(owner.ImbuedEnvironmentHeat, Is.EqualTo(2f));
                owner.AddElementOverride(2, CombatElement.Fire, 100, 4f);
                Assert.That(owner.ImbuedEnvironmentHeat, Is.EqualTo(4f));
                owner.AddElementOverride(3, CombatElement.None, 200, 9f);
                Assert.That(owner.ImbuedEnvironmentHeat, Is.EqualTo(4f));
                owner.AddElementOverride(4, CombatElement.Fire, 200);
                Assert.That(owner.ImbuedEnvironmentHeat, Is.Zero);
                owner.RemoveElementOverride(4);
                Assert.That(owner.ElementOverrideSourceId, Is.EqualTo(2ul));
                owner.RemoveElementOverride(2);
                owner.RemoveElementOverride(1);
                Assert.That(owner.ImbuedEnvironmentHeat, Is.Zero);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void ProjectileCopyAndLaterPhaseKeepCapturedBudget()
        {
            var go = new GameObject("공격 확정 검사");
            try
            {
                var owner = go.AddComponent<Actor>();
                owner.AddElementOverride(1, CombatElement.Fire, 100, 4f);
                var phase = new HitPhaseData { receivesImbuedHeat = true, imbuedHeatWeight = 0.25f };
                phase.environment.waterImpulse = 0.3f;
                var info = new AbilityAttackInfo { baseInfo = new AttackInfoBase() };
                info.baseInfo.hitPhases[0] = phase;
                var attack = PlayerAttackController.CreateFromAbility(info, AttackKind.NormalAttack, 0, owner);
                var projectile = PlayerAttackController.Copy(attack);
                owner.RemoveElementOverride(1);
                PlayerAttackController.ApplyHitPhase(projectile, phase, 0);
                Assert.That(projectile.environment.heatImpulse, Is.EqualTo(1f));
                Assert.That(projectile.environment.waterImpulse, Is.EqualTo(0.3f));
                var later = PlayerAttackController.CreateFromAbility(info, AttackKind.NormalAttack, 0, owner);
                Assert.That(later.environment.heatImpulse, Is.Zero);
                phase.environment.heatImpulse = 2f;
                Assert.That(phase.ResolveEnvironment(4f).heatImpulse, Is.EqualTo(2f));
                phase.receivesImbuedHeat = false;
                Assert.That(phase.ResolveEnvironment(40f).heatImpulse, Is.EqualTo(2f));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
    }
}
