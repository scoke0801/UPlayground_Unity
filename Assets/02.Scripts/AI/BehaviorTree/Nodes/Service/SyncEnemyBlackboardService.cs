using UPlayGround.Components;
using UPlayGround.AI.CombatDecision;
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
        private readonly string _predictedNextPlayerAction;
        private readonly float _predictionConfidence;
        private readonly string _playerActionLastToken;
        private readonly float _playerActionTimeSinceLast;

        private TargetStateBlackboardSnapshot(
            bool hasTarget,
            UnityEngine.Object target,
            float distance,
            string stateName,
            ActorStateTag stateTags,
            PlayerBehaviorPredictor predictor)
        {
            _hasTarget = hasTarget;
            _target = target;
            _distance = distance;
            _stateName = stateName;
            _stateTags = stateTags;

            _predictionConfidence = 0f;
            var predicted = predictor != null
                ? predictor.PredictNext(out _predictionConfidence)
                : PlayerActionToken.None;
            _predictedNextPlayerAction = predicted.ToString();
            _playerActionLastToken = predictor != null ? predictor.LastToken.ToString() : PlayerActionToken.None.ToString();
            _playerActionTimeSinceLast = predictor != null && !float.IsPositiveInfinity(predictor.TimeSinceLastAction)
                ? predictor.TimeSinceLastAction
                : float.MaxValue;
        }

        public static TargetStateBlackboardSnapshot From(EnemyDetection detection, ActorMovementController controller)
        {
            var hasTarget = detection != null && detection.HasTarget;
            return new TargetStateBlackboardSnapshot(
                hasTarget,
                hasTarget ? detection.CurrentTarget : null,
                hasTarget ? detection.DistanceToTarget : float.MaxValue,
                controller?.CurrentState?.StateName ?? "",
                controller?.CurrentState?.StateTags ?? ActorStateTag.None,
                hasTarget ? detection.CurrentTarget.GetComponent<PlayerBehaviorPredictor>() : null);
        }

        public void WriteTo(Blackboard blackboard)
        {
            BlackboardWriteUtility.SetBool(blackboard, _hasTarget, EnemyBlackboardKeys.TargetHas);
            BlackboardWriteUtility.SetObject(blackboard, _target, EnemyBlackboardKeys.TargetObject);
            BlackboardWriteUtility.SetFloat(blackboard, _distance, EnemyBlackboardKeys.TargetDistance);
            BlackboardWriteUtility.SetString(blackboard, _stateName, EnemyBlackboardKeys.SelfStateId);
            BlackboardWriteUtility.SetInt(blackboard, (int)_stateTags, EnemyBlackboardKeys.SelfStateTags);
            BlackboardWriteUtility.SetString(blackboard, _predictedNextPlayerAction, EnemyBlackboardKeys.PredictedNextPlayerAction);
            BlackboardWriteUtility.SetFloat(blackboard, _predictionConfidence, EnemyBlackboardKeys.PredictionConfidence);
            BlackboardWriteUtility.SetString(blackboard, _playerActionLastToken, EnemyBlackboardKeys.PlayerActionLastToken);
            BlackboardWriteUtility.SetFloat(blackboard, _playerActionTimeSinceLast, EnemyBlackboardKeys.PlayerActionTimeSinceLast);
        }

        public string ToDebugString()
        {
            return $"{EnemyBlackboardKeys.TargetHas}={_hasTarget}, {EnemyBlackboardKeys.TargetObject}={(_target != null ? _target.name : "null")}, {EnemyBlackboardKeys.TargetDistance}={_distance:0.00}, {EnemyBlackboardKeys.SelfStateId}={_stateName}, {EnemyBlackboardKeys.SelfStateTags}={_stateTags}, Prediction={_predictedNextPlayerAction}/{_predictionConfidence:0.00}";
        }
    }
}
