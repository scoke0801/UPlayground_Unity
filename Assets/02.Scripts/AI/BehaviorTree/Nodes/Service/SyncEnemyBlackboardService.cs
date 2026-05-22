using UPlayGround.Component;
using UPlayGround.MovementController;
using UPlayGround.State;

namespace UPlayGround.AI.BehaviorTree
{
    /// <summary>
    /// SyncEnemyBlackboardNode의 Service 버전.
    /// 부착된 Composite가 실행 중인 동안 일정 주기로 Detection 상태를 Blackboard에 동기화한다.
    /// 매 Tick Action 슬롯을 차지하지 않으므로 조건 분기 가독성이 좋아진다.
    /// </summary>
    public class SyncEnemyBlackboardService : BTServiceNode
    {
        protected override void OnServiceTick()
        {
            if (Context?.Blackboard == null)
                return;

            var detection = Context.GetComponentCached<EnemyDetection>();
            var controller = Context.GetComponentCached<ActorMovementController>();
            var snapshot = TargetStateBlackboardSnapshot.From(detection, controller);
            snapshot.WriteTo(Context.Blackboard);

            Context.DebugTrace?.Record(
                this,
                "BlackboardWrite",
                BTStatus.Success,
                snapshot.ToDebugString());
        }
    }

    internal readonly struct TargetStateBlackboardSnapshot
    {
        private readonly bool _hasTarget;
        private readonly UnityEngine.Object _target;
        private readonly float _distance;
        private readonly string _stateName;
        private readonly ActorStateTag _stateTags;

        private TargetStateBlackboardSnapshot(
            bool hasTarget,
            UnityEngine.Object target,
            float distance,
            string stateName,
            ActorStateTag stateTags)
        {
            _hasTarget = hasTarget;
            _target = target;
            _distance = distance;
            _stateName = stateName;
            _stateTags = stateTags;
        }

        public static TargetStateBlackboardSnapshot From(EnemyDetection detection, ActorMovementController controller)
        {
            var hasTarget = detection != null && detection.HasTarget;
            return new TargetStateBlackboardSnapshot(
                hasTarget,
                hasTarget ? detection.CurrentTarget : null,
                hasTarget ? detection.DistanceToTarget : float.MaxValue,
                controller?.CurrentState?.StateName ?? "",
                controller?.CurrentState?.StateTags ?? ActorStateTag.None);
        }

        public void WriteTo(Blackboard blackboard)
        {
            BlackboardWriteUtility.SetBool(blackboard, _hasTarget, EnemyBlackboardKeys.TargetHas);
            BlackboardWriteUtility.SetObject(blackboard, _target, EnemyBlackboardKeys.TargetObject);
            BlackboardWriteUtility.SetFloat(blackboard, _distance, EnemyBlackboardKeys.TargetDistance);
            BlackboardWriteUtility.SetString(blackboard, _stateName, EnemyBlackboardKeys.SelfStateId);
            BlackboardWriteUtility.SetInt(blackboard, (int)_stateTags, EnemyBlackboardKeys.SelfStateTags);
        }

        public string ToDebugString()
        {
            return $"{EnemyBlackboardKeys.TargetHas}={_hasTarget}, {EnemyBlackboardKeys.TargetObject}={(_target != null ? _target.name : "null")}, {EnemyBlackboardKeys.TargetDistance}={_distance:0.00}, {EnemyBlackboardKeys.SelfStateId}={_stateName}, {EnemyBlackboardKeys.SelfStateTags}={_stateTags}";
        }
    }
}
