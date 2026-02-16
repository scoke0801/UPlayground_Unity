using UnityEngine;
using UPlayGround.Data.Enum;
using UPlayGround.Component;
using UPlayGround.Data;
using UPlayGround.MovementController;
using UPlayGround.Data.Enemy;

namespace UPlayGround.State
{
    public class EnemyAttackState : GameActorState
    {
        public override string StateName => "Attack";
        
        private EnemyCombat _combat;
        private EnemyBrain _brain;
        private EnemyDetection _detection;
        
        private EnemyAttackInfo _currentSkill;
        private float _attackTimer;
        private bool _isAttackActive;
        private bool _hasLaunchedProjectile; // 원거리 투사체 발사 여부
        
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
            _hasLaunchedProjectile = false;
            
            // 거리 기반 스킬 선택
            float distanceToTarget = _detection.DistanceToTarget;
            _currentSkill = _combat.SelectAndExecuteAttack(distanceToTarget);
            
            if (_currentSkill != null)
            {
                // 공격 애니메이션 재생
                var animState = gameActor.Animator.PlayMotion(_currentSkill.animKey, 0.1f);
                if (animState != null)
                {
                    animState.OwnedEvents.OnEnd = OnAttackAnimationEnd;
                }
                else
                {
                    Debug.LogWarning($"[EnemyAttackState] 애니메이션을 찾을 수 없습니다: {_currentSkill.animKey}");
                    OnAttackAnimationEnd();
                }
            }
            else
            {
                Debug.LogWarning("[EnemyAttackState] 사용 가능한 스킬이 없습니다!");
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
            if (!_isAttackActive || _currentSkill == null)
                return;
            
            _attackTimer += deltaTime;
            
            // 근접 공격 히트 체크
            if (_currentSkill.attackType == EnemyAttackType.Melee && _combat.IsPossibleCollide)
            {
                _combat.CheckMeleeAttackHit();
            }
        }

        private void OnAttackAnimationEnd()
        {
            if (!_isAttackActive)
                return;
            
            _combat.ClearHitTargets();
            TransitionToNextState();
        }

        private void TransitionToNextState()
        {
            if (_detection.HasTarget && _detection.DistanceToTarget <= _brain.GetMaxAttackRange() * 1.5f)
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
            
            // 원거리 공격은 제자리에, 근접 공격은 루트 모션 사용
            if (_currentSkill != null && _currentSkill.attackType == EnemyAttackType.Ranged)
            {
                currentVelocity = Vector3.zero;
            }
            else
            {
                currentVelocity = gameActor.Animator.DeltaPosition / deltaTime;
            }
        }
    }
}