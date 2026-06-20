using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Component;
using UPlayGround.Manager;

namespace UPlayGround.AI.BehaviorTree
{
    public class BehaviorTreeRunner : MonoBehaviour, IManagedTick
    {
        [Header("Tree")]
        [SerializeField] private BehaviorTreeAsset _treeAsset;
        [SerializeField] private bool _startOnEnable = true;

        [Header("Tick")]
        [SerializeField] private BehaviorTreeRunnerMode _tickMode = BehaviorTreeRunnerMode.UpdateInterval;
        [SerializeField] private float _tickInterval = 0.1f;
        [SerializeField] private bool _restartWhenComplete;
        [SerializeField] private bool _resetValuesOnRestart = true;
        [SerializeField] private bool _debugMode;
        [SerializeField] private int _debugTraceCapacity = 128;

        [Header("Tick LOD (거리 기반 평가 빈도 감쇠)")]
        [Tooltip("타겟과의 거리에 따라 BT 평가 간격을 늘려 원거리/화면 밖 적의 풀 평가 부하를 낮춘다. 동작 의미는 유지된다.\n" +
                 "주의: 일반 멜리 적은 보통 탐지반경(lostTargetRadius≈15m) 밖에서 타겟을 놓으므로 거의 발동하지 않는다(무해).\n" +
                 "발동 대상은 장거리 타겟을 유지하는 아키타입(원거리 카이터/보스). 이들은 거리 반응성이 중요하므로 near를 교전거리 위로 두거나 끌 것.")]
        [SerializeField] private bool _useDistanceLod = true;
        [Tooltip("이 거리 이내에서는 기본 간격으로 풀 레이트 평가한다. 카이터 교전거리(≈15~20m)를 덮도록 둔다.")]
        [SerializeField] private float _lodNearDistance = 20f;
        [Tooltip("이 거리 이상에서는 감쇠 배율이 최대로 적용된다.")]
        [SerializeField] private float _lodFarDistance = 45f;
        [Tooltip("최원거리에서의 틱 간격 배율(예: 2 = 간격 2배 → 평가 빈도 1/2).")]
        [SerializeField] private float _lodFarIntervalScale = 2f;

        private BehaviorTreeAsset _runtimeTree;
        private BehaviorTreeContext _context;
        private float _tickTimer;
        private BehaviorTreeRunnerState _state = BehaviorTreeRunnerState.Stopped;
        private bool _pauseRequested;
        private BTNode _pauseRequestedBy;
        private AgentTickManager _tickManager;
        private EnemyDetection _detection;
        private bool _detectionResolved;

        public BehaviorTreeAsset SourceTree => _treeAsset;
        public BehaviorTreeAsset RuntimeTree => _runtimeTree;
        public BehaviorTreeContext Context => _context;
        public BehaviorTreeDebugTrace DebugTrace { get; private set; }
        public BTNode PauseRequestedBy => _pauseRequestedBy;
        public BTStatus ExecutionStatus { get; private set; } = BTStatus.Failure;
        public bool IsRunning => _state == BehaviorTreeRunnerState.Running;
        public bool IsPaused => _state == BehaviorTreeRunnerState.Paused;
        public BehaviorTreeRunnerState State => _state;
        public bool DebugMode => _debugMode;

        public void SetTreeAsset(BehaviorTreeAsset treeAsset, bool restartIfRunning = true)
        {
            if (_treeAsset == treeAsset)
                return;

            bool wasRunning = IsRunning || IsPaused;
            _treeAsset = treeAsset;

            if (restartIfRunning && wasRunning)
                StartTree();
        }

        private void OnEnable()
        {
            // 개별 Update 대신 AgentTickManager가 일괄 틱한다.
            if (Application.isPlaying)
            {
                _tickManager = AgentTickManager.Instance;
                _tickManager?.Register(this);
            }

            if (_startOnEnable)
                StartTree();
        }

        private void OnDisable()
        {
            _tickManager?.Unregister(this);
            _tickManager = null;
            StopTree();
        }

        private void OnValidate()
        {
            // DebugTrace는 StartTree/Restart 시점의 _debugMode로만 생성된다. 플레이 도중 인스펙터에서
            // Debug Mode를 켜면 _debugMode만 true가 되고 트레이스 버퍼는 null로 남아 Trace 탭/per-tick
            // 하이라이트가 비는 문제가 있어, 런타임 토글에도 버퍼를 지연 생성/해제한다.
            if (!Application.isPlaying)
                return;

            if (_debugMode && _runtimeTree != null && DebugTrace == null)
                DebugTrace = new BehaviorTreeDebugTrace(_debugTraceCapacity);
            else if (!_debugMode)
                DebugTrace = null;
        }

        /// <summary>
        /// <see cref="AgentTickManager"/>가 매 프레임 호출. 기존 Update 본문과 동일하다.
        /// </summary>
        public void ManagedTick(float deltaTime)
        {
            if (_state != BehaviorTreeRunnerState.Running || _runtimeTree?.RootNode == null || _tickMode == BehaviorTreeRunnerMode.Manual)
                return;

            if (_tickMode == BehaviorTreeRunnerMode.UpdateInterval)
            {
                _tickTimer += deltaTime;
                if (_tickTimer < GetEffectiveTickInterval())
                    return;

                _tickTimer = 0f;
            }

            TickOnce();
        }

        /// <summary>
        /// 타겟과의 거리에 따라 기본 틱 간격을 늘려 평가 빈도를 낮춘다(거리 기반 LOD).
        /// 근접(_lodNearDistance 이내)은 기본 간격을 그대로 쓰고, 원거리로 갈수록 _lodFarIntervalScale까지 선형 보간한다.
        /// EnemyDetection이 없거나 타겟이 없으면 항상 기본 간격을 사용해 기존 동작을 보존한다.
        /// </summary>
        private float GetEffectiveTickInterval()
        {
            float interval = Mathf.Max(0.01f, _tickInterval);

            if (!_useDistanceLod)
                return interval;

            // 적이 아닌 러너에는 EnemyDetection이 없을 수 있다. 한 번만 조회해 캐싱한다.
            if (!_detectionResolved)
            {
                _detection = GetComponent<EnemyDetection>();
                _detectionResolved = true;
            }

            if (_detection == null || !_detection.HasTarget)
                return interval;

            float dist = _detection.DistanceToTarget;
            if (dist <= _lodNearDistance)
                return interval;

            float t = _lodFarDistance > _lodNearDistance
                ? Mathf.Clamp01((dist - _lodNearDistance) / (_lodFarDistance - _lodNearDistance))
                : 1f;
            float scale = Mathf.Lerp(1f, Mathf.Max(1f, _lodFarIntervalScale), t);
            return interval * scale;
        }

        public void StartTree()
        {
            StopTree();

            if (_treeAsset == null || _treeAsset.RootNode == null)
                return;

            _runtimeTree = _treeAsset.CloneRuntime();
            DebugTrace = _debugMode ? new BehaviorTreeDebugTrace(_debugTraceCapacity) : null;
            _context = new BehaviorTreeContext(gameObject, _runtimeTree.Blackboard, this);
            _runtimeTree.RootNode.Initialize(_context);
            _state = BehaviorTreeRunnerState.Running;
            _tickTimer = _tickInterval;
            ExecutionStatus = BTStatus.Running;
        }

        public void StopTree()
        {
            if (_runtimeTree?.RootNode != null)
                _runtimeTree.RootNode.Abort();

            BehaviorTreeAsset.DisposeRuntime(_runtimeTree);
            _runtimeTree = null;
            _context = null;
            DebugTrace = null;
            _pauseRequested = false;
            _pauseRequestedBy = null;
            _state = BehaviorTreeRunnerState.Stopped;
            _tickTimer = 0f;
            ExecutionStatus = BTStatus.Failure;
        }

        public void RestartTree()
        {
            RestartRuntimeTree(_resetValuesOnRestart);
        }

        public void EnableBehavior()
        {
            if (_state == BehaviorTreeRunnerState.Paused)
            {
                ResumeTree();
                return;
            }

            if (_state == BehaviorTreeRunnerState.Stopped)
                StartTree();
        }

        public void DisableBehavior(bool pause)
        {
            if (pause)
                PauseTree();
            else
                StopTree();
        }

        public void PauseTree()
        {
            if (_state == BehaviorTreeRunnerState.Running)
                _state = BehaviorTreeRunnerState.Paused;
        }

        public void ResumeTree()
        {
            if (_state == BehaviorTreeRunnerState.Paused)
                _state = BehaviorTreeRunnerState.Running;
        }

        public BTStatus TickOnce()
        {
            return TickOnce(false);
        }

        public BTStatus StepTick()
        {
            return TickOnce(true);
        }

        public void RequestPauseFromNode(BTNode node)
        {
            _pauseRequested = true;
            _pauseRequestedBy = node;
            DebugTrace?.Record(node, "Breakpoint", BTStatus.Running, "Breakpoint 요청으로 Tick 종료 후 Pause됩니다.");
        }

        private BTStatus TickOnce(bool allowPaused)
        {
            if (_state == BehaviorTreeRunnerState.Stopped)
                StartTree();

            if (_runtimeTree?.RootNode == null)
                return ExecutionStatus;

            if (_state != BehaviorTreeRunnerState.Running && !(allowPaused && _state == BehaviorTreeRunnerState.Paused))
                return ExecutionStatus;

            if (ExecutionStatus != BTStatus.Running && _restartWhenComplete)
                RestartRuntimeTree(_resetValuesOnRestart);

            _pauseRequested = false;
            _pauseRequestedBy = null;
            DebugTrace?.BeginTick();
            ExecutionStatus = _runtimeTree.RootNode.Tick();
            if (_pauseRequested)
                _state = BehaviorTreeRunnerState.Paused;

            return ExecutionStatus;
        }

        private void RestartRuntimeTree(bool resetBlackboardValues)
        {
            var previousBlackboard = _runtimeTree?.Blackboard;
            var blackboard = resetBlackboardValues ? null : (previousBlackboard != null ? previousBlackboard.Clone() : null);

            if (_runtimeTree?.RootNode != null)
                _runtimeTree.RootNode.Abort();

            BehaviorTreeAsset.DisposeRuntime(_runtimeTree);
            _runtimeTree = null;
            _context = null;
            _tickTimer = _tickInterval;
            ExecutionStatus = BTStatus.Failure;

            if (_treeAsset == null || _treeAsset.RootNode == null)
            {
                _state = BehaviorTreeRunnerState.Stopped;
                return;
            }

            _runtimeTree = _treeAsset.CloneRuntime(blackboard);
            DebugTrace = _debugMode ? new BehaviorTreeDebugTrace(_debugTraceCapacity) : null;
            _context = new BehaviorTreeContext(gameObject, _runtimeTree.Blackboard, this);
            _runtimeTree.RootNode.Initialize(_context);
            _state = BehaviorTreeRunnerState.Running;
            ExecutionStatus = BTStatus.Running;
        }
    }

    [Serializable]
    public readonly struct BehaviorTreeDebugTraceRecord
    {
        public BehaviorTreeDebugTraceRecord(int tick, string nodeGuid, string nodeName, string eventType, BTStatus status, string detail)
        {
            Tick = tick;
            NodeGuid = nodeGuid;
            NodeName = nodeName;
            EventType = eventType;
            Status = status;
            Detail = detail;
            Time = UnityEngine.Time.time;
        }

        public int Tick { get; }
        public float Time { get; }
        public string NodeGuid { get; }
        public string NodeName { get; }
        public string EventType { get; }
        public BTStatus Status { get; }
        public string Detail { get; }
    }

    public class BehaviorTreeDebugTrace
    {
        private readonly Queue<BehaviorTreeDebugTraceRecord> _records = new();
        private readonly int _capacity;

        public BehaviorTreeDebugTrace(int capacity = 128)
        {
            _capacity = Math.Max(16, capacity);
        }

        public int CurrentTick { get; private set; }
        public int Version { get; private set; }
        public IReadOnlyCollection<BehaviorTreeDebugTraceRecord> Records => _records;

        public void BeginTick()
        {
            CurrentTick++;
        }

        public void Record(BTNode node, string eventType, BTStatus status, string detail = "")
        {
            if (node == null)
                return;

            _records.Enqueue(new BehaviorTreeDebugTraceRecord(CurrentTick, node.Guid, node.DisplayName, eventType, status, detail));
            Version++;
            while (_records.Count > _capacity)
                _records.Dequeue();
        }
    }
}
