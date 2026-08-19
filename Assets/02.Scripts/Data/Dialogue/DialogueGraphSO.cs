using System.Collections.Generic;
using System.Linq;
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
            _nodeMap ??= nodes.ToDictionary(n => n.nodeId);
            return _nodeMap.GetValueOrDefault(id);
        }

        public DialogueNodeSO StartNode => GetNode(startNodeId);

        // 에디터에서 노드 추가/삭제 시 캐시 무효화
        public void InvalidateCache() => _nodeMap = null;
    }
}
