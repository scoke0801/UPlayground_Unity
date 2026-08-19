using System.Collections.Generic;

namespace UPlayGround.FlowGraph.Editor
{
    public enum FlowIssueSeverity
    {
        Error = 0,
        Warning = 1,
        Info = 2,
    }

    public enum FlowQuickFix
    {
        None,
        RemoveInvalidConnections,
        CreateDefaultEntry,
        RemoveUnusedVariable,
    }

    public readonly struct FlowValidationIssue
    {
        public FlowValidationIssue(
            FlowIssueSeverity severity,
            string message,
            string nodeId = null,
            FlowQuickFix quickFix = FlowQuickFix.None,
            string target = null)
        {
            Severity = severity;
            Message = message;
            NodeId = nodeId;
            QuickFix = quickFix;
            Target = target;
        }

        public FlowIssueSeverity Severity { get; }
        public string Message { get; }

        /// <summary>관련 노드가 있으면 그 id — 검증 패널 클릭 시 포커스 대상.</summary>
        public string NodeId { get; }
        public FlowQuickFix QuickFix { get; }
        public string Target { get; }
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

            // 1) 직렬화/노드 식별자 무결성 (Error)
            var nodeIds = new HashSet<string>();
            for (int i = 0; i < graph.nodes.Count; i++)
            {
                FlowNode node = graph.nodes[i];
                if (node == null)
                {
                    issues.Add(new FlowValidationIssue(FlowIssueSeverity.Error,
                        $"nodes[{i}]가 null — [SerializeReference] 유실(클래스 리네임/이동 시 [MovedFrom] 누락) 의심"));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(node.id))
                {
                    issues.Add(new FlowValidationIssue(
                        FlowIssueSeverity.Error,
                        $"{node.DisplayName}: 노드 ID가 비어 있음",
                        node.id));
                }
                else if (!nodeIds.Add(node.id))
                {
                    issues.Add(new FlowValidationIssue(
                        FlowIssueSeverity.Error,
                        $"중복 노드 ID: {node.id}",
                        node.id));
                }
            }

            // 2) 연결 스키마 무결성 (Error)
            var exactConnections = new HashSet<string>();
            var endpointCounts = new Dictionary<string, int>();
            for (int i = 0; i < graph.connections.Count; i++)
            {
                FlowConnection c = graph.connections[i];
                if (c == null)
                {
                    issues.Add(new FlowValidationIssue(FlowIssueSeverity.Error,
                        $"connections[{i}]가 null",
                        quickFix: FlowQuickFix.RemoveInvalidConnections));
                    continue;
                }

                FlowNode from = graph.GetNode(c.fromNodeId);
                FlowNode to = graph.GetNode(c.toNodeId);
                if (from == null || to == null)
                {
                    issues.Add(new FlowValidationIssue(
                        FlowIssueSeverity.Error,
                        $"고아 엣지: {c.fromNodeId}.{c.fromPort} → {c.toNodeId}.{c.toPort}",
                        from?.id ?? to?.id,
                        FlowQuickFix.RemoveInvalidConnections));
                    continue;
                }

                bool fromValid = from.TryGetPort(
                    c.fromPort,
                    FlowPortDirection.Output,
                    out FlowPortDef fromPort);
                bool toValid = to.TryGetPort(
                    c.toPort,
                    FlowPortDirection.Input,
                    out FlowPortDef toPort);

                if (!fromValid)
                {
                    issues.Add(new FlowValidationIssue(
                        FlowIssueSeverity.Error,
                        $"출력 포트 유실: {from.DisplayName}.{c.fromPort}",
                        from.id,
                        FlowQuickFix.RemoveInvalidConnections));
                }
                if (!toValid)
                {
                    issues.Add(new FlowValidationIssue(
                        FlowIssueSeverity.Error,
                        $"입력 포트 유실: {to.DisplayName}.{c.toPort}",
                        to.id,
                        FlowQuickFix.RemoveInvalidConnections));
                }
                if (!fromValid || !toValid)
                    continue;

                if (!FlowPortDef.AreCompatible(fromPort, toPort))
                {
                    issues.Add(new FlowValidationIssue(
                        FlowIssueSeverity.Error,
                        $"비호환 포트: {from.DisplayName}.{c.fromPort} → {to.DisplayName}.{c.toPort}",
                        from.id,
                        FlowQuickFix.RemoveInvalidConnections));
                }

                string exactKey = $"{c.fromNodeId}\u001f{c.fromPort}\u001f{c.toNodeId}\u001f{c.toPort}";
                if (!exactConnections.Add(exactKey))
                {
                    issues.Add(new FlowValidationIssue(
                        FlowIssueSeverity.Error,
                        $"중복 엣지: {from.DisplayName}.{c.fromPort} → {to.DisplayName}.{c.toPort}",
                        from.id,
                        FlowQuickFix.RemoveInvalidConnections));
                }

                CheckCapacity(
                    endpointCounts,
                    $"O\u001f{c.fromNodeId}\u001f{c.fromPort}",
                    fromPort,
                    $"{from.DisplayName}.{c.fromPort}",
                    from.id,
                    issues);
                CheckCapacity(
                    endpointCounts,
                    $"I\u001f{c.toNodeId}\u001f{c.toPort}",
                    toPort,
                    $"{to.DisplayName}.{c.toPort}",
                    to.id,
                    issues);
                }

            // 3) 진입점 부재 (Error) — 시작 노드가 없으면 그래프를 발화할 방법이 없다
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
                    "진입점(Entry) 노드가 없음 — 그래프를 시작할 수 없다",
                    quickFix: FlowQuickFix.CreateDefaultEntry));
            }

            // 4) Blackboard 선언 무결성 (Error)
            var variableNames = new HashSet<string>();
            var variableIds = new HashSet<string>();
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
                if (!string.IsNullOrEmpty(variable.id) && !variableIds.Add(variable.id))
                {
                    issues.Add(new FlowValidationIssue(
                        FlowIssueSeverity.Error,
                        $"Blackboard 변수 ID 중복: {variable.id}"));
                }
            }

            var parameterNames = new HashSet<string>();
            var parameterIds = new HashSet<string>();
            foreach (FlowGraphParameterDef parameter in graph.parameters)
            {
                if (parameter == null || string.IsNullOrWhiteSpace(parameter.name))
                {
                    issues.Add(new FlowValidationIssue(
                        FlowIssueSeverity.Error,
                        "SubGraph 공개 인자 이름이 비어 있음"));
                    continue;
                }
                if (!parameterNames.Add(parameter.name))
                {
                    issues.Add(new FlowValidationIssue(
                        FlowIssueSeverity.Error,
                        $"SubGraph 공개 인자 이름 중복: '{parameter.name}'"));
                }
                if (variableNames.Contains(parameter.name))
                {
                    issues.Add(new FlowValidationIssue(
                        FlowIssueSeverity.Error,
                        $"Blackboard 변수와 공개 인자 이름 충돌: '{parameter.name}'"));
                }
                if (!string.IsNullOrEmpty(parameter.id) && !parameterIds.Add(parameter.id))
                {
                    issues.Add(new FlowValidationIssue(
                        FlowIssueSeverity.Error,
                        $"SubGraph 공개 인자 ID 중복: {parameter.id}"));
                }
                if (parameter.defaultValue != null && parameter.defaultValue.type != parameter.type)
                {
                    issues.Add(new FlowValidationIssue(
                        FlowIssueSeverity.Warning,
                        $"공개 인자 기본값 타입 불일치: {parameter.name} ({parameter.defaultValue.type} → {parameter.type})"));
                }
            }

            // 5) 진입점/SubGraph 계약 (Error/Warning)
            var manualEntryIds = new HashSet<string>();
            foreach (FlowNode node in graph.nodes)
            {
                if (node is ManualEntryNode manual
                    && !string.IsNullOrEmpty(manual.entryId)
                    && !manualEntryIds.Add(manual.entryId))
                {
                    issues.Add(new FlowValidationIssue(
                        FlowIssueSeverity.Error,
                        $"Manual Entry ID 중복: '{manual.entryId}'",
                        manual.id));
                }

                if (node is not SubGraphNode sub || sub.subGraph == null)
                    continue;

                if (SubGraphReaches(sub.subGraph, graph, new HashSet<int>()))
                {
                    issues.Add(new FlowValidationIssue(
                        FlowIssueSeverity.Error,
                        $"SubGraph 순환 참조: {graph.name} → {sub.subGraph.name}",
                        sub.id));
                }

                if (!string.IsNullOrEmpty(sub.entryId)
                    && !HasManualEntry(sub.subGraph, sub.entryId))
                {
                    issues.Add(new FlowValidationIssue(
                        FlowIssueSeverity.Error,
                        $"SubGraph Entry 유실: {sub.subGraph.name}/{sub.entryId}",
                        sub.id));
                }

                ValidateSubGraphBindings(graph, sub, issues);
            }

            AddCycleIssues(graph, FlowPortKind.Execution, issues);
            AddCycleIssues(graph, FlowPortKind.Data, issues);

            // 6) 도달성/저작 실수 (Warning)
            var hasIncoming = new HashSet<string>();
            var hasOutgoing = new HashSet<string>();
            var wiredOutputPorts = new HashSet<string>();
            foreach (FlowConnection c in graph.connections)
            {
                if (c == null)
                    continue;
                hasIncoming.Add(c.toNodeId);
                hasOutgoing.Add(c.fromNodeId);
                wiredOutputPorts.Add($"{c.fromNodeId}\u001f{c.fromPort}");
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

                        AddPartiallyWiredOutputIssues(node, wiredOutputPorts, issues);
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

            foreach (FlowVariableDef variable in graph.variables)
            {
                if (variable != null
                    && !string.IsNullOrEmpty(variable.name)
                    && !IsVariableUsed(graph, variable.name))
                {
                    issues.Add(new FlowValidationIssue(
                        FlowIssueSeverity.Info,
                        $"사용되지 않는 Blackboard 변수: '{variable.name}'",
                        quickFix: FlowQuickFix.RemoveUnusedVariable,
                        target: variable.name));
                }
            }

            return issues;
        }

        private static bool HasManualEntry(FlowGraphSO graph, string entryId)
        {
            foreach (FlowNode node in graph.nodes)
            {
                if (node is ManualEntryNode manual && manual.entryId == entryId)
                    return true;
            }
            return false;
        }

        private static void ValidateSubGraphBindings(
            FlowGraphSO parent,
            SubGraphNode sub,
            List<FlowValidationIssue> issues)
        {
            var mappedParameters = new HashSet<string>();
            bool hasOutput = false;
            if (sub.parameterBindings != null)
            {
                foreach (FlowParameterBinding binding in sub.parameterBindings)
                {
                    if (binding == null)
                        continue;
                    FlowGraphParameterDef parameter =
                        sub.subGraph.GetParameter(binding.parameterId, binding.parameterName);
                    FlowVariableDef variable =
                        parent.GetVariable(binding.parentVariableId, binding.parentVariableName);
                    if (parameter == null)
                    {
                        issues.Add(new FlowValidationIssue(
                            FlowIssueSeverity.Error,
                            $"SubGraph 인자 매핑 유실: '{binding.parameterName}'",
                            sub.id));
                        continue;
                    }
                    string parameterKey = !string.IsNullOrEmpty(parameter.id)
                        ? parameter.id
                        : parameter.name;
                    if (!mappedParameters.Add(parameterKey))
                    {
                        issues.Add(new FlowValidationIssue(
                            FlowIssueSeverity.Error,
                            $"SubGraph 인자 중복 매핑: '{parameter.name}'",
                            sub.id));
                    }
                    if (variable == null)
                    {
                        issues.Add(new FlowValidationIssue(
                            FlowIssueSeverity.Error,
                            $"부모 Blackboard 매핑 유실: '{binding.parentVariableName}'",
                            sub.id));
                        continue;
                    }
                    if (variable.type != parameter.type)
                    {
                        issues.Add(new FlowValidationIssue(
                            FlowIssueSeverity.Error,
                            $"SubGraph 인자 타입 불일치: {parameter.name}({parameter.type}) ↔ {variable.name}({variable.type})",
                            sub.id));
                    }
                    hasOutput |= parameter.AllowsOutput;
                }
            }

            foreach (FlowGraphParameterDef parameter in sub.subGraph.parameters)
            {
                if (parameter == null || !parameter.required || !parameter.AllowsInput)
                    continue;
                string key = !string.IsNullOrEmpty(parameter.id) ? parameter.id : parameter.name;
                if (!mappedParameters.Contains(key))
                {
                    issues.Add(new FlowValidationIssue(
                        FlowIssueSeverity.Error,
                        $"필수 SubGraph 입력 미매핑: '{parameter.name}'",
                        sub.id));
                }
            }

            if (hasOutput && !sub.waitForCompletion)
            {
                issues.Add(new FlowValidationIssue(
                    FlowIssueSeverity.Error,
                    "SubGraph Out/InOut 매핑은 완료 대기일 때만 사용할 수 있음",
                    sub.id));
            }
            if (hasOutput && CountMatchingManualEntries(sub) != 1)
            {
                issues.Add(new FlowValidationIssue(
                    FlowIssueSeverity.Error,
                    "SubGraph 출력 매핑은 정확히 하나의 Manual Entry를 호출할 때만 사용할 수 있음",
                    sub.id));
            }
        }

        private static int CountMatchingManualEntries(SubGraphNode sub)
        {
            int count = 0;
            foreach (FlowNode node in sub.subGraph.nodes)
            {
                if (node is ManualEntryNode entry
                    && (string.IsNullOrEmpty(sub.entryId) || entry.entryId == sub.entryId))
                {
                    count++;
                }
            }
            return count;
        }

        private static bool SubGraphReaches(
            FlowGraphSO current,
            FlowGraphSO target,
            HashSet<int> visited)
        {
            if (current == null)
                return false;
            if (current == target)
                return true;
            if (!visited.Add(current.GetInstanceID()))
                return false;

            foreach (FlowNode node in current.nodes)
            {
                if (node is SubGraphNode sub
                    && sub.subGraph != null
                    && SubGraphReaches(sub.subGraph, target, visited))
                {
                    return true;
                }
            }
            return false;
        }

        private static void AddCycleIssues(
            FlowGraphSO graph,
            FlowPortKind portKind,
            List<FlowValidationIssue> issues)
        {
            var adjacency = new Dictionary<string, List<string>>();
            foreach (FlowNode node in graph.nodes)
            {
                if (node != null && !string.IsNullOrEmpty(node.id))
                    adjacency[node.id] = new List<string>();
            }
            foreach (FlowConnection connection in graph.connections)
            {
                if (connection == null)
                    continue;

                FlowNode source = graph.GetNode(connection.fromNodeId);
                if (source == null
                    || !source.TryGetPort(
                        connection.fromPort,
                        FlowPortDirection.Output,
                        out FlowPortDef output)
                    || output.Kind != portKind)
                    continue;

                if (adjacency.TryGetValue(connection.fromNodeId, out List<string> targets)
                    && adjacency.ContainsKey(connection.toNodeId))
                {
                    targets.Add(connection.toNodeId);
                }
            }

            var indexByNode = new Dictionary<string, int>();
            var lowLink = new Dictionary<string, int>();
            var stack = new Stack<string>();
            var onStack = new HashSet<string>();
            int nextIndex = 0;

            void Visit(string nodeId)
            {
                indexByNode[nodeId] = nextIndex;
                lowLink[nodeId] = nextIndex;
                nextIndex++;
                stack.Push(nodeId);
                onStack.Add(nodeId);

                foreach (string targetId in adjacency[nodeId])
                {
                    if (!indexByNode.ContainsKey(targetId))
                    {
                        Visit(targetId);
                        lowLink[nodeId] = System.Math.Min(lowLink[nodeId], lowLink[targetId]);
                    }
                    else if (onStack.Contains(targetId))
                    {
                        lowLink[nodeId] = System.Math.Min(lowLink[nodeId], indexByNode[targetId]);
                    }
                }

                if (lowLink[nodeId] != indexByNode[nodeId])
                    return;

                var component = new List<string>();
                string current;
                do
                {
                    current = stack.Pop();
                    onStack.Remove(current);
                    component.Add(current);
                } while (current != nodeId);

                bool selfLoop = component.Count == 1
                                && adjacency[component[0]].Contains(component[0]);
                if (component.Count <= 1 && !selfLoop)
                    return;

                bool hasBreaker = portKind == FlowPortKind.Execution ? false : true;
                foreach (string id in component)
                {
                    if (portKind == FlowPortKind.Execution
                        && IsImmediateCycleBreaker(graph.GetNode(id)))
                    {
                        hasBreaker = true;
                        break;
                    }
                }
                if (portKind == FlowPortKind.Data)
                {
                    issues.Add(new FlowValidationIssue(
                        FlowIssueSeverity.Error,
                        $"데이터 포트 순환 의존: {component.Count}개 노드",
                        component[0]));
                }
                else if (!hasBreaker)
                {
                    issues.Add(new FlowValidationIssue(
                        FlowIssueSeverity.Warning,
                        $"대기 없는 실행 사이클: {component.Count}개 노드 — 한 프레임 실행 예산 초과 가능",
                        component[0]));
                }
            }

            foreach (string nodeId in adjacency.Keys)
            {
                if (!indexByNode.ContainsKey(nodeId))
                    Visit(nodeId);
            }
        }

        private static bool IsImmediateCycleBreaker(FlowNode node)
        {
            return node is WaitTimeNode
                   or WaitConditionNode
                   or WaitForGameEventNode
                   or PlayDialogueNode
                   or GateNode
                   or PlayDialogueRequiredNode
                   or PlayRecruitmentPostDialogueNode
                   or WaitRecruitmentCombatResolvedNode
                   || node is SubGraphNode { waitForCompletion: true };
        }

        /// <summary>
        /// 여러 갈래를 내놓는 노드에서 일부 갈래만 배선하면, 배선되지 않은 갈래로 나갔을 때
        /// 흐름이 조용히 끝난다. 실패·거부 경로를 빠뜨리는 저작 실수를 잡기 위해 경고한다.
        /// 갈래가 모두 비어 있으면 의도된 종단으로 보고, 조건 분기의 반대편처럼
        /// 비워 두는 것이 정상인 포트는 Optional로 표시돼 있다.
        /// </summary>
        private static void AddPartiallyWiredOutputIssues(
            FlowNode node,
            HashSet<string> wiredOutputPorts,
            List<FlowValidationIssue> issues)
        {
            var unwired = new List<FlowPortDef>();
            int executionOutputs = 0;
            bool hasWired = false;

            foreach (FlowPortDef port in node.Ports)
            {
                if (port.Direction != FlowPortDirection.Output
                    || port.Kind != FlowPortKind.Execution)
                {
                    continue;
                }

                executionOutputs++;
                if (wiredOutputPorts.Contains($"{node.id}\u001f{port.Id}"))
                    hasWired = true;
                else if (!port.Optional)
                    unwired.Add(port);
            }

            if (executionOutputs < 2 || !hasWired)
                return;

            foreach (FlowPortDef port in unwired)
            {
                issues.Add(new FlowValidationIssue(
                    FlowIssueSeverity.Warning,
                    $"{node.DisplayName}: 출력 포트 '{port.DisplayName}' 미연결 — 이 갈래로 나가면 흐름이 끊긴다",
                    node.id));
            }
        }

        private static bool IsVariableUsed(FlowGraphSO graph, string variableName)
        {
            foreach (FlowNode node in graph.nodes)
            {
                if (node is SetVariableNode set && set.variableName == variableName)
                    return true;
                if (node is CheckVariableNode check && check.variableName == variableName)
                    return true;

                FlowCondition condition = node switch
                {
                    BranchNode branch => branch.condition,
                    WaitConditionNode wait => wait.condition,
                    _ => null,
                };
                if (condition is VariableCondition variable
                    && variable.variableName == variableName)
                {
                    return true;
                }
            }
            return false;
        }

        private static void CheckCapacity(
            Dictionary<string, int> endpointCounts,
            string endpointKey,
            FlowPortDef port,
            string displayName,
            string nodeId,
            List<FlowValidationIssue> issues)
        {
            endpointCounts.TryGetValue(endpointKey, out int count);
            count++;
            endpointCounts[endpointKey] = count;
            if (port.Capacity == FlowPortCapacity.Single && count > 1)
            {
                issues.Add(new FlowValidationIssue(
                    FlowIssueSeverity.Error,
                    $"Single 포트 다중 연결: {displayName} ({count}개)",
                    nodeId));
            }
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
