using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Manager;

namespace UPlayGround.FlowGraph
{
    public enum FlowTraceKind
    {
        Entry,
        NodeBegin,
        NodeEnd,
        Emit,
        BlackboardWrite,
        Breakpoint,
        Cancel,
        Exception,
    }

    [Serializable]
    public sealed class FlowTraceEvent
    {
        public long sequence;
        public int frame;
        public float realtime;
        public long contextId;
        public long parentContextId;
        public string graphId;
        public string nodeId;
        public string nodeName;
        public FlowTraceKind kind;
        public string port;
        public string valueName;
        public string valueSummary;
    }

    /// <summary>
    /// FlowGraphSO 하나를 로드해 진입점을 무장(Arm)하고, 발화 시 토큰을 흘려보내는 실행기.
    /// 씬에 배치하며, 비활성화 시 실행 중 코루틴·구독을 모두 정리한다
    /// (TriggerComposer.OnDisable의 _isExecuting 리셋 교훈 계승).
    /// </summary>
    public sealed class FlowGraphRunner : MonoBehaviour
    {
        private const int MaxImmediateNodeExecutionsPerFrame = 256;
        private const int TraceCapacity = 512;

        [SerializeField] private FlowGraphSO _graph;

        [Tooltip("FlowGraphManager에 graphId로 등록해 IFlowGraphService.StartGraph로 발화 가능하게 한다.")]
        [SerializeField] private bool _registerToManager = true;

        [Tooltip("씬이 준비되고 플레이어가 등록되면 지정 Manual 진입점을 한 번 발화한다. 저장 복원형 씬 플로우에 사용한다.")]
        [SerializeField] private bool _fireManualEntryOnStart;

        [SerializeField] private string _manualEntryIdOnStart = "Resume";

        // 진입점 발화 상태 (노드가 아닌 러너 소유 — 에셋 공유 오염 방지)
        private sealed class EntryFireState
        {
            public int FireCount;
            public float LastFireTime = float.NegativeInfinity;
        }

        private readonly Dictionary<string, EntryFireState> _entryStates = new();
        private readonly Dictionary<string, object> _runnerNodeStates = new();
        private readonly Dictionary<string, Action> _entryTeardowns = new();
        private readonly Dictionary<string, int> _activeNodeCounts = new();
        private readonly List<FlowContext> _activeContexts = new();
        private readonly HashSet<IDisposable> _activeNodeRoutines = new();
        private readonly Dictionary<string, int> _breakpointHitCounts = new();
        private readonly FlowTraceEvent[] _traceBuffer = new FlowTraceEvent[TraceCapacity];

        private bool _armed;
        private int _traceHead;
        private int _traceCount;
        private long _traceSequence;
        private long _nextContextId;

        public FlowGraphSO Graph => _graph;

        /// <summary>노드 id → 활성 토큰 수. 에디터 디버그 뷰가 폴링한다.</summary>
        public IReadOnlyDictionary<string, int> ActiveNodeCounts => _activeNodeCounts;

        /// <summary>실행 중인 플로우 컨텍스트 목록 (에디터 블랙보드 뷰용). 완료 시 자동 제거된다.</summary>
        public IReadOnlyList<FlowContext> ActiveContexts => _activeContexts;

        /// <summary>활성 노드 집합이 바뀔 때마다 증가 — 디버그 뷰의 증분 diff 게이트.</summary>
        public int DebugVersion { get; private set; }
        public int TraceVersion { get; private set; }

#if UNITY_EDITOR
        public bool IsDebugPaused { get; private set; }
        public string PausedNodeId { get; private set; }
        private bool _pauseBeforeNextNode;
#endif

#if UNITY_EDITOR
        // 에디터 디버그 트레이스 — "최근 실행" 잔광/엣지 흐름 하이라이트용 (빌드 제외)
        private readonly Dictionary<string, float> _lastNodeExecuteTimes = new();
        private readonly Dictionary<string, float> _lastEdgeEmitTimes = new();

        /// <summary>노드 id → 마지막 실행 시각(realtimeSinceStartup). 순간 통과 노드의 잔광 표시용.</summary>
        public IReadOnlyDictionary<string, float> LastNodeExecuteTimes => _lastNodeExecuteTimes;

        /// <summary>"fromId:port:toId" → 마지막 토큰 통과 시각. 엣지 흐름 하이라이트용.</summary>
        public IReadOnlyDictionary<string, float> LastEdgeEmitTimes => _lastEdgeEmitTimes;
#endif

        /// <summary>그래프 교체(동적 로드/테스트용). 진입점 무장 상태 오염을 막기 위해 비활성 상태에서만 허용.</summary>
        public void SetGraph(FlowGraphSO graph, bool registerToManager = true)
        {
            if (isActiveAndEnabled)
            {
                Debug.LogWarning("[FlowGraph] SetGraph는 러너가 비활성일 때만 호출할 수 있다.", this);
                return;
            }
            _graph = graph;
            _registerToManager = registerToManager;
        }

        private void OnEnable()
        {
            if (_graph == null)
                return;

            ArmEntries();
            if (_registerToManager)
                FlowGraphManager.Instance?.RegisterRunner(this);
        }

        private void OnDisable()
        {
            if (_registerToManager)
                FlowGraphManager.Instance?.UnregisterRunner(this);
            DisarmEntries();
            CancelAll();
        }

        private IEnumerator Start()
        {
            if (!_fireManualEntryOnStart || _graph == null)
                yield break;

            while (isActiveAndEnabled
                   && (!Services.TryGet<IActorQueryService>(out IActorQueryService actors)
                       || actors.Player == null))
            {
                yield return null;
            }

            if (isActiveAndEnabled)
                FireManualEntries(_manualEntryIdOnStart);
        }

        // ──────────────────────────────────────────────────────────
        #region 진입점 무장/발화

        private void ArmEntries()
        {
            if (_armed)
                return;
            _armed = true;

            foreach (FlowNode node in _graph.nodes)
            {
                if (node is EntryNode entry)
                    entry.Arm(this);
            }
        }

        private void DisarmEntries()
        {
            if (!_armed)
                return;
            _armed = false;

            foreach (Action teardown in _entryTeardowns.Values)
                teardown?.Invoke();
            _entryTeardowns.Clear();
        }

        /// <summary>GameManager 서비스 등록 완료 후 초기 무장 실패 가능성을 제거하기 위해 구독을 다시 구성한다.</summary>
        internal void RefreshEntrySubscriptions()
        {
            if (!isActiveAndEnabled || _graph == null)
                return;

            DisarmEntries();
            ArmEntries();
        }

        /// <summary>EntryNode.Arm에서 등록한 구독의 해제 동작을 러너가 보관한다.</summary>
        public void StoreEntryTeardown(EntryNode entry, Action teardown)
        {
            if (entry == null || teardown == null)
                return;

            if (_entryTeardowns.TryGetValue(entry.id, out Action existing))
                teardown = existing + teardown;
            _entryTeardowns[entry.id] = teardown;
        }

        /// <summary>진입점을 발화한다. 재진입 정책을 통과하지 못하면 false.</summary>
        public bool FireEntry(EntryNode entry, Action<FlowContext> configure = null)
        {
            return FireEntryInGraph(_graph, entry, null, configure) != null;
        }

        /// <summary>
        /// 지정 그래프의 진입점을 발화하고 실행 컨텍스트를 반환한다 (SubGraph 중첩 실행 지원).
        /// parent가 있으면 발화 원인(Collider/Actor)과 중첩 깊이를 상속한다.
        /// </summary>
        internal FlowContext FireEntryInGraph(
            FlowGraphSO graph,
            EntryNode entry,
            FlowContext parent,
            Action<FlowContext> configure = null)
        {
            if (entry == null || graph == null || !isActiveAndEnabled)
                return null;

            if (!PassRepeatPolicy(graph, entry))
                return null;

            var context = new FlowContext(this, entry);
            context.Graph = graph;
            context.ContextId = ++_nextContextId;
            context.ParentContextId = parent?.ContextId ?? 0;
            // 그래프 선언 변수를 기본값으로 초기화 (발화마다 독립 사본)
            for (int i = 0; i < graph.variables.Count; i++)
            {
                FlowVariableDef def = graph.variables[i];
                if (def != null && !string.IsNullOrEmpty(def.name))
                    context.Set(def.name, def.GetDefaultValue());
            }
            for (int i = 0; i < graph.parameters.Count; i++)
            {
                FlowGraphParameterDef parameter = graph.parameters[i];
                if (parameter != null && !string.IsNullOrEmpty(parameter.name))
                    context.Set(parameter.name, parameter.GetDefaultValue());
            }
            if (parent != null)
            {
                context.Collider = parent.Collider;
                context.Actor = parent.Actor;
                context.Depth = parent.Depth + 1;
            }
            configure?.Invoke(context);
            _activeContexts.Add(context);
            FlowProgressState.MarkEntryStarted(graph.ResolvedGraphId, entry.id);
            RecordTrace(FlowTraceKind.Entry, context, graph, entry);
            StartCoroutine(TokenRoutine(context, graph, entry));
            return context;
        }

        /// <summary>entryId가 일치하는 Manual 진입점 발화. entryId가 비면 모든 Manual 진입점.</summary>
        public bool FireManualEntries(string entryId, Action<FlowContext> configure = null)
        {
            bool fired = false;
            foreach (FlowNode node in _graph.nodes)
            {
                if (node is ManualEntryNode entry
                    && (string.IsNullOrEmpty(entryId) || entry.entryId == entryId))
                {
                    fired |= FireEntry(entry, configure);
                }
            }
            return fired;
        }

        /// <summary>조건에 맞는 진입점들을 발화한다. (트리거 볼륨 등 프록시 컴포넌트용)</summary>
        public bool FireEntries<TEntry>(Predicate<TEntry> match, Action<FlowContext> configure = null)
            where TEntry : EntryNode
        {
            bool fired = false;
            foreach (FlowNode node in _graph.nodes)
            {
                if (node is TEntry entry && (match == null || match(entry)))
                    fired |= FireEntry(entry, configure);
            }
            return fired;
        }

        private bool PassRepeatPolicy(FlowGraphSO graph, EntryNode entry)
        {
            string stateKey = $"{graph.GetInstanceID()}:{entry.id}";
            if (!_entryStates.TryGetValue(stateKey, out EntryFireState state))
            {
                state = new EntryFireState();
                _entryStates[stateKey] = state;
            }

            switch (entry.repeatPolicy)
            {
                case FlowRepeatPolicy.Once:
                    if (state.FireCount > 0)
                        return false;
                    break;

                case FlowRepeatPolicy.OncePerSession:
                    if (!FlowProgressState.TryMarkFired($"{graph.ResolvedGraphId}:{entry.id}"))
                        return false;
                    break;

                case FlowRepeatPolicy.OncePerSave:
                    if (!FlowProgressState.TryMarkFiredPersistent(
                            $"entry:{graph.ResolvedGraphId}:{entry.id}"))
                        return false;
                    break;

                case FlowRepeatPolicy.Cooldown:
                    if (Time.time - state.LastFireTime < entry.cooldownSeconds)
                        return false;
                    break;
            }

            state.FireCount++;
            state.LastFireTime = Time.time;
            return true;
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 토큰 실행

        /// <summary>from 노드의 출력 포트에 연결된 모든 대상 노드로 토큰을 전파한다.</summary>
        internal void EmitToken(FlowContext context, FlowGraphSO graph, FlowNode from, string port)
        {
            if (context.Cancelled || !isActiveAndEnabled)
                return;

            // StartCoroutine은 첫 yield까지 즉시 실행될 수 있다. 러너 공용 버퍼를 쓰면
            // 하위 노드의 재진입 Emit이 부모 fan-out 목록을 덮어쓰므로 호출별 스냅샷을 사용한다.
            var connections = new List<FlowConnection>();
            graph.GetConnectionsFrom(from.id, port, connections);

            for (int i = 0; i < connections.Count; i++)
            {
                FlowNode target = graph.GetNode(connections[i].toNodeId);
                if (target == null)
                {
                    Debug.LogWarning($"[FlowGraph] {graph.name}: {from.id}.{port} 연결 대상 노드 유실", this);
                    continue;
                }
#if UNITY_EDITOR
                _lastEdgeEmitTimes[$"{from.id}:{port}:{connections[i].toNodeId}"] = Time.realtimeSinceStartup;
#endif
                RecordTrace(
                    FlowTraceKind.Emit,
                    context,
                    graph,
                    from,
                    port: port,
                    valueSummary: target.DisplayName);
                StartCoroutine(TokenRoutine(context, graph, target));
            }
        }

        private IEnumerator TokenRoutine(FlowContext context, FlowGraphSO graph, FlowNode node)
        {
            if (context.Cancelled)
                yield break;

            if (!context.TryConsumeExecutionBudget(MaxImmediateNodeExecutionsPerFrame))
            {
                Debug.LogError(
                    $"[FlowGraph] {graph.name}: 한 프레임 노드 실행 한도({MaxImmediateNodeExecutionsPerFrame}) 초과 — " +
                    "대기 없는 엣지 사이클을 확인하세요.",
                    this);
                yield break;
            }

            IncrementActive(node);
            context.ActiveTokenCount++;
            RecordTrace(FlowTraceKind.NodeBegin, context, graph, node);

#if UNITY_EDITOR
            _lastNodeExecuteTimes[node.id] = Time.realtimeSinceStartup;
            bool pauseHere = _pauseBeforeNextNode || ShouldPauseAtBreakpoint(node, context);
            if (pauseHere)
            {
                _pauseBeforeNextNode = false;
                IsDebugPaused = true;
                PausedNodeId = node.id;
                RecordTrace(FlowTraceKind.Breakpoint, context, graph, node);
                while (IsDebugPaused && !context.Cancelled && isActiveAndEnabled)
                    yield return null;
            }
#endif

            var token = new FlowToken(context, graph, node);
            IEnumerator routine = null;
            IDisposable disposableRoutine = null;
            try
            {
                try
                {
                    routine = node.Execute(token);
                    disposableRoutine = routine as IDisposable;
                    if (disposableRoutine != null)
                        _activeNodeRoutines.Add(disposableRoutine);
                }
                catch (Exception e)
                {
                    RecordTrace(
                        FlowTraceKind.Exception,
                        context,
                        graph,
                        node,
                        valueSummary: e.Message);
                    Debug.LogError($"[FlowGraph] {graph.name}/{node.DisplayName} 실행 예외: {e}", this);
                }

                while (routine != null && !context.Cancelled)
                {
                    bool moved;
                    try
                    {
                        moved = routine.MoveNext();
                    }
                    catch (Exception e)
                    {
                        RecordTrace(
                            FlowTraceKind.Exception,
                            context,
                            graph,
                            node,
                            valueSummary: e.Message);
                        Debug.LogError($"[FlowGraph] {graph.name}/{node.DisplayName} 실행 예외: {e}", this);
                        break;
                    }

                    if (!moved)
                        break;
                    yield return routine.Current;
                }
            }
            finally
            {
                if (disposableRoutine != null && _activeNodeRoutines.Remove(disposableRoutine))
                {
                    try
                    {
                        disposableRoutine.Dispose();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[FlowGraph] {graph.name}/{node.DisplayName} 정리 예외: {e}", this);
                    }
                }

                context.ActiveTokenCount--;
                DecrementActive(node);
                RecordTrace(FlowTraceKind.NodeEnd, context, graph, node);

                // 노드 Execute 중에 후속 토큰이 먼저 증가하므로, 0이면 이 플로우는 완주된 것이다.
                if (context.ActiveTokenCount <= 0 && _activeContexts.Remove(context))
                {
                    // 취소(러너 비활성/씬 전환)는 완주가 아니다 — 완주 기록만 진행도로 남긴다.
                    if (!context.Cancelled)
                    {
                        FlowProgressState.MarkEntryCompleted(
                            (context.Graph != null ? context.Graph : graph).ResolvedGraphId,
                            context.Entry != null ? context.Entry.id : null);
                    }
                    context.DisposeTeardowns();
                }
            }
        }

        /// <summary>
        /// 컨텍스트(발화)를 넘어 러너 수명 동안 유지되는 노드별 상태(예: Gate 쿨다운).
        /// 노드 인스턴스는 에셋 공유이므로 상태는 러너가 소유한다.
        /// </summary>
        public T GetRunnerNodeState<T>(FlowGraphSO graph, FlowNode node) where T : class, new()
        {
            string stateKey = $"{graph.GetInstanceID()}:{node.id}";
            if (!_runnerNodeStates.TryGetValue(stateKey, out object state) || state is not T typed)
            {
                typed = new T();
                _runnerNodeStates[stateKey] = typed;
            }
            return typed;
        }

        /// <summary>실행 중인 모든 플로우를 취소하고 상태를 정리한다.</summary>
        public void CancelAll()
        {
            for (int i = 0; i < _activeContexts.Count; i++)
            {
                RecordTrace(
                    FlowTraceKind.Cancel,
                    _activeContexts[i],
                    _graph,
                    _activeContexts[i].Entry);
                _activeContexts[i].Cancelled = true;
                _activeContexts[i].DisposeTeardowns();
            }
            _activeContexts.Clear();

            if (_activeNodeRoutines.Count > 0)
            {
                var routines = new IDisposable[_activeNodeRoutines.Count];
                _activeNodeRoutines.CopyTo(routines);
                _activeNodeRoutines.Clear();
                for (int i = 0; i < routines.Length; i++)
                {
                    try
                    {
                        routines[i]?.Dispose();
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e, this);
                    }
                }
            }

            StopAllCoroutines();
            _activeNodeCounts.Clear();
#if UNITY_EDITOR
            _lastNodeExecuteTimes.Clear();
            _lastEdgeEmitTimes.Clear();
#endif
            DebugVersion++;
#if UNITY_EDITOR
            IsDebugPaused = false;
            PausedNodeId = null;
            _pauseBeforeNextNode = false;
#endif
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void RecordTrace(
            FlowTraceKind kind,
            FlowContext context,
            FlowGraphSO graph,
            FlowNode node,
            string port = null,
            string valueName = null,
            string valueSummary = null)
        {
            var trace = new FlowTraceEvent
            {
                sequence = ++_traceSequence,
                frame = Time.frameCount,
                realtime = Time.realtimeSinceStartup,
                contextId = context?.ContextId ?? 0,
                parentContextId = context?.ParentContextId ?? 0,
                graphId = graph != null ? graph.ResolvedGraphId : string.Empty,
                nodeId = node?.id,
                nodeName = node?.DisplayName,
                kind = kind,
                port = port,
                valueName = valueName,
                valueSummary = valueSummary,
            };

            int writeIndex = (_traceHead + _traceCount) % TraceCapacity;
            if (_traceCount == TraceCapacity)
            {
                writeIndex = _traceHead;
                _traceHead = (_traceHead + 1) % TraceCapacity;
            }
            else
            {
                _traceCount++;
            }
            _traceBuffer[writeIndex] = trace;
            TraceVersion++;
        }

        internal void RecordBlackboardChange(FlowContext context, string key, object value)
        {
            RecordTrace(
                FlowTraceKind.BlackboardWrite,
                context,
                context?.Graph ?? _graph,
                null,
                valueName: key,
                valueSummary: FormatTraceValue(value));
        }

        public void GetTraceSnapshot(List<FlowTraceEvent> results)
        {
            if (results == null)
                return;
            results.Clear();
            for (int i = 0; i < _traceCount; i++)
            {
                FlowTraceEvent trace = _traceBuffer[(_traceHead + i) % TraceCapacity];
                if (trace != null)
                    results.Add(trace);
            }
        }

        public void ClearTrace()
        {
            Array.Clear(_traceBuffer, 0, _traceBuffer.Length);
            _traceHead = 0;
            _traceCount = 0;
            TraceVersion++;
        }

        private static string FormatTraceValue(object value)
        {
            if (value == null)
                return "null";
            string text = value.ToString();
            return text.Length <= 160 ? text : text.Substring(0, 157) + "...";
        }

#if UNITY_EDITOR
        private bool ShouldPauseAtBreakpoint(FlowNode node, FlowContext context)
        {
            if (!node.breakpoint || node.breakpointDisabled)
                return false;

            _breakpointHitCounts.TryGetValue(node.id, out int hits);
            hits++;
            _breakpointHitCounts[node.id] = hits;
            if (node.breakpointAfterHits > 0 && hits < node.breakpointAfterHits)
                return false;

            if (!string.IsNullOrEmpty(node.breakpointVariable))
            {
                if (node.breakpointExpected == null
                    || !context.TryGet<object>(node.breakpointVariable, out object value)
                    || !node.breakpointExpected.Matches(value))
                {
                    return false;
                }
            }
            return true;
        }

        public void DebugContinue()
        {
            IsDebugPaused = false;
            PausedNodeId = null;
            _pauseBeforeNextNode = false;
        }

        public void DebugStep()
        {
            if (!IsDebugPaused)
                return;
            _pauseBeforeNextNode = true;
            IsDebugPaused = false;
            PausedNodeId = null;
        }

        public void DebugStop()
        {
            CancelAll();
        }
#endif

        private void IncrementActive(FlowNode node)
        {
            _activeNodeCounts.TryGetValue(node.id, out int count);
            _activeNodeCounts[node.id] = count + 1;
            DebugVersion++;
        }

        private void DecrementActive(FlowNode node)
        {
            if (!_activeNodeCounts.TryGetValue(node.id, out int count))
                return;

            if (count <= 1)
                _activeNodeCounts.Remove(node.id);
            else
                _activeNodeCounts[node.id] = count - 1;
            DebugVersion++;
        }

        #endregion
    }
}
