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

            if (variant.executionPayload != null)
            {
                if (variant.executionPayload is not UPlayGroundMotionAbilityPayloadSO payload)
                    return false;
                animKey = payload.ResolveAnimKey();
                attackInfo = payload.playerAttackInfo;
                return payload.IsExecutable;
            }

            // V1 에셋 호환: 기존 Variant 내부 직렬화 데이터는 명시적 변환 전까지 유지한다.
            animKey = variant.ResolveLegacyAnimKey();
            attackInfo = variant.playerAttackInfo;
            return variant.HasLegacyExecutionData;
        }
    }
}
