using System;

namespace UPlayGround.BehaviorTree
{
    /// <summary> 단일 람다로 Condition/Action 리프 노드를 인라인 생성할 때 사용 </summary>
    public class BTLeaf : BTNode
    {
        private readonly Func<EnemyBlackboard, NodeStatus> _execute;

        public BTLeaf(string name, Func<EnemyBlackboard, NodeStatus> execute)
        {
            NodeName = name;
            _execute = execute;
        }

        protected override NodeStatus TickInternal(EnemyBlackboard bb) => _execute(bb);
    }
}
