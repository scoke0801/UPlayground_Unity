using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 브레이크 특수공격의 windup(슬라이드·카메라 연출) 동안 타겟 몬스터를 제자리에 붙잡아 두는 상태.
    /// 실제 피격 반응(넉백/Knockdown)은 임팩트 시점에 MonsterActor.OnTakeSpecialBreakAttack에서 발생하므로,
    /// 이 상태는 타격 전 구간에서 피격 모션이나 넉백을 재생하지 않고 중립 자세로만 대기한다.
    /// </summary>
    public class EnemySpecialBreakVictimState : EnemyActorState
    {
        public override string StateName => "SpecialBreakVictim";
        public override bool BlocksBehaviorTree => true;

        private readonly float _duration;
        private float _remainingDuration;

        // knockback* 파라미터는 임팩트 반응을 OnTakeSpecialBreakAttack의 Knockdown으로 유지하는 정책상 현재 미사용.
        // (호출부 시그니처 보존을 위해 유지)
        public EnemySpecialBreakVictimState(
            ActorMovementController controller,
            float duration = 1.2f,
            Transform source = null,
            float knockbackDistance = 0.75f,
            float knockbackDuration = 0.18f,
            float maxKnockbackSpeed = 7f) : base(controller)
        {
            _duration = Mathf.Max(0.1f, duration);
        }

        public override bool CanTransitionState(string stateName) => stateName is "Death";

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            controller.MotionWarp?.ClearTarget();
            _remainingDuration = _duration;

            // 타격 전 중립 홀드 — 피격(Grabbed/Hit_F) 모션을 미리 재생하지 않는다.
            if (gameActor.Animator.HasMotion(UPlayGround.Data.Actor.Animation.MotionTags.Idle, true))
                gameActor.Animator.PlayMotion(UPlayGround.Data.Actor.Animation.MotionTags.Idle, 0.1f);
        }

        public override void UpdateState(float deltaTime)
        {
            _remainingDuration -= deltaTime;
            if (_remainingDuration <= 0f)
                controller.TransitionToState(new EnemyIdleState(controller));
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            currentRotation = currentRotation.normalized;
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (!motor.GroundingStatus.IsStableOnGround)
            {
                currentVelocity += controller.Gravity * deltaTime;
                return;
            }

            // 제자리 고정
            currentVelocity = Vector3.Lerp(
                currentVelocity,
                Vector3.zero,
                1f - Mathf.Exp(-controller.StableMovementSharpness * deltaTime));
        }
    }
}
