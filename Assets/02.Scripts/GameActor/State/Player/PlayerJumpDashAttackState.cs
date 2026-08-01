using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    public class PlayerJumpDashAttackState : PlayerActorState
    {
        public override ActorStateId StateId => ActorStateId.JumpDashAttack;
        protected override ActorStateTag StateTagsCore => ActorStateTag.Combat;

        private float _decelerationDuration = 0.5f;

        private PlayerCombat _combat;
        private PlayerEquipment _equipment;
        private AttackData   _attackData;
        private bool         _changingState;
        private Vector3      _attackDirection;
        private float        _elapsed;
        private float        _apexElapsed;
        private float        _gravityScale = 1f;
        private bool         _entryVelocityApplied;
        private AerialMovementProfile _aerialMovement;

        public PlayerJumpDashAttackState(ActorMovementController controller) : base(controller) { }

        public override bool CanTransitionState(ActorStateId fromState)
        {
            if (fromState == ActorStateId.Hit) return false;
            return true;
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            _changingState   = false;
            _elapsed         = 0f;
            _apexElapsed     = 0f;
            _gravityScale    = 1f;
            _entryVelocityApplied = false;
            _attackDirection = motor.CharacterForward;
            _combat          = playerActor.GetCombat();
            _equipment       = playerActor.GetPlayerEquipment();
            _equipment?.SetMainWeaponDrawn(true);
            ActorWeaponTrailController.StartAttackTrails(_equipment != null ? _equipment : playerActor);
            _attackData      = _combat?.ExecuteJumpDashAttack();
            _aerialMovement  = _attackData?.aerialMovement ?? new AerialMovementProfile();

            var state = _attackData?.motionAsset != null
                ? gameActor.Animator.PlayMotion(_attackData.motionAsset, 0.1f)
                : null;
            if (state != null)
            {
                _decelerationDuration = state.Duration;
                gameActor.Animator.OnMotionSetCompleted += ChangeToNextState;
            }
            else
                ChangeToNextState();
        }

        public override void OnExit(GameActorState toState)
        {
            gameActor.Animator.OnMotionSetCompleted -= ChangeToNextState;
            _combat?.ClearHitTargets();
            ActorWeaponTrailController.StopAttackTrails(_equipment != null ? _equipment : playerActor);
            base.OnExit(toState);
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            base.UpdateVelocity(ref currentVelocity, deltaTime);

            _elapsed += deltaTime;
            float t = Mathf.Clamp01(_elapsed / _decelerationDuration);
            Vector3 up = motor.CharacterUp;
            float verticalSpeed = Vector3.Dot(currentVelocity, up);
            if (!_entryVelocityApplied)
            {
                if (_attackData?.isDiveAttack == true)
                    verticalSpeed = -Mathf.Max(0f, _attackData.diveDescentSpeed);
                else if (_aerialMovement.minimumEntryUpwardSpeed > 0f)
                    verticalSpeed = Mathf.Max(verticalSpeed, _aerialMovement.minimumEntryUpwardSpeed);
                _entryVelocityApplied = true;
            }

            if (_attackData?.isDiveAttack == true)
            {
                _gravityScale = 1f;
            }
            else if (_elapsed <= _aerialMovement.startupDuration)
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

            Vector3 dashPlanar = Vector3.ProjectOnPlane(_attackDirection, up).normalized
                                 * (controller.DashSpeed * (1f - t));
            currentVelocity = dashPlanar + up * verticalSpeed;
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
            if (verticalSpeed < -terminalFallSpeed)
                currentVelocity += up * (-terminalFallSpeed - verticalSpeed);
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            currentRotation *= gameActor.Animator.RootMotionStepDeltaRotation;
            currentRotation  = currentRotation.normalized;
        }

        private void ChangeToNextState()
        {
            if (_changingState) return;
            _changingState = true;

            _combat?.ResetCombo();
            _combat?.ClearHitTargets();

            if (!motor.GroundingStatus.IsStableOnGround)
            {
                playerController.TransitionToState(ActorStateId.Airborne);
                return;
            }

            if (playerController.HasMoveInput())
                controller.TransitionToState(ActorStateId.GroundMove);
            else
                controller.TransitionToState(ActorStateId.Idle);
        }
    }
}
