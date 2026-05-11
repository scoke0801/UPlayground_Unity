using UPlayGround.Component;
using UPlayGround.MovementController;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    /// <summary>
    /// SyncEnemyBlackboardNode의 Service 버전.
    /// 부착된 Composite가 실행 중인 동안 일정 주기로 Detection 상태를 Blackboard에 동기화한다.
    /// 매 Tick Action 슬롯을 차지하지 않으므로 조건 분기 가독성이 좋아진다.
    /// </summary>
    public class SyncEnemyBlackboardService : BTServiceNode
    {
        [SerializeField] private string _hasTargetKey = "HasTarget";
        [SerializeField] private string _targetKey = "Target";
        [SerializeField] private string _distanceKey = "DistanceToTarget";
        [SerializeField] private string _stateKey = "CurrentState";

        protected override void OnServiceTick()
        {
            if (Context?.Blackboard == null)
                return;

            var detection = Context.GetComponentCached<EnemyDetection>();
            var controller = Context.GetComponentCached<ActorMovementController>();
            var hasTarget = detection != null && detection.HasTarget;

            Context.Blackboard.SetBool(_hasTargetKey, hasTarget);
            Context.Blackboard.SetObject(_targetKey, hasTarget ? detection.CurrentTarget : null);
            Context.Blackboard.SetFloat(_distanceKey, hasTarget ? detection.DistanceToTarget : float.MaxValue);
            Context.Blackboard.SetString(_stateKey, controller?.CurrentState?.StateName ?? "");
            Context.DebugTrace?.Record(
                this,
                "BlackboardWrite",
                BTStatus.Success,
                $"{_hasTargetKey}={hasTarget}, {_targetKey}={(hasTarget && detection.CurrentTarget != null ? detection.CurrentTarget.name : "null")}, {_distanceKey}={(hasTarget ? detection.DistanceToTarget : float.MaxValue):0.00}, {_stateKey}={controller?.CurrentState?.StateName ?? ""}");
        }
    }
}
