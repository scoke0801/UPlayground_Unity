using UPlayGround.Data.Ability;
using UPlayGround.Data.EnumType;
using AbilityAttackInfo = global::UPlayGround.Data.AbilityAttackInfo;

namespace UPlayGround.Ability.UPlayGround
{
    public static class UPlayGroundAbilityPayloadResolver
    {
        public static bool TryResolveAnimKey(
            AbilityVariantDefinition variant,
            out AnimKey animKey)
        {
            animKey = AnimKey.None;
            if (variant?.executionPayload is not UPlayGroundMotionAbilityPayloadSO payload)
                return false;

            animKey = payload.ResolveAnimKey();
            return animKey != AnimKey.None;
        }

        public static bool TryResolve(
            AbilityVariantDefinition variant,
            out AnimKey animKey,
            out AbilityAttackInfo attackInfo)
        {
            animKey = AnimKey.None;
            attackInfo = null;
            if (variant == null) return false;

            if (variant.executionPayload is not UPlayGroundMotionAbilityPayloadSO payload)
                return false;

            animKey = payload.ResolveAnimKey();
            attackInfo = payload.attackInfo;
            return payload.IsAttackExecutable;
        }
    }
}
