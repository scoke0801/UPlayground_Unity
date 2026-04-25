using System.Collections.Generic;

namespace UPlayGround.BehaviorTree
{
    public class BTSequence : BTNode
    {
        private readonly List<BTNode>          _children;
        private readonly List<BTServiceRuntime> _services;
        private int _runningIndex;

        public BTSequence(string name, List<BTNode> children, List<BTServiceRuntime> services = null)
        {
            NodeName  = name;
            _children = children;
            _services = services;
        }

        public override void OnEnter(RuntimeBlackboard bb) => _runningIndex = 0;

        protected override NodeStatus TickInternal(RuntimeBlackboard bb)
        {
            if (_services != null)
                foreach (var svc in _services) svc.TryTick(bb);

            for (int i = _runningIndex; i < _children.Count; i++)
            {
                var status = _children[i].Tick(bb);
                if (status == NodeStatus.Running) { _runningIndex = i; return NodeStatus.Running; }
                if (status == NodeStatus.Failure) { _runningIndex = 0; return NodeStatus.Failure; }
            }
            _runningIndex = 0;
            return NodeStatus.Success;
        }
    }

    public class BTSelector : BTNode
    {
        private readonly List<BTNode>          _children;
        private readonly List<BTServiceRuntime> _services;
        private int _runningIndex;

        public BTSelector(string name, List<BTNode> children, List<BTServiceRuntime> services = null)
        {
            NodeName  = name;
            _children = children;
            _services = services;
        }

        public override void OnEnter(RuntimeBlackboard bb) => _runningIndex = 0;

        protected override NodeStatus TickInternal(RuntimeBlackboard bb)
        {
            if (_services != null)
                foreach (var svc in _services) svc.TryTick(bb);

            for (int i = _runningIndex; i < _children.Count; i++)
            {
                var status = _children[i].Tick(bb);
                if (status == NodeStatus.Running) { _runningIndex = i; return NodeStatus.Running; }
                if (status == NodeStatus.Success) { _runningIndex = 0; return NodeStatus.Success; }
            }
            _runningIndex = 0;
            return NodeStatus.Failure;
        }
    }

    public class BTRandomSelector : BTNode
    {
        private readonly List<BTNode> _children;
        private readonly List<float>  _weights;

        public BTRandomSelector(string name, List<BTNode> children, List<float> weights)
        {
            NodeName  = name;
            _children = children;
            _weights  = weights;
        }

        protected override NodeStatus TickInternal(RuntimeBlackboard bb)
        {
            float total = 0f;
            foreach (var w in _weights) total += w;

            float roll = UnityEngine.Random.value * total;
            float acc  = 0f;
            for (int i = 0; i < _children.Count; i++)
            {
                acc += _weights[i];
                if (roll <= acc)
                {
                    var status = _children[i].Tick(bb);
                    if (status != NodeStatus.Failure) return status;
                }
            }
            return NodeStatus.Failure;
        }
    }
}
