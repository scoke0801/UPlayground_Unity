using UnityEngine;
using UPlayGround.Component;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UPlayGround.InputDefine;
using UPlayGround.Manager;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    public class PlayerJumpAttackState : PlayerActorState
    {
        public override string StateName => "JumpAttack";
        public override bool AdjustGravity => true;

        private PlayerCombat _combat;
        private AttackData   _attackData;
        private float        _timer;
        private bool         _comboInputted;
        private bool         _comboIsFinish;   // 강공격 콤보 입력 → 피니시로 처리
        private bool         _changingState;
        private readonly bool _startAsFinish;  // 공중에서 강공격으로 진입 시 true

        public PlayerJumpAttackState(ActorMovementController controller, bool startAsFinish = false) : base(controller)
        {
            _startAsFinish = startAsFinish;
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            _timer         = 0f;
            _comboInputted = false;
            _comboIsFinish = false;
            _changingState = false;

            _combat = playerActor.GetCombat();
            _attackData = _startAsFinish
                ? _combat?.ExecuteJumpFinishAttack()
                : _combat?.ExecuteJumpAttack(false);

            AnimKey animKey = _attackData?.animKey ?? AnimKey.JumpAttack_1;
            var state = gameActor.Animator.PlayMotion(animKey, 0.25f);
            if (state != null)
                gameActor.Animator.OnMotionSetCompleted += ChangeToNextState;
        }

        public override void OnExit(GameActorState toState)
        {
            gameActor.Animator.OnMotionSetCompleted -= ChangeToNextState;
            _combat?.ClearHitTargets();
            base.OnExit(toState);
        }

        public override bool CanTransitionState(string stateName)
        {
            if (stateName == "Hit")
                return false;
            return true;
        }

        public override void UpdateState(float deltaTime)
        {
            _timer += deltaTime;

            if (_combat != null && _combat.CanCombo)
            {
                if (InputManager.Instance.InputBuffer.ConsumeInput(PlayerAction.Attack) != null)
                {
                    _comboInputted = true;
                    _comboIsFinish = false;
                    _combat.CloseComboWindow();
                }
                else if (InputManager.Instance.InputBuffer.ConsumeInput(PlayerAction.HeavyAttack) != null)
                {
                    _comboInputted = true;
                    _comboIsFinish = true;
                    _combat.CloseComboWindow();
                }
            }

            // 충돌 판정이 끝난 직후 콤보 입력이 있으면 즉시 다음 공격으로 전환
            if (_combat != null && !_combat.IsPossibleCollide && _comboInputted)
            {
                ChangeToNextState();
                return;
            }

            // 착지 시: 콤보 입력 대기 중이면 현재 애니메이션 유지
            if (motor.GroundingStatus.IsStableOnGround && !_comboInputted)
            {
                if (InputManager.Instance.InputBuffer.ConsumeInput(PlayerAction.Dash) != null)
                {
                    if (playerController.TryTransitionToState(new PlayerDashState(controller)))
                        return;
                }
                OnLanded();
            }
        }

        private void ChangeToNextState()
        {
            if (_changingState) return;
            _changingState = true;

            _combat?.ClearHitTargets();

            if (_comboInputted)
            {
                _attackData = _comboIsFinish
                    ? _combat?.ExecuteJumpFinishAttack()
                    : _combat?.ExecuteJumpAttack(true);

                AnimKey animKey = _attackData?.animKey ?? AnimKey.JumpAttack_1;
                var state = gameActor.Animator.PlayMotion(animKey, 0.1f);
                if (state == null)
                {
                    _combat?.ResetCombo();
                    if (playerController.HasMoveInput())
                        controller.TransitionToState(new PlayerGroundMoveState(controller));
                    else
                        controller.TransitionToState(new PlayerIdleState(controller));
                    return;
                }
                _comboInputted = false;
                _comboIsFinish = false;
                _timer         = 0f;
                _changingState = false;
            }
            else
            {
                _combat?.ResetCombo();
                if (playerController.HasMoveInput())
                    controller.TransitionToState(new PlayerGroundMoveState(controller));
                else
                    controller.TransitionToState(new PlayerIdleState(controller));
            }
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            currentRotation *= gameActor.Animator.DeltaRotation;
            currentRotation = currentRotation.normalized;
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            base.UpdateVelocity(ref currentVelocity, deltaTime);
            currentVelocity = motor.CharacterUp * -15f;
        }

        private void OnLanded()
        {
            // 착지 FX: GameObjectManager.Instance.ShowFX("");
        }
    }
}
