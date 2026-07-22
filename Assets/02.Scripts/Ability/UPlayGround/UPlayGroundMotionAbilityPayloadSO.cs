using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Animation;
using UPlayGround.Data.EnumType;
using AbilityAttackInfo = global::UPlayGround.Data.AbilityAttackInfo;

namespace UPlayGround.Ability.UPlayGround
{
    [CreateAssetMenu(
        fileName = "AbilityPayload_",
        menuName = "UPlayGround/Ability/Execution Payload/Motion Attack")]
    public sealed class UPlayGroundMotionAbilityPayloadSO : AbilityExecutionPayloadSO
    {
        public AbilityAttackInfo attackInfo = new();

        public MotionSetAsset ResolveMotion(WeaponType weaponType) =>
            attackInfo?.baseInfo?.ResolveMotion(weaponType);

        public bool IsExecutable =>
            attackInfo?.baseInfo?.motionRef != null
            && attackInfo.baseInfo.motionRef.HasAnyMotion;

        public bool IsAttackExecutable =>
            attackInfo?.baseInfo != null && IsExecutable;
    }
}
