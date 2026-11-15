using System;
using UnityEngine;
using UPlayGround.State;

namespace UPlayGround.Data.Event
{
    /// <summary>공격 상태의 도약 구간에만 중력 배율을 합성한다.</summary>
    [Serializable]
    [MotionEventDescriptor("AttackGravity", "Movement / Time", 0,
        "공격 도약의 중력을 조정합니다.", "gravity", "jump", "중력", "도약")]
    public sealed class AttackGravityEvent : MotionEventBase
    {
        [Min(0f)] public float gravityMultiplier = 1f;

        public override MotionEventEnemyExecutionPolicy EnemyExecutionPolicy => MotionEventEnemyExecutionPolicy.Ignored;
        public override string GetDisplayName() => "공격 중력 배율";

        public override void Execute(GameObject target)
        {
            if (SignatureMotionEventTarget.ResolvePlayer(target)?.PlayerController?.CurrentState is PlayerAttackState state)
                state.BeginGravityOverride(this, gravityMultiplier);
        }

        public override void OnCompleteEvent(GameObject target)
        {
            if (SignatureMotionEventTarget.ResolvePlayer(target)?.PlayerController?.CurrentState is PlayerAttackState state)
                state.EndGravityOverride(this);
        }
    }
}
