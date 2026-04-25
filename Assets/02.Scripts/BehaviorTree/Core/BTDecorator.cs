using UnityEngine;

namespace UPlayGround.BehaviorTree
{
    /// <summary>
    /// BTGuard가 BB 값 변화를 감지했을 때 어떻게 반응할지 결정한다.
    /// </summary>
    public enum AbortType
    {
        /// <summary>변화 감지 없음 — 매 Tick 조건 재평가만 수행 (기본)</summary>
        None,
        /// <summary>observeKey BB 값이 변하면 즉시 _abortRequested 플래그를 세워 다음 Tick에 Failure 반환</summary>
        Self,
    }

    public class BTInverter : BTNode
    {
        private readonly BTNode _child;

        public BTInverter(string name, BTNode child)
        {
            NodeName = name;
            _child   = child;
        }

        protected override NodeStatus TickInternal(RuntimeBlackboard bb)
        {
            var status = _child.Tick(bb);
            return status switch
            {
                NodeStatus.Success => NodeStatus.Failure,
                NodeStatus.Failure => NodeStatus.Success,
                _                  => NodeStatus.Running
            };
        }
    }

    public class BTCooldown : BTNode
    {
        private readonly BTNode _child;
        private readonly float  _cooldown;
        private float           _lastSuccessTime = -999f;

        public BTCooldown(string name, BTNode child, float cooldown)
        {
            NodeName  = name;
            _child    = child;
            _cooldown = cooldown;
        }

        protected override NodeStatus TickInternal(RuntimeBlackboard bb)
        {
            if (Time.time - _lastSuccessTime < _cooldown)
                return NodeStatus.Failure;

            var status = _child.Tick(bb);
            if (status == NodeStatus.Success)
                _lastSuccessTime = Time.time;

            return status;
        }
    }

    /// <summary>
    /// 자식이 Running 중일 때도 매 Tick마다 조건을 재평가한다.
    /// BTSequence와 달리, Running 상태에서 조건이 false가 되면 즉시 Failure를 반환해 자식을 중단시킨다.
    /// AbortType.Self + observeKey 설정 시 BB 값 변화를 구독하여 다음 Tick에 선제 중단한다.
    /// </summary>
    public class BTGuard : BTNode
    {
        private readonly BTNode    _condNode;
        private readonly BTNode    _child;
        private readonly AbortType _abortType;
        private readonly string    _observeKey;
        private bool               _abortRequested;
        private RuntimeBlackboard  _bb;

        public BTGuard(string name, BTNode condNode, BTNode child,
                       AbortType abortType = AbortType.None, string observeKey = "")
        {
            NodeName    = name;
            _condNode   = condNode;
            _child      = child;
            _abortType  = abortType;
            _observeKey = observeKey;
        }

        public override void OnEnter(RuntimeBlackboard bb)
        {
            _bb             = bb;
            _abortRequested = false;
            if (_abortType == AbortType.Self && !string.IsNullOrEmpty(_observeKey))
                bb.OnBoolChanged += OnBBChanged;
        }

        public override void OnExit(RuntimeBlackboard bb)
        {
            if (_abortType == AbortType.Self)
                bb.OnBoolChanged -= OnBBChanged;
            _bb = null;
        }

        private void OnBBChanged(string key)
        {
            if (key != _observeKey || _bb == null) return;
            if (_condNode.Tick(_bb) == NodeStatus.Failure)
                _abortRequested = true;
        }

        protected override NodeStatus TickInternal(RuntimeBlackboard bb)
        {
            if (_abortRequested)
            {
                _abortRequested = false;
                return NodeStatus.Failure;
            }

            if (_condNode.Tick(bb) == NodeStatus.Failure)
                return NodeStatus.Failure;

            return _child.Tick(bb);
        }
    }

    /// <summary>
    /// 자식 결과를 항상 Success로 덮어쓴다. Failure를 무시하고 시퀀스를 계속 진행할 때 사용.
    /// </summary>
    public class BTForceSuccess : BTNode
    {
        private readonly BTNode _child;
        public BTForceSuccess(string name, BTNode child) { NodeName = name; _child = child; }

        protected override NodeStatus TickInternal(RuntimeBlackboard bb)
        {
            _child.Tick(bb);
            return NodeStatus.Success;
        }
    }

    /// <summary>
    /// 자식을 N회(loopCount &lt; 0이면 무한) 반복 실행한다.
    /// 자식이 Success를 반환할 때마다 카운트를 증가시키고, 한도에 도달하면 Success 반환.
    /// 자식이 Failure를 반환하면 즉시 Failure 반환.
    /// </summary>
    public class BTLoop : BTNode
    {
        private readonly BTNode _child;
        private readonly int    _loopCount;
        private int             _done;

        public BTLoop(string name, BTNode child, int loopCount)
        {
            NodeName   = name;
            _child     = child;
            _loopCount = loopCount;
        }

        public override void OnEnter(RuntimeBlackboard bb) => _done = 0;

        protected override NodeStatus TickInternal(RuntimeBlackboard bb)
        {
            var s = _child.Tick(bb);
            if (s == NodeStatus.Failure) return NodeStatus.Failure;
            if (s == NodeStatus.Success)
            {
                _done++;
                if (_loopCount >= 0 && _done >= _loopCount)
                {
                    _done = 0;
                    return NodeStatus.Success;
                }
            }
            return NodeStatus.Running;
        }
    }
}
