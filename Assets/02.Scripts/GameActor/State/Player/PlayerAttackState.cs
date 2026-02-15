using UnityEngine;
using UPlayGround.Data.Enum;
using UPlayGround.Animation;
using UPlayGround.Component;
using UPlayGround.MovementController;
using UPlayGround.Manager; // InputManager를 사용하기 위해 추가
using UPlayGround.InputDefine; // PlayerAction 상수를 사용하기 위해 추가

namespace UPlayGround.State
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
        
        private PlayerActorAnimator _playerActorAnimator;
        
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
            _playerActorAnimator = playerActor.Animator as PlayerActorAnimator;
            
            _combat = playerActor.GetCombat();
            _equipment = playerActor.GetPlayerEquipment();

            _isHeavyAttack = playerController.HasHeavyAttackInput();
            
            var animState = gameActor.Animator.PlayMotion(GetAnimKey(), 0.25f);
            if (animState != null)
            {
                animState.OwnedEvents.OnEnd = ChangeToNextState;
            }
            else
            {
                ChangeToNextState();
            }
        }

        public override void OnExit(GameActorState toState)
        {
            base.OnExit(toState);
            
            _playerActorAnimator.IsOpenedComboWindow = false;
            playerActor.Animator.ApplyRootMotion(false);
        }

        public override void UpdateState(float deltaTime)
        {
            _attackTimer += deltaTime;

            // 콤보 입력 체크 (Component가 타이밍 관리)
            if (_combat.CanCombo)
            {
                if (InputManager.Instance.InputBuffer.ConsumeInput(PlayerAction.Attack) != null)
                {
                    _comboInputted = true;
                    _isHeavyAttack = false;
                }
                else if (InputManager.Instance.InputBuffer.ConsumeInput(PlayerAction.HeavyAttack) != null)
                {
                    _comboInputted = true;
                    _isHeavyAttack = true;
                }
            }

            if (_playerActorAnimator.IsOpenedComboWindow && _comboInputted)
            {
                ChangeToNextState();
            }
        }
        
        private void ChangeToNextState()
        {
            if (_comboInputted)
            {
                _comboInputted = false;
                _playerActorAnimator.IsOpenedComboWindow = false;

                var animState = gameActor.Animator.PlayMotion(GetAnimKey(), 0.25f);
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

            if (_isHeavyAttack != null &&
                playerController.HasMoveInput() 
                && gameActor.MoveAnimType == BaseMoveAnimType.Sprint)
            {
                return AnimKey.DashAttack_1;
            }
            
            _currentAttack = (_isHeavyAttack) ? _combat.ExecuteHeavyAttack() : _combat.ExecuteAttack();

            
            return _currentAttack?.animKey ?? AnimKey.None;
        }
    
        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            base.UpdateVelocity(ref currentVelocity, deltaTime);
            
            currentVelocity = gameActor.Animator.DeltaPosition / deltaTime;
            
            //Debug.Log($"CurrentVelocity: {currentVelocity}");
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            currentRotation *= gameActor.Animator.DeltaRotation;
            
            currentRotation = currentRotation.normalized;
            
        }
    }
}