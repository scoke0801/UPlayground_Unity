using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UPlayGround.InputDefine;
using UPlayGround.Manager;
using UPlayGround.MovementController;

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
        public override string StateName => "Hit";

        private readonly AttackData _attackData;

        // 경직 강도별 캔슬 허용 시간 (애니 시작 후 이 시간 이후부터 캔슬 가능)
        private const float LIGHT_CANCEL_WINDOW  = 0.0f;   // 즉시
        private const float NORMAL_CANCEL_WINDOW = 0.2f;   // 0.2초 후
        private const float HEAVY_CANCEL_WINDOW  = 0.5f;   // 0.5초 후

        private float _cancelWindow;
        private float _elapsedTime;
        private bool _canCancel;
        private bool _heavyHit;   // Heavy일 때는 회피 캔슬만 허용

        public PlayerHitState(ActorMovementController controller, AttackData attackData)
            : base(controller)
        {
            _attackData = attackData;
        }

        public override bool CanTransitionState(string stateName) => true;

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            _elapsedTime = 0f;
            _canCancel   = false;

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
                animState.OwnedEvents.OnEnd = OnHitEnd;
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
                controller.TransitionToState(new PlayerAirborneState(controller));
                return;
            }

            _elapsedTime += deltaTime;

            if (!_canCancel && _elapsedTime >= _cancelWindow)
                _canCancel = true;

            if (!_canCancel) return;

            // 회피 캔슬 (Heavy 포함 허용)
            var dodgeInput = InputManager.Instance.InputBuffer.ConsumeInput(PlayerAction.Dodge);
            if (dodgeInput != null)
            {
                controller.TransitionToState(new PlayerDodgeState(controller));
                return;
            }

            if (_heavyHit) return;  // Heavy 이상은 공격 캔슬 불가

            // 공격 캔슬 (일반 Hit 이하만)
            // 입력 소비는 TryEnter 성공 시에만 일어나도록 HasInput으로 사전 확인.
            bool hasAttack      = InputManager.Instance.InputBuffer.HasInput(PlayerAction.Attack);
            bool hasHeavyAttack = InputManager.Instance.InputBuffer.HasInput(PlayerAction.HeavyAttack);
            if (hasAttack || hasHeavyAttack)
            {
                if (PlayerAttackState.TryEnter(playerController))
                {
                    if (hasAttack)      InputManager.Instance.InputBuffer.ConsumeInput(PlayerAction.Attack);
                    if (hasHeavyAttack) InputManager.Instance.InputBuffer.ConsumeInput(PlayerAction.HeavyAttack);
                    return;
                }
            }

            var skillGauge = playerActor.SkillGauge;
            for (int i = 0; i < UPlayGround.Component.PlayerSkillGauge.SkillSlotCount; i++)
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

            controller.TransitionToState(new PlayerIdleState(controller));
        }

        private AnimKey GetHitAnimKey()
        {
            // 공격별 전용 피격 애니(victimForcedAnimKey)가 지정돼 있고 보유 모션이면 최우선 사용.
            if (_attackData != null &&
                _attackData.victimForcedAnimKey != AnimKey.None &&
                playerActor.Animator.HasMotion(_attackData.victimForcedAnimKey))
                return _attackData.victimForcedAnimKey;

            var reaction = _attackData?.reactionType ?? AttackReactionType.Hit;

            switch (reaction)
            {
                case AttackReactionType.KnockBack:
                    if (playerActor.Animator.HasMotion(AnimKey.Knockback, true))
                        return AnimKey.Knockback;
                    break;

                case AttackReactionType.Knockdown:
                    if (playerActor.Animator.HasMotion(AnimKey.Knockdown, true))
                        return AnimKey.Knockdown;
                    break;

                case AttackReactionType.Airborne:
                case AttackReactionType.Pull:
                    // 방향 무관하게 앞 경직
                    return AnimKey.Hit_F;
            }

            if (_attackData == null) return AnimKey.Hit_F;

            Vector3 localDir = playerActor.transform.InverseTransformDirection(_attackData.attackDirection);

            if (Mathf.Abs(localDir.x) > Mathf.Abs(localDir.z))
                return localDir.x > 0 ? AnimKey.Hit_R : AnimKey.Hit_L;
            else
                return localDir.z > 0 ? AnimKey.Hit_F : AnimKey.Hit_B;
        }
    }
}
