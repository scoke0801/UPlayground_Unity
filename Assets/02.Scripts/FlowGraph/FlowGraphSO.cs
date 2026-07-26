using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Flow;

namespace UPlayGround.FlowGraph
{
    /// <summary>outNodeId.outPort → inNodeId.inPort 엣지.</summary>
    [Serializable]
    public sealed class FlowConnection
    {
        public string fromNodeId;
        public string fromPort;
        public string toNodeId;
        public string toPort;
    }

    /// <summary>에디터 그룹(코멘트 박스). 런타임 실행에는 관여하지 않는다.</summary>
    [Serializable]
    public sealed class FlowGraphGroup
    {
        public string title;
        public Vector2 position;
        public List<string> nodeIds = new();
    }

    /// <summary>
    /// 게임 흐름 제어 노드 그래프 에셋. 노드는 [SerializeReference] 다형 + 단일 에셋 방식
    /// (BT/Dialogue의 SO-서브에셋 방식과 달리 고아 서브에셋·에셋 diff 복잡도를 피한다).
    /// </summary>
    [CreateAssetMenu(menuName = "UPlayGround/FlowGraph/Graph", fileName = "FLOW_")]
    public sealed class FlowGraphSO : FlowGraphAssetBase
    {
        [Tooltip("FlowGraphManager 등록·조회용 식별자. 비우면 에셋 이름을 사용한다.")]
        public string graphId;

        [SerializeReference] public List<FlowNode> nodes = new();
        public List<FlowConnection> connections = new();
        public List<FlowGraphGroup> editorGroups = new();

        /// <summary>그래프 스코프 블랙보드 변수 선언. 발화 시 FlowContext에 기본값으로 복사된다.</summary>
        public List<FlowVariableDef> variables = new();

        /// <summary>SubGraph 호출자에게 공개하는 입출력 계약.</summary>
        public List<FlowGraphParameterDef> parameters = new();

        public bool HasVariable(string variableName)
        {
            for (int i = 0; i < variables.Count; i++)
            {
                if (variables[i] != null && variables[i].name == variableName)
                    return true;
            }
            return false;
        }

        public FlowVariableDef GetVariable(string variableId, string fallbackName = null)
        {
            for (int i = 0; i < variables.Count; i++)
            {
                FlowVariableDef variable = variables[i];
                if (variable == null)
                    continue;
                if (!string.IsNullOrEmpty(variableId) && variable.id == variableId)
                    return variable;
            }
            if (!string.IsNullOrEmpty(fallbackName))
            {
                for (int i = 0; i < variables.Count; i++)
                {
                    FlowVariableDef variable = variables[i];
                    if (variable != null && variable.name == fallbackName)
                        return variable;
                }
            }
            return null;
        }

        public string ResolveVariableName(string variableId, string fallbackName)
        {
            return GetVariable(variableId, fallbackName)?.name ?? fallbackName;
        }

        public FlowGraphParameterDef GetParameter(string parameterId, string fallbackName = null)
        {
            for (int i = 0; i < parameters.Count; i++)
            {
                FlowGraphParameterDef parameter = parameters[i];
                if (parameter == null)
                    continue;
                if (!string.IsNullOrEmpty(parameterId) && parameter.id == parameterId)
                    return parameter;
            }
            if (!string.IsNullOrEmpty(fallbackName))
            {
                for (int i = 0; i < parameters.Count; i++)
                {
                    FlowGraphParameterDef parameter = parameters[i];
                    if (parameter != null && parameter.name == fallbackName)
                        return parameter;
                }
            }
            return null;
        }

        public string ResolvedGraphId => string.IsNullOrEmpty(graphId) ? name : graphId;

        public override string GraphId => ResolvedGraphId;

        public FlowNode GetNode(string nodeId)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] != null && nodes[i].id == nodeId)
                    return nodes[i];
            }
            return null;
        }

        public void GetConnectionsFrom(string nodeId, string port, List<FlowConnection> results)
        {
            for (int i = 0; i < connections.Count; i++)
            {
                FlowConnection c = connections[i];
                if (c.fromNodeId == nodeId && c.fromPort == port)
                    results.Add(c);
            }
        }

        /// <summary>
        /// 소비 노드의 데이터 입력에 연결된 생산자를 요청 시점에 평가한다.
        /// 연결 없음·타입 불일치·순환 평가는 false로 반환해 소비자가 명시적인 기본값을 선택하게 한다.
        /// </summary>
        public bool TryEvaluateDataInput<T>(
            FlowContext context,
            FlowNode consumer,
            string inputPortId,
            out T value)
        {
            value = default;
            if (context == null || consumer == null)
                return false;

            FlowConnection sourceConnection = null;
            for (int i = 0; i < connections.Count; i++)
            {
                FlowConnection connection = connections[i];
                if (connection != null
                    && connection.toNodeId == consumer.id
                    && connection.toPort == inputPortId)
                {
                    sourceConnection = connection;
                    break;
                }
            }

            if (sourceConnection == null)
                return false;

            if (!TryEvaluateDataOutput(
                    context,
                    sourceConnection.fromNodeId,
                    sourceConnection.fromPort,
                    out object raw))
                return false;

            if (raw is T typed)
            {
                value = typed;
                return true;
            }

            return raw == null && default(T) == null;
        }

        public bool TryEvaluateDataOutput(
            FlowContext context,
            string nodeId,
            string outputPortId,
            out object value)
        {
            value = null;
            if (context == null || GetNode(nodeId) is not FlowDataNode source)
                return false;
            if (!context.TryBeginDataEvaluation(this, source.id, outputPortId))
                return false;

            try
            {
                return source.TryEvaluate(context, this, outputPortId, out value);
            }
            finally
            {
                context.EndDataEvaluation(this, source.id, outputPortId);
            }
        }

        public int CountConnectionsTo(string nodeId)
        {
            int count = 0;
            for (int i = 0; i < connections.Count; i++)
            {
                if (connections[i].toNodeId == nodeId)
                    count++;
            }
            return count;
        }

        /// <summary>
        /// 널 노드([SerializeReference] 유실)와 고아 연결을 검출한다. 문제가 없으면 true.
        /// </summary>
        public bool Validate(List<string> errors)
        {
            bool valid = true;
            var nodeIds = new HashSet<string>();
            for (int i = 0; i < nodes.Count; i++)
            {
                FlowNode node = nodes[i];
                if (node == null)
                {
                    errors?.Add($"nodes[{i}]가 null — [SerializeReference] 유실(클래스 리네임/이동 시 [MovedFrom] 누락) 의심");
                    valid = false;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(node.id))
                {
                    errors?.Add($"nodes[{i}]({node.DisplayName})의 ID가 비어 있음");
                    valid = false;
                }
                else if (!nodeIds.Add(node.id))
                {
                    errors?.Add($"중복 노드 ID: {node.id}");
                    valid = false;
                }
            }

            var exactConnections = new HashSet<string>();
            var endpointCounts = new Dictionary<string, int>();
            for (int i = 0; i < connections.Count; i++)
            {
                FlowConnection c = connections[i];
                if (c == null)
                {
                    errors?.Add($"connections[{i}]가 null");
                    valid = false;
                    continue;
                }

                FlowNode from = GetNode(c.fromNodeId);
                FlowNode to = GetNode(c.toNodeId);
                if (from == null || to == null)
                {
                    errors?.Add($"connections[{i}] 고아 엣지: {c.fromNodeId}.{c.fromPort} → {c.toNodeId}.{c.toPort}");
                    valid = false;
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
                    errors?.Add($"connections[{i}] 출력 포트 유실: {from.DisplayName}.{c.fromPort}");
                    valid = false;
                }
                if (!toValid)
                {
                    errors?.Add($"connections[{i}] 입력 포트 유실: {to.DisplayName}.{c.toPort}");
                    valid = false;
                }
                if (!fromValid || !toValid)
                    continue;

                if (!FlowPortDef.AreCompatible(fromPort, toPort))
                {
                    errors?.Add(
                        $"connections[{i}] 비호환 포트: {from.DisplayName}.{c.fromPort} → {to.DisplayName}.{c.toPort}");
                    valid = false;
                }

                string exactKey = $"{c.fromNodeId}\u001f{c.fromPort}\u001f{c.toNodeId}\u001f{c.toPort}";
                if (!exactConnections.Add(exactKey))
                {
                    errors?.Add(
                        $"connections[{i}] 중복 엣지: {from.DisplayName}.{c.fromPort} → {to.DisplayName}.{c.toPort}");
                    valid = false;
                }

                valid &= ValidateCapacity(
                    endpointCounts,
                    $"O\u001f{c.fromNodeId}\u001f{c.fromPort}",
                    fromPort,
                    $"{from.DisplayName}.{c.fromPort}",
                    errors);
                valid &= ValidateCapacity(
                    endpointCounts,
                    $"I\u001f{c.toNodeId}\u001f{c.toPort}",
                    toPort,
                    $"{to.DisplayName}.{c.toPort}",
                    errors);
            }

            return valid;
        }

        private static bool ValidateCapacity(
            Dictionary<string, int> endpointCounts,
            string endpointKey,
            FlowPortDef port,
            string displayName,
            List<string> errors)
        {
            endpointCounts.TryGetValue(endpointKey, out int count);
            count++;
            endpointCounts[endpointKey] = count;
            if (port.Capacity == FlowPortCapacity.Single && count > 1)
            {
                errors?.Add($"Single 포트 다중 연결: {displayName} ({count}개)");
                return false;
            }

            return true;
        }
    }
}
