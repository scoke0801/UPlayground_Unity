using UnityEngine;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Data;
using UPlayGround.Data.Ability;

namespace UPlayGround.Gameplay.Ability
{
    /// <summary>
    /// Ultimate Variant가 소유하는 프로젝트 실행 Payload.
    /// 공격 수치와 Motion Key는 기본 Motion Payload를 그대로 사용하고,
    /// 궁극기 전용 잠금·타겟·연출 데이터만 Sequence 에셋으로 확장한다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "AbilityPayload_Ultimate_",
        menuName = "UPlayGround/Ability/Execution Payload/Ultimate Sequence")]
    public sealed class UPlayGroundUltimateAbilityPayloadSO
        : UPlayGroundMotionAbilityPayloadSO
    {
        [Tooltip("이 Ultimate Variant가 실행할 연출 시퀀스입니다.")]
        public UltimateSequenceAsset sequence;

        public override bool IsExecutable =>
            base.IsExecutable && sequence != null;
    }

    public static class UPlayGroundUltimateAbilityPayloadResolver
    {
        public static bool TryResolve(
            AbilityVariantDefinition variant,
            out UPlayGroundUltimateAbilityPayloadSO payload)
        {
            payload = variant?.executionPayload
                as UPlayGroundUltimateAbilityPayloadSO;
            return payload != null && payload.IsExecutable;
        }
    }
}
