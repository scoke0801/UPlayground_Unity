using UPlayGround.Components;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    /// <summary>
    /// Random.value &lt; Context.DiveChance 1회 굴림. 하강 분기에서 Dive 시도 게이트로 사용된다.
    /// </summary>
    public class RollDiveChanceNode : BTConditionNode
    {
        protected override BTStatus OnUpdate()
        {
            var context = Context?.GetComponentCached<EnemyFlyingAIContext>();
            if (context == null)
                return BTStatus.Failure;

            return Random.value < context.DiveChance ? BTStatus.Success : BTStatus.Failure;
        }
    }
}
