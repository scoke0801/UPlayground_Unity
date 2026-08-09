using NUnit.Framework;
using System.Collections.Generic;
using UPlayGround.Ability.Core;
using UPlayGround.Data.Ability;

namespace UPlayGround.Ability.Tests
{
    public sealed class GameplayEffectMagnitudeFactoryTests
    {
        [Test]
        public void 기본값은_Fixed이며_기존저작과_동일하게_변환된다()
        {
            var modifier = new GameplayEffectModifierDefinition
            {
                attributeId = "Vital.MaxHealth",
                value = 12.5f,
            };

            Assert.That(
                modifier.magnitudeSource,
                Is.EqualTo(GameplayEffectMagnitudeSource.Fixed),
                "기존 에셋 호환을 위해 기본값은 Fixed여야 한다.");
            Assert.That(
                GameplayEffectMagnitudeFactory.TryBuild(
                    modifier,
                    out IGameplayMagnitudeCalculation magnitude,
                    out string error),
                Is.True,
                error);
            Assert.That(magnitude, Is.TypeOf<FixedMagnitudeCalculation>());
            Assert.That(
                ((FixedMagnitudeCalculation)magnitude).Value,
                Is.EqualTo(12.5f));
        }

        [Test]
        public void AttributeBased는_캡처정의와_계수를_그대로_전달한다()
        {
            var modifier = new GameplayEffectModifierDefinition
            {
                attributeId = "Vital.MaxHealth",
                magnitudeSource = GameplayEffectMagnitudeSource.AttributeBased,
                sourceAttributeId = "Combat.AttackPower",
                captureSource = GameplayEffectCaptureSource.Source,
                capturePolicy = GameplayEffectCapturePolicy.SnapshotOnApply,
                coefficient = 0.3f,
                preAdd = 5f,
                postAdd = -1f,
            };

            Assert.That(
                GameplayEffectMagnitudeFactory.TryBuild(
                    modifier,
                    out IGameplayMagnitudeCalculation magnitude,
                    out string error),
                Is.True,
                error);

            var attributeBased = magnitude as AttributeBasedMagnitudeCalculation;
            Assert.That(attributeBased, Is.Not.Null);
            Assert.That(
                attributeBased.Capture.AttributeId.Value,
                Is.EqualTo("Combat.AttackPower"));
            Assert.That(
                attributeBased.Capture.Source,
                Is.EqualTo(GameplayEffectCaptureSource.Source));
            Assert.That(
                attributeBased.Capture.Policy,
                Is.EqualTo(GameplayEffectCapturePolicy.SnapshotOnApply));
            Assert.That(attributeBased.Coefficient, Is.EqualTo(0.3f));
            Assert.That(attributeBased.PreAdd, Is.EqualTo(5f));
            Assert.That(attributeBased.PostAdd, Is.EqualTo(-1f));
        }

        [Test]
        public void AttributeBased에_캡처Attribute가_없으면_실패한다()
        {
            var modifier = new GameplayEffectModifierDefinition
            {
                attributeId = "Vital.MaxHealth",
                magnitudeSource = GameplayEffectMagnitudeSource.AttributeBased,
                sourceAttributeId = "   ",
            };

            Assert.That(
                GameplayEffectMagnitudeFactory.TryBuild(modifier, out _, out string error),
                Is.False);
            Assert.That(error, Is.Not.Empty);
        }

        [Test]
        public void TargetAttribute의_SnapshotOnCreate는_실패한다()
        {
            var modifier = new GameplayEffectModifierDefinition
            {
                attributeId = "Vital.MaxHealth",
                magnitudeSource = GameplayEffectMagnitudeSource.AttributeBased,
                sourceAttributeId = "Combat.AttackPower",
                captureSource = GameplayEffectCaptureSource.Target,
                capturePolicy = GameplayEffectCapturePolicy.SnapshotOnCreate,
            };

            Assert.That(
                GameplayEffectMagnitudeFactory.TryBuild(
                    modifier,
                    out _,
                    out string error),
                Is.False);
            Assert.That(error, Does.Contain("Target"));
        }

        [Test]
        public void SetByCaller는_키와_기본값정책을_전달하고_빈키는_실패한다()
        {
            var modifier = new GameplayEffectModifierDefinition
            {
                attributeId = "Vital.Health",
                magnitudeSource = GameplayEffectMagnitudeSource.SetByCaller,
                setByCallerKey = "Data.Damage",
                allowMissingSetByCaller = true,
                setByCallerDefaultValue = -3f,
            };

            Assert.That(
                GameplayEffectMagnitudeFactory.TryBuild(
                    modifier,
                    out IGameplayMagnitudeCalculation magnitude,
                    out string error),
                Is.True,
                error);
            var setByCaller = magnitude as SetByCallerMagnitudeCalculation;
            Assert.That(setByCaller, Is.Not.Null);
            Assert.That(setByCaller.Key.Value, Is.EqualTo("Data.Damage"));
            Assert.That(setByCaller.AllowDefault, Is.True);
            Assert.That(setByCaller.DefaultValue, Is.EqualTo(-3f));

            modifier.setByCallerKey = string.Empty;
            Assert.That(
                GameplayEffectMagnitudeFactory.TryBuild(modifier, out _, out _),
                Is.False);
        }

        [Test]
        public void Effect적용옵션은_SetByCaller값을_전달한다()
        {
            var values = new Dictionary<string, float>
            {
                ["Data.Damage"] = 23f,
            };
            var options = new GameplayEffectApplicationOptions(
                GameplayEffectHudVisibility.UseDefinition,
                values,
                3f);

            Assert.That(options.SetByCallerMagnitudes, Is.SameAs(values));
            Assert.That(options.SetByCallerMagnitudes["Data.Damage"], Is.EqualTo(23f));
            Assert.That(options.EffectiveSpecLevel, Is.EqualTo(3f));
            Assert.That(default(GameplayEffectApplicationOptions).EffectiveSpecLevel, Is.EqualTo(1f));
        }

        [Test]
        public void ScalableByLevel은_기준값과_레벨당증가량을_전달한다()
        {
            var modifier = new GameplayEffectModifierDefinition
            {
                attributeId = "Vital.MaxHealth",
                magnitudeSource = GameplayEffectMagnitudeSource.ScalableByLevel,
                value = 10f,
                perLevel = 2f,
            };

            Assert.That(
                GameplayEffectMagnitudeFactory.TryBuild(
                    modifier,
                    out IGameplayMagnitudeCalculation magnitude,
                    out string error),
                Is.True,
                error);
            var scalable = magnitude as ScalableMagnitudeCalculation;
            Assert.That(scalable, Is.Not.Null);
            Assert.That(scalable.BaseValue, Is.EqualTo(10f));
            Assert.That(scalable.PerLevel, Is.EqualTo(2f));
        }
    }
}
