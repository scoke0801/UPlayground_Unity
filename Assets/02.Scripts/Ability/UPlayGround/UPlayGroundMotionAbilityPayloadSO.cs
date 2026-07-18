using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Data.EnumType;
using AbilityAttackInfo = global::UPlayGround.Data.AbilityAttackInfo;

namespace UPlayGround.Ability.UPlayGround
{
    [CreateAssetMenu(
        fileName = "AbilityPayload_",
        menuName = "UPlayGround/Ability/Execution Payload/Motion Attack")]
    public sealed class UPlayGroundMotionAbilityPayloadSO : AbilityExecutionPayloadSO
    {
        public AnimKey animKey = AnimKey.None;
        public AbilityAttackInfo attackInfo = new();

        public AnimKey ResolveAnimKey() =>
            animKey != AnimKey.None
                ? animKey
                : attackInfo?.baseInfo?.animKey ?? AnimKey.None;

        public bool IsExecutable => ResolveAnimKey() != AnimKey.None;

        public bool IsAttackExecutable =>
            attackInfo?.baseInfo != null && IsExecutable;
    }
}
