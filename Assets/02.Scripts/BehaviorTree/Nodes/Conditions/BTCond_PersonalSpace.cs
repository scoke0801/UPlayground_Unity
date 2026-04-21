using UnityEngine;

namespace UPlayGround.BehaviorTree
{
    [CreateAssetMenu(menuName = "BehaviorTree/Condition/PersonalSpace", fileName = "BTCond_PersonalSpace")]
    public class BTCond_PersonalSpaceSO : BTNodeSO
    {
        protected override BTNode CreateRuntimeNode(RuntimeBlackboard bb)
            => new BTLeaf(nodeName, b =>
                b.GetFloat(BBKey.DistanceToTarget) < b.GetFloat(BBKey.PersonalSpaceDistance)
                    ? NodeStatus.Success
                    : NodeStatus.Failure);
    }
}
