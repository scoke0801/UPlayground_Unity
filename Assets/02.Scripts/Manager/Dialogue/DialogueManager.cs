using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Manager;

namespace UPlayGround.Dialogue
{
    /// <summary>
    /// 대화 흐름 FSM. UI는 이 Manager의 이벤트를 구독해 그리면 됩니다.
    /// </summary>
    public class DialogueManager : BaseManager<DialogueManager>, IManager
    {
        // UI 레이어가 구독하는 이벤트
        public event Action<DialogueNodeSO> OnNodeEnter;
        public event Action<List<ChoiceData>> OnChoicePresented;
        public event Action OnDialogueEnd;

        private DialogueGraphSO _currentGraph;
        private DialogueNodeSO  _currentNode;
        private bool _isRunning;
        
        #region IManager
        public void Init()
        {
        }

        public void AfterInit()
        {
        }

        public void Dispose()
        {
        }

        public void OnUpdate()
        {
        }

        public void OnFixedUpdate()
        {
        }

        public void OnLateUpdate()
        {
        }

        public void OnSceneChanged(string sceneType)
        {
        }
        #endregion
        // ── Public API ──────────────────────────────────────────────

        public void StartDialogue(DialogueGraphSO graph)
        {
            if (_isRunning) return;

            _currentGraph = graph;
            _isRunning    = true;
            EnterNode(graph.StartNode);
        }

        // 플레이어가 '다음' 버튼을 누를 때 — Talk/Event 노드에서 사용
        public void Advance()
        {
            if (!_isRunning || _currentNode == null) return;
            if (_currentNode.nodeType == NodeType.Choice) return; // 선택지는 SelectChoice 로만 진행

            MoveToNode(_currentNode.nextNodeId);
        }

        // 플레이어가 선택지를 골랐을 때
        public void SelectChoice(int index)
        {
            if (_currentNode?.nodeType != NodeType.Choice) return;
            if (index < 0 || index >= _currentNode.choices.Count) return;

            MoveToNode(_currentNode.choices[index].nextNodeId);
        }

        // ── 내부 흐름 ───────────────────────────────────────────────

        private void MoveToNode(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                EndDialogue();
                return;
            }

            var next = _currentGraph.GetNode(nodeId);
            if (next == null) { Debug.LogWarning($"[Dialogue] 노드를 찾을 수 없음: {nodeId}"); EndDialogue(); return; }

            EnterNode(next);
        }

        private void EnterNode(DialogueNodeSO node)
        {
            _currentNode = node;

            // 이 노드에 달린 게임 이벤트 먼저 실행
            foreach (var action in node.eventActions)
                action.Execute();

            switch (node.nodeType)
            {
                case NodeType.Talk:
                    OnNodeEnter?.Invoke(node);
                    break;

                case NodeType.Choice:
                    OnNodeEnter?.Invoke(node);
                    // 조건에 따라 필터링된 선택지 목록 전달
                    OnChoicePresented?.Invoke(GetVisibleChoices(node));
                    break;

                case NodeType.Condition:
                    // 분기 — UI에 노출 없이 즉시 이동
                    var next = node.condition != null && node.condition.Evaluate()
                        ? node.trueNextNodeId
                        : node.falseNextNodeId;
                    MoveToNode(next);
                    break;

                case NodeType.Event:
                    // 대사 없이 eventActions만 실행하고 다음으로
                    MoveToNode(node.nextNodeId);
                    break;

                case NodeType.End:
                    EndDialogue();
                    break;
            }
        }

        private List<ChoiceData> GetVisibleChoices(DialogueNodeSO node)
        {
            var result = new List<ChoiceData>();
            foreach (var c in node.choices)
            {
                bool condMet = c.displayCondition == null || c.displayCondition.Evaluate();
                if (condMet || c.isGreyedOut)
                    result.Add(c);
                // condMet==false && isGreyedOut==false → 완전히 숨김
            }
            return result;
        }

        private void EndDialogue()
        {
            _isRunning    = false;
            _currentNode  = null;
            _currentGraph = null;
            OnDialogueEnd?.Invoke();
        }
    }
}
