using UnityEngine;
using UPlayGround.Data.EnumType;
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
        
        public EnemyAttackState(ActorMovementController controller, EnemyCombat combat, EnemyBrain brain, EnemyDetection detection) : base(controller)
        {
            _combat = combat;
            _brain = brain;
            _detection = detection;
        }

        public override bool CanTransitionState(string stateName)
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
            
            // 거리 기반 스킬 선택
            float distanceToTarget = _detection.DistanceToTarget;
            _currentSkill = _combat.SelectAndExecuteSkill(distanceToTarget);

            if (_currentSkill != null)
            {
                // 공격 애니메이션 재생
                var animState = gameActor.Animator.PlayMotion(_currentSkill.baseInfo.animKey, 0.1f);
                if (animState != null)
                {
                    animState.OwnedEvents.OnEnd = OnAttackAnimationEnd;
                }
                else
                {
                    Debug.LogWarning($"[EnemyAttackState] 애니메이션을 찾을 수 없습니다: {_currentSkill.baseInfo.animKey}");
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
        }

        public override void UpdateState(float deltaTime)
        {
            if (!_isAttackActive || _currentSkill == null)
                return;
            
            _attackTimer += deltaTime;
            
            // 근접 공격 히트 체크
            if (_currentSkill.baseInfo.attackType == AttackType.Melee && _combat.IsPossibleCollide)
            {
                _combat.CheckMeleeAttackHit();
            }
        }

        private void OnAttackAnimationEnd()
        {
            // 지면에서 떨어지면 Airborne 상태로 전환
            if (!motor.GroundingStatus.IsStableOnGround)
            {
                controller.TransitionToState(new EnemyAirborneState(controller));
                return;
            }
            
            if (!_isAttackActive)
                return;
            
            _combat.ClearHitTargets();
            TransitionToNextState();
        }

        private void TransitionToNextState()
        {
            // 타겟이 유효할 때 → 확률 기반 행동 분기
            if (_detection.HasTarget)
            {
                float distance = _detection.DistanceToTarget;
                float roll = Random.value;
                
                // 1) 연속 공격 확률 체크 - 사거리 내일 때만
                if (roll < _brain.ContinueAttackChance && distance <= _brain.GetMaxAttackRange() * 1.2f)
                {
                    controller.TransitionToState(
                        new EnemyAttackState(controller, _combat, _brain, _detection));
                    return;
                }
                
                roll = Random.value;
                
                // 2) Guard 모션 보유 시 가드 확률 체크
                if (_brain.HasGuardMotion && roll < _brain.GuardChance)
                {
                    controller.TransitionToState(
                        new EnemyGuardState(controller, _brain, _detection, _brain.GuardDuration));
                    return;
                }
                
                // 3) 후퇴 확률 체크 - 가까이 붙어 있을 때
                roll = Random.value;
                if (roll < _brain.RetreatChance && distance < _brain.RetreatDistance)
                {
                    controller.TransitionToState(
                        new EnemyRetreatState(controller, _brain, _detection, _brain.RetreatDistance));
                    return;
                }
                
                // 4) 기본 → 추적
                controller.TransitionToState(
                    new EnemyChaseState(controller, _brain, _detection));
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
            if (_currentSkill != null && _currentSkill.baseInfo.attackType == AttackType.Ranged)
            {
                currentVelocity = Vector3.zero;
            }
            else
            {
                currentVelocity = gameActor.Animator.DeltaPosition / deltaTime;
            }
            
            if (motor.GroundingStatus.IsStableOnGround == false)
            {   
                // Gravity
                currentVelocity += controller.Gravity * deltaTime;
            }
        }
    }
}