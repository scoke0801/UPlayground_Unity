using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPlayGround.Combat;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Pet;
using UPlayGround.Data.Stat;
using UPlayGround.Contracts.Ability;
using UPlayGround.Ability.Core;

namespace UPlayGround.Ability.Tests
{
    public sealed class PetSupportAbilityTests
    {
        private GameObject _root;
        private PetSupportTestActor _actor;
        private PetSkillCatalogSO _catalog;
        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("PetSupportTest");
            _actor = _root.AddComponent<PetSupportTestActor>(); _actor.Initialize();
            _catalog = AssetDatabase.LoadAssetAtPath<PetSkillCatalogSO>("Assets/10.Datas/Pet/PetSkills.asset");
            Assert.That(_catalog, Is.Not.Null);
        }
        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_root);
        private GameplayAbilitySO Grant(string skillId, int level)
        {
            var ability = _catalog.Find(skillId).GetLevel(level).ability;
            _actor.Abilities.GrantTemporaryAbilities(new[] { ability }); return ability;
        }
        [TestCase(1, 4f)]
        [TestCase(5, 8f)]
        public void 회복은_실제_최대체력에_비례하고_공용쿨다운을_소비한다(int level, float healing)
        {
            var attributes = _actor.AbilitySystem.Attributes;
            attributes.SetBase(Attributes.Vital.MaxHealth, 100f); attributes.SetBase(Attributes.Vital.Health, 50f);
            var ability = Grant(PetSkillCatalogSO.Heal, level);
            Assert.That(_actor.Abilities.TryPrepareAbility(ability, true, _actor, out var handle, out _), Is.EqualTo(AbilityActivationResult.Success));
            Assert.That(_actor.Abilities.Commit(handle), Is.EqualTo(AbilityActivationResult.Success));
            Assert.That(attributes.GetCurrent(Attributes.Vital.Health), Is.EqualTo(50f + healing).Within(.001));
            Assert.That(_actor.Abilities.Commit(handle), Is.EqualTo(AbilityActivationResult.AlreadyCommitted));
            var other = Grant(PetSkillCatalogSO.AttackBuff, 1);
            Assert.That(_actor.Abilities.TryPrepareAbility(other, true, _actor, out _, out _), Is.EqualTo(AbilityActivationResult.CooldownActive));
        }
        [Test]
        public void 파티지원_쿨다운은_캐릭터_초기화와_snapshot_복원으로_초기화되지_않는다()
        {
            var ability = Grant(PetSkillCatalogSO.AttackBuff, 1);
            Assert.That(_actor.Abilities.TryPrepareAbility(ability, true, _actor, out var handle, out _), Is.EqualTo(AbilityActivationResult.Success));
            Assert.That(_actor.Abilities.Commit(handle), Is.EqualTo(AbilityActivationResult.Success));
            var cooldowns = _actor.AbilitySystem.Runtime.Cooldowns;
            cooldowns.Start("Character.Skill", 10f);
            _actor.Abilities.ClearCharacterCooldowns();
            Assert.That(cooldowns.GetRemaining("Character.Skill"), Is.Zero);
            Assert.That(cooldowns.GetRemaining(PetSkillCatalogSO.CooldownGroup), Is.GreaterThan(29f));
            var characterSave = _actor.Abilities.CaptureAbilitySystemStateForCharacter();
            Assert.That(characterSave.cooldowns.Exists(entry => entry.groupId == PetSkillCatalogSO.CooldownGroup), Is.False);
            _actor.Abilities.RestoreAbilitySystemStateForCharacter(new AbilitySystemSaveData());
            Assert.That(cooldowns.GetRemaining(PetSkillCatalogSO.CooldownGroup), Is.GreaterThan(29f));
        }
        [Test]
        public void 무효대상_확정실패는_효과와_쿨다운을_소비하지_않는다()
        {
            var ability = Grant(PetSkillCatalogSO.AttackBuff, 1);
            Assert.That(_actor.Abilities.TryPrepareAbility(ability, true, _actor, out var handle, out _), Is.EqualTo(AbilityActivationResult.Success));
            _actor.IsLiving = false;
            Assert.That(_actor.Abilities.Commit(handle), Is.EqualTo(AbilityActivationResult.InvalidTarget));
            Assert.That(_actor.AbilitySystem.Runtime.Cooldowns.GetRemaining(PetSkillCatalogSO.CooldownGroup), Is.Zero);
            Assert.That(_actor.Effects.HasActiveEffect(_catalog.Find(PetSkillCatalogSO.AttackBuff).GetLevel(1).Effect), Is.False);
        }
        [TestCase(PetSkillCatalogSO.AttackBuff, "Combat.AttackPower", 1, 104f)]
        [TestCase(PetSkillCatalogSO.AttackBuff, "Combat.AttackPower", 5, 112f)]
        [TestCase(PetSkillCatalogSO.DefenseBuff, "Combat.Defense", 5, 118f)]
        public void 버프는_실제_Attribute에_적용하고_snapshot은_적용레벨을_보존한다(string id, string attribute, int level, float expected)
        {
            var key = new Core.AttributeId(attribute);
            _actor.AbilitySystem.Attributes.SetBase(key, 100f);
            var ability = Grant(id, level);
            Assert.That(_actor.Abilities.TryPrepareAbility(ability, true, _actor, out var handle, out _), Is.EqualTo(AbilityActivationResult.Success));
            Assert.That(_actor.Abilities.Commit(handle), Is.EqualTo(AbilityActivationResult.Success));
            Assert.That(_actor.AbilitySystem.Attributes.GetCurrent(key), Is.EqualTo(expected).Within(.001));
            var saved = new List<Core.ActiveEffectSaveEntry>();
            _actor.Effects.CaptureRuntimeState(saved, false);
            Assert.That(saved.Count, Is.EqualTo(1));
            Assert.That(saved[0].effectId, Does.EndWith("L" + level));
            saved[0].remainingSeconds = 3f;
            _actor.Effects.RemoveAll();
            _actor.Effects.RestoreRuntimeState(saved, effectId => ability.variants[0].targetEffects[0]);
            Assert.That(_actor.AbilitySystem.Attributes.GetCurrent(key), Is.EqualTo(expected).Within(.001));
            var restored = new List<Core.ActiveEffectSaveEntry>(); _actor.Effects.CaptureRuntimeState(restored, false);
            Assert.That(restored[0].remainingSeconds, Is.EqualTo(3f).Within(.01));
        }
    }
    public sealed class PetSupportTestActor : GameActor, IDamageable
    {
        public bool IsLiving = true;
        protected override bool RequiresCombatVisuals => false;
        public void Initialize() { base.Awake(); AbilitySystem.InitializeDefaultAttributes(); }
        public CombatResult ReceiveHit(in HitRequest request) => default;
        public bool IsAlive() => IsLiving;
        public bool CanTakeDamage() => IsLiving;
        public Transform GetTransform() => transform;
        public void LockOn() { }
        public void UnLockOn() { }
        public float GetHealthPercent() => 1f;
        public float GetCurrentHealth() => AbilitySystem.Attributes.GetCurrent(Attributes.Vital.Health);
        public void ApplyHealingEffect(float amount) { }
    }
}
