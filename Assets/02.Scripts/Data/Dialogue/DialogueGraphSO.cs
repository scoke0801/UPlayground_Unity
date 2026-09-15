using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Dialogue
{
    [CreateAssetMenu(menuName = "UPlayGround/대화/Graph", fileName = "DLG_")]
    public class DialogueGraphSO : ScriptableObject
    {
        public string graphId;
        public string graphName;
        public string startNodeId;
        public List<DialogueNodeSO> nodes = new();

        [Tooltip("대사는 없지만 이 대화 동안 함께 멈춰 서 있어야 하는 인물의 화자 ID."
                 + " 화자·청자로 등장하는 인물은 자동으로 잡히므로 여기 적지 않는다.")]
        public List<string> silentParticipantSpeakerIds = new();

        // 런타임 빠른 조회용 — 첫 접근 시 빌드됨
        private Dictionary<string, DialogueNodeSO> _nodeMap;

        public DialogueNodeSO GetNode(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (_nodeMap == null && !TryBuildNodeMap(out _)) return null;
            return _nodeMap.GetValueOrDefault(id);
        }

        /// <summary>재생 전에 누락 연결·채널 혼용·입력 대기 없는 순환을 검사한다.</summary>
        public bool TryValidatePlayback(out string error)
        {
            if (!TryBuildNodeMap(out error)) return false;
            DialogueNodeSO start = GetNode(startNodeId);
            if (start == null)
            {
                error = "시작 노드가 없습니다.";
                return false;
            }

            var pending = new Queue<DialogueNodeSO>();
            var reachable = new HashSet<DialogueNodeSO>();
            pending.Enqueue(start);
            while (pending.Count > 0)
            {
                DialogueNodeSO node = pending.Dequeue();
                if (!reachable.Add(node)) continue;
                if (node.channel != start.channel
                    || (node.nodeType == NodeType.Choice && start.channel != DialogueChannel.Main))
                {
                    error = $"채널이 일치하지 않거나 선택지를 표시할 수 없습니다: {node.nodeId}";
                    return false;
                }

                foreach (string nextId in GetNextNodeIds(node))
                {
                    DialogueNodeSO next = GetNode(nextId);
                    if (next == null)
                    {
                        error = $"연결된 노드가 없습니다: {node.nodeId} → {nextId}";
                        return false;
                    }
                    pending.Enqueue(next);
                }
                if (node.nodeType == NodeType.Choice && (node.choices == null || node.choices.Count == 0))
                {
                    error = $"선택지가 없습니다: {node.nodeId}";
                    return false;
                }
            }

            // 자동 전이만으로 만든 부분 그래프를 위상 정렬한다. Talk를 거치는 반복은 허용한다.
            var incoming = new Dictionary<DialogueNodeSO, int>();
            foreach (DialogueNodeSO node in reachable)
                if (IsImmediateNode(node)) incoming.Add(node, 0);
            foreach (DialogueNodeSO node in reachable)
            {
                if (!IsImmediateNode(node)) continue;
                foreach (string id in GetNextNodeIds(node))
                {
                    DialogueNodeSO next = GetNode(id);
                    if (incoming.ContainsKey(next)) incoming[next]++;
                }
            }
            foreach (var pair in incoming)
                if (pair.Value == 0) pending.Enqueue(pair.Key);
            int visited = 0;
            while (pending.Count > 0)
            {
                DialogueNodeSO node = pending.Dequeue();
                visited++;
                foreach (string id in GetNextNodeIds(node))
                {
                    DialogueNodeSO next = GetNode(id);
                    if (incoming.ContainsKey(next) && --incoming[next] == 0) pending.Enqueue(next);
                }
            }
            error = visited == incoming.Count ? null : "Condition/Event 노드끼리 순환합니다.";
            return error == null;
        }

        private bool TryBuildNodeMap(out string error)
        {
            _nodeMap = new Dictionary<string, DialogueNodeSO>();
            foreach (DialogueNodeSO node in nodes)
            {
                if (node == null || string.IsNullOrWhiteSpace(node.nodeId)
                    || !_nodeMap.TryAdd(node.nodeId, node))
                {
                    _nodeMap = null;
                    error = "노드 참조·ID가 없거나 ID가 중복됩니다.";
                    return false;
                }
            }
            error = null;
            return true;
        }

        private static bool IsImmediateNode(DialogueNodeSO node) =>
            node.nodeType == NodeType.Event || node.nodeType == NodeType.Condition;

        private static IEnumerable<string> GetNextNodeIds(DialogueNodeSO node)
        {
            switch (node.nodeType)
            {
                case NodeType.Talk:
                case NodeType.Event:
                    yield return node.nextNodeId;
                    break;
                case NodeType.Condition:
                    yield return node.trueNextNodeId;
                    yield return node.falseNextNodeId;
                    break;
                case NodeType.Choice:
                    if (node.choices == null) yield break;
                    foreach (ChoiceData choice in node.choices)
                        yield return choice?.nextNodeId;
                    break;
            }
        }

        public DialogueNodeSO StartNode => GetNode(startNodeId);

        /// <summary>현재 조건에서 재생될 경로의 인물만 준비한다. 선택 이후는 선택된 뒤 수집한다.</summary>
        public void CollectPresentationNodes(List<DialogueNodeSO> result, DialogueNodeSO start = null)
        {
            result.Clear();
            DialogueNodeSO node = start != null ? start : StartNode;
            var visited = new HashSet<DialogueNodeSO>();
            while (node != null && visited.Add(node))
            {
                result.Add(node);
                if (node.nodeType == NodeType.End || node.nodeType == NodeType.Choice) return;
                string nextId = node.nextNodeId;
                if (node.nodeType == NodeType.Condition)
                    nextId = node.condition != null && node.condition.Evaluate()
                        ? node.trueNextNodeId : node.falseNextNodeId;
                node = GetNode(nextId);
            }
        }

        // 에디터에서 노드 추가/삭제 시 캐시 무효화
        public void InvalidateCache() => _nodeMap = null;
    }
}
