using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;

namespace UPlayGround.FlowGraph.Tests
{
    /// <summary>FlowGraphSO 직렬화 왕복과 연결 유효성 검증 테스트 (설계서 12절).</summary>
    public sealed class FlowGraphSerializationTests
    {
        private sealed class TestWorldActor : IWorldActor
        {
            public TestWorldActor(Transform transform, ActorType actorType)
            {
                Transform = transform;
                ActorType = actorType;
            }

            public string ActorId => "test";
            public ActorType ActorType { get; }
            public MonsterActorGrade Grade => MonsterActorGrade.Normal;
            public Transform Transform { get; }
            public bool IsAlive => true;
            public bool TryGetSocket(ActorSocketType socketType, out Transform socket)
            {
                socket = null;
                return false;
            }
            public void LockOn() { }
            public void UnLockOn() { }
        }

        private static FlowGraphSO CreateGraph()
        {
            var graph = ScriptableObject.CreateInstance<FlowGraphSO>();
            graph.name = "FLOW_Test";
            return graph;
        }

        private static FlowConnection Connect(FlowNode from, string fromPort, FlowNode to, string toPort = FlowPort.In)
        {
            return new FlowConnection
            {
                fromNodeId = from.id,
                fromPort = fromPort,
                toNodeId = to.id,
                toPort = toPort,
            };
        }

        [Test]
        public void SerializeReference_왕복_후_노드_타입과_필드가_유지된다()
        {
            var graph = CreateGraph();
            try
            {
                var entry = new ManualEntryNode { entryId = "start" };
                var setFlag = new SetFlagNode { flagKey = "test_flag", value = true };
                graph.nodes.Add(entry);
                graph.nodes.Add(setFlag);
                graph.connections.Add(Connect(entry, FlowPort.Out, setFlag));

                var clone = UnityEngine.Object.Instantiate(graph);
                try
                {
                    Assert.AreEqual(2, clone.nodes.Count);
                    Assert.IsInstanceOf<ManualEntryNode>(clone.nodes[0]);
                    var clonedSetFlag = clone.nodes[1] as SetFlagNode;
                    Assert.NotNull(clonedSetFlag);
                    Assert.AreEqual("test_flag", clonedSetFlag.flagKey);
                    Assert.AreEqual(1, clone.connections.Count);
                    Assert.AreEqual(entry.id, clone.connections[0].fromNodeId);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(clone);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(graph);
            }
        }

        [Test]
        public void Validate_고아_엣지와_null_노드를_검출한다()
        {
            var graph = CreateGraph();
            try
            {
                var entry = new ManualEntryNode();
                graph.nodes.Add(entry);
                graph.nodes.Add(null); // [SerializeReference] 유실 시뮬레이션
                graph.connections.Add(new FlowConnection
                {
                    fromNodeId = entry.id,
                    fromPort = FlowPort.Out,
                    toNodeId = "missing-node",
                    toPort = FlowPort.In,
                });

                var errors = new List<string>();
                bool valid = graph.Validate(errors);

                Assert.IsFalse(valid);
                Assert.AreEqual(2, errors.Count);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(graph);
            }
        }

        [Test]
        public void GetConnectionsFrom은_지정_포트의_엣지만_반환한다()
        {
            var graph = CreateGraph();
            try
            {
                var branch = new BranchNode();
                var onTrue = new LogNode();
                var onFalse = new LogNode();
                graph.nodes.Add(branch);
                graph.nodes.Add(onTrue);
                graph.nodes.Add(onFalse);
                graph.connections.Add(Connect(branch, FlowPort.True, onTrue));
                graph.connections.Add(Connect(branch, FlowPort.False, onFalse));

                var results = new List<FlowConnection>();
                graph.GetConnectionsFrom(branch.id, FlowPort.True, results);

                Assert.AreEqual(1, results.Count);
                Assert.AreEqual(onTrue.id, results[0].toNodeId);
                Assert.AreEqual(2, graph.CountConnectionsTo(onTrue.id) + graph.CountConnectionsTo(onFalse.id));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(graph);
            }
        }

        [Test]
        public void Validate_중복_노드_ID를_검출한다()
        {
            var graph = CreateGraph();
            try
            {
                var first = new ManualEntryNode();
                var second = new LogNode { id = first.id };
                graph.nodes.Add(first);
                graph.nodes.Add(second);

                var errors = new List<string>();
                Assert.IsFalse(graph.Validate(errors));
                Assert.That(errors, Has.Some.Contains("중복 노드 ID"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(graph);
            }
        }

        [Test]
        public void Validate_노드에_없는_포트를_검출한다()
        {
            var graph = CreateGraph();
            try
            {
                var entry = new ManualEntryNode();
                var log = new LogNode();
                graph.nodes.Add(entry);
                graph.nodes.Add(log);
                graph.connections.Add(Connect(entry, "RemovedPort", log));

                var errors = new List<string>();
                Assert.IsFalse(graph.Validate(errors));
                Assert.That(errors, Has.Some.Contains("출력 포트 유실"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(graph);
            }
        }

        [Test]
        public void PortSchema_실행_포트와_데이터_포트를_연결하지_않는다()
        {
            FlowPortDef execution = FlowPortDef.Output();
            FlowPortDef boolInput = FlowPortDef.DataInput<bool>("Value");
            FlowPortDef boolOutput = FlowPortDef.DataOutput<bool>("Value");

            Assert.IsFalse(FlowPortDef.AreCompatible(execution, boolInput));
            Assert.IsTrue(FlowPortDef.AreCompatible(boolOutput, boolInput));
        }

        [Test]
        public void TypedDataPort_ContextActor를_ActorType_Bool로_평가한다()
        {
            var graph = CreateGraph();
            var actorObject = new GameObject("FlowGraph_TestActor");
            try
            {
                var entry = new ManualEntryNode();
                var contextActor = new ContextActorNode();
                var isActorType = new IsActorTypeNode
                {
                    actorType = ActorType.Player | ActorType.Combat,
                };
                graph.nodes.Add(entry);
                graph.nodes.Add(contextActor);
                graph.nodes.Add(isActorType);
                graph.connections.Add(Connect(
                    contextActor,
                    ContextActorNode.ActorPort,
                    isActorType,
                    IsActorTypeNode.ActorPort));

                var context = new FlowContext(null, entry)
                {
                    Actor = new TestWorldActor(
                        actorObject.transform,
                        ActorType.Player | ActorType.Combat),
                };

                Assert.IsTrue(graph.TryEvaluateDataOutput(
                    context,
                    isActorType.id,
                    IsActorTypeNode.ResultPort,
                    out object result));
                Assert.AreEqual(true, result);
            }
            finally
            {
                Object.DestroyImmediate(actorObject);
                Object.DestroyImmediate(graph);
            }
        }

    }
}
