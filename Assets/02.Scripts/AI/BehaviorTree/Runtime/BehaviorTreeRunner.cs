using System;
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    public class BehaviorTreeRunner : MonoBehaviour
    {
        [Header("Tree")]
        [SerializeField] private BehaviorTreeAsset _treeAsset;
        [SerializeField] private bool _startOnEnable = true;

        [Header("Tick")]
        [SerializeField] private BehaviorTreeRunnerMode _tickMode = BehaviorTreeRunnerMode.UpdateInterval;
        [SerializeField] private float _tickInterval = 0.1f;
        [SerializeField] private bool _restartWhenComplete;
        [SerializeField] private bool _resetValuesOnRestart = true;
        [SerializeField] private bool _debugMode = true;
        [SerializeField] private int _debugTraceCapacity = 128;

        private BehaviorTreeAsset _runtimeTree;
        private BehaviorTreeContext _context;
        private float _tickTimer;
        private BehaviorTreeRunnerState _state = BehaviorTreeRunnerState.Stopped;
        private bool _pauseRequested;
        private BTNode _pauseRequestedBy;

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

        private void OnEnable()
        {
            if (_startOnEnable)
                StartTree();
        }

        private void OnDisable()
        {
            StopTree();
        }

        private void Update()
        {
            if (_state != BehaviorTreeRunnerState.Running || _runtimeTree?.RootNode == null || _tickMode == BehaviorTreeRunnerMode.Manual)
                return;

            if (_tickMode == BehaviorTreeRunnerMode.UpdateInterval)
            {
                _tickTimer += Time.deltaTime;
                if (_tickTimer < Mathf.Max(0.01f, _tickInterval))
                    return;

                _tickTimer = 0f;
            }

            TickOnce();
        }

        public void StartTree()
        {
            StopTree();

            if (_treeAsset == null || _treeAsset.RootNode == null)
                return;

            _runtimeTree = _treeAsset.CloneRuntime();
            DebugTrace = new BehaviorTreeDebugTrace(_debugTraceCapacity);
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
            var blackboard = resetBlackboardValues ? null : _runtimeTree?.Blackboard;

            if (_runtimeTree?.RootNode != null)
                _runtimeTree.RootNode.Abort();

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
            DebugTrace = new BehaviorTreeDebugTrace(_debugTraceCapacity);
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
