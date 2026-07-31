using UPlayGround.Data.Ability;
using UPlayGround.Data.Actor.Animation;
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
            out MotionKey motionKey,
            out AbilityAttackInfo attackInfo)
        {
            motionKey = default;
            attackInfo = null;
            if (variant?.executionPayload is not UPlayGroundMotionAbilityPayloadSO payload)
                return false;

            attackInfo = payload.attackInfo;
            if (attackInfo?.baseInfo == null)
                return false;

            motionKey = attackInfo.baseInfo.motionKey;
            return motionKey.IsValid;
        }
    }
}
