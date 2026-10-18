using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UPlayGround.Data.EnumType;
using UPlayGround.Dialogue;
using UPlayGround.Manager;

namespace UPlayGround.FlowGraph.PlayModeTests
{
    /// <summary>실제 영입 노드와 파티 명단을 사용해 보유 분기·취소·늦은 콜백을 검증한다.</summary>
    public sealed class RedTreeR04ConsentTests
    {
        private GameObject _root;
        private GameObject _partyRoot;
        private FlowGraphSO _graph;
        private FlowGraphRunner _runner;
        private IPartyService _party;
        private Dialogues _dialogues;
        private DialogueGraphSO _normal;
        private DialogueGraphSO _owned;

        [SetUp]
        public void SetUp()
        {
            Services.Clear();
            _partyRoot = new GameObject("ConsentPartyTest");
            _partyRoot.SetActive(false);
            var type = Type.GetType("UPlayGround.Manager.PartyManager, Assembly-CSharp", true);
            _party = (IPartyService)_partyRoot.AddComponent(type);
            var roster = type.GetField("_rosterService", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_party);
            foreach (var character in new[] { CharacterActorType.Lili, CharacterActorType.SeolA,
                CharacterActorType.Lian, CharacterActorType.MyoRyeong, CharacterActorType.Reine,
                CharacterActorType.Yura, CharacterActorType.Raon, CharacterActorType.Arin,
                CharacterActorType.Myomyo, CharacterActorType.YeonHoa, CharacterActorType.Sera })
                roster.GetType().GetMethod("AddToRoster").Invoke(roster, new object[] { character });
            Services.Register(_party);
            _dialogues = new Dialogues();
            Services.Register(_dialogues);
            _normal = ScriptableObject.CreateInstance<DialogueGraphSO>();
            _owned = ScriptableObject.CreateInstance<DialogueGraphSO>();
            _graph = ScriptableObject.CreateInstance<FlowGraphSO>();
            _graph.nodes.Add(new ManualEntryNode { id = "start", entryId = "start", repeatPolicy = FlowRepeatPolicy.WhileIdle });
            _root = new GameObject("ConsentFlowTest");
            _root.SetActive(false);
            _runner = _root.AddComponent<FlowGraphRunner>();
        }

        [TearDown]
        public void TearDown()
        {
            _root.SetActive(false);
            Services.Unregister(_party);
            Services.Unregister(_dialogues);
            UnityEngine.Object.Destroy(_root);
            UnityEngine.Object.Destroy(_partyRoot);
            UnityEngine.Object.Destroy(_graph);
            UnityEngine.Object.Destroy(_normal);
            UnityEngine.Object.Destroy(_owned);
        }

        private void Start(CharacterActorType character)
        {
            _graph.nodes.Add(new RecruitCharacterByConsentNode { id = "consent", recruitCharacter = character,
                dialogue = _normal, alreadyOwnedDialogue = _owned, requiredFlagKey = "truth", completedFlagKey = "consent" });
            _graph.connections.Add(new FlowConnection { fromNodeId = "start", fromPort = "Out", toNodeId = "consent", toPort = "In" });
            _runner.SetGraph(_graph, false);
            _root.SetActive(true);
            _dialogues.SetFlag("truth", true);
            Assert.IsTrue(_runner.FireManualEntries("start"));
        }

        [UnityTest]
        public IEnumerator 이미보유한_릴리도_동의취소후_정상완료해야_사건이_완료된다()
        {
            yield return CheckOwned(CharacterActorType.Lili);
        }

        [Test]
        public void 모델준비전_교체요청은_거부되고_보유명단은_유지된다()
        {
            var type = _party.GetType();
            var roster = type.GetField("_rosterService", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_party);
            Assert.IsTrue((bool)roster.GetType().GetMethod("SetBattleOrder").Invoke(roster,
                new object[] { new[] { CharacterActorType.Lili, CharacterActorType.SeolA }, 4 }));
            Assert.IsTrue((bool)type.GetMethod("CanSwap").Invoke(_party, null));
            var active = type.GetProperty("ActiveCharacterType").GetValue(_party);
            Assert.IsFalse((bool)type.GetMethod("CanSwapTo").Invoke(_party, new object[] { 1 }));
            Assert.IsFalse((bool)type.GetMethod("RequestSwapTo").Invoke(_party, new object[] { 1 }));
            Assert.AreEqual(active, type.GetProperty("ActiveCharacterType").GetValue(_party));
            Assert.IsTrue(_party.IsCharacterUnlocked(CharacterActorType.Lili));
            Assert.IsTrue(_party.IsCharacterUnlocked(CharacterActorType.SeolA));
        }

        [UnityTest]
        public IEnumerator 이미보유한_설아도_동의취소후_정상완료해야_사건이_완료된다()
        {
            yield return CheckOwned(CharacterActorType.SeolA);
        }

        [UnityTest]
        public IEnumerator 이미보유한_묘묘의_사건동의는_별도다() { yield return CheckOwned(CharacterActorType.Myomyo); }
        [UnityTest]
        public IEnumerator 이미보유한_연화의_사건동의는_별도다() { yield return CheckOwned(CharacterActorType.YeonHoa); }
        [UnityTest]
        public IEnumerator 이미보유한_세라의_사건동의는_별도다() { yield return CheckOwned(CharacterActorType.Sera); }

        [Test]
        public void 지정명단은_다른동료로_인원수를_채워도_통과하지않는다()
        {
            var condition = new PartyCharactersOwnedCondition { characters = new[] { CharacterActorType.Sera, CharacterActorType.Hwarin } };
            Assert.IsFalse(condition.Evaluate(null));
            condition.characters = new[] { CharacterActorType.Myomyo, CharacterActorType.YeonHoa, CharacterActorType.Sera };
            Assert.IsTrue(condition.Evaluate(null));
            condition.characters = Array.Empty<CharacterActorType>();
            Assert.IsFalse(condition.Evaluate(null));
        }

        [Test]
        public void 세라를_직접조작하면_숨겨진대역대신_현재몸을_사용한다()
        {
            var playerRoot = new GameObject("조작 인물 검사");
            playerRoot.SetActive(false); playerRoot.transform.SetParent(_root.transform);
            var player = playerRoot.AddComponent(Type.GetType("UPlayGround.PlayerActor, UPlayGround.Actor", true));
            var swapType = Type.GetType("UPlayGround.Components.PlayerSwapBehaviour, UPlayGround.Actor", true);
            var swap = playerRoot.AddComponent(swapType);
            var modelType = Type.GetType("UPlayGround.Components.CharacterModelData, UPlayGround.Actor", true);
            var activeModel = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Actor/Player/Models/PlayerModel_Sera.prefab").GetComponent(modelType);
            swapType.GetField("_activeModel", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(swap, activeModel);
            _party.GetType().GetField("_player", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_party, player);
            var type = Type.GetType("UPlayGround.Gameplay.Interaction.NpcLifePresentation, Assembly-CSharp", true);
            var presentation = _root.AddComponent(type);
            var model = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/10.Datas/World/ScenarioMaps/LakeOfLifeScenario/RedTree/R06/NPC_Sera.prefab");
            type.GetField("_actor", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(presentation, model.GetComponent(Type.GetType("UPlayGround.NpcActor, UPlayGround.Actor", true)));
            Assert.IsTrue((bool)type.GetProperty("IsActorControlledByPlayer").GetValue(presentation));
            swapType.GetField("_activeModel", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(swap, null);
            Assert.IsFalse((bool)type.GetProperty("IsActorControlledByPlayer").GetValue(presentation));
        }

        private IEnumerator CheckOwned(CharacterActorType character)
        {
            Start(character);
            yield return null;
            yield return null;
            Assert.AreSame(_owned, _dialogues.Selected);
            Assert.IsFalse(_dialogues.GetFlag("consent"));
            _dialogues.Cancel();
            yield return null;
            yield return null;
            Assert.IsFalse(_dialogues.GetFlag("consent"));
            Assert.IsTrue(_party.IsCharacterUnlocked(character));
            Assert.IsTrue(_runner.FireManualEntries("start"));
            yield return null;
            yield return null;
            _dialogues.Complete();
            yield return null;
            yield return null;
            Assert.IsTrue(_dialogues.GetFlag("consent"));
            Assert.IsTrue(_dialogues.GetFlag("truth"));
        }

        [UnityTest]
        public IEnumerator 리안_동의취소후_기록사건을_재개한다() => CheckOwned(CharacterActorType.Lian);

        [UnityTest]
        public IEnumerator 묘령_동의취소후_진실을_보존한다() => CheckOwned(CharacterActorType.MyoRyeong);

        [UnityTest]
        public IEnumerator 레이네_동의취소는_통행과_분리된다() => CheckOwned(CharacterActorType.Reine);

        [UnityTest]
        public IEnumerator 유라_동의취소후_인계사건을_재개한다() => CheckOwned(CharacterActorType.Yura);

        [UnityTest]
        public IEnumerator 이미보유한_미선택아린도_정상동의가_필요하다() => CheckOwned(CharacterActorType.Arin);

        [UnityTest]
        public IEnumerator 이미보유한_미선택라온도_정상동의가_필요하다() => CheckOwned(CharacterActorType.Raon);

        [TestCase(CharacterActorType.Raon, CharacterActorType.Arin)]
        [TestCase(CharacterActorType.Arin, CharacterActorType.Raon)]
        public void 미선택주인공_분기는_현재조작인물에_영향받지않는다(CharacterActorType protagonist, CharacterActorType other)
        {
            _party.GetType().GetField("_storyProtagonistType", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(_party, protagonist);
            Assert.IsTrue(new StoryProtagonistCondition { character = protagonist }.Evaluate(null));
            Assert.IsFalse(new StoryProtagonistCondition { character = other }.Evaluate(null));
            Assert.IsFalse(new StoryProtagonistCondition { character = CharacterActorType.None }.Evaluate(null));
        }

        [UnityTest]
        public IEnumerator 영입서비스_준비실패는_진실을_보존하고_합류를_소비하지_않는다()
        {
            Start(CharacterActorType.Hwarin);
            yield return null;
            yield return null;
            Assert.AreSame(_normal, _dialogues.Selected);
            _dialogues.Complete();
            yield return null;
            yield return null;
            Assert.IsTrue(_dialogues.GetFlag("truth"));
            Assert.IsFalse(_dialogues.GetFlag("consent"));
            Assert.IsFalse(_party.IsCharacterUnlocked(CharacterActorType.Hwarin));
            Assert.IsTrue(_runner.FireManualEntries("start"));
        }

        [UnityTest]
        public IEnumerator 씬종료뒤_늦은정상콜백은_영입사건을_확정하지_않는다()
        {
            Start(CharacterActorType.SeolA);
            yield return null;
            yield return null;
            _root.SetActive(false);
            _dialogues.Complete();
            yield return null;
            Assert.IsFalse(_dialogues.GetFlag("consent"));
            Assert.IsTrue(_dialogues.GetFlag("truth"));
            Assert.AreEqual(1, _dialogues.Disposed);
        }

        private sealed class Dialogues : IDialogueService, IGlobalFlagService
        {
            private readonly Dictionary<string, bool> _flags = new();
            private Action _completed;
            private Action _cancelled;
            public DialogueGraphSO Selected;
            public int Disposed;
            public bool IsDialogueActive { get; private set; }
            public event Action OnDialogueEnd { add { } remove { } }
            public event Action<DialogueChannel> OnDialogueChannelEnd { add { } remove { } }
            public event Action<string, bool> OnFlagChanged { add { } remove { } }
            public event Action OnFlagsReloaded { add { } remove { } }
            public bool GetFlag(string key) => _flags.TryGetValue(key, out bool value) && value;
            public void SetFlag(string key, bool value) => _flags[key] = value;
            public void StartDialogue(DialogueGraphSO graph) => IsDialogueActive = true;
            public IDisposable TryStartDialogueTracked(DialogueGraphSO graph, Action completed,
                IWorldActor partnerOverride = null, Action onCancelled = null, string partnerSpeakerId = null)
            {
                Selected = graph;
                _completed = completed;
                _cancelled = onCancelled;
                IsDialogueActive = true;
                return new Lease(() => { IsDialogueActive = false; Disposed++; });
            }
            public void Complete() { IsDialogueActive = false; _completed?.Invoke(); }
            public void Cancel() { IsDialogueActive = false; _cancelled?.Invoke(); }
        }

        private sealed class Lease : IDisposable
        {
            private Action _dispose;
            public Lease(Action dispose) => _dispose = dispose;
            public void Dispose() { var callback = _dispose; _dispose = null; callback?.Invoke(); }
        }
    }
}

