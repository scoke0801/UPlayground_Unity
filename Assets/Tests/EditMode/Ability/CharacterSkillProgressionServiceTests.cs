using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Party;
using UPlayGround.Data.Stat;
using UPlayGround.Cycle;

namespace UPlayGround.Ability.Tests
{
    public sealed class CharacterSkillProgressionServiceTests
    {
        private readonly List<Object> _objects = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _objects.Count; i++)
                Object.DestroyImmediate(_objects[i]);
            _objects.Clear();
        }

        [Test]
        public void TotalPointsAtLevel_레벨업과_5레벨_마일스톤을_누적한다()
        {
            var rule = new SkillPointRule();

            Assert.That(rule.TotalPointsAtLevel(1), Is.EqualTo(0));
            Assert.That(rule.TotalPointsAtLevel(5), Is.EqualTo(5));
            Assert.That(rule.TotalPointsAtLevel(10), Is.EqualTo(11));
        }

        [Test]
        public void TryTakeNode_선행과_비용을_결정적으로_검사한다()
        {
            CharacterSkillTreeSO tree = CreateTree();
            int level = 10;
            var service = CreateService(tree, () => level, () => true);

            Assert.That(
                service.CanTakeNode(
                    CharacterActorType.Bokusei,
                    "child",
                    out SkillNodeBlockReason reason),
                Is.False);
            Assert.That(reason, Is.EqualTo(SkillNodeBlockReason.MissingPrerequisite));

            Assert.That(service.TryTakeNode(CharacterActorType.Bokusei, "root"), Is.True);
            Assert.That(service.TryTakeNode(CharacterActorType.Bokusei, "child"), Is.True);
            Assert.That(service.GetNodeRank(CharacterActorType.Bokusei, "child"), Is.EqualTo(1));
            Assert.That(service.GetAvailablePoints(CharacterActorType.Bokusei), Is.EqualTo(8));
        }

        [Test]
        public void ImportStates_소급_지급은_한번만_적용한다()
        {
            CharacterSkillTreeSO tree = CreateTree();
            int level = 10;
            var source = new CharacterSkillProgressState
            {
                characterType = CharacterActorType.Bokusei,
                grantedUpToLevel = 5,
                totalPoints = 5,
                spentPoints = 0,
            };
            var service = CreateService(tree, () => level, () => true);

            service.ImportStates(new[] { source });
            Assert.That(service.GetAvailablePoints(CharacterActorType.Bokusei), Is.EqualTo(11));

            List<CharacterSkillProgressState> saved = service.ExportStates();
            service.ImportStates(saved);
            Assert.That(service.GetAvailablePoints(CharacterActorType.Bokusei), Is.EqualTo(11));
        }

        [Test]
        public void Respec_다른_포인트를_잃지_않고_노드만_초기화한다()
        {
            CharacterSkillTreeSO tree = CreateTree();
            var service = CreateService(tree, () => 10, () => true);
            service.GrantBonusPoints(CharacterActorType.Bokusei, 3);
            service.TryTakeNode(CharacterActorType.Bokusei, "root");

            Assert.That(service.TryRespec(CharacterActorType.Bokusei), Is.True);
            Assert.That(service.GetNodeRank(CharacterActorType.Bokusei, "root"), Is.Zero);
            Assert.That(service.GetAvailablePoints(CharacterActorType.Bokusei), Is.EqualTo(14));
        }

        [Test]
        public void 노드_취득과_리스펙은_안전_지역에서만_허용한다()
        {
            CharacterSkillTreeSO tree = CreateTree();
            bool safe = false;
            var service = CreateService(tree, () => 10, () => safe);

            Assert.That(service.TryTakeNode(CharacterActorType.Bokusei, "root"), Is.False);
            Assert.That(service.TryRespec(CharacterActorType.Bokusei), Is.False);
            Assert.That(
                service.CanTakeNode(
                    CharacterActorType.Bokusei,
                    "root",
                    out SkillNodeBlockReason previewReason,
                    requireSafeZone: false),
                Is.True);
            Assert.That(previewReason, Is.EqualTo(SkillNodeBlockReason.None));

            safe = true;
            Assert.That(service.TryTakeNode(CharacterActorType.Bokusei, "root"), Is.True);
            Assert.That(service.TryRespec(CharacterActorType.Bokusei), Is.True);
        }

        [Test]
        public void Ability_해금과_스칼라는_취득_노드에서만_적용된다()
        {
            const string abilityId = "Player.Bokusei.Test";
            CharacterSkillTreeSO tree = CreateTree();
            tree.nodes[0].effects.Add(new AbilityUnlockEffect { abilityId = abilityId });
            tree.nodes[0].effects.Add(new AbilityScalarEffect
            {
                abilityId = abilityId,
                kind = AbilityScalarKind.Damage,
                operation = ModifierType.Percent,
                valuePerRank = 0.2f,
            });
            var service = CreateService(tree, () => 10, () => true);

            Assert.That(service.IsAbilityUnlocked(CharacterActorType.Bokusei, abilityId), Is.False);
            Assert.That(
                service.GetAbilityScalar(
                    CharacterActorType.Bokusei,
                    abilityId,
                    AbilityScalarKind.Damage),
                Is.EqualTo(1f));

            Assert.That(service.TryTakeNode(CharacterActorType.Bokusei, "root"), Is.True);
            Assert.That(service.IsAbilityUnlocked(CharacterActorType.Bokusei, abilityId), Is.True);
            Assert.That(
                service.GetAbilityScalar(
                    CharacterActorType.Bokusei,
                    abilityId,
                    AbilityScalarKind.Damage),
                Is.EqualTo(1.2f).Within(0.0001f));
        }

        [Test]
        public void StatModifiers_같은_연산을_노드_순서와_무관하게_합산한다()
        {
            CharacterSkillTreeSO tree = CreateTree();
            var service = CreateService(tree, () => 10, () => true);
            service.TryTakeNode(CharacterActorType.Bokusei, "root");
            service.TryTakeNode(CharacterActorType.Bokusei, "child");

            IReadOnlyList<SkillStatModifierEntry> modifiers =
                service.GetStatModifiers(CharacterActorType.Bokusei);

            Assert.That(modifiers.Count, Is.EqualTo(1));
            Assert.That(modifiers[0].AttributeId, Is.EqualTo(GrowthAttributeCatalog.Health));
            Assert.That(modifiers[0].Operation, Is.EqualTo(AttributeModifierOperation.Add));
            Assert.That(modifiers[0].Value, Is.EqualTo(30f));
        }

        [Test]
        public void BossRecruitment_확률없이_세번째_처치에서_보장한다()
        {
            var service = new BossRecruitmentService();
            var roster = new AssistRosterService();
            var context = new BossDefeatContext("Boss.A", "spawn.a", false, false);

            BossRecruitmentResult first = service.Resolve("Assist.A", context, 3, roster);
            BossRecruitmentResult second = service.Resolve("Assist.A", context, 3, roster);
            BossRecruitmentResult third = service.Resolve("Assist.A", context, 3, roster);

            Assert.That(first.success, Is.False);
            Assert.That(second.success, Is.False);
            Assert.That(third.success, Is.True);
            Assert.That(third.trigger, Is.EqualTo(BossRecruitTrigger.DefeatCount));
            Assert.That(third.defeatCountAfter, Is.EqualTo(3));
        }

        [Test]
        public void BossRecruitment_동시조건은_브레이크_마무리를_우선한다()
        {
            var service = new BossRecruitmentService();
            var roster = new AssistRosterService();
            var context = new BossDefeatContext("Boss.A", "spawn.a", true, true);

            BossRecruitmentResult result =
                service.Resolve("Assist.A", context, 1, roster);

            Assert.That(result.success, Is.True);
            Assert.That(result.trigger, Is.EqualTo(BossRecruitTrigger.BreakFinish));
            Assert.That(result.defeatCountAfter, Is.EqualTo(1));
        }

        private CharacterSkillProgressionService CreateService(
            CharacterSkillTreeSO tree,
            System.Func<int> level,
            System.Func<bool> safe)
        {
            var service = new CharacterSkillProgressionService();
            service.Configure(
                new[] { tree },
                new SkillPointRule(),
                _ => level(),
                safe);
            return service;
        }

        private CharacterSkillTreeSO CreateTree()
        {
            var tree = ScriptableObject.CreateInstance<CharacterSkillTreeSO>();
            _objects.Add(tree);
            tree.characterType = CharacterActorType.Bokusei;
            tree.nodes = new List<SkillNodeDefinition>
            {
                new()
                {
                    nodeId = "root",
                    cost = 1,
                    maxRank = 2,
                    effects = new List<SkillNodeEffect>
                    {
                        new StatDeltaEffect
                        {
                            attributeId = GrowthAttributeCatalog.HealthId,
                            operation = AttributeModifierOperation.Add,
                            valuePerRank = 10f,
                        },
                    },
                },
                new()
                {
                    nodeId = "child",
                    cost = 2,
                    maxRank = 1,
                    requiredNodeIds = new List<string> { "root" },
                    effects = new List<SkillNodeEffect>
                    {
                        new StatDeltaEffect
                        {
                            attributeId = GrowthAttributeCatalog.HealthId,
                            operation = AttributeModifierOperation.Add,
                            valuePerRank = 20f,
                        },
                    },
                },
            };
            return tree;
        }
    }
}
