using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Story;
using UPlayGround.Manager;

namespace UPlayGround.FlowGraph.Tests
{
    /// <summary>안전 인계 전 전투 종료·복원이 접근과 동의로 넘어가지 않는지 검사한다.</summary>
    public sealed class RecruitmentSafetyRecoveryTests
    {
        private const string EncounterId = "test.safety_recovery";
        private const string SafetyFlag = "test.refugees_safe";
        private GameObject _root;
        private RecruitmentEncounterDefinitionSO _definition;
        private IRecruitmentEncounterService _service;
        private RecruitmentEncounterStateStore _state;
        private RuntimeStub _runtime;
        private FlagStub _flags;
        private IRecruitmentEncounterService _previousEncounters;
        private IGlobalFlagService _previousFlags;
        private IDisposable _runtimeLease;
        private FieldInfo _quittingField;
        private bool _wasQuitting;

        [SetUp]
        public void SetUp()
        {
            // 직전 Play Mode 종료 상태가 독립된 EditMode 서비스 조회까지 차단하지 않게 격리한다.
            _quittingField = typeof(Services).Assembly.GetType("UPlayGround.Manager.ManagerLifecycle", true)
                .GetField("ApplicationIsQuitting", BindingFlags.Static | BindingFlags.Public);
            _wasQuitting = (bool)_quittingField.GetValue(null);
            _quittingField.SetValue(null, false);
            Services.TryGet(out _previousEncounters);
            Services.TryGet(out _previousFlags);
            Services.Unregister(_previousEncounters);
            Services.Unregister(_previousFlags);
            _root = new GameObject("RecruitmentSafetyRecoveryTest");
            _root.SetActive(false);
            _definition = ScriptableObject.CreateInstance<RecruitmentEncounterDefinitionSO>();
            SetField(_definition, "_encounterId", EncounterId);
            SetField(_definition, "_recruitCharacter", CharacterActorType.Hwarin);
            SetField(_definition, "_recruitmentRequiredFlagKey", SafetyFlag);
            var storyType = Type.GetType("UPlayGround.Story.StoryManager, Assembly-CSharp", true);
            var story = _root.AddComponent(storyType);
            _service = (IRecruitmentEncounterService)story;
            _state = (RecruitmentEncounterStateStore)GetField(story, "_recruitmentStateStore");
            _flags = new FlagStub();
            Services.Register(_service);
            Services.Register(_flags);
            Assert.That(Svc.Flags, Is.SameAs(_flags));
            _runtime = new RuntimeStub(_definition);
            _runtimeLease = _service.RegisterRuntime(_runtime);
            Assert.That(_runtimeLease, Is.Not.Null);
        }

        [TearDown]
        public void TearDown()
        {
            _runtimeLease?.Dispose();
            Services.Unregister(_service);
            Services.Unregister(_flags);
            if (_root != null) UnityEngine.Object.DestroyImmediate(_root);
            if (_definition != null) UnityEngine.Object.DestroyImmediate(_definition);
            Services.Register(_previousEncounters);
            Services.Register(_previousFlags);
            _quittingField?.SetValue(null, _wasQuitting);
        }

        [Test]
        public void 안전인계전에는_접근준비와_대화증명을_거부한다()
        {
            ResolveCombat();
            Assert.That(_service.TryPrepareDialogue(EncounterId), Is.False);
            Assert.That(_service.IsDialogueTransitionReady(EncounterId), Is.False);
            Assert.That(_service.TryBeginDialogueAttempt(EncounterId, out _), Is.False);
            Assert.That(_runtime.PrepareCount, Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void 출전명단에_있어도_모델준비전에는_교체를_거부한다(bool hasSwapComponent)
        {
            var partyType = Type.GetType("UPlayGround.Manager.PartyManager, Assembly-CSharp", true);
            var party = _root.AddComponent(partyType);
            var roster = (UPlayGround.Core.Party.PartyRosterService<CharacterActorType>)GetField(party, "_rosterService");
            roster.AddToRoster(CharacterActorType.Raon);
            roster.AddToRoster(CharacterActorType.Hwarin);
            roster.AddToBattle(CharacterActorType.Raon, 4);
            roster.AddToBattle(CharacterActorType.Hwarin, 4);
            if (hasSwapComponent)
            {
                var swapType = Type.GetType("UPlayGround.Components.PlayerSwapBehaviour, UPlayGround.Actor", true);
                SetField(party, "_playerSwap", _root.AddComponent(swapType));
            }

            Assert.That(partyType.GetMethod("CanSwapTo").Invoke(party, new object[] { 1 }), Is.False);
            Assert.That(roster.BattleOrder, Is.EqualTo(new[] { CharacterActorType.Raon, CharacterActorType.Hwarin }));
        }

        [Test]
        public void 안전인계후_접근을허용하되_영입시점에_다시확인한다()
        {
            ResolveCombat();
            _flags.SetFlag(SafetyFlag, true);
            Assert.That(_service.TryPrepareDialogue(EncounterId), Is.True);
            Assert.That(_service.IsDialogueTransitionReady(EncounterId), Is.True);
            Assert.That(_runtime.PrepareCount, Is.EqualTo(1));
            Assert.That(_service.TryBeginDialogueAttempt(EncounterId, out var attempt), Is.True);
            using (attempt)
            {
                _service.ConfirmDialogueCompleted(attempt);
                _flags.SetFlag(SafetyFlag, false);
                Assert.That(_service.TryCommitRecruitment(EncounterId, attempt),
                    Is.EqualTo(RecruitmentCommitResult.PrerequisiteIncomplete));
                Assert.That(_service.GetPhase(EncounterId), Is.EqualTo(RecruitmentEncounterPhase.CombatResolved));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void 마지막적_처리와_저장재개는_접근없이_전투종료를_적용한다(bool safe)
        {
            _flags.SetFlag(SafetyFlag, safe);
            Assert.That(_state.TryStartCombat(EncounterId), Is.True);
            _runtime.AppliedPhases.Clear();
            _service.RecordHostileDefeated(EncounterId, "hostile");
            Assert.That(_runtime.AppliedPhases, Is.EqualTo(new[] { RecruitmentEncounterPhase.CombatResolved }));
            _runtime.AppliedPhases.Clear();
            Assert.That(_service.TryStartOrResume(EncounterId), Is.EqualTo(RecruitmentEncounterStartResult.DialoguePending));
            Assert.That(_runtime.AppliedPhases, Is.EqualTo(new[] { RecruitmentEncounterPhase.CombatResolved }));
            Assert.That(_runtime.PrepareCount, Is.Zero);
        }

        [Test]
        public void 저장된_마지막처치로_전투가_해결되어도_대기상태를_적용한다()
        {
            Assert.That(_state.TryStartCombat(EncounterId), Is.True);
            Assert.That(_state.RecordHostileDefeated(EncounterId, "hostile"), Is.True);
            _runtime.AppliedPhases.Clear();
            Assert.That(_service.TryStartOrResume(EncounterId), Is.EqualTo(RecruitmentEncounterStartResult.DialoguePending));
            Assert.That(_runtime.AppliedPhases, Is.EqualTo(new[] { RecruitmentEncounterPhase.CombatResolved }));
            Assert.That(_runtime.PrepareCount, Is.Zero);
        }

        [Test]
        public void 전투전_도입대화에는_전투후_안전사실을_요구하지않는다()
        {
            SetField(_definition, "_combatMode", RecruitmentEncounterCombatMode.HostileRecruitTarget);
            Assert.That(_service.TryStartOrResume(EncounterId), Is.EqualTo(RecruitmentEncounterStartResult.IntroductionPending));
            Assert.That(_service.TryPrepareDialogue(EncounterId), Is.True);
            Assert.That(_service.IsDialogueTransitionReady(EncounterId), Is.True);
        }

        [Test]
        public void 안전조건없는_기존조우는_접근동작을_유지한다()
        {
            SetField(_definition, "_recruitmentRequiredFlagKey", string.Empty);
            ResolveCombat();
            Assert.That(_service.TryPrepareDialogue(EncounterId), Is.True);
            Assert.That(_service.IsDialogueTransitionReady(EncounterId), Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void 앵커복원은_이전접근을_정리하고_Flow호출까지_위치를_유지한다(bool safe)
        {
            ResolveCombat();
            _flags.SetFlag(SafetyFlag, safe);
            var anchorType = Type.GetType("UPlayGround.Gameplay.Encounter.RecruitmentEncounterAnchor, Assembly-CSharp", true);
            var actorType = Type.GetType("UPlayGround.MonsterActor, UPlayGround.Actor", true);
            var participantType = Type.GetType("UPlayGround.Gameplay.Encounter.RecruitmentEncounterParticipant, Assembly-CSharp", true);
            var actorRoot = new GameObject("Ally");
            actorRoot.transform.SetParent(_root.transform);
            var actor = actorRoot.AddComponent(actorType);
            var destination = new GameObject("DialogueAnchor");
            destination.transform.SetParent(_root.transform);
            destination.transform.position = new Vector3(10f, 0f, 10f);
            var anchor = (IRecruitmentEncounterRuntimePort)_root.AddComponent(anchorType);
            SetField(anchor, "_definition", _definition);
            SetField(anchor, "_allyActor", actor);
            SetField(anchor, "_participants", Array.CreateInstance(participantType, 0));
            SetField(anchor, "_placeAllyAtDialogueAnchor", true);
            SetField(anchor, "_dialogueAnchor", destination.transform);
            SetField(anchor, "_dialogueTransitionStarted", true);
            SetField(anchor, "_isDialogueTransitionReady", true);

            Assert.That(anchor.TryApplyPhase(RecruitmentEncounterPhase.CombatResolved), Is.True);
            Assert.That(actorRoot.transform.position, Is.EqualTo(Vector3.zero));
            Assert.That(GetField(anchor, "_dialogueTransitionStarted"), Is.False);
            Assert.That(GetField(anchor, "_isDialogueTransitionReady"), Is.False);
            Assert.That(anchor.TryPrepareDialogue(), Is.EqualTo(safe));
            Assert.That(actorRoot.transform.position,
                Is.EqualTo(safe ? destination.transform.position : Vector3.zero));
            if (!safe)
            {
                _flags.SetFlag(SafetyFlag, true);
                Assert.That(anchor.TryPrepareDialogue(), Is.True);
                Assert.That(actorRoot.transform.position, Is.EqualTo(destination.transform.position));
            }
        }

        private void ResolveCombat()
        {
            Assert.That(_state.TryStartCombat(EncounterId), Is.True);
            Assert.That(_state.TryResolveCombat(EncounterId), Is.True);
        }

        [TestCase(RecruitmentEncounterPhase.Completed)]
        [TestCase(RecruitmentEncounterPhase.Dormant)]
        [TestCase(RecruitmentEncounterPhase.CombatResolved)]
        public void 같은씬_로드는_등록된참가자의_배치단계도_복원한다(RecruitmentEncounterPhase phase)
        {
            ResolveCombat();
            var save = new UPlayGround.Data.Save.GameSaveData();
            save.story.recruitmentEncounters.Add(new RecruitmentEncounterSaveEntry
            {
                encounterId = EncounterId,
                recruitCharacter = CharacterActorType.Hwarin,
                phase = phase
            });
            _runtime.AppliedPhases.Clear();
            _service.GetType().GetMethod("ImportSaveData").Invoke(_service, new object[] { save });
            Assert.That(_service.GetPhase(EncounterId), Is.EqualTo(phase));
            Assert.That(_runtime.AppliedPhases, Is.EqualTo(new[] { phase }));
        }

        [TestCase(RecruitmentEncounterPhase.Dormant, true)]
        [TestCase(RecruitmentEncounterPhase.CombatActive, false)]
        [TestCase(RecruitmentEncounterPhase.CombatResolved, false)]
        [TestCase(RecruitmentEncounterPhase.Completed, false)]
        public void 늦게도착한_등장콜백은_복원된_대기단계에서만_대치를준비한다(
            RecruitmentEncounterPhase phase, bool expectedStaged)
        {
            var save = new UPlayGround.Data.Save.GameSaveData();
            save.story.recruitmentEncounters.Add(new RecruitmentEncounterSaveEntry
            {
                encounterId = EncounterId,
                recruitCharacter = CharacterActorType.Hwarin,
                phase = phase
            });
            _service.GetType().GetMethod("ImportSaveData").Invoke(_service, new object[] { save });
            var anchorRoot = new GameObject("LateRevealAnchor");
            try
            {
                var anchorType = Type.GetType("UPlayGround.Gameplay.Encounter.RecruitmentEncounterAnchor, Assembly-CSharp", true);
                var anchor = anchorRoot.AddComponent(anchorType);
                SetField(anchor, "_definition", _definition);
                SetField(anchor, "_stageParticipantsBeforeEntry", true);
                anchorType.GetMethod("StageDormantParticipants", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(anchor, null);
                Assert.That(GetField(anchor, "_participantsStagedBeforeCombat"), Is.EqualTo(expectedStaged));
                Assert.That(_service.GetPhase(EncounterId), Is.EqualTo(phase));
            }
            finally { UnityEngine.Object.DestroyImmediate(anchorRoot); }
        }

        private static object GetField(object target, string name) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);

        private static void SetField(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        private sealed class RuntimeStub : IRecruitmentEncounterRuntimePort
        {
            public RuntimeStub(RecruitmentEncounterDefinitionSO definition) => Definition = definition;
            public string EncounterId => Definition.EncounterId;
            public RecruitmentEncounterDefinitionSO Definition { get; }
            public IWorldActor DialoguePartner => null;
            public IReadOnlyList<string> HostileParticipantIds { get; } = new[] { "hostile" };
            public bool IsDialogueTransitionReady => true;
            public int PrepareCount { get; private set; }
            public List<RecruitmentEncounterPhase> AppliedPhases { get; } = new();
            public bool TryApplyPhase(RecruitmentEncounterPhase phase) { AppliedPhases.Add(phase); return true; }
            public bool TryActivateCombat() => true;
            public bool TryPrepareDialogue() { PrepareCount++; return true; }
        }

        private sealed class FlagStub : IGlobalFlagService
        {
            private readonly Dictionary<string, bool> _values = new();
            public event Action<string, bool> OnFlagChanged;
            public event Action OnFlagsReloaded { add { } remove { } }
            public bool GetFlag(string key) => _values.TryGetValue(key, out bool value) && value;
            public void SetFlag(string key, bool value) { _values[key] = value; OnFlagChanged?.Invoke(key, value); }
        }
    }
}
