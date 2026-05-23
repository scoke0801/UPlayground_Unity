namespace UPlayGround.AI.BehaviorTree
{
    public static partial class EnemyBlackboardKeys
    {
        public static string CooldownReadyTime(string cooldownId)
        {
            return $"Cooldown.{cooldownId}.ReadyTime";
        }
    }
}