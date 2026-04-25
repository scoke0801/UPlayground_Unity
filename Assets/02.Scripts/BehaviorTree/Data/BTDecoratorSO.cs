using UnityEngine;

namespace UPlayGround.BehaviorTree
{
    // AbortType enum은 BTDecorator.cs(Core)에 정의됨

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

    [CreateAssetMenu(menuName = "BehaviorTree/Decorator/Guard", fileName = "BTGuard")]
    public class BTGuardSO : BTNodeSO
    {
        [Tooltip("매 Tick 재평가할 조건 노드 (Condition leaf만 연결)")]
        public BTNodeSO  condition;
        [Tooltip("조건 통과 시 실행할 자식")]
        public BTNodeSO  child;
        [Tooltip("AbortType.Self: observeKey BB 값이 변하면 즉시 중단 플래그를 세운다")]
        public AbortType abortType  = AbortType.None;
        [Tooltip("감시할 BB 키 이름 (BBKey 상수 사용). AbortType.None이면 무시됨")]
        public string    observeKey = "";

        protected override BTNode CreateRuntimeNode(RuntimeBlackboard bb)
        {
            var condNode  = condition?.CreateAndBindNode(bb)
                            ?? new BTLeaf("AlwaysTrue", _ => NodeStatus.Success);
            var childNode = child?.CreateAndBindNode(bb)
                            ?? new BTLeaf("Empty",      _ => NodeStatus.Failure);
            return new BTGuard(nodeName, condNode, childNode, abortType, observeKey);
        }
    }

    [CreateAssetMenu(menuName = "BehaviorTree/Decorator/ForceSuccess", fileName = "BTForceSuccess")]
    public class BTForceSuccessSO : BTNodeSO
    {
        public BTNodeSO child;

        protected override BTNode CreateRuntimeNode(RuntimeBlackboard bb)
        {
            var c = child != null ? child.CreateAndBindNode(bb)
                                  : new BTLeaf("Empty", _ => NodeStatus.Failure);
            return new BTForceSuccess(nodeName, c);
        }
    }

    [CreateAssetMenu(menuName = "BehaviorTree/Decorator/Loop", fileName = "BTLoop")]
    public class BTLoopSO : BTNodeSO
    {
        [Tooltip("-1 = 무한 반복")]
        [Min(-1)] public int loopCount = 3;
        public BTNodeSO child;

        protected override BTNode CreateRuntimeNode(RuntimeBlackboard bb)
        {
            var c = child != null ? child.CreateAndBindNode(bb)
                                  : new BTLeaf("Empty", _ => NodeStatus.Failure);
            return new BTLoop(nodeName, c, loopCount);
        }
    }
}
