using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UPlayGround.Manager;

namespace UPlayGround.Dialogue
{
    /// <summary>
    /// 채널별 독립 Runner를 보유하는 대화 관리자.
    /// Main/System은 단일 실행, Monologue는 큐로 순차 처리합니다.
    /// SpeakerColorTableSO를 Addressables로 로드해 Runner/UI에 제공합니다.
    /// </summary>
    public class DialogueManager : BaseManager<DialogueManager>, IManager
    {
        // UI 레이어가 구독하는 이벤트 — 채널별로 분리
        public event Action<DialogueNodeSO> OnMainNodeEnter;
        public event Action<DialogueNodeSO> OnSystemNodeEnter;
        public event Action<DialogueNodeSO> OnMonologueNodeEnter;
        public event Action<List<ChoiceData>> OnChoicePresented;
        public event Action OnDialogueEnd;

        private readonly Dictionary<DialogueChannel, DialogueRunner> _runners = new();

        // UI가 직접 참조하는 색상 테이블 — 로드 완료 전에는 null
        public SpeakerColorTableSO ColorTable { get; private set; }

        #region IManager

        public void Init()
        {
            _runners[DialogueChannel.Main]      = new DialogueRunner(DialogueChannel.Main,      this, enableQueue: false);
            _runners[DialogueChannel.System]    = new DialogueRunner(DialogueChannel.System,    this, enableQueue: false);
            _runners[DialogueChannel.Monologue] = new DialogueRunner(DialogueChannel.Monologue, this, enableQueue: true);

            LoadColorTable();
        }

        public void AfterInit()  { }

        public void Dispose()
        {
            foreach (var r in _runners.Values) r.Clear();

            // Addressables 핸들 해제
            if (ColorTable != null)
            {
                Addressables.Release(ColorTable);
                ColorTable = null;
            }
        }

        public void OnUpdate()      { }
        public void OnFixedUpdate() { }
        public void OnLateUpdate()  { }
        public void OnSceneChanged(string sceneType) { }

        #endregion

        // ── Public API ──────────────────────────────────────────────

        public void StartDialogue(DialogueGraphSO graph)
        {
            var channel = graph.StartNode.channel;
            _runners[channel].Enqueue(graph);
        }

        public void Advance(DialogueChannel channel = DialogueChannel.Main)
        {
            _runners[channel].Advance();
        }

        public void SelectChoice(int index)
        {
            _runners[DialogueChannel.Main].SelectChoice(index);
        }

        // ── Runner 이벤트 중계 ────────────────────────────────────────

        internal void NotifyNodeEnter(DialogueChannel channel, DialogueNodeSO node)
        {
            OpenUIForChannel(channel);
            switch (channel)
            {
                case DialogueChannel.Main:      OnMainNodeEnter?.Invoke(node);      break;
                case DialogueChannel.System:    OnSystemNodeEnter?.Invoke(node);    break;
                case DialogueChannel.Monologue: OnMonologueNodeEnter?.Invoke(node); break;
            }
        }

        internal void NotifyChoicePresented(List<ChoiceData> choices) =>
            OnChoicePresented?.Invoke(choices);

        internal void NotifyDialogueEnd(DialogueChannel channel)
        {
            HideUIForChannel(channel);
            OnDialogueEnd?.Invoke();
        }

        // ── Addressables 로드 ─────────────────────────────────────────

        private async void LoadColorTable()
        {
            var handle = Addressables.LoadAssetAsync<SpeakerColorTableSO>(SpeakerColorTableSO.AddressableKey);

            try
            {
                ColorTable = await handle.Task;
                Debug.Log("[DialogueManager] SpeakerColorTable 로드 완료");
            }
            catch (Exception e)
            {
                Debug.LogError($"[DialogueManager] SpeakerColorTable 로드 실패: {e.Message}");
            }
        }

        // ── UI 열기/닫기 ─────────────────────────────────────────────

        private static void OpenUIForChannel(DialogueChannel channel)
        {
            string key = ChannelToUIKey(channel);
            if (!UIManager.Instance.IsUIActive(key))
                UIManager.Instance.ShowUI(key);
        }

        private static void HideUIForChannel(DialogueChannel channel)
        {
            string key = ChannelToUIKey(channel);
            if (UIManager.Instance.IsUIActive(key))
                UIManager.Instance.HideUI(key);
        }

        private static string ChannelToUIKey(DialogueChannel channel) => channel switch
        {
            DialogueChannel.Main      => "MainDialogue",
            DialogueChannel.System    => "SystemDialogue",
            DialogueChannel.Monologue => "MonologueDialogue",
            _                         => "MainDialogue"
        };
    }

    // ─────────────────────────────────────────────────────────────────
    /// <summary>
    /// 단일 채널의 그래프 실행 FSM.
    /// enableQueue=true 이면 실행 중 요청된 그래프를 큐에 쌓아 순차 처리합니다.
    /// </summary>
    internal class DialogueRunner
    {
        private readonly DialogueChannel  _channel;
        private readonly DialogueManager  _manager;
        private readonly bool             _enableQueue;
        private readonly Queue<DialogueGraphSO> _queue = new();

        private DialogueGraphSO _currentGraph;
        private DialogueNodeSO  _currentNode;
        public  bool            IsRunning { get; private set; }

        public DialogueRunner(DialogueChannel channel, DialogueManager manager, bool enableQueue)
        {
            _channel     = channel;
            _manager     = manager;
            _enableQueue = enableQueue;
        }

        public void Enqueue(DialogueGraphSO graph)
        {
            if (!IsRunning)
            {
                Run(graph);
                return;
            }

            if (_enableQueue)
                _queue.Enqueue(graph);
            else
                Debug.LogWarning($"[Dialogue] {_channel} 채널 실행 중 — 새 그래프 무시됨: {graph.name}");
        }

        public void Advance()
        {
            if (!IsRunning || _currentNode == null) return;
            if (_currentNode.nodeType == NodeType.Choice) return;
            MoveToNode(_currentNode.nextNodeId);
        }

        public void SelectChoice(int index)
        {
            if (_currentNode?.nodeType != NodeType.Choice) return;
            if (index < 0 || index >= _currentNode.choices.Count) return;
            MoveToNode(_currentNode.choices[index].nextNodeId);
        }

        public void Clear()
        {
            _queue.Clear();
            IsRunning     = false;
            _currentGraph = null;
            _currentNode  = null;
        }

        // ── 내부 흐름 ───────────────────────────────────────────────

        private void Run(DialogueGraphSO graph)
        {
            _currentGraph = graph;
            IsRunning     = true;
            EnterNode(graph.StartNode);
        }

        private void MoveToNode(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId)) { End(); return; }

            var next = _currentGraph.GetNode(nodeId);
            if (next == null)
            {
                Debug.LogWarning($"[Dialogue] 노드를 찾을 수 없음: {nodeId}");
                End();
                return;
            }
            EnterNode(next);
        }

        private void EnterNode(DialogueNodeSO node)
        {
            _currentNode = node;

            foreach (var action in node.eventActions)
                action.Execute();

            switch (node.nodeType)
            {
                case NodeType.Talk:
                    _manager.NotifyNodeEnter(_channel, node);
                    break;

                case NodeType.Choice:
                    _manager.NotifyNodeEnter(_channel, node);
                    _manager.NotifyChoicePresented(GetVisibleChoices(node));
                    break;

                case NodeType.Condition:
                    var nextId = node.condition != null && node.condition.Evaluate()
                        ? node.trueNextNodeId
                        : node.falseNextNodeId;
                    MoveToNode(nextId);
                    break;

                case NodeType.Event:
                    MoveToNode(node.nextNodeId);
                    break;

                case NodeType.End:
                    End();
                    break;
            }
        }

        private void End()
        {
            IsRunning     = false;
            _currentGraph = null;
            _currentNode  = null;

            // 큐에 다음 그래프가 있으면 이어서 실행, 없으면 채널 종료 알림
            if (_enableQueue && _queue.Count > 0)
                Run(_queue.Dequeue());
            else
                _manager.NotifyDialogueEnd(_channel);
        }

        private static List<ChoiceData> GetVisibleChoices(DialogueNodeSO node)
        {
            var result = new List<ChoiceData>();
            foreach (var c in node.choices)
            {
                bool condMet = c.displayCondition == null || c.displayCondition.Evaluate();
                if (condMet || c.isGreyedOut) result.Add(c);
            }
            return result;
        }
    }
}
