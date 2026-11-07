namespace UPlayGround.AI.BehaviorTree
{
    public static class CombatIntentHistoryUtility
    {
        public static void RecordSelectedIntentExecution(Blackboard blackboard)
        {
            if (blackboard == null
                || !blackboard.TryGetString(EnemyBlackboardKeys.DecisionSelectedIntent, out var selectedIntent)
                || string.IsNullOrWhiteSpace(selectedIntent))
                return;

            var nextCount = 1;
            if (blackboard.TryGetString(EnemyBlackboardKeys.DecisionLastIntent, out var lastIntent)
                && lastIntent == selectedIntent
                && blackboard.TryGetInt(EnemyBlackboardKeys.DecisionConsecutiveIntentCount, out var count))
            {
                nextCount = System.Math.Min(count + 1, 99);
            }

            blackboard.SetString(EnemyBlackboardKeys.DecisionLastIntent, selectedIntent);
            blackboard.SetInt(EnemyBlackboardKeys.DecisionConsecutiveIntentCount, nextCount);
        }
    }
}
