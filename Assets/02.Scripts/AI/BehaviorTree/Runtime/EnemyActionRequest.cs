using UPlayGround.Data.EnumType;

namespace UPlayGround.AI.BehaviorTree
{
    public readonly struct EnemyActionRequest
    {
        public EnemyActionRequest(
            EnemyActionIntent intent,
            EnemyActionStyle style = EnemyActionStyle.None,
            EnemyAttackCategory attackCategory = EnemyAttackCategory.None,
            string cooldownId = null,
            float cooldownDuration = 0f)
        {
            Intent = intent;
            Style = style;
            AttackCategory = attackCategory;
            CooldownId = cooldownId;
            CooldownDuration = cooldownDuration;
        }

        public EnemyActionIntent Intent { get; }
        public EnemyActionStyle Style { get; }
        public EnemyAttackCategory AttackCategory { get; }
        public string CooldownId { get; }
        public float CooldownDuration { get; }
    }
}
