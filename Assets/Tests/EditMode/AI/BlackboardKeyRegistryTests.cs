using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UPlayGround.AI.BehaviorTree;

namespace UPlayGround.AI.Tests
{
    public sealed class BlackboardKeyRegistryTests
    {
        private BlackboardKeyRegistrySO _registry;

        [SetUp]
        public void SetUp()
        {
            _registry = ScriptableObject.CreateInstance<BlackboardKeyRegistrySO>();
            _registry.EditorDefinitions.Add(CreateDefinition(
                "11111111111111111111111111111111",
                "Target.Distance",
                BlackboardValueType.Float,
                new[] { "Target.Range" }));
            _registry.EditorDefinitions.Add(CreateDefinition(
                "22222222222222222222222222222222",
                "Target.Has",
                BlackboardValueType.Bool));
            _registry.RebuildLookup();
            BlackboardKeyRegistry.SetEditorRegistry(_registry);
        }

        [TearDown]
        public void TearDown()
        {
            BlackboardKeyRegistry.SetEditorRegistry(null);
            Object.DestroyImmediate(_registry);
        }

        [Test]
        public void Resolve_StableId_Name_Alias가_같은_Reference를_반환한다()
        {
            Assert.That(_registry.TryResolve(
                "11111111111111111111111111111111",
                out BlackboardKeyReference byId), Is.True);
            Assert.That(_registry.TryResolve("Target.Distance", out BlackboardKeyReference byName), Is.True);
            Assert.That(_registry.TryResolve("Target.Range", out BlackboardKeyReference byAlias), Is.True);

            Assert.That(byId, Is.EqualTo(byName));
            Assert.That(byName, Is.EqualTo(byAlias));
            Assert.That(byAlias.KeyName, Is.EqualTo("Target.Distance"));
        }

        [Test]
        public void RebuildLookup_이전_Handle은_거부한다()
        {
            Assert.That(_registry.TryResolve("Target.Distance", out BlackboardKeyReference reference), Is.True);
            Assert.That(reference.TryResolve(out BlackboardKeyHandle oldHandle, out _), Is.True);

            _registry.RebuildLookup();

            Assert.That(_registry.InternTable.TryGetDefinition(oldHandle, out _), Is.False);
            Assert.That(reference.TryResolve(out BlackboardKeyHandle newHandle, out _), Is.True);
            Assert.That(newHandle, Is.Not.EqualTo(oldHandle));
        }

        [Test]
        public void Blackboard_미등록_Key를_Set하면_자동_생성하지_않는다()
        {
            var blackboard = new Blackboard();

            LogAssert.Expect(
                LogType.Error,
                "등록되지 않았거나 타입이 다른 Blackboard Key는 자동 생성할 수 없습니다: 'Typo.Distance' (Float)");
            blackboard.SetFloat("Typo.Distance", 3f);

            Assert.That(blackboard.Entries, Is.Empty);
        }

        [Test]
        public void Blackboard_Reference와_Handle로_타입안전하게_읽고쓴다()
        {
            Assert.That(_registry.TryResolve("Target.Distance", out BlackboardKeyReference reference), Is.True);
            Assert.That(reference.TryResolve(out BlackboardKeyHandle handle, out _), Is.True);
            var blackboard = new Blackboard();
            blackboard.AddEntry(reference, BlackboardValueType.Float);

            Assert.That(blackboard.TrySetFloat(reference, 4.5f), Is.True);
            Assert.That(blackboard.TryGetFloat(handle, out float value), Is.True);
            Assert.That(value, Is.EqualTo(4.5f));
            Assert.That(blackboard.TrySetInt(reference, 3), Is.False);
        }

        [Test]
        public void Blackboard_런타임_Float는_Registry와_직렬화_Entry를_사용하지_않는다()
        {
            var blackboard = new Blackboard();
            const string key = "Cooldown.SpacingBeat.ReadyTime";

            blackboard.SetRuntimeFloat(key, 12.5f);

            Assert.That(blackboard.TryGetRuntimeFloat(key, out float value), Is.True);
            Assert.That(value, Is.EqualTo(12.5f));
            Assert.That(blackboard.Entries, Is.Empty);
            Assert.That(blackboard.Clone().TryGetRuntimeFloat(key, out _), Is.False);
        }

        private static BlackboardKeyDefinition CreateDefinition(
            string stableId,
            string keyName,
            BlackboardValueType valueType,
            IReadOnlyList<string> aliases = null)
        {
            var definition = new BlackboardKeyDefinition();
            definition.SetEditorData(
                stableId,
                keyName,
                aliases,
                keyName,
                string.Empty,
                valueType,
                BlackboardKeyScope.AgentRuntime,
                BlackboardWritePolicy.ReadWrite,
                false);
            return definition;
        }
    }
}
