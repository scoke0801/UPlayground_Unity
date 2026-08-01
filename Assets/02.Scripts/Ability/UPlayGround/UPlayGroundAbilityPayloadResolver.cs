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
            // Motion Key는 공격 수치와 분리돼 있으므로 baseInfo 없이도 해석된다.
            if (attackInfo == null)
                return false;

            motionKey = attackInfo.motionKey;
            return motionKey.IsValid;
        }
    }
}
