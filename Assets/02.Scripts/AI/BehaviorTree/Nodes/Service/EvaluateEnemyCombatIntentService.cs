using UPlayGround.Component;

namespace UPlayGround.AI.BehaviorTree
{
    /// <summary>
    /// 전투 Intent를 매 BT 틱마다 Blackboard에 동기화한다.
    /// Selector 자식 흐름을 점유하지 않도록 Action이 아니라 Service로 운용한다.
    /// </summary>
    public class EvaluateEnemyCombatIntentService : BTServiceNode
    {
        protected override void OnServiceTick()
        {
            if (Context?.Blackboard == null)
                return;

            var evaluator = Context.Owner != null
                ? Context.Owner.GetComponent<EnemyCombatDecisionEvaluator>()
                : null;
            if (evaluator == null && Context.Owner != null)
                evaluator = Context.Owner.AddComponent<EnemyCombatDecisionEvaluator>();

            if (evaluator == null)
                return;

            if (!evaluator.TryEvaluate(Context.Blackboard, out var evaluation))
                return;

            Context.Blackboard.SetString(EnemyBlackboardKeys.SelectedIntent, evaluation.SelectedIntent.ToString());
            Context.Blackboard.SetFloat(EnemyBlackboardKeys.IntentScoreAttack, evaluation.AttackScore);
            Context.Blackboard.SetFloat(EnemyBlackboardKeys.IntentScorePunish, evaluation.PunishScore);
            Context.Blackboard.SetFloat(EnemyBlackboardKeys.IntentScoreCounter, evaluation.CounterScore);
            Context.Blackboard.SetFloat(EnemyBlackboardKeys.IntentScorePressure, evaluation.PressureScore);
            Context.Blackboard.SetFloat(EnemyBlackboardKeys.IntentScoreChase, evaluation.ChaseScore);
            Context.Blackboard.SetFloat(EnemyBlackboardKeys.IntentScoreRetreat, evaluation.RetreatScore);
            Context.Blackboard.SetFloat(EnemyBlackboardKeys.IntentScoreKeepDistance, evaluation.KeepDistanceScore);
            Context.Blackboard.SetFloat(EnemyBlackboardKeys.IntentScoreDefend, evaluation.DefendScore);
            Context.Blackboard.SetFloat(EnemyBlackboardKeys.IntentScoreRecover, evaluation.RecoverScore);
            Context.Blackboard.SetString(EnemyBlackboardKeys.CombatRhythmPhase, evaluation.RhythmPhase);

            Context.DebugTrace?.Record(
                this,
                "IntentEvaluate",
                BTStatus.Success,
                evaluation.Reason);
        }
    }
}
