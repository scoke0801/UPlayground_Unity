using UnityEngine;
using UPlayGround.Ability.Core;
using AbilityAttackInfo = global::UPlayGround.Data.AbilityAttackInfo;

namespace UPlayGround.Ability.UPlayGround
{
    [CreateAssetMenu(
        fileName = "AbilityPayload_",
        menuName = "UPlayGround/Ability/Execution Payload/Motion Attack")]
    public sealed class UPlayGroundMotionAbilityPayloadSO : AbilityExecutionPayloadSO
    {
        public AbilityAttackInfo attackInfo = new();

        public bool IsExecutable =>
            attackInfo?.baseInfo != null
            && attackInfo.baseInfo.motionKey.IsValid;

        public bool IsAttackExecutable =>
            attackInfo?.baseInfo != null && IsExecutable;
    }
}
