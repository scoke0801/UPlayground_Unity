using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace UPlayGround.FlowGraph.Tests
{
    /// <summary>FlowGraphSO 직렬화 왕복과 연결 유효성 검증 테스트 (설계서 12절).</summary>
    public sealed class FlowGraphSerializationTests
    {
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
    }
}
