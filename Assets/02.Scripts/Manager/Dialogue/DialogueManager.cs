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
using UPlayGround.Data.Party;

namespace UPlayGround.Dialogue
{
    /// <summary>
    /// 채널별 독립 Runner를 보유하는 대화 관리자.
    /// Main/System은 단일 실행, Monologue는 큐로 순차 처리합니다.
    /// SpeakerColorTableSO를 Addressables로 로드해 Runner/UI에 제공합니다.
    /// </summary>
    public class DialogueManager : BaseManager<DialogueManager>, IManager, IAsyncInitializableManager,
        IDialogueService, UPlayGround.UI.IUIDialogueService
    {
        // UI 레이어가 구독하는 이벤트 — 채널별로 분리
        public event Action<DialogueNodeSO> OnMainNodeEnter;
        public event Action<DialogueNodeSO> OnSystemNodeEnter;
        public event Action<DialogueNodeSO> OnMonologueNodeEnter;
        public event Action<List<ChoiceData>> OnChoicePresented;
        public event Action OnDialogueEnd;

        private readonly Dictionary<DialogueChannel, DialogueRunner> _runners = new();

        // 정지·자동·스킵 상태와 대화 이력의 단일 소유자. UI는 IUIDialogueService로만 접근한다.
        private readonly DialoguePlaybackController _playback = new();

        // UI가 직접 참조하는 색상 테이블 — 로드 완료 전에는 null
        public SpeakerColorTableSO ColorTable { get; private set; }
        public DialoguePaletteSO Palette { get; private set; }
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
            UniTask paletteTask = LoadPaletteAsync(cancellationToken);
            UniTask speakerBindingsTask = LoadSpeakerActorBindingsAsync(cancellationToken);
            await UniTask.WhenAll(colorTableTask, paletteTask, speakerBindingsTask);
        }

        public void AfterInit()  { }

        public void Dispose()
        {
            foreach (var r in _runners.Values) r.Clear();

            _playback.SetPaused(false);
            _playback.ClearHistory();

            ColorTable = null;
            Palette = null;
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
            TryStartDialogueTracked(graph, null);
        }

        public IDisposable TryStartDialogueTracked(DialogueGraphSO graph, Action onCompleted)
        {
            if (graph == null || graph.StartNode == null)
            {
                Debug.LogWarning("[Dialogue] 시작할 그래프 또는 StartNode가 없습니다.");
                return null;
            }

            DialogueChannel channel = graph.StartNode.channel;
            if (!_runners.TryGetValue(channel, out DialogueRunner runner))
                return null;

            var request = new DialogueRequest(graph, onCompleted);
            return runner.Enqueue(request) ? new DialogueRequestSubscription(request) : null;
        }

        public void Advance(DialogueChannel channel = DialogueChannel.Main)
        {
            // 정지의 의미를 명확히 하기 위해 정지 중에는 진행 요청 자체를 무시한다.
            if (_playback.IsPaused) return;

            _runners[channel].Advance();
        }

        public void SelectChoice(int index)
        {
            if (_playback.IsPaused) return;

            _runners[DialogueChannel.Main].SelectChoice(index);
        }

        // ── 재생 제어 (IUIDialogueService) ───────────────────────────

        public bool IsPaused => _playback.IsPaused;
        public bool IsAuto => _playback.IsAuto;
        public float AutoAdvanceDelay => _playback.AutoAdvanceDelay;
        public float TypingSpeedScale => _playback.TypingSpeedScale;
        public IReadOnlyList<DialogueLogEntry> History => _playback.History;

        public event Action<bool> OnPauseChanged
        {
            add    => _playback.OnPauseChanged += value;
            remove => _playback.OnPauseChanged -= value;
        }

        public event Action<bool> OnAutoChanged
        {
            add    => _playback.OnAutoChanged += value;
            remove => _playback.OnAutoChanged -= value;
        }

        public event Action OnHistoryChanged
        {
            add    => _playback.OnHistoryChanged += value;
            remove => _playback.OnHistoryChanged -= value;
        }

        public event Action OnTypingCompleteRequested
        {
            add    => _playback.OnTypingCompleteRequested += value;
            remove => _playback.OnTypingCompleteRequested -= value;
        }

        public void SetPaused(bool paused) => _playback.SetPaused(paused);

        public void SetAuto(bool auto) => _playback.SetAuto(auto);

        public void CompleteTyping() => _playback.RequestTypingComplete();

        public void RequestSkip(DialogueChannel channel = DialogueChannel.Main)
        {
            if (!_runners.TryGetValue(channel, out DialogueRunner runner))
                return;

            // 스킵은 정지 상태와 모순되므로 먼저 정지를 푼다.
            _playback.SetPaused(false);
            runner.SkipToBreak();
        }

        public void ClearHistory() => _playback.ClearHistory();

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

            // 정지 상태가 다음 대화로 새지 않도록 세션 종료 시 해제한다(자동 토글은 유지).
            _playback.ResetForSessionEnd();

            OnDialogueEnd?.Invoke();
        }

        /// <summary>
        /// Talk/Choice 노드 진입을 대화 이력에 기록합니다.
        /// 화자명·초상화는 뷰(UI_Dialogue)와 같은 해석 규칙을 쓰도록 DialogueSpeakerResolver로 공용화했습니다.
        /// </summary>
        internal void RecordNodeHistory(DialogueChannel channel, DialogueNodeSO node)
        {
            if (node == null || string.IsNullOrEmpty(node.dialogueText))
                return;

            var party = PartyManager.Instance;
            PartyMemberDataSO memberData = party != null ? party.PartyMemberDataSO : null;
            CharacterActorType activeType = party != null ? party.ActiveCharacterType : CharacterActorType.None;

            _playback.RecordHistory(new DialogueLogEntry(
                DialogueSpeakerResolver.ResolveSpeakerName(node, memberData, activeType),
                DialogueMarkup.ToRichText(node.dialogueText, Palette),
                channel,
                DialogueSpeakerResolver.ResolvePortrait(node, memberData, activeType)));
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

        private async UniTask LoadPaletteAsync(CancellationToken cancellationToken)
        {
            try
            {
                Palette = await AssetManager.Instance.LoadGlobalAsync<DialoguePaletteSO>(
                    DialoguePaletteSO.AddressableKey,
                    nameof(DialogueManager),
                    cancellationToken);

                Debug.Log("[DialogueManager] DialoguePalette 로드 완료");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                // 팔레트는 선택 기능이다. 없으면 [c:key]가 흰색으로 폴백될 뿐 대화는 정상 동작한다.
                Debug.LogWarning($"[DialogueManager] DialoguePalette 로드 실패 또는 미등록: {e.Message}");
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
            ShowIfHidden(ChannelToUIKey(channel));

            // 재생 컨트롤 바는 플레이어가 진행을 제어하는 채널에서만 띄운다(System은 알림형이라 제외).
            if (HasControlBar(channel))
                ShowIfHidden(DialogueUIKeys.DialogueControlBar);
        }

        private static void HideUIForChannel(DialogueChannel channel)
        {
            HideIfActive(ChannelToUIKey(channel));

            if (HasControlBar(channel))
            {
                HideIfActive(DialogueUIKeys.DialogueBacklog);
                HideIfActive(DialogueUIKeys.DialogueControlBar);
            }
        }

        private static bool HasControlBar(DialogueChannel channel) =>
            channel == DialogueChannel.Main || channel == DialogueChannel.Monologue;

        private static void ShowIfHidden(string key)
        {
            // 컨트롤 바·이력은 프로젝트에 아직 등록되지 않았을 수 있으므로 없으면 조용히 건너뛴다.
            if (UIManager.Instance.GetUIPrefabEntry(key) == null)
                return;

            if (!UIManager.Instance.IsUIActive(key))
                UIManager.Instance.ShowUI(key);
        }

        private static void HideIfActive(string key)
        {
            if (UIManager.Instance.IsUIActive(key))
                UIManager.Instance.HideUI(key);
        }

        private static string ChannelToUIKey(DialogueChannel channel) => channel switch
        {
            DialogueChannel.Main      => DialogueUIKeys.MainDialogue,
            DialogueChannel.System    => DialogueUIKeys.SystemDialogue,
            DialogueChannel.Monologue => DialogueUIKeys.MonologueDialogue,
            _                         => DialogueUIKeys.MainDialogue
        };
    }

    // ─────────────────────────────────────────────────────────────────
    /// <summary>
    /// 단일 채널의 그래프 실행 FSM.
    /// enableQueue=true 이면 실행 중 요청된 그래프를 큐에 쌓아 순차 처리합니다.
    /// </summary>
    internal sealed class DialogueRequest
    {
        private Action _onCompleted;

        public DialogueRequest(DialogueGraphSO graph, Action onCompleted)
        {
            Graph = graph;
            _onCompleted = onCompleted;
        }

        public DialogueGraphSO Graph { get; }

        public void Complete()
        {
            Action callback = _onCompleted;
            _onCompleted = null;
            callback?.Invoke();
        }

        public void DetachCallback()
        {
            _onCompleted = null;
        }
    }

    internal sealed class DialogueRequestSubscription : IDisposable
    {
        private DialogueRequest _request;

        public DialogueRequestSubscription(DialogueRequest request)
        {
            _request = request;
        }

        public void Dispose()
        {
            _request?.DetachCallback();
            _request = null;
        }
    }

    internal class DialogueRunner
    {
        private readonly DialogueChannel  _channel;
        private readonly DialogueManager  _manager;
        private readonly bool             _enableQueue;
        private readonly Queue<DialogueRequest> _queue = new();

        // 스킵 안전장치 — 순환 그래프에서 무한 루프를 막는 전이 횟수 상한
        private const int MaxSkipTransitions = 512;

        private readonly HashSet<string> _skipVisitedNodeIds = new();
        private bool _isSkipping;

        private DialogueRequest _currentRequest;
        private DialogueGraphSO _currentGraph;
        private DialogueNodeSO  _currentNode;
        public  bool            IsRunning { get; private set; }

        public DialogueRunner(DialogueChannel channel, DialogueManager manager, bool enableQueue)
        {
            _channel     = channel;
            _manager     = manager;
            _enableQueue = enableQueue;
        }

        public bool Enqueue(DialogueRequest request)
        {
            if (!IsRunning)
            {
                Run(request);
                return true;
            }

            if (_enableQueue)
            {
                _queue.Enqueue(request);
                return true;
            }
            else
                Debug.LogWarning($"[Dialogue] {_channel} 채널 실행 중 — 새 그래프 무시됨: {request.Graph.name}");

            return false;
        }

        public void Advance()
        {
            if (!IsRunning || _currentNode == null) return;
            if (_currentNode.nodeType == NodeType.Choice) return;
            MoveToNode(_currentNode.nextNodeId);
        }

        /// <summary>
        /// 대화 스킵(강) — 선택지(Choice) 또는 종료(End)를 만날 때까지 노드를 연속 진행합니다.
        /// 통과하는 노드의 eventActions(플래그·퀘스트)는 정상 실행되므로 진행 상태 부작용이 보존됩니다.
        /// 순환 그래프에서 멈추지 않는 것을 막기 위해 전이 횟수 상한과 방문 감지를 둡니다.
        /// </summary>
        public void SkipToBreak()
        {
            if (!IsRunning || _currentNode == null) return;

            // 이미 선택지에 서 있으면 스킵할 대상이 없다.
            if (_currentNode.nodeType == NodeType.Choice) return;

            _isSkipping = true;
            try
            {
                _skipVisitedNodeIds.Clear();

                int transitions = 0;
                while (IsRunning && _currentNode != null && _currentNode.nodeType != NodeType.Choice)
                {
                    if (++transitions > MaxSkipTransitions)
                    {
                        Debug.LogWarning(
                            $"[Dialogue] {_channel} 스킵 전이 상한({MaxSkipTransitions}) 초과 — 순환 그래프 의심. 중단합니다.");
                        break;
                    }

                    if (!string.IsNullOrEmpty(_currentNode.nodeId) &&
                        !_skipVisitedNodeIds.Add(_currentNode.nodeId))
                    {
                        Debug.LogWarning(
                            $"[Dialogue] {_channel} 스킵 중 노드 순환 감지: {_currentNode.nodeId} — 중단합니다.");
                        break;
                    }

                    DialogueNodeSO before = _currentNode;
                    Advance();

                    // Advance가 아무 전이도 일으키지 못하면(라우팅 누락 등) 무한 루프가 되므로 빠져나온다.
                    if (IsRunning && ReferenceEquals(before, _currentNode))
                        break;
                }
            }
            finally
            {
                _isSkipping = false;
                _skipVisitedNodeIds.Clear();
            }

            // 스킵 중 억제했던 UI 통지를 최종 착지 노드에 대해 한 번만 발행한다.
            if (IsRunning && _currentNode != null)
                NotifyEnterForCurrentNode();
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
            _skipVisitedNodeIds.Clear();
            _isSkipping   = false;
            IsRunning     = false;
            _currentRequest = null;
            _currentGraph = null;
            _currentNode  = null;
        }

        // ── 내부 흐름 ───────────────────────────────────────────────

        private void Run(DialogueRequest request)
        {
            _currentRequest = request;
            _currentGraph = request.Graph;
            IsRunning     = true;
            EnterNode(request.Graph.StartNode);
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
                case NodeType.Choice:
                    // 스킵으로 지나친 대사도 되짚어 볼 수 있어야 하므로 이력은 항상 남긴다.
                    _manager.RecordNodeHistory(_channel, node);

                    // 스킵 중에는 통과 노드마다 타이핑을 시작하지 않고, 착지 노드에서 한 번만 통지한다.
                    if (!_isSkipping)
                        NotifyEnterForCurrentNode();
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

        // 현재 노드 기준으로 UI 통지를 발행한다. 스킵 착지 시 재사용한다.
        private void NotifyEnterForCurrentNode()
        {
            DialogueNodeSO node = _currentNode;
            if (node == null) return;

            _manager.NotifyNodeEnter(_channel, node);

            if (node.nodeType == NodeType.Choice)
                _manager.NotifyChoicePresented(GetVisibleChoices(node));
        }

        private void End()
        {
            DialogueRequest completedRequest = _currentRequest;
            IsRunning     = false;
            _currentRequest = null;
            _currentGraph = null;
            _currentNode  = null;

            completedRequest?.Complete();

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
