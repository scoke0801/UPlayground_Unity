namespace UPlayGround.AI.BehaviorTree
{
    public static partial class EnemyBlackboardKeys
    {
        public const string PredictedNextPlayerAction = "Prediction.Player.NextAction";
        public const string PredictionConfidence = "Prediction.Confidence";
        public const string PlayerActionLastToken = "Prediction.Player.LastToken";
        public const string PlayerActionTimeSinceLast = "Prediction.Player.TimeSinceLast";
        public const string ResolverFailureReason = "Decision.ResolverFailureReason";

        public static string CooldownReadyTime(string cooldownId)
        {
            return $"Cooldown.{cooldownId}.ReadyTime";
        }
    }
}
