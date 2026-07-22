using System;
using System.Collections.Generic;
using UnityEngine;

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
    public sealed class FlowGraphSO : ScriptableObject
    {
        [Tooltip("FlowGraphManager 등록·조회용 식별자. 비우면 에셋 이름을 사용한다.")]
        public string graphId;

        [SerializeReference] public List<FlowNode> nodes = new();
        public List<FlowConnection> connections = new();
        public List<FlowGraphGroup> editorGroups = new();

        /// <summary>그래프 스코프 블랙보드 변수 선언. 발화 시 FlowContext에 기본값으로 복사된다.</summary>
        public List<FlowVariableDef> variables = new();

        public bool HasVariable(string variableName)
        {
            for (int i = 0; i < variables.Count; i++)
            {
                if (variables[i] != null && variables[i].name == variableName)
                    return true;
            }
            return false;
        }

        public string ResolvedGraphId => string.IsNullOrEmpty(graphId) ? name : graphId;

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
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] == null)
                {
                    errors?.Add($"nodes[{i}]가 null — [SerializeReference] 유실(클래스 리네임/이동 시 [MovedFrom] 누락) 의심");
                    valid = false;
                }
            }

            for (int i = 0; i < connections.Count; i++)
            {
                FlowConnection c = connections[i];
                if (GetNode(c.fromNodeId) == null || GetNode(c.toNodeId) == null)
                {
                    errors?.Add($"connections[{i}] 고아 엣지: {c.fromNodeId}.{c.fromPort} → {c.toNodeId}.{c.toPort}");
                    valid = false;
                }
            }

            return valid;
        }
    }
}
