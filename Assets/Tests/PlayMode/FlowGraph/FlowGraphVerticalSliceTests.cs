using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UPlayGround.Dialogue;
using UPlayGround.Manager;

namespace UPlayGround.FlowGraph.PlayModeTests
{
    /// <summary>
    /// FlowGraph PlayMode 수직 슬라이스 (설계서 12절):
    /// 진입점 발화 → 다단 노드 시퀀스 완주 → 플래그 서비스 상태 반영 확인.
    /// GameManager 부팅 없이 페이크 IGlobalFlagService를 Services에 직접 등록해 격리 실행한다.
    /// </summary>
    public sealed class FlowGraphVerticalSliceTests
    {
        /// <summary>GlobalFlagManager와 동일 의미(값 변경 시에만 이벤트)의 테스트 더블.</summary>
        private sealed class FakeFlagService : IGlobalFlagService
        {
            private readonly Dictionary<string, bool> _flags = new();

            public event Action<string, bool> OnFlagChanged;

            public bool GetFlag(string key) => _flags.TryGetValue(key, out bool v) && v;

            public void SetFlag(string key, bool value)
            {
                bool changed = !_flags.TryGetValue(key, out bool prev) || prev != value;
                _flags[key] = value;
                if (changed)
                    OnFlagChanged?.Invoke(key, value);
            }
        }

        private sealed class FakeDialogueService : IDialogueService
        {
            public event Action OnDialogueEnd;
            private readonly Dictionary<DialogueGraphSO, Queue<CallbackRequest>> _callbacks = new();

            public void StartDialogue(DialogueGraphSO graph)
            {
                TryStartDialogueTracked(graph, null);
            }

            public IDisposable TryStartDialogueTracked(DialogueGraphSO graph, Action onCompleted)
            {
                if (graph == null)
                    return null;

                if (!_callbacks.TryGetValue(graph, out Queue<CallbackRequest> callbacks))
                {
                    callbacks = new Queue<CallbackRequest>();
                    _callbacks.Add(graph, callbacks);
                }
                var request = new CallbackRequest(onCompleted);
                callbacks.Enqueue(request);
                return request;
            }

            public void Complete(DialogueGraphSO graph)
            {
                if (_callbacks.TryGetValue(graph, out Queue<CallbackRequest> callbacks) && callbacks.Count > 0)
                    callbacks.Dequeue().Complete();
                OnDialogueEnd?.Invoke();
            }

            private sealed class CallbackRequest : IDisposable
            {
                private Action _callback;

                public CallbackRequest(Action callback)
                {
                    _callback = callback;
                }

                public void Complete()
                {
                    Action callback = _callback;
                    _callback = null;
                    callback?.Invoke();
                }

                public void Dispose()
                {
                    _callback = null;
                }
            }
        }

        [Serializable]
        private sealed class DisposableWaitNode : FlowNode
        {
            public bool Disposed { get; private set; }

            public override IEnumerable<FlowPortDef> Ports
            {
                get
                {
                    yield return FlowPortDef.Input();
                    yield return FlowPortDef.Output();
                }
            }

            public override IEnumerator Execute(FlowToken token)
            {
                try
                {
                    while (!token.Context.Cancelled)
                        yield return null;
                }
                finally
                {
                    Disposed = true;
                }
            }
        }

        private FakeFlagService _flags;
        private FakeDialogueService _dialogue;
        private GameObject _runnerObject;

        [SetUp]
        public void SetUp()
        {
            _flags = new FakeFlagService();
            _dialogue = new FakeDialogueService();
            Services.Register(_flags);
            Services.Register(_dialogue);
        }

        [TearDown]
        public void TearDown()
        {
            if (_runnerObject != null)
                UnityEngine.Object.Destroy(_runnerObject);
            Services.Unregister(_dialogue);
            Services.Unregister(_flags);
        }

        private FlowGraphRunner CreateRunner(FlowGraphSO graph)
        {
            _runnerObject = new GameObject("FlowGraphRunner (Test)");
            _runnerObject.SetActive(false);
            var runner = _runnerObject.AddComponent<FlowGraphRunner>();
            runner.SetGraph(graph, registerToManager: false);
            _runnerObject.SetActive(true);
            return runner;
        }

        private static void Connect(FlowGraphSO graph, FlowNode from, string fromPort, FlowNode to)
        {
            graph.connections.Add(new FlowConnection
            {
                fromNodeId = from.id,
                fromPort = fromPort,
                toNodeId = to.id,
                toPort = FlowPort.In,
            });
        }

        [UnityTest]
        public IEnumerator 진입점_발화_후_5노드_시퀀스가_완주되고_플래그가_반영된다()
        {
            // Entry → SetFlag(step1) → Wait(0.05s) → Branch(step1?) → True → SetFlag(done)
            var graph = ScriptableObject.CreateInstance<FlowGraphSO>();
            var entry = new ManualEntryNode { entryId = "start" };
            var setStep = new SetFlagNode { flagKey = "vs_step1", value = true };
            var wait = new WaitTimeNode { seconds = 0.05f };
            var branch = new BranchNode
            {
                condition = new FlagCondition { flagKey = "vs_step1", expectedValue = true },
            };
            var setDone = new SetFlagNode { flagKey = "vs_done", value = true };

            graph.nodes.AddRange(new FlowNode[] { entry, setStep, wait, branch, setDone });
            Connect(graph, entry, FlowPort.Out, setStep);
            Connect(graph, setStep, FlowPort.Out, wait);
            Connect(graph, wait, FlowPort.Out, branch);
            Connect(graph, branch, FlowPort.True, setDone);

            FlowGraphRunner runner = CreateRunner(graph);
            Assert.IsTrue(runner.FireManualEntries("start"), "Manual 진입점 발화 실패");

            float deadline = Time.time + 2f;
            while (!_flags.GetFlag("vs_done") && Time.time < deadline)
                yield return null;

            Assert.IsTrue(_flags.GetFlag("vs_step1"), "1단계 플래그 미반영");
            Assert.IsTrue(_flags.GetFlag("vs_done"), "시퀀스 완주 실패 (Branch True 경로 미도달)");
            Assert.AreEqual(0, runner.ActiveNodeCounts.Count, "완료 후 활성 토큰이 남아 있음");

            UnityEngine.Object.Destroy(graph);
        }

        [UnityTest]
        public IEnumerator OnFlagChanged_진입점이_플래그_변경으로_발화된다()
        {
            var graph = ScriptableObject.CreateInstance<FlowGraphSO>();
            var entry = new OnFlagChangedEntryNode { flagKey = "vs_signal", requiredValue = true };
            var setDone = new SetFlagNode { flagKey = "vs_reacted", value = true };
            graph.nodes.AddRange(new FlowNode[] { entry, setDone });
            Connect(graph, entry, FlowPort.Out, setDone);

            FlowGraphRunner runner = CreateRunner(graph);
            yield return null; // OnEnable에서 진입점 무장 완료 대기

            _flags.SetFlag("vs_signal", true);

            float deadline = Time.time + 2f;
            while (!_flags.GetFlag("vs_reacted") && Time.time < deadline)
                yield return null;

            Assert.IsTrue(_flags.GetFlag("vs_reacted"), "OnFlagChanged 진입점 미발화");

            UnityEngine.Object.Destroy(graph);
        }

        [UnityTest]
        public IEnumerator 블랙보드_변수가_기본값으로_초기화되고_CheckVariable이_분기한다()
        {
            var graph = ScriptableObject.CreateInstance<FlowGraphSO>();
            graph.variables.Add(new FlowVariableDef
            {
                name = "ready",
                type = FlowVariableType.Bool,
                boolValue = true,
            });

            var entry = new ManualEntryNode { entryId = "start" };
            var check = new CheckVariableNode
            {
                variableName = "ready",
                expected = new FlowVariableValue { type = FlowVariableType.Bool, boolValue = true },
            };
            var setDone = new SetFlagNode { flagKey = "vs_var_done", value = true };
            graph.nodes.AddRange(new FlowNode[] { entry, check, setDone });
            Connect(graph, entry, FlowPort.Out, check);
            Connect(graph, check, FlowPort.True, setDone);

            FlowGraphRunner runner = CreateRunner(graph);
            Assert.IsTrue(runner.FireManualEntries("start"));

            float deadline = Time.time + 2f;
            while (!_flags.GetFlag("vs_var_done") && Time.time < deadline)
                yield return null;

            Assert.IsTrue(_flags.GetFlag("vs_var_done"), "블랙보드 기본값 분기 실패 (변수 초기화 미동작)");

            UnityEngine.Object.Destroy(graph);
        }

        [UnityTest]
        public IEnumerator Once_정책_진입점은_두_번째_발화가_차단된다()
        {
            var graph = ScriptableObject.CreateInstance<FlowGraphSO>();
            var entry = new ManualEntryNode { entryId = "once", repeatPolicy = FlowRepeatPolicy.Once };
            graph.nodes.Add(entry);

            FlowGraphRunner runner = CreateRunner(graph);

            Assert.IsTrue(runner.FireManualEntries("once"), "첫 발화가 차단됨");
            yield return null;
            Assert.IsFalse(runner.FireManualEntries("once"), "Once 정책인데 재발화 허용됨");

            UnityEngine.Object.Destroy(graph);
        }

        [UnityTest]
        public IEnumerator 하나의_출력에_연결된_모든_노드가_실행된다()
        {
            var graph = ScriptableObject.CreateInstance<FlowGraphSO>();
            var entry = new ManualEntryNode { entryId = "fanout" };
            var first = new SetFlagNode { flagKey = "vs_fanout_a", value = true };
            var second = new SetFlagNode { flagKey = "vs_fanout_b", value = true };
            graph.nodes.AddRange(new FlowNode[] { entry, first, second });
            Connect(graph, entry, FlowPort.Out, first);
            Connect(graph, entry, FlowPort.Out, second);

            FlowGraphRunner runner = CreateRunner(graph);
            Assert.IsTrue(runner.FireManualEntries("fanout"));
            yield return null;

            Assert.IsTrue(_flags.GetFlag("vs_fanout_a"), "첫 번째 fan-out 대상 미실행");
            Assert.IsTrue(_flags.GetFlag("vs_fanout_b"), "두 번째 fan-out 대상 미실행");
            UnityEngine.Object.Destroy(graph);
        }

        [UnityTest]
        public IEnumerator 대기_없는_엣지_사이클만_중단되고_대기_중인_형제_토큰은_완주한다()
        {
            var graph = ScriptableObject.CreateInstance<FlowGraphSO>();
            var entry = new ManualEntryNode { entryId = "cycle" };
            var wait = new WaitTimeNode { seconds = 0.2f };
            var done = new SetFlagNode { flagKey = "vs_cycle_sibling_done", value = true };
            var loop = new SetFlagNode { flagKey = "vs_cycle", value = true };
            graph.nodes.AddRange(new FlowNode[] { entry, wait, done, loop });
            Connect(graph, entry, FlowPort.Out, wait);
            Connect(graph, wait, FlowPort.Out, done);
            Connect(graph, entry, FlowPort.Out, loop);
            Connect(graph, loop, FlowPort.Out, loop);

            FlowGraphRunner runner = CreateRunner(graph);
            LogAssert.Expect(LogType.Error, new Regex("한 프레임 노드 실행 한도.*초과"));
            Assert.IsTrue(runner.FireManualEntries("cycle"));
            yield return null;

            Assert.IsFalse(_flags.GetFlag("vs_cycle_sibling_done"), "대기 중인 형제 토큰이 너무 일찍 완료됨");
            Assert.AreEqual(1, runner.ActiveContexts.Count, "대기 중인 형제 토큰의 컨텍스트가 조기 제거됨");

            float deadline = Time.time + 2f;
            while (!_flags.GetFlag("vs_cycle_sibling_done") && Time.time < deadline)
                yield return null;

            Assert.IsTrue(_flags.GetFlag("vs_cycle_sibling_done"), "실행 예산 초과가 정상 형제 토큰까지 취소함");
            Assert.AreEqual(0, runner.ActiveNodeCounts.Count, "형제 토큰 완주 후 활성 노드가 남아 있음");
            Assert.AreEqual(0, runner.ActiveContexts.Count, "형제 토큰 완주 후 컨텍스트가 남아 있음");
            UnityEngine.Object.Destroy(graph);
        }

        [UnityTest]
        public IEnumerator 러너_비활성화는_대기_노드_이터레이터를_Dispose한다()
        {
            var graph = ScriptableObject.CreateInstance<FlowGraphSO>();
            var entry = new ManualEntryNode { entryId = "wait" };
            var wait = new DisposableWaitNode();
            graph.nodes.AddRange(new FlowNode[] { entry, wait });
            Connect(graph, entry, FlowPort.Out, wait);

            FlowGraphRunner runner = CreateRunner(graph);
            Assert.IsTrue(runner.FireManualEntries("wait"));
            yield return null;
            _runnerObject.SetActive(false);

            Assert.IsTrue(wait.Disposed, "취소된 노드 IEnumerator의 finally가 실행되지 않음");
            Assert.AreEqual(0, runner.ActiveNodeCounts.Count);
            UnityEngine.Object.Destroy(graph);
        }

        [UnityTest]
        public IEnumerator PlayDialogue는_자신이_시작한_그래프_완료만_기다린다()
        {
            var dialogue = ScriptableObject.CreateInstance<DialogueGraphSO>();
            var unrelated = ScriptableObject.CreateInstance<DialogueGraphSO>();
            var graph = ScriptableObject.CreateInstance<FlowGraphSO>();
            var entry = new ManualEntryNode { entryId = "dialogue" };
            var play = new PlayDialogueNode { dialogue = dialogue };
            var done = new SetFlagNode { flagKey = "vs_dialogue_done", value = true };
            graph.nodes.AddRange(new FlowNode[] { entry, play, done });
            Connect(graph, entry, FlowPort.Out, play);
            Connect(graph, play, FlowPort.Out, done);

            FlowGraphRunner runner = CreateRunner(graph);
            Assert.IsTrue(runner.FireManualEntries("dialogue"));
            yield return null;

            _dialogue.Complete(unrelated);
            yield return null;
            Assert.IsFalse(_flags.GetFlag("vs_dialogue_done"), "관련 없는 대화 종료로 조기 통과함");

            _dialogue.Complete(dialogue);
            yield return null;
            Assert.IsTrue(_flags.GetFlag("vs_dialogue_done"), "대상 대화 완료 후 통과하지 않음");

            UnityEngine.Object.Destroy(graph);
            UnityEngine.Object.Destroy(dialogue);
            UnityEngine.Object.Destroy(unrelated);
        }

        [UnityTest]
        public IEnumerator 복제된_서브그래프의_Once_상태는_그래프별로_격리된다()
        {
            var subGraphA = ScriptableObject.CreateInstance<FlowGraphSO>();
            var subGraphB = ScriptableObject.CreateInstance<FlowGraphSO>();
            var sharedEntryId = "duplicated-node-id";
            var entryA = new ManualEntryNode
            {
                id = sharedEntryId,
                entryId = "sub",
                repeatPolicy = FlowRepeatPolicy.Once
            };
            var entryB = new ManualEntryNode
            {
                id = sharedEntryId,
                entryId = "sub",
                repeatPolicy = FlowRepeatPolicy.Once
            };
            var doneA = new SetFlagNode { flagKey = "vs_subgraph_a", value = true };
            var doneB = new SetFlagNode { flagKey = "vs_subgraph_b", value = true };
            subGraphA.nodes.AddRange(new FlowNode[] { entryA, doneA });
            subGraphB.nodes.AddRange(new FlowNode[] { entryB, doneB });
            Connect(subGraphA, entryA, FlowPort.Out, doneA);
            Connect(subGraphB, entryB, FlowPort.Out, doneB);

            var root = ScriptableObject.CreateInstance<FlowGraphSO>();
            var rootEntry = new ManualEntryNode { entryId = "root" };
            var sequence = new SequenceNode { outputCount = 2 };
            var callA = new SubGraphNode { subGraph = subGraphA, entryId = "sub" };
            var callB = new SubGraphNode { subGraph = subGraphB, entryId = "sub" };
            root.nodes.AddRange(new FlowNode[] { rootEntry, sequence, callA, callB });
            Connect(root, rootEntry, FlowPort.Out, sequence);
            Connect(root, sequence, "1", callA);
            Connect(root, sequence, "2", callB);

            FlowGraphRunner runner = CreateRunner(root);
            Assert.IsTrue(runner.FireManualEntries("root"));
            yield return null;

            Assert.IsTrue(_flags.GetFlag("vs_subgraph_a"), "첫 번째 서브그래프 미실행");
            Assert.IsTrue(_flags.GetFlag("vs_subgraph_b"), "복제된 node id가 첫 그래프 상태와 충돌함");

            UnityEngine.Object.Destroy(root);
            UnityEngine.Object.Destroy(subGraphA);
            UnityEngine.Object.Destroy(subGraphB);
        }
    }
}
