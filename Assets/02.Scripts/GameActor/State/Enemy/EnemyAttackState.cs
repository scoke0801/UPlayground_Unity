using UnityEngine;
using UPlayGround.Data.Enum;
using UPlayGround.Component;
using UPlayGround.Data;
using UPlayGround.MovementController;
using UPlayGround.Data.Enemy;

namespace UPlayGround.State
{
    /// <summary>
    /// 공격 상태 - 타겟에게 공격 실행
    /// </summary>
    public class EnemyAttackState : GameActorState
    {
        public override string StateName => "Attack";
        
        private EnemyCombat _combat;
        private EnemyBrain _brain;
        private EnemyDetection _detection;
        
        private ComboData _currentAttack;
        private float _attackTimer;
        private bool _isAttackActive;
        private bool _hasRequestedCombo;
        
        public EnemyAttackState(ActorMovementController controller, EnemyCombat combat, EnemyBrain brain, EnemyDetection detection) : base(controller)
        {
            _combat = combat;
            _brain = brain;
            _detection = detection;
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
            
            _attackTimer = 0f;
            _isAttackActive = true;
            _hasRequestedCombo = false;
            
            // 공격 실행
            _currentAttack = _combat.ExecuteAttack();
            
            if (_currentAttack != null)
            {
                // 공격 애니메이션 재생
                var animState = gameActor.Animator.PlayMotion(_currentAttack.animKey, 0.1f);
                if (animState != null)
                {
                    animState.OwnedEvents.OnEnd = OnAttackAnimationEnd;
                }
                else
                {
                    Debug.LogWarning($"[EnemyAttackState] 애니메이션을 찾을 수 없습니다: {_currentAttack.animKey}");
                    OnAttackAnimationEnd();
                }
                
                Debug.Log($"[EnemyAttackState] 공격 시작: {_currentAttack.animKey}");
            }
            else
            {
                Debug.LogWarning("[EnemyAttackState] 공격 정보가 없습니다!");
                TransitionToNextState();
            }
        }

        public override void OnExit(GameActorState toState)
        {
            base.OnExit(toState);
            _isAttackActive = false;
            _combat.ClearHitTargets();

            Debug.Log("[EnemyAttackState] 공격 종료");
        }

        public override void UpdateState(float deltaTime)
        {
            if (!_isAttackActive || _currentAttack == null)
                return;
            
            _attackTimer += deltaTime;
            
            if(_combat.IsPossibleCollide)
            {
                _combat.CheckAttackHit();
            }
        }

        private void OnAttackAnimationEnd()
        {
            if (!_isAttackActive)
                return;
            
            _combat.ClearHitTargets();
            
            if (_hasRequestedCombo && _combat.CurrentComboIndex < _combat.AttackData.AttackList.Count - 1)
            {
                // 다음 콤보로 진행
                _combat.AdvanceCombo();
                controller.TransitionToState(new EnemyAttackState(controller, _combat, _brain, _detection));
            }
            else
            {
                // 공격 종료
                _combat.ResetCombo();
                TransitionToNextState();
            }
        }

        private void TransitionToNextState()
        {
            // 타겟이 여전히 범위 내에 있으면 Chase, 없으면 Idle/Patrol
            if (_detection.HasTarget && _detection.DistanceToTarget <= _brain.AttackRange * 2f)
            {
                controller.TransitionToState(new EnemyChaseState(controller, _brain, _detection));
            }
            else if (_brain.EnablePatrol)
            {
                controller.TransitionToState(new EnemyPatrolState(controller, _brain));
            }
            else
            {
                controller.TransitionToState(new EnemyIdleState(controller));
            }
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            if (_detection.HasTarget && _attackTimer < 0.3f)
            {
                // 공격 초반에는 타겟을 향해 회전
                Vector3 directionToTarget = (_detection.CurrentTarget.position - motor.TransientPosition).normalized;
                directionToTarget.y = 0;
                
                if (directionToTarget.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                    currentRotation = Quaternion.Slerp(
                        currentRotation,
                        targetRotation,
                        1 - Mathf.Exp(-controller.OrientationSharpness * deltaTime));
                }
            }
            
            currentRotation = currentRotation.normalized;
        }
        
        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            base.UpdateVelocity(ref currentVelocity, deltaTime);
            
            currentVelocity = gameActor.Animator.DeltaPosition / deltaTime;
            
            //Debug.Log($"CurrentVelocity: {currentVelocity}");
        }
    }
}