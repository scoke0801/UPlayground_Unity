using UnityEngine;
using UPlayGround.Component;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 비행 몬스터 지상 근접 공격.
    /// 모션 완료 or 타임아웃 → Brain.OnGroundAttackFinished()
    /// </summary>
    public class EnemyFlyingGroundAttackState : EnemyActorState
    {
        public override string StateName => "Flying_GroundAttack";
        public override bool BlocksBehaviorTree => true;

        private readonly EnemyFlyingAIContext _brain;
        private EnemyCombat _combat;
        private float _attackTimer;
        private bool _isActive;
        private EnemyAttackInfo _currentSkill;

        private const float MotionTimeout = 3.0f;

        private float Cfg_MotionTimeout => _brain.FlyingSettings ? _brain.FlyingSettings.groundAttackMotionTimeout : MotionTimeout;

        public EnemyFlyingGroundAttackState(ActorMovementController controller, EnemyFlyingAIContext brain)
            : base(controller)
        {
            _brain = brain;
        }

        public override bool CanTransitionState(string stateName) => true;

        public override bool CanPlayHitReaction(AttackData attackData)
        {
            return base.CanPlayHitReaction(attackData)
                   && _combat != null
                   && !_combat.IsPossibleCollide;
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            motor.SetGroundSolvingActivation(true);

            _combat = _brain.Combat;
            _attackTimer = 0f;
            _isActive = true;

            gameActor.GetComponent<PoiseStat>()?.SetHyperArmor(true);

            float dist = _brain.Detection.DistanceToTarget;
            _currentSkill = _combat.SelectAndExecuteSkill(dist);

            if (_currentSkill != null)
            {
                Debug.Log($"[FlyingGroundAttack] 스킬: {_currentSkill.baseInfo.animKey}");
                var animState = gameActor.Animator.PlayMotion(_currentSkill.baseInfo.animKey, 0.1f);
                if (animState != null)
                    gameActor.Animator.OnMotionSetCompleted += OnAttackEnd;
                else
                {
                    Debug.LogWarning("[FlyingGroundAttack] 모션 재생 실패, 즉시 완료");
                    OnAttackEnd();
                }
            }
            else
            {
                Debug.LogWarning("[FlyingGroundAttack] 스킬 없음, 즉시 완료");
                _isActive = false;
                _brain.OnGroundAttackFinished();
            }
        }

        public override void OnExit(GameActorState toState)
        {
            base.OnExit(toState);
            _isActive = false;
            _combat?.CancelCurrentAction();
            gameActor.Animator.OnMotionSetCompleted -= OnAttackEnd;
            gameActor.GetComponent<PoiseStat>()?.SetHyperArmor(false);
        }

        public override void UpdateState(float deltaTime)
        {
            if (!_isActive || _currentSkill == null) return;

            _attackTimer += deltaTime;

            // 모션 타임아웃 — OnMotionSetCompleted 미발화 대비
            if (_attackTimer >= Cfg_MotionTimeout)
            {
                Debug.LogWarning("[FlyingGroundAttack] 모션 타임아웃, 강제 완료");
                ForceEnd();
                return;
            }

            if (_currentSkill.baseInfo.attackType == AttackType.Melee && _combat.IsPossibleCollide)
                _combat.CheckMeleeAttackHit();
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
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
            _isActive = false;
            _combat?.CancelCurrentAction();
            _brain.OnGroundAttackFinished();
        }

        private void ForceEnd()
        {
            gameActor.Animator.OnMotionSetCompleted -= OnAttackEnd;
            _isActive = false;
            _combat?.CancelCurrentAction();
            _brain.OnGroundAttackFinished();
        }
    }
}
