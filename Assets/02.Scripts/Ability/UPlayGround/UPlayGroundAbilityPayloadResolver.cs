using UPlayGround.Data.Ability;
using UPlayGround.Animation;
using UPlayGround.Data.EnumType;
using AbilityAttackInfo = global::UPlayGround.Data.AbilityAttackInfo;

namespace UPlayGround.Ability.UPlayGround
{
    public static class UPlayGroundAbilityPayloadResolver
    {
        public static bool IsExecutable(AbilityVariantDefinition variant) =>
            variant?.executionPayload is UPlayGroundMotionAbilityPayloadSO payload
            && payload.IsExecutable;

        public static bool TryResolveAttackInfo(
            AbilityVariantDefinition variant,
            out AbilityAttackInfo attackInfo)
        {
            attackInfo = (variant?.executionPayload as UPlayGroundMotionAbilityPayloadSO)?.attackInfo;
            return attackInfo?.baseInfo != null;
        }

        public static bool TryResolve(
            AbilityVariantDefinition variant,
            WeaponType weaponType,
            out MotionSetAsset motionAsset,
            out AbilityAttackInfo attackInfo)
        {
            motionAsset = null;
            attackInfo = null;
            if (variant?.executionPayload is not UPlayGroundMotionAbilityPayloadSO payload)
                return false;

            motionAsset = payload.ResolveMotion(weaponType);
            attackInfo = payload.attackInfo;
            return attackInfo?.baseInfo != null && motionAsset != null;
        }

    }
}
