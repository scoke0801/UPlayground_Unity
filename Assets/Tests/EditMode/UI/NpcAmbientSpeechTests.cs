using NUnit.Framework;
using UnityEngine;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Actor.NPC;
using UPlayGround.Data.Quest;
using System.Reflection;

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

        [Test] public void QuestRulesRequireKnownFlagsAndExactQuestStatus()
        {
            var rule = new NpcAmbientSpeechSO.Rule { requiredFlag = "accepted", blockedFlag = "finished",
                questId = "quest", questStatus = QuestStatus.Active, activities = NpcSpeechActivity.Idle };
            Assert.That(rule.Matches(NpcSpeechActivity.Idle, null, null), Is.False);
            Assert.That(rule.Matches(NpcSpeechActivity.Idle, key => key == "accepted", key => QuestStatus.Available), Is.False);
            Assert.That(rule.Matches(NpcSpeechActivity.Working, key => key == "accepted", key => QuestStatus.Active), Is.False);
            Assert.That(rule.Matches(NpcSpeechActivity.Idle, key => null, key => QuestStatus.Active), Is.False);
            Assert.That(rule.Matches(NpcSpeechActivity.Idle, key => key == "accepted", key => QuestStatus.Active), Is.True);
            Assert.That(rule.Matches(NpcSpeechActivity.Idle, key => true, key => QuestStatus.Active), Is.False);
        }

        [Test] public void StoryPhaseOverridesWorkAndChangesImmediatelyAfterReload()
        {
            var npc = ScriptableObject.CreateInstance<NpcActorSO>();
            try
            {
                _settings.workingVoice = 2;
                _settings.bindings = new[] { new NpcAmbientSpeechSO.Binding { npc = npc, voice = 0,
                    requiredFlag = "finished", rules = new[] {
                        new NpcAmbientSpeechSO.Rule { voice = 4, requiredFlag = "finished", activities = NpcSpeechActivity.Any },
                        new NpcAmbientSpeechSO.Rule { voice = 3, requiredFlag = "accepted", blockedFlag = "finished", activities = NpcSpeechActivity.Any }
                    } } };
                bool accepted = true, finished = false;
                bool? Read(string key) => key == "accepted" ? accepted : finished;
                Assert.That(_settings.ResolveVoice(npc, NpcSpeechActivity.Working, Read, null), Is.EqualTo(3));
                finished = true;
                Assert.That(_settings.ResolveVoice(npc, NpcSpeechActivity.Working, Read, null), Is.EqualTo(4));
                accepted = finished = false;
                Assert.That(_settings.ResolveVoice(npc, NpcSpeechActivity.Idle, Read, null), Is.EqualTo(-1));
                Assert.That(_settings.ResolveVoice(npc, NpcSpeechActivity.Idle, null, null), Is.EqualTo(-1));
            }
            finally { Object.DestroyImmediate(npc); }
        }

        [Test] public void WorkAndBumpNeverFallBackToUnrelatedIdleVoice()
        {
            _settings.defaultVoice = 0;
            _settings.workingVoice = 1;
            _settings.bumpedVoice = 2;
            Assert.That(_settings.ResolveVoice(null, NpcSpeechActivity.Idle, null, null), Is.EqualTo(0));
            Assert.That(_settings.ResolveVoice(null, NpcSpeechActivity.Working, null, null), Is.EqualTo(1));
            Assert.That(_settings.ResolveVoice(null, NpcSpeechActivity.Bumped, null, null), Is.EqualTo(2));
            _settings.workingVoice = -1;
            Assert.That(_settings.ResolveVoice(null, NpcSpeechActivity.Working, null, null), Is.EqualTo(-1));
        }

        [Test] public void ExhaustedContextProfileStaysSilentUntilCooldownExpires()
        {
            var voice = Voice(Line("only", "only"));
            var selector = new UINpcSpeechSelector(_settings, 6);
            var history = new UINpcSpeechSelector.History(1);
            Assert.That(selector.TryChoose(voice, history, NpcSpeechActivity.Idle, 0, 0f, out _), Is.True);
            Assert.That(selector.TryChoose(voice, history, NpcSpeechActivity.Idle, 0, 599f, out _), Is.False);
            Assert.That(selector.TryChoose(voice, history, NpcSpeechActivity.Idle, 0, 600f, out _), Is.True);
        }

        [Test] public void BumpCanRepeatAfterShortCooldownBehindLongLivedAmbientHistory()
        {
            _settings.globalRecentLines = 48;
            _settings.reactionCooldown = 8f;
            var selector = new UINpcSpeechSelector(_settings, 7);
            var ambient = Voice(Line("ambient", "ambient"));
            var reaction = Voice(Line("bump", "bump", NpcSpeechActivity.Bumped));
            var history = new UINpcSpeechSelector.History(1);
            Assert.That(selector.TryChoose(ambient, new UINpcSpeechSelector.History(1), NpcSpeechActivity.Idle, 0, 0f, out _), Is.True);
            Assert.That(selector.TryChoose(reaction, history, NpcSpeechActivity.Idle, 0, 1f, out _), Is.False);
            Assert.That(selector.TryChoose(reaction, history, NpcSpeechActivity.Bumped, 0, 1f, out _), Is.True);
            Assert.That(selector.TryChoose(reaction, history, NpcSpeechActivity.Bumped, 0, 8f, out _), Is.False);
            Assert.That(selector.TryChoose(reaction, history, NpcSpeechActivity.Bumped, 0, 9f, out _), Is.True);
        }

        [Test] public void AuthoredProfilesAndRulesHaveValidReferencesAndCompatibleActivities()
        {
            var settings = Resources.Load<NpcAmbientSpeechSO>(NpcAmbientSpeechSO.ResourcePath);
            Assert.That(settings, Is.Not.Null);
            foreach (var index in new[] { settings.workingVoice, settings.merchantVoice, settings.bumpedVoice })
                Assert.That(settings.HasVoice(index), Is.True);
            foreach (var binding in settings.bindings)
                foreach (var rule in binding.rules ?? System.Array.Empty<NpcAmbientSpeechSO.Rule>())
                {
                    Assert.That(settings.HasVoice(rule.voice), Is.True, binding.npc.name);
                    foreach (var line in settings.voices[rule.voice].lines)
                        Assert.That((line.activities & rule.activities) != 0, Is.True, line.id);
                }
        }

        [Test] public void ReceiverAttachedAfterUiRegistrationStillDeliversBumpSequence()
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            var root = new GameObject("LateBoundBumpNpc");
            var uiRoot = new GameObject("LateBoundBumpUi");
            root.SetActive(false);
            uiRoot.SetActive(false);
            root.AddComponent<CapsuleCollider>();
            var data = ScriptableObject.CreateInstance<NpcActorSO>();
            var profile = ScriptableObject.CreateInstance<NpcBumpProfileSO>();
            try
            {
                // UI 테스트 어셈블리에 Actor 의존을 추가하지 않고 실제 등록 순서를 재현한다.
                var presenterType = typeof(UINpcAmbientSpeechPresenter);
                var register = presenterType.GetMethod("Register", flags);
                var actorType = register.GetParameters()[0].ParameterType.Assembly.GetType("UPlayGround.NpcActor", true);
                var actor = root.AddComponent(actorType);
                Assert.That(actor, Is.Not.Null);
                var sockets = actorType.BaseType.GetField("_socketDict", flags);
                sockets.SetValue(actor, System.Activator.CreateInstance(sockets.FieldType));
                var presenter = new UINpcAmbientSpeechPresenter(uiRoot.transform, null, null);
                register.Invoke(presenter, new object[] { actor });
                data.bumpProfile = profile;
                actorType.GetMethod("SetNpcData").Invoke(actor, new object[] { data });
                var receiver = actorType.GetProperty("BumpReceiver").GetValue(actor);
                Assert.That(receiver, Is.Not.Null);
                receiver.GetType().GetField("<BumpSequence>k__BackingField", flags).SetValue(receiver, 1u);
                presenterType.GetMethod("TryPresentReactions", flags).Invoke(presenter, null);
                var residents = (System.Collections.IList)presenterType.GetField("_residents", flags).GetValue(presenter);
                Assert.That(residents[0].GetType().GetField("bumpSequence", flags).GetValue(residents[0]), Is.EqualTo(1u),
                    "등록 순간에 없던 반응기도 이후 밀침을 전달해야 합니다.");
            }
            finally
            {
                Object.DestroyImmediate(uiRoot);
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(data);
                Object.DestroyImmediate(profile);
            }
        }

        private static NpcAmbientSpeechSO.Voice Voice(params NpcAmbientSpeechSO.Line[] lines) => new() { id = "test", lines = lines };

        [Test] public void HeldLineCannotStartOrContinueSpeaking()
        {
            var line = Line("held", "held");
            line.isHeld = true;
            var voice = Voice(line);
            var selector = new UINpcSpeechSelector(_settings, 8);
            Assert.That(selector.TryChoose(voice, new UINpcSpeechSelector.History(1), NpcSpeechActivity.Idle,
                NpcSpeechPeriod.Day, 0f, out _), Is.False);
            Assert.That(line.CanFinishSpeaking(NpcSpeechActivity.Idle, NpcSpeechActivity.Walking, NpcSpeechPeriod.Day), Is.False);
        }

        [Test] public void LegacyJsonKeepsLineEnabledUnlessExplicitlyHeld()
        {
            var line = JsonUtility.FromJson<NpcAmbientSpeechSO.Line>("{\"id\":\"legacy\",\"text\":\"앗!\",\"activities\":16,\"periods\":15}");
            Assert.That(line.isHeld, Is.False);
            Assert.That(line.CanSpeak(NpcSpeechActivity.Bumped, NpcSpeechPeriod.Day), Is.True);
        }

        [Test] public void DifferentSpeakersMayShareAnInterjectionWithoutDuplicatingOneVoice()
        {
            var first = Voice(Line("first", "bump")); first.id = "guard"; first.lines[0].text = "이봐!";
            var second = Voice(Line("second", "bump")); second.id = "resident"; second.lines[0].text = "이봐!";
            var validate = typeof(UPlayGround.UI.Editor.NpcAmbientSpeechAuthoring)
                .GetMethod("ValidateLines", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.DoesNotThrow(() => validate.Invoke(null, new object[] { new[] { first, second } }));
            first.lines = new[] { first.lines[0], second.lines[0] };
            Assert.Throws<TargetInvocationException>(() => validate.Invoke(null, new object[] { new[] { first } }));
        }

        [Test] public void UnspecifiedStandingResidentsDoNotBroadcastUnmotivatedDaytimeMonologues()
        {
            var settings = Resources.Load<NpcAmbientSpeechSO>(NpcAmbientSpeechSO.ResourcePath);
            foreach (var voice in settings.voices)
            {
                if (voice.id != "everyday" && voice.id != "bright" && voice.id != "practical" && voice.id != "reserved"
                    && voice.id != "gentle" && voice.id != "scholarly" && voice.id != "seasoned" && voice.id != "courtly") continue;
                foreach (var line in voice.lines)
                    Assert.That(line.CanSpeak(NpcSpeechActivity.Idle, NpcSpeechPeriod.Day), Is.False, line.id);
            }
        }

        [Test] public void StartedSentenceCanFinishAcrossWalkingButCannotStartInWrongContext()
        {
            var line = Line("idle", "idle", NpcSpeechActivity.Idle, NpcSpeechPeriod.Day);
            Assert.That(line.CanSpeak(NpcSpeechActivity.Walking, NpcSpeechPeriod.Day), Is.False);
            Assert.That(line.CanFinishSpeaking(NpcSpeechActivity.Idle, NpcSpeechActivity.Walking, NpcSpeechPeriod.Day), Is.True);
            Assert.That(line.CanFinishSpeaking(NpcSpeechActivity.Idle, NpcSpeechActivity.Working, NpcSpeechPeriod.Day), Is.False);
            Assert.That(line.CanFinishSpeaking(NpcSpeechActivity.Idle, NpcSpeechActivity.Resting, NpcSpeechPeriod.Day), Is.False);
            Assert.That(line.CanFinishSpeaking(NpcSpeechActivity.Idle, NpcSpeechActivity.Walking, NpcSpeechPeriod.Night), Is.False);
            var walking = Line("walk", "walk", NpcSpeechActivity.Walking);
            Assert.That(walking.CanFinishSpeaking(NpcSpeechActivity.Walking, NpcSpeechActivity.Idle, 0), Is.True);
        }

        [TestCase("Assets/10.Datas/Actor/Npc/NPC_Story_Guide.asset", "guide_bumped")]
        [TestCase("Assets/10.Datas/World/ScenarioMaps/LakeOfLifeScenario/RedTree/NPC_RedTree_NPC_Guide.asset", "guide_bumped")]
        [TestCase("Assets/10.Datas/Actor/Npc/NPC_Shop.asset", "shop_bumped")]
        [TestCase("Assets/10.Datas/Actor/Npc/NPC_Mia.asset", "mia_bumped")]
        public void NamedNpcUsesItsOwnCollisionVoice(string path, string expectedVoice)
        {
            var settings = Resources.Load<NpcAmbientSpeechSO>(NpcAmbientSpeechSO.ResourcePath);
            var npc = UnityEditor.AssetDatabase.LoadAssetAtPath<NpcActorSO>(path);
            Assert.That(npc, Is.Not.Null, path);
            int index = settings.ResolveVoice(npc, NpcSpeechActivity.Bumped, key => false, null);
            Assert.That(settings.HasVoice(index), Is.True);
            Assert.That(settings.voices[index].id, Is.EqualTo(expectedVoice));
        }

        [Test] public void InjuredJunReactionRequiresActualRescueAndChangesAfterReunion()
        {
            var settings = Resources.Load<NpcAmbientSpeechSO>(NpcAmbientSpeechSO.ResourcePath);
            var npc = UnityEditor.AssetDatabase.LoadAssetAtPath<NpcActorSO>("Assets/10.Datas/Actor/Npc/NPC_Jun.asset");
            int injured = settings.ResolveVoice(npc, NpcSpeechActivity.Bumped, key => key == "lake.side.jun_rescued", null);
            int reunited = settings.ResolveVoice(npc, NpcSpeechActivity.Bumped, key => true, null);
            int unknown = settings.ResolveVoice(npc, NpcSpeechActivity.Bumped, null, null);
            Assert.That(settings.voices[injured].id, Is.EqualTo("jun_bumped_hurt"));
            Assert.That(settings.voices[reunited].id, Is.EqualTo("jun_bumped"));
            Assert.That(settings.voices[unknown].id, Is.EqualTo("jun_bumped"));
        }

        [Test] public void TemporaryOcclusionPreservesSentenceAndHidesTweenAlpha()
        {
            var settings = Resources.Load<NpcAmbientSpeechSO>(NpcAmbientSpeechSO.ResourcePath);
            var root = Object.Instantiate(settings.bubblePrefab);
            try
            {
                root.SetActive(true);
                var bubble = root.GetComponent<UINpcSpeechBubble>();
                var group = root.GetComponent<CanvasGroup>();
                var label = root.GetComponentsInChildren<TMPro.TMP_Text>(true)[0];
                label.text = "읽던 문장";
                var applyAlpha = typeof(UINpcSpeechBubble).GetMethod("ApplyAlpha", BindingFlags.Instance | BindingFlags.NonPublic);
                bubble.SetVisible(true);
                applyAlpha.Invoke(bubble, new object[] { 0.75f });
                Assert.That(group.alpha, Is.EqualTo(0.75f));
                bubble.SetVisible(false);
                applyAlpha.Invoke(bubble, new object[] { 0.9f });
                Assert.That(group.alpha, Is.Zero, "가림 중 트윈이 진행돼도 벽 너머로 보이면 안 됩니다.");
                Assert.That(root.activeSelf, Is.True, "잠깐 가렸다고 풀에 반환하면 안 됩니다.");
                bubble.SetVisible(true);
                Assert.That(group.alpha, Is.EqualTo(0.9f));
                Assert.That(label.text, Is.EqualTo("읽던 문장"));
            }
            finally { Object.DestroyImmediate(root); }
        }
        private static NpcAmbientSpeechSO.Line Line(string id, string topic,
            NpcSpeechActivity activity = NpcSpeechActivity.Any, NpcSpeechPeriod period = NpcSpeechPeriod.Any)
            => new() { id = id, topic = topic, text = id, activities = activity, periods = period };
    }
}
