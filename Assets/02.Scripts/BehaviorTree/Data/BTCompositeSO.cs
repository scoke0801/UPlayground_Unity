using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.BehaviorTree
{
    [CreateAssetMenu(menuName = "BehaviorTree/Selector", fileName = "BTSelector")]
    public class BTSelectorSO : BTNodeSO
    {
        public List<BTNodeSO> children = new();

        protected override BTNode CreateRuntimeNode(RuntimeBlackboard bb)
        {
            var runtimeChildren = new List<BTNode>(children.Count);
            foreach (var child in children)
                if (child != null) runtimeChildren.Add(child.CreateAndBindNode(bb));

            return new BTSelector(nodeName, runtimeChildren);
        }
    }

    [CreateAssetMenu(menuName = "BehaviorTree/Sequence", fileName = "BTSequence")]
    public class BTSequenceSO : BTNodeSO
    {
        public List<BTNodeSO> children = new();

        protected override BTNode CreateRuntimeNode(RuntimeBlackboard bb)
        {
            var runtimeChildren = new List<BTNode>(children.Count);
            foreach (var child in children)
                if (child != null) runtimeChildren.Add(child.CreateAndBindNode(bb));

            return new BTSequence(nodeName, runtimeChildren);
        }
    }

    [CreateAssetMenu(menuName = "BehaviorTree/RandomSelector", fileName = "BTRandomSelector")]
    public class BTRandomSelectorSO : BTNodeSO
    {
        public List<BTNodeSO> children = new();
        [Tooltip("children과 같은 인덱스의 가중치. 비어있으면 균등 가중치.")]
        public List<float> weights = new();

        protected override BTNode CreateRuntimeNode(RuntimeBlackboard bb)
        {
            var runtimeChildren = new List<BTNode>(children.Count);
            foreach (var child in children)
                if (child != null) runtimeChildren.Add(child.CreateAndBindNode(bb));

            var w = (weights.Count == children.Count)
                ? new List<float>(weights)
                : BuildUniform(children.Count);

            return new BTRandomSelector(nodeName, runtimeChildren, w);
        }

        private static List<float> BuildUniform(int count)
        {
            var list = new List<float>(count);
            for (int i = 0; i < count; i++) list.Add(1f);
            return list;
        }
    }
}
