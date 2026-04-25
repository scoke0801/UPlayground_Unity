using UnityEngine;

namespace UPlayGround.BehaviorTree
{
    [CreateAssetMenu(menuName = "BehaviorTree/Action/Wait", fileName = "BTAction_Wait")]
    public class BTAction_WaitSO : BTNodeSO
    {
        [Min(0f)] public float duration = 1f;
        [Tooltip("±이 값으로 대기 시간을 랜덤화. 0이면 고정.")]
        [Min(0f)] public float randomDeviation = 0f;

        protected override BTNode CreateRuntimeNode(RuntimeBlackboard bb)
        {
            float dur = duration;
            float dev = randomDeviation;
            float endTime = -1f;

            return new BTLeaf(nodeName, b =>
            {
                if (endTime < 0f)
                    endTime = Time.time + dur + Random.Range(-dev, dev);

                if (Time.time < endTime)
                    return NodeStatus.Running;

                endTime = -1f;
                return NodeStatus.Success;
            });
        }
    }
}
