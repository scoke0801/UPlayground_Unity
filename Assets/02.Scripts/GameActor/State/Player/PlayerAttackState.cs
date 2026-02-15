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

        public override bool CanTransitionToState(string stateName)
        {
            if (stateName == "Hit")
                return false;
            return true;
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            playerActor.Animator.ApplyRootMotion(true);
            _playerActorAnimator = playerActor.Animator as PlayerActorAnimator;
            
            _combat = playerActor.GetCombat();
            _combat.ResetCombo();
            
            _equipment = playerActor.GetPlayerEquipment();

            _isHeavyAttack = playerController.HasHeavyAttackInput();
            
            var animState = gameActor.Animator.PlayMotion(GetAnimKey(), 0.25f);
            if (animState != null)
            {
                animState.OwnedEvents.OnEnd = ()=>
                {
                    ChangeToNextState();
                };
            }
            else
            {
                ChangeToNextState();
            }
        }

        public override void OnExit(GameActorState toState)
        {
            _combat.ClearHitTargets();
            
            _playerActorAnimator.IsOpenedComboWindow = false;
            playerActor.Animator.ApplyRootMotion(false);
            
            base.OnExit(toState);
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
                    _combat.CloseComboWindow();
                }
                else if (InputManager.Instance.InputBuffer.ConsumeInput(PlayerAction.HeavyAttack) != null)
                {
                    _comboInputted = true;
                    _isHeavyAttack = true;
                    _combat.CloseComboWindow();
                }
            }

            if (_combat.IsPossibleCollide == false && _comboInputted)
            {
                ChangeToNextState();
            }
        }
        
        private void ChangeToNextState()
        {
            _combat.ClearHitTargets();
            
            if (_comboInputted)
            {
                var animState = gameActor.Animator.PlayMotion(GetAnimKey(), 0.25f);
                if (animState != null)
                {
                    animState.OwnedEvents.OnEnd = ()=>
                    {
                        ChangeToNextState();
                    };
                }
                
                _playerActorAnimator.IsOpenedComboWindow = false;
                
                _combat.CloseComboWindow();

                _comboInputted = false;
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

            if (_isHeavyAttack == false &&
                playerController.HasMoveInput() 
                && gameActor.MoveAnimType == BaseMoveAnimType.Sprint)
            {
                return AnimKey.DashAttack_1;
            }
            
            _currentAttack = (_isHeavyAttack) 
                ? _combat.ExecuteHeavyAttack(_comboInputted) 
                : _combat.ExecuteAttack(_comboInputted);

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
            // Lock-On 타겟이 있으면 타겟 방향으로 회전
            Transform lockOnTarget = CameraManager.Instance.GetLockOnTarget();
            if (lockOnTarget != null)
            {
                Vector3 directionToTarget = (lockOnTarget.position - gameActor.transform.position).normalized;
                directionToTarget.y = 0f; // Y축 제거 (수평 회전만)
                
                if (directionToTarget.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                    currentRotation = Quaternion.Slerp(currentRotation, targetRotation, deltaTime * 10f);
                }
            }
            // else
            // {
            //     // Lock-On이 없으면 Root Motion 회전 적용
            //     currentRotation *= gameActor.Animator.DeltaRotation;
            // }
            
            currentRotation = currentRotation.normalized;
        }
    }
}