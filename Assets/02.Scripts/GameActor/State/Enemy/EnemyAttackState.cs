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
            
            // 현재의 수직 속도(중력값)를 임시 저장
            float lastVerticalVelocity = currentVelocity.y;

            // 1. 스킬 로직에 따른 속도 재설정 (여기서 속도가 0이 되거나 초기화됨)
            if (_currentSkill != null && _currentSkill.baseInfo.attackType == AttackType.Ranged)
            {
                currentVelocity = Vector3.zero;
            }
            else
            {
                currentVelocity = gameActor.Animator.DeltaPosition / deltaTime;
            }

            // 2. 이전 프레임의 수직 속도를 다시 복구
            currentVelocity.y = lastVerticalVelocity;

            // 3. 중력 누적 적용
            if (motor.GroundingStatus.IsStableOnGround)
            {
                // 지면에 안정적일 때 수직 속도 억제
                if (currentVelocity.y < 0) currentVelocity.y = -0.1f;
            }
            else
            {
                // 공중일 때 지속적으로 중력 가산
                currentVelocity += controller.Gravity * deltaTime;
            }
        }
    }
}