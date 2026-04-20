using UnityEngine;

namespace UPlayGround.BehaviorTree
{
    [CreateAssetMenu(menuName = "BehaviorTree/Condition/PersonalSpace", fileName = "BTCond_PersonalSpace")]
    public class BTCond_PersonalSpaceSO : BTNodeSO
    {
        protected override BTNode CreateRuntimeNode(EnemyBlackboard bb)
            => new BTLeaf(nodeName, b =>
                b.DistanceToTarget < b.PersonalSpaceDistance
                    ? NodeStatus.Success
                    : NodeStatus.Failure);
    }
}
