using UPlayGround.AI.CombatDecision;
using UPlayGround.AI.Debugging;
using UPlayGround.Components;
using UPlayGround.Combat;
using UnityEngine;

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

            CombatIntentBlackboardSnapshot.From(evaluation).WriteTo(Context.Blackboard);
            Context.GetComponentCached<IntentScoreTimeline>()?.Record(evaluation, Time.time, Context.Blackboard);
            Context.GetComponentCached<EncounterReplayRecorder>()?.RecordFrame(evaluation, Context.Blackboard);
            CombatTelemetrySession.NotifyIntentEvaluated(
                Context.GetComponentCached<MonsterActor>(),
                evaluation.SelectedIntent.ToString());

            Context.DebugTrace?.Record(
                this,
                "IntentEvaluate",
                BTStatus.Success,
                evaluation.Reason);
        }
    }

    internal readonly struct CombatIntentBlackboardSnapshot
    {
        private readonly string _selectedIntent;
        private readonly float _attackScore;
        private readonly float _punishScore;
        private readonly float _counterScore;
        private readonly float _pressureScore;
        private readonly float _chaseScore;
        private readonly float _retreatScore;
        private readonly float _keepDistanceScore;
        private readonly float _defendScore;
        private readonly float _recoverScore;
        private readonly string _rhythmPhase;

        private CombatIntentBlackboardSnapshot(CombatIntentEvaluation evaluation)
        {
            _selectedIntent = evaluation.SelectedIntent.ToString();
            _attackScore = evaluation.AttackScore;
            _punishScore = evaluation.PunishScore;
            _counterScore = evaluation.CounterScore;
            _pressureScore = evaluation.PressureScore;
            _chaseScore = evaluation.ChaseScore;
            _retreatScore = evaluation.RetreatScore;
            _keepDistanceScore = evaluation.KeepDistanceScore;
            _defendScore = evaluation.DefendScore;
            _recoverScore = evaluation.RecoverScore;
            _rhythmPhase = evaluation.RhythmPhase;
        }

        public static CombatIntentBlackboardSnapshot From(CombatIntentEvaluation evaluation)
        {
            return new CombatIntentBlackboardSnapshot(evaluation);
        }

        public void WriteTo(Blackboard blackboard)
        {
            BlackboardWriteUtility.SetString(blackboard, _selectedIntent, EnemyBlackboardKeys.DecisionSelectedIntent);
            BlackboardWriteUtility.SetFloat(blackboard, _attackScore, EnemyBlackboardKeys.DecisionIntentScoreAttack);
            BlackboardWriteUtility.SetFloat(blackboard, _punishScore, EnemyBlackboardKeys.DecisionIntentScorePunish);
            BlackboardWriteUtility.SetFloat(blackboard, _counterScore, EnemyBlackboardKeys.DecisionIntentScoreCounter);
            BlackboardWriteUtility.SetFloat(blackboard, _pressureScore, EnemyBlackboardKeys.DecisionIntentScorePressure);
            BlackboardWriteUtility.SetFloat(blackboard, _chaseScore, EnemyBlackboardKeys.DecisionIntentScoreChase);
            BlackboardWriteUtility.SetFloat(blackboard, _retreatScore, EnemyBlackboardKeys.DecisionIntentScoreRetreat);
            BlackboardWriteUtility.SetFloat(blackboard, _keepDistanceScore, EnemyBlackboardKeys.DecisionIntentScoreKeepDistance);
            BlackboardWriteUtility.SetFloat(blackboard, _defendScore, EnemyBlackboardKeys.DecisionIntentScoreDefend);
            BlackboardWriteUtility.SetFloat(blackboard, _recoverScore, EnemyBlackboardKeys.DecisionIntentScoreRecover);
            BlackboardWriteUtility.SetString(blackboard, _rhythmPhase, EnemyBlackboardKeys.DecisionCombatRhythmPhase);
        }
    }
}
