using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UPlayGround.Manager;
using UPlayGround.CameraSystem;
using UPlayGround.Data;
using UPlayGround.Data.Actor;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Dialogue
{
    /// <summary>
    /// 채널별 독립 Runner를 보유하는 대화 관리자.
    /// Main/System은 단일 실행, Monologue는 큐로 순차 처리합니다.
    /// SpeakerColorTableSO를 Addressables로 로드해 Runner/UI에 제공합니다.
    /// </summary>
    public class DialogueManager : BaseManager<DialogueManager>, IManager, IAsyncInitializableManager
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
        public SpeakerActorBindingTableSO SpeakerActorBindings { get; private set; }

        #region IManager

        public void Init()
        {
            _runners[DialogueChannel.Main]      = new DialogueRunner(DialogueChannel.Main,      this, enableQueue: false);
            _runners[DialogueChannel.System]    = new DialogueRunner(DialogueChannel.System,    this, enableQueue: false);
            _runners[DialogueChannel.Monologue] = new DialogueRunner(DialogueChannel.Monologue, this, enableQueue: true);
        }

        public async UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            UniTask colorTableTask = LoadColorTableAsync(cancellationToken);
            UniTask speakerBindingsTask = LoadSpeakerActorBindingsAsync(cancellationToken);
            await UniTask.WhenAll(colorTableTask, speakerBindingsTask);
        }

        public void AfterInit()  { }

        public void Dispose()
        {
            foreach (var r in _runners.Values) r.Clear();

            ColorTable = null;
            SpeakerActorBindings = null;
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
            UpdateDialogueCamera(channel, node);

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
            if (channel == DialogueChannel.Main)
                CameraManager.Instance?.PopCameraMode();

            OnDialogueEnd?.Invoke();
        }

        // ── Addressables 로드 ─────────────────────────────────────────

        private async UniTask LoadColorTableAsync(CancellationToken cancellationToken)
        {
            try
            {
                ColorTable = await AssetManager.Instance.LoadGlobalAsync<SpeakerColorTableSO>(
                    SpeakerColorTableSO.AddressableKey,
                    nameof(DialogueManager),
                    cancellationToken);

                Debug.Log("[DialogueManager] SpeakerColorTable 로드 완료");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                Debug.LogError($"[DialogueManager] SpeakerColorTable 로드 실패: {e.Message}");
                throw;
            }
        }

        private async UniTask LoadSpeakerActorBindingsAsync(CancellationToken cancellationToken)
        {
            try
            {
                SpeakerActorBindings =
                    await AssetManager.Instance.LoadGlobalAsync<SpeakerActorBindingTableSO>(
                        SpeakerActorBindingTableSO.AddressableKey,
                        nameof(DialogueManager),
                        cancellationToken);
                Debug.Log("[DialogueManager] SpeakerActorBindingTable 로드 완료");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DialogueManager] SpeakerActorBindingTable 로드 실패 또는 미등록: {e.Message}");
            }
        }

        private void UpdateDialogueCamera(DialogueChannel channel, DialogueNodeSO node)
        {
            if (channel != DialogueChannel.Main || node == null)
                return;

            Transform speaker = ResolveSpeakerTransform(node.speakerId);
            if (speaker == null)
                return;

            // 노드에 사전 녹화가 지정되면 자동 추종 대신 녹화 카메라를 화자 기준으로 재생한다.
            // 완료 후 pop하지 않고(restorePreviousOnFinish=false) 마지막 프레임을 유지 → 다음 노드가 카메라 교체.
            if (node.cameraRecording != null)
            {
                CameraManager.Instance?.PushDialogueCameraRecording(
                    node.cameraRecording,
                    anchorOverride: BuildSpeakerAnchor(node.speakerId),
                    onComplete: null,
                    restorePreviousOnFinish: false);
                return;
            }

            Transform listener = GameObjectManager.Instance?.Player != null
                ? GameObjectManager.Instance.Player.transform
                : null;

            CameraManager.Instance?.PushDialogueCamera(speaker, listener);
        }

        // 녹화 카메라를 현재 화자 기준으로 앵커링한다 → 같은 녹화를 여러 화자에 재사용 가능.
        private CameraSnapshotActorReference BuildSpeakerAnchor(string speakerId)
        {
            string actorId = ResolveActorId(speakerId);
            if (string.IsNullOrEmpty(actorId))
                return CameraSnapshotActorReference.None();

            return new CameraSnapshotActorReference
            {
                enabled = true,
                useActivePlayerWhenEmpty = false,
                actorIdType = ActorIdType.None,
                actorId = actorId,
                socketType = ActorSocketType.Center
            };
        }

        private Transform ResolveSpeakerTransform(string speakerId)
        {
            string actorId = ResolveActorId(speakerId);
            if (string.IsNullOrEmpty(actorId))
                return null;

            GameActor actor = FindActorInstance(actorId);
            return actor != null ? actor.transform : null;
        }

        private string ResolveActorId(string speakerId)
        {
            if (string.IsNullOrEmpty(speakerId))
                return null;

            if (SpeakerActorBindings != null &&
                SpeakerActorBindings.TryGetActorId(speakerId, out string actorId))
                return actorId;

            return speakerId;
        }

        private static GameActor FindActorInstance(string actorId)
        {
            var objectManager = GameObjectManager.Instance;
            if (objectManager != null)
            {
                IReadOnlyList<GameActor> actors = objectManager.AllActors;
                for (int i = 0; i < actors.Count; i++)
                {
                    GameActor actor = actors[i];
                    if (actor != null && actor.ActorId == actorId)
                        return actor;
                }
            }

            var spawned = ActorSpawnManager.Instance?.GetSpawnedActors(actorId);
            if (spawned != null && spawned.Count > 0)
                return spawned[0];

            return null;
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
