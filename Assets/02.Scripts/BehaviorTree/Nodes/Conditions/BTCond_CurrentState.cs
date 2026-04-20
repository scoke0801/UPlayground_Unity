using UnityEngine;

namespace UPlayGround.BehaviorTree
{
    [CreateAssetMenu(menuName = "BehaviorTree/Condition/CurrentState", fileName = "BTCond_CurrentState")]
    public class BTCond_CurrentStateSO : BTNodeSO
    {
        [Tooltip("이 State 이름일 때 Success")]
        public string stateName = "Idle";
        [Tooltip("true면 stateName과 다를 때 Success (Not 조건)")]
        public bool invert = false;

        protected override BTNode CreateRuntimeNode(EnemyBlackboard bb)
        {
            string sn  = stateName;
            bool   inv = invert;
            return new BTLeaf(nodeName, b =>
            {
                bool match = b.CurrentStateName == sn;
                return (match != inv) ? NodeStatus.Success : NodeStatus.Failure;
            });
        }
    }
}
