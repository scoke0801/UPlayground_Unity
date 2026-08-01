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
        public override ActorStateId StateId => ActorStateId.JumpAttack;
        protected override ActorStateTag StateTagsCore => ActorStateTag.Combat;

        private PlayerCombat _combat;
        private PlayerEquipment _equipment;
        private MotionWarpController _motionWarp;
        private AttackData   _attackData;
        private float        _timer;
        private bool         _comboInputted;
        private bool         _comboIsFinish;   // 강공격 콤보 입력 → 피니시로 처리
        private bool         _changingState;
        private readonly bool _startAsFinish;  // 공중에서 강공격으로 진입 시 true
        private readonly PlayerInterruptAction _forcedAttackAction; // 공중 연계 라우트 진입 시 입력 종류(예: Skill)
        private AerialMovementProfile _aerialMovement;
        private float _physicsElapsed;
        private float _apexElapsed;
        private float _gravityScale = 1f;
        private bool _entryVelocityApplied;

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
            _physicsElapsed = 0f;
            _apexElapsed = 0f;
            _gravityScale = 1f;
            _entryVelocityApplied = false;

            _combat = playerActor.GetCombat();
            _motionWarp = controller.MotionWarp;
            // 이 공격 동안엔 첫 타겟만 유지 — 타임라인 워프 이벤트가 다른 적으로 재결정하는 것을 막는다.
            // (JumpAttack 자체는 타겟을 지정하지 않으므로, 모션 이벤트가 처음 잡은 타겟이 잠금 대상이 된다.)
            _motionWarp?.BeginTargetLock();
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
            ConfigureAerialMovement();

            var state = _attackData?.motionAsset != null
                ? gameActor.Animator.PlayMotion(_attackData.motionAsset, 0.25f)
                : null;
            if (state != null)
                gameActor.Animator.OnMotionSetCompleted += ChangeToNextState;
            else
            {
                _combat?.ResetCombo();
                TransitionAfterAerialAttack();
            }
        }

        public override void OnExit(GameActorState toState)
        {
            gameActor.Animator.OnMotionSetCompleted -= ChangeToNextState;
            _combat?.ClearHitTargets();
            ActorWeaponTrailController.StopAttackTrails(_equipment != null ? _equipment : playerActor);
            _motionWarp?.EndTargetLock();
            base.OnExit(toState);
        }

        public override bool CanTransitionState(ActorStateId fromState)
        {
            if (fromState == ActorStateId.Hit) return false;
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
                // 콤보 다음 타격 = 새 공격 스코프. 여기서만 타겟을 다시 잡는다.
                _motionWarp?.BeginTargetLock();
                _attackData = _comboIsFinish
                    ? _combat?.ExecuteJumpFinishAttack()
                    : _combat?.ExecuteJumpAttack(true);
                ConfigureAerialMovement();

                var state = _attackData?.motionAsset != null
                    ? gameActor.Animator.PlayMotion(_attackData.motionAsset, 0.1f)
                    : null;
                if (state == null)
                {
                    _combat?.ResetCombo();
                    TransitionAfterAerialAttack();
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
                TransitionAfterAerialAttack();
            }
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            currentRotation *= gameActor.Animator.RootMotionStepDeltaRotation;
            currentRotation = currentRotation.normalized;
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            base.UpdateVelocity(ref currentVelocity, deltaTime);
            _physicsElapsed += deltaTime;

            Vector3 up = motor.CharacterUp;
            float verticalSpeed = Vector3.Dot(currentVelocity, up);
            Vector3 planarVelocity = Vector3.ProjectOnPlane(currentVelocity, up);

            if (!_entryVelocityApplied)
            {
                if (_attackData?.isDiveAttack == true)
                    verticalSpeed = -Mathf.Max(0f, _attackData.diveDescentSpeed);
                else if (_aerialMovement.minimumEntryUpwardSpeed > 0f)
                    verticalSpeed = Mathf.Max(verticalSpeed, _aerialMovement.minimumEntryUpwardSpeed);

                _entryVelocityApplied = true;
            }

            float rootInfluence = Mathf.Clamp01(_aerialMovement.horizontalRootMotionInfluence);
            if (rootInfluence > 0f)
            {
                Vector3 rootVelocity = gameActor.Animator.GetRootMotionStepVelocity(deltaTime);
                Vector3 rootPlanarVelocity = Vector3.ProjectOnPlane(rootVelocity, up);
                planarVelocity = Vector3.Lerp(planarVelocity, rootPlanarVelocity, rootInfluence);
            }

            if (_attackData?.isDiveAttack == true)
            {
                _gravityScale = 1f;
            }
            else if (_physicsElapsed <= _aerialMovement.startupDuration)
            {
                _gravityScale = _aerialMovement.startupGravityScale;
            }
            else if (Mathf.Abs(verticalSpeed) <= _aerialMovement.apexVelocityThreshold
                     && _apexElapsed < _aerialMovement.maximumApexDuration)
            {
                _apexElapsed += deltaTime;
                _gravityScale = _aerialMovement.apexGravityScale;
            }
            else
            {
                _gravityScale = _aerialMovement.recoveryGravityScale;
            }

            currentVelocity = planarVelocity + up * verticalSpeed;
        }

        public override float GetGravityMultiplier(float verticalSpeed)
            => base.GetGravityMultiplier(verticalSpeed) * Mathf.Max(0f, _gravityScale);

        public override void ConstrainVelocityAfterGravity(
            ref Vector3 currentVelocity,
            float deltaTime)
        {
            float terminalFallSpeed = _aerialMovement.terminalFallSpeed;
            if (terminalFallSpeed <= 0f)
                return;

            Vector3 up = motor.CharacterUp;
            float verticalSpeed = Vector3.Dot(currentVelocity, up);
            if (verticalSpeed >= -terminalFallSpeed)
                return;

            currentVelocity += up * (-terminalFallSpeed - verticalSpeed);
        }

        private void ConfigureAerialMovement()
        {
            _aerialMovement = _attackData?.aerialMovement ?? new AerialMovementProfile();
            _physicsElapsed = 0f;
            _apexElapsed = 0f;
            _gravityScale = 1f;
            _entryVelocityApplied = false;
        }

        private void TransitionAfterAerialAttack()
        {
            if (!motor.GroundingStatus.IsStableOnGround)
            {
                controller.TransitionToState(ActorStateId.Airborne);
                return;
            }

            if (playerController.HasMoveInput())
                controller.TransitionToState(ActorStateId.GroundMove);
            else
                controller.TransitionToState(ActorStateId.Idle);
        }

        private void OnLanded()
        {
            // 착지 FX: ActorSvc.Objects.ShowFX("");
        }
    }
}
