using System.Collections.Generic;

namespace UPlayGround.FlowGraph.Editor
{
    public enum FlowIssueSeverity
    {
        Error = 0,
        Warning = 1,
        Info = 2,
    }

    public readonly struct FlowValidationIssue
    {
        public FlowValidationIssue(FlowIssueSeverity severity, string message, string nodeId = null)
        {
            Severity = severity;
            Message = message;
            NodeId = nodeId;
        }

        public FlowIssueSeverity Severity { get; }
        public string Message { get; }

        /// <summary>관련 노드가 있으면 그 id — 검증 패널 클릭 시 포커스 대상.</summary>
        public string NodeId { get; }
    }

    /// <summary>
    /// 그래프 저작 오류를 검출하는 에디터 검증기 (시안: 하단 Validation 패널의 데이터 소스).
    /// 런타임 FlowGraphSO.Validate(널 노드/고아 엣지)에 도달성·노드별 저작 실수 검사를 얹는다.
    /// </summary>
    public static class FlowGraphValidator
    {
        public static List<FlowValidationIssue> Validate(FlowGraphSO graph)
        {
            var issues = new List<FlowValidationIssue>();
            if (graph == null)
                return issues;

            // 1) 직렬화 유실 / 고아 엣지 (Error)
            for (int i = 0; i < graph.nodes.Count; i++)
            {
                if (graph.nodes[i] == null)
                {
                    issues.Add(new FlowValidationIssue(FlowIssueSeverity.Error,
                        $"nodes[{i}]가 null — [SerializeReference] 유실(클래스 리네임/이동 시 [MovedFrom] 누락) 의심"));
                }
            }

            for (int i = 0; i < graph.connections.Count; i++)
            {
                FlowConnection c = graph.connections[i];
                if (graph.GetNode(c.fromNodeId) == null || graph.GetNode(c.toNodeId) == null)
                {
                    issues.Add(new FlowValidationIssue(FlowIssueSeverity.Error,
                        $"고아 엣지: {c.fromNodeId}.{c.fromPort} → {c.toNodeId}.{c.toPort}"));
                }
            }

            // 2) 진입점 부재 (Error) — 시작 노드가 없으면 그래프를 발화할 방법이 없다
            bool hasEntry = false;
            foreach (FlowNode node in graph.nodes)
            {
                if (node is EntryNode)
                {
                    hasEntry = true;
                    break;
                }
            }
            if (!hasEntry)
            {
                issues.Add(new FlowValidationIssue(FlowIssueSeverity.Error,
                    "진입점(Entry) 노드가 없음 — 그래프를 시작할 수 없다"));
            }

            // 3) Blackboard 선언 무결성 (Error)
            var variableNames = new HashSet<string>();
            foreach (FlowVariableDef variable in graph.variables)
            {
                if (variable == null || string.IsNullOrWhiteSpace(variable.name))
                {
                    issues.Add(new FlowValidationIssue(FlowIssueSeverity.Error,
                        "Blackboard 변수 이름이 비어 있음"));
                    continue;
                }

                if (!variableNames.Add(variable.name))
                {
                    issues.Add(new FlowValidationIssue(FlowIssueSeverity.Error,
                        $"Blackboard 변수 이름 중복: '{variable.name}'"));
                }
            }

            // 4) 도달성/저작 실수 (Warning)
            var hasIncoming = new HashSet<string>();
            var hasOutgoing = new HashSet<string>();
            foreach (FlowConnection c in graph.connections)
            {
                hasIncoming.Add(c.toNodeId);
                hasOutgoing.Add(c.fromNodeId);
            }

            foreach (FlowNode node in graph.nodes)
            {
                if (node == null)
                    continue;

                switch (node)
                {
                    case EntryNode entry:
                        if (!hasOutgoing.Contains(entry.id))
                        {
                            issues.Add(new FlowValidationIssue(FlowIssueSeverity.Warning,
                                $"{entry.DisplayName}: 진입점에 연결된 출력이 없음", entry.id));
                        }
                        break;

                    default:
                        if (!hasIncoming.Contains(node.id))
                        {
                            issues.Add(new FlowValidationIssue(FlowIssueSeverity.Warning,
                                $"{node.DisplayName}: 도달 불가 노드 (유입 연결 없음)", node.id));
                        }
                        break;
                }

                switch (node)
                {
                    case WaitTimeNode wait when wait.seconds <= 0f:
                        issues.Add(new FlowValidationIssue(FlowIssueSeverity.Warning,
                            "Wait(Time): 대기 시간이 0초", node.id));
                        break;

                    case BranchNode branch when branch.condition == null:
                        issues.Add(new FlowValidationIssue(FlowIssueSeverity.Warning,
                            "Branch: 조건 미지정 — 항상 False로 분기", node.id));
                        break;

                    case SubGraphNode sub when sub.subGraph == null:
                        issues.Add(new FlowValidationIssue(FlowIssueSeverity.Warning,
                            "SubGraph: 하위 그래프 미지정", node.id));
                        break;

                    case PlayDialogueNode dialogue when dialogue.dialogue == null:
                        issues.Add(new FlowValidationIssue(FlowIssueSeverity.Warning,
                            "PlayDialogue: 대화 그래프 미지정", node.id));
                        break;

                    case SetVariableNode setVar
                        when !string.IsNullOrEmpty(setVar.variableName) && !graph.HasVariable(setVar.variableName):
                        issues.Add(new FlowValidationIssue(FlowIssueSeverity.Warning,
                            $"SetVariable: Blackboard에 선언되지 않은 변수 '{setVar.variableName}'", node.id));
                        break;

                    case SetVariableNode setVar when TryGetVariable(graph, setVar.variableName, out FlowVariableDef setDef)
                                                     && setVar.value != null && setVar.value.type != setDef.type:
                        issues.Add(new FlowValidationIssue(FlowIssueSeverity.Warning,
                            $"SetVariable: '{setVar.variableName}' 타입 불일치 ({setVar.value.type} → {setDef.type})", node.id));
                        break;

                    case CheckVariableNode checkVar
                        when !string.IsNullOrEmpty(checkVar.variableName) && !graph.HasVariable(checkVar.variableName):
                        issues.Add(new FlowValidationIssue(FlowIssueSeverity.Warning,
                            $"CheckVariable: Blackboard에 선언되지 않은 변수 '{checkVar.variableName}'", node.id));
                        break;

                    case CheckVariableNode checkVar when TryGetVariable(graph, checkVar.variableName, out FlowVariableDef checkDef)
                                                         && checkVar.expected != null && checkVar.expected.type != checkDef.type:
                        issues.Add(new FlowValidationIssue(FlowIssueSeverity.Warning,
                            $"CheckVariable: '{checkVar.variableName}' 타입 불일치 ({checkVar.expected.type} ↔ {checkDef.type})", node.id));
                        break;
                }

                // Branch/Wait의 VariableCondition도 선언 여부 검사
                FlowCondition condition = node switch
                {
                    BranchNode b => b.condition,
                    WaitConditionNode w => w.condition,
                    _ => null,
                };
                if (condition is VariableCondition varCondition
                    && !string.IsNullOrEmpty(varCondition.variableName)
                    && !graph.HasVariable(varCondition.variableName))
                {
                    issues.Add(new FlowValidationIssue(FlowIssueSeverity.Warning,
                        $"조건: Blackboard에 선언되지 않은 변수 '{varCondition.variableName}'", node.id));
                }
                else if (condition is VariableCondition typedCondition
                         && TryGetVariable(graph, typedCondition.variableName, out FlowVariableDef conditionDef)
                         && typedCondition.expected != null
                         && typedCondition.expected.type != conditionDef.type)
                {
                    issues.Add(new FlowValidationIssue(FlowIssueSeverity.Warning,
                        $"조건: '{typedCondition.variableName}' 타입 불일치 ({typedCondition.expected.type} ↔ {conditionDef.type})", node.id));
                }
            }

            return issues;
        }

        private static bool TryGetVariable(FlowGraphSO graph, string variableName, out FlowVariableDef result)
        {
            foreach (FlowVariableDef variable in graph.variables)
            {
                if (variable != null && variable.name == variableName)
                {
                    result = variable;
                    return true;
                }
            }

            result = null;
            return false;
        }
    }
}
