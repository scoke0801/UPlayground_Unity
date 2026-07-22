using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 플레이어 잡힘 상태.
    /// Grab 공격에 피격 시 진입. 일정 시간 행동 불능.
    /// 공격자가 FireForcedMotionReleased()를 호출하면 즉시 해제되며,
    /// 호출 없이 grabDuration이 만료되면 자동 탈출한다.
    /// </summary>
    public class PlayerGrabbedState : PlayerActorState
    {
        public override string StateName => "Grabbed";

        private readonly AttackData _attackedData;
        private float _remainingDuration;

        public PlayerGrabbedState(ActorMovementController controller, AttackData attackedData)
            : base(controller)
        {
            _attackedData = attackedData;
        }

        public override bool CanTransitionState(string stateName) => true;

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            _remainingDuration = _attackedData?.grabDuration ?? 1.5f;

            playerActor.GetCombat()?.RefreshCombatState();

            // 공격자가 있으면 릴리즈 이벤트 구독
            if (_attackedData?.attacker != null)
                _attackedData.attacker.OnForcedMotionReleased += Escape;

            // victimForcedMotionSlot > Grabbed > Hit_F 순으로 폴백
            UPlayGround.Gameplay.Tag.GameplayTag animKey;
            if (_attackedData?.victimForcedMotionSlot != default &&
                gameActor.Animator.HasMotion(_attackedData.victimForcedMotionSlot))
                animKey = _attackedData.victimForcedMotionSlot;
            else if (gameActor.Animator.HasMotion(UPlayGround.Data.Actor.Animation.MotionTags.Grabbed))
                animKey = UPlayGround.Data.Actor.Animation.MotionTags.Grabbed;
            else
                animKey = UPlayGround.Data.Actor.Animation.MotionTags.Hit_F;

            gameActor.Animator.PlayMotion(animKey, 0.1f);
        }

        public override void OnExit(GameActorState toState)
        {
            base.OnExit(toState);

            if (_attackedData?.attacker != null)
                _attackedData.attacker.OnForcedMotionReleased -= Escape;
        }

        public override void UpdateState(float deltaTime)
        {
            if (controller.CurrentState != this)
                return;

            _remainingDuration -= deltaTime;

            if (_remainingDuration <= 0f)
                Escape();
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            currentRotation = currentRotation.normalized;
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            currentVelocity = Vector3.zero;
        }

        private void Escape()
        {
            if (controller.CurrentState != this)
                return;

            // 중복 호출 방지
            if (_remainingDuration < -99f) return;
            _remainingDuration = float.MinValue;

            if (gameActor.Animator.HasMotion(UPlayGround.Data.Actor.Animation.MotionTags.Grabbed_End))
            {
                var state = gameActor.Animator.PlayMotion(UPlayGround.Data.Actor.Animation.MotionTags.Grabbed_End, 0.1f);
                if (state != null)
                    state.OwnedEvents.OnEnd = TransitionOut;
                else
                    TransitionOut();
            }
            else
            {
                TransitionOut();
            }
        }

        private void TransitionOut()
        {
            if (controller.CurrentState != this)
                return;

            controller.TransitionToState(new PlayerIdleState(controller));
        }
    }
}
