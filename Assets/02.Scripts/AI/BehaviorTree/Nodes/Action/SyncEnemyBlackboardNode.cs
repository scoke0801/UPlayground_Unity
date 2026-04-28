using UPlayGround.Component;
using UPlayGround.MovementController;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    public class SyncEnemyBlackboardNode : BTActionNode
    {
        [SerializeField] private string _hasTargetKey = "HasTarget";
        [SerializeField] private string _targetKey = "Target";
        [SerializeField] private string _distanceKey = "DistanceToTarget";
        [SerializeField] private string _stateKey = "CurrentState";

        protected override BTStatus OnUpdate()
        {
            if (Context?.Blackboard == null)
                return BTStatus.Failure;

            var detection = Context.GetComponentCached<EnemyDetection>();
            var controller = Context.GetComponentCached<ActorMovementController>();
            var hasTarget = detection != null && detection.HasTarget;

            Context.Blackboard.SetBool(_hasTargetKey, hasTarget);
            Context.Blackboard.SetObject(_targetKey, hasTarget ? detection.CurrentTarget : null);
            Context.Blackboard.SetFloat(_distanceKey, hasTarget ? detection.DistanceToTarget : float.MaxValue);
            Context.Blackboard.SetString(_stateKey, controller?.CurrentState?.StateName ?? "");
            return BTStatus.Success;
        }
    }
}
