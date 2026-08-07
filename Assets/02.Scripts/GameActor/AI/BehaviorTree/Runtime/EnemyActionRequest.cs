using UPlayGround.Data.EnumType;

namespace UPlayGround.AI.BehaviorTree
{
    public readonly struct EnemyActionRequest
    {
        public EnemyActionRequest(
            EnemyActionIntent intent,
            EnemyActionStyle style = EnemyActionStyle.None,
            AbilityAttackCategory attackCategory = AbilityAttackCategory.None,
            string cooldownId = null,
            float cooldownDuration = 0f,
            AbilityAIRole abilityRole = AbilityAIRole.None)
        {
            Intent = intent;
            Style = style;
            AttackCategory = attackCategory;
            AbilityRole = abilityRole;
            CooldownId = cooldownId;
            CooldownDuration = cooldownDuration;
        }

        public EnemyActionIntent Intent { get; }
        public EnemyActionStyle Style { get; }
        public AbilityAttackCategory AttackCategory { get; }
        public AbilityAIRole AbilityRole { get; }
        public string CooldownId { get; }
        public float CooldownDuration { get; }
    }
}
