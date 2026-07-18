using UPlayGround.Data.Ability;
using UPlayGround.Data.EnumType;
using PlayerAttackInfo = global::UPlayGround.Data.PlayerAttackInfo;

namespace UPlayGround.Ability.UPlayGround
{
    public static class UPlayGroundAbilityPayloadResolver
    {
        public static bool TryResolve(
            AbilityVariantDefinition variant,
            out AnimKey animKey,
            out PlayerAttackInfo attackInfo)
        {
            animKey = AnimKey.None;
            attackInfo = null;
            if (variant == null) return false;

            if (variant.executionPayload is not UPlayGroundMotionAbilityPayloadSO payload)
                return false;

            animKey = payload.ResolveAnimKey();
            attackInfo = payload.playerAttackInfo;
            return payload.IsExecutable;
        }
    }
}
