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
                    if (_attackData != null)
                        controller.AddVelocity(_attackData.attackDirection.normalized * _attackData.knockbackForce);
                    break;

                case AttackReactionType.Pull:
                    _cancelWindow = HEAVY_CANCEL_WINDOW;
                    _heavyHit     = true;
                    if (_attackData?.attacker != null)
                    {
                        Vector3 pullDir = (_attackData.attacker.transform.position - motor.TransientPosition).normalized;
                        pullDir.y = 0f;
                        controller.AddVelocity(pullDir * _attackData.pullForce);
                    }
                    break;

                case AttackReactionType.Airborne:
                    _cancelWindow = float.MaxValue;
                    _heavyHit     = true;
                    // 수평 넉백 + 위로 띄움
                    if (_attackData != null)
                    {
                        Vector3 launchDir = _attackData.attackDirection.normalized;
                        launchDir.y = 0f;
                        controller.AddVelocity(launchDir * _attackData.knockbackForce 
                                               + Vector3.up * _attackData.airborneForce);
                    }
                    break;

                case AttackReactionType.Knockdown:
                    _cancelWindow = float.MaxValue;
                    _heavyHit     = true;
                    break;

                case AttackReactionType.Stun:
                    _cancelWindow = float.MaxValue;
                    _heavyHit     = true;
                    break;

                // Grab과 LaunchSmash는 전용 State로 처리되므로 여기 오지 않지만 안전장치
                case AttackReactionType.Grab:
                    _cancelWindow = float.MaxValue;
                    _heavyHit     = true;
                    break;
            }
        }

        public override void UpdateState(float deltaTime)
        {
            if (!motor.GroundingStatus.IsStableOnGround)
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
            if (InputManager.Instance.InputBuffer.ConsumeInput(PlayerAction.Attack) != null ||
                InputManager.Instance.InputBuffer.ConsumeInput(PlayerAction.HeavyAttack) != null)
            {
                playerController.TransitionToState(new PlayerAttackState(playerController));
                return;
            }

            for (int i = 0; i < 4; i++)
            {
                if (!playerController.HasSkillInput(i)) continue;
                playerController.TransitionToState(new PlayerAttackState(playerController));
                return;
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
            controller.TransitionToState(new PlayerIdleState(controller));
        }

        private AnimKey GetHitAnimKey()
        {
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

            Vector3 localDir = playerActor.transform.InverseTransformDirection(
                (_attackData.attackDirection - playerActor.transform.position).normalized);

            if (Mathf.Abs(localDir.x) > Mathf.Abs(localDir.z))
                return localDir.x > 0 ? AnimKey.Hit_R : AnimKey.Hit_L;
            else
                return localDir.z > 0 ? AnimKey.Hit_F : AnimKey.Hit_B;
        }
    }
}
