using UnityEngine;
using UPlayGround.Ability.Core;
using AbilityAttackInfo = global::UPlayGround.Data.AbilityAttackInfo;

namespace UPlayGround.Ability.UPlayGround
{
    [CreateAssetMenu(
        fileName = "AbilityPayload_",
        menuName = "UPlayGround/Ability/Execution Payload/Motion Attack")]
    public class UPlayGroundMotionAbilityPayloadSO : AbilityExecutionPayloadSO
    {
        public AbilityAttackInfo attackInfo = new();

        /// <summary>
        /// 실행 가능 여부는 Motion Key만으로 결정된다. 히트 페이즈가 없는
        /// 모션 전용 Ability도 실행 대상이므로 baseInfo를 전제로 두지 않는다.
        /// </summary>
        public virtual bool IsExecutable =>
            attackInfo != null && attackInfo.motionKey.IsValid;

        /// <summary>공격으로서 실행 가능한지. 모션에 더해 공격 수치까지 필요하다.</summary>
        public bool IsAttackExecutable =>
            IsExecutable && attackInfo.baseInfo != null;
    }
}
