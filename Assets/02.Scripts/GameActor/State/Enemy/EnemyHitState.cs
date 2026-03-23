using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UPlayGround.Component;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 몬스터 피격 경직 상태
    /// PoiseStat에 의해 Poise가 소진됐을 때만 진입한다.
    /// reactionType에 따라 경직 강도(애니 길이)가 달라진다.
    /// </summary>
    public class EnemyHitState : GameActorState
    {
        public override string StateName => "Hit";

        private readonly AttackData _attackData;
        private bool _fromAerialState; // 공중 State에서 진입한 경우 true

        public EnemyHitState(ActorMovementController controller, AttackData attackData = null) : base(controller)
        {
            _attackData = attackData;
        }

        public override bool CanTransitionState(string stateName) => true;

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            var memory = gameActor.GetComponent<EnemyTacticalMemory>();
            memory?.NotifyTookDamage();

            // 공중 State에서 피격 → Poise Break 시 강제 착지 요청
            _fromAerialState = fromState?.StateName is "Aerial" or "AerialAttack" or "TakeOff";
            if (_fromAerialState)
            {
                gameActor.GetComponent<AerialBehaviorLayer>()?.RequestForceLand();
                motor.SetGroundSolvingActivation(true);
            }

            // reactionType에 따라 경직 애니 선택
            AnimKey hitAnim    = GetHitAnimKey();
            float fadeDuration = hitAnim == AnimKey.Knockback ? 0.1f : 0.2f;

            var state = gameActor.Animator.PlayMotion(hitAnim, fadeDuration);
            if (state != null)
                state.OwnedEvents.OnEnd = OnHitEnd;
            else
                OnHitEnd();
        }

        public override void UpdateState(float deltaTime)
        {
            // 공중 몬스터 피격(Aerial → Hit)은 RequestForceLand로 처리되므로 AirborneState 전환 스킵
            if (!_fromAerialState && !motor.GroundingStatus.IsStableOnGround)
                controller.TransitionToState(new EnemyAirborneState(controller));
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
            controller.TransitionToState(new EnemyIdleState(controller));
        }

        private AnimKey GetHitAnimKey()
        {
            if (_attackData == null) return AnimKey.Hit_F;

            return _attackData.reactionType switch
            {
                AttackReactionType.KnockBack  => AnimKey.Knockback,
                AttackReactionType.Pull       => AnimKey.Hit_F,
                AttackReactionType.Airborne   => AnimKey.Hit_F,
                AttackReactionType.Knockdown  => gameActor.Animator.HasMotion(AnimKey.Knockback, true)
                                                    ? AnimKey.Knockback
                                                    : AnimKey.Hit_F,
                AttackReactionType.Grab       => AnimKey.Hit_F,        // 전용 State로 가지만 안전장치
                AttackReactionType.Heavy      => AnimKey.Hit_F,
                _                             => AnimKey.Hit_F,
            };
        }
    }
}
