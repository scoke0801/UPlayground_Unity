using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.BehaviorTree
{
    [CreateAssetMenu(menuName = "BehaviorTree/Selector", fileName = "BTSelector")]
    public class BTSelectorSO : BTNodeSO
    {
        public List<BTNodeSO>    children = new();
        [Tooltip("이 컴포짓이 Tick될 때마다 지정 간격으로 실행되는 서비스 목록")]
        public List<BTServiceSO> services = new();

        protected override BTNode CreateRuntimeNode(RuntimeBlackboard bb)
        {
            var runtimeChildren = new List<BTNode>(children.Count);
            foreach (var child in children)
                if (child != null) runtimeChildren.Add(child.CreateAndBindNode(bb));

            var runtimeServices = new List<BTServiceRuntime>(services.Count);
            foreach (var svc in services)
                if (svc != null) runtimeServices.Add(svc.CreateRuntime());

            return new BTSelector(nodeName, runtimeChildren, runtimeServices);
        }
    }

    [CreateAssetMenu(menuName = "BehaviorTree/Sequence", fileName = "BTSequence")]
    public class BTSequenceSO : BTNodeSO
    {
        public List<BTNodeSO>    children = new();
        [Tooltip("이 컴포짓이 Tick될 때마다 지정 간격으로 실행되는 서비스 목록")]
        public List<BTServiceSO> services = new();

        protected override BTNode CreateRuntimeNode(RuntimeBlackboard bb)
        {
            var runtimeChildren = new List<BTNode>(children.Count);
            foreach (var child in children)
                if (child != null) runtimeChildren.Add(child.CreateAndBindNode(bb));

            var runtimeServices = new List<BTServiceRuntime>(services.Count);
            foreach (var svc in services)
                if (svc != null) runtimeServices.Add(svc.CreateRuntime());

            return new BTSequence(nodeName, runtimeChildren, runtimeServices);
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
