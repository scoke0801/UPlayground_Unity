using UnityEngine;

namespace UPlayGround.BehaviorTree
{
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
}
