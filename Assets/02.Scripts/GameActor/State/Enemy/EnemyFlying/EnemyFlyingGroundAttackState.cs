using UnityEngine;
using UPlayGround.Combat;
using UPlayGround.Components;
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
        public override ActorStateId StateId => ActorStateId.Flying_GroundAttack;
        public override bool BlocksBehaviorTree => true;
        public override GravityOwnership GravityOwner => GravityOwnership.State;

        private readonly EnemyFlyingAIContext _brain;
        private EnemyCombat _combat;
        private float _attackTimer;
        private bool _isActive;
        private bool _returnPending;
        private AbilityAttackInfo _currentSkill;

        private const float MotionTimeout = 3.0f;

        private float Cfg_MotionTimeout => _brain.FlyingSettings ? _brain.FlyingSettings.groundAttackMotionTimeout : MotionTimeout;

        public EnemyFlyingGroundAttackState(ActorMovementController controller, EnemyFlyingAIContext brain)
            : base(controller)
        {
            _brain = brain;
        }

        public override bool CanTransitionState(ActorStateId fromState) => true;

        public override bool CanPlayHitReaction(in HitContext hit)
        {
            return base.CanPlayHitReaction(hit)
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
            _returnPending = false;

            gameActor.GetComponent<PoiseStat>()?.SetHyperArmor(true);

            float dist = _brain.Detection.DistanceToTarget;
            _currentSkill = _combat.SelectAndExecuteSkill(dist);

            if (_currentSkill != null)
            {
                Debug.Log($"[FlyingGroundAttack] 스킬 모션: {_combat.CurrentMotionAsset?.name ?? "-"}");
                var animState = _combat.CurrentMotionAsset != null
                    ? gameActor.Animator.PlayAbilityMotion(_currentSkill.motionKey, 0.1f)
                    : null;
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
                ReturnToChase();
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
            // 복귀는 항상 여기서 처리한다. OnEnter 안(모션 재생 실패·스킬 없음)에서 바로
            // 전이하면 진입 도중 재진입이 되므로 플래그만 세우고 다음 틱에 빠져나간다.
            if (_returnPending)
            {
                _returnPending = false;
                controller.TransitionToState(new EnemyFlyingChaseState(controller, _brain));
                return;
            }

            if (!_isActive || _currentSkill == null) return;

            _attackTimer += deltaTime;

            // 모션 타임아웃 — OnMotionSetCompleted 미발화 대비
            if (_attackTimer >= Cfg_MotionTimeout)
            {
                Debug.LogWarning("[FlyingGroundAttack] 모션 타임아웃, 강제 완료");
                ForceEnd();
                return;
            }

            // 검출 요청만 표시하고 실제 Overlap은 EnemyCombat.LateUpdate에서 수행한다(갓 적용된 포즈).
            if ((_currentSkill.baseInfo.attackType == AttackType.Melee
                 || _combat.HasActiveExplicitCollision)
                && _combat.IsPossibleCollide)
                _combat.RequestMeleeHitCheck();
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
                currentVelocity = gameActor.Animator.GetRootMotionStepVelocity(deltaTime);

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
            _combat?.CompleteCurrentAbility();
            _combat?.CancelCurrentAction();
            _brain.OnGroundAttackFinished();
            ReturnToChase();
        }

        private void ForceEnd()
        {
            gameActor.Animator.OnMotionSetCompleted -= OnAttackEnd;
            _isActive = false;
            _combat?.CompleteCurrentAbility();
            _combat?.CancelCurrentAction();
            _brain.OnGroundAttackFinished();
            ReturnToChase();
        }

        /// <summary>
        /// OnGroundAttackFinished는 카운터만 리셋한다. 이 상태는 BlocksBehaviorTree라서
        /// BT가 스스로 빼낼 수 없으므로 공격이 끝나면 Chase로 복귀시킨다.
        /// Circle/Retreat과 같은 규약 — 다음 판단(재공격 / 이륙 / 이탈)은 BT가 Chase에서 내린다.
        /// 실제 전이는 재진입을 피하려고 UpdateState에서 수행한다.
        /// </summary>
        private void ReturnToChase() => _returnPending = true;
    }
}
