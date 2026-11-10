using NUnit.Framework;
using UnityEngine;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Actor.NPC;

namespace UPlayGround.UI.Tests
{
    /// <summary>일상 발화의 상황 필터와 화자·주변 반복 억제 경계를 검증한다.</summary>
    public sealed class NpcAmbientSpeechTests
    {
        private NpcAmbientSpeechSO _settings;

        [SetUp] public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<NpcAmbientSpeechSO>();
            _settings.topicCooldown = 75f;
            _settings.lineCooldown = 600f;
            _settings.globalRecentLines = 1;
        }

        [TearDown] public void TearDown() => Object.DestroyImmediate(_settings);

        [Test] public void ContextMustMatchBothActivityAndKnownPeriod()
        {
            var voice = Voice(Line("dawn", "travel", NpcSpeechActivity.Walking, NpcSpeechPeriod.Dawn));
            var selector = new UINpcSpeechSelector(_settings, 1);
            var history = new UINpcSpeechSelector.History(1);
            Assert.That(selector.TryChoose(voice, history, NpcSpeechActivity.Idle, NpcSpeechPeriod.Dawn, 0f, out _), Is.False);
            Assert.That(selector.TryChoose(voice, history, NpcSpeechActivity.Walking, 0, 0f, out _), Is.False);
            Assert.That(selector.TryChoose(voice, history, NpcSpeechActivity.Walking, NpcSpeechPeriod.Night, 0f, out _), Is.False);
            Assert.That(selector.TryChoose(voice, history, NpcSpeechActivity.Walking, NpcSpeechPeriod.Dawn, 0f, out _), Is.True);
        }

        [Test] public void SameTopicFromAnotherNpcIsSuppressedEvenWithDifferentWords()
        {
            var selector = new UINpcSpeechSelector(_settings, 2);
            var first = Voice(Line("first", "food"));
            var second = Voice(Line("second", "food"));
            Assert.That(selector.TryChoose(first, new UINpcSpeechSelector.History(1), NpcSpeechActivity.Idle, 0, 100f, out _), Is.True);
            var history = new UINpcSpeechSelector.History(1);
            Assert.That(selector.TryChoose(second, history, NpcSpeechActivity.Idle, 0, 110f, out _), Is.False);
            Assert.That(selector.TryChoose(second, history, NpcSpeechActivity.Idle, 0, 176f, out _), Is.True);
        }

        [Test] public void SharedLineHistorySuppressesImmediateRepeatByAnotherNpc()
        {
            var selector = new UINpcSpeechSelector(_settings, 3);
            var voice = Voice(Line("same", "lost"));
            Assert.That(selector.TryChoose(voice, new UINpcSpeechSelector.History(1), NpcSpeechActivity.Idle, 0, 0f, out _), Is.True);
            Assert.That(selector.TryChoose(voice, new UINpcSpeechSelector.History(1), NpcSpeechActivity.Idle, 0, 100f, out _), Is.False);
        }

        [Test] public void SpeakerCooldownSurvivesEvictionFromGlobalRecentHistory()
        {
            var selector = new UINpcSpeechSelector(_settings, 4);
            var first = Voice(Line("first", "one"));
            var second = Voice(Line("second", "two"));
            var history = new UINpcSpeechSelector.History(1);
            Assert.That(selector.TryChoose(first, history, NpcSpeechActivity.Idle, 0, 0f, out _), Is.True);
            Assert.That(selector.TryChoose(second, new UINpcSpeechSelector.History(1), NpcSpeechActivity.Idle, 0, 1f, out _), Is.True);
            Assert.That(selector.TryChoose(first, history, NpcSpeechActivity.Idle, 0, 599f, out _), Is.False);
            Assert.That(selector.TryChoose(first, history, NpcSpeechActivity.Idle, 0, 600f, out _), Is.True);
        }

        [Test] public void UnheardEligibleLinesAreUsedBeforeRepeatingHeardLines()
        {
            _settings.topicCooldown = 0f;
            _settings.lineCooldown = 0f;
            var selector = new UINpcSpeechSelector(_settings, 5);
            var voice = Voice(Line("a", "a"), Line("b", "b"), Line("c", "c"));
            var history = new UINpcSpeechSelector.History(3);
            for (int cycle = 0; cycle < 4; cycle++)
            {
                var heard = new System.Collections.Generic.HashSet<string>();
                for (int i = 0; i < 3; i++)
                {
                    Assert.That(selector.TryChoose(voice, history, NpcSpeechActivity.Idle, 0, cycle * 3 + i, out var line), Is.True);
                    Assert.That(heard.Add(line.id), Is.True, "미사용 후보보다 반복 문장을 먼저 골랐습니다.");
                }
            }
        }

        [Test] public void BindingUsesNpcReferenceAndCanBeInvalidatedAfterAuthoring()
        {
            var npc = ScriptableObject.CreateInstance<NpcActorSO>();
            try
            {
                _settings.bindings = new[] { new NpcAmbientSpeechSO.Binding { npc = npc, voice = 3, requiredFlag = "story.done" } };
                Assert.That(_settings.ResolveVoice(npc), Is.EqualTo(3));
                Assert.That(_settings.GetRequiredFlag(npc), Is.EqualTo("story.done"));
                _settings.bindings[0].voice = 5;
                _settings.InvalidateBindings();
                Assert.That(_settings.ResolveVoice(npc), Is.EqualTo(5));
                Assert.That(_settings.ResolveVoice(null), Is.EqualTo(_settings.defaultVoice));
            }
            finally { Object.DestroyImmediate(npc); }
        }

        private static NpcAmbientSpeechSO.Voice Voice(params NpcAmbientSpeechSO.Line[] lines) => new() { id = "test", lines = lines };
        private static NpcAmbientSpeechSO.Line Line(string id, string topic,
            NpcSpeechActivity activity = NpcSpeechActivity.Any, NpcSpeechPeriod period = NpcSpeechPeriod.Any)
            => new() { id = id, topic = topic, text = id, activities = activity, periods = period };
    }
}
