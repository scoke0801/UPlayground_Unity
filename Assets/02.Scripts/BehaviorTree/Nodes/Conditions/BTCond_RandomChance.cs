using UnityEngine;

namespace UPlayGround.BehaviorTree
{
    [CreateAssetMenu(menuName = "BehaviorTree/Condition/RandomChance", fileName = "BTCond_RandomChance")]
    public class BTCond_RandomChanceSO : BTNodeSO
    {
        [Range(0f, 1f)]
        [Tooltip("0=절대 실패, 1=항상 성공")]
        public float probability = 0.5f;

        protected override BTNode CreateRuntimeNode(RuntimeBlackboard bb)
        {
            float p = probability;
            return new BTLeaf(nodeName, _ =>
                Random.value <= p ? NodeStatus.Success : NodeStatus.Failure);
        }
    }
}
