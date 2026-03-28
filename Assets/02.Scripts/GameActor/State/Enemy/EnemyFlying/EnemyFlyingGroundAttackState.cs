using UnityEngine;
using UPlayGround.Component;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 비행 보스 지상 근접 공격 (Claw_Single).
    /// 기존 EnemyAttackState를 참고하되 FlyingBossBrain과 연동.
    /// </summary>
    public class EnemyFlyingGroundAttackState : GameActorState
    {
        public override string StateName => "Flying_GroundAttack";

        private readonly EnemyFlyingBrain _brain;
        private EnemyCombat _combat;
        private float _attackTimer;
        private bool _isActive;
        private EnemyAttackInfo _currentSkill;

        public EnemyFlyingGroundAttackState(ActorMovementController controller, EnemyFlyingBrain brain)
            : base(controller)
        {
            _brain = brain;
        }

        public override bool CanTransitionState(string stateName)
            => stateName is not "Hit"; // HyperArmor 중이므로 Hit 무시

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            _combat = _brain.Combat;
            _attackTimer = 0f;
            _isActive = true;

            // Hyper Armor ON
            gameActor.GetComponent<PoiseStat>()?.SetHyperArmor(true);

            // 거리 기반 스킬 선택 (Claw_Single이 선택되도록 SO에 세팅)
            float dist = _brain.Detection.DistanceToTarget;
            _currentSkill = _combat.SelectAndExecuteSkill(dist);

            if (_currentSkill != null)
            {
                var animState = gameActor.Animator.PlayMotion(_currentSkill.baseInfo.animKey, 0.1f);
                if (animState != null)
                    gameActor.Animator.OnMotionSetCompleted += OnAttackEnd;
                else
                    OnAttackEnd();
            }
            else
            {
                // 스킬 없으면 바로 Brain에 알림
                _isActive = false;
                _brain.OnGroundAttackFinished();
            }
        }

        public override void OnExit(GameActorState toState)
        {
            base.OnExit(toState);
            _isActive = false;
            _combat.ClearHitTargets();
            gameActor.Animator.OnMotionSetCompleted -= OnAttackEnd;
            gameActor.GetComponent<PoiseStat>()?.SetHyperArmor(false);
        }

        public override void UpdateState(float deltaTime)
        {
            if (!_isActive || _currentSkill == null) return;

            _attackTimer += deltaTime;

            if (_currentSkill.baseInfo.attackType == AttackType.Melee && _combat.IsPossibleCollide)
                _combat.CheckMeleeAttackHit();
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            // 공격 초반에만 타겟 방향으로 회전
            if (_brain.Detection.HasTarget && _attackTimer < 0.3f)
            {
                Vector3 dir = (_brain.Detection.CurrentTarget.position - motor.TransientPosition);
                dir.y = 0;
                if (dir.sqrMagnitude > 0.01f)
                {
                    Quaternion target = Quaternion.LookRotation(dir.normalized);
                    currentRotation = Quaternion.Slerp(currentRotation, target,
                        1 - Mathf.Exp(-controller.OrientationSharpness * deltaTime));
                }
            }
            currentRotation = currentRotation.normalized;
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            float lastY = currentVelocity.y;

            // 근접이면 루트모션, 원거리면 정지
            if (_currentSkill != null && _currentSkill.baseInfo.attackType == AttackType.Ranged)
                currentVelocity = Vector3.zero;
            else
                currentVelocity = gameActor.Animator.DeltaPosition / deltaTime;

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

        private void OnAttackEnd()
        {
            if (!_isActive) return;
            _combat.ClearHitTargets();
            _brain.OnGroundAttackFinished();
        }
    }
}
