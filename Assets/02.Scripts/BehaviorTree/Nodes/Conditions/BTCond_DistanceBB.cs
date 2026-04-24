using UnityEngine;

namespace UPlayGround.BehaviorTree
{
    /// <summary>
    /// 블랙보드에서 거리 임계값을 읽어 비교한다.
    /// 에디터에서 하드코딩 없이 적마다 다른 OptimalCombatDistance 등을 참조할 수 있다.
    /// </summary>
    [CreateAssetMenu(menuName = "BehaviorTree/Condition/DistanceBB", fileName = "BTCond_DistanceBB")]
    public class BTCond_DistanceBBSO : BTNodeSO
    {
        public DistanceCheck check = DistanceCheck.LessThan;

        [Tooltip("상한 임계값 BB 키 (LessThan/Between)")]
        public string thresholdKey = "MaxAttackRange";

        [Tooltip("임계값에 곱할 배수 (1.0 = 그대로, 1.3 = 130%)")]
        [Min(0.01f)]
        public float multiplier = 1f;

        [Tooltip("Between 체크 시 하한 BB 키")]
        public string minKey = "MinCombatDistance";

        protected override BTNode CreateRuntimeNode(RuntimeBlackboard bb)
        {
            DistanceCheck c   = check;
            string        tk  = thresholdKey;
            float         mul = multiplier;
            string        mk  = minKey;

            return new BTLeaf(nodeName, b =>
            {
                float d         = b.GetFloat(BBKey.DistanceToTarget);
                float threshold = b.GetFloat(tk) * mul;

                bool pass = c switch
                {
                    DistanceCheck.LessThan    => d <= threshold,
                    DistanceCheck.GreaterThan => d > threshold,
                    DistanceCheck.Between     => d >= b.GetFloat(mk) && d <= threshold,
                    _                         => false,
                };
                return pass ? NodeStatus.Success : NodeStatus.Failure;
            });
        }
    }
}
