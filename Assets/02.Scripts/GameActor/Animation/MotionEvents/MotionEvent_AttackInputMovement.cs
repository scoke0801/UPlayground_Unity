using System;
using UnityEngine;
using UPlayGround.Manager;
using UPlayGround.State;

namespace UPlayGround.Data.Event
{
    internal static class SignatureMotionEventTarget
    {
        internal static PlayerActor ResolvePlayer(GameObject target)
        {
            if (target == null || !Services.TryGet(out IActorQueryService actors)) return null;
            var player = actors.Player as PlayerActor;
            return player != null && target.transform.IsChildOf(player.transform) ? player : null;
        }
    }

    /// <summary>공격 상태를 유지한 채 저작된 구간의 입력 이동과 방향 전환을 허용한다.</summary>
    [Serializable]
    [MotionEventDescriptor("AttackInputMovement", "Movement / Time", 0,
        "공격 중 입력 이동을 허용합니다.", "move", "input", "이동", "회전")]
    public sealed class AttackInputMovementEvent : MotionEventBase
    {
        [Min(0f)] public float speedMultiplier = 0.55f;
        [Min(0f)] public float rotationSpeed = 360f;

        public override MotionEventEnemyExecutionPolicy EnemyExecutionPolicy => MotionEventEnemyExecutionPolicy.Ignored;
        public override string GetDisplayName() => "공격 중 입력 이동";

        public override void Execute(GameObject target)
        {
            if (SignatureMotionEventTarget.ResolvePlayer(target)?.PlayerController?.CurrentState is PlayerAttackState state)
                state.BeginInputMovement(this, speedMultiplier, rotationSpeed);
        }

        public override void OnCompleteEvent(GameObject target)
        {
            if (SignatureMotionEventTarget.ResolvePlayer(target)?.PlayerController?.CurrentState is PlayerAttackState state)
                state.EndInputMovement(this);
        }
    }
}
