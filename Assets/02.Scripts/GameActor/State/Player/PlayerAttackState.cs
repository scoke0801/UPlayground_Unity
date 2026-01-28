using UnityEngine;
using UPlayGround.Data.Enum;
using UPlayGround.GameActor.Component;
using UPlayGround.GameActor.MovementController;

namespace UPlayGround.GameActor.State
{
    /// <summary>
    /// 구르기 상태
    /// </summary>
    public class PlayerAttackState : PlayerActorState
    {
        public override string StateName => "Attack";
        
        private PlayerCombat _combat;
        private PlayerEquipment _equipment;
        
        private AttackData _currentAttack;
        private float _attackTimer;

        private bool _comboInputted = false;
        private bool _isHeavyAttack = false;
        
        private Vector3 rootMotionVelocity;
        
        private readonly AnimKey[] skillAnimKeys = 
        { 
            AnimKey.Skill_1, 
            AnimKey.Skill_2, 
            AnimKey.Skill_3, 
            AnimKey.Skill_4 
        };
        
        public PlayerAttackState(ActorMovementController controller) : base(controller)
        {
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            playerActor.Animator.ApplyRootMotion(true);
            
            _combat = playerActor.GetCombat();
            _equipment = playerActor.GetPlayerEquipment();

            _isHeavyAttack = playerController.HasHeavyAttackInput();
            
            var animState = gameActor.Animator.PlayAnimation(GetAnimKey(), 0.25f);
            if (animState != null)
            {
                animState.OwnedEvents.OnEnd = ChangeToNextState;
            }
        }

        public override void OnExit(GameActorState toState)
        {
            base.OnExit(toState);
            
            playerActor.Animator.ApplyRootMotion(false);

        }

        public override void UpdateState(float deltaTime)
        {
            _attackTimer += deltaTime;
        
            // 콤보 입력 체크 (Component가 타이밍 관리)
            if (_combat.CanCombo)
            {
                if (playerController.HasAttackInput())
                {
                    _comboInputted = true;
                    _isHeavyAttack = false;
                }
                else if (playerController.HasHeavyAttackInput())
                {
                    _comboInputted = true;
                    _isHeavyAttack = true;
                }
                return;
            }
            
        }
        
        private void ChangeToNextState()
        {
            if (_comboInputted)
            {
                _comboInputted = false;

                var animState = gameActor.Animator.PlayAnimation(GetAnimKey(), 0.25f);
                if (animState != null)
                {
                    animState.OwnedEvents.OnEnd = ChangeToNextState;
                }
            }
            else
            {
                _combat.ResetCombo();
                // 이동 입력이 있으면 GroundMove, 없으면 Idle
                if (playerController.HasMoveInput())
                {
                    controller.TransitionToState(new PlayerGroundMoveState(controller));
                }
                else
                {
                    controller.TransitionToState(new PlayerIdleState(controller));
                }
            }
        }

        private AnimKey GetAnimKey()
        {
            for (int i = 0; i < skillAnimKeys.Length; i++)
            {
                if (playerController.HasSkillInput(i))
                {
                    return skillAnimKeys[i];
                }
            }

            _currentAttack = (_isHeavyAttack) ? _combat.ExecuteHeavyAttack() : _combat.ExecuteAttack();

            return _currentAttack.animKey;
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            base.UpdateVelocity(ref currentVelocity, deltaTime);
            
            // // 1. Animator에서 이번 프레임에 이동해야 할 루트 모션 델타값 추출
            // Vector3 rootMovement = playerActor.Animator.DeltaPosition;
            // Quaternion rootRotation = playerActor.Animator.DeltaRotation;
            //
            // // 2. KCC의 KinematicCharacterMotor에 해당 이동량 적용
            // // 루트 모션은 거리(m) 단위이므로, 속도(m/s)로 변환하려면 Time.deltaTime으로 나눕니다.
            // Vector3 velocityFromRootMotion = rootMovement / Time.deltaTime;
            // currentVelocity = velocityFromRootMotion;

            currentVelocity = gameActor.Animator.DeltaPosition / deltaTime;
        }
    }
}