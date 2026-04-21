using UnityEngine;

namespace UPlayGround.BehaviorTree
{
    [CreateAssetMenu(menuName = "BehaviorTree/Decorator/Inverter", fileName = "BTInverter")]
    public class BTInverterSO : BTNodeSO
    {
        public BTNodeSO child;

        protected override BTNode CreateRuntimeNode(RuntimeBlackboard bb)
        {
            var runtimeChild = child != null
                ? child.CreateAndBindNode(bb)
                : new BTLeaf("Empty", _ => NodeStatus.Failure);
            return new BTInverter(nodeName, runtimeChild);
        }
    }

    [CreateAssetMenu(menuName = "BehaviorTree/Decorator/Cooldown", fileName = "BTCooldown")]
    public class BTCooldownSO : BTNodeSO
    {
        [Min(0f)] public float cooldown = 1f;
        public BTNodeSO child;

        protected override BTNode CreateRuntimeNode(RuntimeBlackboard bb)
        {
            var runtimeChild = child != null
                ? child.CreateAndBindNode(bb)
                : new BTLeaf("Empty", _ => NodeStatus.Failure);
            return new BTCooldown(nodeName, runtimeChild, cooldown);
        }
    }
}
