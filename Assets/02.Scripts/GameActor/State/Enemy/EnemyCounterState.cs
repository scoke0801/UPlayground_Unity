using UnityEngine;
using UPlayGround.Component;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 카운터 공격 상태
    /// Guard 블록 성공 직후 진입. 빠른 전진 + 공격으로 Guard를 의미있게 만든다.
    /// 별도 애니메이션 없이 Attack 애니를 활용하되, 진입 시 빠른 전진 속도를 부여해 체감을 차별화한다.
    /// </summary>
    public class EnemyCounterState : GameActorState
    {
        public override string StateName => "Counter";
        public override bool BlocksBehaviorTree => true;

        private readonly EnemyCombat _combat;
        private readonly EnemyAIContext _context;
        private readonly EnemyDetection _detection;
        private readonly EnemyTacticalMemory _memory;

        private EnemyAttackInfo _skill;
        private bool _isActive;

        // 카운터 전진 - Guard 블록 후 순간적으로 파고드는 느낌
        private const float DASH_IN_SPEED   = 10f;
        private const float DASH_IN_DURATION = 0.15f;
        private float _dashTimer;

        public EnemyCounterState(
            ActorMovementController controller,
            EnemyCombat combat,
            EnemyAIContext context,
            EnemyDetection detection,
            EnemyTacticalMemory memory) : base(controller)
        {
            _combat   = combat;
            _context  = context;
            _detection = detection;
            _memory   = memory;
        }

        public override bool CanTransitionState(string stateName) => true;

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            _isActive  = true;
            _dashTimer = 0f;

            float distance = _detection.DistanceToTarget;
            _skill = _combat.SelectAndExecuteSkill(distance);

            if (_skill == null)
            {
                // 사용 가능한 스킬이 없으면 그냥 추격으로 빠짐
                controller.TransitionToState(new EnemyChaseState(controller, _context, _detection));
                return;
            }

            var animState = gameActor.Animator.PlayMotion(_skill.baseInfo.animKey, 0.05f);
            if (animState != null)
                animState.OwnedEvents.OnEnd = OnCounterEnd;
            else
                OnCounterEnd();
        }

        public override void OnExit(GameActorState toState)
        {
            base.OnExit(toState);
            _isActive = false;
            _combat.ClearHitTargets();
        }

        public override void UpdateState(float deltaTime)
        {
            if (!_isActive) return;

            if (_skill?.baseInfo.attackType == AttackType.Melee && _combat.IsPossibleCollide)
                _combat.CheckMeleeAttackHit();
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            if (!_detection.HasTarget) return;

            Vector3 dir = (_detection.CurrentTarget.position - motor.TransientPosition).normalized;
            dir.y = 0;
            if (dir.sqrMagnitude > 0.01f)
            {
                currentRotation = Quaternion.Slerp(
                    currentRotation,
                    Quaternion.LookRotation(dir),
                    1 - Mathf.Exp(-controller.OrientationSharpness * deltaTime));
            }
            currentRotation = currentRotation.normalized;
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            float lastY = currentVelocity.y;

            // 초반 DASH_IN_DURATION 동안 타겟 방향으로 빠르게 전진
            if (_dashTimer < DASH_IN_DURATION && _detection.HasTarget)
            {
                Vector3 dir = (_detection.CurrentTarget.position - motor.TransientPosition).normalized;
                dir.y = 0;
                currentVelocity = dir * DASH_IN_SPEED;
                _dashTimer += deltaTime;
            }
            else
            {
                // 루트모션 또는 정지
                currentVelocity = _skill != null
                    ? gameActor.Animator.DeltaPosition / deltaTime
                    : Vector3.zero;
            }

            currentVelocity.y = lastY;
            if (motor.GroundingStatus.IsStableOnGround)
            {
                if (currentVelocity.y < 0) currentVelocity.y = -0.1f;
            }
            else
            {
                currentVelocity += controller.Gravity * deltaTime;
            }
        }

        private void OnCounterEnd()
        {
            if (!_isActive) return;

            _memory?.NotifyAttackLanded();
            _combat.ClearHitTargets();

            // 카운터 후 → 후퇴로 자연스럽게 빠짐 (거리 리셋)
            if (_detection.HasTarget)
            {
                controller.TransitionToState(
                    new EnemyRetreatState(controller, _context, _detection, _context.RetreatDistance));
            }
            else
            {
                controller.TransitionToState(new EnemyIdleState(controller));
            }
        }
    }
}
