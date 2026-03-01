using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    public class PlayerJumpAttackState : PlayerActorState
    {
        
        public override string StateName => "JumpAttack";

        private AttackData _attackData;
        private float _timer;
        private bool _hasHit;

        public PlayerJumpAttackState(ActorMovementController controller) : base(controller)
        {
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            //_attackData = _combat.GetJumpAttack();
            _timer = 0f;
            _hasHit = false;

            var state = gameActor.Animator.PlayMotion(AnimKey.JumpAttack_1, 0.1f);
            if (state != null)
            {
                gameActor.Animator.OnMotionSetCompleted += OnAttackAnimationEnd;
            }
        }

        public override void OnExit(GameActorState toState)
        {
            gameActor.Animator.OnMotionSetCompleted -= OnAttackAnimationEnd;

            base.OnExit(toState);
        }
        private void OnAttackAnimationEnd()
        {
            controller.TransitionToState(new PlayerIdleState(controller));
        }


        public override bool CanTransitionState(string stateName)
        {
            if (stateName == "Hit")
                return false;
            return true;
        }

        public override void UpdateState(float deltaTime)
        {
            _timer += deltaTime;

            // 착지 시 또는 모션 종료 시 → 복귀
            if (motor.GroundingStatus.IsStableOnGround)
            {
                OnLanded();
                return;
            }
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            currentRotation *= gameActor.Animator.DeltaRotation;
            currentRotation = currentRotation.normalized;
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            base.UpdateVelocity(ref currentVelocity, deltaTime);
            
            // 수평 이동은 유지, 수직은 아래로 가속
            currentVelocity = motor.CharacterUp * -15f; // 아래 방향 고정 속도
        }

        private void OnLanded()
        {
            // 착지 시 충격파 히트박스 발동
            // _combat.ExecuteHitbox(_attackData);

            // 착지 모션이 있다면
            // gameActor.Animator.PlayMotion(AnimKey.JumpAttackLand, 0.1f);

            //controller.TransitionToState(new PlayerIdleState(controller));
        }
    }
}