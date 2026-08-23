using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Party;
using UPlayGround.Data.Stat;

namespace UPlayGround.Ability.Tests
{
    public sealed class CharacterSkillProgressionServiceTests
    {
        private const string SkillTreeRoot =
            "Assets/10.Datas/Party/SkillTree";
        private const string RaonTreePath =
            SkillTreeRoot + "/CharacterSkillTree_Raon.asset";
        private static readonly CharacterActorType[] PlayableCharacters =
        {
            CharacterActorType.Raon,
            CharacterActorType.Hwarin,
            CharacterActorType.Reine,
            CharacterActorType.Lian,
            CharacterActorType.SeolA,
            CharacterActorType.Sera,
            CharacterActorType.YeonHoa,
            CharacterActorType.Yura,
            CharacterActorType.MyoRyeong,
            CharacterActorType.Myomyo,
            CharacterActorType.Lili,
        };
        private static readonly Dictionary<CharacterActorType, string>
            AbilityPrefixByCharacter = new()
            {
                [CharacterActorType.Raon] = "Player.Katana",
                [CharacterActorType.Hwarin] = "Player.DoubleAxe",
                [CharacterActorType.Reine] = "Player.Default",
                [CharacterActorType.Lian] = "Player.Whip",
                [CharacterActorType.SeolA] = "Player.Bow",
                [CharacterActorType.Sera] = "Player.GreatSword",
                [CharacterActorType.YeonHoa] = "Player.Default",
                [CharacterActorType.Yura] = "Player.DualBlade",
                [CharacterActorType.MyoRyeong] = "Player.SwordShield",
                [CharacterActorType.Myomyo] = "Player.Default",
                [CharacterActorType.Lili] = "Player.GreatSword",
            };
        private readonly List<Object> _objects = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _objects.Count; i++)
                Object.DestroyImmediate(_objects[i]);
            _objects.Clear();
        }

        [Test]
        public void TotalPointsAtLevel_레벨당_한_포인트만_누적한다()
        {
            var rule = new SkillPointRule();

            Assert.That(rule.TotalPointsAtLevel(1), Is.EqualTo(0));
            Assert.That(rule.TotalPointsAtLevel(5), Is.EqualTo(4));
            Assert.That(rule.TotalPointsAtLevel(10), Is.EqualTo(9));
        }

        [Test]
        public void TryTakeNode_선행과_비용을_결정적으로_검사한다()
        {
            CharacterSkillTreeSO tree = CreateTree();
            int level = 10;
            var service = CreateService(tree, () => level);

            Assert.That(
                service.CanTakeNode(
                    CharacterActorType.Raon,
                    "child",
                    out SkillNodeBlockReason reason),
                Is.False);
            Assert.That(reason, Is.EqualTo(SkillNodeBlockReason.MissingPrerequisite));

            Assert.That(service.TryTakeNode(CharacterActorType.Raon, "root"), Is.True);
            Assert.That(service.TryTakeNode(CharacterActorType.Raon, "child"), Is.True);
            Assert.That(service.GetNodeRank(CharacterActorType.Raon, "child"), Is.EqualTo(1));
            Assert.That(service.GetAvailablePoints(CharacterActorType.Raon), Is.EqualTo(6));
        }

        [Test]
        public void ImportStates_소급_지급은_한번만_적용한다()
        {
            CharacterSkillTreeSO tree = CreateTree();
            int level = 10;
            var source = new CharacterSkillProgressState
            {
                characterType = CharacterActorType.Raon,
                grantedUpToLevel = 5,
                totalPoints = 4,
                spentPoints = 0,
            };
            var service = CreateService(tree, () => level);

            service.ImportStates(new[] { source });
            Assert.That(service.GetAvailablePoints(CharacterActorType.Raon), Is.EqualTo(9));

            List<CharacterSkillProgressState> saved = service.ExportStates();
            service.ImportStates(saved);
            Assert.That(service.GetAvailablePoints(CharacterActorType.Raon), Is.EqualTo(9));
        }

        [Test]
        public void Respec_다른_포인트를_잃지_않고_노드만_초기화한다()
        {
            CharacterSkillTreeSO tree = CreateTree();
            var service = CreateService(tree, () => 10);
            service.GrantBonusPoints(CharacterActorType.Raon, 3);
            service.TryTakeNode(CharacterActorType.Raon, "root");

            Assert.That(service.TryRespec(CharacterActorType.Raon), Is.True);
            Assert.That(service.GetNodeRank(CharacterActorType.Raon, "root"), Is.Zero);
            Assert.That(service.GetAvailablePoints(CharacterActorType.Raon), Is.EqualTo(12));
        }

        [Test]
        public void 노드_취득과_리스펙은_메뉴_위치와_무관하게_허용한다()
        {
            CharacterSkillTreeSO tree = CreateTree();
            var service = CreateService(tree, () => 10);

            Assert.That(service.TryTakeNode(CharacterActorType.Raon, "root"), Is.True);
            Assert.That(service.TryRespec(CharacterActorType.Raon), Is.True);
        }

        [Test]
        public void Ability_해금과_스칼라는_취득_노드에서만_적용된다()
        {
            const string abilityId = "Player.Raon.Test";
            CharacterSkillTreeSO tree = CreateTree();
            tree.nodes[0].effects.Add(new AbilityUnlockEffect { abilityId = abilityId });
            tree.nodes[0].effects.Add(new AbilityScalarEffect
            {
                abilityId = abilityId,
                kind = AbilityScalarKind.Damage,
                operation = ModifierType.Percent,
                valuePerRank = 0.2f,
            });
            var service = CreateService(tree, () => 10);

            Assert.That(service.IsAbilityUnlocked(CharacterActorType.Raon, abilityId), Is.False);
            Assert.That(
                service.GetAbilityScalar(
                    CharacterActorType.Raon,
                    abilityId,
                    AbilityScalarKind.Damage),
                Is.EqualTo(1f));

            Assert.That(service.TryTakeNode(CharacterActorType.Raon, "root"), Is.True);
            Assert.That(service.IsAbilityUnlocked(CharacterActorType.Raon, abilityId), Is.True);
            Assert.That(
                service.GetAbilityScalar(
                    CharacterActorType.Raon,
                    abilityId,
                    AbilityScalarKind.Damage),
                Is.EqualTo(1.2f).Within(0.0001f));
        }

        [Test]
        public void DodgeCooldown_취득_랭크만큼_감소하고_최솟값을_보장한다()
        {
            CharacterSkillTreeSO tree = CreateTree();
            tree.nodes[0].effects.Add(new DodgeCooldownEffect
            {
                reductionPerRank = 0.6f,
            });
            var service = CreateService(tree, () => 10);

            Assert.That(
                service.GetDodgeCooldownMultiplier(CharacterActorType.Raon),
                Is.EqualTo(1f));

            service.TryTakeNode(CharacterActorType.Raon, "root");
            Assert.That(
                service.GetDodgeCooldownMultiplier(CharacterActorType.Raon),
                Is.EqualTo(0.4f).Within(0.0001f));

            service.TryTakeNode(CharacterActorType.Raon, "root");
            Assert.That(
                service.GetDodgeCooldownMultiplier(CharacterActorType.Raon),
                Is.EqualTo(0.2f).Within(0.0001f));
        }

        [Test]
        public void RaonTree_V1은_세_분기와_실제_Ability_매핑을_유지한다()
        {
            CharacterSkillTreeSO tree =
                AssetDatabase.LoadAssetAtPath<CharacterSkillTreeSO>(
                RaonTreePath);

            Assert.That(tree, Is.Not.Null);
            Assert.That(tree.characterType, Is.EqualTo(CharacterActorType.Raon));
            Assert.That(tree.nodes, Has.Count.EqualTo(14));

            int roots = 0;
            int totalCost = 0;
            bool hasDodgeCooldown = false;
            bool hasPassiveCapstone = false;
            var targetAbilityIds = new HashSet<string>();
            for (int i = 0; i < tree.nodes.Count; i++)
            {
                SkillNodeDefinition node = tree.nodes[i];
                if (node.requiredNodeIds == null || node.requiredNodeIds.Count == 0)
                    roots++;
                totalCost += Mathf.Max(1, node.cost) * Mathf.Max(1, node.maxRank);
                for (int j = 0; j < node.effects.Count; j++)
                {
                    switch (node.effects[j])
                    {
                        case AbilityUnlockEffect unlock:
                            targetAbilityIds.Add(unlock.abilityId);
                            break;
                        case AbilityScalarEffect scalar:
                            targetAbilityIds.Add(scalar.abilityId);
                            break;
                        case DodgeCooldownEffect _:
                            hasDodgeCooldown = true;
                            break;
                        case PassiveGrantEffect grant when grant.passive != null:
                            hasPassiveCapstone = true;
                            break;
                    }
                }
            }

            Assert.That(roots, Is.EqualTo(3));
            Assert.That(totalCost, Is.EqualTo(131));
            Assert.That(new SkillPointRule().TotalPointsAtLevel(100), Is.EqualTo(99));
            Assert.That(hasDodgeCooldown, Is.True);
            Assert.That(hasPassiveCapstone, Is.True);
            Assert.That(targetAbilityIds, Is.EquivalentTo(new[]
            {
                "Player.Katana.Light.05",
                "Player.Katana.Light.08",
                "Player.Katana.Heavy.05",
                "Player.Katana.Ability",
                "Player.Katana.Ultimate",
            }));

            HashSet<string> existingAbilityIds = LoadAbilityIds();
            foreach (string abilityId in targetAbilityIds)
                Assert.That(existingAbilityIds.Contains(abilityId), Is.True, abilityId);
        }

        [Test]
        public void 플레이어블_11명_트리는_선택형_세_분기와_실제_Ability를_유지한다()
        {
            string[] treeGuids = AssetDatabase.FindAssets(
                "t:CharacterSkillTreeSO",
                new[] { SkillTreeRoot });
            var trees = new Dictionary<CharacterActorType, CharacterSkillTreeSO>();
            for (int i = 0; i < treeGuids.Length; i++)
            {
                CharacterSkillTreeSO tree =
                    AssetDatabase.LoadAssetAtPath<CharacterSkillTreeSO>(
                        AssetDatabase.GUIDToAssetPath(treeGuids[i]));
                Assert.That(tree, Is.Not.Null);
                Assert.That(
                    trees.TryAdd(tree.characterType, tree),
                    Is.True,
                    $"{tree.characterType} 트리가 중복되었습니다.");
            }

            Assert.That(trees.Count, Is.EqualTo(PlayableCharacters.Length));
            HashSet<string> existingAbilityIds = LoadAbilityIds();
            int levelCapPoints = new SkillPointRule().TotalPointsAtLevel(100);

            for (int i = 0; i < PlayableCharacters.Length; i++)
            {
                CharacterActorType type = PlayableCharacters[i];
                Assert.That(trees.TryGetValue(type, out CharacterSkillTreeSO tree), Is.True, type.ToString());
                Assert.That(tree.nodes.Count, Is.InRange(12, 15), type.ToString());

                int roots = 0;
                int totalCost = 0;
                var nodeIds = new HashSet<string>();
                var prerequisiteIds = new HashSet<string>();
                var targetAbilityIds = new HashSet<string>();
                for (int nodeIndex = 0; nodeIndex < tree.nodes.Count; nodeIndex++)
                {
                    SkillNodeDefinition node = tree.nodes[nodeIndex];
                    Assert.That(node, Is.Not.Null, $"{type}/{nodeIndex}");
                    Assert.That(nodeIds.Add(node.NormalizedId), Is.True, node.NormalizedId);
                    totalCost += Mathf.Max(1, node.cost) * Mathf.Max(1, node.maxRank);
                    if (node.requiredNodeIds == null || node.requiredNodeIds.Count == 0)
                        roots++;
                    for (int effectIndex = 0;
                         effectIndex < (node.effects?.Count ?? 0);
                         effectIndex++)
                    {
                        switch (node.effects[effectIndex])
                        {
                            case AbilityUnlockEffect unlock:
                                targetAbilityIds.Add(unlock.abilityId);
                                break;
                            case AbilityScalarEffect scalar:
                                targetAbilityIds.Add(scalar.abilityId);
                                break;
                        }
                    }
                }

                for (int nodeIndex = 0; nodeIndex < tree.nodes.Count; nodeIndex++)
                {
                    SkillNodeDefinition node = tree.nodes[nodeIndex];
                    for (int requirementIndex = 0;
                         requirementIndex < (node.requiredNodeIds?.Count ?? 0);
                         requirementIndex++)
                    {
                        string requiredId = node.requiredNodeIds[requirementIndex];
                        Assert.That(nodeIds.Contains(requiredId), Is.True, $"{type}/{requiredId}");
                        prerequisiteIds.Add(requiredId);
                    }
                }

                int capstones = 0;
                for (int nodeIndex = 0; nodeIndex < tree.nodes.Count; nodeIndex++)
                {
                    SkillNodeDefinition node = tree.nodes[nodeIndex];
                    if (prerequisiteIds.Contains(node.NormalizedId))
                        continue;
                    capstones++;
                    bool changesPlayStyle = false;
                    for (int effectIndex = 0;
                         effectIndex < (node.effects?.Count ?? 0);
                         effectIndex++)
                        if (node.effects[effectIndex] is AbilityUnlockEffect
                            || node.effects[effectIndex] is PassiveGrantEffect)
                        {
                            changesPlayStyle = true;
                            break;
                        }
                    Assert.That(changesPlayStyle, Is.True, $"{type}/{node.nodeId}");
                }

                Assert.That(roots, Is.EqualTo(3), type.ToString());
                Assert.That(capstones, Is.EqualTo(3), type.ToString());
                Assert.That(totalCost, Is.GreaterThan(levelCapPoints), type.ToString());
                string abilityPrefix = AbilityPrefixByCharacter[type];
                Assert.That(targetAbilityIds.Contains($"{abilityPrefix}.Ability"), Is.True, type.ToString());
                Assert.That(targetAbilityIds.Contains($"{abilityPrefix}.Ultimate"), Is.True, type.ToString());
                foreach (string abilityId in targetAbilityIds)
                    Assert.That(existingAbilityIds.Contains(abilityId), Is.True, $"{type}/{abilityId}");
            }
        }

        [Test]
        public void StatModifiers_같은_연산을_노드_순서와_무관하게_합산한다()
        {
            CharacterSkillTreeSO tree = CreateTree();
            var service = CreateService(tree, () => 10);
            service.TryTakeNode(CharacterActorType.Raon, "root");
            service.TryTakeNode(CharacterActorType.Raon, "child");

            IReadOnlyList<SkillStatModifierEntry> modifiers =
                service.GetStatModifiers(CharacterActorType.Raon);

            Assert.That(modifiers.Count, Is.EqualTo(1));
            Assert.That(modifiers[0].AttributeId, Is.EqualTo(GrowthAttributeCatalog.Health));
            Assert.That(modifiers[0].Operation, Is.EqualTo(AttributeModifierOperation.Add));
            Assert.That(modifiers[0].Value, Is.EqualTo(30f));
        }

        private CharacterSkillProgressionService CreateService(
            CharacterSkillTreeSO tree,
            System.Func<int> level)
        {
            var service = new CharacterSkillProgressionService();
            service.Configure(
                new[] { tree },
                new SkillPointRule(),
                _ => level());
            return service;
        }

        private static HashSet<string> LoadAbilityIds()
        {
            var result = new HashSet<string>();
            string[] guids = AssetDatabase.FindAssets(
                "t:GameplayAbilitySO",
                new[] { "Assets/10.Datas/Ability" });
            for (int i = 0; i < guids.Length; i++)
            {
                var ability = AssetDatabase.LoadAssetAtPath<
                    global::UPlayGround.Data.Ability.GameplayAbilitySO>(
                    AssetDatabase.GUIDToAssetPath(guids[i]));
                if (ability != null && !string.IsNullOrWhiteSpace(ability.abilityId))
                    result.Add(ability.abilityId);
            }
            return result;
        }

        private CharacterSkillTreeSO CreateTree()
        {
            var tree = ScriptableObject.CreateInstance<CharacterSkillTreeSO>();
            _objects.Add(tree);
            tree.characterType = CharacterActorType.Raon;
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
