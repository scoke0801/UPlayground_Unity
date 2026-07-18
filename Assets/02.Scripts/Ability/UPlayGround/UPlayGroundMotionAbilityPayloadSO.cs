using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Data.EnumType;
using PlayerAttackInfo = global::UPlayGround.Data.PlayerAttackInfo;

namespace UPlayGround.Ability.UPlayGround
{
    [CreateAssetMenu(
        fileName = "AbilityPayload_",
        menuName = "UPlayGround/Ability/Execution Payload/Motion Attack")]
    public sealed class UPlayGroundMotionAbilityPayloadSO : AbilityExecutionPayloadSO
    {
        public AnimKey animKey = AnimKey.None;
        public PlayerAttackInfo playerAttackInfo = new();

        public AnimKey ResolveAnimKey() =>
            animKey != AnimKey.None
                ? animKey
                : playerAttackInfo?.baseInfo?.animKey ?? AnimKey.None;

        public bool IsExecutable =>
            playerAttackInfo?.baseInfo != null && ResolveAnimKey() != AnimKey.None;
    }
}
