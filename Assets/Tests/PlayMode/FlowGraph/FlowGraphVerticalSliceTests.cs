using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace UPlayGround.FlowGraph.PlayModeTests
{
    public sealed class FlowGraphVerticalSliceTests
    {
        [UnityTest]
        public IEnumerator ManualEntry_SetVariable_실행과_Trace를_남긴다()
        {
            var graph = ScriptableObject.CreateInstance<FlowGraphSO>();
            var runnerObject = new GameObject("FlowGraph_PlayModeRunner");
            runnerObject.SetActive(false);
            try
            {
                var variable = new FlowVariableDef
                {
                    name = "completed",
                    type = FlowVariableType.Bool,
                };
                var entry = new ManualEntryNode
                {
                    entryId = "start",
                    repeatPolicy = FlowRepeatPolicy.Always,
                };
                var setVariable = new SetVariableNode
                {
                    variableId = variable.id,
                    variableName = variable.name,
                    value = new FlowVariableValue
                    {
                        type = FlowVariableType.Bool,
                        boolValue = true,
                    },
                };
                graph.graphId = "FLOW_PlayModeVerticalSlice";
                graph.variables.Add(variable);
                graph.nodes.Add(entry);
                graph.nodes.Add(setVariable);
                graph.connections.Add(new FlowConnection
                {
                    fromNodeId = entry.id,
                    fromPort = FlowPort.Out,
                    toNodeId = setVariable.id,
                    toPort = FlowPort.In,
                });

                FlowGraphRunner runner = runnerObject.AddComponent<FlowGraphRunner>();
                runner.SetGraph(graph, registerToManager: false);
                runnerObject.SetActive(true);

                Assert.IsTrue(runner.FireManualEntries("start"));
                yield return null;
                yield return null;

                var trace = new List<FlowTraceEvent>();
                runner.GetTraceSnapshot(trace);
                Assert.That(trace, Has.Some.Matches<FlowTraceEvent>(
                    item => item.kind == FlowTraceKind.Entry));
                Assert.That(trace, Has.Some.Matches<FlowTraceEvent>(
                    item => item.kind == FlowTraceKind.BlackboardWrite
                            && item.valueName == "completed"
                            && item.valueSummary == "True"));
                Assert.That(trace, Has.Some.Matches<FlowTraceEvent>(
                    item => item.kind == FlowTraceKind.NodeEnd
                            && item.nodeId == setVariable.id));
            }
            finally
            {
                Object.Destroy(runnerObject);
                Object.Destroy(graph);
            }
        }
    }
}
