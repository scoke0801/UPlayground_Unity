using UnityEngine;
using UPlayGround.Components;
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
        protected override ActorStateTag StateTagsCore => ActorStateTag.Combat;
        public override bool AdjustGravity => true;

        private PlayerCombat _combat;
        private PlayerEquipment _equipment;
        private AttackData   _attackData;
        private float        _timer;
        private bool         _comboInputted;
        private bool         _comboIsFinish;   // 강공격 콤보 입력 → 피니시로 처리
        private bool         _changingState;
        private readonly bool _startAsFinish;  // 공중에서 강공격으로 진입 시 true
        private readonly PlayerInterruptAction _forcedAttackAction; // 공중 연계 라우트 진입 시 입력 종류(예: Skill)

        public PlayerJumpAttackState(
            ActorMovementController controller,
            bool startAsFinish = false,
            PlayerInterruptAction forcedAttackAction = PlayerInterruptAction.None) : base(controller)
        {
            _startAsFinish      = startAsFinish;
            _forcedAttackAction = forcedAttackAction;
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            _timer         = 0f;
            _comboInputted = false;
            _comboIsFinish = false;
            _changingState = false;

            _combat = playerActor.GetCombat();
            _equipment = playerActor.GetPlayerEquipment();
            _equipment?.SetMainWeaponDrawn(true);
            ActorWeaponTrailController.StartAttackTrails(_equipment != null ? _equipment : playerActor);

            // 연계 라우트 우선: 매칭되면 점프 공격 대신 라우트를 실행한다(공중 연계, 예: 대시→점프→스킬1).
            // PlayerAttackState와 동일한 ComboRouteRunner 오케스트레이션을 공유해 peek/execute 드리프트를 막는다.
            _attackData = ComboRouteRunner.TryExecuteRoute(
                playerActor, playerController, _combat, _startAsFinish, _forcedAttackAction, out _);
            if (_attackData == null)
                _attackData = _startAsFinish
                    ? _combat?.ExecuteJumpFinishAttack()
                    : _combat?.ExecuteJumpAttack(false);

            var state = _attackData?.motionAsset != null
                ? gameActor.Animator.PlayMotion(_attackData.motionAsset, 0.25f)
                : null;
            if (state != null)
                gameActor.Animator.OnMotionSetCompleted += ChangeToNextState;
        }

        public override void OnExit(GameActorState toState)
        {
            gameActor.Animator.OnMotionSetCompleted -= ChangeToNextState;
            _combat?.ClearHitTargets();
            ActorWeaponTrailController.StopAttackTrails(_equipment != null ? _equipment : playerActor);
            base.OnExit(toState);
        }

        public override bool CanTransitionState(string stateName)
        {
            if (stateName == "Hit") return false;
            return true;
        }

        public override void UpdateState(float deltaTime)
        {
            _timer += deltaTime;

            if (_combat != null && _combat.CanCombo)
            {
                if (Svc.Input.InputBuffer.ConsumeInput(PlayerAction.Attack) != null)
                {
                    _comboInputted = true;
                    _comboIsFinish = false;
                    _combat.CloseComboWindow();
                }
                else if (Svc.Input.InputBuffer.ConsumeInput(PlayerAction.HeavyAttack) != null)
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
                if (Svc.Input.InputBuffer.HasInput(PlayerAction.Dash))
                {
                    if (playerController.TryTransitionToState(new PlayerDashState(controller)))
                    {
                        Svc.Input.InputBuffer.ConsumeInput(PlayerAction.Dash);
                        return;
                    }
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

                var state = _attackData?.motionAsset != null
                    ? gameActor.Animator.PlayMotion(_attackData.motionAsset, 0.1f)
                    : null;
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
            // 착지 FX: ActorSvc.Objects.ShowFX("");
        }
    }
}
