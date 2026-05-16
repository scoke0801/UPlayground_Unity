using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    public class IsEnemyPhaseNode : BTConditionNode
    {
        [SerializeField] private string _phaseName;
        [SerializeField] private int _phaseIndex = -1;

        protected override BTStatus OnUpdate()
        {
            if (Context?.Blackboard == null)
                return BTStatus.Failure;

            if (!string.IsNullOrWhiteSpace(_phaseName))
            {
                return Context.Blackboard.TryGetString(EnemyBlackboardKeys.CurrentPhaseName, out var currentName)
                       && currentName == _phaseName
                    ? BTStatus.Success
                    : BTStatus.Failure;
            }

            return _phaseIndex >= 0
                   && Context.Blackboard.TryGetInt(EnemyBlackboardKeys.PhaseIndex, out var currentIndex)
                   && currentIndex == _phaseIndex
                ? BTStatus.Success
                : BTStatus.Failure;
        }
    }
}
