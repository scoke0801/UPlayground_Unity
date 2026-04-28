using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    public class BehaviorTreeRunner : MonoBehaviour
    {
        [Header("Tree")]
        [SerializeField] private BehaviorTreeAsset _treeAsset;
        [SerializeField] private bool _startOnEnable = true;

        [Header("Tick")]
        [SerializeField] private float _tickInterval = 0.1f;
        [SerializeField] private bool _debugMode = true;

        private BehaviorTreeAsset _runtimeTree;
        private BehaviorTreeContext _context;
        private float _tickTimer;
        private bool _isRunning;

        public BehaviorTreeAsset SourceTree => _treeAsset;
        public BehaviorTreeAsset RuntimeTree => _runtimeTree;
        public BehaviorTreeContext Context => _context;
        public BTStatus ExecutionStatus { get; private set; } = BTStatus.Failure;
        public bool IsRunning => _isRunning;
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
            if (!_isRunning || _runtimeTree?.RootNode == null)
                return;

            _tickTimer += Time.deltaTime;
            if (_tickTimer < Mathf.Max(0.01f, _tickInterval))
                return;

            _tickTimer = 0f;
            ExecutionStatus = _runtimeTree.RootNode.Tick();
        }

        public void StartTree()
        {
            StopTree();

            if (_treeAsset == null || _treeAsset.RootNode == null)
                return;

            _runtimeTree = _treeAsset.CloneRuntime();
            _context = new BehaviorTreeContext(gameObject, _runtimeTree.Blackboard);
            _runtimeTree.RootNode.Initialize(_context);
            _isRunning = true;
            _tickTimer = _tickInterval;
        }

        public void StopTree()
        {
            if (_runtimeTree?.RootNode != null)
                _runtimeTree.RootNode.Abort();

            _runtimeTree = null;
            _context = null;
            _isRunning = false;
            _tickTimer = 0f;
            ExecutionStatus = BTStatus.Failure;
        }

        public void RestartTree()
        {
            StartTree();
        }
    }
}
