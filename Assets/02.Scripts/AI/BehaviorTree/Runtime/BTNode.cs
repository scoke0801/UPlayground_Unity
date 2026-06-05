using System;
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    public abstract class BTNode : ScriptableObject
    {
        [SerializeField] private string _guid;
        [SerializeField] private string _displayName;
        [SerializeField] [TextArea] private string _comment;
        [SerializeField] private Vector2 _editorPosition;
        [SerializeField] private List<BTNode> _children = new();
        [SerializeField] private bool _disabled;
        [SerializeField] private bool _breakpoint;

        [NonSerialized] private bool _started;
        [NonSerialized] private BehaviorTreeContext _context;
        // 틱마다 반복되던 런타임 캐스팅(this as ...)을 Initialize 시점 1회 캐싱으로 대체
        [NonSerialized] private BTCompositeNode _compositeSelf;
        [NonSerialized] private BTConditionNode _conditionSelf;

        public string Guid
        {
            get
            {
                EnsureGuid();
                return _guid;
            }
            set => _guid = value;
        }

        public string DisplayName
        {
            get => string.IsNullOrWhiteSpace(_displayName) ? GetType().Name : _displayName;
            set => _displayName = value;
        }

        public string Comment
        {
            get => _comment;
            set => _comment = value;
        }

        public Vector2 EditorPosition
        {
            get => _editorPosition;
            set => _editorPosition = value;
        }

        public List<BTNode> Children => _children;
        public BTStatus LastStatus { get; private set; } = BTStatus.Failure;
        public bool IsStarted => _started;
        public bool IsRunning => _started && LastStatus == BTStatus.Running;
        public bool Disabled
        {
            get => _disabled;
            set => _disabled = value;
        }

        public bool Breakpoint
        {
            get => _breakpoint;
            set => _breakpoint = value;
        }

        protected BehaviorTreeContext Context => _context;

        public void Initialize(BehaviorTreeContext context)
        {
            _context = context;
            _started = false;
            LastStatus = BTStatus.Failure;
            _compositeSelf = this as BTCompositeNode;
            _conditionSelf = this as BTConditionNode;
            OnInitialize();

            foreach (var child in _children)
                child?.Initialize(context);

            if (_compositeSelf != null)
            {
                foreach (var service in _compositeSelf.Services)
                    service?.Initialize(context);
            }
        }

        public BTStatus Tick()
        {
            if (_disabled)
            {
                LastStatus = BTStatus.Success;
                _context?.DebugTrace?.Record(this, "Disabled", LastStatus, "Disabled 노드는 Success로 건너뜁니다.");
                return LastStatus;
            }

            var compositeSelf = _compositeSelf;
            if (!_started)
            {
                _started = true;
                _context?.DebugTrace?.Record(this, "Start", BTStatus.Running);
                if (_breakpoint)
                    _context?.RequestPause(this);

                OnStart();
                compositeSelf?.BeginServices();
            }

            compositeSelf?.TickServices();

            LastStatus = OnUpdate();
            if (_conditionSelf != null)
            {
                _conditionSelf.SetAbortEvaluation(LastStatus);
            }
            _context?.DebugTrace?.Record(this, "Tick", LastStatus);

            if (LastStatus != BTStatus.Running)
            {
                OnStop();
                compositeSelf?.EndServices();
                _context?.DebugTrace?.Record(this, "Stop", LastStatus);
                _started = false;
            }

            return LastStatus;
        }

        public virtual void Abort()
        {
            if (_started)
            {
                OnAbort();
                OnStop();
                (this as BTCompositeNode)?.EndServices();
                _context?.DebugTrace?.Record(this, "Abort", LastStatus);
                _started = false;
            }

            foreach (var child in _children)
                child?.Abort();
        }

        public void ResetNode()
        {
            _started = false;
            LastStatus = BTStatus.Failure;
            OnReset();

            foreach (var child in _children)
                child?.ResetNode();

            if (this is BTCompositeNode composite)
            {
                foreach (var service in composite.Services)
                    service?.ResetNode();
            }
        }

        public void EnsureGuid()
        {
            if (string.IsNullOrWhiteSpace(_guid))
                _guid = System.Guid.NewGuid().ToString("N");
        }

        protected virtual void OnInitialize() { }
        protected virtual void OnStart() { }
        protected abstract BTStatus OnUpdate();
        protected virtual void OnStop() { }
        protected virtual void OnAbort() { }
        protected virtual void OnReset() { }
    }
}
