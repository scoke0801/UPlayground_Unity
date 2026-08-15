using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UPlayGround.InputDefine;
using UPlayGround.Manager;
using UPlayGround.MovementController;
using UPlayGround.Animation;

namespace UPlayGround.State
{
    /// <summary>
    /// 플레이어 피격 경직 상태
    ///
    /// [경직 강도별 처리]
    /// - Light  : 경직 없음에 가까움. 즉시 캔슬 허용.
    /// - Hit    : 짧은 경직. cancelWindow 이후 공격/회피 캔슬 가능.
    /// - Heavy  : 긴 경직. cancelWindow가 길고 캔슬 선택지가 회피만.
    /// - KnockBack: 넉백 + 긴 경직. 캔슬 불가.
    ///
    /// 플레이어가 답답함을 느끼는 이유는 "경직 중 아무것도 못 한다"는 점이다.
    /// cancelWindow를 두어 숙련 플레이어는 빠른 반격을 할 수 있고,
    /// 초보 플레이어는 경직이 끝날 때까지 기다리면 된다.
    /// </summary>
    public class PlayerHitState : PlayerActorState
    {
        public override ActorStateId StateId => ActorStateId.Hit;

        private readonly AttackData _attackData;

        // 경직 강도별 캔슬 허용 시간 (애니 시작 후 이 시간 이후부터 캔슬 가능)
        private const float LIGHT_CANCEL_WINDOW  = 0.0f;   // 즉시
        private const float NORMAL_CANCEL_WINDOW = 0.2f;   // 0.2초 후
        private const float HEAVY_CANCEL_WINDOW  = 0.5f;   // 0.5초 후

        private float _cancelWindow;
        private float _elapsedTime;
        private bool _canCancel;
        private bool _wallImpactConsumed;
        private bool _heavyHit;   // Heavy일 때는 회피 캔슬만 허용
        private MotionSet _hitMotionSet;

        public PlayerHitState(ActorMovementController controller, AttackData attackData)
            : base(controller)
        {
            _attackData = attackData;
        }

        public override bool CanTransitionState(ActorStateId fromState) => true;

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            _elapsedTime = 0f;
            _canCancel   = false;
            _wallImpactConsumed = false;

            // 워프 진행 중이면 즉시 clear (Hit 모션이 우선, 헛스윙도 적용 안 함).
            controller.MotionWarp?.ClearTarget();

            // 피격 시 연계 토큰 스트림을 비워 콤보가 피격을 관통하지 않게 한다(설계 §5.1).
            playerActor.ComboInputTracker.Clear();

            playerActor.GetCombat()?.RefreshCombatState();

            // 경직 강도 결정
            var reaction = _attackData?.reactionType ?? AttackReactionType.Hit;
            SetupReaction(reaction);

            var animState = gameActor.Animator.PlayMotion(GetHitAnimKey(), 0.15f);
            if (animState != null)
            {
                // MotionSet은 마지막 포즈에서 재생 상태를 정지한 뒤 자체 타임라인으로 완료한다.
                // AnimancerState.OnEnd는 이 경로에서 발화하지 않을 수 있으므로 디렉터 종료 이벤트를 사용한다.
                _hitMotionSet = gameActor.Animator.CurrentMotionSet;
                gameActor.Animator.OnMotionSetEndedWithReason += OnHitMotionSetEnded;
            }
            else
            {
                OnHitEnd();
            }
        }

        public override void OnExit(GameActorState toState)
        {
            gameActor.Animator.OnMotionSetEndedWithReason -= OnHitMotionSetEnded;
            _hitMotionSet = null;
            base.OnExit(toState);
        }

        private void SetupReaction(AttackReactionType reaction)
        {
            // 물리적 힘(넉백/Pull/Airborne)은 PlayerActor.OnDamaged()에서 일괄 처리.
            // 여기서는 캔슬 윈도우와 경직 강도만 설정한다.
            switch (reaction)
            {
                case AttackReactionType.None:
                case AttackReactionType.Light:
                    _cancelWindow = LIGHT_CANCEL_WINDOW;
                    _heavyHit     = false;
                    break;

                case AttackReactionType.Hit:
                default:
                    _cancelWindow = NORMAL_CANCEL_WINDOW;
                    _heavyHit     = false;
                    break;

                case AttackReactionType.Heavy:
                    _cancelWindow = HEAVY_CANCEL_WINDOW;
                    _heavyHit     = true;
                    break;

                case AttackReactionType.KnockBack:
                    _cancelWindow = float.MaxValue;
                    _heavyHit     = true;
                    break;

                case AttackReactionType.Pull:
                    _cancelWindow = HEAVY_CANCEL_WINDOW;
                    _heavyHit     = true;
                    break;

                case AttackReactionType.Airborne:
                    _cancelWindow = float.MaxValue;
                    _heavyHit     = true;
                    break;

                case AttackReactionType.Knockdown:
                    _cancelWindow = float.MaxValue;
                    _heavyHit     = true;
                    break;

                case AttackReactionType.Stun:
                    _cancelWindow = float.MaxValue;
                    _heavyHit     = true;
                    break;

                case AttackReactionType.Grab:
                    _cancelWindow = float.MaxValue;
                    _heavyHit     = true;
                    break;
            }
        }

        public override void UpdateState(float deltaTime)
        {
            if (ShouldTransitionToAirborne(deltaTime))
            {
                controller.TransitionToState(ActorStateId.Airborne);
                return;
            }

            _elapsedTime += deltaTime;

            if (!_canCancel && _elapsedTime >= _cancelWindow)
                _canCancel = true;

            if (!_canCancel) return;

            // 회피 캔슬 (Heavy 포함 허용)
            var dodgeInput = Svc.Input.InputBuffer.ConsumeInput(PlayerAction.Dodge);
            if (dodgeInput != null)
            {
                controller.TransitionToState(new PlayerDodgeState(controller));
                return;
            }

            if (_heavyHit) return;  // Heavy 이상은 공격 캔슬 불가

            // 공격 캔슬 (일반 Hit 이하만)
            // 입력 소비는 성공한 PlayerAttackState.OnEnter가 승자 입력 하나만 담당한다.
            bool hasAttack      = Svc.Input.InputBuffer.HasInput(PlayerAction.Attack);
            bool hasHeavyAttack = Svc.Input.InputBuffer.HasInput(PlayerAction.HeavyAttack);
            if (hasAttack || hasHeavyAttack)
            {
                if (PlayerAttackState.TryEnter(playerController))
                {
                    return;
                }
            }

            var skillGauge = playerActor.SkillGauge;
            for (int i = 0; i < UPlayGround.Components.PlayerAbilityResourceView.SkillSlotCount; i++)
            {
                if (skillGauge == null) break;
                if (!playerController.HasSkillInput(i)) continue;
                if (skillGauge.CanUseSkill(i) == false) continue;

                if (PlayerAttackState.TryEnter(playerController)) return;
            }
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            currentRotation = currentRotation.normalized;
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (motor.GroundingStatus.IsStableOnGround)
            {
                currentVelocity = motor.GetDirectionTangentToSurface(
                    currentVelocity,
                    motor.GroundingStatus.GroundNormal) * currentVelocity.magnitude;

                currentVelocity = Vector3.Lerp(
                    currentVelocity,
                    Vector3.zero,
                    1 - Mathf.Exp(-controller.StableMovementSharpness * deltaTime));
            }
        }

        private void OnHitEnd()
        {
            if (controller.CurrentState != this)
                return;

            // 회복 직후 경직 내성 부여 — Hit→Idle(찰나)→Hit 재스턴 루프를 차단한다(데미지는 유지).
            playerActor.GrantStaggerImmunity(PlayerActor.StaggerImmunityDuration);
            controller.TransitionToState(ActorStateId.Idle);
        }

        private void OnHitMotionSetEnded(MotionSet motionSet, MotionSetEndReason _)
        {
            if (ReferenceEquals(motionSet, _hitMotionSet))
                OnHitEnd();
        }

        /// <summary>
        /// 넉백으로 밀려나던 중 벽에 부딪힌 경우(환경 넉백 T0).
        /// 플레이어는 T0(잔여 넉백 소멸 + 임팩트)까지만 적용한다 — 추가 경직이나 추가 피해를 주면
        /// 벽 근처에서 조작 불가 누수가 되살아난다.
        /// </summary>
        public override void OnMovementHit(
            UnityEngine.Collider hitCollider,
            Vector3 hitNormal,
            Vector3 hitPoint,
            ref KinematicCharacterController.HitStabilityReport hitStabilityReport)
        {
            if (_wallImpactConsumed)
                return;

            if (UPlayGround.Combat.WallImpactResolver.TryApplyWallImpact(
                    controller,
                    _attackData?.reactionType ?? AttackReactionType.None,
                    hitCollider,
                    hitNormal,
                    hitPoint,
                    hitStabilityReport))
            {
                _wallImpactConsumed = true;
            }
        }

        private UPlayGround.Gameplay.Tag.GameplayTag GetHitAnimKey()
        {
            // 공격별 전용 피격 애니(victimForcedMotionSlot)가 지정돼 있고 보유 모션이면 최우선 사용.
            if (_attackData != null &&
                _attackData.victimForcedMotionSlot != default &&
                playerActor.Animator.HasMotion(_attackData.victimForcedMotionSlot))
                return _attackData.victimForcedMotionSlot;

            var reaction = _attackData?.reactionType ?? AttackReactionType.Hit;

            switch (reaction)
            {
                case AttackReactionType.KnockBack:
                    if (playerActor.Animator.HasMotion(UPlayGround.Data.Actor.Animation.MotionTags.Knockback, true))
                        return UPlayGround.Data.Actor.Animation.MotionTags.Knockback;
                    break;

                case AttackReactionType.Knockdown:
                    if (playerActor.Animator.HasMotion(UPlayGround.Data.Actor.Animation.MotionTags.Knockdown, true))
                        return UPlayGround.Data.Actor.Animation.MotionTags.Knockdown;
                    break;

                case AttackReactionType.Airborne:
                case AttackReactionType.Pull:
                    // 방향 무관하게 앞 경직
                    return UPlayGround.Data.Actor.Animation.MotionTags.Hit_F;
            }

            if (_attackData == null) return UPlayGround.Data.Actor.Animation.MotionTags.Hit_F;

            Vector3 localDir = playerActor.transform.InverseTransformDirection(_attackData.attackDirection);

            if (Mathf.Abs(localDir.x) > Mathf.Abs(localDir.z))
                return localDir.x > 0 ? UPlayGround.Data.Actor.Animation.MotionTags.Hit_R : UPlayGround.Data.Actor.Animation.MotionTags.Hit_L;
            else
                return localDir.z > 0 ? UPlayGround.Data.Actor.Animation.MotionTags.Hit_F : UPlayGround.Data.Actor.Animation.MotionTags.Hit_B;
        }
    }
}
