using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Component;
using UPlayGround.Data;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 가드 상태 - 전투 중 경계 자세로 타겟을 주시하며 대기
    /// Guard 모션이 있는 액터만 사용 가능
    /// </summary>
    public class EnemyGuardState : GameActorState
    {
        public override string StateName => "Guard";
        
        private EnemyBrain _brain;
        private EnemyDetection _detection;
        private EnemyCombat _combat;
        
        private float _guardDuration;
        private float _guardTimer;
        
        public EnemyGuardState(ActorMovementController controller, EnemyBrain brain, EnemyDetection detection, float duration) : base(controller)
        {
            _brain = brain;
            _detection = detection;
            _guardDuration = duration;
        }

        public override bool CanTransitionState(string stateName)
        {
            return true;
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            
            MonsterActor monsterActor = gameActor as MonsterActor;
            if (monsterActor)
            {
                _combat = monsterActor.Combat;
                if (_combat != null)
                {
                    _combat.IsGuarding = true;
                }
            }
            
            _guardTimer = 0f;
            gameActor.Animator.PlayMotion(AnimKey.Guard, 0.2f);
        }

        public override void OnExit(GameActorState toState)
        {
            if (_combat != null)
            {
                _combat.IsGuarding = false;
            }
            base.OnExit(toState);
        }

        public override void UpdateState(float deltaTime)
        {
            if (!motor.GroundingStatus.IsStableOnGround)
            {
                controller.TransitionToState(new EnemyAirborneState(controller));
                return;
            }
            
            _guardTimer += deltaTime;
            
            if (_guardTimer >= _guardDuration)
            {
                controller.TransitionToState(new EnemyIdleState(controller));
            }
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            if (_detection.HasTarget)
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
            if (motor.GroundingStatus.IsStableOnGround)
            {
                currentVelocity = Vector3.Lerp(
                    currentVelocity,
                    Vector3.zero,
                    1 - Mathf.Exp(-controller.StableMovementSharpness * deltaTime));
            }
        }

        public void OnAttackBlocked(AttackData incomingAttack)
        {   
            controller.AddVelocity(incomingAttack.attackDirection.normalized * 2.0f);
        }
    }
}
