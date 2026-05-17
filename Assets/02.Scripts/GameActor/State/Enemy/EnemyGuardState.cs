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
        public override bool BlocksBehaviorTree => true;
        
        private EnemyAIContext _context;
        private EnemyDetection _detection;
        private EnemyCombat _combat;
        private UPlayGround.Component.EnemyTacticalMemory _memory;
        
        private float _guardDuration;
        private float _guardTimer;
        
        public EnemyGuardState(ActorMovementController controller, EnemyAIContext context, EnemyDetection detection, float duration) : base(controller)
        {
            _context = context;
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
                _memory = gameActor.GetComponent<UPlayGround.Component.EnemyTacticalMemory>();
                if (_combat != null)
                    _combat.IsGuarding = true;
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
                // 가드 종료 → Brain 판단에 위임 (Idle로 가면 Brain이 즉시 결정)
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
            _memory?.NotifyBlocked();

            // 블록 성공 → 카운터 공격으로 즉시 전환
            if (_combat != null && _context != null && _detection != null)
            {
                controller.TransitionToState(
                    new EnemyCounterState(controller, _combat, _context, _detection, _memory));
                return;
            }

            // 카운터 불가 시 밀려나기만
            controller.AddVelocity(incomingAttack.attackDirection.normalized * 2.0f);
        }
    }
}
