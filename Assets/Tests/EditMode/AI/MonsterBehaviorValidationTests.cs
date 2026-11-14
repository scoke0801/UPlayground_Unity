using System.IO;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UPlayGround.AI.BehaviorTree;
using UPlayGround.AI.BehaviorTree.Editor;
using UnityEngine;

namespace UPlayGround.AI.Tests
{
    public sealed class MonsterBehaviorValidationTests
    {
        [TestCase(true, true, BTStatus.Success)]
        [TestCase(false, true, BTStatus.Failure)]
        [TestCase(true, false, BTStatus.Failure)]
        public void 지연_후에는_규칙과_그룹의_조건을_다시_확인한다(bool ruleValid, bool groupValid, BTStatus expected)
        {
            var tree = ScriptableObject.CreateInstance<BehaviorTreeAsset>();
            try
            {
                var rule = new MonsterBehaviorRuleJson { name = "Delayed" };
                rule.when.Add(new MonsterBehaviorConditionJson { condition = "BlackboardCompare", key = EnemyBlackboardKeys.EnablePatrol, op = "Equal", value = "true" });
                rule.@do.Add(new MonsterBehaviorActionJson { action = "Wait", duration = 1000f });
                rule.@do.Add(new MonsterBehaviorActionJson { action = "Wait", duration = 0f });
                var group = new List<MonsterBehaviorConditionJson> { new() { condition = "BlackboardCompare", key = EnemyBlackboardKeys.HasAttackSlot, op = "Equal", value = "true" } };
                var method = typeof(MonsterBehaviorTreeJsonImporter).GetMethod("CreateRuleNode", BindingFlags.Static | BindingFlags.NonPublic);
                var root = (BTNode)method.Invoke(null, new object[] { tree, rule, null, new MonsterBehaviorBlackboardJson(), 0, group });
                var blackboard = new Blackboard();
                blackboard.AddEntry(EnemyBlackboardKeys.EnablePatrol, BlackboardValueType.Bool);
                blackboard.AddEntry(EnemyBlackboardKeys.HasAttackSlot, BlackboardValueType.Bool);
                blackboard.SetBool(EnemyBlackboardKeys.EnablePatrol, true);
                blackboard.SetBool(EnemyBlackboardKeys.HasAttackSlot, true);
                root.Initialize(new BehaviorTreeContext(null, blackboard));
                Assert.That(root.Tick(), Is.EqualTo(BTStatus.Running));
                blackboard.SetBool(EnemyBlackboardKeys.EnablePatrol, ruleValid);
                blackboard.SetBool(EnemyBlackboardKeys.HasAttackSlot, groupValid);
                typeof(WaitNode).GetField("_elapsed", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(root.Children[1], 1001f);
                Assert.That(root.Tick(), Is.EqualTo(expected));
            }
            finally
            {
                foreach (var node in tree.Nodes) Object.DestroyImmediate(node);
                Object.DestroyImmediate(tree);
            }
        }

        private static MonsterBehaviorTreeJson CreateData()
        {
            var data = new MonsterBehaviorTreeJson { id = "ValidationTest" };
            data.rules.Add(new MonsterBehaviorRuleJson { name = "Rule" });
            data.rules[0].@do.Add(new MonsterBehaviorActionJson { action = "KeepCurrentState" });
            return data;
        }

        [TestCase("Evade")]
        [TestCase("attack")]
        [TestCase("999")]
        [TestCase("0")]
        public void 잘못된_의도는_거절한다(string intent)
        {
            var data = CreateData();
            data.rules[0].when.Add(new MonsterBehaviorConditionJson { condition = "SelectedIntent", value = intent });
            Assert.Throws<InvalidDataException>(() => MonsterBehaviorTreeJsonImporter.ValidateJson(data));
        }

        [TestCase("Attack")]
        [TestCase("KeepDistance")]
        public void 정상_의도는_통과한다(string intent)
        {
            var data = CreateData();
            data.rules[0].when.Add(new MonsterBehaviorConditionJson { condition = "SelectedIntent", value = intent });
            Assert.DoesNotThrow(() => MonsterBehaviorTreeJsonImporter.ValidateJson(data));
        }

        [TestCase("Attack", 0f)]
        [TestCase(null, 1f)]
        public void 직접_공격의_무시되는_쿨다운은_거절한다(string id, float duration)
        {
            var data = CreateData();
            data.rules[0].@do[0] = new MonsterBehaviorActionJson { action = "ExecuteAttack", cooldownId = id, cooldownDuration = duration };
            Assert.Throws<InvalidDataException>(() => MonsterBehaviorTreeJsonImporter.ValidateJson(data));
        }

        [Test]
        public void 등록되지_않은_쿨다운은_경고하고_등록되면_해제된다()
        {
            var data = CreateData();
            data.rules[0].when.Add(new MonsterBehaviorConditionJson { condition = "CooldownReady", value = "Reaction" });
            Assert.That(MonsterBehaviorTreeJsonImporter.CollectWarnings(data).Count, Is.EqualTo(1));
            data.rules[0].@do[0] = new MonsterBehaviorActionJson { action = "Transition", state = "Dodge", cooldownId = "Reaction", cooldownDuration = 1f };
            Assert.That(MonsterBehaviorTreeJsonImporter.CollectWarnings(data), Is.Empty);
        }

        [Test]
        public void 무조건_대기는_뒤_규칙을_가리지만_실패할_수_있는_액션은_가리지_않는다()
        {
            var data = CreateData();
            data.rules[0].@do[0].action = "Wait";
            data.rules.Add(new MonsterBehaviorRuleJson { name = "Later", priority = -1 });
            Assert.That(MonsterBehaviorTreeJsonImporter.CollectWarnings(data).Count, Is.EqualTo(1));
            data.rules[0].@do[0].action = "ExecuteAttack";
            Assert.That(MonsterBehaviorTreeJsonImporter.CollectWarnings(data), Is.Empty);
        }

        [Test]
        public void 현행_Rules_전체가_오류_없이_검증된다()
        {
            var paths = Directory.GetFiles("Assets/10.Datas/AI/BehaviorTree/SourceJson", "*.json", SearchOption.AllDirectories);
            Assert.That(paths.Length, Is.GreaterThanOrEqualTo(27));
            foreach (string path in paths)
            {
                var data = JsonUtility.FromJson<MonsterBehaviorTreeJson>(File.ReadAllText(path));
                Assert.DoesNotThrow(() => MonsterBehaviorTreeJsonImporter.ValidateJson(data), path);
            }
        }
    }
}
