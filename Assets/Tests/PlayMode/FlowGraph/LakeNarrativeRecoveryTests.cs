using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UPlayGround.Data.EnumType;
using UPlayGround.Dialogue;
using UPlayGround.Manager;

namespace UPlayGround.FlowGraph.PlayModeTests
{
    /// <summary>취소·재진입이 완료로 오인되거나 동시에 두 번 실행되지 않는지 검증한다.</summary>
    public sealed class LakeNarrativeRecoveryTests
    {
        private GameObject _root;
        private FlowGraphSO _graph;
        private FlowGraphRunner _runner;
        private FakeServices _services;
        private ManualEntryNode _entry;

        [SetUp]
        public void SetUp()
        {
            Services.Clear();
            _root = new GameObject("NarrativeRecoveryTest");
            _root.SetActive(false);
            _graph = ScriptableObject.CreateInstance<FlowGraphSO>();
            _graph.graphId = Guid.NewGuid().ToString();
            _entry = new ManualEntryNode { entryId = "start", repeatPolicy = FlowRepeatPolicy.WhileIdle };
            _graph.nodes.Add(_entry);
            _runner = _root.AddComponent<FlowGraphRunner>();
            _runner.SetGraph(_graph, registerToManager: false);
            var actorObject = new GameObject("TestPlayer");
            actorObject.transform.SetParent(_root.transform);
            _services = new FakeServices(new FakeActor(actorObject.transform));
            Services.Register(_services);
        }

        [TearDown]
        public void TearDown()
        {
            _root.SetActive(false);
            Services.Unregister(_services);
            UnityEngine.Object.Destroy(_root);
            UnityEngine.Object.Destroy(_graph);
        }

        [UnityTest]
        public IEnumerator 실행중에는_재진입을_거부하고_취소후에는_재시도한다()
        {
            var wait = new WaitTimeNode { seconds = 10f };
            Add(_entry, FlowPort.Out, wait);
            _root.SetActive(true);
            Assert.IsTrue(_runner.FireManualEntries("start"));
            Assert.IsFalse(_runner.FireManualEntries("start"));
            _runner.enabled = false;
            yield return null;
            _runner.enabled = true;
            Assert.IsTrue(_runner.FireManualEntries("start"));
        }

        [UnityTest]
        public IEnumerator 서로다른_진입점도_전투_게이트를_동시에_통과하지_못한다()
        {
            var second = new ManualEntryNode { entryId = "resume", repeatPolicy = FlowRepeatPolicy.Always };
            var gate = new GateNode { policy = FlowRepeatPolicy.WhileIdle };
            var wait = new WaitTimeNode { seconds = .15f };
            _graph.nodes.Add(second);
            Add(_entry, FlowPort.Out, gate);
            _graph.connections.Add(new FlowConnection
            {
                fromNodeId = second.id, fromPort = FlowPort.Out, toNodeId = gate.id, toPort = FlowPort.In
            });
            Add(gate, FlowPort.Out, Marker("passed"));
            Add(gate, FlowPort.Out, wait);
            _root.SetActive(true);
            FlowContext first = null;
            FlowContext duplicate = null;
            _runner.FireManualEntries("start", context => first = context);
            _runner.FireManualEntries("resume", context => duplicate = context);
            Assert.IsTrue(first.TryGet("passed", out bool passed) && passed);
            Assert.IsFalse(duplicate.TryGet("passed", out bool _));
            yield return new WaitForSeconds(.2f);
            FlowContext resumed = null;
            _runner.FireManualEntries("resume", context => resumed = context);
            Assert.IsTrue(resumed.TryGet("passed", out passed) && passed);
        }

        [UnityTest]
        public IEnumerator 공개전_전투홀드는_명시해제와_취소에서_각각_한번만_풀린다()
        {
            var spawn = new SpawnStoryActorNode { actorId = "Player", holdCombatUntilReleased = true };
            var wait = new WaitTimeNode { seconds = .15f };
            var release = new ReleaseStoryCombatNode { actorId = "Player" };
            Add(_entry, FlowPort.Out, spawn);
            Add(spawn, SpawnStoryActorNode.SpawnedPort, wait);
            Add(wait, FlowPort.Out, release);
            _root.SetActive(true);
            var actor = (FakeActor)_services.Player;
            _runner.FireManualEntries("start");
            Assert.AreEqual(1, actor.CombatHolds);
            yield return new WaitForSeconds(.2f);
            Assert.AreEqual(0, actor.CombatHolds);
            Assert.AreEqual(1, actor.Releases);
            _runner.FireManualEntries("start");
            Assert.AreEqual(1, actor.CombatHolds);
            _runner.enabled = false;
            yield return null;
            Assert.AreEqual(0, actor.CombatHolds);
            Assert.AreEqual(2, actor.Releases);
        }

        [UnityTest]
        public IEnumerator 취소한_대화는_정상완료_대신_재시도_출력으로_이어진다()
        {
            var dialogue = ScriptableObject.CreateInstance<DialogueGraphSO>();
            var play = new PlayDialogueNode { dialogue = dialogue };
            Add(_entry, FlowPort.Out, play);
            Add(play, FlowPort.Out, Marker("completed"));
            Add(play, PlayDialogueNode.CancelledPort, Marker("retry"));
            _root.SetActive(true);
            FlowContext context = null;
            Assert.IsTrue(_runner.FireManualEntries("start", value => context = value));
            yield return null;
            yield return null;
            Assert.IsTrue(_services.IsDialogueActive);
            _services.CancelDialogue();
            yield return null;
            yield return null;
            Assert.IsTrue(context.TryGet("retry", out bool retry) && retry);
            Assert.IsFalse(context.TryGet("completed", out bool completed) && completed);
            Assert.AreEqual(1, _services.DisposedRequests);
            UnityEngine.Object.Destroy(dialogue);
        }

        [UnityTest]
        public IEnumerator 재접근은_밖으로_나간후_돌아와야_하고_대화중에는_기다린다()
        {
            var volumeObject = new GameObject("Arrival");
            volumeObject.transform.SetParent(_root.transform);
            volumeObject.AddComponent<BoxCollider>().size = Vector3.one * 2f;
            var volume = volumeObject.AddComponent<FlowGraphTriggerVolume>();
            typeof(FlowGraphTriggerVolume).GetField("_volumeId",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(volume, "return");
            var wait = new WaitPlayerInVolumeNode { volumeId = "return", requireExitBeforeArrival = true, pollSeconds = .1f };
            Add(_entry, FlowPort.Out, wait);
            Add(wait, FlowPort.Out, Marker("arrived"));
            _root.SetActive(true);
            FlowContext context = null;
            _runner.FireManualEntries("start", value => context = value);
            yield return new WaitForSeconds(.15f);
            Assert.IsFalse(context.TryGet("arrived", out bool _));
            _services.PlayerTransform.position = Vector3.right * 3f;
            yield return new WaitForSeconds(.15f);
            _services.IsDialogueActive = true;
            _services.PlayerTransform.position = Vector3.zero;
            yield return new WaitForSeconds(.15f);
            Assert.IsFalse(context.TryGet("arrived", out bool _));
            _services.IsDialogueActive = false;
            yield return new WaitForSeconds(.15f);
            Assert.IsTrue(context.TryGet("arrived", out bool arrived) && arrived);
        }

        private void Add(FlowNode from, string port, FlowNode to)
        {
            _graph.nodes.Add(to);
            _graph.connections.Add(new FlowConnection
            {
                fromNodeId = from.id, fromPort = port, toNodeId = to.id, toPort = FlowPort.In
            });
        }

        private static SetVariableNode Marker(string name) => new()
        {
            variableName = name,
            value = new FlowVariableValue { type = FlowVariableType.Bool, boolValue = true }
        };

        private sealed class FakeServices : IActorQueryService, IDialogueService
        {
            private Action _cancelled;
            public FakeServices(IWorldActor player) => Player = player;
            public IWorldActor Player { get; }
            public Transform PlayerTransform => Player.Transform;
            public IEnumerable<IWorldActor> AllActors { get { yield return Player; } }
            public IWorldActor FindActor(string id) => Player.ActorId == id ? Player : null;
            public bool IsDialogueActive { get; set; }
            public int DisposedRequests { get; private set; }
            public event Action OnDialogueEnd { add { } remove { } }
            public event Action<DialogueChannel> OnDialogueChannelEnd { add { } remove { } }
            public void StartDialogue(DialogueGraphSO graph) => IsDialogueActive = true;
            public IDisposable TryStartDialogueTracked(DialogueGraphSO graph, Action completed,
                IWorldActor partnerOverride = null, Action onCancelled = null, string partnerSpeakerId = null)
            {
                IsDialogueActive = true;
                _cancelled = onCancelled;
                return new CallbackLease(() => { DisposedRequests++; IsDialogueActive = false; });
            }
            public void CancelDialogue()
            {
                IsDialogueActive = false;
                _cancelled?.Invoke();
            }
        }

        private sealed class FakeActor : IWorldActor, IStoryActorStaging
        {
            public int CombatHolds { get; private set; }
            public int Releases { get; private set; }
            public IDisposable HoldStoryCombat()
            {
                CombatHolds++;
                return new CallbackLease(() => { CombatHolds--; Releases++; });
            }
            public FakeActor(Transform transform) => Transform = transform;
            public string ActorId => "Player";
            public ActorType ActorType => ActorType.Player;
            public MonsterActorGrade Grade => MonsterActorGrade.Normal;
            public Transform Transform { get; }
            public bool IsAlive => true;
            public bool TryGetSocket(ActorSocketType type, out Transform socket) { socket = null; return false; }
            public void LockOn() { }
            public void UnLockOn() { }
        }

        private sealed class CallbackLease : IDisposable
        {
            private Action _release;
            public CallbackLease(Action release) => _release = release;
            public void Dispose()
            {
                Action release = _release;
                _release = null;
                release?.Invoke();
            }
        }
    }
}
