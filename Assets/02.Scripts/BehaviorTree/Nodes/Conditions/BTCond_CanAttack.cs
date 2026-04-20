using UnityEngine;

namespace UPlayGround.BehaviorTree
{
    [CreateAssetMenu(menuName = "BehaviorTree/Condition/CanAttack", fileName = "BTCond_CanAttack")]
    public class BTCond_CanAttackSO : BTNodeSO
    {
        protected override BTNode CreateRuntimeNode(EnemyBlackboard bb)
            => new BTLeaf(nodeName, b =>
            {
                if (b.Combat == null || b.Runner == null) return NodeStatus.Failure;
                if (!b.Runner.CanUseSkillPublic())        return NodeStatus.Failure;
                if (!b.Combat.HasAvailableSkillAtDistance(b.DistanceToTarget)) return NodeStatus.Failure;
                return NodeStatus.Success;
            });
    }
}
