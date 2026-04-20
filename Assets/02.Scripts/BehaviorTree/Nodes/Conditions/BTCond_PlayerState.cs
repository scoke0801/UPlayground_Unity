using UnityEngine;

namespace UPlayGround.BehaviorTree
{
    public enum PlayerStateQuery { IsAttacking, IsGuarding, IsStaggered, IsRecovering, IsDodgingFrequently }

    [CreateAssetMenu(menuName = "BehaviorTree/Condition/PlayerState", fileName = "BTCond_PlayerState")]
    public class BTCond_PlayerStateSO : BTNodeSO
    {
        public PlayerStateQuery query = PlayerStateQuery.IsAttacking;

        protected override BTNode CreateRuntimeNode(EnemyBlackboard bb)
        {
            var q = query;
            return new BTLeaf(nodeName, b =>
            {
                if (b.Memory == null) return NodeStatus.Failure;
                bool result = q switch
                {
                    PlayerStateQuery.IsAttacking         => b.Memory.IsPlayerAttacking(),
                    PlayerStateQuery.IsGuarding          => b.Memory.IsPlayerGuarding(),
                    PlayerStateQuery.IsStaggered         => b.Memory.IsPlayerStaggered(),
                    PlayerStateQuery.IsRecovering        => b.Memory.IsPlayerRecovering(),
                    PlayerStateQuery.IsDodgingFrequently => b.Memory.IsPlayerDodgingFrequently(),
                    _                                    => false
                };
                return result ? NodeStatus.Success : NodeStatus.Failure;
            });
        }
    }
}
