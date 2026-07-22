using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UPlayGround.Animation;

namespace UPlayGround.Combat
{
    public readonly struct CombatActionDefinition
    {
        public readonly MotionSetAsset MotionAsset;
        public readonly AttackData LegacyAttackData;
        public readonly object SourceData;

        public CombatActionDefinition(MotionSetAsset motionAsset, AttackData legacyAttackData, object sourceData)
        {
            MotionAsset = motionAsset;
            LegacyAttackData = legacyAttackData;
            SourceData = sourceData;
        }
    }
}
