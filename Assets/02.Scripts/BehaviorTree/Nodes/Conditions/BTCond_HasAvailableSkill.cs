using UnityEngine;

namespace UPlayGround.BehaviorTree
{
    [CreateAssetMenu(menuName = "BehaviorTree/Condition/HasAvailableSkill", fileName = "BTCond_HasAvailableSkill")]
    public class BTCond_HasAvailableSkillSO : BTNodeSO
    {
        protected override BTNode CreateRuntimeNode(EnemyBlackboard bb)
            => new BTLeaf(nodeName, b =>
            {
                if (b.Combat == null) return NodeStatus.Failure;
                return b.Combat.HasAvailableSkillAtDistance(b.DistanceToTarget)
                    ? NodeStatus.Success
                    : NodeStatus.Failure;
            });
    }
}
