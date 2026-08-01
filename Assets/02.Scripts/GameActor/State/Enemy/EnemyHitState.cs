using UnityEngine;
using UPlayGround.Combat;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UPlayGround.Components;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 몬스터 피격 경직 상태
    /// PoiseStat에 의해 Poise가 소진됐을 때만 진입한다.
    /// reactionType에 따라 경직 강도(애니 길이)가 달라진다.
    /// </summary>
    public class EnemyHitState : EnemyActorState
    {
        public override ActorStateId StateId => ActorStateId.Hit;
        public override bool BlocksBehaviorTree => true;

        private readonly HitContext _hit;
        public EnemyHitState(ActorMovementController controller, in HitContext hit = default) : base(controller)
        {
            _hit = hit;
        }

        public override bool CanTransitionState(ActorStateId fromState) => true;

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            // 워프 진행 중이면 즉시 clear (Hit 모션이 우선).
            controller.MotionWarp?.ClearTarget();

            var memory = gameActor.GetComponent<EnemyTacticalMemory>();
            if (memory != null && !memory.WasHitRecently(0.05f))
                memory.NotifyTookDamage();

            // reactionType에 따라 경직 애니 선택
            UPlayGround.Gameplay.Tag.GameplayTag hitAnim    = GetHitAnimKey();
            float fadeDuration = hitAnim == UPlayGround.Data.Actor.Animation.MotionTags.Knockback ? 0.1f : 0.2f;

            var state = gameActor.Animator.PlayMotion(hitAnim, fadeDuration);
            if (state != null)
                state.OwnedEvents.OnEnd = OnHitEnd;
            else
                OnHitEnd();
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (motor.GroundingStatus.IsStableOnGround)
            {
                currentVelocity = motor.GetDirectionTangentToSurface(
                    currentVelocity,
                    motor.GroundingStatus.GroundNormal) * currentVelocity.magnitude;

                currentVelocity = Vector3.Lerp(
                    currentVelocity,
                    Vector3.zero,
                    1 - Mathf.Exp(-controller.StableMovementSharpness * deltaTime));
            }
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            currentRotation = currentRotation.normalized;
        }

        private void OnHitEnd()
        {
            controller.TransitionToState(ActorStateId.Idle);
        }

        private UPlayGround.Gameplay.Tag.GameplayTag GetHitAnimKey()
        {
            // 공격별 전용 피격 애니(victimForcedMotionSlot)가 지정돼 있고 보유 모션이면 최우선 사용.
            if (_hit.VictimForcedMotionSlot != default &&
                gameActor.Animator.HasMotion(_hit.VictimForcedMotionSlot))
                return _hit.VictimForcedMotionSlot;

            return _hit.ReactionType switch
            {
                AttackReactionType.KnockBack  => UPlayGround.Data.Actor.Animation.MotionTags.Knockback,
                AttackReactionType.Pull       => UPlayGround.Data.Actor.Animation.MotionTags.Hit_F,
                AttackReactionType.Airborne   => UPlayGround.Data.Actor.Animation.MotionTags.Hit_F,
                AttackReactionType.Knockdown  => gameActor.Animator.HasMotion(UPlayGround.Data.Actor.Animation.MotionTags.Knockback, true)
                                                    ? UPlayGround.Data.Actor.Animation.MotionTags.Knockback
                                                    : UPlayGround.Data.Actor.Animation.MotionTags.Hit_F,
                AttackReactionType.Grab       => UPlayGround.Data.Actor.Animation.MotionTags.Hit_F,        // 전용 State로 가지만 안전장치
                AttackReactionType.Heavy      => UPlayGround.Data.Actor.Animation.MotionTags.Hit_F,
                _                             => UPlayGround.Data.Actor.Animation.MotionTags.Hit_F,
            };
        }
    }
}
