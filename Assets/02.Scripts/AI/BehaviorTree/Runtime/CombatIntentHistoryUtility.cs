namespace UPlayGround.AI.BehaviorTree
{
    public static class CombatIntentHistoryUtility
    {
        public static void RecordSelectedIntentExecution(Blackboard blackboard)
        {
            if (blackboard == null
                || !blackboard.TryGetString(EnemyBlackboardKeys.SelectedIntent, out var selectedIntent)
                || string.IsNullOrWhiteSpace(selectedIntent))
                return;

            var nextCount = 1;
            if (blackboard.TryGetString(EnemyBlackboardKeys.LastIntent, out var lastIntent)
                && lastIntent == selectedIntent
                && blackboard.TryGetInt(EnemyBlackboardKeys.ConsecutiveIntentCount, out var count))
            {
                nextCount = System.Math.Min(count + 1, 99);
            }

            blackboard.SetString(EnemyBlackboardKeys.LastIntent, selectedIntent);
            blackboard.SetInt(EnemyBlackboardKeys.ConsecutiveIntentCount, nextCount);
        }
    }
}
