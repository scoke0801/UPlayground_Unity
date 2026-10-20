using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Combat
{
    /// <summary>브레이크 트리거 전용 범위 리액션 데이터. 별도 공격 모션을 시작하지 않는다.</summary>
    [CreateAssetMenu(menuName = "UPlayGround/Ability/Execution Payload/Elite Break Shockwave")]
    public sealed class EliteBreakShockwavePayloadSO : AbilityExecutionPayloadSO
    {
        [Min(0f)] public float radius = 4f;
        public AttackReactionType reaction = AttackReactionType.Knockdown;
        [Min(0f)] public float reactionSeconds = 2f;
        public EliteBreakShockwaveEffectSO presentation;
    }
}
