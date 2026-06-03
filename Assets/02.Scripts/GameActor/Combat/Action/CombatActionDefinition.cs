using UPlayGround.Data;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Combat
{
    public readonly struct CombatActionDefinition
    {
        public readonly AnimKey AnimKey;
        public readonly AttackData LegacyAttackData;
        public readonly object SourceData;

        public CombatActionDefinition(AnimKey animKey, AttackData legacyAttackData, object sourceData)
        {
            AnimKey = animKey;
            LegacyAttackData = legacyAttackData;
            SourceData = sourceData;
        }
    }
}
