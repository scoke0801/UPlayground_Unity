using UnityEngine;

namespace UPlayGround.BehaviorTree
{
    [CreateAssetMenu(menuName = "BehaviorTree/Condition/BBBool", fileName = "BTCond_BBBool")]
    public class BTCond_BBBoolSO : BTNodeSO
    {
        [Tooltip("확인할 블랙보드 bool 키 (BBKey 상수 사용)")]
        public string key = "HasGuardMotion";
        [Tooltip("true면 값이 false일 때 Success")]
        public bool invert = false;

        protected override BTNode CreateRuntimeNode(RuntimeBlackboard bb)
        {
            string k   = key;
            bool   inv = invert;
            return new BTLeaf(nodeName, b =>
            {
                bool v = b.GetBool(k);
                return (v != inv) ? NodeStatus.Success : NodeStatus.Failure;
            });
        }
    }
}
